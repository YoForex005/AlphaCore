# E008 — DemoSeeder + fix-worker status: still forging `LoggedOn`?

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\E008_fix_status.md` |
| Agent | E008 (DemoSeeder / fix-worker / LoggedOn re-measure) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:48:31+05:30 |
| Assigned | Read DemoSeeder and fix-worker status. Still forging `LoggedOn`? Write this file. Do not modify product source. |
| Product source modified | **No** |
| Test source modified | **No** |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback…`) |
| Binding law | Architecture §§25–26, 61, 70; `A25` §2.3 / §3.6; `A101` item 1; `C43` (live Logon **NOT PROVEN**) |
| Honesty siblings | `D32` / `D94` (worker no longer stamps `LoggedOn`), `D22` (**stale** seeder `LoggedOn`), `D07` / `D43` / `C07` / `C43` / `A101` |
| Method | Full read of `DemoSeeder.cs`, `apps/fix-worker/Worker.cs` + `Program.cs`, `mt5-worker/Worker.cs`, `apps/api/Program.cs`, `EfDashboardQueries`, `FixSessionStatus`, `FixSessionState`, `CTraderFixOptions`, `CTraderQuoteService` (header), `FixSimulationHarness` (header), `SeedingAndStoreTests`, `OverviewPage`. `Get-FileHash SHA256`. `git hash-object` + `git status --short`. Grep product `*.cs` for `FixSessionStatus.LoggedOn` / `Status =` / sockets. Product source **not** edited. |

**Honesty rule (same as C43 / D32 / D94):** a `fix_sessions.Status = LoggedOn` row is **not** a FIX session. A 15 s EF `UpdatedAt` bump is **not** a Heartbeat and is **not** a `35=5`. An honest `Disconnected` stamp is **not** a measured TCP drop. Live Logon remains **NOT PROVEN**.

---

## 0. Verdict

**No. Current `DemoSeeder` and `fix-worker` do not forge `FixSessionStatus.LoggedOn`.**

Both writers persist **`Disconnected`** and an error string that admits there is no live socket. There is still **no** TCP, TLS, QuickFIX/n initiator, or inbound `35=A`. Dashboard `QuoteHealthy` / `TradeHealthy` / `FixSessionDto.LoggedOn` therefore evaluate **false** on the current rows.

The mid-wave forge documented in D22 / B07 / C07 / C43 / A101 (TRADE `LoggedOn`, QUOTE `ReadyForMarketData`, worker `LastInboundAt = UtcNow` every 15 s) is **gone from these two files**. Treat those reports as **historical**. Use this file (or D32 / D94) for current bytes.

| Question | Measured answer |
|---|---|
| Does `DemoSeeder` assign `LoggedOn`? | **No.** QUOTE L73 and TRADE L91 are `Disconnected`. |
| Does `DemoSeeder` assign `ReadyForMarketData` / `ReadyForExecution`? | **No.** |
| Does `fix-worker` `Worker.cs` assign `LoggedOn`? | **No.** Zero `LoggedOn` tokens. QUOTE L32 and TRADE L40 are `Disconnected`. |
| Does any other product writer assign `Status = FixSessionStatus.LoggedOn`? | **No.** Only dashboard **reads** of the enum (plus the enum member and DTO field). |
| Does `CTrader:RealCopyExecutionEnabled` change the stamp? | **No.** Flag is log-only. Status path does not branch. |
| Is there a socket / TLS / initiator? | **No.** |
| Do Overview `QuoteHealthy` / `TradeHealthy` go green after seed? | **No** — both require `LoggedOn` or a later FSM state. |
| Is live QUOTE/TRADE Logon proven? | **No** (`C43`, `D43`). `A101` item 1 / §70.1 still **FAIL**. |
| Is live send possible if the process starts? | **No** (`SAFE_BY_ABSENCE`). |
| Product source edited by E008? | **No.** |

Classification:

