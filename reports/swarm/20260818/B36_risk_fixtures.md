# B36 — Risk fixtures: stale quote, stale signal, kill switch, reduce allowed, real send blocked

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\B36_risk_fixtures.md` |
| Agent | B36 (risk fixture design) |
| Date | 2026-08-18 |
| Status | **BINDING fixture design** — specification only |
| Product source edited | **No.** No `.cs`, `.json` fixture files, `.csproj`, or test classes were created. |
| SUT (read-only) | `D:\Prop\src\Domain\Risk\RiskEngine.cs` |
| Existing tests (read-only) | `D:\Prop\tests\Unit\RiskEngineTests.cs` (5 facts) |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §31, §36–§41, §61–§64, §68, §70.11–13, §72.17–19 |
| Binding siblings | A23 risk engine, A27 test inventory, A48 kill switch, A49 feature flags, A67 replay JSON, A71 exposure policy, A72 quote guards, A100 G11–G13/G16, B13 risk review |
| Scope | Five fixture families that lock the go-live reject/allow/send surface. XAUUSD copy path only. In-process / recorded FIX. **Never** the live Pepperstone TRADE account. |

**Law this file exists to make unskippable**

```text
[ ] stale quote rejection works          §68 / A100 G12
[ ] stale signal rejection works         §68 / A100 G13
[ ] kill switch tested                   §68 / A100 G16 / §70.13
[ ] risk-engine rejection happens before FIX send   §70.11
[ ] real execution feature-flagged       §41 / §70.12
```

plus the §64 / §72.18 duty: **reduce/close of a mapped dest is allowed when open/increase is not.**

---

## 0. Verdict

The five families below are the **minimum** fixture pack that can turn those checkboxes from “engine method exists” into measured PASS. Today they are **not** on disk.

| Family | Id prefix | Must prove | Current tree |
|---|---|---|---|
| Stale quote | `RF-SQ-*` | `quote_age > max_quote_age` rejects OPEN/INCREASE with `QUOTE_STALE`; CLOSE family is not blocked by the OPEN threshold | Partial: one unit fact `Stale_quote_rejects_open`. No dual clock. No send probe. |
| Stale signal | `RF-SS-*` | `signal_age > max_source_signal_age` / `expires_at` rejects OPEN/INCREASE with `SIGNAL_STALE`; 20-intent 3-minute catch-up does not fire NOS | Partial: one unit fact `Stale_signal_rejected`. `CopyIntentExpiry` exists, unused on `Evaluate`. |
| Kill switch | `RF-KS-*` | `STOP_NEW_EXECUTION` ≠ `EMERGENCY_FLATTEN`. Stop-new blocks new copy, does not flatten. Dest book untouched. | Partial: one unit fact `Stop_new_execution_blocks_opens_not_closes`. Exclusive `KillSwitchMode`. Close `AllowFixSend=false`. |
| Reduce allowed | `RF-RA-*` | Same rotten snapshot: OPEN rejects, REDUCE/CLOSE of a **linked** dest approves | Missing as a matrix. Engine family split started; loss/DD still freeze exits (B13 / A71 §14). |
| Real send blocked | `RF-RB-*` | `REAL_COPY_EXECUTION_ENABLED=false` ⇒ `AllowFixSend=false` **and** `35=D` builder never runs, even on `APPROVE` | Partial: `Real_flag_false_never_allows_fix_send`. No send probe. Worker has no NOS. Safe by absence. |

**Two expectation columns are mandatory on every row.** Shipping only the stub column would greenwash A71 defects. Shipping only the law column would fail `dotnet test` against today’s `RiskEngine`.

| Column | Meaning | When it may PASS |
|---|---|---|
| `expect_stub` | What `RiskEngine.Evaluate` returns **today** (2026-08-18 measured) | Now, against `TraderIntelligence.Domain` |
| `expect_law` | What A23 / A48 / A49 / A71 / §64 require for go-live | Only after a later, reviewed engine change |

`lane=stub` tests are allowed to lock current behavior so refactors cannot silently flip it.  
`lane=law` tests are the §68 gates. They **must stay FAIL** until the stub deltas in §14 are fixed. Do not `[Fact(Skip=…)]` them into a fake PASS.

This file does **not** implement fixtures, tests, or engine fixes.

---

## 1. Binding law (do not reinterpret)

### 1.1 Stale quote — §31 / §37 / A23 §6.1 / A72

```text
quote_age       = decision_time - quote_received_at
venue_quote_age = decision_time - venue_timestamp   # only if venue_timestamp present and sane

if quote missing OR unusable:
    reject OPEN/INCREASE   QUOTE_MISSING / QUOTE_UNAVAILABLE

if quote_age > configured_max_quote_age  OR  venue_quote_age > configured_max:
    reject OPEN/INCREASE   QUOTE_STALE
```

QUOTE “logged on” is **not** freshness (A25 / A72). Spread and signed price-move are **independent** guards; they have their own fixtures later, not this pack.

CLOSE family does **not** share `max_quote_age_open`. Shadow CLOSE may fill on a last usable quote (`STALE_QUOTE` quality) or hold unpriced (A24 / A71 §7.3). Live CLOSE of a known dest may still send if TRADE is up.

### 1.2 Stale signal — §36 / §63 / A23 §6.2

```text
signal_age = decision_time - source_event_time

if now >= expires_at:                    REJECT  INTENT_EXPIRED     # OPEN family
if signal_age > max_signal_age:          REJECT  SIGNAL_STALE
if signal_age > max_source_signal_age:   REJECT  SIGNAL_STALE
```

The **stricter** of per-intent `max_signal_age` and global `max_source_signal_age` wins.

Normative catch-up: TRADE down 3 minutes, 20 source opens → reconnect must **not** fire 20 `NewOrderSingle`s. Expired OPEN intents die. CLOSE of **existing** dest still processes (A71 E11).

### 1.3 Kill switch — §40 / A23 §8 / A48

Two **independent** controls. A single exclusive enum is an A48 violation if treated as SoT.

| Control | OPEN / INCREASE | Dest book | REDUCE / CLOSE of mapped dest |
|---|---|---|---|
| `STOP_NEW_EXECUTION` | Block (`STOP_NEW_EXECUTION` / `GLOBAL_STOP`) | **Untouched** | Allowed by default (`allow_risk_reduction_while_stop_new=true`) |
| `EMERGENCY_FLATTEN` active | Block (`EMERGENCY_FLATTEN_ACTIVE`) | Flatten run owns closes | Coalesce; no second NOS on owned ids |

`REAL_COPY_EXECUTION_ENABLED` is **not** a kill switch (A49 §5).

### 1.4 Reduce allowed — §64 / §72.18 / A71

OPEN family (`OPEN_EXPOSURE`, `INCREASE_EXPOSURE`) is fail-closed on quote, signal, spread, move, stop-new, recon-for-new-risk, book caps, martingale.

CLOSE family (`REDUCE_EXPOSURE`, `CLOSE_EXPOSURE`) is fail-closed on **identity** (linked dest, qty ≤ remaining, no unknown-state on that dest). It is **not** fail-closed on OPEN market-quality or stop-new.

This family is **not** `RiskDecisionOutcome.ReduceSize` (cap clipping of an OPEN). That is a different fixture pack.

### 1.5 Real send blocked — §41 / A49 §3.2 / §70.12

`REAL_COPY_EXECUTION_ENABLED` defaults **false**. Necessary but not sufficient for `35=D`.

Conjunction immediately before socket write (A49):

```text
CTRADER_FIX_ENABLED
CTRADER_FIX_TRADE_SESSION_ENABLED
REAL_COPY_EXECUTION_ENABLED = true          # config floor AND runtime
TRADE = READY_FOR_EXECUTION
lease owned
risk AllowFixSend = true
STOP_NEW_EXECUTION = false                  # for OPEN family
no EXECUTION_STATE_UNKNOWN
intent persisted, cl_ord_id persisted, status = not_sent
intent not expired
DIAGNOSTIC_LOGON_ONLY = false
```

If the flag is false: **do not send**. Persist a decision. Do not drain a backlog when the flag later flips on (§63). TRADE may still logon and reconcile.

v1 source-driven CLOSE also needs the flag (A49 §5 / A71 §10). Flatten reducing NOS is a **later** A48 exception, not this pack’s happy path.

---

## 2. Measured product surface (do not invent types)

Read 2026-08-18. These are the only types fixtures may bind to **now**. Law-lane fixtures may name fields that A23 requires but the request DTO lacks; those rows stay FAIL until the DTO grows.

### 2.1 Engine types

| Type | Path | Fixture use |
|---|---|---|
| `RiskLimits` | `src/Domain/Risk/RiskEngine.cs` | Pin every threshold. Do not inherit silent defaults in gold files. |
| `DestinationQuote` (record) | same | Bid/ask/`ReceivedAt`/`VenueTimestamp` |
| `RiskEvaluationRequest` | same | Unit-lane input |
| `RiskDecision` | same | Unit-lane output |
| `RiskEngine.Evaluate` | same | Pure function under test |
| `CopyIntentAction` | `src/Domain/Enums/CopyIntentAction.cs` | `OpenExposure` … `CloseExposure` |
| `KillSwitchMode` | `src/Domain/Enums/KillSwitchMode.cs` | `None`, `StopNewExecution`, `EmergencyFlatten` (**exclusive**) |
| `RiskDecisionOutcome` | `src/Domain/Enums/RiskDecisionOutcome.cs` | `Approve`, `ReduceSize`, `Reject`, `PauseTrader`, `PauseVenue`, `GlobalStop` |
| `CopyIntentExpiry.IsExpired` | `src/Domain/Execution/CopyIntentExpiry.cs` | Signal-age helper; **not** called by `Evaluate` |
| `CopyIntent` | `src/Domain/Entities/CopyIntent.cs` | Integration seed (`ExpiresAt` exists; no `max_signal_age`, no dest link) |
| `DestinationQuoteSnapshot` | `src/Domain/Entities/DestinationQuote.cs` | Integration quote row |
| `KillSwitch` | `src/Domain/Entities/KillSwitch.cs` | Integration latch (`Mode`, `SetBy`, `Reason`, `UpdatedAt`) |
| `RiskDecisionRecord` | `src/Domain/Entities/RiskDecisionRecord.cs` | Persist assertion (`AllowFixSend`, `Reason`) |
| `ExecutionIntent` | `src/Domain/Entities/ExecutionIntent.cs` | Persist-before-send; `Status` starts `NotSent` |
| `CTraderFixOptions.RealCopyExecutionEnabled` | `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | Default **false**; `MaxQuoteAgeMs=5000` **≠** `RiskLimits.MaxQuoteAge=3s` |
| `Worker` | `apps/fix-worker/Worker.cs` | Reads `CTrader:RealCopyExecutionEnabled` default false; **no** `35=D` |
| `FixSimulationHarness` | `src/Fix.CTrader/Testing/FixSimulationHarness.cs` | Recorded ER/MD only; no send counter |

