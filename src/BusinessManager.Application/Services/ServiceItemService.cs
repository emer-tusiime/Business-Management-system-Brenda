using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.Interfaces;

namespace BusinessManager.Application.Services;

public class ServiceItemService : IServiceItemService
{
    private readonly IServiceItemRepository _repo;
    private readonly DbAccessGate _dbGate;
    private readonly ILogger<ServiceItemService> _logger;

    public ServiceItemService(IServiceItemRepository repo, DbAccessGate dbGate, ILogger<ServiceItemService> logger)
    {
        _repo = repo;
        _dbGate = dbGate;
        _logger = logger;
    }

    public Task<ServiceItem> CreateServiceItemAsync(ServiceItem item) =>
        _dbGate.RunAsync(() => _repo.AddAsync(item));

    public Task<ServiceItem> UpdateServiceItemAsync(ServiceItem item) =>
        _dbGate.RunAsync(() => _repo.UpdateAsync(item));

    public Task<bool> DeleteServiceItemAsync(int id) =>
        _dbGate.RunAsync(() => _repo.DeleteAsync(id));
}
