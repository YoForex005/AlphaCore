# W500_RESEARCH_109 — Trade #3 is EARLY_SCORE / SHADOW, never auto LIVE

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_109.md` |
| Slot | **109** |
| Agent | W500 research 109 (senior engineer; trade-#3 state + no-live-copy pin) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (this pass: full re-read of scorer / persist / ingest / FIX / native Manager / census JSON / YoPips `src\`) |
| Assigned | Confirm trade 3 is `EARLY_SCORE` / `SHADOW`, **never auto `LIVE`**. Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** This report plus catalog pins in `SWARM_LOG.md` / `INDEX.md` are the only writes. |
| Test source modified | **No.** |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Census logins not dumped. |
| Binding law | Architecture §§1.4, 15, 22–23, 41, 68–70; `docs/scoring.md`; `docs/architecture.md`; A22 I4–I5; A69 S4–S5 |
| Siblings (same claim; **not** copied as this verdict) | `W500_RESEARCH_9.md`, `W500_RESEARCH_29.md`, `W500_RESEARCH_49.md`, `W500_RESEARCH_69.md`, `W500_RESEARCH_89.md` |
| Method | `read_file` + `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Re-sum `LIVE_GROUPS_AND_TRADERS.json` group `accounts` fields. No shell (no `Get-FileHash`, no `dotnet test`, no Manager re-attach). Localhost API not fetched. Nothing from memory. |

**Honesty rule:** the assigned shorthand “EARLY_SCORE/SHADOW” is incomplete. Implemented `FromBaseline` at `N >= 3` can also land `WATCH` or `RISK_BLOCKED`. The safety sentence that must stay true is **never auto LIVE**. `CanPromoteToLive => false` is a **dead pin** (zero product callers), not a persist gate. A TLS Logon (`35=A`) is not a NewOrderSingle. No-loss “copy to cTrader” today is **absence of a LIVE branch** plus **absence of a `35=D` builder**. Do **not** tick Architecture §68 / §70 PASS from this file.

**One-line:** Trade #3 sets `EarlyScoreEligible` and lands in `{EARLY_SCORE, WATCH, SHADOW, RISK_BLOCKED}` only; `FromBaseline` cannot emit `LIVE` / `LIVE_CANDIDATE`; `CanPromoteToLive(_) => false`; copy rows are `SHADOW_ONLY`; FIX emits `35=A` then disposes the socket; `REAL_COPY` is forced **false**. Capital at risk from this process: **none**.

---

## 0. Verdict (binding)

**CONFIRMED.** Trade #3 is early-score evidence. High quality + low risk defaults to **SHADOW**. Weak quality stays **EARLY_SCORE**. Mid quality is **WATCH**. Martingale/blocked is **RISK_BLOCKED**. **Never auto LIVE.** Catalog fetch is Manager-wide (all groups this login can see + all users in those groups). Live cTrader send is **off** (`SAFE_BY_ABSENCE` + forced flag).

| Claim | Result | Class |
|---|---|---|
| Trade #3 = first official score (`N >= 3`) | **Yes** | `EarlyScoreTradeCount = 3`; `EarlyScoreEligible` iff completed XAU ≥ 3 |
| Event name in product C# | **Partial** | sticky bool `EarlyScoreEligible`, **not** the one-shot `EARLY_SCORE_ELIGIBLE` event |
| High quality + low risk at N=3 | **`SHADOW`** | `quality >= 70 && risk < 40` |
| Mid quality at N=3 | **`WATCH`** | `quality >= 55` (legal; not LIVE) |
| Weak quality at N=3 | **`EARLY_SCORE`** | else after eligible |
| Martingale + DD + net loss (or risk ≥ 80) | **`RISK_BLOCKED`** | never SHADOW/LIVE |
| Auto-promote to `LIVE` / `LIVE_CANDIDATE` | **Impossible** | no branch; pin `CanPromoteToLive => false` |
| `PROVEN_PROFITABLE` token | **Absent** | 0 hits in product `*.cs` |
| Copy intent on score | **SHADOW only** | `Status = "SHADOW_ONLY"`; **0** `new ExecutionIntent` |
| Live `35=D` NewOrderSingle | **Does not exist** | FIX builder is `(35, "A")` only; 0 `35=D` in `D:\Prop\src` |
| `REAL_COPY_EXECUTION_ENABLED` | **false (forced)** | DI constructor + post-logon overwrite + options default |
| Fetch ALL Achiever + Starwave groups/traders | **Yes on the live path** | `GroupRequestArray("*")` + `UserRequestArray` per group; census **18 / 8460** |
| Dummy FakeMt5 on API startup | **Off** | `HasRealPasswords` required; `BrokerCatalogSeed` only |
| Risk to capital if process starts now | **None** | no sender, no MT5 `SendTrade` on the C# connector, no wired `RiskEngine` send |

