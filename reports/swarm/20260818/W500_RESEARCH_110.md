# W500_RESEARCH_110 — `CTraderFixSession.cs` live `35=D` / NewOrderSingle search

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_110.md` |
| Agent / slot | W500 research **110** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (also grepped `D:\Projects\YoPips\Backend\C++ Backend PropFirm`) |
| Assigned file | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` |
| Topic | Search `CTraderFixSession.cs` for `35=D` or `NewOrderSingle`. **Verdict FAIL if live send exists.** |
| Goal context | Fetch ALL Achiever + Starwave groups and ALL manager traders; **copy to cTrader must not send live orders yet (no loss).** |
| Product source modified | **No.** This report is the only write from this slot besides swarm index/log pointers. |
| Test source modified | **No.** |
| Secrets printed | **None.** `CTRADER_FIX_PASSWORD` / tag 554 values not read from `.env` and not dumped. |
| Method | Full `read_file` of assigned file (**135 / 135** lines). Targeted `grep` of that file, all of `src\Fix.CTrader`, product `*.cs` under `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tests`, product `*.cs`/`*.json`/`*.csproj`/`*.tsx`/`*.ts` for `35=D` / `(35, "D")`, and YoPips C++ `src`. Supporting reads: hosted service, options, DI, fix-worker, API `/api/settings` + `/api/reconciliation/status`, `LiveRuntimeStatus`, FSM, `ClOrdIdFactory`, RiskEngine `AllowFixSend`, quote service, parser, ownership fence, harness, `NativeMt5BrokerConnector` group/user walks, `EfTradingStore` SHADOW_ONLY, `LIVE_MANAGER_FETCH_MEASURED.md`. **No TLS opened this slot. No Logon sent this slot. No order sent. No Manager re-attach.** |
| Binding law | Architecture §§32–34 / §41 / §68 / §70; A25; A32 (RoE `35=D`); A42 (never retry unknown as `35=D`); A101 item 12; E002 / E034 |
| Siblings (same assigned file; not this measurement) | W500_RESEARCH_10 / 30 / 50 / 70 / 90 |

**Honesty rule:** a comment, log line, `LastError` string, or helper *name* containing `NewOrderSingle` is **not** a FIX `MsgType=D` builder. A live TLS **Logon `35=A`** is **not** a NewOrderSingle. `AllowFixSend` / `MayRetryNewOrderSingle` / `RealCopyExecutionEnabled` are **not** socket writers. `35={msgType}` in a reject `LastError` interpolates the **inbound** tag 35. Absence of `35=D` is **`SAFE_BY_ABSENCE`**, not a unit-tested refuse-on-LoggedOn-TRADE gate. Do **not** tick Architecture §68 / §70 from this file. Do **not** print FIX passwords.

---

## 0. Verdict (binding)

**PASS — live `35=D` / NewOrderSingle send does not exist in `CTraderFixSession.cs`.**

Assigned FAIL condition (“FAIL if live send exists”) is **not met**. The assigned type cannot place a cTrader order. Copy-to-cTrader **cannot lose capital through this file**, because there is no order encoder and the only `WriteAsync` emits Logon `35=A`.

