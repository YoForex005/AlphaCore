# A003 — Copy to cTrader **and** no loss: honest FIX gate

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\A003_fix_noloss.md` |
| Agent | A003 (FIX no-loss / copy gate) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned | Read all files under `D:\Prop\src\Fix.CTrader`. Search `35=D`, `NewOrderSingle`, `OrderQty`, `REAL_COPY_EXECUTION`. Confirm SenderSubID `QUOTE`/`TRADE`, TargetCompID `cServer`, ports `5211`/`5212`, SSL. Confirm live send is impossible in current code. User wants copy to cTrader **and** no loss. State the honest gate. Never print the FIX password. |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Binding law | Architecture §§1.7–1.10, 25–34, 38, 41, 68, 70; A23 / A25 / A42 / A43 / A47 / A100 / A101 |
| Siblings | E002 (flag default + no sender), E034 (`35=D` census; **stale on transport**), C43 (Logon not proven), C14 / A100 (0/19), A101 (0/14) |
| Secret handling | `CTRADER_FIX_PASSWORD` is **named only**. Value **not** read from `.env` and **not** printed. |

**Honesty rule:** wanting both live copy **and** no loss does not make either true. A logon writer is **not** a NewOrderSingle. A comment that names `NewOrderSingle` is **not** a builder. `AllowFixSend` on a risk DTO is **not** a socket write. `SAFE_BY_ABSENCE` is the current safety outcome, not a unit-tested refuse-on-LoggedOn-TRADE gate. Do not tick Architecture §68 / §70 from this file.

---

## 0. Verdict (binding)

**Live copy (`35=D`) is impossible in current code. No-loss live copy is also impossible, because risk + recon + quantity conversion are not a wired send path.**

The user goal is **both**:

1. Copy MT5 XAUUSD into **cTrader** (Pepperstone / `cServer`).
2. **No loss** — no blind lots, no unknown-state retry, no send before recon, no send with a stale quote.

Those two cannot be delivered together **today**. Copy requires a NewOrderSingle. No-loss requires gates that are **not PASS**. Therefore the only honest operating mode is:

```text
HONEST GATE (until risk + recon PASS):
  ALLOW:  TLS logon (35=A) + heartbeat/logout diagnostics
          + TRADE read / recon (35=H / 35=AF / 35=AN) when built
  FORBID: NewOrderSingle (35=D), cancel/replace (35=F/G),
          REAL_COPY_EXECUTION_ENABLED=true,
          any live OrderQty on the wire
