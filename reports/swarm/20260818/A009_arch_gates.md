# A009 — Architecture go-live gates before `35=D`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\A009_arch_gates.md` |
| Agent | A009 (architecture gates / copy / risk / FIX / trade #3) |
| Date | 2026-08-18 |
| Binding source | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| Focus | Copy trading, risk engine, FIX sessions, `REAL_COPY_EXECUTION_ENABLED`, trade #3, MFE/MAE law, venue naming |
| Product source modified | **No** |
| Secrets | **None recorded.** Passwords stay `<SECRET>`. This file does not copy FIX/MT5 credentials. |

**Law (architecture §68 + §70 + §41):** do not send a live `NewOrderSingle` (`35=D`) until **every** gate below is true **and** `REAL_COPY_EXECUTION_ENABLED=true` **and** the risk engine is healthy **and** TRADE is `READY_FOR_EXECUTION`. One FAIL blocks send.

**Current honest state (2026-08-18):** product C# has **no** `35=D` builder (E034: `SAFE_BY_ABSENCE`). `CTraderFixOptions.RealCopyExecutionEnabled` defaults **false**. `apps/fix-worker` stamps QUOTE/TRADE `Disconnected` and refuses send. That is a **missing sender**, not a passed go-live review.

---

## 0. Verdict

| Question | Honest answer |
|---|---|
| Can we copy **and** guarantee no loss? | **No.** That product does not exist. Copy inherits source losers plus destination spread, latency, and slippage. |
| What can we implement? | **Risk-capped copy:** shadow first, bound losses, reject stale/wide/moved prices, pause traders, kill new entries. Never promise zero P&amp;L. |
| When may we emit `35=D`? | After **19/19** §68 **and** **14/14** §70 **and** explicit flag **and** named manual review. Default remains OFF. |
| Is Pepperstone/cTrader an LP? | **No.** It is the **external execution venue**. Do not name it LP. |
| May we invent MFE/MAE? | **No.** Closed deals alone are not a path. No source ticks → omit / `Unavailable`. Never fill from cTrader quotes. |
| Does trade #3 unlock live money? | **No.** Trade #3 = `EARLY_SCORE_ELIGIBLE` → default **SHADOW**. Not `PROVEN_PROFITABLE`. |

One-line:

```text
Copy is allowed only as bounded-loss, shadow-proven, risk-gated execution.
Zero-loss copy is a false requirement. 35=D stays OFF until every gate is evidenced.
```

---

## 1. Naming and data-quality laws (non-negotiable)

### 1.1 cTrader is not an LP (§1 item 6, §25, A87)

Pepperstone / cServer FIX 4.4 is the **destination execution venue**.

| Correct words | Forbidden words |
|---|---|
| execution venue | LP, liquidity provider |
| `execution_venues` | `lps`, `lp_account` |
| QUOTE + TRADE sessions | “LP feed”, “LP fill” |
| destination quote / dest fill | source-book identity |

The software must **not** assume institutional LP semantics (last-look, wholesale credit, LP book). Fills are broker-gateway fills. Tag 55 is **not** assumed `"XAUUSD"`; instrument ID comes from Security List.

### 1.2 Never invent MFE/MAE (§1 item 5, §17, A45)

Exact MFE/MAE requires a **source-broker** tick/price path **while the position is open**.

| Input | Legal MFE/MAE |
|---|---|
| Source MT5 ticks covering `[opened_at, closed_at]` | `feature_quality=EXACT`, `price_source=<that broker>_MT5_TICKS` |
| Explicit poorer substitute (bars, incomplete tape) | `APPROXIMATE` + named `price_source` |
| Closed deals / VWAP / last mark / “typical gold range” | **Forbidden — fabrication** |
| cTrader QUOTE tape used as source excursion | **Forbidden — silent mix** |

Dashboard copy: **“MFE/MAE when valid.”** Current scorer already defaults `MaeMfeQuality = Unavailable` and does not invent numbers (`src/Domain/Scoring/BaselineScorer.cs`). Keep that. Do not “complete” the feature by borrowing venue quotes.

### 1.3 Trade #3 is early evidence, not skill (§15, §23, §72.16)

Count only **3 completed reconstructed XAUUSD position lifecycles**. Do not count order place, deal fill, partial close, or SL/TP change as a trade.

```text
Trade #3 close  →  EARLY_SCORE_ELIGIBLE  →  default SHADOW
```

Not:

```text
Trade #3 close  →  PROVEN_PROFITABLE  →  live 35=D
```

States (§22): `INSUFFICIENT_DATA` → `EARLY_SCORE` → `WATCH` / `SHADOW` → (later evidence) `LIVE_CANDIDATE` → `LIVE`. Live still requires gates + flag + risk.

---

## 2. Correct copy pipeline (must exist before any `35=D`)

Architecture §32. Never send FIX from an MT5 callback.

```text
MT5 SOURCE BROKERS
        ↓
collectors (immutable raw deals/orders/positions)
        ↓
trade reconstruction (completed XAU lifecycles)
        ↓
features + deterministic score  (ML later, never bypasses risk)
        ↓
SHADOW copy on destination QUOTE  (Phase 5)
        ↓
CopyIntent  (persist, idempotent)
        ↓
RiskEngine  (final authority)
        ↓
ExecutionIntent  (persist BEFORE send)
        ↓
FIX TRADE adapter  NewOrderSingle 35=D   ← this line is gated
        ↓
ExecutionReport(s) + destination positions
        ↓
reconciliation
```

Scoring/ML may emit only: **candidate, confidence, suggested allocation**. Risk decides: approve / reduce / reject / pause trader / pause venue / global stop.

---

## 3. FIX session law (QUOTE ≠ TRADE)

Two **independent** sessions (§7, §27–28, §41). Separate connection, sequence, heartbeat, reconnect, metrics, logs. One sequence counter shared = defect.

| Session | Role before `35=D` | May send `35=D`? |
|---|---|---|
| QUOTE (SSL default 5211) | Logon, Security List, live XAU bid/ask, quote age | **Never** |
| TRADE (SSL default 5212) | Logon, mass status, positions, ER ingest, recon | **Only** after all gates + flag |

Rules:

1. Production transport is **TLS**. Plain ports are not the production default.
2. TargetCompID stays the issued form (`cServer`). No silent `CSERVER` fold.
3. `SenderSubID` / `TargetSubID` are configurable; follow current RoE, do not guess from a form label.
4. **One active TRADE owner** per destination account (Redis/DB lease + fencing). Two TRADE sessions → duplicate reports.
5. After login / leadership change: **block new executions** → `OrderMassStatusRequest` + `RequestForPositions` → repair DB → only then `READY_FOR_EXECUTION`.
6. Flags (architecture §41 defaults):

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

Session-on is **not** a send license. `REAL_COPY_EXECUTION_ENABLED` is necessary and **not sufficient**.

Unknown send state (§34): if TCP dies after send, set `EXECUTION_STATE_UNKNOWN`. **Do not** retry `35=D`. Recover via OrderStatus / mass status / ER / positions.

---

## 4. Risk engine — last authority before any send

Architecture §39 hard limits (all must be implemented, tested, and **called** on the live path):

| Limit | Reject / action |
|---|---|
| max loss per selected trader | pause trader |
| max daily execution-account loss | global stop new |
| max portfolio drawdown | global stop new |
| max XAUUSD gross / net exposure | reject or reduce |
| max position quantity / open count | reject |
| max allowed spread | reject |
| max quote age | `QUOTE_STALE` |
| max source-signal age | `SIGNAL_STALE` |
| max price move / slippage | `PRICE_MOVED_TOO_FAR` |
| max dest margin usage | reject |
| martingale / abnormal sizing | block / pause |
| venue health | pause venue |
| not reconciled | block new |
| kill switch `STOP_NEW_EXECUTION` | no new entries; leave existing |
| kill switch `EMERGENCY_FLATTEN` | separate permission; not the same switch |

Reduce/close is **not** “open more” (§72.18). Stale quotes/signals reject **entries**. Flatten is stronger-auth than stop-new.

Domain `RiskEngine.Evaluate` already encodes many of these reasons. That is **not** a go-live PASS until workers persist `CopyIntent` → call `Evaluate` → persist `RiskDecision` → persist `ExecutionIntent` → send only if `AllowFixSend` **and** flag **and** `READY_FOR_EXECUTION`. `AllowFixSend` is a DTO bit; it is not a socket.

---

## 5. User want: “copy AND no loss”

### 5.1 What is false

| Claim | Why it is false |
|---|---|
| Copy winners only, skip losers, keep 1:1 fidelity | That is not copy; it is a different strategy with look-ahead if you wait for the source close. |
| Three green XAU trades prove future profit | §3 / §15: target is **future** dest-net P&amp;L inside risk, not “who made money on the first three.” |
| Destination will fill at source MT5 price | Different book, clock, spread. Shadow exists to measure that gap. |
| cTrader as LP will give institutional fills | Not an LP. No such contract in this architecture. |
| Invented MFE/MAE will filter “safe” traders | Fabricated features are not a risk control. |

### 5.2 What is the honest implementation

Ship **bounded-loss copy**, not lossless copy.

```text
1. Reconstruct completed XAU trades (no fake MFE/MAE).
2. Trade #3 → EARLY_SCORE only.
3. Eligible traders go SHADOW on cTrader QUOTE (dest bid/ask, dest costs).
4. Measure destination-net P&L, dest DD, dest slippage vs source.
5. Promote to LIVE_CANDIDATE only after configured shadow evidence
   (min completed + min shadow trades + min shadow net + max shadow DD
    + current score + no severe flags). Do not hardcode those numbers
    before backtest (§23).
6. LIVE still cannot bypass RiskEngine.
7. Size = canonical notional/risk → dest qty (never MT5 lots == OrderQty).
8. Hard caps make a losing copy *finite*. They do not make it *zero*.
9. STOP_NEW_EXECUTION is the first operational brake.
10. REAL_COPY_EXECUTION_ENABLED stays false until §6 + §7 below are 100%.
```

If the business still requires “no loss,” the only honest product modes are:

| Mode | Meaning | `35=D` |
|---|---|---|
| **SHADOW_ONLY** | Copy in the ledger, never the venue | Never |
| **BOUNDED_LIVE** | Live copy with hard max-loss / DD / kill | After all gates |
| **ZERO_LOSS_LIVE** | Every copied trade profitable | **Refuse to build** |

Do not market SHADOW P&amp;L as live P&amp;L. Do not market source P&amp;L as destination P&amp;L.

---

## 6. Go-live gates that must PASS before `35=D` (architecture §68)

**Count: 19. Zero skips.** Earliest phase that can *produce* evidence is listed; the gate must still be true at enablement.

```text
[ ] G01  MT5 historical / live ingestion is stable
[ ] G02  duplicate event handling is proven
[ ] G03  trade reconstruction tests pass
[ ] G04  XAU symbol mappings are verified
[ ] G05  quote session stable
[ ] G06  trade session stable
[ ] G07  cTrader reconciliation works after restart
[ ] G08  copy intents are idempotent
[ ] G09  unknown execution state recovery works
[ ] G10  position sizing conversion is verified
[ ] G11  risk engine unit / integration tests pass
[ ] G12  stale quote rejection works
[ ] G13  stale signal rejection works
[ ] G14  shadow copy has sufficient sample
[ ] G15  destination costs / slippage measured
[ ] G16  kill switch tested
[ ] G17  secrets removed from repo / logs
[ ] G18  dashboard exposes venue health / risk
[ ] G19  manual review completed
```

| ID | Earliest phase | PASS means (evidence, not chat) |
|---|---|---|
| G01 | 1 | Achiever **and** StarwaveFX connected across reconnect; all groups discovered; history resumable; live deals persist before async work. Fake connectors / in-memory seed ≠ PASS. |
| G02 | 1 | Replay of the same deal/event stream does not duplicate rows; tested. |
| G03 | 2 | Reconstruction fixtures (complete XAU, partials, scale-in, reverse, first-3) pass in CI. |
| G04 | 2 / 4 | Source aliases confirmed on both brokers; Pepperstone instrument ID **discovered** via Security List and stored. Guessed `55=XAUUSD` fails tests. |
| G05 | 4 | Independent **TLS** QUOTE logon stable; real heartbeats; quote age from last **venue** print. Worker timestamp stamps ≠ PASS. |
| G06 | 7 | Independent **TLS** TRADE logon stable; sequence files persist; reconnect proven. Send still flagged off until the rest. |
| G07 | 7 | After restart: mass-status + positions vs DB; inconsistent book **blocks** `READY_FOR_EXECUTION`. |
| G08 | 5 / 8 | Same source event cannot insert a second intent or fire a second order; persist-before-send. |
| G09 | 7 / 8 | Send+disconnect → `EXECUTION_STATE_UNKNOWN`; **no** blind `35=D` retry; recover via status/ER/positions; tested. |
| G10 | 5 / 8 | Known source-lot → dest-qty fixtures; dest min/step/max from venue; no passthrough `OrderQty = MT5 lots`. |
| G11 | 8 | Every hard limit + reduce-vs-open + kill + recon block: **zero** FIX outbound on reject. Integration, not only `Evaluate` unit. |
| G12 | 4 / 8 | `quote_age > max` rejects OPEN/INCREASE on shadow **and** live. Logged-on ≠ fresh. |
| G13 | 5 / 8 | Expired `CopyIntent` cannot open more. Close/reduce still allowed under policy. |
| G14 | 5 | Selected traders shadowed on **destination** quotes; sample size/window agreed and stored. Four demo logins ≠ sample. |
| G15 | 5 | Slippage/spread/commission from dest quotes (later dest fills) drive `shadow_performance`. Source-broker P&amp;L is not dest cost. |
| G16 | 8 | `STOP_NEW_EXECUTION` proven (does not flatten). Flatten is a distinct authorized, audited path. |
| G17 | 0 (always) | No committed secrets; FIX 553/554 and `Password=` redacted; dashboard never receives credentials. |
| G18 | 3 / 4 | React shows real MT5 / QUOTE / TRADE health, quote age, rejects, kill mode, shadow book. No secrets in the browser. |
| G19 | 8 | Named reviewer records G01–G18 PASS + §70 14/14 + flag decision. Swarm reports are not this sign-off. |

---

## 7. Additional live-FIX acceptance before `35=D` (architecture §70)

Required **in addition** to §68. Not part of the first useful version (§69).

```text
[ ] F01  TRADE FIX Logon is stable
[ ] F02  ExecutionReports are persisted correctly
[ ] F03  Position reports reconcile after restart
[ ] F04  Unique ClOrdID rules are proven
[ ] F05  Duplicate report handling is proven
[ ] F06  Unknown-state recovery is proven
[ ] F07  Partial fills are supported
[ ] F08  Order rejects are supported
[ ] F09  Cancel / replace is supported where required
[ ] F10  Destination position mapping is correct
[ ] F11  Risk-engine rejection happens BEFORE FIX send
[ ] F12  Real execution is feature flagged (default OFF)
[ ] F13  Global stop-new-orders works
[ ] F14  Reconciliation blocks execution while inconsistent
```

**Conjunction for the first live `35=D`:**

```text
19/19 §68
AND 14/14 §70
AND REAL_COPY_EXECUTION_ENABLED=true   (explicit, reviewed)
AND RiskEngine.AllowFixSend=true
AND TRADE READY_FOR_EXECUTION
AND kill switch = None
AND CopyIntent not expired
AND ExecutionIntent persisted with unique cl_ord_id
AND no second TRADE owner
```

Anything less → **do not send**.

---

## 8. Phase order (do not skip to send)

```text
0 Audit
1 MT5 ingest (both brokers)
2 Reconstruct + first-3 counter
3 Deterministic score + React
4 QUOTE TLS + Security List + live XAU quote     (no live trading)
5 Shadow copy on dest quotes                     ← first useful version (§69)
6 ML only if it beats baseline OOS               (optional; never bypasses risk)
7 TRADE TLS read + recon                         (35=D still OFF)
   ALL §68 + ALL §70 + review
8 Risk-controlled execution + explicit flag      ← only place 35=D is legal
```

First useful version (§69) is **12 items, no ML, no live send**. Passing §69 does **not** license `35=D`.

---

## 9. What this file forbids implementers to do

```text
[DO NOT] Enable REAL_COPY_EXECUTION_ENABLED to “try one lot”
[DO NOT] Send 35=D from an MT5 callback
[DO NOT] Retry 35=D after a broken send
[DO NOT] Treat trade #3 as proven skill or auto-LIVE
[DO NOT] Calculate MFE/MAE from closed deals or dest quotes
[DO NOT] Call cTrader / Pepperstone / cServer an LP
[DO NOT] Blind-convert MT5 lots to OrderQty
[DO NOT] Guess tag 55 / instrument ID
[DO NOT] Share QUOTE and TRADE sequence state
[DO NOT] Run two active TRADE sessions on the same account
[DO NOT] Conflate STOP_NEW_EXECUTION with EMERGENCY_FLATTEN
[DO NOT] Let ML or a dashboard button bypass RiskEngine
[DO NOT] Commit or log FIX/MT5/proxy passwords
[DO NOT] Claim copy with no loss
```

---

## 10. Current tree vs these gates (honesty, not greenwash)

Observed on 2026-08-18 (supporting E034 / fix-worker / Domain):

| Surface | Observed | Gate impact |
|---|---|---|
| `35=D` builder / QuickFIX initiator | Absent | Safe by **absence**, not G05–G07 PASS |
| `RealCopyExecutionEnabled` | Default `false` | F12 control present as a property; not a tested LoggedOn refuse |
| `apps/fix-worker` | Stamps `Disconnected`; no socket | G05/G06 **FAIL** |
| `RiskEngine` | Limits + stale/kill/recon reasons exist | G11 **FAIL** until wired + integration-tested |
| `BaselineScorer` | MFE/MAE `Unavailable` | Correct omission; do not “fix” by inventing |
| `TraderState` | Includes SHADOW / LIVE_CANDIDATE / LIVE | Trade #3 must not jump to LIVE |
| Secrets in this report | None | G17 still requires a full repo/log re-scan at enablement |

Siblings (do not treat as this file): A28 (phase map), A100 (§68 working checklist), A23 (risk spec), A24 (shadow), A25 (FIX sessions), A41/A49 (flags), A45 (MFE/MAE), A87 (not an LP), E002/E034 (no live send / no `35=D`).

---

## 11. Sign-off boxes

```text
[ ] 19/19 §68 gates PASS (this file §6)
[ ] 14/14 §70 items PASS (this file §7)
[ ] Trade #3 path is SHADOW-only by default
[ ] MFE/MAE omitted unless source ticks + feature_quality
[ ] Destination named execution venue, never LP
[ ] REAL_COPY_EXECUTION_ENABLED reviewed; default remains false if any box unchecked
[ ] Named reviewer / date / evidence links recorded
[ ] First 35=D still refused until the conjunction in §7 is true
```

**Current:** all boxes unchecked. **Real copy: DISABLED.** **Zero-loss copy: not a deliverable.**

End of A009.
