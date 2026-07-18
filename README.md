# ZAYNOR — Smart Shopping Decisions

Zaynor is a real-time price-comparison and shopping-decision assistant. A user searches for a
product; Zaynor aggregates offers across stores, identifies the lowest price, and gives a clear
recommendation — before the user ever leaves for a marketplace.

Full product context, requirements, architecture, and roadmap live in
[`docs/PROJECT_SPECIFICATION.md`](docs/PROJECT_SPECIFICATION.md). Read that first — this README
only covers running the code.

## Repository Layout

```
ZAYNOR/
├── backend/                   ASP.NET Core solution (clean architecture)
│   ├── Zaynor.sln
│   ├── src/
│   │   ├── Zaynor.Domain/         Entities, core models — no dependencies
│   │   ├── Zaynor.Application/    Business logic, service interfaces — depends on Domain
│   │   ├── Zaynor.Infrastructure/ EF Core, external data sources — depends on Application, Domain
│   │   └── Zaynor.Api/            ASP.NET Core Web API (Presentation) — depends on all three
│   └── tests/
│       └── Zaynor.Application.Tests/
├── frontend/                  React + TypeScript + Vite client
└── docs/
    └── PROJECT_SPECIFICATION.md
```

This mirrors the layering in Section 13 of the spec: Presentation → Application → Domain, with
Infrastructure implementing interfaces the Application layer defines (dependency inversion), so
the aggregation engine and business rules stay decoupled from EF Core, databases, and external
feeds.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 18+ and npm

## Running the Backend

```
cd backend
dotnet restore
dotnet run --project src/Zaynor.Api
```

## Running the Frontend

```
cd frontend
npm install
npm run dev
```

## Core Search Flow

The MVP heart (spec Section 8) is working end-to-end against a **mock data source**:

- `GET /api/search?q=<product>` fans the query out across every registered `IProductDataSource`,
  merges the offers, sorts them cheapest-first, flags the lowest, and returns a recommendation
  with the potential saving.
- The frontend renders the results with a recommendation banner and "Go to store" affiliate links.

The data source is currently `MockProductDataSource` (fabricated but stable prices) so the flow is
demonstrable before any real feed is wired. Adding a real source (affiliate feed / store API) means
implementing `IProductDataSource` and registering it in `Zaynor.Infrastructure` — the aggregation
engine does not change.

Key types:

- `Zaynor.Application/Aggregation/IAggregationService.cs` — the engine contract
- `Zaynor.Application/Aggregation/AggregationService.cs` — merge / rank / recommend
- `Zaynor.Application/Aggregation/IProductDataSource.cs` — the source plug-in point
- `Zaynor.Infrastructure/DataSources/MockProductDataSource.cs` — the temporary source

## Accounts & Persistence

User accounts are live (spec FR9), backed by **EF Core + SQLite** (a local `zaynor.db` file — zero
setup; swappable to PostgreSQL/SQL Server via config for production).

- `POST /api/auth/register`, `POST /api/auth/login` — passwords hashed with **BCrypt**, sessions
  issued as **JWT** bearer tokens.
- `GET /api/auth/me` — the current user (requires a valid token).

The schema is created on startup via `EnsureCreated()` for local dev; EF Core migrations replace
this as persistence matures.

## Pages & Bilingual UI

The frontend is a multi-page app (React Router) with full **Arabic / English** support and RTL
(spec NFR5), toggled from the header and persisted across visits:

- **Home** — hero, search, recommendation, results, feature highlights.
- **Categories**, **How It Works**, **About**, **Privacy Policy** — content pages.
- **Log in / Sign up** — wired to the real auth API.
- **My Account** — a protected route (redirects to login when signed out).

Arabic uses the Cairo typeface; English uses Inter/Sora. Language state lives in
`src/i18n/`, auth state in `src/auth/`.

## Caching & Price History

Per spec Section 13, the public search engine is the core `AggregationService` decorated by
`CachedAggregationService` (Infrastructure):

- **Short-lived cache** (5 min, in-memory): repeat searches return instantly; keys use the FR3
  normalized query so spelling variants share one entry. Swappable to Redis at scale.
- **Price-history accumulation**: every *live* search records the observed prices into
  `PriceHistory` (finding-or-creating the `Product` and `Store` rows on first sight, throttled to
  one point per product+store per hour). This is the data that predictive analytics (FR12,
  "buy now or wait?") will need months of — recording started with the first search on purpose.
- Recording is fail-soft: a history failure can never break the search that produced it (NFR4).

## Deployment

The stack is containerized and configurable per environment:

- `docker-compose.yml` runs both services: copy `.env.deploy.example` to `.env`, set `JWT_KEY`
  (required — the API refuses to start without it), then `docker compose up --build`.
- **Frontend** bakes the API origin at build time via `VITE_API_URL` (see `frontend/.env.example`).
- **Backend** reads everything from configuration/env: `ConnectionStrings__Default`, `Jwt__*`,
  `Cors__Origins__N` (the browser origins allowed to call the API), `AlertMonitor__IntervalMinutes`.
- The database schema is applied via **EF Core migrations** on startup (`Database.Migrate()`);
  new schema changes ship as migrations (`dotnet dotnet-ef migrations add <Name>
  --project src/Zaynor.Infrastructure --startup-project src/Zaynor.Api` from `backend/`).
- SQLite persists on a named volume in compose; swap `ConnectionStrings__Default` to
  PostgreSQL when scale demands (spec Section 14).

For affiliate-network applications (spec Section 20) the site needs a public URL — any
container host (Railway, Fly.io, Azure) can run the compose setup as-is.

## Status

- **Done:** clean-architecture scaffold, domain entities (Section 15), the search → aggregate →
  rank → recommend flow against a mock source, SQLite persistence, JWT auth
  (register/login/account), saved products & price-drop alert subscriptions (FR8/FR9) end-to-end,
  search-result caching + price-history accumulation (Section 13), a full multi-page bilingual
  (ar/en + RTL) UI with the real brand assets, and the About / How It Works / Privacy pages needed
  ahead of affiliate-network applications. 19 unit tests.
- **Next (in sequence, per Section 23):** the first real data source (requires ArabClicks /
  Amazon Associates accounts — a founder action), EF Core migrations replacing `EnsureCreated`,
  then background jobs to evaluate alert conditions against accumulating history.
