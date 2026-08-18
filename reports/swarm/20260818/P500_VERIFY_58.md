# P500_VERIFY_58 — Adversarial confirm (slot 58)

| Field | Value |
|---|---|
| Slot | **58** |
| Date | 2026-08-18 |
| Agent | P500_VERIFY_58 (adversarial; independent re-read; sibling `P500_*` / `W500_*` integers are **not** evidence) |
| Assigned | Read `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`. Confirm: (1) no `35=D` builder; (2) `CanPromoteToLive` is false; (3) `RealCopyEnabled` forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. **FAIL any claim that cannot be proved from a file or live GET.** |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **None.** Passwords / proxy / FIX password values were not quoted. Only the on-disk boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850` / host prefix `demo-` / sender prefix `demo.` are named. |
| Live GET this slot | **Attempted and blocked.** `web_fetch` of `http://127.0.0.1:5000/api/health` and `http://localhost:5000/api/health` failed: loopback SSRF. **No live JSON.** File proof only. |
| Live `35=D` sent this slot | **No.** |
| `REAL_COPY` flipped this slot | **No.** |
| Method | Full `read_file` of the four assigned files plus the logon/DI/copy hop they actually call. Targeted `grep` for `35=D` / `Build("D")` / `CanPromoteToLive` / `RealCopyEnabled` / `SHADOW` / `DestinationRealPnl`. `.env` inspected **for flag + dest-identity keys only**. |
| Honesty rule | A comment is **not** a runtime pin. A dashboard `0` is **not** a dest mark. A UI label is **not** dest cash. FAIL anything this slot cannot prove from a file or live GET. |

---

## 0. Verdict (binding)

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **FAIL** unscoped / **PASS_SESSION** | Assigned `CTraderFixSession.cs` **135/135**: only outbound MsgType is `(35, "A")` L96. Grep of that file for `35=D` / `(35, "D")` / `Build("D")` = **0**. Product tree has `Build("D")` **×5** (`CTraderFixCopyOpen` L95, `CTraderFixDemoMatrix` L93, `CTraderFixDemoTestTrade` L139/163/197). Hosted hop **calls** `CTraderFixCopyOpen.SendAsync`. Unscoped “no builder” is **false**. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(TraderState current) => false` at `BaselineScorer.cs` L211. Argument unused. `FromBaseline` ceiling is `SHADOW`. Unit test L26 asserts `.BeFalse()`. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | **Disproved**, not merely unproved. Sole assignment `RealCopyEnabled =` is `DependencyInjection.cs` L41 binding `.env` L73 `true`. `CTraderFixLogonHostedService` L60–70 writes Quote/Trade fields and **logs** the flag; never assigns `false`. Live GET could not re-measure the process bit. |
| 4 | sending now cannot be the profit path | **FAIL** unscoped / **PASS_NOT_BOOKED_DEST_PROFIT** (session + persist) | Assigned session can send only `35=A`. Persist `AllowFixSend = false` L324. `DestinationRealPnl` is constructor `0` (not a mark). Residual **disproves** the unscoped claim: `CopyTradingHostedService` 20s tick calls `ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.Build("D")` when `DemoDest` is true. `.env` dest keys make `DemoDest` **true** (host `demo-`, sender `demo.`, account `5328266` ≠ `1369850`). Ledger has an **open** dest fill. Dest P&L is **unmeasured**. |
| 5 | SHADOW on demo is not destination profit | **PASS** | `SHADOW` is a **source-trader state** from source reconstructed XAU (`NetPnl = Sum(NetRealizedPnl)`). Dashboard `ShadowPnl` is `Sum(SourceVsShadowSlippage)`. Paper `ShadowCopyEngine.SimulateEntry` is not a venue fill. `LiveCopyPage` shows SHADOW **counts**, no dest cash. Residual: policy ADMIT floor is “already SHADOW-or-above”; that can **trigger** dest send; the SHADOW number itself is still not dest profit. |

**Slot verdict: `FAIL`.**

Claim 3 is **disproved**. Claim 1 and claim 4 fail when read as product-wide statements. One FAIL is enough. Claims 2 and 5 hold from files.

**Risk to capital:** **NONE on live `1369850`** (`CTraderFixSession` has no NewOrderSingle; `CTraderFixCopyOpen` L37–42 refuses non-`demo-` host, non-`demo.` sender, or account `1369850`). **Not absent on demo dest `5328266`:** hosted tick can emit `35=D` **without** `RiskEngine.Evaluate` and **without** reading `RealCopyEnabled`. This slot sent **0**. Dest dashboard constructor is **$0** and is **not** a live mark.

---

## 1. Claim 1 — no `35=D` builder — **FAIL** (unscoped) / **PASS** (`CTraderFixSession.cs`)

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

| Check | Measured this slot |
|---|---|
| Outbound MsgType in assigned file | **only** `(35, "A")` L96 |
| `ssl.WriteAsync` count | **1** (L49, logon bytes) |
| `35=D` / `(35, "D")` / `Build("D")` / `NewOrderSingle` in this file | **0** (file-scoped grep) |
| Socket lifetime | `using TcpClient` + `await using SslStream`; disposed after one read |
| Inbound `35=A` treated as | `LoggedOn = true` (L56–64) — **logon ack, not a fill** |
| Other assigned files (`BaselineScorer`, `RiskEngine`, `LiveCopyPage`) | **0** FIX builders |

Adversarial residual (**fails the unscoped wording**): sibling product files **do** build `35=D`.

| File | Measured |
|---|---|
| `src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 / L142 | `Build("D", sender, target, seq, extra)` after TRADE `35=A` + SecurityList `35=x`. **Refuses** non-`demo-` host, non-`demo.` sender, or account `1369850` (L37–42). |
| `src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` L93 | `Build("D", …)` (matrix helper). |
| `src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` L139, L163, L197 | `Build("D", …)` ×3 (demo test helper). |
| `src\Infrastructure\Copy\CopyTradingService.cs` L528, L566 | Hosted `ExecuteDemoCopyAsync` **calls** `CTraderFixCopyOpen.SendAsync`. |
| `src\Infrastructure\Hosting\CopyTradingHostedService.cs` L28–30 | 8s delay then **20s** tick: roster → shadow intents → `ExecuteDemoCopyAsync`. |

