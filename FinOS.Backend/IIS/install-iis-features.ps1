# FinOS - IIS Feature Installation Script
# Run as Administrator
# This script enables IIS features required for hosting FinOS microservices

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FinOS - IIS Feature Installation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

# Detect Windows Server versus Windows client (Windows 10/11)
$isWindowsServer = (Get-CimInstance Win32_OperatingSystem).ProductType -ne 1
$featureFailures = @()

if ($isWindowsServer) {
    Import-Module ServerManager -ErrorAction Stop
    $iisInstalled = (Get-WindowsFeature -Name Web-Server).Installed
    $features = @(
        'Web-Server','Web-WebServer','Web-Common-Http','Web-Default-Doc','Web-Dir-Browsing',
        'Web-Http-Errors','Web-Static-Content','Web-Health','Web-Http-Logging','Web-Request-Monitor',
        'Web-Performance','Web-Stat-Compression','Web-Dyn-Compression','Web-Security','Web-Filtering',
        'Web-Windows-Auth','Web-App-Dev','Web-Net-Ext45','Web-Asp-Net45','Web-ISAPI-Ext',
        'Web-ISAPI-Filter','Web-Mgmt-Tools','Web-Mgmt-Console','Web-Scripting-Tools'
    )

    if (-not $iisInstalled) {
        Write-Host '[STEP 1] Installing IIS and required Server features...' -ForegroundColor Yellow
        foreach ($feature in $features) {
            Write-Host "  Enabling: $feature" -ForegroundColor Gray
            try {
                $result = Install-WindowsFeature -Name $feature -IncludeManagementTools -ErrorAction Stop
                if (-not $result.Success) { $featureFailures += $feature }
            } catch {
                Write-Host "    [ERROR] $($_.Exception.Message)" -ForegroundColor Red
                $featureFailures += $feature
            }
        }
    } else {
        Write-Host '[INFO] IIS is already installed.' -ForegroundColor Green
    }
} else {
    $iisInstalled = (Get-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole -ErrorAction SilentlyContinue).State -eq 'Enabled'
    $features = @(
        'IIS-WebServerRole','IIS-WebServer','IIS-CommonHttpFeatures','IIS-DefaultDocument','IIS-DirectoryBrowsing',
        'IIS-HttpErrors','IIS-StaticContent','IIS-HealthAndDiagnostics','IIS-HttpLogging','IIS-RequestMonitor',
        'IIS-Performance','IIS-HttpCompressionStatic','IIS-HttpCompressionDynamic','IIS-Security','IIS-RequestFiltering',
        'IIS-WindowsAuthentication','IIS-ApplicationDevelopment','IIS-NetFxExtensibility45','IIS-ASPNET45',
        'IIS-ISAPIExtensions','IIS-ISAPIFilter','IIS-WebServerManagementTools','IIS-ManagementConsole','IIS-ManagementScriptingTools'
    )

    if (-not $iisInstalled) {
        Write-Host '[STEP 1] Installing IIS and required Windows optional features...' -ForegroundColor Yellow
    } else {
        Write-Host '[INFO] IIS is already installed; verifying required features...' -ForegroundColor Green
    }

    foreach ($feature in $features) {
        $state = (Get-WindowsOptionalFeature -Online -FeatureName $feature -ErrorAction SilentlyContinue).State
        if ($state -eq 'Enabled') {
            continue
        }
        Write-Host "  Enabling: $feature" -ForegroundColor Gray
        try {
            Enable-WindowsOptionalFeature -Online -FeatureName $feature -All -NoRestart -ErrorAction Stop | Out-Null
        } catch {
            Write-Host "    [ERROR] $($_.Exception.Message)" -ForegroundColor Red
            $featureFailures += $feature
        }
    }
}

