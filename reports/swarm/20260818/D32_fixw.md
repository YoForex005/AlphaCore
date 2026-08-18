# D32 — fix-worker `Worker.cs` does **not** stamp `LoggedOn` (no socket either)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D32_fixw.md` |
| Agent | D32 (fix-worker LoggedOn / socket reconfirm) |
| Date | 2026-08-18 |
| Assigned | Read `apps/fix-worker/Worker.cs`. Does it stamp `LoggedOn` without a socket? Write this file. |
| Product source modified | **No** |
| Test source modified | **No** |
| Snapshot | `Worker.cs` LastWriteTimeUtc `2026-08-18T08:04:48.7473622Z` |
| SHA-256 | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` |
| Size / lines | 2093 bytes / 51 lines |

**Method:** `read_file` of `D:\Prop\apps\fix-worker\Worker.cs` (twice; file changed mid-session), `Program.cs`, `.csproj`, `appsettings.json`, `launchSettings.json`; `read_file` of `FixSessionStatus`, `DemoSeeder` session rows, `EfDashboardQueries` health bits; `grep`/`Select-String` for `LoggedOn`, `TcpClient`, `Socket`, `SslStream`, `QuickFIX`, `Initiator` under `apps/fix-worker` and `src/Fix.CTrader`; SHA-256 via `Get-FileHash`. Product source not edited.

**Stale-vs-this-file:** A08 / B05 / B07 / C07 / C13 / C14 / C19 / C43 / C46 / A101 describe TRADE `Status = LoggedOn` every 15 s. That assignment is **gone** from the on-disk worker (hash above). Use this report for `Worker.cs` status writes. Those older files remain valid for “no live FIX socket” and dashboard enum-as-health.

---

## Verdict (honest)

**No. Current `Worker.cs` does not stamp `FixSessionStatus.LoggedOn`.**

It also does **not** open a socket. There is no TCP, TLS, QuickFIX/n initiator, or inbound `35=A`. The 15 s loop **overwrites** both QUOTE and TRADE rows to `Disconnected` and writes a `LastError` that admits there is no live socket.

| Question | Measured answer |
|---|---|
| Does `Worker.cs` assign `LoggedOn`? | **No.** Zero matches for `LoggedOn` in the file. |
| Does it assign `ReadyForMarketData`? | **No.** |
| What status does it write? | `FixSessionStatus.Disconnected` for QUOTE **and** TRADE. |
| Does `CTrader:RealCopyExecutionEnabled` change that status? | **No.** Flag is read only for a log line. Status path does not branch. |
| Is there a socket / TLS / initiator? | **No.** No `TcpClient`, `Socket`, `SslStream`, `QuickFIX`, `ConnectAsync`. |
| Does it still write the DB every 15 s? | **Yes.** `UpdatedAt = UtcNow` + `SaveChangesAsync`. Not a heartbeat. |
| Does it still forge `LastInboundAt`? | **No** (that forge is gone). |
| Is TRADE “logged on” without a socket? | **Not from this worker.** Seeder also seeds `Disconnected` (same day). Dashboard still *would* treat `LoggedOn` as healthy **if** some other writer put that enum back. |

Classification of the worker as a FIX venue: **MISSING** (no session object).  
Classification of the old “LoggedOn every 15 s” ops-lie **in this file**: **REMOVED**.  
Classification of live send: still **SAFE_BY_ABSENCE** (no `35=D` builder, no initiator).

Do **not** treat `Disconnected` stamps as proof of a real disconnect handshake (`35=5` / TCP drop). The process never connected.

---

## 1. `Worker.cs` as measured

File: `D:\Prop\apps\fix-worker\Worker.cs`

```1:51:D:\Prop\apps\fix-worker\Worker.cs
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Infrastructure.Persistence;

