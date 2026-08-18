# W500_RESEARCH_168 — Confirm `REAL_COPY_EXECUTION_ENABLED` must stay **false**

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_168.md` |
| Agent / slot | W500 research **168** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (product `src/`, `apps/`, `tests/`, architecture, live-census reports) |
| Second tree | `D:\Projects\YoPips\Backend\C++ Backend PropFirm` (`src` token search) |
| Topic | Confirm `REAL_COPY_EXECUTION_ENABLED` **must stay false**. **No** `35=D` NewOrderSingle until **risk + recon** gates. |
| Goal context | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; **copy to cTrader must not send live orders yet (no loss).** |
| Product source modified | **No.** This report is the only product-adjacent write from this slot (plus INDEX / SWARM_LOG pointers). `.env` **not** edited. |
| Test source modified | **No.** |
| Secrets printed | **None.** `.env` quoted only as `REAL_COPY_EXECUTION_ENABLED=true` and `FEATURE_COPY_TRADING_ENABLED=true` (booleans). No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Method | Independent `read_file` of options, session, hosted logon, DI, API `Program.cs`, fix-worker, `LiveRuntimeStatus`, `CopyTradingService`, `CopyTradingHostedService`, `RiskEngine`, FSM, scorer, ingest, Native connector, seed, architecture §§41/42/68/70. Targeted `grep` of product `*.cs` for `35=D`, `NewOrderSingle`, `REAL_COPY`, `OrderMassStatusRequest`, `new ExecutionIntent`. YoPips C++ `src` for `35=D` / `NewOrderSingle` / `cTrader` / `c-trader` / `REAL_COPY` = **0**; `SendTrade` exists as **MT5 dealer** mutation only. Prior live census re-summed from JSON, **not re-attached**. Live `127.0.0.1:5000` not probed this slot. **No TLS opened. No Logon sent. No order sent.** |
| Binding law | Architecture **§41** (L1564–1590), **§42** recon-before-send (L1594–1613), **§68** (L2605–2628), **§70** (L2658–2676), Phase 7/8 (L2571–2601). A100/C14 = §68 **0/19**. A101/D43 = §70 **0/14**. Source-of-truth checkboxes still all `[ ]`. |
| Siblings (do not treat as this file) | W500_68 / W500_108 (same question; **stale** on DI / `.env` / hosted pin). W500_128 (same question; wiring still matches 128; this slot re-read current bytes). W500_57/117/137 (flag defaults). W500_70/90/110/130/150 (`CTraderFixSession` 35=D=0). W500_59/99/119/139 (hop). A003 / A009 / A015 / E038. P500 live book. |

**Honesty rule:** a compile-time `= false` is a default. A `GetValue(..., false)` fallback is a default. A dashboard `featureFlags` value is a **display floor**, not a unit-tested refuse-on-LoggedOn-TRADE gate. `AllowFixSend` is a DTO bit. A comment / log / `LastError` that names `NewOrderSingle` is **not** a builder. A live TLS **Logon `35=A`** is **not** a NewOrderSingle. Absence of `35=D` is **`SAFE_BY_ABSENCE`**, not a §68 / §70 PASS. Fetching all Manager groups/traders is **read-only** and does **not** license send. Wanting copy **and** no loss does not make live send legal. Do **not** print FIX passwords.

**Stale-sibling warning:** W500_68 and W500_108 claim DI L41 + hosted L68 force `RealCopyEnabled=false` and `.env` L73 is `false`. **Those pins are no longer true.** `CREDENTIALS_AND_COPY_STATUS.md` L30 (`false (forced)`) and E038 (`/api/settings` hardcoded false) are **stale vs current DI**. W500_128 already remasured this; slot 168 independently re-read the same current bytes and **agrees**.

---

## 0. Verdict (binding)

**CONFIRMED — `REAL_COPY_EXECUTION_ENABLED` must stay `false`. No `35=D` until risk + recon gates are measured PASS.**

One FAIL on Architecture §68 or §70 blocks enablement. Current on-disk scorecards are **0/19** and **0/14**. Source-of-truth checkboxes at L2610–2628 are still all `[ ]`. Flipping the flag (or leaving the operator `.env` at `true`) is **not** a go-live: there is still **no** NewOrderSingle assembler, **no** persist-before-send, **no** `ExecutionIntent` writer, and **no** TRADE recon that can return `Reconciled=true` from the venue.

| Claim | Measured result | Class |
|---|---|---|
| Must `REAL_COPY_EXECUTION_ENABLED` stay false? | **Yes** | **CONFIRMED_MUST_STAY_FALSE** |
| Architecture §41 default | `REAL_COPY_EXECUTION_ENABLED=false` (L1572) | law |
| C# POCO default | `CTraderFixOptions.RealCopyExecutionEnabled = false` (L35) | fail-closed default |
| Runtime send bit (DI) | `LiveRuntimeStatus.RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", OrdinalIgnoreCase)` (`DependencyInjection.cs` L41) | **env-bound (no longer forced false)** |
| Hosted FIX logon | Logs `RealCopyArmed={Armed}`; **does not** re-assign `_runtime.RealCopyEnabled` (`CTraderFixLogonHostedService.cs` L68–70) | pin **removed** |
| Local `.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` | **OPERATOR RESIDUAL / POLICY FAIL** |
| `.env` L106 | `FEATURE_COPY_TRADING_ENABLED=true` | shadow pipeline ON (API ignores env; hardcodes true) |
| `/api/settings` `featureFlags.REAL_COPY_EXECUTION_ENABLED` | `runtime.RealCopyEnabled` (follows env if process loads `.env`) | display, not a choke |
| `FEATURE_COPY_TRADING_ENABLED` | hardcoded **true** (`Program.cs` L77) | shadow pipeline ON |
| `appsettings.json` `FeatureFlags:LiveCopyEnabled` | **false** (different name; not the §41 token) | leftover |
| Literal `35=D` / `(35, "D")` / `MsgType="D"` in product `src/` + `apps/` `*.cs` | **0** | live hop **MISSING** builder |
| Off-hop residual | `CTraderFixDemoTestTrade.Build("D")` (L139/163/197) | **demo CLI only**; not called by API/workers/copy |
| `CTraderFixSession` outbound tag 35 | **`"A"` only** (`BuildLogon` L96); one `WriteAsync` (L49) | Logon, not order |
| QuickFIX/n / `GuardedNewOrderSingle` | **0** package refs in `TraderIntelligence.Fix.CTrader.csproj` | initiator **MISSING** |
| YoPips C++ `src` `35=D` / `NewOrderSingle` / `cTrader` / `c-trader` / `REAL_COPY` | **0** | not a second cTrader sender |
| YoPips `SendTrade` / `DealerSend` | exists | **MT5 source dealer**, not Pepperstone `35=D` |
| Architecture §68 go-live | **0 PASS / 19 FAIL** (A100 / C14; checkboxes still `[ ]` at L2610–2628) | **block send** |
| Architecture §70 live FIX | **0 PASS / 14 FAIL** (A101 / D43; items 11 + 14 are the named risk/recon send blocks) | **block send** |
| Risk gate on send path | `RiskEngine.Evaluate` **is** called from `CopyTradingService` L159. Persist **hardcodes** `AllowFixSend = false` (L192). `VenueReconciled` **const false** (L15) → Evaluate rejects increasing actions with `VENUE_NOT_RECONCILED`. Empty-if at RiskEngine L90–93 still exists. | **GATE_INCOMPLETE** (called, not a send license) |
| Recon gate on send path | `/api/reconciliation/status` is a **stub** (zeros + English “NewOrderSingle still off”). `OrderMassStatusRequest` / `RequestForPositions` / `35=AF` / `35=AN` in product `*.cs` = **0**. `MarkReconciled` = definition only (0 callers). | **GATE_INCOMPLETE** |
| `ExecutionIntent` product writers | **0** (`new ExecutionIntent` / `ExecutionIntents.Add` = 0). Only `CountAsync` of `SentAt != null` | hop **MISSING** |
| Live send branch | `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` (`CopyTradingService` L198). Last two are **const false**. Branch would only stamp `LIVE_SEND_BLOCKED_UNIMPLEMENTED` | unreachable |
| Scorer auto-LIVE | `CanPromoteToLive => false` (L211). `FromBaseline` set `{INSUFFICIENT_DATA, EARLY_SCORE, WATCH, SHADOW, RISK_BLOCKED}` — no `LIVE` | no auto-promote |
| Live `35=D` if process starts now | **Impossible** | **`SAFE_BY_ABSENCE`** |
| Fetch ALL Achiever+Starwave groups/traders blocked by this flag? | **No** — catalog is Manager read-only (`GroupRequestArray("*")` + `UserRequestArray`) | independent |
| Risk to capital from copy path | **NONE** (this process cannot assemble Pepperstone `35=D`) | no loss |

