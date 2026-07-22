<#
.SYNOPSIS
    FinOS - Start All Services (Windows PowerShell)
.DESCRIPTION
    Starts infrastructure via Docker, initializes the database, builds the .NET solution,
    launches all 9 microservices + gateway, and starts the Vue frontend.
.NOTES
    Run as: .\start-all.ps1
    Requires: Docker Desktop, .NET 8 SDK, Node.js 18+
#>

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# ============================================================================
# Configuration
# ============================================================================
$envFile = Join-Path $ScriptDir '.env'
$SaPassword = $null
if (Test-Path $envFile) {
    $passwordLine = Get-Content $envFile | Where-Object { $_ -match '^SQL_SERVER_SA_PASSWORD=' } | Select-Object -First 1
    if ($passwordLine) { $SaPassword = ($passwordLine -split '=', 2)[1].Trim() }
}
if ([string]::IsNullOrWhiteSpace($SaPassword) -or $SaPassword -eq 'CHANGE_ME_SQL_PASSWORD') {
    Write-Host '  [FAIL] SQL_SERVER_SA_PASSWORD is missing or still a placeholder in .env' -ForegroundColor Red
    exit 1
}
$SqlHost    = "localhost"
$SqlPort    = 1433
$Database   = "FinOS"

# Pass the same SQL credentials to every child service.
$previousConnectionString = $env:ConnectionStrings__DefaultConnection
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=$Database;User Id=sa;Password=$SaPassword;TrustServerCertificate=True;MultipleActiveResultSets=True"

$Services = @(
    @{ Path = "APIGateways\FinOS.Gateway";                   Port = 8000; Name = "Gateway"       },
    @{ Path = "Services\Identity\FinOS.Identity.API";        Port = 5001; Name = "Identity"      },
    @{ Path = "Services\CoreFinance\FinOS.CoreFinance.API";  Port = 5002; Name = "CoreFinance"   },
    @{ Path = "Services\Budget\FinOS.Budget.API";            Port = 5003; Name = "Budget"        },
    @{ Path = "Services\Investment\FinOS.Investment.API";    Port = 5004; Name = "Investment"    },
    @{ Path = "Services\Loan\FinOS.Loan.API";                Port = 5005; Name = "Loan"          },
    @{ Path = "Services\Goals\FinOS.Goals.API";              Port = 5006; Name = "Goals"         },
    @{ Path = "Services\Analytics\FinOS.Analytics.API";      Port = 5007; Name = "Analytics"     },
    @{ Path = "Services\AIAssistant\FinOS.AIAssistant.API";  Port = 5008; Name = "AI Assistant"  },
    @{ Path = "Services\Notification\FinOS.Notification.API"; Port = 5009; Name = "Notification" }
)

# ============================================================================
# Banner
# ============================================================================
Write-Host ""
Write-Host "  +==================================================+" -ForegroundColor Cyan
Write-Host "  |          FinOS - Start All Services               |" -ForegroundColor Cyan
Write-Host "  |     Personal Finance Management System            |" -ForegroundColor Cyan
Write-Host "  +==================================================+" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# Step 1: Check Prerequisites
# ============================================================================
Write-Host "--- Step 1/6: Checking prerequisites ---" -ForegroundColor Yellow

$prereqs = @(
    @{ Name = "Docker";      Cmd = "docker";  Url = "https://docs.docker.com/get-docker/" },
    @{ Name = ".NET 8 SDK";  Cmd = "dotnet";  Url = "https://dotnet.microsoft.com/download/dotnet/8.0" },
    @{ Name = "Node.js";     Cmd = "node";    Url = "https://nodejs.org/" },
    @{ Name = "npm";         Cmd = "npm";     Url = "https://nodejs.org/" }
)

$prereqFailed = $false
foreach ($p in $prereqs) {
    if (Get-Command $p.Cmd -ErrorAction SilentlyContinue) {
        $version = & $p.Cmd --version 2>$null
        Write-Host "  [OK] $($p.Name) found ($($version.Split([Environment]::NewLine)[0]))" -ForegroundColor Green
    } else {
        Write-Host "  [FAIL] $($p.Name) NOT found. Install: $($p.Url)" -ForegroundColor Red
        $prereqFailed = $true
    }
}

if ($prereqFailed) {
    Write-Host ""
    Write-Host "  Please install missing prerequisites and try again." -ForegroundColor Red
    exit 1
}

