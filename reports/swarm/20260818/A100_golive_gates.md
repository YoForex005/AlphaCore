# A100 — Architecture §68 go-live gates (working checklist)

| Field | Value |
|---|---|
| Agent | A100 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A100_golive_gates.md` |
| Source of truth | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§68 Go-Live Gates** (lines 2605–2628) |
| Supporting | §67 Phase 8, §69 first useful version, §70 live FIX acceptance, §72 rules 4–18 |
| Sibling | A28 (phase map), A29 (gap), A57 (§69 0/12), A19/A76 (secrets/logs), A23/A24/A43/A47/A48/A72 |
| Product source modified | **No** |

**Law (verbatim §68):** Do not enable real copying until **all** of these are true.

**Scoreboard (measured 2026-08-18):** **0 PASS / 19 FAIL.** Live `NewOrderSingle` stays **OFF**. One FAIL blocks enablement.

Do not mark a box `[x]` without on-disk evidence (passing tests, logs, hashes, review notes). In-memory demo seed, unused Domain methods, and a FIX worker that stamps `LoggedOn` are **not** proof.

---

## Working checklist (copy of §68)

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

**Enable live copy only when:** `19/19 PASS` **and** `14/14` §70 **and** explicit production flag reviewed. Default remains OFF.

---

## How to flip a gate

| Status | Meaning |
|---|---|
| **FAIL** | Not proven. Leave `[ ]`. |
| **PASS** | Evidence listed under that gate exists on disk and still holds. Mark `[x]` and change status to PASS. |
| Vacuous / demo | Fake connector, in-memory DB, unused method, or seeded rows **cannot** become PASS. |

Phases that *can* produce evidence are from A28. The gate must still be true at go-live.

---

## G01 — MT5 historical/live ingestion is stable

| | |
|---|---|
| §68 text | `MT5 historical/live ingestion is stable` |
| Status | **FAIL** |
| Earliest phase | 1 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- C# registry wires **two fakes**, not Manager API: `DemoBrokerFactory.CreateDefault()` in `D:\Prop\src\Infrastructure\DependencyInjection.cs`.
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` — `ConnectAsync` sets a bool; 4 canned logins (`10001`, `10002`, `10003`, `99001`), canned XAU round-trips.
- `apps/mt5-worker/Worker.cs` syncs those fakes every 30s, then rebuilds only the four demo logins.
- Default persistence is **in-memory** when `ConnectionStrings:TraderIntelligence` is missing/`<SECRET>` (`AddTraderIntelligence`).
- No EF migrations (`EnsureCreated` in `apps/api/Program.cs`). No live deal subscription. C++ `mt5-sdk` is not the C# collector.

**PASS when:** Achiever **and** StarwaveFX stay connected across reconnect; all groups/accounts discovered; history backfill restart-safe; live deals persist before async work; measured ~5k path; not the fake factory.

---

## G02 — Duplicate event handling is proven

| | |
|---|---|
| §68 text | `duplicate event handling is proven` |
| Status | **FAIL** |
| Earliest phase | 1 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- Unique index `(BrokerId, DealTicket)` on `mt5_deals` (`TraderDbContext`).
- `EfTradingStore.UpsertDealAsync` returns `false` if the ticket exists.
- `tests/Unit` and `tests/Integration` contain **zero** `[Fact]` / `[Theory]` sources.

A unique index without a replay/restart test is not “proven.”

**PASS when:** Replaying the same deal/event stream (and FIX ER later) does not create duplicate rows; tested in CI; source ledger matches broker history after restart.

---

## G03 — Trade reconstruction tests pass

| | |
|---|---|
| §68 text | `trade reconstruction tests pass` |
| Status | **FAIL** |
| Earliest phase | 2 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `TradeReconstructor` exists (`D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs`) and is used by `ReconstructionScoringService` on **demo** deals.
- A21 inventory / A27 class names are backlog only. No reconstruction test class on disk.

**PASS when:** A27 reconstruction fixtures pass in CI (completed XAU, first-3, reversals, volume units). Demo seeder success is not a test.

---

## G04 — XAU symbol mappings are verified

