# W500_RESEARCH_170 — `CTraderFixSession.cs` live `35=D` / NewOrderSingle search

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_170.md` |
| Agent / slot | W500 research **170** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (also grepped `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` for a second sender) |
| Assigned file | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` |
| Topic | Search `CTraderFixSession.cs` for `35=D` or `NewOrderSingle`. **Verdict FAIL if live send exists.** |
| Goal context | Fetch ALL Achiever + Starwave groups and ALL manager traders; **copy to cTrader must not send live orders yet (no loss).** |
| Product source modified | **No.** Report + swarm index/log pointers only. |
| Test source modified | **No.** |
| Secrets printed | **None.** `CTRADER_FIX_PASSWORD` / tag 554 / manager passwords / proxy auth **not** dumped. Flag *names*, boolean arm state, public demo host prefix, and public account ids already present as source defaults are not secrets. |
| Method | Full `read_file` of assigned file (**135 / 135**). Full re-read of sibling `CTraderFixDemoTestTrade.cs` (**371 / 371**) after it grew during this slot. Targeted `grep` of that file, all of `src\Fix.CTrader`, product `*.cs` under `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tests` + `D:\Prop\tools`, product `*.cs`/`*.json`/`*.csproj` for `35=D` / `(35, "D")` / `Build("D"`, YoPips C++ `src` for `CTraderFixSession` / `35=D` / `NewOrderSingle` / `FIX.4`. Supporting reads: hosted logon, options, DI L39–42, copy service, copy hosted service, fix-worker, API `/api/settings` + `/api/copy/*` + recon note, `LiveRuntimeStatus`, FSM, RiskEngine `AllowFixSend`, quote service, harness, `NativeMt5BrokerConnector` group/user walks, `DealIngestionService` `GetAccountsAsync(null)`, `LIVE_MANAGER_FETCH_MEASURED.md` + JSON group-row re-sum. **No TLS opened this slot. No Logon sent this slot. No order sent. No Manager re-attach. Demo tool not executed.** |
| Binding law | Architecture §§32–34 / §41 / §68 / §70; A25; A32 (RoE `35=D`); A42 (never retry unknown as `35=D`); A101 item 12; E002 / E034 |
| Siblings (same assigned file; not this measurement) | W500_RESEARCH_10 / 30 / 50 / 70 / 90 / 110 / 130 / 150 |

**Honesty rule:** a comment, log line, `LastError` string, or helper *name* containing `NewOrderSingle` is **not** a FIX `MsgType=D` builder. A TLS **Logon `35=A`** is **not** a NewOrderSingle. `AllowFixSend` / `MayRetryNewOrderSingle` / `RealCopyExecutionEnabled` are **not** socket writers. `35={msgType}` in a reject `LastError` interpolates the **inbound** tag 35. `Build("D", …)` **is** a NewOrderSingle encoder (Assemble writes tag `35` = `D`). Absence of `35=D` in the **assigned** file is **`SAFE_BY_ABSENCE`**, not a unit-tested refuse-on-LoggedOn-TRADE gate. Do **not** tick Architecture §68 / §70 from this file. Do **not** print FIX passwords.

**Stale-sibling correction (binding):**

