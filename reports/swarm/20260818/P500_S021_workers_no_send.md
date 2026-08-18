# P500_S021 — Workers do not send MT5 trades or FIX `35=D`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S021_workers_no_send.md` |
| Agent | P500_S021 (workers no-send pin) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned | Read `apps/fix-worker` `Program.cs` / `Worker.cs` and `apps/mt5-worker`. Confirm workers do not send MT5 trades or FIX `35=D`. Ingest/score only. Do not edit product. |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Method | Full read of both worker hosts (`Program.cs`, `Worker.cs`, `.csproj`, `appsettings*.json`, `launchSettings.json`). Followed every type they resolve: `AddTraderIntelligence`, `DealIngestionService`, `ReconstructionScoringService`, `IMt5BrokerConnector` / `NativeMt5BrokerConnector`, `LiveMt5Registration`, `LiveIngestHostedService`, `CTraderFixLogonHostedService`, `CTraderFixSession`. Product-tree grep for `35=D`, `NewOrderSingle`, `OrderSend`, `DealerSend`, `TradeRequest`. Nothing from memory. |

**Honesty rule:** a log line that says “NewOrderSingle remains off” is not a coded send gate. Shared `AddTraderIntelligence` **does** start extra hosted services (live ingest + FIX logon + copy shadow tick) inside **both** worker processes. That is still **not** a trade send. `PositionCreateArray` is an MT5 Manager array factory, not position creation. `35=A` logon (if `CTRADER_FIX_PASSWORD` is present) is **not** `35=D`. Do not claim “flag-gated execution.” Current live-send safety is **SAFE_BY_ABSENCE** of a NewOrderSingle / DealerSend initiator.

## Profit implication

Workers cannot open a losing dest position from this code. They also cannot **make** dest profit. mt5-worker is ingest/score; fix-worker `Worker` stamps `Disconnected`. Shared DI may send FIX **`35=A`** and write **SHADOW** intents — still no `35=D`. Flipping `REAL_COPY_EXECUTION_ENABLED` still does not place an order (`NewOrderSingleImplemented = false`; no builder). Lower loss = leave it that way until Stage D.

**Remeasured 2026-08-18:** `grep` `(35, "D")` / `OrderQty` / `OrderSend` / `DealerSend` in product workers + `src/Mt5` + `src/Fix.CTrader` = **no sender**. FIX session outbound is still `(35, "A")` only (`CTraderFixSession.cs`). `CopyTradingHostedService` is now also registered by `AddTraderIntelligence` (L59) — shadow intents only.

---

## 0. Verdict (binding)

**CONFIRMED.** Both worker hosts are ingest / score / session-status only. They do **not** place MT5 trades and they do **not** emit FIX `MsgType=D` (`NewOrderSingle`).

| Claim | Result | Class |
|---|---|---|
| `apps/mt5-worker` `Worker` sends an MT5 trade (`OrderSend` / `DealerSend` / dealer request) | **No** | `SAFE_BY_ABSENCE` |
| `apps/fix-worker` `Worker` sends FIX `35=D` | **No** | `SAFE_BY_ABSENCE` |
| Any worker-owned source file builds or writes `35=D` | **No** | `MISSING` sender |
| Worker loop is ingest + score (MT5) / DB session stamp (FIX) | **Yes** | `EXISTS_AND_GOOD` for this pin |
| Shared DI can open **read** MT5 Manager sockets and (if password present) send FIX **`35=A` logon** | **Yes** | side-effect of `AddTraderIntelligence`; **not** a trade |
| Flipping `CTrader:RealCopyExecutionEnabled=true` would place a live order | **No** | still no `35=D` builder / initiator |

One-line:

```text
mt5-worker = DealIngestionService + ReconstructionScoringService (read catalog/deals/positions, write DB scores)
fix-worker Worker = stamp FixSessionStates Disconnected; NewOrderSingle never built
no 35=D, no OrderSend, no DealerSend in either worker or the connectors they call.
```

---

## 1. Inventory (product files only)

### 1.1 `apps/fix-worker`