Do **not** claim A22 R5-before-R6 is implemented. Do **not** claim `CanPromoteToLive` is consulted on persist (Application never calls it). Do **not** claim §68 / §70 PASS.

```text
N<3  → INSUFFICIENT_DATA (unless RISK_BLOCKED)
N>=3 → EARLY_SCORE | WATCH | SHADOW | RISK_BLOCKED
LIVE / LIVE_CANDIDATE ∉ FromBaseline reachable set
CanPromoteToLive(*) == false
CopyIntent.Status == "SHADOW_ONLY" when any copy row is written
CTraderFixSession.BuildLogon tag 35 == "A" only
RealCopyEnabled := false (DI + FIX host)
```

---

## 1. Law (what “trade 3” is allowed to do)

Architecture `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §1.4 (L82–83):

> Do not send a trader to real money immediately after trade #3.  
> The default action after a strong early score should be SHADOW. Live execution should require additional evidence.

§15 (L668–695): count only **3 completed reconstructed XAUUSD position lifecycles**. Trade #3 closure triggers `EARLY_SCORE_ELIGIBLE`, **not** `PROVEN_PROFITABLE`. Orders, fills, partials, SL/TP edits are not trades.

§22 (L940–952) lists the vocabulary (`INSUFFICIENT_DATA` … `LIVE` … `DISQUALIFIED`). Presence of `LIVE` in the vocabulary is **not** a promotion path.

§23 (L956–966):

```text
Trade #3 + high score
        ↓
SHADOW only
```

> Do not automatically send real capital after three trades.

`D:\Prop\docs\scoring.md` L3–7:

```text
Trade #3 completed XAUUSD ⇒ EARLY_SCORE_ELIGIBLE
High quality + low risk ⇒ SHADOW, never LIVE
```

`D:\Prop\docs\architecture.md` L19–21:

```text
REAL_COPY_EXECUTION_ENABLED=false
Trade #3 → SHADOW / EARLY_SCORE, never LIVE
```

UI (`D:\Prop\apps\web\src\pages\TraderDetailPage.tsx` L44):

> First 3 completed XAUUSD trades unlock EARLY_SCORE / SHADOW only. Live promotion is not automatic.

A22 I4–I5 (`D:\Prop\reports\swarm\20260818\A22_scoring_spec.md` L23–25): at `completed_xau_n == 3`, **forbidden** `LIVE` / `LIVE_CANDIDATE`. Trade #3 + high score → **SHADOW only**. Never automatic real capital.

A69 S4–S5 (`D:\Prop\reports\swarm\20260818\A69_trader_states.md` L37–39): at `N == 3`, legal states ⊆ `{EARLY_SCORE, WATCH, SHADOW, PAUSED, RISK_BLOCKED, DISQUALIFIED}`. **Forbidden:** `LIVE`, `LIVE_CANDIDATE`. High score → **SHADOW only**.

---

## 2. State machine (measured this pass)

SUT: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212 lines, full re-read this pass). Prior published SHA-256 `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` (D12/D97). This slot has **no shell**, so SHA was **not** recomputed; the 212-line body matches those pins line-for-line (`EarlyScoreTradeCount = 3`; `FromBaseline` five returns; `CanPromoteToLive => false`).

```40:40:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public const int EarlyScoreTradeCount = 3;
```

`Score()` (L129–171):

1. `ComputeFeatures` keeps only `Completed && IsXauUsd`, ordered by `ClosedAt`.
2. `eligible = features.CompletedXauTrades >= EarlyScoreTradeCount` (hard `3`).
3. Risk / behavior / quality arithmetic uses **no** `LIVE` token.
4. `TraderStateMachine.FromBaseline(eligible, quality, risk, features)` is the only state writer.
5. `SuggestedState = state`; `EarlyScoreEligible = eligible`.

```187:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState FromBaseline(bool earlyEligible, decimal quality, decimal risk, FeatureSnapshot features)
    {
        if (features.CompletedXauTrades == 0)
            return TraderState.INSUFFICIENT_DATA;

        if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
            return TraderState.RISK_BLOCKED;

        if (!earlyEligible)
            return TraderState.INSUFFICIENT_DATA;

        if (quality >= 70 && risk < 40)
            return TraderState.SHADOW;

        if (quality >= 55)
            return TraderState.WATCH;

        return TraderState.EARLY_SCORE;
    }

    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

