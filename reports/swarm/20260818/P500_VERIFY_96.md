# P500_VERIFY_96 — Adversarial four-file verify (slot 96)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_96.md` |
| Agent / slot | P500 adversarial verifier **96** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (live `src/`, `apps/`, lab `.env`) |
| Assigned SUT (read in full this slot) | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135) · `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212; `CanPromoteToLive` is on `TraderStateMachine` in the same file) · `D:\Prop\src\Domain\Risk\RiskEngine.cs` (189/189) · `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (70/70) |
| Supporting files (claims 1/3/4/5 hop; re-read this slot) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `CopyRosterEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyGroupFilter.cs`, `ShadowCopyEngine.cs`, `EfDashboardQueries.cs`, `EfTradingStore.cs`, `DashboardModels.cs`, `apps/api/Program.cs`, `data/demo_copy_ledger.json`, `.env` L49–50 / L64 / L73 (booleans + public dest ids only) |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Live `35=D` sent this slot | **No** (this slot is read-only verify) |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None.** Quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73) and public dest/source ids `5328266` / `1369850` / `305750` / `237339770` / `21250421`. No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live GET this pass | `GET http://127.0.0.1:5000/api/health`, `http://127.0.0.1:18720/api/health`, `http://localhost:5000/api/health` **blocked** (loopback SSRF). Runtime `realCopyEnabled` **not** live-proven. File proof is enough to **FAIL** claim 3. Unmeasured dest mark-to-market **FAIL**s any “dest cash is $0” claim. |
| Binding rule | **FAIL any assigned claim that cannot be proven from a live file or a live GET.** Sibling swarm books are not evidence. This slot re-read the four SUT files and the hop files. |

**Honesty:** Wanting dest profit is not an edge. `SAFE_BY_ABSENCE` on live `1369850` is **not** “`RealCopyEnabled` stays false.” An armed env bit with no live sender is still an armed bit. Demo dest send is dest **exposure**, not dest-profit accounting. `DestinationRealPnl` constructor `0` is not a mark-to-market of dest `5328266`. SHADOW paper slippage is not dest cash. This slot sent **0**.

---

## 0. Verdict (binding)

**FAIL.** The five-claim bundle does **not** pass. Claim 2 is file-proven. Claims 1 / 3 / 4 fail as written (unscoped). Claim 5 is paper-proven, not dest-safety-proven.

| # | Assigned claim | Measured | Class |
|---|---|---|---|
| 1 | no `35=D` builder | **PROVEN** on `CTraderFixSession.cs` (135/135, only `(35, "A")`). **DISPROVEN** product-wide: sibling `Build("D")` ×5, one hosted. | **FAIL** unscoped / **PASS_SESSION** |
| 2 | `CanPromoteToLive` is false | **PROVEN** (`=> false`; unused `current`) | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **DISPROVEN** — only assignment is DI bind of `.env=true`; logon host never writes the bit | **FAIL** |
| 4 | sending now cannot be the profit path | Session/persist hop cannot book dest profit (`AllowFixSend=false`; dest DTO constructor `0`). Hosted demo hopper **can send `35=D` now** and ledger dest is still open. Venue dest P&L **unproven** (no live GET). | **FAIL** unscoped / **PASS_NOT_BOOKED_DEST_PROFIT** |
| 5 | SHADOW on demo is not destination profit | **PROVEN** as paper/source ≠ dest cash (`SimulateEntry` / slippage sum / dest DTO `0`). **NOT proven** as dest-safety: SHADOW is AUTO_ADMIT floor → hopper `35=D`. | **PASS_PAPER** / **FAIL_AS_DEST_SAFETY** |

One-line:

```text
FAIL slot 96: CTraderFixSession 35=A only (no D builder); product Build("D")×5 hosted; CanPromoteToLive=>false; RealCopyEnabled NOT forced false after logon (.env true + DI bind + logon logs-only); send-now is not booked dest-profit (DTO 0) but demo dest hopper can 35=D now; SHADOW-on-demo is source/paper not dest PnL (residual AUTO_ADMIT). Risk NONE on live 1369850. Live GET blocked.
```

Do **not** treat this FAIL as a license to send. Do **not** flip leftover `.env=true` into go-live. Operator should restore `REAL_COPY_EXECUTION_ENABLED=false` (this slot did **not** edit it).

---

