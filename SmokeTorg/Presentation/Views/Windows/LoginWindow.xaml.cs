using System.Windows;
using SmokeTorg.Presentation.ViewModels;

namespace SmokeTorg.Presentation.Views.Windows;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
