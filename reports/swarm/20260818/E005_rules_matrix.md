# E005 — Architecture risk / copy rules → RiskEngine + tests

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\E005_rules_matrix.md` |
| Agent | E005 (rules matrix) |
| Date | 2026-08-18 |
| Assigned | Map architecture risk/copy rules to `RiskEngine` + tests. Write this file. **Do not modify product source.** |
| Product source edited | **No** |
| Test source edited | **No** |
| Law | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §4, §8, §31–§45, §53, §57–§65, §68, §70, §72.7–19, §75 |
| Binding siblings (spec, not re-reviewed) | A23, A24, A27, A43, A48, A49, A53, A60, A71, A72, A89 #50–59; B13/D13 engine; C03/D35 tests; B36 fixtures (design only); C33 flatten adversarial |
| SUT | `D:\Prop\src\Domain\Risk\RiskEngine.cs` |
| SUT SHA-256 | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` |
| SUT size | 8567 bytes / **189** lines / LastWriteUtc 2026-08-18 07:38:10 |
| Tests | `D:\Prop\tests\Unit\RiskEngineTests.cs` |
| Tests SHA-256 | `7B95236489E2FE169BFC8E9F57A9E2C89F6F5F047116D9DC82CFA8820FB2DF51` |
| Tests size | 2909 bytes / **87** lines / LastWriteUtc 2026-08-18 07:47:42 |
| Method | Enumerate every architecture risk/copy obligation, then map it to a measured `Evaluate` branch (or absence) and a measured test. Line numbers from the current files. A89 `EXISTS` is **not** coverage. Nothing answered from memory. |

This file is a **mapping**. It does not replace B13/D13 (engine review) or C03/D35 (test review). Use those for defect narratives. Use this for “does rule *N* exist in the engine, and does a test lock it?”

---

## 0. Verdict

`RiskEngine.Evaluate` is a **21-reason first-match stub** with the right §64 family *names* and an incomplete / sometimes contradictory rule set. Live copy is **SAFE_BY_ABSENCE** (no `NewOrderSingle`, zero product callers of `Evaluate`). That is **not** implementation of the architecture rules.

| Metric | Count |
|---|---:|
| Architecture risk/copy rules inventoried (R001–R110) | **110** |
| Rule implemented in `Evaluate` and matches law | **18** |
| Predicate exists, incomplete vs law (`PARTIAL`) | **22** |
| Predicate exists, **contradicts** law if wired (`STUB_WRONG`) | **11** |
| Not on request / not in `Evaluate` (`MISSING`) | **41** |
| Correctly unused / Phase 2 (`DEFERRED`) | **3** |
| Adjacent SUT only (FSM / expiry helper / worker / flag default) (`ADJACENT`) | **15** |
| Engine reason strings | **21** |
| Reasons with any unit assert | **3** (`QUOTE_STALE`, `VENUE_NOT_RECONCILED`, `SIGNAL_STALE`) |
| Reasons with Outcome+Reason+AllowFixSend | **1** (`QUOTE_STALE`) |
| `RiskEngineTests` facts | **5** (smoke; SHA above) |
| A89 #50–59 named classes on disk | **0 / 10** (A89 `EXISTS` is phantom) |
| Product `Evaluate(` callers | **0** |
| Integration / FIX-harness `Evaluate` | **0** |
| `AllowFixSend=true` facts | **0** |
| `IncreaseExposure` / `ReduceExposure` facts | **0** |
| `EmergencyFlatten` facts | **0** |

**Do not claim** §31 / §37 / §39 / §40 / §41 / §63 / §64 / §68 / §70.11–14 are implemented. A method nobody calls is not a control.

Go-live boxes this matrix owns remain **unchecked**:

```text
[ ] position sizing conversion is verified
[ ] risk engine unit/integration tests pass
[ ] stale quote rejection works
[ ] stale signal rejection works
[ ] kill switch tested
[ ] risk-engine rejection happens before FIX send
[ ] global stop-new-orders works
[ ] reconciliation blocks execution while inconsistent
```

---

## 1. Legend

### 1.1 Engine class

| Class | Meaning |
|---|---|
| `MATCH` | `Evaluate` implements the law on this predicate (still **DEAD** until wired). |
| `PARTIAL` | Branch or field exists; missing clock / family / config bind / identity. |
| `STUB_WRONG` | Branch exists and would **violate** the law on a live send path. |
| `MISSING` | No request field and no branch. |
| `DEAD` | Code exists; **zero** product callers. Implied for every `Evaluate` rule. |
| `DEFERRED` | Architecture says Phase 2 / do not implement now. Correct absence. |
| `ADJACENT` | Copy-path rule lives in another type (FSM, expiry helper, worker, options). Not `Evaluate`. |
| `SAFE_BY_ABSENCE` | Outcome is safe only because NOS / orchestrator is absent. |

Every `Evaluate` row is also `DEAD` (B13-05 / D13-05). The Engine column does not repeat `DEAD` on every line.

### 1.2 Test class

| Class | Meaning |
|---|---|
| `FULL` | Outcome + Reason + ApprovedQuantity + AllowFixSend asserted. |
| `PARTIAL` | Some of those fields. |
| `REASON_ONLY` | String only. |
| `LOCKS_STUB` | Green fact encodes a **defect** as expected. |
| `ADJACENT` | Different SUT; does not call `Evaluate`. |
| `ABSENT` | Required; not on disk. |
| `PHANTOM` | A89/A27 name marked EXISTS; file missing. |
| `N/A` | Deferred / not a unit of this engine. |

### 1.3 Family

| Token | `CopyIntentAction` | `IsIncreasing` | `IsReducing` |
|---|---|---|---|
| `OPEN_EXPOSURE` | `OpenExposure` | yes | no |
| `INCREASE_EXPOSURE` | `IncreaseExposure` | yes | no |
| `REDUCE_EXPOSURE` | `ReduceExposure` | no | yes |
| `CLOSE_EXPOSURE` | `CloseExposure` | no | yes |

Unknown enum values are **neither**. They fall through to `APPROVED` if loss/DD pass (D13-16).

---

## 2. Engine branch index (every `return`)

Source: `RiskEngine.Evaluate` lines 76–171. Fail-closed on first blocking check **is** implemented. The *set* and *family* of checks is the gap.

