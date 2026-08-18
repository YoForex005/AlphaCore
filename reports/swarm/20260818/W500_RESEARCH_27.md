# W500_RESEARCH_27 — cTrader is destination venue, not LP

| Field | Value |
|---|---|
| Slot | **27** |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (this pass: product source + official RoE re-read; live census reused from `LIVE_MANAGER_FETCH_MEASURED.md` 08:42–08:45Z) |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_27.md` |
| Assigned | Confirm cTrader is **destination venue, not LP**. `TargetCompID` **`cServer`** case preserved. Ports **5211 QUOTE** and **5212 TRADE SSL**. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader **must not send live orders yet** (no loss). |
| Product source modified | **No.** Report only. |
| Secret values printed | **None.** |
| Live `35=D` sent this pass | **No.** |
| YoPips C++ tree | `D:\Projects\YoPips\Backend\C++ Backend PropFirm` — source MT5 Manager only; **no** cTrader FIX destination. |

**Honesty rule:** wanting live copy *and* no loss does not make either true. A TLS Logon (`35=A`) is not a NewOrderSingle. Official RoE table spelling `CSERVER` is not a license to silently fold the issued form `cServer`. A Starwave **source group** named `Starwave\real\FX3\LP` is not evidence that Pepperstone/cTrader is an LP.

---

## 0. Verdict

**CONFIRMED on the live path. cTrader/cServer is the destination execution venue, not an LP. Issued `TargetCompID=cServer` is preserved (no `ToUpper`). Production transport is TLS QUOTE 5211 + TRADE 5212. Catalog fetch is ALL manager-visible groups/traders. Live `35=D` is impossible today (`SAFE_BY_ABSENCE`).**

| Claim | Measured | Class |
|---|---|---|
| cTrader is destination venue, **not** LP | **Yes.** Architecture §1.6 / §25; `docs/ctrader-fix.md`; product C# has **0** `LP`/`LiquidityProvider` identifiers on the destination | **CONFIRMED** |
| `TargetCompID` `cServer` case preserved | **Yes** on live defaults, seed rows, hosted-service fallback, harness, UI copy. **No** `ToUpper` in `Fix.CTrader` | **CONFIRMED (live path)** |
| QUOTE SSL **5211** / TRADE SSL **5212** | **Yes.** Options defaults + hosted service hardcodes + seed + official credentials screenshot | **CONFIRMED** |
| Fetch ALL Achiever + Starwave groups + ALL manager traders | **Implemented** (`GetGroupsAsync` + `GetAccountsAsync(null)`). **Previously measured** 8+10 groups / 6512+1948 traders | **CONFIRMED (code + 2026-08-18 census)** |
| Copy to cTrader must not send live orders yet | **Yes.** `RealCopyEnabled` forced `false`. `35=D` builder **absent**. Logon sends only `35=A` then disposes the socket | **CONFIRMED — `SAFE_BY_ABSENCE` / no capital at risk** |

**Leftovers (do not greenwash):**

1. Official RoE *table* still prints tag 56 valid value `CSERVER`. Architecture §26 forbids silently changing issued `cServer` → `CSERVER`. Live code keeps `cServer`. `CSERVER` is legal only as an explicit operator override.
2. Dead leftover `D:\Prop\apps\api\appsettings.json` `CTraderFix.TargetCompId = "CSERVER"` and plain ports **5201/5202** / host `fix.ctrader.com`. **Not bound.** Live logon does **not** read that JSON block.
3. `CTraderFixOptions` is **not** registered (`no Configure<CTraderFixOptions>`). Live logon reads env keys + hardcoded ports.
4. Architecture table `execution_venues` is still **unbuilt**. Absence of the word LP ≠ venue entity exists.
5. This pass did **not** re-attach Manager or re-open TLS. Census numbers below are the 2026-08-18 measured dump, not a new probe.

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

Architecture `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §1 item 6:

> **Do not call the cTrader account an LP unless it actually is your contractual LP relationship.**
> Technically this architecture treats Pepperstone/cServer FIX as the **external execution venue**. The software must not assume institutional LP semantics that the account does not provide.

§25 title:

> **# 25. New Execution Venue: cTrader / cServer FIX 4.4**
>
> Real approved copy trades will route to the provided Pepperstone cTrader account through cServer FIX 4.4.

`D:\Prop\docs\ctrader-fix.md` line 5:

> cTrader is used as a **hedging execution venue** — not a liquidity provider. The prop firm's challenge accounts run on MT5; winning trades are copied to cTrader for real-money hedging via FIX 4.4 protocol (QuickFIX/N engine).

