# D13 — RiskEngine recensus: kill switch, stale quote, REAL_COPY, reduce vs open

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D13_risk_review.md` |
| Agent | D13 (risk review recensus) |
| Date | 2026-08-18 |
| Assigned | Read `RiskEngine.cs`. Write this file. Do not modify product source. |
| Product source edited | **No** |
| Test source edited | **No** |
| SUT | `D:\Prop\src\Domain\Risk\RiskEngine.cs` (entire file: `RiskLimits`, `DestinationQuote`, `RiskEvaluationRequest`, `RiskDecision`, `RiskEngine`) |
| SHA-256 | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` |
| Size | 8567 bytes, **189** lines, LF. LastWriteUtc 2026-08-18 07:38:10 |
| Tests read | `D:\Prop\tests\Unit\RiskEngineTests.cs` (5 `[Fact]`s, 87 lines, SHA-256 `7B95236489E2FE169BFC8E9F57A9E2C89F6F5F047116D9DC82CFA8820FB2DF51`) |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §31, §37, §39–§41, §62–§64, §68, §70.11–13, §72.17–18 |
| Binding siblings | `docs\risk.md`; A23, A48, A49, A71, A72, A100, A101; B13 (same subject, wave B); C03 (test gap); C33 (flatten adversarial) |
| Method | Re-read the file and every product call site of `Evaluate(`, `AllowFixSend`, `RiskLimits`, `KillSwitchMode`, `RealCopyExecutionEnabled`. Quote current line numbers. Independent of B13; same conclusion. Nothing answered from memory. |

