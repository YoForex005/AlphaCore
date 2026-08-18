# P503_S03 — Per-trader XAU loss-cut: remove, dest-flatten that login, never MT5, keep CloseExposure

| Field | Value |
|---|---|
| Slot | **P503_S03** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_S03_loss_cut_design.md` |
| Status | **DESIGN ONLY** — product source **not** modified |
| SUT (read) | `D:\Prop\src\Domain\Risk\RiskEngine.cs` (`RiskLimits` L4–22, `Evaluate` L76–171) |
| Adjacent (read) | `XauUsdOneToOneCopyPolicy.cs`, `BaselineScorer.cs` / `TraderStateMachine`, `CopyTradingService.cs`, `CopyIntentAction`, `KillSwitchMode`, `TraderState`, `RiskEngineTests`, `XauUsdOneToOneCopyPolicyTests`, A23 / A48 / A71, architecture §39–§41 / §64 |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None** |

**Honesty:** this document is a **control design**, not an edge. A loss-cut does not mint expectancy. It stops adding dest gold after the source XAU book has already shown one of four measured failure modes. Dest capital today is unharmed only by `SAFE_BY_ABSENCE` (`NewOrderSingleImplemented=false`, persist `AllowFixSend=false`). If a sender is ever wired, the current engine **cannot** implement this policy: three book-loss rejects **freeze closes**, hop never `Evaluate`s `CloseExposure`, and eligibility rejects blocked traders on **every** action including close.

---

## 0. Recommendation (binding)

When any trigger in §2 fires for `(brokerId, sourceLogin)`:

1. **Remove** the trader from the copy set (`TraderState.RISK_BLOCKED`, reason in §4). No new `OPEN_EXPOSURE` / `INCREASE_EXPOSURE` for that login.
2. **Flatten dest opens for that login only** — emit `CLOSE_EXPOSURE` for every **mapped destination** XAU position whose `source_login` is this trader. Size from **dest remaining qty**, not source lots.
3. **Do not flatten MT5.** Source positions are not ours. No manager close, no source deal, no “mirror the flatten back to the prop account.”
4. **Allow `CloseExposure` after remove.** Removal must not trap dest risk. Policy, `RiskEngine`, and the copy hop must treat close/reduce as a **separate family** (architecture §64 / A71). The existing “blocked trader ⇒ reject everything” gate is **wrong** for this design.

Do **not** implement this as `KillSwitchMode.EmergencyFlatten` (account-wide, SuperAdmin + typed confirm, A48). This is a **per-login dest loss-cut**, not a desk panic flatten.

Do **not** implement this by flipping `MAX_LOSS_PER_TRADER` / `MAX_DAILY_EXECUTION_LOSS` / `MAX_PORTFOLIO_DRAWDOWN` to fire on every `Action`. Those three lines already freeze exits (P500_BOOK_61: 3/19 rejects **increase** trapped dest loss).

---

## 1. What `RiskLimits` actually is (measured)

`D:\Prop\src\Domain\Risk\RiskEngine.cs` compile defaults:

| Field | Default | What `Evaluate` does |
|---|---|---|
| `MaxLossPerTrader` | **500** (dest $) | `TraderRealizedLoss <= -500` → `PAUSE_TRADER` / `MAX_LOSS_PER_TRADER` on **every** action, including close |
| `MaxDailyExecutionLoss` | **2_000** (dest $) | `DailyExecutionPnl <= -2000` → `GLOBAL_STOP` / `MAX_DAILY_EXECUTION_LOSS` on **every** action |
| `MaxPortfolioDrawdown` | **3_000** (dest $ absolute) | `PortfolioDrawdown >= 3000` → `GLOBAL_STOP` / `MAX_PORTFOLIO_DRAWDOWN` on **every** action |
| `MaxXauGrossExposure` | 20 | increasing only |
| `MaxXauNetExposure` | 10 | increasing only (`REDUCE_SIZE`) |
| `MaxPositionQuantity` | 5 | increasing only |
| `MaxOpenPositions` | 20 | increasing only |
| `MaxAllowedSpread` | 2.0 | increasing only |
| `MaxQuoteAge` | 3 s | increasing only |
| `MaxSourceSignalAge` | 15 s | increasing only |
| `MaxPriceMove` | 3.0 | increasing only |
| `MaxSlippage` | 1.5 | **declared, never read** |
| `MaxMarginUsage` | 0.70 | increasing only |
| `BlockMartingale` | true | increasing only → `PAUSE_TRADER` / `MARTINGALE_BLOCK` |
| `BlockAbnormalSizing` | true | increasing only → `ABNORMAL_SIZING_BLOCK` |

Architecture §39 names the same family (max loss per selected trader, daily execution loss, portfolio DD, XAU gross/net, qty, open count, spread, quote age, signal age, price move, slippage, margin, martingale, abnormal sizing, venue health). It does **not** name:

- source XAU book PnL ≤ 0 as a dest-engine reject
- 3 consecutive XAU losses
- peak-to-trough DD as **40% of peak**
- per-login dest flatten on remove
- a carve-out that close survives trader pause

`docs/risk.md` (5% daily / 10% total / 50 lots / 25 positions) is **stale** versus `RiskLimits`. Do not design against that file.

### 1.1 Adjacent layers that already half-do this (and get close wrong)

`XauUsdOneToOneCopyPolicy.IsTraderEligible` already rejects:

| Condition | Reason | Closes too? |
|---|---|---|
| `State ∈ {RISK_BLOCKED, DISQUALIFIED, PAUSED}` | `TRADER_BLOCKED_*` | **Yes** — `Evaluate` calls eligibility first |
| `Martingale \|\| AveragingDown \|\| LotEscalation` | `TRADER_SIZE_PATTERN_BLOCK` | **Yes** |
| `XauNetPnl <= 0` | `XAU_BOOK_NOT_PROFITABLE` | **Yes** |
| `CompletedXauTrades < 20` | `NEED_MORE_XAU_HISTORY` | **Yes** |
| `demo\` / `contest\` group | `DEMO_OR_CONTEST_GROUP` | **Yes** |

Unit fact `Close_of_open_book_is_one_to_one` only passes because the fixture trader is still **eligible**. After a real remove, **HEAD policy would refuse the dest close**. That is the defect this design exists to forbid.

`TraderStateMachine.FromBaseline` sets `RISK_BLOCKED` when `risk >= 80` **or** `(Martingale ∧ MaxDrawdown > 0 ∧ NetPnl < 0)`. `MaxDrawdown` is **absolute $** on the completed-XAU equity curve (peak − equity), **not** 40% of peak. There is **no** consecutive-loss feature. Scorer martingale = next completed trade `MaxVolumeLots > prior * 1.25` after a losing trade.

`CopyTradingService.GenerateShadowIntentsAsync`:

- `Evaluate`s **opens only**
- close loop **skips** `RiskEngine`
- `TraderRealizedLoss = Min(0, this ticket NetRealizedPnl)` — **not** trader book
- `DailyExecutionPnl = 0`, `PortfolioDrawdown = 0`, dest book fields = 0
- `KillSwitch = None` hardcoded
- persist `AllowFixSend = false` always

So today’s “loss-cut” is three **dead or dest-dollar** lines that, if ever fed honest dest PnL, would **block the exit**.

---

## 2. Loss-cut triggers (OR; first match wins; all on **completed XAUUSD** only)

Universe = reconstructed trades for `(BrokerId, Login)` with `CanonicalSymbol == XAUUSD` and `Completed == true`, ordered by `ClosedAt` (then `OpenedAt`, then `PositionId` for ties). Open tickets do **not** count toward PnL / streak / DD / martingale. Do not mix symbols. Do not use dashboard all-symbol `netSourcePnl`.

| Id | Trigger | Precise definition | Why this, not `RiskLimits` |
|---|---|---|---|
| **T1** | **XAU PnL ≤ 0** | `Σ NetRealizedPnl <= 0` over the completed XAU book | Policy already has this as eligibility. Engine `MaxLossPerTrader=500` is dest $ after a fill, too late, and freezes close. |
| **T2** | **3 consecutive XAU losses** | Last 3 completed XAU trades all have `NetRealizedPnl < 0`. Need `n ≥ 3`. A scratch (`== 0`) **breaks** the streak (not a loss). | **Missing** everywhere. Stops a still-green book that just printed L-L-L. |
| **T3** | **Peak-to-trough DD > 40% of peak** | Walk completed XAU in close order. `equity += pnl`. `peak = max(peak, equity)`. If `peak > 0` and `(peak - equity) / peak > 0.40` → fire. If `peak <= 0`, **do not** divide; T1 already covers a never-green book. | Engine `MaxPortfolioDrawdown=3000` is dest-account $ and global-stop. Scorer `MaxDrawdown` is absolute $, never a 40% ratio. |
| **T4** | **Martingale** | Any of: scorer `Martingale` (size-up > 1.25× after a loss); `AveragingDown`; `LotEscalation` (size-up > 1.5×). Treat as one **size-pattern** family. | Engine only `PauseTrader`s **increasing** intents. It does not remove, does not dest-flatten, and does not see grid-as-distinct-tickets (recon `WasAveragedDown` is same-`PositionId` scale-in only). |

**Evaluation cadence:** on every newly completed XAU reconstruction for that login, and once per copy-hop cycle before any open is accepted. Triggers are **latched** (see §4). Recovery of later PnL does **not** auto-clear.

**Not triggers (do not smuggle in):** first-3 dollars, `EarlyQualityScore`, ML probability, dest spread, `MaxDailyExecutionLoss`, global portfolio DD, MAE/MFE (`FeatureQuality=Unavailable`). Those are not this control.

**T1 vs T3 interaction:** a book that never made a peak stays on T1. A book that ran to +$10k then gave back >$4k fires T3 even if still net-positive. That is intentional: 40% of peak is a **give-back** cut, not a “must be red” cut.

---

## 3. Effects (ordered, all four required)

### 3.1 Remove the trader

| Step | Action |
|---|---|
| 1 | Persist `TraderScore.CurrentState = RISK_BLOCKED` (or keep `DISQUALIFIED` if already worse). Do not use `PAUSED` — pause is operator-temporary; this latch is a **risk remove**. |
| 2 | Persist `risk_decisions` / audit: `Outcome = PauseTrader`, `Reason = LOSS_CUT_<T1\|T2\|T3\|T4>` (primary = first trigger in §2 order). |
| 3 | Cancel unsent **increasing** intents for that login (`OPEN` / `INCREASE` with no dest fill). Do not cancel in-flight dest closes. |
| 4 | Copy set: `IsTraderEligible` remains false for **new risk**. Live / SHADOW / LIVE_CANDIDATE promotion paths must see the latch (`CanPromoteToLive` stays false). |

Do **not** delete scores, reconstructed trades, or shadow rows.

### 3.2 Flatten dest opens for that login

| Allowed | Forbidden |
|---|---|
| `CLOSE_EXPOSURE` for dest positions **linked** to this `source_login` (XAUUSD copy book) | Flatten the whole Pepperstone account (other logins) |
| Qty = dest remaining (A71 / §38) | Qty = source lots / `AllocationFactor` re-normalize |
| Persist `CopyIntent` + `risk_decision` + `execution_intent` before any FIX | Send from the MT5 callback or from `Evaluate` itself |
| Shadow-only dest: mark shadow closed; **no** `35=D` | Invent a dest id so a close has something to hit |
| Coalesce with an already-queued source-driven close of the same dest id | Double-close / blind resend on `EXECUTION_STATE_UNKNOWN` (A48 / §34) |

This is **`LOSS_CUT_DEST_FLATTEN`**, scoped `(destination_account, source_login)`. It is **not** `EMERGENCY_FLATTEN`. A48 stay-true:

- no SuperAdmin typed phrase required for this per-login cut (it is automatic policy)
- still persist-before-send, still TRADE logon + lease when live send exists
- still **no** send today (`NewOrderSingleImplemented=false`) — design the intents so a future sender can honor them

If dest mapping is missing (`source_destination_links` still MISSING on HEAD): **do not invent**. Emit `MAPPING_MISSING`, leave a recon item, keep the trader removed so no **new** dest risk is added. Residual unmapped dest is a defect to reconcile, not a license to spray market closes.

### 3.3 Do not flatten MT5

Hard invariant (A48 §0 / §11: “Never flatten source MT5”):

- No `TradePositionClose`, no manager `DealerSend`, no HTTP “close source ticket.”
- Source may keep losing. That is the source broker’s book. We stop **copying** it.
- Do not write source deals. Do not mutate MT5 ledger rows to look closed.
- Shadow is not live; “flatten shadow” is a row status, not a venue order.

If an operator wants the client’s MT5 book closed, that is a **broker operations** action outside this engine.

### 3.4 Allow `CloseExposure` after remove

Carve-out is mandatory in **three** places (all currently wrong or untested for this case):

| Layer | Today | Required |
|---|---|---|
| `XauUsdOneToOneCopyPolicy.Evaluate` | Eligibility fail ⇒ reject **all** actions | If `Action == CloseExposure` (and dest/source identity known): **skip** T1–T4 / `TRADER_BLOCKED_*` / `XAU_BOOK_NOT_PROFITABLE` / size-pattern. Still reject `NOT_XAUUSD` / bad qty. Prefer dest remaining qty when a link exists. |
| `RiskEngine.Evaluate` | L117–124 book-loss rejects apply to close; L141 martingale is increasing-only (OK) | **Never** reject `CloseExposure` / `ReduceExposure` for T1–T4, `MAX_LOSS_PER_TRADER`, `MAX_DAILY_EXECUTION_LOSS`, `MAX_PORTFOLIO_DRAWDOWN`, `MARTINGALE_BLOCK`, `ABNORMAL_SIZING_BLOCK`. Quote/spread/stale already increasing-only — keep that. Identity / mapping / unknown-state may still block. |
| `CopyTradingService` close loop | Skips `Evaluate`; still requires `IsTraderEligible` via `policy.Evaluate` | After remove, still generate (or keep) close intents. Run `Evaluate` on close so the carve-out is the audited decision (`Reason = RISK_REDUCTION` or `LOSS_CUT_DEST_FLATTEN`). Persist `AllowFixSend` by the **reducing** conjunction, not “trader still eligible.” |

`AllowFixSend` for a dest close after remove:

```text
close/reduce may send only when:
  NewOrderSingleImplemented
  AND dest position id known
  AND TRADE session healthy enough to exit (A25/A48: exit ≠ new copy)
  AND not EXECUTION_STATE_UNKNOWN on that id
  AND this is not a second close

