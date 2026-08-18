# C43 — Honesty pin: live cTrader FIX Logon is NOT proven

| Field | Value |
|---|---|
| Agent | C43 (honesty / anti-greenwash only) |
| Date | 2026-08-18 |
| Assigned | Live cTrader FIX logon is **NOT** proven. Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\C43_honesty_no_live_fix.md` |
| Product source modified | **No.** This report is the only write. |
| Method | Re-measure worktree (not A05/A08 stale empty-stub). Full read of `apps/fix-worker` Program/Worker/appsettings, `src/Fix.CTrader` (all four `.cs` + csproj + `deps.json`), `DemoSeeder`, `EfDashboardQueries`, `FixSessionState`, `DependencyInjection`. Grep product `*.cs` for initiator / socket / `LOGON_OK` / session types. SHA-256 of measured files. Confirm no `.env`, no `tests/Fix`, no `FIX44-CSERVER.xml`, no QuickFIX `*.store`. |
| Binding proof bar | Architecture §26 + `A25` §3.6 diagnostic Logon gate; `A101` item 1; official RoE Logon `35=A` |
| Siblings (do not treat as live proof) | `A05` (stale empty stub), `A08`, `A25`, `A100`, `A101` 0/14, `B05`, `B27`, `C07` (send-off), `C14` 0/19, `C19` (QuickFIX/n absent), `C21` (header case) |
| Live account (not a test fixture) | Pepperstone / cServer login `1369850`, host `live-us-eqx-01.p.c-trader.com` |

**Honesty rule:** a `fix_sessions.Status = LoggedOn` row is **not** a FIX session. A 15-second EF timestamp bump is **not** a Heartbeat. A pipe-delimited `35=A` string is **not** an inbound Logon. A default hostname in a POCO is **not** a TLS handshake. Absence of `NewOrderSingle` is **not** Logon proof. Dashboard `TradeHealthy=true` from a seeder enum is **anti-evidence**.

---

## 0. Verdict

**NOT PROVEN.**

No process in this tree has opened TLS to `live-us-eqx-01.p.c-trader.com:5211` or `:5212`, sent a client Logon (`35=A`), or received a cServer Logon reply. There is **zero** `LOGON_OK` (or `LOGON_REJECTED` / `NO_RESPONSE` / `TRANSPORT_FAIL`) record on disk.

| Claim someone might make | Measured truth |
|---|---|
| “TRADE is LoggedOn” | **Lie.** `DemoSeeder` inserts `LoggedOn`. `Worker` rewrites `LoggedOn` every 15 s. No socket. |
| “QUOTE is ReadyForMarketData” | **Lie.** Same seeder + worker. No SecurityList, no MD, no quote from the venue. |
| “We configured the live host, so we connected” | **Config literals only.** Host is a C# / JSON / `.env.example` string. Unbound in `fix-worker`. |
| “Harness `SimulateLogonSuccess` proves Logon” | **String factory.** Builds `35=A` with **client-side** CompIDs. Never talks to cServer. Unused by worker and tests. |
| “QuickFIX/n logged on” | **False.** Official `QuickFIXn.Core` / `QuickFIXn.FIX44` are **not referenced**. Worktree `Fix.CTrader.csproj` has **no** FIX package. `deps.json` lists Domain + Application + FluentValidation only. |
| “Header mapping is proven (`cServer` vs `CSERVER`)” | **Unresolved on the wire.** Defaults exist; nothing was sent. `B27` / `C21` are spelling audits, not Logon. |
| “Live copy is off, therefore Logon is fine” | **Category error.** Send-off (`C07` `SAFE_BY_ABSENCE`) ≠ Logon-proven. |

Classification:

| Slice | Class |
|---|---|
| Live QUOTE Logon (`57=QUOTE`, TLS 5211) | **NOT PROVEN** (no attempt) |
| Live TRADE Logon (`57=TRADE`, TLS 5212) | **NOT PROVEN** (no attempt) |
| A25 §3.6 diagnostic record (both sessions) | **MISSING** |
| `CTraderQuoteSession` / `CTraderTradeSession` | **MISSING** |
| QuickFIX `IInitiator` / `SocketInitiator` / `SessionSettings` | **MISSING** |
| TCP/TLS (`TcpClient`, `SslStream`) in product C# | **ABSENT** |
| Dashboard / seeder / worker `LoggedOn` | **FORGED — anti-evidence** |
| Live `35=D` if process starts now | **SAFE_BY_ABSENCE** (orthogonal; still not Logon) |
| Safe to treat venue as connected | **No** |
| Safe to enable `REAL_COPY_EXECUTION_ENABLED` | **No** |

Do **not** tick `A101` item 1 (“TRADE FIX Logon is stable”). Do **not** tick `A25` §9 “TRADE/QUOTE Logon is stable”. Do **not** tick Architecture §70.1. Do **not** start Phase 4 application messages. A QUOTE-only success would still not unlock TRADE. Today neither session has a success.

---

## 1. What “proven live Logon” means (binding)

Copied from architecture §26 and `A25` §3.6 so this file cannot be satisfied by a DB enum.

A “header mapping proven” record must exist (file **or** `fix_session_events`) **for each** session:

```text
timestamp (UTC)
host, port, TLS yes/no
SenderCompID, TargetCompID (as sent — case preserved)
SenderSubID, TargetSubID (as sent)
Username (numeric login only; no password)
ResetSeqNumFlag, HeartBtInt
outbound Logon checksum-valid? (yes/no)
inbound MsgType (A or 5)
inbound Text (58) if Logout
result: LOGON_OK | LOGON_REJECTED | NO_RESPONSE | TRANSPORT_FAIL
```

Official client Logon body (RoE): `35=A`, `98=0`, `108=<HeartBtInt>`, `141=Y`, `553=<numeric 1369850>`, `554=<secret>`. Successful reply **swaps** Comp/Sub IDs (`49=CSERVER` or issued target, `50=QUOTE|TRADE`, `56=` client). Failed Logon is inbound **`35=5` + `58=…`**, not a session-reject `35=3`.

**Both** sessions must be `LOGON_OK` before Phase 4+ application messages. Simulator Logon does **not** satisfy the live gate. `LoggedOn` without this record is **not** the gate.

Stable Logon (`A101` item 1) further requires: independent QUOTE/TRADE sequence stores, Heartbeat / TestRequest (QUOTE liveness = quote age, not missing `35=0`), clean reconnect with `141=Y` that does **not** flush a send queue, single TRADE owner, and **no** seeder/worker forge.

**None of those records exist.** There is no `FixSessionEvent` type, no `fix_session_events` table, no capture file, no pcap, no QuickFIX `FileStore`, no worker log line that contains inbound `35=A`.

---

## 2. Measured tree (2026-08-18 worktree)

SHA-256 via `Get-FileHash`. Product source only.

### 2.1 Adapter — four files, zero sessions

| Path | Bytes | SHA-256 | Role vs live Logon |
|---|---:|---|---|
| `src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj` | 419 | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` | net8 classlib; refs Domain + Application; **no** `PackageReference` |
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | 2344 | `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` | Host/ports/CompIDs + flags. **Not bound** by any host |
| `src/Fix.CTrader/Parsing/FixMessageParser.cs` | — | `C58681E761D43052B53D2A8D00883C461A9E3CEB5B7DF8995D50F8155F710E3D` | Pipe/`|` checksum helper. No socket |
| `src/Fix.CTrader/Services/FixSessionOwnership.cs` | — | `30029E29EE66C2114643AAF8FD0E0D8566C075A0FF693CCA7043CEADED5E6D20` | In-memory fence. Unused by worker |
| `src/Fix.CTrader/Testing/FixSimulationHarness.cs` | — | `99A28D8F3F49028706C75B9C4DC46B4CCB3FF98E90AAED3B4B874DD1B4351616` | Fixture string factory. `SimulateLogonSuccess` is **not** live |

