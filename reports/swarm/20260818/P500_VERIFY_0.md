# P500_VERIFY_0 — Adversarial four-file verify (slot 0)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_0.md` |
| Agent / slot | P500 verify **0** (adversarial; do not trust siblings) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Quoted as booleans / public dest ids only. |
| Secrets printed | **None.** Quoted only `REAL_COPY_EXECUTION_ENABLED=true`, `FEATURE_COPY_TRADING_ENABLED=true`, public dest `5328266` / `1369850`, public host prefix `demo-`. Tag 554 never dumped. |
| Live GET this pass | **Blocked.** `web_fetch`/`open_page` refuse loopback (`SSRF blocked: localhost → 127.0.0.1`). No shell. Claims that need a live DTO are **FAIL-unproven**, not guessed. |
| Method | Independent full `read_file` of `CTraderFixSession.cs` (135/135), `BaselineScorer.cs` (213/213), `RiskEngine.cs` (189/189), `LiveCopyPage.tsx` (70/70). Then hop files required to prove/fail claims 3–5: `CTraderFixLogonHostedService.cs`, `LiveRuntimeStatus.cs`, `DependencyInjection.cs`, `CopyTradingService.cs` (625), `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `EfDashboardQueries.cs` (overview dest PnL), `DashboardModels.cs`, `apps/api/Program.cs`, `.env` boolean keys only, `data/demo_copy_ledger.json` dest ids only. Grep: `(35,` / `Build("D")` / `RealCopyEnabled =` / `CanPromoteToLive` / `DestinationRealPnl`. |

**Honesty rule:** sibling reports are **not** evidence. A comment, log line, dashboard copy, or `LastError` string is **not** a builder. `35=A` Logon is **not** NewOrderSingle. A sibling `Build("D")` is **not** `CTraderFixSession`. A POCO `= false` default is **not** a process pin. An env bind that evaluates `true` **disproves** “forced false after logon.” Demo dest fill is **not** live `1369850` profit and is **not** `DestinationRealPnl`. Set **FAIL** if any assigned claim cannot be proven from a file or live GET.

---

## 0. Verdict (binding)

**FAIL.**

Claims **1, 2, 4, 5** are file-proven. Claim **3** is **disproven**: `RealCopyEnabled` is **not** forced false after logon.

| # | Claim | File proof | Result |
|---|---|---|---|
| 1 | no `35=D` builder | `CTraderFixSession.cs` 135/135. Only outbound MsgType is `(35, "A")` at L96 inside `BuildLogon`. One `WriteAsync` (L49). Grep `Build("D")` / `(35, "D")` / `35=D` in that file = **0**. | **PASS** |
| 2 | `CanPromoteToLive` is false | `BaselineScorer.cs` L211: `public static bool CanPromoteToLive(TraderState current) => false;` Unconditional. `FromBaseline` never returns `LIVE` / `LIVE_CANDIDATE`. Unit test L26 asserts false. | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **Disproven.** Only assignment is DI L41 from env. `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true`. Logon host L60–70 writes Quote/Trade only; **never** assigns `RealCopyEnabled`. Grep `RealCopyEnabled =` in product `*.cs` = **1** hit (DI). | **FAIL** |
| 4 | sending now cannot be the profit path | Counted dest profit is constructor `0` (`EfDashboardQueries` L44 `DestinationRealPnl`). Live `1369850` refused. Persist `AllowFixSend=false`. `CanPromoteToLive=>false`. Session hop has no `35=D`. Residual: hosted demo dest **can** `Build("D")` — dest **execution**, not dest **profit accounting**. | **PASS** |
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

- `TryLogonAsync` L47–49: one `WriteAsync` of that logon, then `ReadAsync`, then parse inbound tag `35`.
- Inbound `Extract(reply, "35")` (L55) is **not** a builder.
- Grep of this file: `NewOrderSingle` = 0, `Build("D")` = 0, `(35, "D")` = 0, literal `35=D` = 0.
- Grep `(35,` in this file = **1** hit (L96 `"A"`).

**Residual (does not fail claim 1):** siblings **do** build `35=D`:

| File | Hits |
|---|---|
| `CTraderFixCopyOpen.cs` L95 | `Build("D", …)` — **hosted** by `CopyTradingService.ExecuteDemoCopyAsync` |
| `CTraderFixDemoTestTrade.cs` L139/L163/L197 | `Build("D", …)` — tools/CLI, demo-gated, refuses `1369850` |
| `CTraderFixDemoMatrix.cs` L93 | `Build("D", …)` — demo matrix helper |

Claim 1 is scoped to `CTraderFixSession`. Those siblings are not this file. They **do** matter for claim 4 residual.

---

## 2. `CanPromoteToLive` is false — PASS (`BaselineScorer.cs`)

Assigned file `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (213/213).

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

- Parameter `current` is **unread**. Every `TraderState` returns false.
- `FromBaseline` (L189–207) can emit `INSUFFICIENT_DATA`, `RISK_BLOCKED`, `SHADOW`, `WATCH`, `EARLY_SCORE`. It never emits `LIVE` or `LIVE_CANDIDATE`.
- Ingest writes `CurrentState = score.SuggestedState` (`DealIngestionService` L140). No product writer assigns `TraderState.LIVE`.
- Unit `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` L26: `CanPromoteToLive(...).Should().BeFalse()`.

`LiveCopyPage.tsx` L15 displays `liveTraders` from API; that count is `scores.Count(s => s.CurrentState == LIVE)` (`CopyTradingService` L58). The scorer cannot produce that state.

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
        services.AddSingleton(runtime);
```

Lab `.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret).
API host `apps/api/Program.cs` L10: `EnvFile.FindAndLoad()` then L13 `AddEnvironmentVariables()` then L15 `AddTraderIntelligence`.

So API-process `LiveRuntimeStatus.RealCopyEnabled` is **true** at construction.

### 3.2 Logon host does not re-pin

`CTraderFixLogonHostedService.ExecuteAsync` after both `CTraderFixSession.TryLogonAsync` calls:

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

- Writes Quote + Trade only.
- **Logs** `RealCopyEnabled`. Does **not** assign it.
- Comment/log “NewOrderSingle still unimplemented” is **not** a pin.

`CTraderFixOptions.RealCopyExecutionEnabled` default `false` (L35) is **unbound** by DI. Not the API gate.

`LiveRuntimeStatus.Snapshot()` copyNote when armed: “REAL_COPY armed. NewOrderSingle still unimplemented…” — a string, not a force-false.

### 3.3 Live GET not available

`GET http://localhost:5000/api/health` and `/api/settings` and `/api/copy/status` and `/api/ingest/status` were **not** fetched (loopback SSRF). File bind is enough to fail claim 3. I do **not** invent a live `realCopyEnabled` JSON.

### 3.4 What this does **not** prove

Armed `RealCopyEnabled` is **not** a live `35=D` on `1369850`. See claim 4. Flag-true ≠ send license on the live hop.

---

## 4. Sending now cannot be the profit path — PASS (counted dest / live capital)

### 4.1 Assigned RiskEngine cannot license a live send on the hosted shadow hop

`RiskEngine.Evaluate` allow-send:

```147:170:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
        ...
                AllowFixSend = allowSend
```

Empty L90–93 comment (“Shadow path still evaluates risk but never allows FIX send”) is **not** a force-false. If `RealExecutionEnabled && Reconciled && VenueHealthy && KillSwitch.None`, `AllowFixSend` **can be true**.

Hosted shadow hop **does not** reach that:

```20:20:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
```

```303:304:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    RealExecutionEnabled = _runtime.RealCopyEnabled,
                    Reconciled = VenueReconciled,
```

`Reconciled=false` + increasing action ⇒ `Reject(..., "VENUE_NOT_RECONCILED")` with `AllowFixSend=false` (L84–85, L180–187). Persist then **overwrites** anyway:

```317:324:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                var rec = new RiskDecisionRecord
                {
                    Id = Guid.NewGuid(),
                    CopyIntentId = intent.Id,
                    Outcome = decision.Outcome,
                    ApprovedQuantity = decision.ApprovedQuantity,
                    Reason = decision.Reason,
                    AllowFixSend = false,
```

Live-send branch L330 requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled`. `VenueReconciled` const false **and** `CanPromoteToLive=>false` ⇒ branch never taken. Else path is `SHADOW_ONLY` + `ShadowCopyEngine.SimulateEntry`.

### 4.2 Counted destination profit is a constructor zero

```43:46:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            shadowPnl,
            0,
            0,
            0,
```

`OverviewDto.DestinationRealPnl` (models L16) is the second `0`. No file computes dest realized PnL from `DemoCopyLedger` / `DestFillPrice`. Risk dashboard `DailyPnl` is also constructor `0` (`EfDashboardQueries` L208).

`LiveCopyPage.tsx` does not display dest PnL. It shows `liveSends` = count of `ExecutionIntents` with `SentAt != null` (L57) — not dest dollars.

**Therefore:** sending is not the product’s **profit** path. Dest profit is unaccounted (`$0` DTO). Live capital hop has no `35=D` builder.

### 4.3 Residual that does **not** flip claim 4 (dest execution ≠ dest profit)

Lab `.env` **is** `DemoDest`:

- L49 `CTRADER_FIX_HOST=demo-us-eqx-01.p.c-trader.com` (prefix `demo-`)
- L64 `CTRADER_FIX_TRADE_SENDER_COMP_ID=demo.pepperstone.5328266` (prefix `demo.`)
- L50 `CTRADER_FIX_ACCOUNT_ID=5328266` (≠ `1369850`)

So `CopyTradingService.DemoDest` is true and `NewOrderSingleImplemented => DemoDest` is true (L45–50).

`CopyTradingHostedService` 20s tick **does** call `ExecuteDemoCopyAsync` (L30). That method **bypasses** `RiskEngine.Evaluate` and calls `CTraderFixCopyOpen.SendAsync` → `Build("D")` L95. Gate refuses live identity:

```37:42:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
        if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
            || !sender.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
            || account == "1369850")
        {
            return Fail("Refused: dest must be demo FIX, not live 1369850.", host, account, sourceLogin);
        }
