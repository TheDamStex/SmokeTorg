using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;

namespace SmokeTorg.Presentation.ViewModels;

public class LoginViewModel(AuthService authService) : ViewModelBase
{
    private string _username = string.Empty;
    private string _password = string.Empty;

    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }

    public AuthSession? CurrentSession => authService.CurrentSession;

    public async Task<bool> LoginAsync()
    {
        ValidateAll();
        if (HasErrors) return false;

        var session = await authService.LoginAsync(Username, Password);
        if (session is null)
        {
            AddError(nameof(Username), "Невірний логін або пароль");
            AddError(nameof(Password), "Невірний логін або пароль");
            return false;
        }

        ClearErrors(nameof(Username));
        ClearErrors(nameof(Password));
        OnPropertyChanged(nameof(CurrentSession));
        return true;
    }

    protected override void ValidateProperty(string propertyName)
    {
        ClearErrors(propertyName);
        if (propertyName == nameof(Username) && string.IsNullOrWhiteSpace(Username)) AddError(propertyName, "Поле обов'язкове");
        if (propertyName == nameof(Password) && string.IsNullOrWhiteSpace(Password)) AddError(propertyName, "Поле обов'язкове");
    }
}