One-line:

```text
REAL_COPY_EXECUTION_ENABLED must stay false. .env L73 is illegally true; DI now binds it. §68=0/19, §70=0/14. Live hop 35=D=0 (only 35=A). Demo helper Build("D") is off-hop + demo-gated. Risk/recon not on the wire. Fetch-all 18/8460 is read-only. SAFE_BY_ABSENCE. CONFIRMED.
```

Do **not** set `REAL_COPY_EXECUTION_ENABLED=true`. The operator `.env` is already `true` — **set it back to `false`**. Do **not** add a `35=D` sender in this task. Do **not** treat Logon, catalog fetch, `AllowFixSend`, `FEATURE_COPY_TRADING_ENABLED`, or `LiveCopyEnabled` as a go-live waiver.

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
| G07 | cTrader reconciliation works after restart | No `35=AF` / `35=AN` encoder. API recon is a stub. Ownership `MarkReconciled()` is a boolean the caller can set without a venue book (0 product callers). |
| G11 | risk engine unit/integration tests pass | Five in-process facts. Engine is now **called** from copy, but persist overrides `AllowFixSend=false` and recon is a const. Not a live risk gate. |
| G05/G06 | quote/trade session stable | Hosted service can one-shot TLS Logon `35=A` then **disposes** sockets. Worker stamps `Disconnected`. Not a keep-alive TRADE session. |
| G10 | position sizing conversion verified | `QuantityNormalizer` dest-grid passthrough; product hop `lots×0.05` (W500_138). Not G10 PASS. |
| G19 | manual review completed | Not done. This file is research, not review. |

