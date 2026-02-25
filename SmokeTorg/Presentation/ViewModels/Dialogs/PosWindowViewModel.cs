using System.Collections.ObjectModel;
using System.Windows;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Entities;
using SmokeTorg.Presentation.Services;

namespace SmokeTorg.Presentation.ViewModels.Dialogs;

public class PosWindowViewModel(
    ProductService productService,
    SalesService salesService,
    InventoryService inventoryService) : ViewModelBase, IDialogRequestClose
{
    private string _searchText = string.Empty;
    private string _barcodeInput = string.Empty;
    private string _lastScannedBarcode = "-";
    private string _paymentType = "Нал";
    private decimal _received;
    private bool _hasBarcodeError;
    private Product? _selectedProduct;
    private CancellationTokenSource? _barcodeErrorCts;

    public ObservableCollection<Product> SearchResults { get; } = [];
    public ObservableCollection<PosLineItem> CheckItems { get; } = [];
    public IReadOnlyList<string> PaymentOptions { get; } = ["Нал", "Карта"];

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string BarcodeInput
    {
        get => _barcodeInput;
        set => SetProperty(ref _barcodeInput, value);
    }

    public string LastScannedBarcode
    {
        get => _lastScannedBarcode;
        set => SetProperty(ref _lastScannedBarcode, value);
    }

    public bool HasBarcodeError
    {
        get => _hasBarcodeError;
        set => SetProperty(ref _hasBarcodeError, value);
    }

    public string PaymentType
    {
        get => _paymentType;
        set => SetProperty(ref _paymentType, value);
    }

    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set => SetProperty(ref _selectedProduct, value);
    }

    public decimal Received
    {
        get => _received;
        set
        {
            if (SetProperty(ref _received, value))
            {
                OnPropertyChanged(nameof(Change));
            }
        }
    }

    public decimal Subtotal => CheckItems.Sum(x => x.Sum);
    public decimal Discount => 0;
    public decimal Total => Subtotal - Discount;
    public decimal Change => Received - Total;

    public event EventHandler<bool?>? RequestClose;

    public AsyncRelayCommand SearchCommand => new(async _ =>
    {
        SearchResults.Clear();
        foreach (var product in await productService.SearchAsync(SearchText.Trim()))
        {
            SearchResults.Add(product);
        }
    });

    public AsyncRelayCommand ScanBarcodeCommand => new(async _ =>
    {
        var barcode = BarcodeInput.Trim();
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return;
        }

        var product = await productService.FindByBarcode(barcode);
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
    });

    public RelayCommand AddItemCommand => new(p =>
    {
        if (p is Product product)
        {
            AddProductToCheck(product);
        }
    });

    public RelayCommand IncreaseCommand => new(p =>
    {
        if (p is PosLineItem item)
        {
            item.Quantity += 1;
            RaiseTotals();
        }
    });

    public RelayCommand DecreaseCommand => new(p =>
    {
        if (p is not PosLineItem item)
        {
            return;
        }

        if (item.Quantity <= 1)
        {
            CheckItems.Remove(item);
        }
        else
        {
            item.Quantity -= 1;
        }

        RaiseTotals();
    });

    public RelayCommand RemoveItemCommand => new(p =>
    {
        if (p is PosLineItem item)
        {
            CheckItems.Remove(item);
            RaiseTotals();
        }
    });

    public AsyncRelayCommand PayCommand => new(async _ =>
    {
        if (CheckItems.Count == 0)
        {
            MessageBox.Show("Чек порожній.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (Received < Total)
        {
            MessageBox.Show("Недостатньо коштів для оплати.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var stock = await inventoryService.GetStockAsync();
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

        await salesService.FinalizeSaleAsync(sale);
        MessageBox.Show("Оплату успішно проведено.", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
        ClearCheck();
    });

    public RelayCommand CancelCheckCommand => new(_ => ClearCheck());
    public RelayCommand CloseCommand => new(_ => RequestClose?.Invoke(this, false));

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
            line.PropertyChanged += (_, _) => RaiseTotals();
            CheckItems.Add(line);
        }
        else
        {
            existing.Quantity += 1;
        }

        RaiseTotals();
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
        CheckItems.Clear();
        Received = 0;
        BarcodeInput = string.Empty;
        RaiseTotals();
    }

    private void RaiseTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(Change));
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
}
