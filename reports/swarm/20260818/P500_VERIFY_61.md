# P500_VERIFY_61 — Adversarial four-file verify (slot 61)

| Field | Value |
|---|---|
| Slot | **61** |
| Date | 2026-08-18 |
| Agent | P500_VERIFY_61 (adversarial; sibling `P500_*` / `W500_*` numbers are **not** evidence) |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_61.md` |
| Assigned | Read `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`. Confirm: (1) no `35=D` builder; (2) `CanPromoteToLive` is false; (3) `RealCopyEnabled` forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. **FAIL any claim that cannot be proved from a file or live GET.** |
| Assigned reads (this pass, full file) | `CTraderFixSession.cs` **135/135**; `BaselineScorer.cs` **212/212**; `RiskEngine.cs` **189/189**; `LiveCopyPage.tsx` **70/70** |
| Adjacent (this pass) | `CTraderFixLogonHostedService.cs`; `DependencyInjection.cs`; `CopyTradingService.cs`; `CopyTradingHostedService.cs`; `CTraderFixCopyOpen.cs`; `CTraderFixOptions.cs`; `LiveRuntimeStatus.cs`; `ShadowCopyEngine.cs`; `EfDashboardQueries.cs` L21–52; `DashboardModels.cs` L5–22; `XauUsdOneToOneCopyPolicy.cs`; `CopyRosterEngine.cs`; `CopyGroupFilter.cs`; `DemoCopyLedger.cs`; `apps/api/Program.cs`; `apps/web/src/api/hooks.ts`; `tests/Unit/BaselineScorerTests.cs`; `DealIngestionService.cs` L140; `.env` L49/L50/L64/L73 **flag + public dest identity only**; `D:\Prop\data\demo_copy_ledger.json` |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| `.env` modified | **No.** |
| Secrets printed | **None.** No tag 554, no manager/FIX/DB/proxy passwords. Quoted only the already-on-disk boolean `REAL_COPY_EXECUTION_ENABLED=true`, public dest ids `5328266` / `1369850`, public host prefix `demo-`, public sender prefix `demo.`. |
| Live GET this slot | **Attempted and blocked.** `web_fetch` `http://127.0.0.1:5000/api/health` → SSRF reject on loopback. `open_page` same URL → retrieve fail. **No live JSON** for `realCopyEnabled`, quote/trade logon, dest PnL, or intents. |
| Live `35=D` sent this slot | **No.** |
| `REAL_COPY` flipped this slot | **No.** |
| Method | Independent full `read_file` of the four assigned files, then the logon/DI/copy hop they actually call. Targeted `grep` for `35=D` / `Build("D")` / `CanPromoteToLive` / `RealCopyEnabled =` / `REAL_COPY_EXECUTION_ENABLED` / `DestinationRealPnl`. Prior swarm text treated as **untrusted**. |
| Honesty rule | A `35=A` logon is not a fill. A comment is not a runtime pin. A dashboard constructor `0` is not dest cash. `CanPromoteToLive => false` is not a send interlock. Env `true` is not a live-`1369850` license. Sibling hop `Build("D")` **is** a dest path. Wanting profit is not an edge. |

Assigned claims:

1. no `35=D` builder
2. `CanPromoteToLive` is false
3. `RealCopyEnabled` forced false after logon
4. sending now cannot be the profit path
5. SHADOW on demo is not destination profit

---

## 0. Verdict (binding)

