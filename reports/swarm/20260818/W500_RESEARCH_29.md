# W500_RESEARCH_29 — Trade #3 is EARLY_SCORE / SHADOW, never auto LIVE

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_29.md` |
| Slot | **29** |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (re-read product source + prior live census JSON) |
| Assigned | Confirm trade #3 is `EARLY_SCORE` / `SHADOW`, **never auto `LIVE`**. Fetch ALL Achiever + Starwave groups and ALL manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Secrets printed | **No.** |
| Binding law | Architecture §§1.4, 15, 22–23, 41, 68–70; `docs/scoring.md`; `docs/architecture.md`; A22 I4–I5; A69 S4–S5; D12 / D97 / E002 / E007 / E034 |

**One-line:** Trade #3 unlocks `EarlyScoreEligible` and lands in `{EARLY_SCORE, WATCH, SHADOW, RISK_BLOCKED}` only; `FromBaseline` cannot emit `LIVE`/`LIVE_CANDIDATE`; `CanPromoteToLive(_) => false`; copy rows are `SHADOW_ONLY`; FIX emits `35=A` logon only; `35=D` does not exist; `REAL_COPY` is forced **false**. Capital at risk from this process: **none**.

---

## 0. Verdict (binding)

**CONFIRMED.** Trade #3 is early-score evidence, default **SHADOW** when quality is high, and **never auto LIVE**. Fetch path is Manager-wide (all groups this login can see + all users in those groups). Live cTrader send is **off** (`SAFE_BY_ABSENCE` + forced flag).

| Claim | Result | Class |
|---|---|---|
| Trade #3 = first official score (`N >= 3`) | **Yes** | `EarlyScoreTradeCount = 3`; `IsEarlyScoreEligible` iff completed XAU ≥ 3 |
| Event name in code | **Partial** | Sticky bool `EarlyScoreEligible`, **not** the one-shot `EARLY_SCORE_ELIGIBLE` event |
| High quality + low risk at N=3 | **`SHADOW`** | `quality >= 70 && risk < 40` |
| Mid quality at N=3 | **`WATCH`** | `quality >= 55` |
| Weak quality at N=3 | **`EARLY_SCORE`** | else after eligible |
| Martingale + DD + net loss (or risk ≥ 80) | **`RISK_BLOCKED`** | never SHADOW/LIVE |
| Auto-promote to `LIVE` / `LIVE_CANDIDATE` | **Impossible** | no branch; pin `CanPromoteToLive => false` |
| Copy intent on score | **SHADOW only** | `Status = "SHADOW_ONLY"`; no `ExecutionIntent` |
| Live `35=D` NewOrderSingle | **Does not exist** | FIX builder is `(35, "A")` only |
| `REAL_COPY_EXECUTION_ENABLED` | **false (forced)** | DI constructor + post-logon overwrite |
| Fetch ALL Achiever + Starwave groups/traders | **Yes on the live path** | `GroupRequestArray("*")` + `UserRequestArray` per group; measured census 18 / 8460 |
| Dummy FakeMt5 on API startup | **Off** | `BrokerCatalogSeed` only; native connectors required |
| Risk to capital if process starts now | **None** | no sender, no MT5 `SendTrade`, no wired `RiskEngine` send |

Do **not** claim A22 R5-before-R6 is implemented. Do **not** claim `CanPromoteToLive` is a persist gate (Application never calls it). Do **not** claim §68 / §70 PASS. Safety is **absence of a live branch** plus **absence of a FIX order builder**.

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

Architecture §1.4 (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L82–83):

> Do not send a trader to real money immediately after trade #3.  
> The default action after a strong early score should be SHADOW.

§15 (L668–695): count **3 completed reconstructed XAUUSD position lifecycles**. Trade #3 closure emits `EARLY_SCORE_ELIGIBLE`, **not** `PROVEN_PROFITABLE`.

§23 (L956–966):

```text
Trade #3 + high score
        ↓
