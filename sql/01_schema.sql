/* Supplier portal schema (SQL Server 2016+). Run once on an empty database. */
SET NOCOUNT ON;

CREATE TABLE dbo.Suppliers (
    NitBase            VARCHAR(15)   NOT NULL CONSTRAINT PK_Suppliers PRIMARY KEY,
    Name               NVARCHAR(200) NOT NULL,
    Email              NVARCHAR(150) NULL,
    PasswordHash       VARCHAR(200)  NOT NULL,           -- pbkdf2$iter$salt$hash  |  legacy$sha256
    MustChangePassword BIT           NOT NULL CONSTRAINT DF_Suppliers_MustChange DEFAULT (0),
    IsActive           BIT           NOT NULL CONSTRAINT DF_Suppliers_IsActive DEFAULT (1),
    PasswordChangedAt  DATETIME2(0)  NULL,
    CreatedAt          DATETIME2(0)  NOT NULL CONSTRAINT DF_Suppliers_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT CK_Suppliers_Nit CHECK (NitBase NOT LIKE '%[^0-9]%' AND LEN(NitBase) BETWEEN 6 AND 15)
);

CREATE TABLE dbo.Payments (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payments PRIMARY KEY,
    NitBase       VARCHAR(15)   NOT NULL CONSTRAINT FK_Payments_Suppliers REFERENCES dbo.Suppliers (NitBase),
    PaidOn        DATE          NOT NULL,
    InvoiceNumber VARCHAR(30)   NOT NULL,
    GrossAmount   DECIMAL(18,2) NOT NULL CONSTRAINT CK_Payments_Gross CHECK (GrossAmount >= 0),
    Withholding   DECIMAL(18,2) NOT NULL CONSTRAINT DF_Payments_Withholding DEFAULT (0),
    CONSTRAINT CK_Payments_Withholding CHECK (Withholding BETWEEN 0 AND GrossAmount)
);
-- Every portal query filters by supplier and year: cover it.
CREATE INDEX IX_Payments_Nit_PaidOn ON dbo.Payments (NitBase, PaidOn DESC) INCLUDE (InvoiceNumber, GrossAmount, Withholding);
GO

CREATE OR ALTER PROCEDURE dbo.usp_SupplierPayments
    @nit  VARCHAR(15),
    @year INT
AS
BEGIN
    SET NOCOUNT ON;
    -- Sargable range instead of YEAR(PaidOn) = @year, so the index above is used.
    DECLARE @from DATE = DATEFROMPARTS(@year, 1, 1);
    SELECT Id, NitBase, CAST(PaidOn AS DATETIME) AS PaidOn, InvoiceNumber, GrossAmount, Withholding
    FROM dbo.Payments
    WHERE NitBase = @nit AND PaidOn >= @from AND PaidOn < DATEADD(YEAR, 1, @from)
    ORDER BY PaidOn DESC;
END
GO
