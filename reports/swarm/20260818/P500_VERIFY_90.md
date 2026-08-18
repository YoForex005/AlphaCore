# P500_VERIFY_90 — Adversarial confirm (slot 90)

| Field | Value |
|---|---|
| Slot | **90** |
| Date | 2026-08-18 |
| Agent | P500_VERIFY_90 (adversarial; re-read HEAD files; did **not** trust sibling `P500_*` / `W500_*` integers or verdicts) |
| Assigned | Read `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`. Confirm: (1) no `35=D` builder; (2) `CanPromoteToLive` is false; (3) `RealCopyEnabled` forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. **FAIL any claim that cannot be proved from a file or live GET.** Never print secrets. |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **None.** Password / proxy / FIX password values were not quoted. Only the already-on-disk boolean `REAL_COPY_EXECUTION_ENABLED=true` is named. Public dest ids `5328266` / `1369850` appear in product source. |
| Live GET this slot | **Attempted and blocked.** `web_fetch` `http://127.0.0.1:5000/api/health`, `http://localhost:5000/api/copy/status`, `http://127.0.0.1:18720/api/health` → `SSRF blocked` (loopback). `open_page` `http://127.0.0.1:5000/api/health` → retrieve fail. **No live JSON.** File proof only. Runtime `realCopyEnabled` bit is **unmeasured**. |
| Live `35=D` sent this slot | **No.** This slot sent **0**. |
| `REAL_COPY` flipped this slot | **No.** |
| SHA-256 this slot | **Not computed** (no shell). Line counts from full `read_file`. |
| Method | Full `read_file` of the four assigned files (135 / 212 / 189 / 70 lines). Then logon host, DI bind, copy hopper, sibling session builders, dest-PnL constructor, shadow engine, roster/policy, persist-shadow, hooks, `.env` L73 **flag key only**, ledger file as dest-activity residual (not venue cash). Targeted `grep` for tag `35` in the session file, `RealCopyEnabled\s*=`, `CanPromoteToLive`, `DestinationRealPnl`, `Build("D")`. |
| Honesty rule | Sibling swarm PnL / census integers are **not** evidence. A comment is **not** a runtime pin. A dashboard constructor `0` is **not** dest cash. Live GET that did not return JSON is **not** a measured `realCopyEnabled` value. This slot sent **0**. |

---

