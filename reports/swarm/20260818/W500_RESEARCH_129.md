# W500_RESEARCH_129 — Trade #3 is EARLY_SCORE / SHADOW, never auto LIVE

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_129.md` |
| Slot | **129** |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (this pass: full re-read of scorer / persist / ingest / copy hosted loop / FIX / native Manager / census JSON / YoPips `src\`) |
| Assigned | Confirm trade 3 is `EARLY_SCORE` / `SHADOW`, **never auto `LIVE`**. Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** This report is the only product-adjacent write (plus catalog pins in `SWARM_LOG.md` / `INDEX.md`). |
| Test source modified | **No.** |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Census logins not dumped. |
| Binding law | Architecture §§1.4, 15, 22–23, 41, 68–70; `docs/scoring.md`; `docs/architecture.md`; A22 I4–I5; A69 S4–S5 |
| Siblings (same claim; **not** copied as this verdict) | `W500_RESEARCH_9.md`, `W500_RESEARCH_29.md`, `W500_RESEARCH_49.md`, `W500_RESEARCH_69.md`, `W500_RESEARCH_89.md`, `W500_RESEARCH_109.md` |
| Method | `read_file` + `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Re-sum `LIVE_GROUPS_AND_TRADERS.json` group `accounts` fields. No shell (no `Get-FileHash`, no `dotnet test`, no Manager re-attach). Localhost API not fetched. Nothing from memory. |

**Honesty rule:** the assigned shorthand “EARLY_SCORE/SHADOW” is incomplete. Implemented `FromBaseline` at `N >= 3` can also land `WATCH` or `RISK_BLOCKED`. The sentence that must stay true is **never auto LIVE**. `CanPromoteToLive => false` is a **dead pin** (zero product callers), not a persist gate. A TLS Logon (`35=A`) is not a NewOrderSingle. Slot 109’s “`REAL_COPY` forced false in DI + post-logon overwrite” is **stale**. Today the flag **follows env** and `.env` sets it **true**. No-loss “copy to cTrader” is still **absence of a `35=D` builder** plus **const `NewOrderSingleImplemented=false` / `VenueReconciled=false`**. Do **not** tick Architecture §68 / §70 PASS.

**One-line:** Trade #3 sets `EarlyScoreEligible` and lands in `{EARLY_SCORE, WATCH, SHADOW, RISK_BLOCKED}` only; `FromBaseline` cannot emit `LIVE` / `LIVE_CANDIDATE`; `CanPromoteToLive(_) => false`; copy rows stay `SHADOW_ONLY` (or `LIVE_SEND_BLOCKED_UNIMPLEMENTED` on a future LIVE hop that still cannot exist today); FIX emits `35=A` then disposes the socket. Capital at risk from this process: **none**.

---

## 0. Verdict (binding)

**CONFIRMED.** Trade #3 is early-score evidence. High quality + low risk defaults to **SHADOW**. Weak quality stays **EARLY_SCORE**. Mid quality is **WATCH**. Martingale/blocked is **RISK_BLOCKED**. **Never auto LIVE.** Catalog fetch is Manager-wide (all groups this login can see + all users in those groups). Live cTrader send is **off** (`SAFE_BY_ABSENCE`). The env flag can now paint **armed**; that paint is **not** a sender.

| Claim | Result | Class |
|---|---|---|
| Trade #3 = first official score (`N >= 3`) | **Yes** | `EarlyScoreTradeCount = 3`; `EarlyScoreEligible` iff completed XAU ≥ 3 |
| Event name in product C# | **Partial** | sticky bool `EarlyScoreEligible`, **not** the one-shot `EARLY_SCORE_ELIGIBLE` event |
| High quality + low risk at N=3 | **`SHADOW`** | `quality >= 70 && risk < 40` |
| Mid quality at N=3 | **`WATCH`** | `quality >= 55` (legal; not LIVE) |
| Weak quality at N=3 | **`EARLY_SCORE`** | else after eligible |
| Martingale + DD + net loss (or risk ≥ 80) | **`RISK_BLOCKED`** | never SHADOW/LIVE |
| Auto-promote to `LIVE` / `LIVE_CANDIDATE` | **Impossible today** | no `FromBaseline` branch; pin `CanPromoteToLive => false` unused |
| `PROVEN_PROFITABLE` token | **Absent** | 0 hits in product `*.cs` |
| Copy intent on score rebuild | **SHADOW only** | `PersistDemoShadowAsync` writes `Status = "SHADOW_ONLY"` or nothing |
| Hosted copy loop | **SHADOW ledger** | `CopyTradingService.GenerateShadowIntentsAsync`; `AllowFixSend` record hardcoded `false` |
| Live `35=D` NewOrderSingle | **Does not exist** | FIX builder is `(35, "A")` only; 0 `35=D` in product C# |
| `REAL_COPY_EXECUTION_ENABLED` | **Env-driven (drift)** | DI L41 reads config; `.env` key is `true`; FIX host **no longer** overwrites false. Still no sender. |
| Fetch ALL Achiever + Starwave groups/traders | **Yes on the live path** | `GroupRequestArray("*")` + `UserRequestArray` per group; census **18 / 8460** |
| Dummy FakeMt5 on API startup | **Off** | `HasRealPasswords` required; `BrokerCatalogSeed` only |
| Risk to capital if process starts now | **None** | no `35=D`, no `ExecutionIntent` writer, no MT5 `SendTrade` on the C# connector |

