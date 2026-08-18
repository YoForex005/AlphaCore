# W500_RESEARCH_9 — Trade #3 is EARLY_SCORE / SHADOW, never auto LIVE

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_9.md` |
| Slot | **9** |
| Agent | W500_RESEARCH_9 (senior engineer; trade-#3 state + no-live-copy pin) |
| Date | 2026-08-18 |
| Assigned | Confirm trade 3 is EARLY_SCORE/SHADOW never auto LIVE. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders yet (no loss). |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Secrets printed | **No.** Passwords, proxy auth, FIX password, and manager logins’ credentials are not copied here. |
| Method | Re-read `BaselineScorer` / `TraderStateMachine`, persist/shadow, live ingest, native Manager connector, FIX session, API settings, workers, dashboard, tests. Grep `D:\Prop` product `*.cs` for `TraderState.LIVE`, `NewOrderSingle`, `35=D`, `CanPromoteToLive`. Grep `D:\Projects\YoPips\Backend\C++ Backend PropFirm` for cTrader / `NewOrderSingle` / `35=D`. Cite prior live census JSON; this pass did **not** re-open Manager or FIX sockets. |
| Binding law | Architecture v2 §§1.4, 15, 22–23, 41; `docs/scoring.md`; `docs/architecture.md`; A22 I2/I4/I5; A69; D12/D97; E002; A003 |
| Siblings (not copied as this verdict) | D12, D97, E002, E007, A003, A006, A009, A022, LIVE_MANAGER_FETCH_MEASURED, CREDENTIALS_AND_COPY_STATUS, W500_SLICE_9 (stale logon username quote) |

**Honesty rule:** “EARLY_SCORE/SHADOW” is the *marketing shorthand* for trade #3. The implemented reachable set at `N >= 3` is `{EARLY_SCORE, WATCH, SHADOW, RISK_BLOCKED}`. The assigned safety claim is **never auto LIVE**. Vacuous `CanPromoteToLive => false` is a pin, not a wired promotion FSM. No live capital loss on cTrader is **`SAFE_BY_ABSENCE`** (no `35=D` builder) **plus** scoring that cannot emit `LIVE`.

---

## 0. Verdict (binding)

**CONFIRMED: trade #3 never auto-promotes to LIVE / LIVE_CANDIDATE.**

| Claim | Result | Class |
|---|---|---|
| Trade #3 (`CompletedXauTrades >= 3`) can land `EARLY_SCORE` | **Yes** | `FromBaseline` default when `quality < 55` |
| Trade #3 + high quality + low risk lands `SHADOW` | **Yes** | `quality >= 70 && risk < 40` |
| Trade #3 can also land `WATCH` / `RISK_BLOCKED` | **Yes** | do not pretend those are impossible |
| Trade #3 can land `LIVE` or `LIVE_CANDIDATE` | **No** | not in `FromBaseline` reachable set |
| `CanPromoteToLive(*)` | **Always false** | compile-time `=> false`; **zero** product callers |
| Persist invents LIVE | **No** | `CurrentState = score.SuggestedState` (cannot be LIVE today) |
| Copy intent after score | **SHADOW_ONLY rows**, and only if `state == SHADOW` | never `ExecutionIntent` |
| Live cTrader `35=D` NewOrderSingle | **Does not exist** | `SAFE_BY_ABSENCE` |
| `REAL_COPY_EXECUTION_ENABLED` / `RealCopyEnabled` | **false** (env + DI + logon host force) | display + constructor pin; not a sender choke |
| Fetch ALL Achiever + Starwave groups | **Yes in code** (`GroupRequestArray("*")` + `GroupTotal`/`GroupNext` fallback); **measured 8 + 10 = 18** on 2026-08-18 probe | manager-visible universe only |
| Fetch ALL manager traders | **Yes in code** (`GetAccountsAsync(null)` walks every group); **measured 6512 + 1948 = 8460** | same probe |
| This process can open a losing live cTrader position | **No** | no builder, flag off, state ≠ LIVE |

One-line:

```text
N>=3 → EARLY_SCORE | WATCH | SHADOW | RISK_BLOCKED
     ↛ LIVE | LIVE_CANDIDATE
