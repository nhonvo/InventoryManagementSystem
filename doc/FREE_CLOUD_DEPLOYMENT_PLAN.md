# 🚀 Free Cloud Deployment & Hosting Master Plan ($0/Month Demo Blueprint)

**Target System**: `InventoryManagementSystem` (InventoryAlert)  
**Target Cost**: **$0.00 / month** (100% Free Tier Cloud Stack)  
**Platforms**: Supabase, Render, Upstash, Vercel, AWS Free Tier, GitHub Pages  
**Date**: 2026-08-12  

---

## 🏛️ 1. Free Cloud Architecture Topology

Deploy each layer of the solution to the best-in-class free cloud provider without paying any monthly subscription fees:

```mermaid
graph TD
    Client[Browser / User] --> Vercel[Vercel - Free Tier<br/>Next.js 15 UI]
    Client --> GitHubPages[GitHub Pages - Free<br/>Docusaurus Wiki Portal]
    Vercel -- HTTPS / REST / WSS --> RenderAPI[Render - Free Web Service<br/>.NET 10 Minimal API + Worker]
    
    RenderAPI -- Connection Pooler --> Supabase[Supabase - Free Postgres DB<br/>500MB SQL Storage]
    RenderAPI -- TLS Redis --> Upstash[Upstash - Free Redis Cache<br/>10,000 requests/day]
    RenderAPI -- AWS SDK --> AWS[AWS Free Tier<br/>SQS Queue, SNS Topic, DynamoDB]
```

---

## 📊 2. Platform Selection & Resource Allocation Matrix

| Layer / Component | Technology | Recommended Free Cloud Provider | Free Tier Limits | Why Selected? |
| :--- | :--- | :--- | :--- | :--- |
| **Relational Database** | PostgreSQL 16 (EF Core 10) | **Supabase** (or Neon.tech) | 500 MB DB, 50k monthly active users, built-in PgBouncer pooler | Native PostgreSQL with SSL, zero cost, instant connection pooler |
| **Distributed Cache** | Redis 7 | **Upstash Redis** | 10,000 commands/day, 256 MB storage | Serverless Redis, zero idle cost, TLS/SSL connection string |
| **Backend API Host** | .NET 10 ASP.NET Minimal API | **Render** (Web Service) | 512 MB RAM, 0.1 CPU, Docker container support | Free web service, supports custom Dockerfiles, SSL domain included |
| **Background Worker** | .NET 10 Hangfire / Poller | Combined in **Render Container** | Included in Render API instance | Running Worker + API in single Docker container saves free tier slots |
| **Frontend Web UI** | Next.js 15 (React 19) | **Vercel** (Hobby) | 100 GB bandwidth/month, unlimited builds | Official Next.js host, zero cold-starts, automatic CI/CD deployment |
| **Documentation Portal** | Docusaurus 3 Wiki | **GitHub Pages** | Unlimited static hosting | Native integration with GitHub repository via GitHub Actions |
| **Event Messaging & NoSQL** | AWS SQS, SNS, DynamoDB | **AWS Free Tier** (Real AWS) | SQS: 1M req/mo<br/>SNS: 1M pub/mo<br/>DynamoDB: 25GB, 25 WCU/RCU | Permanent AWS free tier rules, 100% production event behavior |

---

## 📋 3. Step-by-Step Deployment Execution Guide

### Step 1: Provision Free PostgreSQL Database on Supabase

