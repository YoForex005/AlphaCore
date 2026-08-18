# P500_S049 — FIX 4.4 Heartbeat `35=0` is advertised (`108=30`) and never sent; TRADE cannot safely send

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S049_no_heartbeat.md` |
| Agent | P500_S049 (senior FIX session / keep-alive) |
| Slot | **S049** |
| Date | 2026-08-18 |
| Assigned | FIX 4.4 needs `35=0` heartbeat. Session builder `HeartBtInt` `108=30` but socket not kept. A TRADE session that cannot heartbeat cannot safely send. Do not edit product. |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Secrets printed | **No.** Config key names only. Password never quoted. |
| Method | Full read of `CTraderFixSession`, `CTraderFixLogonHostedService`, `CTraderFixOptions`, `FixSimulationHarness`, `CTraderQuoteService`, `FixSessionState`, `LiveRuntimeStatus`, `apps/fix-worker/Worker.cs`. Product-tree grep for `108`, `HeartBtInt`, `HeartbeatIntervalSec`, `(35, "0")`, `35=1`, `TestRequest`, `BuildHeartbeat`. Cross-check A25 / A33 RoE heartbeat law. Nothing from memory. |

**Honesty rule:** writing `108=30` on Logon is a **contract**, not a timer. A disposed `TcpClient` cannot emit `35=0`. Dashboard `LastInboundAt`/`LastOutboundAt` stamped at dispose is **not** a heartbeat. `HeartbeatIntervalSec = 30` on a POCO that no live path reads is **not** a keep-alive. `SAFE_BY_ABSENCE` of `35=D` is the current capital outcome. Do **not** tick Architecture §29 / A101 heartbeat-soak from this file.

---

## 0. Verdict (binding)

**CONFIRMED. The only outbound FIX this process can write is one-shot `35=A` Logon with `108=30`. There is no `35=0` builder on a live socket, no idle timer, no `35=1` TestRequest reply, and the TCP/TLS handle is disposed before the first negotiated interval elapses. A TRADE session that cannot heartbeat cannot safely send.**

| Claim | Result | Class |
|---|---|---|
| Logon advertises `HeartBtInt` | **Hardcoded `(108, "30")`** in `BuildLogon` | contract on the wire |
| Client implements that contract | **No** | `MISSING` keep-alive |
| Outbound `35=0` on `*.c-trader.com` | **0 writes** | `MISSING` |
| Outbound `35=1` / inbound `112` echo | **0** | `MISSING` |
| Socket lifetime after Logon | **`using TcpClient` + `await using SslStream` dispose on return** | socket not kept |
| `HeartbeatIntervalSec` used | **1 declaration, 0 readers** | dead config |
| Hosted service loop | **one-shot QUOTE then TRADE, then exit `ExecuteAsync`** | no session thread |
| Persist timestamps | **`LastInboundAt`/`LastOutboundAt` = `UtcNow` after dispose** | fake liveness |
| TRADE FAQ exemption for missing `35=0` | **None** (QUOTE-while-streaming only) | A33 / A25 |
| May send `35=D` on this TRADE object | **No** | capital gate |

One-line:

```text
BuildLogon writes 108=30, then disposes TLS.
NO 35=0 timer. NO 35=1 handler. NO kept socket.
TRADE that cannot heartbeat cannot safely send.
```

Operating mode (honest):

```text
ALLOW:  35=A diagnostic Logon (session proof only)
FORBID: treat LoggedOn as a live session
        send 35=D/F/G on TRADE
        claim heartbeat soak / A101 stay-logged-on
        trust LastInboundAt as venue traffic
```

---

## 1. FIX 4.4 / cTrader law (what `108=30` means)

Session-layer Heartbeat is not optional once Logon negotiates a positive interval.

From this lab’s own RoE extract (`A33_ctrader_fix_send_recv.md`, `A25_fix_session_spec.md`, `A32_ctrader_fix_specification.md`):

| Rule | Consequence |
|---|---|
| `108` is a required Logon integer. Default **30**. `0` = no heartbeat required. | Advertising `108=30` **binds** the client to send `35=0` at least every 30 s of idle outbound. |
| Heartbeats are sent by **both** sides “to confirm a live connection.” | Client silence after Logon is a dead link, not a quiet healthy TRADE. |
| Recurring `35=0` **or** `35=0` as a reply to `35=1` (must echo `112`) | Need a timer **and** a TestRequest handler. |
| QUOTE may omit inbound `35=0` while MD streams | **QUOTE-only** FAQ. Liveness = last venue print / quote age. |
| TRADE has **no** such exemption | Idle TRADE still needs the negotiated heartbeat unless `108=0`. |

Architecture §29 minimum workflows start with:

```text
Logon, Logout, Heartbeat, TestRequest
```

A25 §2.6: Logon / Logout / Heartbeat / TestRequest / Resend / Reject / SequenceReset are allowed on **both** QUOTE and TRADE. NewOrderSingle is TRADE-only **and** only if §15 allows.

**Implication for send:** even if a `35=D` builder existed tomorrow, writing it on a socket that (a) no longer exists or (b) will be dropped for heartbeat miss is how you get a ghost order, a half-fill with no ER stream, or a reconnect/`141=Y` that loses in-flight state. That is not “profit.” That is unreconciled capital.

---

## 2. Session builder: `108=30` is a constant, not a clock

The only live assembler is `CTraderFixSession.BuildLogon`:

```89:109:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
    private static string BuildLogon(
        string sender, string target, string senderSub, string targetSub,
        string username, string password, int seq)
    {
        var sendingTime = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var fields = new List<(int tag, string value)>
        {
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
        };
        return Assemble(fields);
    }
