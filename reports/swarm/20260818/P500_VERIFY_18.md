# P500_VERIFY_18 — Adversarial verifier (slot 18)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_18.md` |
| Agent | P500_VERIFY_18 (adversarial verifier, slot 18) |
| Slot | **18** |
| Date | 2026-08-18 |
| Assigned | Read `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`. Confirm: (1) no `35=D` builder, (2) `CanPromoteToLive` is false, (3) `RealCopyEnabled` forced false after logon, (4) sending now cannot be the profit path, (5) SHADOW on demo is not destination profit. **FAIL any claim not proven from a file or live GET.** |
| SUT (read in full) | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135), `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212), `D:\Prop\src\Domain\Risk\RiskEngine.cs` (189), `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (70) |
| Adjacent (read to test unscoped claims) | `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `CTraderFixLogonHostedService.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `apps/api/Program.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `ShadowCopyEngine.cs`, `EfDashboardQueries.cs`, `DealIngestionService.cs`, `DemoCopyLedger.cs`, `data/demo_copy_ledger.json` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** (this slot did not invoke send) |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None** (quoted only public dest ids `5328266` / `1369850` and boolean `REAL_COPY_EXECUTION_ENABLED=true`) |
| Live GET this pass | `GET http://127.0.0.1:5000/api/health` and `/api/copy/status` **blocked** (tool SSRF on loopback). Runtime snapshot **unproven**. File proof only. |

Classification: `PASS` / `FAIL` / `UNPROVEN` / `SAFE_BY_ABSENCE`.

---

## 0. Verdict (binding)

**FAIL.** Two of five claims are file-proven. Three fail as stated. A rubber-stamp PASS on the package would be a lie.

| # | Claim | Result | One-line |
|---|---|---|---|
| 1 | no `35=D` builder | **FAIL** (unscoped) | `CTraderFixSession` is `35=A` only. Product has **three** `Build("D")` writers. |
| 2 | `CanPromoteToLive` is false | **PASS** | Unconditional `=> false`. Scorer never emits `LIVE`. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | Logon never writes the flag. DI binds config. On-disk env boolean is **true**. Live GET blocked. |
| 4 | sending now cannot be the profit path | **FAIL** | Hosted demo hop emits dest `35=D` without `Evaluate`. Ledger already has a dest fill. |
| 5 | SHADOW on demo is not destination profit | **PASS** | SHADOW dollars are source reconstructed PnL / paper slippage. `DestinationRealPnl` is a literal `0`. |

**Package cannot PASS.** Claims 1, 3, and 4 are disproved or unprovable as written. This slot did not send. Live Pepperstone `1369850` remains refused. Demo dest is **not** `SAFE_BY_ABSENCE`.

```text
CTraderFixSession:     BuildLogon (35,"A") only. One WriteAsync. Socket disposed.
CTraderFixCopyOpen:    Build("D") L95. Called by ExecuteDemoCopyAsync on 20s tick.
CTraderFixDemoTestTrade: Build("D") L139 / L163 / L197.
CTraderFixDemoMatrix:  Build("D") L93.
CanPromoteToLive:      always false. FromBaseline never returns LIVE / LIVE_CANDIDATE.
RealCopyEnabled:       set once in DI from REAL_COPY_EXECUTION_ENABLED. Logon host logs it. No force-false.
Risk-gated send:       VenueReconciled=const false; persist AllowFixSend=false L324; LIVE branch dead.
Demo dest send:        bypasses Evaluate / RealCopyEnabled / AllowFixSend. 1369850 refused.
SHADOW:                source state + ShadowOrders.SourceVsShadowSlippage. Not dest realized.
```

---

## 1. Method (what was actually read)

Full `read_file` of the four assigned files. Then, because claim (1) is unscoped and claim (3) is not in `CTraderFixSession.cs`, adjacent send / logon / DI / copy hop files were read in full or to the cited lines. `grep` for `Build("D")`, `RealCopyEnabled`, `CanPromoteToLive`, `35=D`, `TraderState.LIVE`, `DestinationRealPnl`. Live GET attempted and **blocked**. No `.env` dump. No Manager attach. No send.

Prior same-day books (`P500_BOOK_182`, `P503_V_18`) are **not** evidence. Only files and a live GET count. Those books were used only as format templates.

---

## 2. Claim 1 — no `35=D` builder — **FAIL**

### 2.1 What `CTraderFixSession` actually builds

