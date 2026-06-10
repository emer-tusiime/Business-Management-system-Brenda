using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System; using System.Collections.Generic; using System.Threading.Tasks; using CommunityToolkit.Mvvm.ComponentModel; using CommunityToolkit.Mvvm.Input; using Microsoft.Extensions.Logging; using BusinessManager.Domain.Interfaces; using BusinessManager.Domain.Entities; using BusinessManager.Domain.DTOs; using BusinessManager.Domain.Enums; using BusinessManager.Application.Services;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.DTOs;

namespace BusinessManager.App.ViewModels;

public partial class InventoryViewModel : ObservableObject
{
    private readonly IInventoryService _inventoryService;
    private readonly IProductRepository _productRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<InventoryViewModel> _logger;
    private readonly User _currentUser;

    [ObservableProperty]
    private ObservableCollection<ProductDto> _products = new();

    [ObservableProperty]
    private ObservableCollection<InventoryMovementDto> _movements = new();

    [ObservableProperty]
    private ProductDto? _selectedProduct;

    [ObservableProperty]
    private InventoryMovementDto? _selectedMovement;

    [ObservableProperty]
    private string _movementDescription = string.Empty;

    [ObservableProperty]
    private int _movementQuantity = 1;

    [ObservableProperty]
    private decimal _movementUnitCost;

    [ObservableProperty]
    private InventoryMovementType _movementType = InventoryMovementType.StockIn;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _showLowStockOnly;

    [ObservableProperty]
    private string _selectedProductFilter = "All Products";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private DateTime _filterStartDate = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime _filterEndDate = DateTime.Today.AddDays(1).AddTicks(-1);

    public InventoryViewModel(
        IInventoryService inventoryService,
        IProductRepository productRepository,
        INotificationService notificationService,
        ILogger<InventoryViewModel> logger,
        User currentUser)
    {
        _inventoryService = inventoryService;
        _productRepository = productRepository;
        _notificationService = notificationService;
        _logger = logger;
        _currentUser = currentUser;

        LoadProductsCommand = new RelayCommand(async () => await LoadProductsAsync());
        LoadMovementsCommand = new RelayCommand(async () => await LoadMovementsAsync());
        AddStockCommand = new RelayCommand(async () => await AddStockAsync(), CanAddStock);
        RemoveStockCommand = new RelayCommand(async () => await RemoveStockAsync(), CanRemoveStock);
        AdjustStockCommand = new RelayCommand(async () => await AdjustStockAsync(), CanAdjustStock);
        RefreshCommand = new RelayCommand(async () => await RefreshAsync());
        SearchCommand = new RelayCommand(async () => await FilterProductsAsync());
    }

    public IRelayCommand LoadProductsCommand { get; }
    public IRelayCommand LoadMovementsCommand { get; }
    public IRelayCommand AddStockCommand { get; }
    public IRelayCommand RemoveStockCommand { get; }
    public IRelayCommand AdjustStockCommand { get; }
    public IRelayCommand RefreshCommand { get; }
    public IRelayCommand SearchCommand { get; }

