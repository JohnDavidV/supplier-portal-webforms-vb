# End-to-end smoke test: runs the built site on IIS Express (demo mode) and drives a real
# WebForms login with ViewState, then checks the pages and the signed-link handler.
#   pwsh tests/smoke-test.ps1
param([int]$Port = 8085)
$ErrorActionPreference = 'Stop'

$iis = "${env:ProgramFiles}\IIS Express\iisexpress.exe"
if (-not (Test-Path $iis)) { Write-Warning 'IIS Express not installed: smoke test skipped.'; exit 0 }

$site = Resolve-Path (Join-Path $PSScriptRoot '..\src\SupplierPortal.Web')
$logFile = Join-Path ([IO.Path]::GetTempPath()) 'iisexpress-smoke.log'
$proc = Start-Process $iis -ArgumentList "/path:$site", "/port:$Port", '/systray:false', '/trace:error' -PassThru `
    -RedirectStandardOutput $logFile -NoNewWindow
$base = "http://localhost:$Port"
try {
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $login = $null
    $lastError = ''
    foreach ($i in 1..45) {
        try { $login = Invoke-WebRequest "$base/Login.aspx" -WebSession $session -UseBasicParsing; break }
        catch {
            $lastError = $_.Exception.Message
            # ASP.NET returns the YSOD (compilation/config error details) for localhost requests
            if ($_.ErrorDetails.Message) { $lastError += "`n" + ($_.ErrorDetails.Message -replace '<[^>]+>', ' ' -replace '\s{2,}', ' ') }
            Start-Sleep 2
        }
    }
    if (-not $login) {
        Write-Host '--- last error ---'; Write-Host ($lastError.Substring(0, [Math]::Min(4000, $lastError.Length)))
        Write-Host '--- IIS Express output ---'; if (Test-Path $logFile) { Get-Content $logFile -Tail 60 }
        throw 'Site did not start'
    }
    if ($login.Content -notmatch 'Ingreso de proveedores') { throw 'Login page did not render' }
    if ($login.Headers['X-Frame-Options'] -ne 'DENY') { throw 'Security headers missing' }

    # Anonymous users are sent to the login page
    $anon = Invoke-WebRequest "$base/Default.aspx" -UseBasicParsing
    if ($anon.Content -notmatch 'Ingreso de proveedores') { throw 'Default.aspx is not protected' }

    function Field($html, $name) { [regex]::Match($html, "name=""$name""[^>]*value=""([^""]*)""").Groups[1].Value }
    function Uid($html, $suffix) { [regex]::Match($html, "name=""([^""]*\`$$suffix)""").Groups[1].Value }

    $form = @{
        '__VIEWSTATE'          = Field $login.Content '__VIEWSTATE'
        '__VIEWSTATEGENERATOR' = Field $login.Content '__VIEWSTATEGENERATOR'
        '__EVENTVALIDATION'    = Field $login.Content '__EVENTVALIDATION'
        (Uid $login.Content 'NitBox')       = '900.123.456'
        (Uid $login.Content 'PasswordBox')  = 'andes-demo-2026'
        (Uid $login.Content 'SignInButton') = 'Ingresar'
    }
    $dashboard = Invoke-WebRequest "$base/Login.aspx" -Method Post -Body $form -WebSession $session -UseBasicParsing
    if ($dashboard.Content -notmatch 'Mis pagos' -or $dashboard.Content -notmatch 'FE-') { throw 'Login or payments page failed' }

    $bad = try { Invoke-WebRequest "$base/Certificate.ashx?nit=900123456&year=2026&exp=9999999999&sig=forged" -UseBasicParsing } catch { $_.Exception.Response }
    if ([int]$bad.StatusCode -ne 403) { throw "Forged certificate link returned $([int]$bad.StatusCode)" }

    $config = try { Invoke-WebRequest "$base/Config/appSettings.example.config" -UseBasicParsing } catch { $_.Exception.Response }
    if ([int]$config.StatusCode -ne 404) { throw 'Config folder is reachable over HTTP' }

    Write-Host 'Smoke test passed: login (ViewState), payments, auth guard, signed links, hidden Config.' -ForegroundColor Green
} finally {
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
}
