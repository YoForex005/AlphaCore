# P500_VERIFY_94 — Adversarial verifier (slot 94)

| Field | Value |
|---|---|
| Slot | **94** |
| Date | 2026-08-18 |
| Agent | P500_VERIFY_94 (adversarial; did **not** trust sibling `P500_*` / `W500_*` numbers) |
| Assigned | Read `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`. Confirm: (1) no `35=D` builder; (2) `CanPromoteToLive` is false; (3) `RealCopyEnabled` forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. **FAIL any claim that cannot be proved from a file or live GET.** |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **None.** Password / proxy / FIX password values were not quoted. Only the already-on-disk booleans `REAL_COPY_EXECUTION_ENABLED=true` and `FEATURE_COPY_TRADING_ENABLED=true` are named. Public dest ids `5328266` / `1369850` appear in product source. |
| Live GET this slot | **Attempted and blocked.** `web_fetch` to `http://127.0.0.1:5000/api/health`, `/api/settings`, `/api/copy/status`, `/api/overview` failed (loopback SSRF). `open_page` to `/api/health`, `/api/copy/status`, and `:18720/api/health` failed (retrieve error). **No live JSON.** File proof only. |
| Live `35=D` sent this slot | **No.** |
| `REAL_COPY` flipped this slot | **No.** |
| Method | Full `read_file` of the four assigned files plus the logon/DI/copy hop they actually call. Targeted `grep` for `35=D` / `Build("D")` / `CanPromoteToLive` / `RealCopyEnabled` / `DestinationRealPnl` / `SHADOW`. `.env` L73/L106 inspected **for those flag keys only**. On-disk `data\demo_copy_ledger.json` re-read this slot (public dest fill ids only). |
| Honesty rule | Sibling swarm PnL / census integers are **not** evidence. A comment is **not** a runtime pin. A dashboard label is **not** dest cash. A `35=A` logon ack is **not** a fill. Dest DTO `0` is a constructor, not a mark. |

---

## 0. Verdict (binding)

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **FAIL** unscoped / **PASS_SESSION** | Assigned `CTraderFixSession.cs` **135/135** outbound MsgType is only `(35, "A")` L96. Product hosts `Build("D")` ×5 (`CopyOpen` L95 + `DemoTestTrade` L139/L163/L197 + `DemoMatrix` L93). `CopyTradingHostedService` 20s tick calls `ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.SendAsync`. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(TraderState current) => false` at `BaselineScorer.cs` L211. Argument unused. `FromBaseline` ceiling is `SHADOW`. Unit test asserts SHADOW ≠ LIVE. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | **Disproved.** Logon host **reads** the flag and **never assigns** `false`. Sole product `RealCopyEnabled =` write is DI bind of `.env` `REAL_COPY_EXECUTION_ENABLED=true`. Live GET of the process bit was **blocked**. |
| 4 | sending now cannot be the profit path | **FAIL** unscoped / **PASS_NOT_BOOKED_DEST_PROFIT** | Assigned session cannot send. Persist `AllowFixSend=false`. `DestinationRealPnl` is a literal `0`. Residual: hosted `ExecuteDemoCopyAsync` **does** emit demo dest `35=D` without `Evaluate` / without reading `RealCopyEnabled`. Ledger dest pos `237339770` is **open**. Dest DTO `0` is not a mark. |
| 5 | SHADOW on demo is not destination profit | **PASS** | `SHADOW` is a **source-trader state**. Dashboard `ShadowPnl` is `Sum(SourceVsShadowSlippage)`. `DestinationRealPnl` is constructor `0`. `LiveCopyPage` shows SHADOW **counts**, no dest cash. Paper `ShadowCopyEngine.SimulateEntry` is not a venue fill. Residual: SHADOW is dest `AUTO_ADMIT` floor. |

**Slot verdict: `FAIL`.**

Claim 3 is **disproved**, not merely unproved. Claims 1 and 4 fail as written (unscoped). Claim 2 and claim 5 hold from files. One FAIL is enough.

**Risk to capital:** **NONE on live `1369850`** (`SAFE_BY_ABSENCE` for `CTraderFixSession` + `CTraderFixCopyOpen` refuse at L37–42). **Not absent on demo dest** if `DemoDest` is true: hosted `ExecuteDemoCopyAsync` can emit `35=D` **without** `RiskEngine.Evaluate` and **without** reading `RealCopyEnabled`. On-disk ledger this slot: source `305750` / dest pos `237339770` / `DestFillPrice=4390.2` / `DestClosed=false`. This slot sent **0**.

---

## 1. Claim 1 — no `35=D` builder — **FAIL** (unscoped) / **PASS_SESSION**

Assigned file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135** lines, full read).

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

| Check | Measured |
|---|---|
| Outbound MsgType in this file | **only** `(35, "A")` L96 |
| `WriteAsync` count | **1** (L49, the logon bytes) |
| `35=D` / `(35, "D")` / `Build("D")` / `NewOrderSingle` in this file | **0** |
| Socket lifetime | `using TcpClient` + `await using SslStream`; disposed after one read |
| Inbound `35=A` treated as | `LoggedOn = true` (L56–64) — **logon ack, not a fill** |
| Other outbound types | **none**. `Assemble` only concatenates the logon field list. |

Adversarial residual (**fails the unscoped claim**): sibling product files **do** have a `35=D` builder, and one of them is **on the hosted 20s tick**.

| File | What this slot measured |
|---|---|
| `src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra)` after TRADE logon + SecurityList. **Refuses** non-`demo-` host, non-`demo.` sender, or account `1369850` (L37–42). Hosted by `CopyTradingService.ExecuteDemoCopyAsync`. |
| `src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` L93 | `Build("D", …)` present (matrix helper). Same demo-identity refuse at L22–26. |
| `src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | `Build("D")` ×3. Demo-gated refuse at L43–47 (`live-*` / `live.` / `1369850`). Tools/CLI hop, not DI. |

