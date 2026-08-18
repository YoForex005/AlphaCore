# W500_RESEARCH_7 — cTrader is destination venue (not LP); TargetCompID `cServer`; SSL 5211/5212; no live send

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_7.md` |
| Slot | **7** |
| Date | 2026-08-18 |
| Agent | W500 research slot 7 (senior engineer, read-only) |
| Assigned | Confirm **cTrader is destination venue, not LP**. TargetCompID **`cServer`** (case preserved). Ports **5211 QUOTE** and **5212 TRADE SSL**. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader **must not send live orders yet** (no loss). |
| Product source modified | **No.** This report is the only write. |
| Secret values printed | **None.** `CTRADER_FIX_PASSWORD` / MT5 / proxy passwords named only. |
| Trees | `D:\Prop` (src / apps / docs / architecture / tests / reports). `D:\Projects\YoPips\Backend\C++ Backend PropFirm` (destination-negative). Official cTrader Help re-fetched 2026-08-18. |
| Binding law | Architecture v2 §1.6 item 6, §7–§9, §25–§27, §41 / §56; A87 / D58 (not an LP); R030 (headers/ports); E002 / E034 (no `35=D`). |

**Honesty rule:** a hostname + port + CompID string is **not** a proven live fill. A `35=A` Logon is **not** a NewOrderSingle. Absence of `35=D` is **`SAFE_BY_ABSENCE`**, not a unit-tested refuse-on-LoggedOn-TRADE gate. Official RoE table text `CSERVER` and the issued form `cServer` are **different strings**; §26 forbids a silent fold.

---

## 0. Verdict (binding)

**CONFIRMED on the four assigned claims.**

| Claim | Result | Class |
|---|---|---|
| cTrader / Pepperstone / cServer is the **destination execution venue**, not an LP | **Yes** | Architecture §1.6 + §25 title. Product C#/TS **0** `LP` / `LiquidityProvider` identifiers. Official Help lists **trade copiers** as a client use of FIX, and **liquidity providers** as a *different* industry role. |
| Tag 56 `TargetCompID` stays issued-form **`cServer`** (case preserved) | **Yes on the live/default path** | Options, seed, harness, hosted default, integration assert. `src` has **0** `CSERVER`. No `ToUpper` on CompID. Residual: unused `apps/api/appsettings.json` `CTraderFix.TargetCompId=CSERVER`. |
| Production ports are **QUOTE SSL 5211** and **TRADE SSL 5212** | **Yes** | Official credentials screenshot + Spotware sample + architecture §25 + `CTraderFixOptions` + hosted `TryLogonAsync` hardcode. Session always wraps `SslStream` (TLS 1.2 \| 1.3). Plain 5201/5202 exist as non-production defaults only. |
| Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders | **Code path = all visible.** **Census previously measured.** | `GroupRequestArray("*")` + `UserRequestArray` per group; `GetAccountsAsync(null)` walks every group. No `Take(200)` in `src`. Measured 2026-08-18: Achiever **8 / 6512**, Starwave **10 / 1948**, total **18 / 8460**. |
| Copy to cTrader must **not** send live orders yet (no capital loss) | **Yes — `SAFE_BY_ABSENCE` + flag forced false** | Product C# has **0** `35=D` / `(35, "D")` builders. `CTraderFixSession` emits **`35=A` only**. `RealCopyEnabled` hardcoded **false**. Risk `AllowFixSend` requires `RealExecutionEnabled`. YoPips C++ `SendTrade` is **source MT5**, not this destination. |

One-liner:

```text
VENUE=Pepperstone/cServer FIX 4.4 (NOT LP)
TAG56=cServer (ordinal; no fold)
QUOTE TLS :5211   TRADE TLS :5212
SOURCE=ALL manager-visible Achiever+Starwave groups/logins
DEST SEND=OFF (no 35=D; RealCopyEnabled=false) → no live loss from this process
```

**Do not** call this “live copy trading.” Catalog + optional diagnostic Logon is the current product. Live `NewOrderSingle` stays **off**.

---

## 1. cTrader is the destination venue, not an LP

### 1.1 Binding architecture (quoted)

`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §1.6 item 6 (lines 88–89):

> **Do not call the cTrader account an LP unless it actually is your contractual LP relationship.**
> Technically this architecture treats Pepperstone/cServer FIX as the **external execution venue**. The software must not assume institutional LP semantics that the account does not provide.

§25 title (line 1023):

