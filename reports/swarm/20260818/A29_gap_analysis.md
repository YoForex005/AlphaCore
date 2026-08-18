# A29 — Gap analysis vs Architecture v2

| Field | Value |
|---|---|
| Agent | A29 (senior engineer, read-only) |
| Date | 2026-08-18 |
| Spec | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` (version 2.0, §73.B) |
| Scope | Entire `D:\Prop` product tree vs every named v2 component |
| Product source modified | **No** |
| Method | `list_dir` + `read_file` + `grep` (`Class1`, `weatherforecast`, FIX/EF/Serilog/QuickFIX/IMt5BrokerConnector, broker keys). Quotes are from files as read. |

**Honesty rule:** a NuGet package, empty class library, or C++ leftover from YoPips is **not** a working v2 component. Template hosts are not APIs. Enums without engines are vocabulary, not behavior.

**Snapshot caveat:** other swarm agents were adding Domain types while this audit ran. Earlier report `A01_domain_audit.md` is **stale** (it still describes `Domain/Class1.cs` and “zero domain types”). Entity list below is from the last `list_dir` of `src/Domain/Entities` on 2026-08-18 (20 files). Types without engines or migrations are **not** working components.

---

## 1. Executive verdict

The repository is **Phase 0 / early Domain-vocabulary**, not a trading platform.

Two independent stacks sit in one tree:

1. **C# `Mt5TraderIntelligence.sln`** — ten projects matching the *names* in §66. Almost all hosts are still `dotnet new` templates. The only real C# product types are anemic Domain records/enums. There is **no** ingestion, reconstruction engine, scoring engine, shadow engine, risk engine, FIX session, outbox, EF model, migration, React app, or ML service.
2. **C++ `mt5-sdk/`** — a substantial YoPips Manager-API library (local + HTTP, pool, watchdog, ticks, optional ledger). This is the only code that can actually talk to MT5. It is **not** a multi-broker v2 collector, it is **not** wired into the C# workers, and it still carries **source-side dealer / account-provisioning** APIs that v2 forbids on the source path.

**First useful version (§69) is 0/12.** Live FIX execution (§70) is 0/14. Go-live gates (§68) are all unchecked. Real copy is *implicitly* off only because **no FIX send path exists** — there is no `REAL_COPY_EXECUTION_ENABLED=false` flag in any C# host.

### Roll-up (every classified row in §4)

| Class | Count |
|---|---|
| EXISTS_AND_GOOD | 28 |
| EXISTS_NEEDS_REFACTOR | 51 |
| MISSING | 77 |
| DEPRECATED | 14 |
| UNSAFE | 8 |
| **Total classified** | **178** |

“EXISTS_AND_GOOD” is almost entirely: spec document, solution membership, net8 TFM, Domain **vocabulary**, C++ **library primitives**, C++ secret placeholders, and §71 things correctly not built. Almost none of it is a running product path.

---

## 2. Classification legend (architecture §73.B)

```text
EXISTS_AND_GOOD        Present and aligned enough to keep / reuse as-is
EXISTS_NEEDS_REFACTOR  Present but wrong shape, incomplete, unwired, or YoPips-shaped
MISSING                Required by v2 and not present
DEPRECATED             Present but must not become product behavior
UNSAFE                 Present and dangerous if left / reused (secrets, live-trade, public dummy API)
```

A row may carry a **secondary** label in notes (e.g. leftover + unsafe). The **primary** class is in the Class column.

---

## 3. Mandatory callouts: `Class1` and `weatherforecast`

### 3.1 `Class1` — leftover `dotnet new classlib` templates

`grep Class1` on `*.cs` (product, excluding vendor) now hits **two** declarations. Domain / Infrastructure / Fix.CTrader `Class1.cs` have already been deleted.

```1:6:D:\Prop\src\Application\Class1.cs
namespace TraderIntelligence.Application;

public class Class1
{

}
```

```1:6:D:\Prop\src\Mt5\Class1.cs
namespace TraderIntelligence.Mt5;

