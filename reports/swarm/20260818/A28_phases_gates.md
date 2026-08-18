# A28 — Phase 0–8 Checklist, Go-Live Gates, First Useful Version, What Not to Build

**Source:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§67–73  
**Date:** 2026-08-18  
**Agent:** A28  
**Scope:** Planning / acceptance only. No product source was modified.

---

## How to use this document

- Phases are sequential. Do not start a later phase until the prior phase’s **exit criteria** are true.
- Phase 0 is mandatory before any major implementation (§67 Phase 0, §72 rule 1, §73).
- First useful version is **Phases 0–5 + React dashboard**. It does **not** need ML (§69).
- Live `NewOrderSingle` stays **disabled** until go-live gates (§68) **and** live FIX acceptance (§70) are all true, **and** an explicit production flag is set (§67 Phase 8).
- Items marked `[ ]` are the binding checklists. Do not mark them done without evidence (tests, logs, hashes, review notes).

---

## Cross-cutting rules (apply in every phase)

From §72. These are not optional polish.

| # | Rule | Binding implication |
|---|------|---------------------|
| 1 | Audit first | No major implementation until Phase 0 artifacts exist |
| 2 | Preserve production behavior | Changes must not silently alter live MT5/copy behavior |
| 3 | Use migrations | Schema changes only via versioned migrations |
| 4 | Never commit secrets | Fail Phase 0 / go-live if secrets are in repo |
| 5 | Never expose secrets to the browser | Dashboard must not receive FIX/MT5 credentials |
| 6 | Make MT5 callbacks lightweight | Persist/queue; do not do heavy work in callback |
| 7 | Persist before asynchronous processing | Crash after persist must be recoverable |
| 8 | Every execution request must be idempotent | Same intent never double-fires an order |
| 9 | Never blindly retry a possibly-sent order | Unknown send state → recovery, not retry |
| 10 | Reconcile after FIX reconnect | TRADE/QUOTE must reconcile before new work |
| 11 | Independent QUOTE and TRADE session state | Separate connections, sequence files, health |
| 12 | Use TLS in production | FIX SSL required; no plaintext production |
| 13 | Discover cTrader symbols/instrument IDs; do not guess | Security List / mapping table, never hardcode tag 55 |
| 14 | Normalize quantity explicitly | Lot/unit conversion must be tested |
| 15 | ML never bypasses risk | Score is input to risk, not a send path |
| 16 | Trade #3 means early evidence, not proven skill | First-3 counter is a flag, not a quality claim |
| 17 | New entries must expire when stale | Stale signal / stale quote rejection |
| 18 | Reduce/close exposure treated differently from opening more | Risk engine must distinguish close vs increase |
| 19 | Every manual override must be audited | Immutable audit log |
| 20 | Prefer simple systems until data proves complexity | See §71 — do not build Kafka/K8s/ClickHouse/LLM/etc. yet |

---

# Phase 0–8 checklist

## Phase 0 — Audit

**Purpose:** Understand the current system. **No major implementation until this is understood** (§67).

### Required developer output before coding (§73)

#### A. Repository audit — produce all of:

```text
[ ] Current architecture
[ ] Existing MT5 implementation
[ ] Existing DB tables/migrations
[ ] Existing trading/copy functionality
[ ] Existing broker config
[ ] Security issues
[ ] Dead/duplicate code
```

#### B. Gap analysis vs this architecture — classify every component:

```text
[ ] EXISTS_AND_GOOD
[ ] EXISTS_NEEDS_REFACTOR
[ ] MISSING
[ ] DEPRECATED
[ ] UNSAFE
```

#### C. Implementation sequence

```text
[ ] Exact files / modules / migrations that will change are listed
```

#### D. Risk list — identify all of:

```text
[ ] MT5 SDK constraints
[ ] Windows / native DLL constraints
[ ] Source tick-data availability
[ ] cTrader FIX credential / header ambiguity
[ ] Symbol / quantity mapping
[ ] Live-account safety
```

