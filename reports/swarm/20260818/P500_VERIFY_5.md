# P500_VERIFY_5 — Adversarial verifier (slot 5)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_5.md` |
| Agent / slot | P500 verify **5** (adversarial; sibling W500/P500 reports are **not** evidence) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned files | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` |
| Confirm | (1) no 35=D builder; (2) `CanPromoteToLive` is false; (3) `RealCopyEnabled` forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| `.env` modified | **No.** Quoted **boolean / public dest identity only**: `REAL_COPY_EXECUTION_ENABLED=true`, `FEATURE_COPY_TRADING_ENABLED=true`, `CTRADER_FIX_HOST=demo-us-eqx-01.p.c-trader.com`, `CTRADER_FIX_ACCOUNT_ID=5328266`, quote/trade `SenderCompID` prefix `demo.pepperstone.5328266`. **No** passwords, tag 554, proxy auth, DB strings. |
| Secrets printed | **None.** |
| Live GET this slot | **Attempted and blocked.** `web_fetch` to `http://127.0.0.1:5000/api/{health,settings,copy/status,ingest/status,risk}` → SSRF deny on loopback. Claims that need a live process bit are **not** proved from GET. |
| Honesty rule | **FAIL any claim not proved from a file this slot or a live GET this slot.** A comment, log line, dashboard label, or W500 `SAFE_BY_ABSENCE` slogan is **not** a pin. `35=A` Logon is **not** NewOrderSingle. A demo dest fill **is** destination P&L. |

**Method:** full `read_file` of the four assigned files (`CTraderFixSession.cs` 135/135, `BaselineScorer.cs` 212/212, `RiskEngine.cs` 189/189, `LiveCopyPage.tsx` 70/70). Adjacent hop required to test claims 1/3/4/5 (not trusted as claim-proof by themselves unless cited): `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `CTraderFixLogonHostedService.cs`, `CopyTradingService.cs` (605/605), `CopyTradingHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `ShadowCopyEngine.cs`, `DealIngestionService.cs` (`RebuildTraderAsync`), `DemoCopyLedger.cs`, `apps/api/Program.cs`, `.env` flag/identity lines only, `data/demo_copy_ledger.json`, `reports/swarm/20260818/DEMO_COPY_OPEN.json`. Targeted grep: `35=D`, `Build("D")`, `CanPromoteToLive`, `RealCopyEnabled =`, `CTraderFixCopyOpen`, `ExecuteDemoCopyAsync`.

---

## 0. Verdict (binding)

**FAIL.**

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | no 35=D builder | **PASS** on assigned `CTraderFixSession.cs` only. **FAIL** as a product/copy-hop claim. | Assigned file: only outbound MsgType is `(35, "A")` L96. Product: `CTraderFixCopyOpen.Build("D")` is called from hosted `ExecuteDemoCopyAsync`. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive` is `=> false` (`BaselineScorer.cs` L211). `FromBaseline` never returns `LIVE` / `LIVE_CANDIDATE`. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** (disproved) | Only assignment is DI bind from env. Logon host **does not** write `false`. Lab `.env` L73 is `true`. Assigned session file never touches the flag. |
| 4 | sending now cannot be the profit path | **FAIL** (disproved as dest-P&L path) | Hosted 20s tick sends demo `35=D` via `CTraderFixCopyOpen`, **bypassing** `RiskEngine.AllowFixSend`. On-disk dest fill exists. Live Pepperstone `1369850` is refused; that is **not** the same as “no profit path.” |
| 5 | SHADOW on demo is not destination profit | **FAIL** (disproved) | Scorer max state is `SHADOW`. Roster **auto-admits** eligible `SHADOW`. `ExecuteDemoCopyAsync` sends for any `ADMITTED` seat — **no** `LIVE` check. Paper `ShadowCopyEngine` is a second path, not the only one. |

One FAIL is enough. Claims 3, 4, and 5 are **disproved**, not merely unproved. Claim 1 is **not** a system-wide absence.

W500-era slogans `SAFE_BY_ABSENCE` / `NewOrderSingleImplemented=false` / “copy hop has no 35=D sender” are **stale versus current `CopyTradingService.cs`**.

One-line:

```text
FAIL. CTraderFixSession is 35=A-only (PASS scoped). CanPromoteToLive=>false (PASS). RealCopyEnabled NOT forced false after logon (DI binds .env true). Demo dest 35=D is the send/P&L path (CTraderFixCopyOpen + ExecuteDemoCopyAsync; ledger DestClosed=false). SHADOW is the only scorer copyable state and is dest-sent when ADMITTED. Live GET loopback blocked. Live 1369850 refused; demo 5328266 is dest-at-risk.
```

---

## 1. Claim 1 — no 35=D builder

### 1.1 Assigned file — PASS

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines, full read).

Outbound builder is **only** `BuildLogon`:

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

| Check on this file | Measured |
|---|---|
| Tag 35 outbound | **one** field: `(35, "A")` L96 |
| `WriteAsync` | **one** (L49), of the logon bytes |
| Literal `35=D` / `(35, "D")` / `Build("D")` / `NewOrderSingle` | **0** (grep this file) |
| Inbound `35` | parsed as reply type L55; success iff `"A"` |
| Socket lifetime | `using TcpClient` + `await using SslStream`; disposed on return |

`35=A` Logon is **not** a NewOrderSingle builder. Claim 1 **as scoped to this file** is proved.

### 1.2 Product / copy hop — FAIL (cannot confirm “no 35=D builder”)

Same folder, **not** `CTraderFixSession`, **is** a 35=D builder and **is** wired:

```83:95:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
            var closing = !string.IsNullOrWhiteSpace(destPositionId);
            var cl = (closing ? "X" : "C") + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            // ...
            await Write(ssl, Build("D", sender, target, seq, extra.ToArray()), timeout.Token);
