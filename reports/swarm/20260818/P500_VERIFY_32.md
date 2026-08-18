# P500_VERIFY_32 — Adversarial four-file verify (slot 32)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_32.md` |
| Agent / slot | P500 adversarial verifier **32** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned SUTs (full re-read this slot) | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135), `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212), `D:\Prop\src\Domain\Risk\RiskEngine.cs` (190/190), `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (71/71) |
| Adjacent hops (opened only to try to **disprove**) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `ShadowCopyEngine.cs`, `EfTradingStore.PersistDemoShadowAsync`, `EfDashboardQueries.GetOverviewAsync`, `DashboardModels.OverviewDto`, `XauUsdOneToOneCopyPolicy.cs`, `CopyGroupFilter.cs`, `CopyRosterEngine.cs`, `apps/api/Program.cs`, `apps/web/src/api/hooks.ts`, `apps/api/appsettings.json`, `CTraderFixOptions.cs`, `TraderState.cs`, `BaselineScorerTests.cs`, `RiskEngineTests.cs`, lab `.env` L73 **boolean key only** |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Secrets printed | **None.** Quoted only the already-on-disk boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`. Tag 554 / passwords / proxy / DB strings never dumped. |
| Live GET this slot | **Blocked.** `web_fetch` SSRF-denied `http://127.0.0.1:5000/api/health`. `open_page` on the same URL: retrieve error. No `/api/copy/status` or `/api/settings` JSON this slot. Any claim that needs a live DTO is **FAIL**. |
| This slot sent `35=D` | **No** |
| `REAL_COPY` flipped | **No** |
| Method | Independent `read_file` of the four assigned files (full). Targeted `grep` for `35=D` / `Build("D")` / `CanPromoteToLive` / `RealCopyEnabled =` / `TraderState.LIVE` / `DestinationRealPnl`. Adjacent hops opened only to try to *disprove* a claim. Prior swarm text (`E002`, `W500_*`, sibling `P500_VERIFY_*`) treated as **untrusted**. |

**Honesty rule:** FAIL any assigned claim that is not proven from a file this slot or a live GET this slot. A compile-time default is not a runtime pin. `CTraderFixSession` having no `35=D` is not “the product has no `35=D` builder.” `AllowFixSend` on a risk DTO is not a socket write. `SHADOW` is a source state, not dest cash. Demo dest fills are not live Pepperstone profit. Do **not** print secrets.

---

## 0. Verdict (binding)

**FAIL — claim 3 is disproved from live files. Live GET unobtainable this slot (does not rescue claim 3).**

| # | Assigned claim | File-proven result | Class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** on assigned `CTraderFixSession.cs` (135/135). **FAIL** if read as product-wide. | Assigned file outbound MsgType is only `(35, "A")`. Sibling `CTraderFixCopyOpen.Build("D")` is a real builder and is **on** the hosted 20s hop. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive` is `=> false`. `FromBaseline` never emits `LIVE` or `LIVE_CANDIDATE`. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | Hosted logon **never writes** the flag. DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`. Sole `RealCopyEnabled =` write is that bind. |
| 4 | sending now cannot be the profit path | **PASS** (as worded) | Scorer cannot mint `LIVE`. Persist `AllowFixSend=false`. Venue const unreconciled. UI is GET-only. Demo `35=D` exists and is **not** live dest +EV / not booked `DestinationRealPnl`. |
| 5 | SHADOW on demo is not destination profit | **PASS** | `SHADOW` is a quality/risk label on **source** XAU. Paper `ShadowOrder` is `SimulateEntry`. Policy admits demo/contest **source** groups only. Dashboard `ShadowPnl` is slippage sum. `DestinationRealPnl` constructor is `0`. |

One-line:

```text
FAIL. CTraderFixSession is 35=A only. CanPromoteToLive is false. RealCopyEnabled is NOT forced false after logon (.env true + DI bind + no re-pin). Sending now is not the live profit path. SHADOW-on-demo is not dest profit. Demo dest hop can still Build("D") (refuses 1369850). Live GET blocked. Risk NONE on live 1369850; DEMO dest hop exists.
```

---

## 1. Claim 1 — no `35=D` builder — **PASS** (assigned file) / **FAIL** (product-wide)

Assigned file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines, full read this slot).

The only outbound builder is `BuildLogon`. Tag 35 is hard-coded `"A"` (Logon). There is no `"D"`, no `NewOrderSingle`, no generic `Build(type)` in this type.

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

Measured on this file:

| Fact | Evidence |
|---|---|
| Tag 35 outbound | `"A"` only (`L96`) |
| `WriteAsync` count | **1** (`L49`) — the logon bytes |
| `NewOrderSingle` / `Build("D")` / `"D"` MsgType / `35=D` | **0** (`grep` this file: no matches) |
| Socket lifetime | `using TcpClient` + `await using SslStream` (`L35–L39`) — disposed after one read |
| Inbound `35` | `Extract(reply, "35")` (`L55`) to accept Logon (`"A"`) or record reject. Not a builder. |
| Heartbeat / Resend / NewOrderSingle after ack | **None.** Method returns immediately after one `ReadAsync`. |

Hosted caller `CTraderFixLogonHostedService.ExecuteAsync` (`L48–L58`) calls `TryLogonAsync` twice (QUOTE 5211, TRADE 5212) and persists session rows. It never asks the session type to send an order.

**Adversarial residual (does not flip the assigned-file PASS; kills a product-wide reading):**

`grep` `Build("D"` under `*.cs` = **5** call sites, **none** in `CTraderFixSession.cs`:

| File | What it does |
|---|---|
| `CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra)` then `Write` on TRADE **5212** |
| `CTraderFixDemoMatrix.cs` L93 | `Build("D", ...)` |
| `CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | `Build("D", ...)` ×3 |
| `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566 | **calls** `CTraderFixCopyOpen.SendAsync` |
| `CopyTradingHostedService` L30 | 20s tick calls `ExecuteDemoCopyAsync` |

`CTraderFixCopyOpen` refuses live dest identity (`host` must start `demo-`, `sender` must start `demo.`, `account == "1369850"` fails closed) (`L37–L42`). That is a **demo dest sender**, not absence of a builder. Tools `DemoFixTestTrade` also call the siblings; they are not the assigned session type.

Literal `35=D` string under product `*.cs` = **0**. Builders pass MsgType `"D"` into a generic `Build`. Absence of the three-character literal is **not** absence of a NewOrderSingle constructor.

Claim 1 as “`CTraderFixSession` has no `35=D` builder” is **proven**. Claim 1 as “there is no `35=D` builder in this tree” is **false**. Assigned wording after “Read `CTraderFixSession.cs`” is the scoped reading → **PASS**.

---

## 2. Claim 2 — `CanPromoteToLive` is false — **PASS**

