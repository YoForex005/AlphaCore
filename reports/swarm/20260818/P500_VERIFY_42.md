# P500_VERIFY_42 — Adversarial verifier, slot 42

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_42.md` |
| Slot | **42** |
| Agent | P500_VERIFY_42 (adversarial verifier; independent HEAD re-read) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Topic | Confirm five claims from assigned files. **FAIL any claim not proven from a file or live GET.** |
| Assigned SUT | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` |
| Adjacent (needed to prove/disprove 1, 3–5) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `ShadowCopyEngine.cs`, `CopyRosterEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyLifecycle.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `apps/api/Program.cs`, `CTraderFixOptions.cs`, `data/demo_copy_ledger.json`, `DEMO_COPY_OPEN.json`, `.env` **booleans / public dest ids only** |
| Product source modified | **No.** Report + INDEX / SWARM_LOG / `P500_MANIFEST.tsv` pins only. |
| Test source modified | **No.** |
| Config / `.env` edited | **No.** `REAL_COPY` not flipped. |
| Live `35=D` / NewOrderSingle this slot | **Not sent. Not constructed.** |
| Secrets printed | **None.** Quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73), `FEATURE_COPY_TRADING_ENABLED=true` (L106), public dest id `5328266`, live refuse id `1369850`, public host prefix `demo-us-eqx-01…`, public sender prefix `demo.pepperstone.5328266`. |
| Localhost API this slot | **Attempted, blocked.** `GET http://127.0.0.1:5000/api/health` → worker HTTP `SSRF blocked: 127.0.0.1`. Runtime `realCopyEnabled` **not** live-proven. File-only for claim 3. |

**Honesty rule:** Chat, sibling VERIFY slots, and W500 “product `35=D=0` / `NOS=const false` / logon re-pins false” are **not evidence**. This slot re-read the files. A015 “forces `RealCopyEnabled=false` after logon” is **STALE vs HEAD**.

```text
CTraderFixSession outbound is 35=A only (135/135).
Product 35=D builders exist (CopyOpen / DemoTestTrade / DemoMatrix).
CanPromoteToLive => false.
RealCopyEnabled is NOT forced false after logon (DI binds env; .env L73 true).
Hosted 20s tick can 35=D on demo dest; live 1369850 refused.
SHADOW ledger is SimulateEntry / slippage sum, not dest P&L.
SHADOW is also the dest AUTO_ADMIT floor.
Live GET this slot = SSRF blocked.
```

---

## 0. Verdict (binding)

**FAIL.** The five-claim bundle is not proven. Claim 3 is **disproven**. Claims 1 and 4 fail as stated. Claim 5 is only proven as paper-ledger accounting.

| # | Claim | Verdict | Why |
|---|---|---|---|
| 1 | no `35=D` builder | **FAIL** (product). **PASS_SCOPED** on `CTraderFixSession.cs` only | Assigned session file is Logon `35=A` only. Same folder + hosted hop assemble and send `35=D`. Unqualified “no builder” is false. |
| 2 | `CanPromoteToLive` is false | **PASS** | Literal `=> false`. `FromBaseline` never returns `LIVE`. Unit lock exists. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL (disproven)** | Logon host **reads** the flag; **never assigns** it. Sole write is DI bind from `REAL_COPY_EXECUTION_ENABLED`. `.env` L73 is `true`. Live GET of `/api/settings` not available this slot. |
| 4 | sending now cannot be the profit path | **FAIL (unproven / contradicted)** | Demo dest auto-copy **is** a send path (`ExecuteDemoCopyAsync` → `Build("D")`). On-disk dest fill exists. Dashboard dest PnL `0` is a constructor lie, not proof dest has no P&L. Live `1369850` send is refused — that is **not** the whole claim. |
| 5 | SHADOW on demo is not destination profit | **PASS_PAPER.** **FAIL** if read as “SHADOW cannot produce dest P&L” | Paper `ShadowOrder` / `ShadowPnl` = `SimulateEntry` + slippage sum. `DestinationRealPnl` ctor is literal `0`. Residual: `SHADOW` is the dest `AUTO_ADMIT` class; hosted hop can `35=D` those names. That dest fill is dest execution, not the SHADOW number. Unqualified claim not fully proven. |

```text
OVERALL = FAIL
  because claim 3 is disproven from HEAD files
  and claims 1/4 cannot be confirmed as stated
  and claim 5 is only paper-proven.

PASS only: (2) CanPromoteToLive.
PASS_SCOPED: (1) CTraderFixSession; (5) paper SHADOW ledger.
```

