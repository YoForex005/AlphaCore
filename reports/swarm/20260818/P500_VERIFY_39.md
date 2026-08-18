# P500_VERIFY_39 — Adversarial four-file verify (slot 39)

| Field | Value |
|---|---|
| Slot | **39** |
| Date | 2026-08-18 |
| Agent | P500_VERIFY_39 (adversarial verifier; did **not** trust sibling `P500_*` / `W500_*` numbers) |
| Assigned | Read `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`. Confirm: (1) no `35=D` builder; (2) `CanPromoteToLive` is false; (3) `RealCopyEnabled` forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. **FAIL any claim that cannot be proved from a file or live GET.** |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **None.** Quoted only the already-on-disk boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`. Password / proxy / FIX password values were not quoted. |
| Live GET this slot | **Attempted and blocked.** `web_fetch` `http://127.0.0.1:5000/api/health` → SSRF blocked (loopback). `open_page` `http://localhost:5000/api/health` → retrieve failed. **No live JSON.** File proof only. |
| Live `35=D` sent this slot | **No.** |
| `REAL_COPY` flipped this slot | **No.** |
| Method | Full `read_file` of the four assigned files (135 / 212 / 190 / 70 lines). Then the hop they actually call: logon host, DI bind, copy service, demo sender, roster policy, shadow engine, overview DTO. Targeted `grep` for `35=D` / `Build("D")` / `CanPromoteToLive` / `RealCopyEnabled =` / `DestinationRealPnl`. `.env` flag/host/account keys only. |
| Honesty rule | Sibling swarm PnL / census integers are **not** evidence. A comment is **not** a runtime pin. A dashboard label is **not** dest cash. A live GET that did not return is **not** a value. |

---

## 0. Verdict (binding)

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** (scoped to `CTraderFixSession.cs`) | File **135/135**. Only outbound MsgType is `(35, "A")` at L96. Grep of this file for `35=D` / `(35, "D")` / `Build("D")` / `NewOrderSingle` = **0**. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(TraderState current) => false` at `BaselineScorer.cs` L211. Argument unused. `FromBaseline` ceiling is `SHADOW`. Unit test asserts SHADOW ≠ LIVE. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | **Disproved.** Logon host **reads** the flag and **never assigns** `false`. Sole `RealCopyEnabled =` write is DI bind of env. Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true`. |
| 4 | sending now cannot be the profit path | **PASS_SCOPED_NOT_LIVE_OR_BOOKED** | Assigned session cannot send. Persist `AllowFixSend=false`. `CanPromoteToLive` is hard-false. `VenueReconciled` is const `false`. `DestinationRealPnl` is a positional literal `0`. Residual: sibling `CTraderFixCopyOpen.Build("D")` **is** a demo dest sender on the 20s tick — dest **activity**, not a booked dest-profit constructor, not live `1369850`. Unqualified tree-wide “no sender / SAFE_BY_ABSENCE” is **stale**. |
| 5 | SHADOW on demo is not destination profit | **PASS** | `SHADOW` is a **source-trader state**. Dashboard `ShadowPnl` is `Sum(SourceVsShadowSlippage)`. `DestinationRealPnl` is constructor `0`. `LiveCopyPage` shows SHADOW **counts**, no dest cash. Paper `ShadowCopyEngine.SimulateEntry` is not a venue fill. |

**Slot verdict: `FAIL`.**

Claim 3 is **disproved**, not merely unproved. Four of five assigned claims hold from files when claim 1 is scoped to the assigned session file and claim 4 is scoped to live/booked dest profit. One FAIL is enough.

**Risk to capital:** **NONE on live `1369850`** (`CTraderFixSession` has no `35=D`; `CTraderFixCopyOpen` refuses that account). **Not absent on demo dest `5328266`** if `DemoDest` is true: hosted `ExecuteDemoCopyAsync` can emit `35=D` **without** `RiskEngine.Evaluate` and **without** reading `RealCopyEnabled`. This slot sent **0**.

