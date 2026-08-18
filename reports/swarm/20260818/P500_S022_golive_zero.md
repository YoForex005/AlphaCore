# P500_S022 — Go-live is zero: §68 FAIL, §70 FAIL; `REAL_COPY` must stay false

| Field | Value |
|---|---|
| Agent / slot | **P500_S022** (go-live zero pin) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S022_golive_zero.md` |
| Assigned | Read A100 / C14 / A101 if they exist. Write this report. §68 go-live and §70 live FIX acceptance are **FAIL**. Enabling `REAL_COPY` now violates product law **and** expected value. |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Source of truth | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§41** (L1564–1590), **§68** (L2605–2628), **§70** (L2658–2676), Phase 7/8 (L2575–2601) |
| Primary scorecards | `A100_golive_gates.md` (§68 0/19), `C14_golive_still_fail.md` (§68 still 0/19), `A101_live_fix_acceptance.md` (§70 0/14) |
| Later recensus (same integers) | `D42_gates_now.md` (§68 0/19), `D43_s70.md` (§70 0/14), `W500_RESEARCH_68.md` (flag must stay false) |
| Binding siblings | A25 §6.3, A28, A49, A56, A57/C13/D41 (§69 0/12), E002, D69, D97 |
| Secrets printed | **None** |

**This file does not implement anything and does not flip any checkbox.**

---

## 0. Verdict (binding)

**Go-live score is zero.**

| Bar | Score | Status | Authority |
|---|---|---|---|
| Architecture §68 go-live gates | **0 PASS / 19 FAIL** | **FAIL** | A100, re-measured C14, re-scored D42 |
| Architecture §70 live FIX acceptance | **0 PASS / 14 FAIL** | **FAIL** | A101, re-measured D43 |
| §69 first useful version | **0 / 12** (related, not a license) | **FAIL** | A57 / C13 / D41 |
| `REAL_COPY_EXECUTION_ENABLED` | **false** (must stay false) | **CONFIRMED** | §41 + POCO L35 + DI + hosted pin + `.env` L73 |
| Live `35=D` if process starts now | **Impossible** | `SAFE_BY_ABSENCE` | no builder / no initiator / no TRADE send |
| Safe to enable `REAL_COPY` now? | **No** | product-law **and** EV violation | this file |

One FAIL on §68 **or** §70 blocks enablement. Current integers are **0** and **0**. Conjunction required for the first legal live `NewOrderSingle` is therefore **false**.

```text
§68 = 0/19 FAIL
§70 = 0/14 FAIL
REAL_COPY_EXECUTION_ENABLED = false     -- stays false
Live 35=D to *.c-trader.com             -- FORBIDDEN
Enable REAL_COPY now                    -- ILLEGAL (product law) + −EV
```

**Do not** set `REAL_COPY_EXECUTION_ENABLED=true`. **Do not** treat Logon, catalog fetch, a green `dotnet test`, a dashboard `LoggedOn`, or `SAFE_BY_ABSENCE` as a waiver.

---

## 1. Assigned sources exist

All three named reports are on disk. Product was not edited to produce them or this file.

| File | Path | What it measured |
|---|---|---|
| A100 | `D:\Prop\reports\swarm\20260818\A100_golive_gates.md` | Wave-1 working copy of §68: **0 PASS / 19 FAIL** |
| C14 | `D:\Prop\reports\swarm\20260818\C14_golive_still_fail.md` | Post-tests / 15 React pages: **still 0 / 19 for live** |
| A101 | `D:\Prop\reports\swarm\20260818\A101_live_fix_acceptance.md` | §70 14-item scorecard: **0 / 14 FAIL** |

Later files **D42** and **D43** re-measured the same bars after more Domain tests and honesty stamps. Integers did **not** move. This slot inherits those integers; it does not re-run `dotnet test` or open TLS.

Vacuous / demo law (copied from A100 / C14, still binding):

> Fake connector, in-memory DB, unused method, skipped test, seeded rows, default-false flag, or a recovered compile **cannot** become PASS.

---

## 2. Product law — why `REAL_COPY=true` is illegal today

### 2.1 Verbatim §68

Architecture L2605–2607: **Do not enable real copying until all of these are true.** The 19 boxes at L2610–2628 are still `[ ]` in the source of truth.

```text
[ ] FAIL  G01  MT5 historical/live ingestion is stable
[ ] FAIL  G02  duplicate event handling is proven
[ ] FAIL  G03  trade reconstruction tests pass
[ ] FAIL  G04  XAU symbol mappings are verified
[ ] FAIL  G05  quote session stable
[ ] FAIL  G06  trade session stable
[ ] FAIL  G07  cTrader reconciliation works after restart
[ ] FAIL  G08  copy intents are idempotent
[ ] FAIL  G09  unknown execution state recovery works
[ ] FAIL  G10  position sizing conversion is verified
[ ] FAIL  G11  risk engine unit/integration tests pass
[ ] FAIL  G12  stale quote rejection works
[ ] FAIL  G13  stale signal rejection works
[ ] FAIL  G14  shadow copy has sufficient sample
[ ] FAIL  G15  destination costs / slippage measured
[ ] FAIL  G16  kill switch tested
[ ] FAIL  G17  secrets removed from repo / logs
[ ] FAIL  G18  dashboard exposes venue health / risk
[ ] FAIL  G19  manual review completed
```

Enable live copy only when **19/19 PASS** **and** **14/14 §70** **and** an explicit production-flag review. Default remains OFF. One unchecked box is enough.

### 2.2 Verbatim §70

Architecture L2658–2660: **Before production live execution**, all 14 must be true.

```text
[ ] FAIL  1   TRADE FIX Logon is stable
[ ] FAIL  2   ExecutionReports are persisted correctly
[ ] FAIL  3   Position reports reconcile after restart
[ ] FAIL  4   Unique ClOrdID rules are proven
[ ] FAIL  5   Duplicate report handling is proven
[ ] FAIL  6   Unknown-state recovery is proven
[ ] FAIL  7   Partial fills are supported
[ ] FAIL  8   Order rejects are supported
[ ] FAIL  9   Cancel/replace is supported where required
[ ] FAIL  10  Destination position mapping is correct
[ ] FAIL  11  Risk-engine rejection happens before FIX send
[ ] FAIL  12  Real execution is feature flagged
[ ] FAIL  13  Global stop-new-orders works
[ ] FAIL  14  Reconciliation blocks execution while inconsistent
```

A101 / D43: every item is **FAIL**. Items **11** and **14** are the named risk / recon send blocks. Item **12** is the flag itself: a POCO default of `false` is **policy**, not a unit-tested refuse-on-LoggedOn-TRADE choke (`GATE_INCOMPLETE`).

### 2.3 §41 — flag is necessary, never sufficient

Architecture §41 allows QUOTE/TRADE connect, prices, and status/positions **without** new real orders. Actual `NewOrderSingle` requires:

```env
REAL_COPY_EXECUTION_ENABLED=true
```

**plus** runtime risk-engine healthy state. Session-on is not a send license.

Phase 7 still forbids NewOrderSingle after SSL TRADE + mass status + positions + ER/PR parsers + reconciliation. Phase 8 is the only place live send is a deliverable, and only with an **explicit production flag**.

### 2.4 Conjunction for the first legal live `35=D`

Restated from A25 §6.3 / A009 / W500_68. **Do not satisfy the live branch today.**

```text
19/19 §68
AND 14/14 §70
AND REAL_COPY_EXECUTION_ENABLED=true   (explicit, reviewed)
AND RiskEngine.Evaluate on the send path with AllowFixSend
AND TRADE == READY_FOR_EXECUTION (Logon + recon clean)
AND persist unique ClOrdID before send
AND STOP_NEW_EXECUTION == false
AND no blind retry of EXECUTION_STATE_UNKNOWN
AND venue.Kind == LiveQuickFix only after prod review
```

Today the only true clauses are “flag is false” and “no builder exists.” Flipping the flag would make the **license** true while every safety conjunct stays false. That is an operator lie.

### 2.5 What “enable REAL_COPY” would violate, line by line

| Law | Violation if flag is set true now |
|---|---|
| §68 header | Real copying enabled while **0/19** true |
| §70 header | Production live execution licensed while **0/14** true |
| §41 | Flag treated as sufficient; risk-engine healthy state absent |
| Phase 7 | NewOrderSingle armed before Phase 8 |
| §70.11 | No product `RiskEngine.Evaluate` caller (definition + unit facts only) |
| §70.12 | Unbound / unread as a send gate; worker only logs |
| §70.13 | Exclusive `KillSwitchMode`; no API; unused branch |
| §70.14 | `/api/reconciliation/status` stub (zeros + English “NewOrderSingle still off”) |
| §72 persist-before-send | No `GuardedNewOrderSingle`; no unique-11-before-socket |
| G10 / A43 | `QuantityNormalizer` passthrough-shaped (`0.10` lots → `0.10` “qty”); conversion family skipped |
| G14 / G15 | No destination-priced shadow sample; no dest cost tape |
| G19 | No named Phase 8 sign-off (swarm audits are not review) |

Product law is a hard stop. EV is a second, independent stop (next section). Either one is enough.

---

## 3. Expected value — why enablement is −EV even if someone “just wants to try one lot”

Expected value of live copy **today** is not “unknown, so maybe try.” It is **negative** because the loss channels are open and the edge channels are unmeasured.

### 3.1 No measured edge

| Missing input | Why EV cannot be positive |
|---|---|
| G14 shadow sample | No destination-quoted shadow book on selected traders. Source-broker P&amp;L is not dest EV. |
| G15 dest costs / slippage | No Pepperstone tape. Spread, commission, slippage unknown. Copy of a +EV source can be −EV after dest costs. |
| G04 instrument mapping | No discovered numeric tag 55. Guessed `XAUUSD` is forbidden (§72.13) and can reject or map wrong. |
| G10 sizing | Passthrough lots ≠ cTrader units. First live fill can be **1× or 100×** wrong. That is not a small experiment. |
| §69 0/12 | First useful version (connect both brokers, reconstruct, first-3, dest instrument, shadow, React-from-venue) is **not** signed. Live copy is later than that. |

You cannot have +EV from a strategy whose dest fill distribution has never been observed.

### 3.2 Asymmetric, unhedged loss channels (P0)

From A56 / A101 / C14, still open:

| Channel | Mechanism | EV effect |
|---|---|---|
| Blind retry / unknown state (G09, §70.6) | Disconnect after a future send with no `35=H/AF/AN` recovery | Double fill; unbounded |
| Duplicate ER (§70.5) | FAQ: every TRADE connection copies reports; no exec-id fingerprint | Double-book dest position |
| No persist-before-send (§70.4) | Crash between encode and ack | Ghost order or silent miss |
| No recon gate (§70.3, §70.14, G07) | Postgres treated as venue book after restart | Trade against a lie |
| Risk not on hop (§70.11, G11–G13) | Stale quote / stale signal / kill cannot stop a builder | Fill on dead book |
| Kill switch untested (G16, §70.13) | Cannot stop-new without flattening as a side effect | Cannot cut the experiment |
| Fake / stamped health (G05/G06/G18) | Operator believes TRADE is live | Enables the flag from a dashboard lie |
| Two owners / no lease (A46) | Dual Logon → duplicate reports (official FAQ) | Double exposure |
| Secrets / targeting in tree (G17) | Live host default on options; IDs committed | Accidental live socket |

A single 100× size error or a double send on XAU dominates any plausible copy edge. That is −EV with fat left tail, not “small live test.”

### 3.3 Enabling the flag does not create EV; it removes the last honest control

Current capital risk from this process is **NONE** because:

1. POCO `RealCopyExecutionEnabled = false` (`CTraderFixOptions.cs` L35).
2. DI constructs `LiveRuntimeStatus.RealCopyEnabled = false` and comments “Do not arm a flag that cannot be honored safely.”
3. Hosted FIX logon **re-forces** `RealCopyEnabled = false` after QUOTE/TRADE TryLogon.
4. Local `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=false`.
5. `/api/settings` exposes that runtime bit as `featureFlags.REAL_COPY_EXECUTION_ENABLED`.
6. Product `src/` + `apps/` have **no** `35=D` / `MsgType=D` assembler. Only outbound MsgType in `CTraderFixSession` is `35=A` (Logon).
7. Fix-worker, if it ever saw `CTrader:RealCopyExecutionEnabled=true`, **only logs** and still has no send function.

That is **`SAFE_BY_ABSENCE` + pin-false**. It is the correct *current* no-loss outcome. It is **not** §70.12 PASS.

Setting `REAL_COPY=true` now would:

- violate §68 / §70 / §41 immediately (product law);
- teach operators that the license bit is a toy;
- create pressure to add a `35=D` builder “because the flag is on”;
- add **zero** measured dest edge;
- leave every P0 loss channel in place.

That sequence is −EV. “Try one lot on 1369850” is the A56 P0 the risk register already forbids.

### 3.4 Fetch-all / Logon is not EV of copy

Live Manager census (18 groups / ~8460 traders) is **read-only**. FIX Logon `35=A`, if it exists in a later host, is **not** NewOrderSingle. Neither changes G01–G19 or §70.1–14. Do not convert a working catalog into a live-copy argument.

---

## 4. What later lab surface did **not** change

C14 / D42 / D43 already listed these. Restated so this slot cannot be greenwashed:

| Surface | Exists | Live gate? |
|---|---|---|
| Domain unit tests (reconstruction, risk, sizing) | Yes (in-process) | **No** |
| Integration InMemory + FakeMt5 + `Assert.True(true)` | Yes | **No** |
| 15 React pages | Yes | **No** — consume demo / stamped APIs |
| Unique EF indexes / `CopyIntent.IdempotencyKey` | Types exist | **No writer** |
| `RiskEngine.Evaluate` | Method exists | **0 product callers** |
| `CanPromoteToLive => false` (D97) | Hard pin | **Not a send gate** |
| Worker / seeder honesty stamps (`Disconnected`) | Later than A101 | Honest enum ≠ Logon; §70.1 still FAIL |
| Green `dotnet test` | Lab | Not Achiever / Starwave / Pepperstone proof |

A100’s “zero tests” sentence is **stale**. A100’s **0/19** integer is **not**. A101’s worker-`LoggedOn` narrative is **stale** (D32 / D43 / D94). A101’s **0/14** integer is **not**.

---

## 5. Operator one-pager

```text
P500_S022 go-live zero                          2026-08-18
==========================================================
§68 go-live gates                               0 / 19 FAIL   (A100, C14, D42)
§70 live FIX acceptance                         0 / 14 FAIL   (A101, D43)
§69 first useful version                        0 / 12 FAIL   (not a license)
REAL_COPY_EXECUTION_ENABLED                     false (must stay)
Live 35=D possible if process starts now?       No (SAFE_BY_ABSENCE)
Safe to enable REAL_COPY now?                   No
  — product law                                 violated (0/19 and 0/14)
  — expected value                              negative (no dest sample;
                                                open P0 size/dup/retry/recon)
