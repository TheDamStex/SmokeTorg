using System.ComponentModel;
using System.Windows;
using SmokeTorg.Presentation.ViewModels.Windows;

namespace SmokeTorg.Presentation.Views.Windows;

public partial class SetupWizardWindow : Window
{
    public SetupWizardWindow(SetupWizardViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is SetupWizardViewModel vm && e.PropertyName == nameof(SetupWizardViewModel.IsCompleted) && vm.IsCompleted)
        {
            DialogResult = true;
            Close();
        }
    }
}
