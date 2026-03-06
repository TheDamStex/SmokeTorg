using SmokeTorg.Application.Interfaces;
using SmokeTorg.Domain.Entities;
using SmokeTorg.Domain.Enums;

namespace SmokeTorg.Application.Services;

public class PurchaseService(
    IPurchaseRepository purchaseRepository,
    InventoryService inventoryService,
    AuthService authService,
    ISupplierRepository supplierRepository)
{
    public async Task<List<Purchase>> GetAllAsync()
    {
        var all = await purchaseRepository.GetAllAsync();
        return all
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Number)
            .ToList();
    }

    public Task<Purchase?> GetByIdAsync(Guid id) => purchaseRepository.GetByIdAsync(id);

    public Task<string> GetNextNumberAsync(DateTime date) => purchaseRepository.GetNextNumberAsync(date);

    public async Task<Purchase> CreateDraftAsync(Guid supplierId)
    {
        var session = authService.CurrentSession ?? throw new InvalidOperationException("Немає активної сесії користувача.");
        var supplier = await supplierRepository.GetByIdAsync(supplierId)
            ?? throw new InvalidOperationException("Постачальника не знайдено.");

        var now = DateTime.Now;
        var purchase = new Purchase
        {
            Id = Guid.NewGuid(),
            Number = await purchaseRepository.GetNextNumberAsync(now),
            CreatedAt = now,
            ReceiptDate = now,
            Date = now,
            SupplierId = supplier.Id,
            SupplierName = supplier.Name,
            CreatedByUserId = session.UserId,
            CreatedByLogin = session.FullName,
            AcceptedByUserId = session.UserId,
            AcceptedByLogin = session.FullName,
            Status = DocumentStatus.Draft
        };

        await purchaseRepository.AddAsync(purchase);
        return purchase;
    }

    public async Task SaveDraftAsync(Purchase purchase)
    {
        if (purchase.Status != DocumentStatus.Draft)
        {
            throw new InvalidOperationException("Можна зберігати лише чернетки.");
        }

        NormalizePurchase(purchase);

        var exists = await purchaseRepository.GetByIdAsync(purchase.Id);
        if (exists is null)
        {
            purchase.CreatedAt = purchase.CreatedAt == default ? DateTime.Now : purchase.CreatedAt;
            purchase.Number = string.IsNullOrWhiteSpace(purchase.Number)
                ? await purchaseRepository.GetNextNumberAsync(purchase.CreatedAt)
                : purchase.Number;
            await purchaseRepository.AddAsync(purchase);
            return;
        }

        purchase.CreatedAt = exists.CreatedAt == default ? DateTime.Now : exists.CreatedAt;
        purchase.Number = !string.IsNullOrWhiteSpace(exists.Number)
            ? exists.Number
            : string.IsNullOrWhiteSpace(purchase.Number)
                ? await purchaseRepository.GetNextNumberAsync(purchase.CreatedAt)
                : purchase.Number;
        await purchaseRepository.UpdateAsync(purchase);
    }

    public async Task PostAsync(Guid purchaseId)
    {
        var purchase = await purchaseRepository.GetByIdAsync(purchaseId)
            ?? throw new InvalidOperationException("Накладну не знайдено.");

        if (purchase.Status == DocumentStatus.Posted)
        {
            throw new InvalidOperationException("Накладна вже проведена.");
        }

        if (purchase.Status == DocumentStatus.Cancelled)
        {
            throw new InvalidOperationException("Скасовану накладну не можна провести.");
        }

        NormalizePurchase(purchase);
        foreach (var item in purchase.Items)
        {
            await inventoryService.ApplyMovementAsync(item.ProductId, item.Quantity, "In", $"Purchase:{purchase.Id}");
        }

        purchase.Status = DocumentStatus.Posted;
        await purchaseRepository.UpdateAsync(purchase);
    }

    public async Task CancelAsync(Guid purchaseId)
    {
        var purchase = await purchaseRepository.GetByIdAsync(purchaseId)
            ?? throw new InvalidOperationException("Накладну не знайдено.");

        if (purchase.Status == DocumentStatus.Posted)
        {
            throw new InvalidOperationException("Проведену накладну не можна скасувати.");
        }

        purchase.Status = DocumentStatus.Cancelled;
        await purchaseRepository.UpdateAsync(purchase);
    }

    private static void NormalizePurchase(Purchase purchase)
    {
        purchase.Total = purchase.Items.Sum(i => i.Price * i.Quantity);
        purchase.TotalAmount = purchase.Total;
        purchase.Date = purchase.ReceiptDate == default ? DateTime.Now : purchase.ReceiptDate;
    }
}
