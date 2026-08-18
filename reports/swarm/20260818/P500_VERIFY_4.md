# P500_VERIFY_4 — Adversarial live-path verify (slot 4)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_4.md` |
| Agent / slot | P500 adversarial **verify 4** |
| Date | 2026-08-18 |
| Role | Independent verifier. **Did not trust** sibling W500 / P500_BOOK / A014 / CREDENTIALS prose. Re-read assigned files this slot, then hop files required to test claims 3–5. |
| Assigned reads | `CTraderFixSession.cs` (135/135), `BaselineScorer.cs` (212/212), `RiskEngine.cs` (189/189), `LiveCopyPage.tsx` (70/70) |
| Supporting reads (claims 3–5 only) | `CTraderFixLogonHostedService.cs` (112/112), `DependencyInjection.cs` (62/62), `LiveRuntimeStatus.cs` (66/66), `CopyTradingService.cs` (625/625), `CopyTradingHostedService.cs` (45/45), `CTraderFixCopyOpen.cs` (223/223), `XauUsdOneToOneCopyPolicy.cs` (188/188), `CopyRosterEngine.cs` (136/136), `ShadowCopyEngine.cs` (91/91), `EfDashboardQueries.cs` L21–52, `apps/api/Program.cs` L33–84, `data/demo_copy_ledger.json` (public dest ids only) |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Boolean key quoted only. |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. |
| Live GET this pass | **Attempted, blocked.** `web_fetch`/`open_page` to `http://127.0.0.1:5000/api/{health,settings,copy/status,ingest/status,overview}` — SSRF/private-IP refuse. **No live JSON this slot.** Claims that need a live GET remain unobserved, not invented. |
| Method | Full `read_file` of the four assigned files. Targeted `grep`: `35=D` / `Build("D")` / `CanPromoteToLive` / `RealCopyEnabled\s*=` / `AllowFixSend` / `DestinationRealPnl`. Flag-only grep of `D:\Prop\.env` L73. |

**Honesty rule:** FAIL any claim that cannot be proven from a file this slot or a live GET. Prior swarm prose is not evidence. A Logon `35=A` is not a NewOrderSingle. `SAFE_BY_ABSENCE` on live `1369850` is not “flag stays false.” A paper `ShadowOrder` is not dest PnL. `DestinationRealPnl = 0` is a constructor literal, not a measured edge. Do not print secrets.

---

## 0. Verdict (binding)

**FAIL.** Claim 3 is **disproven** from product files. Claims 1 (assigned-file scope), 2, 4 (live-capital / assigned session), and 5 (paper SHADOW ≠ dest PnL) are file-proven. Unscoped “no `35=D` builder in the tree” is **false** and is **not** claimed as PASS.

| # | Assigned claim | Verdict | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** (scoped to `CTraderFixSession.cs`) / **FAIL** if unscoped | Assigned file 135/135: outbound tag 35 is `"A"` only (`BuildLogon` L96). Grep of this file for `35=D` / `"D"` = **0**. `WriteAsync` = 1 (logon only). Product **does** have builders: `CTraderFixCopyOpen.cs` L95 `Build("D")` (hosted hop), plus `CTraderFixDemoTestTrade.cs` ×3 and `CTraderFixDemoMatrix.cs` L93. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(TraderState current) => false` at `BaselineScorer.cs` L211. Unconditional. `FromBaseline` max state is `SHADOW` (L200–201). Unit test `Three_disciplined_winners_go_to_shadow_not_live` asserts both. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | Only product write is `DependencyInjection.cs` L41 from config. `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true`. `CTraderFixLogonHostedService` L60–70 updates Quote/Trade only and **logs** `RealCopyArmed` — **no assignment**. `CTraderFixSession` never mentions the flag. Grep `RealCopyEnabled\s*=` under `*.cs` = **1 hit** (DI L41). |
| 4 | sending now cannot be the profit path | **PASS** (live capital + assigned session + dest DTO) | Assigned session sends only `35=A`. Persist hop `AllowFixSend = false` (`CopyTradingService` L324). `VenueReconciled = false` (L20) so Evaluate OPEN dies `VENUE_NOT_RECONCILED` (RiskEngine L84–85). `CanPromoteToLive=false`. `LiveCopyPage` has no POST/send control. `OverviewDto.DestinationRealPnl` is literal `0` (`EfDashboardQueries` L44). **Residual (not a live-profit path):** hosted `ExecuteDemoCopyAsync` can emit demo dest `35=D` **without** `Evaluate` / `RealCopyEnabled` (L483–605); ledger has open fill `305750`/`21250421` dest `237339770` @ `4390.2`. Live account `1369850` is refused (`CTraderFixCopyOpen` L37–41). |
| 5 | SHADOW on demo is not destination profit | **PASS** | Scorer never emits dest fills. `ShadowCopyEngine.SimulateEntry` is paper. Dashboard `shadowPnl` = `Sum(SourceVsShadowSlippage)` (`EfDashboardQueries` L29). `DestinationRealPnl` constructor `0`. SHADOW eligibility is source quality, not dest-net. **Residual:** SHADOW + `ADMITTED` + `DemoDest` can still trigger dest `35=D` (policy allows SHADOW; roster AUTO_ADMIT; hop ignores Evaluate). That dest exposure is **not** booked as dest profit (DTO 0). |

