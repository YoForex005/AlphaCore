# P500_VERIFY_3 — Adversarial verifier (slot 3)

| Field | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_3 |
| Slot | **3** |
| Role | Adversarial verifier. Read the four named files myself. Do not trust other agents. |
| Assigned | Confirm: (1) no `35=D` builder; (2) `CanPromoteToLive` is false; (3) `RealCopyEnabled` forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. |
| Product source modified | **No** |
| Live GET this slot | **Attempted, not proven.** `browse_page` to `http://localhost:5000/api/health` and `/api/settings` failed. `web_fetch` to `localhost` / `127.0.0.1` blocked (SSRF). Runtime flag value is therefore **UNVERIFIED**. File proof still kills claim 3. |
| Secret values printed | **None** (quoted only the on-disk boolean `REAL_COPY_EXECUTION_ENABLED=true`; no passwords, no connection strings, no FIX password) |
| Live `35=D` sent by this slot | **No** |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_3.md` |

**Honesty rule:** prove every claim from a file as it sits on disk, or from a live GET. FAIL any claim that is not proven. Prior swarm notes (`A014`, `A015`, `CREDENTIALS_AND_COPY_STATUS.md`, W500 “hosted re-pin”) are **not** evidence. Wanting dest profit is not an edge. Copy-all 8463 would copy `RISK_BLOCKED` losses.

SUT read in full this slot:

| File | Path | Lines read |
|---|---|---|
| `CTraderFixSession.cs` | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 135/135 |
| `BaselineScorer.cs` | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | 212/212 |
| `RiskEngine.cs` | `D:\Prop\src\Domain\Risk\RiskEngine.cs` | 189/189 |
| `LiveCopyPage.tsx` | `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | 70/70 |

Supporting files read only to test (not rubber-stamp) the five claims: `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixOptions.cs`, `Program.cs` (API), `.env` L73 boolean only, `EfDashboardQueries.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `EfTradingStore.cs`, `DealIngestionService.cs`, `hooks.ts`, `TraderState.cs`.

SHA-256 of the four SUT files: **not computed** this slot (no shell). Do not recycle stale hashes from C37 (`LiveCopyPage` 321 B) or D12 (`BaselineScorer`). Those sizes do not match the files just read.

---

## Verdict

**FAIL** — claims 1, 2, 4, 5 are file-proven. **Claim 3 is disproven.**

| # | Claim | Result | Why |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** (scoped to `CTraderFixSession.cs`) | 135/135: only outbound MsgType is `(35, "A")` L96. One `WriteAsync`. No `NewOrderSingle`, no `Build("D")`. Residual: siblings **do** build `D`. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(_) => false` L211. `FromBaseline` reachable set has no `LIVE`. Persist copies `SuggestedState`. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | Logon host **logs** the flag (L70) and never assigns it. The only product write is DI bind of `.env` `REAL_COPY_EXECUTION_ENABLED=true`. |
| 4 | sending now cannot be the profit path | **PASS** (live dest capital) | Session cannot send `D`. Scorer cannot emit `LIVE`. Shadow persist `AllowFixSend=false`. Overview `DestinationRealPnl` is constructor `0`. Residual: demo dest **does** send `35=D` (paper, not 1369850). |
| 5 | SHADOW on demo is not destination profit | **PASS** | `SHADOW` is a score label + `SimulateEntry` paper book. Dest PnL reported `0`. Residual: `SHADOW` is the roster gate that can admit **demo** dest tickets; those tickets are still not live dest profit. |

Overall **FAIL** because claim 3 cannot be proven and is contradicted by the files.

Risk to capital: **NONE** for live Pepperstone `1369850` (`SAFE_BY_ABSENCE` on `CTraderFixSession` + `CTraderFixCopyOpen` refuse). Residual: demo dest `Build("D")` on the 20s copy tick is **paper**, not live capital. This slot did not send anything.

---

## 1. No `35=D` builder — PASS (`CTraderFixSession.cs` only)

Read: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135).

The class is logon-only. The sole outbound assembler is `BuildLogon`. The sole wire write is that logon.

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

```46:50:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
            var seq = 1;
            var logon = BuildLogon(senderCompId, targetCompId, senderSubId, targetSubId, username, password, seq);
            var bytes = Encoding.ASCII.GetBytes(logon);
            await ssl.WriteAsync(bytes, timeoutCts.Token);
            await ssl.FlushAsync(timeoutCts.Token);
```

Inbound `35` is **read**, never sent as `D`:

```55:56:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
            var msgType = Extract(reply, "35");
            if (msgType == "A")
```

