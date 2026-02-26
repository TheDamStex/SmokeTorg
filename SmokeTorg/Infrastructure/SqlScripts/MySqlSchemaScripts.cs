namespace SmokeTorg.Infrastructure.SqlScripts;

public static class MySqlSchemaScripts
{
    public const int CurrentVersion = 1;

    public static IReadOnlyList<string> CreateTables { get; } =
    [
        @"CREATE TABLE IF NOT EXISTS Users (
            Id CHAR(36) PRIMARY KEY,
            Username VARCHAR(100) NOT NULL UNIQUE,
            PasswordHash VARCHAR(255) NOT NULL,
            PasswordSalt VARCHAR(255) NOT NULL,
            Role INT NOT NULL,
            IsActive TINYINT(1) NOT NULL DEFAULT 1,
            FullName VARCHAR(255) NULL,
            CreatedAt DATETIME NOT NULL
        );",
        @"CREATE TABLE IF NOT EXISTS Settings (
            Id CHAR(36) PRIMARY KEY,
            StoreName VARCHAR(255) NOT NULL,
            TaxName VARCHAR(100) NOT NULL,
            TaxPercent DECIMAL(10,2) NOT NULL,
            StorageType VARCHAR(50) NOT NULL,
            ReceiptFooter TEXT NULL
        );",
        @"CREATE TABLE IF NOT EXISTS Categories (
            Id CHAR(36) PRIMARY KEY,
            Name VARCHAR(255) NOT NULL
        );",
        @"CREATE TABLE IF NOT EXISTS Products (
            Id CHAR(36) PRIMARY KEY,
            Name VARCHAR(255) NOT NULL,
            Sku VARCHAR(100) NOT NULL,
            Barcode VARCHAR(100) NOT NULL,
            CategoryId CHAR(36) NOT NULL,
            Unit VARCHAR(20) NOT NULL,
            PurchasePrice DECIMAL(18,2) NOT NULL,
            SalePrice DECIMAL(18,2) NOT NULL,
            TaxGroup VARCHAR(50) NOT NULL,
            IsActive TINYINT(1) NOT NULL,
            MinStock DECIMAL(18,2) NOT NULL,
            UNIQUE INDEX UX_Products_Barcode(Barcode)
        );",
        @"CREATE TABLE IF NOT EXISTS Suppliers (
            Id CHAR(36) PRIMARY KEY,
            Name VARCHAR(255) NOT NULL,
            ContactPerson VARCHAR(255) NULL,
            Phone VARCHAR(100) NULL,
            Email VARCHAR(255) NULL,
            Address VARCHAR(255) NULL,
            TaxId VARCHAR(100) NULL,
            Note TEXT NULL
        );",
        @"CREATE TABLE IF NOT EXISTS Customers (
            Id CHAR(36) PRIMARY KEY,
            Name VARCHAR(255) NOT NULL,
            LastName VARCHAR(255) NULL,
            FirstName VARCHAR(255) NULL,
            MiddleName VARCHAR(255) NULL,
            Phone VARCHAR(100) NULL,
            Email VARCHAR(255) NULL,
            Region VARCHAR(255) NULL,
            DiscountCardNumber VARCHAR(100) NULL,
            DiscountType VARCHAR(100) NULL,
            CardType VARCHAR(100) NULL,
            InitialDiscountAmount DECIMAL(18,2) NOT NULL,
            DiscountPercent DECIMAL(18,2) NOT NULL,
            BonusBalance DECIMAL(18,2) NOT NULL,
            IsVip TINYINT(1) NOT NULL,
            IsBlocked TINYINT(1) NOT NULL,
            Country VARCHAR(100) NULL,
            City VARCHAR(100) NULL,
            RegionArea VARCHAR(100) NULL,
            PostalCode VARCHAR(50) NULL,
            Note TEXT NULL,
            BonusPoints DECIMAL(18,2) NOT NULL,
            UNIQUE INDEX UX_Customers_DiscountCardNumber(DiscountCardNumber)
        );",
        @"CREATE TABLE IF NOT EXISTS Purchases (
            Id CHAR(36) PRIMARY KEY,
            SupplierId CHAR(36) NOT NULL,
            Date DATETIME NOT NULL,
            Total DECIMAL(18,2) NOT NULL,
            Status INT NOT NULL,
            INDEX IX_Purchases_Date(Date)
        );",
        @"CREATE TABLE IF NOT EXISTS PurchaseItems (
            Id BIGINT AUTO_INCREMENT PRIMARY KEY,
            PurchaseId CHAR(36) NOT NULL,
            ProductId CHAR(36) NOT NULL,
            ProductName VARCHAR(255) NOT NULL,
            BarcodeDisplay VARCHAR(100) NULL,
            Quantity DECIMAL(18,3) NOT NULL,
            Price DECIMAL(18,2) NOT NULL,
            FOREIGN KEY (PurchaseId) REFERENCES Purchases(Id) ON DELETE CASCADE
        );",
        @"CREATE TABLE IF NOT EXISTS Sales (
            Id CHAR(36) PRIMARY KEY,
            Date DATETIME NOT NULL,
            CashierId CHAR(36) NOT NULL,
            DiscountPercent DECIMAL(18,2) NOT NULL,
            PaidCash DECIMAL(18,2) NOT NULL,
            PaidCard DECIMAL(18,2) NOT NULL,
            Total DECIMAL(18,2) NOT NULL,
            TaxAmount DECIMAL(18,2) NOT NULL,
            INDEX IX_Sales_Date(Date)
        );",
        @"CREATE TABLE IF NOT EXISTS SaleItems (
            Id BIGINT AUTO_INCREMENT PRIMARY KEY,
            SaleId CHAR(36) NOT NULL,
            ProductId CHAR(36) NOT NULL,
            ProductName VARCHAR(255) NOT NULL,
            BarcodeDisplay VARCHAR(100) NULL,
            Quantity DECIMAL(18,3) NOT NULL,
            Price DECIMAL(18,2) NOT NULL,
            DiscountPercent DECIMAL(18,2) NOT NULL,
            FOREIGN KEY (SaleId) REFERENCES Sales(Id) ON DELETE CASCADE
        );",
        @"CREATE TABLE IF NOT EXISTS StockMovements (
            Id CHAR(36) PRIMARY KEY,
            ProductId CHAR(36) NOT NULL,
            Date DATETIME NOT NULL,
            Quantity DECIMAL(18,3) NOT NULL,
            Type VARCHAR(50) NOT NULL,
            Reason VARCHAR(255) NULL
        );",
        @"CREATE TABLE IF NOT EXISTS StockItems (
            Id CHAR(36) PRIMARY KEY,
            ProductId CHAR(36) NOT NULL UNIQUE,
            BarcodeDisplay VARCHAR(100) NULL,
            Quantity DECIMAL(18,3) NOT NULL
        );",
        @"CREATE TABLE IF NOT EXISTS SchemaInfo (
            Id BIGINT AUTO_INCREMENT PRIMARY KEY,
            SchemaVersion INT NOT NULL,
            AppliedAt DATETIME NOT NULL
        );"
    ];
}