> **# 25. New Execution Venue: cTrader / cServer FIX 4.4**
>
> Real approved copy trades will route to the provided Pepperstone cTrader account through cServer FIX 4.4.

`D:\Prop\docs\ctrader-fix.md` line 5:

> cTrader is used as a **hedging execution venue** — not a liquidity provider.

`D:\Prop\docs\architecture.md` line 22: `TargetCompID = cServer` (case preserved). Live TRADE send is “explicitly not enabled.”

There is **no** signed LP contract in this repo. Until one exists, the name is **execution venue** / **destination**. Required future table name is `execution_venues` (A87), **not** `lps`.

### 1.2 Why “LP” is the wrong word here

| Institutional LP | This account |
|---|---|
| Wholesale liquidity (prime, ECN aggregator, Ultency LP) | Pepperstone cTrader login `1369850` over cServer FIX 4.4 |
| LP book, last-look, LP credit | Two broker-gateway sessions (QUOTE + TRADE) |
| Software may assume LP symbols / LP fills | Software must assume **broker execution reports** (`35=8`) only |
| MetaQuotes Ultency `LiquidityProvider` APIs | Vendor-only under MT5 SDK; **do not map** onto destination |

Official Help (`https://help.ctrader.com/fix/`, re-fetched 2026-08-18) splits industry uses:

- **Provide prices:** “Liquidity providers and price makers such as banks or exchanges use FIX API to provide prices to brokers…”
- **Trade copiers:** “Systems that will automatically replicate trades on multiple trading accounts…”

This product is the **trade-copier / destination-account** side. It is **not** acting as, or talking to, a contractual LP.

### 1.3 Product-source naming (measured this pass)

| Tree | `\bLP\b` / `LiquidityProvider` applied to cTrader |
|---|---|
| `D:\Prop\src` (`*.cs`) | **0** |
| `D:\Prop\apps` (`*.cs` / `*.tsx` / `*.ts`) | **0** |
| `D:\Prop\tests` | **0** (not re-scored as a type name) |
| `D:\Prop\docs\ctrader-fix.md` | 1 **prohibition** (“not a liquidity provider”) |
| Architecture §1.6 | 1 **prohibition** |
| YoPips C++ `src/` | **0** `cTrader` / `cServer` / `5211` / `5212` |

Product vocabulary already used: `DestinationQuote`, `VenueInstrumentId`, `VenueHealthy`, `VENUE_UNHEALTHY`, `PauseVenue`, `DestinationAccount`. Source brokers are **only** `ACHIEVER` and `STARWAVEFX` (`BrokerCodes.cs`).

**OPEN (not an LP violation):** there is still **no** `ExecutionVenue` entity / `execution_venues` table. Destination is implied by FIX session rows + host `live-us-eqx-01.p.c-trader.com`.

### 1.4 Hits that are **not** “cTrader is an LP”

| Hit | Meaning |
|---|---|
| Starwave group `Starwave\real\FX3\LP` (2 accounts) | **Source** MT5 group name from the 2026-08-18 Manager census. Not the destination venue. |
| YoPips `SendTrade` / FIX5 comments | C++ **challenge** backend placing **source MT5** dealer trades. Not cTrader. |
| Vendor `MT5APIConfigUltLiquidity.h` `ParameterFIXSenderCompID` | MetaQuotes Ultency LP config. Unrelated. |
| Architecture “LP / gateway fill” deal reason `GATEWAY` | Source-deal reason enum, not destination type. |

### 1.5 YoPips C++ backend (destination-negative)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` contains **no** `cTrader`, `cServer`, `5211`, or `5212`. Its `trade_execution_service.cpp` `SendTrade` path is the **prop-firm challenge execution** onto Achiever/Starwave MT5. That is the **source** book this product **reads**. It is **not** the Pepperstone destination. Do not route copy orders through YoPips `SendTrade`.

---

## 2. TargetCompID `cServer` — case preserved

### 2.1 Law (quoted)

Architecture §26 item 4 (line 1101):

> never silently change case such as `cServer` to `CSERVER` unless the issued configuration/spec requires it

§25 env sample:

```env
CTRADER_FIX_QUOTE_TARGET_COMP_ID=cServer
CTRADER_FIX_TRADE_TARGET_COMP_ID=cServer
```

Official RoE (`https://help.ctrader.com/fix/specification/`, re-fetched 2026-08-18) table:

> Tag 56 `TargetCompID` — “The valid value is `CSERVER`.”

Official credentials screenshot (`https://help.ctrader.com/fix/getting-credentials/`, R030):

> `TargetCompID: cServer` on **both** Price and Trade connections.

Those two official spellings **conflict**. §26 exists for that reason. **Issued Pepperstone form + architecture env = `cServer`.** `CSERVER` is legal only as an **explicit, logged operator override**, never `ToUpperInvariant()`.

### 2.2 Product C# census (this pass)

`CSERVER` under `D:\Prop\src`: **0 hits.**

`cServer` under `D:\Prop\src`: **12 hits / 5 files**, all ordinal literals:

| File | Line | Role |
|---|---:|---|
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | 49 | `Quote.TargetCompId` default `"cServer"` |
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | 70 | `Trade.TargetCompId` default `"cServer"` |
| `src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs` | 43 | `?? "cServer"` (tag 56 for **both** sessions) |
| `src/Infrastructure/Seeding/BrokerCatalogSeed.cs` | 88, 102 | QUOTE + TRADE `FixSessionState` |
| `src/Infrastructure/Seeding/DemoSeeder.cs` | 77, 95 | demo FIX rows (tests only) |
| `src/Fix.CTrader/Testing/FixSimulationHarness.cs` | 16, 30, 129, 155, 187 | defaults + tag `(56, "cServer")` |

`Fix.CTrader` has **0** `ToUpper` / `ToUpperInvariant` / `ToLower`. `ExecutionOrderStateMachine` uppercases **OrdStatus**, not CompID.

Live header builder (`CTraderFixSession.BuildLogon`) writes tag 56 as the **caller string**:

```94:101:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            (49, sender),
            (56, target),
            (50, senderSub),
            (57, targetSub),
```

Hosted service:

```43:58:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var target = _config["CTRADER_FIX_QUOTE_TARGET_COMP_ID"] ?? "cServer";
        // ...
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
```

Gitignored `.env` keys (values **not** copied): `CTRADER_FIX_QUOTE_TARGET_COMP_ID=cServer`, `CTRADER_FIX_TRADE_TARGET_COMP_ID=cServer`.

Integration pin:

```35:35:D:\Prop\tests\Integration\SeedingAndStoreTests.cs
        db.FixSessionStates.Select(s => s.TargetCompId).Distinct().Should().Equal("cServer");
```

Dashboard copy (`FixSessionsPage.tsx` L8): “TargetCompID stays `cServer`.”

### 2.3 Residual case trap (do not paper over)

| Surface | Value | Bound to live logon? |
|---|---|---|
| `apps/api/appsettings.json` `CTraderFix.TargetCompId` | **`CSERVER`** | **No.** Hosted service does **not** bind `CTraderFix:*`. Dead leftover + unofficial host `fix.ctrader.com` + **plain** 5201/5202. |
| `CTraderFixOptions` `IOptions<>` | unbound | DI never `Configure<CTraderFixOptions>`. |
| TRADE env `CTRADER_FIX_TRADE_TARGET_COMP_ID` | present in `.env` as `cServer` | Hosted service **ignores** it; both sockets use the **QUOTE** target key. Harmless while both are `cServer`. |
| Official RoE examples | `56=CSERVER` | Spec text, not the issued form. |

**Do not** “align with RoE” by editing defaults back to `CSERVER`. **Do not** delete `appsettings.json` `CSERVER` in this research slot (read-only). Next coding wave should remove or quarantine that dead `CTraderFix` block so a future binder cannot emit `56=CSERVER` by accident.

Older reports (B27 / C09 / D26) said **HEAD** still had `CSERVER` in `CTraderFixOptions`. **This worktree file defaults `cServer`.** Treat those HEAD-vs-worktree notes as historical unless a later `git show HEAD` re-measures.

---

## 3. Ports: QUOTE 5211 SSL, TRADE 5212 SSL

### 3.1 Official

Credentials form (Help screenshot, R030 / A31, re-confirmed via `https://help.ctrader.com/fix/getting-credentials/`):

| Connection | SSL | Plain | Form `SenderSubID` | RoE qualifier (tag 57) |
|---|---:|---:|---|---|
| Price / QUOTE | **5211** | 5201 | `QUOTE` | `QUOTE` |
| Trade / TRADE | **5212** | 5202 | `TRADE` | `TRADE` |