## 0. Verdict (binding)

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS_SESSION / FAIL_UNSCOPED** | Assigned `CTraderFixSession.cs` **135/135**: only outbound MsgType is `(35, "A")` at L96. Same-folder product files **do** have `Build("D", …)` (`CTraderFixCopyOpen` L95; `CTraderFixDemoTestTrade` L139/163/197; `CTraderFixDemoMatrix` L93). Unscoped “no builder” is **false**. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(TraderState current) => false` at `BaselineScorer.cs` L211. Argument unused. `FromBaseline` reachable set is `{INSUFFICIENT_DATA, RISK_BLOCKED, SHADOW, WATCH, EARLY_SCORE}` — never `LIVE` / `LIVE_CANDIDATE`. Persist copies `SuggestedState`. Unit test asserts SHADOW ≠ LIVE. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | **Disproved from files.** Logon host **reads** `_runtime.RealCopyEnabled` and **never assigns** `false`. Sole `RealCopyEnabled =` write in `src/` is DI bind of config `REAL_COPY_EXECUTION_ENABLED`. `.env` L73 is `true`. Live GET of the echo endpoints did not return JSON, so the **process bit** is unmeasured; the **force-false-after-logon** sentence is still false on HEAD. |
| 4 | sending now cannot be the profit path | **FAIL** (unscoped) / **PASS_NOT_BOOKED_DEST_PROFIT** (session+persist+DTO) | Assigned session cannot send a ticket. Persist `AllowFixSend=false`. `CanPromoteToLive` is hard-false. `CopyTradingService.VenueReconciled` is `const false`. `DestinationRealPnl` is a literal `0` in `OverviewDto` construction. **Unscoped claim is false:** hosted `ExecuteDemoCopyAsync` → sibling `Build("D")` on demo dest is dest **activity** that can move demo venue cash. Live GET of dest PnL **not obtained**. Cannot prove “sending cannot be dest profit.” |
| 5 | SHADOW on demo is not destination profit | **PASS_PAPER / FAIL_AS_DEST_CLASS** | Paper path: `SHADOW` is a **source-trader state**. Dashboard `ShadowPnl` is `Sum(SourceVsShadowSlippage)`. `DestinationRealPnl` is constructor `0`. `LiveCopyPage` shows SHADOW **counts**, no dest cash. `ShadowCopyEngine.SimulateEntry` is not a venue fill. Dest-class residual: `IsTraderEligible` treats SHADOW-or-better as the dest **AUTO_ADMIT floor**, and the 20s hopper then `35=D`s ADMITTED seats **without** requiring `LIVE`. |

**Slot verdict: `FAIL`.**

Claim 3 is **disproved**, not merely unproved. Claim 1 as written without a file scope is also **false** on the product tree. Claim 4 as written without a booked-DTO scope is **false** on the hosted demo hop. Claim 5 as dest-safety is **not** file-proven. One FAIL is enough.

**Risk to capital:** **NONE on live `1369850`** (`SAFE_BY_ABSENCE` for `CTraderFixSession` + `CTraderFixCopyOpen` / `CTraderFixDemoTestTrade` / `CTraderFixDemoMatrix` refuse of that account). **Not absent on demo dest** if `DemoDest` is true: hosted `ExecuteDemoCopyAsync` can emit `35=D` **without** `RiskEngine.Evaluate` and **without** reading `RealCopyEnabled`. This slot sent **0**.

---

## 1. Claim 1 — no `35=D` builder — **PASS_SESSION / FAIL_UNSCOPED**

Assigned file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135** lines, full read).

Grep of tag `35` in this file = **3 hits**, all logon:

| Line | Role |
|---|---|
| L55 | inbound parse `Extract(reply, "35")` |
| L73 | error string `Logon rejected 35={msgType}` |
| L96 | outbound `(35, "A")` |

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

Adversarial residual (**fails the unscoped wording**): sibling product files **do** have a `35=D` builder. This slot **read** them.

| File | What this slot measured |
|---|---|
| `src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra)` after TRADE logon + SecurityList. **Refuses** non-`demo-` host, non-`demo.` sender, or account `1369850` (L37–42). Hosted by `CopyTradingService.ExecuteDemoCopyAsync`. |
| `src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | `Build("D", …)` at L139 / L163 / L197 (demo test helper; refuses live `1369850`). |
| `src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | `Build("D", …)` at L93 (`SendD` helper). |

Product `Build("D")` count this slot: **5** (grep `Build("D"` under `*.cs`). Claim 1 as written against **`CTraderFixSession.cs`** is true. Tree-wide “no `35=D` builder” is **false** on HEAD. This slot **FAIL**s the unscoped sentence.

`RiskEngine.cs` and `LiveCopyPage.tsx` contain **zero** FIX builders. They cannot rescue an unscoped “no builder” claim.

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
| `CanPromoteToLive` body | `=> false` (argument unused) |
| `FromBaseline` returns `LIVE` | **never** |
| `FromBaseline` returns `LIVE_CANDIDATE` | **never** |
| Best reachable state | `SHADOW` (quality ≥ 70 and risk < 40) |
| `AfterHighEarlyScore()` | `SHADOW` |
| Persist | `ReconstructionScoringService.RebuildTraderAsync` sets `CurrentState = score.SuggestedState`. `EfTradingStore.UpsertScoreAsync` copies `score.CurrentState`. |
| Unit | `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` asserts `SuggestedState == SHADOW` and `CanPromoteToLive(...) == false`. |

`TraderState.LIVE = 5` exists as an enum member (`TraderState.cs` L10). Existence of the enum is **not** promotion. No product writer this slot found that assigns `CurrentState = TraderState.LIVE` independently of the scorer. Claim 2 is **file-proven**.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL (disproved)**

None of the four assigned files force the flag false.

| Assigned file | `RealCopyEnabled` |
|---|---|
| `CTraderFixSession.cs` | **absent** |
| `BaselineScorer.cs` | **absent** |
| `RiskEngine.cs` | reads `request.RealExecutionEnabled`; does **not** write the runtime flag. Comment at L91–93 is a no-op; `AllowFixSend` later is `request.RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). |
| `LiveCopyPage.tsx` | **displays** `status?.realCopyArmed` as YES/NO. Display is not a pin. |

