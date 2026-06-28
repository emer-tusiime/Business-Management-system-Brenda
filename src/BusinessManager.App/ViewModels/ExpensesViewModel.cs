using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using BusinessManager.Domain.Interfaces;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.DTOs;

namespace BusinessManager.App.ViewModels;

public partial class ExpensesViewModel : ObservableObject
{
    private readonly IExpenseService _expenseService;
    private readonly IExpenseCategoryRepository _expenseCategoryRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ExpensesViewModel> _logger;
    private readonly User _currentUser;

    [ObservableProperty]
    private ObservableCollection<ExpenseDto> _expenses = new();

    [ObservableProperty]
    private ObservableCollection<ExpenseCategory> _expenseCategories = new();

    [ObservableProperty]
    private ExpenseDto? _selectedExpense;

    [ObservableProperty]
    private ExpenseCategory? _selectedCategory;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private decimal _amount;

    [ObservableProperty]
    private DateTime _expenseDate = DateTime.Today;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private DateTime _filterStartDate = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime _filterEndDate = DateTime.Today;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int? _filterCategoryId;

    public ExpensesViewModel(
        IExpenseService expenseService,
        IExpenseCategoryRepository expenseCategoryRepository,
        INotificationService notificationService,
        ILogger<ExpensesViewModel> logger,
        User currentUser)
    {
        _expenseService = expenseService;
        _expenseCategoryRepository = expenseCategoryRepository;
        _notificationService = notificationService;
        _logger = logger;
        _currentUser = currentUser;

        LoadExpensesCommand = new RelayCommand(async () => await LoadExpensesAsync());
        SaveExpenseCommand = new RelayCommand(async () => await SaveExpenseAsync(), CanSaveExpense);
        EditExpenseCommand = new RelayCommand<ExpenseDto>(EditExpense);
        DeleteExpenseCommand = new RelayCommand<ExpenseDto>(async (expense) => await DeleteExpenseAsync(expense));
        CancelEditCommand = new RelayCommand(CancelEdit);
        RefreshCommand = new RelayCommand(async () => await LoadExpensesAsync());
        SearchCommand = new RelayCommand(async () => await LoadExpensesAsync());
        ClearFiltersCommand = new RelayCommand(async () => await ClearFiltersAsync());
    }

    public IRelayCommand LoadExpensesCommand { get; }
    public IRelayCommand SaveExpenseCommand { get; }
    public IRelayCommand<ExpenseDto> EditExpenseCommand { get; }
    public IRelayCommand<ExpenseDto> DeleteExpenseCommand { get; }
    public IRelayCommand CancelEditCommand { get; }
    public IRelayCommand RefreshCommand { get; }
    public IRelayCommand SearchCommand { get; }
    public IRelayCommand ClearFiltersCommand { get; }

    public async Task InitializeAsync()
    {
        await LoadExpenseCategoriesAsync();
        await LoadExpensesAsync();
    }