Official Help (`https://help.ctrader.com/fix/`, re-fetched this pass) describes FIX as a **client → cTrader** session (Logon, quotes, NewOrderSingle). That is a **broker execution gateway**, not an institutional LP book.

### 1.2 Source vs destination (this product)

| Side | What it is | What it is not |
|---|---|---|
| Achiever MT5 (`57.128.141.65:443`, manager 2027, HTTP proxy) | **Source** challenge book | Not the hedge account |
| StarwaveFX MT5 (`84.201.6.142:443`, manager 9904, direct) | **Source** challenge book | Not the hedge account |
| Pepperstone / cServer FIX (`live-us-eqx-01.p.c-trader.com`, account `1369850`) | **Destination execution venue** | **Not** an LP |

YoPips `D:\Projects\YoPips\Backend\C++ Backend PropFirm` is the **source** Manager/HTTP prop backend. Grep of that `src\` tree for `cTrader` / `cServer` / `5211` / `5212` / `NewOrderSingle` returned **no venue hits** (only unrelated “FIX #N” comments). cTrader is **not** wired there.

### 1.3 Product naming scan (this pass)

| Tree | `LP` / `LiquidityProvider` applied to cTrader | Result |
|---|---|---|
| `D:\Prop\src` product C# | **0** | PASS |
| Prior D58 product `*.cs` / `*.ts` / `appsettings` | **0** identifiers | PASS |
| `docs/ctrader-fix.md` | “not a liquidity provider” | prohibition, correct |
| Architecture §1.6 | “Do not call the cTrader account an LP” | prohibition, correct |
| Starwave group `Starwave\real\FX3\LP` | **source** MT5 group name in the 2026-08-18 census | **not** a cTrader LP type |

A87 / D58 remain correct: do not name the destination `Lp` / `LiquidityProvider`. When the table is added, the name is `execution_venues`. That table is still **absent**.

---

## 2. TargetCompID `cServer` — case preserved

### 2.1 Why case is a real law

Official **credentials form** screenshot (`https://help.ctrader.com/fix/img/getting-fix-api-0.png`, pinned by R030 / A31):

```text
TargetCompID: cServer
```

Official **RoE** standard header (`https://help.ctrader.com/fix/specification/`, re-fetched this pass):

> Tag 56 TargetCompID — “A message target. The valid value is `CSERVER`.”

Official Logon **example** uses `56=CSERVER`. Official Python sample config uses `cServer`. Architecture §26 item 4:

> never silently change case such as `cServer` to `CSERVER` unless the issued configuration/spec requires it

This lab’s issued form + architecture env sample:

```env
CTRADER_FIX_QUOTE_TARGET_COMP_ID=cServer
CTRADER_FIX_TRADE_TARGET_COMP_ID=cServer
```

`CSERVER` is allowed only as an **explicit, logged override**. It must not be the silent compiled default.

### 2.2 Live path (measured this pass)

| Surface | Literal | Used on wire? |
|---|---|---|
| `CTraderFixOptions.QuoteFixOptions.TargetCompId` default | `"cServer"` | POCO default; type **not** bound in DI |
| `CTraderFixOptions.TradeFixOptions.TargetCompId` default | `"cServer"` | same |
| `CTraderFixLogonHostedService` fallback | `_config["CTRADER_FIX_QUOTE_TARGET_COMP_ID"] ?? "cServer"` | **Yes — this is the live tag-56 value** |
| `BrokerCatalogSeed` QUOTE + TRADE rows | `TargetCompId = "cServer"` | persisted session identity |
| `DemoSeeder` FIX rows (not API startup) | `"cServer"` | demo only |
| `FixSimulationHarness` defaults | `"cServer"` | tests / harness |
| Dashboard `FixSessionsPage.tsx` | “TargetCompID stays `cServer`” | UI copy |
| `docs/architecture.md` | “TargetCompID = `cServer` (case preserved)” | law |
| `Fix.CTrader` `ToUpper` / `ToLower` on CompID | **0 hits** | no fold |

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
            ...
        };
```

Hosted service:

```40:57:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var host = _config["CTRADER_FIX_HOST"] ?? "live-us-eqx-01.p.c-trader.com";
        var account = _config["CTRADER_FIX_ACCOUNT_ID"] ?? "1369850";
        var sender = _config["CTRADER_FIX_QUOTE_SENDER_COMP_ID"] ?? "live.pepperstone.1369850";
        var target = _config["CTRADER_FIX_QUOTE_TARGET_COMP_ID"] ?? "cServer";
        ...
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target, ...);
        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target, ...);
