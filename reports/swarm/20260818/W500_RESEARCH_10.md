# W500_RESEARCH_10 — `CTraderFixSession.cs` live `35=D` / NewOrderSingle search

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_10.md` |
| Slot | **10** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (read also `D:\Projects\YoPips\Backend\C++ Backend PropFirm` for FIX tokens) |
| Assigned | Search `CTraderFixSession.cs` for `35=D` or `NewOrderSingle`. **Verdict FAIL if live send exists.** Goal: fetch ALL Achiever+Starwave groups and ALL manager traders; **copy to cTrader must not send live orders yet (no loss).** |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Binding law | Architecture §§32–34 / §41 / §68 / §70; A25; A32 (RoE `35=D`); A42 (no retry as `35=D`); A101 (live `35=D` forbidden until 14/14); E002 / E034 (`SAFE_BY_ABSENCE`) |
| Siblings (do not treat as this file) | A003 (FIX no-loss gate), E002 (flag default + no sender), E034 (product-C# `35=D` census), A011 (persist / `35=A` only), W500_SLICE_108 / 133 / 158 (same file, other angles) |
| Secret handling | `CTRADER_FIX_PASSWORD` **named only**. Value not read from `.env` and not printed. Tag 554 is assembled in-process; this report does not dump a live frame. |
| Method | Full `read_file` of `CTraderFixSession.cs` (135/135 lines). Grep this file + `Fix.CTrader` + product `*.cs` for `35=D`, `(35, "D")`, `NewOrderSingle`, `OrderQty`, `ClOrdID`. Read sole caller `CTraderFixLogonHostedService`, options, fix-worker, DI, FSM, quote service, parser, harness. Grep YoPips C++ backend `src/` for the same tokens. **No product edit. No TLS opened. No order sent.** |

**Honesty rule:** a log line, XML comment, or helper *name* containing `NewOrderSingle` is **not** a FIX `MsgType=D` builder. `35={msgType}` in a reject `LastError` is an inbound classifier, not an outbound D. `ToString("000")` on checksum tag 10 is a numeric format, not tag 35. Absence of `35=D` is **`SAFE_BY_ABSENCE`**, not a unit-tested refuse-on-LoggedOn-TRADE gate. Do **not** tick Architecture §68 / §70 from this file. TLS Logon `35=A` **can** leave this process; that is **not** a live order.

---

## 0. Verdict (binding)

**PASS — no live send exists.**

`CTraderFixSession` cannot emit NewOrderSingle. The only outbound `MsgType` this type can assemble is **`A` (Logon)**. Assigned FAIL condition (“live send exists”) is **false**.

| Claim | Result | Class |
|---|---|---|
| Literal `35=D` in `CTraderFixSession.cs` | **0 hits** | **MISSING** builder |
| `(35, "D")` / `new(35, "D")` / `MsgType="D"` in this file | **0 hits** | **MISSING** |
| `NewOrderSingle` token in this file | **0 hits** | **MISSING** |
| `OrderQty` / tag 38 / `ClOrdID` / tag 11 in this file | **0 hits** | **MISSING** |
| Outbound tag-35 values in this file | **exactly one:** `(35, "A")` at L96 | Logon only |
| `ssl.WriteAsync` call sites in this file | **1** (L49) — bytes of `BuildLogon` | one-shot probe |
| Socket kept after return | **No** — `using TcpClient` + `await using SslStream` dispose before return | no TRADE keep-alive |
| Sole product caller | `CTraderFixLogonHostedService` QUOTE 5211 + TRADE 5212 | also no `35=D` |
| `RealCopyExecutionEnabled` / `RealCopyEnabled` | default **false**; hosted service **forces false** after logon | fail-closed |
| QuickFIX/n / `SendToTarget` in `Fix.CTrader.csproj` | **absent** | no initiator library |
| YoPips C++ backend `CTraderFixSession` / `35=D` / `NewOrderSingle` | **0 hits** | not a second sender |
| Live send if this process starts now | **`35=A` possible; `35=D` impossible** | **`SAFE_BY_ABSENCE`** |
| Slot FAIL condition (“live send exists”) | **Not met** | **PASS** |

One-line:

```text
CTraderFixSession.cs: NewOrderSingle=0, 35=D=0; only wire write is BuildLogon (35=A); sockets disposed; copy cannot lose capital from this type.
```

Do **not** add a `35=D` builder in this slot. Do **not** set `REAL_COPY_EXECUTION_ENABLED=true`. Fetching Achiever+Starwave groups is a **Manager read** path; it is not licensed to send Pepperstone orders.

---

## 1. Assigned file (measured)

Path: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`