REAL_COPY_EXECUTION_ENABLED and KillSwitch.StopNewExecution
must NOT block this close.
KillSwitch.EmergencyFlatten owning the same dest id coalesces (do not double-send).
```

HEAD `AllowFixSend` requires `RealExecutionEnabled && KillSwitch == None && Reconciled && VenueHealthy`. That conjunction **forbids** the required close. Design change: **reducing family uses a different send bit**. Do not “fix” it by setting `KillSwitch = None` after a loss-cut.

Unit facts that must exist before any product change (not written this slot):

- `Remove_on_T1_blocks_open_allows_close`
- `Three_consecutive_xau_losses_removes`
- `Drawdown_over_40pct_of_peak_removes_even_if_net_positive`
- `Martingale_removes_and_queues_dest_close_not_mt5`
- `MAX_LOSS_PER_TRADER_does_not_reject_CloseExposure`
- `Blocked_trader_CloseExposure_reason_is_not_TRADER_BLOCKED_*`

---

## 4. Reason codes and latch

| Code | Trigger | Outcome | New exposure | Dest flatten | MT5 | Close after |
|---|---|---|---|---|---|---|
| `LOSS_CUT_XAU_PNL_NONPOSITIVE` | T1 | `PauseTrader` | reject | yes (mapped dest) | **no** | **allow** |
| `LOSS_CUT_THREE_CONSECUTIVE_XAU_LOSSES` | T2 | `PauseTrader` | reject | yes | **no** | **allow** |
| `LOSS_CUT_PEAK_TROUGH_DD_40PCT` | T3 | `PauseTrader` | reject | yes | **no** | **allow** |
| `LOSS_CUT_MARTINGALE` | T4 | `PauseTrader` | reject | yes | **no** | **allow** |
| `LOSS_CUT_DEST_FLATTEN` | effect of any T* | `Approve` (reducing) | n/a | the close itself | **no** | n/a |

Do **not** emit `TRADER_RISK_BLOCKED` from `RiskEngine` as the **only** reason (P500_BOOK: engine grep is 0 today). Scorer state `RISK_BLOCKED` may still be the row state; the **decision reason** must name which T* fired.

**Latch:** `loss_cut_latched_at`, `loss_cut_reason`, `loss_cut_equity`, `loss_cut_peak`. Clear only by audited SuperAdmin / RiskManager override (§72.19). A later winning XAU trade must **not** silently re-admit. That would be the classic martingale “it recovered” leak.

**Re-admit (later, not v1 of this cut):** human only; require new sample after latch (do not reuse the losing streak); still `CanPromoteToLive => false` until a separate live-promotion design exists.

---

## 5. What this is not

| Anti-pattern | Why forbidden |
|---|---|
| Reuse L117–124 as the loss-cut | Freezes dest exits; dest $ after fill; hop zeros the inputs |
| `KillSwitchMode.EmergencyFlatten` for one login | Exclusive enum; no `{stop-new × flatten}`; A48 SuperAdmin flatten of **account** |
| Flatten all dest positions on the execution login | Other source logins are innocent |
| Flatten / close source MT5 | Not our position; manager close is out of scope |
| Policy eligibility fail on `CloseExposure` | Traps dest risk — the exact A71 G21–G22 hole |
| Auto-flatten from `GLOBAL_STOP` / daily dest loss | A48: `GLOBAL_STOP` = stop-new only |
| Treat scorer `MaxDrawdown` $ as 40% | Different unit; would false-positive tiny peaks in $ or miss large % |
| Copy-all 8463 and “trust the cut” | Cut is after damage; `RISK_BLOCKED` tail is already −$241,580 source; dest $0 only by no send |
| Claim this is +EV | It is a **stop adding** rule |

---

## 6. Placement (when product is allowed to change — not this slot)

Suggested shape; **do not implement here**:

1. **Detector** (pure): `XauLossCut.Evaluate(IReadOnlyList<ReconstructedTradeResult>) → { Fired, Trigger, Peak, Equity, Streak }`.
2. **Policy:** `IsTraderEligible` stays the open-family gate. `Evaluate(CloseExposure)` uses `IsCloseAllowed(removedTrader, mappedDest)` instead of full eligibility.
3. **RiskEngine:** add request fields `XauBookNetPnl`, `ConsecutiveXauLosses`, `XauPeakToTroughDdPct`, keep `MartingaleFlag`. Apply T* **only** when `IsIncreasing`. Reducing path stays `RISK_REDUCTION` / dest-flatten approve.
4. **Application:** on T*, persist state + enqueue per-login dest closes. Never call MT5 close APIs.
5. **Settings:** `LossCut:ConsecutiveLosses=3`, `LossCut:PeakTroughFraction=0.40`, `LossCut:NonPositiveXauPnl=true`, `LossCut:Martingale=true`. Do not reuse `SettingsController`’s unread `MaxDailyDrawdownPct=5`.

`docs/risk.md` must be rewritten to match `RiskLimits` **and** this cut before anyone treats it as law.

---

## 7. Verdict

| Item | Decision |
|---|---|
| Remove if XAU PnL ≤ 0 | **Yes (T1)** |
| Remove if 3 consecutive XAU losses | **Yes (T2)** |
| Remove if peak-to-trough DD > 40% of peak | **Yes (T3)**; `peak <= 0` defers to T1 |
| Remove if martingale / avg-down / lot escalation | **Yes (T4)** |
| Flatten dest opens for that login | **Yes**, mapped dest only, dest qty |
| Flatten MT5 | **No** |
| Allow `CloseExposure` after remove | **Yes** — required; HEAD policy/engine would trap dest |
| Use current `MaxLossPerTrader=500` as this cut | **No** |
| Edit product this slot | **No** |

**Risk to capital today:** **NONE** (`SAFE_BY_ABSENCE`). **Risk if a sender is wired without this design:** HIGH — eligibility and L117–124 would refuse the only safe order left (the dest close), and martingale names would keep opening until a dest-dollar cap fired too late.

This report is the only write. Product tree unchanged.