Do **not** claim A22 R5-before-R6 is implemented. Do **not** claim `CanPromoteToLive` is consulted on persist. Do **not** claim §68 / §70 PASS. Do **not** repeat W500_109 “flag forced false” as current tree truth.

```text
N<3  → INSUFFICIENT_DATA (unless RISK_BLOCKED)
N>=3 → EARLY_SCORE | WATCH | SHADOW | RISK_BLOCKED
LIVE / LIVE_CANDIDATE ∉ FromBaseline reachable set
CanPromoteToLive(*) == false
CopyIntent.Status == "SHADOW_ONLY" (rebuild + hosted loop today)
CTraderFixSession.BuildLogon tag 35 == "A" only
NewOrderSingleImplemented == false
VenueReconciled == false
RealCopyEnabled := (env == "true")   // can be true; still no sender
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

A22 I4–I5 (`A22_scoring_spec.md` L24–25): at `N == 3`, **forbidden** states are `LIVE` and `LIVE_CANDIDATE`. High score → **SHADOW only**.

A69 S4–S5: at `N == 3`, legal states ⊆ `{EARLY_SCORE, WATCH, SHADOW, PAUSED, RISK_BLOCKED, DISQUALIFIED}`. **Forbidden:** `LIVE`, `LIVE_CANDIDATE`. High score → **SHADOW only**.

---

## 2. State machine (measured this pass)

SUT: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212 lines, full re-read this slot). Enum: `D:\Prop\src\Domain\Enums\TraderState.cs`.

```40:40:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public const int EarlyScoreTradeCount = 3;
```

`Score()` (L129–171) computes features, sets `eligible = CompletedXauTrades >= 3`, then `state = TraderStateMachine.FromBaseline(...)`. Formulas contain **no** `LIVE` token.

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
| `N >= 3`, `quality >= 70`, `risk < 40` | **`SHADOW`** | No |
| `N >= 3`, `quality >= 55` | `WATCH` | No |
| `N >= 3`, else | **`EARLY_SCORE`** | No |
| `risk >= 80` or (martingale ∧ DD ∧ net < 0) | `RISK_BLOCKED` | No |

Tokens **never returned:** `LIVE`, `LIVE_CANDIDATE`, `PAUSED`, `DISQUALIFIED`.

`TraderState` enum still *contains* `LIVE_CANDIDATE = 4` and `LIVE = 5` (`TraderState.cs` L9–10). Those are catalog values for later gates, not outputs of this machine. Dashboard *counts* them (`EfDashboardQueries.cs` L40–41). `CopyTradingService` *reads* them as a future copyable set (L95). A counter / filter is not a writer.

This-pass `grep` of product `*.cs` under `D:\Prop\src`:

| Token | Product writers | Notes |
|---|---|---|
| `SuggestedState = state` | `BaselineScorer.Score` only | `state` is `FromBaseline` |
| `TraderState.LIVE` assignment | **0** | enum + dashboard count + copy status count + live-branch guard |
| `LIVE_CANDIDATE` assignment | **0** | enum + dashboard count + copyable filter |
| `AfterHighEarlyScore` | definition only | **0** product callers |
| `CanPromoteToLive` | definition only | **0** product callers; 1 unit fact |
| `PROVEN_PROFITABLE` | **0** | not in product C# |
| `new ExecutionIntent` / `ExecutionIntents.Add` | **0** | entity exists; no writer |

### 2.2 `CanPromoteToLive` is a vacuous lock

| Check | Measured |
|---|---|
| Body | compile-time `false`; parameter discarded |
| Product callers (`src/`, `apps/`) | **Zero** |
| Test callers | **One** (`BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live`) |
| Persist consults it | **No** |

This is “we forgot live”, not A22 R5-before-R6. If a later edit taught `FromBaseline` to return `LIVE`, `ReconstructionScoringService` would persist it. Today that cannot happen because there is no such branch.

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

Because `SuggestedState` cannot be `LIVE`, `CurrentState` cannot become `LIVE` **today**. There is no `if (N == 3) forbid LIVE` gate and no CHECK constraint.

### 2.4 Unit / integration locks (re-read, not re-run)

`D:\Prop\tests\Unit\BaselineScorerTests.cs`:

| Fact | N | Expected |
|---|---:|---|
| `Two_trades_remain_insufficient` | 2 | `EarlyScoreEligible=false`, `INSUFFICIENT_DATA` |
| `Three_disciplined_winners_go_to_shadow_not_live` | 3 | `EarlyScoreEligible=true`, **`SHADOW`**, `CanPromoteToLive=false` |
| `Martingale_after_losses_is_risk_blocked` | 3 | `RISK_BLOCKED` |

Hand-eval of the N=3 winners fixture (`+80/+70/+90`, lots `0.10`, SL set):

- `net=240`, `grossLoss=0` → `ProfitFactor=99`
- flags all false, `SlUseRate=1`, `MaxDrawdown=0`
- `risk=0`, `behavior=100`
- `quality = 50 + 15 + 10 + 5 + 20 − 0 = 100`
- `quality >= 70 && risk < 40` → **`SHADOW`**

Matches the assertion. This slot did **not** re-run `dotnet test`.

Martingale fixture (`−100/−200/−400` lots `0.10/0.20/0.40`): `Martingale=true`, `MaxDrawdown>0`, `NetPnl<0` → **`RISK_BLOCKED`** even if the additive risk sum is `< 80`.

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

Canceled buy/sell on a `position_id` marks **every** lifecycle of that id `EligibleForFirstThree = false` (L34–50).

### Honesty: production score does **not** use that helper

`RebuildTraderAsync` filters `Completed && IsXauUsd` only. A canceled-tainted position can still increment `CompletedXauTrades` in the scorer (sibling E024: helper YES / production NO). **It still cannot emit LIVE.** Slot 129 does not treat that as a live-money bug.

`EarlyScoreEligible` is a **sticky bool** (`N >= 3`), not the architecture one-shot event `EARLY_SCORE_ELIGIBLE`.

---

## 4. Persist / shadow copy — SHADOW only, never venue

### 4.1 Rebuild persist (`PersistDemoShadowAsync`)

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
| `ExecutionIntent` | **never** | `grep` `new ExecutionIntent` / `ExecutionIntents.Add` = **0** |

`ShadowCopyEngine` (`D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`) is **math on a quote**. No socket. Quantity is source lots into a **ledger** row, not a FIX `OrderQty`.

`EARLY_SCORE` / `WATCH` / `RISK_BLOCKED` / `INSUFFICIENT_DATA` traders get **no** `CopyIntent` / `ShadowOrder` from this method.

### 4.2 Hosted copy loop (NEW vs slots 49/89/109 — do not copy those “0 Evaluate callers”)

`CopyTradingHostedService` ticks every 20s and calls `CopyTradingService.GenerateShadowIntentsAsync`.

Measured constants (`CopyTradingService.cs` L15–17):

| Constant | Value |
|---|---|
| `VenueReconciled` | **`false`** |
| `NewOrderSingleImplemented` | **`false`** |
| `AllocationFactor` | `0.05m` |

Copyable filter (L95): `{SHADOW, LIVE_CANDIDATE, LIVE}`. Because persist never writes the last two, the loop only sees **SHADOW** scores today.

Risk hop **does** exist now (`_risk.Evaluate(...)` L159–183). W500_59 / W500_99 “0 product Evaluate callers” is **stale**. Then the **record** overwrites the engine bit:

```192:205:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    AllowFixSend = false,
                    DecidedAt = now
                };
                // ...
                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
                else
                {
                    intent.Status = "SHADOW_ONLY";
```

Even the hypothetical “all gates open” branch **still does not send**. It stamps `LIVE_SEND_BLOCKED_UNIMPLEMENTED`. No `ExecutionIntent`. No socket write.

Additionally, `VenueReconciled` is a compile-time false, so `RiskEngine.Evaluate` rejects increasing actions at L84–85 (`VENUE_NOT_RECONCILED`) before `AllowFixSend` can become true. OpenExposure intents therefore cannot reach `Approve` + `AllowFixSend=true` on this path.

`GetStatusAsync` summary when any blocker is present (always, because NOS is unimplemented): `"Copy pipeline ON. Shadow intents only. Pepperstone will not receive NewOrderSingle."`

---

## 5. cTrader copy cannot send live orders (no loss)

Independent layers, all fail-closed:

### 5.1 Flag is **no longer** forced false (drift vs W500_68 / 89 / 108 / 109)

`D:\Prop\src\Infrastructure\DependencyInjection.cs` L39–42:

```csharp
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`CTraderFixLogonHostedService` L68–70 **logs** `RealCopyArmed={Armed}` and does **not** assign `_runtime.RealCopyEnabled = false`.

`apps/api/Program.cs` L10 loads dotenv (`EnvFile.FindAndLoad()`, hardcoded fallback `D:\Prop\.env`) **before** `CreateBuilder`, then `AddEnvironmentVariables()`. The key `REAL_COPY_EXECUTION_ENABLED` in that file is **`true`**. Value of neighboring secrets is **not** written here.

`CTraderFixOptions.RealCopyExecutionEnabled` default remains `false` (POCO L35). The API host does **not** bind that POCO for the runtime flag; it uses `LiveRuntimeStatus` from env.

API `GET /api/settings` L74–77:

- `REAL_COPY_EXECUTION_ENABLED` = `runtime.RealCopyEnabled` (**can be true**)
- `FEATURE_COPY_TRADING_ENABLED` = hardcoded **`true`** (slot 89 said false — stale)

`LiveRuntimeStatus.Snapshot()` if the flag is true: `"REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent."`

FIX worker (`apps/fix-worker/Worker.cs` L21–46): reads `CTrader:RealCopyExecutionEnabled` with fallback **false**; even if true, it only **logs a warning** and stamps TRADE `LastError = "No live TRADE socket. NewOrderSingle remains off."` It never builds an order. It also overwrites session rows to `Disconnected` (status paint, not a send).

Flipping env to true **still does not** place an order: there is no builder. Do **not** enable it as a go-live act. Do **not** claim the flag is pinned false anymore.

### 5.2 No `35=D` builder

`CTraderFixSession.BuildLogon` body tags start with `(35, "A")` then 34/49/56/50/57/52/98/108/141/553/554. There is **no** `(35, "D")`, no cancel/replace, no OrderQty. One `WriteAsync` of that logon, then `using` TcpClient/SslStream **dispose**.

This-pass search of product C# under `D:\Prop\src`:

| Pattern | Hits | Meaning |
|---|---:|---|
| `35=D` / `(35, "D")` | **0** | no builder |
| `NewOrderSingle` | comments / logs / `LastError` / helper **name** / const | no sender |
| `SendTrade` / `DealerSend` / `OrderSend` on C# connector | **0** | `IMt5BrokerConnector` is Connect/Groups/Accounts/Deals/Positions only |

`MayRetryNewOrderSingle` is **status math only** (`ExecutionOrderStateMachine.cs` L35–36). No caller sends.

Vendor / `mt5-sdk` `DealerSend` / `SendTrade` exist in the **preserved C++ SDK** and in YoPips challenge-terminal code. They are **not** called from `AddTraderIntelligence`, `NativeMt5BrokerConnector`, FIX logon, or shadow persist.

### 5.3 Logon is not an order

`CTraderFixLogonHostedService` opens TLS to QUOTE `:5211` and TRADE `:5212`, sends Logon `35=A`, records `LoggedOn`, then disposes. Log line: `"NewOrderSingle still unimplemented"`. Tag 553 = integer account id. Password is read from env and **not** written here.

`CTraderQuoteService` parses SecurityList / MD in memory. It never opens a socket. QuickFIX/n is **not** referenced (prior C19/D52). Official engine absence is another reason send is impossible.

Wanting both “copy to cTrader” and “no loss” is satisfied **today** only as:

```text
ALLOW: Manager fetch, reconstruct, score, SHADOW_ONLY ledger, FIX 35=A logon
FORBID: 35=D / 35=F / 35=G, auto LIVE
```

Live copy + no-loss together is **not** implemented. The honest operating mode is **no live send**.

UI (`LiveCopyPage.tsx`): static amber copy that NOS is disabled and gates still required.

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

`GetAccountsAsync(null)` walks **every** group (`NativeMt5BrokerConnector.GetAccountsCore`):

1. Groups: `GroupRequestArray("*")` (L155); if empty, fallback `GroupTotal` / `GroupNext`.
2. Per group: `UserRequestArray(gname)` (L223); if retcode bad, `UserGetByGroup`; if still empty, `UserLogins` + `UserRequestByLogins`.
3. Dedup by login.

No `.Take(200)` on catalog. Plan-group mappings are **labels, not fetch filters** (`docs/architecture.md` L24). Dummy substitution is refused (`LiveIngestHostedService` L70): `"No dummy data will be substituted."`

DI throws without real `MT5_PASSWORD` **and** `MT5_STARWAVEFX_PASSWORD` (`LiveMt5Registration.HasRealPasswords`). Connectors are **native**, not `FakeMt5BrokerConnector`. API startup seeds catalog rows only (`BrokerCatalogSeed.EnsureAsync`). Starwave `ProxyEnabled = false` hardcoded (`LiveMt5Registration.cs` L45).

Dashboard `GetTradersAsync` iterates **all** `Mt5Accounts` (L99–120). Unscored logins paint `INSUFFICIENT_DATA`. That is the ALL-trader catalog surface. Fetching / listing them does **not** promote anyone to LIVE.

### 6.2 Scoring is not the same as fetch (do not greenwash)

| Path | Logins scored |
|---|---|
| Hosted ingest (`LiveIngestHostedService` L106–113) | `ListLoginsWithDealsAsync` — **deal-bearing only** |
| Manual `POST /api/ops/resync` (`Program.cs` L134–140) | `ListLoginsAsync` — **all catalog logins** |
| `apps/mt5-worker/Worker.cs` L31–35 | **hardcoded** `{10001,10002,10003,99001}` only |

Fetch of ALL groups/traders is **not** gated by `REAL_COPY`. Scoring a subset does **not** drop them from `/api/traders`. The **API** host is the ALL-traders catalog + (deals-only) scorer. `mt5-worker` is **not** the ALL-traders scorer.

### 6.3 Measured live census (re-summed this pass; not re-attached)

Source: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`
Probe UTC: **2026-08-18T08:42:16.8519545+00:00** (`LiveBrokerProbe`).
Companion: `LIVE_MANAGER_FETCH_MEASURED.md`, `CREDENTIALS_AND_COPY_STATUS.md` (the latter still says “REAL_COPY forced false” — **stale vs this tree**).
This slot **did not re-attach** Manager.

Header fields + per-group `accounts` re-added independently:

| Broker | Connect | Groups | Traders | Open positions | Path |
|---|---|---:|---:|---:|---|
| ACHIEVER | `connected: true` (HTTP proxy on the live path) | **8** | **6512** | 1506 | `GroupRequestArray` + `UserRequestArray` |
| STARWAVEFX | `connected: true` (direct) | **10** | **1948** | 478 | same |
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

`LiveBrokerProbe` is **read-only**: Connect → `GetGroupsAsync` → `GetAccountsAsync(null)` → optional positions. No FIX. No score. No send. Note in JSON: `"Passwords never written."`

### 6.4 YoPips C++ (related Manager API, not the copy path)

Searched `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` for `EARLY_SCORE`, `SHADOW`, `CanPromoteToLive`, `NewOrderSingle`, `35=D`, `cTrader`, `Pepperstone`: **0 hits** (this-pass grep).

That tree is the challenge-firm backend (admin, payouts, web terminal). `copy_trade_*` tables there are **detection / restriction** of challenge-account copy-trading, not a cTrader copier.

That backend **can** place live **MT5** challenge-terminal orders (different process, different venue). Nothing in `D:\Prop` `AddTraderIntelligence` / FIX logon / shadow persist calls into that binary. Copy-to-cTrader no-loss is decided entirely inside `D:\Prop`. Do not start YoPips as part of this fetch/score.

---

## 7. End-to-end: fetch → score trade #3 → no live order

```text
NativeMt5BrokerConnector (Achiever proxy + Starwave direct)
    GroupRequestArray("*") + UserRequestArray(group)
        → SyncCatalogAsync (all groups, all logins)
        → SyncBrokerAsync (deals / positions; read-only)
        → RebuildTraderAsync for deal-bearing (hosted) or all (resync)
              BaselineScorer.Score
                  N<3  → INSUFFICIENT_DATA
                  N>=3 → EARLY_SCORE | WATCH | SHADOW | RISK_BLOCKED
                  NEVER LIVE / LIVE_CANDIDATE
              PersistDemoShadowAsync
                  SHADOW + dest quote → CopyIntent Status=SHADOW_ONLY + ShadowOrder math
                  else ScoreUpdate outbox only
        → CopyTradingHostedService.GenerateShadowIntentsAsync
              Evaluate (VenueReconciled=false → reject increasing)
              AllowFixSend record = false
              Status = SHADOW_ONLY
              no ExecutionIntent
        → CTraderFixSession.TryLogonAsync 35=A on :5211/:5212 then dispose
        → RealCopyEnabled may be true from env
        → no 35=D exists to fire
```

---

## 8. Residual risks (honest, not greenwash)

1. **Blind persist.** `CurrentState = SuggestedState` with no `if (N==3) forbid LIVE`. Safety is the scorer’s reachable set, not a persist CHECK constraint.
2. **Vacuous `CanPromoteToLive`.** Dead API. Do not market it as the go-live gate.
3. **`SAFE_BY_ABSENCE`.** Adding a `35=D` builder without §68/§70 PASS would be the first real capital-loss path. Do not add it in this wave.
4. **Env flag can be true.** DI honors `REAL_COPY_EXECUTION_ENABLED=true`. W500_68/108/109 “pinned false” is stale. Still no sender; do not treat the paint as a gate.
5. **Copyable set includes LIVE.** `GenerateShadowIntentsAsync` would accept a LIVE score if persist ever wrote one. The inner live branch is still `LIVE_SEND_BLOCKED_UNIMPLEMENTED`.
6. **`RiskEngine` is now on the product path** (hosted copy). That is progress toward A23, not a send path. `AllowFixSend` on the persisted record is hardcoded false.
7. **Hosted score is deals-only; `mt5-worker` is a 4-login loop.** Catalog is still ALL groups/users.
8. **First-3 helper unused on the production score path.** Can inflate `N` with canceled-tainted positions. Still cannot emit LIVE.
9. **This slot did not re-measure** live Manager or live FIX logon. Census 18/8460 is the 08:42Z probe file, re-summed.
10. **C++ YoPips backend** can still lose challenge-account money on **MT5** if that service is used as a trader terminal. Out of scope for this copy-to-cTrader pin.

---

## 9. What this slot did **not** do

- Did not edit product or test source.
- Did not print secrets or dump 8460 logins.
- Did not re-attach Achiever/Starwave Manager.
- Did not recompute SHA-256 (no shell).
- Did not re-run `dotnet test`.
- Did not GET `127.0.0.1:5000`.
- Did not send, or attempt to send, a cTrader order.

---

## 10. Slot-129 goal matrix

Goal: fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders yet (no loss).

| Goal piece | Status |
|---|---|
| Fetch ALL groups | **Code YES** (`GroupRequestArray("*")`); **last measured 18** |
| Fetch ALL manager traders | **Code YES** (`GetAccountsAsync(null)`); **last measured 8460** |
| Score them | **API host YES** for deal-bearing / resync-all; **mt5-worker NO** (4 demo logins) |
| Trade #3 → EARLY_SCORE / SHADOW (or WATCH / RISK_BLOCKED) | **YES** |
| Trade #3 → auto LIVE | **NO** |
| Live cTrader orders | **NO** (`35=D` missing; NOS const false; SHADOW_ONLY ledger only) |
| `REAL_COPY` paint | **May be true** (env). Not a sender. |
| Risk to cTrader capital from this process | **None** |

**Slot 129 verdict: CONFIRMED.** Trade #3 is `EARLY_SCORE` / `SHADOW` (also legally `WATCH` / `RISK_BLOCKED`). It is **never auto LIVE**. ALL manager-visible Achiever + Starwave groups and traders are on the fetch path (measured 8+10 / 6512+1948). Copy to cTrader cannot place a live order from this process (`SAFE_BY_ABSENCE` + `NewOrderSingleImplemented=false`). **Risk to capital: none.**
