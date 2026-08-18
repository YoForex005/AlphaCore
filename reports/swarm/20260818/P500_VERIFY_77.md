# P500_VERIFY_77 — Adversarial verifier (slot 77)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_77.md` |
| Agent / slot | P500 adversarial verifier **77** |
| Date | 2026-08-18 |
| Role | Independent re-read of four assigned files. **Do not trust sibling `P500_*` numbers.** FAIL any claim not proved from a file this slot opened or a live GET this slot performed. |
| Assigned files (full `read_file`) | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135/135**); `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (**212/212**); `D:\Prop\src\Domain\Risk\RiskEngine.cs` (**189/189**); `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (**70/70**) |
| Adjacent files (this pass) | `CTraderFixLogonHostedService.cs`; `DependencyInjection.cs` L39–42; `LiveRuntimeStatus.cs`; `CopyTradingService.cs`; `CopyTradingHostedService.cs`; `CTraderFixCopyOpen.cs` / `CTraderFixDemoTestTrade.cs` / `CTraderFixDemoMatrix.cs`; `ShadowCopyEngine.cs`; `EfTradingStore.PersistDemoShadowAsync`; `ReconstructionScoringService`; `EfDashboardQueries`; `XauUsdOneToOneCopyPolicy`; `CopyRosterEngine`; `DemoCopyLedger.cs`; `data\demo_copy_ledger.json`; `apps\api\Program.cs`; `BaselineScorerTests.cs`; `RiskEngineTests.cs`; `.env` booleans + public dest ids **only** |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Quoted only `REAL_COPY_EXECUTION_ENABLED=true`, `FEATURE_COPY_TRADING_ENABLED=true`, public dest ids `5328266` / `1369850`, public host `demo-us-eqx-01.p.c-trader.com`. |
| Secrets printed | **None.** Tag 554 / FIX / Manager / DB passwords not dumped. |
| Live attach / send this pass | **No.** No Manager Connect. No TLS. No Logon. No `35=D`. |
| Live GET this pass | `GET http://127.0.0.1:5000/api/copy/status` **blocked** (`web_fetch` SSRF on loopback). `open_page` on `:5000/api/copy/status`, `:5000/api/health`, `:5000/api/settings` **failed**. Process bits (`realCopyArmed`, quote/trade LoggedOn, dest PnL) are **not** remeasured. |
| SHA-256 this slot | **Not computed** (no shell). Evidence is line-cited file text. |

**Honesty rule:** wanting dest profit does not create an edge. A TLS Logon (`35=A`) is not a fill. A SHADOW label is not dest money. An armed `REAL_COPY` bit is not a ticket. Sibling `Build("D")` is not `CTraderFixSession`. A constructor `0` is not a venue mark. FAIL any claim this slot cannot prove from a file or a live GET.

Claims to confirm:

1. no `35=D` builder
2. `CanPromoteToLive` is false
3. `RealCopyEnabled` forced false after logon
4. sending now cannot be the profit path
5. SHADOW on demo is not destination profit

---

## 0. Verdict (binding)

**FAIL.**

