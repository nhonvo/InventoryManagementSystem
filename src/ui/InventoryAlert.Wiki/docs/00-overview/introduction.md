# InventoryAlert — Introduction

> A real-time stock portfolio and alert system built on .NET 10 + Next.js 15.

InventoryAlert is a full-stack system that monitors global stock prices via the **Finnhub API** and automatically dispatches per-user in-app alerts when user-defined thresholds are triggered. It is designed to be multi-user, event-driven, and observable by default.

## What Problem Does It Solve?

Users who maintain a portfolio of stock positions need to know immediately when prices hit critical levels — without constantly checking manually. InventoryAlert automates this through:

- **Background jobs** that sync market prices every 15 minutes across the full catalog.
- **Event-driven fan-out** that evaluates each user's thresholds independently.
- **In-app Notification hub** that delivers real-time alerts the moment a threshold is breached.
- **Full audit trails** so every trade and price movement is traceable via structured Seq logs.

## Core Capabilities

| Feature | Description |
|---|---|
| 🔍 **Global Stock Catalog** | Browse and search `StockListing` entries (DB-first with Finnhub fallback) |
| 📊 **Portfolio Management** | Track positions via `Trade` ledger (Buy/Sell/Dividend/Split) with dynamic cost-basis |
| 👁️ **Watchlist** | Watch-only tracking without requiring an open position |
| ⚡ **Alert Rules** | Define threshold conditions (`PriceAbove`, `PriceBelow`, `PercentDropFromCost`, `LowHoldingsCount`) |
| 📈 **Price Sync** | Automatic global price updates from Finnhub every 15 minutes → `PriceHistory` |
| 📉 **Market Intelligence** | Basic Financials, Earnings Surprises, Analyst Recommendations, Insider Transactions |
| 🏢 **Company Profiles** | Auto-fetches Ticker metadata (Logo, Industry, Website, Exchange) on demand |
| 🔔 **In-App Notifications** | Bell-badge hub — breaches write `Notification` rows; UI polls every 30s |
| 🗓️ **Market Calendars** | Earnings release calendar, IPO calendar, and exchange holiday list |
| 📰 **Market & Company News** | Per-ticker and general news from Finnhub, stored permanently in DynamoDB |
| 🔒 **Multi-User Isolation** | Per-user portfolio/alerts/watchlist fully isolated; global market data shared |

## High-Level Component Map

```
┌──────────────┐   REST    ┌───────────────────┐   EF Core  ┌────────────┐
│  Next.js UI  │ ────────▶ │ InventoryAlert    │ ─────────▶ │ PostgreSQL │
│  :3000       │           │ .Api :8080         │            └────────────┘
└──────────────┘           └───────────────────┘
                                    │  SQS                 ┌────────────┐
                           ┌────────▼─────────┐  ────────▶ │  DynamoDB  │
                           │ InventoryAlert   │            │  (News)    │
                           │ .Worker          │            └────────────┘
                           └──────────────────┘
                                    │
                    ┌───────────────┼───────────────┐
                    ▼               ▼               ▼
               Finnhub API     Redis Cache      Seq Logs
                                               :5341
```

## 🌐 Production Environment & Endpoints

| Service / Interface | Production URL | Description |
|---|---|---|
| 🌐 **Production API Host** | [https://inventorymanagementsystem-s55e.onrender.com](https://inventorymanagementsystem-s55e.onrender.com) | Main API ingress host on Render |
| 📜 **Swagger OpenAPI UI** | [https://inventorymanagementsystem-s55e.onrender.com/swagger](https://inventorymanagementsystem-s55e.onrender.com/swagger) | Interactive API exploration and testing |
| 💎 **Scalar API Reference** | [https://inventorymanagementsystem-s55e.onrender.com/scalar/v1](https://inventorymanagementsystem-s55e.onrender.com/scalar/v1) | Modern, searchable OpenAPI documentation |
| 💓 **Production Health Check** | [https://inventorymanagementsystem-s55e.onrender.com/healthz](https://inventorymanagementsystem-s55e.onrender.com/healthz) | Live service status endpoint pinged by keep-alive |
| 🗄️ **DynamoDB Admin Proxy** | [https://inventorymanagementsystem-s55e.onrender.com/aws](https://inventorymanagementsystem-s55e.onrender.com/aws) | Moto proxy endpoint for local `dynamodb-admin` GUI |
| 🔍 **Production Monitoring & Bug Tracing** | [Production Monitoring & Tracing Guide](../07-dev-maintenance/production-monitoring-and-tracing) | Operational runbook for production logs, correlation IDs, and error debugging |

## Seed Accounts

| Username | Password | Role |
|---|---|---|
| `admin` | `password` | Admin |
| `user1` | `password` | User |

## Quick Links

- [Getting Started →](../07-dev-maintenance/getting-started)
- [Production Monitoring & Bug Tracing →](../07-dev-maintenance/production-monitoring-and-tracing)
- [Architecture Overview →](../02-architecture-techstack/architecture-overview)
- [Data Model →](../03-data-model/data-model)
- [API Reference →](../05-api-services/internal-api)
- [Background Workers →](../06-background-jobs/workers)