### 2.1 Reachable set (every `return`)

| Input | `SuggestedState` | Auto LIVE? |
|---|---|---|
| `CompletedXauTrades == 0` | `INSUFFICIENT_DATA` | No |
| `N < 3` and not blocked | `INSUFFICIENT_DATA` | No |
| `N >= 1` and (`risk >= 80` or martingale ∧ DD ∧ net < 0) | `RISK_BLOCKED` | No |
| `N >= 3`, `quality >= 70`, `risk < 40` | **`SHADOW`** | No |
| `N >= 3`, `quality >= 55` (and not the SHADOW pair) | `WATCH` | No |
| `N >= 3`, else | **`EARLY_SCORE`** | No |

Tokens **never returned:** `LIVE`, `LIVE_CANDIDATE`, `PAUSED`, `DISQUALIFIED`.

`TraderState` enum still *contains* `LIVE_CANDIDATE = 4` and `LIVE = 5` (`D:\Prop\src\Domain\Enums\TraderState.cs` L9–10). Those are catalog values for later gates, not outputs of this machine. Dashboard *counts* them (`EfDashboardQueries.cs` L40–41) — a counter is not a writer.

`AfterHighEarlyScore()` is a second SHADOW pin. Product callers of that method this pass: **zero** (`grep` `AfterHighEarlyScore` → definition only). `Score()` does not call it.

### 2.2 `CanPromoteToLive`

| Check | Measured |
|---|---|
| Body | compile-time `false`; parameter discarded |
| Product callers (`src/`, `apps/`) | **Zero** |
| Test callers | **One** (`BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live`) |
| Scratch caller | `_tmp_c23_empty\Program.cs` (not product) |
| Persist consults it | **No** |

This is a **vacuous lock** (“we forgot live”), not A22 R5-before-R6.

`TraderState.LIVE` assignments in product `*.cs`: **none** in `src/` or `apps/`. Only:

- `EfDashboardQueries.GetOverviewAsync` L40–41 — **count** `LIVE_CANDIDATE` / `LIVE`
- `SeedingAndStoreTests` L32 — demo 10001 `Should().NotBe(LIVE)`

### 2.3 Persist copies `SuggestedState` blindly — still cannot be LIVE

```126:144:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore
        {
            // ...
            CurrentState = score.SuggestedState,
            LastScoredAt = DateTimeOffset.UtcNow
        }, ct);

        await _store.PersistDemoShadowAsync(brokerId, login, score.SuggestedState, completedXau, ct);
```

`EfTradingStore.UpsertScoreAsync` copies `score.CurrentState` onto `trader_scores` and history. No `CanPromoteToLive` check. No risk engine. No RBAC.

Because `SuggestedState` cannot be `LIVE` **today**, persisted `CurrentState` cannot become `LIVE`. There is no `if (N == 3) forbid LIVE` gate. If `FromBaseline` later grows `return TraderState.LIVE`, persist will write it.

### 2.4 Unit / integration locks (re-read, not re-run)