- W500_RESEARCH_90 / 110 cite `_runtime.RealCopyEnabled = false` in `CTraderFixLogonHostedService` and a DI hard-pin. **Those snippets are not on current disk.** DI binds the env key; hosted service **logs** `RealCopyArmed` and does **not** force false. `.env` has `REAL_COPY_EXECUTION_ENABLED=true`. That **arms a flag**.
- W500_RESEARCH_130 / 150 claim **product `*.cs` `35=D` / `(35, "D")` = 0** and “only those three `TcpClient`/`SslStream`/`WriteAsync` lines in `CTraderFixSession`.” **STALE as of this re-read.** Sibling `CTraderFixDemoTestTrade.cs` (371 lines, same `Sessions\` folder, same product assembly) now encodes `Build("D", …)` **three** times and writes them on a TRADE TLS socket. Literal substring `35=D` is still **0** product-wide (the encoder uses `Build("D")` + `Assemble`). Copy hop is still not that sender.
- W500_130 default-host cite `live-us-eqx-01.p.c-trader.com` is **STALE**. Hosted service default is `demo-us-eqx-01.p.c-trader.com` / account `5328266` / sender `demo.pepperstone.5328266`.

---

## 0. Verdict (binding)

**PASS — live `35=D` / NewOrderSingle send does not exist in `CTraderFixSession.cs`.**

Assigned FAIL condition (“FAIL if live send exists”) is **not met for the assigned type, the copy pipeline, or live Pepperstone account `1369850`.** The assigned type cannot place a cTrader order. Copy-to-cTrader **cannot lose live capital through this file**.

A **sibling** demo-only sender **does** exist. That is a material product delta vs W500_150, **not** a live-account send, and **not** on the copy hop.

| Claim | Result | Class |
|---|---|---|
| Literal `35=D` in `CTraderFixSession.cs` | **0 hits** | **MISSING** builder |
| `NewOrderSingle` in `CTraderFixSession.cs` | **0 hits** | **MISSING** |
| `(35, "D")` / `new(35, "D")` / `MsgType = "D"` / `Build("D"` in assigned file | **0 hits** | **MISSING** |
| `OrderQty` / `ClOrdID` / `OrdType` / `StopPx` / `Side` / tags 11/38/40/54 in assigned file | **0 hits** | no order fields |
| Outbound tag 35 actually built in assigned file | **`"A"` only** (`BuildLogon` L96) | Logon, not order |
| `ssl.WriteAsync` count in assigned file | **1** — bytes of that Logon (L49) | not an order send |
| `TcpClient` / `SslStream` kept for a later `35=D` in assigned file | **No** — `using` / `await using` dispose before return | no TRADE keep-alive |
| `GuardedNewOrderSingle` / `SubmitNewOrder` / `BuildNewOrder` | **0** in assigned file and `Fix.CTrader` | choke **MISSING** |
| QuickFIX/n / `SendToTarget` in product `*.cs` / `*.csproj` | **0 hits** | initiator **MISSING** |
| Product literal `35=D` / `(35, "D")` | **0 hits** under `D:\Prop\src` + `apps` | no `35=D` *string* |
| Product `Build("D"` NewOrderSingle encoder | **3 writes** in `CTraderFixDemoTestTrade.cs` L138 / L145 / L179 | **demo-gated sibling** |
| Product `TcpClient` / `SslStream` / `WriteAsync` | **two** types: assigned Logon + sibling demo trade | W500_150 “only assigned file” **STALE** |
| Sibling caller | `D:\Prop\tools\DemoFixTestTrade\Program.cs` L32 **only** | not DI / not copy / not API |
| Sibling live-account gate | refuses unless host `demo-*`, sender `demo.*`, no `live.` / `live-`, account ≠ `1369850` | **not live** |
| `CopyTradingService.NewOrderSingleImplemented` | **`const false`** (L16) | dead LIVE branch cannot send |
| Product `new ExecutionIntent` / `ExecutionIntents.Add` | **0 hits** | no persist-before-send row |
| YoPips C++ `src` `CTraderFixSession` / `35=D` / `NewOrderSingle` / `FIX.4` | **0 hits** | not a second cTrader sender |
| Prop `src\Mt5` `DealerSend` / `SendTrade` / `OrderSend` | **0 hits** | Manager path is **read** |
| Live `35=D` if API/workers start now | **Impossible from assigned type and copy hop** | assigned **`SAFE_BY_ABSENCE`** |
| Slot FAIL (live send exists)? | **No** (demo sibling ≠ live copy send) | verdict **PASS** |

One-line:

```text
CTraderFixSession.cs (135/135): NewOrderSingle=0; 35=D=0; only outbound MsgType is A (Logon); one WriteAsync; sockets disposed. Sibling CTraderFixDemoTestTrade Build("D") x3 is demo-gated + tools-only (not copy). Live 1369850 refused. SAFE_BY_ABSENCE on assigned+copy. PASS.
```

Do **not** treat env `REAL_COPY_EXECUTION_ENABLED=true` as a send license. Do **not** add a live `35=D` sender in this task. Do **not** invoke `tools/DemoFixTestTrade` against anything except an already-gated demo host.

---

## 1. Assigned-file census (measured this pass)

Path: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`  
Read: **135 / 135** lines. File ends at L135 `}`. Independent re-read for slot **170** (same length as W500_50 / 70 / 90 / 110 / 130 / 150). SHA-256 **not recomputed** this slot (no shell); identity is the full line census.

### 1.1 Tokens the slot named

| Pattern (this file only) | Hits |
|---|---:|
| `35=D` | **0** |
| `NewOrderSingle` | **0** |
| `(35, "D")` / `(35, 'D')` / `new(35, "D")` / `Build("D"` | **0** |
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

`Assemble` prefixes `8=FIX.4.4` + body length + checksum tag 10. It will encode **whatever list it is given**. Today the **only** caller is `BuildLogon` with `(35, "A")`. There is no `BuildNewOrderSingle` on this type.

`Extract` is inbound-only (split on `|` after SOH→pipe replace).

---

## 2. Sole product caller of the assigned type (still not a sender)

`grep` of `CTraderFixSession` / `TryLogonAsync` in product `*.cs` (`D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tests` + `D:\Prop\tools`):

| File | Role |
|---|---|
| `Sessions\CTraderFixSession.cs` | definition |
| `Hosting\CTraderFixLogonHostedService.cs` | **only** caller: two `TryLogonAsync` + persist signature |

`apps\fix-worker\Worker.cs` does **not** reference `CTraderFixSession`. Tests do **not** call `TryLogonAsync`. The demo tool does **not** call `CTraderFixSession` (it has its own Logon `Build("A")`).

Hosted service **as it exists on disk this pass**:

```40:70:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var host = _config["CTRADER_FIX_HOST"] ?? "demo-us-eqx-01.p.c-trader.com";
        var account = _config["CTRADER_FIX_ACCOUNT_ID"] ?? "5328266";
        var sender = _config["CTRADER_FIX_QUOTE_SENDER_COMP_ID"] ?? "demo.pepperstone.5328266";
        var target = _config["CTRADER_FIX_QUOTE_TARGET_COMP_ID"] ?? "cServer";
        // ...
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            // ...
        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            // ...
        _log.LogInformation(
            "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
            quote.LoggedOn, trade.LoggedOn, _runtime.RealCopyEnabled, account);
```

Hard facts from that caller:

- Ports **5211 QUOTE** / **5212 TRADE** (SSL). Plain 5201/5202 are never used here.
- Default TargetCompID **`cServer`**. Default host **`demo-us-eqx-01.p.c-trader.com`** (W500_130 live-host default is **stale**).
- Default account **`5328266`** (demo). Live `1369850` is **not** the hosted default.
- Tag 553 username is the **integer account id**, not SenderCompID (comment L45).
- After both probes: **`_runtime.RealCopyEnabled` is read, not forced false.**
- The word `NewOrderSingle` appears **only** in the log format string (L69: “still unimplemented”). That is not a builder. Relative to the sibling demo sender the log line is **partially stale** (a `35=D` encoder exists, but **this** hosted service does not call it).
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

`CTraderFixLogonHostedService` is added at L58 of the same file. `CopyTradingHostedService` is added at L59 and only calls `GenerateShadowIntentsAsync`. **Neither** hosted service references `CTraderFixDemoTestTrade`.

---

## 3. Sibling `CTraderFixDemoTestTrade` — real `35=D`, not live, not copy

W500_150 “7 product `.cs` under `Fix.CTrader`” / “product `35=D=0`” is **stale**. Current product `.cs` under `D:\Prop\src\Fix.CTrader`:

| File | Role | Live socket? |
|---|---|---|
| `Sessions\CTraderFixSession.cs` | one-shot TLS Logon | **Yes**, `35=A` only |
| `Sessions\CTraderFixDemoTestTrade.cs` | demo TRADE: Logon + SecList + pos req + **up to 3× `35=D`** | **Yes, demo-gated** |
| `Hosting\CTraderFixLogonHostedService.cs` | two `TryLogonAsync` + persist | via assigned type only |
| `Configuration\CTraderFixOptions.cs` | POCO; `RealCopyExecutionEnabled = false` | no |
| `Services\CTraderQuoteService.cs` | in-memory tag lists `y` / `V` | **No** callers write them |
| `Services\FixSessionOwnership.cs` | in-memory fencing lock | no FIX |
| `Parsing\FixMessageParser.cs` | pipe parser/builder for tests | no TCP |
| `Testing\FixSimulationHarness.cs` | inbound stand-ins `A`/`3`/`0`/`y`/`X`/`8` | in-process only |

Sibling measured **371 / 371** (re-read after in-flight growth during this slot; earlier snapshot this same hour was 338 lines / 2× `Build("D")`). Current outbound writes:

| Line | `Build(...)` MsgType | Meaning |
|---:|---|---|
| 75 | `"A"` | TRADE Logon |
| 95 | `"x"` | SecurityListRequest |
| 125 | `"AN"` | RequestForPositions |
| 138 | `"D"` | flatten **existing** gold pos (if `721` present) |
| 145 | `"D"` | market **buy** `38=1` `40=1` `54=1` (new ClOrdID `T…`) |
| 179 | `"D"` | flatten fill (`54=2`, qty from last/cum) |

Gate (L42–46) refuses unless **all** of: host starts with `demo-`; sender starts with `demo.`; sender does **not** contain `live.`; host does **not** contain `live-`; account **≠** `1369850`. Fail-closed return sets `OrderSent=false`.

```42:58:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs
        if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
            || !senderCompId.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
            || senderCompId.Contains("live.", StringComparison.OrdinalIgnoreCase)
            || host.Contains("live-", StringComparison.OrdinalIgnoreCase)
            || account == "1369850")
        {
            return new DemoTestTradeResult
            {
                Allowed = false,
                // ...
                Error = "Refused: test trade is demo-only (host/sender/account gate).",
```

`grep` of `CTraderFixDemoTestTrade` / `SendAsync` in product `*.cs`:

| File | Role |
|---|---|
| `Sessions\CTraderFixDemoTestTrade.cs` | definition |
| `tools\DemoFixTestTrade\Program.cs` L32 | **only** caller |

The tool loads `.env` keys `CTRADER_FIX_HOST` / `CTRADER_FIX_ACCOUNT_ID` / `CTRADER_FIX_TRADE_SENDER_COMP_ID` / target / password and calls `SendAsync(..., 5212, ...)`. It is a **standalone** `net8.0` exe (`DemoFixTestTrade.csproj` ProjectReference to `Fix.CTrader` only). It is **not** registered in DI, **not** a hosted service, **not** an API route, **not** called from `CopyTradingService` / `CopyTradingHostedService` / `CTraderFixLogonHostedService` / `fix-worker`.

This slot **did not run** that exe. `DEMO_FIX_TEST_TRADE.json` is **absent**.

Why this is **not** this slot’s FAIL:

- FAIL condition is **live** send. Live Pepperstone account `1369850` / `live-*` host / `live.*` sender are refused.
- Copy pipeline never calls it. Fetch-all Manager walk never calls it.
- It can still move **demo** buying power on `5328266` **if an operator runs the tool**. That is demo-capital residual, not live-copy.

`TraderIntelligence.Fix.CTrader.csproj`: net8.0; project refs Domain + Application; packages Hosting/Configuration/Logging abstractions + EFCore. **No** `QuickFIXn.Core`, **no** `QuickFIXn.FIX44`.

---

## 4. Product-wide `NewOrderSingle` / `35=D` (name ≠ send; `Build("D")` = send)

`grep` literal `35=D` / `(35, "D")` / `new(35, "D")` / `MsgType = "D"` on `*.cs` under `D:\Prop\src` + `apps`: **0 hits**.

`grep` `Build("D"` on those trees: **3 hits**, all `CTraderFixDemoTestTrade.cs`.

Product `*.cs` tokens named `NewOrderSingle` (none of these encode tag 35=`D`):

| Location | What it is | Sends `35=D`? |
|---|---|---|
| `src\Fix.CTrader\Configuration\CTraderFixOptions.cs:33–35` | XML comment + `RealCopyExecutionEnabled` default **`false`** | **No** |
| `src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs:69` | log “still unimplemented” | **No** |
| `src\Application\Runtime\LiveRuntimeStatus.cs:42–44` | `copyNote`: armed text still says “NewOrderSingle still unimplemented” | **No** (wording stale vs sibling) |
| `src\Application\Copy\CopyTradingModels.cs:9` | DTO field `NewOrderSingleImplemented` | **No** |
| `src\Infrastructure\Copy\CopyTradingService.cs:16,49,59,198,243–244` | `const false`; blockers include “SAFE_BY_ABSENCE”; LIVE branch only sets status string | **No** |
| `src\Infrastructure\Hosting\CopyTradingHostedService.cs:30` | log “Live NewOrderSingle still blocked” | **No** |
| `src\Infrastructure\Seeding\DemoSeeder.cs:101` | TRADE `LastError` string | **No** |
| `src\Infrastructure\Seeding\BrokerCatalogSeed.cs:105` | TRADE `LastError` “NewOrderSingle off” | **No** |
| `apps\fix-worker\Worker.cs:22,41,46` | startup log / LastError / warning | **No** |
| `apps\api\Program.cs:69` | `/api/reconciliation/status` note | **No** |
| `src\Domain\Execution\ExecutionOrderStateMachine.cs:35–36` | `MayRetryNewOrderSingle` status math (`NotSent`/`Rejected` only) | **No** |

`apps\fix-worker\Worker.cs` reads `CTrader:RealCopyExecutionEnabled` with fallback **false**. If the key is true it **only logs a warning** and still writes `LastError = "No live TRADE socket. NewOrderSingle remains off."` It never calls `CTraderFixSession` or `CTraderFixDemoTestTrade`.

API settings expose the **runtime** flag (bound from env):

```71:78:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", (LiveRuntimeStatus runtime) => Results.Ok(new
{
    // ...
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = true
```

Copy HTTP surface is **GET only**: `/api/copy/status`, `/api/copy/intents`. No POST that places an order.

`RiskEngine` can set `AllowFixSend` on a DTO when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. **CopyTradingService overwrites the persisted record:** `AllowFixSend = false` (L192) regardless of `decision.AllowFixSend`. The only LIVE-looking branch (L198) is conjunct with `NewOrderSingleImplemented` (**const false**) and `VenueReconciled` (**const false**) and only mutates `intent.Status` to `LIVE_SEND_BLOCKED_UNIMPLEMENTED`. **No** product caller maps that boolean onto `ssl.WriteAsync` or `Build("D")`.

`BaselineScorer.CanPromoteToLive => false` (`src\Domain\Scoring\BaselineScorer.cs` L211). Trade #3 cannot auto-LIVE.

`EfTradingStore.PersistDemoShadowAsync` stamps `Status = "SHADOW_ONLY"` (L307). **0** `ExecutionIntent` writers.

---

## 5. Flag arm vs send (honest residual, not FAIL)

| Source | Measured this slot |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** (unbound POCO) |
| DI `LiveRuntimeStatus.RealCopyEnabled` | **bound** from `REAL_COPY_EXECUTION_ENABLED` == `"true"` |
| `CTraderFixLogonHostedService` | **does not** force the runtime flag false |
| `.env` key `REAL_COPY_EXECUTION_ENABLED` | **`true`** (flag only; no secret dumped) |
| `.env` `CTRADER_FIX_HOST` | `demo-us-eqx-01.p.c-trader.com` (name+prefix only) |
| `.env` `CTRADER_FIX_ACCOUNT_ID` | `5328266` (public demo id; already a source default) |
| `CopyTradingService.NewOrderSingleImplemented` | **`const false`** |
| `CopyTradingService.VenueReconciled` | **`const false`** |
| Persisted `RiskDecisionRecord.AllowFixSend` | **hardcoded false** at write |
| Assigned-file `35=D` builder | **0** |
| Sibling demo `Build("D")` | **3** writes, tools-only |

W500_68 / 108 “flag pinned false (POCO/DI/hosted/.env/settings)” is **stale on DI + hosted + `.env`**. POCO default is still false. Settings API now **mirrors runtime**, so a process that loaded `.env` would advertise `featureFlags.REAL_COPY_EXECUTION_ENABLED=true` **and still cannot send via copy**. That is an honesty/UI residual, not a live send.

Architecture §41: session-on + flag-true is **necessary and not sufficient**. Current disk: flag **may be true**; assigned sender **absent**; copy sender **absent**; demo tool sender **present but gated**. Live capital is safe **by absence on the copy hop**, not by a tested refuse-on-LoggedOn-TRADE gate (A101 item 12 still unproven).

---

## 6. YoPips C++ backend (relevant only as contrast)

Tree searched: `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src`.

| Pattern | Hits |
|---|---|
| `CTraderFixSession` | **0** |
| `NewOrderSingle` | **0** |
| `35=D` | **0** |
| `FIX.4` / `CTraderFix` | **0** |

That tree **does** have MT5 dealer APIs (`DealerSend` / `DealerSendOrder` in `mt5_manager.cpp` / pool / HTTP client). That is the **prop-firm MT5 dealer** path for YoPips challenge accounts, **not** cTrader FIX, and it is **not** called from `CTraderFixSession.cs`. Slot 170 does **not** treat YoPips `DealerSend` as a live cTrader `35=D`.

Prop `src\Mt5` `DealerSend` / `SendTrade` / `OrderSend`: **0 hits**. `NativeMt5BrokerConnector` is Manager **read**.

---

## 7. Goal coupling: all-groups/all-traders vs no-loss copy

Fetch path is **Manager read**, not the assigned FIX type.

`NativeMt5BrokerConnector.GetGroupsCore` walks `GroupRequestArray("*")` then falls back to `GroupTotal`+`GroupNext` (`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` L155 / L174). `GetAccountsCore(null)` iterates **every** returned group and `UserRequestArray` / `UserGetByGroup` / `UserLogins` (`L201–232`). `DealIngestionService` calls `GetAccountsAsync(null)` (`DealIngestionService.cs` L48 / L62). `LiveBrokerProbe` same. Dashboard traders list is account-driven (W500_156).

Live Manager census already measured (this slot **re-summed** the JSON group rows; it did **not** re-attach):

| Source | Evidence |
|---|---|
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | Achiever **8 / 6512** (HTTP proxy) + Starwave **10 / 1948** (direct) = **18 / 8460** |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | `utc=2026-08-18T08:42:16Z`; ACHIEVER `groups=8` `accounts=6512` `openPositions=1506`; STARWAVEFX `groups=10` `accounts=1948` `openPositions=478` |
| Same JSON group-row sum | Achiever `2+179+4+5+4+6295+0+23 = 6512`; Starwave `11+4+170+1735+22+0+0+4+0+2 = 1948` |
| Positions | `1506+478 = 1984` |

Achiever groups (manager-visible): `contest\yo-1step`, `contest\yo-2step`, `contest\yo-instant`, `contest\yo-payp`, `demo\yo-1step`, `demo\yo-2step`, `demo\yo-instant`, `demo\yo-payp`.

Starwave groups (manager-visible): `Starwave\cent\FX1\grp1`, `grp2`; `Starwave\demo\FX2\grp1`, `grp2`; `Starwave\real\FX3\grp1`–`grp5`, `Starwave\real\FX3\LP`.

This slot **re-confirms** the copy half of the goal on current disk:

- Fetch path is **Manager read** — not this FIX type. Catalog walk is `*` + all users.
- Copy destination **must stay off** until §68 19/19 + §70 14/14 + persist-before-send + a reviewed live sender + explicit go-live. Those gates are **not** re-scored here (siblings A100 / A101 still historically **0 PASS**).
- Current safety for **live** capital: **`SAFE_BY_ABSENCE`** of `35=D` in `CTraderFixSession` **plus** copy hop `NewOrderSingleImplemented=false` **plus** sibling gate refusing `1369850`.
- Copy writers: `CopyTradingService.GenerateShadowIntentsAsync` → `SHADOW_ONLY` + in-memory `ShadowCopyEngine.SimulateEntry`; `EfTradingStore` persist also stamps `SHADOW_ONLY`. **0** `ExecutionIntent` writers.

Logon `35=A` **can** still leave this process toward `*.c-trader.com` when the password slot is populated (QUOTE 5211 / TRADE 5212). That is **session proof**, not an order. Assigned sockets are dead after the one reply.

---

## 8. What would have been FAIL

The slot brief: **FAIL if live send exists.** Any of the following on current disk would have failed the slot:

1. `CTraderFixSession.BuildLogon` (or a new method on that type) emitting `(35, "D")`.
2. A second `WriteAsync` of a constructed NewOrderSingle after Logon **in the assigned file**.
3. A kept TRADE `SslStream` / QuickFIX initiator that `SendToTarget`s `MsgType=D` toward **live** `1369850` / `live-*`.
4. Copy hop calling `CTraderFixDemoTestTrade.SendAsync` (or any `Build("D")`) when ingesting Achiever/Starwave.
5. Sibling gate missing / allowing account `1369850` or `live-` host.

**None** of those exist. An armed `REAL_COPY_EXECUTION_ENABLED` flag **without** a copy builder is **not** a live send. A **demo-gated** `Build("D")` behind a **manual tool** is **not** a live copy send.

---

## 9. Residual (honest, not FAIL)

| Residual | Why it is not this slot’s FAIL |
|---|---|
| `35=A` can go to the configured cTrader host if password is real | Not NewOrderSingle; no capital move |
| Cert callback `(_, _, _, _) => true` | TLS identity not pinned; assigned type still Logon-only |
| Env `REAL_COPY_EXECUTION_ENABLED=true`; DI binds it; hosted service no longer re-pins false | Flag arm ≠ copy encoder. `NewOrderSingleImplemented` still const false |
| `/api/settings` now mirrors runtime (can advertise true) | UI honesty residual; no socket write |
| `CTraderFixDemoTestTrade` **does** encode/write `35=D` (up to 3×) | Demo host/sender/account gate; tools-only; live `1369850` refused |
| Operator can run `tools/DemoFixTestTrade` against current demo `.env` | **Demo** buying power can move; **not** live copy; this slot did not run it |
| `LiveRuntimeStatus.copyNote` / hosted log “unimplemented” | Wording stale vs sibling encoder; still true for **copy** |
| `SAFE_BY_ABSENCE` ≠ unit-tested refuse-on-LoggedOn-TRADE | A101 item 12 stays unproven |
| No persist-before-send / `GuardedNewOrderSingle` on copy hop | Copy cannot send, so cannot violate it **yet** |
| Three hosts can each register `CTraderFixLogonHostedService` | Duplicate Logon risk, not duplicate live `35=D` |
| `MayRetryNewOrderSingle` exists as status math | Never opens a socket |
| `AllowFixSend` can be true on a risk DTO | Copy path overwrites persisted bool to false; no copy encoder consumes it |
| Quote-service `y`/`V` lists exist | Never written to a socket |
| YoPips `DealerSend` exists in a **different** tree | MT5 dealer, not cTrader FIX; not called from this file |
| SHA-256 of assigned file not recomputed | Identity is the 135-line full read |
| W500_90 / 110 “flag forced false after logon” | **Stale** |
| W500_130 / 150 “product `35=D=0` / only one FIX writer” | **Stale** |

---

## 10. Do / Do not

**Do**

- Keep Manager fetch of **all** Achiever + Starwave groups / logins as a **read** path.
- Treat `CTraderFixSession.cs` as Logon/`35=A` only until a separately reviewed **live** sender exists **and** §68+§70 PASS.
- Treat env `REAL_COPY_EXECUTION_ENABLED=true` as an **operator wish**, not a send license.
- Treat `CTraderFixDemoTestTrade` as a **manual demo probe**, never as copy execution.
- Prefer re-pinning `LiveRuntimeStatus.RealCopyEnabled=false` until a reviewed live sender exists (honesty), but that pin is **not** what currently protects live capital.

**Do not**

- Add live `35=D` / `F` / `G` in this task.
- Treat QUOTE/TRADE `LoggedOn=true` as license to copy.
- Print `CTRADER_FIX_PASSWORD` or tag 554.
- Point `tools/DemoFixTestTrade` at live `1369850` (gate should refuse; do not test that against the venue).
- Claim “no-loss live copy” is implemented. **No-loss live copy is impossible today** because there is no gated live send path. The operating mode is **fetch + logon/recon + SHADOW intents only** (+ optional manual demo probe).
- Confuse YoPips `DealerSend` with a cTrader NewOrderSingle.
- Cite W500_130 / 150 product-wide `35=D=0` as current truth.

---

## 11. Slot close

| Item | Value |
|---|---|
| Slot | **170** |
| Verdict | **PASS** |
| Live `35=D` / NewOrderSingle send exists (assigned file / copy / live 1369850)? | **No** |
| Demo `35=D` encoder exists? | **Yes** — `CTraderFixDemoTestTrade` `Build("D")` ×3, tools-only, demo-gated |
| Risk to capital from assigned file | **None** (`SAFE_BY_ABSENCE`) |
| Risk to live Pepperstone `1369850` from copy hop | **None** (`NewOrderSingleImplemented=false` + no copy caller of sibling) |
| Evidence | Full 135-line read of `CTraderFixSession.cs`; 0 hits for `35=D` and `NewOrderSingle`; sole outbound MsgType `(35, "A")`; one `WriteAsync`; sockets disposed. Sibling 371-line demo sender has 3× `Build("D")` but refuses live host/sender/account. Copy const `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`; 0 `ExecutionIntent` writers. YoPips C++ `src` has 0 cTrader FIX senders. |
| Census cited (JSON re-summed, not re-attached) | Achiever 8/6512 + Starwave 10/1948 = **18 / 8460** (positions 1506+478=1984) |
| Residual (not FAIL) | `.env` flag **true**; DI binds it; hosted service no longer forces false; settings API can advertise true; sibling demo `35=D` exists |
| Product edited | **No** |
