---
description: Manual QA Testing Guide for the Inventory Alert System — step-by-step test cases and areas flagged for human review.
type: qa
status: active
version: 1.0
tags: [testing, qa, manual, review, checklist]
last_updated: 2026-04-05
---

# 🧪 Manual Testing & Review Guide

> **How to use this document:**
> - ✅ = Expected pass behavior
> - ❌ = Expected rejection / error behavior
> - 🔍 = **Needs human review** — business logic decision, edge case, or unvalidated behavior

**Base URL (Docker):** `http://localhost:5000`  
**Start environment:** `docker-compose up --build` from `InventoryManagementSystem/`

---

## 🚀 Pre-Flight: Start & Health Check

| #   | Step                                   | Expected                                                       |
| --- | -------------------------------------- | -------------------------------------------------------------- |
| 1   | Run `docker-compose up --build`        | All 5 containers start without error                           |
| 2   | Check `moto-init` logs                 | `SQS queue created`, `DynamoDB table created` messages visible |
| 3   | `GET http://localhost:5000/swagger`    | Swagger UI loads with all endpoints                            |
| 4   | Check `logs/inventoryalert.log` exists | Serilog has created the log file                               |

> 🔍 **REVIEW:** There is no `/health` endpoint defined. If the `api` or `worker` container crashes silently, there is no heartbeat to detect it. Consider adding `app.MapHealthChecks("/health")`.

---

## 🔐 Section 1: Authentication

### TC-AUTH-01 — Happy Path Login
```http
POST /api/auth/login
{ "username": "admin", "password": "admin123" }
```
- ✅ `200 OK` with `{ "token": "<jwt>" }`
- Copy the token — used in all subsequent `[Authorize]` calls

### TC-AUTH-02 — Wrong Password
```http
POST /api/auth/login
{ "username": "admin", "password": "wrong" }
```
- ✅ `401 Unauthorized` with message `"Invalid credentials."`

### TC-AUTH-03 — Call Protected Endpoint Without Token
```http
GET /api/products
(no Authorization header)
```
- ✅ `401 Unauthorized`

### TC-AUTH-04 — Call Protected Endpoint With Expired Token
- Wait for token to expire (2 hours), then call `GET /api/products`
- ✅ `401 Unauthorized`

> 🔍 **REVIEW:** Credentials (`admin` / `admin123`) are the hardcoded fallback in `AuthController`. In Docker, they should come from `appsettings.Docker.json → Auth:Username / Auth:Password`. Verify these are overridden in the Docker config; otherwise the fallback is always active, even in production images.

---

## 📦 Section 2: Product CRUD

Use `Authorization: Bearer <token>` for all requests in this section.

### TC-PROD-01 — Create Product (Valid)
```http
POST /api/products
{
  "name": "Apple iPhone 15",
  "tickerSymbol": "AAPL",
  "stockCount": 100,
  "originPrice": 950.00,
  "currentPrice": 920.00,
  "priceAlertThreshold": 0.2,
  "stockAlertThreshold": 10
}
```
- ✅ `201 Created` with response body including auto-generated `id`
- ✅ Response includes `Location` header pointing to `GET /api/products/{id}`
- Note the returned `id` for subsequent tests

### TC-PROD-02 — Create Product (Validation Failures)

| Sub-case                    | Payload Change                | Expected                                                  |
| --------------------------- | ----------------------------- | --------------------------------------------------------- |
| Empty name                  | `"name": ""`                  | ❌ `400` — `"Name is required."`                           |
| Name too long               | `"name": "A" * 257`           | ❌ `400` — `"Name must not exceed 256 characters."`        |
| Lowercase ticker            | `"tickerSymbol": "aapl"`      | ❌ `400` — `"TickerSymbol must be 1–16 uppercase letters"` |
| Ticker with numbers         | `"tickerSymbol": "AAP1"`      | ❌ `400` — regex rejects non-alpha characters              |
| `originPrice: 0`            | `"originPrice": 0`            | ❌ `400` — `"OriginPrice must be greater than 0."`         |
| `originPrice: -1`           | `"originPrice": -1`           | ❌ `400`                                                   |
| `currentPrice: -1`          | `"currentPrice": -1`          | ❌ `400` — `"CurrentPrice must be 0 or greater."`          |
| `stockCount: -1`            | `"stockCount": -1`            | ❌ `400`                                                   |
| `priceAlertThreshold: 1.1`  | `"priceAlertThreshold": 1.1`  | ❌ `400` — `"must be between 0.0 and 1.0"`                 |
| `priceAlertThreshold: -0.1` | `"priceAlertThreshold": -0.1` | ❌ `400`                                                   |

