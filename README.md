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

Prereqs: Docker, .NET 10 SDK, Node.js 20.

### 1) Infrastructure (Docker)

```powershell
cd InventoryManagementSystem
docker compose up -d db redis moto moto-init seq
```

### 2) Backend (API + Worker)

```powershell
dotnet run --project InventoryManagementSystem/InventoryAlert.Api
dotnet run --project InventoryManagementSystem/InventoryAlert.Worker
```

- API health: `http://localhost:5294/health`
- Swagger UI: `http://localhost:5294/swagger`
- Scalar API reference: `http://localhost:5294/scalar/v1`

### 3) Frontend (UI)

```powershell
cd InventoryAlert.UI
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
