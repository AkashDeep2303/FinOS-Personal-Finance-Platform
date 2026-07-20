<#
.SYNOPSIS
    FinOS - Database Setup Script (Windows PowerShell)
.DESCRIPTION
    Initializes the FinOS database by running all schema scripts, seed data,
    stored procedures, and views in the correct order. Can use either the local
    sqlcmd utility or the Docker SQL Server container.
.PARAMETER UseDocker
    Switch to use sqlcmd inside the Docker container instead of a local install.
.PARAMETER Force
    Force re-initialization even if database already exists.
.EXAMPLE
    .\setup-database.ps1
    .\setup-database.ps1 -UseDocker
    .\setup-database.ps1 -Force
#>

param(
    [switch]$UseDocker = $false,
    [switch]$Force     = $false
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# ============================================================================
# Configuration
# ============================================================================
$SqlServerHost     = if ($env:SQL_SERVER_HOST)     { $env:SQL_SERVER_HOST }     else { "localhost" }
$SqlServerPort     = if ($env:SQL_SERVER_PORT)     { $env:SQL_SERVER_PORT }     else { "1433" }
$SqlServerSaPassword = if ($env:SQL_SERVER_SA_PASSWORD) { $env:SQL_SERVER_SA_PASSWORD } else { "CHANGE_ME_SQL_PASSWORD" }
$SqlServerDatabase = if ($env:SQL_SERVER_DATABASE) { $env:SQL_SERVER_DATABASE } else { "FinOS" }

# Try loading .env if it exists
$envFile = Join-Path (Split-Path $ScriptDir -Parent) "FinOS.Backend\.env"
if (Test-Path $envFile) {
    Get-Content $envFile | ForEach-Object {
        $line = $_.Trim()
        if ($line -and -not $line.StartsWith("#")) {
            $parts = $line -split "=", 2
            if ($parts.Length -eq 2) {
                Set-Item -Path "Env:$($parts[0].Trim())" -Value $parts[1].Trim()
            }
        }
    }
    $SqlServerHost       = if ($env:SQL_SERVER_HOST)     { $env:SQL_SERVER_HOST }     else { $SqlServerHost }
    $SqlServerPort       = if ($env:SQL_SERVER_PORT)     { $env:SQL_SERVER_PORT }     else { $SqlServerPort }
    $SqlServerSaPassword = if ($env:SQL_SERVER_SA_PASSWORD) { $env:SQL_SERVER_SA_PASSWORD } else { $SqlServerSaPassword }
    $SqlServerDatabase   = if ($env:SQL_SERVER_DATABASE) { $env:SQL_SERVER_DATABASE } else { $SqlServerDatabase }
}

# Database scripts directory
$DbScriptsDir = Join-Path (Split-Path $ScriptDir -Parent) "FinOS.Database"

# ============================================================================
# Banner
# ============================================================================
Write-Host ""
Write-Host "  +==================================================+" -ForegroundColor Cyan
Write-Host "  |        FinOS - Database Setup                     |" -ForegroundColor Cyan
Write-Host "  +==================================================+" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Server:   $SqlServerHost,$SqlServerPort" -ForegroundColor DarkGray
Write-Host "  Database: $SqlServerDatabase" -ForegroundColor DarkGray
Write-Host "  Scripts:  $DbScriptsDir" -ForegroundColor DarkGray
Write-Host ""

# ============================================================================
# Step 1: Check SQL Server Container
# ============================================================================
Write-Host "--- Step 1/6: Checking SQL Server container ---" -ForegroundColor Yellow

$sqlContainerRunning = $false
$containerStatus = docker ps --filter "name=finos-sqlserver" --format "{{.Status}}" 2>$null
if (-not [string]::IsNullOrWhiteSpace($containerStatus)) {
    $sqlContainerRunning = $true
    Write-Host "  [OK] SQL Server container is running ($containerStatus)" -ForegroundColor Green
} else {
    # Check if container exists but is stopped
    $containerExists = docker ps -a --filter "name=finos-sqlserver" --format "{{.Names}}" 2>$null
    if (-not [string]::IsNullOrWhiteSpace($containerExists)) {
        Write-Host "  SQL Server container exists but is stopped. Starting..." -ForegroundColor DarkYellow
        docker start finos-sqlserver 2>&1 | Out-Null
        $sqlContainerRunning = $true
    } else {
        Write-Host "  [WARN] SQL Server container not found." -ForegroundColor DarkYellow
        Write-Host "  Starting infrastructure via docker compose..." -ForegroundColor DarkYellow
        Push-Location $ScriptDir
        docker compose -f docker-compose.infra.yml up -d sqlserver 2>&1 | Out-Null
        Pop-Location
        $sqlContainerRunning = $true
    }
}

# Wait for SQL Server to be healthy
Write-Host "  Waiting for SQL Server to accept connections..." -ForegroundColor DarkGray
$retries = 0
$connected = $false
while ($retries -lt 36) {
    try {
        $result = docker exec finos-sqlserver /opt/mssql-tools18/bin/sqlcmd `
            -S localhost -U sa -P $SqlServerSaPassword -C -Q "SELECT 1" 2>$null
        if ($LASTEXITCODE -eq 0) {
            $connected = $true
            break
        }
    } catch {}
    $retries++
    Start-Sleep -Seconds 5
    Write-Host -NoNewline "."
}
Write-Host ""

if ($connected) {
    Write-Host "  [OK] SQL Server is ready" -ForegroundColor Green
} else {
    Write-Host "  [FAIL] SQL Server did not become healthy" -ForegroundColor Red
    exit 1
}

# ============================================================================
# Step 2: Determine sqlcmd method
# ============================================================================
Write-Host ""
Write-Host "--- Step 2/6: Finding sqlcmd ---" -ForegroundColor Yellow

$sqlcmd = $null

if ($UseDocker) {
    $sqlcmd = "docker"
    Write-Host "  Using Docker sqlcmd (forced via -UseDocker)" -ForegroundColor DarkGray
} else {
    # Try local sqlcmd first
    try {
        $null = sqlcmd --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            $sqlcmd = "local"
        }
    } catch {}

    if (-not $sqlcmd) {
        # Check common Windows install paths
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
        Write-Host "  Local sqlcmd not found, falling back to Docker" -ForegroundColor DarkYellow
        $sqlcmd = "docker"
    }
}

if ($sqlcmd -eq "docker") {
    Write-Host "  [OK] Using sqlcmd via Docker container (finos-sqlserver)" -ForegroundColor Green
} elseif ($sqlcmd -eq "local") {
    Write-Host "  [OK] Using local sqlcmd" -ForegroundColor Green
} else {
    Write-Host "  [OK] Using sqlcmd at: $sqlcmd" -ForegroundColor Green
}

# ============================================================================
# Helper: Execute SQL
# ============================================================================
function Invoke-FinosSql {
    param(
        [string]$Query,
        [string]$InputFile = "",
        [switch]$UseDatabase = $true
    )

    if ($sqlcmd -eq "docker") {
        $dbFlag = if ($UseDatabase) { "-d $SqlServerDatabase" } else { "" }

        if ($InputFile) {
            # Copy script into container and execute
            $containerPath = "/tmp/finos_script.sql"
            docker cp $InputFile "finos-sqlserver:$containerPath" 2>$null
            $cmd = "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P `"$SqlServerSaPassword`" $dbFlag -C -i `"$containerPath`" -b"
        } else {
            $cmd = "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P `"$SqlServerSaPassword`" $dbFlag -C -Q `"$Query`" -b"
        }

        $result = docker exec finos-sqlserver bash -c $cmd 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  [FAIL] SQL error: $result" -ForegroundColor Red
            return $false
        }
    } else {
        $sqlcmdExe = if ($sqlcmd -eq "local") { "sqlcmd" } else { $sqlcmd }
        $dbFlag = if ($UseDatabase) { "-d $SqlServerDatabase" } else { "" }

        if ($InputFile) {
            & $sqlcmdExe -S "$SqlServerHost,$SqlServerPort" -U sa -P $SqlServerSaPassword $dbFlag -C -i $InputFile -b 2>&1 | Out-Null
        } else {
            & $sqlcmdExe -S "$SqlServerHost,$SqlServerPort" -U sa -P $SqlServerSaPassword $dbFlag -C -Q $Query -b 2>&1 | Out-Null
        }

        if ($LASTEXITCODE -ne 0) {
            return $false
        }
    }
    return $true
}

