using SmokeTorg.Application.Interfaces;
using SmokeTorg.Domain.Entities;

namespace SmokeTorg.Infrastructure.Repositories.Sql;

public class SqlProductRepository : IProductRepository
{
    public Task AddAsync(Product entity) => throw new NotImplementedException("TODO: SQL repository");
    public Task DeleteAsync(Guid id) => throw new NotImplementedException("TODO: SQL repository");
    public Task<List<Product>> GetAllAsync() => throw new NotImplementedException("TODO: SQL repository");
    public Task<Product?> GetByIdAsync(Guid id) => throw new NotImplementedException("TODO: SQL repository");
    public Task<Product?> GetByBarcode(string barcode) => throw new NotImplementedException("TODO: SQL repository");
    public Task<List<Product>> SearchAsync(string text) => throw new NotImplementedException("TODO: SQL repository");
    public Task UpdateAsync(Product entity) => throw new NotImplementedException("TODO: SQL repository");
}
