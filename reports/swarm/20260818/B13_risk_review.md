# B13 — RiskEngine review: kill switch, stale quote, REAL_COPY default, reduce vs open

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\B13_risk_review.md` |
| Agent | B13 (risk review) |
| Date | 2026-08-18 |
| Product source edited | **No** |
| SUT | `D:\Prop\src\Domain\Risk\RiskEngine.cs` (entire file: `RiskLimits`, `DestinationQuote`, `RiskEvaluationRequest`, `RiskDecision`, `RiskEngine`) |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §31, §37, §39–§41, §62–§64, §68, §70.11–13, §72.17–18 |
| Binding siblings | A23 risk engine, A24 shadow, A48 kill switch, A49 feature flags, A71 exposure policy, A72 quote guards, A100 go-live gates |
| Tests read | `D:\Prop\tests\Unit\RiskEngineTests.cs` (5 facts). Named A27/A89 classes such as `OpenVsCloseExposurePolicyTests` / `QuoteFreshnessGuardTests` are **not on disk**. |
| Method | Read `RiskEngine.cs` and every product call site of `Evaluate(`, `KillSwitchMode`, `RealCopyExecutionEnabled`, `REAL_COPY`. Quote line numbers from the file as it exists today. Nothing answered from memory. |

---

## 0. Verdict

`RiskEngine.Evaluate` is a **pure in-process stub** that starts the right family split and the right fail-closed *vocabulary*, then fails several of the four assigned invariants in ways that would be unsafe if this method were ever placed on a live send path.

| Assigned check | Class | One-line |
|---|---|---|
| Kill switch | **EXISTS_NEEDS_REFACTOR** | Stop-new and flatten-in-progress both block `OPEN`/`INCREASE` only. Exclusive `KillSwitchMode` cannot represent “stop-new ON + flatten ACTIVE.” `AllowFixSend` requires `KillSwitch == None`, so an approved close cannot send. Loss/DD/daily-loss freeze **exits**. No flatten execution. Not wired. |
| Stale quote | **EXISTS_NEEDS_REFACTOR** | `quote_age = DecisionTime - ReceivedAt`; `age > MaxQuoteAge` (default 3 s) rejects increasing with `QUOTE_STALE`. Missing quote → `QUOTE_MISSING`. Venue clock unused. Thresholds are compile defaults, unbound from `CTraderFixOptions.MaxQuoteAgeMs=5000`. Dead path. |
| `REAL_COPY` default false | **DEFAULT_OK / GATE_INCOMPLETE** | Flag defaults **false** in options, API appsettings, dashboard DTO, worker `GetValue(..., false)`, and the unit-test fixture. `AllowFixSend` is false unless the request bit is true. Empty `if` on lines 90–93 does nothing. Engine is not the NOS gate; NOS does not exist. Safe **by absence**, not by a complete conjunction. |
| Reduce vs open | **FAMILY_SPLIT_STARTED** | `IsIncreasing` vs `IsReducing` is the correct §64 shape. Quote/spread/move/signal/kill/recon/venue-health/caps/martingale skip reduce/close. Three book-loss checks do **not**. Close of an unmapped dest always `APPROVE`. `ReduceSize` returns qty `0`. |

**Do not claim §31 / §37 / §39 / §40 / §41 / §64 are implemented.** A method nobody in Application / API / workers calls is not a go-live guard.

`grep Evaluate(` under product `*.cs` hits **only** `RiskEngine.Evaluate` and `tests/Unit/RiskEngineTests.cs`. `AddTraderIntelligence` does **not** register `RiskEngine`. Dashboard `RealCopyEnabled` is hardcoded `false`. Fix-worker still has no `NewOrderSingle`.

Go-live boxes this file owns remain **unchecked** (A100 G11, G12, G16; §68):

```text
[ ] risk engine unit/integration tests pass
[ ] stale quote rejection works
[ ] kill switch tested
```

---

## 1. What the file actually is

Single compilation unit. No `IRiskEngine`. No persistence. No config binder.

| Type | Role |
|---|---|
| `RiskLimits` | Mutable-init policy object with **hardcoded lab numbers** (`MaxQuoteAge = 3s`, `MaxAllowedSpread = 2.0`, `MaxPriceMove = 3.0`, `MaxSlippage = 1.5`, book caps, martingale/abnormal flags default on). A23 §6: production numbers must be configuration, not code constants. |
| `DestinationQuote` | Bid/ask + `ReceivedAt` + optional `VenueTimestamp`. |
| `RiskEvaluationRequest` | Snapshot the **caller** must assemble: action, qty, expected px, quote, `VenueHealthy`, `RealExecutionEnabled`, `Reconciled`, `KillSwitch`, book/PnL flags. |
| `RiskDecision` | `Outcome`, `ApprovedQuantity`, `Reason`, `AllowFixSend`. |
| `RiskEngine.Evaluate` | First-blocking-check sequence; `Reject(...)` always `ApprovedQuantity=0` and `AllowFixSend=false`. |

Adjacent types (not in this file, required to judge the four checks):

| Path | Relevance |
|---|---|
| `D:\Prop\src\Domain\Enums\KillSwitchMode.cs` | `None=0`, `StopNewExecution=1`, `EmergencyFlatten=2` — **mutually exclusive**. |
| `D:\Prop\src\Domain\Enums\CopyIntentAction.cs` | Four §64 tokens. |
| `D:\Prop\src\Domain\Enums\RiskDecisionOutcome.cs` | Includes `ReduceSize`. |
| `D:\Prop\src\Domain\Entities\KillSwitch.cs` | Persisted exclusive `Mode`. Seeded `None`. |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | `RealCopyExecutionEnabled = false`; `MaxQuoteAgeMs = 5000`. |
| `D:\Prop\apps\api\appsettings.json` | `"RealCopyExecutionEnabled": false`. |
| `D:\Prop\apps\api\Program.cs` | `/api/settings` hardcodes `REAL_COPY_EXECUTION_ENABLED=false`, `maxQuoteAgeSeconds=3`. |
| `D:\Prop\apps\fix-worker\Worker.cs` | Reads `CTrader:RealCopyExecutionEnabled` default **false**; no send path. |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | Overview / risk / FIX session all report `RealCopyEnabled` / `ExecutionEnabled` as **false** literals. |

---

## 2. Kill switch

### 2.1 Architecture law (§40, A23 §8, A48)

Two **independent** controls:

| Control | Must do | Must not do |
|---|---|---|
| `STOP_NEW_EXECUTION` | Block `OPEN_EXPOSURE` / `INCREASE_EXPOSURE`. Leave dest book untouched. | Flatten, cancel-all, or share a single bool with flatten. |
| `EMERGENCY_FLATTEN` | Separately permissioned **run** that closes known dest positions (`CLOSE_EXPOSURE`). While active, also block new opens. | Be an alias of stop-new. Auto-fire from `GLOBAL_STOP`. |

At any instant the system may be `{stop-new off\|on} × {flatten idle\|active}`. A mutually exclusive enum used as SoT is an A48 violation.

Reduce/close of mapped dest positions **may** still be approved while stop-new is on (default `allow_risk_reduction_while_stop_new=true`). Daily-loss / trader-loss / portfolio-DD engage **stop-new**; they must not freeze exits (A71 §7.2, example E8).

### 2.2 What `Evaluate` does (measured)

```78:82:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.KillSwitch == KillSwitchMode.StopNewExecution && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.GlobalStop, "STOP_NEW_EXECUTION");

        if (request.KillSwitch == KillSwitchMode.EmergencyFlatten && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.GlobalStop, "EMERGENCY_FLATTEN_BLOCKS_NEW");
```

```147:161:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;

        if (IsReducing(request.Action))
        {
            return new RiskDecision
            {
                CopyIntentId = request.CopyIntentId,
                Outcome = RiskDecisionOutcome.Approve,
                ApprovedQuantity = request.RequestedQuantity,
                Reason = "RISK_REDUCTION",
                AllowFixSend = allowSend
            };
        }
```

| Required behavior | Measured | Class |
|---|---|---|
| Stop-new ≠ flatten | Two enum values, two reason codes | **Directionally correct** |
| Stop-new blocks only increasing | `&& IsIncreasing(...)` | **PASS** on this predicate |
| Flatten-in-progress blocks only increasing | Same shape, reason `EMERGENCY_FLATTEN_BLOCKS_NEW` | **PASS** as “block new”; **FAIL** as flatten (no close emission, no dest snapshot, no `cl_ord_id`) |
| Both can be on at once | Impossible: `KillSwitchMode` is exclusive | **FAIL** (A48) |
| Stop-new leaves dest book untouched | Engine never mutates positions (it also never consults them) | **PASS by omission** |
| Reduce/close still approvable under stop-new | They skip the two `if`s | **PASS** on `Outcome` |
| Reduce/close still sendable under stop-new (default policy) | `allowSend` requires `KillSwitch == None` | **FAIL** — approved close has `AllowFixSend=false` |
| Engine-raised daily-loss / DD engages stop-new only, not flatten | Returns `GlobalStop` and **also rejects reduce/close** (no `IsIncreasing` on lines 117–124) | **FAIL** — would freeze exits |
| Durable latch + audit + RBAC | Caller-supplied enum; no persist; no API | **MISSING** |
| On the send path | No call sites | **DEAD** |

Loss / daily PnL / drawdown (apply to **every** action, including `CloseExposure`):

```117:124:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.TraderRealizedLoss <= -_limits.MaxLossPerTrader)
            return Reject(request, RiskDecisionOutcome.PauseTrader, "MAX_LOSS_PER_TRADER");

        if (request.DailyExecutionPnl <= -_limits.MaxDailyExecutionLoss)
            return Reject(request, RiskDecisionOutcome.GlobalStop, "MAX_DAILY_EXECUTION_LOSS");

        if (request.PortfolioDrawdown >= _limits.MaxPortfolioDrawdown)
            return Reject(request, RiskDecisionOutcome.GlobalStop, "MAX_PORTFOLIO_DRAWDOWN");