1. Sign up at [supabase.com](https://supabase.com) (Free Tier).
2. Create a new project named `inventory-alert-demo` and set a strong database password.
3. In **Project Settings -> Database**, copy the **Transaction Pooler Connection String**:
   ```
   postgresql://postgres.[PROJECT-REF]:[PASSWORD]@aws-0-[REGION].pooler.supabase.com:6543/postgres?sslmode=Require
   ```
4. Run EF Core database migrations from your local terminal targeting Supabase:
   ```bash
   dotnet ef database update --project src/InventoryAlert.Infrastructure --startup-project src/InventoryAlert.Api --connection "Host=aws-0-[REGION].pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.[PROJECT-REF];Password=[PASSWORD];SSL Mode=Require;Trust Server Certificate=true"
   ```

---

### Step 2: Provision Free Serverless Redis on Upstash

1. Sign up at [upstash.com](https://upstash.com) (Free Tier).
2. Create a new Redis Database named `inventory-alert-redis` in your preferred region.
3. Under **Database Details**, copy the **Redis Connection String**:
   ```
   rediss://default:[PASSWORD]@[ENDPOINT].upstash.io:6379
   ```

---

### Step 3: Provision Permanent AWS Free Tier Resources

1. Log into your AWS Console (or create an AWS Free Tier account).
2. **Create SQS Queue**:
   - Name: `inventory-event-queue`
   - Dead Letter Queue: `inventory-event-dlq`
3. **Create SNS Topic**:
   - Name: `inventory-events`
   - Subscribe SQS queue to this SNS topic.
4. **Create DynamoDB Tables**:
   - Table 1: `inventoryalert-company-news` (PK: `PK` [String], SK: `SK` [String])
   - Table 2: `inventoryalert-market-news` (PK: `PK` [String], SK: `SK` [String])
5. **Create IAM User** (`inventory-alert-cloud-user`) with programmatic access and attach policies for SQS, SNS, and DynamoDB. Save `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY`.

---

### Step 4: Deploy .NET 10 API & Worker on Render

1. Sign up at [render.com](https://render.com).
2. Push your repository to GitHub.
3. Create a **New Web Service** on Render and select your GitHub repository.
4. Select **Docker** environment (Render automatically reads `src/InventoryAlert.Api/Dockerfile` or root Dockerfile).
5. Add the following **Environment Variables** in Render Dashboard:

| Key | Example Value | Description |
| :--- | :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | `Production` | ASP.NET Core environment mode |
| `Database__DefaultConnection` | `Host=...pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.xxx;Password=xxx;SSL Mode=Require;Trust Server Certificate=true` | Supabase Database connection string |
| `Redis__ConnectionString` | `[ENDPOINT].upstash.io:6379,password=[PASSWORD],ssl=True,abortConnect=False` | Upstash Redis connection string |
| `Finnhub__ApiKey` | `c123456789...` | Your free Finnhub.io API Key |
| `Jwt__Key` | `Super_Secret_Production_32_Char_Key_Here_123!` | 256-bit secret key for JWT validation |
| `Jwt__Issuer` | `InventoryAlert.Api` | Token issuer |
| `Jwt__Audience` | `InventoryAlert.Web` | Token audience |
| `AWS_REGION` | `us-east-1` | AWS Cloud Region |
| `AWS_ACCESS_KEY_ID` | `AKIA...` | AWS IAM Access Key |
| `AWS_SECRET_ACCESS_KEY` | `wJalrXUtnFEMI...` | AWS IAM Secret Key |
| `Aws__SqsQueueUrl` | `https://sqs.us-east-1.amazonaws.com/123456789/inventory-event-queue` | AWS SQS Queue URL |
| `Aws__SnsTopicArn` | `arn:aws:sns:us-east-1:123456789:inventory-events` | AWS SNS Topic ARN |

6. Click **Deploy Web Service**. Render builds the .NET 10 Docker image and provides a public HTTPS endpoint:
   `https://inventory-alert-api.onrender.com`

---

### Step 5: Deploy Next.js 15 Web UI on Vercel

1. Sign up at [vercel.com](https://vercel.com).
2. Click **Add New Project** and import your GitHub repository.
3. Set **Root Directory** to `src/ui/InventoryAlert.UI`.
4. Framework Preset: **Next.js**.
5. Add **Environment Variables** in Vercel Dashboard:

| Environment Variable | Value | Purpose |
| :--- | :--- | :--- |
| `NEXT_PUBLIC_API_URL` | `https://inventory-alert-api.onrender.com` | Render Web API endpoint |
| `NEXT_PUBLIC_SIGNALR_URL` | `https://inventory-alert-api.onrender.com/hubs/notifications` | Real-time SignalR WebSocket hub |

6. Click **Deploy**. Vercel builds the Next.js app and provides a fast HTTPS URL:
   `https://inventory-alert-ui.vercel.app`

---

### Step 6: Deploy Docusaurus Wiki Portal on GitHub Pages

1. In your GitHub Repository, create `.github/workflows/deploy-wiki.yml`:

```yaml
name: Deploy Documentation Wiki to GitHub Pages

on:
  push:
    branches: [main]
    paths: ['src/ui/InventoryAlert.Wiki/**']

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 20
      - name: Install dependencies & Build
        run: |
          cd src/ui/InventoryAlert.Wiki
          npm ci
          npm run build
      - name: Deploy to GitHub Pages
        uses: peaceiris/actions-gh-pages@v3
        with:
          github_token: ${{ secrets.GITHUB_TOKEN }}
          publish_dir: ./src/ui/InventoryAlert.Wiki/build
```

2. Enable **GitHub Pages** under **Settings -> Pages** set to `gh-pages` branch.
3. Access your live documentation at `https://[YOUR-USERNAME].github.io/InventoryManagementSystem/`

---

## ⚡ 4. Free Tier Maintenance & Sleep/Wake Warmup Notes

> [!NOTE]
> **Render Web Service Free Tier Sleep Behavior**:
> Render free web services go to sleep after 15 minutes of inactivity. When a request hits `https://inventory-alert-api.onrender.com`, it takes ~30-45 seconds to spin up (cold start).
> 
> **Solution for Demos**:
> - Use a free uptime monitoring service like **UptimeRobot** (free tier: 50 monitors, 5-minute intervals) to ping `https://inventory-alert-api.onrender.com/healthz` every 10 minutes to keep the API awake 24/7 at $0 cost!

---

## 🎯 5. Verification & Pre-Flight Checklist

Before presenting your demo link:

- [ ] **Database Connection**: Verify EF Core migrations applied cleanly on Supabase Postgres.
- [ ] **Redis Connection**: Test stock quote endpoint `GET /api/v1/stocks/AAPL` to verify Upstash Redis caching.
- [ ] **JWT Auth**: Test `/api/v1/auth/login` to confirm token issuance.
- [ ] **SignalR Notifications**: Open Next.js UI on Vercel and verify real-time WebSocket connection to Render API.
- [ ] **API Keep-Alive**: Ping `/healthz` via UptimeRobot to avoid cold-start delays.
