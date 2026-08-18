# P500_VERIFY_59 — Adversarial profit-path verify (slot 59)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_59.md` |
| Agent / slot | P500 adversarial **verify 59** |
| Date | 2026-08-18 |
| Role | Independent verifier. **Did not trust** sibling P500_BOOK / P500_VERIFY / CREDENTIALS / README prose. Re-read the four assigned files + adjacent send/logon hop this pass. |
| Assigned files | `CTraderFixSession.cs` (135/135), `BaselineScorer.cs` (212/212), `RiskEngine.cs` (189/189), `LiveCopyPage.tsx` (70/70) |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Boolean keys + already-public host prefix / account ids only. |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live GET this pass | **Blocked.** `web_fetch` `http://127.0.0.1:5000/api/health` → SSRF private-IP deny. **No** live GET evidence. Any claim that needs a live body is **FAIL**. |
| Live attach / send this pass | **No.** No Manager Connect. No TLS. No Logon. No `35=D`. |
| Method | Full `read_file` of the four assigned files. Adjacent this pass: `CTraderFixLogonHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs` (gate + `Build("D")`), `CTraderFixDemoMatrix.cs` (`SendD`), `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `apps/api/Program.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `CopyGroupFilter.cs`, `DemoCopyLedger.cs`, `data/demo_copy_ledger.json` (public dest ids only), `DealIngestionService.cs`, `BaselineScorerTests.cs`, `CopyRosterEngineTests.cs`, `RiskEngineTests.cs`, `hooks.ts`, `launchSettings.json` (API `:5000`). Grep: `Build("D")` / `RealCopyEnabled\s*=` / `CanPromoteToLive` / `CurrentState\s*=` / `DestinationRealPnl`. Flag-only `.env` L49/L50/L56/L64/L73/L106. |

**Honesty rule:** FAIL any claim that cannot be proven from a file this slot or a live GET. Prior swarm prose is not evidence. A TLS Logon `35=A` is not a NewOrderSingle. `SAFE_BY_ABSENCE` on live `1369850` is not “flag stays false.” A demo hopper that can `Build("D")` is not `CTraderFixSession`. Destination constructor `$0` is not a measured dest book. Wanting profit is not an edge. Do not print secrets.

---

## 0. Verdict (binding)

**FAIL.** Claim 2 is file-proven. Claims **1, 3, 4, 5** fail the bar as written (unscoped / disproven / not proven).

| # | Assigned claim | Verdict | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **FAIL** as written | Assigned `CTraderFixSession` is `35=A` only (**PASS_SESSION**). Unscoped “no builder” is **false**: `Build("D")` ×5 in sibling session files; hosted hop calls `CTraderFixCopyOpen`. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive` in `BaselineScorer.cs` L211 is `=> false`. `FromBaseline` never returns `LIVE` (max `SHADOW`). Unit test asserts SHADOW-not-LIVE. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | DI L41 binds env. Lab `.env` L73 is `true`. Hosted logon **reads** `_runtime.RealCopyEnabled` at L70 and **never assigns false**. Zero `RealCopyEnabled = false` writers in product `*.cs`. Live GET blocked. |
| 4 | sending now cannot be the profit path | **FAIL** | Cannot prove. Hosted `ExecuteDemoCopyAsync` **sends now** on DemoDest (`Build("D")`); ledger has an open dest fill. Dest DTO constructor `0` is **not** dest-account P&L. Live GET of dest book blocked. Live `1369850` still refused. |
| 5 | SHADOW on demo is not destination profit | **FAIL** as written | Paper `SimulateEntry` is not dest cash (**PASS_PAPER**). Unscoped claim is **not proven**: `SHADOW` is the dest AUTO_ADMIT floor; hopper sends dest `35=D` for ADMITTED without checking LIVE. |

**Overall slot verdict: FAIL** (instruction: FAIL any claim that cannot be proven from a file or live GET).

**Risk to capital:** **NONE on live Pepperstone `1369850`** (`SAFE_BY_ABSENCE` on `CTraderFixSession` + CopyOpen refuse). **Not absent on demo dest `5328266`** (hosted `ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.Build("D")`). Flag may be **armed**; that is **not** a live-send license. Do not paper over claim 3.

