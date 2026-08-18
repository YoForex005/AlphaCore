# P500_VERIFY_89 — Adversarial four-file verify (slot 89)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_89.md` |
| Agent / slot | P500 adversarial verifier **89** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned SUTs (full re-read this slot) | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135), `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212), `D:\Prop\src\Domain\Risk\RiskEngine.cs` (190/190), `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (71/71) |
| Adjacent hops (opened only to try to **disprove**) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `ShadowCopyEngine.cs`, `EfTradingStore.PersistDemoShadowAsync`, `EfDashboardQueries.GetOverviewAsync`, `DashboardModels.OverviewDto`, `XauUsdOneToOneCopyPolicy.cs`, `CopyGroupFilter.cs`, `CopyRosterEngine.cs`, `CopyLifecycle.cs`, `DemoCopyLedger.cs`, `data\demo_copy_ledger.json` (public dest ids only), `apps/api/Program.cs`, `apps/web/src/api/hooks.ts`, `apps/api/appsettings.json`, `CTraderFixOptions.cs`, `TraderState.cs`, `BaselineScorerTests.cs`, `RiskEngineTests.cs`, `CopyTradingModels.cs`, `OverviewPage.tsx` (dest P&L card), lab `.env` L73 **boolean key only** |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Secrets printed | **None.** Quoted only the already-on-disk boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`. Tag 554 / passwords / proxy / DB strings never dumped. |
| Live GET this slot | **Blocked.** `web_fetch` SSRF-denied `http://127.0.0.1:5000/api/health` and `/api/copy/status` and `/api/settings`. `open_page` on the same URLs: retrieve error. No live DTO this slot. Any claim that needs a live GET is **FAIL**. |
| This slot sent `35=D` | **No** |
| `REAL_COPY` flipped | **No** |
| Method | Independent `read_file` of the four assigned files (full). Targeted `grep` for `35=D` / `Build("D")` / `CanPromoteToLive` / `RealCopyEnabled =` / `TraderState.LIVE` / `DestinationRealPnl`. Adjacent hops opened only to try to *disprove* a claim. Prior swarm text (`E002`, `W500_*`, sibling `P500_VERIFY_*`) treated as **untrusted** and is not evidence. |

**Honesty rule:** FAIL any assigned claim that is not proven from a file this slot or a live GET this slot. A compile-time default is not a runtime pin. `CTraderFixSession` having no `35=D` is not “the product has no `35=D` builder.” `AllowFixSend` on a risk DTO is not a socket write. `SHADOW` is a source state, not dest cash. Demo dest fills are not live Pepperstone profit. Do **not** print secrets.

---

## 0. Verdict (binding)

**FAIL — claim 3 is disproved from live files. Live GET unobtainable this slot (does not rescue claim 3).**

| # | Assigned claim | File-proven result | Class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** on assigned `CTraderFixSession.cs` (135/135). **FAIL** if read as product-wide. | Assigned file outbound MsgType is only `(35, "A")`. Sibling `CTraderFixCopyOpen.Build("D")` is a real builder and is **on** the hosted 20s hop. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive` is `=> false`. `FromBaseline` never emits `LIVE` or `LIVE_CANDIDATE`. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | Hosted logon **never writes** the flag. DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`. Sole `RealCopyEnabled =` write is that bind. |
| 4 | sending now cannot be the profit path | **PASS** (as worded: live booked dest +EV) | Scorer cannot mint `LIVE`. Persist `AllowFixSend=false`. Venue const unreconciled. UI is GET-only. Booked `DestinationRealPnl` constructor is `0`. Demo `35=D` exists and is **not** live dest 1369850 +EV. |
| 5 | SHADOW on demo is not destination profit | **PASS** | `SHADOW` is a quality/risk label on **source** XAU. Paper `ShadowOrder` is `SimulateEntry`. Policy admits demo/contest **source** groups only. Dashboard `ShadowPnl` is slippage sum. `DestinationRealPnl` constructor is `0`. |

One-line:

