# C03 — `RiskEngineTests` missing-case review

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\C03_risk_tests_review.md` |
| Agent | C03 (risk unit-test review) |
| Date | 2026-08-18 |
| Product source edited | **No** |
| Test source edited | **No** |
| SUT | `D:\Prop\src\Domain\Risk\RiskEngine.cs` (`RiskLimits`, `DestinationQuote`, `RiskEvaluationRequest`, `RiskDecision`, `RiskEngine.Evaluate`) |
| Tests read | `D:\Prop\tests\Unit\RiskEngineTests.cs` (5 `[Fact]`s, 86 lines) |
| Adjacent tests read | `D:\Prop\tests\Unit\ExecutionAndSizingTests.cs` (`Copy_intent_expires` only — not a `RiskEngine` call) |
| Law | Architecture §31, §37, §39–§41, §60, §63–§64, §68, §70.11–13; `docs\risk.md`; A23, A27 §4.5, A48, A49, A71, A72, A89 rows 50–59; B13 |
| Method | Read the test file and every `return` in `Evaluate`. Count reason codes, outcomes, actions, kill modes, and `AllowFixSend` conjunctions that a fact actually asserts. A89 “EXISTS” is **not** treated as coverage. |

---

## 0. Verdict

**FAIL / thin smoke, not a risk-limits suite.**

`RiskEngineTests` is five facts against a 22-branch engine. It proves a handful of *happy-path labels* on the default `OpenExposure` fixture and one `CloseExposure` under `StopNewExecution`. It does **not** prove:

- each hard-limit reason in isolation (A23 §11.3, A89 #50, §60 “risk limits”),
- `IncreaseExposure` or `ReduceExposure` at all,
- `EmergencyFlatten`,
- any `PauseTrader` / `PauseVenue` / `ReduceSize` path,
- `AllowFixSend=true` under the documented conjunction,
- any custom `RiskLimits` (so compile defaults are unpinned),
- family-split matrices A71 E1–E9,
- first-blocking-check order,
- boundaries (`>` vs `>=`).

Measured coverage of **engine reason strings**: **6 of 21 touched, 0 fully asserted** (`Outcome` + `Reason` + `ApprovedQuantity` + `AllowFixSend` + action).

A89 rows 50–59 mark ten named risk classes as `EXISTS`. **None of those files are on disk.** The only `RiskEngine` caller in `tests/` is this one class. Integration never calls `Evaluate`. Go-live boxes “risk engine unit/integration tests pass”, “stale quote rejection works”, “stale signal rejection works”, “kill switch tested” remain **unchecked**.

Do not treat 5 green facts as §39 / §64 / A23 implemented.

---

## 1. What exists (measured)

File: `D:\Prop\tests\Unit\RiskEngineTests.cs`.  
SUT constructed once: `new RiskEngine()` → **default** `RiskLimits` only.

Fixture `Base()` (lines 57–86):

```text
CopyIntentId        = "c1"
BrokerId            = "ACHIEVER"
SourceLogin         = 1
Action              = OpenExposure
RequestedQuantity   = 0.10
ExpectedPrice       = 2400
SourceEventTime     = now - 1s
DecisionTime        = 2026-08-18T12:00:00Z
Quote               = XAUUSD bid 2399.9 / ask 2400.1 / ReceivedAt = now-200ms
VenueHealthy        = true
RealExecutionEnabled= false
Reconciled          = true
KillSwitch          = None
TraderRealizedLoss  = 0
DailyExecutionPnl   = 0
PortfolioDrawdown   = 0
CurrentGrossXau     = 0
CurrentNetXau       = 0
OpenPositions       = 0
MarginUsage         = 0.1
MartingaleFlag      = false
AbnormalSizing      = false
```

That fixture is a **single interior point**. It never sits on a limit, never uses `Increase`/`Reduce`, never turns the real-copy flag on, never injects custom limits.

### 1.1 Fact-by-fact

| # | Fact | Request tweak | Asserts | Proves | Does not prove |
|---:|---|---|---|---|---|
| 1 | `Stale_quote_rejects_open` | `ReceivedAt = DecisionTime - 30s` | `Outcome=Reject`, `Reason=QUOTE_STALE`, `AllowFixSend=false` | Far-past receive-age on **Open** hits the stale branch | Boundary `age==3s` vs `>3s`; `Increase`; reduce/close exemption; `VenueTimestamp`; missing quote; custom `MaxQuoteAge`; `ApprovedQuantity=0`; CopyIntentId echo |
| 2 | `Real_flag_false_never_allows_fix_send` | none | `Outcome=Approve`, `AllowFixSend=false` | Default fixture + flag false → shadow-shaped approve | `Reason=APPROVED`; `ApprovedQuantity=0.10`; flag **true** conjunction; close/reduce with flag false; worker NOS refuse; empty `if` on engine lines 90–93 |
| 3 | `Stop_new_execution_blocks_opens_not_closes` | Open + `StopNewExecution`; Close + `StopNewExecution` | Open `Outcome=GlobalStop` (no reason); Close `Approve` + `AllowFixSend=false` | Stop-new blocks Open **outcome**; Close is not `GlobalStop` | Reason `STOP_NEW_EXECUTION`; Increase; Reduce; flatten distinctness; dest book unchanged; **spec** send-under-stop-new (`AllowFixSend` should be family-aware). **Locks current send defect as expected.** |
| 4 | `Unreconciled_venue_blocks_new_exposure` | `Reconciled=false` | `Reason=VENUE_NOT_RECONCILED` **only** | Open + unreconciliation emits that string | `Outcome=Reject`; `AllowFixSend=false`; qty 0; Increase; Close of known dest still `RISK_REDUCTION`; unknown-on-this-id |
| 5 | `Stale_signal_rejected` | `SourceEventTime = DecisionTime - 5min` | `Reason=SIGNAL_STALE` **only** | 5 min ≫ 15 s on Open emits that string | Outcome / send / qty; `age==15s` vs `>15s`; Increase; Close clock; `expires_at`; 20-intent catch-up; `CopyIntentExpiry` unused by engine |

### 1.2 Assertion quality

| Fact | Outcome | Reason | ApprovedQuantity | AllowFixSend | CopyIntentId | Action coverage |
|---|---|---|---|---|---|---|
| 1 | yes | yes | **no** | yes | **no** | Open only |
| 2 | yes | **no** | **no** | yes | **no** | Open only |
| 3 open | yes | **no** | **no** | **no** | **no** | Open only |
| 3 close | yes | **no** (`RISK_REDUCTION` unasserted) | **no** | yes (false) | **no** | Close only |
| 4 | **no** | yes | **no** | **no** | **no** | Open only |
| 5 | **no** | yes | **no** | **no** | **no** | Open only |

Facts 4 and 5 would stay green if the engine started returning `Approve` + `VENUE_NOT_RECONCILED` / `SIGNAL_STALE`, or `GlobalStop` with those reasons. That is not a hard-limit lock.

### 1.3 Adjacent file (do not double-count)

`ExecutionAndSizingTests.Copy_intent_expires` checks `CopyIntentExpiry.IsExpired` at 16s vs 5s against a 15s span. `RiskEngine` **does not call** `CopyIntentExpiry`. That fact is not `SIGNAL_STALE` coverage.

No integration test references `RiskEngine` / `Evaluate`.

---

## 2. Engine surface vs tests

`Evaluate` is a first-blocking-check chain. Every `return` is a required unit case (A23 §5, A89 #50).

### 2.1 Reason / outcome matrix

| Order | Predicate (current code) | Outcome | Reason | Increasing-only? | Tested? |
|---:|---|---|---|---|---|
| 1 | `KillSwitch==StopNewExecution && IsIncreasing` | `GlobalStop` | `STOP_NEW_EXECUTION` | yes | **Partial** — Open outcome only; Increase missing; reason unasserted |
| 2 | `KillSwitch==EmergencyFlatten && IsIncreasing` | `GlobalStop` | `EMERGENCY_FLATTEN_BLOCKS_NEW` | yes | **Missing** |
| 3 | `!Reconciled && IsIncreasing` | `Reject` | `VENUE_NOT_RECONCILED` | yes | **Partial** — reason only, Open only |
| 4 | `!VenueHealthy && IsIncreasing` | `PauseVenue` | `VENUE_UNHEALTHY` | yes | **Missing** |
| — | `RealExecutionEnabled==false && Action!=CloseExposure` | *(empty body)* | — | n/a | **Missing** (dead `if`; comment can be misread as close-bypass) |
| 5 | `Quote is null && IsIncreasing` | `Reject` | `QUOTE_MISSING` | yes | **Missing** |
| 6 | `DecisionTime-ReceivedAt > MaxQuoteAge && IsIncreasing` | `Reject` | `QUOTE_STALE` | yes | **Partial** — 30s Open only |
| 7 | `Ask-Bid > MaxAllowedSpread && IsIncreasing` | `Reject` | `SPREAD_TOO_WIDE` | yes | **Missing** |
| 8 | `\|mid-ExpectedPrice\| > MaxPriceMove && IsIncreasing` | `Reject` | `PRICE_MOVED_TOO_FAR` | yes | **Missing** |
| 9 | `DecisionTime-SourceEventTime > MaxSourceSignalAge && IsIncreasing` | `Reject` | `SIGNAL_STALE` | yes | **Partial** — 5min Open, reason only |
| 10 | `TraderRealizedLoss <= -MaxLossPerTrader` | `PauseTrader` | `MAX_LOSS_PER_TRADER` | **no — all actions** | **Missing** |
| 11 | `DailyExecutionPnl <= -MaxDailyExecutionLoss` | `GlobalStop` | `MAX_DAILY_EXECUTION_LOSS` | **no — all actions** | **Missing** |
| 12 | `PortfolioDrawdown >= MaxPortfolioDrawdown` | `GlobalStop` | `MAX_PORTFOLIO_DRAWDOWN` | **no — all actions** | **Missing** |
| 13 | `OpenPositions >= MaxOpenPositions && IsIncreasing` | `Reject` | `MAX_OPEN_POSITIONS` | yes | **Missing** |
| 14 | `RequestedQuantity > MaxPositionQuantity && IsIncreasing` | `Reject` | `MAX_POSITION_QUANTITY` | yes | **Missing** |
| 15 | `CurrentGrossXau + RequestedQuantity > MaxXauGross && IsIncreasing` | `Reject` | `MAX_XAU_GROSS` | yes | **Missing** |
| 16 | `\|CurrentNetXau\| + RequestedQuantity > MaxXauNet && IsIncreasing` | `ReduceSize` | `MAX_XAU_NET` | yes | **Missing** (only `ReduceSize` path in the file) |
| 17 | `MarginUsage > MaxMarginUsage && IsIncreasing` | `Reject` | `MAX_MARGIN_USAGE` | yes | **Missing** |
| 18 | `BlockMartingale && MartingaleFlag && IsIncreasing` | `PauseTrader` | `MARTINGALE_BLOCK` | yes | **Missing** |
| 19 | `BlockAbnormalSizing && AbnormalSizing && IsIncreasing` | `Reject` | `ABNORMAL_SIZING_BLOCK` | yes | **Missing** |
| 20 | `IsReducing` fall-through | `Approve` | `RISK_REDUCTION` | reduce/close | **Partial** — Close under stop-new only; Reduce never; reason unasserted |
| 21 | else (increasing fall-through) | `Approve` | `APPROVED` | open/increase | **Partial** — Open + flag false; Increase never; reason unasserted |

`Reject(...)` always sets `ApprovedQuantity=0` and `AllowFixSend=false`. **No test asserts that contract** except fact 1’s send bit.

`MaxSlippage` is a `RiskLimits` property and is **never read**. No test documents the dead field.

`DestinationQuote.VenueTimestamp` is never read. No dual-clock test (A23 §6.1, A72).

### 2.2 Enum / action coverage

| Dimension | Values | Touched |
|---|---|---|
| `CopyIntentAction` | Open, Increase, Reduce, Close | Open, Close. **Increase=0. Reduce=0.** |
| `KillSwitchMode` | None, StopNewExecution, EmergencyFlatten | None (implicit), StopNew. **Flatten=0.** |
| `RiskDecisionOutcome` | Approve, ReduceSize, Reject, PauseTrader, PauseVenue, GlobalStop | Approve, Reject, GlobalStop. **ReduceSize=0. PauseTrader=0. PauseVenue=0.** |
| `RealExecutionEnabled` | false / true | false only |
| `RiskEngine(RiskLimits)` | default / custom | default only |
| `AllowFixSend` | false / true | false only |

### 2.3 `AllowFixSend` conjunction (engine 147–150)

```text
allowSend = RealExecutionEnabled
         && KillSwitch == None
         && Reconciled
         && VenueHealthy
