# P500_VERIFY_16 — Adversarial live-path verify (slot 16)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **16** |
| Role | Adversarial verifier (read assigned files; do not trust other agents) |
| Assigned SUTs | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` |
| Adjacent hop (needed to confirm/deny claims) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `EfDashboardQueries.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyGroupFilter.cs`, `.env` booleans/host ids only |
| Product source modified | **No** |
| Secrets printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean and public dest ids `5328266` / `1369850`) |
| Live GET this slot | `GET http://127.0.0.1:5000/api/health` **blocked** (`web_fetch` SSRF deny on `127.0.0.1`). **No claim is proven from a live GET.** Prior-session integers in `P500_PROFIT_SYNTHESIS.md` are **cited as historical, not re-probed.** |
| Overall verdict | **FAIL** |

**Law used:** set overall **FAIL** if any assigned claim cannot be proven from a live file or a live GET. Wanting a green card does not create proof. Unscoped wording is judged against the whole product hop, not a single class.

Assigned claims to confirm:

1. no `35=D` builder
2. `CanPromoteToLive` is false
3. `RealCopyEnabled` forced false after logon
4. sending now cannot be the profit path
5. SHADOW on demo is not destination profit

---

## Scorecard

| # | Claim | File-proven? | Live GET? | Verdict |
|---|---|---|---|---|
| 1 | no `35=D` builder | **Session-only yes; product no** | No | **FAIL** (unscoped). `CTraderFixSession` is `35=A` only (**PASS_SESSION**). Product hop has `Build("D")`. |
| 2 | `CanPromoteToLive` is false | **Yes** | n/a | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **No — opposite is on disk** | Blocked | **FAIL** |
| 4 | sending now cannot be the profit path | **Yes** (dest constructor $0; no LIVE; persist `AllowFixSend=false`; demo hop is not a costed edge) | Blocked | **PASS** |
| 5 | SHADOW on demo is not destination profit | **Yes** (source/paper fields ≠ dest realized) | Blocked | **PASS** |

**Overall: FAIL** because claim 3 is **disproved** and claim 1 as written is **not true of the product**. Claims 2 / 4 / 5 stand from files.

Risk to capital: **NONE on live Pepperstone `1369850`** (`SAFE_BY_ABSENCE` + explicit refuse). **Not absent on demo dest `5328266`:** hosted 20 s tick can call `CTraderFixCopyOpen.SendAsync` → `Build("D")` when `DemoDest` is true. Lab `.env` **is** DemoDest (host `demo-…`, sender `demo.…`, account `5328266` ≠ `1369850`). This slot did **not** send a ticket.

`P500_PROFIT_SYNTHESIS.md` §0 / §2.1 (“`REAL_COPY` forced false”, “sets `_runtime.RealCopyEnabled = false`”, “Impossible in current code”) is **STALE on HEAD**.

---

## 1. FAIL (unscoped) / PASS_SESSION — no `35=D` builder

### 1.1 Assigned file: `CTraderFixSession` has no NewOrderSingle builder

Live file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines). Full re-read this slot.

Outbound assembler is **only** `BuildLogon`:

```89:110:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
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

- MsgType literal: **`(35, "A")` only**. No `(35, "D")`, no `Build("D")`, no `NewOrderSingle` identifier.
- `WriteAsync` count: **1** (the logon bytes). Then one `ReadAsync`. `using` `TcpClient` / `SslStream` dispose the socket.
- Reply `35=A` → `LoggedOn=true`. Any other type → `LoggedOn=false`. That is a **probe**, not a fill.

`CTraderFixLogonHostedService` calls `TryLogonAsync` twice (QUOTE **5211**, TRADE **5212**) and logs “NewOrderSingle still unimplemented.” Logon is not a ticket.

**Scoped to `CTraderFixSession.cs`: claim 1 is proven.**

### 1.2 Unscoped “no 35=D builder” is false on HEAD

Same folder, product-wired hop:

| File | Evidence |
|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 | `await Write(ssl, Build("D", sender, target, seq, extra.ToArray()), …)` |
| `CTraderFixCopyOpen.Build` L142–156 | generic assembler: first field `(35, type)` |
| `CTraderFixDemoTestTrade.cs` | `Build("D", …)` ×3 |
| `CTraderFixDemoMatrix.cs` L93 | `Build("D", …)` |
| `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566 | calls `CTraderFixCopyOpen.SendAsync` |
| `CopyTradingHostedService` L30 | `await copy.ExecuteDemoCopyAsync(stoppingToken)` every **20 s** |

