# P500_VERIFY_27 — Adversarial verify (slot 27)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_27.md` |
| Agent / slot | P500 adversarial **verify 27** |
| Date | 2026-08-18 |
| Role | Independent verifier. Did **not** trust sibling P500_BOOK / W500 / CREDENTIALS prose. Re-read assigned files + the logon/copy hop they actually call. |
| Assigned files | `CTraderFixSession.cs` (135/135), `BaselineScorer.cs` (212/212), `RiskEngine.cs` (189/189), `LiveCopyPage.tsx` (70/70) |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Boolean key quoted only. |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live GET this pass | **Attempted, blocked.** `web_fetch`/`open_page` to `http://localhost:5000/api/{health,settings,copy/status,overview,ingest/status}` and `http://127.0.0.1:5000/api/copy/status` failed (SSRF/private-IP block). **No live JSON cited.** Claims that need a live body are **FAIL** (none of 1–5 required a live body to decide). |
| Live attach / order | **No.** No Manager Connect. No TLS. No Logon. No `35=D` send. |

**Honesty rule:** FAIL any claim that cannot be proven from a file or a live GET performed this slot. Prior swarm text is not evidence. `SAFE_BY_ABSENCE` is not “flag stays false.” A Logon `35=A` is not NewOrderSingle. A sibling `Build("D")` is not `CTraderFixSession`. SHADOW fill slippage is not destination PnL. Do not print secrets.

---

## 0. Verdict (binding)

**FAIL.** Four of five assigned claims are proven from live source. **Claim 3 is disproven:** `RealCopyEnabled` is **not** forced false after logon.

| # | Assigned claim | Verdict | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** (assigned `CTraderFixSession` only) | File 135/135: outbound tag 35 is `"A"` only. Grep of this file for `35=D` / `Build("D")` / `(35, "D")` = **0**. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive` is `=> false` (`BaselineScorer.cs` L211). `FromBaseline` never returns `LIVE` / `LIVE_CANDIDATE`. Unit test asserts SHADOW, not LIVE. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | Sole assignment is DI bind from env (`DependencyInjection.cs` L41). Hosted logon **reads** the flag and never writes it. Lab `.env` L73 is `true`. |
| 4 | sending now cannot be the profit path | **PASS** | Session send is logon-only. Risk hop persist `AllowFixSend=false` + `VenueReconciled=const false` + no LIVE traders. Dashboard `DestinationRealPnl` is constructor **0**. Wanting profit ≠ edge. |
| 5 | SHADOW on demo is not destination profit | **PASS** | Shadow path writes `SHADOW_ONLY` + `ShadowCopyEngine.SimulateEntry`. Overview `ShadowPnl` = sum of `SourceVsShadowSlippage`. `DestinationRealPnl` literal **0**. Dest fills (if any) live in `DemoCopyLedger`, not shadow rows. |

**Overall slot verdict: FAIL** (instruction: FAIL if any claim cannot be proven from a file or live GET).

**Risk to capital: NONE** on live Pepperstone `1369850` (`SAFE_BY_ABSENCE` on `CTraderFixSession`; `CTraderFixCopyOpen` refuses that account). That is **not** claim 3. Do not paper over the armed flag.

Stale siblings this slot contradicts: `CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**”; any W500/A014 “hosted logon re-pins false”; any BOOK that still says product `NewOrderSingleImplemented = const false` (HEAD is `=> DemoDest`); any BOOK that says product literal `35=D=0` (sibling `CTraderFixCopyOpen.Build("D")` exists).

---

## 1. no `35=D` builder — PASS (`CTraderFixSession` only)

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 physical lines, full read).

Outbound builder is `BuildLogon` only. The only MsgType written is Logon `A`:

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

Single write, then sockets dispose (`using` TcpClient + SslStream):

```46:50:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
            var seq = 1;
            var logon = BuildLogon(senderCompId, targetCompId, senderSubId, targetSubId, username, password, seq);
            var bytes = Encoding.ASCII.GetBytes(logon);
            await ssl.WriteAsync(bytes, timeoutCts.Token);
            await ssl.FlushAsync(timeoutCts.Token);
```

Inbound tag 35 is **parsed**, never built as `D`:

```55:56:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
            var msgType = Extract(reply, "35");
            if (msgType == "A")