```

`Build(string type, ...)` always emits `(35, type)` (`CTraderFixCopyOpen.cs` L142–156). Callers:

| Caller | Role |
|---|---|
| `CopyTradingService.ExecuteDemoCopyAsync` L528 (close) and L566 (open) | **Hosted copy hop** |
| `CopyTradingHostedService.ExecuteAsync` L30 | every **20s** after 8s delay |
| `tools/DemoFixTestTrade/Program.cs` | CLI; also `CTraderFixDemoTestTrade` / `CTraderFixDemoMatrix` `Build("D")` |

`CTraderFixCopyOpen` gates **live** identity (`host` must start `demo-`, `sender` must start `demo.`, `account != "1369850"`). That is a **live-account refuse**, not “no builder.”

**Claim 1 product-wide is FAIL.** Slot records the assigned-file PASS so nobody pretends `CTraderFixSession` grew a D builder.

---

## 2. Claim 2 — `CanPromoteToLive` is false — PASS

`D:\Prop\src\Domain\Scoring\BaselineScorer.cs` L187–211:

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

| Check | Measured |
|---|---|
| `CanPromoteToLive` body | constant `false`; `current` unused |
| `FromBaseline` returns `LIVE` | **never** |
| `FromBaseline` returns `LIVE_CANDIDATE` | **never** |
| Best non-blocked state | `SHADOW` (quality ≥ 70 and risk < 40 and ≥ 3 completed XAU) |
| Persist path | `ReconstructionScoringService.RebuildTraderAsync` sets `CurrentState = score.SuggestedState` — no promotion step |

Unit pin (not required for this claim, consistent): `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` asserts `CanPromoteToLive(...).Should().BeFalse()`.

**PASS.** This does **not** prove dest will not send (see claim 5).

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL

None of the four assigned files assign `RealCopyEnabled`.

`CTraderFixSession.TryLogonAsync` returns `{ LoggedOn, Status, LastError, RawLogonType }`. It has **no** `LiveRuntimeStatus` parameter.

Hosted logon **after** the two `TryLogonAsync` calls:

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

It **logs** `_runtime.RealCopyEnabled`. It does **not** assign it. The log text “NewOrderSingle still unimplemented” is a **string**, not a pin (and is stale vs `CTraderFixCopyOpen`).

Only product assignment found (`grep RealCopyEnabled\s*=` on `*.cs`):

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Lab `.env` L73 (boolean only): `REAL_COPY_EXECUTION_ENABLED=true`.

API `Program.cs` L10 `EnvFile.FindAndLoad()`; L76 `/api/settings` exposes `runtime.RealCopyEnabled` (not a hardcoded false).

POCO default `CTraderFixOptions.RealCopyExecutionEnabled = false` is **unbound** to this env key (binder would need `CTrader__RealCopyExecutionEnabled`). That default is **not** the API/runtime pin.

`RiskEngine` does **not** write the runtime flag. Empty comment at L90–93 when `RealExecutionEnabled == false` does not force anything; `AllowFixSend` is computed later as `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150).

**Live GET** of `/api/settings` or `/api/health` would have shown the process bit. Loopback fetch **blocked**. File proof is still enough: the claim “forced false **after logon**” is **false** — there is no such write.

**FAIL (disproved).**

---

## 4. Claim 4 — sending now cannot be the profit path — FAIL

### 4.1 What the assigned files actually say

