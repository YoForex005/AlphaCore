# P500_VERIFY_22 — Adversarial verify (slot 22)

- **slot:** 22
- **date:** 2026-08-18
- **role:** Adversarial verifier. Read the four assigned SUTs. Do **not** trust other agents. FAIL any claim that is not proven from a file this slot read or a live GET this slot completed.
- **method:** Independent `read_file` of `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`, then the logon / copy / dest-send callers those files imply. Targeted `grep`. No Manager attach. No FIX send. No `.env` secret values (boolean + host/account-id flags only). Product source **not** edited.
- **live GET:** `GET http://localhost:5000/api/{copy/status,health,settings,overview}` and `http://127.0.0.1:5000/api/settings` — **blocked** (tool SSRF / retrieve failed). Process-time DTO values are **unproven**. File facts stand.
- **verdict:** **FAIL**
- **risk_to_capital:** **NONE** on live Pepperstone `1369850` (`SAFE_BY_ABSENCE` of a live sender). **Not** absent on demo dest `5328266` (hosted `ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.Build("D")`). This slot sent **0**.

## 0. Assigned claims

Prove from **files** or a **live GET this slot**. Fail anything else.

| # | Claim | Verdict | Why |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** (assigned SUT `CTraderFixSession` only) | File **135/135**. Outbound MsgType is only `(35, "A")` at L96. `grep` `35=D` / `(35, "D")` / `NewOrderSingle` in that file = **0**. `WriteAsync` = **1** (logon bytes). **Residual (does not fail the named SUT):** product `CTraderFixCopyOpen.Build("D")` L95 **is** a `35=D` builder and **is** hosted. W500_VERIFY_22 “tools-only / NOS const false / product sender absent” is **STALE**. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(_) => false` at `BaselineScorer.cs` L211. Product callers of the method besides the definition + unit test = **0**. `FromBaseline` reachable set has no `LIVE` / `LIVE_CANDIDATE`. Persist copies `SuggestedState` (`DealIngestionService` L140). |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | Disproven. The **only** product assignment is DI L41 from `REAL_COPY_EXECUTION_ENABLED`. Lab `.env` L73 = `true`. `CTraderFixLogonHostedService` **reads** the bool at L70 and **never writes** it. No re-pin exists. |
| 4 | sending now cannot be the profit path | **PASS** | `LiveCopyPage` has **no** send control. Live hop persist `AllowFixSend=false` (L324) and `VenueReconciled=const false` (L20) so `GenerateShadowIntentsAsync` cannot emit a live ticket. `CTraderFixCopyOpen` **refuses** account `1369850`. Policy copies the **next** open 1:1 and refuses lookahead (`XauUsdOneToOneCopyPolicy` header). Wanting to send is not an edge. **Residual:** demo dest **can** send now; that is dest **risk**, not a profit algorithm. Dest PnL as an account number is **unproven** (live GET blocked; dashboard field is a constructor `0`). |
| 5 | SHADOW on demo is not destination profit | **PASS** | `FromBaseline` L200–201 emits `SHADOW`. Hopper writes `SHADOW_ONLY` + `ShadowCopyEngine.SimulateEntry` (`CopyTradingService` L336–359). `OverviewDto.DestinationRealPnl` is a **literal 0** (`EfDashboardQueries` L44). `ShadowPnl` is `Sum(SourceVsShadowSlippage)` (L29) — simulated slip, not dest cash. **Residual:** a `SHADOW` seat that is `ADMITTED` can still trip the **separate** demo dest `35=D` hop. That hop is dest risk, not SHADOW PnL. |

**Slot rule:** FAIL if any assigned claim cannot be proven. Claim 3 is **disproven** (the opposite is on disk). Overall **FAIL**.

Claims 1 (scoped), 2, 4, 5 stand independently. They do **not** rescue claim 3.

Census integers from other P500 books (8463 / −$241,580 / −$154,425) were **not** re-measured here and are **not** used as proof.

## 1. Claim 1 — no 35=D builder — PASS (`CTraderFixSession` only)

File read: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135/135**).

`grep` in that file:

| Pattern | Count |
|---|---|
| `35=D` | **0** |
| `(35, "D")` | **0** |
| `NewOrderSingle` | **0** |
| `(35, "A")` | **1** (L96) |
| `WriteAsync` | **1** (L49, logon bytes only) |

Only outbound builder:

```94:108:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
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
```

`TryLogonAsync`: one TCP+TLS connect, one write, one 4096-byte read, then `using` disposes `TcpClient` / `SslStream`. No heartbeat. No tag 11 / 38. No MsgType D.

