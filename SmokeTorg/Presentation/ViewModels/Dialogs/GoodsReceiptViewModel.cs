using System.Collections.ObjectModel;
using System.Windows;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Entities;
using SmokeTorg.Domain.Enums;
using SmokeTorg.Presentation.Services;

namespace SmokeTorg.Presentation.ViewModels.Dialogs;

public class GoodsReceiptViewModel : ViewModelBase, IDialogRequestClose
{
    private readonly SupplierService _supplierService;
    private readonly ProductService _productService;
    private readonly PurchaseService _purchaseService;
    private readonly IDialogService _dialogService;

    private Supplier? _selectedSupplier;
    private Product? _selectedSearchProduct;
    private string _productSearchText = string.Empty;
    private string _barcodeInput = string.Empty;

    public GoodsReceiptViewModel(
        SupplierService supplierService,
        ProductService productService,
        PurchaseService purchaseService,
        IDialogService dialogService)
    {
        _supplierService = supplierService;
        _productService = productService;
        _purchaseService = purchaseService;
        _dialogService = dialogService;

        SearchProductCommand = new AsyncRelayCommand(async _ => await SearchProductAsync());
        AddByBarcodeCommand = new AsyncRelayCommand(async _ => await AddByBarcodeAsync());
        AddProductCommand = new RelayCommand(AddProductFromParameter);
        RemoveItemCommand = new RelayCommand(RemoveItem);
        SaveDraftCommand = new AsyncRelayCommand(async _ => await SaveDraftAsync(), _ => CanSaveDraft());
        PostCommand = new AsyncRelayCommand(async _ => await PostAsync(), _ => CanPost());
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(this, false));
        AddSupplierCommand = new AsyncRelayCommand(OpenCreateSupplierDialogAsync);

