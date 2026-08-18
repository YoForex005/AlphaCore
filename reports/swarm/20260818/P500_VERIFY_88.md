# P500_VERIFY_88 — Adversarial four-file confirm (slot 88)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_88.md` |
| Slot | **88** |
| Agent | P500_VERIFY_88 (adversarial verifier; independent HEAD re-read this slot) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned SUT (read in full) | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135), `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212), `D:\Prop\src\Domain\Risk\RiskEngine.cs` (189/189), `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (70/70) |
| Adjacent (opened only to prove or disprove 1, 3–5) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `ShadowCopyEngine.cs`, `CopyRosterEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyGroupFilter.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `apps/api/Program.cs`, `DemoCopyLedger.cs`, `data/demo_copy_ledger.json`, `tests/Unit/BaselineScorerTests.cs`, `tests/Unit/RiskEngineTests.cs`, `tools/DemoFixTestTrade/Program.cs` (call sites only), `.env` **booleans / public dest ids only** |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No.** `REAL_COPY` was not flipped. |
| Live `35=D` sent this slot | **No.** Not constructed. Not written. |
| Secrets printed | **None.** Quoted only booleans `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73) and `FEATURE_COPY_TRADING_ENABLED=true` (`.env` L106), public dest `5328266`, live refuse id `1369850`, public sender prefix `demo.pepperstone.5328266`. No password, token, DSN, or tag `554` value. |
| Live GET this pass | **Attempted, blocked.** `GET http://127.0.0.1:5000/api/health` → `SSRF blocked: 127.0.0.1`. `GET http://localhost:5000/api/copy/status` → page fetch failed. Runtime `realCopyEnabled` **not** process-proven. File proof is enough to **disprove** claim 3. |
| Binding rule | **FAIL any assigned claim that cannot be proven from a live file or a live GET.** Sibling `P500_VERIFY_*` / `W500_*` / `P500_BOOK_*` text is **not** evidence. Prior slogans “product `35=D=0`”, “`NewOrderSingleImplemented=const false`”, “logon re-pins `RealCopyEnabled=false`”, “`SAFE_BY_ABSENCE` everywhere” are **STALE vs this HEAD re-read**. |

**Honesty:** Wanting dest profit is not an edge. An armed env bit with no live-1369850 sender is still an armed bit. Demo dest send is dest **exposure**, not booked dest cash. Dashboard `DestinationRealPnl=0` is a constructor constant, not a mark-to-market of dest cash. This FAIL is **not** a license to send.

```text
CTraderFixSession outbound is 35=A only (1 WriteAsync).
Product 35=D builders exist (CopyOpen / DemoTestTrade / DemoMatrix).
CanPromoteToLive => false.
RealCopyEnabled is NOT forced false after logon (sole write = DI bind; .env true).
Hosted 20s tick can 35=D on demo dest; live 1369850 refused.
SHADOW ledger is SimulateEntry / slippage, not dest P&L.
SHADOW is also dest AUTO_ADMIT floor.
Live GET this slot = SSRF blocked.
This slot sent 0.
```

---

## 0. Verdict (binding)

**FAIL.** Claim 3 is **disproven** on disk. Claims 1 and 4 cannot be confirmed as written (unscoped / contradicted by hosted demo hop). Claim 2 is file-proven. Claim 5 is paper-ledger proven and dest-class unproven.