```

Grep of this file for `35` / `"D"`: L55 extract inbound; L73 error string `Logon rejected 35={msgType}`; L96 `(35, "A")`. **Zero** `Build("D")`. **Zero** NewOrderSingle. This is a connect-logon-read-dispose probe.

### Residual (does not fail claim 1 as scoped)

| Residual | Path | Why it is not claim-1 FAIL |
|---|---|---|
| Product **does** have a `35=D` builder | `CTraderFixCopyOpen.Build` L142–156; call site L95 `Build("D", …)` | Different type. Claim 1 was assigned against `CTraderFixSession`. |
| Demo helpers | `CTraderFixDemoTestTrade` / `CTraderFixDemoMatrix` `Build("D")` | Tools / matrix, not the assigned session class. |
| Hosted demo hop | `CopyTradingService.ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.SendAsync` when `DemoDest` | Proves product-wide “`35=D=0`” is **STALE**. Does **not** put a D-builder inside `CTraderFixSession`. |

Unqualified sentence “the product has no `35=D` builder” would be **FAIL**. This slot does **not** make that sentence. Claim 1 as applied to the assigned file **PASS**.

---

## 2. `CanPromoteToLive` is false — PASS

`D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212, full read).

```188:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
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

Proof points:

- `CanPromoteToLive` ignores `current` and is a constant `false`. There is no other overload.
- `FromBaseline` terminal set = `{INSUFFICIENT_DATA, RISK_BLOCKED, SHADOW, WATCH, EARLY_SCORE}`. **Never** `LIVE` or `LIVE_CANDIDATE` (those enum values exist in `TraderState.cs` L9–10 but the scorer does not emit them).
- Ceiling after a high early score is still `SHADOW` (L209).
- `tests/Unit/BaselineScorerTests.cs` L21–26: three disciplined winners → `SuggestedState=SHADOW` and `CanPromoteToLive(...).Should().BeFalse()`.

`LiveCopyPage.tsx` L15 still renders a “LIVE traders” stat from `status.liveTraders`. That is a count of `TraderScores` already stored as `LIVE` (`CopyTradingService.GetStatusAsync` L58). It is **not** a promotion path. Scorer + state machine cannot create that state.

---

## 3. `RealCopyEnabled` forced false after logon — FAIL

**Disproven.** The four assigned files never write the runtime flag false. The only product assignment is env-bind at process start. Logon hosted service does not re-pin.

### 3.1 Assigned files (none force-false)

| File | What it does with the flag |
|---|---|
| `CTraderFixSession.cs` | No `RealCopyEnabled` token. |
| `BaselineScorer.cs` | No `RealCopyEnabled` token. |
| `RiskEngine.cs` | Consumes `request.RealExecutionEnabled`. L90–93 is an empty comment when the request flag is false. L147–150: `allowSend = request.RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. That is Evaluate math, **not** a runtime re-pin. |
| `LiveCopyPage.tsx` L13 | Displays `status?.realCopyArmed ? 'YES' : 'NO'`. Read-only UI. |

`grep RealCopyEnabled =` across `*.cs` / `*.tsx` / `*.json`: **one** assignment.

### 3.2 Sole write: DI binds env (can be true)

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`apps/api/Program.cs` L10 + L13: `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()`. Flag-only grep of `D:\Prop\.env` **L73**: `REAL_COPY_EXECUTION_ENABLED=true`.

POCO default is still off (`CTraderFixOptions.RealCopyExecutionEnabled` L35 `= false`) and `appsettings.json` FeatureFlags `LiveCopyEnabled=false` is a **different name**. Neither overrides the DI bind of `REAL_COPY_EXECUTION_ENABLED`.

### 3.3 After logon: read, never write

`CTraderFixLogonHostedService.ExecuteAsync` after `TryLogonAsync` (QUOTE then TRADE):

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

Mutations: Quote/Trade `LoggedOn` / `Status` / `LastError` / `UpdatedAt`. **No** `_runtime.RealCopyEnabled = false`. The log line **echoes** whatever DI already bound.

`LiveRuntimeStatus.RealCopyEnabled` has a public setter (L32) but no other product writer exists.

### 3.4 Downstream reads the bound value (would be true if env is true)