```

| Case | Expected (current code) | Tested? |
|---|---|---|
| All four true, Open | `Approve` + `AllowFixSend=true` + `APPROVED` | **Missing** — highest-value happy path |
| All four true, Close | `Approve` + `AllowFixSend=true` + `RISK_REDUCTION` | **Missing** |
| Flag false, rest true | `Approve` + `AllowFixSend=false` | Fact 2 (Open only) |
| Flag true, `StopNewExecution`, Close | `Approve` + `AllowFixSend=false` (current); spec A48 default **true** | Fact 3 encodes **current**, not spec |
| Flag true, `!Reconciled`, Close | skips recon reject; `AllowFixSend=false` | **Missing** |
| Flag true, `!VenueHealthy`, Close | skips venue reject; `AllowFixSend=false` | **Missing** |
| Flag true, `EmergencyFlatten`, Close | `Approve` + `AllowFixSend=false` | **Missing** |

Without a fact that sets `RealExecutionEnabled=true` and expects `AllowFixSend=true`, the send bit can be hard-wired `false` and only fact 2 / fact 3 close still pass.

---

## 3. Missing cases (the list)

Split so a later coder does not mix **“pin the stub”** with **“prove the spec.”** Pinning current defects without a `CURRENT_STUB` label will greenwash B13-01/02/04.

### 3.1 P0 — every hard-limit reason in isolation (A23 §11.3, A89 #50)

One theory member (or fact) per reason, default Open fixture, **one** field over the default limit. Assert `Outcome`, `Reason`, `ApprovedQuantity=0` (or residual qty on net — see 3.6), `AllowFixSend=false`, `CopyIntentId="c1"`.

| ID | Missing case | Suggested tweak vs `Base()` | Current outcome / reason |
|---|---|---|---|
| M01 | Missing quote | `Quote=null` | `Reject` / `QUOTE_MISSING` |
| M02 | Venue unhealthy | `VenueHealthy=false` | `PauseVenue` / `VENUE_UNHEALTHY` |
| M03 | Emergency flatten blocks new | `KillSwitch=EmergencyFlatten` | `GlobalStop` / `EMERGENCY_FLATTEN_BLOCKS_NEW` |
| M04 | Stop-new **reason** | already have outcome; add `Reason=STOP_NEW_EXECUTION` | `GlobalStop` / `STOP_NEW_EXECUTION` |
| M05 | Wide spread | `Ask=Bid+2.01` (default cap 2.0) | `Reject` / `SPREAD_TOO_WIDE` |
| M06 | Price moved | `ExpectedPrice=2400`, mid still 2400 → set bid/ask so mid=2403.1, or `ExpectedPrice=2396.9` | `Reject` / `PRICE_MOVED_TOO_FAR` |
| M07 | Trader max loss | `TraderRealizedLoss=-500` | `PauseTrader` / `MAX_LOSS_PER_TRADER` |
| M08 | Daily execution loss | `DailyExecutionPnl=-2000` | `GlobalStop` / `MAX_DAILY_EXECUTION_LOSS` |
| M09 | Portfolio drawdown | `PortfolioDrawdown=3000` | `GlobalStop` / `MAX_PORTFOLIO_DRAWDOWN` |
| M10 | Max open positions | `OpenPositions=20` | `Reject` / `MAX_OPEN_POSITIONS` |
| M11 | Max position qty | `RequestedQuantity=5.01` | `Reject` / `MAX_POSITION_QUANTITY` |
| M12 | Max XAU gross | `CurrentGrossXau=19.91` + qty 0.10 | `Reject` / `MAX_XAU_GROSS` |
| M13 | Max XAU net | `CurrentNetXau=9.91` + qty 0.10 | `ReduceSize` / `MAX_XAU_NET` (**qty 0 today**) |
| M14 | Max margin | `MarginUsage=0.71` | `Reject` / `MAX_MARGIN_USAGE` |
| M15 | Martingale | `MartingaleFlag=true` | `PauseTrader` / `MARTINGALE_BLOCK` |
| M16 | Abnormal sizing | `AbnormalSizing=true` | `Reject` / `ABNORMAL_SIZING_BLOCK` |

**15 of 19 reject/pause reasons have zero facts.** `VENUE_NOT_RECONCILED` / `QUOTE_STALE` / `SIGNAL_STALE` / `STOP_NEW` exist but are incomplete (see §1.2).

### 3.2 P0 — action family matrix (A71 E1–E4, A27 `OpenVsCloseExposurePolicyTests`)

`IncreaseExposure` and `ReduceExposure` are **never** passed to `Evaluate`. §64 is unproven.

For **each** market-quality / venue / kill guard, same snapshot × four actions:

| Guard | Open | Increase | Reduce | Close |
|---|---|---|---|---|
| `QUOTE_STALE` (30s) | Reject (fact 1) | **missing** (must Reject) | **missing** (stub: Approve `RISK_REDUCTION`) | **missing** (stub: Approve) |
| `QUOTE_MISSING` | **missing** | **missing** | **missing** | **missing** |
| `SPREAD_TOO_WIDE` | **missing** | **missing** | **missing** | **missing** |
| `PRICE_MOVED_TOO_FAR` | **missing** | **missing** | **missing** | **missing** |
| `SIGNAL_STALE` (5min) | reason only | **missing** | **missing** | **missing** |
| `StopNewExecution` | outcome only | **missing** | **missing** | Approve + send=false (fact 3) |
| `EmergencyFlatten` | **missing** | **missing** | **missing** | **missing** |
| `!Reconciled` | reason only | **missing** | **missing** | **missing** |
| `!VenueHealthy` | **missing** | **missing** | **missing** | **missing** |
| `MartingaleFlag` | **missing** | **missing** (highest-value INCREASE case) | **missing** | **missing** |
| `OpenPositions>=20` | **missing** | **missing** (spec: INCREASE should **not** consume a slot — A23 §6.4 / A71 G24) | n/a | n/a |

Minimum first methods (A89 §6 + A71 §15):

- `Stale_quote_rejects_open_and_increase_not_reduce_or_close`
- `Increase_uses_same_kill_and_recon_set_as_open`
- `Reduce_is_risk_reduction_not_a_close_alias` (distinct `Action`, same `RISK_REDUCTION` reason)

### 3.3 P0 — loss / DD must not freeze exits (A71 E8/E9, B13-01)

Engine lines 117–124 apply to **every** action. Spec: these engage stop-new; they **must not** reject mapped CLOSE.

| ID | Case | Stub today | Spec (A71 §7.2) | Tested? |
|---|---|---|---|---|
| M17 | Close + `TraderRealizedLoss=-500` | `PauseTrader` / `MAX_LOSS_PER_TRADER` | Approve `RISK_REDUCTION` | **Missing** |
| M18 | Close + `DailyExecutionPnl=-2000` | `GlobalStop` / `MAX_DAILY_EXECUTION_LOSS` | Approve | **Missing** |
| M19 | Close + `PortfolioDrawdown=3000` | `GlobalStop` / `MAX_PORTFOLIO_DRAWDOWN` | Approve | **Missing** |
| M20 | Open + same three | Pause/GlobalStop | Reject / GlobalStop | **Missing** (E9) |

**Do not** add M17–M19 as “must equal stub” without labeling them `CURRENT_STUB`. Shipping those as the contract freezes dest risk on a red day.

### 3.4 P0 — `AllowFixSend=true` happy path (A23 §4, A49, A89 #51/#58)

| ID | Case | Missing assertion |
|---|---|---|
| M21 | Open + `RealExecutionEnabled=true` + `KillSwitch=None` + `Reconciled` + `VenueHealthy` | `Outcome=Approve`, `Reason=APPROVED`, `ApprovedQuantity=0.10`, `AllowFixSend=true` |
| M22 | Close + same conjunction | `Reason=RISK_REDUCTION`, `AllowFixSend=true` |
| M23 | Each conjunction factor independently false | `AllowFixSend=false` (flag / kill / recon / venue) |
| M24 | `CTraderFixOptions.RealCopyExecutionEnabled` default false | Not this class’s SUT today; A89 #58 / #72. Engine request bit is **unbound** from options — a contract test belongs next to options, not only here. |

Fact 2 is necessary and **not sufficient**.

### 3.5 P0 — stop-new vs flatten distinctness (A48, A89 #56/#57)

| ID | Case | Missing |
|---|---|---|
| M25 | Flatten does **not** fire when only `StopNewExecution` | Open+StopNew reason is `STOP_NEW_EXECUTION`, not `EMERGENCY_FLATTEN_*` |
| M26 | Stop-new does **not** fire when only `EmergencyFlatten` | Open+Flatten reason is `EMERGENCY_FLATTEN_BLOCKS_NEW` |
| M27 | Close under flatten still `Approve` / `RISK_REDUCTION` (engine: yes) | Unasserted |
| M28 | Exclusive enum cannot represent stop-new ON **and** flatten ACTIVE | Design hole (B13-03). No test can pass both bits today; document the SoT defect rather than inventing a third mode. |
| M29 | Stop-new does not mutate dest book | Engine is pure — assert it does not require a book input. Weak but honest. |

Fact 3 **must be rewritten** before it is treated as “kill switch tested”: assert `Reason`, Increase, Reduce, and split `CURRENT_STUB` (`AllowFixSend=false` because `KillSwitch!=None`) from `SPEC` (A48 `allow_risk_reduction_while_stop_new=true` → send allowed on CLOSE).

### 3.6 P0 — `ReduceSize` is not `Reject` (A23 §4.1, A89 #59, B13-04, A71 G9)

| ID | Case | Stub | Spec | Tested? |
|---|---|---|---|---|
| M30 | Net cap breach | `ReduceSize`, `ApprovedQuantity=0`, `AllowFixSend=false` | Positive stepped residual (`maxNet - |net|`) or `REJECT`/`SIZE_BELOW_MIN` if residual < min | **Missing** |
| M31 | Gross / qty / margin never emit `ReduceSize` | Hard `Reject` + qty 0 | May reduce-to-cap | **Missing** (document current) |

A fact that only checks `Outcome=ReduceSize` without qty would **greenwash** qty-0.

### 3.7 P1 — boundaries (strict operators)

All comparisons are `>` or `<=` / `>=` as written. Interior-only fixtures hide off-by-one.

| ID | Limit | Operator | Must-approve | Must-block | Tested? |
|---|---|---|---|---|---|
| B01 | Quote age | `age > 3s` | `age==3.000s` | `age==3s+1 tick` | **Missing** |
| B02 | Signal age | `age > 15s` | `==15s` | `15s+1 tick` | **Missing** |
| B03 | Spread | `spread > 2.0` | `==2.0` | `2.00 + 0.01` | **Missing** |
| B04 | Price move | `\|mid-exp\| > 3.0` | `==3.0` | `3.01` | **Missing** |
| B05 | Trader loss | `loss <= -500` | `-499.99` | `-500` | **Missing** |
| B06 | Daily PnL | `pnl <= -2000` | `-1999.99` | `-2000` | **Missing** |
| B07 | Portfolio DD | `dd >= 3000` | `2999.99` | `3000` | **Missing** |
| B08 | Open positions | `n >= 20` | `19` | `20` | **Missing** |
| B09 | Position qty | `qty > 5` | `5.00` | `5.01` | **Missing** |
| B10 | Gross | `gross+qty > 20` | `==20` | `20.01` | **Missing** |
| B11 | Net | `\|net\|+qty > 10` | `==10` | `10.01` | **Missing** |
| B12 | Margin | `usage > 0.70` | `==0.70` | `0.70 + epsilon` | **Missing** |

Fact 1 uses 30s (10× the cap). Fact 5 uses 5 minutes. Neither pins the operator.

Also missing:

- `ReceivedAt == DecisionTime` (age 0) → Open approve.
- `ReceivedAt` in the **future** (negative age) → stub allows; spec should reject clock skew. Unasserted.
- Crossed book `Ask < Bid` (negative spread) → stub does **not** emit `SPREAD_TOO_WIDE`. Unasserted (`QUOTE_INVALID` does not exist).
- `Bid<=0` / unusable quote (A72 §3.2) — unasserted.

### 3.8 P1 — first-blocking-check order (A23 §5)

If two predicates are true, the **earlier** `return` wins. No order facts exist.

| ID | Dual fault | Must win (current order) |
|---|---|---|
| O01 | Stop-new + stale quote | `STOP_NEW_EXECUTION` |
| O02 | Flatten + unreconciled | `EMERGENCY_FLATTEN_BLOCKS_NEW` |
| O03 | Unreconciled + unhealthy | `VENUE_NOT_RECONCILED` |
| O04 | Unhealthy + missing quote | `VENUE_UNHEALTHY` |
| O05 | Stale quote + wide spread + stale signal | `QUOTE_STALE` |
| O06 | Wide spread + price move | `SPREAD_TOO_WIDE` |
| O07 | Stale signal + trader max loss | `SIGNAL_STALE` (signal is earlier) |
| O08 | Trader loss + daily loss + DD | `MAX_LOSS_PER_TRADER` |
| O09 | Daily loss + DD | `MAX_DAILY_EXECUTION_LOSS` |
| O10 | Max positions + max qty + gross + net | `MAX_OPEN_POSITIONS` |
| O11 | Gross + net | `MAX_XAU_GROSS` (gross before net — net `ReduceSize` never reached) |
| O12 | Margin + martingale + abnormal | `MAX_MARGIN_USAGE` |
| O13 | Martingale + abnormal | `MARTINGALE_BLOCK` |

O11 is load-bearing: a book that breaches **both** net and gross will never produce `ReduceSize`. Unasserted.

### 3.9 P1 — custom `RiskLimits` (A23 §6: thresholds are configuration)

`new RiskEngine(limits)` is unused. Defaults can change and every fact still passes.

| ID | Case |
|---|---|
| C01 | `MaxQuoteAge=1s`, quote age 2s → `QUOTE_STALE`; same quote with default 3s → approve |
| C02 | `MaxAllowedSpread=0.05`, fixture spread 0.2 → `SPREAD_TOO_WIDE` |
| C03 | `MaxSourceSignalAge=2s`, signal age 3s → `SIGNAL_STALE` |
| C04 | `BlockMartingale=false` + flag true → **not** `MARTINGALE_BLOCK` |
| C05 | `BlockAbnormalSizing=false` + flag true → **not** `ABNORMAL_SIZING_BLOCK` |
| C06 | `MaxXauNetExposure=0.05` + qty 0.10 → `ReduceSize` |
| C07 | Assert **default** property values (500 / 2000 / 3000 / 20 / 10 / 5 / 20 / 2.0 / 3s / 15s / 3.0 / 1.5 / 0.70 / both blocks true) so a silent default edit is a test fail |

`MaxSlippage=1.5` should have a fact that **today it is ignored** (`CURRENT_STUB`) and a pending spec fact `MAX_SLIPPAGE_EXCEEDED` (A23 §6.3) marked not-implemented.

### 3.10 P1 — net / gross arithmetic (A71 §8.1)

Engine uses `abs(net) + requested` and `gross + requested` with **no side**.

| ID | Case | Stub | Spec |
|---|---|---|---|
| N01 | `CurrentNetXau=-8` (short), Open qty 0.10 (unsigned) | `8+0.10=8.10` < 10 → Approve | Signed: a **long** add **reduces** \|net\|; a **short** add increases it |
| N02 | `CurrentNetXau=9.95`, qty 0.10 | `ReduceSize` qty 0 | Residual 0.05 or below-min reject |
| N03 | `CurrentGrossXau=19.95`, qty 0.10 | `MAX_XAU_GROSS` | same |
| N04 | Negative `RequestedQuantity` | not `> 5`; gross may **decrease**; net `abs+(-x)` can pass | Should reject `INTENT_INCOMPLETE` / `QTY_BELOW_MIN` |
| N05 | `RequestedQuantity=0` | Approve 0 / `APPROVED` | Should reject below min |

No side / dest-link / remaining-qty fields exist on `RiskEvaluationRequest`. Tests cannot yet express A71 E6 `NO_DESTINATION_POSITION`. Record that as **input-gap**, not a skipped fact.

### 3.11 P1 — quote / mid / unsigned move (A72)

| ID | Case | Stub | Spec |
|---|---|---|---|
| Q01 | Favorable gap: expected 2400, mid 2396 (buy improved) | `PRICE_MOVED_TOO_FAR` (unsigned) | Must **not** reject favorable |
| Q02 | Adverse taker touch: buy vs **ask**, sell vs **bid** | Uses mid | Signed adverse vs touch |
| Q03 | `VenueTimestamp` older than `MaxQuoteAge` while `ReceivedAt` fresh | Approve | Dual clock: reject `QUOTE_STALE` |
| Q04 | Fresh receive, venue clock null | Age from receive only | Allowed |
| Q05 | Symbol `EURUSD` on quote | Ignored | Fail closed / not XAU |
| Q06 | Spread exactly at cap vs 1 tick over | see B03 | — |

### 3.12 P1 — evaluation-order vs A23 checklist that has **no request field**

These are missing from **both** engine and tests. Do not write passing facts that pretend they exist.

| Spec check (A23 §5 / §4.3) | On `RiskEvaluationRequest`? | Test |
|---|---|---|
| Database available | no | cannot unit-test here |
| `expires_at` / `INTENT_EXPIRED` | no | missing (CopyIntentExpiry is a different type) |
| Per-intent `max_signal_age` (stricter wins) | no | missing |
| TRADE vs QUOTE session split | single `VenueHealthy` | missing |
| `SOURCE_STALE` / collector health | no | missing |
| Trader state `LIVE` / `TRADER_NOT_LIVE` | no | missing |
| `EXECUTION_STATE_UNKNOWN` | no | missing |
| Sizing normalize / `SIZE_BELOW_MIN` / step | uses raw `RequestedQuantity` | covered elsewhere (`QuantityNormalizer`), **not** re-checked inside `Evaluate` |
| Persist `risk_decision` | return DTO only | no unit fact that a store is written (none is) |
| Scoring / ML cannot bypass | no score field | A89 #40 `ScoringCannotBypassRiskContractTests` **not on disk** |
| Concentration `CONCENTRATION_CAP` | Phase 2 — must stay unused | missing **negative** fact: no such reason is ever returned |

### 3.13 P2 — hygiene / dead paths

| ID | Case |
|---|---|
| H01 | `BrokerId` / `SourceLogin` unused — decisions do not depend on them (identity-blind). Pin or flag. |
| H02 | Empty `if` (engine 90–93): Open/Increase/Reduce/Close with flag false still evaluate; Close is **not** exempt from later checks. Need an explicit no-op fact so a later coder cannot “implement” the comment as a bypass. |
| H03 | `Reject` helper: every blocking path `ApprovedQuantity=0` + `AllowFixSend=false` — one theory over all reason codes. |
| H04 | Approve paths echo `RequestedQuantity` unchanged (no step). |
| H05 | `CopyIntentId` echoed on approve and reject. |
| H06 | Unknown / default enum values (`(CopyIntentAction)99`, `(KillSwitchMode)99`) — neither increasing nor reducing → fall through to `APPROVED`. Unasserted foot-gun. |
| H07 | `UnitTest1` still in the project — ignore; do not count. |

### 3.14 Spec-owned cases that cannot be expressed until the request grows

From A71 §15 / A23 §11.3. Listed so C03 is not read as “we have a test plan for these”:

- No dest link + source close → `NO_DESTINATION_POSITION` (E6)
- Scale-in without dest → `OPEN_NEVER_ACCEPTED` (E7)
- Reversal CLOSE then OPEN; OPEN reject leaves dest flat (E5)
- Flatten owns dest → coalesce, no second NOS (E10)
- 3-minute FIX gap, 20 stale opens expire, existing dest closes process (E11)
- CLOSE qty = dest remaining, not source lots (E4 / §8.2)
- Shadow unpriced close holds (E15)
- High score cannot pass a stale OPEN (E14) — needs a scoring+risk contract class
- Zero FIX outbound on reject — needs a worker/integration harness

Those belong in future `OpenVsCloseExposurePolicyTests` / integration / FIX harness, not as green facts against today’s DTO.

---

## 4. A89 / A27 phantom inventory

A89 §5.4 marks these `EXISTS`. **Measured on disk 2026-08-18: absent.**

| A89 # | Claimed class | Disk | What `RiskEngineTests` actually covers of its “must prove” |
|---:|---|---|---|
| 50 | `RiskEngineHardLimitTests` | **No** | 3 of ~19 reasons, none isolated with full asserts |
| 51 | `RiskEngineApproveReduceRejectTests` | **No** | Approve+Reject+GlobalStop seen; ReduceSize/PauseTrader/PauseVenue never; send-true never |
| 52 | `OpenVsCloseExposurePolicyTests` | **No** | One Close under stop-new |
| 53 | `QuoteFreshnessGuardTests` | **No** | One 30s Open; no `==max` boundary; no missing quote |
| 54 | `PriceMoveAndSpreadGuardTests` | **No** | **Zero** |
| 55 | `StaleCopyIntentExpiryTests` | **No** (expiry helper tested elsewhere) | One 5min Open reason |
| 56 | `KillSwitchStopNewExecutionTests` | **No** | One Open outcome + one Close approve |
| 57 | `KillSwitchEmergencyFlattenTests` | **No** | **Zero** |
| 58 | `RealExecutionFeatureFlagTests` | **No** | Flag false → no send; flag true untested; options unbound |
| 59 | `RiskEngineNetExposureReduceSizeTests` | **No** | **Zero** |

A27 §4.5 names the same cluster under `tests/Unit/Risk/`. Folder **does not exist**. File lives at `tests/Unit/RiskEngineTests.cs`.

**Do not cite A89 status `EXISTS` as evidence these cases are locked.**

---

## 5. Defects the current suite would hide

If a later change “keeps tests green”:

| Change | Facts that stay green | Why that is dangerous |
|---|---|---|
| Hard-wire `AllowFixSend=false` | 1, 2, 3, 4, 5 | Live conjunction never asserted |
| Drop `Reason` on recon / signal (keep any reason / empty) | 3 (no reason check); 4/5 fail only if string changes | Weak |
| Treat Increase as reducing | All facts | Increase never sent |
| Treat Reduce as increasing | All facts | Reduce never sent |
| Delete flatten branch | All facts | Flatten never sent |
| Return `ReduceSize` with qty 0 forever | All facts | G9 untested |
| Apply quote/spread/signal to Close | Fact 3 still passes (Close is not stale in that fact) | Family split unproven on market-quality guards |
| Apply loss/DD to Close (already true) | All facts | B13-01 ships unchallenged |
| Change `MaxQuoteAge` to 60s | Fact 1 still rejects at 30s? **No** — 30s > 60s is false, fact 1 **would** fail. Defaults are only pinned where a fact exceeds them by a wide margin. Changing 3s → 10s still keeps fact 1 green. | Threshold not pinned |

Fact 3 close `AllowFixSend=false` is the **worst lock-in**: implementing A48 “closes may send under stop-new” **breaks** the only Close test.

---

## 6. Recommended replacement shape (not implemented)

This section is a contract for a later increment. C03 does not add tests.

Keep one file or split to the A89 names; do not collapse into a single smoke fact.

```text
RiskEngineTests                          # keep as facade or delete once split
  RiskEngineHardLimitTests               # [Theory] one member per reason (M01–M16)
  RiskEngineApproveReduceRejectTests     # M21–M23, H03–H05, outcomes vocabulary
  OpenVsCloseExposurePolicyTests         # §3.2 matrix + M17–M20 (SPEC vs STUB labeled)
  QuoteFreshnessGuardTests               # B01, M01, Q03–Q04, C01
  PriceMoveAndSpreadGuardTests           # M05–M06, B03–B04, Q01–Q02, C02
  KillSwitchStopNewExecutionTests        # rewrite fact 3; M04, M25, Increase+Reduce
  KillSwitchEmergencyFlattenTests        # M03, M26–M27
  RealExecutionFeatureFlagTests          # fact 2 + M21–M24
  RiskEngineNetExposureReduceSizeTests   # M13, M30–M31, N01–N03
  RiskEngineEvaluationOrderTests         # O01–O13
  RiskLimitsOverrideTests                # C01–C07