| Path | Role |
|---|---|
| `D:\Prop\apps\fix-worker\Program.cs` | Generic host. `AddTraderIntelligence` + `AddHostedService<Worker>`. `EnsureCreated` + `BrokerCatalogSeed`. |
| `D:\Prop\apps\fix-worker\Worker.cs` | 15 s loop. Updates `FixSessionStates` Quote/Trade to `Disconnected`. Never opens a socket. |
| `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj` | net8.0 Worker. Refs Domain, Application, Infrastructure, Fix.CTrader. |
| `D:\Prop\apps\fix-worker\appsettings.json` | Logging only. No FIX host/account. No `CTrader:RealCopyExecutionEnabled`. |
| `D:\Prop\apps\fix-worker\appsettings.Development.json` | Identical logging-only JSON. |
| `D:\Prop\apps\fix-worker\Properties\launchSettings.json` | `DOTNET_ENVIRONMENT=Development` only. |

### 1.2 `apps/mt5-worker`

| Path | Role |
|---|---|
| `D:\Prop\apps\mt5-worker\Program.cs` | Same host shape as fix-worker (shared DI + seed). |
| `D:\Prop\apps\mt5-worker\Worker.cs` | 30 s loop. `SyncBrokerAsync` Achiever + StarwaveFx, then `RebuildTraderAsync` for `{10001,10002,10003,99001}`. |
| `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` | net8.0 Worker. Refs Domain, Application, Infrastructure, Mt5. |
| `D:\Prop\apps\mt5-worker\appsettings.json` | Logging only. |
| `D:\Prop\apps\mt5-worker\appsettings.Development.json` | Logging only. |
| `D:\Prop\apps\mt5-worker\Properties\launchSettings.json` | `DOTNET_ENVIRONMENT=Development` only. |

No other `.cs` files exist under either worker project (excluding `obj/` generated AssemblyInfo / GlobalUsings).

---

## 2. `apps/fix-worker` — measured send surface = none

### 2.1 `Program.cs`

```1:18:D:\Prop\apps\fix-worker\Program.cs
using TraderIntelligence.FixWorker;
using TraderIntelligence.Infrastructure;
using TraderIntelligence.Infrastructure.Persistence;
using TraderIntelligence.Infrastructure.Seeding;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTraderIntelligence(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}

host.Run();
```

Boot does: register shared services, register `FixWorker.Worker`, create schema, seed broker catalog. No FIX client construction here.

### 2.2 `Worker.cs` (the assigned type)

```19:49:D:\Prop\apps\fix-worker\Worker.cs
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
            var quote = db.FixSessionStates.SingleOrDefault(s => s.Qualifier == FixSessionQualifier.Quote);
            // ... Status = Disconnected; LastError = "No live QUOTE socket. Simulator/demo only."
            var trade = db.FixSessionStates.SingleOrDefault(s => s.Qualifier == FixSessionQualifier.Trade);
            // ... Status = Disconnected; LastError = "No live TRADE socket. NewOrderSingle remains off."
            await db.SaveChangesAsync(stoppingToken);
            if (real)
                _logger.LogWarning("Real copy is enabled in config, but worker still refuses NewOrderSingle until risk/reconciliation gates pass.");

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
```

What this loop **does**:

1. Reads `CTrader:RealCopyExecutionEnabled` (default **false**; key absent from both `appsettings` files).
2. Loads `FixSessionStates` rows for Quote and Trade.
3. Writes `Status = Disconnected` and a refuse string.
4. `SaveChangesAsync`.
5. Sleep 15 s.

What this loop **does not** do:

- No `TcpClient`, `SslStream`, QuickFIX/n, or `CTraderFixSession`.
- No message builder, no `35=` field, no `ClOrdID`, no `NewOrderSingle`.
- The `if (real)` branch only **logs**. It does not open TRADE, does not call a sender (there is none), and does not flip `LiveRuntimeStatus.RealCopyEnabled`.

Classification of `FixWorker.Worker`: **DB heartbeat / honesty stamp**. Not a FIX session.

### 2.3 FIX `35=D` search (worker + adapter)

