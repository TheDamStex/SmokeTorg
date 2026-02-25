using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Domain.Entities;

namespace SmokeTorg.Presentation.ViewModels;

public class LoginViewModel(AuthService authService) : ViewModelBase
{
    private string _username = "admin";
    private string _password = "admin123";

    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }

    public User? CurrentUser { get; private set; }

    public async Task<bool> LoginAsync()
    {
        ValidateAll();
        if (HasErrors)
        {
            return false;
        }

        CurrentUser = await authService.LoginAsync(Username, Password);
        if (CurrentUser is null)
        {
            AddError(nameof(Username), "Невірний логін або пароль");
            AddError(nameof(Password), "Невірний логін або пароль");
            return false;
        }

        ClearErrors(nameof(Username));
        ClearErrors(nameof(Password));
        return true;
    }

    protected override void ValidateProperty(string propertyName)
    {
        if (propertyName is not (nameof(Username) or nameof(Password)))
        {
            return;
        }

        ClearErrors(propertyName);

        if (propertyName == nameof(Username) && string.IsNullOrWhiteSpace(Username))
        {
            AddError(nameof(Username), "Поле обов’язкове");
        }

        if (propertyName == nameof(Password) && string.IsNullOrWhiteSpace(Password))
        {
            AddError(nameof(Password), "Поле обов’язкове");
        }
    }
}