| # | Assigned claim | Measured | Class |
|---|---|---|---|
| 1 | no `35=D` builder | Assigned session file: **no** builder. Product: **yes** (`CTraderFixCopyOpen.Build("D")` hosted). | **FAIL** unscoped / **PASS_SCOPED** on `CTraderFixSession` |
| 2 | `CanPromoteToLive` is false | Literal `=> false`. `FromBaseline` never returns `LIVE`. | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | Sole C# write is DI bind of `.env=true`. Logon host logs the bit; never assigns. | **FAIL (disproven)** |
| 4 | sending now cannot be the profit path | Hosted 20s `ExecuteDemoCopyAsync` → dest `35=D` on demo dest. On-disk dest fill open. Dest DTO `0` is not dest cash. Live GET of dest P&L missing. | **FAIL (contradicted / unproven as written)** |
| 5 | SHADOW on demo is not destination profit | `SHADOW` paper book is `SimulateEntry` + slippage. Dest DTO is a separate literal `0`. Unscoped: SHADOW is dest AUTO_ADMIT floor; hopper does not require `LIVE`. | **PASS_PAPER** / **FAIL** unscoped |

One-line:

```text
FAIL slot 88: CTraderFixSession 35=A only (no D builder in that file); CanPromoteToLive=>false; RealCopyEnabled NOT forced false after logon (.env true + DI bind + logon logs-only); send-now CAN be demo dest exposure (ExecuteDemoCopyAsync → Build("D"); ledger 305750/237339770 open); SHADOW-on-demo paper ≠ dest PnL, but SHADOW is dest ADMIT floor. Risk NONE on live 1369850. Demo dest hop WIRED. Live GET blocked.
```

Do **not** treat this FAIL as a license to send. Do **not** flip leftover `.env=true` into go-live. Operator should restore `REAL_COPY_EXECUTION_ENABLED=false` (this slot did **not** edit it).

---

## 1. What was read (HEAD, this slot)

| File | Lines (this read) | Used for |
|---|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 135 | Claim 1 |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | 212 | Claim 2 |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | 189 | Claims 3–4 (`AllowFixSend`) |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | 70 | Claims 4–5 (UI honesty) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | 112 | Claim 3 |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 61 | Claim 3 |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | 66 | Claim 3 |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | 625 | Claims 1, 4, 5 |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 44 | Claim 4 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | 223 | Claims 1, 4 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | L139/163/197 | Claim 1 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | L22–28, L91–93, L105+ | Claim 1 |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | 91 | Claim 5 |
| `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs` | 136 | Claims 4–5 (ADMIT SHADOW) |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | 188 | Claims 4–5 |
| `D:\Prop\src\Domain\Copy\CopyGroupFilter.cs` | 24 | Claim 5 (demo/contest only) |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | L21–53 dest ctor | Claims 4–5 |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | L5–22 | Claims 4–5 (field order) |
| `D:\Prop\src\Infrastructure\Copy\DemoCopyLedger.cs` | 40 | Claim 4 (path + dest fill fields) |
| `D:\Prop\data\demo_copy_ledger.json` | 12 | Claim 4 (on-disk dest fill) |
| `D:\Prop\apps\api\Program.cs` | L33–76 health/settings | Claim 3 live-shape |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | L20–27 | Claim 2 lock |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | L20–26 | Claim 3–4 (flag-false) |
| `D:\Prop\.env` L73, L106 | booleans only | Claim 3 |

---

## 2. Claim 1 — no `35=D` builder — FAIL unscoped / PASS_SCOPED (`CTraderFixSession`)

Assigned file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` is **135** lines. Read in full this slot.

The only outbound assembler is `BuildLogon`. Tag 35 is hardcoded `"A"`:

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

Single `WriteAsync` (L49) of that logon. Then one `ReadAsync`. Socket disposed via `using`. Inbound `Extract(reply, "35")` (L55) is reply parse, not a builder. Tokens in this file: `Build("D")` = 0, `(35, "D")` = 0, `NewOrderSingle` = 0.

Hosted caller of **this** type is logon-only:

```48:58:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            _config["CTRADER_FIX_QUOTE_SENDER_SUB_ID"] ?? "QUOTE",
            _config["CTRADER_FIX_QUOTE_TARGET_SUB_ID"] ?? "QUOTE",
            username, password, stoppingToken);

        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            _config["CTRADER_FIX_TRADE_SENDER_SUB_ID"] ?? "TRADE",
            _config["CTRADER_FIX_TRADE_TARGET_SUB_ID"] ?? "TRADE",
            username, password, stoppingToken);