| Claim | Result | Class |
|---|---|---|
| Literal `35=D` in `CTraderFixSession.cs` | **0 hits** | **MISSING** builder |
| `NewOrderSingle` in `CTraderFixSession.cs` | **0 hits** | **MISSING** |
| `(35, "D")` / `new(35, "D")` / `MsgType = "D"` in assigned file | **0 hits** | **MISSING** |
| `OrderQty` / `ClOrdID` / `OrdType` / `StopPx` / `Side` / tags 11/38/40/54 in assigned file | **0 hits** | no order fields |
| Outbound tag 35 actually built | **`"A"` only** (`BuildLogon` L96) | Logon, not order |
| `ssl.WriteAsync` count in assigned file | **1** — bytes of that Logon (L49) | not an order send |
| `TcpClient` / `SslStream` kept for a later `35=D` | **No** — `using` / `await using` dispose before return | no TRADE keep-alive |
| `GuardedNewOrderSingle` / `SubmitNewOrder` / `BuildNewOrder` | **0** in assigned file and `Fix.CTrader` | choke **MISSING** |
| QuickFIX/n / `SendToTarget` in product `*.cs` / `*.csproj` | **0 hits** | initiator **MISSING** |
| Product `*.cs` / `*.json` / `*.csproj` / `*.tsx` / `*.ts` `35=D` / `(35, "D")` | **0 hits** under `D:\Prop` | no second builder |
| YoPips C++ `src` `CTraderFixSession` / `35=D` / `NewOrderSingle` / `FIX.4` | **0 hits** | not a second cTrader sender |
| Prop `src\Mt5` `DealerSend` / `SendTrade` / `OrderSend` | **0 hits** | Manager path is **read** |
| Live `35=D` if process starts now | **Impossible from this type** | **`SAFE_BY_ABSENCE`** |
| Slot FAIL (live send exists)? | **No** | verdict **PASS** |

One-line:

```text
CTraderFixSession.cs (135/135): NewOrderSingle=0; 35=D=0; only outbound MsgType is A (Logon); one WriteAsync; sockets disposed. Product 35=D=0. SAFE_BY_ABSENCE. PASS.
```

Do **not** enable `REAL_COPY_EXECUTION_ENABLED`. Do **not** add a `35=D` sender in this task.

---

## 1. Assigned-file census (measured this pass)

