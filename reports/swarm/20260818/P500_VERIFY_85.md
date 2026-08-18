# P500_VERIFY_85 — Adversarial verifier (slot 85)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_85.md` |
| Agent / slot | P500 verify **85** (adversarial; siblings are not evidence) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned | Read `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`. Confirm: (1) no `35=D` builder, (2) `CanPromoteToLive` is false, (3) `RealCopyEnabled` forced false after logon, (4) sending now cannot be the profit path, (5) SHADOW on demo is not destination profit. **FAIL any claim not proven from a file or live GET.** |
| SUT (full `read_file`) | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135), `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212), `D:\Prop\src\Domain\Risk\RiskEngine.cs` (190/190), `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (71/71) |
| Hop (claims 3–5 only) | `CTraderFixLogonHostedService.cs`, `LiveRuntimeStatus.cs`, `DependencyInjection.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `CopyLifecycle.cs`, `CopyGroupFilter.cs`, `DealIngestionService.cs` (`ReconstructionScoringService`), `EfDashboardQueries.cs`, `DashboardModels.cs`, `apps/api/Program.cs`, `CTraderFixOptions.cs`, `apps/fix-worker/Worker.cs`, `apps/web/src/api/hooks.ts`, `apps/web/src/api/client.ts`, `tests/Unit/BaselineScorerTests.cs`, `tests/Unit/RiskEngineTests.cs`, `.env` boolean / public dest ids only, `data/demo_copy_ledger.json` dest ids / lots / fill price only |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Secrets printed | **None.** Quoted only `REAL_COPY_EXECUTION_ENABLED=true`, public dest account `5328266`, live account `1369850`, host prefix `demo-`, sender prefix `demo.`, dest pos `237339770`, source login `305750`. Tag `554` never dumped. |
| Live GET this slot | **Blocked.** `web_fetch` `http://127.0.0.1:5000/api/{health,copy/status,settings}` → `SSRF blocked: 127.0.0.1`. `open_page` `http://localhost:5000/api/health` → retrieve fail. No shell. Runtime flag / dest MTM / `realCopyArmed` **not** re-probed. |
| This slot sent `35=D` | **No** |

Honesty rule: a comment, log line, dashboard string, or inbound `Extract(..., "35")` is **not** a builder. `35=A` Logon is **not** NewOrderSingle. A sibling `Build("D")` is **not** `CTraderFixSession`. A POCO default `false` is **not** a process pin. An env bind that evaluates `true` **disproves** “forced false after logon.” A constructor `0` dest PnL is **not** measured dest mark-to-market. Live GET blocked ⇒ runtime claims **FAIL-unproven**.

---

## 0. Verdict (binding)

**FAIL.**

Assigned claim **3 is disproven**. Assigned claim **4 is not proven** (demo dest `35=D` hopper is live in source; dest MTM not measured; live GET blocked). One disproven or unproven assigned claim ⇒ slot FAIL.

| # | Claim | File / GET proof | Result |
|---|---|---|---|
| 1 | no `35=D` builder | Assigned file 135/135. Only outbound MsgType is `(35, "A")` in `BuildLogon`. Zero `Build("D")` / `(35, "D")` / `35=D` in that file. | **PASS** (file-scoped) |
| 2 | `CanPromoteToLive` is false | `BaselineScorer.cs` L211: `=> false`. Unconditional. `FromBaseline` never returns `LIVE` / `LIVE_CANDIDATE`. Persist writes `SuggestedState` only. | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **Disproven.** Only product assignment is DI from env. `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true`. Logon host writes Quote/Trade only; never assigns the flag. Grep `RealCopyEnabled =` in `*.cs` = **1** hit. Live GET of the flag blocked. | **FAIL** |
| 4 | sending now cannot be the profit path | Official persist hop cannot send (`AllowFixSend=false` hardcode; `VenueReconciled` const `false`; no LIVE from scorer). Live `1369850` refused. Dashboard `DestinationRealPnl` is constructor `0`. **But** hosted `ExecuteDemoCopyAsync` sends `35=D` on demo dest; ledger has an **open** dest fill. Dest account MTM **unmeasured**. Live GET blocked. Negative claim not proven. | **FAIL** (unproven) |
| 5 | SHADOW on demo is not destination profit | `SHADOW` is a source state. `ShadowCopyEngine` is paper. Overview `ShadowPnl` = Σ `SourceVsShadowSlippage`. `DestinationRealPnl` literal `0`. Residual: `SHADOW` + `ADMITTED` can still dest-send. Dest send ≠ dest profit DTO. | **PASS** |