Logon hosted service **after** both `TryLogonAsync` calls (the only “after logon” site):

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

Measured: **read** of `_runtime.RealCopyEnabled` for a log line. **Zero** assignments of `RealCopyEnabled` in this file. Quote/Trade status fields are written; the copy-arm bit is not.

Sole `RealCopyEnabled =` write under product `src/`:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`.env` L73 (flag key/value only; no secrets): `REAL_COPY_EXECUTION_ENABLED=true`.

POCO default remains off (`CTraderFixOptions.RealCopyExecutionEnabled` default `false`) and `appsettings.json` `FeatureFlags.LiveCopyEnabled` is `false`. Those are **not** the runtime bit. `apps\api\Program.cs` echoes `runtime.RealCopyEnabled` on `/api/health` and `/api/settings` — echo, not a force-false.

Live GET of those echo endpoints **did not return JSON** this slot. Therefore:

- “forced false after logon” is **disproved** by the logon host + DI bind.
- “runtime is currently false” is **unproven** (no live GET). Unproven ≠ forced false.

Claim 3 **FAIL**.

---

## 4. Claim 4 — sending now cannot be the profit path — **FAIL unscoped / PASS_NOT_BOOKED_DEST_PROFIT**

### 4a. What the assigned files prove

`CTraderFixSession` cannot place an order (claim 1 scoped). `CanPromoteToLive` cannot mint `LIVE` (claim 2). `RiskEngine` can set `AllowFixSend=true` **only if** `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. `LiveCopyPage` does not send; it lists intents and a “Live send blockers (Pepperstone cannot be filled)” banner.

`CopyTradingService` (not assigned, required to interpret “sending now”):

```20:21:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const decimal AllocationFactor = XauUsdOneToOneCopyPolicy.AllocationFactor;
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

Even if `decision.AllowFixSend` were true, `VenueReconciled` is `const false`, `CurrentState` cannot be `LIVE` from the scorer, and the true branch **still does not write a FIX frame** — it sets a blocked status. Persist forces `AllowFixSend = false`.

`OverviewDto.DestinationRealPnl` is constructed as literal `0` (`EfDashboardQueries.GetOverviewAsync` L44–52). A constructor `0` is **not** a measured dest mark.

So: **booked destination profit via the live-copy persist path is not implemented.** Scoped as “session + persist + DTO cannot book dest profit,” this is **PASS_NOT_BOOKED_DEST_PROFIT**.

### 4b. Why the unscoped sentence FAILs

`CopyTradingHostedService` every 20s:

```27:30:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
                var copy = scope.ServiceProvider.GetRequiredService<CopyTradingService>();
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

`ExecuteDemoCopyAsync` (`CopyTradingService.cs` L483–605):

- returns 0 only if `!DemoDest` (`host` starts with `demo-` **and** sender starts with `demo.` **and** account ≠ `1369850`) or password empty;
- **does not read** `RealCopyEnabled`;
- **does not call** `RiskEngine.Evaluate`;
- **does call** `CTraderFixCopyOpen.SendAsync` (sibling `Build("D")`) for ADMITTED roster opens (L566–569) and dest closes (L528–530);
- writes `intent.Status = "DEMO_SENT"` on fill;
- persists `D:\Prop\data\demo_copy_ledger.json`.

Ledger this slot (public dest ids only):

```1:11:D:\Prop\data\demo_copy_ledger.json
[
  {
    "SourceLogin": "305750",
    "SourcePositionId": "21250421",
    "IsLong": true,
    "Lots": 0.01,
    "DestPositionId": "237339770",
    "DestClOrdId": "C20260818093047317",
    "DestFillPrice": 4390.2,
    "DestClosed": false
  }
]
```

That is dest **activity** (open dest id, fill price, `DestClosed: false`). It is **not** a booked `DestinationRealPnl`. `ExecuteDemoCopyAsync` also **seeds** this `305750`/`21250421` row if missing (L500–512) — so the file is a residual hopper artifact, not proof of this slot’s send.

`LiveCopyPage.tsx` L57 empty-state text: “Demo dest auto-sends after a trader is ADMITTED…”. The UI **admits** a send path. That sentence is not dest cash, but it is evidence the product does not treat send as absent.

Unscoped “sending now cannot be the profit path” is **false** on demo dest. Live dest `1369850` remains refused. Live GET of dest PnL **not obtained**. Claim 4 **FAIL** as written.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **PASS_PAPER / FAIL_AS_DEST_CLASS**

### 5a. Paper SHADOW is not dest cash — **PASS**

| Source | What SHADOW is |
|---|---|
| `TraderState.SHADOW = 3` | Source-trader state enum |
| `FromBaseline` | Best scorer output (quality ≥ 70, risk < 40) |
| `ShadowCopyEngine.SimulateEntry` | In-process fill using a quote row + modeled 0.05-point latency slip. **No socket.** |
| `EfTradingStore.PersistDemoShadowAsync` | Writes `CopyIntent` `Status = "SHADOW_ONLY"` + `ShadowOrder` only when `state == SHADOW` and a quote row exists |
| `EfDashboardQueries` L29 | `shadowPnl = Sum(SourceVsShadowSlippage)` |
| `OverviewDto` L15–16 | `ShadowPnl` vs `DestinationRealPnl` (latter constructor `0`) |
| `LiveCopyPage.tsx` L14 | Stat **“SHADOW traders”** = `status?.shadowTraders` count. No dest PnL column. |
| `GenerateShadowIntentsAsync` | Paper `ShadowOrders` on approve; persist `AllowFixSend=false` |

Slippage sum ≠ dest cash. SimulateEntry ≠ venue fill. Claim 5 as “paper SHADOW P&L is not destination profit” is **file-proven**.

### 5b. SHADOW as dest class — **not proven safe**

`XauUsdOneToOneCopyPolicy.IsTraderEligible` **rejects** `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` (`TRADER_NOT_SHADOW_YET`) and **accepts** SHADOW (and LIVE_CANDIDATE / LIVE) if the other gates pass (20 completed XAU, book net > 0, demo/contest group, no size pattern).

`CopyRosterEngine.Decide` AUTO_ADMITs when `IsTraderEligible` is true.

`ExecuteDemoCopyAsync` then `35=D`s **ADMITTED** seats. It does **not** require `TraderState.LIVE`. It does **not** require `RealCopyEnabled`.

So SHADOW-or-better is the dest **admission floor**. “SHADOW on demo is not destination profit” is true for the **paper ledger** and **false as dest-safety**. This slot **FAIL**s the dest-class reading because it cannot be proved from the assigned files that SHADOW cannot cause dest fills.

`RiskEngine` does not convert SHADOW into dest cash. `LiveCopyPage` does not book dest cash. Those facts do not erase the hopper.

---

## 6. Live GET

Launch profile `apps\api\Properties\launchSettings.json` binds `http://localhost:5000` (also IIS Express `:18720`). Hooks: `GET /api/copy/status`, `/api/copy/intents`, `/api/health`, `/api/settings`, `/api/overview`.

