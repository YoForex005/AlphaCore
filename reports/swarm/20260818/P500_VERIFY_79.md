# P500_VERIFY_79 — Adversarial verifier (slot 79)

| Field | Value |
|---|---|
| Slot | **79** |
| Date | 2026-08-18 |
| Agent | P500_VERIFY_79 (adversarial; re-read HEAD files this slot; did **not** trust sibling `P500_*` / `W500_*` numbers) |
| Assigned | Read `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`. Confirm: (1) no `35=D` builder; (2) `CanPromoteToLive` is false; (3) `RealCopyEnabled` forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. **FAIL any claim that cannot be proved from a file or live GET.** |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_79.md` |
| Assigned SUT | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135), `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212), `D:\Prop\src\Domain\Risk\RiskEngine.cs` (189/189), `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (70/70) |
| Hop (not assigned; required to test 3–5) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `apps\api\Program.cs`, `EnvFile.cs`, `.env` **flag / dest-identity keys only**, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `CopyGroupFilter.cs`, `ShadowCopyEngine.cs`, `DealIngestionService.cs`, `EfTradingStore.cs`, `DemoCopyLedger.cs`, `data\demo_copy_ledger.json` |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **None.** Password / proxy / FIX password / connection-string values were not quoted. Only the already-on-disk boolean `REAL_COPY_EXECUTION_ENABLED=true`, public dest host prefix `demo-`, sender prefix `demo.`, and public dest ids `5328266` / `1369850`. |
| Live GET this slot | **Attempted and blocked.** `web_fetch` `http://127.0.0.1:5000/api/health` → SSRF blocked (loopback). `web_fetch` `http://localhost:5000/api/health` → same. `open_page` `http://127.0.0.1:5000/api/health` → retrieve failed. **No live JSON.** File proof only. Claims that need a live body are **FAIL**. |
| Live `35=D` sent this slot | **No.** |
| `REAL_COPY` flipped this slot | **No.** |
| Method | Full `read_file` of the four assigned files plus the logon / DI / copy hop they actually call. Targeted `grep` for `35=D` / `Build("D")` / `CanPromoteToLive` / `RealCopyEnabled` / `DestinationRealPnl` / `CurrentState =`. `.env` inspected **for flag + dest-identity keys only**. |
| Honesty rule | Sibling swarm PnL / census integers are **not** evidence. A comment is **not** a runtime pin. A dashboard label is **not** dest cash. `CREDENTIALS_AND_COPY_STATUS.md` “false (forced)” / “method does not exist” is **STALE vs HEAD**. |

---

