# P500_CODE_7 — `DealIngestionService.cs` / hard-false `CanPromoteToLive`

| Field | Value |
|---|---|
| Slot | **7** |
| Agent | P500_CODE_7 (senior trading-systems; this compilation unit only) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| File | `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` |
| Angle | `CanPromoteToLive` is hard-false — is that protecting profit or blocking it? |
| Verdict | **BLOCKS destination profit; PROTECTS destination capital; pin is dead in this file.** Not an empty PASS. |
| Product source modified | **No.** Report only. |
| Method | Full `read_file` of `DealIngestionService.cs` (146 lines, including `ITradingStore` + `ReconstructionScoringService` in the same unit). Grep of this file for `CanPromoteToLive` / `35=D` / `NewOrderSingle` / `LIVE` / `REAL_COPY` (**all 0**). Grep of `src` for `CanPromoteToLive` (definition only). Read persist (`EfTradingStore.PersistDemoShadowAsync` / `UpsertScoreAsync`), `TraderStateMachine`, `LiveIngestHostedService`, `LiveRuntimeStatus`, dashboard `DestinationRealPnl`. **No** `NewOrderSingle` constructed. **No** passwords printed. |
| Measured live (caller) | 8463 accounts; Achiever scoring; Starwave deals-done scored **0**; SHADOW all demo; `destinationRealPnl` **0**; FIX **LoggedOn**; `REAL_COPY` **false** |

Classification: `VACUOUS_LOCK_OFF_PATH` on the named pin. `SAFE_BY_ABSENCE` for live send from this unit. **Not** a go-live PASS. **Not** “promotion is gated here.”

---

## Angle

`CanPromoteToLive` is unconditionally `false` in Domain. For the Application ingest/score unit, is that pin **protecting profit** (locking in / enabling capture of winner alpha) or **blocking profit** (keeping destination real PnL at 0)?

---

## Verdict

**Blocking live destination profit. Protecting destination capital. The hard-false does neither *inside this file* because this file never calls it.**

Measured facts after reading the assigned file in full:

1. **`CanPromoteToLive` is not in `DealIngestionService.cs`.** Grep of this path: **0 hits**. The hard-false lives in `TraderStateMachine` at `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` line **211**: `public static bool CanPromoteToLive(TraderState current) => false;`
2. **This file’s scoring persist is a blind copy.** `ReconstructionScoringService.RebuildTraderAsync` writes `CurrentState = score.SuggestedState` and calls `PersistDemoShadowAsync(..., score.SuggestedState, ...)` with **no** promotion check, **no** risk engine, **no** RBAC.
3. **`DealIngestionService` itself has no state machine.** `SyncCatalogAsync` / `SyncBrokerAsync` only pull groups, accounts, deals, positions. They cannot promote a trader and cannot send FIX.
4. **`FromBaseline` (other file) never emits `LIVE` / `LIVE_CANDIDATE`.** Highest “good book” token is `SHADOW`. So persist cannot write LIVE today *because the scorer never suggests it*, not because this file refused a promotion.
5. **Product callers of `CanPromoteToLive` under `src/`:** definition only. Tests are the only callers (`CanPromoteToLive(SHADOW) == false`). The pin is a **dead API**.
6. **If someone later made `FromBaseline` return `LIVE`, this file would persist LIVE.** The hard-false would not stop it. That is why this is a vacuous lock, not a wired capital gate.

Answer to the angle, without greenwash:

| Question | Answer for this unit |
|---|---|
| Does hard-false **protect profit** (capture / lock in dest gains)? | **No.** Destination real PnL stays **0**. The pin does not book, hedge, or realize Achiever/Starwave alpha. |
| Does hard-false **block profit** (prevent live copy of winners)? | **Yes as product intent / Domain pin.** **No as a runtime branch in this file** (uncalled). Live dest profit is blocked by *absence of a LIVE persist + absence of `35=D`*, plus `REAL_COPY false` elsewhere. |
| Does it **protect capital** (lower dest loss)? | **Yes by absence of send**, not by this pin firing. This file cannot open a Pepperstone/cTrader position. |
| Empty PASS? | **No.** The pin is off-path. Claiming “PASS — promotion gated” would be a lie. |

Caller-measured live picture is consistent: 8463 accounts on the catalog, Achiever scoring, Starwave **deals-done scored 0**, SHADOW all demo, `destinationRealPnl` 0, FIX LoggedOn, `REAL_COPY` false. Logged-on FIX + scored Achiever books do not become dest fills from this unit.

---

## Evidence quotes

### 1. Assigned file — ingest has no promotion API

`SyncBrokerAsync` upserts catalog + deals + positions and returns an insert count. No `TraderState`, no `CanPromoteToLive`, no FIX:

```54:98:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
    public async Task<int> SyncBrokerAsync(string brokerCode, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var connector = _registry.Get(brokerCode);
        await connector.ConnectAsync(ct);
        var catalog = await SyncCatalogAsync(brokerCode, ct);
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var now = DateTimeOffset.UtcNow;
        var groups = await connector.GetGroupsAsync(ct);
        var accounts = await connector.GetAccountsAsync(null, ct);
        var insertedDeals = 0;
        // ... bulk or per-login deals + positions ...
        _ = catalog;
        return insertedDeals;
    }
```

### 2. Same file — scoring persist never asks the pin