Prior live scorecards (A100, C14): **0 PASS / 19 FAIL**. This slot did **not** flip any checkbox. One FAIL blocks enablement.

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

A101 / D43: **0 / 14 FAIL**. Items **11** and **14** are the named risk/recon send blocks. Item **12** is the flag — default false is **policy**. Current product binds the env token; that is **not** a proven choke on a live TRADE socket, because there is still no send function.

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

Today **none** of those AND-clauses except “no builder” are true. The operator flag is already `true` without the rest. Therefore the flag **must stay / return to false**.

---

## 2. Measured product pins (current bytes — not W500_68/108)

| Surface | What was read | Value |
|---|---|---|
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` L32–35 | POCO initializer + XML “Default OFF” | **`false`** |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` L39–45 | `RealCopyEnabled` **copied from env** `"true"` (case-insensitive). `RiskEngine` registered singleton | **env-bound** |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` L68–70 | info log of `RealCopyArmed`; **no** assignment to `_runtime.RealCopyEnabled` | **does not re-pin** |
| `D:\Prop\apps\fix-worker\Worker.cs` L21 | `GetValue("CTrader:RealCopyExecutionEnabled", false)` | fallback **false** (different key) |
| `D:\Prop\apps\api\Program.cs` L55, L76 | health + settings expose `runtime.RealCopyEnabled` | follows DI |
| `D:\Prop\apps\api\Program.cs` L77 | `FEATURE_COPY_TRADING_ENABLED` | **true** (shadow ON) |
| `D:\Prop\apps\api\appsettings.json` L44–47 | `FeatureFlags:LiveCopyEnabled` | **false** (different name) |
| `D:\Prop\.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` | **true** (gitignored; value only) |
| `D:\Prop\.env` L106 | `FEATURE_COPY_TRADING_ENABLED=true` | true (API unused; hardcoded) |
| `D:\Prop\docs\architecture.md` L20 | safety default | **false** |
| `D:\Prop\docs\deployment.md` L82 | “until cTrader integration is verified” | **false** |
| `D:\Prop\README.md` L28 | “Real NewOrderSingle is **off**” | **false** |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` L30 | “false (forced)” | **STALE vs `.env` + DI** |