Assigned file `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212 lines, full read).

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
| `CanPromoteToLive` body | literal `false`; argument unused |
| `FromBaseline` returns | `INSUFFICIENT_DATA` / `RISK_BLOCKED` / `SHADOW` / `WATCH` / `EARLY_SCORE` only |
| `LIVE` or `LIVE_CANDIDATE` emitted here | **0** |
| `AfterHighEarlyScore` | `SHADOW`, not `LIVE` |
| Enum still has `LIVE = 5` / `LIVE_CANDIDATE = 4` (`TraderState.cs`) | yes — unused by this machine |
| Unit pin | `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` asserts `SHADOW` **and** `CanPromoteToLive(...).Should().BeFalse()` |

`DealIngestionService` persists `CurrentState = score.SuggestedState` (`L140`). No other product writer in `src\` assigns `TraderState.LIVE`. Dashboard / copy service only **count** `CurrentState == LIVE` (`EfDashboardQueries` L40–41, `CopyTradingService` L58). Counting is not promotion.

`CopyRosterEngine.Decide` admits demo/contest SHADOW (etc.) onto a roster (`ADMITTED`). That is a copy-seat flag, **not** a `TraderState.LIVE` mint and **not** a call to `CanPromoteToLive`.

Claim 2 is **proven** from the assigned file.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL**

None of the four assigned files write `RealCopyEnabled`.

| Assigned file | `RealCopyEnabled` |
|---|---|
| `CTraderFixSession.cs` | **0** mentions. Result is `LoggedOn` / `Status` / `LastError` only. |
| `BaselineScorer.cs` | **0** mentions. |
| `RiskEngine.cs` | Request field is `RealExecutionEnabled` (different name). Empty comment at L90–93 when it is false; `allowSend` **ANDs** it (`L147–150`). Engine does not own the runtime flag and does not run “after logon.” |
| `LiveCopyPage.tsx` | **Displays** `status?.realCopyArmed` (`L13`). GET-only. No setter. |

Adjacent hop — hosted logon **reads** the flag and **never assigns** it:

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

Writes after logon: `Quote.*` and `Trade.*` only. `_runtime.RealCopyEnabled` is interpolated into a log line. **No** `_runtime.RealCopyEnabled = false`.

Sole product assignment (`grep` `RealCopyEnabled =` under `src\` = **1** hit):

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Lab `.env` L73 (boolean only; no other keys quoted):

```text
REAL_COPY_EXECUTION_ENABLED=true
```

`EnvFile.FindAndLoad()` runs at API boot (`apps/api/Program.cs` L10). Therefore a process that loads that file constructs `LiveRuntimeStatus.RealCopyEnabled == true` **before** logon, and logon does **not** force it back to false.

`CTraderFixOptions.RealCopyExecutionEnabled` still defaults `false` (`CTraderFixOptions.cs` L35). That POCO is **not** the runtime object the API / copy service / dashboard read. `LiveRuntimeStatus` has no initializer default that wins over the DI bind.

`apps/api/appsettings.json` has `FeatureFlags.LiveCopyEnabled: false` — a **different name**, unused by `LiveRuntimeStatus`.

`apps/fix-worker/Worker.cs` reads `CTrader:RealCopyExecutionEnabled` (default false) for a log line and still refuses NewOrderSingle. That worker is not the API logon host and does not write `LiveRuntimeStatus`.

Claim 3 is **disproved**, not merely unproved. A live GET of `/api/settings` would have been corroboration only; it was blocked. File proof is enough to FAIL.

---

## 4. Claim 4 — sending now cannot be the profit path — **PASS** (as worded)

“Profit path” = booked live-destination +EV from NewOrderSingle on live Pepperstone (`1369850`). That path is not constructible from the assigned files or the copy persist hop.

### 4.1 Assigned session cannot send an order

`CTraderFixSession` writes one `35=A` and disposes the socket (§1). A successful TRADE logon is **not** a fill and is **not** dest PnL.

### 4.2 Scorer cannot mint LIVE

`CanPromoteToLive => false` and `FromBaseline` never returns `LIVE` (§2). Copy’s live-send branch requires `score.CurrentState == TraderState.LIVE` (`CopyTradingService.cs` L330). That conjunct is unreachable from the scorer.

### 4.3 Risk approve ≠ socket write; persist forces `AllowFixSend=false`

```147:171:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;

        if (IsReducing(request.Action))
        {
            return new RiskDecision
            {
                CopyIntentId = request.CopyIntentId,
                Outcome = RiskDecisionOutcome.Approve,
                ApprovedQuantity = request.RequestedQuantity,
                Reason = "RISK_REDUCTION",
                AllowFixSend = allowSend
            };
        }

        return new RiskDecision
        {
            CopyIntentId = request.CopyIntentId,
            Outcome = RiskDecisionOutcome.Approve,
            ApprovedQuantity = request.RequestedQuantity,
            Reason = "APPROVED",
            AllowFixSend = allowSend
        };