Claim **3 is disproved** on disk: hosted logon does **not** assign `RealCopyEnabled = false`. The only product writer is DI from config. Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true`. Live GET of the process bit **failed** this slot — that does **not** rescue claim 3, because the claimed force-false assignment is absent.

Unscoped claim **1** is also false (product has hosted `Build("D")`). Unscoped claim **4** is not proved (demo dest hopper can send; dest DTO `0` is a constructor, not a mark). Claim **2** is proved. Claim **5** is proved as paper-identity (SHADOW/slippage ≠ dest cash) with AUTO_ADMIT residual.

One failed required confirmation ⇒ slot verdict **FAIL**. Live Pepperstone `1369850` is still refused by `CTraderFixCopyOpen`. That does **not** make claim 3 true.

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** on `CTraderFixSession.cs`. **FAIL** if read product-wide. | Assigned file 135/135: outbound tag 35 is `"A"` only (`BuildLogon` L96). This file: `35=D` / `(35, "D")` / `Build("D")` / `NewOrderSingle` = **0**. `WriteAsync` = **1** (that Logon). Sockets `using`-disposed. Product residual: sibling `Build("D")` ×5 + hosted `ExecuteDemoCopyAsync`. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(TraderState current) => false` at `BaselineScorer.cs` L211. `FromBaseline` reachable set has **no** `LIVE` / `LIVE_CANDIDATE`. Persist copies `SuggestedState` (`ReconstructionScoringService` L140). Unit test asserts the same (`BaselineScorerTests` L26). |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | `CTraderFixLogonHostedService` L60–70 stamps QUOTE/TRADE and **logs** `_runtime.RealCopyEnabled`. Zero assignments `_runtime.RealCopyEnabled = false` under `src/`. Sole product write is DI L41 from config. `.env` L73 `=true`. Process bit not live-GETted this slot. |
| 4 | sending now cannot be the profit path | **PASS** (live capital / persist hop / booked dest DTO). **FAIL** unscoped. | `CTraderFixSession` cannot send. Persist hop hard-`AllowFixSend=false` (L324) and `VenueReconciled=false` (L20). `CanPromoteToLive` is false. Overview `DestinationRealPnl` is constructor literal **0**. Residual: 20s `ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.Build("D")`; ledger dest `237339770` open. Dest DTO `0` is **not** a venue mark. |
| 5 | SHADOW on demo is not destination profit | **PASS** (paper identity) | `FromBaseline` SHADOW is a **score state**. `ShadowCopyEngine.SimulateEntry` writes in-memory `ShadowOrder` rows (`SHADOW_ONLY`). Trader `ShadowPnl` hardcoded **0** (`EfDashboardQueries` L118). Dest DTO literal **0**. Residual: SHADOW is dest `AUTO_ADMIT` floor (`IsTraderEligible` allows SHADOW). |

One-line:

```text
SLOT 77 FAIL. CTraderFixSession 35=A only; CanPromoteToLive=>false; SHADOW≠dest PnL. RealCopyEnabled is NOT forced false after logon (.env true + DI bind; host only logs). Persist hop cannot send; dest DTO 0 is constructor. Demo dest hopper residual (Build("D") + ledger 305750 open). Live GET blocked. Live 1369850 NONE.
```

---

## 1. Claim 1 — no `35=D` builder

### 1.1 Assigned file — PASS

Path: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`  
Read: **135 / 135**.

The type is a **one-shot TLS Logon probe**. Two types only: `CTraderFixSessionResult` + static `CTraderFixSession`. There is no order builder, no heartbeat, no quote subscribe, no sequence store, no `NewOrderSingle` identifier.

Only outbound MsgType:

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

`ssl.WriteAsync` is **one** call (L49) of those Logon bytes. `Extract(reply, "35")` (L55) is **inbound** only (`msgType == "A"` accepts Logon; anything else is `"Logon rejected 35="`). `using TcpClient` / `await using SslStream` dispose on every return — no socket is kept for a later `35=D`.

Token census **this file only**:

| Pattern | Hits |
|---|---:|
| `35=D` | **0** |
| `(35, "D")` / `Build("D")` | **0** |
| `NewOrderSingle` | **0** |
| `(35, "A")` | **1** (`BuildLogon` L96) |
| `WriteAsync` | **1** (L49) |

The only product caller of `TryLogonAsync` is `CTraderFixLogonHostedService` (QUOTE **5211**, TRADE **5212**). That host never calls a `35=D` builder.

`RiskEngine.cs` and `LiveCopyPage.tsx` contain **0** FIX builders. `BaselineScorer.cs` contains **0** FIX builders.

### 1.2 Product-wide residual — do not greenwash

Unqualified “the product has no `35=D` builder” is **false**. This slot grepped `Build("D")` under `src\` and counted **5** writes:

| File | Encoder | Gate |
|---|---|---|
| `CTraderFixCopyOpen.cs` L95 / L142–156 | `Build("D", …)` | host must start `demo-`, sender `demo.`, account **≠ 1369850** |
| `CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | `Build("D", …)` | same demo-only refuse of live `1369850` |
| `CTraderFixDemoMatrix.cs` L93 (`SendD`) | `Build("D", …)` | same demo gate |

