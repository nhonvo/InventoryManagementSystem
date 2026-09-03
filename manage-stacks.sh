#!/usr/bin/env bash
set -e

MODE="${1:-status}"
SUB_TARGET="${2:-}"

# Compose File Paths (Grouped inside docker/ directory)
DEV_COMPOSE="docker/docker-compose.yml"
PROD_COMPOSE="docker/docker-compose.prod.yml"
TOOLS_COMPOSE="docker/docker-compose.tools.yml"

show_help() {
  echo "================================================================="
  echo " 🚀 INVENTORYALERT STACK ORCHESTRATOR"
  echo "================================================================="
  echo "Usage: ./manage-stacks.sh [command]"
  echo ""
  echo "Full Docker Stacks:"
  echo "  all-up          Start Dev, Prod, and Tools stacks simultaneously"
  echo "  all-down        Stop all stacks and tools"
  echo "  dev-up          Start Local Dev Stack (API :8080, Worker :8081, UI :3000, Local Postgres :5433, Local Redis :6379, Moto :5000)"
  echo "  dev-down        Stop Local Dev Stack"
  echo "  prod-up         Start Production Stack (Remote Neon PG, Upstash Redis, API :8090, Worker :8091, UI :3005, Moto :5001)"
  echo "  prod-down       Stop Production Stack"
  echo "  tools-up        Start DB Management Tools (pgAdmin :5051, pre-connected to DEV & PROD)"
  echo "  tools-down      Stop DB Management Tools"
  echo "  rebuild         Rebuild all images and restart all stacks with fresh initialization"
  echo "  status          Show container and port status across all stacks"
  echo ""
  echo "Local Run Options (Without Docker or Hybrid):"
  echo "  infra-up        Start ONLY backing services in Docker (DB :5433, Redis :6379, Moto :5000) for local host dev"
  echo "  infra-down      Stop backing services"
  echo "  run-api         Run .NET API natively on host (dotnet run)"
  echo "  run-worker      Run Background Worker natively on host (dotnet run)"
  echo "  run-ui          Run Next.js UI natively on host (npm run dev)"
  echo ""
  echo "Development & Testing Commands:"
  echo "  test            Run full .NET test suite (Unit, Integration, Architecture)"
  echo "  coverage        Run tests and generate HTML code coverage report"
  echo "  logs [dev|prod] Tail live logs for dev or prod stack (default: dev)"
  echo "  clean           Stop all stacks and wipe database & cache volumes"
  echo ""
  echo "Port Mapping Directory:"
  echo "  [DEV]   API: 8080 | Worker: 8081 | UI: 3000 | Postgres: 5433 | Redis: 6379 | Moto: 5000 | DynamoDB Admin: 8001 | Seq: 5341"
  echo "  [PROD]  API: 8090 | Worker: 8091 | UI: 3005 | Moto: 5001 | DynamoDB Admin: 8002 | Cloud: Neon Postgres & Upstash Redis"
  echo "  [TOOLS] pgAdmin: 5051 (auto-login, pre-loaded Dev & Prod servers)"
}

ensure_networks() {
  docker network create inventory-dev-net 2>/dev/null || true
  docker network create inventory-prod-net 2>/dev/null || true
}

