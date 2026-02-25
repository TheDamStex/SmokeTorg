using System.Collections.ObjectModel;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Enums;
using SmokeTorg.Presentation.Models;

namespace SmokeTorg.Presentation.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly LoginViewModel _loginVm;
    private readonly PosViewModel _posVm;
    private readonly ProductsViewModel _productsVm;
    private readonly PurchasesViewModel _purchasesVm;
    private readonly PlaceholderViewModel _placeholderVm;
    private object? _currentViewModel;
    private bool _isHomeView = true;

    public MainViewModel(LoginViewModel loginVm, PosViewModel posVm, ProductsViewModel productsVm, PurchasesViewModel purchasesVm, PlaceholderViewModel placeholderVm)
    {
        _loginVm = loginVm;
        _posVm = posVm;
        _productsVm = productsVm;
        _purchasesVm = purchasesVm;
        _placeholderVm = placeholderVm;

        AppTitle = "SmokeTorg";
        StoreName = "Магазин: Central Store";
        DbName = "БД: SmokeTorgDb";
        UserName = "owner";
        Role = "Владелец";
        LicenseInfo = "Лицензия: ST-2026-001";
        DateInfo = $"{DateTime.Now:dd.MM.yyyy} ({DateTime.Now:dddd})";
        DbInfo = "Версия: 1.0.0";
        ModeInfo = "Режим: Терминал";

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
        LoginCommand = new AsyncRelayCommand(async _ => await DoLoginAsync());

        OpenPOSCommand = new RelayCommand(_ => OpenModule(_posVm));
        OpenProductsCommand = new RelayCommand(_ => OpenModule(_productsVm));
        OpenPurchasesCommand = new RelayCommand(_ => OpenModule(_purchasesVm));
        OpenReportsCommand = new RelayCommand(_ => OpenPlaceholder("Отчеты"));
        OpenSettingsCommand = new RelayCommand(_ => OpenPlaceholder("Настройки"));
        OpenPlaceholderCommand = new RelayCommand(p => OpenPlaceholder(p?.ToString() ?? "Модуль"));
        OpenHomeCommand = new RelayCommand(_ => OpenHome());
    }

    public object? CurrentViewModel { get => _currentViewModel; set => SetProperty(ref _currentViewModel, value); }
    public bool IsHomeView { get => _isHomeView; set => SetProperty(ref _isHomeView, value); }

    public string AppTitle { get; }
    public string StoreName { get; }
    public string DbName { get; }
    public string UserName { get; private set; }
    public string Role { get; private set; }
    public string LicenseInfo { get; }
    public string DbInfo { get; }
    public string DateInfo { get; }
    public string ModeInfo { get; }
    public ObservableCollection<NewsItem> News { get; }

    public string CurrentUserInfo => _loginVm.CurrentUser is null ? "Гость" : $"{_loginVm.CurrentUser.Username} ({_loginVm.CurrentUser.Role})";
    public bool IsAdmin => _loginVm.CurrentUser?.Role == UserRole.Admin;
    public bool CanManageCatalog => _loginVm.CurrentUser?.Role is UserRole.Admin or UserRole.Manager;

    public RelayCommand NavigateCommand { get; }
    public AsyncRelayCommand LoginCommand { get; }
    public RelayCommand OpenPOSCommand { get; }
    public RelayCommand OpenProductsCommand { get; }
    public RelayCommand OpenPurchasesCommand { get; }
    public RelayCommand OpenReportsCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }
    public RelayCommand OpenPlaceholderCommand { get; }
    public RelayCommand OpenHomeCommand { get; }

    private void Navigate(object? p)
    {
        switch (p?.ToString())
        {
            case "POS":
                OpenModule(_posVm);
                break;
            case "Products":
                OpenModule(_productsVm);
                break;
            case "Purchases":
                OpenModule(_purchasesVm);
                break;
            case "Home":
                OpenHome();
                break;
            default:
                OpenPlaceholder(p?.ToString() ?? "Модуль");
                break;
        }
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

    private void OpenHome()
    {
        CurrentViewModel = null;
        IsHomeView = true;
    }

    private async Task DoLoginAsync()
    {
        await _loginVm.LoginAsync();
        UserName = _loginVm.CurrentUser?.Username ?? "owner";
        Role = _loginVm.CurrentUser?.Role switch
        {
            UserRole.Admin => "Администратор",
            UserRole.Manager => "Менеджер",
            UserRole.Cashier => "Кассир",
            _ => "Владелец"
        };

        OnPropertyChanged(nameof(UserName));
        OnPropertyChanged(nameof(Role));
        OnPropertyChanged(nameof(CurrentUserInfo));
        OnPropertyChanged(nameof(IsAdmin));
        OnPropertyChanged(nameof(CanManageCatalog));
    }
}
