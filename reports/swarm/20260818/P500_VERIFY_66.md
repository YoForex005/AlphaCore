# P500_VERIFY_66 — Adversarial verifier (slot 66)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_66.md` |
| Agent / slot | P500 verify **66** (adversarial; siblings are not evidence) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned reads | `CTraderFixSession.cs` (135/135), `BaselineScorer.cs` (213/213), `RiskEngine.cs` (190/190), `LiveCopyPage.tsx` (70/70) |
| Hop files (claims 3–5 only) | `CTraderFixLogonHostedService.cs`, `LiveRuntimeStatus.cs`, `DependencyInjection.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `DealIngestionService.cs`, `EfTradingStore.cs`, `EnvFile.cs`, `apps/api/Program.cs`, `apps/api/appsettings.json`, `apps/api/Properties/launchSettings.json`, `apps/fix-worker/Worker.cs`, `TraderState.cs`, `BaselineScorerTests.cs`, `RiskEngineTests.cs`, `data/demo_copy_ledger.json` (dest ids only), `.env` **boolean key only** |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Quoted only `REAL_COPY_EXECUTION_ENABLED=true`. Tag 554 / passwords / connection strings never dumped. |
| Secrets printed | **None.** |
| Live GET this pass | **Blocked.** `web_fetch` → `SSRF blocked: localhost resolves to private/internal IP 127.0.0.1`. `open_page` `http://127.0.0.1:5000/api/health` → retrieve failed. No shell. Runtime DTO is **FAIL-unproven**, not guessed. |
| Method | Independent full `read_file` of the four assigned files, then hop files required to prove/fail claims 3–5. Grep: `(35,` / `Build("D")` / `RealCopyEnabled =` / `CanPromoteToLive` / `TraderState.LIVE` / `DestinationRealPnl` / `CurrentState =`. |

**Honesty rule:** a comment, log line, dashboard string, or inbound `Extract(..., "35")` is **not** a builder. `35=A` Logon is **not** NewOrderSingle. A sibling `Build("D")` is **not** `CTraderFixSession`. A POCO `{ get; set; }` default is **not** a process pin. An env bind that evaluates `true` **disproves** “forced false after logon.” Demo dest fill is **not** live `1369850` profit and is **not** `DestinationRealPnl`. **FAIL** any assigned claim that cannot be proven from a file or live GET.

---

## 0. Verdict (binding)

**FAIL.**

Claims **1, 2, 4, 5** are file-proven. Claim **3** is **disproven**: `RealCopyEnabled` is **not** forced false after logon.

| # | Claim | File proof | Result |
|---|---|---|---|
| 1 | no `35=D` builder | `CTraderFixSession.cs` 135/135. Only outbound MsgType is `(35, "A")` at L96 inside `BuildLogon`. One `WriteAsync` (L49) of that logon. Grep `Build("D")` / `(35, "D")` / `35=D` in that file = **0**. | **PASS** |
| 2 | `CanPromoteToLive` is false | `BaselineScorer.cs` L211: `public static bool CanPromoteToLive(TraderState current) => false;` Parameter unread. `FromBaseline` never returns `LIVE` / `LIVE_CANDIDATE`. Unit test L26 asserts false. | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **Disproven.** Only assignment is DI L41 from env. `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true`. Logon host L60–70 writes Quote/Trade only; **never** assigns `RealCopyEnabled`. Grep `RealCopyEnabled =` in product `*.cs` = **1** hit (DI). Live GET blocked — not needed; the force-false assignment does not exist. | **FAIL** |
| 4 | sending now cannot be the profit path | Counted dest profit is constructor `0` (`EfDashboardQueries` L44 `DestinationRealPnl`). Live `1369850` refused. Persist `AllowFixSend=false`. `VenueReconciled=false` const. `CanPromoteToLive=>false`. Session hop has no `35=D`. Residual: hosted demo dest **can** `Build("D")` — dest **execution**, not dest **profit accounting**. | **PASS** |
| 5 | SHADOW on demo is not destination profit | `ShadowCopyEngine.SimulateEntry` is paper. Persist status `SHADOW_ONLY`. Overview `ShadowPnl` = sum of `SourceVsShadowSlippage`; `DestinationRealPnl` literal `0`. Residual: `ADMITTED` SHADOW can dest-send; dest send ≠ dest PnL DTO. | **PASS** |

One-line:

```text
FAIL. CTraderFixSession 35=A only. CanPromoteToLive=>false. RealCopyEnabled NOT forced false after logon (.env true + DI bind + no re-pin). Counted dest PnL is constructor 0; live 1369850 refused; SHADOW is paper. Demo dest hop can still 35=D. Risk to live capital NONE.
```

---

## 1. No 35=D builder — PASS (`CTraderFixSession.cs`)

Assigned file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135, full read).

The class is logon-only. The only outbound constructor is `BuildLogon`:

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

Proven from this file:

- `TryLogonAsync` L47–49: one `WriteAsync` of that logon, then `ReadAsync`, then parse inbound tag `35`.
- Inbound `Extract(reply, "35")` (L55) is **not** a builder. Comparing `msgType == "A"` (L56) is inbound classify.
- Grep of this file: `NewOrderSingle` = 0, `Build("D")` = 0, `(35, "D")` = 0, literal `35=D` = 0.
- Grep `(35,` in this file = **1** hit (L96 `"A"`).
- The other three assigned files also contain no FIX `35=D` constructor: `BaselineScorer.cs` is scoring; `RiskEngine.cs` sets `AllowFixSend` boolean only; `LiveCopyPage.tsx` is React display.

**Residual (does not fail claim 1):** siblings **do** build `35=D`. Claim 1 is scoped to `CTraderFixSession`. They **do** matter for claim 4 residual.

| File | Hits |
|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 | `Build("D", …)` — **hosted** by `CopyTradingService.ExecuteDemoCopyAsync` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | `Build("D", …)` — tools/CLI, demo-gated, refuses `1369850` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` L93 | `Build("D", …)` — demo matrix helper |

`CTraderFixCopyOpen.Build` (L142–156) is a generic `Build(string type, …)` that emits `(35, type)`. Call site L95 passes `"D"`. That is a NewOrderSingle builder. It is **not** `CTraderFixSession`.

---

## 2. `CanPromoteToLive` is false — PASS (`BaselineScorer.cs`)

Assigned file `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (213/213).

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

Proven:

- Parameter `current` is **unread**. Every `TraderState` returns false.
- `FromBaseline` (L189–207) can emit `INSUFFICIENT_DATA`, `RISK_BLOCKED`, `SHADOW`, `WATCH`, `EARLY_SCORE`. It never emits `LIVE` or `LIVE_CANDIDATE` (enum values exist at `TraderState.cs` L9–10 but have no scorer writer).
- Ceiling path: `quality >= 70 && risk < 40` → `SHADOW` (L200–201). `AfterHighEarlyScore()` is also `SHADOW` (L209).
- Ingest writes `CurrentState = score.SuggestedState` (`DealIngestionService` L140). Store update copies that field (`EfTradingStore` L232). Grep `CurrentState =` in product `*.cs` = those two writers only. No product writer assigns `TraderState.LIVE`.
- Unit `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` L22–26: suggested state `SHADOW`; `CanPromoteToLive(...).Should().BeFalse()`.

`LiveCopyPage.tsx` L15 displays `liveTraders` from API; that count is `scores.Count(s => s.CurrentState == LIVE)` (`CopyTradingService` L58). The scorer cannot produce that state. Display of a zero count is not a promotion path.

---

## 3. `RealCopyEnabled` forced false after logon — FAIL

**Cannot prove. Disproven as process state.**

### 3.1 Only assignment is env-true

Grep `RealCopyEnabled =` across product `*.cs` = **one** hit:

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`.env` L73 (boolean only): `REAL_COPY_EXECUTION_ENABLED=true`.

`apps/api/Program.cs` L10 calls `EnvFile.FindAndLoad()`. `EnvFile.cs` L14 includes the hard path `D:\Prop\.env` and L38 `Environment.SetEnvironmentVariable(key, value)`. `Program.cs` L13 then `AddEnvironmentVariables()`. After that bind, `string.Equals(..., "true", OrdinalIgnoreCase)` is **true**. The process starts with `RealCopyEnabled == true`.

`LiveRuntimeStatus.RealCopyEnabled` is `{ get; set; }` (`LiveRuntimeStatus.cs` L32) with no field initializer `= false` that would re-pin after construction. Default `bool` is false **until** the DI initializer runs.

### 3.2 Logon does not re-pin the flag

`CTraderFixLogonHostedService.ExecuteAsync` after both `TryLogonAsync` calls:

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