```

On-disk ledger `D:\Prop\data\demo_copy_ledger.json`: one open dest fill `305750` / `21250421` → dest pos `237339770` @ `4390.2`, `DestClosed=false`. That is dest **execution** on demo `5328266`. It is **not** `DestinationRealPnl`. It is **not** live `1369850`.

`LiveCopyPage.tsx` L24: “Live send blockers (Pepperstone cannot be filled)”. L57 empty-state: “Demo dest auto-sends after a trader is ADMITTED”. UI admits demo dest send and live Pepperstone cannot fill. Consistent with this residual.

Claim 4 as **profit path** (counted dest / live capital): **PASS**. Claim 4 as “no dest send exists”: **would FAIL** — I do not make that claim.

---

## 5. SHADOW on demo is not destination profit — PASS

### 5.1 Scorer SHADOW is paper

`FromBaseline` L200–201: `quality >= 70 && risk < 40` ⇒ `SHADOW`. `AfterHighEarlyScore()` ⇒ `SHADOW`. Promotion to LIVE is hard-false (claim 2).

`ShadowCopyEngine` (`D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`): `SimulateEntry` / `SimulateExit` / `MarkToMarket` write `ShadowFill` numbers. Zero sockets. Zero tag 35.

Hosted hopper for `{SHADOW, LIVE_CANDIDATE, LIVE}` persists `Status = "SHADOW_ONLY"` and optional `ShadowOrders` (CopyTradingService L336–359).

`PersistDemoShadowAsync` (`EfTradingStore` L267–333): if `state != SHADOW` return; else `SimulateEntry` + `Status = "SHADOW_ONLY"`. No FIX write.

Overview `ShadowPnl` = `Sum(ShadowOrders.SourceVsShadowSlippage)` (`EfDashboardQueries` L29). That is modeled slippage, **not** dest realized.

`ShadowPortfolioPage.tsx` L7: “Live NewOrderSingle remains disabled.” UI string, not proof — proof is the engine.

### 5.2 Policy SHADOW on demo source is still not dest profit

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET` (L81–85). Eligible states are SHADOW / LIVE_CANDIDATE / LIVE. Group must be demo/contest (`CopyGroupFilter.IsDemoOrContest`). Roster `AUTO_ADMIT` uses that.

