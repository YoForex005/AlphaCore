# P500_VERIFY_74 — Adversarial confirm (slot 74)

| Field | Value |
|---|---|
| Slot | **74** |
| Date | 2026-08-18 |
| Agent | P500_VERIFY_74 (adversarial; did **not** trust sibling `P500_*` / `W500_*` integers or verdicts) |
| Assigned | Read `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`. Confirm: (1) no `35=D` builder; (2) `CanPromoteToLive` is false; (3) `RealCopyEnabled` forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. **FAIL any claim that cannot be proved from a file or live GET.** |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **None.** Password / proxy / FIX password values were not quoted. Only the already-on-disk boolean `REAL_COPY_EXECUTION_ENABLED=true` is named. Public dest ids `5328266` / `1369850` appear in product source. |
| Live GET this slot | **Attempted and blocked.** `web_fetch` `http://127.0.0.1:5000/api/health` and `http://localhost:5000/api/copy/status` → `SSRF blocked` (loopback). `open_page` `http://127.0.0.1:5000/api/health` and `http://localhost:5000/api/settings` → retrieve fail. **No live JSON.** File proof only. |
| Live `35=D` sent this slot | **No.** |
| `REAL_COPY` flipped this slot | **No.** |
| Method | Full `read_file` of the four assigned files. Then logon host, DI bind, copy hopper, sibling session builders, dest-PnL constructor, shadow engine, roster/policy, hooks, `.env` L73 **flag key only**. Targeted `grep` for tag `35` in the session file, `RealCopyEnabled`, `CanPromoteToLive`, `DestinationRealPnl`, `Build("`. |
| Honesty rule | Sibling swarm PnL / census integers are **not** evidence. A comment is **not** a runtime pin. A dashboard label is **not** dest cash. Live GET that did not return JSON is **not** a measured `realCopyEnabled` value. Ledger dest ids are **not** a live mark of venue cash (hopper can seed them). |

---

## 0. Verdict (binding)

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS_SESSION / FAIL_UNSCOPED** | Assigned `CTraderFixSession.cs` **135/135**: only outbound MsgType is `(35, "A")` at L96. Same-folder product files **do** have `Build("D", …)` (`CTraderFixCopyOpen` L95; `CTraderFixDemoTestTrade` L139/163/197; `CTraderFixDemoMatrix` L93). Unscoped “no builder” is **false**. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(TraderState current) => false` at `BaselineScorer.cs` L211. Argument unused. `FromBaseline` never returns `LIVE` / `LIVE_CANDIDATE`. Unit test asserts SHADOW ≠ LIVE. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | **Disproved.** Logon host **reads** the flag and **never assigns** `false`. Sole `RealCopyEnabled =` write in `src/` is DI bind of `.env` `REAL_COPY_EXECUTION_ENABLED=true`. Live GET of the echo endpoints did not return JSON. |
| 4 | sending now cannot be the profit path | **FAIL** | Cannot prove product sending cannot move dest cash. Persist `AllowFixSend=false` and dest DTO constructor `0` are **not** a venue mark. Hosted `ExecuteDemoCopyAsync` → sibling `Build("D")` on demo dest is dest **activity**. Live GET blocked. |
| 5 | SHADOW on demo is not destination profit | **PASS** | `SHADOW` is a **source-trader state**. Dashboard `ShadowPnl` is `Sum(SourceVsShadowSlippage)`. `DestinationRealPnl` is constructor `0`. `LiveCopyPage` shows SHADOW **counts**, no dest cash. `ShadowCopyEngine.SimulateEntry` is not a venue fill. Residual: SHADOW-or-better is the dest **ADMIT floor**, not dest PnL. |

**Slot verdict: `FAIL`.**

Claim 3 is **disproved**, not merely unproved. Claim 1 as written without a file scope is also **false** on the product tree. Claim 4 cannot be proved from a file or live GET. One FAIL is enough.

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
| `src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | `Build("D", …)` at L93 (matrix helper). |

Claim 1 as written against **`CTraderFixSession.cs`** is true. Tree-wide “no `35=D` builder” is **false** on HEAD. This slot **FAIL**s the unscoped sentence.

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

`grep` for `RealCopyEnabled` under `src/`: **12 hits**. The **only assignment** is the DI bind:

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

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (`CTraderFixOptions.cs` L35). `CTraderFixLogonHostedService` does **not** read that POCO. `apps\fix-worker\Worker.cs` reads a worker-local flag — that is the **fix-worker**, not API logon.

### 3.4 What “after logon” actually leaves

| Object | After successful `35=A` |
|---|---|
| `_runtime.Quote.LoggedOn` / `_runtime.Trade.LoggedOn` | set from logon result |
| `_runtime.RealCopyEnabled` | **unchanged** from DI bind of env |
| This slot live GET of `/api/settings` | **not obtained** (loopback blocked) |

Cannot claim “forced false after logon.” The opposite wiring is on disk.

---

## 4. Claim 4 — sending now cannot be the profit path — **FAIL**

Instruction: FAIL any claim that cannot be proved from a file or live GET. This sentence is **not** file-proved as a product-wide fact.

### 4.1 What assigned files prove (not enough)

`CTraderFixSession` is Logon-only (claim 1 session scope). A `35=A` ack is not dest PnL.

`CanPromoteToLive => false` (claim 2). `FromBaseline` ceiling is `SHADOW`. Hopper live-send branch requires `score.CurrentState == TraderState.LIVE` (`CopyTradingService.cs` L330) → **dead** for scorer output.

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

That `if` does not `return`. Shadow-vs-send is **not** enforced here.

