# C45 — `D:\Prop\README.md` vs the repo

| Field | Value |
|---|---|
| Agent | C45 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\C45_readme_review.md` |
| Target | `D:\Prop\README.md` (49 lines, 1746 bytes) compared to the as-built tree |
| Law | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` (product intent). Architecture §66 does **not** require a root README. |
| Quality bar | Honest onboarding. Path names must exist. Behavioral claims must match code, not the goal. |
| Related | C11 (docs + PNG), C13 (§69 0/12), B38, B41 (ports), A75 (`.env.example`), A49 (flags), `docs/architecture.md` |
| Product source modified | **No.** This report is the only write. `D:\Prop\README.md`, `D:\Prop\src`, `D:\Prop\apps` were not edited. |
| Classification | **EXISTS_NEEDS_REFACTOR** |

---

## 0. Verdict

**`README.md` is a short lab onboarding stub. The paths it names exist. Several sentences describe the architecture goal as if it were the running system.**

Do not treat the README as proof of first useful version, live MT5 Manager ingest, live cTrader FIX, or a wired shadow-copy pipeline. `docs/architecture.md` is the more honest one-page map.

| Question | Measured answer |
|---|---|
| File exists at repo root | **Yes** — `D:\Prop\README.md` |
| Bytes / lines / SHA-256 | **1746** / **49** / `C023B4227A1C511F346A1FD96B6648B901001551731D1472FE7A33468B1A2764` |
| LastWrite | 2026-08-18 13:26:07 |
| Named component paths that exist | **7 / 7** (`apps/mt5-worker`, `apps/api`, `apps/fix-worker`, `src/Domain`, `src/Infrastructure`, `src/Fix.CTrader`, `apps/web`) |
| Named spec files that exist | **2 / 2** (v2 spec + `docs/architecture.md`) |
| Embedded diagram | **`docs/architecture.png` exists** and is a **Pillow placeholder**, not the sibling SVG |
| Run-recipe hosts that match default clients | **Yes** — API `:5000`, web `:3000` (B41 closed for the `http` profile) |
| Safety default `REAL_COPY_EXECUTION_ENABLED=false` on disk | **True** in `.env.example`, API `/api/settings`, `CTrader:RealCopyExecutionEnabled`, `CTraderFixOptions` default |
| Live `NewOrderSingle` send path | **Absent** — fail-closed by missing sender, not only by the flag |
| ~5,000 MT5 accounts in the demo | **No** — **4** canned logins |
| Shadow-copy pipeline hosted | **No** — `ShadowCopyEngine` is unused |
| First useful version (§69) | **0 / 12** (C13). README does not say §69 is done; it still *reads* like a working copy system. |
| Architecture v2 mentions `README.md` | **No** |
| Product source edited by this agent | **No** |

**Onboarding usefulness:** a developer can find the solution, start the API + Vite dashboard, and hit InMemory demo data.

**Accuracy:** the summary paragraph and the “~5,000 accounts / shadow / route to Pepperstone” sentence **overclaim**. The Safety and Native MT5 sections are the most truthful parts.

---

## 1. Method

| Step | Action |
|---|---|
| Read | Full `D:\Prop\README.md` (49 lines) |
| Census | PowerShell `Get-Item` + `Get-FileHash SHA256` on README, `.env.example`, `docs/architecture.{md,png,svg}`, v2 spec |
| Tree | `list_dir` on `D:\Prop`, `src/*`, `apps/*`, `docs`, `tests`, `mt5-sdk`, `services` |
| Cross-check | Solution, launchSettings, `vite.config.ts`, `Program.cs` (API + both workers), `DependencyInjection.cs`, `DemoSeeder.cs`, `FakeMt5BrokerConnector.cs`, `BaselineScorer.cs`, `CTraderFixOptions.cs`, `docker-compose.yml`, `.gitignore`, `.env.example` |
| Visual | `docs/architecture.png` (900×420 placeholder) vs `docs/architecture.svg` (real boxes) |
| Prior | C11 §6.2 (PNG provenance), B41 (ports), C13 (FUV) |
| Not done | No `dotnet run`, no `npm`, no HTTP probe, **no product edit** |

