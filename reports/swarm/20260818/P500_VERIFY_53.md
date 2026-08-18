# P500_VERIFY_53 — Adversarial profit-path verify (slot 53)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_53.md` |
| Agent / slot | P500 adversarial **verify 53** |
| Date | 2026-08-18 |
| Role | Independent verifier. **Did not trust** sibling P500_BOOK / P500_VERIFY / W500 / CREDENTIALS prose. Re-read assigned files + adjacent send/logon hop this slot. |
| Assigned files | `CTraderFixSession.cs` (135/135), `BaselineScorer.cs` (212/212), `RiskEngine.cs` (189/189), `LiveCopyPage.tsx` (70/70) |
| Product source modified | **No.** This report is the only product-tree write besides INDEX / SWARM_LOG appends. |
| Test source modified | **No.** |
| `.env` modified | **No.** Boolean key + already-public host prefix / account ids only. |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live GET this pass | **Blocked.** `web_fetch` `http://127.0.0.1:5000/api/health` and `http://localhost:5000/api/copy/status` → SSRF private-IP deny. **No** live GET body. Any claim that needs a live process flag is **FAIL**. |
| Live attach / send this pass | **No.** No Manager Connect. No TLS. No Logon. No `35=D`. This slot sent **0**. |

**Honesty rule:** FAIL any claim that cannot be proven from a file this slot or a live GET. Prior swarm prose is not evidence. A TLS Logon `35=A` is not a NewOrderSingle. `SAFE_BY_ABSENCE` on live `1369850` is not “flag stays false.” A demo hopper that can `Build("D")` is not `CTraderFixSession`. Destination constructor `$0` is not a measured dest book. A comment that says “NewOrderSingle still unimplemented” is not a missing builder. Wanting profit is not an edge. Do not print secrets.

---

## 0. Verdict (binding)

**FAIL.** The five-claim bundle is not proven as written. Claim 2 is file-true. Claim 5 is file-true as a **PnL identity** (SHADOW paper ≠ dest $). Claims **1, 3, 4** fail the bar (unscoped / disproven / not proven).

| # | Assigned claim | Verdict | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **FAIL** as written | Assigned `CTraderFixSession` is `35=A` only (**PASS_SESSION**). Unscoped “no builder” is **false**: `Build("D")` ×5 in sibling session files; hosted hop calls `CTraderFixCopyOpen`. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive` in `BaselineScorer.cs` L211 is `=> false`. `FromBaseline` never returns `LIVE` (max `SHADOW`). Unit test asserts SHADOW-not-LIVE. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | DI L41 binds env. Lab `.env` L73 is `true`. Hosted logon **reads** `_runtime.RealCopyEnabled` at L70 and **never assigns false**. The only `RealCopyEnabled =` writer in product `*.cs` is DI L41. Live GET blocked. |
| 4 | sending now cannot be the profit path | **FAIL** | Cannot prove. Hosted `ExecuteDemoCopyAsync` **sends now** on DemoDest (`Build("D")`); on-disk ledger has an open dest fill. Dest DTO constructor `0` is **not** dest-account P&L. Live GET of dest book blocked. Live `1369850` still refused. |
| 5 | SHADOW on demo is not destination profit | **PASS** | `SHADOW` paper path is `SimulateEntry` + Σ `SourceVsShadowSlippage`. That number is **not** dest P&L. Residual: a SHADOW demo source **can** be AUTO_ADMITTED onto the dest hopper (different hop). |

**Overall slot verdict: FAIL** (instruction: FAIL any claim that cannot be proven from a file or live GET).

**Risk to capital:** **NONE on live Pepperstone `1369850`** (`SAFE_BY_ABSENCE` on `CTraderFixSession` + CopyOpen / DemoTestTrade / DemoMatrix refuse). **Not absent on demo dest `5328266`** (hosted `ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.Build("D")`; ledger `305750` / dest pos `237339770` `DestClosed=false`). Flag may be **armed**; that is **not** a live-send license. Do not paper over claim 3.