| | |
|---|---|
| §68 text | `XAU symbol mappings are verified` |
| Status | **FAIL** |
| Earliest phase | 2 / 4 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `SymbolNormalizer` ships hardcoded aliases (`XAUUSD`, `GOLD`, …). Used only inside reconstruction.
- No persisted mapping verified against live Achiever/Starwave symbols.
- No cTrader Security List. `FixSimulationHarness` defaults `symbol = "XAUUSD"` (guessing tag 55 is forbidden, §72.13).

**PASS when:** Source aliases confirmed on both MT5 brokers; Pepperstone instrument ID **discovered** and stored; tests reject guessed `55=XAUUSD`.

---

## G05 — Quote session stable

| | |
|---|---|
| §68 text | `quote session stable` |
| Status | **FAIL** |
| Earliest phase | 4 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `apps/fix-worker/Worker.cs` every 15s sets QUOTE `LastInboundAt = UtcNow` and `Status = ReadyForMarketData`. **No socket.**
- `QuickFix.Net 1.8.0` is referenced; no initiator, SSL, dictionary, or MD handler.
- Parser + `FixSimulationHarness` only.

**PASS when:** Independent SSL QUOTE session stays logged on across reconnect; heartbeats real; dashboard age from last **venue** quote, not a worker stamp.

---

## G06 — Trade session stable

| | |
|---|---|
| §68 text | `trade session stable` |
| Status | **FAIL** |
| Earliest phase | 7 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- Same worker stamps TRADE `Status = LoggedOn` whether or not `CTrader:RealCopyExecutionEnabled` is true.
- No TRADE SSL initiator. NewOrderSingle correctly **not sent**, but session is not stable — it is **not connected**.

**PASS when:** Independent SSL TRADE logon is stable; seq files persist; disconnect/reconnect proven. Send remains flagged off until G01–G19 + §70.

---

## G07 — cTrader reconciliation works after restart

| | |
|---|---|
| §68 text | `cTrader reconciliation works after restart` |
| Status | **FAIL** |
| Earliest phase | 7 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- No `OrderMassStatusRequest` / `RequestForPositions` client.
- No `StartupReconciliationCoordinator` / `ReconciliationGate` (A47: MISSING).
- Dashboard `ReconciliationPage` import has **no page file**.

**PASS when:** After process restart, mass-status + positions reconcile to `destination_positions`; inconsistent book **blocks** `READY_FOR_EXECUTION`; integration test exists (A10).

---

## G08 — Copy intents are idempotent

| | |
|---|---|
| §68 text | `copy intents are idempotent` |
| Status | **FAIL** |
| Earliest phase | 5 / 8 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `CopyIntent.IdempotencyKey` + unique index exist.
- **No writer** creates intents from source deals. `ShadowCopyEngine` is unused outside its file.
- No idempotency tests (A42).

**PASS when:** Same source event cannot insert a second intent or fire a second order; persist-before-send; unique key proven under retry/crash.

---

## G09 — Unknown execution state recovery works

| | |
|---|---|
| §68 text | `unknown execution state recovery works` |
| Status | **FAIL** |
| Earliest phase | 7 / 8 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `ExecutionOrderStateMachine.AfterDisconnectWithUnknownAck()` → `ExecutionStateUnknown`; `MayRetryNewOrderSingle` only for `NotSent`/`Rejected`.
- No recovery service, no OrderStatus path, no test (A10 unknown-execution suite MISSING).

**PASS when:** After send+disconnect the intent is `EXECUTION_STATE_UNKNOWN`; **no** blind `NewOrderSingle` retry; recover via status/ER/positions; tested.

---

## G10 — Position sizing conversion is verified

| | |
|---|---|
| §68 text | `position sizing conversion is verified` |
| Status | **FAIL** |
| Earliest phase | 5 / 8 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `QuantityNormalizer` is `sourceLots * allocationFactor` stepped to dest min/max. **Passthrough-shaped** if allocation=1.
- No dest contract-size / unit spec from Security List. No `SourceDestinationQuantityConversionTests` (A43).

**PASS when:** Known source-lot → dest-qty fixtures pass; `Never_passthrough_MT5_lots` fails any `requested_quantity = source_lots` shortcut.

---

## G11 — Risk engine unit / integration tests pass