high quality → SHADOW; persist SHADOW_ONLY; no 35=D
```

Do **not** tick Architecture §68 / §70 from this file. Do **not** claim “EX5 decompiled” or “copy trading is live.”

---

## 1. Architecture law (what “trade 3” is allowed to mean)

Source of law: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`.

§1.4 (L82–83): *Do not send a trader to real money immediately after trade #3. The default action after a strong early score should be SHADOW.*

§15 (L685–695): Trade #3 closure emits `EARLY_SCORE_ELIGIBLE`, **not** `PROVEN_PROFITABLE`.

§23 (L960–964):

```text
Trade #3 + high score
        ↓
SHADOW only
```

Repo map `D:\Prop\docs\architecture.md` L20–21 and `D:\Prop\docs\scoring.md` L3–7 repeat the same pin.

A22 I4/I5 (`reports/swarm/20260818/A22_scoring_spec.md`): at `N == 3`, **forbidden** states are `LIVE` and `LIVE_CANDIDATE`. High score → SHADOW only.

---

## 2. Implemented state machine (measured)

File: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212 lines).
Enum: `D:\Prop\src\Domain\Enums\TraderState.cs` (LIVE_CANDIDATE=4, LIVE=5 exist as **enum members only**).

Eligibility latch:

```129:132:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public BaselineScore Score(IReadOnlyList<ReconstructedTradeResult> completedXau)
    {
        var features = ComputeFeatures(completedXau);
        var eligible = features.CompletedXauTrades >= EarlyScoreTradeCount;
```

`EarlyScoreTradeCount = 3` (L40). `EarlyScoreEligible` is a **bool**, not a `ScoreEligibility` enum. There is no `PROVEN_PROFITABLE` token in product C#.

`FromBaseline` reachable set (verbatim):

```189:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
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

| Input | Output |
|---|---|
| `CompletedXauTrades == 0` | `INSUFFICIENT_DATA` |
| `N < 3` (and not hard-blocked) | `INSUFFICIENT_DATA` |
| hard risk / losing martingale | `RISK_BLOCKED` |
| `N >= 3`, `quality >= 70`, `risk < 40` | **`SHADOW`** |
| `N >= 3`, `quality >= 55` | `WATCH` |
| `N >= 3`, else | **`EARLY_SCORE`** |
| any call to `AfterHighEarlyScore()` | `SHADOW` |
| any call to `CanPromoteToLive` | **`false`** |

**Never returned:** `LIVE`, `LIVE_CANDIDATE`, `PAUSED`, `DISQUALIFIED`.

Grep of product `*.cs` for assignment `TraderState.LIVE` / `SuggestedState =` :

- `BaselineScorer.cs` L169: `SuggestedState = state` where `state` is `FromBaseline(...)`.
- `EfDashboardQueries.cs` L40–41: **counts** of LIVE / LIVE_CANDIDATE (dashboard tiles only).
- No product writer assigns `LIVE` or `LIVE_CANDIDATE`.

`AfterHighEarlyScore` and `CanPromoteToLive` have **no callers** under `src/` or `apps/`. The only test lock is `tests/Unit/BaselineScorerTests.cs` L21–26 (`Three_disciplined_winners_go_to_shadow_not_live`) and `tests/Integration/SeedingAndStoreTests.cs` L31–32 (`login 10001` after 3 XAU trades `CurrentState.Should().NotBe(LIVE)`).

**Vacuous-lock honesty (same as D97):** persist does **not** consult `CanPromoteToLive`. If someone later taught `FromBaseline` to return `LIVE`, `ReconstructionScoringService` would write it. Today that cannot happen because `FromBaseline` has no such branch.

---

## 3. Persist path copies SuggestedState; copy rows are SHADOW_ONLY

`D:\Prop\src\Application\Ingestion\DealIngestionService.cs` `ReconstructionScoringService.RebuildTraderAsync`:

```125:143:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
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

`D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` `PersistDemoShadowAsync`:

1. Always enqueues outbox `Type = ScoreUpdate` (not `RiskCheckRequest`, not a send).
2. **Returns before any copy row** unless `state == TraderState.SHADOW` (L267–271).
3. Needs a `destination_quotes` row; else ScoreUpdate only.
4. New `CopyIntent.Status = "SHADOW_ONLY"` (L307). Quantity is source `MaxVolumeLots` into a **ledger** row.
5. `ShadowCopyEngine.SimulateEntry` writes `shadow_orders` (in-process math on dest bid/ask). No socket.

`EARLY_SCORE` / `WATCH` / `RISK_BLOCKED` / `INSUFFICIENT_DATA` traders get **no** `CopyIntent` / `ShadowOrder` from this method.

`OutboxEventType` (`D:\Prop\src\Domain\Enums\OutboxEventType.cs`) includes `ShadowCopyIntent` and `RiskCheckRequest`, but this persist path only writes `ScoreUpdate`. There is **no** outbox dispatcher under `src/` that consumes those events onto FIX.

`ExecutionIntent` exists as an EF entity. Persist never inserts one. `CopyIntent.ExecutionIntentId` stays null.

`RiskEngine.Evaluate` is called only from `tests/Unit/RiskEngineTests.cs`. Product copy/persist **does not** invoke it. `AllowFixSend` is a DTO bit with **no socket writer**. Even if `RealExecutionEnabled` were true, nothing would send.

---

## 4. Copy to cTrader cannot send live orders (no loss on this path)

### 4.1 No NewOrderSingle builder

Grep `D:\Prop` product `*.{cs,cpp,h,js,ts}` for `35=D` / `MsgType=D`: **0 hits**.

`NewOrderSingle` name hits (all non-senders):

| File | What it is |
|---|---|
| `Fix.CTrader/Configuration/CTraderFixOptions.cs` L32–35 | XML comment; `RealCopyExecutionEnabled = false` |
| `Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs` L68–71 | forces `_runtime.RealCopyEnabled = false`; log string “NewOrderSingle still disabled” |
| `Application/Runtime/LiveRuntimeStatus.cs` L42–43 | copyNote when flag false |
| `Infrastructure/DependencyInjection.cs` L40–41 | constructor `RealCopyEnabled = false` + comment “not implemented” |
| `Infrastructure/Seeding/DemoSeeder.cs` / `BrokerCatalogSeed.cs` | `LastError` English |
| `apps/fix-worker/Worker.cs` | stamps TRADE `Disconnected`; warns if config true; **still does not send** |
| `Domain/Execution/ExecutionOrderStateMachine.cs` L35–36 | `MayRetryNewOrderSingle` is status math only |

The only FIX writer is `CTraderFixSession.TryLogonAsync` (`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`). It builds **exactly** `35=A` Logon (L96), writes it once, reads the reply, returns. Body fields are 35/34/49/56/50/57/52/98/108/141/553/554. **No `D`, no `F`, no `G`, no OrderQty.**

Hosted service (current, not the stale W500_SLICE_9 quote): tag 553 = integer account id (`username = account`, L45–57). Logon ≠ copy.

`CTraderQuoteService` parses SecurityList / MD in memory. It never opens a socket.

`FixMessageParser` is a unit-test pipe/SOH helper. No TRADE initiator.

QuickFIX/n is **not** referenced (prior C19/D52). Official engine absence is another reason send is impossible.

### 4.2 Flag stays false

| Surface | Value |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** |
| `LiveRuntimeStatus` DI ctor | **`false`** (`DependencyInjection.cs` L38–42) |
| `CTraderFixLogonHostedService` after logon | **forced `false`** (L68) |
| `GET /api/settings` | `featureFlags.REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled` (therefore false) |
| `FEATURE_COPY_TRADING_ENABLED` | hardcoded **`false`** (`Program.cs` L76) |
| gitignored `D:\Prop\.env` L73 | `REAL_COPY_EXECUTION_ENABLED=false` (name only; value is the literal false) |
| `apps/fix-worker` | `GetValue("CTrader:RealCopyExecutionEnabled", false)` — different key; fallback false |

Flipping the env to true would **still not** place an order: there is no builder. Do **not** enable it.

