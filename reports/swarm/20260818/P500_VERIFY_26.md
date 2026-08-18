# P500_VERIFY_26 — Adversarial verifier (slot 26)

| Field | Value |
|---|---|
| Slot | **26** |
| Date | 2026-08-18 |
| Role | Adversarial verifier. Re-read assigned files. Do not trust sibling agents. |
| Assigned files | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` |
| Adjacent (cite only where needed to prove/fail) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `ShadowCopyEngine.cs`, `EfDashboardQueries.cs`, `EfTradingStore.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `apps/api/Program.cs`, `D:\Prop\.env` L73 (boolean only) |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent this slot | **No** |
| `REAL_COPY` flipped this slot | **No** |
| Live GET this slot | **Blocked.** `web_fetch` SSRF-rejects `127.0.0.1` / `localhost`. `GET http://127.0.0.1:5000/api/health`, `/api/copy/status`, `/api/settings` **not** executed. No process attach. |
| Secrets printed | **None** (boolean flags + public dest ids `5328266` / `1369850` only) |

**Rule used:** FAIL any claim that cannot be proven from a file this slot re-read, or from a live GET this slot actually performed. Stale reports (`P500_CODE_26`, `P500_PROFIT_SYNTHESIS` §2.1, `CREDENTIALS_AND_COPY_STATUS`, `E002`) are **not** evidence.

---

## Overall verdict: **FAIL**

Claim 3 is **disproven** on disk. Claims 1 (session-scoped), 2, 4 (live-capital profit hop), and 5 (SHADOW score ≠ dest P&L) are file-proven. Product-wide “no `35=D` builder” is **false** if read as a tree claim.

| # | Claim | Verdict | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS_SCOPED** (`CTraderFixSession` only). **FAIL** if read as the product. | File |
| 2 | `CanPromoteToLive` is false | **PASS** | File |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** (disproven) | File + `.env` boolean |
| 4 | sending now cannot be the profit path | **PASS** (live-capital / LIVE hop). Demo dest send is **not** that path and is **not** `SAFE_BY_ABSENCE`. | File |
| 5 | SHADOW on demo is not destination profit | **PASS** | File |

**One-liner:** session is `35=A` only; promotion is hard-false; the flag is **not** re-pinned after logon; live Pepperstone cannot be the profit send; SHADOW/slippage is not dest P&L; demo dest `Build("D")` still exists.

---

## 1. No `35=D` builder — **PASS_SCOPED** / **FAIL_IF_GLOBAL**

Read in full: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (136/136).

Outbound builder is **only** `BuildLogon`. Tag 35 is literal `"A"`. One `ssl.WriteAsync` of that logon. Reply is parsed for `35=A` vs reject. Sockets are `using`-disposed. Zero `NewOrderSingle`, zero `Build("D")`, zero tag 38, zero `OrderQty`.

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

**Assigned-file claim holds.** A TLS Logon is not a fill.

**Adversarial remainder (does not fail the session-scoped reading):** the same directory **does** build MsgType `D` and write it to TRADE `:5212`.

| File | Call sites |
|---|---|
| `CTraderFixCopyOpen.cs` L95 | `Build("D", …)` after SecurityList; used by `CopyTradingService.ExecuteDemoCopyAsync` |
| `CTraderFixDemoTestTrade.cs` L139, L163, L197 | `Build("D", …)` flatten / market / close |
| `CTraderFixDemoMatrix.cs` L93 | `SendD` → `Build("D", …)` |

`CTraderFixCopyOpen` refuses live account `1369850` and non-`demo-` host / non-`demo.` sender (L37–L42). Demo dest `5328266` is **in** the allow set. On-disk `D:\Prop\reports\swarm\20260818\DEMO_COPY_OPEN.json` is an ExecutionReport `35=8` / `150=F` for dest pos `237339770` on `demo.pepperstone.5328266`. `D:\Prop\data\demo_copy_ledger.json` still has that row `DestClosed=false`. Those files are not a live GET this slot; they are dest-send evidence on disk.

Older “product `35=D` = 0 / no sender” lines (`E002`, `CREDENTIALS_AND_COPY_STATUS`, early `P500_PROFIT_SYNTHESIS` §2.1) are **STALE**.

---

## 2. `CanPromoteToLive` is false — **PASS**

Read in full: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212).

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

Unconditional. Parameter `current` is unused. Unit lock: `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` asserts `CanPromoteToLive(...) == false`.

