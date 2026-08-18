# P500_VERIFY_73 — Adversarial verifier (slot 73)

| Field | Value |
|---|---|
| Slot | **73** |
| Date | 2026-08-18 |
| Agent | P500_VERIFY_73 (adversarial; re-read HEAD files this slot; sibling `P500_*` / `W500_*` numbers are **not** evidence) |
| Assigned | Read `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`. Confirm: (1) no `35=D` builder; (2) `CanPromoteToLive` is false; (3) `RealCopyEnabled` forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. **FAIL any claim that cannot be proved from a file or live GET.** |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_73.md` |
| Assigned SUT | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135), `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212), `D:\Prop\src\Domain\Risk\RiskEngine.cs` (189/189), `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (70/70) |
| Hop (not assigned; required to test 3–5) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `apps\api\Program.cs`, `EnvFile.cs`, `.env` **flag + dest-identity lines only**, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `CopyGroupFilter.cs`, `ShadowCopyEngine.cs`, `DealIngestionService.cs` (`ReconstructionScoringService`), `EfTradingStore.PersistDemoShadowAsync`, `DemoCopyLedger.cs`, `data\demo_copy_ledger.json`, `CTraderFixOptions.cs` |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **None.** Password / proxy / FIX password values were not quoted. Only the already-on-disk boolean `REAL_COPY_EXECUTION_ENABLED=true`, public dest host prefix `demo-`, sender prefix `demo.`, and public dest ids `5328266` / `1369850`. |
| Live GET this slot | **Attempted and blocked.** `web_fetch` `http://127.0.0.1:5000/api/health` → SSRF blocked (loopback). `open_page` `http://127.0.0.1:5000/api/health` and `http://localhost:5000/api/health` → retrieve failed. **No live JSON.** File proof only. |
| Live `35=D` sent this slot | **No.** |
| `REAL_COPY` flipped this slot | **No.** |
| Method | Full `read_file` of the four assigned files plus the logon / DI / copy hop they actually call. Targeted `grep` for `35=D` / `Build("D")` / `CanPromoteToLive` / `RealCopyEnabled =` / `DestinationRealPnl` / `ShadowPnl`. `.env` inspected **for flag + dest-identity keys only**. |
| Honesty rule | Sibling swarm PnL / census integers are **not** evidence. A comment is **not** a runtime pin. A dashboard label is **not** dest cash. `CTraderFixOptions.RealCopyExecutionEnabled` default `false` is **not** the DI bind. `reports\CREDENTIALS_AND_COPY_STATUS.md` “false (forced)” is **STALE vs HEAD**. |

---