```119:145:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
    public async Task RebuildTraderAsync(string brokerCode, long login, CancellationToken ct)
    {
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var deals = await _store.LoadDealsAsync(brokerId, brokerCode, login, ct);
        var trades = _reconstructor.Reconstruct(brokerCode, login, deals);
        await _store.ReplaceReconstructedAsync(brokerId, login, trades, ct);

        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore
        {
            Id = Guid.NewGuid(),
            BrokerId = brokerId,
            Login = login,
            RiskScore = score.RiskScore,
            BehaviorScore = score.BehaviorScore,
            EarlyQualityScore = score.EarlyQualityScore,
            CompletedXauTrades = score.Features.CompletedXauTrades,
            Martingale = score.Features.Martingale,
            AveragingDown = score.Features.AveragingDown,
            LotEscalation = score.LotEscalation,
            CurrentState = score.SuggestedState,
            LastScoredAt = DateTimeOffset.UtcNow
        }, ct);

        await _store.PersistDemoShadowAsync(brokerId, login, score.SuggestedState, completedXau, ct);
    }
```

`CurrentState = score.SuggestedState` is unconditional. There is no:

```csharp
if (!TraderStateMachine.CanPromoteToLive(score.SuggestedState)) { /* refuse LIVE */ }
```

### 3. Store port on this file only admits demo-shadow persist

```17:17:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
    Task PersistDemoShadowAsync(Guid brokerId, long login, TraderState state, IReadOnlyList<ReconstructedTradeResult> completedXau, CancellationToken ct);
```

Implementation (`EfTradingStore`, not this file) writes `Status = "SHADOW_ONLY"` and simulates a fill. It **returns without writing intents** unless `state == TraderState.SHADOW`. That is a shadow ledger, not a TRADE `35=D`.

### 4. The hard-false (other file; cited because the angle names it)

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

Parameter `current` is discarded. Every enum value returns `false`. `LIVE` / `LIVE_CANDIDATE` exist on `TraderState` (`LIVE_CANDIDATE = 4`, `LIVE = 5`) and are **not** in any `return` of `FromBaseline`.

### 5. Grep of the assigned file

| Needle | Hits in `DealIngestionService.cs` |
|---|---:|
| `CanPromoteToLive` | **0** |
| `35=D` | **0** |
| `NewOrderSingle` | **0** |
| `REAL_COPY` | **0** |
| `LIVE` / `LIVE_CANDIDATE` | **0** |
| `PersistDemoShadowAsync` | **2** (port + call) |
| `SuggestedState` | **2** (score write + shadow persist) |

Grep of `D:\Prop\src` for `CanPromoteToLive`: **1** — the Domain definition. Application never calls it.

### 6. Why `destinationRealPnl` is 0 even if scoring runs

Dashboard overview hard-codes dest real PnL / XAU gross / XAU net to literal `0` (`EfDashboardQueries.GetOverviewAsync`). Combined with `LiveRuntimeStatus.RealCopyEnabled` forced `false` in DI and again after FIX logon, FIX LoggedOn cannot become dest realized PnL from this stack.

### 7. Starwave “deals-done scored 0” is not the pin

`LiveIngestHostedService` scores `ListLoginsWithDealsAsync` only. If Starwave’s deals phase inserts **0** deals, scored logins for that broker stay **0**. That is an ingest/window/connector outcome, **orthogonal** to `CanPromoteToLive`. Hard-false did not cause Starwave score=0; empty deal tape did.

---

## Profit implication

**This file cannot create destination profit. The hard-false pin, if treated as the live gate, would also refuse to.**

- Achiever scoring of a live book (caller: scoring running) can at best land `SHADOW` and write `SHADOW_ONLY` copy intents. Those are simulated `ShadowOrder` rows, not Pepperstone fills.
- Starwave deals-done scored **0** means this unit produced **no** Starwave ranks this pass. Winner identification on that broker is blocked **before** any promotion question.
- `destinationRealPnl` remaining **0** matches: no LIVE state written, no `35=D`, `REAL_COPY` false.
- Hard-false does **not** protect (lock in) source-trader profit. Source PnL on Achiever/Starwave is observation. Dest capture is off.
- Therefore: **blocking live copy profit, not protecting it.** Opportunity cost is every SHADOW-quality book among 8463 accounts that is never copied for real. That is the current safety policy, not a bug in the pin’s *intent*. The bug-shaped fact is: **this Application unit does not consult the pin**, so the profit block is “we never emit LIVE / never send,” not “`CanPromoteToLive` returned false at persist time.”

Do not treat a high `EarlyQualityScore` persisted by `RebuildTraderAsync` as a money path.

---

## Lower-loss implication

**Protects Pepperstone / cTrader capital by never sending. Does not reduce loss on an open dest book (there is none).**

- This compilation unit has no socket, no ClOrdID, no MsgType, no quantity send. Loss floor = **do not open**.
- `CanPromoteToLive => false` is the *intended* refuse of auto-LIVE. It is **not executed** on the persist path in this file. Capital safety that actually holds today:
  1. `FromBaseline` reachable set excludes LIVE.
  2. Persist name and store behavior are demo-shadow / `SHADOW_ONLY`.
  3. `RealCopyEnabled = false` (DI + FIX host).
  4. No `35=D` builder in this process (SAFE_BY_ABSENCE; other slots).
- If `FromBaseline` later returned `LIVE`, **this file would write it** and still would not send — send remains absent — but the named pin would have failed as a persist gate.
- Scoring a losing martingale book to `RISK_BLOCKED` is a **label**, not a flatten. It cannot cut dest loss because dest exposure is 0.
- FIX LoggedOn does not change this leaf. Session is elsewhere; this file does not arm TRADE.

**Risk to capital from this file: NONE.** Lower-loss vs a live-copy world is “never copy,” not “size down.”

---

## Binding one-liner

`DealIngestionService.cs` never mentions `CanPromoteToLive`. The Domain pin is hard-false and uncalled. That **blocks live dest profit** and **protects dest capital** only by absence of a LIVE/send path — not by a persist-time refuse. `destinationRealPnl` stays 0. Not an empty PASS.