Claim 1 as written against **`CTraderFixSession.cs`** is true. Tree-wide “no `35=D` builder” is **false** on HEAD. Unscoped claim **FAIL**.

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
| Quality uses **source** `NetPnl` | L66 `trades.Sum(t => t.NetRealizedPnl)` then L153 `if (features.NetPnl > 0) quality += 15`. Source book, not dest. |
| Test | `tests\Unit\BaselineScorerTests.cs` L21–26: three disciplined winners → `SHADOW`; `CanPromoteToLive(...).Should().BeFalse()`. |

There is **no** code path in this file that emits `TraderState.LIVE`. Promotion to live is not a scorer function. Claim 2 **PASS**.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL** (disproved)

This claim is **false on disk**. None of the four assigned files force the flag false. The only post-logon writer of FIX runtime fields does not re-pin it.

### 3.1 Logon host reads the flag and never assigns it

`D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` is the hosted service that calls `CTraderFixSession.TryLogonAsync` (twice: QUOTE + TRADE). After both calls it writes Quote/Trade status and **logs** `RealCopyEnabled`:

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

No `_runtime.RealCopyEnabled = false` (or any assignment) exists in this file. A log line is **not** a pin.

### 3.2 Sole write is DI bind of `.env` `true`

Tree-wide grep of `RealCopyEnabled =` in `*.cs` returned **one** assignment:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

API boot loads `D:\Prop\.env` via `EnvFile.FindAndLoad()` (`apps\api\Program.cs` L10; hard path in `EnvFile.cs` L15). `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true`.

