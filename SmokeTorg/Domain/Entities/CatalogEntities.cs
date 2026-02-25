using SmokeTorg.Domain.Enums;

namespace SmokeTorg.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string Unit { get; set; } = "pcs";
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public string TaxGroup { get; set; } = "A";
    public bool IsActive { get; set; } = true;
    public decimal MinStock { get; set; } = 0;
}

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public decimal BonusPoints { get; set; }
}

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Cashier;
    public bool IsActive { get; set; } = true;
}
