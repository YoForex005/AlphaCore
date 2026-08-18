# P500_VERIFY_64 — Adversarial four-file verify (slot 64)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_64.md` |
| Agent / slot | P500 verify **64** (adversarial; sibling `P500_*` / `W500_*` text is **not** evidence) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned files (full `read_file`) | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135/135**); `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (**213/213**); `D:\Prop\src\Domain\Risk\RiskEngine.cs` (**189/189**); `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (**70/70**) |
| Hop files (claims 3–5 only) | `CTraderFixLogonHostedService.cs` (112); `LiveRuntimeStatus.cs` (67); `DependencyInjection.cs` L39–42; `CopyTradingService.cs` (625); `CopyTradingHostedService.cs` (44); `CTraderFixCopyOpen.cs` (223); `CTraderFixDemoTestTrade.cs` / `CTraderFixDemoMatrix.cs` (sibling encoders); `ShadowCopyEngine.cs`; `XauUsdOneToOneCopyPolicy.cs`; `CopyRosterEngine.cs`; `EfDashboardQueries.cs` L21–52 + L106–118; `DashboardModels.cs`; `DealIngestionService.cs` (`ReconstructionScoringService` L140); `EfTradingStore.cs` `UpsertScoreAsync` / `PersistDemoShadowAsync`; `apps/api/Program.cs` `/api/copy/status` + `/api/settings` + `/api/health`; `.env` **boolean + public dest ids only**; `data/demo_copy_ledger.json` dest ids only |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Quoted only `REAL_COPY_EXECUTION_ENABLED=true`, `FEATURE_COPY_TRADING_ENABLED=true`, public dest `5328266` / `1369850`, public host prefix `demo-`, public sender prefix `demo.`. |
| Secrets printed | **None.** Tag 554 / FIX password / Manager / DB strings not dumped. |
| Live attach / send this pass | **No.** No Manager Connect. No TLS. No Logon. No `35=D` emitted by this slot. |
| Live GET this pass | **Blocked.** `web_fetch` `http://127.0.0.1:5000/api/health` and `http://localhost:5000/api/copy/status` both refused (`SSRF blocked: … → 127.0.0.1`). No shell. Process bits (`realCopyEnabled`, quote/trade LoggedOn, `liveSends`, dest PnL DTO) are **FAIL-unproven**, not guessed. |
| SHA-256 this slot | **Not computed** (no shell). Evidence is line-cited file text. |

**Honesty rule:** a comment, log line, dashboard string, or `LastError` is **not** a builder. `35=A` Logon is **not** NewOrderSingle. Sibling `Build("D")` is **not** `CTraderFixSession`. A POCO default `= false` is **not** a process pin. An env bind that evaluates `true` **disproves** “forced false after logon.” Demo dest fill is **not** live `1369850` profit and is **not** `DestinationRealPnl`. SHADOW is a score state, not dest money. **FAIL** any assigned claim that cannot be proved from a file or a live GET **this slot**.

Claims to confirm:

1. no `35=D` builder
2. `CanPromoteToLive` is false
3. `RealCopyEnabled` forced false after logon
4. sending now cannot be the profit path
5. SHADOW on demo is not destination profit

---

## 0. Verdict (binding)

**FAIL.**