## 1. Claim 1 — no `35=D` builder — FAIL unscoped / PASS_SESSION

### 1.1 Assigned file `CTraderFixSession.cs` (135/135)

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` read in full. The only outbound builder is `BuildLogon`. Tag 35 is hardcoded `"A"`:

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

Single `WriteAsync` (L49) of that logon. Then one `ReadAsync` (L53). Socket disposed via `using`. Inbound `Extract(reply, "35")` (L55, L122–134) is reply parse, not a builder. Tokens in this file: `Build("D")` = 0, `(35, "D")` = 0, `NewOrderSingle` = 0.

Hosted caller is logon-only (`CTraderFixLogonHostedService.cs` L48–58): two `TryLogonAsync` calls (QUOTE 5211, TRADE 5212). No send of D.

**PASS_SESSION:** the assigned session class cannot build NewOrderSingle.

### 1.2 Product-wide (required because the claim is unscoped)

The assigned claim is “no `35=D` builder”, not “`CTraderFixSession` has no `35=D` builder.” Unscoped claim is **DISPROVEN**:

| File | `Build("D"` count | Wired to hosted hop? |
|---|---|---|
| `src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 | 1 | **Yes** — `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566; hopper `CopyTradingHostedService` L30 every 20s |
| `src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | 3 | Tools path; live `1369850` / `live-*` / `live.*` refused (L43–47) |
| `src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` L93 | 1 | Tools path; same demo gate (L22–28) |

Product `Build("D"` **×5**. Hosted one is `CTraderFixCopyOpen.SendAsync` → `Build("D", …)` after `35=A` logon + `35=x` SecurityList:

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

Live identity refuse (L37–42): dest must be `demo-` host + `demo.` sender + account **≠** `1369850`. That is a **live-account** gate, not “no builder.”

**Claim 1 as written: FAIL.** Session-scoped: **PASS_SESSION**.

---

## 2. Claim 2 — `CanPromoteToLive` is false — PASS

Same file as assigned `BaselineScorer.cs`. The scorer never returns `LIVE`. Highest auto state is `SHADOW`. Promotion function is a constant false:

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

`current` is unused. Enum has `LIVE_CANDIDATE` / `LIVE` (`TraderState.cs` L9–10) but `FromBaseline` never emits them. Unit pin: `tests\Unit\BaselineScorerTests.cs` L21–26 (`Three_disciplined_winners_go_to_shadow_not_live`).

`LiveCopyPage.tsx` does not call `CanPromoteToLive`. `RiskEngine` does not promote.

**Claim 2: PASS.** Proven from assigned file.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL (disproven)

None of the four assigned files write `RealCopyEnabled` at all.

| Location | What it does to the bit |
|---|---|
| `DependencyInjection.cs` L39–42 | **Only assignment in product `src/`**: `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` |
| `.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret) |
| `apps/api/Program.cs` L10 + L13 | `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()` — feeds DI |
| `CTraderFixLogonHostedService.cs` L60–70 | Sets `Quote`/`Trade` logon flags. **Reads** `_runtime.RealCopyEnabled` for a log line. **Never assigns it.** |
| `LiveRuntimeStatus.cs` L32 | Public `{ get; set; }`. Default `false` until DI sets it. No post-logon hook. |
| `CTraderFixSession.cs` | No runtime type. Cannot pin the bit. |
| Product grep `RealCopyEnabled\s*=` | **1 hit**: DI L41 |

Logon host (the “after logon” site):

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

API `/api/settings` and `/api/health` **echo** `runtime.RealCopyEnabled` (`Program.cs` L55, L76). They do not force false. Live GET of those routes is **blocked** this slot — so I do **not** claim the running process is `true`. I **do** claim the source cannot force it false after logon, and the only bind is `.env=true`.

`RiskEngine` L90–93 is a **comment-only** no-op when `RealExecutionEnabled == false`. It does not write `LiveRuntimeStatus.RealCopyEnabled`. `allowSend` (L147–150) **follows** the request flag; it does not pin the runtime bit.

`LiveCopyPage.tsx` L13 displays `status?.realCopyArmed ? 'YES' : 'NO'`. Display is not a force-false.

**Claim 3: FAIL.** The assigned statement is false on disk.

---

## 4. Claim 4 — sending now cannot be the profit path — FAIL unscoped / PASS_NOT_BOOKED_DEST_PROFIT

### 4.1 What the four assigned files can prove

| File | Send? | Books dest profit? |
|---|---|---|
| `CTraderFixSession` | Logon `35=A` only | No |
| `BaselineScorer` | No sender | Scores **source** reconstructed PnL |
| `RiskEngine` | No socket | `AllowFixSend` **can be true** on Approve if `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–170). `Reject` sets `AllowFixSend=false` (L187). Empty L90–93 does **not** hard-return. |
| `LiveCopyPage.tsx` | No FIX | Empty-state copy **admits dest send**: “Demo dest auto-sends after a trader is ADMITTED…” (L57) |

