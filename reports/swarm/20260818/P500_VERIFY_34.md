# P500_VERIFY_34 — Adversarial verifier (slot 34)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_34.md` |
| Agent / slot | P500 adversarial verifier **34** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` live product files. Prior swarm PASS is **not** evidence. |
| Assigned reads (full, this pass) | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` |
| Supporting reads (this pass) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyGroupFilter.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `apps/api/Program.cs`, `TraderState.cs`, `BaselineScorerTests.cs`, `data/demo_copy_ledger.json` |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Quoted **boolean key only**. |
| Secrets printed | **None.** No passwords, no tag 554 values, no connection strings. |
| Live GET this pass | **Blocked** (`web_fetch` `http://127.0.0.1:5000/api/health` → SSRF). Claims that need a live GET and cannot be proven from a file are **FAIL**. |
| Live `35=D` sent this pass | **No.** |

**Honesty:** wanting profit is not an edge. A comment is not a choke. A POCO default is not a runtime pin. `35=A` logon is not a fill. Copying all source logins would copy `RISK_BLOCKED` source losses. SHADOW is a source-book label, not dest cash.

---

## 0. Verdict (binding)

**FAIL.**

Claims **(1) (2) (4) (5)** are file-proven. Claim **(3)** is **disproven** from live files: `RealCopyEnabled` is **not** forced false after logon.

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** (scoped to `CTraderFixSession.cs`) | file-proven |
| 2 | `CanPromoteToLive` is false | **PASS** | file-proven |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | file-disproven |
| 4 | sending now cannot be the profit path | **PASS** | file-proven (dest PnL constructor 0; live hop `SAFE_BY_ABSENCE`; 1:1 copy is not α) |
| 5 | SHADOW on demo is not destination profit | **PASS** | file-proven |

One-line:

```text
CTraderFixSession builder is 35=A only. CanPromoteToLive => false. RealCopyEnabled is DI-bound from .env=true; logon host does NOT re-pin false. Sending is not dest profit (DestinationRealPnl=0; persist AllowFixSend=false; live 1369850 refused). SHADOW is paper/source score, not dest PnL. Slot FAIL on claim 3. Risk NONE on live 1369850; demo dest hop residual.
```

Risk to capital: **NONE** on live Pepperstone `1369850` (`CTraderFixSession` is logon-only; `CTraderFixCopyOpen` refuses that account). **Not absent** on demo dest `5328266` if the API host is running (`ExecuteDemoCopyAsync` can emit `35=D` on the 20s tick). This slot did not send and did not live-GET.

---

## 1. No 35=D builder — PASS (`CTraderFixSession.cs` only)

Live file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines). Independent full read this pass.

Grep of this file for tag `35`: **3 hits**, none a NewOrderSingle builder.

| Line | Role |
|---|---|
| L55 | inbound `Extract(reply, "35")` of the logon **reply** |
| L73 | error string `Logon rejected 35={msgType}` |
| L96 | outbound `(35, "A")` Logon |

The only outbound assembler is `BuildLogon`. Tag 35 is hard-coded `"A"`:

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

One `WriteAsync` (L49). Socket disposed via `using`. No `NewOrderSingle`. No tag 38/54/40/11 assembly. No `Build("D")`.

Hosted logon uses this class only (`CTraderFixLogonHostedService.cs` L48–58). Hosted **copy send** does **not** use this class.

**Residual (does not fail the scoped claim):** sibling `CTraderFixCopyOpen.cs` L95 / L142 **does** have `Build("D", ...)`. Same folder: `CTraderFixDemoMatrix.cs`, `CTraderFixDemoTestTrade.cs`. A **product-wide** “no 35=D builder” claim would **FAIL**. This slot was told to read `CTraderFixSession.cs` for claim 1. That file has **zero** `35=D` builders.

---

## 2. CanPromoteToLive is false — PASS

Live file: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`.

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

`current` is unused. The function is a hard `false`. Unit pin: `BaselineScorerTests.cs` L22–26 (`Three_disciplined_winners_go_to_shadow_not_live`).

`TraderStateMachine.FromBaseline` never returns `LIVE` or `LIVE_CANDIDATE`. Ceiling is `SHADOW` when `quality >= 70 && risk < 40`; else `WATCH` / `EARLY_SCORE` / `INSUFFICIENT_DATA` / `RISK_BLOCKED`:

```187:207:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState FromBaseline(bool earlyEligible, decimal quality, decimal risk, FeatureSnapshot features)
    {
        if (features.CompletedXauTrades == 0)
            return TraderState.INSUFFICIENT_DATA;

        if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
            return TraderState.RISK_BLOCKED;
        // ...
        if (quality >= 70 && risk < 40)
            return TraderState.SHADOW;
        // ...
    }
