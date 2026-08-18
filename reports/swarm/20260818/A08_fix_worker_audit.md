# A08 — `apps/fix-worker` audit vs Architecture §§25–34, 41–43, 67 Phase 4 & 7

| Field | Value |
|---|---|
| Agent | A08 (FIX worker / cTrader venue) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Product source edited | **No** (this file only) |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| Binding flag (this task) | `REAL_COPY_EXECUTION_ENABLED=false` |
| Official RoE (cross-check) | https://help.ctrader.com/fix/specification/ (fetched 2026-08-18) |
| Related swarm notes | `A11_solution_coverage.md`, `A28_phases_gates.md`, `A32_ctrader_fix_specification.md`, `A09_unit_tests_audit.md` |

**Method:** `list_dir` + `read_file` + `grep` of `apps/fix-worker`, `src/Fix.CTrader`, solution, tests, Domain FIX-adjacent types, and the named architecture sections. File hashes via SHA-256. Binary `fc` of worker vs `mt5-worker`. Official cTrader header/logon/NewOrderSingle rules fetched from Spotware RoE. Concurrent swarm writes were in flight; snapshot time is this report’s write. Nothing answered from memory.

---

## Verdict

**FAIL. Scaffold only. Phase 4 = 0/7. Phase 7 behavior = 0/6. Feature-flag control = 0/4.**

`apps/fix-worker` is a stock `dotnet new worker` host. It does not connect to cTrader, does not speak FIX 4.4, does not load venue credentials, and does not implement QUOTE, TRADE, Security List, quotes, orders, or reconciliation.

`src/Fix.CTrader` is an empty class library (no `.cs` source at snapshot). It is not a FIX adapter.

`REAL_COPY_EXECUTION_ENABLED=false` is **architecture policy only**. It is **not** present in `appsettings*.json`, `launchSettings.json`, environment, or C#. There is also **no** `NewOrderSingle` send path. Live copy is therefore **fail-closed by absence**, not by a real gate. That is the correct *current* safety outcome for this flag. It is **not** an implemented control. Do not treat this worker as “flag-gated execution.”

Classification of the worker itself: **EXISTS_NEEDS_IMPLEMENTATION** (project + solution membership good; behavior **MISSING**).  
Classification of live-send risk *today*: **SAFE_BY_ABSENCE**.  
Classification of the required flag gate: **MISSING**.

Do **not** start Phase 8. Do **not** add `NewOrderSingle` until Phase 4 + Phase 7 + risk + the four flags exist.

---

## 1. Inventory (source of truth)

### 1.1 `apps/fix-worker` — product files (exclude `obj/`, `bin/`)

| Path | Bytes | Lines | SHA-256 | Role | Classification |
|---|---:|---:|---|---|---|
| `D:\Prop\apps\fix-worker\Program.cs` | 181 | 5 | `8CB687EF5DC83EBBBDC728C57C3A7236686E6BB0F80AC1191DCF5EF0658AAF0A` | Generic host; registers `Worker` only | Template |
| `D:\Prop\apps\fix-worker\Worker.cs` | 628 | 20 | `54CDAF8A3A480BFC70383E1A554551A99A6743B528E364994E3417156D885542` | `BackgroundService` 1s log loop | Template / dead as FIX |
| `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj` | 856 | 17 | `D7572CBFF273089587D4A68B2A95EB1933C2DF388177F8665BE7C3CC338B5DB4` | net8.0 Worker SDK | Scaffold OK |
| `D:\Prop\apps\fix-worker\appsettings.json` | 137 | 8 | `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` | Logging only | Missing venue config |
| `D:\Prop\apps\fix-worker\appsettings.Development.json` | 137 | 8 | `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` | Identical to Production logging | Missing venue config |
| `D:\Prop\apps\fix-worker\Properties\launchSettings.json` | 296 | 12 | `25A750D823B04F229FBC49A690F997E969276FFB1A5E5F4EF198DF7DE7CBEF9B` | `DOTNET_ENVIRONMENT=Development` only | No FIX env |

No `bin/` for this project. `D:\Prop\build-release.log` restored/built **Mt5 + Mt5Worker + Domain + Application + Infrastructure** only. **FixWorker was not in that build.** `test-release.log` is empty.

`obj/project.assets.json` records **only** `Microsoft.Extensions.Hosting >= 8.0.1` and `"projectReferences": {}`. The **current** `.csproj` *does* list four project references. Restore artifacts are **stale / isolated**. Treat that as a footgun, not as evidence that references are absent from the project file.

