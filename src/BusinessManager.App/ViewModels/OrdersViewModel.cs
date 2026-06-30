using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.DTOs;
using BusinessManager.Domain.Enums;
using BusinessManager.Domain.Interfaces;
using BusinessManager.App.Services;

namespace BusinessManager.App.ViewModels;

public partial class OrdersViewModel : ObservableObject
{
    private readonly IClientOrderService _orderService;
    private readonly INotificationService _notificationService;
    private readonly NotificationCenter _notificationCenter;
    private readonly ILogger<OrdersViewModel> _logger;
    private readonly User _currentUser;

    [ObservableProperty] private ObservableCollection<ClientOrderDto> _orders = new();
    [ObservableProperty] private string _newClientName = string.Empty;
    [ObservableProperty] private string _newPhone = string.Empty;
    [ObservableProperty] private string _newDescription = string.Empty;
    [ObservableProperty] private DateTime _newPickupDate = DateTime.Today.AddDays(1);
    [ObservableProperty] private string _newNotes = string.Empty;
    [ObservableProperty] private bool _showPendingOnly = true;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private int _overdueCount;
    [ObservableProperty] private int _dueTodayCount;

    public OrdersViewModel(IClientOrderService orderService, INotificationService notificationService,
        NotificationCenter notificationCenter, ILogger<OrdersViewModel> logger, User currentUser)
    {
        _orderService = orderService;
        _notificationService = notificationService;
        _notificationCenter = notificationCenter;
        _logger = logger;
        _currentUser = currentUser;

        SaveOrderCommand = new RelayCommand(async () => await SaveOrderAsync(), CanSave);
        MarkReadyCommand = new RelayCommand<ClientOrderDto>(async dto => await UpdateStatusAsync(dto, OrderStatus.Ready));
        MarkDeliveredCommand = new RelayCommand<ClientOrderDto>(async dto => await UpdateStatusAsync(dto, OrderStatus.Delivered));
        DeleteOrderCommand = new RelayCommand<ClientOrderDto>(async dto => await DeleteAsync(dto));
        LoadCommand = new RelayCommand(async () => await LoadAsync());
        ToggleFilterCommand = new RelayCommand(async () => await LoadAsync());
    }

    public IRelayCommand SaveOrderCommand { get; }
    public IRelayCommand<ClientOrderDto> MarkReadyCommand { get; }
    public IRelayCommand<ClientOrderDto> MarkDeliveredCommand { get; }
    public IRelayCommand<ClientOrderDto> DeleteOrderCommand { get; }
    public IRelayCommand LoadCommand { get; }
    public IRelayCommand ToggleFilterCommand { get; }

    public async Task InitializeAsync() => await LoadAsync();

    private bool CanSave() =>
        !string.IsNullOrWhiteSpace(NewClientName) && !string.IsNullOrWhiteSpace(NewDescription);

    partial void OnNewClientNameChanged(string value) =>
        ((RelayCommand)SaveOrderCommand).NotifyCanExecuteChanged();

    partial void OnNewDescriptionChanged(string value) =>
        ((RelayCommand)SaveOrderCommand).NotifyCanExecuteChanged();

    private async Task SaveOrderAsync()
    {
        try
        {
            if (!CanSave()) return;

            var order = new ClientOrder
            {
                ClientName = NewClientName.Trim(),
                Phone = string.IsNullOrWhiteSpace(NewPhone) ? null : NewPhone.Trim(),
                Description = NewDescription.Trim(),
                OrderDate = DateTime.Today,
                PickupDate = NewPickupDate,
                Notes = string.IsNullOrWhiteSpace(NewNotes) ? null : NewNotes.Trim(),
                UserId = _currentUser.Id,
                Status = OrderStatus.Pending
            };

            await _orderService.CreateOrderAsync(order);
            _notificationService.ShowSuccess($"Order for {NewClientName} registered");
            ClearForm();
            await LoadAsync();
            await _notificationCenter.RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            _notificationService.ShowError("Error creating order");
        }
    }

    private async Task UpdateStatusAsync(ClientOrderDto? dto, OrderStatus status)
    {
        if (dto == null) return;
        try
        {
            await _orderService.UpdateStatusAsync(dto.Id, status);
            var label = status == OrderStatus.Ready ? "marked Ready for pickup" : "marked Delivered";
            _notificationService.ShowSuccess($"Order for {dto.ClientName} {label}");
            await LoadAsync();
            await _notificationCenter.RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status");
            _notificationService.ShowError("Error updating order");
        }
    }

    private async Task DeleteAsync(ClientOrderDto? dto)
    {
        if (dto == null) return;
        if (!_notificationService.ShowConfirmation($"Delete order for {dto.ClientName}?")) return;
        try
        {
            await _orderService.DeleteOrderAsync(dto.Id);
            _notificationService.ShowSuccess("Order deleted");
            await LoadAsync();
            await _notificationCenter.RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order");
            _notificationService.ShowError("Error deleting order");
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            IEnumerable<ClientOrder> raw;
            if (ShowPendingOnly)
                raw = await _orderService.GetPendingOrdersAsync();
            else
                raw = await _orderService.GetAllOrdersAsync();

            Orders = new ObservableCollection<ClientOrderDto>(raw.Select(MapDto));
            OverdueCount = Orders.Count(o => o.IsOverdue);
            DueTodayCount = Orders.Count(o => o.IsDueToday);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading orders");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static ClientOrderDto MapDto(ClientOrder o) => new()
    {
        Id = o.Id,
        ClientName = o.ClientName,
        Phone = o.Phone,
        Description = o.Description,
        OrderDate = o.OrderDate,
        PickupDate = o.PickupDate,
        Status = o.Status,
        Notes = o.Notes,
        UserName = o.User?.FullName ?? "",
        IsOverdue = o.IsOverdue,
        IsDueToday = o.IsDueToday
    };

    private void ClearForm()
    {
        NewClientName = string.Empty;
        NewPhone = string.Empty;
        NewDescription = string.Empty;
        NewPickupDate = DateTime.Today.AddDays(1);
        NewNotes = string.Empty;
    }
}
