using System.Windows;
using SmokeTorg.Presentation.ViewModels.Windows;

namespace SmokeTorg.Presentation.Views.Windows;

public partial class UserManagementWindow : Window
{
    public UserManagementWindow(UserManagementViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += async (_, _) => await vm.LoadAsync();
    }
}
