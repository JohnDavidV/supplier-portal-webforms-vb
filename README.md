# Supplier Portal: ASP.NET WebForms in VB.NET

[![CI](https://github.com/JohnDavidV/supplier-portal-webforms-vb/actions/workflows/ci.yml/badge.svg)](https://github.com/JohnDavidV/supplier-portal-webforms-vb/actions/workflows/ci.yml)
![VB.NET](https://img.shields.io/badge/VB.NET-.NET%20Framework%204.8-512BD4?logo=dotnet&logoColor=white)
![ASP.NET WebForms](https://img.shields.io/badge/ASP.NET-WebForms-512BD4)
![SQL Server](https://img.shields.io/badge/SQL%20Server-T--SQL-CC2927?logo=microsoftsqlserver&logoColor=white)
![IIS](https://img.shields.io/badge/IIS-hosted-0078D4?logo=windows&logoColor=white)
![License](https://img.shields.io/badge/license-MIT-green)

A **supplier self-service portal** built the way many line-of-business systems are still built (ASP.NET WebForms + VB.NET + SQL Server on IIS), but with the security and engineering practices those systems usually lack. Suppliers sign in with their Colombian NIT, see their payments and withholdings by year, and get a signed, expiring link to their withholding certificate that they can forward to their accountant.

> This is a clean-room portfolio piece based on my day-to-day work maintaining and modernizing production VB.NET / WebForms applications (ERP modules, supplier and employee portals). I rewrote classic ASP endpoints, moved secrets out of the code, migrated plain-text passwords and automated deployments to IIS. All names and data here are fictional.

## What it demonstrates

| Area | Implementation |
|---|---|
| **VB.NET, modern style** | `Option Strict On`, warnings as errors, interfaces and dependency injection through a composition root (`AppServices.vb`), and LINQ. A **multi-targeted** core library (`net48` + `net8.0`) runs on IIS and in fast cross-platform tests. |
| **WebForms done right** | Master page, `SecurePage` base class, strongly typed data binding (`ItemType` + `<%#: %>`, which HTML-encodes), postback events with `Handles`, validators, and an `.ashx` handler. |
| **No SQL injection by construction** | `Db.vb` only accepts typed `DbParam` values. A stored procedure with a **sargable** date range uses a covering index. |
| **Passwords** | PBKDF2-HMAC-SHA256 with a random salt and a versioned format. Legacy `SHA-256` hashes from the old portal are accepted once and **upgraded transparently** on login. There is a forced change for migrated accounts and a password policy. |
| **Login hardening** | Lockout with exponential backoff, the same message for an unknown NIT or a wrong password, and a dummy hash verification so timing does not reveal registered NITs. The NIT is accepted in any format the user types (`900.123.456-7`) and its DIAN check digit is validated. |
| **CSRF** | `ViewStateUserKey` is bound to the user and session, and ViewState is MAC-validated and encrypted. |
| **Signed links** | HMAC-SHA256 over purpose + NIT + expiry. Changing the year, the NIT or the expiry invalidates the link. |
| **Secrets** | Kept in `Config\appSettings.config`, ignored by Git and hidden by IIS `requestFiltering`. `Web.config` only references the file. |
| **Response hardening** | CSP, `X-Frame-Options`, `nosniff` and no-referrer headers. `Server`, `X-AspNet-Version` and `X-Powered-By` are removed. HttpOnly / SameSite cookies are used, and `requireSSL` is set via `Web.Release.config`. `customErrors` keeps stack traces hidden, and pages with private data are not cached. |
| **Database** | Constraints (NIT format, withholding ≤ gross), a covering index, a **least-privilege login** with column-level `UPDATE`, and an idempotent migration from the old portal's table with a preview mode. |
| **CI** | GitHub Actions on Windows: builds the .NET Framework 4.8 site with `dotnet build` (no Visual Studio needed), runs the VB.NET unit tests, and then a **smoke test on IIS Express** that logs in through a real WebForms postback (ViewState), checks the auth guard, rejects a forged link and verifies that `Config/` is not served. |

## Structure

```
src/
  SupplierPortal.Core/        VB.NET class library (net48 + net8.0)
    Security/                 PasswordHasher, SignedLink, LoginThrottle, Crypto
    Domain/                   Nit (DIAN check digit), models
    Data/                     Db (parameterized ADO.NET), SQL + in-memory repositories
    AuthService.vb            login, transparent upgrade, password policy
  SupplierPortal.Web/         ASP.NET WebForms site (.NET Framework 4.8, VB.NET)
    Site.Master, Login.aspx, Default.aspx, Account/ChangePassword.aspx
    Certificate.ashx          public certificate behind a signed link
    SecurePage.vb             auth + CSRF + no-cache base page
    Web.config / Web.Release.config / Config/appSettings.example.config
sql/                          schema + procedure, least-privilege login, legacy migration
tests/
  SupplierPortal.Core.Tests/  28 xUnit tests written in VB.NET
  smoke-test.ps1              end-to-end test on IIS Express
```

## Run it

Requires Windows with the .NET Framework 4.8 developer pack and IIS Express (installed with Visual Studio).

```powershell
dotnet build
& "$env:ProgramFiles\IIS Express\iisexpress.exe" /path:"$PWD\src\SupplierPortal.Web" /port:8080
# http://localhost:8080  ->  demo mode (no database needed)
#   NIT 900.123.456 / andes-demo-2026
#   NIT 830987654   / puente-old-pass   (legacy SHA-256 account, upgraded on first login)
```

To use SQL Server, run `sql/01_schema.sql` and `sql/02_least_privilege_login.sql`, then copy `Config/appSettings.example.config` to `Config/appSettings.config` and fill in the connection string and signing key.

```powershell
dotnet test                       # 28 unit tests
pwsh tests/smoke-test.ps1         # end-to-end on IIS Express
```

## License

MIT
