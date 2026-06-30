using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.DTOs;
using BusinessManager.Domain.Interfaces;

namespace BusinessManager.App.ViewModels;

public partial class SavingsViewModel : ObservableObject
{
    private readonly ISavingService _savingService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SavingsViewModel> _logger;
    private readonly User _currentUser;

    public static readonly string[] Recipients = { "OVALLA VALLENTINE", "BULE", "BANK" };

    [ObservableProperty] private ObservableCollection<SavingDto> _savings = new();
    [ObservableProperty] private decimal _newAmount;
    [ObservableProperty] private string _selectedRecipient = "BANK";
    [ObservableProperty] private string _newNotes = string.Empty;
    [ObservableProperty] private DateTime _filterStartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    [ObservableProperty] private DateTime _filterEndDate = DateTime.Today;
    [ObservableProperty] private string _filterRecipient = "ALL";
    [ObservableProperty] private decimal _totalSavings;
    [ObservableProperty] private decimal _todaySavings;
    [ObservableProperty] private decimal _ovallaTotal;
    [ObservableProperty] private decimal _buleTotal;
    [ObservableProperty] private decimal _bankTotal;
    [ObservableProperty] private bool _isLoading;

    public SavingsViewModel(ISavingService savingService, INotificationService notificationService,
        ILogger<SavingsViewModel> logger, User currentUser)
    {
        _savingService = savingService;
        _notificationService = notificationService;
        _logger = logger;
        _currentUser = currentUser;

        SaveCommand = new RelayCommand(async () => await SaveAsync(), CanSave);
        LoadCommand = new RelayCommand(async () => await LoadAsync());
        FilterCommand = new RelayCommand(async () => await LoadAsync());
        FilterByRecipientCommand = new RelayCommand<string>(async r =>
        {
            FilterRecipient = r ?? "ALL";
            await LoadAsync();
        });
    }

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand LoadCommand { get; }
    public IRelayCommand FilterCommand { get; }
    public IRelayCommand<string> FilterByRecipientCommand { get; }

    public async Task InitializeAsync() => await LoadAsync();

    private bool CanSave() => NewAmount > 0;

    partial void OnNewAmountChanged(decimal value) =>
        ((RelayCommand)SaveCommand).NotifyCanExecuteChanged();

    private async Task SaveAsync()
    {
        try
        {
            if (!CanSave()) return;

            var saving = new Saving
            {
                Date      = DateTime.Today,
                Amount    = NewAmount,
                Recipient = SelectedRecipient,
                Notes     = string.IsNullOrWhiteSpace(NewNotes) ? null : NewNotes,
                UserId    = _currentUser.Id
            };

            await _savingService.CreateSavingAsync(saving);
            _notificationService.ShowSuccess($"UGX {NewAmount:N0} saved to {SelectedRecipient}");
            NewAmount = 0;
            NewNotes  = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording saving");
            _notificationService.ShowError("Error recording saving");
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            IsLoading = true;

            var start = FilterStartDate.Date;
            var end   = FilterEndDate.Date.AddDays(1).AddTicks(-1);

            var all = (await _savingService.GetSavingsByDateRangeAsync(start, end)).ToList();

            OvallaTotal  = all.Where(s => s.Recipient == "OVALLA VALLENTINE").Sum(s => s.Amount);
            BuleTotal    = all.Where(s => s.Recipient == "BULE").Sum(s => s.Amount);
            BankTotal    = all.Where(s => s.Recipient == "BANK").Sum(s => s.Amount);
            TotalSavings = all.Sum(s => s.Amount);
            TodaySavings = await _savingService.GetTodaySavingsAsync();

            var filtered = FilterRecipient == "ALL"
                ? all
                : all.Where(s => s.Recipient == FilterRecipient).ToList();

            Savings = new ObservableCollection<SavingDto>(
                filtered.OrderByDescending(s => s.Date).Select(s => new SavingDto
                {
                    Id        = s.Id,
                    Date      = s.Date,
                    Amount    = s.Amount,
                    Recipient = s.Recipient,
                    Notes     = s.Notes,
                    UserName  = s.User?.FullName ?? ""
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading savings");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