Official current Spotware C# sample uses **TLS on 5211/5212** (`SslStream.AuthenticateAsClient`). RoE Connectivity section does **not** publish a global hostname/port; FAQ says check **your** form. This lab’s issued host is `live-us-eqx-01.p.c-trader.com` (architecture §25). **Do not** replace it with `fix.ctrader.com`.

Help also: trading operations **cannot** be sent on the price connection (and vice versa). Two sockets are mandatory.

### 3.2 Product defaults (measured)

```41:63:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
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

`UseSsl` default **true**. `RealCopyExecutionEnabled` default **false**.

Hosted service **hardcodes** SSL ports and never opens 5201/5202:

```48:55:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target, ...);
        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target, ...);
```

Transport is always TLS:

```35:44:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
            using var tcp = new TcpClient();
            // ...
            await tcp.ConnectAsync(host, sslPort, timeoutCts.Token);
            await using var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = host,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            }, timeoutCts.Token);
```

Seed rows match: QUOTE **5211**, TRADE **5212** (`BrokerCatalogSeed` L86 / L100). Persist after logon rewrites `row.Port` to the same pair (hosted L103).

`.env` keys (no secret values): `CTRADER_FIX_QUOTE_SSL_PORT=5211`, `CTRADER_FIX_TRADE_SSL_PORT=5212`, `CTRADER_FIX_USE_SSL=true`. Hosted service does **not** read those port env keys; the literals **5211/5212** are the runtime ports.

### 3.3 Dead / stale port config

`apps/api/appsettings.json` `CTraderFix` block:

```json
"QuoteHost": "fix.ctrader.com",
"QuotePort": 5201,
"TradeHost": "fix.ctrader.com",
"TradePort": 5202,
"TargetCompId": "CSERVER"
```

**Unused.** Do not treat this as the live session. Production default is **TLS 5211/5212**, not plaintext.

---

## 4. Fetch ALL Achiever + Starwave groups and ALL manager traders

### 4.1 Law

Architecture §7–§9: enumerate **manager-visible** groups dynamically. Plan-group mappings are **labels, not fetch filters** (`docs/architecture.md` L24). A39: `GroupTotal` / `GroupNext` / `GroupRequestArray("*")` — never filter by plan env.

Source brokers (`BrokerCodes.cs`):

```4:6:D:\Prop\src\Domain\Brokers\BrokerCodes.cs
    public const string Achiever = "ACHIEVER";
    public const string StarwaveFx = "STARWAVEFX";
```

DI registers **both** native connectors (`LiveMt5Registration.CreateConnectors`). Dummy/fake path is refused when real passwords are missing (`DependencyInjection.cs` L35–36).

### 4.2 Code path (no truncation)

`NativeMt5BrokerConnector.GetGroupsCore`:

1. `GroupRequestArray("*", arr)` — all groups the manager can see.
2. Fallback `GroupTotal` / `GroupNext` if the array is empty.

`GetAccountsCore(null)`:

1. Loads **every** group name from `GetGroupsCore`.
2. Per group: `UserRequestArray` → `UserGetByGroup` → `UserLogins` + `UserRequestByLogins`.
3. Dedupes by login. **No** `Take(200)`.

`D:\Prop\src` grep `Take(200)`: **0 hits.**

`DealIngestionService.SyncCatalogAsync`:

```44:50:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`LiveIngestHostedService` and `POST /api/ops/resync` call that catalog for **ACHIEVER** then **STARWAVEFX**. Positions use `GetGroupPositionsAsync("*")` when the bulk interface is present.

### 4.3 Measured census (already on disk; this slot did not re-probe)

`D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` + `CREDENTIALS_AND_COPY_STATUS.md` + `SWARM_LOG.md` (2026-08-18T08:45Z):

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| Achiever | OK via HTTP proxy (`81.29.145.69`) | **8** | **6512** | 1506 |
| StarwaveFX | OK direct | **10** | **1948** | 478 |
| **Total** | | **18** | **8460** | **1984** |

Dashboard `/api/traders` **8460**, `/api/groups` **18**. Full login dump: `LIVE_GROUPS_AND_TRADERS.json` (no passwords).

These are **all groups this manager login can see**. Groups outside manager permission are not claimed.

This slot **did not** re-run Manager connect. Completeness claim for *today’s* process memory is therefore **code-path + prior measured census**, not a new live count.

---

## 5. Copy to cTrader must not send live orders yet (no loss)

### 5.1 What “copy” is allowed to do now