### 2.1 Binding honesty (do not greenwash the choke)

W500_68/108 said “DI + hosted pin false, so env `true` cannot arm send.” **That is stale.**

Current chain (re-read this slot):

1. `EnvFile.FindAndLoad()` (`apps/api/Program.cs` L10) walks up to `D:\Prop\.env` and `Environment.SetEnvironmentVariable` (`EnvFile.cs` L19–38).
2. `builder.Configuration.AddEnvironmentVariables()` (`Program.cs` L13).
3. `AddTraderIntelligence` sets `RealCopyEnabled` **true** if the token equals `"true"` (`DependencyInjection.cs` L41).
4. Hosted logon **leaves that bit armed** (`CTraderFixLogonHostedService.cs` L68–70 logs it; no assignment).
5. `/api/settings` and `/api/health` will report `realCopyEnabled=true` after a process start that loaded this `.env`.

`CTraderFixOptions.RealCopyExecutionEnabled` is still a POCO default **false** and is **still not bound** to the architecture env name (would need `CTrader__RealCopyExecutionEnabled`). Fix-worker reads **`CTrader:RealCopyExecutionEnabled`**, a **different** key; `apps/fix-worker/appsettings*.json` has **no** `CTrader` block, so the worker fallback `false` always wins unless someone injects that nested key.

Even if `RealCopyEnabled=true` at runtime:

1. Worker, if it saw `true`, **only logs** a warning and still has **no** send function (`Worker.cs` L45–46).
2. Copy pipeline persist **forces** `AllowFixSend = false` (`CopyTradingService.cs` L192).
3. `NewOrderSingleImplemented` and `VenueReconciled` are **const false** (L15–16).
4. There is still **no** `35=D` assembler.

So today’s capital safety is **`SAFE_BY_ABSENCE`**, plus const blockers, **not** a single named gate that a unit test proves refuses `35=D` on a LoggedOn TRADE socket. That is the correct *current* no-loss outcome. It is **not** a reason to leave the env token `true` “to try one lot.”

### 2.2 Settings cannot legally arm a sender that does not exist

Live API (`Program.cs`) is **minimal APIs only** — no `AddControllers()` / `MapControllers()`. `SettingsController` (`LiveCopyEnabled` PUT → Redis `settings:flags:live_copy`) is therefore **unbound** on the running host. Even if it were bound:

- it writes a **different name** (`LiveCopyEnabled`), not `REAL_COPY_EXECUTION_ENABLED`;
- Redis is not required (`/api/health` reports redis `healthy=false` in the code path);
- it cannot create a NewOrderSingle builder.

### 2.3 Operator residual (must flip back)

`.env` L73 = `true` while §41 / README / deployment / committed docs all say `false`. This slot **did not edit** `.env`. Next operator action: set `REAL_COPY_EXECUTION_ENABLED=false` before any API restart is treated as honest. Leaving it `true` is a landmine the moment a `35=D` builder lands.

A015 advised “Do **not** copy env onto `LiveRuntimeStatus.RealCopyEnabled`.” Current DI **does** copy it. That is a residual, not a license.

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

`grep` `35=D` / `(35, "D")` / `MsgType="D"` under `D:\Prop\src` + `D:\Prop\apps` `*.cs`: **0** (literal token).

**Residual (do not greenwash “product 35=D=0” as “no assembler exists anywhere”):** `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` has `Build("D", …)` at L139 (flatten), L163 (open qty `1`), L197 (close). It is **not** on the copy hop. Only caller is opt-in CLI `D:\Prop\tools\DemoFixTestTrade\Program.cs`. Fail-closed gate (L43–60): host must start `demo-`, SenderCompID must start `demo.`, host/sender must not contain `live`, account `1369850` refused. **0** calls from `apps/api`, workers, `CopyTradingService`, or `CTraderFixLogonHostedService`. Copy-to-cTrader still cannot emit `35=D`.