One-line:

```text
FAIL. Session 35=A only. CanPromoteToLive=>false. RealCopyEnabled NOT forced false after logon (.env true + DI, no re-pin). Demo dest hopper can 35=D now; dest MTM unmeasured so “not the profit path” is unproven. SHADOW ≠ dest $ DTO. Live 1369850 refused. Live GET blocked.
```

Classification: `SESSION_NO_35D` / `CAN_PROMOTE_FALSE` / `REALCOPY_NOT_REPINNED` / `DEMO_DEST_SEND_EXISTS` / `DEST_PNL_CONSTRUCTOR_0` / **FAIL**.

---

## 1. Claim 1 — no `35=D` builder — PASS (`CTraderFixSession.cs` only)

Read: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135).

The type is logon-only. The only outbound constructor is `BuildLogon`. The only assembled MsgType is Logon `A`:

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

Proven from this file:

- `TryLogonAsync` L47–50: one `WriteAsync` of that logon, then `ReadAsync`. Socket disposed by `using`.
- Inbound `Extract(reply, "35")` (L55) is a **read**, not a builder.
- Error string `"Logon rejected 35={msgType}"` (L73) is inbound text.
- Grep of this file: `NewOrderSingle` = 0, `Build("D")` = 0, `(35, "D")` = 0, literal `35=D` = 0.
- Grep `(35,` in this file = **1** hit (L96 `"A"`).
- `Assemble` is generic but has **one** caller (`BuildLogon`).

**Cannot** prove product-wide “no `35=D` builder.” Same folder, same namespace, hosted on the 20 s copy tick:

| File | Proof |
|---|---|
| `CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra.ToArray())` after demo-only gate |
| `CTraderFixDemoTestTrade.cs` L139, L163, L197 | `Build("D", ...)` flatten / open / close |
| `CTraderFixDemoMatrix.cs` L91–94 | `SendD` → `Build("D", ...)` |

Those siblings do **not** fail claim 1 as scoped to `CTraderFixSession.cs`. They **do** fail any product-wide reading of “no NewOrderSingle assembler.” They matter for claim 4.

`CTraderFixLogonHostedService.cs` L69 comment `"NewOrderSingle still unimplemented"` is a **log string**, not a builder, and is **stale** vs `CTraderFixCopyOpen`.

---

## 2. Claim 2 — `CanPromoteToLive` is false — PASS

Read: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212).

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

- Unconditional `false`. Parameter `current` is unused.
- `FromBaseline` (L189–206) returns only `INSUFFICIENT_DATA`, `RISK_BLOCKED`, `SHADOW`, `WATCH`, `EARLY_SCORE`. **Never** `LIVE`. **Never** `LIVE_CANDIDATE`.
- Highest non-blocked state from quality/risk is `SHADOW` (`quality >= 70 && risk < 40`).
- Persist hop `ReconstructionScoringService.RebuildTraderAsync` (`DealIngestionService.cs` L140): `CurrentState = score.SuggestedState`. No other writer of `CurrentState =` in product source except that persist and the dashboard counters.
- Unit pin `tests/Unit/BaselineScorerTests.cs` L26: `TraderStateMachine.CanPromoteToLive(score.SuggestedState).Should().BeFalse()` after three disciplined winners land in `SHADOW`.
- Grep `CanPromoteToLive` in product `*.cs`: **one** definition, this line.

