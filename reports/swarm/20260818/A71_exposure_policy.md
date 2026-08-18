# A71 — OPEN_EXPOSURE / INCREASE_EXPOSURE / REDUCE_EXPOSURE / CLOSE_EXPOSURE policies

**Artifact:** `D:\Prop\reports\swarm\20260818\A71_exposure_policy.md`  
**Source of truth:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`  
**Primary section:** §64 Source Close Handling  
**Supporting sections:** §32–§43, §53, §57–§65, §68, §70, §72.17–§72.19  
**Sibling specs (do not contradict):** A21 reconstruction, A23 risk engine, A24 shadow copy, A25 FIX session, A27 test inventory, A38 volume units, A48 kill switch, A49 feature flags  
**Date:** 2026-08-18  
**Status:** specification only — **no product source modified**  
**Scope:** XAUUSD copy path. MT5 sources → reconstructed lifecycle event → `CopyIntent.action` → `RiskEngine` → shadow fill **or** live `execution_intent` → cTrader/cServer FIX 4.4.

---

## 0. Verdict

Architecture §64 is two sentences and four tokens. They are **law**, not labels.

```text
Closing an existing copied position is a risk-reduction action and may deserve
different treatment from new entries.

Design separate policy:

OPEN_EXPOSURE
INCREASE_EXPOSURE
REDUCE_EXPOSURE
CLOSE_EXPOSURE