```text
FAIL. CTraderFixSession is 35=A only. CanPromoteToLive is false. RealCopyEnabled is NOT forced false after logon (.env true + DI bind + no re-pin). Sending now is not the live booked profit path. SHADOW-on-demo paper book is not dest profit. Demo dest hop can still Build("D") (refuses 1369850). Live GET blocked. Risk NONE on live 1369850; DEMO dest hop exists.
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
| Reply parse | `Extract(reply, "35")`; accept only `msgType == "A"` (`L55–L56`) |
| Reject / disconnect | returns `LoggedOn = false`; no retry-as-order (`L67–L85`) |
| Sockets | `using TcpClient` + `await using SslStream` — disposed after one read |
| Literal `"D"` / `NewOrderSingle` | **0** hits in this file |

Hosted caller `CTraderFixLogonHostedService` only calls `TryLogonAsync` twice (QUOTE 5211, TRADE 5212) and persists session *status*. It does not send any other MsgType.

**Disproof of the unscoped wording.** Repo grep this slot found real `Build("D")` senders:

| File | Role |
|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 / `Build` L142–156 | `await Write(ssl, Build("D", ...))` after a TRADE logon. Called from `CopyTradingService.ExecuteDemoCopyAsync` (L528 close, L566 open). |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | `Build("D", ...)` ×3. Demo-gated (refuse `live-*` / `live.*` / account `1369850`). |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` L93 | local `SendD` → `Build("D", ...)`. |

`CopyTradingHostedService.ExecuteAsync` (L27–30) ticks every 20s: `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**. That last hop is a product sender, not a CLI toy.

`CTraderFixCopyOpen.SendAsync` refuses live dest identity (`host` must start `demo-`, `sender` must start `demo.`, `account == "1369850"` fails closed — L37–42). That is a dest-identity gate, not “no builder.”

**Claim 1 as assigned to `CTraderFixSession`: PASS. Claim 1 as “the product has no 35=D builder”: FAIL.**

---

## 2. Claim 2 — `CanPromoteToLive` is false — **PASS**

Assigned file `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212).

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

| Fact | Evidence |
|---|---|
| `CanPromoteToLive` | unconditional `=> false`; parameter `current` is unused |
| Ceiling of `FromBaseline` | `SHADOW` (L200–201). Never `LIVE`, never `LIVE_CANDIDATE` |
| `AfterHighEarlyScore` | `SHADOW` (L209) |
| `Score()` | calls `FromBaseline` only (L162); no other state mint |
| Unit lock | `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` asserts `SuggestedState == SHADOW` **and** `CanPromoteToLive(...).Should().BeFalse()` |

`TraderState` enum still *contains* `LIVE_CANDIDATE = 4` and `LIVE = 5` (`TraderState.cs`). That is a vocabulary hole, not a promotion path. `CopyTradingService` *counts* `CurrentState == LIVE` and *would* branch on it, but this scorer cannot emit those states.

No file this slot writes `CurrentState = TraderState.LIVE`. Claim 2 is proven.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL**

This is the binding fail. “Forced false after logon” requires a write of `RealCopyEnabled = false` on the logon hop. That write **does not exist**.

### 3.1 Hosted logon never assigns the flag

`CTraderFixLogonHostedService.ExecuteAsync` after both `TryLogonAsync` calls:

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

Writes: Quote/Trade `LoggedOn` / `Status` / `LastError` / `UpdatedAt`. **Read** of `_runtime.RealCopyEnabled` for a log line. Zero assignments to `RealCopyEnabled`. Successful logon does not pin the flag false. Failed logon does not pin the flag false.

### 3.2 Sole write is DI bind of env `true`

Repo grep `RealCopyEnabled\s*=` this slot: **one** hit.

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

Lab `.env` L73 (boolean key only; no secret values):

```text
REAL_COPY_EXECUTION_ENABLED=true
```

API boot (`apps/api/Program.cs` L10–15) calls `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()` then `AddTraderIntelligence`. A process that loads that file therefore starts with `LiveRuntimeStatus.RealCopyEnabled == true`. Logon leaves it true.

### 3.3 Adjacent POCO default is not a pin

`CTraderFixOptions.RealCopyExecutionEnabled` defaults to `false` (`CTraderFixOptions.cs` L32–35). That property is **not** the runtime object the API/copy hop reads. `CopyTradingService` reads `_runtime.RealCopyEnabled`. `GET /api/health` and `GET /api/settings` expose `_runtime.RealCopyEnabled`. A default on an unused (or separately bound) options type cannot prove “forced false after logon.”

`appsettings.json` FeatureFlags has `LiveCopyEnabled: false`. That name is **not** `REAL_COPY_EXECUTION_ENABLED` and is not what `DependencyInjection` binds.

`RiskEngine` does not write the runtime flag. `LiveCopyPage` only displays `status?.realCopyArmed`.

### 3.4 What would have proven the claim (absent)

A line such as `_runtime.RealCopyEnabled = false;` after `TryLogonAsync`, or a hard `RealCopyEnabled = false` ignoring env. Neither exists. Live GET of `/api/settings` was blocked, so this slot cannot quote a runtime DTO — and does not need one: the source already disproves a post-logon force-false.

