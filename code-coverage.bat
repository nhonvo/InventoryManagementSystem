@echo off
REM ========================
REM Set Tools & Paths
REM ========================
SET "dotnet=dotnet"
SET "testproject=./src/test/InventoryAlert.UnitTests/InventoryAlert.UnitTests.csproj"
SET "coveragedir=coverage"

echo [info] Ensuring tools are installed...
dotnet tool install --global dotnet-reportgenerator-globaltool
dotnet tool install --global coverlet.console

REM ========================
REM Run Tests & Coverage
REM ========================
echo [info] Cleaning old coverage data...
if exist "%coveragedir%" rd /s /q "%coveragedir%"
mkdir "%coveragedir%"

echo [info] Cleaning build artifacts...
%dotnet% clean %testproject%

echo [info] Running tests with coverlet collector...
dotnet test %testproject% --collect:"XPlat Code Coverage" --results-directory %coveragedir%

REM ========================
REM Generate HTML Report
REM ========================
echo [info] Generating merged report...
reportgenerator ^
  "-reports:%coveragedir%\**\coverage.cobertura.xml" ^
  "-targetdir:%coveragedir%\html" ^
  "-filefilters:-*.Migrations.*;-*.AppDbContextModelSnapshot.*;-*.g.cs;-*Program*" ^
  "-classfilters:-*Program;-Program;-InventoryAlert.Domain.DTOs.*;-InventoryAlert.Domain.Entities.*;-InventoryAlert.Domain.External.*;-InventoryAlert.Domain.Constants.*;-InventoryAlert.Domain.Events.*;-InventoryAlert.Domain.Configuration.*;-InventoryAlert.Api.Extensions.*;-InventoryAlert.Api.Filters.*;-InventoryAlert.Api.Utilities.*;-InventoryAlert.Api.Validations.*;-InventoryAlert.Api.ServiceExtensions.*;-InventoryAlert.Api.Services.EventService;-InventoryAlert.Infrastructure.Migrations.*;-InventoryAlert.Infrastructure.Persistence.DynamoDb.*;-InventoryAlert.Infrastructure.Caching.*;-InventoryAlert.Infrastructure.Messaging.SqsQueueService;-InventoryAlert.Infrastructure.Hubs.*;-InventoryAlert.Infrastructure.External.Finnhub.*;-InventoryAlert.Infrastructure.Persistence.Postgres.AppDbContextFactory;-InventoryAlert.Infrastructure.Persistence.Postgres.DatabaseSeeder;-InventoryAlert.Infrastructure.DependencyInjection;-InventoryAlert.Infrastructure.Utilities.CorrelationIdEnricher;-InventoryAlert.Infrastructure.Utilities.LoggingConfiguration;-InventoryAlert.Worker.Hosting.*;-InventoryAlert.Worker.Extensions.*;-InventoryAlert.Worker.Filters.*;-InventoryAlert.Worker.Utilities.*;-InventoryAlert.Worker.DevDashboardAuthorizationFilter" ^
  "-reporttypes:Html;TextSummary;MarkdownSummary"

REM ========================
REM Open the Report
REM ========================
echo [info] Opening HTML report...
start "" "%coveragedir%\html\index.html"
