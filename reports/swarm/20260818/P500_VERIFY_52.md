# P500_VERIFY_52 — Adversarial: session / promote / RealCopy pin / send-as-profit / SHADOW≠dest

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_52.md` |
| Slot | **52** |
| Agent | P500_VERIFY_52 (adversarial verifier; independent HEAD re-read) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Topic | Confirm five claims from assigned files. **FAIL any claim not proven from a file or live GET.** |
| Assigned SUT | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` |
| Adjacent (needed to prove/disprove 1, 3–5) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `ShadowCopyEngine.cs`, `CopyRosterEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `apps/api/Program.cs`, `data/demo_copy_ledger.json`, `.env` **booleans / public dest ids only** |
| Product source modified | **No.** Report + INDEX / SWARM_LOG / `P500_MANIFEST.tsv` pins only. |
| Test source modified | **No.** |
| Config / `.env` edited | **No.** `REAL_COPY` not flipped. |
| Live `35=D` / NewOrderSingle this slot | **Not sent. Not constructed.** |
| Secrets printed | **None.** Quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73), public dest id `5328266`, live refuse id `1369850`. No password, token, or `554=`. |
| Localhost API this slot | **Attempted, blocked.** `GET http://127.0.0.1:5000/api/health`, `/api/settings`, `/api/copy/status` → worker HTTP `SSRF blocked: 127.0.0.1`. Runtime `realCopyEnabled` **not** live-GET proven. Claim 3 is file-disproven. |

**Honesty rule:** Chat is not evidence. Prior slots that still say “product `35=D=0`”, “`NOS=const false`”, or “logon re-pins `RealCopyEnabled=false`” are **STALE vs this HEAD re-read**.

```text
CTraderFixSession outbound is 35=A only (1 WriteAsync).
Product 35=D builders exist (CopyOpen / DemoTestTrade / DemoMatrix).
CanPromoteToLive => false.
RealCopyEnabled is NOT forced false after logon (sole write = DI bind; .env true).
Hosted 20s tick can 35=D on demo dest; live 1369850 refused.
SHADOW ledger is SimulateEntry / slippage, not dest P&L.
Live GET this slot = SSRF blocked.
This slot sent 0.
```

---

## 0. Verdict (binding)

**FAIL.** Two of five claims are file-proven as stated. Three fail the assigned FAIL-if-unproven / FAIL-if-disproven rule.

| # | Claim | Verdict | Why |
|---|---|---|---|
| 1 | no `35=D` builder | **FAIL** (product / unscoped). **PASS_SCOPED** on `CTraderFixSession.cs` only | Assigned session file is Logon `35=A` only. Same folder + hosted hop assemble and send `35=D`. Unqualified “no builder” is false. |
| 2 | `CanPromoteToLive` is false | **PASS** | Literal `=> false` in the assigned scorer file. `FromBaseline` never returns `LIVE`. Unit lock exists. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL (disproven)** | Logon host **reads** the flag; **never assigns** it. Sole write in product C# is DI bind from `REAL_COPY_EXECUTION_ENABLED`. `.env` L73 is `true`. Live GET of `/api/settings` not available this slot. |
| 4 | sending now cannot be the profit path | **FAIL (contradicted)** | Demo dest auto-copy **is** a send path (`ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.Build("D")`). On-disk dest fill exists. Dashboard dest PnL `0` is a constructor constant, not proof dest has no P&L. Live `1369850` refuse is **not** the whole claim. |
| 5 | SHADOW on demo is not destination profit | **PASS** | `SHADOW` is a source `TraderState`. `ShadowOrder` is `SimulateEntry`. Overview `ShadowPnl` is slippage sum. `DestinationRealPnl` is literal `0` (not sourced from shadow). Residual: a SHADOW+`ADMITTED` seat can still trigger **demo dest** `35=D`. That dest fill is still not the SHADOW number. |

```text
OVERALL = FAIL
  because claim 3 is disproven from HEAD files
  and claims 1/4 cannot be confirmed as stated.

PASS only: (2) CanPromoteToLive; (5) SHADOW ledger ≠ dest profit.
```

**Risk to capital:** **NONE on live `1369850`** (`CTraderFixCopyOpen` refuse + session hop `35=A` only + persist `AllowFixSend=false` + `VenueReconciled=const false` + `CanPromoteToLive=>false`). **Not absent on demo dest `5328266`** (hosted 20 s hop; ledger 305750 / dest 237339770 / 0.01 / 4390.2 / `DestClosed=false`). This slot sent **0**.

