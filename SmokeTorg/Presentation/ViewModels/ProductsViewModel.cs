using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Entities;

namespace SmokeTorg.Presentation.ViewModels;

public class ProductsViewModel : ViewModelBase
{
    private static readonly Regex BarcodePattern = new("^[0-9+]+$", RegexOptions.Compiled);

    private readonly ProductService _productService;

    private string _searchText = string.Empty;
    private Product? _selected;

    private Guid _editingId;
    private long _editingVersion;
    private bool _editingIsActive = true;

    private string _name = string.Empty;
    private string _sku = string.Empty;
    private string _barcode = string.Empty;
    private decimal _purchasePrice;
    private decimal _salePrice;
    private decimal _minStock;

    public ProductsViewModel(ProductService productService)
    {
        _productService = productService;

        RefreshCommand = new AsyncRelayCommand(async _ => await LoadAsync());
        SearchCommand = new AsyncRelayCommand(async _ => await SearchAsync());
        AddCommand = new RelayCommand(_ => BeginNew());
        EditCommand = new RelayCommand(_ => BeginEdit());
        SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync(), _ => !HasErrors);
        DeleteCommand = new AsyncRelayCommand(async _ => await DeleteAsync(), _ => Selected is not null);

        BeginNew();
    }

    public ObservableCollection<Product> Items { get; } = [];

    public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }

    public Product? Selected
    {
        get => _selected;
        set
        {
            if (SetProperty(ref _selected, value))
            {
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Sku { get => _sku; set => SetProperty(ref _sku, value); }
    public string Barcode { get => _barcode; set => SetProperty(ref _barcode, value); }
    public decimal PurchasePrice { get => _purchasePrice; set => SetProperty(ref _purchasePrice, value); }
    public decimal SalePrice { get => _salePrice; set => SetProperty(ref _salePrice, value); }
    public decimal MinStock { get => _minStock; set => SetProperty(ref _minStock, value); }

    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand SearchCommand { get; }
    public RelayCommand AddCommand { get; }
    public RelayCommand EditCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand DeleteCommand { get; }

    protected override void ValidateProperty(string propertyName)
    {
        ClearErrors(propertyName);

        switch (propertyName)
        {
            case nameof(Name):
                if (string.IsNullOrWhiteSpace(Name)) AddError(nameof(Name), "Поле обов’язкове");
                else if (Name.Trim().Length < 2) AddError(nameof(Name), "Мінімум 2 символи");
                break;
            case nameof(Barcode):
                if (string.IsNullOrWhiteSpace(Barcode)) AddError(nameof(Barcode), "Поле обов’язкове");
                else if (!BarcodePattern.IsMatch(Barcode.Trim())) AddError(nameof(Barcode), "Дозволені лише цифри та '+'");
                break;
            case nameof(PurchasePrice):
                if (PurchasePrice < 0) AddError(nameof(PurchasePrice), "Ціна має бути >= 0");
                break;
            case nameof(SalePrice):
                if (SalePrice < 0) AddError(nameof(SalePrice), "Ціна має бути >= 0");
                break;
            case nameof(MinStock):
                if (MinStock < 0) AddError(nameof(MinStock), "Мінімальний залишок має бути >= 0");
                break;
        }
    }

    protected override void OnErrorsChanged(string propertyName) => SaveCommand.RaiseCanExecuteChanged();

    public async Task LoadAsync()
    {
        Items.Clear();
        foreach (var p in await _productService.GetAllAsync()) Items.Add(p);
        DeleteCommand.RaiseCanExecuteChanged();
    }

    public async Task SearchAsync()
    {
        Items.Clear();
        foreach (var p in await _productService.SearchAsync(SearchText)) Items.Add(p);
    }

    private void BeginNew()
    {
        _editingId = Guid.Empty;
        _editingVersion = 0;
        _editingIsActive = true;
        Name = string.Empty;
        Sku = string.Empty;
        Barcode = string.Empty;
        PurchasePrice = 0;
        SalePrice = 0;
        MinStock = 0;
        ValidateAll();
    }

    private void BeginEdit()
    {
        if (Selected is null) return;

        _editingId = Selected.Id;
        _editingVersion = Selected.Version;
        _editingIsActive = Selected.IsActive;
        Name = Selected.Name;
        Sku = Selected.Sku;
        Barcode = Selected.Barcode;
        PurchasePrice = Selected.PurchasePrice;
        SalePrice = Selected.SalePrice;
        MinStock = Selected.MinStock;
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
            IsActive = _editingIsActive,
            Name = Name.Trim(),
            Sku = Sku.Trim(),
            Barcode = Barcode.Trim(),
            PurchasePrice = PurchasePrice,
            SalePrice = SalePrice,
            MinStock = MinStock
        };

        try
        {
            await _productService.SaveAsync(product);
            await LoadAsync();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task DeleteAsync()
    {
        if (Selected is null) return;
        await _productService.DeleteAsync(Selected.Id);
        await LoadAsync();
    }

    private async Task ValidateBarcodeUniquenessAsync()
    {
        ValidateProperty(nameof(Barcode));
        if (GetErrors(nameof(Barcode)).Cast<string>().Any()) return;

        var existing = await _productService.FindByBarcode(Barcode.Trim());
        if (existing is not null && existing.Id != _editingId)
        {
            AddError(nameof(Barcode), "Штрихкод вже використовується");
        }
    }
}