---

## 2. README as written (two documents in one file)

The file has **two H1 titles** and repeats the architecture pointer.

| Lines | Heading | Role |
|---|---|---|
| 1–18 | `# Trader Intelligence` | Component map + PNG + pointer to v2 + `docs/architecture.md` |
| 20–49 | `# MT5 XAUUSD Trader Intelligence + cTrader FIX 4.4` | Goal sentence, Safety, Run (demo), Native MT5 |

Line 19 is a second architecture pointer with no heading. A new hire sees two products, not one.

Full text for the record (verbatim, no rewrite):

```1:49:D:\Prop\README.md
# Trader Intelligence

Short architecture overview and where to find implementation details.

![Architecture](docs/architecture.png)

Summary: lightweight C#/.NET backend that ingests MT5 manager events, reconstructs trades, scores XAUUSD traders, shadow-copies approved trades and routes execution to a cTrader FIX 4.4 adapter.

Key components:
- **Ingest / Collectors:** `apps/mt5-worker`
- **API:** `apps/api`
- **Workers:** `apps/mt5-worker`, `apps/fix-worker`
- **Domain logic:** `src/Domain`
- **Persistence / Infrastructure:** `src/Infrastructure`
- **FIX adapter:** `src/Fix.CTrader`
- **Web dashboard:** `apps/web`

For the full architecture spec see `MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` and `docs/architecture.md`.
# MT5 XAUUSD Trader Intelligence + cTrader FIX 4.4

Identify copyable XAUUSD traders from ~5,000 MT5 accounts, shadow them, and only then (explicit flag) route risk-approved orders to Pepperstone/cServer FIX 4.4.

Architecture: `MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`

## Safety
- Real NewOrderSingle is **off** (`REAL_COPY_EXECUTION_ENABLED=false`).
- Trade #3 is early evidence, never LIVE promotion.
- Secrets stay in environment / `.env` (see `.env.example`). Never sent to React.

## Run (demo)
dotnet test / dotnet run API / npm install + npm run dev
API :5000, Dashboard :3000
Without Postgres → EF InMemory + Achiever + StarwaveFX seed

## Native MT5
`mt5-sdk` Windows-only local Manager API. Do not Linux-container the native DLL.
```

---

## 3. Claim-by-claim matrix

Tokens: **TRUE** (matches tree), **DEMO** (shape exists on fakes / InMemory), **OVERCLAIM** (goal written as current), **PARTIAL**, **FALSE**, **EDITORIAL**.