`CopyTradingHostedService.cs` L21–41: after 8s delay, every 20s: `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**. That is a product hop, not a comment.

Claim 1 as written against **`CTraderFixSession.cs`** is true. Tree-wide “no `35=D` builder” is **false** on HEAD. The assigned wording does not restrict scope to the session file. Unscoped reading **FAIL**.

---

## 2. Claim 2 — `CanPromoteToLive` is false — **PASS**

Assigned file: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (**212** lines, full read).

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
| Test | `tests\Unit\BaselineScorerTests.cs` L21–26: three disciplined winners → `SHADOW`; `CanPromoteToLive(...).Should().BeFalse()`. |
| `LIVE` enum exists | `TraderState.cs` L10 (`LIVE = 5`) — unused by this machine. |

There is **no** code path in this file that emits `TraderState.LIVE`. Promotion to live is not a scorer function. Quality can be high while dest cash is never an input. That is source scoring, not dest profit.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL**

This claim is **false on disk**. A comment or older report (“hosted service sets `_runtime.RealCopyEnabled = false`”) is **STALE**.

### 3.1 Logon host does not re-pin

Assigned-adjacent file (the only post-logon writer of `LiveRuntimeStatus` FIX fields): `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` (**112** lines, full read).

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

`grep` for `RealCopyEnabled =` under product `*.cs`: **one hit**.

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

That is a **bind**, not a force-false. `LiveRuntimeStatus.RealCopyEnabled` is a public `{ get; set; }` (`LiveRuntimeStatus.cs` L32). Nothing after logon writes `false`.

### 3.2 Lab env is `true`

`D:\Prop\.env` L73 (flag key only): `REAL_COPY_EXECUTION_ENABLED=true`.

`apps\api\Program.cs` L10 loads that env (`EnvFile.FindAndLoad()`), L13 `AddEnvironmentVariables()`, then L15 `AddTraderIntelligence`. `/api/health` L55 and `/api/settings` L76 echo `runtime.RealCopyEnabled` — they do **not** override it.

`EnvFile.Load` (`src\Mt5\Env\EnvFile.cs` L23–39) sets process env from `KEY=VALUE` lines. Combined with DI L41, a host that loaded this `.env` starts with `RealCopyEnabled=true` and **keeps** that bit through FIX logon.

`D:\Prop\.env` L106 (flag key only): `FEATURE_COPY_TRADING_ENABLED=true`. API `/api/settings` L77 hard-codes `FEATURE_COPY_TRADING_ENABLED = true` regardless of env.

### 3.3 Options default is unused by logon

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (`CTraderFixOptions.cs` L35). `CTraderFixLogonHostedService` does **not** read that POCO. `apps\fix-worker\Worker.cs` L22 logs `REAL_COPY_EXECUTION_ENABLED` and says NewOrderSingle is still off — that is the **fix-worker**, not a post-logon re-pin on the API host.

### 3.4 What “after logon” actually leaves

| Object | After successful `35=A` |
|---|---|
| `_runtime.Quote.LoggedOn` / `_runtime.Trade.LoggedOn` | set from logon result |
| `_runtime.RealCopyEnabled` | **unchanged** from DI bind of env |
| This slot live GET of `/api/settings` | **not obtained** (loopback blocked) |

Cannot claim “forced false after logon.” The opposite wiring is on disk. Claim 3 **FAIL**.

---

## 4. Claim 4 — sending now cannot be the profit path — **FAIL** (unscoped) / **PASS_NOT_BOOKED_DEST_PROFIT**

Scope split is mandatory. **Booked destination profit** through the assigned session / persist hopper / dashboard is not a path. Unscoped “sending now cannot be the profit path” is **false** because a hosted demo dest sender exists and a dest fill is already on disk.

### 4.1 Assigned session cannot send a ticket

`CTraderFixSession` is Logon-only (claim 1). A `35=A` ack is not dest PnL.

### 4.2 Scorer cannot put anyone in LIVE

`CanPromoteToLive => false` (claim 2). `FromBaseline` ceiling is `SHADOW`.

### 4.3 Risk persist never arms FIX send

Assigned `RiskEngine.cs` (**189** lines, full read) + hopper `CopyTradingService.cs` (**625** lines, full read).

`D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`:

- `VenueReconciled` **const `false`** (L20).
- Hopper `Evaluate` passes `Reconciled = VenueReconciled` (L304) → increasing actions reject `VENUE_NOT_RECONCILED` (`RiskEngine.cs` L84–85).
- Persist **hard-codes** `AllowFixSend = false` (L324) regardless of `decision.AllowFixSend`.
- Live-send branch L330 requires `decision.AllowFixSend && score.CurrentState == LIVE && NewOrderSingleImplemented && VenueReconciled`. `VenueReconciled` is const false → branch **dead**.

`RiskEngine` allow-send formula (L147–150):

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

L90–93 (“Shadow path still evaluates risk but never allows FIX send”) is a **comment no-op**. When `RealExecutionEnabled == false` the engine does **not** reject; it falls through. The real gate is `allowSend` plus persist-false.

`RiskEngineTests.cs` L21–26: `RealExecutionEnabled=false` → `Approve` + `AllowFixSend=false`. L45–47: `Reconciled=false` → `VENUE_NOT_RECONCILED`.

`MaxSlippage` is on `RiskLimits` L18 and is **unread** by `Evaluate`. That is a residual, not a send path.

### 4.4 Dest profit is not computed

`OverviewDto.DestinationRealPnl` exists (`DashboardModels.cs` L16). The **only** product assignment is the literal `0` in `EfDashboardQueries.GetOverviewAsync` L44 (args after `shadowPnl`: `0, 0, 0` = dest / XAU gross / XAU net).

`GetRiskAsync` L208 also constructs `DailyPnl`/`Drawdown`/`Xau*` as **literal zeros**. That is not a mark of dest cash.

Assigned `LiveCopyPage.tsx` (**70** lines, full read) has **no** dest-PnL field, **no** send button, **no** “profit” column. It shows counts + blockers titled “Pepperstone cannot be filled” (L24). Hooks `useCopyStatus` / `useCopyIntents` are GETs (`hooks.ts` L60–65). No POST.

Sending **now** through the assigned session / persist hopper / dashboard is therefore **not a booked dest-profit path**.

### 4.5 Residual that **fails** the unscoped claim

HEAD **does** send on demo dest. That is dest **activity**. Dest **profit accounting** is still constructor `0`. The claim as written is “sending now cannot be the profit path,” not “dest PnL is unbooked.” A send that can open a dest ticket **can** be a profit path at the venue even if the DTO is a lie.

```45:50:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public bool DemoDest =>
        (_config["CTRADER_FIX_HOST"] ?? "").StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
        && (_config["CTRADER_FIX_TRADE_SENDER_COMP_ID"] ?? "").StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
        && _config["CTRADER_FIX_ACCOUNT_ID"] != "1369850";

    public bool NewOrderSingleImplemented => DemoDest;
