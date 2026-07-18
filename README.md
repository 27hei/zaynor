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

## Status

This is the initial scaffold (Build Roadmap Phase 1, spec Section 18): solution structure, project
references, and a health-check endpoint proving frontend ↔ backend connectivity. No domain
entities, database, or aggregation logic yet — those come next, in sequence, per Section 23.