Risk engine should normally be stricter about opening/increasing
than reducing/closing.
```

§72.18 restates the same duty: *Reduce/close exposure must be treated differently from opening more.*  
§63 last line: *Closing/reducing risk may have separate policy from opening new exposure.*

**Standing rule of this document**

| Family | Classes | Stance | If in doubt |
|---|---|---|---|
| **OPEN family** | `OPEN_EXPOSURE`, `INCREASE_EXPOSURE` | **Strict.** Fail closed. Stale / unpriced / unmapped / unreconciled / stop-new → **no new risk**. | Reject. Do not send. |
| **CLOSE family** | `REDUCE_EXPOSURE`, `CLOSE_EXPOSURE` | **Lenient on market-quality guards.** Still fail closed on **identity** (known dest position, known qty, no double-close). | Prefer reducing residual dest risk over matching a rotten source print. |

Opening more is optional. Residual destination risk after the source is gone is a **defect**. Those are opposite errors. They **must not share one threshold, one expiry, or one reject set**.

This file is the binding policy for later `ExposureAction` / `RiskEngine` work (A30 Increment 8 names `src/Risk/Rules/ExposureAction.cs`). It does **not** implement.

---

## 1. Architecture quotes (binding)

### 1.1 §64 (verbatim contract)

Four named classes. Separate policy. Risk **stricter** on open/increase than on reduce/close.

### 1.2 Adjacent law this design must honor

| Clause | Obligation |
|---|---|
| §32 | Persist `CopyIntent` **before** risk. Never send FIX from an MT5 callback. |
| §33 | Unique `cl_ord_id`. Persist `execution_intent` before any socket write. |
| §34 | Disconnect after send → `EXECUTION_STATE_UNKNOWN`. No blind resend. A second close is not “recovery.” |
| §35 | Explicit `source reconstructed trade → dest orders → dest position id(s)`. Support initial, scale-in, partial close, full close, reversal. One source event ≠ one dest order forever. |
| §36 | Stale **entries** destroy XAUUSD edge. Reject entries that become too stale. |
| §37 | `PRICE_MOVED_TOO_FAR` / `QUOTE_STALE` / `SPREAD_TOO_WIDE` are **entry** guards (news). |
| §38 | Never `source 0.10 lots = dest OrderQty 0.10`. CLOSE/REDUCE qty comes from the **mapped dest position**, not a second pass through allocation. |
| §39 | Risk is final authority. Scoring/ML never size and never send. |
| §40 | `STOP_NEW_EXECUTION` blocks new copy; leaves dest book untouched. Flatten is `CLOSE_EXPOSURE` under a separate permission. |
| §41 | `REAL_COPY_EXECUTION_ENABLED=false` by default. Not a license for new exposure. Flatten exception: A48 / A49. |
| §42–§43 | No send against an unknown dest book. |
| §62 | MT5/QUOTE/TRADE/DB fail closed for **new** positions. Do not invent source trades. |
| §63 | Every intent has `expires_at` + `max_signal_age`. No 20-intent catch-up of stale **entries**. Close policy is separate. |
| §65 | Correlation caps are **Phase 2**. Do not refuse v1 copy for lack of a cluster graph. |
| §72.17 | New entries expire when stale. |
| §72.18 | Reduce/close ≠ open-more. |
| §72.19 | Manual overrides audited. |

### 1.3 What §64 is **not**

- Not a single `bool isClose`. Four classes, two families.
- Not “closes always approve.” Identity, over-close, flatten ownership, and unknown-state still block.
- Not permission to invent a dest position so a late source close has something to flatten.
- Not permission to skip persist-before-send on flatten or source-driven close.
- Not a substitute for `STOP_NEW_EXECUTION` vs `EMERGENCY_FLATTEN` (A48).

---

## 2. Current measured tree (2026-08-18)

Honest inventory. Do not treat the stub as the policy.

| Item | Path | Class vs §64 |
|---|---|---|
| Wire names | Architecture §64 four tokens | **LAW** |
| C# enum | `D:\Prop\src\Domain\Enums\CopyIntentAction.cs` (`OpenExposure=0` … `CloseExposure=3`) | **EXISTS_AND_GOOD** (labels only) |
| Intent field | `D:\Prop\src\Domain\Entities\CopyIntent.cs` `Action` | **EXISTS_NEEDS_REFACTOR** — no `linked_destination_position_id`, no `max_signal_age`, no family-specific expiry helper |
| Risk stub | `D:\Prop\src\Domain\Risk\RiskEngine.cs` `IsIncreasing` / `IsReducing` | **EXISTS_NEEDS_REFACTOR** — family split started; several guards still wrong (see §14) |
| Classifier | no `ExposureClassifier` / `ExposureAction` | **MISSING** |
| Dest link | no `source_destination_links` table/entity (A20 D26, A29 E15) | **MISSING** |
| Expiry helper | `D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs` single `maxSignalAge` | **EXISTS_NEEDS_REFACTOR** — one age for all classes |
| Sizing | `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` | **EXISTS_NEEDS_REFACTOR** — allocation path only; no dest-remaining close path |
| Reconstruction events | `TradeReconstructor` `ENTRY_IN` / `OUT` / `INOUT` (A21) | **EXISTS_AND_GOOD** as source input to the classifier |
| Tests | A27 `Risk.OpenVsCloseExposurePolicyTests` — **not in tree** | **MISSING** (A29 Q09) |
| Kill latch | `KillSwitchMode` exclusive enum + `KillSwitch.Mode` | **EXISTS — unsafe if treated as SoT** (A48) |

A29 Q09: *Separate OPEN vs CLOSE policy — MISSING (enum only — E11).* This document is the spec that closes that gap on **paper**. Product source stays untouched.

---

## 3. Classes and families

### 3.1 Canonical names

Persist and log the **architecture strings**. Map C# at the boundary.

| Wire / DB / reason-log | C# `CopyIntentAction` | Numeric |
|---|---|---|
| `OPEN_EXPOSURE` | `OpenExposure` | 0 |
| `INCREASE_EXPOSURE` | `IncreaseExposure` | 1 |
| `REDUCE_EXPOSURE` | `ReduceExposure` | 2 |
| `CLOSE_EXPOSURE` | `CloseExposure` | 3 |

Never persist `0/1/2/3` without a check constraint or a Postgres enum that uses the wire names. Dashboard and metrics use the wire names.

### 3.2 Meaning

| Class | Destination effect | Typical source (A21) | Family |
|---|---|---|---|
| `OPEN_EXPOSURE` | Create a **new** dest (or shadow) position. No open link for this `source_trade_id`. | First `ENTRY_IN` of a reconstructed lifecycle (`XAU_LIFECYCLE_OPENED`) | **OPEN** (strict) |
| `INCREASE_EXPOSURE` | Add size **same side** to an **already linked** dest position. | Further `ENTRY_IN` (`XAU_LIFECYCLE_INCREASED`, `was_scaled_in`) | **OPEN** (strict) |
| `REDUCE_EXPOSURE` | Reduce dest size; **remain open**. | `ENTRY_OUT` / `ENTRY_OUT_BY` with source remaining `> 0` (`XAU_LIFECYCLE_REDUCED`) | **CLOSE** (lenient) |
| `CLOSE_EXPOSURE` | Flatten the linked dest (or remaining remainder). | `ENTRY_OUT` / `OUT_BY` that completes the lifecycle; flatten run; remainder below min qty | **CLOSE** (lenient) |

### 3.3 Family predicates (normative)

```text
IsOpenFamily(action)  = action ∈ { OPEN_EXPOSURE, INCREASE_EXPOSURE }
IsCloseFamily(action) = action ∈ { REDUCE_EXPOSURE, CLOSE_EXPOSURE }
```

`INCREASE` is **not** “a little less strict than OPEN.” Quote age, spread, price-move, signal age, stop-new, recon-for-new-risk, and `REAL_COPY` all treat INCREASE like OPEN.

`INCREASE` **does** differ from `OPEN` on **book shape** (A23 §6.4):

- `MAX_OPEN_POSITIONS` rejects **new** `OPEN` only. INCREASE of an existing dest position does not consume a new slot.
- INCREASE **requires** a live dest link. OPEN **forbids** one.
- Martingale / averaging-down flags fire most often on INCREASE.

`REDUCE` vs `CLOSE`:

- Same lenient market-quality policy.
- Different quantity law (partial vs flatten remaining).
- `CLOSE` is the only class flatten runs emit (A48 §3.2).

### 3.4 Reversal is two intents (§35, A21 §7.6, A23 §2, A24 §4.1)

`ENTRY_INOUT` (or an unexpected opposite `ENTRY_IN` that reconstruction treats as reverse) is **never** one blended dest order.

```text
1. CLOSE_EXPOSURE   remaining linked dest qty     (CLOSE family)
2. OPEN_EXPOSURE    leftover opposite source qty  (OPEN family, new dest / new source_trade lifecycle_seq)
```

Order is mandatory. Persist both intents. Evaluate **close first**.

| Step 1 (CLOSE) | Step 2 (OPEN) | Dest result |
|---|---|---|
| Approved + filled / shadowed | Approved | Flat old side, new opposite |
| Approved | Rejected (stale / spread / stop-new / caps) | **Remain flat.** Do not keep the old side. |
| Rejected / `NO_DESTINATION_POSITION` | **Must not run** | No reverse open. If dest never existed, drop both (`CATCH_UP_SUPPRESSED` / `NO_DESTINATION_POSITION`). |
| `UNPRICED_CLOSE_HELD` (shadow only) | **Must not run** | Old shadow flagged unpriced; no opposite open. |

Same-size flatten is `ENTRY_OUT`, never `INOUT` (A21). Do not invent a reverse OPEN when leftover qty is `0`.

---

## 4. Classifier (runs before risk)

Classification is a **pure function** of (1) the reconstructed source event and (2) the durable dest/shadow link. It does **not** look at scores, quotes, or kill switches. Wrong class → wrong policy family → §64 violation even if every later guard is coded.

### 4.1 Inputs

| Input | Required | Notes |
|---|---|---|
| `source_broker_id`, `source_login` | yes | Identity |
| `source_trade_id` | yes | Reconstructed lifecycle id (A21 `lifecycle_seq` included) |
| `source_event_id` | yes | Deal ticket / reconstruction event |
| `canonical_symbol` | yes | Must be `XAUUSD` after mapping (A44). Unmapped → do not classify; reject `SYMBOL_UNMAPPED`. |
| `deal_entry` | yes | `ENTRY_IN=0`, `ENTRY_OUT=1`, `ENTRY_INOUT=2`, `ENTRY_OUT_BY=3` (A37) |
| `deal_action` | yes | `DEAL_BUY` / `DEAL_SELL` only. Balance/credit/commission → no intent. |
| `source_event_volume` | yes | Native → lots via `VolumeConverter` (A38). `0` → no intent. |
| `source_remaining_after` | yes | Lifecycle remaining after applying this deal |
| `source_remaining_before` | yes | For REDUCE vs CLOSE and fraction |
| `dest_link` | yes (nullable) | Current `source_destination_links` / shadow analog for this `source_trade_id` |
| `dest_remaining_qty` | if link | Destination units, not source lots |
| `dest_side` | if link | Must match source lifecycle side for OPEN-family add / CLOSE-family reduce |
| `flatten_owner` | yes | Whether an `EMERGENCY_FLATTEN` run already queued this dest id |

### 4.2 Algorithm (normative)

```text
# Non-trading / non-XAU
if !is_trading_deal or canonical != XAUUSD:  NO_INTENT

# Flatten already owns this dest position
if dest_link exists AND flatten_owner(dest_position_id):
    if close-family source event:  COALESCE (no second intent; attach source_event_id to flatten target)
    if open-family source event:   still classify, risk will REJECT EMERGENCY_FLATTEN_ACTIVE
    # do not emit a parallel source-driven CLOSE

# Reversal
if deal_entry == ENTRY_INOUT OR reconstruction.emits_reverse:
    leftover = source_event_volume - source_remaining_before   # A21: volume - closed
    emit CLOSE_EXPOSURE (qty_hint = dest_remaining if link else 0)
    if leftover > 0:
        emit OPEN_EXPOSURE (new source_trade_id / lifecycle_seq, opposite side)
    stop

