using SmokeTorg.Application.Interfaces;
using SmokeTorg.Domain.Entities;

namespace SmokeTorg.Application.Services;

public class SupplierService(ISupplierRepository supplierRepository)
{
    public Task<List<Supplier>> GetAllAsync() => supplierRepository.GetAllAsync();

    public Task<Supplier> SaveAsync(Supplier supplier) => SaveAsync(supplier, isCreateMode: false);

    public async Task<Supplier> SaveAsync(Supplier supplier, bool isCreateMode)
    {
        if (string.IsNullOrWhiteSpace(supplier.Name))
            throw new InvalidOperationException("Назва постачальника обов'язкова.");

        var existingSupplier = await supplierRepository.GetByIdAsync(supplier.Id);
        var isNewSupplier = existingSupplier is null;

        if (isNewSupplier)
        {
            await supplierRepository.AddAsync(supplier);
        }
        else
        {
            try
            {
                await supplierRepository.UpdateAsync(supplier);
            }
            catch (InvalidOperationException ex) when (isCreateMode && ex.Message.Contains("Запись не найдена", StringComparison.OrdinalIgnoreCase))
            {
                await supplierRepository.AddAsync(supplier);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Запись не найдена", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Постачальника не знайдено. Оновлення неможливе.", ex);
            }
        }

        return supplier;
    }
}
