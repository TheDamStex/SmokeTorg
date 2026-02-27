using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using SmokeTorg.Application.Interfaces;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Enums;
using SmokeTorg.Presentation.Services;

namespace SmokeTorg.Presentation.ViewModels.Windows;

public class SetupWizardViewModel : ViewModelBase, IDialogRequestClose
{
    private static readonly Regex AdminUsernameRegex = new("^[A-Za-z0-9._-]+$", RegexOptions.Compiled);

    private readonly IDbSettingsService _dbSettingsService;
    private readonly IDbInitializer _dbInitializer;
    private readonly IUserService _userService;

    private int _stepIndex;
    private string _host = "localhost";
    private string _port = "3306";
    private string _database = "smoketorg";
    private string _user = "root";
    private string _password = string.Empty;
    private bool _useSsl;
    private bool _allowPublicKeyRetrieval = true;

    private string _adminLogin = "admin";
    private string _adminPassword = string.Empty;
    private string _adminConfirmPassword = string.Empty;
    private string _adminFullName = string.Empty;

    private bool _isBusy;
    private bool _connectionTestPassed;
    private bool _schemaInitialized;
    private bool _adminCreated;
    private int _schemaVersion;
    private string _status = "Готово до налаштування.";

    public SetupWizardViewModel(IDbSettingsService dbSettingsService, IDbInitializer dbInitializer, IUserService userService)
    {
        _dbSettingsService = dbSettingsService;
        _dbInitializer = dbInitializer;
        _userService = userService;

        CheckConnectionCommand = new AsyncRelayCommand(async _ => await CheckConnectionAsync(), _ => CanCheckConnection);
        InitializeSchemaCommand = new AsyncRelayCommand(async _ => await InitializeSchemaAsync(), _ => CanInitializeSchema);
        CreateAdminCommand = new AsyncRelayCommand(async _ => await CreateAdminAsync(), _ => CanCreateAdmin);
        NextCommand = new RelayCommand(_ => StepIndex++, _ => CanMoveNext);
        BackCommand = new RelayCommand(_ => StepIndex--, _ => CanMoveBack);
        FinishCommand = new AsyncRelayCommand(async _ => await FinishAsync(), _ => CanFinish);

        ValidateAll();
    }

    public event EventHandler<bool?>? RequestClose;

    public string Host
    {
        get => _host;
        set
        {
            if (SetProperty(ref _host, value))
            {
                InvalidateConnectionState();
            }
        }
    }

    public string Port
    {
        get => _port;
        set
        {
            if (SetProperty(ref _port, value))
            {
                InvalidateConnectionState();
            }
        }
    }

    public string Database
    {
        get => _database;
        set
        {
            if (SetProperty(ref _database, value))
            {
                InvalidateConnectionState();
            }
        }
    }

    public string User
    {
        get => _user;
        set
        {
            if (SetProperty(ref _user, value))
            {
                InvalidateConnectionState();
                OnPropertyChanged(nameof(PasswordWarning));
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                InvalidateConnectionState();
                OnPropertyChanged(nameof(PasswordWarning));
            }
        }
    }

    public bool UseSsl
    {
        get => _useSsl;
        set
        {
            if (SetProperty(ref _useSsl, value))
            {
                InvalidateConnectionState();
            }
        }
    }

    public bool AllowPublicKeyRetrieval
    {
        get => _allowPublicKeyRetrieval;
        set
        {
            if (SetProperty(ref _allowPublicKeyRetrieval, value))
            {
                InvalidateConnectionState();
            }
        }
    }

    public string AdminLogin
    {
        get => _adminLogin;
        set
        {
            if (SetProperty(ref _adminLogin, value))
            {
                _adminCreated = false;
                RefreshCommandStates();
                OnPropertyChanged(nameof(CanGoToSummary));
                OnPropertyChanged(nameof(SummaryAdminLogin));
            }
        }
    }

    public string AdminPassword
    {
        get => _adminPassword;
        set
        {
            if (SetProperty(ref _adminPassword, value))
            {
                _adminCreated = false;
                ValidateProperty(nameof(AdminConfirmPassword));
                RefreshCommandStates();
                OnPropertyChanged(nameof(CanGoToSummary));
            }
        }
    }

    public string AdminConfirmPassword
    {
        get => _adminConfirmPassword;
        set
        {
            if (SetProperty(ref _adminConfirmPassword, value))
            {
                _adminCreated = false;
                RefreshCommandStates();
                OnPropertyChanged(nameof(CanGoToSummary));
            }
        }
    }

