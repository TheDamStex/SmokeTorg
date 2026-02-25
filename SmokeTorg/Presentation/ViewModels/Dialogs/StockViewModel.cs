using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Presentation.Services;

namespace SmokeTorg.Presentation.ViewModels.Dialogs;

public class StockViewModel : ViewModelBase, IDialogRequestClose
{
    private readonly InventoryService _inventoryService;
    private readonly ProductService _productService;
    private readonly CategoryService _categoryService;
    private string _searchText = string.Empty;
    private bool _belowMinimumOnly;

    public StockViewModel(InventoryService inventoryService, ProductService productService, CategoryService categoryService)
    {
        _inventoryService = inventoryService;
        _productService = productService;
        _categoryService = categoryService;
        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FilterRows;
    }

    public ObservableCollection<StockRowItem> Items { get; } = [];
    public ICollectionView ItemsView { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ItemsView.Refresh();
            }
        }
    }

    public bool BelowMinimumOnly
    {
        get => _belowMinimumOnly;
        set
        {
            if (SetProperty(ref _belowMinimumOnly, value))
            {
                ItemsView.Refresh();
            }
        }
    }

    public event EventHandler<bool?>? RequestClose;

    public AsyncRelayCommand RefreshCommand => new(async _ => await LoadAsync());

    public RelayCommand ExportCommand => new(_ =>
    {
        MessageBox.Show("Експорт буде реалізовано на наступному етапі.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
    });

    public RelayCommand CloseCommand => new(_ => RequestClose?.Invoke(this, false));

    public async Task LoadAsync()
    {
        var stock = await _inventoryService.GetStockAsync();
        var products = await _productService.GetAllAsync();
        var categories = await _categoryService.GetAllAsync();
        var categoryMap = categories.ToDictionary(c => c.Id, c => c.Name);

        Items.Clear();
        foreach (var row in stock)
        {
            var product = products.FirstOrDefault(p => p.Id == row.ProductId);
            if (product is null)
            {
                continue;
            }

            var min = product.MinStock;
            var isLow = row.Quantity < min;
            Items.Add(new StockRowItem
            {
                ProductName = product.Name,
                Barcode = product.Barcode,
                Category = categoryMap.GetValueOrDefault(product.CategoryId, "Без категорії"),
                Quantity = row.Quantity,
                MinQuantity = min,
                Status = isLow ? "Нижче мінімуму" : "В нормі"
            });
        }

        ItemsView.Refresh();
    }

    private bool FilterRows(object obj)
    {
        if (obj is not StockRowItem row)
        {
            return false;
        }

        var passesText = string.IsNullOrWhiteSpace(SearchText)
            || row.ProductName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || row.Barcode.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || row.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

        var passesLowStock = !BelowMinimumOnly || row.Quantity < row.MinQuantity;
        return passesText && passesLowStock;
    }
}

public class StockRowItem
{
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal MinQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
}
