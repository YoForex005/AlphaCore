# P500_VERIFY_50 — Adversarial profit-path verify (slot 50)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_50.md` |
| Agent / slot | P500 adversarial **verify 50** |
| Date | 2026-08-18 |
| Role | Independent verifier. Did **not** trust sibling P500_BOOK / CREDENTIALS / README / prior VERIFY prose. Re-read the four assigned files + the send/logon hop needed to judge unscoped claims. |
| Assigned files | `CTraderFixSession.cs` (135/135), `BaselineScorer.cs` (212/212), `RiskEngine.cs` (189/189), `LiveCopyPage.tsx` (70/70) |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Flag + already-public dest host/account/sender prefix only. |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live GET this pass | **Blocked.** `web_fetch` `http://127.0.0.1:5000/api/{health,copy/status,settings}` → SSRF private-IP deny. **No live GET body.** Any claim that needs a live body is **FAIL**. |
| Live attach / send this pass | **No.** No Manager Connect. No TLS. No Logon. No `35=D` from this slot. |
| Method | Full `read_file` of the four assigned files. Adjacent this pass: `CTraderFixLogonHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs` (`SendD`), `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `apps/api/Program.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `CopyGroupFilter.cs`, `DemoCopyLedger.cs`, `EfTradingStore.PersistDemoShadowAsync`, `DealIngestionService` score upsert, `TraderState.cs`, `data/demo_copy_ledger.json` (public dest ids only), `hooks.ts`, `launchSettings.json` (API `:5000`), `BaselineScorerTests.cs`, `RiskEngineTests.cs`. Grep: `Build("D")` / `RealCopyEnabled =` / `CanPromoteToLive` / `ExecutionIntents.Add` / `DestinationRealPnl`. Flag-only `.env` L49/L50/L56/L64/L73. |

**Honesty rule:** FAIL any claim that cannot be proven from a file this slot or a live GET. Prior swarm prose is not evidence. A TLS Logon `35=A` is not a NewOrderSingle. `SAFE_BY_ABSENCE` on live `1369850` is not “flag stays false.” A demo hopper that can `Build("D")` is not `CTraderFixSession`. Destination constructor `$0` is not a measured dest book. Wanting profit is not an edge. Do not print secrets.

---

## 0. Verdict (binding)

**FAIL.** Claim 2 is file-proven. Claims **1, 3, 4** fail the bar (unscoped / disproven / not proven). Claim **5** is proven only as “paper SHADOW ledger ≠ dest cash”; as an absolute “SHADOW on demo cannot be dest profit” it **fails**.

| # | Assigned claim | Verdict | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **FAIL** as written | Assigned `CTraderFixSession` is `35=A` only (**PASS_SESSION**). Unscoped “no builder” is **false**: `Build("D")` ×5 in sibling session files; hosted hop calls `CTraderFixCopyOpen`. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive` in `BaselineScorer.cs` L211 is `=> false`. `FromBaseline` never returns `LIVE` (ceiling `SHADOW`). Unit test asserts SHADOW-not-LIVE. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | DI L41 binds env. Lab `.env` L73 is `true`. Hosted logon **reads** `_runtime.RealCopyEnabled` at L70 and **never assigns false**. Product `*.cs` has **one** writer: DI L41. Live GET blocked. |
| 4 | sending now cannot be the profit path | **FAIL** | Cannot prove. Hosted `ExecuteDemoCopyAsync` **sends now** on DemoDest (`Build("D")`); on-disk ledger has an open dest fill. Dest DTO `0` is a constructor, not a mark. Live GET of dest book blocked. Live `1369850` still refused. |
| 5 | SHADOW on demo is not destination profit | **FAIL** as absolute / **PASS_PAPER** | Paper path (`SimulateEntry` + Σ slippage) is **not** dest P&L. Residual that kills the absolute: a SHADOW demo source **can** be `AUTO_ADMIT` and then dest-sent by the hopper. |

**Overall slot verdict: FAIL** (instruction: FAIL any claim that cannot be proven from a file or live GET).

