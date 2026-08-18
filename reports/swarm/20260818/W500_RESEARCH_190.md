# W500_RESEARCH_190 — `CTraderFixSession.cs` live `35=D` / NewOrderSingle search

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_190.md` |
| Agent / slot | W500 research **190** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (also grepped `D:\Projects\YoPips\Backend\C++ Backend PropFirm`) |
| Assigned file | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` |
| Topic | Search `CTraderFixSession.cs` for `35=D` or `NewOrderSingle`. **Verdict FAIL if live send exists.** |
| Goal context | Fetch ALL Achiever + Starwave groups and ALL manager traders; **copy to cTrader must not send live orders yet (no loss).** |
| Product source modified | **No.** This report is the only product-adjacent write from this slot besides swarm index/log pointers. |
| Test source modified | **No.** |
| Secrets printed | **None.** `CTRADER_FIX_PASSWORD` / tag 554 values not dumped. `.env` quoted only for the boolean `REAL_COPY_EXECUTION_ENABLED` key and public host/account ids already used as source defaults. |
| Method | Full `read_file` of assigned file (**135 / 135** lines) twice this slot. Targeted `grep` of that file, all of `src\Fix.CTrader`, product `*.cs` under `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tools` + `D:\Prop\tests`, product `*.cs`/`*.json`/`*.csproj`/`*.tsx`/`*.ts` for `35=D` / `(35, "D")` / `Build("D"`, YoPips C++ `src` for `CTraderFixSession` / `35=D` / `NewOrderSingle` / `FIX.4`. Supporting reads: hosted logon, options, DI L39–42, fix-worker, API `/api/settings` + `/api/reconciliation/status`, `LiveRuntimeStatus`, `TraderStateMachine`, `CopyTradingService` persist `AllowFixSend:=false`, quote service, `NativeMt5BrokerConnector` group/user walks, sibling `CTraderFixDemoTestTrade.cs` (demo-gated, not assigned), `LIVE_MANAGER_FETCH_MEASURED.md` + re-sum of `LIVE_GROUPS_AND_TRADERS.json` (08:42Z). **No TLS opened this slot. No Logon sent this slot. No order sent. No Manager re-attach.** |
| Binding law | Architecture §§32–34 / §41 / §68 / §70; A25; A32 (RoE `35=D`); A42 (never retry unknown as `35=D`); A101 item 12; E002 / E034 |
| Siblings (same assigned file; not this measurement) | W500_RESEARCH_10 / 30 / 50 / 70 / 90 / 110 / 130 / 150 |

**Honesty rule:** a comment, log line, `LastError` string, or helper *name* containing `NewOrderSingle` is **not** a FIX `MsgType=D` builder. A live TLS **Logon `35=A`** is **not** a NewOrderSingle. `AllowFixSend` / `MayRetryNewOrderSingle` / `RealCopyExecutionEnabled` are **not** socket writers. `35={msgType}` in a reject `LastError` interpolates the **inbound** tag 35. Absence of `35=D` in the assigned session type is **`SAFE_BY_ABSENCE`**, not a unit-tested refuse-on-LoggedOn-TRADE gate. Do **not** tick Architecture §68 / §70 from this file. Do **not** print FIX passwords.

**Stale-sibling warning (W500_110 and earlier same-file slots):** those reports claimed DI + hosted logon **hard-pin** `RealCopyEnabled=false` and `.env` L73 `false`. **Current disk (slot 190) is different:** DI L41 binds `REAL_COPY_EXECUTION_ENABLED`; lab `.env` L73 is **`true`**; hosted logon **does not** overwrite the flag. That residual does **not** create a sender in `CTraderFixSession`. Do not copy the old pin-false claim.