Classification: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE` / `SAFE_BY_ABSENCE` / `DEAD`.

---

## 0. Verdict

`RiskEngine.Evaluate` is a **pure in-process stub** that starts the right §64 family split (`IsIncreasing` vs `IsReducing`) and the right fail-closed *vocabulary*, then fails several hard invariants in ways that would be **unsafe if this method were placed on a live send path**.

Live copy is **SAFE_BY_ABSENCE**, not because this gate works.

| Assigned check | Class | One-line (re-measured 2026-08-18) |
|---|---|---|
| Kill switch | **EXISTS_NEEDS_REFACTOR** | Stop-new and flatten-in-progress both block `OPEN`/`INCREASE` only. Exclusive `KillSwitchMode` cannot represent “stop-new ON + flatten ACTIVE.” `AllowFixSend` requires `KillSwitch == None`, so an approved close cannot send. Loss/DD/daily-loss freeze **exits**. No flatten execution. Not wired. |
| Stale quote | **EXISTS_NEEDS_REFACTOR** | `quote_age = DecisionTime - ReceivedAt`; `age > MaxQuoteAge` (default 3 s) rejects increasing with `QUOTE_STALE`. Missing quote → `QUOTE_MISSING`. Venue clock unused. Thresholds are compile defaults, unbound from `CTraderFixOptions.MaxQuoteAgeMs=5000`. Dead path. |
| `REAL_COPY` default false | **DEFAULT_OK / GATE_INCOMPLETE** | Flag defaults **false** in options, API appsettings, dashboard DTO, worker `GetValue(..., false)`, and the unit fixture. `AllowFixSend` is false unless the request bit is true. Empty `if` on lines 90–93 does nothing. Engine is not the NOS gate; NOS does not exist. Safe **by absence**. |
| Reduce vs open | **FAMILY_SPLIT_STARTED** | `IsIncreasing` vs `IsReducing` is the correct §64 shape. Quote/spread/move/signal/kill/recon/venue-health/caps/martingale skip reduce/close. Three book-loss checks do **not**. Close of an unmapped dest always `APPROVE`. `ReduceSize` returns qty `0`. |

**Do not claim §31 / §37 / §39 / §40 / §41 / §64 are implemented.** A method nobody in Application / API / workers calls is not a go-live guard.

`grep Evaluate(` under product `*.cs` hits **only** `RiskEngine.Evaluate` and `tests/Unit/RiskEngineTests.cs`. `AddTraderIntelligence` does **not** register `RiskEngine`. Dashboard `RealCopyEnabled` is hardcoded `false`. Fix-worker still has no `NewOrderSingle`.

**Delta vs B13:** none on the SUT. Same 189-line file, same 14 defects. C03/C33 still describe this hash. Go-live boxes this file owns remain **unchecked** (A100 G11, G12, G16; §68):

```text
[ ] risk engine unit/integration tests pass
[ ] stale quote rejection works
[ ] kill switch tested
```

---

## 1. File identity (measured)

Single compilation unit. No `IRiskEngine`. No persistence. No config binder.

| Type | Role | Lines |
|---|---|---|
| `RiskLimits` | Mutable-init policy object with **hardcoded lab numbers** | 5–22 |
| `DestinationQuote` | Bid/ask + `ReceivedAt` + optional `VenueTimestamp` (DTO; **not** the EF entity) | 24–30 |
| `RiskEvaluationRequest` | Snapshot the **caller** must assemble | 32–56 |
| `RiskDecision` | `Outcome`, `ApprovedQuantity`, `Reason`, `AllowFixSend` | 58–65 |
| `RiskEngine.Evaluate` | First-blocking-check sequence | 67–172 |
| `IsIncreasing` / `IsReducing` | §64 family helpers | 174–178 |
| `Reject` | Always `ApprovedQuantity=0`, `AllowFixSend=false` | 180–188 |

Adjacent types (not in this file, required to judge the four checks):

| Path | SHA-256 (prefix) | Relevance |
|---|---|---|
| `Domain/Enums/KillSwitchMode.cs` | `528429B0…` | `None=0`, `StopNewExecution=1`, `EmergencyFlatten=2` — **mutually exclusive** |
| `Domain/Enums/CopyIntentAction.cs` | `94BA143D…` | Four §64 tokens |
| `Domain/Enums/RiskDecisionOutcome.cs` | `A0753C0F…` | Includes `ReduceSize` |
| `Domain/Entities/KillSwitch.cs` | `68EA2D92…` | Persisted exclusive `Mode`. Seeded `None` |
| `Domain/Entities/RiskDecisionRecord.cs` | `C8FA95BF…` | Table row; **nobody writes one from `Evaluate`** |
| `Fix.CTrader/Configuration/CTraderFixOptions.cs` | — | `RealCopyExecutionEnabled = false`; `MaxQuoteAgeMs = 5000` |

Name collision: `TraderIntelligence.Domain.Risk.DestinationQuote` (this file) vs `TraderIntelligence.Domain.Entities.DestinationQuoteSnapshot` (`destination_quotes`). The engine never reads the EF snapshot.

---

## 2. Compile defaults (`RiskLimits`)

A23 §6 / architecture §31: production numbers must be **configuration**, not code constants.

| Property | Default | Read by `Evaluate`? |
|---|---|---|
| `MaxLossPerTrader` | `500` | yes (`<= -cap`, **all actions**) |
| `MaxDailyExecutionLoss` | `2_000` | yes (`<= -cap`, **all actions**) |
| `MaxPortfolioDrawdown` | `3_000` | yes (`>= cap`, **all actions**) |
| `MaxXauGrossExposure` | `20` | yes (increasing only) |
| `MaxXauNetExposure` | `10` | yes (increasing only) |
| `MaxPositionQuantity` | `5` | yes (increasing only) |
| `MaxOpenPositions` | `20` | yes (increasing only, including Increase) |
| `MaxAllowedSpread` | `2.0` | yes (increasing only) |
| `MaxQuoteAge` | `3s` | yes (`ReceivedAt` only) |
| `MaxSourceSignalAge` | `15s` | yes |
| `MaxPriceMove` | `3.0` | yes (unsigned mid) |
| `MaxSlippage` | `1.5` | **never read** |
| `MaxMarginUsage` | `0.70` | yes (increasing only) |
| `BlockMartingale` | `true` | yes (increasing only) |
| `BlockAbnormalSizing` | `true` | yes (increasing only) |

`new RiskEngine()` uses these defaults. No product binder maps `CTraderFixOptions` / `/api/settings` / env into `RiskLimits`. API `/api/settings` hardcodes `maxQuoteAgeSeconds=3` and `maxSignalAgeSeconds=15` independently of this type.

---

## 3. Evaluation order (every `return`)

Fail-closed on first blocking check **is** implemented. The *set* of checks is incomplete and two families share the loss/DD set.

| # | Predicate (current code) | Outcome | Reason | Increasing-only? | Spec (A23 §5 / A71) |
|---:|---|---|---|---|---|
| 1 | `KillSwitch==StopNewExecution && IsIncreasing` | `GlobalStop` | `STOP_NEW_EXECUTION` | yes | Directionally OK |
| 2 | `KillSwitch==EmergencyFlatten && IsIncreasing` | `GlobalStop` | `EMERGENCY_FLATTEN_BLOCKS_NEW` | yes | Block-new OK; flatten **MISSING** |
| 3 | `!Reconciled && IsIncreasing` | `Reject` | `VENUE_NOT_RECONCILED` | yes | Bool only; no per-id unknown |
| 4 | `!VenueHealthy && IsIncreasing` | `PauseVenue` | `VENUE_UNHEALTHY` | yes | No QUOTE vs TRADE split |
| — | `RealExecutionEnabled==false && Action!=CloseExposure` | *(empty body)* | — | n/a | Dead `if` (lines 90–93) |
| 5 | `Quote is null && IsIncreasing` | `Reject` | `QUOTE_MISSING` | yes | OK vocabulary-adjacent |
| 6 | `DecisionTime-ReceivedAt > MaxQuoteAge && IsIncreasing` | `Reject` | `QUOTE_STALE` | yes | Venue clock unused |
| 7 | `Ask-Bid > MaxAllowedSpread && IsIncreasing` | `Reject` | `SPREAD_TOO_WIDE` | yes | No crossed-book check |
| 8 | `\|mid-ExpectedPrice\| > MaxPriceMove && IsIncreasing` | `Reject` | `PRICE_MOVED_TOO_FAR` | yes | Unsigned mid, not taker touch |
| 9 | `DecisionTime-SourceEventTime > MaxSourceSignalAge && IsIncreasing` | `Reject` | `SIGNAL_STALE` | yes | No `expires_at` on request |
| 10 | `TraderRealizedLoss <= -MaxLossPerTrader` | `PauseTrader` | `MAX_LOSS_PER_TRADER` | **no** | **UNSAFE** — freezes close |
| 11 | `DailyExecutionPnl <= -MaxDailyExecutionLoss` | `GlobalStop` | `MAX_DAILY_EXECUTION_LOSS` | **no** | **UNSAFE** — freezes close |
| 12 | `PortfolioDrawdown >= MaxPortfolioDrawdown` | `GlobalStop` | `MAX_PORTFOLIO_DRAWDOWN` | **no** | **UNSAFE** — freezes close |
| 13 | `OpenPositions >= MaxOpenPositions && IsIncreasing` | `Reject` | `MAX_OPEN_POSITIONS` | yes | Spec: **OPEN only** |
| 14 | `RequestedQuantity > MaxPositionQuantity && IsIncreasing` | `Reject` | `MAX_POSITION_QUANTITY` | yes | Hard reject; no reduce-to-cap |
| 15 | `CurrentGrossXau + RequestedQuantity > MaxXauGross && IsIncreasing` | `Reject` | `MAX_XAU_GROSS` | yes | Blind add; no side |
| 16 | `\|CurrentNetXau\| + RequestedQuantity > MaxXauNet && IsIncreasing` | `ReduceSize` | `MAX_XAU_NET` | yes | Via `Reject()` → **qty 0** |
| 17 | `MarginUsage > MaxMarginUsage && IsIncreasing` | `Reject` | `MAX_MARGIN_USAGE` | yes | Hard reject |
| 18 | `BlockMartingale && MartingaleFlag && IsIncreasing` | `PauseTrader` | `MARTINGALE_BLOCK` | yes | Flag is caller-supplied |
| 19 | `BlockAbnormalSizing && AbnormalSizing && IsIncreasing` | `Reject` | `ABNORMAL_SIZING_BLOCK` | yes | Flag is caller-supplied |
| 20 | `IsReducing` fall-through | `Approve` | `RISK_REDUCTION` | reduce/close | Passthrough qty; no dest id |
| 21 | else (increasing fall-through) | `Approve` | `APPROVED` | open/increase | Echoes requested qty |

A23 §5 steps **missing** from this function: infrastructure/DB available; feature-flag as control flow (not an empty `if`); source collector health; trader `LIVE` state; `expires_at`; sizing normalize/step; persist `risk_decision`; concentration (correctly unused — Phase 2).

Unknown / default enum values (`(CopyIntentAction)99`, `(KillSwitchMode)99`) are neither increasing nor reducing → fall through to `APPROVED` if loss/DD pass.

`BrokerId` and `SourceLogin` are unused. Decisions are identity-blind.

---

## 4. Kill switch

### 4.1 Law (§40, A23 §8, A48)

Two **independent** controls. At any instant the system may be `{stop-new off|on} × {flatten idle|active}`. A mutually exclusive enum used as SoT is an A48 violation.

Reduce/close of mapped dest positions **may** still be approved **and sendable** while stop-new is on (default `allow_risk_reduction_while_stop_new=true`). Daily-loss / trader-loss / portfolio-DD engage **stop-new**; they must not freeze exits (A71 §7.2, example E8).

### 4.2 Measured

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
| Reduce/close still sendable under stop-new | `allowSend` requires `KillSwitch == None` | **FAIL** |
| Loss/DD engage stop-new only, not flatten | Returns `GlobalStop` and **also rejects reduce/close** (lines 117–124) | **FAIL** — freezes exits |
| Durable latch + audit + RBAC | Caller-supplied enum; no persist; no API mutation | **MISSING** |
| On the send path | No call sites | **DEAD** |

`GLOBAL_STOP` as an *outcome* is not a durable latch write. A23 §8.3: engagement of the kill-switch record is an audited state transition. Here the outcome is returned and forgotten.

Dashboard `GetRiskAsync` reports `(ks?.Mode ?? None).ToString()` and `RealCopyEnabled=false`. Seed (`DemoSeeder` L113–120) inserts `Mode=None`. A48 §3.3: missing row should treat stop-new as **ON**. Boot is fail-**open** for new copy (currently `SAFE_BY_ABSENCE` at send).

`docs/risk.md`: `STOP_NEW_EXECUTION` ≠ `EMERGENCY_FLATTEN`. The *labels* honor that. The *control* does not.

---

## 5. Stale quote / market-quality guards

### 5.1 Law (§31, §37, A23 §6.1, A72)

```text
quote_age = decision_time - quote_received_timestamp
if venue timestamp present: also reject if venue_quote_age > max
if quote missing OR quote session unhealthy: reject OPEN/INCREASE
if quote_age > configured_max_quote_age:     reject OPEN/INCREASE  (QUOTE_STALE)
if spread > max_allowed_spread:              reject OPEN/INCREASE  (SPREAD_TOO_WIDE)
```

Threshold **must be configurable and measured**. Logged-on ≠ fresh. Reduce/close use a **separate** (much looser) clock.

### 5.2 Measured

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
```

| Required behavior | Measured | Class |
|---|---|---|
| Reject increasing when `quote_age > max` | Yes; strict `>` so `age == MaxQuoteAge` is allowed | **PASS** on this predicate |
| Reason `QUOTE_STALE` | Yes | **PASS** |
| Missing quote ≡ reject open/increase | `QUOTE_MISSING` | **PASS** vocabulary-adjacent |
| Family split on stale/missing/spread/move/signal | All five checks are `&& IsIncreasing` | **PASS** on this file |
| Dual clock (`VenueTimestamp`) | Field exists; **never read** | **MISSING** |
| Quote usability (`bid<=0`, `ask<bid`, crossed) | Not checked | **MISSING** |
| Taker-touch signed adverse move | Unsigned `\|mid - expected\|` | **UNSAFE if shipped** (favorable gap rejects) |
| `MaxSlippage` | Property never read | **MISSING** |
| Configurable / measured | Compile default 3 s | **FAIL** vs §31 |
| One bound across the tree | `RiskLimits` 3 s vs `CTraderFixOptions.MaxQuoteAgeMs=5000` vs API settings `maxQuoteAgeSeconds=3` | **FAIL** — two silent defaults |
| Live + shadow send/fill path | `Evaluate` unused; `ShadowCopyEngine` records `QuoteAge` but does **not** reject stale | **DEAD / SHADOW BYPASS** |

Negative quote age (`ReceivedAt` in the future) is not rejected. `MaxSlippage` sitting unused next to a mid-price move check is not a slippage guard. A72: price-move ≠ slippage.

---

## 6. `REAL_COPY_EXECUTION_ENABLED` default false

### 6.1 Law (§41, A49)

`NewOrderSingle` requires the flag **true** *and* runtime risk-engine healthy. The flag is a deploy/config floor, distinct from kill switches. A dashboard button must not raise it above the config floor.

A48/A49 flatten exception (Phase 8): emergency flatten may send **reducing** NOS even if the copy flag is false. v1: flatten mutation is **not** shipped.

### 6.2 Measured

Empty branch — **no control flow**:

```90:93:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.RealExecutionEnabled == false && request.Action != CopyIntentAction.CloseExposure)
        {
            // Shadow path still evaluates risk but never allows FIX send.
        }
```

The comment is true of `AllowFixSend` for **all** actions, including `CloseExposure`. The `!= CloseExposure` condition is dead. A later coder can misread it as “closes bypass the flag.” They do not: `allowSend` still requires `RealExecutionEnabled`.

| Location | Default | Bound to engine? |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | `false` | **No** |
| `apps/api/appsettings.json` `CTrader:RealCopyExecutionEnabled` | `false` | **No** |
| `apps/api/Program.cs` `/api/settings` | hardcoded `false` | **No** |
| `EfDashboardQueries` overview / risk / FIX session | literal `false` | **No** |
| `apps/fix-worker/Worker.cs` `GetValue("CTrader:RealCopyExecutionEnabled", false)` | `false` | Logs only; still no NOS. Line 39: `Status = real ? LoggedOn : LoggedOn` (no-op ternary) |
| `RiskEngineTests.Base()` | `RealExecutionEnabled = false` | Yes, for the five unit facts |
| Env name `REAL_COPY_EXECUTION_ENABLED` | **Not bound** in product | Architecture name ≠ config key |

`docs/risk.md`: FIX send requires `REAL_COPY_EXECUTION_ENABLED=true` **and** a passing risk decision with `AllowFixSend`. Neither half is on a send path.

**REAL_COPY verdict:** default-false is honored wherever a default exists. The engine will not set `AllowFixSend` unless the caller lies and passes `true`. There is no product binder from env `REAL_COPY_EXECUTION_ENABLED` into `RiskEvaluationRequest`. Live copy is still **fail-closed by absence of NewOrderSingle**, not by this gate.

---

## 7. Reduce vs open

### 7.1 Law (§64, A23 §2, A71)

| Family | Classes | Stance |
|---|---|---|
| OPEN | `OPEN_EXPOSURE`, `INCREASE_EXPOSURE` | Strict. Fail closed on stale / unpriced / unmapped / unreconciled / stop-new. |
| CLOSE | `REDUCE_EXPOSURE`, `CLOSE_EXPOSURE` | Lenient on **market-quality** guards. Still fail closed on **identity** (known dest id, known qty, no double-close, no unknown-on-this-id). |

Opening more is optional. Residual dest risk after the source is gone is a defect.

### 7.2 Measured

```174:178:D:\Prop\src\Domain\Risk\RiskEngine.cs
    private static bool IsIncreasing(CopyIntentAction action) =>
        action is CopyIntentAction.OpenExposure or CopyIntentAction.IncreaseExposure;

    private static bool IsReducing(CopyIntentAction action) =>
        action is CopyIntentAction.ReduceExposure or CopyIntentAction.CloseExposure;
```

Guards **not** gated (wrong for CLOSE family):

```117:124:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.TraderRealizedLoss <= -_limits.MaxLossPerTrader)
            return Reject(request, RiskDecisionOutcome.PauseTrader, "MAX_LOSS_PER_TRADER");

        if (request.DailyExecutionPnl <= -_limits.MaxDailyExecutionLoss)
            return Reject(request, RiskDecisionOutcome.GlobalStop, "MAX_DAILY_EXECUTION_LOSS");

        if (request.PortfolioDrawdown >= _limits.MaxPortfolioDrawdown)
            return Reject(request, RiskDecisionOutcome.GlobalStop, "MAX_PORTFOLIO_DRAWDOWN");
```

A losing day therefore cannot close a copied XAU position through this engine. That is the opposite of §64 / A71 E8.

| Required CLOSE-family check (A71 §7.1) | Measured |
|---|---|
| Linked dest / shadow position remaining `> 0` | **MISSING** — no dest id on the request |
| Qty ≤ remaining; clip, never flip | **MISSING** — echoes `RequestedQuantity` |
| Same side as link | **MISSING** |
| Unknown state on **this** dest | **MISSING** (unreconciled is skipped for close) |
| Flatten already owns dest → coalesce | **MISSING** |
| Do not invent a price | N/A (no fill here) |

So the close branch is **too strict** on PnL/DD and `AllowFixSend`, and **too loose** on identity.

OPEN-family defects that also break the split:

| Item | Why it is wrong |
|---|---|
| `MAX_OPEN_POSITIONS` on all increasing | A23 / A71: **OPEN only**. An INCREASE of an existing dest is not a new slot. |
| `MAX_XAU_NET` → `ReduceSize` via `Reject()` | `ApprovedQuantity=0`. A23: `REDUCE_SIZE` must carry a positive stepped qty. |
| `MAX_XAU_GROSS` / max qty / margin | Hard `REJECT` only; no reduce-to-cap path. |
| Net check `abs(net) + requested` | Blind add; a short add that **reduces** net is treated as increasing net (A71 §8.1). |
| Single `MaxSourceSignalAge` | Two clocks required. |
| `CopyIntent` has `ExpiresAt` but no dest link | `ExpiresAt` is **not** on `RiskEvaluationRequest`. Engine recomputes `DecisionTime - SourceEventTime` and never calls `CopyIntentExpiry`. |

`QuantityNormalizer` is unused by `RiskEngine` (also asserted by `SourceDestinationQuantityConversionTests`).

---

## 8. `AllowFixSend` conjunction

Computed **after** all limits, **not** family-aware:

```text
allowSend ⇔ RealExecutionEnabled
         ∧ KillSwitch == None
         ∧ Reconciled
         ∧ VenueHealthy
```

| # | Real | KillSwitch | Recon | Venue | Action | Loss/DD | Code send | Spec source-close | Spec flatten CLOSE |
|---:|---|---|---|---|---|---|---|---|---|
| C1 | T | None | T | T | Close | ok | **true** | true if dest known | n/a |
| C2 | T | StopNew | T | T | Close | ok | **false** | **true** (default) | n/a |
| C3 | T | Flatten | T | T | Close | ok | **false** | coalesce or true | **true** |
| C4 | F | Flatten | T | T | Close | ok | **false** | false (v1) | **true** (A25) |
| C5 | T | None | T | T | Close | daily −2000 | n/a (rejected) | **true** | **true** |
| C6 | T | Flatten | T | T | Open | ok | n/a (`GlobalStop`) | n/a | block new — **PASS** |
| C7 | T | None | T | T | Open | ok | **true** | n/a | must stay **false** for flatten |
| C8 | F | None | T | T | Open | ok | **false** | n/a | n/a — shadow shape |

C1 is the **only** close cell that can set send true, and it has **no dest identity**. C2/C3/C4 are the cells flatten and stop-new exist for; all are **false**. C5 never reaches `allowSend`.

No unit fact sets `RealExecutionEnabled=true` and expects `AllowFixSend=true`. The send bit can be hard-wired `false` and every current fact still passes except none would catch it on the true path (there is no true path fact).

---

## 9. Request vs spec input gap

`RiskEvaluationRequest` fields the **spec** needs and the **request does not have**:

```text
linked_destination_position_id
dest_remaining_qty
dest_side
expires_at / per-intent max_signal_age
flatten_run_id / flatten_owner(this dest id) / flatten_phase
unknown_on_this_dest_id
TRADE logged on / lease owned          -- only a bool VenueHealthy
stop_new_execution  (independent bit)
allow_risk_reduction_while_stop_new
trader_state (LIVE / SHADOW / …)
canonical_symbol / side
database_available
source collector health
```

Without those, the engine **cannot** implement A48 flatten or A71 identity rules. Any close approval is an opinion about the caller’s snapshot, not about a venue position.

Every gate is a field the **caller** sets. There is no `IKillSwitchQuery` re-read. A buggy worker can pass `KillSwitch=None`, `Reconciled=true`, `VenueHealthy=true`, `RealExecutionEnabled=true`, `Action=CloseExposure`, `RequestedQuantity=99` and receive `AllowFixSend=true`. Risk approval is specified as **not** a capability token (A49 §5). Here it is the only token, and it is made of caller-supplied bits. **UNSAFE** once on a send path. **DEAD** today.

---

## 10. Wiring (why this is not yet a control)

| Layer | Finding | Class |
|---|---|---|
| DI | `DependencyInjection.AddTraderIntelligence` registers `TradeReconstructor`, `BaselineScorer`, `DealIngestionService` — **not** `RiskEngine` / `RiskLimits` | **MISSING** |
| Application | No `IRiskEngine`. Ingestion and `ReconstructionScoringService` never call `Evaluate` | **MISSING** |
| Persistence | `risk_decisions` table mapped; no code path writes `RiskDecisionRecord` from `Evaluate` | **DEAD** |
| Copy / execution | `CopyIntent` + `ExecutionIntent` exist; no orchestrator calls risk then persist-before-send | **MISSING** |
| FIX worker | 15 s heartbeat stub. Reads real-copy key only to log. When `true`, logs a warning and still sends nothing | **SAFE_BY_ABSENCE** |
| Shadow | `ShadowCopyEngine` does not call `RiskEngine`. It will simulate an entry on a 30 s quote | **SHADOW BYPASS** |
| Dashboard | Risk card shows kill **mode** string + `RealCopyEnabled=false` + last reject reasons from whatever rows were inserted elsewhere — not from this engine | **EXISTS_NEEDS_REFACTOR** |
| API | `GET /api/risk`, `GET /api/risk/status` read-only. No flatten POST, no kill mutation | **MISSING** (correct v1 omission for flatten) |

Safety today: **no NewOrderSingle**. That is the correct current outcome. It is not evidence that these four checks work.

---

## 11. Tests (recensus of C03; not re-litigated)

File: `D:\Prop\tests\Unit\RiskEngineTests.cs`. Five facts. Default `RiskLimits` only. Fixture is a single interior `OpenExposure` point.

| Fact | Proves | Does not prove |
|---|---|---|
| `Stale_quote_rejects_open` | 30 s receive-age → `QUOTE_STALE` / `Reject` / no send | Boundary, dual clock, reduce/close, Increase, config bind, qty 0 |
| `Real_flag_false_never_allows_fix_send` | Fixture default `false` → `Approve` + no send | Flag `true` path; worker refuse |
| `Stop_new_execution_blocks_opens_not_closes` | Open → `GlobalStop`; close → `Approve` + no send | Reason string; Increase; Reduce; flatten; dest book. **Locks B13-02 / D13-02 as expected** |
| `Unreconciled_venue_blocks_new_exposure` | Open + `Reconciled=false` → reason string only | Outcome, send, qty, Increase, close-of-known |
| `Stale_signal_rejected` | 5 min signal → reason string only | Outcome / send / qty; `expires_at`; Increase |

| Metric | Count |
|---|---|
| Engine reason strings | **21** |
| Reasons with any assert | **6** (3 unasserted as strings) |
| Reasons with full tuple assert | **1** (`QUOTE_STALE`) |
| Actions exercised | **2 / 4** (no Increase, no Reduce) |
| Kill modes exercised | **2 / 3** (no Flatten) |
| Outcomes exercised | **3 / 6** (no `ReduceSize` / `PauseTrader` / `PauseVenue`) |
| `AllowFixSend=true` facts | **0** |
| Custom `RiskLimits` facts | **0** |
| Integration `Evaluate` calls | **0** |
| A89 risk classes on disk (`RiskEngineHardLimitTests` etc.) | **0 / 10** |

A89 rows 50–59 mark ten named classes `EXISTS`. **None of those files are on disk.** Do not cite A89 as measured coverage.

---

## 12. Findings

Severity: **P0** = would be wrong on a live send path or contradicts a hard §; **P1** = incomplete vs spec; **P2** = hygiene.

B13 IDs are restated as D13 IDs so this file stands alone. Same defects; same hash.

| ID | Sev | Topic | Finding | Class |
|---|---|---|---|---|
| D13-01 | P0 | Reduce vs open | `MAX_LOSS_PER_TRADER` / `MAX_DAILY_EXECUTION_LOSS` / `MAX_PORTFOLIO_DRAWDOWN` apply to reduce/close. Red day cannot exit. Violates §64 / A71 E8. = B13-01 | **UNSAFE** |
| D13-02 | P0 | Kill switch | `AllowFixSend` requires `KillSwitch == None`. Approved `RISK_REDUCTION` cannot send under stop-new. Default A48 policy is the opposite. = B13-02. Unit test **pins** this. | **UNSAFE** |
| D13-03 | P0 | Kill switch | Exclusive `KillSwitchMode` cannot represent stop-new ON + flatten ACTIVE. A48: same violation as a single bool. Seed `None` is fail-open. = B13-03 | **UNSAFE** |
| D13-04 | P0 | Reduce vs open | `RiskDecisionOutcome.ReduceSize` for `MAX_XAU_NET` is produced by `Reject()` → `ApprovedQuantity=0`, `AllowFixSend=false`. Not a size reduction. = B13-04 | **UNSAFE** |
| D13-05 | P0 | Wiring | `Evaluate` has **zero** product call sites. Shadow fills ignore it. G12 cannot PASS. = B13-05 | **DEAD** |
| D13-06 | P0 | REAL_COPY | No env/config binder into the request. Worker/API defaults are independently `false`. Conjunction “flag + healthy risk + lease + recon” is not implemented. Safe only because NOS is absent. = B13-06 | **GATE_INCOMPLETE** |
| D13-07 | P1 | Kill switch | `EmergencyFlatten` is a second “block new” alias. No flatten run, no dest snapshot, no CLOSE intents, no RBAC. = B13-07 | **MISSING** flatten |
| D13-08 | P1 | Stale quote | `VenueTimestamp` unused; no `QUOTE_INVALID`; unsigned mid move; `MaxSlippage` unread. = B13-08 | **EXISTS_NEEDS_REFACTOR** |
| D13-09 | P1 | Stale quote | `MaxQuoteAge=3s` vs `MaxQuoteAgeMs=5000`. Two unbound lab numbers. §31 requires one measured config. = B13-09 | **EXISTS_NEEDS_REFACTOR** |
| D13-10 | P1 | Reduce vs open | Close/reduce always `APPROVE` `RequestedQuantity` if they reach the branch (including 0 / negative / fat-finger). No mapping, clip, unknown-state, or flatten coalesce. = B13-10 | **UNSAFE** once wired |
| D13-11 | P1 | Reduce vs open | `MAX_OPEN_POSITIONS` blocks `IncreaseExposure`. Spec: OPEN only. = B13-11 | **EXISTS_NEEDS_REFACTOR** |
| D13-12 | P1 | Tests | Five facts; A27/A89 named classes absent. Existing close test **locks in** `AllowFixSend=false` under stop-new. = B13-12 / C03 | **MISSING** suite |
| D13-13 | P2 | REAL_COPY | Empty `if` (lines 90–93) special-cases `CloseExposure` in the condition and does nothing. Misleading. = B13-13 | **EXISTS_NEEDS_REFACTOR** |
| D13-14 | P2 | Kill switch | Stop-new outcome is `GlobalStop` but no latch is written. Dashboard cannot distinguish engine-raised vs operator-raised. = B13-14 | **EXISTS_NEEDS_REFACTOR** |
| D13-15 | P1 | Net arithmetic | `abs(net) + requested` is side-blind. A long add against a short book that **reduces** \|net\| is treated as increasing it. A71 §8.1. | **UNSAFE** if wired |
| D13-16 | P2 | Identity-blind | `BrokerId` / `SourceLogin` unused. Unknown `CopyIntentAction` / `KillSwitchMode` fall through to `APPROVED`. | **EXISTS_NEEDS_REFACTOR** |
| D13-17 | P1 | Trust model | All gates are caller-supplied. No re-read of durable kill / recon / flag immediately before send (A25 / A48 §5.2). | **UNSAFE** if wired |

**Do not “fix” D13-02 without D13-10 identity/qty clip and D13-03 two-bit SoT.** That pairing is the live-account landmine (C33 F02+F06).

---

## 13. What is actually good (do not regress)

Keep these when the stub is replaced.

- Two reason codes, not one bool, for stop-new vs flatten-in-progress.
- `IsIncreasing` gating on quote / spread / mid-move / signal / recon / venue-health / book caps / martingale — close family is **not** failed by entry news guards. Matches A23 §8.2 / A71 G12–G16 **direction**.
- Flatten-in-progress does **not** auto-fire dest closes from `Evaluate`. Auto-flatten from a threshold is forbidden; the missing piece is a **separate** authorized executor.
- `Reject` always `AllowFixSend=false` and qty 0.
- `Approve` + `AllowFixSend=false` is the correct **shadow shape** when the flag is false (A23 §5 step 2) — once the empty `if` is deleted so it cannot be misread.
- API has **no** flatten POST in the first-useful host. Do not add one to “complete” this review.
- FIX worker still does not send. Do not add flatten `35=D` to prove a unit test.
- `docs/risk.md` one-pager still states the right law: scoring proposes; risk decides; `STOP_NEW` ≠ flatten; send = flag **and** `AllowFixSend`.

---

## 14. What a later increment must change (not this task)

Do **not** treat this list as an implementation order for D13. Product source stays untouched.

1. Split SoT: `stop_new` bit × flatten-run phase (A48). Stop using exclusive `KillSwitchMode` as the only persisted state.
2. Gate loss/DD/daily-loss to OPEN family (or have them raise the latch **and** still approve CLOSE of a mapped dest).
3. Compute `AllowFixSend` **per family**: stop-new must not clear send on CLOSE when `allow_risk_reduction_while_stop_new=true`. Flatten CLOSE is the A48 exception to `REAL_COPY=false`.
4. Implement `REDUCE_SIZE` as a positive stepped qty or fall through to `REJECT` / `SIZE_BELOW_MIN`. Never `ReduceSize` + qty 0.
5. Dual quote clock + signed taker-touch + bind **one** `max_quote_age_open` from config. Family clocks for signal/quote on close. Read `MaxSlippage` or delete it.
6. Require `linked_destination_position_id` + remaining qty on REDUCE/CLOSE; reject `NO_DESTINATION_POSITION` instead of approving air.
7. Register and call the engine from the copy orchestrator **before** any `execution_intent` / shadow fill. Persist `risk_decisions`. FIX worker re-checks `AllowFixSend` immediately before socket write.
8. Replace `RiskEngineTests` close assertion so it cannot greenwash D13-02. Add the A71 E1–E9 matrix. Label stub locks `CURRENT_STUB`.

---

## 15. Go-live boxes this file owns (still FAIL)

| Box | Law | Why FAIL |
|---|---|---|
| risk engine unit/integration tests pass | §68 / A100 G11 | 5 smoke facts; A89 classes absent |
| stale quote rejection works | §68 / A100 G12 | One interior Open fact; no feed, no send path, no dual clock |
| stale signal rejection works | §68 / A100 G13 | Reason-only fact; `expires_at` unused |
| kill switch tested | §68 / A100 G16 / §70.13 | One Open outcome + one Close that pins the send defect |
| risk-engine rejection happens before FIX send | §70.11 | No send function consults `AllowFixSend` |
| global stop-new-orders works | §68 | Outcome is a return value, not a latch |
| reconciliation blocks execution while inconsistent | §70.14 | Caller bool; not derived from a recon run |

---

## 16. Explicit non-claims

- Not “kill switch tested.”
- Not “stale quote rejection works” on a feed or send path.
- Not “REAL_COPY is an implemented NOS gate.”
- Not “§64 implemented.”
- Not “B13 fixed.” Same hash, same 14 defects.
- Not “≥95%” anything.
- No product source was modified. No test source was modified. No MQ5. No FIX send. No secrets.

---

## 17. Traceability

| Review question | Primary evidence | Law |
|---|---|---|
| Kill switch | `RiskEngine.cs` 78–82, 147–150; `KillSwitchMode.cs`; `KillSwitch.cs`; `DemoSeeder` 113–120; `RiskEngineTests` `Stop_new_execution_*` | §40, A23 §8, A48 |
| Stale quote | `RiskEngine.cs` 15, 95–110; `CTraderFixOptions.MaxQuoteAgeMs`; `RiskEngineTests` `Stale_quote_*` | §31, §37, A23 §6.1, A72 |
| REAL_COPY default false | Request field 44; allowSend 147; empty if 90–93; options/appsettings/worker/dashboard defaults; `Real_flag_false_*` | §41, A49 |
| Reduce vs open | `IsIncreasing` / `IsReducing` 174–178; un-gated 117–124; close branch 152–161; net `ReduceSize` 135–136 | §64, §72.18, A23 §2, A71 |
| Wiring | `DependencyInjection.cs` 37–42; `grep Evaluate(`; `Worker.cs`; `EfDashboardQueries.cs` 149–159 | §32, §70.11 |
| Tests | `RiskEngineTests.cs`; C03 counts still match this hash | A23 §11.3, A27, A89 #50–59 |
| Flatten/close adversarial | C33 S01–S20 — still valid against this hash | A48, A71 |

**Bottom line:** `RiskEngine` is a 189-line first-match stub with the right family *names* and the wrong loss/DD, send-bit, `ReduceSize`, and identity behavior. Residual destination risk cannot be exited through this engine on a red day, and cannot be sent under stop-new or flatten even on a green day. The only reason that is not a live incident is that **nobody calls `Evaluate` and nobody sends FIX.**
