using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.Interfaces;

namespace BusinessManager.Application.Services;

public class SavingService : ISavingService
{
    private readonly ISavingRepository _repo;
    private readonly DbAccessGate _dbGate;
    private readonly ILogger<SavingService> _logger;

    public SavingService(ISavingRepository repo, DbAccessGate dbGate, ILogger<SavingService> logger)
    {
        _repo = repo;
        _dbGate = dbGate;
        _logger = logger;
    }

    public Task<Saving> CreateSavingAsync(Saving saving) =>
        _dbGate.RunAsync(() => _repo.AddAsync(saving));

    public Task<IEnumerable<Saving>> GetSavingsByDateRangeAsync(DateTime startDate, DateTime endDate) =>
        _dbGate.RunAsync(() => _repo.GetByDateRangeAsync(startDate, endDate));

    public Task<decimal> GetTodaySavingsAsync() =>
        _dbGate.RunAsync(() => _repo.GetTotalForDateAsync(DateTime.Today));

    public Task<decimal> GetTotalSavingsAsync(DateTime startDate, DateTime endDate) =>
        _dbGate.RunAsync(() => _repo.GetTotalForDateRangeAsync(startDate, endDate));

    public Task<bool> DeleteSavingAsync(int id) =>
        _dbGate.RunAsync(() => _repo.DeleteAsync(id));
}