### Phase 0 deliverable map (§67)

```text
[ ] existing architecture map
[ ] existing MT5 code map
[ ] existing DB schema map
[ ] existing background services
[ ] duplicate / dead code report
[ ] security issues
[ ] migration state
[ ] deployment map
```

### Phase 0 exit criteria

```text
[ ] All §73 A–D artifacts exist on disk (not only in chat)
[ ] All §67 Phase 0 maps exist
[ ] Secrets-in-repo scan completed; findings listed
[ ] No major implementation started before these artifacts exist
[ ] Incremental implementation sequence is agreed
```

---

## Phase 1 — Reliable MT5 ingestion

**Depends on:** Phase 0 exit.

**Deliver (§67):**

```text
[ ] Achiever connected
[ ] StarwaveFX connected
[ ] all groups discovered
[ ] accounts synchronized
[ ] history backfilled
[ ] live deals persisted
[ ] idempotency proven
[ ] reconciliation working
```

### Suggested proof (derived from deliver + §72)

```text
[ ] Both brokers stay connected across reconnect
[ ] Group discovery is complete (not a hardcoded subset)
[ ] ~5,000-account sync path is designed and measured (ties to §69 item 3)
[ ] History backfill is restart-safe and resumable
[ ] Live deal persist happens before async processing (§72.7)
[ ] MT5 callbacks remain lightweight (§72.6)
[ ] Duplicate deal / event insert does not create duplicate rows
[ ] Source ledger reconciles to broker history after restart
[ ] Schema changes (if any) shipped as migrations (§72.3)
```

### Phase 1 exit criteria

```text
[ ] All eight §67 Phase 1 delivers are evidenced
[ ] Duplicate-event handling is proven (feeds §68)
[ ] Ingestion is stable enough to reconstruct trades
```

---

## Phase 2 — XAUUSD reconstruction

**Depends on:** Phase 1 exit.

**Deliver (§67):**

```text
[ ] source symbol mappings
[ ] true reconstructed trades
[ ] first-3-trade counter
[ ] unit tests
```

### Suggested proof (derived from deliver + §72.16)

```text
[ ] Source XAUUSD (and broker aliases) mapped per venue — not guessed
[ ] Logical trades reconstruct from deals/positions correctly
[ ] First 3 **completed** XAUUSD trades counted per trader
[ ] Trade #3 is labeled early evidence, not proven skill (§72.16)
[ ] Reconstruction unit tests pass
[ ] Reconstruction tests are part of CI / release gate
```

### Phase 2 exit criteria

```text
[ ] All four §67 Phase 2 delivers are evidenced
[ ] Trade reconstruction tests pass (feeds §68 and §69 items 4–6)
[ ] XAU symbol mappings verified against real broker symbols
```

---

## Phase 3 — Statistical baseline + dashboard

**Depends on:** Phase 2 exit.

**Deliver (§67):**

```text
[ ] deterministic feature engine
[ ] risk flags
[ ] early scoring baseline
[ ] React trader dashboard
```

### Suggested proof (derived from deliver + §69 + §72)

```text
[ ] Features are deterministic given the same input ledger
[ ] Risk flags are explicit, reviewable, and not hidden in ML
[ ] Early score ranks traders without claiming skill beyond evidence
[ ] Dashboard shows traders, scores, first-3 state, venue health
[ ] Dashboard never receives secrets (§72.5)
[ ] Manual overrides (if any) are audited (§72.19)
```

### Phase 3 exit criteria

```text
[ ] All four §67 Phase 3 delivers are evidenced
[ ] Deterministic trader/risk score exists (§69 item 7)
[ ] Ranking works (§69 item 8)
[ ] React UI shows ingestion + reconstruction + scores (§69 item 12)
```

---

## Phase 4 — cTrader QUOTE integration

**Depends on:** Phase 3 exit (dashboard health is a deliver).  
**Hard constraint:** **No live trading yet** (§67 Phase 4).

