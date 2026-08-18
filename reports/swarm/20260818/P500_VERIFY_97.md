# P500_VERIFY_97 — Adversarial verifier (slot 97)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_97.md` |
| Agent / slot | P500 adversarial verifier **97** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (HEAD `src/`, `apps/`, lab `.env`) |
| Assigned SUT (read in full this slot) | `CTraderFixSession.cs` (135/135), `BaselineScorer.cs` (212/212), `RiskEngine.cs` (189/189), `LiveCopyPage.tsx` (70/70) |
| Supporting files (claims 3–5 hop only) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `EnvFile.cs`, `apps/api/Program.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `CTraderFixOptions.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `CopyGroupFilter.cs`, `CopyLifecycle.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `hooks.ts`, `apps/api/Properties/launchSettings.json`, `data/demo_copy_ledger.json`, `DEMO_COPY_OPEN.json`, `tests/Unit/BaselineScorerTests.cs` |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Live `35=D` sent this slot | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None.** Quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73), `FEATURE_COPY_TRADING_ENABLED=true` (`.env` L106), and public dest ids `5328266` / `1369850`. No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. Ledger `DestFillPrice` is a public fill print already on disk, not a secret. |
| Live GET this pass | `GET http://127.0.0.1:5000/api/health` and `http://localhost:5000/api/health` **SSRF-blocked**. Runtime armed-bit **not** live-proven. File proof is enough to **FAIL** claims 1 (unscoped), 3, 4 (as written). |
| Binding rule | **FAIL any assigned claim that cannot be proven from a live file or a live GET.** Sibling swarm books are not evidence. This slot re-read the four named files in full, then walked every `RealCopyEnabled =` and every product `Build("D")`. |

**Honesty:** Wanting dest profit is not an edge. Copy-all of the catalog would copy `RISK_BLOCKED` source losses. `SAFE_BY_ABSENCE` on live `1369850` is **not** “`RealCopyEnabled` stays false.” An armed env bit with no live sender is still an armed bit. Demo dest send is dest **exposure**, not dest-profit accounting. Constructor `DestinationRealPnl=0` is not a mark-to-market.

---

## 0. Verdict (binding)

**FAIL.** Claim 2 is the only claim proven as written. Claim 1 is proven only on `CTraderFixSession`. Claim 3 is **disproven**. Claims 4–5 are not proven as absolute product statements (hosted demo dest hop + SHADOW as AUTO_ADMIT floor).

| # | Assigned claim | Measured | Class |
|---|---|---|---|
| 1 | no `35=D` builder | **PROVEN** on `CTraderFixSession.cs` 135/135 (`35=A` only). **NOT proven** product-wide: sibling `CTraderFixCopyOpen.Build("D")` is hosted on the 20s tick. | **PASS_SESSION / FAIL_UNSCOPED** |
| 2 | `CanPromoteToLive` is false | **PROVEN** (`=> false`; parameter unused) | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **DISPROVEN** — only assignment is DI bind of `.env=true`; logon host never writes the bit | **FAIL** |
| 4 | sending now cannot be the profit path | **NOT proven** as written. Booked dest PnL is constructor `0`; live `1369850` refused. Residual: 20s `ExecuteDemoCopyAsync` → dest `35=D` on demo. | **FAIL** (as written) / **PASS_NOT_BOOKED_DEST_PROFIT** |
| 5 | SHADOW on demo is not destination profit | Paper SHADOW / slippage **is not** dest cash. SHADOW **is** the dest AUTO_ADMIT floor. Dest DTO is still `0`. | **PASS_PAPER / FAIL_AS_DEST_CLASS** |

One-line:

```text
FAIL slot 97: CTraderFixSession 35=A only (no D builder in that file); product Build("D") x5 hosted; CanPromoteToLive=>false; RealCopyEnabled NOT forced false after logon (.env true + DI bind + logon logs-only); send-now is not booked dest-profit (DTO 0; live 1369850 refused) but demo dest hop can 35=D; SHADOW paper != dest PnL but SHADOW is dest AUTO_ADMIT. Risk NONE on live 1369850. Live GET blocked. This slot sent 0.
```

