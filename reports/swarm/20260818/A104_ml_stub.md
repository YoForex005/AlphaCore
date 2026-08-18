# A104 — `services/ml-service`: FastAPI Health Stub Only (No Training; Phase 6 Later)

| Field | Value |
|---|---|
| Agent | A104 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A104_ml_stub.md` |
| Source of truth | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| Primary sections | §5 ML stack (Python / FastAPI / XGBoost later), §66 `/services/ml-service`, **§67 Phase 6**, §19–21, §18, §39 / §72.15, §62, §69, §71 |
| Binding siblings | **A52** (Phase 6 hold), A22 (deterministic baseline), A23 / A53 (`ML_NOT_IN_USE` vs `ML_UNAVAILABLE`), A26 / A63 (`mlProbability: null`), A28 (phase gates), A30 (do not create now), A54 (Linux later), A65 (not on default compose), A75 (do not invent `FEATURE_ML_SCORING_ENABLED`), A80 (no DNN/LLM/RL) |
| Product source edited | **No** |
| Scope | Recommendation only. Do not implement from this file today. |

---

## 0. Verdict

**If `services/ml-service` is created before Phase 6 opens, it may be a FastAPI health stub only.**

It must not train, load a booster, accept feature vectors, emit probabilities, write `model_*` rows, or fill `mlProbability`. Phase 6 (dataset → chronological split → XGBoost → calibration → top-N vs baseline) stays **later**, after Phase 5 exit **and** proven data quality (A28, A52, §67).

Empty `D:\Prop\services` is still the **correct default**. The stub is the **maximum** legal Python surface before Phase 6 — not a required §69 deliverable, not a scorer, and not a green health light for ML.

A52 forbids a FastAPI **scorer** and forbids training. This file does **not** reopen Phase 6. It carves one exception: a process that answers liveness and advertises `ML_NOT_IN_USE` / `PHASE_6_CLOSED` is **not** a scorer.

---

## 1. How to use this document

- Treat this as the **folder contract** for `/services/ml-service` until Phase 6 actually opens.
- Do **not** implement the stub from this file as part of Phases 0–5 / I0–I9 (A30). First useful version does not need it (§69).
- If an implementer later reserves the §66 path (compose slot, API health name, repo shape), they must follow §4–§7 here. Anything beyond that is a policy FAIL even if it compiles.
- UI / API `mlProbability` stays **null** (A26, A63). **Do not stub a number.**
- Reason code while Phase 6 is closed: **`ML_NOT_IN_USE`** (A53 §2.2). Do **not** report `ML_UNAVAILABLE` — that means “a promoted scorer should be up and is not.”
- Scoring that exists in v1 is the **C# deterministic baseline** (`IScoringService` / A22 `baseline.v1`), not this Python process.

---

## 2. Measured state (2026-08-18)

| Item | State |
|---|---|
| `D:\Prop\services` | Empty directory. No children. |
| `D:\Prop\services\ml-service` | **Does not exist.** |
| Product `*.py` / `pyproject.toml` / `requirements.txt` under `services/` | **None.** |
| XGBoost / scikit-learn / Polars / MLflow / FastAPI in the product tree | **None.** Names live in architecture + swarm reports only (A52). |
| `IScoringService` | **MISSING** (A02 S1). Phase 3 C# baseline, not this service. |
| `mlProbability` on dashboard contracts | Always **null** in v1 (A26, A63). |
| Default compose | Must **never** start `ml-service` (A65). |
| Phase 6 | **Closed.** No dataset, no split, no baseline to beat (A52). |

This emptiness is **EXISTS_AND_GOOD** relative to Phase 6 being closed (A80, A11). It is **not** a gap to close in the first useful version.

---

## 3. Why a health stub is the only legal early shape

Three legal states for the folder, in preference order:

| State | When | Classification |
|---|---|---|
| **A. Absent** (`services/` empty) | Phases 0–5, §69, A30 I0–I9 | **Preferred.** Matches A52 close, A11, A65 “Never: ml-service”. |
| **B. FastAPI health stub** (this file) | Optional later scaffold if someone must reserve §66 + a named probe | **Maximum** pre–Phase 6 surface. Process up; model **not in use**. |
| **C. Phase 6 scorer** | After Phase 5 exit **and** frozen leakage-checked extract **and** measured beat of `baseline.v1` | **Later.** Training + calibrated `/v1/score`. Still no send path. |

Illegal states (policy FAIL even if green):

| Illegal | Why |
|---|---|
| FastAPI + `/predict` returning `0.5` / random / last source PnL | Fake `mlProbability` (A26, A80). |
| `train.py` / notebook on deals or latest `TraderScore` | No as-of snapshots, wrong `y`, leakage (A52 §§4–8). |
| `xgboost` / `sklearn` / `torch` / `mlflow` in the stub lockfile | Training stack is Phase 6. Pulling it now invites a “quick fit.” |
| Compose profile on by default | A65 everyday plane is postgres + redis (+ optional apps). |
| API treating stub 200 as “ML ready” | Confuses operators; would greenwash Phase 6. |
| Empty `model_*` migrations “to look ready” | A52 §9.3: do not create those tables now. |
| LLM / DNN / RL in the stub | §71, A80. Phase 6 still does not unlock them. |

A52 line “Starting … a FastAPI scorer … would be a policy FAIL” still holds. A **health-only** app is not a scorer.

---

## 4. Stub contract (if created)

### 4.1 Process

- **Language / framework:** Python 3.12+, FastAPI, Uvicorn. Matches §5. No other ML libraries.
- **OS / image:** Linux (A54). Never on the Windows MT5 worker host as a required sidecar.
- **Listen:** loopback / compose network only. No public ingress. No browser calls.
- **Auth:** none on `/health` and `/health/ready`. There is nothing to authorize; there is no model.
- **Secrets:** none. The stub must not receive MT5 / FIX / Postgres passwords. It must not open a trading-DB session “just in case.”
- **Default off:** compose `profiles: [ml-stub]`. Everyday `docker compose up -d` must not start it (A65).

### 4.2 Endpoints — allowlist

| Method | Path | HTTP | Body (normative) |
|---|---|---|---|
| `GET` | `/health` | **200** | `{ "status": "ok" }` — process liveness only. No inventory, no versions of secrets, no model name. Same spirit as API `GET /health` (A63). |
| `GET` | `/health/live` | **200** | Alias of `/health` for orchestrators. |
| `GET` | `/health/ready` | **200** | Readiness of **this process**, plus explicit Phase-6-closed payload. **Not** “ready to score.” |

Normative `/health/ready` body:

```json
{
  "status": "not_in_use",
  "reason": "ML_NOT_IN_USE",
  "phase": 6,
  "phaseState": "PHASE_6_CLOSED",
  "scorer": "none",
  "modelVersion": null,
  "mlProbability": null
}
```

Rules:

- `status` is **`not_in_use`**, never `"ok"` in the “model ready” sense, never `"degraded"` as if a booster crashed.
- `reason` is **`ML_NOT_IN_USE`** (A53 §2.2). Do **not** use `ML_UNAVAILABLE` until a promoted Phase 6 model is the configured authority and the process/model is actually down.
- `modelVersion` and `mlProbability` are always **null** on the stub.
- HTTP **200** on `/health/ready` is allowed so compose `healthcheck` can keep the *container* healthy without implying ML is in production. Document that distinction in the Dockerfile comment.
- Optional: return **503** on `/health/ready` if an operator wants “ready” to mean “ready to score.” If so, the body **must still** carry `ML_NOT_IN_USE` / `PHASE_6_CLOSED`. Do not flip to 200 when someone drops a `.ubj` in `models/`.

### 4.3 Endpoints — denylist (must 404)

Do not register, even as `501` or “coming soon”:

```text
/predict
/score
/v1/score
/infer
/train
/fit
/evaluate
/models
/models/{id}
/promote
/calibrate
/dataset
/features
```

A 501 on `/predict` invites clients. **404** is the contract. No OpenAPI “score” schema. No example probability in Swagger.

### 4.4 Files that would constitute the stub (do not create from this report)

Proposed only. Creating them is a later, explicit increment — not I0–I9.

```text
services/ml-service/
  README.md                 # one screen: health stub; Phase 6 later; no training
  pyproject.toml            # fastapi, uvicorn[standard] ONLY
  Dockerfile                # python:3.12-slim; no GPU; no xgboost wheels
  app/
    __init__.py
    main.py                 # FastAPI app; two health routes; nothing else