## 0. Verdict (binding)

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** (scoped to `CTraderFixSession.cs`) / **FAIL** if unscoped | Assigned file **135/135**. Only outbound MsgType is `(35, "A")` at L96. File grep `35=D` / `(35, "D")` / `Build("D")` / `NewOrderSingle` = **0**. Product residual: sibling `Build("D")` ×**5**. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(TraderState current) => false` at `BaselineScorer.cs` L211. Argument unused. `FromBaseline` never returns `LIVE` / `LIVE_CANDIDATE`. Persist writes `CurrentState = score.SuggestedState`. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** (**disproved**) | Logon host **reads** the flag and **never assigns** `false`. Sole product `RealCopyEnabled =` write is DI L41 binding `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true`. Live GET of `/api/health` **blocked** — not required to disprove the pin (the assignment does not exist). |
| 4 | sending now cannot be the profit path | **FAIL** (unscoped) / **PASS_NOT_BOOKED_DEST_PROFIT** (dashboard ctor) | Assigned session cannot send. Persist hop writes `AllowFixSend=false` and Evaluate uses `VenueReconciled` const `false`. Dashboard `DestinationRealPnl` is constructor `0`. Residual that **fails** the unscoped claim: hosted `ExecuteDemoCopyAsync` **can** emit dest `35=D` now, **without** `RiskEngine.Evaluate` and **without** reading `RealCopyEnabled`. On-disk ledger dest pos `237339770` is dest **activity**, not dest cash. Live dest mark **not** GET-proven. |
| 5 | SHADOW on demo is not destination profit | **PASS** | `SHADOW` is a **source-trader state**. Dashboard `ShadowPnl` is `Sum(SourceVsShadowSlippage)`. `DestinationRealPnl` is constructor `0`. `LiveCopyPage` shows SHADOW **counts**, no dest cash. Paper `ShadowCopyEngine.SimulateEntry` is not a venue fill. Residual: SHADOW + `ADMITTED` can still fire demo dest `35=D` (dest **hop**, not dest **profit**). |

**Slot verdict: `FAIL`.**

Claim 3 is **disproved**, not merely unproved. One FAIL is enough. Claim 1 holds only when scoped to the assigned session file. Tree-wide “no `35=D` builder” is **false** on HEAD. Claim 4 as written (“sending now cannot be the profit path”) is **false** as a product-wide statement because the 20s hopper is a dest-send path.

**Risk to capital:** **NONE on live `1369850`** (`SAFE_BY_ABSENCE` for `CTraderFixSession` + `CTraderFixCopyOpen` refuse). **Not absent on demo dest `5328266`** if `DemoDest` is true: hosted `ExecuteDemoCopyAsync` can emit `35=D` **without** `RiskEngine.Evaluate` and **without** reading `RealCopyEnabled`. This slot sent **0**.

---

## 1. Claim 1 — no `35=D` builder — **PASS** (`CTraderFixSession.cs` only) / **FAIL** (product)

Assigned file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135** lines, full read).

The only outbound builder is `BuildLogon`. The only MsgType it assembles is logon `A`:

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

| Check | Measured this slot |
|---|---|
| Outbound MsgType in this file | **only** `(35, "A")` L96 |
| `WriteAsync` count | **1** (L49, the logon bytes) |
| `35=D` / `(35, "D")` / `Build("D")` / `NewOrderSingle` in this file | **0** (file grep; only `(35, "A")` hit) |
| `Assemble` callers | **1** (`BuildLogon`) |
| Socket lifetime | `using TcpClient` + `await using SslStream`; disposed after one read |
| Inbound `Extract(reply, "35")` L55 | **read**, not a builder |
| Inbound `35=A` treated as | `LoggedOn = true` (L56–64) — **logon ack, not a fill** |
| Error text L73 `"Logon rejected 35={msgType}"` | inbound type echoed; not outbound D |

Adversarial residual (**does not fail the assigned-file claim**; **does fail** a product-wide “no `35=D` builder” claim):

| File | What this slot measured |
|---|---|
| `src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 / L142–156 | Generic `Build(string type, …)` then `Build("D", …)` after TRADE `35=A` + SecurityList. **Refuses** non-`demo-` host, non-`demo.` sender, or account `1369850` (L37–42). Hosted by `CopyTradingService.ExecuteDemoCopyAsync`. |
| `src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` L93 | `Build("D", …)` (matrix helper). |
| `src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | `Build("D", …)` ×3 (demo test helper). |

Product `Build("D")` count in `*.cs`: **5**. Claim 1 as written against **`CTraderFixSession.cs`** is true. Tree-wide “product `35=D=0`” / `CREDENTIALS_AND_COPY_STATUS.md` “method does not exist” is **false** on HEAD.

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
| Method body | `=> false` (L211). `current` unused. |
| `FromBaseline` ceiling | `SHADOW` (L200–201). Never `LIVE` / `LIVE_CANDIDATE`. |
| Quality formula | L152–160: +15 if `NetPnl > 0`, PF bonuses, `+ behavior*0.2`, `- risk*0.25`. **Quality is not dest cash.** |
| Persist | `DealIngestionService` L140 `CurrentState = score.SuggestedState`. `EfTradingStore.UpsertScoreAsync` L232 copies `score.CurrentState`. No product writer sets `LIVE`. |
| Unit | `BaselineScorerTests` L21–26: three disciplined winners → `SHADOW`; `CanPromoteToLive` asserted false. |

Assigned `RiskEngine` does **not** promote. It can `Approve` + `AllowFixSend=true` only when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). That is a send gate, not a state promotion.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL** (disproved)

Assigned hop: `CTraderFixLogonHostedService` is what runs after `CTraderFixSession.TryLogonAsync`. Full read of that host (112 lines).

After both sockets return, the host writes **only** Quote/Trade status fields, then **logs** the existing flag:

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

There is **no** `_runtime.RealCopyEnabled = false` (or `true`) in this file. A comment that NewOrderSingle is unimplemented is **not** a pin.

Workspace `*.cs` grep of `RealCopyEnabled` writes: the **only** assignment is DI bind.

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

API boot loads `D:\Prop\.env` via `EnvFile.FindAndLoad()` (`apps\api\Program.cs` L10; hardcoded candidate `D:\Prop\.env` at `EnvFile.cs` L14). `.env` L73 (flag key only):

```
REAL_COPY_EXECUTION_ENABLED=true
```

`GET /api/settings` and `GET /api/health` would expose `runtime.RealCopyEnabled` (`Program.cs` L55 / L76). This slot **could not** live-GET those bodies (loopback SSRF). File proof is enough to **disprove** “forced false after logon”: there is no such assignment, and the only write binds `true`.

`CREDENTIALS_AND_COPY_STATUS.md` L30 “`REAL_COPY_EXECUTION_ENABLED` **false** (forced)” is **STALE vs HEAD**.

---

## 4. Claim 4 — sending now cannot be the profit path — **FAIL** (unscoped)

### 4.1 Assigned session cannot send

`CTraderFixSession` writes one `35=A` logon and disposes the socket. No NewOrderSingle. Sending **through this class** cannot be dest profit.

### 4.2 Persist / risk hop cannot send LIVE Pepperstone

Assigned `RiskEngine` (189/189):

- Rejects always persist `AllowFixSend = false` (L180–188).
- Approves set `AllowFixSend = request.RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150).
- When `RealExecutionEnabled == false`, the engine **does not reject**; it continues and can `APPROVE` with send false (L90–93 empty block). Unit `Real_flag_false_never_allows_fix_send` matches.