**RiskEngine** can approve and set `AllowFixSend=true` when all four bits are true (`RealExecutionEnabled`, `KillSwitch.None`, `Reconciled`, `VenueHealthy`). It is **not** a hard “never send.” The `RealExecutionEnabled==false` branch is an empty comment (L90–93). Rejects set `AllowFixSend=false`. That is a **gate**, not an absence of a sender.

**LiveCopyPage** is **not** a “no send” page. It renders `realCopyArmed`, `liveSends`, and:

```22:28:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      {status?.blockers?.length > 0 && (
        <div className="rounded border border-amber-900 bg-amber-950/40 p-3 text-sm text-amber-200">
          <div className="font-medium mb-1">Live send blockers (Pepperstone cannot be filled)</div>
```

Empty-state copy (L57):

> No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.

That sentence is dest-send, not paper shadow.

**CTraderFixSession** cannot be the profit path (logon only). That is **not** the only FIX writer.

### 4.2 The actual send / dest-P&L hop (file-proved)

`CopyTradingHostedService` (registered in `DependencyInjection.cs` L59):

```27:30:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
                var copy = scope.ServiceProvider.GetRequiredService<CopyTradingService>();
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

`ExecuteDemoCopyAsync` (`CopyTradingService.cs` L483–605):

- Returns 0 only if `!DemoDest` or password empty.
- `DemoDest` = host starts `demo-` **and** trade sender starts `demo.` **and** account ≠ `1369850` (L45–48).
- Lab `.env` L49/L50/L64: `demo-us-eqx-01.p.c-trader.com` / `5328266` / `demo.pepperstone.5328266` → **`DemoDest == true`** if that env is loaded.
- `NewOrderSingleImplemented => DemoDest` (L50) — **true on this lab dest**. W500 “Implemented=false” is **stale**.
- Opens: `CTraderFixCopyOpen.SendAsync(...)` **without** dest position id (L566–569) → `Build("D")` market open.
- Closes: same sender with `destPositionId` (L528–530).
- **Does not read** `RealCopyEnabled`, `AllowFixSend`, `CanPromoteToLive`, or `TraderState.LIVE`.
- Caps `maxPerTick = 5`, `MaxAutoLots = 0.05m`.

Shadow-intent path (`GenerateShadowIntentsAsync`) is **not** this hop:

- Passes `Reconciled = VenueReconciled` and `VenueReconciled` is `const false` (L20, L304).
- Persists `AllowFixSend = false` (L324) even if the engine said otherwise.
- LIVE branch only sets status `LIVE_SEND_BLOCKED_UNIMPLEMENTED` (L330–332) — **dead** because scorer never emits LIVE (claim 2).
- Then writes `SHADOW_ONLY` + `ShadowCopyEngine.SimulateEntry` (paper).

**Two hops.** Paper hop is not dest P&L. Demo hop **is**. Claiming “sending cannot be the profit path” because the paper hop hardcodes `AllowFixSend=false` is **wrong**.

### 4.3 On-disk dest fill (not a live GET; still a file)

`D:\Prop\data\demo_copy_ledger.json`: login `305750` / pos `21250421` / dest pos `237339770` / dest px `4390.2` / **`DestClosed: false`**.

`D:\Prop\reports\swarm\20260818\DEMO_COPY_OPEN.json`: `OrderSent=true`, `Filled=true`, `Host=demo-us-eqx-01.p.c-trader.com`, `Account=5328266`, exec `35=8` `150=F` `39=2`. (No tag 554 in that dump.)

That is a **destination fill** on the demo Pepperstone account. Open dest = dest P&L (demo money). `ExecuteDemoCopyAsync` L500–512 will even **re-inject** this row if missing.

`GetStatusAsync` when `DemoDest` (L76–77): “Demo dest auto-copy ON. Eligible demo/contest opens send on the 20s tick… Live 1369850 is never used.”

### 4.4 What is **not** proved

- Process is running **now** (loopback GET blocked).
- Live account `1369850` can be filled (file **refuses** it).
- Demo dest P&L is +EV (not asked; dest exposure ≠ expectancy).

**FAIL.** “Cannot be the profit path” is false for dest-send-on-demo. Narrower claim “cannot fill live 1369850” is file-true and was **not** the assigned wording.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — FAIL

### 5.1 Paper SHADOW (true, incomplete)

`ShadowCopyEngine.SimulateEntry` computes a model fill from the **quote** object. `PersistDemoShadowAsync` writes `Status = "SHADOW_ONLY"` only when `state == SHADOW`. `GenerateShadowIntentsAsync` does the same for `SHADOW` / `LIVE_CANDIDATE` / `LIVE` after roster `ADMITTED`. Those rows are **not** dest tickets.

That is **one** meaning of SHADOW. It does **not** exhaust dest behavior.

### 5.2 Dest SHADOW (file-disproved)

`XauUsdOneToOneCopyPolicy.IsTraderEligible`:

- Rejects `RISK_BLOCKED` / `DISQUALIFIED` / `PAUSED`.
- Rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` (`TRADER_NOT_SHADOW_YET`).
- Does **not** reject `SHADOW`.
- Then requires ≥ 20 completed XAU, `XauNetPnl > 0`, demo/contest group, no size-pattern flags.

