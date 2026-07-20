#!/usr/bin/env pwsh
# ============================================================================
#  FinOS - Database Initialization Script (PowerShell / Windows)
#  Waits for SQL Server, then runs all schema, seed, SP, and view scripts
# ============================================================================

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   FinOS - Database Initialization" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Configuration (with defaults)
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
$SqlServerHost = if ($env:SQL_SERVER_HOST) { $env:SQL_SERVER_HOST } else { "localhost" }
$SqlServerPort = if ($env:SQL_SERVER_PORT) { $env:SQL_SERVER_PORT } else { "1433" }
$SqlServerSaPassword = if ($env:SQL_SERVER_SA_PASSWORD) { $env:SQL_SERVER_SA_PASSWORD } else { "CHANGE_ME_SQL_PASSWORD" }
$SqlServerDatabase = if ($env:SQL_SERVER_DATABASE) { $env:SQL_SERVER_DATABASE } else { "FinOS" }

# Try to load .env if it exists
$envFile = Join-Path (Split-Path $ScriptDir -Parent) "FinOS.Backend\.env"
if (Test-Path $envFile) {
    Get-Content $envFile | ForEach-Object {
        $line = $_.Trim()
        if ($line -and -not $line.StartsWith("#")) {
            $parts = $line -split "=", 2
            if ($parts.Length -eq 2) {
                $key = $parts[0].Trim()
                $value = $parts[1].Trim()
                Set-Item -Path "Env:$key" -Value $value
            }
        }
    }
    # Re-read after .env
    $SqlServerHost = if ($env:SQL_SERVER_HOST) { $env:SQL_SERVER_HOST } else { $SqlServerHost }
    $SqlServerPort = if ($env:SQL_SERVER_PORT) { $env:SQL_SERVER_PORT } else { $SqlServerPort }
    $SqlServerSaPassword = if ($env:SQL_SERVER_SA_PASSWORD) { $env:SQL_SERVER_SA_PASSWORD } else { $SqlServerSaPassword }
    $SqlServerDatabase = if ($env:SQL_SERVER_DATABASE) { $env:SQL_SERVER_DATABASE } else { $SqlServerDatabase }
}

Write-Host "  Server:   $SqlServerHost,$SqlServerPort" -ForegroundColor DarkGray
Write-Host "  Database: $SqlServerDatabase" -ForegroundColor DarkGray
Write-Host ""

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Check for sqlcmd
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
$sqlcmd = $null
try {
    $sqlcmdVersion = sqlcmd --version 2>$null
    if ($LASTEXITCODE -eq 0 -or $sqlcmdVersion) {
        $sqlcmd = "sqlcmd"
    }
} catch {}

if (-not $sqlcmd) {
    # Check common install paths
    $sqlcmdPaths = @(
        "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\*\Tools\Binn\SQLCMD.EXE",
        "C:\Program Files\Microsoft SQL Server\*\Tools\Binn\SQLCMD.EXE",
        "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\*\Tools\Binn\SQLCMD.EXE"
    )

    foreach ($path in $sqlcmdPaths) {
        $found = Get-Item $path -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) {
            $sqlcmd = $found.FullName
            break
        }
    }
}

if (-not $sqlcmd) {
    Write-Host "ERROR: sqlcmd not found!" -ForegroundColor Red
    Write-Host "  Install SQL Server Command Line Utilities:" -ForegroundColor DarkYellow
    Write-Host "    https://learn.microsoft.com/en-us/sql/tools/sqlcmd/sqlcmd-utility" -ForegroundColor DarkYellow
    Write-Host ""
    Write-Host "  Or use the Docker container:" -ForegroundColor DarkYellow
    Write-Host "    docker exec -it finos-sqlserver /opt/mssql-tools18/bin/sqlcmd" -ForegroundColor DarkYellow
    Write-Host ""
    exit 1
}

Write-Host "  Using sqlcmd: $sqlcmd" -ForegroundColor DarkGray
Write-Host ""

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Wait for SQL Server to be available
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Write-Host "--- Waiting for SQL Server ---" -ForegroundColor Yellow

$maxWaitSeconds = 120
$waited = 0
$connected = $false

while ($waited -lt $maxWaitSeconds) {
    try {
        # -I enables QUOTED_IDENTIFIER (required by filtered indexes + JSON string literals)
        $result = & $sqlcmd -S "$SqlServerHost,$SqlServerPort" -U sa -P $SqlServerSaPassword -C -I -Q "SELECT 1" 2>&1
        if ($LASTEXITCODE -eq 0) {
            $connected = $true
            break
        }
    } catch {}

    Write-Host "  Waiting for SQL Server... ($waited/$maxWaitSeconds seconds)" -ForegroundColor DarkGray
    Start-Sleep -Seconds 3
    $waited += 3
}

if ($connected) {
    Write-Host "  [OK] SQL Server is available!" -ForegroundColor Green
} else {
    Write-Host "ERROR: Could not connect to SQL Server after $maxWaitSeconds seconds" -ForegroundColor Red
    exit 1
}

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Create Database
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Write-Host ""
Write-Host "--- Creating Database ---" -ForegroundColor Yellow