```

`CopyTradingHostedService` 20s tick (`CopyTradingHostedService.cs` L21–41): `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**.

`ExecuteDemoCopyAsync` (L483–605):

- Returns 0 if `!DemoDest`.
- Does **not** read `RealCopyEnabled`.
- Does **not** call `RiskEngine.Evaluate`.
- Calls `CTraderFixCopyOpen.SendAsync` → `Build("D")`.
- Caps `MaxAutoLots = 0.05m` (L22) on **source** tickets, then sends **1:1** those lots.
- Refuses live identity `1369850` inside `CTraderFixCopyOpen` L37–42.
- Seeds ledger row `305750`/`21250421` if missing (L500–512) so a close hop can run.

`GetStatusAsync` reports `VenueReconciled: DemoDest` (L67) while the persist hopper uses const `VenueReconciled=false`. UI can say venue-reconciled on demo while the paper hop still rejects. That is a **display split**, not dest profit.

`LiveCopyPage.tsx` L57 empty-state **admits** dest send: “Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.”

On-disk `D:\Prop\data\demo_copy_ledger.json` (this slot): one open dest fill, source login `305750`, dest pos `237339770`, `DestFillPrice=4390.2`, `DestClosed=false`. That is a **fill price**, not dest PnL. I did not compute dest P&L from it. I also did not obtain a live GET that would confirm the ticket is still open at the venue.

