using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using BusinessManager.App.Services;
using BusinessManager.Domain.Interfaces;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.DTOs;

namespace BusinessManager.App.ViewModels;

public partial class DebtorsViewModel : ObservableObject
{
    private readonly IDebtorService _debtorService;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<DebtorsViewModel> _logger;
    private readonly User _currentUser;

    [ObservableProperty]
    private ObservableCollection<DebtorDto> _debtors = new();

    [ObservableProperty]
    private ObservableCollection<CustomerDebtSummaryDto> _customerSummaries = new();

    [ObservableProperty]
    private DebtorDto? _selectedDebtor;

    [ObservableProperty]
    private string _customerName = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private decimal _totalAmount;

    [ObservableProperty]
    private decimal _paymentAmount;

    [ObservableProperty]
    private DateTime _recordDate = DateTime.Today;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _paymentNotes = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private decimal _totalOutstanding;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _showPaymentForm;

    public DebtorsViewModel(
        IDebtorService debtorService,
        INotificationService notificationService,
        IDialogService dialogService,
        ILogger<DebtorsViewModel> logger,
        User currentUser)
    {
        _debtorService = debtorService;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _logger = logger;
        _currentUser = currentUser;

        SaveDebtCommand = new RelayCommand(async () => await SaveDebtAsync(), CanSaveDebt);
        RecordPaymentCommand = new RelayCommand(async () => await RecordPaymentAsync(), CanRecordPayment);
        DeleteDebtCommand = new RelayCommand<DebtorDto>(async d => await DeleteDebtAsync(d));
        RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
        ClearFormCommand = new RelayCommand(ClearForm);
        SearchCommand = new RelayCommand(async () => await LoadDataAsync());
        ShowPaymentCommand = new RelayCommand<DebtorDto>(ShowPayment);
        CancelPaymentCommand = new RelayCommand(CancelPayment);
    }

    public IRelayCommand SaveDebtCommand { get; }
    public IRelayCommand RecordPaymentCommand { get; }
    public IRelayCommand<DebtorDto> DeleteDebtCommand { get; }
    public IRelayCommand RefreshCommand { get; }
    public IRelayCommand ClearFormCommand { get; }
    public IRelayCommand SearchCommand { get; }
    public IRelayCommand<DebtorDto> ShowPaymentCommand { get; }
    public IRelayCommand CancelPaymentCommand { get; }

    public async Task InitializeAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;

            var debtors = await _debtorService.GetActiveDebtorsAsync();
            var summaries = await _debtorService.GetCustomerDebtSummariesAsync();
            TotalOutstanding = await _debtorService.GetTotalOutstandingAsync();

            var debtorDtos = debtors.Select(MapToDto);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                debtorDtos = debtorDtos.Where(d =>
                    d.CustomerName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    d.Phone.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    d.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            Debtors = new ObservableCollection<DebtorDto>(debtorDtos.OrderByDescending(d => d.RecordDate));
            CustomerSummaries = new ObservableCollection<CustomerDebtSummaryDto>(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading debtors");
            _notificationService.ShowError("Error loading debtors");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static DebtorDto MapToDto(Debtor d) => new()
    {
        Id = d.Id,
        CustomerName = d.CustomerName,
        Phone = d.Phone ?? string.Empty,
        Description = d.Description,
        TotalAmount = d.TotalAmount,
        AmountPaid = d.AmountPaid,
        Balance = d.TotalAmount - d.AmountPaid,
        RecordDate = d.RecordDate,
        Notes = d.Notes ?? string.Empty,
        UserName = d.User?.FullName ?? string.Empty,
        IsSettled = d.IsSettled
    };

    private bool CanSaveDebt()
    {
        return !string.IsNullOrWhiteSpace(CustomerName) &&
               !string.IsNullOrWhiteSpace(Description) &&
               TotalAmount > 0;
    }

    private bool CanRecordPayment()
    {
        return SelectedDebtor != null && PaymentAmount > 0 && PaymentAmount <= SelectedDebtor.Balance;
    }

    private async Task SaveDebtAsync()
    {
        try
        {
            if (!CanSaveDebt()) return;

            var debtor = new Debtor
            {
                CustomerName = CustomerName.Trim(),
                Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                Description = Description.Trim(),
                TotalAmount = TotalAmount,
                AmountPaid = 0,
                RecordDate = RecordDate.Date,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                UserId = _currentUser.Id
            };

            await _debtorService.CreateDebtorAsync(debtor);
            _notificationService.ShowSuccess("Credit record added");
            ClearForm();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving debtor");
            _notificationService.ShowError("Error saving credit record");
        }
    }

    private void ShowPayment(DebtorDto? debtor)
    {
        if (debtor == null) return;

        SelectedDebtor = debtor;
        PaymentAmount = debtor.Balance;
        PaymentNotes = string.Empty;
        ShowPaymentForm = true;
        RecordPaymentCommand.NotifyCanExecuteChanged();
    }

    private void CancelPayment()
    {
        ShowPaymentForm = false;
        PaymentAmount = 0;
        PaymentNotes = string.Empty;
        SelectedDebtor = null;
    }

    private async Task RecordPaymentAsync()
    {
        if (SelectedDebtor == null || !CanRecordPayment()) return;

        try
        {
            await _debtorService.RecordPaymentAsync(
                SelectedDebtor.Id,
                PaymentAmount,
                string.IsNullOrWhiteSpace(PaymentNotes) ? null : PaymentNotes.Trim(),
                _currentUser.Id);

            _notificationService.ShowSuccess("Payment recorded");
            CancelPayment();
            ClearForm();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording payment");
            _notificationService.ShowError(ex.Message);
        }
    }

    private async Task DeleteDebtAsync(DebtorDto? debtor)
    {
        if (debtor == null) return;

        if (!_notificationService.ShowConfirmation($"Delete credit record for {debtor.CustomerName}?"))
            return;

        try
        {
            await _debtorService.DeleteDebtorAsync(debtor.Id);
            _notificationService.ShowSuccess("Credit record deleted");
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting debtor");
            _notificationService.ShowError("Error deleting credit record");
        }
    }

    private void ClearForm()
    {
        CustomerName = string.Empty;
        Phone = string.Empty;
        Description = string.Empty;
        TotalAmount = 0;
        RecordDate = DateTime.Today;
        Notes = string.Empty;
        SaveDebtCommand.NotifyCanExecuteChanged();
    }

    partial void OnCustomerNameChanged(string value) => SaveDebtCommand.NotifyCanExecuteChanged();
    partial void OnDescriptionChanged(string value) => SaveDebtCommand.NotifyCanExecuteChanged();
    partial void OnTotalAmountChanged(decimal value) => SaveDebtCommand.NotifyCanExecuteChanged();
    partial void OnPaymentAmountChanged(decimal value) => RecordPaymentCommand.NotifyCanExecuteChanged();
}