| Slice | Class |
|---|---|
| Seeder FIX **status** (current) | **HONEST ENUM** (`Disconnected`) — not a session |
| Worker FIX **status** (current) | **HONEST ENUM** (`Disconnected` every 15 s) — not a session |
| Mid-wave TRADE `LoggedOn` / QUOTE `Ready*` / `LastInboundAt` tick | **REMOVED** (was **FORGED** / anti-evidence) |
| Seeded dest quote `2399.45` / `2399.85` | **STILL FORGED BOOK** (`VenueInstrumentId = null`) |
| Live host / `SenderCompId` / ports 5211–5212 in demo rows | **LIVE IDENTIFIERS IN A DEMO ROW** (not a handshake) |
| Dashboard enum-as-health | **LATENT LIE** — green again if any writer puts `LoggedOn` back |
| Live TLS / `35=A` | **MISSING** |
| Live send | **SAFE_BY_ABSENCE** |

Do **not** tick `A101` item 1 from this file. Do **not** treat “we stopped forging LoggedOn” as a FIX pass.

---

## 1. File hashes (this pass)

| Path | Bytes | Physical lines | LastWriteTimeUtc | SHA-256 | git blob | Worktree |
|---|---:|---:|---|---|---|---|
| `src/Infrastructure/Seeding/DemoSeeder.cs` | 5082 | 140 | 2026-08-18T08:04:59.2131544Z | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | `d65f09fa48d9045537c4fff358f523d9e4440896` | untracked (`??`) |
| `apps/fix-worker/Worker.cs` | 2093 | 51 | 2026-08-18T08:04:48.7473622Z | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` | `4a0cf33486bdb9fae8435d0fe8b2a87d604f6a5d` | modified (` M`) |
| `apps/fix-worker/Program.cs` | 859 | 22 | 2026-08-18T07:45:01.3638263Z | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` | — | modified (` M`) |
| `apps/mt5-worker/Worker.cs` | 1882 | 45 | 2026-08-18T07:45:01.3638263Z | `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` | — | (not a FIX writer) |
| `apps/api/Program.cs` | 4731 | 95 | 2026-08-18T08:05:15.0457194Z | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | — | seeds via `DemoSeeder` |
| `src/Infrastructure/Dashboard/EfDashboardQueries.cs` | 8708 | 200 | 2026-08-18T08:05:15.0366978Z | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | — | untracked (`??`); **reader**, not writer |

Worker SHA matches D32 / D94. Seeder SHA matches D94’s “D22 stale” pin (`A6416491…`), **not** D22’s `139D8F87…` / 4942 bytes.

HEAD `Worker.cs` is still the `dotnet new worker` 1 s log loop (never touched `fix_sessions`). The honest `Disconnected` loop exists only in the worktree.

---

## 2. `DemoSeeder` as measured

File: `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`

Guard: `if (await db.Brokers.AnyAsync(ct)) return;` — first writer wins. Called from `apps/api/Program.cs`, `apps/fix-worker/Program.cs`, and `apps/mt5-worker/Program.cs` after `EnsureCreatedAsync`.

FIX block (both rows):

```68:103:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
        db.FixSessionStates.AddRange(
            new FixSessionState
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1"),
                Qualifier = FixSessionQualifier.Quote,
                Status = FixSessionStatus.Disconnected,
                Host = "live-us-eqx-01.p.c-trader.com",
                Port = 5211,
                SenderCompId = "live.pepperstone.1369850",
                TargetCompId = "cServer",
                SenderSubId = null,
                TargetSubId = "QUOTE",
                InboundSeq = 1,
                OutboundSeq = 1,
                LastInboundAt = now,
                LastOutboundAt = now,
                LastError = "No live QUOTE socket. Demo seed only.",
                UpdatedAt = now
            },
            new FixSessionState
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2"),
                Qualifier = FixSessionQualifier.Trade,
                Status = FixSessionStatus.Disconnected,
                Host = "live-us-eqx-01.p.c-trader.com",
                Port = 5212,
                SenderCompId = "live.pepperstone.1369850",
                TargetCompId = "cServer",
                TargetSubId = "TRADE",
                InboundSeq = 1,
                OutboundSeq = 1,
                LastInboundAt = now,
                LastOutboundAt = now,
                LastError = "No live TRADE socket. NewOrderSingle off.",
                UpdatedAt = now
            });
```