| Check | Measured |
|---|---|
| `.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` |
| DI bind | equality to `"true"` → **armed** |
| Logon re-pin | **absent** |
| Assigned `LiveCopyPage.tsx` | displays `status?.realCopyArmed` (L13). Does **not** write the flag. |
| Assigned `RiskEngine.cs` | consumes `request.RealExecutionEnabled`; does **not** write runtime. Empty L90–93 comment claims shadow “never allows FIX send”; the **actual** gate is L147–150 `allowSend = RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. |
| Live GET `/api/health` `realCopyEnabled` | **not measured** (SSRF). Cannot prove the running process bit. File bind is still `true`. |

Claim 3 **FAIL**. “Forced false after logon” is **disproved**.

---

## 4. Claim 4 — sending now cannot be the profit path — **FAIL** (unscoped) / **PASS_NOT_BOOKED_DEST_PROFIT** (session + persist)

### 4.1 What the assigned files can send

- `CTraderFixSession` outbound is **logon `35=A` only**. Logon is not dest P&L.
- `RiskEngine.Evaluate` can return `AllowFixSend=true` **only if** `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Rejects always set `AllowFixSend=false` (L187).
- `LiveCopyPage.tsx` is **GET-only** UI. No POST. Empty-state text (L57) admits dest auto-send: “Demo dest auto-sends after a trader is ADMITTED…”.

### 4.2 Persist hop cannot book dest profit

`CopyTradingService.GenerateShadowIntentsAsync`:

```20:21:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const decimal AllocationFactor = XauUsdOneToOneCopyPolicy.AllocationFactor;
```

```303:304:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    RealExecutionEnabled = _runtime.RealCopyEnabled,
                    Reconciled = VenueReconciled,
```

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

| Check | Measured |
|---|---|
| `VenueReconciled` | **const `false`** → Evaluate `AllowFixSend` is **always false** on this hop |
| Persist `AllowFixSend` | **hard `false`** L324 (overwrites Evaluate) |
| Live send branch | dead: requires `decision.AllowFixSend && LIVE && NOS && VenueReconciled` — last two cannot be true together (`NOS => DemoDest`, `VenueReconciled` const false) |
| `CanPromoteToLive` | hard false → scorer never emits `LIVE` |

Dashboard dest cash is a **constructor zero**, not a mark:

```33:46:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
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
```

`OverviewDto` field after `ShadowPnl` is `DestinationRealPnl` (`DashboardModels.cs` L16). Literal `0`. That is **not** proof dest has no P&L.

### 4.3 Unscoped “cannot be the profit path” is false: dest send is wired **now**

`CopyTradingHostedService` L21–32: after 8s, every 20s: `TickRosterAsync` → `GenerateShadowIntentsAsync` → `ExecuteDemoCopyAsync`.

`ExecuteDemoCopyAsync` (L483–605):

- Returns 0 only if `!DemoDest` or password blank.
- `DemoDest` (`CopyTradingService` L45–48) is true when host starts with `demo-`, trade sender starts with `demo.`, and account ≠ `1369850`.
- `.env` dest-identity keys (no secrets): `CTRADER_FIX_HOST=demo-us-eqx-01.p.c-trader.com` (L49), `CTRADER_FIX_ACCOUNT_ID=5328266` (L50), `CTRADER_FIX_TRADE_SENDER_COMP_ID=demo.pepperstone.5328266` (L64). **`DemoDest` is true on this lab config.**
- Does **not** call `RiskEngine.Evaluate`.
- Does **not** read `RealCopyEnabled`.
- Does **not** check `CanPromoteToLive`.
- Calls `CTraderFixCopyOpen.SendAsync` (close L528; open L566) which writes `Build("D")`.
- Caps `maxPerTick = 5`. Skips source lots `> MaxAutoLots` (0.05).
- On fill: writes `DemoCopyLedger` + intent `Status = "DEMO_SENT"` + dest id/px.

On-disk ledger `D:\Prop\data\demo_copy_ledger.json` (this slot **read**, not written):

| Field | Value (public dest/source ids only) |
|---|---|
| SourceLogin | `305750` |
| SourcePositionId | `21250421` |
| Lots | `0.01` |
| DestPositionId | `237339770` |
| DestFillPrice | `4390.2` |
| DestClosed | **false** |

`ExecuteDemoCopyAsync` L500–512 also **re-seeds** that same 305750/21250421/237339770 row if missing. That is dest **exposure**, not a booked dest-profit constructor. Dest P&L is **unmeasured** (no mark-to-market of dest fills in `EfDashboardQueries`). This slot cannot prove dest P&L is zero. Therefore the unscoped claim “sending now cannot be the profit path” is **not proven** and is **disproved as a send-absent statement**.

Claim 4 **FAIL** unscoped. **PASS_NOT_BOOKED_DEST_PROFIT** only for: assigned session send + persist `AllowFixSend=false` + dest DTO constructor `0`.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **PASS**

