# P500_VERIFY_65 — Adversarial profit-path verify (slot 65)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_65.md` |
| Agent / slot | P500 adversarial **verify 65** |
| Date | 2026-08-18 |
| Role | Independent verifier. **Did not trust** sibling P500_BOOK / W500 / CREDENTIALS / INDEX prose. Re-read assigned files + adjacent send/logon hop this pass. |
| Assigned files | `CTraderFixSession.cs` (135/135), `BaselineScorer.cs` (212/212), `RiskEngine.cs` (189/189), `LiveCopyPage.tsx` (70/70) |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Boolean keys + already-public host prefix / account ids only. |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live GET this pass | **Blocked.** `web_fetch` `http://127.0.0.1:5000/api/copy/status` → SSRF private-IP deny. **No** live GET body. Any claim that needs a live runtime body is **FAIL**. |
| Live attach / send this pass | **No.** No Manager Connect. No TLS. No Logon. No `35=D`. This slot sent **0**. |
| Method | Full `read_file` of the four assigned files. Adjacent this pass: `CTraderFixLogonHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs` (`SendD`), `CopyTradingService.cs` (625), `CopyTradingHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `apps/api/Program.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `CopyGroupFilter.cs`, `DemoCopyLedger.cs`, `data/demo_copy_ledger.json` (public dest ids only), `BaselineScorerTests.cs`, `TraderState.cs`, `hooks.ts`, `launchSettings.json` (API `:5000`). Grep: `Build("D")` / `(35,` / `NewOrderSingle` / `RealCopyEnabled` / `CanPromoteToLive` / `DestinationRealPnl`. Flag-only `.env` L49/L50/L64/L73. |

**Honesty rule:** FAIL any claim that cannot be proven from a file this slot or a live GET. Prior swarm prose is not evidence. A TLS Logon `35=A` is not a NewOrderSingle. `SAFE_BY_ABSENCE` on live `1369850` is not “flag stays false.” A demo hopper that can `Build("D")` is not `CTraderFixSession`. Destination constructor `$0` is not a measured dest book. Wanting profit is not an edge. Do not print secrets.

---

## 0. Verdict (binding)

**FAIL.** Claim 2 is file-proven. Claim 5 is file-proven only as **paper SHADOW ≠ dest cash**. Claims **1, 3, 4** fail the bar (unscoped / disproven / not proven). Claim 5 residual: SHADOW is dest AUTO_ADMIT floor.

| # | Assigned claim | Verdict | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **FAIL** as written | Assigned `CTraderFixSession` is `35=A` only (**PASS_SESSION**). Unscoped “no builder” is **false**: `Build("D")` ×5 in sibling session files; hosted hop calls `CTraderFixCopyOpen`. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive` in `BaselineScorer.cs` L211 is `=> false`. `FromBaseline` never returns `LIVE` (max `SHADOW`). Unit test asserts SHADOW-not-LIVE. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | DI L41 binds env. Lab `.env` L73 is `true`. Hosted logon **reads** `_runtime.RealCopyEnabled` at L70 and **never assigns false**. Product `*.cs` has **one** writer: DI L41. Live GET blocked. |
| 4 | sending now cannot be the profit path | **FAIL** | Cannot prove. Hosted `ExecuteDemoCopyAsync` **sends now** on DemoDest (`Build("D")`); ledger has an open dest fill. Dest DTO constructor `0` is **not** dest-account P&L. Live GET of dest book blocked. Live `1369850` still refused. |
| 5 | SHADOW on demo is not destination profit | **PASS_PAPER** | `SHADOW` paper path is `SimulateEntry` + Σ slippage. That number is **not** dest P&L. Residual: a SHADOW demo source **can** be AUTO_ADMITTED onto the hopper (different hop). Unscoped “SHADOW cannot produce dest P&L” is **not** proven. |

**Overall slot verdict: FAIL** (instruction: FAIL any claim that cannot be proven from a file or live GET).

**Risk to capital:** **NONE on live Pepperstone `1369850`** (`SAFE_BY_ABSENCE` on `CTraderFixSession` + CopyOpen refuse). **Not absent on demo dest `5328266`** (hosted `ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.Build("D")`). Flag may be **armed**; that is **not** a live-send license. Do not paper over claim 3.

Stale siblings this slot contradicts: any pin that `REAL_COPY_EXECUTION_ENABLED` is “false (forced)” after logon; any BOOK that still pins product `35=D=0` / `NOS=const false` / persist L306 / logon re-pin false.

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

Live identity gate on CopyOpen (`account == "1369850"` / host not `demo-` / sender not `demo.`) **refuses** live dest. Lab `.env` **is** DemoDest: host `demo-us-eqx-01.p.c-trader.com` (L49), sender `demo.pepperstone.5328266` (L64), account `5328266` (L50) ≠ `1369850`.

The claim as written is **not** “`CTraderFixSession` has no `35=D` builder.” It is “no `35=D` builder.” That is **false** on this tree. Hosted `CopyTradingHostedService` L30 calls `ExecuteDemoCopyAsync`, which calls `CTraderFixCopyOpen.SendAsync` (L528 / L566). Lab `.env` **is** DemoDest.

