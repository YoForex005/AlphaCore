# P500_S044 — Architecture profit goal: future destination-net PnL inside risk limits

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S044_arch_profit_goal.md` |
| Agent | P500_S044 (read-only architecture, profit goal + non-goals) |
| Date | 2026-08-18 |
| Source of truth | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **v2.0** |
| Read | **§§1–4** (lines 11–214) and **§§68–71** (lines 2605–2698) |
| Product source edited | **No.** This file is the only write. |
| Secrets printed | **None** |

**Verdict:** The binding profit goal is **future execution-venue-net (destination-net) P&L while remaining inside risk limits**. It is **not** “who made the most money in their first 3 trades.” What-not-to-build **includes ML-first**: §1 forbids using ML first; §69 says the first useful version does **not** need ML; §71 lists LLM/AI API, deep learning, reinforcement learning, and automated model self-promotion as initial non-goals.

---

## 0. Mandate (do not reinterpret)

This slot extracts the **objective function** and the **non-goal list** from the v2 architecture prompt. It does not implement ranking, scoring, risk, or FIX. It does not edit product.

Two sentences that must stay glued together:

1. Optimize for **future destination-net PnL inside risk limits**.
2. Do **not** build ML first (or any §71 item) to chase that goal.

---

## 1. Primary business goal (§3)

Quoted in full from the source of truth:

```128:156:D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
# 3. Primary Business Goal

We have roughly:

```text
5,000+ MT5 trader accounts
```

Most trading activity is expected to be:

```text
XAUUSD
```

The platform must identify traders whose behavior after their first few completed XAUUSD trades suggests a higher probability of **future copyable profitability**.

The target is not:

```text
Who made the most money in their first 3 trades?
```

The target is:

```text
Given the behavior visible up to now,
which traders have the highest probability of generating
positive future execution-venue-net P&L
while remaining inside risk limits?
```
```

### 1.1 Binding parse

| Phrase in §3 | Binding meaning |
|---|---|
| “first few completed XAUUSD trades” | **Evidence window**, not the objective. Trade #3 is early evidence (§1 change #2, §72.16). |
| “future copyable profitability” | Profit that can be **copied onto the destination venue**, not source-demo leaderboard vanity. |
| “Who made the most money in their first 3 trades?” | **Explicit anti-goal.** Do not rank or promote on first-3 source cash. |
| “positive future execution-venue-net P&L” | **Official label:** destination (Pepperstone / cServer FIX) **net** after costs/slippage — not source MT5 realized PnL, not first-3 sum. |
| “while remaining inside risk limits” | Destination-net is **constrained**. A high-PnL path that breaches risk is a **fail**, not a win. |

Shorthand used by this slot and by operators:

```text
future destination-net PnL inside risk limits
    ≠
who made most in first 3
```

“Execution-venue-net” in §3 is the architecture’s name for **destination-net**. The venue is the cTrader/cServer FIX account (§1 pipeline, §4 right-hand side). Net means after destination costs/slippage — §68 even gates live copy on “destination costs/slippage measured.”

### 1.2 What this forbids in selection / scoring / dashboards

- Ranking traders by first-3 closed XAU cash, source gross, or “who is hottest after trade #3.”
- Promoting to live money because early source PnL is large.
- Treating dashboard source `NetSourcePnl` (or any first-N sum) as the official label.
- Optimizing a model or rule set to reconstruct **past** first-3 winners rather than **future** destination-net inside risk.

### 1.3 What this requires instead

- Features from **behavior visible up to now** (completed XAUUSD tape, risk conduct, not fabricated MFE/MAE — §1 change #5).
- An official label of **future destination-net P&L**, only after shadow + measured destination costs exist.
- Hard risk sitting **between** scoring and execution (§1 pipeline; §4 `Shadow copy → CopyIntent → Risk Engine`).
- Default after a strong early score: **SHADOW**, not live (§1 change #4).

---

## 2. Executive verdict and pipeline (§1) — how the goal is realized

§1 says the overall direction is **correct**, but the naive stack is **wrong**:

```text
MT5 Manager API → ML → cTrader FIX     ← NOT the system
```

The correct system (quoted from §1) is a **long deterministic pipeline**:

```text
MT5 SOURCE BROKERS
(Achiever + StarwaveFX + future brokers)
        ↓
MT5 Manager API collectors
        ↓
Raw immutable trading data
        ↓
Trade reconstruction
        ↓
XAUUSD feature/scoring engine
        ↓
Rules + statistics + ML ranking
        ↓
Shadow copy
        ↓
Copy intent
        ↓
Deterministic risk engine
        ↓
Execution intent
        ↓
cTrader FIX 4.4 adapter
        ↓
Pepperstone / cServer account
        ↓