`FromBaseline` never emits `LIVE` or `LIVE_CANDIDATE`:

```189:207:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
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
```

Ceiling auto-state is **SHADOW**. Quality is a shape score (`NetPnl>0`, PF, behavior, risk), not dest expectancy.

**Adversarial remainder (does not fail the claim):** product hop **never calls** `CanPromoteToLive`. Grep hits: definition + unit test + a `_tmp` eval. `CopyRosterEngine` / `XauUsdOneToOneCopyPolicy.IsTraderEligible` admit `SHADOW` (and would admit `LIVE_CANDIDATE`/`LIVE` if those states existed). Hard-false promotion does **not** block demo dest send.

`FeatureFlags:AutoPromotionEnabled=false` in `appsettings.json` is a JSON default on an unmapped `SettingsController`. Not a send gate.

---

## 3. `RealCopyEnabled` forced false after logon — **FAIL**

This is the slot fail.

`CTraderFixSession` never mentions the flag.

The logon host **used to** assign `_runtime.RealCopyEnabled = false` (quoted as current HEAD in `P500_CODE_26` L66). **HEAD no longer does that.** Full re-read of `CTraderFixLogonHostedService.cs` (113/113): after QUOTE+TRADE `TryLogonAsync` it writes session fields and **logs** the existing bit. No assignment.

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

The only product write of the property is DI construction from env:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

API boot: `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()`. Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret quoted). After a successful logon the bit **stays** whatever DI bound. Logon does not force false.

`CTraderFixOptions.RealCopyExecutionEnabled` still defaults `false` — a **different identifier**, unused by `LiveRuntimeStatus`.

`apps/api/Program.cs` `/api/health` and `/api/settings` **echo** `runtime.RealCopyEnabled`. This slot did **not** GET them. A live process with that `.env` loaded would report **true**. `CREDENTIALS_AND_COPY_STATUS` “`REAL_COPY_EXECUTION_ENABLED` = false (forced)” is **STALE**.

Armed ≠ ticket. Fail of this claim is **not** a live fill. It is a failed safety pin.

---

## 4. Sending now cannot be the profit path — **PASS** (narrow)

Proven from assigned + hop files:

1. **Official session cannot send.** Claim 1: `CTraderFixSession` is logon-only; socket disposed. Wanting profit is not an edge and not a `35=D`.
2. **No LIVE book to send as “the” profit hop.** `FromBaseline` never returns `LIVE`. `CanPromoteToLive` is hard-false. `CopyTradingService` LIVE send branch requires `decision.AllowFixSend && score.CurrentState == LIVE && NewOrderSingleImplemented && VenueReconciled`. `VenueReconciled` is `const false`. Persist then **overwrites** `AllowFixSend = false` (L324). Branch writes `LIVE_SEND_BLOCKED_UNIMPLEMENTED` only if all four are true — they are not.
3. **Risk engine cannot authorize a live FIX send on the hosted risk hop.** `AllowFixSend` is `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Hosted Evaluate passes `Reconciled = VenueReconciled` (`false`), so new exposure is `VENUE_NOT_RECONCILED` before approve. Comment at L90–93: shadow path “never allows FIX send” when `RealExecutionEnabled == false`. The empty `if` is **not** itself the refuse (refuse is `allowSend` + `Reject`). Unit fixture `Real_flag_false_never_allows_fix_send` locks `AllowFixSend=false` when the flag is false.
4. **Live Pepperstone dest is refused.** `CTraderFixCopyOpen` returns on `account == "1369850"`. LiveCopyPage blocker header: “Live send blockers (Pepperstone cannot be filled)”.
5. **SHADOW quality is not dest EV.** Claim 5. Copy-all of the scored XAU book is a loss path (synthesis pin, not re-GET this slot: scored XAU **−$154,425**, `RISK_BLOCKED` **−$241,580**). That pin is **not** used as a PASS for this claim; the structural close of the LIVE hop is.

`LiveCopyPage.tsx` (70/70) is a dashboard. It cannot send. Empty-state text is honest about **demo dest auto-send**, which is the caveat:

```56:58:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      {intents.length === 0 && (
        <p className="text-gray-500 text-sm">No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.</p>
      )}