| | |
|---|---|
| §68 text | `risk engine unit/integration tests pass` |
| Status | **FAIL** |
| Earliest phase | 8 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `RiskEngine.Evaluate` exists (`QUOTE_STALE`, `SIGNAL_STALE`, kill-switch, recon block).
- `grep Evaluate(` under product code hits **only the definition**. Dead path.
- Zero risk tests.

**PASS when:** A23/A27 unit + integration suite passes: each hard limit, quote/signal stale, reduce vs open, kill switch, recon block, **zero** FIX outbound on reject.

---

## G12 — Stale quote rejection works

| | |
|---|---|
| §68 text | `stale quote rejection works` |
| Status | **FAIL** |
| Earliest phase | 4 / 8 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `RiskLimits.MaxQuoteAge = 3s` vs `CTraderFixOptions.MaxQuoteAgeMs = 5000` — two unbound defaults (A72).
- `QUOTE_STALE` is never called from Application/workers. No destination quote feed.
- FIX worker health is a timestamp stamp, not quote age.

**PASS when:** `quote_age > configured_max` rejects OPEN/INCREASE on **live and shadow**; config not compile constants; `QuoteFreshnessGuardTests` pass; logged-on ≠ fresh.

---

## G13 — Stale signal rejection works

| | |
|---|---|
| §68 text | `stale signal rejection works` |
| Status | **FAIL** |
| Earliest phase | 5 / 8 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `RiskEngine` emits `SIGNAL_STALE`; `CopyIntentExpiry.IsExpired` exists. Neither is on a send/shadow path. No tests.

**PASS when:** Expired `CopyIntent` cannot open more; tested; reduce/close not treated as open-more (§72.17–18).

---

## G14 — Shadow copy has sufficient sample

| | |
|---|---|
| §68 text | `shadow copy has sufficient sample` |
| Status | **FAIL** |
| Earliest phase | 5 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `ShadowCopyEngine` simulates fills in memory; **no call sites**.
- Overview “shadow P&L” is `Sum(ShadowOrders.SourceVsShadowSlippage)` (`EfDashboardQueries`) — not a sample of destination-priced trades.
- Four demo logins are not a go-live sample (A24).

**PASS when:** Selected traders shadow-copied on **destination** quotes; sample size and window agreed and stored; source-vs-shadow analysis exists before any live copy.

---

## G15 — Destination costs / slippage measured

| | |
|---|---|
| §68 text | `destination costs / slippage measured` |
| Status | **FAIL** |
| Earliest phase | 5 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- Engine can compute `SourceVsShadowSlippage` if given a quote. No live/recorded Pepperstone tape. No cost model from dest fills.

**PASS when:** Slippage/spread/commission measured from destination quotes (and later dest fills); numbers drive `shadow_performance`, not source-broker P&L.

---

## G16 — Kill switch tested

| | |
|---|---|
| §68 text | `kill switch tested` |
| Status | **FAIL** |
| Earliest phase | 8 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- `KillSwitch` entity + `KillSwitchMode`. Risk engine would reject OPEN/INCREASE on `StopNewExecution` / `EmergencyFlatten`.
- No command API, no audit write path used, `Evaluate` unused, no tests (A48).

**PASS when:** `STOP_NEW_EXECUTION` proven (does not flatten); flatten is a distinct authorized path; audited; integration test; §70.13 also true.

---

## G17 — Secrets removed from repo / logs

| | |
|---|---|
| §68 text | `secrets removed from repo / logs` |
| Status | **FAIL** |
| Earliest phase | 0 (re-check always) |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- A19: **no live passwords** in `D:\Prop` (good, not sufficient).
- A76: **no** central `FixWireRedactor` / Serilog denylist. Call-site luck is not §57.
- `DemoSeeder` persists live venue **targeting** (Achiever `57.128.141.65` / manager `2027`; Starwave `84.201.6.142`).
- Product-root `.gitignore` still a Phase 0 risk (A29). Architecture markdown holds the same identifiers.

**PASS when:** Re-scan finds no committed secrets; FIX tags 553/554 and `Password=` redacted to `***`; dashboard never receives credentials (§72.4–5). Identifier policy signed.

---

## G18 — Dashboard exposes venue health / risk

