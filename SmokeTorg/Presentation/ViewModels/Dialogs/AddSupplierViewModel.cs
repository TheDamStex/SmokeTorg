using SmokeTorg.Application.Services;
using SmokeTorg.Common.Base;
using SmokeTorg.Common.Commands;
using SmokeTorg.Domain.Entities;
using SmokeTorg.Presentation.Services;

namespace SmokeTorg.Presentation.ViewModels.Dialogs;

public class AddSupplierViewModel(SupplierService supplierService) : ViewModelBase, IDialogRequestClose
{
    private string _name = string.Empty;
    private string _contactPerson = string.Empty;
    private string _phone = string.Empty;
    private string _email = string.Empty;

    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string ContactPerson { get => _contactPerson; set => SetProperty(ref _contactPerson, value); }
    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
    public string Email { get => _email; set => SetProperty(ref _email, value); }

    public Supplier? CreatedSupplier { get; private set; }

    public event EventHandler<bool?>? RequestClose;

    public AsyncRelayCommand SaveCommand => new(async _ =>
    {
        var supplier = new Supplier
        {
            Name = Name.Trim(),
            ContactPerson = ContactPerson.Trim(),
            Phone = Phone.Trim(),
            Email = Email.Trim()
        };

        CreatedSupplier = await supplierService.SaveAsync(supplier);
        RequestClose?.Invoke(this, true);
    }, _ => !string.IsNullOrWhiteSpace(Name));

    public RelayCommand CancelCommand => new(_ => RequestClose?.Invoke(this, false));
}