**FAIL.** Claim 2 holds on the assigned scorer. Claim 1 holds **only** if scoped to `CTraderFixSession.cs`. Claims 3 and 4 are **disproved** from files. Claim 5 holds for paper SHADOW / booked dest PnL and **fails** as an unscoped “SHADOW cannot become dest cash” statement.

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS_FILE / FAIL_UNSCOPED** | Assigned session **135/135** outbound MsgType is `(35, "A")` only. Product `Build("D")` exists (`CTraderFixCopyOpen` L95 hosted; `CTraderFixDemoTestTrade` ×3; `CTraderFixDemoMatrix` L93). |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(TraderState current) => false` at `BaselineScorer.cs` L211. `FromBaseline` ceiling is `SHADOW`. Unit test asserts it. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | **Disproved.** Logon host **reads** the flag and **never assigns** `false`. Sole `RealCopyEnabled =` write is DI bind of `.env` `REAL_COPY_EXECUTION_ENABLED=true`. Live GET of RAM not obtained. |
| 4 | sending now cannot be the profit path | **FAIL** | Hosted 20 s tick calls `ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.SendAsync` → `Build("D")` **without** `RiskEngine.Evaluate` and **without** reading `RealCopyEnabled`. Lab `.env` satisfies `DemoDest`. Ledger holds dest fill `305750` / dest pos `237339770` / px `4390.2` / `DestClosed=false`. Dashboard `DestinationRealPnl` is constructor `0`, not a mark. Live `1369850` refused. |
| 5 | SHADOW on demo is not destination profit | **PASS_PAPER / FAIL_UNSCOPED** | Paper: `SHADOW` is a source state; `ShadowPnl` = sum of `SourceVsShadowSlippage`; dest DTO is `0`; UI shows counts. Unscoped: `SHADOW` is AUTO_ADMIT-eligible; hopper sends on `ADMITTED` and does **not** require `LIVE`. `LiveCopyPage` L57 admits dest auto-send after ADMIT. |

One-line:

```text
FAIL. CTraderFixSession is 35=A only (PASS_FILE). CanPromoteToLive=>false (PASS). RealCopyEnabled is NOT forced false after logon (FAIL). Hosted demo hopper CAN 35=D (FAIL as “sending cannot be profit”). SHADOW paper ≠ dest PnL; SHADOW can ADMIT and dest-send (FAIL unscoped). Live 1369850 refused. Live GET blocked. This slot sent 0.
```

**Risk to capital:** **NONE on live `1369850`** (`SAFE_BY_ABSENCE`: assigned session has no `35=D`; `CTraderFixCopyOpen` refuses non-`demo-` host, non-`demo.` sender, or account `1369850`). **Not absent on demo dest `5328266`:** hosted hop can emit `35=D` and the on-disk ledger already records an open dest fill. This slot sent **0**.

---

## 1. Claim 1 — no `35=D` builder — **PASS_FILE / FAIL_UNSCOPED**

Assigned file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135** lines, full read).

Token census **this file only**:

| Pattern | Hits |
|---|---:|
| Literal `35=D` | **0** |
| `NewOrderSingle` | **0** |
| `(35, "D")` / `Build("D")` | **0** |
| `(35, "A")` | **1** (`BuildLogon` L96) |
| `ssl.WriteAsync` | **1** (L49) — bytes of that Logon |
| `Extract(..., "35")` | **1** — **inbound** reply (L55) |

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
| Socket lifetime | `using TcpClient` + `await using SslStream`; disposed after one read |
| Inbound `35=A` treated as | `LoggedOn = true` (L56–64) — **logon ack, not a fill** |

Adversarial residual (**fails the unscoped wording**): product `*.cs` has **five** `Build("D")` call sites.

| File | What this slot measured |
|---|---|
| `src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra)` after TRADE logon + SecurityList. **Refuses** non-`demo-` host, non-`demo.` sender, or account `1369850` (L37–42). **Called from** `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566, which is on the 20 s hosted tick. |
| `src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | `Build("D", …)` ×3. Called from `tools/DemoFixTestTrade` only (not DI). |
| `src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` L93 | `Build("D", …)`. Tools/matrix only. |

Claim 1 as written against **`CTraderFixSession.cs`** is true. Tree-wide “no `35=D` builder” is **false** on HEAD. Unscoped confirmation **FAIL**.

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
| Product callers of `CanPromoteToLive` | **0** in `src\` / `apps\`. Only `tests\Unit\BaselineScorerTests.cs` L21–26 and a `_tmp_*` probe. |
| Persist | `DealIngestionService.cs` L140 copies `CurrentState = score.SuggestedState`. Suggested ceiling is `SHADOW`. |
| Test | three disciplined winners → `SHADOW`; `CanPromoteToLive(...).Should().BeFalse()`. |

There is **no** code path in this file that emits `TraderState.LIVE`. Promotion to live is not a scorer function.

**Does not** interlock the demo dest sender (claim 4). `ExecuteDemoCopyAsync` keys on roster `ADMITTED`, not `LIVE`.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL**

This claim is **false on disk**. Live RAM was **not** fetched.

### 3.1 Logon host does not re-pin

Assigned-adjacent file (the only post-logon writer of `LiveRuntimeStatus` FIX fields): `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`.

After both `TryLogonAsync` calls it writes Quote/Trade status and **logs** the flag. It never assigns it:

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

`grep` `RealCopyEnabled =` under product `*.cs`: **one hit**.

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

That is a **bind**, not a force-false.

### 3.2 Lab env is `true`

`D:\Prop\.env` L73 (flag key only): `REAL_COPY_EXECUTION_ENABLED=true`.

`apps\api\Program.cs` L10 loads that env (`EnvFile.FindAndLoad()`), then L15 `AddTraderIntelligence`. `/api/health` L55 and `/api/settings` L76 echo `runtime.RealCopyEnabled` — they do **not** override it.

### 3.3 Options default is unused by logon

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (`CTraderFixOptions.cs` L35). `CTraderFixLogonHostedService` does **not** read that POCO. `apps\fix-worker\Worker.cs` L21 reads `CTrader:RealCopyExecutionEnabled` with default `false` — that is the **fix-worker**, not API logon, and it still **does not send**.

### 3.4 What “after logon” actually leaves

| Object | After successful `35=A` |
|---|---|
| `_runtime.Quote.LoggedOn` / `_runtime.Trade.LoggedOn` | set from logon result |
| `_runtime.RealCopyEnabled` | **unchanged** from DI bind of env |
| This slot live GET of `/api/health` or `/api/settings` | **not obtained** (loopback blocked) |

Cannot claim “forced false after logon.” The opposite wiring is on disk. Claim 3 is **FAIL**.

---

## 4. Claim 4 — sending now cannot be the profit path — **FAIL**

Scope used: **destination cash activity**, not “booked `DestinationRealPnl`.” A constructor `0` is not proof that sending cannot be dest P&L. The assigned session cannot send. The **hosted hop can**.

### 4.1 Assigned session cannot send a ticket

`CTraderFixSession` is Logon-only (claim 1). A `35=A` ack is not dest PnL. **Not sufficient** to prove the process cannot send.

### 4.2 Scorer cannot put anyone in LIVE

`CanPromoteToLive => false` (claim 2). Hopper live-send branch L330 requires `TraderState.LIVE` **and** `VenueReconciled` (const `false` at `CopyTradingService.cs` L20). That branch is **dead**. Persist hard-codes `AllowFixSend = false` (L324).

`RiskEngine` allow-send formula (L147–150):

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

Hopper `Evaluate` passes `Reconciled = VenueReconciled` (const `false`) → increasing actions reject `VENUE_NOT_RECONCILED` (`RiskEngine.cs` L84–85).

L90–93 (“Shadow path … never allows FIX send”) is a **comment no-op**. It does not return. The real paper wall is persist-false + dead LIVE branch.

### 4.3 Dest profit is not computed (does **not** prove claim 4)

`OverviewDto.DestinationRealPnl` exists (`DashboardModels.cs` L16). The product assignment in `EfDashboardQueries.GetOverviewAsync` is the literal `0` (positional arg after `shadowPnl`, L44). That is a constructor, **not** a mark-to-market of dest fills.

`LiveCopyPage.tsx` has **no** dest-PnL field, **no** send button, **no** “profit” column. It shows counts + blockers titled “Pepperstone cannot be filled” (L24). Hooks `useCopyStatus` / `useCopyIntents` are GETs only (`hooks.ts` L60–65).

### 4.4 Why the claim is **false**

HEAD **does** send on demo dest. Lab identity matches `DemoDest`:

| `.env` key (public) | Value | `DemoDest` predicate |
|---|---|---|
| `CTRADER_FIX_HOST` L49 | `demo-us-eqx-01.p.c-trader.com` | `StartsWith("demo-")` |
| `CTRADER_FIX_TRADE_SENDER_COMP_ID` L64 | `demo.pepperstone.5328266` | `StartsWith("demo.")` |
| `CTRADER_FIX_ACCOUNT_ID` L50 | `5328266` | `!= "1369850"` |

```45:50:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public bool DemoDest =>
        (_config["CTRADER_FIX_HOST"] ?? "").StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
        && (_config["CTRADER_FIX_TRADE_SENDER_COMP_ID"] ?? "").StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
        && _config["CTRADER_FIX_ACCOUNT_ID"] != "1369850";

    public bool NewOrderSingleImplemented => DemoDest;
