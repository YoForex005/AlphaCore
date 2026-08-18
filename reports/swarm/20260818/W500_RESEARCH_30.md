# W500_RESEARCH_30 — `CTraderFixSession.cs` live `35=D` / NewOrderSingle census

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_30.md` |
| Agent / slot | W500 research **30** |
| Date | 2026-08-18 |
| Assigned file | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` |
| Topic | Search `CTraderFixSession.cs` for `35=D` or `NewOrderSingle`. **Verdict FAIL if live send exists.** |
| Goal context | Fetch ALL Achiever + Starwave groups and ALL manager traders; copy to cTrader **must not send live orders yet** (no loss). |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Secrets printed | **None** (no password / tag 554 values). |
| Method | Full `read_file` of assigned file (135/135 lines). Targeted `grep` on that file, `Fix.CTrader`, product `*.cs` under `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tests`, and YoPips C++ `src` when relevant. Supporting reads: hosted service, options, DI, fix-worker, FSM, catalog seed, API `/api/settings`, `LIVE_MANAGER_FETCH_MEASURED.md`. **No TLS opened. No Logon sent this slot. No product edit.** |
| Binding law | Architecture §§32–34 / §41 / §68 / §70; A25; A42 (no retry as `35=D`); A101 item 12; E002 / E034 |
| Siblings (do not treat as this file) | E034 (`SAFE_BY_ABSENCE` product grep), E002 (flag default), A003 (no-loss gate), A011 (persist, still `35=A` only), W500_SLICE_133 / 183 (same file, NewOrderSingle angle), LIVE_MANAGER_FETCH_MEASURED |

**Honesty rule:** a comment, log line, `LastError` string, or helper *name* containing `NewOrderSingle` is **not** a FIX `MsgType=D` builder. A live TLS **Logon `35=A`** is **not** a NewOrderSingle. `AllowFixSend` / `MayRetryNewOrderSingle` / `RealCopyExecutionEnabled` are **not** socket writers. Absence of `35=D` is **`SAFE_BY_ABSENCE`**, not a unit-tested refuse-on-LoggedOn-TRADE gate. Do **not** tick Architecture §68 / §70 from this file. Do **not** print FIX passwords.

---

## 0. Verdict (binding)

**PASS — live `35=D` / NewOrderSingle send does not exist in `CTraderFixSession.cs`.**

FAIL condition from the slot brief (“FAIL if live send exists”) is **not met**. The assigned type cannot place a cTrader order. Copy-to-cTrader **cannot lose capital through this file**, because there is no order encoder and the only `WriteAsync` emits Logon `35=A`.

| Claim | Result | Class |
|---|---|---|
| Literal `35=D` in `CTraderFixSession.cs` | **0 hits** | **MISSING** builder |
| `NewOrderSingle` in `CTraderFixSession.cs` | **0 hits** | **MISSING** |
| `(35, "D")` / `new(35, "D")` / `MsgType = "D"` in assigned file | **0 hits** | **MISSING** |
| Outbound tag 35 actually built | **`"A"` only** (`BuildLogon` L96) | Logon, not order |
| `ssl.WriteAsync` count | **1** — bytes of that Logon | not an order send |
| Socket kept for a later `35=D` | **No** — `using TcpClient` / `await using SslStream` dispose before return | no TRADE keep-alive |
| `GuardedNewOrderSingle` / `SubmitNewOrder` / `BuildNewOrder` | **0** in assigned file and `Fix.CTrader` | choke **MISSING** |
| QuickFIX/n in `Fix.CTrader` csproj | **0** package refs | initiator **MISSING** |
| Live `35=D` if process starts now | **Impossible from this type** | **`SAFE_BY_ABSENCE`** |
| Slot FAIL (live send exists)? | **No** | verdict **PASS** |

One-line:

```text
CTraderFixSession.cs (135 lines): NewOrderSingle=0; 35=D=0; only outbound MsgType is A (Logon); sockets disposed after one read. SAFE_BY_ABSENCE. PASS.
```

Do **not** enable `REAL_COPY_EXECUTION_ENABLED`. Do **not** add a `35=D` sender in this task.

---

## 1. Assigned-file census (measured)