# Entry
if deal_entry == ENTRY_IN:
    if no dest_link OR dest_remaining == 0:
        if source_remaining_before == 0:  OPEN_EXPOSURE
        else:
            # Source scaled in, dest never opened (prior OPEN rejected / expired)
            NO_INTENT + reason OPEN_NEVER_ACCEPTED     # default; do not chase
            # Config promote_orphan_increase_to_open = false (MUST stay false in v1)
    if dest_side == source_side AND dest_remaining > 0:
        INCREASE_EXPOSURE
    if dest_side != source_side AND dest_remaining > 0:
        fail POSITION_INCONSISTENT                     # should have been INOUT

# Exit
if deal_entry in { ENTRY_OUT, ENTRY_OUT_BY }:
    if no dest_link OR dest_remaining == 0:
        CLOSE_EXPOSURE + terminal NO_DESTINATION_POSITION   # persist intent, no send
    mapped_close = MapCloseQty(source_event_volume, source_remaining_before, dest_remaining)
    if mapped_close >= dest_remaining - dest_qty_epsilon:
        CLOSE_EXPOSURE
    else:
        REDUCE_EXPOSURE
```

`MapCloseQty` is **not** `source_lots`. See §8.

### 4.3 Orphan and duplicate rules (fail closed)

| Situation | Action | Code |
|---|---|---|
| Source OPEN, dest link already open for same `source_trade_id` | Idempotent no-op of the **same** `source_event_id`; else `POSITION_INCONSISTENT` | `DUPLICATE_OPEN` / `POSITION_INCONSISTENT` |
| Source INCREASE, dest never accepted | Do **not** promote to OPEN in v1 | `OPEN_NEVER_ACCEPTED` |
| Source REDUCE/CLOSE, dest never accepted | Persist, terminal, no send, no synthetic open | `NO_DESTINATION_POSITION` |
| Source CLOSE qty maps above dest remaining | Clip to dest remaining; class becomes `CLOSE_EXPOSURE` | `QTY_CLIPPED` (info) |
| Source REDUCE would leave dest `< min_qty` | Promote class to `CLOSE_EXPOSURE` (flatten remainder) | `REMAINDER_FLATTENED` (info) |
| Two intents same `(broker, login, trade, event, action)` | Unique key (A20) — second insert is a no-op | — |

### 4.4 One source event, N dest orders

§35: do not assume 1:1. A single `ENTRY_IN` may still be **one** `OPEN_EXPOSURE` intent. Slice-on-partial-fill is an **execution** concern, not a second class. Reversal is the only classifier fan-out (2 intents).

---

## 5. Guard matrix (normative)

`B` = blocking reject / no send.  
`A` = allowed (may still fail identity).  
`Q` = quality flag on fill / decision, **not** a blocker.  
`W` = pricing waterfall (A24 §8.3 analog; live: wait/retry under **close** expiry, never invent px).  
`N/A` = check does not apply.

| # | Check | OPEN | INCREASE | REDUCE | CLOSE | Notes |
|---|---|---|---|---|---|---|
| G01 | Database available | B | B | B | B | §62. No real send from RAM. |
| G02 | Intent identity complete | B | B | B | B | `INTENT_INCOMPLETE` |
| G03 | Canonical XAUUSD + dest instrument mapped | B | B | B | B | `SYMBOL_UNMAPPED` |
| G04 | `REAL_COPY_EXECUTION_ENABLED` (live NOS) | B | B | B* | B* | *Source-driven live reduce/close also require the flag in v1. Flatten CLOSE **may** send with flag false (A48 §3.3, A49). Shadow never sends. |
| G05 | `STOP_NEW_EXECUTION` | B | B | A† | A† | †`allow_risk_reduction_while_stop_new` default **true** (A48 §3.1). |
| G06 | Flatten run owns **this** dest id | B | B | B | B | Coalesce; flatten is exclusive closer. |
| G07 | Flatten run active (other ids) | B | B | A | A | Open family always blocked while flatten `active` / `confirm_pending`. |
| G08 | `READY_FOR_EXECUTION` | B | B | A‡ | A‡ | ‡Close family may send only if **this** dest id is known and has **no** `EXECUTION_STATE_UNKNOWN`. Global book not-ready still blocks OPEN family. |
| G09 | Unresolved unknown on **this** dest / clOrd | B | B | B | B | §34. Do not “fix” with another NOS. |
| G10 | TRADE session down | B (expire) | B (expire) | W | W | Do not enqueue unbounded OPEN. CLOSE persists; retry until `expires_at_close`. |
| G11 | QUOTE missing / session down | B | B | W | W | OPEN: `QUOTE_UNAVAILABLE`. CLOSE: last-quote waterfall / unpriced-hold (shadow) or wait (live). |
| G12 | `quote_age > max_quote_age_open` | B | B | Q / W | Q / W | Separate `max_quote_age_close`. |
| G13 | `spread > max_allowed_spread` | B | B | Q | Q | News control is an **entry** control (§37). |
| G14 | Adverse `PRICE_MOVED_TOO_FAR` | B | B | Q | Q | Signed adverse vs taker touch (§6.3). Favorable move does not reject OPEN. |
| G15 | `max_slippage` vs expected dest px | B | B | Q | Q | |
| G16 | `signal_age > max_signal_age_open` | B | B | A | A | CLOSE uses `max_signal_age_close` (much larger). |
| G17 | `now >= expires_at` | B | B | B§ | B§ | §Close `expires_at` is long enough that restart cannot strand dest vs a persisted source close. Missing expiry is a defect, not “never expire.” |
| G18 | Source collector stale / MT5 down | B | B | A‖ | A‖ | ‖Process **already persisted** close events. Do not invent new source closes. |
| G19 | Trader not `LIVE` (live path) | B | B | A | A | Residual dest risk must still be reducible. Shadow: trader must be shadow-eligible on OPEN only (A24). |
| G20 | `PAUSED` / `RISK_BLOCKED` / `DISQUALIFIED` | B | B | A | A | Pause stops **new** copy, not dest exits. |
| G21 | `MAX_LOSS_PER_TRADER` | B + `PAUSE_TRADER` | B + `PAUSE_TRADER` | A | A | Do **not** trap a losing dest position. |
| G22 | `MAX_DAILY_EXECUTION_LOSS` | B + `GLOBAL_STOP` | B + `GLOBAL_STOP` | A | A | `GLOBAL_STOP` engages **stop-new only** (A48). Flatten remains a human action. |
| G23 | `MAX_PORTFOLIO_DRAWDOWN` | B + `GLOBAL_STOP` | B + `GLOBAL_STOP` | A | A | Same. |
| G24 | `MAX_OPEN_POSITIONS` | B | N/A | N/A | N/A | Increase of existing dest is not a new slot (A23 §6.4). |
| G25 | `MAX_POSITION_QUANTITY` | B or `REDUCE_SIZE` | B or `REDUCE_SIZE` | N/A | N/A | After add: `dest_remaining + approved <= max`. |
| G26 | `MAX_XAU_GROSS` | B or `REDUCE_SIZE` | B or `REDUCE_SIZE` | N/A | N/A | REDUCE/CLOSE **never** rejected for caps. |
| G27 | `MAX_XAU_NET` | B or `REDUCE_SIZE` | B or `REDUCE_SIZE` | N/A | N/A | |
| G28 | `MAX_MARGIN_USAGE` / insufficient margin | B or `REDUCE_SIZE` | B or `REDUCE_SIZE` | N/A | N/A | Closing frees margin; do not block. |
| G29 | Martingale flag | B + optional pause | B + optional pause | N/A | N/A | Flag pipeline is input; engine does not re-reconstruct. |
| G30 | Abnormal sizing / averaging-down | B | B | N/A | N/A | INCREASE is the usual fire. |
| G31 | Dest link present, same side, remaining `> 0` | B if present | **required** | **required** | **required** | OPEN with an open link = inconsistent / duplicate. |
| G32 | Sizing `qty < min` after step | B `QTY_BELOW_MIN` | B `QTY_BELOW_MIN` | Flatten remainder → CLOSE | Flatten remainder | A24 §9.1 |
| G33 | Concentration / cluster cap | N/A v1 | N/A v1 | N/A | N/A | §65 Phase 2. Reserved code `CONCENTRATION_CAP`. |
| G34 | Score / ML confidence | advisory | advisory | N/A | N/A | Never bypasses a B cell. High score does not open through a stale quote. |

`REDUCE_SIZE` is legal **only** in the OPEN family, and only when the reduced dest qty still satisfies min/step and is still the same class (do not flip OPEN into CLOSE here).

---

## 6. OPEN family policy (strict)

Applies to `OPEN_EXPOSURE` and `INCREASE_EXPOSURE` only.

### 6.1 Hard preconditions (first failure wins)

1. G01–G03. Incomplete intent → `INTENT_INCOMPLETE`.
2. Live path: `REAL_COPY_EXECUTION_ENABLED=true` **and** risk-engine healthy (A23, A49 conjunction). Else no `AllowFixSend`. Shadow: `SHADOW_COPY_ENABLED` (A24); never FIX.
3. G05–G07. Stop-new / flatten-active → `STOP_NEW_EXECUTION` / `EMERGENCY_FLATTEN_ACTIVE`. Decision `REJECT` or engine-raised `GLOBAL_STOP`.
4. G08–G09. Not ready or unknown → `RECONCILIATION_BLOCK` / `EXECUTION_STATE_UNKNOWN`.
5. G10–G11. TRADE/QUOTE down → mark, **expire**, do not backlog (§62, §63).
6. G19–G20. Live trader must be `LIVE` (or an explicit live-candidate gate that is **off** until Phase 8). `TRADER_NOT_LIVE` / `TRADER_PAUSED` / `TRADER_RISK_BLOCKED`.
7. G31. OPEN: no open dest link. INCREASE: open dest link, same side, remaining `> 0`.
8. G16–G17. `signal_age <= max_signal_age_open` **and** `now < expires_at`. Else `SIGNAL_STALE` / `INTENT_EXPIRED`.
9. G12–G15. Fresh quote, spread, signed adverse move, slippage.
10. §8 OPEN sizing. `qty >= min` and on step. Else `QTY_BELOW_MIN`.
11. G24–G30. Caps / martingale / abnormal size. May `REDUCE_SIZE` down to the binding cap; `REJECT` if remainder `< min`.

Shadow repeats 9 after `simulated_execution_delay` against a **new** quote (A24 §7.4). Live FIX worker re-checks the same snapshot immediately before send (A48 §7). A quote that aged between decision and send is a new `QUOTE_STALE`, not a send.

### 6.2 Taker touch (entry)

| New / add direction | Expected dest price |
|---|---|
| Long (buy to open / increase) | dest **ask** |
| Short (sell to open / increase) | dest **bid** |

Mid is not the fill and is not the default guard (A24 §7.3).

### 6.3 Signed adverse move

```text
# Open / increase long: worse if dest ask rose
adverse_vs_expected = dest_ask_now - expected_ask