`grep Evaluate(` under product `*.cs` hits **only** the definition and `tests/Unit/RiskEngineTests.cs`. Application / API / workers do not call the engine. Fixtures that require a send path must inject a **fake** gateway (design in §5). They must not open `live-us-eqx-01.p.c-trader.com`.

### 2.2 Existing unit facts (keep; do not rewrite)

`D:\Prop\tests\Unit\RiskEngineTests.cs` already locks five behaviors against `Base()` (`DecisionTime=2026-08-18T12:00:00Z`, `RealExecutionEnabled=false`):

| Fact | Input tweak | Asserted today |
|---|---|---|
| `Stale_quote_rejects_open` | `ReceivedAt = T-30s` | `Reject` / `QUOTE_STALE` / `AllowFixSend=false` |
| `Real_flag_false_never_allows_fix_send` | default Base | `Approve` / `AllowFixSend=false` |
| `Stop_new_execution_blocks_opens_not_closes` | `StopNewExecution` | open `GlobalStop`; close `Approve` + `AllowFixSend=false` |
| `Unreconciled_venue_blocks_new_exposure` | `Reconciled=false` | reason `VENUE_NOT_RECONCILED` |
| `Stale_signal_rejected` | `SourceEventTime = T-5min` | reason `SIGNAL_STALE` |

Those facts are the seed of `RF-SQ-OPEN-30S`, `RF-RB-OPEN-FLAG-OFF`, `RF-KS-STOP-OPEN` / `RF-KS-STOP-CLS`, `RF-SS-OPEN-5M`. This design **extends** them into a matrix. It does not authorize deleting them.

### 2.3 Threshold collision (fixtures must pin one clock)

| Source | Value | Used by |
|---|---|---|
| `RiskLimits.MaxQuoteAge` | **3 s** | `Evaluate` quote-age check |
| `CTraderFixOptions.MaxQuoteAgeMs` | **5000** | unbound; not read by `Evaluate` |
| API `/api/settings` `maxQuoteAgeSeconds` | **3** (B13) | display only |
| `RiskLimits.MaxSourceSignalAge` | **15 s** | `Evaluate` signal-age check |
| `CopyIntentExpiry` tests | 15 s | `ExecutionAndSizingTests.Copy_intent_expires` |

Gold files pin **milliseconds** and name the authority (`risk_limits.max_quote_age_ms`). They never say “use the default.” A later binder that unifies 3 s vs 5 s must update the catalog, not hide the change.

Comparison in the stub is **strict greater-than**:

```text
age > MaxQuoteAge          → QUOTE_STALE
signalAge > MaxSourceSignalAge → SIGNAL_STALE
```

Equality is **fresh**. Boundary rows exist so nobody “fixes” this by accident.

---

## 3. On-disk layout (proposed; do not create in this pass)

Align with A27 / A67. Risk fixtures are **not** a fifth test project. A27: `tests/Risk` is not a project.

```text
tests/
  Fixtures/
    Risk/
      schema/ti.risk.fixture.v1.schema.json
      catalog.tsv
      golden/
        RF-SQ-OPEN-STALE.json
        RF-SQ-OPEN-FRESH.json
        RF-SQ-OPEN-EQ-BOUNDARY.json
        RF-SQ-OPEN-MISSING.json
        RF-SQ-INC-STALE.json
        RF-SQ-RED-STALE.json
        RF-SQ-CLS-STALE.json
        RF-SQ-VENUE-TS-STALE.json
        RF-SS-OPEN-STALE.json
        RF-SS-OPEN-FRESH.json
        RF-SS-OPEN-EQ-BOUNDARY.json
        RF-SS-OPEN-EXPIRED.json
        RF-SS-INC-STALE.json
        RF-SS-RED-STALE.json
        RF-SS-CLS-STALE.json
        RF-SS-CATCHUP-20.json
        RF-KS-STOP-OPEN.json
        RF-KS-STOP-INC.json
        RF-KS-STOP-RED.json
        RF-KS-STOP-CLS.json
        RF-KS-FLAT-OPEN.json
        RF-KS-FLAT-CLS.json
        RF-KS-NONE-OPEN-SEND.json
        RF-KS-BOTH-BITS.json
        RF-KS-NOTSENT-DRAIN.json
        RF-RA-MATRIX-ROTTEN.json
        RF-RA-LOSS-EXIT.json
        RF-RA-NO-DEST-LINK.json
        RF-RA-REVERSAL.json
        RF-RB-OPEN-FLAG-OFF.json
        RF-RB-RED-FLAG-OFF.json
        RF-RB-CLS-FLAG-OFF.json
        RF-RB-OPEN-FLAG-ON.json
        RF-RB-FLAG-ON-BUT-KILL.json
        RF-RB-PARSE-MISSING.json
        RF-RB-PARSE-GARBAGE.json
        RF-RB-WORKER-REFUSE.json
  Unit/
    Risk/
      RiskFixtureLoader.cs          # proposed
      RiskEvaluationRequestFactory.cs
      QuoteFreshnessGuardTests.cs   # A27
      StaleCopyIntentExpiryTests.cs
      KillSwitchStopNewExecutionTests.cs
      OpenVsCloseExposurePolicyTests.cs
      RealExecutionFeatureFlagTests.cs
  Integration/
    Flags/
      RealExecutionDisabledIntegrationTests.cs
  Fix/
    Harness/
      RiskRejectionBeforeFixSendTests.cs
      GlobalStopNewOrdersTests.cs
      RecordingFixTradeGateway.cs   # §5
  Replay/
    Fixtures/
      no_blind_catchup.json         # A67 already named this
```

One JSON document per **scenario** (or one document with `cases[]` for the RA matrix). UTF-8, no BOM, LF. Deterministic key order for catalog SHA-256 (A67 §15 style: sorted keys, no insignificant whitespace).

**Do not** put fixtures beside live product files. **Do not** create `*.MUTATED*`. **Do not** point any fixture at the live TRADE port.

---

## 4. Frozen clocks, identities, and golden baseline

Every fixture in this pack shares the same lab universe unless it explicitly overrides one field.

### 4.1 Clocks

| Name | Value | Role |
|---|---|---|
| `T0` / `decision_time` | `2026-08-18T12:00:00.000Z` | Same instant as `RiskEngineTests.Base` |
| `T_QUOTE_FRESH` | `T0 - 200ms` | Default `ReceivedAt` / `VenueTimestamp` |
| `T_QUOTE_STALE` | `T0 - 30s` | Classic OPEN reject (`30s > 3s`) |
| `T_QUOTE_EQ` | `T0 - 3s` | Boundary: stub treats as **fresh** |
| `T_SIGNAL_FRESH` | `T0 - 1s` | Default `SourceEventTime` |
| `T_SIGNAL_STALE` | `T0 - 5min` | Classic OPEN reject |
| `T_SIGNAL_EQ` | `T0 - 15s` | Boundary: stub treats as **fresh** |
| `T_FIX_DOWN` | `T0 - 3min` | Catch-up tape start |

A `DeterministicClock` (A27 / A67) must supply `decision_time`. Tests must not call `DateTimeOffset.UtcNow` inside assertions of age.

### 4.2 Identities

| Field | Golden value | Notes |
|---|---|---|
| `copy_intent_id` | fixture id, e.g. `RF-SQ-OPEN-STALE` | Stable; never `Guid.NewGuid()` in unit lane |
| `broker_id` | `ACHIEVER` | Wire string; Domain `BrokerCodes.Achiever` at the EF boundary |
| `source_login` | `10001` | Demo seed login; not a live account we trade |
| `canonical_symbol` | `XAUUSD` | Never raw FIX 55 as the identity |
| `venue_instrument_id` | `1` | Lab stub. Live discovery is a different fixture pack. |
| `destination_account` | `1369850` | Identity only. Fixtures never send to it. |
| `linked_destination_position_id` | `DEST-XAU-10001-1` | Required on INCREASE/REDUCE/CLOSE **law** rows |
| `requested_quantity` | `0.10` | Already dest-normalized. Sizing conversion is **not** this pack. |
| `expected_price` | `2400.00` | Mid of the golden book |
| `bid` / `ask` | `2399.90` / `2400.10` | Spread `0.20` ≪ `MaxAllowedSpread=2.0` |

### 4.3 Pinned `RiskLimits` (lab, not production law)

Every fixture embeds this object so a default-change fails the catalog:

```json
{
  "max_loss_per_trader": 500,
  "max_daily_execution_loss": 2000,
  "max_portfolio_drawdown": 3000,
  "max_xau_gross_exposure": 20,
  "max_xau_net_exposure": 10,
  "max_position_quantity": 5,
  "max_open_positions": 20,
  "max_allowed_spread": 2.0,
  "max_quote_age_ms": 3000,
  "max_source_signal_age_ms": 15000,
  "max_price_move": 3.0,
  "max_slippage": 1.5,
  "max_margin_usage": 0.70,
  "block_martingale": true,
  "block_abnormal_sizing": true
}
```

Law-lane CLOSE clocks (A71 §9) are **named** but not hardcoded as production milliseconds:

```text
max_quote_age_open_ms              = 3000   # same as stub until config exists
max_quote_age_close_ms             = CONFIG # must be ≫ open; fixture uses 60000 as a lab stand-in
max_quote_age_close_stale_fallback = CONFIG # fixture uses 300000
max_signal_age_open_ms             = 15000
max_signal_age_close_ms            = CONFIG # fixture uses 14400000 (4 h) as a lab stand-in
```

Lab stand-ins are **relationships**, not Pepperstone measurements. A72 forbids treating compile constants as production law.

### 4.4 Golden “would approve OPEN” request

This is `RiskEngineTests.Base()` plus explicit flags. Every reject fixture is a **single-field** (or documented multi-field) mutation of this snapshot.

```text
CopyIntentId          = <fixture id>
BrokerId              = ACHIEVER
SourceLogin           = 10001
Action                = OpenExposure
RequestedQuantity     = 0.10
ExpectedPrice         = 2400
SourceEventTime       = T0 - 1s
DecisionTime          = T0
Quote.CanonicalSymbol = XAUUSD
Quote.VenueInstrumentId = 1
Quote.Bid             = 2399.9
Quote.Ask             = 2400.1
Quote.ReceivedAt      = T0 - 200ms
Quote.VenueTimestamp  = T0 - 200ms
VenueHealthy          = true
RealExecutionEnabled  = <per fixture>
Reconciled            = true
KillSwitch            = None
TraderRealizedLoss    = 0
DailyExecutionPnl     = 0
PortfolioDrawdown     = 0
CurrentGrossXau       = 0
CurrentNetXau         = 0
OpenPositions         = 0
MarginUsage           = 0.10
MartingaleFlag        = false
AbnormalSizing        = false
```

Default for **stub** tests: `RealExecutionEnabled=false` (matches existing `Base()`).  
Default for **send-probe** tests: the fixture states the flag explicitly; the probe still refuses unless **both** the flag and `AllowFixSend` are true.

---

## 5. Recording send probe (required for “blocked” to mean anything)

`AllowFixSend=false` on a record nobody sends is not §70.11. Every RF-SQ / RF-SS / RF-KS / RF-RB row that claims “no real send” must run through a probe.

Proposed type (design only; do not add to product in this pass):

```text
namespace TraderIntelligence.Tests.Fix.Harness;

interface IFixTradeGateway
{
    int SubmitNewCount { get; }
    int SubmitCancelCount { get; }
    IReadOnlyList<RecordedOutbound> Outbound { get; }

    SubmitResult TrySendNewOrderSingle(ApprovedExecutionIntent intent, RiskDecision decision);
}

record RecordedOutbound(string MsgType, string? ClOrdId, string ReasonNotSent);
```

**Probe rules**

1. If `decision.AllowFixSend == false` → increment `RefusedCount`, append `RecordedOutbound("D", null, decision.Reason or REAL_COPY_DISABLED)`, return without building FIX.
2. If `REAL_COPY_EXECUTION_ENABLED == false` → same refuse, reason `REAL_COPY_DISABLED`, even if a buggy decision says `AllowFixSend=true`. This is the **second latch** A49 requires.
3. If `intent.Status != NotSent` → refuse (`ALREADY_SENT` / unknown-state). Never a second `35=D` with the same `ClOrdId`.
4. Never open a socket. Host/port in the probe constructor must be `null` / `test-mode`.
5. Assert on every reject fixture:

```text
gateway.SubmitNewCount == 0
gateway.Outbound has no MsgType=D that was sent
RiskDecision.AllowFixSend == false
```

6. `apps/mt5-worker` is not a legal caller. A separate integration fact (A27 `OutboxDoesNotCallFixFromCallbackTests`) remains out of this pack but the probe must not be injectable there.

Until Application wires `Evaluate` → persist → probe, **unit** tests compose `RiskEngine` + probe directly. That is enough to fail a future “approve means send” regression. Integration tests add EF persist of `RiskDecisionRecord` + `ExecutionIntent` with `Status=NotSent` and prove the worker loop does not flip it to sent when the flag is false.

---

## 6. Family RF-SQ — stale quote

**A27 classes:** `Risk.QuoteFreshnessGuardTests`, `Risk.PriceMoveGuardTests` (spread/move are siblings, not this family’s reject), `Harness.QuoteUnavailableBlocksNewCopyTests`, `Harness.RiskRejectionBeforeFixSendTests`.

**Age formula (stub):** `DecisionTime - Quote.ReceivedAt`.  
**Age formula (law):** also `DecisionTime - VenueTimestamp` when present and sane (A23 §6.1 / A72 §3.3). Reject if **either** exceeds `max_quote_age`.

### 6.1 Cases

| Id | Mutation from golden OPEN | expect_stub | expect_law | Send probe |
|---|---|---|---|---|
| `RF-SQ-OPEN-FRESH` | none (quote T−200ms) | `Approve` / `APPROVED` / `AllowFixSend=false` if flag off | Same outcome; send only if flag on **and** conjunction | 0 unless flag-on variant |
| `RF-SQ-OPEN-STALE` | `ReceivedAt=T−30s` | `Reject` / `QUOTE_STALE` / qty 0 / send false | Same | 0 |
| `RF-SQ-OPEN-EQ-BOUNDARY` | `ReceivedAt=T−3000ms` | `Approve` (`>` not `>=`) | Same unless config later switches to `>=` — catalog must change | 0 if flag off |
| `RF-SQ-OPEN-1MS-OVER` | `ReceivedAt=T−3001ms` | `Reject` / `QUOTE_STALE` | Same | 0 |
| `RF-SQ-OPEN-MISSING` | `Quote=null` | `Reject` / `QUOTE_MISSING` | `QUOTE_UNAVAILABLE` / `QUOTE_MISSING` (either code acceptable if documented; prefer `QUOTE_UNAVAILABLE` on law lane) | 0 |
| `RF-SQ-OPEN-INVALID-CROSSED` | `bid=2400.2`, `ask=2399.8` | stub: may emit `SPREAD_TOO_WIDE` (negative spread vs `MaxAllowedSpread`) or approve | **Law:** `QUOTE_INVALID` / `QUOTE_UNAVAILABLE` **before** spread math (A72 §3.2) | 0 |
| `RF-SQ-OPEN-INVALID-NONPOS` | `bid=0` | stub: spread check / maybe approve | Law: `QUOTE_UNAVAILABLE` | 0 |
| `RF-SQ-VENUE-TS-STALE` | `ReceivedAt=T−200ms`, `VenueTimestamp=T−30s` | **Approve** (venue clock unused) | `Reject` / `QUOTE_STALE` | 0 on law |
| `RF-SQ-INC-STALE` | Action=`IncreaseExposure`, stale received | `Reject` / `QUOTE_STALE` | Same | 0 |
| `RF-SQ-RED-STALE` | Action=`ReduceExposure`, stale received, dest link set | `Approve` / `RISK_REDUCTION` / `AllowFixSend` per flag∧`KillSwitch==None` | `Approve`; quote quality `STALE`; **not** `QUOTE_STALE` reject | 0 if flag off |
| `RF-SQ-CLS-STALE` | Action=`CloseExposure`, stale received | same as RED | same as RED | 0 if flag off |
| `RF-SQ-SHADOW-OPEN-STALE` | flag off + stale + shadow path | `Reject` / `QUOTE_STALE` | Shadow OPEN also fail-closed (A24) | 0 |
| `RF-SQ-LOGON-NOT-FRESH` | `VenueHealthy=true`, quote T−30s | `QUOTE_STALE` | Logged-on ≠ fresh | 0 |
| `RF-SQ-RESEND-STALE` | prior `Approve` at T−10s, re-check at T0 with last quote T−10s | n/a (no re-check in stub) | Law: send gate re-evaluates; `QUOTE_STALE`; builder not run | 0 |

`RF-SQ-OPEN-STALE` is the G12 proof. `RF-SQ-RED-STALE` / `RF-SQ-CLS-STALE` prove §64 does not reuse the OPEN threshold.

### 6.2 What this family must **not** claim

- Passing `RF-SQ-OPEN-STALE` does **not** prove spread or price-move (A72). Those are separate golds.
- Shadow CLOSE unpriced hold (`UNPRICED_CLOSE_HELD`) is A24; only mention it as a non-blocker for live CLOSE send.
- Do not use `DateTimeOffset.UtcNow - quote.ReceivedAt` from `EfDashboardQueries` as the engine clock.

---

## 7. Family RF-SS — stale signal

**A27 classes:** `Risk.StaleCopyIntentExpiryTests`, `Replay.NoBlindCatchUpReplayTests`, `Harness.TradeUnavailableDoesNotQueueUnlimitedBacklogTests`.

**Age formula:** `DecisionTime - SourceEventTime`.  
**Expiry helper:** `CopyIntentExpiry.IsExpired(sourceEventTime, now, maxSignalAge)` — `true` when `now - source > max`. Same `>` semantics.