Reads `_runtime.RealCopyEnabled` for a log line. **Does not assign it.** Quote/Trade status only. A log string saying “NewOrderSingle still unimplemented” is not a force-false.

### 3.3 Assigned files do not force it false either

| File | `RealCopyEnabled` / `RealExecutionEnabled` |
|---|---|
| `CTraderFixSession.cs` | absent |
| `BaselineScorer.cs` | absent |
| `RiskEngine.cs` | **input** `request.RealExecutionEnabled`; empty comment-block at L90–93; `AllowFixSend` later `&& request.RealExecutionEnabled` (L147) |
| `LiveCopyPage.tsx` | **display** `status?.realCopyArmed ? 'YES' : 'NO'` (L13) |

`appsettings.json` `FeatureFlags:LiveCopyEnabled = false` is a **different key**. Grep shows it is only read by `SettingsController` Redis settings, not by `LiveRuntimeStatus`.

`apps/fix-worker/Worker.cs` L21 reads yet another key (`CTrader:RealCopyExecutionEnabled`, default false) and still does not write `LiveRuntimeStatus.RealCopyEnabled`.

### 3.4 Live GET

Need `/api/health` (`realCopyEnabled`) or `/api/settings` (`featureFlags.REAL_COPY_EXECUTION_ENABLED`) or `/api/copy/status` (`realCopyArmed`) or `/api/overview`. Launch profile binds `http://localhost:5000`. Both fetch tools refused loopback. **Runtime DTO unproven.** File proof already falsifies the claim: there is no post-logon assignment to false, and the only assignment is env-true.

**Claim 3 FAIL.**

---

## 4. Sending now cannot be the profit path — PASS

Scoped meaning that can be proven: **sending is not the counted destination-profit path, and live `1369850` cannot be filled.** Unscoped “no dest fill can ever P/L” is **false** (demo dest hop) and is recorded as residual, not as the claim.

### 4.1 Session hop cannot send a ticket

`CTraderFixSession` writes only `35=A` (claim 1). That hop cannot be a profit path.

### 4.2 Risk-gated live send is dead

`RiskEngine.Evaluate` L147–150:

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

`CopyTradingService`:

- `VenueReconciled = false` **const** (L20).
- `GenerateShadowIntentsAsync` passes `Reconciled = VenueReconciled` (L304) → `AllowFixSend` is false even if `RealCopyEnabled` is true.
- Persist then **overwrites** `AllowFixSend = false` (L324) regardless of the engine result.
- Live branch (L330) requires `decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled`. Fourth conjunct is const false. Branch body only sets status `LIVE_SEND_BLOCKED_UNIMPLEMENTED` — it does **not** call `CTraderFixCopyOpen`.
- `CanPromoteToLive => false` and `FromBaseline` never emits `LIVE`, so the `CurrentState == LIVE` conjunct is also dead.

Unit `RiskEngineTests.Real_flag_false_never_allows_fix_send` (L21–26) asserts `AllowFixSend` false when `RealExecutionEnabled = false`. That test does **not** pin the runtime flag (claim 3).

### 4.3 Counted dest profit is constructor 0

`OverviewDto` field `DestinationRealPnl` (`DashboardModels.cs` L16) is positional arg 11. `EfDashboardQueries.GetOverviewAsync` L33–52:

```
shadowPnl,  // ShadowPnl
0,          // DestinationRealPnl
0,          // XauGross
0,          // XauNet
```

No other writer of `DestinationRealPnl` exists (grep). Sends do not update that DTO.

### 4.4 Live account refused

`CTraderFixCopyOpen.SendAsync` L37–42: if host is not `demo-` **or** sender is not `demo.` **or** `account == "1369850"` → refuse, `OrderSent = false`, `Filled = false`. Live Pepperstone cannot be the dest of this builder.

### 4.5 UI does not send

`LiveCopyPage.tsx` (70/70): GET hooks only (`useCopyStatus`, `useCopyIntents`). No POST. Copy: “Live send blockers (Pepperstone cannot be filled)” (L24). Empty-state copy: “Demo dest auto-sends after a trader is ADMITTED…” (L57). Display of a send is not a profit-path builder.

### 4.6 Residual — dest **execution**, not dest **profit accounting**

`CopyTradingHostedService` L28–30 every 20s: `TickRosterAsync` → `GenerateShadowIntentsAsync` → `ExecuteDemoCopyAsync`.