Grep of this file: `NewOrderSingle` = 0; `Build("D")` = 0; `(35, "D")` = 0; `35=D` = 0. `TcpClient` / `SslStream` are `using`-disposed after the probe. Hosted caller `CTraderFixLogonHostedService` invokes `TryLogonAsync` twice (QUOTE 5211, TRADE 5212) and never writes another MsgType through this class.

**Adversarial residual (does not fail claim 1 as stated):** siblings in the same folder **are** `35=D` builders.

| Sibling | Evidence |
|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra.ToArray())` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | `Build("D", …)` ×3 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` L93 | `Build("D", …)` |

`CTraderFixCopyOpen.SendAsync` is called from `CopyTradingService.ExecuteDemoCopyAsync` (product copy tick). It refuses live identity (`host` must start `demo-`, `sender` must start `demo.`, `account == "1369850"` refused). That is a **demo** sender, not a builder inside `CTraderFixSession.cs`. Claim 1 as scoped to the assigned session file **PASSES**.

If someone restates claim 1 as “the process has no `35=D` builder,” that restatement is **false** and would FAIL.

---

## 2. `CanPromoteToLive` is false — PASS

Read: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212).

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

Proven:

- `CanPromoteToLive` is a constant `false`. The `current` argument is ignored.
- `FromBaseline` reachable set = `{INSUFFICIENT_DATA, RISK_BLOCKED, SHADOW, WATCH, EARLY_SCORE}`. **No `LIVE`. No `LIVE_CANDIDATE`.**
- `AfterHighEarlyScore()` is `SHADOW`, not `LIVE`.
- Persist path copies the scorer, it does not promote:

```140:144:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
            CurrentState = score.SuggestedState,
            LastScoredAt = DateTimeOffset.UtcNow
        }, ct);

        await _store.PersistDemoShadowAsync(brokerId, login, score.SuggestedState, completedXau, ct);
```

```232:232:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
            existing.CurrentState = score.CurrentState;
```

Unit pin: `BaselineScorerTests` “Three_disciplined_winners_go_to_shadow_not_live” asserts `SuggestedState == SHADOW` and `CanPromoteToLive(...) == false`.

`TraderState.LIVE = 5` exists on the enum. Existence of the enum member is not a promotion path. Product C# never assigns `CurrentState = TraderState.LIVE`. A hand-edited DB row could still count as `liveTraders` on `/api/copy/status`. That is not `CanPromoteToLive`.

Claim 2 **PASSES**.

---

## 3. `RealCopyEnabled` forced false after logon — FAIL

This is the claim that fails the slot.

### 3.1 Logon does not write the flag

Read: `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`.

After QUOTE/TRADE `TryLogonAsync`, the host writes session health only:

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

`_runtime.RealCopyEnabled` is an **argument to a log line**. There is no `_runtime.RealCopyEnabled = false` in this file. Older A015 / CREDENTIALS “forced false after logon” is **stale vs HEAD**.

Grep of product `*.cs` for `_runtime.RealCopyEnabled`: six hits, **zero assignments**. Reads only (dashboard, copy status, risk eval input, logon log, blocker string).

### 3.2 The only product write binds env `true`

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Lab `.env` L73 (boolean only; no other keys quoted):

```
REAL_COPY_EXECUTION_ENABLED=true
```

API `Program.cs` L10 loads that file (`EnvFile.FindAndLoad()`), L13 adds environment variables, L15 calls `AddTraderIntelligence`. Therefore **if** this process starts against this `.env`, `LiveRuntimeStatus.RealCopyEnabled` is **true** at construction and is **not** cleared after logon.

`CTraderFixOptions.RealCopyExecutionEnabled` defaults `false` (`CTraderFixOptions.cs` L35). That POCO is **not** what DI writes onto `LiveRuntimeStatus`. Different object. Cannot rescue claim 3.

`apps/fix-worker/Worker.cs` L21 reads a third key, `CTrader:RealCopyExecutionEnabled`, default `false`, and only logs. It does not pin `LiveRuntimeStatus`.

### 3.3 Live GET

Required to prove the **running** process, not the file.

| Probe | Result |
|---|---|
| `browse_page` `http://localhost:5000/api/health` | fail |
| `browse_page` `http://localhost:5000/api/settings` | fail |
| `web_fetch` `http://localhost:5000/api/health` | SSRF blocked (localhost) |
| `web_fetch` `http://127.0.0.1:5000/api/copy/status` | SSRF blocked |

Runtime `realCopyEnabled` on the wire is **UNVERIFIED** this slot. That does not save claim 3. The claim is “forced false after logon.” The logon source does not force it. The DI source forces whatever the env boolean is. Env is `true`. Claim 3 is **disproven from files**.

`GET /api/health` (file) would emit `realCopyEnabled = runtime.RealCopyEnabled` (`Program.cs` L55). `GET /api/settings` emits `featureFlags.REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled` (L76). `GET /api/copy/status` emits `RealCopyArmed: _runtime.RealCopyEnabled` (`CopyTradingService.cs` L64). None of those endpoints write `false` after logon.