This slot:

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | `web_fetch` SSRF blocked; `open_page` retrieve fail |
| `http://localhost:5000/api/copy/status` | `web_fetch` SSRF blocked |
| `http://127.0.0.1:18720/api/health` | `web_fetch` SSRF blocked |

**No live body.** `realCopyArmed` / dest PnL / liveSends **unmeasured**. File proof only. A missing GET is not a PASS.

---

## 7. What this slot will not claim

- That EX5 / Quantum Queen work happened. Out of scope.
- That dest demo cash is a measured dollar amount. Ledger price `4390.2` is a residual fill field, not a closed PnL.
- That `DestinationRealPnl == 0` means dest cash is zero. It is a constructor.
- That `.env` `true` means the running process is armed. DI would bind it **if** the process loaded that file; this slot did not see the process.
- That copy-all of any census would be profitable. Wanting profit is not an edge. `RISK_BLOCKED` remains a scorer output.

---

## 8. Files this slot actually read

| Path | Role |
|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | Assigned. 135 lines. `(35, "A")` only. |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | Assigned. 212 lines. `CanPromoteToLive => false`. |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | Assigned. 189 lines. `AllowFixSend` gated; no force-false of runtime. |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | Assigned. 70 lines. Status display + demo-dest empty-state. |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | Sibling `Build("D")` + live-account refuse. |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | Sibling `Build("D")` ×3. |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | Sibling `SendD` / `Build("D")`. |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | After-logon: **no** `RealCopyEnabled = false`. |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Sole `RealCopyEnabled =` bind. |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Runtime bit + snapshot note. |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | Persist `AllowFixSend=false`; demo hopper. |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 20s tick → `ExecuteDemoCopyAsync`. |
| `D:\Prop\src\Infrastructure\Copy\DemoCopyLedger.cs` | Ledger path. |
| `D:\Prop\data\demo_copy_ledger.json` | Residual dest open `237339770`. |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | Paper simulate. |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | SHADOW-or-better eligibility. |
| `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs` | AUTO_ADMIT. |
| `D:\Prop\src\Domain\Copy\CopyLifecycle.cs` | dest open/close predicates. |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | Persist score + paper shadow. |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | `ShadowPnl` sum; dest ctor `0`. |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | `DestinationRealPnl` field. |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `CurrentState = SuggestedState`. |
| `D:\Prop\src\Domain\Enums\TraderState.cs` | Enum includes `LIVE`. |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | SHADOW ≠ LIVE. |
| `D:\Prop\apps\api\Program.cs` | Echo endpoints. |
| `D:\Prop\apps\api\appsettings.json` | `LiveCopyEnabled: false` (not the runtime bit). |
| `D:\Prop\apps\api\Properties\launchSettings.json` | `:5000` / `:18720`. |
| `D:\Prop\apps\web\src\api\hooks.ts` | Copy status/intents GETs. |
| `D:\Prop\apps\web\src\pages\ShadowPortfolioPage.tsx` | UI copy: NewOrderSingle disabled. |
| `D:\Prop\apps\fix-worker\Worker.cs` | Worker does not send; logs flag. |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default off. |
| `D:\Prop\.env` L73 | Flag key `REAL_COPY_EXECUTION_ENABLED=true` only. |

---

## 9. Slot close

| Item | Value |
|---|---|
| Verdict | **FAIL** |
| Claim 1 | PASS_SESSION / FAIL_UNSCOPED |
| Claim 2 | PASS |
| Claim 3 | FAIL (disproved) |
| Claim 4 | FAIL unscoped / PASS_NOT_BOOKED_DEST_PROFIT |
| Claim 5 | PASS_PAPER / FAIL_AS_DEST_CLASS |
| Live GET | blocked; no JSON |
| This slot sent | **0** |
| Risk to capital | **NONE** on live `1369850`; demo dest hop **not** `SAFE_BY_ABSENCE` |
| Product edits | **None** |