| Metric | Value |
|---|---|
| Lines read | **135 / 135** (EOF) |
| Types | `CTraderFixSessionResult` (L10–17), `CTraderFixSession` static (L19–135) |
| Public API | **`TryLogonAsync` only** |
| Private helpers | `BuildLogon`, `Assemble`, `Extract` |
| Transport | `TcpClient` + `SslStream` (`Tls12 \| Tls13`), 20 s cancel |
| Writes | **one** `WriteAsync` of `BuildLogon` ASCII |
| Reads | **one** 4 KiB `ReadAsync`; classify inbound tag 35 |
| Result statuses | inbound `35=A` → `LoggedOn`; other 35 → `Error`; exception → `Disconnected` |

### 1.1 Assigned grep on this file — **zero live-send tokens**

| Pattern | Hits in `CTraderFixSession.cs` |
|---|---:|
| `35=D` | **0** |
| `(35, "D")` / `(35, 'D')` / `new(35, "D")` | **0** |
| `MsgType="D"` / `MsgType = "D"` | **0** |
| `NewOrderSingle` | **0** |
| `OrderQty` / `(38,` / tag 38 | **0** |
| `ClOrdID` / `(11,` / tag 11 | **0** |
| `35=F` / `35=G` / `35=H` / `35=AF` / `35=AN` (cancel / replace / status / positions) | **0** |

`35=` appears only as:

| Line | Kind | Live order? |
|---|---|---|
| L55 `Extract(reply, "35")` | inbound parse | **No** |
| L73 `$"Logon rejected 35={msgType} …"` | error text | **No** |
| L96 `(35, "A")` | **outbound Logon** | **No** (session, not order) |

### 1.2 The only outbound builder

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

Body tags are **35, 34, 49, 56, 50, 57, 52, 98, 108, 141, 553, 554**. There is no Side (54), OrdType (40), OrderQty (38), Symbol (55), ClOrdID (11), or TimeInForce (59). `Assemble` is private and is only called from `BuildLogon`. There is no `BuildNewOrderSingle`.

### 1.3 One-shot socket — cannot keep a TRADE initiator for a later D

```33:50:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
        try
        {
            using var tcp = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(20));
            await tcp.ConnectAsync(host, sslPort, timeoutCts.Token);
            await using var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = host,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            }, timeoutCts.Token);

            var seq = 1;
            var logon = BuildLogon(senderCompId, targetCompId, senderSubId, targetSubId, username, password, seq);
            var bytes = Encoding.ASCII.GetBytes(logon);
            await ssl.WriteAsync(bytes, timeoutCts.Token);
```

`seq` is the literal `1`. There is no second `WriteAsync`, no heartbeat loop, no sequence store, and no public send method. When `TryLogonAsync` returns, `using` / `await using` dispose TCP+TLS. A later copy intent has **no socket** to hang a `35=D` on.

---

## 2. Sole product caller (still no `35=D`)

`CTraderFixSession` is referenced only from `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` (product `*.cs` grep: 2 `TryLogonAsync` calls + persist signature).

```48:71:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            _config["CTRADER_FIX_QUOTE_SENDER_SUB_ID"] ?? "QUOTE",
            _config["CTRADER_FIX_QUOTE_TARGET_SUB_ID"] ?? "QUOTE",
            username, password, stoppingToken);

        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            _config["CTRADER_FIX_TRADE_SENDER_SUB_ID"] ?? "TRADE",
            _config["CTRADER_FIX_TRADE_TARGET_SUB_ID"] ?? "TRADE",
            username, password, stoppingToken);
        // ...
        _runtime.RealCopyEnabled = false;

        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
            quote.LoggedOn, trade.LoggedOn, account);
```