```

`CopyTradingHostedService` 20 s tick: `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`** (`CopyTradingHostedService.cs` L28–30).

`ExecuteDemoCopyAsync` (L483–605):

- Returns 0 if `!DemoDest`.
- Does **not** read `RealCopyEnabled`.
- Does **not** call `RiskEngine.Evaluate`.
- Calls `CTraderFixCopyOpen.SendAsync` → `Build("D")`.
- Caps `MaxAutoLots = 0.05m` (L22) on **source** tickets, then sends **1:1** those lots (`AllocationFactor = 1m`).
- Refuses live identity `1369850` inside `CTraderFixCopyOpen` L37–42.
- Seeds a dest fill if missing: source `305750` / pos `21250421` / dest `237339770` / clord `C20260818093047317` / px `4390.2` (L500–511).

On-disk ledger `D:\Prop\data\demo_copy_ledger.json` **this slot**:

| Field | Value |
|---|---|
| SourceLogin | `305750` |
| SourcePositionId | `21250421` |
| Lots | `0.01` |
| DestPositionId | `237339770` |
| DestClOrdId | `C20260818093047317` |
| DestFillPrice | `4390.2` |
| DestClosed | **false** |

That is dest **activity** with a fill price. It is not live Pepperstone. It is not a measured edge. It **is** a dest cash path. Claim 4 as written is **FAIL**.

`LiveCopyPage.tsx` L57 empty-state **admits** this: “Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.”

`GetStatusAsync` summary when `DemoDest` (L76–77): “Demo dest auto-copy ON. Eligible demo/contest opens send on the 20s tick… Live 1369850 is never used.” `VenueReconciled` **reported** as `DemoDest` (L67) while the hopper const is `false` (L20). Status lie, not a gate.

This slot did not live-GET dest fills and did not send.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **PASS_PAPER / FAIL_UNSCOPED**

### 5.1 Paper SHADOW is a source state — PASS

`BaselineScorer` / `TraderStateMachine` assign `TraderState.SHADOW` when `quality >= 70 && risk < 40` (L200–201). Fields used: source XAU features (`NetPnl`, martingale, SL use, …). **No dest account, no dest fill, no dest currency.**

`AfterHighEarlyScore() => SHADOW` (L209). Still a state enum (`TraderState.SHADOW = 3`).

Hopper (`CopyTradingService` L336–360): non-LIVE path sets `Status = "SHADOW_ONLY"` and, if a quote row exists **and** `Outcome == Approve`, writes `ShadowOrder` from `_shadow.SimulateEntry(...)`.

`ShadowCopyEngine.SimulateEntry` returns a **modeled** price/slippage. It does not write FIX. It does not touch `DestinationRealPnl`.

Dashboard `ShadowPnl` (`EfDashboardQueries.cs` L29) is `Sum(SourceVsShadowSlippage)`. Next field `DestinationRealPnl` is the literal `0` (L44).

### 5.2 UI does not treat SHADOW as dest profit — PASS

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

Counts only. No dest PnL. `GetStatusAsync` `ShadowTraders` = count of scores with `CurrentState == SHADOW` (L59). `ShadowFills` = `ShadowOrders.Count` (L56). Neither is dest realized PnL.

### 5.3 Unscoped: SHADOW is dest AUTO_ADMIT floor — FAIL

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET` (L81–85). `SHADOW` (and theoretically `LIVE_CANDIDATE` / `LIVE`) can pass if N≥20, `XauNetPnl > 0`, no size-pattern flags, and `CopyGroupFilter.IsDemoOrContest` (demo/contest path segments only).

