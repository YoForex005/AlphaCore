# D64 — `D:\Prop\README.md` vs the as-built tree

| Field | Value |
|---|---|
| Agent | D64 (senior engineer; README claim-check only) |
| Date | 2026-08-18 |
| Measured at (local) | 2026-08-18 (this pass; README `LastWriteTime` 13:26:07 +05:30) |
| Artifact | `D:\Prop\reports\swarm\20260818\D64_readme.md` |
| Target | `D:\Prop\README.md` compared to the live tree under `D:\Prop` |
| Law | Architecture v2 (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`). §66 does **not** require a root README. §41 / §55–§56 / §69 apply to the claims the README makes. |
| Quality bar | Path names must exist. Present-tense behavioral claims must match hosted code, not the goal. Safety defaults must still be true. |
| Related (do not treat as this file) | C30 (existence + landing gaps), C45 (claim matrix; **partially stale**), C11 / D10 (docs), B41 (ports), C13 (§69 0/12), C42 / C43 (no live MT5 / FIX), B40 (env), A75 |
| Product source modified | **No.** This report is the only write. `D:\Prop\README.md`, `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\docs` were not edited. |
| Classification | **EXISTS_NEEDS_REFACTOR** |

---

## 0. Verdict

**`README.md` is an unchanged 49-line lab stub.** Every path it names still exists. Several sentences still describe the **architecture goal** as if it were the running system. Two claims that were true at C30/C45 are **no longer true** against disk:

1. **Root `.env.example` is gone.** README Safety says “see `.env.example`.” Recursive search under `D:\Prop` (excluding `node_modules` / `bin` / `obj` / `vendor`) finds **only** `D:\Prop\mt5-sdk\.env.example`. A live `D:\Prop\.env` exists (gitignored). The pointer in the landing page is now a **broken link**.
2. **`ShadowCopyEngine` is no longer unused.** `EfTradingStore.PersistDemoShadowAsync` constructs it and writes `shadow_orders` for `TraderState.SHADOW`. C45 §3 #7 and D16 “zero product callers” are **stale**. The call is still a **demo replay** on a canned dest quote, not a live shadow book.

Do **not** treat the README as proof of first useful version (§69), live Manager ingest, live cTrader FIX, or a flip-the-flag `NewOrderSingle` path. `docs/architecture.md` remains the more honest one-page map.

| Question | Measured answer |
|---|---|
| File exists at repo root | **Yes** — `D:\Prop\README.md` |
| Bytes / physical lines / SHA-256 | **1746** / **49** / `C023B4227A1C511F346A1FD96B6648B901001551731D1472FE7A33468B1A2764` |
| Encoding | UTF-8, **no BOM**, **LF only** (49 LF, 0 CR) |
| LastWrite | 2026-08-18T13:26:07.6594355+05:30 — **unchanged since C30/C45** |
| Named component paths that exist | **7 / 7** |
| Named spec files that exist | **2 / 2** (v2 spec + `docs/architecture.md`) |
| Embedded diagram | `docs/architecture.png` **exists** and is a **Pillow placeholder**, not the sibling SVG |
| Run-recipe hosts | API `:5000` (`http` profile), web `:3000` (Vite) — **match** |
| `REAL_COPY_EXECUTION_ENABLED=false` on disk | **True** as a default / hard-coded settings payload. **No send path to gate.** |
| Root `.env.example` | **MISSING** (README pointer **FALSE**) |
| Live `NewOrderSingle` send path | **Absent** — fail-closed by missing sender, not only by the flag |
| ~5,000 MT5 accounts in the demo | **No** — **4** canned logins |
| Shadow engine hosted as §24 book | **No** — demo persist only, for `SHADOW` state |
| First useful version (§69) | **0 / 12 accepted** (C13). README does not claim §69 PASS; it still *reads* like a working copy system. |
| Product source edited by this agent | **No** |

**Onboarding usefulness:** a developer on this lab box can find the solution, start the API + Vite dashboard, and see InMemory demo data.

**Accuracy:** the summary paragraph and the “~5,000 accounts / shadow / route to Pepperstone” sentence **overclaim**. Safety + Native MT5 are the most truthful sections, except the now-broken `.env.example` pointer.

---

## 1. Method

| Step | Action |
|---|---|
| Read | Full `D:\Prop\README.md` (49 lines). File opened; not a missing-path error. |
| Census | `Get-Item` + `Get-FileHash SHA256` + raw LF/CR/BOM on README, docs, sln, compose, launchSettings, vite, package.json |
| Tree | `list_dir` / recursive `*.cs` on `src/*`, `apps/*`, `docs`, `tests`, `mt5-sdk`, `services` |
| Cross-check | Solution, `DependencyInjection.cs`, `DemoSeeder.cs`, `FakeMt5BrokerConnector.cs`, `BaselineScorer.cs`, `CTraderFixOptions.cs`, API `Program.cs`, both workers, `EfTradingStore.PersistDemoShadowAsync`, `.gitignore`, `docker-compose.yml`, `.env` existence only (contents **not** dumped) |
| Visual | `docs/architecture.png` (placeholder) vs `docs/architecture.svg` (real boxes) |
| Prior | C30, C45, C11, D10, C13, B41, C42, C43, D16, D22 — used as history, **re-measured** where they conflict with disk |
| Not done | No `dotnet test` / `dotnet run` / `npm` / HTTP probe. **No product edit.** |

---

## 2. On-disk README (authoritative)

| Field | Value |
|---|---|
| Path | `D:\Prop\README.md` |
| Exists | **True** |
| Bytes | **1746** |
| Lines | **49** (ends after the Native MT5 paragraph) |
| Non-blank (C30) | **33** |
| SHA-256 | `C023B4227A1C511F346A1FD96B6648B901001551731D1472FE7A33468B1A2764` |
| H1 count | **2** (`# Trader Intelligence` L1; `# MT5 XAUUSD Trader Intelligence + cTrader FIX 4.4` L20) |
| H2 count | **3** (`Safety`, `Run (demo)`, `Native MT5`) |

The file is **two untitled blocks concatenated**. No blank line between the spec pointer (L19) and the second H1 (L20). Body is unchanged vs C30/C45.

Verbatim (this hash):

```markdown
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
Without Postgres the API uses EF InMemory + Achiever + StarwaveFX seed

## Native MT5
`mt5-sdk` Windows-only local Manager API. Do not Linux-container the native DLL.
```

---

## 3. Claim-by-claim matrix (this pass)

Tokens: **TRUE** (matches tree), **DEMO** (shape exists on fakes / InMemory), **OVERCLAIM** (goal written as current), **PARTIAL**, **FALSE**, **EDITORIAL**.

| # | README claim | Tree (2026-08-18 this pass) | Token |
|---|---|---|---|
| 1 | Title “Trader Intelligence” | Namespaces / sln are `TraderIntelligence.*`. Web chrome says “MT5 Intelligence”. | **TRUE** / **EDITORIAL** (two H1s) |
| 2 | Image `docs/architecture.png` | File exists, 12081 bytes, SHA `0F7BAF6D2461A5A055C83C278FCD0A8F718B3C2B86C19886221FDCB259EC98C9`. Visual this pass: truncated “MT5 Source Brokers → Collectors → DB → Reconstruction → Scori” + **“Architecture diagram (placeholder)”**. Sibling `docs/architecture.svg` (2697 bytes, SHA `23F51B89D6CA6FC4A649E9A3F7DC04AFCB42485892D8604E3ACAD18EAFEB4327`) is the real box diagram and is **not** embedded. | **TRUE path / FALSE diagram** |
| 3 | “lightweight C#/.NET backend” | net8.0 solution, 10 product projects in `Mt5TraderIntelligence.sln`. | **TRUE** |
| 4 | “ingests MT5 manager events” | `DealIngestionService` + `apps/mt5-worker` 30 s loop. Sole `IMt5BrokerConnector` impl is `FakeMt5BrokerConnector`. `ConnectAsync` sets `_connected = true`. No `DllImport` / `MT5APIManager64.dll`. C++ `mt5-sdk` is **not wired**. | **DEMO** |
| 5 | “reconstructs trades” | `TradeReconstructor` used by `ReconstructionScoringService`. Integration test asserts completed XAUUSD after seed. | **DEMO** (algorithm real; tape canned) |
| 6 | “scores XAUUSD traders” | `BaselineScorer` + `ITradingStore.UpsertScoreAsync`. | **DEMO** |
| 7 | “shadow-copies approved trades” | Engine **is** called from `EfTradingStore.PersistDemoShadowAsync` when state is `SHADOW`. Writes `CopyIntent` + `ShadowOrder` against the **seeded** dest quote (`VenueInstrumentId = null`, 2399.45/2399.85). Not in DI as a hosted book. No dest QUOTE socket. No fail-closed stale-quote path (D16 on the calculator still holds). | **DEMO** (was OVERCLAIM/unused at C45) |
| 8 | “routes execution to a cTrader FIX 4.4 adapter” | `src/Fix.CTrader` = options + `FixMessageParser` + `FixSessionOwnership` + `CTraderQuoteService` + `FixSimulationHarness`. **No** QuickFIX/n package (`Fix.CTrader.csproj` has zero PackageReference). **No** `35=D` builder. API does **not** reference the project. `apps/fix-worker` references it but `Worker.cs` only stamps `Disconnected` every 15 s. | **OVERCLAIM** |
| 9 | Collectors = `apps/mt5-worker` | Project exists. Syncs **fake** brokers every 30 s for 4 logins. | **TRUE path / DEMO behavior** |
| 10 | API = `apps/api` | `TraderIntelligence.Api.csproj` exists. Minimal APIs + Swagger in Development + CORS `AllowAnyOrigin`. Unused `SettingsController` (needs Redis `IConnectionMultiplexer`; host never `AddControllers` / `MapControllers`). | **TRUE** |
| 11 | Workers = mt5-worker + fix-worker | Both exist and are in the sln. | **TRUE** |
| 12 | Domain = `src/Domain` | Reconstruction, scoring, risk, shadow, execution, instruments, volume, entities, enums. More accurate than §66’s extra `/src/Scoring` folders (those folders were **not** created). | **TRUE** |
| 13 | Persistence = `src/Infrastructure` | `TraderDbContext`, `EfTradingStore`, `DemoSeeder`, InMemory/Npgsql switch. **0** EF migrations. Hosts call `EnsureCreatedAsync`. | **TRUE** |
| 14 | FIX adapter = `src/Fix.CTrader` | Folder + csproj exist. Not a live initiator. | **TRUE path / OVERCLAIM “adapter”** |
| 15 | Web = `apps/web` | Vite + React 18, 15 page files, port 3000. Not in the sln (expected). | **TRUE** |
| 16 | Spec files v2 + `docs/architecture.md` | Both exist. v2 = 50966 bytes, SHA `0B3C0EDC09081C25D097FF0E6AADC7A638562EBB8DB345DC325DC54EC904D37E`. `docs/architecture.md` = 1379 bytes, honest status table. | **TRUE** |
| 17 | “~5,000 MT5 accounts” | `DemoBrokerFactory` seeds **3 Achiever + 1 StarwaveFX** logins (`10001`, `10002`, `10003`, `99001`). Architecture §69 still wants ~5k. | **OVERCLAIM** (goal, not demo) |
| 18 | “shadow them, then (explicit flag) route … to Pepperstone/cServer FIX 4.4” | Intent of v2. Demo shadow persist exists. No NOS. `TargetCompID` in seeder / options default is `cServer` (correct). `apps/api/appsettings.json` `CTraderFix:TargetCompId` is **`CSERVER`** (case bug; unused by the worker). Live host/account strings appear in seeder + `CTraderFixOptions` defaults. Nothing connects. | **OVERCLAIM** |
| 19 | Real NOS **off** via `REAL_COPY_EXECUTION_ENABLED=false` | API `/api/settings` hard-codes the flag `false`. `CTraderFixOptions.RealCopyExecutionEnabled` default `false`. Fix-worker reads `CTrader:RealCopyExecutionEnabled` (default false) and logs refuse. Overview DTO last field `RealCopyEnabled` is hardcoded `false` in `EfDashboardQueries`. **There is still no send path to gate.** Root `.env` exists and is not dumped here; committed configs do not turn the flag on. | **TRUE default / PARTIAL as a coded gate** |
| 20 | “Trade #3 is early evidence, never LIVE promotion” | `BaselineScorer.EarlyScoreTradeCount = 3`. `TraderStateMachine.FromBaseline` never returns `LIVE`. `CanPromoteToLive` is `=> false`. Seed test: login `10001` has 3 completed XAU trades and `CurrentState != LIVE`. At N=3 state can be `EARLY_SCORE` / `WATCH` / `SHADOW` / `RISK_BLOCKED`. | **TRUE** (never LIVE) / **PARTIAL** wording |
| 21 | Secrets in env / `.env`; see `.env.example` | `D:\Prop\.env` **exists** (3408 bytes, gitignored by `.gitignore` L28–30). **Root `.env.example` is MISSING.** Only `D:\Prop\mt5-sdk\.env.example` (4999 bytes) remains. `.gitignore` still has `!.env.example`. | **FALSE pointer** / **TRUE** that a local `.env` is the intended secret home |
| 22 | “Never sent to React” | Live `/api/settings` (minimal API) returns risk limits, feature flag, broker **ids/names** only. `EfDashboardQueries` masks manager login. Settings page dumps that JSON. `apps/web/src` has no password fields. API is **unauthenticated** (`AllowAnyOrigin`) — no secret payload, also no RBAC. Dead `SettingsController` would also avoid passwords if it were mapped. | **TRUE** for payload / **PARTIAL** for “never” as a control |
| 23 | `dotnet test D:\Prop\Mt5TraderIntelligence.sln` | SlN exists (SHA `AD5030070166D81EE478B632BCE3F381F2C9D3FFBBC6EB0FDD407047C5B5A7B4`). Unit + Integration projects are in the sln. `test-release.log` at repo root is **empty**. This agent **did not** run tests. | **TRUE path** / **UNVERIFIED run** |
| 24 | `dotnet run --project D:\Prop\apps\api\TraderIntelligence.Api.csproj` | Project exists. `launchSettings` `http` profile = `http://localhost:5000`. Absolute lab path. | **TRUE** (path) |
| 25 | `cd D:\Prop\apps\web` + `npm install` + `npm run dev` | `package.json` scripts `dev` = `vite`. `vite.config.ts` `server.port = 3000`. `node_modules` already present. | **TRUE** |
| 26 | API `http://localhost:5000` | Matches `profiles.http`, `TraderIntelligence.Api.http`, web axios/SignalR fallback, compose `5000:5000`. IIS Express is still `:18720`. `https` profile also includes `:5000`. | **TRUE** for documented path |
| 27 | Dashboard `http://localhost:3000` | Vite 3000. `appsettings.json` `Cors:AllowedOrigins` still lists **`http://localhost:5173`** (Vite default, unused). Host uses `AllowAnyOrigin`, so the dashboard still loads. | **TRUE** port / leftover CORS list |
| 28 | “Without Postgres the API uses EF InMemory and seeds Achiever + StarwaveFX” | DI: empty / `<SECRET>` `ConnectionStrings:TraderIntelligence` **or** `DATABASE_URL` → `UseInMemoryDatabase("trader-intelligence")`. API `Program.cs` `EnsureCreated` + `DemoSeeder`. Seeder inserts both brokers. **`appsettings.json` only has `ConnectionStrings:Postgres`**, which DI **does not read**. Compose `api` does **not** set `DATABASE_URL`. So even *with* compose Postgres up, the API stays InMemory. | **TRUE** for the default lab path / **PARTIAL** wording (“without Postgres”) |
| 29 | `mt5-sdk` Windows-only for local Manager API; do not Linux-container the native DLL | `mt5-sdk/README.md`: local = native Manager, Windows x64. `CMakeLists.txt` `if(WIN32)` gates manager/pool/watchdog. Compose comment L30 matches. C# worker does **not** load the DLL. | **TRUE** (constraint) / **DEMO** (not used by C# worker) |

No live FIX/MT5/DB password appears in the README body. No “Phase 1 Done.” No ML-as-shipped claim. Those absences remain **correct**.

---

## 4. Component map vs tree

### 4.1 Named in README — present

| README path | On disk | In `Mt5TraderIntelligence.sln` | Notes |
|---|---|---|---|
| `apps/mt5-worker` | Yes | `TraderIntelligence.Mt5Worker` | Fake ingest + score of 4 logins / 30 s |
| `apps/api` | Yes | `TraderIntelligence.Api` | Dashboard JSON; no SignalR hub mapped |
| `apps/fix-worker` | Yes | `TraderIntelligence.FixWorker` | 15 s status writer; stamps **Disconnected** |
| `src/Domain` | Yes | `TraderIntelligence.Domain` | Algorithms live here |
| `src/Infrastructure` | Yes | `TraderIntelligence.Infrastructure` | EF + seeder + dashboard queries |
| `src/Fix.CTrader` | Yes | `TraderIntelligence.Fix.CTrader` | Parser / options / harness; no QuickFIX |
| `apps/web` | Yes | **Not in sln** (Node) | Expected |

### 4.2 Present in repo, omitted from README key-components

| Path | What it is | Why the omission matters |
|---|---|---|
| `src/Application` | 3 source files: `Mt5Contracts.cs`, `DashboardModels.cs`, `DealIngestionService.cs` (also hosts `ReconstructionScoringService` + `ITradingStore`) | README jumps Domain → Infrastructure. New hires miss the composition layer. |
| `src/Mt5` | `FakeMt5BrokerConnector`, unused `IBrokerConnector`, `Mt5BrokerOptions` | This is what “ingest” actually talks to today. |
| `mt5-sdk/` | C++ Manager / HTTP client (named only in Native MT5) | Correctly called out later; not in the component list. |
| `tests/Unit`, `tests/Integration` | xUnit projects in the sln | `dotnet test` is listed; inventory is not. |
| `docker-compose.yml` | postgres 16 + redis 7 + `dotnet run` API | Undocumented. API still InMemory without `DATABASE_URL`. |
| `services/` | **Empty** dir; `.gitignore` reserves `services/ml-service` | README is right not to advertise ML. |
| `docs/*.md` | Now **7** Markdown files (see §8) | README only cites `architecture.md`. |
| Root `.env.example` | **Cited, missing** | Run block never said “copy `.env.example`”; the file is now gone. |

### 4.3 Architecture §66 folders README correctly does **not** invent

§66 lists `/src/TradeReconstruction`, `/src/Scoring`, `/src/Shadow`, `/src/Risk`, `/src/Execution`, `/tests/Replay`, `/tests/Fix`, `/tests/Risk`. Those directories **do not exist**. Logic is under `src/Domain/{Reconstruction,Scoring,Shadow,Risk,Execution}`. README’s map is **more accurate than §66** on this point.

---

## 5. Run recipe vs what actually starts

### 5.1 What the block implies

| Step | Path / port | Risk |
|---|---|---|
| `dotnet test D:\Prop\Mt5TraderIntelligence.sln` | 10 sln projects + Unit + Integration | Absolute path. This agent did **not** execute it. `test-release.log` is empty. |
| `dotnet run --project …Api.csproj` | Kestrel `http` profile `:5000`; seeds 2 brokers | Needs .NET 8 SDK. No prereq listed. |
| `npm install` / `npm run dev` | Vite `:3000`; axios → `:5000` | No Node version pin. |
| Browser URLs | Match clients | IIS Express is a different port (B41). README does not mention VS IIS. |

Workers are **not** in the Run block. Demo still functions because **the API seeds itself** on startup. Acceptable for “see the dashboard.” Incomplete for “run the system.”

Suggested completeness (do **not** implement from this report):

```powershell
dotnet run --project apps/mt5-worker/TraderIntelligence.Mt5Worker.csproj
dotnet run --project apps/fix-worker/TraderIntelligence.FixWorker.csproj
```

Those hosts also seed + loop. They do not contact live venues.

### 5.2 InMemory rule (measured)

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

`connection` is `ConnectionStrings:TraderIntelligence` **or** `DATABASE_URL`.

`apps/api/appsettings.json` has `ConnectionStrings:Postgres` (empty password) — **wrong key**. Compose `api` service does not pass `DATABASE_URL`. A running `localhost:5432` does **not** switch the provider.

### 5.3 Demo seed vs “Achiever + StarwaveFX demo accounts”

`DemoSeeder` inserts broker rows `ACHIEVER` / `STARWAVEFX` (hosts `57.128.141.65` / `84.201.6.142`, manager logins `2027` / `9904`) then syncs through `DemoBrokerFactory` fakes:

| Broker | Logins | Closed XAU round-trips |
|---|---|---|
| Achiever | 10001, 10002, 10003 | 3 / 3 / **0** |
| StarwaveFX | 99001 | 3 |

That matches “seeds Achiever + StarwaveFX demo accounts.” It does **not** match “~5,000 MT5 accounts.”

FIX session rows are now **`Disconnected`** with honest `LastError` strings (“No live QUOTE/TRADE socket”). D22’s “seeder writes `LoggedOn`” is **stale** against current `DemoSeeder.cs` (5082 bytes, SHA prefix `A641649125EE9D10`).

Dashboard `BrokerStatusDto.Connected` is still hardcoded `true` in `EfDashboardQueries` (line 53). `/api/health` is more honest: `"demo FakeMt5BrokerConnector — not live Manager"` / `"no live TLS socket"`.

---

## 6. Safety section vs code

| README Safety line | Code | Honest restatement |
|---|---|---|
| NOS off (`REAL_COPY_EXECUTION_ENABLED=false`) | Default false on options. `/api/settings` hard-codes false. Fix-worker logs the flag. Live page stub. **No 35=D encoder.** | Keep the default. Say **also**: there is no send path yet; turning the flag on does not place orders. |
| Trade #3 never LIVE | `CanPromoteToLive => false`; seeder test on `10001`. | Keep. Optionally: N=3 unlocks `EARLY_SCORE` / `WATCH` / `SHADOW` / `RISK_BLOCKED` only. |
| Secrets in `.env`; see `.env.example` | Local `.env` exists and is gitignored. **Example file at repo root is gone.** Seeder still persists live hostnames + manager login numbers. Brokers API returns **masked** login. | Passwords are not in React. The README **must not** keep pointing at a missing `.env.example`. Do not paste `.env` contents into a later README edit. |

`apps/api/appsettings.json` has empty `CTraderFix:SenderCompId` and empty Postgres password. It also hard-codes `TargetCompId: "CSERVER"` (wrong case vs architecture `cServer`) and a leftover CORS origin `:5173`. README does not mention either.

Dead `apps/api/Controllers/SettingsController.cs` talks to Redis. It is **not** the mapped `/api/settings`. Do not document it as the settings API.

---

## 7. Diagram

README L5: `![Architecture](docs/architecture.png)`.

| File | Role | Accurate? |
|---|---|---|
| `docs/architecture.svg` | Boxes: Achiever/StarwaveFX → `apps/mt5-worker` → Postgres/Outbox → Reconstruction → Scoring → Shadow/Risk → `src/Fix.CTrader` | Directionally the **goal**. Implies live Postgres/outbox/FIX sessions that are demo/stub. **Not linked** from `docs/architecture.md`. |
| `docs/architecture.png` | Pillow placeholder, 900×420, truncated pipeline + “Architecture diagram (placeholder)” | **No.** Provenance: `scripts/svg_to_png.py` fallback after cairosvg missing (C11). |

If README wants a picture, it should embed the **SVG** (or a real raster of it). The PNG as committed is a broken illustration.

---

## 8. Docs the README points at vs the docs folder now

README names two docs: the v2 spec and `docs/architecture.md`. It does not claim §66 is complete. That omission is fine.

Disk under `D:\Prop\docs` **this pass** (D10 is stale on the extra files):

| Name | Bytes | SHA-256 | In README? |
|---|---:|---|---|
| `architecture.md` | 1379 | `A5FB4FEF…` | **Yes** |
| `architecture.png` | 12081 | `0F7BAF6D…` | Embedded |
| `architecture.svg` | 2697 | `23F51B89…` | **No** |
| `ctrader-fix.md` | 3195 | `52E80263…` | No |
| `deployment.md` | 2997 | `7F4B6130…` | No (D10: **MISSING** — **stale**) |
| `risk.md` | 2678 | `26ACB40F…` | No |
| `scoring.md` | 327 | `91558CDB…` | No |
| `trade-reconstruction.md` | 2293 | `500B4FF1…` | No |
| `xauusd-normalization.md` | 263 | `28C228F1…` | No |

§66 still **MISSING**: `mt5-integration.md`, `ml.md`, `shadow-copy.md`, `reconciliation.md`. Present **7 / 11**. `docs/README.md` still absent.

`docs/deployment.md` exists and **overclaims** relative to the tree (QuickFIX/N “is pure .NET”, `build\Release\mt5_worker.exe`, `dotnet out/TraderIntelligence.dll`, compose snippet `build: ./src/api`). A later README must **not** copy those sentences. Cite it only as a draft, or wait until it matches `docker-compose.yml`.

---

## 9. Editorial / structure (not behavioral)

1. **Two H1s** (L1 and L20). Merge.
2. **Architecture pointer twice** (L19 and L24).
3. **Absolute `D:\Prop\...` paths** in the Run block. Lab-local; wrong for a portable clone.
4. **No prerequisites:** .NET 8 SDK, Node 18+, optional Docker.
5. **No “what this is not”:** not an LP, not live copy, not 5k sync, not ML. `docs/architecture.md` already says live TRADE send and ML are off.
6. **Ingest and Workers both name `apps/mt5-worker`.** Hides `src/Mt5` and `mt5-sdk`.
7. Summary uses present tense for shadow + FIX route.
8. **Broken `.env.example` pointer** (new vs C45).

`docs/architecture.md` already has a better status table (Domain / Application / Mt5+sdk / FIX simulator / workers “heartbeat/status only”). README’s first paragraph contradicts that sibling.

---

## 10. Greenwash check

| Phrase a reader could over-read | Measured |
|---|---|
| “ingests MT5 manager events” | Fake connector, 4 accounts, 9 closed XAU round-trips (18 deal legs) |
| “shadow-copies approved trades” | Demo persist for `SHADOW` only, canned dest quote, no dest socket |
| “routes execution to a cTrader FIX 4.4 adapter” | No socket, no QuickFIX, no NOS |
| “~5,000 MT5 accounts” | Architecture target |
| “Pepperstone/cServer FIX 4.4” | Identifiers in seeder / options; no session |
| PNG “Architecture” | Placeholder |
| Collectors / Workers as production hosts | Demo loops + Disconnected stamps |
| “see `.env.example`” | **File missing at repo root** |

README does **not** claim §69 PASS, “fully decompiled,” or `REAL_COPY_EXECUTION_ENABLED=true`. Those absences are good. The present-tense pipeline sentence is the problem.

C13 accepted **0 / 12**. A README that sounds like a working copy stack is still **in tension** with that gate. Item 11 (shadow) moved from “engine unused” to “demo persist” — still **not** accepted.

---

## 11. What changed since C30 / C45 (do not reuse their rows blindly)

| Prior sentence | Now |
|---|---|
| C30/C45: root `.env.example` exists (3408 bytes, SHA `56C81786…`) | **FALSE.** Root example is **gone**. A gitignored `D:\Prop\.env` of the same **3408** byte size exists. Do not assume the example was renamed; do not dump `.env`. |
| C45 #7: `ShadowCopyEngine` definition only | **Stale.** `PersistDemoShadowAsync` calls `SimulateEntry` and writes `shadow_orders`. |
| D16: “zero product callers” | **Stale** for callers. Calculator limitations in D16 still apply. |
| D22: seeder writes TRADE `LoggedOn` / QUOTE `ReadyForMarketData` | **Stale.** Current seeder writes `Disconnected` + honest `LastError`. |
| D10: `docs/deployment.md` missing | **Stale.** File exists (2997 bytes). |
| D10: `ctrader-fix.md` 686 B / `risk.md` 404 B / `trade-reconstruction.md` 545 B | **Stale sizes.** Those three files have grown (see §8). |
| C13 item 11 “engine unused” | **Stale wording.** Still not §69-accepted. |
| B38: README embeds the SVG | **Stale.** README embeds the **PNG**. |
| A06 / A54 “API is `:5160`” | **Stale** (B41). Keep **5000**. |
| C28: no hub mapped | **Still true.** Web `startConnection()` still dials `/hubs/dashboard`. |
| C13 `/api/health` hard-codes healthy with no caveat | **Partially stale.** Details strings now admit Fake MT5 / no TLS. `healthy = true` on MT5 and DB remains a **shape lie**. |

README **content** has not moved (same SHA as C30/C45). The **tree around it** has.

---

## 12. Recommended later README edit (authorized docs task — **not this agent**)

Single file: keep `D:\Prop\README.md`. Do not add a second root name. Do not paste architecture §56 live IPs, manager logins, or FIX account identifiers into the README.

Suggested shape:

1. **One** H1: `MT5 XAUUSD Trader Intelligence + cTrader FIX 4.4`.
2. One-paragraph **is / is-not**: observe ~5k XAUUSD traders (**goal**); demo today is Fake MT5 → reconstruct → baseline score → React; not an LP; not live-by-default.
3. Embed `docs/architecture.svg` (or a real PNG of it). Stop embedding the placeholder.
4. Component table that adds `src/Application`, `src/Mt5`, `mt5-sdk`, `tests/`, `docker-compose.yml`.
5. Safety block: keep NOS-off and Trade #3; add “flag off **and** no send path”; `CanPromoteToLive` is hard-false; restore a **placeholder-only** `.env.example` before pointing at it again.
6. Run (demo) with **relative** paths; state the InMemory rule as “empty / placeholder `TraderIntelligence` / `DATABASE_URL`”; note compose Postgres is unused until that key is set; workers optional because API seeds.
7. Link list: v2 spec, `docs/architecture.md` as “what exists now”, and the other `docs/*.md` **that exist**. Do not link the four missing §66 names as if written. Do not copy `docs/deployment.md` overclaims.
8. Keep the Native MT5 Windows sentence.

---

## 13. Evidence index

| Path | Why |
|---|---|
| `D:\Prop\README.md` | SUT — SHA `C023B422…` |
| `D:\Prop\docs\architecture.png` | Embedded placeholder |
| `D:\Prop\docs\architecture.svg` | Actual diagram, unused by README |
| `D:\Prop\docs\architecture.md` | Honest implementer map |
| `D:\Prop\docs\deployment.md` | Exists now; overclaims vs compose |
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` | Law / goal |
| `D:\Prop\Mt5TraderIntelligence.sln` | 10 projects; matches `dotnet test` path |
| `D:\Prop\.gitignore` | `.env` ignored; `!.env.example` with no file |
| `D:\Prop\.env` | Exists; **not opened** in this report |
| `D:\Prop\mt5-sdk\.env.example` | Only remaining example |
| `D:\Prop\docker-compose.yml` | Undocumented; API still InMemory |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | InMemory vs Npgsql switch |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Achiever + StarwaveFX seed; FIX `Disconnected` |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | 4 accounts |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | Trade #3 / never LIVE |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | Calculator |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | Now calls the engine |
| `D:\Prop\src\Fix.CTrader\` | Options/parser/harness only |
| `D:\Prop\apps\api\Program.cs` | Seed + `/api/settings` flag false + honest health details |
| `D:\Prop\apps\api\Properties\launchSettings.json` | `:5000` http profile |
| `D:\Prop\apps\web\vite.config.ts` | `:3000` |
| `D:\Prop\apps\web\src\api\client.ts` | `VITE_API_URL \|\| http://localhost:5000` |
| `D:\Prop\apps\fix-worker\Worker.cs` | NOS refused / Disconnected stamp |
| `D:\Prop\mt5-sdk\README.md` + `CMakeLists.txt` | Windows local Manager |
| `D:\Prop\scripts\svg_to_png.py` | PNG fallback provenance |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | 2 brokers; `10001` not LIVE |

---

## 14. Classification

| Item | §73.B-style |
|---|---|
| Root `README.md` as onboarding | **EXISTS_NEEDS_REFACTOR** |
| Path map of apps/src named in README | **CURRENT** |
| Behavioral summary (ingest / 5k / FIX route) | **UNSAFE** (present-tense overclaim) |
| “shadow-copies approved trades” | **DEMO** (persist path exists; not §24 / §69) |
| Safety + Native MT5 sections | **CURRENT** except the **broken `.env.example` pointer** |
| Embedded PNG | **EXISTS** / **placeholder** |
| `.env.example` at repo root | **MISSING** |
| §66 documentation obligation | **Not this file’s job** (7/11 present; 0/11 complete) |

**Done when (for a future README edit, not this agent):** one title; image is the real SVG; present tense matches the fake-connector demo; goal (~5k, live FIX, live shadow book) is labeled **goal**; Application + Mt5 + tests appear; Run recipe states the InMemory rule correctly; `.env.example` exists again before it is cited; no reader can think NewOrderSingle is a flip away.

---

## 15. Out of scope (not done)

- Did **not** edit `D:\Prop\README.md`.
- Did **not** recreate `.env.example`.
- Did **not** replace `docs/architecture.png`.
- Did **not** run `dotnet test` / `dotnet run` / `npm`.
- Did **not** open or quote `D:\Prop\.env`.
- Did **not** modify product source under `src/` or `apps/`.
- Did **not** rewrite `INDEX.md` / `SWARM_LOG.md` (out of this ticket’s write set).

**Deliverable:** `D:\Prop\reports\swarm\20260818\D64_readme.md` only.

---

## 16. Assigned one-liner

**`D:\Prop\README.md` is present and byte-identical to C30/C45** (1746 bytes, 49 lines, SHA-256 `C023B4227A1C511F346A1FD96B6648B901001551731D1472FE7A33468B1A2764`). Class **EXISTS_NEEDS_REFACTOR**: dual H1, placeholder PNG, present-tense ingest/5k/FIX overclaim; **new:** root `.env.example` pointer is **FALSE**; **updated:** demo shadow persist now exists (still not live copy). Safety defaults (NOS off, Trade #3 ≠ LIVE) and ports 5000/3000 still check out.
