namespace SmokeTorg.Application.Interfaces;

public interface IStorageProvider
{
    Task<List<T>> ReadCollectionAsync<T>(string fileName);
    Task WriteCollectionAsync<T>(string fileName, List<T> items);
}

public interface IStorageProviderFactory
{
    IStorageProvider CreateProvider(string storageType);
}
