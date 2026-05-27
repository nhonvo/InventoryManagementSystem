---
trigger: always_on
glob:
description:
---

# InventoryAlert — Project Context

---

description: AI cold-start briefing for the InventoryAlert solution. Read before every session.
type: reference
status: active
version: 2.1
tags: [context, gemini, inventoryalert, ddd, dotnet, onboarding]
last_updated: 2026-05-01

---

> **Read this file first.** This is the AI cold-start briefing for this codebase.
> Run `/init` on every new session to re-index BM25 and sync context.

---

## Global Coding Standards

- Always follow Clean Architecture and Clean Code principles.
- Prefer readability over cleverness.
- Explore codebase before implementing changes (use BM25 search first).
- Plan before coding on complex tasks.
- Run targeted tests after changes (prefer single-scope test runs before full suite).
- Never commit sensitive data (API keys, credentials).

## Current Repository Layout (source of truth)

Top-level:

- `InventoryManagementSystem/` — .NET solution (`InventoryManagementSystem.sln`)
- `InventoryAlert.UI/` — Next.js UI
- `InventoryAlert.Wiki/` — Docusaurus docs (source in `InventoryAlert.Wiki/docs/`)
- `doc/` — internal engineering docs
- `.agents/` — agent workflows/skills/scripts (this folder)

Runtime components (current):

- API: `InventoryManagementSystem/InventoryAlert.Api`
- Worker: `InventoryManagementSystem/InventoryAlert.Worker` (Hangfire + SQS polling + handlers)
- Infra: PostgreSQL + Redis + Moto (AWS emulator) + Seq

---

## Reference Data (verified current)

This section is intended to be copied into prompts/issues. It is checked against the current repository layout and code paths.

### Current entry points

- API host: `InventoryManagementSystem/InventoryAlert.Api/Program.cs`
- Worker host: `InventoryManagementSystem/InventoryAlert.Worker/Program.cs`
- EF Core DbContext: `InventoryManagementSystem/InventoryAlert.Infrastructure/Persistence/Postgres/AppDbContext.cs`
- EF Core mappings: `InventoryManagementSystem/InventoryAlert.Infrastructure/Persistence/Postgres/Configurations/`

### Current data model (high level)

- Postgres entities: `InventoryManagementSystem/InventoryAlert.Domain/Entities/Postgres/`
  - `User`, `StockListing`, `WatchlistItem`, `AlertRule`, `Trade`, `Notification`
  - `PriceHistory`, `StockMetric`, `EarningsSurprise`, `RecommendationTrend`, `InsiderTransaction`
- DynamoDB read models: `InventoryManagementSystem/InventoryAlert.Domain/Entities/Dynamodb/`
  - `inventoryalert-market-news` (`MarketNewsDynamoEntry`)
  - `inventoryalert-company-news` (`CompanyNewsDynamoEntry`)

### Current “hot spots”

- Market + symbol intelligence + caching: `InventoryManagementSystem/InventoryAlert.Api/Services/StockDataService.cs`
- Portfolio/trades logic: `InventoryManagementSystem/InventoryAlert.Api/Services/PortfolioService.cs`
- Scheduled quote sync + evaluation: `InventoryManagementSystem/InventoryAlert.Worker/ScheduledJobs/SyncPricesJob.cs`
- Native SQS polling + idempotency: `InventoryManagementSystem/InventoryAlert.Worker/ScheduledJobs/ProcessQueueJob.cs`
- Event routing: `InventoryManagementSystem/InventoryAlert.Worker/IntegrationEvents/Routing/IntegrationMessageRouter.cs`

## What This Project Does

Real-time inventory management system with portfolio + watchlist + alerting.

- Tracks symbols (e.g., `AAPL`, `GOOGL`) via `StockListing` + `WatchlistItem`
- Records ownership changes via immutable `Trade` ledger (includes optional `Notes`)
- Evaluates `AlertRule` against live quotes and generates `Notification` events
- Syncs price/intelligence data from **Finnhub API** and caches reads via Redis
- Stores news read models in DynamoDB (market news + company news)

---

## Tech Stack