---

## 1. What was read (HEAD, this slot)

| File | Lines (this read) | Used for |
|---|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 135 | Claim 1 |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | 212 | Claim 2 |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | 189 | Claim 3–4 (`AllowFixSend` formula) |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | 70 | Claim 4–5 (UI honesty) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | 112 | Claim 3 |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 61 | Claim 3 (sole write) |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | 66 | Claim 3 |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | 625 | Claim 1, 3, 4, 5 |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 44 | Claim 4 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | 223 | Claim 1, 4 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | L139/L163/L197 | Claim 1 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | L93 `SendD` | Claim 1 |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | 91 | Claim 5 |
| `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs` | 136 | Claim 4–5 (ADMIT SHADOW) |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | 188 | Claim 4–5 (SHADOW floor) |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | dest ctor L33–52 | Claim 4–5 |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | L5–22 | Claim 4–5 (`DestinationRealPnl`) |
| `D:\Prop\apps\api\Program.cs` | 159 | Claim 3 (exposes runtime flag) |
| `D:\Prop\data\demo_copy_ledger.json` | 11 | Claim 4 (on-disk dest fill) |
| `D:\Prop\reports\swarm\20260818\DEMO_COPY_OPEN.json` | fill record | Claim 4 (prior dest ER `150=F`) |
| `D:\Prop\.env` L49/L50/L56/L64/L73 | booleans + public ids | Claim 3–4 (`DemoDest` + REAL_COPY) |
| `tests/Unit/BaselineScorerTests.cs` | 74 | Claim 2 lock |
| `tests/Unit/RiskEngineTests.cs` | 86 | Claim 4 (`AllowFixSend` when flag false) |

No password, token, or FIX `554=` value is quoted.

---

## 2. Claim 1 — no `35=D` builder — **FAIL** (product) / **PASS_SCOPED** (`CTraderFixSession`)

### 2.1 Assigned file: no NewOrderSingle assembler

`CTraderFixSession` has one outbound builder, `BuildLogon`, and one `WriteAsync`. Tag 35 is hardcoded `"A"`. The only other `35` uses extract the **inbound** logon reply.

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

Grep of this file: three `35` hits — inbound extract L55, reject text L73, outbound `(35, "A")` L96. **Zero** `"D"`. Sockets disposed via `using`. **PASS for this type.**

### 2.2 Product-wide “no builder” is false

Same namespace `TraderIntelligence.Fix.CTrader.Sessions` contains three `Build(type, …)` helpers that accept `"D"` and write it:

| Type | Outbound `Build("D"` / `SendD` | Role |
|---|---|---|
| `CTraderFixCopyOpen` | L95 | **Hosted** dest open/close |
| `CTraderFixDemoTestTrade` | L139, L163, L197 | Demo test flatten / open / close |
| `CTraderFixDemoMatrix` | L93 (`SendD`) | Demo scenario matrix |

`CopyTradingHostedService` (20 s) calls `ExecuteDemoCopyAsync`, which calls `CTraderFixCopyOpen.SendAsync` when `DemoDest` is true. `.env` host starts with `demo-`, trade sender starts with `demo.`, account is `5328266` (≠ `1369850`) → **`DemoDest` is true** from files.

```37:42:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
        if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
            || !sender.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
            || account == "1369850")
        {
            return Fail("Refused: dest must be demo FIX, not live 1369850.", host, account, sourceLogin);
        }
```

```95:95:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
            await Write(ssl, Build("D", sender, target, seq, extra.ToArray()), timeout.Token);
```

```142:148:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
    private static string Build(string type, string sender, string target, int seq, params (int, string)[] extra)
    {
        var fields = new List<(int, string)>
        {
            (35, type), (34, seq.ToString(CultureInfo.InvariantCulture)),
            (49, sender), (56, target), (50, "TRADE"), (57, "TRADE"),
            (52, DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture))
        };
```

`NewOrderSingleImplemented => DemoDest` (`CopyTradingService` L50). Reports that still say `NOS=const false` or “product `35=D=0`” are **STALE**.

**Claim 1 as stated cannot be confirmed.** FAIL.

---

## 3. Claim 2 — `CanPromoteToLive` is false — **PASS**

