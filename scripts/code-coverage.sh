#!/usr/bin/env bash
set -e

# ========================
# Set Tools & Paths
# ========================
DOTNET="dotnet"
TEST_PROJECT="./src/test/InventoryAlert.UnitTests/InventoryAlert.UnitTests.csproj"
COVERAGE_DIR="coverage"

echo "[info] Ensuring tools are installed..."
dotnet tool install --global dotnet-reportgenerator-globaltool 2>/dev/null || true
dotnet tool install --global coverlet.console 2>/dev/null || true

# Add dotnet tools to PATH if not already present
export PATH="$PATH:$HOME/.dotnet/tools"

# ========================
# Run Tests & Coverage
# ========================
echo "[info] Cleaning old coverage data..."
rm -rf "$COVERAGE_DIR"
mkdir -p "$COVERAGE_DIR"

echo "[info] Cleaning build artifacts..."
$DOTNET clean "$TEST_PROJECT"

echo "[info] Running tests with coverlet collector..."
dotnet test "$TEST_PROJECT" --collect:"XPlat Code Coverage" --results-directory "$COVERAGE_DIR"

# ========================
# Generate HTML Report
# ========================
echo "[info] Generating merged report..."
reportgenerator \
  "-reports:$COVERAGE_DIR/**/coverage.cobertura.xml" \
  "-targetdir:$COVERAGE_DIR/html" \
  "-filefilters:-*.Migrations.*;-*.AppDbContextModelSnapshot.*;-*.g.cs;-*Program*" \
  "-classfilters:-*Program;-Program;-InventoryAlert.Domain.DTOs.*;-InventoryAlert.Domain.Entities.*;-InventoryAlert.Domain.External.*;-InventoryAlert.Domain.Constants.*;-InventoryAlert.Domain.Events.*;-InventoryAlert.Domain.Configuration.*;-InventoryAlert.Api.Extensions.*;-InventoryAlert.Api.Filters.*;-InventoryAlert.Api.Utilities.*;-InventoryAlert.Api.Validations.*;-InventoryAlert.Api.ServiceExtensions.*;-InventoryAlert.Api.Services.EventService;-InventoryAlert.Infrastructure.Migrations.*;-InventoryAlert.Infrastructure.Persistence.DynamoDb.*;-InventoryAlert.Infrastructure.Caching.*;-InventoryAlert.Infrastructure.Messaging.SqsQueueService;-InventoryAlert.Infrastructure.Hubs.*;-InventoryAlert.Infrastructure.External.Finnhub.*;-InventoryAlert.Infrastructure.Persistence.Postgres.AppDbContextFactory;-InventoryAlert.Infrastructure.Persistence.Postgres.DatabaseSeeder;-InventoryAlert.Infrastructure.DependencyInjection;-InventoryAlert.Infrastructure.Utilities.CorrelationIdEnricher;-InventoryAlert.Infrastructure.Utilities.LoggingConfiguration;-InventoryAlert.Worker.Hosting.*;-InventoryAlert.Worker.Extensions.*;-InventoryAlert.Worker.Filters.*;-InventoryAlert.Worker.Utilities.*;-InventoryAlert.Worker.DevDashboardAuthorizationFilter" \
  "-reporttypes:Html;TextSummary;MarkdownSummary"

echo "[info] Coverage report successfully generated at: $COVERAGE_DIR/html/index.html"