| Fact | Measured |
|---|---|
| Password missing / `<SECRET>` placeholder | logon **skipped**; no TCP |
| Ports | QUOTE **5211**, TRADE **5212** (SSL; not 5201/5202) |
| Tag 553 username | integer **account id**, not SenderCompID |
| `NewOrderSingle` mention | **information log only** — “still disabled” |
| Copy flag after probe | **`_runtime.RealCopyEnabled = false`** (forced, not read from env) |
| Persist | updates existing `FixSessionState` rows; **does not encode FIX** |

DI registers this hosted service (`DependencyInjection.cs` L56) and pins `LiveRuntimeStatus.RealCopyEnabled = false` (L38–42) with comment “Live NewOrderSingle is not implemented.”

---

## 3. Adjacent Fix.CTrader surfaces (none send D)

Product `.cs` under `D:\Prop\src\Fix.CTrader` (bin/obj ignored):

| File | Role | Emits `35=D`? |
|---|---|---|
| `Sessions\CTraderFixSession.cs` | TLS logon probe | **No** — only `35=A` |
| `Hosting\CTraderFixLogonHostedService.cs` | caller + persist + log | **No** — name in log |
| `Configuration\CTraderFixOptions.cs` | `RealCopyExecutionEnabled = false` | **No** — XML comment |
| `Parsing\FixMessageParser.cs` | generic pipe/SOH codec | **No** — no D caller |
| `Services\CTraderQuoteService.cs` | in-memory `35=y` / `35=V` tag lists | **No socket** |
| `Services\FixSessionOwnership.cs` | in-memory fence | **No FIX** |
| `Testing\FixSimulationHarness.cs` | in-process `A`/`3`/`8`/`y`/`X`/`0` | **No venue**; ClOrdID only on **simulated ER `35=8`** |
| `TraderIntelligence.Fix.CTrader.csproj` | net8.0; Domain + Application; Hosting/Config/Logging/EF | **no QuickFIX/n** |

Every product `(35, "…")` / `new(35, "…")` under `Fix.CTrader`:

| Site | Tag 35 value | Wire? |
|---|---|---|
| `CTraderFixSession.BuildLogon` L96 | **`A`** | **Yes**, one-shot TLS |
| `FixSimulationHarness` L20 | `A` | in-process string |
| harness L34 | `3` | in-process |
| harness L125 | `0` | in-process |
| harness L136 | `y` | in-process |
| harness L153 | `X` | in-process |
| harness L185 | `8` | in-process |
| `CTraderQuoteService` L113 | `y` | tag list only |
| `CTraderQuoteService` L127 | `V` | tag list only |

**No `D`.** Quote-service `y`/`V` lists are never passed to `CTraderFixSession.Assemble` or `WriteAsync`.

---

## 4. Product-wide `NewOrderSingle` (name-only; not this file)

`CTraderFixSession.cs` itself has **zero** `NewOrderSingle` tokens. Product `*.cs` hits elsewhere (none encode tag 35=`D`):

| File:line | Kind | Encodes `35=D`? |
|---|---|---|
| `src\Domain\Execution\ExecutionOrderStateMachine.cs:35` | `MayRetryNewOrderSingle` status predicate (`NotSent`/`Rejected` only) | **No** |
| `src\Fix.CTrader\Configuration\CTraderFixOptions.cs:33` | XML comment on default-OFF flag | **No** |
| `src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs:70` | log “still disabled” | **No** |
| `src\Infrastructure\DependencyInjection.cs:40` | comment; `RealCopyEnabled=false` | **No** |
| `src\Infrastructure\Seeding\BrokerCatalogSeed.cs:105` | TRADE `LastError` string | **No** |
| `src\Infrastructure\Seeding\DemoSeeder.cs:101` | TRADE `LastError` string | **No** |
| `src\Application\Runtime\LiveRuntimeStatus.cs:44` | snapshot copyNote when flag false | **No** |
| `apps\fix-worker\Worker.cs:22,41,46` | log / LastError / warning | **No** — stamps `Disconnected`; no socket |
| `apps\api\Program.cs:68` | recon note “NewOrderSingle still off” | **No** |
| `tests\Unit\ExecutionAndSizingTests.cs:14` | asserts retry helper false after send-state | **No** |