namespace TraderIntelligence.FixWorker;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _config;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopes, IConfiguration config)
    {
        _logger = logger;
        _scopes = scopes;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
            var quote = db.FixSessionStates.SingleOrDefault(s => s.Qualifier == FixSessionQualifier.Quote);
            if (quote is not null)
            {
                quote.UpdatedAt = DateTimeOffset.UtcNow;
                quote.Status = FixSessionStatus.Disconnected;
                quote.LastError = "No live QUOTE socket. Simulator/demo only.";
            }

            var trade = db.FixSessionStates.SingleOrDefault(s => s.Qualifier == FixSessionQualifier.Trade);
            if (trade is not null)
            {
                trade.UpdatedAt = DateTimeOffset.UtcNow;
                trade.Status = FixSessionStatus.Disconnected;
                trade.LastError = "No live TRADE socket. NewOrderSingle remains off.";
            }

            await db.SaveChangesAsync(stoppingToken);
            if (real)
                _logger.LogWarning("Real copy is enabled in config, but worker still refuses NewOrderSingle until risk/reconciliation gates pass.");

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}
```

### 1.1 What the loop writes

| Field | QUOTE | TRADE |
|---|---|---|
| `Status` | `Disconnected` | `Disconnected` |
| `LastError` | `"No live QUOTE socket. Simulator/demo only."` | `"No live TRADE socket. NewOrderSingle remains off."` |
| `UpdatedAt` | `DateTimeOffset.UtcNow` | `DateTimeOffset.UtcNow` |
| `LastInboundAt` | untouched | untouched |
| `LastOutboundAt` | untouched | untouched |
| `InboundSeq` / `OutboundSeq` | untouched | untouched |
| `OwnerHeld` / `OwnerInstance` | untouched | untouched |
| Socket / TLS / `35=A` | none | none |

`real` is bound from `CTrader:RealCopyExecutionEnabled` (default `false`). `apps/fix-worker/appsettings.json` has **logging only** — that key is absent. Env name `REAL_COPY_EXECUTION_ENABLED` is still **unread**. When `real==true` the worker logs a refusal; it still writes `Disconnected`. There is no send function to refuse.

### 1.2 Socket / initiator inventory (this process)

`apps/fix-worker` product C# (`Program.cs` + `Worker.cs`):

| API / type | Present? |
|---|---|
| `System.Net.Sockets.Socket` / `TcpClient` | **No** |
| `SslStream` | **No** |
| `QuickFIX` / `IInitiator` / `SessionID` | **No** |
| `CTraderFixOptions` usage | **No** (`Fix.CTrader` project is referenced, unused by `Worker`) |
| `FixSessionOwnership` | **No** |
| `FixMessageParser` / `FixSimulationHarness` | **No** (not called) |
| Any `Connect` / `Logon` send | **No** |

`src/Fix.CTrader` has a pipe-delimited string factory (`FixSimulationHarness`) and an in-memory lock. **No** `TcpClient` / `SslStream` / QuickFIX package. That is not a socket.

`Program.cs` still: `AddTraderIntelligence` → `EnsureCreatedAsync` → `DemoSeeder.SeedAsync` → `AddHostedService<Worker>`. Host start is not a venue Logon.

---

## 2. Adjacent writers (not `Worker.cs`, still relevant)

### 2.1 `DemoSeeder` (startup, first empty DB)

`D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` now inserts **both** session rows as `Disconnected` with `LastError` “No live … socket”:

- QUOTE L73: `Status = FixSessionStatus.Disconnected`, port 5211, host `live-us-eqx-01.p.c-trader.com`
- TRADE L91: `Status = FixSessionStatus.Disconnected`, port 5212, same host

Seeder still plants a live hostname and a static XAU quote (`2399.45` / `2399.85`, `VenueInstrumentId = null`). That is **not** `LoggedOn`. It is still not a socket.

### 2.2 Dashboard still *interprets* `LoggedOn` as healthy

`D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`:

- Overview `QuoteHealthy`: status ∈ `{LoggedOn, ReadyForMarketData, ReadyForExecution}`
- Overview `TradeHealthy`: status ∈ `{LoggedOn, Reconciling, ReadyForExecution}`
- `FixSessionDto.Connected`: status ∉ `{Disconnected, Error}`
- `FixSessionDto.LoggedOn`: status ∈ `{LoggedOn, ReadyForMarketData, ReadyForExecution, Reconciling}`

With the current worker+seeder statuses, those bits evaluate **false**. The dashboard will go green again the moment **any** writer puts `LoggedOn` (or Ready*) back on the row — including a future worker regression. Health is still an enum, not a session object.

---

## 3. Contrast with earlier swarm (do not mix)

This agent’s **first** read in-session still saw the old body:

```csharp
quote.LastInboundAt = DateTimeOffset.UtcNow;
quote.Status = FixSessionStatus.ReadyForMarketData;
trade.LastInboundAt = DateTimeOffset.UtcNow;
trade.Status = real ? FixSessionStatus.LoggedOn : FixSessionStatus.LoggedOn;
```

That is the lie documented in C43 / B07 / C07 / A101. A concurrent edit (not this agent; product source was **not** modified here) replaced it with the `Disconnected` + `LastError` loop hashed above.

| Claim in A08/B07/C07/C43 | Current `Worker.cs` |
|---|---|
| TRADE `LoggedOn` both sides of `real` ternary | **Absent.** No `LoggedOn` token. |
| QUOTE `ReadyForMarketData` | **Absent.** Writes `Disconnected`. |
| `LastInboundAt = UtcNow` every 15 s | **Absent.** Only `UpdatedAt`. |
| No socket | **Still true.** |

If an orchestrator is asking “does the worker still paint TRADE logged-on so the dashboard lies?”, the measured answer **today** is **no, not from `Worker.cs`**.

---

## 4. Residual hazards (honest, not a PASS)

1. **No venue.** `Disconnected` is a clock write, not a measured TCP state. Phase 4 Logon is still **0**.
2. **Smash-from-above.** If a later process owns a real session object and persists `LoggedOn` / seq, this worker will force `Disconnected` every 15 s and clobber `LastError`. That is the inverse lie: it can hide a real session as well as it used to invent one.
3. **`real` flag is theater.** It does not gate a send path because no send path exists. Log line only.
4. **Dashboard contract unchanged.** Enum-as-health remains. Do not add `LoggedOn` back without an `IFixSession` (or QuickFIX session) that actually received `35=A`.
5. **`UpdatedAt` tick** is not inbound traffic. Do not wire freshness to `UpdatedAt`.

---

## 5. Direct answer

**Does fix-worker `Worker.cs` stamp `LoggedOn` without a socket?**

**No.** It stamps `Disconnected` without a socket. `LoggedOn` is not assigned in this file (SHA-256 `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2`).
