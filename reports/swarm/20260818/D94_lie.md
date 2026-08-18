# D94 — Anti-evidence: “fix-worker stamps LoggedOn”

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D94_lie.md` |
| Agent | D94 (fix-worker LoggedOn / anti-evidence pin) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:44:41+05:30 |
| Assigned | `fix-worker stamps LoggedOn`. Anti-evidence. Write this file. Do not modify product source. |
| Product source modified | **No.** This report (and swarm log / index rows) are the only writes. |
| Test source modified | **No.** |
| Primary SUT | `D:\Prop\apps\fix-worker\Worker.cs` |
| Current SHA-256 | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` |
| Size / physical lines / LastWriteTimeUtc | **2093** bytes / **51** / `2026-08-18T08:04:48.7473622Z` |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`6c41447` initial commit) |
| Worktree blob | `4a0cf33486bdb9fae8435d0fe8b2a87d604f6a5d` (` M apps/fix-worker/Worker.cs`) |
| Binding law | Architecture §§25–26, 41, 70; `A25` §2.3 / §3.6; `A101` item 1; `C43` (live Logon **NOT PROVEN**) |
| Honesty siblings | `D32` (current worker does **not** assign `LoggedOn`), `D43` (do **not** inherit A101 worker-LoggedOn narrative), `D07`, `D22` (**stale** seeder `LoggedOn`), `C07` / `B07` / `C43` / `A101` (mid-wave forge, **stale as current Worker.cs**) |
| Method | Full `read_file` of `Worker.cs`, `Program.cs`, seeder, `EfDashboardQueries`, status enum, DTO. `Get-FileHash SHA256`. `git show HEAD:apps/fix-worker/Worker.cs`, `git blame -L 28,42`, `git hash-object`. Grep `LoggedOn` / `Status = FixSessionStatus` / sockets under `apps/fix-worker` + `src/`. UTF-16 string scan of `TraderIntelligence.FixWorker.dll`. Product source **not** edited. |

**Honesty rule (same as C43 / D32 / D43):** a `fix_sessions.Status = LoggedOn` row is **not** a FIX session. A 15-second EF `UpdatedAt` bump is **not** a Heartbeat and is **not** a `35=5`. A dashboard bit that *would* go green if that enum returned is **anti-evidence**. Repeating a mid-wave forge after the bytes changed is a **second** lie.

---

## 0. Verdict

**The assignment sentence is false against current bytes. It was true of an intermediate uncommitted `Worker.cs`. That intermediate stamp is anti-evidence of live FIX, not proof.**

| Claim | Measured truth (this pass) |
|---|---|
| “fix-worker stamps `LoggedOn`” **today** | **LIE.** `Worker.cs` contains **zero** `LoggedOn` tokens. Both session rows are forced to `FixSessionStatus.Disconnected`. |
| “fix-worker stamps `LoggedOn`” **as A101 / C07 / C43 / D22 current fact** | **STALE.** Those files hashed `Worker.cs` at **1971** bytes / `B48033A5A13C56DB747D3C9F0B94E36CB8DC2866FBCF3789A62C3D7B318B0D48`. That body is gone. |
| “HEAD worker stamps `LoggedOn`” | **LIE.** HEAD is the `dotnet new worker` 1 s log loop. It never touches `fix_sessions`. |
| Mid-wave TRADE `Status = real ? LoggedOn : LoggedOn` | **Was a forge.** Documented in C07 §4 / C43 §2.3 / A101. **Anti-evidence** of Logon (no socket then, no socket now). |
| Live TRADE / QUOTE `35=A` / `LOGON_OK` | **NOT PROVEN** (C43, D43). Unchanged by either stamp. |
| `A101` item 1 / Architecture §70.1 | Still **FAIL**. Do **not** tick from this file. |
| Real `35=D` if the process starts | Still **SAFE_BY_ABSENCE**. Flag only logs. |

Classification:

| Slice | Class |
|---|---|
| Assignment sentence as a fact about **current** `Worker.cs` | **FALSE / STALE** |
| Mid-wave `LoggedOn` / `ReadyForMarketData` / `LastInboundAt = UtcNow` loop | **FORGED — anti-evidence** (historical; removed) |
| Current 15 s `Disconnected` + `LastError` loop | **HONEST ENUM, still not a session** (`EXISTS_NEEDS_REFACTOR`) |
| Live TLS / initiator / inbound `35=A` | **MISSING** |
| Dashboard enum-as-health (`LoggedOn` → `TradeHealthy`) | **LATENT LIE** — green again the moment any writer puts `LoggedOn` back |
| Seeder FIX status (current) | **`Disconnected`** (D22 `LoggedOn` seed is **stale**) |
| Live send | **SAFE_BY_ABSENCE** |
| Product source edited by D94 | **No** |

D43 already forbade inheriting the A101 worker-`LoggedOn` narrative. This file is the anti-evidence pin for that sentence: **do not treat “fix-worker stamps LoggedOn” as a current worktree fact**, and **do not treat any `LoggedOn` row this process ever wrote as Logon**.

---

## 1. Three measured bodies of `Worker.cs` (do not mix)

Same path: `D:\Prop\apps\fix-worker\Worker.cs`. Three distinct contents on 2026-08-18.