```

`pyproject.toml` dependencies (**closed set**):

```text
fastapi
uvicorn[standard]
```

Do **not** add: `xgboost`, `scikit-learn`, `polars`, `numpy`, `pandas`, `mlflow`, `torch`, `tensorflow`, `onnxruntime`, `lightgbm`, `optuna`, `httpx` to Postgres, `asyncpg`, `redis`, LLM SDKs.

Do **not** add: `models/`, `data/`, `notebooks/`, `mlruns/`, `*.ubj`, `*.json` booster dumps, `train.py`, `fit.py`, `evaluate.py`.

`README.md` first paragraph must say: this process is **not** Phase 6; it does not train; `mlProbability` stays null.

### 4.5 Compose sketch (not for the default file)

If a later compose revision adds the slot, it is profile-gated and has **no** Postgres/Redis `depends_on`:

```yaml
  ml-service:
    profiles: [ml-stub]
    build:
      context: ./services/ml-service
      dockerfile: Dockerfile
    image: ti-ml-stub:local
    container_name: ti-ml-stub
    restart: unless-stopped
    environment:
      ML_PHASE6_OPEN: "false"
    ports:
      - "8090:8080"    # lab only; not public
    healthcheck:
      test: ["CMD-SHELL", "python -c \"import urllib.request; urllib.request.urlopen('http://127.0.0.1:8080/health')\""]
      interval: 15s
      timeout: 3s
      retries: 5
      start_period: 10s
    networks: [ti]