```

### 2.3 Dead leftover (must not be treated as live)

`D:\Prop\apps\api\appsettings.json` still has:

```json
"CTraderFix": {
  "QuoteHost": "fix.ctrader.com",
  "QuotePort": 5201,
  "TradeHost": "fix.ctrader.com",
  "TradePort": 5202,
  "TargetCompId": "CSERVER"
}
```

`AddTraderIntelligence` does **not** `Configure<CTraderFixOptions>` and does **not** `GetSection("CTraderFix")`. The hosted service reads `CTRADER_FIX_*` env keys, not this JSON. Treat the block as **DEPRECATED / unbound**. Do not “fix” live case by copying this leftover.

Older reports (B27 / C09 / C21 / D26) that said “HEAD still `CSERVER` in `CTraderFixOptions`” are **stale vs today’s worktree**: both option defaults are `"cServer"` on disk now.

---

## 3. Ports 5211 QUOTE and 5212 TRADE — SSL

### 3.1 Official numbers

Official credentials screenshot (A31 / R030, Help `getting-fix-api-0.png`):

| UI block | Port line | Qualifier on same screenshot |
|---|---|---|
| Price Connection | **5211 (SSL)**, 5201 (plain) | SenderSubID `QUOTE` |
| Trade Connection | **5212 (SSL)**, 5202 (plain) | SenderSubID `TRADE` |

Official Spotware C# sample uses `_pricePort = 5211`, `_tradePort = 5212` and wraps both in `SslStream.AuthenticateAsClient`.

Official RoE Connectivity section does **not** publish a global hostname or port. FAQ: check **your** host/port. This lab’s issued host remains `live-us-eqx-01.p.c-trader.com`. Do not replace it with `fix.ctrader.com`.

Architecture §25 production transport:

```text
QUOTE = 5211
TRADE = 5212
```

Plain 5201/5202 must not be the production default.

### 3.2 Product (measured this pass)

| Location | QUOTE | TRADE | TLS |
|---|---:|---:|---|
| `CTraderFixOptions.Quote/Trade.SslPort` | **5211** | **5212** | `UseSsl = true` |
| Same POCO `PlainPort` | 5201 | 5202 | not production default |
| `CTraderFixLogonHostedService` | hardcoded **5211** | hardcoded **5212** | **always** `SslStream` TLS 1.2 \| 1.3 |
| `BrokerCatalogSeed` / `DemoSeeder` session rows | 5211 | 5212 | identity only |
| Dead `appsettings.json` `CTraderFix` | 5201 | 5202 | **unbound leftover** |

`CTraderFixSession.TryLogonAsync` always:

1. `TcpClient.ConnectAsync(host, sslPort)`
2. `SslStream` + `AuthenticateAsClient` (`Tls12 | Tls13`)
3. send Logon
4. **dispose** the socket after one read

It never dials 5201/5202.

Header qualifier on the live path (form + RoE together):

| Tag | QUOTE | TRADE |
|---|---|---|
| 56 TargetCompID | `cServer` | `cServer` |
| 57 TargetSubID | `QUOTE` | `TRADE` |
| 50 SenderSubID | `QUOTE` (RoE: must be QUOTE when 57=QUOTE) | `TRADE` (legal originator string) |

---

## 4. Fetch ALL Achiever + Starwave groups and ALL manager traders

### 4.1 Product path (no plan-group filter)

`LiveMt5Registration.CreateConnectors` builds **exactly two** native connectors: Achiever + StarwaveFX. Dummy/fake brokers are refused if real passwords are missing.

`DealIngestionService.SyncCatalogAsync`:

```44:48:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`group == null` means **every group just listed**, not a plan mask.

`NativeMt5BrokerConnector.GetGroupsCore`:

1. `GroupRequestArray("*", …)`
2. fallback `GroupTotal` + `GroupNext`

`GetAccountsCore(null)` walks every returned group. Per group:

1. `UserRequestArray`
2. fallback `UserGetByGroup`
3. if still empty: `UserLogins` then `UserRequestByLogins`

There is **no** `Take(200)` on this path. Plan-group mappings are labels, not fetch filters (`docs/architecture.md`).

`LiveIngestHostedService` runs that catalog for **every** registered connector, then deals/score for **every** stored login. `/api/ops/resync` repeats the same for `ACHIEVER` and `STARWAVEFX`. API startup seeds **broker catalog only** (`BrokerCatalogSeed`), not `DemoSeeder` dummy traders.

### 4.2 YoPips C++ (same Manager recipe, source side only)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` `GetAllGroups`:

```962:981:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetAllGroups(std::vector<std::string>& groups) {
    ...
    uint32_t total = m_manager->GroupTotal();
    ...
    for (uint32_t i = 0; i < total; i++) {
        if (m_manager->GroupNext(i, grp) == MT_RET_OK) {
            groups.push_back(StringUtils::toUtf8(grp->Group()));
        }
    }
```

