# P500_VERIFY_46 — Adversarial four-file verify (slot 46)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_46.md` |
| Slot | **46** |
| Agent | P500_VERIFY_46 (adversarial verifier; independent HEAD re-read) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Topic | Confirm five claims from assigned files. **FAIL any claim not proven from a file or live GET.** |
| Assigned SUT | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` |
| Adjacent (needed to prove/disprove 1, 3–5) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `ShadowCopyEngine.cs`, `CopyRosterEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyGroupFilter.cs`, `EfDashboardQueries.cs`, `DemoCopyLedger.cs`, `apps/api/Program.cs`, `data/demo_copy_ledger.json`, `DEMO_COPY_OPEN.json`, `.env` **booleans / public dest ids only** |
| Product source modified | **No.** Report + INDEX / SWARM_LOG pins only. |
| Test source modified | **No.** |
| Config / `.env` edited | **No.** `REAL_COPY` was not flipped. |
| Live `35=D` / NewOrderSingle this slot | **Not sent. Not constructed.** |
| Secrets printed | **None.** Quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73), public dest id `5328266`, live refuse id `1369850`. No password / FIX `554=` / token. |
| Localhost API this slot | **Attempted, blocked.** `GET http://127.0.0.1:5000/api/copy/status`, `GET http://127.0.0.1:5080/api/settings`, `GET http://localhost:5000/api/health` → tool `SSRF blocked` (private IP). Runtime `realCopyEnabled` **not** live-proven. File-only for claim 3. |

**Honesty rule:** Chat, prior P500_VERIFY slots, and comments such as “NewOrderSingle still unimplemented” are **not** evidence. This slot re-read HEAD files. `VenueReconciled = const false` on the *risk persist hop* is not absence of the *demo dest hop*. `CanPromoteToLive => false` does not stop `ExecuteDemoCopyAsync`. Dashboard `DestinationRealPnl = 0` is a constructor, not a measured dest book.

```text
CTraderFixSession outbound is 35=A only.
Product 35=D builders exist (CopyOpen / DemoTestTrade / DemoMatrix).
CanPromoteToLive => false.
RealCopyEnabled is NOT forced false after logon (DI binds env; .env true).
Hosted 20s tick can 35=D on demo dest; live 1369850 refused.
SHADOW ledger is SimulateEntry, not dest P&L.
Live GET this slot = SSRF blocked.
```

---

## 0. Verdict (binding)

**FAIL.** Two of five claims are file-proven as stated. Three fail the assigned FAIL-if-unproven rule.

| # | Claim | Verdict | Why |
|---|---|---|---|
| 1 | no `35=D` builder | **FAIL** (product). **PASS_SCOPED** on `CTraderFixSession.cs` only | Assigned session file is Logon `35=A` only. Same folder + hosted hop assemble and send `35=D`. Unqualified “no builder” is false. |
| 2 | `CanPromoteToLive` is false | **PASS** | Literal `=> false`. `FromBaseline` never returns `LIVE`. Unit lock exists. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL (disproven)** | Logon host **reads** the flag; **never assigns** it. Sole write is DI bind from `REAL_COPY_EXECUTION_ENABLED`. `.env` L73 is `true`. Live GET of `/api/settings` not available this slot. |
| 4 | sending now cannot be the profit path | **FAIL (disproven as written)** | Demo dest auto-copy **is** a send path (`ExecuteDemoCopyAsync` → `Build("D")`). On-disk dest fill exists (open). Dashboard dest PnL `0` is a constructor lie, not proof dest has no P&L. Live `1369850` send is refused — that is **not** the whole claim. |
| 5 | SHADOW on demo is not destination profit | **PASS** | `SHADOW` is source-shape. `ShadowOrder` is `SimulateEntry`. Overview `ShadowPnl` is slippage sum. `DestinationRealPnl` is literal `0` (not sourced from shadow). Residual: a SHADOW+`ADMITTED` name can still trigger **demo dest** `35=D`. That dest fill is still not the SHADOW number. |

```text
OVERALL = FAIL
  because claim 3 is disproven from HEAD files
  and claim 1/4 cannot be confirmed as stated.

PASS only: (2) CanPromoteToLive; (5) SHADOW ledger ≠ dest profit.
```

**Risk to capital:** **NONE on live `1369850`** (`CTraderFixCopyOpen` refuse + session hop `35=A` only + persist `AllowFixSend=false` + `VenueReconciled=const false` + `CanPromoteToLive=>false`). **Not absent on demo dest `5328266`** (hosted 20 s hop; ledger source `305750` / dest `237339770` / 0.01 / 4390.2 / `DestClosed=false`). This slot sent **0**.

