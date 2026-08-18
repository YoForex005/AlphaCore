# W500_RESEARCH_130 — `CTraderFixSession.cs` live `35=D` / NewOrderSingle search

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_130.md` |
| Agent / slot | W500 research **130** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (also grepped `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` for a second sender) |
| Assigned file | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` |
| Topic | Search `CTraderFixSession.cs` for `35=D` or `NewOrderSingle`. **Verdict FAIL if live send exists.** |
| Goal context | Fetch ALL Achiever + Starwave groups and ALL manager traders; **copy to cTrader must not send live orders yet (no loss).** |
| Product source modified | **No.** Report + swarm index/log pointers only. |
| Test source modified | **No.** |
| Secrets printed | **None.** `CTRADER_FIX_PASSWORD` / tag 554 / manager passwords / proxy auth **not** read from `.env` and not dumped. Flag *names* and boolean arm state are not secrets. |
| Method | Full `read_file` of assigned file (**135 / 135** lines). Targeted `grep` of that file, all of `src\Fix.CTrader`, product `*.cs` under `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tests`, product `*.cs`/`*.json`/`*.csproj`/`*.tsx`/`*.ts` for `35=D` / `(35, "D")`, and YoPips C++ `src`. Supporting reads: hosted service, options, DI, copy service, copy hosted service, fix-worker, API `/api/settings` + `/api/reconciliation/status`, `LiveRuntimeStatus`, FSM, `ClOrdIdFactory`, RiskEngine `AllowFixSend`, quote service, parser, ownership fence, harness, `NativeMt5BrokerConnector` group/user walks, `EfTradingStore` SHADOW_ONLY, `LIVE_MANAGER_FETCH_MEASURED.md` + JSON group-row re-sum. **No TLS opened this slot. No Logon sent this slot. No order sent. No Manager re-attach.** |
| Binding law | Architecture §§32–34 / §41 / §68 / §70; A25; A32 (RoE `35=D`); A42 (never retry unknown as `35=D`); A101 item 12; E002 / E034 |
| Siblings (same assigned file; not this measurement) | W500_RESEARCH_10 / 30 / 50 / 70 / 90 / 110 |

**Honesty rule:** a comment, log line, `LastError` string, or helper *name* containing `NewOrderSingle` is **not** a FIX `MsgType=D` builder. A live TLS **Logon `35=A`** is **not** a NewOrderSingle. `AllowFixSend` / `MayRetryNewOrderSingle` / `RealCopyExecutionEnabled` are **not** socket writers. `35={msgType}` in a reject `LastError` interpolates the **inbound** tag 35. Absence of `35=D` is **`SAFE_BY_ABSENCE`**, not a unit-tested refuse-on-LoggedOn-TRADE gate. Do **not** tick Architecture §68 / §70 from this file. Do **not** print FIX passwords.

**Stale-sibling correction (binding):** W500_RESEARCH_90 / 110 cite `_runtime.RealCopyEnabled = false` in `CTraderFixLogonHostedService` and `RealCopyEnabled = false` hard-pin in DI. **Those snippets are not on current disk.** Measured 2026-08-18 this slot: DI binds the env key; hosted service **logs** `RealCopyArmed` and does **not** force false. `.env` has `REAL_COPY_EXECUTION_ENABLED=true`. That **arms a flag**. It still **cannot** emit `35=D` because no builder exists.

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
| Product `*.cs` `WriteAsync` / `TcpClient` / `SslStream` | **only** those three lines in this file | no second live FIX writer |
| `GuardedNewOrderSingle` / `SubmitNewOrder` / `BuildNewOrder` | **0** in assigned file and `Fix.CTrader` | choke **MISSING** |
| QuickFIX/n / `SendToTarget` in product `*.cs` / `*.csproj` | **0 hits** | initiator **MISSING** |
| Product `*.cs` / `*.json` / `*.csproj` / `*.tsx` / `*.ts` `35=D` / `(35, "D")` | **0 hits** under `D:\Prop` | no second builder |
| Product `new ExecutionIntent` / `ExecutionIntents.Add` | **0 hits** | no persist-before-send row |
| YoPips C++ `src` `CTraderFixSession` / `35=D` / `NewOrderSingle` / `FIX.4` | **0 hits** | not a second cTrader sender |
| Prop `src\Mt5` `DealerSend` / `SendTrade` / `OrderSend` | **0 hits** | Manager path is **read** |
| `CopyTradingService.NewOrderSingleImplemented` | **`const false`** (L16) | dead LIVE branch cannot send |
| Live `35=D` if process starts now | **Impossible from this type** | **`SAFE_BY_ABSENCE`** |
| Slot FAIL (live send exists)? | **No** | verdict **PASS** |

