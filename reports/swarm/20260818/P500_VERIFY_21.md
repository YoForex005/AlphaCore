# P500_VERIFY_21 — Adversarial verifier (slot 21)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_21.md` |
| Agent / slot | P500 adversarial verifier **21** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` live product files. Prior swarm PASS is **not** evidence. |
| Assigned reads | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` (read in full this pass) |
| Supporting reads | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `ShadowCopyEngine.cs`, `CopyRosterEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyGroupFilter.cs`, `EfDashboardQueries.cs`, `apps/api/Program.cs` |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Quoted **boolean key only**. |
| Secrets printed | **None.** No passwords, no tag 554 values, no connection strings. |
| Live GET this pass | **Blocked** (localhost SSRF). Claims that need a live GET and cannot be proven from a file are **FAIL**. |
| Live `35=D` sent this pass | **No.** |

**Honesty:** wanting profit is not an edge. Copying all 8463 logins would copy `RISK_BLOCKED` source losses. A comment is not a choke. A POCO default is not a runtime pin. `35=A` logon is not a fill.

---

## 0. Verdict (binding)

**FAIL.**

Claims **(1) (2) (4) (5)** are file-proven. Claim **(3)** is **disproven** from live files: `RealCopyEnabled` is **not** forced false after logon.

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** (scoped to `CTraderFixSession.cs`) | file-proven |
| 2 | `CanPromoteToLive` is false | **PASS** | file-proven |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | file-disproven |
| 4 | sending now cannot be the profit path | **PASS** | file-proven (dest PnL constructor 0; live hop `SAFE_BY_ABSENCE`) |
| 5 | SHADOW on demo is not destination profit | **PASS** | file-proven |

One-line:

```text
CTraderFixSession builder is 35=A only. CanPromoteToLive => false. RealCopyEnabled is DI-bound from .env=true; logon host does NOT re-pin false. Sending is not dest profit (DestinationRealPnl=0; persist AllowFixSend=false; live 1369850 refused). SHADOW is paper/source score, not dest PnL. Slot FAIL on claim 3. Risk NONE on live 1369850; demo dest hop residual.
```

Risk to capital: **NONE** on live Pepperstone `1369850` (`CTraderFixSession` is logon-only; `CTraderFixCopyOpen` refuses that account). **Not absent** on demo dest `5328266` if the API host is running (`ExecuteDemoCopyAsync` can emit `35=D` on the 20s tick). This slot did not send and did not live-GET.

---

## 1. No 35=D builder — PASS (CTraderFixSession.cs only)

Live file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines). Independent full read this pass.

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

Grep of this file for `35=D`, `Build(`, `(35,`: **one hit** — `(35, "A")` at L96.

Inbound `Extract(reply, "35")` at L55 is a **read** of the logon reply, not a NewOrderSingle builder. One `WriteAsync` (L49). Socket disposed via `using`. No `NewOrderSingle`, no tag 38/54/40/11 assembly.

**Residual (does not fail the scoped claim):** sibling `CTraderFixCopyOpen.cs` L95 / L142 **does** have `Build("D", ...)`. Same folder: `CTraderFixDemoMatrix.cs`, `CTraderFixDemoTestTrade.cs`. A **product-wide** “no 35=D builder” claim would **FAIL**. This slot was told to read `CTraderFixSession.cs` for claim 1. That file has **zero** `35=D` builders.

Hosted logon uses this class only (`CTraderFixLogonHostedService.cs` L48–58). Hosted **copy send** does **not** use this class; it calls `CTraderFixCopyOpen.SendAsync` from `CopyTradingService.ExecuteDemoCopyAsync`.

---

## 2. CanPromoteToLive is false — PASS

Live file: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`.

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

`current` is unused. The function is a hard `false`.

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

**Disproven.** Grep of `D:\Prop` `*.cs` for `RealCopyEnabled =` : **one assignment** — DI bind, **not** the logon host.

`D:\Prop\src\Infrastructure\DependencyInjection.cs` L39–42:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret). API `Program.cs` L10 `EnvFile.FindAndLoad()` then L13 `AddEnvironmentVariables()` then L15 `AddTraderIntelligence`. The singleton is therefore **armed** at process start when that env is present.

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

`LiveRuntimeStatus.RealCopyEnabled` is a public `{ get; set; }` with **no** post-logon pin. `/api/health` and `/api/settings` echo the runtime bool (`Program.cs` L55, L76). Live GET of those endpoints was **SSRF-blocked** this pass; the file path is enough to fail the claim.

`RiskEngine` does **not** force the flag false either. When `RealExecutionEnabled == false` it comments and continues (L90–93). `AllowFixSend` is then `request.RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). That is an **evaluate-time** AND, not a logon re-pin of `LiveRuntimeStatus`.

`CREDENTIALS_AND_COPY_STATUS.md` / older “forced false” notes are **STALE** against HEAD.

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