`CopyRosterEngine.Decide` AUTO_ADMITs when `IsTraderEligible` (L72–79). `TickRosterAsync` writes `Status = "ADMITTED"`. `ExecuteDemoCopyAsync` then sends for `Status == "ADMITTED"` opens (L542–569) **without** requiring `LIVE`.

So: **paper SHADOW ≠ dest profit** (proven). **SHADOW on demo can be the seat that dest-sends** (proven). Unscoped “SHADOW on demo is not destination profit” is **not** a safe confirmation — dest fills are dest cash even if the dashboard constructor stays `0`. Claim 5 unscoped **FAIL**.

This slot did not live-GET trader `305750` state, so it does **not** claim that specific dest fill is currently `SHADOW`. The wiring is enough to refuse the unscoped confirmation.

---

## 6. Live GET

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | **blocked** (`web_fetch` SSRF; `open_page` retrieve fail) |
| `http://127.0.0.1:5000/api/settings` | **not fetched** |
| `http://127.0.0.1:5000/api/copy/status` | **not fetched** |
| `http://127.0.0.1:5000/api/overview` | **not fetched** |

Launch profile would have been `http://localhost:5000` (`apps\api\Properties\launchSettings.json` L17). This slot **does not** claim a live `realCopyEnabled` JSON value. File bind + env `true` + no re-pin is sufficient to **FAIL claim 3**. File hop + ledger is sufficient to **FAIL claim 4**.

