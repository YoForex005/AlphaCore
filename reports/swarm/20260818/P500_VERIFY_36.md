# P500_VERIFY_36 — Adversarial confirm (slot 36)

| Field | Value |
|---|---|
| Slot | **36** |
| Date | 2026-08-18 |
| Agent | P500_VERIFY_36 (adversarial; did **not** trust sibling `P500_*` / `W500_*` numbers) |
| Assigned | Read `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`. Confirm: (1) no `35=D` builder; (2) `CanPromoteToLive` is false; (3) `RealCopyEnabled` forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. **FAIL any claim that cannot be proved from a file or live GET.** |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **None.** Password / proxy / FIX password values were not quoted. Only the already-on-disk boolean `REAL_COPY_EXECUTION_ENABLED=true` is named. Public dest ids `5328266` / `1369850` appear in product source. |
| Live GET this slot | **Attempted and blocked.** `web_fetch` to `http://127.0.0.1:5000/api/health` → SSRF blocked. `open_page` to the same URL → retrieve error. **No live JSON.** File proof only. |
| Live `35=D` sent this slot | **No.** |
| `REAL_COPY` flipped this slot | **No.** |
| Method | Full `read_file` of the four assigned files plus the logon / DI / copy hop they actually call. Targeted `grep` for `35=D` / `Build("D")` / `CanPromoteToLive` / `RealCopyEnabled` / `SHADOW` / `DestinationRealPnl`. `.env` L73 inspected **for that flag key only**. |
| Honesty rule | Sibling swarm PnL / census integers are **not** evidence. A comment is **not** a runtime pin. A dashboard label is **not** dest cash. Live GET failure is **not** a proof of `false`. |

---

## 0. Verdict (binding)

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** (scoped to `CTraderFixSession.cs`) | File **135/135**. Only outbound MsgType is `(35, "A")` at L96. Grep of this file for `35=D` / `(35, "D")` / `Build("D")` / `NewOrderSingle` = **0**. Tree-wide product `35=D=0` is **false** (siblings `CTraderFixCopyOpen` / `CTraderFixDemoTestTrade` / `CTraderFixDemoMatrix`). |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(TraderState current) => false` at `BaselineScorer.cs` L211. Argument unused. Unit test asserts SHADOW ≠ LIVE. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | **Disproved.** Logon host **reads** the flag and **never assigns** `false`. Sole `RealCopyEnabled =` write is DI bind of `.env` `REAL_COPY_EXECUTION_ENABLED=true`. Live GET of `/api/health` **not obtained**. |
| 4 | sending now cannot be the profit path | **FAIL** | **Cannot prove.** Assigned session cannot send. Persist `AllowFixSend=false`. Dest DTO constructor is `0`. Residual **disproves** the unscoped claim: hosted `ExecuteDemoCopyAsync` → sibling `CTraderFixCopyOpen.Build("D")` on demo dest, **without** `RiskEngine.Evaluate` and **without** reading `RealCopyEnabled`. Dest fill price is stored. Dest cash PnL was **not** live-GET. Dashboard `0` is not dest cash. |
| 5 | SHADOW on demo is not destination profit | **PASS** | `SHADOW` is a **source-trader state**. Dashboard `ShadowPnl` is `Sum(SourceVsShadowSlippage)`. `DestinationRealPnl` is constructor `0`. Paper `ShadowCopyEngine.SimulateEntry` is not a venue fill. Residual: SHADOW is the **admit class** for demo dest send (`IsTraderEligible` rejects WATCH/EARLY/INSUFFICIENT; SHADOW can `AUTO_ADMIT`). Admit ≠ dest profit. |

**Slot verdict: `FAIL`.**

Claim 3 is **disproved**, not merely unproved. Claim 4 is **unproved** as a general statement and **disproved** if “profit path” includes dest-venue cash from a demo fill. One FAIL is enough. Claims 1 (file-scoped), 2, and 5 hold from files.

**Risk to capital:** **NONE on live `1369850`** (`CTraderFixSession` has no order builder; `CTraderFixCopyOpen` refuses non-`demo-` host / non-`demo.` sender / account `1369850`). **Not absent on demo dest** if `DemoDest` is true: hosted 20s tick can emit `35=D`. This slot sent **0**.

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
| `WriteAsync` count | **1** (L49, the logon bytes) |
| `35=D` / `(35, "D")` / `Build("D")` / `NewOrderSingle` in this file | **0** (`grep` on this path: no matches) |
| Socket lifetime | `using TcpClient` + `await using SslStream`; disposed after one read |
| Inbound `35=A` treated as | `LoggedOn = true` (L56–64) — **logon ack, not a fill** |
| Other outbound MsgTypes | **none** |

Adversarial residual (**does not fail the assigned-file claim**): sibling product files **do** have a `35=D` builder.

| File | What this slot measured |
|---|---|
| `src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra)` after TRADE logon + SecurityList. **Refuses** non-`demo-` host, non-`demo.` sender, or account `1369850` (L37–42). **Called from** `CopyTradingService.ExecuteDemoCopyAsync`. |
| `src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | `Build("D", …)` ×3 (L139 flatten, L163 open, L197 close). Demo-gated; refuses `1369850`. Tools helper. |
| `src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` L93 | `Build("D", …)` present (matrix helper). |

