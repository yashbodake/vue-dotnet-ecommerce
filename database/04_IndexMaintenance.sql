-- Spec 10: ensure non-clustered indexes + manual rebuild for SQL Express
-- Express has no SQL Agent — run this periodically (e.g. weekly) during low traffic.

USE LegacyEcommerceDb;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Product_CategoryId' AND object_id = OBJECT_ID(N'dbo.Product'))
    CREATE NONCLUSTERED INDEX IX_Product_CategoryId ON dbo.Product(CategoryId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CartItem_UserId' AND object_id = OBJECT_ID(N'dbo.CartItem'))
    CREATE NONCLUSTERED INDEX IX_CartItem_UserId ON dbo.CartItem(UserId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_UserId' AND object_id = OBJECT_ID(N'dbo.Orders'))
    CREATE NONCLUSTERED INDEX IX_Orders_UserId ON dbo.Orders(UserId);
GO

-- Manual index maintenance (no SQL Agent on Express)
ALTER INDEX IX_Product_CategoryId ON dbo.Product REBUILD;
ALTER INDEX IX_CartItem_UserId ON dbo.CartItem REBUILD;
ALTER INDEX IX_Orders_UserId ON dbo.Orders REBUILD;
GO

-- Optional: watch Express 10GB cap
SELECT
    DB_NAME() AS DatabaseName,
    CAST(SUM(size) * 8.0 / 1024 AS DECIMAL(18,2)) AS SizeMB
FROM sys.master_files
WHERE database_id = DB_ID();
GO

PRINT N'Index ensure + REBUILD complete.';
GO