```

A losing day therefore cannot close a copied XAU position through this engine. That is the opposite of §64 / A71 E8.

`GLOBAL_STOP` as an *outcome* is not a durable latch write. A23 §8.3: engagement of the kill-switch record is an audited state transition, not a boolean flipped in the hot path. Here the outcome is returned and forgotten.

Dashboard `GetRiskAsync` reports `(ks?.Mode ?? None).ToString()` and `RealCopyEnabled=false`. It does **not** show flatten *availability* separately from stop-new state (§53).

### 2.3 Tests that exist

`Stop_new_execution_blocks_opens_not_closes` (`RiskEngineTests.cs`):

- Open + `StopNewExecution` → `GlobalStop` (does not assert reason `STOP_NEW_EXECUTION`).
- Close + `StopNewExecution` → `Approve`, `AllowFixSend=false`.

The close assertion **encodes the send defect** as expected behavior. A later implementer can “keep the test green” while remaining unable to flatten or source-close under stop-new.

Missing vs A27/A48:

- `IncreaseExposure` under stop-new.
- `ReduceExposure` under stop-new (only `CloseExposure` is tested).
- `EmergencyFlatten` vs stop-new distinctness.
- Stop-new does not emit flatten NOS.
- Loss/DD must not reject close.
- Durable latch + audit.

**Kill-switch verdict:** the *predicate* “do not open more” is sketched. The *control* required by §40 is not.

---

## 3. Stale quote

### 3.1 Architecture law (§31, §37, A23 §6.1, A72)

```text
quote_age = decision_time - quote_received_timestamp
if venue timestamp present: also reject if venue_quote_age > max
if quote missing OR quote session unhealthy: reject OPEN/INCREASE  (QUOTE_UNAVAILABLE)
if quote_age > configured_max_quote_age:     reject OPEN/INCREASE  (QUOTE_STALE)
if spread > max_allowed_spread:              reject OPEN/INCREASE  (SPREAD_TOO_WIDE)
```

Threshold **must be configurable and measured**. Logged-on ≠ fresh. Reduce/close use a **separate** (much looser) clock; they must not share the 3 s open cap as a blocker (A71 §7.2).

### 3.2 What `Evaluate` does (measured)

Limits:

```15:18:D:\Prop\src\Domain\Risk\RiskEngine.cs
    public TimeSpan MaxQuoteAge { get; init; } = TimeSpan.FromSeconds(3);
    public TimeSpan MaxSourceSignalAge { get; init; } = TimeSpan.FromSeconds(15);
    public decimal MaxPriceMove { get; init; } = 3.0m;
    public decimal MaxSlippage { get; init; } = 1.5m;