**Claim 3 FAIL.**

---

## 4. Claim 4 — sending now cannot be the profit path — **PASS** (live booked dest +EV) with demo residual

“The profit path” in this product is booked destination real P&L (`OverviewDto.DestinationRealPnl`) / live Pepperstone dest `1369850`. That path is not “send now.”

### 4.1 Assigned files cannot send dest cash

**`CTraderFixSession`:** one `35=A` write, then dispose. No order.

**`RiskEngine.Evaluate`:** `AllowFixSend` is a boolean on a DTO, not a socket.

```147:171:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
        // ...
                AllowFixSend = allowSend
```

Rejects always set `AllowFixSend = false` (`L180–188`). The empty block at L90–93 when `RealExecutionEnabled == false` does **not** return; later `allowSend` still AND-gates the flag. Unit `Real_flag_false_never_allows_fix_send` locks `AllowFixSend == false` when `RealExecutionEnabled = false`.

Increasing actions with `Reconciled == false` reject `VENUE_NOT_RECONCILED` (`L84–85`) before that AND.

**`LiveCopyPage.tsx` (71/71):** `useCopyStatus` + `useCopyIntents` are GET-only (`hooks.ts` L60–65 → `/api/copy/status`, `/api/copy/intents`). No button, no `fetch` POST, no form. Empty-state copy (`L57`) *describes* dest auto-send; the page itself cannot fire it.

### 4.2 Persist hop cannot send

`CopyTradingService.GenerateShadowIntentsAsync`:

| Gate | Value |
|---|---|
| `VenueReconciled` | `public const bool VenueReconciled = false` (`CopyTradingService.cs` L20) |
| Risk request `Reconciled` | that const (`L304`) |
| Persist `AllowFixSend` | **hardcoded `false`** (`L324`) — engine result discarded |
| Live-send branch | `decision.AllowFixSend && CurrentState == LIVE && NewOrderSingleImplemented && VenueReconciled` (`L330`) — const false short-circuits; even then status is `"LIVE_SEND_BLOCKED_UNIMPLEMENTED"` — **no socket** |

`FromBaseline` cannot mint `LIVE`, so the LIVE conjunct is also dead from scoring.

Booked dest P&L:

```33:44:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
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
```

The `0` after `shadowPnl` is `DestinationRealPnl` (`DashboardModels.cs` L16). Overview UI shows “Dest. real P&L” from that field (`OverviewPage.tsx` L27). Sending on the persist hop cannot become booked dest profit.

`LiveRuntimeStatus.Snapshot()` copy notes (even if armed): “NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.” That comment is **stale relative to the demo hopper** (next subsection) but it is the persist-hop contract.

### 4.3 Residual — demo dest hopper *can* send (not live 1369850)

`NewOrderSingleImplemented => DemoDest` (`CopyTradingService.cs` L50). `DemoDest` is true when host starts `demo-`, TRADE sender starts `demo.`, and account is not `"1369850"` (`L45–48`).

`ExecuteDemoCopyAsync` **bypasses** `RiskEngine` / persist `AllowFixSend`. On `DemoDest` it calls `CTraderFixCopyOpen.SendAsync`, which writes `35=D` and treats `|150=F|` / `|39=2|` as fill. `CopyTradingHostedService` runs that every 20s.

On-disk ledger `D:\Prop\data\demo_copy_ledger.json` (public ids only; this slot did not send):

| Field | Value (already on disk) |
|---|---|
| Source login / pos | `305750` / `21250421` |
| Dest position / ClOrdId | `237339770` / `C20260818093047317` |
| Dest fill price | `4390.2` |
| DestClosed | `false` |

That is dest activity on a **demo** identity. `CTraderFixCopyOpen` refuses account `1369850`. Live booked `DestinationRealPnl` remains `0`.

**Claim 4 PASS as worded** (sending now is not the live booked profit path). Residual: demo dest `35=D` hopper is wired. A reading “no send can move dest cash anywhere” would be **FAIL** — that reading is not how this slot scored the claim, and is called out so it cannot be laundered into “SAFE_BY_ABSENCE on all dests.”

Live GET of `/api/copy/status` (would show `realCopyArmed` / `summary` / `liveSends`) was blocked. File proof is enough for the persist hop; live DTO is not used as a PASS.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **PASS**

### 5.1 SHADOW is a source label; the paper book is not dest cash

`FromBaseline` ceiling is `SHADOW`. `ShadowCopyEngine.SimulateEntry` / `SimulateExit` compute a modeled price from a `DestinationQuote` and return a `ShadowFill`. No TCP, no FIX, no account id.