Execution reports + reconciliation
```

Read this against the §3 goal:

- **Source side** produces immutable deals and reconstructed XAUUSD behavior.
- **Scoring** estimates *probability of future copyable profitability* — rules + statistics first; ML ranking is a **later** stage in the *same* pipeline, not the first box.
- **Shadow copy** is where destination-quote economics start to exist (the only honest preview of execution-venue-net).
- **Deterministic risk engine** is what “inside risk limits” *is*. It is not optional garnish.
- **FIX + execution reports + reconciliation** are how destination-net is **measured**, not assumed from source PnL.

### 2.1 Changes in §1 that bind the profit goal

Quoted items that must not be “reinterpreted” as product shortcuts:

| # | §1 change | Effect on the profit goal |
|---|---|---|
| 1 | Do not build everything at once. Prove data → reconstruction → shadow → scoring → controlled live. | Destination-net is **not** available until shadow/costs exist. |
| 2 | Three trades are not enough to declare skill. Trade #3 = early probability/ranking, not classification. | First-3 cash is **not** skill and **not** the label. |
| 3 | **Do not use ML first.** Deterministic statistical baseline first. ML must beat it out-of-sample. | **ML-first is a what-not-to-build.** |
| 4 | Do not send a trader to real money after trade #3. Default = SHADOW. | Early score ≠ live destination PnL. |
| 5 | Do not fabricate MFE/MAE from closed deals alone. | Do not invent features to “win” a first-3 contest. |
| 6 | Do not call cTrader an LP unless it is. | Destination is an **external execution venue**. Venue-net ≠ LP book. |
| 8 | Do not write a raw TcpClient FIX engine. | Destination-net measurement depends on a mature FIX stack. |
| 10 | Never blindly convert MT5 lots to cTrader OrderQty. | Wrong size falsifies destination-net. |

Header of the same file: **AI/LLM dependency: None.** **Execution default: Disabled** until shadow/reconciliation/risk are proven.

---

## 3. Role (§2) and high-level architecture (§4)

§2 assigns a principal / FIX / MT5 / quant / security role. Material duties for *this* goal:

- deterministic and testable boundaries
- avoiding future-data leakage in ML (when ML exists)
- preventing duplicate orders (duplicate fills poison destination-net)
- protecting credentials
- documenting decisions that affect production trading

§4 is the same pipeline drawn as a box diagram: React → ASP.NET Core API → PostgreSQL / Redis / SignalR; left = MT5 source (Achiever, StarwaveFX, future brokers) through reconstruction, features + scoring, shadow; right = cTrader FIX 4.4 QUOTE + TRADE on Pepperstone/cServer. The join is:

```text
Shadow copy ──> CopyIntent ──> Risk Engine ──> execution side
```

There is **no** “first-3 PnL → NewOrderSingle” edge. There is **no** “ML → FIX” edge.

---

## 4. Go-live and acceptance that protect the goal (§§68–70)

The profit goal is **future destination-net inside risk**. Live send is therefore gated on proving the measurement and the constraint — not on a pretty first-3 leaderboard.

### 4.1 §68 Go-Live Gates (do not enable real copying until all true)

Quoted checklist (source lines 2609–2629):

```text
[ ] MT5 historical/live ingestion is stable
[ ] duplicate event handling is proven
[ ] trade reconstruction tests pass
[ ] XAU symbol mappings are verified
[ ] quote session stable
[ ] trade session stable
[ ] cTrader reconciliation works after restart
[ ] copy intents are idempotent
[ ] unknown execution state recovery works
[ ] position sizing conversion is verified
[ ] risk engine unit/integration tests pass
[ ] stale quote rejection works
[ ] stale signal rejection works
[ ] shadow copy has sufficient sample
[ ] destination costs/slippage measured
[ ] kill switch tested
[ ] secrets removed from repo/logs
[ ] dashboard exposes venue health/risk
[ ] manual review completed
```

Gates that exist **because** the label is destination-net inside risk:

- **shadow copy has sufficient sample** — without it there is no preview of copyable PnL
- **destination costs/slippage measured** — without it “venue-net” is fiction
- **risk engine unit/integration tests pass** + **kill switch tested** — “inside risk limits”
- **stale quote / stale signal rejection** — stale fills are not the official label
- **position sizing conversion verified** — lot-blind copy falsifies destination-net
- **reconciliation after restart / unknown-state recovery / idempotent intents** — double sends and lost fills falsify destination-net

None of the 19 boxes say “top first-3 source winners are live.”

### 4.2 §69 First useful version — no ML required

Quoted:

```2633:2654:D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
# 69. Acceptance Criteria for the First Useful Version

The first genuinely useful system does **not** need ML.

It should already be able to:

```text
1. Connect to both MT5 brokers.
2. Discover all groups.
3. Synchronize ~5,000 accounts.
4. Capture XAUUSD trades correctly.
5. Reconstruct logical trades.
6. Detect the first 3 completed XAUUSD trades.
7. Produce a deterministic trader/risk score.
8. Rank traders.
9. Connect to cTrader QUOTE FIX securely.
10. Discover the Pepperstone XAUUSD instrument ID.
11. Shadow-copy selected traders using destination quotes.
12. Show all of this in React.
```

Only after this works should ML be judged.
```

Parse item 6 carefully: **detect** the first 3 completed XAUUSD trades. That is the **early-evidence trigger** (§1 change #2), not the profit contest. Item 7 is a **deterministic** trader/risk score. Item 11 is **shadow** on destination quotes — the first honest approximation of execution-venue-net. Live FIX send is **not** in this twelve.

**Only after this works should ML be judged.** That sentence is the first-useful-version restatement of **do not use ML first**.

### 4.3 §70 Live FIX execution — still not a first-3 contest

Before production live execution (quoted 2662–2676): TRADE logon stable; ExecutionReports persisted; position reports reconcile after restart; unique ClOrdID; duplicate reports; unknown-state recovery; partial fills; rejects; cancel/replace where required; destination position mapping; **risk-engine rejection before FIX send**; real execution feature-flagged; global stop-new-orders; **reconciliation blocks execution while inconsistent**.

Again: destination integrity + risk-before-send. No “who made most in first 3” gate.

---

## 5. What not to build yet (§71) — includes ML-first

Quoted in full:

```2681:2696:D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
# 71. What Not to Build Yet

Do not add initially:

```text
Kafka
Kubernetes
ClickHouse
LLM/AI API
deep learning
reinforcement learning
complex microservice mesh
cross-region active-active FIX execution
automated model self-promotion
```

These can be revisited only when measurements justify them.
```

### 5.1 ML-first is in the non-goal set

§71’s named ML/AI rows:

| §71 item | Why it is banned *initially* given the §3 goal |
|---|---|
| LLM/AI API | Header: AI/LLM dependency **None**. Does not estimate destination-net. Not in §69. |
| deep learning | §1: do not use ML first. No baseline, no out-of-sample beat, no official destination-net label yet. |
| reinforcement learning | Same, plus it would treat live/shadow as an environment before risk/recon are proven. |
| automated model self-promotion | Would skip “ML must beat the deterministic baseline out-of-sample” and skip manual review (§68). |

Together with §1 change #3 and §69 (“does **not** need ML” / “Only after this works should ML be judged”), the architecture’s **what-not-to-build includes ML-first**.

ML-first would optimize the **wrong** objective (likely first-3 or source realized) on the **wrong** label (no destination costs yet) and would sit **upstream of** a risk engine that is supposed to be deterministic.

When ML is eventually allowed, §1 still requires: **deterministic statistical baseline first**; ML must **beat that baseline out-of-sample**; §2: **no future-data leakage**; later §72.15 (adjacent, not in the assigned slice): **ML never bypasses risk**.

### 5.2 Infra non-goals (same §71 list)

| §71 item | Legal substitute already in §§1–4 / §69 |
|---|---|
| Kafka | Domain + outbox (§4). |
| Kubernetes | Not required for first useful version. |
| ClickHouse | PostgreSQL is the durable source of truth (§1 strength #3, §4). |
| complex microservice mesh | One API + workers + FIX adapter (§4). |
| cross-region active-active FIX execution | Single TRADE owner; independent QUOTE + TRADE sessions (§1 changes #7–#9). |

Absence of these items is the **correct** state until **measurements** (including destination-net and risk) justify a revisit.

---

## 6. Compact law for later slots

```text
OFFICIAL OBJECTIVE
  P(future destination-net PnL > 0 | behavior so far)  subject to  risk limits

NOT THE OBJECTIVE
  argmax(source cash on first 3 completed XAUUSD trades)

EVIDENCE
  first few completed XAUUSD trades → early score / SHADOW, not skill, not live

FIRST USEFUL VERSION (§69)
  deterministic score + rank + destination-quote shadow
  does not need ML

WHAT NOT TO BUILD YET (§71 + §1.3)
  Kafka, K8s, ClickHouse, LLM/AI API, deep learning, RL,
  mesh, cross-region active-active FIX, automated model self-promotion,
  and ML-first (baseline must exist; ML must beat it OOS)

LIVE COPY
  only after §68 (incl. destination costs/slippage + risk tests)
  and §70 (risk reject before FIX send)
```

---

## 7. Scope of this write

- Read-only on the v2 architecture file, **§§1–4 and §§68–71**.
- **No product files created or edited.**
- Adjacent sections (§5 stack, §72 senior rules, §73 audit/gap) were **not** used as implementation work; §72.15 / §72.16 / §72.20 are noted only as consistent with the assigned slices.

**DONE** for P500_S044: profit goal quoted; first-3 anti-goal quoted; ML-first recorded as a what-not-to-build via §1 + §69 + §71.