### 1.2 `src/Fix.CTrader` — intended adapter

| Path | Snapshot | Classification |
|---|---|---|
| `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` | net8.0 classlib; refs Domain + Application; **no** QuickFIX/n | Empty adapter project |
| `D:\Prop\src\Fix.CTrader\*.cs` | **Zero** source files. Earlier in this session `Class1.cs` existed (77 bytes, empty class, SHA-256 `8118E415…`). It is **gone** now. | Empty assembly |

No FIX data dictionary, no session config, no TLS options, no message types.

### 1.3 What the process actually does

`Program.cs` (entire file):

```csharp
using TraderIntelligence.FixWorker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
```

`Worker.ExecuteAsync` logs `"Worker running at: {time}"` every 1000 ms and does nothing else.

`fc /b` of `Worker.cs` vs `D:\Prop\apps\mt5-worker\Worker.cs`: first difference at offset `0x1D` is the namespace bytes `Fix` vs `Mt5`. Same for `Program.cs` at `0x19`. These are **two copies of the same template**, not two venue adapters.

Project references (`Domain`, `Application`, `Infrastructure`, `Fix.CTrader`) are **unused** by any `using` or type. Dead compile-time edges.

### 1.4 Adjacent types (not owned by this worker; listed so they are not over-counted)

At snapshot, Domain had a single FIX-named type:

```csharp
// D:\Prop\src\Domain\Enums\FixSessionQualifier.cs
public enum FixSessionQualifier { Quote = 0, Trade = 1 }
```

Also present: `CopyIntentAction` (`Open/Increase/Reduce/CloseExposure`), `RiskDecisionOutcome`, `OutboxEventType.ShadowCopyIntent`. **No** `CopyIntent` entity, **no** `ExecutionIntent`, **no** `FixOrder`, **no** destination quote/position types. `CanonicalInstrument` is `{Id, Symbol, Description?}` only — no cTrader instrument id.

`grep` of `*.cs` / `*.json` / env / yml under `D:\Prop` for `REAL_COPY`, `CTRADER_FIX`, `NewOrderSingle`, `CTraderQuote`, `CTraderTrade`, `cl_ord_id`, `OrderMassStatus`, `RequestForPositions`: **architecture markdown only**. Zero product hits.

### 1.5 Tests / docs / ops

| Expected (§66 / §60 / §61) | Status |
|---|---|
| `tests/Fix` | **Missing** |
| Unit: ClOrdID generation, ExecutionReport transitions | **Missing** (empty `UnitTest1`) |
| Integration: QuickFIX session, ER parse, position recon, unknown-state | **Missing** |
| FIX simulation harness (§61) | **Missing** |
| `docs/ctrader-fix.md`, `docs/reconciliation.md` | `D:\Prop\docs` is empty |
| Docker / compose / repo-root `.env` | **None** (only `mt5-sdk/.env.example`) |
| Git | **No** `D:\Prop\.git` |

Unit + Integration test projects **reference** `Fix.CTrader` and have been built (Debug bin contains `TraderIntelligence.Fix.CTrader.dll`). They do **not** reference `TraderIntelligence.FixWorker`. There is no test of the worker host.

---

## 2. Architecture requirement matrix

Legend: **MISSING** = not in product. **VACUOUS** = requirement is satisfied only because the send/connect path does not exist. **SCAFFOLD** = folder/project exists, no behavior. **OK** = implemented and evidenced.

### §25 — Venue: cTrader / cServer FIX 4.4

| Requirement | Evidence | Result |
|---|---|---|
| Host `live-us-eqx-01.p.c-trader.com` | Not in any appsettings / env | **MISSING** |
| QUOTE SSL 5211 / plain 5201 | Not configured | **MISSING** |
| TRADE SSL 5212 / plain 5202 | Not configured | **MISSING** |
| SenderCompID `live.pepperstone.1369850` | Not configured | **MISSING** |
| TargetCompID configurable | Not configured | **MISSING** |
| Qualifiers QUOTE / TRADE | Only unused Domain enum | **MISSING** |
| Account 1369850 + password via secret | No options, no UserSecrets consumption | **MISSING** (good: no secret committed) |
| Production transport = SSL; plain not default | No ports at all | **MISSING** (cannot accidentally default to plain; also cannot use TLS) |

### §26 — Header mapping warning (binding)

