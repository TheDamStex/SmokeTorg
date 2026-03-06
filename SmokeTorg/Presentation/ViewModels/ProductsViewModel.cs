using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Entities;

namespace SmokeTorg.Presentation.ViewModels;

public class ProductsViewModel : ViewModelBase
{
    private readonly ProductService _productService;
    private readonly InventoryService _inventoryService;
    private readonly CategoryService _categoryService;

    private readonly ObservableCollection<ProductStockRowViewModel> _items = [];

    private ProductStockRowViewModel? _selected;
    private string _searchText = string.Empty;
    private CategoryFilterItem? _selectedCategoryFilter;
    private bool _belowMinimumOnly;
    private ActivityFilterOption _selectedActivityFilter = ActivityFilterOption.ActiveOnly;

    private Guid _editingId;
    private long _editingVersion;

    private string _name = string.Empty;
    private string _sku = string.Empty;
    private string _barcode = string.Empty;
    private Guid _categoryId;
    private decimal _purchasePrice;
    private decimal _salePrice;
    private decimal _minStock;
    private bool _isActive = true;

    public ProductsViewModel(ProductService productService, InventoryService inventoryService, CategoryService categoryService)
    {
        _productService = productService;
        _inventoryService = inventoryService;
        _categoryService = categoryService;

        ItemsView = CollectionViewSource.GetDefaultView(_items);
        ItemsView.Filter = FilterRows;

        RefreshCommand = new AsyncRelayCommand(async _ => await LoadAsync());
        AddCommand = new RelayCommand(_ => BeginNew());
        EditCommand = new RelayCommand(_ => BeginEdit(), _ => Selected is not null);
        SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync(), _ => !HasErrors);
        DeleteCommand = new AsyncRelayCommand(async _ => await DeactivateAsync(), _ => Selected is not null);
        CancelCommand = new RelayCommand(_ => CancelEdit());

        ActivityFilters = new ObservableCollection<ActivityFilterItem>
        {
            new("Лише активні", ActivityFilterOption.ActiveOnly),
            new("Лише неактивні", ActivityFilterOption.InactiveOnly),
            new("Усі", ActivityFilterOption.All)
        };

