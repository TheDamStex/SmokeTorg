using System.Collections.ObjectModel;
using System.Windows;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Entities;
using SmokeTorg.Presentation.Services;

namespace SmokeTorg.Presentation.ViewModels.Dialogs;

public class PosWindowViewModel : ViewModelBase, IDialogRequestClose
{
    private readonly ProductService _productService;
    private readonly SalesService _salesService;
    private readonly InventoryService _inventoryService;

    private string _searchText = string.Empty;
    private string _barcodeInput = string.Empty;
    private string _lastScannedBarcode = "-";
    private string _paymentType = "Нал";
    private decimal _received;
    private bool _hasBarcodeError;
    private Product? _selectedProduct;
    private CancellationTokenSource? _barcodeErrorCts;

    public PosWindowViewModel(
        ProductService productService,
        SalesService salesService,
        InventoryService inventoryService)
    {
        _productService = productService;
        _salesService = salesService;
        _inventoryService = inventoryService;

        SearchCommand = new AsyncRelayCommand(async _ => await SearchAsync());
        ScanBarcodeCommand = new AsyncRelayCommand(async _ => await ScanBarcodeAsync());
        AddItemCommand = new RelayCommand(AddItem);
        IncreaseCommand = new RelayCommand(IncreaseItem);
        DecreaseCommand = new RelayCommand(DecreaseItem);
        RemoveItemCommand = new RelayCommand(RemoveItem);
        PayCommand = new AsyncRelayCommand(async _ => await PayAsync(), _ => !HasErrors);
        CancelCheckCommand = new RelayCommand(_ => ClearCheck());
        CloseCommand = new RelayCommand(_ => RequestClose?.Invoke(this, false));

        CheckItems.CollectionChanged += (_, _) => OnCheckChanged();
        ValidateAll();
    }

    public ObservableCollection<Product> SearchResults { get; } = [];
    public ObservableCollection<PosLineItem> CheckItems { get; } = [];
    public IReadOnlyList<string> PaymentOptions { get; } = ["Нал", "Карта"];