**Deliver (§67):**

```text
[ ] SSL FIX quote session
[ ] logon / session health
[ ] Security List
[ ] XAU instrument mapping
[ ] live XAU quote
[ ] quote persistence / cache
[ ] dashboard health
```

### Suggested proof (derived from deliver + §72.11–13)

```text
[ ] QUOTE uses TLS / SSL in any non-local environment (§72.12)
[ ] QUOTE session state is independent of TRADE (§72.11)
[ ] Instrument ID discovered via Security List — not guessed (§72.13)
[ ] Pepperstone XAUUSD instrument ID stored in mapping table
[ ] Live quote cached with timestamp (for stale-quote rejection later)
[ ] Dashboard shows QUOTE venue health
[ ] NewOrderSingle remains disabled
```

### Phase 4 exit criteria

```text
[ ] All seven §67 Phase 4 delivers are evidenced
[ ] Quote session is stable (feeds §68)
[ ] XAU instrument mapping verified (feeds §68 and §69 item 10)
[ ] Still no live trading
```

---

## Phase 5 — Shadow copy

**Depends on:** Phase 4 exit (destination quote pricing required).

**Deliver (§67):**

```text
[ ] copy intents
[ ] shadow entries / exits
[ ] destination quote pricing
[ ] shadow P&L
[ ] source-vs-shadow analysis
```

### Suggested proof (derived from deliver + §72.8, 14, 17, 18)

```text
[ ] CopyIntent is persisted and idempotent
[ ] Shadow fills use destination quotes, not source ticks assumed equal
[ ] Quantity normalized explicitly (lots / units / contract size) (§72.14)
[ ] New entries expire when stale (§72.17)
[ ] Reduce/close is not treated as “open more” (§72.18)
[ ] Shadow P&L includes measured destination costs/slippage path
[ ] Source-vs-shadow analysis has a sufficient sample before any live copy (§68)
[ ] React shows shadow book, P&L, and drift
```

### Phase 5 exit criteria

```text
[ ] All five §67 Phase 5 delivers are evidenced
[ ] First useful version 12-item acceptance can be scored (§69)
[ ] Shadow sample + cost/slippage measurement underway (feeds §68)
[ ] ML is still not required
```

---

## Phase 6 — ML

**Depends on:** Phase 5 exit **and** data quality proven.  
**Deliver only after data quality is proven** (§67 Phase 6).  
**Hard constraint:** ML never bypasses risk (§72.15). First useful version does **not** need this phase (§69).

**Deliver (§67):**

```text
[ ] training dataset
[ ] chronological split
[ ] XGBoost
[ ] probability calibration
[ ] top-N evaluation
[ ] comparison against deterministic baseline
```

### Suggested proof

```text
[ ] Dataset built from reconstructed, reconciled, de-duplicated trades
[ ] Split is chronological (no leakage from future into past)
[ ] Model compared to Phase 3 deterministic baseline — must beat it on agreed metric
[ ] Calibration reported (not only AUC)
[ ] Top-N vs baseline ranking documented
[ ] Model output cannot send an order; risk engine remains the gate (§72.15)
[ ] No automated model self-promotion (§71)
[ ] No LLM, deep learning, or RL substituted for this baseline (§71)
```

### Phase 6 exit criteria

```text
[ ] All six §67 Phase 6 delivers are evidenced
[ ] Quality vs deterministic baseline is measured, not assumed
[ ] If ML does not beat baseline, baseline remains the production scorer
```

---

## Phase 7 — cTrader TRADE read / reconciliation

**Depends on:** Phase 4 (QUOTE proven) recommended; Phase 5 (shadow exists) recommended.  
**Hard constraint:** **Still keep real `NewOrderSingle` disabled** (§67 Phase 7).

**Deliver (§67):**

```text
[ ] SSL FIX trade session
[ ] OrderMassStatusRequest
[ ] RequestForPositions
[ ] ExecutionReport parser
[ ] PositionReport parser
[ ] reconciliation
```