## 0. Verdict (binding)

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** (scoped to `CTraderFixSession.cs`) / **FAIL** if unscoped | Assigned file **135/135**. Only outbound MsgType is `(35, "A")` at L96. Grep of this file for `35=D` / `(35, "D")` / `Build("D")` / `NewOrderSingle` = **0**. Product residual: sibling `Build("D")` **×5** on the hosted demo hopper. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(TraderState current) => false` at `BaselineScorer.cs` L211. Argument unused. `FromBaseline` never returns `LIVE` / `LIVE_CANDIDATE`. Persist writes `CurrentState = score.SuggestedState`. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** (**disproved**) | Logon host **reads** the flag and **never assigns** `false`. Sole product `RealCopyEnabled =` write is DI L41 binding `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true`. |
| 4 | sending now cannot be the profit path | **FAIL** (unscoped) / **PASS_NOT_BOOKED_DEST_PROFIT** (dashboard ctor) | Assigned session cannot send. Persist hop writes `AllowFixSend=false` and uses `VenueReconciled` const `false`. Dashboard `DestinationRealPnl` is constructor `0`. Residual that **fails** the unscoped claim: hosted `ExecuteDemoCopyAsync` **can** emit `35=D` on demo dest and persist `DestFillPrice`. No live GET to mark dest cash. |
| 5 | SHADOW on demo is not destination profit | **PASS** | `SHADOW` is a **source-trader state**. Dashboard `ShadowPnl` is `Sum(SourceVsShadowSlippage)`. `DestinationRealPnl` is constructor `0`. `LiveCopyPage` shows SHADOW **counts**, no dest cash. Paper `ShadowCopyEngine.SimulateEntry` is not a venue fill. Residual: SHADOW + `ADMITTED` can still fire demo dest `35=D` (dest **activity**, not booked dest profit). |

**Slot verdict: `FAIL`.**

Claim 3 is **disproved**, not merely unproved. One FAIL is enough. Claim 1 holds only when scoped to the assigned session file. Tree-wide “no `35=D` builder” is **false** on HEAD. Claim 4 as written (“sending now cannot be the profit path”) is **not** file-proven once the hosted demo hopper is in scope.

**Risk to capital:** **NONE on live `1369850`** (`SAFE_BY_ABSENCE` for `CTraderFixSession` + `CTraderFixCopyOpen` refuse). **Not absent on demo dest `5328266`** if `DemoDest` is true: hosted `ExecuteDemoCopyAsync` can emit `35=D` **without** `RiskEngine.Evaluate` and **without** reading `RealCopyEnabled`. This slot sent **0**.

---

## 1. Claim 1 — no `35=D` builder — **PASS** (`CTraderFixSession.cs` only)

Assigned file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135** lines, full read).

The only outbound builder is `BuildLogon`. The only MsgType it assembles is logon `A`:

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

| Check | Measured this slot |
|---|---|
| Outbound MsgType in this file | **only** `(35, "A")` L96 |
| `WriteAsync` count | **1** (L49, the logon bytes) |
| `35=D` / `(35, "D")` / `Build("D")` / `NewOrderSingle` in this file | **0** (file grep) |
| `Assemble` callers | **1** (`BuildLogon`) |
| Socket lifetime | `using TcpClient` + `await using SslStream`; disposed after one read |
| Inbound `Extract(reply, "35")` L55 | **read**, not a builder |
| Inbound `35=A` treated as | `LoggedOn = true` (L56–64) — **logon ack, not a fill** |
| Error text L73 `"Logon rejected 35={msgType}"` | inbound type echoed; not outbound D |

Adversarial residual (**does not fail the assigned-file claim**; **does fail** a product-wide “no `35=D` builder” claim):

| File | What this slot measured |
|---|---|
| `src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 / L142–156 | Generic `Build(string type, …)` then `Build("D", …)` after TRADE `35=A` + SecurityList. **Refuses** non-`demo-` host, non-`demo.` sender, or account `1369850` (L37–42). Hosted by `CopyTradingService.ExecuteDemoCopyAsync`. |
| `src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` L93 | `Build("D", …)` (matrix helper `SendD`). |
| `src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | `Build("D", …)` ×3 (demo test helper). |

Product `Build("D")` count in `*.cs`: **5**. Claim 1 as written against **`CTraderFixSession.cs`** is true. Tree-wide “product `35=D=0`” is **false** on HEAD.

---

## 2. Claim 2 — `CanPromoteToLive` is false — **PASS**

Assigned file: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (**212** lines, full read).

```188:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
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

| Check | Measured this slot |
|---|---|
| `CanPromoteToLive` body | literal `false`; `current` unused |
| `FromBaseline` return set | `INSUFFICIENT_DATA`, `RISK_BLOCKED`, `SHADOW`, `WATCH`, `EARLY_SCORE` only |
| `LIVE` / `LIVE_CANDIDATE` in this file | **0** |
| Persist hop | `ReconstructionScoringService.RebuildTraderAsync` L140: `CurrentState = score.SuggestedState` |
| Unit pin | `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` asserts `SHADOW` + `CanPromoteToLive(...).Should().BeFalse()` |