| Field | QUOTE | TRADE |
|---|---|---|
| `Status` | `Disconnected` | `Disconnected` |
| `LastError` | `"No live QUOTE socket. Demo seed only."` | `"No live TRADE socket. NewOrderSingle off."` |
| `Host` / ports | `live-us-eqx-01.p.c-trader.com` **5211** | same host **5212** |
| `SenderCompId` | `live.pepperstone.1369850` | same |
| `TargetCompId` / `TargetSubId` | `cServer` / `QUOTE` | `cServer` / `TRADE` |
| Seq | 1 / 1 | 1 / 1 |
| `LastInboundAt` / `LastOutboundAt` | seed clock (`UtcNow`) | seed clock |
| `using` of `Fix.CTrader` | **none** | **none** |
| Socket / `35=A` | **none** | **none** |

D22 claimed L90 `LoggedOn` and L73 `ReadyForMarketData` against seeder SHA `139D8F87…` (4942 B). That body is gone. Current seeder is **140** lines / **5082** B / `A6416491…`.

Residual (not `LoggedOn`, still not honest venue data):

1. Live Pepperstone host + `SenderCompId` literals (same defaults as `CTraderFixOptions`).
2. `LastInboundAt = LastOutboundAt = now` at first empty-broker boot — a seed clock, not a Heartbeat. Worker does **not** refresh those two columns.
3. Invented dest snapshot `Bid=2399.45` / `Ask=2399.85`, `VenueInstrumentId=null` (L105–113). Dashboard `GetFixSessionsAsync` will attach that book to **both** session DTOs.
4. Integration test `SeedingAndStoreTests` asserts two session rows and `TargetCompId == cServer`. It does **not** assert `Disconnected`. A regression back to `LoggedOn` would still pass that test.

---

## 3. `fix-worker` as measured

