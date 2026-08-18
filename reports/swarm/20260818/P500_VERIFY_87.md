# P500_VERIFY_87 — Adversarial four-file verify (slot 87)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_87.md` |
| Slot | **87** |
| Date | 2026-08-18 |
| Agent | P500_VERIFY_87 (adversarial verifier; sibling `P500_*` / `W500_*` numbers are **not** evidence) |
| Assigned | Read `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`. Confirm: (1) no `35=D` builder; (2) `CanPromoteToLive` is false; (3) `RealCopyEnabled` forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. **FAIL any claim that cannot be proved from a file or live GET.** |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| `.env` modified | **No.** |
| Secrets printed | **None.** Quoted only already-on-disk booleans `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true`, public dest ids `5328266` / `1369850`, public host prefix `demo-`. Tag 554 / passwords / proxy never dumped. |
| Live GET this slot | **Attempted and blocked.** `web_fetch` → `SSRF blocked: 127.0.0.1`. `open_page` `http://127.0.0.1:5000/api/health` and `http://localhost:5000/api/copy/status` → retrieve error. **No live JSON.** File proof only. Runtime `realCopyEnabled` DTO is therefore **unmeasured**. |
| Live `35=D` sent this slot | **No.** |
| `REAL_COPY` flipped this slot | **No.** |
| Method | Independent full `read_file` of the four assigned files (`CTraderFixSession.cs` 135/135, `BaselineScorer.cs` 213/213, `RiskEngine.cs` 189/189, `LiveCopyPage.tsx` 70/70). Then hop files those files actually call: `CTraderFixLogonHostedService.cs`, `LiveRuntimeStatus.cs`, `DependencyInjection.cs`, `CopyTradingService.cs` (625), `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `apps/api/Program.cs`, `DealIngestionService.cs`, `CTraderFixOptions.cs`, `DemoCopyLedger.cs`, `data/demo_copy_ledger.json` (dest ids / fill price only), `.env` **boolean / public host / public account keys only**. Grep: `Build("D")` / `(35,` / `RealCopyEnabled =` / `CanPromoteToLive` / `DestinationRealPnl` / `CurrentState =` / `TraderState.LIVE`. |

**Honesty rule:** a comment, log line, dashboard label, or `LastError` string is **not** a builder. `35=A` Logon is **not** NewOrderSingle. A sibling `Build("D")` is **not** `CTraderFixSession`. A POCO default `= false` is **not** a process pin. An env bind that evaluates `true` **disproves** “forced false after logon.” Demo dest fill is **not** live `1369850` profit and is **not** booked `DestinationRealPnl`. Sibling swarm reports are **not** evidence. Set **FAIL** if any assigned claim cannot be proven from a file or live GET.

---

## 0. Verdict (binding)

**FAIL.**

Claims **1, 2, 4, 5** are file-proven (claim 1 scoped to the assigned session file; claim 4 scoped to booked live dest profit). Claim **3** is **disproved**, not merely unproved.

| # | Claim | File proof | Result |
|---|---|---|---|
| 1 | no `35=D` builder | `CTraderFixSession.cs` 135/135. Only outbound MsgType is `(35, "A")` at L96 inside `BuildLogon`. One `WriteAsync` (L49). Grep `Build("D")` / `(35, "D")` / `35=D` / `NewOrderSingle` in that file = **0**. | **PASS** (assigned file) |
| 2 | `CanPromoteToLive` is false | `BaselineScorer.cs` L211: `public static bool CanPromoteToLive(TraderState current) => false;` Unconditional. `FromBaseline` never returns `LIVE` / `LIVE_CANDIDATE`. Unit test L26 asserts false. | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **Disproved.** Only assignment is DI L41 from env. `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true`. Logon host L60–70 writes Quote/Trade only; **never** assigns `RealCopyEnabled`. Grep `RealCopyEnabled =` in product `*.cs` = **1** hit (DI). Live GET of the DTO **blocked**. | **FAIL** |
| 4 | sending now cannot be the profit path | Booked dest profit is constructor `0` (`EfDashboardQueries` L44 `DestinationRealPnl`). Live `1369850` refused. Persist `AllowFixSend=false`. `CanPromoteToLive=>false`. Session hop has no `35=D`. Residual: hosted demo dest **can** `Build("D")` — dest **execution**, not dest **profit accounting**. | **PASS** (not booked dest profit) |
| 5 | SHADOW on demo is not destination profit | `ShadowCopyEngine.SimulateEntry` is paper. Persist status `SHADOW_ONLY`. Overview `ShadowPnl` = sum of `SourceVsShadowSlippage`; `DestinationRealPnl` literal `0`. `LiveCopyPage` shows SHADOW **counts**, no dest cash. Residual: `ADMITTED` SHADOW can dest-send; dest send ≠ dest PnL DTO. | **PASS** |

One-line:

```text
FAIL. CTraderFixSession 35=A only. CanPromoteToLive=>false. RealCopyEnabled NOT forced false after logon (.env true + DI bind + no re-pin; live GET blocked). Counted dest PnL is constructor 0; live 1369850 refused; SHADOW is paper. Demo dest hop can still 35=D. Risk to live capital NONE.
```

---

## 1. Claim 1 — no `35=D` builder — **PASS** (`CTraderFixSession.cs` only)

Assigned file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135** lines, full read).

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

| Check | Measured in this file |
|---|---|
| Outbound MsgType | **only** `(35, "A")` L96 |
| `WriteAsync` count | **1** (L49, the logon bytes) |
| Inbound parse of tag `35` | `Extract(reply, "35")` L55 — **parser, not a builder** |
| Inbound `35=A` treated as | `LoggedOn = true` L56–64 — logon ack, **not** a fill |
| Socket lifetime | `using TcpClient` + `await using SslStream`; disposed after one read |
| `35=D` / `(35, "D")` / `Build("D")` / `NewOrderSingle` | **0** |
| Grep `(35,` in this file | **1** hit (L96 `"A"`) |

`TryLogonAsync` L47–50 writes that logon, flushes, reads once, then returns. There is no heartbeat loop, no order book, no ClOrdID, no tag 40/38/54 outbound.

**Residual (does not fail the assigned-file claim):** sibling product files **do** have a `35=D` builder. A system-wide “no `35=D` builder” claim would **FAIL**.

| File | What this slot measured |
|---|---|
| `CTraderFixCopyOpen.cs` L95 | `Build("D", …)` — **hosted** by `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566 |
| `CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | `Build("D", …)` — tools/CLI, demo-gated, refuses `1369850` |
| `CTraderFixDemoMatrix.cs` L93 | `Build("D", …)` via local `SendD` — demo matrix helper |

Claim 1 is scoped to `CTraderFixSession`. Those siblings are not this file. They **do** matter for claim 4 residual.

---

## 2. Claim 2 — `CanPromoteToLive` is false — **PASS**

Assigned file: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (**213** lines, full read).

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

- Parameter `current` is **unread**. Every `TraderState` returns false.
- `FromBaseline` L189–207 can emit `INSUFFICIENT_DATA`, `RISK_BLOCKED`, `SHADOW`, `WATCH`, `EARLY_SCORE`. It never emits `LIVE` or `LIVE_CANDIDATE`. Best early path is `quality >= 70 && risk < 40` → `SHADOW` (L200–201).
- `AfterHighEarlyScore()` is hard-`SHADOW` (L209).
- Ingest writes `CurrentState = score.SuggestedState` (`DealIngestionService.cs` L140). Product `CurrentState =` writers: ingest + `EfTradingStore` copy of the score. Grep `TraderState.LIVE` in product writers: **no assignment** of LIVE; only counts / a dead send-branch predicate.
- Unit `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` L21–26: three winners → `SHADOW`; `CanPromoteToLive(...).Should().BeFalse()`.

`LiveCopyPage.tsx` L15 displays `liveTraders` from API; that count is `scores.Count(s => s.CurrentState == LIVE)` (`CopyTradingService` L58). The scorer cannot produce that state. A non-zero live count on a live GET would be a **data residual**, not a scorer promotion. Live GET blocked → that count is unmeasured.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL**

**Cannot prove. Disproved as process state.** Live GET of the DTO was blocked; file proof is enough to fail the claim.

### 3.1 Assigned session file never touches the flag

`CTraderFixSession.cs` has no `RealCopyEnabled` identifier. Logon success sets `LoggedOn = true` only.

### 3.2 Logon host reads the flag; never assigns false

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

Post-logon writes: Quote/Trade `LoggedOn` / `Status` / `LastError` / `UpdatedAt`. `_runtime.RealCopyEnabled` is **logged**, not assigned. There is no `_runtime.RealCopyEnabled = false` in this file.

### 3.3 Sole product assignment is env-true

Grep `RealCopyEnabled =` across product `*.cs` = **one** hit:

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`.env` L73 (boolean key only): `REAL_COPY_EXECUTION_ENABLED=true`.

`string.Equals("true", "true", OrdinalIgnoreCase)` is **true**. The process therefore **starts armed** unless some later pin exists. No later pin exists.

### 3.4 POCO default is not a pin

`CTraderFixOptions.RealCopyExecutionEnabled` defaults to `false` (`CTraderFixOptions.cs` L35). That options type is **not** what DI binds onto `LiveRuntimeStatus`. `LiveRuntimeStatus.RealCopyEnabled` is a settable bool with **no** field initializer (`LiveRuntimeStatus.cs` L32). `apps/api/Program.cs` L55 / L76 **echo** `runtime.RealCopyEnabled`; they do not force false.

`apps/api/appsettings.json` FeatureFlags `LiveCopyEnabled: false` is a **different key**. Unused by the DI bind above.

`apps/fix-worker/Worker.cs` L21 reads `CTrader:RealCopyExecutionEnabled` (also a **different** key) and still does not send `35=D`. Irrelevant to the API runtime flag.

### 3.5 Live GET unproven

`GET /api/health` would return `realCopyEnabled = runtime.RealCopyEnabled`. `GET /api/settings` would return `featureFlags.REAL_COPY_EXECUTION_ENABLED`. `GET /api/copy/status` would return `realCopyArmed`. This slot **could not** retrieve them. File bind + `.env=true` + no re-pin already **disproves** “forced false after logon.” A live `false` DTO would not rescue the claim: the code path does not force it.

---

## 4. Claim 4 — sending now cannot be the profit path — **PASS** (not booked dest profit)

Assigned `RiskEngine.cs` (**189** lines) and `LiveCopyPage.tsx` (**70** lines), plus the copy hop they feed.

### 4.1 What “profit path” is, for this slot

Booked destination profit is `OverviewDto.DestinationRealPnl`. Constructor argument in `EfDashboardQueries.GetOverviewAsync` L44 is the **literal** `0`:

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

`DashboardModels.cs` L16 names that slot `DestinationRealPnl`. Hardcoded `0` is **not** measured dest cash; it **is** proof the dashboard does not book dest profit. Live GET of `/api/overview` was blocked; the constructor is file proof of the DTO.

Live Pepperstone identity `1369850` is refused by `CTraderFixCopyOpen.SendAsync` L37–42.

### 4.2 Assigned session cannot send a ticket

`CTraderFixSession` outbound is `35=A` only (§1). Logon is not a fill and is not dest PnL.

### 4.3 Assigned page cannot send

`LiveCopyPage.tsx` 70/70:

- Imports only `useCopyStatus` / `useCopyIntents` (GET hooks in `apps/web/src/api/hooks.ts` L60–65).
- **No** `fetch` POST, **no** button, **no** form, **no** `35=D`.
- Displays `realCopyArmed`, SHADOW/LIVE **counts**, `liveSends`, intents, blockers.
- Empty-state L57: *“Demo dest auto-sends after a trader is ADMITTED…”* — UI **copy**, not a sender.

The page cannot be the profit path.

### 4.4 RiskEngine AllowFixSend is gated; persist hop forces false

```147:171:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
        ...
                AllowFixSend = allowSend
```

`CopyTradingService.VenueReconciled` is `const bool` **false** (L20). Shadow-intent eval passes `Reconciled = VenueReconciled` (L304). Increasing actions therefore reject `VENUE_NOT_RECONCILED` (RiskEngine L84–85). Even on a reducing approve, `allowSend` is false because `Reconciled` is false.

Then the persist hop **overwrites** the engine:

```317:333:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                var rec = new RiskDecisionRecord
                {
                    ...
                    AllowFixSend = false,
                    DecidedAt = now
                };
                ...
                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
```

That branch never calls `CTraderFixCopyOpen`. It only sets a status string. Combined with `CanPromoteToLive => false` and `VenueReconciled` const false, the predicate is dead. **This hop cannot send.**

Unit `RiskEngineTests.Real_flag_false_never_allows_fix_send` (L21–26) asserts `AllowFixSend` false when `RealExecutionEnabled = false`. That does **not** prove the runtime flag is false (claim 3 failed).

### 4.5 Residual that does **not** fail booked-dest-profit

`CopyTradingHostedService` L28–30 ticks every 20s: `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**.

`ExecuteDemoCopyAsync`:

- Returns 0 unless `DemoDest` (host starts with `demo-`, sender starts with `demo.`, account ≠ `1369850`) — L45–48, L485–488.
- Does **not** read `RealCopyEnabled`.
- Does **not** call `RiskEngine.Evaluate`.
- Calls `CTraderFixCopyOpen.SendAsync` which `Build("D")` (L95).
- `.env` L49 host is `demo-us-eqx-01.p.c-trader.com`; L50 account is public dest `5328266`. Those keys make `DemoDest` **true** if the matching sender also starts with `demo.` (sender value not quoted).

`data/demo_copy_ledger.json` records dest activity (public ids only): source `305750` / `21250421` → dest pos `237339770`, dest px `4390.2`, `DestClosed: false`. That is dest **execution**, not `DestinationRealPnl`.

`GetStatusAsync` L67 reports `VenueReconciled: DemoDest` (can be **true**) while the risk eval uses const `false`. Status copy L76–77 says demo dest auto-copy is ON. That is dest **activity advertising**, not booked dest profit.

`NewOrderSingleImplemented => DemoDest` (L50) is **true** on demo dest. Older “const false” claims are **stale** vs this file.

**If the claim were “nothing can send 35=D now,” it would FAIL.** The assigned claim is that sending is not **the profit path**. Booked dest profit is constructor 0; live `1369850` is refused. Demo dest cash is **unmeasured** (live GET blocked; DTO not computed). Unmeasured demo dest P&L is not proof of booked profit.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **PASS**

### 5.1 SHADOW is a source-trader state

`TraderState.SHADOW = 3` (`TraderState.cs` L8). `FromBaseline` emits it for eligible high-quality / low-risk books (`BaselineScorer.cs` L200–201). It is **not** a dest fill.

### 5.2 Paper shadow engine

`ShadowCopyEngine.SimulateEntry` / `SimulateExit` (`ShadowCopyEngine.cs` L35–83) compute a `ShadowFill` from a `DestinationQuote`. No socket. No tag 35. `CopyTradingService` L336–359 writes `ShadowOrder` rows and sets intent status `SHADOW_ONLY` when the dead LIVE-send predicate fails (the normal path).

### 5.3 Dashboard SHADOW numbers are not dest cash

- Overview `ShadowPnl` = `Sum(ShadowOrders.SourceVsShadowSlippage)` (`EfDashboardQueries` L29, L43).
- Overview `DestinationRealPnl` = literal `0` (L44).
- `LiveCopyPage.tsx` L14 shows `shadowTraders` (a **count**). No dest PnL column. Intent table shows `status` / `riskReason`, not dest cash.

### 5.4 Residual: SHADOW can be dest-**sent**, still not dest-**profit DTO**

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` / blocked states (L81–85). It **does not** reject `SHADOW`. A SHADOW trader with ≥20 completed XAU, `XauNetPnl > 0`, demo/contest group, no size pattern → `AUTO_ADMIT` (`CopyRosterEngine` L72–80). `ExecuteDemoCopyAsync` then dest-sends for `ADMITTED` seats **without** checking `CurrentState`.

That residual is dest **activity** on demo (refuses `1369850`). It is **not** `DestinationRealPnl`. Claim 5 as “SHADOW ≠ destination profit” holds.

---

## 6. Live GET matrix (this slot)

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` (`web_fetch`) | SSRF blocked |
| `http://127.0.0.1:5000/api/health` (`open_page`) | retrieve error |
| `http://localhost:5000/api/copy/status` (`open_page`) | retrieve error |
| `/api/settings`, `/api/overview`, `/api/risk` | **not retrieved** |

Claims that needed a live DTO and lacked file proof would FAIL-unproven. Claim 3 failed on **files**. Claims 4–5 dest PnL used the constructor `0`, not a live GET.

---

## 7. Risk to capital

| Surface | This slot |
|---|---|
| Live Pepperstone `1369850` | **NONE.** `CTraderFixSession` cannot send D. `CTraderFixCopyOpen` / `CTraderFixDemoTestTrade` / `CTraderFixDemoMatrix` refuse that account. |
| API persist hop / RiskEngine LIVE send | **NONE.** `AllowFixSend` persisted false. `VenueReconciled` const false. `CanPromoteToLive` hard-false. Dead status string only. |
| Demo dest `5328266` (public id) | **Not absent.** Hosted `ExecuteDemoCopyAsync` can emit `35=D` without `RiskEngine` and without reading `RealCopyEnabled`. Ledger already has a dest fill price. This slot sent **0**. Demo dest P&L is **unmeasured**. |
| This slot | Did not send. Did not flip `REAL_COPY`. Did not print secrets. |

---

## 8. What would flip this slot to PASS

All five assigned claims must be file- or live-GET-proven. Claim 3 needs a **post-logon assignment** `_runtime.RealCopyEnabled = false` (or deletion of the env-true bind plus a hard-false pin) **and** a live GET showing `realCopyEnabled: false` after TRADE `35=A`. Residuals on claims 1/4 (sibling `Build("D")` / demo dest hopper) do not block a slot PASS by themselves if claim 1 stays scoped to `CTraderFixSession` and claim 4 stays scoped to booked dest profit.

---

End of P500_VERIFY_87. Product source was not modified. No secrets printed. This slot did not send `35=D`. `REAL_COPY` was not flipped.