Stale siblings this slot contradicts: `CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**”; README “Real NewOrderSingle is **off** (`…=false`)”; any BOOK that still pins product `35=D=0` / `NOS=const false` / persist L306 / logon re-pin false.

One-line:

```text
CTraderFixSession is 35=A only. CanPromoteToLive=>false.
RealCopyEnabled is NOT forced false (DI binds .env true; logon host does not write the flag).
Hosted ExecuteDemoCopyAsync -> CTraderFixCopyOpen.Build("D") on demo dest.
SHADOW paper ≠ dest $. Live 1369850 refused. Live GET blocked. Bundle FAIL.
```

Operating mode (honest):

```text
ALLOW:  treat CTraderFixSession as logon-proof only
        treat CanPromoteToLive as a hard no-LIVE from scoring
        treat ShadowOrder / dashboard ShadowPnl as simulated, not dest $
FORBID: claim the process has no 35=D builder
        claim RealCopyEnabled is forced false after logon
        claim "sending now cannot be the profit path"
        treat dest DTO $0 as a measured dest book
        treat live GET as measured this slot
```

---

## 1. Files read (this slot; not memory)

Assigned four, plus the hops required to try to **disprove** the claims.

| Path | Role |
|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | assigned; logon-only; 135/135 |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | assigned; `TraderStateMachine` L211 |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | assigned; `AllowFixSend` formula |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | assigned; UI honesty / demo dest copy |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | dest `35=D` builder; live `1369850` refuse |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | off-hop `Build("D")` ×3; demo-gated |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | off-hop `Build("D")` ×1; demo-gated |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | post-logon flag? **no write** |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | flag storage; mutable getter |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | **only** `RealCopyEnabled =` |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | risk hop + demo dest hop (625) |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 20s tick → `ExecuteDemoCopyAsync` |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | SHADOW eligibility + `AllocationFactor=1m` |
| `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs` | AUTO_ADMIT |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | simulate-only |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | dest $ constructor **0** |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | `ShadowPnl` vs `DestinationRealPnl` |
| `D:\Prop\src\Infrastructure\Copy\DemoCopyLedger.cs` + `D:\Prop\data\demo_copy_ledger.json` | dest fill residual |
| `D:\Prop\reports\swarm\20260818\DEMO_COPY_OPEN.json` | prior dest fill ER (public dest ids) |
| `D:\Prop\apps\api\Program.cs` | `/api/copy/status`, settings bind runtime flag |
| `D:\Prop\apps\web\src\pages\OverviewPage.tsx` | dest P&L card is constructor 0 |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | SHADOW-not-LIVE |
| `D:\Prop\.env` | `REAL_COPY_EXECUTION_ENABLED` boolean + public host/sender/account (password not quoted) |

Grep this pass: `(35,` in `CTraderFixSession` = **1** (`"A"`). `Build("D")` in `src/` = **5**. `CanPromoteToLive` in `src/` = **1**. `RealCopyEnabled =` in `*.cs` = **1** (DI L41).

---

## 2. Claim 1 — “no 35=D builder” — FAIL as written (PASS_SESSION only)

### 2.1 Assigned session file (full read, 135/135)

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`

Public API is **only** `TryLogonAsync`. The sole outbound builder:

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

| Fact | Measured this pass |
|---|---|
| Physical lines | **135 / 135** (ends L135 `}`) |
| Literal `35=D` / `(35, "D")` / `Build("D")` | **0** |
| `NewOrderSingle` identifier | **0** |
| Outbound tag 35 actually built | **`"A"` only** (L96) |
| `ssl.WriteAsync` | **1** (L49) — bytes of that Logon |
| `Extract(..., "35")` | **1** — **inbound** reply only (L55) |
| Socket kept for a later `35=D` | **No** — `using TcpClient` / `await using SslStream` dispose on every return |
| Generic `Assemble` callers in this file | **1** — `BuildLogon` |

A one-shot Logon probe is **not** a NewOrderSingle builder. **Session-scoped** “`CTraderFixSession` has no `35=D` builder” is proven. The assigned claim text is **unscoped**.

`CTraderFixLogonHostedService` is the only product caller of `CTraderFixSession.TryLogonAsync` (QUOTE 5211 then TRADE 5212). After return, the socket is gone. Log line L69 says `NewOrderSingle still unimplemented` — that is a **comment on this session object**, not a census of the assembly.

### 2.2 Process / product — FAIL

Same folder, same assembly, three other builders emit MsgType `D`:

| File | Evidence |
|---|---|
| `CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra.ToArray())` — **hosted** dest hop |
| `CTraderFixDemoTestTrade.cs` L139, L163, L197 | `Build("D", …)` ×3 — tools/CLI |
| `CTraderFixDemoMatrix.cs` L93 | `Build("D", sender, target, seq++, extra)` — tools/CLI |

`CTraderFixCopyOpen.Build` is generic:

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

Hosted wiring (not a comment):

```30:30:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

```566:569:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                var fill = await CTraderFixCopyOpen.SendAsync(
                    host, sender, target, account, password,
                    seat.SourceLogin.ToString(), trade.PositionId.ToString(),
                    trade.Direction == TradeDirection.Long, trade.MaxVolumeLots, ct);
```

Live identity refuse on the dest hop (does **not** delete the builder):

```37:42:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
        if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
            || !sender.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
            || account == "1369850")
        {
            return Fail("Refused: dest must be demo FIX, not live 1369850.", host, account, sourceLogin);
        }
