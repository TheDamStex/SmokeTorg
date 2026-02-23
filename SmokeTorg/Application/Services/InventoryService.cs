using SmokeTorg.Application.Interfaces;
using SmokeTorg.Domain.Entities;

namespace SmokeTorg.Application.Services;

public class InventoryService(IStockRepository stockRepository)
{
    public Task<List<StockItem>> GetStockAsync() => stockRepository.GetAllAsync();

    public async Task ApplyMovementAsync(Guid productId, decimal quantity, string type, string reason)
    {
        var current = await stockRepository.GetByProductIdAsync(productId) ?? new StockItem { ProductId = productId, Quantity = 0 };
        current.Quantity += type == "In" ? quantity : -quantity;
        current.Version = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await stockRepository.UpsertAsync(current);
        await stockRepository.AddMovementAsync(new StockMovement
        {
            ProductId = productId,
            Quantity = quantity,
            Type = type,
            Reason = reason,
            Version = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
    }
}