`RiskEvaluationRequest` today has **no** `ExpiresAt` / `MaxSignalAge` / `CollectorReceiveTime`. Stub lane cannot emit `INTENT_EXPIRED`. Law lane requires those fields.

### 7.1 Cases

| Id | Mutation | expect_stub | expect_law | Send |
|---|---|---|---|---|
| `RF-SS-OPEN-FRESH` | `SourceEventTime=T−1s` | `Approve` / `APPROVED` | Same | 0 if flag off |
| `RF-SS-OPEN-STALE` | `SourceEventTime=T−5min` | `Reject` / `SIGNAL_STALE` | Same | 0 |
| `RF-SS-OPEN-EQ-BOUNDARY` | `SourceEventTime=T−15s` | `Approve` | Same (`>` not `>=`) | 0 if flag off |
| `RF-SS-OPEN-1MS-OVER` | `SourceEventTime=T−15001ms` | `Reject` / `SIGNAL_STALE` | Same | 0 |
| `RF-SS-OPEN-EXPIRED` | `expires_at=T0−1s`, signal still 1s old | stub: **Approve** (field absent) | `Reject` / `INTENT_EXPIRED` | 0 |
| `RF-SS-INTENT-TIGHTER` | per-intent `max_signal_age=2s`, source T−5s, global 15s | stub: **Approve** (uses only global) | `Reject` / `SIGNAL_STALE` (stricter wins) | 0 |
| `RF-SS-INC-STALE` | Increase + T−5min | `SIGNAL_STALE` | Same | 0 |
| `RF-SS-RED-STALE` | Reduce + T−5min + dest link | `Approve` / `RISK_REDUCTION` | `Approve` (close clock hours, not 15s) | 0 if flag off |
| `RF-SS-CLS-STALE` | Close + T−5min | same as RED | same as RED | 0 if flag off |
| `RF-SS-CATCHUP-20` | 20 OPEN intents, `source_event_time=T0−3min`, reconnect at T0 | each `Evaluate` → `SIGNAL_STALE` | 20× `SIGNAL_STALE` or `INTENT_EXPIRED`; **zero** NOS; no backlog drain | **0** |
| `RF-SS-CATCHUP-3-CLOSES` | same outage + 3 CLOSE of existing dest | stub: 3× `Approve` / `RISK_REDUCTION` | 3 CLOSE process (A71 E11); 20 OPEN die | 0 if flag off; law may send CLOSE only if flag/flatten policy allows |

`RF-SS-CATCHUP-20` is the §63 / G13 proof. Implement as one JSON with `cases[20]` sharing `T_FIX_DOWN` and distinct `copy_intent_id` = `RF-SS-CATCHUP-20-01` … `20`.

Replay sibling `tests/Replay/Fixtures/no_blind_catchup.json` (A67) drives the full tape. This risk fixture is the **decision-only** slice so G13 does not wait on the replay harness.

---

## 8. Family RF-KS — kill switch

**A27 classes:** `Risk.KillSwitchStopNewExecutionTests`, `Risk.KillSwitchEmergencyFlattenAuthorizationTests`, `Harness.GlobalStopNewOrdersTests`.

**Stub latch:** one `KillSwitchMode` on the request and one `kill_switches.Mode` row (seeded `None` in `DemoSeeder`).  
**Law latch:** two independent bits + flatten run state (A48). `RF-KS-BOTH-BITS` is law-only until the table is migrated.

### 8.1 Cases

| Id | Setup | expect_stub | expect_law | Dest book | Send |
|---|---|---|---|---|---|
| `RF-KS-NONE-OPEN` | `Mode=None`, flag off | `Approve` / `APPROVED` / send false | Same | unchanged | 0 |
| `RF-KS-NONE-OPEN-SEND` | `Mode=None`, flag **true**, recon+venue ok | `Approve` / `AllowFixSend=true` | Same **and** probe may build `35=D` only in test-mode | unchanged until fill | test-mode 1 **only** if a later harness enables it; **this pack keeps probe refuse unless explicitly opted in** |
| `RF-KS-STOP-OPEN` | `StopNewExecution` + OPEN | `GlobalStop` / `STOP_NEW_EXECUTION` / qty 0 / send false | Same | **unchanged** | 0 |
| `RF-KS-STOP-INC` | stop-new + INCREASE | `GlobalStop` / `STOP_NEW_EXECUTION` | Same | unchanged | 0 |
| `RF-KS-STOP-RED` | stop-new + REDUCE + dest link | `Approve` / `RISK_REDUCTION` / **`AllowFixSend=false`** (because `KillSwitch!=None`) | `Approve` / `AllowFixSend=true` **if** flag on ∧ TRADE ready ∧ `allow_risk_reduction_while_stop_new=true` | reduce only after fill | stub 0; law may 1 in test-mode |
| `RF-KS-STOP-CLS` | stop-new + CLOSE | same as RED | same as RED | close only after fill | same |
| `RF-KS-STOP-NOT-FLATTEN` | stop-new, dest remaining 1.20, **no** flatten confirm | no flatten intents | **zero** flatten `CLOSE_EXPOSURE` emitted by the latch itself | dest 1.20 remains | flatten NOS = 0 |
| `RF-KS-FLAT-OPEN` | `EmergencyFlatten` + OPEN | `GlobalStop` / `EMERGENCY_FLATTEN_BLOCKS_NEW` | `EMERGENCY_FLATTEN_ACTIVE` (A23 name) | flatten run owns book | 0 for OPEN |
| `RF-KS-FLAT-CLS` | flatten mode + source CLOSE of dest flatten already owns | stub: `Approve` / `RISK_REDUCTION` (would **second-send** if wired) | **coalesce**; no second NOS (`FLATTEN_OWNS_POSITION`) | flatten-owned | 0 extra |
| `RF-KS-BOTH-BITS` | stop-new ON **and** flatten ACTIVE | **inexpressible** (exclusive enum) | both bits on; OPEN rejected; flatten closes proceed | flatten | OPEN 0 |
| `RF-KS-NOTSENT-DRAIN` | persisted OPEN `execution_intent` `NotSent` + latch flipped ON | n/a (no worker drain) | worker must **not** send; mark / leave `NotSent` | unchanged | 0 |
| `RF-KS-CLEAR-NO-CATCHUP` | latch cleared at T0; 20 expired OPENs waiting | n/a | clearing stop-new does **not** release expired intents (A48 §3.1) | unchanged | 0 |
| `RF-KS-AUDIT` | RiskManager sets stop-new | n/a (no API) | `audit_logs` row: actor, role, action `STOP_NEW.ENGAGED`, reason, `At` | n/a | 0 |
| `RF-KS-RBAC-FLATTEN` | RiskManager requests flatten | n/a | **deny** (SuperAdmin + confirm only) | untouched | 0 |
| `RF-KS-GLOBAL-STOP-NOT-FLATTEN` | daily loss breach | OPEN `GlobalStop` / `MAX_DAILY_EXECUTION_LOSS`; CLOSE **also rejected** (stub defect) | OPEN engages stop-new only; CLOSE still `Approve` (A71 E8/E9) | dest closeable | OPEN 0 |

`RF-KS-STOP-NOT-FLATTEN` is the G16 / §70.13 proof: **global stop-new works** and is not a flatten button.

### 8.2 Seed rows (integration)

```text
kill_switches:
  Id        = dddddddd-dddd-dddd-dddd-ddddddddddd1   # DemoSeeder already uses this
  Mode      = StopNewExecution | EmergencyFlatten | None
  SetBy     = "fixture.RF-KS-STOP-OPEN"
  Reason    = "B36 fixture"
  UpdatedAt = T0
```

Do not overwrite the demo seed in the running API. Integration tests use a **fresh** store / Testcontainers DB.

---

## 9. Family RF-RA — reduce allowed

**A27 class:** `Risk.OpenVsCloseExposurePolicyTests`.

One **rotten snapshot**, four actions. This is A71 examples E1–E4 compressed into a single gold file `RF-RA-MATRIX-ROTTEN.json`.

### 9.1 Rotten snapshot (all true at once)

```text
quote_age              = 30s          > max_quote_age_open (3s)
                         < max_quote_age_close lab (60s)
spread                 = 0.20         (keep tight so QUOTE_STALE is the first quote reason;
                                       a sister matrix RF-RA-MATRIX-WIDE uses spread 8.0)
signal_age             = 5 min        > max_signal_age_open (15s)
KillSwitch             = StopNewExecution
Reconciled             = true         (identity ok)
VenueHealthy           = true
RealExecutionEnabled   = false
Trader                 = not required on stub request
dest_remaining         = 1.20 long, linked DEST-XAU-10001-1
RequestedQuantity      = 0.10 (OPEN/INC) or dest-remaining path (RED/CLS)
```

First blocking reason on OPEN in stub evaluation order (A23 §5 vs stub lines 78–115):

1. `STOP_NEW_EXECUTION` (kill switch is checked **before** quote/signal)

So the OPEN control row’s `expect_stub.reason` is `STOP_NEW_EXECUTION`, **not** `QUOTE_STALE`. That is correct and must be asserted. A second matrix `RF-RA-MATRIX-QUOTE-ONLY` sets `KillSwitch=None` so the quote/signal rows are visible.

### 9.2 Matrix A — stop-new dominates (`RF-RA-MATRIX-ROTTEN`)

| Case | Action | expect_stub | expect_law |
|---|---|---|---|
| A-OPEN | `OpenExposure` | `GlobalStop` / `STOP_NEW_EXECUTION` / qty 0 / send false | Same |
| A-INC | `IncreaseExposure` | `GlobalStop` / `STOP_NEW_EXECUTION` | Same |
| A-RED | `ReduceExposure` | `Approve` / `RISK_REDUCTION` / qty 0.10 / send **false** | `Approve` dest fraction of 1.20; send false (flag off) |
| A-CLS | `CloseExposure` | `Approve` / `RISK_REDUCTION` / qty 0.10 | `Approve` qty **1.20** (dest remaining, not source 0.10) |

