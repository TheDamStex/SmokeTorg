using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using SmokeTorg.Application.Interfaces;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Converters;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Entities;
using SmokeTorg.Domain.Enums;

namespace SmokeTorg.Presentation.ViewModels.Windows;

public class UserManagementViewModel : ViewModelBase
{
    private static readonly Regex LoginRegex = new("^[A-Za-z0-9._-]+$", RegexOptions.Compiled);

    private readonly IUserService _userService;
    private readonly AuthService _authService;

    private UserRowViewModel? _selectedUser;
    private string _search = string.Empty;
    private UserRole? _roleFilter;
    private string _newUsername = string.Empty;
    private string _newPassword = string.Empty;
    private string _newFullName = string.Empty;
    private UserRole _newRole = UserRole.Seller;
    private UserRole _editRole = UserRole.Seller;
    private string _status = string.Empty;

    private static readonly IReadOnlyList<UserRole> RoleSelection = Enum.GetValues<UserRole>()
        .Distinct()
        .ToArray();

    public UserManagementViewModel(IUserService userService, AuthService authService)
    {
        _userService = userService;
        _authService = authService;

        CreateUserCommand = new AsyncRelayCommand(async _ => await CreateUserAsync(), _ => CanCreateUser());
        SaveChangesCommand = new AsyncRelayCommand(async _ => await SaveChangesAsync(), _ => CanSaveChanges());
        ToggleActiveCommand = new AsyncRelayCommand(async _ => await ToggleActiveAsync(), _ => IsAdmin && SelectedUser is not null);
        ResetPasswordCommand = new AsyncRelayCommand(async _ => await ResetPasswordAsync(), _ => IsAdmin && SelectedUser is not null);
        LoadCommand = new AsyncRelayCommand(async _ => await LoadAsync());

        ValidateAll();
    }

    public ObservableCollection<UserRowViewModel> Users { get; } = [];
    public IReadOnlyList<UserRole> Roles { get; } = RoleSelection;
    public IReadOnlyList<RoleFilterOption> RoleFilters { get; } =
    [
        new RoleFilterOption(null, "Усі ролі"),
        ..RoleSelection.Select(role => new RoleFilterOption(role, RoleToUkrainianConverter.ToDisplay(role)))
    ];

