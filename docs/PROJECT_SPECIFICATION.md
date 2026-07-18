# ZAYNOR — Complete Project Specification

**Smart Shopping Decisions**

Version 1.0 — Founding Document
Prepared for development kickoff in Visual Studio.

---

## Table of Contents

1. Executive Summary
2. The Problem
3. Founder's Full Vision
4. Product Concept & Core Value
5. Requirements Analysis (Functional & Non-Functional)
6. Scope Decisions (Agreed)
7. Core Engineering Principles (Non-Negotiable)
8. The MVP (Minimum Viable Product)
9. Data Acquisition Strategy (Heart of the Project)
10. Monetization Model
11. The Sequential Build Approach (and Why)
12. Revenue Journey
13. System Architecture
14. Recommended Tech Stack
15. Database Design (Entities)
16. Site Map & Pages
17. User Flow
18. Build Roadmap (5 Phases)
19. Future Vision (Deferred Features)
20. Affiliate Networks (Details & Registration)
21. Challenges & Risks (Honest Assessment)
22. Brand Identity
23. Getting Started in Visual Studio

---

## 1. Executive Summary

Zaynor is an intelligent, AI-assisted shopping companion that helps people make confident buying decisions. Instead of searching across dozens of stores and comparing prices manually, a user simply enters a product name — and Zaynor searches multiple stores, gathers the prices, finds the real lowest price, highlights the best deal, recommends the best option, and (in later versions) advises whether to buy now or wait.

Zaynor does not sell products. It sells **confidence in the buying decision**. Its promise: *never buy until you're sure you got the best price and the best value.*

The long-term vision is for Zaynor to become the **starting point of every purchase** — a trusted layer above all stores. Before going to any marketplace, users open Zaynor first to answer three questions: What is the right product? What is the best price? Should I buy now, or wait?

The name Zaynor blends the Arabic roots *zayn* (the best, the finest) and *noor* (light, clarity): the best choice, made clear.

---

## 2. The Problem

When a user wants to buy any product (a phone, laptop, monitor, headphones, a game console, etc.), they face:

- Scattered results across dozens of stores, with different prices for the same product.
- Uncertainty about whether the current price is actually good.
- Not knowing whether a better alternative exists at the same price.
- Fear of buying and later discovering they overpaid.

The user is essentially **lost between prices, stores, and products**. Zaynor solves exactly this problem.

---

## 3. Founder's Full Vision (In the Founder's Own Words, Organized)

This section preserves the founder's original intent, restructured for clarity.

- The idea exists in the market, but the goal is to **surpass existing competitors**.
- Start with a website that lets users **search for products**, then add **many categories**.
- Concrete example: when someone searches "Sony PlayStation 5" on Google, several results and prices appear. Zaynor should **take those results, filter them, organize them**, and act as a **financial assistant** to the customer.
- Show all search results for the product across stores. Then tell the user which store has the lowest price — e.g., "Sony PS5 is 5,000 in Noon and 4,000 in Amazon." Give a recommendation and a suggestion for the best price.
- Also show analytics — e.g., suggest that the user buy the PS5 in about a month because its price is expected to drop (this is a deferred feature; see Section 19).
- Enable **image-based search**: the user uploads a photo of any product, and the AI-powered site recognizes the product, then shows all search results, all prices, the lowest price, and a purchase recommendation (this is a deferred feature; see Section 19).
- Monetize the idea: earn income through **discount codes and affiliate marketing** from Amazon and other stores. The goal is to work with and earn affiliate commissions from **more than 10,000 approved stores** — and even more.
- The design must apply **design principles and usability**: comfortable, accessible, standards-compliant, and considerate of all people.

Additional founding principles emphasized by the founder:

- The code must be **clean, orderly, and professional** — not random or unclear AI output.
- The goal is an **official, credible platform** that competes in the market, attracts users by the millions, and is used daily.
- The platform should become **well-known and famous**.
- The founder committed to: **continuity** (finishing the journey even if long), **quality over speed**, and **growth** (reaching people after building).
- The founder's initial instinct: the site should not store prices but fetch them **live at search time**, take data (originally imagined from Google results) and **re-format and re-organize it**. (This instinct is correct as an "aggregator" pattern; the safe data source is stores directly, not Google — see Section 9.)

