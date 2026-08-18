# P500_VERIFY_72 — Adversarial confirm (slot 72)

| Field | Value |
|---|---|
| Slot | **72** |
| Date | 2026-08-18 |
| Agent | P500_VERIFY_72 (adversarial; did **not** trust sibling `P500_*` / `W500_*` integers or verdicts) |
| Assigned | Read `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`. Confirm: (1) no `35=D` builder; (2) `CanPromoteToLive` is false; (3) `RealCopyEnabled` forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. **FAIL any claim that cannot be proved from a file or live GET.** |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **None.** Password / proxy / FIX password values were not quoted. Only the already-on-disk boolean `REAL_COPY_EXECUTION_ENABLED=true` is named. Public dest ids `5328266` / `1369850` appear in product source. |
| Live GET this slot | **Attempted and blocked.** `web_fetch` `http://127.0.0.1:5000/api/health` and `http://localhost:5000/api/copy/status` → `SSRF blocked` (loopback). `open_page` `http://127.0.0.1:5000/api/health` → retrieve fail. **No live JSON.** File proof only. Runtime `realCopyEnabled` bit is **unmeasured**. |
| Live `35=D` sent this slot | **No.** |
| `REAL_COPY` flipped this slot | **No.** |
| Method | Full `read_file` of the four assigned files. Then logon host, DI bind, copy hopper, sibling session builders, dest-PnL constructor, shadow engine, roster/policy, persist-shadow, hooks, `.env` L73 **flag key only**, ledger file as dest-activity residual (not venue cash). Targeted `grep` for tag `35` in the session file, `RealCopyEnabled\s*=`, `CanPromoteToLive`, `DestinationRealPnl`, `Build("D")`. |
| Honesty rule | Sibling swarm PnL / census integers are **not** evidence. A comment is **not** a runtime pin. A dashboard constructor `0` is **not** dest cash. Live GET that did not return JSON is **not** a measured `realCopyEnabled` value. This slot sent **0**. |

---