```

Assertion helper (suggested, not written):

```text
AssertDecision(d, outcome, reason, qty, allowSend, copyIntentId: "c1")
```

Use it on **every** fact so Reason-only tests cannot return.

Label facts that document stub defects:

```text
[Fact(Skip) or trait "CURRENT_STUB"] Close_on_daily_loss_is_rejected_by_stub
[Fact] Close_on_daily_loss_must_approve_once_B13_01_fixed   // pending
```

Do **not** skip the SPEC fact silently. A `Skip` with B13-01 in the reason is honest; a green stub-only fact is not.

---

## 7. Counts (honest)

| Metric | Count |
|---|---|
| `[Fact]` in `RiskEngineTests` | **5** |
| `[Theory]` | **0** |
| Engine reason strings | **21** |
| Reasons with any assert | **6** (`QUOTE_STALE`, `VENUE_NOT_RECONCILED`, `SIGNAL_STALE`, implied `APPROVED`/`STOP_NEW`/`RISK_REDUCTION` — last three **unasserted as strings**) |
| Reasons with full tuple assert | **1** (`QUOTE_STALE`) |
| Actions exercised | **2 / 4** |
| Kill modes exercised | **2 / 3** |
| Outcomes exercised | **3 / 6** |
| `AllowFixSend=true` facts | **0** |
| Custom `RiskLimits` facts | **0** |
| `IncreaseExposure` facts | **0** |
| `ReduceExposure` facts | **0** |
| `EmergencyFlatten` facts | **0** |
| `ReduceSize` facts | **0** |
| `PauseTrader` / `PauseVenue` facts | **0** |
| Integration `Evaluate` calls | **0** |
| A89 risk classes on disk | **0 / 10** |
| Product `Evaluate` call sites | **0** (dead path; B13-05) |
| Product source changed by C03 | **No** |

Approximate completeness vs A23 §11.3 unit list: **well under 20%** of required *cases*, even if every existing fact is counted at full credit (they are not).

---

## 8. Cross-links

| Topic | Evidence | Law |
|---|---|---|
| Tests as they stand | `D:\Prop\tests\Unit\RiskEngineTests.cs` | this file |
| Engine branches | `D:\Prop\src\Domain\Risk\RiskEngine.cs` 76–171 | A23 §5 |
| Engine review (behavior, not tests) | `B13_risk_review.md` | do not duplicate B13; C03 is the **test gap** |
| Required classes | A27 §4.5, A89 #50–59 | §60 “risk limits” |
| Family policy / E1–E15 | A71 §13–§15 | §64 |
| Quote / spread / move | A72 | §31, §37 |
| Kill split | A48 | §40 |
| One-pager | `D:\Prop\docs\risk.md` | “hard rejects include …”; FIX send = flag **and** `AllowFixSend` |

---

## 9. Explicit non-claims

- Not “risk engine unit tests pass” as a go-live box. Five facts pass; the **suite required by §60 / §68 is absent**.
- Not “stale quote rejection works” beyond one interior Open case.
- Not “stale signal rejection works” beyond a Reason string.
- Not “kill switch tested.”
- Not “§64 Open vs Close policy tested.”
- Not “REAL_COPY gated.” Flag-false is touched; flag-true is not.
- Not a claim that A89 `EXISTS` rows are implemented.
- No product source modified. No test source modified. No MQ5. No FIX send. No secrets.

---

## 10. Disposition

`RiskEngineTests` is a **5-fact smoke file** over a **21-reason** first-match engine. Missing cases are not polish: the untested branches include every book cap, both behavioral blocks, flatten, venue-health, missing quote, spread, price-move, `ReduceSize`, both unused actions, the only `AllowFixSend=true` path, and the A71 red-day exit matrix.

**Next increment (not this task):** replace Reason-only facts with a full-tuple helper; add the isolation theory (M01–M16); add the 4-action family matrix; add M21 send-true; rewrite fact 3 so it cannot greenwash B13-02; add CURRENT_STUB vs SPEC pairs for B13-01/04. Do not implement those tests in C03.