Outbound MsgType is only Logon `A`. There is one write, then the socket is disposed. Reply `35` is **parsed**, never sent as `D`.

```89:108:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
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

Scoped to this file: **PASS** — no `35=D` builder.

### 2.2 The unscoped claim is false

`grep Build("D")` under `D:\Prop\src` hits **five** call sites in **three** product builders:

| File | Lines | Role |
|---|---|---|
| `src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | 95 | Hosted dest open/close. `ExecuteDemoCopyAsync` calls `SendAsync`. |
| `src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | 139, 163, 197 | Manual demo test / flatten / close. |
| `src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | 93 | Demo matrix sender. |

Copy-open writer (same folder as the assigned session file):

```83:95:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
            var closing = !string.IsNullOrWhiteSpace(destPositionId);
            var cl = (closing ? "X" : "C") + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            var now = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var side = closing ? (isLong ? "2" : "1") : (isLong ? "1" : "2");
            var extra = new List<(int, string)>
            {
                (11, cl), (55, gold), (54, side), (60, now), (40, "1"),
                (38, units.ToString("0.##", CultureInfo.InvariantCulture)),
                (494, (closing ? "close-" : "copy-") + sourceLogin + "-" + sourcePositionId)
            };
            if (closing)
                extra.Add((721, destPositionId!));
            await Write(ssl, Build("D", sender, target, seq, extra.ToArray()), timeout.Token);
```

The generic assembler takes `type` and writes tag 35:

```142:148:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
    private static string Build(string type, string sender, string target, int seq, params (int, string)[] extra)
    {
        var fields = new List<(int, string)>
        {
            (35, type), (34, seq.ToString(CultureInfo.InvariantCulture)),
            (49, sender), (56, target), (50, "TRADE"), (57, "TRADE"),
            (52, DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture))
        };
```

**FAIL.** “No `35=D` builder” is not true of the product. It is only true of `CTraderFixSession`. An adversarial verifier does not silently narrow the claim.

Live `1369850` is refused inside `CTraderFixCopyOpen.SendAsync` (host must start `demo-`, sender must start `demo.`, account must not be `1369850`). That is a dest gate, not the absence of a builder.

---

## 3. Claim 2 — `CanPromoteToLive` is false — **PASS**

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

The parameter is unused. There is no branch that returns `true`. Unit pin: `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` asserts `CanPromoteToLive(...).Should().BeFalse()`.

`FromBaseline` never returns `LIVE` or `LIVE_CANDIDATE`. Highest non-blocked state is `SHADOW`:

```189:206:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
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
```

Persisted state is that suggested state, not a later promotion:

```127:140:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore
        {
            ...
            CurrentState = score.SuggestedState,
```

`grep` of product `src/**/*.cs` found **no** `CurrentState = TraderState.LIVE` writer. Copy hopper treats `LIVE` as copyable, but the scorer cannot mint that state.

**PASS** from file.

---

## 4. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL**

### 4.1 Assigned session file does not touch the flag

`CTraderFixSession.cs` has no `RealCopyEnabled` symbol. Logon success returns `LoggedOn = true` and disposes the socket. It cannot force a runtime flag it never sees.

### 4.2 Logon hosted service does not write the flag

After both QUOTE (5211) and TRADE (5212) one-shot logons, the host copies session rows onto `LiveRuntimeStatus` and **logs** the current armed bit. It does not assign `RealCopyEnabled`.

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

The log string “NewOrderSingle still unimplemented” is a comment. It is not an assignment.

### 4.3 The only product write is DI, from config

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

`LiveRuntimeStatus.RealCopyEnabled` is a public `{ get; set; }` with **no** other product assignment (`grep RealCopyEnabled` = this write + reads). `apps/api/Program.cs` loads `D:\Prop\.env` via `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()`. On-disk `D:\Prop\.env` contains `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret echoed). If the API process loaded that env, DI sets the flag **true** at construction and logon leaves it true.

`appsettings.json` `FeatureFlags:LiveCopyEnabled=false` is a **different** key. It is not what DI reads.

### 4.4 Live GET unproven

`GET http://127.0.0.1:5000/api/health` and `/api/copy/status` were attempted and **blocked** (loopback SSRF). This slot cannot quote a live `realCopyEnabled` JSON field. That does **not** rescue the claim: “forced false after logon” requires a post-logon write to `false`. That write does not exist.

**FAIL.** Opposite of forced-false: env boolean can arm the flag, and logon will not clear it.

