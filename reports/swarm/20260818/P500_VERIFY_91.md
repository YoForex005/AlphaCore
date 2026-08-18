# P500_VERIFY_91 — Adversarial four-file verify (slot 91)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_91.md` |
| Agent / slot | P500 adversarial verifier **91** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (live `src/`, `apps/`, lab `.env`) |
| Assigned SUT (read in full this slot) | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135), `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212), `D:\Prop\src\Domain\Risk\RiskEngine.cs` (189/189), `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (70/70) |
| Supporting hop files (claims 1/3/4/5) | `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs` (625/625), `CopyTradingHostedService.cs`, `CopyRosterEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyGroupFilter.cs`, `ShadowCopyEngine.cs`, `EfDashboardQueries.cs`, `EfTradingStore.cs`, `DealIngestionService.cs` / `ReconstructionScoringService`, `apps/api/Program.cs`, `EnvFile.cs`, `data/demo_copy_ledger.json`, `DemoCopyLedger.cs`, `apps/web/src/api/hooks.ts` |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Live `35=D` sent this slot | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None.** Quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73), public host prefix `demo-us-eqx-01…`, public ids `5328266` / `1369850` / `305750` / `21250421` / `237339770` / dest fill `4390.2`. No MT5 / FIX / proxy / DB passwords. Tag `554` never dumped. |
| Live GET this pass | `GET http://127.0.0.1:5000/api/health`, `http://localhost:5000/api/health`, `http://localhost:18720/api/health`, `http://127.0.0.1:5000/api/copy/status` **blocked** (loopback SSRF). Runtime flag and dest mark **not** live-proven. |
| Binding rule | **FAIL any assigned claim that cannot be proven from a live file or a live GET.** Sibling swarm books are not evidence. This slot re-read the four SUT files and the hop files. |

**Honesty:** Wanting dest profit is not an edge. `SAFE_BY_ABSENCE` on live `1369850` is **not** “`RealCopyEnabled` stays false.” An armed env bit with no live sender is still an armed bit. Demo dest send is dest **exposure**, not dest-profit accounting. `DestinationRealPnl` constructor `0` is not a mark-to-market of dest `5328266`.

---

## 0. Verdict (binding)

**FAIL.** Claim 2 is file-proven. Claims **1** (as written, unscoped), **3**, **4** (as written, unscoped), and **5** (as a dest-safety / dest-cash-absent statement) do **not** all pass. Five-claim bundle therefore **FAIL**.

| # | Assigned claim | Measured this slot | Class |
|---|---|---|---|
| 1 | no `35=D` builder | **PROVEN** on `CTraderFixSession.cs` 135/135: only outbound tag 35 is `(35, "A")`. **DISPROVEN** product-wide: `Build("D")` ×5, one of them hosted on the 20s tick. | **FAIL** unscoped / **PASS_SESSION** |
| 2 | `CanPromoteToLive` is false | **PROVEN** (`=> false`; unused `current`). `FromBaseline` never returns `LIVE`. | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **DISPROVEN.** Sole assignment is DI bind of `.env` L73 `true`. Logon host **reads** the bit and logs it; never writes `false`. Live GET blocked, so runtime snapshot unproven — file proof already kills the claim. | **FAIL** |
| 4 | sending now cannot be the profit path | Session/persist hop cannot **book** dest profit (`AllowFixSend=false`; dest DTO constructor `0`; LIVE send branch is `LIVE_SEND_BLOCKED_UNIMPLEMENTED`). Hosted demo hopper **can send `35=D` now**. Ledger dest `237339770` still open. Venue dest P&L **unproven** (no live GET). | **FAIL** unscoped / **PASS_NOT_BOOKED_DEST_PROFIT** / **PASS_LIVE_1369850_ABSENCE** |
| 5 | SHADOW on demo is not destination profit | Paper SHADOW = `SimulateEntry` slippage + source `TraderState`. Dest DTO literal `0` is **not** a mark. SHADOW **is** dest AUTO_ADMIT floor (`TRADER_NOT_SHADOW_YET`). Dest cash **unproven**. | **PASS_PAPER** / **FAIL** as dest-class / dest-profit-absent **UNPROVEN** |

