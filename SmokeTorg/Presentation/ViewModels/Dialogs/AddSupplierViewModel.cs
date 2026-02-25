using System.Text.RegularExpressions;
using System.Windows;
using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Entities;
using SmokeTorg.Presentation.Services;

namespace SmokeTorg.Presentation.ViewModels.Dialogs;

public class AddSupplierViewModel : ViewModelBase, IDialogRequestClose
{
    private static readonly Regex PhonePattern = new(@"^\+?\d{10,13}$", RegexOptions.Compiled);
    private readonly SupplierService _supplierService;

    private string _name = string.Empty;
    private string _contactPerson = string.Empty;
    private string _phone = string.Empty;
    private string _email = string.Empty;

    public AddSupplierViewModel(SupplierService supplierService)
    {
        _supplierService = supplierService;
        SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync(), _ => !HasErrors);
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(this, false));
        ValidateAll();
    }

    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string ContactPerson { get => _contactPerson; set => SetProperty(ref _contactPerson, value); }
    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
    public string Email { get => _email; set => SetProperty(ref _email, value); }

    public Supplier? CreatedSupplier { get; private set; }

    public event EventHandler<bool?>? RequestClose;

    public AsyncRelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    protected override void ValidateProperty(string propertyName)
    {
        ClearErrors(propertyName);

        switch (propertyName)
        {
            case nameof(Name):
                if (string.IsNullOrWhiteSpace(Name)) AddError(nameof(Name), "Поле обов’язкове");
                break;
            case nameof(Phone):
                if (!string.IsNullOrWhiteSpace(Phone) && !PhonePattern.IsMatch(Phone.Trim())) AddError(nameof(Phone), "Некоректний номер телефону");
                break;
            case nameof(Email):
                if (!string.IsNullOrWhiteSpace(Email) && !Email.Contains('@')) AddError(nameof(Email), "Некоректний email");
                break;
        }
    }

    protected override void OnErrorsChanged(string propertyName) => SaveCommand.RaiseCanExecuteChanged();

    private async Task SaveAsync()
    {
        ValidateAll();
        if (HasErrors)
        {
            MessageBox.Show("Виправте помилки у формі.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var supplier = new Supplier
        {
            Name = Name.Trim(),
            ContactPerson = ContactPerson.Trim(),
            Phone = Phone.Trim(),
            Email = Email.Trim()
        };

        CreatedSupplier = await _supplierService.SaveAsync(supplier);
        RequestClose?.Invoke(this, true);
    }
}