**Risk to capital:** **NONE on live `1369850`** (`CTraderFixCopyOpen` refuse + session hop `35=A` only + persist `AllowFixSend=false` + `VenueReconciled=const false` + `CanPromoteToLive=>false`). **Not absent on demo dest `5328266`** (hosted 20 s hop; ledger 305750 / dest 237339770 / 0.01 / 4390.2 / `DestClosed=false`). This slot sent **0**.

---

## 1. What was read (HEAD, this slot)

| File | Lines (this read) | Used for |
|---|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 135 | Claim 1 |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | 212 | Claim 2 |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | 189 | Claims 3–4 (`AllowFixSend`) |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | 70 | Claims 4–5 (UI honesty) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | 112 | Claim 3 |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 61 | Claim 3 |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | 66 | Claim 3 |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | 625 | Claims 1, 4, 5 |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 44 | Claim 4 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | 223 | Claims 1, 4 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | 391 | Claim 1 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | `SendD` / `Build("D")` | Claim 1 |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | 91 | Claim 5 |
| `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs` | 136 | Claims 4–5 (`AUTO_ADMIT`) |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | 188 | Claims 4–5 |
| `D:\Prop\src\Domain\Copy\CopyLifecycle.cs` | 10 | Claim 4 |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | dest ctor L33–52 | Claims 4–5 |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | L5–22 | Claims 4–5 |
| `D:\Prop\apps\api\Program.cs` | L33–84, L102–103 | Claim 3 (exposes runtime flag) |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | 79 | Claim 3 (POCO default unused by DI) |
| `D:\Prop\data\demo_copy_ledger.json` | 11 | Claim 4 (on-disk dest fill) |
| `D:\Prop\reports\swarm\20260818\DEMO_COPY_OPEN.json` | 19 | Claim 4 (prior dest ER 35=8 / 150=F) |
| `D:\Prop\.env` L49/L50/L64/L73/L106 | booleans + public ids | Claims 3–4 (`DemoDest` + REAL_COPY) |
| `tests/Unit/BaselineScorerTests.cs` | L20–27 | Claim 2 lock |

No password, token, connection string, or FIX `554=` value is quoted.

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

Grep of this file: three `35` hits — inbound extract L55, reject text L73, outbound `(35, "A")` L96. **Zero** `"D"`. **Zero** `NewOrderSingle`. Sockets disposed via `using TcpClient` / `await using SslStream`. **PASS for this type.**

### 2.2 Product-wide “no builder” is false

Same namespace `TraderIntelligence.Fix.CTrader.Sessions` contains three `Build(type, …)` helpers that accept `"D"` and write it:

| Type | Outbound `Build("D"` / `SendD` | Role |
|---|---|---|
| `CTraderFixCopyOpen` | L95 | **Hosted** dest open/close |
| `CTraderFixDemoTestTrade` | L139, L163, L197 | Demo test flatten / open / close |
| `CTraderFixDemoMatrix` | L93 (`SendD`) | Demo scenario matrix |

`CopyTradingHostedService` (20 s) calls `ExecuteDemoCopyAsync`, which calls `CTraderFixCopyOpen.SendAsync` when `DemoDest` is true. `.env` host starts with `demo-`, trade sender starts with `demo.`, account is `5328266` (≠ `1369850`) → **`DemoDest` is true** from files.

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

Live refuse (not a missing builder):

```37:42:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
        if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
            || !sender.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
            || account == "1369850")
        {
            return Fail("Refused: dest must be demo FIX, not live 1369850.", host, account, sourceLogin);
        }
```

`NewOrderSingleImplemented => DemoDest` (`CopyTradingService` L50). Reports that still say `NOS=const false` or “product `35=D=0`” are **STALE**.

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

- `CanPromoteToLive` ignores `current` and returns **false**.
- Ceiling of `FromBaseline` is `SHADOW` (or `RISK_BLOCKED` / `WATCH` / `EARLY_SCORE` / `INSUFFICIENT_DATA`). **Never `LIVE` / `LIVE_CANDIDATE`.**
- `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` asserts `CanPromoteToLive(...).Should().BeFalse()`.

`LiveCopyPage` does not call promote. It only displays `status?.liveTraders`. That cannot flip the scorer.

**PASS.** File-proven.

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

