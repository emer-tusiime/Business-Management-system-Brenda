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
    private readonly DbAccessGate _dbGate;
    private readonly ILogger<SaleService> _logger;

    public SaleService(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        IInventoryService inventoryService,
        DbAccessGate dbGate,
        ILogger<SaleService> logger)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _inventoryService = inventoryService;
        _dbGate = dbGate;
        _logger = logger;
    }

    public Task<Sale> CreateSaleAsync(Sale sale, IEnumerable<SaleItem> saleItems) =>
        _dbGate.RunAsync(() => CreateSaleCoreAsync(sale, saleItems));

    private async Task<Sale> CreateSaleCoreAsync(Sale sale, IEnumerable<SaleItem> saleItems)
    {
        try
        {
            // Generate receipt number if not provided
            if (string.IsNullOrEmpty(sale.ReceiptNumber))
            {
                sale.ReceiptNumber = await GenerateReceiptNumberAsync();
            }

            // Calculate totals from line items (source of truth)
            foreach (var item in saleItems)
            {
                item.TotalPrice = item.Quantity * item.UnitPrice;
            }

            sale.Subtotal = saleItems.Sum(si => si.TotalPrice);
            sale.TotalAmount = sale.Subtotal + sale.TaxAmount - sale.DiscountAmount;
            sale.AmountPaid = sale.TotalAmount;
            sale.ChangeAmount = 0;
            if (sale.SaleDate == default)
                sale.SaleDate = DateTime.Now;

            sale.CreatedAt = DateTime.Now;

            foreach (var item in saleItems)
            {
                item.CreatedAt = DateTime.Now;
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

    public Task<IEnumerable<Sale>> GetSalesByDateRangeAsync(DateTime startDate, DateTime endDate) =>
        _dbGate.RunAsync(() => GetSalesByDateRangeCoreAsync(startDate, endDate));

    private async Task<IEnumerable<Sale>> GetSalesByDateRangeCoreAsync(DateTime startDate, DateTime endDate)
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

    public Task<bool> DeleteSaleAsync(int saleId) =>
        _dbGate.RunAsync(() => DeleteSaleCoreAsync(saleId));

    private async Task<bool> DeleteSaleCoreAsync(int saleId)
    {
        try
        {
            var sale = await _saleRepository.GetWithItemsAsync(saleId);
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
