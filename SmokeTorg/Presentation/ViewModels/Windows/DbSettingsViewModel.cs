using SmokeTorg.Application.Interfaces;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Enums;

namespace SmokeTorg.Presentation.ViewModels.Windows;

public class DbSettingsViewModel : ViewModelBase
{
    private readonly IDbSettingsService _settingsService;
    private readonly AuthService _authService;

    public DbSettingsViewModel(IDbSettingsService settingsService, AuthService authService)
    {
        _settingsService = settingsService;
        _authService = authService;
        SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync(), _ => IsAdmin);
        LoadCommand = new AsyncRelayCommand(async _ => await LoadAsync(), _ => IsAdmin);
    }

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Database { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public bool IsAdmin => _authService.CurrentSession?.Role == UserRole.Admin;

    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand LoadCommand { get; }

    public async Task LoadAsync()
    {
        if (!IsAdmin) throw new InvalidOperationException("Недостатньо прав доступу");
        var s = await _settingsService.LoadAsync();
        Host = s.Host; Port = s.Port; Database = s.Database; User = s.User; Password = s.Password;
        OnPropertyChanged(nameof(Host));OnPropertyChanged(nameof(Port));OnPropertyChanged(nameof(Database));OnPropertyChanged(nameof(User));OnPropertyChanged(nameof(Password));
    }

    public async Task SaveAsync()
    {
        if (!IsAdmin) throw new InvalidOperationException("Недостатньо прав доступу");
        await _settingsService.SaveAsync(new DbSettings { Host = Host, Port = Port, Database = Database, User = User, Password = Password, IsConfigured = true });
        Status = "Налаштування збережено";
        OnPropertyChanged(nameof(Status));
    }
}