Operating mode (honest):

```text
ALLOW:  treat CTraderFixSession as logon-proof only
        treat CanPromoteToLive as a hard no-LIVE from scoring
        treat ShadowOrder / dashboard ShadowPnl as simulated, not dest $
FORBID: claim the process has no 35=D builder
        claim RealCopyEnabled is forced false after logon
        claim "sending now cannot be the profit path"
        treat dest DTO $0 as a measured dest book
        treat live GET as measured this slot
```

---

## 1. What was read (HEAD, this slot)

| File | Lines (this read) | Used for |
|---|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 135 | Claim 1 |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | 212 | Claim 2 |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | 189 | Claim 3–4 (`AllowFixSend`) |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | 70 | Claim 4–5 (UI honesty) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | 112 | Claim 3 |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 61 | Claim 3 (sole `RealCopyEnabled =`) |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | 66 | Claim 3 |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | 625 | Claim 1, 4, 5 |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 44 | Claim 4 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | 223 | Claim 1, 4 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | 391 | Claim 1 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | 287 | Claim 1 |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | 91 | Claim 5 |
| `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs` | 136 | Claim 4–5 (ADMIT SHADOW) |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | 188 | Claim 4–5 |
| `D:\Prop\src\Domain\Copy\CopyGroupFilter.cs` | 24 | Claim 5 (demo/contest only) |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | dest ctor | Claim 4–5 |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | `OverviewDto` | Claim 4–5 field names |
| `D:\Prop\src\Infrastructure\Copy\DemoCopyLedger.cs` | 40 | Claim 4 path |
| `D:\Prop\data\demo_copy_ledger.json` | 11 | Claim 4 (on-disk dest fill) |
| `D:\Prop\reports\swarm\20260818\DEMO_COPY_OPEN.json` | dest ER | Claim 4 prior fill |
| `D:\Prop\apps\api\Program.cs` | 159 | Claim 3 (exposes runtime flag) |
| `D:\Prop\apps\fix-worker\Worker.cs` | 50 | Adjacent (does **not** send) |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | 80 | Unused POCO default `false` |
| `D:\Prop\.env` L49/L50/L64/L73 | booleans + public ids | Claim 3–4 (`DemoDest` + REAL_COPY) |
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

This-slot grep of `CTraderFixSession.cs`: three `35` hits — inbound extract L55, reject text L73, outbound `(35, "A")` L96. **Zero** `"D"`. Sockets disposed via `using`. `Assemble` is generic but **no caller in this type** passes `"D"`. **PASS for this type.**

### 2.2 Product-wide “no builder” is false

Same namespace `TraderIntelligence.Fix.CTrader.Sessions` contains three `Build(type, …)` helpers that accept `"D"` and write it. This-slot grep of `src/Fix.CTrader`:

| Type | Outbound `Build("D"` / `SendD` | Role |
|---|---|---|
| `CTraderFixCopyOpen` | L95 | **Hosted** dest open/close |
| `CTraderFixDemoTestTrade` | L139, L163, L197 | Demo test flatten / open / close |
| `CTraderFixDemoMatrix` | L93 (`SendD`) | Demo scenario matrix |

`CopyTradingHostedService` (20 s) calls `ExecuteDemoCopyAsync`, which calls `CTraderFixCopyOpen.SendAsync` when `DemoDest` is true. Lab `.env`: host starts with `demo-` (L49), trade sender starts with `demo.` (L64), account is `5328266` (L50) ≠ `1369850` → **`DemoDest` is true** from files.

```95:95:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
            await Write(ssl, Build("D", sender, target, seq, extra.ToArray()), timeout.Token);
```