One-line:

```text
CTraderFixSession.cs (135/135): NewOrderSingle=0; 35=D=0; only outbound MsgType is A (Logon); one WriteAsync; sockets disposed. Product 35=D=0. Flag may be armed; send still absent. SAFE_BY_ABSENCE. PASS.
```

Do **not** treat env `REAL_COPY_EXECUTION_ENABLED=true` as a send license. Do **not** add a `35=D` sender in this task.

---

## 1. Assigned-file census (measured this pass)

Path: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`  
Read: **135 / 135** lines. File ends at L135 `}`. Independent re-read for slot **130** (same length as W500_50 / 70 / 90 / 110). SHA-256 **not recomputed** this slot (no shell); identity is the full line census.

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

`grep` of `WriteAsync|TcpClient|SslStream` on product `*.cs` under `D:\Prop`: **exactly those three lines** in this file (L35 `TcpClient`, L39 `SslStream`, L49 `WriteAsync`). No other live FIX writer exists in the product C#.

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

Hosted service **as it exists on disk this pass** (W500_90/110 snippets that assigned `RealCopyEnabled = false` here are **stale**):

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
        _log.LogInformation(
            "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
            quote.LoggedOn, trade.LoggedOn, _runtime.RealCopyEnabled, account);
```

Hard facts from that caller:

- Ports **5211 QUOTE** / **5212 TRADE** (SSL). Plain 5201/5202 are never used here.
- Default TargetCompID **`cServer`**. Default host `live-us-eqx-01.p.c-trader.com`.
- Tag 553 username is the **integer account id**, not SenderCompID (comment L45).
- After both probes: **`_runtime.RealCopyEnabled` is read, not forced false.**
- The word `NewOrderSingle` appears **only** in the log format string (L69: “still unimplemented”). That is not a builder.
- Persist writes `FixSessionState` host/port/status only. No ClOrdID, no order row.
- After one reply the session type **disposes** TCP/SSL. There is no keep-alive TRADE initiator.

Password gate: if `CTRADER_FIX_PASSWORD` is missing or contains the literal `<SECRET>`, logon is **skipped** entirely (`CTraderFixLogonHostedService.cs` L33–38). This slot did **not** read the secret value.

DI **does not** hard-pin the flag (current disk):

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`CTraderFixLogonHostedService` is added at L58 of the same file. `CopyTradingHostedService` is added at L59 and only calls `GenerateShadowIntentsAsync`.

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

`grep` of `(35,` under `D:\Prop\src\Fix.CTrader` (this pass):

| File | Tag 35 values | Wired to a live socket? |
|---|---|---|
| `Sessions\CTraderFixSession.cs` L96 | **`A`** | **Yes**, one-shot TLS Logon |
| `Services\CTraderQuoteService.cs` L113 / L127 | `y` (SecurityListRequest), `V` (MarketDataRequest) | **No.** `Build*Tags()` returns in-memory lists. **Zero** product callers outside that file. |
| `Testing\FixSimulationHarness.cs` | `A`, `3`, `0`, `y`, `X`, `8` | In-process harness. `8` is ExecutionReport (inbound stand-in). Tag 11 `ClOrdID` is on the **ER**, not a NewOrderSingle. |

`CTraderQuoteService` comments describe instrument discovery + MD subscribe. That is **not** NewOrderSingle. Those tag lists are never passed to `CTraderFixSession.Assemble` or `WriteAsync`.

`FixMessageParser` is a test parser/builder (pipe delimiter, checksum). It does not open TCP.

`FixSessionOwnership` is an in-memory fencing lock commented as “single instance allowed to place/accept execution intents.” `ExecutionIntentsAllowed` is a bool (`L111`). `grep` found **0** `new ExecutionIntent` / `ExecutionIntents.Add` in product C#.

`TraderIntelligence.Fix.CTrader.csproj`: net8.0; project refs Domain + Application; packages Hosting/Configuration/Logging abstractions + EFCore. **No** `QuickFIXn.Core`, **no** `QuickFIXn.FIX44`. `grep` of `QuickFIX|QuickFix|GuardedNewOrderSingle|BuildNewOrder|SendToTarget` on product `*.cs`/`*.csproj`: **0**.

`ClOrdIdFactory` (`src\Domain\Execution\ClOrdIdFactory.cs`) formats `TI{yyyyMMddHHmmss}{seq}{guid16}`. It is a string helper. It is **not** called from `CTraderFixSession` and never writes tag 11 to a socket.

---

## 4. Product-wide `NewOrderSingle` / `35=D` (name ≠ send)