`CopyTradingHostedService` L30 calls `ExecuteDemoCopyAsync`, which calls `CTraderFixCopyOpen.SendAsync` (close L528, open L566). `NewOrderSingleImplemented => DemoDest` (`CopyTradingService` L50), **not** `const false`. Lab `.env` satisfies `DemoDest` (host `demo-us-eqx-01.p.c-trader.com` / sender `demo.pepperstone.5328266` / account **5328266** ≠ **1369850**).

Literal string `35=D` in product `*.cs` is still **0** (encoders use `Build("D")` / `(35, type)`). That is not “no builder.”

**Claim 1 result:** PASS if scoped to the assigned session file. FAIL if the claim is product-wide.

---

## 2. Claim 2 — `CanPromoteToLive` is false

**PASS.** Proved from assigned `BaselineScorer.cs` 212/212.

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

The method ignores `current`. There is no other `CanPromoteToLive` under product `*.cs` (only this definition, one unit assertion, one tmp harness).

`FromBaseline` reachable set (L189–206):

| Condition | State |
|---|---|
| `CompletedXauTrades == 0` | `INSUFFICIENT_DATA` |
| `risk >= 80` or (martingale + DD + net < 0) | `RISK_BLOCKED` |
| `!earlyEligible` (< 3 completed XAU) | `INSUFFICIENT_DATA` |
| `quality >= 70 && risk < 40` | `SHADOW` |
| `quality >= 55` | `WATCH` |
| else | `EARLY_SCORE` |

**No** `LIVE`. **No** `LIVE_CANDIDATE`. Enum values exist (`TraderState.cs` L9–10) but the scorer never emits them.

Persist copies the scorer, it does not promote:

```126:144:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore
        {
            // ...
            CurrentState = score.SuggestedState,
            LastScoredAt = DateTimeOffset.UtcNow
        }, ct);

        await _store.PersistDemoShadowAsync(brokerId, login, score.SuggestedState, completedXau, ct);
```

Unit lock: `BaselineScorerTests` L20–26 — three disciplined winners go to `SHADOW`; `CanPromoteToLive` is `false`.

`LiveCopyPage.tsx` displays `liveTraders` from API; it has **no** promote button.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon

**FAIL.** Disproved from files. Live GET did not remeasure the process bit; absence of the assignment is enough.

### 3.1 Assigned files have no such force

- `CTraderFixSession.cs`: no `RealCopyEnabled` symbol. Logon success returns `LoggedOn = true` / `Status = "LoggedOn"`. It does not touch runtime flags.
- `BaselineScorer.cs`: no runtime flag.
- `RiskEngine.cs`: **reads** `request.RealExecutionEnabled`. If false, comment at L91–93 says shadow still evaluates; it does **not** write a runtime pin. `AllowFixSend` later requires the flag true **and** kill-switch none **and** reconciled **and** venue healthy (L147–150).
- `LiveCopyPage.tsx`: displays `status?.realCopyArmed`. No setter.

### 3.2 The only logon host does not re-pin

After both `TryLogonAsync` calls, `CTraderFixLogonHostedService` stamps QUOTE/TRADE and **logs** the existing bit:

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