One-line:

```text
FAIL slot 91: CTraderFixSession 135/135 is 35=A only (no D builder); product Build("D")×5 hosted via ExecuteDemoCopyAsync; CanPromoteToLive=>false; RealCopyEnabled NOT forced false after logon (.env true + DI bind + logon logs-only); send-now is not booked dest-profit (DTO 0) but demo dest hopper can 35=D now (ledger 305750/237339770 open); SHADOW-on-demo is source/paper not dest PnL and is AUTO_ADMIT floor. Live GET blocked. Risk NONE on live 1369850. This slot sent 0.
```

Do **not** treat this FAIL as a license to send. Do **not** flip leftover `.env=true` into go-live. Operator should restore `REAL_COPY_EXECUTION_ENABLED=false` (this slot did **not** edit it).

---

## 1. Claim 1 — no `35=D` builder — FAIL unscoped / PASS_SESSION

### 1.1 Assigned file `CTraderFixSession.cs` (135/135)

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` read in full this slot. The only outbound builder is `BuildLogon`. Tag 35 is hardcoded `"A"`:

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

Single `WriteAsync` (L49) of that logon. Then one `ReadAsync` (L53). Socket disposed in `using`. Inbound `Extract(reply, "35")` (L55, L67, L122–134) is reply parse, not a builder. Tokens in this file:

- `Build("D")` = **0**
- `(35, "D")` = **0**
- `NewOrderSingle` = **0**
- `WriteAsync` = **1** (logon only)
- outbound `(35, …)` = **1**, value `"A"`

Hosted caller of this class is logon-only (`CTraderFixLogonHostedService` L48–58: QUOTE 5211 + TRADE 5212). **PASS_SESSION.**

### 1.2 Unscoped product claim — DISPROVEN

Same folder, not the assigned class. Product `Build("D")` call sites (workspace `*.cs`, excluding reports):

| File | Line | Hosted? |
|---|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | L95 `Build("D", sender, target, seq, extra.ToArray())` | **Yes** — `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | L139, L163, L197 | No (CLI / tool) |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | L93 | No (CLI / tool) |

Generic builders accept `type` as tag 35 (`CopyOpen` L142–156, `DemoMatrix` L204, `DemoTestTrade` L243). CopyOpen refuse gate (L37–42) blocks live `1369850` / non-`demo-` host / non-`demo.` sender — it does **not** delete the builder.

Hosted hop:

```23:34:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var copy = scope.ServiceProvider.GetRequiredService<CopyTradingService>();
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

`ExecuteDemoCopyAsync` (CopyTradingService L483–605) calls `CTraderFixCopyOpen.SendAsync` for dest close (L528) and dest open (L566). It does **not** call `RiskEngine.Evaluate`. It does **not** read `RealCopyEnabled`. Gate is `DemoDest` only (L485–489).

**Claim 1 as written is unscoped → FAIL.** Session-scoped “this file has no `35=D` builder” → **PASS_SESSION**.

---

## 2. Claim 2 — `CanPromoteToLive` is false — PASS

Assigned file `BaselineScorer.cs` 212/212.

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

Proven facts:

- `CanPromoteToLive` is a constant `false`. Parameter `current` is unused.
- `FromBaseline` terminal states: `INSUFFICIENT_DATA` / `RISK_BLOCKED` / `SHADOW` / `WATCH` / `EARLY_SCORE`. **Never** `LIVE` or `LIVE_CANDIDATE`.
- Production persist: `ReconstructionScoringService` L140 `CurrentState = score.SuggestedState` (SuggestedState comes from `FromBaseline` via `Score` L162).
- Production callers of `CanPromoteToLive`: **0**. Only definition + `tests/Unit/BaselineScorerTests.cs` L26 (`Should().BeFalse()`).
- Feature flags in `apps/api/appsettings.json` L44–48: `LiveCopyEnabled=false`, `AutoPromotionEnabled=false` (not bound to `CanPromoteToLive`; residual, not a counterexample).

**PASS.** This does **not** prove dest send is off (claim 4).

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL (disproven)

Assigned files do not contain `RealCopyEnabled` except LiveCopyPage **display** (`status?.realCopyArmed`). Claim 3 is a runtime-bit claim. Hop files:

### 3.1 Sole assignment is DI bind of env, not a post-logon pin

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

Workspace `RealCopyEnabled =` assignments: **1** (this line). No other writer.

`.env` L73 (boolean only): `REAL_COPY_EXECUTION_ENABLED=true`.

API boot loads that file:

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();
...
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile` candidates include `D:\Prop\.env` (L15). Values are pushed into process env (L38). No clamp to `false`.

