# C07 — mt5-worker / fix-worker Program + Worker: is real send off?

| Field | Value |
|---|---|
| Agent | C07 (workers send-off review) |
| Date | 2026-08-18 |
| Assigned question | Read `apps/mt5-worker` and `apps/fix-worker` `Program` / `Worker`. **Real send off?** |
| Artifact | `D:\Prop\reports\swarm\20260818\C07_workers_review.md` |
| Product source modified | **No** |
| Method | Full read of both hosts + DI + Fake connector + Fix.CTrader + seeder + dashboard health + SHA-256 of worker source. Grep of product `*.cs` (exclude vendor) for send/socket/QuickFIX tokens. |
| Supersedes for *this question* | A07 / A08 measured-state (those still describe the 1 s template loop). |
| Sibling (broader gap) | `B07_workers_gap.md` — same hashes; C07 answers send-off only. |

**Honesty rule:** a `LoggedOn` row is not a FIX session. A Fake `ConnectAsync` is not Achiever. Absence of `NewOrderSingle` is not an implemented flag gate. A log line that says “refuses NewOrderSingle” is theater if no send function exists.

---

## 0. Verdict

**YES. Real send is OFF.**

Neither worker can place a live order, send FIX `35=D` (`NewOrderSingle`), or call MT5 `SendTrade` / dealer send if started now.

| Question | Answer |
|---|---|
| Can `apps/mt5-worker` send a source or dest order? | **No.** Ingest + score only, against `FakeMt5BrokerConnector`. Connector has **no send verb**. |
| Can `apps/fix-worker` send `NewOrderSingle`? | **No.** No socket, no FIX engine, no message builder, no intent consumer. |
| Does flipping `CTrader:RealCopyExecutionEnabled=true` enable send? | **No.** Flag only changes a log line. Both branches of the TRADE status ternary are `LoggedOn`. |
| Does env `REAL_COPY_EXECUTION_ENABLED=true` enable send? | **No.** That name is **unread** by either worker. |
| Is send off because of an auditable gate (`MaySendNewOrderSingle`)? | **No.** Send is off by **absence of a send path** (`SAFE_BY_ABSENCE`). |
| Can an operator *believe* send/sessions are live? | **Yes (ops lie).** Seeder + fix-worker stamp QUOTE `ReadyForMarketData` and TRADE `LoggedOn` and bump `LastInboundAt`. |

Classification:

| Slice | Class |
|---|---|
| Live money / `35=D` / MT5 `SendTrade` today | **SAFE_BY_ABSENCE** |
| Implemented real-copy control | **MISSING** |
| mt5-worker as production collector | **FAIL** (demo Fake loop) |
| fix-worker as venue adapter | **FAIL** (session-row heartbeat) |
| Dashboard FIX/broker “healthy” if DB is shared | **UNSAFE** (forged status, not a send) |

Do **not** treat either process as a live copy engine. Do **not** add `35=D` to this heartbeat.

---

## 1. Inventory (source of truth; exclude `bin/` / `obj/`)

Hashes measured 2026-08-18 (SHA-256). Identical to B07.

### 1.1 `D:\Prop\apps\mt5-worker`

| Path | Bytes | SHA-256 | Role |
|---|---:|---|---|
| `Program.cs` | 859 | `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` | Host + `EnsureCreated` + `DemoSeeder` + one hosted service |
| `Worker.cs` | 1882 | `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` | 30 s Fake ingest + reconstruct + score |
| `TraderIntelligence.Mt5Worker.csproj` | 840 | `E0321028B0E12EEFE97A9BE2D0A08E8E8F89B819CCA4403D301B76A90C56B91C` | net8 Worker; refs Domain, Application, Infrastructure, Mt5 |
| `appsettings.json` | 137 | `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` | Logging only |
| `appsettings.Development.json` | 137 | same | Logging only |
| `Properties/launchSettings.json` | 296 | `8E2A7548E3EBFF12FDB3E078E06ADA944E3ABB83BA8F9128746542CAA8AA3E36` | `DOTNET_ENVIRONMENT=Development` only |

