# P500_VERIFY_51 — Adversarial four-file verify (slot 51)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_51.md` |
| Agent / slot | P500 verify **51** (adversarial; siblings are not evidence) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Quoted as booleans / public dest ids only. |
| Secrets printed | **None.** Quoted only `REAL_COPY_EXECUTION_ENABLED=true`, `FEATURE_COPY_TRADING_ENABLED=true`, public dest ids `5328266` / `1369850`, public host prefix `demo-`. Tag 554 never dumped. Password fields unread beyond existence checks. |
| Live GET this pass | **Blocked.** `web_fetch` refused `http://127.0.0.1:5000/api/{health,settings,copy/status,overview}` (`SSRF blocked: 127.0.0.1`). `open_page` on `http://localhost:5000/api/health` returned no content. No process DTO. Claims that need a live JSON value are **FAIL-unproven**, not invented. |
| Method | Independent full `read_file` of `CTraderFixSession.cs` (135/135), `BaselineScorer.cs` (213/213), `RiskEngine.cs` (189/189), `LiveCopyPage.tsx` (70/70). Then hop files required to prove/fail claims 3–5: `CTraderFixLogonHostedService.cs`, `LiveRuntimeStatus.cs`, `DependencyInjection.cs`, `CopyTradingService.cs` (625/625), `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs` (gate only), `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `DealIngestionService.cs`, `EfTradingStore.PersistDemoShadowAsync`, `apps/api/Program.cs`, `CTraderFixOptions.cs`, `apps/api/appsettings.json` FeatureFlags, `.env` boolean keys only, `data/demo_copy_ledger.json` dest ids/prices only. Grep: `(35,` / `Build("D")` / `RealCopyEnabled =` / `CanPromoteToLive` / `DestinationRealPnl` / `CurrentState =`. |

**Honesty rule:** sibling reports are **not** evidence. A comment, log line, dashboard copy, or `LastError` string is **not** a builder. `35=A` Logon is **not** NewOrderSingle. A sibling `Build("D")` is **not** `CTraderFixSession`. A POCO `= false` default is **not** a process pin. An env bind that evaluates `true` **disproves** “forced false after logon.” Demo dest fill is **not** live `1369850` profit and is **not** `DestinationRealPnl`. Set **FAIL** if any assigned claim cannot be proven from a file or live GET.

---

## 0. Verdict (binding)

**FAIL.**

Claims **1, 2, 4, 5** are file-proven. Claim **3** is **disproven**: `RealCopyEnabled` is **not** forced false after logon.

| # | Claim | File proof | Result |
|---|---|---|---|
| 1 | no `35=D` builder | `CTraderFixSession.cs` 135/135. Only outbound MsgType is `(35, "A")` at L96 inside `BuildLogon`. One `WriteAsync` (L49). Grep `(35,` in that file = **1** (`"A"`). `Build("D")` / `(35, "D")` / `35=D` / `NewOrderSingle` in that file = **0**. | **PASS** |
| 2 | `CanPromoteToLive` is false | `BaselineScorer.cs` L211: `public static bool CanPromoteToLive(TraderState current) => false;` Unconditional. `FromBaseline` never returns `LIVE` / `LIVE_CANDIDATE`. Unit test L26 asserts false. Ingest writes `CurrentState = score.SuggestedState`. | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **Disproven.** Only assignment is DI L41 from env. `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true`. Logon host L60–70 writes Quote/Trade only; **never** assigns `RealCopyEnabled`. Grep `RealCopyEnabled =` in product `*.cs` = **1** hit (DI). Live GET blocked — file bind is enough. | **FAIL** |
| 4 | sending now cannot be the profit path | Counted dest profit is constructor `0` (`OverviewDto.DestinationRealPnl` ← `EfDashboardQueries` L44). Live `1369850` refused by `CTraderFixCopyOpen` L37–42. Persist hop `AllowFixSend=false` (CopyTradingService L324). `CanPromoteToLive=>false`. Session hop has no `35=D`. Residual: hosted demo dest **can** `Build("D")` — dest **execution**, not dest **profit accounting**, not live capital. | **PASS** |
| 5 | SHADOW on demo is not destination profit | `ShadowCopyEngine.SimulateEntry` is paper. Persist status `SHADOW_ONLY`. Overview `ShadowPnl` = sum of `SourceVsShadowSlippage`; `DestinationRealPnl` literal `0`. Residual: `ADMITTED` SHADOW can dest-send via roster+`ExecuteDemoCopyAsync`; dest send ≠ dest PnL DTO. | **PASS** |

One-line:

```text
FAIL. CTraderFixSession 35=A only. CanPromoteToLive=>false. RealCopyEnabled NOT forced false after logon (.env true + DI bind + no re-pin). Counted dest PnL is constructor 0; live 1369850 refused; SHADOW is paper. Demo dest hop can still 35=D. Risk to live capital NONE.
```

---

## 1. No 35=D builder — PASS (`CTraderFixSession.cs`)

Assigned file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135, full read).

The class is logon-only. The only outbound constructor is `BuildLogon`:

```89:109:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
    private static string BuildLogon(
        string sender, string target, string senderSub, string targetSub,
        string username, string password, int seq)
    {
        var sendingTime = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            (49, sender),
            (56, target),
            (50, senderSub),
            (57, targetSub),
            (52, sendingTime),
            (98, "0"),
            (108, "30"),
            (141, "Y"),
            (553, username),
            (554, password)
        };
        return Assemble(fields);
    }
```

- `TryLogonAsync` L47–49: one `WriteAsync` of that logon, then `ReadAsync`, then parse inbound tag `35`.
- Inbound `Extract(reply, "35")` (L55) is **not** a builder. It only classifies the reply (`"A"` → LoggedOn).
- `Assemble` (L112–119) prefixes `8=FIX.4.4` + body length + checksum. It does not choose MsgType.
- Sockets are `using`/`await using` and drop after the single reply. No heartbeat loop. No order path.
- Grep of this file: `NewOrderSingle` = 0, `Build("D")` = 0, `(35, "D")` = 0, literal `35=D` = 0.
- Grep `(35,` in this file = **1** hit (L96 `"A"`).

**Residual (does not fail claim 1):** siblings **do** build `35=D`. Claim 1 is scoped to the assigned session file. They **do** matter for claim 4 residual.

| File | Hits | Wired? |
|---|---|---|
| `CTraderFixCopyOpen.cs` L95 | `Build("D", …)` | **Yes** — `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566, hosted every 20s |
| `CTraderFixDemoTestTrade.cs` L139/L163/L197 | `Build("D", …)` | tools/CLI (`tools/DemoFixTestTrade`); demo-gated; refuses `1369850` |
| `CTraderFixDemoMatrix.cs` L93 | `Build("D", …)` | demo matrix helper; not on copy DI hop |

---

## 2. `CanPromoteToLive` is false — PASS (`BaselineScorer.cs`)

Assigned file `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (213/213).

```187:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
public static class TraderStateMachine
{
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
}
```

- Parameter `current` is **unread**. Every `TraderState` returns false.
- `FromBaseline` can emit `INSUFFICIENT_DATA`, `RISK_BLOCKED`, `SHADOW`, `WATCH`, `EARLY_SCORE`. It never emits `LIVE` or `LIVE_CANDIDATE` (`TraderState.cs` L9–10 exist as enum members only).
- Highest auto state is `SHADOW` (quality ≥ 70 and risk < 40, after 3 completed XAU trades).
- Ingest writes `CurrentState = score.SuggestedState` (`DealIngestionService.cs` L140). Product grep `CurrentState =` writers: ingest L140 + `EfTradingStore` L232 (`existing.CurrentState = score.CurrentState`). No product writer assigns `TraderState.LIVE`.
- Unit `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` L21–26: SuggestedState `SHADOW`; `CanPromoteToLive(...).Should().BeFalse()`.

`LiveCopyPage.tsx` L15 displays `liveTraders` from API; that count is `scores.Count(s => s.CurrentState == LIVE)` (`CopyTradingService` L58). The scorer cannot produce that state. A stale DB row could still display as LIVE; that is display, not promotion. Promotion API/button: **absent** from `LiveCopyPage.tsx` (70/70; GET-only hooks).

`apps/api/appsettings.json` L44–47: `LiveCopyEnabled: false`, `AutoPromotionEnabled: false`. Those FeatureFlags are **not** read by `TraderStateMachine` (hard `=> false` already). They are corroboration, not the pin.

---

## 3. `RealCopyEnabled` forced false after logon — FAIL

**Cannot prove. Disproven as process state.**

### 3.1 Only assignment is env-true

Grep `RealCopyEnabled =` across product `*.cs` = **one** hit:

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

Lab `.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret).
`.env` L106: `FEATURE_COPY_TRADING_ENABLED=true` (boolean only; unused by DI — API `/api/settings` hardcodes FEATURE `true` at `Program.cs` L77).

API host `apps/api/Program.cs` L10: `EnvFile.FindAndLoad()` then L13 `AddEnvironmentVariables()` then L15 `AddTraderIntelligence`.

So API-process `LiveRuntimeStatus.RealCopyEnabled` is **true** at construction when the lab `.env` is loaded.

`LiveRuntimeStatus.RealCopyEnabled` is a public `{ get; set; }` (L32). Nothing after construction writes it.

### 3.2 Logon host does not re-pin

`CTraderFixLogonHostedService.ExecuteAsync` after both `CTraderFixSession.TryLogonAsync` calls:

```60:70:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.Quote.LoggedOn = quote.LoggedOn;
        _runtime.Quote.Status = quote.Status;
        _runtime.Quote.LastError = quote.LastError;
        _runtime.Quote.UpdatedAt = DateTimeOffset.UtcNow;
        _runtime.Trade.LoggedOn = trade.LoggedOn;
        _runtime.Trade.Status = trade.Status;
        _runtime.Trade.LastError = trade.LastError;
        _runtime.Trade.UpdatedAt = DateTimeOffset.UtcNow;
        _log.LogInformation(
            "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
            quote.LoggedOn, trade.LoggedOn, _runtime.RealCopyEnabled, account);
```

- Writes Quote + Trade only.
- **Logs** `RealCopyEnabled`. Does **not** assign it.
- Comment/log “NewOrderSingle still unimplemented” is **not** a pin. That log is stale relative to `CTraderFixCopyOpen.Build("D")`.

`CTraderFixOptions.RealCopyExecutionEnabled` default `false` (L35) is **unbound** by DI (`Configure<CTraderFixOptions>` = 0 hits). Not the API gate.

`apps/fix-worker/Worker.cs` L21 reads `CTrader:RealCopyExecutionEnabled` default `false` — a **different** key, different process, log-only. Not a post-logon pin of `LiveRuntimeStatus`.

`LiveRuntimeStatus.Snapshot()` copyNote when armed (L42–43): “REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.” — a string, not a force-false.

### 3.3 Live GET not available

Attempted:

- `web_fetch http://127.0.0.1:5000/api/health` → SSRF blocked
- `web_fetch http://127.0.0.1:5000/api/settings` → SSRF blocked
- `web_fetch http://127.0.0.1:5000/api/copy/status` → SSRF blocked
- `web_fetch http://127.0.0.1:5000/api/overview` → SSRF blocked
- `open_page http://localhost:5000/api/health` → no content

I do **not** invent a live `realCopyEnabled` JSON. File bind is enough to fail claim 3: the process flag is constructed from `.env=true` and never written false after logon.

If a live GET later shows `realCopyEnabled: false`, that would contradict the current source (unless env was changed). This slot did not observe that DTO.

### 3.4 What this does **not** prove

Armed `RealCopyEnabled` is **not** a live `35=D` on `1369850`. See claim 4. Flag-true ≠ send license on the live hop.

---

## 4. Sending now cannot be the profit path — PASS (counted dest / live capital)

Scoped proof: **counted destination profit** and **live Pepperstone `1369850`**. Not “no dest execution exists anywhere.”

### 4.1 Assigned RiskEngine cannot license a live send on the hosted shadow hop

`RiskEngine.Evaluate` allow-send:

```147:170:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
        ...
                AllowFixSend = allowSend
```

Empty L90–93 comment (“Shadow path still evaluates risk but never allows FIX send”) is **not** a force-false. If `RealExecutionEnabled && Reconciled && VenueHealthy && KillSwitch.None`, `AllowFixSend` **can be true**. Unit `Real_flag_false_never_allows_fix_send` only covers the `RealExecutionEnabled=false` default fixture.

Hosted shadow hop **does not** reach a true send license:

```20:20:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
```

```301:304:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    Quote = quote,
                    VenueHealthy = _runtime.Trade.LoggedOn && _runtime.Quote.LoggedOn,
                    RealExecutionEnabled = _runtime.RealCopyEnabled,
                    Reconciled = VenueReconciled,
```

`Reconciled=false` + increasing action ⇒ `Reject(..., "VENUE_NOT_RECONCILED")` with `AllowFixSend=false` (RiskEngine L84–85, L180–187). Persist then **overwrites** anyway:

```317:336:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                var rec = new RiskDecisionRecord
                {
                    Id = Guid.NewGuid(),
                    CopyIntentId = intent.Id,
                    Outcome = decision.Outcome,
                    ApprovedQuantity = decision.ApprovedQuantity,
                    Reason = decision.Reason,
                    AllowFixSend = false,
                    DecidedAt = now
                };
                _db.RiskDecisions.Add(rec);
                intent.RiskDecisionId = rec.Id;

                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
                else
                {
                    intent.Status = "SHADOW_ONLY";
```

Conjunction at L330 cannot fire: `VenueReconciled` is const `false`. Branch is dead. All shadow-hop intents become `SHADOW_ONLY`. Even if it fired, the status is `LIVE_SEND_BLOCKED_UNIMPLEMENTED` — still no `35=D` on this hop.

`GetStatusAsync` L67 reports `VenueReconciled: DemoDest` to the UI. That is a **status DTO lie** vs the const used in `Evaluate`. It does not change the persist hop. Adversarial note: UI “venue reconciled” on demo is not `RiskEngine.Reconciled`.

### 4.2 Counted destination profit is constructor 0

```5:22:D:\Prop\src\Application\Dashboard\DashboardModels.cs
public sealed record OverviewDto(
    ...
    decimal ShadowPnl,
    decimal DestinationRealPnl,
    decimal XauGross,
    decimal XauNet,
    ...
    bool RealCopyEnabled);
```

```29:52:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var shadowPnl = await _db.ShadowOrders.SumAsync(s => (decimal?)s.SourceVsShadowSlippage, ct) ?? 0;
        ...
        return new OverviewDto(
            ...
            shadowPnl,
            0,
            0,
            0,
            ...
            _runtime.RealCopyEnabled);
```

`DestinationRealPnl` is the first `0`. No dest-fill ledger is summed into overview. `data/demo_copy_ledger.json` dest fills are **not** this DTO.

### 4.3 Live `1369850` cannot be the dest

```37:42:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
        if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
            || !sender.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
            || account == "1369850")
        {
            return Fail("Refused: dest must be demo FIX, not live 1369850.", host, account, sourceLogin);
        }
```

`CopyTradingService.DemoDest` (L45–48) is true only when host starts `demo-`, trade sender starts `demo.`, **and** account ≠ `1369850`. `ExecuteDemoCopyAsync` returns 0 if `!DemoDest` (L485–488). `CTraderFixDemoTestTrade` L43–47 also refuses `1369850`.

### 4.4 Assigned session + assigned page cannot send

- `CTraderFixSession` = `35=A` only (claim 1).
- `LiveCopyPage.tsx` 70/70: `useCopyStatus` + `useCopyIntents` GET only. No POST, no “send”, no promote. Empty-state L57 documents dest auto-send as **hosted** behavior, not a page action.

### 4.5 Residual that does **not** fail this scoped claim

Hosted 20s tick **does** send `35=D` on **demo dest**:

```19:34:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
            ...
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

`NewOrderSingleImplemented => DemoDest` (`CopyTradingService` L50) is **true** on the lab demo host. `ExecuteDemoCopyAsync` L566 calls `CTraderFixCopyOpen.SendAsync` → `Build("D")`. Ledger on disk:

- source login `305750` / pos `21250421`
- dest pos `237339770`, clOrd `C20260818093047317`, fill `4390.2`, `DestClosed=false`

That is **demo dest execution**. It is **not** `DestinationRealPnl`. It is **not** live `1369850`. Claim 4 as a **profit-accounting / live-capital** statement holds. Claiming “no `35=D` exists in the product” would be **false** (that is not this claim).

`RiskEngine` is **not** consulted by `ExecuteDemoCopyAsync`. Demo dest send is a side hopper, not the risk-gated profit path.

---

## 5. SHADOW on demo is not destination profit — PASS

### 5.1 Paper SHADOW engine

`ShadowCopyEngine.SimulateEntry` (L35–60) computes a modeled ask/bid + 0.05 latency slip. No socket. No FIX. `SimulateExit` / `MarkToMarket` same.

Hosted persist of that paper fill:

- `GenerateShadowIntentsAsync` L336–359: status `SHADOW_ONLY` + `ShadowOrders` row from `SimulateEntry`.
- `EfTradingStore.PersistDemoShadowAsync` L267–280: only when `state == SHADOW` and a `DestinationQuotes` row exists; then `ShadowCopyEngine` again. Outbox payload is state JSON, not an order.

Overview `ShadowPnl` = `Sum(ShadowOrders.SourceVsShadowSlippage)` — a **slippage statistic**, not dest realized P&L. `DestinationRealPnl` remains `0`.

### 5.2 SHADOW is the highest scorer state

`FromBaseline` tops at `SHADOW`. `AfterHighEarlyScore() => SHADOW`. Policy `IsTraderEligible` **accepts** SHADOW (blocks only RISK_BLOCKED / DISQUALIFIED / PAUSED / INSUFFICIENT_DATA / EARLY_SCORE / WATCH). Eligibility also requires 20 completed XAU, XAU book > 0, demo/contest group (`XauUsdOneToOneCopyPolicy` L73–112). That is **source-book** profitability, not dest profit.

### 5.3 Residual: ADMITTED SHADOW can dest-send

`CopyRosterEngine.Decide` admits when `IsTraderEligible` (SHADOW + 20 + book > 0 + demo/contest). `TickRosterAsync` writes `ADMITTED`. `ExecuteDemoCopyAsync` then dest-sends for `ADMITTED` roster seats **without** re-checking SHADOW vs LIVE and **without** `RiskEngine`.

So: SHADOW **state** on demo can cause dest `35=D`. That dest fill is still **not** `DestinationRealPnl` and still **not** live `1369850`. Claim 5 as “SHADOW accounting ≠ destination profit” holds. Claiming “SHADOW never touches dest” would be **false**.

`LiveCopyPage` L14/L18 display `shadowTraders` / `shadowFills` as counts. No dest PnL column.

---

## 6. Assigned `LiveCopyPage.tsx` (70/70) — display only

```1:29:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
import { useCopyIntents, useCopyStatus } from '../api/hooks';

export default function LiveCopyPage() {
  const { data: status, isLoading } = useCopyStatus();
  const { data: intents = [] } = useCopyIntents();
  ...
        <Stat label="REAL_COPY armed" value={status?.realCopyArmed ? 'YES' : 'NO'} hot={status?.realCopyArmed} />
        <Stat label="SHADOW traders" value={status?.shadowTraders ?? 0} />
        <Stat label="LIVE traders" value={status?.liveTraders ?? 0} />
        <Stat label="Live sends" value={status?.liveSends ?? 0} />
```

- Hooks (`apps/web/src/api/hooks.ts` L60–66) are GET `/api/copy/status` and GET `/api/copy/intents`.
- `realCopyArmed` will display **YES** if the API process bound `.env=true` (claim 3). The page does not force it false.
- Blockers copy (L24) says “Live send blockers (Pepperstone cannot be filled)” — UI text, not a gate.
- Empty-state L57: “Demo dest auto-sends after a trader is ADMITTED…” — documents the residual hopper. The page still cannot send.

---

## 7. Risk to capital

| Surface | File proof | Risk |
|---|---|---|
| Live dest `1369850` | `CTraderFixCopyOpen` refuse; `DemoDest` excludes it; session hop `35=A` only | **NONE** |
| Counted dest PnL | `DestinationRealPnl` constructor `0` | **NONE** (not booked) |
| Shadow hop FIX send | persist `AllowFixSend=false`; `VenueReconciled` const false; no LIVE state from scorer | **NONE** |
| Demo dest `5328266` (public id; default logon account string) | hosted `ExecuteDemoCopyAsync` → `Build("D")`; ledger has a dest fill | **DEMO dest execution exists** (not live capital) |
| Process flag | `.env` REAL_COPY **true**, DI-bound, no post-logon pin | Armed flag; **not** live send license |

**Risk to live capital: NONE.** Residual: demo dest can and does `35=D`. This slot did not send `35=D`.

---

## 8. Claims this slot will **not** rubber-stamp

- “Product has no `35=D` builder” — **false** (`CTraderFixCopyOpen` / demo helpers). Claim 1 is **session-file** scoped.
- “`RealCopyEnabled` is false after logon” — **false** (DI + `.env=true`, no re-pin).
- “NewOrderSingle unimplemented on the hosted process” — **stale** (`NewOrderSingleImplemented => DemoDest`; demo hopper sends).
- “SHADOW never dest-sends” — **false** (roster + `ExecuteDemoCopyAsync`). SHADOW **PnL DTO** is still not dest profit.
- Any live `realCopyEnabled` JSON — **unproven** (GET blocked).

---

End of P500_VERIFY_51. Product source was not modified. No secrets printed. This slot did not send `35=D`. `REAL_COPY` was not flipped by this slot.
