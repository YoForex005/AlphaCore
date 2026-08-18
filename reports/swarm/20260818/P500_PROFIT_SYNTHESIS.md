# P500 — How the cTrader account gets profitable (measured)

| Field | Value |
|---|---|
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Swarm | workflow `ctrader-profit-path` (500 agents) + 56 named subagents |
| Live API | `http://127.0.0.1:5000` (remeasured this pass) |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| Secrets printed | **None** |

**Honesty rule:** wanting higher profit and lower loss does not create an edge. A TLS Logon (`35=A`) is not a fill. Copying 8,463 challenge logins onto one Pepperstone account is how you lose the account.

---

## 0. Direct answer

The Pepperstone / cTrader account becomes profitable **only** if you copy a **tiny, filtered subset** of source XAUUSD behavior that still has **positive expectancy after venue costs**, at **tiny size**, after **shadow on real quotes** proves it. It does **not** become profitable by connecting FIX and sending trades today.

| User ask | Measured answer |
|---|---|
| Connect to cTrader | **Already done.** QUOTE `:5211` and TRADE `:5212` TLS Logon = `LoggedOn` (`live-us-eqx-01.p.c-trader.com`, account id `1369850`, `TargetCompID=cServer`). |
| Send trades to cTrader | **Impossible in current code** (`SAFE_BY_ABSENCE`). `CTraderFixSession` outbound MsgType is only `A`. Socket is disposed after the probe. `REAL_COPY_EXECUTION_ENABLED` is forced `false`. `CanPromoteToLive` is hard-`false`. |
| Higher profits | **Not by sending more.** By **not copying** the left tail (martingale / demo pass-target / scalps / lot explosions) and by sizing **far below** source lots. |
| Lower loss | **Do not send.** Then, if/when send exists: never copy `RISK_BLOCKED`, never copy holds that die in the spread, never retry unknown `ClOrdID`, dest lot cap **0.05** not 5.0. |

**If we flipped the flag and sprayed `35=D` now, expected destination PnL is negative.** The scored XAU book is already net **−$154,425** at source. Destination real PnL is **$0**. Shadow PnL is **$0** because there is **no quote tape**.

**Addendum (same session, API restart ~09:01Z):** in-memory scores wiped (ingest re-running). `REAL_COPY_EXECUTION_ENABLED` is now **true in process config** (`LiveRuntimeStatus` reads the env). `CopyTradingService` exists with `NewOrderSingleImplemented=false`, `VenueReconciled=false`, and it **forces** `AllowFixSend=false` / `SHADOW_ONLY`. Copy note: *“NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.”* The flag being armed is **not** a fill. Do not treat the dashboard “armed” bit as profit mode.

---

## 1. Live book (this process, mid-scoring)

Source: `GET /api/health`, `/api/overview`, `/api/ingest/status`, `/api/fix/sessions`, `/api/traders`.

| Metric | Value |
|---|---|
| Accounts | **8463** (Achiever 6512 + Starwave ~1951) |
| Achiever deals inserted | **260352** (phase `scoring`, scored climbing through 225+) |
| Starwave deals inserted | **91966** (phase `deals-done`, **scored = 0**) |
| XAU traders with a score | **197** and rising (Achiever only) |
| Traders with ≥3 completed XAU | **178** |
| `SHADOW` | **70** |
| `WATCH` | **79** |
| `RISK_BLOCKED` | **29** (all `martingale=true`) |
| `LIVE` / `LIVE_CANDIDATE` | **0 / 0** |
| `INSUFFICIENT_DATA` | **~8284** |
| SHADOW source PnL sum | **+$78,276** |
| WATCH source PnL sum | **+$8,178** |
| RISK_BLOCKED source PnL sum | **−$241,580** |
| All scored XAU source PnL | **−$154,425** |
| Destination real PnL | **$0** |
| Shadow PnL | **$0** |
| FIX bid/ask | **null** |
| `realCopyEnabled` | **false** |
| SHADOW groups | **100% demo** (`demo\yo-2step` + `demo\yo-payp`) |

Copy-all EV is the XAU book: **negative six figures**. The blocked tail is larger than the SHADOW head.

---

## 2. Why “just send” loses money