```

Hot path:

```95:115:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.Quote is null && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "QUOTE_MISSING");

        if (request.Quote is not null)
        {
            var age = request.DecisionTime - request.Quote.ReceivedAt;
            if (age > _limits.MaxQuoteAge && IsIncreasing(request.Action))
                return Reject(request, RiskDecisionOutcome.Reject, "QUOTE_STALE");

            var spread = request.Quote.Ask - request.Quote.Bid;
            if (spread > _limits.MaxAllowedSpread && IsIncreasing(request.Action))
                return Reject(request, RiskDecisionOutcome.Reject, "SPREAD_TOO_WIDE");

            var mid = (request.Quote.Bid + request.Quote.Ask) / 2m;
            if (Math.Abs(mid - request.ExpectedPrice) > _limits.MaxPriceMove && IsIncreasing(request.Action))
                return Reject(request, RiskDecisionOutcome.Reject, "PRICE_MOVED_TOO_FAR");
        }

        var signalAge = request.DecisionTime - request.SourceEventTime;
        if (signalAge > _limits.MaxSourceSignalAge && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "SIGNAL_STALE");
```

| Required behavior | Measured | Class |
|---|---|---|
| Reject increasing when `quote_age > max` | Yes; strict `>` so `age == MaxQuoteAge` is allowed | **PASS** on this predicate |
| Reason `QUOTE_STALE` | Yes | **PASS** |
| Missing quote ≡ reject open/increase | `QUOTE_MISSING` (A23 also allows `QUOTE_UNAVAILABLE`) | **PASS** vocabulary-adjacent |
| Family split: reduce/close not blocked by stale/missing/spread/move/signal | All five checks are `&& IsIncreasing` | **PASS** on this file |
| Dual clock (`VenueTimestamp`) | Field exists on `DestinationQuote`; **never read** | **MISSING** |
| Quote usability (`bid<=0`, `ask<bid`, crossed) | Not checked | **MISSING** |
| Taker-touch signed adverse move | Unsigned `\|mid - expected\|` | **UNSAFE if shipped** (favorable gap rejects) |
| `MaxSlippage` | Property never read | **MISSING** |
| Configurable / measured | Compile default 3 s | **FAIL** vs §31 last sentence |
| One bound across the tree | `RiskLimits` 3 s vs `CTraderFixOptions.MaxQuoteAgeMs=5000` vs API settings `maxQuoteAgeSeconds=3` (unrelated to the engine) | **FAIL** — two silent defaults |
| QUOTE-down ≠ logged-on | `VenueHealthy` is a separate bool; not derived from quote age | Caller-dependent |
| Live + shadow send/fill path | `Evaluate` unused; `ShadowCopyEngine` records `QuoteAge` but does **not** reject stale | **DEAD / SHADOW BYPASS** |
| Destination quote feed | Entity `DestinationQuoteSnapshot` exists; FIX worker stamps session `LastInboundAt` every 15 s — **not** a quote | **MISSING feed** |

`MaxSlippage` sitting unused next to a mid-price move check is not a slippage guard. A72: price-move ≠ slippage.

Negative quote age (`ReceivedAt` in the future) is not rejected.

### 3.3 Tests that exist

`Stale_quote_rejects_open`: `ReceivedAt = DecisionTime - 30s` → `Reject` / `QUOTE_STALE` / `AllowFixSend=false`.

Not covered:

- Boundary `age == 3s` (must approve) vs `age == 3s + 1 tick` (must reject).
- `IncreaseExposure` vs `ReduceExposure` / `CloseExposure` on the same rotten quote (core §64 proof).
- Missing quote on open vs close.
- `VenueTimestamp` older than receive clock.
- Spread / price-move / slippage isolation.
- Invalid bid/ask.
- Shadow path using the same rule.

A89 rows 53–54 list `QuoteFreshnessGuardTests` / `PriceMoveAndSpreadGuardTests` as **EXISTS**. Those classes are **not in `tests/Unit`**. Do not treat A89 as measured coverage.

**Stale-quote verdict:** the open-family reject line exists and is unit-touched once. It is not configurable, not dual-clock, not on a feed, not on a send path, and not proven against reduce/close.

---

## 4. `REAL_COPY_EXECUTION_ENABLED` default false

### 4.1 Architecture law (§41, A49)

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

Connect / quote / request positions **without** placing new real orders. `NewOrderSingle` requires the flag **true** *and* runtime risk-engine healthy. The flag is a **deploy/config floor**, distinct from kill switches (A48). A dashboard button must not raise it above the config floor (A25).

A48/A49 flatten exception (later Phase 8): emergency flatten may send **reducing** NOS even if the copy flag is false, because it is exit, not new copy. v1 first-useful-version: flatten mutation is **not** shipped; if called → 409.

### 4.2 What `Evaluate` does (measured)

Request bit is required (no implicit `true`):

```44:44:D:\Prop\src\Domain\Risk\RiskEngine.cs
    public required bool RealExecutionEnabled { get; init; }
