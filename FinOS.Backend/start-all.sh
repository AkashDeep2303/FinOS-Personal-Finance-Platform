#!/bin/bash
# ============================================================================
# FinOS - Start All Services (Linux/macOS)
# Runs infrastructure via Docker, .NET services via Kestrel, Vue frontend via Vite
# Usage: ./start-all.sh
# ============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
DARKGRAY='\033[0;37m'
NC='\033[0m'

SA_PASSWORD="CHANGE_ME_SQL_PASSWORD"

echo -e "${CYAN}"
echo "  +==================================================+"
echo "  |          FinOS - Start All Services               |"
echo "  |     Personal Finance Management System            |"
echo "  +==================================================+"
echo -e "${NC}"

# --- Step 1: Check Prerequisites ---
echo -e "${YELLOW}--- Step 1/6: Checking prerequisites ---${NC}"

check_prereq() {
    if ! command -v "$1" &> /dev/null; then
        echo -e "  ${RED}[FAIL]${NC} $1 is not installed. Install: $2"
        exit 1
    fi
    local version=$($1 --version 2>/dev/null | head -1)
    echo -e "  ${GREEN}[OK]${NC} $1 found ($version)"
}

check_prereq docker  "https://docs.docker.com/get-docker/"
check_prereq dotnet  "https://dotnet.microsoft.com/download/dotnet/8.0"
check_prereq node    "https://nodejs.org/"
check_prereq npm     "https://nodejs.org/"

# Verify Docker daemon is running
if ! docker info &>/dev/null; then
    echo -e "  ${RED}[FAIL]${NC} Docker daemon is not running"
    exit 1
fi
echo -e "  ${GREEN}[OK]${NC} Docker daemon is running"

# --- Step 2: Start Infrastructure ---
echo ""
echo -e "${YELLOW}--- Step 2/6: Starting infrastructure ---${NC}"
docker compose -f docker-compose.infra.yml up -d
echo -e "  ${GREEN}[OK]${NC} Infrastructure containers started"

# --- Step 3: Wait for SQL Server ---
echo ""
echo -e "${YELLOW}--- Step 3/6: Waiting for SQL Server (up to 180s) ---${NC}"

sql_ready=false
for i in $(seq 1 36); do
    if docker exec finos-sqlserver /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$SA_PASSWORD" -C -Q "SELECT 1" &>/dev/null; then
        sql_ready=true
        break
    fi
    echo -e "  ${DARKGRAY}Waiting... ($i/36)${NC}"
    sleep 5
done

if [ "$sql_ready" = true ]; then
    echo -e "  ${GREEN}[OK]${NC} SQL Server is ready"
else
    echo -e "  ${RED}[FAIL]${NC} SQL Server did not become healthy in time"
    echo -e "  Check: docker logs finos-sqlserver"
    exit 1
fi



# --- Step 4: Build .NET Solution ---
echo ""
echo -e "${YELLOW}--- Step 4/6: Building .NET solution ---${NC}"
echo -e "  ${DARKGRAY}Restoring NuGet packages...${NC}"
dotnet restore FinOS.sln --verbosity quiet
echo -e "  ${DARKGRAY}Building solution (Debug)...${NC}"
dotnet build FinOS.sln --configuration Debug --verbosity quiet
echo -e "  ${GREEN}[OK]${NC} Build successful"

# --- Step 5: Start .NET Services ---
echo ""
echo -e "${YELLOW}--- Step 5/6: Starting microservices ---${NC}"

SERVICES=(
    "APIGateways/FinOS.Gateway:6000:Gateway"
    "Services/Identity/FinOS.Identity.API:5001:Identity"
    "Services/CoreFinance/FinOS.CoreFinance.API:5002:CoreFinance"
    "Services/Budget/FinOS.Budget.API:5003:Budget"
    "Services/Investment/FinOS.Investment.API:5004:Investment"
    "Services/Loan/FinOS.Loan.API:5005:Loan"
    "Services/Goals/FinOS.Goals.API:5006:Goals"
    "Services/Analytics/FinOS.Analytics.API:5007:Analytics"
    "Services/AIAssistant/FinOS.AIAssistant.API:5008:AI Assistant"
    "Services/Notification/FinOS.Notification.API:5009:Notification"
)

PIDS=()

for svc in "${SERVICES[@]}"; do
    IFS=':' read -r path port name <<< "$svc"
    echo -e "  Starting ${CYAN}$name${NC} on port $port..."
    dotnet run --project "$path" --urls "http://localhost:$port" --no-build &
    PIDS+=($!)
    sleep 0.5
done

# --- Step 6: Start Vue Frontend ---
echo ""
echo -e "${YELLOW}--- Step 6/6: Starting Vue frontend ---${NC}"

FRONTEND_DIR="$SCRIPT_DIR/../FinOS.Frontend"
if [ -d "$FRONTEND_DIR" ]; then
    cd "$FRONTEND_DIR"
    if [ ! -d "node_modules" ]; then
        echo -e "  ${DARKGRAY}Installing npm dependencies...${NC}"
        npm install --silent 2>/dev/null
    fi
    npm run dev &
    PIDS+=($!)
    cd "$SCRIPT_DIR"
    echo -e "  ${GREEN}[OK]${NC} Frontend started on http://localhost:5173"
else
    echo -e "  ${YELLOW}[WARN]${NC} Frontend directory not found at $FRONTEND_DIR"
fi

# --- Status Dashboard ---
echo ""
echo -e "${CYAN}  +==================================================+"
echo "  |          FinOS Services Running                   |"
echo "  +==================================================+"
echo "  |                                                    |"
echo "  |  Infrastructure:                                   |"
echo "  |    SQL Server:   localhost:1433                    |"
echo "  |    Redis:        localhost:6379                    |"
echo "  |                                                    |"
echo "  |  .NET Microservices:                               |"
echo "  |    Gateway:      http://localhost:6000             |"
echo "  |    Identity:     http://localhost:5001             |"
echo "  |    CoreFinance:  http://localhost:5002             |"
echo "  |    Budget:       http://localhost:5003             |"
echo "  |    Investment:   http://localhost:5004             |"
echo "  |    Loan:         http://localhost:5005             |"
echo "  |    Goals:        http://localhost:5006             |"
echo "  |    Analytics:    http://localhost:5007             |"
echo "  |    AI:           http://localhost:5008             |"
echo "  |    Notification: http://localhost:5009             |"
echo "  |                                                    |"
echo "  |  Frontend:                                         |"
echo "  |    Vue App:      http://localhost:5173             |"
echo "  |                                                    |"
echo "  |  Swagger UI:                                       |"
echo "  |    http://localhost:{PORT}/swagger                 |"
echo "  |                                                    |"
echo "  +==================================================+${NC}"
echo ""
echo -e "  To stop all services:  ${YELLOW}./stop-all.sh${NC}"
echo -e "  To re-init database:   ${YELLOW}../FinOS.Database/init-database.sh${NC}"
echo ""

# Wait for all background processes
wait
