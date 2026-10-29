/* The web application connects with its own login: read/write data + execute, nothing else. */
USE [master];
GO
CREATE LOGIN [app_portal] WITH PASSWORD = N'<generated-password>', CHECK_POLICY = ON, DEFAULT_DATABASE = [SupplierPortal];
GO
USE [SupplierPortal];
GO
CREATE USER [app_portal] FOR LOGIN [app_portal];
ALTER ROLE db_datareader ADD MEMBER [app_portal];
GRANT UPDATE ON dbo.Suppliers (PasswordHash, MustChangePassword, PasswordChangedAt) TO [app_portal];  -- column-level
GRANT EXECUTE ON dbo.usp_SupplierPayments TO [app_portal];
GO
