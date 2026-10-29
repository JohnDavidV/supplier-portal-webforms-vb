/* =====================================================================================
   Import suppliers from the OLD portal, whose table kept passwords in plain text or as
   unsalted SHA-256. Every value is stored as legacy$<sha256> (nothing readable is ever
   written to the new table) and upgraded to PBKDF2 by the app on the supplier's next login.
   Run with @Apply = 0 first (preview), then @Apply = 1. Safe to run more than once.
   ===================================================================================== */
SET NOCOUNT ON;
DECLARE @Apply BIT = 0;

IF OBJECT_ID('tempdb..#ToMigrate') IS NOT NULL DROP TABLE #ToMigrate;

SELECT  LTRIM(RTRIM(CAST(o.nit AS VARCHAR(20))))                    AS NitBase,
        LEFT(LTRIM(RTRIM(o.razon_social)), 200)                       AS Name,
        CASE WHEN o.correo LIKE '%_@_%._%' AND o.correo NOT LIKE '% %' THEN LOWER(LEFT(o.correo, 150)) END AS Email,
        'legacy$' + CASE
            WHEN LEN(o.clave) = 64 AND o.clave NOT LIKE '%[^0-9a-fA-F]%' THEN LOWER(o.clave)     -- already SHA-256
            ELSE LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CAST(LTRIM(RTRIM(o.clave)) AS VARCHAR(200))), 2))
        END                                                           AS PasswordHash
INTO    #ToMigrate
FROM    OldPortal.dbo.proveedores o                                   -- source table of the previous system
WHERE   o.clave IS NOT NULL
  AND   LTRIM(RTRIM(CAST(o.nit AS VARCHAR(20)))) NOT LIKE '%[^0-9]%'
  AND   NOT EXISTS (SELECT 1 FROM dbo.Suppliers s WHERE s.NitBase = LTRIM(RTRIM(CAST(o.nit AS VARCHAR(20)))));

SELECT COUNT(*) AS SuppliersToImport, SUM(CASE WHEN Email IS NULL THEN 1 ELSE 0 END) AS WithoutValidEmail FROM #ToMigrate;

IF @Apply = 1
BEGIN
    INSERT INTO dbo.Suppliers (NitBase, Name, Email, PasswordHash, MustChangePassword)
    SELECT NitBase, Name, Email, PasswordHash, 1 FROM #ToMigrate;    -- 1 = choose a new password at first login
    PRINT CONCAT(@@ROWCOUNT, ' supplier(s) imported.');
END