Session cannot be the profit path. That is **not** the whole product.

### 4.2 Persist hop cannot book dest profit

`CopyTradingService.GenerateShadowIntentsAsync` persists risk with a **literal false**, ignoring `decision.AllowFixSend`:

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

Evaluate on that hop also passes `Reconciled = VenueReconciled` and `VenueReconciled` is `const false` (`CopyTradingService.cs` L20). So even if `.env` arms `RealCopyEnabled`, Evaluate `allowSend` is false on the shadow hop.

Dashboard dest cash is a **constructor zero**, not a venue mark:

```33:52:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
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
            ...
            _runtime.RealCopyEnabled);
```

`OverviewDto.DestinationRealPnl` is that `0` (`DashboardModels.cs` L16). `GetRiskAsync` returns `RiskDashboardDto(0, 0, 0, 0, 0, …)` (`EfDashboardQueries.cs` L208). **Constructor, not a mark.** Cannot prove dest cash is $0 from this DTO.

**PASS_NOT_BOOKED_DEST_PROFIT** on the persist/dashboard hop.

### 4.3 Hosted hopper **is** a dest send path now

`.env` dest identity (public): host `demo-us-eqx-01.p.c-trader.com` (L49), account `5328266` (L50), trade sender `demo.pepperstone.5328266` (L64). That makes `DemoDest == true`:

```45:50:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public bool DemoDest =>
        (_config["CTRADER_FIX_HOST"] ?? "").StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
        && (_config["CTRADER_FIX_TRADE_SENDER_COMP_ID"] ?? "").StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
        && _config["CTRADER_FIX_ACCOUNT_ID"] != "1369850";

    public bool NewOrderSingleImplemented => DemoDest;
```

Hopper (`CopyTradingHostedService.cs` L21–41): every 20s, `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**. The last call **does not** consult `RiskEngine.Evaluate`, `AllowFixSend`, `CanPromoteToLive`, or `RealCopyEnabled`. It calls `CTraderFixCopyOpen.SendAsync` (L528 close / L566 open) which writes `35=D`.

Seeded residual dest (file, not a live GET):

```1:12:D:\Prop\data\demo_copy_ledger.json
[
  {
    "SourceLogin": "305750",
    "SourcePositionId": "21250421",
    "IsLong": true,
    "Lots": 0.01,
    "DestPositionId": "237339770",
    "DestClOrdId": "C20260818093047317",
    "DestFillPrice": 4390.2,
    "DestClosed": false
  }
]
```

`ExecuteDemoCopyAsync` L500–512 **re-inserts** that 305750 / 21250421 / dest `237339770` row if missing. `DestClosed: false` means the dest ticket is treated as **still open**. Venue P&L of that ticket is **unproven** (no live GET).

`GetStatusAsync` summary when `DemoDest` (L76–77): “Demo dest auto-copy ON. Eligible demo/contest opens send on the 20s tick… Live 1369850 is never used.” The UI repeats this (`LiveCopyPage.tsx` L57).

Unscoped “sending now cannot be the profit path” is **false**: sending now **can** open/close dest tickets on demo `5328266`. Those tickets **can** make or lose dest money. Dashboard not booking them does not make them non-profit.

Live `1369850` is refused (`CTraderFixCopyOpen` L37–42). That is **SAFE_BY_ABSENCE** on the **live** dest, not “no send path.”

**Claim 4 as written: FAIL.** Booked dest-profit on dashboard: **PASS_NOT_BOOKED_DEST_PROFIT**.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — PASS_PAPER / FAIL_AS_DEST_SAFETY

### 5.1 Paper / source (proven)

- `TraderState.SHADOW` is a **source** state (`TraderState.cs` L8). `FromBaseline` emits it at quality≥70 / risk<40. It is not dest cash.
- `ShadowCopyEngine.SimulateEntry` (L35–61) writes modeled ask/bid + 0.05 latency slip. `SourceVsShadowSlippage` is a spread-vs-source number, not venue dest PnL.
- Copy persist path (`CopyTradingService` L337–359) and `EfTradingStore.PersistDemoShadowAsync` (L267–333) both call `SimulateEntry` and store `ShadowOrder` rows. Status `"SHADOW_ONLY"`.
- Dashboard `ShadowPnl` = `Sum(SourceVsShadowSlippage)` (`EfDashboardQueries.cs` L29). That is **not** dest profit.
- `DestinationRealPnl` is constructor `0` (same file L44). Not a dest mark.
- `CanPromoteToLive => false` so the scorer cannot promote SHADOW → LIVE.

**PASS_PAPER:** the SHADOW ledger is not dest profit.

### 5.2 Dest-safety (not proven; residual is dest exposure)

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET` (L81–85). **SHADOW passes that state gate.** Combined with ≥20 XAU, XAU book > 0, no size-pattern, and **demo/contest group** (`CopyGroupFilter.IsDemoOrContest`, L105–109), a SHADOW demo source is eligible.