`apps\fix-worker\Worker.cs` reads `CTrader:RealCopyExecutionEnabled` with fallback **false**. If the key is true it **only logs a warning** and still writes `LastError = "No live TRADE socket. NewOrderSingle remains off."` It never calls `CTraderFixSession` and never builds `35=D`.

Literal `35=D` / `(35, "D")` / `MsgType="D"` across `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tests` product `*.cs`: **0**.

---

## 5. YoPips C++ backend (relevant negative)

Grep of `D:\Projects\YoPips\Backend\C++ Backend PropFirm` (`src\`, `src\services\`, `src\http\`) and `D:\Projects\YoPips` for `CTraderFixSession`, `NewOrderSingle`, `35=D`, `FIX.4`:

| Pattern | Hits |
|---|---|
| `CTraderFixSession` | **0** |
| `NewOrderSingle` | **0** |
| `35=D` | **0** |
| `FIX.4` | **0** |

There is **no** second live cTrader FIX sender in the YoPips C++ prop-firm backend. Manager fetch (Achiever / Starwave) is MT5 Manager I/O, not Pepperstone `35=D`.

---

## 6. Goal fit: full Manager census **and** no live copy yet

Prior measured census (`LIVE_MANAGER_FETCH_MEASURED.md`, 2026-08-18): Achiever **8 groups / 6512 traders** (proxy) + Starwave **10 groups / 1948 traders** (direct) = **18 / 8460**. That is **read** from Manager APIs.

This slot answers the complementary constraint: **copy to cTrader must not send live orders yet.**

| Layer | Can lose destination capital? |
|---|---|
| Manager group/user/deal fetch | **No** — read-only ingest |
| `CTraderFixSession.TryLogonAsync` | **No orders** — `35=A` only; socket dropped |
| `CTraderFixLogonHostedService` | **No** — forces `RealCopyEnabled=false` |
| `CTraderFixOptions.RealCopyExecutionEnabled` | **false** default; unused by session |
| fix-worker loop | **No** — no TRADE socket, refuses send even if flag true |
| Risk / recon / quantity / ClOrdID persist-before-send | **Not wired to a sender** — cannot fire a D |
| First live `35=D` | **Unbuildable** until a new encoder + initiator + §68/§70 PASS + explicit flag |

**Risk to capital from slot-10 file: none.** A successful QUOTE/TRADE logon does not place XAUUSD. A failed logon returns `LoggedOn=false` and writes no order.

`SAFE_BY_ABSENCE` is the current safety outcome, not G05–G07 / A101.12 PASS. Do not enable the copy flag. Do not treat Logon-proven as send-proven.

---

## 7. What this file does **not** prove

- Does **not** prove live FIX Logon on the wire (no capture in this slot).
- Does **not** prove §68 19/19 or §70 14/14.
- Does **not** prove Manager census still 18/8460 (that is a different slot / `LIVE_MANAGER_FETCH_MEASURED.md`).
- Does **not** implement a refuse-on-LoggedOn-TRADE unit test — refuse is structural (no builder).

---

## 8. Checklist

- [x] Full read of `CTraderFixSession.cs` (135 lines)
- [x] Grep `35=D` / `NewOrderSingle` on that file = **0 / 0**
- [x] Only outbound MsgType = **`A`**
- [x] One `WriteAsync`; sockets disposed
- [x] Caller does not add a D; forces copy flag false
- [x] Product `35=D` builder still absent
- [x] YoPips C++ has no parallel FIX order sender
- [x] Product source not edited
- [x] No secrets printed
- [x] Verdict **PASS** (FAIL condition “live send exists” is false)