Path: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`  
Read: **135 / 135 lines** this pass (some older slices quoted 136; current disk ends at L135 `}`).

### 1.1 Tokens the slot named

| Pattern (this file only) | Hits |
|---|---:|
| `35=D` | **0** |
| `NewOrderSingle` | **0** |
| `(35, "D")` / `(35, 'D')` / `new(35, "D")` | **0** |
| `MsgType = "D"` | **0** |
| `OrderQty` / `ClOrdID` / `OrdType` / `StopPx` | **0** |
| tag 11 / 38 / 40 / 44 / 54 / 55 as outbound order fields | **0** |

`grep` of `NewOrderSingle|35=D|(35,\s*"D")` on this file: **no matches**.

### 1.2 What the type actually is

Two types in one file:

- `CTraderFixSessionResult` — DTO (`Qualifier`, `LoggedOn`, `Status`, `LastError`, `RawLogonType`).
- `CTraderFixSession` — **static** class. **One** public method: `TryLogonAsync`.

Private helpers: `BuildLogon`, `Assemble`, `Extract`. There is no second public entry that could grow into an order sender without a new method.

Public surface:

```19:31:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
public static class CTraderFixSession
{
    public static async Task<CTraderFixSessionResult> TryLogonAsync(
        FixSessionQualifier qualifier,
        string host,
        int sslPort,
        string senderCompId,
        string targetCompId,
        string senderSubId,
        string targetSubId,
        string username,
        string password,
        CancellationToken ct)
```

Wire I/O is a **one-shot** TLS Logon probe (20 s linked CTS). Connect → `SslStream` (TLS 1.2|1.3, cert callback always true) → **one** `BuildLogon` → **one** `WriteAsync` → **one** 4 KiB `ReadAsync` → classify inbound tag 35 → return. `using` / `await using` dispose the sockets **before** the method returns.

```35:54:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
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
            await ssl.FlushAsync(timeoutCts.Token);

            var buffer = new byte[4096];
            var read = await ssl.ReadAsync(buffer, timeoutCts.Token);
```

Inbound handling inspects tag `35` only. Success is **Logon ACK** (`msgType == "A"`). Any other type becomes `Status = "Error"` with tag `58` text. Exceptions become `Status = "Disconnected"`. No ExecutionReport (`35=8`) apply, no fill, no flatten:

```55:65:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
            var msgType = Extract(reply, "35");
            if (msgType == "A")
            {
                return new CTraderFixSessionResult
                {
                    Qualifier = qualifier,
                    LoggedOn = true,
                    Status = "LoggedOn",
                    RawLogonType = msgType
                };
            }
```

### 1.3 The only outbound MsgType

`BuildLogon` body starts with `(35, "A")`. Remaining body tags are session/logon only: 34, 49, 56, 50, 57, 52, 98, 108, 141, 553, 554. `Assemble` adds 8=`FIX.4.4`, computed 9, checksum 10.

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

**Missing on purpose (not in this file):** `D` (NewOrderSingle), `F` (cancel), `G` (replace), `H` (status), `AF` (mass status), `AN` (positions), `0` heartbeat writer, SecurityList, MarketData.

There is **no** parameter that can change tag 35. `seq` is hardcoded `1`. `Assemble` concatenates the list it is given; the only caller is `BuildLogon`.

---

## 2. Call graph (product C# only)

`grep` of `TryLogonAsync` / `CTraderFixSession` in `D:\Prop` `*.cs` (product, not reports):

| Caller | What it does | Can it send `35=D`? |
|---|---|---|
| `CTraderFixLogonHostedService.ExecuteAsync` L48 / L54 | QUOTE `:5211` then TRADE `:5212` `TryLogonAsync` | **No** — callee already disposed; no second write |

No other product caller. `apps/fix-worker/Worker.cs` does **not** reference `CTraderFixSession`. Tests do **not** call `TryLogonAsync`.

After the two probes the hosted service **re-pins** the copy flag and logs that orders stay off:

```60:71:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.Quote.LoggedOn = quote.LoggedOn;
        _runtime.Quote.Status = quote.Status;
        _runtime.Quote.LastError = quote.LastError;
        _runtime.Quote.UpdatedAt = DateTimeOffset.UtcNow;
        _runtime.Trade.LoggedOn = trade.LoggedOn;
        _runtime.Trade.Status = trade.Status;
        _runtime.Trade.LastError = trade.LastError;
        _runtime.Trade.UpdatedAt = DateTimeOffset.UtcNow;
        _runtime.RealCopyEnabled = false;

        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
            quote.LoggedOn, trade.LoggedOn, account);
