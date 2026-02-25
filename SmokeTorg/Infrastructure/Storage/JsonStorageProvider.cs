using System.IO;
using System.Text.Json;
using SmokeTorg.Application.Interfaces;

namespace SmokeTorg.Infrastructure.Storage;

public class JsonStorageProvider : IStorageProvider
{
    private readonly string _dataDir;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public JsonStorageProvider()
    {
        _dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(_dataDir);
    }

    public async Task<List<T>> ReadCollectionAsync<T>(string fileName)
    {
        var path = Path.Combine(_dataDir, fileName);
        if (!File.Exists(path))
        {
            await File.WriteAllTextAsync(path, "[]");
            return [];
        }

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? [];
    }

    public async Task WriteCollectionAsync<T>(string fileName, List<T> items)
    {
        var path = Path.Combine(_dataDir, fileName);
        var json = JsonSerializer.Serialize(items, _jsonOptions);
        await File.WriteAllTextAsync(path, json);
    }
}