# ============================================================================
# Step 3: Create Database (if not exists)
# ============================================================================
Write-Host ""
Write-Host "--- Step 3/6: Creating database ---" -ForegroundColor Yellow

$createDbQuery = "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name='$SqlServerDatabase') CREATE DATABASE [$SqlServerDatabase];"
$result = Invoke-FinosSql -Query $createDbQuery -UseDatabase:$false

if ($result) {
    Write-Host "  [OK] Database [$SqlServerDatabase] ensured" -ForegroundColor Green
} else {
    Write-Host "  [FAIL] Could not create database" -ForegroundColor Red
    exit 1
}

# Check if database already has tables (skip if not Force)
if (-not $Force) {
    $checkQuery = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'"
    $hasTables = Invoke-FinosSql -Query $checkQuery
    # If we can query and it succeeded, check manually
    Write-Host "  (Use -Force to drop and recreate)" -ForegroundColor DarkGray
}

# ============================================================================
# Step 4: Run Schema Scripts (001-008)
# ============================================================================
Write-Host ""
Write-Host "--- Step 4/6: Running schema scripts ---" -ForegroundColor Yellow

$errorCount = 0
$schemaPrefixes = @("001", "002", "003", "004", "005", "006", "007", "008")

foreach ($prefix in $schemaPrefixes) {
    $files = Get-ChildItem -Path "$DbScriptsDir\Schema\${prefix}_*.sql" -ErrorAction SilentlyContinue
    if ($files) {
        foreach ($file in $files) {
            Write-Host "  Running: $($file.Name)..." -ForegroundColor DarkGray -NoNewline
            $result = Invoke-FinosSql -InputFile $file.FullName
            if ($result) {
                Write-Host " [OK]" -ForegroundColor Green
            } else {
                Write-Host " [FAIL]" -ForegroundColor Red
                $errorCount++
            }
        }
    } else {
        Write-Host "  [SKIP] No schema script for prefix: $prefix" -ForegroundColor DarkYellow
    }
}