| Allowed now | Forbidden now |
|---|---|
| Manager **read** of all groups/logins/deals/positions | `35=D` NewOrderSingle |
| Persist catalog + scores + SHADOW / CopyIntent | `35=F` cancel / `35=G` replace |
| Diagnostic FIX **Logon `35=A`** on 5211/5212 | Any OrderQty mapping to a live TRADE socket |
| Shadow **simulate** fills from destination quotes | Enable `REAL_COPY_EXECUTION_ENABLED` |

User goal “copy to cTrader **and** no loss” is resolved as: **destination is cTrader; capital protection wins until a real sender + gates exist.**

### 5.2 No `35=D` builder (re-measured)

Product `NewOrderSingle` hits are **name/comment/log only**:

| File | Kind | Emits `35=D`? |
|---|---|---|
| `CTraderFixOptions.cs` L33 | XML comment on flag | **No** |
| `CTraderFixLogonHostedService.cs` L70 | log “NewOrderSingle still disabled” | **No** |
| `DependencyInjection.cs` L40 | comment; forces `RealCopyEnabled = false` | **No** |
| `BrokerCatalogSeed.cs` L105 | TRADE `LastError` | **No** |
| `DemoSeeder.cs` L101 | TRADE `LastError` | **No** |
| `LiveRuntimeStatus.cs` L44 | snapshot copy note | **No** |
| `ExecutionOrderStateMachine.MayRetryNewOrderSingle` | status predicate | **No** |
| `apps/fix-worker/Worker.cs` | log + `LastError`; even if flag true, **refuses** | **No** |

Grep `35=D` / `(35, "D")` / `MsgType="D"` under `D:\Prop\src`: **0** builders.

The **only** outbound FIX MsgType in `CTraderFixSession` is **`35=A`**. `CTraderQuoteService` can *construct in-memory* tag sets `35=y` / `35=V` (SecurityList / MD). Those lists are **not** written to a socket by any hosted service.

`apps/fix-worker/Worker.cs` stamps TRADE `Disconnected` / “NewOrderSingle remains off.” every 15s. If `CTrader:RealCopyExecutionEnabled=true`, it **logs a warning** and still does not send.

### 5.3 Flags and risk (not a wire)

| Surface | Value |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | **`false`** |
| `DependencyInjection` / hosted service | **`runtime.RealCopyEnabled = false`** (cannot be armed) |
| `.env` `REAL_COPY_EXECUTION_ENABLED` | **`false`** |
| `GET /api/settings` | `featureFlags.REAL_COPY_EXECUTION_ENABLED` = `runtime.RealCopyEnabled` |
| `apps/api/appsettings.json` `FeatureFlags.LiveCopyEnabled` | **`false`** (different name, unused by logon) |
| `RiskEngine` L90–93 | when `RealExecutionEnabled==false`, comment-only; **does not reject** |
| `RiskEngine` L147–150 | `AllowFixSend = RealExecutionEnabled && kill==None && Reconciled && VenueHealthy` |

So risk can still `APPROVE` a shadow-sized intent while `AllowFixSend=false`. There is **no** writer that consults `AllowFixSend` and opens TRADE. Vacuous safety: **cannot lose because cannot send.**

Web:

- `LiveCopyPage.tsx`: “Pepperstone/cTrader NewOrderSingle is disabled so this process cannot open a losing live position.”
- `ShadowPortfolioPage.tsx`: “Live NewOrderSingle remains disabled.”

### 5.4 Honest gate before any future live send (not implemented)

Architecture / A101 conjunction, **all** required:

1. Independent QUOTE TLS 5211 Logon **and** TRADE TLS 5212 Logon (`56=cServer`, 57/50 per RoE).
2. Security List → persist numeric XAU instrument id (do not hardcode; do not send tag 55=`XAUUSD`).
3. TRADE recon clean (`VENUE_NOT_RECONCILED` already rejects increasing exposure).
4. Single TRADE session owner (no duplicate ERs).
5. Risk approve + kill switch None + quote fresh.
6. Persist `ClOrdID` **before** send; `MayRetryNewOrderSingle` false after send-attempt.
7. Explicit `REAL_COPY_EXECUTION_ENABLED=true` **and** a real `GuardedNewOrderSingle` that does not exist today.

Until that exists, copy stays **SHADOW**. First-three / early-score must not go LIVE (`docs/architecture.md` L21).

### 5.5 Stale sibling notes