SHADOW only
```

`docs/scoring.md` L3–7:

```text
Trade #3 completed XAUUSD ⇒ EARLY_SCORE_ELIGIBLE
High quality + low risk ⇒ SHADOW, never LIVE
```

`docs/architecture.md` L19–21:

```text
REAL_COPY_EXECUTION_ENABLED=false
Trade #3 → SHADOW / EARLY_SCORE, never LIVE
```

UI footer (`apps/web/src/pages/TraderDetailPage.tsx` L44): “First 3 completed XAUUSD trades unlock EARLY_SCORE / SHADOW only. Live promotion is not automatic.”

---

## 2. State machine (measured this pass)

SUT: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212 lines). Prior SHA-256 `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` (D12/D97). This pass re-read the full file; body is unchanged vs those pins.

```40:40:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public const int EarlyScoreTradeCount = 3;
```

```129:171:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public BaselineScore Score(IReadOnlyList<ReconstructedTradeResult> completedXau)
    {
        var features = ComputeFeatures(completedXau);
        var eligible = features.CompletedXauTrades >= EarlyScoreTradeCount;
        // ... risk / behavior / quality arithmetic ...
        var state = TraderStateMachine.FromBaseline(eligible, quality, risk, features);
        return new BaselineScore { /* SuggestedState = state, EarlyScoreEligible = eligible */ };
    }
```

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

### 2.1 Reachable set

| Input | `SuggestedState` | Can be LIVE? |
|---|---|---|
| `CompletedXauTrades == 0` | `INSUFFICIENT_DATA` | No |
| `N < 3` and not blocked | `INSUFFICIENT_DATA` | No |
| `N >= 3`, `quality >= 70`, `risk < 40` | **`SHADOW`** | No |
| `N >= 3`, `quality >= 55` | `WATCH` | No |
| `N >= 3`, else | **`EARLY_SCORE`** | No |
| `risk >= 80` or (martingale ∧ DD ∧ net < 0) | `RISK_BLOCKED` | No |

Tokens **never returned:** `LIVE`, `LIVE_CANDIDATE`, `PAUSED`, `DISQUALIFIED`.

`TraderState` enum still *contains* `LIVE_CANDIDATE = 4` and `LIVE = 5` (`D:\Prop\src\Domain\Enums\TraderState.cs` L9–10). Those are catalog values for later gates, not outputs of this machine.

### 2.2 `CanPromoteToLive`

- Body is compile-time `false`. Parameter discarded.
- Product callers in `src/` + `apps/`: **zero**.
- Test caller: one fact, `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` (`CanPromoteToLive(SHADOW).Should().BeFalse()`).
- Persist does **not** consult it. `ReconstructionScoringService` writes `CurrentState = score.SuggestedState` blindly (`DealIngestionService.cs` L139). Because Suggested cannot be LIVE, persist cannot become LIVE **today**. That is a vacuous lock, not A22 R5.

### 2.3 Unit lock (qualitative)

`D:\Prop\tests\Unit\BaselineScorerTests.cs`:

| Fact | N | Expected |
|---|---:|---|
| `Two_trades_remain_insufficient` | 2 | `EarlyScoreEligible=false`, `INSUFFICIENT_DATA` |
| `Three_disciplined_winners_go_to_shadow_not_live` | 3 | `EarlyScoreEligible=true`, **`SHADOW`**, `CanPromoteToLive=false` |
| `Martingale_after_losses_is_risk_blocked` | 3 | `RISK_BLOCKED` |

Integration: `SeedingAndStoreTests` asserts login 10001 `CurrentState.Should().NotBe(LIVE)`.

This pass did **not** re-run `dotnet test` (no shell in this slot). The assertions were re-read verbatim.

---

## 3. First-3 reconstruction (what “trade 3” counts)

```72:76:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
    public int CountCompletedXauUsdTrades(...) =>
        CompletedXauUsdTrades(...).Count;

    public bool IsEarlyScoreEligible(...) =>
        CountCompletedXauUsdTrades(...) >= 3;