```

**Claim 1 as written is FAIL.** PASS only if silently scoped to `CTraderFixSession.cs`. This slot will not do that.

---

## 3. Claim 2 — `CanPromoteToLive` is false — PASS

Full read of `BaselineScorer.cs` (212/212). The only definition in `src/`:

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

`FromBaseline` (L189–206) returns only `INSUFFICIENT_DATA` / `RISK_BLOCKED` / `SHADOW` / `WATCH` / `EARLY_SCORE`. It never returns `LIVE` or `LIVE_CANDIDATE`. Highest reachable state from scoring is `SHADOW` (`quality >= 70 && risk < 40`).

Unit file `tests\Unit\BaselineScorerTests.cs` L22–26: three disciplined winners → `SuggestedState == SHADOW` and `CanPromoteToLive(...).Should().BeFalse()`.

`TraderState` enum still **contains** `LIVE_CANDIDATE=4` and `LIVE=5` (`src\Domain\Enums\TraderState.cs`). Those values exist. Scoring will not emit them. Copy hop still **lists** `LIVE` as copyable (`CopyTradingService` L202). That does not make `CanPromoteToLive` true.

**Claim 2 PASS.** Hardcoded `=> false`. Argument `current` is unused.

---

## 4. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL

### 4.1 Logon host does not write the flag

`CTraderFixLogonHostedService.ExecuteAsync` after both `TryLogonAsync` returns:

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

Assignments: Quote/Trade status only. `_runtime.RealCopyEnabled` is **logged**, not set. There is no `_runtime.RealCopyEnabled = false` in this file. PersistAsync writes `FixSessionState` rows, not the runtime flag.

### 4.2 The only writer binds `.env`

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Grep `RealCopyEnabled =` over product `*.cs`: **one** hit, that line. `LiveRuntimeStatus.RealCopyEnabled` is `{ get; set; }` (mutable). Nothing after logon forces it false.

Lab `.env` L73 (boolean only): `REAL_COPY_EXECUTION_ENABLED=true`.

Public dest identity this slot (not secrets):

| Key | Value |
|---|---|
| `CTRADER_FIX_HOST` L49 | `demo-us-eqx-01.p.c-trader.com` (starts `demo-`) |
| `CTRADER_FIX_ACCOUNT_ID` L50 | `5328266` (not `1369850`) |
| `CTRADER_FIX_TRADE_SENDER_COMP_ID` L64 | `demo.pepperstone.5328266` (starts `demo.`) |

Therefore `CopyTradingService.DemoDest` is **true** and `NewOrderSingleImplemented => DemoDest` is **true**. Slots that still pin `NOS=const false` are **STALE**.

API `Program.cs` L76 exposes `featureFlags.REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled` (env-bound, not a literal false). Live GET of that endpoint was **blocked** this slot, so the **process** bit is not remeasured. File proof is enough to **FAIL** “forced false after logon.”

`CREDENTIALS_AND_COPY_STATUS.md` “forced false” is **STALE**. README “Real NewOrderSingle is off (`…=false`)” is **STALE** as an env fact.

**Claim 3 FAIL / disproven.**

---

## 5. Claim 4 — “sending now cannot be the profit path” — FAIL

Two hops exist. Conflating them is how stale BOOK slots stay green.

### 5.1 Risk / shadow hop cannot send (file-true, not the whole product)

`CopyTradingService.GenerateShadowIntentsAsync`:

- `VenueReconciled` **const false** (L20) is passed as `Reconciled` (L304).
- Persist **hardcodes** `AllowFixSend = false` (L324), ignoring `decision.AllowFixSend`.
- Live-send branch (L330) also requires `score.CurrentState == LIVE` (unreachable from scoring) **and** `VenueReconciled` (const false). Dead.

`RiskEngine.Evaluate` allow formula:

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

The empty `if (RealExecutionEnabled == false)` block at L90–93 is a **comment**, not a force-false. If a caller passed `Reconciled=true` and `RealExecutionEnabled=true`, `AllowFixSend` would be **true**. Persist currently lies on top of that. That is not “sending cannot be the profit path.” That is “this one persist line currently drops the bit.”

`GetStatusAsync` **reports** `VenueReconciled: DemoDest` (L67) while the const used by Evaluate is **false**. UI/status lie. Not a send proof.

### 5.2 Demo dest hop **sends now** and **can** book dest P&L

`CopyTradingHostedService` every 20s: `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**.

