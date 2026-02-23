using SmokeTorg.Application.Interfaces;
using SmokeTorg.Domain.Entities;

namespace SmokeTorg.Infrastructure.Repositories.Json;

public abstract class JsonRepositoryBase<T>(IStorageProvider storageProvider, string fileName) : IRepository<T>
    where T : BaseEntity
{
    public async Task<List<T>> GetAllAsync() => await storageProvider.ReadCollectionAsync<T>(fileName);

    public async Task<T?> GetByIdAsync(Guid id) => (await GetAllAsync()).FirstOrDefault(x => x.Id == id);

    public async Task AddAsync(T entity)
    {
        var all = await GetAllAsync();
        all.Add(entity);
        await storageProvider.WriteCollectionAsync(fileName, all);
    }

    public async Task UpdateAsync(T entity)
    {
        var all = await GetAllAsync();
        var index = all.FindIndex(x => x.Id == entity.Id);
        if (index < 0) throw new InvalidOperationException("Запись не найдена.");
        if (all[index].Version != entity.Version)
            throw new InvalidOperationException("Конфликт версий. Запись изменена другим процессом.");

        entity.Version = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        all[index] = entity;
        await storageProvider.WriteCollectionAsync(fileName, all);
    }

    public async Task DeleteAsync(Guid id)
    {
        var all = await GetAllAsync();
        all.RemoveAll(x => x.Id == id);
        await storageProvider.WriteCollectionAsync(fileName, all);
    }
}