### 2.1 There is no sender

`src/Fix.CTrader/Sessions/CTraderFixSession.cs` builds **only** Logon (`35=A`), reads one reply, then `using` disposes `TcpClient`/`SslStream`. Grep of `Fix.CTrader` for `(35, "D")` / `OrderQty` is empty. Hosted service logs `NewOrderSingle still disabled` and sets `_runtime.RealCopyEnabled = false`.

Official cTrader FIX 4.4 (`https://help.ctrader.com/fix/`) **does** define TRADE-session NewOrderSingle. This repo does not implement it. Spotware even lists “trade copiers” as a FIX use-case and then says other Spotware APIs may fit copy better. That is not a license to fire market orders from a one-shot logon probe.

### 2.2 There is no quote tape

`PersistDemoShadowAsync` **returns without shadow fills** if `DestinationQuotes` is empty (`EfTradingStore.cs`). Live FIX cards show `bid=null`, `ask=null`. Risk guards (`QUOTE_STALE`, `SPREAD_TOO_WIDE`, `PRICE_MOVED_TOO_FAR`) cannot run. Shadow engine never marks to Pepperstone.

### 2.3 SHADOW is not a profit filter

Quality formula (`BaselineScorer.Score`):

```text
quality = 50
        + 15 if XAU net PnL > 0
        + 10 if PF >= 1.2
        +  5 if PF >= 1.8
        + 0.20 * behavior
        - 0.25 * risk
SHADOW if quality >= 70 and risk < 40 and trades >= 3
```

Dashboard `netSourcePnl` sums **all symbols**. Score uses **completed XAU only**. That is why ACHIEVER **302252** is `SHADOW` at **95.50** with dashboard PnL **−68.46**, and **303174** is `SHADOW` **95.50** at **−29.38**.

`CanPromoteToLive` is **always false**. Trade #3 is `EARLY_SCORE` / `SHADOW`, never LIVE. Architecture §3: the target is **future destination-net PnL inside risk limits**, not “who made the most in the first 3 trades.”

### 2.4 The “best” SHADOW names are not copyable

| Login | Group | XAU trades | Source PnL | Problem |
|---|---|---:|---:|---|
| 303310 | demo\yo-2step | 22 | +41,634 | `lotEscalation=true`, max **2.0** lots, mixed FX/BTC/XAU, one ticket +13,692. Copying source size blows Pepperstone. |
| 322947 | demo\yo-payp | 194 | +4,950 | Avg hold **~163s**. Gold scalps die in spread + 15s `MaxSourceSignalAge`. |
| 303274 | demo\yo-2step | 102 | +1,228 | 0.05 lot **same-second multi-ticket** grid. First 3 XAU: **−0.35, −55.30, +25.90**. Scorer did not treat grid as averaging. |
| 302252 | demo\yo-2step | 11 | **−68.46** | Still SHADOW 95.50. |

Averaging is only flagged on **scale-in of the same position** at a worse price (`TradeReconstructor.ScaleIn`). Parallel tickets at the same second do **not** set `WasAveragedDown`.

### 2.5 Demo challenge book is adverse selection

Achiever visible book is **6,295 / 6,512** in `demo\yo-2step`. Every current SHADOW row is `demo\yo-2step` or `demo\yo-payp`. These accounts exist to **pass a profit target**, then many martingale the rest (the −$241k blocked bucket). Starwave **real** groups are tens of accounts, not thousands, and Starwave is **unscored**.

### 2.6 Costs the scorer ignores

- Hold time is **computed** (`AverageHoldSeconds`) and **not used** in quality.
- `MaxSlippage = 1.5` exists on `RiskLimits` and is **never read** in `Evaluate`.
- `MaxAllowedSpread = 2.0` on gold is ~$200/lot if the unit is dollars — far too loose.
- `MaxPositionQuantity = 5` lots gold is a blow-up cap, not a working cap.
- `MaxMarginUsage = 0.70` is liquidation territory.
- MFE/MAE is `FeatureQuality.Unavailable` (correct: do not fabricate ticks).
- Source PnL includes MT5 `Profit+Commission+Swap`. Pepperstone spread/commission will be **worse**, especially on 1–3 minute gold.