`NewOrderSingleImplemented` is **not** `const false`:

```45:50:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public bool DemoDest =>
        (_config["CTRADER_FIX_HOST"] ?? "").StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
        && (_config["CTRADER_FIX_TRADE_SENDER_COMP_ID"] ?? "").StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
        && _config["CTRADER_FIX_ACCOUNT_ID"] != "1369850";

    public bool NewOrderSingleImplemented => DemoDest;
```

Lab `.env` (booleans / public ids only): `CTRADER_FIX_HOST=demo-us-eqx-01.p.c-trader.com`, `CTRADER_FIX_ACCOUNT_ID=5328266`, `CTRADER_FIX_TRADE_SENDER_COMP_ID=demo.pepperstone.5328266` → **`DemoDest=true`**. Live identity `1369850` is refused inside `CTraderFixCopyOpen` L37–42.

On-disk ledger `D:\Prop\data\demo_copy_ledger.json` records dest pos `237339770` / source `305750` / 0.01 lot / dest px `4390.2` / `DestClosed=false`. That is a **file** of a prior demo dest fill, **not** a this-slot send.

`LiveCopyPage.tsx` L57 itself says dest auto-sends after `ADMITTED`. A page that documents a sender cannot prove “no builder.”

**Unscoped claim 1 FAIL.** Older “product `35=D=0` / `NOS=const false`” pins are **STALE**.

---

## 2. PASS — `CanPromoteToLive` is false

Live file: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212 lines). Full re-read.

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

`FromBaseline` (L189–207) can return only:

| Condition | State |
|---|---|
| 0 completed XAU | `INSUFFICIENT_DATA` |
| risk ≥ 80 **or** (martingale ∧ DD>0 ∧ net<0) | `RISK_BLOCKED` |
| not early-eligible (`<3` XAU) | `INSUFFICIENT_DATA` |
| quality ≥ 70 ∧ risk < 40 | **`SHADOW`** (ceiling) |
| quality ≥ 55 | `WATCH` |
| else | `EARLY_SCORE` |

**Never** `LIVE_CANDIDATE`. **Never** `LIVE`. The enum still lists those values (`TraderState.cs` L9–10). The machine does not emit them.

Unit proof: `tests/Unit/BaselineScorerTests.cs` L21–26 — three disciplined winners → `SHADOW`; `CanPromoteToLive(score.SuggestedState).Should().BeFalse()`.

`RiskEngine` does not read `TraderState` at all. Promotion is not hiding in the risk hop.

**Claim 2 proven from the file.**

---

## 3. FAIL — `RealCopyEnabled` is **not** forced false after logon

This is the claim that kills the card.

### 3.1 Logon host only **reads** the flag

`D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` L60–70 (full file re-read):

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

There is **no** `_runtime.RealCopyEnabled = false` after logon. The log line **prints** the already-bound value. Older A015 / CREDENTIALS / synthesis “forced false after logon” quotes are **STALE**.

Product `*.cs` assignment of `RealCopyEnabled` is **exactly one**:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

### 3.2 Lab env arms the flag

`D:\Prop\.env` L73: `REAL_COPY_EXECUTION_ENABLED=true`.

API startup (`apps/api/Program.cs` L10–15) loads that env file, then `AddTraderIntelligence`. `/api/health` L55 and `/api/settings` L76 expose `runtime.RealCopyEnabled`. This slot could **not** GET those routes (SSRF). File path says: if the API process loaded this `.env`, the runtime flag is **true**, not forced false.