### 4.3 Workers / UI

- `apps/mt5-worker/Worker.cs` L19: “Execution copy is not performed here.”
- `apps/fix-worker/Worker.cs` L40–46: TRADE last error “NewOrderSingle remains off”; if `real` it **logs a warning and still does not send**.
- `apps/web/src/pages/LiveCopyPage.tsx`: static amber copy that NOS is disabled.
- `TraderDetailPage.tsx` L44: “First 3 completed XAUUSD trades unlock EARLY_SCORE / SHADOW only. Live promotion is not automatic.”
- `ShadowPortfolioPage.tsx`: “Live NewOrderSingle remains disabled.”

---

## 5. Fetch ALL Achiever + Starwave groups and ALL manager traders

### 5.1 Code path (current)

Live API DI (`AddTraderIntelligence`) registers **only** `NativeMt5BrokerConnector` pairs via `LiveMt5Registration.CreateConnectors`. Dummy/fake is refused: missing real MT5 passwords throw (`DependencyInjection.cs` L35–36).

Catalog:

```37:50:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
    public async Task<BrokerSyncResult> SyncCatalogAsync(string brokerCode, CancellationToken ct)
    {
        var connector = _registry.Get(brokerCode);
        await connector.ConnectAsync(ct);
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var now = DateTimeOffset.UtcNow;

        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`GetAccountsAsync(null)` means **no group filter**.

Native groups (`NativeMt5BrokerConnector.GetGroupsCore`, L144–186):

1. `GroupRequestArray("*", arr)` — all groups this manager can see.
2. If that list is empty, fall back to `GroupTotal` / `GroupNext`.
3. Dedup by name.

Native accounts (`GetAccountsCore`, L189–214):

- `group == null` → iterate **every** name from `GetGroupsCore()`.
- Per group: `UserRequestArray` → else `UserGetByGroup` → else `UserLogins` + `UserRequestByLogins`.
- No `.Take(200)`. No plan-group allow-list. `plan-group` mapping is **not** a fetch filter (`docs/architecture.md` L24). Fake `demo\yo-2step` strings live only in `FakeMt5BrokerConnector`.

Live host scores **every** persisted login after catalog (`LiveIngestHostedService.cs` L86–102). Manual `POST /api/ops/resync` (`Program.cs` L121–137) does the same for `ACHIEVER` then `STARWAVEFX`.

**Caveat (do not greenwash):** `apps/mt5-worker/Worker.cs` L31–35 still rebuilds only `{10001,10002,10003,99001}`. That worker is **not** the API live-ingest path. If someone runs only `mt5-worker`, scoring is the four demo logins even if catalog syncs both brokers. The **API** host is the ALL-traders scorer.

### 5.2 Measured census (prior probe this calendar day; not re-opened this slot)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`

```
probe=LiveBrokerProbe  utc=2026-08-18T08:42:16.8519545+00:00  envLoaded=true
```

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | true (HTTP whitelist proxy) | 8 | 6512 | 1506 |
| STARWAVEFX | true (direct) | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever groups (all this manager can see): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` 0, `demo\yo-payp` 23.

Starwave groups: `Starwave\cent\FX1\grp1` 11, `grp2` 4, `demo\FX2\grp1` 170, `grp2` 1735, `real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `LP` 2.

Honesty: this is the **manager-visible** universe, not a claim that the brokers have no other groups outside this login’s ACL. Groups with 0 accounts are still fetched (ALL groups, including empty).

This slot did **not** re-hit Manager. Counts above are the last on-disk measured probe, consistent with `LIVE_MANAGER_FETCH_MEASURED.md` and `CREDENTIALS_AND_COPY_STATUS.md`.

---

## 6. C++ PropFirm backend (relevant only as a non-path)

Searched `D:\Projects\YoPips\Backend\C++ Backend PropFirm` (excluding unused SDK noise) for `cTrader`, `cServer`, `pepperstone`, `NewOrderSingle`, `35=D`, `REAL_COPY`, `EARLY_SCORE`, `SHADOW`.

