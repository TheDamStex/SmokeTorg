using System.Collections.ObjectModel;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Entities;
using SmokeTorg.Domain.Enums;

namespace SmokeTorg.Presentation.ViewModels;

public class PurchasesViewModel(PurchaseService purchaseService, ProductService productService) : ViewModelBase
{
    private Purchase? _selected;

    public ObservableCollection<Purchase> Purchases { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];
    public Purchase Current { get; set; } = new();

    public Purchase? Selected { get => _selected; set => SetProperty(ref _selected, value); }

    public AsyncRelayCommand RefreshCommand => new(async _ => await LoadAsync());
    public RelayCommand AddCommand => new(_ => { Current = new Purchase(); OnPropertyChanged(nameof(Current)); });
    public RelayCommand AddLineCommand => new(p =>
    {
        if (p is Product pr)
            Current.Items.Add(new PurchaseItem { ProductId = pr.Id, ProductName = pr.Name, BarcodeDisplay = pr.Barcode, Quantity = 1, Price = pr.PurchasePrice });
        OnPropertyChanged(nameof(Current));
    });

    public AsyncRelayCommand SaveCommand => new(async _ => { await purchaseService.SaveAsync(Current); await LoadAsync(); });
    public AsyncRelayCommand PostCommand => new(async _ => { if (Selected is not null && Selected.Status == DocumentStatus.Draft) await purchaseService.PostAsync(Selected); await LoadAsync(); });

    public async Task LoadAsync()
    {
        Purchases.Clear();
        foreach (var p in await purchaseService.GetAllAsync()) Purchases.Add(p);

        Products.Clear();
        foreach (var p in await productService.GetAllAsync()) Products.Add(p);

        var barcodeMap = Products.ToDictionary(x => x.Id, x => x.Barcode);
        foreach (var purchase in Purchases)
        {
            foreach (var item in purchase.Items)
            {
                if (string.IsNullOrWhiteSpace(item.BarcodeDisplay) && barcodeMap.TryGetValue(item.ProductId, out var barcode))
                {
                    item.BarcodeDisplay = barcode;
                }
            }
        }
    }
}