`VenueReconciled` is a **const false** (`CopyTradingService.cs` L20). Evaluate is called with `Reconciled = VenueReconciled` (L304), so `AllowFixSend` from `RiskEngine` is already false on OPEN (`!Reconciled && IsIncreasing` → `VENUE_NOT_RECONCILED` at RiskEngine L84–85). The live branch is dead twice.

**Dest profit is not booked.** `EfDashboardQueries.GetOverviewAsync` passes `destinationRealPnl: 0` (literal, L44). Trader-row `ShadowPnl` is literal `0` (L118). Overview `shadowPnl` is `Sum(SourceVsShadowSlippage)` (L29) — slippage, not dest cash.

**Assigned UI is not a profit tape.** `LiveCopyPage.tsx` shows `realCopyArmed`, SHADOW/LIVE counts, `liveSends`, intents, shadow fills, QUOTE/TRADE up/down. No dest PnL column. Empty-state text admits demo dest auto-send; that is a **status string**, not a measured edge:

```56:58:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      {intents.length === 0 && (
        <p className="text-gray-500 text-sm">No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.</p>
      )}
```

**Honesty residual (does not revive a live profit path):** HEAD `NewOrderSingleImplemented => DemoDest` (L50; const-false reports are **STALE**). `CopyTradingHostedService` 20s tick calls `ExecuteDemoCopyAsync`, which can `CTraderFixCopyOpen.Build("D")` on a demo host/sender and **refuses** account `1369850` (`CTraderFixCopyOpen.cs` L37–42). That send **bypasses** `RiskEngine.Evaluate`. Demo ledger `D:\Prop\data\demo_copy_ledger.json` records source `305750` / dest pos `237339770` / 0.01 lots — a dest **fill**, still **not** `DestinationRealPnl`. Sending that ticket is not the profit path: dest cash is unmeasured, α is 1:1 (`AllocationFactor=1m`), and copy-all 8463 would import `RISK_BLOCKED` source losses.

Live GET `/api/copy/status` / `/api/overview` **not** obtained (SSRF). File constructors are enough.

---

## 5. SHADOW on demo is not destination profit — PASS

`BaselineScorer` `SHADOW` is a **source-book** label (quality/risk on reconstructed XAU `NetPnl`). It is not dest cash.

`ShadowCopyEngine.SimulateEntry` / `SimulateExit` write paper fills (modeled 0.05-point latency slip). `GenerateShadowIntentsAsync` only calls `SimulateEntry` after `SHADOW_ONLY` (CopyTradingService L336–360). No dest ticket, no dest mark.

Dashboard:

| Field | Constructor | Meaning |
|---|---|---|
| `OverviewDto.DestinationRealPnl` | literal `0` | dest cash **not computed** |
| `OverviewDto.ShadowPnl` | `Sum(ShadowOrders.SourceVsShadowSlippage)` | slip, not dest PnL |
| `TraderRowDto.ShadowPnl` | literal `0` | unused |

`CopyGroupFilter.IsDemoOrContest` is **required** for roster admit and policy eligibility (`NOT_DEMO_OR_CONTEST_GROUP`). So the SHADOW copy set is **demo/contest source groups**. That is adverse-selection of challenge books, not destination profit.

`XauUsdOneToOneCopyPolicy.IsTraderEligible` uses **source** `XauNetPnl` (must be `> 0`) and `State` not in `{INSUFFICIENT_DATA, EARLY_SCORE, WATCH}` (`TRADER_NOT_SHADOW_YET`). Eligibility is source history, not dest marks.

`LiveCopyPage.tsx` L14 “SHADOW traders” is a **count** from `/api/copy/status`. It does not display dest PnL.

On-disk demo ledger fill is a dest position on demo `5328266`. The product still reports dest PnL as **0**. A demo fill is not destination profit in this system.

---

## 6. What this slot did **not** prove

- Runtime `GET /api/health` `realCopyEnabled` (SSRF). File path is `.env=true` + DI bind + no re-pin.
- Current SHADOW count, dest mark-to-market of dest `237339770`, or whether the API host is running.
- Tree-wide “product `35=D=0`” (false; sibling `Build("D")` exists).
- That demo dest cannot lose money if the host is up. It can. That is **not** live `1369850` capital.

---

## 7. Risk to capital

| Book | Risk | Why |
|---|---|---|
| Live Pepperstone `1369850` | **NONE** | `CTraderFixSession` outbound is only `35=A`. `CTraderFixCopyOpen` refuses this account. Persist `AllowFixSend=false`. `CanPromoteToLive => false`. |
| Demo dest `5328266` | **RESIDUAL** | Hosted 20s `ExecuteDemoCopyAsync` can send `35=D` without Evaluate / without `RealCopyEnabled` check. Ledger already has a 0.01 dest fill. Not booked as dest PnL. |
| Copy-all 8463 if a live sender existed | **HIGH / ruin** | Would clone `RISK_BLOCKED` source losses. Wanting profit ≠ edge. |

This slot: no send, no secret, no source edit.