`LiveRuntimeStatus.RealCopyEnabled` is a public settable bool (L32). Copy note when true still claims “NewOrderSingle still unimplemented” (L42–44) — a **comment**, not a pin.

### 3.2 Logon host does not write the bit

`CTraderFixLogonHostedService.ExecuteAsync` after QUOTE/TRADE `TryLogonAsync`:

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

Writes: Quote/Trade session fields only. `RealCopyEnabled` is **read** for the log line. There is no `_runtime.RealCopyEnabled = false`. PersistAsync (L91–111) writes `FixSessionState` host/port/status only.

### 3.3 Live GET cannot confirm the running bit

`GET /api/health` and `/api/settings` expose `runtime.RealCopyEnabled` (`Program.cs` L55, L76). This slot’s live GET is **SSRF-blocked**. File proof is enough: the claim “forced false **after logon**” is **false** in source. Whether the current process has the bit true is **unproven** without GET; the claim still **FAIL**s because it asserts a force that does not exist.

**FAIL / DISPROVEN.**

---

## 4. Claim 4 — sending now cannot be the profit path — FAIL unscoped / PASS_NOT_BOOKED_DEST_PROFIT

### 4.1 Assigned files — session + UI cannot book dest profit

**`CTraderFixSession`:** cannot emit `35=D` (claim 1.1). Logon-only.

**`RiskEngine`:** `AllowFixSend` is true only when **all** of these hold (L147–150):

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

`RealExecutionEnabled == false` leaves an empty comment (L90–93) and still evaluates; `allowSend` is then false. Rejects always set `AllowFixSend = false` (L180–188). Unit test `Real_flag_false_never_allows_fix_send` (RiskEngineTests L21–26) matches.

Hop persist **overwrites** the engine bit:

```317:333:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
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
                ...
                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
```

Also `VenueReconciled = const false` (CopyTradingService L20), so hop Evaluate of increasing actions rejects `VENUE_NOT_RECONCILED` (RiskEngine L84–85) before `allowSend`. `NewOrderSingleImplemented => DemoDest` (L50) — **not** const false.

**`LiveCopyPage.tsx`:** GET-only (`useCopyStatus` / `useCopyIntents` → `GET /api/copy/status`, `GET /api/copy/intents`). No POST, no send button, no `35=D`. It **displays** `realCopyArmed` and `liveSends`. Empty-state text **admits** dest send:

```56:57:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      {intents.length === 0 && (
        <p className="text-gray-500 text-sm">No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.</p>
```

Blocker copy says “Live send blockers (Pepperstone cannot be filled)” (L24) — live Pepperstone, not demo dest.

Dashboard dest cash is a constructor, not a mark:

```33:46:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        return new OverviewDto(
            accounts,
            brokers,
            xauTraders,
            three,
            ...
            shadowPnl,
            0,
            0,
            0,
```

`OverviewDto.DestinationRealPnl` is the `0` after `shadowPnl` (`DashboardModels.cs` L16). **Not measured dest cash.**

### 4.2 Hosted send-now **is** a dest path (demo)

`ExecuteDemoCopyAsync` (L483–605):