```

Empty branch — **no control flow**:

```90:93:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.RealExecutionEnabled == false && request.Action != CopyIntentAction.CloseExposure)
        {
            // Shadow path still evaluates risk but never allows FIX send.
        }
```

The comment is true of `AllowFixSend` for **all** actions, including `CloseExposure`. The `!= CloseExposure` condition is dead. A later coder can misread it as “closes bypass the flag.” They do not, because:

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

`Approve` + `AllowFixSend=false` is the intended shadow shape (A23 §5 step 2). That conjunction is **necessary** and **not sufficient**:

- Engine never sees TRADE logon, lease, or persist-before-send.
- Engine never sends. Nothing in Application calls it.
- Flatten-while-flag-false cannot be expressed (would need `AllowFixSend=true` with `RealExecutionEnabled=false` for `CLOSE` owned by a flatten run). Today that is impossible — which is the **safe** v1 outcome (no flatten, no NOS) and the **wrong** Phase-8 flatten outcome.

### 4.3 Defaults elsewhere (measured)

| Location | Default | Bound to engine? |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | `false` | **No** |
| `apps/api/appsettings.json` `CTrader:RealCopyExecutionEnabled` | `false` | **No** |
| `apps/api/Program.cs` `/api/settings` | hardcoded `false` | **No** |
| `EfDashboardQueries` overview / risk / FIX session | literal `false` | **No** |
| `apps/fix-worker/Worker.cs` `GetValue("CTrader:RealCopyExecutionEnabled", false)` | `false` | Logs only; still no NOS |
| `RiskEngineTests.Base()` | `RealExecutionEnabled = false` | Yes, for the five unit facts |
| Env name `REAL_COPY_EXECUTION_ENABLED` | **Not bound** in product | Architecture name ≠ config key |

`apps/fix-worker/appsettings.json` and Development json do **not** set the key; the worker relies on the `GetValue` default. API production-shaped `appsettings.json` does set `false`. That is the correct *default*. It is not a single named flag.

`Real_flag_false_never_allows_fix_send` proves: default fixture → `Approve` + `AllowFixSend=false`. It does **not** prove `RealExecutionEnabled=true` + healthy conjunction yields `true`, and it does not prove a worker refuse of `35=D`.

**REAL_COPY verdict:** default-false is honored wherever a default exists. The engine will not set `AllowFixSend` unless the caller lies and passes `true`. There is no product binder from env `REAL_COPY_EXECUTION_ENABLED` into `RiskEvaluationRequest`. Live copy is still **fail-closed by absence of NewOrderSingle**, not by this gate. Do not treat the stub as “flag-gated execution.”

---

## 5. Reduce vs open

### 5.1 Architecture law (§64, §63 last line, §72.18, A23 §2, A71)

Four classes, two families:

| Family | Classes | Stance |
|---|---|---|
| OPEN | `OPEN_EXPOSURE`, `INCREASE_EXPOSURE` | Strict. Fail closed on stale / unpriced / unmapped / unreconciled / stop-new. |
| CLOSE | `REDUCE_EXPOSURE`, `CLOSE_EXPOSURE` | Lenient on **market-quality** guards. Still fail closed on **identity** (known dest id, known qty, no double-close, no unknown-on-this-id). |

Opening more is optional. Residual dest risk after the source is gone is a defect. Those errors must not share one threshold.

Qty: OPEN/INCREASE from allocation + dest step. REDUCE/CLOSE from **mapped dest remaining**, not source lots × allocation.

Reversal = CLOSE then OPEN. Never one event → one leftover short dest.

### 5.2 What `Evaluate` does (measured)

```174:178:D:\Prop\src\Domain\Risk\RiskEngine.cs
    private static bool IsIncreasing(CopyIntentAction action) =>
        action is CopyIntentAction.OpenExposure or CopyIntentAction.IncreaseExposure;

    private static bool IsReducing(CopyIntentAction action) =>
        action is CopyIntentAction.ReduceExposure or CopyIntentAction.CloseExposure;
