using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System; using System.Collections.Generic; using System.Threading.Tasks; using CommunityToolkit.Mvvm.ComponentModel; using CommunityToolkit.Mvvm.Input; using Microsoft.Extensions.Logging; using BusinessManager.Domain.Interfaces; using BusinessManager.Domain.Entities; using BusinessManager.Domain.DTOs; using BusinessManager.Domain.Enums; using BusinessManager.Application.Services;
using BusinessManager.App.Services;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.DTOs;
using BusinessManager.Domain.Enums;

namespace BusinessManager.App.ViewModels;

public partial class SalesViewModel : ObservableObject
{
    private readonly ISaleService _saleService;
    private readonly IServiceItemRepository _serviceItemRepository;
    private readonly IProductRepository _productRepository;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<SalesViewModel> _logger;
    private readonly User _currentUser;

    [ObservableProperty]
    private ObservableCollection<SaleDto> _sales = new();

    [ObservableProperty]
    private ObservableCollection<SaleItemDto> _currentSaleItems = new();

    [ObservableProperty]
    private ObservableCollection<ServiceItem> _serviceItems = new();

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private SaleDto? _selectedSale;

    [ObservableProperty]
    private SaleItemDto? _selectedSaleItem;

    [ObservableProperty]
    private decimal _subtotal;

    [ObservableProperty]
    private decimal _taxAmount = 0;

    [ObservableProperty]
    private decimal _discountAmount = 0;

    [ObservableProperty]
    private decimal _totalAmount;

    [ObservableProperty]
    private decimal _amountPaid;

    [ObservableProperty]
    private decimal _changeAmount;

    [ObservableProperty]
    private string _customerName = string.Empty;

    [ObservableProperty]
    private string _customerPhone = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private DateTime _filterStartDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _filterEndDate = DateTime.Today.AddDays(1).AddTicks(-1);

    [ObservableProperty]
    private string _searchText = string.Empty;

    public SalesViewModel(
        ISaleService saleService,
        IServiceItemRepository serviceItemRepository,
        IProductRepository productRepository,
        INotificationService notificationService,
        IDialogService dialogService,
        ILogger<SalesViewModel> logger,
        User currentUser)
    {
        _saleService = saleService;
        _serviceItemRepository = serviceItemRepository;
        _productRepository = productRepository;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _logger = logger;
        _currentUser = currentUser;

        LoadSalesCommand = new RelayCommand(async () => await LoadSalesAsync());
        AddServiceItemCommand = new RelayCommand<ServiceItem>(AddServiceItem);
        AddProductCommand = new RelayCommand<Product>(AddProduct);
        RemoveSaleItemCommand = new RelayCommand<SaleItemDto>(RemoveSaleItem);
        SaveSaleCommand = new RelayCommand(async () => await SaveSaleAsync(), CanSaveSale);
        ClearSaleCommand = new RelayCommand(ClearSale);
        DeleteSaleCommand = new RelayCommand<SaleDto>(async (sale) => await DeleteSaleAsync(sale), CanDeleteSale);
        RefreshCommand = new RelayCommand(async () => await LoadSalesAsync());
        SearchCommand = new RelayCommand(async () => await FilterSalesAsync());
    }

    public IRelayCommand LoadSalesCommand { get; }
    public IRelayCommand<ServiceItem> AddServiceItemCommand { get; }
    public IRelayCommand<Product> AddProductCommand { get; }
    public IRelayCommand<SaleItemDto> RemoveSaleItemCommand { get; }
    public IRelayCommand SaveSaleCommand { get; }
    public IRelayCommand ClearSaleCommand { get; }
    public IRelayCommand<SaleDto> DeleteSaleCommand { get; }
    public IRelayCommand RefreshCommand { get; }
    public IRelayCommand SearchCommand { get; }

    public async Task InitializeAsync()
    {
        await LoadSalesAsync();
        await LoadServiceItemsAsync();
        await LoadProductsAsync();
    }

