# A80 — What not to build (§71)

| Field | Value |
|---|---|
| Agent | A80 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A80_not_to_build.md` |
| Source of truth | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§71 What Not to Build Yet** |
| Supporting sections | Header (AI/LLM dependency: None), §1.1–1.3, §5 (stack + “No LLM API is required”), §13 (outbox instead of Kafka), §19–21 (XGBoost later, not DNN first), §27–28 (single-active TRADE), §39 / §72.15 (ML never bypasses risk), §41 / §68–70 (live send gated), §54 / A54 (Docker-where-compatible, not K8s), §62, §66–67, **§69 first useful version**, §72.20 |
| Adjacent swarm notes (read, not rewritten) | A03, A08, A20, A25, A28, A29, A30, A41, A46, A51, A52, A53, A54 |
| Product source edited | **No.** This file is the only write. |

Classification vocabulary is architecture §73.B. Correct *absence* of a forbidden item is **`EXISTS_AND_GOOD`**.

---

## 0. Mandate (do not reinterpret)

Architecture **§71** is a hard non-goal list for the first useful version and until **measurements** justify a revisit. It is not a backlog. It is not a “nice to have later this sprint.”

Quoted in full:

```text
# 71. What Not to Build Yet

Do not add initially:

Kafka
Kubernetes
ClickHouse
LLM/AI API
deep learning
reinforcement learning
complex microservice mesh
cross-region active-active FIX execution
automated model self-promotion

