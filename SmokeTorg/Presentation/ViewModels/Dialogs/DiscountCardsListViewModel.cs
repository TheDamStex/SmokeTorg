using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using SmokeTorg.Application.Interfaces;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Entities;
using SmokeTorg.Presentation.Services;
using System.ComponentModel;

namespace SmokeTorg.Presentation.ViewModels.Dialogs;

public class DiscountCardsListViewModel : ViewModelBase, IDialogRequestClose
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IDialogService _dialogService;
    private readonly ClientCardViewModel _clientCardViewModel;
    private string _searchText = string.Empty;
    private Customer? _selectedCard;

    public DiscountCardsListViewModel(
        ICustomerRepository customerRepository,
        IDialogService dialogService,
        ClientCardViewModel clientCardViewModel)
    {
        _customerRepository = customerRepository;
        _dialogService = dialogService;
        _clientCardViewModel = clientCardViewModel;

        CardsView = CollectionViewSource.GetDefaultView(Cards);
        CardsView.Filter = FilterCard;

        AddCommand = new AsyncRelayCommand(async _ => await AddAsync());
        EditCommand = new AsyncRelayCommand(async _ => await EditAsync(), _ => SelectedCard is not null);
        DeleteCommand = new AsyncRelayCommand(async _ => await DeleteAsync(), _ => SelectedCard is not null);
        SelectCommand = new RelayCommand(_ => RequestClose?.Invoke(this, true), _ => SelectedCard is not null);
        CloseCommand = new RelayCommand(_ => RequestClose?.Invoke(this, false));
    }

    public ObservableCollection<Customer> Cards { get; } = [];
    public ICollectionView CardsView { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                CardsView.Refresh();
            }
        }
    }

    public Customer? SelectedCard
    {
        get => _selectedCard;
        set
        {
            if (SetProperty(ref _selectedCard, value))
            {
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                SelectCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public AsyncRelayCommand AddCommand { get; }
    public AsyncRelayCommand EditCommand { get; }
    public AsyncRelayCommand DeleteCommand { get; }
    public RelayCommand SelectCommand { get; }
    public RelayCommand CloseCommand { get; }

    public event EventHandler<bool?>? RequestClose;

    public async Task LoadAsync()
    {
        var cards = await _customerRepository.GetAllAsync();
        var selectedId = SelectedCard?.Id;

        Cards.Clear();
        foreach (var customer in cards.Where(c => !string.IsNullOrWhiteSpace(c.DiscountCardNumber)).OrderBy(c => c.DiscountCardNumber))
        {
            Cards.Add(customer);
        }

        SelectedCard = selectedId.HasValue
            ? Cards.FirstOrDefault(x => x.Id == selectedId.Value)
            : null;

        CardsView.Refresh();
    }

    private bool FilterCard(object obj)
    {
        if (obj is not Customer card)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        var search = SearchText.Trim();
        return card.DiscountCardNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
               || card.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
               || card.Phone.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private async Task AddAsync()
    {
        _clientCardViewModel.Load(null);
        var result = _dialogService.ShowDialog(_clientCardViewModel);
        if (result != true)
        {
            return;
        }

        var cardNumber = _clientCardViewModel.DiscountCardNumber.Trim();
        await LoadAsync();
        SelectedCard = Cards.FirstOrDefault(x => x.DiscountCardNumber.Equals(cardNumber, StringComparison.OrdinalIgnoreCase));
    }

    private async Task EditAsync()
    {
        if (SelectedCard is null)
        {
            return;
        }

        var selectedId = SelectedCard.Id;
        _clientCardViewModel.Load(SelectedCard);
        var result = _dialogService.ShowDialog(_clientCardViewModel);
        if (result != true)
        {
            return;
        }

        await LoadAsync();
        SelectedCard = Cards.FirstOrDefault(x => x.Id == selectedId);
    }

    private async Task DeleteAsync()
    {
        if (SelectedCard is null)
        {
            return;
        }

        if (MessageBox.Show("Видалити дисконтну картку клієнта?", "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        var id = SelectedCard.Id;
        await _customerRepository.DeleteAsync(id);
        await LoadAsync();
    }
}