```

One-line:

```text
logon+recon only until risk/recon pass; live 35=D is OFF and currently unbuildable.
```

| Claim | Result | Class |
|---|---|---|
| User wants copy **and** no loss | **Yes** (product goal) | intent, not a license |
| Can the process send live `35=D` now? | **No** | **`SAFE_BY_ABSENCE`** |
| `35=D` / `(35, "D")` / `MsgType="D"` in `Fix.CTrader` | **0 hits** | **MISSING** builder |
| `OrderQty` / tag 38 in `Fix.CTrader` | **0 hits** | **MISSING** |
| `NewOrderSingle` in `Fix.CTrader` | **name / log only** | not a sender |
| `RealCopyExecutionEnabled` default | **`false`** | fail-closed default |
| SenderSubID `QUOTE` / `TRADE` | **Confirmed** (hosted service + seed) | header defaults |
| TargetCompID `cServer` | **Confirmed** | header defaults |
| Ports `5211` / `5212` + SSL | **Confirmed** | QUOTE/TRADE SSL |
| Risk + recon wired to a send choke | **No** | **`GATE_INCOMPLETE`** |
| Safe to enable `REAL_COPY_EXECUTION` | **No** | A100 **0/19**, A101 **0/14** |

Do **not** enable the flag. Do **not** add a `35=D` sender in this task.

---

## 1. Files read (`D:\Prop\src\Fix.CTrader`)

All product `.cs` under the project (bin/obj ignored):

| Path | Role |
|---|---|
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | Host, SSL ports, Comp/Sub IDs, `RealCopyExecutionEnabled=false` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Optional TLS Logon (`35=A`) only; logs “NewOrderSingle still disabled” |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | Raw `TcpClient` + `SslStream`; **only** outbound MsgType is `A` |
| `D:\Prop\src\Fix.CTrader\Parsing\FixMessageParser.cs` | Pipe/SOH test codec; generic builder; no `D` caller |
| `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs` | In-memory SecurityList / MD tag lists (`y`, `V`); no socket |
| `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs` | In-memory fence; `ExecutionIntentsAllowed` unused by worker |
| `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs` | In-process `|` strings (`A`/`3`/`8`/`y`/`X`/`0`); no venue |
| `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` | net8.0; Domain + Application only; **no** QuickFIX/n |

Adjacent product (needed to judge “live send”):

| Path | Role |
|---|---|
| `D:\Prop\apps\fix-worker\Worker.cs` | 15 s loop stamps QUOTE+TRADE `Disconnected`; no send |
| `D:\Prop\apps\fix-worker\Program.cs` | `AddTraderIntelligence` + `AddHostedService<Worker>` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Registers `CTraderFixLogonHostedService` **without** a Fix.CTrader project reference (see §6) |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | TRADE `LastError` = “logon/recon only; NewOrderSingle off” |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | TRADE `LastError` = “NewOrderSingle off.” |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | `AllowFixSend` math; **no** socket |
| `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs` | `MayRetryNewOrderSingle` is status math only |

---

## 2. Assigned greps (measured)

### 2.1 Inside `Fix.CTrader` (this task’s tree)

| Pattern | Hits | Meaning |
|---|---:|---|
| `35=D` | **0** | no NewOrderSingle wire text |
| `(35, "D")` / `new(35, "D")` / `MsgType = "D"` | **0** | no builder |
| `OrderQty` / `38=` | **0** | no destination quantity on FIX |
| `NewOrderSingle` | **2** | XML comment (`CTraderFixOptions` L33); log format (`CTraderFixLogonHostedService` L53) |
| `REAL_COPY_EXECUTION` (exact env token) | **0** in this project | C# name is `RealCopyExecutionEnabled` |
| `RealCopyExecutionEnabled` | **1** | POCO default **`false`** |

`FixMessageParser` `ToString("D3")` is a **3-digit checksum format**, not tag 35.

### 2.2 Tag 35 values that **do** exist in `Fix.CTrader`

| Site | Tag 35 | Written to cServer? |
|---|---|---|
| `CTraderFixSession.BuildLogon` | **`A`** | **Only if** `TryLogonAsync` is actually invoked with a real password |
| `CTraderQuoteService.BuildSecurityListRequestTags` | `y` | **No** (tag list only; also the **response** type, not request `x`) |
| `CTraderQuoteService.BuildMarketDataRequestTags` | `V` | **No** (tag list only) |
| Harness logon / reject / ER / MD / HB | `A` / `3` / `8` / `y` / `X` / `0` | **No** (in-process strings) |

**Missing on purpose until the honest gate lifts:** `D` (NewOrderSingle), `F` (cancel), `G` (replace), `H` (status), `AF` (mass status), `AN` (positions). Recon **cannot** run on the wire today.

### 2.3 Broader product (so “impossible” is not a local lie)

No `Fix.CTrader` caller, worker, or API endpoint constructs OrderQty / `35=D`. Worker `NewOrderSingle` hits are log + `LastError` English. `MayRetryNewOrderSingle` never opens a socket. Quantity tests that would map MT5 lots → cTrader `OrderQty` are **skipped** (`SourceDestinationQuantityConversionTests`).

---

## 3. Confirmed session identity (QUOTE / TRADE / cServer / SSL)

### 3.1 Ports and SSL

| Session | SSL port (used) | Plain port (options only) | `UseSsl` default |
|---|---:|---:|---|
| QUOTE | **5211** | 5201 | **`true`** |
| TRADE | **5212** | 5202 | **`true`** |

Evidence:

```41:51:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    public sealed class QuoteFixOptions
    {
        public int SslPort { get; set; } = 5211;
        public int PlainPort { get; set; } = 5201;
        // ...
    }
    public sealed class TradeFixOptions
    {
        public int SslPort { get; set; } = 5212;
        public int PlainPort { get; set; } = 5202;
```

```26:26:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    public bool UseSsl { get; set; } = true;
```

`CTraderFixSession.TryLogonAsync` **always** wraps the TCP stream in `SslStream` (TLS 1.2 | 1.3). The hosted service **hardcodes** 5211 / 5212 and never uses the plain 5201 / 5202 ports.

Cert callback is `(_, _, _, _) => true` (accept-any). That is a transport hole for a future diagnostic soak, **not** a send license.

### 3.2 CompIDs / SubIDs

| Header | QUOTE default (hosted service) | TRADE default (hosted service) |
|---|---|---|
| SenderCompID (49) | `live.pepperstone.1369850` (env override) | same sender string |
| TargetCompID (56) | **`cServer`** | **`cServer`** |
| SenderSubID (50) | **`QUOTE`** | **`TRADE`** |
| TargetSubID (57) | **`QUOTE`** | **`TRADE`** |

```41:50:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            _config["CTRADER_FIX_QUOTE_SENDER_SUB_ID"] ?? "QUOTE",
            _config["CTRADER_FIX_QUOTE_TARGET_SUB_ID"] ?? "QUOTE",
            sender, password, stoppingToken);

        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            _config["CTRADER_FIX_TRADE_SENDER_SUB_ID"] ?? "TRADE",
            _config["CTRADER_FIX_TRADE_TARGET_SUB_ID"] ?? "TRADE",