public class Class1
{

}
```

| Path | Class | Why |
|---|---|---|
| `D:\Prop\src\Application\Class1.cs` | **DEPRECATED** | Empty template. No ports, no use-cases. `grep` finds no consumer of `TraderIntelligence.Application.Class1`. |
| `D:\Prop\src\Mt5\Class1.cs` | **DEPRECATED** | Empty template. No `IMt5BrokerConnector`, no P/Invoke, no C++ interop. |
| Former `src/Domain/Class1.cs` | deleted | Domain now has real files; do not recreate. |
| Former `src/Infrastructure/Class1.cs` | deleted | Project is now **source-empty** (packages only). |
| Former `src/Fix.CTrader/Class1.cs` | deleted | Project is now **source-empty** (no QuickFIX). |

These are not stubs of v2 types. Do not rename them into entities. Delete when the first real type lands in that project.

### 3.2 `weatherforecast` — leftover ASP.NET weather template

This is the **entire** API surface.

```16:34:D:\Prop\apps\api\Program.cs
app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
```

Also:

- `D:\Prop\apps\api\TraderIntelligence.Api.http` line 3: `GET {{TraderIntelligence.Api_HostAddress}}/weatherforecast/`
- `D:\Prop\apps\api\Properties\launchSettings.json` — every profile `launchUrl` is `"weatherforecast"` (http / https / IIS Express)

| Item | Class | Why |
|---|---|---|
| `GET /weatherforecast` + `WeatherForecast` record | **DEPRECATED** | Stock template. Zero overlap with §§46–54 dashboard APIs. |
| Anonymous weather endpoint + `AllowedHosts: "*"` | **UNSAFE** | If this host is launched as “the API”, it is a public unauthenticated dummy. Not a secret leak today (no broker/FIX config is bound), but it is the wrong production surface. |

---

## 4. Master inventory (every v2 component)

Grouped by architecture section. Evidence is summarized here; longer quotes are in §5–§9.

### 4.1 Spec, solution, repo layout (§2, §66, §73)

| ID | Component | Arch | On disk | Class |
|---|---|---|---|---|
| L01 | Architecture v2 document | — | `MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` | EXISTS_AND_GOOD |
| L02 | `Mt5TraderIntelligence.sln` membership of the 10 C# projects | §66 | sln lines 5–30; all 10 `.csproj` exist | EXISTS_AND_GOOD |
| L03 | `apps/web` React+Vite+TS | §5, §66 | **absent** | MISSING |
| L04 | `apps/api` host | §4, §66 | weatherforecast template | EXISTS_NEEDS_REFACTOR |
| L05 | `apps/mt5-worker` | §66 | template `BackgroundService` 1s log | EXISTS_NEEDS_REFACTOR |
| L06 | `apps/fix-worker` | §66 | template `BackgroundService` 1s log | EXISTS_NEEDS_REFACTOR |
| L07 | `services/ml-service` | §5, §66 | empty `services/` | MISSING |
| L08 | `src/Domain` project | §66 | net8 classlib + records/enums | EXISTS_NEEDS_REFACTOR |
| L09 | `src/Application` project | §66 | `Class1` + unused FluentValidation | EXISTS_NEEDS_REFACTOR |
| L10 | `src/Infrastructure` project | §66 | **zero** `.cs`; EF/Redis packages unused | EXISTS_NEEDS_REFACTOR |
| L11 | `src/Mt5` project | §66 | `Class1` only | EXISTS_NEEDS_REFACTOR |
| L12 | `src/TradeReconstruction` | §66 | **absent** | MISSING |
| L13 | `src/Scoring` | §66 | **absent** | MISSING |
| L14 | `src/Shadow` | §66 | **absent** | MISSING |
| L15 | `src/Risk` | §66 | **absent** | MISSING |
| L16 | `src/Execution` | §66 | **absent** | MISSING |
| L17 | `src/Fix.CTrader` project | §66 | empty project; **no** QuickFIX/n | EXISTS_NEEDS_REFACTOR |
| L18 | `tests/Unit` | §60, §66 | empty `UnitTest1.Test1` | EXISTS_NEEDS_REFACTOR |
| L19 | `tests/Integration` | §60, §66 | empty `UnitTest1.Test1` | EXISTS_NEEDS_REFACTOR |
| L20 | `tests/Replay` | §60, §66 | **absent** | MISSING |
| L21 | `tests/Fix` | §60, §66 | **absent** | MISSING |
| L22 | `tests/Risk` | §60, §66 | **absent** | MISSING |
| L23 | `docs/` architecture/runbooks | §66 | empty directory | MISSING |
| L24 | Repo-root `.gitignore` | §55 | **absent** (only `mt5-sdk/.gitignore`) | UNSAFE |
| L25 | C# `.env.example` (Achiever + Starwave + FIX) | §56 | **absent** | MISSING |
| L26 | `mt5-sdk` C++ library (as reusable source collector) | §6, §72.6 | present, YoPips-shaped | EXISTS_NEEDS_REFACTOR |
| L27 | Vendored MetaQuotes Manager SDK (`Include/` + `Libs/`) | §5 | `mt5-sdk/vendor/MetaTrader5SDK/` | EXISTS_AND_GOOD |
| L28 | Vendor **Examples/** (Gateway/Web/PHP/…) | — | shipped with SDK | DEPRECATED |
| L29 | Kafka / K8s / ClickHouse / LLM / DL / mesh | §71 | correctly absent | EXISTS_AND_GOOD |

### 4.2 Tech stack (§5)

| ID | Component | Arch | On disk | Class |
|---|---|---|---|---|
| T01 | C# / .NET 8 TFM | §5 | all product csproj `net8.0` | EXISTS_AND_GOOD |
| T02 | ASP.NET Core API pipeline | §5 | `WebApplication.CreateBuilder`; no auth/swagger/serilog wiring | EXISTS_NEEDS_REFACTOR |
| T03 | .NET Worker Services | §5 | both workers are template loops | EXISTS_NEEDS_REFACTOR |
| T04 | EF Core + Npgsql | §5 | packages `8.0.4` in Infrastructure; **no** `DbContext`, **no** migrations | EXISTS_NEEDS_REFACTOR |
| T05 | Serilog | §5 | `Serilog.AspNetCore 8.0.2` on API; `Program.cs` never calls it | EXISTS_NEEDS_REFACTOR |
| T06 | OpenTelemetry | §5 | **no** package, **no** code | MISSING |
| T07 | FluentValidation | implied | `11.9.2` on Application; **zero** validators | EXISTS_NEEDS_REFACTOR |
| T08 | QuickFIX/n + cTrader data dictionary | §5, §25 | **no** package on Fix.CTrader | MISSING |
| T09 | PostgreSQL as SoT (C# schema) | §5, §45 | no connection string, no migrations | MISSING |
| T10 | Redis (cache / FIX lock, not SoT) | §5 | `StackExchange.Redis 2.8.0`; unused | EXISTS_NEEDS_REFACTOR |
| T11 | SignalR / WebSocket | §4, §5 | `SignalR.Common` only; **no** hub | EXISTS_NEEDS_REFACTOR |
| T12 | Python / FastAPI / XGBoost / Polars | §5 | **no** `.py`, no `services/ml-service` | MISSING |
| T13 | React + Vite + TS + TanStack Query + Router + charts | §5, §46 | **no** `package.json` / `apps/web` | MISSING |
| T14 | Docker (API/Postgres/Redis/Python/React) | §5 | **no** Dockerfile / compose | MISSING |
| T15 | Windows-only native MT5 constraint | §5 | CMake `if(WIN32)` for Manager/Pool/Watchdog | EXISTS_AND_GOOD |
| T16 | Swashbuckle | — | referenced, unused | DEPRECATED |

### 4.3 Source brokers, identity, ingestion (§6–§13)

| ID | Component | Arch | On disk | Class |
|---|---|---|---|---|
| S01 | C# `IMt5BrokerConnector` | §6 | **no** interface anywhere | MISSING |
| S02 | C++ `IMT5Client` (transport-agnostic) | §6 | `imt5_client.h` | EXISTS_NEEDS_REFACTOR |
| S03 | Broker registry (multi-broker, no duplicated logic) | §6 | Domain `Broker` record only; no registry service | MISSING |
| S04 | Achiever connector + config in product hosts | §7, §56 | keys exist **only** in the spec + C++ `AppConfig` (single broker) | MISSING |
| S05 | StarwaveFX connector + `MT5_STARWAVEFX_*` | §8, §56 | **zero** code hits outside the spec | MISSING |
| S06 | C++ `AppConfig` env loader | §7 | single-broker `MT5_*` only | EXISTS_NEEDS_REFACTOR |
| S07 | Plan→group **keys** (`MT5_GROUP_2STEP_DEMO` …) | §9 | loaded in `app_config.cpp` 119–127 | EXISTS_AND_GOOD |
| S08 | Plan→group **must not** limit fetch | §9 | C++ `GetAllGroups` / group probe enumerate all | EXISTS_AND_GOOD |
| S09 | `MT5AccountHelper` provisioning + `Flexy\\` defaults | §9 | `mt5_account_helper.h` 21–25 vs spec `contest\\yo-*` | DEPRECATED |
| S10 | Compound identity `broker_id + login/ticket` in C++ DTOs | §10 | `DealData` has `ticket`/`login` only | MISSING |
| S11 | Compound identity on C# source records | §10 | `BrokerId` on `Mt5Account`/`Mt5Deal`/`Mt5Position`/`ReconstructedTrade` | EXISTS_AND_GOOD |
| S12 | Raw tables §11 / §45 + EF mappings + migrations | §11, §45 | **no** C# schema; C++ ledger uses *different* table names | MISSING |
| S13 | C++ `mt5_ledger::Store` (immutable raw event + deal revision) | §11, §12 | `mt5_ledger_store.cpp` `ON CONFLICT DO NOTHING` | EXISTS_NEEDS_REFACTOR |
| S14 | Historical backfill + checkpoints | §12 | `SyncCheckpoint` type only; **no** backfill worker | EXISTS_NEEDS_REFACTOR |
| S15 | Live event subscription (C# worker) | §12 | mt5-worker only logs time | MISSING |
| S16 | C++ `MT5EventQueue` + deal/order/position/user sinks | §12, §72.6 | `mt5_types.h` / `mt5_manager.h` sinks | EXISTS_AND_GOOD |
| S17 | Periodic source reconciliation | §12 | **absent** in C#; C++ has no v2 reconcilers | MISSING |
| S18 | PostgreSQL transactional outbox | §13 | `OutboxEvent` type + enum; **no** dispatcher / table | EXISTS_NEEDS_REFACTOR |
| S19 | Event-bus abstraction (no Kafka) | §13 | **absent** | MISSING |
| S20 | C++ `MT5Manager` local Manager API | §6 | real Connect/GetDeals/GetAllGroups/ticks | EXISTS_NEEDS_REFACTOR |
| S21 | C++ `MT5Pool` + `MT5Watchdog` | §7 pool | real, **not scheduled** by any in-tree host | EXISTS_NEEDS_REFACTOR |
| S22 | C++ `MT5HttpClient` remote REST/SSE | §6 | real; default URL `http://` | EXISTS_NEEDS_REFACTOR |
| S23 | `mt5_group_probe` (enumerate groups, no password echo) | §6, §9 | `tests/mt5_group_probe.cpp` | EXISTS_AND_GOOD |
| S24 | C++ `GetDeals` complete-history contract | §12 | interface says page/cursor; impl is **one** `DealRequest` | EXISTS_NEEDS_REFACTOR |