The symbol lives in the assigned scorer file, not a separate type file.

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

`FromBaseline` max state is `SHADOW`. `LIVE` / `LIVE_CANDIDATE` are never returned. `current` is unused. Unit lock:

```20:27:D:\Prop\tests\Unit\BaselineScorerTests.cs
    public void Three_disciplined_winners_go_to_shadow_not_live()
    {
        var score = _s.Score(new[] { Closed(1, 80), Closed(2, 70), Closed(3, 90) });
        score.EarlyScoreEligible.Should().BeTrue();
        score.SuggestedState.Should().Be(TraderState.SHADOW);
        TraderStateMachine.CanPromoteToLive(score.SuggestedState).Should().BeFalse();
    }
```

**PASS.** File-proven.

---

## 4. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL (disproven)**

### 4.1 Sole write is DI bind, not logon

Workspace grep of `RealCopyEnabled\s*=` in `*.cs` returns **one** assignment:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`.env` L73 (boolean only): `REAL_COPY_EXECUTION_ENABLED=true`.

API boot loads that file (`EnvFile.FindAndLoad()` in `apps/api/Program.cs` L10) then `AddTraderIntelligence`. `appsettings.json` does **not** define `REAL_COPY_EXECUTION_ENABLED`. Docs/README that still say the flag is `false` are **STALE vs `.env`**.

### 4.2 Logon does not re-pin

`CTraderFixLogonHostedService.ExecuteAsync` updates Quote/Trade `LoggedOn` / `Status` / `LastError` / `UpdatedAt` only. It **logs** `_runtime.RealCopyEnabled`. It never assigns it.

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

Even if `.env` were `false`, logon still would **not force** false after logon — there is no post-logon write. The claim is not “flag happens to be false”; it is “forced false after logon.” That write does not exist.

`LiveRuntimeStatus.Snapshot()` copy-note still claims “NewOrderSingle still unimplemented” / “No ticket will be sent” when armed — that string is **STALE vs** hosted `ExecuteDemoCopyAsync`.

Live GET of `/api/settings` (`featureFlags.REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled`) and `/api/health` (`realCopyEnabled`) **blocked** this slot. Runtime value is therefore **file-inferred**, not live-GET proven. File evidence is already enough to **disprove** a post-logon force-false.

**FAIL.** Disproven.

---

## 5. Claim 4 — sending now cannot be the profit path — **FAIL**

Two hops exist. Only one can send.

### 5.1 Persist / risk hop cannot send (true, not sufficient)

`CopyTradingService.VenueReconciled = const false` (L20). `GenerateShadowIntentsAsync` passes that into `RiskEngine.Evaluate`. Increasing exposure with `Reconciled=false` returns `VENUE_NOT_RECONCILED` and `AllowFixSend=false` (`RiskEngine` L84–85, L180–187). Persist then **overwrites** anyway:

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

`CanPromoteToLive => false` plus `VenueReconciled=const false` makes the LIVE-send `if` dead. This hop is **not** dest profit.

`RiskEngine` would allow send only when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Persist never stores that `true`. Assigned `RiskEngine` is therefore **not** the dest sender.

### 5.2 Hosted demo hop **is** a dest send / dest P&L path