---

## 7. What this slot refuses to say

- Will not say “EX5 / copy edge / dest profit proven.”
- Will not recycle sibling `−$241,580` / `8463` / SHADOW census integers as this-slot measurements (not re-read here).
- Will not say tree-wide `35=D=0` (stale). Assigned session is `35=A` only.
- Will not say `NewOrderSingleImplemented` is const `false` (HEAD is `=> DemoDest`).
- Will not say `RealCopyEnabled` is forced false after logon (claim 3 FAIL).
- Will not say demo dest is `SAFE_BY_ABSENCE`.
- Will not say dashboard dest `$0` means dest cash is zero.

---

## 8. Files read (this slot)

| Path | Role |
|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | assigned (1) |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | assigned (2) |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | assigned (3/4) |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | assigned (4/5) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | post-logon flag |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | sole `RealCopyEnabled =` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | flag + copyNote |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | hopper / demo send |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | sibling `Build("D")` |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 20 s tick |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | dest PnL `0` |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | `DestinationRealPnl` |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | paper fill |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | SHADOW ADMIT floor |
| `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs` | AUTO_ADMIT |
| `D:\Prop\src\Domain\Copy\CopyGroupFilter.cs` | demo/contest only |
| `D:\Prop\src\Infrastructure\Copy\DemoCopyLedger.cs` | dest ledger path |
| `D:\Prop\data\demo_copy_ledger.json` | dest fill residual |
| `D:\Prop\apps\api\Program.cs` | health/settings echo |
| `D:\Prop\apps\web\src\api\hooks.ts` | GET-only copy hooks |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | promote-false test |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | unused POCO default false |
| `D:\Prop\apps\fix-worker\Worker.cs` | unused `CTrader:` default false |
| `D:\Prop\.env` L49/L50/L64/L73 | DemoDest identity + flag boolean only |

---

**End P500_VERIFY_61.** Slot **61**. Verdict **FAIL** (claims 3 and 4 disproved; claim 1/5 fail unscoped). Risk to live capital **NONE**; demo dest send **wired**.