These can be revisited only when measurements justify them.
```

This report’s job: **confirm each named item is out of scope**, name the legal substitute, prove the repo does not already contain it, and stop anyone from smuggling one in as “infrastructure for Phase 3.”

§72.20 is the same rule in one line: *prefer simple systems until data proves more complexity is necessary.*

---

## 1. Verdict

**All nine §71 items are out of scope.** Confirmed.

| # | User shorthand | Architecture name | Scope | Current tree |
|---|----------------|-------------------|-------|--------------|
| 1 | Kafka | Kafka | **OUT** | correctly absent — `EXISTS_AND_GOOD` |
| 2 | K8s | Kubernetes | **OUT** | correctly absent — `EXISTS_AND_GOOD` |
| 3 | ClickHouse | ClickHouse | **OUT** | correctly absent — `EXISTS_AND_GOOD` |
| 4 | LLM | LLM / AI API | **OUT** | correctly absent — `EXISTS_AND_GOOD` |
| 5 | DNN | deep learning | **OUT** | correctly absent — `EXISTS_AND_GOOD` |
| 6 | RL | reinforcement learning | **OUT** | correctly absent — `EXISTS_AND_GOOD` |
| 7 | mesh | complex microservice mesh | **OUT** | correctly absent — `EXISTS_AND_GOOD` |
| 8 | active-active FIX | cross-region active-active FIX execution | **OUT** | correctly absent — `EXISTS_AND_GOOD` |
| 9 | self-promotion | automated model self-promotion | **OUT** | correctly absent — `EXISTS_AND_GOOD` |

**Parse the task title correctly.** “active-active FIX self-promotion” is **two** §71 rows, not a ban on FIX and not a ban on human SuperAdmin promotion after Phase 6:

- **FIX itself is in scope** (QUOTE in Phases 4–5 / §69 items 9–11; TRADE read in Phase 7; live `NewOrderSingle` only in Phase 8 after §68 + §70).
- **Cross-region active-active FIX execution is out of scope.** One TRADE owner per destination account (A46, §1.9, §28).
- **Automated model self-promotion is out of scope.** A later human SuperAdmin promote, after out-of-sample proof on the official label, is the only legal promote path (A51, A52). That path is **also** out of the first useful version.

Header of the architecture file: **AI/LLM dependency: None.** §5: **No LLM API is required.**

First useful version (§69) is twelve items. **None of them require any row in this table.**

Do **not** add these items to close a perceived gap. Their absence is the correct state (A29 L29).

---

## 2. How to use this document

- Treat this file as the **binding non-goal list** for Phases 0–5 and for any “drive-by infra.”
- Do not implement Kafka, a cluster, a feature store, an LLM client, a net, an RL loop, a mesh, a second live TRADE region, or an auto-promote job from this file.
- Items marked `[DO NOT]` stay `[DO NOT]` until a **new** measured design note names the metric, the threshold, the date, and the SuperAdmin decision. Intention is not a measurement.
- When a later phase opens (Phase 6 XGBoost, Phase 7 TRADE read, Phase 8 live send), **re-read this file**. Phase 6 does **not** unlock DNN / RL / LLM / self-promotion. Phase 8 does **not** unlock active-active FIX.

---

## 3. Item-by-item confirmation

### 3.1 Kafka — OUT

**Why §71 + §13 forbid it now.** At ~5,000 users, a broker on day one is unnecessary. Distributed infrastructure must not be introduced preemptively.

**Legal substitute.** PostgreSQL transactional outbox in the **same commit** as the raw persist (§12 live path: validate → deduplicate → persist raw → write outbox → commit). Background workers drain five event kinds only (§13, A41):

```text
trade-completed
score-update
shadow-copy-intent
risk-check-request
notification-event
```

If measured throughput later requires a dedicated broker, migrate **behind** `IEventBus`. The outbox (or an equivalent local table) still writes in the domain transaction. Publish from the poller, never from the MT5 callback.

**Also out (same reason).** MassTransit, NATS, RabbitMQ, Azure Service Bus, Redis Streams-as-SoT. A41: Redis-first trading outbox is **UNSAFE**.

**Forbidden packages.** `Confluent.Kafka`, `MassTransit.*`, `RabbitMQ.Client`, `NATS.Client`.

**Do not.** Use Kafka to “buffer catch-up,” to fan-out `NewOrderSingle`, or as a feature store. A durable outbox of **source facts** is not a send queue (A53).

### 3.2 Kubernetes — OUT

**Why.** §5 deployment law is Docker **where compatible**, Windows worker if the Manager DLL requires Windows, Linux for API / Postgres / Redis / Python / React. A54: the Manager API is measured Windows-only (`LoadLibraryW`, PE `AMD64`). A K8s “unify the OS” proposal does not load a PE on a Linux node and violates §71.

**Legal substitute.**

```text
Windows Server x64  →  apps/mt5-worker  (local Manager DLL)
Linux compose       →  API + Postgres 16 + Redis 7 + React + later Python
Linux process       →  apps/fix-worker  (managed QuickFIX/n; no Manager DLL)
```

A future `docker-compose.yml` for postgres + redis (+ later api/web) is **allowed by §5** and is currently **MISSING**. That gap is not a license to add Helm, operators, or a DaemonSet of `MT5APIManager64.dll`.

**Forbidden artifacts.** `Chart.yaml`, Helm values, `kustomization.yaml`, `kind: Deployment` / `StatefulSet` / `DaemonSet` for this product, `KubernetesClient` package.

**Do not.** Put the native DLL in a Linux image, Wine it, or schedule it as a cluster DaemonSet (A54: `UNSAFE` / `DEPRECATED`).

### 3.3 ClickHouse — OUT

**Why.** PostgreSQL is the durable source of truth (§5, §254–260). ~5,000 accounts do not justify a second analytical store.

**Legal substitute.** Postgres tables in A20 (`mt5_*`, reconstructed trades, scores, shadow book, outbox). Read replicas or materialized views later, **if** a measured dashboard/query SLO fails.

**Forbidden packages.** `ClickHouse.Client`, any CH HTTP ingest, a “feature store” schema in CH “for ML scale” (A52 §10).

**Do not.** Dual-write the ledger to CH. Dual-write creates a second SoT and a reconciliation problem we do not have staff or tests for.

### 3.4 LLM / AI API — OUT

**Why.** Architecture header: *AI/LLM dependency: None.* §5: *No LLM API is required.* Scoring is deterministic first (§18, §69 item 7), XGBoost later only if it beats that baseline (§19–21). An LLM is not a scorer, not a risk engine, not a FIX codec, and not a symbol mapper.

**Legal substitute.** Rules + statistics (`risk_score`, `behavior_score`, `early_quality_score`). Humans read the React dashboard.

**Forbidden packages.** `Microsoft.SemanticKernel`, `Azure.AI.OpenAI`, `OpenAI`, LangChain, any chat-completions client, any “agent trader.”

**Do not.** Call an LLM from the MT5 callback, from scoring, from risk, or from ClOrdID / symbol discovery. Do not stub a fake `mlProbability` so the UI looks complete (A26).

### 3.5 Deep learning (DNN) — OUT

**Why.** §19: use **XGBoost** initially. Do **not** use a deep neural network first. §71 repeats the ban. Phase 6, when it opens, is still gradient-boosted trees + calibration + top-N economics (A52).

**Legal substitute (later, Phase 6 only).** Python / FastAPI / XGBoost / scikit-learn / Polars / NumPy. Optional later: MLflow. **Not now.** Phase 6 is closed (A52). `D:\Prop\services` is empty. That emptiness is correct.

**Forbidden now and as the first model.** PyTorch, TensorFlow, Keras, ONNX “just in case,” a GPU training loop, an embedding tower.

**Do not.** Train a net on raw deals and call it Phase 6. Deals are not trades; source P&L is not copy P&L.

### 3.6 Reinforcement learning — OUT

**Why.** The product ranks traders for shadow copy. It does not train an agent to trade gold. There is no legal reward signal, no safe online loop, and no permission to send from a policy network.

**Legal substitute.** None in v1. Phase 3 baseline. Phase 6 XGBoost **ranking** only, output = `candidate / confidence / suggested allocation` (§39). Risk remains the send authority.

**Do not.** Bandits on live size, “RL for execution algos,” or any path that emits `NewOrderSingle` from a trained policy.

### 3.7 Complex microservice mesh — OUT

**Why.** §66 / A30: a small set of processes (API, mt5-worker, fix-worker, later one Python scorer). Not a mesh. Not Istio / Linkerd / Dapr / Consul Connect. Not a dozen repos talking over sidecars.

**Legal substitute.** Three .NET hosts + Postgres + Redis + React. Cross-process work goes through the **outbox** and HTTP contracts already named (A16 `/mt5/*`, A26 dashboard). One Windows collector, one Linux API.

**Do not.** Split scoring, reconstruction, risk, and copy into independently deployed “services” so they need a mesh to find each other.

### 3.8 Cross-region active-active FIX execution — OUT

**Why.** §1.9 / §28: do **not** run multiple simultaneous active TRADE sessions for the same FIX account. cTrader copies every report to every active connection (A05 / A25 / A46). Two TRADE sockets ⇒ two `NewOrderSingle` senders and a broken destination book.

**Legal substitute.** Single-active TRADE ownership: Redis lease **with fencing token**, PostgreSQL as authority (A46). QUOTE and TRADE are independent session objects. Owning QUOTE does not authorize TRADE sends. Fail closed if Redis or Postgres cannot prove ownership.

**Do not.**

- Active-active TRADE across regions, AZs, or laptops “for HA.”
- Treat replica-count = 1 as sufficient (a forgotten staging box with the live password bypasses it).
- Fail open to “DB lease only” in production (`TRADE_OWNERSHIP_ALLOW_DB_ONLY` defaults false).
- Implement Redlock. One Redis primary + fail-closed is enough.

**FIX that remains in scope (so this row is not over-read):**

| Phase | FIX work | §71 status |
|---|---|---|
| 4–5 / §69 | QUOTE TLS, Security List, Pepperstone XAU instrument ID, destination quotes, shadow P&L | **IN** |
| 7 | TRADE **read** / `OrderMassStatusRequest` / `RequestForPositions` / reconcile; `NewOrderSingle` still off | later, not §71 |
| 8 | Live send after §68 + §70 + `REAL_COPY_EXECUTION_ENABLED` | later, not §71 |
| never (until measured) | Cross-region active-active TRADE | **OUT** |

### 3.9 Automated model self-promotion — OUT

**Why.** A model that promotes itself is how a leaked or uncalibrated ranker reaches `LIVE`. §21: promote only if ML beats simple baselines **out of sample** on top-N **copy** economics, not accuracy. A52: never promote a model trained only on a source-P&L proxy.

**Legal substitute.** SuperAdmin, audited, after a locked chronological test and a written evaluation table (A20 `model_versions` / `model_evaluations`, A51). Human promote is **out of the first useful version**. Until then `mlProbability` is **null**.

**Do not.**

- Cron / outbox handler that flips `is_production` when AUC moves.
- Auto-promote from a proxy label (`proxy_source_future_pnl`).
- Create empty `model_*` tables now “to look ready” (A52). Phase 3 snapshots come first.
- Let a high ML score skip `RISK_BLOCKED` / `DISQUALIFIED`.

---

## 4. What to build instead (the positive list)

| Need | Build this | Not that |
|---|---|---|
| Async after ingest | Postgres `outbox_events` + poller + `IEventBus` implemented by the outbox (A41) | Kafka / NATS / Rabbit / mesh |
| Scale to ~5k accounts | Postgres 16 + indexes + workers | ClickHouse |
| Deploy | Windows mt5-worker + Linux compose (A54) | Kubernetes |
| Rank traders in v1 | Deterministic §18 scores (A22) | LLM, DNN, RL |
| Rank traders later | Phase 6 XGBoost **if** it beats the baseline (A52) | DNN / RL / LLM / auto-promote |
| FIX HA | Single owner + fence + fail closed (A46) | Active-active TRADE |
| Ship events | Five §13 kinds | Broker topics / stream processors |
| Feature / tick history | Postgres tick + snapshot tables when Phase 1–3 need them (A17, A20) | CH feature store |

A30 §0 / §15 already encode this. This file does not change that sequence.

---

## 5. Measured current state (honest)

Searched product source (`*.cs`, `*.csproj`, `*.cpp`, `*.h`, `*.ts`, `*.json`, `*.yml`, `*.yaml`, `*.xml`, `*.props`) under `D:\Prop` for Kafka, ClickHouse, Kubernetes, OpenAI, SemanticKernel, MassTransit, NATS, RabbitMQ, Istio, Linkerd, Dapr, LangChain, ChatGPT, self-promotion. **No product-source hits.** Mentions exist only in the architecture file and swarm reports (this one included).

| Check | Result | Class |
|---|---|---|
| `Confluent.Kafka` / MassTransit / NATS / RabbitMQ packages | absent from every csproj (A03, A30) | **EXISTS_AND_GOOD** |
| Helm / K8s manifests / `KubernetesClient` | absent; no `.github` workflows either | **EXISTS_AND_GOOD** |
| ClickHouse client / ingest | absent | **EXISTS_AND_GOOD** |
| LLM / OpenAI / Semantic Kernel packages | absent | **EXISTS_AND_GOOD** |
| `services/ml-service`, `*.py`, PyTorch / TF / RLlib | `D:\Prop\services` is **empty**; no training tree | **EXISTS_AND_GOOD** (Phase 6 closed) |
| Istio / Linkerd / Dapr / extra microservice hosts | three .NET hosts only; no mesh yaml | **EXISTS_AND_GOOD** |
| Second-region TRADE / active-active FIX | no TRADE send path; worker is a template loop (A08) | **EXISTS_AND_GOOD** |
| Auto-promote job / `is_production` flipper | no `model_versions` table, no promoter (A20, A52) | **EXISTS_AND_GOOD** |
| `IEventBus` + `outbox_events` migration | **MISSING** (A02 O9, A03, A41) | implement the **substitute**, not Kafka |
| `docker-compose.yml` (allowed by §5, not §71) | **MISSING** (A10, A29 T14, A54) | not a §71 violation |

Honest one-liner: **§71 is honored in the tree. The work is to build the simple substitutes (outbox, compose, baseline scorer, single-active QUOTE), not to fill these holes with the banned products.**

---

## 6. Forbidden packages and files (copy-paste gate)

Do **not** add:

```text
Confluent.Kafka
MassTransit
MassTransit.RabbitMq
RabbitMQ.Client
NATS.Client
Azure.Messaging.ServiceBus          (same job as Kafka here)
ClickHouse.Client
KubernetesClient
Microsoft.SemanticKernel
Azure.AI.OpenAI
OpenAI
LangChain / any chat SDK
TorchSharp / TensorFlow.NET / Microsoft.ML.OnnxRuntime  (as a first model)
```

Do **not** create:

```text
deploy/k8s/**
charts/**
kustomization.yaml
services/ml-service/**              (not until Phase 6 is actually open)
src/**/Kafka*.cs
src/**/ClickHouse*.cs
apps/** that exist only to sit behind a sidecar mesh
```

A30 already listed the same gate. If a PR adds one of these, reject it as a §71 fail even if tests are green.

---

## 7. Revisit criteria (the only unlock)

§71 last sentence: *These can be revisited only when measurements justify them.*

A revisit requires **all** of the following, on disk, not in chat:

1. The first useful version (§69, twelve items) is signed off with evidence (A28).
2. A named metric is failing on production-like load (examples: outbox lag SLO, p95 dashboard query, single-region RPO after a measured incident).
3. The simple substitute was actually implemented and measured (outbox, Postgres indexes, compose, single-active lease).
4. A new design note states: item, metric, current value, threshold, proposed design, rollback, SuperAdmin owner.
5. For anything ML-shaped: Phase 6 exit (A52) plus **human** promote. Auto-promote stays forbidden unless a later architecture revision explicitly deletes that row.

Until then, “we will need it at 50k users” is not a measurement.

---

## 8. Adjacent law that keeps §71 closed

| Location | Binding statement |
|---|---|
| Header | AI/LLM dependency: **None.** Execution default: **disabled.** |
| §1 change #1 | Do not build everything at once. |
| §1 change #3 | Do not use ML first. |
| §5 | Postgres SoT. Redis is cache/coordination, not the book. **No LLM API.** Docker where compatible. **Not** K8s. |
| §12 / §72.6–7 | Callback is lightweight. Persist, then async. No ML/FIX from the callback. |
| §13 | Outbox instead of Kafka. No preemptive broker. |
| §19 | XGBoost initially. **Not** a DNN first. |
| §21 | ML justified only if it beats baselines OOS. |
| §28 / §1.9 | One active TRADE owner. Duplicate connections copy every report. |
| §39 / §72.15 | ML never bypasses risk. |
| §62 | ML unavailable: keep ingesting; do **not** promote new traders to live. |
| §67 Phase 6 | Optional vs §69. Still XGBoost, not LLM/DNN/RL. |
| §69 | First useful system **does not need ML** and does not need this list. |
| §72.20 | Prefer simple systems until data proves complexity. |

---

## 9. What is still in scope (do not over-ban)

§71 is not a ban on the product.

```text
[IN]  PostgreSQL 16 as SoT
[IN]  Redis for cache, live scores, FIX lease + fence
[IN]  PostgreSQL transactional outbox
[IN]  IEventBus as a thin seam (outbox now; broker only later, behind it)
[IN]  Docker compose for Linux data plane (when A54 writes it)
[IN]  Windows mt5-worker + native Manager DLL
[IN]  QuickFIX/n FIX 4.4 QUOTE (Phase 4)
[IN]  Shadow copy on destination quotes (Phase 5)
[IN]  Deterministic scoring (Phase 3)
[IN]  XGBoost later (Phase 6) — still no LLM/DNN/RL/auto-promote
[IN]  TRADE read (Phase 7) and flagged live send (Phase 8)
[IN]  Human, audited SuperAdmin promote — after Phase 6 proof, not in §69
```

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

---

## 10. Sign-off

```text
[x] §71 list quoted in full
[x] Kafka confirmed OUT; substitute = Postgres outbox (§13, A41)
[x] Kubernetes confirmed OUT; substitute = Windows worker + Linux compose (A54)
[x] ClickHouse confirmed OUT; substitute = PostgreSQL
[x] LLM confirmed OUT; header + §5
[x] DNN confirmed OUT; §19 XGBoost first, and not now
[x] RL confirmed OUT
[x] Mesh confirmed OUT
[x] Active-active FIX confirmed OUT; substitute = single-active + fence (A46)
[x] Automated model self-promotion confirmed OUT; human SuperAdmin only, not in §69
[x] Product source not modified
[x] Absence of all nine in product source measured this date
```

**Status:** §71 honored. Do not build these. Build the first useful version instead.
