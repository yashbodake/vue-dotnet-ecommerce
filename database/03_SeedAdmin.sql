-- Spec 09: Admin role + user seed (optional SQL path).
-- Preferred: app Startup calls AdminSeed.EnsureAdminUser() which hashes the password correctly.
-- This script only ensures the Admin role exists; password hashing should still go through Identity.

USE LegacyEcommerceDb;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE Name = N'Admin')
BEGIN
    INSERT INTO dbo.AspNetRoles (Id, Name)
    VALUES (NEWID(), N'Admin');
    PRINT N'Admin role created.';
END
ELSE
BEGIN
    PRINT N'Admin role already exists.';
END
GO

PRINT N'Run the web app once so AdminSeed creates admin@legacy.local / Admin123! with a valid password hash.';
GO
