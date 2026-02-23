using System.ComponentModel;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Domain.Entities;

namespace SmokeTorg.Presentation.ViewModels;

public class LoginViewModel(AuthService authService) : ViewModelBase, IDataErrorInfo
{
    private string _username = "admin";
    private string _password = "admin123";

    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }

    public User? CurrentUser { get; private set; }

    public async Task<bool> LoginAsync()
    {
        CurrentUser = await authService.LoginAsync(Username, Password);
        return CurrentUser is not null;
    }

    public string Error => string.Empty;
    public string this[string columnName] => columnName switch
    {
        nameof(Username) when string.IsNullOrWhiteSpace(Username) => "Введите логин",
        nameof(Password) when string.IsNullOrWhiteSpace(Password) => "Введите пароль",
        _ => string.Empty
    };
}