| # | README claim | Tree | Token |
|---|---|---|---|
| 1 | Title “Trader Intelligence” | Repo / sln / namespaces are `TraderIntelligence.*`. Web chrome says “MT5 Intelligence”. | **TRUE** (name) / **EDITORIAL** (two titles) |
| 2 | Image `docs/architecture.png` | File exists, 12081 bytes, 900×420, SHA `0F7BAF6D2461A5A055C83C278FCD0A8F718B3C2B86C19886221FDCB259EC98C9`. Visual: truncated pipeline line + **“Architecture diagram (placeholder)”**. Produced by `scripts/svg_to_png.py` Pillow fallback after cairosvg failed (C11). Sibling `docs/architecture.svg` is the real box diagram (SHA `23F51B89…`, 2697 bytes) and is **not** what README embeds. | **TRUE path / FALSE diagram** |
| 3 | “lightweight C#/.NET backend” | net8.0 solution, 10 product projects. | **TRUE** |
| 4 | “ingests MT5 manager events” | `DealIngestionService` + `apps/mt5-worker` loop. Connector is `FakeMt5BrokerConnector` registered in DI. No `DllImport` of `MT5APIManager64.dll`. No live Achiever/Starwave Manager session. | **DEMO** |
| 5 | “reconstructs trades” | `src/Domain/Reconstruction/TradeReconstructor.cs` used by `ReconstructionScoringService`. Integration test asserts completed XAUUSD trades after seed. | **DEMO** (algorithm real; input canned) |
| 6 | “scores XAUUSD traders” | `BaselineScorer` + persist via `ITradingStore.UpsertScoreAsync`. | **DEMO** |
| 7 | “shadow-copies approved trades” | `ShadowCopyEngine` exists (`src/Domain/Shadow/ShadowCopyEngine.cs`). Grep of product `*.cs`: **definition only**. Not in DI (`C05`). Seeder / workers / API do not call it. `ShadowOrders` table can stay empty; overview `ShadowPnl` sums that table. | **OVERCLAIM** |
| 8 | “routes execution to a cTrader FIX 4.4 adapter” | `src/Fix.CTrader` is options + `FixMessageParser` + `FixSessionOwnership` + `FixSimulationHarness`. **No** QuickFIX/n package. **No** `35=D` builder. `apps/fix-worker` stamps `LoggedOn` / `ReadyForMarketData` every 15 s and logs that NOS is disabled. | **OVERCLAIM** |
| 9 | Collectors = `apps/mt5-worker` | Project exists (`TraderIntelligence.Mt5Worker`). Hosted service syncs **fake** brokers every 30 s for 4 logins. | **TRUE path / DEMO behavior** |
| 10 | API = `apps/api` | `TraderIntelligence.Api.csproj` exists. Minimal APIs, Swagger in Development, CORS any-origin. | **TRUE** |
| 11 | Workers = mt5-worker + fix-worker | Both exist and are in the sln. | **TRUE** |
| 12 | Domain = `src/Domain` | Reconstruction, scoring, risk, shadow, execution, instruments, volume, entities, enums. Matches as-built better than architecture §66’s extra `/src/Scoring` folders (those folders were **not** created). | **TRUE** |
| 13 | Persistence = `src/Infrastructure` | `TraderDbContext`, `EfTradingStore`, `DemoSeeder`, InMemory/Npgsql switch. | **TRUE** |
| 14 | FIX adapter = `src/Fix.CTrader` | Folder + csproj exist. Not a live initiator. API project does **not** reference it. Fix-worker csproj references it but `Worker.cs` does not use FIX types. | **TRUE path / OVERCLAIM “adapter”** |
| 15 | Web = `apps/web` | Vite + React 18, 15 page files, port 3000. | **TRUE** |
| 16 | Spec files `…Architecture_v2.md` and `docs/architecture.md` | Both exist. v2 = 50966 bytes. `docs/architecture.md` = 1379 bytes, honest “what exists now” table. | **TRUE** |
| 17 | “~5,000 MT5 accounts” | `DemoBrokerFactory` seeds **3 Achiever + 1 StarwaveFX** accounts (logins `10001`, `10002`, `10003`, `99001`). Architecture §69 still wants ~5k. | **OVERCLAIM** (goal, not demo) |
| 18 | “shadow them, then (explicit flag) route risk-approved orders to Pepperstone/cServer FIX 4.4” | Intent of v2. No hosted shadow. No NOS. TargetCompID default `cServer` is correct. Live host/account appear in `.env.example` and seeder (`live-us-eqx-01.p.c-trader.com`, account `1369850`) but nothing connects. | **OVERCLAIM** |
| 19 | Real NOS **off** via `REAL_COPY_EXECUTION_ENABLED=false` | `.env.example` line 73 = `false`. `apps/api/appsettings.json` `CTrader:RealCopyExecutionEnabled` = false. `CTraderFixOptions.RealCopyExecutionEnabled` default false. API `/api/settings` hard-codes the flag false. Fix-worker reads `CTrader:RealCopyExecutionEnabled` (default false). **There is still no send path to gate.** Safety is fail-closed by absence **and** a documented default. | **TRUE default / PARTIAL as a coded gate** |
| 20 | “Trade #3 is early evidence, never LIVE promotion” | `BaselineScorer.EarlyScoreTradeCount = 3`. `TraderStateMachine.FromBaseline` never returns `LIVE`. `CanPromoteToLive` is `=> false`. Seed test: login `10001` has 3 completed XAU trades and `CurrentState != LIVE`. At N=3 the state can be `EARLY_SCORE`, `WATCH`, `SHADOW`, or `RISK_BLOCKED` — not only “early evidence.” | **TRUE** (never LIVE) / **PARTIAL** wording |
| 21 | Secrets in env / `.env`; see `.env.example` | `D:\Prop\.env.example` exists (3408 bytes, SHA `56C81786…`). Placeholders `<SECRET>`. `.gitignore` has `.env` + `.env.*` + `!.env.example`. | **TRUE** |
| 22 | “Never sent to React” | `/api/settings` returns risk limits, feature flag, broker **ids/names** only. `EfDashboardQueries` masks manager login. Web Settings page dumps that JSON; Live page has no secrets. `apps/web/src` has no password fields. API is **unauthenticated** (CORS `AllowAnyOrigin`) — no secret payload, also no RBAC. | **TRUE** for payload / **PARTIAL** for “never” as a control |
| 23 | `dotnet test D:\Prop\Mt5TraderIntelligence.sln` | SlN exists; 2 test projects (`Unit`, `Integration`) are in the sln. Command is valid on this lab machine. | **TRUE** |
| 24 | `dotnet run --project D:\Prop\apps\api\TraderIntelligence.Api.csproj` | Project exists. `launchSettings` `http` profile = `http://localhost:5000`. | **TRUE** (absolute path is lab-local) |
| 25 | `cd D:\Prop\apps\web` + `npm install` + `npm run dev` | `package.json` has those scripts. `vite.config.ts` `server.port = 3000`. | **TRUE** |
| 26 | API `http://localhost:5000` | Matches `profiles.http`, `.http` file, web axios/SignalR fallback, compose `5000:5000`. IIS Express is `:18720` + leftover `launchUrl: weatherforecast` — not what README documents. | **TRUE** for documented path |
| 27 | Dashboard `http://localhost:3000` | Vite 3000. | **TRUE** |
| 28 | “Without Postgres the API uses EF InMemory and seeds Achiever + StarwaveFX” | `AddTraderIntelligence`: empty / `<SECRET>` connection → `UseInMemoryDatabase("trader-intelligence")`. API `Program.cs` `EnsureCreated` + `DemoSeeder`. Seeder inserts both brokers. | **TRUE** |
| 29 | Implied: *with* Postgres it uses Postgres | Only if `ConnectionStrings:TraderIntelligence` or `DATABASE_URL` is a real string. `apps/api/appsettings.json` has `""`. `docker-compose.yml` starts postgres:16 but **does not set** `DATABASE_URL` on `api`, so compose API is still InMemory. | **PARTIAL** |
| 30 | `mt5-sdk` Windows-only for local Manager API | `mt5-sdk/README.md`: local = native Manager, Windows x64. `vendor/MetaTrader5SDK/Libs` present. Compose comment: do not Linux-container native DLL. C# does not load the DLL today. | **TRUE** (constraint) / **DEMO** (not used by C# worker) |

