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

    [ObservableProperty] private ObservableCollection<SavingDto> _savings = new();
    [ObservableProperty] private decimal _newAmount;
    [ObservableProperty] private string _newNotes = string.Empty;
    [ObservableProperty] private DateTime _filterStartDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime _filterEndDate = DateTime.Today;
    [ObservableProperty] private decimal _totalSavings;
    [ObservableProperty] private decimal _todaySavings;
    [ObservableProperty] private bool _isLoading;

    public SavingsViewModel(ISavingService savingService, INotificationService notificationService,
        ILogger<SavingsViewModel> logger, User currentUser)
    {
        _savingService = savingService;
        _notificationService = notificationService;
        _logger = logger;
        _currentUser = currentUser;

        SaveCommand = new RelayCommand(async () => await SaveAsync(), CanSave);
        DeleteCommand = new RelayCommand<SavingDto>(async dto => await DeleteAsync(dto));
        LoadCommand = new RelayCommand(async () => await LoadAsync());
        FilterCommand = new RelayCommand(async () => await LoadAsync());
    }

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand<SavingDto> DeleteCommand { get; }
    public IRelayCommand LoadCommand { get; }
    public IRelayCommand FilterCommand { get; }

    public async Task InitializeAsync() => await LoadAsync();

    private bool CanSave() => NewAmount > 0;

    private async Task SaveAsync()
    {
        try
        {
            if (!CanSave()) return;

            var saving = new Saving
            {
                Date = DateTime.Today,
                Amount = NewAmount,
                Notes = string.IsNullOrWhiteSpace(NewNotes) ? null : NewNotes,
                UserId = _currentUser.Id
            };

            await _savingService.CreateSavingAsync(saving);
            _notificationService.ShowSuccess($"UGX {NewAmount:N0} saved successfully");
            NewAmount = 0;
            NewNotes = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording saving");
            _notificationService.ShowError("Error recording saving");
        }
    }

    private async Task DeleteAsync(SavingDto? dto)
    {
        if (dto == null) return;
        if (!_notificationService.ShowConfirmation($"Delete saving of UGX {dto.Amount:N0}?")) return;

        try
        {
            await _savingService.DeleteSavingAsync(dto.Id);
            _notificationService.ShowSuccess("Saving deleted");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting saving");
            _notificationService.ShowError("Error deleting saving");
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            var items = await _savingService.GetSavingsByDateRangeAsync(FilterStartDate, FilterEndDate);
            Savings = new ObservableCollection<SavingDto>(items.Select(s => new SavingDto
            {
                Id = s.Id,
                Date = s.Date,
                Amount = s.Amount,
                Notes = s.Notes,
                UserName = s.User?.FullName ?? ""
            }));
            TotalSavings = Savings.Sum(s => s.Amount);
            TodaySavings = await _savingService.GetTodaySavingsAsync();
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
