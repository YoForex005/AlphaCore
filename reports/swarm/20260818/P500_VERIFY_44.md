# P500_VERIFY_44 — Adversarial confirm of five copy/profit claims (slot 44)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_44.md` |
| Agent / slot | P500 adversarial verifier **44** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned SUTs (full re-read this slot) | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135), `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212), `D:\Prop\src\Domain\Risk\RiskEngine.cs` (189/189), `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (70/70) |
| Adjacent hops (opened only to try to disprove) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyGroupFilter.cs`, `CopyRosterEngine.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `apps/api/Program.cs`, `apps/web/src/api/hooks.ts`, `EnvFile.cs`, `TraderState.cs`, lab `.env` **boolean / public dest ids only** |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Secrets printed | **None.** Quoted only the already-on-disk boolean `REAL_COPY_EXECUTION_ENABLED=true`, public dest ids `5328266` / `1369850`, public host prefix `demo-`, public sender prefix `demo.`. Tag 554 / passwords / proxy / DB strings never dumped. |
| Live GET this slot | **Blocked.** `web_fetch` SSRF-denied `http://127.0.0.1:5000/api/health`, `http://localhost:5000/api/copy/status`, `http://localhost:5000/api/settings`. `open_page` on `http://127.0.0.1:5000/api/health` failed to retrieve. No shell in this slot. Any claim that needs a live DTO is **FAIL**. |
| This slot sent `35=D` | **No** |
| `REAL_COPY` flipped | **No** |
| Prior swarm text | Untrusted. `E002` / `W500_*` / `P500_BOOK_*` / sibling `P500_VERIFY_*` were **not** used as proof. |

**Honesty rule:** FAIL any assigned claim that is not proven from a file this slot or a live GET this slot. A compile-time default is not a runtime pin. `CTraderFixSession` having no `35=D` is not “the product has no `35=D` builder.” `AllowFixSend` on a risk DTO is not a socket write. `SHADOW` is a source state, not dest cash. Demo dest fills are not live Pepperstone profit. Do **not** print secrets.

---

## 0. Verdict (binding)

**FAIL — claim 3 is disproven from live files. Claim 1 fails if read product-wide. Live GET unobtainable this slot (does not rescue claim 3).**

| # | Assigned claim | File-proven result | Class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS_SESSION** on assigned `CTraderFixSession.cs` (135/135 is `35=A` only). **FAIL_UNSCOPED** as product-wide. | Assigned file outbound MsgType is only `(35, "A")`. Sibling `CTraderFixCopyOpen.Build("D")` is a real builder and is **on** the hosted 20s hop. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive` is `=> false`. `FromBaseline` never emits `LIVE` or `LIVE_CANDIDATE`. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL (disproven)** | Hosted logon **never writes** the flag. DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`. Grep `RealCopyEnabled\s*=` in `*.cs` = **1** hit (`DependencyInjection.cs` L41). |
| 4 | sending now cannot be the profit path | **PASS_NOT_BOOKED_DEST_PROFIT** | Scorer cannot mint `LIVE`. Persist `AllowFixSend=false`. Venue const unreconciled. Overview `DestinationRealPnl` is constructor `0`. UI is GET-only. Residual: demo dest hop **can** `Build("D")` and is **not** live dest +EV. |
| 5 | SHADOW on demo is not destination profit | **PASS** | `SHADOW` is a quality/risk label on **source** XAU. Paper `ShadowOrder` is `SimulateEntry`. Policy admits demo/contest **source** groups only. Dashboard dest PnL is literal `0`. That is not dest cash. |

One-line:

```text
FAIL. CTraderFixSession is 35=A only; product Build("D") exists on demo dest hop. CanPromoteToLive is false. RealCopyEnabled is NOT forced false after logon (.env true + DI bind + no re-pin). Sending now is not booked dest profit. SHADOW-on-demo is not dest profit. Live GET blocked. Risk NONE on live 1369850; DEMO dest hop exists.
```

---

## 1. Claim 1 — no `35=D` builder — **PASS_SESSION** / **FAIL_UNSCOPED**

Assigned file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines, full read this slot).

The only outbound builder is `BuildLogon`. Tag 35 is hard-coded `"A"`:

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

The only `WriteAsync` is the logon (`L47–L50`). Socket is `using TcpClient` + `await using SslStream` (`L35–L39`) and is disposed after one read. Inbound `Extract(reply, "35")` (`L55`) accepts Logon `"A"` or records reject. That is not a builder.

Measured on this file:

| Fact | Evidence |
|---|---|
| Outbound tag 35 | `"A"` only (`L96`) |
| `WriteAsync` count | **1** (`L49`) — the logon |
| `NewOrderSingle` / `Build("D")` / `"D"` MsgType | **0** |
| Public API | `TryLogonAsync` only |

