using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BusinessManager.Domain.DTOs;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.Interfaces;

namespace BusinessManager.Application.Services;

public class DebtorService : IDebtorService
{
    private readonly IDebtorRepository _debtorRepository;
    private readonly DbAccessGate _dbGate;
    private readonly ILogger<DebtorService> _logger;

    public DebtorService(IDebtorRepository debtorRepository, DbAccessGate dbGate, ILogger<DebtorService> logger)
    {
        _debtorRepository = debtorRepository;
        _dbGate = dbGate;
        _logger = logger;
    }

    public Task<Debtor> CreateDebtorAsync(Debtor debtor) =>
        _dbGate.RunAsync(() => CreateDebtorCoreAsync(debtor));

    private async Task<Debtor> CreateDebtorCoreAsync(Debtor debtor)
    {
        if (debtor.RecordDate == default)
            debtor.RecordDate = DateTime.Now;

        debtor.CreatedAt = DateTime.Now;
        debtor.IsSettled = false;
        debtor.AmountPaid = 0;

        if (debtor.TotalAmount <= 0)
            throw new InvalidOperationException("Credit amount must be greater than zero.");

        var created = await _debtorRepository.AddAsync(debtor);
        _logger.LogInformation("Debtor record created with ID {DebtorId}", created.Id);
        return created;
    }

    public Task<Debtor?> GetDebtorByIdAsync(int id) =>
        _dbGate.RunAsync(() => _debtorRepository.GetByIdAsync(id));

    public Task<IEnumerable<Debtor>> GetActiveDebtorsAsync() =>
        _dbGate.RunAsync(() => _debtorRepository.GetActiveAsync());

    public Task<IEnumerable<CustomerDebtSummaryDto>> GetCustomerDebtSummariesAsync() =>
        _dbGate.RunAsync(GetCustomerDebtSummariesCoreAsync);

    private async Task<IEnumerable<CustomerDebtSummaryDto>> GetCustomerDebtSummariesCoreAsync()
    {
        var debtors = await _debtorRepository.GetActiveAsync();

        return debtors
            .GroupBy(d => d.CustomerName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new CustomerDebtSummaryDto
            {
                CustomerName = g.Key,
                Phone = g.Select(d => d.Phone).FirstOrDefault(p => !string.IsNullOrWhiteSpace(p)) ?? string.Empty,
                TotalOwed = g.Sum(d => d.TotalAmount - d.AmountPaid),
                OpenRecords = g.Count()
            })
            .Where(x => x.TotalOwed > 0)
            .OrderByDescending(x => x.TotalOwed)
            .ToList();
    }

    public Task<decimal> GetTotalOutstandingAsync() =>
        _dbGate.RunAsync(() => _debtorRepository.GetTotalOutstandingAsync());

    public Task<Debtor> RecordPaymentAsync(int debtorId, decimal amount, string? notes, int userId) =>
        _dbGate.RunAsync(() => RecordPaymentCoreAsync(debtorId, amount, notes, userId));

    private async Task<Debtor> RecordPaymentCoreAsync(int debtorId, decimal amount, string? notes, int userId)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Payment amount must be greater than zero.");

        var debtor = await _debtorRepository.GetByIdAsync(debtorId)
            ?? throw new InvalidOperationException("Debtor record not found.");

        var balance = debtor.TotalAmount - debtor.AmountPaid;
        if (amount > balance)
            throw new InvalidOperationException("Payment amount cannot exceed the remaining balance.");

        debtor.AmountPaid += amount;
        debtor.IsSettled = debtor.AmountPaid >= debtor.TotalAmount;
        debtor.UpdatedAt = DateTime.Now;

        var payment = new DebtPayment
        {
            DebtorId = debtorId,
            Amount = amount,
            PaymentDate = DateTime.Now,
            Notes = notes,
            UserId = userId,
            CreatedAt = DateTime.Now
        };

        await _debtorRepository.UpdateAsync(debtor);
        await _debtorRepository.AddPaymentAsync(payment);

        _logger.LogInformation("Recorded payment of {Amount} for debtor {DebtorId}", amount, debtorId);
        return debtor;
    }

    public Task<Debtor> UpdateDebtorAsync(Debtor debtor) =>
        _dbGate.RunAsync(() => UpdateDebtorCoreAsync(debtor));

    private async Task<Debtor> UpdateDebtorCoreAsync(Debtor debtor)
    {
        debtor.IsSettled = debtor.AmountPaid >= debtor.TotalAmount;
        debtor.UpdatedAt = DateTime.Now;
        return await _debtorRepository.UpdateAsync(debtor);
    }

    public Task<bool> DeleteDebtorAsync(int debtorId) =>
        _dbGate.RunAsync(() => _debtorRepository.DeleteAsync(debtorId));
}