`OrderMassStatusRequest` / `RequestForPositions` / `35=AF` / `35=AN` / `(35, "F")` / `(35, "G")` in product `*.cs`: **0** on the live hop. Demo helper can emit `AN` / `x` / `D` only when the CLI is run against a demo host.

`new ExecutionIntent` / `ExecutionIntents.Add` in product `*.cs`: **0**.

`NewOrderSingle` hits in product `src/` + `apps/` are **name / log / LastError / const / blocker only**:

| File | Role | Wire? |
|---|---|---|
| `CTraderFixOptions.cs` L33 | XML comment | No |
| `CTraderFixLogonHostedService.cs` L69 | info log “still unimplemented” | No |
| `LiveRuntimeStatus.cs` L43–44 | snapshot copy | No |
| `BrokerCatalogSeed.cs` L105 | TRADE `LastError` “logon/recon only; NewOrderSingle off” | No |
| `DemoSeeder.cs` L101 | TRADE `LastError` | No |
| `apps/api/Program.cs` L69 | recon stub note | No |
| `apps/fix-worker/Worker.cs` L22, L41, L46 | start log + LastError + refuse warning | No |
| `CopyTradingService.cs` L16, L49, L59, L243 | const `false` + status + blocker | No |
| `CopyTradingHostedService.cs` L30 | “Live NewOrderSingle still blocked” | No |
| `ExecutionOrderStateMachine.cs` L35 | `MayRetryNewOrderSingle` status math | No socket |
| `CopyTradingModels.cs` L9 | DTO field | No |

`TraderIntelligence.Fix.CTrader.csproj`: Hosting + Configuration + Logging + EF only. **No** `QuickFIXn.Core` / `QuickFIXn.FIX44`. Product `*.csproj` grep `QuickFIX` = **0**.

### 3.3 YoPips C++ is not a second cTrader sender

`grep` of `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` for `35=D`, `NewOrderSingle`, `cTrader`, `c-trader`, `REAL_COPY`: **no matches**.

That tree has MT5 `SendTrade` / `DealerSend` (dealer mutation on the **source** Manager — `mt5_manager.cpp`, `mt5_pool.cpp`, `trade_execution_service.cpp`). It is **not** a Pepperstone/cServer FIX NewOrderSingle path. Do not treat YoPips `SendTrade` as copy-to-cTrader.

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

That is **unsafe if a future sender keys off `Outcome==Approve` and ignores `AllowFixSend`**.

### 4.1 What changed since W500_59 / W500_68 / W500_108

Those slots said `Evaluate` product callers = 0 and RiskEngine not in DI. **Stale.**

Current (re-read this slot):

- `AddTraderIntelligence` L45: `services.AddSingleton<RiskEngine>()`.
- `CopyTradingService` still constructs **its own** `private readonly RiskEngine _risk = new();` (L23) — the DI singleton is unused by the copy path.
- `GenerateShadowIntentsAsync` calls `_risk.Evaluate(...)` at L159 with `RealExecutionEnabled = _runtime.RealCopyEnabled` and `Reconciled = VenueReconciled` (**const false**).
- Persist L192: `AllowFixSend = false` **hardcoded**, ignoring `decision.AllowFixSend`.
- Live branch L198 is unreachable (`NewOrderSingleImplemented` + `VenueReconciled` const false).
- **0** `new ExecutionIntent` / `ExecutionIntents.Add` in product.

Calling Evaluate on SHADOW intents is **not** “risk rejection happens before FIX send” (§70.11). There is no FIX send to reject.

---

## 5. Recon gate is **not** ready (so flag stays false)

Architecture §42 / §70.3 / §70.14: after TRADE logon, **block new executions**, run OrderMassStatus + RequestForPositions, reconcile, only then consider send.

What exists:

| Piece | Status |
|---|---|
| `GET /api/reconciliation/status` (`Program.cs` L63–69) | Hardcoded `unknownPositions=0`, `mismatches=0`, `orphanFills=0`, note = “recon runs only after FIX TRADE logon; NewOrderSingle still off” |
| `ExecutionOrderStateMachine.RequiresReconciliation` | Status math only |
| `FixSessionOwnership.MarkReconciled()` | In-memory bool the **caller** sets; no venue book; **0 product callers** of `MarkReconciled` |
| `CopyTradingService.VenueReconciled` | **`const false`** (L15) |
| `CTraderFixSession` MsgTypes | `A` only. **Missing** `AF` / `AN` / `H` / `D` / `F` / `G` |
| Persist-before-send / `GuardedNewOrderSingle` | **MISSING** |
| `MayRetryNewOrderSingle` after send | **false** except `NotSent` / `Rejected` (unit math) — no send to retry |

