using SmokeTorg.Domain.Entities;

namespace SmokeTorg.Application.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<List<T>> GetAllAsync();
    Task<T?> GetByIdAsync(Guid id);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
}

public interface IProductRepository : IRepository<Product>
{
    Task<List<Product>> SearchAsync(string text);
    Task<Product?> GetByBarcode(string barcode);
}

public interface ICategoryRepository : IRepository<Category>;
public interface ISupplierRepository : IRepository<Supplier>;
public interface ICustomerRepository : IRepository<Customer>;
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> UsernameExistsAsync(string username);
    Task<List<User>> FilterAsync(string? search, Domain.Enums.UserRole? role);
}

public interface ISaleRepository : IRepository<Sale>;
public interface IPurchaseRepository : IRepository<Purchase>
{
    Task<string> GetNextNumberAsync(DateTime date);
}
public interface ISettingsRepository : IRepository<AppSettings>;

public interface IStockRepository
{
    Task<List<StockItem>> GetAllAsync();
    Task<StockItem?> GetByProductIdAsync(Guid productId);
    Task UpsertAsync(StockItem item);
    Task<List<StockMovement>> GetMovementsAsync();
    Task AddMovementAsync(StockMovement movement);
}