```

Quality formula can print a high `EarlyQualityScore` with **negative** `NetPnl` (only `+15` if `NetPnl > 0`; L152–160). High quality is **not** dest profit and is **not** a live promotion.

Enum still lists `LIVE_CANDIDATE` / `LIVE` (`TraderState.cs` L8–10). That is a type, not a promotion path. This slot did not live-GET `/api/traders?state=LIVE` (SSRF). File proof is the hard `=> false`.

---

## 3. RealCopyEnabled forced false after logon — FAIL

**Disproven.** Grep of product `*.cs` for `RealCopyEnabled =` : **one assignment** — DI bind, **not** the logon host.

`D:\Prop\src\Infrastructure\DependencyInjection.cs` L39–42:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret). API `Program.cs` L10 `EnvFile.FindAndLoad()` then L13 `AddEnvironmentVariables()` then L15 `AddTraderIntelligence`. The singleton is therefore **armed** at process start when that env is present.

`LiveRuntimeStatus.RealCopyEnabled` is a public `{ get; set; }` (`LiveRuntimeStatus.cs` L32) with **no** constructor force-false and **no** post-logon pin. Snapshot `copyNote` even advertises the armed state (L42–44).

After FIX logon, `CTraderFixLogonHostedService` writes Quote/Trade `LoggedOn`/`Status`/`LastError`/`UpdatedAt` and **logs** the flag. It never assigns `false`:

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

`/api/health` and `/api/settings` echo the runtime bool (`Program.cs` L55, L76). Live GET of those endpoints was **SSRF-blocked** this pass; the file path is enough to fail the claim.

`RiskEngine` does **not** force the flag false either. When `RealExecutionEnabled == false` it comments and continues (L90–93). `AllowFixSend` is then `request.RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). That is an **evaluate-time** AND, not a logon re-pin of `LiveRuntimeStatus`.

`CopyTradingService.BuildBlockers` L621 only **displays** `"REAL_COPY_EXECUTION_ENABLED is false"` when the flag is already false. It does not write it.

`CREDENTIALS_AND_COPY_STATUS.md` / older “forced false after logon” notes are **STALE** against HEAD.

---

## 4. Sending now cannot be the profit path — PASS

Proven from the assigned files plus the hop that would actually send.

**Live hop cannot send.** `CTraderFixSession` is `35=A` only (claim 1). `CanPromoteToLive => false` so the scorer never emits `LIVE` (claim 2). Copy persist **overwrites** `AllowFixSend = false` even if Evaluate approved:

```317:336:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                var rec = new RiskDecisionRecord
                {
                    // ...
                    AllowFixSend = false,
                    DecidedAt = now
                };
                // ...
                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
                else
                {
                    intent.Status = "SHADOW_ONLY";
```

`VenueReconciled` is a **const false** (`CopyTradingService.cs` L20). Evaluate is called with `Reconciled = VenueReconciled` (L304), so `AllowFixSend` from `RiskEngine` is already false on OPEN (`!Reconciled && IsIncreasing` → `VENUE_NOT_RECONCILED` at RiskEngine L84–85). The live branch is dead twice (`CanPromoteToLive => false` plus persist overwrite plus const unreconciled).

**Dest profit is not booked.** `EfDashboardQueries.GetOverviewAsync` passes `destinationRealPnl: 0` (literal, L44). Trader-row `ShadowPnl` is literal `0` (L118). Overview `shadowPnl` is `Sum(SourceVsShadowSlippage)` (L29) — slippage, not dest cash. Risk dashboard daily/drawdown/XAU exposures are also constructor `0` (`GetRiskAsync` L208).

**Assigned UI is not a profit tape.** `LiveCopyPage.tsx` shows `realCopyArmed`, SHADOW/LIVE counts, `liveSends`, intents, shadow fills, QUOTE/TRADE up/down. Columns: Broker / Login / Pos / Side / Qty / Status / Risk. No dest PnL column. Empty-state text admits demo dest auto-send; that is a **status string**, not a measured edge:

```56:58:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      {intents.length === 0 && (
        <p className="text-gray-500 text-sm">No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.</p>
      )}
```