& $sqlcmd -S "$SqlServerHost,$SqlServerPort" -U sa -P $SqlServerSaPassword -C -I -Q "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name='$SqlServerDatabase') CREATE DATABASE [$SqlServerDatabase];"

if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] Database [$SqlServerDatabase] ensured" -ForegroundColor Green
} else {
    Write-Host "  [FAIL] Could not create database" -ForegroundColor Red
    exit 1
}

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Helper function to run a SQL script
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
$errorCount = 0

function Run-SqlFile {
    param(
        [string]$FilePath
    )

    $fileName = Split-Path $FilePath -Leaf

    if (-not (Test-Path $FilePath)) {
        Write-Host "  [SKIP] File not found: $fileName" -ForegroundColor DarkYellow
        return
    }

    Write-Host "  Running: $fileName" -ForegroundColor DarkGray
    & $sqlcmd -S "$SqlServerHost,$SqlServerPort" -U sa -P $SqlServerSaPassword -d $SqlServerDatabase -C -I -i $FilePath -b 2>&1 | Out-Null

    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [OK] $fileName" -ForegroundColor Green
    } else {
        Write-Host "  [FAIL] $fileName" -ForegroundColor Red
        $script:errorCount++
    }
}

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Step 1: Run Schema Scripts (001-008)
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Write-Host ""
Write-Host "--- Step 1: Schema Scripts (001-008) ---" -ForegroundColor Yellow

$schemaPrefixes = @("001", "001b", "002", "003", "004", "005", "006", "007", "008")

foreach ($prefix in $schemaPrefixes) {
    $files = Get-ChildItem -Path "$ScriptDir\Schema\${prefix}_*.sql" -ErrorAction SilentlyContinue
    if ($files) {
        foreach ($file in $files) {
            Run-SqlFile -FilePath $file.FullName
        }
    } else {
        Write-Host "  [SKIP] No schema script found for prefix: $prefix" -ForegroundColor DarkYellow
    }
}

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Step 2: Run Seed Data (001-003)
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Write-Host ""
Write-Host "--- Step 2: Seed Data (001-003) ---" -ForegroundColor Yellow

$seedPrefixes = @("001", "002", "003")

foreach ($prefix in $seedPrefixes) {
    $files = Get-ChildItem -Path "$ScriptDir\SeedData\${prefix}_*.sql" -ErrorAction SilentlyContinue
    if ($files) {
        foreach ($file in $files) {
            Run-SqlFile -FilePath $file.FullName
        }
    } else {
        Write-Host "  [SKIP] No seed data script found for prefix: $prefix" -ForegroundColor DarkYellow
    }
}

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Step 3: Run Stored Procedures
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Write-Host ""
Write-Host "--- Step 3: Stored Procedures ---" -ForegroundColor Yellow

$spFiles = @("Security_sp", "Core_sp", "Budget_sp", "Investment_sp", "Loan_sp", "Goals_sp", "Analytics_sp")

foreach ($sp in $spFiles) {
    $filePath = Join-Path $ScriptDir "StoredProcedures\$sp.sql"
    if (Test-Path $filePath) {
        Run-SqlFile -FilePath $filePath
    } else {
        Write-Host "  [SKIP] Stored procedure file not found: $sp.sql" -ForegroundColor DarkYellow
    }
}

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Step 4: Run Views
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Write-Host ""
Write-Host "--- Step 4: Views ---" -ForegroundColor Yellow

$viewFiles = @("Dashboard_Views", "Analytics_Views", "Loan_Views", "Budget_Views", "Admin_Views", "Investment_Views")

foreach ($view in $viewFiles) {
    $filePath = Join-Path $ScriptDir "Views\$view.sql"
    if (Test-Path $filePath) {
        Run-SqlFile -FilePath $filePath
    } else {
        Write-Host "  [SKIP] View file not found: $view.sql" -ForegroundColor DarkYellow
    }
}

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Summary
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
if ($errorCount -eq 0) {
    Write-Host "   Database Initialization Complete!" -ForegroundColor Green
    Write-Host "   0 errors" -ForegroundColor Green
} else {
    Write-Host "   Database Initialization Complete with $errorCount error(s)" -ForegroundColor DarkYellow
}
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Database:          $SqlServerDatabase" -ForegroundColor DarkGray
Write-Host "  Schema scripts:    001-008" -ForegroundColor DarkGray
Write-Host "  Seed data:         001-003" -ForegroundColor DarkGray
Write-Host "  Stored procedures: Security_sp, Core_sp, Budget_sp, Investment_sp, Loan_sp, Goals_sp, Analytics_sp" -ForegroundColor DarkGray
Write-Host "  Views:             Dashboard_Views, Analytics_Views, Loan_Views, Budget_Views, Admin_Views, Investment_Views" -ForegroundColor DarkGray
Write-Host ""

if ($errorCount -gt 0) {
    exit 1
}