No `Sessions/`, no `QuickFix/`, no `*.cfg`, no `FIX44-CSERVER.xml`. Repo search under `D:\Prop` (excluding `reports/`) for `FIX44*` = architecture markdown only.

`bin/Debug/net8.0/TraderIntelligence.Fix.CTrader.deps.json` runtime dependencies:

```text
TraderIntelligence.Application
TraderIntelligence.Domain
FluentValidation 11.9.2   (transitive)
```

**No** `QuickFix`, **no** `QuickFIXn`. Confirming `C19`: official QuickFIX/n is not referenced; unofficial `QuickFix.Net` 1.8.0 is gone from the **worktree** csproj (HEAD may still list it — that is still not an initiator).

Product `*.cs` grep (`src/` + `apps/`) for `SocketInitiator`, `IInitiator`, `IApplication`, `SessionSettings`, `TcpClient`, `SslStream`, `CTraderQuoteSession`, `CTraderTradeSession`, `ICTraderFixVenue`, `LOGON_OK`: **zero hits** except the unused enum value `PriceSource.CTraderQuoteSession`.

### 2.2 `apps/fix-worker` — stamps health, never connects

| Path | Bytes | SHA-256 | Role vs live Logon |
|---|---:|---|---|
| `Program.cs` | 859 | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` | `AddTraderIntelligence` + `EnsureCreated` + `DemoSeeder`. **No FIX DI** |
| `Worker.cs` | 1971 | `B48033A5A13C56DB747D3C9F0B94E36CB8DC2866FBCF3789A62C3D7B318B0D48` | 15 s EF heartbeat. **No socket** |
| `appsettings.json` | 137 | `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` | Logging only. **No** `CTrader:*` |
| `TraderIntelligence.FixWorker.csproj` | 856 | `D7572CBFF273089587D4A68B2A95EB1933C2DF388177F8665BE7C3CC338B5DB4` | Project ref to `Fix.CTrader` is **unused** |

`DependencyInjection.AddTraderIntelligence` registers Fake MT5 connectors, EF (InMemory if no real connection string), dashboard, ingestion. **Does not** bind `CTraderFixOptions`. **Does not** register a venue.

`Program.cs` never constructs `FixSimulationHarness`, `FixMessageParser`, `FixSessionOwnership`, or `CTraderFixOptions`.

`FixWorker.deps.json` lists `TraderIntelligence.Fix.CTrader` as a project dependency and then EF / Redis / Hosting. **No QuickFIX DLL** is copied to `apps/fix-worker/bin`.

There is **no** `D:\Prop\.env` and **no** `D:\Prop\apps\fix-worker\.env`. Password slot in API JSON is empty (`CTrader:Password=""`). `CTRADER_FIX_PASSWORD` exists only as `<SECRET>` in `.env.example` and architecture.

### 2.3 The forge (treat as anti-evidence)

`apps/fix-worker/Worker.cs` (complete Logon-related body):

```csharp
var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
_logger.LogInformation(
    "FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.",
    real);