## 0. Verdict (binding)

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS_SESSION / FAIL_UNSCOPED** | Assigned `CTraderFixSession.cs` **135/135**: only outbound MsgType is `(35, "A")` at L96. Same-folder product files **do** have `Build("D", …)` (`CTraderFixCopyOpen` L95; `CTraderFixDemoTestTrade` L139/163/197; `CTraderFixDemoMatrix` L93). Unscoped “no builder” is **false**. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(TraderState current) => false` at `BaselineScorer.cs` L211. Argument unused. `FromBaseline` never returns `LIVE` / `LIVE_CANDIDATE`. Unit test asserts SHADOW ≠ LIVE. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | **Disproved.** Logon host **reads** the flag and **never assigns** `false`. Sole `RealCopyEnabled =` write in `src/` is DI bind of `.env` `REAL_COPY_EXECUTION_ENABLED=true`. Live GET of the echo endpoints did not return JSON. |
| 4 | sending now cannot be the profit path | **FAIL** (unscoped) / **PASS_NOT_BOOKED_DEST_PROFIT** (session+persist+DTO) | Assigned session cannot send a ticket. Persist `AllowFixSend=false`. `CanPromoteToLive` is hard-false. `VenueReconciled` const `false`. `DestinationRealPnl` is a literal `0`. **Unscoped claim is false:** hosted `ExecuteDemoCopyAsync` → sibling `Build("D")` on demo dest is dest **activity** that can move demo venue cash. Live GET of dest PnL **not obtained**. Cannot prove “sending cannot be dest profit.” |
| 5 | SHADOW on demo is not destination profit | **PASS** | `SHADOW` is a **source-trader state**. Dashboard `ShadowPnl` is `Sum(SourceVsShadowSlippage)`. `DestinationRealPnl` is constructor `0`. `LiveCopyPage` shows SHADOW **counts**, no dest cash. `ShadowCopyEngine.SimulateEntry` is not a venue fill. Residual: SHADOW-or-better is the dest **ADMIT floor**, not dest PnL. |

**Slot verdict: `FAIL`.**

Claim 3 is **disproved**, not merely unproved. Claim 1 as written without a file scope is also **false** on the product tree. Claim 4 as written without a booked-DTO scope is **false** on the hosted demo hop. One FAIL is enough.

**Risk to capital:** **NONE on live `1369850`** (`SAFE_BY_ABSENCE` for `CTraderFixSession` + `CTraderFixCopyOpen` refuse of that account). **Not absent on demo dest** if `DemoDest` is true: hosted `ExecuteDemoCopyAsync` can emit `35=D` **without** `RiskEngine.Evaluate` and **without** reading `RealCopyEnabled`. This slot sent **0**.

---

## 1. Claim 1 — no `35=D` builder — **PASS_SESSION / FAIL_UNSCOPED**

Assigned file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135** lines, full read).

Grep of tag `35` in this file = **3 hits**, all logon:

| Line | Role |
|---|---|
| L55 | inbound parse `Extract(reply, "35")` |
| L73 | error string `Logon rejected 35={msgType}` |
| L96 | outbound `(35, "A")` |

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
| `WriteAsync` count | **1** (L49, the logon bytes) |
| `35=D` / `(35, "D")` / `Build("D")` / `NewOrderSingle` in this file | **0** |
| Socket lifetime | `using TcpClient` + `await using SslStream`; disposed after one read |
| Inbound `35=A` treated as | `LoggedOn = true` (L56–64) — **logon ack, not a fill** |

Adversarial residual (**fails the unscoped wording**): sibling product files **do** have a `35=D` builder. This slot **read** them.

| File | What this slot measured |
|---|---|
| `src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra)` after TRADE logon + SecurityList. **Refuses** non-`demo-` host, non-`demo.` sender, or account `1369850` (L37–42). Hosted by `CopyTradingService.ExecuteDemoCopyAsync`. |
| `src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | `Build("D", …)` at L139 / L163 / L197 (demo test helper; refuses live `1369850`). |
| `src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | `Build("D", …)` at L93 (`SendD` helper). |

Product `Build("D")` count this slot: **5**. Claim 1 as written against **`CTraderFixSession.cs`** is true. Tree-wide “no `35=D` builder” is **false** on HEAD. This slot **FAIL**s the unscoped sentence.

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
| Reachable set | `{INSUFFICIENT_DATA, RISK_BLOCKED, SHADOW, WATCH, EARLY_SCORE}` |
| `AfterHighEarlyScore` | `=> TraderState.SHADOW` (L209) |
| `CanPromoteToLive` | **hard `false`**. Parameter `current` is unused. |
| Enum still has LIVE tokens | `TraderState.cs` L9–10 `LIVE_CANDIDATE=4`, `LIVE=5` — **unused by this machine**. |
| Test | `tests\Unit\BaselineScorerTests.cs` L21–26: three disciplined winners → `SHADOW`; `CanPromoteToLive(...).Should().BeFalse()`. |

There is **no** code path in this file that emits `TraderState.LIVE`. Promotion to live is not a scorer function.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL**

This claim is **false on disk**. Assigned files do not mention `RealCopyEnabled` at all (`CTraderFixSession` / `BaselineScorer` / `RiskEngine` / `LiveCopyPage` have **zero** assignments). The only post-logon writer of FIX runtime fields is adjacent and was read.

### 3.1 Logon host does not re-pin

`D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`

After both `TryLogonAsync` calls it writes Quote/Trade status and **logs** the flag. It never assigns it:

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

`grep` for `RealCopyEnabled\s*=` under product `*.cs` = **1 hit**. The **only assignment** is the DI bind:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

That is a **bind**, not a force-false. Logon host L70 **reads** `_runtime.RealCopyEnabled`. Copy service L64 / L303 / L621 **reads** it. Dashboard L52 / L208 **echoes** it. Snapshot L41 **echoes** it. None force `false`.

### 3.2 Lab env is `true`

`D:\Prop\.env` L73 (flag key only): `REAL_COPY_EXECUTION_ENABLED=true`.

`apps\api\Program.cs` L10 loads that env (`EnvFile.FindAndLoad()`), then L15 `AddTraderIntelligence`. `/api/health` L55 and `/api/settings` L76 echo `runtime.RealCopyEnabled` — they do **not** override it.

This slot **did not** obtain live JSON, so it does **not** claim a measured process bit. File bind + env `true` + no re-pin is enough to **FAIL** “forced false after logon.”

### 3.3 Options default is unused by logon

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (`CTraderFixOptions.cs` L35). `CTraderFixLogonHostedService` does **not** read that POCO. `apps\fix-worker\Worker.cs` L21 reads `CTrader:RealCopyExecutionEnabled` with default `false` — that is the **fix-worker**, not API logon. A default-false POCO that logon never reads is **not** a post-logon force-false.

### 3.4 What “after logon” actually leaves

| Object | After successful `35=A` |
|---|---|
| `_runtime.Quote.LoggedOn` / `_runtime.Trade.LoggedOn` | set from logon result |
| `_runtime.RealCopyEnabled` | **unchanged** from DI bind of env |
| This slot live GET of `/api/settings` | **not obtained** (loopback blocked) |

Cannot claim “forced false after logon.” The opposite wiring is on disk.

---

## 4. Claim 4 — sending now cannot be the profit path — **FAIL** unscoped

Scope this slot **will prove**: booked destination profit via the assigned session / hopper persist / dashboard constructor is **not** the path. Scope this slot **will not prove**: “zero senders exist anywhere” or “sending cannot move dest cash.”

### 4.1 Assigned session cannot send a ticket

`CTraderFixSession` is Logon-only (claim 1 session scope). A `35=A` ack is not dest PnL.

### 4.2 Scorer cannot put anyone in LIVE

`CanPromoteToLive => false` (claim 2). `FromBaseline` ceiling is `SHADOW`. Hopper live-send branch requires `score.CurrentState == TraderState.LIVE` (`CopyTradingService.cs` L330) → **dead** for scorer output.

### 4.3 Risk persist never arms FIX send

Assigned `RiskEngine.cs` (**189** lines, full read). Allow-send formula:

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

`Reject(...)` always sets `AllowFixSend = false` (L180–188).

L90–93 is a **comment no-op**:

```90:93:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.RealExecutionEnabled == false && request.Action != CopyIntentAction.CloseExposure)
        {
            // Shadow path still evaluates risk but never allows FIX send.
        }