`RiskEngine` L90–93: `RealExecutionEnabled == false` is an **empty comment**, not a reject. `AllowFixSend` L147–150 still ANDs `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. Persist hop then **overwrites** to `AllowFixSend = false` (CopyTradingService L324) regardless.

**Claim 3 disproved from files.** Flag-pin FAIL. Not a live `1369850` ticket.

---

## 4. PASS — sending now cannot be the profit path

“Profit path” ≠ “a socket can emit `35=D` on demo.” Dest expectancy after venue costs is the object.

### 4.1 Live Pepperstone send is dead in the persist hop

```20:21:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
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
                ...
                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
```

Four ANDs. `CanPromoteToLive => false` keeps `LIVE=0`. `VenueReconciled` is const false. Persist writes `AllowFixSend=false` even if `Evaluate` would have set true. The LIVE branch is **unreachable**.

`RiskEngine.Evaluate` can return `AllowFixSend=true` only when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Hosted hop passes `Reconciled = VenueReconciled` (**false**), so `Evaluate` rejects increasing actions with `VENUE_NOT_RECONCILED` (L84–85) before that AND. Test: `RiskEngineTests.Real_flag_false_never_allows_fix_send`.

### 4.2 Destination PnL is not measured

`EfDashboardQueries.GetOverviewAsync` L44–46:

```
shadowPnl,   // Sum(ShadowOrders.SourceVsShadowSlippage)
0,           // DestinationRealPnl  ← constructor literal
0,           // XauGross
0,           // XauNet
```

`GetTradersAsync` L118 writes `ShadowPnl=0` per row. `NetSourcePnl` is **reconstructed MT5 source** (L90–94). There is **no** dest mark-to-market field that can print a profit.

`ShadowCopyEngine.SimulateEntry` records **entry slippage vs a dest quote**, not dest realized PnL. If `DestinationQuotes` is empty, the persist hop never inserts a shadow fill (`quote is not null && Approve` at L337).

### 4.3 Demo hop send is not an edge

`ExecuteDemoCopyAsync` (L483–605) **bypasses** `RiskEngine.Evaluate`. No quote age, no spread cap, no `MaxSlippage` (RiskLimits L13 is unread by the hop). `MaxAutoLots=0.05` is a **source skip**, then **1:1** (`AllocationFactor=1m`) of whatever remains. Policy **requires** demo/contest (`NOT_DEMO_OR_CONTEST_GROUP`). Challenge-demo pass-target is adverse selection, not dest profit.

`LiveCopyPage.tsx` L11–20 shows `realCopyArmed` / `liveSends` / SHADOW counts from `/api/copy/status`. It does **not** show dest realized PnL. Empty-state L57 documents demo dest auto-send. The page is a **pipeline monitor**, not a P&L proof.

This slot did **not** re-GET `/api/overview`. Constructor `DestinationRealPnl=0` is file-proven. Historical synthesis dest **$0** / scored XAU **−$154,425** is a prior pin, **not** this-slot GET.

**Claim 4 proven:** flipping the flag / logging on / demo-`35=D` now is **not** a destination-profit path.

---

## 5. PASS — SHADOW on demo is not destination profit

### 5.1 SHADOW is a source-shape ceiling

`TraderStateMachine.FromBaseline` tops out at `SHADOW`. `AfterHighEarlyScore() => SHADOW`. `CanPromoteToLive => false`. A high early quality on three XAU winners is **not** dest expectancy (`BaselineScorerTests` L21–26).

`CopyTradingService.GenerateShadowIntentsAsync` allow-list is `{SHADOW, LIVE_CANDIDATE, LIVE}` (L202). Scorer never emits the last two. Intents persist as `SHADOW_ONLY` (L336).

### 5.2 Copy set is demo/contest by policy, not dest cash

```105:109:D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs
        if (!CopyGroupFilter.IsDemoOrContest(trader.GroupName))
        {
            reason = "NOT_DEMO_OR_CONTEST_GROUP";
            return false;
        }
