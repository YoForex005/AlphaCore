# P500_VERIFY_33 — Adversarial four-file verify (slot 33)

| Field | Value |
|---|---|
| Slot | **33** |
| Date | 2026-08-18 |
| Agent | P500_VERIFY_33 (adversarial verifier). Did **not** trust sibling `P500_*` / `W500_*` integers or verdicts. Re-read product files this slot. |
| Assigned | Read `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`. Confirm: (1) no `35=D` builder; (2) `CanPromoteToLive` is false; (3) `RealCopyEnabled` forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. **FAIL any claim that cannot be proved from a file or live GET.** Never print secrets. |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **None.** No password / proxy / FIX password values. Only the already-on-disk boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850` that appear in product source. |
| Live GET this slot | **Attempted. Blocked.** `web_fetch`/`open_page` to `http://127.0.0.1:5000/api/health` and `http://localhost:5000/api/settings` failed (loopback SSRF / retrieve error). **No live JSON.** File proof only. |
| Live `35=D` sent this slot | **No.** |
| `REAL_COPY` flipped this slot | **No.** |
| Method | Full `read_file` of the four assigned files plus the logon / DI / hopper / dest-PnL hop they actually call. Targeted `grep` for `35=D` / `Build("D")` / `CanPromoteToLive` / `RealCopyEnabled =` / `DestinationRealPnl` / `SHADOW`. `.env` L73 inspected **for that flag key only**. |
| Honesty | A comment is not a runtime pin. A dashboard label is not dest cash. Sibling swarm PnL / census numbers are **not** this-slot evidence. |

---