---

## 1. Claim 1 — no `35=D` builder — **PASS** (`CTraderFixSession.cs` only)

Assigned file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135** lines, full read).

The only outbound builder is `BuildLogon`:

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

| Check | Measured |
|---|---|
| Outbound MsgType in this file | **only** `(35, "A")` L96 |
| `ssl.WriteAsync` count | **1** (L49, the logon bytes) |
| `35=D` / `(35, "D")` / `Build("D")` / `NewOrderSingle` in this file | **0** |
| Socket lifetime | `using TcpClient` + `await using SslStream`; disposed after one read |
| Inbound `35=A` treated as | `LoggedOn = true` (L56–64) — **logon ack, not a fill** |
| Public surface | `TryLogonAsync` only. No order/qty/side parameters. |

Adversarial residual (**does not fail the assigned-file claim**): sibling product files **do** have a `35=D` builder. Tree-wide “product `35=D=0`” is **false** on HEAD.

| File | What this slot measured |
|---|---|
| `src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra)` after TRADE logon + SecurityList. **Refuses** non-`demo-` host, non-`demo.` sender, or account `1369850` (L37–42). Called from `CopyTradingService.ExecuteDemoCopyAsync`. |
| `src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | `Build("D", …)` ×3 (flatten / open / close). Demo-gated. |
| `src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` L93 | `Build("D", …)` present (matrix helper). |

Claim 1 as written against **`CTraderFixSession.cs`** is true. I will **not** claim the tree has no `35=D` builder.

---

## 2. Claim 2 — `CanPromoteToLive` is false — **PASS**

Assigned file: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (**212** lines, full read).

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

| Check | Measured |
|---|---|
| Ceiling of `FromBaseline` | `SHADOW` (L200–201). No `LIVE` / `LIVE_CANDIDATE` return. |
| `AfterHighEarlyScore` | `=> TraderState.SHADOW` (L209) |
| `CanPromoteToLive` | **hard `false`**. Parameter `current` is unused. |
| Product call sites | `grep CanPromoteToLive` in `*.cs` = **definition + one unit test + one `_tmp` eval**. **Not** used as a runtime gate on the copy hop. |
| Test | `tests\Unit\BaselineScorerTests.cs` L21–26: three disciplined winners → `SHADOW`; `CanPromoteToLive(...).Should().BeFalse()`. |
| Enum still has LIVE | `TraderState.LIVE = 5` exists (`TraderState.cs` L10). The scorer **never emits it**. |

There is **no** code path in this file that emits `TraderState.LIVE`. Promotion to live is not a scorer function. Claim 2 is file-true.

Residual (does not fail the claim): roster admit (`CopyRosterEngine` + `XauUsdOneToOneCopyPolicy.IsTraderEligible`) can admit a `SHADOW` trader without calling `CanPromoteToLive`. That is dest-roster, not LIVE promotion.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL**

This claim is **false on disk**. I cannot confirm it, and I can **disprove** it.

### 3.1 Assigned-adjacent logon host does not re-pin

The only post-logon writer of `LiveRuntimeStatus` FIX fields is `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`.

After both `CTraderFixSession.TryLogonAsync` calls it writes Quote/Trade status and **logs** the flag. It never assigns it:

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

`grep` for `RealCopyEnabled =` under `*.cs` (product, not reports): **one hit**.

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

That is a **bind**, not a force-false. Successful `35=A` does not change the bool.

### 3.2 Lab env is `true`

`D:\Prop\.env` L73 (flag key only): `REAL_COPY_EXECUTION_ENABLED=true`.

`apps\api\Program.cs` L10 loads that env (`EnvFile.FindAndLoad()`), then L15 `AddTraderIntelligence`. `/api/health` L55 and `/api/settings` L76 echo `runtime.RealCopyEnabled` — they do **not** override it.

This slot did **not** obtain a live JSON echo (loopback blocked). File bind + env `true` + no re-pin is sufficient to **FAIL** the claim. I will **not** invent a live `realCopyEnabled` value.

### 3.3 Options default is unused by logon

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (`CTraderFixOptions.cs` L35). `CTraderFixLogonHostedService` does **not** read that POCO. `apps\fix-worker\Worker.cs` L21 reads `CTrader:RealCopyExecutionEnabled` with default `false` — that is the **fix-worker**, not API logon, and it still does not send.

### 3.4 What “after logon” actually leaves

| Object | After successful `35=A` |
|---|---|
| `_runtime.Quote.LoggedOn` / `_runtime.Trade.LoggedOn` | set from logon result |
| `_runtime.RealCopyEnabled` | **unchanged** from DI bind of env |
| This slot live GET of `/api/settings` | **not obtained** |

Cannot claim “forced false after logon.” The opposite wiring is on disk. Historical “hosted pin-false” reports are **stale** against this HEAD.

---

## 4. Claim 4 — sending now cannot be the profit path — **PASS_SCOPED_NOT_LIVE_OR_BOOKED**

Scope I **can** prove from files: **booked destination profit** and **live Pepperstone `1369850`**. Scope I **cannot** prove: “no `35=D` can leave this process.” Per the FAIL-if-unproved rule, the **unqualified** sentence is not tree-true. I therefore pass only the scoped reading and refuse `SAFE_BY_ABSENCE` for demo dest.

### 4.1 Assigned session cannot send a ticket

`CTraderFixSession` is Logon-only (claim 1). A `35=A` ack is not dest PnL.

### 4.2 Scorer cannot put anyone in LIVE

`CanPromoteToLive => false` (claim 2). `FromBaseline` ceiling is `SHADOW`.

### 4.3 Risk persist never arms FIX send on the hopper

Assigned `RiskEngine.cs` (**190** lines, full read) allow-send formula:

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

L90–93 (“Shadow path still evaluates risk but never allows FIX send”) is a **comment no-op**. When `RealExecutionEnabled == false` the method does not return; it falls through. The real gate is `allowSend` (needs the flag **and** recon **and** venue). Unit test `Real_flag_false_never_allows_fix_send` (`RiskEngineTests.cs` L21–26) uses `RealExecutionEnabled = false` and expects `AllowFixSend == false` even on `Approve`.

`D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` (full read):

- `VenueReconciled` **const `false`** (L20).
- Hopper `Evaluate` passes `Reconciled = VenueReconciled` (L304) → increasing actions reject `VENUE_NOT_RECONCILED` (`RiskEngine.cs` L84–85).
- Persist **hard-codes** `AllowFixSend = false` (L324) regardless of `decision.AllowFixSend`.
- Live-send branch L330 requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled`. `VenueReconciled` is const false → branch **dead**.

