# W500_RESEARCH_90 — `CTraderFixSession.cs` live `35=D` / NewOrderSingle search

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_90.md` |
| Agent / slot | W500 research **90** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (also grepped `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` for a second sender) |
| Assigned file | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` |
| Topic | Search `CTraderFixSession.cs` for `35=D` or `NewOrderSingle`. **Verdict FAIL if live send exists.** |
| Goal context | Fetch ALL Achiever + Starwave groups and ALL manager traders; **copy to cTrader must not send live orders yet (no loss).** |
| Product source modified | **No.** Report + swarm index/log pointers only. |
| Test source modified | **No.** |
| Secrets printed | **None.** `CTRADER_FIX_PASSWORD` / tag 554 values not read from `.env` and not dumped. |
| Method | Full `read_file` of assigned file (**135 / 135** lines). Targeted `grep` of that file, all of `src\Fix.CTrader`, product `*.cs` under `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tests`, and YoPips C++ `src`. Supporting reads: hosted service, options, DI, fix-worker, `LiveRuntimeStatus`, FSM, RiskEngine `AllowFixSend`, quote service, parser, harness, API `/api/settings`, `NativeMt5BrokerConnector` group/user walks, `LIVE_MANAGER_FETCH_MEASURED.md` + JSON census re-sum. **No TLS opened this slot. No Logon sent this slot. No order sent. No Manager re-attach.** |
| Binding law | Architecture §§32–34 / §41 / §68 / §70; A25; A32 (RoE `35=D`); A42 (never retry unknown as `35=D`); A101 item 12; E002 / E034 |
| Siblings (do not treat as this file) | W500_RESEARCH_10 / 30 / 50 / 70 (same assigned file, earlier slots), A003 (no-loss gate), E002 (flag default), E034 (product `35=D` census), A011 (persist, still `35=A`), A100 / A101 (go-live still historically 0 PASS) |

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
| Outbound tag 35 actually built | **`"A"` only** (`BuildLogon` L96) | Logon, not order |
| `ssl.WriteAsync` count in assigned file | **1** — bytes of that Logon (L49) | not an order send |
| `TcpClient` / `SslStream` kept for a later `35=D` | **No** — `using` / `await using` dispose before return | no TRADE keep-alive |
| `OrderQty` / `ClOrdID` / `OrdType` / `Side` / `StopPx` in assigned file | **0** | no order fields |
| `GuardedNewOrderSingle` / `SubmitNewOrder` / `BuildNewOrder` | **0** in assigned file and `Fix.CTrader` | choke **MISSING** |
| QuickFIX/n / `SendToTarget` in `TraderIntelligence.Fix.CTrader.csproj` | **0** package refs (Hosting + Configuration + Logging + EF only) | initiator **MISSING** |
| Product `*.cs` / `*.json` / `*.csproj` `35=D` / `(35, "D")` | **0 hits** under `D:\Prop` | no second builder |
| Product `new ExecutionIntent` / `ExecutionIntents.Add` | **0 hits** | no persist-before-send row |
| YoPips C++ `src` `CTraderFixSession` / `35=D` / `NewOrderSingle` / `FIX.4` | **0 hits** | not a second cTrader sender |
| Live `35=D` if process starts now | **Impossible from this type** | **`SAFE_BY_ABSENCE`** |
| Slot FAIL (live send exists)? | **No** | verdict **PASS** |

One-line:

```text
CTraderFixSession.cs (135 lines): NewOrderSingle=0; 35=D=0; only outbound MsgType is A (Logon); sockets disposed after one read. SAFE_BY_ABSENCE. PASS.
```

Do **not** enable `REAL_COPY_EXECUTION_ENABLED`. Do **not** add a `35=D` sender in this task.

---

## 1. Assigned-file census (measured this pass)