Hosted persist **overrides** the engine:

```317:336:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                var rec = new RiskDecisionRecord
                {
                    Id = Guid.NewGuid(),
                    CopyIntentId = intent.Id,
                    Outcome = decision.Outcome,
                    ApprovedQuantity = decision.ApprovedQuantity,
                    Reason = decision.Reason,
                    AllowFixSend = false,
                    DecidedAt = now
                };
                ...
                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
```

Also `VenueReconciled` is a **const false** (`CopyTradingService` L20). Evaluate is called with `Reconciled = VenueReconciled` (L304), so increasing actions hit `VENUE_NOT_RECONCILED` (RiskEngine L84–85) **before** the approve/send formula. The LIVE branch is dead even when `NewOrderSingleImplemented => DemoDest` is true (L50). `CanPromoteToLive => false` plus `FromBaseline` ceiling `SHADOW` means `CurrentState == LIVE` is not produced by scoring.

### 4.3 Booked dest profit is constructor 0 — not a dest-cash proof

```33:46:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        return new OverviewDto(
            ...
            shadowPnl,
            0,
            0,
            0,
            ...
```

`OverviewDto` field order (`DashboardModels.cs` L15–17): `ShadowPnl`, **`DestinationRealPnl`**, `XauGross`, `XauNet`. Dest real PnL is the first literal `0`. No product writer computes dest mark from `DestFillPrice`. A constructor `0` is **absence of booking**, not a venue statement that dest P&L is zero.

`LiveCopyPage.tsx` (70/70) shows `realCopyArmed`, SHADOW/LIVE counts, `liveSends`, intents, shadow fills, QUOTE/TRADE up/down. It does **not** render dest cash. Empty state L57 is dest **activity** copy, not dest profit:

> “Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position.”

That UI sentence **contradicts** an unscoped “sending now cannot be the profit path” claim.

### 4.4 Why the unscoped claim FAILs

`CopyTradingHostedService` 8s + 20s tick calls `ExecuteDemoCopyAsync` (L28–30). That method:

- Returns 0 if `!DemoDest` (L485–488).
- Else calls `CTraderFixCopyOpen.SendAsync` (L528, L566) **without** `RiskEngine.Evaluate` and **without** reading `_runtime.RealCopyEnabled`.
- Lab `.env` dest identity (public ids only): host `demo-us-eqx-01.p.c-trader.com` (`demo-` prefix), trade sender `demo.pepperstone.5328266` (`demo.` prefix), account `5328266` ≠ `1369850` → `DemoDest` **true** if that file is loaded.
- On-disk ledger `D:\Prop\data\demo_copy_ledger.json`: source `305750` / pos `21250421` → dest pos `237339770`, `DestFillPrice=4390.2`, `DestClosed=false`. That is dest **activity**. It is **not** `DestinationRealPnl`. This slot did not send it.

