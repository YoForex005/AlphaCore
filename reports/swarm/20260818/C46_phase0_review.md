# C46 — Independent review of `reports/PHASE0_AUDIT.md` vs repo

| Field | Value |
|---|---|
| Agent | C46 (senior engineer, Phase 0 audit review only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:28:48+05:30 |
| Assigned | Read `reports/PHASE0_AUDIT.md` vs repo. Rubber-stamp? Write this report. Do not modify product source. |
| Subject | `D:\Prop\reports\PHASE0_AUDIT.md` |
| Subject size | 1564 bytes, **26** non-blank / 43 physical lines |
| Subject SHA-256 | `09CD30F357837987E55362C4C7B04C70CF50723982FD3F7E2C17A1775454D61E` |
| Subject mtime | 2026-08-18 13:20:12 +05:30 |
| Git | **untracked** (`git status --porcelain -- reports/PHASE0_AUDIT.md` → `??`) |
| Law | Architecture v2 **§67 Phase 0**, **§73.A–D** (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 2726–2773); honesty rule: measured evidence, no greenwash |
| Siblings (read, not copied) | A28, A29, A57, A80, A100, C06, C08, C10, C13, C15, C18, C19, C20 |
| Product source modified | **No.** This report is the only write. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict — Rubber-stamp?

**YES. `PHASE0_AUDIT.md` is a rubber-stamp summary, not a measured Phase 0 audit.**

It is **not** a go-live greenwash. Live MT5, live FIX, and ML are correctly marked `MISSING`. Kafka/K8s/LLM are correctly out of scope. Those three “not done” rows are why this file can look honest at a glance.

It **is** a rubber-stamp because:

1. **No method.** Zero file paths with line numbers, zero hashes, zero `grep` counts, zero quotes. 26 non-blank lines cannot discharge §73.A–D.
2. **Over-grades.** Domain algorithms and `mt5-sdk` are stamped `EXISTS_AND_GOOD` while the C# hosts never call the SDK, DI always registers `FakeMt5BrokerConnector`, and `ShadowCopyEngine` is unused.
3. **Omits contradicting facts.** Health lies, anonymous CORS, live account id in `appsettings.json`, leftover `launchUrl: weatherforecast`, dead `IBrokerConnector`, empty `Configurations/`, unused Redis/SignalR packages, reconstructed-trade unique-index miss, §69 **0/12**.
4. **Never uses `UNSAFE`.** Architecture §73.B lists it. The file has no such row. `/api/health` hard-codes `healthy = true`; the FIX worker stamps `LoggedOn` every 15 s with no socket.
5. **Incomplete vs §73.** Headings cover §73.A and a thin §73.B table. **§73.C (implementation sequence) and §73.D (risk list) are absent.** Phase 0 exit (A28) is therefore **not** met by this file alone.

Do **not** treat `PHASE0_AUDIT.md` as the Phase 0 closeout. Use A28/A29/C13 plus this review. Do **not** rewrite product source from this stamp.

| Question | Answer |
|---|---|
| Is it a “everything is 95% / fully decompiled / go-live ready” fake PASS? | **No.** |
| Is it an evidence-backed §73 audit? | **No.** |
| Rubber-stamp? | **YES** — uncritical recap of intended shape. |
| Honest residual? | Live paths `MISSING` is the only load-bearing truth. |

---

## 1. Method (this review)

Read-only. Product trees were not edited.

| Step | What was measured |
|---|---|
| 1 | Full read of `D:\Prop\reports\PHASE0_AUDIT.md` (43 lines). |
| 2 | `list_dir` of `D:\Prop`, `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\mt5-sdk`, `D:\Prop\tests`, `D:\Prop\reports\swarm\20260818`. |
| 3 | Claim-by-claim `grep` / `read_file` of the product files the audit names or implies. |
| 4 | SHA-256 + line counts of the subject and 16 load-bearing product files (table §10). |
| 5 | `Test-Path` of the five historical `Class1.cs` paths; search for `Migrations` folders (exclude `bin`/`obj`/`vendor`/`node_modules`). |
| 6 | Product `*.cs` census: **apps=5, src=63, tests=11** (no `bin`/`obj`). |
| 7 | Compare to architecture §73.A–D and A28 Phase 0 checklist. |
| 8 | Did **not** run `dotnet test`, did **not** open sockets, did **not** edit `src/`, `apps/`, `tests/`, or `mt5-sdk`. |

Workspace default was `D:\Prop\src`. Evidence roots include the parent tree `D:\Prop` because the audit describes the whole solution.

---

## 2. What Phase 0 is required to contain

Architecture §73 (verbatim headings) plus A28 exit:

```text
A. Repository audit
   Current architecture / Existing MT5 / Existing DB tables/migrations /
   Existing trading/copy / Existing broker config / Security issues / Dead/duplicate code
B. Gap analysis — classify every component
   EXISTS_AND_GOOD / EXISTS_NEEDS_REFACTOR / MISSING / DEPRECATED / UNSAFE
C. Implementation sequence — exact files/modules/migrations
D. Risk list — SDK, Windows DLL, ticks, FIX headers, symbol/qty, live-account safety
```

`PHASE0_AUDIT.md` maps as:

| Required | Present in PHASE0? | Quality |
|---|---|---|
| §73.A seven bullets | Headings exist | One or two sentences each. No inventory. |
| §73.B classify **every** component | 8-row table | Misses API, workers, tests, auth, QuickFIX, outbox, recon, Redis, SignalR. Never uses `UNSAFE`. |
| §73.C sequence | **Absent** | Fail |
| §73.D risk list | **Absent** | Fail |
| §67 maps (schema, services, deployment, dead-code report) | **Absent** | Fail |
| Secrets-in-repo scan | One sentence | Misses committed `AccountId: 1369850` |

A28 already assigned the real Phase 0 work to A01–A105 / B-series. This 1.5 KB file cannot supersede them.

---

## 3. Claim-by-claim vs the tree

### 3.1 “Current architecture”

> .NET 8 solution `Mt5TraderIntelligence.sln` plus preserved C++ `mt5-sdk`. React dashboard under `apps/web`.

**TRUE (incomplete).**

| Measured | Path / fact |
|---|---|
| Solution exists | `D:\Prop\Mt5TraderIntelligence.sln` (7019 B, SHA-256 `AD5030070166D81E…`) |
| Projects | 10 C# projects + 3 solution folders: Domain, Application, Infrastructure, Mt5, Fix.CTrader, Api, Mt5Worker, FixWorker, Tests.Unit, Tests.Integration |
| React | `D:\Prop\apps\web` — Vite; `App.tsx` imports **15/15** page modules (C08) |
| C++ | `D:\Prop\mt5-sdk` — CMake C++20; first-party `src/`+`config/`+`tests/` = **33** `.cpp`/`.h`; `IMT5Client` present |
| Omitted | `docs/`, `docker-compose.yml` (Postgres 16 + Redis 7 + `dotnet run` API), empty `D:\Prop\services/` |

Directionally right. Not an architecture map.

### 3.2 “Existing MT5”

> Real Manager API lives in `mt5-sdk` (`IMT5Client`, local + HTTP). C# first useful version uses `FakeMt5BrokerConnector` so ingestion/reconstruction/scoring can be proven without live broker credentials.

**TRUE as inventory. FALSE as “first useful version proven.”**

| Fact | Evidence |
|---|---|
| `IMT5Client` exists | `D:\Prop\mt5-sdk\src\core\imt5_client.h` (9625 B, SHA-256 `CB8D632BB94ADC11…`). Local `MT5Manager` + `MT5HttpClient` implement it. |
| C# implementors of `IMt5BrokerConnector` | **Exactly one:** `FakeMt5BrokerConnector` (`src\Mt5\Connectors\FakeMt5BrokerConnector.cs:6`). |
| `HttpClient` / `DllImport` / `MT5APIManager` / `NativeLibrary` under `D:\Prop\src` | **Zero.** |
| Production DI | `DependencyInjection.cs` lines 31–34 **always** `DemoBrokerFactory.CreateDefault()`. No env switch. Connection-string presence does **not** change the connector. |
| Fake catalog | 4 accounts (`10001`,`10002`,`10003`,`99001`), 4 groups, canned XAUUSD round-trips. |
| Ingestion “connect” | `DealIngestionService.SyncBrokerAsync` calls `connector.ConnectAsync` — Fake sets `_connected = true`. No socket. |

C20 is correct that the C++ tree is **preserved**. PHASE0 is wrong to let the reader infer that C# ingestion talks to Manager. It talks to an in-memory double.

### 3.3 “Existing DB”

> No production migrations yet. `TraderDbContext` maps first-useful tables with compound unique indexes (`broker_id` + ticket/login). Development falls back to EF InMemory.

**MOSTLY TRUE. Compound-unique claim is overstated.**

| Check | Measured |
|---|---|
| `Migrations/` folders under `D:\Prop` (exclude vendor/bin/obj/node_modules) | **None** |
| Host bootstrap | `apps/api/Program.cs` lines 84–93: `EnsureCreatedAsync()` + `DemoSeeder.SeedAsync` |
| InMemory fallback | `DependencyInjection.cs` lines 19–28: empty / `<SECRET>` connection → `UseInMemoryDatabase("trader-intelligence")` |
| `Configurations/` | **Empty** (0 files). All fluent maps inline. |
| Entity files | **20** under `src/Domain/Entities` |
| `DbSet`s | 20, including `DbSet<Broker> Brokers` (standard plural **name**, singular **type**) |

Compound identity (C06, re-checked on `TraderDbContext.cs` SHA-256 `AFB195ACB2C061EF…`):

| Table | Index | Unique? | §10 |
|---|---|---|---|
| `mt5_accounts` | `(BrokerId, Login)` | **Yes** | Yes |
| `mt5_deals` | `(BrokerId, DealTicket)` | **Yes** | Yes |
| `mt5_positions_current` | `(BrokerId, PositionTicket)` | **Yes** | Approximate |
| `reconstructed_trades` | `(BrokerId, Login, PositionId, OpenedAt)` | **No** | **Fail** |
| `sync_checkpoints` | `(BrokerId, Login, Stream)` | Yes | Wrong shape vs A61 |
| `mt5_orders` | — | — | **Table missing** |

PHASE0’s parenthetical “`broker_id` + ticket/login” is the intended law, not the measured model. `HasKey` is always surrogate `Guid Id`. Zero `HasAlternateKey`, zero named `*_uk`.

### 3.4 “Trading / copy”

> Shadow engine exists. Live copy is feature-flagged **off**. No NewOrderSingle send path is armed.

**Split.**

| Sub-claim | Verdict | Evidence |
|---|---|---|
| Shadow engine exists | **TRUE** | `src/Domain/Shadow/ShadowCopyEngine.cs` (3249 B). `SimulateEntry` / `SimulateExit` / `MarkToMarket`. |
| Shadow engine is in the product pipeline | **FALSE** | `grep ShadowCopyEngine` on product `*.cs`: definition only. Tests mention it only to assert `QuantityNormalizer` is unused by it. No DI registration. Shadow React page is a static stub (`ShadowPortfolioPage.tsx`). |
| Live copy feature-flagged **off** | **OVERSTATED** | `CTraderFixOptions.RealCopyExecutionEnabled` defaults `false` (`CTraderFixOptions.cs` lines 32–35). `appsettings.json` repeats `"RealCopyExecutionEnabled": false`. Worker reads `CTrader:RealCopyExecutionEnabled` (default false). **There is no send function for the flag to gate.** A08/C19: fail-closed **by absence**, not a proven gate. |
| No NewOrderSingle send path armed | **TRUE** | `grep` `NewOrderSingle` in `src/*.cs`: comment on the option + `MayRetryNewOrderSingle` helper. `Fix.CTrader.csproj` has **zero** QuickFIX packages. Zero `35=D` builder. Worker never constructs `FixSimulationHarness`. |

FIX worker honesty hole (omitted by PHASE0):

```39:44:D:\Prop\apps\fix-worker\Worker.cs
                trade.Status = real ? FixSessionStatus.LoggedOn : FixSessionStatus.LoggedOn;
            }

            await db.SaveChangesAsync(stoppingToken);
            if (real)
                _logger.LogWarning("Real copy is enabled in config, but worker still refuses NewOrderSingle until risk/reconciliation gates pass.");
```

Both branches write `LoggedOn`. When `real==true` the worker **logs** a refusal; it does not encode or send. When `real==false` it still paints TRADE `LoggedOn` every 15 s. That is a **health lie**, not a feature flag.

Dashboard `RealCopyEnabled` is hard-coded `false` in `EfDashboardQueries` (`OverviewDto` last arg line 42; `RiskDashboardDto` line 159) and does **not** read the binder.

### 3.5 “Broker config”

> Achiever + StarwaveFX in `.env.example` (placeholders for secrets).

**TRUE.**

`D:\Prop\.env.example` (3408 B, SHA-256 `56C81786F2B4DCCF…`):

- Achiever: `MT5_SERVER=57.128.141.65`, `MT5_LOGIN=2027`, `MT5_PASSWORD=<SECRET>`
- StarwaveFX: `MT5_STARWAVEFX_SERVER=84.201.6.142`, `MT5_STARWAVEFX_LOGIN=9904`, `MT5_STARWAVEFX_PASSWORD=<SECRET>`
- FIX: `CTRADER_FIX_PASSWORD=<SECRET>`, `TARGET_COMP_ID=cServer`, `REAL_COPY_EXECUTION_ENABLED=false`

Omitted: those env keys are **not bound** by `apps/mt5-worker` (`appsettings.json` is logging-only). Seeder writes the same hosts/logins as **EF metadata**, not as live sessions. C++ `AppConfig` remains **single-broker**.

### 3.6 “Security”

> Passwords are not in appsettings. Dashboard contracts omit secrets. `cServer` case preserved.

**NARROWLY TRUE on passwords. INCOMPLETE as a security audit. Omits `UNSAFE` surfaces.**

| Check | Measured |
|---|---|
| Real password strings in product `appsettings*.json` | **None.** `apps/api/appsettings.json` has `"Password": ""`. Worker appsettings are logging-only. |
| Live identity committed | `"AccountId": "1369850"` in `apps/api/appsettings.json` line 14. `CTraderFixOptions` defaults `SenderCompId = "live.pepperstone.1369850"` and `Host = "live-us-eqx-01.p.c-trader.com"`. Demo seeder persists those CompIDs. |
| Dashboard DTOs | `DashboardModels.cs`: no password/secret fields. `BrokerStatusDto.ManagerLoginMasked`. `FixSessionDto` has host/port/seq, not password. **TRUE.** |
| Masking implementation | `MaskLogin` keeps last-two-digits-zero (`2027` → `2000`). Weak, but it is a mask. |
| `cServer` vs `CSERVER` in product `*.cs` / `.env.example` / React | **Only `cServer`.** Options, seeder, harness, integration test, `FixSessionsPage.tsx`. |
| Auth / RBAC | **MISSING** (C18). `Program.cs`: no `AddAuthentication` / `AddAuthorization`. CORS `AllowAnyOrigin` + `AllowAnyHeader` + `AllowAnyMethod`. |
| `/api/health` | Hard-coded `healthy = true` for Achiever + QUOTE; Redis `healthy = false` is the only honest bit. |
| `GET /api/trades` | Filters `login` **without** `broker_id` (`Program.cs` lines 63–70). Cross-broker identity leak / mix. |

“Passwords are not in appsettings” is true if the bar is “no plaintext manager/FIX password.” It is not a security section. §73.A asked for **security issues**. PHASE0 listed none.

### 3.7 “Dead / duplicate”

> `Class1` / weatherforecast removed from API. Infrastructure briefly had plural EF types (`Brokers`) that did not match Domain; rewritten.

**PARTIAL.**

| Leftover | Now |
|---|---|
| `src/*/Class1.cs` (5 historical paths) | **All `Test-Path` = False** |
| `Class1` in product `*.cs` under `apps/` and `src/` | **0** |
| `WeatherForecast` type / `MapGet("/weatherforecast")` | **Gone** from `Program.cs` |
| `TraderIntelligence.Api.http` | Retargeted to `/health` + `/api/*` |
| IIS Express `launchUrl` | **Still `"weatherforecast"`** — `apps/api/Properties/launchSettings.json` line 35 |
| Entity type `Brokers` vs `Broker` | **Rewritten.** Current type is `Broker`; `DbSet<Broker> Brokers` is EF convention, not a type mismatch. |
| Dead `IBrokerConnector` | **Still present.** `src/Mt5/Connectors/IBrokerConnector.cs` (1557 B). `grep` consumers: **only its own declaration.** Parallel to `IMt5BrokerConnector`. PHASE0 does not mention it. |
| `UnitTest1.cs` filenames | Still on disk; types renamed to `SmokeTests` / `PlaceholderRemoved`. Integration placeholder is `Assert.True(true)`. |
| `Persistence/Configurations/` | Empty folder left behind. |
| `StackExchange.Redis` 2.8.0 | Package only; no multiplexer. |
| `Microsoft.AspNetCore.SignalR.Common` | Package only; no hub. |

PHASE0 reports the cleanup that happened and stops. C15 already measured the `launchUrl` leftover. A rubber-stamp “removed” sentence hides it.

### 3.8 Classification table

PHASE0 table vs this review:

| Component | PHASE0 | Independent class | Why |
|---|---|---|---|
| mt5-sdk C++ | `EXISTS_AND_GOOD` | **EXISTS_AND_GOOD as preserved C++ transport; not the C# collector** | Tree present, `IMT5Client` + `MT5Manager` + `MT5HttpClient`. Single-broker `AppConfig`. **Not referenced** by any C# host. C20. |
| Domain algorithms | `EXISTS_AND_GOOD (new)` | **EXISTS_NEEDS_REFACTOR** | Reconstructor / scorer / risk / symbol / volume **exist and have unit tests**. Shadow engine unused. `QuantityNormalizer` unused by engines (test **asserts** that). MFE/MAE stay `Unavailable`. No shadow-pipeline tests. “GOOD” requires a wired, tested path. |
| EF persistence | `EXISTS_NEEDS_REFACTOR` | **EXISTS_NEEDS_REFACTOR** | Agree. Add: 0 migrations, `EnsureCreated`, reconstructed unique miss, empty `Configurations/`. |
| Live MT5 connect | `MISSING` | **MISSING** | Agree. Fake is not a connect. |
| Live FIX logon | `MISSING` (simulator + session state only) | **MISSING**; session rows are **UNSAFE** as health | Simulator exists (`FixSimulationHarness`) and is **unwired** (C19). Worker stamps `ReadyForMarketData` / `LoggedOn`. Seeder starts TRADE as `LoggedOn`. |
| React dashboard | `EXISTS_NEEDS_REFACTOR` | **EXISTS_NEEDS_REFACTOR** | 15 pages routed. Stubs: Shadow, Live, Audit. Health/Recon dump JSON. No Login/Models (C08). No RBAC (C18). Unversioned `/api/*`. |
| ML | `MISSING` (correct — Phase 6) | **MISSING** | Agree. `FEATURE_ML_SCORING_ENABLED=false` in `.env.example`; `MlProbability` always `null` in DTO. Scoring page copy says XGBoost is not active. |
| Kafka/K8s/LLM | `DEPRECATED / not to build` | **Correctly absent** (`EXISTS_AND_GOOD` *as absence*, A80/§71) | Zero product hits for Kafka/Confluent/Kubernetes/ClickHouse/OpenAI. Compose is Postgres+Redis+API. |

**Missing from the table (material):**

| Component | Independent class |
|---|---|
| C# `IMt5BrokerConnector` production impl | **MISSING** (Fake only) |
| QuickFIX/n 1.14.1 / live initiator | **MISSING** |
| `GuardedNewOrderSingle` | **MISSING** (safe-by-absence) |
| Auth / RBAC | **MISSING** |
| API health / FIX worker session paint | **UNSAFE** |
| CORS `AllowAnyOrigin` | **UNSAFE** (demo) |
| Outbox dispatcher | **MISSING** (`OutboxEvent` table only) |
| Reconciliation engine | **MISSING** (API returns zeros) |
| Redis usage | **MISSING** (package only) |
| SignalR hub | **MISSING** (package only) |
| Shadow pipeline | **MISSING** (engine orphan) |
| §69 first useful version | **0 / 12 accepted** (C13) |

---

## 4. Health lies PHASE0 did not name

These are the reason the audit cannot be treated as honest even when it says “MISSING” for live connect.

| Surface | File | What it does |
|---|---|---|
| `/api/health` | `apps/api/Program.cs` 26–33 | Achiever `healthy = true`, details `"demo connector"`; QUOTE `healthy = true`; DB `healthy = true` without a ping. |
| Brokers API | `EfDashboardQueries.cs` 53 | `Connected = true`, `LastEventAt = DateTimeOffset.UtcNow` for every broker. Never calls `IsConnectedAsync`. |
| Overview MT5 health | same, line 39 | `brokers > 0` ⇒ `Mt5Healthy = true`. |
| Overview FIX health | same, 40–41 | Trusts `FixSessionStatus` painted by seeder/worker. |
| Demo seeder | `DemoSeeder.cs` 73, 90 | QUOTE starts `ReadyForMarketData`; TRADE starts `LoggedOn`. |
| FIX worker | `Worker.cs` 28–39 | Overwrites inbound timestamps and status every 15 s. Tautology `real ? LoggedOn : LoggedOn`. |
| React health page | `SystemHealthPage.tsx` | Pretty-prints the lie. |

C13 already scored §69.1 / §69.9 as **FAIL** for this reason. PHASE0’s “Live MT5 connect: MISSING” is compatible with that **only if** the reader does not also trust the dashboard.

---

## 5. Domain “EXISTS_AND_GOOD (new)” — measured depth

Product Domain is no longer `Class1`. That is real progress vs A01. It is not `GOOD`.

| Algorithm | Path | Tests | Wired? |
|---|---|---|---|
| `TradeReconstructor` | `Domain/Reconstruction` | `TradeReconstructionTests.cs` | Yes — `ReconstructionScoringService` |
| `BaselineScorer` | `Domain/Scoring` | `BaselineScorerTests.cs` | Yes |
| `RiskEngine` | `Domain/Risk` | `RiskEngineTests.cs` | **Not** on the worker/API path as a live gate (no copy intents from ingest) |
| `SymbolNormalizer` | `Domain/Instruments` | `SymbolNormalizerTests.cs` | Used by reconstructor |
| `VolumeConverter` | `Domain/Volume` | `VolumeConverterTests.cs` | Used by reconstructor |
| `QuantityNormalizer` | `Domain/Execution` | `QuantityNormalizerStepMinMaxTests.cs` | **Unused** by Shadow/Risk (test documents this) |
| `ExecutionOrderStateMachine` | `Domain/Execution` | `ExecutionAndSizingTests.cs` | No live session |
| `ShadowCopyEngine` | `Domain/Shadow` | **No dedicated tests** | **Unused** |
| `ClOrdIdFactory` / `CopyIntentExpiry` | `Domain/Execution` | partial | No send path |

“New” is true relative to the morning scaffold. `EXISTS_AND_GOOD` is a stamp.

---

## 6. What PHASE0 got right (do not throw away)

Keep these facts. They survive independent measurement.

1. Solution is .NET 8 + preserved C++ `mt5-sdk` + React under `apps/web`.
2. The only C# broker double is `FakeMt5BrokerConnector`.
3. There are **no** EF migrations. Dev can run InMemory.
4. There is **no** armed `NewOrderSingle` encoder/sender.
5. `.env.example` has Achiever + StarwaveFX + `<SECRET>` placeholders.
6. Dashboard contracts do not carry passwords.
7. Worktree `TargetCompId` is `cServer` (not silently folded).
8. `Class1` source and the weather **route** are gone.
9. Live Manager connect, live FIX logon, and ML are **MISSING**.
10. Kafka / Kubernetes / LLM are **not** to build and are absent from product code.

Those ten points would fit a status slide. They are not an audit.

---

## 7. Rubber-stamp mechanics (why this file failed)

| Mechanic | How PHASE0 does it |
|---|---|
| Checklist headings without inventory | Each §73.A bullet is one sentence. |
| Intent as fact | “first useful version uses Fake…” reads as a completed FUV. C13: **0/12 accepted**. |
| Soft language | “polish/RBAC later” for a dashboard with no auth and three stub pages. |
| Hidden leftovers | “Class1 / weatherforecast removed” — IIS `launchUrl` remains. |
| Grade inflation | `EXISTS_AND_GOOD` on Domain + SDK. |
| Safety conflation | “feature-flagged off” + “no send path armed” merged; the flag is unread by the dashboard and has nothing to disable. |
| No `UNSAFE` row | Health lies and open CORS unmentioned. |
| Truncated §73 | C and D missing; B incomplete. |
| No evidence trailer | Contrast A19/C15/C19/C20 (hashes, commands, line quotes). |

Sister file `docs/architecture.md` (“What exists now”) is the same optimistic tone. PHASE0 looks like that README compressed, not like A29.

---

## 8. Independent Phase 0 scoreboard (this snapshot)

Product `*.cs` excluding `bin`/`obj`: **79** (5 apps + 63 src + 11 tests).

| §73.A item | PHASE0 | This review |
|---|---|---|
| Current architecture | 2 sentences | 10 C# projects, React 15 pages, C++ SDK, compose Postgres/Redis, empty `services/` |
| Existing MT5 | Fake + SDK named | SDK unwired; Fake-only; DI hard-wired; 4 demo logins |
| Existing DB | No migrations + compound uniques | 0 migrations; 20 tables; **7** compound uniques; recon identity **not** unique |
| Trading / copy | Shadow exists; flag off; no NOS | Shadow unused; flag unread; NOS absent; worker health lie |
| Broker config | `.env.example` | True; **unbound** in workers |
| Security issues | “no passwords / no DTO secrets / cServer” | Empty password fields; **AccountId 1369850 committed**; no RBAC; CORS `*`; health lies |
| Dead / duplicate | Class1/weather gone; Brokers rewritten | `launchUrl` leftover; dead `IBrokerConnector`; empty Configurations; unused Redis/SignalR; `UnitTest1.cs` names |

| §73.B / extra | Class |
|---|---|
| Phase 0 **exit** (A28) | **FAIL** — this file is not sufficient |
| §69 first useful | **0 / 12** (C13 stands) |
| Live `NewOrderSingle` | **OFF** (absence) |
| Rubber-stamp? | **YES** |

---

## 9. Hashes of files used as evidence

| Bytes | Lines | SHA-256 prefix | Path |
|---:|---:|---|---|
| 1564 | 26 | `09CD30F357837987` | `reports/PHASE0_AUDIT.md` |
| 5951 | 151 | `AFB195ACB2C061EF` | `src/Infrastructure/Persistence/TraderDbContext.cs` |
| 1900 | 39 | `EF0E0E466A23F724` | `src/Infrastructure/DependencyInjection.cs` |
| 7049 | 145 | `AE7C1B1B01B1A573` | `src/Mt5/Connectors/FakeMt5BrokerConnector.cs` |
| 1557 | 36 | `6B7AA65F293AF43A` | `src/Mt5/Connectors/IBrokerConnector.cs` |
| 3249 | 83 | `F41578F95EBAE3E6` | `src/Domain/Shadow/ShadowCopyEngine.cs` |
| 2344 | 55 | `A354BBEA4665EE21` | `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` |
| 4658 | 86 | `E914FA984A377972` | `apps/api/Program.cs` |
| 431 | 21 | `8DCE4CBECDD1F8E7` | `apps/api/appsettings.json` |
| 1133 | 41 | `E092DE590CC74329` | `apps/api/Properties/launchSettings.json` |
| 1971 | 41 | `B48033A5A13C56DB` | `apps/fix-worker/Worker.cs` |
| 1882 | 40 | `5749970025C357A2` | `apps/mt5-worker/Worker.cs` |
| 2577 | 89 | `7A69C0E729A6962D` | `src/Application/Dashboard/DashboardModels.cs` |
| 7407 | 150 | `37A4DDD233057085` | `src/Infrastructure/Dashboard/EfDashboardQueries.cs` |
| 3408 | 103 | `56C81786F2B4DCCF` | `.env.example` |
| 7019 | 93 | `AD5030070166D81E` | `Mt5TraderIntelligence.sln` |
| 9625 | 153 | `CB8D632BB94ADC11` | `mt5-sdk/src/core/imt5_client.h` |

---

## 10. What this review is not

- Not a license to enable `RealCopyExecutionEnabled`.
- Not a claim that Domain should be deleted; it should be **reclassified**, not erased.
- Not a claim that `mt5-sdk` was rewritten (C20: preserved).
- Not an implementation sequence (that is still §73.C / A30).
- Not a product-source change.

---

## 11. Close

`D:\Prop\reports\PHASE0_AUDIT.md` is a **43-line rubber-stamp** of the demo shape. It ticks §73.A headings, over-grades two `EXISTS_AND_GOOD` rows, skips `UNSAFE`, and omits §73.C–D.

**Keep:** live MT5 / live FIX / ML are `MISSING`; no `NewOrderSingle` sender; no migrations; Kafka/K8s/LLM out of scope.

**Reject as Phase 0 closeout.** Measured first useful version remains **0/12**. Product source was not modified.
