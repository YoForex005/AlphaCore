# P500_VERIFY_45 — Adversarial verifier (slot 45)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_45.md` |
| Agent / slot | P500 adversarial verifier **45** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned reads (this pass, full file) | `CTraderFixSession.cs` **135/135**; `BaselineScorer.cs` **212/212**; `RiskEngine.cs` **189/189**; `LiveCopyPage.tsx` **70/70** |
| Adjacent (this pass; not assigned) | `CTraderFixLogonHostedService.cs`; `DependencyInjection.cs` L39–42; `CopyTradingService.cs`; `CopyTradingHostedService.cs`; `CTraderFixCopyOpen.cs`; `CTraderFixDemoTestTrade.cs`; `CTraderFixDemoMatrix.cs`; `XauUsdOneToOneCopyPolicy.cs`; `CopyRosterEngine.cs`; `CopyGroupFilter.cs`; `ShadowCopyEngine.cs`; `DealIngestionService.cs` L127–144; `EfTradingStore.cs` L216–232 / PersistDemoShadow; `EfDashboardQueries.cs` L33–52; `LiveRuntimeStatus.cs`; `apps/api/Program.cs` `/api/health` + `/api/settings` + `/api/copy/status`; `.env` public keys only (`REAL_COPY_EXECUTION_ENABLED=true` L73; demo host / account `5328266` / `demo.` sender prefixes); `D:\Prop\data\demo_copy_ledger.json` |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Secrets printed | **None.** No tag 554, no manager/FIX/DB passwords. Quoted only already-public demo host prefix, gate account ids (`5328266` ≠ `1369850`), and the boolean `REAL_COPY_EXECUTION_ENABLED=true`. |
| Live GET this pass | **Blocked.** `web_fetch` `http://127.0.0.1:5000/api/health` and `http://localhost:5080/api/health` → SSRF reject on loopback. Runtime RAM (`realCopyEnabled`, quote/trade logon, dest PnL, intent rows) is **not** proven this slot. |
| Live `35=D` sent this slot | **No** (report-only). |
| `REAL_COPY` flipped this slot | **No.** |
| Method | Independent `read_file` of the four assigned files. Targeted `grep` on product `*.cs`/`*.tsx`. Prior swarm text treated as **untrusted**. **FAIL** any claim not proven from a file or a live GET. |

**Honesty rule:** a logon (`35=A`) is not a fill. `CanPromoteToLive => false` is not a send interlock. Env `REAL_COPY_EXECUTION_ENABLED=true` is not a send license. A paper `ShadowOrder` is not dest cash. A constructor `DestinationRealPnl=0` is not a measured dest book. A sibling `Build("D")` on the 20 s tick **is** a dest path. Wanting profit is not an edge. Copy-all would copy `RISK_BLOCKED` losses.

Assigned claims:

1. no `35=D` builder
2. `CanPromoteToLive` is false
3. `RealCopyEnabled` forced false after logon
4. sending now cannot be the profit path
5. SHADOW on demo is not destination profit

---

## 0. Verdict (binding)

**FAIL.** Two claims hold on the assigned files. Three do not hold as written. Slot verdict is FAIL.