Hosted caller `CTraderFixLogonHostedService` (read **112/112**) calls `TryLogonAsync` twice (QUOTE 5211, TRADE 5212) and logs `NewOrderSingle still unimplemented`. It never builds D.

### Residual — do not confuse with product-wide “no sender”

`grep` `Build("D")` under product `*.cs` (this slot):

| File | Lines |
|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | L95 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | L139, L163, L197 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | L93 |

`CTraderFixCopyOpen.SendAsync` **is** the dest sender:

- Refuses unless host starts `demo-`, sender starts `demo.`, and account **≠** `1369850` (L37–42).
- Writes `Build("D", …)` after a TRADE logon + SecurityList (L95).
- Called from `CopyTradingService.ExecuteDemoCopyAsync` (L528, L566).
- `CopyTradingHostedService` ticks that method every 20s (L30).

Lab `.env` flags (no secrets): `CTRADER_FIX_HOST` starts `demo-`; `CTRADER_FIX_TRADE_SENDER_COMP_ID` starts `demo.`; `CTRADER_FIX_ACCOUNT_ID=5328266` (≠ `1369850`). So `DemoDest` is **true** and `NewOrderSingleImplemented => DemoDest` (L50) is **true**. W500_VERIFY_22 `NOS=const false` is **STALE**.

Claim 1 is the assigned type. Unscoped “the product has no 35=D builder” would be **FAIL**. That sentence is **not** what this row proves.

## 2. Claim 2 — CanPromoteToLive is false — PASS

File read: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (**212/212**).

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

`FromBaseline` (L189–207) returns only:

`INSUFFICIENT_DATA` | `RISK_BLOCKED` | `SHADOW` | `WATCH` | `EARLY_SCORE`

No `LIVE`. No `LIVE_CANDIDATE`. `AfterHighEarlyScore()` is hard `SHADOW`.

`grep` `CanPromoteToLive` in product `*.cs`:

| Path | Role |
|---|---|
| `BaselineScorer.cs` L211 | Definition |
| `tests/Unit/BaselineScorerTests.cs` L26 | Assert false after three winners go `SHADOW` |
| `reports/.../_tmp_c23_empty/Program.cs` | Offline harness, not a host |

`DealIngestionService` persist (L140): `CurrentState = score.SuggestedState`. That cannot become `LIVE` from this scorer.

Vacuous lock: nothing in the copy hopper **calls** `CanPromoteToLive`. Safety is the hard `false` plus `FromBaseline` never emitting `LIVE`, not an A22 R5-before-R6 gate.

`LiveCopyPage` “LIVE traders” is a **count** of `TraderState.LIVE` (`GetStatusAsync` L58). The scorer cannot populate it.

## 3. Claim 3 — RealCopyEnabled forced false after logon — FAIL

The assigned claim is that logon **forces** `LiveRuntimeStatus.RealCopyEnabled` to **false**. Files prove it does **not**.

### 3.1 The only write

`grep` `RealCopyEnabled =` in product `*.cs` = **1 hit**:

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

### 3.2 What logon actually does

```68:70:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _log.LogInformation(
            "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
            quote.LoggedOn, trade.LoggedOn, _runtime.RealCopyEnabled, account);
```

The hosted logon service writes `Quote` / `Trade` session fields (L60–67). It **does not** assign `RealCopyEnabled`. There is no `_runtime.RealCopyEnabled = false` anywhere after construction.

### 3.3 What the lab binds (flags only; no secrets)

```
D:\Prop\.env L73: REAL_COPY_EXECUTION_ENABLED=true
D:\Prop\.env L106: FEATURE_COPY_TRADING_ENABLED=true
```

API loads that file **before** DI (`apps/api/Program.cs` L10–15 `EnvFile.FindAndLoad()` + `AddEnvironmentVariables()` + `AddTraderIntelligence`).

`/api/settings` echoes `runtime.RealCopyEnabled` (L76). It is **not** a hardcoded false. `GetStatusAsync` reports `RealCopyArmed: _runtime.RealCopyEnabled` (L64). `BuildBlockers` adds `"REAL_COPY_EXECUTION_ENABLED is false"` **only if** the runtime bool is already false (L621–622).

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (L35) and is a **different** POCO. `appsettings.json` `FeatureFlags.LiveCopyEnabled=false` is a **different** name, unused by DI. Neither is a logon re-pin.

### 3.4 Live GET this slot

Attempted:

- `GET http://localhost:5000/api/copy/status`
- `GET http://localhost:5000/api/health`
- `GET http://localhost:5000/api/settings`
- `GET http://localhost:5000/api/overview`
- `GET http://127.0.0.1:5000/api/settings`