Enum still contains `LIVE_CANDIDATE = 4` and `LIVE = 5` (`TraderState.cs`). Presence of enum members is **not** promotion. Nothing in the four assigned files, and nothing in the scoring persist hop, sets those states.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL (disproven)

Assigned `CTraderFixSession.cs` never mentions `RealCopyEnabled`. Post-logon behavior is in the hosted logon service, which this slot read in full.

`CTraderFixLogonHostedService.ExecuteAsync` after both `TryLogonAsync` calls (L60–70):

- Writes `_runtime.Quote.LoggedOn/Status/LastError/UpdatedAt`.
- Writes `_runtime.Trade.LoggedOn/Status/LastError/UpdatedAt`.
- **Logs** `_runtime.RealCopyEnabled`.
- **Does not assign** `RealCopyEnabled`.

Grep `RealCopyEnabled =` across product `*.cs` = **exactly one** assignment:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`.env` L73 (boolean only): `REAL_COPY_EXECUTION_ENABLED=true`.

Therefore: process construction binds the flag **true**. Successful FIX logon does **not** flip it false. Failed logon does **not** flip it false. The flag is a constructor pin from env, not a post-logon safety latch.

Additional file facts (not a live pin):

- `CTraderFixOptions.RealCopyExecutionEnabled` defaults `false` and is **not** what DI binds (`AddTraderIntelligence` uses the env string, not this POCO).
- `apps/fix-worker/Worker.cs` L21 reads a **different** key (`CTrader:RealCopyExecutionEnabled`, default false) and never writes `LiveRuntimeStatus`.
- `apps/api/Program.cs` L55 / L76 **exposes** `runtime.RealCopyEnabled`; does not force it false.
- `LiveRuntimeStatus.Snapshot()` copyNote when true still claims `"NewOrderSingle still unimplemented"` — a string, not a latch, and stale vs `CTraderFixCopyOpen`.

Live GET `/api/health` and `/api/settings` would be the only way to pin the running process bool. Both were **SSRF-blocked** this slot. File proof already **disproves** “forced false after logon.” A live `false` would still not prove a post-logon force; it would only prove the env was not `true` in that process, or that some un-grepped writer exists (none found).

Priors that say hosted logon sets `_runtime.RealCopyEnabled = false` are **STALE vs HEAD**.

---

## 4. Claim 4 — sending now cannot be the profit path — FAIL (unproven)

Split the paths. The assigned files plus hops show **two** send stories.

### 4a. Official risk / persist hop — cannot send (proven)

`RiskEngine.Evaluate` L147–150:

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

Rejects always set `AllowFixSend = false` (L180–188). When `RealExecutionEnabled == false`, L90–93 is an empty `if` (no return); later `allowSend` is still false.

`CopyTradingService` wires this hop so it **cannot** fire a live send:

- `VenueReconciled = false` (const, L20).
- Risk request `Reconciled = VenueReconciled` (L304) ⇒ increasing actions reject `VENUE_NOT_RECONCILED`.
- Persist record **hardcodes** `AllowFixSend = false` (L324), ignoring `decision.AllowFixSend`.
- Live-send branch (L330) requires `decision.AllowFixSend && CurrentState == LIVE && NewOrderSingleImplemented && VenueReconciled`. `VenueReconciled` is const false; `FromBaseline` never emits `LIVE`. Branch is dead.
- Dead-branch status would be `LIVE_SEND_BLOCKED_UNIMPLEMENTED`; else `SHADOW_ONLY` + optional paper `ShadowOrders`.

`CTraderFixSession` cannot be this hop’s sender (claim 1). Live account `1369850` is refused by `CTraderFixCopyOpen` L37–41 and `CTraderFixDemoTestTrade` L43–47.

Overview dest profit is **not measured**. `EfDashboardQueries.GetOverviewAsync` L33–52 passes constructor `0` into `OverviewDto.DestinationRealPnl` (the first `0` after `shadowPnl`). `DashboardModels.cs` L16 names that slot. No other writer of `DestinationRealPnl` exists.