        Items.CollectionChanged += (_, _) => OnItemsChanged();
    }

    public ObservableCollection<Supplier> Suppliers { get; } = [];
    public ObservableCollection<ReceiptLineItem> Items { get; } = [];
    public ObservableCollection<Product> ProductSearchResults { get; } = [];

    public Supplier? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            if (SetProperty(ref _selectedSupplier, value))
            {
                RevalidateCommands();
            }
        }
    }

    public string ProductSearchText { get => _productSearchText; set => SetProperty(ref _productSearchText, value); }
    public string BarcodeInput { get => _barcodeInput; set => SetProperty(ref _barcodeInput, value); }
    public Product? SelectedSearchProduct { get => _selectedSearchProduct; set => SetProperty(ref _selectedSearchProduct, value); }

    public decimal Total => Items.Sum(i => i.Sum);

    public AsyncRelayCommand SearchProductCommand { get; }
    public AsyncRelayCommand AddByBarcodeCommand { get; }
    public RelayCommand AddProductCommand { get; }
    public RelayCommand RemoveItemCommand { get; }
    public AsyncRelayCommand SaveDraftCommand { get; }
    public AsyncRelayCommand PostCommand { get; }
    public RelayCommand CancelCommand { get; }
    public AsyncRelayCommand AddSupplierCommand { get; }

    public event EventHandler<bool?>? RequestClose;

    public async Task InitializeAsync()
    {
        await ReloadSuppliers();
        SelectedSupplier ??= Suppliers.FirstOrDefault();
        ValidateAll();
        RevalidateCommands();
    }

    protected override void ValidateProperty(string propertyName)
    {
        ClearErrors(propertyName);

        switch (propertyName)
        {
            case nameof(SelectedSupplier):
                if (SelectedSupplier is null)
                {
                    AddError(nameof(SelectedSupplier), "Поле обов’язкове");
                }
                break;

            case nameof(Items):
                if (Items.Count == 0)
                {
                    AddError(nameof(Items), "Додайте хоча б одну позицію");
                }
                else if (Items.Any(x => x.Quantity <= 0 || x.PurchasePrice < 0))
                {
                    AddError(nameof(Items), "Перевірте кількість та ціну в позиціях");
                }
                break;
        }
    }

    protected override void OnErrorsChanged(string propertyName)
    {
        RevalidateCommands();
    }

    public async Task ReloadSuppliers()
    {
        Suppliers.Clear();
        foreach (var supplier in await _supplierService.GetAllAsync()) Suppliers.Add(supplier);
    }

    public void SelectCreatedSupplier(Supplier createdSupplier)
    {
        SelectedSupplier = Suppliers.FirstOrDefault(x => x.Id == createdSupplier.Id);
    }

    private async Task SearchProductAsync()
    {
        ProductSearchResults.Clear();
        foreach (var product in await _productService.SearchAsync(ProductSearchText.Trim()))
        {
            ProductSearchResults.Add(product);
        }
    }

    private async Task AddByBarcodeAsync()
    {
        var barcode = BarcodeInput.Trim();
        if (string.IsNullOrWhiteSpace(barcode)) return;

        var product = await _productService.FindByBarcode(barcode);
        if (product is null)
        {
            MessageBox.Show("Товар не знайдено за штрихкодом", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        AddProduct(product);
        BarcodeInput = string.Empty;
    }

    private void AddProductFromParameter(object? parameter)
    {
        if (parameter is Product product)
        {
            AddProduct(product);
        }
    }

    private void RemoveItem(object? parameter)
    {
        if (parameter is not ReceiptLineItem item) return;

        item.PropertyChanged -= ItemOnPropertyChanged;
        Items.Remove(item);
        OnItemsChanged();
    }

    private async Task SaveDraftAsync()
    {
        ValidateProperty(nameof(Items));
        if (!CanSaveDraft())
        {
            MessageBox.Show("Виправте помилки у формі.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var purchase = BuildPurchase(DocumentStatus.Draft);
        await _purchaseService.SaveAsync(purchase);
        MessageBox.Show("Чернетку приходу збережено.", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task PostAsync()
    {
        ValidateAll();
        if (HasErrors)
        {
            MessageBox.Show("Виправте помилки у формі.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var purchase = BuildPurchase(DocumentStatus.Draft);
        await _purchaseService.SaveAsync(purchase);
        await _purchaseService.PostAsync(purchase);

        MessageBox.Show("Документ приходу проведено та залишки оновлено.", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
        RequestClose?.Invoke(this, true);
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

    private bool CanSaveDraft() => Items.Count > 0 && Items.All(i => i.Quantity > 0);

    private bool CanPost() => !HasErrors && SelectedSupplier is not null && Items.Count > 0 && Items.All(i => i.Quantity > 0 && i.PurchasePrice >= 0);

    private Purchase BuildPurchase(DocumentStatus status) => new()
    {
        SupplierId = SelectedSupplier?.Id ?? Guid.Empty,
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
            line.PropertyChanged += ItemOnPropertyChanged;
            Items.Add(line);
        }
        else
        {
            existing.Quantity += 1;
        }

        OnItemsChanged();
    }

    private void ItemOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ReceiptLineItem.Quantity) or nameof(ReceiptLineItem.PurchasePrice) or nameof(ReceiptLineItem.HasErrors))
        {
            OnItemsChanged();
        }
    }

    private void OnItemsChanged()
    {
        OnPropertyChanged(nameof(Total));
        ValidateProperty(nameof(Items));
        RevalidateCommands();
    }

    private void RevalidateCommands()
    {
        SaveDraftCommand.RaiseCanExecuteChanged();
        PostCommand.RaiseCanExecuteChanged();
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

    protected override void ValidateProperty(string propertyName)
    {
        ClearErrors(propertyName);

        if (propertyName == nameof(Quantity) && Quantity <= 0)
        {
            AddError(nameof(Quantity), "Кількість має бути > 0");
        }

        if (propertyName == nameof(PurchasePrice) && PurchasePrice < 0)
        {
            AddError(nameof(PurchasePrice), "Ціна має бути >= 0");
        }
    }
}
