---
title: Master Feature & Sequence Flow Blueprint
sidebar_position: 12
description: Master execution flow specification covering 5 end-to-end user and system sequence flows.
---

# 🗺️ Master Feature & Sequence Flow Blueprint

This master plan documents the 5 core end-to-end operational sequence flows of InventoryAlert.

---

## 📌 1. Flow 1: User Authentication & Persistent Login (Remember Me)

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant UI as Next.js UI (/login)
    participant API as AuthController
    participant Auth as AuthService
    participant DB as PostgreSQL Users

    User->>UI: Input Credentials & Check "Remember Me"
    UI->>API: POST /api/v1/Auth/login { username, password, rememberMe }
    API->>Auth: LoginAsync(request)
    Auth->>DB: GetByUsernameAsync(username)
    DB-->>Auth: User (with PasswordHash)
    Auth->>Auth: Verify BCrypt Hash
    Auth->>Auth: Generate JWT Access Token (60m)
    Auth->>Auth: Generate Refresh Token Cookie (30d if RememberMe, else 7d)
    Auth-->>API: AuthTokenPair
    API-->>UI: 200 OK + JWT AccessToken + httpOnly Refresh Cookie
    UI->>UI: Save JWT in localStorage & remembered_username
    UI-->>User: Redirect to Dashboard (/)
```

---

## 📌 2. Flow 2: Watchlist Management & Finnhub Symbol Auto-Discovery

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant UI as Next.js UI (/watchlist)
    participant API as WatchlistController
    participant Service as WatchlistService
    participant Finnhub as Finnhub REST API
    participant DB as PostgreSQL

    User->>UI: Enter Ticker Symbol "NVDA"
    UI->>API: POST /api/v1/Watchlist { symbol: "NVDA" }
    API->>Service: AddToWatchlistAsync(userId, "NVDA")
    Service->>DB: FindBySymbolAsync("NVDA")
    alt Listing Missing in Local DB
        Service->>Finnhub: GetProfileAsync("NVDA")
        Finnhub-->>Service: Profile Details (NVIDIA Corp, Tech)
        Service->>DB: Save New StockListing Entity
    end
    Service->>DB: Create WatchlistItem (Observation Only)
    Service-->>API: WatchlistItemResponse
    API-->>UI: 201 Created
```

---

## 📌 3. Flow 3: Portfolio Ledger & Trade-Driven Position Derivation

> **Architectural Rule**: Portfolio positions are derived strictly from actual trades in `Trades`. Watchlist items are observation-only.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant UI as Next.js UI (/portfolio)
    participant API as PortfolioController
    participant Service as PortfolioService
    participant DB as PostgreSQL Trades

    User->>UI: Record Trade (BUY 10 AAPL @ $180)
    UI->>API: POST /api/v1/Portfolio/trades
    API->>Service: RecordTradeAsync(request)
    Service->>DB: Save Trade Record (Immutable Ledger Entry)
    Service-->>API: TradeResponse
    API-->>UI: 201 Created
    
    User->>UI: Open Portfolio View
    UI->>API: GET /api/v1/Portfolio/positions
    API->>Service: GetPositionsPagedAsync(userId)
    Service->>DB: GetTradedSymbolsPagedAsync(userId)
    Service->>Service: Compute HoldingsCount = SUM(Buy) - SUM(Sell)
    Service-->>API: PagedList<PortfolioPositionResponse>
    API-->>UI: 200 OK (Displays Traded Positions Only)
```

---

## 📌 4. Flow 4: Real-time Alert Rule Evaluation & SignalR Push

```mermaid
sequenceDiagram
    autonumber
    participant Worker as SyncPricesJob
    participant Finnhub as Finnhub /quote
    participant Evaluator as AlertRuleEvaluator
    participant Redis as Redis Backplane
    participant API as Api SignalR Hub
    participant UI as Next.js UI

    Worker->>Worker: GetActiveSymbolsAsync()
    Worker->>Finnhub: GetQuoteAsync("TSLA")
    Finnhub-->>Worker: Quote $220.00
    Worker->>Evaluator: EvaluateAsync(AlertRule, $220.00)
    alt Rule Breached (PriceAbove $200.00)
        Evaluator-->>Worker: Breached = true
        Worker->>Worker: Create Notification Entity & Update Cooldown
        Worker->>Redis: Publish SignalR Alert Event
        Redis->>API: Relay Notification Message
        API->>UI: WebSocket Push Notification
        UI-->>User: Visual Alert Toast & Sound
    end
```

---

## 📌 5. Flow 5: Consolidated Daily Fundamentals Sync (`SyncStockFundamentalsJob`)

```mermaid
sequenceDiagram
    autonumber
    participant Hangfire as Hangfire Scheduler (06:10 UTC)
    participant Job as SyncStockFundamentalsJob
    participant DB as PostgreSQL
    participant Finnhub as Finnhub REST

    Hangfire->>Job: ExecuteAsync()
    Job->>DB: GetActiveSymbolsAsync()
    loop For Each Active Symbol (with 1s Rate-Limit Delay)
        Job->>Finnhub: GET /stock/metric
        Finnhub-->>Job: Financial Metrics (P/E, Beta, 52W High/Low)
        Job->>Finnhub: GET /stock/earnings
        Finnhub-->>Job: EPS Surprises
        Job->>Finnhub: GET /stock/recommendation
        Finnhub-->>Job: Analyst Buy/Hold/Sell Trends
        Job->>Finnhub: GET /stock/insider-transactions
        Finnhub-->>Job: SEC Insider Trades
        Job->>DB: Upsert Metrics, Earnings, Recommendations, Insiders
    end
    Job->>DB: SaveChangesAsync()
    Job-->>Hangfire: JobStatus.Success
```
