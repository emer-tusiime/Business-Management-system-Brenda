using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.Interfaces;

namespace BusinessManager.Application.Services;

public class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<SaleService> _logger;

    public SaleService(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        IInventoryService inventoryService,
        ILogger<SaleService> logger)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async Task<Sale> CreateSaleAsync(Sale sale, IEnumerable<SaleItem> saleItems)
    {
        try
        {
            // Generate receipt number if not provided
            if (string.IsNullOrEmpty(sale.ReceiptNumber))
            {
                sale.ReceiptNumber = await GenerateReceiptNumberAsync();
            }

            // Calculate totals
            sale.Subtotal = saleItems.Sum(si => si.TotalPrice);
            sale.TotalAmount = sale.Subtotal + sale.TaxAmount - sale.DiscountAmount;
            sale.ChangeAmount = sale.AmountPaid - sale.TotalAmount;
            sale.CreatedAt = DateTime.UtcNow;

            foreach (var item in saleItems)
            {
                item.CreatedAt = DateTime.UtcNow;
                sale.SaleItems.Add(item);
            }

            var createdSale = await _saleRepository.AddAsync(sale);

            foreach (var item in saleItems)
            {
                if (item.ProductId.HasValue)
                {
                    var product = await _productRepository.GetByIdAsync(item.ProductId.Value);
                    if (product != null && product.CurrentStock >= item.Quantity)
                    {
                        await _inventoryService.RemoveStockAsync(
                            product.Id,
                            item.Quantity,
                            $"Sale - {sale.ReceiptNumber}",
                            sale.UserId);
                    }
                }
            }

            _logger.LogInformation("Sale created successfully with receipt number {ReceiptNumber}", sale.ReceiptNumber);
            return createdSale;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sale");
            throw;
        }
    }

    public async Task<Sale?> GetSaleByIdAsync(int id)
    {
        try
        {
            return await _saleRepository.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sale with ID {SaleId}", id);
            return null;
        }
    }

    public async Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            return await _saleRepository.GetByDateRangeAsync(startDate, endDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales between {StartDate} and {EndDate}", startDate, endDate);
            return Enumerable.Empty<Sale>();
        }
    }

    public async Task<string> GenerateReceiptNumberAsync()
    {
        try
        {
            var date = DateTime.Now;
            var random = new Random().Next(1000, 9999);
            return $"RCP{date:yyyyMMdd}{random}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating receipt number");
            throw;
        }
    }

    public async Task<bool> DeleteSaleAsync(int saleId)
    {
        try
        {
            var sale = await _saleRepository.GetByIdAsync(saleId);
            if (sale == null)
            {
                return false;
            }

            // Restore stock for products
            foreach (var item in sale.SaleItems)
            {
                if (item.ProductId.HasValue)
                {
                    await _inventoryService.AddStockAsync(
                        item.ProductId.Value,
                        item.Quantity,
                        item.UnitPrice,
                        $"Sale deleted - {sale.ReceiptNumber}",
                        sale.UserId);
                }
            }

            await _saleRepository.DeleteAsync(saleId);
            _logger.LogInformation("Sale {SaleId} deleted successfully", saleId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sale {SaleId}", saleId);
            return false;
        }
    }
}