`GetUserLogins(group)` → SDK `UserLogins`. Prop `mt5-sdk` is the same recipe. Achiever on this LAN still needs HTTP `ProxySet` to allow-list `81.29.145.69` (R012). Starwave is direct.

YoPips is **not** the cTrader destination. It is the proven Manager enumeration pattern the C# connector now owns.

### 4.3 Last measured live census (2026-08-18T08:42Z)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` (`envLoaded=true`, passwords never written).

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| Achiever | OK via HTTP proxy | **8** | **6512** | 1506 |
| StarwaveFX | OK direct | **10** | **1948** | 478 |
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

If the server has more groups, they are outside this manager’s permission set. That is an ACL fact, not a code cap.

This research slot did **not** re-run the probe. Numbers above are the permanent measured dump.

---

## 5. Copy to cTrader must not send live orders yet (no loss)

### 5.1 Gates that stay off

| Gate | Measured state |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** |
| `DependencyInjection` `LiveRuntimeStatus.RealCopyEnabled` | **forced `false`** (“Live NewOrderSingle is not implemented”) |
| Hosted service after logon | `_runtime.RealCopyEnabled = false` |
| `/api/settings` `featureFlags.REAL_COPY_EXECUTION_ENABLED` | `runtime.RealCopyEnabled` (false) |
| `FEATURE_COPY_TRADING_ENABLED` | hardcoded **false** |
| fix-worker | reads `CTrader:RealCopyExecutionEnabled` default false; even if true, **logs a warning and still does not send** |
| `CTraderQuoteService` | in-memory tag lists only (`35=y` / `35=V`); **not registered**; never TRADE |

`LiveRuntimeStatus.Snapshot` copy note when false:

> “NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.”

### 5.2 No `35=D` builder (re-measured this pass)

Grep of `D:\Prop\src` for `35=D` / `(35, "D")` / `MsgType = "D"`: **0 hits**.

`CTraderFixSession` emits only `(35, "A")`. After one reply it **disposes** TCP/SSL. There is no keep-alive TRADE initiator, no `OrderQty`, no cancel/replace (`35=F/G`).

`NewOrderSingle` strings that remain are comments, logs, `LastError`, and `MayRetryNewOrderSingle` (status math only). `RiskEngine.AllowFixSend` is a DTO bit. It is **not** a socket write. `ShadowCopyEngine` simulates fills in memory.

Architecture §41 / §56 allow QUOTE+TRADE sessions **on** while:

```env
REAL_COPY_EXECUTION_ENABLED=false
```

TRADE up ≠ license to send (A49 / R031).

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

---

## 7. Cross-checks (siblings; do not treat as this file)

| Sibling | What it pins |
|---|---|
| A87 / D58 | Venue ≠ LP naming law |
| A25 / A31 / A32 / R030 | Official headers, `cServer` vs `CSERVER`, 5211/5212 SSL |
| A003 / E002 / E034 / R031 | No live send; `REAL_COPY` stays false |
| A004 | YoPips `GetAllGroups` / `UserLogins` recipe |
| LIVE_MANAGER_FETCH_MEASURED + `LIVE_GROUPS_AND_TRADERS.json` | 18 / 8460 census |
| W500_SLICE_27 | Different angle (`LiveMt5Registration` has no `Take(200)`). This file is the **venue/header/no-loss** pin. |

---

## 8. Sources read (this slot)

- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\apps\api\Program.cs` + `appsettings.json` (dead `CTraderFix` block)
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\web\src\pages\FixSessionsPage.tsx`
- `D:\Prop\docs\ctrader-fix.md`, `docs\architecture.md`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §1.6, §25–27, §41, §56
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` (`GetAllGroups` / `UserLogins`)
- Official: https://help.ctrader.com/fix/ · https://help.ctrader.com/fix/specification/ · https://help.ctrader.com/fix/getting-credentials/
- Prior measured dump: `LIVE_MANAGER_FETCH_MEASURED.md`, `LIVE_GROUPS_AND_TRADERS.json`, `CREDENTIALS_AND_COPY_STATUS.md`

---

## 9. Slot-27 close

**CONFIRMED.** Pepperstone/cTrader FIX is the **destination venue**, not an LP. Wire `TargetCompID` stays issued **`cServer`**. Production ports are **QUOTE 5211 SSL** and **TRADE 5212 SSL**. The ingest goal is **all** manager-visible Achiever + Starwave groups and traders (last measured **18 / 8460**). Copy must stay SHADOW / Logon-only: **no live orders, no live loss**.
