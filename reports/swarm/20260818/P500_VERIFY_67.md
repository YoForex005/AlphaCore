# P500_VERIFY_67 — Adversarial verifier (slot 67)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_67.md` |
| Slot | **67** |
| Agent | P500_VERIFY_67 (adversarial; independent HEAD re-read) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned SUT (read in full) | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135), `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212), `D:\Prop\src\Domain\Risk\RiskEngine.cs` (189/189), `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (70/70) |
| Adjacent (needed to prove/disprove 1, 3–5) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `ShadowCopyEngine.cs`, `CopyRosterEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyGroupFilter.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `apps/api/Program.cs`, `data/demo_copy_ledger.json`, `.env` **booleans / public dest ids only** |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No.** `REAL_COPY` not flipped. |
| Live `35=D` sent this slot | **No.** Not constructed. Not written. |
| Secrets printed | **None.** Quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73), public dest `5328266`, live refuse id `1369850`, public sender prefix `demo.pepperstone.5328266`. No password, token, DB DSN secret, or tag `554`. |
| Live GET this pass | **Attempted, blocked.** `GET http://127.0.0.1:5000/api/health` → worker HTTP `SSRF blocked: 127.0.0.1`. `/api/settings`, `/api/copy/status` not live-proven. Runtime `realCopyEnabled` **not** process-proven. File proof is enough to **disprove** claim 3. |
| Binding rule | **FAIL any assigned claim that cannot be proven from a live file or a live GET.** Sibling swarm books are not evidence. Prior “product `35=D=0`”, “`NOS=const false`”, “logon re-pins `RealCopyEnabled=false`” are **STALE vs this HEAD re-read**. |

**Honesty:** Wanting dest profit is not an edge. `SAFE_BY_ABSENCE` on live `1369850` is **not** “`RealCopyEnabled` stays false.” An armed env bit with no live sender is still an armed bit. Demo dest send is dest **exposure**, not booked dest profit. Dashboard `DestinationRealPnl=0` is a constructor constant, not a mark-to-market of dest cash.

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

**FAIL.** Claim 3 is **disproven** on disk. Claims 1 and 4 cannot be confirmed as written (unscoped / contradicted by hosted demo hop). Claims 2 and 5 are file-proven.

| # | Assigned claim | Measured | Class |
|---|---|---|---|
| 1 | no `35=D` builder | Assigned session file: **no** builder. Product: **yes** (`CTraderFixCopyOpen.Build("D")` hosted). | **FAIL** unscoped / **PASS_SCOPED** on `CTraderFixSession` |
| 2 | `CanPromoteToLive` is false | Literal `=> false`. `FromBaseline` never returns `LIVE`. | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | Sole C# write is DI bind of `.env=true`. Logon host logs the bit; never assigns. | **FAIL (disproven)** |
| 4 | sending now cannot be the profit path | Hosted 20s `ExecuteDemoCopyAsync` → dest `35=D` on demo `5328266`. On-disk dest fill open. Dest DTO `0` is not dest cash. Live GET of dest P&L missing. | **FAIL (contradicted / unproven as written)** |
| 5 | SHADOW on demo is not destination profit | `SHADOW` is source state + paper `SimulateEntry`. `ShadowPnl` is slippage sum. Dest DTO is a separate literal `0`. | **PASS** (residual: SHADOW is dest ADMIT floor) |

One-line:

```text
FAIL slot 67: CTraderFixSession 35=A only (no D builder in that file); CanPromoteToLive=>false; RealCopyEnabled NOT forced false after logon (.env true + DI bind + logon logs-only); send-now CAN be demo dest exposure (ExecuteDemoCopyAsync → Build("D"); ledger 305750/237339770 open); SHADOW-on-demo is source/paper not dest PnL. Risk NONE on live 1369850. Demo dest hop WIRED. Live GET blocked.
```

Do **not** treat this FAIL as a license to send. Do **not** flip leftover `.env=true` into go-live. Operator should restore `REAL_COPY_EXECUTION_ENABLED=false` (this slot did **not** edit it).

---

## 1. Claim 1 — no `35=D` builder — FAIL unscoped / PASS_SCOPED (`CTraderFixSession`)

Assigned file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` is **135** lines. Read in full this slot.

The only outbound assembler is `BuildLogon`. Tag 35 is hardcoded `"A"`:

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

Single `WriteAsync` (L49) of that logon. Then one `ReadAsync`. Socket disposed. Inbound `Extract(reply, "35")` (L55) is reply parse, not a builder. Tokens in this file: `Build("D")` = 0, `(35, "D")` = 0, `NewOrderSingle` = 0.

Hosted caller of **this** type is logon-only:

```48:58:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            ...);
        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            ...);
```

**Unscoped product claim fails.** Same `Sessions/` folder has a real NewOrderSingle builder, and the copy host **calls it**:

```95:95:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
            await Write(ssl, Build("D", sender, target, seq, extra.ToArray()), timeout.Token);
