# E002 — `REAL_COPY_EXECUTION_ENABLED` defaults **false**; no `NewOrderSingle` sender exists

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\E002_no_live_send.md` |
| Agent | E002 (no-live-send pin) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (worktree after D32/D69; `Worker.cs` mtime `2026-08-18T08:04:48.7473622Z`) |
| Assigned | Confirm `REAL_COPY_EXECUTION_ENABLED` default **false** and that **no** `NewOrderSingle` sender exists. Write this file. Do not modify product source. |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Binding law | Architecture §41 / §56 (`REAL_COPY_EXECUTION_ENABLED=false`); §33–§34 / §70 (no live `35=D` until gates); A25 §6.3; A49; A101 item 12 |
| Siblings (do not treat as this file) | D69 (flag default recensus), D32 (worker stamps `Disconnected`), C07 / D07 (`SAFE_BY_ABSENCE`), C19 / D52 (QuickFIX/n absent), C43 (Logon not proven), A08 (stale “flag not in C#”), A101 0/14 |
| Method | Re-read options POCO, fix-worker `Worker`/`Program`/`csproj`/`appsettings*`/`launchSettings`, API `Program` + `SettingsController` + `appsettings`, dashboard queries/DTOs, `RiskEngine`, FSM/`ClOrdIdFactory`, Fix.CTrader parser/quote/ownership/harness + `deps.json`, seeder, DI, mt5-worker, fake connector, web Live/Shadow pages, docker-compose, local `.env`. Product-tree `Select-String` (exclude `bin`/`obj`/`node_modules`) for send/flag symbols. SHA-256 via `Get-FileHash`. Nothing from memory. **No product edit.** |

**Honesty rule:** a compile-time `= false` is a **default**. A `GetValue(..., false)` fallback is a **default**. A hardcoded JSON/API `false` is a **display floor**, not a send gate. Absence of a `35=D` builder / initiator is **SAFE_BY_ABSENCE**, not proof that the flag is a wired choke. `AllowFixSend` on a risk DTO is **not** a socket write. Do not claim “flag-gated execution.”

---

## 0. Verdict (binding)

**CONFIRMED on both assigned claims.**

| Claim | Result | Class |
|---|---|---|
| `REAL_COPY_EXECUTION_ENABLED` default is **false** | **Yes** | `EXISTS_AND_GOOD` vs §41 |
| A `NewOrderSingle` / `35=D` **sender** exists in product | **No** | `MISSING` sender; live send **`SAFE_BY_ABSENCE`** |
| The flag is an implemented send gate (`GuardedNewOrderSingle` / `MaySendNewOrderSingle`) | **No** | `GATE_INCOMPLETE` |
| Flipping the flag to `true` would place a live order | **No** | still no builder, no initiator, no TRADE socket |
| Safe to enable `REAL_COPY_EXECUTION_ENABLED=true` | **No** | A100/C14 still 0/19; A101/D43 still 0/14 |

One-line:

```text
default RealCopyExecutionEnabled = false
AND there is no function that emits FIX MsgType=D to a socket.
```

Do **not** tick Architecture §70.12 as a coded refuse-path PASS. Vacuous “cannot send because nothing can send” is the current safety outcome, not a unit-tested gate.

---

## 1. Flag default = false (measured surfaces)

Architecture §41 / §56 name the env twin:

```env
REAL_COPY_EXECUTION_ENABLED=false
```

Every measured product site is **false**, hardcodes **false**, or falls back to **false**.

| Surface | What it does | Default | Bound to `CTraderFixOptions`? |
|---|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | C# initializer; comment “Default OFF” | **`false`** | **is** the POCO |
| Architecture §41 / §56 | design law | **`false`** | design only |
| `apps/fix-worker` `GetValue("CTrader:RealCopyExecutionEnabled", false)` | log + unused `if (real)` warning | fallback **`false`** | **No** — different key, no `IOptions<CTraderFixOptions>` |
| `apps/fix-worker/appsettings*.json` | logging only | key **absent** | N/A (fallback applies) |
| `apps/fix-worker` `launchSettings.json` | `DOTNET_ENVIRONMENT=Development` only | key **absent** | N/A |
| `apps/api/Program.cs` `GET /api/settings` | hardcoded dictionary | **`false`** | **No** (display) |
| `apps/api/appsettings.json` `FeatureFlags:LiveCopyEnabled` | different name | **`false`** | **No** |
| `SettingsController` `LiveCopyEnabled` | Redis `settings:flags:live_copy` | `GetValue(..., false)` | **No**; controller **not mapped** (`AddControllers`/`MapControllers` absent) |
| `EfDashboardQueries` `OverviewDto.RealCopyEnabled` | literal `false` | **`false`** | **No** |
| `EfDashboardQueries` `FixSessionDto.ExecutionEnabled` | literal `false` | **`false`** | **No** |
| `EfDashboardQueries` `RiskDashboardDto.RealCopyEnabled` | literal `false` | **`false`** | **No** |
| `LiveCopyPage.tsx` | static copy “is false” | display | **No** |
| Local `.env` (gitignored) | `REAL_COPY_EXECUTION_ENABLED=false` | **`false`** | **No** — worker does not read this env name |
| Tracked `.env.example` | **missing from worktree** | — | N/A |
| `docker-compose.yml` | api + postgres + redis only | key **absent** | N/A |
| `tests/` `RealCopyExecutionEnabled` | **0** hits | — | N/A |
| `RiskEngine` `RealExecutionEnabled` | caller bit; unit fixture **`false`** | fixture **`false`** | **No** — different identifier |

Owning property (the only compile-time default that matches the architecture name):

```32:35:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;
```

Worker bind (fallback default, **not** the env name `REAL_COPY_EXECUTION_ENABLED`):

```21:22:D:\Prop\apps\fix-worker\Worker.cs
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);
```

API display floor (hardcoded; does not read config or the POCO):

```42:46:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", () => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool> { ["REAL_COPY_EXECUTION_ENABLED"] = false },
    brokerConfigs = new[] { new { id = "ACHIEVER", name = "Achiever", enabled = true }, new { id = "STARWAVEFX", name = "StarwaveFX", enabled = true } }
}));
```

Dashboard literals (not a gate):

```36:43:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            shadowPnl,
            0,
            0,
            0,
            brokers > 0,
            quote?.Status is FixSessionStatus.LoggedOn or FixSessionStatus.ReadyForMarketData or FixSessionStatus.ReadyForExecution,
            trade?.Status is FixSessionStatus.LoggedOn or FixSessionStatus.Reconciling or FixSessionStatus.ReadyForExecution,
            false);