        SelectedActivityFilter = ActivityFilters.First();
        BeginNew();
    }

    public ICollectionView ItemsView { get; }
    public ObservableCollection<CategoryFilterItem> CategoryFilters { get; } = [];
    public ObservableCollection<CategoryLookupItem> CategoryOptions { get; } = [];
    public ObservableCollection<ActivityFilterItem> ActivityFilters { get; }

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

    public CategoryFilterItem? SelectedCategoryFilter
    {
        get => _selectedCategoryFilter;
        set
        {
            if (SetProperty(ref _selectedCategoryFilter, value))
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

    public ActivityFilterItem? SelectedActivityFilter
    {
        get => ActivityFilters.FirstOrDefault(x => x.Option == _selectedActivityFilter);
        set
        {
            if (value is null)
            {
                return;
            }

            if (SetProperty(ref _selectedActivityFilter, value.Option, nameof(SelectedActivityFilter)))
            {
                ItemsView.Refresh();
            }
        }
    }

    public ProductStockRowViewModel? Selected
    {
        get => _selected;
        set
        {
            if (SetProperty(ref _selected, value))
            {
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                if (value is not null)
                {
                    PopulateForm(value);
                }
            }
        }
    }

    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Sku { get => _sku; set => SetProperty(ref _sku, value); }
    public string Barcode { get => _barcode; set => SetProperty(ref _barcode, value); }

    public Guid CategoryId
    {
        get => _categoryId;
        set => SetProperty(ref _categoryId, value);
    }

    public decimal PurchasePrice { get => _purchasePrice; set => SetProperty(ref _purchasePrice, value); }
    public decimal SalePrice { get => _salePrice; set => SetProperty(ref _salePrice, value); }
    public decimal MinStock { get => _minStock; set => SetProperty(ref _minStock, value); }
    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }

    public AsyncRelayCommand RefreshCommand { get; }
    public RelayCommand AddCommand { get; }
    public RelayCommand EditCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand DeleteCommand { get; }
    public RelayCommand CancelCommand { get; }

    protected override void ValidateProperty(string propertyName)
    {
        ClearErrors(propertyName);

        switch (propertyName)
        {
            case nameof(Name):
                if (string.IsNullOrWhiteSpace(Name))
                {
                    AddError(nameof(Name), "Назва обов'язкова");
                }
                break;
            case nameof(Barcode):
                if (string.IsNullOrWhiteSpace(Barcode))
                {
                    AddError(nameof(Barcode), "Штрихкод обов'язковий");
                }
                break;
            case nameof(CategoryId):
                if (CategoryOptions.Count > 0 && CategoryId == Guid.Empty)
                {
                    AddError(nameof(CategoryId), "Оберіть категорію");
                }
                break;
            case nameof(PurchasePrice):
                if (PurchasePrice < 0)
                {
                    AddError(nameof(PurchasePrice), "Ціна має бути >= 0");
                }
                break;
            case nameof(SalePrice):
                if (SalePrice < 0)
                {
                    AddError(nameof(SalePrice), "Ціна має бути >= 0");
                }
                break;
            case nameof(MinStock):
                if (MinStock < 0)
                {
                    AddError(nameof(MinStock), "Мінімальний залишок має бути >= 0");
                }
                break;
        }
    }

    protected override void OnErrorsChanged(string propertyName) => SaveCommand.RaiseCanExecuteChanged();

    public async Task LoadAsync()
    {
        var products = await _productService.GetAllAsync();
        var stock = await _inventoryService.GetStockAsync();
        var categories = await _categoryService.GetAllAsync();

        var stockMap = stock.ToDictionary(x => x.ProductId, x => x.Quantity);
        var categoryMap = categories.ToDictionary(x => x.Id, x => x.Name);

        CategoryOptions.Clear();
        foreach (var category in categories.OrderBy(c => c.Name))
        {
            CategoryOptions.Add(new CategoryLookupItem(category.Id, category.Name));
        }

        CategoryFilters.Clear();
        CategoryFilters.Add(new CategoryFilterItem(Guid.Empty, "Усі категорії"));
        foreach (var category in categories.OrderBy(c => c.Name))
        {
            CategoryFilters.Add(new CategoryFilterItem(category.Id, category.Name));
        }

        SelectedCategoryFilter ??= CategoryFilters.FirstOrDefault();

        _items.Clear();
        foreach (var product in products)
        {
            var quantity = stockMap.GetValueOrDefault(product.Id, 0);
            _items.Add(new ProductStockRowViewModel
            {
                Product = product,
                Name = product.Name,
                Sku = product.Sku,
                Barcode = product.Barcode,
                CategoryName = categoryMap.GetValueOrDefault(product.CategoryId, "Без категорії"),
                PurchasePrice = product.PurchasePrice,
                SalePrice = product.SalePrice,
                StockQuantity = quantity,
                MinStock = product.MinStock,
                IsActive = product.IsActive
            });
        }

        ItemsView.Refresh();
        EditCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
    }

    private void BeginNew()
    {
        _editingId = Guid.Empty;
        _editingVersion = 0;
        Selected = null;
        Name = string.Empty;
        Sku = string.Empty;
        Barcode = string.Empty;
        CategoryId = CategoryOptions.FirstOrDefault()?.Id ?? Guid.Empty;
        PurchasePrice = 0;
        SalePrice = 0;
        MinStock = 0;
        IsActive = true;
        ValidateAll();
    }

    private void BeginEdit()
    {
        if (Selected is null)
        {
            return;
        }

        PopulateForm(Selected);
    }

    private void PopulateForm(ProductStockRowViewModel row)
    {
        _editingId = row.Product.Id;
        _editingVersion = row.Product.Version;
        Name = row.Product.Name;
        Sku = row.Product.Sku;
        Barcode = row.Product.Barcode;
        CategoryId = row.Product.CategoryId;
        PurchasePrice = row.Product.PurchasePrice;
        SalePrice = row.Product.SalePrice;
        MinStock = row.Product.MinStock;
        IsActive = row.Product.IsActive;
        ValidateAll();
    }

    private async Task SaveAsync()
    {
        ValidateAll();
        await ValidateBarcodeUniquenessAsync();

        if (HasErrors)
        {
            MessageBox.Show("Виправте помилки у формі.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var product = new Product
        {
            Id = _editingId,
            Version = _editingVersion,
            Name = Name.Trim(),
            Sku = Sku.Trim(),
            Barcode = Barcode.Trim(),
            CategoryId = CategoryId,
            PurchasePrice = PurchasePrice,
            SalePrice = SalePrice,
            MinStock = MinStock,
            IsActive = IsActive
        };

        try
        {
            await _productService.SaveAsync(product);
            await LoadAsync();
            Selected = _items.FirstOrDefault(x => x.Product.Id == product.Id);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task DeactivateAsync()
    {
        if (Selected is null)
        {
            return;
        }

        var product = Selected.Product;
        product.IsActive = false;

        await _productService.SaveAsync(product);
        await LoadAsync();
        BeginNew();
    }

    private void CancelEdit()
    {
        if (Selected is not null)
        {
            PopulateForm(Selected);
            return;
        }

        BeginNew();
    }

    private bool FilterRows(object obj)
    {
        if (obj is not ProductStockRowViewModel row)
        {
            return false;
        }

        var query = SearchText?.Trim() ?? string.Empty;
        var textMatches = string.IsNullOrWhiteSpace(query)
            || row.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || row.Barcode.Contains(query, StringComparison.OrdinalIgnoreCase)
            || row.Sku.Contains(query, StringComparison.OrdinalIgnoreCase);

        var categoryMatches = SelectedCategoryFilter is null
            || SelectedCategoryFilter.Id == Guid.Empty
            || row.Product.CategoryId == SelectedCategoryFilter.Id;

        var stockMatches = !BelowMinimumOnly || row.IsBelowMinimum;

        var activeMatches = _selectedActivityFilter switch
        {
            ActivityFilterOption.ActiveOnly => row.IsActive,
            ActivityFilterOption.InactiveOnly => !row.IsActive,
            _ => true
        };

        return textMatches && categoryMatches && stockMatches && activeMatches;
    }

    private async Task ValidateBarcodeUniquenessAsync()
    {
        ValidateProperty(nameof(Barcode));
        if (GetErrors(nameof(Barcode)).Cast<string>().Any())
        {
            return;
        }

        var existing = await _productService.FindByBarcode(Barcode.Trim());
        if (existing is not null && existing.Id != _editingId)
        {
            AddError(nameof(Barcode), "Штрихкод вже використовується");
        }
    }
}

public class ProductStockRowViewModel
{
    public required Product Product { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string Barcode { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public decimal PurchasePrice { get; init; }
    public decimal SalePrice { get; init; }
    public decimal StockQuantity { get; init; }
    public decimal MinStock { get; init; }
    public bool IsActive { get; init; }

    public bool IsBelowMinimum => StockQuantity < MinStock;
    public bool IsOutOfStock => StockQuantity <= 0;

    public string StockStatusDisplay
    {
        get
        {
            if (IsOutOfStock)
            {
                return "Відсутній";
            }

            return IsBelowMinimum ? "Нижче мінімуму" : "Норма";
        }
    }

    public string ActivityDisplay => IsActive ? "Активний" : "Неактивний";
}

public record CategoryFilterItem(Guid Id, string Name);
public record CategoryLookupItem(Guid Id, string Name);
public record ActivityFilterItem(string Name, ActivityFilterOption Option);

public enum ActivityFilterOption
{
    ActiveOnly,
    InactiveOnly,
    All
}
