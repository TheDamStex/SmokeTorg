using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SmokeTorg.Presentation.ViewModels.Dialogs;
using SmokeTorg.Presentation.Views.Windows;

namespace SmokeTorg.Presentation.Services;

public class WpfDialogService(IServiceProvider serviceProvider) : IDialogService
{
    private readonly Dictionary<Type, Type> _windowMap = new()
    {
        [typeof(GoodsReceiptViewModel)] = typeof(GoodsReceiptWindow),
        [typeof(PosWindowViewModel)] = typeof(PosWindow),
        [typeof(DiscountCardsListViewModel)] = typeof(DiscountCardsListWindow),
        [typeof(StockViewModel)] = typeof(StockWindow),
        [typeof(SupplierCreateViewModel)] = typeof(SupplierCreateWindow),
        [typeof(ClientCardViewModel)] = typeof(ClientCardWindow)
    };

    public bool? ShowDialog<TViewModel>(TViewModel vm) where TViewModel : class
    {
        if (!_windowMap.TryGetValue(vm.GetType(), out var windowType))
        {
            throw new InvalidOperationException($"No window mapping configured for view model {vm.GetType().Name}.");
        }

        var window = (Window)serviceProvider.GetRequiredService(windowType);
        window.DataContext = vm;
        window.Owner = System.Windows.Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.IsActive)
            ?? System.Windows.Application.Current.MainWindow;

        if (vm is IDialogRequestClose closable)
        {
            void OnRequestClose(object? _, bool? dialogResult)
            {
                closable.RequestClose -= OnRequestClose;
                window.DialogResult = dialogResult;
                window.Close();
            }

            closable.RequestClose += OnRequestClose;
        }

        return window.ShowDialog();
    }
}
