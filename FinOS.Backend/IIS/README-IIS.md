# FinOS - IIS Deployment Guide

## Prerequisites

### System Requirements
- **OS**: Windows 10 Pro/Enterprise or Windows 11 Pro/Enterprise, or Windows Server 2019/2022
- **RAM**: Minimum 8 GB (16 GB recommended)
- **Disk**: 10 GB free space for deployment files
- **CPU**: 4 cores minimum

### Software Requirements
1. **IIS 10+** (Internet Information Services)
2. **.NET 8 SDK** (for publishing)
3. **.NET 8 Hosting Bundle** (for runtime on IIS)
4. **SQL Server Express 2019+** (or full SQL Server)
5. **PowerShell 5.1+** (run as Administrator)

---

## Step 1: Enable IIS Features

Open PowerShell as Administrator and run:

```powershell
.\install-iis-features.ps1
```

This script will:
- Enable IIS with all required features
- Enable ASP.NET 4.5 compatibility
- Check and install .NET 8 Hosting Bundle
- Verify the ASP.NET Core Module V2 is registered

**Manual alternative** — Open "Turn Windows features on or off" and enable:
- Internet Information Services → Web Management Tools → IIS Management Console
- Internet Information Services → World Wide Web Services → Application Development Features → ASP.NET 4.8
- Internet Information Services → World Wide Web Services → Application Development Features → ISAPI Extensions
- Internet Information Services → World Wide Web Services → Application Development Features → ISAPI Filters
- Internet Information Services → World Wide Web Services → Common HTTP Features → Static Content
- Internet Information Services → World Wide Web Services → Security → Request Filtering

---

## Step 2: Install .NET 8 Hosting Bundle

The `install-iis-features.ps1` script handles this automatically. If you need to install manually:

1. Download from: https://dotnet.microsoft.com/download/dotnet/8.0
2. Look for ".NET 8.0 Runtime (v8.0.x) - Windows Hosting Bundle Installer"
3. Run the installer
4. Open an Administrator command prompt and run: `iisreset`

Verify installation:
```powershell
dotnet --list-runtimes
# Should show: Microsoft.AspNetCore.App 8.x.x
```

---

## Step 3: Prepare SQL Server

1. Install SQL Server Express (if not already installed):
   - Download from: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
   - Choose "Express" edition
   - Enable "Mixed Mode" authentication or use Windows Authentication

2. Create databases:
```sql
-- Run in SQL Server Management Studio or sqlcmd
CREATE DATABASE FinOS_Identity;
CREATE DATABASE FinOS_CoreFinance;
CREATE DATABASE FinOS_Budget;
CREATE DATABASE FinOS_Investment;
CREATE DATABASE FinOS_Loan;
CREATE DATABASE FinOS_Goals;
CREATE DATABASE FinOS_Analytics;
CREATE DATABASE FinOS_AI;
CREATE DATABASE FinOS_Notification;
GO
```

3. Entity Framework migrations will create tables on first run if using `EnsureCreated()` or you can run migrations manually:

```bash
# In each service project directory
dotnet ef database update --context ApplicationDbContext
```

---

## Step 4: Deploy FinOS Services

Open PowerShell as Administrator and run:

```powershell
.\deploy-to-iis.ps1
```

The script will prompt for:
- **Source code path**: Path to the FinOS solution root
- **Connection string**: SQL Server connection string
- **JWT Secret**: Secret key for token signing (auto-generates if empty)

The script automatically:
1. Publishes each microservice in Release mode
2. Creates IIS application pools (.NET 8, No Managed Code, AlwaysRunning)
3. Creates IIS websites with proper bindings
4. Generates web.config files with environment variables
5. Configures firewall rules for each port
6. Sets folder permissions for IIS_IUSRS
7. Starts all websites

### Service Port Mapping

| Service       | Port | URL                        | Database            |
|---------------|------|----------------------------|---------------------|
| Gateway       | 6000 | http://localhost:6000      | —                   |
| Identity      | 5001 | http://localhost:5001      | FinOS_Identity      |
| CoreFinance   | 5002 | http://localhost:5002      | FinOS_CoreFinance   |
| Budget        | 5003 | http://localhost:5003      | FinOS_Budget        |
| Investment    | 5004 | http://localhost:5004      | FinOS_Investment    |
| Loan          | 5005 | http://localhost:5005      | FinOS_Loan          |
| Goals         | 5006 | http://localhost:5006      | FinOS_Goals         |
| Analytics     | 5007 | http://localhost:5007      | FinOS_Analytics     |
| AI            | 5008 | http://localhost:5008      | FinOS_AI            |
| Notification  | 5009 | http://localhost:5009      | FinOS_Notification  |

---

## Step 5: Deploy the Vue.js Frontend

### Build the frontend:
```bash
cd FinOS.Frontend
npm install
npm run build
```

### Deploy to IIS:
1. Copy the `dist` folder to `C:\FinOS\Frontend`
2. In IIS Manager, add a new website:
   - Site name: `FinOS_Frontend`
   - Physical path: `C:\FinOS\Frontend`
   - Port: `80` (or any desired port)
   - App Pool: `.NET v4.5` or create a new one with No Managed Code