### Suggested proof (derived from deliver + §70 + §72.9–12)

```text
[ ] TRADE uses TLS / SSL (§72.12)
[ ] TRADE session state is independent of QUOTE (§72.11)
[ ] ExecutionReports persisted correctly (§70.2)
[ ] Position reports reconcile after restart (§70.3, §72.10)
[ ] Duplicate report handling proven (§70.5)
[ ] Unique ClOrdID rules exist even while send is disabled (§70.4)
[ ] Unknown-state recovery designed (no blind retry) (§72.9)
[ ] Reconciliation blocks any future execution while inconsistent (§70.14)
[ ] NewOrderSingle still compiled-off / feature-flagged off
```

### Phase 7 exit criteria

```text
[ ] All six §67 Phase 7 delivers are evidenced
[ ] TRADE session stable (feeds §68)
[ ] cTrader reconciliation works after restart (feeds §68 and §70.3)
[ ] Real NewOrderSingle still disabled
```

---

## Phase 8 — Risk-controlled execution

**Depends on:** Phase 7 exit **and** all §68 go-live gates **and** all §70 live FIX acceptance.  
**Hard constraint:** **Enable only with explicit production flag** (§67 Phase 8). Safe by default: real submission OFF.

**Deliver (§67):**

```text
[ ] risk engine
[ ] execution intent
[ ] idempotency
[ ] NewOrderSingle
[ ] ExecutionReport lifecycle
[ ] cancel / replace
[ ] unknown-state recovery
[ ] kill switch
```

### Suggested proof (derived from deliver + §68 + §70 + §72)

```text
[ ] Risk-engine rejection happens **before** FIX send (§70.11)
[ ] ExecutionIntent is persisted, unique, idempotent (§67, §72.8)
[ ] Unique ClOrdID rules proven (§70.4)
[ ] Partial fills supported (§70.7)
[ ] Order rejects supported (§70.8)
[ ] Cancel/replace supported where required (§70.9)
[ ] Destination position mapping correct (§70.10)
[ ] Unknown-state recovery proven — never blindly retry (§70.6, §72.9)
[ ] Stale quote rejection works (§68, §72.17)
[ ] Stale signal rejection works (§68, §72.17)
[ ] Position sizing conversion verified (§68, §72.14)
[ ] Reduce/close vs open-more distinguished (§72.18)
[ ] Global stop-new-orders / kill switch tested (§70.13, §68)
[ ] Reconciliation blocks execution while inconsistent (§70.14)
[ ] Real execution is feature flagged; default OFF (§70.12)
[ ] Explicit production flag required to enable send
```

### Phase 8 exit criteria

```text
[ ] All eight §67 Phase 8 delivers are evidenced
[ ] Entire §68 go-live gate list is checked
[ ] Entire §70 live FIX acceptance list is checked
[ ] Manual review completed
[ ] Production flag remains OFF until that review signs off
```

---

## Phase dependency map

```text
Phase 0 Audit
    ↓
Phase 1 Reliable MT5 ingestion
    ↓
Phase 2 XAUUSD reconstruction
    ↓
Phase 3 Statistical baseline + React dashboard
    ↓
Phase 4 cTrader QUOTE (SSL, Security List, live XAU quote)
    ↓
Phase 5 Shadow copy  ──────────────►  FIRST USEFUL VERSION (§69)
    ↓                                  (ML not required)
Phase 6 ML (only if data quality proven; must beat baseline)
    ↓
Phase 7 cTrader TRADE read / reconciliation (NewOrderSingle still OFF)
    ↓
    ALL §68 go-live gates + ALL §70 live FIX acceptance
    ↓
Phase 8 Risk-controlled execution (explicit production flag only)
```

---

# Go-live gates (§68)

**Do not enable real copying until all of these are true.**

These gates sit **after** Phase 7 capability exists and **before** Phase 8 send is enabled.