| # | Assigned claim | Result | Why |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS_FILE / FAIL_UNSCOPED** | Assigned `CTraderFixSession.cs` outbound tag 35 is `"A"` only. Process-wide there **is** a `35=D` builder (`CTraderFixCopyOpen.Build("D")` L95; also `CTraderFixDemoTestTrade` ×3 and `CTraderFixDemoMatrix`). Unscoped “no builder” is **false**. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(TraderState current) => false` at `BaselineScorer.cs` L211. `FromBaseline` reachable set never includes `LIVE`. Persist copies `SuggestedState`. **Does not** block demo dest send. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | Hosted logon **never assigns** `_runtime.RealCopyEnabled`. The **only** product assignment is DI from config. `.env` L73 is `true`. Live GET of `/api/health` **not obtained**. Claim is **disproven** from files; RAM value unproven. |
| 4 | sending now cannot be the profit path | **FAIL** | Hosted 20 s tick calls `ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.SendAsync` → `Build("D")`. Bypasses `RiskEngine.Evaluate`. Lab `.env` satisfies `DemoDest`. Ledger already holds dest fill `305750` / dest pos `237339770` / px `4390.2`. Live `1369850` refused. Dashboard dest PnL is constructor `0`, not a measurement. |
| 5 | SHADOW on demo is not destination profit | **FAIL** | Paper `ShadowCopyEngine.SimulateEntry` is not dest cash (that hop **PASS**). Unscoped claim **FAIL**: `SHADOW` is AUTO_ADMIT-eligible; `ExecuteDemoCopyAsync` sends on `ADMITTED` and **does not** require `LIVE`. `LiveCopyPage.tsx` L57 tells the operator dest auto-sends after ADMIT. |

One-line:

```text
FAIL. CTraderFixSession is 35=A only (PASS_FILE). CanPromoteToLive=>false (PASS). RealCopyEnabled is NOT forced false after logon (FAIL). Hosted demo hopper CAN 35=D (FAIL as “sending cannot be profit”). SHADOW can ADMIT and dest-send (FAIL). Live 1369850 refused. Live GET blocked. This slot sent 0.
```

---

## 1. Claim 1 — no `35=D` builder

### 1.1 Assigned file — PASS

Path: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`  
Read: **135 / 135**.

Token census **this file only** (`grep` + full read):

| Pattern | Hits |
|---|---:|
| Literal `35=D` | **0** |
| `NewOrderSingle` | **0** |
| `(35, "D")` / `Build("D")` | **0** |
| `(35, "A")` | **1** (`BuildLogon` L96) |
| `ssl.WriteAsync` | **1** (L49) — bytes of that Logon |
| `Extract(..., "35")` | **1** — **inbound** reply (L55) |

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

`Assemble` (L112–120) is generic. Its only caller in this file is `BuildLogon`. One `WriteAsync` (L49), then sockets dispose (`using TcpClient` / `await using SslStream`). This is a one-shot Logon probe, not a standing TRADE session.

`CTraderFixLogonHostedService` is the only product caller of `TryLogonAsync` (QUOTE **5211**, TRADE **5212**). After the reply it writes session rows. It does not keep the socket and does not send an order.

**File-scoped claim 1: PASS.**

### 1.2 Process-wide — FAIL if the claim is unscoped

Sibling builders (not the assigned file; still product `src`):

| File | Encoder | Hosted? |
|---|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 / L142–156 | `Build("D", ...)` then `Write` | **Yes** — `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566 via `CopyTradingHostedService` L30 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | `Build("D", ...)` | CLI `tools/DemoFixTestTrade` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` L93 | `Build("D", ...)` | test/matrix tool |

`CTraderFixCopyOpen.Build` is a real NewOrderSingle assembler (tag 35 = caller `type`; open/close uses `"D"`; tag 38 qty; tag 11 ClOrdID; optional 721 dest pos):

```142:156:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
    private static string Build(string type, string sender, string target, int seq, params (int, string)[] extra)
    {
        var fields = new List<(int, string)>
        {
            (35, type), (34, seq.ToString(CultureInfo.InvariantCulture)),
            (49, sender), (56, target), (50, "TRADE"), (57, "TRADE"),
            (52, DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture))
        };
        fields.AddRange(extra);
        // FIX.4.4 + checksum
    }
```

Call site that writes NewOrderSingle:

```83:95:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
            var closing = !string.IsNullOrWhiteSpace(destPositionId);
            // ...
            await Write(ssl, Build("D", sender, target, seq, extra.ToArray()), timeout.Token);