```

Guards gated on `IsIncreasing` (correct *direction*):

| Guard | Reason |
|---|---|
| Kill stop-new / flatten-blocks-new | `STOP_NEW_EXECUTION` / `EMERGENCY_FLATTEN_BLOCKS_NEW` |
| `!Reconciled` | `VENUE_NOT_RECONCILED` |
| `!VenueHealthy` | `VENUE_UNHEALTHY` |
| Quote null / stale / spread / mid-move | `QUOTE_*` / `SPREAD_TOO_WIDE` / `PRICE_MOVED_TOO_FAR` |
| Signal age | `SIGNAL_STALE` |
| Open position count / max qty / gross / net / margin | book caps |
| Martingale / abnormal | behavioral |

Guards **not** gated (wrong for CLOSE family):

| Guard | Lines | Effect on reduce/close |
|---|---|---|
| `TraderRealizedLoss` | 117–118 | `PAUSE_TRADER` — **freezes exit** |
| `DailyExecutionPnl` | 120–121 | `GLOBAL_STOP` — **freezes exit** |
| `PortfolioDrawdown` | 123–124 | `GLOBAL_STOP` — **freezes exit** |

If those three pass, reduce/close skip remaining caps and return:

```152:161:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (IsReducing(request.Action))
        {
            return new RiskDecision
            {
                ...
                Outcome = RiskDecisionOutcome.Approve,
                ApprovedQuantity = request.RequestedQuantity,
                Reason = "RISK_REDUCTION",
                AllowFixSend = allowSend
            };
        }
