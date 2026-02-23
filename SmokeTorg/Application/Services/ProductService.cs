using SmokeTorg.Application.Interfaces;
using SmokeTorg.Domain.Entities;

namespace SmokeTorg.Application.Services;

public class ProductService(IProductRepository productRepository)
{
    public Task<List<Product>> GetAllAsync() => productRepository.GetAllAsync();
    public Task<List<Product>> SearchAsync(string text) => productRepository.SearchAsync(text);

    public async Task SaveAsync(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
            throw new InvalidOperationException("Название товара обязательно.");

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
}