A stub that reports zeros is **anti-evidence** for G07 / §70.14. Recon **cannot** run on the wire today. Therefore recon **cannot** PASS, and the flag **must stay false**.

Worker honesty (L36–46): stamps TRADE `Disconnected` + `LastError = "No live TRADE socket. NewOrderSingle remains off."` If config `real` is true it **logs a warning** and still does not send.

---

## 6. Fetch ALL Achiever + Starwave groups/traders is independent of send

Prior **measured** Manager census (`D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` + `LIVE_GROUPS_AND_TRADERS.json` utc `2026-08-18T08:42:16Z`; `CREDENTIALS_AND_COPY_STATUS.md`):

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| Achiever | HTTP proxy | 8 | 6512 | 1506 |
| StarwaveFX | direct | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Re-sum this slot from JSON group `accounts` (no logins dumped):

- Achiever: `2+179+4+5+4+6295+0+23 = 6512` (8 groups)
- Starwave: `11+4+170+1735+22+0+0+4+0+2 = 1948` (10 groups)
- Total **18 / 8460**

P500 later live API mid-scoring cited **8463** accounts (Starwave ~1951). That is a later in-process catalog, **not** a new Manager attach by this slot. This slot **did not live-reattach**.

Catalog walks (flag-blind):

| Path | What |
|---|---|
| `NativeMt5BrokerConnector.GetGroupsCore` L155 | `GroupRequestArray("*")`; cache `GroupTotal`/`GroupNext` only if empty |
| `ReadAccountsForGroup` L223 | `UserRequestArray` first; `UserGetByGroup` only on hard fail; empty → `UserLogins` + `UserRequestByLogins` |
| `DealIngestionService.SyncCatalogAsync` L45–48 | all groups + `GetAccountsAsync(null)` |
| `EfDashboardQueries.GetTradersAsync` | account-driven left-join scores (prior slots; not re-opened this pass) |

Those walks do **not** read `REAL_COPY_EXECUTION_ENABLED`. Fetch-all remains the **goal** and is **not** blocked by keeping the copy flag false.

Copy-to-cTrader **destination** stays: QUOTE/TRADE Logon `35=A` for session proof + future recon. **No** `35=D` until the conjunction in §1.4.

`TraderStateMachine.CanPromoteToLive => false`. `FromBaseline` cannot emit `LIVE`. Copy service may *look* at `SHADOW` / `LIVE_CANDIDATE` / `LIVE` scores; auto-LIVE is still impossible. P500 last book: dest PnL `$0`, do not send.

---

## 7. What would have to be true before the flag may flip

Do **not** treat this list as a waiver. Default remains OFF if any box is unchecked.

1. Operator `.env` (and any process env) is `REAL_COPY_EXECUTION_ENABLED=false` until review. Current `true` is a residual, not a license.
2. §68 **19/19 PASS** with on-disk evidence (not FakeMt5, not in-memory demo, not unused methods).
3. §70 **14/14 PASS**, including:
   - **11** risk rejection **before** FIX send (product `Evaluate` on the hop; spy `Submit=0` when flag false / unreconciled / stale quote; persist must not invent Approve→send);
   - **14** recon **blocks** execution while inconsistent (real `35=AF`/`AN` + persist + refuse).
4. Unique ClOrdID persist-before-send; `MayRetryNewOrderSingle` false on unknown; no blind catch-up when the flag later goes true (§63 / A53).
5. Quantity conversion G10 measured (not passthrough).
6. Named manual review. Committed `appsettings*` / docs stay `false` until that review.
7. A `35=D` builder that is **unreachable** unless the flag **and** `AllowFixSend` **and** `READY_FOR_EXECUTION`. Unit-test the refuse on LoggedOn TRADE.

Until then: **fetch + score + SHADOW only.**

