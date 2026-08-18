# P502 — XAUUSD 1:1 copy selection rules

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P502_SELECTION_RULES.md` |
| Slot | **P502** |
| Date | 2026-08-18 |
| Product source edited | **No** (report only; product/tests not changed) |
| Assigned reads | `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs`, `D:\Prop\tests\Unit\XauUsdOneToOneCopyPolicyTests.cs` |
| Adjacent reads | `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `QuantityNormalizer.cs`, `SymbolNormalizer.cs`, `RiskEngine.cs`, `CopyIntentAction.cs`, `TraderState.cs`, `BaselineScorer.cs` |
| Verdict | Policy **CONFIRMS** all seven assigned rules at the domain gate. Tests cover the happy path of each. **Holes remain** in symbol prefix matching, lot cap/step, demo-group prefix only, ReduceExposure not lookahead-gated, 19/20 boundary untested, and close being **poll-driven** (20 s) rather than a deal event. |
| Tests wrong? | **No.** Do not change product or tests. |
| Secrets | None in this slot. No passwords, tokens, or FIX keys quoted. |

Empty PASS is forbidden. Both assigned files were opened in full.

---

## 0. What the policy is

`XauUsdOneToOneCopyPolicy` selects **traders**, then copies their **next** XAUUSD event 1:1. It does **not** wait until a ticket is profitable.

Quoted class law (`XauUsdOneToOneCopyPolicy.cs` 57–61):

> Live copy selects **traders** with a measured XAUUSD edge, then copies their next XAUUSD events 1:1 (lots, SL/TP, side). It does not wait until a ticket is profitable — that is lookahead and cannot be traded live. Close is copied when the source closes, not at a predicted time.

Constants:

| Constant | Value | Role |
|---|---|---|
| `MinCompletedXauTrades` | `20` | Trader-history floor |
| `AllocationFactor` | `1m` | 1:1 lots |
| `GoldOuncesPerLot` | `100m` | FIX `OrderQty` ounces |
| `GoldLots` | min `0.01`, max `5`, step `0.01`, precision `2` | Dest qty grid |

Caller (not the policy, but the only production consumer): `CopyTradingService.GenerateShadowIntentsAsync` builds `CopyTraderSnapshot` from `TraderScore` + completed XAU reconstructed PnL + `Mt5Account.GroupName`, then opens still-open reconstructed XAU tickets and later closes them when `Completed && ClosedAt` is set.

---

## 1. Confirmation matrix (the seven rules)

| # | Rule | Policy | Unit test | Verdict |
|---|---|---|---|---|
| 1 | Does **not** select individual closed winners (lookahead banned) | `OpenExposure` / `IncreaseExposure` reject when `!SourceStillOpen` → `NO_LOOKAHEAD_CLOSED_WINNER` | `Closed_winner_is_lookahead_and_rejected` | **CONFIRMED** (with holes §3.1) |
| 2 | XAUUSD only | Reject unless canonical/source is `XAUUSD` or source starts with `XAU` / `GOLD` → `NOT_XAUUSD` | `EurUsd_is_rejected` | **CONFIRMED intent**; matcher is **looser** than canonical XAUUSD (§3.2) |
| 3 | 1:1 lots | `AllocationFactor = 1m`; `Lots = Normalize(SourceLots, 1, GoldLots)` | `Eligible_open_xau_is_one_to_one_lots_and_sl_tp` (`0.05` → `0.05`, ounces `5`) | **CONFIRMED** on-grid; **not** 1:1 above 5 lots or off-step (§3.3) |
| 4 | SL/TP copied | Accept path copies `signal.StopLoss` / `signal.TakeProfit` onto `CopyInstruction` | Same test: `4380` / `4410` | **CONFIRMED** when present; nulls pass through (§3.4) |
| 5 | Demo groups blocked | `GroupName` starts with `demo\` or `contest\` (ignore case) → `DEMO_OR_CONTEST_GROUP` | `Demo_group_blocked` (`demo\yo-2step`) | **CONFIRMED** for that prefix; **narrow** matcher (§3.5) |
| 6 | 20-trade min | `CompletedXauTrades < 20` → `NEED_MORE_XAU_HISTORY` | `First_three_trades_not_enough` (`N=3`) | **CONFIRMED** constant; **boundary untested** (§3.6) |
| 7 | Close is event-driven, not predicted time | No hold-time / MFE / “exit at T+N”. `CloseExposure` is accepted as an incoming action; still-open flag is **not** required | `Close_of_open_book_is_one_to_one` (`Action=CloseExposure`, `SourceStillOpen=false`, accept) | **CONFIRMED at policy**. Pipeline is a **20 s poll**, not a deal sink (§3.7) |

---

## 2. How each rule is implemented

### 2.1 Lookahead banned (select traders, not closed tickets)

`Evaluate` after eligibility:

```130:134:D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs
        if (signal.Action is CopyIntentAction.OpenExposure or CopyIntentAction.IncreaseExposure)
        {
            if (!signal.SourceStillOpen)
                return Reject("NO_LOOKAHEAD_CLOSED_WINNER");
        }