`LiveCopyPage.tsx` does not send. It GETs `/api/copy/status` and `/api/copy/intents` (hooks). Empty-state copy L57 documents dest auto-send; that is UI text, not a builder.

### 4b. Hosted demo dest hopper — **does send now** (proven)

`CopyTradingHostedService` every 20 s: `TickRosterAsync` → `GenerateShadowIntentsAsync` → `ExecuteDemoCopyAsync`.

`ExecuteDemoCopyAsync` (`CopyTradingService.cs` L483–605):

- Returns 0 only if `!DemoDest` or password blank.
- `DemoDest` (L45–48) is true when host starts with `demo-`, trade sender starts with `demo.`, and account ≠ `1369850`.
- `.env` public pins: `CTRADER_FIX_HOST=demo-us-eqx-01.p.c-trader.com`, `CTRADER_FIX_TRADE_SENDER_COMP_ID=demo.pepperstone.5328266`, `CTRADER_FIX_ACCOUNT_ID=5328266`. **DemoDest is true from file.**
- Does **not** consult `RiskEngine`, `AllowFixSend`, `RealCopyEnabled`, `CanPromoteToLive`, or `CurrentState == LIVE`.
- Opens: ADMITTED roster seats + open XAUUSD ≤ `MaxAutoLots` (0.05) → `CTraderFixCopyOpen.SendAsync` → `Build("D")`.
- Closes: ledger dest still open + source completed → same `SendAsync` with dest pos id.
- On fill: writes dest pos / ClOrdId / fill price; intent status `DEMO_SENT`.

`data/demo_copy_ledger.json` (this slot, dest ids / lots / price only):

- Source login `305750`, source pos `21250421`, **0.01** lots, dest pos `237339770`, dest ClOrdId `C20260818093047317`, dest fill **4390.2**, `DestClosed: false`.

That is an **open destination position** created by the same `35=D` builder. `CopyTradingService.ExecuteDemoCopyAsync` L500–512 even **re-seeds** this row if missing.

`GetStatusAsync` L76–77 when `DemoDest`: `"Demo dest auto-copy ON. Eligible demo/contest opens send on the 20s tick..."`. `NewOrderSingleImplemented => DemoDest` (L50) is **true** under this env.

### 4c. Why the assigned claim fails this slot

The claim is a **negative**: sending now cannot be the profit path.

Proven: sending now cannot be **live `1369850` profit**, and cannot be **dashboard `DestinationRealPnl`** (constructor 0).

**Not proven:** sending now cannot produce dest-account P&L.

- Demo dest `35=D` is compiled, hosted, env-enabled, and has an open fill.
- Dest mark-to-market is not computed in product source.
- Dashboard `0` is a constructor, not a venue query.
- Live GET of `/api/overview` / `/api/copy/status` was blocked, so dest MTM / `liveSends` / `realCopyArmed` are **not** re-pinned this slot.

Adversarial rule: FAIL the claim. Demo dest execution **is** a send path that can move dest equity (paper). Calling that “not the profit path” requires a measured dest PnL of 0 or a proof no dest ticket exists. Neither is available. Constructor 0 is **not** that proof.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — PASS

`TraderState.SHADOW` is a **source** state from `FromBaseline` (`quality >= 70 && risk < 40` after 3+ XAU). It is not dest cash.

`GenerateShadowIntentsAsync` copyable set includes `SHADOW` (L202). On the persist hop those intents become `SHADOW_ONLY` and optional `ShadowOrders` from `ShadowCopyEngine.SimulateEntry` — paper bid/ask + 0.05 pt latency slip. `SimulateEntry` / `SimulateExit` / `MarkToMarket` never call FIX.

Overview `ShadowPnl` (`EfDashboardQueries` L29) is `Sum(SourceVsShadowSlippage)`, not dest realized.

`XauUsdOneToOneCopyPolicy.IsTraderEligible`: `SHADOW` is **not** rejected (only `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` / blocked states). Combined with `CompletedXauTrades >= 20`, `XauNetPnl > 0`, demo/contest group, no size-pattern flags → roster `AUTO_ADMIT`. `ExecuteDemoCopyAsync` then dest-sends **without** re-checking state.