    private async Task LoadSalesAsync()
    {
        try
        {
            IsLoading = true;
            
            var sales = await _saleService.GetSalesByDateRangeAsync(FilterStartDate, FilterEndDate);
            var salesDtos = sales.Select(s => new SaleDto
            {
                Id = s.Id,
                ReceiptNumber = s.ReceiptNumber,
                SaleDate = s.SaleDate,
                TotalAmount = s.TotalAmount,
                CustomerName = s.CustomerName ?? "",
                UserName = s.User.FullName,
                SaleItems = s.SaleItems.Select(si => new SaleItemDto
                {
                    Id = si.Id,
                    Description = si.Description ?? "",
                    Quantity = si.Quantity,
                    UnitPrice = si.UnitPrice,
                    TotalPrice = si.TotalPrice,
                    ServiceName = si.ServiceItem?.Name,
                    ProductName = si.Product?.Name
                }).ToList()
            });

            if (!string.IsNullOrEmpty(SearchText))
            {
                salesDtos = salesDtos.Where(s => 
                    s.ReceiptNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    s.CustomerName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    s.UserName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            Sales = new ObservableCollection<SaleDto>(salesDtos.OrderByDescending(s => s.SaleDate));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading sales");
            _notificationService.ShowError("Error loading sales");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadServiceItemsAsync()
    {
        try
        {
            var serviceItems = await _serviceItemRepository.GetActiveServicesAsync();
            ServiceItems = new ObservableCollection<ServiceItem>(serviceItems.OrderBy(si => si.Category).ThenBy(si => si.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading service items");
        }
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            var products = await _productRepository.GetActiveProductsAsync();
            Products = new ObservableCollection<Product>(products.OrderBy(p => p.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading products");
        }
    }

    private void AddServiceItem(ServiceItem? serviceItem)
    {
        if (serviceItem == null) return;

        var amountInput = _dialogService.ShowInputDialog(
            $"Enter amount received for {serviceItem.Name}:",
            "Record Activity",
            serviceItem.DefaultPrice.ToString("0"));

        if (string.IsNullOrWhiteSpace(amountInput) || !decimal.TryParse(amountInput, out var amount) || amount <= 0)
        {
            return;
        }

        var saleItem = new SaleItemDto
        {
            Description = serviceItem.Name,
            Quantity = 1,
            UnitPrice = amount,
            TotalPrice = amount,
            ServiceName = serviceItem.Name
        };

        CurrentSaleItems.Add(saleItem);
        CalculateTotals();
    }

    private void AddProduct(Product? product)
    {
        if (product == null) return;

        if (product.CurrentStock <= 0)
        {
            _notificationService.ShowWarning($"Product {product.Name} is out of stock");
            return;
        }

        var saleItem = new SaleItemDto
        {
            Description = product.Name,
            Quantity = 1,
            UnitPrice = product.SellingPrice,
            TotalPrice = product.SellingPrice,
            ProductName = product.Name
        };

        CurrentSaleItems.Add(saleItem);
        CalculateTotals();
    }

    private void RemoveSaleItem(SaleItemDto? saleItem)
    {
        if (saleItem == null) return;

        CurrentSaleItems.Remove(saleItem);
        CalculateTotals();
    }

    private void CalculateTotals()
    {
        Subtotal = CurrentSaleItems.Sum(si => si.TotalPrice);
        TotalAmount = Subtotal + TaxAmount - DiscountAmount;
        AmountPaid = TotalAmount;
        ChangeAmount = 0;
        SaveSaleCommand.NotifyCanExecuteChanged();
    }

    private bool CanSaveSale()
    {
        return CurrentSaleItems.Any() && TotalAmount > 0;
    }

    private async Task SaveSaleAsync()
    {
        try
        {
            if (!CanSaveSale())
            {
                _notificationService.ShowWarning("Add at least one item before saving.");
                return;
            }

            AmountPaid = TotalAmount;
            ChangeAmount = 0;

            var sale = new Sale
            {
                SaleDate = DateTime.UtcNow,
                Subtotal = Subtotal,
                TaxAmount = TaxAmount,
                DiscountAmount = DiscountAmount,
                TotalAmount = TotalAmount,
                AmountPaid = AmountPaid,
                ChangeAmount = ChangeAmount,
                CustomerName = string.IsNullOrWhiteSpace(CustomerName) ? null : CustomerName,
                CustomerPhone = string.IsNullOrWhiteSpace(CustomerPhone) ? null : CustomerPhone,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                UserId = _currentUser.Id
            };

            var saleItems = CurrentSaleItems.Select(si => new SaleItem
            {
                Quantity = si.Quantity,
                UnitPrice = si.UnitPrice,
                TotalPrice = si.TotalPrice,
                Description = si.Description,
                ServiceItemId = si.ServiceName != null ? 
                    ServiceItems.FirstOrDefault(s => s.Name == si.ServiceName)?.Id : null,
                ProductId = si.ProductName != null ? 
                    Products.FirstOrDefault(p => p.Name == si.ProductName)?.Id : null
            }).ToList();

            await _saleService.CreateSaleAsync(sale, saleItems);

            _notificationService.ShowSuccess("Transaction saved. Ready for next client.");
            ClearSale();
            await LoadSalesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving sale");
            _notificationService.ShowError("Error saving sale");
        }
    }

    private void ClearSale()
    {
        CurrentSaleItems.Clear();
        CustomerName = string.Empty;
        CustomerPhone = string.Empty;
        Notes = string.Empty;
        AmountPaid = 0;
        DiscountAmount = 0;
        TaxAmount = 0;
        CalculateTotals();
    }

    private bool CanDeleteSale(SaleDto? sale)
    {
        return sale != null && _currentUser.Role == UserRole.Admin;
    }

    private async Task DeleteSaleAsync(SaleDto? sale)
    {
        if (sale == null) return;

        if (!_notificationService.ShowConfirmation($"Are you sure you want to delete sale {sale.ReceiptNumber}?"))
            return;

        try
        {
            await _saleService.DeleteSaleAsync(sale.Id);
            _notificationService.ShowSuccess("Sale deleted successfully");
            await LoadSalesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sale");
            _notificationService.ShowError("Error deleting sale");
        }
    }

    private async Task FilterSalesAsync()
    {
        await LoadSalesAsync();
    }

    partial void OnAmountPaidChanged(decimal value)
    {
        CalculateTotals();
    }

    partial void OnDiscountAmountChanged(decimal value)
    {
        CalculateTotals();
    }

    partial void OnTaxAmountChanged(decimal value)
    {
        CalculateTotals();
    }

    partial void OnFilterStartDateChanged(DateTime value)
    {
        if (FilterStartDate > FilterEndDate)
        {
            FilterEndDate = FilterStartDate;
        }
    }

    partial void OnFilterEndDateChanged(DateTime value)
    {
        if (FilterEndDate < FilterStartDate)
        {
            FilterStartDate = FilterEndDate;
        }
    }
}
