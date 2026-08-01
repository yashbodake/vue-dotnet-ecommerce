-- SPEC 00 — Seed data
-- Acceptance: at least 5 sample products across 2 categories
-- Run: SQLCMD -S .\SQLEXPRESS -E -i 01_SeedData.sql

USE LegacyEcommerceDb;
GO

-- Clear seedable tables in FK-safe order (keeps schema intact for re-seed)
DELETE FROM dbo.OrderItem;
DELETE FROM dbo.Orders;
DELETE FROM dbo.CartItem;
DELETE FROM dbo.ProductVariant;
DELETE FROM dbo.ProductImage;
DELETE FROM dbo.Product;
DELETE FROM dbo.Category;
GO

-- Reset identities
DBCC CHECKIDENT ('dbo.Category', RESEED, 0);
DBCC CHECKIDENT ('dbo.Product', RESEED, 0);
DBCC CHECKIDENT ('dbo.ProductImage', RESEED, 0);
DBCC CHECKIDENT ('dbo.ProductVariant', RESEED, 0);
GO

-- Categories (2)
SET IDENTITY_INSERT dbo.Category ON;
INSERT INTO dbo.Category (CategoryId, Name, ParentCategoryId, DisplayOrder) VALUES
(1, N'Electronics', NULL, 1),
(2, N'Apparel',     NULL, 2);
SET IDENTITY_INSERT dbo.Category OFF;
GO

-- Products (6 across 2 categories)
SET IDENTITY_INSERT dbo.Product ON;
INSERT INTO dbo.Product (ProductId, CategoryId, Name, Description, Price, ThumbnailUrl, Stock, IsActive, CreatedDate) VALUES
(1, 1, N'Wireless Bluetooth Headphones',
    N'Over-ear headphones with 30-hour battery life and noise isolation.',
    79.99, N'/Content/images/products/headphones.jpg', 25, 1, GETDATE()),
(2, 1, N'USB-C Hub 7-in-1',
    N'HDMI, USB 3.0, SD/TF, and PD charging in one compact hub.',
    49.99, N'/Content/images/products/usb-hub.jpg', 40, 1, GETDATE()),
(3, 1, N'Portable SSD 1TB',
    N'USB 3.2 Gen 2 external SSD — up to 1050 MB/s read.',
    129.99, N'/Content/images/products/ssd.jpg', 15, 1, GETDATE()),
(4, 2, N'Classic Cotton T-Shirt',
    N'Soft midweight cotton tee. Unisex fit.',
    24.99, N'/Content/images/products/tshirt.jpg', 100, 1, GETDATE()),
(5, 2, N'Denim Jacket',
    N'Mid-wash denim jacket with brass buttons.',
    89.99, N'/Content/images/products/denim-jacket.jpg', 20, 1, GETDATE()),
(6, 2, N'Running Sneakers',
    N'Lightweight trainers with cushioned midsole. Out of stock sample.',
    109.99, N'/Content/images/products/sneakers.jpg', 0, 1, GETDATE());
SET IDENTITY_INSERT dbo.Product OFF;
GO

-- Product images
INSERT INTO dbo.ProductImage (ProductId, Url, DisplayOrder) VALUES
(1, N'/Content/images/products/headphones.jpg', 0),
(1, N'/Content/images/products/headphones-side.jpg', 1),
(2, N'/Content/images/products/usb-hub.jpg', 0),
(3, N'/Content/images/products/ssd.jpg', 0),
(4, N'/Content/images/products/tshirt.jpg', 0),
(4, N'/Content/images/products/tshirt-back.jpg', 1),
(5, N'/Content/images/products/denim-jacket.jpg', 0),
(6, N'/Content/images/products/sneakers.jpg', 0);
GO

-- Variants (apparel sizing / color)
INSERT INTO dbo.ProductVariant (ProductId, Name, SkuSuffix, Stock, PriceAdjustment) VALUES
(4, N'Size: S / Color: White', N'-S-WHT', 30, 0),
(4, N'Size: M / Color: White', N'-M-WHT', 40, 0),
(4, N'Size: L / Color: Black', N'-L-BLK', 30, 0),
(5, N'Size: M', N'-M', 10, 0),
(5, N'Size: L', N'-L', 10, 0),
(6, N'Size: 9 / Color: Grey', N'-9-GRY', 0, 0),
(6, N'Size: 10 / Color: Grey', N'-10-GRY', 0, 0);
GO

PRINT N'Seed data inserted.';
PRINT N'Categories:';
SELECT CategoryId, Name FROM dbo.Category ORDER BY DisplayOrder;

PRINT N'Products:';
SELECT p.ProductId, c.Name AS Category, p.Name, p.Price, p.Stock, p.IsActive
FROM dbo.Product p
INNER JOIN dbo.Category c ON c.CategoryId = p.CategoryId
ORDER BY p.ProductId;
GO
