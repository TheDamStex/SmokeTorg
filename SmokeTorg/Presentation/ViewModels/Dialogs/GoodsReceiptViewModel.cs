using System.Collections.ObjectModel;
using System.Windows;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Entities;
using SmokeTorg.Domain.Enums;
using SmokeTorg.Presentation.Services;

namespace SmokeTorg.Presentation.ViewModels.Dialogs;

public class GoodsReceiptViewModel(
    SupplierService supplierService,
    ProductService productService,
    PurchaseService purchaseService,
    IDialogService dialogService) : ViewModelBase, IDialogRequestClose
{
    private readonly SupplierService _supplierService = supplierService;
    private readonly ProductService _productService = productService;
    private readonly PurchaseService _purchaseService = purchaseService;
    private readonly IDialogService _dialogService = dialogService;

    private Supplier? _selectedSupplier;
    private Product? _selectedSearchProduct;
    private string _productSearchText = string.Empty;
    private string _barcodeInput = string.Empty;

    public ObservableCollection<Supplier> Suppliers { get; } = [];
    public ObservableCollection<ReceiptLineItem> Items { get; } = [];
    public ObservableCollection<Product> ProductSearchResults { get; } = [];

    public Supplier? SelectedSupplier
    {
        get => _selectedSupplier;
        set => SetProperty(ref _selectedSupplier, value);
    }

    public string ProductSearchText
    {
        get => _productSearchText;
        set => SetProperty(ref _productSearchText, value);
    }

    public string BarcodeInput
    {
        get => _barcodeInput;
        set => SetProperty(ref _barcodeInput, value);
    }

    public Product? SelectedSearchProduct
    {
        get => _selectedSearchProduct;
        set => SetProperty(ref _selectedSearchProduct, value);
    }

    public decimal Total => Items.Sum(i => i.Sum);

    public event EventHandler<bool?>? RequestClose;

    public async Task InitializeAsync()
    {
        await ReloadSuppliers();
        SelectedSupplier ??= Suppliers.FirstOrDefault();
    }

    public AsyncRelayCommand SearchProductCommand => new(async _ =>
    {
        ProductSearchResults.Clear();
        foreach (var product in await _productService.SearchAsync(ProductSearchText.Trim()))
        {
            ProductSearchResults.Add(product);
        }
    });

    public AsyncRelayCommand AddByBarcodeCommand => new(async _ =>
    {
        var barcode = BarcodeInput.Trim();
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return;
        }

        var product = await _productService.FindByBarcode(barcode);
        if (product is null)
        {
            MessageBox.Show("Товар не знайдено за штрихкодом", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        AddProduct(product);
        BarcodeInput = string.Empty;
    });

    public RelayCommand AddProductCommand => new(p =>
    {
        if (p is Product product)
        {
            AddProduct(product);
        }
    });

    public RelayCommand RemoveItemCommand => new(p =>
    {
        if (p is not ReceiptLineItem item)
        {
            return;
        }

        Items.Remove(item);
        OnPropertyChanged(nameof(Total));
    });

    public AsyncRelayCommand SaveDraftCommand => new(async _ =>
    {
        var purchase = BuildPurchase(DocumentStatus.Draft);
        await _purchaseService.SaveAsync(purchase);
        MessageBox.Show("Чернетку приходу збережено.", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
    }, _ => CanSave());

    public AsyncRelayCommand PostCommand => new(async _ =>
    {
        var purchase = BuildPurchase(DocumentStatus.Draft);
        await _purchaseService.SaveAsync(purchase);
        await _purchaseService.PostAsync(purchase);

        MessageBox.Show("Документ приходу проведено та залишки оновлено.", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
        RequestClose?.Invoke(this, true);
    }, _ => CanSave());

    public RelayCommand CancelCommand => new(_ => RequestClose?.Invoke(this, false));

    public AsyncRelayCommand AddSupplierCommand => new(OpenCreateSupplierDialogAsync);

    public async Task ReloadSuppliers()
    {
        Suppliers.Clear();
        foreach (var supplier in await _supplierService.GetAllAsync())
        {
            Suppliers.Add(supplier);
        }
    }

    public void SelectCreatedSupplier(Supplier createdSupplier)
    {
        SelectedSupplier = Suppliers.FirstOrDefault(x => x.Id == createdSupplier.Id);
    }

    private async Task OpenCreateSupplierDialogAsync(object? _)
    {
        var vm = new SupplierCreateViewModel(_supplierService);
        if (_dialogService.ShowDialog(vm) == true && vm.CreatedSupplier is not null)
        {
            await ReloadSuppliers();
            SelectCreatedSupplier(vm.CreatedSupplier);
        }
    }

    private bool CanSave() => SelectedSupplier is not null && Items.Count > 0 && Items.All(i => i.Quantity > 0 && i.PurchasePrice > 0);

    private Purchase BuildPurchase(DocumentStatus status) => new()
    {
        SupplierId = SelectedSupplier!.Id,
        Date = DateTime.Now,
        Status = status,
        Items = Items.Select(i => new PurchaseItem
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            BarcodeDisplay = i.Barcode,
            Quantity = i.Quantity,
            Price = i.PurchasePrice
        }).ToList()
    };

    private void AddProduct(Product product)
    {
        var existing = Items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing is null)
        {
            var line = new ReceiptLineItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Barcode = product.Barcode,
                Quantity = 1,
                PurchasePrice = product.PurchasePrice <= 0 ? product.SalePrice : product.PurchasePrice
            };
            line.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Total));
            Items.Add(line);
        }
        else
        {
            existing.Quantity += 1;
        }

        OnPropertyChanged(nameof(Total));
    }
}

public class ReceiptLineItem : ViewModelBase
{
    private decimal _quantity = 1;
    private decimal _purchasePrice;

    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                OnPropertyChanged(nameof(Sum));
            }
        }
    }

    public decimal PurchasePrice
    {
        get => _purchasePrice;
        set
        {
            if (SetProperty(ref _purchasePrice, value))
            {
                OnPropertyChanged(nameof(Sum));
            }
        }
    }

    public decimal Sum => Quantity * PurchasePrice;
}