**Law defect to lock:** stub CLOSE uses `RequestedQuantity` as approved qty. Law uses dest remaining (A71 §8.2). `lane=law` asserts `ApprovedQuantity=1.20`.

### 9.3 Matrix B — quote/signal only (`RF-RA-MATRIX-QUOTE-ONLY`)

`KillSwitch=None`, quote T−30s, signal T−5min, flag off.

| Case | Action | expect_stub | expect_law |
|---|---|---|---|
| B-OPEN | OPEN | `Reject` / `QUOTE_STALE` (quote checked before signal) | Same |
| B-INC | INCREASE | `QUOTE_STALE` | Same |
| B-RED | REDUCE | `Approve` / `RISK_REDUCTION` | `Approve`; fill quality `STALE_QUOTE` |
| B-CLS | CLOSE | `Approve` / `RISK_REDUCTION` | Same |

Evaluation order in the stub: missing quote → stale quote → spread → price-move → **then** signal. A third row `RF-RA-MATRIX-SIGNAL-ONLY` uses a fresh quote and T−5min signal so OPEN reason is `SIGNAL_STALE`.

### 9.4 Guards that must not block CLOSE (one row each)

All: Action=`CloseExposure`, dest link present, flag off, `KillSwitch=None` unless noted. Control OPEN sibling must reject.

| Id | Mutation | OPEN expect_stub | CLOSE expect_stub | CLOSE expect_law |
|---|---|---|---|---|
| `RF-RA-SPREAD` | ask−bid = 8.0 | `SPREAD_TOO_WIDE` | `Approve` / `RISK_REDUCTION` | Approve |
| `RF-RA-MOVE` | mid vs expected = 10 | `PRICE_MOVED_TOO_FAR` | Approve | Approve |
| `RF-RA-MARTINGALE` | `MartingaleFlag=true` | `PauseTrader` / `MARTINGALE_BLOCK` | Approve | Approve |
| `RF-RA-ABNORMAL` | `AbnormalSizing=true` | `ABNORMAL_SIZING_BLOCK` | Approve | Approve |
| `RF-RA-MARGIN` | `MarginUsage=0.95` | `MAX_MARGIN_USAGE` | Approve | Approve |
| `RF-RA-POS-CAP` | `OpenPositions=20` | `MAX_OPEN_POSITIONS` | Approve | Approve |
| `RF-RA-UNRECON` | `Reconciled=false` | `VENUE_NOT_RECONCILED` | Approve | Approve if dest **known**; block if unknown-state on **this** dest |
| `RF-RA-UNHEALTHY` | `VenueHealthy=false` | `PauseVenue` / `VENUE_UNHEALTHY` | Approve | TRADE down: persist CLOSE `NotSent`, do not convert to OPEN expire |
| `RF-RA-LOSS-EXIT` | `TraderRealizedLoss=-500`, `DailyExecutionPnl=-2000`, `PortfolioDrawdown=3000` | first hit `PauseTrader` / `MAX_LOSS_PER_TRADER` | **`PauseTrader` / `MAX_LOSS_PER_TRADER`** | **`Approve` / `RISK_REDUCTION`** (A71 E8 — **stub defect**) |
| `RF-RA-NO-DEST-LINK` | CLOSE, no dest id | `Approve` / `RISK_REDUCTION` | same (too loose) | `Reject` / `NO_DESTINATION_POSITION` / `MAPPING_MISSING` |
| `RF-RA-REVERSAL` | CLOSE then leftover OPEN on rotten snapshot + stop-new | CLOSE Approve; OPEN `STOP_NEW_EXECUTION` | same | dest **flat**; leftover OPEN rejected (A71 E5) |

`RF-RA-LOSS-EXIT` is the most important honesty row in this pack. A green stub test that expects CLOSE reject would **cement the defect**. Split:

- `RF-RA-LOSS-EXIT.stub` — documents current freeze-exits behavior (`PauseTrader`) so a drive-by edit is visible.
- `RF-RA-LOSS-EXIT.law` — **required FAIL today**; go-live cannot PASS G11 while this fails.

### 9.5 What “reduce allowed” is not

| Not this | Why |
|---|---|
| `RiskDecisionOutcome.ReduceSize` on `MAX_XAU_NET` | That path calls `Reject()` and returns qty **0** (A71 §14). Separate sizing fixture. |
| Approving CLOSE without a dest link | Inventing a short-lived open to have something to flatten is forbidden. |
| Blind catch-up of never-copied entries | `NO_DESTINATION_POSITION` is terminal, not a delayed OPEN. |

---

## 10. Family RF-RB — real send blocked when flag false

**A27 classes:** `Risk.RealExecutionFeatureFlagTests`, `Flags.RealExecutionDisabledIntegrationTests`, `Harness.RiskRejectionBeforeFixSendTests`.

This family has **two latches**. Both must fire.

```text
Latch 1 — RiskEngine:   AllowFixSend = RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy
Latch 2 — Send probe:   refuse 35=D unless config REAL_COPY_EXECUTION_ENABLED==true AND AllowFixSend
```

A future bug that sets `AllowFixSend=true` while the env flag is false must still produce `SubmitNewCount=0` (A49 second latch).

### 10.1 Engine cases

| Id | Flags / action | expect_stub | expect_law | Probe |
|---|---|---|---|---|
| `RF-RB-OPEN-FLAG-OFF` | flag false, golden OPEN | `Approve` / `APPROVED` / **`AllowFixSend=false`** | Same + persist `REAL_COPY_DISABLED` as secondary reason or decision note | 0 |
| `RF-RB-INC-FLAG-OFF` | flag false, INCREASE | `Approve` / send false | Same | 0 |
| `RF-RB-RED-FLAG-OFF` | flag false, REDUCE | `Approve` / `RISK_REDUCTION` / send false | v1 source-driven CLOSE: still no NOS | 0 |
| `RF-RB-CLS-FLAG-OFF` | flag false, CLOSE | same | same (flatten exception is **out of this pack**) | 0 |
| `RF-RB-OPEN-FLAG-ON` | flag **true**, `KillSwitch=None`, recon+venue | `Approve` / **`AllowFixSend=true`** | Same; still no live TCP | probe may record a **would-send** only in test-mode; default harness keeps `DiagnosticLogonOnly` |
| `RF-RB-FLAG-ON-BUT-KILL` | flag true + stop-new + OPEN | `GlobalStop` / send false | Same | 0 |
| `RF-RB-FLAG-ON-BUT-UNRECON` | flag true + `Reconciled=false` + OPEN | `VENUE_NOT_RECONCILED` / send false | Same | 0 |
| `RF-RB-FLAG-ON-BUT-UNHEALTHY` | flag true + `VenueHealthy=false` + OPEN | `VENUE_UNHEALTHY` / send false | Same | 0 |
| `RF-RB-FLAG-ON-STALE-QUOTE` | flag true + quote T−30s | `QUOTE_STALE` / send false | Flag-on is not a quote waiver (A49) | 0 |
| `RF-RB-SHADOW-STILL-EVALS` | flag false, stale quote OPEN | `QUOTE_STALE` (engine still runs) | Shadow evaluates the same rules; no execution_intent | 0 |

Empty stub block (lines 90–93) does **nothing**. Fixtures must not depend on that `if`. Behavior comes only from `allowSend` (lines 147–150).

### 10.2 Config / parse cases (worker + options)

Authority: A49 §3.5. Bind **explicit** env names; default binder will miss `REAL_COPY_EXECUTION_ENABLED`.

| Id | Input | Bound `RealCopyExecutionEnabled` | Send |
|---|---|---|---|
| `RF-RB-PARSE-MISSING` | env unset, appsettings unset | **false** | 0 |
| `RF-RB-PARSE-FALSE` | `false` / `False` / `FALSE` | false | 0 |
| `RF-RB-PARSE-TRUE` | `true` | true (config only; conjunction still required) | still 0 unless full conjunction |
| `RF-RB-PARSE-GARBAGE` | `1`, `yes`, `on`, `enable`, `""` | **false** + warn (do **not** treat `1` as true) | 0 |
| `RF-RB-CONFIG-FLOOR` | env false, dashboard PATCH true | stays **false**; API 409/412 | 0 |
| `RF-RB-WORKER-REFUSE` | `CTrader:RealCopyExecutionEnabled=false`, worker loop | no NOS; current worker only stamps sessions | 0 |
| `RF-RB-NO-BACKLOG-DRAIN` | 20 approved-but-flag-off intents, then flag on | n/a today | only still-fresh intents re-evaluated; expired die (§63) | 0 for the 20 stale |

Committed config that must remain false in every gold environment file (assert in integration, do not edit them in this pass):

| Path | Key | Required value |
|---|---|---|
| `D:\Prop\apps\api\appsettings.json` | `CTrader:RealCopyExecutionEnabled` | `false` |
| `D:\Prop\apps\api\Program.cs` | public flags map | `REAL_COPY_EXECUTION_ENABLED=false` |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | property default | `false` |
| `D:\Prop\.env.example` | `REAL_COPY_EXECUTION_ENABLED` | `false` |
| `D:\Prop\apps\fix-worker\Worker.cs` | `GetValue(..., false)` | default false |
| `EfDashboardQueries` | `RealCopyEnabled` / `ExecutionEnabled` | hardcoded `false` (honest today; later must read the real flag, never invent `true`) |

`apps/fix-worker/appsettings.json` currently has **no** CTrader section. `RF-RB-PARSE-MISSING` is the measured worker state.

