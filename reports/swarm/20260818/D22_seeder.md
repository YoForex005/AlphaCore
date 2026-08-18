# D22 — `DemoSeeder` writes `LoggedOn` without a FIX session

| Field | Value |
|---|---|
| Agent | D22 (seeder honesty / anti-greenwash) |
| Date | 2026-08-18 |
| Assigned | Read `DemoSeeder.cs`. Note `LoggedOn` without FIX. Write this report. Do not modify product source. |
| Primary SUT | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` |
| Lines / bytes / SHA-256 | **138** / **4942** / `139D8F872DC473F0C5381AF2393BDBBE60E1D9A2A5179DD1D1737E04CCC00BEF` |
| Product source modified | **No.** This report is the only write. |
| Method | Full read of `DemoSeeder.cs`. Cross-read `FixSessionState`, `FixSessionStatus`, `FixSessionQualifier`, `CTraderFixOptions`, `EfDashboardQueries`, `FixSessionDto`, `apps/fix-worker/Worker.cs` + `Program.cs`, `apps/api/Program.cs`, `apps/mt5-worker/Program.cs`, `SeedingAndStoreTests`, `FakeMt5BrokerConnector.DemoBrokerFactory`, `TraderDbContext`. Grep product `*.cs` for `FixSessionStatus` and `DemoSeeder`. Re-hash seeder (`Get-FileHash SHA256`). |
| Binding law | Architecture §§25–26, 61, 70; `A25` §2.3 / §3.6; `A101` item 1; `C43` (live Logon **NOT PROVEN**) |
| Siblings | `C05` (DI vs seeder), `C16` (InMemory seed test), `C43` (no live FIX), `B05` / `C07` (worker forge), `A90` §1.5 (do not treat seeder as test fixture) |

**Honesty rule (same as C43):** a `fix_sessions.Status = LoggedOn` row is **not** a FIX session. Seeding the live Pepperstone host, port, and `SenderCompId` is **not** a TLS handshake. `LastInboundAt = UtcNow` at seed time is **not** a Heartbeat. A dashboard that reads those columns and paints green is **anti-evidence**.

---

## 0. Verdict

**FORGED. `DemoSeeder` persists TRADE `FixSessionStatus.LoggedOn` (and QUOTE `ReadyForMarketData`) with zero FIX.**

There is no call into `TraderIntelligence.Fix.CTrader`. There is no `using` for that assembly. There is no `TcpClient`, `SslStream`, QuickFIX `IInitiator`, `35=A`, Heartbeat, SecurityList, market-data subscribe, or TRADE reconcile. The seeder **constructs two `FixSessionState` POCOs and `AddRange`s them**. That is the entire “session.”

| Claim the row invites | Measured truth |
|---|---|
| TRADE is `LoggedOn` | **Lie.** Line 90 assigns the enum. No socket. |
| QUOTE is `ReadyForMarketData` | **Lie.** Line 73 assigns a **later** FSM state than Logon. No MD, no SecurityList, no quote from the venue. |
| Host `live-us-eqx-01.p.c-trader.com` means we connected | **Config literal only.** Same string as `CTraderFixOptions.Host` default. Unbound by the seeder. |
| Seq 1/1 + `LastInboundAt = now` means the session is live | **Seed clock.** Both timestamps are `DateTimeOffset.UtcNow` at first empty-broker boot. |
| Dashboard `TradeHealthy` / `QuoteHealthy` after API start | **True bits from a false enum.** `EfDashboardQueries` treats `LoggedOn` / `ReadyForMarketData` as healthy. |
| Live copy is on | **No.** `FixSessionDto.ExecutionEnabled` is hardcoded `false`. `ReadyForExecution` is never seeded. Send remains **SAFE_BY_ABSENCE** (`C07`). That does **not** make the status honest. |

Classification:

| Slice | Class |
|---|---|
| TRADE seed status | **FORGED `LoggedOn`** — no FIX |
| QUOTE seed status | **FORGED `ReadyForMarketData`** — no FIX (worse than `LoggedOn`; implies MD) |
| Seeded dest quote (`2399.45` / `2399.85`) | **FORGED book** — `VenueInstrumentId = null` (honest id); prices are invented |
| Live host / SenderCompId / ports 5211–5212 | **LIVE IDENTIFIERS IN A DEMO ROW** |
| Actual TLS / `35=A` | **ABSENT** (confirm `C43`) |
| Fake-MT5 ingest + score rebuild | **REAL ORCHESTRATION of a Fake tape** (not live Manager API) |
| Safe to treat venue as connected | **No** |
| Safe to enable `REAL_COPY_EXECUTION_ENABLED` | **No** |
| Product source edited by D22 | **No** |

Do **not** tick `A101` item 1 from this file. Do **not** treat a green `SeedingAndStoreTests` run as FIX proof (`C16`). A successor may only flip the forge flag after the seeder writes `Disconnected` (or omits session rows) **and** a session object is the only writer of `LoggedOn`.

---

## 1. File surface (measured)

```14:23:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
public static class DemoSeeder
{
    public static async Task SeedAsync(
        TraderDbContext db,
        ITradingStore store,
        ReconstructionScoringService scoring,
        CancellationToken ct)
    {
        if (await db.Brokers.AnyAsync(ct))
            return;
```

| Item | Value |
|---|---|
| Kind | `public static class` — **not** in DI (`C05`) |
| Guard | `if (await db.Brokers.AnyAsync(ct)) return;` — first writer wins; **untested** (`C16`) |
| Clock | `var now = DateTimeOffset.UtcNow;` used for brokers, both FIX rows, dest quote, kill-switch |
| Hardcoded broker ids | `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1` Achiever; `…aaa2` StarwaveFX — **not** `DeterministicGuid` (`B04`) |
| Usings that touch FIX | `TraderIntelligence.Domain.Entities` + `Domain.Enums` only |
| Usings that touch `Fix.CTrader` | **none** |
| Unused usings | `Domain.Reconstruction`, `Domain.Scoring` (noise; scoring is injected) |

The seeder is a **dev bootstrap**: catalog rows → fake-broker ingest → score four logins. The FIX block is catalog theatre.

---

## 2. Finding: `LoggedOn` without FIX (primary)

The TRADE row, complete:

```86:101:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
            new FixSessionState
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2"),
                Qualifier = FixSessionQualifier.Trade,
                Status = FixSessionStatus.LoggedOn,
                Host = "live-us-eqx-01.p.c-trader.com",
                Port = 5212,
                SenderCompId = "live.pepperstone.1369850",
                TargetCompId = "cServer",
                TargetSubId = "TRADE",
                InboundSeq = 1,
                OutboundSeq = 1,
                LastInboundAt = now,
                LastOutboundAt = now,
                UpdatedAt = now
            });