case "$MODE" in
  dev-up)
    echo "🚀 Starting Local Dev Stack..."
    ensure_networks
    docker compose --project-directory . -p inventory-alert-dev -f "$DEV_COMPOSE" up -d --build
    ;;
  dev-down)
    echo "🛑 Stopping Local Dev Stack..."
    docker compose --project-directory . -p inventory-alert-dev -f "$DEV_COMPOSE" down
    ;;
  prod-up)
    echo "🚀 Starting Production Stack with Remote Cloud DBs..."
    ensure_networks
    docker compose --project-directory . -p inventory-alert-prod -f "$PROD_COMPOSE" up -d --build
    ;;
  prod-down)
    echo "🛑 Stopping Production Stack..."
    docker compose --project-directory . -p inventory-alert-prod -f "$PROD_COMPOSE" down
    ;;
  tools-up)
    echo "🛠️ Starting DB Management Tools (pgAdmin)..."
    ensure_networks
    docker compose --project-directory . -p inventory-alert-tools -f "$TOOLS_COMPOSE" up -d
    echo "✅ pgAdmin is ready at http://localhost:5051 (pre-connected to DEV & PROD)"
    ;;
  tools-down)
    echo "🛑 Stopping DB Management Tools..."
    docker compose --project-directory . -p inventory-alert-tools -f "$TOOLS_COMPOSE" down
    ;;
  infra-up)
    echo "📦 Starting Backing Services in Docker (DB, Redis, Moto, DynamoDB Admin, Seq)..."
    ensure_networks
    docker compose --project-directory . -p inventory-alert-dev -f "$DEV_COMPOSE" up -d db redis moto moto-init dynamodb-admin seq
    echo "✅ Backing infrastructure ready! You can now run API, Worker, and UI natively on your machine."
    echo "   • Run API:    ./manage-stacks.sh run-api"
    echo "   • Run Worker: ./manage-stacks.sh run-worker"
    echo "   • Run UI:     ./manage-stacks.sh run-ui"
    ;;
  infra-down)
    echo "🛑 Stopping Backing Services..."
    docker compose --project-directory . -p inventory-alert-dev -f "$DEV_COMPOSE" stop db redis moto dynamodb-admin seq
    ;;
  run-api)
    echo "⚡ Launching .NET API natively on host..."
    dotnet run --project src/InventoryAlert.Api
    ;;
  run-worker)
    echo "⚡ Launching Background Worker natively on host..."
    dotnet run --project src/InventoryAlert.Worker
    ;;
  run-ui)
    echo "⚡ Launching Next.js UI natively on host..."
    npm --prefix src/ui/InventoryAlert.UI run dev
    ;;
  all-up)
    echo "🚀 Starting DEV, PROD, and DB TOOLS stacks..."
    ensure_networks
    docker compose --project-directory . -p inventory-alert-dev -f "$DEV_COMPOSE" up -d --build
    docker compose --project-directory . -p inventory-alert-prod -f "$PROD_COMPOSE" up -d --build
    docker compose --project-directory . -p inventory-alert-tools -f "$TOOLS_COMPOSE" up -d
    echo "✅ All stacks started successfully!"
    ;;
  all-down)
    echo "🛑 Stopping all containers across all groups..."
    docker compose --project-directory . -p inventory-alert-tools -f "$TOOLS_COMPOSE" down || true
    docker compose --project-directory . -p inventory-alert-prod -f "$PROD_COMPOSE" down || true
    docker compose --project-directory . -p inventory-alert-dev -f "$DEV_COMPOSE" down || true
    echo "✅ All stacks stopped."
    ;;
  rebuild)
    echo "🔄 Cleaning and rebuilding all stacks fresh..."
    docker compose --project-directory . -p inventory-alert-tools -f "$TOOLS_COMPOSE" down -v || true
    docker compose --project-directory . -p inventory-alert-prod -f "$PROD_COMPOSE" down -v || true
    docker compose --project-directory . -p inventory-alert-dev -f "$DEV_COMPOSE" down -v || true
    ensure_networks
    echo "🚀 Rebuilding images with no cache..."
    docker compose --project-directory . -p inventory-alert-dev -f "$DEV_COMPOSE" up -d --build --force-recreate
    docker compose --project-directory . -p inventory-alert-prod -f "$PROD_COMPOSE" up -d --build --force-recreate
    docker compose --project-directory . -p inventory-alert-tools -f "$TOOLS_COMPOSE" up -d --build --force-recreate
    echo "✅ Stacks rebuilt and initialized with seed data!"
    ;;
  status)
    echo "================================================================="
    echo " 📊 INVENTORYALERT CONTAINER STATUS"
    echo "================================================================="
    docker ps -a --filter "name=inventory" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    ;;
  test)
    echo "🧪 Running full test suite..."
    dotnet test src/InventoryManagementSystem.sln
    ;;
  coverage)
    echo "📊 Running code coverage analysis..."
    ./scripts/code-coverage.sh
    ;;
  logs)
    TARGET="${SUB_TARGET:-dev}"
    if [ "$TARGET" = "prod" ]; then
      echo "📜 Tailing logs for Production stack..."
      docker compose --project-directory . -p inventory-alert-prod -f "$PROD_COMPOSE" logs -f
    else
      echo "📜 Tailing logs for Local Dev stack..."
      docker compose --project-directory . -p inventory-alert-dev -f "$DEV_COMPOSE" logs -f
    fi
    ;;
  clean)
    echo "🧹 Cleaning up all containers and volumes..."
    docker compose --project-directory . -p inventory-alert-tools -f "$TOOLS_COMPOSE" down -v || true
    docker compose --project-directory . -p inventory-alert-prod -f "$PROD_COMPOSE" down -v || true
    docker compose --project-directory . -p inventory-alert-dev -f "$DEV_COMPOSE" down -v || true
    docker network rm inventory-dev-net inventory-prod-net 2>/dev/null || true
    echo "✅ Cleaned up successfully."
    ;;
  *)
    show_help
    ;;
esac