## 0. Verdict (binding)

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** (scoped to `CTraderFixSession.cs`) | File **135/135**. Only outbound MsgType is `(35, "A")` at L96. Grep of this file for `35=D` / `(35, "D")` / `Build("D")` = **0**. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(TraderState current) => false` at `BaselineScorer.cs` L211. Parameter unused. `FromBaseline` ceiling is `SHADOW`. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | **Disproved.** Logon host **reads** the flag and **never assigns** `false`. Sole `RealCopyEnabled =` write is DI bind of `.env` `REAL_COPY_EXECUTION_ENABLED=true`. |
| 4 | sending now cannot be the profit path | **PASS_NOT_BOOKED_DEST_PROFIT** | Assigned session cannot send. Persist `AllowFixSend=false`. `CanPromoteToLive` hard-false. `DestinationRealPnl` is constructor literal `0`. Residual: sibling `CTraderFixCopyOpen.Build("D")` **is** a demo dest sender on the 20s tick — dest **activity**, not a booked dest-profit constructor. |
| 5 | SHADOW on demo is not destination profit | **PASS** | `SHADOW` is a **source-trader state**. Dashboard `ShadowPnl` is `Sum(SourceVsShadowSlippage)`. `DestinationRealPnl` is constructor `0`. `LiveCopyPage` shows SHADOW **counts**, no dest cash. `ShadowCopyEngine.SimulateEntry` is paper, not a venue fill. |

**Slot verdict: `FAIL`.**

Claim 3 is **disproved**, not merely unproved. Four of five assigned claims hold from files (claim 1 scoped to the assigned session file). One FAIL is enough.

**Risk to capital:** **NONE on live `1369850`** (`SAFE_BY_ABSENCE` for `CTraderFixSession` + `CTraderFixCopyOpen` refuse of that account). **Not absent on demo dest** if `DemoDest` is true: hosted `ExecuteDemoCopyAsync` can emit `35=D` **without** `RiskEngine.Evaluate` and **without** reading `RealCopyEnabled`. This slot sent **0**.

---

## 1. Claim 1 — no `35=D` builder — **PASS** (`CTraderFixSession.cs` only)

Assigned file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135** lines, full read this slot).

The only outbound builder is `BuildLogon`:

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

Call site: `TryLogonAsync` L47 builds that logon, L49 `ssl.WriteAsync` once, L53 one read, L55 extracts inbound `35`. If inbound is `"A"`, `LoggedOn = true` (L56–64). Socket is `using TcpClient` + `await using SslStream` and is disposed after that single exchange.

| Check | Measured this slot |
|---|---|
| Outbound MsgType in this file | **only** `(35, "A")` L96 |
| `WriteAsync` count | **1** (L49, the logon bytes) |
| `35=D` / `(35, "D")` / `Build("D")` / `NewOrderSingle` in this file | **0** (file-scoped `grep`) |
| Inbound `35=A` meaning | logon ack, **not** a fill |
| Password handling | tag 554 is assembled; this report does **not** quote the value |

Adversarial residual (**does not fail the assigned-file claim**): sibling product files **do** have a `35=D` builder.

| File | What this slot measured |
|---|---|
| `src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra)` after TRADE logon + SecurityList. **Refuses** non-`demo-` host, non-`demo.` sender, or account `1369850` (L37–42). |
| `src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | `Build("D", …)` present (matrix helper). |
| `src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | `Build("D", …)` ×3 (demo test helper). |

Claim 1 as written against **`CTraderFixSession.cs`** is true. Tree-wide “product `35=D=0`” is **false** on HEAD.

---

## 2. Claim 2 — `CanPromoteToLive` is false — **PASS**

Assigned file: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (**212** lines, full read this slot).

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

| Check | Measured |
|---|---|
| Ceiling of `FromBaseline` | `SHADOW` (L200–201). No `LIVE` / `LIVE_CANDIDATE` return. |
| `AfterHighEarlyScore` | `=> TraderState.SHADOW` (L209) |
| `CanPromoteToLive` | **hard `false`**. Parameter `current` is unused. |
| Inputs | source reconstructed XAU features only (`NetPnl`, martingale, SL use, …). No dest account. |
| Test | `tests\Unit\BaselineScorerTests.cs` L21–26: three disciplined winners → `SHADOW`; `CanPromoteToLive(...).Should().BeFalse()`. |

There is **no** code path in this file that emits `TraderState.LIVE`. Promotion to live is not a scorer function. `grep CanPromoteToLive` under `*.cs` hits only this method, the unit test, and a tmp eval harness.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL**

This claim is **false on disk**.

### 3.1 Logon host does not re-pin

Assigned-adjacent file (the only post-logon writer of `LiveRuntimeStatus` FIX fields): `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` (full read).

After both `CTraderFixSession.TryLogonAsync` calls it writes Quote/Trade status and **logs** the flag. It never assigns it:

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

`grep` `RealCopyEnabled =` under `*.cs`: **one product hit**.

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

That is a **bind**, not a force-false. `AddTraderIntelligence` then `AddHostedService<CTraderFixLogonHostedService>()` (L58). Logon cannot override the bind because it never writes the property.

### 3.2 Lab env is `true`

`D:\Prop\.env` L73 (flag key only): `REAL_COPY_EXECUTION_ENABLED=true`.

`apps\api\Program.cs` L10 loads that env (`EnvFile.FindAndLoad()`), then L15 `AddTraderIntelligence`. `/api/health` L55 and `/api/settings` L76 echo `runtime.RealCopyEnabled` — they do **not** override it.

`FEATURE_COPY_TRADING_ENABLED=true` exists at `.env` L106 (boolean only; unused by the RealCopy bind).

### 3.3 Options default is unused by logon

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (`CTraderFixOptions.cs` L35). `CTraderFixLogonHostedService` does **not** read that POCO. That default is **not** evidence that API logon re-pins the runtime flag.

### 3.4 What “after logon” actually leaves

| Object | After successful inbound `35=A` |
|---|---|
| `_runtime.Quote.LoggedOn` / `_runtime.Trade.LoggedOn` | set from logon result |
| `_runtime.RealCopyEnabled` | **unchanged** from DI bind of env |
| This slot live GET of `/api/settings` | **not obtained** (loopback blocked) |

Cannot claim “forced false after logon.” The opposite wiring is on disk. Live GET would only have confirmed the echo; the bind + no re-pin already **disproves** the claim.

---

## 4. Claim 4 — sending now cannot be the profit path — **PASS_NOT_BOOKED_DEST_PROFIT**

Scope: **booked destination profit**, not “zero senders exist anywhere.”

Assigned `RiskEngine.cs` (**190** lines, full read). `AllowFixSend` is computed, not dest cash:

```147:171:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
        // ...
                AllowFixSend = allowSend
```

L90–93 (“Shadow path still evaluates risk but never allows FIX send.”) is a **comment no-op**. If a caller passed `RealExecutionEnabled=true`, `Reconciled=true`, `VenueHealthy=true`, `KillSwitch=None`, this class **would** return `AllowFixSend=true`. That is why the hopper persist and the session file matter.

### 4.1 Assigned session cannot send a ticket

`CTraderFixSession` is Logon-only (claim 1). A `35=A` ack is not dest PnL.

### 4.2 Scorer cannot put anyone in LIVE

`CanPromoteToLive => false` (claim 2). `FromBaseline` ceiling is `SHADOW`.

### 4.3 Hopper persist never arms FIX send

`D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` (full read):

- `VenueReconciled` **const `false`** (L20).
- Hopper `Evaluate` passes `Reconciled = VenueReconciled` (L304) → increasing actions reject `VENUE_NOT_RECONCILED` (`RiskEngine.cs` L84–85).
- Persist **hard-codes** `AllowFixSend = false` (L324) regardless of `decision.AllowFixSend`.
- Live-send branch L330 requires `decision.AllowFixSend && score.CurrentState == LIVE && NewOrderSingleImplemented && VenueReconciled`. Const `VenueReconciled=false` → branch **dead**. Even if it ran, it only sets `Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED"` — it does **not** call a sender.

