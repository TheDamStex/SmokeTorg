using SmokeTorg.Application.Interfaces;
using SmokeTorg.Domain.Entities;

namespace SmokeTorg.Application.Services;

public class CategoryService(ICategoryRepository categoryRepository)
{
    public Task<List<Category>> GetAllAsync() => categoryRepository.GetAllAsync();
}