### 10.3 High-score cannot bypass the flag

`RF-RB-SCORE-CANNOT-SEND`: attach advisory `confidence=0.99`, `suggested_allocation=1.0` (when the DTO grows). Flag false + golden OPEN → `Approve` + `AllowFixSend=false` + probe 0. A27 `Risk.ScoringCannotBypassRiskTests` owns the sibling (hard limits vs score). This row owns **flag vs score**.

---

## 11. JSON document format (`ti.risk.fixture/v1`)

Compatible in spirit with A67 envelopes. Risk fixtures are **decision snapshots**, not MT5 tapes.

```json
{
  "$schema": "ti.risk.fixture/v1",
  "schema_version": "1.0.0",
  "fixture_id": "RF-SQ-OPEN-STALE",
  "family": "stale_quote",
  "title": "OPEN rejected when destination quote is 30s old",
  "architecture_refs": ["§31", "§37", "§68", "A23§6.1", "A72", "A100-G12"],
  "a27_tests": [
    "Risk.QuoteFreshnessGuardTests",
    "Harness.RiskRejectionBeforeFixSendTests"
  ],
  "lanes": ["stub", "law"],
  "clock": {
    "decision_time": "2026-08-18T12:00:00.000Z"
  },
  "limits": { "max_quote_age_ms": 3000 },
  "flags": {
    "real_copy_execution_enabled": false,
    "allow_risk_reduction_while_stop_new": true
  },
  "request": {
    "copy_intent_id": "RF-SQ-OPEN-STALE",
    "broker_id": "ACHIEVER",
    "source_login": 10001,
    "action": "OPEN_EXPOSURE",
    "requested_quantity": "0.10",
    "expected_price": "2400.00",
    "source_event_time": "2026-08-18T11:59:59.000Z",
    "decision_time": "2026-08-18T12:00:00.000Z",
    "quote": {
      "canonical_symbol": "XAUUSD",
      "venue_instrument_id": "1",
      "bid": "2399.90",
      "ask": "2400.10",
      "received_at": "2026-08-18T11:59:30.000Z",
      "venue_timestamp": "2026-08-18T11:59:30.000Z"
    },
    "venue_healthy": true,
    "real_execution_enabled": false,
    "reconciled": true,
    "kill_switch": "None",
    "trader_realized_loss": "0",
    "daily_execution_pnl": "0",
    "portfolio_drawdown": "0",
    "current_gross_xau": "0",
    "current_net_xau": "0",
    "open_positions": 0,
    "margin_usage": "0.10",
    "martingale_flag": false,
    "abnormal_sizing": false,
    "linked_destination_position_id": null
  },
  "expect_stub": {
    "outcome": "Reject",
    "reason": "QUOTE_STALE",
    "approved_quantity": "0",
    "allow_fix_send": false,
    "submit_new_count": 0
  },
  "expect_law": {
    "outcome": "Reject",
    "reason": "QUOTE_STALE",
    "approved_quantity": "0",
    "allow_fix_send": false,
    "submit_new_count": 0,
    "telemetry": {
      "quote_age_ms": 30000,
      "signal_age_ms": 1000,
      "spread": "0.20"
    }
  }
}
```

### 11.1 Wire enums (persist these strings, not 0/1/2)

| JSON | C# |
|---|---|
| `OPEN_EXPOSURE` / `INCREASE_EXPOSURE` / `REDUCE_EXPOSURE` / `CLOSE_EXPOSURE` | `CopyIntentAction` |
| `None` / `StopNewExecution` / `EmergencyFlatten` | `KillSwitchMode` (stub) |
| `stop_new` / `flatten_active` booleans | law latch (A48) |
| `Approve` / `ReduceSize` / `Reject` / `PauseTrader` / `PauseVenue` / `GlobalStop` | `RiskDecisionOutcome` |

Decimals are JSON **strings** to avoid binary float drift in catalog hashes.

### 11.2 Multi-case document

`RF-RA-MATRIX-*.json` and `RF-SS-CATCHUP-20.json` use `cases[]`. Each case has its own `case_id`, `request` overlay (JSON merge patch from a shared `base_request`), and the two expect objects.

### 11.3 Loader contract (proposed)

```text
RiskFixtureLoader.Load(path) → RiskFixtureDocument
RiskEvaluationRequestFactory.From(document, case) → RiskEvaluationRequest
  unknown JSON fields → fail (do not drop)
  missing required request field → fail (do not default silently)
  action wire name → enum
```

Factory may set `linked_destination_position_id` only on a **future** request DTO. Until then, law-lane cases that require the field stay FAIL with reason `INTENT_INCOMPLETE` / loader skip-not-allowed.

---

## 12. Catalog (`tests/Fixtures/Risk/catalog.tsv`)

Header (TAB-separated):

```text
fixture_id	family	action	lane	expect_outcome	expect_reason	expect_allow_send	submit_new	a27_class	gate	notes
```

Authoritative rows (abbreviated `notes`). Implementers copy this table into the TSV; SHA-256 the file in the Replay catalog style if desired.

### 12.1 Stale quote

```text
RF-SQ-OPEN-FRESH	stale_quote	OPEN_EXPOSURE	both	Approve	APPROVED	false	0	QuoteFreshnessGuardTests	G12	flag off
RF-SQ-OPEN-STALE	stale_quote	OPEN_EXPOSURE	both	Reject	QUOTE_STALE	false	0	QuoteFreshnessGuardTests	G12	T-30s
RF-SQ-OPEN-EQ-BOUNDARY	stale_quote	OPEN_EXPOSURE	both	Approve	APPROVED	false	0	QuoteFreshnessGuardTests	G12	age==3s
RF-SQ-OPEN-1MS-OVER	stale_quote	OPEN_EXPOSURE	both	Reject	QUOTE_STALE	false	0	QuoteFreshnessGuardTests	G12	age==3001ms
RF-SQ-OPEN-MISSING	stale_quote	OPEN_EXPOSURE	stub	Reject	QUOTE_MISSING	false	0	QuoteFreshnessGuardTests	G12	
RF-SQ-OPEN-INVALID-CROSSED	stale_quote	OPEN_EXPOSURE	law	Reject	QUOTE_UNAVAILABLE	false	0	QuoteFreshnessGuardTests	G12	stub diverges
RF-SQ-VENUE-TS-STALE	stale_quote	OPEN_EXPOSURE	law	Reject	QUOTE_STALE	false	0	QuoteFreshnessGuardTests	G12	stub currently Approve
RF-SQ-INC-STALE	stale_quote	INCREASE_EXPOSURE	both	Reject	QUOTE_STALE	false	0	QuoteFreshnessGuardTests	G12	
RF-SQ-RED-STALE	stale_quote	REDUCE_EXPOSURE	both	Approve	RISK_REDUCTION	false	0	OpenVsCloseExposurePolicyTests	G12+§64	
RF-SQ-CLS-STALE	stale_quote	CLOSE_EXPOSURE	both	Approve	RISK_REDUCTION	false	0	OpenVsCloseExposurePolicyTests	G12+§64	
RF-SQ-LOGON-NOT-FRESH	stale_quote	OPEN_EXPOSURE	both	Reject	QUOTE_STALE	false	0	QuoteFreshnessGuardTests	G12	
```

### 12.2 Stale signal

```text
RF-SS-OPEN-FRESH	stale_signal	OPEN_EXPOSURE	both	Approve	APPROVED	false	0	StaleCopyIntentExpiryTests	G13	
RF-SS-OPEN-STALE	stale_signal	OPEN_EXPOSURE	both	Reject	SIGNAL_STALE	false	0	StaleCopyIntentExpiryTests	G13	T-5min
RF-SS-OPEN-EQ-BOUNDARY	stale_signal	OPEN_EXPOSURE	both	Approve	APPROVED	false	0	StaleCopyIntentExpiryTests	G13	age==15s
RF-SS-OPEN-1MS-OVER	stale_signal	OPEN_EXPOSURE	both	Reject	SIGNAL_STALE	false	0	StaleCopyIntentExpiryTests	G13	
RF-SS-OPEN-EXPIRED	stale_signal	OPEN_EXPOSURE	law	Reject	INTENT_EXPIRED	false	0	StaleCopyIntentExpiryTests	G13	stub Approve
RF-SS-INC-STALE	stale_signal	INCREASE_EXPOSURE	both	Reject	SIGNAL_STALE	false	0	StaleCopyIntentExpiryTests	G13	
RF-SS-RED-STALE	stale_signal	REDUCE_EXPOSURE	both	Approve	RISK_REDUCTION	false	0	OpenVsCloseExposurePolicyTests	G13+§64	
RF-SS-CLS-STALE	stale_signal	CLOSE_EXPOSURE	both	Approve	RISK_REDUCTION	false	0	OpenVsCloseExposurePolicyTests	G13+§64	
RF-SS-CATCHUP-20	stale_signal	OPEN_EXPOSURE	both	Reject	SIGNAL_STALE	false	0	NoBlindCatchUpReplayTests	G13	20 rows
```

### 12.3 Kill switch