Stale siblings this slot contradicts: `CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**”; README “Real NewOrderSingle is **off** (`…=false`)”; any BOOK that still pins product `35=D=0` / `NOS=const false` / persist L306 / logon re-pin false.

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

Grep `Build("D")` on `*.cs` this pass = **5** call sites, **none** in the four assigned files:

| File | Lines |
|---|---|
| `src/Fix.CTrader/Sessions/CTraderFixCopyOpen.cs` | L95 `Build("D", …)` — **hosted** via `CopyTradingService.ExecuteDemoCopyAsync` L528 close / L566 open |
| `src/Fix.CTrader/Sessions/CTraderFixDemoTestTrade.cs` | L139 flatten, L163 open, L197 close |
| `src/Fix.CTrader/Sessions/CTraderFixDemoMatrix.cs` | L93 `SendD` → `Build("D", …)` |

Live identity gate on CopyOpen (`account == "1369850"` / host not `demo-` / sender not `demo.`) **refuses** live dest:

```37:42:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
        if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
            || !sender.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
            || account == "1369850")
        {
            return Fail("Refused: dest must be demo FIX, not live 1369850.", host, account, sourceLogin);
        }
```

Lab `.env` **is** DemoDest: host `demo-us-eqx-01.p.c-trader.com` (L49), sender `demo.pepperstone.5328266` (L56/L64), account `5328266` (L50) ≠ `1369850`.

The claim as written is **not** “`CTraderFixSession` has no `35=D` builder.” It is “no `35=D` builder.” That is **false** on this tree. Hosted `CopyTradingHostedService` L30 calls `ExecuteDemoCopyAsync`, which calls `CTraderFixCopyOpen.SendAsync` (L528 / L566). Lab `.env` **is** DemoDest.

`LiveCopyPage.tsx` has **0** FIX builders. It only renders `/api/copy/status` + `/api/copy/intents`. Empty-state L57 honestly says demo dest auto-sends after ADMITTED — UI copy, not an encoder, and evidence the product **does** send.

`RiskEngine.cs` has **0** FIX builders. It only computes `AllowFixSend`.

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
| Product callers of `CanPromoteToLive` | **tests only** (`BaselineScorerTests` L26 expects `BeFalse()` after three disciplined winners → SHADOW) |
| Persist writer | `DealIngestionService` L140 `CurrentState = score.SuggestedState` — cannot become LIVE via this machine |
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

That is a **read** of the already-bound flag. There is **no** `_runtime.RealCopyEnabled = false` in this file or anywhere in product `*.cs`.

Grep `RealCopyEnabled\s*=` on `*.cs` this pass = **1** hit: DI L41. Product `RealCopyEnabled` mentions = 15 (reads / DTO fields / copy note). Zero writers of `false`.

Lab `.env` (boolean + public dest only):

- L49 `CTRADER_FIX_HOST=demo-us-eqx-01.p.c-trader.com`
- L50 `CTRADER_FIX_ACCOUNT_ID=5328266`
- L73 `REAL_COPY_EXECUTION_ENABLED=true`
- L106 `FEATURE_COPY_TRADING_ENABLED=true`

Therefore at process start the flag is **true**, and logon **leaves it true**.

`/api/health` L55 and `/api/settings` L76 expose `runtime.RealCopyEnabled`. Live GET of those bodies was **SSRF-blocked** this slot — not used as proof. File wiring is enough to **disprove** “forced false after logon.”

`CREDENTIALS_AND_COPY_STATUS.md` “false (forced)” and README “off (`…=false`)” are **stale** vs this tree.

Claim 3 **FAIL**.

---

## 4. sending now cannot be the profit path — FAIL

Cannot prove. “Profit path” was not scoped to live `1369850`. Demo dest **is** a destination account; the hopper **sends now**. Dashboard `DestinationRealPnl=0` is a constructor, not a dest-ledger mark. Live GET of dest P&L was blocked — so dest `$0` is **not** live-proven.

### 4.1 Assigned files

**`CTraderFixSession`:** one-shot `35=A`, socket disposed. Cannot place an order.

**`BaselineScorer`:** quality uses source `NetPnl` / `ProfitFactor`. Suggested state ceiling is `SHADOW`. `CanPromoteToLive=false`. Source score is **not** dest P&L.

**`RiskEngine`:** `AllowFixSend` is true only if `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Rejects always `AllowFixSend=false` (L187). This is a gate, not a sender. Test `Real_flag_false_never_allows_fix_send` (`RiskEngineTests` L21–26) only covers `RealExecutionEnabled=false`.