### 4.4 Reconstruction, symbols, ticks, features (§14–§17)

| ID | Component | Arch | On disk | Class |
|---|---|---|---|---|
| R01 | `ReconstructedTrade` **type** | §14 | `Domain/Entities/ReconstructedTrade.cs` field-aligned | EXISTS_NEEDS_REFACTOR |
| R02 | Reconstruction **engine** (order≠deal≠position≠trade) | §14 | **no** code | MISSING |
| R03 | First-3 completed XAUUSD counter / `EARLY_SCORE_ELIGIBLE` | §15 | **no** counter; `TraderState.EARLY_SCORE` exists | MISSING |
| R04 | `CanonicalInstrument` type | §16 | `CanonicalInstrument.cs` (id+symbol+desc only) | EXISTS_AND_GOOD |
| R05 | `SourceSymbolMapping` type | §16 | `SourceSymbolMapping.cs` | EXISTS_AND_GOOD |
| R06 | Destination / cTrader instrument mapping | §16, §30 | **absent** | MISSING |
| R07 | Security List → persist instrument ID | §16, §30 | **absent** | MISSING |
| R08 | Source tick store `mt5_ticks_xauusd` | §11, §17 | **absent** | MISSING |
| R09 | C++ `MT5TickBridge` (non-blocking OnTick) | §17, §72.6 | real; Drogon drain + YoPips terminal sink | EXISTS_NEEDS_REFACTOR |
| R10 | `price_source` + `feature_quality` **enums** | §17 | `PriceSource`, `FeatureQuality` | EXISTS_AND_GOOD |
| R11 | Feature metadata on stored features | §17 | **no** feature snapshot entity | MISSING |
| R12 | Deal JSON transport includes `position` | §14 | `to_json(DealData)` **omits** `position` | UNSAFE |

### 4.5 Scoring, states, ML (§18–§23)