`grep` of the literal `35=D` under product `*.cs` = **0** string literals. Builders use `Build("D", ...)`.

**Adjacent (does not flip the assigned-file PASS; kills a product-wide reading):**

| File | What it does |
|---|---|
| `CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra.ToArray())` then `Write` on TRADE **5212** |
| `CTraderFixCopyOpen.cs` L142–L156 | Generic `Build(string type, ...)` — `(35, type)` |
| `CTraderFixDemoMatrix.cs` L91–L93 | Local `SendD` → `Build("D", ...)` |
| `CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | `Build("D", ...)` ×3 |
| `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566 | **calls** `CTraderFixCopyOpen.SendAsync` |
| `CopyTradingHostedService` L28–L30 | 20s tick: `TickRosterAsync` → `GenerateShadowIntentsAsync` → `ExecuteDemoCopyAsync` |

`CTraderFixCopyOpen` refuses live dest identity (`host` must start `demo-`, `sender` must start `demo.`, `account == "1369850"` fails closed) (`L37–L42`). That is a **demo dest sender**, not absence of a builder.

Lab `.env` public dest identity (no secrets): `CTRADER_FIX_HOST` starts `demo-`, `CTRADER_FIX_ACCOUNT_ID=5328266`, `CTRADER_FIX_TRADE_SENDER_COMP_ID` starts `demo.`. `CopyTradingService.DemoDest` (`L45–L48`) is therefore **true** if that `.env` is loaded. `NewOrderSingleImplemented => DemoDest` (`L50`). HEAD is **not** “NewOrderSingle const false.”

**Claim 1 as “`CTraderFixSession` has no `35=D` builder” is proven. Claim 1 as “there is no `35=D` builder in this tree” is false.** Assigned wording after “Read `CTraderFixSession.cs`” is the scoped reading. Adversarial overall on the unscoped phrase **FAIL_UNSCOPED**. This slot does **not** greenwash product `35=D=0`.

---

## 2. Claim 2 — `CanPromoteToLive` is false — **PASS**

Assigned file `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212 lines, full read this slot).

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

| Fact | Evidence |
|---|---|
| `CanPromoteToLive` body | `=> false`. The `current` argument is unused. |
| `FromBaseline` outputs | `INSUFFICIENT_DATA`, `RISK_BLOCKED`, `SHADOW`, `WATCH`, `EARLY_SCORE` only |
| `LIVE` / `LIVE_CANDIDATE` in this file | **0** emissions |
| Ceiling after 3 disciplined winners | `AfterHighEarlyScore() => SHADOW` |
| Unit lock | `BaselineScorerTests.cs` L25–L26: three winners → `SHADOW`; `CanPromoteToLive` must be false |

`TraderState` enum still **contains** `LIVE_CANDIDATE=4` and `LIVE=5` (`TraderState.cs` L9–L10). That is a type, not a promotion path. `CopyTradingService` L202 includes those states in a copyable set **if a row already has them**. This slot found **no** writer that assigns `TraderState.LIVE` from the scorer. Claim 2 as worded (`CanPromoteToLive is false`) is **file-proven**.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL (disproven)**

This claim is **false** on HEAD. Assigned `CTraderFixSession` does not touch the flag. The hosted logon that **uses** that session also does not re-pin it.

**Only write** of the property in product `*.cs` (grep `RealCopyEnabled\s*=`):

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

API boot loads lab `.env` then environment variables, then that DI:

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` includes `D:\Prop\.env` (`EnvFile.cs` L14). Lab `.env` L73 (boolean only):

```text
REAL_COPY_EXECUTION_ENABLED=true
```

Hosted logon after `TryLogonAsync` **reads** the flag and logs it. It writes Quote/Trade `LoggedOn` / `Status` / `LastError` / `UpdatedAt` only:

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

`LiveRuntimeStatus.RealCopyEnabled` is a public `{ get; set; }` (`LiveRuntimeStatus.cs` L32) with **no** post-logon force-false. `Snapshot()` **reports** the current value; it does not pin it.

`RiskEngine` is not a pin either. The empty branch at L90–L93 comments that the shadow path “never allows FIX send,” but `AllowFixSend` is later:

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

If `RealCopyEnabled` is true and the other three bits are true, **RiskEngine will return `AllowFixSend=true`**. That is the opposite of “forced false after logon.” (Hosted persist then hard-codes `AllowFixSend = false` — claim 4, not a logon re-pin of `RealCopyEnabled`.)

`LiveCopyPage.tsx` L13 displays `status?.realCopyArmed ? 'YES' : 'NO'`. That is a GET of `/api/copy/status` (`hooks.ts` L60–L61). `GetStatusAsync` sets `RealCopyArmed: _runtime.RealCopyEnabled` (`CopyTradingService.cs` L64). UI cannot force the flag false.