```

`BrokerCatalogSeed` matches: QUOTE 5211 / `cServer` / SenderSubID `QUOTE`; TRADE 5212 / `cServer` / SenderSubID `TRADE`.

**POCO gap (do not paper over):** `CTraderFixOptions.Quote.SenderSubId` and `Trade.SenderSubId` default to **`string.Empty`**. The hosted service does **not** bind `IOptions<CTraderFixOptions>`; it reads env keys and applies `QUOTE`/`TRADE` itself. Seed `DemoSeeder` leaves TRADE `SenderSubId` unset. Header mapping is therefore **inconsistent across surfaces**, but the **intended** RoE pair is still SenderSubID `QUOTE`/`TRADE` and TargetCompID `cServer`.

### 3.3 Password (named, never printed)

- Options comment: password **must never be logged**.
- Hosted service reads `CTRADER_FIX_PASSWORD`. If missing or containing `<SECRET>`, it logs “password missing” and **returns** (no connect).
- Logon line logs **account id** and boolean logon results only — not the secret.
- `BuildLogon` puts the secret in tag **554** on the TLS write. That is Logon, not this report.
- **This file does not open `.env` and does not quote any password value.**

---

## 4. Live send is impossible (current code)

“Live send” here means: a process in this tree emits FIX **`35=D`** (or `F`/`G`) to `*.c-trader.com`.

| Required piece | Measured |
|---|---|
| NewOrderSingle builder | **MISSING** |
| OrderQty / tag 38 / Side 54 / OrdType 40 construction | **MISSING** in `Fix.CTrader` |
| Persist-before-send (`GuardedNewOrderSingle`) | **MISSING** |
| QuickFIX/n initiator | **MISSING** (`Fix.CTrader.csproj` has **no** `PackageReference`) |
| TRADE application loop after Logon | **MISSING** — `TryLogonAsync` writes `35=A`, reads **one** reply, disposes the socket |
| `fix-worker` send path | **MISSING** — stamps `Disconnected` every 15 s |
| Flag as a coded choke in front of a sender | **`GATE_INCOMPLETE`** — there is no sender to choke |

What **can** go on the wire, if the hosted service actually runs and a real password is present:

- Client Logon **`35=A`** on TLS 5211 and 5212.
- Nothing else. No heartbeat loop. No SecurityList. No MD. No OrderStatus. No NewOrderSingle.

That is **diagnostic logon at most**, then the `using` TcpClient/SslStream **tears the session down**.

`fix-worker` still says:

```40:46:D:\Prop\apps\fix-worker\Worker.cs
                trade.Status = FixSessionStatus.Disconnected;
                trade.LastError = "No live TRADE socket. NewOrderSingle remains off.";
            // ...
            if (real)
                _logger.LogWarning("Real copy is enabled in config, but worker still refuses NewOrderSingle until risk/reconciliation gates pass.");