`GetStatusAsync` reports `VenueReconciled: DemoDest` (L67) to the UI. That is a **status lie relative to the hopper const**. The hopper still passes `Reconciled = false`. I do not treat the status DTO as a send license.

### 4.4 Dest profit is not computed

`OverviewDto.DestinationRealPnl` exists (`DashboardModels.cs` L16). `grep DestinationRealPnl` in `*.cs` = **the DTO field only**. The **only** product assignment is the positional literal `0` in `EfDashboardQueries.GetOverviewAsync` L44 (field order: `ShadowPnl`, then `DestinationRealPnl`, then `XauGross`, `XauNet`).

Assigned `LiveCopyPage.tsx` (**70** lines, full read):

- GETs only: `useCopyStatus` / `useCopyIntents` (`hooks.ts` L60–66 → `GET /api/copy/status` and `GET /api/copy/intents`).
- **No** dest-PnL field, **no** send button, **no** “profit” column.
- Blockers titled “Pepperstone cannot be filled” (L24).
- Empty-state L57 **admits** demo dest auto-send: “Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position.”

Sending **now** through the assigned session / hopper / dashboard is therefore **not a booked dest-profit path** and **not live Pepperstone**.

### 4.5 Residual the claim must not hide

HEAD **does** send on demo dest. This is dest **activity**, not dest **profit accounting**.