Claim 3 **FAIL**.

---

## 4. Sending now cannot be the profit path — PASS (live dest capital)

“Profit path” here means **measured destination capital P&L on live Pepperstone**. Wanting that P&L does not create it.

### 4.1 Assigned session cannot send a ticket

§1: `CTraderFixSession` outbound MsgType is only `A`. No NewOrderSingle. Socket disposed after logon. Logon host never keeps a TRADE socket for later `D`.

### 4.2 Scorer cannot unlock the live-send branch

§2: `CanPromoteToLive => false`. `FromBaseline` never returns `LIVE`. The only live-send *status string* in the shadow generator is unreachable via scoring:

```330:337:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
                else
                {
                    intent.Status = "SHADOW_ONLY";
```

Even if a row were already `LIVE`, this branch still does **not send**. It writes `LIVE_SEND_BLOCKED_UNIMPLEMENTED`. And `VenueReconciled` used here is the **const false**:

```20:20:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
```

```304:304:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    Reconciled = VenueReconciled,
```

```324:324:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    AllowFixSend = false,
```

Persist **forces** `AllowFixSend = false` regardless of `decision.AllowFixSend`.

### 4.3 RiskEngine never licenses a live send on this hop

Read: `D:\Prop\src\Domain\Risk\RiskEngine.cs` (189/189).

```90:93:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.RealExecutionEnabled == false && request.Action != CopyIntentAction.CloseExposure)
        {
            // Shadow path still evaluates risk but never allows FIX send.
        }
```

That `if` is a **no-op**. It does not return. The actual send latch is later:

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

Copy hop passes `Reconciled = false` (const). Therefore `Evaluate` returns `AllowFixSend = false` on every approve. Every `Reject` also sets `AllowFixSend = false` (L187). Risk rejects (stale quote, martingale, max loss, …) would cut dest loss **if** a live send existed. They are not a dest-profit engine. They are unused on a hop that cannot send live.

### 4.4 Product dest PnL is a constructor zero

```43:52:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            shadowPnl,
            0,
            0,
            0,
            _runtime.Brokers.Values.Count(b => b.Connected) > 0,
```

`OverviewDto` field order (`DashboardModels.cs` L15–17): `ShadowPnl`, **`DestinationRealPnl`**, `XauGross`, `XauNet`. The three zeros are dest real PnL and dest XAU exposure. The API cannot report dest profit because it never measures it.

`ShadowPnl` is `Sum(SourceVsShadowSlippage)` — modeled slippage, not dest venue PnL.

### 4.5 Policy refuses lookahead “wait until it is profitable”

```57:61:D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs
/// Live copy selects <b>traders</b> with a measured XAUUSD edge, then copies their
/// next XAUUSD events 1:1 (lots, SL/TP, side). It does not wait until a ticket
/// is profitable — that is lookahead and cannot be traded live.
/// Close is copied when the source closes, not at a predicted time.
```

Sending a currently open source ticket is copy, not a dest-profit realization. High `EarlyQualityScore` is also not dest profit: quality starts at 50, `NetPnl > 0` only adds 15, and `quality >= 70 && risk < 40` can still land `SHADOW` with **non-positive** net (50 + 20 behavior − 0 risk = 70). Copy-all 8463 would include `RISK_BLOCKED` books. Wanting profit ≠ edge.

### 4.6 UI cannot press send

Read: `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (70/70).

Hooks: `useCopyStatus` → `GET /api/copy/status`; `useCopyIntents` → `GET /api/copy/intents`. No `POST`, no button, no mutate. Title is “Live copy portfolio.” Blocker banner text is honest: “Live send blockers (Pepperstone cannot be filled).”

Empty-state copy (L57) says demo dest auto-sends after `ADMITTED`. That is the residual below, not a UI send path.

### 4.7 Residual that does **not** fail this claim as live profit

`CopyTradingHostedService` every 20s:

```28:30:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

`ExecuteDemoCopyAsync` **bypasses** `RiskEngine`. It does **not** read `RealCopyEnabled`. Gate is `DemoDest` only (demo host + demo sender + account ≠ `1369850`). Then `CTraderFixCopyOpen.SendAsync` → `Build("D")`. `NewOrderSingleImplemented => DemoDest` (no longer a const false). Status summary on demo: “Demo dest auto-copy ON… Live 1369850 is never used.”

That is a **demo paper** send path. It is not live Pepperstone dest profit. Dashboard dest PnL remains `0`. Claim 4 as **live dest profit path** **PASSES**.

If someone restates claim 4 as “nothing in this process can emit `35=D`,” that restatement is **false** (demo dest sender).