    public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }
    public string BarcodeInput { get => _barcodeInput; set => SetProperty(ref _barcodeInput, value); }
    public string LastScannedBarcode { get => _lastScannedBarcode; set => SetProperty(ref _lastScannedBarcode, value); }
    public bool HasBarcodeError { get => _hasBarcodeError; set => SetProperty(ref _hasBarcodeError, value); }

    public string PaymentType
    {
        get => _paymentType;
        set
        {
            if (SetProperty(ref _paymentType, value))
            {
                if (PaymentType == "Карта")
                {
                    Received = Total;
                }

                ValidateProperty(nameof(Received));
            }
        }
    }

    public Product? SelectedProduct { get => _selectedProduct; set => SetProperty(ref _selectedProduct, value); }

    public decimal Received
    {
        get => _received;
        set
        {
            if (SetProperty(ref _received, value))
            {
                OnPropertyChanged(nameof(Change));
                ValidateProperty(nameof(Received));
            }
        }
    }

    public decimal Subtotal => CheckItems.Sum(x => x.Sum);
    public decimal Discount => 0;
    public decimal Total => Subtotal - Discount;
    public decimal Change => Received - Total;

    public AsyncRelayCommand SearchCommand { get; }
    public AsyncRelayCommand ScanBarcodeCommand { get; }
    public RelayCommand AddItemCommand { get; }
    public RelayCommand IncreaseCommand { get; }
    public RelayCommand DecreaseCommand { get; }
    public RelayCommand RemoveItemCommand { get; }
    public AsyncRelayCommand PayCommand { get; }
    public RelayCommand CancelCheckCommand { get; }
    public RelayCommand CloseCommand { get; }

    public event EventHandler<bool?>? RequestClose;

    protected override void ValidateProperty(string propertyName)
    {
        ClearErrors(propertyName);

        switch (propertyName)
        {
            case nameof(CheckItems):
                if (CheckItems.Count == 0)
                {
                    AddError(nameof(CheckItems), "Додайте хоча б одну позицію");
                }
                else if (CheckItems.Any(x => x.Quantity <= 0))
                {
                    AddError(nameof(CheckItems), "Кількість має бути > 0");
                }
                break;

            case nameof(Received):
                if (PaymentType == "Нал" && Received < Total)
                {
                    AddError(nameof(Received), "Отримано має бути не менше суми до оплати");
                }
                break;
        }
    }

    protected override void OnErrorsChanged(string propertyName) => PayCommand.RaiseCanExecuteChanged();

    private async Task SearchAsync()
    {
        SearchResults.Clear();
        foreach (var product in await _productService.SearchAsync(SearchText.Trim()))
        {
            SearchResults.Add(product);
        }
    }

    private async Task ScanBarcodeAsync()
    {
        var barcode = BarcodeInput.Trim();
        if (string.IsNullOrWhiteSpace(barcode)) return;

        var product = await _productService.FindByBarcode(barcode);
        if (product is null)
        {
            LastScannedBarcode = barcode;
            await ShowBarcodeErrorAsync();
            MessageBox.Show("Товар не знайдено за штрихкодом", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        AddProductToCheck(product);
        LastScannedBarcode = barcode;
        BarcodeInput = string.Empty;
        HasBarcodeError = false;
    }

    private void AddItem(object? parameter)
    {
        if (parameter is Product product)
        {
            AddProductToCheck(product);
        }
    }

    private void IncreaseItem(object? parameter)
    {
        if (parameter is PosLineItem item)
        {
            item.Quantity += 1;
            RaiseTotals();
        }
    }

    private void DecreaseItem(object? parameter)
    {
        if (parameter is not PosLineItem item)
        {
            return;
        }

        if (item.Quantity <= 1)
        {
            item.PropertyChanged -= OnLineChanged;
            CheckItems.Remove(item);
        }
        else
        {
            item.Quantity -= 1;
        }

        RaiseTotals();
    }

    private void RemoveItem(object? parameter)
    {
        if (parameter is PosLineItem item)
        {
            item.PropertyChanged -= OnLineChanged;
            CheckItems.Remove(item);
            RaiseTotals();
        }
    }

    private async Task PayAsync()
    {
        ValidateAll();
        if (HasErrors)
        {
            MessageBox.Show("Виправте помилки у формі.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var stock = await _inventoryService.GetStockAsync();
        foreach (var line in CheckItems)
        {
            var inStock = stock.FirstOrDefault(s => s.ProductId == line.ProductId)?.Quantity ?? 0;
            if (line.Quantity > inStock)
            {
                MessageBox.Show($"Недостатньо залишку для товару '{line.ProductName}'. Доступно: {inStock}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        var sale = new Sale
        {
            Date = DateTime.Now,
            Items = CheckItems.Select(x => new SaleItem
            {
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                BarcodeDisplay = x.Barcode,
                Quantity = x.Quantity,
                Price = x.Price,
                DiscountPercent = 0
            }).ToList(),
            PaidCash = PaymentType == "Нал" ? Received : 0,
            PaidCard = PaymentType == "Карта" ? Received : 0,
            DiscountPercent = 0
        };

        await _salesService.FinalizeSaleAsync(sale);
        MessageBox.Show("Оплату успішно проведено.", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
        ClearCheck();
    }

    private void AddProductToCheck(Product product)
    {
        var existing = CheckItems.FirstOrDefault(x => x.ProductId == product.Id);
        if (existing is null)
        {
            var line = new PosLineItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Barcode = product.Barcode,
                Quantity = 1,
                Price = product.SalePrice
            };
            line.PropertyChanged += OnLineChanged;
            CheckItems.Add(line);
        }
        else
        {
            existing.Quantity += 1;
        }

        RaiseTotals();
    }

    private void OnLineChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PosLineItem.Quantity) or nameof(PosLineItem.Price) or nameof(PosLineItem.HasErrors))
        {
            RaiseTotals();
        }
    }

    private async Task ShowBarcodeErrorAsync()
    {
        _barcodeErrorCts?.Cancel();
        _barcodeErrorCts = new CancellationTokenSource();

        HasBarcodeError = true;
        try
        {
            await Task.Delay(1500, _barcodeErrorCts.Token);
            HasBarcodeError = false;
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
    }

    private void ClearCheck()
    {
        foreach (var item in CheckItems)
        {
            item.PropertyChanged -= OnLineChanged;
        }

        CheckItems.Clear();
        Received = 0;
        BarcodeInput = string.Empty;
        RaiseTotals();
    }

    private void OnCheckChanged()
    {
        ValidateProperty(nameof(CheckItems));
        RaiseTotals();
    }

    private void RaiseTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(Change));

        if (PaymentType == "Карта")
        {
            Received = Total;
        }

        ValidateProperty(nameof(CheckItems));
        ValidateProperty(nameof(Received));
        PayCommand.RaiseCanExecuteChanged();
    }
}

public class PosLineItem : ViewModelBase
{
    private decimal _quantity = 1;
    private decimal _price;

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

    public decimal Price
    {
        get => _price;
        set
        {
            if (SetProperty(ref _price, value))
            {
                OnPropertyChanged(nameof(Sum));
            }
        }
    }

    public decimal Sum => Quantity * Price;

    protected override void ValidateProperty(string propertyName)
    {
        ClearErrors(propertyName);

        if (propertyName == nameof(Quantity) && Quantity <= 0)
        {
            AddError(nameof(Quantity), "Кількість має бути > 0");
        }

        if (propertyName == nameof(Price) && Price < 0)
        {
            AddError(nameof(Price), "Ціна має бути >= 0");
        }
    }
}
