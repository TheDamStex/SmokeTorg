using SmokeTorg.Application.Interfaces;

namespace SmokeTorg.Infrastructure.Storage;

public class SqlStorageProvider : IStorageProvider
{
    public Task<List<T>> ReadCollectionAsync<T>(string fileName)
        => throw new NotImplementedException("TODO: Реализовать SQL чтение.");

    public Task WriteCollectionAsync<T>(string fileName, List<T> items)
        => throw new NotImplementedException("TODO: Реализовать SQL запись.");
}