```text
[DO NOT] Enable REAL_COPY_EXECUTION_ENABLED to “try one lot”
[DO NOT] Leave .env REAL_COPY_EXECUTION_ENABLED=true
[DO NOT] Send 35=D from an MT5 callback
[DO NOT] Retry 35=D after a broken send
[DO NOT] Flush a backlog when the flag later flips true
[DO NOT] Treat YoPips SendTrade as copy-to-cTrader
```

---

## 8. Files read / grepped (this slot)

| Path | Why |
|---|---|
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default false |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | only `35=A` writer |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | off-hop demo `Build("D")`; CLI-only |
| `D:\Prop\tools\DemoFixTestTrade\Program.cs` | only caller of demo helper |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | no re-pin; log only |
| `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` | no QuickFIX |
| `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs` | caller-set recon bool |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | env-bound `RealCopyEnabled` |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | Evaluate + const blockers |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | SHADOW tick only |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | snapshot copy |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `*` catalog |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Group/User request walks |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | loads `D:\Prop\.env` |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | unused send authority |
| `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs` | retry/recon math |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | `CanPromoteToLive => false` |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | TRADE LastError |
| `D:\Prop\apps\api\Program.cs` | settings + recon stub + FEATURE true |
| `D:\Prop\apps\api\appsettings.json` | `LiveCopyEnabled=false` |
| `D:\Prop\apps\fix-worker\Worker.cs` | refuse + Disconnected stamp |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | flag-false AllowFixSend |
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` | §§41, 42, 68, 70 |
| `D:\Prop\docs\architecture.md`, `docs\deployment.md`, `README.md` | committed defaults |
| `D:\Prop\.env` L73 / L106 | booleans only |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | 18/8460 |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | probe utc 08:42Z; re-summed |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | flag listed forced false (**stale vs `.env`**) |
| `D:\Prop\reports\swarm\20260818\A100_golive_gates.md`, `C14_golive_still_fail.md` | §68 0/19 |
| `D:\Prop\reports\swarm\20260818\D43_s70.md`, `A101_live_fix_acceptance.md` | §70 0/14 |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_68.md`, `W500_RESEARCH_108.md`, `W500_RESEARCH_128.md` | siblings; 68/108 pins stale |
| YoPips `...\C++ Backend PropFirm\src` | 0 cTrader FIX senders; MT5 `SendTrade` only |

SHA-256 **not recomputed** this slot (no shell). Line counts from full `read_file`: options 80, session 135, hosted 113, worker 51, API Program 160, DI 62, runtime 66, RiskEngine 190, CopyTradingService 257.

This slot did **not** live-attach Manager or FIX. Census numbers are last measured probe / P500 citation, not a new Connect.

---

## 9. Slot answers

1. **Must `REAL_COPY_EXECUTION_ENABLED` stay false?**  
   **Yes.** Architecture §41 default, §68 0/19, §70 0/14, recon stub, no persist-before-send, no `35=D` builder. Operator `.env` is illegally `true` and DI now honors it — flip it back.

2. **May we emit `35=D` NewOrderSingle now?**  
   **No.** Builder absent (`SAFE_BY_ABSENCE`). Even after a builder exists: not until risk + recon gates **and** the flag are jointly true.

3. **Does fetch-all conflict with no-loss?**  
   **No.** Manager catalog is read-only. Copy destination stays logon/recon-only. Constraint wins: **no live orders yet.**

4. **Risk to capital if this process starts now?**  
   **NONE** from cTrader copy. The process cannot assemble `35=D`. Residual: env `true` would arm `RealCopyEnabled` the moment a sender is added.

---

## 10. JSON (slot contract)

```json
{"slot":168,"verdict":"CONFIRMED_MUST_STAY_FALSE","evidence":"Must stay false: §41, §68 0/19, §70 0/14. Live hop 35=D=0; CTraderFixSession only 35=A. Copy consts NewOrderSingleImplemented=false + VenueReconciled=false; persist AllowFixSend=false. Residual: CTraderFixDemoTestTrade Build(D) demo-CLI only. .env L73 illegally true; DI L41 binds it; hosted no re-pin. Fetch-all 18/8460 read-only. YoPips src 0 cTrader senders.","risk_to_capital":"NONE"}
```
