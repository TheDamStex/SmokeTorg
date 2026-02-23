using SmokeTorg.Application.Interfaces;
using SmokeTorg.Domain.Entities;
using SmokeTorg.Domain.Enums;

namespace SmokeTorg.Application.Services;

public class PurchaseService(IPurchaseRepository purchaseRepository, InventoryService inventoryService)
{
    public Task<List<Purchase>> GetAllAsync() => purchaseRepository.GetAllAsync();

    public async Task SaveAsync(Purchase purchase)
    {
        purchase.Total = purchase.Items.Sum(i => i.Price * i.Quantity);
        if (purchase.Id == Guid.Empty)
        {
            purchase.Id = Guid.NewGuid();
            await purchaseRepository.AddAsync(purchase);
        }
        else
        {
            await purchaseRepository.UpdateAsync(purchase);
        }
    }

    public async Task PostAsync(Purchase purchase)
    {
        if (purchase.Status == DocumentStatus.Posted) return;

        foreach (var item in purchase.Items)
        {
            await inventoryService.ApplyMovementAsync(item.ProductId, item.Quantity, "In", $"Purchase:{purchase.Id}");
        }

        purchase.Status = DocumentStatus.Posted;
        await purchaseRepository.UpdateAsync(purchase);
    }
}
