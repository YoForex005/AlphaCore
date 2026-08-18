# P500_VERIFY_60 — Adversarial: session / promote / RealCopy pin / send-as-profit / SHADOW≠dest

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_60.md` |
| Slot | **60** |
| Agent | P500_VERIFY_60 (adversarial verifier; independent HEAD re-read) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Topic | Confirm five claims from assigned files. **FAIL any claim not proven from a file or live GET.** |
| Assigned SUT | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` |
| Adjacent (needed to prove/disprove 1, 3–5) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `ShadowCopyEngine.cs`, `CopyRosterEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyGroupFilter.cs`, `EfDashboardQueries.cs`, `apps/api/Program.cs`, `data/demo_copy_ledger.json`, `DEMO_COPY_OPEN.json`, `.env` **booleans / public dest ids only** |
| Product source modified | **No.** Report + INDEX / SWARM_LOG / `P500_MANIFEST.tsv` pins only. |
| Test source modified | **No.** |
| Config / `.env` edited | **No.** `REAL_COPY` not flipped. |
| Live `35=D` / NewOrderSingle this slot | **Not sent. Not constructed.** |
| Secrets printed | **None.** Quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73), public dest id `5328266`, live refuse id `1369850`. |
| Localhost API this slot | **Attempted, blocked.** `GET http://127.0.0.1:5000/api/health`, `/api/copy/status`, `/api/settings` → worker `SSRF blocked: 127.0.0.1`. `open_page` also failed. Runtime `realCopyEnabled` **not** live-proven. File-only for claim 3. |

**Honesty rule:** A015 / “logon re-pins `RealCopyEnabled=false`”, “product `35=D=0`”, and “`NOS=const false`” are **STALE vs HEAD**. This slot re-read the files. Chat is not evidence. Prior P500_VERIFY_* verdicts were not copied; every citation below is a line this slot opened.

```text
CTraderFixSession outbound is 35=A only.
Product 35=D builders exist (CopyOpen / DemoTestTrade / DemoMatrix).
CanPromoteToLive => false.
RealCopyEnabled is NOT forced false after logon (DI binds env; .env true).
Hosted 20s tick can 35=D on demo dest; live 1369850 refused.
SHADOW ledger is SimulateEntry; SHADOW is also dest AUTO_ADMIT floor.
Live GET this slot = SSRF blocked.
```

---

## 0. Verdict (binding)

**FAIL.** One of five claims is fully file-proven. Four fail the assigned FAIL-if-unproven rule when read as stated.

| # | Claim | Verdict | Why |
|---|---|---|---|
| 1 | no `35=D` builder | **FAIL** (product). **PASS_SCOPED** on `CTraderFixSession.cs` only | Assigned session file is Logon `35=A` only. Same folder + hosted hop assemble and send `35=D`. Unqualified “no builder” is false. |
| 2 | `CanPromoteToLive` is false | **PASS** | Literal `=> false`. `FromBaseline` never returns `LIVE`. Unit lock exists. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL (disproven)** | Sole C# write is DI bind from `REAL_COPY_EXECUTION_ENABLED`. `.env` L73 is `true`. Logon host **reads** the flag; **never assigns** it. Live GET of `/api/settings` not available this slot. |
| 4 | sending now cannot be the profit path | **FAIL (contradicted)** | Demo dest auto-copy **is** a send path (`ExecuteDemoCopyAsync` → `Build("D")`). Assigned `LiveCopyPage.tsx` L57 advertises dest auto-send. On-disk dest fill exists. Dashboard dest PnL `0` is a constructor, not a mark. Live `1369850` refuse is **not** the whole claim. |
| 5 | SHADOW on demo is not destination profit | **FAIL (unscoped) / PASS_SCOPED** (paper ledger) | Paper `ShadowOrder` is `SimulateEntry`; overview `ShadowPnl` is slippage sum; `DestinationRealPnl` is literal `0`. Unscoped claim fails: SHADOW is dest AUTO_ADMIT floor; hopper sends dest `35=D` without requiring `LIVE`. |

```text
OVERALL = FAIL
  because claim 3 is disproven from HEAD files
  and claims 1/4/5 cannot be confirmed as stated.

PASS only: (2) CanPromoteToLive.
PASS_SCOPED: (1) CTraderFixSession 35=A only; (5) SHADOW ledger ≠ dest cash column.
```

**Risk to capital:** **NONE on live `1369850`** (`CTraderFixCopyOpen` refuse L37–41 + session hop `35=A` only + persist `AllowFixSend=false` L324 + `VenueReconciled=const false` + `CanPromoteToLive=>false`). **Not absent on demo dest `5328266`** (hosted 20 s hop; ledger 305750 / dest 237339770 / 0.01 / 4390.2 / `DestClosed=false`). This slot sent **0**.

