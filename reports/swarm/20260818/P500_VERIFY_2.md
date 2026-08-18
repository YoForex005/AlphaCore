# P500_VERIFY_2 — Adversarial slot 2 (CTraderFixSession / BaselineScorer / RiskEngine / LiveCopyPage)

| Field | Value |
|---|---|
| Slot | **2** |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_2.md` |
| Date | 2026-08-18 |
| Role | Adversarial verifier. Fail any claim not proven from a file this slot or a live GET this slot. |
| Product source modified | **No.** |
| Test source modified | **No.** |
| Secrets printed | **None.** Quoted only public dest ids `5328266` / `1369850` and boolean `REAL_COPY_EXECUTION_ENABLED=true`. |
| SHA-256 of SUTs | **Not measured this slot** (no shell / no `Get-FileHash`). Do not copy stale hashes from E002/D12. |
| Live GET this slot | **Attempted, blocked.** `GET http://127.0.0.1:5000/api/copy/status`, `/api/health`, `/api/overview` → SSRF block. Runtime flags, dest MTM, and `realCopyArmed` are **unproven**. |

**Assigned claims to confirm**

1. No `35=D` builder
2. `CanPromoteToLive` is false
3. `RealCopyEnabled` forced false after logon
4. Sending now cannot be the profit path
5. SHADOW on demo is not destination profit

**Package verdict: FAIL.** Claim 2 is the only claim that holds as written. Claim 1 holds only if narrowed to `CTraderFixSession.cs`. Claims 3–5 are disproven or unprovable. Stale same-day reports that say “no NewOrderSingle sender / SAFE_BY_ABSENCE” (`E002_no_live_send.md`, `LiveRuntimeStatus` copyNote, logon log line) are **lies against the current tree**.

---

## 0. Files read this slot (primary SUTs)

| File | Lines read | What it actually does |
|---|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 1–135 (entire) | TLS connect + **one** outbound builder: `BuildLogon` tag `35=A`. No `35=D`. |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | 1–212 (entire) | Features + scores. `TraderStateMachine.CanPromoteToLive(_) => false`. `FromBaseline` never returns `LIVE`. |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | 1–189 (entire) | Rejects + `AllowFixSend = RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. Rejects force `AllowFixSend=false`. Does **not** send FIX. |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | 1–70 (entire) | Dashboard: `useCopyStatus` / `useCopyIntents`. No send button. Empty-state text admits **demo dest auto-send** after `ADMITTED`. |

**Adjacent files read because claim 1/3/4/5 cannot be decided from the four SUTs alone**

- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` — **is** a `35=D` builder + socket writer.
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` — `Build("D", …)` ×3.
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` — `SendD` → `Build("D", …)`.
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` — logon only; **does not write** `RealCopyEnabled`.
- `D:\Prop\src\Infrastructure\DependencyInjection.cs` L39–42 — **only assignment** of `RealCopyEnabled`.
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` — shadow persist + **`ExecuteDemoCopyAsync` send hop**.
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` — 20s tick: roster → shadow → **`ExecuteDemoCopyAsync`**.
- `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs` + `XauUsdOneToOneCopyPolicy.cs` — SHADOW is eligible / admit-able.
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` — `SimulateEntry` / `SimulateExit` only.
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` — flag + stale “unimplemented” copyNote.
- `D:\Prop\src\Infrastructure\Copy\DemoCopyLedger.cs` + `D:\Prop\data\demo_copy_ledger.json`
- `D:\Prop\reports\swarm\20260818\DEMO_COPY_OPEN.json` (ExecutionReport `35=8` / `150=F` / `39=2`; no secrets)
- `D:\Prop\.env` L73 boolean only: `REAL_COPY_EXECUTION_ENABLED=true`

---

## 1. Claim 1 — “no 35=D builder”

### 1a. Narrow (the named SUT `CTraderFixSession.cs`) — **PASS**

The only outbound assembler is `BuildLogon`. Tag 35 is hard-coded `"A"`. There is no `NewOrderSingle`, no `Build("D")`, no `(35, "D")`. Inbound `Extract(reply, "35")` only classifies the logon reply.

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
            // … 49/56/50/57/52/98/108/141/553/554 …
        };
        return Assemble(fields);
    }
```

`TryLogonAsync` writes that logon once, reads once, returns. **This file cannot place an order.**

### 1b. Product-wide (“there is no 35=D builder”) — **FAIL**

Same folder, live senders:

| File | Proof |
|---|---|
| `CTraderFixCopyOpen.cs` L142–149 `Build(string type, …)` + L95 `Build("D", sender, target, seq, extra)` | Generic FIX builder. `type` is written as tag 35. Called with `"D"`. Then `Write` to TRADE:5212. |
| `CTraderFixDemoTestTrade.cs` L139, L163, L197 | `Build("D", …)` flatten / open / close. |
| `CTraderFixDemoMatrix.cs` L91–93 `SendD` | `Build("D", …)` market/limit/stop matrix. |

`CopyTradingHostedService` (every 20s after 8s delay) calls `ExecuteDemoCopyAsync`, which calls `CTraderFixCopyOpen.SendAsync` for dest open and dest close. Gate is **demo host / demo sender / account ≠ 1369850**, not “no builder.”

```36:42:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
        if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
            || !sender.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
            || account == "1369850")
        {
            return Fail("Refused: dest must be demo FIX, not live 1369850.", host, account, sourceLogin);
        }
```

```83:95:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
            var closing = !string.IsNullOrWhiteSpace(destPositionId);
            // …
            await Write(ssl, Build("D", sender, target, seq, extra.ToArray()), timeout.Token);
```

```28:30:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

**Honesty:** confirming “`CTraderFixSession` has no 35=D” and implying the process cannot send is a category error. The send hop moved next door.

---

## 2. Claim 2 — `CanPromoteToLive` is false — **PASS**

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

Reachable `FromBaseline` set is `{INSUFFICIENT_DATA, RISK_BLOCKED, SHADOW, WATCH, EARLY_SCORE}` only. `LIVE` / `LIVE_CANDIDATE` are enum members (`TraderState.cs` L9–10) but **not produced here**. Persist copies `SuggestedState` (`DealIngestionService.cs` L140). Unit pin: `BaselineScorerTests.cs` L26 expects `CanPromoteToLive == false`.

**Limit of this PASS:** promotion-to-LIVE is locked. That does **not** lock dest send. `ExecuteDemoCopyAsync` does not require `TraderState.LIVE`.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL (disproven)**

There is **no** assignment `RealCopyEnabled = false` anywhere after logon.

**Only write site** (process start, not post-logon):

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Local `.env` L73 (boolean only):

```text
REAL_COPY_EXECUTION_ENABLED=true
```

API boot loads that env (`apps/api/Program.cs` L10 `EnvFile.FindAndLoad()` + L13 `AddEnvironmentVariables()`). Therefore a process started against this worktree **starts with `RealCopyEnabled=true`**.

Post-logon host **reads** the flag for a log line and **does not re-pin it**:

```60:70:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.Quote.LoggedOn = quote.LoggedOn;
        // … Trade.LoggedOn / Status / LastError / UpdatedAt …
        _log.LogInformation(
            "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
            quote.LoggedOn, trade.LoggedOn, _runtime.RealCopyEnabled, account);
```

`CTraderFixOptions.RealCopyExecutionEnabled` still defaults `false` (comment “Default OFF”) but **DI does not bind that POCO** to `LiveRuntimeStatus`. Two flags, one live.

`GET /api/health` and `/api/settings` would show the runtime boolean. **This slot could not GET them.** File proof is enough to fail “forced false after logon”: the logon path never writes the property.

---

## 4. Claim 4 — “sending now cannot be the profit path” — **FAIL**

Two different hops exist. Only one can fill dest.

### 4a. Intent / RiskEngine hop — cannot send (file-proven)

`CopyTradingService.VenueReconciled` is `const false` (L20). Persist of risk always sets `AllowFixSend = false` (L324). The dead branch that would mark `LIVE_SEND_BLOCKED_UNIMPLEMENTED` also requires `TraderState.LIVE` **and** `VenueReconciled` (L330), so it is unreachable. Intents go `SHADOW_ONLY` + `ShadowCopyEngine.SimulateEntry`.

`RiskEngine.Evaluate` *can* set `AllowFixSend=true` if `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). The `RealExecutionEnabled == false` block (L90–93) is a **comment only** — it does not return. That does not matter: the copy service ignores `decision.AllowFixSend` when persisting.

This hop is **not** a dest P&L path.

### 4b. Demo dest hop — **is** a send path, already filled

`NewOrderSingleImplemented => DemoDest` (L50). `DemoDest` is true when host starts `demo-`, trade sender starts `demo.`, and account ≠ `1369850`.

`ExecuteDemoCopyAsync`:

- returns 0 if `!DemoDest` (L485–488)
- does **not** check `RealCopyEnabled`
- does **not** check `CanPromoteToLive`
- does **not** call `RiskEngine`
- does **not** require `TraderState.LIVE`
- sends `CTraderFixCopyOpen.SendAsync` for ADMITTED roster opens (`MaxAutoLots = 0.05`) and for dest closes when source completed
- writes `intent.Status = "DEMO_SENT"` and dest fill fields (L590–593)

`CopyTradingHostedService` runs that method every 20s.

File-proven dest fill (demo `5328266`, not live `1369850`):

`D:\Prop\data\demo_copy_ledger.json` — source `305750` / pos `21250421` / 0.01 long / dest `237339770` / clOrd `C20260818093047317` / px `4390.2` / **`DestClosed: false`**.

`D:\Prop\reports\swarm\20260818\DEMO_COPY_OPEN.json` — `OrderSent: true`, `Filled: true`, `Host: demo-us-eqx-01.p.c-trader.com`, `Account: 5328266`, raw `35=8` + `150=F` + `39=2` + `6=4390.2` + `721=237339770`.

`CopyTradingService.cs` L500–511 **hard-seeds that same open fill** if the ledger lacks it.

`LiveCopyPage.tsx` L57 (UI, not a send): “Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position.”

**Profit-path reading (adversarial):** dest P&L is created by dest fills, not by `SHADOW_ONLY` rows and not by `CanPromoteToLive`. Sending **now** is the dest constructor. I cannot measure dest MTM this slot (no live GET, ledger has no exit). I therefore **cannot** prove sending cannot be the profit path. The opposite is coded and already executed on demo.

Wanting dest profit still does not make the 305750 fill +EV. An open 0.01 demo ticket is dest risk, not a measured edge.

---

## 5. Claim 5 — “SHADOW on demo is not destination profit” — **FAIL as a blanket claim**

Split the two meanings. Do not let one launder the other.

### 5a. `ShadowOrder` / `SimulateEntry` rows — **not dest profit** (PASS, narrow)

`ShadowCopyEngine.SimulateEntry` (L35–60) computes a modeled price from a stored quote + 0.05 point latency slip. It never opens a socket.

`PersistDemoShadowAsync` writes `Status = "SHADOW_ONLY"` and a `ShadowOrder` only when `state == TraderState.SHADOW` (EfTradingStore L267–333). `GenerateShadowIntentsAsync` does the same for copyable states when the live-send conjunction fails (always, because `VenueReconciled` is const false).

Those rows are **not** venue P&L. I have no live GET of shadow MTM. I do not claim a dollar number.

### 5b. TraderState.SHADOW on demo dest — **can be dest send** (FAIL)

`XauUsdOneToOneCopyPolicy.IsTraderEligible` **blocks** `{RISK_BLOCKED, DISQUALIFIED, PAUSED, INSUFFICIENT_DATA, EARLY_SCORE, WATCH}`. It does **not** block `SHADOW`. SHADOW with ≥20 completed XAU, `XauNetPnl > 0`, no size-pattern flags, demo/contest group → eligible.

`CopyRosterEngine.Decide` admits that eligible set (`AUTO_ADMIT`). `ExecuteDemoCopyAsync` then sends for every `ADMITTED` roster seat with an open XAU ticket ≤ 0.05 lots. **No `CurrentState == LIVE` check.**

So “SHADOW on demo” is the **intended admit state** for dest `35=D`. Simulated shadow fills are not dest profit; **SHADOW + ADMITTED + demo dest is the dest profit/loss constructor.**

I cannot prove dest P&L is $0 (open fill, no mark, GET blocked). Claim 5 as assigned is therefore **not proven** and, in sense 5b, **false**.

---

## 6. LiveCopyPage — what it proves / does not

```1:58:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
import { useCopyIntents, useCopyStatus } from '../api/hooks';
export default function LiveCopyPage() {
  const { data: status, isLoading } = useCopyStatus();
  const { data: intents = [] } = useCopyIntents();
  // … Stat REAL_COPY armed / SHADOW traders / LIVE traders / Live sends / Shadow fills …
  // empty: "Demo dest auto-sends after a trader is ADMITTED …"
}
```

- Hooks: `GET /api/copy/status` (3s) and `/api/copy/intents` (4s) via `apps/web/src/api/hooks.ts` L60–65.
- No POST. No FIX. No profit.
- Displays `realCopyArmed`, `liveSends`, `shadowFills`. Those numbers are **unverified this slot** (GET blocked).
- Older swarm notes that this page is a 321 B stub with literal `false` are **stale**. Current file is a live status table.

---

## 7. RiskEngine — what it does **not** do