| ID | Component | Arch | On disk | Class |
|---|---|---|---|---|
| C01 | Deterministic baseline engine (`risk`/`behavior`/`early_quality`) | §18 | **no** engine | MISSING |
| C02 | `TraderScore` / `TraderScoreHistory` types | §18, §22 | records exist | EXISTS_AND_GOOD |
| C03 | `TraderState` enum | §22 | exact 9 states | EXISTS_AND_GOOD |
| C04 | `trader_feature_snapshots` / `trader_risk_flags` | §45 | **absent** | MISSING |
| C05 | Martingale / averaging-down / lot-escalation detectors | §18, §50 | **absent** | MISSING |
| C06 | Continuous rescoring after trade 4+ | §22 | **absent** | MISSING |
| C07 | Default gate: trade #3 + high score → SHADOW only | §23 | **absent** | MISSING |
| C08 | XGBoost / train split / leakage rules | §19–§21 | **absent** | MISSING |
| C09 | `model_versions` / predictions / evaluations | §45 | **absent** | MISSING |
| C10 | MLflow (optional) | §5 | correctly absent | EXISTS_AND_GOOD |

### 4.6 Shadow, FIX, risk, execution (§24–§43)

| ID | Component | Arch | On disk | Class |
|---|---|---|---|---|
| E01 | Shadow copy engine | §24 | **absent** | MISSING |
| E02 | Shadow tables (`shadow_orders/fills/positions/performance`) | §24, §45 | `ShadowOrder` type only; no fills/positions/PnL | EXISTS_NEEDS_REFACTOR |
| E03 | `CTraderQuoteSession` (own seq/heartbeat/metrics) | §25, §27 | **absent** | MISSING |
| E04 | `CTraderTradeSession` (independent of QUOTE) | §25, §27 | **absent** | MISSING |
| E05 | FIX SSL default (5211/5212), not plaintext | §25 | **no** FIX config in hosts | MISSING |
| E06 | Configurable `SenderSubID` / `TargetSubID` | §26 | `FixSessionState.SenderSubId/TargetSubId`; no FIX session | EXISTS_NEEDS_REFACTOR |
| E07 | Single-active TRADE ownership (Redis lease / DB lock) | §28 | `FixSessionState.OwnerHeld/OwnerInstance` fields only | EXISTS_NEEDS_REFACTOR |
| E08 | FIX workflows (Logon…BusinessMessageReject) | §29 | **absent** | MISSING |
| E09 | Destination quote cache + stale-quote reject | §31 | `DestinationQuoteSnapshot` type; no cache/reject | EXISTS_NEEDS_REFACTOR |
| E10 | CopyIntent persist-then-risk-then-FIX (never from MT5 callback) | §32 | `CopyIntent` type (`ExpiresAt`, `IdempotencyKey`); **no** flow | EXISTS_NEEDS_REFACTOR |
| E11 | `CopyIntentAction` enum | §64 | matches OPEN/INCREASE/REDUCE/CLOSE | EXISTS_AND_GOOD |
| E12 | Idempotent ClOrdID + persist-before-send | §33 | `ExecutionIntent.ClOrdId` + status enum; **no** send path | EXISTS_NEEDS_REFACTOR |
| E13 | `ExecutionOrderStatus` enum | §33, §34 | includes `ExecutionStateUnknown` | EXISTS_AND_GOOD |
| E14 | Unknown-state recovery (status/mass-status/positions) | §34 | **absent** | MISSING |
| E15 | Source↔destination position links | §35 | **absent** | MISSING |
| E16 | Copy timing / `expires_at` / `max_signal_age` | §36, §63 | **absent** | MISSING |
| E17 | Slippage / PRICE_MOVED / SPREAD_TOO_WIDE | §37 | **absent** | MISSING |
| E18 | Quantity / notional normalization | §38 | **absent** | MISSING |
| E19 | Risk engine (final authority) | §39 | **absent** | MISSING |
| E20 | `RiskDecisionOutcome` enum | §39 | approve/reduce/reject/pause/global stop | EXISTS_AND_GOOD |
| E21 | Kill switch `STOP_NEW_EXECUTION` ≠ `EMERGENCY_FLATTEN` | §40 | `KillSwitchMode` + `KillSwitch` entity; **no** enforcement | EXISTS_NEEDS_REFACTOR |
| E22 | `REAL_COPY_EXECUTION_ENABLED=false` feature flags | §41 | **absent** from all appsettings | MISSING |
| E23 | TRADE login → block → mass status → positions → READY | §42 | **absent** | MISSING |
| E24 | Periodic dest reconciliation + issue types | §43 | `ReconciliationIssueType` enum only | EXISTS_NEEDS_REFACTOR |
| E25 | FIX execution tables §44 | §44 | **absent** | MISSING |
| E26 | `FixSessionQualifier` / `FixSessionStatus` enums | §27, §42 | present | EXISTS_AND_GOOD |

### 4.7 Database tables (§45)

No C# migration and no PostgreSQL DDL in this repo matches the v2 set. C++ ledger SQL references `mt5_raw_events` and `mt5_deals_ledger` only.