### 1.2 `D:\Prop\apps\fix-worker`

| Path | Bytes | SHA-256 | Role |
|---|---:|---|---|
| `Program.cs` | 859 | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` | Same boot as mt5-worker (namespace only) |
| `Worker.cs` | 1971 | `B48033A5A13C56DB747D3C9F0B94E36CB8DC2866FBCF3789A62C3D7B318B0D48` | 15 s `fix_sessions` heartbeat |
| `TraderIntelligence.FixWorker.csproj` | 856 | `D7572CBFF273089587D4A68B2A95EB1933C2DF388177F8665BE7C3CC338B5DB4` | net8 Worker; refs Domain, Application, Infrastructure, Fix.CTrader |
| `appsettings.json` | 137 | `AB16B7B7…FF33` | **Byte-identical** to mt5-worker logging |
| `appsettings.Development.json` | 137 | same | Logging only |
| `Properties/launchSettings.json` | 296 | `25A750D823B04F229FBC49A690F997E969276FFB1A5E5F4EF198DF7DE7CBEF9B` | Dev env only |

Neither host commits `CTrader:*`, `REAL_COPY_*`, `MT5_*`, or a connection string. UserSecrets IDs exist and are unused.

---

## 2. What `Program.cs` actually starts

Both hosts are the same composition:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTraderIntelligence(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DemoSeeder.SeedAsync(/* store + scoring */, CancellationToken.None);
}
host.Run();
```

Evidence that this is **not** a send path:

1. **No FIX initiator / no Manager session registered.** `AddTraderIntelligence` always installs two `FakeMt5BrokerConnector` singletons (`DemoBrokerFactory.CreateDefault()`). No `CTraderFixOptions` bind. No `Configure<CTraderFixOptions>`. No `HttpClient`. No native DLL.
2. **Default store is in-process memory.** Worker `appsettings` have no connection string → `UseInMemoryDatabase("trader-intelligence")` unless an operator injects `ConnectionStrings:TraderIntelligence` / `DATABASE_URL`.
3. **Seeder writes a fake venue picture** (QUOTE `ReadyForMarketData`, TRADE `LoggedOn`, static XAU quote `2399.45`/`2399.85`) but never opens `live-us-eqx-01.p.c-trader.com`.
4. **No hosted execution service.** One `Worker` each. No outbox claimer, no lease, no risk-to-send loop.

`fix-worker/Program.cs` references `Fix.CTrader` at compile time via the csproj, but **does not `using` any Fix.CTrader type**. The adapter assembly is an unused project reference.

---

## 3. mt5-worker `Worker` — read path only

```17:44:D:\Prop\apps\mt5-worker\Worker.cs
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MT5 ingestion worker started. Execution copy is not performed here.");
        while (!stoppingToken.IsCancellationRequested)
        {
            // SyncBrokerAsync(Achiever) + SyncBrokerAsync(StarwaveFx)
            // RebuildTraderAsync for logins {10001,10002,10003,99001}
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
```

### 3.1 Call graph (no send)

```text
Worker.ExecuteAsync
  → DealIngestionService.SyncBrokerAsync
       → IBrokerRegistry.Get(code)
       → IMt5BrokerConnector.ConnectAsync        // Fake: _connected = true
       → GetGroups / GetAccounts(null)
       → GetDeals(login, from, to)               // in-memory list filter
       → GetPositions(login)                     // empty fixture list
       → ITradingStore upserts
  → ReconstructionScoringService.RebuildTraderAsync
       → LoadDeals → TradeReconstructor → BaselineScorer → UpsertScore
```

`IMt5BrokerConnector` (`src/Application/Contracts/Mt5Contracts.cs`) methods:

