using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Enums;

namespace SmokeTorg.Presentation.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly LoginViewModel _loginVm;
    private readonly PosViewModel _posVm;
    private readonly ProductsViewModel _productsVm;
    private readonly PurchasesViewModel _purchasesVm;
    private readonly PlaceholderViewModel _placeholderVm;
    private object _currentViewModel;

    public MainViewModel(LoginViewModel loginVm, PosViewModel posVm, ProductsViewModel productsVm, PurchasesViewModel purchasesVm, PlaceholderViewModel placeholderVm)
    {
        _loginVm = loginVm;
        _posVm = posVm;
        _productsVm = productsVm;
        _purchasesVm = purchasesVm;
        _placeholderVm = placeholderVm;
        _currentViewModel = _posVm;

        NavigateCommand = new RelayCommand(Navigate);
        LoginCommand = new AsyncRelayCommand(async _ => await DoLoginAsync());
    }

    public object CurrentViewModel { get => _currentViewModel; set => SetProperty(ref _currentViewModel, value); }
    public string CurrentUserInfo => _loginVm.CurrentUser is null ? "Гость" : $"{_loginVm.CurrentUser.Username} ({_loginVm.CurrentUser.Role})";
    public bool IsAdmin => _loginVm.CurrentUser?.Role == UserRole.Admin;
    public bool CanManageCatalog => _loginVm.CurrentUser?.Role is UserRole.Admin or UserRole.Manager;

    public RelayCommand NavigateCommand { get; }
    public AsyncRelayCommand LoginCommand { get; }

    private void Navigate(object? p)
    {
        CurrentViewModel = p?.ToString() switch
        {
            "POS" => _posVm,
            "Products" => _productsVm,
            "Purchases" => _purchasesVm,
            _ => _placeholderVm.WithTitle(p?.ToString() ?? "Модуль")
        };
    }

    private async Task DoLoginAsync()
    {
        await _loginVm.LoginAsync();
        OnPropertyChanged(nameof(CurrentUserInfo));
        OnPropertyChanged(nameof(IsAdmin));
        OnPropertyChanged(nameof(CanManageCatalog));
    }
}