`grep` `35=D` / `(35, "D")` / `new(35, "D")` / `MsgType = "D"` on `*.cs` / `*.json` / `*.csproj` under `D:\Prop`: **0 hits**.

Product `*.cs` tokens named `NewOrderSingle` (none encode tag 35=`D`):

| Location | What it is | Sends `35=D`? |
|---|---|---|
| `src\Fix.CTrader\Configuration\CTraderFixOptions.cs:33–35` | XML comment + `RealCopyExecutionEnabled` default **`false`** | **No** |
| `src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs:69` | log “still unimplemented” | **No** |
| `src\Application\Runtime\LiveRuntimeStatus.cs:42–44` | `copyNote`: armed text still says “NewOrderSingle still unimplemented”; unarmed says “disabled. SHADOW/CopyIntent only” | **No** |
| `src\Application\Copy\CopyTradingModels.cs:9` | DTO field `NewOrderSingleImplemented` | **No** |
| `src\Infrastructure\Copy\CopyTradingService.cs:16,49,59,198,243–244` | `const false`; blockers include “SAFE_BY_ABSENCE”; LIVE branch only sets status string | **No** |
| `src\Infrastructure\Hosting\CopyTradingHostedService.cs:30` | log “Live NewOrderSingle still blocked” | **No** |
| `src\Infrastructure\Seeding\DemoSeeder.cs:101` | TRADE `LastError` string | **No** |
| `src\Infrastructure\Seeding\BrokerCatalogSeed.cs:105` | TRADE `LastError` “NewOrderSingle off” | **No** |
| `apps\fix-worker\Worker.cs:22,41,46` | startup log / LastError / warning | **No** |
| `apps\api\Program.cs:69` | `/api/reconciliation/status` note | **No** |
| `apps\web\src\pages\OverviewPage.tsx:15` | dashboard copy | **No** |
| `apps\web\src\pages\ShadowPortfolioPage.tsx:7` | “Live NewOrderSingle remains disabled.” | **No** |
| `src\Domain\Execution\ExecutionOrderStateMachine.cs:35–36` | `MayRetryNewOrderSingle` status math (`NotSent`/`Rejected` only) | **No** |
| `tests\Unit\ExecutionAndSizingTests.cs:14` | asserts retry helper false after send-attempt **state** | **No** |

`apps\fix-worker\Worker.cs` reads `CTrader:RealCopyExecutionEnabled` with fallback **false**. If the key is true it **only logs a warning** and still writes `LastError = "No live TRADE socket. NewOrderSingle remains off."` It never calls `CTraderFixSession` and never builds `35=D`. Flipping that nested key **cannot** place an order.

```21:46:D:\Prop\apps\fix-worker\Worker.cs
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);
        // ...
                trade.LastError = "No live TRADE socket. NewOrderSingle remains off.";
        // ...
            if (real)
                _logger.LogWarning("Real copy is enabled in config, but worker still refuses NewOrderSingle until risk/reconciliation gates pass.");
```

API settings expose the **runtime** flag (now bound from env, not hardcoded false):

```71:78:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", (LiveRuntimeStatus runtime) => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = true
    },
```

`RiskEngine` can set `AllowFixSend` on a DTO (`src\Domain\Risk\RiskEngine.cs` L64 / L160 / L170 / L187) when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. **CopyTradingService overwrites the persisted record:** `AllowFixSend = false` (L192) regardless of `decision.AllowFixSend`. The only LIVE-looking branch (L198) is conjunct with `NewOrderSingleImplemented` (**const false**) and `VenueReconciled` (**const false**) and only mutates `intent.Status` to `LIVE_SEND_BLOCKED_UNIMPLEMENTED`. **No** product caller maps that boolean onto `ssl.WriteAsync` or `35=D`.

`BaselineScorer.CanPromoteToLive => false` (`src\Domain\Scoring\BaselineScorer.cs` L211). Trade #3 cannot auto-LIVE.

---

## 5. Flag arm vs send (honest residual, not FAIL)

| Source | Measured this slot |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** (unbound POCO; env name is not `CTrader__RealCopyExecutionEnabled`) |
| DI `LiveRuntimeStatus.RealCopyEnabled` | **bound** from `REAL_COPY_EXECUTION_ENABLED` == `"true"` (OrdinalIgnoreCase) |
| `CTraderFixLogonHostedService` | **does not** force the runtime flag false |
| `.env` key `REAL_COPY_EXECUTION_ENABLED` | **`true`** (flag only; no secret dumped) |
| `CopyTradingService.NewOrderSingleImplemented` | **`const false`** |
| `CopyTradingService.VenueReconciled` | **`const false`** |
| Persisted `RiskDecisionRecord.AllowFixSend` | **hardcoded false** at write |
| Product `35=D` builder | **0** |