# Open / increase short: worse if dest bid fell
adverse_vs_expected = expected_bid - dest_bid_now
```

Reject `PRICE_MOVED_TOO_FAR` if `adverse_vs_expected > max_adverse_move_open`.  
Favorable move does **not** reject. Unsigned `|mid - source|` is **not** the v1 rule (the current stub uses unsigned mid — §14).

Optional second bound vs `source_price` (`max_adverse_vs_source_open`) may be enabled in config. Default off until measured.

### 6.4 Catch-up of opens is forbidden (§63)

If TRADE/QUOTE was down (architecture example: 3 minutes) while sources opened 20 trades:

```text
Do NOT reconnect and fire 20 NewOrderSingle / 20 shadow opens.
```

On reconnect:

- Expire every OPEN-family intent with `now >= expires_at` or `signal_age > max_signal_age_open`.
- Clearing `STOP_NEW_EXECUTION` does **not** resurrect expired intents (A48).
- Do not open-then-immediately-close a source trade that both opened and closed in the gap **if no dest/shadow position exists** (`CATCH_UP_SUPPRESSED` + `NO_DESTINATION_POSITION`).
- Never “make the dest book match source history.”

### 6.5 OPEN-family reject codes (closed set)

```text
INTENT_INCOMPLETE
SYMBOL_UNMAPPED
QUOTE_UNAVAILABLE
QUOTE_STALE
QUOTE_INVALID
SPREAD_TOO_WIDE
PRICE_MOVED_TOO_FAR
MAX_SLIPPAGE_EXCEEDED
SIGNAL_STALE
INTENT_EXPIRED
QTY_BELOW_MIN
QTY_STEP_INVALID
TRADER_NOT_LIVE
TRADER_PAUSED
TRADER_RISK_BLOCKED
TRADER_NOT_SHADOW_ELIGIBLE      # shadow path
STOP_NEW_EXECUTION
EMERGENCY_FLATTEN_ACTIVE
REAL_EXECUTION_DISABLED
QUOTE_FIX_UNAVAILABLE
TRADE_FIX_UNAVAILABLE
SOURCE_STALE
RECONCILIATION_BLOCK
EXECUTION_STATE_UNKNOWN
DATABASE_UNAVAILABLE
MAX_LOSS_PER_TRADER
MAX_DAILY_EXECUTION_LOSS
MAX_PORTFOLIO_DRAWDOWN
MAX_OPEN_POSITIONS              # OPEN only
MAX_POSITION_QUANTITY
MAX_XAU_GROSS
MAX_XAU_NET
MAX_MARGIN_USAGE
INSUFFICIENT_MARGIN
MARTINGALE_BLOCK
ABNORMAL_SIZING_BLOCK
POSITION_INCONSISTENT
DUPLICATE_OPEN
OPEN_NEVER_ACCEPTED             # INCREASE without dest
CATCH_UP_SUPPRESSED
SHADOW_OPENS_STOPPED            # shadow analog
```

`REDUCE_SIZE` informational: `SIZE_REDUCED_TO_LIMIT` + `binding_cap`.

---

## 7. CLOSE family policy (lenient, risk-reduction)

Applies to `REDUCE_EXPOSURE` and `CLOSE_EXPOSURE` only.

Closing an **already copied** dest/shadow position is risk-reduction (§64). It **must not** share OPEN thresholds.

### 7.1 What CLOSE still requires (non-negotiable)

1. A linked dest (live) or shadow position with remaining qty `> 0`. Else terminal `NO_DESTINATION_POSITION` / `NO_SHADOW_POSITION`. **Do not invent** a short-lived open to have something to close. That is how catch-up of never-copied entries stays inert.
2. Quantity after §8 **≤ remaining dest qty**. Excess clipped (`QTY_CLIPPED`); never flip by over-close.
3. Same dest side as the link. Opposite side → `POSITION_INCONSISTENT`.
4. No unresolved `EXECUTION_STATE_UNKNOWN` on **this** dest position / in-flight close.
5. Flatten does not already own this dest id (A48). If it does: coalesce, do not second-send.
6. Live send still needs TRADE logon + lease + persist-before-send + unique `cl_ord_id`.
7. A destination price **if one exists**. CLOSE does not invent ticks (A24 §8.1).

### 7.2 Guards that must **not** block CLOSE

These **must not** reject a reduce/close of an existing mapped position:

- `SPREAD_TOO_WIDE` (OPEN threshold)
- `PRICE_MOVED_TOO_FAR` (OPEN threshold)
- `MAX_SLIPPAGE_EXCEEDED` (OPEN threshold)
- `SIGNAL_STALE` at `max_signal_age_open`
- `STOP_NEW_EXECUTION` (default policy)
- `MAX_*` book / margin / open-position caps
- `MARTINGALE_BLOCK` / `ABNORMAL_SIZING_BLOCK`
- `MAX_LOSS_PER_TRADER` / daily loss / portfolio DD *(these engage stop-new; they do not freeze exits)*
- Trader `PAUSED` / `RISK_BLOCKED` / not `LIVE`
- QUOTE session currently down **if** a last usable quote exists (waterfall)

They **may** be recorded as fill-quality flags / telemetry (`quote_quality=STALE`, `spread_at_close`, `signal_age`). Ops must see them. They are not blockers.

Operator override: `allow_risk_reduction_while_stop_new=false` makes G05 blocking for CLOSE family too. Default **true**. Changing it is an audited RiskManager+ action (A48).

### 7.3 Close pricing waterfall

**Shadow (A24 §8.3) — binding**

1. QUOTE up and `quote_age <= max_quote_age_close` → fill at current taker touch. Quality `LIVE`.
2. Else last usable quote with `quote_age <= max_quote_age_close_stale_fallback` → fill at that touch. Quality `STALE_QUOTE`. Persist age.
3. Else **do not invent a price**. Position → `SOURCE_CLOSED_UNPRICED` / `SOURCE_REDUCED_UNPRICED`. Freeze remaining qty. `shadow_pnl.unrealized_quality=UNPRICED`. No fabricated `shadow_copy_fill`.

`max_quote_age_close` ≫ `max_quote_age_open`.  
`max_quote_age_close_stale_fallback` is larger still. Both configurable. Do not hardcode production milliseconds in this spec.

**Live FIX**

- Never invent a fill. The venue prints the px.
- If TRADE is down: persist the CLOSE/REDUCE `execution_intent` as `not_sent`, retry under **close** expiry, do not convert it to an OPEN-family expire.
- If QUOTE is down but TRADE is up: still allowed to send a marketable close of a **known** dest id (risk reduction). Expected px = last quote if any, else `expected_price` left null and `max_slippage` **not** applied as an entry reject.
- Flatten CLOSE skips entry quote/move guards entirely (A48 §7) but still requires TRADE + known dest id.

### 7.4 Taker touch (exit)

| Close direction | Expected dest price (when quoted) |
|---|---|
| Close / reduce **long** (sell) | dest **bid** |
| Close / reduce **short** (buy) | dest **ask** |

### 7.5 Catch-up of closes is **required**

After QUOTE/TRADE/process restart, pending CLOSE-family intents against **existing** dest/shadow positions **must** be processed. Leaving a dest long after the source flattened falsifies shadow P&L (promotion gate, §23) and leaves live residual risk.

Rules:

- Process CLOSE-family intents **before** any surviving OPEN-family intents in the same replay batch.
- Source opened **and** fully closed during the outage **and** no dest/shadow exists: drop both (`CATCH_UP_SUPPRESSED` + `NO_DESTINATION_POSITION`).
- Dest/shadow exists and source fully closed during the outage: close via §7.3 even if `signal_age > max_signal_age_open`.
- Do not build an unbounded queue while TRADE is down (§62). Persist, retry with backoff, expire only at `expires_at_close` (hours, not seconds). Expiry of a close of an **open dest** is an **ops alert** (`CLOSE_EXPIRY_STRANDED`), not a silent drop.

### 7.6 Partial close

`REDUCE_EXPOSURE` fills `min(mapped_qty, dest_remaining)` and leaves the dest open.

- Realized P&L on the closed slice only.
- Remaining dest VWAP / average-cost **unchanged**.
- Do not reopen.
- Do not emit a second `OPEN`.
- If leftover dest `< min_qty`, promote to `CLOSE_EXPOSURE` and flatten remainder (G32).

### 7.7 CLOSE-family terminal / info codes

```text
NO_DESTINATION_POSITION     # live; not an error if OPEN was never accepted
NO_SHADOW_POSITION          # shadow analog
QTY_CLIPPED                 # informational
REMAINDER_FLATTENED         # informational promote REDUCE → CLOSE
UNPRICED_CLOSE_HELD         # shadow; no fill
POSITION_INCONSISTENT
EXECUTION_STATE_UNKNOWN     # this dest only
MAPPING_MISSING             # link row required but absent
FLATTEN_OWNS_POSITION       # coalesced
CLOSE_EXPIRY_STRANDED       # alert; dest still open
TRADE_FIX_UNAVAILABLE       # live; intent held, not converted to OPEN expire
```

Spread / price-move / open-staleness / stop-new / book caps are **not** in this reject set.

---

## 8. Quantity law (by class)

Authority: §38, A23 §7, A24 §9, A38. **Never** `source lots = dest OrderQty`.

### 8.1 OPEN / INCREASE — allocation path

```text
source volume (native → lots via VolumeConverter)
    ↓
