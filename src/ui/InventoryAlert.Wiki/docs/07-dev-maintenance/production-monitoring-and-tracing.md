---
title: Production Monitoring & Bug Tracing Guide
sidebar_position: 10
description: Complete operational reference for monitoring production health, tracing request correlation IDs, viewing logs, and diagnosing production bugs.
---

# 🔍 Production Monitoring & Bug Tracing Guide

This guide details how to monitor system health, trace logs across microservice components, inspect production data, and diagnose bugs in the live InventoryAlert production environment on Render.

---

## 🌐 1. Live Production Endpoints

| Service / Interface | Production Link | Purpose |
| :--- | :--- | :--- |
| **API Host** | `https://inventorymanagementsystem-s55e.onrender.com` | Base production API host |
| **Swagger UI** | `https://inventorymanagementsystem-s55e.onrender.com/swagger` | Interactive API testing & endpoint documentation |
| **Scalar Docs** | `https://inventorymanagementsystem-s55e.onrender.com/scalar/v1` | Searchable OpenAPI specification |
| **Health Check** | `https://inventorymanagementsystem-s55e.onrender.com/healthz` | System health probe (returns `Healthy` HTTP 200) |
| **AWS Moto Proxy** | `https://inventorymanagementsystem-s55e.onrender.com/aws` | Public endpoint proxying requests to internal Moto SQS/DynamoDB |

---

## 📊 2. System Health Monitoring

### Health Check Endpoint (`/healthz`)
- **URL**: `https://inventorymanagementsystem-s55e.onrender.com/healthz`
- **Method**: `GET`
- **Response**: `200 OK` with JSON payload indicating system status:
  ```json
  {
    "status": "Healthy",
    "totalDuration": "00:00:00.0123000",
    "entries": {
      "postgresql": { "status": "Healthy" },
      "redis": { "status": "Healthy" }
    }
  }
  ```
- **Automated Uptime Monitoring**: A background `KeepAliveJob` periodically pings `/healthz` to prevent free-tier container sleep.

---

## 🧵 3. Log Tracing & Correlation IDs

### Distributed Tracing Header
Every request processed by the API host is stamped with a unique Correlation ID:
- **HTTP Header**: `X-Correlation-ID`
- **Log Property**: `CorrelationId`

If a client sends an `X-Correlation-ID` header, the system retains it. Otherwise, middleware generates a new `Guid` for the request lifecycle.

### Log Format & Serilog Setup
In production, stdout emits structured JSON logs formatted with Serilog:
```json
{
  "@t": "2026-08-13T13:00:00.1234567Z",
  "@l": "Error",
  "@mt": "Unhandled exception occurred while processing request {RequestPath}",
  "CorrelationId": "c8a1e2f3-4b5c-6d7e-8f90-1a2b3c4d5e6f",
  "SourceContext": "InventoryAlert.Api.Middleware.GlobalExceptionHandler",
  "RequestPath": "/api/v1/portfolio/positions",
  "Exception": "System.InvalidOperationException: ..."
}
```

### Request & Response Body Logging
Production environment has `Api__EnableBodyLogging=true` enabled:
- Request payloads (JSON bodies) are recorded under log property `RequestBody` for HTTP POST/PUT operations.
- Allows immediate verification of exact payload submitted during error investigation.

---

## 🐛 4. Step-by-Step Production Bug Diagnosis Runbook

### Step 1: Obtain Correlation ID or Timestamp
When a user reports an issue or an HTTP 500 error occurs:
1. Check the response body returned to the client (RFC 7807 `ProblemDetails` contains `traceId` / `correlationId`).
2. If unknown, note the exact timestamp (UTC) and endpoint path (`e.g., POST /api/v1/alert-rules`).

### Step 2: Query Container Logs on Render
1. Open the Render Dashboard for `inventorymanagementsystem-s55e`.
2. Filter logs by log level: `Error` or `Warning`.
3. Search for the specific `CorrelationId` to view the full request-response lifecycle across API handlers and background workers.

### Step 3: Local Log Viewer with Seq (Docker)
To analyze production logs locally in Seq (`http://localhost:5341`):
1. In Seq search bar, filter by Correlation ID:
   ```sql
   CorrelationId = 'c8a1e2f3-4b5c-6d7e-8f90-1a2b3c4d5e6f'
   ```
2. Filter for uncaught errors:
   ```sql
   @Level = 'Error' or @Level = 'Fatal'
   ```
3. Filter by component context:
   ```sql
   SourceContext like '%Worker%' or SourceContext like '%StockDataService%'
   ```

### Step 4: DynamoDB Data Tracing via `dynamodb-admin`
To inspect news read models and DynamoDB items in production:
1. Execute `dynamodb-admin` locally pointing to the Render Moto proxy:
   ```powershell
   DYNAMO_ENDPOINT=https://inventorymanagementsystem-s55e.onrender.com/aws npx dynamodb-admin
   ```
2. Open `http://localhost:8001` to view live tables:
   - `inventoryalert-market-news`
   - `inventoryalert-company-news`

---

## ⚙️ 5. Monitoring Background Workers & Hangfire

- **Worker Process**: Managed by Supervisord alongside API host inside the container (`InventoryAlert.Worker`).
- **Hangfire Job Execution**:
  - `SyncPricesJob`: Syncs Finnhub stock quotes every 30 minutes.
  - `ProcessQueueJob`: Continuous SQS polling loop for alert evaluations.
  - `SyncCompanyNewsJob` & `SyncMarketNewsJob`: Background fetchers for DynamoDB news entries.
- **Failures & Retries**: Hangfire automatically retries failed jobs up to 10 times with exponential backoff. Failed job details are logged under `Hangfire.AutomaticRetryAttribute`.

---

## 🛠️ 6. Quick Troubleshooting Matrix

| Symptom | Probable Cause | Action |
| :--- | :--- | :--- |
| **HTTP 500 on API request** | Unhandled exception in application logic | Search Render logs for `CorrelationId` & review stack trace |
| **HTTP 401 Unauthorized** | Missing or expired JWT Token | Re-authenticate via `POST /api/v1/auth/login` |
| **Prices not updating** | Finnhub rate limit or worker error | Check worker logs for `Finnhub:ApiKey` limits or `SyncPricesJob` status |
| **DynamoDB empty/unreachable** | Proxy endpoint mismatch | Verify `DYNAMO_ENDPOINT=https://inventorymanagementsystem-s55e.onrender.com/aws` |
| **Neon PostgreSQL connection drop** | SSL mode requirement or connection pool exhaustion | Confirm `sslmode=require` in connection string |