Workspace grep of `RealCopyEnabled =` under product `*.cs`: **one hit**.

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`.env` L73 (boolean only): `REAL_COPY_EXECUTION_ENABLED=true`.

API boot (`apps/api/Program.cs` L10) calls `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()`. `/api/health` L55 and `/api/settings` L76 **expose** `runtime.RealCopyEnabled` — they do not pin it false.

`CTraderFixOptions.RealCopyExecutionEnabled` **defaults** false (POCO L35). That type is **not** what DI writes onto `LiveRuntimeStatus`. Irrelevant to “forced false after logon.”

`RiskEngine` does not touch `LiveRuntimeStatus.RealCopyEnabled`. `LiveCopyPage` only renders `status?.realCopyArmed`.

### 4.2 Live GET

`GET http://127.0.0.1:5000/api/health` was **SSRF-blocked** this slot. Cannot prove the running process’s boolean. File bind + `.env` `true` + no post-logon write is enough to **disprove** “forced false after logon.”

**FAIL.**

---

## 5. Claim 4 — sending now cannot be the profit path — **FAIL**

Two hops exist. Collapsing them is how stale reports greenwashed this claim.

### 5.1 Paper hop cannot send

`CopyTradingService.GenerateShadowIntentsAsync`:

- `VenueReconciled = const false` (L20) → `RiskEngine` `allowSend` is false (`Reconciled` required at `RiskEngine` L147–150).
- Persist **hardcodes** `AllowFixSend = false` (L324) even if Evaluate returned true.
- Live-send branch (L330) requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` → **dead**. Status becomes `SHADOW_ONLY`.

`RiskEngine` L90–93 when `RealExecutionEnabled == false` is an **empty** comment. It does **not** reject. It also does not matter: the persist overwrite and `VenueReconciled=false` already kill this hop.

`LiveCopyPage` “Live sends” is `ExecutionIntents.Count(SentAt != null)` (`GetStatusAsync` L57). That counter is not the dest path.

### 5.2 Demo dest hop **can** send — and has

```21:34:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
            try
            {
                using var scope = _scopes.CreateScope();
                var copy = scope.ServiceProvider.GetRequiredService<CopyTradingService>();
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

`ExecuteDemoCopyAsync` **bypasses** `RiskEngine.Evaluate`. If `DemoDest`, it calls `CTraderFixCopyOpen.SendAsync` for `ADMITTED` roster seats with open XAU ≤ `MaxAutoLots` (0.05) and for ledger closes when source `Completed`.

`.env` makes `DemoDest` true (host `demo-…`, sender `demo.…`, account `5328266` ≠ `1369850`).

On-disk dest fill (`D:\Prop\data\demo_copy_ledger.json`):

- source login `305750`, source pos `21250421`
- dest pos `237339770`, dest px `4390.2`, lots `0.01`, **`DestClosed=false`**

`DEMO_COPY_OPEN.json` records the same fill as `OrderSent=true`, `Filled=true`, account `5328266`, inbound `35=8` / `150=F` / `39=2`. That is dest execution, not paper.

`LiveCopyPage` empty-state (L57) and `GetStatusAsync` summary when `DemoDest` (L76–77) **tell the operator** dest auto-sends on the 20 s tick. The page heading is “Live copy portfolio.”

```76:78:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
            Summary: DemoDest
                ? "Demo dest auto-copy ON. Eligible demo/contest opens send on the 20s tick; dest closes when the MT5 source closes. Live 1369850 is never used."
                : "Copy pipeline ON. Shadow intents only. Live Pepperstone will not receive NewOrderSingle.");
```

```56:58:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      {intents.length === 0 && (
        <p className="text-gray-500 text-sm">No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.</p>
      )}
```

### 5.3 Why “cannot be the profit path” is unproven

| Interpretation | File result |
|---|---|
| Live Pepperstone `1369850` cannot be the send/profit path | **True** — CopyOpen refuse + session `35=A` only |
| Dashboard dest P&L is not computed from sends | **True** — `EfDashboardQueries` passes literal `0` into `DestinationRealPnl` (L44) |
| Therefore sending cannot produce dest P&L | **False / unproven** — dest fill + price exist on the demo ledger; DTO `0` is a lie, not a venue statement |
| Sending is off so it cannot be how we make money | **False** — hosted hop is on |

`BuildBlockers` adds “No NewOrderSingle sender — SAFE_BY_ABSENCE” only when `!DemoDest` (L610–616). On the lab `.env`, those live-Pepperstone blockers are **omitted**. UI does not hide the demo send path.

Cannot prove the unqualified claim from a file or live GET. **FAIL.**

---

## 6. Claim 5 — SHADOW on demo is not destination profit — **PASS_PAPER** / **FAIL_AS_DEST_CLASS**

### 6.1 SHADOW is a source-shape label

`FromBaseline` emits `SHADOW` when `quality >= 70 && risk < 40` after ≥3 completed XAU (`BaselineScorer` L200–201). Inputs are reconstructed **source** trades (`NetRealizedPnl`, martingale, SL use). No dest fill, no dest commission, no dest spread.

`CanPromoteToLive => false` keeps that label off `LIVE`.

### 6.2 Shadow ledger is simulated

`ShadowCopyEngine.SimulateEntry` marks a modeled bid/ask + 0.05 pt latency slip. `GenerateShadowIntentsAsync` writes `ShadowOrder` rows from that fill when a dest **quote row** exists and Evaluate `Approve`s — still **no venue send** on that hop.

Overview “shadow P&L” is `Sum(SourceVsShadowSlippage)` (`EfDashboardQueries` L29), **not** dest realized. `DestinationRealPnl` is the next constructor argument and is **literal `0`** (L44).

`ShadowOrder` has no dest ticket / dest PnL field.

**Paper reading: PASS.** SHADOW numbers are not dest cash.

### 6.3 Dest-class residual (flips the unqualified claim)

`XauUsdOneToOneCopyPolicy.IsTraderEligible` **allows** `SHADOW` (rejects only `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` / blocked states). `CopyRosterEngine` can `AUTO_ADMIT` those names on demo/contest groups (`Reason = "AUTO_ADMIT"`, L77). `ExecuteDemoCopyAsync` then sends dest `35=D` for `ADMITTED` seats **without** requiring `TraderState.LIVE`.

So: **SHADOW traders on demo can cause dest fills.** Those fills are dest execution (ledger 305750 → dest 237339770). They are **still not** the SHADOW score or the shadow slippage sum.

Adversarial rule: cannot confirm the **unqualified** sentence from a file or live GET, because dest P&L from a SHADOW+`ADMITTED` name is possible and already recorded. **FAIL as dest-class.** Paper-only reading remains true.

`LiveCopyPage` shows `SHADOW traders` and `Shadow fills` as separate stats from `Live sends`. It does not add them into dest P&L. That supports PASS_PAPER, not dest-class safety.

---

## 7. RiskEngine notes (assigned file, not a sixth claim)

- Increasing-exposure rejects force `AllowFixSend=false`.
- Approvals set `AllowFixSend = RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150).
- Hosted paper hop passes `Reconciled=false` and then overwrites persist to `false`.
- Hosted **demo send hop does not call** `Evaluate`. RiskEngine is not a capital gate on dest `35=D`.

This supports claim 4 FAIL (send bypass) and does not change claim 2.

---

## 8. Live GET matrix (this slot)

| URL | Result | Usable as proof? |
|---|---|---|
| `GET http://127.0.0.1:5000/api/health` | Worker HTTP **SSRF blocked: 127.0.0.1** | **No** |
| `GET /api/settings` | Not fetched (same host) | **No** |
| `GET /api/copy/status` | Not fetched (same host) | **No** |

Runtime `realCopyEnabled` is therefore **file-inferred** (`.env` true + DI bind), not process-measured.

---

## 9. What this slot did **not** do

- Did not edit product, tests, or `.env`.
- Did not send or assemble `35=D`.
- Did not print secrets.
- Did not treat dest DTO `0` as dest cash.
- Did not treat wanting profit as an edge.
- Did not copy-all 8463 (that book is outside this five-claim SUT; not re-measured here).

---

## 10. Stale pins this HEAD contradicts

| Pin | HEAD |
|---|---|
| A015 / older slots: logon forces `RealCopyEnabled=false` | **Removed.** Logon logs only. |
| W500 “product `35=D=0`” / `NewOrderSingleImplemented=const false` | **False.** `NOS => DemoDest`. Three sibling `Build("D")` helpers. Hosted hop wired. |
| “SAFE_BY_ABSENCE on the process” | **True only for live `1369850`.** False for demo dest `5328266`. |
| Dashboard dest `$0` means dest has no P&L | **Lie.** Constructor zero. Ledger has a fill. |

---

End of P500_VERIFY_42. Product source was not modified. No secrets printed. This slot did not send `35=D`.