```

The “refuse” is a **log line**. If `CTrader:RealCopyExecutionEnabled=true`, the worker still has **no function** that can emit `35=D`. Flipping the flag **cannot** place an order.

`CTraderFixOptions.RealCopyExecutionEnabled` default:

```32:35:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;
```

Architecture env name `REAL_COPY_EXECUTION_ENABLED` is **not** bound onto that POCO by ASP.NET (would need `CTrader__RealCopyExecutionEnabled`). Unbound env `true` would still not create a builder.

---

## 5. Why “copy **and** no loss” cannot ship now

Copy-to-cTrader, done honestly, is:

```text
source deal → reconstruct → size (lots ↛ OrderQty) → risk.Evaluate
  → persist ClOrdID → 35=D on TRADE → ER apply → recon
```

No-loss requires **every** box. Current box states:

| Box | Needed for no-loss copy | Now |
|---|---|---|
| Symbol / instrument id | XAUUSD → numeric tag 55 | Quote service maps in memory only; never sent |
| Quantity | Contract-size conversion; never passthrough 0.10 lots as 0.10 OrderQty | **No `OrderQty` in Fix.CTrader**; G7 tests **skipped** |
| Risk | `AllowFixSend` only if flag + recon + venue + kill-switch | Engine exists; **zero FIX callers** |
| Recon | `35=H` / `AF` / `AN` before any increasing send | **No builders**; `MarkReconciled` unused by worker |
| Unknown-state | Never second `35=D` if `RequiresReconciliation` | FSM helper only; no send to retry |
| Ownership | Single TRADE owner + fencing token | In-memory lock; unused |
| Feature flag | `REAL_COPY_EXECUTION_ENABLED=false` until §68 + §70 | Default false; **not** a wired choke |
| Persist-before-send | Unique ClOrdID row then send | **MISSING** |
| Go-live license | A100 19/19 + A101 14/14 | **0/19** and **0/14** (C14 still FAIL) |

Risk already **rejects** increasing exposure when `Reconciled == false` (`VENUE_NOT_RECONCILED`). That is the correct **domain** rule. It does not make copy safe, because nothing sends, and nothing marks the venue reconciled from cServer.

So:

- **Copy without the gate** = future loss (blind size, double send, send into an unreconciled book).
- **Gate without a sender** = no copy, no venue loss from this process.
- **Honest product stance:** keep the second; do not fake the first.

---

## 6. Hosted logon vs worker (do not confuse with copy)

`AddTraderIntelligence` contains:

```47:47:D:\Prop\src\Infrastructure\DependencyInjection.cs
        services.AddHostedService<CTraderFixLogonHostedService>();