```text
[ ] MT5 historical / live ingestion is stable
[ ] duplicate event handling is proven
[ ] trade reconstruction tests pass
[ ] XAU symbol mappings are verified
[ ] quote session stable
[ ] trade session stable
[ ] cTrader reconciliation works after restart
[ ] copy intents are idempotent
[ ] unknown execution state recovery works
[ ] position sizing conversion is verified
[ ] risk engine unit / integration tests pass
[ ] stale quote rejection works
[ ] stale signal rejection works
[ ] shadow copy has sufficient sample
[ ] destination costs / slippage measured
[ ] kill switch tested
[ ] secrets removed from repo / logs
[ ] dashboard exposes venue health / risk
[ ] manual review completed
```

### Gate-to-phase traceability

| Gate | Earliest phase that can prove it | Must still be true at go-live |
|------|----------------------------------|-------------------------------|
| MT5 historical/live ingestion stable | 1 | yes |
| Duplicate event handling proven | 1 | yes |
| Trade reconstruction tests pass | 2 | yes |
| XAU symbol mappings verified | 2 / 4 | yes |
| Quote session stable | 4 | yes |
| Trade session stable | 7 | yes |
| cTrader reconciliation after restart | 7 | yes |
| Copy intents idempotent | 5 / 8 | yes |
| Unknown execution state recovery | 7 / 8 | yes |
| Position sizing conversion verified | 5 / 8 | yes |
| Risk engine unit/integration tests | 8 | yes |
| Stale quote rejection | 4 / 8 | yes |
| Stale signal rejection | 5 / 8 | yes |
| Shadow copy sufficient sample | 5 | yes |
| Destination costs/slippage measured | 5 | yes |
| Kill switch tested | 8 | yes |
| Secrets removed from repo/logs | 0, re-check always | yes |
| Dashboard venue health/risk | 3 / 4 | yes |
| Manual review completed | 8 sign-off | yes |

**Count:** 19 gates. Zero skips. A single unchecked item blocks real copy.

---

# First useful version — 12-item acceptance (§69)

The first genuinely useful system does **not** need ML.

It should already be able to:

```text
[ ]  1. Connect to both MT5 brokers.
[ ]  2. Discover all groups.
[ ]  3. Synchronize ~5,000 accounts.
[ ]  4. Capture XAUUSD trades correctly.
[ ]  5. Reconstruct logical trades.
[ ]  6. Detect the first 3 completed XAUUSD trades.
[ ]  7. Produce a deterministic trader / risk score.
[ ]  8. Rank traders.
[ ]  9. Connect to cTrader QUOTE FIX securely.
[ ] 10. Discover the Pepperstone XAUUSD instrument ID.
[ ] 11. Shadow-copy selected traders using destination quotes.
[ ] 12. Show all of this in React.
```

**Only after this works should ML be judged.**

### Mapping to phases

| # | Acceptance item | Phase that delivers it |
|---|-----------------|------------------------|
| 1 | Connect to both MT5 brokers | 1 |
| 2 | Discover all groups | 1 |
| 3 | Synchronize ~5,000 accounts | 1 |
| 4 | Capture XAUUSD trades correctly | 1–2 |
| 5 | Reconstruct logical trades | 2 |
| 6 | Detect first 3 completed XAUUSD trades | 2 |
| 7 | Deterministic trader/risk score | 3 |
| 8 | Rank traders | 3 |
| 9 | Connect to cTrader QUOTE FIX securely | 4 |
| 10 | Discover Pepperstone XAUUSD instrument ID | 4 |
| 11 | Shadow-copy selected traders using destination quotes | 5 |
| 12 | Show all of this in React | 3–5 |

**Definition of first useful version:** items 1–12 all true. Phase 6/7/8 are **not** part of this bar.

---

# Live FIX execution acceptance (§70)

Not part of the first useful version. Required **before production live execution** (Phase 8 enablement), in addition to §68.

