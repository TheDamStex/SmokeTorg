using System.Collections.ObjectModel;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Entities;

namespace SmokeTorg.Presentation.ViewModels;

public class ProductsViewModel(ProductService productService) : ViewModelBase
{
    private string _searchText = string.Empty;
    private Product? _selected;

    public ObservableCollection<Product> Items { get; } = [];
    public Product Editable { get; set; } = new();

    public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }
    public Product? Selected { get => _selected; set => SetProperty(ref _selected, value); }

    public AsyncRelayCommand RefreshCommand => new(async _ => await LoadAsync());
    public AsyncRelayCommand SearchCommand => new(async _ => await SearchAsync());
    public RelayCommand AddCommand => new(_ => Editable = new Product());
    public RelayCommand EditCommand => new(_ => { if (Selected is not null) Editable = new Product
    {
        Id = Selected.Id, Name = Selected.Name, Sku = Selected.Sku, Barcode = Selected.Barcode,
        CategoryId = Selected.CategoryId, Unit = Selected.Unit, PurchasePrice = Selected.PurchasePrice,
        SalePrice = Selected.SalePrice, TaxGroup = Selected.TaxGroup, IsActive = Selected.IsActive,
        MinStock = Selected.MinStock, Version = Selected.Version
    }; OnPropertyChanged(nameof(Editable)); });
    public AsyncRelayCommand SaveCommand => new(async _ => { await productService.SaveAsync(Editable); await LoadAsync(); });
    public AsyncRelayCommand DeleteCommand => new(async _ => { if (Selected is not null) { await productService.DeleteAsync(Selected.Id); await LoadAsync(); } });

    public async Task LoadAsync()
    {
        Items.Clear();
        foreach (var p in await productService.GetAllAsync()) Items.Add(p);
    }

    public async Task SearchAsync()
    {
        Items.Clear();
        foreach (var p in await productService.SearchAsync(SearchText)) Items.Add(p);
    }
}
