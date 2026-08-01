-- SPEC 00 — Database Schema
-- Target: .\SQLEXPRESS → LegacyEcommerceDb
-- Run: SQLCMD -S .\SQLEXPRESS -E -i 00_CreateSchema.sql

IF DB_ID(N'LegacyEcommerceDb') IS NULL
BEGIN
    CREATE DATABASE LegacyEcommerceDb;
END
GO

USE LegacyEcommerceDb;
GO

-- Drop in FK-safe order (idempotent re-runs for learning project)
IF OBJECT_ID(N'dbo.OrderItem', N'U') IS NOT NULL DROP TABLE dbo.OrderItem;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID(N'dbo.CartItem', N'U') IS NOT NULL DROP TABLE dbo.CartItem;
IF OBJECT_ID(N'dbo.ProductVariant', N'U') IS NOT NULL DROP TABLE dbo.ProductVariant;
IF OBJECT_ID(N'dbo.ProductImage', N'U') IS NOT NULL DROP TABLE dbo.ProductImage;
IF OBJECT_ID(N'dbo.Product', N'U') IS NOT NULL DROP TABLE dbo.Product;
IF OBJECT_ID(N'dbo.Category', N'U') IS NOT NULL DROP TABLE dbo.Category;
GO

CREATE TABLE dbo.Category (
    CategoryId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    ParentCategoryId INT NULL,
    DisplayOrder INT NOT NULL CONSTRAINT DF_Category_DisplayOrder DEFAULT (0),
    CONSTRAINT FK_Category_Parent
        FOREIGN KEY (ParentCategoryId) REFERENCES dbo.Category(CategoryId)
);
GO

CREATE TABLE dbo.Product (
    ProductId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CategoryId INT NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Price DECIMAL(18,2) NOT NULL,
    ThumbnailUrl NVARCHAR(300) NULL,
    Stock INT NOT NULL CONSTRAINT DF_Product_Stock DEFAULT (0),
    IsActive BIT NOT NULL CONSTRAINT DF_Product_IsActive DEFAULT (1),
    CreatedDate DATETIME NOT NULL CONSTRAINT DF_Product_CreatedDate DEFAULT (GETDATE()),
    CONSTRAINT FK_Product_Category
        FOREIGN KEY (CategoryId) REFERENCES dbo.Category(CategoryId)
);
GO

CREATE TABLE dbo.ProductImage (
    ProductImageId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProductId INT NOT NULL,
    Url NVARCHAR(300) NOT NULL,
    DisplayOrder INT NOT NULL CONSTRAINT DF_ProductImage_DisplayOrder DEFAULT (0),
    CONSTRAINT FK_ProductImage_Product
        FOREIGN KEY (ProductId) REFERENCES dbo.Product(ProductId)
);
GO

CREATE TABLE dbo.ProductVariant (
    ProductVariantId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProductId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,      -- e.g. "Size: L / Color: Red"
    SkuSuffix NVARCHAR(50) NULL,
    Stock INT NOT NULL CONSTRAINT DF_ProductVariant_Stock DEFAULT (0),
    PriceAdjustment DECIMAL(18,2) NOT NULL CONSTRAINT DF_ProductVariant_PriceAdjustment DEFAULT (0),
    CONSTRAINT FK_ProductVariant_Product
        FOREIGN KEY (ProductId) REFERENCES dbo.Product(ProductId)
);
GO

CREATE TABLE dbo.CartItem (
    CartItemId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId NVARCHAR(128) NULL,        -- nullable: guest carts keyed by SessionId instead
    SessionId NVARCHAR(100) NULL,
    ProductId INT NOT NULL,
    ProductVariantId INT NULL,
    Quantity INT NOT NULL,
    AddedDate DATETIME NOT NULL CONSTRAINT DF_CartItem_AddedDate DEFAULT (GETDATE()),
    CONSTRAINT FK_CartItem_Product
        FOREIGN KEY (ProductId) REFERENCES dbo.Product(ProductId),
    CONSTRAINT FK_CartItem_ProductVariant
        FOREIGN KEY (ProductVariantId) REFERENCES dbo.ProductVariant(ProductVariantId)
);
GO

CREATE TABLE dbo.Orders (
    OrderId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId NVARCHAR(128) NOT NULL,
    OrderDate DATETIME NOT NULL CONSTRAINT DF_Orders_OrderDate DEFAULT (GETDATE()),
    Status NVARCHAR(50) NOT NULL CONSTRAINT DF_Orders_Status DEFAULT (N'Pending'),  -- Pending, Paid, Shipped, Cancelled
    ShippingAddress NVARCHAR(500) NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL
);
GO

CREATE TABLE dbo.OrderItem (
    OrderItemId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    ProductVariantId INT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_OrderItem_Orders
        FOREIGN KEY (OrderId) REFERENCES dbo.Orders(OrderId),
    CONSTRAINT FK_OrderItem_Product
        FOREIGN KEY (ProductId) REFERENCES dbo.Product(ProductId),
    CONSTRAINT FK_OrderItem_ProductVariant
        FOREIGN KEY (ProductVariantId) REFERENCES dbo.ProductVariant(ProductVariantId)
);
GO

-- Indexes called out in Spec 10 (safe to add now)
CREATE NONCLUSTERED INDEX IX_Product_CategoryId ON dbo.Product(CategoryId);
CREATE NONCLUSTERED INDEX IX_CartItem_UserId ON dbo.CartItem(UserId);
CREATE NONCLUSTERED INDEX IX_Orders_UserId ON dbo.Orders(UserId);
GO

PRINT N'Schema created successfully in LegacyEcommerceDb.';
GO