`TraderState` enum still **contains** `LIVE_CANDIDATE=4` and `LIVE=5` (`src\Domain\Enums\TraderState.cs`). That is a type-system slot, not a promotion path. No scorer / state-machine write produces those values on HEAD.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL (disproved)**

This claim is **not** in any of the four assigned files. The logon hop was read in full.

`CTraderFixLogonHostedService.ExecuteAsync` (full read, 112 lines):

- Calls `CTraderFixSession.TryLogonAsync` twice (QUOTE 5211, TRADE 5212).
- Copies `LoggedOn` / `Status` / `LastError` onto `_runtime.Quote` / `_runtime.Trade`.
- Logs `RealCopyArmed={Armed}` from **`_runtime.RealCopyEnabled`** (L69–70).
- **Does not assign** `_runtime.RealCopyEnabled` at all.

Sole product assignment of the runtime flag:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`.env` L73 (flag line only; no secret quoted): `REAL_COPY_EXECUTION_ENABLED=true`.

`apps\api\Program.cs` L10–13 loads that file via `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()` before DI.

`grep` of product `*.cs` for `RealCopyEnabled =` this slot: **exactly 1 hit** (DI L41). No post-logon re-pin. No `= false` write.

POCO default `CTraderFixOptions.RealCopyExecutionEnabled = false` (L35) is **not** wired into `LiveRuntimeStatus`. Treating that default as a runtime pin is a **lie**.

`RiskEngine` L90–93 is an empty comment when `RealExecutionEnabled == false`. That is **not** a force-false of the runtime flag.

**Claim 3 is disproved.** If the process boots with `.env` loaded, `RealCopyEnabled` is **true after logon**, same as before logon.

Live GET that would have shown `/api/health.realCopyEnabled` / `/api/settings.featureFlags.REAL_COPY_EXECUTION_ENABLED` was **blocked**. File proof is sufficient to FAIL.

---

## 4. Claim 4 — sending now cannot be the profit path — **FAIL** (unscoped)

Assigned pieces that **cannot** send:

| File | Why it is not a send path |
|---|---|
| `CTraderFixSession.cs` | logon-only; socket disposed after one inbound read |
| `BaselineScorer.cs` | no LIVE promotion; `CanPromoteToLive => false` |
| `RiskEngine.cs` L147–150 | `AllowFixSend = RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` |
| `LiveCopyPage.tsx` | display only; no POST / no FIX client |

Persist hop (`CopyTradingService.GenerateShadowIntentsAsync`):

- Passes `Reconciled = VenueReconciled` and `VenueReconciled` is **`public const bool VenueReconciled = false`** (L20).
- Therefore `RiskEngine.Evaluate` cannot return `AllowFixSend=true` on this hop (L147–150 requires `Reconciled`).
- Persist then **overwrites** `AllowFixSend = false` (L324) regardless of `decision.AllowFixSend`.
- The only “live send” branch (L330) requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` and even then sets `LIVE_SEND_BLOCKED_UNIMPLEMENTED`. That branch is **dead** on HEAD.

Dashboard dest profit is a **constructor literal**, not a mark:

```33:44:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
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

`OverviewDto.DestinationRealPnl` is the **second** `0` (after `shadowPnl`). That proves **booked dest profit is not computed**. It does **not** prove dest venue cash is $0.

What **fails** the unscoped claim — hosted demo dest hopper:

```483:605:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public async Task<int> ExecuteDemoCopyAsync(CancellationToken ct)
    {
        if (!DemoDest)
        {
            _log.LogInformation("Demo dest auto-copy skipped (host is not demo FIX).");
            return 0;
        }
        // ...
                var fill = await CTraderFixCopyOpen.SendAsync(
                    host, sender, target, account, password,
                    seat.SourceLogin.ToString(), trade.PositionId.ToString(),
                    trade.Direction == TradeDirection.Long, trade.MaxVolumeLots, ct);
```

