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

    private Purchase? _purchase;
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

        SearchProductCommand = new AsyncRelayCommand(async _ => await SearchProductAsync(), _ => !IsReadOnly);
        AddByBarcodeCommand = new AsyncRelayCommand(async _ => await AddByBarcodeAsync(), _ => !IsReadOnly);
        AddProductCommand = new RelayCommand(AddProductFromParameter, _ => !IsReadOnly);
        RemoveItemCommand = new RelayCommand(RemoveItem, _ => !IsReadOnly);
        SaveDraftCommand = new AsyncRelayCommand(async _ => await SaveDraftAsync(), _ => CanSaveDraft());
        PostCommand = new AsyncRelayCommand(async _ => await PostAsync(), _ => CanPost());
        CancelReceiptCommand = new AsyncRelayCommand(async _ => await CancelReceiptAsync(), _ => CanCancel());
        CloseCommand = new RelayCommand(_ => RequestClose?.Invoke(this, false));
        AddSupplierCommand = new AsyncRelayCommand(OpenCreateSupplierDialogAsync, _ => !IsReadOnly);

        Items.CollectionChanged += (_, _) => OnItemsChanged();
    }

    public ObservableCollection<Supplier> Suppliers { get; } = [];
    public ObservableCollection<ReceiptLineItem> Items { get; } = [];
    public ObservableCollection<Product> ProductSearchResults { get; } = [];

    public string Number => _purchase?.Number ?? string.Empty;
    public DateTime ReceiptDate => _purchase?.ReceiptDate ?? DateTime.Now;
    public string CreatedByLogin => _purchase?.CreatedByLogin ?? string.Empty;
    public string AcceptedByLogin => _purchase?.AcceptedByLogin ?? string.Empty;
    public DocumentStatus Status => _purchase?.Status ?? DocumentStatus.Draft;
    public bool IsReadOnly => Status is DocumentStatus.Posted or DocumentStatus.Cancelled;

    public Supplier? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            if (SetProperty(ref _selectedSupplier, value) && _purchase is not null && value is not null)
            {
                _purchase.SupplierId = value.Id;
                _purchase.SupplierName = value.Name;
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
    public AsyncRelayCommand CancelReceiptCommand { get; }
    public RelayCommand CloseCommand { get; }
    public AsyncRelayCommand AddSupplierCommand { get; }

    public event EventHandler<bool?>? RequestClose;

    public async Task InitializeForPurchaseAsync(Purchase purchase)
    {
        _purchase = purchase;
        await ReloadSuppliers();
        SelectedSupplier = Suppliers.FirstOrDefault(x => x.Id == purchase.SupplierId);

        Items.Clear();
        foreach (var item in purchase.Items)
        {
            var line = new ReceiptLineItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Barcode = item.BarcodeDisplay,
                Quantity = item.Quantity,
                PurchasePrice = item.Price
            };
            line.PropertyChanged += ItemOnPropertyChanged;
            Items.Add(line);
        }

        OnPropertyChanged(nameof(Number));
        OnPropertyChanged(nameof(ReceiptDate));
        OnPropertyChanged(nameof(CreatedByLogin));
        OnPropertyChanged(nameof(AcceptedByLogin));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(IsReadOnly));
        OnItemsChanged();
    }

    private async Task ReloadSuppliers()
    {
        Suppliers.Clear();
        foreach (var supplier in await _supplierService.GetAllAsync())
        {
            Suppliers.Add(supplier);
        }
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
        if (_purchase is null) return;
        ApplyItemsToPurchase();
        await _purchaseService.SaveDraftAsync(_purchase);
        MessageBox.Show("Чернетку накладної збережено.", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
        RequestClose?.Invoke(this, true);
    }

    private async Task PostAsync()
    {
        if (_purchase is null) return;
        ApplyItemsToPurchase();
        await _purchaseService.SaveDraftAsync(_purchase);
        await _purchaseService.PostAsync(_purchase.Id);

        MessageBox.Show("Накладну проведено та залишки оновлено.", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
        RequestClose?.Invoke(this, true);
    }

    private async Task CancelReceiptAsync()
    {
        if (_purchase is null) return;
        await _purchaseService.CancelAsync(_purchase.Id);
        MessageBox.Show("Накладну скасовано.", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
        RequestClose?.Invoke(this, true);
    }

    private async Task OpenCreateSupplierDialogAsync(object? _)
    {
        var vm = new SupplierCreateViewModel(_supplierService);
        if (_dialogService.ShowDialog(vm) == true && vm.CreatedSupplier is not null)
        {
            await ReloadSuppliers();
            SelectedSupplier = Suppliers.FirstOrDefault(x => x.Id == vm.CreatedSupplier.Id);
        }
    }

    private bool CanSaveDraft() => !IsReadOnly && _purchase is not null && SelectedSupplier is not null && Items.Count > 0 && Items.All(i => i.Quantity > 0);

    private bool CanPost() => !IsReadOnly && _purchase is not null && SelectedSupplier is not null && Items.Count > 0 && Items.All(i => i.Quantity > 0 && i.PurchasePrice >= 0);

    private bool CanCancel() => _purchase is not null && Status == DocumentStatus.Draft;

    private void ApplyItemsToPurchase()
    {
        if (_purchase is null || SelectedSupplier is null) return;

        _purchase.SupplierId = SelectedSupplier.Id;
        _purchase.SupplierName = SelectedSupplier.Name;
        _purchase.ReceiptDate = DateTime.Now;
        _purchase.Items = Items.Select(i => new PurchaseItem
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            BarcodeDisplay = i.Barcode,
            Quantity = i.Quantity,
            Price = i.PurchasePrice
        }).ToList();
    }

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
        RevalidateCommands();
    }

    private void RevalidateCommands()
    {
        SearchProductCommand.RaiseCanExecuteChanged();
        AddByBarcodeCommand.RaiseCanExecuteChanged();
        AddProductCommand.RaiseCanExecuteChanged();
        RemoveItemCommand.RaiseCanExecuteChanged();
        SaveDraftCommand.RaiseCanExecuteChanged();
        PostCommand.RaiseCanExecuteChanged();
        CancelReceiptCommand.RaiseCanExecuteChanged();
        AddSupplierCommand.RaiseCanExecuteChanged();
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
