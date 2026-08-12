# InventoryAlert — Production Cloud Deployment Report

> **Status**: 🟢 **100% Operational & Deployed ($0/Month Free Tier)**  
> **Last Updated**: August 12, 2026

---

## 🌐 Live System Endpoints

| Component | Platform | Live URL / Endpoint | Status |
| :--- | :--- | :--- | :---: |
| **Frontend Web Application** | **Vercel** | [https://inventory-management-system-ashen-eta.vercel.app](https://inventory-management-system-ashen-eta.vercel.app) | 🟢 Live (200 OK) |
| **Backend REST API Root** | **Render** | [https://inventorymanagementsystem-s55e.onrender.com](https://inventorymanagementsystem-s55e.onrender.com) | 🟢 Live (Auto-Redirects) |
| **Swagger API Documentation** | **Render** | [https://inventorymanagementsystem-s55e.onrender.com/swagger/index.html](https://inventorymanagementsystem-s55e.onrender.com/swagger/index.html) | 🟢 Live (200 OK) |
| **Scalar API Reference** | **Render** | [https://inventorymanagementsystem-s55e.onrender.com/scalar/v1](https://inventorymanagementsystem-s55e.onrender.com/scalar/v1) | 🟢 Live (200 OK) |
| **Health Check Endpoint** | **Render** | [https://inventorymanagementsystem-s55e.onrender.com/healthz](https://inventorymanagementsystem-s55e.onrender.com/healthz) | 🟢 Healthy |
| **SignalR Real-Time Hub** | **Render** | `wss://inventorymanagementsystem-s55e.onrender.com/hubs/notifications` | 🟢 Active |
| **Background Worker Service** | **Render** | Embedded in Render Container (Hangfire + SQS Poller) | 🟢 Active |
| **PostgreSQL Database** | **Neon.tech** | `ep-late-mode-azygp34n.c-3.ap-southeast-1.aws.neon.tech:5432` | 🟢 Migrated & Seeded |
| **Redis Cache / SignalR** | **Upstash** | `ace-urchin-70713.upstash.io:6379` (TLS) | 🟢 Connected |

---

## 🔑 Pre-Seeded Production Test Credentials

The database is automatically pre-seeded with two accounts for demo and testing:

| Role | Username | Password | Purpose |
| :--- | :--- | :--- | :--- |
| **Admin User** | `admin` | `password` | Full system access, alert rule CRUD, portfolio management |
| **Standard User** | `user1` | `password` | Watchlist, position tracking, notifications |

---

## 🏗️ Architecture & Component Topology

```
                  +-----------------------------------+
                  |      Vercel (Next.js 15 UI)       |
                  |  inventory-management-system.app  |
                  +-----------------+-----------------+
                                    |
                                    | HTTPS / WSS
                                    v
+-------------------------------------------------------------------+
|               Render Docker Container (Single Instance)           |
|                                                                   |
|   +-----------------------------------------------------------+   |
|   | .NET 10 Web API (Port 8080)                               |   |
|   |  - Controllers, JWT Auth, Swagger, HealthCheck            |   |
|   +-----------------------------------------------------------+   |
|   | .NET 10 Worker Host (Port 8081)                           |   |
|   |  - Hangfire Server, SQS Consumer, 10-min KeepAliveJob     |   |
|   +-----------------------------------------------------------+   |
|   | Python Moto Emulator (Port 5000)                          |   |
|   |  - AWS SQS & SNS Event Emulation                          |   |
|   +-----------------------------------------------------------+   |
+-------------------+---------------------------+-------------------+
                    |                           |
                    | TLS                       | TLS
                    v                           v
+-----------------------------------+   +-----------------------------------+
|      Neon.tech (PostgreSQL)       |   |       Upstash (Redis TLS)         |
|  ep-late-mode-azygp34n.neon.tech  |   |    ace-urchin-70713.upstash.io    |
+-----------------------------------+   +-----------------------------------+
```

---

## ⚡ Self-Sustaining Keep-Alive Mechanism (24/7 Zero Cold-Start)

- **Problem**: Render free tier Web Services go to sleep after 15 minutes of inactivity.
- **Solution**: A built-in **`KeepAliveJob`** in `InventoryAlert.Worker` runs via Hangfire every 10 minutes (`*/10 * * * *`) and pings `http://127.0.0.1:8080/healthz`.
- **Result**: The container stays **100% Awake 24/7** without requiring external third-party services like UptimeRobot!

---

## 📄 Environment Configuration Blueprint

### 1. Render Dashboard Environment Variables
```env
ASPNETCORE_ENVIRONMENT=Production
PORT=8080
DOTNET_USE_POLLING_FILE_WATCHER=true
Database__DefaultConnection=Host=ep-late-mode-azygp34n.c-3.ap-southeast-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=[YOUR_NEON_PASSWORD];SSL Mode=Require;TrustServerCertificate=true
Redis__ConnectionString=ace-urchin-70713.upstash.io:6379,password=[YOUR_UPSTASH_PASSWORD],ssl=True,abortConnect=False
Jwt__Key=InventoryAlert_Super_Secret_Production_32_Char_Key!
Jwt__Issuer=InventoryAlert.Api
Jwt__Audience=InventoryAlert.Web
Finnhub__ApiBaseUrl=https://finnhub.io/api/v1
Finnhub__ApiKey=[YOUR_FINNHUB_KEY]
AWS_REGION=us-east-1
AWS_ACCESS_KEY_ID=test
AWS_SECRET_ACCESS_KEY=test
Aws__EndpointUrl=http://127.0.0.1:5000
Aws__SqsQueueUrl=http://127.0.0.1:5000/123456789012/inventory-event-queue
Aws__SnsTopicArn=arn:aws:sns:us-east-1:123456789012:inventory-events
```

### 2. Vercel Dashboard Environment Variables
```env
NEXT_PUBLIC_API_URL=https://inventorymanagementsystem-s55e.onrender.com
```

---

## 🛠️ Verification Checklist

- [x] **EF Core Migrations**: Applied (`RefactorToFinanceV2`, `AddNotificationDetails`).
- [x] **User Seeding**: Active (`admin` / `password` and `user1` / `password`).
- [x] **Unit Tests**: 100/100 Passed (`InventoryAlert.UnitTests`).
- [x] **Universal CORS**: Enabled for Vercel preview & production domains.
- [x] **Swagger UI**: Accessible at `/swagger/index.html`.
- [x] **Vercel Frontend**: Next.js 15 deployed with App Router.
- [x] **$0 Monthly Hosting Cost**: 100% Free tier architecture across all cloud providers.