### 5.1 SHADOW is a source state

`FromBaseline` returns `SHADOW` when `quality >= 70 && risk < 40` after ≥3 completed **source** XAU trades (`BaselineScorer.cs` L200–201). `FeatureSnapshot.NetPnl` is `Sum(t.NetRealizedPnl)` of reconstructed source trades (L66, L111). That is source-book PnL.

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` with `TRADER_NOT_SHADOW_YET` (L81–85). SHADOW (or `LIVE_CANDIDATE` / `LIVE`) is the **admit floor**, not dest cash.

### 5.2 Paper shadow ≠ dest fill

`CopyTradingService.GenerateShadowIntentsAsync` hopper is `{SHADOW, LIVE_CANDIDATE, LIVE}` (L202). On non-live-send it writes `SHADOW_ONLY` and `ShadowCopyEngine.SimulateEntry` (`ShadowCopyEngine.cs` L35–61): modeled ask/bid + 0.05 latency slip. No socket.

Dashboard `ShadowPnl` = `Sum(ShadowOrders.SourceVsShadowSlippage)` (`EfDashboardQueries.cs` L29). Slippage sum is **not** dest realized.

`LiveCopyPage.tsx` L14–18 shows `shadowTraders` / `shadowFills` / `liveSends` counts. No dest PnL column.

### 5.3 Residual (does not fail claim 5)

A SHADOW source that is `ADMITTED` (roster `AUTO_ADMIT` via `CopyRosterEngine` + demo/contest group filter) **can** be picked by `ExecuteDemoCopyAsync` and dest-sent. That makes SHADOW a **hopper label**, not dest profit. Dest profit would require a dest mark; dest DTO is constructor `0`; ledger fill is unmarked.

Claim 5 **PASS**. SHADOW-on-demo is source state + paper slippage. It is **not** destination profit.

---

## 6. Assigned UI file (`LiveCopyPage.tsx`) — display only

`D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (**70** lines, full read).

| Check | Measured |
|---|---|
| Data | `useCopyStatus()` + `useCopyIntents()` (GET hooks) |
| Send / promote / flag write | **0** |
| `REAL_COPY armed` | `status?.realCopyArmed ? 'YES' : 'NO'` (L13) — display of DI-bound bit |
| SHADOW | count tile (L14) |
| Blockers copy | “Live send blockers (Pepperstone cannot be filled)” (L24) |
| Empty state | admits demo dest auto-send after ADMITTED (L57) |

UI cannot prove live GET. UI cannot force `RealCopyEnabled` false. UI cannot send `35=D`.

---

## 7. Live GET

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | **SSRF blocked** (loopback) |
| `http://localhost:5000/api/health` | **SSRF blocked** (resolves to 127.0.0.1) |

`apps\api\Properties\launchSettings.json` advertises `http://localhost:5000`. This slot has **no** live body for `realCopyEnabled` / copy status / overview dest PnL. File proof stands. Process bit is **unverified**.

---

## 8. What this slot does **not** claim

- Did not send FIX.
- Did not flip `REAL_COPY_EXECUTION_ENABLED`.
- Did not edit product or test source.
- Did not re-measure sibling census / SHADOW dollar totals. Those integers are **not** evidence here.
- Did not mark dest `237339770` to market. Open dest fill ≠ booked dest profit; also ≠ proof of zero dest P&L.
- Did not attach to a running API (SSRF).

---

## 9. Binding close

| Item | Value |
|---|---|
| Overall | **FAIL** |
| Claim 1 | **FAIL** unscoped; **PASS_SESSION** (`35=A` only in `CTraderFixSession.cs`) |
| Claim 2 | **PASS** (`CanPromoteToLive => false`) |
| Claim 3 | **FAIL** (DI binds `.env` `true`; logon does not re-pin) |
| Claim 4 | **FAIL** unscoped (hosted demo `35=D` now); **PASS_NOT_BOOKED_DEST_PROFIT** on session+persist+dest constructor `0` |
| Claim 5 | **PASS** (SHADOW ≠ dest profit) |
| Risk to live `1369850` | **NONE** (`SAFE_BY_ABSENCE`) |
| Risk to demo dest `5328266` | **Not absent** (hopper wired; ledger open 0.01; this slot sent 0) |
| Secrets | **None printed** |
