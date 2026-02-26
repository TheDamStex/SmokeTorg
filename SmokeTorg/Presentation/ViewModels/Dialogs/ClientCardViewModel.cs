using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using SmokeTorg.Application.Interfaces;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Entities;
using SmokeTorg.Presentation.Services;

namespace SmokeTorg.Presentation.ViewModels.Dialogs;

public class ClientCardViewModel : ViewModelBase, IDialogRequestClose
{
    private static readonly Regex EmailRegex = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"^[0-9\+\-\(\)\s]{7,20}$", RegexOptions.Compiled);

    private readonly ICustomerRepository _customerRepository;

    private Guid _customerId;
    private bool _isExistingCustomer;
    private string _customerName = string.Empty;
    private string _lastName = string.Empty;
    private string _firstName = string.Empty;
    private string _middleName = string.Empty;
    private string _phone = string.Empty;
    private string _email = string.Empty;
    private string _discountCardNumber = string.Empty;
    private string _selectedRegion = string.Empty;
    private string _selectedDiscountType = "Бонусна система";
    private string _selectedCardType = "Бонус";
    private decimal _initialDiscountAmount;
    private decimal _discountPercent;
    private decimal _bonusBalance;
    private bool _isVip;
    private bool _isBlocked;
    private string _selectedCountry = "Україна";
    private string _selectedCity = string.Empty;
    private string _selectedArea = string.Empty;
    private string _postalCode = string.Empty;
    private string _note = string.Empty;

    public ClientCardViewModel(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;

        SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync(), _ => !HasErrors);
        DeleteCommand = new AsyncRelayCommand(async _ => await DeleteAsync(), _ => CanDelete);
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(this, false));

        SelectedRegion = Regions.FirstOrDefault() ?? string.Empty;
        SelectedCity = Cities.FirstOrDefault() ?? string.Empty;
        SelectedArea = Areas.FirstOrDefault() ?? string.Empty;
        ValidateAll();
    }

    public ObservableCollection<string> Regions { get; } = ["Київ", "Львів", "Одеса", "Харків", "Дніпро"];
    public ObservableCollection<string> DiscountTypes { get; } = ["Бонусна система", "Відсоткова", "Фіксована"];
    public ObservableCollection<string> CardTypes { get; } = ["Бонус", "Silver", "Gold"];
    public ObservableCollection<string> Countries { get; } = ["Україна", "Польща", "Молдова"];
    public ObservableCollection<string> Cities { get; } = ["Київ", "Львів", "Одеса", "Харків"];
    public ObservableCollection<string> Areas { get; } = ["Київська", "Львівська", "Одеська", "Харківська"];

    public ObservableCollection<FamilyMemberRow> FamilyMembers { get; } = [];
    public ObservableCollection<PurchaseRow> Purchases { get; } = [];

    public string WindowTitle => _isExistingCustomer ? "Редагування клієнта" : "Новий клієнт";
    public bool CanDelete => _isExistingCustomer;

    public string CustomerName { get => _customerName; set => SetProperty(ref _customerName, value); }
    public string LastName { get => _lastName; set => SetProperty(ref _lastName, value); }
    public string FirstName { get => _firstName; set => SetProperty(ref _firstName, value); }
    public string MiddleName { get => _middleName; set => SetProperty(ref _middleName, value); }
    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string DiscountCardNumber { get => _discountCardNumber; set => SetProperty(ref _discountCardNumber, value); }
    public string SelectedRegion { get => _selectedRegion; set => SetProperty(ref _selectedRegion, value); }
    public string SelectedDiscountType { get => _selectedDiscountType; set => SetProperty(ref _selectedDiscountType, value); }
    public string SelectedCardType { get => _selectedCardType; set => SetProperty(ref _selectedCardType, value); }
    public decimal InitialDiscountAmount { get => _initialDiscountAmount; set => SetProperty(ref _initialDiscountAmount, value); }
    public decimal DiscountPercent { get => _discountPercent; set => SetProperty(ref _discountPercent, value); }
    public decimal BonusBalance { get => _bonusBalance; set => SetProperty(ref _bonusBalance, value); }
    public bool IsVip { get => _isVip; set => SetProperty(ref _isVip, value); }
    public bool IsBlocked { get => _isBlocked; set => SetProperty(ref _isBlocked, value); }
    public string SelectedCountry { get => _selectedCountry; set => SetProperty(ref _selectedCountry, value); }
    public string SelectedCity { get => _selectedCity; set => SetProperty(ref _selectedCity, value); }
    public string SelectedArea { get => _selectedArea; set => SetProperty(ref _selectedArea, value); }
    public string PostalCode { get => _postalCode; set => SetProperty(ref _postalCode, value); }
    public string Note { get => _note; set => SetProperty(ref _note, value); }

    public RelayCommand CancelCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand DeleteCommand { get; }

    public event EventHandler<bool?>? RequestClose;

    public void Load(Customer? customer)
    {
        _isExistingCustomer = customer is not null;
        _customerId = customer?.Id ?? Guid.Empty;

        CustomerName = customer?.Name ?? string.Empty;
        LastName = customer?.LastName ?? string.Empty;
        FirstName = customer?.FirstName ?? string.Empty;
        MiddleName = customer?.MiddleName ?? string.Empty;
        Phone = customer?.Phone ?? string.Empty;
        Email = customer?.Email ?? string.Empty;
        DiscountCardNumber = customer?.DiscountCardNumber ?? string.Empty;
        SelectedRegion = customer?.Region ?? Regions.FirstOrDefault() ?? string.Empty;
        SelectedDiscountType = customer?.DiscountType ?? DiscountTypes.First();
        SelectedCardType = customer?.CardType ?? CardTypes.First();
        InitialDiscountAmount = customer?.InitialDiscountAmount ?? 0;
        DiscountPercent = customer?.DiscountPercent ?? 0;
        BonusBalance = customer?.BonusBalance ?? 0;
        IsVip = customer?.IsVip ?? false;
        IsBlocked = customer?.IsBlocked ?? false;
        SelectedCountry = customer?.Country ?? Countries.First();
        SelectedCity = customer?.City ?? Cities.FirstOrDefault() ?? string.Empty;
        SelectedArea = customer?.RegionArea ?? Areas.FirstOrDefault() ?? string.Empty;
        PostalCode = customer?.PostalCode ?? string.Empty;
        Note = customer?.Note ?? string.Empty;

        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(CanDelete));
        DeleteCommand.RaiseCanExecuteChanged();
        SaveCommand.RaiseCanExecuteChanged();
    }

    protected override void ValidateProperty(string propertyName)
    {
        ClearErrors(propertyName);

        switch (propertyName)
        {
            case nameof(CustomerName):
                if (string.IsNullOrWhiteSpace(CustomerName)) AddError(propertyName, "Найменування клієнта є обов'язковим.");
                break;
            case nameof(DiscountCardNumber):
                if (string.IsNullOrWhiteSpace(DiscountCardNumber)) AddError(propertyName, "Код картки обов'язковий.");
                break;
            case nameof(DiscountPercent):
                if (DiscountPercent is < 0 or > 100) AddError(propertyName, "% знижки має бути в межах 0..100.");
                break;
            case nameof(InitialDiscountAmount):
                if (InitialDiscountAmount < 0) AddError(propertyName, "Початкова сума для знижки не може бути від'ємною.");
                break;
            case nameof(BonusBalance):
                if (BonusBalance < 0) AddError(propertyName, "Початкова сума бонусу не може бути від'ємною.");
                break;
            case nameof(Email):
                if (!string.IsNullOrWhiteSpace(Email) && !EmailRegex.IsMatch(Email)) AddError(propertyName, "Некоректний формат електронної адреси.");
                break;
            case nameof(Phone):
                if (!string.IsNullOrWhiteSpace(Phone) && !PhoneRegex.IsMatch(Phone)) AddError(propertyName, "Некоректний формат телефону.");
                break;
        }
    }

    protected override void OnErrorsChanged(string propertyName) => SaveCommand.RaiseCanExecuteChanged();

    private async Task SaveAsync()
    {
        ValidateAll();
        if (HasErrors)
        {
            MessageBox.Show("Перевірте правильність заповнення полів.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var entity = new Customer
        {
            Id = _isExistingCustomer ? _customerId : Guid.NewGuid(),
            Name = CustomerName.Trim(),
            LastName = LastName.Trim(),
            FirstName = FirstName.Trim(),
            MiddleName = MiddleName.Trim(),
            Phone = Phone.Trim(),
            Email = Email.Trim(),
            DiscountCardNumber = DiscountCardNumber.Trim(),
            Region = SelectedRegion,
            DiscountType = SelectedDiscountType,
            CardType = SelectedCardType,
            InitialDiscountAmount = InitialDiscountAmount,
            DiscountPercent = DiscountPercent,
            BonusBalance = BonusBalance,
            IsVip = IsVip,
            IsBlocked = IsBlocked,
            Country = SelectedCountry,
            City = SelectedCity,
            RegionArea = SelectedArea,
            PostalCode = PostalCode.Trim(),
            Note = Note.Trim(),
            BonusPoints = BonusBalance
        };

        if (_isExistingCustomer) await _customerRepository.UpdateAsync(entity);
        else await _customerRepository.AddAsync(entity);

        RequestClose?.Invoke(this, true);
    }

    private async Task DeleteAsync()
    {
        if (!_isExistingCustomer) return;

        if (MessageBox.Show("Видалити клієнта?", "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

        await _customerRepository.DeleteAsync(_customerId);
        RequestClose?.Invoke(this, true);
    }
}

public class FamilyMemberRow
{
    public string РодиннийЗв'язок { get; set; } = string.Empty;
    public string ПІБ { get; set; } = string.Empty;
    public string Телефон { get; set; } = string.Empty;
}

public class PurchaseRow
{
    public DateTime Дата { get; set; }
    public string Документ { get; set; } = string.Empty;
    public decimal Сума { get; set; }
    public decimal НарахованоБонусів { get; set; }
}
