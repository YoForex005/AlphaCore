# P500_S031 — No QuickFIX/n: raw `TcpClient`+`SslStream` Logon then dispose is not a TRADE engine

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S031_no_quickfix.md` |
| Agent | P500_S031 (no-QuickFIX / raw-engine gap) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned | Read `src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj` and architecture law *do not write a FIX engine from raw TcpClient*. Product uses raw `TcpClient`+`SslStream` Logon then dispose. No heartbeat loop, no sequence store, no resend. A profitable TRADE session needs a real engine **before** `35=D`. |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Secret values printed | **None.** Password slot named only (`CTRADER_FIX_PASSWORD` / tag **554** not quoted). |

**Honesty rule:** a one-shot TLS write of `35=A` that returns `LoggedOn=true` and then **disposes the socket** is **not** a FIX session. Advertising `108=30` is **not** a heartbeat loop. Columns `InboundSeq`/`OutboundSeq` on a row the probe never updates are **not** a sequence store. `SAFE_BY_ABSENCE` of `35=D` is capital-safe and is **not** a §70 pass. Official QuickFIX/n is `QuickFIXn.Core` + `QuickFIXn.FIX44` **1.14.1** (`A35`). Unofficial `QuickFix.Net` is a different family.

Classification vocabulary: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE` / `SAFE_BY_ABSENCE` / `FORBIDDEN_PATH`.

---

## 0. Verdict (binding)

**CONFIRMED. There is no QuickFIX/n (and no unofficial QuickFix) on `TraderIntelligence.Fix.CTrader.csproj`. The live path is a raw `TcpClient` + `SslStream` Logon probe that writes one `35=A`, reads one reply, then disposes. That violates architecture §5.8. It cannot carry a profitable TRADE session. Do not add `35=D` on this socket.**

| Assigned question | Measured answer |
|---|---|
| Does the csproj reference official QuickFIX/n? | **No.** Zero `QuickFIXn.Core` / `QuickFIXn.FIX44` / any `QuickFIXn.*`. |
| Does the csproj reference unofficial `QuickFix.Net`? | **No.** Worktree line is gone. Restore graph has no QuickFix package. |
| What is the live “engine”? | **`CTraderFixSession.TryLogonAsync`**: `new TcpClient()` → `SslStream` → `BuildLogon` `(35, "A")` → one `WriteAsync` → one `ReadAsync` → **return** (dispose via `using` / `await using`). |
| Heartbeat loop? | **No.** Tag `108=30` is advertised. `HeartbeatIntervalSec` is a POCO default. Zero `35=0` senders, zero TestRequest (`35=1`) handlers, no `while` after Logon. Hosted service `ExecuteAsync` is **one-shot** (no periodic loop). |
| Sequence store? | **No.** Local `var seq = 1` only. `FixSessionState.InboundSeq`/`OutboundSeq` exist as EF columns; persist **does not write them**. No file/store reset policy beyond `141=Y` on every probe. |
| Resend? | **No.** Zero `35=2` / `ResendRequest` / gap-fill / PossDup. |
| Live `35=D` / `NewOrderSingle`? | **`SAFE_BY_ABSENCE`.** Only outbound MsgType is `(35, "A")`. Hosted log line says *NewOrderSingle still disabled*. `RealCopyEnabled` forced **false**. |
| Can this path become a profitable TRADE session? | **No.** A real initiator (A35 QuickFIX/n + cTrader RoE dictionary) must own the socket **before** any `35=D`. |

| Slice | Class |
|---|---|
| Official QuickFIX/n 1.14.1 pair | **`MISSING`** |
| Unofficial `QuickFix.Net` on this csproj | **absent** (correct; A35 forbids it) |
| Raw `TcpClient`+`SslStream` Logon | **`FORBIDDEN_PATH`** vs §5.8 — exists as a probe only |
| Persistent FIX session (keep-alive) | **`MISSING`** — sockets disposed on return |
| Heartbeat / TestRequest loop | **`MISSING`** |
| Sequence store / persist-before-send | **`MISSING`** (columns exist, unused by the probe) |
| ResendRequest / replay | **`MISSING`** |
| cTrader RoE data dictionary XML | **`MISSING`** |
| `SocketInitiator` / `IApplication` / `SessionSettings` | **`MISSING`** |
| `35=D` builder / send | **`SAFE_BY_ABSENCE`** |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **false**; runtime pin **false** |
| Cert validation `(_,_,_,_) => true` | **`UNSAFE`** as production TLS policy |
| In-process `FixSimulationHarness` / `FixMessageParser` | **EXISTS** — pipe strings, not a session |
| `CTraderQuoteService` | in-memory SecurityList map — **no wire** |
| `FixSessionOwnership` | in-memory lock stub — **not** a session |

