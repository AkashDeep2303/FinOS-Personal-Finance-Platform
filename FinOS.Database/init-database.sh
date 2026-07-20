#!/usr/bin/env bash
# ============================================================================
#  FinOS - Database Initialization Script (Bash / Linux / macOS)
#  Waits for SQL Server, then runs all schema, seed, SP, and view scripts
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
DARKGRAY='\033[0;37m'
NC='\033[0m' # No Color

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Configuration (with defaults)
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
SQL_SERVER_HOST="${SQL_SERVER_HOST:-localhost}"
SQL_SERVER_PORT="${SQL_SERVER_PORT:-1433}"
SQL_SERVER_SA_PASSWORD="${SQL_SERVER_SA_PASSWORD:-CHANGE_ME_SQL_PASSWORD}"
SQL_SERVER_DATABASE="${SQL_SERVER_DATABASE:-FinOS}"

# Try to load .env if it exists
ENV_FILE="$(dirname "$SCRIPT_DIR")/FinOS.Backend/.env"
if [ -f "$ENV_FILE" ]; then
    set -a
    while IFS='=' read -r key value; do
        [[ -z "$key" || "$key" =~ ^# ]] && continue
        key=$(echo "$key" | xargs)
        value=$(echo "$value" | xargs)
        export "$key=$value"
    done < "$ENV_FILE"
    set +a
    # Re-read in case .env changed them
    SQL_SERVER_HOST="${SQL_SERVER_HOST:-localhost}"
    SQL_SERVER_PORT="${SQL_SERVER_PORT:-1433}"
    SQL_SERVER_SA_PASSWORD="${SQL_SERVER_SA_PASSWORD:-CHANGE_ME_SQL_PASSWORD}"
    SQL_SERVER_DATABASE="${SQL_SERVER_DATABASE:-FinOS}"
fi

echo ""
echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}   FinOS - Database Initialization${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""
echo -e "  Server:   ${SQL_SERVER_HOST},${SQL_SERVER_PORT}"
echo -e "  Database: ${SQL_SERVER_DATABASE}"
echo ""

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Check for sqlcmd
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
SQLCMD=""
if command -v sqlcmd &>/dev/null; then
    SQLCMD="sqlcmd"
elif [ -x "/opt/mssql-tools18/bin/sqlcmd" ]; then
    SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
elif [ -x "/opt/mssql-tools/bin/sqlcmd" ]; then
    SQLCMD="/opt/mssql-tools/bin/sqlcmd"
else
    echo -e "${RED}ERROR: sqlcmd not found!${NC}"
    echo -e "  Install SQL Server command-line tools:"
    echo -e "    Ubuntu/Debian: sudo apt install mssql-tools18"
    echo -e "    macOS:         brew install mssql-tools"
    echo -e "    Or use Docker: docker exec -it finos-sqlserver /opt/mssql-tools18/bin/sqlcmd"
    echo ""
    echo -e "  Alternatively, run this inside the Docker container:"
    echo -e "    docker exec -it finos-db-init /bin/bash"
    echo ""
    exit 1
fi

echo -e "  Using sqlcmd: ${SQLCMD}"
echo ""

# Determine if we need -C flag (Trust Server Certificate)
# mssql-tools18 requires -C flag for trust server certificate
# -I enables QUOTED_IDENTIFIER (required by filtered indexes + JSON string literals)
SQLCMD_FLAGS="-S ${SQL_SERVER_HOST},${SQL_SERVER_PORT} -U sa -P ${SQL_SERVER_SA_PASSWORD} -I"
if [[ "$SQLCMD" == *"tools18"* ]]; then
    SQLCMD_FLAGS="$SQLCMD_FLAGS -C"
fi

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Wait for SQL Server to be available
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
echo -e "${YELLOW}--- Waiting for SQL Server ---${NC}"

MAX_WAIT=120
WAITED=0
CONNECTED=false

while [ $WAITED -lt $MAX_WAIT ]; do
    if $SQLCMD $SQLCMD_FLAGS -Q "SELECT 1" &>/dev/null; then
        CONNECTED=true
        break
    fi
    echo -e "  ${DARKGRAY}Waiting for SQL Server... ($WAITED/$MAX_WAIT seconds)${NC}"
    sleep 3
    WAITED=$((WAITED + 3))
done

if [ "$CONNECTED" = true ]; then
    echo -e "  ${GREEN}[OK]${NC} SQL Server is available!"
else
    echo -e "${RED}ERROR: Could not connect to SQL Server after $MAX_WAIT seconds${NC}"
    exit 1
fi

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Create Database
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
echo ""
echo -e "${YELLOW}--- Creating Database ---${NC}"

$SQLCMD $SQLCMD_FLAGS -Q "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name='${SQL_SERVER_DATABASE}') CREATE DATABASE [${SQL_SERVER_DATABASE}];"

if [ $? -eq 0 ]; then
    echo -e "  ${GREEN}[OK]${NC} Database [${SQL_SERVER_DATABASE}] ensured"
else
    echo -e "  ${RED}[FAIL]${NC} Could not create database"
    exit 1
fi

# Database-specific flags
DB_SQLCMD_FLAGS="$SQLCMD_FLAGS -d ${SQL_SERVER_DATABASE}"

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Helper function to run a SQL script
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
run_sql_file() {
    local file_path=$1
    local file_name=$(basename "$file_path")

    if [ ! -f "$file_path" ]; then
        echo -e "  ${YELLOW}[SKIP]${NC} File not found: $file_name"
        return 0
    fi

    echo -e "  ${DARKGRAY}Running: $file_name${NC}"
    $SQLCMD $DB_SQLCMD_FLAGS -i "$file_path" -b 2>&1
    local exit_code=$?

    if [ $exit_code -eq 0 ]; then
        echo -e "  ${GREEN}[OK]${NC} $file_name"
    else
        echo -e "  ${RED}[FAIL]${NC} $file_name (exit code: $exit_code)"
        return 1
    fi
    return 0
}

ERRORS=0

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Step 1: Run Schema Scripts (001-008)
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
echo ""
echo -e "${YELLOW}--- Step 1: Schema Scripts (001-008) ---${NC}"

for i in 001 001b 002 003 004 005 006 007 008; do
    # Find file matching the pattern
    FILE=$(ls "$SCRIPT_DIR/Schema/${i}_"*.sql 2>/dev/null | head -1)
    if [ -n "$FILE" ]; then
        run_sql_file "$FILE" || ERRORS=$((ERRORS + 1))
    else
        echo -e "  ${YELLOW}[SKIP]${NC} No schema script found for prefix: $i"
    fi
done

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Step 2: Run Seed Data (001-003)
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
echo ""
echo -e "${YELLOW}--- Step 2: Seed Data (001-003) ---${NC}"

for i in 001 002 003; do
    FILE=$(ls "$SCRIPT_DIR/SeedData/${i}_"*.sql 2>/dev/null | head -1)
    if [ -n "$FILE" ]; then
        run_sql_file "$FILE" || ERRORS=$((ERRORS + 1))
    else
        echo -e "  ${YELLOW}[SKIP]${NC} No seed data script found for prefix: $i"
    fi
done

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Step 3: Run Stored Procedures
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
echo ""
echo -e "${YELLOW}--- Step 3: Stored Procedures ---${NC}"

SP_FILES=("Security_sp" "Core_sp" "Budget_sp" "Investment_sp" "Loan_sp" "Goals_sp" "Analytics_sp")

for sp in "${SP_FILES[@]}"; do
    FILE="$SCRIPT_DIR/StoredProcedures/${sp}.sql"
    if [ -f "$FILE" ]; then
        run_sql_file "$FILE" || ERRORS=$((ERRORS + 1))
    else
        echo -e "  ${YELLOW}[SKIP]${NC} Stored procedure file not found: ${sp}.sql"
    fi
done

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Step 4: Run Views
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
echo ""
echo -e "${YELLOW}--- Step 4: Views ---${NC}"

VIEW_FILES=("Dashboard_Views" "Analytics_Views" "Loan_Views" "Budget_Views" "Admin_Views" "Investment_Views")

for view in "${VIEW_FILES[@]}"; do
    FILE="$SCRIPT_DIR/Views/${view}.sql"
    if [ -f "$FILE" ]; then
        run_sql_file "$FILE" || ERRORS=$((ERRORS + 1))
    else
        echo -e "  ${YELLOW}[SKIP]${NC} View file not found: ${view}.sql"
    fi
done

# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Summary
# â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
echo ""
echo -e "${CYAN}========================================${NC}"
if [ $ERRORS -eq 0 ]; then
    echo -e "${GREEN}   Database Initialization Complete!${NC}"
    echo -e "${GREEN}   0 errors${NC}"
else
    echo -e "${YELLOW}   Database Initialization Complete with ${ERRORS} error(s)${NC}"
fi
echo -e "${CYAN}========================================${NC}"
echo ""
echo -e "  Database: ${SQL_SERVER_DATABASE}"
echo -e "  Schema scripts:   001-008"
echo -e "  Seed data:        001-003"
echo -e "  Stored procedures: Security_sp, Core_sp, Budget_sp, Investment_sp, Loan_sp, Goals_sp, Analytics_sp"
echo -e "  Views:            Dashboard_Views, Analytics_Views, Loan_Views, Budget_Views, Admin_Views, Investment_Views"
echo ""

if [ $ERRORS -gt 0 ]; then
    exit 1
fi