| Location | `35=D` / NewOrderSingle send? |
|---|---|
| `apps/fix-worker/Worker.cs` | Mentions NewOrderSingle in **log / LastError strings only** |
| `apps/fix-worker/Program.cs` | None |
| `src/Fix.CTrader/Sessions/CTraderFixSession.cs` | Builds **`35=A` Logon only** (`BuildLogon`). Writes that one frame, reads one reply, disposes TCP. |
| `src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs` | Calls `TryLogonAsync` twice (QUOTE 5211, TRADE 5212). Forces `_runtime.RealCopyEnabled = false`. Log: “NewOrderSingle still disabled”. |
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | `RealCopyExecutionEnabled` default **false**. Comment only; **not bound** by the worker. |
| `src/Fix.CTrader/Services/CTraderQuoteService.cs` | In-memory SecurityList / MD snapshot helper. **Not registered** by worker DI. No socket. |
| `src/Fix.CTrader/Parsing/FixMessageParser.cs` | Parser / assembler. No initiator. |
| `src/Fix.CTrader/Testing/FixSimulationHarness.cs` | Test harness. Emits simplified `35=y` SecurityList, not `D`. |

The only outbound FIX body in product is:

```96:108:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
            (35, "A"),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            (49, sender),
            (56, target),
            (50, senderSub),
            (57, targetSub),
            (52, sendingTime),
            (98, "0"),
            (108, "30"),
            (141, "Y"),
            (553, username),
            (554, password)
```

That is **Logon**, not NewOrderSingle. There is no function that emits `35=D` to a socket.

---

## 3. `apps/mt5-worker` — ingest / score only

### 3.1 `Program.cs`

```1:18:D:\Prop\apps\mt5-worker\Program.cs
using TraderIntelligence.Infrastructure;
using TraderIntelligence.Infrastructure.Persistence;
using TraderIntelligence.Infrastructure.Seeding;
using TraderIntelligence.Mt5Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTraderIntelligence(builder.Configuration);
builder.Services.AddHostedService<Worker>();
// EnsureCreated + BrokerCatalogSeed, then host.Run()
```

Same host skeleton as fix-worker. No dealer API, no trade client.

### 3.2 `Worker.cs`

```17:44:D:\Prop\apps\mt5-worker\Worker.cs
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MT5 ingestion worker started. Execution copy is not performed here.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var ingestion = scope.ServiceProvider.GetRequiredService<DealIngestionService>();
                var scoring = scope.ServiceProvider.GetRequiredService<ReconstructionScoringService>();
                var from = DateTimeOffset.UtcNow.AddDays(-30);
                var to = DateTimeOffset.UtcNow.AddMinutes(1);
                await ingestion.SyncBrokerAsync(BrokerCodes.Achiever, from, to, stoppingToken);
                await ingestion.SyncBrokerAsync(BrokerCodes.StarwaveFx, from, to, stoppingToken);
                foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
                {
                    var code = login >= 99000 ? BrokerCodes.StarwaveFx : BrokerCodes.Achiever;
                    await scoring.RebuildTraderAsync(code, login, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "MT5 sync cycle failed; will retry. No source trades invented.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
```

Call graph (this `Worker` only):

```
Worker.ExecuteAsync
  ├─ DealIngestionService.SyncBrokerAsync(ACHIEVER)
  ├─ DealIngestionService.SyncBrokerAsync(STARWAVEFX)
  └─ ReconstructionScoringService.RebuildTraderAsync × 4 logins
```

### 3.3 `DealIngestionService` — read Manager, write store

`SyncBrokerAsync` / `SyncCatalogAsync` (`D:\Prop\src\Application\Ingestion\DealIngestionService.cs`):

| Step | Connector / store call | Direction |
|---|---|---|
| Connect | `IMt5BrokerConnector.ConnectAsync` | Manager logon (read session) |
| Groups | `GetGroupsAsync` → `UpsertGroupsBatchAsync` | READ → DB |
| Accounts | `GetAccountsAsync` → `UpsertAccountsBatchAsync` | READ → DB |
| Deals | `GetGroupDealsAsync` / `GetDealsAsync` → `UpsertDealsBatchAsync` | READ → DB |
| Positions | `GetGroupPositionsAsync` / `GetPositionsAsync` → replace positions | READ → DB |

No write-back to the broker.

### 3.4 `ReconstructionScoringService` — DB only

`RebuildTraderAsync`:

1. `LoadDealsAsync` from EF store.
2. `TradeReconstructor.Reconstruct` in-process.
3. `ReplaceReconstructedAsync`.
4. `BaselineScorer.Score`.
5. `UpsertScoreAsync`.
6. `PersistDemoShadowAsync` (outbox + optional **shadow** rows; no venue).

No MT5 handle is even injected into this type.