Architecture: do not infer tag placement from the human form; make **both** `SenderSubID` and `TargetSubID` configurable; do not silently change `cServer` → `CSERVER`; prove Logon before execution.

Official RoE (2026-08-18), client header:

| Tag | Field | Required | Official value |
|---|---|---|---|
| 49 | `SenderCompID` | Yes | `<Environment>.<BrokerUID>.<Trader Login>` |
| 56 | `TargetCompID` | Yes | **`CSERVER`** |
| 57 | `TargetSubID` | Yes | **`QUOTE` or `TRADE`** (this *is* the session qualifier) |
| 50 | `SenderSubID` | No | Originator id; **must be `QUOTE` if `TargetSubID=QUOTE`** |

Official Logon request uses `56=CSERVER`, `57=TRADE`, `50=any_string`. Successful response **swaps** Comp/Sub IDs (`49=CSERVER`, `50=TRADE`, `57=any_string`). Architecture §56 example still says `CTRADER_FIX_*_TARGET_COMP_ID=cServer` (mixed case). That conflict is **unresolved in code** because no header is built.

| Requirement | Result |
|---|---|
| Configurable `SenderSubID` + `TargetSubID` | **MISSING** |
| Preserve broker-issued case; do not guess | **VACUOUS** (nothing hardcoded) |
| Prove Logon both sessions before execution | **MISSING** |
| Do not treat `SenderSubID` as the TRADE qualifier | **N/A** (no session object) |

**Future implementer trap:** mapping `SESSION_QUALIFIER` only into tag 50 will fail official RoE. QUOTE requires `57=QUOTE` **and** `50=QUOTE`. TRADE requires `57=TRADE` and a free-form `50`. Inbound 50/57 meanings are **not** the same as outbound.

### §27 — Session separation

| Independent state | QUOTE | TRADE |
|---|---|---|
| `CTraderQuoteSession` / `CTraderTradeSession` types | **MISSING** | **MISSING** |
| Connection | **MISSING** | **MISSING** |
| Sequence | **MISSING** | **MISSING** |
| Heartbeat / last in / last out | **MISSING** | **MISSING** |
| Reconnect / metrics / logs | **MISSING** | **MISSING** |

One shared sequence counter cannot exist because **zero** sequence counters exist. That is not compliance.

### §28 — FIX session ownership

| Requirement | Result |
|---|---|
| Single-active TRADE ownership (singleton / advisory lock / Redis lease / election) | **MISSING** |
| DB remains execution-state authority | **MISSING** (no execution tables, no DbContext) |
| New instance → session → reconcile → then accept intents | **MISSING** |

Infrastructure references `Npgsql.EntityFrameworkCore.PostgreSQL` and `StackExchange.Redis` but has **no** DbContext / lease type. Redis is available as a package only. Two copies of this worker today do **not** collide on FIX (they never connect). Two copies **after** a naive TRADE implementation **will** collide. Official FAQ / architecture §74: multiple simultaneous API connections can duplicate reports.

### §29 — Capabilities (minimum catalog)

| Workflow | MsgType | Result |
|---|---|---|
| Logon / Logout | A / 5 | **MISSING** |
| Heartbeat / TestRequest | 0 / 1 | **MISSING** |
| ResendRequest / SequenceReset / Reject | 2 / 4 / 3 | **MISSING** |
| SecurityListRequest / SecurityList | x / y | **MISSING** |
| MarketDataRequest / Snapshot / Incremental | V / W / X | **MISSING** |
| NewOrderSingle | D | **MISSING** — required **disabled** while `REAL_COPY_EXECUTION_ENABLED=false` |
| ExecutionReport | 8 | **MISSING** |
| OrderStatusRequest / OrderMassStatusRequest | H / AF | **MISSING** |
| RequestForPositions / PositionReport | AN / AP (cTrader catalog) | **MISSING** |
| OrderCancel / CancelReject / CancelReplace | F / 9 / G | **MISSING** |
| BusinessMessageReject | j | **MISSING** |

§29 also says: do not implement only “send market order” and call FIX done. Current risk is the opposite: **nothing** is implemented. The next failure mode is implementing only `35=D`.

QuickFIX/n is the preferred engine (§5, §1 item 8). **No** `QuickFIXn` / `QuickFix` package on FixWorker or Fix.CTrader. Do not write a raw `TcpClient` FIX engine.

cTrader tag 55 is a **Spotware Long instrument id**, not the text `XAUUSD`. No dictionary / custom fields exist.