---

## 4. Component map vs tree

### 4.1 Named in README — present

| README path | On disk | In `Mt5TraderIntelligence.sln` | Notes |
|---|---|---|---|
| `apps/mt5-worker` | Yes | `TraderIntelligence.Mt5Worker` | Fake ingest loop; seeds on startup |
| `apps/api` | Yes | `TraderIntelligence.Api` | Dashboard JSON; no SignalR hub mapped despite web `startConnection()` |
| `apps/fix-worker` | Yes | `TraderIntelligence.FixWorker` | Heartbeat status writer, not a FIX engine |
| `src/Domain` | Yes | `TraderIntelligence.Domain` | Algorithms live here (not separate §66 projects) |
| `src/Infrastructure` | Yes | `TraderIntelligence.Infrastructure` | EF + seeder + dashboard queries |
| `src/Fix.CTrader` | Yes | `TraderIntelligence.Fix.CTrader` | Parser / options / harness; no QuickFIX |
| `apps/web` | Yes | **Not in sln** (Node) | Expected |

### 4.2 Present in repo, omitted from README key-components

| Path | What it is | Why the omission matters |
|---|---|---|
| `src/Application` | Ports, `DealIngestionService`, `ReconstructionScoringService`, dashboard DTOs | README jumps Domain → Infrastructure. New hires miss the composition layer. |
| `src/Mt5` | `FakeMt5BrokerConnector`, `IBrokerConnector`, `Mt5BrokerOptions` | This is what “ingest” actually talks to today. |
| `mt5-sdk/` | C++ Manager / HTTP client (named only in Native MT5) | Correctly called out later; not in the component list. |
| `tests/Unit`, `tests/Integration` | xUnit projects in the sln | `dotnet test` is listed; inventory is not. |
| `docker-compose.yml` | postgres 16 + redis 7 + `dotnet run` API | Undocumented. API still InMemory without a connection string. |
| `services/` | Empty dir; `.gitignore` reserves `services/ml-service` | Architecture §66 `/services/ml-service`. README is right not to advertise ML. |
| `docs/*.md` (6 stubs) | `ctrader-fix`, `risk`, `scoring`, `trade-reconstruction`, `xauusd-normalization` + architecture | README only cites architecture.md. Five §66 names are still missing (C11). |
| `.env.example` | Cited in Safety, not in Run | Run block never says “copy `.env.example`”. Demo works without it (InMemory). |