All **blocked** (localhost SSRF / retrieve failed). Process-time `realCopyArmed` is therefore **unproven**. File proof is enough to fail the claim: the flag is env-bound `true` and logon does not force it false.

Stale “forced false after logon” sentences (CREDENTIALS pin, older W500 “stays false”, A014 “DI pins false”) are **wrong** for current DI + `.env` + logon host.

## 4. Claim 4 — sending now cannot be the profit path — PASS

### 4.1 UI cannot send

`D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (**71/71**) is GET-only:

- `useCopyStatus` → `GET /api/copy/status`
- `useCopyIntents` → `GET /api/copy/intents`
- No `<button>`, no `POST`, no `onClick` send.
- Empty-state text (L57) admits demo dest **auto-sends** after `ADMITTED`. That is a **status** string, not a click path, and it is **not** a live-Pepperstone profit control.
- Blocker banner (L24): “Live send blockers (Pepperstone cannot be filled)”.

### 4.2 Live hop cannot send a ticket

`CopyTradingService.GenerateShadowIntentsAsync`:

- Evaluates `RiskEngine` with `Reconciled = VenueReconciled` and `VenueReconciled` is **`const false`** (L20, L304).
- Persist **overwrites** `AllowFixSend = false` (L324) regardless of `decision.AllowFixSend`.
- The only “live send” branch (L330) requires `decision.AllowFixSend && CurrentState==LIVE && NewOrderSingleImplemented && VenueReconciled`. `VenueReconciled` is false; `CurrentState==LIVE` cannot come from `FromBaseline`. Branch body only sets `LIVE_SEND_BLOCKED_UNIMPLEMENTED` — it does **not** call `CTraderFixCopyOpen`.

`RiskEngine.Evaluate` (read **189/189**):

- `RealExecutionEnabled == false` is a **comment-only** no-op (L90–93).
- `AllowFixSend` becomes true only if `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150).
- Rejects always set `AllowFixSend=false` (L187).
- Even an `APPROVED` decision is then persisted as `AllowFixSend=false`.

So turning the flag on, or “sending now” from the live page, is **not** a live dest-profit path.

### 4.3 Live 1369850 is refused

```37:42:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
        if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
            || !sender.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
            || account == "1369850")
        {
            return Fail("Refused: dest must be demo FIX, not live 1369850.", host, account, sourceLogin);
        }
```

### 4.4 What sending *would* be, even on demo dest

`XauUsdOneToOneCopyPolicy` (header L57–61): select traders, copy the **next** XAUUSD event 1:1. “It does not wait until a ticket is profitable — that is lookahead and cannot be traded live.”

That is a **copy** path, not a profit path. `ExecuteDemoCopyAsync` also bypasses `RiskEngine.Evaluate` entirely (no call in that method). `MaxAutoLots=0.05` is a source skip, not a dest clip. `AllocationFactor=1m`.

`data/demo_copy_ledger.json` (read this slot; no secrets) has one open dest row: source `305750` / pos `21250421` / dest pos `237339770` / 0.01 lot / `DestClosed=false`. That is dest **exposure**, not measured dest profit. Dashboard `DestinationRealPnl` is constructor **0** (`EfDashboardQueries` L44). Live dest cash is **unproven** this slot (GET blocked).

Claim 4 holds: there is no “send now → dest profit” lever. Demo dest send is dest risk / 1:1 follow, not an edge.

## 5. Claim 5 — SHADOW on demo is not destination profit — PASS

`FromBaseline` promotes quality≥70 / risk<40 to **`SHADOW`**, never dest PnL:

```200:201:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        if (quality >= 70 && risk < 40)
            return TraderState.SHADOW;
```

Unit test `Three_disciplined_winners_go_to_shadow_not_live` asserts `SHADOW` + `CanPromoteToLive==false`.

Hopper for SHADOW (and `LIVE_CANDIDATE` / `LIVE` if they existed) creates `SHADOW_ONLY` intents and simulated fills:

```336:359:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    intent.Status = "SHADOW_ONLY";
                    if (quote is not null && decision.Outcome == RiskDecisionOutcome.Approve)
                    {
                        var fill = _shadow.SimulateEntry(
                            ...
```

`ShadowCopyEngine.SimulateEntry` / `SimulateExit` / `MarkToMarket` write a `ShadowFill` record. No FIX. No dest account. No cash.

Dashboard:

| Field | Source this slot | Meaning |
|---|---|---|
| `OverviewDto.ShadowPnl` | `Sum(ShadowOrders.SourceVsShadowSlippage)` L29 | Simulated slip, not dest |
| `OverviewDto.DestinationRealPnl` | literal **0** L44 | Constructor, not a mark |
| `OverviewDto.XauGross` / `XauNet` | literal **0** L45–46 | Constructor |
| `TraderRowDto.ShadowPnl` | literal **0** L118 | Not dest |
| `RiskDashboardDto` first five decimals | literal **0** L208 | Not dest |