---

## 4. Product Concept & Core Value

Zaynor is a **real-time price aggregator and financial shopping assistant**. Its five capabilities:

1. **Search** for a product (by text now; by image later via AI).
2. **Gather and normalize** results for the same product across multiple stores.
3. **Identify the lowest price** and produce a clear recommendation ("Buy from Amazon at 4,050 instead of Noon at 5,000").
4. **Predictive analytics** ("Wait a month — the price will drop") — deferred.
5. **Earn revenue** via affiliate commissions and discount codes across many stores.

The whole experience is wrapped in a clean, comfortable, accessible UX.

**Core value statement:** A platform that helps the user buy the right product at the lowest possible price and the highest possible value — a **trust layer** above all stores.

---

## 5. Requirements Analysis

### 5.1 Functional Requirements (What the system does)

- **FR1 — Text search:** User enters a product name; system searches configured stores.
- **FR2 — Result aggregation:** System collects results for the same product from multiple stores.
- **FR3 — Product matching / normalization:** System recognizes that "iPhone 15" in Noon and "آيفون ١٥" in Amazon are the same product, and groups them.
- **FR4 — Price sorting:** Results are sorted from lowest to highest by default.
- **FR5 — Lowest-price identification:** System flags the cheapest offer.
- **FR6 — Recommendation engine:** System produces a clear textual recommendation (best deal, savings amount).
- **FR7 — Outbound store links:** "Go to best price" button links to the store (this link is the affiliate tracking link).
- **FR8 — Price-drop alerts:** User can subscribe to be notified when a product's price drops. (Introduced in the expansion phase.)
- **FR9 — User accounts:** Registration, login, saved products, alert preferences. (Expansion phase.)
- **FR10 — Categories:** Support multiple product categories, expanding over time toward "all products."
- **FR11 — Image search (deferred):** User uploads a product photo; AI identifies it and runs the normal search flow.
- **FR12 — Predictive analytics (deferred):** Buy-now-or-wait guidance based on historical price data.

### 5.2 Non-Functional Requirements (How the system behaves)

- **NFR1 — Performance:** Fast search and result rendering; live aggregation should feel responsive.
- **NFR2 — Scalability:** Architected from day one to grow toward millions of users.
- **NFR3 — Security:** Protect user data (accounts, alerts); secure handling of credentials and tokens.
- **NFR4 — Reliability / Availability:** Stable under load; graceful handling when a data source fails.
- **NFR5 — Bilingual support:** Arabic and English (RTL and LTR).
- **NFR6 — Usability & Accessibility:** Apply UX principles; accessible and comfortable for all users.
- **NFR7 — Maintainability:** Clean, layered, documented, testable code — easy to extend.
- **NFR8 — Legal compliance:** Respect store terms of service and affiliate network agreements; avoid disallowed scraping.

---

## 6. Scope Decisions (Agreed)

| Decision Area | Agreed Decision |
|---|---|
| Geographic scope | Saudi Arabia first; architecture designed to expand globally later. |
| Target audience | Anyone who wants to buy a product and is lost among prices/stores (broad audience). |
| Product scope | All products eventually — a "Google-for-prices" model — but expanded gradually. |
| Recommendation neutrality | Show the best store and recommendation **even if a store pays no commission** (neutrality first). |
| Image search | Deferred until text search works. |
| Predictive analytics | Deferred (needs months of historical price data first). |
| Build tool | Claude (assisting the founder), implemented by the founder in Visual Studio. |
| Build method | **Sequential** — build the core first with high quality, then expand layer by layer. |

---

## 7. Core Engineering Principles (Non-Negotiable)