### §30 — Instrument discovery

Required: session up → Security List Request → find XAUUSD → persist id / name / digits. **Do not hardcode** an instrument id from another account.

Result: **MISSING**. No hardcode found (**VACUOUS** good). `CanonicalInstrument` cannot store a venue id.

### §31 — Destination quote feed

Required cache: latest quote, receive timestamp, venue timestamp if any, symbol id, bid, ask. Risk rejects stale quotes via configurable max age.

Result: **MISSING**. No MD subscription, no cache, no stale-quote policy, no risk engine. QUOTE-down fail-closed (§62) cannot fire because there is no quote consumer.

### §32 — Trade execution flow

Required: source event → copy candidate → **persist CopyIntent** → RiskEngine → **persist ApprovedExecutionIntent** → FIX worker → NewOrderSingle → persist ER → update dest position → reconcile.

**Never** send FIX from an MT5 callback.

| Check | Result |
|---|---|
| Intent persist-before-send | **MISSING** |
| Worker consumes intents | **MISSING** |
| Worker does not send from MT5 callback | **VACUOUS OK** (no MT5, no send) |

### §33 — Idempotent order submission

Required persist-before-send: `execution_intent_id`, `cl_ord_id`, source ids, dest account, symbol, side, qty, `created_at`, `status`.

Required states: not sent / sent-ack-unknown / accepted / partial / filled / rejected / cancelled.

**Never** retry `NewOrderSingle` because TCP broke. Reconcile first.

Result: **MISSING**. No ClOrdID generator. No state machine. §60 unit test “ClOrdID generation” does not exist (see A09).

### §34 — Unknown execution state

Required: disconnect after send → `EXECUTION_STATE_UNKNOWN` → OrderStatus / MassStatus / ER / positions → then decide. **Do not** resend.

Result: **MISSING**. No unknown-state enum, no recovery. **VACUOUS**: no send means no unknown-send today.

### §41 — Real execution feature flags

Architecture default:

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

Meaning: connect, receive prices, request orders/positions, validate connectivity — **without** placing new real orders. `NewOrderSingle` requires `REAL_COPY_EXECUTION_ENABLED=true` **and** risk-engine healthy.

| Flag | In config | In code | Default honored |
|---|---|---|---|
| `CTRADER_FIX_ENABLED` | No | No | N/A |
| `CTRADER_FIX_QUOTE_ENABLED` | No | No | N/A |
| `CTRADER_FIX_TRADE_SESSION_ENABLED` | No | No | N/A |
| `REAL_COPY_EXECUTION_ENABLED` | No | No | **Policy only** |

**Task constraint `REAL_COPY_EXECUTION_ENABLED=false`:**

- As a **runtime switch**: **not implemented**. Setting the env var today would change **nothing**.
- As a **safety outcome** (no live copy): **held**, because no `35=D` builder/sender exists.
- As a **control you can audit in production**: **FAIL**.

This is the most important finding after “nothing is implemented.” The first lines of Phase 4/7 code must introduce the four flags and a hard refuse on outbound `35=D` unless the copy flag is true **and** risk is healthy. Default the copy flag to `false` in every committed config.

### §42 — Startup reconciliation (TRADE login)

Required: login → **block new executions** → `OrderMassStatusRequest` → `RequestForPositions` → consume ER/Position reports → compare DB → repair → only then `READY_FOR_EXECUTION`.

Result: **MISSING**. Worker never logs on. There is no READY state.

### §43 — Daily / periodic reconciliation

Required compare: internal open orders + dest positions vs cServer. Alert: unknown external, missing internal, qty/side mismatch, orphan ER, unexpected fill.

Result: **MISSING**. No tables `execution_reconciliation_runs` / `_issues` (those are §44; also absent).

---

## 3. Phase 4 — cTrader QUOTE integration (§67)

Architecture: **no live trading** in this phase.

| Deliverable | Status | Notes |
|---|---|---|
| SSL FIX quote session | **MISSING** | No engine, no TLS, no 5211 |
| Logon / session health | **MISSING** | No Logon `35=A`; no health card (§52) |
| Security List | **MISSING** | No `SecurityListRequest` |
| XAU instrument mapping | **MISSING** | No persist of Spotware id / digits |
| Live XAU quote | **MISSING** | No `35=V` / `W` / `X` |
| Quote persistence / cache | **MISSING** | No `destination_quotes` |
| Dashboard health | **MISSING** | API is still weather-forecast template |

