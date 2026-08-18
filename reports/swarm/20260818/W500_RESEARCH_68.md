# W500_RESEARCH_68 — Confirm `REAL_COPY_EXECUTION_ENABLED` must stay **false**

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_68.md` |
| Agent / slot | W500 research **68** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (product `src/`, `apps/`, `tests/`, architecture, live-census reports) |
| Second tree | `D:\Projects\YoPips\Backend\C++ Backend PropFirm` (`src` + name search) |
| Topic | Confirm `REAL_COPY_EXECUTION_ENABLED` **must stay false**. **No** `35=D` NewOrderSingle until **risk + recon** gates. |
| Goal context | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; **copy to cTrader must not send live orders yet (no loss).** |
| Product source modified | **No.** This report is the only write from this slot (plus INDEX / SWARM_LOG pointers). |
| Test source modified | **No.** |
| Secrets printed | **None.** `.env` quoted only as `REAL_COPY_EXECUTION_ENABLED=false`. No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Method | Full `read_file` of options, session, hosted logon, DI, API `Program.cs`, fix-worker, `LiveRuntimeStatus`, `RiskEngine`, FSM, `BrokerCatalogSeed`, dashboard queries, LiveCopy page, architecture §§41/68/70. Targeted `grep` of product `*.cs`/`*.json`/`*.csproj` for `35=D`, `NewOrderSingle`, `REAL_COPY`, `RiskEngine.Evaluate`. YoPips C++ `src` for `35=D` / `NewOrderSingle` / `cTrader` / `c-trader` / `REAL_COPY` = **0**. Prior live census cited, **not re-attached**. **No TLS opened this slot. No Logon sent. No order sent.** |
| Binding law | Architecture **§41** (lines 1564–1590), **§68** (2605–2628), **§70** (2658–2676), **§42** recon-before-send, Phase 7/8 (2575–2601). A100/C14/D42 = §68 **0/19**. A101/D43 = §70 **0/14**. |
| Siblings (do not treat as this file) | W500_57 (flag defaults), W500_50 (`CTraderFixSession` 35=D=0), W500_59 (no RiskEngine hop), A003 (no-loss), A009 (conjunction), D69 (POCO default), E002/E038, A25 §6.3 |

**Honesty rule:** a compile-time `= false` is a default. A `GetValue(..., false)` fallback is a default. A dashboard `featureFlags` `false` is a **display floor**, not a unit-tested refuse-on-LoggedOn-TRADE gate. `AllowFixSend` is a DTO bit. A comment / log / `LastError` that names `NewOrderSingle` is **not** a builder. A live TLS **Logon `35=A`** is **not** a NewOrderSingle. Absence of `35=D` is **`SAFE_BY_ABSENCE`**, not a §68 / §70 PASS. Fetching all Manager groups/traders is **read-only** and does **not** license send. Do **not** print FIX passwords.

---

## 0. Verdict (binding)

**CONFIRMED — `REAL_COPY_EXECUTION_ENABLED` must stay `false`. No `35=D` until risk + recon gates are measured PASS.**

One FAIL on Architecture §68 or §70 blocks enablement. Current scorecards are **0/19** and **0/14**. Flipping the flag today would be an operator lie: there is still **no** NewOrderSingle assembler, **no** persist-before-send, **no** product `RiskEngine.Evaluate` caller, and **no** TRADE recon that can return `Reconciled=true` from the venue.

| Claim | Measured result | Class |
|---|---|---|
| Must `REAL_COPY_EXECUTION_ENABLED` stay false? | **Yes** | **CONFIRMED** |
| Architecture §41 default | `REAL_COPY_EXECUTION_ENABLED=false` (L1572) | law |
| C# POCO default | `CTraderFixOptions.RealCopyExecutionEnabled = false` (L35) | fail-closed default |
| Runtime send bit | `LiveRuntimeStatus.RealCopyEnabled = false` at DI (L41) **and** re-forced after FIX logon (hosted L68) | pin |
| Local `.env` | `REAL_COPY_EXECUTION_ENABLED=false` (line 73; value only, no secrets) | operator floor |
| `/api/settings` `featureFlags.REAL_COPY_EXECUTION_ENABLED` | `runtime.RealCopyEnabled` (forced false) | display |
| `FEATURE_COPY_TRADING_ENABLED` | hardcoded **false** next to it (`Program.cs` L76) | unused twin |
| Literal `35=D` / `(35, "D")` / `MsgType="D"` in product `src/` + `apps/` `*.cs` | **0** | **MISSING** builder |
| `CTraderFixSession` outbound tag 35 | **`"A"` only** (`BuildLogon` L96) | Logon, not order |
| QuickFIX/n / `GuardedNewOrderSingle` | **0** package refs in `TraderIntelligence.Fix.CTrader.csproj` | initiator **MISSING** |
| YoPips C++ `src` `35=D` / `NewOrderSingle` / `cTrader` / `c-trader` / `REAL_COPY` | **0** | not a second cTrader sender |
| Architecture §68 go-live | **0 PASS / 19 FAIL** (A100 / C14 / D42; checkboxes still `[ ]` in source-of-truth L2610–2628) | **block send** |
| Architecture §70 live FIX | **0 PASS / 14 FAIL** (A101 / D43; items 11 + 14 are the named risk/recon send blocks) | **block send** |
| Risk gate on send path | `RiskEngine.Evaluate` product callers = **0** (definition + 5 unit facts only) | **GATE_INCOMPLETE** |
| Recon gate on send path | `/api/reconciliation/status` is a **stub** (zeros + English “NewOrderSingle still off”). No `OrderMassStatusRequest` / `RequestForPositions` encoder | **GATE_INCOMPLETE** |
| Live `35=D` if process starts now | **Impossible** | **`SAFE_BY_ABSENCE`** + flag pin |
| Fetch ALL Achiever+Starwave groups/traders blocked by this flag? | **No** — catalog is Manager read-only | independent |
| Risk to capital from copy path | **NONE** (this process cannot open a Pepperstone/cTrader position) | no loss |

One-line:

```text
REAL_COPY_EXECUTION_ENABLED must stay false. §68=0/19, §70=0/14. No 35=D builder. Risk/recon not on the wire. Fetch-all 18/8460 is read-only. SAFE_BY_ABSENCE. CONFIRMED.
```

Do **not** set `REAL_COPY_EXECUTION_ENABLED=true`. Do **not** add a `35=D` sender in this task. Do **not** treat Logon, catalog fetch, or `AllowFixSend` as a go-live waiver.

---

## 1. Why the flag must stay false (law, not preference)

### 1.1 Architecture §41 — necessary, not sufficient

```1564:1590:D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
# 41. Real Execution Feature Flags

