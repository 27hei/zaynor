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

## Status

- **Done:** clean-architecture scaffold, domain entities (Section 15), the search → aggregate →
  rank → recommend flow against a mock source, unit tests, SQLite persistence, JWT auth
  (register/login/account), a full multi-page bilingual (ar/en + RTL) UI, and the About / How It
  Works / Privacy pages needed ahead of affiliate-network applications.
- **Next (in sequence, per Section 23):** the first real data source (replacing the mock), EF Core
  migrations, and saved-products / price-drop alerts (the account features currently marked
  "coming soon").