`ExecuteDemoCopyAsync` (L483–605) does **not** consult `RealCopyEnabled`, `AllowFixSend`, or `CanPromoteToLive`. Gate is `DemoDest` (host `demo-` + sender `demo.` + account ≠ `1369850`). It calls `CTraderFixCopyOpen.SendAsync` which `Build("D")`. Ledger `D:\Prop\data\demo_copy_ledger.json` already records dest position `237339770` / clOrd `C20260818093047317` / px `4390.2` / `DestClosed: false` for source `305750`/`21250421`. That is a **demo dest fill**, not `DestinationRealPnl`, not live `1369850`.

`GetStatusAsync` L76–77: when `DemoDest`, summary is “Demo dest auto-copy ON… Live 1369850 is never used.”

Claim 4 **PASS** as counted/live profit path. Residual dest execution remains.

---

## 5. SHADOW on demo is not destination profit — PASS

### 5.1 Scorer SHADOW is a trader state, not dest PnL

`FromBaseline` can return `SHADOW`. That is eligibility, not a fill.

### 5.2 Paper engine

`ShadowCopyEngine.SimulateEntry` / `SimulateExit` compute a `ShadowFill` from quote bid/ask + modeled 0.05 pt delay. No socket. No tag 35.

`PersistDemoShadowAsync` (`EfTradingStore` L267–271): if `state != SHADOW`, return. Else simulate against last `DestinationQuotes` row. No FIX send.

`GenerateShadowIntentsAsync` else-branch (L336–360): `intent.Status = "SHADOW_ONLY"` then `_shadow.SimulateEntry(...)` into `ShadowOrders`.

### 5.3 Dashboard SHADOW PnL is slippage, not dest profit

`EfDashboardQueries` L29: `shadowPnl = Sum(SourceVsShadowSlippage)`. That is the `ShadowPnl` field. `DestinationRealPnl` is the next arg and is literal `0`.

### 5.4 Policy: SHADOW can be admitted; admit ≠ dest profit DTO

`XauUsdOneToOneCopyPolicy.IsTraderEligible` **rejects** `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` (`TRADER_NOT_SHADOW_YET`) and blocked states. It **accepts** `SHADOW` (and `LIVE_CANDIDATE` / `LIVE` if those ever existed) when N≥20, XAU book > 0, demo/contest group, no size pattern.

`CopyRosterEngine.Decide` can `Admit` that eligible snapshot. `ExecuteDemoCopyAsync` then dest-sends **ADMITTED** seats. That is dest **execution** on demo (claim 4 residual). It does **not** write `DestinationRealPnl`. Shadow paper rows remain paper.

`LiveCopyPage` L14–18 shows `SHADOW traders` and `Shadow fills` as counts. Counts are not dest profit.

Claim 5 **PASS**. Residual: SHADOW→ADMITTED can cause demo `35=D`. That residual is dest execution, not dest profit accounting.

---

## 6. Live GET appendix

| Probe | Result |
|---|---|
| `web_fetch` `http://localhost:5000/api/health` | SSRF blocked (localhost → 127.0.0.1) |
| `open_page` `http://127.0.0.1:5000/api/health` | Failed to retrieve page content |
| `/api/copy/status`, `/api/overview`, `/api/settings` | Not fetched (same loopback) |

No live DTO is cited. Claim 3 does not need one: the force-false write is absent and the only write is env-true.

---

## 7. Risk to capital

| Path | Risk |
|---|---|
| `CTraderFixSession` (QUOTE 5211 / TRADE 5212 logon) | **None.** `35=A` only. |
| Risk-gated `GenerateShadowIntentsAsync` live send | **None.** `VenueReconciled=false`, persist `AllowFixSend=false`, no `LIVE` state, branch does not send. |
| Live Pepperstone `1369850` | **None.** `CTraderFixCopyOpen` + `DemoDest` refuse. |
| Counted `DestinationRealPnl` | **None / always 0.** |
| Demo dest auto-copy (`ExecuteDemoCopyAsync` → `Build("D")`) | **Residual demo-account execution** (public dest account default `5328266`; ledger dest pos `237339770`). Not live capital. Not `DestinationRealPnl`. |

**Risk to live capital: NONE.** Residual risk is demo dest fills only.

---

End of P500_VERIFY_66. Product source was not modified. No secrets printed. This slot did not send `35=D`. `REAL_COPY` was not flipped by this slot.