**Phase 4 score: 0 / 7.** Not started. Scaffold of `/apps/fix-worker` + `/src/Fix.CTrader` is the only §66 alignment.

---

## 4. Phase 7 — cTrader TRADE read / reconciliation (§67)

Architecture: still keep real `NewOrderSingle` **disabled**.

| Deliverable | Status | Notes |
|---|---|---|
| SSL FIX trade session | **MISSING** | No 5212 session |
| `OrderMassStatusRequest` | **MISSING** | |
| `RequestForPositions` | **MISSING** | |
| ExecutionReport parser | **MISSING** | |
| PositionReport parser | **MISSING** | |
| Reconciliation | **MISSING** | |
| Real `NewOrderSingle` disabled | **VACUOUS PASS** | Disabled because **absent**, not because flagged |

**Phase 7 score: 0 / 6 implemented; NewOrderSingle-disabled = vacuous pass.**

Phase 8 items (risk engine, intent, idempotency, `35=D`, ER lifecycle, cancel/replace, unknown-state recovery, kill switch) are **correctly not built**. Keep it that way.

---

## 5. Safety assessment under `REAL_COPY_EXECUTION_ENABLED=false`

| Question | Answer |
|---|---|
| Can this process send a live order if started now? | **No.** No socket, no Logon, no `35=D`. |
| Can an env var turn live send on? | **No.** Flag is unread. |
| Can an env var turn live send off? | **N/A.** Nothing to turn off. |
| Are Pepperstone credentials in the repo? | **No** (only architecture placeholders with `<SECRET>`). |
| Does the worker log passwords? | **No** (it logs a timestamp). |
| Is TLS the production default? | **Unset.** |
| Is TRADE single-owner? | **Unset.** Safe only while disconnected. |
| Would enabling TRADE later without this flag be possible? | **Yes.** That is the control gap. |
| Does §62 “fail closed if DB down” apply? | Not yet; no execution memory **or** DB path. |

**Honest statement:** the platform cannot copy-trade through this worker today. That matches the *spirit* of the flag. The platform also cannot quote, cannot reconcile, and cannot prove FIX connectivity. Phase 4/7 are not “disabled”; they are **not written**.

---

## 6. Cross-cutting gaps that will block Phase 4/7 even after sessions exist

| Area | Architecture | Current |
|---|---|---|
| Engine | QuickFIX/n + **cTrader** data dictionary (§5, §1.8) | No package |
| Logging | Serilog; ids: `fix_session`, `cl_ord_id`, intent ids; never log tag 554 (§57) | Generic MEL; no redaction layer |
| Metrics | `fix_quote_connected`, `fix_trade_connected`, logon failures, reconnects, in/out, rejects, ER, unknown states (§58) | None |
| Dashboard §52 | Separate QUOTE/TRADE cards; never show password | API has no FIX page |
| Tables §44 | `fix_sessions`, `destination_quotes`, `fix_orders`, `fix_execution_reports`, `destination_positions`, recon tables | No EF model, no migrations |
| Tests §60/§61 | Parse recorded ER, replay MD, simulate disconnect / dup ER / partial / reject / unknown-state | Empty `Test1`; no harness |
| Ownership §28 | Redis lease is the natural fit (package already referenced by Infrastructure) | Unused |
| Quantity | Tag 38 max precision 0.01; do not pass MT5 lots through (§1.10, §72.14) | No conversion |
| Sequence | RoE: reset seq on Logon (`141=Y`) | No store |
| Username | Tag 553 = **numeric** login (`1369850`); 49 = dotted triple | Not mapped |

---

## 7. What is correctly *not* here

These absences are **good** given the flag and the phase law:

1. No `NewOrderSingle` builder.
2. No market-order-only shortcut.
3. No hardcoded Spotware instrument id.
4. No committed FIX password.
5. No Kafka / mesh / multi-region TRADE (§71).
6. Worker is **not** wired to an MT5 callback (template isolation).
7. Solution membership of FixWorker + Fix.CTrader is already correct (A11). Do not add a second FIX host.

Dead template `Class1` in Fix.CTrader was removed during this swarm window. Leaving the adapter **empty** is better than a fake session.

---

## 8. Risks (ordered)