```text
[ ]  1. TRADE FIX Logon is stable.
[ ]  2. ExecutionReports are persisted correctly.
[ ]  3. Position reports reconcile after restart.
[ ]  4. Unique ClOrdID rules are proven.
[ ]  5. Duplicate report handling is proven.
[ ]  6. Unknown-state recovery is proven.
[ ]  7. Partial fills are supported.
[ ]  8. Order rejects are supported.
[ ]  9. Cancel / replace is supported where required.
[ ] 10. Destination position mapping is correct.
[ ] 11. Risk-engine rejection happens before FIX send.
[ ] 12. Real execution is feature flagged.
[ ] 13. Global stop-new-orders works.
[ ] 14. Reconciliation blocks execution while inconsistent.
```

**Default:** real order submission remains OFF until §68 + §70 + explicit production flag.

---

# What not to build yet (§71)

Do **not** add initially:

```text
[DO NOT] Kafka
[DO NOT] Kubernetes
[DO NOT] ClickHouse
[DO NOT] LLM / AI API
[DO NOT] deep learning
[DO NOT] reinforcement learning
[DO NOT] complex microservice mesh
[DO NOT] cross-region active-active FIX execution
[DO NOT] automated model self-promotion
```

These can be revisited **only when measurements justify them**.

### Also do not build (implied by §§67–73)

```text
[DO NOT] Major implementation before Phase 0 audit + §73 A–D
[DO NOT] Live NewOrderSingle in Phases 0–7
[DO NOT] Guessed cTrader symbol / instrument IDs (tag 55)
[DO NOT] Blind retry of a possibly-sent order
[DO NOT] Secrets in repo, logs, or browser bundles
[DO NOT] Schema edits without migrations
[DO NOT] ML path that bypasses the risk engine
[DO NOT] Treating trade #3 as proven skill
[DO NOT] Treating reduce/close as “open more”
[DO NOT] New entries that never expire
[DO NOT] Unaudited manual overrides
[DO NOT] Complexity beyond the Phase 0–5 first useful version until data proves it
```

---

# Required output before coding (Phase 0 reminder, §73)

Before large implementation changes, produce on disk:

1. **Repository audit** — architecture, MT5, DB/migrations, trading/copy, broker config, security, dead/duplicate code.
2. **Gap analysis** — `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.
3. **Implementation sequence** — exact files / modules / migrations.
4. **Risk list** — MT5 SDK, Windows/native DLL, tick-data availability, FIX credential/header ambiguity, symbol/quantity mapping, live-account safety.

Then implement incrementally, phase by phase.

---

# Sign-off boxes

## First useful version (Phases 0–5, §69)

```text
[ ] Phase 0 exit
[ ] Phase 1 exit
[ ] Phase 2 exit
[ ] Phase 3 exit
[ ] Phase 4 exit
[ ] Phase 5 exit
[ ] 12/12 first-useful-version items true
[ ] ML not required for this sign-off
```

## Live copy enablement (Phase 8, §§68–70)

```text
[ ] First useful version signed off
[ ] Phase 6 judged only after data quality (optional vs baseline)
[ ] Phase 7 exit (NewOrderSingle still disabled until flag)
[ ] 19/19 §68 go-live gates true
[ ] 14/14 §70 live FIX acceptance items true
[ ] Explicit production flag reviewed
[ ] Manual review completed
[ ] Default remains OFF if any box is unchecked
```

---

# Source extract index

| Section | Title | Used as |
|---------|-------|---------|
| 67 | Engineering Phases | Phase 0–8 delivers and hard constraints |
| 68 | Go-Live Gates | 19-item live-copy block list |
| 69 | Acceptance Criteria for the First Useful Version | 12-item useful-system bar (no ML) |
| 70 | Acceptance Criteria for Live FIX Execution | 14-item pre-send bar |
| 71 | What Not to Build Yet | Explicit exclusion list |
| 72 | Senior Engineer Rules | Cross-cutting rules 1–20 |
| 73 | Required Developer Output Before Coding | Phase 0 A–D artifacts |

End of A28.