```

Trader gate uses **book** XAU net PnL (`XauNetPnl <= 0` → `XAU_BOOK_NOT_PROFITABLE`) and size-pattern flags. It never inspects the candidate ticket’s realized PnL. That is the correct anti-lookahead split: profitable **book** may be copied; a **closed winning ticket** may not be opened after the fact.

Service layer agrees: open loop is `xau.Where(t => !t.Completed)` and hard-codes `SourceStillOpen = true`. Close loop is `t.Completed && t.ClosedAt.HasValue` and only if an open idempotency key already exists (`copy:{broker}:{login}:{position}:open`). You cannot mint an open intent from a completed winner through the current caller.

### 2.2 XAUUSD only

```122:128:D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs
        if (!string.Equals(signal.CanonicalSymbol, "XAUUSD", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(signal.SourceSymbol, "XAUUSD", StringComparison.OrdinalIgnoreCase)
            && !signal.SourceSymbol.StartsWith("XAU", StringComparison.OrdinalIgnoreCase)
            && !signal.SourceSymbol.StartsWith("GOLD", StringComparison.OrdinalIgnoreCase))
        {
            return Reject("NOT_XAUUSD");
        }
```

Does **not** call `SymbolNormalizer`. EURUSD is rejected (tested). Anything starting `XAU`/`GOLD` is accepted even if it is not USD gold.

`CopyTradingService` pre-filters `CanonicalSymbol == "XAUUSD"` before `Evaluate`, so the live caller is stricter than the policy.

### 2.3 1:1 lots

`AllocationFactor = 1m`. `QuantityNormalizer` does `sourceLots * 1`, truncates to `0.01`, rounds toward zero to 2 dp, returns `0` below min, **clamps to 5** above max. `FixOrderQtyUnits = Round(lots * 100, 2, ToZero)`.

Tested: `0.05` lots → `0.05` lots / `5` ounces. `0.001` → `QTY_BELOW_MIN_OR_STEP`. Close of `0.10` → `0.10` / `10` ounces.

### 2.4 SL/TP copied

Accept instruction assigns `StopLoss = signal.StopLoss`, `TakeProfit = signal.TakeProfit` with no rewrite. Limit/stop extras (`LimitPrice`, `StopTrigger`, `OrdType`) also pass through. Limit without price / stop without trigger reject.

Service open path: `StopLoss = trade.FinalSl ?? trade.InitialSl`, same for TP. Service close path does **not** persist SL/TP (irrelevant for a market close).

### 2.5 Demo / contest groups blocked

```105:111:D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs
        if (!string.IsNullOrWhiteSpace(trader.GroupName)
            && (trader.GroupName.StartsWith("demo\\", StringComparison.OrdinalIgnoreCase)
                || trader.GroupName.StartsWith("contest\\", StringComparison.OrdinalIgnoreCase)))
        {
            reason = "DEMO_OR_CONTEST_GROUP";
            return false;
        }
```

Only **prefix** `demo\` or `contest\` (backslash). Null/blank group is **eligible**. Test covers `demo\yo-2step` only.

### 2.6 Twenty completed XAU trades

```93:97:D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs
        if (trader.CompletedXauTrades < MinCompletedXauTrades)
        {
            reason = "NEED_MORE_XAU_HISTORY";
            return false;
        }
```

`MinCompletedXauTrades = 20`. Distinct from scorer `EarlyScoreTradeCount = 3` (dashboard / SHADOW promotion). Test uses `N=3` (not 19).

Other trader gates (same method): `RISK_BLOCKED` / `DISQUALIFIED` / `PAUSED` → `TRADER_BLOCKED_*`; `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` → `TRADER_NOT_SHADOW_YET`; martingale / averaging-down / lot-escalation → `TRADER_SIZE_PATTERN_BLOCK`; `XauNetPnl <= 0` → `XAU_BOOK_NOT_PROFITABLE`. SHADOW, LIVE_CANDIDATE, LIVE pass the state check.

### 2.7 Close is an event, not a predicted clock

Policy has **no** average-hold, MFE target, or “exit at source OpenedAt + N”. Close is whatever `CopyIntentAction` the caller feeds.

`CloseExposure` is **not** subject to `NO_LOOKAHEAD_CLOSED_WINNER`. Test `Close_of_open_book_is_one_to_one` accepts a close with `SourceStillOpen = false` and copies lots 1:1.

There is no predicted-time field on `CopySignal` / `CopyInstruction`.

---

## 3. Holes (do not treat as PASS)

### 3.1 Lookahead

| Hole | Why it matters |
|---|---|
| `ReduceExposure` is **not** gated on `SourceStillOpen` | A closed winner passed as Reduce would be accepted (lots, SL/TP) if the caller lies about action. Enum exists (`CopyIntentAction.ReduceExposure = 2`). No unit test. |
| `IncreaseExposure` closed-winner reject is untested | Same code path as Open; only Open is tested. |
| Trusts `SourceStillOpen` | Policy cannot see the book. A buggy caller that sets `true` on a completed winner would copy it. Current `CopyTradingService` does not do this. |
| Book PnL includes the closed sample that just completed | Eligibility is `XauNetPnl` of **all** completed XAU. That is trader selection, not ticket selection — acceptable — but a 19-loser + 1 huge winner book becomes eligible the instant that winner closes. No recency / walk-forward split. |
| Open intents use `FinalSl ?? InitialSl` | If reconstruction already baked a later SL move into `FinalSl` before we emit the open, dest SL is the **updated** stop, not the stop at source entry. Not ticket-PnL lookahead, but it is post-entry information on the same ticket. |
| Stale still-open tickets | Service will try to open any `!Completed` reconstructed XAU on each 20 s tick (idempotent). Policy does not check `SourceEventTime` age; `RiskEngine` does (`MaxSourceSignalAge = 15s`). Old open positions discovered late should die at risk, not at this policy. |

### 3.2 XAUUSD only

| Hole | Why it matters |
|---|---|
| `StartsWith("XAU")` / `StartsWith("GOLD")` | `XAUEUR`, `XAUGBP`, `XAUAUD`, `GOLDNUGGET`, `GOLDFX` accept. `SymbolNormalizer` would reject most of these. Policy and normalizer **diverge**. |
| Exact `XAUUSD` on source **or** canonical is enough | `CanonicalSymbol=EURUSD` + `SourceSymbol=XAUUSD` accepts. Inverse also accepts. Mixed-symbol garbage can pass. |
| No use of `SymbolNormalizer.IsXauUsd` | Alias set (`XAUUSD.A`, `XAUUSDM`, …) is not the gate; prefix is. |
| No unit test for GOLD / `XAUUSD.` / rejected `XAUEUR` | Only EURUSD both-fields is tested. |
| Mitigated in caller | `CopyTradingService` loads `CanonicalSymbol == "XAUUSD"` only. Domain gate is still the reusable API. |

### 3.3 1:1 lots

| Hole | Why it matters |
|---|---|
| `GoldLots` max `5` | Source `6.00` becomes dest `5.00`. Silent clip, still `Accept=true`, reason `ONE_TO_ONE_XAUUSD`. **Not 1:1.** Untested. |
| Step truncate | `0.333` → `0.33` (`QuantityNormalizer` + `ExecutionAndSizingTests`). Labelled 1:1 but floored. |
| Service uses `MaxVolumeLots` | Scale-in / partial close: dest open/close size is peak lots, not remaining or last deal lots. Oversize close / miss increase. No Increase/Reduce generation in the service at all. |
| Dest account equity ignored | Intentional 1:1, but a 5-lot source print on a small dest is a ruin size. Risk max position is also 5 lots — they agree, they do not protect a small book. |
| `FixOrderQtyUnits` is ounces | 1:1 is **lots**. Ounces (`* 100`) is a FIX translation. Fine if the venue is 100 oz/lot; wrong if dest gold contract differs. |

### 3.4 SL/TP copied

| Hole | Why it matters |
|---|---|
| Null SL/TP accepted | No `REQUIRE_SL`. Source with no stop is copied naked. Scorer penalizes low SL-use; copy policy does not refuse. |
| No side / sanity check | SL above entry on a long, inverted SL/TP, or SL=0 vs null — all pass. |
| Close instruction still copies SL/TP if the signal has them | Harmless if unused; service close omits them. |
| Limit/Stop accept path untested for pass-through | Only the reject-without-price cases exist. |

### 3.5 Demo groups

| Hole | Why it matters |
|---|---|
| Prefix `demo\` / `contest\` only | `demo/yo-2step`, `demo`, `Demo`, `contest` (no slash), `Starwave\demo\...`, `real\contest\...` are **not** blocked. |
| Null `GroupName` is eligible | Missing `Mt5Account` row → `account?.GroupName` null → demo trader copies. |
| No `contest\` test | Code path untested. |
| Challenge / prop groups | Many YoPips / 2-step groups are **not** named `demo\...`. Those are real-money or challenge books and are **not** blocked here. That may be intended (copy real challenge traders) but it is not “all demo blocked.” |
| Case | `DEMO\...` **is** blocked (`OrdinalIgnoreCase`). Not a hole. |

### 3.6 Twenty-trade minimum

| Hole | Why it matters |
|---|---|
| Test is `N=3`, not `N=19` / `N=20` | Off-by-one on the constant would not fail CI. `GoodTrader()` uses `22`. |
| Count comes from `TraderScore.CompletedXauTrades` | Service PnL is summed from reconstructed rows independently. Score count and reconstructed count can desync after a partial score write. |
| Lifetime count, no window | Twenty XAU closes in 2023 + flat 2026 still eligible. |
| `N=20` and `XauNetPnl == 0` still blocked | `<= 0` is the PnL gate. Breakeven 20-trade book is out. Untested. |
| Early-score 3 vs copy 20 | Correct split, but easy to confuse. Dashboard “3 trades” is **not** copy-eligible. |

### 3.7 Close event vs predicted time

| Hole | Why it matters |
|---|---|
| Policy is event-shaped; **infra is not event-driven** | `CopyTradingHostedService` sleeps **20 s**, then scans reconstructed trades. `CopyTradingService` close loop is “row now has `ClosedAt`.” That is **poll-on-book**, not `OnDealAdd` / FIX exec report. |
| Ingest itself is not a live deal sink | Adjacent `P501_SOURCE_MISS.md`: C# ingest is a history batch + score, not `DealSubscribe`. A source close can sit unseen until the next ingest/rebuild, then wait up to 20 s more for the copy tick. |
| Close intents expire in 15 s | `ExpiresAt = now.AddSeconds(15)` on close as well as open. If send is delayed, the **close** dies. Policy does not own expiry. |
| Close only if we already stored an open intent | Correct (do not flatten a dest we never opened). If the open was skipped (stale / risk / ineligible) and the source later closes, no close intent — good. If dest was opened by another path, this loop will not flatten it. |
| No predicted-time test that would fail a regress | Nothing asserts “must not use AverageHoldSeconds / MFE to close.” A future coder can add a timer close beside this policy and these tests still pass. |
| `ReduceExposure` could be used as a fake close | Same as §3.1. |

---

## 4. Test inventory vs rules

File: `D:\Prop\tests\Unit\XauUsdOneToOneCopyPolicyTests.cs` (12 facts).

| Test | Rule touched | Gap |
|---|---|---|
| `Eligible_open_xau_is_one_to_one_lots_and_sl_tp` | 2, 3, 4 | Happy path only |
| `Closed_winner_is_lookahead_and_rejected` | 1 | Open only |
| `EurUsd_is_rejected` | 2 | No GOLD/XAU prefix / XAUEUR |
| `Martingale_trader_blocked` | trader gate | AveragingDown / LotEscalation not separate |
| `Negative_xau_pnl_blocked` | trader gate | Zero PnL untested |
| `First_three_trades_not_enough` | 6 | Not 19/20 |
| `Demo_group_blocked` | 5 | No contest / slash / null group |
| `Risk_blocked_state_rejected` | trader gate | Reason string not asserted; PAUSED / DISQUALIFIED / WATCH untested |
| `Limit_without_price_rejected` | extras | — |
| `Stop_without_trigger_rejected` | extras | — |
| `Close_of_open_book_is_one_to_one` | 3, 7 | No “must not predict time” |
| `Lot_below_min_rejected` | 3 | No max-clip 5.01 |

Tests are **consistent with the product**. None are wrong. Do not edit.

---

## 5. Verdict on the seven confirms

1. **Does not select individual closed winners** — **YES** at policy + current caller. Banned as `NO_LOOKAHEAD_CLOSED_WINNER` on Open/Increase when the source is no longer open. Close of a previously opened copy is allowed.
2. **XAUUSD only** — **YES** as product intent. **NO** as a strict canonical check (`XAU*` / `GOLD*` prefixes).
3. **1:1 lots** — **YES** when `0.01 … 5.00` on a 0.01 grid. **NO** when clipped to 5 or stepped down.
4. **SL/TP copied** — **YES** (pass-through, including null).
5. **Demo groups blocked** — **YES** for `demo\` / `contest\` prefix. **NO** for other demo-shaped names or missing group.
6. **20-trade min** — **YES** (`MinCompletedXauTrades = 20`). Test only proves `3 < 20`.
7. **Close is event-driven, not predicted time** — **YES** in the policy (no time model; close is an action). **NO** in the running system (20 s reconstructed-book poll, not a source close event).

**Product left unchanged.** Tests left unchanged.

---

## 6. Paths

- `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs`
- `D:\Prop\tests\Unit\XauUsdOneToOneCopyPolicyTests.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs`
- `D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs`
- `D:\Prop\src\Domain\Enums\CopyIntentAction.cs`
- `D:\Prop\src\Domain\Enums\TraderState.cs`