Next legal work                                 InProcess simulator +
                                                risk/recon on the hop;
                                                still no live 35=D
Pepperstone 1369850 as first test?              Forbidden
==========================================================
```

When a later coding wave ticks a box, update **A100 / A101 or a dated successor** with test class, command, timestamp, and SHA-256. Do not tick from this file.

---

## 6. Sign-off (all remain unchecked)

```text
[ ] 19/19 §68 gates PASS
[ ] 14/14 §70 live FIX acceptance PASS
[ ] First useful version (§69) signed
[ ] Explicit production flag reviewed
[ ] Manual review name / date / evidence links recorded
[ ] Default remains OFF if any box is unchecked
```

**Current:** all boxes unchecked. **Real copy: DISABLED.**

Enabling `REAL_COPY_EXECUTION_ENABLED` now is a **product-law FAIL** and an **expected-value FAIL**. Leave the flag false.

---

## 7. Sources

- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §41, Phase 7/8, §68, §69, §70
- `D:\Prop\reports\swarm\20260818\A100_golive_gates.md`
- `D:\Prop\reports\swarm\20260818\C14_golive_still_fail.md`
- `D:\Prop\reports\swarm\20260818\A101_live_fix_acceptance.md`
- `D:\Prop\reports\swarm\20260818\D42_gates_now.md`
- `D:\Prop\reports\swarm\20260818\D43_s70.md`
- `D:\Prop\reports\swarm\20260818\W500_RESEARCH_68.md`
- `D:\Prop\reports\swarm\20260818\E002_no_live_send.md`
- `D:\Prop\reports\swarm\20260818\A56_risk_list.md`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` (L35 `= false`)
- `D:\Prop\src\Infrastructure\DependencyInjection.cs` (forced `RealCopyEnabled = false`)
- `D:\Prop\apps\fix-worker\Worker.cs` (log-only; no send)
- `D:\Prop\apps\api\Program.cs` (`featureFlags.REAL_COPY_EXECUTION_ENABLED`)

---

*End of P500_S022. Product source was not modified. Architecture §68 remains **0/19 FAIL**. Architecture §70 remains **0/14 FAIL**. `REAL_COPY_EXECUTION_ENABLED` stays **false**.*