---

## 5. SHADOW on demo is not destination profit — PASS

### 5.1 `SHADOW` is a score label, not a dest fill

`FromBaseline` top non-blocked eligible state is `SHADOW` (quality ≥ 70, risk < 40). That is source-tape scoring. It does not book dest cash.

### 5.2 The SHADOW book is `SimulateEntry`

```335:346:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                else
                {
                    intent.Status = "SHADOW_ONLY";
                    if (quote is not null && decision.Outcome == RiskDecisionOutcome.Approve)
                    {
                        var fill = _shadow.SimulateEntry(
                            intent.Id.ToString(),
                            trade.Direction,
                            qty,
                            trade.EntryVwap,
                            quote,
                            now,
                            TimeSpan.FromMilliseconds(80));
```

`ShadowCopyEngine.SimulateEntry` (`D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` L35–61) writes a modeled price (`Ask`/`Bid` ± 0.05 latency) into `ShadowOrders`. No FIX. No dest account. `PersistDemoShadowAsync` likewise only runs when `state == TraderState.SHADOW` and writes `Status = "SHADOW_ONLY"` + `SimulateEntry` (`EfTradingStore.cs` L267–312).

Overview “shadow PnL” = sum of `SourceVsShadowSlippage` (§4.4). That is not dest venue P&L. Dest real PnL is hardcoded `0`.

### 5.3 LiveCopyPage does not treat SHADOW as dest profit

The page counts `status.shadowTraders`, `status.shadowFills`, `status.liveSends`, `status.liveTraders`. It does not show dest cash. It warns Pepperstone cannot be filled. SHADOW count on a demo dest host is still a score/shadow-book count.

### 5.4 Residual: SHADOW is the **admission gate** for demo dest tickets

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET`. `CopyRosterEngine.Decide` admits only when that policy returns true (plus demo/contest group, no size-pattern, `XauNetPnl > 0`, ≥ 20 completed XAU). `ExecuteDemoCopyAsync` then sends `D` for `ADMITTED` seats.

So a `SHADOW` source **can** cause a **demo** dest ticket. That ticket’s P&L, if any, is demo paper and is **not** what the SHADOW book or `DestinationRealPnl` reports. Claim 5 as stated (“SHADOW on demo is not destination profit”) **PASSES**. Demo dest tickets are a different object than the SHADOW book.

---

## Cross-claim honesty (do not greenwash)

| Tempting sentence | File truth |
|---|---|
| “Hosted logon forces `RealCopyEnabled=false`.” | **False on HEAD.** Log line only. |
| “DI hard-pins false.” | **False on HEAD.** DI binds env exact `"true"`. |
| “Product has zero `35=D`.” | **False.** Session file has zero. `CTraderFixCopyOpen` / demo helpers build `D`. |
| “`NewOrderSingleImplemented` is const false.” | **Stale.** Now `=> DemoDest`. |
| “Quality 70+ is dest profit.” | **False.** Quality is a source-tape number; dest PnL is `0`. |
| “Copy all logins.” | Would copy `RISK_BLOCKED` losers. `FromBaseline` explicitly emits that state. |
| “Sending now makes dest money.” | Live 1369850 cannot be filled from these four files + CopyOpen refuse. Demo dest can fill paper. Dashboard still reports dest PnL `0`. |

---

## Live GET appendix

This slot **cannot** quote a live JSON body. Endpoints that would have been used (file routes only):

- `GET /api/health` → `realCopyEnabled`
- `GET /api/settings` → `featureFlags.REAL_COPY_EXECUTION_ENABLED`
- `GET /api/copy/status` → `realCopyArmed`, `newOrderSingleImplemented`, `venueReconciled`, `summary`, `blockers`
- `GET /api/overview` → `destinationRealPnl`, `realCopyEnabled`, `shadow`, `live`

`GET /api/copy/status` `VenueReconciled` field is `DemoDest` (L67), **not** the const `VenueReconciled = false` used in risk eval. Status can say reconciled-on-demo while the shadow risk hop still passes `false`. Do not treat that DTO as a live-send license.

---

## What this slot did not do

- Did not modify product source.
- Did not send FIX `35=D`.
- Did not attach MT5 Manager / re-count 8460 vs 8463.
- Did not compute SHA-256 (no shell).
- Did not obtain a live GET body (localhost blocked).

---

## JSON pin (slot 3)

```json
{
  "slot": 3,
  "verdict": "FAIL",
  "evidence": "D:\\\\Prop\\\\reports\\\\swarm\\\\20260818\\\\P500_VERIFY_3.md",
  "risk_to_capital": "NONE for live 1369850 (CTraderFixSession 35=A only; CopyOpen refuses 1369850). Residual demo dest Build(D) is paper, not live capital."
}
```