```

**Adversarial remainder (does not fail the live-capital reading):**

- `NewOrderSingleImplemented => DemoDest` (`CopyTradingService` L50). Const-false is **STALE**.
- `CopyTradingHostedService` 20s tick calls `ExecuteDemoCopyAsync`, which calls `CTraderFixCopyOpen.SendAsync` **without** `RiskEngine.Evaluate`. Cap `MaxAutoLots=0.05`. That is dest activity on demo `5328266`.
- That hop is **not** a measured +EV profit path (no standing quote tape on the official session; `MaxSlippage` unread; α=`AllocationFactor=1m` on the policy; demo/contest AUTO_ADMIT). It is still a **send**.
- `DestinationRealPnl` on `OverviewDto` is a **constructor `0`** (`EfDashboardQueries` L44). That is not a measured dest flat. Ledger fill `305750` / `237339770` @ 4390.2 / 0.01 is unmarked.

So: **sending now cannot be the live-capital profit path.** Sending now **can** be a demo-dest ticket. Do not collapse those.

---

## 5. SHADOW on demo is not destination profit — **PASS**

Assigned scorer: SHADOW is `quality >= 70 && risk < 40 && trades >= EarlyScoreTradeCount` (3). It is a **source XAU shape** state. `MaeMfeQuality` is hardcoded `Unavailable`. Hold time is computed and unused.

Assigned UI: `LiveCopyPage` shows `shadowTraders` / `shadowFills` as separate cells from `liveSends` / `LIVE traders`. No dest P&L column.

Adjacent proof that SHADOW rows are **not** dest fills:

- `ShadowCopyEngine.SimulateEntry` / `SimulateExit` price off a `DestinationQuote` bid/ask + 0.05 modeled slip. No socket.
- `EfTradingStore.PersistDemoShadowAsync`: if `state != SHADOW` return; else `Status = "SHADOW_ONLY"` + `SimulateEntry`.
- `CopyTradingService.GenerateShadowIntentsAsync`: non-LIVE path `Status = "SHADOW_ONLY"` + same simulate.
- `EfDashboardQueries.GetOverviewAsync`: `ShadowPnl = Sum(ShadowOrders.SourceVsShadowSlippage)`; `DestinationRealPnl = 0` literal.

Demo **group** membership (`CopyGroupFilter.IsDemoOrContest`) is the **admission** filter, not dest P&L. A SHADOW name on `demo\yo-2step` can be `AUTO_ADMIT` and then hit `ExecuteDemoCopyAsync`. Those dest fills (if any) are **destination positions on demo `5328266`**, not the SHADOW score and not `ShadowPnl`. Claim 5 as written (“SHADOW on demo is not destination profit”) holds for the **score / shadow-order metric**. It does **not** mean “no demo dest ticket can exist for a SHADOW name.”

---

## Risk to capital

| Surface | This slot |
|---|---|
| Live Pepperstone `1369850` | **NONE** — session is `35=A` only; CopyOpen refuses that account; LIVE hop dead (`CanPromoteToLive` false, `VenueReconciled` false, persist `AllowFixSend=false`). |
| Demo dest `5328266` | **NOT NONE** — hosted 20s `ExecuteDemoCopyAsync` → `Build("D")` when `DemoDest`. Ledger row 305750 still open on disk. This slot did not send. |
| Flag after logon | **Armed possible.** `.env` L73 `true`; DI binds; host does not re-pin. Armed ≠ fill. Armed + a live sender would be a loss path. |

`SAFE_BY_ABSENCE` applies to **live** dest only. Do not stamp it on demo dest.

---

## Stale reports this slot must not recycle

| Artifact | Stale line vs HEAD |
|---|---|
| `P500_CODE_26.md` | Quotes `_runtime.RealCopyEnabled = false` after logon. **Gone.** |
| `P500_PROFIT_SYNTHESIS.md` §2.1 | Same false assign; “no sender”; `NOS=false`. |
| `reports/CREDENTIALS_AND_COPY_STATUS.md` | Flag “false (forced)”; “Live `35=D` method does not exist.” |
| `E002_no_live_send.md` | “no function that emits FIX MsgType=D.” Three sibling builders exist. |
| Any BOOK/`W500` that still says `NewOrderSingleImplemented=false` as a const | HEAD is `=> DemoDest`. |

---

## Method / honesty

- Assigned four files re-read in full this slot. Adjacent hop files re-read for claims 3–5.
- No live GET. Process `realCopyArmed` / `liveSends` / current SHADOW count **unverified** this slot.
- No product edit. No password, FIX `554`, or Manager secret printed.
- Empty-PASS? **No.** All four assigned files have substance.

**DONE for this slot:** reviewer-grade FAIL on claim 3; remaining claims proven from files as scoped above.
