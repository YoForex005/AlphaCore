# P500_VERIFY_1 — Adversarial verifier (slot 1)

| Field | Value |
|---|---|
| Slot | **1** |
| Date | 2026-08-18 |
| Role | Adversarial verifier. Read the four assigned files first. Fail any claim not proven from a file or a live GET this slot. |
| Product source edited | **No** |
| Test source edited | **No** |
| Live `35=D` sent this slot | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None** (boolean flags + already-public dest ids `5328266` / `1369850` only) |
| Live GET this slot | **Blocked.** `web_fetch` / `open_page` to `http://localhost:5000/api/*` failed (localhost SSRF / retrieve error). Runtime numbers from sibling reports are **not** accepted as proof. |
| Verdict | **FAIL** |

## Assigned claims

Confirm from the live tree (not prior swarm notes):

1. No `35=D` builder.
2. `CanPromoteToLive` is false.
3. `RealCopyEnabled` is forced false after logon.
4. Sending now cannot be the profit path.
5. SHADOW on demo is not destination profit.

Rule: **FAIL any claim that cannot be proven from a file or a live GET performed this slot.**

## Files read this slot

| Path | Why |
|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135) | Assigned. Outbound FIX builder. |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212) | Assigned. `CanPromoteToLive`. |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` (189/189) | Assigned. `AllowFixSend`. |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (70/70) | Assigned. UI SHADOW vs dest send. |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | Residual `Build("D")` + live-id refuse. |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | Residual `Build("D")` ×3. |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | Residual `Build("D")`. |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon. **No** `RealCopyEnabled` write. |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Sole `RealCopyEnabled =` assignment. |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Flag is settable; snapshot text only. |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | Persist `AllowFixSend=false`; demo send hop. |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 20s tick calls `ExecuteDemoCopyAsync`. |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | `ShadowPnl` vs dest ctor `0`. |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | `DestinationRealPnl` field. |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | Paper `SimulateEntry`. |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | No-lookahead; SHADOW gate. |
| `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs` | ADMITTED roster ≠ dest PnL. |
| `D:\Prop\src\Infrastructure\Copy\DemoCopyLedger.cs` | Dest fills are a separate store. |
| `D:\Prop\data\demo_copy_ledger.json` | One 0.01 dest row; no PnL field. |
| `D:\Prop\apps\api\Program.cs` | `/api/health` + `/api/settings` bind runtime flag. |
| `D:\Prop\apps\web\src\pages\OverviewPage.tsx` | Paints `shadowPnl` as “Shadow P&L”. |
| `D:\Prop\.env` L49–50, L64, L73, L106 | Booleans + public host/account only. |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | Three winners → SHADOW; `CanPromoteToLive` false. |

## Scorecard

| # | Claim | Verdict | Proof status |
|---|---|---|---|
| 1 | No `35=D` builder | **PASS_SESSION / FAIL_PRODUCT** | Proven: `CTraderFixSession` has **zero** `35=D` builders. **Not** proven product-wide: `CTraderFixCopyOpen.Build("D")` exists and is on the hosted 20s tick. |
| 2 | `CanPromoteToLive` is false | **PASS** | File-proven `=> false`. Unused `current` argument. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | **Disproven.** Sole assignment is DI from env; lab `.env` L73 is `true`; logon host only **logs** the flag. |
| 4 | Sending now cannot be the profit path | **PASS** | File-proven: live `1369850` refused; scorer never emits LIVE; persist hop never sends; dest ctor `$0`; policy forbids lookahead. Demo `35=D` is **not** a measured edge. Copy-all dollar figures **not** re-summed (GET blocked). |
| 5 | SHADOW on demo is not destination profit | **PASS** | File-proven: `TraderState.SHADOW` + `ShadowOrders.SourceVsShadowSlippage` ≠ dest PnL. Dest ctor is literal `0`. Ledger has no PnL field. |

**Aggregate: FAIL** because claim 3 is false, and claim 1 is false if read as a product statement.

---

## 1. No `35=D` builder — PASS_SESSION / FAIL_PRODUCT

Assigned file `CTraderFixSession.cs` (135 lines):

- One outbound builder: `BuildLogon` with `(35, "A")` only.

```89:109:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
    private static string BuildLogon(
        string sender, string target, string senderSub, string targetSub,
        string username, string password, int seq)
    {
        var sendingTime = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            ...
        };
        return Assemble(fields);
    }