3. Add a `web.config` file in `C:\FinOS\Frontend` for URL rewriting (SPA support):

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <rule name="SPA Routes" stopProcessing="true">
          <match url=".*" />
          <conditions logicalGrouping="MatchAll">
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
            <add input="{REQUEST_URI}" pattern="^/api" negate="true" />
          </conditions>
          <action type="Rewrite" url="/index.html" />
        </rule>
      </rules>
    </rewrite>
  </system.webServer>
</configuration>
```

4. Update the frontend API base URL in `.env.production`:
```
VITE_API_BASE_URL=http://localhost:6000
```

---

## Step 6: Access the Application

- **Frontend**: http://localhost (or your configured port)
- **Gateway API**: http://localhost:6000
- **Swagger (Gateway)**: http://localhost:6000/swagger
- **Individual Service APIs**: http://localhost:{PORT}/swagger

---

## Troubleshooting

### Common Issues

#### 1. HTTP Error 502.5 - Process Failure
**Cause**: .NET 8 Hosting Bundle not installed or incorrect.

**Solution**:
```powershell
# Verify .NET runtime
dotnet --list-runtimes

# Install Hosting Bundle and restart IIS
iisreset
```

#### 2. HTTP Error 500.19 - Configuration Error
**Cause**: URL Rewrite module not installed.

**Solution**: Install IIS URL Rewrite Module:
```powershell
# Download and install URL Rewrite
choco install urlrewrite -y
# OR download from: https://www.iis.net/downloads/microsoft/url-rewrite
```

#### 3. Application Pool Stops Immediately
**Cause**: Missing dependencies or configuration error.

**Solution**:
1. Check stdout logs: `C:\FinOS\{ServiceName}\logs\`
2. Check Windows Event Viewer → Application logs
3. Verify the DLL exists in the deployment folder
4. Test manually:
```powershell
cd C:\FinOS\{ServiceName}
dotnet {ServiceName}.dll
```

#### 4. 403 Forbidden Error
**Cause**: IIS_IUSRS doesn't have read permissions.

**Solution**:
```powershell
$path = "C:\FinOS"
$acl = Get-Acl $path
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($rule)
Set-Acl -Path $path -AclObject $acl
```

#### 5. Connection String Issues
**Cause**: SQL Server not accessible or connection string incorrect.

**Solution**:
1. Verify SQL Server is running:
```powershell
Get-Service -Name 'MSSQL$SQLEXPRESS'
```
2. Test connection:
```powershell
sqlcmd -S "localhost\SQLEXPRESS" -E -Q "SELECT 1"
```
3. Check connection string in web.config environment variables

#### 6. Port Already in Use
**Cause**: Another service using the same port.

**Solution**:
```powershell
# Find what's using a port
netstat -ano | findstr :6000

# Kill the process (if safe to do so)
taskkill /PID <PID> /F
```

#### 7. CORS Errors
**Cause**: Frontend origin not allowed.

**Solution**: Update the `CorsOrigins` environment variable in the Gateway's web.config:
```xml
<environmentVariable name="CorsOrigins" value="http://localhost,http://localhost:80,http://yourdomain.com" />
```

---

## Useful Commands

```powershell
# Check all FinOS websites status
Get-Website | Where-Object { $_.Name -like 'FinOS_*' } | Format-Table Name, State, Port

# Restart a specific service
Restart-Website -Name "FinOS_Gateway"

# View application pool status
Get-IISAppPool | Where-Object { $_.Name -like 'FinOS_*' } | Format-Table Name, State

# Restart IIS completely
iisreset

# View recent IIS logs
Get-Content "C:\inetpub\logs\LogFiles\W3SVC*\*.log" -Tail 50

# View application stdout logs
Get-Content "C:\FinOS\Gateway\logs\*.log" -Tail 50
```

---

## Cleanup

To remove all FinOS IIS deployments:

```powershell
.\remove-iis-sites.ps1
```

This will:
- Stop and remove all FinOS IIS websites
- Remove all FinOS application pools
- Remove firewall rules
- Remove URL ACLs
- Optionally remove deployment files from `C:\FinOS`

---

## Security Considerations

1. **Change the JWT Secret**: Never use the auto-generated secret in production
2. **Enable HTTPS**: Configure SSL certificates for all services
3. **Secure Connection Strings**: Use encrypted connection strings or Windows Authentication
4. **Restrict CORS**: Only allow specific origins
5. **Firewall**: Only expose the Gateway (port 6000) and Frontend (port 80/443) externally
6. **Rate Limiting**: Configure rate limiting on the Gateway
7. **Logging**: Enable comprehensive logging and monitoring

---

## Production Recommendations

1. Use **HTTPS** with valid SSL certificates
2. Set up a **reverse proxy** (IIS ARR or Nginx) in front of services
3. Use **Windows Service** hosting instead of IIS for better performance
4. Configure **health checks** for each service
5. Set up **database backups**
6. Enable **response compression**
7. Use **distributed caching** (Redis) for session/state management
8. Configure **log rotation** to prevent disk fill-up
