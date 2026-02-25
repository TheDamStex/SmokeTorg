using System.Text.RegularExpressions;
using SmokeTorg.Application.Interfaces;
using SmokeTorg.Domain.Entities;

namespace SmokeTorg.Application.Services;

public class ProductService(IProductRepository productRepository)
{
    private static readonly Regex DigitsOnlyRegex = new("^\\d+$", RegexOptions.Compiled);

    public Task<List<Product>> GetAllAsync() => productRepository.GetAllAsync();
    public Task<List<Product>> SearchAsync(string text) => productRepository.SearchAsync(text);

    public async Task<Product?> FindByBarcode(string barcode)
    {
        var normalized = barcode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return await productRepository.GetByBarcode(normalized);
    }

    public async Task SaveAsync(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
            throw new InvalidOperationException("Назва товару обов'язкова.");

        product.Barcode = product.Barcode.Trim();

        if (product.IsActive && string.IsNullOrWhiteSpace(product.Barcode))
            throw new InvalidOperationException("Штрихкод обов'язковий для активного товару.");

        if (!string.IsNullOrWhiteSpace(product.Barcode) && !IsSupportedBarcode(product.Barcode))
            throw new InvalidOperationException("Некоректний формат штрихкоду.");

        if (!string.IsNullOrWhiteSpace(product.Barcode))
        {
            var existingByBarcode = await productRepository.GetByBarcode(product.Barcode);
            if (existingByBarcode is not null && existingByBarcode.Id != product.Id)
            {
                throw new InvalidOperationException("Товар з таким штрихкодом вже існує.");
            }
        }

        if (product.Id == Guid.Empty)
        {
            product.Id = Guid.NewGuid();
            await productRepository.AddAsync(product);
        }
        else
        {
            await productRepository.UpdateAsync(product);
        }
    }

    public Task DeleteAsync(Guid id) => productRepository.DeleteAsync(id);

    private static bool IsSupportedBarcode(string barcode)
    {
        if (!DigitsOnlyRegex.IsMatch(barcode))
        {
            return true;
        }

        return barcode.Length is 8 or 13;
    }
}