| | |
|---|---|
| §68 text | `dashboard exposes venue health / risk` |
| Status | **FAIL** |
| Earliest phase | 3 / 4 |
| Checkbox | `[ ]` |

**Current evidence (not enough):**

- API: `/api/overview`, `/api/fix/sessions`, `/api/risk` (`apps/api/Program.cs`).
- `Mt5Healthy` = `brokers > 0`. `QuoteHealthy` / `TradeHealthy` read **stamped** session rows. `GetBrokersAsync` hardcodes `Connected = true`.
- `apps/web/src/App.tsx` imports 14 pages. `apps/web/src/pages/` is **empty** — UI cannot render.
- Risk DTO daily P&L / DD / XAU exposure are zeros.

**PASS when:** React shows real MT5/QUOTE/TRADE health, quote age, risk rejects, kill-switch mode, shadow book — from venue facts, not seed/heartbeat. No secrets in the browser.

---

## G19 — Manual review completed

| | |
|---|---|
| §68 text | `manual review completed` |
| Status | **FAIL** |
| Earliest phase | 8 sign-off |
| Checkbox | `[ ]` |

No signed review exists. Swarm audits (A01–A87, A28, A57) are **not** Phase 8 sign-off.

**PASS when:** Named reviewer records G01–G18 PASS + §70 14/14 + production flag decision (default OFF if any box unchecked).

---

## Gate-to-phase traceability (A28)

| ID | Gate | Earliest phase | Must be true at go-live | Status |
|----|------|----------------|-------------------------|--------|
| G01 | MT5 historical/live ingestion stable | 1 | yes | **FAIL** |
| G02 | Duplicate event handling proven | 1 | yes | **FAIL** |
| G03 | Trade reconstruction tests pass | 2 | yes | **FAIL** |
| G04 | XAU symbol mappings verified | 2 / 4 | yes | **FAIL** |
| G05 | Quote session stable | 4 | yes | **FAIL** |
| G06 | Trade session stable | 7 | yes | **FAIL** |
| G07 | cTrader reconciliation after restart | 7 | yes | **FAIL** |
| G08 | Copy intents idempotent | 5 / 8 | yes | **FAIL** |
| G09 | Unknown execution state recovery | 7 / 8 | yes | **FAIL** |
| G10 | Position sizing conversion verified | 5 / 8 | yes | **FAIL** |
| G11 | Risk engine unit/integration tests | 8 | yes | **FAIL** |
| G12 | Stale quote rejection | 4 / 8 | yes | **FAIL** |
| G13 | Stale signal rejection | 5 / 8 | yes | **FAIL** |
| G14 | Shadow copy sufficient sample | 5 | yes | **FAIL** |
| G15 | Destination costs/slippage measured | 5 | yes | **FAIL** |
| G16 | Kill switch tested | 8 | yes | **FAIL** |
| G17 | Secrets removed from repo/logs | 0, re-check always | yes | **FAIL** |
| G18 | Dashboard venue health/risk | 3 / 4 | yes | **FAIL** |
| G19 | Manual review completed | 8 sign-off | yes | **FAIL** |

**Count:** 19 gates. Zero skips. A single unchecked item blocks real copy.

---

## Related bars (not substitutes)

| Bar | Score | Note |
|-----|-------|------|
| §69 first useful version (A57) | **0 / 12** | Required before judging ML; still not a live-copy license |
| §70 live FIX acceptance (A28) | **0 / 14** | Required **in addition** to this list before Phase 8 send |
| §71 do-not-build | Kafka / K8s / ClickHouse / LLM / mesh | Correctly absent; do not add to pass these gates |

`CTrader:RealCopyExecutionEnabled` default **false** and “worker refuses NewOrderSingle” are **controls**, not G01–G19 PASS.

---

## Sign-off

```text
[ ] 19/19 §68 gates PASS
[ ] 14/14 §70 live FIX acceptance PASS
[ ] First useful version (§69) signed (A57)
[ ] Explicit production flag reviewed
[ ] Manual review name / date / evidence links recorded
[ ] Default remains OFF if any box is unchecked
```

**Current:** all boxes unchecked. **Real copy: DISABLED.**

End of A100.