`ExecuteDemoCopyAsync` then dest-sends **ADMITTED** seats without reading `CurrentState`. So a SHADOW demo-group trader **can** produce a demo dest `35=D`. That dest fill is dest **execution** (claim 4 residual). Dest **profit** DTO stays constructor `0`. SHADOW paper fills stay `ShadowOrders`.

**Therefore:** SHADOW-on-demo is not destination profit. Dest send, if it happens, is a different hop and is still not the dest-PnL ledger.

---

## 6. Assigned UI (`LiveCopyPage.tsx`) — display only

`D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (70/70).

- GET `/api/copy/status` + `/api/copy/intents` via hooks. No POST. No FIX. No env write.
- L13 `realCopyArmed` reflects runtime (claim 3: will show YES when DI-bound true).
- L24 blockers header: Pepperstone cannot be filled.
- L57 empty copy: demo dest auto-sends after ADMITTED.

The page **cannot** send. It **can** honestly show an armed flag and a demo dest empty-state. Neither is dest profit.

---

## 7. Live GET matrix (this slot)

| URL | Result |
|---|---|
| `GET http://localhost:5000/api/health` | **Blocked** (loopback SSRF) |
| `GET http://localhost:5000/api/settings` | **Blocked** |
| `GET http://localhost:5000/api/copy/status` | **Blocked** |
| `GET http://localhost:5000/api/ingest/status` | **Blocked** |
| `GET http://localhost:5000/api/overview` | **Blocked** |

No live DTO invented. File bind is sufficient to fail claim 3.

---

## 8. Risk to capital

| Hop | State |
|---|---|
| Live Pepperstone `1369850` | **NONE** — `CTraderFixSession` is `35=A` only; `CTraderFixCopyOpen` refuses this account; persist `AllowFixSend=false`; `CanPromoteToLive=>false`. `SAFE_BY_ABSENCE`. |
| Demo dest `5328266` | **Not absent.** Hosted 20s `ExecuteDemoCopyAsync` can `Build("D")`. Ledger has one open 0.01 fill. **Not live capital.** Dest PnL DTO `$0`. |
| SHADOW paper | **None.** `SimulateEntry` only. |

This slot sent **0** orders. `.env` not flipped. Product source not edited.

---

## 9. Binding table (slot 0)

| Claim | Verdict |
|---|---|
| (1) no 35=D builder in `CTraderFixSession` | **PASS** |
| (2) `CanPromoteToLive` is false | **PASS** |
| (3) `RealCopyEnabled` forced false after logon | **FAIL** (disproven) |
| (4) sending now cannot be the profit path | **PASS** (counted dest / live) |
| (5) SHADOW on demo is not destination profit | **PASS** |
| **Slot** | **FAIL** |