**`LiveCopyPage.tsx`:**
- L23–28: “Live send blockers (Pepperstone cannot be filled)” — UI admits live dest cannot fill.
- L13 `REAL_COPY armed` = `status?.realCopyArmed`.
- L16 `Live sends` = `status?.liveSends` (count of `ExecutionIntents` with `SentAt != null`; product has **0** `ExecutionIntents.Add` writers).
- L57 empty-state: “Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position.” That is **demo dest**, not live dest profit.

### 4.2 Persist / roster hop (cannot send live)

`CopyTradingService`:

| Pin | Line | Effect |
|---|---|---|
| `VenueReconciled = false` | L20 | Evaluate `Reconciled=false` → increasing actions `VENUE_NOT_RECONCILED` |
| `NewOrderSingleImplemented => DemoDest` | L50 | **true** on lab `.env` (BOOK `const false` **STALE**) |
| Status DTO `VenueReconciled: DemoDest` | L67 | honesty split vs const L20 |
| Persist `AllowFixSend = false` | L324 | hard false even if Evaluate approved |
| LIVE send branch | L330 | requires `AllowFixSend && LIVE && NOS && VenueReconciled` — **dead** (`CanPromoteToLive=false`, persist false, const recon false) |
| Else status | L336 | `SHADOW_ONLY` + optional `SimulateEntry` |

`DemoDest` (L45–48) is true when host starts `demo-` **and** trade sender starts `demo.` **and** account ≠ `1369850`. Lab `.env` matches.

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

`OverviewDto.DestinationRealPnl` is the field after `ShadowPnl` (`DashboardModels.cs` L16). The `0` is a **constructor literal**, not a sum of dest fills. Live GET of `/api/overview` was blocked; the constructor is file proof that the product does not book dest profit.

### 4.3 Residual that kills the claim

Hosted 20 s tick (`CopyTradingHostedService` L28–30) calls `ExecuteDemoCopyAsync`. That method:

- returns 0 unless `DemoDest` (L485–488);
- **bypasses** `RiskEngine.Evaluate`;
- calls `CTraderFixCopyOpen.SendAsync` (`Build("D")`) for ADMITTED opens ≤ `MaxAutoLots=0.05` and for ledger closes when source completed;
- refuses live `1369850`.

On-disk ledger (`D:\Prop\data\demo_copy_ledger.json`): one open demo fill, source `305750` / pos `21250421` / dest pos `237339770` / 0.01 lot / px `4390.2` / `DestClosed=false`. That is a **demo** dest ticket. It is **not** live dest P&L, **not** in `DestinationRealPnl`, **not** a scored-edge live send.

`ExecuteDemoCopyAsync` L500–512 also **hard-inserts** that same 305750/21250421 row if missing. Sending is not hypothetical.

What **is** proven (does not rescue the claim):

- Live dest `1369850` cannot be the send target (CopyOpen refuse + session `35=A` only).
- Persist/score hop cannot emit a live NewOrderSingle (`AllowFixSend=false`, `CanPromoteToLive=false`, `VenueReconciled` const false).
- Policy copies the **next** open, not a closed winner — send-now is not “take the winner.”

What **kills** the claim:

- `ExecuteDemoCopyAsync` is on the 20 s tick and **sends** `35=D` when `DemoDest` (lab `.env` matches).
- On-disk ledger already holds dest pos `237339770` @ 0.01 / `4390.2`, `DestClosed=false`. That is a dest fill. This slot cannot mark its P&L (no live GET; DTO field is hardcoded `0`).
- Therefore “sending now cannot be the profit path” is **not proven**. Demo dest P&L from those fills is a real dest book the dashboard refuses to show.

Claim 4 **FAIL**.

---

## 5. SHADOW on demo is not destination profit — FAIL as written (PASS_PAPER only)

### 5.1 What SHADOW is

`TraderState.SHADOW = 3` (`TraderState.cs`). `FromBaseline` assigns it when `quality >= 70 && risk < 40` after ≥3 completed XAU trades. That is a **source-trader** label on Achiever/Starwave books.

HEAD copy policy **requires** demo/contest groups (`CopyGroupFilter.IsDemoOrContest`; `NOT_DEMO_OR_CONTEST_GROUP` otherwise). So SHADOW sources that can be copied on this HEAD are demo/contest **sources**. That still does not make SHADOW = dest profit — and it also does **not** prove SHADOW cannot produce dest fills.