```

That `if` does not `return`. Shadow-vs-send is **not** enforced here. The real gates are `allowSend` plus persist-false.

`D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` (read this slot):

- `VenueReconciled` **const `false`** (L20).
- Hopper `Evaluate` passes `Reconciled = VenueReconciled` (L304) → increasing actions reject `VENUE_NOT_RECONCILED` (`RiskEngine.cs` L84–85).
- Persist **hard-codes** `AllowFixSend = false` (L324) regardless of `decision.AllowFixSend`.
- Live-send branch L330 requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled`. `VenueReconciled` is const false → branch **dead**. Even if it ran, it only sets `Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED"` — it does **not** call a FIX writer.

Unit `RiskEngineTests.Real_flag_false_never_allows_fix_send` (L21–26) only covers `RealExecutionEnabled = false` + `Reconciled = true`. It does **not** prove the hosted hop cannot send.

### 4.4 Dest profit is not computed (constructor `0` is not a mark)

`OverviewDto.DestinationRealPnl` exists (`DashboardModels.cs` L16). The **only** product assignment is the literal `0` in `EfDashboardQueries.GetOverviewAsync` (positional arg after `shadowPnl`):

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

`grep DestinationRealPnl` under product `*.cs` = **the DTO field only**. Constructor `0` is dest PnL **accounting**, not dest cash.

