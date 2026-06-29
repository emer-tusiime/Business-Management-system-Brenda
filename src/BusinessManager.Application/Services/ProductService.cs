using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.Interfaces;

namespace BusinessManager.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repo;
    private readonly DbAccessGate _dbGate;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository repo, DbAccessGate dbGate, ILogger<ProductService> logger)
    {
        _repo = repo;
        _dbGate = dbGate;
        _logger = logger;
    }

    public Task<Product> CreateProductAsync(Product product) =>
        _dbGate.RunAsync(() => _repo.AddAsync(product));

    public Task<Product> UpdateProductAsync(Product product) =>
        _dbGate.RunAsync(() => _repo.UpdateAsync(product));

    public Task<bool> DeleteProductAsync(int id) =>
        _dbGate.RunAsync(() => _repo.DeleteAsync(id));
}