```19:33:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var copy = scope.ServiceProvider.GetRequiredService<CopyTradingService>();
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

`ExecuteDemoCopyAsync` (L483–605) **does not call** `RiskEngine.Evaluate`. When `DemoDest` it opens/closes via `CTraderFixCopyOpen.SendAsync` → `Build("D")`. It seeds/keeps ledger row source `305750` / dest `237339770` / 0.01 / 4390.2 if missing.

On-disk `D:\Prop\data\demo_copy_ledger.json` (this read):

- SourceLogin `305750`, SourcePositionId `21250421`
- DestPositionId `237339770`, DestClOrdId `C20260818093047317`
- DestFillPrice `4390.2`, Lots `0.01`, **`DestClosed=false`**

Prior fill record `DEMO_COPY_OPEN.json` (public dest only): `Filled=true`, `LastPx=4390.2`, `PosId=237339770`, `Account=5328266`, ER contains `|150=F|` / `|39=2|`. No `554=` quoted.

`LiveCopyPage.tsx` L57 is explicit: *“Demo dest auto-sends after a trader is ADMITTED…”*

Dashboard `DestinationRealPnl` is the 11th `OverviewDto` arg and is the literal `0` in `EfDashboardQueries.GetOverviewAsync` (L44), after `shadowPnl` (L29, L43). That `0` is a **constructor constant**, not a mark-to-market of dest `237339770`. It cannot prove “sending cannot be the profit path.”

Live `1369850` refuse (`CTraderFixCopyOpen` L39) proves **live Pepperstone** is not the dest. It does not prove sending cannot book dest P&L.

**Claim 4 as stated is contradicted.** FAIL.

---

## 6. Claim 5 — SHADOW on demo is not destination profit — **PASS**

Proven from assigned + adjacent files:

1. `TraderState.SHADOW = 3` is a **source** score state (`TraderState.cs`). `FromBaseline` can emit it; it is not dest cash.
2. `XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` with `TRADER_NOT_SHADOW_YET` (L81–85). SHADOW (or higher) is the **eligibility floor**, not dest PnL.
3. Persist hop writes `ShadowOrder` only via `_shadow.SimulateEntry(...)` (`CopyTradingService` L339–359). `ShadowCopyEngine.SimulateEntry` uses quote bid/ask + 0.05 modeled slip. **No socket. No `35=D`.**
4. Overview `ShadowPnl` = `Sum(ShadowOrders.SourceVsShadowSlippage)` (`EfDashboardQueries` L29). That is a **slippage sum**, not dest realized.
5. Overview `DestinationRealPnl` is literal `0` (`DashboardModels` L16; queries L44). It is **not** read from SHADOW rows and **not** read from `demo_copy_ledger.json`.
6. `LiveCopyPage` shows `shadowTraders` / `shadowFills` as counts. It does not treat those as dest cash.

**Residual (does not flip the claim):** `CopyRosterEngine` can `AUTO_ADMIT` an eligible SHADOW demo/contest trader; `ExecuteDemoCopyAsync` can then `35=D` on dest `5328266`. That dest fill is dest exposure, **not** the SHADOW ledger number. Claim as stated: SHADOW-on-demo **is not** destination profit. **PASS.**

If a later slot rewrites the claim as “SHADOW cannot gate dest send,” that rewrite would **FAIL**. This slot was not given that rewrite.

---

## 7. Live GET (attempted; not evidence of runtime flag)

| URL | Result |
|---|---|
| `GET http://127.0.0.1:5000/api/health` | Worker HTTP **SSRF blocked** (`127.0.0.1` private) |
| `GET http://127.0.0.1:5000/api/settings` | Same |
| `GET http://127.0.0.1:5000/api/copy/status` | Same |

No live JSON. File-only for claim 3 bind. Launch profile would have been `:5000` (`apps/api/Properties/launchSettings.json`).

---

## 8. What this slot did **not** do

- Did not send or construct `35=D`.
- Did not flip `.env` / `REAL_COPY_EXECUTION_ENABLED`.
- Did not edit product or test source.
- Did not print secrets.
- Did not claim dest `5328266` mark-to-market (dashboard dest is constructor `0`; ledger is an open fill record, not a PnL).
- Did not claim “EX5 decompiled” or any ≥95% figure.

---

## 9. Binding pins

| Pin | HEAD fact |
|---|---|
| `CTraderFixSession` outbound | `35=A` only; 1 `WriteAsync` |
| Product `35=D` builders | `CopyOpen` + `DemoTestTrade` + `DemoMatrix` |
| Hosted dest hop | `CopyTradingHostedService` 20 s → `ExecuteDemoCopyAsync` → `Build("D")` when `DemoDest` |
| `NewOrderSingleImplemented` | `=> DemoDest` (L50). const-false **STALE** |
| `CanPromoteToLive` | `=> false` |
| `RealCopyEnabled` write | DI L41 only; logon **read/log** only |
| `.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` |
| Persist `AllowFixSend` | hardcoded `false` L324 |
| `VenueReconciled` | `const false` L20; status DTO lies `DemoDest` at L67 |
| Live dest `1369850` | refused in `CTraderFixCopyOpen` |
| Demo dest | `.env` host `demo-…`, sender `demo.…`, account `5328266` |
| On-disk dest fill | `305750` → `237339770` / 0.01 / 4390.2 / open |
| SHADOW PnL | slippage sum; dest DTO `0` |
| This slot sent | **0** |
