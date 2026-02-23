using SmokeTorg.Application.Interfaces;
using SmokeTorg.Infrastructure.Storage;

namespace SmokeTorg.Infrastructure.Factories;

public class StorageProviderFactory : IStorageProviderFactory
{
    public IStorageProvider CreateProvider(string storageType)
        => storageType.Equals("Sql", StringComparison.OrdinalIgnoreCase)
            ? new SqlStorageProvider()
            : new JsonStorageProvider();
}