Assigned `LiveCopyPage.tsx` (**70** lines, full read) has **no** dest-PnL field, **no** send button, **no** “profit” column. It shows counts + blockers titled “Pepperstone cannot be filled” (L24). Hooks `useCopyStatus` / `useCopyIntents` are GETs (`hooks.ts` L60–65). No POST.

Sending **now** through the assigned session / risk persist / dashboard is therefore **not a booked dest-profit path**. That scoped sentence is **PASS_NOT_BOOKED_DEST_PROFIT**.

### 4.5 Residual that fails the unscoped claim

HEAD **does** send on demo dest. That is dest **activity**. This slot will **not** pretend `SAFE_BY_ABSENCE` on that hop. The assigned claim was “sending now cannot be the profit path,” not “dashboard dest PnL is a constructor 0.”

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
- Calls `CTraderFixCopyOpen.SendAsync` → `Build("D")`.
- Caps `MaxAutoLots = 0.05m` (L22) on **source** tickets, then sends **1:1** those lots (`AllocationFactor = 1m`).
- Seeds a hardcoded ledger row for source `305750` / pos `21250421` / dest pos `237339770` if missing (L500–512).
- Refuses live identity `1369850` inside `CTraderFixCopyOpen` L37–42.

`LiveCopyPage.tsx` L57 empty-state **admits** this: “Demo dest auto-sends after a trader is ADMITTED … Dest closes when that MT5 position closes.”

On-disk residual (`D:\Prop\data\demo_copy_ledger.json`, file not live GET): one open dest fill, `DestClosed=false`, lots `0.01`, dest pos `237339770`, dest fill price `4390.2`. That is **ledger residue**, not a venue mark this slot obtained. It is enough to refuse “no dest send ever existed.”

So: **sending now can open a demo dest ticket.** That ticket is **not** written into `DestinationRealPnl` (still `0`). It is **not** live Pepperstone. It **can** be dest cash at the demo venue. Unscoped “cannot be the profit path” is **FAIL**. Live GET of dest cash **not obtained** — even if the residual were absent, the claim would still be **unproved**.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **PASS**

### 5.1 `SHADOW` is a source state, not dest cash

`BaselineScorer` / `TraderStateMachine` assign `TraderState.SHADOW` when `quality >= 70 && risk < 40` (L200–201). Fields used: source XAU features (`NetPnl`, martingale, SL use, …). **No dest account, no dest fill, no dest currency.**

`AfterHighEarlyScore() => SHADOW` (L209). Still a state enum.

Quality formula (L152–160) can be high while `NetPnl` is unused except `+15` if `NetPnl > 0`. That is source scoring, not dest profit.

### 5.2 Paper shadow fills are slippage, not dest PnL

Hopper (`CopyTradingService` L336–360): non-LIVE path sets `Status = "SHADOW_ONLY"` and, if a quote row exists **and** `Outcome == Approve`, writes `ShadowOrder` from `_shadow.SimulateEntry(...)`.

`EfTradingStore.PersistDemoShadowAsync` (L267–333) also only writes paper `SHADOW_ONLY` + `SimulateEntry` when `state == SHADOW`.

`ShadowCopyEngine.SimulateEntry` (read this slot) returns a **modeled** price/slippage. It does not write FIX. It does not touch `DestinationRealPnl`.

Dashboard `ShadowPnl` (`EfDashboardQueries.cs` L29):

```29:29:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var shadowPnl = await _db.ShadowOrders.SumAsync(s => (decimal?)s.SourceVsShadowSlippage, ct) ?? 0;
```

That is **sum of source-vs-shadow slippage**, then stuffed into `OverviewDto.ShadowPnl`. Next field `DestinationRealPnl` is the literal `0`.

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