```

Refuse gate on that sender (live identity, not a missing builder):

```37:42:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
        if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
            || !sender.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
            || account == "1369850")
        {
            return Fail("Refused: dest must be demo FIX, not live 1369850.", host, account, sourceLogin);
        }
```

Unscoped “no `35=D` builder” is **false**. The assigned session file is not the process.

**Claim 1 overall: PASS_FILE / FAIL_UNSCOPED.**

---

## 2. Claim 2 — `CanPromoteToLive` is false

Path: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`  
Read: **212 / 212**.

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

`FromBaseline` reachable set (L189–207) is only:

| Condition | State |
|---|---|
| `CompletedXauTrades == 0` | `INSUFFICIENT_DATA` |
| `risk >= 80` **or** (martingale ∧ DD > 0 ∧ net < 0) | `RISK_BLOCKED` |
| `!earlyEligible` | `INSUFFICIENT_DATA` |
| `quality >= 70 && risk < 40` | `SHADOW` |
| `quality >= 55` | `WATCH` |
| else | `EARLY_SCORE` |

`LIVE` and `LIVE_CANDIDATE` exist on the enum (`TraderState.cs` L9–10) but are **not** produced by `FromBaseline`. Persist copies the scorer output:

```127:140:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore
        {
            // ...
            CurrentState = score.SuggestedState,
```

`EfTradingStore.UpsertScoreAsync` L232 writes `existing.CurrentState = score.CurrentState`. No other product writer promotes to `LIVE`.

`CanPromoteToLive` has **no product callers** (unit test + a tmp harness only). It is a vacuous lock. It is **not** consulted by `ExecuteDemoCopyAsync`.

**Claim 2: PASS.** Does not prove “cannot send.”

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon

**FAIL. Disproven from files. RAM unproven (live GET blocked).**

Product assignment census (`grep` `RealCopyEnabled\s*=` on `*.cs`):

| Site | What it does |
|---|---|
| `DependencyInjection.cs` L41 | **Only assignment.** `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` |
| `CTraderFixLogonHostedService.cs` L60–70 | Sets Quote/Trade logon flags. **Logs** `_runtime.RealCopyEnabled`. **Does not write it.** |
| `LiveRuntimeStatus.cs` L32 | Mutable property; default `false` until DI sets it. |

Logon host after sockets:

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

There is no `_runtime.RealCopyEnabled = false` anywhere after that.

Lab config (public boolean + public dest identity only):

| Key | Value (non-secret) |
|---|---|
| `.env` L73 `REAL_COPY_EXECUTION_ENABLED` | `true` |
| `.env` L49 `CTRADER_FIX_HOST` | `demo-us-eqx-01.p.c-trader.com` |
| `.env` L50 `CTRADER_FIX_ACCOUNT_ID` | `5328266` |
| `.env` L64 `CTRADER_FIX_TRADE_SENDER_COMP_ID` | `demo.pepperstone.5328266` |

API boot (`apps/api/Program.cs` L10 + L13) loads `.env` then environment variables, then `AddTraderIntelligence`. `/api/health` and `/api/settings` **echo** `runtime.RealCopyEnabled`; they do not pin it false.

`RiskEngine` L90–93 comments that `RealExecutionEnabled == false` is a shadow path; it does **not** force the runtime flag false.

Live GET of `/api/health` was **not** obtained (SSRF). I therefore cannot prove the process RAM bit. I **can** prove the claim “forced false after logon” is **false in source**: logon does not assign the flag; DI binds `.env` `true`.

**Claim 3: FAIL.**

---

## 4. Claim 4 — sending now cannot be the profit path

**FAIL** as an unscoped claim. Session-scoped “this file cannot send” is true and insufficient.

### 4.1 Assigned files — send is not the booked dest book

`CTraderFixSession` cannot place an order (claim 1.1).

`RiskEngine.Evaluate` AllowFixSend (L147–150):

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

`CopyTradingService` then **throws that bit away**:

- L20: `public const bool VenueReconciled = false;`
- L304: `Reconciled = VenueReconciled` → Evaluate `AllowFixSend` is always false on this path.
- L324: persist `AllowFixSend = false` regardless of `decision.AllowFixSend`.
- L330–333: even the theoretical live branch only sets `LIVE_SEND_BLOCKED_UNIMPLEMENTED`. It does **not** call a FIX writer.

Dashboard dest PnL is a constructor zero, not a venue mark:

```33:46:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        return new OverviewDto(
            // ...
            shadowPnl,
            0,   // DestinationRealPnl
            0,   // XauGross
            0,   // XauNet
```

`LiveCopyPage.tsx` does not POST an order. It GETs `/api/copy/status` and `/api/copy/intents` and renders blockers + a table.

That is **not** enough to prove “sending cannot be the profit path.”

### 4.2 Hosted dest hop — send exists and is the dest path

```19:41:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
        await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            // ...
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
            // ...
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
```

Registered at `DependencyInjection.cs` L59. Same process as the API.

`ExecuteDemoCopyAsync` (L483–605):

- Returns 0 only if `!DemoDest`.
- `DemoDest` (L45–48) is host `demo-*` ∧ sender `demo.*` ∧ account ≠ `1369850`. Lab `.env` matches.
- Does **not** read `RealCopyEnabled`.
- Does **not** read `CanPromoteToLive`.
- Does **not** call `RiskEngine.Evaluate`.
- Calls `CTraderFixCopyOpen.SendAsync` for dest close (L528) and dest open (L566).
- On fill, writes ledger + intent `Status = "DEMO_SENT"` (L593).

`GetStatusAsync` L76–77 **advertises** the hop: “Demo dest auto-copy ON. Eligible demo/contest opens send on the 20s tick”.

`LiveCopyPage.tsx` L57 repeats it to the operator:

```56:58:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      {intents.length === 0 && (
        <p className="text-gray-500 text-sm">No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.</p>
      )}
```

On-disk dest fill (non-secret ids only): `D:\Prop\data\demo_copy_ledger.json`

| Field | Value |
|---|---|
| SourceLogin | `305750` |
| SourcePositionId | `21250421` |
| Lots | `0.01` |
| DestPositionId | `237339770` |
| DestClOrdId | `C20260818093047317` |
| DestFillPrice | `4390.2` |
| DestClosed | `false` |

`ExecuteDemoCopyAsync` L500–512 **re-seeds** that same open if missing. A dest ticket at 4390.2 on demo FIX is dest P&L exposure. Constructor `DestinationRealPnl=0` does not erase it.

Live Pepperstone `1369850` is refused (CopyOpen L39). That is **not** “sending cannot be profit.” It is “live identity is gated; demo dest is not.”

**Claim 4: FAIL.** Sending now **can** be the dest path on the demo hopper. Session-only send-absence is true and does not save the claim.

---

## 5. Claim 5 — SHADOW on demo is not destination profit

Split the hops. Unscoped claim **FAIL**.

### 5.1 Paper SHADOW — not dest cash (PASS this hop)

`GenerateShadowIntentsAsync` L336–359 writes `SHADOW_ONLY` + `ShadowCopyEngine.SimulateEntry`. That engine never opens a socket:

```35:60:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
    public ShadowFill SimulateEntry(...)
    {
        var useAsk = direction == TradeDirection.Long;
        var raw = useAsk ? quote.Ask : quote.Bid;
        // modeled delay slip only
        return new ShadowFill { /* Price, Quantity, SourceVsShadowSlippage */ };
    }
```

`EfDashboardQueries` L29 sums `ShadowOrders.SourceVsShadowSlippage` into `ShadowPnl`. That is modeled source-vs-quote slip, not dest cash.

`PersistDemoShadowAsync` (`EfTradingStore` L267+) also only simulates when `state == SHADOW` and a quote row exists. No FIX write.

