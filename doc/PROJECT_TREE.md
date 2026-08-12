# InventoryAlert — Project Tree & Directory Structure

---

## 📌 Project Overview

**InventoryAlert** (InventoryManagementSystem) is a real-time inventory management system built with clean architecture, domain-driven design (DDD), and an event-driven architecture. 

- **Backend Runtime**: .NET 10 / C# 12 (Minimal API + Hangfire / SQS Worker)
- **Frontend UI**: Next.js 15 (React 19, TypeScript, Tailwind CSS)
- **Documentation**: Docusaurus 3 Wiki + Markdown specs in `doc/`
- **Database & Storage**: PostgreSQL (EF Core 10), Redis (Cache), AWS DynamoDB (Read Models), AWS SQS/SNS (Messaging)

---

## 📁 Repository Directory Tree

```
InventoryManagementSystem/
├── .agents/                                ← AI agent briefing, rules, skills & scripts
│   ├── rules/                              ← Project coding & architectural rules
│   ├── skills/                             ← Specialized DDD, EF Core, Finnhub & test skills
│   ├── workflows/                          ← Step-by-step workflow commands (/init, /plan, etc.)
│   ├── scripts/core/                       ← BM25 code indexer & fast search tools
│   └── GEMINI.md                           ← AI agent cold-start briefing document
├── doc/                                    ← Engineering specs & architecture documentation
│   ├── plan/                               ← FSD feature specs & UI/API/DynamoDB designs
│   ├── archive/                            ← Completed feature specifications & history
│   ├── ENHANCEMENT_PLAN.md                 ← Planned system enhancements
│   ├── EVENT_DRIVEN_PLAN.md                ← AWS SQS/SNS event architecture plan
│   ├── README.md                           ← Documentation index
│   ├── ROADMAP.md                          ← Feature roadmap & backlog
│   └── WALKTHROUGH.md                      ← Getting started & end-to-end developer guide
├── scripts/                                ← Helper scripts & infrastructure automation
│   ├── init_aws_resources.sh               ← SQS/SNS queue & topic creation script
│   └── seed_data.py                        ← Local database seeder script
├── src/                                    ← Primary source code directory
│   ├── InventoryAlert.Api/                 ← ASP.NET Core Minimal Web API host
│   │   ├── Controllers/                    ← REST API Endpoint Controllers
│   │   ├── Extensions/                     ← Startup & Swagger middleware extensions
│   │   ├── Middleware/                     ← Exception & Auth middleware
│   │   ├── Services/                       ← Application business logic & API services
│   │   ├── ServiceExtensions/              ← Dependency Injection service registrations
│   │   ├── appsettings.json                ← Base API configuration
│   │   ├── appsettings.Development.json    ← Local dev settings (overrides)
│   │   └── Program.cs                      ← Web API entry point
│   ├── InventoryAlert.Domain/              ← Core Domain Layer (DDD Zero Dependencies)
│   │   ├── Common/                         ← Base Entity, ValueObject & DomainEvent interfaces
│   │   ├── Entities/                       ← Postgres SQL & DynamoDB Read Model entities
│   │   │   ├── Dynamodb/                   ← DynamoDB read model classes
│   │   │   └── Postgres/                   ← EF Core relational entities
│   │   ├── Enums/                          ← Domain enums (AlertCondition, NotificationStatus, etc.)
│   │   ├── Interfaces/                     ← Repository & Service interfaces
│   │   ├── ValueObjects/                   ← Immutable Domain Value Objects
│   │   └── Exceptions/                     ← Custom Domain Exceptions
│   ├── InventoryAlert.Infrastructure/      ← Infrastructure Layer (DB, Finnhub, AWS, Redis)
│   │   ├── DependencyInjection.cs          ← AddInfrastructure extension method
│   │   ├── Identity/                       ← Password hashing & JWT helpers
│   │   ├── ExternalServices/ Finnhub /     ← Finnhub REST client implementation
│   │   ├── Messaging/ SQS / SNS /          ← AWS S3/SQS/SNS publisher & poller setup
│   │   ├── Persistence/ Postgres /         ← EF Core AppDbContext & Configurations
│   │   │   ├── AppDbContext.cs             ← Primary EF Core DbContext
│   │   │   ├── Configurations/             ← Entity type configuration mappings
│   │   │   ├── Migrations/                 ← EF Core database migrations
│   │   │   ├── DatabaseSeeder.cs           ← SQL dev seed data
│   │   │   └── Repositories/               ← Postgres repository implementations
│   │   └── Persistence/ Dynamodb /         ← DynamoDB repository implementations
│   ├── InventoryAlert.Worker/              ← Background Processing Host (.NET Worker)
│   │   ├── Handlers/                       ← Integration Event Message Handlers
│   │   ├── IntegrationEvents/              ← Event definitions & message router
│   │   ├── ScheduledJobs/                  ← Hangfire cron jobs (SyncPrices, ProcessQueue)
│   │   └── Program.cs                      ← Worker host entry point
│   ├── SolutionFolder/                     ← Visual Studio solution configurations & build props
│   ├── test/                               ← Test Suites
│   │   ├── InventoryAlert.ArchitectureTests/ ← Clean Architecture & DDD dependency validation
│   │   ├── InventoryAlert.E2ETests/        ← End-to-End API HTTP integration tests
│   │   ├── InventoryAlert.IntegrationTests/← WireMock & WebApplicationFactory tests
│   │   ├── InventoryAlert.Sample/          ← Console sample runner / scratch project
│   │   └── InventoryAlert.UnitTests/       ← xUnit domain, app & infrastructure unit tests
│   └── ui/                                 ← Frontend Applications & User Interfaces
│       ├── InventoryAlert.UI/              ← Next.js 15 Web Application
│       │   ├── src/app/                    ← App Router pages & API routes
│       │   ├── src/components/             ← UI component library
│       │   ├── src/hooks/                  ← Custom React hooks
│       │   ├── src/lib/                    ← API clients & state management helpers
│       │   ├── Dockerfile                  ← Docker container spec for Next.js UI
│       │   └── package.json                ← Next.js dependencies & scripts
│       └── InventoryAlert.Wiki/            ← Docusaurus 3 Documentation Portal
│           ├── docs/                       ← Markdown documentation site pages
│           ├── docusaurus.config.ts        ← Docusaurus site configuration
│           └── package.json                ← Docusaurus dependencies
│   ├── Directory.Packages.props            ← Central Package Management (CPM) versions
│   ├── InventoryManagementSystem.sln       ← Master .NET Solution File
│   └── docker-compose.yml                  ← Docker Compose (Postgres, Redis, Moto AWS, Seq)
├── .editorconfig                           ← C# & editor formatting rules
├── .gitignore                              ← Git ignore patterns
├── README.md                               ← Root repository documentation
├── REFACTORING_SUGGESTIONS.md              ← Codebase optimization & refactoring notes
├── code-coverage.bat                       ← Local code coverage report script
└── skills-lock.json                        ← Lock file for agent skills
```

