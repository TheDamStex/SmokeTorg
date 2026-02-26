using System.Collections.ObjectModel;
using SmokeTorg.Application.Interfaces;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Enums;

namespace SmokeTorg.Presentation.ViewModels.Windows;

public class SetupWizardViewModel : ViewModelBase
{
    private readonly IDbSettingsService _dbSettingsService;
    private readonly IDbInitializer _dbInitializer;
    private readonly IUserService _userService;
    private int _stepIndex;
    private bool _connectionTestPassed;
    private bool _schemaInitialized;
    private string _status = "Очікування";

    public SetupWizardViewModel(IDbSettingsService dbSettingsService, IDbInitializer dbInitializer, IUserService userService)
    {
        _dbSettingsService = dbSettingsService;
        _dbInitializer = dbInitializer;
        _userService = userService;
        CheckConnectionCommand = new AsyncRelayCommand(async _ => await CheckConnectionAsync());
        InitializeSchemaCommand = new AsyncRelayCommand(async _ => await InitializeSchemaAsync(), _ => _connectionTestPassed);
        CreateAdminCommand = new AsyncRelayCommand(async _ => await CreateAdminAsync(), _ => _schemaInitialized);
        NextCommand = new RelayCommand(_ => StepIndex++, _ => StepIndex < 3);
        BackCommand = new RelayCommand(_ => StepIndex--, _ => StepIndex > 0);
        FinishCommand = new AsyncRelayCommand(async _ => await FinishAsync(), _ => _adminCreated);
    }

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 3306;
    public string Database { get; set; } = "smoketorg";
    public string User { get; set; } = "root";
    public string Password { get; set; } = string.Empty;
    public bool UseSsl { get; set; }
    public bool AllowPublicKeyRetrieval { get; set; } = true;

    public string AdminLogin { get; set; } = "admin";
    public string AdminPassword { get; set; } = string.Empty;
    public string AdminConfirmPassword { get; set; } = string.Empty;
    public string AdminFullName { get; set; } = string.Empty;

    private bool _adminCreated;
    public bool IsCompleted { get; private set; }
    public int StepIndex { get => _stepIndex; set => SetProperty(ref _stepIndex, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    public ObservableCollection<string> Logs { get; } = [];

    public AsyncRelayCommand CheckConnectionCommand { get; }
    public AsyncRelayCommand InitializeSchemaCommand { get; }
    public AsyncRelayCommand CreateAdminCommand { get; }
    public RelayCommand NextCommand { get; }
    public RelayCommand BackCommand { get; }
    public AsyncRelayCommand FinishCommand { get; }

    private DbSettings BuildSettings(bool configured = false) => new()
    {
        Host = Host,
        Port = Port,
        Database = Database,
        User = User,
        Password = Password,
        UseSsl = UseSsl,
        AllowPublicKeyRetrieval = AllowPublicKeyRetrieval,
        IsConfigured = configured
    };

    private async Task CheckConnectionAsync()
    {
        try
        {
            var settings = BuildSettings();
            await _dbInitializer.TestConnectionAsync(_dbSettingsService.GetConnectionString(settings));
            _connectionTestPassed = true;
            Status = "Підключення успішне";
            Logs.Add("✔ З'єднання з MySQL успішне.");
        }
        catch (Exception ex)
        {
            _connectionTestPassed = false;
            Status = $"Помилка підключення: {ex.Message}";
            Logs.Add($"✖ Помилка: {ex.Message}");
        }
    }

    private async Task InitializeSchemaAsync()
    {
        try
        {
            var settings = BuildSettings();
            Logs.Add("Починаю ініціалізацію БД...");
            await _dbInitializer.EnsureDatabaseAsync(_dbSettingsService.GetServerConnectionString(settings), Database);
            Logs.Add("✔ База даних створена або вже існує.");
            await _dbInitializer.EnsureSchemaAsync(_dbSettingsService.GetConnectionString(settings));
            Logs.Add("✔ Схема та індекси ініціалізовано.");
            _schemaInitialized = true;
            Status = "Структура БД готова";
        }
        catch (Exception ex)
        {
            Logs.Add($"✖ Помилка ініціалізації: {ex.Message}");
            Status = "Помилка ініціалізації";
            _schemaInitialized = false;
        }
    }

    private async Task CreateAdminAsync()
    {
        if (string.IsNullOrWhiteSpace(AdminLogin) || string.IsNullOrWhiteSpace(AdminPassword))
        {
            Status = "Заповніть логін та пароль адміністратора";
            return;
        }

        if (AdminPassword != AdminConfirmPassword)
        {
            Status = "Паролі не співпадають";
            return;
        }

        try
        {
            await _userService.CreateUserAsync(AdminLogin.Trim(), AdminPassword, UserRole.Admin, AdminFullName);
            _adminCreated = true;
            Status = "Адміністратора створено";
            Logs.Add("✔ Користувача Admin створено.");
        }
        catch (Exception ex)
        {
            Status = ex.Message;
            Logs.Add($"✖ Помилка створення адміністратора: {ex.Message}");
        }
    }

    private async Task FinishAsync()
    {
        await _dbSettingsService.SaveAsync(BuildSettings(true));
        IsCompleted = true;
        Status = "Налаштування завершено";
    }
}
