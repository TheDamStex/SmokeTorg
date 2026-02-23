using SmokeTorg.Application.Interfaces;
using SmokeTorg.Domain.Entities;

namespace SmokeTorg.Application.Services;

public class SalesService(ISaleRepository saleRepository, InventoryService inventoryService)
{
    public async Task<Sale> FinalizeSaleAsync(Sale sale)
    {
        sale.Total = sale.Items.Sum(i => i.Price * i.Quantity * (1 - i.DiscountPercent / 100m));
        sale.TaxAmount = sale.Total * 0.2m;
        sale.Version = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        sale.Id = Guid.NewGuid();

        foreach (var item in sale.Items)
        {
            await inventoryService.ApplyMovementAsync(item.ProductId, item.Quantity, "Out", $"Sale:{sale.Id}");
        }

        await saleRepository.AddAsync(sale);
        return sale;
    }

    public Task<List<Sale>> GetAllAsync() => saleRepository.GetAllAsync();
}
