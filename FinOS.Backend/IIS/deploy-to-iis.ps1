# FinOS - IIS Deployment Script
# Run as Administrator
# This script publishes all FinOS microservices and deploys them to IIS

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FinOS - IIS Deployment Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    exit 1
}

# Configuration
$FinOSBasePath = "C:\FinOS"
$SourcePath = Read-Host "Enter FinOS source code path (e.g., C:\Source\FinOS)"
if (-NOT (Test-Path $SourcePath)) {
    Write-Host "ERROR: Source path not found: $SourcePath" -ForegroundColor Red
    exit 1
}

# SQL Server Connection String
$ConnectionString = Read-Host "Enter SQL Server Connection String (or press Enter for default)"
if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    $ConnectionString = "Server=localhost\SQLEXPRESS;Database=FinOS_{Service};Trusted_Connection=True;TrustServerCertificate=True;"
}

# JWT Configuration
$JwtSecret = Read-Host "Enter JWT Secret Key (or press Enter for auto-generated)"
if ([string]::IsNullOrWhiteSpace($JwtSecret)) {
    $JwtSecret = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | ForEach-Object { [char]$_ })
    Write-Host "[INFO] Auto-generated JWT Secret" -ForegroundColor Yellow
}

# Service Definitions
$Services = @(
    @{ Name = "Gateway";      Project = "FinOS.Gateway.API";      Port = 6000; Database = "" },
    @{ Name = "Identity";     Project = "FinOS.Identity.API";      Port = 5001; Database = "FinOS_Identity" },
    @{ Name = "CoreFinance";  Project = "FinOS.CoreFinance.API";   Port = 5002; Database = "FinOS_CoreFinance" },
    @{ Name = "Budget";       Project = "FinOS.Budget.API";        Port = 5003; Database = "FinOS_Budget" },
    @{ Name = "Investment";   Project = "FinOS.Investment.API";    Port = 5004; Database = "FinOS_Investment" },
    @{ Name = "Loan";         Project = "FinOS.Loan.API";          Port = 5005; Database = "FinOS_Loan" },
    @{ Name = "Goals";        Project = "FinOS.Goals.API";         Port = 5006; Database = "FinOS_Goals" },
    @{ Name = "Analytics";    Project = "FinOS.Analytics.API";     Port = 5007; Database = "FinOS_Analytics" },
    @{ Name = "AI";           Project = "FinOS.AI.API";            Port = 5008; Database = "FinOS_AI" },
    @{ Name = "Notification"; Project = "FinOS.Notification.API";  Port = 5009; Database = "FinOS_Notification" }
)

# Step 1: Create deployment directories
Write-Host ""
Write-Host "[STEP 1] Creating deployment directories..." -ForegroundColor Yellow
if (-NOT (Test-Path $FinOSBasePath)) {
    New-Item -Path $FinOSBasePath -ItemType Directory -Force | Out-Null
}
foreach ($svc in $Services) {
    $deployPath = Join-Path $FinOSBasePath $svc.Name
    if (-NOT (Test-Path $deployPath)) {
        New-Item -Path $deployPath -ItemType Directory -Force | Out-Null
    }
}
Write-Host "[OK] Deployment directories created at $FinOSBasePath" -ForegroundColor Green

# Step 2: Publish each microservice
Write-Host ""
Write-Host "[STEP 2] Publishing microservices..." -ForegroundColor Yellow

foreach ($svc in $Services) {
    Write-Host "  Publishing $($svc.Name)..." -ForegroundColor Cyan
    
    $projectPath = Get-ChildItem -Path $SourcePath -Recurse -Filter "$($svc.Project).csproj" | Select-Object -First 1
    
    if ($projectPath) {
        $publishPath = Join-Path $FinOSBasePath $svc.Name
        $publishResult = dotnet publish $projectPath.FullName -c Release -o $publishPath --no-self-contained
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "    [OK] $($svc.Name) published successfully" -ForegroundColor Green
        } else {
            Write-Host "    [ERROR] Failed to publish $($svc.Name)" -ForegroundColor Red
        }
    } else {
        Write-Host "    [WARN] Project not found: $($svc.Project).csproj" -ForegroundColor DarkYellow
    }
}

# Step 3: Create IIS Application Pools
Write-Host ""
Write-Host "[STEP 3] Creating IIS Application Pools..." -ForegroundColor Yellow

Import-Module WebAdministration

foreach ($svc in $Services) {
    $appPoolName = "FinOS_$($svc.Name)_Pool"
    
    if (Test-Path "IIS:\AppPools\$appPoolName") {
        Write-Host "  [INFO] App pool '$appPoolName' already exists - updating" -ForegroundColor DarkYellow
    } else {
        New-Item -Path "IIS:\AppPools\$appPoolName" -Force | Out-Null
        Write-Host "  [OK] Created app pool: $appPoolName" -ForegroundColor Green
    }
    
    # Configure app pool
    $appPool = Get-Item "IIS:\AppPools\$appPoolName"
    $appPool.managedRuntimeVersion = ""  # No Managed Code for .NET Core
    $appPool.startMode = "AlwaysRunning"
    $appPool.processModel.idleTimeout = [TimeSpan]::FromMinutes(0)
    $appPool.processModel.identityType = "ApplicationPoolIdentity"
    $appPool | Set-Item
    
    # Set .NET CLR version to No Managed Code
    Set-ItemProperty -Path "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value ""
}

# Step 4: Create IIS Websites
Write-Host ""
Write-Host "[STEP 4] Creating IIS Websites..." -ForegroundColor Yellow