```

Password gate: if `CTRADER_FIX_PASSWORD` is empty or contains `<SECRET>`, the hosted service **returns** without opening TCP. This slot did not read secret values.

DI composition (`AddTraderIntelligence`) registers that hosted service and hardcodes `LiveRuntimeStatus.RealCopyEnabled = false` with comment “Live NewOrderSingle is not implemented.”

---

## 3. Wider `Fix.CTrader` MsgType census (so a later grep does not surprise)

Every product `(35, "…")` / `new(35, "…")` under `D:\Prop\src\Fix.CTrader`:

| File | Tag 35 value | Live socket? |
|---|---|---|
| `Sessions/CTraderFixSession.cs` L96 | **`A`** (Logon) | Yes, one-shot TLS **if** hosted service has a real password |
| `Testing/FixSimulationHarness.cs` | `A`, `3`, `0`, `y`, `X`, `8` | **No** — pipe strings for unit tests |
| `Services/CTraderQuoteService.cs` | `y` (SecurityListRequest), `V` (MarketDataRequest) | **No** — returns tag lists; nothing writes them to a socket |

**Zero** `D` / `F` / `G` / `H` / `AF` / `AN`.

`TraderIntelligence.Fix.CTrader.csproj`: `net8.0`; refs Domain + Application + Hosting/Configuration/Logging/EF abstractions. **No** `QuickFIXn.Core` / `QuickFIXn.FIX44`. `grep` `QuickFIX|QuickFixn|QuickFIXn` under `Fix.CTrader`: **0**.

`CTraderQuoteService` / `FixSessionOwnership` / `FixMessageParser` / `FixSimulationHarness` have **no** `TcpClient` / `SslStream` / `WriteAsync`.

---

## 4. Product-tree `NewOrderSingle` (name-only; not a sender)

`grep` `NewOrderSingle` under `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tests` `*.cs` (this pass):

| File:line | Kind | Encodes `35=D`? |
|---|---|---|
| `src/Domain/Execution/ExecutionOrderStateMachine.cs:35` | `MayRetryNewOrderSingle` status predicate | **No** |
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs:33` | XML comment on `RealCopyExecutionEnabled` | **No** |
| `src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs:70` | log “still disabled” | **No** |
| `src/Application/Runtime/LiveRuntimeStatus.cs:44` | copyNote when flag false | **No** |
| `src/Infrastructure/DependencyInjection.cs:40` | comment; flag pinned false | **No** |
| `src/Infrastructure/Seeding/DemoSeeder.cs:101` | TRADE `LastError` string | **No** |
| `src/Infrastructure/Seeding/BrokerCatalogSeed.cs:105` | TRADE `LastError` “NewOrderSingle off” | **No** |
| `apps/fix-worker/Worker.cs:22,41,46` | startup log / LastError / warning | **No** |
| `apps/api/Program.cs:68` | `/api/reconciliation/status` note | **No** |
| `tests/Unit/ExecutionAndSizingTests.cs:14` | asserts retry helper false after send-attempt **state** | **No** |
| `tests/Unit/Normalization/SourceDestinationQuantityConversionTests.cs` | skipped OrderQty mapping tests | **No** |

`grep` `35=D` / `(35, "D")` / `new(35, "D")` / `MsgType = "D"` on product `*.cs` under `D:\Prop\src` + `D:\Prop\apps`: **0 hits**.

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **`false`**. `apps/fix-worker` reads `CTrader:RealCopyExecutionEnabled` with fallback **false** and, even if true, only logs a warning — it still has **no** function that can emit `35=D`. Flipping the flag **cannot** place an order.

---

## 5. YoPips C++ backend (relevant only as contrast)

Tree: `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src`

| Pattern | Hits |
|---|---|
| `CTraderFixSession` | **0** |
| `NewOrderSingle` | **0** |
| `35=D` | **0** |
| `FIX.4` / `CTraderFix` | **0** |