---

## 1. What was read (HEAD, this slot)

| File | Lines (this read) | Used for |
|---|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 135 | Claim 1 |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | 212 | Claim 2 |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | 189 | Claim 3–4 (`AllowFixSend`) |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | 70 | Claim 4–5 (UI honesty) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | 112 | Claim 3 |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 61 | Claim 3 |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | 66 | Claim 3 |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | 625 | Claim 1, 4, 5 |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 44 | Claim 4 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | 223 | Claim 1, 4 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | L139/163/197 | Claim 1 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | L93 (`SendD`) | Claim 1 |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | 91 | Claim 5 |
| `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs` | 136 | Claim 4–5 (ADMIT SHADOW) |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | 188 | Claim 4–5 |
| `D:\Prop\src\Domain\Copy\CopyGroupFilter.cs` | 24 | Claim 5 (demo/contest only) |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | dest ctor L33–52 | Claim 4–5 |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | L5–22 | Claim 4–5 (field order) |
| `D:\Prop\apps\api\Program.cs` | 159 | Claim 3 (exposes runtime flag) |
| `D:\Prop\src\Infrastructure\Copy\DemoCopyLedger.cs` | 40 | Claim 4 |
| `D:\Prop\data\demo_copy_ledger.json` | 11 | Claim 4 (on-disk dest fill) |
| `D:\Prop\reports\swarm\20260818\DEMO_COPY_OPEN.json` | 19 | Claim 4 (prior dest fill) |
| `D:\Prop\.env` L49/L50/L64/L73 | booleans + public ids | Claim 3–4 (`DemoDest` + REAL_COPY) |
| `tests/Unit/BaselineScorerTests.cs` | 74 | Claim 2 lock |
| `tests/Unit/RiskEngineTests.cs` | L21–26 | Claim 3–4 (`AllowFixSend` false when flag false) |

No password, token, or FIX `554=` value is quoted.

---

## 2. Claim 1 — no `35=D` builder — **FAIL** (product) / **PASS_SCOPED** (`CTraderFixSession`)

### 2.1 Assigned file: no NewOrderSingle assembler

`CTraderFixSession` has one outbound builder, `BuildLogon`, and one `WriteAsync`. Tag 35 is hardcoded `"A"`. The only other `35` uses extract the **inbound** logon reply.

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

Grep of this file: three `35` hits — inbound extract L55, reject text L73, outbound `(35, "A")` L96. **Zero** `"D"`. Sockets disposed via `using`. **PASS for this type.**

### 2.2 Product-wide “no builder” is false

Same namespace `TraderIntelligence.Fix.CTrader.Sessions` contains three `Build(type, …)` helpers that accept `"D"` and write it:

| Type | Outbound `Build("D"` / `SendD` | Role |
|---|---|---|
| `CTraderFixCopyOpen` | L95 | **Hosted** dest open/close |
| `CTraderFixDemoTestTrade` | L139, L163, L197 | Demo test flatten / open / close |
| `CTraderFixDemoMatrix` | L93 (`SendD`) | Demo scenario matrix |

`CopyTradingHostedService` (20 s) calls `ExecuteDemoCopyAsync`, which calls `CTraderFixCopyOpen.SendAsync` when `DemoDest` is true. `.env` host starts with `demo-`, trade sender starts with `demo.`, account is `5328266` (≠ `1369850`) → **`DemoDest` is true** from files.

```95:95:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
            await Write(ssl, Build("D", sender, target, seq, extra.ToArray()), timeout.Token);
```

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

`NewOrderSingleImplemented => DemoDest` (`CopyTradingService` L50). Reports that still say `NOS=const false` or “product `35=D=0`” are **STALE**.

**Claim 1 as stated cannot be confirmed.** FAIL.

---

## 3. Claim 2 — `CanPromoteToLive` is false — **PASS**

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

`FromBaseline` (L189–207) returns only `INSUFFICIENT_DATA`, `RISK_BLOCKED`, `SHADOW`, `WATCH`, or `EARLY_SCORE`. It never returns `LIVE` or `LIVE_CANDIDATE`.

Unit lock:

```20:27:D:\Prop\tests\Unit\BaselineScorerTests.cs
    public void Three_disciplined_winners_go_to_shadow_not_live()
    {
        var score = _s.Score(new[] { Closed(1, 80), Closed(2, 70), Closed(3, 90) });
        score.EarlyScoreEligible.Should().BeTrue();
        score.SuggestedState.Should().Be(TraderState.SHADOW);
        TraderStateMachine.CanPromoteToLive(score.SuggestedState).Should().BeFalse();
    }
```

**PASS.** Proven from assigned file + unit lock. Residual: enum still *defines* `LIVE=5` (`TraderState.cs` L10). That is a type, not a promotion path.