`E034_no_35d.md` claimed **0** `TcpClient` / `SslStream` in product C#. **Stale.** Current `CTraderFixSession` **does** open TLS for **Logon only**. That does **not** create a `35=D` path.

`C42` / early C-wave “live Manager not proven” is **stale** vs `LIVE_MANAGER_FETCH_MEASURED.md`.

---

## 6. Session identity scorecard (this slot)

| Item | Required | On-disk / runtime | Status |
|---|---|---|---|
| Role | Destination execution venue | Docs + architecture + 0 LP types | **PASS** |
| Not an LP | No LP semantics | Confirmed | **PASS** |
| Host | `live-us-eqx-01.p.c-trader.com` | Options + seed + hosted default | **PASS** (API JSON `fix.ctrader.com` dead) |
| Tag 56 | `cServer` ordinal | Defaults + env + seed + harness | **PASS** (dead appsettings `CSERVER`) |
| Silent case fold | Forbidden | None in Fix.CTrader | **PASS** |
| QUOTE port | SSL **5211** | Options + hosted hardcode + seed | **PASS** |
| TRADE port | SSL **5212** | same | **PASS** |
| TLS | Production default | `SslStream` always | **PASS** |
| SenderSubID / TargetSubID | QUOTE / TRADE | Hosted defaults `QUOTE`/`TRADE`; options `SenderSubId` still `""` if someone binds the POCO | **PARTIAL** |
| Tag 553 | Integer account id | Hosted `username = account` | **PASS** (prior reject when CompID was used) |
| `35=A` | Diagnostic only | Implemented | EXISTS |
| `35=D` | Off | Missing builder | **`SAFE_BY_ABSENCE`** |
| QuickFIX/n 1.14.1 + `FIX44-CSERVER.xml` | Preferred engine (§1.8) | Still a hand-rolled logon | **GAP** (not a send path) |
| ALL groups/traders | Manager-visible universe | `*` + per-group users; census 18 / 8460 | **PASS** (code + prior measure) |

---

## 7. Risk to capital (slot 7)

| Path | Can lose live money? |
|---|---|
| Manager catalog / deal / position **read** | **No** |
| Dashboard `/api/groups` `/api/traders` | **No** |
| Shadow simulate | **No** (in-process math) |
| FIX `35=A` on 5211/5212 | **No fill.** Session only. |
| FIX `35=D` | **Impossible in current product C#** |
| YoPips `SendTrade` | Different process/product (challenge MT5). Out of this destination path. |
| Flipping `.env` `REAL_COPY_EXECUTION_ENABLED=true` | **Still no send** (`RealCopyEnabled` forced false; no builder) |

**Risk to capital from this slot’s subject (copy-to-cTrader): none.** Safety is **absence of a sender**, not a proven production gate. Do not enable live send from this research file.

---

## 8. Authorized later work (do **not** apply here)

1. Delete or quarantine unused `apps/api/appsettings.json` `CTraderFix` (`CSERVER`, `fix.ctrader.com`, 5201/5202).
2. Bind `CTRADER_FIX_*_TARGET_COMP_ID` **verbatim** per session; log if operator sets `CSERVER`.
3. Set options `SenderSubId` defaults to `QUOTE`/`TRADE` (RoE: tag 50 must be `QUOTE` when 57=`QUOTE`).
4. Add `ExecutionVenue` / `execution_venues` — never `Lp`.
5. Keep `35=D` compiled out until A100 + A101 pass.
6. Do not send copy orders through YoPips `SendTrade`.

---

## 9. Sources

- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§1.6, 7–9, 25–27, 41, 56
- `D:\Prop\docs\architecture.md`, `D:\Prop\docs\ctrader-fix.md`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs`
- `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs`
- `D:\Prop\apps\api\Program.cs`, `apps/api/appsettings.json`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx`, `FixSessionsPage.tsx`, `ShadowPortfolioPage.tsx`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- `D:\Prop\reports\swarm\20260818\A87_not_an_lp.md`, `D58_lp.md`, `R030_fix_headers.md`, `A003_fix_noloss.md`, `E002_no_live_send.md`, `A31_ctrader_fix_overview.md`
- Official: https://help.ctrader.com/fix/ · https://help.ctrader.com/fix/getting-credentials/ · https://help.ctrader.com/fix/specification/
- YoPips: `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` (no cTrader destination)

*End of W500_RESEARCH_7. Product source was not modified. No secrets printed.*