```text
RF-KS-STOP-OPEN	kill_switch	OPEN_EXPOSURE	both	GlobalStop	STOP_NEW_EXECUTION	false	0	KillSwitchStopNewExecutionTests	G16	
RF-KS-STOP-INC	kill_switch	INCREASE_EXPOSURE	both	GlobalStop	STOP_NEW_EXECUTION	false	0	KillSwitchStopNewExecutionTests	G16	
RF-KS-STOP-RED	kill_switch	REDUCE_EXPOSURE	stub	Approve	RISK_REDUCTION	false	0	KillSwitchStopNewExecutionTests	G16	AllowFixSend false
RF-KS-STOP-CLS	kill_switch	CLOSE_EXPOSURE	stub	Approve	RISK_REDUCTION	false	0	KillSwitchStopNewExecutionTests	G16	
RF-KS-STOP-CLS-SEND	kill_switch	CLOSE_EXPOSURE	law	Approve	RISK_REDUCTION	true	0	KillSwitchStopNewExecutionTests	G16	send true only if flag on; this pack flag off ⇒ 0
RF-KS-STOP-NOT-FLATTEN	kill_switch	n/a	law	n/a	n/a	false	0	GlobalStopNewOrdersTests	G16	dest qty unchanged
RF-KS-FLAT-OPEN	kill_switch	OPEN_EXPOSURE	stub	GlobalStop	EMERGENCY_FLATTEN_BLOCKS_NEW	false	0	KillSwitchEmergencyFlattenAuthorizationTests	G16	
RF-KS-BOTH-BITS	kill_switch	OPEN_EXPOSURE	law	GlobalStop	STOP_NEW_EXECUTION	false	0	KillSwitchEmergencyFlattenAuthorizationTests	G16	inexpressible on stub
RF-KS-NOTSENT-DRAIN	kill_switch	OPEN_EXPOSURE	law	GlobalStop	STOP_NEW_EXECUTION	false	0	GlobalStopNewOrdersTests	G16	
RF-KS-GLOBAL-STOP-NOT-FLATTEN	kill_switch	CLOSE_EXPOSURE	law	Approve	RISK_REDUCTION	false	0	OpenVsCloseExposurePolicyTests	G16	stub currently PauseTrader/GlobalStop
```

### 12.4 Reduce allowed

```text
RF-RA-ROTTEN-OPEN	reduce_allowed	OPEN_EXPOSURE	both	GlobalStop	STOP_NEW_EXECUTION	false	0	OpenVsCloseExposurePolicyTests	§64	
RF-RA-ROTTEN-CLS	reduce_allowed	CLOSE_EXPOSURE	both	Approve	RISK_REDUCTION	false	0	OpenVsCloseExposurePolicyTests	§64	
RF-RA-QUOTE-ONLY-OPEN	reduce_allowed	OPEN_EXPOSURE	both	Reject	QUOTE_STALE	false	0	OpenVsCloseExposurePolicyTests	§64	
RF-RA-QUOTE-ONLY-CLS	reduce_allowed	CLOSE_EXPOSURE	both	Approve	RISK_REDUCTION	false	0	OpenVsCloseExposurePolicyTests	§64	
RF-RA-SIGNAL-ONLY-OPEN	reduce_allowed	OPEN_EXPOSURE	both	Reject	SIGNAL_STALE	false	0	OpenVsCloseExposurePolicyTests	§64	
RF-RA-LOSS-EXIT	reduce_allowed	CLOSE_EXPOSURE	law	Approve	RISK_REDUCTION	false	0	OpenVsCloseExposurePolicyTests	§64	stub PauseTrader
RF-RA-NO-DEST-LINK	reduce_allowed	CLOSE_EXPOSURE	law	Reject	NO_DESTINATION_POSITION	false	0	OpenVsCloseExposurePolicyTests	§64	stub Approve
RF-RA-REVERSAL	reduce_allowed	CLOSE+OPEN	law	Approve then GlobalStop	RISK_REDUCTION + STOP_NEW_EXECUTION	false	0	OpenVsCloseExposurePolicyTests	§64	remain flat
```

### 12.5 Real send blocked

```text
RF-RB-OPEN-FLAG-OFF	real_send_blocked	OPEN_EXPOSURE	both	Approve	APPROVED	false	0	RealExecutionFeatureFlagTests	G11/§70.12	
RF-RB-RED-FLAG-OFF	real_send_blocked	REDUCE_EXPOSURE	both	Approve	RISK_REDUCTION	false	0	RealExecutionFeatureFlagTests	§70.12	
RF-RB-CLS-FLAG-OFF	real_send_blocked	CLOSE_EXPOSURE	both	Approve	RISK_REDUCTION	false	0	RealExecutionFeatureFlagTests	§70.12	
RF-RB-OPEN-FLAG-ON	real_send_blocked	OPEN_EXPOSURE	both	Approve	APPROVED	true	0	RealExecutionFeatureFlagTests	§70.12	probe still test-mode
RF-RB-FLAG-ON-BUT-KILL	real_send_blocked	OPEN_EXPOSURE	both	GlobalStop	STOP_NEW_EXECUTION	false	0	RealExecutionFeatureFlagTests	§70.12	
RF-RB-FLAG-ON-STALE-QUOTE	real_send_blocked	OPEN_EXPOSURE	both	Reject	QUOTE_STALE	false	0	RealExecutionFeatureFlagTests	§70.12	
RF-RB-PARSE-MISSING	real_send_blocked	n/a	law	n/a	REAL_COPY_DISABLED	false	0	RealExecutionDisabledIntegrationTests	§70.12	
RF-RB-PARSE-GARBAGE	real_send_blocked	n/a	law	n/a	REAL_COPY_DISABLED	false	0	RealExecutionDisabledIntegrationTests	§70.12	
RF-RB-CONFIG-FLOOR	real_send_blocked	n/a	law	n/a	REAL_COPY_DISABLED	false	0	RealExecutionDisabledIntegrationTests	§70.12	
RF-RB-WORKER-REFUSE	real_send_blocked	OPEN_EXPOSURE	law	Approve	APPROVED	false	0	RealExecutionDisabledIntegrationTests	§70.12	
```

**Count:** 11 + 9 + 10 + 8 + 10 = **48** named rows (CATCHUP-20 counts as one catalog line covering 20 evaluates). That is the minimum pack. Spread/move/sizing/martingale isolation beyond the RA matrix is **out of scope**.

---

## 13. Test method names (A27-shaped; do not implement here)

### 13.1 `Risk.QuoteFreshnessGuardTests`

```text
Open_quote_age_over_max_is_QUOTE_STALE
Open_quote_age_equal_max_is_fresh
Open_missing_quote_is_QUOTE_MISSING
Increase_stale_quote_is_QUOTE_STALE
Reduce_stale_quote_is_not_QUOTE_STALE
Close_stale_quote_is_not_QUOTE_STALE
Logged_on_session_does_not_waive_quote_age
Venue_timestamp_stale_rejects_open          // law; FAIL today
Crossed_book_is_QUOTE_UNAVAILABLE           // law; FAIL today
```

### 13.2 `Risk.StaleCopyIntentExpiryTests`

```text
Open_signal_age_over_max_is_SIGNAL_STALE
Open_signal_age_equal_max_is_fresh
Increase_stale_signal_is_SIGNAL_STALE
Reduce_stale_signal_is_approved
Close_stale_signal_is_approved
Expired_expires_at_rejects_open             // law
Per_intent_max_signal_age_stricter_wins     // law
Twenty_opens_after_three_minute_gap_do_not_send
```

### 13.3 `Risk.KillSwitchStopNewExecutionTests`

```text
Stop_new_rejects_open_with_STOP_NEW_EXECUTION
Stop_new_rejects_increase
Stop_new_approves_reduce
Stop_new_approves_close
Stop_new_does_not_emit_flatten_orders
Stop_new_does_not_change_dest_qty
Not_sent_open_intents_are_not_drained_while_latched
Clearing_latch_does_not_release_expired_intents   // law
```

### 13.4 `Risk.KillSwitchEmergencyFlattenAuthorizationTests`

```text
Flatten_mode_blocks_open
Flatten_is_not_an_alias_of_stop_new
Both_bits_can_be_on                         // law; FAIL on exclusive enum
RiskManager_cannot_confirm_flatten          // law
Source_close_of_flatten_owned_dest_does_not_second_send  // law
```

### 13.5 `Risk.OpenVsCloseExposurePolicyTests`

```text
Rotten_snapshot_open_rejects_close_approves
Quote_only_open_QUOTE_STALE_close_approves
Signal_only_open_SIGNAL_STALE_close_approves
Daily_loss_rejects_open_approves_close      // law; FAIL today
Unmapped_close_is_NO_DESTINATION_POSITION   // law
Reversal_close_then_open_leaves_flat
```

### 13.6 `Risk.RealExecutionFeatureFlagTests`

```text
Flag_false_approve_sets_AllowFixSend_false
Flag_false_reduce_does_not_send
Flag_true_alone_is_not_enough_when_quote_stale
Flag_true_and_stop_new_does_not_send
Garbage_env_parses_false
Missing_env_parses_false
Score_cannot_set_AllowFixSend
```

### 13.7 Harness / integration

```text
Harness.RiskRejectionBeforeFixSendTests.QUOTE_STALE_never_reaches_builder
Harness.RiskRejectionBeforeFixSendTests.Flag_false_never_reaches_builder
Harness.GlobalStopNewOrdersTests.Stop_new_submit_count_is_zero
Flags.RealExecutionDisabledIntegrationTests.Committed_appsettings_remain_false
Flags.RealExecutionDisabledIntegrationTests.Worker_loop_does_not_send
```

Existing five `RiskEngineTests` facts stay. New classes **load fixtures**; they should not fork a second `Base()` with different numbers.

---

## 14. Stub vs law deltas these fixtures are allowed to expose

Copied from measured `RiskEngine` + A71 §14 / B13. Fixtures do not fix them.