`ConnectAsync`, `DisconnectAsync`, `IsConnectedAsync`, `GetGroupsAsync`, `GetAccountsAsync`, `GetDealsAsync`, `GetPositionsAsync`.

**No** `Send*`, `Place*`, `NewOrder*`, `Dealer*`, `CreateUser`.

The unused twin port `IBrokerConnector` is also read/subscribe only (`SubscribeEventsAsync`). Zero implementers.

### 3.2 Fake connect is not a venue

`FakeMt5BrokerConnector.ConnectAsync`:

```30:34:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
    public Task ConnectAsync(CancellationToken ct)
    {
        _connected = true;
        return Task.CompletedTask;
    }
```

No DNS, no `57.128.141.65:443`, no Manager login `2027` / `9904`, no `MT5APIManager64.dll`, no HTTP to `mt5-sdk`. `Mt5BrokerOptions` is never bound.

C++ `mt5-sdk` `SendTrade` / `CacheExecutedDeal` are **not referenced** by this host. Product `apps/` + `src/` C# grep for `SendTrade` / `DealerSend` / `OrderSend`: **0 hits**.

### 3.3 What this worker is allowed to do (and does)

| Action | Happens? | Live send? |
|---|---|---|
| Upsert fixture groups/accounts/deals | Yes (seeder window; worker 30-day window misses June 2026 fixtures) | No |
| Rebuild scores for 4 hard-coded logins | Yes | No |
| Write `CopyIntent` / `ExecutionIntent` | **No** | — |
| Call `RiskEngine` / `ShadowCopyEngine` | **No** | — |
| Outbox / checkpoint | **No** | — |

The log line “Execution copy is not performed here” is **true**.

---

## 4. fix-worker `Worker` — heartbeat, not a session

```19:47:D:\Prop\apps\fix-worker\Worker.cs
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);

        while (!stoppingToken.IsCancellationRequested)
        {
            // load QUOTE + TRADE FixSessionState rows
            quote.LastInboundAt = DateTimeOffset.UtcNow;
            quote.Status = FixSessionStatus.ReadyForMarketData;
            trade.LastInboundAt = DateTimeOffset.UtcNow;
            trade.Status = real ? FixSessionStatus.LoggedOn : FixSessionStatus.LoggedOn;
            await db.SaveChangesAsync(stoppingToken);
            if (real)
                _logger.LogWarning("Real copy is enabled in config, but worker still refuses NewOrderSingle until risk/reconciliation gates pass.");
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
```

### 4.1 What is *not* on this stack

| Required for a real send | Present? |
|---|---|
| TCP/TLS to `live-us-eqx-01.p.c-trader.com:5211/5212` | **No** |
| QuickFIX/n initiator (`QuickFIXn.Core` / `QuickFIXn.FIX44` 1.14.1) | **No** — `Fix.CTrader.csproj` has **zero** package references (current file; B05’s `QuickFix.Net` 1.8.0 pin is **gone**) |
| `CTraderQuoteSession` / `CTraderTradeSession` | **No** |
| Logon `35=A` | **No** |
| Persist-before-send `ExecutionIntent` + `ClOrdId` | Tables exist; worker never reads/writes them |
| `ClOrdIdFactory.Next` | Unused by this host |
| `ExecutionOrderStateMachine.AfterSendAttempt` | Unused |
| `RiskEngine.Evaluate` → `AllowFixSend` | Unused |
| `FixSessionOwnership` lease / fencing | Unused |
| Kill switch observer | Seeded `None`; unread |
| Dest-family outbox claim (`execution.approved`) | **No** |
| `35=D` builder (tag 11/21/38/40/54/55/60…) | **No** |

`Fix.CTrader` on disk today (product `.cs` only):

| File | Role vs send |
|---|---|
| `Configuration/CTraderFixOptions.cs` | `RealCopyExecutionEnabled` default **false**. **Never bound** on this host. |
| `Parsing/FixMessageParser.cs` | Pipe/SOH checksum helper for tests. Not a socket. |
| `Testing/FixSimulationHarness.cs` | Builds **inbound-looking strings** (`35=8` ER, `35=A` logon). Does not transmit. |
| `Services/FixSessionOwnership.cs` | In-memory lock. Worker never constructs it. |