Claim 1 as written against **`CTraderFixSession.cs`** is true. Tree-wide “product `35=D=0`” is **false** on HEAD. An unscoped reading of claim 1 would **FAIL**. This slot scopes the claim to the assigned session file.

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
| Product callers | `grep CanPromoteToLive` in `*.cs`/`*.tsx`: definition + `tests\Unit\BaselineScorerTests.cs` L26 + a `_tmp_*` scratch program. **Zero** product callers that can flip it. |
| Test | `Three_disciplined_winners_go_to_shadow_not_live`: three winners → `SHADOW`; `CanPromoteToLive(...).Should().BeFalse()`. |

There is **no** code path in this file that emits `TraderState.LIVE`. Promotion to live is not a scorer function. Vacuous lock: nothing in product calls it to gate send. That does **not** fail the claim “is false.”

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL**

This claim is **false on disk**. A live GET that would show the runtime bit was **blocked**, so this FAIL is from files only. Files are enough: the force-false write does not exist.

### 3.1 Logon host does not re-pin

Assigned-adjacent file (the only post-logon writer of `LiveRuntimeStatus` FIX fields): `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`.

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

`grep` for `RealCopyEnabled =` under `*.cs`: **one hit**.

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

That is a **bind**, not a force-false.

### 3.2 Lab env is `true`

`D:\Prop\.env` L73 (flag key only): `REAL_COPY_EXECUTION_ENABLED=true`.

`apps\api\Program.cs` L10 loads that env (`EnvFile.FindAndLoad()`), then L15 `AddTraderIntelligence`. `/api/health` L55 and `/api/settings` L76 echo `runtime.RealCopyEnabled` — they do **not** override it.

### 3.3 Options default is unused by logon

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (`CTraderFixOptions.cs` L35). `CTraderFixLogonHostedService` does **not** read that POCO. `apps\fix-worker\Worker.cs` reads a separate worker flag. That is **not** API logon.

`LiveRuntimeStatus.Snapshot()` copyNote when armed still claims “NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.” That string is **not** a pin. HEAD `NewOrderSingleImplemented => DemoDest` and the hosted hop **can** send on demo dest.

### 3.4 What “after logon” actually leaves

| Object | After successful `35=A` |
|---|---|
| `_runtime.Quote.LoggedOn` / `_runtime.Trade.LoggedOn` | set from logon result |
| `_runtime.RealCopyEnabled` | **unchanged** from DI bind of env (`true` on this lab disk) |
| This slot live GET of `/api/health` or `/api/settings` | **not obtained** (loopback SSRF) |

Cannot claim “forced false after logon.” The opposite wiring is on disk. Prior “hosted re-pin false” reports are **stale**.

---

## 4. Claim 4 — sending now cannot be the profit path — **FAIL**

Instruction: FAIL any claim that cannot be proved from a file or live GET.

“Sending now cannot be the profit path” is a **negation about dest cash**. To PASS it I must prove dest-venue P&L cannot be produced by a send that exists **now**. I cannot.

### 4.1 What I *can* prove (not enough)