```

Assigned `RiskEngine.cs` / `BaselineScorer.cs` / `LiveCopyPage.tsx` contain **0** FIX builders. That does not license “no `35=D` builder” as a product fact.

**Unscoped product claim fails.** Same `Sessions/` folder has a real NewOrderSingle builder, and the copy host **calls it**:

```95:95:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
            await Write(ssl, Build("D", sender, target, seq, extra.ToArray()), timeout.Token);
```

```142:149:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
    private static string Build(string type, string sender, string target, int seq, params (int, string)[] extra)
    {
        var fields = new List<(int, string)>
        {
            (35, type), (34, seq.ToString(CultureInfo.InvariantCulture)),
            (49, sender), (56, target), (50, "TRADE"), (57, "TRADE"),
            (52, DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture))
        };
```

`Build("D", …)` therefore emits tag `35=D`. CopyOpen refuses live identity at L37–41 (`!host.StartsWith("demo-")` **or** `!sender.StartsWith("demo.")` **or** `account == "1369850"`). That gate is **not** “no builder.”

Additional product `Build("D")` sites this slot counted:

| File | Sites | Hosted? |
|---|---|---|
| `CTraderFixCopyOpen.cs` L95 | 1 | **Yes** — `CopyTradingService.ExecuteDemoCopyAsync` L528 close + L566 open |
| `CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | 3 | No — `tools/DemoFixTestTrade` CLI |
| `CTraderFixDemoMatrix.cs` `SendD` L91–93 (called L105, L114, L120, L126, L133, L149, L165, L181, L196) | 1 builder, many sends | No — same CLI |

Product builder count = **3 types / 5 `Build("D")` call sites**. Matrix `SendD` is the same assembler. DemoTestTrade / DemoMatrix also refuse live `1369850` / non-`demo-` host / non-`demo.` sender.

`NewOrderSingleImplemented` is **not** a const false. HEAD:

```50:50:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public bool NewOrderSingleImplemented => DemoDest;
```

`DemoDest` is true when host starts with `demo-`, sender starts with `demo.`, and account is not `1369850` (L45–48). Prior swarm “NOS=const false / SAFE_BY_ABSENCE on the copy hop” is **STALE**.

**Claim 1 as written = FAIL.** Scoped to `CTraderFixSession.cs` only = PASS.

---

## 3. Claim 2 — `CanPromoteToLive` is false — PASS

`TraderStateMachine` lives in the assigned `BaselineScorer.cs` file.

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

`current` is unused. There is no other overload.

`FromBaseline` never returns `LIVE` or `LIVE_CANDIDATE`. Exhaustive exits this slot:

| Condition | Return |
|---|---|
| `CompletedXauTrades == 0` | `INSUFFICIENT_DATA` |
| `risk >= 80` or (martingale + drawdown + net < 0) | `RISK_BLOCKED` |
| `!earlyEligible` | `INSUFFICIENT_DATA` |
| `quality >= 70 && risk < 40` | **`SHADOW` (ceiling)** |
| `quality >= 55` | `WATCH` |
| else | `EARLY_SCORE` |

```189:206:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState FromBaseline(bool earlyEligible, decimal quality, decimal risk, FeatureSnapshot features)
    {
        if (features.CompletedXauTrades == 0)
            return TraderState.INSUFFICIENT_DATA;
        // ...
        if (quality >= 70 && risk < 40)
            return TraderState.SHADOW;
        if (quality >= 55)
            return TraderState.WATCH;
        return TraderState.EARLY_SCORE;
    }
```

Unit lock:

```20:27:D:\Prop\tests\Unit\BaselineScorerTests.cs
    public void Three_disciplined_winners_go_to_shadow_not_live()
    {
        var score = _s.Score(new[] { Closed(1, 80), Closed(2, 70), Closed(3, 90) });
        score.EarlyScoreEligible.Should().BeTrue();
        score.SuggestedState.Should().Be(TraderState.SHADOW);
        TraderStateMachine.CanPromoteToLive(score.SuggestedState).Should().BeFalse();
    }
```

`TraderState.LIVE` still exists as an enum (`TraderState.cs` L10). That is a type, not a promotion path. Nothing in the four assigned files writes `CurrentState = LIVE`.

**Claim 2 = PASS.**

---

## 4. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL (disproven)

Grep of product `*.cs` this slot: **one write** of `RealCopyEnabled =`.

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`.env` L73 (boolean only): `REAL_COPY_EXECUTION_ENABLED=true`.

So boot bind is **true** when that env is loaded (`apps/api/Program.cs` L10 `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()`).

Post-logon host **reads** the bit. It does **not** assign false:

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

Assignments after logon: Quote/Trade `LoggedOn` / `Status` / `LastError` / `UpdatedAt`. Token `_runtime.RealCopyEnabled =` is **absent**. The log line even names the bit `RealCopyArmed`. Comment “NewOrderSingle still unimplemented” is **stale vs** `NewOrderSingleImplemented => DemoDest` and hosted `Build("D")`.

`LiveRuntimeStatus.RealCopyEnabled` is a public `{ get; set; }` (L32). Default `false` is overwritten by DI. Snapshot copyNote (L42–44) still claims “NewOrderSingle still unimplemented” even when the flag is true — documentation, not a pin.

API surface (file-proven shape; **not** live-proven this slot):

- `GET /api/health` returns `realCopyEnabled = runtime.RealCopyEnabled` (`Program.cs` L55).
- `GET /api/settings` returns `featureFlags["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` (`Program.cs` L76).
- `GET /api/copy/status` returns `RealCopyArmed: _runtime.RealCopyEnabled` (`CopyTradingService.cs` L64).

Live GET of those endpoints was **SSRF-blocked**. Cannot quote a process body. File bind + missing re-pin is already enough to **disprove** “forced false after logon.”

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (L35). That POCO is **not** what DI binds. `apps/api/appsettings.json` `FeatureFlags.LiveCopyEnabled=false` is **not** the runtime bit.

`RiskEngine` L90–93 empty block when `RealExecutionEnabled == false` does **not** assign runtime. Unit `Real_flag_false_never_allows_fix_send` only proves Evaluate’s `AllowFixSend` when the request flag is false.

**Claim 3 = FAIL (disproven).** Residual: operator should set `.env` L73 back to `false`. This slot did not edit it.

---

## 5. Claim 4 — sending now cannot be the profit path — FAIL (contradicted / unproven as written)

Split the hops.

### 5.1 Persist / risk hop cannot send (file-proven)

`GenerateShadowIntentsAsync` always passes `Reconciled = VenueReconciled` and `VenueReconciled` is **const false** (L20, L304). `RiskEngine` then:

```84:85:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (!request.Reconciled && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "VENUE_NOT_RECONCILED");
```

OpenExposure is increasing (L174–175). Reject sets `AllowFixSend = false` (L180–188).

Even on Approve, send requires **all** of:

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

Persist then **overwrites** the record:

```317:336:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                var rec = new RiskDecisionRecord
                {
                    // ...
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

The LIVE branch is unreachable (`VenueReconciled` const false; scorer never emits LIVE). Persist hop = **no ticket**.

Inconsistency this slot: `GetStatusAsync` reports `VenueReconciled: DemoDest` (L67) while Evaluate uses const `false`. Dashboard can say reconciled on demo dest while risk always rejects opens as `VENUE_NOT_RECONCILED`. That is honesty debt, not a send.

### 5.2 Hosted demo hopper **can** send (file-proven; contradicts the claim)

`CopyTradingHostedService` every 20s:

```28:30:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