`GetStatusAsync` L67 reports `VenueReconciled: DemoDest` to the UI. That is a **status lie relative to the const** used in `Evaluate`. Adversarial note: the dashboard can say venue reconciled on demo while the hopper still evaluates `Reconciled=false`. That still does not create dest PnL.

### 4.4 Dest profit is not computed

`OverviewDto.DestinationRealPnl` exists (`DashboardModels.cs` L16). `grep DestinationRealPnl` under `*.cs` / `*.tsx` = **that DTO field only**. The **only** product assignment is the literal `0` in `EfDashboardQueries.GetOverviewAsync` L44 (after `shadowPnl`, then `0, 0, 0` for dest / xau gross / xau net).

`GetRiskAsync` L208 returns `RiskDashboardDto(0, 0, 0, 0, 0, …)` — DailyPnl / Drawdown / XAU legs are also constructor zeros.

`LiveCopyPage.tsx` has **no** dest-PnL field, **no** send button, **no** “profit” column. Hooks `useCopyStatus` / `useCopyIntents` are GETs only (`apps\web\src\api\hooks.ts` L60–65). No POST from this page.

Sending **now** through the assigned session / hopper / dashboard is therefore **not a booked dest-profit path**.

### 4.5 Residual the claim must not hide

HEAD **does** send on demo dest. This is dest **activity**, not dest **profit accounting**.

```45:50:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public bool DemoDest =>
        (_config["CTRADER_FIX_HOST"] ?? "").StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
        && (_config["CTRADER_FIX_TRADE_SENDER_COMP_ID"] ?? "").StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
        && _config["CTRADER_FIX_ACCOUNT_ID"] != "1369850";

    public bool NewOrderSingleImplemented => DemoDest;
```

`CopyTradingHostedService` 20s tick (`src\Infrastructure\Hosting\CopyTradingHostedService.cs` L21–41): `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**.

`ExecuteDemoCopyAsync` (L483–605):

- Returns 0 if `!DemoDest`.
- Does **not** read `RealCopyEnabled`.
- Does **not** call `RiskEngine.Evaluate`.
- Calls `CTraderFixCopyOpen.SendAsync` → `Build("D")` (L528, L566).
- Caps `MaxAutoLots = 0.05m` (L22) on **source** tickets, then sends **1:1** those lots.
- Refuses live identity `1369850` inside `CTraderFixCopyOpen` L37–42.

`LiveCopyPage.tsx` L57 empty-state **admits** this: “Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.”

So: **sending now can open a demo dest ticket.** That ticket is **not** written into `DestinationRealPnl` (still `0`). It is **not** live Pepperstone. It is **not** a measured profit path. This slot will **not** claim tree-wide `SAFE_BY_ABSENCE` for demo dest.

This slot did not live-GET dest fills and did not send.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **PASS**

### 5.1 `SHADOW` is a source state, not dest cash

`BaselineScorer` / `TraderStateMachine` assign `TraderState.SHADOW` when `quality >= 70 && risk < 40` (L200–201). Fields used: source XAU features. **No dest account, no dest fill, no dest currency.**

`AfterHighEarlyScore() => SHADOW` (L209). Still a state enum.

`XauUsdOneToOneCopyPolicy.IsTraderEligible` requires SHADOW-or-better (WATCH/EARLY/INSUFFICIENT rejected as `TRADER_NOT_SHADOW_YET`), 20 completed XAU, **source** `XauNetPnl > 0`, and demo/contest group. That source-book filter is **eligibility**, not dest realized profit.

### 5.2 Paper shadow fills are slippage, not dest PnL

Hopper (`CopyTradingService` L336–360): non-LIVE path sets `Status = "SHADOW_ONLY"` and, if a quote row exists **and** `Outcome == Approve`, writes `ShadowOrder` from `_shadow.SimulateEntry(...)`.

`ShadowCopyEngine.SimulateEntry` (`src\Domain\Shadow\ShadowCopyEngine.cs` L35–61) returns a **modeled** price / `SourceVsShadowSlippage`. It does not write FIX. It does not touch `DestinationRealPnl`.

`EfTradingStore.PersistDemoShadowAsync` only persists when `state == SHADOW` and a quote row exists; same `SimulateEntry`; status `"SHADOW_ONLY"`.

Dashboard `ShadowPnl` (`EfDashboardQueries.cs` L29):

```29:29:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var shadowPnl = await _db.ShadowOrders.SumAsync(s => (decimal?)s.SourceVsShadowSlippage, ct) ?? 0;
```

That is **sum of source-vs-shadow slippage**, stuffed into `OverviewDto.ShadowPnl`. Next field `DestinationRealPnl` is the literal `0` (L44). Per-trader `TraderRowDto.ShadowPnl` is hardcoded `0` at L118.

### 5.3 UI does not treat SHADOW as dest profit

`D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (**70** lines, full read):

