using System.Windows;
using SmokeTorg.Presentation.ViewModels;

namespace SmokeTorg.Presentation.Views.Windows;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;

    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        DataContext = _vm = vm;
    }

    private async void OnLoginClick(object sender, RoutedEventArgs e)
    {
        if (await _vm.LoginAsync())
        {
            DialogResult = true;
            Close();
            return;
        }

        MessageBox.Show("Невірний логін або пароль", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
