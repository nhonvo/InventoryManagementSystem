---
title: Complete Operational Guide & Setup Runbook
sidebar_position: 9
description: Comprehensive operational guide for running, testing, managing DynamoDB Admin, and deploying InventoryAlert.
---

# 📖 Complete Operational Guide & Setup Runbook

This guide covers local development startup, Docker environment initialization, DynamoDB visual GUI access, EF Core migrations, and production deployment operations.

---

## 🚀 1. Local Development Setup

### Prerequisites
- **.NET 10 SDK** (`dotnet --version` ➔ `10.x`)
- **Node.js 20+** & **npm**
- **Docker Desktop**
- **Finnhub API Key** (Free tier key from [finnhub.io](https://finnhub.io))

### Local Stack Launch
```bash
# 1. Start local PostgreSQL, Redis, and Moto AWS Emulator
docker-compose up -d

# 2. Run EF Core Migrations
dotnet ef database update --project src/InventoryAlert.Infrastructure --startup-project src/InventoryAlert.Api

# 3. Launch Web API Host (Port 8080)
dotnet run --project src/InventoryAlert.Api/InventoryAlert.Api.csproj

# 4. Launch Worker Host (Port 8081)
dotnet run --project src/InventoryAlert.Worker/InventoryAlert.Worker.csproj

# 5. Launch Next.js UI Frontend (Port 3000)
cd src/ui/InventoryAlert.UI
npm run dev
```

---

## 🗄️ 2. DynamoDB Visual Admin Access (`dynamodb-admin`)

### Accessing Production DynamoDB (Render Proxy)
Run `dynamodb-admin` pointing across the internet to the live Render proxy URL:

```powershell
# PowerShell / Terminal:
DYNAMO_ENDPOINT=https://inventorymanagementsystem-s55e.onrender.com/aws npx dynamodb-admin
```

1. Open `http://localhost:8001` in your browser.
2. View, scan, search, and manage production tables (`inventoryalert-market-news`, `inventoryalert-company-news`).

---

## 🧪 3. Running Test Suites

```bash
# Execute unit tests
dotnet test src/test/InventoryAlert.UnitTests/InventoryAlert.UnitTests.csproj

# Execute integration tests
dotnet test src/test/InventoryAlert.IntegrationTests/InventoryAlert.IntegrationTests.csproj
```

---

## 📦 4. Production Deployment

The project builds as a multi-stage Docker container deployed to Render:
- `ASPNETCORE_URLS=http://+:8080` (API host ingress)
- `ASPNETCORE_ENVIRONMENT=Production`