| Check | Measured this slot |
|---|---|
| Called from | `CopyTradingHostedService` every **20s** after an 8s delay (`TickRosterAsync` → `GenerateShadowIntentsAsync` → `ExecuteDemoCopyAsync`) |
| Reads `RealCopyEnabled` | **No** |
| Calls `RiskEngine.Evaluate` | **No** |
| Gate | `DemoDest`: host starts with `demo-`, sender starts with `demo.`, account ≠ `1369850` |
| `.env` dest identity (no secrets) | host `demo-us-eqx-01.p.c-trader.com`; sender `demo.pepperstone.5328266`; account `5328266` → **`DemoDest` true if that env is loaded** |
| Builder | `CTraderFixCopyOpen.Build("D", …)` L95 |
| Live `1369850` | **refused** (`CTraderFixCopyOpen` L37–42) |
| On-disk ledger | `D:\Prop\data\demo_copy_ledger.json`: source `305750` / pos `21250421` / dest pos `237339770` / `DestFillPrice` **4390.2** / `DestClosed` **false** |
| Seeded residual | `ExecuteDemoCopyAsync` L500–512 **re-inserts** that same 305750 row if missing |

`LiveCopyPage.tsx` L57 itself states dest auto-sends: *“Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position.”* That is dest **activity**, not dest-profit booking.

`GetStatusAsync` L67 reports `VenueReconciled: DemoDest` (true on demo) while Evaluate uses const `false`. Status DTO is **not** the send gate.

**Unscoped claim 4 is not proved.** I cannot prove from a file or live GET that sending cannot produce dest P&L at demo venue `5328266`. I **can** prove: (a) live `1369850` cannot be the path; (b) dashboard dest profit is constructor `0`; (c) persist hop cannot `AllowFixSend`. That is `PASS_NOT_BOOKED_DEST_PROFIT`, not “sending cannot be the profit path.”

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **PASS**

Assigned + hop evidence that SHADOW is **paper / source state**, not dest cash:

| Fact | File proof |
|---|---|
| Highest auto state | `FromBaseline` → `SHADOW` (quality ≥ 70 and risk < 40). Never dest fill. |
| Persist of SHADOW | `EfTradingStore.PersistDemoShadowAsync` only when `state == SHADOW`; writes `Status = "SHADOW_ONLY"` and `ShadowCopyEngine.SimulateEntry` (L267–333). No FIX write. |
| Intent hop | `GenerateShadowIntentsAsync` copyable set is `SHADOW` / `LIVE_CANDIDATE` / `LIVE`; resulting status is `SHADOW_ONLY` (or the dead `LIVE_SEND_BLOCKED_UNIMPLEMENTED` branch). |
| Dashboard `ShadowPnl` | `Sum(ShadowOrders.SourceVsShadowSlippage)` (`EfDashboardQueries` L29). That is **modeled slippage**, not dest realized PnL. |
| Dashboard `DestinationRealPnl` | constructor `0` (L44). |
| `LiveCopyPage` | Stat “SHADOW traders” is a **count**. No dest-PnL column. Table is intents (broker/login/pos/side/qty/status/risk). |
| `ShadowCopyEngine` | `SimulateEntry` / `SimulateExit` / `MarkToMarket` — in-process math against a `DestinationQuote`. No socket. |

Residual that does **not** fail claim 5 as written:

- `XauUsdOneToOneCopyPolicy.IsTraderEligible` **rejects** `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET` (L81–85). SHADOW (or anything above it that is not blocked) is the **admit floor**.
- `CopyRosterEngine.Decide` + `TickRosterAsync` can `AUTO_ADMIT` an eligible SHADOW trader on a demo/contest group (`CopyGroupFilter.IsDemoOrContest`).
- `ExecuteDemoCopyAsync` then sends dest `35=D` for `ADMITTED` seats with open XAUUSD ≤ `MaxAutoLots` (0.05). That is dest **exposure**, not a SHADOW ledger mark.