```

A65 comment remains law for the **default** file:

```text
# Never: mt5-worker, mt5-collector, fix-worker, ml-service, Kafka, K8s
```

Do not invent `FEATURE_ML_SCORING_ENABLED` as architecture (A75). If an env key is needed on the stub, use a **closed** name such as `ML_PHASE6_OPEN=false`. The stub must **refuse to start scoring routes** while that flag is false. Flipping the flag to true without Phase 6 exit evidence is a policy FAIL, not a feature.

---

## 5. How the rest of the system must treat the stub

### 5.1 ASP.NET API health aggregation

`GET /api/v1/health` (A26 §6.14, A63) today has `mt5`, `reconstruction`, `scoring`, `fix`. If an `ml` object is added:

| Probe result | `data.ml` |
|---|---|
| Stub not deployed (default) | `{ "status": "absent", "reason": "ML_NOT_IN_USE" }` — **correct** for §69 |
| Stub up, Phase 6 closed | `{ "status": "not_in_use", "reason": "ML_NOT_IN_USE", "phaseState": "PHASE_6_CLOSED" }` |
| Phase 6 open, model promoted, process down | `{ "status": "down", "reason": "ML_UNAVAILABLE" }` — **only then** |
| Phase 6 open, model promoted, process serving | `{ "status": "ok", "modelVersion": "<id>" }` — still does **not** set `mlProbability` on traders until promotion rules in A52 §9.5 hold |

The API must **never**:

- call the stub for a score,
- substitute `0` / `1` / `0.5` when the stub is absent,
- mark overview / leaderboard “ML ready” from a 200 on `/health`,
- block ingestion, reconstruction, baseline scoring, or hard-limit risk because the stub is absent (A23, A53 §4.2).

v1 `scoring` on `/api/v1/health` means the **deterministic baseline** (requests/failures/shadow candidates), not this Python process.

### 5.2 Dashboard

- `mlProbability` remains **null** on trader list, trader detail, and overview (A26, A63).
- Models page (if present) shows **“Phase 6 not open”** / no production model. Do not draw a sparkline of fake probabilities.
- System Health may show ML as **Not in use**, not as a red outage.

### 5.3 Risk / state machine

- Risk engine **does not call** the stub (A23, §72.15).
- `ML_NOT_IN_USE` does **not** freeze hard limits and does **not** demote `LIVE` (there is no live send in §69 anyway).
- `ML_NOT_IN_USE` **does** block any story that would promote on “the model will like this” (A53). Trade #3 + high **baseline** score → **SHADOW only** (A22 I5, §23).

### 5.4 Outbox

- `score-update requests` (A02 S2) go to **C# `IScoringService`** (Phase 3). They must **not** HTTP-call the stub.
- Phase 6 later may add a second consumer that calls `/v1/score` on a **promoted** model. That consumer does not exist now and must not be stubbed to no-op-success with a probability.

---

## 6. What “no training” means (binding)

The stub increment, if it ever lands, is **forbidden** from:

1. Fitting XGBoost, linear models, calibrators, or any estimator.
2. Reading `mt5_deals` / reconstructed trades / `trader_scores` to build a matrix.
3. Writing `model_versions`, `model_predictions`, `model_evaluations`.
4. Checking in a booster file, even “dummy.”
5. Nightly jobs, `cron`, or CI steps named `train` / `fit` / `evaluate`.
6. Using source-broker PnL or challenge pass/fail as `y` (A52 §4.2 — official label is execution-venue-net copy PnL of trades #4–#23 under a versioned DD cap).
7. Fabricating MFE/MAE (A17, §17).
8. Shuffled train/test splits (A52 §7).
9. Calling the experiment “Phase 6.”

A52 six blockers still all apply. Any one keeps real ML closed:

- Phase order (not at Phase 6).
- Official label `future_net_copy_pnl` does not exist (needs Phase 5 shadow).
- As-of feature snapshots do not exist (needs Phase 3).
- No chronological population with leakage controls.
- No `baseline.v1` to beat (§18, §21).
- Data quality unproven.

---

## 7. Phase 6 later — how the same folder grows

Do not start this section’s work until A28 Phase 6 exit inputs exist: Phase 5 exit, frozen extract, split hashes, baseline comparison plan.

Then, **and only then**, the same `services/ml-service` may add:

| Piece | Role |
|---|---|
| Offline extract (read-only from snapshots + shadow book) | Dataset; never in the MT5 callback (§12, §72.6) |
| Chronological 70 / 15 / 15 by trade-#3 close time | A52 §7 |
| XGBoost + validation-only calibration | §19, §67; **not** a DNN first (§71, A80) |
| Top-N copy economics vs `baseline.v1` | §21; accuracy/AUC alone cannot promote |
| `model_versions` / `model_evaluations` | A20; human SuperAdmin promote; **no** self-promotion (§71) |
| `POST /v1/score` | Input: as-of feature snapshot id. Output: **only** `candidate`, `confidence` (calibrated), `suggestedAllocation` (§39) |
| Ready probe | Flips from `ML_NOT_IN_USE` to loaded `modelVersion` **after** audited promote |

Still forbidden after Phase 6 opens (A52 §10, A80):

- LLM / DNN / RL / agent trader
- ML path that sends `NewOrderSingle` or bypasses risk
- Auto-promote
- Kafka / ClickHouse “for ML scale”
- Live send (that is Phase 8 + §68 / §70 / `REAL_COPY_EXECUTION_ENABLED`)

If ML does not beat the baseline out of sample, **baseline remains the production scorer** (A28 Phase 6 exit). The FastAPI process may keep serving health + `ML_NOT_IN_USE`.

Expected first Phase 6 files (A30 §17 — **do not create now**):

```text
services/ml-service/pyproject.toml          # then grows xgboost / sklearn / polars
src/Infrastructure/Persistence/Migrations/…_ModelRegistry.cs
```

---

## 8. Relation to other swarm law

| Artifact | This file’s stance |
|---|---|
| A52 | Unchanged hold. Stub ≠ scorer ≠ training. |
| A22 / A30 I5 | Production scores before Phase 6 are C# `baseline.v1`. |
| A23 / A53 | `ML_NOT_IN_USE` until a promoted model exists; `ML_UNAVAILABLE` only after that. |
| A26 / A63 | `mlProbability: null`. Health stub must not fill it. |
| A28 / §69 | First useful version = Phases 0–5 + React. **No ML.** Stub is not a gate. |
| A30 | Do not add `services/ml-service/**` in I0–I9. |
| A54 | Linux Python later. Not on the Windows Manager host. |
| A65 | Default compose never starts `ml-service`. |
| A75 | Do not treat `FEATURE_ML_SCORING_ENABLED` as architecture. |
| A80 | No LLM / DNN / RL; emptiness of `services/` is currently good. |
| A11 / A29 L07 | “MISSING” folder is a **proposed** extra, not a §69 defect. |

---

## 9. Honest status vs a false PASS

| Claim | True measured state |
|---|---|
| “We have an ML service” | **False.** Directory does not exist. |
| “A FastAPI health stub means Phase 6 started” | **False.** Stub is explicitly `PHASE_6_CLOSED`. |
| “200 on `/health` means the model is ready” | **False.** Liveness of an empty process. |
| “We should train a quick XGBoost so the folder is not empty” | **False as Phase 6.** A52. |
| “Stub `/predict` returning 0.5 unblocks the UI” | **False.** Forbidden fake `mlProbability`. |
| “Leaving `services/` empty is a gap” | **False.** Compliance with §1, §18, §67, §69. |
| First useful version needs this stub | **False.** |

Prefer a missing folder over a leaked PASS.

---

## 10. Recommendation (one paragraph)

**Do not build ML now.** Optionally, and only in a later increment that is **not** part of §69, reserve `services/ml-service` as a **FastAPI health stub**: `GET /health` → process up, `GET /health/ready` → `ML_NOT_IN_USE` / `PHASE_6_CLOSED`, dependencies = FastAPI + Uvicorn only, compose profile **off**, no training, no score routes, no model files, no trading-DB credentials. Phase 6 later — after shadow labels and as-of snapshots exist and `baseline.v1` can be beaten on locked top-N copy economics — may grow that same process into the §5 XGBoost scorer. Until then the production ranker is the deterministic baseline, `mlProbability` is null, and empty `D:\Prop\services` remains legal.

**Product source was not modified.**