`CopyRosterEngine.Decide`: if `IsTraderEligible` and not already on roster → `Admit` / `AUTO_ADMIT`. Unit test `New_eligible_trader_is_auto_admitted` uses `State = TraderState.SHADOW`.

`TickRosterAsync` writes `Status = "ADMITTED"` on that decision.

`ExecuteDemoCopyAsync` iterates **every** `ADMITTED` roster seat (L542–544) and dest-sends open XAU ≤ 0.05 lots. **No** `CurrentState == LIVE` predicate.

`FromBaseline` never emits LIVE (claim 2). `RebuildTraderAsync` copies `SuggestedState` onto `CurrentState`. Therefore the only scorer-produced copyable state is **SHADOW**. The dest auto-copy hop, if it sends, sends **SHADOW**.

`LiveCopyPage` empty copy: dest auto-sends after **ADMITTED**, not after LIVE.

### 5.3 Assigned scorer vs dest

`BaselineScorer` / `CanPromoteToLive=>false` **blocks LIVE**. It does **not** block dest. Treating “cannot promote to LIVE” as “SHADOW cannot take dest P&L” is a category error.

**FAIL (disproved).**

---

## 6. Risk to capital

| Book | File proof | Exposure |
|---|---|---|
| Live Pepperstone `1369850` | `CTraderFixCopyOpen` / `CTraderFixDemoTestTrade` / `CTraderFixDemoMatrix` refuse `account == "1369850"` and non-`demo-` host / non-`demo.` sender | **Gated.** Not proved fillable from these files. |
| Demo Pepperstone `5328266` | Hosted `ExecuteDemoCopyAsync` → `Build("D")`; `.env` identity matches `DemoDest`; ledger dest pos open (`DestClosed=false`) | **Dest P&L at risk** (demo cash, real venue tickets). |
| MT5 source books | Roster comment + flatten is dest-only; this slot did not prove a source-side send | **Not a FIX dest risk.** |
| Paper shadow book | `ShadowCopyEngine` / `SHADOW_ONLY` | **Not capital.** |

**Not `NONE`.** W500 `SAFE_BY_ABSENCE` is **false** for the current copy hop.

This slot sent **0** orders (read-only). Prior dest fill is on disk from another process.

---

## 7. Live GET

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | **Blocked** (SSRF to 127.0.0.1) |
| `http://127.0.0.1:5000/api/settings` | **Blocked** |
| `http://127.0.0.1:5000/api/copy/status` | **Blocked** |
| `http://127.0.0.1:5000/api/ingest/status` | **Blocked** |
| `http://127.0.0.1:5000/api/risk` | **Blocked** |

No process bit (`realCopyEnabled`, `quoteLoggedOn`, `tradeLoggedOn`, `liveSends`, `shadowTraders`) is claimed from GET. File + `.env` + ledger stand on their own. Claim 3 does not need GET (no post-logon write exists). Claims 4–5 do not need GET to **disprove** “cannot / is not.”

---

## 8. Residuals (do not flip the slot)

- `GetStatusAsync` reports `VenueReconciled: DemoDest` while `GenerateShadowIntentsAsync` still passes `const VenueReconciled = false`. Dashboard can say reconciled; shadow-risk hop still treats unreconciled. Demo send hop ignores both.
- `RiskEngine` L90–93 empty body is dead code.
- `CTraderFixLogonHostedService` log still says “NewOrderSingle still unimplemented.”
- `LiveRuntimeStatus.Snapshot` copy still says “NewOrderSingle still unimplemented / disabled.”
- `README.md` / `docs/*` still say `REAL_COPY_EXECUTION_ENABLED=false`. Committed docs ≠ process.
- `CTraderFixDemoTestTrade` / `DemoMatrix` remain additional demo `35=D` builders (CLI).
- Loopback API not observed this slot.

---

## 9. Files read (absolute)

- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs`
- `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs`
- `D:\Prop\src\Domain\Copy\CopyLifecycle.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Domain\Enums\TraderState.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` (`RebuildTraderAsync`)
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (`PersistDemoShadowAsync`)
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\data\demo_copy_ledger.json`
- `D:\Prop\reports\swarm\20260818\DEMO_COPY_OPEN.json`
- `.env` L49/L50/L56/L64/L73/L106 only (identity + booleans)

**Slot verdict: FAIL.**
