using System.Collections.ObjectModel;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Entities;
using SmokeTorg.Domain.Enums;
using SmokeTorg.Presentation.Services;
using SmokeTorg.Presentation.ViewModels.Dialogs;

namespace SmokeTorg.Presentation.ViewModels;

public class PurchasesViewModel : ViewModelBase
{
    private readonly PurchaseService _purchaseService;
    private readonly SupplierService _supplierService;
    private readonly GoodsReceiptViewModel _goodsReceiptViewModel;
    private readonly IDialogService _dialogService;

    private Purchase? _selected;
    private Supplier? _selectedFilterSupplier;
    private Supplier? _selectedCreateSupplier;
    private DocumentStatus? _selectedFilterStatus;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private string _numberSearch = string.Empty;

    public PurchasesViewModel(
        PurchaseService purchaseService,
        SupplierService supplierService,
        GoodsReceiptViewModel goodsReceiptViewModel,
        IDialogService dialogService)
    {
        _purchaseService = purchaseService;
        _supplierService = supplierService;
        _goodsReceiptViewModel = goodsReceiptViewModel;
        _dialogService = dialogService;

        RefreshCommand = new AsyncRelayCommand(async _ => await LoadAsync());
        CreateDraftCommand = new AsyncRelayCommand(async _ => await CreateDraftAsync(), _ => CanCreateInvoice);
        OpenSelectedCommand = new AsyncRelayCommand(async _ => await OpenSelectedAsync(), _ => CanOpenSelected);
        ApplyFiltersCommand = new RelayCommand(_ => ApplyFilters());
        ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
    }

    public ObservableCollection<Purchase> Purchases { get; } = [];
    public ObservableCollection<Purchase> FilteredPurchases { get; } = [];
    public ObservableCollection<Supplier> Suppliers { get; } = [];
    public ObservableCollection<DocumentStatus?> Statuses { get; } =
    [
        null,
        DocumentStatus.Draft,
        DocumentStatus.Posted,
        DocumentStatus.Cancelled
    ];

    public Purchase? Selected
    {
        get => _selected;
        set
        {
            if (SetProperty(ref _selected, value))
            {
                OnPropertyChanged(nameof(CanOpenSelected));
                OpenSelectedCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public Supplier? SelectedFilterSupplier { get => _selectedFilterSupplier; set => SetProperty(ref _selectedFilterSupplier, value); }
    public Supplier? SelectedCreateSupplier
    {
        get => _selectedCreateSupplier;
        set
        {
            if (SetProperty(ref _selectedCreateSupplier, value))
            {
                OnPropertyChanged(nameof(CanCreateInvoice));
                CreateDraftCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public DocumentStatus? SelectedFilterStatus { get => _selectedFilterStatus; set => SetProperty(ref _selectedFilterStatus, value); }

    public bool CanCreateInvoice => SelectedCreateSupplier is not null;
    public bool CanOpenSelected => Selected is not null;
    public DateTime? FromDate { get => _fromDate; set => SetProperty(ref _fromDate, value); }
    public DateTime? ToDate { get => _toDate; set => SetProperty(ref _toDate, value); }
    public string NumberSearch { get => _numberSearch; set => SetProperty(ref _numberSearch, value); }

    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand CreateDraftCommand { get; }
    public AsyncRelayCommand OpenSelectedCommand { get; }
    public RelayCommand ApplyFiltersCommand { get; }
    public RelayCommand ClearFiltersCommand { get; }

    public async Task LoadAsync()
    {
        Suppliers.Clear();
        foreach (var supplier in await _supplierService.GetAllAsync())
        {
            Suppliers.Add(supplier);
        }

        if (SelectedCreateSupplier is null)
        {
            SelectedCreateSupplier = Suppliers.FirstOrDefault();
        }

        Purchases.Clear();
        foreach (var purchase in await _purchaseService.GetAllAsync())
        {
            Purchases.Add(purchase);
        }

        ApplyFilters();
    }

    private async Task CreateDraftAsync()
    {
        if (SelectedCreateSupplier is null) return;

        var purchase = await _purchaseService.CreateDraftAsync(SelectedCreateSupplier.Id);
        await _goodsReceiptViewModel.InitializeForPurchaseAsync(purchase);
        if (_dialogService.ShowDialog(_goodsReceiptViewModel) == true)
        {
            await LoadAsync();
        }
    }

    private async Task OpenSelectedAsync()
    {
        if (Selected is null) return;

        var purchase = await _purchaseService.GetByIdAsync(Selected.Id);
        if (purchase is null) return;

        await _goodsReceiptViewModel.InitializeForPurchaseAsync(purchase);
        if (_dialogService.ShowDialog(_goodsReceiptViewModel) == true)
        {
            await LoadAsync();
        }
    }

    private void ApplyFilters()
    {
        var query = Purchases.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(NumberSearch))
        {
            query = query.Where(x => x.Number.Contains(NumberSearch.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (FromDate is not null)
        {
            query = query.Where(x => x.CreatedAt.Date >= FromDate.Value.Date);
        }

        if (ToDate is not null)
        {
            query = query.Where(x => x.CreatedAt.Date <= ToDate.Value.Date);
        }

        if (SelectedFilterSupplier is not null)
        {
            query = query.Where(x => x.SupplierId == SelectedFilterSupplier.Id);
        }

        if (SelectedFilterStatus is not null)
        {
            query = query.Where(x => x.Status == SelectedFilterStatus);
        }

        var filtered = query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Number)
            .ToList();

        FilteredPurchases.Clear();
        foreach (var purchase in filtered)
        {
            FilteredPurchases.Add(purchase);
        }
    }

    private void ClearFilters()
    {
        NumberSearch = string.Empty;
        FromDate = null;
        ToDate = null;
        SelectedFilterSupplier = null;
        SelectedFilterStatus = null;
        ApplyFilters();
    }
}