C19 / D52 said product C# had **zero** `TcpClient` hits. **That is stale.** Current `CTraderFixSession.cs` **is** the raw engine the architecture told us not to write.

---

## 1. Binding law

Architecture `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§5 item 8**:

> **Do not write a FIX engine from raw TcpClient unless absolutely necessary.**
> Prefer a mature FIX engine such as QuickFIX/n with a cTrader-specific Rules-of-Engagement dictionary/configuration.

Sibling law that the probe also fails:

| Section | Requirement | Probe? |
|---|---|---|
| §5.7 / §27 | Two **independent** sessions with connection, **message sequence**, **heartbeat**, last in/out, reconnect, metrics | Two one-shot connects. No seq/hb/reconnect objects. |
| §28 | Single-active TRADE ownership + reconcile before new intents | `FixSessionOwnership` is an unused in-memory stub. Probe does not take a lease. |
| §29 | Logon **and** Logout, Heartbeat, TestRequest, **ResendRequest / sequence**, Reject, then MD/NOS | Probe implements **Logon request + one reply parse** only. No Logout. |
| §5.8 / A35 | `QuickFIXn.Core` + `QuickFIXn.FIX44` **1.14.1** | **Not referenced.** |
| A35 | Do **not** add `QuickFix.Net` / `QuickFIXn.FIX4.4` / FIX5 / FIXT | Worktree csproj has none of these (good). |
| §70 / A101 | Live FIX acceptance (kept session, heartbeats, no `35=D` until gates) | **0/14** still. A `LoggedOn` bool after dispose is **not** a kept session. |

A35 pin (quoted, **not implemented**):

```xml
<PackageReference Include="QuickFIXn.Core" Version="1.14.1" />
<PackageReference Include="QuickFIXn.FIX44" Version="1.14.1" />
```

---

## 2. `TraderIntelligence.Fix.CTrader.csproj` (measured)

Path: `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj`

| Kind | What is there |
|---|---|
| TFM | `net8.0` |
| ProjectReference | `..\Domain\TraderIntelligence.Domain.csproj` |
| ProjectReference | `..\Application\TraderIntelligence.Application.csproj` |
| PackageReference | `Microsoft.Extensions.Hosting.Abstractions` 8.0.1 |
| PackageReference | `Microsoft.Extensions.Configuration.Abstractions` 8.0.0 |
| PackageReference | `Microsoft.Extensions.Logging.Abstractions` 8.0.2 |
| PackageReference | `Microsoft.EntityFrameworkCore` 8.0.4 |
| QuickFIX/n | **none** |
| QuickFix.Net | **none** |

Restore (`obj\project.assets.json`, `*.nuget.dgspec.json`): no `QuickFix*` / `QuickFIXn*` package ids. Transitive Application pull is FluentValidation **11.9.2** plus the Microsoft.* stack above.

`obj\Debug\net8.0\TraderIntelligence.Fix.CTrader.csproj.FileListAbsolute.txt` output assemblies: this project + Application + Domain only. **No** `QuickFix.dll` / `QuickFIXn*.dll`.

Product C# under `src/`, `apps/`, `tests/`: **0** `using QuickFix`, **0** `SocketInitiator`, **0** `IApplication`, **0** `SessionSettings`, **0** `IInitiator`, **0** `DataDictionary` (FIX).

---

## 3. The probe is Logon-then-dispose (not a session)

File: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines).

Lifecycle, in order:

1. `using var tcp = new TcpClient();`
2. 20 s linked timeout.
3. `tcp.ConnectAsync(host, sslPort, …)`
4. `await using var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);` — **accepts any remote certificate**.
5. `AuthenticateAsClientAsync` TLS 1.2 / 1.3, `TargetHost = host`.
6. `var seq = 1;` — **not loaded** from `FixSessionState.OutboundSeq`.
7. `BuildLogon(…, seq)` → body starts `(35, "A")`, `(34, "1")`, `(108, "30")`, `(141, "Y")`, `(553, username)`, `(554, password)`.
8. One `ssl.WriteAsync` + `FlushAsync`.
9. One `ssl.ReadAsync` into a **4096-byte** buffer (no SOH framing loop, no leftover, no multi-message).
10. If tag `35==A`, return `LoggedOn=true`, `Status="LoggedOn"`.
11. Method returns → `using` / `await using` **dispose SslStream and TcpClient**. The venue now sees a drop with **no Logout (`35=5`)**.

Hosted caller: `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`

- Password gate: skip if `CTRADER_FIX_PASSWORD` empty or contains `<SECRET>`.
- QUOTE: port **5211**, sub IDs default `QUOTE`/`QUOTE`.
- TRADE: port **5212**, sub IDs default `TRADE`/`TRADE`.
- Copies booleans into `LiveRuntimeStatus`; **`_runtime.RealCopyEnabled = false`**.
- Logs `NewOrderSingle still disabled`.
- Persist is **update-only** on existing `FixSessionState` rows (reflection lookup of type name `TraderDbContext`). Writes Host/Port/Status/LastError/timestamps. **Does not write `InboundSeq` / `OutboundSeq`.**
- `ExecuteAsync` then **ends**. There is no `while (!stoppingToken.IsCancellationRequested)` heartbeat, no reconnect, no second Logon.

Two independent **probes** are not two independent **sessions**. After dispose, sequence state on the venue (if the Logon was accepted) is abandoned. The next start will send `141=Y` + `34=1` again. That is a reset-every-boot client, not a recoverable TRADE initiator.

---

## 4. Heartbeat / sequence / resend — measured absence

| Engine capability | Where it would live | Measured |
|---|---|---|
| HeartBtInt advertised | `BuildLogon` tag **108** = `"30"`; `CTraderFixOptions.HeartbeatIntervalSec = 30` | Advertised only. **`HeartbeatIntervalSec` has 0 runtime readers** outside the POCO. |
| Outbound Heartbeat `35=0` | session loop | **0** product senders. Harness comment only (`FixSimulationHarness` placeholder). |
| Inbound Heartbeat / TestRequest `35=1` | session read loop | **No read loop.** One `ReadAsync`. |
| Logout `35=5` | dispose path | **0** builders. Dispose is TCP drop. |
| Sequence increment | after each send/recv | Hardcoded `seq = 1`. Persist does not touch seq columns. |
| Persist-before-send | outbox / `OutboundSeq++` then write | **MISSING** |
| Independent QUOTE vs TRADE counters | §27 | Two calls, each starts at 1. Shared nothing — also **stored** nothing. |
| ResendRequest `35=2` | gap detect | **0** hits in `Fix.CTrader` product send/recv. |
| SequenceReset `35=4` / PossDup `43` | replay | **0** |
| File store / QuickFIX store | A35 initiator | **MISSING** |

`FixSessionState` (`D:\Prop\src\Domain\Entities\FixSessionState.cs`) has `InboundSeq` / `OutboundSeq`. Seeder stamps both to **1**. The probe never reads or writes those fields. Dashboard DTO surfaces the stale integers. That is UI, not a store.

---

## 5. Why a profitable TRADE session cannot use this path before `35=D`

cTrader TRADE (SSL **5212**) is a **kept** FIX 4.4 initiator:

- Heartbeats (or TestRequest) at the negotiated interval, or the venue drops the session.
- Strict `MsgSeqNum`. After a drop you **resume or reset under RoE**, you do not invent a second `34=1` Logon from a new `TcpClient` and call it the same session.
- ResendRequest on gaps. Duplicate TRADE sessions produce **duplicate execution reports** (architecture §5.9 / cTrader FAQ).
- Execution reports (`35=8`) arrive asynchronously after `35=D`. A client that already disposed cannot receive fills, rejects, or cancel/replace acks.
- Disconnect after send is `EXECUTION_STATE_UNKNOWN` (A42) — **must not** retry as a new `35=D`. This probe has no unknown-state machine on the wire.

Therefore:

```text
raw probe 35=A  →  dispose
        ≠