**Stale-sibling warning (W500_150 “product `35=D` = 0” as a blanket):** the **literal** string `35=D` is still **0** in product `*.cs`/`*.json`/`*.csproj`/`*.ts`/`*.tsx`. A **sibling** type `CTraderFixDemoTestTrade` now encodes `Build("D", …)` (tag 35 = `msgType`) on a **demo-gated** standalone path. That is **not** `CTraderFixSession` and is **not** the copy hop. Slot 150 did not name that sibling as a product `35=D` string (correct on the literal). This slot names it so later readers do not treat “no `35=D` token” as “no order encoder exists anywhere.”

---

## 0. Verdict (binding)

**PASS — live `35=D` / NewOrderSingle send does not exist in `CTraderFixSession.cs`.**

Assigned FAIL condition (“FAIL if live send exists”) is **not met** for the assigned type. `CTraderFixSession` cannot place a cTrader order. Copy-to-cTrader **cannot lose live Pepperstone capital through this file**, because there is no order encoder and the only `WriteAsync` emits Logon `35=A`.

| Claim | Result | Class |
|---|---|---|
| Literal `35=D` in `CTraderFixSession.cs` | **0 hits** | **MISSING** builder |
| `NewOrderSingle` in `CTraderFixSession.cs` | **0 hits** | **MISSING** |
| `(35, "D")` / `new(35, "D")` / `MsgType = "D"` in assigned file | **0 hits** | **MISSING** |
| `OrderQty` / `ClOrdID` / `OrdType` / `StopPx` / `Side` / tags 11/38/40/54 in assigned file | **0 hits** | no order fields |
| Outbound tag 35 actually built | **`"A"` only** (`BuildLogon` L96) | Logon, not order |
| `ssl.WriteAsync` count in assigned file | **1** — bytes of that Logon (L49) | not an order send |
| `TcpClient` / `SslStream` kept for a later `35=D` | **No** — `using` / `await using` dispose before return | no TRADE keep-alive |
| `GuardedNewOrderSingle` / `SubmitNewOrder` / `BuildNewOrder` | **0** in assigned file | choke **MISSING** |
| QuickFIX/n / `SendToTarget` in product `*.cs` / `*.csproj` | **0 hits** (`Fix.CTrader.csproj` has Hosting/Config/Logging/EF only) | initiator **MISSING** |
| Literal `35=D` / `(35, "D")` in product `*.cs` / `*.json` / `*.csproj` / `*.tsx` / `*.ts` | **0 hits** under `D:\Prop` | no second *literal* builder |
| Sibling `CTraderFixDemoTestTrade.Build("D", …)` | **Exists** (L139 / L163 / L197) | **demo-gated**; **not** assigned file; **not** copy hop |
| Product callers of `CTraderFixDemoTestTrade.SendAsync` | **`tools/DemoFixTestTrade/Program.cs` only** | hosted copy/logon **0** |
| YoPips C++ `src` `CTraderFixSession` / `35=D` / `NewOrderSingle` / `FIX.4` | **0 hits** | not a second cTrader sender |
| Prop `src\Mt5` `DealerSend` / `SendTrade` / `OrderSend` | **0 hits** | Manager path is **read** |
| `CopyTradingService.NewOrderSingleImplemented` | **`const false`** (L16) | persist `AllowFixSend:=false` (L192) |
| `ExecutionIntent` writers (`new ExecutionIntent` / `ExecutionIntents.Add`) | **0** in product `*.cs` | no send row |
| `CanPromoteToLive` | **`=> false`** (`TraderStateMachine` L211) | no auto LIVE |
| Live `35=D` if API/worker/copy process starts now via this type | **Impossible from this type** | **`SAFE_BY_ABSENCE`** |
| Slot FAIL (live send exists in assigned file)? | **No** | verdict **PASS** |

One-line:

```text
CTraderFixSession.cs (135/135): NewOrderSingle=0; 35=D=0; only outbound MsgType is A (Logon); one WriteAsync; sockets disposed. Copy hop const NewOrderSingleImplemented=false. Sibling demo helper can Build("D") but is demo-gated + tools-only. Env REAL_COPY=true armed. SAFE_BY_ABSENCE. PASS.
```