    public async Task InitializeAsync()
    {
        await LoadProductsAsync();
        await LoadMovementsAsync();
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            IsLoading = true;
            
            var products = await _productRepository.GetAllAsync();
            var productsDtos = products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description ?? "",
                CurrentStock = p.CurrentStock,
                ReorderLevel = p.ReorderLevel,
                SellingPrice = p.SellingPrice,
                CostPrice = p.CostPrice,
                IsActive = p.IsActive
            });

            // Apply filters
            if (!string.IsNullOrEmpty(SearchText))
            {
                productsDtos = productsDtos.Where(p => 
                    p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            if (ShowLowStockOnly)
            {
                productsDtos = productsDtos.Where(p => p.IsLowStock);
            }

            if (SelectedProductFilter != "All Products")
            {
                productsDtos = productsDtos.Where(p => p.Name == SelectedProductFilter);
            }

            Products = new ObservableCollection<ProductDto>(productsDtos.OrderBy(p => p.Name));
            
            // Update product filter options
            UpdateProductFilterOptions(productsDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading products");
            _notificationService.ShowError("Error loading products");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadMovementsAsync()
    {
        try
        {
            if (SelectedProduct == null) return;

            var movements = await _inventoryService.GetProductMovementsAsync(SelectedProduct.Id);
            var movementsDtos = movements.Select(m => new InventoryMovementDto
            {
                Id = m.Id,
                MovementDate = m.MovementDate,
                MovementType = m.MovementType,
                Quantity = m.Quantity,
                UnitCost = m.UnitCost,
                TotalCost = m.TotalCost,
                Description = m.Reason ?? "",
                UserName = m.User.FullName,
                StockBefore = m.StockBefore,
                StockAfter = m.StockAfter
            });

            Movements = new ObservableCollection<InventoryMovementDto>(
                movementsDtos.OrderByDescending(m => m.MovementDate));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading movements");
            _notificationService.ShowError("Error loading movements");
        }
    }

    private void UpdateProductFilterOptions(IEnumerable<ProductDto> products)
    {
        var productNames = products.Select(p => p.Name).Distinct().OrderBy(name => name).ToList();
        // Update filter options (you might want to bind this to a ComboBox in the UI)
    }

    private bool CanAddStock()
    {
        return SelectedProduct != null && MovementQuantity > 0 && MovementUnitCost >= 0;
    }

    private bool CanRemoveStock()
    {
        return SelectedProduct != null && MovementQuantity > 0 && 
               SelectedProduct.CurrentStock >= MovementQuantity;
    }

    private bool CanAdjustStock()
    {
        return SelectedProduct != null && MovementQuantity != 0;
    }

    private async Task AddStockAsync()
    {
        try
        {
            if (!CanAddStock()) return;

            await _inventoryService.AddStockAsync(
                SelectedProduct!.Id,
                MovementQuantity,
                MovementUnitCost,
                MovementDescription,
                _currentUser.Id
            );

            _notificationService.ShowSuccess("Stock added successfully");
            ClearMovementForm();
            await LoadProductsAsync();
            await LoadMovementsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding stock");
            _notificationService.ShowError("Error adding stock");
        }
    }

    private async Task RemoveStockAsync()
    {
        try
        {
            if (!CanRemoveStock()) return;

            if (!_notificationService.ShowConfirmation(
                $"Are you sure you want to remove {MovementQuantity} units of {SelectedProduct?.Name}?"))
                return;

            await _inventoryService.RemoveStockAsync(
                SelectedProduct!.Id,
                MovementQuantity,
                MovementDescription,
                _currentUser.Id
            );

            _notificationService.ShowSuccess("Stock removed successfully");
            ClearMovementForm();
            await LoadProductsAsync();
            await LoadMovementsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing stock");
            _notificationService.ShowError("Error removing stock");
        }
    }

    private async Task AdjustStockAsync()
    {
        try
        {
            if (!CanAdjustStock()) return;

            if (!_notificationService.ShowConfirmation(
                $"Are you sure you want to adjust stock for {SelectedProduct?.Name} to {MovementQuantity} units?"))
                return;

            await _inventoryService.AdjustStockAsync(
                SelectedProduct!.Id,
                MovementQuantity,
                MovementDescription,
                _currentUser.Id
            );

            _notificationService.ShowSuccess("Stock adjusted successfully");
            ClearMovementForm();
            await LoadProductsAsync();
            await LoadMovementsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting stock");
            _notificationService.ShowError("Error adjusting stock");
        }
    }

    private void ClearMovementForm()
    {
        MovementDescription = string.Empty;
        MovementQuantity = 1;
        MovementUnitCost = 0;
        MovementType = InventoryMovementType.StockIn;
    }

    private async Task FilterProductsAsync()
    {
        await LoadProductsAsync();
    }

    private async Task RefreshAsync()
    {
        await LoadProductsAsync();
        if (SelectedProduct != null)
        {
            await LoadMovementsAsync();
        }
    }

    partial void OnSelectedProductChanged(ProductDto? value)
    {
        if (value != null)
        {
            MovementUnitCost = value.CostPrice;
            _ = LoadMovementsAsync();
        }
    }

    partial void OnMovementTypeChanged(InventoryMovementType value)
    {
        // Adjust UI based on movement type
        MovementQuantity = 1;
    }

    partial void OnShowLowStockOnlyChanged(bool value)
    {
        _ = FilterProductsAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        // Debounce search
        Task.Delay(300).ContinueWith(_ => FilterProductsAsync());
    }
}