`ExecuteDemoCopyAsync` (L483–605):

- Skips only if `!DemoDest` (live host / live sender / account `1369850`).
- Does **not** read `RealCopyEnabled`.
- Does **not** call `RiskEngine.Evaluate`.
- Does **not** read persist `AllowFixSend`.
- Calls `CTraderFixCopyOpen.SendAsync` which `Build("D")`s.
- On fill, writes dest pos/px onto the intent (`Status = "DEMO_SENT"`) and `DemoCopyLedger`.

On-disk ledger `D:\Prop\data\demo_copy_ledger.json` (public dest ids only):

| Field | Value |
|---|---|
| SourceLogin | `305750` |
| SourcePositionId | `21250421` |
| Lots | `0.01` |
| DestPositionId | `237339770` |
| DestClOrdId | `C20260818093047317` |
| DestFillPrice | `4390.2` |
| DestClosed | **false** |

`ExecuteDemoCopyAsync` L500–512 will **re-seed** that same 305750/21250421 row if missing. Prior ER dump `DEMO_COPY_OPEN.json`: `OrderSent=true`, `Filled=true`, account `5328266`, host `demo-us-eqx-01…`, inbound `35=8` / `150=F` / `39=2`. That is dest inventory, not paper.

`LiveCopyPage.tsx` L57 (assigned file) tells the operator the same thing:

> “No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.”

Dashboard `DestinationRealPnl` is hardcoded **0**:

```43:46:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            shadowPnl,
            0,
            0,
            0,
```

`OverviewDto` field order: `ShadowPnl`, **`DestinationRealPnl`**, `XauGross`, `XauNet`. Overview card “Dest. real P&L” therefore always prints `0.00`. That constructor is **not** a mark of the demo dest account. Treating it as “sending cannot be dest profit” is a **lie**.

Live `1369850` is refused. That proves **live** send is not the profit path. The assigned claim is unscoped (“sending now”). Demo dest sending **is** a dest P&L path.

**Claim 4 FAIL.** Cannot prove “sending now cannot be the profit path” from files. The files prove the opposite on demo dest. Live GET of dest P&L blocked → dest $ unmeasured, not proven zero.

---

## 6. Claim 5 — “SHADOW on demo is not destination profit” — PASS (identity)

### 6.1 SHADOW paper book is not dest $

`ShadowCopyEngine.SimulateEntry` (L35–61) computes a modeled fill from a `DestinationQuote` + 0.05-point latency slip. No socket. No tag 35.

`GenerateShadowIntentsAsync` writes `ShadowOrder` from that fill (L339–359). Persist `PersistDemoShadowAsync` is the same simulate path.

Dashboard `ShadowPnl`:

```29:29:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var shadowPnl = await _db.ShadowOrders.SumAsync(s => (decimal?)s.SourceVsShadowSlippage, ct) ?? 0;
```

That is Σ source-vs-shadow **slippage**, not dest-account realized P&L. `DestinationRealPnl` is a different field and is constructor `0` (see §5.2).

Scoring `SHADOW` is a **source** `TraderState` (`FromBaseline` L200–201). It is not a dest fill.

**Claim 5 PASS** as: the SHADOW book on a demo source is not destination profit.

### 6.2 Residual (does not flip claim 5)

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET` (L81–85). SHADOW is the **floor** for AUTO_ADMIT.

`CopyRosterEngine.Decide` admits when `IsTraderEligible` (L72–80, reason `AUTO_ADMIT`). `TickRosterAsync` writes `Status = "ADMITTED"`. `ExecuteDemoCopyAsync` then dest-sends **ADMITTED** seats with open XAU ≤ `MaxAutoLots` (0.05), **bypassing** the SHADOW paper book.

So: SHADOW paper ≠ dest $. SHADOW-eligible demo **sources** can still sit on the dest hopper. That residual belongs to claim 4, not a FAIL of the identity.

Policy also **requires** demo/contest group (`NOT_DEMO_OR_CONTEST_GROUP`) and `XauNetPnl > 0`. HEAD admits demo; it does not treat SHADOW slippage as dest cash.

---

## 7. Live GET

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | **SSRF blocked** (private IP) |
| `http://localhost:5000/api/copy/status` | **SSRF blocked** (private IP) |

`apps\api\Properties\launchSettings.json` profile `http` binds `http://localhost:5000`. No process body this slot. Runtime `realCopyEnabled` / `realCopyArmed` / dest mark **not** remeasured. File proof already FAILs claims 1/3/4.

---

## 8. Risk to capital (measured, not hoped)

| Book | Exposure this slot |
|---|---|
| Live Pepperstone `1369850` | **NONE.** `CTraderFixSession` cannot send. CopyOpen / DemoTestTrade / DemoMatrix all refuse `account == "1369850"` (and live host/sender prefixes). `SAFE_BY_ABSENCE`. |
| Demo dest `5328266` | **Not absent.** Hosted 20s hopper → `Build("D")`. Ledger open 0.01 lot dest pos `237339770` (`DestClosed=false`). Prior ER filled. Dest DTO `$0` is **not** a hedge. |
| SHADOW paper | Simulated slippage only. Not dest cash. |
| This slot | Sent **0**. Did not flip flags. Did not print secrets. |

`AllocationFactor = 1m` (1:1) on the policy used by shadow intents. Hopper sends `trade.MaxVolumeLots` 1:1 up to `MaxAutoLots=0.05`. That is dest size on **demo**, not live.

Wanting profit is not an edge. Copy-all of RISK_BLOCKED books is not this slot’s census (not re-summed). Do not invent dest EV.

---

## 9. What would have to be true for a PASS bundle

1. Either delete / unwire every `Build("D")` **or** rephrase claim 1 as “`CTraderFixSession` is logon-only.”
2. `CanPromoteToLive => false` — already true.
3. An actual assignment `_runtime.RealCopyEnabled = false` after logon, **or** DI no longer binds `.env=true`. A log line is not a pin. Live GET would then have to show the process bit false.
4. `ExecuteDemoCopyAsync` gone or gated so it cannot `35=D` — **and** dest ledger flat — **and** a live GET dest mark of 0, not a constructor.
5. Keep SHADOW as simulate-only (already true). Do not pretend AUTO_ADMIT cannot dest-send.

Until 1, 3, and 4 are file-true, this bundle stays **FAIL**.

---

## 10. Slot 53 close

| Item | Value |
|---|---|
| Verdict | **FAIL** |
| Claim 1 | **FAIL** unscoped / **PASS_SESSION** |
| Claim 2 | **PASS** |
| Claim 3 | **FAIL** |
| Claim 4 | **FAIL** |
| Claim 5 | **PASS** (SHADOW paper ≠ dest $) |
| Live GET | **blocked** — not used as proof |
| Secrets | **none** |
| Product / test edits | **none** |
| `35=D` sent this slot | **0** |