W500_68 / 108 “flag pinned false (POCO/DI/hosted/.env/settings)” is **stale on DI + hosted + `.env`**. POCO default is still false. Settings API now **mirrors runtime**, so a process that loaded `.env` would advertise `featureFlags.REAL_COPY_EXECUTION_ENABLED=true` **and still cannot send**. That is an honesty/UI residual, not a live send.

Architecture §41: session-on + flag-true is **necessary and not sufficient**. Current disk: flag **may be true**; sender **absent**. Capital is safe **by absence**, not by a tested refuse-on-LoggedOn-TRADE gate (A101 item 12 still unproven).

---

## 6. YoPips C++ backend (relevant only as contrast)

Tree searched: `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` (directory exists; `src\core`, `src\http`, `src\services` present).

| Pattern | Hits |
|---|---|
| `CTraderFixSession` | **0** |
| `NewOrderSingle` | **0** |
| `35=D` | **0** |
| `FIX.4` / `CTraderFix` | **0** |

That tree **does** have MT5 dealer APIs in earlier measurements (`DealerSend` / `DealerSendOrder` in `mt5_manager.cpp` / pool / HTTP client). That is the **prop-firm MT5 dealer** path for YoPips challenge accounts, **not** cTrader FIX, and it is **not** called from `CTraderFixSession.cs`. Slot 130 does **not** treat YoPips `DealerSend` as a live cTrader `35=D`. It also does **not** authorize using that dealer as a copy destination.

Prop `src\Mt5` `DealerSend` / `SendTrade` / `OrderSend`: **0 hits**. `NativeMt5BrokerConnector` is Manager **read** (`GroupRequestArray`, `UserRequestArray`, deals/positions).

---

## 7. Goal coupling: all-groups/all-traders vs no-loss copy

Fetch path is **Manager read**, not this FIX type.

`NativeMt5BrokerConnector.GetGroupsCore` walks `GroupRequestArray("*")` then falls back to `GroupTotal`+`GroupNext` (`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` L155 / L174). `GetAccountsCore(null)` iterates **every** returned group and `UserRequestArray` / `UserGetByGroup` / `UserLogins` (`L201–232`). `DealIngestionService` calls `GetAccountsAsync(null)` (`DealIngestionService.cs` L48 / L62). Dashboard traders list is account-driven, not scores-only (prior slots; this slot did not re-open `EfDashboardQueries`).

Live Manager census already measured (this slot **re-summed** the JSON group rows; it did **not** re-attach):

| Source | Evidence |
|---|---|
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | Achiever **8 / 6512** (HTTP proxy) + Starwave **10 / 1948** (direct) = **18 / 8460** |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | `utc=2026-08-18T08:42:16Z`; ACHIEVER `groups=8` `accounts=6512` `openPositions=1506`; STARWAVEFX `groups=10` `accounts=1948` `openPositions=478` |
| Same JSON group-row sum | Achiever `2+179+4+5+4+6295+0+23 = 6512`; Starwave `11+4+170+1735+22+0+0+4+0+2 = 1948` |
| Positions | `1506+478 = 1984` |
| Same file / CREDENTIALS note | “No `35=D` NewOrderSingle exists in `CTraderFixSession`.” |

Achiever groups (manager-visible): `contest\yo-1step`, `contest\yo-2step`, `contest\yo-instant`, `contest\yo-payp`, `demo\yo-1step`, `demo\yo-2step`, `demo\yo-instant`, `demo\yo-payp`.

Starwave groups (manager-visible): `Starwave\cent\FX1\grp1`, `grp2`; `Starwave\demo\FX2\grp1`, `grp2`; `Starwave\real\FX3\grp1`–`grp5`, `Starwave\real\FX3\LP`.

This slot **re-confirms** the copy half of the goal on current disk:

- Fetch path is **Manager read** — not this FIX type. Catalog walk is `*` + all users.
- Copy destination **must stay off** until §68 19/19 + §70 14/14 + persist-before-send + a real sender + explicit reviewed go-live. Those gates are **not** re-scored here (siblings A100 / A101 still historically **0 PASS**).
- Current safety for capital: **`SAFE_BY_ABSENCE`** of `35=D` in the only live FIX writer.
- Copy writers: `CopyTradingService.GenerateShadowIntentsAsync` → `SHADOW_ONLY` + in-memory `ShadowCopyEngine.SimulateEntry`; `EfTradingStore` persist also stamps `Status = "SHADOW_ONLY"` (L307). **0** `ExecutionIntent` writers.