`ExecuteDemoCopyAsync` does **not** consult `AllowFixSend`, `CanPromoteToLive`, `RealCopyEnabled`, or `TraderState.LIVE`. Gate is `DemoDest` + password present. Then it calls `CTraderFixCopyOpen.SendAsync` (close L528, open L566). That assembler writes `35=D`.

`NewOrderSingleImplemented => DemoDest` is **true** on the demo identity. Status string on fill: `DEMO_SENT` (L593).

Assigned UI **advertises** dest auto-send:

```56:58:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      {intents.length === 0 && (
        <p className="text-gray-500 text-sm">No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.</p>
      )}
```

Status summary when `DemoDest`:

```76:78:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
            Summary: DemoDest
                ? "Demo dest auto-copy ON. Eligible demo/contest opens send on the 20s tick; dest closes when the MT5 source closes. Live 1369850 is never used."
                : "Copy pipeline ON. Shadow intents only. Live Pepperstone will not receive NewOrderSingle.");
```

### 5.3 On-disk dest fill is dest exposure, not booked dest profit

`D:\Prop\data\demo_copy_ledger.json` (read this slot, no secrets):

- SourceLogin `305750`
- SourcePositionId `21250421`
- Lots `0.01`
- DestPositionId `237339770`
- DestClOrdId `C20260818093047317`
- DestFillPrice `4390.2`
- DestClosed `false`

`CopyTradingService` L500–511 **seeds that same row** if missing. So the file is both a seed and a ledger. It is still a dest fill record with an open dest id.

Dashboard dest cash is **not** computed from that ledger:

```29:44:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var shadowPnl = await _db.ShadowOrders.SumAsync(s => (decimal?)s.SourceVsShadowSlippage, ct) ?? 0;
        // ...
        return new OverviewDto(
            // ...
            shadowPnl,
            0,
            0,
            0,
```

`OverviewDto` field order (`DashboardModels.cs` L15–16): `ShadowPnl`, then `DestinationRealPnl`. The `0` after `shadowPnl` **is** `DestinationRealPnl`. Constructor constant. **Not** a live dest mark.

### 5.4 Why the claim fails the assigned rule

“Sending now cannot be the profit path” as written requires proving **no** send can become dest P&L.

Cannot prove that:

1. Hosted hop sends `35=D` on demo dest.
2. Live 1369850 refuse is a subset, not the whole claim.
3. `DestinationRealPnl=0` is a constructor, not dest cash = 0.
4. Open dest fill exists on disk.
5. Live GET of dest P&L / `/api/copy/status` **blocked**.

Persist-hop SAFE_BY_ABSENCE is real and is **not** the whole path.

**Claim 4 = FAIL.**

---

## 6. Claim 5 — SHADOW on demo is not destination profit — PASS_PAPER / FAIL unscoped

### 6.1 Paper SHADOW book is not dest cash (PASS)

`GenerateShadowIntentsAsync` writes `Status = "SHADOW_ONLY"` and, only if risk **Approve** and a quote exists, a `ShadowOrder` from `ShadowCopyEngine.SimulateEntry` (CopyTradingService L336–359).

`SimulateEntry` (`ShadowCopyEngine.cs` L35–60) picks dest quote bid/ask, adds modeled latency slippage, returns a `ShadowFill`. No socket. No `35=D`. No dest account.

Overview `ShadowPnl` = `Sum(ShadowOrders.SourceVsShadowSlippage)` (EfDashboardQueries L29). That is a **slippage sum**, not dest realized P&L. `DestinationRealPnl` is a different field and is literal `0`.

Because Evaluate always sees `Reconciled=false` on opens, new paper fills currently also fail `VENUE_NOT_RECONCILED` (decision is not Approve). Intents can still be `SHADOW_ONLY` without a `ShadowOrder` row.