    private async Task LoadExpensesAsync()
    {
        try
        {
            IsLoading = true;

            var endDate = FilterEndDate.Date.AddDays(1).AddTicks(-1);
            var expenses = await _expenseService.GetExpensesByDateRangeAsync(FilterStartDate.Date, endDate);
            var expensesDtos = expenses.Select(e => new ExpenseDto
            {
                Id = e.Id,
                Description = e.Description,
                Amount = e.Amount,
                ExpenseDate = e.ExpenseDate,
                CategoryName = e.ExpenseCategory.Name,
                UserName = e.User.FullName,
                Notes = e.Notes ?? ""
            });

            if (FilterCategoryId.HasValue)
            {
                expensesDtos = expensesDtos.Where(e => e.CategoryName ==
                    _expenseCategories.FirstOrDefault(c => c.Id == FilterCategoryId.Value)?.Name);
            }

            if (!string.IsNullOrEmpty(SearchText))
            {
                expensesDtos = expensesDtos.Where(e =>
                    e.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    e.CategoryName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    e.UserName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            Expenses = new ObservableCollection<ExpenseDto>(expensesDtos.OrderByDescending(e => e.ExpenseDate));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading expenses");
            _notificationService.ShowError("Error loading expenses");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadExpenseCategoriesAsync()
    {
        try
        {
            var categories = await _expenseCategoryRepository.GetActiveCategoriesAsync();
            ExpenseCategories = new ObservableCollection<ExpenseCategory>(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading expense categories");
        }
    }

    private void EditExpense(ExpenseDto? expense)
    {
        if (expense == null) return;

        SelectedExpense = expense;
        Description = expense.Description;
        Amount = expense.Amount;
        ExpenseDate = expense.ExpenseDate.Date;
        Notes = expense.Notes;
        SelectedCategory = ExpenseCategories.FirstOrDefault(c => c.Name == expense.CategoryName);
        IsEditing = true;
        SaveExpenseCommand.NotifyCanExecuteChanged();
    }

    private void CancelEdit()
    {
        ClearForm();
        IsEditing = false;
        SelectedExpense = null;
    }

    private void ClearForm()
    {
        Description = string.Empty;
        Amount = 0;
        ExpenseDate = DateTime.Today;
        Notes = string.Empty;
        SelectedCategory = null;
        SaveExpenseCommand.NotifyCanExecuteChanged();
    }

    private bool CanSaveExpense()
    {
        return !string.IsNullOrWhiteSpace(Description) &&
               Amount > 0 &&
               SelectedCategory != null;
    }

    private async Task SaveExpenseAsync()
    {
        try
        {
            if (!CanSaveExpense()) return;

            if (IsEditing && SelectedExpense != null)
            {
                var expense = new Expense
                {
                    Id = SelectedExpense.Id,
                    Description = Description,
                    Amount = Amount,
                    ExpenseDate = ExpenseDate.Date,
                    ExpenseCategoryId = SelectedCategory!.Id,
                    Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                    UserId = _currentUser.Id
                };

                await _expenseService.UpdateExpenseAsync(expense);
                _notificationService.ShowSuccess("Expense updated successfully");
            }
            else
            {
                var expense = new Expense
                {
                    Description = Description,
                    Amount = Amount,
                    ExpenseDate = ExpenseDate.Date,
                    ExpenseCategoryId = SelectedCategory!.Id,
                    Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                    UserId = _currentUser.Id
                };

                await _expenseService.CreateExpenseAsync(expense);
                _notificationService.ShowSuccess("Expense added successfully");
            }

            ClearForm();
            IsEditing = false;
            SelectedExpense = null;
            await LoadExpensesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving expense");
            _notificationService.ShowError("Error saving expense");
        }
    }

    private async Task DeleteExpenseAsync(ExpenseDto? expense)
    {
        if (expense == null) return;

        if (!_notificationService.ShowConfirmation($"Are you sure you want to delete this expense: {expense.Description}?"))
            return;

        try
        {
            await _expenseService.DeleteExpenseAsync(expense.Id);
            _notificationService.ShowSuccess("Expense deleted successfully");
            await LoadExpensesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense");
            _notificationService.ShowError("Error deleting expense");
        }
    }

    private async Task ClearFiltersAsync()
    {
        FilterStartDate = DateTime.Today.AddDays(-30);
        FilterEndDate = DateTime.Today;
        FilterCategoryId = null;
        SearchText = string.Empty;
        await LoadExpensesAsync();
    }

    partial void OnDescriptionChanged(string value) => SaveExpenseCommand.NotifyCanExecuteChanged();

    partial void OnAmountChanged(decimal value) => SaveExpenseCommand.NotifyCanExecuteChanged();

    partial void OnSelectedCategoryChanged(ExpenseCategory? value) => SaveExpenseCommand.NotifyCanExecuteChanged();

    partial void OnFilterStartDateChanged(DateTime value)
    {
        if (FilterStartDate > FilterEndDate)
            FilterEndDate = FilterStartDate;
    }

    partial void OnFilterEndDateChanged(DateTime value)
    {
        if (FilterEndDate < FilterStartDate)
            FilterStartDate = FilterEndDate;
    }

    partial void OnFilterCategoryIdChanged(int? value) => _ = LoadExpensesAsync();
}
