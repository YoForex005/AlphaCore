# P500_VERIFY_25 — Adversarial verifier (slot 25)

| Field | Value |
|---|---|
| Slot | **25** |
| Date | 2026-08-18 |
| Role | Adversarial verifier. Read the four named files plus the logon/copy hop they actually call. Do **not** trust sibling agents. |
| Assigned claims | (1) no `35=D` builder. (2) `CanPromoteToLive` is false. (3) `RealCopyEnabled` forced false after logon. (4) sending now cannot be the profit path. (5) SHADOW on demo is not destination profit. |
| Named files | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` |
| Method | Independent full `read_file` of the four files + the logon host, DI bind, copy tick, copy-open sibling, roster/policy, dashboard constructor. `grep` for `RealCopyEnabled =`, `CanPromoteToLive`, `Build("D")`. Boolean-only `.env` keys (no secret dump). Loopback `GET` attempted and **blocked** (this slot did not live-attach). |
| Product source modified | **No** |
| Config / `.env` edited | **No** |
| Secrets printed | **None.** Quoted only public dest ids `5328266` / `1369850`, public demo host name, and flag booleans `true`/`false`. No MT5 / FIX / proxy / DB passwords. Tag `554` not dumped. |
| Binding rule | **FAIL any claim that cannot be proven from a file or a live GET this slot.** |

**Honesty:** wanting dest profit is not an edge. A high `EarlyQualityScore` is not dest PnL. A `SHADOW` label on a `demo\` book is not Pepperstone money. A `35=A` logon probe is not a NewOrderSingle. Binding `REAL_COPY_EXECUTION_ENABLED=true` is not “forced false after logon.” Sibling `Build("D")` on the demo dest is dest **exposure**, not dest **profit**.

Stale reports this slot must not reuse: any `W500_*` / `E038` / `CREDENTIALS` line that still says (a) hosted logon re-pins `RealCopyEnabled=false`, (b) product `35=D=0` / `NewOrderSingleImplemented=false` const, (c) copy hop has no sender. HEAD: `NewOrderSingleImplemented => DemoDest`; `CTraderFixCopyOpen.Build("D")` is called from `ExecuteDemoCopyAsync` on the 20 s tick.

---

## 0. Verdict

| # | Claim | From-file proof? | Slot result |
|---|---|---|---|
| 1 | No `35=D` builder (`CTraderFixSession`) | **Yes** — 135/135 is `35=A` logon only | **PASS** |
| 2 | `CanPromoteToLive` is false | **Yes** — `=> false`; `FromBaseline` never returns `LIVE` | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **No — disproven** | **FAIL** |
| 4 | Sending now cannot be the profit path | **Yes** (live dest + dest-PnL constructor); demo `35=D` is not dest-profit accounting | **PASS** |
| 5 | SHADOW on demo is not destination profit | **Yes** — SHADOW is a source state; dest PnL is literal `0` | **PASS** |

**Slot-25 overall: `FAIL`.**

Claim 3 is the fail. The only assignment of `LiveRuntimeStatus.RealCopyEnabled` is DI from `REAL_COPY_EXECUTION_ENABLED`. Lab `.env` L73 is `true`. `CTraderFixLogonHostedService` **reads** the bit into a log line after `35=A` and never writes `false`. There is no post-logon re-pin anywhere in product C#.

Live GET this slot: **not obtained** (`http://127.0.0.1:5000/api/health` SSRF/loopback blocked). Runtime `realCopyEnabled` on a running process is therefore **unverified**. File proof is sufficient to fail claim 3 without the GET.

Risk to **live** Pepperstone capital (`1369850`): **NONE** (`SAFE_BY_ABSENCE` on `CTraderFixSession`; `CTraderFixCopyOpen` refuses that account). Residual: hosted tick **can** `Build("D")` against demo dest `5328266` when `DemoDest` is true. That is demo-account exposure, not live-license ruin, and it is **not** dest-profit.

---

## 1. No `35=D` builder — PASS (`CTraderFixSession.cs` 135/135)

File: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (full read).