> 🔍 **REVIEW:** `tickerSymbol` is declared as `string?` (nullable) in `ProductRequest.cs` but the validator treats it as required. Confirm the swagger UI does not allow submitting without it. Check if `null` body field passes validation.

### TC-PROD-03 — Get All Products (Pagination)
```http
GET /api/products?pageNumber=1&pageSize=5
```
- ✅ `200 OK` with `PagedResult` shape: `{ items: [...], totalItems, pageNumber, pageSize }`
- ✅ Max page size is capped at `50` — test `?pageSize=999` returns only 50 items

### TC-PROD-04 — Get Product By ID (Cache Test)
```http
GET /api/products/{id}     ← first call: DB hit
GET /api/products/{id}     ← second call within 10 min: cache hit
```
- ✅ Both return `200 OK` with identical body
- To verify cache: Add a DB breakpoint or Serilog trace showing cache miss/hit

> 🔍 **REVIEW:** Cache TTL is hardcoded as `TimeSpan.FromMinutes(10)` in `ProductService.cs`. There is no configuration key for this. Consider externalizing it.

### TC-PROD-05 — Get Product By ID (Not Found)
```http
GET /api/products/99999
```
- ✅ `404 Not Found`

### TC-PROD-06 — Update Product (Full Overwrite)
```http
PUT /api/products/{id}
{
  "name": "Apple iPhone 15 Pro",
  "tickerSymbol": "AAPL",
  "stockCount": 80,
  "originPrice": 1050.00,
  "currentPrice": 1000.00,
  "priceAlertThreshold": 0.15,
  "stockAlertThreshold": 5
}
```
- ✅ `200 OK` with all fields updated
- ✅ Verify a subsequent `GET /api/products/{id}` returns new values (cache invalidated)

### TC-PROD-07 — Update Non-Existent Product
```http
PUT /api/products/99999
{ ... valid body ... }
```
- ✅ `404 Not Found`

> 🔍 **REVIEW:** The update currently throws `KeyNotFoundException` if not found, which is caught by `ExceptionHandlingMiddleware` and converted to `404`. But the middleware maps `KeyNotFoundException` → `404` only if explicitly handled. Verify this mapping is set up in `ExceptionHandlingMiddleware.cs`.

### TC-PROD-08 — Delete Product
```http
DELETE /api/products/{id}
```
- ✅ `204 No Content`
- ✅ Follow-up `GET /api/products/{id}` returns `404`

### TC-PROD-09 — Delete Non-Existent Product
```http
DELETE /api/products/99999
```
- ✅ `404 Not Found`

---

## 📉 Section 3: Stock Count PATCH

### TC-STOCK-01 — Valid Patch
```http
PATCH /api/products/{id}/stock?stockCount=50
```
- ✅ `200 OK` — returns full `ProductResponse` with `stockCount: 50`

### TC-STOCK-02 — Set Stock to Zero
```http
PATCH /api/products/{id}/stock?stockCount=0
```
- ✅ `200 OK` — `stockCount: 0` is valid (product exists but out of stock)

### TC-STOCK-03 — Product Not Found
```http
PATCH /api/products/99999/stock?stockCount=10
```
- ✅ `404 Not Found`

### TC-STOCK-04 — Negative Stock Count
```http
PATCH /api/products/{id}/stock?stockCount=-5
```
- 🔍 **REVIEW:** There is **no validator** for the `stockCount` query parameter in `UpdateStockCount`. The code directly assigns `existing.StockCount = stockCount;` — a value of `-5` will be persisted. Consider adding a guard: `if (stockCount < 0) return BadRequest(...)`.

---

## 📈 Section 4: Finnhub Price Sync

### TC-SYNC-01 — Manual Trigger
```http
POST /api/products/sync-price
```
- ✅ `204 No Content` (runs synchronously, returns after completion)
- ✅ Check that `currentPrice` of products with valid tickers has been updated

> 🔍 **REVIEW:** The Docker `appsettings.Docker.json` has the Finnhub API key hardcoded: `"ApiKey": "d77d58hr01qp6afl79qgd77d58hr01qp6afl79r0"`. This is a **real API key committed to version control**. Even though `appsettings.Docker.json` may be gitignored, confirm with `git status` and `git log --all -- appsettings.Docker.json`. The key should be moved to a Docker secret or environment variable.

### TC-SYNC-02 — Invalid Ticker Symbol
- Create a product with `tickerSymbol: "XXXINVALID"`
- Trigger `POST /api/products/sync-price`
- ✅ `204 No Content` — the invalid ticker is **skipped** (not an error)
- ✅ Log file contains: `[ProductService] Null or zero price returned for XXXINVALID. Skipping.`

