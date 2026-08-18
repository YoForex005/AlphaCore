# W500_RESEARCH_188 — Confirm `REAL_COPY_EXECUTION_ENABLED` must stay **false**

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_188.md` |
| Agent / slot | W500 research **188** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (product `src/`, `apps/`, `tests/`, architecture, live-census reports) |
| Second tree | `D:\Projects\YoPips\Backend\C++ Backend PropFirm` (`src` token search) |
| Topic | Confirm `REAL_COPY_EXECUTION_ENABLED` **must stay false**. **No** `35=D` NewOrderSingle until **risk + recon** gates. |
| Goal context | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; **copy to cTrader must not send live orders yet (no loss).** |
| Product source modified | **No.** This report is the only product-adjacent write from this slot (plus INDEX / SWARM_LOG pointers). |
| Test source modified | **No.** |
| `.env` modified | **No.** Value quoted as boolean only. |
| Secrets printed | **None.** `.env` quoted only as `REAL_COPY_EXECUTION_ENABLED=true` (L73) and `FEATURE_COPY_TRADING_ENABLED=true` (L106). No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS opened. No Logon sent. No order sent. Runtime flag state inferred from source + `.env` bind. |
| Method | Independent `read_file` of options, `CTraderFixSession` (135/135), `CTraderFixDemoTestTrade` (demo-gated `Build("D")`), hosted logon, DI, API `Program.cs`, fix-worker, `LiveRuntimeStatus`, `CopyTradingService`, `CopyTradingHostedService`, `LiveIngestHostedService`, `RiskEngine`, FSM, ingest, dashboard traders, `TraderStateMachine`, LiveCopy page, architecture §§41/42/68/69/70, D41/D42/D43. Targeted `grep` of product `src/` + `apps/` `*.cs` for `35=D`, `NewOrderSingle`, `REAL_COPY`, `Evaluate(`, `OrderMassStatusRequest`, `new ExecutionIntent`. YoPips C++ `src` for `35=D` / `NewOrderSingle` / `cTrader` / `c-trader` / `REAL_COPY` / `FIX.4` = **0** (`SendTrade` is MT5 dealer, not cTrader FIX). Prior census cited, **not re-attached**. |
| Binding law | Architecture **§41** (L1564–1590), **§42** recon-before-send (L1594–1614), **§68** (L2605–2628), **§69** (L2633–2654), **§70** (L2658–2676), Phase 7/8 (L2571–2601). D41 = §69 **0/12** (FakeMt5 wording stale; still not a send license). A100/C14/D42 = §68 **0/19**. A101/D43 = §70 **0/14**. Architecture checkboxes re-read this slot: still all `[ ]`. |
| Sibling (same topic, not this file) | W500_68 / W500_108 (same question; **pins are stale**). W500_128 / W500_148 (same question; **“only outbound 35=A” is stale** — demo helper now exists). Also W500_57/117/137 (defaults), W500_70/90/110/130/150 (`CTraderFixSession` 35=D=0), W500_119 (`Evaluate` now on hop), A003 / A009 / A015 / E002 / P500 / P501. |

**Honesty rule:** a compile-time `= false` is a default. A `GetValue(..., false)` fallback is a default. A dashboard `featureFlags` bit is a **display**, not a unit-tested refuse-on-LoggedOn-TRADE gate. `AllowFixSend` is a DTO/DB bit. A comment / log / `LastError` that names `NewOrderSingle` is **not** a builder. A live TLS **Logon `35=A`** is **not** a NewOrderSingle. A **standalone demo tool** that can emit `Build("D")` is **not** the copy hop and is **not** a §70 PASS. Absence of a copy-path `35=D` is **`SAFE_BY_ABSENCE`**, not a §68 / §70 PASS. Fetching all Manager groups/traders is **read-only** and does **not** license send. Wanting copy **and** no loss does not make live send legal. Do **not** print FIX passwords.

**Stale-sibling warning (binding for this slot):** W500_68 / W500_108 claimed DI + hosted logon + `.env` were **pinned false**. That is **no longer true**. W500_148 claimed product outbound MsgType is **`35=A` only**. That is **half-stale**: `CTraderFixSession` is still `35=A` only; `CTraderFixDemoTestTrade` (tools caller only) now assembles `35=D`. The **policy** “must stay false” still holds. The **operator leftover** is that lab `.env` L73 is already `true` and DI now honors it.

---

## 0. Verdict (binding)

**CONFIRMED — `REAL_COPY_EXECUTION_ENABLED` must stay `false`. No copy-path `35=D` until risk + recon gates are measured PASS.**

One FAIL on Architecture §68 or §70 blocks enablement. Current on-disk scorecards are **0/19** (§68, D42/C14/A100; architecture L2610–2628 still all `[ ]`) and **0/14** (§70, D43/A101). §69 first useful version is **0/12** (D41; that file’s FakeMt5 wording is stale vs current fail-closed Native DI, but it is still **not** a send license). Flipping the flag (or leaving the lab leftover `true`) is **not** a go-live: the **copy hop** still has **no** NewOrderSingle assembler, **no** persist-before-send, **no** `ExecutionIntent` writer, **no** TRADE recon that can return venue `Reconciled=true`, and `CopyTradingService` hard-codes `NewOrderSingleImplemented=false` + `VenueReconciled=false` + persist `AllowFixSend=false`.

| Claim | Measured result | Class |
|---|---|---|
| Must `REAL_COPY_EXECUTION_ENABLED` stay false? | **Yes** | **CONFIRMED_MUST_STAY_FALSE** |
| Architecture §41 default | `REAL_COPY_EXECUTION_ENABLED=false` (L1572) | law |
| C# POCO default | `CTraderFixOptions.RealCopyExecutionEnabled = false` (L35) | fail-closed default; **unbound** (no `Configure<>`) |
| Runtime send bit | `LiveRuntimeStatus.RealCopyEnabled` = env `REAL_COPY_EXECUTION_ENABLED == "true"` (`DependencyInjection.cs` L41) | **bound, not pinned** |
| Hosted FIX logon re-pin | **Gone.** `CTraderFixLogonHostedService` L68–70 logs `RealCopyArmed={Armed}`; does **not** assign `false` | **STALE** vs W500_108 L68 |
| Local `.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` | **operator leftover — policy violation** |
| Local `.env` L106 | `FEATURE_COPY_TRADING_ENABLED=true` | shadow pipeline wish; API ignores this key |
| API loads `.env` | `Program.cs` L10 `EnvFile.FindAndLoad()` then L13 `AddEnvironmentVariables()` | process **will arm** the bit |
| `/api/settings` `featureFlags.REAL_COPY_EXECUTION_ENABLED` | `runtime.RealCopyEnabled` (follows env) | display, not a choke |
| `FEATURE_COPY_TRADING_ENABLED` | hardcoded **true** (`Program.cs` L77) | shadow pipeline on |
| `appsettings.json` `FeatureFlags:LiveCopyEnabled` | **false** (different name; not the §41 token) | leftover |
| Literal `35=D` in product `src/` + `apps/` `*.cs` | **0** | string token absent |
| `CTraderFixSession` outbound tag 35 | **`"A"` only** (`BuildLogon` L96); one `WriteAsync` (L49) | Logon, not order |
| `CTraderFixDemoTestTrade` `Build("D")` | **3 writes** (L138 flatten, L145 open, L179 close) | **demo tool only**; not copy hop |
| QuickFIX/n / `GuardedNewOrderSingle` | **0** package refs in `TraderIntelligence.Fix.CTrader.csproj` | initiator **MISSING** |
| YoPips C++ `src` `35=D` / `NewOrderSingle` / `cTrader` / `c-trader` / `REAL_COPY` / `FIX.4` | **0** | `SendTrade` is MT5 dealer, **not** a second cTrader sender |
| Architecture §68 go-live | **0 PASS / 19 FAIL** (A100 / C14 / D42; checkboxes still `[ ]` at L2610–2628) | **block send** |
| Architecture §70 live FIX | **0 PASS / 14 FAIL** (A101 / D43; items 11 + 14 are the named risk/recon send blocks) | **block send** |
| Architecture §69 first useful | **0 / 12** (D41) | not a send license |
| Risk on copy hop | `CopyTradingService.GenerateShadowIntentsAsync` L159 calls `Evaluate`; persist L192 **`AllowFixSend = false`** | hop exists; **send authority forced off** |
| Recon gate on send path | `VenueReconciled = false` const (CopyTradingService L15). `/api/reconciliation/status` is a **stub**. Product `CTraderFixSession` has no `AF`/`AN`. | **GATE_INCOMPLETE** |
| `ExecutionIntent` product writers | **0** (`new ExecutionIntent` / `ExecutionIntents.Add` = 0) | hop **MISSING** |
| Scorer auto-LIVE | `CanPromoteToLive => false` (BaselineScorer.cs L211). `FromBaseline` set has no `LIVE` | no auto-promote |
| Live copy `35=D` if API/workers start now | **Impossible** | **`SAFE_BY_ABSENCE`** (flag may be armed) |
| Fetch ALL Achiever+Starwave groups/traders blocked by this flag? | **No** — catalog is Manager read-only (`GroupRequestArray("*")` + `UserRequestArray`) | independent |
| Risk to capital from **copy path** | **NONE** (this process cannot open a Pepperstone/cTrader position) | no loss |

One-line:

```text
REAL_COPY_EXECUTION_ENABLED must stay false. Lab .env L73 is already true and DI binds it — copy hop still has no 35=D builder. Demo helper Build("D") is tools-only + demo-gated. §68=0/19, §70=0/14. Risk/recon not venue-true. Fetch-all 18/8460 is read-only. SAFE_BY_ABSENCE. CONFIRMED.
```

Do **not** set `REAL_COPY_EXECUTION_ENABLED=true`. Do **not** treat the leftover `true` as a go-live waiver. Do **not** add a copy-path `35=D` sender in this task. Do **not** wire `CTraderFixDemoTestTrade` into the hosted copy hop. Do **not** treat Logon, catalog fetch, `AllowFixSend`, `FEATURE_COPY_TRADING_ENABLED`, `LiveCopyEnabled`, or a demo-host test trade as a send license. Operator should flip lab `.env` L73 back to `false` (this slot did **not** edit it).

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

Architecture Phase 7 (L2571–2584) lists SSL TRADE + mass status + positions + ER/PR parsers + reconciliation, then: **“Still keep real NewOrderSingle disabled.”** Phase 8 (L2586–2601) is the only place NewOrderSingle is a deliverable, and only with an **explicit production flag**.

### 1.3 §68 — do not enable real copying until **all 19** are true

Source-of-truth checkboxes (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L2609–2628) are still all `[ ]`, including the two this slot names:

| Gate | §68 text | Why still FAIL (this re-read) |
|---|---|---|
| G07 | cTrader reconciliation works after restart | Product `CTraderFixSession` has no `35=AF` / `35=AN` encoder. API recon is a stub. `CopyTradingService.VenueReconciled` is a **const false**. Ownership `MarkReconciled()` is a boolean the caller can set without a venue book. Demo helper `35=AN` is **not** product recon. |
| G11 | risk engine unit/integration tests pass | Five in-process facts + one product `Evaluate` caller that **forces** `AllowFixSend=false`. Not a live risk-before-send proof. `IRiskEngine` **MISSING**. |
| G05/G06 | quote/trade session stable | Hosted service can one-shot TLS Logon `35=A` then **disposes** sockets. Worker stamps `Disconnected`. Not a keep-alive TRADE session. |
| G10 | position sizing conversion verified | `QuantityNormalizer` now called on shadow qty (`lots × 0.05`); still no copy-path tag 38 encoder. |
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

A101 / D43: **0 / 14 FAIL**. Items **11** and **14** are the named risk/recon send blocks. Item **12** is the flag — default false is **policy**. Lab leftover `true` does **not** make item 12 PASS: there is still no refuse-on-LoggedOn-TRADE unit test against a **copy-path** sender.

**Conjunction for the first legal copy-path `35=D` (A009 / A25 §6.3, restated):**

```text
19/19 §68
AND 14/14 §70
AND REAL_COPY_EXECUTION_ENABLED=true   (explicit, reviewed)
AND RiskEngine.Evaluate on the send path with AllowFixSend
AND TRADE READY_FOR_EXECUTION (Logon + recon clean)
AND persist unique ClOrdID before send
AND no blind catch-up / no retry of EXECUTION_STATE_UNKNOWN
```

Today **none** of those AND-clauses except “no copy-path builder” are true. The flag is **already armed in lab env**. That is a residual, not a waiver. Therefore the flag **must stay / return to false**.

---

## 2. Measured product pins (honest: env can arm the bit)

| Surface | What was read | Value |
|---|---|---|
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` L32–35 | POCO initializer + XML “Default OFF” | **`false`** (unbound; no `services.Configure<CTraderFixOptions>`) |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` L39–43 | `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` | **env-bound** |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` L68–70 | logs `RealCopyArmed={Armed}`; **no** `_runtime.RealCopyEnabled = false` | **does not re-pin** |
| `D:\Prop\apps\api\Program.cs` L10 + L13 | `EnvFile.FindAndLoad()` + `AddEnvironmentVariables()` | `.env` enters config |
| `D:\Prop\apps\fix-worker\Program.cs` | **no** `EnvFile.FindAndLoad` | worker does **not** load `.env` |
| `D:\Prop\apps\fix-worker\Worker.cs` L21 | `GetValue("CTrader:RealCopyExecutionEnabled", false)` | **different key**; `appsettings.json` has no `CTrader` block → fallback **false** |
| `D:\Prop\apps\api\Program.cs` L55, L76 | health + settings expose `runtime.RealCopyEnabled` | follows env |
| `D:\Prop\apps\api\Program.cs` L77 | `FEATURE_COPY_TRADING_ENABLED` | **true** (shadow pipeline) |
| `D:\Prop\apps\api\appsettings.json` L44–47 | `FeatureFlags:LiveCopyEnabled` | **false** (different name) |
| `D:\Prop\.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` | **true** (gitignored; value only) |
| `D:\Prop\.env` L106 | `FEATURE_COPY_TRADING_ENABLED=true` | unused by API (literal true) |
| `D:\Prop\docs\architecture.md` L20 | safety default | **false** |
| `D:\Prop\docs\deployment.md` L82 | “until cTrader integration is verified” | **false** |
| `D:\Prop\docs\ctrader-fix.md` L73 | disables live execution | **false** |
| `D:\Prop\README.md` L28 | “Real NewOrderSingle is **off**” | **false** (docs lag env) |
| `D:\Prop\docker-compose.yml` | `REAL_COPY` / `35=D` | **0 hits** |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` L13 | UI shows `status.realCopyArmed` | display |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` L30 | “**false** (forced)” | **STALE** vs DI L41 |

### 2.1 Binding honesty (do not greenwash the choke)

Architecture env name `REAL_COPY_EXECUTION_ENABLED` is **not** bound onto `CTraderFixOptions` (would need `CTrader__RealCopyExecutionEnabled`). `CTraderQuoteService` takes an unbound `CTraderFixOptions` and never opens TLS. Fix-worker reads **`CTrader:RealCopyExecutionEnabled`**, a **different** key; `apps/fix-worker` `*.json` has **no** `CTrader` / `RealCopy` block, so the worker fallback `false` always wins unless someone injects that nested key. Worker **does not** load `D:\Prop\.env`.

API **does** load `.env` and DI **does** copy the token onto `LiveRuntimeStatus.RealCopyEnabled`.

Even with the env token `true`:

1. `CopyTradingService.NewOrderSingleImplemented` is a **const false**.
2. `CopyTradingService.VenueReconciled` is a **const false**.
3. Persist path **hard-codes** `AllowFixSend = false` (L192), ignoring `decision.AllowFixSend`.
4. The only “live” branch (L198–200) requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` and then only stamps `LIVE_SEND_BLOCKED_UNIMPLEMENTED`. It still does not write FIX.
5. There is still **no** copy-path `35=D` assembler.
6. Worker, if it saw `CTrader:RealCopyExecutionEnabled=true`, **only logs** a warning and still has **no** send function (L45–46).
7. Hosted logon calls `CTraderFixSession.TryLogonAsync` only (`35=A`). It never calls `CTraderFixDemoTestTrade.SendAsync`.

So today’s copy-path safety is **`SAFE_BY_ABSENCE` + const unimplemented/unreconciled + persist-force-false**, **not** a process pin of the §41 token. W500_108’s “pin-false + SAFE_BY_ABSENCE” is half-stale. That is the correct *current* no-loss outcome. It is **not** a reason to leave the flag true “to try one lot.”

### 2.2 Settings cannot legally arm send

Live API (`Program.cs`, 160 lines) is **minimal APIs only** — `AddControllers` / `MapControllers` = **0** in `D:\Prop\apps\api`. `SettingsController` (`LiveCopyEnabled` PUT → Redis `settings:flags:live_copy`) is therefore **unbound** on the running host. Even if it were bound:

- it writes a **different name** (`LiveCopyEnabled`), not `REAL_COPY_EXECUTION_ENABLED`;
- Redis is not required (`/api/health` reports redis `healthy=false` in the stub);
- it cannot create a NewOrderSingle builder.

`/api/settings` GET returns `featureFlags.REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled` which **follows env**. A dashboard “YES” on LiveCopyPage L13 is **not** a fill.

---

## 3. No copy-path `35=D` NewOrderSingle (measured this pass)

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

### 3.2 Residual: `CTraderFixDemoTestTrade` can emit `35=D` — **not** the copy hop

W500_148 “product outbound MsgType is `35=A` only” is **stale for the Fix.CTrader tree**. Current `CTraderFixDemoTestTrade.SendAsync` (signature now includes `flattenOnly = false`) writes:

| Line | MsgType | Role |
|---|---|---|
| L75 | `A` | TRADE logon |
| L95 | `x` | SecurityListRequest |
| L125 | `AN` | RequestForPositions |
| L138 | `D` | flatten existing demo pos |
| L145 | `D` | open 1-lot market |
| L179 | `D` | close if filled |

Fail-closed **before** TCP (L43–59): host must start with `demo-`; SenderCompID must start with `demo.`; refuse `live.` / `live-`; refuse live account **`1369850`**. Conjunction miss returns `Allowed=false`, `OrderSent=false`.

**Callers of `CTraderFixDemoTestTrade` in product `*.cs`:** only `D:\Prop\tools\DemoFixTestTrade\Program.cs` L33. **Zero** hits in `apps/api`, `apps/fix-worker`, `apps/mt5-worker`, `CopyTradingService`, `CTraderFixLogonHostedService`, `CopyTradingHostedService`. The helper **does not read** `REAL_COPY_EXECUTION_ENABLED`. This slot did **not** invoke the tool.

Treat this as a **residual second sender**, not as copy-to-cTrader. Do **not** wire it into the hosted hop. Do **not** treat a demo-host test trade as §70 item 12.

### 3.3 Product C# census (this pass)

`grep` `35=D` under `D:\Prop\src` `*.cs`: **0** (demo helper uses `Build("D", …)`, not the literal `35=D`).  
`grep` `35=D` under `D:\Prop\apps` `*.cs`/`*.json`/`*.csproj`: **0**.

`NewOrderSingle` hits in product `src/` + `apps/` are **name / log / LastError / const only**, except the demo helper’s `Build("D")`:

| File | Role | Wire? |
|---|---|---|
| `CTraderFixOptions.cs` L33 | XML comment | No |
| `CTraderFixLogonHostedService.cs` L69 | info log “still unimplemented” | No |
| `LiveRuntimeStatus.cs` L42–44 | snapshot copy: armed ⇒ “No ticket will be sent”; unarmed ⇒ “No capital at risk” | No |
| `CopyTradingService.cs` L16 | `NewOrderSingleImplemented = false` | No |
| `CopyTradingService.cs` L198–200 | `LIVE_SEND_BLOCKED_UNIMPLEMENTED` | No |
| `CopyTradingHostedService.cs` L30 | “Live NewOrderSingle still blocked” | No |
| `BrokerCatalogSeed.cs` L105 | TRADE `LastError` “logon/recon only; NewOrderSingle off” | No |
| `DemoSeeder.cs` L101 | TRADE `LastError` | No |
| `apps/api/Program.cs` L69 | recon stub note | No |
| `apps/fix-worker/Worker.cs` L22, L41, L46 | start log + LastError + refuse warning | No |
| `ExecutionOrderStateMachine.cs` L35 | `MayRetryNewOrderSingle` status math | No socket |
| `CTraderFixDemoTestTrade.cs` | standalone demo `Build("D")` | **Yes, demo-gated; not copy** |
| `OverviewPage.tsx` / `ShadowPortfolioPage.tsx` | UI copy | No |

`TraderIntelligence.Fix.CTrader.csproj`: Hosting + Configuration + Logging + EF only. **No** `QuickFIXn.Core` / `QuickFIXn.FIX44`. `grep` `QuickFix` / `QuickFIX` / `IRiskEngine` in product `*.cs`/`*.csproj`: **0**.

### 3.4 YoPips C++ is not a second cTrader sender

`grep` of `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` for `35=D`, `NewOrderSingle`, `cTrader`, `c-trader`, `REAL_COPY`, `FIX.4`: **no matches**.

That tree has `MT5Manager::SendTrade` / `TradeExecutionService::dispatchSendTrade` — **source-broker MT5 dealer mutations**, not Pepperstone/cTrader FIX. Do not treat YoPips `SendTrade` as copy-to-cTrader.

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

**Empty-if honesty (L90–93):** when `RealExecutionEnabled == false` and action is not `CloseExposure`, the `if` body is **empty**. The engine may still return `Outcome=Approve` / `Reason=APPROVED` with `AllowFixSend=false`. Unit fact `Real_flag_false_never_allows_fix_send` (`RiskEngineTests.cs` L21–26) asserts exactly that: Approve + `AllowFixSend=false` while fixture `RealExecutionEnabled = false` (L72).

That is **unsafe if a future sender keys off `Outcome==Approve` and ignores `AllowFixSend`**. Another reason the production flag must stay false **and** no copy-path `35=D` builder may be added until the sender is proven to consult `AllowFixSend`.

`grep` `.Evaluate(` under `D:\Prop` `*.cs`: definition + **5** unit facts + **1** product caller `CopyTradingService.cs` L159. W500_59 / W500_99 / D43 “0 product callers” is **stale** (see W500_119).

What the product caller actually does:

```184:204:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                var rec = new RiskDecisionRecord
                {
                    Id = Guid.NewGuid(),
                    CopyIntentId = intent.Id,
                    Outcome = decision.Outcome,
                    ApprovedQuantity = decision.ApprovedQuantity,
                    Reason = decision.Reason,
                    AllowFixSend = false,
                    DecidedAt = now
                };
                _db.RiskDecisions.Add(rec);
                intent.RiskDecisionId = rec.Id;

                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
                else
                {
                    intent.Status = "SHADOW_ONLY";
```

`CopyTradingService` also passes `Reconciled = VenueReconciled` (**const false**) and `RealExecutionEnabled = _runtime.RealCopyEnabled` (env). For `OpenExposure` that means `Evaluate` returns `VENUE_NOT_RECONCILED` / `AllowFixSend=false` **even if the flag is armed**. Then persist overwrites `AllowFixSend` to false again.

`new ExecutionIntent` / `ExecutionIntents.Add` in product `*.cs` = **0**. The EF `DbSet<ExecutionIntent>` exists; nobody writes it.

`AllowFixSend` is **not** a socket. `RiskEngine` is registered `AddSingleton` in DI (L45) but the copy service constructs `new RiskEngine()` itself (L23) — the singleton is unused by the hop.

`GetRiskAsync` (`EfDashboardQueries.cs` L198–208) now exposes `_runtime.RealCopyEnabled` on the DTO. That is a **dashboard bit**, not a choke.

---

## 5. Recon gate is **not** ready (so flag stays false)

Architecture §42 / §70.3 / §70.14: after TRADE logon, **block new executions**, run OrderMassStatus + RequestForPositions, reconcile, only then consider send.

What exists:

| Piece | Status |
|---|---|
| `GET /api/reconciliation/status` (`Program.cs` L63–69) | Hardcoded `unknownPositions=0`, `mismatches=0`, `orphanFills=0`, note = “recon runs only after FIX TRADE logon; NewOrderSingle still off” |
| `CopyTradingService.VenueReconciled` | **`const false`** (L15) |
| `ExecutionOrderStateMachine.RequiresReconciliation` | Status math only |
| `FixSessionOwnership.MarkReconciled()` | In-memory bool the **caller** sets (`_reconciled = true`); no venue book |
| `CTraderFixSession` MsgTypes | `A` only. **Missing** `AF` (mass status), `AN` (positions), `H` (status), `D`/`F`/`G` |
| Demo helper `35=AN` / `35=D` | **Not** product recon; tools-only |
| Persist-before-send / `GuardedNewOrderSingle` | **MISSING** |
| `MayRetryNewOrderSingle` after send | **false** except `NotSent`/`Rejected` (L35–36) — correct math, no copy send to retry |

`grep` `OrderMassStatusRequest` / `RequestForPositions` / `35=AF` / `35=AN` / `35=F` / `35=G` in product `*.cs` (excluding demo helper): **0**.

A stub that reports zeros is **anti-evidence** for G07 / §70.14. Product recon **cannot** run on the wire today. Therefore recon **cannot** PASS, and the flag **must stay false**.

Worker honesty (L36–46): stamps TRADE `Disconnected` + `LastError = "No live TRADE socket. NewOrderSingle remains off."` If config `real` is true it **logs a warning** and still does not send.

---

## 6. Fetch ALL Achiever + Starwave groups/traders is independent of send

Catalog walk (flag-blind):

- `NativeMt5BrokerConnector.GetGroupsCore` L155: `GroupRequestArray("*", arr)` then cache fallback `GroupTotal`/`GroupNext`.
- `ReadAccountsForGroup` L223: `UserRequestArray(gname, users)` first; `UserGetByGroup` only on hard fail; empty → `UserLogins` + `UserRequestByLogins`.
- `DealIngestionService.SyncCatalogAsync` L45–49: `GetGroupsAsync` + `GetAccountsAsync(null)` (all groups).
- Positions: `GetGroupPositionsAsync("*")` or per-account. Ingest file has **0** `Take(`/`Skip` on catalog.
- Dashboard `GetTradersAsync` L99: `foreach (var account in accounts)` left-join scores. No `Take` on the trader list.
- Manual resync (`Program.cs` L124) walks **both** `ACHIEVER` and `STARWAVEFX`.
- Hosted ingest (`LiveIngestHostedService`) catalogs **every** registered connector, then scores `ListLoginsWithDealsAsync` only.

Prior **measured** Manager census (`LIVE_MANAGER_FETCH_MEASURED.md`, `LIVE_GROUPS_AND_TRADERS.json`, `CREDENTIALS_AND_COPY_STATUS.md`) **re-summed this slot** from JSON group counts (not re-attached):

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| Achiever | HTTP proxy | 8 | 6512 | 1506 |
| StarwaveFX | direct | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever group sum: `2+179+4+5+4+6295+0+23 = 6512`. Starwave group sum: `11+4+170+1735+22+0+0+4+0+2 = 1948`.

P500 later live API (same day, not re-probed here) reported **8463** accounts (Achiever 6512 + Starwave ~1951). Delta is ingest/timing, not a new Manager attach by this slot.

Dashboard `/api/groups` and `/api/traders` are catalog reads. Those walks do **not** read `REAL_COPY_EXECUTION_ENABLED`.

Copy path from ingest scoring remains **SHADOW only** via `PersistDemoShadowAsync` (`EfTradingStore.cs` L307 `Status = "SHADOW_ONLY"`) — **bypasses** `RiskEngine.Evaluate` (W500_119 residual). Hosted `CopyTradingHostedService` also writes SHADOW intents every 20s.

`TraderStateMachine.FromBaseline` reachable set = `{INSUFFICIENT_DATA, RISK_BLOCKED, SHADOW, WATCH, EARLY_SCORE}`. `CanPromoteToLive => false`.

**This slot did not live-reattach.** Census numbers are the last measured probe / P500 GET, not a new Connect. Fetch-all remains the **goal** and is **not** blocked by keeping the copy flag false.

Copy-to-cTrader **destination** stays: QUOTE/TRADE Logon `35=A` for session proof + future recon. **No** copy-path `35=D` until the conjunction in §1.4.

---

## 7. What would have to be true before the flag may flip

Do **not** treat this list as a waiver. Default remains OFF if any box is unchecked.

1. §68 **19/19 PASS** with on-disk evidence (not FakeMt5, not in-memory demo, not unused methods).
2. §70 **14/14 PASS**, including:
   - **11** risk rejection **before** FIX send (product `Evaluate` on the hop; spy `Submit=0` when flag false / unreconciled / stale quote);
   - **14** recon **blocks** execution while inconsistent (real `35=AF`/`AN` + persist + refuse).
3. Unique ClOrdID persist-before-send; `MayRetryNewOrderSingle` false on unknown; no blind catch-up when the flag later goes true (§63 / A53).
4. Quantity conversion G10 measured (not passthrough; copy-path tag 38 exists).
5. Named manual review. Committed `appsettings*` / `.env.example` / compose stay `false` until that review.
6. A copy-path `35=D` builder that is **unreachable** unless the flag **and** `AllowFixSend` **and** `READY_FOR_EXECUTION`. Unit-test the refuse on LoggedOn TRADE.
7. Lab `.env` L73 returned to `false` (operator leftover today). Process pin restored if the env token is not sufficient as a choke.
8. Demo helper remains **out** of the hosted copy hop (or is deleted). Demo-host `35=D` is **not** a go-live.

Until then: **fetch + score + SHADOW only.**

```text
[DO NOT] Enable REAL_COPY_EXECUTION_ENABLED to “try one lot”
[DO NOT] Leave lab .env true and treat dashboard “armed” as profit mode
[DO NOT] Send 35=D from an MT5 callback
[DO NOT] Retry 35=D after a broken send
[DO NOT] Flush a backlog when the flag later flips true
[DO NOT] Wire CTraderFixDemoTestTrade into CopyTradingService
```

---

## 8. Files read / grepped (this slot)

| Path | Why |
|---|---|
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default false, unbound |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | only `35=A` writer |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | demo-gated `Build("D")`; not copy |
| `D:\Prop\tools\DemoFixTestTrade\Program.cs` | sole caller of demo helper |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | **no** re-pin; logs Armed |
| `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs` | caller-set recon bool |
| `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs` | unbound; no TLS |
| `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` | no QuickFIX |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | env bind L41 |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | const unimplemented + force-false send |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | SHADOW tick |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | flag-blind catalog |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | snapshot copy |
| `D:\Prop\src\Application\Copy\CopyTradingModels.cs` | gate DTO |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | flag-blind catalog + score |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | AllowFixSend math |
| `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs` | retry/recon math |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | no auto LIVE |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | `*` + UserRequestArray |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | SHADOW_ONLY writer (bypasses Evaluate) |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | all-accounts traders; risk DTO env-bound |
| `D:\Prop\apps\api\Program.cs` | settings + recon stub + env load |
| `D:\Prop\apps\api\appsettings.json` | `LiveCopyEnabled=false` |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | unbound Redis PUT |
| `D:\Prop\apps\fix-worker\Worker.cs` | refuse + Disconnected stamp |
| `D:\Prop\apps\fix-worker\Program.cs` | no EnvFile load |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | UI armed bit |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | flag-false AllowFixSend |
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` | §§41/42/68/69/70 |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | 18/8460 prior |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | re-sum 8/6512 + 10/1948 |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | “forced false” **stale** vs current DI |
| `D:\Prop\reports\swarm\20260818\D41_fuv_now.md` | §69 0/12 |
| `D:\Prop\reports\swarm\20260818\D42_gates_now.md` | §68 0/19 |
| `D:\Prop\reports\swarm\20260818\D43_s70.md` | §70 0/14 |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_148.md` | sibling; “only 35=A” stale |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_108.md` | stale pin-false sibling |
| YoPips `...\src\` | 0 cTrader senders; MT5 `SendTrade` only |

---

## 9. Residual honesty (not a FAIL of the stay-false claim)

| Residual | Why it does not authorize send |
|---|---|
| Lab `.env` L73 = `true` and DI binds it | Policy leftover. No copy-path builder. Persist `AllowFixSend=false`. Const unimplemented. |
| Hosted logon **no longer** re-pins false | Logon ≠ order. Socket disposed after `35=A`. |
| `FEATURE_COPY_TRADING_ENABLED=true` | Shadow pipeline only. |
| W500_68/108 “everywhere false” | **Stale.** Do not cite as current pins. |
| W500_148 “only outbound 35=A” | **Stale for Fix.CTrader tree.** Demo helper now `Build("D")`. Copy hop still 35=A-only. |
| CREDENTIALS “forced false” | **Stale** vs `DependencyInjection.cs` L41. |
| Worker refuse is a **log line** | No function can emit copy-path `35=D` even if nested key is true |
| Hosted service **can** TLS Logon `35=A` if password present | Logon ≠ order |
| `CTraderFixDemoTestTrade` | Tools-only; demo-host/sender/account gate; refuses `1369850`; unread by copy hop |
| Prior census (not re-probed this slot) | Fetch-all is read-only either way |
| `SettingsController` PUT exists on disk | Unbound; different flag name; no builder |
| `RiskEngine` can return `Approve` with flag false | `AllowFixSend=false` and no copy sender |
| D41 FakeMt5 wording | Stale vs fail-closed Native DI. Still not a send license. |

---

## 10. Slot close

**Verdict: `CONFIRMED_MUST_STAY_FALSE`.**

Fetch ALL Achiever+Starwave groups/traders (prior **18/8460**, P500 later **8463**) continues as a **Manager read**. Copy to cTrader must **not** send live orders. Risk to capital from the copy process: **NONE** (`SAFE_BY_ABSENCE`; flag may be armed in lab env; demo helper is a separate opt-in tool, not the hop). Product source not edited. Secrets not printed. Do not treat leftover `.env=true` as a go-live.
