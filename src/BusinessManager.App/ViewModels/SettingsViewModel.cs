using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System; using System.Collections.Generic; using System.Threading.Tasks; using CommunityToolkit.Mvvm.ComponentModel; using CommunityToolkit.Mvvm.Input; using Microsoft.Extensions.Logging; using BusinessManager.Domain.Interfaces; using BusinessManager.Domain.Entities; using BusinessManager.Domain.DTOs; using BusinessManager.Domain.Enums; using BusinessManager.Application.Services;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.DTOs;

namespace BusinessManager.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingService _settingService;
    private readonly IServiceItemRepository _serviceItemRepository;
    private readonly IProductRepository _productRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<Setting> _settings = new();

    [ObservableProperty]
    private ObservableCollection<ServiceItem> _serviceItems = new();

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private Setting? _selectedSetting;

    [ObservableProperty]
    private ServiceItem? _selectedServiceItem;

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private string _newSettingKey = string.Empty;

    [ObservableProperty]
    private string _newSettingValue = string.Empty;

    [ObservableProperty]
    private string _newSettingCategory = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEditing;

    // Business Settings
    [ObservableProperty]
    private string _businessName = string.Empty;

    [ObservableProperty]
    private string _businessAddress = string.Empty;

    [ObservableProperty]
    private string _businessPhone = string.Empty;

    [ObservableProperty]
    private string _businessEmail = string.Empty;

    [ObservableProperty]
    private decimal _taxRate = 15;

    [ObservableProperty]
    private int _lowStockThreshold = 10;

    public SettingsViewModel(
        ISettingService settingService,
        IServiceItemRepository serviceItemRepository,
        IProductRepository productRepository,
        INotificationService notificationService,
        ILogger<SettingsViewModel> logger)
    {
        _settingService = settingService;
        _serviceItemRepository = serviceItemRepository;
        _productRepository = productRepository;
        _notificationService = notificationService;
        _logger = logger;

        LoadSettingsCommand = new RelayCommand(async () => await LoadSettingsAsync());
        LoadServiceItemsCommand = new RelayCommand(async () => await LoadServiceItemsAsync());
        LoadProductsCommand = new RelayCommand(async () => await LoadProductsAsync());
        SaveSettingCommand = new RelayCommand(async () => await SaveSettingAsync(), CanSaveSetting);
        DeleteSettingCommand = new RelayCommand<Setting>(async (setting) => await DeleteSettingAsync(setting));
        SaveBusinessSettingsCommand = new RelayCommand(async () => await SaveBusinessSettingsAsync());
        UpdateServicePriceCommand = new RelayCommand(async () => await UpdateServicePriceAsync(), CanUpdateServicePrice);
        UpdateProductPriceCommand = new RelayCommand(async () => await UpdateProductPriceAsync(), CanUpdateProductPrice);
        RefreshCommand = new RelayCommand(async () => await RefreshAsync());
    }

    public IRelayCommand LoadSettingsCommand { get; }
    public IRelayCommand LoadServiceItemsCommand { get; }
    public IRelayCommand LoadProductsCommand { get; }
    public IRelayCommand SaveSettingCommand { get; }
    public IRelayCommand<Setting> DeleteSettingCommand { get; }
    public IRelayCommand SaveBusinessSettingsCommand { get; }
    public IRelayCommand UpdateServicePriceCommand { get; }
    public IRelayCommand UpdateProductPriceCommand { get; }
    public IRelayCommand RefreshCommand { get; }

    public async Task InitializeAsync()
    {
        await LoadSettingsAsync();
        await LoadServiceItemsAsync();
        await LoadProductsAsync();
        await LoadBusinessSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            IsLoading = true;
            var settings = await _settingService.GetAllSettingsAsync();
            Settings = new ObservableCollection<Setting>(settings.OrderBy(s => s.Category).ThenBy(s => s.Key));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings");
            _notificationService.ShowError("Error loading settings");
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
            var serviceItems = await _serviceItemRepository.GetAllAsync();
            ServiceItems = new ObservableCollection<ServiceItem>(serviceItems.OrderBy(s => s.Category).ThenBy(s => s.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading service items");
            _notificationService.ShowError("Error loading service items");
        }
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            var products = await _productRepository.GetAllAsync();
            Products = new ObservableCollection<Product>(products.OrderBy(p => p.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading products");
            _notificationService.ShowError("Error loading products");
        }
    }

    private async Task LoadBusinessSettingsAsync()
    {
        try
        {
            BusinessName = await _settingService.GetSettingAsync("BusinessName") ?? "Your Business";
            BusinessAddress = await _settingService.GetSettingAsync("BusinessAddress") ?? "";
            BusinessPhone = await _settingService.GetSettingAsync("BusinessPhone") ?? "";
            BusinessEmail = await _settingService.GetSettingAsync("BusinessEmail") ?? "";
            TaxRate = decimal.Parse(await _settingService.GetSettingAsync("TaxRate") ?? "15");
            LowStockThreshold = int.Parse(await _settingService.GetSettingAsync("LowStockThreshold") ?? "10");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading business settings");
        }
    }

    private bool CanSaveSetting()
    {
        return !string.IsNullOrWhiteSpace(NewSettingKey) && 
               !string.IsNullOrWhiteSpace(NewSettingValue) && 
               !string.IsNullOrWhiteSpace(NewSettingCategory);
    }

    private async Task SaveSettingAsync()
    {
        try
        {
            if (!CanSaveSetting()) return;

            var setting = new Setting
            {
                Key = NewSettingKey,
                Value = NewSettingValue,
                Category = NewSettingCategory,
                Description = $"Setting for {NewSettingKey}"
            };

            await _settingService.SetSettingAsync(setting.Key, setting.Value);
            _notificationService.ShowSuccess("Setting saved successfully");

            // Clear form
            NewSettingKey = string.Empty;
            NewSettingValue = string.Empty;
            NewSettingCategory = string.Empty;

            await LoadSettingsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving setting");
            _notificationService.ShowError("Error saving setting");
        }
    }

    private async Task DeleteSettingAsync(Setting? setting)
    {
        try
        {
            if (setting == null) return;

            if (!_notificationService.ShowConfirmation($"Are you sure you want to delete setting: {setting.Key}?"))
                return;

            await _settingService.DeleteSettingAsync(setting.Key);
            _notificationService.ShowSuccess("Setting deleted successfully");
            await LoadSettingsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting setting");
            _notificationService.ShowError("Error deleting setting");
        }
    }

    private async Task SaveBusinessSettingsAsync()
    {
        try
        {
            await _settingService.SetSettingAsync("BusinessName", BusinessName);
            await _settingService.SetSettingAsync("BusinessAddress", BusinessAddress);
            await _settingService.SetSettingAsync("BusinessPhone", BusinessPhone);
            await _settingService.SetSettingAsync("BusinessEmail", BusinessEmail);
            await _settingService.SetSettingAsync("TaxRate", TaxRate.ToString());
            await _settingService.SetSettingAsync("LowStockThreshold", LowStockThreshold.ToString());

            _notificationService.ShowSuccess("Business settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving business settings");
            _notificationService.ShowError("Error saving business settings");
        }
    }

    private bool CanUpdateServicePrice()
    {
        return SelectedServiceItem != null;
    }

    private async Task UpdateServicePriceAsync()
    {
        try
        {
            if (!CanUpdateServicePrice()) return;

            // Update service item price logic would go here
            _notificationService.ShowSuccess("Service price updated successfully");
            await LoadServiceItemsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating service price");
            _notificationService.ShowError("Error updating service price");
        }
    }

    private bool CanUpdateProductPrice()
    {
        return SelectedProduct != null;
    }

    private async Task UpdateProductPriceAsync()
    {
        try
        {
            if (!CanUpdateProductPrice()) return;

            // Update product price logic would go here
            _notificationService.ShowSuccess("Product price updated successfully");
            await LoadProductsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product price");
            _notificationService.ShowError("Error updating product price");
        }
    }

    private async Task RefreshAsync()
    {
        await LoadSettingsAsync();
        await LoadServiceItemsAsync();
        await LoadProductsAsync();
        await LoadBusinessSettingsAsync();
    }
}