```

`CompletedXauUsdTrades` requires `Completed && IsXauUsd && EligibleForFirstThree`. Canceled buy/sell on a `position_id` marks **every** lifecycle of that id `EligibleForFirstThree = false` (L34–50).

Unit: `First_three_completed_xau_unlocks_early_score` → count 3 / eligible true.  
`Canceled_deal_on_a_position_excludes_it_from_first_three` → 3 completed reconstructed, **count 2**, eligible **false**.

### Honesty: production score does **not** use that helper

```125:126:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
```

`RebuildTraderAsync` drops the `EligibleForFirstThree` filter. A canceled-tainted position can still increment `CompletedXauTrades` in the scorer. That can flip `EarlyScoreEligible` earlier than the helper. **It still cannot emit LIVE.** Known sibling: E024 (helper YES / production NO). Slot 29 does not treat that as a live-money bug.

`EarlyScoreEligible` is a **sticky bool** (`N >= 3`), not the architecture one-shot event `EARLY_SCORE_ELIGIBLE`. `AfterHighEarlyScore()` is never called by `Score()`.

---

## 4. Persist / shadow copy — SHADOW only, never venue

Caller: `ReconstructionScoringService.RebuildTraderAsync` always calls `PersistDemoShadowAsync(..., score.SuggestedState, completedXau)`.

```267:308:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
        if (state != TraderState.SHADOW)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }
        // ... requires a destination_quotes row ...
                Status = "SHADOW_ONLY",