canonical notional / risk
    ↓
min(suggested_allocation, remaining OPEN-family caps)
    ↓
destination instrument quantity
    ↓
floor to destination step
    ↓
enforce dest min / max
    ↓
re-check G25–G28  →  APPROVE | REDUCE_SIZE | REJECT
```

INCREASE proposed dest qty is the **add**, not the new total. After approve:

```text
dest_remaining + approved_add  ≤  max_position_quantity
current_gross + approved_add   ≤  max_xau_gross
```

Net check uses signed dest book, not `abs(net) + add` blindly when the add **reduces** net (a short add against a long book). Implementation must use:

```text
projected_net   = current_net + signed(approved_add, dest_side)
projected_gross = current_gross + approved_add
```

Reject / reduce if `|projected_net| > max_net` or `projected_gross > max_gross`.

If step/caps produce `qty < min` → `REJECT` / `QTY_BELOW_MIN`. Do not send a non-tradable NOS.

### 8.2 REDUCE / CLOSE — dest-remaining path

**Do not** re-run source lots through allocation.

```text
# Fraction of the source remaining that this exit removes
source_frac = source_event_volume / source_remaining_before     # (0, 1]
source_frac = min(1, source_frac)

raw_dest    = dest_remaining * source_frac
dest_qty    = floor_to_step(raw_dest, dest_step)