**Risk to capital:** **NONE on live Pepperstone `1369850`** (`CTraderFixSession` cannot send; `CTraderFixCopyOpen` refuses that account / non-demo host / non-demo sender). **Not absent on demo dest `5328266`** (hosted `ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.Build("D")`; ledger row still open). Flag may be **armed**; that is **not** a live-send license and is **not** “forced false after logon.”

Stale siblings this slot contradicts: `CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**”; README “Real NewOrderSingle is **off** (`…=false`)”; `appsettings.json` `FeatureFlags.LiveCopyEnabled=false` is **unread** by DI (DI binds `REAL_COPY_EXECUTION_ENABLED`, not that JSON key).

---

## 1. no `35=D` builder — FAIL as written (PASS_SESSION only)

### 1.1 Assigned session file (full read, 135/135)

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`

Public API is **only** `TryLogonAsync`. The sole outbound builder:

```89:110:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
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

| Fact | Measured this pass |
|---|---|
| Physical lines | **135 / 135** (ends L135 `}`) |
| Literal `35=D` / `(35, "D")` / `Build("D")` | **0** |
| `NewOrderSingle` identifier | **0** |
| Outbound tag 35 actually built | **`"A"` only** |
| `ssl.WriteAsync` | **1** (L49) — bytes of that Logon |
| `Extract(..., "35")` | **1** — **inbound** reply only (L55) |
| Socket kept for a later `35=D` | **No** — `using TcpClient` / `await using SslStream` dispose on every return |
| Generic `Assemble` callers in this file | **1** — `BuildLogon` |

A one-shot Logon probe is **not** a NewOrderSingle builder. **Session-scoped** “`CTraderFixSession` has no `35=D` builder” is proven. The assigned claim text is **unscoped**.

### 1.2 Why the unscoped claim FAILs

Grep `Build("D")` on product `*.cs` this pass = **5** call sites, **none** in the four assigned files:

| File | Lines |
|---|---|
| `src/Fix.CTrader/Sessions/CTraderFixCopyOpen.cs` | L95 `Build("D", …)` — **hosted** via `CopyTradingService.ExecuteDemoCopyAsync` L528 close / L566 open |
| `src/Fix.CTrader/Sessions/CTraderFixDemoTestTrade.cs` | L139 flatten, L163 open, L197 close |
| `src/Fix.CTrader/Sessions/CTraderFixDemoMatrix.cs` | L93 `SendD` → `Build("D", …)` |

Live identity gate on CopyOpen (`account == "1369850"` / host not `demo-` / sender not `demo.`) **refuses** live dest. Lab `.env` **is** DemoDest: host starts `demo-`, sender starts `demo.`, account `5328266` ≠ `1369850`.

`CopyTradingHostedService` L28–30 ticks every **20s**: `TickRosterAsync` → `GenerateShadowIntentsAsync` → `ExecuteDemoCopyAsync`. The last call is the hosted `35=D` hop.

`LiveCopyPage.tsx` has **0** FIX builders. It only renders `/api/copy/status` + `/api/copy/intents`. Empty-state L57 says demo dest auto-sends after ADMITTED — UI copy, not an encoder, and evidence the product **does** send.

Claim 1 **FAIL** (unscoped). Session-only remainder: **PASS_SESSION**.

---

## 2. `CanPromoteToLive` is false — PASS

`D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (full read, 212/212). The machine lives in the same file:

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

| Fact | Measured |
|---|---|
| `CanPromoteToLive` body | **literal `false`**, argument unused |
| `FromBaseline` returns `LIVE` or `LIVE_CANDIDATE` | **Never** — ceiling is `SHADOW` |
| `AfterHighEarlyScore` | `SHADOW` |
| Score persist | `DealIngestionService` L140 `CurrentState = score.SuggestedState` — cannot become `LIVE` via scoring |
| Product callers of `CanPromoteToLive` | **tests only** (`BaselineScorerTests` L26 expects `BeFalse()` after three disciplined winners → SHADOW) |
| Enum still has `LIVE=5` / `LIVE_CANDIDATE=4` | Yes (`TraderState.cs`) — unused by this machine |