Grep `_runtime.RealCopyEnabled =` under `src\` = **0**. Grep `RealCopyEnabled =` under `src\` = **1** hit: DI constructor bind.

### 3.3 Sole writer is startup config bind

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Lab `.env` L73 (boolean only): `REAL_COPY_EXECUTION_ENABLED=true`.  
Also `.env` L106: `FEATURE_COPY_TRADING_ENABLED=true`.

`LiveRuntimeStatus.RealCopyEnabled` is a public `{ get; set; }` defaulting to `false` only until DI overwrites it. API `/api/health` and `/api/settings` expose the runtime bit (`Program.cs` L55, L76). This slot could **not** GET those endpoints.

POCO default `CTraderFixOptions.RealCopyExecutionEnabled = false` (`CTraderFixOptions.cs` L35) is a **different type**. It is not the hosted `LiveRuntimeStatus` bit and is not written after logon.

`apps/api/appsettings.json` `FeatureFlags.LiveCopyEnabled` is `false` and `AutoPromotionEnabled` is `false`. Those keys are **not** what DI binds to `LiveRuntimeStatus.RealCopyEnabled`.

**Claim 3 is false.** “Host forces false after logon” is stale.

---

## 4. Claim 4 — sending now cannot be the profit path

Split, because the unqualified sentence is not one fact.

### 4.1 Assigned hop / live Pepperstone — PASS (cannot be live-capital profit)

`CTraderFixSession` sends only `35=A` and disposes the socket. It cannot be a fill path.

`RiskEngine.Evaluate` can return `AllowFixSend=true` only when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Rejects always set `AllowFixSend=false` (L187). Unit: `Real_flag_false_never_allows_fix_send` (`RiskEngineTests` L21–26).

The persist hop **throws that decision away**:

```20:20:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
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

`Reconciled = VenueReconciled` is passed into Evaluate (L304), so `decision.AllowFixSend` cannot be true for an increasing action (unreconciled reject at `RiskEngine` L84–85). Even if it were, persist stores `AllowFixSend = false` and the LIVE branch is dead (`CanPromoteToLive => false` + `VenueReconciled=false`).

`CTraderFixCopyOpen` refuses live identity: `account == "1369850"` returns before any `Build("D")` (L37–41).

Overview dest PnL is **not measured**:

```33:47:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
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

`OverviewDto` field order (`DashboardModels.cs` L15–16): `ShadowPnl`, then `DestinationRealPnl`. The first `0` after `shadowPnl` is dest PnL. That is a **constructor literal**, not a venue mark. It does not prove dest cash is zero. It proves dest cash is **not booked**.

`LiveCopyPage.tsx` L24 header: “Live send blockers (Pepperstone cannot be filled)”. UI honesty for live Pepperstone.

### 4.2 Unscoped “sending cannot be the profit path” — FAIL (not proved; dest send exists)

`CopyTradingHostedService` every 20s:

```28:30:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

`ExecuteDemoCopyAsync` does **not** consult `RealCopyEnabled`, `CanPromoteToLive`, or `RiskEngine`. Gate is `DemoDest` + password present. Then it calls `CTraderFixCopyOpen.SendAsync`, which encodes `Build("D")`.

Lab `.env` makes `DemoDest` true (public values only):

- L49 `CTRADER_FIX_HOST=demo-us-eqx-01.p.c-trader.com`
- L50 `CTRADER_FIX_ACCOUNT_ID=5328266`
- L64 `CTRADER_FIX_TRADE_SENDER_COMP_ID=demo.pepperstone.5328266`

`data\demo_copy_ledger.json` this slot (no secrets):

| Field | Value |
|---|---|
| SourceLogin | `305750` |
| SourcePositionId | `21250421` |
| Lots | `0.01` |
| DestPositionId | `237339770` |
| DestClOrdId | `C20260818093047317` |
| DestFillPrice | `4390.2` |
| DestClosed | `false` |

That is an **open dest position record** on demo dest `5328266`. CopyTradingService L500–511 will re-seed this same tuple if missing. This slot did **not** live-GET dest cash, so dest P&L is **unmeasured**. Unmeasured ≠ “cannot be the profit path.”

`LiveCopyPage.tsx` L57: “Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position.” The UI contradicts an unqualified no-send claim.

