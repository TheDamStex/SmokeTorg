using System.Collections.ObjectModel;
using SmokeTorg.Application.Interfaces;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Entities;
using SmokeTorg.Domain.Enums;

namespace SmokeTorg.Presentation.ViewModels.Windows;

public class UserManagementViewModel : ViewModelBase
{
    private readonly IUserService _userService;
    private readonly AuthService _authService;
    private User? _selectedUser;
    private string _search = string.Empty;

    public UserManagementViewModel(IUserService userService, AuthService authService)
    {
        _userService = userService;
        _authService = authService;
        CreateUserCommand = new AsyncRelayCommand(async _ => await CreateUserAsync(), _ => IsAdmin);
        ToggleActiveCommand = new AsyncRelayCommand(async _ => await ToggleActiveAsync(), _ => IsAdmin && SelectedUser is not null);
        ResetPasswordCommand = new AsyncRelayCommand(async _ => await ResetPasswordAsync(), _ => IsAdmin && SelectedUser is not null);
        LoadCommand = new AsyncRelayCommand(async _ => await LoadAsync());
    }

    public ObservableCollection<User> Users { get; } = [];
    public IEnumerable<UserRole> Roles { get; } = Enum.GetValues<UserRole>();
    public UserRole? RoleFilter { get; set; }
    public string Search { get => _search; set => SetProperty(ref _search, value); }

    public string NewUsername { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string NewFullName { get; set; } = string.Empty;
    public UserRole NewRole { get; set; } = UserRole.Seller;
    public string Status { get; set; } = string.Empty;

    public User? SelectedUser { get => _selectedUser; set => SetProperty(ref _selectedUser, value); }
    public bool IsAdmin => _authService.CurrentSession?.Role == UserRole.Admin;

    public AsyncRelayCommand CreateUserCommand { get; }
    public AsyncRelayCommand ToggleActiveCommand { get; }
    public AsyncRelayCommand ResetPasswordCommand { get; }
    public AsyncRelayCommand LoadCommand { get; }

    public async Task LoadAsync()
    {
        if (!IsAdmin) throw new InvalidOperationException("Недостатньо прав доступу");
        Users.Clear();
        var list = await _userService.GetUsersAsync(RoleFilter, Search);
        foreach (var user in list) Users.Add(user);
    }

    private async Task CreateUserAsync()
    {
        if (!IsAdmin) throw new InvalidOperationException("Недостатньо прав доступу");
        await _userService.CreateUserAsync(NewUsername, NewPassword, NewRole, NewFullName);
        Status = "Користувача створено";
        OnPropertyChanged(nameof(Status));
        await LoadAsync();
    }

    private async Task ToggleActiveAsync()
    {
        if (!IsAdmin || SelectedUser is null) throw new InvalidOperationException("Недостатньо прав доступу");
        await _userService.SetUserActiveAsync(SelectedUser.Id, !SelectedUser.IsActive);
        await LoadAsync();
    }

    private async Task ResetPasswordAsync()
    {
        if (!IsAdmin || SelectedUser is null) throw new InvalidOperationException("Недостатньо прав доступу");
        await _userService.ResetPasswordAsync(SelectedUser.Id, "Temp1234");
        Status = "Пароль скинуто до Temp1234";
        OnPropertyChanged(nameof(Status));
    }
}
