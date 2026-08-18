# P500_VERIFY_17 — Adversarial four-file verify (slot 17)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **17** |
| Role | Adversarial verifier. Read live files. Do not trust other agents. FAIL any claim not proven from a file or live GET. |
| SUT | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` |
| Product source modified | **No** |
| Secrets printed | **None** (quoted only already-on-disk booleans `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live GET this slot | **Blocked** (`web_fetch` SSRF-refuses `127.0.0.1:5000` / `localhost:5000`). No runtime `/api/copy/status` or `/api/health` attach. File proof only. |
| Overall verdict | **FAIL** |

**Law used:** confirm all five assigned claims. Overall **FAIL** if any claim is false or unproven. Wanting a green card does not create proof. Prior swarm “product `35=D=0` / `NewOrderSingleImplemented=const false` / hosted hop never sends” cards are **STALE** against HEAD.

Assigned claims:

1. No `35=D` builder
2. `CanPromoteToLive` is false
3. `RealCopyEnabled` forced false after logon
4. Sending now cannot be the profit path
5. SHADOW on demo is not destination profit

---

## Scorecard

| # | Claim | File-proven? | Verdict |
|---|---|---|---|
| 1 | no `35=D` builder | **Session yes / product no** | **PASS_SESSION; FAIL_PRODUCT** |
| 2 | `CanPromoteToLive` is false | **Yes** (`BaselineScorer.cs` L211) | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **No** — opposite is on disk | **FAIL** |
| 4 | sending now cannot be the profit path | **Yes for live / measured dest**; demo dest send exists | **PASS_LIVE_AND_MEASURED** |
| 5 | SHADOW on demo is not destination profit | **Yes** | **PASS** |

**Overall: FAIL** because claim 3 is false on the live path, and the unqualified claim “no `35=D` builder” is false for the product (hosted demo hop calls `CTraderFixCopyOpen.Build("D")`).

Risk to **live** capital today: **NONE** (`CTraderFixSession` is `35=A` only; `CTraderFixCopyOpen` refuses account `1369850`; persist `AllowFixSend=false`; `CanPromoteToLive => false`). Risk to **demo dest** `5328266` is **not** `SAFE_BY_ABSENCE` if the 20s hosted tick is running. This slot sent **0**.

---

## 1. PASS_SESSION / FAIL_PRODUCT — no `35=D` builder

Live file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135).

Outbound builder is only Logon:

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

File census (this slot, full read):

| Token in `CTraderFixSession.cs` | Count |
|---|---|
| outbound `(35, "A")` | **1** (`BuildLogon`) |
| `Build("D")` / `(35, "D")` / `NewOrderSingle` | **0** |
| `WriteAsync` | **1** (the logon bytes) |
| inbound `Extract(..., "35")` | reads reply type only (`A` → LoggedOn) |

**Assigned session class has no `35=D` builder. Proven.**

Unqualified product claim **fails**. Sibling builders exist and one is on the hosted copy tick:

| File | Evidence |
|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra.ToArray())` |
| same file L37–42 | refuses live identity (`account == "1369850"` / non-`demo-` host / non-`demo.` sender) |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | `Build("D", ...)` ×3, demo-gated |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` L93 | `Build("D", ...)` |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` L50, L483–605 | `NewOrderSingleImplemented => DemoDest` (const-false is **STALE**); `ExecuteDemoCopyAsync` calls `CTraderFixCopyOpen.SendAsync` |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` L28–30 | 20s tick: `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`** |

`LiveCopyPage.tsx` does not build FIX. It only renders `/api/copy/status` and `/api/copy/intents`.

**Cannot confirm “no `35=D` builder” as a product fact.** Session-only subclaim holds.

---

## 2. PASS — `CanPromoteToLive` is false

Live file: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`.

`TraderStateMachine` lives in the same file (not a separate compilation unit):

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

Proof:

- `CanPromoteToLive` is a constant `false`. Argument `current` is unused.
- `FromBaseline` ceiling is `SHADOW`. There is no `LIVE` / `LIVE_CANDIDATE` return.
- Unit test `Three_disciplined_winners_go_to_shadow_not_live` (`D:\Prop\tests\Unit\BaselineScorerTests.cs` L21–27) asserts `SuggestedState == SHADOW` and `CanPromoteToLive(...).Should().BeFalse()`.

`LiveCopyPage.tsx` L15 shows `LIVE traders` as a count from the API. It has **no** promote control. The page cannot override L211.

**Claim 2 proven from the file.**

---

## 3. FAIL — `RealCopyEnabled` is **not** forced false after logon

Assigned logon host: `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`.

After QUOTE/TRADE `TryLogonAsync` the host writes session health only:

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

There is **no** `_runtime.RealCopyEnabled = false` (or any assignment) in this file. The flag is only **logged**.

The only product assignment is DI at startup, bound to lab env:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

On-disk lab `.env` L73: `REAL_COPY_EXECUTION_ENABLED=true`. API `Program.cs` L10 loads that file via `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()`. `LiveRuntimeStatus.RealCopyEnabled` is a public settable bool (`D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` L32).

`RiskEngine` does **not** pin the runtime flag. When `RealExecutionEnabled == false` it comments and continues (L90–93); `AllowFixSend` is later `request.RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). That is a per-request gate, not a post-logon force-false of `LiveRuntimeStatus.RealCopyEnabled`.

