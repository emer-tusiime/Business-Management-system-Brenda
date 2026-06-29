using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.Enums;
using BusinessManager.Domain.Interfaces;

namespace BusinessManager.Application.Services;

public class ClientOrderService : IClientOrderService
{
    private readonly IClientOrderRepository _repo;
    private readonly DbAccessGate _dbGate;
    private readonly ILogger<ClientOrderService> _logger;

    public ClientOrderService(IClientOrderRepository repo, DbAccessGate dbGate, ILogger<ClientOrderService> logger)
    {
        _repo = repo;
        _dbGate = dbGate;
        _logger = logger;
    }

    public Task<ClientOrder> CreateOrderAsync(ClientOrder order) =>
        _dbGate.RunAsync(() => _repo.AddAsync(order));

    public Task<ClientOrder?> GetOrderByIdAsync(int id) =>
        _dbGate.RunAsync(() => _repo.GetByIdAsync(id));

    public Task<IEnumerable<ClientOrder>> GetAllOrdersAsync() =>
        _dbGate.RunAsync(() => _repo.GetAllAsync());

    public Task<IEnumerable<ClientOrder>> GetPendingOrdersAsync() =>
        _dbGate.RunAsync(() => _repo.GetPendingAsync());

    public Task<IEnumerable<ClientOrder>> GetDueTodayAsync() =>
        _dbGate.RunAsync(() => _repo.GetDueTodayAsync());

    public Task<IEnumerable<ClientOrder>> GetOverdueAsync() =>
        _dbGate.RunAsync(() => _repo.GetOverdueAsync());

    public async Task<ClientOrder> UpdateStatusAsync(int orderId, OrderStatus status)
    {
        return await _dbGate.RunAsync(async () =>
        {
            var order = await _repo.GetByIdAsync(orderId)
                ?? throw new InvalidOperationException($"Order {orderId} not found");
            order.Status = status;
            order.UpdatedAt = DateTime.Now;
            return await _repo.UpdateAsync(order);
        });
    }

    public async Task<ClientOrder> UpdateOrderAsync(ClientOrder order)
    {
        order.UpdatedAt = DateTime.Now;
        return await _dbGate.RunAsync(() => _repo.UpdateAsync(order));
    }

    public Task<bool> DeleteOrderAsync(int id) =>
        _dbGate.RunAsync(() => _repo.DeleteAsync(id));
}
