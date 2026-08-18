# P500_VERIFY_11 — Adversarial profit-path verify (slot 11)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_11.md` |
| Agent / slot | Adversarial verifier **11** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` live product under `apps/`, `src/` (not other-agent prose) |
| Assigned SUT | `CTraderFixSession.cs` (135/135), `BaselineScorer.cs` (212/212), `RiskEngine.cs` (189/189), `LiveCopyPage.tsx` (70/70) |
| Adjacent (proof only) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs` (625), `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `ShadowCopyEngine.cs`, `EfDashboardQueries.cs`, `ReconstructionScoringService`, `.env` **booleans / public dest ids only**, on-disk `DEMO_COPY_OPEN.json` + `data/demo_copy_ledger.json` |
| Assigned claims | (1) no `35=D` builder. (2) `CanPromoteToLive` is false. (3) `RealCopyEnabled` forced false after logon. (4) sending now cannot be the profit path. (5) SHADOW on demo is not destination profit. |
| Method | Independent full `read_file` of the four SUT files. Targeted `grep` of product `*.cs`/`*.tsx` for `35=D` / `Build("D"` / `CanPromoteToLive` / `RealCopyEnabled` / `ExecuteDemoCopyAsync`. `.env` flag + public FIX host/account/sender only. Live `GET http://127.0.0.1:5000/api/health` and `/api/copy/status` **blocked** (localhost SSRF). |
| Product source modified | **No.** |
| `.env` modified | **No.** |
| Secrets printed | **None.** Quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` and public dest ids `5328266` / `1369850`. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager Connect. No new TLS order. Prior dest fill cited from on-disk JSON only. |
| Honesty rule | **FAIL** any assigned claim that cannot be proven from a file this slot read or a live GET this slot performed. Other-agent reports (`P500_PROFIT_SYNTHESIS`, `CREDENTIALS_AND_COPY_STATUS`, `W500_VERIFY_11`, `E002`) are **not** evidence. `SAFE_BY_ABSENCE` on `CTraderFixSession` is not absence of every `35=D` builder. |

**One-line:**

```text
FAIL. (1) PASS on CTraderFixSession (35=A only) — sibling CTraderFixCopyOpen.Build("D") is production-wired. (2) PASS CanPromoteToLive=>false. (3) FAIL: logon does not pin RealCopyEnabled false; .env L73 true + DI L41 bind. (4) FAIL: 20s ExecuteDemoCopyAsync sends dest 35=D on DemoDest; DEMO_COPY_OPEN Filled=true. (5) PASS paper SHADOW≠dest cash; FAIL if read as “SHADOW demo cannot become dest P&L” (ADMITTED floor is SHADOW).
```

---

## 0. Verdict matrix

| # | Claim | Verdict | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** on assigned `CTraderFixSession.cs`. **FAIL** as a product-wide claim. | Session outbound tag 35 is `"A"` only (`BuildLogon` L96). Product **has** `CTraderFixCopyOpen.Build("D", …)` L95, called from `CopyTradingService.ExecuteDemoCopyAsync` L528/L566. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive` L211 is `=> false`. `FromBaseline` never returns `LIVE`. Production scorer writes `CurrentState = SuggestedState` (no LIVE). |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | Only write is DI ctor L41 from env. `CTraderFixLogonHostedService` L60–70 **reads** the bit for a log line; **zero** assignments to `false`. Lab `.env` L73 is `true`. Live GET of `/api/health` **not performed** (SSRF). |
| 4 | sending now cannot be the profit path | **FAIL** | Risk/LIVE hop cannot send (`VenueReconciled=false`, persist `AllowFixSend=false`). **Different hop** `ExecuteDemoCopyAsync` bypasses `RiskEngine` and emits dest `35=D` when `DemoDest`. On-disk fill: dest pos `237339770` @ 4390.2 on account `5328266`. LiveCopyPage empty-state copy **says** dest auto-sends. |
| 5 | SHADOW on demo is not destination profit | **PASS** (paper ledger). **FAIL** (dest hop). | `ShadowCopyEngine.SimulateEntry` writes `ShadowOrders`. Overview `DestinationRealPnl` is constructor **0**. Policy `IsTraderEligible` **allows** `SHADOW`; roster ADMIT + `ExecuteDemoCopyAsync` can turn that into dest cash on demo `5328266`. |

**Overall slot verdict: `FAIL`**

Claims 3 and 4 are assigned and not proven. Claim 1 is not a tree-wide absence. Claim 5 is only true for the simulated SHADOW book.

---

## 1. Claim 1 — no `35=D` builder

### 1.1 Assigned file: `CTraderFixSession.cs` (135/135) — PASS

Full read. The only outbound builder is `BuildLogon`:

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

- One `ssl.WriteAsync` (L49) of that Logon.
- Reply parse of tag `35` (L55–56). Success requires `msgType == "A"`. Reject string interpolates inbound `35={msgType}` (L73) — **not** a builder.
- File grep: `35=D` / `(35, "D")` / `NewOrderSingle` / `Build("D"` = **0**.
- Socket is `using` and disposed after the one read. This class cannot keep a TRADE session or send NewOrderSingle.

`BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`: **0** FIX builders.

### 1.2 Product-wide “no 35=D builder” — FAIL (cannot prove; opposite is on disk)

`grep` of `src/` + `apps/` + `tools/` (this slot):

| File | What |
|---|---|
| `src/Fix.CTrader/Sessions/CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra)` then `Write` |
| `src/Fix.CTrader/Sessions/CTraderFixDemoTestTrade.cs` L139, L163, L197 | `Build("D", …)` |
| `src/Fix.CTrader/Sessions/CTraderFixDemoMatrix.cs` L93 | `Build("D", …)` |

`CTraderFixCopyOpen.Build` is a generic MsgType assembler:

```142:149:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
    private static string Build(string type, string sender, string target, int seq, params (int, string)[] extra)
    {
        var fields = new List<(int, string)>
        {
            (35, type), (34, seq.ToString(CultureInfo.InvariantCulture)),
            (49, sender), (56, target), (50, "TRADE"), (57, "TRADE"),
            (52, DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture))
        };
```

Gate on that sender (L37–42): refuse unless `host` starts with `demo-`, `sender` starts with `demo.`, and `account != "1369850"`. That is a **live-identity refuse**, not “no builder.”

`CopyTradingHostedService` L27–30 every 20s: `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**. W500_VERIFY_11 / E002 / `CREDENTIALS_AND_COPY_STATUS` “method does not exist” / `NewOrderSingleImplemented=false` are **stale**.

---

## 2. Claim 2 — `CanPromoteToLive` is false — PASS

```187:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
public static class TraderStateMachine
{
    public static TraderState FromBaseline(bool earlyEligible, decimal quality, decimal risk, FeatureSnapshot features)
    {
        if (features.CompletedXauTrades == 0)
            return TraderState.INSUFFICIENT_DATA;
        // …
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

- Ceiling of `FromBaseline` is `SHADOW`. Tokens `LIVE` / `LIVE_CANDIDATE` do not appear in this method.
- `CanPromoteToLive` ignores `current` and returns `false`.
- Unit pin: `tests/Unit/BaselineScorerTests.cs` L25–26 (three winners → `SHADOW`; `CanPromoteToLive` false).
- Production write path (`ReconstructionScoringService` L128–140) sets `CurrentState = score.SuggestedState`. Grep of product `*.cs` for `CanPromoteToLive` callers outside tests: **0**. The function is a hard door that **nothing in the dest hop consults**.

Residual (does not fail the claim): dest send is **not** gated on `LIVE`. Roster ADMIT + `DemoDest` is enough (claim 4).

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL

### 3.1 What logon actually does

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

No `_runtime.RealCopyEnabled = false`. The log line **echoes** whatever DI already stored. Persist (L91–111) writes FIX session rows only.

### 3.2 The only assignment in product C#

`grep` `RealCopyEnabled =` under `*.cs` (this slot) = **one** hit:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Lab `.env` L73: `REAL_COPY_EXECUTION_ENABLED=true`. API `Program.cs` L10–13 loads that env before `AddTraderIntelligence`. Therefore process bind is **true** unless some other host omits the env.

`CTraderFixOptions.RealCopyExecutionEnabled` still defaults `false` (POCO L35). That POCO is **not** what `LiveRuntimeStatus` reads. `/api/health` and `/api/settings` echo `runtime.RealCopyEnabled` (`Program.cs` L55, L76).

### 3.3 Live GET

`GET http://127.0.0.1:5000/api/health` and `/api/copy/status` this slot: **SSRF blocked**. Cannot prove the running process bit. File proof already **disproves** “forced false after logon.” `CREDENTIALS_AND_COPY_STATUS.md` “false (forced)” and synthesis “hosted sets false” are **stale**.

---

## 4. Claim 4 — sending now cannot be the profit path — FAIL

Two hops exist. Only one is a sender.

### 4.1 Risk / LIVE hop — cannot send (proven)

`CopyTradingService`:

- `VenueReconciled` **const `false`** (L20).
- `GenerateShadowIntentsAsync` passes `Reconciled = VenueReconciled` into `RiskEngine.Evaluate` (L304).
- `RiskEngine` L84–85: unreconciled **increasing** actions `Reject("VENUE_NOT_RECONCILED")` with `AllowFixSend=false`.
- Approve path L147–150: `allowSend = RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. With `Reconciled=false`, `AllowFixSend` is false even on closes.
- Persist L317–324 **overwrites** `AllowFixSend = false`.
- Live-send `if` L330 requires `decision.AllowFixSend && CurrentState==LIVE && NewOrderSingleImplemented && VenueReconciled`. The last conjunct is a compile-time false. Branch is dead. Else: `SHADOW_ONLY` + optional `ShadowCopyEngine.SimulateEntry`.

`RiskEngine` L90–93 when `RealExecutionEnabled==false` is an **empty comment**, not a return. The later `allowSend` product is still false if the flag is false. That is not a send choke on the demo hop.

`LiveCopyPage.tsx` is GET-only (`useCopyStatus` / `useCopyIntents`). No POST. The amber banner is display. This page cannot be the profit path.

### 4.2 Demo dest hop — **is** a send path (disproves the claim)

```45:50:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public bool DemoDest =>
        (_config["CTRADER_FIX_HOST"] ?? "").StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
        && (_config["CTRADER_FIX_TRADE_SENDER_COMP_ID"] ?? "").StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
        && _config["CTRADER_FIX_ACCOUNT_ID"] != "1369850";

    public bool NewOrderSingleImplemented => DemoDest;
```

Lab `.env` public ids (no secrets):

| Key | Value | `DemoDest` limb |
|---|---|---|
| `CTRADER_FIX_HOST` | `demo-us-eqx-01.p.c-trader.com` | starts with `demo-` |
| `CTRADER_FIX_TRADE_SENDER_COMP_ID` | `demo.pepperstone.5328266` | starts with `demo.` |
| `CTRADER_FIX_ACCOUNT_ID` | `5328266` | ≠ `1369850` |

`DemoDest` is **true** on this lab bind. `NewOrderSingleImplemented` is therefore **true**. W500/E002 const-false pins are **stale**.

`ExecuteDemoCopyAsync` (L483–605):

1. Returns 0 if `!DemoDest` (live host/sender/account `1369850` skip).
2. Seeds ledger row `305750` / `21250421` / dest `237339770` if missing (L500–512).
3. Closes dest via `CTraderFixCopyOpen.SendAsync(..., destPositionId)` when source completed.
4. For each `ADMITTED` roster seat, open XAU ≤ `MaxAutoLots` (0.05), calls `CTraderFixCopyOpen.SendAsync` **without** `RiskEngine.Evaluate`.
5. On fill: ledger + intent `Status = "DEMO_SENT"`.

Hosted tick (`CopyTradingHostedService` L30) calls this every 20s. `CTraderFixCopyOpen` L95 writes `35=D` on TRADE `:5212`.

On-disk dest fill (not a live GET; not a secret):

`D:\Prop\reports\swarm\20260818\DEMO_COPY_OPEN.json`: `Allowed=true`, `LoggedOn=true`, `OrderSent=true`, `Filled=true`, `Account=5328266`, `Host=demo-us-eqx-01.p.c-trader.com`, `PosId=237339770`, `LastPx=4390.2`, `ClOrdId=C20260818093047317`. Raw ExecReport `35=8` / `150=F` / `39=2`.

`D:\Prop\data\demo_copy_ledger.json`: same dest still `DestClosed: false`.

`LiveCopyPage.tsx` L57 empty-state (this slot read):

> Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position.

The UI contradicts “sending cannot be the profit path.”

### 4.3 Why this still is not a *live-capital* profit machine

- Live Pepperstone `1369850` is refused by `CTraderFixCopyOpen` L37–42 and by `DemoDest`.
- `CanPromoteToLive` is false; scorer never emits `LIVE`.
- Copy-all of the scored XAU book is **not** what this hop sends; it sends ADMITTED + ≤0.05 lot. That is dest P&L on **demo** `5328266`, not proof of +EV.
- Wanting profit is not an edge. Sending now **can** move dest cash on the demo account. That is enough to **fail** “cannot be the profit path.”

---

## 5. Claim 5 — SHADOW on demo is not destination profit

### 5.1 Paper SHADOW ledger — PASS (not dest cash)

| Surface | What it is |
|---|---|
| `ShadowCopyEngine.SimulateEntry` | Mid/ask-bid model + 0.05 latency slip. No socket. |
| `GenerateShadowIntentsAsync` else-branch | `Status = "SHADOW_ONLY"` + `ShadowOrders` row |
| `EfTradingStore.PersistDemoShadowAsync` | Only if `state == SHADOW`; same simulate; early-return if no `DestinationQuotes` |
| `EfDashboardQueries.GetOverviewAsync` L29 / L44 | `ShadowPnl` = `Sum(SourceVsShadowSlippage)`; **`DestinationRealPnl` literal `0`** |
| `LiveCopyPage` | Displays `shadowFills` / `shadowTraders`. Does not post dest. |

Simulated SHADOW PnL is **not** destination cash. `ShadowPortfolioPage.tsx` L7 still says “Live NewOrderSingle remains disabled” — that sentence is **stale** relative to `ExecuteDemoCopyAsync`.

### 5.2 SHADOW as dest-selection floor — cannot prove “not dest profit”

`XauUsdOneToOneCopyPolicy.IsTraderEligible` L81–85 rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET`. It **accepts** `SHADOW` (and `LIVE_CANDIDATE` / `LIVE`) if 20 completed XAU, `XauNetPnl > 0`, no size-pattern, **and** `CopyGroupFilter.IsDemoOrContest` (demo/contest groups only).

`CopyRosterEngine.Decide` AUTO_ADMITs that eligible set. `ExecuteDemoCopyAsync` then sends dest `35=D` for `ADMITTED` + open XAU. **No `TraderState.LIVE` check on that hop.**

Therefore: a **demo-group SHADOW** name with 20+ profitable XAU is exactly who can print dest P&L on `5328266`. Claim 5 as a global “SHADOW on demo is not dest profit” is **not proven**. The paper book is not dest; the dest hop **uses** SHADOW as the admission floor.

---

## 6. Live GET this slot

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | **Not fetched.** Tool SSRF-blocks `127.0.0.1`. |
| `http://127.0.0.1:5000/api/copy/status` | **Not fetched.** Same. |
| `http://127.0.0.1:5000/api/overview` | **Not fetched.** Same. |

Any claim that needs the running `realCopyEnabled` / `realCopyArmed` / dest PnL DTO **fails this slot**. File + on-disk ledger are sufficient to fail claims 3 and 4.

P500 synthesis mid-scoring pin (8463 / SHADOW 70 / dest DTO $0) is **cited, not re-measured**.

---

## 7. Stale pins this slot will not repeat

| Pin | Why stale |
|---|---|
| `NewOrderSingleImplemented = false` (const) | HEAD is `=> DemoDest` (`CopyTradingService` L50) |
| `CTraderFixLogonHostedService` forces `RealCopyEnabled=false` | File has **no** such assignment |
| Product `35=D=0` / SAFE_BY_ABSENCE on **all** hops | `CTraderFixCopyOpen` + hosted `ExecuteDemoCopyAsync` |
| `CREDENTIALS` “Live 35=D method does not exist” | Method exists; demo-gated |
| `E002` / W500_VERIFY_11 “copy hop SAFE_BY_ABSENCE” | True only for `CTraderFixSession` + LIVE/`Evaluate` hop |
| `LiveCopyPage` “Pepperstone cannot be filled” | True for live `1369850`; false for demo dest if `DemoDest` |

---

## 8. Risk to capital

| Dest | Risk |
|---|---|
| Live Pepperstone `1369850` | **NONE** this hop (`account == "1369850"` refuse + `DemoDest` false). `CTraderFixSession` is logon-only. |
| Demo Pepperstone `5328266` | **DEST P&L ACTIVE.** Hosted 20s sender + on-disk fill `237339770` still open. Not live cash. Still a venue position. |
| MT5 source books | Not flattened by dest hop (`CopyRosterEngine` comment + dest-only flatten intents). |

Do not write `SAFE_BY_ABSENCE` as a tree-wide capital statement.

---

## 9. What this slot did not do

- Did not edit product or `.env`.
- Did not send a new `35=D`.
- Did not attach Manager / re-sum 18/8460.
- Did not treat other-agent PASS as proof.
