using SmokeTorg.Application.Interfaces;
using SmokeTorg.Domain.Entities;

namespace SmokeTorg.Application.Services;

public class ReportsService(ISaleRepository saleRepository, IProductRepository productRepository, IStockRepository stockRepository)
{
    public async Task<decimal> GetSalesTotalAsync(DateTime from, DateTime to)
    {
        var sales = await saleRepository.GetAllAsync();
        return sales.Where(s => s.Date.Date >= from.Date && s.Date.Date <= to.Date).Sum(s => s.Total);
    }

    public async Task<List<(string Product, decimal Qty)>> GetTopProductsAsync(int top = 5)
    {
        var sales = await saleRepository.GetAllAsync();
        return sales.SelectMany(s => s.Items)
            .GroupBy(i => i.ProductName)
            .Select(g => (g.Key, g.Sum(i => i.Quantity)))
            .OrderByDescending(x => x.Item2)
            .Take(top)
            .ToList();
    }

    public async Task<List<Product>> GetBelowMinStockAsync()
    {
        var products = await productRepository.GetAllAsync();
        var stock = await stockRepository.GetAllAsync();
        return products.Where(p => stock.FirstOrDefault(s => s.ProductId == p.Id)?.Quantity < p.MinStock).ToList();
    }
}
