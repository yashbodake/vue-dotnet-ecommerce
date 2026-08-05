# dev.ps1 - lifecycle manager for the Ecommerce-Redo shop (Vue + .NET 10)
#
#   ./dev.ps1 start   [api|frontend|all]   # default = all
#   ./dev.ps1 stop    [api|frontend|all]
#   ./dev.ps1 restart [api|frontend|all]
#   ./dev.ps1 status                        # show running services
#   ./dev.ps1 logs    <api|frontend>        # tail a log (Ctrl-C to exit)
#   ./dev.ps1 down                          # stop everything

param(
    [Parameter(Position=0)] [string] $Command = 'status',
    [Parameter(Position=1)] [string] $Service = 'all'
)

$ErrorActionPreference = 'SilentlyContinue'

$Root   = Split-Path -Parent $MyInvocation.MyCommand.Path
$ApiDir = Join-Path $Root 'src\backend\Ecommerce.Api'
$FeDir  = Join-Path $Root 'src\frontend'
$RunDir = Join-Path $Root '.run'
$LogDir = Join-Path $RunDir 'logs'
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null

# .NET 10 is a user install; put it on PATH.
$DotnetPath = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet'
if (Test-Path $DotnetPath) { $env:PATH = "$DotnetPath;$env:PATH" }

# Database: use shared memory by default (works even with SQL TCP/NamedPipes disabled).
if (-not $env:EC_DB_SERVER) { $env:EC_DB_SERVER = 'lpc:.\SQLEXPRESS' }
$env:ConnectionStrings__DefaultConnection = `
    "Server=$env:EC_DB_SERVER;Database=LegacyEcommerceDb;Trusted_Connection=True;TrustServerCertificate=True;Pooling=true;Min Pool Size=2;Max Pool Size=50;Connect Timeout=10;Application Name=Ecommerce.Api"
$env:ASPNETCORE_ENVIRONMENT = 'Development'

$ApiPort = 5100
$FePort  = 5173

function Get-PortPid($port) {
    $c = Get-NetTCPConnection -LocalPort $port -State Listen
    if ($c) { return [int]$c[0].OwningProcess }
    return 0
}

function Test-PortUp($port, $path) {
    try {
        $r = Invoke-WebRequest "http://localhost:$port$path" -UseBasicParsing -TimeoutSec 3
        return $r.StatusCode -lt 400
    } catch { return $false }
}

function Wait-Port($name, $port, $path, $secs) {
    for ($i = 1; $i -le $secs; $i++) {
        if (Test-PortUp $port $path) {
            Write-Host "[OK] $name up -> http://localhost:$port" -ForegroundColor Green
            return $true
        }
        Start-Sleep -Seconds 1
    }
    Write-Host "[!] $name not responding after ${secs}s (check .run\logs\${name}.log)" -ForegroundColor Yellow
    return $true
}

function Start-Api {
    if ((Get-PortPid $ApiPort) -ne 0) { Write-Host "[!] api already running"; return }
    Write-Host "== Starting api on :$ApiPort ==" -ForegroundColor White
    Push-Location $ApiDir
    & dotnet build --nologo -v quiet 2>&1 | Out-Null
    Pop-Location
    $logf = Join-Path $LogDir 'api.log'
    $p = Start-Process dotnet -ArgumentList 'run','--no-build',"--urls","http://127.0.0.1:$ApiPort" `
        -WorkingDirectory $ApiDir -WindowStyle Hidden -PassThru -RedirectStandardOutput $logf -RedirectStandardError "$logf.err"
    Set-Content (Join-Path $RunDir 'api.pid') $p.Id
    Wait-Port 'api' $ApiPort '/api/health' 45
}

function Start-Frontend {
    if ((Get-PortPid $FePort) -ne 0) { Write-Host "[!] frontend already running"; return }
    if (-not (Test-Path (Join-Path $FeDir 'node_modules'))) {
        Write-Host "== Installing frontend deps =="
        Push-Location $FeDir; & npm install; Pop-Location
    }
    Write-Host "== Starting frontend on :$FePort ==" -ForegroundColor White
    $logf = Join-Path $LogDir 'frontend.log'
    # Use npm.cmd explicitly - Start-Process with the .ps1/.cmd shim resolution is unreliable here.
    $p = Start-Process 'npm.cmd' -ArgumentList 'run','dev','--',"--port",$FePort,'--strictPort' `
        -WorkingDirectory $FeDir -WindowStyle Hidden -PassThru -RedirectStandardOutput $logf -RedirectStandardError "$logf.err"
    Set-Content (Join-Path $RunDir 'frontend.pid') $p.Id
    Wait-Port 'frontend' $FePort '/' 40
}

function Stop-Port($port) {
    $p = Get-PortPid $port
    if ($p -ne 0) { Stop-Process -Id $p -Force; & taskkill /PID $p /F /T 2>&1 | Out-Null }
    Start-Sleep -Milliseconds 500
}

function Stop-Api      { Write-Host "== Stopping api ==";       Stop-Port $ApiPort }
function Stop-Frontend { Write-Host "== Stopping frontend ==";  Stop-Port $FePort }

function Show-Status {
    Write-Host "== Ecommerce-Redo status ==" -ForegroundColor White
    foreach ($s in @(@('api',$ApiPort,'/api/health'), @('frontend',$FePort,'/'))) {
        $name = $s[0]; $port = $s[1]; $path = $s[2]
        if (Test-PortUp $port $path) {
            Write-Host ("  {0,-10} :{1,-5} up    (healthy)" -f $name,$port) -ForegroundColor Green
        } elseif ((Get-PortPid $port) -ne 0) {
            Write-Host ("  {0,-10} :{1,-5} up?   (starting)" -f $name,$port) -ForegroundColor Yellow
        } else {
            Write-Host ("  {0,-10} :{1,-5} down" -f $name,$port) -ForegroundColor Red
        }
    }
    Write-Host ""
    Write-Host "  DB: $env:EC_DB_SERVER"
}

function Show-Logs($svc) {
    $logf = Join-Path $LogDir "$svc.log"
    if (Test-Path $logf) { Get-Content $logf -Tail 50 -Wait } else { Write-Host "no log for $svc" }
}

function Get-Targets($svc) {
    if ($svc -eq 'all' -or -not $svc) { return @('api','frontend') }
    return @($svc)
}

switch ($Command) {
    'start'   { foreach ($t in (Get-Targets $Service)) { if ($t -eq 'api') { Start-Api } elseif ($t -eq 'frontend') { Start-Frontend } } }
    'stop'    { foreach ($t in (Get-Targets $Service)) { if ($t -eq 'api') { Stop-Api } elseif ($t -eq 'frontend') { Stop-Frontend } } }
    'restart' { foreach ($t in (Get-Targets $Service)) { if ($t -eq 'api') { Stop-Api } elseif ($t -eq 'frontend') { Stop-Frontend } }; Start-Sleep 1; foreach ($t in (Get-Targets $Service)) { if ($t -eq 'api') { Start-Api } elseif ($t -eq 'frontend') { Start-Frontend } } }
    'status'  { Show-Status }
    'logs'    { Show-Logs $Service }
    'down'    { Stop-Api; Stop-Frontend }
    default   { Write-Host "Commands: start | stop | restart | status | logs <api|frontend> | down" }
}