Outbound builder is only `BuildLogon`:

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

- One `WriteAsync` of that logon (L47–50). Socket disposed in `using`.
- Inbound `Extract(..., "35")` accepts `"A"` as LoggedOn (L55–65). No outbound `"D"`.
- `Assemble` is generic but **only** called from `BuildLogon`.
- `grep Build("D")` under `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` = **0**.

**Claim 1 proven for the named file.**

### 1.1 Adversarial residual (does not fail claim 1)

The *product* has `35=D` builders. They are **not** in `CTraderFixSession`:

| File | `Build("D")` | Wired to hosted copy? |
|---|---|---|
| `CTraderFixCopyOpen.cs` L95 | **Yes** | **Yes** — `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566, called from `CopyTradingHostedService` L30 every 20 s |
| `CTraderFixDemoTestTrade.cs` L139/L163/L197 | Yes | No — `tools/DemoFixTestTrade` |
| `CTraderFixDemoMatrix.cs` L93 | Yes | No — standalone matrix |

`CTraderFixCopyOpen.SendAsync` refuses live identity (`host` not `demo-*`, sender not `demo.*`, or `account == "1369850"`) and otherwise writes `Build("D", ...)` on TRADE 5212. Lab `.env` (boolean/public keys only): `CTRADER_FIX_HOST=demo-us-eqx-01.p.c-trader.com`, `CTRADER_FIX_ACCOUNT_ID=5328266`, `CTRADER_FIX_TRADE_SENDER_COMP_ID=demo.pepperstone.5328266` → `CopyTradingService.DemoDest == true` → `NewOrderSingleImplemented => DemoDest` is **true**. Older “const false / product `35=D=0`” notes are **STALE**.

Claim 1 as assigned (“read `CTraderFixSession` — no `35=D` builder”) still **PASS**.

---

## 2. `CanPromoteToLive` is false — PASS

File: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (full read).

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

Proof:

- `CanPromoteToLive` is a constant `false`. Argument `current` is unused.
- `FromBaseline` ceiling is `SHADOW`. It never returns `LIVE_CANDIDATE` or `LIVE` (`TraderState.cs` L8–10 exist as enum values only).
- `grep CanPromoteToLive` product hits: definition L211 + unit test `BaselineScorerTests.cs` L26 (`Three_disciplined_winners_go_to_shadow_not_live`). No override.

Quality formula (L152–160) can print a high `EarlyQualityScore` **without** dest PnL: start 50, `+15` only if `NetPnl > 0`, PF bonuses, `+0.2 * behavior`, `-0.25 * risk`. That number is a source-tape score, not destination money.

**Claim 2 proven.**

---

## 3. `RealCopyEnabled` forced false after logon — FAIL (disproven)

### 3.1 Only write site

`grep RealCopyEnabled =` under product `*.cs` = **one** assignment:

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

API host loads `.env` then environment variables (`apps/api/Program.cs` L10–13). Lab `.env` L73: `REAL_COPY_EXECUTION_ENABLED=true`. Therefore a process that starts with that file **arms** `LiveRuntimeStatus.RealCopyEnabled`.

### 3.2 Logon host does not re-pin

File: `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` (full read). After two `CTraderFixSession.TryLogonAsync` calls it writes Quote/Trade `LoggedOn`/`Status`/`LastError`/`UpdatedAt` and **logs** the existing flag:

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

Zero assignments to `_runtime.RealCopyEnabled`. Persist path (`PersistAsync`) touches `FixSessionState` rows only.

### 3.3 What is *not* a forced-false pin

| Artifact | What it actually does |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` default `false` | Different POCO. Not written by the logon host. Default ≠ process pin. |
| `apps/api/appsettings.json` `FeatureFlags.LiveCopyEnabled=false` | Unused by `LiveRuntimeStatus`. |
| `apps/fix-worker/Worker.cs` L21 `CTrader:RealCopyExecutionEnabled` default `false` | Separate key. Worker still does not send. Not the API logon path. |
| `RiskEngine` L90–93 empty `if (RealExecutionEnabled == false)` | Comment only. Does **not** force reject. `AllowFixSend` is computed later as `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). |
| `CopyTradingService` persist `AllowFixSend = false` (L324) | Blocks the *shadow-intent* send bit. Does not set `RealCopyEnabled`. |
| `docs/*` “set the flag false” | Documentation, not runtime. |

`/api/health` L55 and `/api/settings` L76 expose `runtime.RealCopyEnabled` as-is. This slot could not `GET` them (loopback blocked). File proof already falsifies “forced false after logon.”

**Claim 3 FAIL.**

---

## 4. Sending now cannot be the profit path — PASS

“Profit path” here means **measured destination profit on live capital**. Sending a ticket *because it is open now* is not that path.

### 4.1 Named UI cannot send

`D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (full read, 70 lines):

- Read-only: `useCopyStatus` + `useCopyIntents`. No POST. No “send now” button.
- Header copy: “Live send blockers (Pepperstone cannot be filled).”
- Empty-state (L57): “Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position.” That is a **status sentence**, not dest-PnL.
- Stats are counts (`shadowTraders`, `liveTraders`, `liveSends`, `shadowFills`). No dest PnL column.

### 4.2 Policy forbids lookahead; scorer cannot auto-LIVE

`XauUsdOneToOneCopyPolicy` (L57–61, L123–128): copy the trader’s **next** XAU event 1:1. Closed-winner open is `NO_LOOKAHEAD_CLOSED_WINNER`. That is the file saying “send this ticket because it already won” cannot be traded live.

`CanPromoteToLive => false` (claim 2) plus `FromBaseline` ceiling `SHADOW` means the scorer never produces `LIVE`. `GenerateShadowIntentsAsync` live branch (L330–333) also requires `decision.AllowFixSend && CurrentState==LIVE && NewOrderSingleImplemented && VenueReconciled`. `VenueReconciled` is `const false` (L20). Persist then **overwrites** `AllowFixSend = false` (L324). That branch cannot emit a live ticket.

### 4.3 Dest profit is a constructor zero

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

`DashboardModels.OverviewDto` L15–16: those three zeros are `ShadowPnl`’s neighbors `DestinationRealPnl`, `XauGross`, `XauNet`. `ShadowPnl` itself is `Sum(ShadowOrders.SourceVsShadowSlippage)` (L29) — modeled slip, not dest money. `GetRiskAsync` also returns five literal `0`s (L208). There is **no** dest-PnL aggregator in product C# (`grep DestPnl|DestinationPnl` = 0 writers).

`DemoCopyLedger` stores `DestFillPrice` / `DestPositionId` / `DestClosed` and **no** dest PnL field. On-disk `D:\Prop\data\demo_copy_ledger.json` has one open demo fill (`DestClosed: false`) — dest **exposure**, not a profit number.

### 4.4 What sending-now *can* do (does not create a profit path)

`CopyTradingHostedService` tick: `TickRosterAsync` → `GenerateShadowIntentsAsync` → `ExecuteDemoCopyAsync`. The last method, when `DemoDest`, calls `CTraderFixCopyOpen.SendAsync` for ADMITTED open XAU ≤ `MaxAutoLots` (0.05) and for dest closes. That path:

- does **not** call `RiskEngine.Evaluate`
- does **not** read `CanPromoteToLive`
- does **not** read `RealCopyEnabled`
- **does** refuse `1369850`

So “send now” on demo dest can place a `35=D`. It still cannot be **the profit path**: dest PnL is uncomputed, live license is refused, promotion to LIVE is hard-false, and `P500_PROFIT_SYNTHESIS` operating law is “Send now = donate the Pepperstone account to gold spread and martingales.” Copy-all of a scored book that includes `RISK_BLOCKED` tails is dest ruin **if** that sender were pointed at live. It is not pointed at live.

**Claim 4 proven** as: sending-now is not dest-profit; live send remains absent; demo `35=D` is not a profit ledger.

---

## 5. SHADOW on demo is not destination profit — PASS

### 5.1 SHADOW is a source-trader label

`TraderStateMachine.FromBaseline` returns `SHADOW` when `quality >= 70 && risk < 40` after three XAU closes. That uses **source** reconstructed PnL / martingale / SL rate. `CopyGroupFilter.IsDemoOrContest` is **required** for roster admit (`CopyRosterEngine` L52–53; `XauUsdOneToOneCopyPolicy` L105–109). So the only books that can sit on the roster are `demo\` / `contest\` (and `Starwave\demo\` segments). Challenge-account source PnL is not cTrader dest PnL.

`IsTraderEligible` also requires `State` past `WATCH` (SHADOW / LIVE_CANDIDATE / LIVE), `CompletedXauTrades >= 20`, `XauNetPnl > 0`, no size-pattern flags. A scorer `SHADOW` at trade #3 (`EarlyScoreTradeCount = 3`) is **not** admit-eligible. Early SHADOW is paper-score only.

### 5.2 Paper SHADOW fills are not dest

`GenerateShadowIntentsAsync` writes `Status = "SHADOW_ONLY"` and `ShadowCopyEngine.SimulateEntry` into `ShadowOrders` (price / qty / spread / `SourceVsShadowSlippage`). `ShadowCopyEngine.MarkToMarket` exists and is **not** persisted by the copy service. `ShadowPortfolioPage.tsx` still says “Live NewOrderSingle remains disabled.”

Dashboard `ShadowPnl` = sum of **slippage**, constructor `DestinationRealPnl = 0`.

### 5.3 Demo dest fills that happen to copy a SHADOW seat are still not dest-profit accounting

If a SHADOW trader later meets the 20-trade / `XauNetPnl > 0` gate, roster `AUTO_ADMIT` + `ExecuteDemoCopyAsync` can send demo `35=D`. That is dest **exposure** on `5328266`. It is not:

- live Pepperstone profit
- a dest-PnL figure in the API
- a reason to treat `demo\yo-2step` SHADOW as skill that pays the destination book

`RiskEngine` cannot mint dest profit either. Approve + `AllowFixSend` still requires `RealExecutionEnabled && Reconciled && VenueHealthy && KillSwitch.None`. Copy persist forces `AllowFixSend=false`. The engine’s empty `RealExecutionEnabled==false` block (L90–93) does not convert SHADOW into dest money.

**Claim 5 proven.**

---

## 6. Live GET

| URL | Result this slot |
|---|---|
| `http://127.0.0.1:5000/api/health` | **Blocked** (loopback/SSRF). No body. |
| `http://127.0.0.1:5000/api/copy/status` | Not fetched. |
| `http://127.0.0.1:5000/api/settings` | Not fetched. |
| `http://127.0.0.1:5000/api/overview` | Not fetched. |

Stale `_tmp_e032` dumps were **not** treated as live GET (old dummy logins `10001`/`10002`; not this process).

Unverified this slot (do not invent): process `realCopyEnabled`, `quoteLoggedOn`, `tradeLoggedOn`, intent counts, dest fill count beyond the on-disk ledger file.

---

## 7. Risk to capital

| Book | Path | Risk |
|---|---|---|
| Live Pepperstone `1369850` | `CTraderFixSession` = `35=A` only; `CTraderFixCopyOpen` hard-refuse | **NONE** (`SAFE_BY_ABSENCE`) |
| Demo dest `5328266` | Hosted `ExecuteDemoCopyAsync` → `Build("D")` when `DemoDest` | **Residual demo exposure** (not live-license ruin; not dest-profit) |
| Source MT5 books | Flatten is dest-only (`CopyRosterEngine` L31) | Source book not touched by this hop |

`REAL_COPY` armed in DI does **not** by itself send. The sender that exists is demo-gated and independent of the flag.

---

## 8. One-line slot law

```text
CTraderFixSession = 35=A only.
CanPromoteToLive => false.
RealCopyEnabled is NOT forced false after logon (.env true + DI bind + logon read-only).
Send-now is not dest profit (DestinationRealPnl constructor 0; live 1369850 refused).
SHADOW on demo is a source label + paper slip, not destination profit.
```