```142:149:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
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

`LiveCopyPage.tsx` L57 itself documents dest auto-send: “Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position.” A UI that tells the operator dest sends exist cannot prove “no builder.”

**Claim 1 as stated cannot be confirmed.** FAIL.

---

## 3. Claim 2 — `CanPromoteToLive` is false — **PASS**

```187:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
public static class TraderStateMachine
{
    public static TraderState FromBaseline(bool earlyEligible, decimal quality, decimal risk, FeatureSnapshot features)
    {
        if (features.CompletedXauTrades == 0)
            return TraderState.INSUFFICIENT_DATA;
        // ...
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

- `CanPromoteToLive` ignores `current` and returns **false**.
- Ceiling of `FromBaseline` is `SHADOW` (or `RISK_BLOCKED` / `WATCH` / `EARLY_SCORE` / `INSUFFICIENT_DATA`). **Never `LIVE` / `LIVE_CANDIDATE`.**
- Product callers of `CanPromoteToLive`: **one** — `tests/Unit/BaselineScorerTests.cs` L26 `Should().BeFalse()`. No production caller can flip it.
- `LiveCopyPage` does not call promote. It only displays `status?.liveTraders`. That cannot flip the scorer.

`TraderState` enum still *contains* `LIVE = 5`, so a row can be written by some other writer. That does not make `CanPromoteToLive` true. The assigned claim is the function. **PASS.** File-proven.

---

## 4. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL (disproven)**

### 4.1 Logon host does not write the flag

`CTraderFixSession.TryLogonAsync` never mentions `RealCopyEnabled`.

After both QUOTE and TRADE attempts, `CTraderFixLogonHostedService` updates session health and **logs** the current armed bit. There is **no** `_runtime.RealCopyEnabled = false`.

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

The log line *reads* `_runtime.RealCopyEnabled`. A comment “NewOrderSingle still unimplemented” is not an assignment.

### 4.2 Sole write is DI bind from env

This-slot grep of `RealCopyEnabled =` in `*.cs` (product): **one** assignment.

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`.env` L73 (boolean only): `REAL_COPY_EXECUTION_ENABLED=true`.

`LiveRuntimeStatus.RealCopyEnabled` is a public `{ get; set; }` with **no** post-logon pin. API exposes it:

- `GET /api/health` → `realCopyEnabled = runtime.RealCopyEnabled` (`Program.cs` L55)
- `GET /api/settings` → `featureFlags["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` (L76)

`CTraderFixOptions.RealCopyExecutionEnabled` defaults `false` (L35) and is **not** the runtime object DI binds. `apps/fix-worker/Worker.cs` L21 reads a **different** key `CTrader:RealCopyExecutionEnabled` (default false) and still does not send `35=D`. That worker is not a re-pin of `LiveRuntimeStatus.RealCopyEnabled`.

### 4.3 Live GET not available this slot

`GET :5000/api/health`, `:5000/api/copy/status`, `:5080/api/settings` were attempted and **blocked** (`SSRF blocked: 127.0.0.1` / `localhost`). Cannot live-prove the in-process bit. File bind + no re-pin is already enough to **disprove** “forced false after logon.”

**FAIL.** Claim is the opposite of HEAD.

---

## 5. Claim 4 — sending now cannot be the profit path — **FAIL**

Two hops exist. Conflating them is how stale reports stay green.

### 5.1 Risk persist hop cannot live-send

`CopyTradingService.VenueReconciled = const false` (L20). Hopper Evaluate sets `Reconciled = VenueReconciled` (L304). Persist **overwrites** `AllowFixSend = false` (L324) even if `RiskEngine` would have approved. Live-send branch L330 requires `decision.AllowFixSend && score.CurrentState == LIVE && NewOrderSingleImplemented && VenueReconciled` — `VenueReconciled` is false, so the branch is dead. Else branch writes `SHADOW_ONLY`.

`RiskEngine.Evaluate` can set `AllowFixSend=true` only when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Unit test `Real_flag_false_never_allows_fix_send` locks the flag-false case. The empty comment at RiskEngine L90–93 does **not** return; it is not a force-false.

This hop is **not** a dest P&L path. It is also **not** the only hop.

### 5.2 Demo dest hop *is* a send path and bypasses Evaluate

```483:530:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public async Task<int> ExecuteDemoCopyAsync(CancellationToken ct)
    {
        if (!DemoDest)
        {
            _log.LogInformation("Demo dest auto-copy skipped (host is not demo FIX).");
            return 0;
        }
        // ...
            var close = await CTraderFixCopyOpen.SendAsync(
                host, sender, target, account, password,
                fill.SourceLogin, fill.SourcePositionId, fill.IsLong, fill.Lots, ct, fill.DestPositionId);
```

`CopyTradingHostedService` L28–30, every 20 s: `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**.

`ExecuteDemoCopyAsync` does **not** call `RiskEngine.Evaluate`. It does **not** consult `AllowFixSend`. It does **not** consult `CanPromoteToLive`. Gate is `DemoDest` + password present + `ADMITTED` roster + open XAU ≤ `MaxAutoLots` (0.05). Open uses `CTraderFixCopyOpen.SendAsync` → `Build("D")`.

Policy comment (`XauUsdOneToOneCopyPolicy` L57–61): copy **does not wait** until a ticket is profitable. Sending **now** (next open event) is the designed dest path.

### 5.3 On-disk dest fill (not this slot)

`D:\Prop\data\demo_copy_ledger.json`:

- source `305750` / pos `21250421`
- dest `237339770` / cl `C20260818093047317`
- 0.01 lots / px `4390.2` / **`DestClosed=false`**

`DEMO_COPY_OPEN.json` ER (sanitized, no `554`): `35=8` `150=F` `39=2` `6=4390.2` `721=237339770` `Account=5328266`. That is a dest fill on demo, not a shadow row.

`GetStatusAsync` summary when `DemoDest` (L76–77): “Demo dest auto-copy ON. Eligible demo/contest opens send on the 20s tick…”

`LiveCopyPage` L57 empty-state: dest auto-sends after ADMITTED + open XAUUSD. L24 blockers say “Pepperstone cannot be filled” **only when `blockers.length > 0`**. When `DemoDest` is true, `BuildBlockers` **does not** add `SAFE_BY_ABSENCE` (L610–616).

### 5.4 Dashboard dest $0 is a lie, not a proof

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

`OverviewDto.DestinationRealPnl` (models L16) is the first `0`. That constructor does not read `demo_copy_ledger.json`. **$0 is unmeasured**, not “no dest P&L.”

Live `1369850` is refused (`CTraderFixCopyOpen` L37–41). That is **SAFE_BY_ABSENCE on live only**. The claim was not “live Pepperstone cannot be the profit path.”

**FAIL.** Sending now **can** be the dest fill / dest P&L path on demo dest `5328266`.

---

## 6. Claim 5 — SHADOW on demo is not destination profit — **PASS**

### 6.1 SHADOW book is paper

`ShadowCopyEngine.SimulateEntry` / `SimulateExit` compute a modeled price + `SourceVsShadowSlippage`. No socket. No `35=D`.

Hopper, after persist `AllowFixSend=false`, writes `SHADOW_ONLY` and optionally `ShadowOrder` from `SimulateEntry` (`CopyTradingService` L336–359).

Overview `ShadowPnl` = `Sum(ShadowOrders.SourceVsShadowSlippage)` (`EfDashboardQueries` L29). That is a slippage sum, not dest cash.

`DestinationRealPnl` is **not** sourced from `ShadowOrders`. It is literal `0` (L44).

`FromBaseline` ceiling is `SHADOW`. `AfterHighEarlyScore()` returns `SHADOW`. Scoring cannot mint dest P&L.

### 6.2 Residual: SHADOW is dest-eligible, dest fill is still not the SHADOW number

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET` (L81–85). Eligible states are `SHADOW` / `LIVE_CANDIDATE` / `LIVE` (plus 20 completed XAU, net > 0, demo/contest group).

`CopyRosterEngine.Decide` AUTO_ADMITs when eligible. `ExecuteDemoCopyAsync` sends dest `35=D` for `ADMITTED` seats. `CopyGroupFilter` **requires** demo/contest path segments.

So a SHADOW demo name **can** cause a dest send. That dest fill is recorded in `DemoCopyFill` (`DestFillPrice`, `DestPositionId`), **not** in `ShadowOrder`. The SHADOW number remains slippage. Dest P&L is still not booked on the dashboard.

**PASS.** SHADOW-on-demo is not the dest-profit ledger. Residual dest hop is claim 4, not a rewrite of the SHADOW column.

---

## 7. Live GET (required attempt)

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/copy/status` | **blocked** (`SSRF blocked: 127.0.0.1`) |
| `http://127.0.0.1:5080/api/settings` | **blocked** (same) |
| `http://localhost:5000/api/health` | **blocked** (`localhost` → `127.0.0.1`) |

No runtime JSON this slot. File bind is enough to fail claim 3. Claims 1, 2, 4, 5 do not need a live GET.

---

## 8. Stale pins (do not recycle)

| Pin | Status vs this HEAD |
|---|---|
| Product `35=D=0` / single FIX writer | **STALE** — three `Build("D")` types; hosted CopyOpen |
| `NewOrderSingleImplemented = const false` | **STALE** — `=> DemoDest` L50 |
| Logon re-pins `RealCopyEnabled=false` | **STALE** — logon logs only; DI binds env |
| `CREDENTIALS_AND_COPY_STATUS.md` “REAL_COPY false (forced)” | **STALE** vs `.env` L73 `true` |
| README / docs `REAL_COPY_EXECUTION_ENABLED=false` | **STALE** vs lab `.env` |
| Dest `$0` = no dest P&L | **STALE** as proof — constructor, ledger open |
| `SAFE_BY_ABSENCE` as process-wide | **STALE** — true only for live `1369850` |

---

## 9. What this slot did **not** do

- Did not send or assemble `35=D`.
- Did not flip `REAL_COPY_EXECUTION_ENABLED`.
- Did not edit product / test / `.env`.
- Did not print secrets.
- Did not treat prior P500_VERIFY chat as evidence.

End of P500_VERIFY_46. Product source was not modified. No `35=D` was built or sent by this slot. No secrets printed.