| Layer            | Technology                            |
| :--------------- | :------------------------------------ |
| Runtime          | .NET 10 (C# 12)                       |
| Web Framework    | ASP.NET Core Minimal Hosting          |
| ORM              | EF Core 10 + Npgsql (PostgreSQL)      |
| External API     | Finnhub REST (RestSharp client)       |
| Background Jobs  | Hangfire (Worker) + native SQS poller |
| Documentation    | Swashbuckle / Swagger                 |
| Tests            | xUnit + Moq + FluentAssertions        |
| Containerization | Docker + Docker Compose               |
| Search/Memory    | BM25+ (`.agents/scripts/core/`)       |

---

## Solution Structure

```
ojt-training/
├── .agents/
│   ├── GEMINI.md                      ← AI cold-start briefing (this file)
│   ├── rules/project-rules.md         ← project coding standards
│   ├── workflows/                     ← slash-command workflows
│   ├── skills/                        ← deep knowledge documents
│   └── scripts/core/                  ← BM25 indexer + search engine
├── doc/                               ← active feature specs and guides
│   ├── README.md                      ← doc index
│   ├── ROADMAP.md
│   ├── ENHANCEMENT_PLAN.md
│   ├── EVENT_DRIVEN_PLAN.md
│   ├── WALKTHROUGH.md
│   ├── plan/                          ← page-level specs
│   │   ├── 01_APPLICATION_REDEFINITION.md
│   │   ├── 02_API_ENDPOINT_PLAN.md
│   │   ├── 03_DYNAMODB_TABLE_DESIGN.md
│   │   ├── 04_JOBS_WORKERS_TELEGRAM.md
│   │   └── 05_UI_PLAN.md
│   └── archive/                       ← completed archived documentation
├── InventoryAlert.UI/                 ← Next.js 15 frontend
└── InventoryManagementSystem/
    ├── InventoryAlert.Api/            ← API project
    ├── InventoryAlert.Domain/         ← domain entities + interfaces
    ├── InventoryAlert.Infrastructure/ ← EF Core + repositories + Finnhub + SNS/SQS + Dynamo repos
    ├── InventoryAlert.Worker/         ← Hangfire + SQS poller + handlers
    ├── InventoryAlert.UnitTests/      ← unit tests
    ├── InventoryAlert.IntegrationTests/
    ├── InventoryAlert.E2ETests/
    └── InventoryAlert.ArchitectureTests/
```

---

## Key Domain Model

### Postgres entities (core)

| Entity          | Purpose / notes                                                 |
| :-------------- | :-------------------------------------------------------------- |
| `User`          | Auth identity (seeded in dev)                                   |
| `StockListing`  | Symbol directory (name, exchange, logo, industry, etc.)         |
| `WatchlistItem` | User → symbol subscription                                      |
| `Trade`         | Ownership ledger (immutable position changes); optional `Notes` |
| `AlertRule`     | User-defined alert conditions per symbol                        |
| `Notification`  | Persisted alert notifications                                   |

### Postgres entities (intelligence / caching)

| Entity                | Purpose                       |
| :-------------------- | :---------------------------- |
| `PriceHistory`        | Historical quotes / cache     |
| `StockMetric`         | Fundamental metrics cache     |
| `EarningsSurprise`    | Earnings history cache        |
| `RecommendationTrend` | Analyst recommendations cache |
| `InsiderTransaction`  | Insider trades cache          |

### DynamoDB read models

| DynamoDB table                | Entry model              |
| :---------------------------- | :----------------------- |
| `inventoryalert-market-news`  | `MarketNewsDynamoEntry`  |
| `inventoryalert-company-news` | `CompanyNewsDynamoEntry` |

---

## Key Services (API)

| Service               | What it does                                         |
| :-------------------- | :--------------------------------------------------- |
| `PortfolioService`    | Positions, cost basis, trades, portfolio alerts      |
| `WatchlistService`    | CRUD watchlist symbols for a user                    |
| `AlertRuleService`    | CRUD alert rules + evaluation helpers                |
| `NotificationService` | Read/ack notifications                               |
| `StockDataService`    | Quotes + profile + market intelligence (cache-first) |

---

## Key Service: `StockDataService`

| Method                     | What it does                                    |
| :------------------------- | :---------------------------------------------- |
| `GetQuoteAsync`            | Returns price quote (Redis cache-first)         |
| `GetProfileAsync`          | Returns company profile (Postgres cache-first)  |
| `GetFinancialsAsync`       | Returns cached financial metrics (fundamentals) |
| `GetEarningsAsync`         | Returns earnings surprises/history              |
| `GetRecommendationsAsync`  | Returns analyst recommendation trends           |
| `GetInsidersAsync`         | Returns insider transactions                    |
| `GetPeersAsync`            | Returns peer symbols                            |
| `GetCompanyNewsAsync`      | Returns company news (DynamoDB-backed)          |
| `GetMarketNewsAsync`       | Returns market news (DynamoDB-backed)           |
| `GetMarketStatusAsync`     | Returns market status                           |
| `GetMarketHolidaysAsync`   | Returns market holidays                         |
| `GetEarningsCalendarAsync` | Returns earnings calendar                       |
| `GetIpoCalendarAsync`      | Returns IPO calendar                            |
| `SearchSymbolsAsync`       | Search for tickers/companies                    |

---

## DI Registration Points

| What                                            | Where                                                                                                                           |
| :---------------------------------------------- | :------------------------------------------------------------------------------------------------------------------------------ |
| App settings bind/validate                      | `InventoryManagementSystem/InventoryAlert.Api/Program.cs`                                                                       |
| API service registration                        | `InventoryManagementSystem/InventoryAlert.Api/ServiceExtensions/InfrastructureServiceExtensions.cs` (`AddWebApiInfrastructure`) |
| Infrastructure (EF, repos, Finnhub, AWS, Redis) | `InventoryManagementSystem/InventoryAlert.Infrastructure/DependencyInjection.cs` (`AddInfrastructure`)                          |

---

## Known Pitfalls (avoid repeating)

- Always assign the final response/result **inside** `_unitOfWork.ExecuteTransactionAsync` (avoid capturing a default/blank value outside the lambda).
- Worker services are singleton by default: use `IServiceScopeFactory` to resolve scoped dependencies safely.
- Finnhub endpoints can fail / rate-limit / be plan-restricted: treat null/empty responses as normal; log and continue.

---

## Available Slash Commands

| Command         | When to use                                                                 |
| :-------------- | :-------------------------------------------------------------------------- |
| `/init`         | Start of every session — re-index BM25, sync context                        |
| `/plan`         | Design a feature before implementation                                      |
| `/feature-flow` | Master dev lifecycle: requirement → Domain → App → Infra → Web → Tests → PR |
| `/add-feature`  | Adding a new method/endpoint to an existing entity                          |
| `/add-entity`   | Adding a completely new domain entity end-to-end                            |
| `/db-migration` | Creating or applying an EF Core migration                                   |
| `/run-tests`    | Running tests + optional coverage report                                    |
| `/docker-run`   | Bringing up the full stack locally                                          |
| `/code-review`  | Pre-merge checklist                                                         |
| `/fix-build`    | Diagnosing build/test failures                                              |
| `/doc`          | Sync documentation after implementing a feature                             |
| `/search`       | BM25 search for context without reading full files                          |

---

## BM25 Search Quick Reference

```bash

# From project root — re-index
python .agents/scripts/core/bm25_indexer.py

# Search for context
python .agents/scripts/core/bm25_search.py "your query" -n 5

# Scope to a folder
python .agents/scripts/core/bm25_search.py "ExecuteTransactionAsync" -n 3 -f ".agents"

# Verify reliability score
python .agents/scripts/core/bm25_search.py "StockDataService GetQuoteAsync" --verify
```

---

## Environment Setup Checklist

- [ ] .NET 10 SDK: `dotnet --version` → `10.x`
- [ ] `dotnet-ef` tool: `dotnet ef --version`
- [ ] Docker Desktop running: `docker info`
- [ ] `appsettings.Development.json` created from `appsettings.Example.json`
- [ ] Finnhub API key set in `appsettings.Development.json`
- [ ] PostgreSQL reachable: `docker-compose up postgres -d`
- [ ] BM25 indexed: `python .agents/scripts/core/bm25_indexer.py`

---

## Seed Data

Seeded users (development/docker) via `InventoryManagementSystem/InventoryAlert.Infrastructure/Persistence/Postgres/DatabaseSeeder.cs`:

| Username | Email               |
| :------- | :------------------ |
| `admin`  | `admin@example.com` |
| `user1`  | `user1@example.com` |

---

## Antigravity AI Recommendations

> These settings help Antigravity give you the best responses for this project.

**Always run `/init` at the start of a new session** — it re-indexes BM25 so search results reflect the latest code.

**Use BM25 before reading full files** — saves tokens and finds the most relevant section:
```bash
python .agents/scripts/core/bm25_search.py "your question about the code" -n 5
```

**Preferred workflow order**:
1. `/init` → re-index
2. `/plan` → design + spec
3. `/feature-flow` or `/add-feature` → implement
4. `/run-tests` → verify
5. `/doc` → sync docs
6. `/code-review` → merge gate

**Skills to read before coding**:
- `.agents/skills/ddd-architecture/SKILL.md` — before placing any file
- `.agents/skills/testing-patterns/SKILL.md` — before writing any test
- `.agents/skills/finnhub-integration/SKILL.md` — before touching Finnhub

---

## Quick Links

| Resource               | Path                                          |
| :--------------------- | :-------------------------------------------- |
| AI System Core         | `.agents/GEMINI.md`                           |
| DDD Architecture Rules | `.agents/skills/ddd-architecture/SKILL.md`    |
| Testing Patterns       | `.agents/skills/testing-patterns/SKILL.md`    |
| Finnhub Integration    | `.agents/skills/finnhub-integration/SKILL.md` |
| Project Rules          | `.agents/rules/project-rules.md`              |
| Roadmap                | `doc/ROADMAP.md`                              |