### 4.3 Architecture §66 folders README correctly does **not** invent

§66 lists `/src/TradeReconstruction`, `/src/Scoring`, `/src/Shadow`, `/src/Risk`, `/src/Execution`, `/tests/Replay`, `/tests/Fix`, `/tests/Risk`. Those directories **do not exist**. Logic is under `src/Domain/{Reconstruction,Scoring,Shadow,Risk,Execution}`. README’s map is **more accurate than §66** on this point.

---

## 5. Run recipe (demo)

### 5.1 What works if followed on this lab box

| Step | Expected | Risk |
|---|---|---|
| `dotnet test D:\Prop\Mt5TraderIntelligence.sln` | Builds 10 sln projects + runs Unit + Integration | Absolute path. Unit still contains `UnitTest1.cs` leftovers (not a README bug). |
| `dotnet run --project D:\Prop\apps\api\…` | Kestrel `http` profile `:5000`; seeds 2 brokers | First-run needs SDK 8. No prereq listed. |
| `npm install` / `npm run dev` in `apps/web` | Vite `:3000`; axios → `:5000` | `node_modules` already present. No Node version pin in README. |
| Browser API / dashboard URLs | Match clients | **IIS Express** profile is a different port (B41). README does not mention VS IIS. |

Workers are **not** in the Run block. Demo still functions because **the API seeds itself** on startup. That is acceptable for “see the dashboard.” It is incomplete for “run the system.”

Suggested completeness (do **not** implement from this report):

```powershell
dotnet run --project D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj
dotnet run --project D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj
```

Those hosts also seed + loop. They do not contact live venues.

### 5.2 Postgres sentence vs compose

```22:29:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (string.IsNullOrWhiteSpace(connection) || connection.Contains("<SECRET>", StringComparison.Ordinal))
        {
            services.AddDbContext<TraderDbContext>(o => o.UseInMemoryDatabase("trader-intelligence"));
        }
        else
        {
            services.AddDbContext<TraderDbContext>(o => o.UseNpgsql(connection));
        }
```

README: “Without Postgres the API uses EF InMemory.” Measured rule: **without a non-empty, non-placeholder connection string**. A running `localhost:5432` from compose does not switch the provider. Compose `api` service does not pass `DATABASE_URL` or `ConnectionStrings__TraderIntelligence`.