SHADOW **paper** ≠ destination profit. SHADOW **as a roster class** can still arm the demo hopper. Those are different objects. Claim 5 is about the former and **passes**.

---

## 6. Assigned `RiskEngine.cs` notes (189/189)

```90:93:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.RealExecutionEnabled == false && request.Action != CopyIntentAction.CloseExposure)
        {
            // Shadow path still evaluates risk but never allows FIX send.
        }
```

Empty body. Comment is **not** an early return. Later `allowSend` (L147–150) is the real gate. `Reject(...)` always sets `AllowFixSend = false` (L187). Reducing actions can still `Approve` with `AllowFixSend = allowSend`.

Unit pin `Real_flag_false_never_allows_fix_send` uses `RealExecutionEnabled = false` and expects `AllowFixSend == false` even on `Approve`. That is consistent with L147–150. It does **not** pin the runtime flag after logon.

`ExecuteDemoCopyAsync` never constructs a `RiskEvaluationRequest`. RiskEngine is **off the demo dest hop**.

---

## 7. Assigned `LiveCopyPage.tsx` notes (70/70)

Display-only. Hooks: `useCopyStatus` / `useCopyIntents` (GET `/api/copy/status`, `/api/copy/intents`).

| UI | What it is |
|---|---|
| `REAL_COPY armed` | `status.realCopyArmed` ← `_runtime.RealCopyEnabled` (env-bound, **not** forced false) |
| `SHADOW traders` / `LIVE traders` | score-state counts |
| `Live sends` | `ExecutionIntents` with `SentAt != null` count — **not** dest PnL |
| Blocker banner | “Live send blockers (Pepperstone cannot be filled)” |
| Empty copy | “Demo dest auto-sends after a trader is ADMITTED…” |

No dest-profit number is rendered. No send button. Page cannot be the profit path. Page **does** advertise dest auto-send.

---

## 8. Live GET

| Attempt | Result |
|---|---|
| `web_fetch` `http://127.0.0.1:5000/api/health` | SSRF blocked (loopback) |
| `open_page` `http://127.0.0.1:5000/api/health` | retrieve failed |
| `open_page` `http://localhost:5000/api/health` | retrieve failed |
| `/api/settings`, `/api/copy/status`, `/api/overview` | **not obtained** |

Launch profile binds API to `http://localhost:5000` (`apps\api\Properties\launchSettings.json`). Absence of JSON is **not** a PASS. Claims that needed live confirmation of dest cash remain unproved and are **failed** where they cannot be proved from files.

---

## 9. What this slot will not claim

- Did **not** claim EX5 / MQ5 work (wrong tree).
- Did **not** claim dest venue PnL is $0 (constructor `0` is not a mark; ledger has `DestFillPrice`).
- Did **not** claim product `35=D=0` (5 builders exist).
- Did **not** claim `REAL_COPY` is false at runtime (`.env` L73 is `true`; no re-pin).
- Did **not** print secrets.
- Did **not** send `35=D`.
- Did **not** modify product or test source.

---

## 10. One-line close

P500_VERIFY_73 **FAIL**. (1) `CTraderFixSession` 135/135 is `35=A` only / **PASS_SESSION**; product `Build("D")` ×5 + hosted hopper / **FAIL_UNSCOPED**. (2) `CanPromoteToLive => false` **PASS**. (3) `RealCopyEnabled` forced false after logon **FAIL/disproved** (DI binds `.env` L73 `true`; logon logs only). (4) sending cannot be profit path **FAIL** unscoped (`ExecuteDemoCopyAsync` + ledger 305750 open; dest DTO `0` is constructor). (5) SHADOW paper ≠ dest profit **PASS**. Live GET `:5000` blocked. Live `1369850` **NONE**. Demo dest `5328266` **not** absent. This slot sent **0**.

End of P500_VERIFY_73. Product source was not modified. No secrets printed. `REAL_COPY` was not flipped by this slot.
