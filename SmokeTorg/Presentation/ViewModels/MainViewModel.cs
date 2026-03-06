using System.Collections.ObjectModel;
using System.Windows;
using SmokeTorg.Application.Services;
using SmokeTorg;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Common.Converters;
using SmokeTorg.Domain.Enums;
using SmokeTorg.Presentation.Models;
using SmokeTorg.Presentation.Services;
using SmokeTorg.Presentation.ViewModels.Dialogs;
using SmokeTorg.Presentation.Views.Windows;

namespace SmokeTorg.Presentation.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly LoginViewModel _loginVm;
    private readonly PosViewModel _posVm;
    private readonly ProductsViewModel _productsVm;
    private readonly PurchasesViewModel _purchasesVm;
    private readonly PlaceholderViewModel _placeholderVm;
    private readonly IWindowService _windowService;
    private readonly PosWindowViewModel _posWindowViewModel;
    private readonly IServiceProvider _serviceProvider;
    private readonly AuthService _authService;
    private object? _currentViewModel;
    private bool _isHomeView = true;
    private bool _isAuthenticated;
    private string _currentUserLogin = string.Empty;
    private string _currentUserRoleDisplay = string.Empty;
    private string _currentUserFullName = string.Empty;
    private string _accessStatus = "Гостьовий доступ";

    public MainViewModel(
        LoginViewModel loginVm,
        PosViewModel posVm,
        ProductsViewModel productsVm,
        PurchasesViewModel purchasesVm,
        PlaceholderViewModel placeholderVm,
        IWindowService windowService,
        PosWindowViewModel posWindowViewModel,
        IServiceProvider serviceProvider,
        AuthService authService)
    {
        _loginVm = loginVm;
        _posVm = posVm;
        _productsVm = productsVm;
        _purchasesVm = purchasesVm;
        _placeholderVm = placeholderVm;
        _windowService = windowService;
        _posWindowViewModel = posWindowViewModel;
        _serviceProvider = serviceProvider;
        _authService = authService;

        AppTitle = "SmokeTorg";
        StoreName = "Магазин: Central Store";
        DbName = "БД: SmokeTorgDb";
        LicenseInfo = "Ліцензія: ST-2026-001";
        DateInfo = $"{DateTime.Now:dd.MM.yyyy} ({DateTime.Now:dddd})";
        DbInfo = "Версія: 1.0.0";
        ModeInfo = "Режим: Термінал";

        News = new ObservableCollection<NewsItem>
        {
            new()
            {
                Title = "Оновлено стартову панель",
                Date = new DateTime(2021, 11, 30),
                Text = "Додано сучасний домашній екран із швидкими діями, панеллю стану та блоком новин."
            },
            new()
            {
                Title = "Порада дня",
                Date = new DateTime(2021, 11, 29),
                Text = "Для швидкого старту використайте кнопки швидкого доступу до реалізації, приходу та звітів."
            }
        };

        NavigateCommand = new RelayCommand(Navigate);
        LoginCommand = new AsyncRelayCommand(async _ => await DoLoginAsync(), _ => !IsAuthenticated);
        LogoutCommand = new AsyncRelayCommand(async _ => await DoLogoutAsync(), _ => IsAuthenticated);

        OpenPOSCommand = new RelayCommand(_ => OpenModule(_posVm));
        OpenProductsCommand = new AsyncRelayCommand(async _ => await OpenProductsModuleAsync());
        OpenPurchasesWindowCommand = new AsyncRelayCommand(async _ => await OpenPurchasesWindowAsync());
        OpenReportsCommand = new RelayCommand(_ => OpenPlaceholder("Звіти"));
        OpenSettingsCommand = new RelayCommand(_ => OpenDbSettings());
        OpenUserManagementCommand = new RelayCommand(_ => OpenUserManagement());
        OpenPlaceholderCommand = new RelayCommand(p => OpenPlaceholder(p?.ToString() ?? "Модуль"));
        OpenHomeCommand = new RelayCommand(_ => OpenHome());

        OpenPosCommand = new RelayCommand(_ => _windowService.ShowWindow(_posWindowViewModel));
        OpenStockCommand = new AsyncRelayCommand(async _ => await OpenProductsModuleAsync());

        RefreshCurrentUserState();
    }

    public object? CurrentViewModel { get => _currentViewModel; set => SetProperty(ref _currentViewModel, value); }
    public bool IsHomeView { get => _isHomeView; set => SetProperty(ref _isHomeView, value); }

    public string AppTitle { get; }
    public string StoreName { get; }
    public string DbName { get; }
    public string LicenseInfo { get; }
    public string DbInfo { get; }
    public string DateInfo { get; }
    public string ModeInfo { get; }
    public ObservableCollection<NewsItem> News { get; }

    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        private set => SetProperty(ref _isAuthenticated, value);
    }

    public bool IsNotAuthenticated => !IsAuthenticated;

    public string CurrentUserLogin
    {
        get => _currentUserLogin;
        private set => SetProperty(ref _currentUserLogin, value);
    }

    public string CurrentUserRoleDisplay
    {
        get => _currentUserRoleDisplay;
        private set => SetProperty(ref _currentUserRoleDisplay, value);
    }

    public string CurrentUserFullName
    {
        get => _currentUserFullName;
        private set => SetProperty(ref _currentUserFullName, value);
    }

    public string AccessStatus
    {
        get => _accessStatus;
        private set => SetProperty(ref _accessStatus, value);
    }

    public string CurrentUserInfo => IsAuthenticated
        ? $"{CurrentUserLogin} | {CurrentUserRoleDisplay}"
        : "Гість";

    public bool IsAdmin => _authService.CurrentSession?.Role == UserRole.Admin;
    public bool CanManageCatalog => _authService.CurrentSession?.Role is UserRole.Admin or UserRole.Manager;

    public RelayCommand NavigateCommand { get; }
    public AsyncRelayCommand LoginCommand { get; }
    public AsyncRelayCommand LogoutCommand { get; }
    public RelayCommand OpenPOSCommand { get; }
    public AsyncRelayCommand OpenProductsCommand { get; }
    public AsyncRelayCommand OpenPurchasesWindowCommand { get; }
    public RelayCommand OpenReportsCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }
    public RelayCommand OpenPlaceholderCommand { get; }
    public RelayCommand OpenUserManagementCommand { get; }
    public RelayCommand OpenHomeCommand { get; }
    public RelayCommand OpenPosCommand { get; }
    public AsyncRelayCommand OpenStockCommand { get; }

    public void RefreshCurrentUserState()
    {
        var session = _authService.CurrentSession;

        IsAuthenticated = session is not null;
        CurrentUserLogin = session?.Username ?? string.Empty;
        CurrentUserRoleDisplay = session is null
            ? string.Empty
            : RoleToUkrainianConverter.ToDisplay(session.Role);
        CurrentUserFullName = session?.FullName ?? string.Empty;
        AccessStatus = session?.IsActive == true ? "Доступ активний" : "Немає активної сесії";

        OnPropertyChanged(nameof(IsNotAuthenticated));
        OnPropertyChanged(nameof(CurrentUserInfo));
        OnPropertyChanged(nameof(IsAdmin));
        OnPropertyChanged(nameof(CanManageCatalog));

        LoginCommand.RaiseCanExecuteChanged();
        LogoutCommand.RaiseCanExecuteChanged();
    }

    private void Navigate(object? p)
    {
        switch (p?.ToString())
        {
            case "POS":
                OpenModule(_posVm);
                break;
            case "Products":
                _ = OpenProductsModuleAsync();
                break;
            case "Purchases":
                _ = OpenPurchasesWindowAsync();
                break;
            case "Home":
                OpenHome();
                break;
            default:
                OpenPlaceholder(p?.ToString() ?? "Модуль");
                break;
        }
    }

    private async Task OpenProductsModuleAsync()
    {
        await _productsVm.LoadAsync();
        OpenModule(_productsVm);
    }

    private void OpenModule(object vm)
    {
        CurrentViewModel = vm;
        IsHomeView = false;
    }

    private void OpenPlaceholder(string title)
    {
        CurrentViewModel = _placeholderVm.WithTitle(title);
        IsHomeView = false;
    }

    private async Task OpenPurchasesWindowAsync()
    {
        await _purchasesVm.LoadAsync();
        _windowService.ShowWindow(_purchasesVm);
    }

    private void OpenHome()
    {
        CurrentViewModel = null;
        IsHomeView = true;
    }

    private void OpenDbSettings()
    {
        if (!IsAdmin)
        {
            MessageBox.Show("Недостатньо прав доступу");
            return;
        }

        var window = (DbSettingsWindow)_serviceProvider.GetService(typeof(DbSettingsWindow))!;
        window.Owner = System.Windows.Application.Current.MainWindow;
        window.ShowDialog();
    }

    private void OpenUserManagement()
    {
        if (!IsAdmin)
        {
            MessageBox.Show("Недостатньо прав доступу");
            return;
        }

        var window = (UserManagementWindow)_serviceProvider.GetService(typeof(UserManagementWindow))!;
        window.Owner = System.Windows.Application.Current.MainWindow;
        window.ShowDialog();
    }

    private async Task DoLoginAsync()
    {
        await _loginVm.LoginAsync();
        RefreshCurrentUserState();
    }

    private async Task DoLogoutAsync()
    {
        _authService.Logout();
        RefreshCurrentUserState();

        var application = System.Windows.Application.Current;
        application.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var currentMainWindow = application.MainWindow;
        currentMainWindow?.Close();

        var loginWindow = (LoginWindow)_serviceProvider.GetService(typeof(LoginWindow))!;
        var loginVm = (LoginViewModel)_serviceProvider.GetService(typeof(LoginViewModel))!;

        loginVm.Username = string.Empty;
        loginVm.Password = string.Empty;
        loginWindow.DataContext = loginVm;

        EventHandler<bool?>? loginRequestClose = null;
        loginRequestClose = (_, dialogResult) =>
        {
            loginVm.RequestClose -= loginRequestClose;

            if (dialogResult != true)
            {
                loginWindow.Close();
                application.Shutdown();
                return;
            }

            var mainWindow = (MainWindow)_serviceProvider.GetService(typeof(MainWindow))!;
            if (mainWindow.DataContext is MainViewModel mainVm)
            {
                mainVm.RefreshCurrentUserState();
            }

            application.MainWindow = mainWindow;
            application.ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();
            loginWindow.Close();
        };

        loginVm.RequestClose += loginRequestClose;
        application.MainWindow = loginWindow;
        application.ShutdownMode = ShutdownMode.OnMainWindowClose;
        loginWindow.Show();

        await Task.CompletedTask;
    }
}