```

The QUOTE sibling (same host, port **5211**, `TargetSubId = "QUOTE"`, `SenderSubId = null`) sets `Status = FixSessionStatus.ReadyForMarketData` (line 73). That is **two steps past** `LoggedOn` in the enum:

```3:13:D:\Prop\src\Domain\Enums\FixSessionStatus.cs
public enum FixSessionStatus
{
    Disconnected = 0,
    Connecting = 1,
    LogonSent = 2,
    LoggedOn = 3,
    Reconciling = 4,
    ReadyForMarketData = 5,
    ReadyForExecution = 6,
    LogoutSent = 7,
    Error = 8
}
```

`A25` §2.3: `READY_FOR_EXECUTION` is **not** implied by Logon; QUOTE `ReadyForMarketData` is not implied by a POCO either. The seeder never even pretends to send Logon. It **skips** `Disconnected → Connecting → LogonSent` and writes a terminal-looking healthy state.

What is **not** set (entity defaults):

| `FixSessionState` field | After seed | Meaning |
|---|---|---|
| `ReconnectCount` | `0` | never connected, so never reconnected — accidentally honest |
| `LastError` | `null` | no transport fail recorded |
| `OwnerHeld` | `false` | no TRADE lease (`A46`) |
| `OwnerInstance` | `null` | no owner |
| Logon result / inbound `35` / tag `58` | **columns do not exist** | cannot store `LOGON_OK` (`C43` §1) |

Grep of `DemoSeeder.cs` for socket / FIX verbs: **zero** hits for `TcpClient`, `SslStream`, `Logon`, `35=`, `QuickFix`, `Initiator`, `Heartbeat`, `SecurityList`, `MarketData`. The only FIX-shaped tokens are enum members and CompID strings.

---

## 3. Seeded FIX rows (inventory)

| Field | QUOTE (`…ccc1`) | TRADE (`…ccc2`) |
|---|---|---|
| `Qualifier` | `Quote` | `Trade` |
| **`Status`** | **`ReadyForMarketData`** | **`LoggedOn`** |
| `Host` | `live-us-eqx-01.p.c-trader.com` | same |
| `Port` | **5211** (TLS quote) | **5212** (TLS trade) |
| `SenderCompId` | `live.pepperstone.1369850` | same |
| `TargetCompId` | `cServer` (issued-form case; `C21`/`B27`) | same |
| `TargetSubId` | `QUOTE` | `TRADE` |
| `SenderSubId` | explicit `null` | unset → `null` |
| `InboundSeq` / `OutboundSeq` | `1` / `1` | `1` / `1` |
| `LastInboundAt` / `LastOutboundAt` | seed `now` | seed `now` |

These literals **duplicate** `CTraderFixOptions` defaults (`Host`, ports, `SenderCompId`, `TargetCompId`, sub IDs). The options type is **not referenced**. Changing the POCO defaults will **not** change already-seeded rows; the seeder will not re-run if `Brokers` is non-empty.

Plain-text ports `5201`/`5202` are **not** seeded. Production TLS ports are. That makes the lie look more production-shaped, not less.

---

## 4. Downstream lie (how the enum becomes a green dashboard)

### 4.1 Overview bits

`EfDashboardQueries.GetOverviewAsync`:

```40:41:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            quote?.Status is FixSessionStatus.LoggedOn or FixSessionStatus.ReadyForMarketData or FixSessionStatus.ReadyForExecution,
            trade?.Status is FixSessionStatus.LoggedOn or FixSessionStatus.Reconciling or FixSessionStatus.ReadyForExecution,