Lab dest identity (keys only, no secrets):

| Key | Value |
|---|---|
| `CTRADER_FIX_HOST` | `demo-us-eqx-01.p.c-trader.com` (starts with `demo-`) |
| `CTRADER_FIX_TRADE_SENDER_COMP_ID` | `demo.pepperstone.5328266` (starts with `demo.`) |
| `CTRADER_FIX_ACCOUNT_ID` | `5328266` (≠ `1369850`) |

Therefore `CopyTradingService.DemoDest` is **true** against this `.env`, and `NewOrderSingleImplemented => DemoDest` is **true**.

```45:50:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public bool DemoDest =>
        (_config["CTRADER_FIX_HOST"] ?? "").StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
        && (_config["CTRADER_FIX_TRADE_SENDER_COMP_ID"] ?? "").StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
        && _config["CTRADER_FIX_ACCOUNT_ID"] != "1369850";

    public bool NewOrderSingleImplemented => DemoDest;
```

`CopyTradingHostedService` 20s tick: `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**.

`ExecuteDemoCopyAsync` (L483–605):

- Returns 0 if `!DemoDest`.
- Does **not** read `RealCopyEnabled`.
- Does **not** call `RiskEngine.Evaluate`.
- Calls `CTraderFixCopyOpen.SendAsync` → `Build("D")` (open and close).
- Caps `MaxAutoLots = 0.05m` (L22) on **source** tickets, then sends **1:1** those lots.
- Refuses live identity `1369850` inside `CTraderFixCopyOpen` L37–42.
- Seeds a hardcoded ledger row for source `305750` / pos `21250421` if missing (L500–512). That is dest **bookkeeping**, not a profit constructor.

So: **sending now can open a demo dest ticket.** That ticket is **not** written into `DestinationRealPnl` (still `0`). It is **not** live Pepperstone. It is **not** a measured booked profit path. I will **not** claim tree-wide `SAFE_BY_ABSENCE` for demo dest.

This slot did not live-GET dest fills and did not send.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **PASS**

### 5.1 `SHADOW` is a source state, not dest cash

`BaselineScorer` / `TraderStateMachine` assign `TraderState.SHADOW` when `quality >= 70 && risk < 40` (L200–201). Fields used: source XAU features (`NetPnl`, martingale, SL use, …). **No dest account, no dest fill, no dest currency.**

`AfterHighEarlyScore() => SHADOW` (L209). Still a state enum.

### 5.2 Paper shadow fills are slippage, not dest PnL

Hopper (`CopyTradingService` L336–360): non-LIVE path sets `Status = "SHADOW_ONLY"` and, if a quote row exists **and** `Outcome == Approve`, writes `ShadowOrder` from `_shadow.SimulateEntry(...)`.

`ShadowCopyEngine.SimulateEntry` (`D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` L35–60) returns a **modeled** price/slippage. It does not write FIX. It does not touch `DestinationRealPnl`.

Dashboard `ShadowPnl` (`EfDashboardQueries.cs` L29):

```29:29:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var shadowPnl = await _db.ShadowOrders.SumAsync(s => (decimal?)s.SourceVsShadowSlippage, ct) ?? 0;
```

That is **sum of source-vs-shadow slippage**, then stuffed into `OverviewDto.ShadowPnl`. Next field `DestinationRealPnl` is the literal `0` (L44).

### 5.3 UI does not treat SHADOW as dest profit

`D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (**70** lines, full read):

