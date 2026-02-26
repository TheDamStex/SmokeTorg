using System.Windows;
using SmokeTorg.Presentation.ViewModels.Windows;

namespace SmokeTorg.Presentation.Views.Windows;

public partial class DbSettingsWindow : Window
{
    public DbSettingsWindow(DbSettingsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += async (_, _) => await vm.LoadAsync();
    }
}