Worker usings: `TraderIntelligence.Domain.Enums` + `TraderIntelligence.Infrastructure.Persistence` only. **Zero** Fix.CTrader types.

### 4.2 The flag is not a gate

| Knob | Who reads it | Effect if true |
|---|---|---|
| `CTrader:RealCopyExecutionEnabled` | fix-worker `GetValue(..., false)` | Log warning. TRADE status ternary is a **no-op** (`LoggedOn` both sides). |
| `CTraderFixOptions.RealCopyExecutionEnabled` | nobody (unbound) | — |
| `REAL_COPY_EXECUTION_ENABLED` env | nobody in these hosts | — |
| API `apps/api/appsettings.json` → `"RealCopyExecutionEnabled": false` | API process only | Not this worker’s config |

There is no `MaySendNewOrderSingle(...)` conjunction (flag ∧ risk healthy ∧ reconciled ∧ lease ∧ kill-switch none). Domain `RiskEngine` can compute `AllowFixSend`; **no worker calls it**.

### 4.3 Why this is still “send off” even when `real==true`

If an operator sets `CTrader__RealCopyExecutionEnabled=true`:

1. Worker logs a warning every 15 s.
2. TRADE row stays `LoggedOn` (same as `false`).
3. **No additional method runs.** No `if (real) session.Send(...)`.

Fail-closed is implemented as **dead code paths**, not a refuse-to-compile / refuse-to-send function.

---

## 5. Grep evidence (product C#, vendor excluded)

Search roots: `D:\Prop\apps`, `D:\Prop\src` (`*.cs`). Not `mt5-sdk/vendor`.

| Needle | Product hits | Notes |
|---|---|---|
| `NewOrderSingle` | fix-worker log strings; `CTraderFixOptions` comment; `ExecutionOrderStateMachine.MayRetryNewOrderSingle` | **No builder, no send** |
| `35=D` / `MsgType=D` | **0** | |
| `TcpClient` / `SslStream` / `SocketInitiator` / `QuickFix` | **0** in product | Vendor SDK examples only |
| `SendTrade` / `OrderSend` / `DealerSend` | **0** | |
| `CopyIntents` / `ExecutionIntents` in `apps/` | **0** | |
| `AllowFixSend` | `RiskEngine` + unit tests only | |
| `AddHostedService` | the two `Worker`s only | |

---

## 6. Adjacent lies that are **not** send (do not confuse)

These do **not** move money. They **will** fool an operator or a dashboard if a shared Postgres is configured.

| Lie | Source | Dashboard effect |
|---|---|---|
| QUOTE status forced `ReadyForMarketData` every 15 s | fix-worker | `EfDashboardQueries` treats that as quote-healthy |
| TRADE status forced `LoggedOn` every 15 s | fix-worker | trade-healthy if status ∈ {LoggedOn, Reconciling, ReadyForExecution} |
| `LastInboundAt = UtcNow` with no inbound FIX | fix-worker | session looks live |
| Seeder plants the same statuses + host `live-us-eqx-01.p.c-trader.com` + SenderCompId `live.pepperstone.1369850` | both `Program.cs` | Looks like a real Pepperstone session |
| `GetBrokers` `Connected = true` | API queries, not worker | Broker tile green without `IsConnectedAsync` |
| Fake `ConnectAsync` | mt5-worker ingest | Log implies sync; no Manager |

Default launch: each process has its **own** in-memory DB, so the API may **not** see the heartbeat. The moment `DATABASE_URL` points at compose Postgres (`docker-compose.yml` has postgres + redis + api; **workers are not in compose**), the lie becomes visible.