```

Facts from that list:

| Tag | Value | Meaning |
|---|---|---|
| 35 | `A` | Logon. **Only** outbound MsgType assembled for the socket. |
| 108 | **literal `"30"`** | Not `CTraderFixOptions.HeartbeatIntervalSec`. Not configurable on this path. |
| 141 | `Y` | Seq reset on establish. A later reconnect is a **new** session, not a resume. |
| 0 / 1 / 112 | **absent** | No Heartbeat, no TestRequest, no TestReqID. |

There is no `BuildHeartbeat`, no `Assemble` call with `(35, "0")`, no increment of `seq` past `1`.

---

## 3. Socket is not kept

`TryLogonAsync` is a connect → write Logon → one read → return. Disposal is structural:

```35:50:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
            using var tcp = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(20));
            await tcp.ConnectAsync(host, sslPort, timeoutCts.Token);
            await using var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
            // ...
            var seq = 1;
            var logon = BuildLogon(...);
            var bytes = Encoding.ASCII.GetBytes(logon);
            await ssl.WriteAsync(bytes, timeoutCts.Token);
            await ssl.FlushAsync(timeoutCts.Token);
```

Then **one** `ssl.ReadAsync` (20 s bound). If `35=A`, it returns `LoggedOn = true` and **leaves the method**. `using` / `await using` close TCP and TLS.

There is:

- no `while (!ct.IsCancellationRequested)` read loop
- no `PeriodicTimer` / `Task.Delay(HeartBtInt)` send of `35=0`
- no last-outbound / last-inbound idle clocks
- no handling of inbound `35=0` or `35=1`
- no Logout `35=5` before dispose (unclean drop after promising 30 s heartbeats)

Hosted caller does this **twice** (QUOTE `:5211`, TRADE `:5212`) and then **ends** `ExecuteAsync`. It is not a session host. It is a diagnostic probe that stamps runtime flags.

```48:71:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(/* :5211 QUOTE */);
        var trade = await CTraderFixSession.TryLogonAsync(/* :5212 TRADE */);
        _runtime.Quote.LoggedOn = quote.LoggedOn;
        // ...
        _runtime.Trade.LoggedOn = trade.LoggedOn;
        _runtime.RealCopyEnabled = false;
```

`LoggedOn = true` here means “the acceptor answered `35=A` once,” not “a FIX session is up.”

---

## 4. Grep census: no keep-alive anywhere in product

| Symbol | Product hits | Meaning |
|---|---|---|
| `(108, "30")` | `CTraderFixSession.BuildLogon`; harness inbound Logon echo | advertised only |
| `HeartbeatIntervalSec` | **only** `CTraderFixOptions.cs` L37 default `30` | **never read** |
| `(35, "0")` / `35=0` builder used for send | **0** in live path | harness `SimulateDisconnect` uses `35=0` as a **placeholder**, not a heartbeat engine |
| `35=1` / `TestRequest` / tag `112` send or echo | **0** in `src/Fix.CTrader` product types | `MISSING` |
| `BuildHeartbeat` / `HeartbeatMessage` | **0** | `MISSING` |
| `TcpClient` / `SslStream` / `WriteAsync` | **one file**, one write | Logon only |
| QuickFIX/n `HeartBtInt` session | **0** packages | no engine to inherit a timer |

`CTraderFixOptions.HeartbeatIntervalSec` is a POCO field. The live logon path does not bind `IOptions<CTraderFixOptions>` for this value. Changing the POCO would not change the wire (`108` is a string literal).

`FixSimulationHarness.SimulateDisconnect` comments `35=0` as “Heartbeat (used as placeholder).” That is test fiction. It is not scheduled, not written to cTrader, and does not echo `112`.

`apps/fix-worker/Worker.cs` is a 15 s DB stamper that forces both rows to `Disconnected` / “No live … socket.” It does not open FIX. It is the opposite of a heartbeat: it **denies** a live socket every tick.

---

## 5. Fake liveness vs venue liveness

After the sockets are gone, persist writes **now** onto both directions:

```104:108:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
            row.Status = result.LoggedOn ? FixSessionStatus.LoggedOn : FixSessionStatus.Error;
            row.LastError = result.LastError;
            row.LastInboundAt = DateTimeOffset.UtcNow;
            row.LastOutboundAt = DateTimeOffset.UtcNow;
            row.UpdatedAt = DateTimeOffset.UtcNow;
