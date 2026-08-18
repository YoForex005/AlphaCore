# W500_RESEARCH_39 — Does `RiskEngine` sit between `CopyIntent` and `ExecutionIntent`?

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_39.md` |
| Agent / slot | W500 research **39** |
| Date | 2026-08-18 |
| Topic | Check that **RiskEngine sits between CopyIntent and ExecutionIntent**. |
| Goal context | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader **must not send live orders yet** (no loss). |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Secrets printed | **None** (no Manager / FIX / proxy passwords). |
| Method | Full `read_file` of `RiskEngine.cs` (190/190), `CopyIntent.cs`, `ExecutionIntent.cs`, `RiskDecision*.cs`, `EfTradingStore.PersistDemoShadowAsync`, `ReconstructionScoringService`, `DependencyInjection`, `LiveIngestHostedService`, `DealIngestionService.SyncCatalogAsync`, `NativeMt5BrokerConnector` group/user walk, `CTraderFixSession`, `CTraderFixLogonHostedService`, `apps/fix-worker/Worker.cs`, `LiveRuntimeStatus`, `Program.cs` `/api/settings` + `/api/ops/resync`, `SettingsController`, `RiskEngineTests`, architecture §§4/32/39/41/75, A23, E005 R001–R015, LIVE census. `grep` of `Evaluate(`, `IRiskEngine`, `new ExecutionIntent`, `ExecutionIntents.Add`, `35=D`, `NewOrderSingle` under `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tests`. Contrast grep on `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src`. **No TLS opened. No Manager Connect this slot. No product edit.** |
| Binding law | Architecture **§4 / §32 / §39 / §41 / §75**; A23; E005 R001/R003/R008/R009; E002 / E034 / W500_RESEARCH_30 |
| Siblings | A006 / A007 / A009 / A23 / B02 / B13 / C03 / C33 / C59 / D13 / D35 / D47 / D98 / E002 / E005 / E007 / W500_RESEARCH_29 / W500_RESEARCH_30 / LIVE_MANAGER_FETCH_MEASURED |

**Honesty rule:** a Domain `Evaluate` method is **not** a seated gate. A `DbSet<ExecutionIntent>` is **not** a persist-before-send. `AllowFixSend` is a DTO bit, **not** a socket. Architecture diagrams are **law**, not proof the process implements them. Absence of `35=D` is **`SAFE_BY_ABSENCE`**, not “risk-gated live copy.” Do **not** print passwords. Do **not** tick §68 / §70 from this slot.

---

## 0. Verdict (binding)

**FAIL_UNWIRED — `RiskEngine` does *not* sit between `CopyIntent` and `ExecutionIntent` on any production path.**

Architecture §32 / §39 / §75 require:

```text
persist CopyIntent  →  RiskEngine.Evaluate  →  persist ApprovedExecutionIntent  →  FIX worker  →  35=D
```

Measured on current disk:

| Claim | Result | Class |
|---|---|---|
| Architecture seats RiskEngine between the two intents | **Yes (law only)** | §4 L211, §32 L1275–1283, §39 L1496–1517, §75 L2843–2847 |
| Domain `RiskEngine.Evaluate` exists | **Yes** | `D:\Prop\src\Domain\Risk\RiskEngine.cs` L76–172 (190 lines) |
| Application port `IRiskEngine` | **0** | `grep IRiskEngine` product `*.cs` = **0** |
| DI registers `RiskEngine` | **No** | `AddTraderIntelligence` registers reconstructor / scorer / ingest — **not** risk |
| Product caller of `Evaluate(` | **None** | only `tests/Unit/RiskEngineTests.cs` (5 facts) |
| `CopyIntent` writer | **One** — `EfTradingStore.PersistDemoShadowAsync` | Status **`SHADOW_ONLY`**; **skips** Evaluate |
| `ExecutionIntent` writer | **Zero** | `new ExecutionIntent` / `ExecutionIntents.Add` in product `*.cs` = **0** |
| `CopyIntent.RiskDecisionId` / `ExecutionIntentId` assigned | **Never** | properties exist; initializer omits them |
| `risk_decisions` row from Evaluate | **Never** | `RiskDecisions.Add` = **0** |
| Live `35=D` / NewOrderSingle | **Does not exist** | W500_RESEARCH_30 + this re-grep |
| `RealCopyEnabled` | **Forced false** | DI L41 + FIX host L68 |
| Risk to capital if process starts now | **None** | **`SAFE_BY_ABSENCE`** of send + no ExecutionIntent |

One-line:

```text
Law: CopyIntent → RiskEngine → ExecutionIntent.
Disk: CopyIntent SHADOW_ONLY (optional) → ShadowCopyEngine.SimulateEntry.
      Evaluate is unit-only. ExecutionIntent never written. 35=D absent.
      FAIL_UNWIRED as authority. NONE capital risk (SAFE_BY_ABSENCE).
```

Do **not** enable `REAL_COPY_EXECUTION_ENABLED`. Do **not** add a `35=D` sender in this task. Do **not** claim §39 “final authority” is implemented.

---

## 1. Law (what “sits between” means)

`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`

§4 diagram (L211):

```text
Shadow copy ──> CopyIntent ──> Risk Engine ─────────┘
```

§32 production flow (L1266–1298):

```text
Create CopyIntent
      ↓
Persist
      ↓
RiskEngine evaluates
      ↓
ApprovedExecutionIntent
      ↓
Persist
      ↓
FIX Execution Worker
      ↓
NewOrderSingle
```

“Never send a FIX order directly from an MT5 event callback.”

§39 (L1496–1517): “The risk engine is the **final authority**.” Scoring/ML may emit only `candidate` / `confidence` / `suggested allocation`. Engine decides `approve` / `reduce size` / `reject` / `pause trader` / `pause venue` / `global stop`. Hard limits listed (loss, DD, XAU gross/net, qty, open positions, spread, quote age, signal age, price move, slippage, margin, martingale, abnormal size, venue health).

§41 (L1566–1590): `REAL_COPY_EXECUTION_ENABLED=false` by default. NewOrderSingle requires the flag **and** runtime risk-engine healthy state.

§75 (L2843–2847):

```text
CopyIntent
      ↓
Risk Engine
      ↓
ExecutionIntent
      ↓
cTrader TRADE FIX → NewOrderSingle
```

A23 (`D:\Prop\reports\swarm\20260818\A23_risk_engine_spec.md`) restates the same seat. E005 R001 scores the orchestrator **MISSING**. This slot **re-measures** the seat on current product C#; it does not implement it.

**PASS condition for this slot’s seating claim:** a worker/use-case that (1) persists `CopyIntent`, (2) calls `RiskEngine.Evaluate` (or `IRiskEngine`), (3) persists `RiskDecision` / `RiskDecisionRecord`, (4) persists `ExecutionIntent` **only** on `APPROVE`/`REDUCE_SIZE` with `AllowFixSend` consulted before any FIX builder. **None of (2)–(4) exist.**

---

## 2. What exists (Domain vocabulary)

### 2.1 `RiskEngine` — pure function, 190 lines

`D:\Prop\src\Domain\Risk\RiskEngine.cs`

- `RiskLimits` (L4–22): hardcoded defaults (`MaxQuoteAge=3s`, `MaxSourceSignalAge=15s`, `MaxOpenPositions=20`, `MaxPositionQuantity=5`, …). **Not** bound from `appsettings.json` `RiskEngine:*`.
- `RiskEvaluationRequest` (L32–56): carries `CopyIntentId`, `Action`, qty, quote, `RealExecutionEnabled`, `Reconciled`, `KillSwitch`, exposure counters.
- `RiskDecision` **record** (L58–65): `Outcome`, `ApprovedQuantity`, `Reason`, `AllowFixSend`. This is **not** the EF entity.
- `Evaluate` (L76–172): kill-switch / recon / venue / missing-stale quote / spread / mid-move / signal age / loss / DD / open-pos / qty / XAU gross / XAU net / margin / martingale / abnormal size.
- Final approve conjunction (L147–150):

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

`AllowFixSend` is that bool. **Nobody on a send path reads it.**

Empty branch when flag is false (L90–93) is **not** control flow — it comments that shadow still evaluates but never allows FIX. The method still returns `Approve` + `AllowFixSend=false` for a passing open (test `Real_flag_false_never_allows_fix_send`).

`IsIncreasing` = `OpenExposure` / `IncreaseExposure`. `IsReducing` = `Reduce` / `Close`. OPEN is stricter. Reducing skips most quote/signal/exposure caps and returns `RISK_REDUCTION` with `AllowFixSend=allowSend`.

Reject helper always sets `ApprovedQuantity=0`, `AllowFixSend=false`. The `MAX_XAU_NET` branch returns `ReduceSize` **via `Reject`**, so approved qty is **0** (not a real reduce). That is a stub defect (B13/D13/E005 STUB_WRONG). **Not a live send bug**, because nothing calls it.

Prior swarm SHA (E005 / D13 / D35 / C33, unchanged claim): `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D`. This slot re-read all 190 lines; surface matches those reports. SHA not re-hashed here (no shell in this agent).

### 2.2 Intent entities (shape only)

`D:\Prop\src\Domain\Entities\CopyIntent.cs` — `Action`, `RequestedQuantity`, `ExpiresAt`, `IdempotencyKey`, nullable `RiskDecisionId` / `ExecutionIntentId`.

`D:\Prop\src\Domain\Entities\ExecutionIntent.cs` — `CopyIntentId`, `RiskDecisionId`, `ClOrdId`, `Status` default `"Pending"`, `SentAt`/`FilledAt`.

`TraderDbContext` maps `copy_intents` (unique `IdempotencyKey`), `risk_decisions` (`RiskDecisionRecord`), `execution_intents` (unique `ClOrdId`). Tables are **schema**, not a pipeline.

### 2.3 Name collision (do not confuse)

| Type | Role | Wired? |
|---|---|---|
| `Domain.Risk.RiskDecision` | Evaluate return DTO | tests only |
| `Domain.Entities.RiskDecision` | older EF-shaped class | **not** in `TraderDbContext` |
| `Domain.Entities.RiskDecisionRecord` | mapped `risk_decisions` | **no writer** |
| `apps/api/Controllers/SettingsController.RiskEngineSettings` | Redis/config DTO (`MaxDailyDrawdownPct` …) | controller **unmapped**; Redis **unregistered** |
| `appsettings.json` `"RiskEngine"` | JSON paint | **not** bound to `RiskLimits` |

`Program.cs` uses minimal APIs (`MapGet("/api/settings", …)`). **`MapControllers` is absent.** `SettingsController` cannot serve. Its PUT writes Redis keys `settings:risk:*` / `settings:flags:live_copy` that **Evaluate never reads**.

### 2.4 Adjacent helpers, also unseated

| Helper | Path | On copy path? |
|---|---|---|
| `CopyIntentExpiry.IsExpired` | `Domain/Execution/CopyIntentExpiry.cs` | **No** — only `ExecutionAndSizingTests` |
| `ShadowCopyEngine.SimulateEntry` | `Domain/Shadow/ShadowCopyEngine.cs` | **Yes** — after CopyIntent persist; **bypasses** risk |
| `QuantityNormalizer` | Domain Execution | unused by RiskEngine / Shadow (test says so) |
| `ExecutionOrderStateMachine.MayRetryNewOrderSingle` | Domain Execution | status math; **no socket** |
| `KillSwitch` entity | seeded `Mode=None` | dashboard paint; Evaluate takes a **request field**, not this row |

---

## 3. Measured wiring (the seat is empty)

### 3.1 `Evaluate(` callers

`grep` `\.Evaluate\(` on `D:\Prop` `*.cs`:

| File | Hits |
|---|---:|
| `src/Domain/Risk/RiskEngine.cs` | definition only |
| `tests/Unit/RiskEngineTests.cs` | **5** unit facts |
| `src/**` Application / Infrastructure / Fix / apps | **0** |

`grep` `IRiskEngine` / `AddSingleton<RiskEngine>` / `AddScoped<RiskEngine>` / `new RiskEngine` in product `*.cs`: **0** (`new RiskEngine` exists only as `private readonly RiskEngine _e = new();` in the test class).

### 3.2 DI — risk not in the graph

`D:\Prop\src\Infrastructure\DependencyInjection.cs` L38–57:

```38:57:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
        services.AddSingleton(runtime);
        // ...
        services.AddSingleton<TradeReconstructor>();
        services.AddSingleton<BaselineScorer>();
        services.AddScoped<DealIngestionService>();
        services.AddScoped<ReconstructionScoringService>();
        services.AddHostedService<LiveIngestHostedService>();
        services.AddHostedService<CTraderFixLogonHostedService>();
```

Registered: store, dashboard, reconstructor, scorer, ingest, live ingest host, FIX **logon** host. **Not** `RiskEngine`, **not** an execution-intent service, **not** a FIX send worker.

### 3.3 The only `CopyIntent` writer skips risk

`ReconstructionScoringService.RebuildTraderAsync` (`DealIngestionService.cs` L119–145): reconstruct → persist trades → score → `UpsertScoreAsync` → **`PersistDemoShadowAsync`**. No risk call.

`EfTradingStore.PersistDemoShadowAsync` L251–337:

1. Always writes `OutboxEventType.ScoreUpdate`.
2. If `state != TraderState.SHADOW` → **return** (no CopyIntent).
3. If no `destination_quotes` row → **return**.
4. Else, per completed XAU trade, idempotency key `shadow:{brokerId}:{login}:{positionId}`:
   - `new CopyIntent` with `Action = OpenExposure`, `Status = "SHADOW_ONLY"`, `ExpiresAt = trade.OpenedAt.AddSeconds(15)`.
   - **Does not set** `Direction`, `SourceTradeId`, `SourcePositionId`, `RiskDecisionId`, `ExecutionIntentId`.
   - `new ShadowCopyEngine().SimulateEntry(...)` → `ShadowOrder`.
5. **No** `RiskEngine`. **No** `CopyIntentExpiry.IsExpired`. **No** `ExecutionIntent`. **No** `35=D`.

That is **score → optional shadow fill**, not **CopyIntent → RiskEngine → ExecutionIntent**.

### 3.4 `ExecutionIntent` is never constructed

`grep` `new ExecutionIntent` / `ExecutionIntents.Add` under `D:\Prop` product `*.cs`: **0** (only the `RiskEngine` return `new RiskDecision { ... }` matches a `new Risk*` pattern; that is the in-memory DTO).

`CopyIntent.ExecutionIntentId` is never assigned (only the property declaration).

`apps/fix-worker/Worker.cs` does **not** query `ExecutionIntents`. It stamps QUOTE/TRADE `FixSessionState` to `Disconnected` every 15s and, if `CTrader:RealCopyExecutionEnabled` is true, **logs a warning and still has no send function**. `apps/fix-worker/appsettings.json` does not even set the flag (GetValue fallback **false**).

### 3.5 Tests do not prove the seat

`RiskEngineTests` (5 facts): stale quote rejects open; flag false ⇒ `AllowFixSend=false`; STOP_NEW blocks open not close; unreconciled blocks new; stale signal rejects. All construct `RiskEvaluationRequest` in-memory. **No** persist CopyIntent → Evaluate → persist ExecutionIntent integration. E005 / C03 / D35 already called this smoke, not a limits suite. Unchanged.

---

## 4. Goal coupling: ALL groups / ALL manager traders

The seating gap does **not** block the **read** census. Catalog is Manager-wide:

`DealIngestionService.SyncCatalogAsync` L38–51: `GetGroupsAsync` then `GetAccountsAsync(null, …)` — **null group = every group name**.

`NativeMt5BrokerConnector.GetGroupsCore` L144–185: `GroupRequestArray("*")`, fallback `GroupTotal` / `GroupNext`. `GetAccountsCore` L189–212: if group is null/empty, walk **all** groups. `ReadAccountsForGroup` L216–262: `UserRequestArray` → `UserGetByGroup` → `UserLogins` + `UserRequestByLogins`; accounts via `UserAccountRequestArray` / `UserAccountGetByGroup`.

`LiveIngestHostedService` runs that catalog for **every** registered connector (Achiever + Starwave from `LiveMt5Registration.CreateConnectors`). `/api/ops/resync` walks the same two codes.

`LiveMt5Registration` **requires both** `MT5_PASSWORD` and `MT5_STARWAVEFX_PASSWORD` (non-empty, not `<SECRET>` / `(a/c`). Dummy `FakeMt5BrokerConnector` is **not** registered on the live DI path. Starwave `ProxyEnabled` is **hardcoded false** (direct). Achiever proxy is env-gated.

Prior **measured** live census (`D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`) — this slot did not re-connect:

| Broker | Connect | Groups | Traders |
|---|---|---:|---:|
| ACHIEVER | OK via HTTP proxy | 8 | 6512 |
| STARWAVEFX | OK direct | 10 | 1948 |
| **Total** | | **18** | **8460** |

Those are **all groups / all logins those two manager accounts can see**. Fetch is **read-only** Manager API. It does **not** go through RiskEngine and **must not** become a FIX send.

---

## 5. Copy to cTrader — no live orders (no loss)

Reconfirmed this slot (aligns with W500_RESEARCH_30 / E002 / E034):

| Control | Evidence |
|---|---|
| Only outbound FIX MsgType | `CTraderFixSession.BuildLogon` `(35, "A")` — Logon |
| `35=D` / `(35, "D")` in product `*.cs` (`src`+`apps`) | **0** |
| `ssl.WriteAsync` | **one** Logon write; sockets `using`-disposed |
| `CTraderFixLogonHostedService` after probes | `_runtime.RealCopyEnabled = false`; log “NewOrderSingle still disabled” |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **false** |
| `LiveRuntimeStatus.copyNote` when false | `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."` |
| `/api/settings` feature flag | `REAL_COPY_EXECUTION_ENABLED` = `runtime.RealCopyEnabled` (forced false); `FEATURE_COPY_TRADING_ENABLED` = **false** |
| QuickFIX/n | **not** referenced in `Fix.CTrader` |
| MT5 `SendTrade` / dealer send from this product | **not** on the copy path |

YoPips C++ (`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src`): **0** hits for `RiskEngine`, `CopyIntent`, `ExecutionIntent`, `35=D`, `NewOrderSingle`, `cTrader`. Its `risk_decisions` table is **challenge / prop-firm compliance** (`admin_approval_final_review_service.cpp`), not the XAUUSD copy seat. Its `DealerSend` is MT5 dealer for the prop platform — **not** a cTrader copy destination and **not** called from Prop `CTraderFixSession`.

**Flipping any flag cannot place a cTrader order today.** There is no builder, no ExecutionIntent drain, no TRADE keep-alive.

Safety class: **`SAFE_BY_ABSENCE`**. That is **not** a unit-tested “RiskEngine rejected before send” (A101 item 11 / §70.11 still FAIL). Vacuous safety is the desired no-loss outcome for this goal.

---

## 6. What a future seat must look like (do not implement here)

Required before live copy (A23 / §32 / §70):

1. Persist `CopyIntent` from a **fresh source lifecycle event** (not demo backfill of completed XAU).
2. Consult `CopyIntentExpiry` / `SIGNAL_STALE` on OPEN/INCREASE.
3. Call `RiskEngine.Evaluate` with **live** quote, kill-switch **row**, recon **run**, flag, exposure book.
4. Persist `RiskDecisionRecord` (unique correlation).
5. Persist `ExecutionIntent` **only** on approve/reduce, unique `ClOrdId`, **before** socket write.
6. FIX worker drains intents **only if** `AllowFixSend && REAL_COPY && LoggedOn TRADE && reconciled && READY_FOR_EXECUTION`.
7. Scoring / dashboard / Redis PUT **cannot** override a reject.

Until that exists, claiming “RiskEngine sits between CopyIntent and ExecutionIntent” is **false**.

---

## 7. Residuals (honest, not this slot’s live-loss FAIL)

| Residual | Why it is not capital loss today |
|---|---|
| Evaluate unused | No send path to skip |
| `MAX_XAU_NET` ReduceSize qty=0 | Dead code |
| `RealExecutionEnabled==false` empty `if` | Flag already forced false; no sender |
| Settings Redis / `RiskEngine:*` JSON disconnected from `RiskLimits` | Nothing sends |
| `SettingsController` unmapped | Dead; live `/api/settings` is the minimal-API floor (flags false) |
| Entity `RiskDecision` vs `RiskDecisionRecord` | Schema paint |
| `35=A` Logon can still hit `*.c-trader.com` if FIX password is set | Session proof, not an order |
| Cert callback always-true | TLS identity, not qty/side |
| Live Manager Connect can fail on this LAN without Achiever proxy | Catalog problem, not a send problem |
| A100 / A101 historically 0 PASS | Do not greenwash go-live |

---

## 8. Do / Do not

**Do**

- Keep `RealCopyEnabled` / `RealCopyExecutionEnabled` **false**.
- Keep Manager fetch of **all** Achiever + Starwave groups/logins as a **read** path.
- Treat `RiskEngine` as a **unit-tested vocabulary stub**, not the §39 authority, until a worker seats it.
- Persist this report as the slot-39 measurement.

**Do not**

- Add `35=D` / flatten / `GuardedNewOrderSingle` in this task.
- Treat SHADOW `CopyIntent` rows as approved live intents.
- Wire `Evaluate` → FIX without persist-before-send + recon + flag + tests.
- Print Manager / FIX / proxy secrets.
- Claim “no-loss live copy is implemented.” Operating mode is **fetch + optional Logon/recon + SHADOW_ONLY**. Live copy **cannot lose money because it cannot send**.

---

## 9. Slot close

| Item | Value |
|---|---|
| Slot | **39** |
| Verdict | **FAIL_UNWIRED** |
| Does RiskEngine sit between CopyIntent and ExecutionIntent? | **No** (law yes; process no) |
| Live `35=D` exists? | **No** |
| Fetch-all groups/traders path present? | **Yes** (`GroupRequestArray("*")` + `GetAccountsAsync(null)`; measured 18 / 8460 on prior live run) |
| Risk to capital | **NONE** (`SAFE_BY_ABSENCE`) |
| Evidence | Full RiskEngine 190-line read; `Evaluate(` product callers = 0; DI omits RiskEngine; sole CopyIntent writer is SHADOW_ONLY + SimulateEntry; ExecutionIntent writers = 0; FIX builder is 35=A only; RealCopy forced false |
| Product edited | **No** |
