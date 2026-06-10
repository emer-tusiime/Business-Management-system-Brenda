using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.Interfaces;
using BusinessManager.Domain.Enums;

namespace BusinessManager.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryMovementRepository _inventoryMovementRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        IInventoryMovementRepository inventoryMovementRepository,
        IProductRepository productRepository,
        ILogger<InventoryService> logger)
    {
        _inventoryMovementRepository = inventoryMovementRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<InventoryMovement> AddStockAsync(int productId, int quantity, decimal unitCost, string reason, int userId)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                throw new ArgumentException($"Product with ID {productId} not found");
            }

            var stockBefore = product.CurrentStock;
            var stockAfter = stockBefore + quantity;

            // Update product stock
            product.CurrentStock = stockAfter;
            product.UpdatedAt = DateTime.UtcNow;
            await _productRepository.UpdateAsync(product);

            // Create inventory movement record
            var movement = new InventoryMovement
            {
                ProductId = productId,
                MovementType = InventoryMovementType.StockIn,
                Quantity = quantity,
                UnitCost = unitCost,
                TotalCost = quantity * unitCost,
                StockBefore = stockBefore,
                StockAfter = stockAfter,
                Reason = reason,
                UserId = userId,
                MovementDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            var createdMovement = await _inventoryMovementRepository.AddAsync(movement);
            
            _logger.LogInformation("Added {Quantity} units to product {ProductId}. New stock: {NewStock}", 
                quantity, productId, stockAfter);

            return createdMovement;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding stock to product {ProductId}", productId);
            throw;
        }
    }

    public async Task<InventoryMovement> RemoveStockAsync(int productId, int quantity, string reason, int userId)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                throw new ArgumentException($"Product with ID {productId} not found");
            }

            if (product.CurrentStock < quantity)
            {
                throw new InvalidOperationException($"Insufficient stock. Available: {product.CurrentStock}, Requested: {quantity}");
            }

            var stockBefore = product.CurrentStock;
            var stockAfter = stockBefore - quantity;

            // Update product stock
            product.CurrentStock = stockAfter;
            product.UpdatedAt = DateTime.UtcNow;
            await _productRepository.UpdateAsync(product);

            // Create inventory movement record
            var movement = new InventoryMovement
            {
                ProductId = productId,
                MovementType = InventoryMovementType.StockOut,
                Quantity = quantity,
                UnitCost = product.CostPrice,
                TotalCost = quantity * product.CostPrice,
                StockBefore = stockBefore,
                StockAfter = stockAfter,
                Reason = reason,
                UserId = userId,
                MovementDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            var createdMovement = await _inventoryMovementRepository.AddAsync(movement);
            
            _logger.LogInformation("Removed {Quantity} units from product {ProductId}. New stock: {NewStock}", 
                quantity, productId, stockAfter);

            return createdMovement;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing stock from product {ProductId}", productId);
            throw;
        }
    }

    public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
    {
        try
        {
            return await _productRepository.GetLowStockProductsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting low stock products");
            return Enumerable.Empty<Product>();
        }
    }

    public async Task<Product> UpdateProductAsync(Product product)
    {
        try
        {
            product.UpdatedAt = DateTime.UtcNow;
            var updatedProduct = await _productRepository.UpdateAsync(product);
            
            _logger.LogInformation("Product {ProductId} updated successfully", product.Id);
            return updatedProduct;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", product.Id);
            throw;
        }
    }

    public async Task<InventoryMovement> AdjustStockAsync(int productId, int newQuantity, string reason, int userId)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                throw new ArgumentException($"Product with ID {productId} not found");
            }

            var stockBefore = product.CurrentStock;
            var stockAfter = newQuantity;
            var quantity = stockAfter - stockBefore;

            // Update product stock
            product.CurrentStock = stockAfter;
            product.UpdatedAt = DateTime.UtcNow;
            await _productRepository.UpdateAsync(product);

            // Create inventory movement record
            var movement = new InventoryMovement
            {
                ProductId = productId,
                MovementType = InventoryMovementType.Adjustment,
                Quantity = Math.Abs(quantity),
                UnitCost = product.CostPrice,
                TotalCost = Math.Abs(quantity) * product.CostPrice,
                StockBefore = stockBefore,
                StockAfter = stockAfter,
                Reason = reason,
                UserId = userId,
                MovementDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            var createdMovement = await _inventoryMovementRepository.AddAsync(movement);
            
            _logger.LogInformation("Adjusted stock for product {ProductId} from {OldStock} to {NewStock}", 
                productId, stockBefore, stockAfter);

            return createdMovement;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting stock for product {ProductId}", productId);
            throw;
        }
    }

    public async Task<IEnumerable<InventoryMovement>> GetProductMovementsAsync(int productId)
    {
        try
        {
            return await _inventoryMovementRepository.GetByProductAsync(productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting movements for product {ProductId}", productId);
            return Enumerable.Empty<InventoryMovement>();
        }
    }
}