`LiveCopyPage.tsx` shows `liveTraders` from API; it does **not** promote.

Claim 2 **PASS**.

---

## 3. `RealCopyEnabled` forced false after logon — FAIL

**None of the four assigned files force this flag false.**

| Assigned file | `RealCopyEnabled` |
|---|---|
| `CTraderFixSession.cs` | **0** mentions. Logon does not touch runtime. |
| `BaselineScorer.cs` | **0** |
| `RiskEngine.cs` | Consumes `request.RealExecutionEnabled` as an **input**. L147–150 `allowSend = RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. Does not write runtime. L90–93 empty shadow comment when `RealExecutionEnabled==false`. |
| `LiveCopyPage.tsx` | **Displays** `status?.realCopyArmed` (`YES`/`NO`, amber when true). No setter. |

### 3.1 Actual writers / readers (adjacent, required to judge “after logon”)

DI constructs the singleton **from env** and never re-pins:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

API host loads `.env` then overlays process env (`Program.cs` L10 + L13).

Hosted FIX logon **after** QUOTE/TRADE `TryLogonAsync`:

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

That is a **read** of the already-bound flag. There is **no** `_runtime.RealCopyEnabled = false` in this file.

Grep `RealCopyEnabled =` on product `*.cs` this pass = **1** hit: `DependencyInjection.cs` L41. Zero `RealCopyEnabled = false` writers.

Lab `.env` (boolean + public dest only; no secrets):

- L49 host prefix `demo-…`
- L50 account `5328266`
- L56 / L64 sender prefix `demo.pepperstone.5328266`
- L73 `REAL_COPY_EXECUTION_ENABLED=true`

Therefore at process start the flag is **true**, and logon **leaves it true**.

`/api/health` L55 and `/api/settings` L76 expose `runtime.RealCopyEnabled`. Live GET of those bodies was **SSRF-blocked** this slot — not used as proof. File wiring is enough to **disprove** “forced false after logon.”

`CTraderFixOptions.RealCopyExecutionEnabled` defaults `false` (L35) but that options type is **not** the runtime singleton the logon host reads.

`apps/api/appsettings.json` `FeatureFlags.LiveCopyEnabled=false` is **not** the key DI binds.

Claim 3 **FAIL**.

---

## 4. sending now cannot be the profit path — FAIL

Cannot prove. “Profit path” was not scoped to live `1369850`. Demo dest **is** a destination account; the hopper **sends now**. Dashboard `DestinationRealPnl=0` is a constructor, not a dest-ledger mark. Live GET of dest P&L was blocked — so dest `$0` is **not** live-proven.

### 4.1 Assigned files

**`CTraderFixSession`:** one-shot `35=A`, socket disposed. Cannot place an order.

**`BaselineScorer`:** quality uses source `NetPnl` / `ProfitFactor`. Suggested state ceiling is `SHADOW`. `CanPromoteToLive=false`. Source score is **not** dest P&L.

**`RiskEngine`:** `AllowFixSend` is true only if `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Rejects always `AllowFixSend=false` (L187). This is a gate, not a sender. With `RealExecutionEnabled==false` the unit test `Real_flag_false_never_allows_fix_send` expects Approve + `AllowFixSend=false`. If the flag is **true** and the other three gates pass, Evaluate **will** return `AllowFixSend=true`. That is not “cannot send.”

**`LiveCopyPage.tsx`:**

- L13: `REAL_COPY armed` = `status?.realCopyArmed` (amber when true).
- L16: `Live sends` = `status?.liveSends` (count of `ExecutionIntents` with `SentAt != null`; grep `ExecutionIntents.Add` / `new ExecutionIntent` = **0** writers).
- L23–28: “Live send blockers (Pepperstone cannot be filled)” — UI admits live dest cannot fill.
- L57 empty-state: “Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position.” That is **demo dest**, and it is an admission that sending **is** the dest hop.

### 4.2 Persist / roster hop (cannot send live)

`CopyTradingService`:

| Pin | Line | Effect |
|---|---|---|
| `VenueReconciled = false` | L20 | Evaluate `Reconciled=false` → increasing actions `VENUE_NOT_RECONCILED` |
| `NewOrderSingleImplemented => DemoDest` | L50 | **true** on lab `.env` |
| Status DTO `VenueReconciled: DemoDest` | L67 | honesty split vs const L20 |
| Persist `AllowFixSend = false` | L324 | hard false even if Evaluate approved |
| LIVE send branch | L330 | requires `AllowFixSend && LIVE && NOS && VenueReconciled` — **dead** (`CanPromoteToLive=false`, persist false, const recon false) |
| Else status | L336 | `SHADOW_ONLY` + optional `SimulateEntry` |

Policy (`XauUsdOneToOneCopyPolicy` L57–61): copies **next** XAUUSD events 1:1; “does not wait until a ticket is profitable — that is lookahead.” Sending an open now is **not** “take the winner.”

`AllocationFactor=1m` (1:1). If this hop ever sent live it would be dest-ruin sizing, not an edge. It does not send live.

Dashboard dest book:

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

`OverviewDto` field after `ShadowPnl` is `DestinationRealPnl` (`DashboardModels.cs` L16). That `0` is a **constructor literal**, not a sum of dest fills. Live GET of `/api/overview` was blocked; the constructor is file proof that the **dashboard** does not book dest profit. It is **not** proof that dest tickets do not exist.

### 4.3 Residual that kills the claim

Hosted 20 s tick (`CopyTradingHostedService` L28–30) calls `ExecuteDemoCopyAsync`. That method:

- returns 0 unless `DemoDest` (L485–488);
- **bypasses** `RiskEngine.Evaluate`;
- does **not** read `RealCopyEnabled`;
- calls `CTraderFixCopyOpen.SendAsync` (`Build("D")`) for ADMITTED opens ≤ `MaxAutoLots=0.05` and for ledger closes when source completed;
- refuses live `1369850`.

On-disk ledger (`D:\Prop\data\demo_copy_ledger.json`): one open demo fill, source `305750` / pos `21250421` / dest pos `237339770` / 0.01 lot / px `4390.2` / `DestClosed=false`. That is a **demo** dest ticket. It is dest P&L exposure on `5328266`, whether or not `DestinationRealPnl` books it.

What **is** proven (does not rescue the claim):

- Live dest `1369850` cannot be the send target (CopyOpen refuse + session `35=A` only).
- Persist/score hop cannot emit a live NewOrderSingle (`AllowFixSend=false`, `CanPromoteToLive=false`, `VenueReconciled` const false).
- Policy copies the **next** open, not a closed winner — send-now is not “take the winner.”

What **kills** the claim:

- `ExecuteDemoCopyAsync` is on the 20 s tick and **sends** `35=D` when `DemoDest` (lab `.env` matches).
- A filled dest ticket is a dest P&L path. The claim did not say “live 1369850 only.”

Claim 4 **FAIL**.

---

## 5. SHADOW on demo is not destination profit — FAIL as absolute / PASS_PAPER

### 5.1 Paper SHADOW is not dest cash (proven)

`ShadowCopyEngine.SimulateEntry` / `SimulateExit` compute a modeled fill from quote bid/ask + optional 0.05 latency slip. No socket. No tag 35.

`GenerateShadowIntentsAsync` L336–359: non-LIVE path writes `SHADOW_ONLY` + optional `ShadowOrder` from `SimulateEntry`.

`PersistDemoShadowAsync` (`EfTradingStore` L267–312): only when `state == SHADOW`; intents status `SHADOW_ONLY`; fills via the same simulator.

Dashboard `ShadowPnl` (`EfDashboardQueries` L29) = `Sum(ShadowOrders.SourceVsShadowSlippage)`. That number is **slippage of a paper fill**, not dest-account P&L.

`DestinationRealPnl` is constructor `0` (L44), not a dest-ledger sum.

`CopyGroupFilter.IsDemoOrContest` only admits `demo` / `contest` path segments. Real groups cannot be roster-admitted. That is a **source** filter, not a dest-P&L proof.