### 5.3 Demo seed vs “Achiever + StarwaveFX demo accounts”

`DemoSeeder` inserts broker rows `ACHIEVER` / `STARWAVEFX` (live-shaped hosts `57.128.141.65` / `84.201.6.142`, manager logins `2027` / `9904`) then syncs through `DemoBrokerFactory` fakes:

| Broker | Logins | Deals (closed round-trips) |
|---|---|---|
| Achiever | 10001, 10002, 10003 | 3 / 3 / 0 completed XAU trades |
| StarwaveFX | 99001 | 3 completed XAU trades |

That matches “seeds Achiever + StarwaveFX demo accounts.” It does **not** match “~5,000 MT5 accounts.”

---

## 6. Safety section vs code

| README Safety line | Code | Honest restatement |
|---|---|---|
| NOS off (`REAL_COPY_EXECUTION_ENABLED=false`) | Default false everywhere committed. Fix-worker logs the flag. Live page is a stub. **No 35=D encoder.** | Keep the default. Say **also**: there is no send path yet; turning the flag on does not place orders. |
| Trade #3 never LIVE | `CanPromoteToLive => false`; seeder test on `10001`. | Keep. Optionally: N=3 unlocks `EARLY_SCORE` / `WATCH` / `SHADOW` only. |
| Secrets in `.env`; never to React | `.env.example` placeholders; gitignore; settings API has no password. Seeder **does** persist live hostnames + manager login numbers into InMemory/Postgres. Brokers API returns **masked** login. | Secrets (passwords) are not in React. Venue **identifiers** are in seeder + `.env.example` + `CTraderFixOptions` defaults. |

`apps/api/appsettings.json` has empty `CTrader:Password` and empty connection string. That is correct. It also hard-codes the live FIX **host** and **account id** `1369850`. README does not mention that identifiers (not passwords) ship in committed JSON.

---

## 7. Diagram

README line 5: `![Architecture](docs/architecture.png)`.

| File | Role | Accurate? |
|---|---|---|
| `docs/architecture.svg` | Boxes: Achiever/StarwaveFX → `apps/mt5-worker` → Postgres/Outbox → Reconstruction → Scoring → Shadow/Risk → `src/Fix.CTrader` | Directionally yes; implies live Postgres/outbox/FIX sessions that are demo/stub |
| `docs/architecture.png` | Pillow placeholder, same 900×420, label “Architecture diagram (placeholder)” | **No.** Truncated text. C11: not a raster of the SVG. |

If README wants a picture, it should embed the **SVG** (or a real raster of it). The PNG as committed is a broken illustration.

`docs/architecture.md` does **not** link either image.

---

## 8. Editorial / structure issues (not behavioral)

1. **Two H1s** (lines 1 and 20). Merge.
2. **Architecture pointer twice** (lines 19 and 24).
3. **Absolute `D:\Prop\...` paths** in the Run block. Fine for this lab; wrong for a portable clone.
4. **No prerequisites:** .NET 8 SDK, Node 18+, optional Docker. `Directory.Build.props` is net-agnostic; every csproj is `net8.0`.
5. **No “what this is not”:** not an LP, not live copy, not 5k sync, not ML. `docs/architecture.md` already says live TRADE send and ML are off.
6. **No pointer** to `.env.example` in Run, `docker-compose.yml`, or `mt5-sdk/README.md` beyond the Native blurb.
7. Summary uses present tense for shadow + FIX route.

`docs/architecture.md` already has a better status table (Domain / Application / Mt5+sdk / FIX simulator / workers “heartbeat only”). README’s first paragraph contradicts that sibling.

---

## 9. Greenwash check

| Phrase a reader could over-read | Measured |
|---|---|
| “ingests MT5 manager events” | Fake connector, 4 accounts, ~18 deal legs |
| “shadow-copies approved trades” | Engine unused |
| “routes execution to a cTrader FIX 4.4 adapter” | No socket, no dictionary, no NOS |
| “~5,000 MT5 accounts” | Architecture target |
| “Pepperstone/cServer FIX 4.4” | Identifiers in config; no session |
| PNG “Architecture” | Placeholder |
| Collectors / Workers as production hosts | Demo loops + status stamps |