Unscoped “sending now cannot be the profit path” is **false**: dest `35=D` is a dest P&L path even if the dashboard refuses to mark it. Scoped “sending now is not the booked dest-profit constructor / not live `1369850`” **is** file-proven. Live dest-account PnL body **not** GET-proven → cannot claim dest cash is $0.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **PASS**

Assigned scorer: `FromBaseline` emits `SHADOW` from **source** quality/risk (`BaselineScorer` L200–201). `NetPnl` there is reconstructed **source** XAU (`ComputeFeatures` L66–67 / L111).

Assigned page: `LiveCopyPage` Stat “SHADOW traders” is `status?.shadowTraders` (count). No dest PnL column.

Paper path:

```336:359:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    intent.Status = "SHADOW_ONLY";
                    if (quote is not null && decision.Outcome == RiskDecisionOutcome.Approve)
                    {
                        var fill = _shadow.SimulateEntry(...);
                        _db.ShadowOrders.Add(new ShadowOrder { ... SourceVsShadowSlippage = fill.SourceVsShadowSlippage ... });
                    }
```

`ShadowCopyEngine.SimulateEntry` prices from a `DestinationQuote` in-process; it does not write FIX. Dashboard `ShadowPnl` = `Sum(SourceVsShadowSlippage)` (`EfDashboardQueries` L29). That is source-vs-shadow **slippage**, not dest realized.

Policy residual (does **not** turn SHADOW into dest profit):

- `XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `WATCH` / `EARLY_SCORE` / `INSUFFICIENT_DATA` as `TRADER_NOT_SHADOW_YET` (L81–85). `SHADOW` (and unused LIVE states) can be eligible if 20+ XAU, `XauNetPnl > 0`, no size pattern, **and** `CopyGroupFilter.IsDemoOrContest` (L105–109).
- `CopyRosterEngine` AUTO_ADMIT (L72–80) uses that eligibility. `TickRosterAsync` writes `ADMITTED`. `ExecuteDemoCopyAsync` then dest-sends **ADMITTED** opens (L542–598) without requiring `TraderState.LIVE`.
- So SHADOW-on-demo **can** be the dest **admit floor**. That is dest **hop**, not dest **profit**. Dest DTO remains constructor `0`.

---

## 6. Live GET

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | `web_fetch` SSRF blocked (loopback). `open_page` retrieve failed. |
| `http://localhost:5000/api/health` | `web_fetch` SSRF blocked. |
| `http://127.0.0.1:5000/api/settings` | not independently retrieved this slot (loopback blocked). |
| `http://127.0.0.1:5000/api/copy/status` | not independently retrieved this slot (loopback blocked). |

`apps\api\Properties\launchSettings.json` profile `http` binds `http://localhost:5000`. Runtime `realCopyEnabled` / dest PnL **not** re-probed. Claim 3 does not need the GET: the pin is absent in source. Any claim that “the running process currently shows `realCopyEnabled=false`” is **FAIL** (unproven).

---

## 7. What this slot will not claim

- Did **not** claim EX5 / MT5 decompile parity.
- Did **not** treat quality 95.50 / sibling census / copy-all 8463 integers as this slot’s evidence (not re-measured here).
- Did **not** treat `LiveCopyPage` “Pepperstone cannot be filled” (L24) as a dest-send absence — HEAD demo hopper exists.
- Did **not** treat `GetStatusAsync` `VenueReconciled: DemoDest` (L67) as RiskEngine recon — Evaluate still sees const `false`.
- Did **not** treat dashboard dest `0` as a live dest-account mark.
- Did **not** send `35=D`. Did **not** flip `REAL_COPY`. Did **not** print secrets.

---

## 8. One-line

Session has no NewOrderSingle; promotion is a hard false; the flag is **not** forced off after logon (`.env` true + DI bind); sending now **can** be a demo dest P&L path; booked dest DTO is constructor 0; SHADOW paper is not dest PnL; live `1369850` remains refused.

**End P500_VERIFY_79.** Slot **79**. Verdict **FAIL** (claim 3 disproved; claim 1/4 fail unscoped). Risk to live capital **NONE**; demo dest send **wired**. Product source was not modified.