if action == CLOSE_EXPOSURE OR dest_qty >= dest_remaining - eps OR (dest_remaining - dest_qty) < min_qty:
    dest_qty = dest_remaining          # flatten remaining (venue min may require this)
    action   = CLOSE_EXPOSURE

if dest_qty <= 0:
    if dest_remaining > 0 AND dest_remaining < min_qty:
        dest_qty = dest_remaining      # flatten remainder
        action   = CLOSE_EXPOSURE
    else:
        REJECT / QTY_BELOW_MIN         # should be rare; alert
```

`CLOSE_EXPOSURE` from flatten: `dest_qty = dest_remaining` exactly. Ignore source lots entirely (A48 §3.2).

### 8.3 Side of the destination order

| Class | Dest position | NOS side (FIX) |
|---|---|---|
| `OPEN_EXPOSURE` long | none | Buy |
| `OPEN_EXPOSURE` short | none | Sell |
| `INCREASE_EXPOSURE` long | long | Buy |
| `INCREASE_EXPOSURE` short | short | Sell |
| `REDUCE_EXPOSURE` / `CLOSE_EXPOSURE` long | long | Sell |
| `REDUCE_EXPOSURE` / `CLOSE_EXPOSURE` short | short | Buy |

cTrader position id on reduce/close comes from `destination_positions` / FIX position reports, **not** from guessing tag 55 (A25, §72.13).

---

## 9. Timing and expiry (two clocks)

Authority: §36, §63, A23 §6.2, A24 §6.

Every `CopyIntent` **must** carry:

```text
source_event_time
collector_receive_time
decision_time                 # set at eval
expires_at                    # absolute, family-specific default offset
max_signal_age                # duration, family-specific
```

Missing `expires_at` is a defect (`INTENT_INCOMPLETE`), not “never expire.”

```text
signal_age = decision_time - source_event_time
```

| Knob | OPEN family | CLOSE family |
|---|---|---|
| `max_signal_age_*` | Tight (entry edge). Architecture motivation: seconds, not minutes. | Loose (hours). Restart / TRADE gap must not strand dest. |
| `expires_at` offset from `source_event_time` | Short. Stale entries die. | Long. ≥ max expected TRADE outage + restart. |
| Quote age | `max_quote_age_open` | `max_quote_age_close` + stale fallback |
| 3-minute FIX gap, 20 source opens | All 20 expire if over age | Existing dest closes still process |

Per-intent `max_signal_age` may be tighter than the global family cap; the **stricter** bound wins (A23 §6.2). A CLOSE intent must not be created with an OPEN-family age.

`CopyIntentExpiry` today takes one `maxSignalAge` (`D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs`). Later implementation must be family-aware. This spec does not change that file.

Measure the §36 latency chain on **every** decision, including rejects.

---

## 10. Kill switch, flags, and send conjunction

Do not conflate four different levers.

| Lever | OPEN family | CLOSE family (source-driven) | Flatten `CLOSE_EXPOSURE` |
|---|---|---|---|
| `STOP_NEW_EXECUTION` | Block | Allow (default) | Allowed (flatten is not “new copy”) |
| `EMERGENCY_FLATTEN` active | Block | Block if flatten owns that dest id | The flatten orders themselves |
| `REAL_COPY_EXECUTION_ENABLED=false` | No NOS | No NOS in v1 source-driven path | May send reducing NOS (A48/A49) |
| `CTRADER_FIX_TRADE_SESSION_ENABLED=false` | No NOS | No NOS | No NOS |
| Trader pause | Block | Allow | N/A (account-level flatten) |
| `GLOBAL_STOP` (engine) | Engages stop-new; block | Allow (exit) | Not auto-fired from daily loss (A48) |

Live OPEN-family send conjunction (A49): all true, else no socket write.

```text
CTRADER_FIX_ENABLED
CTRADER_FIX_TRADE_SESSION_ENABLED
REAL_COPY_EXECUTION_ENABLED
STOP_NEW_EXECUTION = off
flatten not blocking this class
READY_FOR_EXECUTION
TRADE lease owned
risk decision APPROVE | REDUCE_SIZE
execution_intent persisted not_sent
unique cl_ord_id
this intent not expired
quote/spread/move still pass at pre-send re-check
```

CLOSE-family source-driven conjunction: same **except** stop-new may be on, quote/move re-check is quality-only, and trader pause does not block.

Flatten conjunction: A48 §7. Skip quote-age entry, price-move entry, `REAL_COPY`. Require TRADE + lease + known dest id + no unknown on that id.

`AllowFixSend` on a CLOSE-family approval **must not** require `KillSwitchMode.None`. Stop-new on + close approved ⇒ `AllowFixSend=true` when the rest of the close conjunction holds. The current stub requires `KillSwitch == None` for every class — that is a §64 / A48 bug (§14).

---

## 11. Evaluation order (deterministic)

Fail closed on the first **blocking** check. Order is fixed so `OpenVsCloseExposurePolicyTests` is stable. Extends A23 §5 with an explicit classify step.

```text
0.  Classify action (this document §4). Persist CopyIntent with that action.
    Reversal → two intents; evaluate CLOSE intent before OPEN intent.

1.  Database available.

2.  Feature flags (live vs shadow path). Shadow never sets AllowFixSend.

3.  Kill / flatten snapshot (G05–G07).
    OPEN family: stop-new and flatten-active are blocking.
    CLOSE family: stop-new allowed; flatten-owns-this-id coalesces.

4.  Identity / mapping (G02, G03, G31).
    CLOSE family without a dest link → terminal NO_DESTINATION_POSITION (not an OPEN).

5.  Reconciliation / unknown (G08–G09).
    OPEN family: global READY required.
    CLOSE family: this dest id known and not unknown.

6.  Venue sessions (G10–G11).
    OPEN family: QUOTE+TRADE required; else expire.
    CLOSE family: TRADE for live send; QUOTE waterfall for price.

7.  Trader eligibility (G19–G20) — OPEN family only as a blocker.

8.  Expiry / signal age — family clocks (§9).