**Claim 4 result:** PASS for live `1369850` / persist hop / booked dest DTO. FAIL if the claim is “no send can produce dest P&L now.” Dest send is wired; dest P&L is not booked and not live-GETted.

---

## 5. Claim 5 — SHADOW on demo is not destination profit

**PASS** as paper identity. Residual: SHADOW is the dest AUTO_ADMIT floor.

### 5.1 SHADOW is a score state, not dest cash

`FromBaseline` can emit `SHADOW` (L200–201) after 3+ completed XAU with quality ≥ 70 and risk < 40. That is a **source-book** label.

`PersistDemoShadowAsync` (`EfTradingStore.cs` L251–337): if state ≠ SHADOW, return. Else `ShadowCopyEngine.SimulateEntry` against a stored quote. Status `"SHADOW_ONLY"`. No FIX write.

`ShadowCopyEngine.SimulateEntry` (L35–60) computes a modeled fill + `SourceVsShadowSlippage`. No socket.

Overview `ShadowPnl` (`EfDashboardQueries` L29) is `Sum(ShadowOrders.SourceVsShadowSlippage)` — slippage vs source, not dest realized cash.

Trader row `ShadowPnl` is hardcoded **0** (`EfDashboardQueries` L118). Dest DTO is constructor **0** (L44).

### 5.2 Residual — do not confuse identity with dest-safety

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` / blocked states (L75–85). It does **not** reject `SHADOW`. SHADOW + 20 completed XAU + net > 0 + demo/contest group = eligible.

`CopyRosterEngine.Decide` AUTO_ADMIT when eligible (L72–80). `TickRosterAsync` writes `Status = "ADMITTED"`. `ExecuteDemoCopyAsync` sends dest `35=D` for ADMITTED seats with open XAU ≤ 0.05 lots — **state is not re-checked** (only roster row + open reconstructed trade).

So: SHADOW **paper** is not dest profit. SHADOW **can** be the admit class that the hopper copies onto demo dest. That residual does not turn the SHADOW slippage ledger into dest cash. It does mean “SHADOW on demo cannot become dest exposure” is **false**.

This slot did not live-GET dest PnL. Paper identity is still file-proved.

---

## 6. Live GET (required; failed)

Attempted this slot:

| URL | Tool | Result |
|---|---|---|
| `http://127.0.0.1:5000/api/copy/status` | `web_fetch` | SSRF blocked (loopback) |
| `http://127.0.0.1:5000/api/copy/status` | `open_page` | failed to retrieve |
| `http://localhost:5000/api/health` | `open_page` | failed to retrieve |
| `http://127.0.0.1:5000/api/settings` | `open_page` | failed to retrieve |

No process body. Launch profile listens on `http://localhost:5000` (`apps\api\Properties\launchSettings.json` L17). Whether the API is up is **unproved**. Claim 3 does not need the GET: the force-false assignment is not in the source.

---

## 7. Capital / residual

| Book | This slot |
|---|---|
| Live Pepperstone `1369850` | **NONE.** CopyOpen refuses that account before encode. Session file cannot send `35=D`. Persist hop `AllowFixSend=false` + `VenueReconciled=false`. `CanPromoteToLive => false`. |
| Demo dest `5328266` | **Not absent.** Hosted 20s hopper + `Build("D")` + ledger dest `237339770` open. Dest PnL **unmeasured** (DTO constructor `0`; live GET blocked). |
| This slot | Sent **0**. Did not flip `REAL_COPY`. Did not print secrets. Did not modify product. |

---

## 8. What this slot will not claim

- Will not claim “EX5 / MQ5” anything (wrong tree).
- Will not claim dest cash is +EV or zero.
- Will not claim `REAL_COPY` is false in the running process (GET blocked; `.env` is `true`).
- Will not claim product-wide “no `35=D` builder.”
- Will not claim SHADOW cannot admit to the dest hopper.

End of P500_VERIFY_77. Product source was not modified. No secrets printed. This slot did not send `35=D`.