`LiveCopyPage.tsx` has **0** FIX builders. It only renders `/api/copy/status` + `/api/copy/intents`. Empty-state L57 honestly says demo dest auto-sends after ADMITTED — UI copy, not an encoder, and evidence the product **does** send.

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
| Enum still has `LIVE=5` / `LIVE_CANDIDATE=4` | Yes (`TraderState.cs`) — unused by this machine |

`LiveCopyPage.tsx` shows `liveTraders` from API; it does **not** promote. Claim 2 **PASS**.

---

## 3. `RealCopyEnabled` forced false after logon — FAIL

**None of the four assigned files force this flag false.**

| Assigned file | `RealCopyEnabled` |
|---|---|
| `CTraderFixSession.cs` | **0** mentions. Logon does not touch runtime. |
| `BaselineScorer.cs` | **0** |
| `RiskEngine.cs` | Consumes `request.RealExecutionEnabled` as an **input**. L147–150 `allowSend = RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. Does not write runtime. L90–93 empty shadow comment when `RealExecutionEnabled==false`. |
| `LiveCopyPage.tsx` | **Displays** `status?.realCopyArmed` (`YES`/`NO`, amber when true). No setter. |

### 3.1 What logon actually does

`CTraderFixLogonHostedService.ExecuteAsync` (full read this pass):

- Calls `CTraderFixSession.TryLogonAsync` twice (QUOTE 5211, TRADE 5212).
- Writes `_runtime.Quote.*` and `_runtime.Trade.*` (LoggedOn / Status / LastError / UpdatedAt).
- Logs `RealCopyArmed={Armed}` from **current** `_runtime.RealCopyEnabled` (L68–70).
- **Zero assignments** to `_runtime.RealCopyEnabled`.
- Then persists FIX session rows. Returns.

Grep `RealCopyEnabled =` on `*.cs` this pass = **1 hit**:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Lab `.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret). DI binds that to `true` at process start. Logon does **not** re-pin false.

`LiveRuntimeStatus.Snapshot()` L42–44 even advertises “REAL_COPY armed…” when the flag is true. That copyNote still claims “NewOrderSingle still unimplemented” — **stale vs** `CopyTradingService.NewOrderSingleImplemented => DemoDest` (L50). A stale note is not a force-false.

Live GET of `/api/health` / `/api/settings` / `/api/copy/status` **blocked** this pass. Cannot prove the in-process bool from a live body. File proof is enough to **disprove** “forced false after logon.”

Claim 3 **FAIL**.

---

## 4. sending now cannot be the profit path — FAIL

### 4.1 What the assigned files prove (not enough)

`RiskEngine.Evaluate` can set `AllowFixSend = true` when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). That is a **gate**, not a sender. Persist hop in `CopyTradingService` **overwrites**:

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
                // ...
                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
```

`VenueReconciled` is `const false` (L20). The Evaluate/persist hop therefore **cannot** emit a live NewOrderSingle even if the flag is armed.

That does **not** prove “sending now cannot be the profit path.” A second hop exists.

### 4.2 Hosted demo send hop (file-proven)

`CopyTradingHostedService` 20s tick (L21–41): `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**.

`ExecuteDemoCopyAsync` (L483–605):

- Returns 0 if `!DemoDest` (host not `demo-` / sender not `demo.` / account `1369850`).
- Lab `.env` **is** DemoDest (L49/L50/L64).
- Does **not** call `RiskEngine.Evaluate`.
- Does **not** read `AllowFixSend`.
- Calls `CTraderFixCopyOpen.SendAsync` for dest close (L528) and dest open (L566).
- CopyOpen L95 writes `Build("D", …)` after a fresh TRADE `35=A`.
- Max 5 sends per tick; `MaxAutoLots = 0.05m`.
- Marks intent `DEMO_SENT` on fill.

`GetStatusAsync` L76–77: when DemoDest, summary is **“Demo dest auto-copy ON. Eligible demo/contest opens send on the 20s tick…”**

`LiveCopyPage.tsx` L57 empty-state: *“Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position.”*

On-disk ledger `D:\Prop\data\demo_copy_ledger.json` (this pass, public dest ids only):

| Field | Value |
|---|---|
| Source | `305750` / `21250421` |
| Dest position | `237339770` |
| Dest ClOrdId | `C20260818093047317` |
| Dest fill px | `4390.2` |
| `DestClosed` | **false** (still open on ledger) |

`ExecuteDemoCopyAsync` L500–512 **re-injects** that same 305750/21250421 row if missing. Adjacent artifact `DEMO_COPY_OPEN.json` records a prior fill (`Allowed/LoggedOn/OrderSent/Filled=true`, dest account `5328266`). That is **prior** send evidence, not this slot. This slot sent **0**.