```

`GetFixSessionsAsync` last arg `false` → `FixSessionDto.ExecutionEnabled`.  
`GetRiskAsync` seventh arg `false` → `RiskDashboardDto.RealCopyEnabled`.

**Binding gap (do not greenwash):** ASP.NET Core will **not** map env `REAL_COPY_EXECUTION_ENABLED` onto `CTrader:RealCopyExecutionEnabled`. Setting the architecture env name to `true` would **not** flip the worker’s `GetValue` unless `CTrader__RealCopyExecutionEnabled` is also set. `AddTraderIntelligence` does **not** register `IOptions<CTraderFixOptions>`. `CTraderQuoteService` takes the options object and **never reads** `RealCopyExecutionEnabled`.

If `real` is true, the worker **only logs a warning** and still stamps TRADE `Disconnected`. It does not send.

```45:46:D:\Prop\apps\fix-worker\Worker.cs
            if (real)
                _logger.LogWarning("Real copy is enabled in config, but worker still refuses NewOrderSingle until risk/reconciliation gates pass.");
```

That “refuse” is English in a log line, not a choke function.

---

## 2. No `NewOrderSingle` sender (measured)

### 2.1 Product-tree census (`src/`, `apps/`, `tests/`; exclude `bin`/`obj`/`node_modules`)

| Pattern | Hits | Meaning |
|---|---:|---|
| `35=D` | **0** | no outbound NewOrderSingle wire text |
| `GuardedNewOrder` | **0** | A101 choke type **MISSING** |
| `SubmitNewOrder` | **0** | no trade-client submit |
| `MaySendNewOrder` | **0** | A64 conjunction **MISSING** |
| `QuickFix` / `QuickFIX` | **0** | official adapter **not referenced** |
| `SocketInitiator` / `IInitiator` / `SessionSettings` | **0** | no initiator |
| `TcpClient` / `SslStream` | **0** | no FIX transport |
| `OrderSend` / `DealerSend` | **0** | no MT5 venue send either |
| `NewOrderSingle` | **8** | comments / log / helper **name** only (listed below) |
| `REAL_COPY_EXECUTION_ENABLED` | **3** | log format, API display, UI copy |
| `RealCopyExecutionEnabled` | **2** | POCO + worker `GetValue` |

The eight `NewOrderSingle` product hits — **none encode or write `35=D`:**

| File:line | Kind |
|---|---|
| `src/Domain/Execution/ExecutionOrderStateMachine.cs:35` | `MayRetryNewOrderSingle` — pure status helper |
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs:33` | XML comment |
| `src/Infrastructure/Seeding/DemoSeeder.cs:101` | TRADE `LastError` string |
| `apps/fix-worker/Worker.cs:22` | log format |
| `apps/fix-worker/Worker.cs:41` | TRADE `LastError` string |
| `apps/fix-worker/Worker.cs:46` | warning log |
| `apps/web/src/pages/ShadowPortfolioPage.tsx:7` | UI copy |
| `tests/Unit/ExecutionAndSizingTests.cs:14` | asserts retry helper is false after send-attempt **state** |