kept TRADE initiator  →  seq store  →  heartbeat  →  35=D
```

`SAFE_BY_ABSENCE` of `35=D` is the **correct** capital posture. Adding a `NewOrderSingle` assembler on this `SslStream` would be **UNSAFE**: no persist-before-send, no ER listener, no resend, no ownership lease, no logout, cert callback always-true, 4 KiB single read.

Do **not** “just keep the socket open” and hand-write heartbeats. That **is** writing a FIX engine from raw `TcpClient`, which §5.8 forbids unless absolutely necessary. It is not necessary: A35 already pinned official QuickFIX/n 1.14.1.

---

## 6. Adjacent files (not an engine)

| Path | Role | Engine? |
|---|---|---|
| `Sessions/CTraderFixSession.cs` | raw TLS Logon probe | **No** — forbidden path |
| `Hosting/CTraderFixLogonHostedService.cs` | one-shot dual probe + persist + flags | **No** |
| `Configuration/CTraderFixOptions.cs` | host/ports/`cServer`/HB interval/copy flag | config only |
| `Parsing/FixMessageParser.cs` | pipe/`|` unit-test codec + checksum | **No** |
| `Testing/FixSimulationHarness.cs` | in-process string factory | **No** |
| `Services/CTraderQuoteService.cs` | maps SecurityList dicts in memory | **No wire** |
| `Services/FixSessionOwnership.cs` | in-memory fencing stub | **No socket** |

Registered from `src/Infrastructure/DependencyInjection.cs` via `AddHostedService<CTraderFixLogonHostedService>()`. Three hosts that call `AddTraderIntelligence` can each fire two probes. That is the opposite of §28 single-owner TRADE.

---

## 7. What “real engine before 35=D” means (not implemented; product not edited)

Gate order (do not skip):

1. Add **only** A35 packages to this csproj: `QuickFIXn.Core` 1.14.1 + `QuickFIXn.FIX44` 1.14.1. Do not re-add `QuickFix.Net`.
2. cTrader RoE **custom** data dictionary (generic stock `FIX44.xml` is not sufficient — A35 / A36).
3. Two `SocketInitiator` sessions (QUOTE **5211**, TRADE **5212**), independent stores, SSL per QuickFIX/n config (`SSLEnable`, not a hand-rolled `SslStream`).
4. Prove **kept** Logon: heartbeats observed, seq persisted, reconnect/resend exercised in staging. Logout on shutdown.
5. Single-active TRADE lease (§28) + position/order reconcile **before** any execution intent.
6. Keep `RealCopyExecutionEnabled=false` until §68 / §70 / risk / persist-before-send (A42) measure PASS.
7. Only then a guarded `35=D` path. Never on the current probe.

Until step 4 is **measured**, dashboard `LoggedOn` after this hosted service is a **probe result**, not a live session.

---

## 8. Capital / send posture

| Item | State |
|---|---|
| Outbound MsgType in product Fix.CTrader | **`A` only** |
| `35=D` / `NewOrderSingle` encoder | **0** |
| Copy flag | **false** (POCO + DI + hosted overwrite) |
| Risk to venue capital from this process | **NONE** (`SAFE_BY_ABSENCE`) |
| Risk if someone adds `35=D` to `CTraderFixSession` | **HIGH** — no engine |

This slot did **not** open a live socket and did **not** print secrets.

---

## 9. Files read (absolute)

- `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Parsing\FixMessageParser.cs`
- `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs`
- `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs`
- `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs`
- `D:\Prop\src\Fix.CTrader\obj\project.assets.json`
- `D:\Prop\src\Fix.CTrader\obj\TraderIntelligence.Fix.CTrader.csproj.nuget.dgspec.json`
- `D:\Prop\src\Fix.CTrader\obj\Debug\net8.0\TraderIntelligence.Fix.CTrader.csproj.FileListAbsolute.txt`
- `D:\Prop\src\Domain\Entities\FixSessionState.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` (§5.8, §27–§29)
- Pins: `D:\Prop\reports\swarm\20260818\A35_quickfixn_packages.md`, `C19_quickfix_not_wired.md` (stale on TcpClient), `D52_qfn.md` (stale on TcpClient), `A011_fix_persist.md`

---

## 10. One-line answer

**No QuickFIX/n. Product Logon is raw `TcpClient`+`SslStream` write `35=A` then dispose — not a heartbeat/seq/resend engine. Keep `35=D` off until a real initiator exists.**