Reject reasons (all `AllowFixSend=false`): `STOP_NEW_EXECUTION`, `EMERGENCY_FLATTEN_BLOCKS_NEW`, `VENUE_NOT_RECONCILED`, `VENUE_UNHEALTHY`, `QUOTE_MISSING`, `QUOTE_STALE`, `SPREAD_TOO_WIDE`, `PRICE_MOVED_TOO_FAR`, `SIGNAL_STALE`, `MAX_LOSS_PER_TRADER`, `MAX_DAILY_EXECUTION_LOSS`, `MAX_PORTFOLIO_DRAWDOWN`, `MAX_OPEN_POSITIONS`, `MAX_POSITION_QUANTITY`, `MAX_XAU_GROSS`, `MAX_XAU_NET`, `MAX_MARGIN_USAGE`, `MARTINGALE_BLOCK`, `ABNORMAL_SIZING_BLOCK`.

Those reasons would cut dest loss **if** the send hop used `Evaluate`. `ExecuteDemoCopyAsync` does not. RiskEngine is not a capital lock on the path that actually writes `35=D`.

---

## 8. Stale honesty (same-day documents vs this tree)

Do not treat these as evidence that send is off:

| Surface | Current contradiction |
|---|---|
| `CTraderFixLogonHostedService` L69 “NewOrderSingle still unimplemented” | `CTraderFixCopyOpen.SendAsync` implements `35=D` and is ticked. |
| `LiveRuntimeStatus.Snapshot` copyNote when armed: “NewOrderSingle still unimplemented; … No ticket will be sent.” | File-proven ticket `C20260818093047317` filled. |
| `GET /api/reconciliation/status` note “NewOrderSingle still off” (`Program.cs` L69) | Demo dest send is on. |
| `OverviewPage.tsx` / `ShadowPortfolioPage.tsx` “Live NewOrderSingle is off” | Live `1369850` is gated; **demo dest is not**. |
| `E002_no_live_send.md` “no function that emits FIX MsgType=D” | Three builders in `Sessions\`. |
| `BuildBlockers` “No NewOrderSingle sender — SAFE_BY_ABSENCE” | Only pushed when `!DemoDest`. On demo dest that blocker is **omitted**. |

---

## 9. Live GET (required; failed)

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/copy/status` | SSRF blocked |
| `http://127.0.0.1:5000/api/health` | SSRF blocked |
| `http://127.0.0.1:5000/api/overview` | SSRF blocked |

Therefore **unproven this slot:** process `realCopyEnabled`, `quoteLoggedOn` / `tradeLoggedOn`, `liveSends`, `shadowFills`, dest MTM, whether the 20s tick is running right now. File path + on-disk ledger/fill JSON still stand.

---

## 10. Claim scoreboard

| # | Claim | Verdict | Class |
|---|---|---|---|
| 1 | No `35=D` builder | **FAIL** product-wide. **PASS** only inside `CTraderFixSession.cs`. | Adjacent `CTraderFixCopyOpen` / DemoTestTrade / DemoMatrix emit `35=D`. |
| 2 | `CanPromoteToLive` is false | **PASS** | Literal `=> false`; `FromBaseline` never `LIVE`. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | Logon never writes the flag. DI binds `.env` boolean `true`. |
| 4 | Sending now cannot be the profit path | **FAIL** | Demo dest hop sends, has filled, is ticked every 20s. Dest P&L unmeasured (GET blocked). |
| 5 | SHADOW on demo is not destination profit | **FAIL** (blanket) | `SimulateEntry` ≠ dest P&L (**narrow PASS**). SHADOW is the admit state for dest `35=D`. Dest $0 **not proven**. |

**Package: FAIL.**

---

## 11. Risk to capital

| Book | File-proven |
|---|---|
| Live Pepperstone `1369850` | `CTraderFixCopyOpen` + `DemoDest` refuse. No live `35=D` in these files. |
| Demo dest `5328266` | **At risk.** Hosted 20s send + open 0.01 dest `237339770` @ 4390.2, `DestClosed=false`. |
| MT5 source book | Roster flatten is dest-only (`CopyRosterEngine` comment L31). Not proven via GET. |
| Shadow ledger | Simulated. Not dest cash. |

`risk_to_capital`: **DEMO dest YES / LIVE 1369850 gated / dest MTM unproven.**

---

## 12. What this slot will not say

- Will not say dest P&L is $0 or +EV.
- Will not say EX5 / MQ5 anything.
- Will not say the API is currently logged on.
- Will not treat `CanPromoteToLive => false` as a send lock.
- Will not print passwords, SenderComp secrets, or `.env` values other than the already-on-disk boolean.
