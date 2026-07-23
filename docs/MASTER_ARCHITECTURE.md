# ZAYNOR — Master Architecture Document

**Analysis-only. No code was modified to produce this document.**
Companion to `docs/PROJECT_SPECIFICATION.md` (the founding product spec — vision, phases, monetization). This document picks up where that one stops being a technical spec and starts being a product vision: it is the *engineering* plan for scaling the already-shipped, already-live implementation (zaynor.onrender.com) from a solo-founder MVP into a platform that can carry an AI shopping assistant, a real search engine, dozens of store connectors, and a mobile app.

Every claim below cites an exact file and, where useful, a line number. Nothing here is generic "best practice" filler — if a recommendation doesn't reference something concretely observed in this repository, it isn't in this document.

**A note on reconciliation.** A separate "ZAYNOR Engineering Bible" document was proposed alongside this one, specifying a from-scratch rebuild on Node.js/Express/TypeScript with a multi-agent AI core, message bus, and vector-database preparation, executed autonomously over 30 days. The founder confirmed the intent explicitly: **keep the existing ASP.NET Core + React stack — it is live, working, and earning real affiliate revenue today — and adopt that document's genuinely good ideas incrementally, inside the architecture already described below, instead of rewriting.** Several of its ideas independently converged with recommendations already in this document (its "Store Connector SDK" concept and this document's §16.2 `ExternalProductDataSourceBase` are the same idea, arrived at separately — a good sign both are right) and have been folded in below (§16.2, §20, §29). Its ideas that only make sense at a scale or team size Zaynor doesn't have yet (a message bus, a vector database, a 24/7 multi-agent runtime) are noted as explicit future triggers, not present-day requirements, consistent with this document's Phase-gated roadmap (§30) and the founding spec's own sequencing principle.

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Current Architecture Review](#2-current-architecture-review)
3. [Current Folder Structure Analysis](#3-current-folder-structure-analysis)
4. [Backend Analysis](#4-backend-analysis)
5. [Frontend Analysis](#5-frontend-analysis)
6. [Database Analysis](#6-database-analysis)
7. [API Analysis](#7-api-analysis)
8. [AI Readiness](#8-ai-readiness)
9. [Search Engine Readiness](#9-search-engine-readiness)
10. [Security Review](#10-security-review)
11. [Performance Review](#11-performance-review)
12. [Scalability Review](#12-scalability-review)
13. [Code Quality Review](#13-code-quality-review)
14. [Missing Components](#14-missing-components)
15. [Recommended Folder Structure](#15-recommended-folder-structure)
16. [Recommended Backend Architecture](#16-recommended-backend-architecture)
17. [Recommended Frontend Architecture](#17-recommended-frontend-architecture)
18. [Recommended Database Architecture](#18-recommended-database-architecture)
19. [Recommended API Architecture](#19-recommended-api-architecture)
20. [Recommended AI Architecture](#20-recommended-ai-architecture)
21. [Recommended Search Architecture](#21-recommended-search-architecture)
22. [Deployment Architecture](#22-deployment-architecture)
23. [CI/CD Architecture](#23-cicd-architecture)
24. [Monitoring Architecture](#24-monitoring-architecture)
25. [Logging Architecture](#25-logging-architecture)
26. [Testing Strategy](#26-testing-strategy)
27. [Caching Strategy](#27-caching-strategy)
28. [Future Mobile Strategy](#28-future-mobile-strategy)
29. [Future Microservices Strategy](#29-future-microservices-strategy)
30. [Enterprise Roadmap](#30-enterprise-roadmap)

---

## 1. Executive Summary

Zaynor today is a **live, working, revenue-instrumented product** — not a prototype. That is the single most important fact this review has to reconcile: the codebase is small (132 backend `.cs` files, ~100 frontend `.ts`/`.tsx` files, one 4,218-line CSS file), built by essentially one contributor working with AI assistance over roughly a week of wall-clock time, and it already has: JWT auth, an aggregation engine fanning out to five live external product-data sources in parallel, an affiliate-monetization path (Amazon Associates tag + Noon partner tracking link) verified against real production traffic, a reviews system, a support-ticket system, an admin role, price-history recording, price-drop alerts, bilingual (Arabic-first RTL / English) UI with dark mode, and a CI/CD pipeline that builds a Docker image and redeploys on every push to `main`.

That combination — genuine production traffic and revenue mechanics, built at solo-founder speed — is both the asset and the risk. The architecture is **disciplined in the small** (Clean Architecture layering in the backend is real, not cargo-culted: `Zaynor.Domain` has zero package references, `Zaynor.Application` depends only on `Zaynor.Domain`, and the "dormant until configured" pattern is applied consistently across five independent external integrations) and **undifferentiated in the large** (one 4,218-line `App.css` file, no path aliases, no state-management library, no runtime API-contract validation, no CI test gate, a Postgres migration path that has apparently never been exercised against a real Postgres instance). Both of the independent, from-scratch codebase audits performed for this document reached the same headline conclusion from opposite ends of the stack: **the patterns that exist are good and consistent; the problem is that nothing except developer memory enforces them**, and every item on the founder's stated roadmap (AI assistant, more store connectors, admin dashboard growth, mobile app) is exactly the kind of change that multiplies whatever isn't enforced.

This document's core thesis: **do not rewrite Zaynor.** Nothing found here justifies a rewrite — the two biggest real bugs uncovered during this review (`Product.NormalizedKey` has no unique constraint, and the Postgres migration path is unverified) are each fixable in isolation, in place, without touching working revenue-generating code. The recommended path is targeted, sequenced hardening of the existing structure — introduce the folders/abstractions/tests that are currently missing, in the order the roadmap actually needs them — not a rip-and-replace. Phase 1 of the roadmap in §30 is explicitly "stop the bleeding, change nothing user-visible," because the project's own founding principle (`docs/PROJECT_SPECIFICATION.md` §11, "The Sequential Build Approach") already argues against big-bang rewrites, and that argument applies as much to architecture work as it does to features.

---

## 2. Current Architecture Review

### 2.1 What actually exists today

```
Browser (React SPA, same-origin /api calls)
        │
        ▼
ASP.NET Core 8 Web API (single process, single Docker image)
  ├─ 14 Controllers (thin, mostly correct — see §4)
  ├─ AggregationService (Zaynor.Application) — fan-out engine
  │     ├─ CuratedProductDataSource   (JSON file, cheap, always on)
  │     ├─ RainforestAmazonDataSource (dormant — no key configured)
  │     ├─ AliExpressProductDataSource (dormant — no key configured)
  │     ├─ GoogleShoppingDataSource   (SerpApi, live)
  │     └─ DataForSeoAmazonDataSource (DataForSEO Merchant API, live)
  ├─ CachedAggregationService decorator (5-min IMemoryCache, per-process)
  ├─ JWT auth (HS256, 7-day tokens, no refresh-token rotation)
  ├─ AlertMonitorService (in-process background worker, polls every 30 min)
  └─ EF Core — ZaynorDbContext (SQLite) / PostgresZaynorDbContext (Postgres)
        │
        ▼
SQLite file (dev) or Postgres (prod, if wired — see §6) — no Redis, no message queue, no search index
```

This is a **modular monolith**, not a set of microservices, and — this needs to be said plainly, because "microservices" is often assumed to be the goal of any "enterprise architecture" exercise — **that is the correct choice at current scale**, and will remain correct through most of the roadmap in §30. Zaynor has one process, one deployable, one datastore. §29 explains exactly which future pieces (not all of them) eventually justify being pulled out, and why the rest never should be.

### 2.2 What the architecture gets right (do not lose these on the way to "enterprise")

- **Real Clean Architecture layering**, verified by dependency direction, not by folder names: `Zaynor.Domain.csproj` has *zero* `PackageReference` entries — it cannot accidentally depend on EF Core, ASP.NET, or any HTTP client, because nothing forces it not to except discipline, and that discipline has held. `Zaynor.Application` references only `Zaynor.Domain` plus two abstraction-only packages (`Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`). This is the actual textbook Clean Architecture dependency rule, working, in a solo-founder codebase — most teams get this wrong at 10x the headcount.
- **A genuinely consistent integration pattern.** Every external paid data source (Rainforest, AliExpress, DataForSEO, SerpApi/GoogleShopping, Serper) follows the identical "dormant until configured" contract: read config, expose `IsEnabled`, return `Array.Empty<StoreOffer>()` immediately if not enabled, wrap the real call in try/catch that logs and fails soft. This was applied five separate times, by hand, and stayed consistent — that is a strong signal the team (even a team of one) understands the pattern well enough to keep enforcing it without a base class forcing them to (see §4.3 for why a base class is still the right next step).
- **Fail-soft is real, not aspirational.** `AggregationService.QuerySourceAsync` catches per-source failures so one dead API never 500s the whole search (`backend/src/Zaynor.Application/Aggregation/AggregationService.cs` — confirmed in the backend audit). This is exactly the NFR4 requirement from the founding spec, actually implemented.
- **The affiliate monetization path is live and was verified end-to-end during this engagement**, not just designed: `/api/out` correctly appends the Amazon Associates tag (confirmed via a direct redirect test returning `Location: https://www.amazon.sa/...&tag=zaynor-21`) and correctly passes a pre-tagged Noon partner tracking link through untouched. Revenue mechanics work today, in production.
- **Bilingual RTL/LTR + dark mode is not bolted on** — it's structural: 266 translation keys with **exact parity** between `en` and `ar` blocks (verified programmatically during the frontend audit — zero missing, zero orphaned keys in either language), and dark-mode/RTL overrides live in the same stylesheet as the base rules they override, which is the right call for a single-stylesheet architecture (splitting into CSS Modules later, §17, has to solve this same problem differently, and that migration cost should be planned for, not discovered).
- **The test suite that exists is genuinely rigorous where it exists**: `ApiIntegrationTests.cs` (653 lines, 31 tests, full `WebApplicationFactory` coverage of auth, reviews, saved products, support tickets, and the affiliate-link edge cases like the `dib_tag` vs `tag` false-positive regression) and `GoogleShoppingDataSourceTests.cs` (543 lines, 16 tests covering relevance filtering, accessory-mismatch filtering, price-outlier removal) are not superficial smoke tests — they encode real, previously-observed production bugs as regression tests. 109 backend tests currently pass.

### 2.3 What the architecture gets wrong, in one paragraph each

- **Nothing enforces the layering it has.** There is no architecture test (`NetArchTest`/`ArchUnitNET`) asserting "Api must not reference concrete Infrastructure types" or "Application must not reference EF/HttpClient." Two violations already exist as a result: `CatalogController` constructs `Zaynor.Infrastructure.DataSources.CuratedProductDataSource` directly, and `OutController` injects `ZaynorDbContext` directly and contains inline affiliate-tagging business logic that's duplicated (not shared) with `AffiliateEligibility.IsMonetized`. Both violations happened because nothing stopped them, not because of an isolated lapse — which means a tenth controller written the same way (by a future contributor, or by an AI coding agent pattern-matching on existing files) is exactly as likely.
- **One CSS file for the entire product.** 4,218 lines, zero scoping, zero build-time dead-code detection. Confirmed dead CSS already exists (`.site-reviews-section` rules for a component that is never rendered, §5.9) and nothing catches it. This is fine at 34 comment-banner sections; it will not be fine at 50, especially once an admin dashboard, a notifications center, and mobile-adjacent web views are all fighting over the same global class namespace.
- **The database-durability story is inconsistent between what's committed and what's presumably actually running.** `render.yaml` (committed, line 27) still points at `Data Source=/tmp/zaynor.db` — SQLite, on a filesystem Render's own free tier explicitly does not persist across deploys. The repository *also* contains a fully-formed `PostgresZaynorDbContext` and a parallel migration history, but nothing in CI or `docker-compose.yml` ever runs those migrations against a real Postgres instance. Either production is silently resetting user data on every deploy (a severe, unnoticed data-loss bug), or the real Render dashboard config has manually diverged from what's checked into git (an infrastructure-as-code drift problem that's arguably just as serious, because it means the committed `render.yaml` is not a trustworthy source of truth for how the system actually runs). §6 and §22 treat this as the single highest-priority infrastructure item in this whole document.
- **CI builds and deploys; it does not gate.** `.github/workflows/deploy.yml` runs `docker build` → push → trigger Render redeploy on every push to `main`, with **no `dotnet test` step and no `npm run build`/typecheck step** in the pipeline itself. Every green deploy in this project's history happened because a human (or an AI assistant acting as one) manually ran tests locally before pushing — which has worked so far precisely because there has only ever been one person pushing. It stops working the moment a second contributor, or a scheduled/automated code-writing agent, pushes without knowing that unwritten rule.
- **The frontend has no runtime type safety at the API boundary and no strict null checks.** `strictNullChecks` is off in both `tsconfig.app.json` and `tsconfig.node.json` (confirmed: zero occurrences of "strict" anywhere in any tsconfig), and every API response is a raw `as X` type assertion with no runtime validation. Zero literal `any` types exist in the codebase — a genuinely strong result — but that's a much weaker guarantee than it sounds, because with `strictNullChecks` off, `null`/`undefined` silently satisfy every type anyway. The backend DTOs are exactly the shapes most likely to evolve as more store connectors and a broader search engine are added (§9), which is exactly when silent frontend drift becomes expensive.

---

## 3. Current Folder Structure Analysis

```
ZAYNOR/
├── backend/
│   ├── src/
│   │   ├── Zaynor.Domain/            13 files — entities only, 0 package refs
│   │   ├── Zaynor.Application/       39 files — interfaces, DTOs, pure logic, AggregationService
│   │   ├── Zaynor.Infrastructure/    50 files (26 are EF migrations) — EF, all DataSources, all concrete services
│   │   └── Zaynor.Api/               16 files — 14 controllers + Program.cs + curated-catalog.json
│   └── tests/
│       └── Zaynor.Application.Tests/ 14 files — 109 tests total
├── frontend/
│   └── src/
│       ├── pages/          18 page components, 1:1 with routes, no nesting
│       ├── components/     ~35 components, flat, no feature grouping
│       ├── api/             client.ts (341 lines, 31 functions) + types.ts (144 lines, 16 interfaces)
│       ├── auth/            AuthContext, AuthProvider, useAuth, ProtectedRoute, AdminRoute, token.ts
│       ├── theme/            ThemeContext, ThemeProvider, useTheme
│       ├── i18n/              LanguageContext, LanguageProvider, useTranslation, translations.ts (645 lines)
│       ├── toast/               ToastContext, ToastProvider, useToast
│       ├── hooks/                 usePageTitle, useRecentSearches
│       ├── App.css              4,218 lines — every component's styles, one file
│       └── index.css               62 lines — reset + base
├── docs/
│   └── PROJECT_SPECIFICATION.md  535 lines — founding product spec (this doc's companion)
├── .github/workflows/    deploy.yml, keepalive.yml, set-render-env.yml
├── Dockerfile            root — single-image combined build (used for actual Render deploy)
├── backend/Dockerfile    split build — used only by docker-compose.yml
├── frontend/Dockerfile   split build — used only by docker-compose.yml
├── docker-compose.yml    local full-stack dev, split services
├── render.yaml           Render Blueprint — still defaults to SQLite /tmp (see §2.3, §6)
└── src/                  ⚠ EMPTY orphaned directory at repo root — dead, should be deleted
```

### 3.1 Problems specific to structure (not content)

1. **`./src` at the repository root is a confirmed-empty, orphaned directory.** Not a build artifact, not gitignored content — just an empty folder sitting next to `backend/` and `frontend/`. Harmless today; actively misleading to anyone (human or AI agent) who globs the repo root looking for source and finds a `src/` that isn't the real one.
2. **Frontend has no feature grouping** — `pages/`, `components/`, `hooks/` are each one flat folder regardless of feature. This works at 18 pages / 35 components. It will not read cleanly once "AI Shopping Assistant," "Notifications," and "Admin Dashboard" (already 4 pages: `AdminDashboardPage`, `AdminTicketsPage`, `AdminTicketThreadPage`, `AdminReviewsPage`) each want their own components, hooks, and API calls — right now those four admin pages' components already live in the same flat `components/` folder as everything else, indistinguishable at a glance from consumer-facing ones.
3. **Backend has no per-feature vertical folder** either, but this matters less: `Zaynor.Application`'s existing sub-namespaces (`Aggregation/`, `Auth/`, `Reviews/`, `Support/`, `UserItems/`, `SiteReviews/`) already *are* a reasonable feature grouping inside the Application layer, mirrored exactly in `Zaynor.Infrastructure`. The backend's structural problem is narrower and more specific than "no grouping" — it's the two concrete layering leaks (§2.3, §4.2) and the missing shared base for `IProductDataSource` implementations (§4.3), not the folder layout itself.
4. **Three Dockerfiles, one of them the one actually used in production** (root `Dockerfile`), the other two (`backend/Dockerfile`, `frontend/Dockerfile`) existing solely to support `docker-compose.yml`'s local split-service dev flow. Not wrong, but undocumented as a *reason* anywhere in the repo — a new contributor has no way to know which Dockerfile is "the real one" without reading `render.yaml` and `.github/workflows/deploy.yml` first. §22 recommends a short `docs/DEPLOYMENT.md` explaining this split, not collapsing it (both really are needed for their respective use cases).
5. **`docs/` currently contains exactly one file.** Good that it exists at all (many projects at this stage have zero committed product spec); the gap is technical/operational documentation — no `CONTRIBUTING.md`, no architecture-decision-record folder, no runbook. §15 places this document and its recommended siblings inside `docs/` explicitly so this doesn't stay a single-file folder.

---

## 4. Backend Analysis

*(Full detail from the dedicated backend audit; summarized and re-organized here. All file/line citations were independently verified.)*

### 4.1 Layering — mostly correct, two concrete leaks

| Project | Depends on | Package refs | Verdict |
|---|---|---|---|
| `Zaynor.Domain` | (nothing) | **none** | Clean — 13 POCO entities, no EF attributes (mapping lives in `ZaynorDbContext.OnModelCreating`) |
| `Zaynor.Application` | Domain only | DI + Logging abstractions only | Clean — interfaces, DTOs, `AggregationService`, `ArabicBrandNormalizer`, `ProductNormalizer`, `OutboundLinkSigner`, `AlertConditions`, `AffiliateEligibility` |
| `Zaynor.Infrastructure` | Domain + Application | EF Core (SQLite + Npgsql), BCrypt, JWT libs | Clean — both DbContexts, all 5 `IProductDataSource`s, all concrete services |
| `Zaynor.Api` | all three | ASP.NET, Swashbuckle | **Two leaks**: `CatalogController` → concrete `CuratedProductDataSource`; `OutController` → raw `ZaynorDbContext` + inline business logic |

`OutController` (`backend/src/Zaynor.Api/Controllers/OutController.cs`) is the more serious of the two leaks: it decides which affiliate tag/UTM-suffix/deeplink-template applies (lines 78–99), verifies the host allowlist and HMAC signature for the open-redirect guard (lines 68–76), and writes directly to `ZaynorDbContext.ClickEvents` (lines 104–111) — none of which is unit-testable without spinning up the full ASP.NET pipeline, and it duplicates host-matching rules that `AffiliateEligibility.IsMonetized` (Application layer) already encodes separately. §16 gives the concrete extraction plan.

### 4.2 The `IProductDataSource` family — the clearest "more of this is coming" risk

Five implementations exist (`CuratedProductDataSource`, `RainforestAmazonDataSource`, `AliExpressProductDataSource`, `GoogleShoppingDataSource`, `DataForSeoAmazonDataSource`). The four "live" ones (everything but Curated) repeat, field-for-field, the same ~25-line skeleton: constructor shape (`IHttpClientFactory, IConfiguration, ILogger<T>`), the `IsEnabled` config-presence check, the early-return-if-disabled guard, and the `catch (Exception ex) when (ex is not OperationCanceledException)` fail-soft wrapper. This is the **single most important finding for the stated roadmap**, because "Store Connectors" (a named roadmap item) means writing this exact skeleton a 6th, 7th, 8th time by hand, copy-pasted from whichever existing source is closest. §16.2 specifies the shared base class that turns this from "copy 25 lines and hope you got the try/catch right" into "implement one abstract method."

Two additional, concrete bugs were found in the DataForSEO source *during this engagement* (both already fixed in a prior turn of this session, cited here because they're exactly the class of bug the shared base class in §16.2 would make structurally impossible to reintroduce for the next connector): the class initially forgot to normalize Arabic-script queries before sending them to the vendor API (an Arabic category word like "نظارة" was sent untranslated and matched nothing), and its `HttpClient` timeout (20s) was shorter than a real observed vendor response time (28.9s measured directly), causing valid slow responses to be silently discarded as timeouts, indistinguishable from "no data" without testing the raw vendor API directly.

### 4.3 Duplication inventory (concrete, cited)

| What | Where | Fix |
|---|---|---|
| "Find-or-create Product by NormalizedKey" | `UserItemsService.cs:138-153` and `PriceHistoryRecorder.cs:76-90`, independently | Extract a shared `ProductLookup` helper — exactly mirroring the `StoreLookup` helper that *already exists* for the equivalent Store case (`Aggregation/StoreLookup.cs`), i.e. this is finishing a refactor that was already half-done |
| Auth-guard idiom `if (User.GetUserId() is not int userId) return Unauthorized();` | 13 call sites across `AlertsController`, `AdminSupportController`, `SupportTicketsController`, `SavedProductsController`, `SiteReviewsController`, `ReviewsController` | A base controller (`AuthenticatedControllerBase` with a `CurrentUserId` property, or 401-mapping middleware) — a forgotten copy of this guard is a real auth bug, not just style |
| `ReviewDto` 8-field projection | Repeated 4× in `ReviewService.cs` (lines 35-45, 56-66, 77-86, 141-151), only 2 of 4 call sites use the existing private `ToDto` helper | Use `ToDto` (or a `static FromEntity`) at all four call sites |
| Affiliate host-matching rules | `OutController.Go()` (lines 78-99) **and** `AffiliateEligibility.IsMonetized` (Application layer) — independently maintained, doc-commented as "kept in sync deliberately" | Single `AffiliateHostMatcher` used by both — the "kept in sync deliberately" comment is itself the smell; deliberate duplication that must be remembered is still duplication |
| Length-bounded field validation (`if (string.IsNullOrWhiteSpace(x) \|\| x.Length > N) return BadRequest(...)`) | Hand-repeated per field across `AdminReviewsController`, `AdminSupportController`, `ReviewsController`, `SiteReviewsController`, `SupportTicketsController` — zero use of DataAnnotations anywhere | A small `[Required(maxLength)]`-style validation attribute, or FluentValidation if the roadmap's admin dashboard grows enough forms to justify the dependency |

### 4.4 Testing gaps (backend)

`AlertsController`, `CatalogController`, and `AlertMonitorService` have **zero** test coverage — no unit test, no integration test. Every Infrastructure service class (`ReviewService`, `SiteReviewService`, `SupportTicketService`, `UserItemsService`, `AuthService`, `PriceHistoryRecorder`, `PriceHistoryService`, `SearchSuggestionService`, `StoreLookup`) is reachable only indirectly through `ApiIntegrationTests` — several are not reached by *any* test. `CuratedProductDataSource`'s scoring logic (`Score`/`WordOverlapScore`, the algorithm that decides "most specific product wins" for curated-catalog matches) has no dedicated unit test despite being the most intricate matching logic in the `DataSources` folder.

---

## 5. Frontend Analysis

*(Full detail from the dedicated frontend audit; summarized and re-organized here.)*

### 5.1 Inventory

- **18 pages** (`src/pages/`), one-to-one with routes, no nesting beyond a single shared `Layout`.
- **~35 components** (`src/components/`), flat, no feature subfolders.
- **31 exported API functions** in one `client.ts` (341 lines) + 16 DTO interfaces in `types.ts` (144 lines).
- **4 Context+Provider+hook triples**: Auth, Theme, Language, Toast — a clean, consistently-applied pattern, all manually nested in `main.tsx`.
- **266 translation keys**, exact `en`/`ar` parity, dot-notation namespacing, no pluralization/ICU support.
- **4,218-line single CSS file**, no scoping mechanism of any kind.
- **3 runtime dependencies total**: `react`, `react-dom`, `react-router-dom`. No state library, no HTTP client library, no UI kit, no date library, no form library.

### 5.2 The API layer has two competing patterns

20 of 31 functions in `client.ts` go through a shared `authFetch()` helper (attaches bearer token, throws via a shared `readError()`) — a genuinely good, consistent pattern. The other 11 (unauthenticated calls — search, catalog, public reviews, registration/login) each hand-roll their own `fetch()` + `if (!response.ok)` check, and **inconsistently** choose between throwing and silently returning an empty array/`null` on failure, with no documented rule for which behavior a given caller should expect. A caller of `getCatalog()` cannot distinguish "genuinely no products" from "the server is down." This matters more, not less, as more store connectors mean more ways a request can partially fail.

### 5.3 Duplicated near-identical page pairs

- `NoonFallbackLink.tsx` / `AmazonFallbackLink.tsx` — deliberately parallel (this document's own author built the second by mirroring the first, earlier in this engagement), a reasonable trade at 2 instances, worth a shared `<StoreFallbackLink store={} query={} />` the moment a 3rd store-connector fallback is needed.
- `AdminTicketThreadPage.tsx` (115 lines) / `SupportTicketPage.tsx` (103 lines) — ~90% identical thread-rendering/reply-form code, differing only in which `client.ts` functions they call and the admin-only close button.
- `ReviewsSection.tsx` / `SiteReviewsSection.tsx` — identical 6-`useState` rating-input state shape declared independently in both.
- Date formatting (`toLocaleDateString` with locale/options JSX) hand-repeated in 6 different files instead of one `formatDate()` utility alongside the existing `format.ts`.

### 5.4 Type safety: strong on the surface, weaker underneath

Zero literal `any` anywhere in `src/` — a genuinely strong result. But `strictNullChecks` is off (confirmed: zero occurrences of the string "strict" in any of the three tsconfig files), so every `(await response.json()) as X` type assertion is unchecked against `null`/missing fields, and the several `result!.offers`-style non-null assertions in `ProductPage.tsx` are currently no-ops from the compiler's perspective — there is nothing strict for them to assert past. This is the single highest-value, lowest-risk config change available in the whole codebase (§17.4).

### 5.5 Dead code: one fully-orphaned feature

`SiteReviewsSection.tsx` (160 lines, a complete "rate Zaynor itself" component) is never imported anywhere in `src/` — confirmed by searching every file for the string. This makes `getSiteReviews`/`submitSiteReview`/`deleteSiteReview` in `client.ts` (lines 323-341), an entire CSS section in `App.css` (from the "Home testimonials" banner onward), and 11 `siteReviews.*` translation keys (×2 languages) all unreachable dead weight shipped to every visitor. This reads like a missed integration step, not abandoned work — it's feature-complete. **Decide either way before it bit-rots further**: wire it into `HomePage.tsx`, or delete all four artifacts together.

### 5.6 No code-splitting at all

Zero occurrences of `React.lazy`/`Suspense` anywhere. All 18 pages — including all 4 admin-only pages, which require an admin account and are the least likely to be needed by a typical anonymous visitor — ship in one JS bundle to every visitor. This is a direct, easily-fixed cost that compounds with every roadmap item that adds pages (notifications UI, price-history UI, AI assistant UI).

---

## 6. Database Analysis

### 6.1 Schema (as it exists in migrations)

`Store`, `Product`, `Category`, `Offer`, `User`, `Alert`, `SavedProduct`, `PriceHistory`, `ClickEvent`, `Review`, `SupportTicket`, `SupportMessage` — 12 entities across 6 migrations (`InitialCreate`, `AddClickEvents`, `AddIsAdminToUsers`, `AddReviews`, `AddSupportTickets`, `AddSiteReviews`), each migration hand-duplicated into a second, parallel SQLite-vs-Postgres pair (`Migrations/` and `Migrations/Postgres/`, distinguished by `[DbContext(typeof(...))]` attribute).

### 6.2 The two critical findings

1. **`Product.NormalizedKey` has a non-unique index** (`ZaynorDbContext.cs:49-50` — `.HasIndex(p => p.NormalizedKey)`, no `.IsUnique()`), and the check-then-insert "find or create" pattern is implemented independently in two places (§4.3) with no transaction or DB-level constraint backing either. **Two concurrent requests for a never-before-seen product name can and will, under real concurrent traffic, insert two `Product` rows with the identical key**, silently fragmenting that product's price history and saved-product/alert linkage from that point forward. This is invisible at today's traffic and will surface, unexplained, as "why does this product have two separate price histories" the moment real concurrent load exists. Fix: add a unique index (via migration) plus either a DB-level upsert or an application-level retry-on-conflict; consolidate the two independent find-or-create implementations into one shared helper while doing it (§16.3).
2. **The Postgres migration path is, as far as this audit can determine, never exercised.** `render.yaml` (committed) still configures SQLite at `/tmp/zaynor.db` — a path Render's free tier does not persist across deploys. `docker-compose.yml` only stands up SQLite. `PostgresZaynorDbContextFactory` exists solely for design-time migration scaffolding against a hardcoded `localhost` connection string nothing in CI ever connects to. Either (a) production is genuinely running on ephemeral SQLite and silently loses all user accounts/reviews/saved-products/price-history on every redeploy — a severe bug that just hasn't been *noticed* because a solo founder doesn't necessarily re-test their own login after every deploy — or (b) the real Render dashboard has a manually-set Postgres connection string that diverges from the committed `render.yaml`, meaning the infrastructure-as-code in this repo cannot be trusted to describe what's actually running. **This must be resolved before any other database recommendation in this document matters**, because every other recommendation assumes data actually persists. See §22.2 for the concrete verification steps.

### 6.3 Other findings

- `Category` and `Offer` tables are defined and migrated in every schema version but **never read or written anywhere in application code** (confirmed via full-repo search — only `DbSet` declarations and migration files reference them). Category browsing is actually served entirely from a static `curated-catalog.json` file via `CuratedProductDataSource.GetSummaries()`. Either wire these tables into the roadmapped category/admin work or drop them — right now they're schema that lies about what's actually persisted.
- `Store.Name` has no index at all, and every lookup does `s.Name.ToLower() == trimmed.ToLower()` — a function-wrapped predicate no plain B-tree index can serve. Called on every review submission and every price-history write. Invisible today (tiny Stores table); a full-scan-per-write pattern that degrades as store count grows with more connectors.
- No pagination anywhere — every list endpoint (`AdminReviewsController`, `SupportTicketsController`, `ReviewsController`, `SiteReviewsController`) returns its full result set. Fine today; needs keyset pagination before the admin dashboard or any high-review-count store gets real usage.
- No distributed cache — `IMemoryCache` only (§27), meaning cached aggregation results do not survive a process restart and would not be shared across multiple instances if the API is ever horizontally scaled.

---

## 7. API Analysis

### 7.1 Shape

REST over JSON, 14 controllers, camelCase serialization (ASP.NET Core default). No API versioning scheme exists (no `/v1/` prefix, no header-based versioning). No OpenAPI consumers exist yet beyond Swashbuckle's own generated document (confirmed present via the `Swashbuckle.AspNetCore` package reference), but nothing publishes or version-pins that document for external consumption.

### 7.2 Concrete problems

- **No versioning at all.** The moment a mobile app (§28) or a public API (explicitly on the founder's long-term feature list, `docs/PROJECT_SPECIFICATION.md` doesn't name it but the brief for this document does) consumes these endpoints, any breaking change to a DTO shape breaks every existing client with no migration window. Introducing versioning *before* a second consumer exists is dramatically cheaper than introducing it after.
- **Error responses are an untyped anonymous object** (`new { error = "..." }`), consistent in shape across the codebase but not a real contract — nothing generates a client-side type for it, and a mobile client or third-party API consumer has no schema to code against.
- **No pagination contract** (§6.3) — this is as much an API design problem as a database one, since it's the response shape that has no `page`/`pageSize`/`totalCount` envelope anywhere.
- **`OutController` mixes a redirect endpoint with business logic** that arguably belongs behind its own internal service call, not inline in the controller (§4.1) — from a pure API-design lens, `/api/out` is doing three jobs (host validation, affiliate tagging, click logging) that a well-factored API would expose as one clear responsibility with the other two delegated.
- **No rate limiting observed anywhere in `Program.cs`** beyond whatever ASP.NET Core's defaults provide (none, by default) — every endpoint, including the unauthenticated search endpoint that fans out to five paid external APIs per request, has no request-rate protection. This is both a cost-control gap (a scraper or bot hammering `/api/search` burns paid DataForSEO/SerpApi credits with no ceiling) and a basic API-hygiene gap.

---

## 8. AI Readiness

### 8.1 Where Zaynor stands today relative to "AI Shopping Assistant"

There is currently **zero AI/ML code in the repository** — no LLM client, no embeddings, no vector store, no recommendation model. This is not a criticism (the founding spec explicitly defers AI features, `docs/PROJECT_SPECIFICATION.md` §19), but it means the "AI Readiness" question is really "how much of the current architecture would an AI assistant feature have to fight against," not "how far along is the AI work."

### 8.2 What's already reusable for an AI assistant

- `AggregationService` already does the hard, boring 80% of "find and compare products" — an AI assistant's job is to sit *on top of* this (query understanding → call the existing search → reason over results → respond), not replace it. This is a real asset: the assistant doesn't need its own product-search logic.
- `ArabicBrandNormalizer` already solves a real, narrow NLP problem (colloquial Arabic brand-name/category-word normalization via Levenshtein-distance fuzzy matching, `backend/src/Zaynor.Application/Aggregation/ArabicBrandNormalizer.cs`) that any AI assistant answering in Arabic would otherwise have to solve again — this logic should be a dependency of the assistant, not duplicated inside it.
- The bilingual i18n architecture (§5) means an AI assistant's UI chrome (buttons, disclaimers, loading states) can reuse the exact same `useTranslation()`/`translations.ts` pattern with zero new infrastructure.

### 8.3 What's missing, concretely

- **No abstraction boundary for "a thing that calls an LLM."** Every external integration in this codebase (§4.2) is a product-*data* source implementing `IProductDataSource`. An LLM call is a different kind of external dependency (conversational, stateful across a session, streaming-capable, cost-metered per-token not per-request) and needs its own interface family (`IAiAssistantProvider` or similar) — not shoehorned into the existing `IProductDataSource` contract.
- **No conversation/session state model.** `Alert`, `SavedProduct`, `PriceHistory` are all single-record-per-fact entities; an AI assistant needs a `Conversation`/`Message` history model (even a simple one) to hold context across turns — nothing in the current schema anticipates this.
- **No token/cost metering.** `ClickEvent` (§6) is the closest existing analog (append-only event log for a billable-adjacent action) — the same pattern (an append-only `AiInteraction` table: user, tokens in/out, cost, timestamp) is the natural fit, and should be built the same way `ClickEvent` was: cheap, append-only, queryable later for cost dashboards.
- **No streaming response support anywhere in the API layer.** ASP.NET Core supports Server-Sent Events / chunked responses natively; nothing in the current 14 controllers uses them (every response is a single JSON payload). An assistant that "types" its answer needs this, and it's worth prototyping against a low-stakes endpoint before the assistant feature itself needs it.

### 8.4 Recommendation (see §20 for the concrete architecture)

Build the AI assistant as a **new, isolated module** (`Zaynor.Application/AiAssistant/` + `Zaynor.Infrastructure/AiAssistant/`, following the exact folder-per-feature convention already used for `Reviews`/`Support`/`UserItems`) that *calls* `IAggregationService` as a dependency, the same way a human user's search does — never as a fork or parallel reimplementation of product search. This keeps the assistant honest (it can only ever recommend products the real aggregation engine actually found, which matters enormously for a platform whose entire brand promise is "we don't fabricate data," per this session's own repeated engineering practice of never inventing offer data) and means improvements to the core search engine (§9, §21) automatically improve the assistant too.

---

## 9. Search Engine Readiness

### 9.1 What "search" means in Zaynor today

There is no search *index* — no Elasticsearch/Postgres full-text/Meilisearch/Algolia. "Search" today is: normalize the query (`ProductNormalizer`, `ArabicBrandNormalizer`) → fan out to 5 live `IProductDataSource`s in parallel → merge, dedupe by store, rank cheapest-first, filter relevance/outliers → return. This is **live-aggregation-as-search**, not indexed search, and it is a deliberate, correct choice for a price-comparison engine whose whole value proposition is *current* prices — indexing prices would mean serving stale data, which directly contradicts the founding spec's own instinct (`docs/PROJECT_SPECIFICATION.md` §3, "the site should not store prices but fetch them live at search time").

### 9.2 Where this pattern is already showing strain

- `GoogleShoppingDataSource`'s relevance filtering (`IsRelevant`/`IsAccessoryMismatch`/`TokensMatch`, ~200 lines of hand-tuned token-matching heuristics, §4) is real, working, and *also* the clearest sign that live-aggregation search is approaching the limit of what hand-written string heuristics can carry. Every new observed failure mode (an accessory mismatched as the product, an unrelated item sharing one keyword) has been patched with another hand-added keyword or rule — this works, but each fix is bespoke and the list only grows.
- Category browsing (§6.3) is served from a static JSON file, not the live search engine at all — meaning "browse by category" and "search for a product" are two entirely separate code paths today, which will need to converge as "Product Search Engine" (a named roadmap item) matures past curated-catalog coverage.
- There is no query-log/analytics table capturing what users actually search for and what returned zero results — `AggregationService.SearchAsync` logs a zero-result query at `Information` level (`_logger.LogInformation("No offers found for query {Query}", trimmed)`) but this goes to application logs, not a queryable store, so there is currently no way to answer "what are the top 50 searches that return nothing" without grepping log files — exactly the data needed to prioritize the next 10 category-word translations or connector integrations.

### 9.3 Recommendation (see §21 for the concrete architecture)

Do **not** introduce a search index to replace live aggregation — that would be solving a problem Zaynor doesn't have (stale-data risk) to gain a capability it doesn't yet need (sub-100ms full-text search over a static corpus). Do introduce a lightweight **query-analytics table** now (append-only, same pattern as `ClickEvent`) so that search-quality decisions (which categories to add typo-correction for, which connectors to prioritize) are driven by real zero-result-query data instead of whichever failure a user happens to report that week — which is exactly how the "نظارة"/"مكياج"/"sunglasses" fixes earlier in this engagement were actually discovered: by a user reporting a specific miss, not by data. That's a fine way to find the *first* ten misses; it does not scale to finding the next thousand.

---

## 10. Security Review

### 10.1 What's solid

- JWT auth with HS256, a required (fail-fast-if-missing) signing key (`Program.cs:42` throws if `Jwt:Key` is absent), and password hashing via BCrypt.Net — all standard, correct choices.
- Every data-source API credential and the JWT signing key are sourced from environment variables in production (verified: `appsettings.Development.json` contains only an explicitly-labeled local placeholder key, never a real secret) — no secret was found committed to the repository.
- The outbound-redirect endpoint (`OutController`) has a real open-redirect guard: a static host allowlist plus an HMAC signature check (`OutboundLinkSigner`) for dynamically-discovered store URLs that aren't on the allowlist — this is a genuine, non-trivial security control that a lot of simpler affiliate-link setups skip entirely.
- Ownership isolation is tested: `ApiIntegrationTests` explicitly verifies a user cannot read another user's support tickets.

### 10.2 Gaps

- **No rate limiting anywhere** (§7.2) — the unauthenticated `/api/search` endpoint has no per-IP or per-session throttle, and it's the single most expensive endpoint in the system (fans out to paid external APIs). This is simultaneously a security gap (trivial to abuse) and a cost-control gap.
- **No refresh-token rotation** — JWTs are long-lived (7 days per the auth review) with no revocation mechanism; a leaked token is valid until natural expiry. Acceptable at current scale and user count; worth a short-lived-access-token + refresh-token pattern before the user base or the sensitivity of stored data (payment info, if that's ever added) grows.
- **No architecture test enforcing layering** (§2.3) is also a *security*-relevant gap, not just a style one: the two existing leaks mean `CatalogController` and `OutController` are the two controllers most likely to accumulate ad-hoc logic outside the tested, reviewed Application-layer path.
- **No security headers audit performed** as part of this document (out of scope for a read-only architecture review of this size) — recommend a follow-up pass specifically checking CSP, HSTS, `X-Content-Type-Options`, and CORS configuration (`Cors:Origins`, currently defaulting to `localhost:5173` per `Program.cs:28`) against the actual production origin list.
- **Admin bootstrap mechanism**: the plan file referenced elsewhere in this project's history describes an `Admin:Email`-config-driven bootstrap that promotes a matching registered user to admin on startup — this is a reasonable pattern for a solo-founder admin account, but it means admin status is granted by *email string match in config*, not by any auditable action; worth revisiting once a second admin/support-agent role is needed (§28/§29 both eventually want role granularity beyond "admin or not").

---

## 11. Performance Review

### 11.1 Confirmed, fixed-during-this-engagement issues (kept here for the record)

- **Price-history recording used to run synchronously on the request path** — up to ~30 offers × 2 unbatched DB round-trips each, added multiple real seconds to every live search. Fixed (already shipped): recording now happens fire-and-forget on its own DI scope, never blocking the response.
- **`DataForSeoAmazonDataSource`'s HttpClient timeout (20s) was shorter than real observed vendor response times (28.9s measured)**, silently discarding valid slow responses as timeouts. Fixed (already shipped): raised to 40s.

### 11.2 Standing performance risks

- **`AggregationService` waits for the slowest of every enabled live source on every search** (`Task.WhenAll` over cheap + expensive sources) — correct for completeness, but means overall search latency is bounded by whichever external vendor is slowest *that request*, with no per-source result streamed early. A user searching for something only Amazon (via DataForSEO, ~10-29s observed) covers waits the full DataForSEO latency even if Google Shopping answered in 2s. §21 discusses progressive/streamed results as the eventual fix.
- **`CachedAggregationService`'s cache is process-local `IMemoryCache`**, not distributed — every horizontal replica would maintain its own independent cache, multiplying paid-API calls linearly with replica count instead of sharing hits. Not a problem at one instance; a direct, easily-forgotten cost multiplier the moment the API is scaled horizontally (§27).
- **`AlertMonitorService` checks alerted products sequentially in a `foreach`** (`backend/src/Zaynor.Infrastructure/Alerts/AlertMonitorService.cs`), each iteration re-running the *entire* multi-source aggregation fan-out for one product. Fine at today's alert volume; will not scale to thousands of distinct tracked products without becoming a multi-hour background pass, since there's no batching, concurrency cap, or backoff the way per-search aggregation already has for its own sources.
- **No pagination anywhere** (§6.3, §7.2) means every list response's payload size grows unbounded with data volume — today harmless (small tables), a real payload-size and query-time cost the moment any table (Reviews, SupportTickets) accumulates real production volume.
- **Frontend ships one JS bundle for all 18 pages** (§5.6) — every visitor downloads admin-dashboard code they'll never run.

---

## 12. Scalability Review

### 12.1 Vertical vs. horizontal readiness

Zaynor today scales **vertically only** — one process, one SQLite-or-Postgres connection, one in-memory cache. This is fine through a meaningful range of real traffic (a single modern ASP.NET Core instance comfortably serves thousands of requests/minute for workloads this size), and matches the "modular monolith is correct at this stage" conclusion in §2.1. The concrete blockers to **horizontal** scaling (multiple API instances behind a load balancer), should traffic ever require it, are:

1. `IMemoryCache` is per-process — horizontal scaling multiplies cache misses and paid-API cost linearly with replica count until this becomes a distributed cache (§27).
2. `AlertMonitorService` is an in-process background worker (`IHostedService`-style) — running it on every replica means the same alert gets checked N times per interval instead of once; this needs either a leader-election guard or extraction into a separate singleton worker process before horizontal scaling.
3. SQLite (if that's genuinely what's running in production, §6.2) does not support multiple concurrent writer processes at all — this alone blocks horizontal scaling entirely until the Postgres question is resolved.

### 12.2 The 1 → 10 million user question, honestly

Getting from today's traffic to meaningfully large numbers is **not primarily an architecture problem** at this codebase's current size — it's a product/growth problem (per the founding spec's own honest assessment, §21 of `PROJECT_SPECIFICATION.md`: "growth… depends on marketing and continuity, not just building"). The architecture's job is to not be the reason growth fails once it starts, which means: resolve the database durability question now (§6.2, cheap to fix today, catastrophic to discover after real user data accumulates), introduce the shared connector base class now (§4.2/§16.2, cheap now, exponentially more expensive to retrofit across 10+ connectors later), and defer the genuinely expensive scaling work (distributed cache, horizontal API scaling, read replicas, CDN-fronted static assets) until traffic actually demands it — building that infrastructure speculatively today would violate the same "sequential build" principle the founding spec already correctly argues for.

---

## 13. Code Quality Review

### 13.1 Strengths (verified, not assumed)

- Zero `TODO`/`FIXME`/`HACK`/`NotImplementedException` markers anywhere in `backend/src`.
- Zero literal `any` types anywhere in `frontend/src`.
- Exceptionally thorough doc-comments throughout the backend explaining *why*, not *what* — e.g. `AffiliateEligibility`'s comment explicitly names the duplication risk with `OutController` it's aware of; `GoogleShoppingDataSource`'s `AccessoryKeywords` list comment traces each keyword back to a specific real observed failure. This is a genuinely rare, valuable trait: the code teaches its own history to the next reader.
- 266/266 translation-key parity between English and Arabic, verified programmatically, with no casing inconsistencies across the whole set.

### 13.2 Weaknesses

- No architecture-enforcement tooling (§2.3, §10.2) — every layering rule is currently "convention that has held so far," not "convention the build fails without."
- No dead-export detection (`ts-prune`/`knip`) — this is exactly the class of gap that let `SiteReviewsSection` and its three API functions ship unreferenced without anyone noticing (§5.5).
- `oxlint` is configured with only 2 custom rules (`react/rules-of-hooks`, `react/only-export-components`) — no import-order, no accessibility linting, no TypeScript-specific bans (`no-explicit-any`, `no-non-null-assertion`) configured explicitly.
- No `.editorconfig` or analyzer ruleset on the backend enforcing the layering rules that are currently just... true, by habit.

---

## 14. Missing Components

Consolidated, cross-referenced list — every item below is referenced from its detailed section above.

| # | Missing component | Why it matters for the roadmap | Section |
|---|---|---|---|
| 1 | Shared base/helper for `IProductDataSource` implementations | Every new "Store Connector" currently means hand-copying ~25 lines | §4.2, §16.2 |
| 2 | Unique constraint + shared helper for Product/Store lookup | Live data-integrity bug under concurrent traffic | §6.2, §16.3 |
| 3 | Verified, CI-exercised Postgres migration path | Unknown whether production data durably persists at all | §6.2, §22.2 |
| 4 | CI test gate (`dotnet test` + frontend typecheck) before deploy | Nothing currently prevents a broken build from reaching production | §23 |
| 5 | Architecture-enforcement test (layering rules) | Two leaks already exist with nothing to prevent a third | §10.2, §16.1 |
| 6 | API versioning scheme | Any client beyond the current SPA breaks on the first breaking DTO change | §7.2, §19 |
| 7 | Rate limiting | Cost-control and abuse gap on the most expensive endpoint | §7.2, §10.2 |
| 8 | Distributed cache | Blocks horizontal scaling, multiplies paid-API cost per replica | §12.1, §27 |
| 9 | Pagination contract | Unbounded response sizes as data volume grows | §6.3, §7.2 |
| 10 | CSS scoping mechanism (CSS Modules or equivalent) | One 4,218-line file, zero collision protection, growing every feature | §5, §17.3 |
| 11 | `strictNullChecks` / TypeScript `strict` mode | Cheapest available type-safety fix in the whole codebase | §5.4, §17.4 |
| 12 | Runtime API-contract validation (Zod or similar) at the frontend boundary | Silent drift as backend DTOs evolve for new connectors/search features | §5.2, §19 |
| 13 | Route-based code splitting | Every visitor downloads admin-dashboard code | §5.6, §17.5 |
| 14 | Dead-export detection tooling (`ts-prune`/`knip`) | Already would have caught `SiteReviewsSection` | §13.2 |
| 15 | Query-analytics table (zero-result searches) | No data-driven way to prioritize connector/typo-fix work | §9.2, §21 |
| 16 | AI-assistant abstraction boundary (`IAiAssistantProvider`) | Nothing today anticipates a conversational, streaming, token-metered dependency | §8.3, §20 |
| 17 | Monitoring/APM (no dashboard exists beyond Render's own basic metrics) | No visibility into per-source latency/error rate in production today | §24 |
| 18 | Structured logging / log aggregation | Logs currently go wherever Render's default stdout capture sends them | §25 |
| 19 | Frontend test tooling (zero installed — no Vitest/Jest/Testing Library) | Frontend has 0 automated tests today, vs. 109 on the backend | §26 |
| 20 | Deployment documentation (`docs/DEPLOYMENT.md`) | Three Dockerfiles exist; nothing explains which is authoritative and why | §3.1, §22 |

---

## 15. Recommended Folder Structure

The guiding rule: **grow the existing structure, don't replace it.** Every folder below either already exists or is a natural sibling of one that does.

```
ZAYNOR/
├── docs/
│   ├── PROJECT_SPECIFICATION.md         (existing — unchanged)
│   ├── MASTER_ARCHITECTURE.md            (this document)
│   ├── DEPLOYMENT.md                     (new — explains the 3-Dockerfile split, §22)
│   ├── adr/                              (new — Architecture Decision Records, one file per significant decision going forward)
│   └── runbooks/                         (new — "Postgres migration verification", "how to add a store connector", etc.)
│
├── backend/
│   ├── src/
│   │   ├── Zaynor.Domain/                (unchanged)
│   │   ├── Zaynor.Application/
│   │   │   ├── Aggregation/               (unchanged)
│   │   │   ├── Auth/                      (unchanged)
│   │   │   ├── Reviews/ Support/ UserItems/ SiteReviews/  (unchanged)
│   │   │   ├── AiAssistant/               (new, §20 — interfaces + orchestration only)
│   │   │   └── Search/                    (new, §21 — query-analytics interfaces, search-quality abstractions)
│   │   ├── Zaynor.Infrastructure/
│   │   │   ├── DataSources/
│   │   │   │   ├── ExternalProductDataSourceBase.cs   (new, §16.2)
│   │   │   │   └── ...(existing 5 sources, refactored to derive from the base)
│   │   │   ├── Aggregation/                (unchanged, + new ProductLookup.cs alongside existing StoreLookup.cs)
│   │   │   ├── AiAssistant/                (new, §20 — the actual LLM client implementation)
│   │   │   └── ...(everything else unchanged)
│   │   └── Zaynor.Api/
│   │       ├── Controllers/                (unchanged, minus the two layering fixes in §16.1)
│   │       └── Common/                     (new — AuthenticatedControllerBase, shared validation attributes, §16.1)
│   └── tests/
│       └── Zaynor.Application.Tests/       (unchanged structure; new test files for currently-untested classes, §26)
│
├── frontend/
│   └── src/
│       ├── pages/                          (unchanged for now — see §17.1 for when/why to introduce feature folders)
│       ├── components/
│       │   ├── common/                     (new — StoreLogo, icons, toast UI: genuinely shared primitives)
│       │   ├── admin/                      (new — the 4 admin pages' components move here)
│       │   └── ...(feature folders added only when a feature earns one — §17.1)
│       ├── api/                            (unchanged files; add zod schemas alongside types.ts, §19)
│       ├── auth/ theme/ i18n/ toast/ hooks/ (unchanged)
│       └── styles/                         (new, §17.3 — CSS Modules migration target; App.css shrinks over time, isn't deleted on day one)
│
├── Dockerfile / backend/Dockerfile / frontend/Dockerfile / docker-compose.yml / render.yaml  (unchanged — documented, not collapsed)
└── (src/ at repo root — DELETE, §3.1)
```

**What this deliberately does not do**: introduce a top-level `packages/`/monorepo-tooling split, a `libs/` shared-code folder between frontend and backend, or a `services/` microservices layout. None of those are justified by anything found in this audit — see §29 for exactly when (not if) that changes.

---

## 16. Recommended Backend Architecture

### 16.1 Close the two layering leaks

- **`CatalogController`**: introduce `ICatalogService` in `Zaynor.Application` (one method, `GetSummariesAsync()`), have `Zaynor.Infrastructure`'s existing `CuratedProductDataSource` (or a new thin wrapper around it) implement it, and have the controller depend on the interface. This is a ~20-line change that removes the Api project's only concrete-Infrastructure dependency.
- **`OutController`**: extract an `IOutboundLinkService` (Application layer) owning the host-allowlist check, signature verification, and affiliate-tag/UTM/deeplink decision (reusing `AffiliateEligibility`'s existing logic instead of duplicating it — this single change also resolves the §4.3 duplication finding), and an `IClickEventRecorder` (Infrastructure layer, mirroring the existing `IPriceHistoryRecorder` pattern already used for fire-and-forget writes) for the DB write. The controller becomes: validate input → call `IOutboundLinkService.ResolveTarget()` → fire-and-forget `IClickEventRecorder.RecordAsync()` → redirect. Now unit-testable without `WebApplicationFactory`.
- Add a `NetArchTest`/`ArchUnitNET`-based test (`Zaynor.Application.Tests/Architecture/LayeringTests.cs`) asserting: Domain has no references, Application doesn't reference EF/HttpClient/ASP.NET types, Api only references Application interfaces (never concrete Infrastructure types). This is what makes the fix permanent instead of a one-time cleanup that regresses on the next controller.

### 16.2 `ExternalProductDataSourceBase` — the single highest-leverage backend change

```csharp
public abstract class ExternalProductDataSourceBase<TConfig> : IProductDataSource
{
    protected readonly IHttpClientFactory HttpClientFactory;
    protected readonly ILogger Logger;
    protected readonly TConfig Config;

    public abstract string SourceName { get; }
    public bool IsExpensiveLive => true;
    public bool IsEnabled => Config is not null && IsConfigValid(Config);

    protected abstract bool IsConfigValid(TConfig config);
    protected abstract Task<IReadOnlyList<StoreOffer>> FetchAsync(string query, CancellationToken ct);

    public async Task<IReadOnlyList<StoreOffer>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (!IsEnabled) return Array.Empty<StoreOffer>();
        try { return await FetchAsync(query, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogWarning(ex, "{Source} failed for query {Query}; skipping it", SourceName, query);
            return Array.Empty<StoreOffer>();
        }
    }
}
```

Each of the 4 existing live sources becomes: a `TConfig` record, an `IsConfigValid` one-liner, and a `FetchAsync` implementation containing *only* the vendor-specific HTTP call and response mapping — the ~25 lines of repeated skeleton (§4.2) disappears entirely, and the timeout/normalization bugs found in `DataForSeoAmazonDataSource` during this engagement become structurally harder to reintroduce in connector #6 because there's one fail-soft wrapper instead of five hand-copied ones. **Why now, not later**: this refactor gets linearly more expensive with every new connector added on top of the old pattern; doing it before "Store Connectors" (a named roadmap phase, §30 Phase 1) scales the count is the entire point of doing it now.

**Convergent validation**: a separately-authored engineering spec proposed for this project (independently, without seeing this analysis) describes the identical concept under the name "Store Connector SDK" — a base every store integration implements via `search()`, `getProduct()`, `getPrice()`, `normalize()`, `health()`, `metadata()`, `validation()`. Two things worth taking from that convergence: first, it's a good sign the design is right, since two independent analyses of the same problem landed on the same shape. Second, it names one real gap the design above doesn't yet have: **a `HealthCheckAsync()` method**, separate from the request-path `IsEnabled` check — a lightweight, independently-callable probe (e.g. "does this vendor's API respond to a trivial request within N seconds *right now*") that an ops dashboard (§24) could poll on a schedule, distinct from "is a key configured." Add it to the base class:

```csharp
public virtual async Task<bool> HealthCheckAsync(CancellationToken ct = default)
{
    if (!IsEnabled) return false;
    try { return await ProbeAsync(ct); }
    catch { return false; }
}
protected virtual Task<bool> ProbeAsync(CancellationToken ct) => Task.FromResult(true);
```
Default no-op (`ProbeAsync` returns true), overridable per-source only where a cheap real probe exists (e.g. DataForSEO's `appendix/user_data` balance endpoint, already used manually during this engagement to check account status — exactly the kind of check that should be automated once §24's monitoring exists instead of run by hand). This is additive to §16.2's design, not a change to it, and costs nothing until a connector actually implements `ProbeAsync`.

### 16.3 `ProductLookup`, mirroring the existing `StoreLookup`

Extract the identical find-or-create logic from `UserItemsService.FindOrCreateProductAsync` and `PriceHistoryRecorder.FindOrCreateProductAsync` into one `Aggregation/ProductLookup.cs`, exactly matching the shape of the `StoreLookup` helper that already exists for the equivalent Store case — this is finishing a refactor the codebase already started, not inventing a new pattern. Pair this with the unique-index migration from §6.2/§18.

### 16.4 Trade-off explicitly acknowledged

None of §16.1-16.3 changes any external behavior, API contract, or database schema in a user-visible way (the unique index is additive). This is intentional: **Phase 1 of the roadmap (§30) is entirely internal hardening**, precisely so it can be done with confidence while the site keeps serving real users and real affiliate clicks throughout.

---

## 17. Recommended Frontend Architecture

### 17.1 Feature folders — introduced gradually, not all at once

Do not restructure `pages/`/`components/` wholesale. Instead: the next time a feature area is touched (admin dashboard is the obvious first candidate, since it already has 4 pages and several admin-only components mixed into the flat `components/` folder), move *that feature's* files into `pages/admin/` + `components/admin/` and leave everything else where it is. This mirrors exactly how the backend's `Application`/`Infrastructure` folders are already organized by feature (`Reviews/`, `Support/`, `UserItems/`) — the frontend should converge toward the same shape the backend already has, one touched feature at a time.

### 17.2 Consolidate the two near-duplicate admin/support ticket thread pages

Extract a shared, presentational `<TicketThread messages={} onReply={} renderExtra={} />` component consumed by both `AdminTicketThreadPage` and `SupportTicketPage`, with each page supplying only its own data-fetching (`getAdminTicket`/`addAdminReply`/`closeTicket` vs `getMyTicket`/`addTicketMessage`) and the admin-only close button via `renderExtra`. Do the same for the `ReviewsSection`/`SiteReviewsSection` duplicated rating-input state — but only once the §5.5 decision (wire up or delete `SiteReviewsSection`) is made, since consolidating dead code is wasted effort.

### 17.3 CSS: introduce CSS Modules incrementally, keep `App.css` as the shared/global layer

Do not attempt a big-bang split of the 4,218-line file. Concretely:
1. New components going forward get their own `Component.module.css`.
2. `App.css` keeps everything already there, *plus* the genuinely global concerns (RTL overrides, dark-mode `[data-theme]` variables, the design-token custom properties) — these are correctly global today and should stay global even after modularization, per the frontend audit's own observation that splitting them into per-module files would mean re-solving how cross-cutting RTL/dark-mode rules attach to module-scoped class names.
3. Existing rules migrate to their component's module file only when that component is next meaningfully touched — the same "gradual, feature-triggered" migration discipline as §17.1.

**Why not Tailwind/a UI kit instead**: introducing a full utility-CSS framework or component library at this stage would touch every existing page for zero functional gain — pure churn risk against a live, revenue-generating UI, for a problem (scoping/collision-safety) that CSS Modules solves with dramatically less blast radius. Revisit only if the founder specifically wants a visual-design-system overhaul, which is an explicit product decision, not an architecture one.

### 17.4 Turn on `strict` in both tsconfigs

Do this as its own, isolated PR: set `"strict": true` in `tsconfig.app.json` and `tsconfig.node.json`, then fix whatever the compiler now flags (expect the non-null assertions already present in `ProductPage.tsx` and similar files to need real narrowing, not just silencing). This is the cheapest, highest-value type-safety fix available (§5.4) — do it before the codebase is meaningfully bigger, since every file added after this point without `strict` on is a file that will need retrofitting later at higher cost.

### 17.5 Route-based code splitting

`React.lazy(() => import('../pages/AdminDashboardPage'))` (and the other 3 admin pages) wrapped in a single `<Suspense>` at the admin route boundary in `App.tsx` — the cheapest, most surgical version of §5.6's fix, targeting exactly the pages least likely to be needed by a typical visitor.

---

## 18. Recommended Database Architecture

1. **Resolve the SQLite-vs-Postgres question first** (§6.2, §22.2) — every other database recommendation is downstream of knowing what's actually running in production today.
2. **Add a unique index migration** for `Product.NormalizedKey` (both SQLite and Postgres migration pairs, maintaining the existing dual-migration convention) — pair with the `ProductLookup` consolidation (§16.3) so the race-condition fix and the duplication fix land together.
3. **Add an index on `Store.Name`** appropriate to the eventual database engine (a Postgres expression index on `lower(name)`, or a normalized-key column mirroring `Product.NormalizedKey`'s existing pattern — the latter is more consistent with how the codebase already solves this exact problem for Products, and is the recommended choice).
4. **Wire up or drop `Category`/`Offer`** (§6.3) — a decision, not just a migration; if category browsing is meant to grow beyond the static curated-catalog JSON (which the roadmap's "Product Search Engine" phase implies it should), these tables are the natural home and should be wired up as part of that phase (§30 Phase 2), not left dormant.
5. **Introduce keyset pagination** on the four unbounded list endpoints (§6.3, §7.2) as part of whichever roadmap phase first gives one of those tables (most likely Reviews) real production volume.
6. **New tables for new features, following the existing entity conventions** (no FK constraints at the DB level, matching the codebase's established convention of enforcing referential rules in application code — confirmed as a deliberate, consistent choice across every existing entity, not an oversight): `Conversation`/`Message` for the AI assistant (§8.3, §20), `SearchQueryLog` for search analytics (§9.3, §21).

---

## 19. Recommended API Architecture

1. **Version now, before a second consumer exists.** The cheapest option given the current shape: a URL-prefix scheme (`/api/v1/...`), introduced as a redirect/routing change with zero controller-logic changes — every existing controller keeps working, `/api/` (unversioned) can alias to `/api/v1/` during the transition. Do this *before* the mobile app (§28) or any public API consumer exists, not after — versioning a stable API with real clients is a different, much more expensive exercise than versioning one with a single first-party consumer that's straightforward to update.
2. **Typed error contract**: replace the anonymous `{ error: "..." }` shape with a small `ApiError { code: string, message: string }` DTO shared across every controller — cheap, and it's the shape a future generated TypeScript client (from the OpenAPI/Swashbuckle document that already exists) would want anyway.
3. **Runtime contract validation on the frontend** (Zod schemas mirroring `api/types.ts`, validated at the `authFetch`/raw-`fetch` boundary in `client.ts`) — this is the concrete fix for §5.2/§14 item 12, and it should be introduced *alongside* API versioning, since a versioned API is exactly what makes "the frontend can trust this shape until it opts into v2" a meaningful guarantee.
4. **Rate limiting** on `/api/search` and `/api/search/by-image` specifically (the two endpoints that fan out to paid external APIs) via ASP.NET Core's built-in `Microsoft.AspNetCore.RateLimiting` middleware (.NET 8 native, no new dependency) — a per-IP fixed-window or token-bucket policy is sufficient to start; this is both a security fix (§10.2) and a direct cost-control measure.
5. **Standardize pagination** as a shared `PagedResult<T> { Items, Page, PageSize, TotalCount }` envelope, applied to the four unbounded list endpoints as they're touched for other reasons (§18.5) rather than as a single big-bang migration.

---

## 20. Recommended AI Architecture

Given §8's findings, the concrete shape:

```
Zaynor.Application/AiAssistant/
  IAiAssistantProvider.cs      — interface: SendMessageAsync(conversationId, userMessage) -> AssistantResponse (streamable)
  IConversationStore.cs        — interface: append/read Conversation+Message history
  AiAssistantService.cs        — orchestration: reads conversation, calls IAggregationService for any product lookups
                                  the assistant's reasoning requires, calls IAiAssistantProvider, persists the turn

Zaynor.Infrastructure/AiAssistant/
  OpenAiAssistantProvider.cs   — (or Anthropic/Azure OpenAI — vendor choice is a business decision, not an
                                  architecture one; the interface boundary is what matters) config-gated
                                  exactly like every existing IProductDataSource ("dormant until configured")
  ConversationStore.cs         — EF-backed, using the same DbContext, no new database engine
```

**Why this shape, concretely referencing the existing codebase**: `AiAssistantService` depends on `IAggregationService` — the *exact same interface* `SearchController` already depends on — so the assistant can never see or recommend a product the real aggregation engine didn't actually find. This is not a generic best practice; it is the direct, concrete answer to a specific risk this document has to name plainly: an LLM-backed assistant that's allowed to describe products or prices from its own training data (instead of exclusively from live `IAggregationService` results) would fabricate offers, which is a direct, brand-destroying violation of the "never invent data" principle this codebase has otherwise upheld consistently throughout every data source built during this engagement (every `IProductDataSource` implementation explicitly fails soft to *no result* rather than ever guessing). The architecture must make fabrication structurally impossible, not just discouraged by prompt instructions — which means the assistant's tool-use surface should be `IAggregationService.SearchAsync` and nothing else for factual product/price claims.

**Cost control**: `IAiAssistantProvider`'s config includes per-request token ceilings, and every turn is recorded in an `AiInteraction` append-only table (mirroring `ClickEvent`'s existing shape) — cost dashboards and abuse detection both fall out of this for free later (§24).

**Confidence, not just correctness.** Two independent proposals for this project both raised "confidence scoring" as a requirement — worth taking seriously specifically because `AggregationService`'s search results already vary in how trustworthy they are (a `RainforestAmazonDataSource` hit with a real ASIN and price is a stronger signal than a `CuratedProductDataSource` fallback match on a fuzzy keyword score, §4.2). `AssistantResponse` should carry a `Confidence` field derived from *real, already-available signals* — how many independent sources agreed, whether the match came from an exact vs. fuzzy product-name match (`CuratedProductDataSource.Score`, §4.2 already computes and discards this exact number today), how recent the price data is — not a separate ML model to build. Below a threshold, the assistant asks a clarifying question instead of answering ("did you mean the 256GB or 512GB model?") rather than guessing — this is the same "never fabricate, fail soft to no answer" discipline already proven throughout every `IProductDataSource`, applied to the assistant's *reasoning* instead of its *data*.

**What's explicitly deferred, and why**: a vector database, RAG over documentation/FAQs, and a standalone model-router (auto-selecting cheap vs. expensive models per query complexity) are all reasonable *eventual* ideas that both proposals for this project raised — none are needed for Phase 3 (§30) to ship a genuinely useful assistant. `IAiAssistantProvider` as an interface already means swapping or adding a second provider later is a config change, not a redesign (the same "dormant until configured, vendor-agnostic" pattern already proven five times over in `Zaynor.Infrastructure/DataSources/`) — so none of this is foreclosed, it's sequenced behind actually having a first assistant in production to learn from, per this whole document's operating principle of not building speculative infrastructure ahead of a demonstrated need (§12.2).

**Prompts are code, not string literals.** One genuinely worth-adopting idea from the proposals: system/tool prompts for `IAiAssistantProvider` should live as version-controlled files (e.g. `Zaynor.Infrastructure/AiAssistant/Prompts/*.md`, each with an explicit Role/Context/Rules/Output-format structure), not inline C# string literals — the same reason `curated-catalog.json` is a tracked file and not a hardcoded array (`Zaynor.Api/curated-catalog.json`, already an established pattern in this codebase for "content that changes independently of code logic"). This makes prompt changes reviewable in a diff, and testable the same way `GoogleShoppingDataSourceTests.cs` tests real observed query/response pairs (§26.1) — a prompt-regression test is just another fixture-based test against a fixed input/expected-output pair.

---

## 21. Recommended Search Architecture

1. **Keep live aggregation as the search mechanism** — do not introduce Elasticsearch/a vector DB to replace it (§9.3's reasoning stands). 
2. **Introduce `SearchQueryLog`** (append-only: query text, normalized query, result count, which sources contributed, timestamp) written fire-and-forget from `AggregationService.SearchAsync`, mirroring the exact fire-and-forget pattern already proven for price-history recording (§11.1) — same mechanism, applied to a new, smaller table. This single table is what turns "search quality improvement" from reactive (a user reports a miss) to proactive (a weekly query against this table surfaces the top zero-result searches to prioritize).
3. **Formalize the relevance-filtering heuristics** (`IsRelevant`/`IsAccessoryMismatch` in `GoogleShoppingDataSource`) into a named, independently-testable `Zaynor.Application/Search/RelevanceFilter.cs` — today this logic is private to one data source; as more connectors are added (§16.2), each one will likely need the same accessory-mismatch/relevance filtering, and it should be shared, not re-invented per connector the way the `IProductDataSource` skeleton itself was (§4.2).
4. **Category browsing converges with search** once `Category`/`Offer` are wired up (§18.4) — category pages become "a saved/curated search," not a separate static-JSON code path, which is what actually lets "Product Search Engine" (the roadmap phase name) become one coherent system instead of two.
5. **Progressive/streamed results** (§11.2's latency finding) — a longer-term option once the connector count grows enough that "wait for the single slowest source" becomes a real UX cost: stream cheap-source results immediately, patch in live-source results as they complete (Server-Sent Events, the same primitive recommended for the AI assistant in §8.3 — one piece of new infrastructure serving two roadmap items).

---

## 22. Deployment Architecture

### 22.1 Current state (accurately described, not idealized)

Single Docker image (root `Dockerfile`), building frontend (Vite, static output baked into `wwwroot`) and backend (published .NET binary) into one container, deployed to Render's free tier via a GitHub Actions workflow that builds, pushes to GHCR, and calls Render's deploy-trigger API. `docker-compose.yml` + the split `backend/Dockerfile`/`frontend/Dockerfile` exist purely for local full-stack development and are not part of the production path.

### 22.2 The one urgent action item in this entire document

**Verify, this week, whether production data is actually durable.** Concretely: log in as the existing admin account, note the exact review/ticket/saved-product counts, trigger a redeploy (a normal `git push`), and check those counts again immediately after. If they reset, production is running on ephemeral SQLite and every deploy is silently destroying user data — this needs a real Postgres instance (Render itself offers one, or Supabase/Neon per `render.yaml`'s own comment) wired via `ConnectionStrings__Default`, with the existing `PostgresZaynorDbContext` and its migration history exercised against it *before* the next deploy, not after. If the counts persist, the real production connection string has already been manually set in Render's dashboard and diverges from the committed `render.yaml` — in which case, update `render.yaml` to match reality (or document the divergence explicitly in the new `docs/DEPLOYMENT.md`) so the infrastructure-as-code stops being misleading about what's actually running.

### 22.3 Recommended, sequenced changes

1. Resolve §22.2 first — everything else here assumes durable storage exists.
2. Add a staging environment (a second, free-tier Render service pointed at a separate database) so the CI test gate (§23) has somewhere to deploy *before* production, not just a local `dotnet test` run.
3. Document the three-Dockerfile split in `docs/DEPLOYMENT.md` (§3.1, §15) so it stops being tribal knowledge.
4. Only after the above: consider a CDN in front of static frontend assets (currently served same-origin from the .NET app's `wwwroot`) — genuinely useful once traffic justifies it, premature before.

---

## 23. CI/CD Architecture

### 23.1 The gap, stated plainly

`.github/workflows/deploy.yml` today: checkout → Docker build → push to GHCR → trigger Render deploy. **No test step exists in this pipeline.** Every one of the ~15 backend commits made during this engagement was manually verified locally (`dotnet build && dotnet test`, `npm run build`) *before* being pushed — the pipeline itself would have deployed any of them regardless of whether tests passed, because it never asks.

### 23.2 The fix

Add a `test` job to `deploy.yml` that runs `dotnet test` (backend, 109 tests today) and `npm run build` (frontend — this is already a type-check-then-bundle command per `package.json`'s own `"build": "tsc -b && vite build"`, so it doubles as a TypeScript gate for free) as a **required prerequisite** to the existing `build` job (`needs: test`), so a failing test or a TypeScript error blocks the Docker build/deploy entirely. This is a small, additive workflow change — it does not touch `Dockerfile`, `render.yaml`, or any application code, and it's the single highest-leverage CI change available, because it's the difference between "broken code can reach production" and "broken code cannot."

### 23.3 Sequenced next steps

- Add the architecture-layering test (§16.1) to the same `test` job once it exists, so a layering regression is caught the same way a logic regression would be.
- Introduce the staging environment (§22.3) so `main` deploys to staging automatically, and production deploys become a deliberate, separate promotion step (even a manual `workflow_dispatch`, matching the pattern `set-render-env.yml` already uses) rather than every push to `main` going straight to production — reasonable today given solo-founder velocity is a real asset worth protecting, but worth revisiting the moment a second contributor starts pushing to `main` directly.

---

## 24. Monitoring Architecture

Today: Render's own basic platform metrics (CPU/memory/request count) plus whatever `/api/health` (`HealthController`) and the `keepalive.yml` 10-minute cron ping provide — no application-level metrics (per-data-source latency/error rate, cache hit rate, search-result-count distribution) exist anywhere. Recommended, in priority order matching the roadmap:

1. **Structured, queryable per-source metrics** — the cheapest first step is logging (§25) done well enough to answer "which of the 5 sources is slowest/failing most" without needing a dedicated APM product yet; `AggregationService`/`CachedAggregationService` are the two natural instrumentation points (they already have `ILogger` injected).
2. **A real APM/metrics product** (Application Insights, given the ASP.NET Core-native integration path, or an OpenTelemetry exporter to any backend) once the query-analytics table (§21) and structured logs (§25) stop being sufficient to answer operational questions quickly.
3. **Uptime/error alerting** beyond the current `keepalive.yml` ping (which only proves the server responds to `/api/health`, not that search/auth/payments-adjacent flows work) — a synthetic check that actually exercises `/api/search` end-to-end would have caught the DataForSEO timeout bug (§4.2) automatically instead of requiring a user report.

---

## 25. Logging Architecture

Today: standard ASP.NET Core `ILogger<T>` throughout (confirmed consistently used across every service and data source — no `Console.WriteLine`/`Debug.Print` found anywhere), captured wherever Render's platform sends container stdout, with no structured-logging library (Serilog/NLog) and no log-aggregation destination (no Seq, no ELK, no Datadog). Recommended:

1. **Adopt Serilog with structured (JSON) output** — a drop-in replacement for the existing `ILogger<T>` calls (zero call-site changes needed, since Serilog implements the same interface), unlocking queryable fields (`SourceName`, `Query`, `DurationMs`) instead of grep-only text logs. This is the concrete prerequisite for §24.1's "which source is slowest" question being answerable in minutes instead of requiring a manual DataForSEO API test the way this engagement's own debugging session required.
2. **Ship logs somewhere queryable** (even a free-tier log-aggregation service) once structured logging exists — Render's own log retention/searchability is limited, and this was directly felt during this engagement (diagnosing the DataForSEO empty-results bug required direct, manual API probing specifically because production logs weren't queryable enough to just check what exception the live code was actually throwing).

---

## 26. Testing Strategy

### 26.1 Backend — extend what already works

109 tests pass today, and the pattern (xUnit, hand-rolled fakes for `IHttpClientFactory`/`IProductDataSource`, a full `WebApplicationFactory` integration suite) is sound and should simply be extended to the gaps identified in §4.4: `AlertsController`, `CatalogController`, `AlertMonitorService`, and every currently-untested Infrastructure service. Add the architecture-layering test (§16.1) as its own category. No new testing framework or philosophy is needed — this is coverage-completion, not a testing-strategy change.

### 26.2 Frontend — currently zero, needs to start from scratch

No test runner, no component-testing library, no E2E tooling installed at all (§5.1). Recommended stack, chosen to match the existing Vite-based toolchain with minimal new configuration: **Vitest** (shares Vite's config/transform pipeline, near-zero setup cost) + **React Testing Library** for component tests, starting with the highest-value, lowest-effort targets: `client.ts`'s 31 functions (pure, mockable `fetch` calls — the exact kind of function unit tests are cheapest for) and the shared hooks (`useAuth`, `useTranslation`, `useRecentSearches`). Defer E2E (Playwright, already used throughout this engagement for manual verification and trivial to formalize into a real test suite) until the highest-traffic user flows (search, auth, saved-products) have unit/component coverage first — sequencing matters here the same way it matters for the product roadmap (`docs/PROJECT_SPECIFICATION.md` §11's argument applies just as well to test-suite construction order).

### 26.3 The CI gate ties both together

Neither of §26.1/§26.2 matters operationally until §23.2's CI gate exists — tests that exist but aren't required-to-pass-before-deploy are documentation, not a safety net.

---

## 27. Caching Strategy

### 27.1 Today

One layer: `CachedAggregationService` wraps `AggregationService` with a 5-minute `IMemoryCache`, keyed by normalized query, only caching non-empty results (§9's "zero-result queries never benefit from caching" finding is a deliberate, correct choice — caching a zero-result miss would mean a subsequently-fixed typo/connector still shows stale "no results" for up to 5 minutes).

### 27.2 Recommended evolution, sequenced with actual need

1. **Stay with `IMemoryCache` as long as the API runs as a single instance** (§12.1) — introducing Redis before horizontal scaling is actually needed would add an operational dependency (a service to provision, monitor, and pay for) with zero present benefit, directly contradicting the founding spec's own sequencing principle.
2. **The moment horizontal scaling is planned** (§12.1, §22.3's staging-environment step is a natural trigger for first noticing this need), swap `IMemoryCache` for a distributed cache (Redis, via `IDistributedCache` — a drop-in interface swap since ASP.NET Core already abstracts this) so cache hits are shared across replicas instead of each replica paying for its own cold cache.
3. **Consider a longer-lived cache tier for category/curated-catalog data** (§18.4) separately from the live-search cache — static/slow-changing data (the curated catalog, eventually `Category`/`Offer` if wired up) has a fundamentally different freshness requirement than live prices and shouldn't share a single 5-minute TTL policy with them.

---

## 28. Future Mobile Strategy

The founding spec (`docs/PROJECT_SPECIFICATION.md` §14, §19) already names React Native / .NET MAUI as the two natural options, reusing the same backend APIs. Given the frontend is already React (not Blazor), **React Native is the lower-friction choice concretely for this codebase**, for reasons specific to what already exists, not generically:

- The 4 Context+hook pairs (§5.1 — Auth, Theme, Language, Toast) are pure React and port to React Native with minimal change; only the Provider's storage layer (`localStorage` → `AsyncStorage`) needs swapping, and even that swap is small because nothing currently reads `localStorage` directly outside the 4 provider files and `token.ts` (§5's finding that persistence is hand-rolled per-concern, while a real cost today, becomes a small, enumerable list of exactly what needs touching for a mobile port).
- `api/client.ts`'s 31 functions are plain `fetch`-based and framework-agnostic — they port to React Native essentially unchanged (React Native's `fetch` is the same API).
- `App.css`'s 4,218 lines **do not port at all** — React Native has no CSS. This is the single largest mobile-readiness cost in the current architecture, and it's the strongest concrete argument for the §17.3 CSS Modules migration: components that move to scoped, co-located style modules are already halfway to being restyled with React Native's `StyleSheet` API (same component boundary, different style mechanism), whereas components styled by matching a global class name into a 4,218-line file have no clean seam to port from at all.
- This is **not an argument to rush the CSS migration for mobile's sake today** — mobile is explicitly a late-roadmap item (§30 Phase 8) — it's the concrete reason §17.3's migration should be genuinely gradual-but-real, not deferred indefinitely, since every component migrated to CSS Modules between now and Phase 8 is a component that doesn't need re-work twice.

**API versioning (§19.1) is a harder prerequisite for mobile than for the web SPA**: a web SPA redeploys atomically with its backend; a mobile app does not (app-store review latency, users on old versions) — meaning the API-versioning work in §19.1 should genuinely be *done*, not just planned, before the first mobile client ships, not merely "in progress."

---

## 29. Future Microservices Strategy

### 29.1 The honest answer: not yet, and not most of it, ever

§2.1 already states the modular monolith is correct today. This section exists to be specific about **which** future pieces (not "everything," not "nothing") eventually justify extraction, and by what trigger — because "microservices" as a goal-in-itself, absent a specific pain the monolith is actually causing, is exactly the kind of premature complexity this document's own principles (matching the founding spec's sequential-build argument) argue against.

### 29.2 Candidates, each with its own concrete trigger — not a timeline

| Candidate for extraction | Concrete trigger that would justify it | Why not now |
|---|---|---|
| `AlertMonitorService` (background worker) | Horizontal API scaling (§12.1) makes running it in-process on every replica actively wrong (duplicate checks), not just wasteful | Today it's one replica, one worker — correct and simplest as-is |
| AI Assistant (§20) | Token-cost/latency profile diverges enough from the rest of the API that independent scaling (more assistant capacity without scaling the whole API) becomes worth the operational cost of a second deployable | Nothing about the assistant's *design* (§20) requires this from day one — `AiAssistantService` can live in-process exactly like every other Application-layer service until real usage says otherwise |
| Search/Aggregation engine | Query volume grows enough that its own scaling curve (mostly I/O-bound waits on external vendor APIs, per §11.2) genuinely diverges from the rest of the API's (mostly DB-bound) scaling curve | Today it's the same process, and splitting it would only add a network hop with zero present benefit — revisit only alongside §21's progressive-results work, if that work reveals a real independent-scaling need |
| Notifications (if push notifications become part of §28's mobile work) | Push-notification delivery has a genuinely different reliability/retry profile than request-response APIs (it's fire-and-forget, needs its own retry/dead-letter handling) | Doesn't exist yet at all — build it in-process first (mirroring `AlertMonitorService`'s existing pattern), extract only if delivery-reliability requirements outgrow what an in-process worker can guarantee |

### 29.3 What this section is explicitly *not* recommending

Splitting the Admin Dashboard, Reviews, or Support Tickets into separate services. None of these have any resource-profile or team-ownership reason to be separate — they're exactly the kind of feature-area that Clean Architecture's existing folder-per-feature convention (§3, already applied consistently in `Zaynor.Application`) already serves correctly as-is, and splitting them would only add deployment/versioning overhead for zero benefit.

### 29.4 Event-driven architecture and a message bus: a trigger, not a starting point

Two independent proposals for this project's future both describe an event-driven core (`UserSearchedProduct`, `PriceDropped`, `AffiliateClicked`, etc., published to a bus — RabbitMQ/Kafka/Redis Streams/NATS all named as options) as foundational. It is worth naming plainly why that's premature *today* rather than silently disagreeing: Zaynor has one process and no consumer that isn't also the producer — every one of those example events today has exactly one interested reader (the same request that raised it), which means a message bus would add an operational dependency and a network hop for zero present decoupling benefit. The correct trigger for introducing one is concrete and checkable: **the day a second, independent process needs to react to something the API does**, without that process being in the API's own request path — e.g. `AlertMonitorService` (§29.2) genuinely becoming its own deployable, or a future analytics pipeline (§30 Phase 6) consuming search events asynchronously for reporting without slowing down the search request itself. Until that day, an in-process event convention (a plain C# event or a simple internal pub/sub, no external broker) captures the same *design* discipline — code that reacts to "a price dropped" instead of being called directly by whatever noticed it — without paying for infrastructure nothing yet needs. This mirrors §27.2's identical reasoning about Redis: the abstraction is worth designing for now; the infrastructure is worth deferring until something real needs it.

---

## 30. Enterprise Roadmap

Every phase below produces something real and shippable, per the founding spec's own explicitly-stated principle (`docs/PROJECT_SPECIFICATION.md` §11, §18) — no phase is "invisible infrastructure work with nothing to show," because that principle has already been validated once, by this exact project, and there's no reason to abandon it now.

### Phase 1 — Foundation (internal hardening, zero user-visible change)
Close the two layering leaks (§16.1); introduce `ExternalProductDataSourceBase` (§16.2) and migrate the 4 existing live sources onto it; add the `ProductLookup` helper + unique-index migration (§16.3, §18.2); **resolve the Postgres/SQLite durability question** (§22.2 — the single most urgent item in this document); add the CI test gate (§23.2). Ship criterion: same site, same behavior, verifiably safer underneath — 109+ backend tests still green, plus the new architecture-layering test passing.

### Phase 2 — Search Engine
Wire up `Category`/`Offer` (§18.4) so category browsing and search converge; extract `RelevanceFilter` as a shared, testable component (§21.3); introduce `SearchQueryLog` (§21.2). Ship criterion: category pages backed by the real database instead of static JSON; a queryable answer to "top 20 zero-result searches this week."

### Phase 3 — AI Shopping Assistant
Build `IAiAssistantProvider`/`IConversationStore`/`AiAssistantService` (§20), strictly dependent on the existing `IAggregationService` for all factual product claims. Ship criterion: a conversational assistant that can answer "find me X under Y SAR" using only real, live aggregation results — never fabricated data.

### Phase 4 — Affiliate Integration (expansion)
Apply §16.2's connector base class to onboard additional affiliate networks/stores beyond the current 5 sources — this phase is *cheaper* than it would have been without Phase 1's refactor, which is the point of sequencing it after. Ship criterion: Nth new store connector implemented in a day, not a week, because it's a config record + one `FetchAsync` method, not a hand-copied 150-line class.

### Phase 5 — Google Shopping Integration (formalized)
Google Shopping already exists today via SerpApi (`GoogleShoppingDataSource`) — this phase means evaluating direct/official Google Shopping Content API access as coverage and budget justify it, and applying §21.3's shared relevance-filter to whichever integration wins, rather than re-deriving accessory/relevance heuristics a second time.

### Phase 6 — Analytics
Build the query-analytics (§21.2) and click-analytics (already exists as `ClickEvent`, §6.3) data into an actual admin-facing dashboard view — the data collection is largely a byproduct of Phases 1-2; this phase is mostly about surfacing it.

### Phase 7 — Recommendation Engine
Only viable once Phase 2's `SearchQueryLog` and Phase 6's analytics have accumulated enough real usage data to recommend *from* — matches the founding spec's own honest constraint on predictive analytics (`docs/PROJECT_SPECIFICATION.md` §19: "requires months of accumulated PriceHistory data before it can work at all... a mathematical fact, not a preference"). Building this earlier would mean recommending from no data, which isn't a recommendation engine, it's a coin flip with extra steps.

### Phase 8 — Mobile Apps
Gate on: §19.1's API versioning genuinely shipped (not just planned — see §28's explicit reasoning for why this is a harder prerequisite for mobile than for the web SPA), and §17.3's CSS-Modules migration meaningfully underway (so component logic, not just API calls, has a clean seam to port). React Native (§28) reusing the existing `api/client.ts` and Context/hook layer nearly unchanged.

### Phase 9 — Scaling to Millions
Only the pieces actually blocked on user count, applied in the order real load reveals a need for them: distributed cache (§27.2) when horizontal replicas start; `AlertMonitorService` extraction (§29.2) at the same trigger; read replicas / connection pooling tuning once Postgres write/read load actually justifies it; CDN for static assets once traffic volume justifies the cost. None of these should be built speculatively ahead of that evidence — building them early is exactly the kind of premature-complexity risk this whole document, and the founding spec before it, argues against.

---

*This document is a companion to, not a replacement for, `docs/PROJECT_SPECIFICATION.md`. Where the two appear to disagree, the product vision in `PROJECT_SPECIFICATION.md` wins — this document exists to describe how to build toward that vision without the engineering debt compounding faster than the product grows.*