```

`CopyGroupFilter.IsDemoOrContest` is a path-segment match on `demo` / `contest`. `CopyRosterEngine.Decide` **REMOVE**s non-demo/contest (`NOT_DEMO_OR_CONTEST_GROUP`). Eligible SHADOW names the hopper will admit are **challenge/demo/contest**, not a live Starwave book.

Scorer itself does **not** look at `GroupName`. A real-group login **can** sit in `TraderState.SHADOW` on the dashboard. That score is still **source reconstructed PnL**, not dest. Policy will not copy it.

### 5.3 The dollars on the page are the wrong venue

| Field | What it is | Dest profit? |
|---|---|---|
| `TraderRowDto.NetSourcePnl` | Σ completed reconstructed source trades | **No** |
| `OverviewDto.ShadowPnl` | Σ `SourceVsShadowSlippage` (paper entry slip) | **No** |
| `OverviewDto.DestinationRealPnl` | literal `0` | **No** (unmeasured) |
| `TraderRowDto.ShadowPnl` | literal `0` in `GetTradersAsync` | **No** |
| `LiveCopyPage` “SHADOW traders” | count of `TraderState.SHADOW` scores | **No** |

`LiveCopyPage` has no dest realized column. Intent table is broker/login/qty/status/risk — pipeline, not cash.

This slot **cannot** prove the live process’s SHADOW set is 100% `demo\yo-2step`/`demo\yo-payp` (GET blocked). File-proven: **SHADOW dollars are not destination profit**, and the **copy-eligible** SHADOW set is demo/contest-only.

**Claim 5 proven from files.** Historical synthesis “SHADOW +$78,276 / 100% demo” is a prior GET pin, not this slot.

---

## Live GET (this slot)

Attempted: `GET http://127.0.0.1:5000/api/health`.

Result: **`SSRF blocked: 127.0.0.1`**.

Therefore **no** this-slot proof of process `realCopyEnabled`, FIX LoggedOn, dest PnL, SHADOW group mix, or liveSends. File path + `.env` are the evidence. Do not greenwash a blocked GET into a 200.

---

## Residuals / stale pins this slot must not repeat

| Stale claim | HEAD fact |
|---|---|
| `CTraderFixLogonHostedService` sets `RealCopyEnabled=false` | **No assignment.** Log only. |
| DI hard-pins `RealCopyEnabled=false` | DI **binds** env (`L41`). `.env` L73 **`true`**. |
| Product has no `35=D` builder / `NOS=const false` | `NOS => DemoDest`. `Build("D")` on `CTraderFixCopyOpen`. Hosted 20 s hop calls it. |
| `SAFE_BY_ABSENCE` on every dest | True for live `1369850`. **False** for demo `5328266` when `DemoDest`. |
| Synthesis §1 `realCopyEnabled=false` as current process | **Not re-probed.** File path says env-bound **true** if this `.env` is loaded. |
| SHADOW +$78k is dest profit | Source reconstructed; dest constructor **$0**. |

---

## Verdict

**FAIL.**

- Claim 1 **FAIL** as written (product `35=D` builder exists and is hosted). `CTraderFixSession` itself is **PASS_SESSION** (`35=A` only).
- Claim 2 **PASS** (`CanPromoteToLive => false`; `FromBaseline` ceiling `SHADOW`).
- Claim 3 **FAIL** (logon does **not** force `RealCopyEnabled=false`; DI binds `.env` `true`).
- Claim 4 **PASS** (sending now is not a dest-profit path; persist LIVE send dead; dest PnL unmeasured `0`).
- Claim 5 **PASS** (SHADOW / demo source PnL is not dest realized).

Wanting the five claims to be jointly true does not make claim 3 true. Do not send live `1369850`. Do not treat demo dest fills or SHADOW source dollars as the profit path.

---

*End of P500_VERIFY_16. Product source was not modified. No `35=D` was built or sent by this slot. No secrets printed. `REAL_COPY` was not flipped by this slot.*