| # | Stub today | Law | Fixture that fails until fixed |
|---|---|---|---|
| 1 | `Evaluate` not called from Application/workers | Risk is last authority before send | `RF-RB-WORKER-REFUSE`, harness classes |
| 2 | Venue timestamp ignored | Dual clock | `RF-SQ-VENUE-TS-STALE` |
| 3 | No quote usability check | `bid<=0` / crossed → unavailable | `RF-SQ-OPEN-INVALID-*` |
| 4 | No `ExpiresAt` on request | `INTENT_EXPIRED` | `RF-SS-OPEN-EXPIRED` |
| 5 | Single signal clock | Family clocks | `RF-SS-RED-STALE` law telemetry |
| 6 | Exclusive `KillSwitchMode` | Two bits | `RF-KS-BOTH-BITS` |
| 7 | `AllowFixSend` requires `KillSwitch==None` | Stop-new still allows CLOSE send (default) | `RF-KS-STOP-CLS-SEND` |
| 8 | Loss / daily / DD apply to CLOSE | Exits stay open | `RF-RA-LOSS-EXIT` |
| 9 | CLOSE always `Approve` if it reaches the branch | Need dest link / qty clip / unknown-state | `RF-RA-NO-DEST-LINK` |
| 10 | CLOSE qty = `RequestedQuantity` | Dest remaining | `RF-RA-ROTTEN-CLS` law qty 1.20 |
| 11 | `ReduceSize` via `Reject()` ⇒ qty 0 | Positive stepped qty | **out of this pack** |
| 12 | Empty `if (RealExecutionEnabled==false)` | Second latch on the send function | `RF-RB-*` probe |
| 13 | `MaxQuoteAge` 3s vs `MaxQuoteAgeMs` 5000 | One configured value | catalog pin `3000` + a later binder test |
| 14 | Flatten does not emit closes | A48 run | `RF-KS-STOP-NOT-FLATTEN` proves stop-new is **not** that run |

---

## 15. Worked goldens (copy into files in a later pass)

### 15.1 `RF-SQ-OPEN-STALE` — G12

See §11 document. Probe: `submit_new_count=0`.

### 15.2 `RF-SS-OPEN-STALE` — G13

Same envelope as §11 with:

```text
family                 = stale_signal
source_event_time      = 2026-08-18T11:55:00.000Z
quote.received_at      = 2026-08-18T11:59:59.800Z
expect_*.reason        = SIGNAL_STALE
expect_*.outcome       = Reject
```

### 15.3 `RF-KS-STOP-OPEN` + `RF-KS-STOP-CLS` — G16 pair

Shared overlay: `kill_switch=StopNewExecution`, fresh quote, fresh signal, flag false.

| | OPEN | CLOSE |
|---|---|---|
| outcome | `GlobalStop` | `Approve` |
| reason | `STOP_NEW_EXECUTION` | `RISK_REDUCTION` |
| qty | 0 | stub `0.10` / law dest remaining |
| send | false | stub false / law false (flag off) |
| dest remaining after evaluate | 1.20 | 1.20 (evaluate does not fill) |

A follow-up **book** assertion belongs on the fake position store, not on `Evaluate` (the engine is pure). `RF-KS-STOP-NOT-FLATTEN` is that store test: after OPEN reject, dest qty still 1.20 and flatten target list empty.

### 15.4 `RF-RA-MATRIX-QUOTE-ONLY` — §64

`kill_switch=None`, quote T−30s, signal T−5min.

```text
OPEN  → Reject  QUOTE_STALE     send 0
INC   → Reject  QUOTE_STALE     send 0
RED   → Approve RISK_REDUCTION  send 0
CLS   → Approve RISK_REDUCTION  send 0
```

High ML score overlay (`confidence=0.99`) on the OPEN row still `QUOTE_STALE` (A71 E14).

### 15.5 `RF-RB-OPEN-FLAG-OFF` — §70.12

Golden OPEN, `real_execution_enabled=false`:

```text
outcome          = Approve
reason           = APPROVED
approved_qty     = 0.10
allow_fix_send   = false
submit_new_count = 0
secondary        = REAL_COPY_DISABLED   # law persist; stub has no secondary field
```

This is the case people will try to “simplify” into `Reject`. Do not. Shadow and dry-run need an **approval that cannot send**. The flag is not a risk reject; it is a send latch.

---

## 16. Composition with Application / worker (when they exist)

Until then, unit tests compose by hand. Target pipeline for integration (A23 §1):

```text
fixture.request
    → persist CopyIntent (Pending)
    → RiskEngine.Evaluate
    → persist RiskDecisionRecord
    → if Approve|ReduceSize AND live path:
          persist ExecutionIntent (NotSent, unique ClOrdId)
    → IFixTradeGateway.TrySendNewOrderSingle
          refuses unless flag ∧ AllowFixSend ∧ … 
    → assert probe + tables
```

Never: MT5 callback → `35=D`.  
Never: approve minutes earlier reused as a capability token without re-check (`RF-SQ-RESEND-STALE`).

Dashboard: `GetRiskAsync` should eventually surface `RecentRejectReasons` from these decisions (`QUOTE_STALE`, `SIGNAL_STALE`, `STOP_NEW_EXECUTION`). Not a fixture PASS criterion, but the reason **strings in this file are the metric labels**.

---

## 17. What this pass does **not** do

| Forbidden | Why |
|---|---|
| Edit `RiskEngine.cs` or any product `.cs` | Task: design only |
| Add `tests/Fixtures/Risk/*.json` | Design lives in this markdown; A67 same rule |
| Add A27 test classes | Later coding task; reviewer + test loop |
| Enable `REAL_COPY_EXECUTION_ENABLED` | Config floor stays false |
| Hit live TRADE `5212` / host `p.c-trader.com` | §61 |
| Treat exclusive `KillSwitchMode` as A48-complete | Fixture `RF-KS-BOTH-BITS` exists to stop that |
| Greenwash G12/G13/G16 as PASS because five unit facts exist | A100 still FAIL: dead path, no send probe, stub defects |
| Invent dest positions to close | `RF-RA-NO-DEST-LINK` |
| Use source lots as dest qty | RA law qty is dest remaining |
| Skip `lane=law` because it fails | Failures are the backlog |

---

## 18. Go-live mapping (honest)

| Gate | Fixture that must PASS on **law** lane | Current |
|---|---|---|
| G12 stale quote rejection | `RF-SQ-OPEN-STALE` + `RF-SQ-RED-STALE` + dual-clock + config not two magic numbers | **FAIL** — stub fact only, dead path, 3s vs 5s |
| G13 stale signal rejection | `RF-SS-OPEN-STALE` + `RF-SS-CATCHUP-20` + `expires_at` | **FAIL** — stub fact only, no catch-up harness |
| G16 kill switch tested | `RF-KS-STOP-OPEN` + `RF-KS-STOP-NOT-FLATTEN` + audit + two bits | **FAIL** — exclusive enum, no API, close send latch wrong |
| §64 reduce ≠ open | `RF-RA-MATRIX-*` + `RF-RA-LOSS-EXIT` law | **FAIL** — family split started; loss freezes exits |
| §70.11 reject before send | every reject row `submit_new_count=0` through the probe | **FAIL** — no gateway |
| §70.12 flag off | `RF-RB-OPEN-FLAG-OFF` + parse + worker | **FAIL** — safe by absence of NOS, gate incomplete |

**PASS rule for this pack:** all `lane=stub` rows match `Evaluate` **and** all `lane=law` rows match A23/A48/A49/A71 **and** every reject/flag-off row has `submit_new_count=0` on a real `IFixTradeGateway` implementation **and** `Evaluate` is on the Application send path. Until then A100 boxes stay unchecked.

---

## 19. Evidence pins (read this pass)

| Path | What was used |
|---|---|
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | Limits, request, evaluate order, `allowSend`, reject helper |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | Five facts + `Base()` clock |
| `D:\Prop\src\Domain\Enums\KillSwitchMode.cs` | Exclusive enum |
| `D:\Prop\src\Domain\Enums\CopyIntentAction.cs` | Four §64 tokens |
| `D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs` | `>` age helper |
| `D:\Prop\src\Domain\Entities\KillSwitch.cs` | Persisted `Mode` |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Seed `Mode=None`, quote row |
| `D:\Prop\apps\fix-worker\Worker.cs` | Flag read, no NOS |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | Default false, `MaxQuoteAgeMs=5000` |
| `D:\Prop\apps\api\appsettings.json` | `RealCopyExecutionEnabled: false` |
| `D:\Prop\docs\risk.md` | Send requires flag **and** `AllowFixSend` |
| `D:\Prop\reports\swarm\20260818\A23_risk_engine_spec.md` | Reasons, order, tests |
| `D:\Prop\reports\swarm\20260818\A27_test_inventory.md` | Class names |
| `D:\Prop\reports\swarm\20260818\A48_kill_switch.md` | Two controls |
| `D:\Prop\reports\swarm\20260818\A49_feature_flags.md` | Send conjunction + parse |
| `D:\Prop\reports\swarm\20260818\A71_exposure_policy.md` | Reduce allowed + stub deltas |
| `D:\Prop\reports\swarm\20260818\A72_quote_guards.md` | Dual clock, usability |
| `D:\Prop\reports\swarm\20260818\A100_golive_gates.md` | G12/G13/G16 FAIL |
| `D:\Prop\reports\swarm\20260818\B13_risk_review.md` | Measured stub verdict |

---

## 20. Direct answers

| Question | Answer |
|---|---|
| Did this pass modify product source? | **No.** |
| Did this pass add fixture JSON or tests? | **No.** Design only. |
| What are the five families? | `RF-SQ` stale quote, `RF-SS` stale signal, `RF-KS` kill switch, `RF-RA` reduce allowed, `RF-RB` real send blocked when flag false. |
| Can G12/G13/G16 be checked off now? | **No.** |
| Smallest next coding task? | Loader + catalog + `lane=stub` facts for the five existing behaviors, plus a recording probe that asserts `SubmitNewCount=0`. Do not “fix” `AllowFixSend` / loss-on-close in the same change as the first loader. |
