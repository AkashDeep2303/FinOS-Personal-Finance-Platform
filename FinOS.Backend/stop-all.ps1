<#
.SYNOPSIS
    FinOS - Stop All Services (Windows PowerShell)
.DESCRIPTION
    Stops all FinOS .NET processes, Docker infrastructure containers,
    and Node.js (Vue/Vite) processes.
.NOTES
    Run as: .\stop-all.ps1
#>

$ErrorActionPreference = "Continue"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host ""
Write-Host "  FinOS - Stopping All Services" -ForegroundColor Yellow
Write-Host ""

# ============================================================================
# Step 1: Stop .NET microservice processes
# ============================================================================
Write-Host "--- Stopping .NET microservices ---" -ForegroundColor Yellow

$dotnetProcessNames = @(
    "FinOS.Gateway",
    "FinOS.Identity.API",
    "FinOS.CoreFinance.API",
    "FinOS.Budget.API",
    "FinOS.Investment.API",
    "FinOS.Loan.API",
    "FinOS.Goals.API",
    "FinOS.Analytics.API",
    "FinOS.AIAssistant.API",
    "FinOS.Notification.API"
)

$dotnetStopped = 0
foreach ($name in $dotnetProcessNames) {
    $running = Get-Process -Name $name -ErrorAction SilentlyContinue
    if ($running) {
        Stop-Process -Name $name -Force -ErrorAction SilentlyContinue
        Write-Host "  [OK] Stopped $name" -ForegroundColor Green
        $dotnetStopped++
    }
}

# Also kill any "dotnet" process whose command line contains FinOS paths
$finosDotnet = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -match "FinOS\.(Gateway|Identity|CoreFinance|Budget|Investment|Loan|Goals|Analytics|AIAssistant|Notification)" }

if ($finosDotnet) {
    foreach ($proc in $finosDotnet) {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        Write-Host "  [OK] Stopped dotnet PID $($proc.Id)" -ForegroundColor Green
        $dotnetStopped++
    }
}

if ($dotnetStopped -eq 0) {
    Write-Host "  No .NET FinOS processes found (already stopped)" -ForegroundColor DarkGray
}

# ============================================================================
# Step 2: Stop Node.js / Vue frontend processes
# ============================================================================
Write-Host ""
Write-Host "--- Stopping Vue frontend (Node.js/Vite) ---" -ForegroundColor Yellow

$nodeStopped = 0

# Find node processes running Vite dev server on port 5173
$viteNodes = Get-Process -Name "node" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -match "vite" -or $_.CommandLine -match "5173" }

if ($viteNodes) {
    foreach ($proc in $viteNodes) {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        Write-Host "  [OK] Stopped node PID $($proc.Id) (Vite)" -ForegroundColor Green
        $nodeStopped++
    }
}

# Fallback: check for any process listening on port 5173
if ($nodeStopped -eq 0) {
    $port5173 = Get-NetTCPConnection -LocalPort 5173 -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique
    if ($port5173) {
        foreach ($pid in $port5173) {
            Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
            Write-Host "  [OK] Stopped PID $pid (port 5173)" -ForegroundColor Green
            $nodeStopped++
        }
    }
}

if ($nodeStopped -eq 0) {
    Write-Host "  No Vue/Vite processes found (already stopped)" -ForegroundColor DarkGray
}

# ============================================================================
# Step 3: Stop Docker infrastructure containers
# ============================================================================
Write-Host ""
Write-Host "--- Stopping Docker infrastructure ---" -ForegroundColor Yellow

Push-Location $ScriptDir
try {
    $composeFile = Join-Path $ScriptDir "docker-compose.infra.yml"
    if (Test-Path $composeFile) {
        docker compose -f $composeFile down 2>&1 | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray }
        Write-Host "  [OK] Docker infrastructure stopped" -ForegroundColor Green
    } else {
        Write-Host "  [WARN] docker-compose.infra.yml not found" -ForegroundColor DarkYellow
        # Try stopping containers directly
        $containers = @("finos-sqlserver", "finos-redis", "finos-db-init")
        foreach ($c in $containers) {
            $exists = docker ps -q -f "name=$c" 2>$null
            if ($exists) {
                docker stop $c 2>$null
                Write-Host "  [OK] Stopped container $c" -ForegroundColor Green
            }
        }
    }
} catch {
    Write-Host "  [WARN] Error stopping Docker: $_" -ForegroundColor DarkYellow
}
Pop-Location

# ============================================================================
# Summary
# ============================================================================
Write-Host ""
Write-Host "  +--------------------------------------------------+" -ForegroundColor Green
Write-Host "  |  All FinOS services stopped.                      |" -ForegroundColor Green
Write-Host "  |  .NET processes: $dotnetStopped stopped" -ForegroundColor Green
Write-Host "  |  Node processes: $nodeStopped stopped" -ForegroundColor Green
Write-Host "  |  Docker:         infrastructure down               |" -ForegroundColor Green
Write-Host "  +--------------------------------------------------+" -ForegroundColor Green
Write-Host ""
