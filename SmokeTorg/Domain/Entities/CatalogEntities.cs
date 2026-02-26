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
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string DiscountCardNumber { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public string CardType { get; set; } = string.Empty;
    public decimal InitialDiscountAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal BonusBalance { get; set; }
    public bool IsVip { get; set; }
    public bool IsBlocked { get; set; }
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string RegionArea { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public decimal BonusPoints { get; set; }
}

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Cashier;
    public bool IsActive { get; set; } = true;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