- Skips only when `!DemoDest` (host not `demo-` **or** trade sender not `demo.` **or** account `1369850`).
- Seeds ledger row source `305750` / pos `21250421` / dest `237339770` / px `4390.2` if missing (L500–512).
- Closes dest via `CTraderFixCopyOpen.SendAsync(..., destPositionId)` (L528–530) → `Build("D")` with tag 721.
- Opens dest for every `ADMITTED` roster seat with open XAU ≤ `MaxAutoLots=0.05` (L542–598) → `Build("D")` without 721.
- Writes `Status = "DEMO_SENT"` and persist dest ids (L586–594).
- **Does not** call `Evaluate`. **Does not** read `RealCopyEnabled`. **Does not** require `TraderState.LIVE`.

On-disk ledger this slot (`D:\Prop\data\demo_copy_ledger.json`):

- SourceLogin `305750`, SourcePositionId `21250421`
- DestPositionId `237339770`, DestFillPrice `4390.2`
- Lots `0.01`, **DestClosed `false`**

`.env` (public dest only): `CTRADER_FIX_HOST=demo-us-eqx-01.p.c-trader.com`, `CTRADER_FIX_ACCOUNT_ID=5328266`, both sender CompIDs start `demo.pepperstone.5328266`. That is `DemoDest == true` if those values are loaded. Live GET blocked → **running** DemoDest unproven; **file** hop is wired.

`GetStatusAsync` summary when DemoDest (L76–78): “Demo dest auto-copy ON. Eligible demo/contest opens send on the 20s tick … Live 1369850 is never used.”

### 4.3 Scoring of claim 4

| Reading | Result |
|---|---|
| Live Pepperstone / `1369850` cannot be the profit path | **PASS_LIVE_1369850_ABSENCE** (`CopyOpen` L37–42 refuse; DemoDest requires `!= "1369850"`) |
| Booked dashboard dest profit cannot be the send path | **PASS_NOT_BOOKED_DEST_PROFIT** (persist `AllowFixSend=false`; dest DTO `0`; LIVE branch unimplemented) |
| “Sending now cannot be **the** profit path” (unscoped) | **FAIL** — hosted demo `35=D` is dest exposure / dest fill path; ledger dest is open; dest P&L unmeasured |

Unscoped assigned wording **FAIL**. Dest cash of `5328266` is **UNPROVEN** (constructor `0` + no GET). Constructor `0` is **not** proof dest is flat.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — PASS_PAPER / FAIL dest-class / dest-cash UNPROVEN

### 5.1 Paper / source SHADOW ≠ dest cash — PROVEN

- `FromBaseline` emits `TraderState.SHADOW` as a **source-trader** state (BaselineScorer L200–201). That is not dest PnL.
- Hop paper fill is `ShadowCopyEngine.SimulateEntry` (L35–61): ask/bid ± 0.05 latency points. Writes `SourceVsShadowSlippage`. No FIX.
- `GenerateShadowIntentsAsync` else-branch (L336–360): `Status = "SHADOW_ONLY"` + `SimulateEntry` when quote exists and Evaluate `Approve`.
- `PersistDemoShadowAsync` (EfTradingStore L312–333): same `SimulateEntry`, status `SHADOW_ONLY`.
- Overview `ShadowPnl` = `Sum(ShadowOrders.SourceVsShadowSlippage)` (EfDashboardQueries L29). That is slippage, not dest cash.
- Trader row `ShadowPnl` constructor `0` (EfDashboardQueries L118).
- Dest DTO `DestinationRealPnl` constructor `0` (L44).

So the **SHADOW ledger / score** is not destination profit. **PASS_PAPER.**

### 5.2 SHADOW-on-demo **is** dest AUTO_ADMIT — PROVEN (dest class, not dest profit)