9.  Quote age / spread / signed move / slippage — OPEN family blocking; CLOSE family flags.

10. Sizing (§8) — allocation vs dest-remaining.

11. Book / account caps (G24–G28) — OPEN family only.

12. Martingale / abnormal (G29–G30) — OPEN family only.

13. Persist risk_decision. On APPROVE / REDUCE_SIZE persist execution_intent
    (live) or shadow order (shadow) BEFORE any send / simulated fill.

14. Pre-send / pre-fill re-check of the same family policy.
```

ML unavailable: do not promote traders; do not skip this order (§62).

---

## 12. Persistence, idempotency, correlation

### 12.1 `copy_intents` (A20)

Unique:

```text
(source_broker_id, source_login, source_trade_id, source_event_id, action)
```

A reversal therefore stores **two** rows (`CLOSE_EXPOSURE` and `OPEN_EXPOSURE`) for the same `source_event_id`. That is required, not a uniqueness bug.

Required columns beyond today’s entity:

```text
action                      # four wire names
side                        # dest NOS side
linked_destination_position_id   NULL on OPEN; required on INCREASE/REDUCE/CLOSE
source_event_id
max_signal_age
expires_at
collector_receive_time
flatten_run_id              NULL unless flatten-emitted CLOSE
parent_copy_intent_id       OPEN-of-reversal points at the CLOSE sibling
```

Status (A26): `PENDING | APPROVED | REDUCED | REJECTED | EXPIRED | EXECUTED | SHADOWED`. Terminal `NO_DESTINATION_POSITION` uses `REJECTED` + that reason (not `EXPIRED`).

### 12.2 `risk_decisions`

Every evaluation appends `(copy_intent_id, decision_seq)` (A20). Carry `exposure_class`, `primary_reason`, `approved_quantity`, `allow_fix_send`, quote/signal ages, spread, adverse move.

`AllowFixSend` is computed **per family** (§10). `ApprovedQuantity=0` is illegal on `REDUCE_SIZE` — that outcome must carry a positive dest qty on step.

### 12.3 `execution_intents` / shadow

Carry `exposure_class`. FIX worker and shadow engine **re-read** the class; they do not re-classify from source lots.

### 12.4 `source_destination_links` (§35)

```text
source reconstructed trade
        ↓
destination execution orders  (or shadow_copy_orders)
        ↓
destination cTrader position ID(s)  (or shadow_position id)
```

INCREASE / REDUCE / CLOSE without a link is a classifier/risk reject, not a best-effort guess.

---

## 13. Worked examples

Examples use dest units already normalized. Thresholds are illustrative **relationships**, not production constants.

Shared snapshot:

```text
quote_age            > max_quote_age_open
                     < max_quote_age_close
spread               > max_allowed_spread
signal_age           > max_signal_age_open
                     < max_signal_age_close
