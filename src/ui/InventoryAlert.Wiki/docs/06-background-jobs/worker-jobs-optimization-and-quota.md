---
title: Worker Jobs Optimization & Finnhub Quota Strategy
sidebar_position: 3
description: Background worker job consolidation, active ticker rate limiting, and zero-collision scheduling.
---

# ⚙️ Worker Jobs Optimization & Finnhub Quota Strategy

This document details the background worker job architecture in `InventoryAlert.Worker`, focusing on job consolidation, Finnhub free-tier API rate limiting, and zero-collision scheduling.

---

## 📊 Consolidated Job Catalog (6 Total Jobs)

| Job Class Name | CRON Schedule | Role & Target Endpoint | Key Duty |
| :--- | :--- | :--- | :--- |
| **`SyncPricesJob`** | `*/15 * * * *` | Finnhub `/quote` | Active symbol fetch → `PriceHistory` insertion → `AlertRule` evaluation → `Notification` creation → SignalR push. |
| **`SyncStockFundamentalsJob`** | `10 6 * * *` | `/metric`, `/earnings`, `/recommendation`, `/insider-transactions` | Consolidated daily sync for basic financials, quarterly earnings, analyst trends, and SEC insider trades for active symbols. |
| **`NewsSyncJob`** | `5 */2 * * *` | Finnhub `/news` & `/company-news` | Consolidated market + active company news batch sync to DynamoDB read models. |
| **`CleanupPriceHistoryJob`** | `20 2 * * *` | Postgres `PriceHistory` | Purges price history snapshots older than 1 year to keep DB storage lean. |
| **`KeepAliveJob`** | `*/10 * * * *` | `http://127.0.0.1:8080/healthz` | Self-pings health endpoint to guarantee 24/7 Render free-tier container uptime ($0 cost). |
| **`ProcessQueueJob`** | Continuous Poller | AWS SQS `event-queue` | Native SQS listener, Redis deduplication (`msg:processed:{id}`), and integration event routing. |

---

## 📈 Finnhub API Rate-Limiting Strategy

1. **Free Tier Limit**: **60 API requests/minute**.
2. **Active Ticker Resolution (`GetActiveSymbolsAsync`)**:
   Background jobs do NOT scan all historic listings in the database. Instead, jobs fetch distinct symbols actively present in user `WatchlistItems`, `Trades`, or `AlertRules`.
3. **Throttling Delays**:
   `SyncStockFundamentalsJob` enforces a **1,000ms delay** between requests to ensure maximum throughput stays strictly below the 60 req/min limit.

---

## 🕒 Zero-Collision Staggered Schedule

Jobs run on distinct minute marks to avoid concurrent API call collisions:

```text
[Minute 00, 15, 30, 45] ➔ SyncPricesJob (Runs for ~50s)
[Minute 05]            ➔ NewsSyncJob (Runs every 2 hours at :05)
[Minute 10 (at 06:10)] ➔ SyncStockFundamentalsJob (Throttled 1s delay, runs ~3.3 mins)
[Minute 20 (at 02:20)] ➔ CleanupPriceHistoryJob (Runs ~2s)
```