`LiveCopyPage.tsx` L13 displays `status?.realCopyArmed` (`YES`/`NO`). Display cannot force the flag false.

Live GET of `/api/health` / `/api/settings` / `/api/copy/status` was **not** obtained this slot (localhost SSRF block). File proof already **disproves** “forced false after logon.” A live GET could only confirm the bound `true`; it could not rescue the claim.

**Claim 3 FAIL.** Architecture/POCO `CTraderFixOptions.RealCopyExecutionEnabled` still defaults **false** (`CTraderFixOptions.cs` L35). That default is unused by DI. The running API binds lab `.env` `true` and logon does not re-pin.

---

## 4. PASS_LIVE_AND_MEASURED — sending now cannot be the (live / measured) profit path

What “sending now” can mean, and what the four SUTs plus the one hosted caller prove:

### 4a. `LiveCopyPage.tsx` cannot send

File: `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (70 lines).

- Hooks: `useCopyStatus` + `useCopyIntents` — both GET (`D:\Prop\apps\web\src\api\hooks.ts` L60–66).
- No `<button>`, no `POST`, no promote, no “send now”.
- Empty-state copy (L56–57): *“Demo dest auto-sends after a trader is ADMITTED…”* — that text describes the **hosted** hop, not a page action.
- Stats include `REAL_COPY armed`, `SHADOW traders`, `LIVE traders`, `Live sends`. Display is not dest PnL.

### 4b. Assigned session cannot send a ticket

`CTraderFixSession` outbound is `35=A` only (claim 1 session proof). Logon is not a fill.

### 4c. Scorer cannot create a LIVE book

`CanPromoteToLive => false`. `FromBaseline` never returns `LIVE`. The live-send if-branch in `CopyTradingService` L330 requires `score.CurrentState == TraderState.LIVE` **and** `decision.AllowFixSend` **and** `NewOrderSingleImplemented` **and** `VenueReconciled`. `VenueReconciled` is `const false` (L20). Persist always writes `AllowFixSend = false` (L324). That branch is dead. Intents fall through to `SHADOW_ONLY`.

### 4d. RiskEngine cannot mint dest profit

`RiskEngine.Evaluate` never writes a fill, never calls FIX, never updates dest PnL. `AllowFixSend` on Approve still requires `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. Copy hop passes `Reconciled = VenueReconciled = false`, so **increasing** actions `Reject("VENUE_NOT_RECONCILED")` before Approve (L84–85). Closes can Approve with `AllowFixSend=false` when the flag/recon gates fail.

Dashboard dest PnL is a **constructor zero**, not a venue mark:

```29:45:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var shadowPnl = await _db.ShadowOrders.SumAsync(s => (decimal?)s.SourceVsShadowSlippage, ct) ?? 0;
        // ...
            shadowPnl,
            0,
            0,
            0,
```

`OverviewDto` fields (`DashboardModels.cs` L15–17): `ShadowPnl`, `DestinationRealPnl`, `XauGross`, `XauNet`. The three zeros are dest / book placeholders. **Measured destination profit is $0 by construction.** Sending is not booked as profit.

### 4e. Residual that does **not** make sending the profit path

`ExecuteDemoCopyAsync` **can** emit demo `35=D` **without** `RiskEngine.Evaluate` (L483–605; `CTraderFixCopyOpen` L95). Gate is `DemoDest` (demo host + `demo.` sender + account **≠** `1369850`) plus `CopyLifecycle.ShouldOpenDest` / source-complete close. Ledger `D:\Prop\data\demo_copy_ledger.json` records dest fill `305750` / `21250421` / `237339770` / `4390.2` / `DestClosed=false`. That is dest **activity** on demo `5328266`. It is **not** live `1369850`. It is **not** `DestinationRealPnl`. It is **not** a measured edge.

Honesty: wanting dest fills is not an edge. This slot did not remasure catalog EV and does **not** cite other agents’ −$241,580 / 8463 figures as proof.

**Claim 4 proven for live capital and for the product’s measured dest PnL.** Demo dest send exists; it is not the measured profit path.