# Remove default website if it conflicts
$defaultSite = Get-Website | Where-Object { $_.Name -eq "Default Web Site" }
if ($defaultSite) {
    Write-Host "  [INFO] Stopping Default Web Site" -ForegroundColor DarkYellow
    Stop-Website -Name "Default Web Site" -ErrorAction SilentlyContinue
}

foreach ($svc in $Services) {
    $siteName = "FinOS_$($svc.Name)"
    $physicalPath = Join-Path $FinOSBasePath $svc.Name
    $appPoolName = "FinOS_$($svc.Name)_Pool"
    $bindingInfo = "*:$($svc.Port):"
    
    # Remove existing site if it exists
    if (Test-Path "IIS:\Sites\$siteName") {
        Write-Host "  [INFO] Removing existing site: $siteName" -ForegroundColor DarkYellow
        Remove-Website -Name $siteName -ErrorAction SilentlyContinue
    }
    
    # Create the website
    New-Website -Name $siteName `
        -PhysicalPath $physicalPath `
        -ApplicationPool $appPoolName `
        -Port $svc.Port `
        -Force | Out-Null
    
    Write-Host "  [OK] Created site: $siteName on port $($svc.Port)" -ForegroundColor Green
    
    # Configure URL ACL if needed (for non-admin ports)
    try {
        $existingAcl = netsh http show urlacl url=http://+:$($svc.Port)/ 2>&1
        if ($existingAcl -notmatch "Reserved URL") {
            Write-Host "    [INFO] Configuring URL ACL for port $($svc.Port)..." -ForegroundColor DarkYellow
            netsh http add urlacl url=http://+:$($svc.Port)/ user="Everyone" 2>$null
        }
    } catch {
        Write-Host "    [WARN] Could not configure URL ACL for port $($svc.Port)" -ForegroundColor DarkYellow
    }
    
    # Configure firewall rule
    $ruleName = "FinOS_$($svc.Name)_Port_$($svc.Port)"
    $existingRule = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
    if (-NOT $existingRule) {
        New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP -LocalPort $svc.Port -Action Allow | Out-Null
        Write-Host "    [OK] Firewall rule added for port $($svc.Port)" -ForegroundColor Green
    }
}

# Step 5: Configure web.config for each service
Write-Host ""
Write-Host "[STEP 5] Configuring web.config files..." -ForegroundColor Yellow

foreach ($svc in $Services) {
    $webConfigPath = Join-Path (Join-Path $FinOSBasePath $svc.Name) "web.config"
    $dllName = "$($svc.Project).dll"
    
    $serviceConnString = ""
    if (-NOT [string]::IsNullOrWhiteSpace($svc.Database)) {
        $serviceConnString = $ConnectionString -replace '\{Service\}', $svc.Name
    }
    
    $webConfigContent = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments="$dllName"
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
          <environmentVariable name="ASPNETCORE_URLS" value="http://*:$($svc.Port)" />
$(if (-NOT [string]::IsNullOrWhiteSpace($serviceConnString)) {
"          <environmentVariable name=""ConnectionStrings__DefaultConnection"" value=""$serviceConnString"" />"
})
          <environmentVariable name="JwtSettings__Secret" value="$JwtSecret" />
          <environmentVariable name="JwtSettings__Issuer" value="FinOS" />
          <environmentVariable name="JwtSettings__Audience" value="FinOS.Users" />
          <environmentVariable name="GatewayUrl" value="http://localhost:6000" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
"@
    
    Set-Content -Path $webConfigPath -Value $webConfigContent -Encoding UTF8
    Write-Host "  [OK] Configured web.config for $($svc.Name)" -ForegroundColor Green
    
    # Create logs directory
    $logsPath = Join-Path (Join-Path $FinOSBasePath $svc.Name) "logs"
    if (-NOT (Test-Path $logsPath)) {
        New-Item -Path $logsPath -ItemType Directory -Force | Out-Null
    }
}

# Step 6: Set folder permissions
Write-Host ""
Write-Host "[STEP 6] Setting folder permissions..." -ForegroundColor Yellow

foreach ($svc in $Services) {
    $deployPath = Join-Path $FinOSBasePath $svc.Name
    $acl = Get-Acl $deployPath
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($rule)
    Set-Acl -Path $deployPath -AclObject $acl
}
Write-Host "[OK] Folder permissions set" -ForegroundColor Green

# Step 7: Start all websites
Write-Host ""
Write-Host "[STEP 7] Starting all websites..." -ForegroundColor Yellow

foreach ($svc in $Services) {
    $siteName = "FinOS_$($svc.Name)"
    try {
        Start-Website -Name $siteName -ErrorAction Stop
        Write-Host "  [OK] Started: $siteName" -ForegroundColor Green
    } catch {
        Write-Host "  [ERROR] Failed to start $siteName : $_" -ForegroundColor Red
    }
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FinOS Deployment Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Service URLs:" -ForegroundColor White
foreach ($svc in $Services) {
    Write-Host "  $($svc.Name.PadRight(15)) : http://localhost:$($svc.Port)" -ForegroundColor Gray
}
Write-Host ""
Write-Host "Gateway API: http://localhost:6000" -ForegroundColor Cyan
Write-Host "Swagger UI:  http://localhost:6000/swagger" -ForegroundColor Cyan
Write-Host ""
Write-Host "To check status: Get-Website | Where-Object { `$_.Name -like 'FinOS_*' } | Format-Table Name, State, Port" -ForegroundColor DarkGray
Write-Host "To view logs: Check C:\FinOS\{ServiceName}\logs\" -ForegroundColor DarkGray
