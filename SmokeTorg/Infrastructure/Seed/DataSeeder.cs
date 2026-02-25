using SmokeTorg.Application.Interfaces;
using SmokeTorg.Application.Services;
using SmokeTorg.Domain.Entities;
using SmokeTorg.Domain.Enums;

namespace SmokeTorg.Infrastructure.Seed;

public class DataSeeder(
    IUserRepository userRepository,
    ICategoryRepository categoryRepository,
    IProductRepository productRepository,
    ISettingsRepository settingsRepository)
{
    public async Task EnsureSeedAsync()
    {
        if (!(await userRepository.GetAllAsync()).Any())
        {
            var (hash, salt) = AuthService.CreateHash("admin123");
            await userRepository.AddAsync(new User
            {
                Username = "admin",
                PasswordHash = hash,
                Salt = salt,
                Role = UserRole.Admin,
                IsActive = true
            });
        }

        var categories = await categoryRepository.GetAllAsync();
        if (!categories.Any())
        {
            var c1 = new Category { Name = "Напитки" };
            var c2 = new Category { Name = "Снеки" };
            await categoryRepository.AddAsync(c1);
            await categoryRepository.AddAsync(c2);

            await productRepository.AddAsync(new Product { Name = "Кола 0.5", Sku = "DR-001", Barcode = "4820000000001", CategoryId = c1.Id, PurchasePrice = 30, SalePrice = 55, MinStock = 10 });
            await productRepository.AddAsync(new Product { Name = "Чипсы 80г", Sku = "SN-001", Barcode = "4820000000002", CategoryId = c2.Id, PurchasePrice = 40, SalePrice = 75, MinStock = 15 });
        }

        if (!(await settingsRepository.GetAllAsync()).Any())
        {
            await settingsRepository.AddAsync(new AppSettings());
        }
    }
}