```12:18:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-sm">
        <Stat label="REAL_COPY armed" value={status?.realCopyArmed ? 'YES' : 'NO'} hot={status?.realCopyArmed} />
        <Stat label="SHADOW traders" value={status?.shadowTraders ?? 0} />
        <Stat label="LIVE traders" value={status?.liveTraders ?? 0} />
        <Stat label="Live sends" value={status?.liveSends ?? 0} />
        <Stat label="Intents" value={status?.intents ?? 0} />
        <Stat label="Shadow fills" value={status?.shadowFills ?? 0} />
```

Counts only. No dest PnL column. Blockers title (L24): “Live send blockers (Pepperstone cannot be filled)”.

`GetStatusAsync`: `ShadowTraders` = count of scores with `CurrentState == SHADOW` (L59). `ShadowFills` = `ShadowOrders.Count` (L56). `LiveSends` = count of `ExecutionIntents` with `SentAt != null` (L57) — **not** dest realized PnL.

### 5.4 Demo dest send is a **different** hop

Roster ADMIT (`CopyRosterEngine.Decide`) uses source eligibility + demo/contest group. `TickRosterAsync` writes `CopyIntent.Status = "ADMITTED"`. `ExecuteDemoCopyAsync` then sends for `Status == "ADMITTED"` open source XAU.

That dest fill — if it happens — is **demo-account activity**, not the SHADOW score and not `ShadowPnl`. Claim 5 holds: **SHADOW on demo is not destination profit.**

---

## 6. Live GET

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | **blocked** (`SSRF blocked: 127.0.0.1`) |
| `http://localhost:5000/api/settings` | **retrieve failed** |
| `http://127.0.0.1:5000/api/copy/status` | **not fetched** (prior loopback blocked) |

Launch profile would have been `http://localhost:5000` (`apps\api\Properties\launchSettings.json` L17) or `http://localhost:18720` (L7). This slot **does not** claim a live `realCopyEnabled` JSON value. File bind + env `true` + no re-pin is sufficient to **FAIL claim 3**.

---

## 7. What this slot refuses to say

- Will not say “EX5 / copy edge / dest profit proven.”
- Will not recycle sibling `−$241,580` / `8463` / dest-ledger fill prices as this-slot measurements (not re-read here).
- Will not say tree-wide `35=D=0` (stale). Assigned session is `35=A` only.
- Will not say `NewOrderSingleImplemented` is const `false` (HEAD is `=> DemoDest`).
- Will not say `RealCopyEnabled` is forced false after logon (claim 3 FAIL).
- Will not say demo dest is `SAFE_BY_ABSENCE`.
- Will not treat `RiskEngine` L90–93 comment as a gate.
- Will not treat `GetStatusAsync` `VenueReconciled: DemoDest` as hopper recon.

---

## 8. Files read (this slot)

| Path | Role |
|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | assigned (1) |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | assigned (2) |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | assigned (4) |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | assigned (4/5) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | post-logon flag |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | sole `RealCopyEnabled =` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | flag + copyNote |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | hopper / demo send |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | sibling `Build("D")` |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 20s tick |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | dest PnL `0`; ShadowPnl = slippage sum |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | `DestinationRealPnl` |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | paper fill |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | SHADOW eligibility / source PnL |
| `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs` | ADMIT vs source book |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | PersistDemoShadow |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | unused POCO default false |
| `D:\Prop\apps\api\Program.cs` | health/settings echo |
| `D:\Prop\apps\web\src\api\hooks.ts` | GET-only copy hooks |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | promote-false test |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | AllowFixSend false when flag false |
| `D:\Prop\.env` L73 | flag boolean only |

---

**End P500_VERIFY_33.** Slot **33**. Verdict **FAIL** (claim 3 disproved). Risk to live capital **NONE**; demo dest send **wired**.