`CopyRosterEngine.Decide` AUTO_ADMITs any eligible trader not already on roster (L72–80). Hopper then `35=D`s dest for ADMITTED seats with open XAU (`ExecuteDemoCopyAsync` L542–598). SHADOW is therefore a **dest AUTO_ADMIT floor**, not a dest-profit lockout.

`LiveCopyPage.tsx` L14 counts “SHADOW traders” from copy status (source scores). Empty copy (L57) tells the operator dest auto-sends after ADMITTED. The page does not claim SHADOW = dest cash; it also does not claim dest cannot fill.

I **cannot** prove dest `5328266` venue PnL from a file or a live GET. I **cannot** prove SHADOW-on-demo cannot become dest P&L. I **can** prove dest send is wired after AUTO_ADMIT.

**Claim 5 as dest-safety: FAIL.** As paper ≠ dest cash: **PASS_PAPER**. Bundle treats dest-safety as the safety-relevant reading → residual **FAIL_AS_DEST_SAFETY**.

---

## 6. Live GET

Attempted this slot (read-only):

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | **SSRF blocked** (loopback) |
| `http://127.0.0.1:18720/api/health` | **SSRF blocked** |
| `http://localhost:5000/api/health` | **SSRF blocked** |

No `/api/copy/status`, `/api/overview`, `/api/settings` body. Runtime `realCopyEnabled`, FIX logon, and dest mark are **unproven live**. File proof still **FAILs** claim 3. File proof still **FAILs** unscoped claims 1 and 4. Any “process is currently `realCopyEnabled=false`” or “dest cash is $0” statement from this slot would be a **FAIL** (unproven).

---

## 7. Risk to capital

| Dest | Measured |
|---|---|
| Live Pepperstone `1369850` | **NONE** — `CTraderFixCopyOpen` / demo tools refuse this account. `CTraderFixSession` cannot send D. Persist `AllowFixSend=false`. `CanPromoteToLive=>false`. **SAFE_BY_ABSENCE** on live dest. |
| Demo dest `5328266` | **NOT absent.** Hosted 20s hopper can `Build("D")`. Ledger dest `237339770` (0.01 lot, source `305750`/`21250421`) is `DestClosed: false`. Venue PnL **unmeasured** (no live GET; dashboard dest is constructor `0`). |
| This slot | Sent **0**. Did not flip `.env`. Did not edit product. |

`SAFE_BY_ABSENCE` on live ≠ `RealCopyEnabled` forced false. Armed `.env=true` + demo hopper is dest **exposure**, not a profit edge.

---

## 8. What this slot did **not** do

- Did not send FIX.
- Did not edit `.env`, product, or tests.
- Did not print secrets.
- Did not treat sibling `P500_VERIFY_*` books as evidence (re-read files).
- Did not invent dest MTM.
- Did not claim EX5 / ML / ≥95% anything.

---

## 9. Operator note (not executed)

Restore `REAL_COPY_EXECUTION_ENABLED=false` if the intent is “armed bit stays false.” That is **not** a substitute for deleting `CTraderFixCopyOpen.Build("D")` from the hosted hopper. This slot did not change either.