1. **Clean Code** — organized in clear layers; readable by any developer; maintainable and extensible; documented. No random or unclear AI-generated code.
2. **Production-Grade Platform** — secure (protects user data), stable (does not break under load), scalable (handles millions).
3. **Scalability from Day One** — architectural choices are made assuming eventual millions of users. Scalability is planned early, built gradually.
4. **Growth & Brand** — the platform is meant to reach people and become well-known; growth strategy is considered from the start, executed later.

**Key insight:** These principles themselves prove that the sequential approach is correct. Clean code is impossible when everything is rushed at once. A platform that serves millions is built on a solid foundation first, then scaled. Building the core first, with quality, is the only path that achieves the founder's ambition.

---

## 8. The MVP (Minimum Viable Product)

The core, to be built first:

1. User searches for a product by name.
2. Zaynor gathers results for the same product from a few stores.
3. Prices are displayed sorted, with the lowest identified.
4. A clear recommendation is shown.
5. Outbound links carry affiliate tracking (revenue is built into the core).

Everything else (image search, predictive analytics, expanded affiliate coverage, mobile app) comes after, in sequence.

---

## 9. Data Acquisition Strategy (Heart of the Project)

This is the single most important technical decision. There are four ways to obtain product prices:

### 9.1 Official Store API (cleanest)
Some large stores provide an official gateway to request product and price data in a structured way.
- **Pros:** accurate, legal, stable, fast.
- **Cons:** few stores offer it; may require approval or partnership. (Example: Amazon's Product Advertising API requires an active affiliate account.)

### 9.2 Affiliate Network Feeds (best starting point) — RECOMMENDED
Instead of dealing with each store individually, subscribe to a network (e.g., ArabClicks, Admitad). The network provides **product data feeds** — products, prices, and affiliate links — from **dozens of stores at once**.
- **Pros:** solves data + revenue together; legal; organized.
- **Cons:** requires acceptance into the network; product coverage depends on participating stores; feeds may refresh every few hours rather than by the second.

### 9.3 Web Scraping
A program reads a store's product page and extracts the price.
- **Pros:** can reach any store even without an API.
- **Cons:** fragile (any store redesign breaks it), slow, and sometimes **violates a store's terms of service**; some sites block scrapers.

### 9.4 Manual Entry / Direct Partnerships
Agree with specific stores to receive prices directly, or enter them manually early on.
- **Pros:** accurate, guaranteed.
- **Cons:** does not scale.

### 9.5 The "Aggregator" Pattern (the founder's correct instinct)
The founder's instinct — *don't store prices; fetch them live at search time and re-organize them* — is a real engineering pattern called an **aggregator**:

1. User searches "Sony 5".
2. Zaynor sends the request, in real time, to several sources (affiliate feeds + store APIs).
3. It receives the results, **unifies** them, **sorts** them, and identifies the lowest.
4. It displays them with a recommendation.

Same user experience the founder imagined (live + re-ranked), but the source is trusted stores instead of Google.

### 9.6 Why NOT scrape Google results
- **Google does not own the prices** — it is itself an intermediary that fetches/receives data from stores. Taking from Google adds a weaker, second-hand layer.
- **Scraping Google results is explicitly prohibited** by Google's terms, and Google actively detects and blocks it — legally and technically unsafe as a foundation.
- The official alternatives (e.g., Google Shopping Content API) are designed for merchants listing their products, not for building a comparison engine on top, and are typically costly or restricted for this use case.

**Conclusion:** Take data from the **same source Google takes from** — the stores directly, via affiliate networks and APIs.

### 9.7 Data Acquisition Rollout
1. Start with **ArabClicks + Amazon Associates** (covers the largest stores) via feeds.
2. Use feeds as the initial price source **and** the affiliate links.
3. Add official APIs where available.
4. Add scraping only where necessary for stores not otherwise reachable, carefully and within terms.
5. Expand networks (Admitad, Awin, etc.) to widen coverage.

Comparison summary:

| Method | Ease | Cost | Gives Commission? | Best for Start |
|---|---|---|---|---|
| Affiliate network feed | Medium | Low | Yes | Strongly recommended |
| Official API | Medium | Low | Sometimes | Recommended |
| Web scraping | Hard | Medium | No | Last resort |
| Manual / partnership | Easy | High (time) | Negotiable | Testing only |

---

## 10. Monetization Model

Zaynor does not sell products. Revenue sources, in order of importance for the roadmap:

1. **Affiliate commission (built into the core):** The same "Go to store" button the user clicks is the affiliate tracking link. When the user buys, the store pays a commission via the network. This works from the very first version — no separate phase needed.
2. **Premium subscription (the strategic, independent income):** Free tier for everyone; paid tier with instant price alerts, unlimited product tracking, advanced analytics and reports. This income does not depend on any single store and is the hardest for competitors to copy.
3. **Featured store placement (later):** Stores pay to appear prominently once the platform has significant traffic.
4. **Market data & analytics (long-term):** Sell aggregated market insights (most-searched, most-bought, average prices, trends) to companies once large-scale data accumulates.

**Honesty note:** Early affiliate income is modest (few users = little commission). Large numbers come with growth. What matters is that the earning mechanism works from day one, and every new user increases income. The commission formula:

commission = number_of_users × purchase_rate × average_commission

Because revenue is tied to real users, and users only arrive after launch, **launching earlier means earning earlier** — the sequential approach reaches revenue faster, not slower.

---

## 11. The Sequential Build Approach (and Why)

The platform is built **in sequence**, not all-at-once. The founder initially preferred "all features at once"; the engineering reasoning against that:

1. **Integration complexity explodes:** Building 5 interdependent features at once multiplies failure points non-linearly. A problem in image search could block the entire delivery even though text search is ready.
2. **Some features depend on data you don't yet have:** Predictive analytics ("price will drop next month") is impossible on day one because it needs months of price history. The data itself does not exist yet — this is a mathematical fact, not a preference.
3. **No feedback = building blind:** Launching after a year risks discovering users wanted something different. Early launch of the core yields real data that guides the rest of the build.
4. **Risk of collapse before launch:** Most large "build everything at once" projects never ship — time or motivation runs out first.

**The resolution:** Build all the features — but **in the right order**, not in one batch. The difference is not ambition; it is sequencing. Even Google and Amazon have all their features today, but each started with one.

**Additionally, sequencing is imposed by the system itself:** affiliate networks require a working platform before they accept you, so you cannot launch "everything at once" even if you wanted to. Gradual building is mandatory, not optional.

---

## 12. Revenue Journey

- **Stage 1 (Foundation + Commission):** modest income begins via affiliate links embedded in the core.
- **Stage 2 (Store Expansion):** income grows with users.
- **Stage 3 (Expanded Affiliate):** good income as coverage and traffic grow.
- **Stage 4 (Subscriptions):** steady, store-independent recurring income.

Revenue is tied to user growth, not feature count — reinforcing the sequential approach: launch fast → get users → earn → reinvest in expansion.

---

## 13. System Architecture

High-level, layered architecture (aggregator model):

```
[ User (Browser / Mobile App) ]
              |
              v
[ Frontend / UI Layer ]            <- search box, results, recommendation, alerts
              |
              v
[ Backend / API Layer ]            <- request handling, business logic
              |
     +--------+---------+
     |                  |
     v                  v
[ Aggregation Service ] [ User & Data Services ]
     |                        |
     |  (live, per-search)    |  (accounts, alerts, saved products)
     v                        v
[ Data Sources ]          [ Database ]
  - Affiliate feeds
  - Store APIs
  - (later) scrapers
```

Key architectural ideas:

- **Aggregation Service:** on each search, calls multiple sources, normalizes and merges results, ranks by price, and returns the recommendation. Stateless per request where possible.
- **Product Matching / Normalization:** logic (and later AI/ML) to determine that different store listings refer to the same product.
- **Caching layer:** to keep the experience fast and reduce repeated source calls (feeds may update every few hours, so short-lived caching is acceptable).
- **User & Data Services:** accounts, saved products, alert subscriptions.
- **Background jobs (expansion):** periodic price checks to power price-drop alerts and accumulate history for future analytics.
- **Separation of concerns:** clear boundaries between UI, business logic, aggregation, and persistence — this is what makes clean code and future scaling possible.

---

## 14. Recommended Tech Stack

The founder has basic programming knowledge and will implement in Visual Studio. Recommendations favor clean structure, strong ecosystem, scalability, and good bilingual/RTL support. Final choices are the founder's; these are sensible defaults.

**Note:** Verify current versions and availability at implementation time, since tooling evolves.

### Option A — Modern, scalable, widely-supported (recommended)
- **Frontend:** React (or Next.js for SEO — important for a discovery platform) with a component library; full RTL/Arabic support.
- **Backend:** Node.js (NestJS for clean, layered architecture) or ASP.NET Core (excellent in Visual Studio, strongly typed, great for clean architecture).
- **Database:** PostgreSQL (relational, reliable, scales well).
- **Caching:** Redis (fast, for live search caching and alerts).
- **Hosting:** a cloud provider that scales (start small, grow).

### Option B — Microsoft-centric (natural in Visual Studio)
- **Frontend:** Blazor or React.
- **Backend:** ASP.NET Core Web API (C#).
- **Database:** SQL Server or PostgreSQL.
- **ORM:** Entity Framework Core.

Given the founder uses Visual Studio, **ASP.NET Core + C# + Entity Framework Core + PostgreSQL/SQL Server**, with a **React or Blazor** frontend, is a clean, professional, well-documented, and scalable choice.

Cross-cutting:
- **Version control:** Git (GitHub).
- **Testing:** unit + integration tests from early on (supports the clean-code principle).
- **API style:** REST (well-understood, easy to consume by both web and future mobile app).
- **Mobile (later):** native iOS (Swift) or cross-platform (React Native / .NET MAUI) reusing the same backend APIs.

---

## 15. Database Design (Entities)

Initial core entities and relationships (to be refined):

- **Store** — id, name, logo, base_url, affiliate_network, is_active.
- **Product** — id, canonical_name, category_id, brand, model, normalized_key (for matching), image_url.
- **Category** — id, name, parent_category_id.
- **Offer** — id, product_id (FK), store_id (FK), price, currency, product_url (affiliate link), in_stock, shipping_info, last_updated.
- **User** — id, email, password_hash, created_at, locale.
- **Alert** — id, user_id (FK), product_id (FK), target_condition (e.g., price drop), is_active, created_at.
- **SavedProduct** — id, user_id (FK), product_id (FK), saved_at.
- **PriceHistory** (for future analytics) — id, product_id (FK), store_id (FK), price, recorded_at.

Relationships (summary):
- A Product has many Offers (one per store).
- A Store has many Offers.
- A User has many Alerts and SavedProducts.
- PriceHistory accumulates over time to enable future predictive analytics.

Use an ERD tool to formalize this before coding. Design for normalization first; optimize later as scale demands.

---

## 16. Site Map & Pages

- **Home** — prominent search box, value proposition, popular/last searches, categories.
- **Search Results** — the aggregated offers for a product, sorted by price, with the recommendation banner, "Go to best price" and "Notify me if price drops" actions.
- **Product Detail** (optional early) — full offer list, price history (later), alternatives.
- **Categories** — browse by category.
- **Account** — login/register, saved products, alerts, settings (expansion).
- **Alerts** — manage price-drop notifications (expansion).
- **About / How It Works / Privacy Policy** — needed for credibility and affiliate-network approval.

---

## 17. User Flow

1. User lands on Home and sees a clear, prominent search box.
2. User types a product name (e.g., "Sony PlayStation 5").
3. Zaynor aggregates offers live from configured sources.
4. Results appear sorted lowest-to-highest, with the lowest flagged.
5. A **recommendation banner** summarizes the best deal and the savings.
6. User clicks "Go to best price" (affiliate link) to buy, or "Notify me if price drops" to set an alert.
7. (Expansion) Registered users save products and manage alerts.

---

## 18. Build Roadmap (5 Phases)

**Phase 1 — Foundation & Design**
Finish requirements analysis, design the database, and design all interfaces/wireframes. No code yet — planning only. (Current phase.)

**Phase 2 — Build the Core**
Build Zaynor's heart: search box, price aggregation engine, sorting, and recommendation. Code is written here with quality and cleanliness.

**Phase 3 — Launch (MVP)**
Launch a real version with one or two stores, register in an affiliate network, and acquire the first users. Revenue begins here.

**Phase 4 — Expansion**
Add more stores, more categories, price-drop alerts, and user accounts.

**Phase 5 — Intelligence & App**
Image search (AI), predictive analytics, and the iOS app.

**Then — Growth toward millions:** marketing, distribution, and a global web + app expansion.

Every phase builds on the previous one and produces something usable — no building for a year with no result.

---

## 19. Future Vision (Deferred Features)

These are all part of the plan, built later in sequence:

- **Image-based search (AI):** user uploads a product photo; AI recognizes it and runs the standard search flow. Powerful but complex and costly; deferred until text search is solid.
- **Predictive analytics ("buy now or wait?"):** requires months of accumulated PriceHistory data before it can work at all. Deferred; a simpler version may be explored first.
- **Full personal shopping assistant:** e.g., "Find me the best laptop for university under 3,000 SAR" → best options, best prices, best value.
- **iOS app (and beyond):** built after the web has real users and push notifications matter for alerts; reuses the same backend APIs.
- **Global expansion:** the architecture is designed from day one to support this later.

---

## 20. Affiliate Networks (Details & Registration)

### 20.1 What an affiliate network is
An intermediary between you and thousands of stores. Instead of dealing with each store individually, you register once with a network already partnered with many stores. It gives you:
- **Tracking links** — your unique link; if someone buys through it, you earn a commission.
- **Product feeds** — lists of store products, prices, and links (your data source).

### 20.2 How commission works
1. User searches on Zaynor and sees the cheapest offer.
2. User clicks the button (an affiliate tracking link).
3. User buys at the store.
4. The store pays commission to the network; the network pays your share.

The same "Go to store" button that serves the user is the revenue mechanism — fully integrated.

### 20.3 Key networks for the Saudi market
- **ArabClicks** — strongest for the Middle East; aggregates Noon, Namshi, Amazon, and others; Arabic dashboard; built for the Gulf market. Likely your starting point.
- **Admitad** — large global network operating in the region.
- **Amazon Associates** — Amazon's own program; important since Amazon is a major store; note Amazon is strict and typically requires a minimum number of qualifying sales within a trial window (often ~180 days) or the account may be closed.
- **Noon Affiliate** — Noon's program (directly or via ArabClicks).
- **CJ / Awin / Impact** — large global networks, useful when expanding globally later.

### 20.4 One network solves two problems
The same network provides both the **data** (feeds with prices) and the **revenue** (affiliate links) — which is why it is the ideal starting point.

### 20.5 Registration & approval process
1. **Create an account** as a Publisher / Affiliate.
2. **Add your platform** (website/app URL) and describe your content (a price-comparison platform).
3. **Provide payment & identity details** for payouts and verification.
4. **Review & approval** — hours to days; may request more info.
5. **Store-level approval** — network approval ≠ store approval; some stores (e.g., Amazon) require separate approval.

### 20.6 What they require for approval (be prepared)
- A **working website or app** (not just an idea).
- **Clear content** — they understand what you offer and how you promote their products.
- **Traffic** (sometimes) — some require a minimum; others are lenient with beginners.
- **Clean content** — nothing prohibited or misleading.
- Amazon specifically: often a **trial with a required number of sales within ~180 days**.

### 20.7 The chicken-and-egg problem (and its solution)
The network wants a working site, but your site needs the network as a data source. Solution (which again proves sequencing is necessary):
1. Launch a **simple version first** — using one or two stores via their direct programs, or temporary manual data.
2. Build it as a **real, professional-looking platform** with content so networks see a serious publisher and accept you.
3. After acceptance, use network **feeds** to expand to thousands of products.

### 20.8 Practical tips to get approved
- Make Zaynor look **professional** (name, logo, identity are ready).
- Add essential pages: **About, Privacy Policy, How It Works** — networks like to see these.
- In the application, describe Zaynor as a **price-comparison platform that sends serious buyers to stores** — a value they appreciate.
- Verify each network's current terms (rates, limits, procedures) at application time, since they change frequently.

---

## 21. Challenges & Risks (Honest Assessment)

**The idea is proven and the path is sound.** Price-comparison models exist and succeed globally (Trivago, Google Shopping, PriceRunner). This is an improvement on a proven model, not an unknown gamble — which lowers risk substantially. The technology (search, database, results display, accounts) is mature and well-documented.

Real challenges — all solvable:

- **Data challenge:** collecting prices from many stores is gradual, not instant. Solved via affiliate feeds, then expansion.
- **Product matching:** recognizing that listings from different stores are the same product needs smart logic. Built gradually.
- **Network acceptance:** requires a working platform first. Solved by launching a simple, professional version.
- **Competition:** existing players exist. Win via better experience (AI, recommendations, superior Arabic-first design).
- **Growth to millions:** depends on marketing and continuity, not just building.

**Where the real risk lies (in the founder's hands, not technical):**
- **Continuity** — finishing the journey if it becomes long. (Founder committed: yes.)
- **Correct execution** — building with quality, not rushing. (Founder committed: yes.)
- **Growth** — reaching people after building. (Founder committed: yes.)

The risk is in execution and persistence, not in technical feasibility. Similar projects have succeeded; there is no technical reason this cannot, given correct, foundational building.

---

## 22. Brand Identity

- **Name:** ZAYNOR / زينور — from *zayn* (the best/finest) + *noor* (light/clarity) = "the best choice, made clear."
- **Logo:** the letter Z built from the project's ideas (price tag, checkmark, search lens, shopping cart, rising analytics bar), with a professional light/dark/green icon system and a rounded-square app icon.
- **Primary color:** Zaynor Green — `#1D9E75`.
- **Dark shades:** `#04342C` and `#0F6E56`.
- **Light background:** `#E1F5EE`.
- **Secondary / action accent:** warm orange `#D85A30` (for key buttons and price-drop alerts).
- **Slogan:** SMART SHOPPING DECISIONS.
- **Feature icons (for site/app):** Trust, Intelligence, Savings, Discovery, Analysis, Alerts.

Note: keep a flat single-color version of the logo alongside the gradient version for small sizes (favicon) and print.

---

## 23. Getting Started in Visual Studio

A clean, professional starting sequence:

1. **Set up the repository:** create a Git repo (GitHub), add a clear README (you can adapt this document), and set a sensible `.gitignore`.
2. **Choose the stack** (recommended: ASP.NET Core Web API + C# + Entity Framework Core + PostgreSQL/SQL Server, with a React or Blazor frontend).
3. **Create the solution structure with clean architecture:**
   - Presentation (API/UI)
   - Application (business logic, services)
   - Domain (entities, core models)
   - Infrastructure (database, external data sources / aggregators)
4. **Model the database** (Section 15) with EF Core migrations.
5. **Build the Aggregation Service interface first** with a mock data source, so the core search → aggregate → rank → recommend flow works end-to-end before wiring real feeds.
6. **Build the search UI** and results page (Section 16–17) against the API.
7. **Integrate the first real data source** (one store's direct program or a first affiliate feed).
8. **Add About / Privacy / How It Works pages** to prepare for affiliate-network applications.
9. **Apply for ArabClicks + Amazon Associates** once the platform is presentable.
10. **Write tests** as you go (unit + integration) to uphold the clean-code principle.
11. **Deploy the MVP**, acquire first users, and begin the sequential expansion (Section 18).

**Guiding rule throughout:** build the core cleanly first, keep the architecture layered and scalable, and expand one layer at a time toward the vision of a platform used by millions.

---

*End of specification. Nothing in the founder's vision is dropped — deferred features are explicitly scheduled, not removed. Build in sequence, with quality, toward the full vision.*