```

| Write | When | Live order? |
|---|---|---|
| `OutboxEvent` `ScoreUpdate` | every rebuild | No |
| `CopyIntent` | only `state == SHADOW` **and** a dest quote exists | No (`SHADOW_ONLY`) |
| `ShadowOrder` | same | Simulated fill via `ShadowCopyEngine.SimulateEntry` |
| `ExecutionIntent` | **never** on this path | — |

`ShadowCopyEngine` (`D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`) is **math on a quote**. No socket.

`RiskEngine` is **not** called from ingest/score/shadow persist. Grep of `new RiskEngine` / `.Evaluate(` in `src/` + `apps/`: **zero product callers** (unit tests only). `AllowFixSend` is a DTO bit with no writer.

`IMt5BrokerConnector` (`Mt5Contracts.cs` L53–63) is **read-only**: Connect / Groups / Accounts / Deals / Positions. No `SendTrade` / `DealerSend` / `OrderSend` in `D:\Prop\src` `*.cs`.

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

```csharp
["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,  // forced false
["FEATURE_COPY_TRADING_ENABLED"] = false                    // hardcoded
```

`LiveRuntimeStatus.Snapshot()` when false: `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."`

FIX worker (`apps/fix-worker/Worker.cs` L21–46): reads `CTrader:RealCopyExecutionEnabled` with fallback **false**; even if true, it only **logs a warning** and still stamps TRADE `LastError = "No live TRADE socket. NewOrderSingle remains off."` It never builds an order.

### 5.2 No `35=D` builder

`CTraderFixSession.BuildLogon` body tags start with `(35, "A")` then 34/49/56/50/57/52/98/108/141/553/554. There is **no** `(35, "D")`, no cancel/replace, no OrderQty.

This-pass grep in `D:\Prop\src` `*.cs`:

| Pattern | Hits | Meaning |
|---|---:|---|
| `NewOrderSingle` | 7 | comments / logs / `LastError` / helper **name** (`MayRetryNewOrderSingle`) |
| `35=D` / `(35, "D")` | **0** | no builder |
| `SendTrade` / `DealerSend` / `OrderSend` | **0** | no MT5 send |

`MayRetryNewOrderSingle` is **status math only** (`ExecutionOrderStateMachine.cs` L35–36). No caller sends.

E034 (83 product `*.cs`): `35=D` = 0; NewOrderSingle = 7 name-only. Reconfirmed this pass on `src/`.

### 5.3 Logon is not an order

`CTraderFixLogonHostedService` opens TLS to QUOTE `:5211` and TRADE `:5212`, sends Logon `35=A`, records `LoggedOn`. Log line: `"NewOrderSingle still disabled"`. Tag 553 = integer account id (not SenderCompID). Password is read from env and **not** written here.

Wanting both “copy to cTrader” and “no loss” is satisfied **today** only as:

```text
ALLOW: Manager fetch, reconstruct, score, SHADOW_ONLY ledger, FIX 35=A logon
FORBID: 35=D / 35=F / 35=G, REAL_COPY=true, auto LIVE
```

Live copy + no-loss together is **not** implemented. The honest operating mode is **no live send**.

---

## 6. Fetch ALL Achiever + Starwave groups and ALL manager traders

### 6.1 Code path (current)

`LiveIngestHostedService` + `POST /api/ops/resync` call `SyncCatalogAsync` then score **every** login from `ListLoginsAsync`.

```44:48:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`GetAccountsAsync(null)` walks **every** group (`NativeMt5BrokerConnector.GetAccountsCore`):

1. Groups: `GroupRequestArray("*")`; if empty, fallback `GroupTotal` / `GroupNext`.
2. Per group: `UserRequestArray(gname)`; if retcode bad, `UserGetByGroup`; if still empty, `UserLogins` + `UserRequestByLogins`.
3. Dedup by login.

Dummy substitution is explicitly refused (`LiveIngestHostedService` L70): `"No dummy data will be substituted."`

DI throws without real `MT5_PASSWORD` **and** `MT5_STARWAVEFX_PASSWORD` (`LiveMt5Registration.HasRealPasswords`). Connectors are **native**, not `FakeMt5BrokerConnector`. API startup seeds catalog rows only (`BrokerCatalogSeed.EnsureAsync`), not FakeMt5 logins 10001/10002.

Plan-group mappings are **labels, not fetch filters** (`docs/architecture.md` L24).

### 6.2 Measured live census (do not re-invent)

Source: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe UTC: **2026-08-18T08:42:16.8519545+00:00** (`LiveBrokerProbe`).  
Companion: `LIVE_MANAGER_FETCH_MEASURED.md`.

This slot **did not re-attach** Manager (no shell; localhost HTTP blocked). Counts below are from that on-disk JSON, re-summed.

| Broker | Connect | Groups | Traders | Open positions | Path |
|---|---|---:|---:|---:|---|
| ACHIEVER | OK via HTTP proxy | **8** | **6512** | 1506 | `GroupRequestArray` + `UserRequestArray` |
| STARWAVEFX | OK direct | **10** | **1948** | 478 | same |
| **Total** | | **18** | **8460** | **1984** | |

Achiever groups (sum 2+179+4+5+4+6295+0+23 = **6512**):

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

Starwave groups (sum 11+4+170+1735+22+0+0+4+0+2 = **1948**):

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

**Honesty:** these are **all groups this manager login can see**. If the server has more groups, they are outside this ACL. Zero-account groups **are** listed (fetch is not filtered to non-empty). JSON contains the full login list; this report does not dump logins.

CREDENTIALS pin (names only, no values): `.env` present; Achiever + StarwaveFX passwords present; Achiever HTTP proxy present; `CTRADER_FIX_PASSWORD` present; `DATABASE_URL` still placeholder → API uses in-memory DB.

### 6.3 YoPips C++ (related Manager API, not the copy path)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`:

- `GroupTotal` / `GetAllGroups` / `GetGroupDetails` use `GroupNext` (L956–1012).
- `GetGroupLogins` → `UserLogins`.
- This is the **prop-firm** backend (challenge accounts, leverage updates). It is **not** wired to Pepperstone/cTrader `35=D`.
- Completeness difference: YoPips `GetAllGroups` does **not** call `GroupRequestArray("*")`. Prop C# live connector does, then falls back to `GroupTotal`/`GroupNext`. Do not treat YoPips group totals as this product’s census.

---

## 7. End-to-end: fetch → score trade #3 → no live order

```text
NativeMt5BrokerConnector (Achiever proxy + Starwave direct)
    GroupRequestArray("*") + UserRequestArray(group)
        → SyncCatalogAsync (all groups, all logins)
        → SyncBrokerAsync (deals / positions; read-only)
        → RebuildTraderAsync for every login
            Reconstruct (position lifecycle)
            BaselineScorer.Score
                N<3 → INSUFFICIENT_DATA
                N>=3 high/low-risk → SHADOW | WATCH | EARLY_SCORE | RISK_BLOCKED
                never LIVE
            PersistDemoShadowAsync
                SHADOW → CopyIntent SHADOW_ONLY + ShadowOrder (if dest quote)
                else → ScoreUpdate outbox only
CTraderFixLogonHostedService
    TLS 35=A QUOTE+TRADE
    RealCopyEnabled = false
    no 35=D method exists
```

Dashboard `/api/overview` **can count** `LIVE` / `LIVE_CANDIDATE` tiles (`EfDashboardQueries` L40–41). Those counters stay **0** unless something writes those states. Nothing in the score/persist path does.

Historical demo capture E031 (`shadow=2`, `riskBlocked=1`, `live=0`) was **FakeMt5 10001/10002/99001**, not the 8460-trader census. Do not mix those books.

---

## 8. Residual holes (honest, not LIVE)

| Hole | Effect on capital |
|---|---|
| Vacuous LIVE lock (no R5/R6 machine) | None today; would matter if someone added a LIVE branch without a sender |
| Production score ignores `EligibleForFirstThree` | Can mis-score dirty first-3; still not LIVE |
| `EarlyScoreEligible` is sticky, not one-shot event | Spec gap; not a send path |
| `AfterHighEarlyScore` unused | Pin only |
| `RiskEngine` unwired | Shadow/live send not gated by risk in production (send still absent) |
| Dest quote missing → no shadow rows | Fewer SHADOW ledger rows; no venue order |
| In-memory DB | Restart re-fetches; not a live send |
| §68 0/19, §70 0/14 | Live FIX send still forbidden |
| E031 demo 0 LIVE vs live 8460 catalog | Different books; both have no 35=D |

---

## 9. Files read (this slot)

| Path | Why |
|---|---|
| `src/Domain/Scoring/BaselineScorer.cs` | FromBaseline / pins |
| `src/Domain/Enums/TraderState.cs` | LIVE tokens exist, unused |
| `src/Domain/Reconstruction/TradeReconstructor.cs` | first-3 helper |
| `src/Application/Ingestion/DealIngestionService.cs` | rebuild + catalog |
| `src/Application/Runtime/LiveRuntimeStatus.cs` | copy note / flag |
| `src/Application/Contracts/Mt5Contracts.cs` | read-only connector |
| `src/Infrastructure/Persistence/EfTradingStore.cs` | SHADOW_ONLY |
| `src/Infrastructure/DependencyInjection.cs` | RealCopy forced false |
| `src/Infrastructure/Hosting/LiveIngestHostedService.cs` | all-login score loop |
| `src/Infrastructure/Mt5Live/LiveMt5Registration.cs` | native Achiever+Starwave |
| `src/Infrastructure/Dashboard/EfDashboardQueries.cs` | LIVE tile counter |
| `src/Infrastructure/Seeding/BrokerCatalogSeed.cs` | no FakeMt5 |
| `src/Mt5/Connectors/NativeMt5BrokerConnector.cs` | GroupRequestArray / UserRequestArray |
| `src/Fix.CTrader/Sessions/CTraderFixSession.cs` | 35=A only |
| `src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs` | logon, flag false |
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | default OFF |
| `src/Domain/Risk/RiskEngine.cs` | AllowFixSend (unwired) |
| `src/Domain/Shadow/ShadowCopyEngine.cs` | simulate only |
| `src/Domain/Execution/ExecutionOrderStateMachine.cs` | retry name only |
| `apps/api/Program.cs` | settings + resync |
| `apps/fix-worker/Worker.cs` | refuses send |
| `apps/web/src/pages/TraderDetailPage.tsx` | first-3 footer |
| `apps/web/src/pages/LiveCopyPage.tsx` | NewOrderSingle disabled |
| `tests/Unit/BaselineScorerTests.cs` | N=2/3/martingale |
| `tests/Unit/TradeReconstructionTests.cs` | first-3 + cancel |
| `tests/Unit/RiskEngineTests.cs` | flag false ⇒ AllowFixSend false |
| `docs/scoring.md`, `docs/architecture.md`, arch v2 §§1.4/15/22/23 | law |
| `reports/CREDENTIALS_AND_COPY_STATUS.md` | creds names + census |
| `reports/swarm/20260818/LIVE_MANAGER_FETCH_MEASURED.md` | census |
| `reports/swarm/20260818/LIVE_GROUPS_AND_TRADERS.json` | 8+10 groups, 6512+1948 traders |
| YoPips `mt5_manager.cpp` GroupTotal/GetAllGroups | sibling Manager API |

---

## 10. Slot-29 answer to the assigned goal

1. **Trade 3 is EARLY_SCORE / SHADOW, never auto LIVE.** Measured in `TraderStateMachine.FromBaseline` + unit facts + persist copy of SuggestedState.
2. **Fetch ALL Achiever + Starwave groups and ALL manager traders.** Code does `GroupRequestArray("*")` + per-group user arrays; last measured dump is **18 groups / 8460 traders**.
3. **Copy to cTrader must not send live orders yet.** `35=D` method does not exist; `RealCopyEnabled` forced false; intents are `SHADOW_ONLY`; MT5 connector cannot send. **No loss from this process.**

**Slot 29 verdict: CONFIRMED.**