`GetStatusAsync` `ShadowTraders` = count of scores with `CurrentState == SHADOW` (L59). `ShadowFills` = `ShadowOrders.Count` (L56). `LiveSends` = `ExecutionIntents` with `SentAt != null` (L57). None of those is dest realized PnL.

### 5.4 Residual the claim must not hide

SHADOW **is** the dest **ADMIT floor**:

- `XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET` (L81–85). Eligible set is SHADOW / LIVE_CANDIDATE / LIVE, plus 20 completed XAU, `XauNetPnl > 0`, demo/contest group, no size-pattern flags.
- `CopyRosterEngine.Decide` ADMITs when `IsTraderEligible` (L72–80, reason `AUTO_ADMIT`).
- `ExecuteDemoCopyAsync` then sends `35=D` for `Status == "ADMITTED"` roster seats (L542–569).

So: **SHADOW paper ≠ dest profit** (proved). **SHADOW-or-better can unlock dest send** (proved). This slot still **PASS**es claim 5 as written: the SHADOW book itself is not dest cash. It does **not** claim SHADOW cannot cause dest activity.

---

## 6. Live GET (required; not obtained)

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | `web_fetch` SSRF blocked (loopback). `open_page` retrieve fail. |
| `http://localhost:5000/api/copy/status` | `web_fetch` SSRF blocked. |
| `http://localhost:5000/api/settings` | not separately fetched; same loopback policy. |

`apps\api\Properties\launchSettings.json` binds `http://localhost:5000`. Echo endpoints exist (`Program.cs` L33–58 `/api/health` `realCopyEnabled`; L71–76 `/api/settings` `REAL_COPY_EXECUTION_ENABLED`; L102 `/api/copy/status`). **No JSON body this slot.** File bind remains the only `RealCopyEnabled` proof.

---

## 7. What this slot will not claim

- That EX5 / Quantum Queen work is in scope (it is not).
- That dest dashboard `0` means dest cash is zero.
- That `.env` `true` is the live process bit (GET blocked).
- That this slot sent, closed, or flattened any dest ticket.
- Any password, proxy, or FIX secret value.

---

## 8. Files read (absolute)

| Path | Role |
|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | assigned; 135/135; `35=A` only |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | assigned; 212/212; `CanPromoteToLive => false` |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | assigned; 189/189; `AllowFixSend` formula + comment no-op |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | assigned; 70/70; counts + demo auto-send empty-state |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | sibling `Build("D")` + refuse `1369850` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | sibling `Build("D")` ×3 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | sibling `Build("D")` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | logon reads flag; no re-pin |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | sole `RealCopyEnabled =` bind |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | persist-false; hosted `ExecuteDemoCopyAsync` |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 20s tick hopper |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | dest PnL constructor `0`; shadow = slippage sum |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | paper SHADOW persist |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | snapshot echo; no force-false |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | `DestinationRealPnl` field |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | SHADOW floor; demo/contest required |
| `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs` | `AUTO_ADMIT` |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | `SimulateEntry` paper |
| `D:\Prop\src\Domain\Enums\TraderState.cs` | LIVE tokens unused by scorer |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | unused default-false POCO |
| `D:\Prop\apps\api\Program.cs` | env load + echo GETs |
| `D:\Prop\apps\api\Properties\launchSettings.json` | `:5000` |
| `D:\Prop\apps\web\src\api\hooks.ts` | GET-only copy hooks |
| `D:\Prop\apps\fix-worker\Worker.cs` | unused-by-API default-false |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | SHADOW ≠ LIVE |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | flag-false never `AllowFixSend` |
| `D:\Prop\.env` L73 | flag key only: `true` |
| `D:\Prop\data\demo_copy_ledger.json` | dest-activity residue, not live GET |

---

**DONE.** Reviewer-facing: five-claim bundle **FAIL**. Live `1369850` **NONE**. Demo dest hop **wired**. This slot sent **0**.