| Hit | Meaning |
|---|---|
| `copy_trade_clusters` / `copy_trade_cluster_members` / `COPY_TRADING_RESTRICTION` | **Detection / restriction** of challenge-account copy-trading, not a cTrader copier |
| No FIX 4.4 client, no Pepperstone host, no NewOrderSingle | C++ backend is **not** the D:\Prop copy-to-cTrader sender |

The C++ service **can** place live **MT5** challenge-terminal orders (YoPips prop-firm product). That is a **different process and a different venue**. Nothing in `D:\Prop` `AddTraderIntelligence` / FIX logon / shadow persist calls into that binary. Copy-to-cTrader no-loss is decided entirely inside `D:\Prop`.

---

## 7. What a completed trade #3 actually does today

```
MT5 Manager (ALL groups `*`, ALL logins)
    → deals/positions persist
    → reconstruct completed XAUUSD lifecycles
    → BaselineScorer.Score
         N<3  → INSUFFICIENT_DATA   (no copy rows)
         N>=3 + toxic risk → RISK_BLOCKED (no copy rows)
         N>=3 + quality>=70 & risk<40 → SHADOW
              → CopyIntent Status=SHADOW_ONLY + ShadowOrder simulate
         N>=3 + quality>=55 → WATCH (no copy rows)
         N>=3 + else → EARLY_SCORE (no copy rows)
    → CanPromoteToLive = false (unused)
    → FIX 35=A logon optional; 35=D absent
    → RealCopyEnabled forced false
```

Demo unit gold (`BaselineScorerTests`): three disciplined winners → `EarlyScoreEligible=true`, `SuggestedState=SHADOW`, `CanPromoteToLive=false`. Integration seed: login 10001 after 3 XAU `NotBe(LIVE)`; 10002 martingale `RISK_BLOCKED`.

Prior demo API rollup (E031, Fake/seeder era): `shadow=2`, `riskBlocked=1`, `live=0`. That is **not** the 8460-login live book. After live ingest, individual states will be a mix of INSUFFICIENT_DATA / EARLY_SCORE / WATCH / SHADOW / RISK_BLOCKED depending on each login’s first-3 XAU book. **None of those paths emit LIVE.**

---

## 8. Residual risks (honest, not greenwash)

1. **Blind persist.** `CurrentState = SuggestedState` with no `if (N==3) forbid LIVE`. Safety is the scorer’s reachable set, not a persist CHECK constraint.
2. **Vacuous `CanPromoteToLive`.** Dead API. Do not market it as the go-live gate.
3. **`SAFE_BY_ABSENCE`.** Adding a `35=D` builder without §68/§70 PASS would be the first real capital-loss path. Do not add it in this wave.
4. **`RiskEngine` L90–93 is a no-op comment.** It does not return; later `allowSend` uses the flag. Irrelevant today because Evaluate is not on the product path and no sender exists.
5. **mt5-worker 4-login loop** if that process is what ops run instead of the API host.
6. **This slot did not re-measure** live Manager or live FIX logon. Census 18/8460 is the 08:42Z probe file.
7. **C++ YoPips backend** can still lose challenge-account money on **MT5** if that service is used as a trader terminal. Out of scope for this copy-to-cTrader pin; do not start it as part of fetch/score.

---

## 9. Slot-9 no-loss implication (goal)

Goal: fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders yet (no loss).

| Goal piece | Status |
|---|---|
| Fetch ALL groups | **Code YES** (`GroupRequestArray("*")`); **last measured 18** |
| Fetch ALL manager traders | **Code YES** (`GetAccountsAsync(null)`); **last measured 8460** |
| Score them | **API host YES** (every `ListLoginsAsync`); **mt5-worker NO** (4 demo logins) |
| Trade #3 → EARLY_SCORE / SHADOW (or WATCH / RISK_BLOCKED) | **YES** |
| Trade #3 → auto LIVE | **NO** |
| Live cTrader orders | **NO** (`35=D` missing; flag false; SHADOW_ONLY ledger only) |
| Risk to cTrader capital from this process | **None** |

**Slot 9 verdict: PASS on the assigned safety claim. Fetch-all is implemented and was measured earlier today. Live send remains off.**