**Live GET** of `/api/health`, `/api/settings`, `/api/copy/status` was **blocked** this slot. That does **not** rescue the claim: the write path is file-disproven. A compile-time comment “NewOrderSingle still unimplemented” is not a runtime pin.

Claim 3 = **FAIL**.

---

## 4. Claim 4 — sending now cannot be the profit path — **PASS_NOT_BOOKED_DEST_PROFIT**

Read as: “a send **now** is not how this product books destination profit / live +EV.” File-proven. Not read as: “no socket on this machine can emit `35=D`.”

### 4.1 Hosted live send path cannot fire

`CopyTradingService.GenerateShadowIntentsAsync`:

| Gate | HEAD value | File |
|---|---|---|
| Persist `AllowFixSend` | **hard-coded `false`** | L324 |
| `VenueReconciled` | `public const bool VenueReconciled = false` | L20 |
| `Reconciled` passed to risk | that const | L304 |
| Live-send `if` | `decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled` | L330 |
| Else | `intent.Status = "SHADOW_ONLY"` + paper `SimulateEntry` | L336–L346 |
| Scorer `LIVE` | never (`CanPromoteToLive => false`) | BaselineScorer L211 |

Even if DI left `RealCopyEnabled=true` (claim 3) and `DemoDest` made `NewOrderSingleImplemented=true`, **L330 is dead** because `VenueReconciled` is const false **and** persist overwrites `AllowFixSend`. The live-send branch only writes `"LIVE_SEND_BLOCKED_UNIMPLEMENTED"` — it does not call `CTraderFixCopyOpen`.

`CTraderFixSession` (assigned) can only send `35=A`. That cannot be dest profit.

### 4.2 Dest profit is not booked from any send

`OverviewDto` has `DestinationRealPnl` (`DashboardModels.cs` L16). The only constructor:

```33:52:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        return new OverviewDto(
            accounts,
            brokers,
            xauTraders,
            three,
            scores.Count(s => s.CurrentState == TraderState.WATCH),
            scores.Count(s => s.CurrentState == TraderState.SHADOW),
            scores.Count(s => s.CurrentState == TraderState.LIVE_CANDIDATE),
            scores.Count(s => s.CurrentState == TraderState.LIVE),
            scores.Count(s => s.CurrentState == TraderState.RISK_BLOCKED),
            shadowPnl,
            0,
            0,
            0,
            ...
            _runtime.RealCopyEnabled);
```

`shadowPnl` (L29) is `Sum(SourceVsShadowSlippage)` — paper slippage, not dest cash. The three literal `0`s are `DestinationRealPnl`, `XauGross`, `XauNet`. Trader-row `ShadowPnl` is also literal `0` (`EfDashboardQueries.cs` L118). Risk dashboard daily/drawdown/XAU exposures are constructor `0` (`L208`).

`LiveCopyPage.tsx` is GET-only (`useCopyStatus` / `useCopyIntents`). No POST. Stats are counts (`shadowTraders`, `liveTraders`, `liveSends`, `shadowFills`). Empty-state text admits demo dest auto-send (`L57`) — that is a **send hop**, not dest PnL booking.

### 4.3 Residual that must not be greenwashed

`CopyTradingHostedService` L30 **does** call `ExecuteDemoCopyAsync` every 20s. That method **bypasses** `RiskEngine.Evaluate` and calls `CTraderFixCopyOpen.SendAsync` (`L528` close, `L566` open) when `DemoDest` is true. `CTraderFixCopyOpen` **will** `Build("D")` on demo dest and **refuses** `1369850`. Lab `.env` dest is public demo `5328266`.

So: sending now **can** be a **demo dest hop**. It **cannot** be the **booked dest-profit path** and **cannot** be live Pepperstone `1369850`. Claim 4 as profit-path wording = **PASS**. Claim 4 as “no send exists” = **FAIL** (not the assigned wording).

`RiskEngine` itself can approve `AllowFixSend=true` if `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. Hosted tick never feeds that combination (`Reconciled` const false; persist forced false). `MaxSlippage` is unread in `Evaluate` — unused; not a dest-profit path.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **PASS**

Assigned `BaselineScorer` + `LiveCopyPage` + adjacent paper/dest constructors.

`SHADOW` is a **source trader state** from **source** reconstructed XAU features (`NetPnl`, PF, martingale, …). Quality formula (`L152–L160`) can be high while `NetPnl` is later negative-tolerant only via the `NetPnl > 0` +15 bump — it is still source book, not dest:

```199:209:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        if (!earlyEligible)
            return TraderState.INSUFFICIENT_DATA;

        if (quality >= 70 && risk < 40)
            return TraderState.SHADOW;

        if (quality >= 55)
            return TraderState.WATCH;

        return TraderState.EARLY_SCORE;
    }

    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;