Do **not** treat this FAIL as a license to send. Do **not** flip leftover `.env=true` into go-live. Operator should restore `REAL_COPY_EXECUTION_ENABLED=false` (this slot did **not** edit it).

---

## 1. Claim 1 — no `35=D` builder — PASS_SESSION / FAIL_UNSCOPED

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` is **135** lines. Read in full this slot.

Outbound builder is only `BuildLogon`. Tag 35 is hardcoded `"A"`:

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

Single `WriteAsync` (L49) of that logon. Then one `ReadAsync`. `using` disposes `TcpClient` + `SslStream`. There is **no** persistent TRADE socket and **no** second write.

Tokens in this file (rg this slot):

| Token | Hits | Role |
|---|---|---|
| `(35, "A")` | L96 | outbound Logon |
| `Extract(reply, "35")` | L55 | inbound parse |
| interpolated `35={msgType}` | L73 | inbound reject text |
| `Build("D")` / `(35, "D")` / `NewOrderSingle` | **0** | — |

The other three assigned files also contain **zero** FIX NewOrderSingle builders:

- `BaselineScorer.cs` — scoring + `TraderStateMachine` only.
- `RiskEngine.cs` — `AllowFixSend` bool; no FIX assemble.
- `LiveCopyPage.tsx` — GET display only (`useCopyStatus` / `useCopyIntents`).

**Unscoped claim FAILS.** Product `Build("D")` call sites this slot (rg `Build\("D"\)` on `*.cs`):

| File | Lines | Hosted? |
|---|---|---|
| `CTraderFixCopyOpen.cs` | L95 | **Yes** — `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566 |
| `CTraderFixDemoTestTrade.cs` | L139, L163, L197 | CLI `tools/DemoFixTestTrade` (demo-gated; refuse `1369850`) |
| `CTraderFixDemoMatrix.cs` | L93 (`SendD` helper) | CLI matrix (demo-gated) |

`CTraderFixCopyOpen.Build` is a generic `Build(string type, ...)` that emits tag 35 from the caller. The hosted caller passes `"D"`:

```83:95:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
            var closing = !string.IsNullOrWhiteSpace(destPositionId);
            var cl = (closing ? "X" : "C") + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            var now = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var side = closing ? (isLong ? "2" : "1") : (isLong ? "1" : "2");
            var extra = new List<(int, string)>
            {
                (11, cl), (55, gold), (54, side), (60, now), (40, "1"),
                (38, units.ToString("0.##", CultureInfo.InvariantCulture)),
                (494, (closing ? "close-" : "copy-") + sourceLogin + "-" + sourcePositionId)
            };
            if (closing)
                extra.Add((721, destPositionId!));
            await Write(ssl, Build("D", sender, target, seq, extra.ToArray()), timeout.Token);
```

Hosted hop (`CopyTradingHostedService` L28–30) every 20s:

```28:30:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

CopyOpen **refuses** live identity (`host` must start `demo-`, `sender` must start `demo.`, `account == "1369850"` is hard-fail). That is **not** “no builder.”

On-disk prior dest fill (not sent this slot): `reports/swarm/20260818/DEMO_COPY_OPEN.json` — `OrderSent=true`, `Filled=true`, `PosId=237339770`, inbound `35=8` / `150=F` / `39=2`, account `5328266`, host `demo-us-eqx-01.p.c-trader.com`.

**Claim 1 as a product-safety statement: FAIL.** Scoped to the assigned session file: **PASS_SESSION**.

---

## 2. Claim 2 — `CanPromoteToLive` is false — PASS

`D:\Prop\src\Domain\Scoring\BaselineScorer.cs` is **212** lines. Read in full.

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

Measured:

- `CanPromoteToLive` is an expression-bodied `false`. Parameter `current` is unused. There is no branch that returns `true`.
- `FromBaseline` never returns `LIVE`, `LIVE_CANDIDATE`, `PAUSED`, or `DISQUALIFIED`. Ceiling is `SHADOW`.
- `AfterHighEarlyScore()` is `SHADOW`, not `LIVE`.
- Unit lock: `tests/Unit/BaselineScorerTests.cs` L21–26 — three disciplined winners → `SHADOW`; `CanPromoteToLive(...).Should().BeFalse()`.

`LiveCopyPage.tsx` does not call `CanPromoteToLive`. Copy hop does not consult it. Promotion-to-LIVE is not a file-proven path.

**Claim 2: PASS.**

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL (disproven)

rg `RealCopyEnabled\s*=` on product `*.cs` this slot: **one write**.

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Lab `.env` L73 (boolean only): `REAL_COPY_EXECUTION_ENABLED=true`.

API boot (`apps/api/Program.cs` L10) calls `EnvFile.FindAndLoad()`, which loads `D:\Prop\.env` into the process environment (`EnvFile.cs` L14 + L38). `AddEnvironmentVariables()` (L13) then feeds DI.

`CTraderFixLogonHostedService` after both `TryLogonAsync` calls:

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

It **reads** `_runtime.RealCopyEnabled`. It does **not** assign `false`. There is no `RealCopyEnabled = false` anywhere in `src/` or `apps/`.

Other readers (not writers):

- `LiveRuntimeStatus.Snapshot()` — if armed, note still says “NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.” That is a **string**, not a force-false.
- `CopyTradingService.GetStatusAsync` exposes `RealCopyArmed: _runtime.RealCopyEnabled`.
- `GenerateShadowIntentsAsync` L303 passes `_runtime.RealCopyEnabled` into `RiskEvaluationRequest.RealExecutionEnabled`.
- `BuildBlockers` L621 adds `"REAL_COPY_EXECUTION_ENABLED is false"` only when the bit is already false (does not clear it).
- `Program.cs` `/api/health` L55 and `/api/settings` L76 echo the runtime bit.
- `CTraderFixOptions.RealCopyExecutionEnabled` **defaults false** (L35) and is **not** what DI binds into `LiveRuntimeStatus`.

`RiskEngine` L90–93 is a comment-only no-op when `RealExecutionEnabled == false`. It does not pin the runtime flag.

Live GET of `/api/health` → `realCopyEnabled` was **SSRF-blocked** this slot. File proof is enough: the claimed post-logon force-false **does not exist**. If the API process loaded `.env`, the bit is **true** at construction and stays true through logon.

**Claim 3: FAIL / DISPROVEN.**

---

## 4. Claim 4 — sending now cannot be the profit path — FAIL as written / PASS_NOT_BOOKED_DEST_PROFIT

Split the claim.

### 4a. Booked dest-profit accounting — not a profit path (PASS_NOT_BOOKED_DEST_PROFIT)

`GenerateShadowIntentsAsync` hard-persists `AllowFixSend = false` regardless of `decision.AllowFixSend`:

```317:337:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
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
                _db.RiskDecisions.Add(rec);
                intent.RiskDecisionId = rec.Id;

                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
                else
                {
                    intent.Status = "SHADOW_ONLY";
```

`VenueReconciled` is `const false` (L20). `FromBaseline` never emits `LIVE`. So the LIVE-send branch is dead even if `NewOrderSingleImplemented => DemoDest` is true.

`RiskEngine.Evaluate` computes `AllowFixSend` only when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Copy hop passes `Reconciled = VenueReconciled` (`false`), so `decision.AllowFixSend` is false on this hop even if `.env` armed the runtime bit.

Dashboard dest PnL is a **constructor zero**, not a mark:

```33:46:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
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
```

`OverviewDto.DestinationRealPnl` is the second `0` (L44). It is not summed from the ledger, dest fills, or `ShadowOrders`.

Live identity is refused (`CTraderFixCopyOpen` L37–42: `account == "1369850"`). `CTraderFixSession` itself cannot send a ticket.

### 4b. Unscoped “sending now cannot be the profit path” — FAIL

Hosted `ExecuteDemoCopyAsync` is a **second path**. It does **not** read `RealCopyEnabled`, `CanPromoteToLive`, `AllowFixSend`, `VenueReconciled`, or `RiskEngine.Evaluate`. Gate is `DemoDest` only:

```45:50:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public bool DemoDest =>
        (_config["CTRADER_FIX_HOST"] ?? "").StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
        && (_config["CTRADER_FIX_TRADE_SENDER_COMP_ID"] ?? "").StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
        && _config["CTRADER_FIX_ACCOUNT_ID"] != "1369850";

    public bool NewOrderSingleImplemented => DemoDest;
```

Then L528 / L566 call `CTraderFixCopyOpen.SendAsync` → `Build("D")`. Open condition is `ADMITTED` roster + open XAUUSD + `MaxAutoLots=0.05` + `CopyLifecycle.ShouldOpenDest`. Close condition is source completed + dest filled + not already closed.

On-disk residual (`D:\Prop\data\demo_copy_ledger.json`):

- `SourceLogin=305750`, `SourcePositionId=21250421`
- `DestPositionId=237339770`, `DestClOrdId=C20260818093047317`
- `DestFillPrice=4390.2`, `Lots=0.01`, **`DestClosed=false`**

`ExecuteDemoCopyAsync` L500–512 **re-seeds** that same open dest row if missing. Every 20s tick will attempt dest close if the source reconstructs completed.

`LiveCopyPage.tsx` empty-state **admits** dest send:

```56:58:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      {intents.length === 0 && (
        <p className="text-gray-500 text-sm">No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.</p>
      )}
```

Status strip L13: `REAL_COPY armed` = `status?.realCopyArmed`. That is the env bit, not a send proof.

A dest fill on demo account `5328266` **is** a dest P&L path (demo cash / demo equity). Dashboard `0` does not erase the venue ticket. Prior `DEMO_COPY_OPEN.json` already recorded `Filled=true`.

**Claim 4 as written: FAIL.** Booked-dest-PnL scoped: **PASS_NOT_BOOKED_DEST_PROFIT**. Live `1369850`: **NONE**.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — PASS_PAPER / FAIL_AS_DEST_CLASS

### 5a. Paper SHADOW ledger is not dest cash — PASS_PAPER

Shadow hop (`GenerateShadowIntentsAsync` L337–359) writes `Status="SHADOW_ONLY"` and, on Approve + quote, `_shadow.SimulateEntry(...)`. `ShadowCopyEngine.SimulateEntry` returns a modeled fill (`DefaultLatencySlippagePoints=0.05`) — no socket, no tag 35.

Dashboard `ShadowPnl` is `Sum(SourceVsShadowSlippage)` (`EfDashboardQueries` L29). That is source-vs-model slippage, not dest realized.

`RiskEngine` comment L90–93: “Shadow path still evaluates risk but never allows FIX send.” The later `allowSend` formula implements that **if** the request’s `Reconciled`/`RealExecutionEnabled` bits are honest. Copy hop then **overwrites** persist `AllowFixSend=false` anyway.

### 5b. SHADOW as dest class — FAIL_AS_DEST_CLASS

`XauUsdOneToOneCopyPolicy.IsTraderEligible` **rejects** `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` (`TRADER_NOT_SHADOW_YET`) and accepts `SHADOW` / `LIVE_CANDIDATE` / `LIVE` (plus 20 completed XAU, net>0, demo/contest group, no size pattern):

```81:85:D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs
        if (trader.State is TraderState.INSUFFICIENT_DATA or TraderState.EARLY_SCORE or TraderState.WATCH)
        {
            reason = "TRADER_NOT_SHADOW_YET";
            return false;
        }
```

`CopyRosterEngine.Decide` AUTO_ADMITs when `IsTraderEligible` is true. Because `FromBaseline` never emits LIVE, **SHADOW is the dest AUTO_ADMIT floor**.

`ExecuteDemoCopyAsync` then dest-sends for `ADMITTED` seats. It does **not** re-check `TraderState.SHADOW` vs `LIVE`. A SHADOW demo/contest name with an open ≤0.05 lot XAUUSD ticket is the dest-send class.

`CopyGroupFilter.IsDemoOrContest` **requires** a `demo` or `contest` path segment. Real groups are excluded. That is adverse-selection (copy challenge books), not “SHADOW cannot produce dest profit.”

**Claim 5 as written (SHADOW on demo is not dest profit): FAIL** as a dest-safety statement. Paper ledger: **PASS_PAPER**. Dest DTO remains constructor `0`.

---

## 6. Assigned UI file — `LiveCopyPage.tsx` (70/70)

Read in full. No POST. No FIX. Hooks: `useCopyStatus` → `GET /api/copy/status`; `useCopyIntents` → `GET /api/copy/intents`.

| UI | Source | Honesty |
|---|---|---|
| `REAL_COPY armed` YES/NO | `status.realCopyArmed` ← `_runtime.RealCopyEnabled` | Shows the **env bit**, not a venue send |
| `LIVE traders` | score count `CurrentState==LIVE` | Scorer never emits LIVE |
| `Live sends` | `ExecutionIntents` with `SentAt != null` | Not the demo ledger |
| Blockers box | `BuildBlockers` | On `DemoDest` the `SAFE_BY_ABSENCE` / `0 LIVE` blockers are **omitted** |
| Empty-state L57 | hardcoded | **Admits dest auto-send** — contradicts claim 4 as written |

The page title is “Live copy portfolio.” The pipeline it displays can dest-send on demo without LIVE state.

---

## 7. Live GET

Attempted this slot:

- `GET http://127.0.0.1:5000/api/health` — **SSRF blocked** (loopback).
- `GET http://localhost:5000/api/health` — **SSRF blocked**.

`launchSettings.json` profile `http` binds `http://localhost:5000`. No live body. Runtime `realCopyEnabled` not live-proven. File proof stands.

---

## 8. Risk to capital

| Book | Exposure |
|---|---|
| Live Pepperstone `1369850` | **NONE** (`SAFE_BY_ABSENCE`). CopyOpen refuses that account. Session file cannot `35=D`. Scorer cannot LIVE. Persist `AllowFixSend=false`. `VenueReconciled=const false`. |
| Demo dest `5328266` | **Not absent.** Hosted 20s hopper can `Build("D")`. Ledger dest `237339770` still `DestClosed=false`. Prior fill print exists. This slot sent **0**. |
| Paper SHADOW | Model slippage only. Not dest cash. |
| Env armed bit | `.env` L73 `true` bound at DI. Logon does not clear it. Not a live ticket by itself. |

Wanting dest profit ≠ edge. This slot did not send, did not flip flags, did not print secrets.

---

## 9. What would be required to PASS all five as written

1. Delete or un-host product `Build("D")` (`CTraderFixCopyOpen` + hosted `ExecuteDemoCopyAsync`), **or** narrow claim 1 to `CTraderFixSession` only.
2. Keep `CanPromoteToLive => false` (already true).
3. After logon, assign `RealCopyEnabled = false` (or stop binding `.env=true`).
4. Either remove dest send or prove with a live GET that no dest ticket is open and dest PnL is measured `0` — constructor `0` is not that proof.
5. Either stop AUTO_ADMIT from SHADOW, or reword claim 5 to “paper `ShadowOrder` ≠ dest cash.”

None of that was done this slot.
