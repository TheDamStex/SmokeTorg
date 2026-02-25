using System.Collections;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Entities;
using SmokeTorg.Presentation.Services;

namespace SmokeTorg.Presentation.ViewModels.Dialogs;

public class SupplierCreateViewModel : ViewModelBase, IDialogRequestClose, INotifyDataErrorInfo
{
    private static readonly Regex PhonePattern = new(@"^\+?\d{10,13}$", RegexOptions.Compiled);

    private readonly SupplierService _supplierService;
    private readonly Dictionary<string, List<string>> _errors = [];

    private string _name = string.Empty;
    private string _phone = string.Empty;
    private string _email = string.Empty;
    private string _contactPerson = string.Empty;
    private string _address = string.Empty;
    private string _taxId = string.Empty;
    private string _note = string.Empty;

    public SupplierCreateViewModel(SupplierService supplierService)
    {
        _supplierService = supplierService;

        SaveCommand = new AsyncRelayCommand(SaveAsync, _ => !HasErrors);
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(this, false));

        ValidateAll();
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                ValidateName();
            }
        }
    }

    public string Phone
    {
        get => _phone;
        set
        {
            if (SetProperty(ref _phone, value))
            {
                ValidatePhone();
            }
        }
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string ContactPerson
    {
        get => _contactPerson;
        set => SetProperty(ref _contactPerson, value);
    }

    public string Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public string TaxId
    {
        get => _taxId;
        set => SetProperty(ref _taxId, value);
    }

    public string Note
    {
        get => _note;
        set => SetProperty(ref _note, value);
    }

    public Supplier? CreatedSupplier { get; private set; }

    public AsyncRelayCommand SaveCommand { get; }

    public RelayCommand CancelCommand { get; }

    public bool HasErrors => _errors.Count != 0;

    public event EventHandler<bool?>? RequestClose;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName) || !_errors.TryGetValue(propertyName, out var errors))
        {
            return Enumerable.Empty<string>();
        }

        return errors;
    }

    private async Task SaveAsync(object? _)
    {
        ValidateAll();
        if (HasErrors)
        {
            return;
        }

        var supplier = new Supplier
        {
            Name = Name.Trim(),
            ContactPerson = ContactPerson.Trim(),
            Phone = Phone.Trim(),
            Email = Email.Trim(),
            Address = Address.Trim(),
            TaxId = TaxId.Trim(),
            Note = Note.Trim()
        };

        CreatedSupplier = await _supplierService.SaveAsync(supplier);

        MessageBox.Show("Постачальника успішно збережено.", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
        RequestClose?.Invoke(this, true);
    }

    private void ValidateAll()
    {
        ValidateName();
        ValidatePhone();
    }

    private void ValidateName()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Вкажіть назву постачальника.");
        }

        SetErrors(nameof(Name), errors);
    }

    private void ValidatePhone()
    {
        var errors = new List<string>();
        var phone = Phone.Trim();

        if (!string.IsNullOrWhiteSpace(phone) && !PhonePattern.IsMatch(phone))
        {
            errors.Add("Телефон має містити лише + та цифри, довжина 10–13 символів.");
        }

        SetErrors(nameof(Phone), errors);
    }

    private void SetErrors(string propertyName, List<string> errors)
    {
        if (errors.Count == 0)
        {
            if (_errors.Remove(propertyName))
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }
        else
        {
            _errors[propertyName] = errors;
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        SaveCommand.RaiseCanExecuteChanged();
    }
}