---

## 5. Claim 4 — sending now cannot be the profit path — **FAIL**

Interpretations checked:

| Reading | Proven? |
|---|---|
| `LiveCopyPage` cannot send a ticket | **Yes** — display-only. No POST, no button, no FIX client. |
| Risk-gated LIVE send cannot fire | **Yes** — `VenueReconciled = const false`; persist overwrites `AllowFixSend = false`; LIVE branch also requires that const. |
| No dest send path exists | **No** — hosted `ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.Build("D")`. |
| Dest send cannot produce dest P&L | **No** — ledger already records a dest fill. |
| Live Pepperstone cannot be the dest | **Yes** — account `1369850` refused. |

The claim as written is the third/fourth reading. Those fail.

### 5.1 UI is not a sender (necessary, not sufficient)

```1:28:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
import { useCopyIntents, useCopyStatus } from '../api/hooks';

export default function LiveCopyPage() {
  const { data: status, isLoading } = useCopyStatus();
  const { data: intents = [] } = useCopyIntents();
  ...
        <Stat label="REAL_COPY armed" value={status?.realCopyArmed ? 'YES' : 'NO'} hot={status?.realCopyArmed} />
        ...
        <Stat label="Live sends" value={status?.liveSends ?? 0} />
        ...
            <div className="font-medium mb-1">Live send blockers (Pepperstone cannot be filled)</div>
```

Empty-state copy **advertises** dest send:

```56:58:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      {intents.length === 0 && (
        <p className="text-gray-500 text-sm">No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.</p>
      )}
```

Hooks are GET-only (`/api/copy/status`, `/api/copy/intents`). The page cannot be the profit path. The hosted worker can.

### 5.2 Risk engine does not allow FIX send when real execution is off — and the persist layer nails it shut

```147:170:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
        ...
                AllowFixSend = allowSend
```

```90:93:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.RealExecutionEnabled == false && request.Action != CopyIntentAction.CloseExposure)
        {
            // Shadow path still evaluates risk but never allows FIX send.
        }
```

That comment is not a `return`. Closes can still `Approve` with `AllowFixSend` following the same `allowSend` formula.

Hopper forces the reject of new exposure regardless, because `VenueReconciled` is a **const false** passed as `Reconciled`:

```20:20:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
```

```84:85:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (!request.Reconciled && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "VENUE_NOT_RECONCILED");
```

And the persist record **throws away** whatever `Evaluate` returned:

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

The LIVE send branch is dead twice: `CanPromoteToLive=false` so `CurrentState` is never `LIVE`, and `VenueReconciled` is const false. **This** path cannot be dest profit.

### 5.3 The actual send path ignores that gate

Hosted tick, every 20 s after 8 s:

```28:30:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

`ExecuteDemoCopyAsync` does **not** read `RealCopyEnabled`, `AllowFixSend`, `Evaluate`, bid/ask, or quote age. It requires `DemoDest` (demo host + demo sender + account ≠ `1369850`), a password, `ADMITTED` roster seats, and `MaxAutoLots=0.05`. Then it calls `CTraderFixCopyOpen.SendAsync`, which builds `35=D`.

```566:569:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                var fill = await CTraderFixCopyOpen.SendAsync(
                    host, sender, target, account, password,
                    seat.SourceLogin.ToString(), trade.PositionId.ToString(),
                    trade.Direction == TradeDirection.Long, trade.MaxVolumeLots, ct);
```

On-disk dest ledger `D:\Prop\data\demo_copy_ledger.json` already holds an **open dest fill**:

- source `305750` / `21250421`
- dest position `237339770`
- dest clOrd `C20260818093047317`
- dest fill `4390.2`
- `0.01` lot
- `DestClosed: false`

That is dest inventory. Sending **is** a dest P&L path on demo FIX. Whether that path has a measured edge is a different question (this slot did not remasure dest EV). The claim “cannot be the profit path” is **false**.

`GetStatusAsync` summary when `DemoDest` is true: *“Demo dest auto-copy ON. Eligible demo/contest opens send on the 20s tick”*. `NewOrderSingleImplemented => DemoDest` (not const false).

**FAIL.** Risk-gated LIVE send is dead. Hosted demo send is live code and has already filled.

---

## 6. Claim 5 — SHADOW on demo is not destination profit — **PASS**

### 6.1 SHADOW is a source-trader state from reconstructed MT5 PnL

`FeatureSnapshot.NetPnl` is `Sum(t.NetRealizedPnl)` over completed **source** XAU trades. `FromBaseline` assigns `SHADOW` when `quality >= 70 && risk < 40` after three such trades. That quality uses source net / profit-factor, not dest fills.

Policy eligibility (which admits SHADOW+ books onto the dest roster) also uses **source** `XauNetPnl` and **requires** demo/contest groups:

```99:109:D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs
        if (trader.XauNetPnl <= 0)
        {
            reason = "XAU_BOOK_NOT_PROFITABLE";
            return false;
        }

        if (!CopyGroupFilter.IsDemoOrContest(trader.GroupName))
        {
            reason = "NOT_DEMO_OR_CONTEST_GROUP";
            return false;
        }
