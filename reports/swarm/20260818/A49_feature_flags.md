# A49 — Design flags `CTRADER_FIX_*` and `REAL_COPY_EXECUTION_ENABLED=false`: how workers enforce them

| Field | Value |
|---|---|
| Agent | A49 (feature-flag / worker enforcement) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A49_feature_flags.md` |
| Product source edited | **No** |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§25–28, 32–34, 40–43, 56, 61–64, 67–70, 72 |
| Binding siblings | `A05`, `A07`, `A08`, `A23`, `A24`, `A25` §6, `A26` §6.16, `A27`, `A28` |
| Method | Read architecture + sibling swarm specs + current `apps/*-worker` + `src/Fix.CTrader/Configuration/CTraderFixOptions.cs`. Nothing answered from memory. |

**Overall verdict:** The four §41 flags are **design law**. `REAL_COPY_EXECUTION_ENABLED` defaults **false** and is the only license for a new real `NewOrderSingle` (35=D). `CTRADER_FIX_*` session flags may be on so workers can connect, quote, and reconcile **without** placing orders. **No worker currently binds or enforces any of these flags.** Live copy is **safe by absence** (no send path), not by a gate. That is not an implemented control.

---

## 0. What this note is (and is not)

This is the worker-enforcement contract for:

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

It tells `apps/fix-worker` and `apps/mt5-worker` what they may start, what they may send, and what they must refuse. It does **not** implement the gate. Coding the binder / send function is a later task.

Related controls that are **not** feature flags (do not conflate):

| Control | Kind | Default |
|---|---|---|
| `STOP_NEW_EXECUTION` | Operational kill switch (§40) | off |
| `EMERGENCY_FLATTEN` | Separately permissioned flatten (§40) | off / unavailable |
| `READY_FOR_EXECUTION` | TRADE recon state (§42), not a config flag | not ready until reconcile |
| `CTRADER_FIX_USE_SSL` | Transport default | `true` |
| `CTRADER_FIX_DIAGNOSTIC_LOGON_ONLY` | Recommended extra gate (A25 §6.6) | `false` |

---

## 1. Flag catalog

### 1.1 Boolean gates (architecture §41)

These four names are the **execution safety surface**. They are independent. Turning TRADE on is not a license to send 35=D.

| Env name | Architecture default | Meaning when **off** | Meaning when **on** |
|---|---|---|---|
| `CTRADER_FIX_ENABLED` | `true` | Both FIX sessions stay `DISABLED`. Fix-worker may still run health / lease housekeeping. No sockets to cServer. | Session objects **may** start (each still gated by its own flag). |
| `CTRADER_FIX_QUOTE_ENABLED` | `true` | No QUOTE socket. Destination quotes unavailable. Shadow OPEN/INCREASE must fail closed (A24). Live OPEN/INCREASE that need a fresh price reject `QUOTE_UNAVAILABLE`. | QUOTE Logon + SecurityList + market-data subscription allowed. |
| `CTRADER_FIX_TRADE_SESSION_ENABLED` | `true` | No TRADE socket. No OrderMassStatus / RequestForPositions. No lease acquisition for TRADE. | TRADE Logon + read/reconcile + (policy) cancel/flatten. **Not** a license for `NewOrderSingle`. |
| `REAL_COPY_EXECUTION_ENABLED` | **`false`** | **Hard block** on outbound `NewOrderSingle` and any **new** real exposure. TRADE may still logon and request status/positions. | Necessary **but not sufficient** to send 35=D. See §4 conjunction. |

Architecture §41 purpose, quoted in effect: connect, receive prices, request orders/positions, validate FIX connectivity — **without automatically placing new real orders**.

Actual 35=D submission requires:

```env
REAL_COPY_EXECUTION_ENABLED=true
```

**plus** runtime risk-engine healthy state (§41 last sentence, §70.11).

### 1.2 `CTRADER_FIX_*` connection keys (not send licenses)

These are venue identity / transport. They do **not** authorize orders. They must never be treated as “if set, trade.”

| Env name | Role | Production default / issued value |
|---|---|---|
| `CTRADER_FIX_HOST` | Gateway | `live-us-eqx-01.p.c-trader.com` |
| `CTRADER_FIX_ACCOUNT_ID` | Logon Username (553) = numeric login | `1369850` |
| `CTRADER_FIX_PASSWORD` | Logon Password (554) | `<SECRET>` — never in repo, logs, or dashboard |
| `CTRADER_FIX_USE_SSL` | Transport | `true` (plain 5201/5202 must not be production default) |
| `CTRADER_FIX_QUOTE_SSL_PORT` | QUOTE TLS | `5211` |
| `CTRADER_FIX_QUOTE_PLAIN_PORT` | QUOTE plain (diagnostics only) | `5201` |
| `CTRADER_FIX_QUOTE_SENDER_COMP_ID` | tag 49 | `live.pepperstone.1369850` |
| `CTRADER_FIX_QUOTE_TARGET_COMP_ID` | tag 56 | issued form `cServer` (RoE table `CSERVER` — no silent case fold; A25 §3) |
| `CTRADER_FIX_QUOTE_SESSION_QUALIFIER` | tag 57 | `QUOTE` |
| `CTRADER_FIX_QUOTE_SENDER_SUB_ID` | tag 50 | broker-issued; on QUOTE RoE requires `QUOTE` if 57=`QUOTE` |
| `CTRADER_FIX_QUOTE_TARGET_SUB_ID` | optional override | broker-issued |
| `CTRADER_FIX_TRADE_SSL_PORT` | TRADE TLS | `5212` |
| `CTRADER_FIX_TRADE_PLAIN_PORT` | TRADE plain | `5202` |
| `CTRADER_FIX_TRADE_SENDER_COMP_ID` | tag 49 | `live.pepperstone.1369850` |
| `CTRADER_FIX_TRADE_TARGET_COMP_ID` | tag 56 | same case-fold warning |
| `CTRADER_FIX_TRADE_SESSION_QUALIFIER` | tag 57 | `TRADE` |
| `CTRADER_FIX_TRADE_SENDER_SUB_ID` | tag 50 | broker-issued originator string (`any_string` on TRADE, **not** `TRADE`) |
| `CTRADER_FIX_TRADE_TARGET_SUB_ID` | optional override | broker-issued |

Recommended extra gates (A25 §6.6; same family, keep explicit):

```env
CTRADER_FIX_ALLOW_PLAINTEXT=false
CTRADER_FIX_DIAGNOSTIC_LOGON_ONLY=false
CTRADER_FIX_HEARTBT_INT=30
CTRADER_FIX_RESET_SEQ_NUM=true
MAX_QUOTE_AGE_MS=<measured, not guessed>
```

`CTRADER_FIX_DIAGNOSTIC_LOGON_ONLY=true`: Logon + heartbeat + optional SecurityList, then stop. No MD subscription required, no TRADE application messages. Diagnostic Logon of live account `1369850` is allowed; diagnostic `NewOrderSingle` is **not**.

### 1.3 What these flags are **not**

- Not a substitute for `READY_FOR_EXECUTION` after TRADE Logon (§42).
- Not a substitute for TRADE ownership lease / fence (§28). Two FixWorker replicas with `CTRADER_FIX_TRADE_SESSION_ENABLED=true` without a lease is a P0 (cServer duplicates every 35=8).
- Not a substitute for persist-before-send + unique `ClOrdID` (§33).
- Not permission to send 35=D from `apps/mt5-worker` or an MT5 callback (§32).
- Not a dashboard button that may override a config `false` (A25 §6.4).

---

## 2. Worker responsibility map

Architecture §66 / A25 §10: `apps/fix-worker` is the host. Domain rules do not live in `Worker.cs`. `apps/mt5-worker` is source ingestion only.

| Process | Owns sockets? | Reads §41 flags? | May send 35=D? | Enforcement duty |
|---|---|---|---|---|
| `apps/fix-worker` | **Yes** (only process) | **Yes — primary binder** | **Only** if §4 conjunction is all true | Bind flags, start/stop sessions, **single send gate** immediately before socket write |
| `apps/mt5-worker` | **No** | Optional read for telemetry only | **Never** | Must not reference a FIX send API. Persist + outbox only. |
| `apps/api` | **No** | Yes, for dashboard snapshot | **Never** | Expose non-secret flags. SuperAdmin PATCH cannot raise `REAL_COPY` above the config floor. |
| Shadow engine (Application / later `src/Shadow`) | No TRADE send | Yes — QUOTE flags only | **Never** | OPEN/INCREASE require `CTRADER_FIX_ENABLED` + `CTRADER_FIX_QUOTE_ENABLED` + logged-on QUOTE. `REAL_COPY` stays false in Phase 5 (A24). |

`apps/api` and `apps/mt5-worker` **cannot** “enforce” live send by omission of a gate if they ever grow a FIX client. The rule is stronger: **those processes must not have a send function**. Enforcement is structural, not a boolean check.

---

## 3. How `fix-worker` must enforce (design)

Host composition (`Program.cs` / a registration extension — not business logic in the delay loop):

```text
bind env / secrets / appsettings  →  CTraderFixFeatureFlags + CTraderFixVenueOptions
        ↓
if !CTRADER_FIX_ENABLED:
      do not create QuickFIX initiators
      do not connect
      expose flags + “FIX disabled” health
        ↓
if CTRADER_FIX_QUOTE_ENABLED:
      start CTraderQuoteSession (TLS :5211)
else:
      leave QUOTE in DISABLED (TRADE may still run)
        ↓
if CTRADER_FIX_TRADE_SESSION_ENABLED:
      acquire TRADE lease (or refuse to open TRADE)
      start CTraderTradeSession (TLS :5212)
      on Logon: block new exec → mass status → positions → repair DB
      READY_FOR_EXECUTION only if reconcile clean
else:
      leave TRADE in DISABLED; do not take the lease
        ↓
drain ApprovedExecutionIntent only through SubmitNew()
```

### 3.1 Session start table

| Flag combination | QUOTE socket | TRADE socket | MD / SecurityList | Mass status / positions | 35=D |
|---|---|---|---|---|---|
| all four default (§41) | yes | yes | yes | yes | **no** |
| `CTRADER_FIX_ENABLED=false` | no | no | no | no | no |
| QUOTE off, TRADE on | no | yes | no (unless TRADE discovery) | yes | no (and OPEN/INCREASE fail quote-freshness) |
| TRADE off, QUOTE on | yes | no | yes | no | no |
| `REAL_COPY=true` but TRADE off | yes (if quote on) | no | quote only | no | no |
| `REAL_COPY=true` + TRADE on + not reconciled | yes | yes | yes | yes | **no** (`READY_FOR_EXECUTION` missing) |
| `DIAGNOSTIC_LOGON_ONLY=true` | Logon only | Logon only if TRADE on | optional SecurityList | **no** | **no** |

A session may be `DISABLED` by its own flag **without** tearing down the other session (A25 §2.3). QUOTE failure does not drop TRADE TCP; it **does** change execution policy (no new priced copy).

### 3.2 Single send gate (the only place 35=D may be built)

A25 §12 item 8: “send path is a single guarded function.” Suggested name: `IFixTradeGateway.SubmitNew` / `TrySendNewOrderSingle`.

Re-check **immediately before the socket write**, not only when the intent was approved minutes earlier:

```text
CTRADER_FIX_ENABLED = true
CTRADER_FIX_TRADE_SESSION_ENABLED = true
REAL_COPY_EXECUTION_ENABLED = true          -- config floor AND effective runtime
TRADE session = READY_FOR_EXECUTION         -- Logon + §42 reconcile, not “logged on”
lease owned + fencing token current
risk engine healthy
STOP_NEW_EXECUTION = false
venue pause / global stop = false
no unresolved EXECUTION_STATE_UNKNOWN
QUOTE usable if the order needs a fresh price
  (CTRADER_FIX_QUOTE_ENABLED
   AND quote_age <= MAX_QUOTE_AGE_MS
   AND instrument mapped from SecurityList — never hardcoded tag 55)
execution_intent persisted
cl_ord_id persisted
status = not_sent
intent not expired (expires_at / max_signal_age)
USE_SSL = true unless ALLOW_PLAINTEXT (prod: never)
DIAGNOSTIC_LOGON_ONLY = false
```

If any check fails: **do not send**. Persist a risk/execution decision. Fail closed. Do not queue an unlimited backlog (TRADE-down / flag-off intents expire — §62–§63).

`REAL_COPY_EXECUTION_ENABLED=true` with a missing or stale quote is still a reject (`QUOTE_STALE` / venue unhealthy).

### 3.3 What TRADE may still do while `REAL_COPY=false`

This is the whole point of the default. Phase 7 (and the §41 default matrix) keep TRADE **on** and 35=D **off**.

Allowed with `REAL_COPY=false` and `CTRADER_FIX_TRADE_SESSION_ENABLED=true`:

- Logon / Logout / Heartbeat / TestRequest / Resend / Reject / SequenceReset
- SecurityListRequest (if used for discovery)
- OrderStatusRequest, OrderMassStatusRequest
- RequestForPositions / inbound PositionReport
- Inbound ExecutionReport (unsolicited or status)
- Startup + periodic reconciliation (§42–§43)
- **Cancel / flatten only under kill-switch policy** (see §5), still persist-before-send

Forbidden with `REAL_COPY=false`:

- `NewOrderSingle` (35=D) for copy OPEN/INCREASE
- `NewOrderSingle` for “catch-up” after reconnect (§63)
- `OrderCancelReplaceRequest` that **increases** exposure
- Any second 35=D because TCP dropped (§33–§34)

### 3.4 Config floor vs runtime (A25 §6.4 + A26 PATCH)

```text
effective_real_copy =
      config.REAL_COPY_EXECUTION_ENABLED     -- env / secret / appsettings; default false
  AND settings_store.real_copy               -- SuperAdmin PATCH, audited; default false
  AND NOT STOP_NEW_EXECUTION
```

Binding rules for the worker:

1. **Config is the floor.** If env/appsettings is `false`, dashboard PATCH must **not** flip the in-process gate to true (A25). API should return 409/412, not silently “enable.”
2. Runtime / kill-switch may turn execution **off** without a restart (`STOP_NEW_EXECUTION`).
3. Runtime must **not** turn `REAL_COPY` on if config is `false`.
4. False → true (when config allows) still requires `READY_FOR_EXECUTION`. **Do not drain the backlog** that accumulated while the flag was off (§63). Only intents that still pass `expires_at` / `max_signal_age` at the new decision time may be re-evaluated. Prefer: mark flag-off intents `FLAG_DISABLED` / expired rather than leaving them sendable.
5. Promoting to live is a **config + audit** event (`confirmPhrase: ENABLE_REAL_EXECUTION` in A26), not an unauthenticated button.

### 3.5 Binding contract (ASP.NET will not do this for free)

Architecture names are **flat** env vars (`CTRADER_FIX_QUOTE_ENABLED`). `Microsoft.Extensions.Configuration` nested binding expects `CTraderFix:QuoteEnabled` or `CTRADERFIX__QUOTEENABLED`. **Default binder will miss `CTRADER_FIX_*`.**

Fix-worker must use an **explicit map** (env name → property). Suggested section + aliases:

| Architecture env | Options property (exists today) | Nested JSON (optional) |
|---|---|---|
| `CTRADER_FIX_ENABLED` | **MISSING** on `CTraderFixOptions` | `CTraderFix:Enabled` |
| `CTRADER_FIX_QUOTE_ENABLED` | `QuoteEnabled` | `CTraderFix:QuoteEnabled` |
| `CTRADER_FIX_TRADE_SESSION_ENABLED` | `TradeSessionEnabled` | `CTraderFix:TradeSessionEnabled` |
| `REAL_COPY_EXECUTION_ENABLED` | `RealCopyExecutionEnabled` | `CTraderFix:RealCopyExecutionEnabled` |
| `CTRADER_FIX_USE_SSL` | `UseSsl` | `CTraderFix:UseSsl` |
| `CTRADER_FIX_HOST` | `Host` | `CTraderFix:Host` |
| `CTRADER_FIX_ACCOUNT_ID` | `AccountId` | `CTraderFix:AccountId` |
| `CTRADER_FIX_PASSWORD` | `Password` (UserSecrets / env only) | never commit |

Parse rules (fail closed):

| Flag | Missing | `"true"` / `"false"` (any case) | `"1"` / `"yes"` / `"on"` / garbage |
|---|---|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | **`false`** | bool.Parse | **`false`** + warn/alert. Do **not** treat `1` as true unless a documented allow-list is added later. |
| `CTRADER_FIX_*_ENABLED` session flags | architecture default (`true`) | bool.Parse | fail closed to **session off** if unparseable (prefer not to open a live socket on garbage) |
| `CTRADER_FIX_USE_SSL` | `true` | bool.Parse | unparseable → `true` (fail closed to TLS) |
| `CTRADER_FIX_ALLOW_PLAINTEXT` | `false` | bool.Parse | unparseable → `false` |

Production should still **set** `REAL_COPY_EXECUTION_ENABLED=false` explicitly so a future default-change cannot enable send.

Do not log `CTRADER_FIX_PASSWORD` or `AccountId` password material. Log flag values as structured fields: `ctrader_fix_enabled`, `quote_enabled`, `trade_session_enabled`, `real_copy_execution_enabled`.

---

## 4. How `mt5-worker` must enforce (design)

`apps/mt5-worker` is Achiever + StarwaveFX ingestion (§7–§12, Phase 1). It is **not** an execution venue.

Hard rules:

1. **No FIX client.** No QuickFIX initiator, no `IFixTradeGateway`, no `NewOrderSingle` builder in this process.
2. MT5 callbacks stay lightweight (§72.6). Persist raw deal + outbox, return. Never evaluate flags as “maybe send now.”
3. Outbox types today (`OutboxEventType`) are `TradeCompleted`, `ScoreUpdate`, `ShadowCopyIntent`, `RiskCheckRequest`, `NotificationEvent`. There is **no** `SendFixOrder` event. Do not add one that the MT5 worker consumes.
4. Correct path (§32): source event → persist → CopyIntent (another process / later hosted service) → RiskEngine → `ApprovedExecutionIntent` → **fix-worker**.
5. If `REAL_COPY_EXECUTION_ENABLED=false` (the default), MT5 worker behavior is **unchanged**: keep ingesting. Flags do not pause MT5.
6. If `CTRADER_FIX_ENABLED=false`, MT5 worker still ingests. Source ledger is independent of destination venue.
7. Stale-source rule (§62) is **not** a `CTRADER_FIX_*` flag: if MT5 is down, do not invent trades and do not open new copied positions from stale source data. That check belongs in risk / copy-intent expiry, not in the MT5 callback.

Enforcement test (A27): `Outbox.OutboxDoesNotCallFixFromCallbackTests` — ingest thread never calls 35=D.

If someone later “just calls the gateway from the collector because it is faster,” that is a **defect**, not a flag miss.

---

## 5. Kill switch vs feature flags (§40)

Do not implement `STOP_NEW_EXECUTION` as an alias of `REAL_COPY_EXECUTION_ENABLED`.

| Control | Who flips it | New copy 35=D | Existing dest positions | Reduce/close 35=D |
|---|---|---|---|---|
| `REAL_COPY_EXECUTION_ENABLED=false` | Config / audited promote | blocked | untouched | **not** used for routine copy close; flatten is a different control |
| `STOP_NEW_EXECUTION` | Ops / risk `GLOBAL_STOP` | blocked | untouched | allowed under reduce/close policy if TRADE ready |
| `EMERGENCY_FLATTEN` | Stronger auth + confirm | blocked (opens) | close attempted | **may** send reducing orders even if `REAL_COPY=false`, **only if** TRADE logged on, lease owned, flatten authorized, persist-before-send, unknown-state rules |

`EMERGENCY_FLATTEN` is **not** a backdoor to enable copy. It is a privileged close path. It still must not blind-retry.

Risk evaluation order (A23 §5) — live path, fail closed on first blocker:

1. Database available
2. **`REAL_COPY_EXECUTION_ENABLED`** (shadow path may evaluate the same rules **without** emitting execution intents)
3. Kill switch
4. Reconciliation / unknown state
5. Venue health (QUOTE / TRADE flags + session state)
6. Source health
7. Trader eligibility
8. Expiry / signal age
9. Quote age / spread / price-move
10. Sizing
11. Book / account hard limits
12. Martingale / abnormal size
13. Persist `risk_decision`; only then persist `execution_intent` `not_sent`

Fix-worker send gate **repeats** steps 2–5 at send time. Risk approval is not a capability token that survives flag-off or recon-fail.

---

## 6. Phase mapping (when which flags are on)

| Phase | `CTRADER_FIX_ENABLED` | QUOTE | TRADE session | `REAL_COPY` | Worker duty |
|---|---|---|---|---|---|
| 0–3 | unused / may be true | unused | unused | **false** | No FIX send. MT5 worker ingest only. |
| 4 QUOTE | true | **true** | false or unused | **false** | Fix-worker: TLS QUOTE, SecurityList, XAU map, quote cache. **No TRADE send.** |
| 5 Shadow | true | **true** | unrelated to shadow fills | **false** | Shadow uses QUOTE only (A24). TRADE flag does not authorize 35=D. |
| 6 ML | same | same | same | **false** | ML never sends (§72.15). |
| 7 TRADE read/recon | true | true | **true** | **false** (still) | Fix-worker: TRADE Logon, 35=AF / 35=AN, recon. **NewOrderSingle compiled/flagged off.** |
| 8 Live | true | true | true | **true only after §68 + §70** | Conjunction in §3.2. Explicit production flag. |

§70.12: “Real execution is feature flagged.” §67 Phase 8: “Enable only with explicit production flag.” §75 last line: real submission remains OFF until shadow, recon, sizing, and risk gates pass.

First useful version (§69) **does not** require `REAL_COPY=true`.

---

## 7. Measured code vs design (2026-08-18)

Honest state. Do not treat a compiling worker as a gate.

### 7.1 `CTraderFixOptions` — POCO defaults only

Path: `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`

| Property | Code default | Matches §41? |
|---|---|---|
| `QuoteEnabled` | `true` | yes (`CTRADER_FIX_QUOTE_ENABLED`) |
| `TradeSessionEnabled` | `true` | yes (`CTRADER_FIX_TRADE_SESSION_ENABLED`) |
| `RealCopyExecutionEnabled` | **`false`** | **yes** — this is the only implemented default |
| `UseSsl` | `true` | yes |
| master `Enabled` / `CTRADER_FIX_ENABLED` | **property absent** | **gap** |
| `DiagnosticLogonOnly` / `AllowPlaintext` | absent | gap vs A25 §6.6 |
| env-name binder | **absent** | architecture flat names will not bind |
| send-gate method | **absent** | POCO is unused |

`TargetCompId` defaults to `"CSERVER"` in this POCO. Architecture env sample uses issued-form `cServer`. That is a **header-mapping** issue (A25 / A32), not a feature flag, but the worker must not silently mutate case when flags turn sessions on.

`Fix.CTrader.csproj` references `QuickFix.Net` 1.11.2 (note: A05 preferred QuickFIXn 1.14.x). There is still **no** session class, **no** `IApplication`, **no** initiator.

### 7.2 `apps/fix-worker` — does not read or enforce flags

```1:7:D:\Prop\apps\fix-worker\Program.cs
using TraderIntelligence.FixWorker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
```

```1:23:D:\Prop\apps\fix-worker\Worker.cs
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

`appsettings.json`, `appsettings.Development.json`, and `Properties/launchSettings.json` contain **logging only**. Zero `CTRADER_*` / `REAL_COPY_*` keys. No `IOptions<CTraderFixOptions>`. Project references Domain / Application / Infrastructure / Fix.CTrader are unused.

### 7.3 `apps/mt5-worker` — same template, no FIX (correct so far)

Identical 1-second heartbeat. No FIX reference (csproj points at `src/Mt5`, not `Fix.CTrader`). Accidental compliance with “do not send from MT5.” Not an enforcement implementation.

### 7.4 Application / tests / API

| Location | Flag types / gates | Status |
|---|---|---|
| `src/Application` | `RealExecutionFeatureFlags`, `ReadyForExecutionGate`, `IFixTradeGateway` | **MISSING** (`Class1` only) |
| `src/Domain` | `FixSessionStatus.ReadyForExecution`, `KillSwitchMode`, `ExecutionOrderStatus` | enums exist; no flag value object |
| `apps/api` | `GET /api/v1/settings` flag snapshot (A26) | **MISSING** (weatherforecast template) |
| `tests/Unit` | `Risk.RealExecutionFeatureFlagTests` (A27) | **MISSING** (`UnitTest1`) |
| `tests/Integration` | `Flags.RealExecutionDisabledIntegrationTests` | **MISSING** |

### 7.5 Classification

| Control | Classification |
|---|---|
| Architecture §41 defaults | **EXISTS_AND_GOOD** (on disk in the markdown) |
| `RealCopyExecutionEnabled = false` on POCO | **EXISTS** (default only; unused) |
| Worker bind + enforce | **MISSING** |
| Live-send risk **today** | **SAFE_BY_ABSENCE** (no 35=D builder) |
| Required flag gate for Phase 8 | **MISSING** — do not treat absence as the gate once a send path appears |

A08’s earlier snapshot said Fix.CTrader had zero `.cs` files. That is **stale**. The POCO now exists. The **enforcement** finding is unchanged.

---

## 8. Tests workers must pass before `REAL_COPY` may be true

From A27 / §70.12. None of these exist yet.

| Test | Must prove |
|---|---|
| `Risk.RealExecutionFeatureFlagTests` | `REAL_COPY=false` cannot emit 35=D even with approved intent + TRADE logged on |
| `Flags.RealExecutionDisabledIntegrationTests` | TRADE may Logon + mass status + positions; send path closed |
| `Reconcile.StartupReconciliationGateTests` | After Logon, new exec blocked until `READY_FOR_EXECUTION` |
| `Risk.KillSwitchStopNewExecutionTests` | `STOP_NEW_EXECUTION` blocks new copy; positions untouched |
| `Harness.RiskRejectionBeforeFixSendTests` | Risk reject happens before socket write |
| `Outbox.OutboxDoesNotCallFixFromCallbackTests` | MT5 worker never calls FIX |
| Flag parse tests (not yet named) | missing / garbage `REAL_COPY` → false; session garbage → do not open socket |
| Binder tests | flat `CTRADER_FIX_QUOTE_ENABLED` actually reaches `QuoteEnabled` |

Until those are green **and** §68 / §70 checklists are evidenced, production env stays:

```env
REAL_COPY_EXECUTION_ENABLED=false
```

---

## 9. Implementation checklist (later coding task — not this file)

When a coding agent implements enforcement, do this and nothing sneakier:

1. Add `CTraderFixFeatureFlags` (Application or options) with §41 names **and** `Enabled` for `CTRADER_FIX_ENABLED`. Keep `RealCopyExecutionEnabled` default **false**.
2. Explicit env binder for flat `CTRADER_FIX_*` / `REAL_COPY_EXECUTION_ENABLED` (do not rely on `__` nested convention).
3. Bind in `apps/fix-worker` only. Register sessions conditionally. Do not start TRADE without a lease.
4. One `SubmitNew` function that re-reads effective flags + recon + lease + kill switch + persist-before-send.
5. Refuse 35=D on the QUOTE session (session-role defect, not a flag).
6. `apps/mt5-worker`: no FIX package, no send API.
7. API: publish non-secret flags; PATCH cannot raise `REAL_COPY` above config floor; never return password.
8. Unit + integration tests in §8 green on the **simulator** (§61). Do not use Pepperstone `1369850` as the first test.
9. Only then consider `REAL_COPY_EXECUTION_ENABLED=true` in a non-prod proof environment (A25 §12.10).

---

## 10. Do-not list (worker edition)

1. Do not send 35=D from `apps/mt5-worker` or an MT5 callback because “the flag is on.”
2. Do not treat `CTRADER_FIX_TRADE_SESSION_ENABLED=true` as live trading.
3. Do not treat TRADE Logon as `READY_FOR_EXECUTION`.
4. Do not enable `REAL_COPY` to “see if FIX works.” Use QUOTE + TRADE read + simulator.
5. Do not drain expired intents when the flag flips false → true.
6. Do not retry 35=D on disconnect.
7. Do not start two TRADE owners.
8. Do not commit `CTRADER_FIX_PASSWORD`.
9. Do not expose flags’ secrets to React (A06 / A26). Boolean flags and host/ports only.
10. Do not implement kill switch as `REAL_COPY=false`.
11. Do not default `RealCopyExecutionEnabled` to `true` in Development `appsettings` “to make testing easier.” Tests use the simulator + an explicit test fixture flag.
12. Do not add `NewOrderSingle` until Phase 4 + Phase 7 + risk + these four flags are real controls (A08).

---

## 11. One-page operator view

Default production posture (architecture §41 + §56):

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
CTRADER_FIX_USE_SSL=true
REAL_COPY_EXECUTION_ENABLED=false
```

| You want to… | Set |
|---|---|
| Stop all cServer sockets | `CTRADER_FIX_ENABLED=false` |
| Keep TRADE recon, drop quotes | `CTRADER_FIX_QUOTE_ENABLED=false` |
| Quotes + shadow only (Phase 4–5) | TRADE session `false` **or** TRADE on + `REAL_COPY=false` |
| Phase 7 recon without live send | TRADE `true`, `REAL_COPY=false` ← **default** |
| Halt new copy without restart | `STOP_NEW_EXECUTION` (not a config flag) |
| Place a real copy order | `REAL_COPY=true` **and** every row in §3.2 |

**Today’s workers enforce none of the above.** They do not trade because they cannot, not because they refuse.

---

*End of A49. Product source was not modified.*
