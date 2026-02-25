using SmokeTorg.Application.Interfaces;
using SmokeTorg.Domain.Entities;

namespace SmokeTorg.Application.Services;

public class SupplierService(ISupplierRepository supplierRepository)
{
    public Task<List<Supplier>> GetAllAsync() => supplierRepository.GetAllAsync();

    public async Task<Supplier> SaveAsync(Supplier supplier)
    {
        if (string.IsNullOrWhiteSpace(supplier.Name))
            throw new InvalidOperationException("Назва постачальника обов'язкова.");

        if (supplier.Id == Guid.Empty)
        {
            supplier.Id = Guid.NewGuid();
            await supplierRepository.AddAsync(supplier);
        }
        else
        {
            await supplierRepository.UpdateAsync(supplier);
        }

        return supplier;
    }
}