```

`FixSessionState` has no `LastHeartbeatAt` / `LastTestRequestAt`. Dashboard (`EfDashboardQueries`) surfaces `LastInboundAt` / `LastOutboundAt`. Those stamps are **worker clocks at dispose**, not `35=0` / `35=1` / ER / MD.

A101 already names this class of defect: worker stamps `LastInboundAt` + `LoggedOn` = **fake heartbeat**. This slot re-confirms it on current source.

`LiveRuntimeStatus.FixLiveStatus` holds `LoggedOn` / `Status` / `LastError` / `UpdatedAt` only. No heartbeat fields. After the hosted service returns, those flags are frozen until process restart.

---

## 6. Why TRADE cannot safely send

### 6.1 Protocol

Negotiated `108=30` + no client `35=0` ⇒ acceptor will (on its schedule) send `35=1`, then drop. TRADE has no MD stream to hide behind. First idle half-minute after Logon is already past our socket lifetime.

### 6.2 Socket identity

Even if Logon succeeded at T0, the TRADE `SslStream` is dead at T0+ε. Any later `35=D` would require a **new** TCP/TLS + new Logon (`141=Y` in this builder) + new seq=1. That is not “send on the logged-on session.” That is a new session with no ER subscription, no order-status replay completed, no ownership lease, and no heartbeat either.

### 6.3 Capital

Sending without a kept session means:

| Failure | Capital effect |
|---|---|
| Venue already dropped TRADE | reject / silent void; operator thinks they are live |
| Order accepted then socket dies | fills exist at broker; this process has no ER loop |
| Re-Logon `141=Y` mid-flight | seq reset; resend/gap handling **does not exist** |
| Two TRADE owners (no lease) | duplicate ERs / double send (A25 §2.5) |

P500_S002 already proved **no `35=D` builder**. This slot adds the independent gate: **even after a builder exists, send is unsafe until a real heartbeat/TestRequest engine owns a kept TRADE socket.**

`RealCopyEnabled` is forced `false` after the probe. Leave it false. Heartbeat is a **precondition** of send, not a follow-up ticket after the first live order.

### 6.4 QUOTE is also not kept (different liveness rule)

QUOTE Logon also advertises `108=30` and also disposes. FAQ says inbound `35=0` may be omitted **while quotes stream**. Quotes do not stream here (`CTraderQuoteService` tag lists are uncalled; no `35=V` on the wire). So QUOTE is also dead. Do not treat missing QUOTE heartbeat as the FAQ case. Treat it as **no session**.

---

## 7. What a later keep-alive must include (do not implement in this slot)

This file does **not** edit product. When a later slot is allowed to implement session layer, the minimum that makes `108=30` honest:

1. **Keep** the TLS socket on a long-lived session object (separate QUOTE vs TRADE; independent seq / clocks).
2. Idle outbound ≥ `HeartBtInt` → send `35=0` (header only).
3. Inbound `35=1` → immediate `35=0` with the **same** `112`.
4. Optional outbound `35=1` with incremental `112`; expect matching heartbeat.
5. Missed inbound (TRADE: any admin/app; QUOTE: any MD **or** admin) → logout / drop / reconnect with recorded reason. Do not stamp `UtcNow` as inbound.
6. Read `HeartBtInt` from config; write that same integer on Logon. `0` is allowed by RoE but **must not** be used on TRADE as a shortcut to skip keep-alive.
7. Clean `35=5` on shutdown.
8. Persist last **venue** inbound/outbound and last heartbeat/test — never dispose-time `UtcNow`.
9. **Still** refuse `35=D` until recon + live quotes + sizing + ownership + flags (existing A100/A101). Heartbeat soak is necessary, not sufficient.

QuickFIX/n already implements 2–5 if configured (`HeartBtInt=30`). Raw `TcpClient` as it stands does not.

---

## 8. Binding gate

```text
P500_S049 FAIL (expected): heartbeat not implemented
  108=30 advertised
  socket disposed
  35=0 never sent
  TRADE send = UNSAFE
```

Do not:

- add `35=D` “just to see”
- set `RealCopyEnabled` / `RealCopyExecutionEnabled` true
- treat `LoggedOn` or `LastInboundAt` as proof the session is alive
- implement heartbeat in this slot (assigned: report only)

Product source was not modified.