`OverviewDto.DestinationRealPnl` is constructor literal **`0`** (`EfDashboardQueries.cs` L44). That is **not** a mark of dest-account P&L. A `$0` DTO cannot prove dest is flat.

Live GET of dest book / copy status **blocked**. Cannot prove dest cash P&L this pass. **Can** prove sending **can** be a dest-P&L path on demo.

Live `1369850` remains refused by CopyOpen L37–41. Sending to **live** Pepperstone is still `SAFE_BY_ABSENCE`. The assigned claim is unscoped (“the profit path”), not “live 1369850.”

Claim 4 **FAIL**.

---

## 5. SHADOW on demo is not destination profit — PASS_PAPER (residual ADMIT)

### 5.1 Paper path (proven)

`ShadowCopyEngine.SimulateEntry` / `SimulateExit` compute a modeled fill vs quote bid/ask + 0.05 latency slip. No socket. No tag 35.

`GenerateShadowIntentsAsync` L336–359: non-LIVE hop sets `intent.Status = "SHADOW_ONLY"` and, on Approve + quote, persists a `ShadowOrder` from `SimulateEntry`.

`EfDashboardQueries.GetOverviewAsync` L29: `shadowPnl = Sum(SourceVsShadowSlippage)`. L44: `DestinationRealPnl = 0` (constructor).

`LiveCopyPage.tsx` shows `shadowTraders` / `shadowFills` as **counts**. It does not book dest cash.

Paper SHADOW is **not** destination profit. **PASS_PAPER**.

### 5.2 Residual the unscoped claim cannot hide

`XauUsdOneToOneCopyPolicy.IsTraderEligible` L81–85 rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` with `TRADER_NOT_SHADOW_YET`. SHADOW (and `LIVE_CANDIDATE` / `LIVE`) is the **ADMIT floor**.

`CopyRosterEngine.Decide` L72–80: if eligible → `AUTO_ADMIT` / `KEEP`. `TickRosterAsync` L140–163 writes `Status = "ADMITTED"` on `roster:{broker}:{login}`.

`ExecuteDemoCopyAsync` L542–569: loads `ADMITTED` roster seats and can `Build("D")` on open XAUUSD ≤ 0.05 lots. That dest hop does **not** read `ShadowOrders`.

So: SHADOW **paper PnL** ≠ dest cash (**proven**). SHADOW **state** can be dest AUTO_ADMIT (**file-proven residual**). Unscoped “SHADOW on demo cannot be dest profit” is **not** proven. Instruction is FAIL-unproven: residual recorded; paper half **PASS**.

`CopyGroupFilter` requires a `demo` / `contest` group segment. HEAD **admits** demo/contest and **rejects** real groups. That is a source-group filter, not a dest-profit proof.

Claim 5 **PASS_PAPER**. Residual: dest ADMIT floor.

---

## 6. Live GET

| Attempt | Result |
|---|---|
| `GET http://127.0.0.1:5000/api/copy/status` | **SSRF blocked** (private IP). No body. |
| Other `:5000` health/settings/overview | Not retried after first deny. **No** live GET evidence. |

`launchSettings.json` profiles bind API `http://localhost:5000`. That is a file, not a live body. Claims that need a live `realCopyArmed` / dest book **FAIL**.

---

## 7. Risk to capital (this slot)

| Book | State |
|---|---|
| Live Pepperstone `1369850` | **NONE** — `CTraderFixSession` cannot send `35=D`; CopyOpen / DemoTestTrade / DemoMatrix **refuse** account `1369850`. `SAFE_BY_ABSENCE`. |
| Demo dest `5328266` | **Not absent.** Hosted 20s hop can `Build("D")`. Ledger row 305750/21250421 dest `237339770` is **open** (`DestClosed=false`). This slot did not send. |
| Flag | `.env` L73 `true` + DI bind. **Not** forced false after logon. Flag ≠ live-send license (`VenueReconciled=const false`; persist `AllowFixSend=false`; live account refused). |
| This slot | **0** `35=D`. **0** product edits. **0** secrets printed. |

Wanting profit is not an edge. Copy-all of a RISK_BLOCKED tail is dest-ruin **if** sent 1:1; that is adjacent book math, not re-measured here. Dest DTO `$0` is absence of a mark, not a measured flat book.

---

## 8. Assigned-file checksum (honesty)

| File | Lines | `35=D` builder | `CanPromoteToLive` | `RealCopyEnabled` write | Dest send |
|---|---|---|---|---|---|
| `CTraderFixSession.cs` | 135 | **No** (`35=A` only) | n/a | n/a | **No** |
| `BaselineScorer.cs` | 212 | n/a | **`=> false` L211** | n/a | n/a |
| `RiskEngine.cs` | 189 | n/a | n/a | **No** (reads `RealExecutionEnabled`) | Gate only; persist hop zeros send |
| `LiveCopyPage.tsx` | 70 | **No** | n/a | Display only | Empty-state admits demo auto-send |

---

**DONE.** Slot 65 overall **FAIL**. Live `1369850` **NONE**. Demo dest hop **wired**. This slot sent **0**.