Do **not** treat env `REAL_COPY_EXECUTION_ENABLED=true` as a send license. Do **not** add a `35=D` sender in this task. Do **not** invoke `CTraderFixDemoTestTrade` from the copy host.

---

## 1. Assigned-file census (measured this pass)

Path: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`  
Read: **135 / 135** lines. File ends at L135 `}`. Independent re-read for slot **190** (same length as W500_50 / 70 / 90 / 110 / 130 / 150). Second `read_file` this slot confirmed the file did **not** grow a sender while the sibling demo helper was edited (`flattenOnly`).

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

`grep` of `WriteAsync|ssl.Write|TcpClient` under `D:\Prop\src\Fix.CTrader`: those sites exist **only** in this file (one write) and in sibling `CTraderFixDemoTestTrade.cs` (separate type; see §3.1).

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
        _log.LogInformation(
            "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
            quote.LoggedOn, trade.LoggedOn, _runtime.RealCopyEnabled, account);
```

Hard facts from that caller:

- Ports **5211 QUOTE** / **5212 TRADE** (SSL). Plain 5201/5202 are never used here.
- Default TargetCompID **`cServer`**. Default host **`demo-us-eqx-01.p.c-trader.com`** (demo Pepperstone, account default `5328266`). Not a live `35=D`.
- Tag 553 username is the **integer account id**, not SenderCompID (comment L45).
- After both probes the hosted service **reads** `_runtime.RealCopyEnabled` for the log line. It does **not** assign `false` (W500_110 “re-pin false” is **stale**).
- The word `NewOrderSingle` appears **only** in the log format string (L69: “still unimplemented”). That is not a builder.
- Persist writes `FixSessionState` host/port/status only. No ClOrdID, no order row.
- After one reply the session type **disposes** TCP/SSL. There is no keep-alive TRADE initiator.

Password gate: if `CTRADER_FIX_PASSWORD` is missing or contains the literal `<SECRET>`, logon is **skipped** entirely (`CTraderFixLogonHostedService.cs` L33–38). This slot did **not** read the secret value.