```

`CopyGroupFilter.IsDemoOrContest` (`CopyGroupFilter.cs` L9–L23) is a **source-group** gate (`demo` / `contest` path segments). `XauUsdOneToOneCopyPolicy.IsTraderEligible` requires `SHADOW+`, 20 completed XAU, `XauNetPnl > 0`, and that source-group filter (`L105–L112`). Roster admit uses the same filter (`CopyRosterEngine.cs` L52–L53). That selects **which MT5 source logins** may be copied. It is not dest cash.

Paper fills:

```35:60:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
    public ShadowFill SimulateEntry(
        string shadowOrderId,
        TradeDirection direction,
        decimal quantity,
        decimal sourcePrice,
        DestinationQuote quote,
        DateTimeOffset now,
        TimeSpan modeledDelay)
    {
        ...
        return new ShadowFill
        {
            ShadowOrderId = shadowOrderId,
            Price = raw,
            Quantity = quantity,
            ...
            SourceVsShadowSlippage = slippage
        };
    }
```

`GenerateShadowIntentsAsync` writes `ShadowOrder` only on `SHADOW_ONLY` + `Approve` (`CopyTradingService.cs` L336–L359). `LiveCopyPage` L14 / L18 shows `shadowTraders` and `shadowFills` **counts**. `DestinationRealPnl` remains constructor `0` (claim 4.2).

Demo dest `35=D` (claim 1 adjacent / 4.3) is a **separate** hop. It is not the `SHADOW` label. SHADOW-on-demo-source ≠ destination profit. Claim 5 = **PASS**.

---

## 6. Live GET this slot

Attempted:

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` (`web_fetch`) | SSRF blocked (private IP) |
| `http://localhost:5000/api/copy/status` (`web_fetch`) | SSRF blocked (private IP) |
| `http://localhost:5000/api/settings` (`web_fetch`) | SSRF blocked (private IP) |
| `http://127.0.0.1:5000/api/health` (`open_page`) | Failed to retrieve page content |

No runtime DTO for `realCopyEnabled` / `realCopyArmed` was obtained. File proof already **disproves** claim 3; a live `true` would confirm, a live `false` would require a writer this slot did not find. Claim 3 stays **FAIL**. Claims 1/2/4/5 do not depend on a live DTO.

---

## 7. Risk to capital

| Book | Risk | Why |
|---|---|---|
| Live Pepperstone dest `1369850` | **NONE** (`SAFE_BY_ABSENCE`) | Hosted session is `35=A` only. `CTraderFixCopyOpen` refuses `account == "1369850"` and non-`demo-` host / non-`demo.` sender. Persist `AllowFixSend=false`. `VenueReconciled` const false. `CanPromoteToLive => false`. |
| Demo dest `5328266` (public id; lab `.env` dest) | **Residual dest hop** | Hosted 20s tick **can** `Build("D")` via `ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.SendAsync` without `RiskEngine.Evaluate`. Not booked as `DestinationRealPnl`. Not live license. |
| Source MT5 books | Not dest | Ingest/score only. Roster flatten is dest-intent rows (`FLATTEN_LOSS_CUT`), not Manager close. |

Wanting dest profit does not create an edge. Copy-all catalog would copy `RISK_BLOCKED` source losses if a send hop were opened 1:1. That is **not** claimed as dest PnL here.

---

## 8. Assigned SUT honesty (what each file can and cannot prove)

| Assigned file | Proves | Cannot prove |
|---|---|---|
| `CTraderFixSession.cs` | This type has no `35=D` builder; outbound is `35=A` logon only | Product-wide “no `35=D` builder”; `RealCopyEnabled` after logon |
| `BaselineScorer.cs` | `CanPromoteToLive => false`; `SHADOW` is source quality; no dest $ | Whether a row was manually set `LIVE`; live GET trader counts |
| `RiskEngine.cs` | Rejects set `AllowFixSend=false`; approve `AllowFixSend` is `RealExecutionEnabled && !KS && Reconciled && VenueHealthy`; empty “shadow never send” branch does **not** return | A logon re-pin of `RealCopyEnabled`; a socket write |
| `LiveCopyPage.tsx` | GET-only counts; SHADOW is a headcount; empty-state admits demo dest auto-send | Runtime `realCopyArmed` without GET; dest PnL (field does not exist on this page) |

---

## 9. What this slot did **not** do

- Did not modify product, tests, or `.env`.
- Did not send `35=D`.
- Did not flip `REAL_COPY_EXECUTION_ENABLED`.
- Did not print secrets.
- Did not live-attach the API (SSRF / no shell).
- Did not treat prior swarm markdown as evidence.

---

*End of P500_VERIFY_44. Product source was not modified. No `35=D` was built or sent by this slot. No secrets printed. `REAL_COPY` was not flipped.*