### 3.1 `Worker.cs` (SHA `92A8F492…`, same as D32/D94)

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
```

| Field | QUOTE | TRADE |
|---|---|---|
| `Status` | `Disconnected` | `Disconnected` |
| `LastError` | `"No live QUOTE socket. Simulator/demo only."` | `"No live TRADE socket. NewOrderSingle remains off."` |
| `UpdatedAt` | `UtcNow` every 15 s | same |
| `LastInboundAt` / `LastOutboundAt` / seq / owner | **untouched** | **untouched** |
| Socket / TLS / `35=A` | none | none |

`apps/fix-worker/appsettings.json` is logging only — no `CTrader` block. Env `REAL_COPY_EXECUTION_ENABLED` is **unread**. `real==true` only logs; status is still `Disconnected`. There is no send function to refuse.

### 3.2 Host + project

`Program.cs`: `AddTraderIntelligence` → `EnsureCreatedAsync` → `DemoSeeder.SeedAsync` → `AddHostedService<Worker>`. Host start is not a venue Logon.

`TraderIntelligence.FixWorker.csproj` references `Fix.CTrader`. `Worker.cs` does not use it. No `TcpClient`, `SslStream`, `QuickFIX`, `IInitiator`, `ConnectAsync`.

`mt5-worker/Worker.cs` is a 30 s Fake ingest + score loop. It never touches `FixSessionStates`.

---

## 4. Who still *mentions* `LoggedOn` (readers, not forgers)

Product `FixSessionStatus.LoggedOn` occurrences outside the enum definition:

| File | Role |
|---|---|
| `EfDashboardQueries.GetOverviewAsync` L40–41 | `QuoteHealthy` / `TradeHealthy` if status ∈ `{LoggedOn, Ready*, …}` |
| `EfDashboardQueries.GetFixSessionsAsync` L170–171 | `Connected` = status ∉ `{Disconnected, Error}`; `LoggedOn` bit = status ∈ `{LoggedOn, Ready*, Reconciling}` |
| `DashboardModels.FixSessionDto.LoggedOn` | DTO field name |

With current seeder + worker rows (`Disconnected`):

| Bit | Evaluates |
|---|---|
| Overview `QuoteHealthy` | **false** |
| Overview `TradeHealthy` | **false** |
| `FixSessionDto.Connected` | **false** |
| `FixSessionDto.LoggedOn` | **false** |
| `FixSessionDto.ExecutionEnabled` | hardcoded **false** |
| Overview QUOTE/TRADE tile (`OverviewPage.tsx` L28) | `"- / -"` |

`/api/health` independently hardcodes `fixSessions[0].healthy = false` with details `"no live TLS socket"` (`apps/api/Program.cs` L26–33). That path does **not** read the enum.

The dashboard contract is unchanged: health is still an enum, not a session object. **Any** future writer that puts `LoggedOn` (or Ready*) back on the row will paint green again. That is a **latent** lie, not a current one.

---

## 5. Stale-vs-this-file (do not mix epochs)

Same paths, three `Worker.cs` bodies and two `DemoSeeder` bodies on 2026-08-18:

| Epoch | Who hashed it | Worker | Seeder | FIX status written |
|---|---|---|---|---|
| HEAD | D94 | template 1 s log loop | file untracked / absent at HEAD | **none** |
| Mid-wave forge | B07 / C07 / C43 / A101 / D22 | 1971 B / `B48033A5…` (`real ? LoggedOn : LoggedOn` + QUOTE `ReadyForMarketData` + `LastInboundAt` tick) | 4942 B / `139D8F87…` TRADE `LoggedOn` / QUOTE `ReadyForMarketData` | **FORGED** |
| Current worktree | D32 / D94 / **this file** | 2093 B / `92A8F492…` | 5082 B / `A6416491…` | **`Disconnected`** |

D07’s classification table still says `DemoSeeder` “forges QUOTE Ready / TRADE LoggedOn until fix-worker overwrites”. That sentence is **stale** against current seeder bytes. The worker overwrite is now `Disconnected` → `Disconnected`.

INDEX already pins: “D22 seeder LoggedOn stale”; “fix-worker now stamps Disconnected”.

---

## 6. Residual hazards (honest, not a PASS)

1. **No venue.** `Disconnected` is a clock write, not a measured TCP state. Phase 4 Logon is still **0**. Confirm C43 / D43: §70 **0/14**.
2. **Smash-from-above.** If a later process owns a real session and persists `LoggedOn` / seq, this worker will force `Disconnected` every 15 s and clobber `LastError`. Inverse lie.
3. **`real` flag is theater.** It does not gate a send path because no send path exists.
4. **Dashboard contract unchanged.** Do not add `LoggedOn` back without an `IFixSession` (or QuickFIX session) that actually received `35=A`.
5. **`UpdatedAt` tick is not inbound traffic.** Do not wire freshness to `UpdatedAt`.
6. **Fake dest quote + live CompIDs** remain in the seed. Status honesty ≠ book honesty.
7. **No test locks the honest enum.** `SeedingAndStoreTests` will not catch a LoggedOn regression.

---

## 7. Direct answers

**Read DemoSeeder status?**  
Both FIX rows seed `FixSessionStatus.Disconnected` with LastError text that admits no live socket (SHA-256 `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20`).

**Read fix-worker status?**  
The 15 s loop stamps QUOTE and TRADE `Disconnected` and does not open a socket (SHA-256 `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2`).

**Still forging `LoggedOn`?**  
**No** — not from `DemoSeeder`, not from `fix-worker`, not from any other product `Status =` writer. Live Logon is still **not proven**. The dest-quote book is still invented. Dashboard *would* greenwash if the enum returned.