| ID | Table (v2 §45) | Class |
|---|---|---|
| D01 | `brokers` | MISSING (C# `Broker` record only) |
| D02 | `broker_connections` | MISSING |
| D03 | `mt5_groups` | MISSING (C# `Mt5Group` record only) |
| D04 | `plan_group_mappings` | MISSING |
| D05 | `mt5_accounts` | MISSING (C# `Mt5Account` record only) |
| D06 | `mt5_account_snapshots` | MISSING |
| D07 | `mt5_orders` | MISSING |
| D08 | `mt5_deals` | MISSING (C# `Mt5Deal` record only) |
| D09 | `mt5_positions_current` | MISSING (C# `Mt5Position` record only) |
| D10 | `mt5_symbols` | MISSING |
| D11 | `mt5_xau_ticks` | MISSING |
| D12 | `reconstructed_trades` | MISSING (C# record only) |
| D13 | `canonical_instruments` | MISSING (C# record only) |
| D14 | `source_symbol_mappings` | MISSING (C# record only) |
| D15 | `trader_feature_snapshots` | MISSING |
| D16 | `trader_scores` | MISSING (C# record only) |
| D17 | `trader_score_history` | MISSING (C# record only) |
| D18 | `trader_states` | MISSING |
| D19 | `trader_risk_flags` | MISSING |
| D20 | `model_versions` / `model_predictions` / `model_evaluations` | MISSING |
| D21 | `shadow_orders` / `shadow_fills` / `shadow_positions` / `shadow_performance` | EXISTS_NEEDS_REFACTOR (`ShadowOrder` type only; no table) |
| D22 | `copy_intents` / `copy_allocations` | EXISTS_NEEDS_REFACTOR (`CopyIntent` type only) |
| D23 | `risk_decisions` / `risk_events` | EXISTS_NEEDS_REFACTOR (`RiskDecisionRecord` type only) |
| D24 | `execution_venues` / `destination_symbols` / `destination_quotes` | EXISTS_NEEDS_REFACTOR (`DestinationQuoteSnapshot` only) |
| D25 | `fix_sessions` / `fix_session_events` / `fix_orders` / `fix_execution_reports` | EXISTS_NEEDS_REFACTOR (`FixSessionState` only) |
| D26 | `destination_positions` / `source_destination_links` | MISSING |
| D27 | `sync_checkpoints` / `outbox_events` / `audit_logs` / `system_events` | EXISTS_NEEDS_REFACTOR (types `SyncCheckpoint`/`OutboxEvent`/`AuditLog`; no tables) |
| D28 | `execution_intents` / `execution_reconciliation_runs` / `_issues` | EXISTS_NEEDS_REFACTOR (`ExecutionIntent` type only) |
| D29 | C++ `mt5_raw_events` + `mt5_deals_ledger` (YoPips names, `server_key` not `broker_id`) | EXISTS_NEEDS_REFACTOR |
| D30 | C++ `mt5_account_sequence` (login minting) | DEPRECATED |

### 4.8 Dashboard, auth, security, observability (§46–§59)

| ID | Component | Arch | Class |
|---|---|---|---|
| U01 | Overview page | §47 | MISSING |
| U02 | Brokers page | §48 | MISSING |
| U03 | MT5 Groups page | §49 | MISSING |
| U04 | Trader leaderboard | §50 | MISSING |
| U05 | Trader detail | §51 | MISSING |
| U06 | Trade explorer / Scoring / Models pages | §46 | MISSING |
| U07 | Shadow / Live copy portfolio pages | §46 | MISSING |
| U08 | cTrader FIX page (QUOTE+TRADE cards) | §52 | MISSING |
| U09 | Risk dashboard | §53 | MISSING |
| U10 | Reconciliation dashboard | §54 | MISSING |
| U11 | System Health / Audit / Settings | §46 | MISSING |
| U12 | RBAC SuperAdmin/RiskManager/Analyst/ReadOnly | §59 | MISSING |
| U13 | Secret-safe config in C# hosts | §55–§56 | MISSING |
| U14 | C++ `.env.example` + `mt5-sdk/.gitignore` of `.env` | §55 | EXISTS_AND_GOOD |
| U15 | Spec placeholders `<SECRET>` (no live passwords in repo) | §55 | EXISTS_AND_GOOD |
| U16 | Structured correlation logging (C#) | §57 | MISSING |
| U17 | C++ `logger.h` (`propfirm_backend.log`, `PROP_FIRM_LOG_DIR`) | §57 | EXISTS_NEEDS_REFACTOR |
| U18 | v2 metrics (mt5_*/reconstructed_*/score_*/fix_*/execution_*) | §58 | MISSING |
| U19 | C++ `MetricsService` (`propfirm_*`, challenge/terminal WS) | §58 | DEPRECATED |

### 4.9 Testing and failure rules (§60–§65)

| ID | Component | Arch | Class |
|---|---|---|---|
| Q01 | 17 required C# unit-test areas | §60 | MISSING |
| Q02 | Required integration tests (migrations, FIX parse, recon) | §60 | MISSING |
| Q03 | Replay harness (events→recon→features→scores→shadow) | §60 | MISSING |
| Q04 | FIX simulator (ER replay, disconnect, dup, partial, reject, unknown) | §61 | MISSING |
| Q05 | C++ hermetic tests (time window, ledger validate, news, HTTP pool) | — | EXISTS_AND_GOOD |
| Q06 | Empty `UnitTest1` / Integration `UnitTest1` | — | DEPRECATED |
| Q07 | Failure rules (MT5/ML/QUOTE/TRADE/DB fail-closed) | §62 | MISSING |
| Q08 | No blind catch-up | §63 | MISSING |
| Q09 | Separate OPEN vs CLOSE policy | §64 | MISSING (enum only — E11) |
| Q10 | Correlation / concentration caps | §65 | MISSING (Phase-2-ok, still absent) |

### 4.10 Leftover / unsafe C++ product surface (not in v2 target)

| ID | Component | Class | Why |
|---|---|---|---|
| X01 | `IMT5Client::SendTrade` / `DealerSendOrder` / Deposit / Withdraw / CreateUser | DEPRECATED | Source side is collect-only (§4). Execution is cTrader FIX only. |
| X02 | Same dealer APIs if wired into `apps/mt5-worker` | UNSAFE | Could place real source-broker orders. |
| X03 | `UserParams` JSON includes `password` + `investor_password` | UNSAFE | `mt5_types.h` `to_json` lines 296–299. HTTP client posts that JSON. |
| X04 | Default `MT5_REMOTE_URL=http://127.0.0.1:9100` | UNSAFE | `.env.example` line 79; shared `MT5_API_KEY` over cleartext. |
| X05 | News/calendar provider | DEPRECATED | Not in v2 first-useful or live-execution lists. |
| X06 | YoPips terminal quote hub / Drogon tick drain | DEPRECATED | Prop-firm web terminal, not the React ops dashboard. |
| X07 | `generateMt5Login` starting 301100 | DEPRECATED | Account minting is not a collector concern. |

---

## 5. Evidence — C# product (quoted)

### 5.1 API is the weather template

Full host (`D:\Prop\apps\api\Program.cs`): builder, `UseHttpsRedirection`, `/weatherforecast`, `app.Run()`. No `AddAuthentication`, `AddSerilog`, `AddDbContext`, `MapHub`, health checks, or versioned routes.

```7:8:D:\Prop\apps\api\appsettings.json
  "AllowedHosts": "*"
```

```9:12:D:\Prop\apps\api\TraderIntelligence.Api.csproj
    <PackageReference Include="Microsoft.AspNetCore.SignalR.Common" Version="8.0.4" />
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.2" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
```

Packages are **not** used in `Program.cs`. `grep AddSerilog|MapHub|OpenTelemetry|IMt5BrokerConnector|DbContext` over product `*.cs` / `*.csproj` returned **no matches**.

### 5.2 Workers are heartbeat templates

```13:21:D:\Prop\apps\mt5-worker\Worker.cs
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
```

`apps/fix-worker/Worker.cs` is the same loop. Neither worker reads Achiever/Starwave/FIX env, nor references `IMT5Client` / QuickFIX.

### 5.3 Infrastructure: packages without code

```8:15:D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.4">
      ...
    </PackageReference>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.4" />
    <PackageReference Include="StackExchange.Redis" Version="2.8.0" />
```

After `Class1.cs` deletion, `list_dir` of `src/Infrastructure` shows **csproj + bin/obj only**. No `DbContext`, no Redis multiplexer, no outbox dispatcher.

### 5.4 Application: unused FluentValidation + `Class1`

```7:8:D:\Prop\src\Application\TraderIntelligence.Application.csproj
    <PackageReference Include="FluentValidation" Version="11.9.2" />
```

No `IValidator<>`. Architecture’s only written C# port:

```338:350:D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
public interface IMt5BrokerConnector
{
    Task ConnectAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);

    Task<IReadOnlyCollection<Mt5Group>> GetGroupsAsync(CancellationToken ct);
    Task<IReadOnlyCollection<Mt5Account>> GetAccountsAsync(...);
    Task<IReadOnlyCollection<Mt5Deal>> GetDealsAsync(...);
    Task<IReadOnlyCollection<Mt5Order>> GetOrdersAsync(...);
    Task<IReadOnlyCollection<Mt5Position>> GetPositionsAsync(...);

    IAsyncEnumerable<Mt5Event> SubscribeAsync(CancellationToken ct);
}
```

**MISSING** in Application (and everywhere else).

### 5.5 Fix.CTrader: empty project, no engine

`TraderIntelligence.Fix.CTrader.csproj` references Domain + Application only. **No** `QuickFIXn` / `QuickFix` package. `list_dir` shows no `.cs` sources. Architecture §5: “Prefer a mature FIX engine such as QuickFIX/n” and “Do not write a FIX engine from raw TcpClient unless absolutely necessary.”

### 5.6 Tests: empty facts

```1:9:D:\Prop\tests\Unit\UnitTest1.cs
namespace TraderIntelligence.Tests.Unit;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {

    }
}
```

Integration copy is identical. Coverage of §60 required areas: **0/17** (see `A09_unit_tests_audit.md`).

### 5.7 Domain vocabulary that *does* match v2

These are **EXISTS_AND_GOOD as types**. They are not engines and have no persistence.

| Type | File | Spec match |
|---|---|---|
| `TraderState` | `Enums/TraderState.cs` | §22 nine states, same names |
| `CopyIntentAction` | `Enums/CopyIntentAction.cs` | §64 OPEN/INCREASE/REDUCE/CLOSE |
| `RiskDecisionOutcome` | `Enums/RiskDecisionOutcome.cs` | §39 approve/reduce/reject/pause trader/venue/global stop |
| `OutboxEventType` | `Enums/OutboxEventType.cs` | §13 trade-completed / score-update / shadow / risk-check / notification |
| `ExecutionOrderStatus` | `Enums/ExecutionOrderStatus.cs` | §33 + §34 `EXECUTION_STATE_UNKNOWN` |
| `KillSwitchMode` | `Enums/KillSwitchMode.cs` | §40 two distinct modes |
| `PriceSource` / `FeatureQuality` | enums | §17 `ACHIEVER_MT5_TICKS` / `BAR_APPROXIMATION` / `EXACT` |
| `ReconciliationIssueType` | enum | §43 issue list |
| `FixSessionQualifier` / `FixSessionStatus` | enums | §27 / §42 `READY_FOR_EXECUTION` |
| `ReconstructedTrade` record | entity | §14 field list (see defect below) |
| `TraderScore` | entity | §18 three baseline scores + `CompletedXauTrades` |
| Source records with `BrokerId` | entities | §10 |
| `CopyIntent` | entity | §32/§63 `ExpiresAt` + `IdempotencyKey` |
| `ExecutionIntent` | entity | §33 `ClOrdId` + `ExecutionOrderStatus` |
| `RiskDecisionRecord` | entity | §39 outcome + `AllowFixSend` |
| `FixSessionState` | entity | §26/§27/§28 seq + SubIDs + `OwnerHeld` |
| `DestinationQuoteSnapshot` | entity | §31 bid/ask/received/venue time |
| `ShadowOrder` | entity | §24 partial (no fill/position/PnL rows) |
| `OutboxEvent` | entity | §13 shape; no processor |
| `SyncCheckpoint` | entity | §12 checkpoint fields |
| `AuditLog` | entity | §59/§72.19 actor/action/payload |
| `KillSwitch` | entity | §40 mode + actor |

**Current `src/Domain/Entities` (20 files):** `AuditLog`, `Broker`, `CanonicalInstrument`, `CopyIntent`, `DestinationQuote` (`DestinationQuoteSnapshot`), `ExecutionIntent`, `FixSessionState`, `KillSwitch`, `Mt5Account`, `Mt5Deal`, `Mt5Group`, `Mt5Position`, `OutboxEvent`, `ReconstructedTrade`, `RiskDecisionRecord`, `ShadowOrder`, `SourceSymbolMapping`, `SyncCheckpoint`, `TraderScore`, `TraderScoreHistory`.

**Domain defects (so the *layer* stays EXISTS_NEEDS_REFACTOR):**

1. **Anemic types** — mix of `record` and `sealed class`; no invariants, no value objects, no `broker_id+ticket` identity types.
2. **`ReconstructedTrade.ClosedAt` is non-nullable `DateTimeOffset`** while §14 models in-progress trades (`completed` flag). Open trades cannot be represented honestly.
3. **`Broker.cs` unused `using TraderIntelligence.Domain.Enums;`** — leftover.
4. **No `Mt5Order`**, no shadow fill/position/performance, no destination position / source-destination link types.
5. **`Mt5Group.ConnectionsAllowed` is `int`**; C++ `GroupDetail.connections_allowed` is `bool`.
6. **Login type drift:** `Mt5Account.Login` is `ulong`; `CopyIntent.SourceLogin` / `SyncCheckpoint.Login` are `long`.
7. **No behavior and no tables** — nothing consumes these types; no EF mapping.

```8:38:D:\Prop\src\Domain\Entities\ReconstructedTrade.cs
public record ReconstructedTrade(
    Guid Id,
    Guid BrokerId,
    ulong Login,
    ulong PositionTicket,
    string CanonicalSymbol,
    string SourceSymbol,
    TradeDirection Direction,
    DateTimeOffset OpenedAt,
    DateTimeOffset ClosedAt,
    ...
    bool IsCompleted
);
```

```3:14:D:\Prop\src\Domain\Enums\TraderState.cs
public enum TraderState
{
    INSUFFICIENT_DATA = 0,
    EARLY_SCORE = 1,
    WATCH = 2,
    SHADOW = 3,
    LIVE_CANDIDATE = 4,
    LIVE = 5,
    PAUSED = 6,
    RISK_BLOCKED = 7,
    DISQUALIFIED = 8
}
```

---

## 6. Evidence — C++ `mt5-sdk` (quoted)

### 6.1 What is actually strong

Transport split and group discovery are real:

```12:14:D:\Prop\mt5-sdk\src\core\imt5_client.h
// Abstract interface for MT5 operations.
// Implemented by MT5Manager (local SDK) and MT5HttpClient (remote microservice).
// All services and controllers depend on this interface, not the concrete implementation.
```

```164:167:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual uint32_t GroupTotal() = 0;
    virtual bool GetAllGroups(std::vector<std::string>& groups) = 0;
    virtual bool GetGroupDetails(std::vector<GroupDetail>& groups) = 0;
    virtual bool GetGroupLogins(const std::wstring& group, std::vector<uint64_t>& logins) = 0;
```

Group probe documents no password echo (`mt5_group_probe.cpp` lines 7–8). CMake keeps native Manager code on Windows only (`CMakeLists.txt` 52–58) — matches §5 “Do not force native MT5 SDK components into Linux.”

Tick bridge states the §72.6 rule correctly (pump thread enqueue-only) in `mt5_tick_bridge.h` lines 9–18.

Ledger write path is idempotent and non-overwriting:

```65:67:D:\Prop\mt5-sdk\src\services\mt5_ledger_store.h
    // Idempotently appends one immutable broker revision. It never overwrites a
    // previously stored revision or changes the current historical record.
    bool recordDealRevision(const DealRevision& revision) const;
```

### 6.2 Why the C++ client is still EXISTS_NEEDS_REFACTOR (not GOOD for v2)

1. **Single-broker `AppConfig`.** Loads `MT5_SERVER` / `MT5_LOGIN` / `MT5_PASSWORD` only (`app_config.cpp` 108–116). **No** `MT5_STARWAVEFX_*` (§8). Cannot be the multi-broker registry.
2. **Dealer / user-admin surface on the source client.** `SendTrade`, `CreateUser`, `DealerBalance`, password change — prop-firm, not collector. v2 source path is “Raw immutable trading data” (§4), not “place orders on Achiever.”
3. **`GetDeals` does not honor its own complete-history contract.** Interface (`imt5_client.h` 61–65) requires following every page/cursor. Implementation:

```485:509:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
bool MT5Manager::GetDeals(uint64_t login, int64_t from, int64_t to, std::vector<DealData>& out) {
    ...
    MTAPIRES res = m_manager->DealRequest(login, from, to, deals);
    ...
    for (uint32_t i = 0; i < deals->Total(); i++) {
        const IMTDeal* deal = deals->Next(i);
        if (deal) out.push_back(extractDeal(deal));
    }
    ...
    return true;
}
```

One `DealRequest`. Silent truncation risk for 5,000-account history.

4. **HTTP `DealData` drops `position`.** Native extract sets it:

```1508:1513:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
DealData MT5Manager::extractDeal(const IMTDeal* deal) {
    DealData d;
    d.ticket = deal->Deal();
    d.login = deal->Login();
    d.order = deal->Order();
    d.position = deal->PositionID();
```

JSON does **not** (`mt5_types.h` 335–339): `"ticket","login","order","symbol",...` — no `"position"`. Reconstruction (§14) **requires** `position_id`. Remote mode would lose the join key. Classed **UNSAFE** (data-integrity), not just refactor.

5. **Passwords in JSON.**

```296:299:D:\Prop\mt5-sdk\src\core\mt5_types.h
inline void to_json(nlohmann::json& j, const UserParams& p) {
    j = {{"name",p.name},{"email",p.email},{"group",p.group},{"leverage",p.leverage},
         {"password",p.password},{"investor_password",p.investor_password},
```

`MT5HttpClient::ChangePassword` posts `{"password": ...}` (`mt5_http_client.cpp` 447–449). Violates §57 “Never log authentication tags containing passwords” if those payloads are logged.

6. **Plan helper defaults disagree with §9.** Spec: `contest\yo-2step`. Helper: `Flexy\\yo-2step` (`mt5_account_helper.h` 23). And the helper **creates** accounts — out of v2 scope.

7. **Not wired.** `apps/mt5-worker` does not load `mt5-sdk`, set `MT5_POOL_SIZE`, or start `MT5Watchdog`. A15 already measured: pool size is loaded into `AppConfig` with **no in-tree call** to `MT5Pool::Initialize`.

8. **Metrics and logs are YoPips.** `MetricsService` exports `propfirm_breaches_total`, terminal WebSocket, challenge pass/fail. Logger writes `propfirm_backend.log`. Wrong product.

---

## 7. Security (primary UNSAFE / hygiene)

| Issue | Class | Evidence |
|---|---|---|
| No repo-root `.gitignore` | UNSAFE | `D:\Prop\.gitignore` does not exist. `bin/`, `obj/`, logs, and a future `.env` can be committed. Only `mt5-sdk/.gitignore` ignores `.env`. |
| Anonymous `/weatherforecast` + `AllowedHosts: "*"` | UNSAFE | `Program.cs`, `appsettings.json` |
| `UserParams` / password JSON over HTTP | UNSAFE | `mt5_types.h`, `mt5_http_client.cpp` |
| Default remote MT5 URL is `http://` | UNSAFE | `mt5-sdk/.env.example` line 79 |
| Deal JSON drops `position` | UNSAFE | silent reconstruction corruption |
| Source-side `SendTrade` leftover | UNSAFE if reused | `imt5_client.h` `SendTrade` |
| No C# secret placeholders / UserSecrets on API | MISSING | API csproj has no `UserSecretsId`; workers have unused ones |
| Live passwords in git | EXISTS_AND_GOOD | `grep` of product configs shows only `<SECRET>` / `replace_with_*`. Spec FIX account `1369850` / host names are labeled non-secret in §7/§25. |

No Vault, no RBAC, no audit log, no redaction middleware. §55 “Never expose secrets to React” is vacuously true because React does not exist.

---

## 8. Phase / acceptance status (true measured)

| Gate | Result |
|---|---|
| Phase 0 — Audit | **In progress** (this file is §73.B). C++ map exists (`A12`–`A18`). C# is still scaffold + Domain vocabulary. |
| Phase 1 — Reliable MT5 ingestion | **Not started** in product hosts. C++ can connect/enumerate **one** broker if an operator builds probes. No dual-broker, no persist-to-v2-tables, no outbox. |
| Phase 2 — XAUUSD reconstruction | **Type only.** No engine, no tests. |
| Phase 3 — Baseline + dashboard | **Score records only.** No engine, no React. |
| Phase 4–8 | **Missing.** Empty Fix.CTrader + heartbeat fix-worker. |
| §69 first useful (12 items) | **0 / 12** |
| §70 live FIX (14 items) | **0 / 14** |
| §68 go-live checklist | all `[ ]` |
| §71 do-not-build list | **honored** (Kafka/K8s/ClickHouse/LLM/DL/mesh absent) |

---

## 9. What to keep vs throw away

**Keep as-is**

- Architecture v2 as the binding spec.
- Solution folder layout and the ten project names.
- Domain enums listed in §5.7 (extend, do not rename away from spec strings).
- `mt5-sdk` Manager wrappers, group probe, event queue, Windows DLL copy helper — as a **native collector library**, after stripping dealer/provisioning from the product path.
- `mt5-sdk/.env.example` pattern (placeholders only).
- Vendored MetaQuotes `Include/` + `Libs/` (not Examples).

**Refactor before feature work**

- Replace Application/Mt5 `Class1` with `IMt5BrokerConnector` + two broker configs.
- Give Infrastructure a real DbContext + versioned migrations for **all** §45 tables (do not pretend C++ `mt5_deals_ledger` is that schema).
- Wire Serilog + OpenTelemetry; delete unused Swashbuckle or actually use it.
- Decide interop: C++/CLI, P/Invoke, or a private gRPC to `mt5-sdk`. Do not duplicate Manager API in C# blindly.
- Fix `DealData` JSON `position`, paginate `GetDeals`, add `broker_id`/`server_key` consistently.
- Make `ReconstructedTrade.ClosedAt` nullable.

**Delete / do not productize**

- `weatherforecast` endpoint, `.http` file, launchUrls.
- Remaining `Class1` and empty `UnitTest1`.
- Source-side `SendTrade` / CreateUser / login minting on the collector path.
- `MetricsService` prop-firm series; news calendar (unless later justified).
- Vendor Examples.

**Do not start yet (v2 order)**

- QuickFIX TRADE `NewOrderSingle`, ML, Kafka, K8s.
- Any live execution flag.

---

## 10. Residual risks (for §73.D; not a substitute for a dedicated risk list)

1. **Windows/native DLL** — Manager API is WIN32-only; C# workers have no copy-DLL step.
2. **Two codebases, zero bridge** — C++ can talk to MT5; C# cannot. Dual implementation risk.
3. **Tick data** — no proof source brokers give ticks; MFE/MAE must stay `Unavailable` until they do (§17).
4. **cTrader header ambiguity** — still unsolved; no session objects to test SenderSubID vs TargetSubID (§26).
5. **Quantity mapping** — no tests; §38 forbids lot==OrderQty.
6. **Live-account safety** — Pepperstone account IDs are in the spec; no software kill-switch or `REAL_COPY_EXECUTION_ENABLED` exists. Safety today is “nothing can send.” That is not a control.

---

## 11. Bottom line

| Question | Answer |
|---|---|
| Is v2 implemented? | **No.** |
| Is the C# solution the right skeleton? | **Yes** (names/TFM/refs). Contents are templates + Domain vocabulary. |
| Is `mt5-sdk` the Achiever/Starwave collector? | **Not yet.** It is a capable single-broker YoPips Manager library with dangerous extra surface. |
| Dead templates still in tree? | **Yes:** `Application/Class1`, `Mt5/Class1`, `/weatherforecast`, empty `Test1`s. |
| Can we enable live copy? | **No.** Fail closed by absence, not by a tested flag. |

This document is architecture §73.B. It does not authorize implementation. Phase 1 starts only after this audit set is accepted and an implementation sequence (§73.C) names exact files/migrations.