`IsTraderEligible` rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET` (XauUsdOneToOneCopyPolicy L81–85). Remaining non-blocked states that pass the state gate are `SHADOW`, `LIVE_CANDIDATE`, `LIVE`. Scorer never emits the last two (claim 2). Plus: ≥20 XAU, source XAU net > 0, not martingale/avg/escalation, **and** `CopyGroupFilter.IsDemoOrContest` (L105–109). Filter is **admit-demo**, reject-real (`demo` / `contest` path segments).

`CopyRosterEngine.Decide`: same demo/contest gate (L52–53 `NOT_DEMO_OR_CONTEST_GROUP`); eligible → `AUTO_ADMIT` / `KEEP` (L72–80). `TickRosterAsync` writes `Status = "ADMITTED"` (CopyTradingService L154 / L160).

`ExecuteDemoCopyAsync` sends dest `35=D` for `ADMITTED` seats (L542–569), **ignoring** `CurrentState == LIVE`. SHADOW is the dest-send floor.

### 5.3 “Not destination profit” as dest-cash-absent — UNPROVEN → FAIL that reading

Cannot prove dest `5328266` P&L is zero:

- Dest DTO `0` is a constructor (claim 4.1).
- Ledger dest `237339770` is **open** at fill `4390.2` / 0.01 lot. Unrealized dest P&L not computed anywhere in assigned or hop files.
- Live GET of `/api/overview` blocked.

Assigned wording “SHADOW on demo is not destination profit”:

- Accounting (SHADOW numbers ≠ dest cash): **PASS_PAPER**.
- Causal / safety (being SHADOW on demo cannot produce dest profit): **FAIL** — SHADOW is AUTO_ADMIT + dest hopper. Dest cash **UNPROVEN**.

Adversarial rule: cannot prove dest-profit-absent from a file or live GET → **FAIL** that stronger reading.

---

## 6. Live GET

Attempted this slot:

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | SSRF blocked (private IP) |
| `http://localhost:5000/api/health` | SSRF blocked |
| `http://localhost:18720/api/health` | SSRF blocked |
| `http://127.0.0.1:5000/api/copy/status` | retrieve failed / blocked |

`launchSettings.json` binds API `http://localhost:5000` and IIS Express `18720`. No live body. Claims that need a running snapshot (`realCopyArmed`, dest mark, FIX loggedOn) stay **UNPROVEN**. File-disproven claims still FAIL.

---

## 7. Risk to capital (this slot sent 0)

| Book | Exposure | Proof |
|---|---|---|
| Live Pepperstone `1369850` | **NONE** (`SAFE_BY_ABSENCE`) | `CTraderFixCopyOpen` L37–42 refuse; `DemoDest` requires account `!= "1369850"`; logon default account `5328266`; this slot sent **0** |
| Demo dest `5328266` | **NOT absent** | Hosted `ExecuteDemoCopyAsync` → `Build("D")`; `.env` dest is demo `5328266`; ledger dest `237339770` **open** 0.01 @ 4390.2 |
| Dashboard dest PnL | Unmeasured `$0` constructor | `EfDashboardQueries` L44 |
| Armed bit | `.env` `REAL_COPY_EXECUTION_ENABLED=true` bound at DI | Not forced false after logon; not a live-send license |

Wanting profit ≠ edge. Copy-all of a catalog that includes `RISK_BLOCKED` names is dest ruin **if** sent. Residual dest hop is demo, not live.

---

## 8. What this slot did not do

- Did not edit product, tests, `.env`, or ledger.
- Did not send `35=D`.
- Did not print secrets.
- Did not treat sibling `P500_VERIFY_*` books as evidence (re-read files).
- Did not claim dest `5328266` is flat.
- Did not claim EX5 / 95% / anything outside this SUT.

---

## 9. Operator note (not executed)

Restore `REAL_COPY_EXECUTION_ENABLED=false` if the leftover `true` was not an explicit go-live. Pin `RealCopyEnabled=false` after logon **in code** if that is the intended invariant. Unscoped “no `35=D` builder” is false while `CTraderFixCopyOpen.Build("D")` is hosted. This slot did **not** change those bits.
