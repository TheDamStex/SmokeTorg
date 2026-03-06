using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SmokeTorg.Presentation.ViewModels;
using SmokeTorg.Presentation.Views.Windows;

namespace SmokeTorg.Presentation.Services;

public class WpfWindowService(IServiceProvider serviceProvider) : IWindowService
{
    private readonly Dictionary<Type, Type> _windowMap = new()
    {
        [typeof(PurchasesViewModel)] = typeof(PurchasesWindow)
    };

    public void ShowWindow<TViewModel>(TViewModel vm) where TViewModel : class
    {
        if (!_windowMap.TryGetValue(vm.GetType(), out var windowType))
        {
            throw new InvalidOperationException($"No window mapping configured for view model {vm.GetType().Name}.");
        }

        var window = (Window)serviceProvider.GetRequiredService(windowType);
        window.DataContext = vm;
        window.Owner = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.IsActive)
            ?? Application.Current.MainWindow;
        window.Show();
    }
}
