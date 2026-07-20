#!/bin/bash
# FinOS - Stop All Services

echo "Stopping FinOS services..."

# Kill .NET processes running FinOS services
pkill -f "FinOS.Gateway" 2>/dev/null
pkill -f "FinOS.Identity.API" 2>/dev/null
pkill -f "FinOS.CoreFinance.API" 2>/dev/null
pkill -f "FinOS.Budget.API" 2>/dev/null
pkill -f "FinOS.Investment.API" 2>/dev/null
pkill -f "FinOS.Loan.API" 2>/dev/null
pkill -f "FinOS.Goals.API" 2>/dev/null
pkill -f "FinOS.Analytics.API" 2>/dev/null
pkill -f "FinOS.AIAssistant.API" 2>/dev/null
pkill -f "FinOS.Notification.API" 2>/dev/null
echo "✓ .NET services stopped"

# Kill Vue dev server
pkill -f "vite.*5173" 2>/dev/null
echo "✓ Vue frontend stopped"

# Stop Docker infrastructure
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"
docker compose -f docker-compose.infra.yml down 2>/dev/null
echo "✓ Docker infrastructure stopped"

echo "All FinOS services stopped."