**1:1 copy is not α.** `XauUsdOneToOneCopyPolicy.AllocationFactor = 1m` (`CopyTradingService` L21). Policy copies source lots/side/SL/TP. Comment in the policy (L57–61) states it does **not** wait until a ticket is profitable. Wanting dest profit from that hop is lookahead, not an edge.

**Honesty residual (does not revive a live profit path):** HEAD `NewOrderSingleImplemented => DemoDest` (L50; const-false reports are **STALE**). `CopyTradingHostedService` 20s tick calls `ExecuteDemoCopyAsync` (L30), which can `CTraderFixCopyOpen.Build("D")` on a demo host/sender and **refuses** account `1369850` (`CTraderFixCopyOpen.cs` L37–42). That send **bypasses** `RiskEngine.Evaluate` and does **not** check `RealCopyEnabled`. Demo ledger `D:\Prop\data\demo_copy_ledger.json` records source `305750` / dest pos `237339770` / 0.01 lots / `DestClosed=false` — a dest **fill**, still **not** `DestinationRealPnl`. Sending that ticket is not the profit path: dest cash is unmeasured by this product, α is 1:1, and a copy-all of the scored book would import `RISK_BLOCKED` source losses.

Live GET `/api/copy/status` / `/api/overview` **not** obtained (SSRF). File constructors are enough.

---

## 5. SHADOW on demo is not destination profit — PASS

`BaselineScorer` `SHADOW` is a **source-book** label (quality/risk on reconstructed XAU `NetPnl`). It is not dest cash.

`ShadowCopyEngine.SimulateEntry` / `SimulateExit` write paper fills (modeled 0.05-point latency slip). `GenerateShadowIntentsAsync` only calls `SimulateEntry` after `SHADOW_ONLY` (CopyTradingService L336–360). No dest ticket, no dest mark. `MarkToMarket` exists on the engine and is **unwired** from dashboard dest PnL (overview dest is still literal 0).

Dashboard constructors (this pass):

| Field | Constructor | Meaning |
|---|---|---|
| `OverviewDto.DestinationRealPnl` | literal `0` (`EfDashboardQueries` L44) | dest cash **not computed** |
| `OverviewDto.ShadowPnl` | `Sum(ShadowOrders.SourceVsShadowSlippage)` (L29) | slip, not dest PnL |
| `TraderRowDto.ShadowPnl` | literal `0` (L118) | unused |

`CopyGroupFilter.IsDemoOrContest` is **required** for policy eligibility (`NOT_DEMO_OR_CONTEST_GROUP`, `XauUsdOneToOneCopyPolicy` L105–108). So the SHADOW copy set is **demo/contest source groups**. That is adverse-selection of challenge books, not destination profit.

`XauUsdOneToOneCopyPolicy.IsTraderEligible` uses **source** `XauNetPnl` (must be `> 0`) and `State` not in `{INSUFFICIENT_DATA, EARLY_SCORE, WATCH}` (`TRADER_NOT_SHADOW_YET`). Eligibility is source history, not dest marks.

`LiveCopyPage.tsx` L14 “SHADOW traders” is a **count** from `/api/copy/status`. It does not display dest PnL.

On-disk demo ledger fill is a dest position on demo dest. The product still reports dest PnL as **0**. A demo fill is not destination profit in this system.

---

## 6. What this slot did **not** prove

- Runtime `GET /api/health` `realCopyEnabled` (SSRF). File path is `.env=true` + DI bind + no re-pin.
- Current SHADOW count, dest mark-to-market of dest `237339770`, or whether the API host is running.
- Tree-wide “product `35=D=0`” (false; sibling `Build("D")` exists).
- That demo dest cannot lose money if the host is up. It can. That is **not** live `1369850` capital.
- Copy-all 8463 loss dollar figure (cited by other slots; **not re-measured** this pass — not used as proof).

---

## 7. Risk to capital

| Book | Risk | Why |
|---|---|---|
| Live Pepperstone `1369850` | **NONE** | `CTraderFixSession` outbound is only `35=A`. `CTraderFixCopyOpen` refuses this account. Persist `AllowFixSend=false`. `CanPromoteToLive => false`. |
| Demo dest `5328266` | **RESIDUAL** | Hosted 20s `ExecuteDemoCopyAsync` can send `35=D` without Evaluate / without `RealCopyEnabled` check. Ledger already has a 0.01 dest fill. Not booked as dest PnL. |
| Copy-all of the scored book if a live sender existed | **HIGH / ruin** | Would clone `RISK_BLOCKED` source losses. Wanting profit ≠ edge. Dollar total **not re-measured** this pass. |

This slot: no send, no secret, no source edit.