| Epoch | Who hashed it | Bytes | SHA-256 | What it writes to `fix_sessions` |
|---|---|---:|---|---|
| **HEAD** `6c41447` | this file (`git show HEAD:…`) | template (~628 historically in A08) | git blob `f02ff0939a438978e3cf5443ad4f3ac2b300d17d` | **Nothing.** 1 s `"Worker running at: {time}"`. |
| **Mid-wave forge** | B07 / C07 / C43 / A101 | **1971** | `B48033A5A13C56DB747D3C9F0B94E36CB8DC2866FBCF3789A62C3D7B318B0D48` | QUOTE `ReadyForMarketData` + TRADE `LoggedOn` (both sides of `real`) + `LastInboundAt = UtcNow` every 15 s. |
| **Current worktree** | D32 / D07 / D43 / D69 / **this file** | **2093** | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` | QUOTE **and** TRADE `Disconnected` + `LastError` + `UpdatedAt = UtcNow`. **No** `LastInboundAt` write. **No** `LoggedOn` token. |

`git status`: ` M apps/fix-worker/Worker.cs`. `git blame -L 28,42` marks the status assignments `Not Committed Yet`. The forge and the honesty stamp are **both uncommitted relative to HEAD**. Only the **current** uncommitted body is on disk now.

---

## 2. Current `Worker.cs` (source of truth for this file)

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

### 2.1 Grep (this process)

| Token in `apps/fix-worker` | Hits |
|---|---|
| `LoggedOn` | **0** |
| `ReadyForMarketData` | **0** |
| `FixSessionStatus.Disconnected` | **2** (`Worker.cs` L32, L40) |
| `LastInboundAt` | **0** |
| `TcpClient` / `SslStream` / `Socket` / `QuickFIX` / `IInitiator` / `35=A` | **0** |

Product-wide `Status = FixSessionStatus` writers (`*.cs` under `D:\Prop`):

| File | Assignment |
|---|---|
| `apps/fix-worker/Worker.cs` L32 | QUOTE `Disconnected` |
| `apps/fix-worker/Worker.cs` L40 | TRADE `Disconnected` |
| `src/Infrastructure/Seeding/DemoSeeder.cs` L73 | QUOTE `Disconnected` |
| `src/Infrastructure/Seeding/DemoSeeder.cs` L91 | TRADE `Disconnected` |

No other product C# assigns `FixSessionStatus.LoggedOn`.

### 2.2 What the loop writes (current)

| Field | QUOTE | TRADE |
|---|---|---|
| `Status` | `Disconnected` | `Disconnected` |
| `LastError` | `"No live QUOTE socket. Simulator/demo only."` | `"No live TRADE socket. NewOrderSingle remains off."` |
| `UpdatedAt` | `DateTimeOffset.UtcNow` | `DateTimeOffset.UtcNow` |
| `LastInboundAt` / `LastOutboundAt` | untouched | untouched |
| Seq / owner / host / port | untouched | untouched |
| Socket / TLS / `35=A` | none | none |

`real` (`CTrader:RealCopyExecutionEnabled`, default `false`) does **not** branch the status path. `apps/fix-worker/appsettings.json` has logging only (SHA-256 `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33`). Env name `REAL_COPY_EXECUTION_ENABLED` is **unread** (D69). When `real==true` the worker logs a refusal; it still writes `Disconnected`. There is no send function to refuse.

### 2.3 Compiled Debug DLL (not a second source of truth)

`D:\Prop\apps\fix-worker\bin\Debug\net8.0\TraderIntelligence.FixWorker.dll` — 12800 bytes, LastWriteUtc `2026-08-18T08:10:29.0090640Z`, SHA-256 `6238951AB621DE8F0CDDF8BB22AB84109A26E3BF8BEE31DEB4B2E4B8EDB4A1C1`.

UTF-16LE scan: **HAS** `NewOrderSingle` (log text). **MISS** `LoggedOn` / `ReadyForMarketData`. Enum names live in `TraderIntelligence.Domain.dll`, not this assembly. Do not treat the DLL as a live session either.

---

## 3. The mid-wave forge (anti-evidence — historical only)

C07 §4 / C43 §2.3 / A101 quoted this body (hash `B48033A5…`, 1971 bytes). It is **not** on disk now. Quoted here so nobody “restores” it as a current finding:

```csharp
quote.LastInboundAt = DateTimeOffset.UtcNow;
quote.Status = FixSessionStatus.ReadyForMarketData;
trade.LastInboundAt = DateTimeOffset.UtcNow;
trade.Status = real ? FixSessionStatus.LoggedOn : FixSessionStatus.LoggedOn;
```

Why that was **anti-evidence** of Logon (A25 §3.6 / Architecture §26 / A101 item 1):

| What the row invited | What was actually true |
|---|---|
| TRADE is `LoggedOn` | Enum write. No TCP, no TLS, no `35=A`, no inbound reply, no `LOGON_OK` record. |
| QUOTE is `ReadyForMarketData` | Later FSM state than Logon. No SecurityList, no MD, no venue quote. |
| `LastInboundAt = UtcNow` every 15 s | Clock tick. Not `35=0` / `35=1`. |
| `real ? LoggedOn : LoggedOn` | Theater. Flag cannot change the lie. |
| Dashboard `TradeHealthy` / `FixSessionDto.LoggedOn` | **true** from a false enum (`EfDashboardQueries`). |

That is the lie A101 / C43 told operators to treat as **anti-evidence**. Removing the assignment does **not** create a session. It only stops the dashboard from going green off this worker.

HEAD (for contrast) never forged health:

```csharp
_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
await Task.Delay(1000, stoppingToken);
```

---

## 4. Adjacent writers (not `Worker.cs`, still anti-evidence if misread)

### 4.1 `DemoSeeder` — D22 is stale

Current `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`: **5082** bytes, SHA-256 `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20`, LastWriteUtc `2026-08-18T08:04:59.2131544Z`. File is **untracked** (`??`). Not in HEAD.

L73 / L91 now seed **both** rows as `FixSessionStatus.Disconnected` with `LastError` “No live … socket”. D22’s hash `139D8F87…` / TRADE `LoggedOn` / QUOTE `ReadyForMarketData` is **not** current.

Residual seeder anti-evidence (honest status, dishonest picture):

| Field | Still planted |
|---|---|
| Host | `live-us-eqx-01.p.c-trader.com` |
| Ports | 5211 QUOTE / 5212 TRADE |
| `SenderCompId` | `live.pepperstone.1369850` |
| Seq | `1` / `1` |
| `LastInboundAt` / `LastOutboundAt` | seed `DateTimeOffset.UtcNow` |
| Dest quote | `2399.45` / `2399.85`, `VenueInstrumentId = null` |

A `Disconnected` row with a live Pepperstone CompID and a fresh inbound timestamp is **not** a handshake. It is also **not** `LoggedOn`.

`Program.cs` of fix-worker (SHA-256 `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC`) still: `AddTraderIntelligence` → `EnsureCreatedAsync` → `DemoSeeder.SeedAsync` → `AddHostedService<Worker>`. Boot is not Logon. `Fix.CTrader` is an unused project reference (`csproj` SHA-256 `D7572CBFF273089587D4A68B2A95EB1933C2DF388177F8665BE7C3CC338B5DB4`).

### 4.2 Dashboard still *interprets* `LoggedOn` as healthy

`D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` — **8708** bytes, SHA-256 `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60`.

```40:41:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            quote?.Status is FixSessionStatus.LoggedOn or FixSessionStatus.ReadyForMarketData or FixSessionStatus.ReadyForExecution,
            trade?.Status is FixSessionStatus.LoggedOn or FixSessionStatus.Reconciling or FixSessionStatus.ReadyForExecution,