```

- One `WriteAsync` (L49): the logon bytes. Socket/SSL disposed on return.
- `Extract(..., "35")` is **inbound** parse of the logon reply (`"A"` vs reject). Not a builder.
- Grep of this file: `35=D` / `Build("D")` / `NewOrderSingle` = **0**.

Unqualified claim **FAIL**. Product `Build("D")` sites (this slot, `*.cs` grep):

| File | Sites |
|---|---|
| `CTraderFixCopyOpen.cs` L95 | hosted dest open/close |
| `CTraderFixDemoMatrix.cs` L93 | matrix helper |
| `CTraderFixDemoTestTrade.cs` L139, L163, L197 | CLI helper |

`CopyTradingHostedService` L28–30 ticks `ExecuteDemoCopyAsync`, which calls `CTraderFixCopyOpen.SendAsync` when `DemoDest` is true. `.env` host is `demo-us-eqx-01.p.c-trader.com`, sender `demo.pepperstone.5328266`, account `5328266` (≠ `1369850`). That is a live-wired `35=D` builder on demo dest.

Live `1369850` is hard-refused:

```37:42:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
        if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
            || !sender.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
            || account == "1369850")
        {
            return Fail("Refused: dest must be demo FIX, not live 1369850.", host, account, sourceLogin);
        }
```

Prior notes that say “product `35=D=0`” / “`NewOrderSingleImplemented` const false” are **STALE**. HEAD is `NewOrderSingleImplemented => DemoDest` (`CopyTradingService.cs` L50).

---

## 2. `CanPromoteToLive` is false — PASS

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

`FromBaseline` ceiling is `TraderState.SHADOW` (quality ≥ 70 and risk < 40). It never returns `LIVE` or `LIVE_CANDIDATE`.

Unit pin: `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` asserts `SuggestedState == SHADOW` and `CanPromoteToLive(...) == false`.

---

## 3. `RealCopyEnabled` forced false after logon — FAIL

**Disproven from files.** There is no post-logon write of `false`.

1. Sole assignment in product `*.cs`:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

2. Lab `.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only). API `Program.cs` L10 loads that file via `EnvFile.FindAndLoad()` before DI.

3. `CTraderFixLogonHostedService` after both `TryLogonAsync` calls (L60–70) writes Quote/Trade `LoggedOn` / `Status` / `LastError` only. It **logs** `_runtime.RealCopyEnabled`. It does **not** assign it.

4. `CTraderFixSession` has zero tokens of `RealCopyEnabled`.

5. `LiveRuntimeStatus.RealCopyEnabled` is a public settable auto-property. Nothing after logon pins it false.

6. POCO default `CTraderFixOptions.RealCopyExecutionEnabled = false` is unused by DI (nested `CTrader:` key is not the bind path).

Live GET of `/api/health` / `/api/settings` / `/api/ingest/status` was **not** obtained this slot. That does not rescue the claim: the source cannot force the flag false after logon, and the env value it would bind is `true`.

---

## 4. Sending now cannot be the profit path — PASS

Proven as **“even if a ticket leaves, that is not a measured profit path.”** Not proven as “there is no sender.”

File chain:

| Gate | What the file does |
|---|---|
| Scorer | Never emits `LIVE`. `CanPromoteToLive => false`. |
| Shadow hop | `CopyTradingService` L324 persists `AllowFixSend = false` (literal, ignores `decision.AllowFixSend`). L330 live-send branch also requires `TraderState.LIVE` and `VenueReconciled`. |
| `VenueReconciled` | `public const bool VenueReconciled = false` (L20). Evaluate therefore hits `VENUE_NOT_RECONCILED` on increasing actions (`RiskEngine` L84–85). |
| `RiskEngine` L147–150 | `AllowFixSend` is `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. Persist then throws that away. |
| Live identity | `CTraderFixCopyOpen` refuses non-`demo-` host, non-`demo.` sender, and account `1369850`. |
| Policy | `XauUsdOneToOneCopyPolicy` copies the **next** XAU event 1:1. Comment L58–61: does **not** wait until a ticket is profitable (lookahead). |
| Dest dashboard | `GetOverviewAsync` L44–46: `destinationRealPnl` / `xauGross` / `xauNet` are constructor literals **`0`**. |
| UI | `LiveCopyPage.tsx` has **no** send button. It paints `realCopyArmed` and blockers. Empty copy: demo dest auto-sends after **ADMITTED**, not after a measured dest edge. |

`ExecuteDemoCopyAsync` **bypasses** `RiskEngine.Evaluate` and can emit `35=D` on demo dest. That is a send path. It is **not** a profit path:

- Dest account is demo (`5328266`), not live `1369850`.
- Admission is source-trader filters (SHADOW+, ≥20 XAU, source XAU net > 0, demo/contest group), not dest EV after costs.
- `AllocationFactor = 1m` (1:1). `MaxAutoLots = 0.05` only skips larger tickets; it does not size for expectancy.
- Dashboard dest PnL is hardcoded `0`. Ledger (`data/demo_copy_ledger.json`) stores `DestFillPrice` / `DestClosed` only — **no** dest realized PnL field. One open 0.01 long (`305750` / `21250421` / dest `237339770` / px `4390.2` / `DestClosed: false`).

Copy-all 8463 / `RISK_BLOCKED` −$241,580 / scored XAU −$154,425: **not proven this slot** (GET blocked; not re-summed from a file I computed). Those sibling integers are **not** used as evidence.

Wanting profit is not an edge. Sending now (demo or otherwise) is not a proven profit path.

---

## 5. SHADOW on demo is not destination profit — PASS

`SHADOW` is a **source-trader state**, not dest cash.

```200:201:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        if (quality >= 70 && risk < 40)
            return TraderState.SHADOW;