```

After a first-run seed, **before** `fix-worker` even ticks:

| DTO field | Seeded input | Result |
|---|---|---|
| `QuoteHealthy` | QUOTE `ReadyForMarketData` | **true** |
| `TradeHealthy` | TRADE `LoggedOn` | **true** |
| `RealCopyEnabled` | hardcoded `false` | honest accident |
| `Mt5Healthy` | `brokers > 0` | **true** (two Fake-backed broker **rows**, not live Manager) |

### 4.2 FIX page DTO

`GetFixSessionsAsync` maps **both** rows:

| `FixSessionDto` field | Formula | After seed |
|---|---|---|
| `Connected` | status ∉ {Disconnected, Error} | **true** |
| `LoggedOn` | status ∈ {LoggedOn, ReadyForMarketData, ReadyForExecution, Reconciling} | **true** (both) |
| `Status` | `ToString()` | `"ReadyForMarketData"` / `"LoggedOn"` |
| `Host` / `Port` | persisted literals | live venue |
| `InboundSeq` / `OutboundSeq` | `1` / `1` | fake |
| `QuoteAgeSeconds` | `UtcNow - dest.ReceivedAt` | ~0 s at boot (fresh **fake** quote) |
| `InstrumentId` | dest `VenueInstrumentId` | `null` (honest) |
| `Bid` / `Ask` | dest snapshot | `2399.45` / `2399.85` (invented) |
| `ExecutionEnabled` | hardcoded `false` | honest accident |

An operator who starts `apps/api` and opens the FIX page will see Pepperstone 1369850 **connected and logged on**. That display is false.

### 4.3 Worker does not correct it

`apps/fix-worker/Worker.cs` every 15 s:

```text
quote.Status = ReadyForMarketData;
trade.Status = real ? LoggedOn : LoggedOn;   // both branches LoggedOn
```

The worker **re-forges** the same two values. The seeder is the **first** writer; the worker is the **steady-state** writer. Removing only one leaves the lie.

### 4.4 Integration test cements CompIDs, not honesty

`SeedingAndStoreTests.Demo_seed_discovers_groups_reconstructs_and_scores` asserts:

- `FixSessionStates.Should().HaveCount(2)`
- `TargetCompId.Distinct().Should().Equal("cServer")`

It does **not** assert `Status == Disconnected`. A later honest seeder that still writes two `cServer` rows will keep this fact green. A later honest seeder that **omits** FIX rows will fail the count. The test currently **accepts** `LoggedOn`. It is not a Logon gate (`C16`).

---

## 5. Rest of the seed (not the FIX lie, still measured)

Order of writes, one `SaveChangesAsync`, then ingest:

| Step | What | Honest? |
|---|---|---|
| 1 | Early-return if any `Broker` | Startup guard; untested |
| 2 | Two `Broker` rows | Live **lab IPs** + manager logins in a demo catalog |
| 3 | One `CanonicalInstrument` `XAUUSD` | Fine as catalog |
| 4 | Two `FixSessionState` rows | **FORGED health + live identifiers** |
| 5 | One `DestinationQuoteSnapshot` | Invented mid/spread; `VenueInstrumentId=null` |
| 6 | One `KillSwitch` `Mode=None`, `SetBy="system"` | Fine as default latch |
| 7 | `SaveChangesAsync` | Persists the lie **before** ingest |
| 8 | `DemoBrokerFactory.CreateDefault()` → **new** `BrokerRegistry` → `DealIngestionService.SyncBrokerAsync` both codes, window `2026-01-01` … `2026-12-31T00:00:00Z` | Real Fake-tape ingest; **ignores** container registry (`C05`) |
| 9 | `RebuildTraderAsync` for `10001`, `10002`, `10003`, `99001` (`login >= 99000` → Starwave) | Real scoring of the Fake book |

### 5.1 Broker catalog (live lab targeting)

| Code | Id | Server | Port | ManagerLogin | ServerName | Mode | PoolSize |
|---|---|---|---:|---:|---|---|---:|
| `ACHIEVER` | `…aaa1` | `57.128.141.65` | 443 | **2027** | `AchieverGlobalMarkets-Server` | `local` | 8 |
| `STARWAVEFX` | `…aaa2` | `84.201.6.142` | 443 | **9904** | `StarwaveFX` | `local` | 4 |

No manager password is stored (`Broker` has no password column). Identifiers are still **live lab**, not `test.invalid` / login `0` (`A90` §1.5, `A61`). `Enabled=true`. Ingest does **not** use these IPs: step 8 builds `FakeMt5BrokerConnector` instances. The rows can later fool an operator (or a future real connector) into pointing at the lab.

### 5.2 Fake dest quote

```103:111:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
        db.DestinationQuotes.Add(new DestinationQuoteSnapshot
        {
            Id = Guid.NewGuid(),
            CanonicalSymbol = "XAUUSD",
            VenueInstrumentId = null,
            Bid = 2399.45m,
            Ask = 2399.85m,
            ReceivedAt = now
        });