- `CopyTradingService.GetStatusAsync` L64: `RealCopyArmed: _runtime.RealCopyEnabled` → LiveCopyPage “REAL_COPY armed”.
- `GenerateShadowIntentsAsync` L303: `RealExecutionEnabled = _runtime.RealCopyEnabled`.
- API `GET /api/health` L55 and `GET /api/settings` L76 expose `runtime.RealCopyEnabled`.
- `CopyTradingService.BuildBlockers` L621 adds `"REAL_COPY_EXECUTION_ENABLED is false"` **only if** the bound value is already false.

Live GET of those routes was **blocked this slot**, so the running process’s boolean is **not** re-measured. File proof is enough to **FAIL** “forced false after logon”: there is no force-false statement to execute.

`SAFE_BY_ABSENCE` of a D-builder on `CTraderFixSession` is **not** this claim.

---

## 4. sending now cannot be the profit path — PASS

“Profit path” here means: measured destination / live-capital PnL produced by sending orders now. That path is not open.

### 4.1 `CTraderFixSession` cannot send an order

Proven in §1. One `WriteAsync` of `35=A`. Process then disposes the socket. No clOrdId, no qty, no symbol.

### 4.2 Scored / risk / shadow hop cannot send live

`CopyTradingService`:

| Gate | File pin | Effect |
|---|---|---|
| `VenueReconciled` | L20 `const bool … = false` | `RiskEngine.Evaluate` with `Reconciled=false` + increasing action → `VENUE_NOT_RECONCILED` (`RiskEngine.cs` L84–85). `allowSend` also requires `Reconciled` (L147–150). |
| Persist overwrite | L324 `AllowFixSend = false` | Even if Evaluate returned true, the row written is false. |
| LIVE send branch | L330 `decision.AllowFixSend && score.CurrentState == LIVE && NewOrderSingleImplemented && VenueReconciled` | Dead: `VenueReconciled` is const false **and** scorer cannot emit LIVE (§2). Else branch is `SHADOW_ONLY` (L336). |
| Live 1369850 | `DemoDest` L45–48 requires demo host + `demo.` sender + account **≠** `1369850` | `NewOrderSingleImplemented => DemoDest` (L50). Live identity never implements NOS. |

`RiskEngine` L90–93 comment claims the shadow path “never allows FIX send.” The **code** that makes `AllowFixSend` false on approve is L147 (`RealExecutionEnabled && …`), plus persist L324. Rejects always write `AllowFixSend=false` (L180–188). Unit test `Real_flag_false_never_allows_fix_send` (`RiskEngineTests.cs` L21–26) only covers the request flag false case.

`LiveCopyPage.tsx` L22–27 labels blockers “Live send blockers (Pepperstone cannot be filled)”. That is UI honesty, not a sender.

### 4.3 Dashboard dest PnL is not computed from sends

```29:46:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var shadowPnl = await _db.ShadowOrders.SumAsync(s => (decimal?)s.SourceVsShadowSlippage, ct) ?? 0;
        ...
            shadowPnl,
            0,
            0,
            0,
```

`OverviewDto` L15–16: those three zeros are `DestinationRealPnl`, `XauGross`, `XauNet`. Overview page renders dest real P&L from that constructor **0**. Sending now cannot change the dest-profit number the product reports.

### 4.4 Residual send that is still not the profit path

`CopyTradingHostedService` L28–30 ticks roster → shadow intents → `ExecuteDemoCopyAsync` every 20s. When `DemoDest` is true, that method calls `CTraderFixCopyOpen.SendAsync`, which **does** `Build("D")` (L95) after a demo-only identity check (refuse `1369850`, require `demo-` host + `demo.` sender).

That is a **demo dest** NewOrderSingle, **bypassing** `RiskEngine.Evaluate`. It is:

- not live Pepperstone;
- not scored SHADOW PnL;
- not `DestinationRealPnl` (still 0);
- not an edge: copy policy is 1:1 lots (`AllocationFactor=1m`); copy-all of `RISK_BLOCKED` tape is negative EV (not re-summed this slot — not claimed as a new dollar figure).

`LiveCopyPage.tsx` L56–58 empty copy: “Demo dest auto-sends after a trader is ADMITTED … Live 1369850 is never used” is consistent with `GetStatusAsync` summary L76–78.