`D:\Prop\tests\Unit\BaselineScorerTests.cs`:

| Fact | N | Expected |
|---|---:|---|
| `Two_trades_remain_insufficient` | 2 | `EarlyScoreEligible=false`, `INSUFFICIENT_DATA` |
| `Three_disciplined_winners_go_to_shadow_not_live` | 3 | `EarlyScoreEligible=true`, **`SHADOW`**, `CanPromoteToLive=false` |
| `Martingale_after_losses_is_risk_blocked` | 3 | `RISK_BLOCKED` |

Hand-eval of the N=3 winners fixture (`+80/+70/+90`, lots `0.10`, SL set):

- flags: martingale=false, averaging=false, lot-escalation=false, lot CV=0, SL use=1.0, DD=0
- `risk = 0`
- `behavior = 100`
- `quality = 50 + 15 (net>0) + 10 (PF≥1.2) + 5 (PF≥1.8) + 20 (behavior×0.2) − 0 = 100`
- eligible=true, `quality≥70 && risk<40` → **`SHADOW`**

Matches the assertion. This slot did **not** re-run `dotnet test`.

Martingale fixture (`−100/−200/−400` lots `0.10/0.20/0.40`): martingale true (size-up after loss > 1.25×), DD=700, net=−700 → `RISK_BLOCKED` **before** the quality table. Not LIVE.

Integration `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L31–33: demo login **10001** (`CompletedXauTrades == 3`) `CurrentState.Should().NotBe(LIVE)`. Login **10002** is `RISK_BLOCKED`.

No unit fact locks the `WATCH` / `EARLY_SCORE` branches. Those returns exist in `FromBaseline` and still cannot be LIVE.

---

## 3. What “trade 3” counts (reconstruction)

```60:76:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
    public IReadOnlyList<ReconstructedTradeResult> CompletedXauUsdTrades(...)
        => Reconstruct(...).Where(t => t.Completed && t.IsXauUsd && t.EligibleForFirstThree)...

    public bool IsEarlyScoreEligible(...) =>
        CountCompletedXauUsdTrades(...) >= 3;
```

Canceled buy/sell on a `position_id` marks **every** lifecycle of that id `EligibleForFirstThree = false` (L34–50). Default on `ReconstructedTradeResult` is `EligibleForFirstThree = true` (`ReconstructedTradeResult.cs` L38).

### Honesty: production score does **not** use that helper

`RebuildTraderAsync` filters `Completed && IsXauUsd` only. A canceled-tainted position can still increment `CompletedXauTrades` in the scorer (sibling E024: helper YES / production NO). **It still cannot emit LIVE.** Slot 109 does not treat that as a live-money bug.

`EarlyScoreEligible` is a **sticky bool** (`N >= 3`), not the architecture one-shot event `EARLY_SCORE_ELIGIBLE`. Reconstruction helper `IsEarlyScoreEligible` is unused by persist.

`IMt5BrokerConnector` (`D:\Prop\src\Application\Contracts\Mt5Contracts.cs` L53–63) is **read-only**: Connect / Groups / Accounts / Deals / Positions. No send method on the port.

---

## 4. Persist / shadow copy — SHADOW only, never venue

```267:308:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
        if (state != TraderState.SHADOW)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }
        // destination_quotes required; else ScoreUpdate only
                Status = "SHADOW_ONLY",
```

| Write | When | Live order? |
|---|---|---|
| `OutboxEvent` `ScoreUpdate` | every rebuild | No |
| `CopyIntent` | only `state == SHADOW` **and** a dest quote exists | No (`SHADOW_ONLY`) |
| `ShadowOrder` | same | Simulated fill via `ShadowCopyEngine.SimulateEntry` |
| `ExecutionIntent` | **never** | `grep` `new ExecutionIntent` = **0**; `ExecutionIntents` is a DbSet + ownership flag only |

`ShadowCopyEngine` (`D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`) is **math on a quote**. No socket. Quantity copied is source `MaxVolumeLots` into the **ledger**, not into FIX `OrderQty`.

`RiskEngine.Evaluate` product callers in `src/` + `apps/`: **zero** (unit tests only). `AllowFixSend` is a DTO bit. Even if a future caller appeared, `AllowFixSend` requires `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Unit `Real_flag_false_never_allows_fix_send` keeps `RealExecutionEnabled = false` and asserts `AllowFixSend == false`. Ingest never calls Evaluate.