Claims **1 (scoped to `CTraderFixSession.cs`), 2, 4 (live-capital / persist hop / dest-PnL DTO), 5** are file-proven. Claim **3 is disproved** on disk: hosted logon does **not** assign `RealCopyEnabled = false`. Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true` and DI binds it. Live GET of the process bit **failed** this slot — that does **not** rescue claim 3, because the claimed force-false writer is absent.

One failed required confirmation ⇒ slot verdict **FAIL**.

| # | Claim | File proof | Result |
|---|---|---|---|
| 1 | no `35=D` builder | Assigned `CTraderFixSession.cs` 135/135. Only outbound MsgType is `(35, "A")` at L96 inside `BuildLogon`. One `WriteAsync` (L49). Grep this file: `Build("D")` / `(35, "D")` / `35=D` / `NewOrderSingle` = **0**. | **PASS** (file-scoped). **FAIL** if read product-wide (sibling `Build("D")` ×5 + hosted hopper). |
| 2 | `CanPromoteToLive` is false | `BaselineScorer.cs` L211: `public static bool CanPromoteToLive(TraderState current) => false;` Unconditional. `FromBaseline` never returns `LIVE` / `LIVE_CANDIDATE`. Unit test L26 asserts false. | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **Disproven.** Only product assignment is DI L41 from env. `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true`. Logon host L60–70 writes Quote/Trade only; **never** assigns `RealCopyEnabled`. Grep `RealCopyEnabled =` in product `*.cs` = **1** hit (DI). Live GET blocked. | **FAIL** |
| 4 | sending now cannot be the profit path | Counted dest profit is constructor `0` (`EfDashboardQueries` L44 → `OverviewDto.DestinationRealPnl`). Live `1369850` refused. Persist `AllowFixSend=false`. `VenueReconciled` const `false`. `CanPromoteToLive=>false`. Session hop has no `35=D`. Residual: hosted demo dest **can** `Build("D")` — dest **execution**, not dest **profit accounting**. | **PASS** (live / counted dest $). Not proved: “no dest send exists.” |
| 5 | SHADOW on demo is not destination profit | `ShadowCopyEngine.SimulateEntry` is paper. Persist status `SHADOW_ONLY`. Trader-row `ShadowPnl` literal `0`. Overview `ShadowPnl` = sum of `SourceVsShadowSlippage`; `DestinationRealPnl` literal `0`. Residual: `ADMITTED` SHADOW can dest-send; dest send ≠ dest PnL DTO. | **PASS** |

One-line:

```text
FAIL. CTraderFixSession 35=A only. CanPromoteToLive=>false. RealCopyEnabled NOT forced false after logon (.env true + DI bind + no re-pin). Counted dest PnL is constructor 0; live 1369850 refused; SHADOW is paper. Demo dest hop can still 35=D. Live GET blocked. Risk to live capital NONE.
```

---

## 1. No 35=D builder — PASS (`CTraderFixSession.cs`)

Assigned file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135, full read).

The class is a one-shot TLS Logon probe. Two types only: `CTraderFixSessionResult` + static `CTraderFixSession`. There is no order builder, no heartbeat loop, no quote subscribe, no sequence store, no `NewOrderSingle` identifier.

The only outbound constructor is `BuildLogon`:

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

- `TryLogonAsync` L47–49: one `WriteAsync` of that logon, then `ReadAsync`, then parse inbound tag `35`.
- Inbound `Extract(reply, "35")` (L55) is **not** a builder. Accept path requires inbound `msgType == "A"` (L56).
- Sockets / SSL are `using`-disposed. No keep-alive TRADE session remains after return.
- Grep of this file: `NewOrderSingle` = 0, `Build("D")` = 0, `(35, "D")` = 0, literal `35=D` = 0.
- Grep `(35,` in this file = **1** hit (L96 `"A"`).

`LiveCopyPage.tsx` (70/70) has **zero** FIX encode, **zero** POST, **zero** send button. It only renders `/api/copy/status` + `/api/copy/intents`.

**Residual (does not fail claim 1 as scoped):** other product files **do** build `35=D`:

| File | Hits | Wired? |
|---|---|---|
| `CTraderFixCopyOpen.cs` L95 | `Build("D", …)` | **Yes** — `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566, hosted every 20s by `CopyTradingHostedService` L30. Demo-gated; refuses `account == "1369850"`. |
| `CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | `Build("D", …)` ×3 | Tools/CLI (`tools/DemoFixTestTrade`). Demo-gated; refuses live identity. |
| `CTraderFixDemoMatrix.cs` L93 | `Build("D", …)` | Demo matrix helper. |

Claim 1 is scoped to `CTraderFixSession`. Those siblings are not this file. They **do** matter for claim 4 residual.

---

## 2. `CanPromoteToLive` is false — PASS (`BaselineScorer.cs`)

Assigned file `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (213/213).

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

- Parameter `current` is **unread**. Every `TraderState` returns false.
- `FromBaseline` (L189–207) reachable set: `INSUFFICIENT_DATA`, `RISK_BLOCKED`, `SHADOW`, `WATCH`, `EARLY_SCORE`. It **never** emits `LIVE` or `LIVE_CANDIDATE`. Highest auto state is `SHADOW` when `quality >= 70 && risk < 40` and early-eligible.
- Ingest writes `CurrentState = score.SuggestedState` (`DealIngestionService.cs` / `ReconstructionScoringService` L140). `EfTradingStore.UpsertScoreAsync` L232 copies that state. Product `CurrentState =` writers under `src/` do not assign `TraderState.LIVE`.
- Unit `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` L26: `CanPromoteToLive(...).Should().BeFalse()`. Suggested state for three disciplined winners is `SHADOW` (L25).

`TraderState` enum still contains `LIVE_CANDIDATE = 4` and `LIVE = 5` (`TraderState.cs`). That is a label, not a promotion path.

`LiveCopyPage.tsx` L15 displays `liveTraders` from API; that count is `scores.Count(s => s.CurrentState == LIVE)` (`CopyTradingService` L58). The scorer cannot produce that state. Live GET of the count is blocked this slot — file proof still holds.

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

Lab `.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret). Same file L106: `FEATURE_COPY_TRADING_ENABLED=true`.

`string.Equals(..., "true", OrdinalIgnoreCase)` on that value is **true**. The runtime bit is armed at DI construction, **before** any FIX logon.

### 3.2 Logon host does not re-pin

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

- Writes: Quote + Trade status only.
- `_runtime.RealCopyEnabled` is **read** for the log line. It is **not** assigned.
- Grep `_runtime.RealCopyEnabled =` / `runtime.RealCopyEnabled =` under `src/` + `apps/` = **0** after DI.
- The log string “NewOrderSingle still unimplemented” is **not** a force-false.

### 3.3 Unused POCO default is not the pin

`CTraderFixOptions.RealCopyExecutionEnabled` defaults to `false` (`CTraderFixOptions.cs` L35). That property is **not** the `LiveRuntimeStatus` bit. `apps/fix-worker/Worker.cs` L21 reads `CTrader:RealCopyExecutionEnabled` (different key) and still does not write `LiveRuntimeStatus`. A comment “Default OFF” is not a post-logon force.

`LiveRuntimeStatus.Snapshot()` (`L42–44`) even documents the armed case: `"REAL_COPY armed. NewOrderSingle still unimplemented; …"`. That branch exists because the bit can be true.

### 3.4 Live GET unproven

`GET /api/health` and `GET /api/settings` would report `realCopyEnabled` / `featureFlags.REAL_COPY_EXECUTION_ENABLED` from the process (`Program.cs` L55, L76). This slot **could not** fetch them. Absence of a live DTO does **not** convert the missing assignment into a force-false.

Reports that still say “host forces false after logon” (e.g. `reports/CREDENTIALS_AND_COPY_STATUS.md` “forced”) are **STALE** relative to current `CTraderFixLogonHostedService.cs`.

---

## 4. Sending now cannot be the profit path — PASS (live / counted dest $)

Assigned `RiskEngine.cs` (189/189) + hop `CopyTradingService.cs` (625/625).

### 4.1 Persist hop cannot emit a live ticket

`RiskEngine.Evaluate` allow-send conjunction (L147–150):

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

Copy hop feeds that evaluator with **const** `VenueReconciled = false` (`CopyTradingService` L20) as `Reconciled` (L304). Increasing actions therefore reject at L84–85 (`VENUE_NOT_RECONCILED`) **before** the allow-send conjunction.

Even if Evaluate returned `AllowFixSend=true`, persist **overwrites**:

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
                _db.RiskDecisions.Add(rec);
                intent.RiskDecisionId = rec.Id;

                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
```

- Persisted `AllowFixSend` is **literal false** (L324).
- The “live send” branch never writes a FIX frame; it sets status `LIVE_SEND_BLOCKED_UNIMPLEMENTED`.
- That branch is dead anyway: `VenueReconciled` const false **and** `CanPromoteToLive` is false so `CurrentState` cannot be `LIVE` from scoring.

`RiskEngineTests.Real_flag_false_never_allows_fix_send` (L21–26) asserts `AllowFixSend` false when `RealExecutionEnabled=false`. That does **not** prove the process flag is false (claim 3 failed). It proves the evaluator respects the bit.

The empty block at L90–93 (`RealExecutionEnabled == false`) is a comment, not a return. `allowSend` still requires the flag, so it stays false when the flag is false.

### 4.2 Counted dest profit is constructor 0

`OverviewDto` field order (`DashboardModels.cs` L15–16): `ShadowPnl`, then `DestinationRealPnl`.

`EfDashboardQueries.GetOverviewAsync` L43–46:

```43:46:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            shadowPnl,
            0,
            0,
            0,
```

`DestinationRealPnl` is the first `0`. There is no venue mark, no dest-fill sum, no ledger PnL. Live GET of `/api/overview` is blocked this slot; the constructor is compile-time `0` regardless of process state.

### 4.3 Live Pepperstone identity is refused

`CTraderFixCopyOpen.SendAsync` L37–42 refuses unless **all** of: host starts `demo-`, sender starts `demo.`, account **≠** `1369850`. Live dest identity cannot receive this builder.

Lab `.env` public dest (no secrets):

- L49 `CTRADER_FIX_HOST=demo-us-eqx-01.p.c-trader.com` → `demo-`
- L50 `CTRADER_FIX_ACCOUNT_ID=5328266` → not `1369850`
- L64 `CTRADER_FIX_TRADE_SENDER_COMP_ID=demo.pepperstone.5328266` → `demo.`

So `CopyTradingService.DemoDest` (L45–48) is **true** on this lab env. `NewOrderSingleImplemented => DemoDest` (L50) is therefore **true**. That is dest **execution** on demo `5328266`, not live `1369850`.

### 4.4 Residual: demo dest hopper **can** send `35=D`

`CopyTradingHostedService` L27–30, every 20s: `TickRosterAsync` → `GenerateShadowIntentsAsync` → `ExecuteDemoCopyAsync`.

`ExecuteDemoCopyAsync` bypasses `RiskEngine`. On `DemoDest` it calls `CTraderFixCopyOpen.SendAsync` (L528 close, L566 open) which encodes `Build("D", …)` (CopyOpen L95). Ledger `D:\Prop\data\demo_copy_ledger.json` already records dest pos `237339770`, dest ClOrdId `C20260818093047317`, dest fill `4390.2`, `DestClosed: false` (source login/pos are public demo ids, not secrets).

`GetStatusAsync` L67 reports `VenueReconciled: DemoDest` (true on this env) while Evaluate uses the **const** `false`. Status DTO is not the send gate. Summary L76–77: `"Demo dest auto-copy ON. … Live 1369850 is never used."`

`LiveCopyPage.tsx` L57 empty-state copy: `"Demo dest auto-sends after a trader is ADMITTED…"`. That is UI text describing the hopper, not a profit book.

**Claim 4 as “sending is not the counted / live profit path” is proved.** Claim 4 as “no dest send exists anywhere” is **false** and is **not** what this slot certifies.

Wanting dest profit ≠ an edge. A TLS Logon ≠ a fill. An armed `REAL_COPY` bit ≠ a live ticket.

---

## 5. SHADOW on demo is not destination profit — PASS

### 5.1 SHADOW is a score state + paper fill

`FromBaseline` emits `SHADOW` (BaselineScorer L200–201). That is eligibility / roster input, not dest cash.

`ShadowCopyEngine.SimulateEntry` (`ShadowCopyEngine.cs` L35–60) computes an in-memory `ShadowFill` from quote bid/ask + modeled 0.05-point latency. No socket. No tag 35.

`GenerateShadowIntentsAsync` writes status `SHADOW_ONLY` (CopyTradingService L336) and `ShadowOrder` rows from `SimulateEntry` (L339–359) when the paper path approves.

`PersistDemoShadowAsync` (`EfTradingStore.cs` L267–320): if state ≠ `SHADOW`, returns after an outbox score event. If `SHADOW` and a dest quote exists, it writes `Status = "SHADOW_ONLY"` + another `SimulateEntry`. Still no FIX.

### 5.2 Dashboard does not book dest $ from SHADOW

- Overview `ShadowPnl` = `Sum(ShadowOrders.SourceVsShadowSlippage)` (`EfDashboardQueries` L29) — modeled slippage, not dest realized PnL.
- Overview `DestinationRealPnl` = literal `0` (L44).
- `TraderRowDto.ShadowPnl` is hardcoded `0` (`EfDashboardQueries` L118; DTO field at `DashboardModels.cs` L56).

### 5.3 Residual: SHADOW can be ADMITTED and then dest-sent

`XauUsdOneToOneCopyPolicy.IsTraderEligible` **rejects** `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` (L81–85) and blocked states. It **accepts** `SHADOW` (and `LIVE_CANDIDATE` / `LIVE` if those labels ever appear) once `CompletedXauTrades >= 20`, `XauNetPnl > 0`, demo/contest group, no size-pattern flags.

`CopyRosterEngine.Decide` admits when `IsTraderEligible` is true (L72–80). `ExecuteDemoCopyAsync` then dest-sends for `ADMITTED` roster seats (L542–598) **without** re-checking `TraderState.SHADOW` vs paper.

So: SHADOW-on-demo **can** cause dest **execution** on demo `5328266`. That is still **not** `DestinationRealPnl`. Dest send ≠ dest profit DTO. Claim 5 as written (“SHADOW on demo is not destination profit”) holds.

---

## 6. Live GET matrix (this slot)

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | `web_fetch` SSRF blocked |
| `http://localhost:5000/api/copy/status` | `web_fetch` SSRF blocked |
| `/api/settings` `REAL_COPY_EXECUTION_ENABLED` process bit | **Unproven** |
| `/api/overview` `destinationRealPnl` process value | **Unproven** (file constructor is `0`) |
| `/api/copy/status` `realCopyArmed` / `liveSends` / `liveTraders` | **Unproven** |

No claim that required a live DTO is marked PASS. Claim 3 failed on **file** disproof, not on missing GET.

---

## 7. Risk to capital

| Book | This slot |
|---|---|
| Live Pepperstone `1369850` | **NONE.** CopyOpen refuse + session has no `35=D` + persist hop cannot send + scorer cannot promote LIVE. |
| Demo dest `5328266` | **Not absent.** Hosted `ExecuteDemoCopyAsync` can `Build("D")` when `DemoDest` (lab `.env` matches). Ledger already has an open dest pos. This is demo dest **execution**, not live capital, and not `DestinationRealPnl`. |
| MT5 sources | Copy hop does not send to MT5. Flatten is destination-only (`CopyRosterEngine` comment + dest close via CopyOpen). |

`risk_to_capital` for the assigned live-profit question: **NONE**.

---

## 8. What this slot did **not** do

- Did not edit product, tests, or `.env`.
- Did not attach Manager, open TLS, send Logon, or send `35=D`.
- Did not compute SHA-256 (no shell).
- Did not trust sibling numbers (18/8460 census etc.) — unused here.
- Did not print secrets.

**End P500_VERIFY_64.** Slot **64**. Verdict **FAIL** (claim 3 disproved). Risk to live capital **NONE**; demo dest send **wired**.