```

`TraderIntelligence.Infrastructure.csproj` does **not** reference `Fix.CTrader`, and `DependencyInjection.cs` has **no** `using TraderIntelligence.Fix.CTrader.Hosting`. There is **no** second `CTraderFixLogonHostedService` under `Infrastructure/Hosting`. That registration is **not a buildable wire** as measured (missing project reference + using). Even if a later compile fix lands:

1. Logon is still **`35=A` only**, then disconnect.
2. `fix-worker` `Worker` will **overwrite** persisted status to `Disconnected` every 15 s.
3. Still **no** `35=D`.

C43 (“no `TcpClient` / Logon not proven”) is **stale on source**: `CTraderFixSession` now has TLS Logon. C43 remains correct on **proof**: a one-shot Logon without a persisted `LOGON_OK` record, heartbeat soak, and independent sequence stores is **not** A101 item 1. This file does **not** tick Logon-stable.

E034’s “`TcpClient`/`SslStream` = 0” is **stale**. The new session class exists. Its only outbound MsgType is still **`A`**.

---

## 7. The honest gate (operator-facing)

Until **risk + recon** are measured PASS (not a dashboard enum):

| Action | Allowed? |
|---|---|
| Set `REAL_COPY_EXECUTION_ENABLED=true` | **No** |
| Send `35=D` / `F` / `G` | **No** |
| Treat seed/worker `LoggedOn` as venue health | **No** (anti-evidence) |
| TLS Logon `35=A` on 5211/5212 with SenderSubID QUOTE/TRADE, TargetCompID `cServer` | **Diagnostic only** (when wired and password present) |
| After Logon-stable: TRADE **read** (`H`/`AF`/`AN`) and persist recon | **Next increment** — still no copy |
| Shadow / in-process fill simulation | **Yes** (no venue) |
| Live copy of a source deal | **No** |

**Lift condition (all required):**

1. A100 §68 = 19/19 PASS with on-disk evidence.
2. A101 §70 = 14/14 PASS, including coded refuse when the flag is false.
3. Quantity converter: MT5 lots → ounces/units → **OrderQty** (never passthrough).
4. Persist unique ClOrdID **before** any future `35=D`; `MayRetryNewOrderSingle` false on unknown.
5. Venue `Reconciled=true` from real AF/AN, not `MarkReconciled()` in a unit test.
6. Explicit production review. Flag stays **false** in every committed config until then.

**Copy to cTrader is the destination. No-loss is the constraint. The constraint wins.** Current code already cannot violate it on the wire, because there is no NewOrderSingle. That is **`SAFE_BY_ABSENCE`**, not a finished product.

---

## 8. Classification

| Slice | Class |
|---|---|
| SenderSubID QUOTE / TRADE | **EXISTS** (hosted defaults + catalog seed) |
| TargetCompID `cServer` | **EXISTS** |
| SSL 5211 / 5212 | **EXISTS** (options + hosted hardcode + session `SslStream`) |
| `RealCopyExecutionEnabled` default false | **EXISTS_AND_GOOD** |
| `35=D` / OrderQty / live copy path | **MISSING** |
| Coded risk/recon send gate | **GATE_INCOMPLETE** |
| Live `35=D` if process starts now | **`SAFE_BY_ABSENCE`** |
| User goal “copy **and** no loss” | **NOT DELIVERABLE** until the honest gate lifts |
| Allowed mode now | **logon + recon only** (recon builders still MISSING) |
| FIX password printed | **No** |
| Product source edited | **No** |

---

## 9. Assigned answers (do not paraphrase away)

1. **`35=D` / NewOrderSingle / OrderQty / `REAL_COPY_EXECUTION` in `Fix.CTrader`?**  
   No `35=D`. No OrderQty. `NewOrderSingle` is a comment and a log string. The C# flag is `RealCopyExecutionEnabled = false` (not the env token). None of these emit an order.

2. **SenderSubID QUOTE/TRADE, TargetCompID `cServer`, ports 5211/5212, SSL?**  
   **Yes.** Hosted service and options/seed agree on those four facts. Options `SenderSubId` default empty is a bind gap, not a different intended pair.

3. **Is live send impossible in current code?**  
   **Yes — live NewOrderSingle is impossible.** The only possible venue write is a one-shot TLS Logon `35=A` if a password is present and the hosted service is actually compiled/started. That is not copy.

4. **User wants copy to cTrader and no loss. What is the honest gate?**  
   **Logon + recon only until risk/recon pass.** Do not enable `REAL_COPY_EXECUTION`. Do not add `35=D` until A100 + A101 + quantity + persist-before-send are measured PASS. No-loss forbids shipping copy first.

**Do not print the FIX password. Do not enable the flag. Do not add a sender in this task.**