This-pass `grep` of `SendTrade` / `DealerSend` / `OrderSend` / `PROVEN_PROFITABLE` / `new ExecutionIntent` in `D:\Prop\src` = **0**.

---

## 5. cTrader copy cannot send live orders (no loss)

Independent layers, all fail-closed:

### 5.1 Flag forced false

```38:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
```

FIX host overwrites again after logon (`CTraderFixLogonHostedService.cs` L68): `_runtime.RealCopyEnabled = false`.

`CTraderFixOptions.RealCopyExecutionEnabled` default `= false` (L35).

API `GET /api/settings` (`apps/api/Program.cs` L73–76):

- `REAL_COPY_EXECUTION_ENABLED` = `runtime.RealCopyEnabled` (forced false)
- `FEATURE_COPY_TRADING_ENABLED` = hardcoded `false`

`LiveRuntimeStatus.Snapshot()` when false: `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."`

FIX worker (`apps/fix-worker/Worker.cs` L21–46): reads `CTrader:RealCopyExecutionEnabled` with fallback **false**; even if true, it only **logs a warning** and stamps TRADE `LastError = "No live TRADE socket. NewOrderSingle remains off."` It never builds an order.

Dashboard `GetFixSessionsAsync` (`EfDashboardQueries.cs` L195) hardcodes the last DTO flag **`false`** (execution-enabled bit). A counter of LoggedOn is not a send path.

### 5.2 No `35=D` builder

`CTraderFixSession` is 135 lines. `BuildLogon` (`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` L96) body tags start with `(35, "A")` then 34/49/56/50/57/52/98/108/141/553/554. There is **no** `(35, "D")`, no cancel/replace, no OrderQty. The only `WriteAsync` emits that Logon; `TcpClient`/`SslStream` are `using`/`await using` and dispose before return.

This-pass `grep` on `D:\Prop\src`:

| Pattern | Hits | Meaning |
|---|---:|---|
| `35=D` / `(35, "D")` | **0** | no builder |
| `NewOrderSingle` | comments / logs / `LastError` / helper **name** | no sender |
| `SendTrade` / `DealerSend` / `OrderSend` | **0** | no MT5 send |

`MayRetryNewOrderSingle` is **status math only** (`ExecutionOrderStateMachine.cs` L35–36). No caller sends. It returns true only for `NotSent` / `Rejected` — never after a wire write, and there is no wire write.

### 5.3 Logon is not an order

`CTraderFixLogonHostedService` opens TLS to QUOTE `:5211` and TRADE `:5212`, sends Logon `35=A`, records `LoggedOn`, then the sockets **dispose**. Log line: `"NewOrderSingle still disabled"`. Tag 553 = integer account id. Password is read from env and **not** written here.

Wanting both “copy to cTrader” and “no loss” is satisfied **today** only as:

```text
ALLOW: Manager fetch, reconstruct, score, SHADOW_ONLY ledger, FIX 35=A logon
FORBID: 35=D / 35=F / 35=G, REAL_COPY=true, auto LIVE
```

Live copy + no-loss together is **not** implemented. The honest operating mode is **no live send**.

`LiveCopyPage.tsx` L5 (static, true): copy intents may be SHADOW only; NewOrderSingle disabled so this process cannot open a losing live position.

---

## 6. Fetch ALL Achiever + Starwave groups and ALL manager traders

### 6.1 Code path (current)

`LiveIngestHostedService` + `POST /api/ops/resync` call `SyncCatalogAsync`:

```44:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`GetAccountsAsync(null)` walks **every** group (`NativeMt5BrokerConnector.GetAccountsCore` L189–213):

1. Groups: `GroupRequestArray("*")` (L155); if empty, fallback `GroupTotal` / `GroupNext` (L174–181).
2. Per group: `UserRequestArray(gname)` (L223); if retcode bad, `UserGetByGroup`; if still empty, `UserLogins` + `UserRequestByLogins`.
3. Dedup by login.

Dummy substitution is refused (`LiveIngestHostedService` L70): `"No dummy data will be substituted."`

DI throws without real `MT5_PASSWORD` **and** `MT5_STARWAVEFX_PASSWORD` (`LiveMt5Registration.HasRealPasswords`). Connectors are **native**, not `FakeMt5BrokerConnector`. API startup seeds catalog rows only (`BrokerCatalogSeed.EnsureAsync` at `Program.cs` L153).

Achiever uses optional HTTP proxy (`ACHIEVER_PROXY_*`). Starwave `ProxyEnabled = false` hard pin (`LiveMt5Registration.cs` L45).

Plan-group mappings are **labels, not fetch filters** (`docs/architecture.md` L24).

Dashboard `GetTradersAsync` iterates **all** `Mt5Accounts` (`EfDashboardQueries.cs` L99–120). Unscored logins paint `INSUFFICIENT_DATA`. That is the ALL-trader catalog surface. Fetching/listing them does **not** promote anyone to LIVE send.

### 6.2 Scoring is not the same as fetch (do not greenwash)

| Path | Logins scored |
|---|---|
| Hosted ingest (`LiveIngestHostedService` L106–113) | `ListLoginsWithDealsAsync` — **deal-bearing only** |
| Manual `POST /api/ops/resync` (`Program.cs` L131–136) | `ListLoginsAsync` — **all catalog logins** |
| Residual `apps/mt5-worker/Worker.cs` L31 | hard-coded `{10001,10002,10003,99001}` only |

Fetch of ALL groups/traders is **not** gated by `REAL_COPY`. Scoring a subset does **not** drop them from `/api/traders`. The four-login worker loop is a leftover dummy scorer; hosted API ingest is the live path.

### 6.3 Measured live census (re-summed this pass; not re-attached)

Source: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe UTC: **2026-08-18T08:42:16.8519545+00:00** (`LiveBrokerProbe`).  
Companion: `CREDENTIALS_AND_COPY_STATUS.md` (names only).  
This slot **did not re-attach** Manager. Counts below are from that on-disk JSON, group `accounts` fields re-added.

| Broker | Connect | Groups | Traders | Open positions | Path |
|---|---|---:|---:|---:|---|
| ACHIEVER | OK via HTTP proxy | **8** | **6512** | 1506 | `GroupRequestArray` + `UserRequestArray` |
| STARWAVEFX | OK direct | **10** | **1948** | 478 | same |
| **Total** | | **18** | **8460** | **1984** | |

Achiever groups (2+179+4+5+4+6295+0+23 = **6512**):

| Group | Accounts |
|---|---:|
| contest\yo-1step | 2 |
| contest\yo-2step | 179 |
| contest\yo-instant | 4 |
| contest\yo-payp | 5 |
| demo\yo-1step | 4 |
| demo\yo-2step | 6295 |
| demo\yo-instant | 0 |
| demo\yo-payp | 23 |

Starwave groups (11+4+170+1735+22+0+0+4+0+2 = **1948**):

| Group | Accounts |
|---|---:|
| Starwave\cent\FX1\grp1 | 11 |
| Starwave\cent\FX1\grp2 | 4 |
| Starwave\demo\FX2\grp1 | 170 |
| Starwave\demo\FX2\grp2 | 1735 |
| Starwave\real\FX3\grp1 | 22 |
| Starwave\real\FX3\grp2 | 0 |
| Starwave\real\FX3\grp3 | 0 |
| Starwave\real\FX3\grp4 | 4 |
| Starwave\real\FX3\grp5 | 0 |
| Starwave\real\FX3\LP | 2 |

**Honesty:** these are **all groups this manager login can see**. If the server has more groups, they are outside this ACL. Zero-account groups **are** listed (fetch is not filtered to non-empty). JSON contains the full login list; this report does not dump logins. A Starwave **source** group named `Starwave\real\FX3\LP` is not evidence that Pepperstone/cTrader is an LP.

CREDENTIALS pin (names only): `.env` present; Achiever + StarwaveFX passwords present; Achiever HTTP proxy present; `CTRADER_FIX_PASSWORD` present; `DATABASE_URL` still placeholder → API uses in-memory DB.

Do **not** confuse this 18/8460 catalog with E031’s demo overview (`shadow=2`, `riskBlocked=1`, `live=0` on Fake tape 10001/10002/99001). That capture is a **different process/book**. Both agree on **0 LIVE**.

Fake demo (not the live census): Achiever 3 groups / 3 logins; Starwave 1 group / 1 login. 10001 three XAU winners → SHADOW; 10002 martingale → RISK_BLOCKED; 10003 empty → INSUFFICIENT_DATA; 99001 three winners → SHADOW. Used by tests only; DI refuses Fake when passwords exist.

### 6.4 YoPips C++ (related Manager API, not the copy path)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` this-pass `grep` for `35=D` / `cTrader` / `cServer` / `NewOrderSingle` / `FIX.4` = **0**.