`docker-compose.yml` comment: “Native MT5 Manager DLL workers stay on Windows hosts.” Correct isolation for native DLL. It does not make these C# workers send.

---

## 7. Scorecard for the assigned question

| Check | Result |
|---|---|
| Real FIX send off? | **YES — OFF** |
| Real MT5 dealer/send off? | **YES — OFF** |
| Real copy execution feature-flag encoded as a send gate? | **NO** (unread / log-only) |
| Can config turn send on today? | **NO** |
| Persist-before-send? | N/A (nothing to persist-then-send) |
| TRADE single-owner lease before send? | N/A (no TRADE socket) |
| Phase 8 live copy possible by starting these processes? | **NO** |
| Phase 4 QUOTE actually connected? | **NO** |
| Phase 7 TRADE recon actually running? | **NO** |
| Safe to call this “flag-gated execution”? | **NO** — call it **safe by absence** |

**A07/A08 are stale** (template `Task.Delay(1000)`). Current hosts do more, but **none of the extra work is a send**.

---

## 8. Risks if someone “enables real send” in these files

| ID | Risk | Severity |
|---|---|---|
| S1 | Add `35=D` inside the 15 s loop when `real` is true | **Critical** — no lease, no recon, no persist-before-send, no ClOrdID store |
| S2 | Trust dashboard LoggedOn as “ready to copy” | **High** — status is forged |
| S3 | Point Fake DI at a future real connector and assume send is still off | **High** — collector must stay read-only; send must never live on mt5-worker |
| S4 | Treat `if (real) LogWarning(...)` as the gate | **High** — warning is not a refuse |
| S5 | Bind API `CTrader` block (host + account 1369850) onto fix-worker and assume SSL session exists | **Medium** — options exist; session objects do not |

Correct order (audit only — **not implemented here**): implement `MaySendNewOrderSingle` + unit-test refuse **before** any initiator; keep default `false`; do not grow `Worker.cs`.

---

## 9. Files read (not modified)

- `D:\Prop\apps\mt5-worker\{Program.cs,Worker.cs,TraderIntelligence.Mt5Worker.csproj,appsettings.json,appsettings.Development.json,Properties\launchSettings.json}`
- `D:\Prop\apps\fix-worker\{Program.cs,Worker.cs,TraderIntelligence.FixWorker.csproj,appsettings.json,appsettings.Development.json,Properties\launchSettings.json}`
- `D:\Prop\apps\api\appsettings.json` (flag location only)
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs`
- `D:\Prop\src\Mt5\Connectors\{FakeMt5BrokerConnector.cs,IBrokerConnector.cs}`
- `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs`
- `D:\Prop\src\Fix.CTrader\{Configuration\CTraderFixOptions.cs,Parsing\FixMessageParser.cs,Services\FixSessionOwnership.cs,Testing\FixSimulationHarness.cs,TraderIntelligence.Fix.CTrader.csproj}`
- `D:\Prop\src\Domain\Execution\{ExecutionOrderStateMachine.cs,ClOrdIdFactory.cs}`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Domain\Entities\{CopyIntent.cs,ExecutionIntent.cs,FixSessionState.cs}`
- `D:\Prop\docker-compose.yml`
- Siblings: `A07`, `A08`, `A64`, `B04`, `B05`, `B07`

---

## 10. Bottom line

**Real send is off.**

`mt5-worker` is a 30-second Fake ingest/rescore timer that **cannot** talk to Achiever, StarwaveFX, or any execution venue.

`fix-worker` is a 15-second `fix_sessions` forger that **cannot** speak FIX 4.4 and **cannot** emit `NewOrderSingle` even when `CTrader:RealCopyExecutionEnabled` is true.

That matches the *spirit* of `REAL_COPY_EXECUTION_ENABLED=false`. It is **not** a production control. The remaining hazard is **ops theater** (LoggedOn / ReadyForMarketData / LastInboundAt), not a wire order.

**End of C07.** Product source was not modified.