```

Spread = **0.40**. `VenueTimestamp` unset. Combined with QUOTE `ReadyForMarketData`, the FIX page shows a **0-second-old** XAU book that never came from cServer. `VenueInstrumentId=null` is the one honest field (`B15` / `A94`).

### 5.3 What the Fake ingest actually produces (for context)

`DemoBrokerFactory` tape (`C16` / `C10`; not re-run this review):

| Broker | Groups | Logins | Deals | Completed XAU |
|---|---:|---|---:|---:|
| ACHIEVER | 3 | 10001, 10002, 10003 | 12 | 3 + 3 + 0 |
| STARWAVEFX | 1 | 99001 | 6 | 3 |

This path is **demo MT5 orchestration**. It is orthogonal to FIX. Do not use “seed reconstructs 9 trades” as evidence that TRADE is logged on.

Not written: `SourceSymbolMappings`, `OutboxEvents`, `SyncCheckpoints`, `CopyIntents`, `ExecutionIntents`, `ShadowOrders`, `AuditLogs`, `RiskDecisions`. Kill-switch only. No RBAC user (`C18`).

---

## 6. Callers (who plants the lie)

| Caller | When | Shared DB risk |
|---|---|---|
| `D:\Prop\apps\api\Program.cs` ~88 | After `EnsureCreatedAsync`, every API boot | First empty store seeds |
| `D:\Prop\apps\mt5-worker\Program.cs` ~15 | Worker boot | Same seeder; **destination process writes `mt5_*` + FIX rows** (`B07` U2) |
| `D:\Prop\apps\fix-worker\Program.cs` ~15 | Worker boot | FIX worker writes **MT5 catalog + Fake deals** then Worker stamps status |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | Fact 1 | Isolated InMemory; still asserts live `cServer` CompID |

All three hosts copy-paste the same four-argument call. `AddTraderIntelligence` does **not** register `DemoSeeder` (`C05`). `/api/ops/resync` does **not** re-seed FIX; it only re-syncs Fake deals and rebuilds the four logins. The forged session rows **survive** resync.

If API and both workers share one Postgres later, the first process to see an empty `brokers` table wins. Later processes early-return. The worker then keeps TRADE at `LoggedOn` forever.

---

## 7. What this file is **not**

| Not claimed | Why |
|---|---|
| Live FIX Logon is proven | **Opposite.** Seed is anti-evidence (`C43`). |
| `35=D` can fire because TRADE is `LoggedOn` | No NOS builder. `ExecutionEnabled=false`. Status is unused by any send path (there is no send path). |
| QUOTE `ReadyForMarketData` is “more honest” than TRADE `LoggedOn` | It is **less** honest. MD-ready is a post-Logon application state. |
| Seeder should be deleted entirely | Fake-broker ingest + score is a useful **dev** bootstrap. The FIX **status** is the defect. |
| Hash drift vs C05 / C18 | Same SHA-256 `139D8F87…0BEF`. File unchanged since those reviews. |
| `A05` empty `Class1` | Stale. Four `Fix.CTrader` files exist. Seeder still does not call them. |

---

## 8. What would make the seeder honest (coding task; not this agent)

Minimum, in product (do **not** do it from this report):

1. Seed FIX rows as `FixSessionStatus.Disconnected` **or** do not seed session rows at all (let the session object insert them).
2. Do not seed `LastInboundAt` / `LastOutboundAt` as “now.” Leave null until a real inbound/outbound.
3. Do not seed seq `1/1` as if a Logon reset happened. `0/0` or omit.
4. Replace live host / `live.pepperstone.1369850` in **demo** seed with `test.invalid` + a non-live SenderCompId, **or** keep live identifiers only in env / options and never persist them from the seeder (`A90` §1.5, `A101` §4.2).
5. Either omit the dest quote or mark it unusable (`ReceivedAt` far in the past, or a `PriceSource` test-only once that column exists). Do not pair a fresh bid/ask with `ReadyForMarketData`.
6. Stop `fix-worker` / `mt5-worker` from calling `DemoSeeder` (`B07`). Seed is an API/dev concern.
7. Worker must not assign `LoggedOn` / `ReadyForMarketData` (`C43` §5.1). Dashboard healthy bits come from a session object, not from this enum after seed.

Until (1) + (7) land, **any** process that boots against an empty store will paint TRADE logged on.

Forbidden as “proof” that this file is fixed:

- Changing only the comment.
- Seeding `ReadyForExecution` instead.
- Pointing the dashboard at a different DTO field while the row stays `LoggedOn`.
- A unit test that constructs `FixSessionState { Status = LoggedOn }` and asserts the DTO.

---

## 9. Residual risks

1. **Fake health is worse than a blank dashboard.** First-run API is enough; the worker is not required. `A101` item 1 cannot pass while this insert exists.
2. **Live identifiers in demo rows.** Even after status is `Disconnected`, persisting `live-us-eqx-01.p.c-trader.com` + `live.pepperstone.1369850` + lab MT5 IPs into InMemory/Postgres is a footgun for tests and for a future naive initiator (`B25` B25-02).
3. **QUOTE is over-promoted.** `ReadyForMarketData` without SecurityList / MD is a second, independent lie. Fixing only TRADE `LoggedOn` leaves Overview `QuoteHealthy=true`.
4. **Three hosts seed.** Race on first boot; workers should not write `fix_sessions` or `mt5_*` via this method.
5. **Test lock.** Fact 1 will need a status assertion (`Disconnected`) when the seeder is fixed, or it will keep passing for the wrong reason.
6. **Dest quote age.** A 0-second-old invented book plus `ReadyForMarketData` will defeat any future “stale quote” guard that only looks at `ReceivedAt`.

---

## 10. One-page operator view

```text
D22  DemoSeeder FIX status                              2026-08-18
================================================================
File     src/Infrastructure/Seeding/DemoSeeder.cs
SHA-256  139D8F872DC473F0C5381AF2393BDBBE60E1D9A2A5179DD1D1737E04CCC00BEF
Bytes    4942   Lines 138
----------------------------------------------------------------
TRADE Status = LoggedOn                                 FORGED
QUOTE Status = ReadyForMarketData                       FORGED
TLS / 35=A / QuickFIX / Fix.CTrader call                ABSENT
Host live-us-eqx-01.p.c-trader.com :5211/:5212          LITERAL
SenderCompId live.pepperstone.1369850                   LITERAL
Dest quote 2399.45 / 2399.85, VenueInstrumentId=null    FORGED BOOK
Overview QuoteHealthy / TradeHealthy after seed         TRUE (LIE)
FixSessionDto.Connected / LoggedOn                      TRUE (LIE)
ExecutionEnabled                                        false (honest)
Worker 15s stamp                                        RE-FORGES
Callers                                                 api + both workers + 1 test
Live FIX Logon                                          NOT PROVEN (C43)
Product source edited by D22                            NO
================================================================
```

---

## 11. Sources

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (SHA-256 `139D8F872DC473F0C5381AF2393BDBBE60E1D9A2A5179DD1D1737E04CCC00BEF`)
- `D:\Prop\src\Domain\Entities\FixSessionState.cs`
- `D:\Prop\src\Domain\Enums\FixSessionStatus.cs`
- `D:\Prop\src\Domain\Enums\FixSessionQualifier.cs`
- `D:\Prop\src\Domain\Entities\KillSwitch.cs`
- `D:\Prop\src\Domain\Entities\DestinationQuote.cs` (`DestinationQuoteSnapshot`)
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`
- `D:\Prop\src\Application\Dashboard\DashboardModels.cs` (`FixSessionDto`, `OverviewDto`)
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` (defaults only; unused by seeder)
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` (`DemoBrokerFactory`)
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\reports\swarm\20260818\C43_honesty_no_live_fix.md`
- `D:\Prop\reports\swarm\20260818\C16_seed_test_review.md`
- `D:\Prop\reports\swarm\20260818\C05_di_review.md`
- `D:\Prop\reports\swarm\20260818\C07_workers_review.md`
- `D:\Prop\reports\swarm\20260818\B05_fix_gap.md`
- `D:\Prop\reports\swarm\20260818\A25_fix_session_spec.md`
- `D:\Prop\reports\swarm\20260818\A101_live_fix_acceptance.md`

---

*End of D22. Product source was not modified. `DemoSeeder` TRADE `LoggedOn` (and QUOTE `ReadyForMarketData`) are seeded without a FIX session.*