### 2.2 Every product `35=` builder (Fix.CTrader)

| File | MsgType | Role | Sent to venue? |
|---|---|---|---|
| `CTraderQuoteService.BuildSecurityListRequestTags` | `y` | tag list only | **No** |
| `CTraderQuoteService.BuildMarketDataRequestTags` | `V` | tag list only | **No** |
| `FixSimulationHarness.SimulateLogonSuccess` | `A` | in-memory string | **No** |
| `FixSimulationHarness.SimulateLogonFail` | `3` | in-memory string | **No** |
| `FixSimulationHarness.SimulateDisconnect` | `0` | placeholder | **No** |
| `FixSimulationHarness.SimulateSecurityList` | `y` | in-memory string | **No** |
| `FixSimulationHarness.SimulateMarketDataSnapshot` | `X` | in-memory string | **No** |
| `FixSimulationHarness` ER helpers | `8` | in-memory string | **No** |

**Missing (required for a live sender):** `35=D` NewOrderSingle, `35=F` cancel, `35=G` replace, `35=H` status, `35=AF` mass status, `35=AN` positions.  
`FixMessageParser.BuildFixMessage` is a **generic** pipe builder for tests. No caller passes `35=D`.

### 2.3 Hosts that could have been senders

| Host | What it actually does | Send path? |
|---|---|---|
| `apps/fix-worker/Worker.cs` | 15 s EF loop: stamp QUOTE+TRADE `Disconnected`; log flag | **No** |
| `apps/fix-worker/Program.cs` | DI + seeder + `AddHostedService<Worker>` | **No** |
| `apps/mt5-worker/Worker.cs` | ingest + score; log “Execution copy is not performed here.” | **No** |
| `FakeMt5BrokerConnector` | in-memory groups/accounts/deals | **No** `OrderSend` |
| `ShadowCopyEngine` | in-process fill simulation | **No** venue |
| `RiskEngine` | may set `AllowFixSend`; **zero callers send** | **No** |
| `ClOrdIdFactory` / `ExecutionOrderStateMachine` | id + status math | **No** |
| `ExecutionIntent` entity | persist shape (`SentAt` unused by a sender) | **No** |
| `FixSessionOwnership` | in-memory fence; unused by worker | **No** |
| `apps/api` | dashboard + demo resync | **No** order endpoint |
| `SettingsController` PUT `LiveCopyEnabled` | Redis string only; **not routed** | **No** |

`Fix.CTrader.csproj` has **no** `PackageReference`. `Fix.CTrader.deps.json` runtime deps = Domain + Application (+ transitive FluentValidation). **No** `QuickFIXn.Core` / `QuickFIXn.FIX44`.

`AddTraderIntelligence` registers EF, fake MT5 connectors, ingestion, scoring, dashboard. **No** FIX session, **no** execution worker, **no** `GuardedNewOrderSingle`.

### 2.4 Adjacent types that look like send (they are not)

`MayRetryNewOrderSingle` is **not** a retry sender:

```35:36:D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs
    public static bool MayRetryNewOrderSingle(ExecutionOrderStatus status) =>
        status is ExecutionOrderStatus.NotSent or ExecutionOrderStatus.Rejected;
```

`RiskEngine` computes `AllowFixSend` from a **caller-supplied** `RealExecutionEnabled` bit. The empty `if (RealExecutionEnabled == false …)` block does **not** force `AllowFixSend=false` by itself; the later `allowSend = request.RealExecutionEnabled && …` does. Unit fixture uses `RealExecutionEnabled = false` and asserts `AllowFixSend == false`. **No worker reads `AllowFixSend`.** A true bit would still have nowhere to go.

---

## 3. File identity (this pass)