### 3.5 `IMt5BrokerConnector` contract is read-only

```53:63:D:\Prop\src\Application\Contracts\Mt5Contracts.cs
public interface IMt5BrokerConnector
{
    string BrokerCode { get; }
    Task ConnectAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);
    Task<bool> IsConnectedAsync(CancellationToken ct);
    Task<IReadOnlyList<Mt5GroupDto>> GetGroupsAsync(CancellationToken ct);
    Task<IReadOnlyList<Mt5AccountDto>> GetAccountsAsync(string? group, CancellationToken ct);
    Task<IReadOnlyList<Mt5DealDto>> GetDealsAsync(long login, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
    Task<IReadOnlyList<Mt5PositionDto>> GetPositionsAsync(long login, CancellationToken ct);
}
```

Bulk extras: `GetGroupDealsAsync`, `GetGroupPositionsAsync`. No `Send*`, no `Dealer*`, no `Order*`.

### 3.6 `NativeMt5BrokerConnector` Manager verbs (complete send-adjacent census)

Grep of `D:\Prop\src\Mt5` for `OrderSend|DealerSend|SendRequest|TradeRequest|NewOrder|35=D`: **zero hits**.

Measured CIMTManagerAPI usage in `NativeMt5BrokerConnector.cs`:

| API | Purpose |
|---|---|
| `SMTManagerAPIFactory.Initialize` / `CreateManager` | Load native DLL |
| `ProxySet` | Optional HTTP proxy for Achiever |
| `Connect(..., PUMP_MODE_GROUPS\|USERS\|POSITIONS)` then fallback `PUMP_MODE_NONE` | Manager **login** |
| `Disconnect` / `Dispose` | Teardown |
| `GroupRequestArray` / `GroupNext` / `GroupTotal` | Catalog read |
| `UserRequestArray` / `UserGetByGroup` / `UserLogins` / `UserRequestByLogins` / `UserAccountRequestArray` / `UserAccountGetByGroup` | Account read |
| `DealRequest` / `DealRequestByGroup` | Historical deal **pull** |
| `PositionRequest` / `PositionRequestByGroup` / `PositionGetByGroup` | Open-position **pull** |
| `PositionCreateArray` / `DealCreateArray` / `GroupCreateArray` / `UserCreateArray` | **Array allocators**, not trade create |

`Connect` is a manager session, not a client `OrderSend`. Pump modes are `GROUPS`, `USERS`, `POSITIONS` — no dealer / request / mail pump.

---

## 4. Shared DI caveat (honest, still not a send)

Both workers call `AddTraderIntelligence` (`D:\Prop\src\Infrastructure\DependencyInjection.cs`). That method (remeasured):

1. Requires real `MT5_PASSWORD` + `MT5_STARWAVEFX_PASSWORD` or throws (dummy/fake path disabled).
2. Sets `LiveRuntimeStatus.RealCopyEnabled` from `REAL_COPY_EXECUTION_ENABLED=="true"` (not hard-false).
3. Registers two `NativeMt5BrokerConnector` singletons.
4. Registers `DealIngestionService` + `ReconstructionScoringService` + `RiskEngine` + `CopyTradingService`.
5. **Also** `AddHostedService<LiveIngestHostedService>()`.
6. **Also** `AddHostedService<CTraderFixLogonHostedService>()`.
7. **Also** `AddHostedService<CopyTradingHostedService>()` — SHADOW intents only; `NewOrderSingleImplemented = false`.

So each worker **process** can run extra hosts the Worker class itself does not mention:

| Extra hosted service | Started by | What it can do | Trade / `35=D`? |
|---|---|---|---|
| `LiveIngestHostedService` | **both** workers + API | One-shot catalog + 90-day deal pull + score logins that have deals | **No** — same read/score path |
| `CTraderFixLogonHostedService` | **both** workers + API | If `CTRADER_FIX_PASSWORD` is a real secret: TCP+TLS `35=A` logon on 5211/5212, persist session rows, **log** `RealCopyArmed`. If password missing/`<SECRET>`: **return immediately**, no socket. | **No** — Logon only |
| `CopyTradingHostedService` | **both** workers + API | 20s tick: `GenerateShadowIntentsAsync` (qty × 0.05, risk, optional shadow fill) | **No** — `NewOrderSingleImplemented = false` |