STOP_NEW_EXECUTION   = on
trader               = PAUSED
dest_remaining       = 1.20  (long, linked)
```

| # | Source event | Class | Decision | Why |
|---|---|---|---|---|
| E1 | First `ENTRY_IN` 1.00, no dest | `OPEN_EXPOSURE` | `REJECT` `STOP_NEW_EXECUTION` (also quote/spread/signal/pause) | First blocking code in §11 order is stop-new |
| E2 | Same snapshot, dest 1.20 long, `ENTRY_OUT` full | `CLOSE_EXPOSURE` | `APPROVE` qty `1.20` | Stop-new, pause, spread, stale quote, stale signal are **not** blockers |
| E3 | `ENTRY_IN` scale-in 0.50, dest 1.20 long | `INCREASE_EXPOSURE` | `REJECT` `STOP_NEW_EXECUTION` | Same family as OPEN |
| E4 | `ENTRY_OUT` 0.40 of source 1.20, dest 1.20 | `REDUCE_EXPOSURE` | `APPROVE` dest fraction (not 0.40 lots) | Partial; dest remains open |
| E5 | `ENTRY_INOUT` sell 1.80, dest long 1.20 | `CLOSE_EXPOSURE` 1.20 then `OPEN_EXPOSURE` leftover short | Close **APPROVE**; Open **REJECT** (stop-new + stale) | Remain **flat**. No leftover short dest |
| E6 | `ENTRY_OUT` full, **no** dest link | `CLOSE_EXPOSURE` | Terminal `NO_DESTINATION_POSITION` | No synthetic open |
| E7 | `ENTRY_IN` scale-in, **no** dest link | no INCREASE | `OPEN_NEVER_ACCEPTED` | v1 does not chase |
| E8 | Daily loss breached, dest 1.20 long, source full close | `CLOSE_EXPOSURE` | `APPROVE` | Daily-loss raises stop-new; exits still allowed |
| E9 | Same daily-loss, source new open | `OPEN_EXPOSURE` | `REJECT` / `GLOBAL_STOP` | |
| E10 | Flatten active owns dest 1.20; source close arrives | (coalesced) | No second NOS | Flatten exclusive closer |
| E11 | TRADE down 3 min, 20 source opens, 3 source closes of **existing** dest | 20 OPEN expire; 3 CLOSE process | §6.4 / §7.5 | |
| E12 | OPEN sizing rounds to 0 | `OPEN_EXPOSURE` | `REJECT` `QTY_BELOW_MIN` | |
| E13 | REDUCE leaves dest 0.4 of `min=1.0` | promoted `CLOSE_EXPOSURE` | Flatten remainder | G32 |
| E14 | High ML score, rotten quote | `OPEN_EXPOSURE` | `REJECT` `QUOTE_STALE` | §72.15 |
| E15 | Shadow CLOSE, no quote even on fallback | `CLOSE_EXPOSURE` | `UNPRICED_CLOSE_HELD` | No fake fill |

---

## 14. Delta vs current `RiskEngine` stub

File: `D:\Prop\src\Domain\Risk\RiskEngine.cs`. **Not modified** by this task. Recorded so a later coder does not ship the stub as §64.

| Stub behavior (today) | This policy |
|---|---|
| `IsIncreasing` / `IsReducing` exist | Keep the split; rename mentally to OpenFamily / CloseFamily |
| `STOP_NEW` / flatten-blocks-new only on increasing | Correct for family. Flatten-owns-id coalesce is **missing**. |
| Unreconciled / unhealthy venue only block increasing | Close of a **known** dest may proceed; unknown-on-this-id must still block (not implemented). |
| Quote missing / stale / spread / unsigned mid-move / signal age only on increasing | Correct **direction**. Must become **signed adverse** + family clocks. Mid is wrong. |
| `TraderRealizedLoss`, daily PnL, portfolio DD apply to **all** classes (lines 117–124) | **Defect.** Would freeze exits on a losing day. CLOSE family must pass. |
| `MAX_OPEN_POSITIONS` on all increasing | **Defect for INCREASE.** OPEN only. |
| `MAX_XAU_NET` returns `ReduceSize` via `Reject()` ⇒ `ApprovedQuantity=0` | **Defect.** `REDUCE_SIZE` must keep a positive stepped qty. |
| Close family always `APPROVE` + `RISK_REDUCTION` if it reaches the branch | Too loose: no mapping check, no flatten coalesce, no unknown-state check, no qty clip. |
| `AllowFixSend` requires `KillSwitch == None` | **Defect.** Stop-new on must still allow CLOSE-family send (default). |
| `RealExecutionEnabled==false` special-cases only `CloseExposure` in a comment | Flatten exception is A48, not a silent comment. Source-driven live close still needs the flag in v1. |
| Single `MaxSourceSignalAge` | Two clocks. |
| No dest link / no classifier | Required. |

Until those deltas are fixed in a later, reviewed change, A29 Q09 remains **MISSING**.

---

## 15. Tests this policy owns

A27 already named the lock: `Risk.OpenVsCloseExposurePolicyTests`. Expand to the following **must-prove** list. Do not implement tests in this task.

| Test | Must prove |
|---|---|
| Same rotten snapshot, OPEN rejects, CLOSE of linked dest approves | Core §64 |
| INCREASE uses OPEN reject set (quote, spread, move, signal, stop-new) | INCREASE ∈ OPEN family |
| `MAX_OPEN_POSITIONS` rejects OPEN, allows INCREASE of existing dest | G24 |
| Daily loss / trader max loss / DD reject OPEN, approve CLOSE | G21–G23 |
| Pause / not-LIVE reject OPEN, approve CLOSE of linked dest | G19–G20 |
| Stop-new rejects OPEN/INCREASE; dest book unchanged; CLOSE still `AllowFixSend` | A48 + §10 |
| Flatten-active rejects OPEN; source CLOSE of owned dest coalesces (no second NOS) | A48 §7 |
| Reversal: CLOSE then OPEN; OPEN reject leaves dest flat | §3.4 E5 |
| No dest link + source close → `NO_DESTINATION_POSITION`, zero FIX | §4.3 |
| Scale-in without dest → `OPEN_NEVER_ACCEPTED`, not a late OPEN | §4.3 |
| 3-minute gap, 20 stale opens expire; existing dest closes process | §63 |
| Open-then-close in gap, no dest → both suppressed | A24 §8.4 |
| `QTY_BELOW_MIN` on OPEN; remainder flatten on REDUCE | §8 |
| CLOSE qty = dest remaining, not source lots | §8.2 |
| High score cannot pass a stale OPEN quote | §72.15 |
| `REDUCE_SIZE` never returns qty 0 | §14 |
| Signed adverse: favorable gap does not reject OPEN; adverse does | §6.3 |
| Shadow unpriced close holds; no fabricated px | A24 §8.3 |
| Classifier maps `ENTRY_IN/OUT/INOUT/OUT_BY` per §4 | A21 + A37 |
| Unique key allows same `source_event_id` with CLOSE + OPEN (reversal) | §12.1 |

---

## 16. Config knobs (names only)

All numeric thresholds are **configuration**, not code constants (A23 §6). RiskManager / SuperAdmin change with audit (§59). This spec does **not** publish production numbers.

| Knob | Applies to |
|---|---|
| `max_quote_age_open` | OPEN family |
| `max_quote_age_close` | CLOSE family live/shadow quality |
| `max_quote_age_close_stale_fallback` | Shadow / last-quote close |
| `max_signal_age_open` / `max_signal_age_close` | Family clocks |
| `expires_at_offset_open` / `expires_at_offset_close` | Absolute expiry |
| `max_allowed_spread` | OPEN family block; CLOSE flag |
| `max_adverse_move_open` | OPEN family |
| `max_slippage` | OPEN family |
| `max_xau_gross` / `max_xau_net` / `max_position_quantity` / `max_open_positions` | OPEN family |
| `max_loss_per_trader` / `max_daily_execution_loss` / `max_portfolio_drawdown` | Engage stop-new; do not block CLOSE |
| `max_margin_usage` | OPEN family |
| `allow_risk_reduction_while_stop_new` | Default **true** |
| `promote_orphan_increase_to_open` | Default **false** (v1) |
| dest `min_qty` / `step` / contract maps | §8 |

Shadow may use parallel `SHADOW_*` caps so the simulated book resembles live (A24 §9.2). CLOSE family is never rejected for those caps.

---

## 17. Implementation notes (later — not this task)

When a later increment implements this policy:

- Suggested home (A30 Increment 8): `src/Risk/Rules/ExposureAction.cs` plus a classifier next to reconstruction output, not inside `apps/mt5-worker` callbacks.
- `CopyIntentAction` enum stays the C# surface; wire names stay the four §64 tokens.
- Do **not** hand-write MQ5. Do **not** send FIX from MT5 callbacks.
- Replace exclusive `KillSwitchMode` as SoT (A48) before relying on `AllowFixSend`.
- Migrations for `copy_intents.action`, `linked_destination_position_id`, family expiry columns, `source_destination_links`.
- Reviewer must fail the PR if OPEN and CLOSE share one `max_signal_age` or if daily-loss blocks CLOSE.

This task writes **only** this file.

---

## 18. Explicit non-goals

- No product source changes.
- No hardcoded production thresholds.
- No cluster concentration (§65 Phase 2).
- No auto-flatten from `GLOBAL_STOP`.
- No flattening of source MT5 or of shadow-as-if-live.
- No inventing dest positions to absorb late source closes.
- No Kafka / mesh / extra venue (§71).
- No ML inside classify or risk (§72.15).

---

## 19. Traceability

| Topic | Authority |
|---|---|
| Four classes, stricter open/increase | **§64**, §72.18 |
| Classify before limits | A23 §2, this §4 |
| Position mapping / scale-in / partial / reverse | §35, A21 §7.4–7.6 |
| Stale entries expire; close policy separate | §36, §63 |
| Quote / spread / move are entry guards | §37, A24 §7–§8 |
| Sizing: allocation vs dest remaining | §38, A23 §7, this §8 |
| Risk decisions | §39, A23 |
| Stop-new vs flatten | §40, A48 |
| Feature flags / REAL_COPY | §41, A49 |
| Fail closed | §62 |
| Tables / unique keys | §44–§45, A20 |
| Tests | §60, A27 `OpenVsCloseExposurePolicyTests` |
| Gap | A29 Q09, E11, E15 |
| Sequence | A30 Increment 8 `ExposureAction.cs` |

---

## 20. One-page operator card

```text
OPEN_EXPOSURE      new dest risk           STRICT   stop-new blocks
INCREASE_EXPOSURE  add to dest, same side  STRICT   stop-new blocks; needs link
REDUCE_EXPOSURE    take dest down, remain  LENIENT  dest qty, not source lots
CLOSE_EXPOSURE     flatten dest remaining  LENIENT  including flatten-run closes

Never open on a rotten XAU quote.
Never leave dest risk because the close quote was wide.
Never close what we never opened.
Never open the reverse if the close did not happen.
Never use one expiry for both families.
```
)