`CopyTradingService.cs` persist path:

- `VenueReconciled` **const `false`** (L20).
- Hopper `Evaluate` passes `Reconciled = VenueReconciled` (L304) → increasing actions reject `VENUE_NOT_RECONCILED` (`RiskEngine.cs` L84–85).
- Persist **hard-codes** `AllowFixSend = false` (L324) regardless of `decision.AllowFixSend`.
- Live-send branch L330 requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled`. `VenueReconciled` is const false → branch **dead**. Even if it ran, it only sets `Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED"` — it does **not** call a FIX writer.

`OverviewDto.DestinationRealPnl` exists (`DashboardModels.cs` L16). The **only** product assignment is the literal `0` in `EfDashboardQueries.GetOverviewAsync` (positional after `shadowPnl`). That is a **constructor**, not a dest mark.

### 4.2 What assigned UI itself says

Assigned `LiveCopyPage.tsx` (**70** lines, full read):

- L13: `REAL_COPY armed` is a **status echo**, not a pin.
- L24: “Live send blockers (Pepperstone cannot be filled)” — label, not a gate.
- L56–57 empty copy: **“Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position.”**

The assigned page **documents a dest send path**. That contradicts “sending now cannot be the profit path” as a product claim.

### 4.3 Hosted dest hopper can `35=D`

`CopyTradingHostedService.cs` L28–30 every 20s: `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**.

`ExecuteDemoCopyAsync` (`CopyTradingService.cs` L483–605):

- Gates only on `DemoDest` (demo host + demo sender + account **≠** `1369850`).
- Does **not** call `RiskEngine.Evaluate`.
- Does **not** read `_runtime.RealCopyEnabled`.
- Calls `CTraderFixCopyOpen.SendAsync` which emits `Build("D")` (L95 of that file).
- On fill, writes `intent.Status = "DEMO_SENT"` and ledger dest ids.

`data\demo_copy_ledger.json` records dest pos `237339770` / source `305750` / `DestClosed: false`. **Adversarial caveat:** hopper L500–512 **seeds** that same triple if missing. The file is **not** a live GET of dest cash. It is residual dest-activity evidence, not booked dest PnL.

### 4.4 Why the claim FAILs

| Statement | Status |
|---|---|
| Assigned session cannot send a ticket | **Proved** |
| Persist `AllowFixSend` is false | **Proved** |
| Dashboard dest PnL is constructor `0` | **Proved** |
| Sending cannot be dest profit | **Not proved** — dest hopper can `35=D`; live dest cash unknown |
| Live dest PnL = 0 | **Not proved** — live GET blocked |

FAIL. Booked-dashboard-zero is **not** dest-cash-zero.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **PASS**

Assigned `LiveCopyPage.tsx` shows `status?.shadowTraders` (a **count**) and intent rows. It never renders dest cash.

`SHADOW` is `TraderState = 3` (`TraderState.cs` L8). `FromBaseline` can emit it. That is a **source** classification.

Dashboard `ShadowPnl` is **not** dest:

```29:44:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var shadowPnl = await _db.ShadowOrders.SumAsync(s => (decimal?)s.SourceVsShadowSlippage, ct) ?? 0;
        // ...
            shadowPnl,
            0,   // DestinationRealPnl constructor
            0,
            0,
```

`ShadowCopyEngine.SimulateEntry` (`ShadowCopyEngine.cs` L35–60) models ask/bid + 0.05 latency points. Persist `EfTradingStore.PersistDemoShadowAsync` and hopper `GenerateShadowIntentsAsync` write `Status = "SHADOW_ONLY"` + `SourceVsShadowSlippage`. That is paper slippage vs source VWAP, **not** venue realized dest PnL.

### Residual (does not flip the claim)

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET` (L81–85). `CopyRosterEngine.Decide` then `AUTO_ADMIT`s eligible traders (`CopyRosterEngine.cs` L72–80). `ExecuteDemoCopyAsync` sends dest tickets for `ADMITTED` roster seats. So SHADOW-or-better is the dest **ADMIT floor**. That is a **gate**, not dest profit. The claim as written is true.

---

## 6. Live GET

| URL | Result |
|---|---|
| `GET http://127.0.0.1:5000/api/health` | `web_fetch` SSRF blocked; `open_page` retrieve fail |
| `GET http://localhost:5000/api/copy/status` | `web_fetch` SSRF blocked |
| `GET http://localhost:5000/api/settings` | `open_page` retrieve fail |

Launch profile binds API to `http://localhost:5000` (`apps\api\Properties\launchSettings.json` L17). This slot **cannot** echo `realCopyEnabled` from a process. File bind remains `.env` L73 `true`.

---

## 7. What this slot will not claim

- “EX5 / MQ5 / Quantum Queen” — out of scope.
- “Dest demo cash is $0” — no live dest GET.
- “REAL_COPY is false in the running API” — no live JSON.
- “No `35=D` exists in the product” — siblings prove otherwise.
- Ledger `237339770` / `4390.2` as measured dest PnL — hopper can seed that row.

---

## 8. Binding close

| Item | Value |
|---|---|
| Slot | 74 |
| Verdict | **FAIL** |
| Claim 1 | PASS_SESSION / FAIL_UNSCOPED |
| Claim 2 | PASS |
| Claim 3 | FAIL (disproved) |
| Claim 4 | FAIL (unproved; dest hop wired) |
| Claim 5 | PASS (SHADOW/slippage ≠ dest cash) |
| Risk to capital | **NONE** on live `1369850` (`SAFE_BY_ABSENCE`). Demo dest hop **not** absent. |
| This slot sent | **0** |
