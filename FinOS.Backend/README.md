# FinOS - Personal Finance Management System
## Complete Local Development Guide (100% FREE)

### Architecture Overview
```
┌─────────────────────────────────────────────────────────┐
│                    YOUR LAPTOP                           │
│                                                         │
│  ┌──────────────┐     ┌────────────────────────────┐   │
│  │  Vue.js 3    │────▶│  API Gateway (YARP)        │   │
│  │  Frontend    │     │  http://localhost:6000      │   │
│  │  :5173       │     └──────┬─────────────────────┘   │
│  └──────────────┘            │                           │
│                     ┌───────┴───────┐                   │
│                     │  Route /api/* │                    │
│                     └───────┬───────┘                    │
│          ┌──────┬──────┬───┴──┬─────┬─────┬─────┐      │
│          ▼      ▼      ▼      ▼     ▼     ▼     ▼      │
│       Identity Core  Budget Invest Loan Goals Analytics │
│        :5001  :5002 :5003  :5004 :5005 :5006 :5007    │
│                       AI(:5008)  Notification(:5009)    │
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │  Docker Infrastructure (ONLY databases/mq)       │  │
│  │  SQL Server :1433  Redis :6379  RabbitMQ :5672   │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘

IIS Alternative:
  ┌──────────────────────────────────────────────────┐
  │  Windows IIS (FREE - built into Windows)         │
  │  Each microservice = separate IIS Website        │
  │  Same ports, managed by IIS App Pools            │
  └──────────────────────────────────────────────────┘
```

### Tech Stack (ALL FREE)
| Component | Technology | License |
|-----------|-----------|---------|
| Database | SQL Server 2022 Developer | Free for dev |
| ORM | ADO.NET + Dapper | Free (MIT) |
| Backend | .NET 8 | Free (MIT) |
| Frontend | Vue 3 + Vite | Free (MIT) |
| Cache | Redis 7 | Free (BSD) |
| Message Queue | RabbitMQ 3 | Free (MPL) |
| API Gateway | YARP | Free (MIT) |
| Web Server | Windows IIS | Free (built-in) |
| CQRS | MediatR | Free (MIT) |
| Validation | FluentValidation | Free (MIT) |

### Prerequisites
1. **Docker Desktop** - [Download](https://docs.docker.com/get-docker/)
2. **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
3. **Node.js 18+** - [Download](https://nodejs.org/)
4. **Git** - [Download](https://git-scm.com/)
5. **(Optional) Windows IIS** - For production-like hosting on Windows

---

## Quick Start (3 Steps)

### Step 1: Start Infrastructure
```bash
cd FinOS.Backend
docker compose -f docker-compose.infra.yml up -d
```
This starts SQL Server (with auto-init) and Redis.

### Step 2: Start Backend
**Option A: Kestrel (Dev) - Linux/Mac:**
```bash
cd FinOS.Backend
chmod +x start-all.sh
./start-all.sh
```

**Option A: Kestrel (Dev) - Windows:**
```powershell
cd FinOS.Backend
.\start-all.ps1
```

**Option B: IIS (Windows) - Production-like:**
```powershell
cd FinOS.Backend\IIS
.\install-iis-features.ps1    # First time only
.\deploy-to-iis.ps1           # Deploy all services
```

### Step 3: Start Frontend
```bash
cd FinOS.Frontend
npm install
npm run dev
```
Open http://localhost:5173

---

## Repository Structure

### FinOS.Database/
```
FinOS.Database/
├── Schema/           (8 files - CREATE TABLE scripts)
├── StoredProcedures/ (7 files - 47+ stored procedures)
├── Views/            (6 files - 25+ views)
├── SeedData/         (3 files - reference data)
├── Jobs/             (5 files - SQL Agent jobs)
├── Manual/           (5 files - utility scripts)
└── README.md
```

### FinOS.Backend/
```
FinOS.Backend/
├── FinOS.sln
├── .env                              # Environment variables
├── docker-compose.infra.yml          # Docker infra only
├── start-all.sh / start-all.ps1      # Start everything
├── stop-all.sh / stop-all.ps1        # Stop everything
├── APIGateways/
│   └── FinOS.Gateway/                # YARP API Gateway :6000
├── Shared/
│   └── FinOS.Common/                 # ADO.NET + Dapper shared lib
├── Services/
│   ├── Identity/    (4 projects)     # :5001
│   ├── CoreFinance/ (4 projects)     # :5002
│   ├── Budget/      (4 projects)     # :5003
│   ├── Investment/  (4 projects)     # :5004
│   ├── Loan/        (4 projects)     # :5005
│   ├── Goals/       (4 projects)     # :5006
│   ├── Analytics/   (4 projects)     # :5007
│   ├── AIAssistant/ (4 projects)     # :5008
│   └── Notification/(4 projects)     # :5009
└── IIS/
    ├── install-iis-features.ps1
    ├── deploy-to-iis.ps1
    ├── remove-iis-sites.ps1
    ├── web.config.template
    └── README-IIS.md
```

### FinOS.Frontend/
```
FinOS.Frontend/
├── package.json
├── vite.config.js
├── src/
│   ├── api/        (9 files - API layer)
│   ├── stores/     (8 files - Pinia stores)
│   ├── router/     (1 file - 13 routes)
│   ├── views/      (12 files - pages)
│   ├── components/ (4 files - shared components)
│   └── assets/     (CSS)
└── .env.development
```

---

## Service URLs
| Service | URL | Swagger |
|---------|-----|---------|
| Vue Frontend | http://localhost:5173 | - |
| API Gateway | http://localhost:6000 | http://localhost:6000/swagger |
| Identity | http://localhost:5001 | http://localhost:5001/swagger |
| CoreFinance | http://localhost:5002 | http://localhost:5002/swagger |
| Budget | http://localhost:5003 | http://localhost:5003/swagger |
| Investment | http://localhost:5004 | http://localhost:5004/swagger |
| Loan | http://localhost:5005 | http://localhost:5005/swagger |
| Goals | http://localhost:5006 | http://localhost:5006/swagger |
| Analytics | http://localhost:5007 | http://localhost:5007/swagger |
| AI Assistant | http://localhost:5008 | http://localhost:5008/swagger |
| Notification | http://localhost:5009 | http://localhost:5009/swagger |
| RabbitMQ Mgmt | http://localhost:15672 | finos / finos@2024 |

---

## IIS Hosting (Windows - FREE)

IIS is built into Windows 10/11 Pro and is completely free. No licenses needed.

### Setup Steps:
1. Enable IIS: `.\IIS\install-iis-features.ps1`
2. Deploy services: `.\IIS\deploy-to-iis.ps1`
3. Access via same ports (5001-6000)

### How it works:
- Each microservice is published as a standalone .NET 8 app
- Each gets its own IIS Application Pool (No Managed Code)
- Each gets its own IIS Website bound to its port
- IIS handles process management, auto-restart, and logging
- No Kestrel console windows needed

### Advantages over Kestrel:
- Auto-restart on crash
- Windows Event Log integration
- Process isolation per app pool
- Production-like environment
- No console windows cluttering desktop

---

## Cost Summary: ₹0 / $0

| Component | Cost |
|-----------|------|
| SQL Server Developer | Free |
| .NET 8 | Free |
| Vue.js 3 | Free |
| Docker Desktop (personal) | Free |
| Redis | Free |
| RabbitMQ | Free |
| Windows IIS | Free (built-in) |
| Visual Studio Code | Free |
| **TOTAL** | **₹0** |