| ID | Risk | Severity | Why |
|---|---|---|---|
| R1 | Implement `35=D` without the four flags | **Critical** | No gate exists; first send would be live on Pepperstone 1369850 |
| R2 | Two TRADE sessions after a naive deploy | **Critical** | No ownership; RoE warns duplicate reports |
| R3 | Wrong header map (`SenderSubID` as qualifier; `cServer` vs `CSERVER`) | **High** | Logon will fail or silently mis-route; §26 + official table disagree with the human form |
| R4 | Text `XAUUSD` in tag 55 | **High** | Official type is Long Spotware id |
| R5 | Retry on TCP drop | **High** | Classic double-fill; §33/§34 not encoded |
| R6 | Stale isolated `project.assets.json` | **Medium** | Restore graph does not list project refs |
| R7 | Identical worker loop as `mt5-worker` | **Low/ops** | Process looks “healthy” while doing no venue work |
| R8 | Concurrent Domain writes during audit | **Process** | Enums/entities appeared mid-audit; worker still does not consume them |

---

## 9. Recommended sequence (audit only — do not implement in this task)

Respect §67 order. This worker is **Phase 4+**. Phases 1–3 are still incomplete elsewhere.

When implementation is authorized:

1. **Encode the four flags first.** Commit `REAL_COPY_EXECUTION_ENABLED=false`. Make outbound `MsgType=D` unreachable unless the flag is true **and** risk is healthy. Log a structured refuse. Unit-test the refuse.
2. Add QuickFIX/n + a **cTrader** dictionary (do not assume generic FIX 4.4 is enough).
3. Configuration object: host, SSL ports, both CompIDs, **both** SubIDs, account, password from secrets, `CTRADER_FIX_USE_SSL=true`. No secrets in git.
4. **Phase 4:** independent QUOTE session, TLS 5211, Logon proof, Security List, persist XAU mapping, MD snapshot+incremental, quote cache + age, metrics, dashboard health. Still no TRADE send.
5. **Phase 7:** independent TRADE session, TLS 5212, single-owner lease, MassStatus + Positions, ER/Position parsers, persist, recon, block READY until match. `NewOrderSingle` still compiled out or flag-locked.
6. Only then Phase 8 (out of scope here).

Do not start a raw TCP FIX client. Do not share sequence files between QUOTE and TRADE. Do not enable plain 5201/5202 in Production.

---

## 10. Scorecard

| Slice | Score | Honest read |
|---|---|---|
| §66 folder / sln membership | 2 / 2 | `apps/fix-worker` + `src/Fix.CTrader` exist and are in `Mt5TraderIntelligence.sln` |
| §25 venue config | 0 / 8 | No host/ports/ids/SSL |
| §26 header configurability | 0 / 3 | No SubID options; Logon unproven |
| §27 two sessions | 0 / 2 | No session types |
| §28 ownership | 0 / 1 | No lease |
| §29 message catalog | 0 / 16 | None |
| §30 discovery | 0 / 1 | |
| §31 quotes | 0 / 1 | |
| §32 persist-then-send flow | 0 / 1 | Vacuous: no send from MT5 callback |
| §33 idempotency | 0 / 1 | |
| §34 unknown state | 0 / 1 | |
| §41 flags | 0 / 4 | Policy not encoded |
| §42 startup recon | 0 / 1 | |
| §43 periodic recon | 0 / 1 | |
| Phase 4 deliverables | **0 / 7** | |
| Phase 7 deliverables | **0 / 6** | |
| `NewOrderSingle` disabled | Vacuous **PASS** | Absence, not a flag |
| Live-copy possible today | **No** | Matches flag *intent* |

**Overall: FAIL for architecture compliance. PASS for “will not fire a live copy if you start the process right now.” Those are different questions.**

---

## 11. Evidence appendix

### 11.1 Worker body (complete)

```csharp
namespace TraderIntelligence.FixWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

### 11.2 Grep

Product tree (`*.cs`, `*.json`, env, yml) for `REAL_COPY_EXECUTION_ENABLED`, `CTRADER_FIX_`, `NewOrderSingle`, `QuickFix`, `CTraderQuoteSession`: **no product matches**. Architecture file only.

### 11.3 Official Logon example (RoE)

Client: `35=A|49=live.theBroker.12345|56=CSERVER|57=TRADE|50=any_string|98=0|108=30|141=Y|553=12345|554=passw0rd!`

This worker never emits a Logon. Tag 554 is not present in any product file.

### 11.4 Build evidence

`D:\Prop\build-release.log` (UTF-16 LE): succeeded for Mt5Worker path; FixWorker not listed. No `apps/fix-worker/bin`.

---

**End of A08.** Product source was not modified.