So: the **SHADOW artifact** (ShadowOrders / ShadowPnl) is not destination profit. **PASS_PAPER**.

### 5.2 Why the absolute FAILs

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` / blocked / size-pattern / `<20` XAU / non-positive XAU book / non-demo-contest. It does **not** reject `SHADOW`. `FromBaseline` ceiling is `SHADOW`, so every eligible trader the scorer can emit is `SHADOW`.

`CopyRosterEngine.Decide` L72–80: if eligible → `Admit` / `Keep` (`AUTO_ADMIT`).

`ExecuteDemoCopyAsync` L542–569: iterates `Status == "ADMITTED"` roster seats and sends `CTraderFixCopyOpen.SendAsync` for open XAUUSD ≤ 0.05 lots.

Therefore a **SHADOW** demo source with 20+ profitable completed XAU trades **can** be AUTO_ADMITTED and then dest-filled. SHADOW is the dest-hopper floor, not a wall against dest P&L.

The assigned claim is “SHADOW on demo is not destination profit.” As “paper SHADOW ≠ dest cash” it is proven. As “SHADOW-on-demo cannot be dest profit” it is **false**. Instruction: FAIL any claim that cannot be proven. The absolute is **not** proven.

Claim 5 **FAIL** as written (absolute). **PASS_PAPER** remainder.

---

## 6. Live GET

Attempted this slot:

- `GET http://127.0.0.1:5000/api/health`
- `GET http://127.0.0.1:5000/api/copy/status`
- `GET http://127.0.0.1:5000/api/settings`

All **SSRF-blocked** (`127.0.0.1` private). No live body. `launchSettings.json` still advertises API `:5000`. Runtime `realCopyEnabled` / `realCopyArmed` / dest book **not** remeasured from HTTP. File wiring only.

---

## 7. Risk to capital (measured)

| Surface | This-slot proof | Risk |
|---|---|---|
| Live Pepperstone `1369850` | `CTraderFixSession` cannot `35=D`. `CTraderFixCopyOpen` / `DemoTestTrade` / `DemoMatrix` refuse that account and non-demo host/sender. Persist hop dead (`CanPromoteToLive=false`, `AllowFixSend` persist false, `VenueReconciled` const false). | **NONE** (`SAFE_BY_ABSENCE`) |
| Demo dest `5328266` | Lab `.env` is DemoDest. Hosted 20s `ExecuteDemoCopyAsync` → `Build("D")`. Ledger: `305750` / dest `237339770` / 0.01 / `DestClosed=false`. | **Not absent.** Demo dest P&L path is live in product. |
| `REAL_COPY_EXECUTION_ENABLED` | Bound `true`; logon does not force false. Flag is **not** the send license for the demo hopper (hopper ignores it) and is **not** a live-`1369850` license. | Armed ≠ live send. Do not paper over claim 3. |
| Paper SHADOW | `SimulateEntry` only. | **None** (not dest cash). |

This slot sent **0** FIX messages.

---

## 8. What would change the verdict

| Claim | What would make it PASS |
|---|---|
| 1 | Delete or disconnect `CTraderFixCopyOpen` / `DemoTestTrade` / `DemoMatrix` `Build("D")`, **or** scope the claim to `CTraderFixSession` only. |
| 3 | After `TryLogonAsync`, assign `_runtime.RealCopyEnabled = false` (and keep DI default false). Prove with live GET `realCopyEnabled=false`. |
| 4 | Remove hosted `ExecuteDemoCopyAsync`, **or** scope the claim to live `1369850` and prove dest book from a live GET. |
| 5 | Stop AUTO_ADMIT of `SHADOW` into the dest hopper, **or** scope the claim to paper `ShadowPnl`. |

Claim 2 needs no change.

---

## 9. Slot pin

| Pin | Value |
|---|---|
| Slot | **50** |
| Verdict | **FAIL** |
| Evidence | Assigned four files + send/logon hop + ledger + flag-only `.env`. Live GET blocked. |
| Risk to capital | **NONE** on live `1369850`. Demo dest `5328266` hopper **not** absent. |