`IsTraderEligible` **does** accept `SHADOW` (rejects only `RISK_BLOCKED` / `DISQUALIFIED` / `PAUSED` / `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH`). Roster can `AUTO_ADMIT` a SHADOW demo/contest name with n≥20 and net>0. That seat can then be sent by `ExecuteDemoCopyAsync`. That is the **demo dest hop**, not the SHADOW book. SHADOW rows remain simulated.

`ShadowPortfolioPage.tsx` still says “Live NewOrderSingle remains disabled.” That sentence is **stale for demo dest** and is **not** dest profit.

Claim 5 holds: the SHADOW book on demo is not destination profit.

## 6. Inventory (this slot)

| File | Lines read | Used for |
|---|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 135/135 | Claim 1 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | 223/223 | Claim 1 residual, 4 |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | 112/112 | Claims 1, 3 |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | 80/80 | Claim 3 (unused POCO default) |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | 212/212 | Claims 2, 5 |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | 189/189 | Claims 3, 4 |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | 71/71 | Claims 3, 4 |
| `D:\Prop\apps\web\src\api\hooks.ts` | copy hooks | Claim 4 (GET only) |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | 625/625 | Claims 1 residual, 3, 4, 5 |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 44/44 | 20s tick includes demo send |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 62/62 | Claim 3 bind |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | 66/66 | Claim 3 object |
| `D:\Prop\apps\api\Program.cs` | 160/160 | Claim 3 env + settings echo |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | dest literals | Claims 4, 5 |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | dest fields | Claims 4, 5 |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | 91/91 | Claim 5 |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | 188/188 | Claims 4, 5 |
| `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs` | 136/136 | Claims 4, 5 |
| `D:\Prop\src\Domain\Copy\CopyLifecycle.cs` | 10/10 | Demo open/close |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | persist tail | Claim 2 |
| `D:\Prop\src\Domain\Enums\TraderState.cs` | 14/14 | Claim 2 |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | 74/74 | Claim 2 |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | 87/87 | Claim 4 (`AllowFixSend` false when flag false) |
| `D:\Prop\apps\api\appsettings.json` | flags | Unused `LiveCopyEnabled=false` |
| `D:\Prop\data\demo_copy_ledger.json` | dest row | Claim 4 residual exposure |
| `D:\Prop\.env` | **flags only** | Claims 1 residual, 3 |

`grep` (no secret values):

- `35=D` / `(35, "D")` in `CTraderFixSession.cs` = **0**
- `Build("D")` product = **5** call sites in 3 sibling session files
- `CanPromoteToLive` product callers = definition + unit test
- `RealCopyEnabled =` product = **1** (DI L41)
- `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true`
- persist `AllowFixSend=false` = **L324**
- `NewOrderSingleImplemented => DemoDest` = **L50** (const-false reports **STALE**)

## 7. Risk to capital

**NONE** on live `1369850`:

- `CTraderFixSession` outbound is `35=A` then dispose.
- `CTraderFixCopyOpen` refuses that account.
- Persist live hop cannot send (`AllowFixSend=false`, `VenueReconciled=false`, no `LIVE` from scorer).

**Not** “safe because RealCopyEnabled is forced false after logon.” That sentence is **false**. Safety on live is **absence of a live sender**.

**Not** `SAFE_BY_ABSENCE` on demo dest `5328266`:

- Lab `.env` satisfies `DemoDest`.
- Hosted 20s tick **can** `Build("D")`.
- Ledger shows an **open** 0.01 dest (305750 → 237339770). Mark-to-market **unproven** this slot.
- `ExecuteDemoCopyAsync` does **not** call `RiskEngine.Evaluate`.

This slot did not send FIX. Secrets not printed. Product source not modified.

## 8. Verdict

**FAIL.**

- (1) **PASS** on `CTraderFixSession` (no `35=D` builder). Product sibling `CTraderFixCopyOpen` **does** build D and is hosted — residual, not a SUT fail.
- (2) **PASS** — `CanPromoteToLive => false`; persist cannot become `LIVE` from `FromBaseline`.
- (3) **FAIL** — `RealCopyEnabled` is env-bound `true`; logon does **not** force false.
- (4) **PASS** — sending now is not a dest-profit lever (no UI send; live hop blocked; live account refused; 1:1 copy ≠ edge).
- (5) **PASS** — SHADOW book is `SimulateEntry` / slip sum, not dest cash.

Overall **FAIL** because claim 3 is disproven.