```

`RiskEngine` can theoretically set `AllowFixSend=true` if all four ANDs hold. The copy hop **does not persist that**:

```303:337:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    RealExecutionEnabled = _runtime.RealCopyEnabled,
                    Reconciled = VenueReconciled,
                    ...
                    AllowFixSend = false,
                    ...
                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
                else
                {
                    intent.Status = "SHADOW_ONLY";
```

`VenueReconciled` is `public const bool VenueReconciled = false` (`CopyTradingService.cs` L20). The LIVE-send `if` is dead even if `RealCopyEnabled` is true and even if `NewOrderSingleImplemented => DemoDest` is true. The “success” branch still only sets a **status string**; it does not call `CTraderFixCopyOpen`.

Unit pin: `RiskEngineTests.Real_flag_false_never_allows_fix_send` — with `RealExecutionEnabled=false` the engine Approves and `AllowFixSend` is false.

### 4.4 Assigned UI cannot send

`LiveCopyPage.tsx` (71/71) is display-only:

- Hooks: `useCopyStatus` / `useCopyIntents` → `GET /api/copy/status` and `GET /api/copy/intents` (`hooks.ts` L60–66).
- No `<form>`, no `POST`, no button, no FIX client.
- Banner: “Live send blockers (Pepperstone cannot be filled)” (`L24`).
- Empty-state text admits **demo dest auto-sends** (`L57`) — that is a **label**, not a sender. The sender is the hosted tick, not this page.

### 4.5 Booked dest profit constructor is literal `0`

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

`OverviewDto` field after `ShadowPnl` is `DestinationRealPnl` (`DashboardModels.cs` L16). It is the literal `0`. Overview UI renders `data.destinationRealPnl` (`OverviewPage.tsx` L27). Sending is not booked as dest profit.

### 4.6 Residual — demo dest hop **is** a sender (not the live profit path)

`CopyTradingHostedService` 20s tick (`L28–L30`): `TickRosterAsync` → `GenerateShadowIntentsAsync` → `ExecuteDemoCopyAsync`.

`ExecuteDemoCopyAsync` (`CopyTradingService.cs` L483–605):

- Returns 0 unless `DemoDest` (`host` starts `demo-`, trade sender starts `demo.`, account ≠ `1369850`).
- Calls `CTraderFixCopyOpen.SendAsync` for dest close and dest open.
- Does **not** call `RiskEngine.Evaluate`.
- Does **not** read `RealCopyEnabled`.
- Caps `MaxAutoLots = 0.05m`, `maxPerTick = 5`.
- Writes ledger + optional `intent.Status = "DEMO_SENT"`.

That is **demo dest activity**. It is not live `1369850`. It is not `DestinationRealPnl`. It is not “the profit path” of this book. Claim 4 as worded **PASSES**. Claiming “product `35=D=0` / no sender exists” would **FAIL**.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **PASS**

### 5.1 `SHADOW` is a source-trader state

`TraderState.SHADOW = 3` (`TraderState.cs` L8). `FromBaseline` assigns it when `quality >= 70 && risk < 40` after ≥3 completed XAU (`BaselineScorer.cs` L200–201). Inputs are **source** reconstructed trades (`ComputeFeatures` filters `t.Completed && t.IsXauUsd`). No dest account, no FIX, no cash.

### 5.2 Policy “demo” is the **source group**, not dest cash

`CopyGroupFilter.IsDemoOrContest` is true when a path segment is `demo` or `contest` (`CopyGroupFilter.cs` L17–19). `XauUsdOneToOneCopyPolicy.IsTraderEligible` requires that filter (`L105–109`) plus SHADOW-or-later, ≥20 XAU, source book `XauNetPnl > 0`. Roster admits those **source** seats. That is “copy **from** demo/contest MT5 groups,” not “dest profit on demo FIX.”

### 5.3 Paper shadow fills

`ShadowCopyEngine.SimulateEntry` / `SimulateExit` return a `ShadowFill` priced off a `DestinationQuote` in memory (`ShadowCopyEngine.cs` L35–82). No socket.

`EfTradingStore.PersistDemoShadowAsync` no-ops unless `state == TraderState.SHADOW`, then writes `CopyIntent.Status = "SHADOW_ONLY"` and a `ShadowOrder` from `SimulateEntry` (`L267–333`).

`CopyTradingService.GenerateShadowIntentsAsync` also writes `SHADOW_ONLY` + `SimulateEntry` when the dead LIVE-send `if` is false (always, given `VenueReconciled=false`).

### 5.4 Dashboard money labels

| Number | Construction | Dest cash? |
|---|---|---|
| `ShadowPnl` | `Sum(ShadowOrders.SourceVsShadowSlippage)` (`EfDashboardQueries` L29) | **No** — modeled slippage |
| `DestinationRealPnl` | literal `0` (`L44`) | **No** — not computed |
| `LiveCopyPage` “SHADOW traders” / “Shadow fills” | counts from `CopyGateStatus` (`L14`, `L18`) | **No** — integers |
| `LiveCopyPage` “Live sends” | `ExecutionIntents` with `SentAt != null` (`CopyTradingService` L57) | count, not dest PnL |

Assigned page does not render a dest-PnL number. It cannot turn SHADOW into destination profit.

Claim 5 is **proven**. Residual: demo dest `35=D` (§4.6) is a **different** object from SHADOW. SHADOW-on-demo-source ≠ dest profit. Demo dest fills, if any, are still not booked in `DestinationRealPnl`.

---

## 6. Live GET this slot

Attempted:

| URL | Tool | Result |
|---|---|---|
| `http://127.0.0.1:5000/api/health` | `web_fetch` | SSRF blocked (loopback) |
| `http://127.0.0.1:5000/api/copy/status` | `web_fetch` | SSRF blocked |
| `http://127.0.0.1:5000/api/settings` | `web_fetch` | SSRF blocked |
| `http://127.0.0.1:5000/api/health` | `open_page` | retrieve error |

No live JSON. File proof stands. Runtime `realCopyEnabled` DTO is **unverified this slot**; the DI bind + `.env` boolean already **disprove** “forced false after logon” without the DTO.

---

## 7. Risk to capital

| Surface | This slot |
|---|---|
| Live dest `1369850` | **NONE.** `CTraderFixSession` cannot send D. `CTraderFixCopyOpen` refuses that account / non-`demo-` host / non-`demo.` sender. Persist `AllowFixSend=false`. `VenueReconciled` const false. Scorer cannot mint LIVE. |
| Demo dest (public id `5328266` appears in defaults) | **Not absent.** Hosted 20s tick can `Build("D")` via `ExecuteDemoCopyAsync` when `DemoDest` is true, **without** `RiskEngine` and **without** reading `RealCopyEnabled`. This slot sent **0**. |
| SHADOW paper | **NONE** (in-process `SimulateEntry`). |
| This slot | No order built. No flag flipped. No secret printed. |

`SAFE_BY_ABSENCE` holds for **assigned** `CTraderFixSession` and for **live** dest identity. It does **not** hold as a product-wide “zero NewOrderSingle constructors” claim.

---

## 8. What this slot did **not** do

- Did not edit product or test source.
- Did not write `.env`.
- Did not send FIX.
- Did not trust sibling `P500_VERIFY_*` / `W500_*` integers or verdicts (re-read HEAD).
- Did not treat `appsettings.json` `LiveCopyEnabled: false` as `RealCopyEnabled`.
- Did not treat `CTraderFixOptions` default `false` as a runtime pin.

---

*End of P500_VERIFY_32. Product source was not modified. No `35=D` was built or sent by this slot. No secrets printed. `REAL_COPY` was not flipped.*