| Path | SHA-256 | Bytes |
|---|---|---:|
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` | 2344 |
| `apps/fix-worker/Worker.cs` | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` | 2093 |
| `apps/fix-worker/Program.cs` | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` | — |
| `apps/fix-worker/TraderIntelligence.FixWorker.csproj` | `D7572CBFF273089587D4A68B2A95EB1933C2DF388177F8665BE7C3CC338B5DB4` | — |
| `apps/fix-worker/appsettings.json` | `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` | — |
| `apps/fix-worker/appsettings.Development.json` | `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` | — |
| `apps/fix-worker/Properties/launchSettings.json` | `25A750D823B04F229FBC49A690F997E969276FFB1A5E5F4EF198DF7DE7CBEF9B` | — |
| `apps/api/Program.cs` | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | 4731 |
| `apps/api/appsettings.json` | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` | 1254 |
| `apps/api/Controllers/SettingsController.cs` | `B19274DC71F6BECB54B6C1A270D3CA9F47C9B1CCD340D0C303F4C663CA50C23F` | 3732 |
| `apps/web/src/pages/LiveCopyPage.tsx` | `F85CF339AAD7B2A9F639DA83466DC7949EF765EB3321C985044044978010BC82` | 321 |
| `src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj` | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` | — |
| `src/Fix.CTrader/Parsing/FixMessageParser.cs` | `C58681E761D43052B53D2A8D00883C461A9E3CEB5B7DF8995D50F8155F710E3D` | — |
| `src/Fix.CTrader/Services/CTraderQuoteService.cs` | `7D2FDE1D33B47D619EA8BB0EC5F943BC21D8D97B46BEA269D70D46A20859B44A` | — |
| `src/Fix.CTrader/Services/FixSessionOwnership.cs` | `30029E29EE66C2114643AAF8FD0E0D8566C075A0FF693CCA7043CEADED5E6D20` | — |
| `src/Fix.CTrader/Testing/FixSimulationHarness.cs` | `99A28D8F3F49028706C75B9C4DC46B4CCB3FF98E90AAED3B4B874DD1B4351616` | — |
| `src/Domain/Risk/RiskEngine.cs` | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` | 8567 |
| `src/Domain/Execution/ExecutionOrderStateMachine.cs` | `CDF7B67EB0D032513C2EBF73BC5B3F208F665D6A2A18327E39975198DCF12219` | — |
| `src/Infrastructure/Seeding/DemoSeeder.cs` | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | — |
| `src/Infrastructure/DependencyInjection.cs` | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | — |
| `src/Infrastructure/Dashboard/EfDashboardQueries.cs` | (see D69; worktree still literals `false`) | 8708 |
| `docker-compose.yml` | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` | — |
| `.env` (local, gitignored) | `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA` | — |

Hashes for `CTraderFixOptions.cs`, `Worker.cs`, `Program.cs` (api), `LiveCopyPage.tsx`, `.env` **match D69**. Worker hash **matches D32**.

---

## 4. What this file does **not** prove

- Live FIX Logon (C43: **NOT PROVEN**). Send-off ≠ connected.
- A coded refuse when `REAL_COPY_EXECUTION_ENABLED=false` and TRADE is LoggedOn (cannot happen: TRADE never logs on; no 35=D function to refuse).
- `RiskEngine` empty `if (RealExecutionEnabled == false)` as a hard block (it is a no-op comment site; send is blocked later only if the caller bit is false).
- Settings UI cannot flip live copy (controller unmapped; even if mapped it writes Redis, which no sender reads).
- Phase 8 readiness. §68 / §70 remain FAIL.

---

## 5. Classification table

| Slice | Class |
|---|---|
| `RealCopyExecutionEnabled` C# default | **`false` (`EXISTS_AND_GOOD`)** |
| Committed `appsettings` / compose / launchSettings live-send key | **absent** (fail-closed fallback) |
| Architecture env `REAL_COPY_EXECUTION_ENABLED` bound into worker | **NOT WIRED** |
| `GuardedNewOrderSingle` / `MaySendNewOrderSingle` | **MISSING** |
| QuickFIX/n initiator | **MISSING** |
| Product `35=D` builder | **MISSING** |
| Live TRADE socket | **ABSENT** (`Disconnected` stamp only) |
| Live send if process starts now | **`SAFE_BY_ABSENCE`** |
| Implemented flag gate | **`GATE_INCOMPLETE`** |
| Product source edited by E002 | **No** |

---

## 6. Assigned answers (do not paraphrase away)

1. **Does `REAL_COPY_EXECUTION_ENABLED` default false?**  
   **Yes.** The owning POCO is `RealCopyExecutionEnabled { get; set; } = false`. Worker `GetValue(..., false)`, API/UI/dashboard, `.env`, and `FeatureFlags:LiveCopyEnabled` are all false. No committed config sets the flag true.

2. **Does a `NewOrderSingle` sender exist?**  
   **No.** Zero `35=D` / `GuardedNewOrder*` / `SubmitNewOrder*` / QuickFIX initiator / TCP-TLS FIX client / MT5 `OrderSend` in product C#. Helpers and log strings are not senders.

**Do not enable the flag. Do not add a sender in this task.** Product source was not modified.