Path: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`  
Read: **135 / 135** lines. File ends at L135 `}`. SHA-256 **not recomputed** this slot (no shell); line census is the measured identity.

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

`grep` of `WriteAsync|TcpClient|BuildLogon` on this file: L35 `TcpClient`, L47 `BuildLogon`, L49 `WriteAsync`, L89 `BuildLogon` definition.

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

`grep` of `WriteAsync|SendToTarget|TcpClient|SslStream` on product `*.cs` under `D:\Prop`: **exactly those three lines** in this file (L35 `TcpClient`, L39 `SslStream`, L49 `WriteAsync`). No other live FIX writer exists in the product C#.

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

`Extract` is inbound-only (split on `|` after SOH→pipe replace).

---

## 2. Sole product caller (still not a sender)

`grep` of `CTraderFixSession` / `TryLogonAsync` in product `*.cs` (`D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tests`):

| File | Role |
|---|---|
| `Sessions\CTraderFixSession.cs` | definition |
| `Hosting\CTraderFixLogonHostedService.cs` | **only** caller: two `TryLogonAsync` + persist signature |

`apps\fix-worker\Worker.cs` does **not** reference `CTraderFixSession`. Tests do **not** call `TryLogonAsync`.

Hosted service (measured this pass):

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

Password gate: if `CTRADER_FIX_PASSWORD` is missing or contains the literal `<SECRET>`, logon is **skipped** entirely (L33–38). This slot did **not** read the secret value.

DI registers that hosted service and **also** pins the flag:

```38:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
```

`CTraderFixLogonHostedService` is added at L56 of the same file. `apps\fix-worker\Program.cs` L6–7 calls `AddTraderIntelligence` **and** also hosts the worker that stamps TRADE `Disconnected`.

---

## 3. Neighbor types in `Fix.CTrader` (none emit `35=D`)

`grep` of `(35,` under `D:\Prop\src\Fix.CTrader` (this pass):

| File | Tag 35 values | Wired to a live socket? |
|---|---|---|
| `Sessions\CTraderFixSession.cs` L96 | **`A`** | **Yes**, one-shot TLS Logon |
| `Services\CTraderQuoteService.cs` L113 / L127 | `y` (SecurityListRequest), `V` (MarketDataRequest) | **No.** `Build*Tags()` returns in-memory lists. **Zero** product callers outside that file. |
| `Testing\FixSimulationHarness.cs` | `A`, `3`, `0`, `y`, `X`, `8` | In-process harness. `8` is ExecutionReport (inbound stand-in). Tag 11 `ClOrdID` is on the **ER**, not a NewOrderSingle. |

`CTraderQuoteService` comments describe instrument discovery + MD subscribe. That is **not** NewOrderSingle. Those tag lists are never passed to `CTraderFixSession.Assemble` or `WriteAsync`.

`FixMessageParser` is a test parser/builder (pipe delimiter, checksum). It does not open TCP.

`FixSessionOwnership` is an in-memory fencing lock **commented** as “single instance allowed to place/accept execution intents.” It never builds FIX. `ExecutionIntentsAllowed` is a bool; `grep` found **0** `new ExecutionIntent` / `ExecutionIntents.Add` in product C#.

`TraderIntelligence.Fix.CTrader.csproj`: net8.0; project refs Domain + Application; packages Hosting/Configuration/Logging abstractions + EFCore. **No** `QuickFIXn.Core`, **no** `QuickFIXn.FIX44`. `grep` of `QuickFIX|QuickFix|GuardedNewOrderSingle|BuildNewOrder|SendTrade` on product `*.cs`/`*.csproj`: **0**.

---

## 4. Product-wide `NewOrderSingle` / `35=D` (name ≠ send)

`grep` `35=D` / `(35, "D")` / `new(35, "D")` / `MsgType = "D"` on `*.cs` / `*.json` / `*.csproj` under `D:\Prop`: **0 hits**.

Product `*.cs` tokens named `NewOrderSingle` (none encode tag 35=`D`):

| Location | What it is | Sends `35=D`? |
|---|---|---|
| `src\Fix.CTrader\Configuration\CTraderFixOptions.cs:33–35` | XML comment + `RealCopyExecutionEnabled` default **`false`** | **No** |
| `src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs:70` | log “still disabled” | **No** |
| `src\Application\Runtime\LiveRuntimeStatus.cs:42–43` | `copyNote` when flag false: “NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.” | **No** |
| `src\Infrastructure\DependencyInjection.cs:40` | comment; flag pinned false | **No** |
| `src\Infrastructure\Seeding\DemoSeeder.cs:101` | TRADE `LastError` string | **No** |
| `src\Infrastructure\Seeding\BrokerCatalogSeed.cs:105` | TRADE `LastError` “NewOrderSingle off” | **No** |
| `apps\fix-worker\Worker.cs:22,41,46` | startup log / LastError / warning | **No** |
| `apps\api\Program.cs:68` | `/api/reconciliation/status` note | **No** |
| `src\Domain\Execution\ExecutionOrderStateMachine.cs:35–36` | `MayRetryNewOrderSingle` status math (`NotSent`/`Rejected` only) | **No** |
| `tests\Unit\ExecutionAndSizingTests.cs:14` | asserts retry helper false after send-attempt **state** | **No** |

Scratch `_tmp_d98_noretry\Program.cs` also names the helper. It is not product.

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

`RiskEngine` can set `AllowFixSend` on a DTO (`src\Domain\Risk\RiskEngine.cs` L64 / L160 / L170 / L187). Tests assert `AllowFixSend` false on several paths. **No** product caller maps that boolean onto `ssl.WriteAsync` or `35=D`. A bool on a risk record is not a live send.

`OrderQty` exists only in **skipped** unit tests (`SourceDestinationQuantityConversionTests`, `QuantityNormalizerStepMinMaxTests` comments). No product encoder writes tag 38.

---

## 5. YoPips C++ backend (relevant only as contrast)

Tree searched: `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` (directory exists; `src\core`, `src\http`, `src\services` present).

| Pattern | Hits |
|---|---|
| `CTraderFixSession` | **0** |
| `NewOrderSingle` | **0** |
| `35=D` | **0** |
| `FIX.4` / `CTraderFix` | **0** |

That tree **does** have MT5 `DealerSend` / `DealerSendOrder` (`mt5_manager.cpp` L1119 / L1134, `mt5_pool.cpp`, `mt5_http_client.cpp`). That is the **prop-firm MT5 dealer** path for YoPips challenge accounts, **not** cTrader FIX, and it is **not** called from `CTraderFixSession.cs`. Slot 90 does **not** treat YoPips `DealerSend` as a live cTrader `35=D`. It also does **not** authorize using that dealer as a copy destination.

---

## 6. Goal coupling: all-groups/all-traders vs no-loss copy

Fetch path is **Manager read**, not this FIX type.

`NativeMt5BrokerConnector.GetGroupsCore` walks `GroupRequestArray("*")` then falls back to `GroupTotal`+`GroupNext` (`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` L155 / L174). `GetAccountsCore(null)` iterates **every** returned group and `UserRequestArray` / `UserGetByGroup` / `UserLogins` (`L201–232`). `DealIngestionService.SyncCatalogAsync` calls `GetAccountsAsync(null)` (`DealIngestionService.cs` L48). Dashboard `GetTradersAsync` is account-driven (`foreach (var account in accounts)` at `EfDashboardQueries.cs` L99), not scores-only.

Live Manager census already measured (this slot **re-summed** the JSON group rows; it did **not** re-attach):

| Source | Evidence |
|---|---|
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | Achiever **8 / 6512** (HTTP proxy) + Starwave **10 / 1948** (direct) = **18 / 8460** |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | `utc=2026-08-18T08:42:16Z`; ACHIEVER `groups=8` `accounts=6512` `openPositions=1506`; STARWAVEFX `groups=10` `accounts=1948` `openPositions=478` |
| Same JSON group-row sum | Achiever `2+179+4+5+4+6295+0+23 = 6512`; Starwave `11+4+170+1735+22+0+0+4+0+2 = 1948` |
| Same file / CREDENTIALS note | “No `35=D` NewOrderSingle exists in `CTraderFixSession`.” |

Achiever groups (manager-visible): `contest\yo-1step`, `contest\yo-2step`, `contest\yo-instant`, `contest\yo-payp`, `demo\yo-1step`, `demo\yo-2step`, `demo\yo-instant`, `demo\yo-payp`.

Starwave groups (manager-visible): `Starwave\cent\FX1\grp1`, `grp2`; `Starwave\demo\FX2\grp1`, `grp2`; `Starwave\real\FX3\grp1`–`grp5`, `Starwave\real\FX3\LP`.

This slot **re-confirms** the copy half of the goal on current disk:

- Fetch path is **Manager read** — not this FIX type.
- Copy destination **must stay off** until §68 19/19 + §70 14/14 + persist-before-send + explicit flag. Those gates are **not** re-scored here (siblings A100 / A101 still historically **0 PASS**).
- Current safety for capital: **`SAFE_BY_ABSENCE`** of `35=D` in the only live FIX writer.
- Only CopyIntent writer is `PersistDemoShadowAsync` with `Status = "SHADOW_ONLY"` (`EfTradingStore.cs` L307). No `ExecutionIntent` writer.

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
| `AllowFixSend` can be true on a risk DTO | No encoder consumes it |
| Quote-service `y`/`V` lists exist | Never written to a socket |
| YoPips `DealerSend` exists in a **different** tree | MT5 dealer, not cTrader FIX; not called from this file |
| SHA-256 of assigned file not recomputed | Identity is the 135-line full read; hash left to a hashing slot |

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
| Slot | **90** |
| Verdict | **PASS** |
| Live `35=D` / NewOrderSingle send exists? | **No** |
| Risk to capital from assigned file | **None** (`SAFE_BY_ABSENCE`) |
| Evidence | Full 135-line read; 0 hits for `35=D` and `NewOrderSingle` in `CTraderFixSession.cs`; sole outbound MsgType `(35, "A")`; one `WriteAsync`; sockets disposed; sole caller re-pins `RealCopyEnabled=false`; product `*.cs`/`*.json`/`*.csproj` have 0 `35=D`; 0 `ExecutionIntent` writers; YoPips C++ `src` has 0 cTrader FIX senders |
| Census cited (JSON re-summed, not re-attached) | Achiever 8/6512 + Starwave 10/1948 = **18 / 8460** (positions 1506+478=1984) |
| Product edited | **No** |