```

```170:171:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            s.Status != FixSessionStatus.Disconnected && s.Status != FixSessionStatus.Error,
            s.Status is FixSessionStatus.LoggedOn or FixSessionStatus.ReadyForMarketData or FixSessionStatus.ReadyForExecution or FixSessionStatus.Reconciling,
```

| DTO field | Formula | With **current** worker+seeder | If anyone writes `LoggedOn` again |
|---|---|---|---|
| Overview `QuoteHealthy` | QUOTE ∈ {LoggedOn, ReadyForMarketData, ReadyForExecution} | **false** | **true** |
| Overview `TradeHealthy` | TRADE ∈ {LoggedOn, Reconciling, ReadyForExecution} | **false** | **true** |
| `FixSessionDto.Connected` | status ∉ {Disconnected, Error} | **false** | **true** |
| `FixSessionDto.LoggedOn` | status ∈ {LoggedOn, Ready*, Reconciling} | **false** | **true** |
| `ExecutionEnabled` | hardcoded `false` | false (honest accident) | still false |

Health is still an **enum**, not a session object. The current worker keeps those bits false. That is **not** A101 item 1.

### 4.3 Smash-from-above (inverse lie)

If a later process owned a real session and persisted `LoggedOn` / seq / `LastInboundAt`, this worker would overwrite `Status` to `Disconnected` and clobber `LastError` every 15 s. `Disconnected` here is a **clock write**, not a measured TCP drop or outbound `35=5`. Do not treat it as a disconnect handshake.

---

## 5. What is still absent (so no one upgrades this pin to PASS)

Grep of `apps/fix-worker` + `src/Fix.CTrader` product C#: **zero** `TcpClient`, `SslStream`, `SocketInitiator`, `IInitiator`, `QuickFIX`, `CTraderQuoteSession`, `CTraderTradeSession`, `LOGON_OK`.

| Required for live Logon (A25 §3.6) | Present? |
|---|---|
| TLS to `live-us-eqx-01.p.c-trader.com:5211` / `:5212` | **No** |
| Client `35=A` with `553` numeric login | **No** |
| Inbound `35=A` or `35=5`+`58` capture | **No** |
| `LOGON_OK` record (file or `fix_session_events`) | **No** such table |
| Independent QUOTE/TRADE seq stores | **No** |
| QuickFIX/n official packages | **No** (C19 / D05 / D52) |
| Worker tests hosting `TraderIntelligence.FixWorker` | **0** |

`SAFE_BY_ABSENCE` of `35=D` is the correct current send outcome. It is **not** Logon proof (C07, D43).

---

## 6. Stale swarm sentences (do not quote as current)

| File | Sentence that is now wrong as *current* Worker.cs |
|---|---|
| A101 / A100 | Worker 15 s loop forces TRADE `LoggedOn` |
| B07 | Worker.cs hash `B48033A5…`; class `UNSAFE` for Ready/LoggedOn + `LastInboundAt` |
| C07 | Both branches of TRADE ternary are `LoggedOn`; seeder + worker stamp health |
| C43 | “`Worker` rewrites `LoggedOn` every 15 s”; Worker 1971 / `B48033A5…` |
| D22 | Seeder TRADE `LoggedOn` / QUOTE `ReadyForMarketData`; “worker re-forges” |
| INDEX D22 row | Same seeder forge (seeder hash moved to `A6416491…`) |

**Still valid** in those files: no live socket; dashboard enum-as-health; `SAFE_BY_ABSENCE`; live Logon **NOT PROVEN**.

**Use for current Worker.cs status writes:** this file + `D32_fixw.md` + `D07_workers_census.md` + `D43_s70.md`.

---

## 7. Direct answers

**Does fix-worker stamp `LoggedOn`?**

**No — not in the file on disk.** SHA-256 `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2`. It stamps `Disconnected` without a socket.

**Is “fix-worker stamps LoggedOn” anti-evidence?**

**Yes, as a claim about live FIX:** the only time that sentence was true, the stamp was a forge (C07/C43/A101). A forged `LoggedOn` is **anti-evidence** of `LOGON_OK`. It must never tick §70.1 / A101 item 1.

**Is “fix-worker stamps LoggedOn” anti-evidence of *this worktree*?**

**Yes — the sentence itself is a lie today.** Repeating A101’s worker-LoggedOn narrative after D32/D43 is greenwash.

**Is the current `Disconnected` stamp proof of a real disconnect or of a real session?**

**No.** Clock write. No handshake. Phase 4 Logon remains **0**.

---

## 8. One-page operator view

```text
D94  “fix-worker stamps LoggedOn”                       2026-08-18T13:44:41+05:30
================================================================
File     apps/fix-worker/Worker.cs
SHA-256  92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2
Bytes    2093   Lines 51   blob 4a0cf334… (unstaged vs HEAD template)
----------------------------------------------------------------
Assignment “stamps LoggedOn”                            FALSE (current)
HEAD worker                                             1s log; no status
Mid-wave Worker B48033A5… 1971 B                        FORGED LoggedOn (gone)
Current Status writes                                   Disconnected / Disconnected
LoggedOn token in Worker.cs                             0
Tcp / TLS / QuickFIX / 35=A / LOGON_OK                  ABSENT
Seeder (A6416491…)                                      Disconnected (D22 stale)
Dashboard still maps LoggedOn → healthy                 LATENT
A101 item 1 / §70.1                                     FAIL
Live send                                               SAFE_BY_ABSENCE
Product source edited by D94                            NO
================================================================
```

---

## 9. Sources

- `D:\Prop\apps\fix-worker\Worker.cs` (SHA-256 `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2`)
- `D:\Prop\apps\fix-worker\Program.cs` (`05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC`)
- `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj` (`D7572CBFF273089587D4A68B2A95EB1933C2DF388177F8665BE7C3CC338B5DB4`)
- `D:\Prop\apps\fix-worker\appsettings.json` (`AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33`)
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (`A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20`)
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` (`328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60`)
- `D:\Prop\src\Domain\Enums\FixSessionStatus.cs` (`49AD4FD0DB6DF8DF2AD57365822CCA70E0106E49BCD7F153D8CD332EF8FF3268`)
- `D:\Prop\src\Application\Dashboard\DashboardModels.cs` (`9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496`)
- `git show HEAD:apps/fix-worker/Worker.cs` (template; blob `f02ff0939a438978e3cf5443ad4f3ac2b300d17d`)
- `D:\Prop\reports\swarm\20260818\D32_fixw.md`
- `D:\Prop\reports\swarm\20260818\D43_s70.md`
- `D:\Prop\reports\swarm\20260818\D07_workers_census.md`
- `D:\Prop\reports\swarm\20260818\C43_honesty_no_live_fix.md`
- `D:\Prop\reports\swarm\20260818\C07_workers_review.md`
- `D:\Prop\reports\swarm\20260818\B07_workers_gap.md`
- `D:\Prop\reports\swarm\20260818\A101_live_fix_acceptance.md`
- `D:\Prop\reports\swarm\20260818\D22_seeder.md` (stale seeder status)
- `D:\Prop\reports\swarm\20260818\D69_flag.md`

---

*End of D94. Product source was not modified. Current fix-worker does **not** stamp `LoggedOn`. The sentence “fix-worker stamps LoggedOn” is a stale lie about today’s bytes and, when it was true, was anti-evidence of live FIX.*