```

```142:149:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
    private static string Build(string type, string sender, string target, int seq, params (int, string)[] extra)
    {
        var fields = new List<(int, string)>
        {
            (35, type), (34, seq.ToString(CultureInfo.InvariantCulture)),
            ...
        };
```

Additional off-hop `Build("D")` in `CTraderFixDemoTestTrade.cs` (3) and `CTraderFixDemoMatrix.cs` (1). Product total **5**. CopyOpen refuses live identity (`account == "1369850"` or host/sender not `demo-` / `demo.`) at L37–41.

Assigned `RiskEngine.cs` / `BaselineScorer.cs` / `LiveCopyPage.tsx` contain **0** FIX builders. That does not license “no `35=D` builder” as a product fact.

**Claim 1 as written: FAIL.** Scoped to the assigned session file: **PASS_SCOPED**.

---

## 2. Claim 2 — `CanPromoteToLive` is false — PASS

Assigned `BaselineScorer.cs` ends with a hard-false promotion gate. `FromBaseline` never emits `LIVE` or `LIVE_CANDIDATE`:

```189:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
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
```

Unit lock (`tests/Unit/BaselineScorerTests.cs` L25–26): three disciplined winners → `SHADOW`; `CanPromoteToLive(...)` must be false.

Quality formula (L152–160) can be high while `NetPnl` is not the driver of dest cash. That is a scoring fact, not a live promotion.

Assigned `RiskEngine.cs` and `LiveCopyPage.tsx` do not promote. `LiveCopyPage` only **displays** `liveTraders`.

**Claim 2: PASS.**

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL (disproven)

Assigned four files **never assign** `RealCopyEnabled`.

Product-wide grep for `RealCopyEnabled =` in `*.cs` returns **one** write:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Lab `.env` L73 (boolean only):

```
REAL_COPY_EXECUTION_ENABLED=true
```

API host loads that file (`apps/api/Program.cs` L10 `EnvFile.FindAndLoad()` + L13 `AddEnvironmentVariables()`). Settings GET exposes the **runtime** bit (`Program.cs` L76), not a hard-false.

Logon host **after** both `TryLogonAsync` calls:

```60:70:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.Quote.LoggedOn = quote.LoggedOn;
        ...
        _runtime.Trade.LoggedOn = trade.LoggedOn;
        ...
        _log.LogInformation(
            "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
            quote.LoggedOn, trade.LoggedOn, _runtime.RealCopyEnabled, account);
```

It **reads** `_runtime.RealCopyEnabled`. It does **not** assign `false`. Persist (`PersistAsync` L91–111) writes FIX session rows only.

`LiveRuntimeStatus` (`src/Application/Runtime/LiveRuntimeStatus.cs` L32) is a mutable auto-property. Snapshot copy-note when true still claims “NewOrderSingle still unimplemented” (L43). That comment is **STALE** vs `CopyTradingService.NewOrderSingleImplemented => DemoDest` (L50) + hosted `ExecuteDemoCopyAsync`.

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (`CTraderFixOptions.cs` L35). That POCO is **not** what DI binds into `LiveRuntimeStatus`. Design default ≠ runtime.

Live GET `/api/health` / `/api/settings` would have re-measured the process bit. This slot’s loopback GET was **SSRF-blocked**. File proof already **disproves** “forced false after logon”: there is no after-logon assignment at all.

**Claim 3: FAIL (disproven).** Armed bit ≠ live send license. Do not treat FAIL as go-live.

---

## 4. Claim 4 — sending now cannot be the profit path — FAIL as written

What the assigned files prove:

- `CTraderFixSession` cannot send a dest ticket (claim 1 scoped).
- `RiskEngine` can theoretically set `AllowFixSend=true` when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Rejects hard-set `AllowFixSend=false` (L180–188). The `RealExecutionEnabled==false` branch (L90–93) is a **no-op comment**, not a reject.
- Persist in `GenerateShadowIntentsAsync` **overwrites** `AllowFixSend = false` (`CopyTradingService.cs` L324) regardless of Evaluate.
- Live-send branch (L330) also requires `score.CurrentState == LIVE && NewOrderSingleImplemented && VenueReconciled`. `VenueReconciled` **const is false** (L20). That branch cannot fire.
- `LiveCopyPage.tsx` has **no** send button. It is a GET display of `/api/copy/status` + `/api/copy/intents`. Empty-state text (L57) **admits** dest auto-send:

```56:58:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      {intents.length === 0 && (
        <p className="text-gray-500 text-sm">No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.</p>
      )}
```

What **disproves** the claim as written (“sending now cannot be the profit path”):

Hosted 8s + 20s tick **does send** on demo dest, **bypassing** `RiskEngine.Evaluate`:

```21:34:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
        await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            ...
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

```45:50:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public bool DemoDest =>
        (_config["CTRADER_FIX_HOST"] ?? "").StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
        && (_config["CTRADER_FIX_TRADE_SENDER_COMP_ID"] ?? "").StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
        && _config["CTRADER_FIX_ACCOUNT_ID"] != "1369850";

    public bool NewOrderSingleImplemented => DemoDest;
```

Lab `.env` (public ids only): host `demo-us-eqx-01.p.c-trader.com`, account `5328266`, trade sender `demo.pepperstone.5328266`. That **is** `DemoDest`. `ExecuteDemoCopyAsync` L485–489 only skips when **not** DemoDest. Then L566–569 / L528–530 call `CTraderFixCopyOpen.SendAsync` → `Build("D")`.

On-disk dest fill (`D:\Prop\data\demo_copy_ledger.json`; also hard-injected at L500–512 if missing):

| Field | Value |
|---|---|
| SourceLogin | `305750` |
| SourcePositionId | `21250421` |
| DestPositionId | `237339770` |
| DestClOrdId | `C20260818093047317` |
| DestFillPrice | `4390.2` |
| Lots | `0.01` |
| DestClosed | **false** |

Dashboard dest cash is **not** measured from that fill. `OverviewDto.DestinationRealPnl` is the 11th ctor arg; `GetOverviewAsync` passes literal `0`:

```33:45:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        return new OverviewDto(
            accounts,
            brokers,
            xauTraders,
            three,
            ...
            shadowPnl,
            0,
            0,
            0,
```

A constructor `0` is **not** proof dest has no P&L. This slot could not live-GET dest cash. Therefore “sending now cannot be the profit path” is **not proven**. Files show sending now **can** be dest exposure on demo `5328266`. Live `1369850` refuse is **SAFE_BY_ABSENCE** on the **live** book only.

**Claim 4: FAIL.** Residual if someone re-scopes the claim to “not booked dest profit / not live 1369850”: dest DTO 0 + live refuse hold; hosted demo hop still exists.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — PASS

Assigned `BaselineScorer` / `TraderStateMachine`: ceiling after a good early book is `SHADOW` (L200–201, L209). That is a **source** `TraderState`, not dest cash.

Assigned `LiveCopyPage` “SHADOW traders” is `status?.shadowTraders` — a **count** of source scores (`CopyTradingService.GetStatusAsync` L59).

Paper hop (`GenerateShadowIntentsAsync` L336–359): intents become `SHADOW_ONLY`; fills are `_shadow.SimulateEntry(...)` written to `ShadowOrders`. `ShadowCopyEngine.SimulateEntry` (`ShadowCopyEngine.cs` L35–61) computes a modeled ask/bid + 0.05-point latency; it does not call FIX.

Overview `ShadowPnl` is `Sum(SourceVsShadowSlippage)` (`EfDashboardQueries.cs` L29), **not** dest realized.

`CopyGroupFilter.IsDemoOrContest` (`CopyGroupFilter.cs` L9–23) is a **source-group** admit filter (path segment `demo` / `contest`). It does not book dest profit.

**Residual (does not flip claim 5):** `XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET` (L81–85). `CopyRosterEngine` AUTO_ADMITs eligible seats (`CopyRosterEngine.cs` L72–80). `ExecuteDemoCopyAsync` then dest-sends for `Status=="ADMITTED"` roster keys (L542–569). So **SHADOW is the dest ADMIT floor**. That dest fill is still **not** the SHADOW number. Claim as written (“SHADOW on demo is not destination profit”) holds.

**Claim 5: PASS.**

---

## 6. Risk to capital (this slot)

| Book | Measured |
|---|---|
| Live Pepperstone `1369850` | **NONE.** `CTraderFixSession` cannot `35=D`. `CTraderFixCopyOpen` refuses `1369850`. Persist `AllowFixSend=false`. LIVE promote `=> false`. This slot sent **0**. |
| Demo dest `5328266` | **WIRED, not absent.** Hosted 20s hop can `Build("D")` when `DemoDest` (lab `.env` matches). Ledger dest `237339770` **open**. Dest DTO `$0` is a constructor, not a mark. This slot did **not** send. |
| Process `REAL_COPY` | **Armed in config** (`.env` L73 `true` + DI bind). **Not** re-pinned false after logon. Live GET of the process bit **blocked**. Armed ≠ live license. |

`SAFE_BY_ABSENCE` applies to **live** `1369850` only. Do not extend it to demo dest.

---

## 7. Live GET

```text
GET http://127.0.0.1:5000/api/health
→ SSRF blocked: 127.0.0.1 resolves to private/internal IP 127.0.0.1
```

No `/api/settings`, `/api/copy/status`, `/api/overview` body this slot. File proof is sufficient to score all five claims. Claim 3 does not need a GET to fail (there is no after-logon write). Claim 4 dest cash remains **unmeasured** (constructor 0 + open ledger fill).

---

## 8. What this slot did **not** do

- Did not edit product, tests, or `.env`.
- Did not send `35=D` / NewOrderSingle.
- Did not print secrets.
- Did not claim dest EV, copy-all 8463 EV, or “fully decompiled.” Those are out of assigned scope.
- Did not treat prior swarm books as evidence.

---

## 9. Operator note (not a change)

Restore `REAL_COPY_EXECUTION_ENABLED=false` unless a measured send license exists. This slot did **not** flip it.