if ($featureFailures.Count -gt 0) {
    Write-Host "[ERROR] Could not enable IIS features: $($featureFailures -join ', ')" -ForegroundColor Red
    exit 1
}
Write-Host '[OK] IIS features verified.' -ForegroundColor Green
# Verify IIS is running
Write-Host ""
Write-Host "[STEP 2] Verifying IIS service..." -ForegroundColor Yellow
$iisService = Get-Service -Name "W3SVC" -ErrorAction SilentlyContinue
if ($iisService -and $iisService.Status -eq "Running") {
    Write-Host "[OK] IIS Service (W3SVC) is running." -ForegroundColor Green
} else {
    Write-Host "[INFO] Starting IIS Service..." -ForegroundColor Yellow
    Start-Service -Name "W3SVC"
    Write-Host "[OK] IIS Service started." -ForegroundColor Green
}

# Check for .NET 8 Hosting Bundle
Write-Host ""
Write-Host "[STEP 3] Checking .NET 8 Hosting Bundle..." -ForegroundColor Yellow

$dotnetHostBundle = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\ASP.NET\*" -ErrorAction SilentlyContinue | Where-Object { $_.PSChildName -like "*8.*" }
$runtimePath = "C:\Program Files\dotnet\dotnet.exe"

if (Test-Path $runtimePath) {
    $dotnetVersion = & $runtimePath --list-runtimes 2>$null | Select-String "Microsoft.AspNetCore.App 8."
    if ($dotnetVersion) {
        Write-Host "[OK] .NET 8 ASP.NET Core Runtime is installed:" -ForegroundColor Green
        Write-Host "  $dotnetVersion" -ForegroundColor Gray
    } else {
        Write-Host "[WARN] .NET 8 ASP.NET Core Runtime not found." -ForegroundColor Yellow
        Install-DotNet8HostingBundle
    }
} else {
    Write-Host "[WARN] .NET Runtime not found at $runtimePath" -ForegroundColor Yellow
    Install-DotNet8HostingBundle
}

# Verify ASP.NET Core Module
Write-Host ""
Write-Host "[STEP 4] Verifying ASP.NET Core Module..." -ForegroundColor Yellow
$aspNetCoreModule = Get-WebGlobalModule | Where-Object { $_.Name -eq "AspNetCoreModuleV2" }
if ($aspNetCoreModule) {
    Write-Host "[OK] ASP.NET Core Module V2 is installed." -ForegroundColor Green
} else {
    Write-Host "[WARN] ASP.NET Core Module V2 not found. The .NET Hosting Bundle may need to be installed." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  IIS Installation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor White
Write-Host "  1. Run 'deploy-to-iis.ps1' to deploy FinOS services" -ForegroundColor Gray
Write-Host "  2. Access Gateway at http://localhost:8080" -ForegroundColor Gray
Write-Host ""

# Function to install .NET 8 Hosting Bundle
function Install-DotNet8HostingBundle {
    Write-Host "[INFO] Downloading .NET 8 Hosting Bundle..." -ForegroundColor Yellow
    
    $installerUrl = "https://download.visualstudio.microsoft.com/download/pr/93951abe-4be6-4e2b-b7c2-5564dc54de7b/d6a3e0e7e61f6d885d0d8e44022b9a9f/dotnet-hosting-8.0.3-win.exe"
    $installerPath = "$env:TEMP\dotnet-hosting-8.0.3-win.exe"
    
    try {
        Invoke-WebRequest -Uri $installerUrl -OutFile $installerPath -UseBasicParsing
        Write-Host "[INFO] Installing .NET 8 Hosting Bundle..." -ForegroundColor Yellow
        $installProcess = Start-Process -FilePath $installerPath -ArgumentList "/install", "/quiet", "/norestart" -Wait -PassThru
        
        if ($installProcess.ExitCode -eq 0) {
            Write-Host "[OK] .NET 8 Hosting Bundle installed successfully." -ForegroundColor Green
            Write-Host "[INFO] Restarting IIS to register the module..." -ForegroundColor Yellow
            & iisreset
        } else {
            Write-Host "[ERROR] .NET 8 Hosting Bundle installation failed with exit code: $($installProcess.ExitCode)" -ForegroundColor Red
            Write-Host "[INFO] Please install manually from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "[ERROR] Failed to download .NET 8 Hosting Bundle: $_" -ForegroundColor Red
        Write-Host "[INFO] Please install manually from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    }
}