```

Source challenge/demo dollars are not Pepperstone dest realized.

### 6.2 Dashboard “Shadow P&L” is paper slippage, not dest

```29:46:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var shadowPnl = await _db.ShadowOrders.SumAsync(s => (decimal?)s.SourceVsShadowSlippage, ct) ?? 0;
        ...
            shadowPnl,
            0,
            0,
            0,
            ...
```

`OverviewDto` field order: `ShadowPnl`, then `DestinationRealPnl`, `XauGross`, `XauNet`. The last three are **literals `0`**. `DestinationRealPnl` has **zero** other writers in `src/`. Dest ledger fills are not summed into the DTO.

`ShadowCopyEngine.SimulateEntry` writes `SourceVsShadowSlippage = modeled quote vs source price`. That is a paper mark. Hopper only calls it after `Evaluate` and only when a `DestinationQuote` row exists; hosted OPEN still dies on `VENUE_NOT_RECONCILED` before that fill in the usual hop (`Reconciled=false`). Paper shadow rows are not dest tickets.

### 6.3 Caveat (does not flip the claim)

SHADOW **eligibility** can put a demo login on `ADMITTED`, after which `ExecuteDemoCopyAsync` may send dest `35=D`. That dest fill is dest P&L. The **SHADOW number** (state / source net / dashboard shadowPnl) is still not destination profit.

`CanPromoteToLive=false` also blocks treating SHADOW as LIVE dest.

**PASS** as an accounting claim.

---

## 7. `LiveCopyPage` honesty (assigned file, not a fifth-plus claim)

- Paints `realCopyArmed` from GET. If runtime is true (env boolean), the page will show **YES** in amber. That is not a send.
- Blocker banner says “Pepperstone cannot be filled”. True for `1369850`. Misleading if the operator reads it as “no dest can be filled”: demo dest can.
- Empty-state sentence about demo dest auto-send is **true** of HEAD (`ExecuteDemoCopyAsync`).
- `liveSends` counts `ExecutionIntents` with `SentAt != null`. Demo hop persists dest ids onto `CopyIntents` (`DEMO_SENT`) and the JSON ledger, **not** `ExecutionIntents`. The “Live sends” tile can stay `0` while dest `35=D` fills exist. Unproven live (GET blocked). File-proven mismatch.

---

## 8. What this slot will not claim

- Live process `realCopyEnabled` JSON — GET blocked.
- Dest expectancy after costs — not remasured here.
- Copy-all 8463 / `RISK_BLOCKED` −$241,580 / scored XAU −$154,425 — same-day pins in other books, **not** re-extracted from a live GET this slot. Not used as proof.
- “EX5 fully decompiled” / “≥95%” — out of scope.
- Any password, host secret, or SenderCompID secret.

---

## 9. Binding pin

| Item | Value |
|---|---|
| Slot | 18 |
| Verdict | **FAIL** |
| Claims PASS | 2 (`CanPromoteToLive=false`), 5 (SHADOW ≠ dest profit) |
| Claims FAIL | 1 (product has `Build("D")` ×3 files), 3 (no post-logon force-false; env boolean true), 4 (demo dest send is a dest P&L path) |
| Live GET | **blocked** — runtime snapshot unproven |
| This slot sent `35=D` | **No** |
| Live dest `1369850` | **SAFE_BY_ABSENCE** (refused) |
| Demo dest | **EXPOSED** if `DemoDest` and hosted tick runs; ledger fill `305750`/`21250421` dest `237339770` still open on disk |

**One-line:** session is `35=A` only and promotion is hard-false, but the product already builds `35=D`, does not force `RealCopyEnabled` off after logon, and the 20 s demo hop can (and has) filled dest. SHADOW dollars are still not dest profit.
