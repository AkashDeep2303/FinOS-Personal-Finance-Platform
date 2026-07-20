<#
.SYNOPSIS
    FinOS - Quick Start (Windows PowerShell)
.DESCRIPTION
    One-command script to start the entire FinOS system from scratch:
      1. Start Docker infrastructure
      2. Wait for SQL Server
      3. Initialize database
      4. Build backend
      5. Start all services
      6. Start frontend
.NOTES
    Run as: .\quick-start.ps1
    Requires: Docker Desktop, .NET 8 SDK, Node.js 18+
#>

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$SaPassword = "CHANGE_ME_SQL_PASSWORD"

$Services = @(
    @{ Path = "APIGateways\FinOS.Gateway";                   Port = 6000; Name = "Gateway"       },
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
Write-Host "  ****************************************************************" -ForegroundColor Magenta
Write-Host "  *         FinOS - Quick Start (One Command)                    *" -ForegroundColor Magenta
Write-Host "  *   This will set up and start the entire FinOS system         *" -ForegroundColor Magenta
Write-Host "  ****************************************************************" -ForegroundColor Magenta
Write-Host ""

$startTime = Get-Date

# ============================================================================
# Quick Prereq Check
# ============================================================================
Write-Host "[1/6] Quick prerequisite check..." -ForegroundColor Yellow -NoNewline
$missing = @()
foreach ($cmd in @("docker", "dotnet", "node", "npm")) {
    if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
        $missing += $cmd
    }
}
if ($missing.Count -gt 0) {
    Write-Host " FAIL" -ForegroundColor Red
    Write-Host "  Missing: $($missing -join ', ')" -ForegroundColor Red
    exit 1
}

# Check Docker daemon
try { docker info *> $null } catch {
    Write-Host " FAIL" -ForegroundColor Red
    Write-Host "  Docker daemon is not running" -ForegroundColor Red
    exit 1
}
Write-Host " OK" -ForegroundColor Green

# ============================================================================
# Start Docker Infrastructure
# ============================================================================
Write-Host "[2/6] Starting Docker infrastructure..." -ForegroundColor Yellow -NoNewline
Push-Location $ScriptDir
docker compose -f docker-compose.infra.yml up -d 2>&1 | Out-Null
Pop-Location
Write-Host " OK" -ForegroundColor Green

# ============================================================================
# Wait for SQL Server
# ============================================================================
Write-Host "[3/6] Waiting for SQL Server..." -ForegroundColor Yellow
$retries = 0
$sqlReady = $false
while ($retries -lt 36) {
    try {
        $null = docker exec finos-sqlserver /opt/mssql-tools18/bin/sqlcmd `
            -S localhost -U sa -P $SaPassword -C -Q "SELECT 1" 2>$null
        if ($LASTEXITCODE -eq 0) {
            $sqlReady = $true
            break
        }
    } catch {}
    $retries++
    Write-Host -NoNewline "."
    Start-Sleep -Seconds 5
}

if (-not $sqlReady) {
    Write-Host " FAIL" -ForegroundColor Red
    Write-Host "  SQL Server did not start in time" -ForegroundColor Red
    exit 1
}
Write-Host " OK" -ForegroundColor Green

# Wait for init container
$initWait = 0
while ($initWait -lt 60) {
    $initRunning = docker ps --filter "name=finos-db-init" --format "{{.Names}}" 2>$null
    if ([string]::IsNullOrWhiteSpace($initRunning)) { break }
    Start-Sleep -Seconds 3
    $initWait += 3
    Write-Host -NoNewline "."
}

# If init container didn't run or finished, also run setup-database.ps1 if it exists
# This ensures schema/seed/SPs/views are set up even if init container had issues
$setupScript = Join-Path $ScriptDir "setup-database.ps1"
if (Test-Path $setupScript) {
    Write-Host ""
    Write-Host "       Running database setup script..." -ForegroundColor DarkGray
    & $setupScript -UseDocker
    if ($LASTEXITCODE -ne 0) {
        Write-Host "       [WARN] Database setup had errors (init container may have already run)" -ForegroundColor DarkYellow
    }
}

# ============================================================================
# Build Backend
# ============================================================================
Write-Host "[4/6] Building .NET solution..." -ForegroundColor Yellow -NoNewline
Push-Location $ScriptDir
dotnet restore FinOS.sln --verbosity quiet 2>&1 | Out-Null
dotnet build FinOS.sln --configuration Debug --verbosity quiet 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host " FAIL" -ForegroundColor Red
    Write-Host "  Build failed. Run: dotnet build FinOS.sln" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location
Write-Host " OK" -ForegroundColor Green

# ============================================================================
# Start All Services
# ============================================================================
Write-Host "[5/6] Starting microservices..." -ForegroundColor Yellow
foreach ($svc in $Services) {
    $projectPath = Join-Path $ScriptDir $svc.Path
    if (Test-Path $projectPath) {
        Start-Process -FilePath "dotnet" `
            -ArgumentList "run","--project",$svc.Path,"--urls","http://localhost:$($svc.Port)","--no-build" `
            -WorkingDirectory $ScriptDir `
            -WindowStyle Normal
        Write-Host "       $($svc.Name) -> :$($svc.Port)" -ForegroundColor DarkGray
    } else {
        Write-Host "       $($svc.Name) -> SKIP (not found)" -ForegroundColor DarkYellow
    }
    Start-Sleep -Milliseconds 500
}
Write-Host "       Services launched" -ForegroundColor Green

# ============================================================================
# Start Frontend
# ============================================================================
Write-Host "[6/6] Starting Vue frontend..." -ForegroundColor Yellow -NoNewline
$frontendDir = Join-Path (Split-Path $ScriptDir -Parent) "FinOS.Frontend"
if (Test-Path $frontendDir) {
    if (-not (Test-Path "$frontendDir\node_modules")) {
        Push-Location $frontendDir
        npm install --silent 2>$null
        Pop-Location
    }
    Start-Process -FilePath "npm" -ArgumentList "run","dev" -WorkingDirectory $frontendDir -WindowStyle Normal
    Write-Host " OK" -ForegroundColor Green
} else {
    Write-Host " SKIP (frontend not found)" -ForegroundColor DarkYellow
}

# ============================================================================
# Done
# ============================================================================
$elapsed = ((Get-Date) - $startTime).ToString("mm\:ss")

Write-Host ""
Write-Host "  ****************************************************************" -ForegroundColor Green
Write-Host "  *  FinOS is running!  (took $elapsed)" -ForegroundColor Green
Write-Host "  ****************************************************************" -ForegroundColor Green
Write-Host ""
Write-Host "  Frontend:    http://localhost:5173" -ForegroundColor White
Write-Host "  Gateway:     http://localhost:6000" -ForegroundColor White
Write-Host "  Swagger:     http://localhost:5001/swagger (any service port)" -ForegroundColor White
Write-Host ""
Write-Host "  SQL Server:  localhost:1433  |  Redis: localhost:6379" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  Stop all:    .\stop-all.ps1" -ForegroundColor Yellow
Write-Host ""