### 6.2 SHADOW is dest AUTO_ADMIT floor (FAIL as dest-class claim)

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` with `TRADER_NOT_SHADOW_YET` (L81–85). It accepts `SHADOW` / `LIVE_CANDIDATE` / `LIVE` if size-pattern, 20-trade, XAU book > 0, and demo/contest group all pass.

`CopyRosterEngine.Decide` AUTO_ADMITs when `IsTraderEligible` (L72–80). `TickRosterAsync` writes `Status = "ADMITTED"` (CopyTradingService L140–156).

`ExecuteDemoCopyAsync` iterates `ADMITTED` roster seats (L542–544) and sends dest `35=D` **without** reading `TraderState.LIVE` or `CanPromoteToLive`. Source group must be demo/contest (`CopyGroupFilter.IsDemoOrContest`).

So: a **SHADOW** demo/contest trader can be dest-admitted and dest-sent. That is not “SHADOW = dest profit booked.” It **is** “SHADOW on demo can be the dest send class.” The assigned sentence does not distinguish those. FAIL-if-unproven on the dest-class reading.

Live GET of dest account P&L was not available. Cannot prove dest cash is zero. Cannot prove dest cash is nonzero.

**Claim 5 = PASS_PAPER (ledger ≠ dest $). FAIL unscoped (SHADOW is dest ADMIT floor; hopper ignores LIVE).**

---

## 7. Live GET

| URL | Result this slot |
|---|---|
| `http://127.0.0.1:5000/api/health` | Worker `SSRF blocked: 127.0.0.1` |
| `http://localhost:5000/api/copy/status` | Page fetch failed (no body) |
| `http://localhost:5000/api/settings` | Not reached |

No live JSON. No process proof of `realCopyEnabled`. File bind of `.env=true` still disproves claim 3. Claims 4–5 dest cash remain unproven from GET.

---

## 8. Risk to capital

| Surface | Measured |
|---|---|
| Live Pepperstone `1369850` | **NONE.** `CTraderFixCopyOpen` / DemoTestTrade / DemoMatrix refuse that account. `CTraderFixSession` sends `35=A` only. Persist `AllowFixSend=false`. `VenueReconciled` const false on Evaluate. `CanPromoteToLive => false`. |
| Demo dest (public id `5328266` / host prefix `demo-` / sender prefix `demo.pepperstone.5328266`) | **Not absent.** Hosted 20s hopper. Ledger dest `237339770` open, `DestClosed=false`, fill `4390.2`. |
| This slot | **0** `35=D` constructed or sent. |
| Paper SHADOW | DB / in-memory slippage. Not dest cash. |

`SAFE_BY_ABSENCE` applies to **live `1369850`**, not to the product as a whole.

---

## 9. Stale slogans this HEAD kills

| Slogan | HEAD |
|---|---|
| Product `35=D=0` | False. 5 `Build("D")` call sites. |
| `NewOrderSingleImplemented=const false` | False. `=> DemoDest`. |
| Logon re-pins `RealCopyEnabled=false` | False. Logon logs only. |
| `REAL_COPY` stays false | False. `.env` L73 `true` + DI L41. |
| Sending cannot be a dest path | False for demo dest hopper. |
| SHADOW cannot reach dest | False as ADMIT floor. True as paper cash column. |

---

## 10. What this slot did not do

- Did not edit product, tests, or `.env`.
- Did not send or assemble `35=D`.
- Did not print secrets.
- Did not treat sibling `P500_VERIFY_*` / `W500_*` integers or verdicts as proof (re-read HEAD).
- Did not live-attach MT5 or FIX TRADE for a new order.
- Will not recommend enabling live `1369850`.

---

**End P500_VERIFY_88.** Slot **88**. Verdict **FAIL** (claim 3 disproved; claims 1/4 unscoped-false; claim 5 paper-only). Risk to live capital **NONE** on `1369850`; demo dest send **wired**. Product source was not modified. No secrets printed.