Logon `35=A` **can** still leave this process toward `*.c-trader.com` when the password slot is populated (QUOTE 5211 / TRADE 5212). That is **session proof**, not an order. It cannot attach qty/side/symbol/price. Sockets are dead after the one reply.

---

## 8. What would have been FAIL

The slot brief: **FAIL if live send exists.** Any of the following on current disk would have failed the slot:

1. `BuildLogon` or a sibling builder emitting `(35, "D")`.
2. A second `WriteAsync` of a constructed NewOrderSingle after Logon.
3. A kept TRADE `SslStream` / QuickFIX initiator that `SendToTarget`s `MsgType=D`.
4. `OrderQty` / `ClOrdID` / `Side` / `OrdType` assembled onto the live socket.

**None** of those exist in `CTraderFixSession.cs`. An armed `REAL_COPY_EXECUTION_ENABLED` flag **without** a builder is **not** a live send.

---

## 9. Residual (honest, not FAIL)

| Residual | Why it is not this slot’s FAIL |
|---|---|
| `35=A` can go to the live Pepperstone cTrader host if password is real | Not NewOrderSingle; no capital move |
| Cert callback `(_, _, _, _) => true` | TLS identity not pinned; still Logon-only |
| Env `REAL_COPY_EXECUTION_ENABLED=true`; DI binds it; hosted service no longer re-pins false | Flag arm ≠ encoder. `NewOrderSingleImplemented` still const false |
| `/api/settings` now mirrors runtime (can advertise true) | UI honesty residual; no socket write |
| `SAFE_BY_ABSENCE` ≠ unit-tested refuse-on-LoggedOn-TRADE | A101 item 12 stays unproven |
| No persist-before-send / `GuardedNewOrderSingle` | Cannot send, so cannot violate it **yet** |
| Three hosts can each register `CTraderFixLogonHostedService` | Duplicate Logon risk, not duplicate `35=D` |
| `MayRetryNewOrderSingle` exists as status math | Never opens a socket |
| `AllowFixSend` can be true on a risk DTO | Copy path overwrites persisted bool to false; no encoder consumes it |
| Quote-service `y`/`V` lists exist | Never written to a socket |
| YoPips `DealerSend` exists in a **different** tree | MT5 dealer, not cTrader FIX; not called from this file |
| SHA-256 of assigned file not recomputed | Identity is the 135-line full read; hash left to a hashing slot |
| W500_90 / 110 “flag forced false after logon” | **Stale.** Do not copy those snippets forward |

---

## 10. Do / Do not

**Do**

- Keep Manager fetch of **all** Achiever + Starwave groups / logins as a **read** path.
- Treat this file as Logon/`35=A` only until a separately reviewed sender exists **and** §68+§70 PASS.
- Treat env `REAL_COPY_EXECUTION_ENABLED=true` as an **operator wish**, not a send license.
- Prefer re-pinning `LiveRuntimeStatus.RealCopyEnabled=false` until a sender exists (honesty), but that pin is **not** what currently protects capital.

**Do not**

- Add `35=D` / `F` / `G` in this task.
- Treat QUOTE/TRADE `LoggedOn=true` as license to copy.
- Print `CTRADER_FIX_PASSWORD` or tag 554.
- Claim “no-loss live copy” is implemented. **No-loss live copy is impossible today** because there is no gated send path. The operating mode is **fetch + logon/recon + SHADOW intents only**.
- Confuse YoPips `DealerSend` with a cTrader NewOrderSingle.
- Cite W500_90 / 110 DI/hosted `RealCopyEnabled = false` as current truth.

---

## 11. Slot close

| Item | Value |
|---|---|
| Slot | **130** |
| Verdict | **PASS** |
| Live `35=D` / NewOrderSingle send exists? | **No** |
| Risk to capital from assigned file | **None** (`SAFE_BY_ABSENCE`) |
| Evidence | Full 135-line read; 0 hits for `35=D` and `NewOrderSingle` in `CTraderFixSession.cs`; sole outbound MsgType `(35, "A")`; one `WriteAsync`; sockets disposed; product `*.cs`/`*.json`/`*.csproj` have 0 `35=D`; 0 `ExecutionIntent` writers; YoPips C++ `src` has 0 cTrader FIX senders; copy const `NewOrderSingleImplemented=false` |
| Census cited (JSON re-summed, not re-attached) | Achiever 8/6512 + Starwave 10/1948 = **18 / 8460** (positions 1506+478=1984) |
| Residual (not FAIL) | `.env` flag **true**; DI binds it; hosted service no longer forces false; settings API can advertise true |
| Product edited | **No** |