    public UserRole? RoleFilter
    {
        get => _roleFilter;
        set
        {
            if (SetProperty(ref _roleFilter, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public string Search
    {
        get => _search;
        set
        {
            if (SetProperty(ref _search, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public string NewUsername
    {
        get => _newUsername;
        set
        {
            if (SetProperty(ref _newUsername, value))
            {
                CreateUserCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewPassword
    {
        get => _newPassword;
        set
        {
            if (SetProperty(ref _newPassword, value))
            {
                CreateUserCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewFullName
    {
        get => _newFullName;
        set
        {
            if (SetProperty(ref _newFullName, value))
            {
                CreateUserCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public UserRole NewRole
    {
        get => _newRole;
        set
        {
            if (SetProperty(ref _newRole, value))
            {
                CreateUserCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public UserRole EditRole
    {
        get => _editRole;
        set
        {
            if (SetProperty(ref _editRole, value))
            {
                SaveChangesCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public UserRowViewModel? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (!SetProperty(ref _selectedUser, value))
            {
                return;
            }

            if (_selectedUser is not null)
            {
                EditRole = _selectedUser.Role;
            }

            SaveChangesCommand.RaiseCanExecuteChanged();
            ToggleActiveCommand.RaiseCanExecuteChanged();
            ResetPasswordCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsAdmin => _authService.CurrentSession?.Role == UserRole.Admin;

    public AsyncRelayCommand CreateUserCommand { get; }
    public AsyncRelayCommand SaveChangesCommand { get; }
    public AsyncRelayCommand ToggleActiveCommand { get; }
    public AsyncRelayCommand ResetPasswordCommand { get; }
    public AsyncRelayCommand LoadCommand { get; }

    public async Task LoadAsync()
    {
        if (!IsAdmin)
        {
            throw new InvalidOperationException("Недостатньо прав доступу");
        }

        Users.Clear();
        var list = await _userService.GetUsersAsync(RoleFilter, Search);
        foreach (var user in list)
        {
            Users.Add(new UserRowViewModel(user));
        }

        if (SelectedUser is not null)
        {
            SelectedUser = Users.FirstOrDefault(u => u.Id == SelectedUser.Id);
        }
    }

    protected override void ValidateProperty(string propertyName)
    {
        base.ValidateProperty(propertyName);

        switch (propertyName)
        {
            case nameof(NewUsername):
                ClearErrors(nameof(NewUsername));
                if (string.IsNullOrWhiteSpace(NewUsername))
                {
                    AddError(nameof(NewUsername), "Логін обов'язковий.");
                }
                else if (NewUsername.Trim().Length < 3)
                {
                    AddError(nameof(NewUsername), "Логін має містити щонайменше 3 символи.");
                }
                else if (!LoginRegex.IsMatch(NewUsername.Trim()))
                {
                    AddError(nameof(NewUsername), "Логін може містити лише латиницю, цифри та символи . _ -");
                }
                break;
            case nameof(NewPassword):
                ClearErrors(nameof(NewPassword));
                if (string.IsNullOrWhiteSpace(NewPassword))
                {
                    AddError(nameof(NewPassword), "Пароль обов'язковий.");
                }
                else if (NewPassword.Length < 8)
                {
                    AddError(nameof(NewPassword), "Пароль має містити щонайменше 8 символів.");
                }
                break;
            case nameof(NewFullName):
                ClearErrors(nameof(NewFullName));
                if (string.IsNullOrWhiteSpace(NewFullName))
                {
                    AddError(nameof(NewFullName), "ПІБ обов'язковий.");
                }
                break;
            case nameof(NewRole):
                ClearErrors(nameof(NewRole));
                break;
        }
    }

    protected override void OnErrorsChanged(string propertyName)
    {
        base.OnErrorsChanged(propertyName);
        CreateUserCommand.RaiseCanExecuteChanged();
    }

    private bool CanCreateUser() => IsAdmin && !HasErrors;

    private bool CanSaveChanges() => IsAdmin && SelectedUser is not null && SelectedUser.Role != EditRole;

    private async Task CreateUserAsync()
    {
        if (!IsAdmin || !ValidateAll())
        {
            return;
        }

        await _userService.CreateUserAsync(NewUsername.Trim(), NewPassword, NewRole, NewFullName.Trim());
        Status = "Користувача створено.";

        NewUsername = string.Empty;
        NewPassword = string.Empty;
        NewFullName = string.Empty;
        NewRole = UserRole.Seller;

        await LoadAsync();
    }

    private async Task SaveChangesAsync()
    {
        if (!IsAdmin || SelectedUser is null)
        {
            return;
        }

        if (SelectedUser.Role != EditRole)
        {
            await _userService.UpdateUserRoleAsync(SelectedUser.Id, EditRole);
            Status = "Зміни ролі користувача збережено.";
            await LoadAsync();
        }
    }

    private async Task ToggleActiveAsync()
    {
        if (!IsAdmin || SelectedUser is null)
        {
            return;
        }

        await _userService.SetUserActiveAsync(SelectedUser.Id, !SelectedUser.IsActive);
        Status = SelectedUser.IsActive ? "Користувача деактивовано." : "Користувача активовано.";
        await LoadAsync();
    }

    private async Task ResetPasswordAsync()
    {
        if (!IsAdmin || SelectedUser is null)
        {
            return;
        }

        await _userService.ResetPasswordAsync(SelectedUser.Id, "Temp1234");
        Status = "Пароль скинуто до Temp1234.";
    }
}

public sealed class RoleFilterOption
{
    public RoleFilterOption(UserRole? role, string displayName)
    {
        Role = role;
        DisplayName = displayName;
    }

    public UserRole? Role { get; }

    public string DisplayName { get; }
}

public class UserRowViewModel
{
    public UserRowViewModel(User user)
    {
        Id = user.Id;
        Username = user.Username;
        Role = user.Role;
        IsActive = user.IsActive;
        FullName = user.FullName;
    }

    public Guid Id { get; }
    public string Username { get; }
    public UserRole Role { get; }
    public bool IsActive { get; }
    public string FullName { get; }
}
