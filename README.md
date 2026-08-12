# InventoryAlert — Inventory & Stock Alerting System

InventoryAlert is a full-stack inventory management and stock monitoring system built with **.NET 10** and **Next.js 15**. It tracks stock prices via Finnhub, evaluates user-defined rules, and delivers in-app notifications (SignalR), with documentation published via Docusaurus and GitHub Actions.

## Repository layout

- `src/` — .NET solution (`InventoryManagementSystem.sln`), API, Worker, UI, and Wiki
- `doc/` — internal engineering documentation and architectural reports

## 📚 Internal Documentation Index

Detailed specifications and architectural guides located in the `doc/` folder:

- 📘 **[Complete Execution & Setup Guide](doc/COMPLETE_GUIDE.md)**: End-to-end local setup, AWS-free strategy, and free cloud deployment guide.
- 🚀 **[Free Cloud Deployment Plan](doc/FREE_CLOUD_DEPLOYMENT_PLAN.md)**: $0/month hosting guide for Supabase, Upstash, Render, Vercel, and GitHub Pages.
- 📁 **[Project Tree & Structure](doc/PROJECT_TREE.md)**: Comprehensive repository directory map and component breakdown.
- 🏢 **[Architecture Deep Scan Report](doc/ARCHITECTURE_SCAN_REPORT.md)**: Clean architecture audit findings and refactoring scorecard.
- 🏛️ **[Web API Architecture Checklist](doc/NET_WEB_API_ARCHITECTURE_CHECKLIST.md)**: Enterprise .NET Web API & DDD coding standards checklist.
- 🛠️ **[Refactoring Suggestions](doc/REFACTORING_SUGGESTIONS.md)**: Code optimization recommendations.

## Quick start (development)

Prereqs: Docker Desktop, .NET 10 SDK, Node.js 20.

### 0) Configuration & Security Setup
Before running the application, set up your configuration:
1. Copy `appsettings.Example.json` to create `appsettings.Development.json` in both project folders:
   - `src/InventoryAlert.Api/appsettings.Development.json`
   - `src/InventoryAlert.Worker/appsettings.Development.json`
2. Insert your Finnhub API Key in `appsettings.Development.json` (`Finnhub.ApiKey`).
3. *(Note: All environment-specific `appsettings.*.json` and `.env` files are gitignored. Never commit real API keys to repository!)*

### 1) Infrastructure (Docker)

```powershell
cd src
docker-compose up -d
```

### 2) Backend (API + Worker)

```powershell
# Run API (Terminal 1)
dotnet run --project src/InventoryAlert.Api

# Run Worker (Terminal 2)
dotnet run --project src/InventoryAlert.Worker
```

- API health: `http://localhost:5001/healthz` (or `http://localhost:8080/healthz`)
- Swagger UI: `http://localhost:5001/swagger`
- Hangfire Dashboard: `http://localhost:8081` (or `http://localhost:5002/hangfire`)
- DynamoDB Admin UI: `http://localhost:8001`

### 3) Frontend (UI)

```powershell
cd src/ui/InventoryAlert.UI
npm install
npm run dev
```

- UI: `http://localhost:3000`

### 4) Documentation (Docusaurus)

```powershell
cd InventoryAlert.Wiki
npm ci
npm run start
```

- Wiki site: `http://localhost:3001`

## Documentation publishing

- GitHub Pages (Docusaurus): `.github/workflows/deploy-wiki.yml`
- GitHub Wiki sync (markdown export): `.github/workflows/sync-github-wiki.yml`

## Observability

- Seq: `http://localhost:5341`

## Tech stack (actual)

- Backend: C# 12, .NET 10, EF Core, Hangfire, FluentValidation, SignalR, Scalar + Swagger
- Frontend: React 19, Next.js 15
- Infra: PostgreSQL, Redis, Moto (AWS emulator), Seq

## Docs rule

If you change a domain entity, endpoint, or execution flow, update the matching page under `InventoryAlert.Wiki/docs/` (and keep `doc/` references in sync when applicable).