// every 15 seconds:
quote.LastInboundAt = DateTimeOffset.UtcNow;
quote.Status = FixSessionStatus.ReadyForMarketData;
trade.LastInboundAt = DateTimeOffset.UtcNow;
trade.Status = real ? FixSessionStatus.LoggedOn : FixSessionStatus.LoggedOn;
```

Both branches of the TRADE ternary are `LoggedOn`. The flag cannot even change the forged status. `appsettings.json` does not contain `CTrader:RealCopyExecutionEnabled`; default is `false`. Env name `REAL_COPY_EXECUTION_ENABLED` is **unread**.

`DemoSeeder` (first-run only, if `Brokers` is empty) inserts:

| Qualifier | Status seeded | Host | Port | SenderCompId |
|---|---|---|---:|---|
| QUOTE | `ReadyForMarketData` | `live-us-eqx-01.p.c-trader.com` | 5211 | `live.pepperstone.1369850` |
| TRADE | `LoggedOn` | `live-us-eqx-01.p.c-trader.com` | 5212 | `live.pepperstone.1369850` |

Seq in/out = 1. `LastInboundAt` / `LastOutboundAt` = seed clock. Also seeds a fake XAU quote with `VenueInstrumentId=null`.

`EfDashboardQueries`:

| DTO field | Formula | Result after seed / worker |
|---|---|---|
| Overview `QuoteHealthy` | status ∈ {LoggedOn, ReadyForMarketData, ReadyForExecution} | **true** |
| Overview `TradeHealthy` | status ∈ {LoggedOn, Reconciling, ReadyForExecution} | **true** |
| `FixSessionDto.Connected` | status ∉ {Disconnected, Error} | **true** |
| `FixSessionDto.LoggedOn` | status ∈ {LoggedOn, ReadyForMarketData, ReadyForExecution, Reconciling} | **true** |
| `FixSessionDto.ExecutionEnabled` | hardcoded `false` | honest accident |

`FixSessionState` has **no** column for inbound MsgType, Logon result, or tag 58. `TraderDbContext` exposes `DbSet<FixSessionState>` only. There is **no** `FixSessionEvent` entity.

If an operator opens the FIX page after `fix-worker` has run against the in-memory (or later Postgres) DB, the UI will say the live Pepperstone venue is connected. **That display is false.**

### 2.4 Simulator Logon is not live Logon

`FixSimulationHarness.SimulateLogonSuccess`:

- Emits `35=A` with **caller-supplied** 49/56/57/50 (defaults `targetCompId="cServer"`, `targetSubId="QUOTE"`).
- Does **not** swap Comp/Sub IDs the way a cServer reply does.
- Does **not** include 553/554 (correct for a *reply*, but this builder is used as a generic “success” string, not a recorded inbound).
- `SimulateLogonFail` uses `35=3` + tag 371. Official failed Logon is **`35=5` + `58`**. Wrong even as a fixture.

No test project calls these methods. `tests/Fix` **does not exist**. Unit/Integration tests do not reference `SimulateLogon*`.

A later InProcess `CTraderFixSimulator.LogonAsync` (specified in `B05` / `A68`, **not implemented**) would still be **in-process**. It cannot become this file’s live proof.

### 2.5 Config that names the live venue (not a connection)

| Location | What it contains | Bound? | Connected? |
|---|---|---|---|
| `CTraderFixOptions.Host` default | `live-us-eqx-01.p.c-trader.com` | **No** | **No** |
| `CTraderFixOptions.Quote.SslPort` / `Trade.SslPort` | 5211 / 5212 | **No** | **No** |
| `Quote/Trade.TargetCompId` default (worktree) | `cServer` | **No** | **No** |
| `apps/api/appsettings.json` `CTrader` | Host + AccountId `1369850` + empty Password + `RealCopyExecutionEnabled=false` | API does not start a FIX session | **No** |
| `apps/fix-worker/appsettings.json` | Logging only | N/A | **No** |
| `D:\Prop\.env.example` | Full `CTRADER_FIX_*` template, password `<SECRET>` | Example only; no `.env` on disk | **No** |

Defaulting the **live** hostname on a POCO is a footgun (`A101` §4.2). It is not evidence that anyone reached that host.

`Test-Path D:\Prop\.env` = **False**. `Get-ChildItem -Recurse *.store` under `D:\Prop` = **empty**. No QuickFIX sequence files, no diagnostic logon markdown besides swarm specs.

---

## 3. Proof checklist (all open)

| # | Required evidence | On disk 2026-08-18 |
|---|---|---|
| 1 | TLS connect to `live-us-eqx-01.p.c-trader.com:5211` | **No** |
| 2 | TLS connect to same host `:5212` | **No** |
| 3 | Outbound client `35=A` QUOTE (checksum valid, 49/50/56/57 as configured, 553 numeric, 554 not logged) | **No** |
| 4 | Outbound client `35=A` TRADE (same rules, `57=TRADE`) | **No** |
| 5 | Inbound `35=A` (swapped Comp/Sub) **or** inbound `35=5`+`58` persisted | **No** |
| 6 | `LOGON_OK` row/file for QUOTE (`A25` §3.6) | **No** |
| 7 | `LOGON_OK` row/file for TRADE | **No** |
| 8 | Independent sequence stores (two FileStore paths or equivalent) | **No** |
| 9 | Header case **as sent** recorded (no silent `cServer`↔`CSERVER`) | **No send ⇒ unresolved** |
| 10 | Worker/seeder/dashboard no longer write `LoggedOn` without a session object | **FAIL** — they still forge |
| 11 | `tests/Fix` isolation: InProcess must not DNS `*.c-trader.com` | Folder **absent** |
| 12 | Optional soak: Heartbeat / TestRequest after live Logon, then Logout | **No** |

Score: **0 / 12.**

`A101` item 1 remains **FAIL**. `C14` / `A100` §68 stay **0 / 19** for live. This file does not reopen those lists; it nails the Logon box so nobody “passes” it from a seeded enum.

---

## 4. What this does **not** claim

| Not claimed | Why |
|---|---|
| Live `NewOrderSingle` is possible | It is **not**. No `35=D` builder (`C07`). |
| Official QuickFIX/n is wired | It is **not** (`C19`). |
| Header spelling in worktree is wrong | Worktree defaults are `cServer` (`C21`). Spelling ≠ Logon. |
| Password is committed | Empty / `<SECRET>` only (`B25`). |
| `Fix.CTrader` is still `Class1` | `A05` is **stale**. Four files exist. None log on. |
| Phase 4 QUOTE work has started | Logon is the first Phase 4 deliverable. It has not happened. |
| In-memory Fake MT5 “connected” is related | Different venue. Also demo (`C07`). |

Safe-by-absence of send is **good** for money. It is **not** a connectivity certificate.

---

## 5. What would flip this file (coding tasks; not this agent)

Minimum to change the verdict from **NOT PROVEN** to **DIAGNOSTIC LOGON RECORDED** (still not “stable,” still not execution):

1. Stop the lie: seeder status = `Disconnected`; worker must not assign `LoggedOn` / `ReadyForMarketData`. Dashboard healthy bits come from a session object, not from `LastInboundAt = UtcNow`.
2. Bind `CTraderFixOptions` from env (`CTRADER_FIX_*`). Keep `REAL_COPY_EXECUTION_ENABLED=false`. Add `CTRADER_FIX_DIAGNOSTIC_LOGON_ONLY=true` for the first live run.
3. Pin official `QuickFIXn.Core` + `QuickFIXn.FIX44` **1.14.1** (`A35`). Load `FIX44-CSERVER.xml`. Two SessionSettings, two stores, TLS 5211 / 5212. **No** `TcpClient` engine. **No** `QuickFix.Net`.
4. Single-operator run against 1369850 with production TRADE send **off**. Persist the §3.6 record for **each** session (headers as sent, inbound `35=A` or `35=5`+`58`, result enum).
5. Attach that record (redact 554) under `reports/` with command, timestamp, host, port, TLS, and SHA-256 of the event file. Only then may a **successor** of this document say “diagnostic Logon recorded.”
6. “Stable” still needs soak + isolation + no second TRADE owner (`A46`) + reconnect without a send-queue flush. That is a later successor, not a silent edit of C43.

Until step 4 exists on disk, **live cTrader FIX Logon is not proven.**

Forbidden as “proof”:

- Inserting `LoggedOn` in SQL or the seeder.
- Pointing the dashboard at the seeder.
- Checking in `SimulateLogonSuccess` output.
- Resolving DNS for `live-us-eqx-01.p.c-trader.com` without a FIX Logon.
- A passing unit test that never opens a socket.
- Flipping `TargetCompId` between `cServer` and `CSERVER` in C# without a recorded acceptor reply.

---

## 6. Residual risks (do not paper over)

1. **Fake health is worse than a blank dashboard.** Operators can believe Pepperstone 1369850 is logged on. Item 1 of §70 cannot pass until this forge is removed (`A101` §19).
2. **Header case is still a live unknown.** Issued form `cServer` vs RoE `CSERVER`. The only legal resolver is a diagnostic Logon that records tag 56 **as sent** and the acceptor’s reply. Do not silently mutate (`B27`).
3. **Live hostname as C# default** makes a future naive initiator dangerous. Tests must not inherit `*.c-trader.com`.
4. **No TRADE lease.** Two workers after a naive Logon would dual-connect; FAQ: duplicate ExecutionReports. Do not live-Logon TRADE until `A46` exists.
5. **Password still unset in-repo.** A “failed Logon” from empty 554 is still not this file’s job; do not attempt it from a coding-forbidden honesty agent.
6. **`A05` / `A08` stale snapshots** describe `Class1` / 1 Hz log loop. Implementers must use **this file + `B05` + `C07` + `C19`** for current shape.

---

## 7. One-page operator view

```text
C43  live cTrader FIX Logon                         2026-08-18
==============================================================
QUOTE TLS 5211 35=A exchanged with cServer          NO
TRADE TLS 5212 35=A exchanged with cServer          NO
A25 §3.6 LOGON_OK (both sessions)                   MISSING
QuickFIX/n initiator                                MISSING
TcpClient / SslStream FIX engine                    ABSENT (correct)
CTraderQuoteSession / CTraderTradeSession           MISSING
fix_session_events                                  MISSING
tests/Fix                                           ABSENT
.env / FileStore / capture                          ABSENT
--------------------------------------------------------------
DemoSeeder TRADE LoggedOn                           FORGED
Worker stamps LoggedOn / ReadyForMarketData         FORGED
Dashboard QuoteHealthy / TradeHealthy               FORGED
Harness SimulateLogonSuccess                        STRING ONLY
--------------------------------------------------------------
Verdict                                             NOT PROVEN
Safe to enable REAL_COPY_EXECUTION_ENABLED          NO
Safe to send 35=D / F / G                           NO
Product source edited by C43                        NO
==============================================================
```

When a later wave records a real diagnostic Logon, write a **dated successor** (do not silently retitle this file to PASS). Cite the event SHA-256, the exact 49/50/56/57 sent, inbound MsgType, and that `35=D` count stayed 0.

---

## 8. Sources

- `D:\Prop\apps\fix-worker\Worker.cs` (SHA-256 `B48033A5…0D48`)
- `D:\Prop\apps\fix-worker\Program.cs` (SHA-256 `05732C24…D7CC`)
- `D:\Prop\apps\fix-worker\appsettings.json`
- `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Parsing\FixMessageParser.cs`
- `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs`
- `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs`
- `D:\Prop\src\Fix.CTrader\bin\Debug\net8.0\TraderIntelligence.Fix.CTrader.deps.json`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (SHA-256 `139D8F87…0BEF`)
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Domain\Entities\FixSessionState.cs`
- `D:\Prop\apps\api\appsettings.json` (`CTrader` block)
- `D:\Prop\.env.example` (template only; `D:\Prop\.env` absent)
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§25–26, §41, §70
- `D:\Prop\reports\swarm\20260818\A25_fix_session_spec.md` §3.6
- `D:\Prop\reports\swarm\20260818\A101_live_fix_acceptance.md` item 1
- `D:\Prop\reports\swarm\20260818\B05_fix_gap.md`
- `D:\Prop\reports\swarm\20260818\C07_workers_review.md`
- `D:\Prop\reports\swarm\20260818\C14_golive_still_fail.md`
- `D:\Prop\reports\swarm\20260818\C19_quickfix_not_wired.md`
- `D:\Prop\reports\swarm\20260818\C21_cserver_grep.md`
- https://help.ctrader.com/fix/specification/
- https://help.ctrader.com/fix/faqs/

---

*End of C43. Product source was not modified. Live cTrader FIX Logon is not proven.*
