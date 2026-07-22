# FinOS - IIS Cleanup Script
# Run as Administrator
# This script removes all FinOS IIS sites, app pools, firewall rules, and deployment files

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FinOS - IIS Cleanup Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    exit 1
}

# Confirm deletion
$confirm = Read-Host "Are you sure you want to remove ALL FinOS IIS sites, app pools, and files? (YES/no)"
if ($confirm -ne "YES") {
    Write-Host "Operation cancelled." -ForegroundColor Yellow
    exit 0
}

Import-Module WebAdministration

# Service names
$ServiceNames = @("Gateway", "Identity", "CoreFinance", "Budget", "Investment", "Loan", "Goals", "Analytics", "AI", "Notification")
$Ports = @(8080, 5001, 5002, 5003, 5004, 5005, 5006, 5007, 5008, 5009)
$FinOSBasePath = "C:\FinOS"

# Step 1: Stop and remove websites
Write-Host ""
Write-Host "[STEP 1] Removing IIS Websites..." -ForegroundColor Yellow

foreach ($svcName in $ServiceNames) {
    $siteName = "FinOS_$svcName"
    
    if (Test-Path "IIS:\Sites\$siteName") {
        try {
            Stop-Website -Name $siteName -ErrorAction SilentlyContinue
            Remove-Website -Name $siteName -ErrorAction Stop
            Write-Host "  [OK] Removed site: $siteName" -ForegroundColor Green
        } catch {
            Write-Host "  [ERROR] Failed to remove site $siteName : $_" -ForegroundColor Red
        }
    } else {
        Write-Host "  [SKIP] Site not found: $siteName" -ForegroundColor DarkGray
    }
}

# Step 2: Remove application pools
Write-Host ""
Write-Host "[STEP 2] Removing IIS Application Pools..." -ForegroundColor Yellow

foreach ($svcName in $ServiceNames) {
    $appPoolName = "FinOS_${svcName}_Pool"
    
    if (Test-Path "IIS:\AppPools\$appPoolName") {
        try {
            # Stop app pool first
            $appPool = Get-Item "IIS:\AppPools\$appPoolName"
            if ($appPool.state -ne "Stopped") {
                Stop-WebAppPool -Name $appPoolName -ErrorAction SilentlyContinue
                Start-Sleep -Seconds 2
            }
            
            Remove-WebAppPool -Name $appPoolName -ErrorAction Stop
            Write-Host "  [OK] Removed app pool: $appPoolName" -ForegroundColor Green
        } catch {
            Write-Host "  [ERROR] Failed to remove app pool $appPoolName : $_" -ForegroundColor Red
            # Force removal
            try {
                $appPoolPath = "IIS:\AppPools\$appPoolName"
                Remove-Item $appPoolPath -Recurse -Force -ErrorAction SilentlyContinue
                Write-Host "  [OK] Force removed app pool: $appPoolName" -ForegroundColor DarkYellow
            } catch {
                Write-Host "  [ERROR] Could not force remove: $_" -ForegroundColor Red
            }
        }
    } else {
        Write-Host "  [SKIP] App pool not found: $appPoolName" -ForegroundColor DarkGray
    }
}

# Step 3: Remove firewall rules
Write-Host ""
Write-Host "[STEP 3] Removing Firewall Rules..." -ForegroundColor Yellow

for ($i = 0; $i -lt $ServiceNames.Count; $i++) {
    $ruleName = "FinOS_$($ServiceNames[$i])_Port_$($Ports[$i])"
    
    $existingRule = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
    if ($existingRule) {
        Remove-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
        Write-Host "  [OK] Removed firewall rule: $ruleName" -ForegroundColor Green
    } else {
        Write-Host "  [SKIP] Firewall rule not found: $ruleName" -ForegroundColor DarkGray
    }
}

# Step 4: Remove URL ACLs
Write-Host ""
Write-Host "[STEP 4] Removing URL ACLs..." -ForegroundColor Yellow

foreach ($port in $Ports) {
    try {
        $existingAcl = netsh http show urlacl url=http://+:$port/ 2>&1
        if ($existingAcl -match "Reserved URL") {
            netsh http delete urlacl url=http://+:$port/ 2>$null
            Write-Host "  [OK] Removed URL ACL for port $port" -ForegroundColor Green
        } else {
            Write-Host "  [SKIP] No URL ACL for port $port" -ForegroundColor DarkGray
        }
    } catch {
        Write-Host "  [SKIP] URL ACL check failed for port $port" -ForegroundColor DarkGray
    }
}

# Step 5: Remove deployment files
Write-Host ""
Write-Host "[STEP 5] Removing deployment files..." -ForegroundColor Yellow

if (Test-Path $FinOSBasePath) {
    $deleteFiles = Read-Host "Delete all FinOS deployment files at $FinOSBasePath ? (YES/no)"
    if ($deleteFiles -eq "YES") {
        try {
            Remove-Item -Path $FinOSBasePath -Recurse -Force -ErrorAction Stop
            Write-Host "  [OK] Removed deployment directory: $FinOSBasePath" -ForegroundColor Green
        } catch {
            Write-Host "  [ERROR] Failed to remove $FinOSBasePath : $_" -ForegroundColor Red
            Write-Host "  [INFO] Try closing any applications using these files and run again." -ForegroundColor Yellow
        }
    } else {
        Write-Host "  [SKIP] Deployment files preserved at $FinOSBasePath" -ForegroundColor DarkGray
    }
} else {
    Write-Host "  [SKIP] Deployment directory not found: $FinOSBasePath" -ForegroundColor DarkGray
}

# Step 6: Restart IIS
Write-Host ""
Write-Host "[STEP 6] Restarting IIS..." -ForegroundColor Yellow
& iisreset
Write-Host "[OK] IIS restarted" -ForegroundColor Green

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FinOS Cleanup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "All FinOS IIS sites, app pools, firewall rules, and URL ACLs have been removed." -ForegroundColor Gray
