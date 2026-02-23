using SmokeTorg.Domain.Enums;

namespace SmokeTorg.Domain.Entities;

public class Sale : BaseEntity
{
    public DateTime Date { get; set; } = DateTime.Now;
    public Guid CashierId { get; set; }
    public List<SaleItem> Items { get; set; } = [];
    public decimal DiscountPercent { get; set; }
    public decimal PaidCash { get; set; }
    public decimal PaidCard { get; set; }
    public decimal Total { get; set; }
    public decimal TaxAmount { get; set; }
}

public class SaleItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal DiscountPercent { get; set; }
}

public class Purchase : BaseEntity
{
    public Guid SupplierId { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public List<PurchaseItem> Items { get; set; } = [];
    public decimal Total { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
}

public class PurchaseItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
}

public class StockMovement : BaseEntity
{
    public Guid ProductId { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public decimal Quantity { get; set; }
    public string Type { get; set; } = "In";
    public string Reason { get; set; } = string.Empty;
}

public class StockItem : BaseEntity
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
}

public class CashShift : BaseEntity
{
    public DateTime OpenedAt { get; set; } = DateTime.Now;
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningAmount { get; set; }
    public decimal ClosingAmount { get; set; }
}

public class CashOperation : BaseEntity
{
    public Guid ShiftId { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public string Type { get; set; } = "In";
    public decimal Amount { get; set; }
}

public class Return : BaseEntity
{
    public Guid? SaleId { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public List<ReturnItem> Items { get; set; } = [];
}

public class ReturnItem
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
}

public class AppSettings : BaseEntity
{
    public string StoreName { get; set; } = "Demo Store";
    public string TaxName { get; set; } = "VAT";
    public decimal TaxPercent { get; set; } = 20;
    public string StorageType { get; set; } = "Json";
    public string ReceiptFooter { get; set; } = "Спасибо за покупку!";
}
