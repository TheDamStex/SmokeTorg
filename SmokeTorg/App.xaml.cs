using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SmokeTorg.Application.Interfaces;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Logging;
using SmokeTorg.Infrastructure.Factories;
using SmokeTorg.Infrastructure.Repositories.Json;
using SmokeTorg.Infrastructure.Repositories.Sql;
using SmokeTorg.Infrastructure.Seed;
using SmokeTorg.Infrastructure.Storage;
using SmokeTorg.Presentation.Services;
using SmokeTorg.Presentation.ViewModels;
using SmokeTorg.Presentation.ViewModels.Dialogs;
using SmokeTorg.Presentation.Views.Windows;

namespace SmokeTorg;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        var collection = new ServiceCollection();
        ConfigureServices(collection);
        Services = collection.BuildServiceProvider();

        var seeder = Services.GetRequiredService<DataSeeder>();
        await seeder.EnsureSeedAsync();

        var window = Services.GetRequiredService<MainWindow>();
        window.Show();
        base.OnStartup(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ILogger, SimpleLogger>();
        services.AddSingleton<IStorageProviderFactory, StorageProviderFactory>();
        services.AddSingleton<IStorageProvider>(sp =>
            sp.GetRequiredService<IStorageProviderFactory>().CreateProvider("Json"));

        services.AddSingleton<IProductRepository, JsonProductRepository>();
        services.AddSingleton<ICategoryRepository, JsonCategoryRepository>();
        services.AddSingleton<IUserRepository, JsonUserRepository>();
        services.AddSingleton<IPurchaseRepository, JsonPurchaseRepository>();
        services.AddSingleton<ISaleRepository, JsonSaleRepository>();
        services.AddSingleton<IStockRepository, JsonStockRepository>();
        services.AddSingleton<ISupplierRepository, JsonSupplierRepository>();
        services.AddSingleton<ICustomerRepository, JsonCustomerRepository>();
        services.AddSingleton<ISettingsRepository, JsonSettingsRepository>();

        services.AddSingleton<AuthService>();
        services.AddSingleton<ProductService>();
        services.AddSingleton<SalesService>();
        services.AddSingleton<PurchaseService>();
        services.AddSingleton<InventoryService>();
        services.AddSingleton<ReportsService>();
        services.AddSingleton<SupplierService>();
        services.AddSingleton<CategoryService>();
        services.AddSingleton<DataSeeder>();

        services.AddSingleton<IDialogService, WpfDialogService>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<LoginViewModel>();
        services.AddSingleton<ProductsViewModel>();
        services.AddSingleton<PosViewModel>();
        services.AddSingleton<PurchasesViewModel>();
        services.AddSingleton<PlaceholderViewModel>();

        services.AddSingleton<GoodsReceiptViewModel>();
        services.AddSingleton<PosWindowViewModel>();
        services.AddSingleton<StockViewModel>();

        services.AddTransient<SupplierCreateWindow>();
        services.AddTransient<GoodsReceiptWindow>();
        services.AddTransient<PosWindow>();
        services.AddTransient<StockWindow>();

        services.AddSingleton<MainWindow>();

        _ = typeof(SqlProductRepository);
    }
}