### 2.7 Persistence cannot host a live book

`DATABASE_URL` is still a placeholder. API uses in-memory EF. Scores vanish on restart. No Postgres SoT, no outbox consumer that emits `35=D`, no QuickFIX/n session (no heartbeat, no seq store, no resend).

---

## 3. The actual profit path (higher profit, lower loss)

Do these in order. Skipping a step to “just send” is how the account dies.

### Stage A — Do not lose (now)

1. Keep `REAL_COPY_EXECUTION_ENABLED=false`.
2. Do not add a `35=D` builder until Stage D.
3. Keep `CanPromoteToLive == false`.
4. Finish scoring **Starwave** and persist to **Postgres**, not memory.

### Stage B — Build a real destination tape (no send)

1. Keep QUOTE TLS **open** (heartbeat `35=0`, not logon-and-dispose).
2. `35=x` SecurityList → store numeric instrument id (cTrader tag 55 is **not** the string `XAUUSD`).
3. `35=V` market-data subscribe → persist bid/ask/age.
4. Then `PersistDemoShadowAsync` can actually fill. Today it no-ops.

### Stage C — Shadow expectancy **after costs**

Eligibility for shadow (not live):

| Gate | Why |
|---|---|
| Not `RISK_BLOCKED` / not martingale | Left tail is −$241k |
| Not demo/contest unless later OOS-proven | Adverse selection |
| Completed XAU **≥ 20** (not 3) | First-3 is luck |
| XAU-only PnL **> 0** and PF **after** a cost haircut | Dashboard all-symbol PnL lies |
| No lot escalation; dest qty after `allocationFactor` ≥ min and **≤ 0.05** lot | 303310 problem |
| Median hold **≥ 15 minutes** | 322947 / 303274 scalps |
| No same-second multi-ticket grid | Scorer hole |
| Fresh destination quote, spread inside a **gold-specific** cap | Guards actually fire |

Only after **30+ shadow days** with **destination** (not source) expectancy > 0 after modeled spread/slippage.

### Stage D — Tiny live, still fail-closed

Only if Stage C is green **and** §68/§70 gates are actually PASS:

1. QuickFIX/n (or equivalent) **living** TRADE session. Persist `ClOrdID` **before** send. Never retry `EXECUTION_STATE_UNKNOWN`.
2. `allocationFactor` **0.01–0.05** of source lots; hard dest cap **0.05** XAU until the live book itself is green.
3. One TRADE owner (fence). Duplicate TRADE = duplicate orders = instant loss.
4. Net XAU cap far below `MaxXauNet=10` at first (start at **0.15–0.30** lot net).
5. Kill switch: daily dest loss **$200–500** (not $2,000) then `STOP_NEW_EXECUTION`. Never flatten the MT5 source.
6. Block copy in the first minutes of high-impact USD events (not in `RiskEngine` today).
7. De-duplicate same-direction gold signals in the same minute (70 SHADOW names are **one gold bet**).

### What “higher profit” is **not**

- Not ML (Phase 6; must beat this baseline OOS; not built).
- Not copying more logins.
- Not raising lot caps.
- Not treating `earlyScore=95.5` as skill.
- Not sending because FIX is `LoggedOn`.

---

## 4. Concrete “do this next” (engineering, still no live send)

1. Persist ingest/scores to Postgres.
2. Finish Starwave `RebuildTraderAsync`.
3. Add dashboard columns: **XAU-only PnL**, median hold, max lot, group, lot-escalation — so 303310 cannot hide inside 91.75.
4. Reject SHADOW if `netSourcePnl < 0` **or** if group starts with `demo\` / `contest\` (config flag).
5. Use hold-time in the score / copy gate.
6. Flag same-second multi-position grids as averaging.
7. Keep a **standing QUOTE** session and store ticks; then shadow PnL becomes a real number.
8. Only then design `GuardedNewOrderSingle` behind the existing flag.

---

## 5. One-line operating law

```text
FIX is LoggedOn for recon/quotes only.
SHADOW on demo is not destination profit.
Scored XAU book is net negative.
35=D stays OFF.
Profit = filter the left tail + tiny size + prove shadow after costs.
Send now = donate the Pepperstone account to gold spread and martingales.
```