| Fact | File |
|---|---|
| Assigned session cannot emit `35=D` | `CTraderFixSession.cs` 135/135 (claim 1) |
| Scorer cannot emit `LIVE` | `CanPromoteToLive => false`; `FromBaseline` ceiling `SHADOW` |
| Hopper persist never arms FIX send | `CopyTradingService.cs` L324 `AllowFixSend = false` |
| Hopper `Evaluate` uses `VenueReconciled` **const `false`** (L20, L304) | increasing actions reject `VENUE_NOT_RECONCILED` (`RiskEngine.cs` L84–85) |
| Live-send branch L330 is dead | requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled`; const `false` |
| Dashboard dest PnL constructor | `EfDashboardQueries.GetOverviewAsync` L44 literal `0` into `OverviewDto.DestinationRealPnl` |
| `LiveCopyPage.tsx` | no dest-PnL field, no send button, no profit column |

`RiskEngine` allow-send formula (L147–150):

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

L90–93 (“Shadow path still evaluates risk but never allows FIX send”) is a **comment no-op**. When `RealExecutionEnabled == false` the engine does **not** reject. The real gate is `allowSend` plus persist-false.

`LiveCopyPage.tsx` L13 still shows `REAL_COPY armed` from `status?.realCopyArmed` (`CopyGateStatus.RealCopyArmed` = `_runtime.RealCopyEnabled`). That is a **dashboard lamp**, not dest cash.

### 4.2 What disproves the unscoped claim

HEAD **does** send on demo dest. A filled dest ticket **can** be dest cash even if the overview DTO stays `0`.

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
- Calls `CTraderFixCopyOpen.SendAsync` → `Build("D")` (open L566; close L528).
- Caps `MaxAutoLots = 0.05m` (L22) on **source** tickets, then sends **1:1** those lots.
- Seeds a ledger row for source `305750` / pos `21250421` dest pos `237339770` if missing (L500–512).
- On fill, writes `intent.Status = "DEMO_SENT"` and `DestFillPrice` (L590–593).
- Refuses live identity `1369850` inside `CTraderFixCopyOpen` L37–42.

`GetStatusAsync` L76–77: when `DemoDest`, summary is **“Demo dest auto-copy ON. Eligible demo/contest opens send on the 20s tick”**. `LiveCopyPage.tsx` L57 empty-state **admits** the same: “Demo dest auto-sends after a trader is ADMITTED … Dest closes when that MT5 position closes.”

`GetStatusAsync` also reports `VenueReconciled: DemoDest` (L67) while hopper Evaluate uses const `false`. The status DTO **lies** relative to the risk hop. That is not dest profit; it is a second honesty hole.

### 4.3 Why dest DTO `0` is not a proof

`grep DestinationRealPnl` in product `*.cs`/`*.tsx`: DTO field + overview constructor `0` + web `OverviewPage` display. **No** dest-account mark-to-market. `DestFillPrice` is stored on intents/ledger and **never** folded into `DestinationRealPnl`.

A constructor `0` proves the **dashboard does not book dest profit**. It does **not** prove dest venue cash is unchanged. This slot has **no live GET** of dest PnL. Therefore “sending now cannot be the profit path” is **unproved**. Because a send hop exists and can fill, the unscoped claim is **false**.

Scoped restatement that *would* PASS: “the assigned session + risk hopper + overview DTO are not a booked dest-profit constructor.” That is not the assigned wording.

This slot did not live-GET dest fills and did not send.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **PASS**

### 5.1 `SHADOW` is a source state, not dest cash

`BaselineScorer` / `TraderStateMachine` assign `TraderState.SHADOW` when `quality >= 70 && risk < 40` (L200–201). Inputs: source XAU features (`NetPnl`, martingale, SL use, lot CV). **No dest account, no dest fill, no dest currency.**

`AfterHighEarlyScore() => SHADOW` (L209). Still a state enum.

Quality formula (L152–160) can be high while `NetPnl` is negative (behavior term − 0.25×risk). That is **source quality**, not dest profit.

### 5.2 Dashboard `ShadowPnl` is paper slippage

`EfDashboardQueries.GetOverviewAsync` L29:

```29:29:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var shadowPnl = await _db.ShadowOrders.SumAsync(s => (decimal?)s.SourceVsShadowSlippage, ct) ?? 0;
```

`ShadowCopyEngine.SimulateEntry` (L35–60) computes a modeled ask/bid ± 0.05 and `SourceVsShadowSlippage`. No socket. No `35=D`.

Hopper `GenerateShadowIntentsAsync` L201–360: for states `{SHADOW, LIVE_CANDIDATE, LIVE}` that are roster `ADMITTED`, writes `Status = "SHADOW_ONLY"` and optional paper `ShadowOrder`. Persist `AllowFixSend = false`.

### 5.3 `LiveCopyPage` shows SHADOW counts, not dest cash

`LiveCopyPage.tsx` L14: `SHADOW traders` = `status?.shadowTraders` (count of `TraderState.SHADOW`). No dest PnL column. Intent table shows `status` / `riskReason`. Empty-state talks about demo dest auto-send on **ADMITTED**, not on the SHADOW label.

### 5.4 Residual the claim must not hide

`XauUsdOneToOneCopyPolicy.IsTraderEligible` (L81–85) rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET`. It **allows** `SHADOW` (and `LIVE` / `LIVE_CANDIDATE` if those states existed). Combined with `CopyGroupFilter.IsDemoOrContest` + 20 completed XAU + `XauNetPnl > 0` + no size-pattern, `CopyRosterEngine` returns `AUTO_ADMIT`.