| # | Lines | Predicate | Outcome | Reason | Family | Law | Engine | Test |
|---:|---|---|---|---|---|---|---|---|
| B01 | 78–79 | `KillSwitch==StopNewExecution && IsIncreasing` | `GlobalStop` | `STOP_NEW_EXECUTION` | open/inc | §40 | `MATCH` block-new | `PARTIAL` Open outcome only (`Stop_new_execution_*`); reason **unasserted**; Increase **ABSENT** |
| B02 | 81–82 | `KillSwitch==EmergencyFlatten && IsIncreasing` | `GlobalStop` | `EMERGENCY_FLATTEN_BLOCKS_NEW` | open/inc | §40 | `PARTIAL` (block-new only; no flatten run) | `ABSENT` |
| B03 | 84–85 | `!Reconciled && IsIncreasing` | `Reject` | `VENUE_NOT_RECONCILED` | open/inc | §42, §70.14 | `PARTIAL` (caller bool, not a recon run) | `REASON_ONLY` Open (`Unreconciled_venue_*`) |
| B04 | 87–88 | `!VenueHealthy && IsIncreasing` | `PauseVenue` | `VENUE_UNHEALTHY` | open/inc | §62 QUOTE/TRADE | `PARTIAL` (one bool; no QUOTE vs TRADE) | `ABSENT` |
| B— | 90–93 | `RealExecutionEnabled==false && Action!=CloseExposure` | *(empty)* | — | n/a | §41 | `STUB_WRONG` dead `if`; comment can be misread as close-bypass | `ABSENT` |
| B05 | 95–96 | `Quote is null && IsIncreasing` | `Reject` | `QUOTE_MISSING` | open/inc | §31 | `PARTIAL` (A23 also `QUOTE_UNAVAILABLE`) | `ABSENT` |
| B06 | 100–102 | `DecisionTime-ReceivedAt > MaxQuoteAge && IsIncreasing` | `Reject` | `QUOTE_STALE` | open/inc | §31, §37 | `PARTIAL` (receive clock only; 3 s compile default) | `PARTIAL` 30 s Open (`Stale_quote_rejects_open`) |
| B07 | 104–106 | `Ask-Bid > MaxAllowedSpread && IsIncreasing` | `Reject` | `SPREAD_TOO_WIDE` | open/inc | §37 | `PARTIAL` (no crossed-book / usability) | `ABSENT` |
| B08 | 108–110 | `\|mid-ExpectedPrice\| > MaxPriceMove && IsIncreasing` | `Reject` | `PRICE_MOVED_TOO_FAR` | open/inc | §37 | `STUB_WRONG` unsigned mid, not signed taker-touch | `ABSENT` |
| B09 | 113–115 | `DecisionTime-SourceEventTime > MaxSourceSignalAge && IsIncreasing` | `Reject` | `SIGNAL_STALE` | open/inc | §36, §63 | `PARTIAL` (no `expires_at`; unused `CopyIntentExpiry`) | `REASON_ONLY` 5 min Open (`Stale_signal_rejected`) |
| B10 | 117–118 | `TraderRealizedLoss <= -MaxLossPerTrader` | `PauseTrader` | `MAX_LOSS_PER_TRADER` | **all** | §39, §64 | `STUB_WRONG` freezes CLOSE | `ABSENT` |
| B11 | 120–121 | `DailyExecutionPnl <= -MaxDailyExecutionLoss` | `GlobalStop` | `MAX_DAILY_EXECUTION_LOSS` | **all** | §39, §64 | `STUB_WRONG` freezes CLOSE | `ABSENT` |
| B12 | 123–124 | `PortfolioDrawdown >= MaxPortfolioDrawdown` | `GlobalStop` | `MAX_PORTFOLIO_DRAWDOWN` | **all** | §39, §64 | `STUB_WRONG` freezes CLOSE | `ABSENT` |
| B13 | 126–127 | `OpenPositions >= MaxOpenPositions && IsIncreasing` | `Reject` | `MAX_OPEN_POSITIONS` | open/**inc** | §39, A23 §6.4 | `STUB_WRONG` Increase is not a new slot | `ABSENT` |
| B14 | 129–130 | `RequestedQuantity > MaxPositionQuantity && IsIncreasing` | `Reject` | `MAX_POSITION_QUANTITY` | open/inc | §39 | `PARTIAL` hard reject; no reduce-to-cap | `ABSENT` |
| B15 | 132–133 | `CurrentGrossXau + RequestedQuantity > MaxXauGross && IsIncreasing` | `Reject` | `MAX_XAU_GROSS` | open/inc | §39 | `PARTIAL` side-blind add; no reduce-to-cap | `ABSENT` |
| B16 | 135–136 | `\|CurrentNetXau\| + RequestedQuantity > MaxXauNet && IsIncreasing` | `ReduceSize` | `MAX_XAU_NET` | open/inc | §39, A23 §4.1 | `STUB_WRONG` via `Reject()` → qty **0** | `ABSENT` |
| B17 | 138–139 | `MarginUsage > MaxMarginUsage && IsIncreasing` | `Reject` | `MAX_MARGIN_USAGE` | open/inc | §39 | `PARTIAL` hard reject | `ABSENT` |
| B18 | 141–142 | `BlockMartingale && MartingaleFlag && IsIncreasing` | `PauseTrader` | `MARTINGALE_BLOCK` | open/inc | §39 | `PARTIAL` caller flag; not computed | `ABSENT` |
| B19 | 144–145 | `BlockAbnormalSizing && AbnormalSizing && IsIncreasing` | `Reject` | `ABNORMAL_SIZING_BLOCK` | open/inc | §39 | `PARTIAL` caller flag | `ABSENT` |
| B20 | 152–161 | `IsReducing` fall-through | `Approve` | `RISK_REDUCTION` | red/close | §64 | `STUB_WRONG` no dest id; `AllowFixSend` needs `KillSwitch==None` | `LOCKS_STUB` Close under StopNew, send=false |
| B21 | 164–171 | else (increasing fall-through) | `Approve` | `APPROVED` | open/inc | §39 | `PARTIAL` echoes requested qty | `PARTIAL` Open + flag false (`Real_flag_false_*`); reason unasserted |

`Reject(...)` (180–188): always `ApprovedQuantity=0`, `AllowFixSend=false`. **No fact asserts that contract** except B06’s send bit.

`allowSend` (147–150), applied only on B20/B21:

```text
allowSend ⇔ RealExecutionEnabled ∧ KillSwitch == None ∧ Reconciled ∧ VenueHealthy
```

This conjunction is **not family-aware**. It is the root of B20 `LOCKS_STUB`.

Unread: `RiskLimits.MaxSlippage`, `DestinationQuote.VenueTimestamp`, `BrokerId`, `SourceLogin`.

---

## 3. Architecture rules matrix

Rule ids are **this file’s**. Architecture section is law. Sibling specs (A23/A48/A71/A72) interpret; they do not outrank the `.md` v2 text.

### 3.1 Pipeline, authority, persist-before-send

| ID | Law | Rule | Engine | Test | Notes |
|---|---|---|---|---|---|
| R001 | §4, §32, §75 | Flow: source event → copy candidate → persist `CopyIntent` → RiskEngine → persist `ApprovedExecutionIntent` → FIX worker → `NewOrderSingle` | `MISSING` orchestrator; `Evaluate` is a pure function | `ABSENT` | No Application port `IRiskEngine`. DI (`DependencyInjection.cs` 36–42) registers reconstructor/scorer/ingestion — **not** `RiskEngine`. |
| R002 | §32 | Never send FIX from an MT5 event callback | `ADJACENT` / `SAFE_BY_ABSENCE` | `ABSENT` as a send-path test | No NOS anywhere. Not proven by `RiskEngineTests`. |
| R003 | §39 | Risk engine is the **final authority** | `PARTIAL` vocabulary only | `ABSENT` (`ScoringCannotBypassRiskTests` **PHANTOM**) | Scorer cannot be shown to be bypassed: nothing asks risk after scoring. |
| R004 | §39, §72.15 | Scoring/ML emit only `candidate` / `confidence` / `suggested allocation`. Never send, never size dest qty, never override reject | `MISSING` (no score fields on request; good) | `ABSENT` | Correct *absence* of ML-in-hot-path. Not a proof ML cannot bypass. |
| R005 | §33, §72.7–8 | Persist execution intent + unique `cl_ord_id` **before** socket write | `MISSING` | `ADJACENT` `ClOrdId_is_deterministic_*` in `ExecutionAndSizingTests` | Factory exists; not called from risk. |
| R006 | §33, §34, §72.9 | Never blindly retry `NewOrderSingle` after TCP break; first reconcile | `ADJACENT` | `ADJACENT` `Unknown_ack_cannot_retry_new_order` | FSM helper. Not a risk rule. Zero product callers (D98). |
| R007 | §34 | Disconnect after send → `EXECUTION_STATE_UNKNOWN`; no second order to “fix” | `MISSING` on request | `ADJACENT` `Disconnect_after_send_is_unknown_state` | Engine has only `Reconciled` bool. |
| R008 | §70.11 | Risk rejection happens **before** FIX send (builder never runs) | `SAFE_BY_ABSENCE` | `ABSENT` (`Harness.RiskRejectionBeforeFixSendTests` **PHANTOM**) | `AllowFixSend=false` is a DTO bit. Nobody consults it. |
| R009 | §44, §45, A23 §4 | Every evaluation persists `risk_decisions` / `risk_events` with `risk_decision_id` | `MISSING` | `ABSENT` | `RiskDecisionRecord` entity exists; nothing maps `Evaluate` → row. Name collision: entity `RiskDecision` vs record `Risk.RiskDecision`. |
| R010 | A23 §4.4, §36, §58 | Emit `risk_latency`, quote/signal age, spread, qty in/out, primary_reason | `MISSING` | `ABSENT` | Return DTO has Outcome/Reason/qty/send only. |

### 3.2 Feature flag (`REAL_COPY_EXECUTION_ENABLED`)

| ID | Law | Rule | Engine | Test | Notes |
|---|---|---|---|---|---|
| R011 | §41 | Default `REAL_COPY_EXECUTION_ENABLED=false` | `ADJACENT` `MATCH` on options/appsettings/dashboard literals | `PARTIAL` fixture default false (`Real_flag_false_*`) | `CTraderFixOptions.RealCopyExecutionEnabled=false`. Worker `GetValue(..., false)`. Program `/api/settings` hardcodes `false`. **Not bound** into `RiskLimits` / request. |
| R012 | §41, §70.12 | `NewOrderSingle` requires flag **true** *and* runtime risk-engine healthy | `PARTIAL` `allowSend` includes the bit; no TRADE logon / lease / persist-before-send | `ABSENT` flag-true + send-true | Empty `if` 90–93 is not control flow. |
| R013 | §41 | Connect / quote / request positions **without** placing new real orders | `ADJACENT` | `ABSENT` | Worker still refuses NOS even if config true (logs a warning). |
| R014 | A48 / A49 | Flatten CLOSE may send reducing NOS while flag is false (Phase 8). v1: flatten **not** shipped | `MISSING` (and correctly not shipped) | `N/A` v1 | Do not add flatten NOS to “complete” a unit test. |
| R015 | A25 / A49 | Dashboard must not raise the flag above the deploy/config floor | `ADJACENT` | `ABSENT` | Settings PUT writes Redis keys that **nothing reads** into `Evaluate`. |

### 3.3 Exposure classes and copy mapping

| ID | Law | Rule | Engine | Test | Notes |
|---|---|---|---|---|---|
| R016 | §64, §72.18 | Four classes: `OPEN_EXPOSURE` / `INCREASE_EXPOSURE` / `REDUCE_EXPOSURE` / `CLOSE_EXPOSURE` | `MATCH` enum + `IsIncreasing` / `IsReducing` (174–178) | `PARTIAL` Open+Close only | `CopyIntentAction` EXISTS_AND_GOOD as labels. |
| R017 | §64, §63 last line | Stricter on open/increase than reduce/close | `PARTIAL` market-quality/kill/recon gated; **loss/DD not** | `LOCKS_STUB` only Close under StopNew | Core §64 duty unproven as a 4-action matrix. |
| R018 | §36, §37, A71 | Quote age / spread / price-move / signal age apply to **entries**, not mapped closes | `MATCH` on those five checks (`&& IsIncreasing`) | `ABSENT` same-snapshot 4-action | Fact 1 is Open-only; Close is never given a stale quote. |
| R019 | §35 | Reversal = CLOSE then OPEN. Never one source event = one leftover dest | `MISSING` | `ABSENT` | Classifier not in engine (correct). No classifier type at all. |
| R020 | §35, A74 | Explicit `source trade → dest orders → dest position id(s)` | `MISSING` no dest id on request | `ABSENT` | Close of air `APPROVE`s `RequestedQuantity` (D13-10). |
| R021 | §38, §64, A71 §8.2 | REDUCE/CLOSE qty from **mapped dest remaining**, not source lots × allocation | `STUB_WRONG` echoes `RequestedQuantity` | `ABSENT` (`ReduceCloseSizingUsesMappedDestinationTests` **PHANTOM**) | `QuantityNormalizer` unused by engine (skipped fact in conversion tests). |
| R022 | A71 §3.3 | `MAX_OPEN_POSITIONS` rejects **OPEN** only; INCREASE of a linked dest is not a new slot | `STUB_WRONG` B13 | `ABSENT` | |
| R023 | A71 §3.3 | INCREASE **requires** a live dest link; OPEN **forbids** one | `MISSING` | `ABSENT` | |
| R024 | A71 §7.1 | CLOSE family still fail-closed on identity (known dest, qty ≤ remaining, no double-close, no unknown-on-this-id) | `MISSING` / `STUB_WRONG` always approve if loss/DD pass | `ABSENT` | |
| R025 | A23 §2 | Do not assume one source event equals one dest order forever | `ADJACENT` (orchestrator) | `ABSENT` | |

### 3.4 Kill switches

| ID | Law | Rule | Engine | Test | Notes |
|---|---|---|---|---|---|
| R026 | §40 | `STOP_NEW_EXECUTION` prevents new copy (`OPEN`/`INCREASE`) | `MATCH` B01 | `PARTIAL` Open outcome only | Reason string unasserted. Increase untested. |
| R027 | §40 | `STOP_NEW_EXECUTION` leaves existing dest positions **untouched** | `MATCH` by omission (engine mutates nothing) | `ABSENT` (weak to unit-test a pure function) | Not a flatten. |
| R028 | §40 | `EMERGENCY_FLATTEN` is separately permissioned and **closes** dest positions | `PARTIAL` B02 blocks new only | `ABSENT` | No flatten run, snapshot, CLOSE intents, RBAC. |
| R029 | §40 | Do **not** conflate the two controls | `STUB_WRONG` exclusive `KillSwitchMode` cannot be both ON | `ABSENT` | A48: exclusive enum = same violation as one bool. |
| R030 | §40, A48 | System may be `{stop-new off\|on} × {flatten idle\|active}` | `STUB_WRONG` | `ABSENT` | Persisted `KillSwitch.Mode` is the same exclusive enum. Seed `None` is fail-**open** for new copy (SAFE_BY_ABSENCE at send). |
| R031 | §39, A23 §8.3 | Engine-raised `GLOBAL_STOP` engages **stop-new**, not flatten | `PARTIAL` outcome `GlobalStop` is a return value, **no latch** | `ABSENT` | Daily-loss / DD return `GlobalStop` and also reject CLOSE (R039). |
| R032 | A48 default | Reduce/close of **mapped** dest **may send** while stop-new is on | `STUB_WRONG` `allowSend` requires `KillSwitch==None` | `LOCKS_STUB` fact 3 Close send=false | Implementing the law **breaks** the only Close test. |
| R033 | §59, §72.19 | `activate stop-new-orders` = RiskManager or SuperAdmin; flatten = stronger confirmation; **all audited** | `MISSING` | `ABSENT` | API has **no** kill/flatten POST (correct v1 omission for flatten). No inbound auth (D53). |
| R034 | §53 | Risk dashboard shows **both** stop-new **state** and flatten **availability** | `ADJACENT` | `ABSENT` | Dashboard reports `(ks?.Mode ?? None).ToString()` + `RealCopyEnabled=false`. |
| R035 | §68, §70.13 | “Kill switch tested” + “global stop-new-orders works” | `DEAD` | **FAIL** as a gate | One Open outcome + one Close that pins R032. |

### 3.5 Quote, spread, price-move, slippage

| ID | Law | Rule | Engine | Test | Notes |
|---|---|---|---|---|---|
| R036 | §31 | Maintain latest dest quote: bid/ask, receive ts, venue ts, symbol id | `PARTIAL` DTO has the fields | `ABSENT` feed | `DestinationQuoteSnapshot` entity exists. FIX worker stamps session `LastInboundAt` every 15 s — **not** a quote. Name collision: `Risk.DestinationQuote` vs entity snapshot. |
| R037 | §31 | `if quote_age > configured_max_quote_age: reject new copy` | `PARTIAL` B06; compile default **3 s** | `PARTIAL` 30 s Open | Strict `>` so `age==MaxQuoteAge` would approve. Unpinned. |
| R038 | §31 last sentence | Threshold **configurable and measured** | `STUB_WRONG` / `PARTIAL` hardcoded `RiskLimits` | `ABSENT` custom `RiskLimits` | `CTraderFixOptions.MaxQuoteAgeMs=5000` **unbound**. API settings `maxQuoteAgeSeconds=3` unbound. |
| R039 | A23 §6.1 | If venue ts present, reject if **either** receive-age or venue-age exceeds max | `MISSING` `VenueTimestamp` never read | `ABSENT` | |
| R040 | §31, A23 | Missing quote or quote session unhealthy → reject OPEN/INCREASE (`QUOTE_UNAVAILABLE` / `QUOTE_MISSING`) | `PARTIAL` B05 `QUOTE_MISSING`; unhealthy is B04 `VENUE_UNHEALTHY` | `ABSENT` missing-quote | |
| R041 | §37 | Reject `SPREAD_TOO_WIDE` when `ask-bid > max_allowed_spread` | `PARTIAL` B07 | `ABSENT` | No `QUOTE_INVALID` for crossed/`bid<=0`. |
| R042 | §37 | Reject `PRICE_MOVED_TOO_FAR` from expected vs current dest quote vs source | `STUB_WRONG` unsigned `\|mid-expected\|` | `ABSENT` | Favorable gap rejects. Spec: signed adverse vs **taker touch** (ask buy / bid sell). |
| R043 | §37, A23 §6.3 | `MAX_SLIPPAGE_EXCEEDED` independent of quote age | `MISSING` `MaxSlippage=1.5` **never read** | `ABSENT` | Price-move ≠ slippage (A72). |
| R044 | A72 | Logged-on ≠ fresh. Do not substitute QUOTE session health for `quote_age` | `PARTIAL` two separate fields | `ABSENT` | Caller can pass `VenueHealthy=true` with a 30 s quote; B06 still rejects (good) **if called**. |
| R045 | §24, A23 §6.1, A24 | Shadow uses the **same** freshness rules | `STUB_WRONG` / `ADJACENT` | `ABSENT` | `ShadowCopyEngine` records `QuoteAge` and **does not reject**. Bypass. |
| R046 | A72 | Quote usability: `bid<=0`, `ask<bid`, unmapped symbol | `MISSING` | `ABSENT` | EURUSD quote is accepted. |
| R047 | §68 | “Stale quote rejection works” | `DEAD` | **FAIL** as a gate | One interior Open fact. No feed. No send path. No dual clock. No Increase. No reduce exemption fact. |

### 3.6 Signal age, expiry, no blind catch-up

| ID | Law | Rule | Engine | Test | Notes |
|---|---|---|---|---|---|
| R048 | §36 | Each signal carries `source_event_time`, `collector_receive_time`, `decision_time`, later `fix_send_time` / `execution_time` | `PARTIAL` source + decision only | `ABSENT` | No collector receive. No latency chain persist. |
| R049 | §36, §72.17 | Reject entries that become too stale (configurable) | `PARTIAL` B09 `MaxSourceSignalAge=15s` | `REASON_ONLY` 5 min Open | Increase untested. Close exemption untested. |
| R050 | §63 | Every `CopyIntent` has `expires_at` **and** `max_signal_age` | `MISSING` on request | `ADJACENT` `Copy_intent_expires` | Entity `CopyIntent.ExpiresAt` exists. Engine **never reads it**. `CopyIntentExpiry.IsExpired` is a single-span helper, unused by `Evaluate`. |
| R051 | A23 §6.2 | Per-intent `max_signal_age` vs global: **stricter wins** | `MISSING` | `ABSENT` | |
| R052 | A23 §6.2 | `now >= expires_at` → `INTENT_EXPIRED` | `MISSING` | `ABSENT` | |
| R053 | §63 | TRADE down 3 minutes + 20 source opens → **do not** fire 20 NOS on reconnect. Expired die. Only still-fresh OPEN/INCREASE re-eval against a live quote | `MISSING` | `ABSENT` (`StaleCopyIntentExpiryTests` catch-up **PHANTOM**) | Highest-value copy-safety rule. Zero tests. |
| R054 | §63 last line | Closing/reducing may have **separate** expiry policy | `PARTIAL` B09 is increasing-only | `ABSENT` | One helper, one age, all classes (`CopyIntentExpiry`). |
| R055 | §68 | “Stale signal rejection works” | `DEAD` | **FAIL** as a gate | Reason-only, 20× the cap. |

### 3.7 Hard limits (§39)

All numeric thresholds are **configuration**, not production constants (§23, §31, A23 §6). `RiskLimits` ships **lab numbers**. No product binder.

| ID | Law | Limit / action | Default | Engine | Test |
|---|---|---|---|---|---|
| R056 | §39 | max loss per selected trader → pause/reject | `500` | `STUB_WRONG` B10 all actions; `<= -cap` | `ABSENT` |
| R057 | §39 | max daily execution-account loss → reject + optional `GLOBAL_STOP` | `2_000` | `STUB_WRONG` B11 all actions | `ABSENT` |
| R058 | §39 | max portfolio drawdown → reject + optional `GLOBAL_STOP` | `3_000` | `STUB_WRONG` B12 all actions | `ABSENT` |
| R059 | §39, A23 §6.4 | max XAUUSD **gross** → `REDUCE_SIZE` or `REJECT` | `20` | `PARTIAL` B15 hard `REJECT` only | `ABSENT` |
| R060 | §39, A23 §6.4 | max XAUUSD **net** → `REDUCE_SIZE` or `REJECT` | `10` | `STUB_WRONG` B16 `ReduceSize` + qty 0 | `ABSENT` |
| R061 | §39 | max position quantity → `REDUCE_SIZE` or `REJECT` | `5` | `PARTIAL` B14 hard `REJECT` | `ABSENT` |
| R062 | §39 | max number of open positions | `20` | `STUB_WRONG` on Increase (R022) | `ABSENT` |
| R063 | §39 | max allowed spread | `2.0` | see R041 | `ABSENT` |
| R064 | §39 | max quote age | `3s` | see R037–R038 | `PARTIAL` |
| R065 | §39 | max source-signal age | `15s` | see R049 | `REASON_ONLY` |
| R066 | §39 | max tolerated price move | `3.0` | see R042 | `ABSENT` |
| R067 | §39 | max slippage | `1.5` | `MISSING` unread | `ABSENT` |
| R068 | §39 | max execution account margin usage | `0.70` | `PARTIAL` B17 | `ABSENT` |
| R069 | §39 | martingale block | `true` | `PARTIAL` B18 | `ABSENT` |
| R070 | §39 | abnormal sizing block | `true` | `PARTIAL` B19 | `ABSENT` |
| R071 | §39 | venue health requirement | caller bool | `PARTIAL` B04 | `ABSENT` |
| R072 | A23 §4.1 | `REDUCE_SIZE` carries a **positive** stepped qty, or fall through to `REJECT` / `SIZE_BELOW_MIN` | — | `STUB_WRONG` B16 | `ABSENT` (`RiskEngineNetExposureReduceSizeTests` **PHANTOM**) |
| R073 | A71 §8.1 | Net/gross arithmetic is **signed / side-aware**. A long add against a short book can **reduce** \|net\| | — | `STUB_WRONG` `abs(net)+requested` | `ABSENT` |
| R074 | A23 §5 #11 | After normalize/step, remainder below min → `SIZE_BELOW_MIN`, do not send | — | `MISSING` | `ADJACENT` `QuantityNormalizer` returns 0; engine never calls it |
| R075 | §60 | Unit tests: **each hard limit in isolation** | — | n/a | `ABSENT` (`RiskEngineHardLimitTests` **PHANTOM**) |

Loss/DD/daily-loss **must not freeze mapped CLOSE** (A71 E8). Today they do. That is the highest-severity *logic* defect in this file (B13-01 / D13-01).

### 3.8 Decisions and sizing

| ID | Law | Rule | Engine | Test | Notes |
|---|---|---|---|---|---|
| R076 | §39 | Decisions: approve / reduce size / reject / pause trader / pause venue / global stop | `MATCH` enum; `PARTIAL` use | `PARTIAL` Approve/Reject/GlobalStop seen; ReduceSize/PauseTrader/PauseVenue **never** | |
| R077 | A23 §8.3 | `PAUSE_*` / `GLOBAL_STOP` **mutate durable risk state** (audited). Not implicit | `MISSING` | `ABSENT` | Outcome forgotten. |
| R078 | §38, exec #10 | **Never** `source 0.10 MT5 lots = dest OrderQty 0.10` | `MISSING` in engine (uses pre-cooked `RequestedQuantity`) | `ADJACENT` / **PARTIAL** conversion tests + skipped “unused by RiskEngine” | Not a live-path proof. |
| R079 | §38 | Normalize: source volume → notional/risk → allocation → dest qty → step/min/max → re-check caps | `MISSING` | `ADJACENT` `Quantity_normalizer_steps_and_min` + `QuantityNormalizerStepMinMaxTests` | Engine does not re-check after step. |
| R080 | §38, §68 | Unit tests against **real known** source/destination examples before live | `ADJACENT` | `PARTIAL` conversion table exists; not consumed by `Evaluate` | Gate still FAIL. |
| R081 | A23 §7 | Sizing applies to OPEN/INCREASE. REDUCE/CLOSE from mapped dest | see R021 | `ABSENT` | |

### 3.9 Failure rules (§62) and recon

| ID | Law | Rule | Engine | Test | Notes |
|---|---|---|---|---|---|
| R082 | §62 MT5 | Do not invent source trades. Do not open new copy from stale source | `MISSING` no `SOURCE_STALE` | `ABSENT` | |
| R083 | §62 ML | Continue ingest. Do not *promote* on missing score. Hard limits stay on | `DEFERRED` / `ADJACENT` | `ADJACENT` `CanPromoteToLive => false` (D97) | Engine never sees ML. Correct. |
| R084 | §62 QUOTE | Do not create new live copy requiring fresh pricing | `PARTIAL` B04/B05/B06 | `ABSENT` as QUOTE-down | |
| R085 | §62 TRADE | Do not queue an unlimited stale-entry backlog. Mark and expire. Do not resend unknown | `MISSING` | `ABSENT` | Overlaps R053. |
| R086 | §62 DB | Fail closed for new orders. No real execution from volatile memory | `MISSING` no `database_available` | `ABSENT` | |
| R087 | §42 | After TRADE logon: block new exec → mass status + positions → compare DB → only then `READY_FOR_EXECUTION` | `PARTIAL` `Reconciled` bit | `REASON_ONLY` Open unreconciliation | Bit is **caller-supplied**, not derived from `execution_reconciliation_runs`. |
| R088 | §70.14 | Reconciliation blocks execution while inconsistent | `PARTIAL` B03 increasing-only | `REASON_ONLY` | Close of known dest skips the check (correct *direction*) but has no dest identity. |
| R089 | A23 §5 #4 | Unresolved `EXECUTION_STATE_UNKNOWN` → do not send another order | `MISSING` | `ADJACENT` FSM | |

### 3.10 Trader eligibility, shadow, concentration

| ID | Law | Rule | Engine | Test | Notes |
|---|---|---|---|---|---|
| R090 | §22, A23 §3.5 | Live copy only if trader `LIVE` (or explicit live-candidate gate) | `MISSING` no `trader_state` | `ABSENT` | `TraderState` enum exists; scorer never emits `LIVE` (`CanPromoteToLive=false`). |
| R091 | §23 | Severe risk flags / martingale / averaging-down block live promotion | `PARTIAL` B18/B19 flags only | `ABSENT` in risk tests | Detection lives in `BaselineScorer`, not re-run in engine (A23 §6.5 — correct split). Wiring of flags → request is **MISSING**. |
| R092 | exec #4, §72.16 | Trade #3 = early evidence → **SHADOW**, not live | `ADJACENT` scorer | `ADJACENT` scorer tests | Not a `Evaluate` rule. |
| R093 | §24 | Shadow-copy before live. Shadow P&L must not mark on rotten quotes | see R045 | `ABSENT` | |
| R094 | §65 | Do not copy 50 lookalikes. Concentration caps. **Phase 2 after basic copy is stable** | `DEFERRED` | `ABSENT` **negative** fact (`CONCENTRATION_CAP` never returned) | v1 must **not** refuse copy for lack of a cluster graph. |
| R095 | A23 §10 | Reason `CONCENTRATION_CAP` reserved, unused until flag on (default off) | `DEFERRED` / `MATCH` by absence | `ABSENT` negative | Do not implement now. |

### 3.11 RBAC, dashboard, telemetry (copy-adjacent)

| ID | Law | Rule | Engine | Test | Notes |
|---|---|---|---|---|---|
| R096 | §59 | Only authorized roles: enable real execution, change risk limits, pause trader, activate stop-new, request flatten | `MISSING` | `ABSENT` | D53: API is anonymous including `POST /api/ops/resync`. |
| R097 | §53 | Risk page: equity/margin, daily P&L, DD, XAU long/short/net, rejects+reasons, both kill controls | `ADJACENT` | `ABSENT` as engine tests | Dashboard last-reject reasons are **not** from `Evaluate`. |
| R098 | §57 | Log `correlation_id`, broker, login, trade, copy_intent, risk_decision, execution_intent | `MISSING` | `ABSENT` | `BrokerId`/`SourceLogin` unused. |
| R099 | §58 | Metrics: `risk_rejections_total`, copy/execution counters, slippage | `MISSING` | `ABSENT` | |

### 3.12 Required tests and go-live (the architecture’s own checklist)

| ID | Law | Required proof | On disk | Status |
|---|---|---|---|---|
| R100 | §60 | Unit: risk limits (isolation) | `RiskEngineTests` 5 facts | **FAIL** |
| R101 | §60 | Unit: copy-intent idempotency | no `Evaluate` involvement | `ADJACENT` / **MISSING** orchestrator |
| R102 | A23 §11.3 | Stale quote; stale signal + `expires_at`; no 20-intent catch-up; price-move/spread; lots ≠ dest qty; `REDUCE_SIZE` vs reject-below-min; OPEN stricter than CLOSE; stop-new does not flatten; flatten permission distinct; recon/unknown blocks; **zero FIX outbound** on reject | see §§3.5–3.9 | **FAIL** |
| R103 | §68 | position sizing conversion verified | conversion tests exist; unused on path | **FAIL** |
| R104 | §68 | risk engine unit/integration tests pass | 5 smoke facts; 0 integration | **FAIL** |
| R105 | §68 | stale quote rejection works | 1 Open fact | **FAIL** |
| R106 | §68 | stale signal rejection works | 1 Reason-only fact | **FAIL** |
| R107 | §68 | kill switch tested | 1 Open+Close stub lock | **FAIL** |
| R108 | §70.11 | risk-engine rejection before FIX send | no send function | **FAIL** (`SAFE_BY_ABSENCE` ≠ pass) |
| R109 | §70.12 | real execution feature flagged | defaults false; unbound; no NOS | **PARTIAL** default / **FAIL** as gate |
| R110 | §70.13–14 | global stop-new works; recon blocks while inconsistent | return values, not latches / runs | **FAIL** |

---

## 4. Input map (`CopyIntent` / snapshot → `RiskEvaluationRequest`)

What the **law** needs vs what the **request** has. Engine cannot implement a rule whose field is absent.

| Spec input (A23 §3 / §63 / A71) | On `CopyIntent` entity | On `RiskEvaluationRequest` | Used by `Evaluate` |
|---|---|---|---|
| `copy_intent_id` | `Id` | `CopyIntentId` | echoed only |
| `source_broker_id` | `BrokerId` | `BrokerId` | **unread** |
| `source_login` | `SourceLogin` | `SourceLogin` | **unread** |
| `source_trade_id` | `SourceTradeId` | **no** | — |
| `source_event_id` | **no** (`SourcePositionId` only) | **no** | — |
| `canonical_symbol` | `CanonicalSymbol` default XAUUSD | **no** | — |
| `side` | `Direction` | **no** | net/gross **side-blind** |
| `exposure_class` | `Action` | `Action` | yes |
| `source_volume` | folded into `RequestedQuantity` | `RequestedQuantity` (treated as dest qty) | yes |
| `source_price` / expected dest | `ExpectedPrice` | `ExpectedPrice` | mid-move only |
| `source_event_time` | `SourceEventTime` | `SourceEventTime` | signal age |
| `collector_receive_time` | **no** | **no** | — |
| `decision_time` | **no** (`CreatedAt`) | `DecisionTime` | yes |
| `expires_at` | `ExpiresAt` | **no** | **unused** |
| `max_signal_age` | **no** | via `RiskLimits.MaxSourceSignalAge` only | global only |
| `suggested_allocation` / `confidence` | **no** | **no** | correctly unused |
| `trader_state` | **no** | **no** | — |
| `linked_destination_position_id` | **no** | **no** | CLOSE identity **impossible** |
| dest remaining qty / dest side | **no** | **no** | — |
| quote snapshot | entity `DestinationQuoteSnapshot` | `Quote` DTO | receive-age / spread / mid |
| `venue_timestamp` | on snapshot | on DTO | **unread** |
| `REAL_COPY_EXECUTION_ENABLED` | — | `RealExecutionEnabled` | `allowSend` |
| `READY_FOR_EXECUTION` | — | `Reconciled` | increasing reject |
| QUOTE vs TRADE health | — | single `VenueHealthy` | increasing reject |
| `stop_new` × flatten phase | exclusive `KillSwitch.Mode` | exclusive `KillSwitch` | B01/B02 |
| book: trader loss, daily PnL, DD, gross, net, opens, margin | — | yes | yes (loss/DD **all** actions) |
| `MartingaleFlag` / `AbnormalSizing` | scorer features | yes | increasing |
| `database_available` | — | **no** | — |
| `source_data_stale` | — | **no** | — |
| `unknown_on_this_dest_id` | — | **no** | — |
| `flatten_run_id` / owner | — | **no** | — |

`INTENT_INCOMPLETE` / `MAPPING_MISSING` / `TRADER_NOT_LIVE` / `SOURCE_STALE` / `QUOTE_FIX_UNAVAILABLE` / `TRADE_FIX_UNAVAILABLE` / `DATABASE_UNAVAILABLE` / `EXECUTION_STATE_UNKNOWN` / `INTENT_EXPIRED` / `MAX_SLIPPAGE_EXCEEDED` / `SIZE_BELOW_MIN` are **named in A23** and **never emitted**.

---

## 5. Reason-code map

| Reason (engine or spec) | Emitted? | Branch | Test assert | Spec family |
|---|---|---|---|---|
| `STOP_NEW_EXECUTION` | yes | B01 | **unasserted** (outcome only) | open/inc |
| `EMERGENCY_FLATTEN_BLOCKS_NEW` | yes | B02 | none | open/inc |
| `VENUE_NOT_RECONCILED` | yes | B03 | Reason only, Open | open/inc |
| `VENUE_UNHEALTHY` | yes | B04 | none | open/inc |
| `QUOTE_MISSING` | yes | B05 | none | open/inc |
| `QUOTE_STALE` | yes | B06 | Outcome+Reason+Send, Open 30 s | open/inc |
| `SPREAD_TOO_WIDE` | yes | B07 | none | open/inc |
| `PRICE_MOVED_TOO_FAR` | yes | B08 | none | open/inc |
| `SIGNAL_STALE` | yes | B09 | Reason only, Open 5 min | open/inc |
| `MAX_LOSS_PER_TRADER` | yes | B10 | none | **all (wrong)** |
| `MAX_DAILY_EXECUTION_LOSS` | yes | B11 | none | **all (wrong)** |
| `MAX_PORTFOLIO_DRAWDOWN` | yes | B12 | none | **all (wrong)** |
| `MAX_OPEN_POSITIONS` | yes | B13 | none | open/**inc (wrong)** |
| `MAX_POSITION_QUANTITY` | yes | B14 | none | open/inc |
| `MAX_XAU_GROSS` | yes | B15 | none | open/inc |
| `MAX_XAU_NET` | yes | B16 | none | open/inc |
| `MAX_MARGIN_USAGE` | yes | B17 | none | open/inc |
| `MARTINGALE_BLOCK` | yes | B18 | none | open/inc |
| `ABNORMAL_SIZING_BLOCK` | yes | B19 | none | open/inc |
| `RISK_REDUCTION` | yes | B20 | **unasserted** | red/close |
| `APPROVED` | yes | B21 | **unasserted** | open/inc |
| `QUOTE_UNAVAILABLE` | **no** | — | — | A23 |
| `INTENT_EXPIRED` | **no** | — | — | §63 |
| `MAX_SLIPPAGE_EXCEEDED` | **no** | unread limit | — | §37 |
| `SIZE_BELOW_MIN` / `SIZE_REDUCED_TO_LIMIT` | **no** | — | — | A23 §7 |
| `INSUFFICIENT_MARGIN` | **no** | — | — | A23 |
| `TRADER_NOT_LIVE` / `TRADER_PAUSED` / `TRADER_RISK_BLOCKED` | **no** | — | — | A23 |
| `SEVERE_RISK_FLAG` | **no** | — | — | §23 |
| `REAL_EXECUTION_DISABLED` | **no** (flag only clears send) | — | Fact 2 implicit | §41 |
| `QUOTE_FIX_UNAVAILABLE` / `TRADE_FIX_UNAVAILABLE` | **no** | — | — | §62 |
| `SOURCE_STALE` | **no** | — | — | §62 |
| `RECONCILIATION_BLOCK` | **no** (uses `VENUE_NOT_RECONCILED`) | — | — | A23 |
| `EXECUTION_STATE_UNKNOWN` | **no** | — | — | §34 |
| `DATABASE_UNAVAILABLE` | **no** | — | — | §62 |
| `MAPPING_MISSING` / `NO_DESTINATION_POSITION` | **no** | — | — | A71 |
| `INTENT_INCOMPLETE` | **no** | — | — | A23 |
| `CONCENTRATION_CAP` | **no** — **correct** | — | no negative fact | §65 Phase 2 |
| `ML_UNAVAILABLE` | **no** — **correct** (must not block hard limits) | — | — | §62 |

---

## 6. The five facts, mapped to rules

File: `D:\Prop\tests\Unit\RiskEngineTests.cs`. Fixture `Base()` is a single interior `OpenExposure` point, `RealExecutionEnabled=false`, default `RiskLimits`.

| Fact | Rules it **touches** | Assert quality | What it **cannot** tick |
|---|---|---|---|
| `Stale_quote_rejects_open` | R037, R047 (interior) | Outcome+Reason+Send | R038–R040, R045, Increase, Close exemption, `age==3s`, dual clock, qty 0 |
| `Real_flag_false_never_allows_fix_send` | R011 (request bit) | Outcome+Send; Reason unasserted | R012 send-true; worker refuse; options bind; R014 flatten exception |
| `Stop_new_execution_blocks_opens_not_closes` | R026 (Open outcome); R032 **inverted** | Open Outcome only; Close Outcome+Send=false | R027 dest untouched; R028 flatten; R029 two-bit SoT; Increase/Reduce; reason strings |
| `Unreconciled_venue_blocks_new_exposure` | R087/R088 (Open string) | Reason only | Outcome, send, qty, Increase, Close-of-known, recon-run derivation |
| `Stale_signal_rejected` | R049 (Open string) | Reason only | R050–R053 `expires_at` / catch-up; Increase; Close clock; Outcome/send/qty |

**Worst lock-in:** fact 3 Close `AllowFixSend=false` **is** current code and **is not** A48 law (R032). Label any rewrite `CURRENT_STUB` vs `SPEC`.

---

## 7. Required test classes vs disk

A27 §4.5 / A89 #50–59. A89 `EXISTS` is **false** (measured 2026-08-18). Folder `tests/Unit/Risk/` **does not exist**.

| A89 # | Claimed class | Disk | Rules it would lock | Actual coverage |
|---:|---|---|---|---|
| 50 | `RiskEngineHardLimitTests` | **No** | R056–R071, R075, R100 | 3/19 reasons, none isolated full-tuple |
| 51 | `RiskEngineApproveReduceRejectTests` | **No** | R076, R012 send-true | 3/6 outcomes; send-true = 0 |
| 52 | `OpenVsCloseExposurePolicyTests` | **No** | R016–R024, R017 matrix | 1 Close under StopNew |
| 53 | `QuoteFreshnessGuardTests` | **No** | R037–R040, R047 | 1× 30 s Open |
| 54 | `PriceMoveAndSpreadGuardTests` | **No** | R041–R043 | **Zero** |
| 55 | `StaleCopyIntentExpiryTests` | **No** (helper tested elsewhere) | R048–R053 | 1× 5 min Reason; `Copy_intent_expires` is **not** `Evaluate` |
| 56 | `KillSwitchStopNewExecutionTests` | **No** | R026–R027, R032, R035 | 1 Open + 1 Close stub lock |
| 57 | `KillSwitchEmergencyFlattenTests` | **No** | R028–R030 | **Zero** |
| 58 | `RealExecutionFeatureFlagTests` | **No** | R011–R013, R109 | Flag false only |
| 59 | `RiskEngineNetExposureReduceSizeTests` | **No** | R060, R072–R073 | **Zero** |

A27 FIX-harness names (`RiskRejectionBeforeFixSendTests`, `GlobalStopNewOrdersTests`, `QuoteUnavailableBlocksNewCopyTests`, `TradeUnavailableDoesNotQueueUnlimitedBacklogTests`): **not on disk**. `tests/Fix/` **does not exist**.

B36 fixture families `RF-SQ-*` / `RF-SS-*` / `RF-KS-*` / `RF-RA-*` / `RF-RB-*`: **design only**. No JSON / extra facts.

### Adjacent tests that are **not** this coverage

| Path | Why it does not count as a risk-rule lock |
|---|---|
| `ExecutionAndSizingTests.Copy_intent_expires` | `CopyIntentExpiry` helper; engine never calls it (R050) |
| `ExecutionAndSizingTests.Unknown_ack_cannot_retry_new_order` | FSM (R006), not `Evaluate` |
| `ExecutionAndSizingTests.Quantity_normalizer_steps_and_min` | Sizing helper unused by engine (R078–R079) |
| `Sizing/QuantityNormalizerStepMinMaxTests.cs` | Same |
| `Normalization/SourceDestinationQuantityConversionTests.cs` | Includes a **skipped** fact that `QuantityNormalizer` is unused by `ShadowCopyEngine` and `RiskEngine` |
| `BaselineScorerTests` | Flags/state; not wired into `Evaluate` (R091) |
| `tests/Integration/*` | No `Evaluate` |

---

## 8. Config / docs conflicts (do not treat as one limit set)

Three **disagreeing** documents plus compile defaults. None is bound into `Evaluate` except `new RiskLimits()`.

| Source | Quote age | Signal age | Open positions | Position size | Daily loss | Kill |
|---|---|---|---|---|---|---|
| `RiskLimits` defaults | **3 s** | **15 s** | **20** | **5** qty | **2000** currency | enum on request |
| `CTraderFixOptions` | **5000 ms** | — | — | — | — | — |
| `apps/api/Program.cs` `/api/settings` | **3 s** | **15 s** | — | — | — | — |
| `apps/api/appsettings.json` `RiskEngine` | — | — | **20** | **10.0** | **5.0%** drawdown | `KillSwitchEnabled=true`, `KillSwitchOn=false` |
| `SettingsController` GET | — | — | 20 | 10 | 5% | `KillSwitchEnabled` |
| `docs/risk.md` | delay window **100–2000 ms** | same window | **25** | **50 lots** | **5%** day / **10%** total | different flatten auto-chain |

`docs/risk.md` is **not** architecture-faithful (percent DD, 50-lot cap, 100 ms *minimum* delay, auto-flatten on total-loss). Do not implement from that one-pager. Architecture v2 + A23/A48/A71/A72 win.

Settings PUT writes Redis `settings:risk:*` / `settings:flags:*`. **No reader** feeds `RiskLimits` or `RiskEvaluationRequest`.

---

## 9. `AllowFixSend` truth table (current code vs law)

Computed only after all limits, **not** family-aware.

| # | Real | Kill | Recon | Venue | Action | Loss/DD | Code send | Law source-close | Law flatten CLOSE |
|---:|---|---|---|---|---|---|---|---|---|
| C1 | T | None | T | T | Close | ok | **true** | true **if dest known** (identity MISSING) | n/a |
| C2 | T | StopNew | T | T | Close | ok | **false** | **true** (A48 default) | n/a |
| C3 | T | Flatten | T | T | Close | ok | **false** | coalesce or true | **true** (Phase 8) |
| C4 | F | Flatten | T | T | Close | ok | **false** | false (v1) | **true** (A25 exception) |
| C5 | T | None | T | T | Close | daily −2000 | rejected (B11) | **true** (must exit) | **true** |
| C6 | T | Flatten | T | T | Open | ok | n/a `GlobalStop` | n/a | block new — **MATCH** |
| C7 | T | None | T | T | Open | ok | **true** | n/a | must stay false if flatten owns book |
| C8 | F | None | T | T | Open | ok | **false** | n/a | shadow shape — **MATCH** |

Only C8 is unit-touched (fact 2). C1 send-true is **untested**. C2 is **locked inverted** (fact 3). C5 is the red-day freeze (R056–R058).

A buggy caller can pass `KillSwitch=None`, `Reconciled=true`, `VenueHealthy=true`, `RealExecutionEnabled=true`, `Action=CloseExposure`, `RequestedQuantity=99` and receive `AllowFixSend=true`. Approval is specified as **not** a capability token (A49). Here it is the only token, and it is made of caller-supplied bits. **UNSAFE if wired. DEAD today.**

---

## 10. Evaluation-order vs A23 §5

| Spec step | Present? | Notes |
|---:|---|---|
| 1 Infrastructure / DB available | **no** | R086 |
| 2 Feature flag as control flow | empty `if`; send bit **after** limits | R012 |
| 3 Kill switch | **yes** first real checks | B01/B02 |
| 4 Recon / unknown | bool only | B03; R089 missing |
| 5 Venue health QUOTE vs TRADE | one bool | B04 |
| 6 Source health / stale MT5 | **no** | R082 |
| 7 Trader eligibility / `LIVE` | flags late; no state | R090 |
| 8 Intent expiry + signal age | signal only | R050–R052 |
| 9 Quote age / spread / signed move / slippage | receive-age + spread + unsigned mid | R037–R043 |
| 10 Sizing normalize / step | **no** | R078–R079 |
| 11 Book / account caps; reduce-to-cap | present; `ReduceSize` broken | R056–R073 |
| 12 Martingale / abnormal | yes, increasing | B18/B19 |
| 13 Concentration | correctly unused | R094 |
| 14 Persist `risk_decision` | **no** | R009 |

Unknown `CopyIntentAction` / `KillSwitchMode` → fall through to `APPROVED` (D13-16). Unasserted.

---

## 11. What is actually good (do not regress)

Keep these names/shapes when the stub is replaced.

- Four §64 tokens and `IsIncreasing` / `IsReducing` helpers.
- Two **reason codes** for stop-new vs flatten-in-progress (not one bool).
- Market-quality / recon / venue / book-cap / behavioral guards gated on increasing — Close family is **not** failed by entry news guards (directionally A23 §8.2 / A71).
- Flatten-in-progress does **not** auto-fire dest closes from `Evaluate` (auto-flatten from a threshold is forbidden).
- `Reject` always `AllowFixSend=false` and qty 0.
- Approve + `AllowFixSend=false` is the correct **shadow shape** when the flag is false (C8).
- `CONCENTRATION_CAP` / ML-in-hot-path correctly unused.
- FIX worker still does not send. API has no flatten POST. Do not add either to “complete” this matrix.
- `docs/risk.md` *labels* (`STOP_NEW` ≠ flatten; scoring proposes; risk decides) are right even when its **numbers** are wrong.

---

## 12. Honest counts (this matrix)

| Bucket | IDs | n |
|---|---|---:|
| `MATCH` (predicate = law, still unwired) | R016, R018 (those five checks), R026, R027 (omission), R076 enum | **5** core; plus B01/B06/B07/B09 shape |
| `PARTIAL` | R003, R012, R017, R028, R031, R036, R037, R040, R041, R044, R048, R049, R054, R059, R061, R063–R066, R068–R071, R084, R087, R088, R091, R109 | **22** |
| `STUB_WRONG` | R008-as-token, R021, R022, R024, R029, R030, R032, R038 (unbound constants), R042, R045 (shadow bypass), R056–R058, R060, R072, R073 | **11** distinct defects (loss/DD counted once as a family) |
| `MISSING` | R001, R004-as-proof, R007, R009, R010, R019, R020, R023, R033, R039, R043, R046, R050–R053, R067, R074, R077, R082, R085, R086, R089, R090, R096–R099 | **41** |
| `DEFERRED` | R083 (ML), R094, R095 | **3** |
| `ADJACENT` | R002, R005, R006, R011, R013, R015, R025, R034, R078–R080, R092, R097, R101 | **15** |
| `SAFE_BY_ABSENCE` | R002, R008, R108, live copy overall | send path |

These buckets overlap on a few IDs (a rule can be `PARTIAL` *and* `DEAD`). Counts in §0 use the **primary** class in the Engine column.

---

## 13. Explicit non-claims

- Not “§39 implemented.”
- Not “§64 implemented.” Family *names* exist.
- Not “kill switch tested.”
- Not “stale quote / stale signal rejection works” on a feed or send path.
- Not “REAL_COPY is an implemented NOS gate.” Default-false is honored wherever a default exists.
- Not “A89 EXISTS rows are on disk.”
- Not “B13/D13 fixed.” Same SUT hash `AE0F9FAE…9052D`, same 5-fact test hash `7B952364…2DF51`.
- Not “≥95%” anything.
- No product source modified. No test source modified. No MQ5. No FIX send. No secrets.

---

## 14. Traceability

| Topic | Primary evidence | Law |
|---|---|---|
| Engine branches | `RiskEngine.cs` 76–188 | A23 §5; this file §2 |
| Limits | `RiskEngine.cs` 5–22 | §39 |
| Family helpers | `RiskEngine.cs` 174–178 | §64, §72.18 |
| Tests | `RiskEngineTests.cs` 11–55 | §60, §68 |
| Expiry helper unused | `CopyIntentExpiry.cs`; `ExecutionAndSizingTests` 56–61 | §63 |
| Sizing unused | `QuantityNormalizer.cs`; skipped conversion fact | §38 |
| Shadow bypass | `ShadowCopyEngine.cs` | §24, A24 |
| Flag default | `CTraderFixOptions.cs` 35; `Program.cs` 45; `Worker.cs` 21–22, 45–46 | §41 |
| DI | `DependencyInjection.cs` 36–42 | §32 |
| Kill SoT | `KillSwitchMode.cs`; `Entities/KillSwitch.cs` | §40, A48 |
| Phantom inventory | A89 #50–59; A27 §4.5 | do not cite as coverage |
| Engine defects | B13 / D13 | same hash |
| Test gaps | C03 / D35 | same 5 facts |
| Fixture design (not landed) | B36 | RF-* families |

**Bottom line:** 110 architecture risk/copy rules map onto a 21-branch stub, 5 smoke facts, and a dead send path. **18** predicates roughly match the law. **11** contradict it if wired (red-day freeze, send-under-stop-new, `ReduceSize` qty 0, exclusive kill enum, unsigned mid, unmapped close, Increase-as-new-slot, side-blind net). **41** are simply not in the function. The only reason that is not a live incident is that **nobody calls `Evaluate` and nobody sends FIX.**
)