    public string AdminFullName
    {
        get => _adminFullName;
        set => SetProperty(ref _adminFullName, value);
    }

    public int StepIndex
    {
        get => _stepIndex;
        private set
        {
            if (SetProperty(ref _stepIndex, value))
            {
                OnPropertyChanged(nameof(IsStep1));
                OnPropertyChanged(nameof(IsStep2));
                OnPropertyChanged(nameof(IsStep3));
                OnPropertyChanged(nameof(IsStep4));
                OnPropertyChanged(nameof(StepTitle));
                OnPropertyChanged(nameof(StepSubtitle));
                RefreshCommandStates();
            }
        }
    }

    public bool IsStep1 => StepIndex == 0;
    public bool IsStep2 => StepIndex == 1;
    public bool IsStep3 => StepIndex == 2;
    public bool IsStep4 => StepIndex == 3;

    public string StepTitle => StepIndex switch
    {
        0 => "Підключення до MySQL",
        1 => "Ініціалізація схеми бази даних",
        2 => "Створення облікового запису адміністратора",
        _ => "Завершення налаштування"
    };

    public string StepSubtitle => StepIndex switch
    {
        0 => "Вкажіть параметри з’єднання та перевірте доступ до сервера.",
        1 => "Створіть базу даних, таблиці та службову версію схеми.",
        2 => "Створіть основного адміністратора для входу в систему.",
        _ => "Перевірте підсумок та завершіть майстер налаштування."
    };

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RefreshCommandStates();
            }
        }
    }

    public bool IsCompleted { get; private set; }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public string? PasswordWarning =>
        string.Equals(User.Trim(), "root", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(Password)
            ? "Увага: користувач root без пароля є небезпечним для продакшн-середовища."
            : null;

    public bool CanCheckConnection => !IsBusy && IsConnectionInputValid;
    public bool CanInitializeSchema => !IsBusy && IsStep2 && _connectionTestPassed;
    public bool CanCreateAdmin => !IsBusy && IsStep3 && IsAdminInputValid;
    public bool CanGoToInitialization => _connectionTestPassed;
    public bool CanGoToAdmin => _schemaInitialized && SchemaVersion > 0;
    public bool CanGoToSummary => _adminCreated;

    public bool CanMoveNext => !IsBusy && StepIndex switch
    {
        0 => CanGoToInitialization,
        1 => CanGoToAdmin,
        2 => CanGoToSummary,
        _ => false
    };

    public bool CanMoveBack => !IsBusy && StepIndex > 0;
    public bool CanFinish => !IsBusy && IsStep4 && _adminCreated && SchemaVersion > 0;

    public bool IsConnectionInputValid =>
        !HasPropertyErrors(nameof(Host)) &&
        !HasPropertyErrors(nameof(Port)) &&
        !HasPropertyErrors(nameof(Database)) &&
        !HasPropertyErrors(nameof(User));

    public bool IsAdminInputValid =>
        !HasPropertyErrors(nameof(AdminLogin)) &&
        !HasPropertyErrors(nameof(AdminPassword)) &&
        !HasPropertyErrors(nameof(AdminConfirmPassword));

    public int SchemaVersion
    {
        get => _schemaVersion;
        private set
        {
            if (SetProperty(ref _schemaVersion, value))
            {
                OnPropertyChanged(nameof(CanGoToAdmin));
                OnPropertyChanged(nameof(SummarySchemaVersion));
                RefreshCommandStates();
            }
        }
    }

    public string SummaryConnection => $"{Host}:{Port} / {Database}";
    public string SummaryUser => User;
    public string SummarySchemaVersion => SchemaVersion.ToString();
    public string SummaryAdminLogin => AdminLogin;

    public ObservableCollection<WizardLogEntry> Logs { get; } = [];

    public AsyncRelayCommand CheckConnectionCommand { get; }
    public AsyncRelayCommand InitializeSchemaCommand { get; }
    public AsyncRelayCommand CreateAdminCommand { get; }
    public RelayCommand NextCommand { get; }
    public RelayCommand BackCommand { get; }
    public AsyncRelayCommand FinishCommand { get; }

    protected override void ValidateProperty(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(Host):
                ClearErrors(nameof(Host));
                if (string.IsNullOrWhiteSpace(Host))
                {
                    AddError(nameof(Host), "Вкажіть хост MySQL.");
                }
                break;

            case nameof(Port):
                ClearErrors(nameof(Port));
                if (!int.TryParse(Port, out var parsedPort) || parsedPort is < 1 or > 65535)
                {
                    AddError(nameof(Port), "Порт має бути числом від 1 до 65535.");
                }
                break;

            case nameof(Database):
                ClearErrors(nameof(Database));
                if (string.IsNullOrWhiteSpace(Database))
                {
                    AddError(nameof(Database), "Назва бази даних обов’язкова.");
                }
                else if (Database.Any(char.IsWhiteSpace))
                {
                    AddError(nameof(Database), "Назва бази даних не може містити пробілів.");
                }
                break;

            case nameof(User):
                ClearErrors(nameof(User));
                if (string.IsNullOrWhiteSpace(User))
                {
                    AddError(nameof(User), "Вкажіть користувача MySQL.");
                }
                break;

            case nameof(AdminLogin):
                ClearErrors(nameof(AdminLogin));
                if (string.IsNullOrWhiteSpace(AdminLogin))
                {
                    AddError(nameof(AdminLogin), "Логін адміністратора обов’язковий.");
                }
                else if (AdminLogin.Trim().Length < 3)
                {
                    AddError(nameof(AdminLogin), "Логін має містити щонайменше 3 символи.");
                }
                else if (!AdminUsernameRegex.IsMatch(AdminLogin.Trim()))
                {
                    AddError(nameof(AdminLogin), "Дозволені лише латиниця, цифри та символи . _ -");
                }
                break;

            case nameof(AdminPassword):
                ClearErrors(nameof(AdminPassword));
                if (string.IsNullOrWhiteSpace(AdminPassword))
                {
                    AddError(nameof(AdminPassword), "Пароль адміністратора обов’язковий.");
                }
                else if (AdminPassword.Length < 8)
                {
                    AddError(nameof(AdminPassword), "Пароль має містити щонайменше 8 символів.");
                }
                break;

            case nameof(AdminConfirmPassword):
                ClearErrors(nameof(AdminConfirmPassword));
                if (!string.Equals(AdminPassword, AdminConfirmPassword, StringComparison.Ordinal))
                {
                    AddError(nameof(AdminConfirmPassword), "Підтвердження пароля не співпадає.");
                }
                break;
        }

        RefreshCommandStates();
    }

    private async Task CheckConnectionAsync()
    {
        if (!ValidateConnectionStep())
        {
            Status = "Виправте помилки у параметрах підключення.";
            return;
        }

        try
        {
            IsBusy = true;
            AddLog("Перевірка з’єднання з MySQL…", WizardLogType.Info);
            await _dbInitializer.TestConnectionAsync(_dbSettingsService.GetConnectionString(BuildSettings()));

            _connectionTestPassed = true;
            Status = "Підключення успішно перевірено.";
            AddLog("З’єднання успішне.", WizardLogType.Success);
        }
        catch (Exception ex)
        {
            _connectionTestPassed = false;
            Status = BuildFriendlyDbError(ex);
            AddLog(Status, WizardLogType.Error);
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanGoToInitialization));
            RefreshCommandStates();
        }
    }

    private async Task InitializeSchemaAsync()
    {
        try
        {
            IsBusy = true;
            AddLog("Починаю ініціалізацію БД…", WizardLogType.Info);
            var settings = BuildSettings();
            var serverConnection = _dbSettingsService.GetServerConnectionString(settings);
            var appConnection = _dbSettingsService.GetConnectionString(settings);

            await _dbInitializer.EnsureDatabaseAsync(serverConnection, Database.Trim());
            AddLog("Базу даних створено або вона вже існує.", WizardLogType.Success);

            await _dbInitializer.EnsureSchemaAsync(appConnection);
            SchemaVersion = await _dbInitializer.GetSchemaVersionAsync(appConnection);
            _schemaInitialized = SchemaVersion > 0;

            Status = _schemaInitialized
                ? "Схему БД успішно ініціалізовано."
                : "Не вдалося підтвердити версію схеми.";

            AddLog(_schemaInitialized
                ? $"Схему застосовано. Версія: {SchemaVersion}."
                : "Схему застосовано, але версія не визначена.",
                _schemaInitialized ? WizardLogType.Success : WizardLogType.Error);
        }
        catch (Exception ex)
        {
            _schemaInitialized = false;
            SchemaVersion = 0;
            Status = BuildFriendlyDbError(ex);
            AddLog($"Помилка ініціалізації: {Status}", WizardLogType.Error);
        }
        finally
        {
            IsBusy = false;
            RefreshCommandStates();
        }
    }

    private async Task CreateAdminAsync()
    {
        if (!ValidateAdminStep())
        {
            Status = "Виправте помилки в обліковому записі адміністратора.";
            return;
        }

        try
        {
            IsBusy = true;
            AddLog("Створення адміністратора…", WizardLogType.Info);

            await _userService.CreateUserAsync(AdminLogin.Trim(), AdminPassword, UserRole.Admin, AdminFullName.Trim());

            _adminCreated = true;
            Status = "Адміністратора успішно створено.";
            AddLog($"Користувача {AdminLogin.Trim()} створено.", WizardLogType.Success);
        }
        catch (Exception ex)
        {
            _adminCreated = false;
            Status = ex.Message;
            AddLog($"Помилка створення адміністратора: {ex.Message}", WizardLogType.Error);
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanGoToSummary));
            RefreshCommandStates();
        }
    }

    private async Task FinishAsync()
    {
        await _dbSettingsService.SaveAsync(BuildSettings(true));
        IsCompleted = true;
        Status = "Налаштування завершено.";
        AddLog("Налаштування збережено. Майстер завершено.", WizardLogType.Success);
        RequestClose?.Invoke(this, true);
    }

    private bool ValidateConnectionStep()
    {
        ValidateProperty(nameof(Host));
        ValidateProperty(nameof(Port));
        ValidateProperty(nameof(Database));
        ValidateProperty(nameof(User));
        return IsConnectionInputValid;
    }

    private bool ValidateAdminStep()
    {
        ValidateProperty(nameof(AdminLogin));
        ValidateProperty(nameof(AdminPassword));
        ValidateProperty(nameof(AdminConfirmPassword));
        return IsAdminInputValid;
    }

    private bool HasPropertyErrors(string propertyName) => GetErrors(propertyName).Cast<object>().Any();

    private DbSettings BuildSettings(bool configured = false)
    {
        var parsedPort = int.TryParse(Port, out var value) ? value : 3306;

        return new DbSettings
        {
            Host = Host.Trim(),
            Port = parsedPort,
            Database = Database.Trim(),
            User = User.Trim(),
            Password = Password,
            UseSsl = UseSsl,
            AllowPublicKeyRetrieval = AllowPublicKeyRetrieval,
            IsConfigured = configured
        };
    }

    private void InvalidateConnectionState()
    {
        _connectionTestPassed = false;
        _schemaInitialized = false;
        SchemaVersion = 0;
        OnPropertyChanged(nameof(CanGoToInitialization));
        OnPropertyChanged(nameof(SummaryConnection));
        OnPropertyChanged(nameof(SummaryUser));
        RefreshCommandStates();
    }

    private void AddLog(string message, WizardLogType type)
    {
        Logs.Add(new WizardLogEntry(DateTime.Now, message, type));
    }

    private string BuildFriendlyDbError(Exception ex)
    {
        var message = ex.Message;
        if (message.Contains("Access denied", StringComparison.OrdinalIgnoreCase))
        {
            return "Відмовлено в доступі. Перевірте логін, пароль і права користувача MySQL.";
        }

        if (message.Contains("Unable to connect", StringComparison.OrdinalIgnoreCase)
            || message.Contains("connect", StringComparison.OrdinalIgnoreCase))
        {
            return "Неможливо підключитися до MySQL. Перевірте хост, порт і доступність сервера.";
        }

        if (message.Contains("Unknown database", StringComparison.OrdinalIgnoreCase))
        {
            return "Вказана база даних не існує. Створіть її на кроці 2.";
        }

        return $"Сталася помилка MySQL: {message}";
    }

    private void RefreshCommandStates()
    {
        CheckConnectionCommand.RaiseCanExecuteChanged();
        InitializeSchemaCommand.RaiseCanExecuteChanged();
        CreateAdminCommand.RaiseCanExecuteChanged();
        NextCommand.RaiseCanExecuteChanged();
        BackCommand.RaiseCanExecuteChanged();
        FinishCommand.RaiseCanExecuteChanged();
    }
}

public sealed record WizardLogEntry(DateTime Timestamp, string Message, WizardLogType Type)
{
    public string TimeLabel => Timestamp.ToString("HH:mm:ss");
}

public enum WizardLogType
{
    Info,
    Success,
    Error
}