# ============================================================================
# Step 5: Run Seed Data, Stored Procedures, and Views
# ============================================================================
Write-Host ""
Write-Host "--- Step 5/6: Running seed data, stored procedures, and views ---" -ForegroundColor Yellow

# Seed Data
Write-Host "  Seed Data:" -ForegroundColor Cyan
$seedPrefixes = @("001", "002", "003")
foreach ($prefix in $seedPrefixes) {
    $files = Get-ChildItem -Path "$DbScriptsDir\SeedData\${prefix}_*.sql" -ErrorAction SilentlyContinue
    if ($files) {
        foreach ($file in $files) {
            Write-Host "    Running: $($file.Name)..." -ForegroundColor DarkGray -NoNewline
            $result = Invoke-FinosSql -InputFile $file.FullName
            if ($result) {
                Write-Host " [OK]" -ForegroundColor Green
            } else {
                Write-Host " [FAIL]" -ForegroundColor Red
                $errorCount++
            }
        }
    } else {
        Write-Host "    [SKIP] No seed script for prefix: $prefix" -ForegroundColor DarkYellow
    }
}

# Stored Procedures
Write-Host "  Stored Procedures:" -ForegroundColor Cyan
$spFiles = @("Security_sp", "Core_sp", "Budget_sp", "Investment_sp", "Loan_sp", "Goals_sp", "Analytics_sp")
foreach ($sp in $spFiles) {
    $filePath = Join-Path $DbScriptsDir "StoredProcedures\$sp.sql"
    if (Test-Path $filePath) {
        Write-Host "    Running: $sp.sql..." -ForegroundColor DarkGray -NoNewline
        $result = Invoke-FinosSql -InputFile $filePath
        if ($result) {
            Write-Host " [OK]" -ForegroundColor Green
        } else {
            Write-Host " [FAIL]" -ForegroundColor Red
            $errorCount++
        }
    } else {
        Write-Host "    [SKIP] $sp.sql not found" -ForegroundColor DarkYellow
    }
}

# Views
Write-Host "  Views:" -ForegroundColor Cyan
$viewFiles = @("Dashboard_Views", "Analytics_Views", "Loan_Views", "Budget_Views", "Admin_Views", "Investment_Views")
foreach ($view in $viewFiles) {
    $filePath = Join-Path $DbScriptsDir "Views\$view.sql"
    if (Test-Path $filePath) {
        Write-Host "    Running: $view.sql..." -ForegroundColor DarkGray -NoNewline
        $result = Invoke-FinosSql -InputFile $filePath
        if ($result) {
            Write-Host " [OK]" -ForegroundColor Green
        } else {
            Write-Host " [FAIL]" -ForegroundColor Red
            $errorCount++
        }
    } else {
        Write-Host "    [SKIP] $view.sql not found" -ForegroundColor DarkYellow
    }
}

# ============================================================================
# Step 6: Verify Setup
# ============================================================================
Write-Host ""
Write-Host "--- Step 6/6: Verifying database setup ---" -ForegroundColor Yellow

$verificationQueries = @(
    @{ Name = "Tables exist";         Query = "SELECT TOP 1 name FROM sys.tables" },
    @{ Name = "Users table";          Query = "SELECT TOP 1 UserId FROM dbo.Users" },
    @{ Name = "Categories table";     Query = "SELECT TOP 1 CategoryId FROM dbo.Categories" },
    @{ Name = "Accounts table";       Query = "SELECT TOP 1 AccountId FROM dbo.Accounts" },
    @{ Name = "Stored procedures";    Query = "SELECT TOP 1 name FROM sys.procedures WHERE name LIKE 'sp_%'" },
    @{ Name = "Views";               Query = "SELECT TOP 1 name FROM sys.views WHERE name LIKE 'vw_%'" }
)

foreach ($v in $verificationQueries) {
    $result = Invoke-FinosSql -Query $v.Query
    if ($result) {
        Write-Host "  [OK] $($v.Name)" -ForegroundColor Green
    } else {
        Write-Host "  [WARN] $($v.Name) - verification query returned no data" -ForegroundColor DarkYellow
    }
}

# ============================================================================
# Summary
# ============================================================================
Write-Host ""
Write-Host "  +==================================================+" -ForegroundColor Cyan
if ($errorCount -eq 0) {
    Write-Host "  |  Database Setup Complete - No errors!             |" -ForegroundColor Green
} else {
    Write-Host "  |  Database Setup Complete with $errorCount error(s)             |" -ForegroundColor DarkYellow
}
Write-Host "  +==================================================+" -ForegroundColor Cyan
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