```

| Required CLOSE-family check (A71 §7.1) | Measured |
|---|---|
| Linked dest / shadow position remaining `> 0` | **MISSING** — no dest id on the request |
| Qty ≤ remaining; clip, never flip | **MISSING** — echoes `RequestedQuantity` |
| Same side as link | **MISSING** |
| Unknown state on **this** dest | **MISSING** (unreconciled is skipped for close) |
| Flatten already owns dest → coalesce | **MISSING** |
| TRADE + persist-before-send | Not this type’s job; `AllowFixSend` then **blocks** close if kill ≠ `None` |
| Do not invent a price | N/A (no fill here) |

So the close branch is **too strict** on PnL/DD and `AllowFixSend`, and **too loose** on identity.

OPEN-family defects that also break the split:

| Item | Why it is wrong |
|---|---|
| `MAX_OPEN_POSITIONS` on all increasing | A23 / A71: **OPEN only**. An INCREASE of an existing dest is not a new slot. |
| `MAX_XAU_NET` → `ReduceSize` via `Reject()` | `ApprovedQuantity=0`. A23: `REDUCE_SIZE` must carry a positive stepped qty. A71 §14: **defect**. |
| `MAX_XAU_GROSS` / max qty / margin | Hard `REJECT` only; no reduce-to-cap path. |
| Net check `abs(net) + requested` | Blind add; a short add that **reduces** net is treated as increasing net (A71 §8.1). |
| Single `MaxSourceSignalAge` | Two clocks required. |
| `CopyIntent` has no `linked_destination_position_id` | Classifier / mapping cannot feed the engine. |

`CopyIntentExpiry.IsExpired` is a **single** `maxSignalAge` helper, unused by `RiskEngine` (engine recomputes `DecisionTime - SourceEventTime` itself).

### 5.3 Tests that exist

Only the kill-switch close case. No `ReduceExposure` fact. No same-snapshot matrix (A71 E1 vs E2). No “no dest link → `NO_DESTINATION_POSITION`.” No “daily loss rejects open, approves close.”

A89 row 52 lists `OpenVsCloseExposurePolicyTests` as **EXISTS**. **Not on disk.**

**Reduce-vs-open verdict:** the helper names are the right law. Three un-gated loss checks plus `AllowFixSend && KillSwitch==None` plus “always approve if you get here” mean the stub would both **strand dest risk on a red day** and **approve a close of nothing**.

---

## 6. Evaluation order vs A23 §5

Specified order (abbreviated) vs this file:

| # | Spec | File |
|---|---|---|
| 1 | Infrastructure / DB available | **MISSING** |
| 2 | Feature flag (live vs shadow) | Empty `if`; send bit computed **after** all limits |
| 3 | Kill switch | First real checks — OK positionally |
| 4 | Reconciliation / unknown | `Reconciled` bool only; no `EXECUTION_STATE_UNKNOWN` |
| 5 | Venue health | `VenueHealthy` bool; no QUOTE vs TRADE split |
| 6 | Source health / stale MT5 | **MISSING** |
| 7 | Trader eligibility / state | Only martingale/abnormal flags, after book caps |
| 8 | Intent expiry + signal age | Signal age only; `CopyIntent.ExpiresAt` **not on the request** |
| 9 | Quote age / spread / signed move / slippage | Receive-age + spread + unsigned mid; no slippage |
| 10 | Sizing normalize | **MISSING** — uses `RequestedQuantity` as if already dest qty |
| 11 | Book / account caps | Present; `ReduceSize` broken |
| 12 | Martingale / abnormal | Present, increasing-only |
| 13 | Persist `risk_decision` | **MISSING** — returns a record type; nobody writes `RiskDecisionRecord` from `Evaluate` |

Fail-closed on first blocking check is implemented. The *set* of checks is incomplete and two families share the loss/DD set.

---

## 7. Wiring (why this is not yet a control)

| Layer | Finding |
|---|---|
| DI | `D:\Prop\src\Infrastructure\DependencyInjection.cs` does not register `RiskEngine` / `RiskLimits`. |
| Application | No `IRiskEngine`. Scoring (`BaselineScorer`) cannot be shown to be bypassed because nothing asks risk after scoring. |
| Persistence | `RiskDecisionRecord` exists; no code path writes one from `Evaluate`. |
| Copy / execution | `CopyIntent` and `ExecutionIntent` exist; no orchestrator calls risk then persist-before-send. |
| FIX worker | Heartbeat stub. Reads the real-copy key only to log. When `true`, logs a warning and still sends nothing. |
| Shadow | `ShadowCopyEngine` does not call `RiskEngine`. It will simulate an entry on a 30 s quote. |
| Dashboard | Risk card shows kill **mode** string + `RealCopyEnabled=false` + last reject **reasons** from whatever rows were seeded/inserted elsewhere — not from this engine. |

Safety today: **no NewOrderSingle**. That is the correct current outcome. It is not evidence that these four checks work.

---

## 8. Test inventory (measured, not A89)

File: `D:\Prop\tests\Unit\RiskEngineTests.cs`.

| Fact | Proves | Does not prove |
|---|---|---|
| `Stale_quote_rejects_open` | 30 s receive-age → `QUOTE_STALE` on default `OpenExposure` | Boundary, dual clock, reduce/close, Increase, config bind |
| `Real_flag_false_never_allows_fix_send` | Fixture default `false` → `Approve` + no send | Flag `true` path; worker refuse; flatten exception |
| `Stop_new_execution_blocks_opens_not_closes` | Open → `GlobalStop`; close → `Approve` + no send | Increase; Reduce; flatten mode; send-under-stop-new policy; dest book unchanged |
| `Unreconciled_venue_blocks_new_exposure` | Open + `Reconciled=false` → `VENUE_NOT_RECONCILED` | Close of known dest still allowed; unknown-on-this-id still blocked |
| `Stale_signal_rejected` | 5 min signal → `SIGNAL_STALE` on open | Close clock; `ExpiresAt`; catch-up of 20 intents |

No integration test touches `RiskEngine`.

---

## 9. Findings (reviewer list)

Severity: **P0** = would be wrong on a live send path or contradicts a hard §; **P1** = incomplete vs spec; **P2** = hygiene.

| ID | Sev | Topic | Finding |
|---|---|---|---|
| B13-01 | P0 | Reduce vs open | `MAX_LOSS_PER_TRADER` / `MAX_DAILY_EXECUTION_LOSS` / `MAX_PORTFOLIO_DRAWDOWN` apply to reduce/close. Red day cannot exit. Violates §64 / A71 E8. |
| B13-02 | P0 | Kill switch | `AllowFixSend` requires `KillSwitch == None`. Approved `RISK_REDUCTION` cannot send under stop-new. Default A48 policy is the opposite. |
| B13-03 | P0 | Kill switch | Exclusive `KillSwitchMode` cannot represent stop-new ON + flatten ACTIVE. A48: same violation as a single bool. |
| B13-04 | P0 | Reduce vs open | `RiskDecisionOutcome.ReduceSize` for `MAX_XAU_NET` is produced by `Reject()` → `ApprovedQuantity=0`, `AllowFixSend=false`. Not a size reduction. |
| B13-05 | P0 | Stale quote / wiring | `Evaluate` has **zero** product call sites. Shadow fills ignore it. G12 cannot PASS. |
| B13-06 | P0 | REAL_COPY | No env/config binder into the request. Worker/API defaults are independently `false`. Conjunction “flag + healthy risk + lease + recon” is not implemented. Safe only because NOS is absent. |
| B13-07 | P1 | Kill switch | `EmergencyFlatten` is a second “block new” alias. No flatten run, no dest snapshot, no CLOSE intents, no RBAC. |
| B13-08 | P1 | Stale quote | `VenueTimestamp` unused; no `QUOTE_INVALID`; unsigned mid move; `MaxSlippage` unread. |
| B13-09 | P1 | Stale quote | `MaxQuoteAge=3s` vs `MaxQuoteAgeMs=5000`. Two unbound lab numbers. §31 requires one measured config. |
| B13-10 | P1 | Reduce vs open | Close/reduce always `APPROVE` `RequestedQuantity` if they reach the branch. No mapping, clip, unknown-state, or flatten coalesce. |
| B13-11 | P1 | Reduce vs open | `MAX_OPEN_POSITIONS` blocks `IncreaseExposure`. Spec: OPEN only. |
| B13-12 | P1 | Tests | Five facts; A27/A89 named classes absent. Existing close test **locks in** `AllowFixSend=false` under stop-new. |
| B13-13 | P2 | REAL_COPY | Empty `if` (lines 90–93) special-cases `CloseExposure` in the condition and does nothing. Misleading. |
| B13-14 | P2 | Kill switch | Stop-new outcome is `GlobalStop` (acceptable as “engage stop-new”) but no latch is written. Dashboard cannot distinguish engine-raised vs operator-raised. |

---

## 10. What a later increment must change (not this task)

Do **not** treat this list as an implementation order for B13. Product source stays untouched.

1. Split SoT: `stop_new` bit × flatten-run phase (A48). Stop using exclusive `KillSwitchMode` as the only persisted state.
2. Gate loss/DD/daily-loss to OPEN family (or have them raise the latch **and** still approve CLOSE of a mapped dest).
3. Compute `AllowFixSend` **per family**: stop-new must not clear send on CLOSE when `allow_risk_reduction_while_stop_new=true`. Flatten CLOSE is the A48 exception to `REAL_COPY=false`.
4. Implement `REDUCE_SIZE` as a positive stepped qty or fall through to `REJECT` / `QTY_BELOW_MIN`. Never `ReduceSize` + qty 0.
5. Dual quote clock + signed taker-touch + bind **one** `max_quote_age_open` from config. Family clocks for signal/quote on close.
6. Require `linked_destination_position_id` + remaining qty on REDUCE/CLOSE; reject `NO_DESTINATION_POSITION` instead of approving air.
7. Register and call the engine from the copy orchestrator **before** any `execution_intent` / shadow fill. Persist `risk_decisions`. FIX worker re-checks `AllowFixSend` immediately before socket write.
8. Replace `RiskEngineTests` close assertion so it cannot greenwash B13-02. Add the A71 E1–E9 matrix.

---

## 11. Explicit non-claims

- Not “kill switch tested.”
- Not “stale quote rejection works” on a feed or send path.
- Not “REAL_COPY is an implemented NOS gate.”
- Not “§64 implemented.”
- Not “≥95%” anything.
- No product source was modified. No MQ5. No FIX send. No secrets.

---

## 12. Traceability

| Review question | Primary evidence | Law |
|---|---|---|
| Kill switch | `RiskEngine.cs` 78–82, 147–150; `KillSwitchMode.cs`; `KillSwitch.cs`; `RiskEngineTests` `Stop_new_execution_*` | §40, A23 §8, A48 |
| Stale quote | `RiskEngine.cs` 15, 95–110; `CTraderFixOptions.MaxQuoteAgeMs`; `RiskEngineTests` `Stale_quote_*` | §31, §37, A23 §6.1, A72 |
| REAL_COPY default false | Request field 44; allowSend 147; empty if 90–93; options/appsettings/worker/dashboard defaults; `Real_flag_false_*` | §41, A49 |
| Reduce vs open | `IsIncreasing` / `IsReducing` 174–178; un-gated 117–124; close branch 152–161; net `ReduceSize` 135–136 | §64, §72.18, A23 §2, A71 |
| Wiring | `DependencyInjection.cs`; `grep Evaluate(`; `Worker.cs`; `EfDashboardQueries.cs` | §32, §70.11 |