This is a **process-shape** finding (shared composition root), not a send finding. Worker `appsettings` do not contain cTrader credentials, so the FIX logon host typically **skips**. Worker `appsettings` also do not contain MT5 passwords; if those env vars are absent, **the worker fails to start** (`InvalidOperationException`) rather than sending dummy trades.

---

## 5. Grep evidence (workers + called libraries)

Searched `apps/fix-worker`, `apps/mt5-worker`, `src/Mt5`, `src/Fix.CTrader`, `src/Application/Ingestion` (excluding `bin`/`obj` noise where possible).

| Pattern | Worker hosts | Verdict |
|---|---|---|
| `35=D` / `MsgType=D` / `(35, "D")` | **0** builders | No NewOrderSingle |
| `NewOrderSingle` | Strings + comments + option POCO + “still disabled” logs | No sender |
| `OrderSend` / `DealerSend` / `DealerBalance` / `TradeRequest` | **0** in `src/Mt5` | No MT5 trade API |
| `CIMTDealer` / `CIMTRequest` | **0** | No dealer sink |
| `ExecutionIntent` / `CopyIntent` send | Not referenced by either `Worker.cs` | Unused |
| `RealCopyExecutionEnabled` | Read for log only (fix-worker); forced `false` in DI | Not a wired choke |

---

## 6. What the workers **are** allowed to do (ingest / score)

| Process | Loop | Writes | Venue I/O |
|---|---|---|---|
| `TraderIntelligence.Mt5Worker` | 30 s | Groups, accounts, deals, positions, reconstructed trades, scores, shadow/outbox | MT5 Manager **read** (if env passwords present and Connect succeeds) |
| `TraderIntelligence.FixWorker` `Worker` | 15 s | `FixSessionStates.Status=Disconnected` | **None** |
| Shared `LiveIngestHostedService` (if host stays up) | once after 2 s | Same ingest/score tables | MT5 Manager **read** |
| Shared `CTraderFixLogonHostedService` | once | Session rows if DB type found | Optional FIX **`35=A`** only |

Neither process opens a destination cTrader order, neither mutates source MT5 users/orders/positions.

---

## 7. Classification vs prior swarm notes

| Prior note | Recycle? | Why |
|---|---|---|
| A08 “fix-worker is a 1 s template log loop; Fix.CTrader empty” | **Stale** | Current `Worker` is a 15 s DB stamp; Fix.CTrader has Logon + parser |
| D31 “always two FakeMt5BrokerConnector; DemoSeeder” | **Stale** | Current DI uses `LiveMt5Registration` + `BrokerCatalogSeed`; fake path **throws** |
| E002 “no NewOrderSingle sender” | **Still true** | Reconfirmed on 2026-08-18 worker + adapter read |
| C07 / D07 `SAFE_BY_ABSENCE` | **Still the correct safety class** | Absence of initiator, not a unit-tested refuse gate |

---

## 8. Product edits

**None.** Product tree untouched.

---

## 9. Residual risks (not send, still record)

1. **Shared composition root.** Running `fix-worker` also registers MT5 live ingest. Running `mt5-worker` also registers FIX logon. Neither is `35=D` / `OrderSend`, but it is more process surface than the `Worker.cs` files advertise.
2. **`if (real)` log is not a gate.** There is still no `GuardedNewOrderSingle`. Do not tick Architecture §70.12 as implemented.
3. **Manager `Connect` is live** when env secrets exist. That is **read ingest**, not trade. Do not describe it as “offline.”
4. **`PositionCreateArray` name** can be misread in a casual grep. It allocates a `CIMTPositionArray` for `PositionRequest*`. It does not create a position on the server.

---

## 10. DONE criteria for this pin

| Criterion | Status |
|---|---|
| `apps/fix-worker` `Program.cs` + `Worker.cs` read | Yes |
| `apps/mt5-worker` host + Worker + csproj + settings read | Yes |
| Call graph followed through ingestion, scoring, native connector, FIX session | Yes |
| Confirm no MT5 trade send | **Confirmed** |
| Confirm no FIX `35=D` | **Confirmed** |
| Ingest / score only (plus FIX session DB stamp / optional `35=A`) | **Confirmed** |
| Product edited | **No** |
| Permanent report | This file |

**PASS for the assigned no-send pin.** Workers cannot open a losing live destination position from this code.