DI **no longer** pins the flag false. Measured:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`CTraderFixLogonHostedService` is still registered at L58 of the same file. Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true` (boolean only). That **arms the runtime bool**. It still cannot emit `35=D` from this type because no builder exists. `LiveRuntimeStatus.Snapshot()` documents the armed case as “NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.” (L43).

`CTraderFixOptions.RealCopyExecutionEnabled` still **defaults false** (L35). Nothing in product binds env onto that POCO (`CTrader__RealCopyExecutionEnabled` is unused). The **runtime** bool is a different object.

---

## 3. Neighbor types in `Fix.CTrader` (assigned file still cannot send)

Product `.cs` under `D:\Prop\src\Fix.CTrader` (8 files + csproj):

| File | Role | Live socket? |
|---|---|---|
| `Sessions\CTraderFixSession.cs` | one-shot TLS Logon | **Yes**, `35=A` only |
| `Sessions\CTraderFixDemoTestTrade.cs` | standalone demo test-trade | **Yes, if invoked** — `Build("D")`; **demo gate**; **not** this slot’s assigned type |
| `Hosting\CTraderFixLogonHostedService.cs` | two `TryLogonAsync` + persist | via session type (`35=A`) |
| `Configuration\CTraderFixOptions.cs` | POCO; `RealCopyExecutionEnabled = false` | no |
| `Services\CTraderQuoteService.cs` | in-memory tag lists `y` / `V` | **No** callers write them |
| `Services\FixSessionOwnership.cs` | in-memory fencing lock | no FIX |
| `Parsing\FixMessageParser.cs` | pipe parser/builder for tests | no TCP |
| `Testing\FixSimulationHarness.cs` | inbound stand-ins `A`/`3`/`0`/`y`/`X`/`8` | in-process only; **0** product callers |

`grep` of `(35,` under `D:\Prop\src\Fix.CTrader`:

| File | Tag 35 values | Wired to a live socket? |
|---|---|---|
| `Sessions\CTraderFixSession.cs` L96 | **`A`** | **Yes**, one-shot TLS Logon |
| `Sessions\CTraderFixDemoTestTrade.cs` L255 | **`msgType` parameter** (`A` / `x` / `AN` / `D`) | **Yes, only if** `SendAsync` is called and the demo gate passes |
| `Services\CTraderQuoteService.cs` L113 / L127 | `y` (SecurityListRequest), `V` (MarketDataRequest) | **No.** `Build*Tags()` returns in-memory lists. **Zero** product callers outside that file. |
| `Testing\FixSimulationHarness.cs` | `A`, `3`, `0`, `y`, `X`, `8` | In-process harness. `8` is ExecutionReport (inbound stand-in). **Zero** callers. |

`CTraderQuoteService` comments describe instrument discovery + MD subscribe. That is **not** NewOrderSingle. Those tag lists are never passed to `CTraderFixSession.Assemble` or `WriteAsync`.

`FixSessionOwnership` is an in-memory fencing lock **commented** as “single instance allowed to place/accept execution intents.” It never builds FIX.

`TraderIntelligence.Fix.CTrader.csproj`: net8.0; project refs Domain + Application; packages Hosting/Configuration/Logging abstractions + EFCore. **No** `QuickFIXn.Core`, **no** `QuickFIXn.FIX44`.

### 3.1 Sibling encoder (not FAIL for this slot)

`CTraderFixDemoTestTrade.SendAsync` (current disk, includes `flattenOnly = false`):

- Fail-closed demo gate **before** TCP (L43–60): host must start `demo-`; SenderCompID must start `demo.`; refuse `live.` / `live-`; refuse account **`1369850`**.
- After TRADE logon `35=A`, it can write `35=x` (SecurityListRequest), `35=AN` (RequestForPositions), then **`Build("D", …)`** to flatten an existing gold pos, open 1 unit (`38=1`), and flatten the fill.
- **Only** product-tree caller: `D:\Prop\tools\DemoFixTestTrade\Program.cs` L33. **Zero** hits from `CTraderFixLogonHostedService`, `CopyTradingService`, `CopyTradingHostedService`, `apps\api`, `apps\fix-worker`, `apps\mt5-worker`.
- This is a **demo** socket writer, **not** the copy book, **not** live Pepperstone `1369850`, **not** `CTraderFixSession`.
- Slot 190 **did not invoke it**.

FAIL predicate for this slot is **live send in `CTraderFixSession`**. A demo-gated standalone helper does **not** trip that predicate. It **is** a residual: the product *assembly* now contains an order encoder. Do not wire it to the copy host.

---

## 4. Product-wide `NewOrderSingle` / `35=D` (name ≠ send)

`grep` `35=D` / `(35, "D")` / `new(35, "D")` / `MsgType = "D"` on `*.cs` / `*.json` / `*.csproj` / `*.tsx` / `*.ts` under `D:\Prop`: **0 hits**.

Product `*.cs` tokens named `NewOrderSingle` (none of these encode tag 35=`D` in `CTraderFixSession`):

| Location | What it is | Sends `35=D`? |
|---|---|---|
| `src\Fix.CTrader\Configuration\CTraderFixOptions.cs:33–35` | XML comment + `RealCopyExecutionEnabled` default **`false`** | **No** |
| `src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs:69` | log “still unimplemented” | **No** |
| `src\Application\Runtime\LiveRuntimeStatus.cs:42–44` | `copyNote` — armed or not, text says no ticket | **No** |
| `src\Application\Copy\CopyTradingModels.cs:9` | DTO field name | **No** |
| `src\Infrastructure\Copy\CopyTradingService.cs:16,49,59,198,243–244` | `const NewOrderSingleImplemented = false`; SHADOW only; blocker `SAFE_BY_ABSENCE` | **No** |
| `src\Infrastructure\Hosting\CopyTradingHostedService.cs:30` | log “Live NewOrderSingle still blocked” after shadow tick | **No** |
| `src\Infrastructure\Seeding\DemoSeeder.cs:101` | TRADE `LastError` string | **No** |
| `src\Infrastructure\Seeding\BrokerCatalogSeed.cs:105` | TRADE `LastError` “NewOrderSingle off” | **No** |
| `apps\fix-worker\Worker.cs:22,41,46` | startup log / LastError / warning | **No** |
| `apps\api\Program.cs:69` | `/api/reconciliation/status` note | **No** |
| `src\Domain\Execution\ExecutionOrderStateMachine.cs:35–36` | `MayRetryNewOrderSingle` status math (`NotSent`/`Rejected` only) | **No** |

`CopyTradingService` is the live copy hop (hosted every 20 s via `CopyTradingHostedService`). Measured safety:

- `NewOrderSingleImplemented = false` (const L16).
- `VenueReconciled = false` (const L15).
- Persist of `RiskDecisionRecord.AllowFixSend` is **hardcoded `false`** (L192) even if `RiskEngine.Evaluate` returns `allowSend`.
- Live-send branch (L198) requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled`. The const `false` makes that branch **unreachable**. Else path writes `SHADOW_ONLY` + optional in-memory `ShadowCopyEngine.SimulateEntry`.
- Product `*.cs` has **0** `new ExecutionIntent` / `ExecutionIntents.Add`.
- `TraderStateMachine.FromBaseline` reachable set `{INSUFFICIENT_DATA, EARLY_SCORE, WATCH, SHADOW, RISK_BLOCKED}` — **no LIVE**. `CanPromoteToLive => false` (L211).

`apps\fix-worker\Worker.cs` reads `CTrader:RealCopyExecutionEnabled` with fallback **false** (nested key; **not** the flat env name). If that nested key is true it **only logs a warning** and still writes `LastError = "No live TRADE socket. NewOrderSingle remains off."` It never calls `CTraderFixSession` and never builds `35=D`. It **stamps Disconnected** on both session rows every 15 s.

API `/api/settings` exposes `runtime.RealCopyEnabled` (lab env `true` would show **true**) and `FEATURE_COPY_TRADING_ENABLED` literal **true** (`Program.cs` L74–77). `/api/reconciliation/status` is a stub (`unknownPositions=0`, note “NewOrderSingle still off”). Recon is **not** implemented.

Prop `src\Mt5` has **0** `DealerSend` / `SendTrade` / `OrderSend`. Manager connector is **read**.

---

## 5. YoPips C++ backend (relevant only as contrast)

Tree searched: `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src`

| Pattern | Hits |
|---|---|
| `CTraderFixSession` | **0** |
| `NewOrderSingle` | **0** |
| `35=D` | **0** |
| `FIX.4` / `CTraderFix` / `c-trader` / `QuickFIX` | **0** |

That tree **does** have MT5 `DealerSend` / `DealerSendOrder` / `SendTrade` (`mt5_manager.cpp`, `mt5_pool.cpp`, `mt5_http_client.cpp`, `trade_execution_service.cpp`). That is the **prop-firm MT5 dealer** path for YoPips challenge accounts, **not** cTrader FIX, and it is **not** called from `CTraderFixSession.cs` or from any `D:\Prop\src` C# file. Slot 190 does **not** treat YoPips `DealerSend` as a live cTrader `35=D`. It also does **not** authorize using that dealer as a copy destination.

---

## 6. Goal coupling: all-groups/all-traders vs no-loss copy

Live Manager census already measured (do not re-invent; this slot did **not** re-attach to either Manager). JSON re-summed this slot from `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (`utc` `2026-08-18T08:42:16.8519545+00:00`):

| Broker | Groups (header) | Accounts (header) | Group-row sum (this slot) | Positions |
|---|---:|---:|---:|---:|
| ACHIEVER | 8 | 6512 | 2+179+4+5+4+6295+0+23 = **6512** | 1506 |
| STARWAVEFX | 10 | 1948 | 11+4+170+1735+22+0+0+4+0+2 = **1948** | 478 |
| **Total** | **18** | **8460** | **8460** | 1984 |

Achiever groups (manager-visible): `contest\yo-1step`, `contest\yo-2step`, `contest\yo-instant`, `contest\yo-payp`, `demo\yo-1step`, `demo\yo-2step`, `demo\yo-instant`, `demo\yo-payp`.

Starwave groups (manager-visible): `Starwave\cent\FX1\grp1`, `grp2`; `Starwave\demo\FX2\grp1`, `grp2`; `Starwave\real\FX3\grp1`–`grp5`, `Starwave\real\FX3\LP`.

Fetch path on current disk (`NativeMt5BrokerConnector`):

- Groups: `GroupRequestArray("*")` L155; cache fallback `GroupTotal`/`GroupNext` only if the request list is empty.
- Traders: per-group `UserRequestArray` L223; cache `UserGetByGroup` only on hard fail; empty → `UserLogins` + `UserRequestByLogins`.
- Ingest `DealIngestionService.SyncCatalogAsync` uses `GetAccountsAsync(null)` → all groups (L48 / L62). Flag-blind.

This slot **re-confirms** the copy half of the goal on current disk:

- Fetch path is **Manager read** — not this FIX type.
- Copy destination **must stay off** until §68 19/19 + §70 14/14 + persist-before-send + explicit flag **and** an actual *copy* sender. Those gates are **not** re-scored here (siblings A100 / A101 still historically **0 PASS**).
- Current safety for **copy** capital: **`SAFE_BY_ABSENCE`** of `35=D` in `CTraderFixSession` (the only FIX writer the hosted copy/logon path can reach), plus `CopyTradingService` const `NewOrderSingleImplemented=false`.

Logon `35=A` **can** still leave this process toward `*.c-trader.com` when the password slot is populated (QUOTE 5211 / TRADE 5212; hosted default host is the **demo** EQX). That is **session proof**, not an order. It cannot attach qty/side/symbol/price. Sockets are dead after the one reply.

`LIVE_MANAGER_FETCH_MEASURED.md` still says “`REAL_COPY` forced false.” That line is **stale** vs current DI. Its “No `35=D` NewOrderSingle exists in `CTraderFixSession`” sentence remains true.

---

## 7. What would have been FAIL

The slot brief: **FAIL if live send exists.** Any of the following **in `CTraderFixSession.cs`** (or a copy-path caller of that type) on current disk would have failed the slot:

1. `BuildLogon` or a sibling builder **in this file** emitting `(35, "D")`.
2. A second `WriteAsync` of a constructed NewOrderSingle after Logon **from this type**.
3. A kept TRADE `SslStream` / QuickFIX initiator that `SendToTarget`s `MsgType=D` from the hosted copy/logon path.
4. `OrderQty` / `ClOrdID` / `Side` / `OrdType` assembled onto the live socket **from this type**.

**None** of those exist in `CTraderFixSession.cs`.

Env `REAL_COPY_EXECUTION_ENABLED=true` is **not** a FAIL by itself. The FAIL predicate is **live send exists**, not “flag is true.”

`CTraderFixDemoTestTrade` is **not** this slot’s FAIL: demo gate + tools-only + live account `1369850` refused. If a later slot’s predicate is “any order encoder in the Fix.CTrader assembly,” that sibling would be in scope. This slot’s assigned search is `CTraderFixSession.cs`.

---

## 8. Residual (honest, not FAIL)

| Residual | Why it is not this slot’s FAIL |
|---|---|
| Lab `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` now bound by DI L41 | Arms a bool / API settings flag. No encoder in assigned file. |
| Hosted logon no longer re-pins `RealCopyEnabled=false` | Flag visibility only. Log line still says unimplemented. |
| `FEATURE_COPY_TRADING_ENABLED=true` on `/api/settings` | Shadow pipeline, not FIX send. |
| `35=A` can go to demo (or live) Pepperstone cTrader if password is real | Not NewOrderSingle; no capital move from this type |
| Cert callback `(_, _, _, _) => true` | TLS identity not pinned; still Logon-only |
| `SAFE_BY_ABSENCE` ≠ unit-tested refuse-on-LoggedOn-TRADE | A101 item 12 stays unproven |
| No persist-before-send / `GuardedNewOrderSingle` | Cannot send from this type, so cannot violate it **yet** |
| Three hosts can each register `CTraderFixLogonHostedService` | Duplicate Logon risk, not duplicate `35=D` |
| `MayRetryNewOrderSingle` exists as status math | Never opens a socket |
| `RiskEngine.Evaluate` can set `AllowFixSend` true | Persist overwrites to `false`; const sender flag is false |
| Quote-service `y`/`V` lists exist | Never written to a socket |
| `CTraderFixDemoTestTrade` can `Build("D")` | Demo-gated; tools-only; live `1369850` refused; not assigned file |
| YoPips `DealerSend` exists in a **different** tree | MT5 dealer, not cTrader FIX; not called from this file |
| `Assemble` is a generic encoder | Only fed `(35, "A")` today in this file |
| `LIVE_MANAGER_FETCH_MEASURED.md` / README / CREDENTIALS still say flag forced false | Stale docs; do not treat as current pin |

---

## 9. Do / Do not

**Do**

- Treat `CTraderFixSession` as Logon/`35=A` only until a separately reviewed **copy** sender exists **and** §68+§70 PASS.
- Keep Manager fetch of **all** Achiever + Starwave groups / logins as a **read** path.
- Keep `CopyTradingService.NewOrderSingleImplemented = false` and persist `AllowFixSend:=false`.
- Flip lab `.env` `REAL_COPY_EXECUTION_ENABLED` back to **`false`** as hygiene (does not change this slot’s PASS).
- Keep `CTraderFixDemoTestTrade` off the hosted copy path; keep the live-account refuse.

**Do not**

- Add `35=D` / `F` / `G` to `CTraderFixSession` in this task.
- Treat QUOTE/TRADE `LoggedOn=true` or `RealCopyEnabled=true` as license to copy.
- Print `CTRADER_FIX_PASSWORD` or tag 554.
- Claim “no-loss live copy” is implemented. **No-loss live copy is impossible today** because there is no gated copy send path. The operating mode is **fetch + logon/recon + SHADOW intents**.
- Confuse YoPips `DealerSend` with a cTrader NewOrderSingle.
- Cite W500_110 “DI/hosted pin false” as current law.
- Wire `CTraderFixDemoTestTrade` into `CopyTradingService`.

---

## 10. Slot close

| Item | Value |
|---|---|
| Slot | **190** |
| Verdict | **PASS** |
| Live `35=D` / NewOrderSingle send exists in `CTraderFixSession.cs`? | **No** |
| Risk to capital from assigned file | **None** (`SAFE_BY_ABSENCE`) |
| Evidence | Full 135-line re-read (×2); 0 hits for `35=D` and `NewOrderSingle` in `CTraderFixSession.cs`; sole outbound MsgType `(35, "A")`; one `WriteAsync`; sockets disposed; product literal `35=D` = 0; copy hop const `NewOrderSingleImplemented=false` + persist `AllowFixSend:=false`; YoPips C++ `src` has 0 cTrader FIX senders; census 8+10 / 6512+1948 = **18 / 8460** re-summed |
| Residual (not FAIL) | DI binds env; lab `.env` L73 **`true`**; hosted logon does not re-pin false; sibling demo helper can `Build("D")` (tools-only, demo-gated) |
| Census cited (not re-attached) | Achiever 8/6512 + Starwave 10/1948 = **18 / 8460** (JSON 08:42Z, group-row sums checked) |
| Product edited | **No** |
