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
    private string _formValidationMessage = string.Empty;

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
        SaveDraftCommand = new AsyncRelayCommand(async _ => await SaveDraftAsync(), _ => CanSaveDraft);
        PostCommand = new AsyncRelayCommand(async _ => await PostAsync(), _ => CanPost);
        CancelReceiptCommand = new AsyncRelayCommand(async _ => await CancelReceiptAsync(), _ => CanCancel);
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
            if (!SetProperty(ref _selectedSupplier, value))
            {
                return;
            }

            if (_purchase is not null)
            {
                _purchase.SupplierId = value?.Id ?? Guid.Empty;
                _purchase.SupplierName = value?.Name ?? string.Empty;
            }

            RevalidateCommands();
        }
    }

    public string ProductSearchText { get => _productSearchText; set => SetProperty(ref _productSearchText, value); }
    public string BarcodeInput { get => _barcodeInput; set => SetProperty(ref _barcodeInput, value); }
    public Product? SelectedSearchProduct { get => _selectedSearchProduct; set => SetProperty(ref _selectedSearchProduct, value); }
    public string FormValidationMessage
    {
        get => _formValidationMessage;
        private set => SetProperty(ref _formValidationMessage, value);
    }

    public bool CanSaveDraft => !IsReadOnly && _purchase is not null;
    public bool CanPost => !IsReadOnly
                           && _purchase is not null
                           && SelectedSupplier is not null
                           && Items.Count > 0
                           && Items.All(i => !i.HasErrors && i.ProductId != Guid.Empty);

    public bool CanCancel => _purchase is not null && Status == DocumentStatus.Draft;

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
        ValidateAll();
        OnItemsChanged();

        if (HasErrors)
        {
            FormValidationMessage = "Документ містить некоректні дані. Перевірте реквізити накладної та позиції.";
            MessageBox.Show(FormValidationMessage, "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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

        if (!ValidateStateForSave())
        {
            MessageBox.Show("Неможливо зберегти чернетку. Перевірте реквізити накладної.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ApplyItemsToPurchase();
        await _purchaseService.SaveDraftAsync(_purchase);
        MessageBox.Show("Чернетку накладної збережено.", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
        RequestClose?.Invoke(this, true);
    }

    private async Task PostAsync()
    {
        if (_purchase is null) return;

        if (!ValidateStateForPost())
        {
            MessageBox.Show("Неможливо провести накладну. Перевірте постачальника та позиції.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

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
        if (product.Id == Guid.Empty)
        {
            MessageBox.Show("Неможливо додати позицію без товару.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

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
        ValidateProperty(nameof(Items));
        OnPropertyChanged(nameof(Total));
        RevalidateCommands();
    }

    private void RevalidateCommands()
    {
        OnPropertyChanged(nameof(CanSaveDraft));
        OnPropertyChanged(nameof(CanPost));
        OnPropertyChanged(nameof(CanCancel));
        SearchProductCommand.RaiseCanExecuteChanged();
        AddByBarcodeCommand.RaiseCanExecuteChanged();
        AddProductCommand.RaiseCanExecuteChanged();
        RemoveItemCommand.RaiseCanExecuteChanged();
        SaveDraftCommand.RaiseCanExecuteChanged();
        PostCommand.RaiseCanExecuteChanged();
        CancelReceiptCommand.RaiseCanExecuteChanged();
        AddSupplierCommand.RaiseCanExecuteChanged();
    }

    protected override void ValidateProperty(string propertyName)
    {
        ClearErrors(propertyName);

        switch (propertyName)
        {
            case nameof(SelectedSupplier):
                if (SelectedSupplier is null)
                {
                    AddError(nameof(SelectedSupplier), "Оберіть постачальника.");
                }
                break;
            case nameof(Number):
                if (string.IsNullOrWhiteSpace(Number))
                {
                    AddError(nameof(Number), "Номер накладної відсутній.");
                }
                break;
            case nameof(ReceiptDate):
                if (ReceiptDate == default)
                {
                    AddError(nameof(ReceiptDate), "Дата накладної відсутня.");
                }
                break;
            case nameof(Items):
                if (Items.Count == 0)
                {
                    AddError(nameof(Items), "Додайте хоча б одну позицію для проведення.");
                }

                if (Items.Any(i => i.HasErrors || i.ProductId == Guid.Empty))
                {
                    AddError(nameof(Items), "У позиціях є помилки. Виправте кількість, ціну та товар.");
                }
                break;
            case nameof(Status):
                if (Status == DocumentStatus.Posted)
                {
                    AddError(nameof(Status), "Проведену накладну не можна провести повторно.");
                }
                else if (Status == DocumentStatus.Cancelled)
                {
                    AddError(nameof(Status), "Скасовану накладну не можна редагувати або проводити.");
                }
                break;
        }

        UpdateFormValidationMessage();
    }

    private bool ValidateStateForSave()
    {
        ValidateProperty(nameof(Number));
        ValidateProperty(nameof(ReceiptDate));
        ValidateProperty(nameof(SelectedSupplier));
        ValidateProperty(nameof(Status));
        return !GetErrors(nameof(Number)).Cast<string>().Any()
               && !GetErrors(nameof(ReceiptDate)).Cast<string>().Any()
               && !GetErrors(nameof(SelectedSupplier)).Cast<string>().Any()
               && !GetErrors(nameof(Status)).Cast<string>().Any();
    }

    private bool ValidateStateForPost()
    {
        ValidateStateForSave();
        foreach (var item in Items)
        {
            item.ValidateAllProperties();
        }

        ValidateProperty(nameof(Items));
        return !HasErrors;
    }

    private void UpdateFormValidationMessage()
    {
        FormValidationMessage = HasErrors
            ? "Виправте помилки перед проведенням накладної."
            : string.Empty;
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

    public bool ValidateAllProperties() => ValidateAll();

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

        if (propertyName == nameof(ProductId) && ProductId == Guid.Empty)
        {
            AddError(nameof(ProductId), "Товар обов'язковий.");
        }
    }
}