`CopyGroupFilter` restricts roster to demo/contest **source groups**. That is source selection, not dest profit.

Paper SHADOW ≠ destination profit. **This hop PASS.**

### 5.2 SHADOW as roster state — dest send (FAIL unscoped)

`XauUsdOneToOneCopyPolicy.IsTraderEligible` **blocks** `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` (`TRADER_NOT_SHADOW_YET`, L81–85). It does **not** block `SHADOW`. It admits `SHADOW` when:

- no size-pattern flags
- `CompletedXauTrades >= 20`
- `XauNetPnl > 0`
- demo/contest group

`CopyRosterEngine.Decide` L72–80 then `AUTO_ADMIT`s that trader. Unit test `CopyRosterEngineTests.New_eligible_trader_is_auto_admitted` uses `State = SHADOW` and expects `RosterAction.Admit`.

`TickRosterAsync` writes `CopyIntent.Status = "ADMITTED"` for that seat.

`ExecuteDemoCopyAsync` L542–569 iterates `Status == "ADMITTED"` roster rows and dest-sends. It never checks `CurrentState == LIVE`. SHADOW + ADMITTED + open XAUUSD + `DemoDest` **is** a dest `35=D`.

`LiveCopyPage.tsx` L57 states the same operator contract: dest auto-sends after ADMIT, not after LIVE.

So “SHADOW on demo is not destination profit” is **false** as a process claim: SHADOW is the admit gate, and admit is the dest-send gate. Paper `ShadowOrder` rows remain not dest cash; dest fills sit in `demo_copy_ledger.json` / `DEMO_SENT` intents instead.

**Claim 5: FAIL** (unscoped). Paper hop isolated: PASS.

---

## 6. Live GET

Attempted this slot:

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | SSRF blocked (loopback) |
| `http://localhost:5080/api/health` | SSRF blocked (loopback) |

No `/api/copy/status`, `/api/settings`, or `/api/overview` body. Runtime `realCopyEnabled`, FIX logon bits, live-trader counts, and dest PnL **unproven**. File claims above do not depend on RAM except claim 3’s live echo, which is already failed from source.

---

## 7. Risk to capital

| Book | This slot |
|---|---|
| Live Pepperstone `1369850` | **NONE.** CopyOpen + DemoTestTrade refuse that account. `CTraderFixSession` cannot `35=D`. This slot sent **0**. |
| Demo dest (`demo-` host, `demo.` sender, account `5328266`) | **Not absent.** Hosted 20 s `ExecuteDemoCopyAsync` can `Build("D")`. Ledger shows an open dest pos. Not live capital; still dest exposure. |
| Source MT5 books | Roster flatten is dest-only (`CopyRosterEngine` comment L31). Not proven touched. |
| Paper shadow | Modeled slip only. |

`SAFE_BY_ABSENCE` applies to **`CTraderFixSession.cs`** and to **live `1369850`**. It does **not** apply to the hosted demo hopper.

---

## 8. What this slot did not do

- Did not edit product, tests, or `.env`.
- Did not send FIX.
- Did not flip `REAL_COPY`.
- Did not obtain a live GET.
- Did not treat prior `P500_VERIFY_*` text as evidence.

---

## 9. Binding close

**FAIL.**

1. `CTraderFixSession` has no `35=D` builder (**PASS_FILE**). Product has `CTraderFixCopyOpen.Build("D")` hosted (**FAIL_UNSCOPED**).
2. `CanPromoteToLive => false` (**PASS**). Vacuous; not a send interlock.
3. `RealCopyEnabled` is **not** forced false after logon (**FAIL**). DI binds `.env` `true`; logon logs only.
4. Sending now **can** be the dest path via the 20 s demo hopper (**FAIL**).
5. Paper SHADOW is not dest cash; SHADOW **can** ADMIT and dest-send (**FAIL** unscoped).

Live `1369850` refused. Live GET blocked. This slot sent 0.