Default:

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

This allows:

- connecting,
- receiving prices,
- requesting orders/positions,
- validating FIX connectivity,

without automatically placing new real orders.

Actual NewOrderSingle submission should require:

```env
REAL_COPY_EXECUTION_ENABLED=true
```

plus runtime risk-engine healthy state.
```

Session-on is **not** a send license. QUOTE/TRADE logon may exist for recon. `REAL_COPY_EXECUTION_ENABLED=true` is **necessary and not sufficient**.

### 1.2 Phase 7 still forbids NewOrderSingle

Architecture Phase 7 (L2575–2584) lists SSL TRADE + mass status + positions + ER/PR parsers + reconciliation, then: **“Still keep real NewOrderSingle disabled.”** Phase 8 (L2586–2601) is the only place NewOrderSingle is a deliverable, and only with an **explicit production flag**.

### 1.3 §68 — do not enable real copying until **all 19** are true

Source-of-truth checkboxes (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L2609–2628) are still all `[ ]`, including the two this slot names:

| Gate | §68 text | Why still FAIL (this re-read) |
|---|---|---|
| G07 | cTrader reconciliation works after restart | No `35=AF` / `35=AN` encoder. API recon is a stub. Ownership `MarkReconciled()` is a boolean the caller can set without a venue book. |
| G11 | risk engine unit/integration tests pass | Five in-process facts; engine **not** in DI; **zero** product `Evaluate(` sites. Not a live risk gate. |
| G05/G06 | quote/trade session stable | Hosted service can one-shot TLS Logon `35=A` then **disposes** sockets. Worker stamps `Disconnected`. Not a keep-alive TRADE session. |
| G10 | position sizing conversion verified | `QuantityNormalizer` unused; passthrough `0.10→0.10` (W500_38/58). |
| G19 | manual review completed | Not done. This file is research, not review. |

Prior live scorecards (A100, C14, D42): **0 PASS / 19 FAIL**. This slot did **not** flip any checkbox. One FAIL blocks enablement.

### 1.4 §70 — risk + recon are explicit live-FIX items

```2658:2676:D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
# 70. Acceptance Criteria for Live FIX Execution

Before production live execution:

```text
1. TRADE FIX Logon is stable.
...
11. Risk-engine rejection happens before FIX send.
12. Real execution is feature flagged.
13. Global stop-new-orders works.
14. Reconciliation blocks execution while inconsistent.
```
```

A101 / D43: **0 / 14 FAIL**. Items **11** and **14** are the named risk/recon send blocks. Item **12** is the flag — default false is **policy**, not a proven choke on a live TRADE socket.

**Conjunction for the first legal `35=D` (A009 / A25 §6.3, restated):**

```text
19/19 §68
AND 14/14 §70
AND REAL_COPY_EXECUTION_ENABLED=true   (explicit, reviewed)
AND RiskEngine.Evaluate on the send path with AllowFixSend
AND TRADE READY_FOR_EXECUTION (Logon + recon clean)
AND persist unique ClOrdID before send
AND no blind catch-up / no retry of EXECUTION_STATE_UNKNOWN
```

Today **none** of those AND-clauses except “flag is false” and “no builder” are true. Therefore the flag **must stay false**.

---

## 2. Measured product pins (flag is false everywhere it exists)

| Surface | What was read | Value |
|---|---|---|
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` L32–35 | POCO initializer + XML “Default OFF” | **`false`** |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` L38–41 | `new LiveRuntimeStatus { RealCopyEnabled = false }` + comment “Do not arm a flag that cannot be honored safely.” | **forced false** |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` L68 | `_runtime.RealCopyEnabled = false;` **after** QUOTE/TRADE TryLogon | **re-forced false** |
| `D:\Prop\apps\fix-worker\Worker.cs` L21 | `GetValue("CTrader:RealCopyExecutionEnabled", false)` | fallback **false** |
| `D:\Prop\apps\api\Program.cs` L54, L75 | health + settings expose `runtime.RealCopyEnabled` | **false** |
| `D:\Prop\apps\api\Program.cs` L76 | `FEATURE_COPY_TRADING_ENABLED` | **false** |
| `D:\Prop\apps\api\appsettings.json` L44–47 | `FeatureFlags:LiveCopyEnabled` | **false** (different name, not the §41 token) |
| `D:\Prop\.env` L73 | `REAL_COPY_EXECUTION_ENABLED=false` | **false** (gitignored; value only) |
| `D:\Prop\docs\architecture.md` L20 | safety default | **false** |
| `D:\Prop\docs\deployment.md` L82 | “until cTrader integration is verified” | **false** |
| `D:\Prop\README.md` L28 | “Real NewOrderSingle is **off**” | **false** |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` L5 | JSX: gates still required including the flag | display, not a binder |

### 2.1 Binding honesty (do not greenwash the choke)

Architecture env name `REAL_COPY_EXECUTION_ENABLED` is **not** bound onto `CTraderFixOptions` by ASP.NET (would need `CTrader__RealCopyExecutionEnabled`). Fix-worker reads **`CTrader:RealCopyExecutionEnabled`**, a **different** key; `apps/fix-worker/appsettings*.json` has **no** `CTrader` block, so the fallback `false` always wins unless someone injects that nested key.

Even if an operator set the env token `true`:

1. DI still constructs `RealCopyEnabled = false`.
2. Hosted logon **overwrites** it to `false`.
3. Worker, if it saw `true`, **only logs** a warning and still has **no** send function (L45–46).
4. There is still **no** `35=D` assembler.

So today’s safety is **pin-false + SAFE_BY_ABSENCE**, not a single named gate that a unit test proves refuses `35=D` on a LoggedOn TRADE socket. That is the correct *current* no-loss outcome. It is **not** a reason to flip the flag “to try one lot.”

### 2.2 Settings cannot legally arm send

Live API (`Program.cs`) is **minimal APIs only** — no `AddControllers()` / `MapControllers()` in the file that was read (156 lines). `SettingsController` (`LiveCopyEnabled` PUT → Redis `settings:flags:live_copy`) is therefore **unbound** on the running host. Even if it were bound:

- it writes a **different name** (`LiveCopyEnabled`), not `REAL_COPY_EXECUTION_ENABLED`;
- Redis is not required (`/api/health` reports redis `healthy=false`);
- it cannot create a NewOrderSingle builder.

`/api/settings` GET returns `featureFlags.REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled` which DI + hosted service pin **false**.

---

## 3. No `35=D` NewOrderSingle (measured this pass)

### 3.1 `CTraderFixSession.cs` (135 / 135 lines)

Public surface: **one** method, `TryLogonAsync`. Only socket write:

```46:50:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
            var seq = 1;
            var logon = BuildLogon(senderCompId, targetCompId, senderSubId, targetSubId, username, password, seq);
            var bytes = Encoding.ASCII.GetBytes(logon);
            await ssl.WriteAsync(bytes, timeoutCts.Token);
            await ssl.FlushAsync(timeoutCts.Token);
```

`BuildLogon` body starts with `(35, "A")` (L96). Other tags: 34, 49, 56, 50, 57, 52, 98, 108, 141, 553, 554. **No** 11 / 38 / 40 / 44 / 54 / 55 (ClOrdID / OrderQty / OrdType / Price / Side / Symbol). TCP/SSL are `using` / `await using` — sockets **disposed** before return. No keep-alive TRADE session that could later emit `D`.

`grep` this file: `35=D` = **0**, `NewOrderSingle` = **0**, `(35, "D")` = **0**.

### 3.2 Product C# census (this pass)

`grep` `35=D` / `(35, "D")` / `MsgType="D"` under `D:\Prop\src` `*.cs`: **0**.

`NewOrderSingle` hits in product `src/` + `apps/` are **name / log / LastError only**:

| File | Role | Wire? |
|---|---|---|
| `CTraderFixOptions.cs` L33 | XML comment | No |
| `CTraderFixLogonHostedService.cs` L70 | info log “still disabled” | No |
| `DependencyInjection.cs` L40 | comment | No |
| `LiveRuntimeStatus.cs` L44 | snapshot copy when flag false | No |
| `BrokerCatalogSeed.cs` L105 | TRADE `LastError` “logon/recon only; NewOrderSingle off” | No |
| `DemoSeeder.cs` L101 | TRADE `LastError` | No |
| `apps/api/Program.cs` L68 | recon stub note | No |
| `apps/fix-worker/Worker.cs` L22, L41, L46 | start log + LastError + refuse warning | No |
| `ExecutionOrderStateMachine.cs` L35 | `MayRetryNewOrderSingle` status math | No socket |
| `LiveCopyPage.tsx` / `OverviewPage.tsx` | UI copy | No |

`TraderIntelligence.Fix.CTrader.csproj`: Hosting + Configuration + Logging + EF only. **No** `QuickFIXn.Core` / `QuickFIXn.FIX44`.

### 3.3 YoPips C++ is not a second cTrader sender

`grep` of `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` for `35=D`, `NewOrderSingle`, `cTrader`, `c-trader`, `REAL_COPY`: **no matches**.

That tree has MT5 `SendTrade` (dealer mutation on the **source** Manager). It is **not** a Pepperstone/cServer FIX NewOrderSingle path. Do not treat YoPips `SendTrade` as copy-to-cTrader.

---

## 4. Risk gate is **not** ready (so flag stays false)

`D:\Prop\src\Domain\Risk\RiskEngine.cs` (190 lines) is a **pure function**. `AllowFixSend` computation:

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

Unreconciled increasing actions reject with `VENUE_NOT_RECONCILED` (L84–85). `Reject(...)` always sets `AllowFixSend = false` (L180–188).

**Empty-if honesty (L91–93):** when `RealExecutionEnabled == false` and action is not `CloseExposure`, the `if` body is **empty**. The engine may still return `Outcome=Approve` / `Reason=APPROVED` with `AllowFixSend=false`. Unit fact `Real_flag_false_never_allows_fix_send` (`RiskEngineTests.cs` L21–26) asserts exactly that: Approve + `AllowFixSend=false` while fixture `RealExecutionEnabled = false` (L72).

That is **unsafe if a future sender keys off `Outcome==Approve` and ignores `AllowFixSend`**. Another reason the production flag must stay false **and** no `35=D` builder may be added until:

1. `RiskEngine` is registered in DI;
2. a worker persists `CopyIntent` → `Evaluate` → `risk_decisions` → `ExecutionIntent` only on approve/reduce **and** `AllowFixSend`;
3. the empty-if is a real `REAL_COPY_DISABLED` reject (or the sender is proven to consult `AllowFixSend`);
4. G11 integration tests pass on that hop.

`grep` `Evaluate(` / `new RiskEngine` / `IRiskEngine` under `D:\Prop\src` + `D:\Prop\apps`: **definition only**. Callers = `tests/Unit/RiskEngineTests.cs` (5 facts). `AddTraderIntelligence` does **not** register `RiskEngine`.

`AllowFixSend` is **not** a socket.

---

## 5. Recon gate is **not** ready (so flag stays false)

Architecture §42 / §70.3 / §70.14: after TRADE logon, **block new executions**, run OrderMassStatus + RequestForPositions, reconcile, only then consider send.

What exists:

| Piece | Status |
|---|---|
| `GET /api/reconciliation/status` (`Program.cs` L62–68) | Hardcoded `unknownPositions=0`, `mismatches=0`, `orphanFills=0`, note = “recon runs only after FIX TRADE logon; NewOrderSingle still off” |
| `ExecutionOrderStateMachine.RequiresReconciliation` | Status math only |
| `FixSessionOwnership.MarkReconciled()` | In-memory bool the **caller** sets; no venue book |
| `CTraderFixSession` MsgTypes | `A` only. **Missing** `AF` (mass status), `AN` (positions), `H` (status), `D`/`F`/`G` |
| Persist-before-send / `GuardedNewOrderSingle` | **MISSING** |
| `MayRetryNewOrderSingle` after send | **false** (unit fact) — correct math, no send to retry |

A stub that reports zeros is **anti-evidence** for G07 / §70.14. Recon **cannot** run on the wire today. Therefore recon **cannot** PASS, and the flag **must stay false**.

Worker honesty (L36–46): stamps TRADE `Disconnected` + `LastError = "No live TRADE socket. NewOrderSingle remains off."` If config `real` is true it **logs a warning** and still does not send.

---

## 6. Fetch ALL Achiever + Starwave groups/traders is independent of send

Prior **measured** Manager census (`D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`, probe JSON utc `2026-08-18T08:42:16Z`; `CREDENTIALS_AND_COPY_STATUS.md`):

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| Achiever | HTTP proxy | 8 | 6512 | 1506 |
| StarwaveFX | direct | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Dashboard `/api/groups` and `/api/traders` are catalog reads. `GetTradersAsync` walks **every** `Mt5Accounts` row (left-join scores; L99–120 of `EfDashboardQueries.cs`) — **no** `Take` on the trader list. Ingest catalog uses `GroupRequestArray("*")` + per-group `UserRequestArray` (prior W500_55/56). Those walks do **not** read `REAL_COPY_EXECUTION_ENABLED`.

**This slot did not live-reattach.** Census numbers are the last measured probe, not a new Connect. Fetch-all remains the **goal** and is **not** blocked by keeping the copy flag false.

Copy-to-cTrader **destination** stays: QUOTE/TRADE Logon `35=A` for session proof + future recon. **No** `35=D` until the conjunction in §1.4.

---

## 7. What would have to be true before the flag may flip

Do **not** treat this list as a waiver. Default remains OFF if any box is unchecked.

1. §68 **19/19 PASS** with on-disk evidence (not FakeMt5, not in-memory demo, not unused methods).
2. §70 **14/14 PASS**, including:
   - **11** risk rejection **before** FIX send (product `Evaluate` on the hop; spy `Submit=0` when flag false / unreconciled / stale quote);
   - **14** recon **blocks** execution while inconsistent (real `35=AF`/`AN` + persist + refuse).
3. Unique ClOrdID persist-before-send; `MayRetryNewOrderSingle` false on unknown; no blind catch-up when the flag later goes true (§63 / A53).
4. Quantity conversion G10 measured (not passthrough).
5. Named manual review. Committed `appsettings*` / `.env.example` / compose stay `false` until that review.
6. A `35=D` builder that is **unreachable** unless the flag **and** `AllowFixSend` **and** `READY_FOR_EXECUTION`. Unit-test the refuse on LoggedOn TRADE.

Until then: **fetch + score + SHADOW only.**

```text
[DO NOT] Enable REAL_COPY_EXECUTION_ENABLED to “try one lot”
[DO NOT] Send 35=D from an MT5 callback
[DO NOT] Retry 35=D after a broken send
[DO NOT] Flush a backlog when the flag later flips true
```

---

## 8. Files read / grepped (this slot)

| Path | Why |
|---|---|
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default false |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | only `35=A` writer |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | re-pin false after logon |
| `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` | no QuickFIX |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | `RealCopyEnabled=false` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | snapshot copy |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | unused send authority |
| `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs` | retry/recon math |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | TRADE LastError |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | all-accounts traders; dest P&L 0 |
| `D:\Prop\apps\api\Program.cs` | settings + recon stub |
| `D:\Prop\apps\api\appsettings.json` | `LiveCopyEnabled=false` |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | unbound Redis PUT |
| `D:\Prop\apps\fix-worker\Worker.cs` | refuse + Disconnected stamp |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | UI gates |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | flag-false AllowFixSend |
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` | §§41, 42, 61, 68, 70 |
| `D:\Prop\docs\architecture.md`, `docs\deployment.md`, `README.md` | committed defaults |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | 18/8460 |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | flag forced false |
| `D:\Prop\reports\swarm\20260818\A100_golive_gates.md`, `C14_golive_still_fail.md`, `D42_gates_now.md` | §68 0/19 |
| `D:\Prop\reports\swarm\20260818\D43_s70.md`, `A101_live_fix_acceptance.md` | §70 0/14 |
| YoPips `...\C++ Backend PropFirm\src` | 0 cTrader FIX senders |

SHA-256 **not recomputed** this slot (no shell). Line counts from full `read_file`: options 80, session 135, hosted 113, worker 51, API Program 156, DI 59, runtime 66, RiskEngine 190.

---

## 9. Slot answers

1. **Must `REAL_COPY_EXECUTION_ENABLED` stay false?**  
   **Yes.** Architecture §41 default, §68 0/19, §70 0/14, risk unused, recon stub, no persist-before-send.

2. **May we emit `35=D` NewOrderSingle now?**  
   **No.** Builder absent (`SAFE_BY_ABSENCE`). Even after a builder exists: not until risk + recon gates **and** the flag are jointly true.

3. **Does fetch-all conflict with no-loss?**  
   **No.** Manager catalog is read-only. Copy destination stays logon/recon-only. Constraint wins: **no live orders yet.**

4. **Risk to capital if this process starts now?**  
   **NONE** from cTrader copy. The process cannot assemble `35=D`. Flag is pinned false.

---

## 10. JSON (slot contract)

```json
{"slot":68,"verdict":"CONFIRMED_MUST_STAY_FALSE","evidence":"REAL_COPY_EXECUTION_ENABLED=false at §41, CTraderFixOptions L35, DI L41, hosted L68, .env L73, /api/settings. Product 35=D=0; only outbound MsgType is 35=A. §68 0/19, §70 0/14. RiskEngine.Evaluate product callers=0. Recon API is a stub. YoPips src has 0 cTrader senders. Census 18/8460 read-only.","risk_to_capital":"NONE"}
```