```

Paper path (`GenerateShadowIntentsAsync`):

- Status forced `SHADOW_ONLY` unless the unreachable LIVE+reconciled branch.
- Fills are `ShadowCopyEngine.SimulateEntry` (quote slippage, `DefaultLatencySlippagePoints = 0.05`).
- Overview `ShadowPnl` = `Sum(ShadowOrders.SourceVsShadowSlippage)` (`EfDashboardQueries` L29). That is **slippage**, not dest PnL.
- `TraderRowDto.ShadowPnl` is hardcoded `0` at L118.

Dest path is a **different object**:

- Roster `ADMITTED` + `ExecuteDemoCopyAsync` + `DemoCopyLedger` + `CTraderFixCopyOpen`.
- `DestinationRealPnl` ctor `0`.
- Ledger JSON has no PnL column.

`LiveCopyPage.tsx` L14 paints `status.shadowTraders` (count of `TraderState.SHADOW`). L18 paints `shadowFills` (`ShadowOrders` count). L57 empty-state talks about demo dest auto-send after ADMITTED. Those are three different things. The page never treats SHADOW count or shadow fills as dest profit.

`OverviewPage.tsx` L27 labels the slippage sum “Shadow P&L”. That is a UI lie about **shadow**, not dest. Dest card L28 is the ctor `0`.

Policy: `WATCH` / `EARLY_SCORE` / `INSUFFICIENT_DATA` fail with `TRADER_NOT_SHADOW_YET`. Being SHADOW is a **source eligibility gate**, not dest money.

---

## RiskEngine residual (does not flip claims 4–5)

`RiskEngine` L90–93: when `RealExecutionEnabled == false` the body is an empty comment. It does **not** `return` and does **not** force `AllowFixSend=false` at that point. Later `allowSend` uses the flag. Persist L324 is the hop that zeros send. Demo dest send **never calls** `Evaluate`.

`RiskDashboardDto` daily/drawdown/XAU fields are also ctor `0` (`GetRiskAsync` L208). Not dest profit.

---

## Live GET

Attempted:

- `GET http://localhost:5000/api/health`
- `GET http://localhost:5000/api/overview`
- `GET http://localhost:5000/api/copy/status`
- `GET http://localhost:5000/api/settings`
- `GET http://localhost:5000/api/risk`
- `GET http://localhost:5000/api/ingest/status`

All blocked (localhost SSRF / retrieve failure). `_tmp_e032` on-disk bodies are **stale dummy** (`10001`/`10002`) and are **not** used.

Claims 1 (session), 2, 4, 5 stand on files. Claim 3 is file-disproven. No claim is passed on a GET I did not perform.

---

## Risk to capital

- **Live Pepperstone `1369850`:** **NONE** (`SAFE_BY_ABSENCE`). `CTraderFixCopyOpen` / `CTraderFixDemoTestTrade` refuse that account and any non-demo host/sender. `CTraderFixSession` cannot emit `35=D`.
- **Demo dest `5328266`:** sender **exists** on the 20s hosted tick. Ledger shows one unmarked 0.01. That is **demo**, not live capital. Not a profit path.
- This slot sent **no** orders.

---

## Honesty

- Did not confirm product-wide “no `35=D` builder.”
- Did not confirm `RealCopyEnabled` forced false after logon (the opposite is in the tree).
- Did not invent dest profit from SHADOW counts or slippage sums.
- Did not reuse sibling copy-all dollar figures as this-slot proof.
- Did not print passwords or FIX secrets.