**Overall slot verdict: FAIL** (instruction: FAIL if any claim cannot be proven from a file or live GET; claim 3 is affirmatively false).

**Risk to capital:** **NONE** on live Pepperstone `1369850` (`SAFE_BY_ABSENCE` — CopyOpen refuse + assigned session `35=A` only). **Not absent** on demo dest `5328266` (hosted hop + open ledger fill). Flag may be **armed**. Armed ≠ sent-to-live.

Stale siblings this slot contradicts: A014 / CREDENTIALS “`REAL_COPY` forced false”; A015 “logon sets `_runtime.RealCopyEnabled = false`”; README “Real NewOrderSingle is off (`REAL_COPY_EXECUTION_ENABLED=false`)”; any BOOK that still says `NewOrderSingleImplemented = const false` or product `35=D=0` / hop-absent CopyOpen (HEAD is `NOS => DemoDest` L50; persist `AllowFixSend=false` is **L324**; hosted tick calls `ExecuteDemoCopyAsync`).

This slot did **not** send `35=D`.

---

## 1. no `35=D` builder — PASS (assigned file) / FAIL (product-wide)

### 1.1 Assigned file (the claim as mapped to the first read)

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` — 135 physical lines, full read.

Outbound builder is **only** `BuildLogon`:

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

Single write:

```46:50:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
            var seq = 1;
            var logon = BuildLogon(senderCompId, targetCompId, senderSubId, targetSubId, username, password, seq);
            var bytes = Encoding.ASCII.GetBytes(logon);
            await ssl.WriteAsync(bytes, timeoutCts.Token);
            await ssl.FlushAsync(timeoutCts.Token);
