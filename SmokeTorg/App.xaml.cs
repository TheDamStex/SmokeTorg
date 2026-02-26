using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SmokeTorg.Application.Interfaces;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Logging;
using SmokeTorg.Infrastructure.Repositories.Json;
using SmokeTorg.Infrastructure.Repositories.Sql;
using SmokeTorg.Infrastructure.Seed;
using SmokeTorg.Infrastructure.Services;
using SmokeTorg.Infrastructure.Storage;
using SmokeTorg.Presentation.Services;
using SmokeTorg.Presentation.ViewModels;
using SmokeTorg.Presentation.ViewModels.Dialogs;
using SmokeTorg.Presentation.ViewModels.Windows;
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

        var dbSettingsService = Services.GetRequiredService<IDbSettingsService>();
        var settings = await dbSettingsService.LoadAsync();

        if (!settings.IsConfigured)
        {
            var setupWindow = Services.GetRequiredService<SetupWizardWindow>();
            var setupResult = setupWindow.ShowDialog();
            if (setupResult != true)
            {
                Shutdown();
                return;
            }

            settings = await dbSettingsService.LoadAsync();
        }

        var loginWindow = Services.GetRequiredService<LoginWindow>();
        var loginResult = loginWindow.ShowDialog();
        if (loginResult != true)
        {
            Shutdown();
            return;
        }

        var seeder = Services.GetRequiredService<DataSeeder>();
        await seeder.EnsureSeedAsync();

        var window = Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
        base.OnStartup(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ILogger, SimpleLogger>();
        services.AddSingleton<IStorageProvider, JsonStorageProvider>();
        services.AddSingleton<IDbSettingsService, DbSettingsService>();
        services.AddSingleton<IDbInitializer, MySqlDbInitializer>();
        services.AddSingleton<IMySqlConnectionFactory, MySqlConnectionFactory>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IProductRepository, JsonProductRepository>();
        services.AddSingleton<ICategoryRepository, JsonCategoryRepository>();
        services.AddSingleton<IUserRepository, MySqlUserRepository>();
        services.AddSingleton<IPurchaseRepository, JsonPurchaseRepository>();
        services.AddSingleton<ISaleRepository, JsonSaleRepository>();
        services.AddSingleton<IStockRepository, JsonStockRepository>();
        services.AddSingleton<ISupplierRepository, JsonSupplierRepository>();
        services.AddSingleton<ICustomerRepository, JsonCustomerRepository>();
        services.AddSingleton<ISettingsRepository, JsonSettingsRepository>();

        services.AddSingleton<AuthService>();
        services.AddSingleton<IUserService, UserService>();
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
        services.AddSingleton<SetupWizardViewModel>();
        services.AddSingleton<UserManagementViewModel>();
        services.AddSingleton<DbSettingsViewModel>();

        services.AddSingleton<GoodsReceiptViewModel>();
        services.AddSingleton<PosWindowViewModel>();
        services.AddSingleton<DiscountCardsListViewModel>();
        services.AddSingleton<StockViewModel>();
        services.AddSingleton<ClientCardViewModel>();

        services.AddTransient<SupplierCreateWindow>();
        services.AddTransient<GoodsReceiptWindow>();
        services.AddTransient<PosWindow>();
        services.AddTransient<DiscountCardsListWindow>();
        services.AddTransient<StockWindow>();
        services.AddTransient<ClientCardWindow>();
        services.AddTransient<SetupWizardWindow>();
        services.AddTransient<LoginWindow>();
        services.AddTransient<UserManagementWindow>();
        services.AddTransient<DbSettingsWindow>();

        services.AddSingleton<MainWindow>();
    }
}