README does **not** claim §69 PASS, “fully decompiled,” or `REAL_COPY_EXECUTION_ENABLED=true`. Those absences are good. The present-tense pipeline sentence is the problem.

Compare: C13 accepted **0 / 12**. A README that sounds like a working copy stack is **in tension** with that gate.

---

## 10. Recommended README corrections (do not apply in this agent)

Product source stays untouched. If a later edit is authorized, the honest stub is:

- One title.
- Embed `docs/architecture.svg` (or a real PNG of it), not the placeholder.
- Summary: **demo** path = fake MT5 → reconstruct → baseline score → React. Live Manager, live QUOTE/TRADE, shadow book, and NOS are **not** enabled.
- Component list add `src/Application`, `src/Mt5`, `tests/`, `mt5-sdk/`.
- Keep Safety. Add: flag off **and** no send path; `CanPromoteToLive` is hard-false.
- Run: note InMemory when the connection string is empty; compose Postgres is unused until `DATABASE_URL` is set; workers optional because API seeds.
- Keep Native MT5 Windows warning.
- Point at `docs/architecture.md` as “what exists now” and v2 as law.

---

## 11. Evidence index

| Path | Why |
|---|---|
| `D:\Prop\README.md` | SUT |
| `D:\Prop\docs\architecture.png` | Embedded placeholder |
| `D:\Prop\docs\architecture.svg` | Actual diagram, unused by README |
| `D:\Prop\docs\architecture.md` | Honest implementer map |
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` | Law / goal (~5k, FIX, §66, §69) |
| `D:\Prop\Mt5TraderIntelligence.sln` | 10 projects; matches `dotnet test` path |
| `D:\Prop\.env.example` | Flag + secret placeholders |
| `D:\Prop\.gitignore` | `.env` ignored; `!.env.example` |
| `D:\Prop\docker-compose.yml` | Undocumented; API still InMemory |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | InMemory vs Npgsql switch |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Achiever + StarwaveFX seed |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | 4 accounts |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | Trade #3 / never LIVE |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | Unused engine |
| `D:\Prop\src\Fix.CTrader\` | Options/parser/harness only |
| `D:\Prop\apps\api\Program.cs` | Seed + `/api/settings` flag false |
| `D:\Prop\apps\api\Properties\launchSettings.json` | `:5000` http profile |
| `D:\Prop\apps\web\vite.config.ts` | `:3000` |
| `D:\Prop\apps\web\src\api\client.ts` | `VITE_API_URL \|\| http://localhost:5000` |
| `D:\Prop\apps\fix-worker\Worker.cs` | NOS refused / status stamp |
| `D:\Prop\mt5-sdk\README.md` | Windows local Manager |
| `D:\Prop\scripts\svg_to_png.py` | PNG fallback provenance |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | 2 brokers; `10001` not LIVE |

---

## 12. Classification

| Item | §73.B-style |
|---|---|
| Root `README.md` as onboarding | **EXISTS_NEEDS_REFACTOR** |
| Path map of apps/src named in README | **CURRENT** |
| Behavioral summary (ingest / shadow / FIX route / 5k) | **UNSAFE** (present tense overclaim) |
| Safety + Native MT5 sections | **CURRENT** with the caveats in §6 |
| Embedded PNG | **EXISTS** / **placeholder** (not a diagram) |
| §66 documentation obligation | **Not this file’s job** (see C11: 6/11 stubs, 0/11 complete) |

**Done when (for a future README edit, not this agent):** one title; image is the real SVG; present tense matches the fake-connector demo; goal (~5k, live FIX, shadow book) is labeled **goal**; Application + Mt5 + tests appear; Run recipe states the InMemory rule correctly; no reader can think NewOrderSingle is a flip away.

C45 did not edit product source.