So: **SHADOW on a demo/contest source can be the admit class** for `ExecuteDemoCopyAsync` dest `35=D`. That is dest **activity**, not dest **profit accounting**. The assigned claim is “SHADOW on demo is not destination profit.” Identity holds: the enum and the slippage sum are not dest cash. Dest cash from a later demo fill is claim 4’s residual, not a dest-PnL constructor named SHADOW.

---

## 6. Assigned `RiskEngine.cs` and `LiveCopyPage.tsx` (census)

### 6.1 `RiskEngine.cs` (**189** lines, full read)

| Item | Measured |
|---|---|
| Reject reasons that set `AllowFixSend=false` | `STOP_NEW_EXECUTION`, `EMERGENCY_FLATTEN_BLOCKS_NEW`, `VENUE_NOT_RECONCILED`, `VENUE_UNHEALTHY`, `QUOTE_MISSING`, `QUOTE_STALE`, `SPREAD_TOO_WIDE`, `PRICE_MOVED_TOO_FAR`, `SIGNAL_STALE`, `MAX_LOSS_PER_TRADER`, `MAX_DAILY_EXECUTION_LOSS`, `MAX_PORTFOLIO_DRAWDOWN`, `MAX_OPEN_POSITIONS`, `MAX_POSITION_QUANTITY`, `MAX_XAU_GROSS`, `MAX_XAU_NET` (ReduceSize), `MAX_MARGIN_USAGE`, `MARTINGALE_BLOCK`, `ABNORMAL_SIZING_BLOCK` |
| Approve reasons | `RISK_REDUCTION`, `APPROVED` — `AllowFixSend = allowSend` |
| `RealExecutionEnabled == false` | **no reject** (L90–93 comment only) |
| Does this file send FIX? | **No.** Pure decision. |
| Does this file force `RealCopyEnabled` false? | **No.** It reads `request.RealExecutionEnabled`. |

A reject list is **not** dest profit. It would reduce dest loss **if** a live send existed. The hosted demo hop **bypasses** this file.

### 6.2 `LiveCopyPage.tsx` (**70** lines, full read)

| Item | Measured |
|---|---|
| Send control | **none** |
| Dest PnL | **none** |
| Lamps | `realCopyArmed`, SHADOW/LIVE counts, `liveSends`, intents, shadow fills, QUOTE/TRADE |
| Blockers | listed under “Live send blockers (Pepperstone cannot be filled)” |
| Empty state L57 | admits demo dest auto-send on ADMITTED |

The page is a **status chrome**, not a profit path.

---

## 7. Live GET

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | `web_fetch` SSRF blocked; `open_page` retrieve failed |
| `/api/settings`, `/api/copy/status`, `/api/ingest/status` | **not fetched** (same loopback) |

Cannot confirm runtime `realCopyEnabled` from a live process this slot. File bind of `.env=true` stands. Cannot confirm dest account cash.

---

## 8. What this slot did **not** do

- Did not modify product or test source.
- Did not send `35=D`.
- Did not flip `REAL_COPY`.
- Did not print secrets.
- Did not treat sibling census / dest-PnL integers as evidence.
- Did not hash files (no hash tool in this agent). Line counts are from full reads.

---

*End of P500_VERIFY_36. Product source was not modified. No `35=D` was built or sent by this slot. No secrets printed. `REAL_COPY` was not flipped by this slot.*