So: **sending now can open a demo dest ticket.** That ticket is **not** written into `DestinationRealPnl` (still `0`). It is **not** live Pepperstone `1369850`. It is **not** a measured booked profit. It **is** a send path that can produce dest cash at the demo venue. Unscoped claim 4 **FAIL**. I will **not** claim tree-wide `SAFE_BY_ABSENCE` for demo dest.

This slot did not live-GET dest fills and did not send.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **PASS**

### 5.1 `SHADOW` is a source state, not dest cash

`BaselineScorer` / `TraderStateMachine` assign `TraderState.SHADOW` when `quality >= 70 && risk < 40` (L200–201). Fields used: source XAU features (`NetPnl`, martingale, SL use, …). **No dest account, no dest fill, no dest currency.**

`AfterHighEarlyScore() => SHADOW` (L209). Still a state enum (`TraderState.SHADOW = 3`).

Quality formula (L152–160) can score well on behavior even when dest cash is zero — dest is never an input.

### 5.2 Paper shadow fills are slippage, not dest PnL

Hopper (`CopyTradingService` L336–360): non-LIVE path sets `Status = "SHADOW_ONLY"` and, if a quote row exists **and** `Outcome == Approve`, writes `ShadowOrder` from `_shadow.SimulateEntry(...)`.

`ShadowCopyEngine.SimulateEntry` (`ShadowCopyEngine.cs` L35–61) returns a **modeled** price/slippage. It does not write FIX. It does not touch `DestinationRealPnl`.

Dashboard `ShadowPnl` (`EfDashboardQueries.cs` L29):

```29:29:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var shadowPnl = await _db.ShadowOrders.SumAsync(s => (decimal?)s.SourceVsShadowSlippage, ct) ?? 0;
```

That is **sum of source-vs-shadow slippage**, then stuffed into `OverviewDto.ShadowPnl`. Next field `DestinationRealPnl` is the literal `0` (L44).

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

Counts only. No dest PnL. `useCopyStatus` / `useCopyIntents` are GETs. No POST.

`GetStatusAsync` `ShadowTraders` = count of scores with `CurrentState == SHADOW` (L59). `ShadowFills` = `ShadowOrders.Count` (L56). Neither is dest realized PnL.

`LiveSends` = count of `ExecutionIntents` with `SentAt != null` (L57). That is **not** dest realized PnL either.

### 5.4 Demo dest send is a **different** hop (residual, does not fail claim 5)

`XauUsdOneToOneCopyPolicy.IsTraderEligible` (`XauUsdOneToOneCopyPolicy.cs` L73–112):

- Rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET`.
- Requires ≥ `MinCompletedXauTrades`, `XauNetPnl > 0`, demo/contest group (`CopyGroupFilter.IsDemoOrContest`).
- Accepts `SHADOW` / `LIVE_CANDIDATE` / `LIVE` (and any other non-blocked state).

`CopyRosterEngine.Decide` L72–80: if eligible → `AUTO_ADMIT`. `ExecuteDemoCopyAsync` then sends for `Status == "ADMITTED"` opens.

That dest fill — if it happens — is **demo-account activity**, not the SHADOW score and not `ShadowPnl`. SHADOW can be the **admit floor**, not dest profit. Claim 5 holds: **SHADOW on demo is not destination profit.**

`CopyGroupFilter` **requires** demo/contest (`NOT_DEMO_OR_CONTEST_GROUP`). That is a source-group gate, not dest PnL.

`RiskEngine` L90–93 comment agrees in intent (shadow path must not FIX-send) but is a no-op; persist-false + session-without-D are the real paper wall. Demo dest send **bypasses** that wall.

---

## 6. Live GET

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | **blocked** (`web_fetch` SSRF; `open_page` retrieve fail) |
| `http://127.0.0.1:5000/api/settings` | **blocked** (`web_fetch` SSRF) |
| `http://127.0.0.1:5000/api/copy/status` | **blocked** (`web_fetch` SSRF; `open_page` retrieve fail) |
| `http://127.0.0.1:5000/api/overview` | **blocked** (`web_fetch` SSRF) |
| `http://localhost:5000/api/health` | **blocked** (`web_fetch` SSRF) |
| `http://localhost:5000/api/copy/status` | **blocked** (`open_page` retrieve fail) |
| `http://127.0.0.1:18720/api/health` | **blocked** (`open_page` retrieve fail) |

Launch profile would have been `http://localhost:5000` (`apps\api\Properties\launchSettings.json` L17). IIS Express profile cites `18720`. This slot **does not** claim a live `realCopyEnabled` JSON value. File bind + env `true` + no re-pin is sufficient to **FAIL claim 3**. Process bit was **not** remeasured.

---

## 7. What this slot refuses to say

- Will not say “EX5 / copy edge / dest profit proven.”
- Will not recycle sibling `−$241,580` / `8463` as this-slot measurements (not re-summed here).
- Will not say tree-wide `35=D=0` (stale). Assigned session is `35=A` only.
- Will not say `NewOrderSingleImplemented` is const `false` (HEAD is `=> DemoDest`).
- Will not say `RealCopyEnabled` is forced false after logon (claim 3 FAIL).
- Will not say demo dest is `SAFE_BY_ABSENCE`.
- Will not treat `DestFillPrice=4390.2` as dest profit.
- Will not treat dest DTO constructor `0` as a live mark.

---

## 8. Files read (this slot)

| Path | Role |
|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | assigned (1) — 135/135 |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | assigned (2) — 212/212 |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | assigned (3/4) — 189/189 |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | assigned (4/5) — 70/70 |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | post-logon flag |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | sole `RealCopyEnabled =` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | flag + copyNote |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | hopper / demo send — 625/625 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | sibling `Build("D")` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | sibling `Build("D")` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | sibling `Build("D")` ×3 |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 20s tick |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | dest PnL `0` |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | `DestinationRealPnl` |
| `D:\Prop\src\Application\Copy\CopyTradingModels.cs` | status DTO |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | paper fill |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | SHADOW-or-better admit |
| `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs` | AUTO_ADMIT |
| `D:\Prop\src\Domain\Copy\CopyGroupFilter.cs` | demo/contest gate |
| `D:\Prop\src\Domain\Enums\TraderState.cs` | SHADOW / LIVE enums |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | unused POCO default false |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | `.env` loader |
| `D:\Prop\apps\api\Program.cs` | health/settings echo |
| `D:\Prop\apps\web\src\api\hooks.ts` | GET-only copy hooks |
| `D:\Prop\apps\api\Properties\launchSettings.json` | port 5000 |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | promote-false test |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | AllowFixSend false |
| `D:\Prop\data\demo_copy_ledger.json` | dest fill residual |
| `D:\Prop\.env` L73 / L106 | flag booleans only |

---

**End P500_VERIFY_94.** Slot **94**. Verdict **FAIL** (claim 3 disproved; claims 1 and 4 fail unscoped). Risk to live capital **NONE**; demo dest send **wired**.
