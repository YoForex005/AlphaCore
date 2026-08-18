# W500_RESEARCH_107 — cTrader is destination venue, not LP

| Field | Value |
|---|---|
| Slot | **107** |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (this pass: product worktree + YoPips C++ `src\` + official RoE/Help re-fetch + independent census re-sum). Live Manager attach **not** re-run. Census dump reused: `LIVE_GROUPS_AND_TRADERS.json` `utc=2026-08-18T08:42:16.8519545+00:00`. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_107.md` |
| Assigned | Confirm cTrader is **destination venue, not LP**. `TargetCompID` **`cServer`** case preserved. Ports **5211 QUOTE** and **5212 TRADE SSL**. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader **must not send live orders yet** (no loss). |
| Product source modified | **No.** Report + index/log pin only. |
| Test / config / `.env` edited | **No.** |
| Secret values printed | **None.** Manager / proxy / FIX tag `554` values not copied. `.env` `CTRADER_FIX_PASSWORD` classified present-by-name only. Account id `1369850` is a non-secret venue login. |
| Live `35=A` / `35=D` sent this pass | **No.** |
| YoPips C++ tree | `D:\Projects\YoPips\Backend\C++ Backend PropFirm` — source MT5 Manager only. Grep of `src\` for `cTrader` / `cServer` / `5211` / `5212` / `NewOrderSingle` / `TargetComp`: **0 hits**. |
| Siblings (same topic, different slots) | `W500_RESEARCH_27.md`, `W500_RESEARCH_47.md`, `W500_RESEARCH_67.md` — not copied as proof. Slot 107 re-read law + current worktree + official Help. |

**Honesty rule:** wanting live copy *and* no loss does not make either true. A TLS Logon (`35=A`) is not a NewOrderSingle. Official RoE table spelling `CSERVER` is not a license to silently fold the issued form `cServer`. A Starwave **source group** named `Starwave\real\FX3\LP` is not evidence that Pepperstone/cTrader is an LP. Vendor MetaQuotes Ultency `LiquidityProvider` headers in YoPips/MT5 SDK are **server-side LP APIs**, not this destination account.

---

## 0. Verdict

**CONFIRMED on the live path. Pepperstone/cTrader FIX (`cServer`) is the destination execution venue, not an LP. Issued `TargetCompID=cServer` is preserved (no `ToUpper`/`ToLower` anywhere in `Fix.CTrader`). Production transport is TLS QUOTE 5211 + TRADE 5212. Catalog fetch is ALL manager-visible groups/traders. Live `35=D` is impossible today (`SAFE_BY_ABSENCE`). Risk to capital from this process: NONE.**

| Claim | Measured this slot | Class |
|---|---|---|
| cTrader is destination venue, **not** LP | Architecture §1.6 / §25; `docs/ctrader-fix.md` L5; official Help = client→cTrader gateway; product C#/TS/JSON **0** `LP`/`LiquidityProvider` identifiers | **CONFIRMED** |
| `TargetCompID` `cServer` case preserved | Options defaults, hosted-service fallback, seed rows, harness, UI, integration test, `.env` key names. `Fix.CTrader` has **0** `ToUpper`/`ToLower` calls. Tag 56 is the string given. | **CONFIRMED (live path)** |
| QUOTE SSL **5211** / TRADE SSL **5212** | Options `SslPort`; hosted service **hardcodes** 5211/5212; seed ports; official credentials form. `TryLogonAsync` always `SslStream` TLS 1.2\|1.3. | **CONFIRMED** |
| Fetch ALL Achiever + Starwave groups + ALL manager traders | `GetGroupsAsync` + `GetAccountsAsync(null)`; `GroupRequestArray("*")` + `UserRequestArray`; ingest `Take(` = 0. JSON re-sum **8+10 / 6512+1948**. | **CONFIRMED (code + 2026-08-18 census re-sum)** |
| Copy to cTrader must not send live orders yet | `RealCopyEnabled` forced `false` (DI + hosted + POCO). Product `*.cs`/`*.cpp`/`*.h`/`*.json` have **0** `35=D` / `(35, "D")`. Logon emits only `35=A` then **disposes** the socket. | **CONFIRMED — `SAFE_BY_ABSENCE` / no capital at risk** |

**Leftovers (do not greenwash):**

1. Official RoE *table* still prints tag 56 valid value `CSERVER`. Architecture §26 forbids silently changing issued `cServer` → `CSERVER`. Live code keeps `cServer`. `CSERVER` is legal only as an explicit operator override (`CTRADER_FIX_*_TARGET_COMP_ID`).
2. Dead leftover `D:\Prop\apps\api\appsettings.json` `CTraderFix.TargetCompId = "CSERVER"` and plain ports **5201/5202** / host `fix.ctrader.com`. **Not bound.** Live logon does **not** read that JSON block.
3. `CTraderFixOptions` is **not** registered (`no Configure<CTraderFixOptions>`). Live logon reads `CTRADER_FIX_*` env keys + hardcoded ports.
4. Architecture table `execution_venues` is still **unbuilt**. Absence of the word LP ≠ venue entity exists.
5. This pass did **not** re-attach Manager or re-open TLS. Census numbers below are the 2026-08-18 measured dump, independently re-summed, not a new probe.
6. `CTraderQuoteService` exists but has **zero** DI registrations. It cannot emit TRADE application messages. Its in-memory `BuildSecurityListRequestTags` even emits `35=y` (SecurityList *response*), never a socket write.

One-liner:

```text
VENUE ≠ LP
56=cServer (issued case; no fold)
QUOTE TLS :5211  TRADE TLS :5212
ALL groups/traders (mask * / group=null)
35=D OFF — logon/recon/shadow only — no live loss
```

---

## 1. cTrader is destination venue, not LP

### 1.1 Binding law (quoted)

Architecture `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §1 item 6 (L88–89):

> **Do not call the cTrader account an LP unless it actually is your contractual LP relationship.**
> Technically this architecture treats Pepperstone/cServer FIX as the **external execution venue**. The software must not assume institutional LP semantics that the account does not provide.

§25 title (L1023–1025):

> **# 25. New Execution Venue: cTrader / cServer FIX 4.4**
>
> Real approved copy trades will route to the provided Pepperstone cTrader account through cServer FIX 4.4.

`D:\Prop\docs\ctrader-fix.md` L5:

> cTrader is used as a **hedging execution venue** — not a liquidity provider. The prop firm's challenge accounts run on MT5; winning trades are copied to cTrader for real-money hedging via FIX 4.4 protocol (QuickFIX/N engine).

`D:\Prop\docs\architecture.md` safety defaults (L20–22): `REAL_COPY_EXECUTION_ENABLED=false`; TargetCompID = `cServer` (case preserved).

A87 / D58 remain the naming law: destination entity, when added, is `ExecutionVenue` / `execution_venues`. Never `Lp`, `LiquidityProvider`, or `lp_account`.

### 1.2 Official Help (re-fetched this pass)

`https://help.ctrader.com/fix/` (2026-08-18, this slot):

- cTrader FIX is **FIX 4.4**.
- Typical industry applications include brokerage (brokers receive prices / execute **their clients'** orders) and a **separate** “Provide prices” application: “Liquidity providers and price makers such as banks or exchanges use FIX API to provide prices to brokers.” That LP role is **not** this lab’s Pepperstone retail/prop account.
- Trade-copier is listed as a *possible* FIX use; Spotware even prefers other APIs for that. Direction is still **client → cTrader**.

`https://help.ctrader.com/fix/getting-credentials/` (2026-08-18, this slot):

> There are 2 types of connection, price connection and trade connection, and each type has its own separate set of credentials. Trading operations requests cannot be sent through the price connection's credentials and vice versa.

That is two **client** sessions to a broker FIX engine. It is not an institutional LP credit line.

`https://help.ctrader.com/fix/specification/` standard header (2026-08-18, this slot):

| Tag | Field | Official RoE table |
|---|---|---|
| 56 | `TargetCompID` | Valid value **`CSERVER`** |
| 57 | `TargetSubID` | **`QUOTE`** or **`TRADE`** (session qualifier) |
| 50 | `SenderSubID` | Must be `QUOTE` if `TargetSubID=QUOTE` |

Official Logon **example** uses `56=CSERVER`. Official **credentials form** (R030 / A31, Help screenshot `getting-fix-api-0.png`) prints **`TargetCompID: cServer`**, Price port **`5211 (SSL), 5201 (Plain text)`**, Trade port **`5212 (SSL), 5202 (Plain text)`**, SenderSubID `QUOTE` / `TRADE`. Architecture §26 item 4: never silently change case. This lab’s issued env sample is `cServer`.

RoE application list (same page) names **`New Order Single (Client → cTrader)`**. Direction is **this process → venue**. The venue is not streaming LP liquidity *into* this product.

### 1.3 Source vs destination (this product)

| Side | What it is | What it is not |
|---|---|---|
| Achiever MT5 (manager via HTTP proxy to allow-list `81.29.145.69`) | **Source** challenge book | Not the hedge account |
| StarwaveFX MT5 (`MT5_STARWAVEFX_*`, manager, direct) | **Source** challenge book | Not the hedge account |
| Pepperstone / cServer FIX (`live-us-eqx-01.p.c-trader.com`, account `1369850`) | **Destination execution venue** | **Not** an LP |

`LiveMt5Registration.CreateConnectors` (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L20–49) builds **exactly two** native connectors: Achiever + StarwaveFX. Those are sources. Destination is `Fix.CTrader` only.

### 1.4 YoPips C++ is source Manager, not the venue

Grep of `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` for `cTrader` / `cServer` / `5211` / `5212` / `NewOrderSingle` / `TargetComp`: **0 hits**.

The only “liquidity provider” strings in that tree live under `MetaTrader5SDK\Include\` Ultency types (`MT5APIUltDeal.h`, `MT5APIUltLiquidityOrder.h`, `MT5APIConfigUltLiquidity.h`). Those are **MT5 server-side Ultency LP APIs**. They must not be mapped onto Pepperstone account `1369850`. Owned YoPips wrapper `src\core\mt5_manager.cpp` enumerates **groups/users**, not cTrader.

This slot re-grepped `D:\Prop\src` + `D:\Prop\apps` `*.{cs,tsx,ts,json}` for `LiquidityProvider` / identifier `LP`: **0 product identifier hits**. The only `\bLP\b` hits under the report tree’s live dump are the Starwave **source** group name `Starwave\real\FX3\LP` (2 accounts) in `LIVE_GROUPS_AND_TRADERS.json`. That is not a cTrader LP type.

When the destination entity is added, the name is **`ExecutionVenue` / `execution_venues`**. That table is still **absent**.

---

## 2. TargetCompID `cServer` — case preserved

### 2.1 Why case is a real law

Official **credentials form** (Help screenshot, R030):

```text
TargetCompID: cServer
```

Official **RoE** (`https://help.ctrader.com/fix/specification/`):

> Tag 56 TargetCompID — “A message target. The valid value is `CSERVER`.”

Architecture §26 item 4 (L1101):

> never silently change case such as `cServer` to `CSERVER` unless the issued configuration/spec requires it

Lab issued form + architecture env sample (§25 L1041 / L1051):

```env
CTRADER_FIX_QUOTE_TARGET_COMP_ID=cServer
CTRADER_FIX_TRADE_TARGET_COMP_ID=cServer
```

`.env` (gitignored; **names + case only**, values for secrets not copied) matches that spelling on both keys. `CSERVER` is allowed only as an **explicit, logged override**. It must not be the silent compiled default.

### 2.2 Live path (measured this pass)

| Surface | Literal | Used on wire? |
|---|---|---|
| `CTraderFixOptions.QuoteFixOptions.TargetCompId` default L49 | `"cServer"` | POCO default; type **not** bound in DI |
| `CTraderFixOptions.TradeFixOptions.TargetCompId` default L70 | `"cServer"` | same |
| `CTraderFixLogonHostedService` L43 | `_config["CTRADER_FIX_QUOTE_TARGET_COMP_ID"] ?? "cServer"` | **Yes — live tag-56 value** (same string reused for TRADE) |
| `BrokerCatalogSeed` QUOTE L88 + TRADE L102 | `TargetCompId = "cServer"` | persisted session identity |
| `DemoSeeder` FIX rows L77 / L95 | `"cServer"` | demo only (API startup does **not** call DemoSeeder) |
| `FixSimulationHarness` defaults + `(56, "cServer")` | `"cServer"` | tests / harness |
| Dashboard `FixSessionsPage.tsx` L8 | “TargetCompID stays `cServer`” | UI copy |
| `docs/architecture.md` L22 | “TargetCompID = `cServer` (case preserved)” | law |
| Integration `SeedingAndStoreTests` L35 | `TargetCompId` Distinct **Equal `"cServer"`** | test pin |
| `.env` `CTRADER_FIX_*_TARGET_COMP_ID` | `cServer` (both) | live hosted-service read |
| `Fix.CTrader` `ToUpper` / `ToLower` / `ToUpperInvariant` | **0 hits** | no fold |

`CTraderQuoteService` L54 uses `StringComparison.OrdinalIgnoreCase` **only** for SecurityList symbol name `XAUUSD`. That is not tag 56.

`ExecutionOrderStateMachine.MapOrdStatus` `ToUpperInvariant()` maps **OrdStatus/ExecType**, not CompID. Do not confuse that with tag 56.

Live logon builder writes tag 56 as the string it is given — no case mutate:

```89:108:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
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
```

Hosted service (same target string for both sessions; ports hardcoded):

```40:68:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var host = _config["CTRADER_FIX_HOST"] ?? "live-us-eqx-01.p.c-trader.com";
        var account = _config["CTRADER_FIX_ACCOUNT_ID"] ?? "1369850";
        var sender = _config["CTRADER_FIX_QUOTE_SENDER_COMP_ID"] ?? "live.pepperstone.1369850";
        var target = _config["CTRADER_FIX_QUOTE_TARGET_COMP_ID"] ?? "cServer";
        var username = account;
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target, ...);
        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target, ...);
        ...
        _runtime.RealCopyEnabled = false;
```

File is **135** lines. Single outbound `WriteAsync`. `using TcpClient` + `await using SslStream` dispose after one read.

### 2.3 Dead leftover (must not be treated as live)

`D:\Prop\apps\api\appsettings.json` L23–34 still has:

```json
"CTraderFix": {
  "QuoteHost": "fix.ctrader.com",
  "QuotePort": 5201,
  "TradeHost": "fix.ctrader.com",
  "TradePort": 5202,
  "TargetCompId": "CSERVER"
}
```

Product-tree grep of `CSERVER` in `*.{cs,json,tsx,ts}`: **exactly this one line**.

`AddTraderIntelligence` (`D:\Prop\src\Infrastructure\DependencyInjection.cs`) does **not** `Configure<CTraderFixOptions>` and does **not** `GetSection("CTraderFix")`. The hosted service reads `CTRADER_FIX_*` env keys, not this JSON. Treat the block as **DEPRECATED / unbound**. Do not “fix” live case by copying this leftover. Do not replace issued host `live-us-eqx-01.p.c-trader.com` with `fix.ctrader.com`.

Older reports (B27 / C09 / C21) that said “HEAD still `CSERVER` in `CTraderFixOptions`” are **stale vs today’s worktree**: both option defaults are `"cServer"` on disk now (D26).

---

## 3. Ports 5211 QUOTE and 5212 TRADE — SSL

### 3.1 Official numbers

Official credentials form (R030 / Help `getting-fix-api-0.png`):

| UI block | Port line | Qualifier on same screenshot |
|---|---|---|
| Price Connection | **5211 (SSL)**, 5201 (plain) | SenderSubID `QUOTE` |
| Trade Connection | **5212 (SSL)**, 5202 (plain) | SenderSubID `TRADE` |

Official RoE Connectivity section does **not** publish a global hostname or port (A31). FAQ: check **your** host/port. This lab’s issued host remains `live-us-eqx-01.p.c-trader.com`.

Architecture §25 production transport (L1065–1068):

```text
QUOTE = 5211
TRADE = 5212
```

Plain 5201/5202 must not be the production default.

`.env` names (this slot; secret values not copied): `CTRADER_FIX_QUOTE_SSL_PORT=5211`, `CTRADER_FIX_TRADE_SSL_PORT=5212`, `CTRADER_FIX_USE_SSL=true`. Hosted service **does not read those port keys** — it hardcodes 5211/5212. That is still the correct production pair.

### 3.2 Product (measured this pass)

| Location | QUOTE | TRADE | TLS |
|---|---:|---:|---|
| `CTraderFixOptions.Quote/Trade.SslPort` L43 / L61 | **5211** | **5212** | `UseSsl = true` L26 |
| Same POCO `PlainPort` | 5201 | 5202 | not production default |
| `CTraderFixLogonHostedService` L49 / L55 / persist L103 | hardcoded **5211** | hardcoded **5212** | **always** `SslStream` TLS 1.2 \| 1.3 |
| `BrokerCatalogSeed` L86 / L100 | 5211 | 5212 | identity only |
| `DemoSeeder` L75 / L93 | 5211 | 5212 | demo only |
| Dead `appsettings.json` `CTraderFix` | 5201 | 5202 | **unbound leftover** |

`CTraderFixSession.TryLogonAsync` always:

1. `TcpClient.ConnectAsync(host, sslPort)`
2. `SslStream` + `AuthenticateAsClient` (`Tls12 | Tls13`)
3. send Logon `35=A`
4. **dispose** the socket after one read (`using` TcpClient + `await using` SslStream)

It never dials 5201/5202. Parameter name is `sslPort`.

Header qualifier on the live path (form + RoE together):

| Tag | QUOTE | TRADE |
|---|---|---|
| 56 TargetCompID | `cServer` | `cServer` |
| 57 TargetSubID | `QUOTE` | `TRADE` |
| 50 SenderSubID | `QUOTE` (RoE: must be QUOTE when 57=QUOTE) | `TRADE` (legal originator string) |

POCO gap (do not paper over): `CTraderFixOptions.Quote/Trade.SenderSubId` default to `string.Empty`. The hosted service **does not** bind that POCO; it applies `QUOTE`/`TRADE` from env/fallback itself. Seed `DemoSeeder` leaves TRADE `SenderSubId` unset. Intended RoE pair is still SenderSubID `QUOTE`/`TRADE` and TargetCompID `cServer`.

---

## 4. Fetch ALL Achiever + Starwave groups and ALL manager traders

### 4.1 Product path (no plan-group filter)

`LiveMt5Registration.CreateConnectors` builds **exactly two** native connectors: Achiever + StarwaveFX. Dummy/fake brokers are refused if real passwords are missing (`HasRealPasswords` requires both `MT5_PASSWORD` and `MT5_STARWAVEFX_PASSWORD`, not `<SECRET>`, not `(a/c`). Starwave `ProxyEnabled = false` is a hard pin (L45).

`DealIngestionService.SyncCatalogAsync`:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`group == null` means **every group just listed**, not a plan mask.

`NativeMt5BrokerConnector.GetGroupsCore` (`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` L144–186):

1. `GroupRequestArray("*", …)`
2. fallback `GroupTotal` + `GroupNext`

`GetAccountsCore(null)` (L189–213) walks every returned group. Per group (`ReadAccountsForGroup` L216–233):

1. `UserRequestArray`
2. fallback `UserGetByGroup`
3. if still empty: `UserLogins` then `UserRequestByLogins`

Grep of `D:\Prop\src\Application\Ingestion` for `Take(`: **0 hits**. Catalog is not silently capped.

Plan-group mappings are labels, not fetch filters (`docs/architecture.md` L24).

`LiveIngestHostedService` runs that catalog for **every** registered connector. API startup seeds **broker catalog only** (`BrokerCatalogSeed`), not `DemoSeeder` dummy traders.

### 4.2 YoPips C++ (same Manager recipe, source side only)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` `GetAllGroups` L962–981:

```cpp
uint32_t total = m_manager->GroupTotal();
...
for (uint32_t i = 0; i < total; i++) {
    if (m_manager->GroupNext(i, grp) == MT_RET_OK) {
        groups.push_back(StringUtils::toUtf8(grp->Group()));
    }
}
```

`GetUserLogins(group)` → SDK `UserLogins`. Prop `NativeMt5BrokerConnector` is the same recipe plus `GroupRequestArray("*")` first. Achiever on this LAN still needs HTTP `ProxySet` to allow-list `81.29.145.69` (R012). Starwave is direct.

YoPips is **not** the cTrader destination. It is the proven Manager enumeration pattern the C# connector now owns.

### 4.3 Last measured live census (2026-08-18T08:42:16Z) — re-summed this slot

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` (`envLoaded=true`, note: “Passwords never written. Groups and manager logins only.”).  
Write-up: `LIVE_MANAGER_FETCH_MEASURED.md` + `CREDENTIALS_AND_COPY_STATUS.md`.

JSON header re-read this slot (`utc=2026-08-18T08:42:16.8519545+00:00`). Independent arithmetic on `groupNames[].accounts`:

Achiever `2+179+4+5+4+6295+0+23 = 6512` (8 groups).  
Starwave `11+4+170+1735+22+0+0+4+0+2 = 1948` (10 groups).  
Totals **18 / 8460**. Matches JSON `groups`/`accounts` fields.

| Broker | `connected` | `groups` | `accounts` | `openPositions` |
|---|---|---:|---:|---:|
| ACHIEVER | true | **8** | **6512** | 1506 |
| STARWAVEFX | true | **10** | **1948** | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever groups (all this manager can see):

| Group | Accounts |
|---|---:|
| contest\yo-1step | 2 |
| contest\yo-2step | 179 |
| contest\yo-instant | 4 |
| contest\yo-payp | 5 |
| demo\yo-1step | 4 |
| demo\yo-2step | 6295 |
| demo\yo-instant | 0 |
| demo\yo-payp | 23 |

Starwave groups:

| Group | Accounts |
|---|---:|
| Starwave\cent\FX1\grp1 | 11 |
| Starwave\cent\FX1\grp2 | 4 |
| Starwave\demo\FX2\grp1 | 170 |
| Starwave\demo\FX2\grp2 | 1735 |
| Starwave\real\FX3\grp1 | 22 |
| Starwave\real\FX3\grp2 | 0 |
| Starwave\real\FX3\grp3 | 0 |
| Starwave\real\FX3\grp4 | 4 |
| Starwave\real\FX3\grp5 | 0 |
| Starwave\real\FX3\LP | 2 |

Dashboard `/api/traders` returned **8460**. `/api/groups` returned **18** (`CREDENTIALS_AND_COPY_STATUS.md`).

If the server has more groups, they are outside this manager’s permission set. That is an ACL fact, not a code cap.

This research slot did **not** re-run the probe. Numbers above are the permanent measured dump, re-summed. Logins exist in the JSON; they are not reprinted here.

---

## 5. Copy to cTrader must not send live orders yet (no loss)

### 5.1 Gates that stay off

| Gate | Measured state |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` L35 | default **`false`** |
| `DependencyInjection` L38–41 `LiveRuntimeStatus.RealCopyEnabled` | **forced `false`** (“Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.”) |
| Hosted service after logon L68 | `_runtime.RealCopyEnabled = false` |
| `/api/settings` `featureFlags.REAL_COPY_EXECUTION_ENABLED` | `runtime.RealCopyEnabled` (false) |
| `FEATURE_COPY_TRADING_ENABLED` | hardcoded **false** (`Program.cs` L76) |
| `.env` `REAL_COPY_EXECUTION_ENABLED` | **`false`** (name+value of the flag only) |
| `appsettings.json` `FeatureFlags.LiveCopyEnabled` | **false** (different name; not the wired flag) |
| fix-worker L21–46 | reads `CTrader:RealCopyExecutionEnabled` default false; even if true, **logs a warning and still does not send**; stamps TRADE `LastError = "No live TRADE socket. NewOrderSingle remains off."` |
| `CTraderQuoteService` | in-memory SecurityList/MD helpers only; **not registered**; never TRADE |
| `ShadowCopyEngine` | in-memory `SimulateEntry` only; **no socket** |

`LiveRuntimeStatus.Snapshot` copy note when false (L42–43):

> “NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.”

Architecture §41 (L1572–1587):

```env
REAL_COPY_EXECUTION_ENABLED=false
```

allows connect / prices / recon **without** placing new real orders. TRADE up ≠ license to send.

### 5.2 No `35=D` builder (re-measured this pass)

Grep of `D:\Prop` product `*.{cs,cpp,h,json}` for `35=D` / `(35, "D")` / `MsgType = "D"`: **0 hits**.

`CTraderFixSession` emits only `(35, "A")`. After one reply it **disposes** TCP/SSL. There is no keep-alive TRADE initiator, no `OrderQty` on the wire, no cancel/replace (`35=F`/`35=G`).

`NewOrderSingle` strings that remain are comments (`CTraderFixOptions` L33), logs (`CTraderFixLogonHostedService` L70), `LastError` English, `/api/reconciliation/status` note, dashboard copy, and `MayRetryNewOrderSingle` (status math only — `NotSent`/`Rejected`; never opens a socket). `RiskEngine.AllowFixSend` is a DTO bit. It is **not** a socket write.

Official RoE documents `35=D` as **Client → cTrader**. That is the future send we must **not** enable until A100 (§68) and A101 (§70) are measured PASS. Current go-live scorecards remain **0/19** and **0/14** (INDEX headline).

### 5.3 What “copy without live loss” honestly means today

| Allowed now | Forbidden now |
|---|---|
| Manager catalog of all groups/traders | `35=D` NewOrderSingle |
| Reconstruct / score / SHADOW / CopyIntent | `35=F` / `35=G` cancel-replace |
| Diagnostic TLS Logon `35=A` on 5211/5212 | Enabling `REAL_COPY_EXECUTION_ENABLED` |
| Persist FIX session rows | Treating Logon as a fill |

User wants copy **and** no loss. Those two cannot be delivered together **today**: live copy requires a NewOrderSingle; no-loss live copy requires A100/A101 gates that are **not PASS**. The only honest operating mode is **fetch + shadow + venue Logon/recon only**. That is how this process avoids taking a live loss.

Do **not** add a sender in a “research” task. Do **not** flip the flag to “match” configured sessions.

---

## 6. Header / session map (issued form, case preserved)

```text
Host            live-us-eqx-01.p.c-trader.com
QUOTE SSL       5211     50=QUOTE  57=QUOTE
TRADE SSL       5212     50=TRADE  57=TRADE
49 SenderCompID live.pepperstone.1369850
56 TargetCompID cServer          ← issued case; do not fold to CSERVER
553 Username    integer account id (not SenderCompID)
554 Password    never logged
35 outbound     A only (this tree)
```

Account id `1369850` is a venue identifier (non-secret login), not a password.

---

## 7. Cross-checks (siblings; do not treat as this file)

| Sibling | What it pins |
|---|---|
| A87 / D58 | Venue ≠ LP naming law |
| A25 / A31 / A32 / R030 | Official headers, `cServer` vs `CSERVER`, 5211/5212 SSL |
| A003 / E002 / W500_RESEARCH_70 / W500_RESEARCH_68 | No live send; `REAL_COPY` stays false |
| A004 | YoPips `GetAllGroups` / `UserLogins` recipe |
| LIVE_MANAGER_FETCH_MEASURED + `LIVE_GROUPS_AND_TRADERS.json` | 18 / 8460 census |
| W500_RESEARCH_27 / 47 / 67 | Same assigned topic, slots 27 / 47 / 67. This file is slot **107** independent re-measure. |

---

## 8. Sources read (this slot)

- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines)
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs` (unregistered)
- `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\apps\api\Program.cs` + `appsettings.json` (dead `CTraderFix` block)
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\web\src\pages\FixSessionsPage.tsx`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\docs\ctrader-fix.md`, `docs\architecture.md`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §1.6, §25–27, §41
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` (`GetAllGroups` / `UserLogins`)
- Official: https://help.ctrader.com/fix/ · https://help.ctrader.com/fix/specification/ · https://help.ctrader.com/fix/getting-credentials/
- Prior measured dump: `LIVE_MANAGER_FETCH_MEASURED.md`, `LIVE_GROUPS_AND_TRADERS.json` (re-summed), `CREDENTIALS_AND_COPY_STATUS.md`
- `.env` key **names** only (`CTRADER_FIX_*_TARGET_COMP_ID`, SSL ports, `REAL_COPY_EXECUTION_ENABLED`)

---

## 9. Slot-107 close

**CONFIRMED.** Pepperstone/cTrader FIX is the **destination venue**, not an LP. Wire `TargetCompID` stays issued **`cServer`**. Production ports are **QUOTE 5211 SSL** and **TRADE 5212 SSL**. The ingest goal is **all** manager-visible Achiever + Starwave groups and traders (last measured **18 / 8460**, re-summed). Copy must stay SHADOW / Logon-only: **no live orders, no live loss**.

| JSON field | Value |
|---|---|
| slot | 107 |
| verdict | CONFIRMED |
| risk_to_capital | NONE (`SAFE_BY_ABSENCE`; `RealCopyEnabled=false`; no `35=D` builder) |
| evidence | Venue ≠ LP; `56=cServer` no fold; QUOTE TLS 5211 / TRADE TLS 5212; census 18/8460 re-summed; live send off |