---

## 🗂️ Detailed Layer Breakdown

### 1. `src/InventoryAlert.Api/` (API Presentation Layer)
- **Role**: Entry point for HTTP REST requests.
- **Key Components**:
  - `Program.cs`: Minimal API setup, OpenAPI/Swagger configuration, middleware pipelines.
  - `Controllers/`: Exposes endpoints for Auth, Watchlists, Alert Rules, Portfolio, Trades, Stock Market Data, and Notifications.
  - `Services/`: High-level presentation services (e.g., [StockDataService](file:///C:/Users/sshuser/project/InventoryManagementSystem/src/InventoryAlert.Api/Services/StockDataService.cs), [PortfolioService](file:///C:/Users/sshuser/project/InventoryManagementSystem/src/InventoryAlert.Api/Services/PortfolioService.cs)).

### 2. `src/InventoryAlert.Domain/` (Domain Layer)
- **Role**: Contains core domain logic, business rules, entities, and repository interfaces. Has **zero** external dependencies.
- **Key Components**:
  - `Entities/Postgres/`: Domain entities (`User`, `StockListing`, `WatchlistItem`, `AlertRule`, `Trade`, `Notification`, `PriceHistory`, `StockMetric`).
  - `Entities/Dynamodb/`: DynamoDB read model entries (`MarketNewsDynamoEntry`, `CompanyNewsDynamoEntry`).
  - `Interfaces/`: Repository contracts (`IWatchlistRepository`, `IAlertRuleRepository`, `ITradeRepository`, `IStockListingRepository`).

### 3. `src/InventoryAlert.Infrastructure/` (Infrastructure Layer)
- **Role**: Implements domain interfaces and handles external infrastructure connections.
- **Key Components**:
  - `Persistence/Postgres/`: [AppDbContext](file:///C:/Users/sshuser/project/InventoryManagementSystem/src/InventoryAlert.Infrastructure/Persistence/Postgres/AppDbContext.cs), EF Core mapping configurations, Npgsql provider integration.
  - `Persistence/Dynamodb/`: AWS DynamoDB SDK operations and repository implementations.
  - `ExternalServices/Finnhub/`: Finnhub REST client integration for fetching stock quotes, earnings, financial metrics, and news.
  - `Messaging/`: AWS SQS & SNS event publishers, queue client helpers.

### 4. `src/InventoryAlert.Worker/` (Background Worker Layer)
- **Role**: Asynchronous job processor and queue listener running independently from the Web API.
- **Key Components**:
  - `ScheduledJobs/SyncPricesJob.cs`: Periodically polls Finnhub API for updated stock quotes and evaluates alert conditions.
  - `ScheduledJobs/ProcessQueueJob.cs`: Polls AWS SQS for integration events and dispatches them via `IntegrationMessageRouter`.
  - `Handlers/`: Handlers for executing background tasks and notification triggers.

### 5. `src/test/` (Test Layer)
- **Role**: Comprehensive testing suite maintaining system reliability and DDD layer discipline.
- **Key Components**:
  - `InventoryAlert.UnitTests/`: Fast xUnit tests covering domain rules, service logic, and handlers with Moq.
  - `InventoryAlert.IntegrationTests/`: Tests using EF Core InMemory and WireMock to verify API endpoints and DB persistence.
  - `InventoryAlert.E2ETests/`: End-to-end tests validating full HTTP workflows.
  - `InventoryAlert.ArchitectureTests/`: Enforces Clean Architecture dependency rules (e.g. Domain layer must not reference Infrastructure or Web).

### 6. `src/ui/` (Frontend & Documentation Layer)
- **Role**: Web client and internal documentation engine.
- **Key Components**:
  - `InventoryAlert.UI/`: Next.js 15 application providing user interfaces for stock tracking, portfolio position management, and alerts dashboard.
  - `InventoryAlert.Wiki/`: Docusaurus documentation portal covering system architecture, API specs, data models, and dev guides.

---

## ⚡ Quick Entry Points

- **API Entry Point**: [Program.cs (Api)](file:///C:/Users/sshuser/project/InventoryManagementSystem/src/InventoryAlert.Api/Program.cs)
- **Worker Entry Point**: [Program.cs (Worker)](file:///C:/Users/sshuser/project/InventoryManagementSystem/src/InventoryAlert.Worker/Program.cs)
- **EF Core DbContext**: [AppDbContext.cs](file:///C:/Users/sshuser/project/InventoryManagementSystem/src/InventoryAlert.Infrastructure/Persistence/Postgres/AppDbContext.cs)
- **Master Docker Setup**: [docker-compose.yml](file:///C:/Users/sshuser/project/InventoryManagementSystem/src/docker-compose.yml)
- **Master Solution**: [InventoryManagementSystem.sln](file:///C:/Users/sshuser/project/InventoryManagementSystem/src/InventoryManagementSystem.sln)