# Verify .NET 8 specifically
$dotnetVersion = (dotnet --version 2>$null)
if ($dotnetVersion -notlike "8.*") {
    Write-Host "  [WARN] .NET SDK version is $dotnetVersion, expected 8.x" -ForegroundColor DarkYellow
}
Write-Host ""

# ============================================================================
# Step 2: Start Infrastructure via Docker Compose
# ============================================================================
Write-Host "--- Step 2/6: Starting infrastructure (SQL Server, Redis) ---" -ForegroundColor Yellow

Push-Location $ScriptDir
try {
    $ErrorActionPreference = 'Continue'
    $composeOutput = docker-compose -f docker-compose.infra.yml up -d 2>&1
    $composeExitCode = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    
    foreach ($line in $composeOutput) { Write-Host "  $line" -ForegroundColor DarkGray }
    if ($composeExitCode -ne 0) {
        Write-Host "  [FAIL] docker-compose exited with code $composeExitCode" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Write-Host "  [OK] Infrastructure containers started" -ForegroundColor Green
} catch {
    Write-Host "  [FAIL] Could not start infrastructure: $_" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location

# ============================================================================
# Step 3: Wait for SQL Server to be healthy
# ============================================================================
Write-Host ""
Write-Host "--- Step 3/6: Waiting for SQL Server to be healthy (up to 180s) ---" -ForegroundColor Yellow

$sqlReady = $false
$retries  = 0
$maxRetries = 36

while ($retries -lt $maxRetries) {
    try {
        $result = docker exec finos-sqlserver /opt/mssql-tools18/bin/sqlcmd `
            -S localhost -U sa -P $SaPassword -C -Q "SELECT 1" 2>$null
        if ($LASTEXITCODE -eq 0) {
            $sqlReady = $true
            break
        }
    } catch {}

    $retries++
    Write-Host "  Waiting... ($retries/$maxRetries)" -ForegroundColor DarkGray
    Start-Sleep -Seconds 5
}

if ($sqlReady) {
    Write-Host "  [OK] SQL Server is ready" -ForegroundColor Green
} else {
    Write-Host "  [FAIL] SQL Server did not become healthy in time" -ForegroundColor Red
    Write-Host "  Check: docker logs finos-sqlserver" -ForegroundColor DarkYellow
    exit 1
}

# Wait for DB init container to complete
Write-Host "  Waiting for database initialization container..." -ForegroundColor DarkGray
$initWait = 0
while ($initWait -lt 60) {
    $initRunning = docker ps --filter "name=finos-db-init" --format "{{.Names}}" 2>$null
    if ([string]::IsNullOrWhiteSpace($initRunning)) {
        break
    }
    Start-Sleep -Seconds 3
    $initWait += 3
    Write-Host -NoNewline "."
}
Write-Host ""
Write-Host "  [OK] Database initialization container finished" -ForegroundColor Green

# ============================================================================
# Step 4: Build the .NET Solution
# ============================================================================
Write-Host ""
Write-Host "--- Step 4/6: Building .NET solution ---" -ForegroundColor Yellow

Push-Location $ScriptDir
try {
    Write-Host "  Restoring NuGet packages..." -ForegroundColor DarkGray
    dotnet restore FinOS.sln --verbosity quiet 2>&1 | Out-Null

    Write-Host "  Building solution (Debug)..." -ForegroundColor DarkGray
    dotnet build FinOS.sln --configuration Debug --verbosity quiet 2>&1 | Out-Null

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [FAIL] Build failed. Running verbose build for details..." -ForegroundColor Red
        dotnet build FinOS.sln --configuration Debug
        Pop-Location
        exit 1
    }

    Write-Host "  [OK] Build successful" -ForegroundColor Green
} catch {
    Write-Host "  [FAIL] Build error: $_" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location

# ============================================================================
# Step 5: Start All .NET Microservices + Gateway
# ============================================================================
Write-Host ""
Write-Host "--- Step 5/6: Starting microservices in separate windows ---" -ForegroundColor Yellow

$startedServices = @()

foreach ($svc in $Services) {
    $projectPath = Join-Path $ScriptDir $svc.Path
    if (-not (Test-Path $projectPath)) {
        Write-Host "  [SKIP] $($svc.Name) - project not found at $($svc.Path)" -ForegroundColor DarkYellow
        continue
    }

    Write-Host "  Starting $($svc.Name) on port $($svc.Port)..." -ForegroundColor Cyan

    Start-Process -FilePath "dotnet" `
        -ArgumentList "run","--project",$svc.Path,"--urls","http://localhost:$($svc.Port)","--no-build" `
        -WorkingDirectory $ScriptDir `
        -WindowStyle Normal

    $startedServices += $svc
    Start-Sleep -Milliseconds 800
}

Write-Host "  [OK] $($startedServices.Count) service(s) launched" -ForegroundColor Green

# ============================================================================
# Step 6: Start Vue Frontend
# ============================================================================
Write-Host ""
$env:ConnectionStrings__DefaultConnection = $previousConnectionString
Write-Host "--- Step 6/6: Starting Vue frontend ---" -ForegroundColor Yellow

$frontendDir = Join-Path (Split-Path $ScriptDir -Parent) "FinOS.Frontend"
if (Test-Path $frontendDir) {
    Push-Location $frontendDir
    if (-not (Test-Path "node_modules")) {
        Write-Host "  Installing npm dependencies (first time)..." -ForegroundColor DarkGray
        npm install --silent 2>$null
    }

    # Local Kestrel uses the gateway on port 8000; override the IIS .env value for this process only.
    $previousViteApiBaseUrl = $env:VITE_API_BASE_URL
    $env:VITE_API_BASE_URL = 'http://localhost:8000'
    Start-Process -FilePath "npm" `
        -ArgumentList "run","dev" `
        -WorkingDirectory $frontendDir `
        -WindowStyle Normal
    if ($null -eq $previousViteApiBaseUrl) { Remove-Item Env:VITE_API_BASE_URL -ErrorAction SilentlyContinue } else { $env:VITE_API_BASE_URL = $previousViteApiBaseUrl }

    Pop-Location
    Write-Host "  [OK] Frontend started on http://localhost:5173" -ForegroundColor Green
} else {
    Write-Host "  [WARN] Frontend directory not found at $frontendDir" -ForegroundColor DarkYellow
    Write-Host "  Clone or extract FinOS.Frontend to the parent directory" -ForegroundColor DarkYellow
}

# ============================================================================
# Status Dashboard
# ============================================================================
Write-Host ""
Write-Host ""
Write-Host "  +==================================================+" -ForegroundColor Cyan
Write-Host "  |          FinOS Services Running                   |" -ForegroundColor Cyan
Write-Host "  +==================================================+" -ForegroundColor Cyan
Write-Host "  |                                                    |" -ForegroundColor Cyan
Write-Host "  |  Infrastructure:                                   |" -ForegroundColor Cyan
Write-Host "  |    SQL Server:   localhost:1433                    |" -ForegroundColor Cyan
Write-Host "  |    Redis:        localhost:6379                    |" -ForegroundColor Cyan
Write-Host "  |                                                    |" -ForegroundColor Cyan
Write-Host "  |  .NET Microservices:                               |" -ForegroundColor Cyan
Write-Host "  |    Gateway:      http://localhost:8000             |" -ForegroundColor Cyan
Write-Host "  |    Identity:     http://localhost:5001             |" -ForegroundColor Cyan
Write-Host "  |    CoreFinance:  http://localhost:5002             |" -ForegroundColor Cyan
Write-Host "  |    Budget:       http://localhost:5003             |" -ForegroundColor Cyan
Write-Host "  |    Investment:   http://localhost:5004             |" -ForegroundColor Cyan
Write-Host "  |    Loan:         http://localhost:5005             |" -ForegroundColor Cyan
Write-Host "  |    Goals:        http://localhost:5006             |" -ForegroundColor Cyan
Write-Host "  |    Analytics:    http://localhost:5007             |" -ForegroundColor Cyan
Write-Host "  |    AI:           http://localhost:5008             |" -ForegroundColor Cyan
Write-Host "  |    Notification: http://localhost:5009             |" -ForegroundColor Cyan
Write-Host "  |                                                    |" -ForegroundColor Cyan
Write-Host "  |  Frontend:                                         |" -ForegroundColor Cyan
Write-Host "  |    Vue App:      http://localhost:5173             |" -ForegroundColor Cyan
Write-Host "  |                                                    |" -ForegroundColor Cyan
Write-Host "  |  Swagger UI:                                       |" -ForegroundColor Cyan
Write-Host "  |    http://localhost:{PORT}/swagger                 |" -ForegroundColor Cyan
Write-Host "  |                                                    |" -ForegroundColor Cyan
Write-Host "  +==================================================+" -ForegroundColor Cyan
Write-Host ""
Write-Host "  To stop all services:  .\stop-all.ps1" -ForegroundColor Yellow
Write-Host "  To re-init database:   .\setup-database.ps1" -ForegroundColor Yellow
Write-Host "  Quick restart:         .\quick-start.ps1" -ForegroundColor Yellow
Write-Host ""
