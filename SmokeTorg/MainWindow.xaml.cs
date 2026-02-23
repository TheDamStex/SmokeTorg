using System.Windows;
using SmokeTorg.Presentation.ViewModels;

namespace SmokeTorg;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