### 5.2 Shadow engine is paper (PASS_PAPER)

`ShadowCopyEngine.SimulateEntry` / `SimulateExit` / `MarkToMarket` compute a modeled fill from a `DestinationQuote`. No socket. No tag 35.

Persist hop (`GenerateShadowIntentsAsync`): eligible states include `SHADOW` (L202). After Evaluate it writes `Status = "SHADOW_ONLY"` and, only if quote present **and** `Outcome==Approve`, a `ShadowOrder` from `SimulateEntry`. Persist `AllowFixSend=false`.

Overview `ShadowPnl` (`EfDashboardQueries` L29) = `Sum(ShadowOrders.SourceVsShadowSlippage)` — a **slippage** aggregate, not dest realized P&L. Trader-row `ShadowPnl` is hardcoded **0** (L118).

`LiveCopyPage` shows `SHADOW traders` and `Shadow fills` as **counts**. No dest P&L column.

Paper SHADOW is **not** destination cash. That remainder is proven.

### 5.3 Why the unscoped claim FAILs

`XauUsdOneToOneCopyPolicy.IsTraderEligible` **blocks** `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET` (L81–85). `SHADOW` is the **minimum eligible** source state (along with `LIVE_CANDIDATE` / `LIVE`, which this scorer never emits).

`CopyRosterEngine.Decide` AUTO_ADMITs when `_policy.IsTraderEligible` (L72–80). Unit test `New_eligible_trader_is_auto_admitted` (`CopyRosterEngineTests` L53–62) uses `State = SHADOW` + `demo\yo-2step` and expects `RosterAction.Admit`.

`ExecuteDemoCopyAsync` selects `CopyIntents` with `Status=="ADMITTED"` and roster key `roster:…` (L542–544). It does **not** require `TraderState.LIVE`. An ADMITTED SHADOW source with an open XAUUSD ≤ `MaxAutoLots` is sent via `CTraderFixCopyOpen.Build("D")` and marked `DEMO_SENT` (L593).

So “SHADOW on demo is not destination profit” is **not proven**. SHADOW is the dest-send floor. Paper remainder: **PASS_PAPER**. Hopper remainder: dest P&L **possible** for ADMITTED SHADOW on demo dest `5328266`.

Claim 5 **FAIL** as written.

---

## 6. Live GET matrix (required; all blocked)

API listens on `:5000` (`apps/api/Properties/launchSettings.json` profile `http`).

| URL | Result | Usable as proof? |
|---|---|---|
| `GET http://127.0.0.1:5000/api/health` | SSRF blocked | **No** |
| `GET http://127.0.0.1:5000/api/copy/status` | not fetched (same SSRF) | **No** |
| `GET http://127.0.0.1:5000/api/overview` | not fetched (same SSRF) | **No** |
| `GET http://127.0.0.1:5000/api/settings` | not fetched (same SSRF) | **No** |
| `GET http://127.0.0.1:5000/api/risk` | not fetched (same SSRF) | **No** |

No live `realCopyEnabled` body this slot. Claim 3 does **not** need it (file-disproven). Dest `$0` is constructor-proven, not live-proven.

---

## 7. Risk to capital

| Surface | Risk |
|---|---|
| Live Pepperstone dest `1369850` | **NONE** — `CTraderFixSession` cannot build `35=D`; CopyOpen refuses this account/host/sender; persist `AllowFixSend=false`; `CanPromoteToLive=false`; 0 `ExecutionIntent` writers |
| Demo dest `5328266` | **Not absent** — hosted hopper can `Build("D")` on DemoDest; ledger already holds one 0.01 open. Demo money, not live dest. Bypasses `Evaluate`. |
| `REAL_COPY` flag | **Armed** (`true` via DI). Not a live-send license. **Claim 3 FAIL.** |
| SHADOW sources | Can AUTO_ADMIT onto the hopper. Paper `SimulateEntry` is not dest cash; dest send is a different hop. |

---

## 8. What this slot did not do

- Did not edit product, tests, or `.env`.
- Did not send `35=D`.
- Did not flip `REAL_COPY`.
- Did not print secrets.
- Did not treat sibling BOOK/VERIFY/CREDENTIALS prose as evidence.
- Did not claim “EX5 decompiled” or any ≥95% figure (out of scope).