### TC-SYNC-03 — Scheduled Auto-Sync
- Configured in `appsettings.Docker.json → MinuteSyncCurrentPrice: 10`
- ✅ After 10 minutes, `currentPrice` updates automatically without manual trigger
- 🔍 **REVIEW:** Confirm the `MinuteSyncCurrentPrice` setting is actually read and applied to the Hangfire job cron expression in the Worker's `Program.cs`.

---

## 🚨 Section 5: Price Loss Alerts

### Setup for this section:
Create a product with `originPrice: 1000`, `currentPrice: 700`, `priceAlertThreshold: 0.2`.
- Drop% = (700-1000)/1000 = -0.3 → 30% loss → **exceeds** 20% threshold → should alert.

### TC-ALERT-01 — Product Triggering Alert
```http
GET /api/products/price-alerts
```
- ✅ `200 OK` — response array includes the product above
- ✅ `priceChangePercent` ≈ `-0.3000`
- ✅ `priceDiff` = `300.00`

### TC-ALERT-02 — Product Below Threshold (No Alert)
- Create product with `originPrice: 1000`, `currentPrice: 850`, `priceAlertThreshold: 0.2`
- Drop% = 15% < 20% threshold
- ✅ This product does **not** appear in the alert response

### TC-ALERT-03 — Cooldown Gate (60-Minute Spam Prevention)
- Trigger `GET /api/products/price-alerts` twice in 60 minutes for the same product
- Both should return the product in the list (the endpoint only **reads**, it doesn't update `LastAlertSentAt`)
- 🔍 **REVIEW:** `LastAlertSentAt` is **never written** by `GetPriceLossAlertsAsync`. The cooldown check reads `LastAlertSentAt` but nothing in the codebase sets it. This means the cooldown gate is **permanently bypassed** — it will always pass the `if (product.LastAlertSentAt.HasValue...)` check because `HasValue` is always `false`. Needs implementation.

### TC-ALERT-04 — Zero Price Edge Case
- Set `currentPrice: 0` or `originPrice: 0`
- ✅ Product is **skipped** — no alert produced (guarded by `if (product?.CurrentPrice is 0 || product?.OriginPrice is 0) continue`)

---

## 📨 Section 6: Event Publishing

### TC-EVENT-01 — Generic Event Publish
```http
POST /api/events
{
  "eventType": "inventoryalert.pricing.price-drop.v1",
  "payload": { "productId": 1, "symbol": "AAPL", "dropPercent": 0.25 }
}
```
- ✅ `202 Accepted`
- ✅ Record appears in `GET /api/events/logs/inventoryalert.pricing.price-drop.v1`

### TC-EVENT-02 — Missing EventType
```http
POST /api/events
{ "eventType": "", "payload": {} }
```
- ✅ `400 Bad Request` — `"EventType is required."`

### TC-EVENT-03 — Manual Market Alert
```http
POST /api/events/market-alert
{
  "productId": 1,
  "symbol": "AAPL",
  "originPrice": 950.00,
  "currentPrice": 700.00,
  "dropPercent": 0.263
}
```
- ✅ `202 Accepted`
- ✅ Worker logs `🚨 PRICE DROP ALERT` in container output

### TC-EVENT-04 — News Alert
```http
POST /api/events/news-alert
{
  "symbol": "TSLA",
  "headline": "Tesla recalls 500,000 vehicles",
  "source": "Reuters",
  "url": "https://reuters.com/article/example",
  "publishedAt": "2026-04-05T06:00:00Z"
}
```
- ✅ `202 Accepted`
- ✅ Worker `NewsHandler` persists `NewsRecord` to PostgreSQL

### TC-EVENT-05 — List Supported Event Types
```http
GET /api/events/types
```
- ✅ Returns all 5 event type strings:
  ```json
  [
    "inventoryalert.pricing.price-drop.v1",
    "inventoryalert.inventory.stock-low.v1",
    "inventoryalert.fundamentals.earnings.v1",
    "inventoryalert.fundamentals.insider-sell.v1",
    "inventoryalert.news.headline.v1"
  ]
  ```

### TC-EVENT-06 — Query Event Logs
```http
GET /api/events/logs/inventoryalert.pricing.price-drop.v1?limit=5
```
- ✅ Returns up to 5 DynamoDB log entries for that event type

> 🔍 **REVIEW:** Sending an **unknown event type** to `POST /api/events` (e.g. `"eventType": "foo.bar"`) is accepted and dispatched to SNS — no validation against `EventTypes.All` occurs at the controller level. The Worker's `UnknownEventHandler` will discard it, but it still pollutes the audit log. Consider adding `if (!EventTypes.IsKnown(request.EventType)) return BadRequest(...)`.

---

## 🔄 Section 7: Worker SQS Consumer (End-to-End)

### TC-WORKER-01 — Full Pipeline Smoke Test
1. Publish an event via `POST /api/events/market-alert`
2. Watch Worker Docker logs for: `🚨 PRICE DROP ALERT Symbol: AAPL`
3. ✅ Event flows: API → SNS (Moto) → SQS → PollSqsJob → PriceAlertHandler → log

### TC-WORKER-02 — Earnings Event Persistence
1. Publish:
```http
POST /api/events
{
  "eventType": "inventoryalert.fundamentals.earnings.v1",
  "payload": {
    "symbol": "AAPL",
    "period": "Q1 2026",
    "actualEPS": 2.18,
    "estimatedEPS": 2.10,
    "surprisePercent": 3.8
  }
}
```
2. ✅ Worker `EarningsHandler` persists `EarningsRecord` to PostgreSQL
3. ✅ Verify via direct DB: `SELECT * FROM "EarningsRecords";`

### TC-WORKER-03 — DLQ Escalation
- Temporarily break the Worker handler (e.g. comment out DB save, throw exception)
- Publish an event
- After 3 retries ✅ message appears in `inventory-event-dlq`
- 🔍 **REVIEW:** There is no monitoring or alerting on DLQ depth. If messages pile up in the DLQ, no one is notified. Consider adding a CloudWatch alarm (or Moto-equivalent) for `ApproximateNumberOfMessagesNotVisible > 0`.

---

## 📊 Section 8: Bulk Insert

### TC-BULK-01 — Valid Bulk Insert
```http
POST /api/products/bulk
[
  { "name": "Tesla Part A", "tickerSymbol": "TSLA", "stockCount": 50, "originPrice": 200.00, "currentPrice": 190.00, "priceAlertThreshold": 0.1, "stockAlertThreshold": 5 },
  { "name": "Microsoft Mouse", "tickerSymbol": "MSFT", "stockCount": 200, "originPrice": 30.00, "currentPrice": 28.00, "priceAlertThreshold": 0.15, "stockAlertThreshold": 20 }
]
```
- ✅ `204 No Content`
- ✅ Follow-up `GET /api/products` shows new items

### TC-BULK-02 — Bulk Insert with One Invalid Item
```http
POST /api/products/bulk
[
  { "name": "Valid Product", "tickerSymbol": "GOOG", ... },
  { "name": "", "tickerSymbol": "AAPL", ... }   ← invalid: empty name
]
```
- 🔍 **REVIEW:** FluentValidation rules apply per-item in a collection via `RuleForEach`. Verify whether `BulkInsertProductsAsync` triggers FluentValidation or bypasses it (the validation filter operates at the Controller level on the outer `IEnumerable<ProductRequest>`, not per-item). If one item is invalid, is the whole batch rejected or does it partially insert?

### TC-BULK-03 — Empty Bulk Insert
```http
POST /api/products/bulk
[]
```
- 🔍 **REVIEW:** Empty array → `AddRangeAsync([])` → likely a no-op. Should return `204` or `400`? Currently no guard for empty list.

---

## 🔍 Summary: All Items Needing Human Review

| ID  | Location                                        | Issue                                                                      | Priority |
| --- | ----------------------------------------------- | -------------------------------------------------------------------------- | -------- |
| A   | `AuthController.cs:26-27`                       | `admin`/`admin123` fallback always active if config missing                | 🔴 High   |
| B   | `appsettings.Docker.json:7`                     | Real Finnhub API key possibly committed in git history                     | 🔴 High   |
| C   | `ProductsController.cs` — `UpdateStockCount`    | No validation on negative `stockCount` query param                         | 🟡 Medium |
| D   | `ProductService.cs` — `GetPriceLossAlertsAsync` | `LastAlertSentAt` cooldown is never written — gate always bypassed         | 🟡 Medium |
| E   | `EventsController.cs` — `PublishEvent`          | Unknown event types accepted without validation against `EventTypes.All`   | 🟡 Medium |
| F   | `ProductService.cs:55`                          | Cache TTL (10 min) is hardcoded — no config key                            | 🟢 Low    |
| G   | No health endpoint                              | No `/health` or `/ready` probe for Docker orchestration                    | 🟢 Low    |
| H   | `ProductRequest.cs:7`                           | `TickerSymbol` is `string?` (nullable) but validator treats it as required | 🟢 Low    |
| I   | `TC-BULK-02` / `TC-BULK-03`                     | Validation behavior for bulk insert edge cases unverified                  | 🟢 Low    |
| J   | DLQ monitoring                                  | No alerting on dead-letter queue depth                                     | 🟢 Low    |
| K   | `MinuteSyncCurrentPrice`                        | Verify setting is actually wired into Hangfire cron in Worker `Program.cs` | 🟢 Low    |