That tree **does** have MT5 `DealerSend` / `DealerSendOrder` (`mt5_manager.cpp`, `mt5_pool.cpp`, `mt5_http_client.cpp`). That is the **prop-firm MT5 dealer** path, not cTrader FIX, and it is **not** called from `CTraderFixSession.cs`. Slot 30 does not treat YoPips `DealerSend` as a live cTrader `35=D`. It also does not authorize using that dealer as a copy destination.

---

## 6. Goal coupling: all-groups/all-traders vs no-loss copy

Live Manager census already measured (do not re-invent; do not greenwash):

| Source | Evidence |
|---|---|
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | Achiever **8 groups / 6512 traders** (HTTP proxy) + Starwave **10 groups / 1948 traders** (direct) = **18 / 8460** |
| Same file § Copy | “No `35=D` NewOrderSingle exists in `CTraderFixSession`.” |

This slot **re-confirms** the copy half of that sentence on current disk:

- Fetch path is **Manager read** (`GroupRequestArray` / `UserRequestArray`) — not this FIX type.
- Copy destination **must stay off** until §68 19/19 + §70 14/14 + persist-before-send + explicit flag. Those gates are **not** re-scored here (siblings A100 / A101 still historically **0 PASS**).
- Current safety for capital: **`SAFE_BY_ABSENCE`** of `35=D` in the only live FIX writer.

Logon `35=A` **can** still leave this process toward `*.c-trader.com` when the password slot is populated (QUOTE 5211 / TRADE 5212). That is **session proof**, not an order. It cannot attach qty/side/symbol/price. Sockets are dead after the one reply.

---

## 7. What would have been FAIL

The slot brief: **FAIL if live send exists.** Any of the following on current disk would have failed the slot:

1. `BuildLogon` or a sibling builder emitting `(35, "D")`.
2. A second `WriteAsync` of a constructed NewOrderSingle after Logon.
3. A kept TRADE `SslStream` / QuickFIX initiator that `SendToTarget`s `MsgType=D`.
4. `OrderQty` / `ClOrdID` / `Side` / `OrdType` assembled onto the live socket.

**None** of those exist in `CTraderFixSession.cs`.

---

## 8. Residual (honest, not FAIL)

| Residual | Why it is not this slot’s FAIL |
|---|---|
| `35=A` can go to the live Pepperstone cTrader host if password is real | Not NewOrderSingle; no capital move |
| Cert callback `(_, _, _, _) => true` | TLS identity not pinned; still Logon-only |
| `SAFE_BY_ABSENCE` ≠ unit-tested refuse-on-LoggedOn-TRADE | A101 item 12 stays unproven |
| No persist-before-send / `GuardedNewOrderSingle` | Cannot send, so cannot violate it **yet** |
| Three hosts can each register `CTraderFixLogonHostedService` | Duplicate Logon risk, not duplicate `35=D` |
| `MayRetryNewOrderSingle` exists as status math | Never opens a socket |

---

## 9. Do / Do not

**Do**

- Keep `RealCopyExecutionEnabled` / `LiveRuntimeStatus.RealCopyEnabled` **false**.
- Keep Manager fetch of **all** Achiever + Starwave groups / logins as a **read** path.
- Treat this file as Logon/`35=A` only until a separately reviewed sender exists **and** §68+§70 PASS.

**Do not**

- Add `35=D` / `F` / `G` in this task.
- Treat QUOTE/TRADE `LoggedOn=true` as license to copy.
- Print `CTRADER_FIX_PASSWORD` or tag 554.
- Claim “no-loss live copy” is implemented. **No-loss live copy is impossible today** because there is no gated send path. The operating mode is **fetch + logon/recon only**.

---

## 10. Slot close

| Item | Value |
|---|---|
| Slot | **30** |
| Verdict | **PASS** |
| Live `35=D` / NewOrderSingle send exists? | **No** |
| Risk to capital from assigned file | **None** (`SAFE_BY_ABSENCE`) |
| Evidence | Full 135-line read; 0 hits for `35=D` and `NewOrderSingle` in `CTraderFixSession.cs`; sole outbound MsgType `(35, "A")`; one `WriteAsync`; sockets disposed; sole caller re-pins `RealCopyEnabled=false` |
| Product edited | **No** |