On-disk `D:\Prop\data\demo_copy_ledger.json` has one **unmarked** dest row (`SourceLogin=305750`, `Lots=0.01`, `DestPositionId=237339770`, `DestClosed=false`). That is demo-ledger state, **not** destination profit in the dashboard, **not** SHADOW. This slot did not send and did not mark it.

Wanting a profitable send is not an edge. Sending now is not the dest-profit path.

---

## 5. SHADOW on demo is not destination profit — PASS

### 5.1 What SHADOW writes

`GenerateShadowIntentsAsync` (only for `SHADOW` / `LIVE_CANDIDATE` / `LIVE` **and** roster `ADMITTED`):

- Intent status `SHADOW_ONLY` (L336–337).
- Optional `ShadowOrder` from `ShadowCopyEngine.SimulateEntry` (L339–359): modeled quote + 0.05 point latency slip. **No socket. No `35=D`.**

`ShadowCopyEngine` L35–61: `SimulateEntry` returns a `ShadowFill` priced from the last stored `DestinationQuote`. `MarkToMarket` is unused by the persist path.

Close intents are also `SHADOW_ONLY` (L403) with **no** shadow exit row.

### 5.2 What the product calls “Shadow P&L”

`EfDashboardQueries` L29: `ShadowPnl = Sum(ShadowOrders.SourceVsShadowSlippage)`. That is **source-vs-shadow slippage**, not dest-account realized PnL, not demo-ledger mark, not `DestFillPrice`.

`OverviewPage.tsx` L26–27 shows “Shadow P&L” and “Dest. real P&L” as **separate** cards. Dest card is the literal 0 from §4.3.

`LiveCopyPage.tsx` L14 vs L16 vs L18: SHADOW trader count, live sends, and shadow fills are three different stats. Empty-state copy talks about **demo dest auto-sends** as a different mechanism from SHADOW fills.

`ShadowPortfolioPage.tsx` L6–10: “Shadow fills use the cTrader QUOTE session … Live NewOrderSingle remains disabled.” Quote-derived simulation ≠ dest TRADE fill.

### 5.3 Demo dest fills are a different store

`DemoCopyLedger` (`D:\Prop\data\demo_copy_ledger.json`) holds dest position / clOrdId / fill price. `ExecuteDemoCopyAsync` never writes `ShadowOrders`. A SHADOW row cannot become dest profit by being on a demo host.

Policy residual (does not flip claim 5): `XauUsdOneToOneCopyPolicy` **requires** demo/contest group (`NOT_DEMO_OR_CONTEST_GROUP` L105–108). That admits SHADOW **source** traders from demo/contest MT5 groups. It does not turn their shadow slippage into destination dollars.

---

## 6. Risk to capital

| Surface | State this slot |
|---|---|
| Live Pepperstone `1369850` | **NONE.** `CTraderFixSession` = `35=A` only. `CTraderFixCopyOpen` L37–42 refuses that account. `DemoDest` is false for it. `CanPromoteToLive=false`. Persist `AllowFixSend=false`. |
| Runtime flag | **Armed in config** if `.env` L73 is loaded (`true`). Not force-false after logon. Flag ≠ ticket. |
| Demo dest hop | Residual: hosted 20s tick **can** `Build("D")` when host/sender/account are demo. Ledger shows 0.01 lot dest id `237339770` unmarked. **Not live capital.** Not measured as `DestinationRealPnl`. |
| SHADOW | Paper. Slippage sum only. |

**Risk to capital: NONE** (`SAFE_BY_ABSENCE` on live `1369850`). Claim 3 still **FAIL**.

---

## 7. What this slot did not do

- Did not obtain a live GET body (tooling blocked loopback).
- Did not attach Manager or FIX.
- Did not re-sum copy-all 8463 / `RISK_BLOCKED` −$241,580 (prior pins; not this slot’s dollar claim).
- Did not edit product, tests, or `.env`.

---

## 8. Sources (absolute paths, no secrets)

- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Application\Dashboard\DashboardModels.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\tests\Unit\BaselineScorerTests.cs`
- `D:\Prop\tests\Unit\RiskEngineTests.cs`
- `D:\Prop\.env` L73 boolean only
