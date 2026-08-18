# W500_RESEARCH_167 — cTrader is destination venue, not LP

| Field | Value |
|---|---|
| Slot | **167** |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (this pass: current product worktree + YoPips C++ `src\` + official Help/RoE re-fetch + independent census re-sum). Live Manager attach **not** re-run. TLS **not** re-opened. This slot did **not** invoke `tools/DemoFixTestTrade`. Census dump reused: `LIVE_GROUPS_AND_TRADERS.json` `utc=2026-08-18T08:42:16.8519545+00:00`. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_167.md` |
| Assigned | Confirm cTrader is **destination venue, not LP**. `TargetCompID` **`cServer`** case preserved. Ports **5211 QUOTE** and **5212 TRADE SSL**. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader **must not send live orders yet** (no loss). |
| Product source modified | **No.** Report + INDEX/SWARM_LOG pin only. |
| Test / config / `.env` edited | **No.** |
| Secret values printed | **None.** Manager / proxy / FIX tag `554` values not copied. `.env` `CTRADER_FIX_PASSWORD` classified present-by-name only. Account ids `5328266` (current lab default) and `1369850` (architecture live sample) are non-secret venue logins. |
| Live `35=A` / `35=D` sent this pass | **No.** |
| YoPips C++ tree | `D:\Projects\YoPips\Backend\C++ Backend PropFirm` — source MT5 Manager only. Grep of `src\` for `cTrader` / `cServer` / `5211` / `5212` / `NewOrderSingle` / `TargetComp` / `LiquidityProvider`: **0 hits**. |
| Siblings (same topic; not copied as proof) | `W500_RESEARCH_27.md`, `47`, `67`, `87`, `107`, `127`, `147`. Slot 167 independently re-read law + **today’s** worktree + official Help. **107/127 “logon re-pins false / live host fallback” is stale.** **147’s “product `*.cs` have 0 `35=D`” is still true as a *string grep*; a demo-only helper now emits MsgType `D` via `Build("D", …)` off the copy hop.** |

**Honesty rule:** wanting live copy *and* no loss does not make either true. A TLS Logon (`35=A`) is not a NewOrderSingle. Official RoE table spelling `CSERVER` is not a license to silently fold the issued form `cServer`. A Starwave **source group** named `Starwave\real\FX3\LP` is not evidence that Pepperstone/cTrader is an LP. Vendor MetaQuotes Ultency `LiquidityProvider` headers in the MT5 SDK are **server-side LP APIs**, not this destination account. Arming `REAL_COPY_EXECUTION_ENABLED=true` is **not** a send. A demo-gated test-trade helper is **not** the copy pipeline.

---

## 0. Verdict

**CONFIRMED on the copy/live path. Pepperstone/cTrader FIX (`cServer`) is the destination execution venue, not an LP. Issued `TargetCompID=cServer` is preserved (no `ToUpper`/`ToLower` in `Fix.CTrader`). Production transport is TLS QUOTE 5211 + TRADE 5212. Catalog fetch is ALL manager-visible groups/traders. Copy-pipeline live `35=D` is impossible today (`SAFE_BY_ABSENCE`). Risk to capital from the copy process: NONE.**

| Claim | Measured this slot | Class |
|---|---|---|
| cTrader is destination venue, **not** LP | Architecture §1.6 / §25; `.env` comment “execution venue (not an LP)”; `docs/ctrader-fix.md` L5; official Help = client→cTrader gateway (LPs are a *different* typical application: “provide prices”). Product `*.cs` **0** `LP` / `LiquidityProvider` identifiers. YoPips `src\` **0** cTrader senders. | **CONFIRMED** |
| `TargetCompID` `cServer` case preserved | Options defaults, hosted-service fallback, seed rows, harness, UI, integration test, `.env` key values. `Fix.CTrader` has **0** `ToUpper`/`ToLower`. Tag 56 is the string given. Product `src\` **0** `CSERVER` literals. | **CONFIRMED (live path)** |
| QUOTE SSL **5211** / TRADE SSL **5212** | Options `SslPort`; hosted service **hardcodes** 5211/5212; seed ports; official credentials form. `TryLogonAsync` always `SslStream` TLS 1.2\|1.3. | **CONFIRMED** |
| Fetch ALL Achiever + Starwave groups + ALL manager traders | `GetGroupsAsync` + `GetAccountsAsync(null)`; `GroupRequestArray("*")` + `UserRequestArray`; ingest `Take(` = 0. JSON re-sum **8+10 / 6512+1948**. | **CONFIRMED (code + 2026-08-18 census re-sum)** |
| Copy to cTrader must not send live orders yet | Copy hop: `CTraderFixSession` emits only `(35, "A")` then **disposes**. `CopyTradingService.NewOrderSingleImplemented=false`; persist `AllowFixSend=false`; `VenueReconciled=false`. Product `*.cs` have **0** literal `35=D`. | **CONFIRMED — `SAFE_BY_ABSENCE` / no capital at risk from copy** |

**Leftovers (do not greenwash):**

1. Official RoE *table* still prints tag 56 valid value `CSERVER` (re-fetched this pass: https://help.ctrader.com/fix/specification/). Architecture §26 forbids silently changing issued `cServer` → `CSERVER`. Live code keeps `cServer`. `CSERVER` is legal only as an explicit operator override (`CTRADER_FIX_*_TARGET_COMP_ID`).
2. Dead leftover `D:\Prop\apps\api\appsettings.json` `CTraderFix.TargetCompId = "CSERVER"` and plain ports **5201/5202** / host `fix.ctrader.com`. **Not bound.** Live logon does **not** read that JSON block. It is the **only** product `CSERVER` literal under `apps`/`src`.
3. `CTraderFixOptions` is **not** registered (`0` `Configure<CTraderFixOptions>` / `GetSection("CTraderFix")` in product `*.cs`). Live logon reads `CTRADER_FIX_*` env keys + **hardcoded** ports 5211/5212. Env SSL-port keys exist and match, but the hosted service does not read them.
4. Hosted service uses `CTRADER_FIX_QUOTE_TARGET_COMP_ID` for **both** sessions (does not read `CTRADER_FIX_TRADE_TARGET_COMP_ID`). Both issued values are `cServer`, so tag 56 is still the issued case.
5. Architecture table `execution_venues` is still **unbuilt**. Grep of `D:\Prop\src` `*.cs` for `ExecutionVenue` / `execution_venues`: **0 hits**. Absence of the word LP ≠ venue entity exists.
6. This pass did **not** re-attach Manager or re-open TLS. Census numbers below are the 2026-08-18 measured dump, independently re-summed, not a new probe.
7. `CTraderQuoteService` exists but has **zero** DI registrations. It cannot emit TRADE application messages.
8. `TraderIntelligence.Fix.CTrader.csproj` has **no** QuickFIX/n package. Copy-path transport is a one-shot `TcpClient` + `SslStream` Logon.
9. **Flag residual (stale siblings 27/47/67/87/107/127):** `DependencyInjection` L41 now binds `REAL_COPY_EXECUTION_ENABLED` from configuration (`OrdinalIgnoreCase`). Lab `.env` L73 is **`true`**. Hosted service **does not** pin `_runtime.RealCopyEnabled = false` (W127 “re-pin” is stale). `/api/settings` therefore reports the flag armed. `README.md` L28 and `docs/architecture.md` L20 still say `false`. Safety is **sender absence on the copy hop**, not a hard-false pin.
10. Current lab host/account defaults are **demo** (`demo-us-eqx-01.p.c-trader.com`, account `5328266`, SenderCompID `demo.pepperstone.5328266`). Architecture §25 still documents **live** `live-us-eqx-01.p.c-trader.com` / `1369850`. `DemoSeeder` (not called at API startup) still seeds the live CompIDs. That is an identity leftover, not an LP claim.
11. **New vs 147 string-grep:** `CTraderFixDemoTestTrade.SendAsync` (`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` L126 / L157) emits MsgType `D` via `Build("D", …)` — not the literal `35=D`. Callers: **only** `D:\Prop\tools\DemoFixTestTrade\Program.cs`. **0** callers in `apps\` or `Infrastructure\`. Gate refuses `live-` host, `live.` sender, and account `1369850`. This is a **demo-only manual tool**, not the copy pipeline. This slot did not run it.

One-liner:

```text
VENUE ≠ LP
56=cServer (issued case; no fold)
QUOTE TLS :5211  TRADE TLS :5212
ALL groups/traders (mask * / group=null)
COPY 35=D OFF — logon/recon/shadow only — no live loss
ENV REAL_COPY=true is armed-but-unhonored (copy sender missing)
Demo-only test-trade helper exists; not on copy hop; live account gated
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

Gitignored `D:\Prop\.env` comment (L47–48, names only; secret not copied):

```text
# cTrader FIX execution venue (not an LP)
```

`D:\Prop\docs\architecture.md` safety defaults: TargetCompID = `cServer` (case preserved); `REAL_COPY_EXECUTION_ENABLED=false` (docs still say false; lab env disagrees — see §5).

### 1.2 Official Help (re-fetched this pass)

`https://help.ctrader.com/fix/` (2026-08-18):

- cTrader FIX is **FIX 4.4**, **client → cTrader**.
- “Typical application methods” lists **liquidity providers** as a **different** use: *“Liquidity providers and price makers such as banks or exchanges use FIX API to provide prices to brokers.”*
- This lab’s intended use is closer to the listed **trade copier** application (client replicating trades onto a broker account). That is a **taker / execution client**, not an LP book.

`https://help.ctrader.com/fix/getting-credentials/` (2026-08-18):

> There are 2 types of connection, price connection and trade connection, and each type has its own separate set of credentials. Trading operations requests cannot be sent through the price connection's credentials and vice versa.

That is two **client** sessions to a broker FIX engine. It is not an institutional LP credit line.

`https://help.ctrader.com/fix/specification/` standard header (2026-08-18, re-fetched this slot):

| Tag | Field | Official RoE table |
|---|---|---|
| 56 | `TargetCompID` | Valid value **`CSERVER`** |
| 57 | `TargetSubID` | **`QUOTE`** or **`TRADE`** (session qualifier) |
| 50 | `SenderSubID` | Must be `QUOTE` if `TargetSubID=QUOTE` |

Application messages include **New Order Single (Client → cTrader)** — a broker execution gateway, not an LP book.

Official Logon **example** uses `56=CSERVER`. Official **credentials form** (Help screenshot + architecture §26 / R030) prints **`TargetCompID: cServer`**. Architecture §26 item 4: never silently change case. This lab’s issued env sample is `cServer`.

### 1.3 Source vs destination (this product)

| Side | What it is | What it is not |
|---|---|---|
| Achiever MT5 (manager login from env, HTTP proxy to allow-list `81.29.145.69`) | **Source** challenge book | Not the hedge account |
| StarwaveFX MT5 (`MT5_STARWAVEFX_*`, manager, direct) | **Source** challenge book | Not the hedge account |
| Pepperstone / cServer FIX (lab default `demo-us-eqx-01.p.c-trader.com`, account `5328266`; architecture live sample `live-us-eqx-01.p.c-trader.com` / `1369850`) | **Destination execution venue** | **Not** an LP |

`LiveMt5Registration.CreateConnectors` (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L20–49) builds **exactly two** native connectors: Achiever + StarwaveFX. Those are sources. Destination is `Fix.CTrader` only. `BrokerCodes` exposes only `ACHIEVER` and `STARWAVEFX`.

### 1.4 YoPips C++ is source Manager, not the venue

Grep of `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` for `cTrader` / `cServer` / `5211` / `5212` / `NewOrderSingle` / `LiquidityProvider`: **0 hits**.

The only “liquidity provider” strings in that tree live under `MetaTrader5SDK\Include\` Ultency types. Those are **MT5 server-side Ultency LP APIs**. They must not be mapped onto Pepperstone account `1369850` or demo account `5328266`. Owned YoPips wrapper `src\core\mt5_manager.cpp` enumerates **groups/users**, not cTrader.

Product C# under `D:\Prop\src` `*.cs`: **0** hits for `LiquidityProvider` and **0** word-`LP` identifiers. Docs and the `.env` comment carry the prohibition only.

When the destination entity is added, the name is **`ExecutionVenue` / `execution_venues`**. Never `Lp`, `LiquidityProvider`, or `lp_account`. That table is still **absent**.

**Trap:** Starwave census group `Starwave\real\FX3\LP` (2 accounts) is a **source MT5 group name**. It is not a cTrader LP type.

---

## 2. TargetCompID `cServer` — case preserved

### 2.1 Why case is a real law

Official **credentials form** (Help screenshot, R030):

```text
TargetCompID: cServer
```

Official **RoE** (`https://help.ctrader.com/fix/specification/`, re-fetched this slot):

> Tag 56 TargetCompID — “A message target. The valid value is `CSERVER`.”

Architecture §26 item 4 (L1101):

> never silently change case such as `cServer` to `CSERVER` unless the issued configuration/spec requires it

Lab issued form (names only; password not copied):

```env
CTRADER_FIX_QUOTE_TARGET_COMP_ID=cServer
CTRADER_FIX_TRADE_TARGET_COMP_ID=cServer
```

`CSERVER` is allowed only as an **explicit, logged override**. It must not be the silent compiled default.

### 2.2 Live path (measured this pass)

| Surface | Literal | Used on wire? |
|---|---|---|
| `CTraderFixOptions.QuoteFixOptions.TargetCompId` default L49 | `"cServer"` | POCO default; type **not** bound in DI |
| `CTraderFixOptions.TradeFixOptions.TargetCompId` default L70 | `"cServer"` | same |
| `CTraderFixLogonHostedService` L43 | `_config["CTRADER_FIX_QUOTE_TARGET_COMP_ID"] ?? "cServer"` | **Yes — live tag-56 value for both sessions** |
| `BrokerCatalogSeed` QUOTE L88 + TRADE L102 | `TargetCompId = "cServer"` | persisted session identity |
| `DemoSeeder` FIX rows L77 / L95 | `"cServer"` | demo only (API startup does **not** call DemoSeeder) |
| `FixSimulationHarness` defaults + L155 / L187 | `"cServer"` | tests / harness |
| Dashboard `FixSessionsPage.tsx` L8 | “TargetCompID stays `cServer`” | UI copy |
| `docs/architecture.md` L22 | “TargetCompID = `cServer` (case preserved)” | law |
| Integration `SeedingAndStoreTests` L35 | `TargetCompId` Distinct **Equal `"cServer"`** | test pin |
| `tools/DemoFixTestTrade` L29 | `CTRADER_FIX_TRADE_TARGET_COMP_ID` ?? `"cServer"` | tool only; not copy hop |
| `Fix.CTrader` `ToUpper` / `ToLower` / `ToUpperInvariant` | **0 hits** | no fold |
| Product `src\` `CSERVER` | **0 hits** | issued case only |

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
        return Assemble(fields);
    }
```

Hosted service (same target string for both sessions; ports hardcoded; **no** `RealCopyEnabled` re-pin):

```40:70:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var host = _config["CTRADER_FIX_HOST"] ?? "demo-us-eqx-01.p.c-trader.com";
        var account = _config["CTRADER_FIX_ACCOUNT_ID"] ?? "5328266";
        var sender = _config["CTRADER_FIX_QUOTE_SENDER_COMP_ID"] ?? "demo.pepperstone.5328266";
        var target = _config["CTRADER_FIX_QUOTE_TARGET_COMP_ID"] ?? "cServer";
        var username = account;
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target, ...);
        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target, ...);
        ...
        _log.LogInformation(
            "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
            ...);
```

`.env` keys (this slot classified names + case only; password not copied): both `CTRADER_FIX_QUOTE_TARGET_COMP_ID` and `CTRADER_FIX_TRADE_TARGET_COMP_ID` are the ordinal string `cServer`. `EnvFile.Load` (`D:\Prop\src\Mt5\Env\EnvFile.cs` L28–38) copies those keys into process env without mutating case (`SetEnvironmentVariable(key, value)` after trim / optional quote strip only).

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

`AddTraderIntelligence` (`D:\Prop\src\Infrastructure\DependencyInjection.cs`) does **not** `Configure<CTraderFixOptions>` and does **not** `GetSection("CTraderFix")`. The hosted service reads `CTRADER_FIX_*` env keys, not this JSON. Treat the block as **DEPRECATED / unbound**. Do not “fix” live case by copying this leftover. Do not replace issued host `demo-us-eqx-01.p.c-trader.com` (or architecture live host) with `fix.ctrader.com`.

Older reports (B27 / C09 / C21) that said “HEAD still `CSERVER` in `CTraderFixOptions`” are **stale vs today’s worktree**: both option defaults are `"cServer"` on disk now (D26). This slot re-read L49 and L70.

---

## 3. Ports 5211 QUOTE and 5212 TRADE — SSL

### 3.1 Official numbers

Official credentials form (R030 / Help `getting-fix-api-0.png`):

| UI block | Port line | Qualifier on same screenshot |
|---|---|---|
| Price Connection | **5211 (SSL)**, 5201 (plain) | SenderSubID `QUOTE` |
| Trade Connection | **5212 (SSL)**, 5202 (plain) | SenderSubID `TRADE` |

Official RoE Connectivity section does **not** publish a global hostname or port. FAQ: check **your** host/port. This lab’s issued host is currently `demo-us-eqx-01.p.c-trader.com` (architecture §25 still lists live `live-us-eqx-01.p.c-trader.com`).

Architecture §25 production transport (L1065–1071):

```text
QUOTE = 5211
TRADE = 5212
```

Plain 5201/5202 must not be the production default.

### 3.2 Product (measured this pass)

| Location | QUOTE | TRADE | TLS |
|---|---:|---:|---|
| `CTraderFixOptions.Quote/Trade.SslPort` L43 / L61 | **5211** | **5212** | `UseSsl = true` L26 |
| Same POCO `PlainPort` | 5201 | 5202 | not production default |
| `CTraderFixLogonHostedService` L49 / L55 / persist L102 | hardcoded **5211** | hardcoded **5212** | **always** `SslStream` TLS 1.2 \| 1.3 |
| `.env` `CTRADER_FIX_*_SSL_PORT` | 5211 | 5212 | unread by hosted service; matches hardcoded |
| `BrokerCatalogSeed` L86 / L100 | 5211 | 5212 | identity only |
| `DemoSeeder` L75 / L93 | 5211 | 5212 | demo only (live CompIDs leftover) |
| Dead `appsettings.json` `CTraderFix` | 5201 | 5202 | **unbound leftover** |

`CTraderFixSession.TryLogonAsync` always:

1. `TcpClient.ConnectAsync(host, sslPort)`
2. `SslStream` + `AuthenticateAsClient` (`Tls12 | Tls13`)
3. send Logon `35=A`
4. **dispose** the socket after one read (`using` TcpClient + `await using` SslStream)

It never dials 5201/5202. Parameter name is `sslPort`. Only outbound `WriteAsync` in the **copy-path** session class is that Logon.

Header qualifier on the live path (form + RoE together):

| Tag | QUOTE | TRADE |
|---|---|---|
| 56 TargetCompID | `cServer` | `cServer` |
| 57 TargetSubID | `QUOTE` | `TRADE` |
| 50 SenderSubID | `QUOTE` (RoE: must be QUOTE when 57=QUOTE) | `TRADE` (legal originator string) |

POCO gap (do not paper over): `CTraderFixOptions.Quote/Trade.SenderSubId` default to `string.Empty`. The hosted service **does not** bind that POCO; it applies `QUOTE`/`TRADE` from env/fallback itself.

---

## 4. Fetch ALL Achiever + Starwave groups and ALL manager traders

### 4.1 Product path (no plan-group filter)

`LiveMt5Registration.CreateConnectors` builds **exactly two** native connectors: Achiever + StarwaveFX. Dummy/fake brokers are refused if real passwords are missing (`HasRealPasswords` requires both `MT5_PASSWORD` and `MT5_STARWAVEFX_PASSWORD`, not `<SECRET>`, not `(a/c`). Starwave `ProxyEnabled` is hardcoded `false` (L45).

`DealIngestionService.SyncCatalogAsync`:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`group == null` means **every group just listed**, not a plan mask.

`NativeMt5BrokerConnector.GetGroupsCore` (`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`):

1. `GroupRequestArray("*", …)` L155
2. fallback `GroupTotal` + `GroupNext` L174–180

`GetAccountsCore(null)` L189–213 walks every returned group. Per group (`ReadAccountsForGroup` L216–233):

1. `UserRequestArray`
2. fallback `UserGetByGroup` only on hard fail
3. if still empty: `UserLogins` then `UserRequestByLogins`

Grep of product ingest `*.cs` for `Take(`: **0** in `DealIngestionService`. Residual `Take(200)` = `GET /api/trades` recent reconstructed trades and `GET /api/copy/intents` (`apps/api/Program.cs` L103 / L110) — not the Manager census.

Plan-group mappings are labels, not fetch filters (`docs/architecture.md` L24).

`LiveIngestHostedService` runs that catalog for **every** registered connector. API startup seeds **broker catalog only** (`BrokerCatalogSeed`), not `DemoSeeder` dummy traders.

### 4.2 YoPips C++ (same Manager recipe, source side only)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` `GetAllGroups` uses `GroupTotal` + `GroupNext`. `GetUserLogins(group)` → SDK `UserLogins`. Prop `NativeMt5BrokerConnector` is the same recipe plus `GroupRequestArray("*")` first. Achiever on this LAN still needs HTTP `ProxySet` to allow-list `81.29.145.69` (R012). Starwave is direct.

YoPips is **not** the cTrader destination. It is the proven Manager enumeration pattern the C# connector now owns.

### 4.3 Last measured live census (2026-08-18T08:42:16Z)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` (`envLoaded=true`, note: “Passwords never written. Groups and manager logins only.”).  
Write-up: `LIVE_MANAGER_FETCH_MEASURED.md` + `CREDENTIALS_AND_COPY_STATUS.md`.

JSON header re-read and **independently re-summed** this slot:

| Broker | `connected` | `groups` | `accounts` | `openPositions` |
|---|---|---:|---:|---:|
| ACHIEVER | true | **8** | **6512** | 1506 |
| STARWAVEFX | true | **10** | **1948** | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever group-account re-sum: `2+179+4+5+4+6295+0+23 = 6512`.

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

Starwave group-account re-sum: `11+4+170+1735+22+0+0+4+0+2 = 1948`.

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

This research slot did **not** re-run the probe. Numbers above are the permanent measured dump. Logins exist in the JSON; they are not reprinted here.

---

## 5. Copy to cTrader must not send live orders yet (no loss)

### 5.1 Gates (remeasured — do not copy W87 / W127)

| Gate | Measured state this slot |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` L35 | default **`false`** (POCO unused — type not bound) |
| `DependencyInjection` L39–41 `LiveRuntimeStatus.RealCopyEnabled` | **reads** `configuration["REAL_COPY_EXECUTION_ENABLED"] == "true"` (ordinal ignore-case). **Not** a hard `false`. |
| Hosted FIX logon after sessions | **no** `_runtime.RealCopyEnabled = false` re-pin (W127 stale) |
| `.env` `REAL_COPY_EXECUTION_ENABLED` | ordinal **`true`** (name + boolean only; not a password). Architecture §41 default is `false`. |
| `/api/settings` `featureFlags.REAL_COPY_EXECUTION_ENABLED` | `runtime.RealCopyEnabled` (true after DI if env is true) |
| `/api/settings` `FEATURE_COPY_TRADING_ENABLED` | **`true`** (shadow pipeline) |
| `CopyTradingService.NewOrderSingleImplemented` | **`const false`** |
| `CopyTradingService.VenueReconciled` | **`const false`** |
| Persist `RiskDecisionRecord.AllowFixSend` | **hardcoded `false`** (L192) even if `decision.AllowFixSend` is true |
| `appsettings.json` `FeatureFlags.LiveCopyEnabled` | **false** (different name; not the wired flag) |
| `CopyTradingHostedService` | **registered** (DI L59). Calls `GenerateShadowIntentsAsync` only. Logs “Live NewOrderSingle still blocked.” |
| fix-worker L21–46 | reads `CTrader:RealCopyExecutionEnabled` default false; even if true, **logs a warning and still does not send**; stamps TRADE `LastError = "No live TRADE socket. NewOrderSingle remains off."` |
| `CTraderQuoteService` | in-memory SecurityList/MD helpers only; **not registered**; never TRADE |
| `CTraderFixSession` | Logon `35=A` only; socket disposed |

`LiveRuntimeStatus.Snapshot` copy note when armed (L42–43):

> “REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.”

Architecture §41:

```env
REAL_COPY_EXECUTION_ENABLED=false
```

allows connect / prices / recon **without** placing new real orders. TRADE up ≠ license to send.

**Honesty on the env leftover:** the worktree `.env` now **disagrees** with architecture (`true` vs `false`). DI constructs runtime **armed**. That is a flag, not a sender. `CopyTradingHostedService` waits 8s then only writes SHADOW intents. Do **not** add a copy-path `35=D` builder while this env bit is true.

### 5.2 Copy-path `35=D` builder (re-measured this pass)

Grep of product `*.{cs,cpp,h,json}` under `D:\Prop\src` and `D:\Prop\apps` for literal `35=D` / `(35, "D")` / `MsgType = "D"`: **0 hits**.

`CTraderFixSession` emits only `(35, "A")`. After one reply it **disposes** TCP/SSL. There is no keep-alive TRADE initiator, no `OrderQty`, no cancel/replace (`35=F`/`35=G`) on the copy hop.

`NewOrderSingle` strings that remain on the copy hop are comments, logs, `LastError` English, UI copy, and `MayRetryNewOrderSingle` (status math only — `NotSent`/`Rejected`; never opens a socket). `RiskEngine.AllowFixSend` is a DTO bit. It is **not** a socket write. `ShadowCopyEngine` simulates fills in memory.

`CopyTradingService.GenerateShadowIntentsAsync` live-send branch (L198) requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled`. The last two constants are **false**. Even if all four were true, the branch only sets status `LIVE_SEND_BLOCKED_UNIMPLEMENTED` — it does not open a socket.

Official RoE documents `35=D` as **Client → cTrader**. That is the future send we must **not** enable until A100 (§68) and A101 (§70) are measured PASS. Current go-live scorecards remain **0/19** and **0/14** (INDEX headline).

### 5.3 Demo-only test-trade helper (off the copy hop)

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` is a **new** residual vs older “zero sender” greps that only searched the literal `35=D`:

- L126 / L157: `Build("D", …)` → tag 35 = `D` (NewOrderSingle). Qty `38=1` market buy, then optional flatten sell.
- L42–58 gate: host must start with `demo-`; sender must start with `demo.`; refuses `live.` / `live-` / account `1369850`.
- Callers: **only** `D:\Prop\tools\DemoFixTestTrade\Program.cs`. **0** callers in `apps\` or `Infrastructure\`.
- This slot did **not** run the tool. Copy hosted services do not construct it.

So: **copy cannot send live**. A human running the demo tool against the current lab demo host **could** send a demo `35=D`. That is not the assigned copy pipeline and is not the live Pepperstone account. Do not treat “product has 0 `35=D` strings” as “the tree cannot emit MsgType D.”

### 5.4 What “copy without live loss” honestly means today

| Allowed now | Forbidden now |
|---|---|
| Manager catalog of all groups/traders | Copy-path `35=D` NewOrderSingle |
| Reconstruct / score / SHADOW / CopyIntent | `35=F` / `35=G` cancel-replace on copy hop |
| Diagnostic TLS Logon `35=A` on 5211/5212 | Enabling a copy sender because env is `true` |
| Persist FIX session rows | Treating Logon as a fill |
| Manual demo-only test tool (if invoked) | Sending against live `1369850` |

User wants copy **and** no loss. Those two cannot be delivered together **today**: live copy requires a NewOrderSingle; no-loss live copy requires A100/A101 gates that are **not PASS**. The only honest operating mode is **fetch + shadow + venue Logon/recon only**. That is how this process avoids taking a live loss.

Do **not** add a copy sender in a “research” task. Do **not** flip the flag to “match” configured sessions. Prefer flipping `.env` back to `false` in a later *ops* task, not this research slot.

`CREDENTIALS_AND_COPY_STATUS.md` still says `REAL_COPY_EXECUTION_ENABLED` is **false (forced)**. That write-up is **stale vs today’s DI** (env true is bound). The `35=D` OFF row on the copy hop remains true.

---

## 6. Header / session map (issued form, case preserved)

```text
Host (lab now)  demo-us-eqx-01.p.c-trader.com
Host (arch §25) live-us-eqx-01.p.c-trader.com
QUOTE SSL       5211     50=QUOTE  57=QUOTE
TRADE SSL       5212     50=TRADE  57=TRADE
49 SenderCompID demo.pepperstone.5328266  (lab) / live.pepperstone.1369850 (arch)
56 TargetCompID cServer          ← issued case; do not fold to CSERVER
553 Username    integer account id (not SenderCompID)
554 Password    never logged
35 copy outbound A only (CTraderFixSession)
```

Account ids `5328266` / `1369850` are venue identifiers (non-secret logins), not passwords.

---

## 7. Cross-checks (siblings; do not treat as this file)

| Sibling | What it pins |
|---|---|
| A87 / D58 | Venue ≠ LP naming law |
| A25 / A31 / A32 / R030 | Official headers, `cServer` vs `CSERVER`, 5211/5212 SSL |
| A003 / E002 / E034 / R031 / W500_RESEARCH_150 | No live send on copy hop |
| A004 / W500_RESEARCH_142 | YoPips `GetAllGroups` / `UserLogins` recipe |
| LIVE_MANAGER_FETCH_MEASURED + `LIVE_GROUPS_AND_TRADERS.json` | 18 / 8460 census |
| W500_RESEARCH_27 / 47 / 67 / 87 / 107 / 127 / 147 | Same assigned topic, earlier slots. This file is slot **167** independent re-measure. 107/127 “DI forced false / logon re-pin / live host fallback” is **stale**. 147 already noted demo host + armed flag. This slot additionally pins `Build("D")` in the **demo-only** helper. |

---

## 8. Sources read (this slot)

- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs`
- `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs`
- `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` (no QuickFIX/n)
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\apps\api\Program.cs` + `appsettings.json` (dead `CTraderFix` block)
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\web\src\pages\FixSessionsPage.tsx`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\tools\DemoFixTestTrade\Program.cs`
- `D:\Prop\docs\ctrader-fix.md`, `docs\architecture.md`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §1.6, §25–27, §41
- `D:\Prop\.env` (key names + TargetCompID case + ports + REAL_COPY boolean only; password not copied)
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` (0 cTrader FIX hits)
- Official: https://help.ctrader.com/fix/ · https://help.ctrader.com/fix/specification/ · https://help.ctrader.com/fix/getting-credentials/
- Prior measured dump: `LIVE_MANAGER_FETCH_MEASURED.md`, `LIVE_GROUPS_AND_TRADERS.json`, `CREDENTIALS_AND_COPY_STATUS.md`

---

## 9. Slot-167 close

**CONFIRMED.** Pepperstone/cTrader FIX is the **destination venue**, not an LP. Wire `TargetCompID` stays issued **`cServer`**. Production ports are **QUOTE 5211 SSL** and **TRADE 5212 SSL**. The ingest goal is **all** manager-visible Achiever + Starwave groups and traders (last measured **18 / 8460**). Copy must stay SHADOW / Logon-only: **no live orders, no live loss**. Env `REAL_COPY_EXECUTION_ENABLED=true` is bound (no logon re-pin); there is still no copy-path `35=D` builder. Residual: demo-only `CTraderFixDemoTestTrade` can emit MsgType `D` if a human runs the tool against the demo host; live account `1369850` is gated.

| JSON field | Value |
|---|---|
| slot | 167 |
| verdict | CONFIRMED |
| risk_to_capital | NONE (`SAFE_BY_ABSENCE` on copy hop; no copy `35=D` builder; demo tool off hop + live-gated) |
| evidence | Venue ≠ LP; `56=cServer` no fold; QUOTE TLS 5211 / TRADE TLS 5212; census 18/8460 re-sum; copy live send off |