```12:18:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-sm">
        <Stat label="REAL_COPY armed" value={status?.realCopyArmed ? 'YES' : 'NO'} hot={status?.realCopyArmed} />
        <Stat label="SHADOW traders" value={status?.shadowTraders ?? 0} />
        <Stat label="LIVE traders" value={status?.liveTraders ?? 0} />
        <Stat label="Live sends" value={status?.liveSends ?? 0} />
        <Stat label="Intents" value={status?.intents ?? 0} />
        <Stat label="Shadow fills" value={status?.shadowFills ?? 0} />
```

Counts only. No dest PnL. `useCopyStatus` / `useCopyIntents` are GETs. No POST.

`GetStatusAsync` `ShadowTraders` = count of scores with `CurrentState == SHADOW` (L59). `ShadowFills` = `ShadowOrders.Count` (L56). Neither is dest realized PnL.

### 5.4 Demo dest send is a **different** hop

Roster ADMIT uses `XauUsdOneToOneCopyPolicy.IsTraderEligible` (needs SHADOW-or-better — it **rejects** `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET` — plus 20 completed XAU, `XauNetPnl > 0`, demo/contest group). `ExecuteDemoCopyAsync` then sends for `Status == "ADMITTED"` opens.

That dest fill — if it happens — is **demo-account activity**, not the SHADOW score and not `ShadowPnl`. Claim 5 holds: **SHADOW on demo is not destination profit.**

`RiskEngine` L90–93 comment agrees in intent (shadow path must not FIX-send) but is a no-op; persist-false + session-without-D are the real paper wall. Demo dest send **bypasses** that wall. Bypass ≠ “SHADOW is dest profit.”

---

## 6. Live GET

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | **blocked** (`web_fetch` SSRF: loopback) |
| `http://localhost:5000/api/health` | **retrieve failed** (`open_page`) |
| `http://127.0.0.1:5000/api/settings` | **not fetched** |
| `http://127.0.0.1:5000/api/copy/status` | **not fetched** |

Launch profile would have been `http://localhost:5000` (`apps\api\Properties\launchSettings.json` L17). This slot **does not** claim a live `realCopyEnabled` JSON value. File bind + env `true` + no re-pin is sufficient to **FAIL claim 3**.

---

## 7. What this slot refuses to say

- Will not say “EX5 / copy edge / dest profit proven.”
- Will not recycle sibling dest-ledger fill prices or census integers as this-slot measurements (not re-read here).
- Will not say tree-wide `35=D=0` (stale). Assigned session is `35=A` only.
- Will not say `NewOrderSingleImplemented` is const `false` (HEAD is `=> DemoDest`).
- Will not say `RealCopyEnabled` is forced false after logon (claim 3 FAIL).
- Will not say demo dest is `SAFE_BY_ABSENCE`.
- Will not treat `GetStatusAsync.VenueReconciled: DemoDest` as hopper recon.

---

## 8. Files read (this slot)

| Path | Role |
|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | assigned (1) — 135/135 |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | assigned (2) — 212/212 |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | assigned (3/4) — 190/190 |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | assigned (4/5) — 70/70 |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | post-logon flag |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | sole `RealCopyEnabled =` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | flag + copyNote |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | hopper / demo send |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | sibling `Build("D")` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | sibling `Build("D")` ×3 |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 20s tick |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | dest PnL `0` |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | `DestinationRealPnl` |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | paper fill |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | SHADOW-or-better eligibility |
| `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs` | ADMIT / flatten |
| `D:\Prop\src\Domain\Enums\TraderState.cs` | LIVE enum exists; scorer does not emit |
| `D:\Prop\apps\api\Program.cs` | health/settings echo |
| `D:\Prop\apps\web\src\api\hooks.ts` | GET-only copy hooks |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | promote-false test |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | flag-false → no FIX send |
| `D:\Prop\.env` L49–50, L64, L73, L106 | host / account / sender / flag booleans only |

---

**End P500_VERIFY_39.** Slot **39**. Verdict **FAIL** (claim 3 disproved). Risk to live capital **NONE**; demo dest send **wired**.
