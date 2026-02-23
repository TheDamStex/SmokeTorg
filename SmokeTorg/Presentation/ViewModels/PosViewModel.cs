using System.Collections.ObjectModel;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Entities;

namespace SmokeTorg.Presentation.ViewModels;

public class PosViewModel(ProductService productService, SalesService salesService) : ViewModelBase
{
    private string _searchText = string.Empty;
    private decimal _paidCash;
    private decimal _paidCard;

    public ObservableCollection<Product> SearchResults { get; } = [];
    public ObservableCollection<SaleItem> Cart { get; } = [];

    public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }
    public decimal PaidCash { get => _paidCash; set { if (SetProperty(ref _paidCash, value)) OnPropertyChanged(nameof(Change)); } }
    public decimal PaidCard { get => _paidCard; set { if (SetProperty(ref _paidCard, value)) OnPropertyChanged(nameof(Change)); } }

    public decimal Total => Cart.Sum(i => i.Price * i.Quantity * (1 - i.DiscountPercent / 100m));
    public decimal Tax => Total * 0.2m;
    public decimal Change => PaidCash + PaidCard - Total;

    public AsyncRelayCommand SearchCommand => new(async _ =>
    {
        SearchResults.Clear();
        foreach (var p in await productService.SearchAsync(SearchText)) SearchResults.Add(p);
    });

    public RelayCommand AddToCartCommand => new(p =>
    {
        if (p is not Product product) return;
        var line = Cart.FirstOrDefault(i => i.ProductId == product.Id);
        if (line is null) Cart.Add(new SaleItem { ProductId = product.Id, ProductName = product.Name, Quantity = 1, Price = product.SalePrice });
        else line.Quantity++;
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(Tax));
        OnPropertyChanged(nameof(Change));
    });

    public RelayCommand RemoveLineCommand => new(p =>
    {
        if (p is SaleItem line) Cart.Remove(line);
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(Tax));
        OnPropertyChanged(nameof(Change));
    });

    public AsyncRelayCommand FinalizeCommand => new(async _ =>
    {
        var sale = new Sale { Items = Cart.ToList(), PaidCash = PaidCash, PaidCard = PaidCard, DiscountPercent = 0 };
        await salesService.FinalizeSaleAsync(sale);
        Cart.Clear();
        PaidCash = PaidCard = 0;
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(Tax));
        OnPropertyChanged(nameof(Change));
    }, _ => Cart.Any());
}