That residual is **dest execution**, not dest **profit accounting**. Dest profit DTO remains constructor `0`. `SHADOW` itself is not dest dollars.

Claim 5 as stated (“SHADOW on demo is not destination profit”) is file-proven. It is **not** a proof that SHADOW-admitted seats cannot dest-send (they can). Dest send ≠ dest profit DTO.

`LiveCopyPage.tsx` L14–16 displays `shadowTraders` / `liveTraders` / `liveSends` from GET. It does not convert SHADOW into dest PnL.

---

## 6. Assigned UI file — `LiveCopyPage.tsx` (71/71)

Read in full. No FIX. No `35=D`. No setter for `RealCopyEnabled`. No promote-to-live control.

- L13: `REAL_COPY armed` = `status?.realCopyArmed` (from `/api/copy/status` → `_runtime.RealCopyEnabled`). Display only.
- L23–28: blockers box titled `"Live send blockers (Pepperstone cannot be filled)"` — **copy**, not a gate. The same process can still fill **demo** Pepperstone via §4b.
- L57: `"Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position."` — honest about dest send; does **not** claim dest profit.

UI cannot prove claim 3 or 4. It does not contradict claims 1, 2, or 5.

---

## 7. Live GET this slot

| URL | Result |
|---|---|
| `GET http://127.0.0.1:5000/api/health` | `web_fetch` SSRF blocked |
| `GET http://127.0.0.1:5000/api/copy/status` | `web_fetch` SSRF blocked |
| `GET http://127.0.0.1:5000/api/settings` | `web_fetch` SSRF blocked |
| `GET http://localhost:5000/api/health` | `open_page` retrieve fail |

No live body. No claim is PASSed from a live DTO. Runtime `realCopyEnabled` / `realCopyArmed` / dest MTM remain **unpinned**.

---

## 8. Risk to capital

| Book | Proof | Risk |
|---|---|---|
| Live Pepperstone `1369850` | `CTraderFixCopyOpen` / demo-test / matrix refuse `account == "1369850"`; host/sender must be `demo-` / `demo.` | **None** from this process’s `35=D` builders |
| Official LIVE persist hop | `CanPromoteToLive=>false`; `VenueReconciled` const false; persist `AllowFixSend=false` | **None** |
| Demo dest `5328266` | Hosted 20 s `ExecuteDemoCopyAsync` → `Build("D")`; ledger open 0.01 at dest `237339770` fill 4390.2 | **Demo / paper dest equity is at risk.** Not live capital. MTM not measured this slot |
| `REAL_COPY` flag | Env `true`; **not** forced false after logon | Flag is **not** a capital latch. Official hop still blocked by recon/LIVE. Demo hop **ignores** the flag |

`risk_to_capital`: **NONE on live `1369850`.** Demo dest can and does take tickets; that is paper dest risk, not withdrawable live profit, and dest MTM is unmeasured.

---

## 9. Stale priors (do not reuse)

- Hosted logon forces `RealCopyEnabled = false` — **false** vs HEAD (`CTraderFixLogonHostedService` does not assign it).
- Product has no `35=D` assembler / `NewOrderSingleImplemented` const false — **false** vs HEAD (`=> DemoDest`; `CTraderFixCopyOpen.Build("D")`).
- Log line `"NewOrderSingle still unimplemented"` — **stale** vs sibling builder.
- Dashboard dest `$0` as measured dest MTM — **constructor 0**, not a venue query.

---

## 10. Slot close

P500_VERIFY_85 **FAIL**. (1) PASS session has no `35=D`. (2) PASS `CanPromoteToLive=>false`. (3) FAIL flag not forced false after logon. (4) FAIL unproven: demo dest `35=D` hopper exists and dest MTM was not gotten. (5) PASS SHADOW ≠ dest profit DTO. Product source not modified. No secrets printed. This slot did not send `35=D`.