---

## 4. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL (disproven)**

### 4.1 Assigned files never assign the flag

- `CTraderFixSession` has no `RealCopyEnabled` symbol.
- `RiskEngine` *reads* `request.RealExecutionEnabled` to compute `AllowFixSend` (L147–150). It does not write any runtime flag. When the flag is false it **does not reject** (L90–93 is an empty comment).
- `LiveCopyPage` *displays* `status?.realCopyArmed` (L13). Display is not a pin.

### 4.2 Sole write is DI bind from env, not logon

Grep of `*.cs` for `RealCopyEnabled =` : **one hit**.

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`.env` L73 (boolean only): `REAL_COPY_EXECUTION_ENABLED=true`.

`LiveRuntimeStatus.RealCopyEnabled` is a public `{ get; set; }` (L32). Nothing after logon writes `false`.

### 4.3 Logon host reads, logs, does not re-pin

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

Assignments are Quote/Trade session bits only. `RealCopyEnabled` is interpolated into a log line. That is the opposite of “forced false after logon.”

API would expose the DI-bound value (`Program.cs` L55, L76). This slot could not GET it (SSRF). File-only: process that loads `.env` L73 starts with the flag **true**.

**Claim 3 is disproven.** FAIL.

---

## 5. Claim 4 — sending now cannot be the profit path — **FAIL**

### 5.1 What *is* blocked (not enough to confirm the claim)

| Gate | File pin | Effect |
|---|---|---|
| Session hop | `CTraderFixSession` `35=A` only | Logon cannot book dest P&L |
| Persist | `CopyTradingService` L324 `AllowFixSend = false` | Risk row never arms FIX |
| Venue | `VenueReconciled = const false` (L20) | Evaluate rejects new exposure `VENUE_NOT_RECONCILED` |
| Promote | `CanPromoteToLive => false` | Scorer cannot mint `LIVE` |
| Live account | `CTraderFixCopyOpen` L37–41 | `account == "1369850"` refused |
| Risk allow | `AllowFixSend` requires `RealExecutionEnabled && KillSwitch.None && Reconciled && VenueHealthy` (L147–150) | Shadow hop cannot live-send even if flag is true |

Unit lock: `Real_flag_false_never_allows_fix_send` (`RiskEngineTests` L21–26) — **Approve + AllowFixSend=false**. That is the *risk* hop, not the *demo dest* hop.

### 5.2 What *sends now*

`CopyTradingHostedService` every 20 s:

```28:30:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

`ExecuteDemoCopyAsync` (L483–605):

- Returns 0 only if `!DemoDest` or password blank.
- Does **not** call `RiskEngine.Evaluate`.
- Does **not** require `TraderState.LIVE`, `AllowFixSend`, or `RealCopyEnabled`.
- Opens via `CTraderFixCopyOpen.SendAsync` → `Build("D")`.
- Closes dest when source reconstructed trade is completed.
- Caps 5 sends/tick; skips source lots `> MaxAutoLots` (0.05).

`.env` pins `DemoDest=true`:

- L49 `CTRADER_FIX_HOST=demo-us-eqx-01.p.c-trader.com` (`demo-` prefix)
- L64 `CTRADER_FIX_TRADE_SENDER_COMP_ID=demo.pepperstone.5328266` (`demo.` prefix)
- L50 `CTRADER_FIX_ACCOUNT_ID=5328266` (≠ `1369850`)

Assigned UI already tells the operator dest send is the path:

```56:57:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      {intents.length === 0 && (
        <p className="text-gray-500 text-sm">No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.</p>
```

### 5.3 Dest fill on disk (not this slot)

`D:\Prop\data\demo_copy_ledger.json` (this read):

| Field | Value |
|---|---|
| SourceLogin | `305750` |
| SourcePositionId | `21250421` |
| Lots | `0.01` |
| DestPositionId | `237339770` |
| DestClOrdId | `C20260818093047317` |
| DestFillPrice | `4390.2` |
| DestClosed | `false` |

`DEMO_COPY_OPEN.json` matches: `OrderSent=true`, `Filled=true`, account `5328266`, inbound `35=8` / `150=F` / `39=2`. `ExecuteDemoCopyAsync` L500–512 **re-seeds that same row** if missing. Dest is open until source reconstructed trade completes.

Dashboard `DestinationRealPnl` is the 11th ctor arg, hard `0` (`EfDashboardQueries` L44). That is **not** a mark of dest cash. Claiming “dest P&L is $0 therefore send is not a profit path” is a lie.

**Claim 4 as stated is false.** Demo dest send **is** a dest P&L path. Live Pepperstone send is refused. FAIL.

---

## 6. Claim 5 — SHADOW on demo is not destination profit — **FAIL unscoped / PASS_SCOPED paper**

### 6.1 Paper SHADOW is not dest cash — **PASS_SCOPED**

`FromBaseline` emits `SHADOW` as a **source** state (quality ≥ 70 and risk < 40). It is not a dest fill.

`GenerateShadowIntentsAsync` writes `ShadowOrder` via `_shadow.SimulateEntry` (L339–359). `ShadowCopyEngine.SimulateEntry` computes bid/ask + 0.05 latency slippage. No socket. No tag 35.

Overview `ShadowPnl` = `Sum(ShadowOrders.SourceVsShadowSlippage)` (`EfDashboardQueries` L29). `DestinationRealPnl` = literal `0` (L44). Those columns are not dest broker P&L.

Assigned `LiveCopyPage` splits `SHADOW traders` from `Live sends` (L14–16). Display is not dest cash.

### 6.2 SHADOW-on-demo **is** the dest-send admission class — **FAIL unscoped**

Eligibility rejects everything below SHADOW:

```81:85:D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs
        if (trader.State is TraderState.INSUFFICIENT_DATA or TraderState.EARLY_SCORE or TraderState.WATCH)
        {
            reason = "TRADER_NOT_SHADOW_YET";
            return false;
        }
```

Roster `Decide` AUTO_ADMITs when `IsTraderEligible` (requires SHADOW-or-above + ≥20 XAU + PnL>0 + demo/contest group). `CopyGroupFilter` admits only `demo` / `contest` path segments.

Hopper `ExecuteDemoCopyAsync` then sends dest `35=D` for `ADMITTED` seats. It does **not** re-check `TraderState.LIVE`. The shadow-intent branch that *would* require LIVE (L330) is unreachable for live send (`VenueReconciled=false` + persist `AllowFixSend=false`); the **demo dest hop bypasses that branch**.

So: a SHADOW demo/contest name is exactly who can book dest lots. Dest fill 305750 sits on that path. Paper SHADOW P&L ≠ dest P&L **column**; SHADOW-on-demo **is** the dest-profit *gate*.

Unscoped claim “SHADOW on demo is not destination profit” cannot be confirmed. FAIL.

---

## 7. Assigned-file residual honesty

| Assigned file | What it proves | What it does **not** prove |
|---|---|---|
| `CTraderFixSession.cs` | No `35=D` in this type | No product builder |
| `BaselineScorer.cs` | Promotion to LIVE is impossible | Dest hopper is gated by LIVE |
| `RiskEngine.cs` | `AllowFixSend` can be false; flag-false does not reject | Hopper calls Evaluate |
| `LiveCopyPage.tsx` | UI shows armed / SHADOW / live sends / blockers | “cannot send”; L57 states dest auto-send |

`RiskEngine` L90–93 empty comment (“Shadow path still evaluates risk but never allows FIX send”) is **stale vs HEAD hopper**. Hopper never consults that comment.

---

## 8. Live GET

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | `web_fetch` SSRF blocked |
| `http://127.0.0.1:5000/api/copy/status` | `web_fetch` SSRF blocked |
| `http://127.0.0.1:5000/api/settings` | `web_fetch` SSRF blocked |
| `open_page` same host | Failed to retrieve |

No live body. Process bits (`quoteLoggedOn`, `tradeLoggedOn`, `realCopyArmed`, intent rows) **not remeasured**. File pins only.

---

## 9. What this slot did **not** do

- Did not flip `REAL_COPY_EXECUTION_ENABLED`.
- Did not call `CTraderFixCopyOpen.SendAsync`.
- Did not construct `35=D`.
- Did not print passwords, tokens, or tag `554`.
- Did not edit product / test / `.env`.
- Did not claim dest dashboard `$0` is a live mark.
- Did not claim “fully decompiled” or any EX5 metric (wrong tree).

---

## 10. Stale pins this slot kills

| Pin | Status vs this HEAD |
|---|---|
| Product `35=D` builder count = 0 | **STALE** — `Build("D")` ×5 (CopyOpen 1 + DemoTestTrade 3 + DemoMatrix SendD) |
| `NewOrderSingleImplemented => false` | **STALE** — `=> DemoDest` L50 |
| Logon forces `RealCopyEnabled=false` | **STALE / FALSE** |
| Persist `AllowFixSend=false` at L306 | **STALE line** — HEAD is **L324** (value still false) |
| Hopper “never sends” | **STALE** — `ExecuteDemoCopyAsync` is on the 20 s tick |
| Dest `$0` proves no dest P&L | **STALE / LIE** — constructor 0; ledger dest open |

---

*End of P500_VERIFY_60. Product source was not modified. No `35=D` was built or sent by this slot. No secrets printed. `REAL_COPY` was not flipped by this slot.*
