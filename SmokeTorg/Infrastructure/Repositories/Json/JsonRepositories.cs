using SmokeTorg.Application.Interfaces;
using SmokeTorg.Domain.Entities;

namespace SmokeTorg.Infrastructure.Repositories.Json;

public class JsonProductRepository(IStorageProvider storage) : JsonRepositoryBase<Product>(storage, "products.json"), IProductRepository
{
    public async Task<List<Product>> SearchAsync(string text)
    {
        var all = await GetAllAsync();
        if (string.IsNullOrWhiteSpace(text)) return all;
        return all.Where(p => p.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
                           || p.Barcode.Contains(text, StringComparison.OrdinalIgnoreCase)
                           || p.Sku.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<Product?> GetByBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }

        var normalized = barcode.Trim();
        return (await GetAllAsync()).FirstOrDefault(p => p.Barcode.Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }
}

public class JsonCategoryRepository(IStorageProvider storage) : JsonRepositoryBase<Category>(storage, "categories.json"), ICategoryRepository;
public class JsonSupplierRepository(IStorageProvider storage) : JsonRepositoryBase<Supplier>(storage, "suppliers.json"), ISupplierRepository;
public class JsonCustomerRepository(IStorageProvider storage) : JsonRepositoryBase<Customer>(storage, "customers.json"), ICustomerRepository;
public class JsonSaleRepository(IStorageProvider storage) : JsonRepositoryBase<Sale>(storage, "sales.json"), ISaleRepository;
public class JsonPurchaseRepository(IStorageProvider storage) : JsonRepositoryBase<Purchase>(storage, "purchases.json"), IPurchaseRepository
{
    public async Task<string> GetNextNumberAsync(DateTime date)
    {
        var prefix = $"ПН-{date:yyyyMMdd}-";
        var all = await GetAllAsync();
        var lastNumber = all
            .Where(x => !string.IsNullOrWhiteSpace(x.Number) && x.Number.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Number)
            .OrderByDescending(x => x)
            .FirstOrDefault();

        var sequence = 1;
        if (!string.IsNullOrWhiteSpace(lastNumber))
        {
            var sequencePart = lastNumber[prefix.Length..];
            if (int.TryParse(sequencePart, out var currentSequence))
            {
                sequence = currentSequence + 1;
            }
        }

        return $"{prefix}{sequence:0000}";
    }
}
public class JsonSettingsRepository(IStorageProvider storage) : JsonRepositoryBase<AppSettings>(storage, "settings.json"), ISettingsRepository;

public class JsonUserRepository(IStorageProvider storage) : JsonRepositoryBase<User>(storage, "users.json"), IUserRepository
{
    public async Task<User?> GetByUsernameAsync(string username)
        => (await GetAllAsync()).FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

    public async Task<bool> UsernameExistsAsync(string username)
        => (await GetByUsernameAsync(username)) is not null;

    public async Task<List<User>> FilterAsync(string? search, Domain.Enums.UserRole? role)
    {
        var all = await GetAllAsync();
        return all
            .Where(u => role is null || u.Role == role)
            .Where(u => string.IsNullOrWhiteSpace(search) || u.Username.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}

public class JsonStockRepository(IStorageProvider storage) : IStockRepository
{
    private const string StockFile = "stock.json";
    private const string MovementsFile = "stock_movements.json";

    public Task<List<StockItem>> GetAllAsync() => storage.ReadCollectionAsync<StockItem>(StockFile);

    public async Task<StockItem?> GetByProductIdAsync(Guid productId)
        => (await GetAllAsync()).FirstOrDefault(s => s.ProductId == productId);

    public async Task UpsertAsync(StockItem item)
    {
        var all = await GetAllAsync();
        var index = all.FindIndex(s => s.ProductId == item.ProductId);
        if (index >= 0) all[index] = item; else all.Add(item);
        await storage.WriteCollectionAsync(StockFile, all);
    }

    public Task<List<StockMovement>> GetMovementsAsync() => storage.ReadCollectionAsync<StockMovement>(MovementsFile);

    public async Task AddMovementAsync(StockMovement movement)
    {
        var all = await GetMovementsAsync();
        all.Add(movement);
        await storage.WriteCollectionAsync(MovementsFile, all);
    }
}