Path: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`  
Read: **135 / 135** lines. File ends at L135 `}`. Independent re-read for slot **110** (same length as W500_50 / 70 / 90).

### 1.1 Tokens the slot named

| Pattern (this file only) | Hits |
|---|---:|
| `35=D` | **0** |
| `NewOrderSingle` | **0** |
| `(35, "D")` / `(35, 'D')` / `new(35, "D")` | **0** |
| `MsgType = "D"` | **0** |
| `OrderQty` / `ClOrdID` / `OrdType` / `StopPx` / `Side` | **0** |
| outbound tags 11 / 38 / 40 / 44 / 54 / 55 | **0** |

`grep` of `NewOrderSingle|35=D|(35,\s*"D")` on this file: **no matches**.  
`grep` of `OrderQty|ClOrdID|OrdType|StopPx|(54,|(38,|(40,|(11,` on this file: **no matches**.

### 1.2 What the type actually is

Two types in one file:

- `CTraderFixSessionResult` (L10–17) — DTO: `Qualifier`, `LoggedOn`, `Status`, `LastError`, `RawLogonType`.
- `CTraderFixSession` (L19–135) — **static** class. **One** public method: `TryLogonAsync`.

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

### 1.3 The only socket write is Logon

`TryLogonAsync` always:

1. `new TcpClient()` in a `using`.
2. Connect with a 20 s linked cancel.
3. Wrap the stream in `SslStream` (TLS 1.2 | 1.3). Cert callback is `(_, _, _, _) => true` (identity **not** pinned).
4. `BuildLogon(...)` → ASCII bytes → **one** `ssl.WriteAsync` + `FlushAsync`.
5. One 4 KiB `ReadAsync`.
6. Classify inbound tag 35: `"A"` → `LoggedOn=true` / `Status=LoggedOn`; else `Status=Error` with `Logon rejected 35={msgType}`.
7. Dispose TCP/SSL on every path (success, reject, exception). Catch returns `Status=Disconnected`.

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

`grep` of `WriteAsync|ssl.Write|TcpClient` under `D:\Prop\src\Fix.CTrader`: **exactly those sites** in this file (`TcpClient` L35, `SslStream` L39, `WriteAsync` L49). No other live FIX writer exists in the project.

### 1.4 The only outbound MsgType is `A`

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

Body tags: **35=A**, 34, 49, 56, 50, 57, 52, 98, 108, 141, 553, 554.  
**No** `D` (NewOrderSingle), **no** `F` (cancel), **no** `G` (replace), **no** `H` / `AF` / `AN` (status / mass status / positions), **no** tag 38 `OrderQty`, **no** tag 11 `ClOrdID`.

`Assemble` prefixes `8=FIX.4.4` + body length + checksum tag 10. It will encode **whatever list it is given**. Today the **only** caller is `BuildLogon` with `(35, "A")`. There is no `BuildNewOrderSingle`.

`Extract` is inbound-only (split on `|` after SOH→pipe replace). The reject path `Logon rejected 35={msgType}` interpolates the **reply** tag 35. That string can contain the character `D` only if the **venue** sent `35=D` inbound (it would still be classified as Error, not sent). This slot did not open TLS.

---

## 2. Sole product caller (still not a sender)

`grep` of `CTraderFixSession` / `TryLogonAsync` in product `*.cs` (`D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tests`):

| File | Role |
|---|---|
| `Sessions\CTraderFixSession.cs` | definition |
| `Hosting\CTraderFixLogonHostedService.cs` | **only** caller: two `TryLogonAsync` + persist signature |

`apps\fix-worker\Worker.cs` does **not** reference `CTraderFixSession`. Tests do **not** call `TryLogonAsync`.

Hosted service (measured):

```48:70:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
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
```

Hard facts from that caller:

- Ports **5211 QUOTE** / **5212 TRADE** (SSL). Plain 5201/5202 are never used here.
- Default TargetCompID **`cServer`**. Default host `live-us-eqx-01.p.c-trader.com`.
- Tag 553 username is the **integer account id**, not SenderCompID (comment L45).
- After both probes: **`_runtime.RealCopyEnabled = false`** is forced, regardless of config.
- The word `NewOrderSingle` appears **only** in the log format string (L70). That is not a builder.
- Persist writes `FixSessionState` host/port/status only. No ClOrdID, no order row.
- After one reply the session type **disposes** TCP/SSL. There is no keep-alive TRADE initiator.

Password gate: if `CTRADER_FIX_PASSWORD` is missing or contains the literal `<SECRET>`, logon is **skipped** entirely (`CTraderFixLogonHostedService.cs` L33–38). This slot did **not** read the secret value.

DI registers that hosted service and **also** pins the flag:

```38:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
```

`CTraderFixLogonHostedService` is added at L56 of the same file.

---

## 3. Neighbor types in `Fix.CTrader` (none emit `35=D`)

Product `.cs` under `D:\Prop\src\Fix.CTrader` (7 files + csproj):

| File | Role | Live socket? |
|---|---|---|
| `Sessions\CTraderFixSession.cs` | one-shot TLS Logon | **Yes**, `35=A` only |
| `Hosting\CTraderFixLogonHostedService.cs` | two `TryLogonAsync` + persist | via session type |
| `Configuration\CTraderFixOptions.cs` | POCO; `RealCopyExecutionEnabled = false` | no |
| `Services\CTraderQuoteService.cs` | in-memory tag lists `y` / `V` | **No** callers write them |
| `Services\FixSessionOwnership.cs` | in-memory fencing lock | no FIX |
| `Parsing\FixMessageParser.cs` | pipe parser/builder for tests | no TCP |
| `Testing\FixSimulationHarness.cs` | inbound stand-ins `A`/`3`/`0`/`y`/`X`/`8` | in-process only |

`grep` of `(35,` under `D:\Prop\src\Fix.CTrader`:

| File | Tag 35 values | Wired to a live socket? |
|---|---|---|
| `Sessions\CTraderFixSession.cs` L96 | **`A`** | **Yes**, one-shot TLS Logon |
| `Services\CTraderQuoteService.cs` L113 / L127 | `y` (SecurityListRequest), `V` (MarketDataRequest) | **No.** `Build*Tags()` returns in-memory lists. **Zero** product callers outside that file. |
| `Testing\FixSimulationHarness.cs` | `A`, `3`, `0`, `y`, `X`, `8` | In-process harness. `8` is ExecutionReport (inbound stand-in). Tag 11 `ClOrdID` is on the **ER**, not a NewOrderSingle. |

`CTraderQuoteService` comments describe instrument discovery + MD subscribe. That is **not** NewOrderSingle. Those tag lists are never passed to `CTraderFixSession.Assemble` or `WriteAsync`.

`FixSessionOwnership` is an in-memory fencing lock **commented** as “single instance allowed to place/accept execution intents.” `ExecutionIntentsAllowed` is a bool. It never builds FIX.

`TraderIntelligence.Fix.CTrader.csproj`: net8.0; project refs Domain + Application; packages Hosting/Configuration/Logging abstractions + EFCore. **No** `QuickFIXn.Core`, **no** `QuickFIXn.FIX44`. Product-wide `QuickFIX` / `SendToTarget` on `*.cs` / `*.csproj`: **0 hits**.

---

## 4. Product-wide `NewOrderSingle` / `35=D` (name ≠ send)

`grep` `35=D` / `(35, "D")` / `new(35, "D")` / `MsgType = "D"` on `*.cs` / `*.json` / `*.csproj` / `*.tsx` / `*.ts` under `D:\Prop`: **0 hits**.

Product `*.cs` tokens named `NewOrderSingle` (none encode tag 35=`D`):

| Location | What it is | Sends `35=D`? |
|---|---|---|
| `src\Fix.CTrader\Configuration\CTraderFixOptions.cs:33–35` | XML comment + `RealCopyExecutionEnabled` default **`false`** | **No** |
| `src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs:70` | log “still disabled” | **No** |
| `src\Application\Runtime\LiveRuntimeStatus.cs:42–44` | `copyNote` when flag false: “NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.” | **No** |
| `src\Infrastructure\DependencyInjection.cs:40` | comment; flag pinned false | **No** |
| `src\Infrastructure\Seeding\DemoSeeder.cs:101` | TRADE `LastError` string | **No** |
| `src\Infrastructure\Seeding\BrokerCatalogSeed.cs:105` | TRADE `LastError` “NewOrderSingle off” | **No** |
| `apps\fix-worker\Worker.cs:22,41,46` | startup log / LastError / warning | **No** |
| `apps\api\Program.cs:68` | `/api/reconciliation/status` note | **No** |
| `src\Domain\Execution\ExecutionOrderStateMachine.cs:35–36` | `MayRetryNewOrderSingle` status math (`NotSent`/`Rejected` only) | **No** |
| `tests\Unit\ExecutionAndSizingTests.cs:14` | asserts retry helper false after send-attempt **state** | **No** |

`apps\fix-worker\Worker.cs` reads `CTrader:RealCopyExecutionEnabled` with fallback **false**. If the key is true it **only logs a warning** and still writes `LastError = "No live TRADE socket. NewOrderSingle remains off."` It never calls `CTraderFixSession` and never builds `35=D`. Flipping the flag **cannot** place an order.

```21:46:D:\Prop\apps\fix-worker\Worker.cs
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);
        // ...
                trade.LastError = "No live TRADE socket. NewOrderSingle remains off.";
        // ...
            if (real)
                _logger.LogWarning("Real copy is enabled in config, but worker still refuses NewOrderSingle until risk/reconciliation gates pass.");
```

API settings expose the runtime flag (forced false at DI + again after logon):

```70:77:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", (LiveRuntimeStatus runtime) => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = false
    },
```

`.env` line 73 is `REAL_COPY_EXECUTION_ENABLED=false` (value only; no secrets copied).

`RiskEngine.Evaluate` (`src\Domain\Risk\RiskEngine.cs` L76) can set `AllowFixSend` on a DTO (L64 / L160 / L170 / L187). Product `*.cs` callers of `RiskEngine.Evaluate` / `new RiskEngine` / `IRiskEngine`: **0**. A bool on a risk record is not a live send.

`ClOrdIdFactory.Next` builds a string `TI{timestamp}{seq}{intent}`. It is never passed to `Assemble` or `WriteAsync`.

`EfTradingStore` persists copy rows as **`SHADOW_ONLY`** (`Status = "SHADOW_ONLY"` at L307). `ShadowCopyEngine.SimulateEntry` is in-memory. No socket.

Prop `src\Mt5` has **0** `DealerSend` / `SendTrade` / `OrderSend`. Manager connector is **read** (`GroupRequestArray` / `UserRequestArray`).

---

## 5. YoPips C++ backend (relevant only as contrast)

Tree searched: `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src`

| Pattern | Hits |
|---|---|
| `CTraderFixSession` | **0** |
| `NewOrderSingle` | **0** |
| `35=D` | **0** |
| `FIX.4` / `CTraderFix` | **0** |

That tree **does** have MT5 `DealerSend` / `DealerSendOrder` / `SendTrade` (`mt5_manager.cpp`, `mt5_pool.cpp`, `mt5_http_client.cpp`, `trade_execution_service.cpp`). That is the **prop-firm MT5 dealer** path for YoPips challenge accounts, **not** cTrader FIX, and it is **not** called from `CTraderFixSession.cs` or from any `D:\Prop\src` C# file. Slot 110 does **not** treat YoPips `DealerSend` as a live cTrader `35=D`. It also does **not** authorize using that dealer as a copy destination.

---

## 6. Goal coupling: all-groups/all-traders vs no-loss copy

Live Manager census already measured (do not re-invent; this slot did **not** re-attach to either Manager):

| Source | Evidence |
|---|---|
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | Achiever **8 groups / 6512 traders** (HTTP proxy) + Starwave **10 groups / 1948 traders** (direct) = **18 / 8460** |
| Same file § Copy | “No `35=D` NewOrderSingle exists in `CTraderFixSession`.” |

Achiever groups (manager-visible): `contest\yo-1step`, `contest\yo-2step`, `contest\yo-instant`, `contest\yo-payp`, `demo\yo-1step`, `demo\yo-2step`, `demo\yo-instant`, `demo\yo-payp`.

Starwave groups (manager-visible): `Starwave\cent\FX1\grp1`, `grp2`; `Starwave\demo\FX2\grp1`, `grp2`; `Starwave\real\FX3\grp1`–`grp5`, `Starwave\real\FX3\LP`.

Fetch path on current disk (`NativeMt5BrokerConnector`):

- Groups: `GroupRequestArray("*")` L155; cache fallback `GroupTotal`/`GroupNext` only if the request list is empty.
- Traders: per-group `UserRequestArray` L223; cache `UserGetByGroup` only on hard fail; empty → `UserLogins` + `UserRequestByLogins`.
- Ingest uses `GetAccountsAsync(null)` → all groups.

This slot **re-confirms** the copy half of the goal on current disk:

- Fetch path is **Manager read** — not this FIX type.
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
| `AllowFixSend` can be true on a risk DTO | No encoder consumes it; 0 product `Evaluate` callers |
| Quote-service `y`/`V` lists exist | Never written to a socket |
| YoPips `DealerSend` exists in a **different** tree | MT5 dealer, not cTrader FIX; not called from this file |
| `Assemble` is a generic encoder | Only fed `(35, "A")` today |

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
- Confuse YoPips `DealerSend` with a cTrader NewOrderSingle.

---

## 10. Slot close

| Item | Value |
|---|---|
| Slot | **110** |
| Verdict | **PASS** |
| Live `35=D` / NewOrderSingle send exists? | **No** |
| Risk to capital from assigned file | **None** (`SAFE_BY_ABSENCE`) |
| Evidence | Full 135-line read; 0 hits for `35=D` and `NewOrderSingle` in `CTraderFixSession.cs`; sole outbound MsgType `(35, "A")`; one `WriteAsync`; sockets disposed; sole caller re-pins `RealCopyEnabled=false`; product `*.cs`/`*.json`/`*.csproj` have 0 `35=D`; YoPips C++ `src` has 0 cTrader FIX senders |
| Census cited (not re-attached) | Achiever 8/6512 + Starwave 10/1948 = **18 / 8460** |
| Product edited | **No** |
