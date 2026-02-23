using System.Collections.ObjectModel;
using System.Collections.Specialized;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Entities;

namespace SmokeTorg.Presentation.ViewModels;

public class PosViewModel : ViewModelBase
{
    private readonly ProductService _productService;
    private readonly SalesService _salesService;

    private string _searchText = string.Empty;
    private decimal _paidCash;
    private decimal _paidCard;

    private readonly AsyncRelayCommand _searchCommand;
    private readonly RelayCommand _addToCartCommand;
    private readonly RelayCommand _removeLineCommand;
    private readonly AsyncRelayCommand _finalizeCommand;

    public PosViewModel(ProductService productService, SalesService salesService)
    {
        _productService = productService;
        _salesService = salesService;

        _searchCommand = new AsyncRelayCommand(async _ =>
        {
            SearchResults.Clear();
            foreach (var p in await _productService.SearchAsync(SearchText)) SearchResults.Add(p);
        });

        _addToCartCommand = new RelayCommand(p =>
        {
            if (p is not Product product) return;
            var line = Cart.FirstOrDefault(i => i.ProductId == product.Id);
            if (line is null) Cart.Add(new SaleItem { ProductId = product.Id, ProductName = product.Name, Quantity = 1, Price = product.SalePrice });
            else line.Quantity++;
            RaiseCartTotalsChanged();
        });

        _removeLineCommand = new RelayCommand(p =>
        {
            if (p is SaleItem line) Cart.Remove(line);
            RaiseCartTotalsChanged();
        });

        _finalizeCommand = new AsyncRelayCommand(async _ =>
        {
            var sale = new Sale { Items = Cart.ToList(), PaidCash = PaidCash, PaidCard = PaidCard, DiscountPercent = 0 };
            await _salesService.FinalizeSaleAsync(sale);
            Cart.Clear();
            PaidCash = PaidCard = 0;
            RaiseCartTotalsChanged();
        }, _ => Cart.Any());

        Cart.CollectionChanged += CartOnCollectionChanged;
    }

    public ObservableCollection<Product> SearchResults { get; } = [];
    public ObservableCollection<SaleItem> Cart { get; } = [];

    public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }
    public decimal PaidCash { get => _paidCash; set { if (SetProperty(ref _paidCash, value)) OnPropertyChanged(nameof(Change)); } }
    public decimal PaidCard { get => _paidCard; set { if (SetProperty(ref _paidCard, value)) OnPropertyChanged(nameof(Change)); } }

    public decimal Total => Cart.Sum(i => i.Price * i.Quantity * (1 - i.DiscountPercent / 100m));
    public decimal Tax => Total * 0.2m;
    public decimal Change => PaidCash + PaidCard - Total;

    public AsyncRelayCommand SearchCommand => _searchCommand;
    public RelayCommand AddToCartCommand => _addToCartCommand;
    public RelayCommand RemoveLineCommand => _removeLineCommand;
    public AsyncRelayCommand FinalizeCommand => _finalizeCommand;

    private void CartOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _finalizeCommand.RaiseCanExecuteChanged();
    }

    private void RaiseCartTotalsChanged()
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(Tax));
        OnPropertyChanged(nameof(Change));
    }
}