`GenerateShadowIntentsAsync` writes `Status = "SHADOW_ONLY"` and, on approve + quote, a `ShadowOrder` from `SimulateEntry` (`CopyTradingService.cs` L336–359). Persist `AllowFixSend = false`.

`EfTradingStore.PersistDemoShadowAsync` early-returns unless `state == SHADOW`, then writes `SHADOW_ONLY` + `SimulateEntry`. Same paper path.

Dashboard `ShadowPnl` is `Sum(ShadowOrders.SourceVsShadowSlippage)` (`EfDashboardQueries.cs` L29). That is source-vs-shadow **slippage**, not dest realized cash. `DestinationRealPnl` is the literal `0` cited in §4.2.

`CopyGroupFilter.IsDemoOrContest` gates **source MT5 group** path segments `demo` / `contest`. That is which *source* books may be copied. It is not dest P&L.

Policy (`XauUsdOneToOneCopyPolicy.IsTraderEligible`): `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` fail `TRADER_NOT_SHADOW_YET`. `SHADOW` (and `LIVE_CANDIDATE` / `LIVE`) can be eligible if 20 completed XAU, `XauNetPnl > 0`, no size-pattern flags, demo/contest group. Eligibility ≠ dest cash.

`LiveCopyPage` displays `shadowTraders` and `shadowFills` as counts. It does not mark-to-market dest.

### 5.2 Residual — SHADOW-eligible source can ride the demo dest hopper

`CopyRosterEngine.Decide` admits when `IsTraderEligible` is true. `ExecuteDemoCopyAsync` then opens dest for `ADMITTED` roster seats with an open XAUUSD reconstructed trade (`CopyTradingService.cs` L542–598). That hop does **not** re-check `CurrentState == SHADOW` vs `LIVE`. A SHADOW source that is ADMITTED can therefore cause a demo dest `35=D`.

That residual is dest activity from an **ADMITTED roster**, not from the SHADOW paper book. The assigned claim is “SHADOW on demo is not destination profit”: the SHADOW *simulation* / `ShadowPnl` is not dest profit. Proven. The roster hopper is the same residual as §4.3.

---

## 6. Live GET

Attempted this slot:

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | `web_fetch` SSRF-blocked; `open_page` retrieve error |
| `http://localhost:5000/api/copy/status` | `web_fetch` SSRF-blocked |
| `http://127.0.0.1:5000/api/settings` | `web_fetch` SSRF-blocked; `open_page` retrieve error |

No live JSON. Claims that would need a runtime DTO (e.g. “process currently reports `realCopyEnabled: false`”) are **FAIL**. Claim 3 is already FAIL from files. Claims 1 (session), 2, 4 (booked path), 5 (paper SHADOW) do not require the DTO.

---

## 7. Risk to capital

| Surface | This slot |
|---|---|
| Assigned `CTraderFixSession` | **NONE** — `35=A` logon only; socket disposed |
| Persist / risk / scorer hop | **NONE** — no `LIVE` mint; persist `AllowFixSend=false`; `VenueReconciled=false`; booked `DestinationRealPnl=0` |
| Live Pepperstone dest `1369850` | **NONE** — `CTraderFixCopyOpen` / `CTraderFixDemoTestTrade` refuse that account id |
| Demo dest hopper (public id `5328266`) | **EXISTS** — hosted 20s `ExecuteDemoCopyAsync` → `Build("D")`. On-disk ledger already has dest pos `237339770`. Not live booked +EV. |
| `LiveCopyPage` | **NONE** — GET-only |
| This slot | Did not send `35=D`. Did not flip `REAL_COPY`. Did not print secrets. |

---

## 8. What was not used as proof

- Sibling `P500_VERIFY_*` / `W500_*` / `E002` integers and verdicts (untrusted; some still say copy hop `NewOrderSingleImplemented=false` / `SAFE_BY_ABSENCE`, which is **stale** vs current `NewOrderSingleImplemented => DemoDest` + `ExecuteDemoCopyAsync`).
- `CTraderFixOptions` default `false` as a runtime pin.
- README / `docs/architecture.md` / `CREDENTIALS_AND_COPY_STATUS.md` flag tables (conflict with `.env` L73 `true`).
- Live GET JSON (blocked).

---

## 9. End

**End P500_VERIFY_89.** Slot **89**. Verdict **FAIL** (claim 3 disproved; claim 1 unscoped false). Risk to live capital **NONE**; demo dest send **wired**. Product source was not modified. No secrets printed. This slot did not send `35=D`. `REAL_COPY` was not flipped.
