using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.Interfaces;

namespace BusinessManager.Application.Services;

public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(IExpenseRepository expenseRepository, ILogger<ExpenseService> logger)
    {
        _expenseRepository = expenseRepository;
        _logger = logger;
    }

    public async Task<Expense> CreateExpenseAsync(Expense expense)
    {
        try
        {
            if (expense.ExpenseDate == default)
                expense.ExpenseDate = DateTime.Now;

            expense.CreatedAt = DateTime.Now;
            var createdExpense = await _expenseRepository.AddAsync(expense);
            
            _logger.LogInformation("Expense created successfully with ID {ExpenseId}", createdExpense.Id);
            return createdExpense;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            throw;
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(int id)
    {
        try
        {
            return await _expenseRepository.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense with ID {ExpenseId}", id);
            return null;
        }
    }

    public async Task<IEnumerable<Expense>> GetExpensesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            return await _expenseRepository.GetByDateRangeAsync(startDate, endDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses between {StartDate} and {EndDate}", startDate, endDate);
            return Enumerable.Empty<Expense>();
        }
    }

    public async Task<Expense> UpdateExpenseAsync(Expense expense)
    {
        try
        {
            expense.UpdatedAt = DateTime.Now;
            var updatedExpense = await _expenseRepository.UpdateAsync(expense);
            
            _logger.LogInformation("Expense {ExpenseId} updated successfully", expense.Id);
            return updatedExpense;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense {ExpenseId}", expense.Id);
            throw;
        }
    }

    public async Task<bool> DeleteExpenseAsync(int expenseId)
    {
        try
        {
            var expense = await _expenseRepository.GetByIdAsync(expenseId);
            if (expense == null)
            {
                return false;
            }

            await _expenseRepository.DeleteAsync(expenseId);
            _logger.LogInformation("Expense {ExpenseId} deleted successfully", expenseId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense {ExpenseId}", expenseId);
            return false;
        }
    }
}
