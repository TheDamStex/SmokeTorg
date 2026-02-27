using System.Windows.Input;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Common.Logging;
using SmokeTorg.Presentation.Services;

namespace SmokeTorg.Presentation.ViewModels;

public class LoginViewModel : ViewModelBase, IDialogRequestClose
{
    private readonly AuthService _authService;
    private readonly ILogger _logger;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _isBusy;
    private string _errorMessage = string.Empty;

    public LoginViewModel(AuthService authService, ILogger logger)
    {
        _authService = authService;
        _logger = logger;

        LoginCommand = new AsyncRelayCommand(_ => LoginAsync(), _ => CanLogin);
        ExitCommand = new RelayCommand(_ => RequestClose?.Invoke(this, false), _ => !IsBusy);

        ValidateAll();
        RefreshCommandStates();
    }

    public event EventHandler<bool?>? RequestClose;

    public string Username
    {
        get => _username;
        set
        {
            if (SetProperty(ref _username, value))
            {
                ErrorMessage = string.Empty;
                RefreshCommandStates();
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
                ErrorMessage = string.Empty;
                RefreshCommandStates();
            }
        }
    }

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

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool CanLogin =>
        !IsBusy &&
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password);

    public AuthSession? CurrentSession => _authService.CurrentSession;

    public ICommand LoginCommand { get; }
    public ICommand ExitCommand { get; }

    public async Task<bool> LoginAsync()
    {
        ValidateAll();
        if (HasErrors)
        {
            ErrorMessage = "Заповніть логін і пароль.";
            return false;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            _logger.Info($"Спроба входу користувача: {Username}");
            var session = await _authService.LoginAsync(Username, Password);

            if (session is null)
            {
                _logger.Info($"Невдала авторизація користувача: {Username}");
                AddError(nameof(Username), "Невірний логін або пароль");
                AddError(nameof(Password), "Невірний логін або пароль");
                ErrorMessage = "Невірний логін або пароль";
                return false;
            }

            ClearErrors(nameof(Username));
            ClearErrors(nameof(Password));
            ErrorMessage = string.Empty;
            _logger.Info($"Успішна авторизація користувача: {Username}");
            OnPropertyChanged(nameof(CurrentSession));
            RequestClose?.Invoke(this, true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Помилка авторизації користувача: {Username}", ex);
            ErrorMessage = "Не вдалося виконати вхід. Спробуйте ще раз.";
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override void ValidateProperty(string propertyName)
    {
        ClearErrors(propertyName);
        if (propertyName == nameof(Username) && string.IsNullOrWhiteSpace(Username)) AddError(propertyName, "Поле обов'язкове");
        if (propertyName == nameof(Password) && string.IsNullOrWhiteSpace(Password)) AddError(propertyName, "Поле обов'язкове");
    }

    private void RefreshCommandStates()
    {
        if (LoginCommand is AsyncRelayCommand loginCommand)
        {
            loginCommand.RaiseCanExecuteChanged();
        }

        if (ExitCommand is RelayCommand exitCommand)
        {
            exitCommand.RaiseCanExecuteChanged();
        }
    }
}