That tree **does** implement MT5 `SendTrade` (`mt5_manager.cpp`, `trade_execution_service.cpp`) for the **prop-firm challenge** backend. It is **not** wired into `D:\Prop` DI, not a cTrader FIX sender, and is not invoked by trade-#3 scoring. Do not treat YoPips group totals as this product’s census.

`LiveBrokerProbe` (`D:\Prop\tools\LiveBrokerProbe\Program.cs`) is **read-only**: Connect → `GetGroupsAsync` → `GetAccountsAsync(null)` → optional positions. No FIX. No score. No send.

---

## 7. End-to-end: fetch → score trade #3 → no live order

```text
NativeMt5BrokerConnector (Achiever proxy + Starwave direct)
    GroupRequestArray("*") + UserRequestArray(group)
        → SyncCatalogAsync (all groups, all logins)
        → SyncBrokerAsync (deals / positions; read-only)
        → RebuildTraderAsync for deal-bearing (hosted) or all (resync)
              BaselineScorer.Score
                  N<3  → INSUFFICIENT_DATA (or RISK_BLOCKED)
                  N>=3 → EARLY_SCORE | WATCH | SHADOW | RISK_BLOCKED
                  NEVER LIVE / LIVE_CANDIDATE
              PersistDemoShadowAsync
                  SHADOW + dest quote → CopyIntent Status=SHADOW_ONLY + ShadowOrder math
                  else ScoreUpdate outbox only
        → CTraderFixSession.TryLogonAsync 35=A on :5211/:5212 then dispose
        → RealCopyEnabled stays false
        → no 35=D exists to fire
```

---

## 8. What this slot did **not** do

- Did not edit product or test source.
- Did not print secrets or dump 8460 logins.
- Did not re-attach Achiever/Starwave Manager.
- Did not recompute SHA-256 (no shell).
- Did not re-run `dotnet test`.
- Did not GET `127.0.0.1:5000`.
- Did not send, or attempt to send, a cTrader order.

---

## 9. Slot-109 verdict

**CONFIRMED.** Trade #3 is `EARLY_SCORE` / `SHADOW` (also legally `WATCH` / `RISK_BLOCKED`). It is **never auto LIVE**. ALL manager-visible Achiever + Starwave groups and traders are on the fetch path (measured 8+10 / 6512+1948). Copy to cTrader cannot place a live order from this process (`SAFE_BY_ABSENCE` + `RealCopyEnabled=false`). **Risk to capital: none.**

---

## 10. Sources

- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`
- `D:\Prop\src\Domain\Enums\TraderState.cs`
- `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs`
- `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs`
- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\web\src\pages\TraderDetailPage.tsx`
- `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx`
- `D:\Prop\tests\Unit\BaselineScorerTests.cs`
- `D:\Prop\tests\Unit\RiskEngineTests.cs`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\docs\scoring.md`
- `D:\Prop\docs\architecture.md`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` (0 cTrader / `35=D` senders)