```

Reply parse accepts `35=A` as LoggedOn (L55–64). There is no `NewOrderSingle`, no `Build("D")`, no tag 35 other than `"A"`. Grep of this file for `35=D` / `"D"` = **0**. Sockets are `using`/`await using` and disposed after one read.

**PASS** as “`CTraderFixSession` has no `35=D` builder.” A logon is not a fill.

### 1.2 Unscoped product claim (adversarial; not a PASS)

`grep Build("D")` under `D:\Prop\src`:

| File | Hits |
|---|---|
| `src/Fix.CTrader/Sessions/CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra.ToArray())` — demo-gated; **called from hosted copy tick** |
| `src/Fix.CTrader/Sessions/CTraderFixDemoTestTrade.cs` L139, L163, L197 | tools / demo-gated |
| `src/Fix.CTrader/Sessions/CTraderFixDemoMatrix.cs` L93 | demo helper |

CopyOpen refuse gate (live identity cannot be the dest):

```37:41:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
        if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
            || !sender.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
            || account == "1369850")
        {
            return Fail("Refused: dest must be demo FIX, not live 1369850.", host, account, sourceLogin);
```

Hosted wiring (`CopyTradingHostedService` L28–30): every 20s `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**. That last hop calls `CTraderFixCopyOpen.SendAsync` (open L566; close L528) with **no** `RiskEngine.Evaluate` and **no** `RealCopyEnabled` check.

If someone claimed “the product has no `35=D` builder,” that claim is **FAIL**. This slot does **not** treat that stale product-wide sentence as PASS.

---

## 2. `CanPromoteToLive` is false — PASS

`D:\Prop\src\Domain\Scoring\BaselineScorer.cs` — 212/212.

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

Proof points:

- `CanPromoteToLive` ignores `current` and is a constant `false`.
- `FromBaseline` never returns `LIVE` or `LIVE_CANDIDATE`. Ceiling is `SHADOW`.
- Early-score gate is 3 completed XAU trades (`EarlyScoreTradeCount = 3`, L40). That is source quality, not dest-net, and still cannot auto-LIVE.
- `tests/Unit/BaselineScorerTests.cs` L21–27: three disciplined winners → `SHADOW` and `CanPromoteToLive(...) == false`.

`LiveCopyPage.tsx` L15 displays `liveTraders` from API status. It does not promote. Copy hop blockers (when not DemoDest) include `"0 traders in LIVE (promotion is manual; trade #3 cannot auto-LIVE)"` (`CopyTradingService` L614–615).

**PASS.** Promotion to LIVE is not implemented.

---

## 3. `RealCopyEnabled` forced false after logon — FAIL

None of the four assigned files force the flag false.

| File | What it does with the flag |
|---|---|
| `CTraderFixSession.cs` | **Zero** tokens `RealCopy` / `REAL_COPY`. Logon only. |
| `BaselineScorer.cs` | **Zero** tokens. |
| `RiskEngine.cs` | Reads `request.RealExecutionEnabled`. Empty comment-branch at L90–93 does **not** return. `AllowFixSend` = `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Input, not a force-false. |
| `LiveCopyPage.tsx` | Displays `status?.realCopyArmed ? 'YES' : 'NO'` (L13). Read-only. |

The logon host (required to test “after logon”) also does **not** re-pin:

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

Only product assignment:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

API host loads `.env` then environment variables (`apps/api/Program.cs` L10, L13). Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret). `/api/health` and `/api/settings` expose `runtime.RealCopyEnabled` (`Program.cs` L55, L76). Live GET of those routes **blocked this slot** — the **file** proof is enough: the flag is **config-bound**, not forced false after logon.

`grep RealCopyEnabled\s*=` across `*.cs` = **one** write (DI L41). There is no `_runtime.RealCopyEnabled = false` after `TryLogonAsync`.

**FAIL.** Claim 3 is the opposite of the code.

---

## 4. sending now cannot be the profit path — PASS (live / assigned / DTO)

### 4.1 Assigned session is not a profit path

`CTraderFixSession.TryLogonAsync` writes one `35=A` and disposes the socket. No ClOrdID, no 38/54/40, no ExecutionReport wait. Logon ≠ fill ≠ dest PnL.

### 4.2 Risk hop cannot approve a live send

```20:20:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
```

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

Copy hop Evaluate OPEN always passes `Reconciled = VenueReconciled` (L304) → L84–85 `VENUE_NOT_RECONCILED` and `AllowFixSend=false`. Persist then **overwrites** anyway:

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

`VenueReconciled` is const false, so the LIVE-send branch is dead. `CanPromoteToLive=false` so `CurrentState==LIVE` cannot come from the scorer.

`RiskEngine` L90–93 when `RealExecutionEnabled==false` is a no-op comment (“Shadow path still evaluates risk but never allows FIX send”). Approve can still return; send is gated by `allowSend`. Unit `Real_flag_false_never_allows_fix_send` matches.

### 4.3 UI cannot send

`LiveCopyPage.tsx` 70/70: `useCopyStatus` + `useCopyIntents` GETs only (`hooks.ts` L60–65). No button, no `fetch` POST, no FIX writer. Empty-state copy (L57) is honest that **demo dest auto-sends** after ADMITTED — that is the residual in §4.4, not a UI profit click.

### 4.4 Dest profit is not measured (constructor 0)

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

`DestinationRealPnl` (second `0`) is a literal. Sending now cannot be “the profit path” because dest profit is **not computed**. Wanting a send does not create an edge.

### 4.5 Residual: demo dest send exists (not live profit)

`NewOrderSingleImplemented => DemoDest` (`CopyTradingService` L45–50). `ExecuteDemoCopyAsync` L483–605 sends via CopyOpen when host/sender are demo and account ≠ `1369850`. Bypasses `Evaluate`. Caps `MaxAutoLots=0.05` 1:1. Ledger `D:\Prop\data\demo_copy_ledger.json` (public ids only): source `305750` / pos `21250421` / dest `237339770` / px `4390.2` / `DestClosed=false`. Hard-seeded again at L500–511 if missing.

That is **demo dest exposure**, not live `1369850`, and not booked dest profit. Claim 4 as “sending now is not how this process makes live money” is **PASS**. Claim 4 as “nothing in HEAD can emit `35=D`” would be **FAIL** — that is not the assigned wording.

**PASS** for the assigned meaning. Live GET of `/api/copy/status` was **not** observed this slot; file hop is sufficient.

---

## 5. SHADOW on demo is not destination profit — PASS

### 5.1 SHADOW is a source-score state, not a dest book

`FromBaseline` can emit `SHADOW` after 3 XAU trades with quality ≥ 70 and risk < 40. That score uses **source** `NetRealizedPnl` / profit factor / martingale flags. It never writes a dest ticket.

`AfterHighEarlyScore() => SHADOW` (L209). Still not LIVE.

### 5.2 Paper shadow fills are not dest PnL

`GenerateShadowIntentsAsync` on non-LIVE (always, given L330 dead) writes `SHADOW_ONLY` and optionally `ShadowCopyEngine.SimulateEntry` into `ShadowOrders` (L336–359). `SimulateEntry` takes a `DestinationQuote` and models 0.05-point latency slippage. No socket.

Dashboard `shadowPnl` = sum of `SourceVsShadowSlippage` (L29). That is a paper slippage total, not dest realized.

`DestinationRealPnl` is the next Overview field and is **hard 0** (L44). SHADOW dollars are therefore **not** dest profit in the only dest field the API exposes.

### 5.3 Policy residual (does not convert SHADOW paper into dest profit in the DTO)

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET` (L81–85) and **allows** `SHADOW` / `LIVE_CANDIDATE` / `LIVE` if n≥20, XAU net > 0, demo/contest, no size-pattern. Roster AUTO_ADMIT then lets `ExecuteDemoCopyAsync` send dest `35=D` for ADMITTED seats. That dest fill is **demo exposure**, still **not** written into `DestinationRealPnl`.

So: SHADOW-on-demo **paper** ≠ dest profit (**PASS**). SHADOW-on-demo **can** be a dest-send *selector* after n≥20 (**residual**, not dest-PnL accounting).

`LiveCopyPage` L14 shows “SHADOW traders” as a count. It does not mark them as dest PnL.

**PASS.**

---

## 6. Live GET (required class; this slot blocked)

Attempted:

- `http://127.0.0.1:5000/api/health`
- `http://127.0.0.1:5000/api/settings`
- `http://127.0.0.1:5000/api/copy/status`
- `http://127.0.0.1:5000/api/ingest/status`
- `http://127.0.0.1:5000/api/overview`

Result: fetch tools refuse private/loopback. **No live body.** Runtime `realCopyEnabled` / `quoteLoggedOn` / `tradeLoggedOn` / `shadowPnl` / dest DTO **unobserved** this slot.

File proof still FAILS claim 3 (no re-pin; `.env` L73 `true`; DI L41 bind). File proof still PASSES 1 (scoped), 2, 4, 5. I will not invent a 200 OK.

---

## 7. What this slot will not claim

- Not “EX5 decompiled.” Not a go-live PASS.
- Not “dest EV positive.” Dest DTO is 0. Ledger fill is unmarked.
- Not “copy-all 8463 is an edge.” That would copy `RISK_BLOCKED` source losses (sibling pin; **not re-summed this slot** — do not treat as this slot’s measurement).
- Not “REAL_COPY stays false.” It does not.
- Not “product has zero `35=D` writers.” It has them; live `1369850` is refused.

---

## 8. Residual / stale pins

| Pin | This-slot status |
|---|---|
| A015 / CREDENTIALS “logon forces `RealCopyEnabled=false`” | **STALE.** Logon host L60–70 does not assign. |
| README “`REAL_COPY_EXECUTION_ENABLED=false`” | **STALE vs lab `.env` L73 `true` + DI bind.** |
| BOOK `NewOrderSingleImplemented = const false` | **STALE.** HEAD is `=> DemoDest` L50. |
| BOOK persist `AllowFixSend=false` at L306 | **STALE.** HEAD is **L324**. |
| BOOK product `35=D=0` / CopyOpen unwired | **STALE.** Hosted tick → `CTraderFixCopyOpen.Build("D")`. |
| “SHADOW dollars are dest profit” | **FALSE.** DTO dest is 0; shadow is paper slippage. |

---

## 9. Risk to capital

| Book | Risk this slot |
|---|---|
| Live Pepperstone `1369850` | **NONE** — CopyOpen refuse; assigned session `35=A` only; persist `AllowFixSend=false`; `CanPromoteToLive=false`. `SAFE_BY_ABSENCE`. |
| Demo dest (lab DemoDest / ledger `237339770`) | **Not absent.** Hosted 20s hop can `35=D` without Evaluate. Open 0.01 long @ 4390.2 on disk. Not live capital. Dest DTO still 0. |
| This process / this report | Report-only. No send. No `.env` flip. |

**Slot 4 overall: FAIL** (claim 3). Risk to **live** capital: **NONE**.