---

## 5. PASS — SHADOW on demo is not destination profit

### Scorer SHADOW is a source state, not dest money

`FromBaseline` can return `SHADOW` when `quality >= 70 && risk < 40` after three completed XAU trades. Quality formula (`BaselineScorer.cs` L152–160) can be high while `NetPnl` is unused as a hard floor (only `NetPnl > 0` adds +15). SHADOW is a **source-trader** label.

### Paper fills are not dest PnL

`GenerateShadowIntentsAsync` (`CopyTradingService.cs` L200–413) writes `Status = "SHADOW_ONLY"` and, on Approve + quote, `ShadowCopyEngine.SimulateEntry` (`D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` L35–61). That method never opens a socket. Dashboard `ShadowPnl` is `Sum(SourceVsShadowSlippage)` — modeled **slippage**, not dest realized PnL. `DestinationRealPnl` stays `0`.

### Demo-group eligibility is not dest profit

`XauUsdOneToOneCopyPolicy.IsTraderEligible` requires demo/contest group (`CopyGroupFilter.IsDemoOrContest`) plus SHADOW-or-better, 20 XAU, `XauNetPnl > 0`, no size-pattern. That admits **source** challenge books onto the roster. Roster `AUTO_ADMIT` (`CopyRosterEngine.cs` L72–80) writes `CopyIntent.Status = "ADMITTED"`. Paper SHADOW rows are still not dest marks.

### Separate dest hop is still not “SHADOW profit”

If `DemoDest` is true, `ExecuteDemoCopyAsync` can `35=D` for **ADMITTED** open XAU (`MaxAutoLots=0.05`, 1:1 lots). That hop does not read `ShadowOrders` and does not write `DestinationRealPnl`. A SHADOW-state demo trader can be dest-copied; the **SHADOW** artifact remains paper. Dest fills on demo are dest activity, not SHADOW profit.

`LiveCopyPage.tsx` L14 (`SHADOW traders`) is a count. L56–57 describes dest auto-send after ADMITTED. The page does not equate SHADOW with dest PnL.

**Claim 5 proven from the files.**

---

## Cross-SUT matrix (assigned four)

| File | `35=D` builder | Promote to LIVE | Forces `RealCopyEnabled=false` | Send / profit |
|---|---|---|---|---|
| `CTraderFixSession.cs` | **No** (`35=A` only) | n/a | n/a | logon only |
| `BaselineScorer.cs` | n/a | **`CanPromoteToLive => false`** | n/a | ceiling `SHADOW` |
| `RiskEngine.cs` | n/a | n/a | **No** (comment-only L90–93) | `AllowFixSend` computed; copy persist hard-false |
| `LiveCopyPage.tsx` | **No** | **No control** | **No** (displays `realCopyArmed`) | GET display; no POST |

---

## Residuals (do not greenwash)

1. Unqualified “no `35=D` builder” is **false** for HEAD. Hosted 20s tick can `Build("D")` on demo dest. BOOK cards that say product `35=D=0` / `NOS=const false` / hop-never-sends are **STALE**.
2. `RealCopyEnabled` is **armed** from `.env` `true`. Logon host no longer re-pins (older pin-false cards **STALE**).
3. `ExecuteDemoCopyAsync` **bypasses** `RiskEngine.Evaluate` (quote age, spread, martingale, `AllowFixSend`). Live `1369850` still refused.
4. `RiskLimits.MaxSlippage` is unread in `Evaluate`. Not needed to fail claim 3.
5. No live GET this slot. Claim 3 does not need one.

---

## Verdict

**FAIL.**

- (1) `CTraderFixSession` 135/135 has no `35=D` builder — **PASS_SESSION**. Product has `CTraderFixCopyOpen.Build("D")` on the hosted tick — **FAIL_PRODUCT**.
- (2) `TraderStateMachine.CanPromoteToLive => false` — **PASS**.
- (3) `RealCopyEnabled` is **not** forced false after logon. DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`; logon only logs the flag — **FAIL**.
- (4) Live send / page send / measured dest PnL cannot be the profit path (`DestinationRealPnl` constructor 0; session `35=A`; persist `AllowFixSend=false`; `VenueReconciled=false`; `CanPromoteToLive=false`) — **PASS_LIVE_AND_MEASURED**. Demo dest hop exists and is not that path.
- (5) SHADOW (state + `ShadowOrders` + `ShadowPnl` slippage sum) is not destination profit — **PASS**.

Risk to live capital **NONE** (`SAFE_BY_ABSENCE` on `1369850` + session `35=A` only). Demo dest `5328266` is **not** `SAFE_BY_ABSENCE`. This slot sent **0**.
