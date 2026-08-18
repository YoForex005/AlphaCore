# W500_RESEARCH_3 — Confirm `MT_RET_AUTH_MANAGER_IPBLOCK=1012` and Achiever HTTP proxy `81.29.145.69:49527`

| Field | Value |
|---|---|
| Slot | **3** |
| Agent | W500_RESEARCH_3 (senior engineer, read-only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (this pass: vendor headers + C#/C++ wrappers + live census JSON already on disk) |
| Assigned | Confirm `MT_RET_AUTH_MANAGER_IPBLOCK=1012` and that Achiever requires HTTP proxy `81.29.145.69:49527`. Goal: fetch **ALL** Achiever+Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_3.md` |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Config / `.env` edited | **No.** Keys classified PRESENT + non-secret identifiers only. |
| Secret values printed | **None.** No Manager password, no proxy user/password, no FIX password. |
| Live Manager logon this pass | **Not re-run.** Census already measured at `2026-08-18T08:42:16.8519545+00:00` by `LiveBrokerProbe`. |
| Binding law | Official MetaQuotes `MT5APIConstants.h`; architecture §§7, 8, 41, 56, 68, 70; C55 (egress IP non-secret); R012 (this LAN needs the hop); R022 (Achiever-only scope); A39 (ALL groups = manager-visible set); E002/E034 (`SAFE_BY_ABSENCE`) |

This is a **confirmation pin** for slot 3. It does not whitelist IPs, does not send `35=D`, and does not invent extra groups beyond the manager ACL.

---

## 0. Verdict (binding)

**CONFIRMED on every assigned claim.**

| Claim | Result | Class |
|---|---|---|
| `MT_RET_AUTH_MANAGER_IPBLOCK` **equals integer 1012** | **Yes** | Official SDK enum + three identical wrappers |
| Official English is “IP address unallowed for manager” | **Yes** | `MT5APIFormat.h` + PHP/NET examples |
| Achiever Manager from this LAN **requires** HTTP proxy `81.29.145.69:49527` | **Yes** | Allow-list `81.29.145.69`; this desktop egress ≠ that IP; historical **1012** with proxy disabled; live census **connected=true** after `ACHIEVER_PROXY_ENABLED=true` |
| Proxy type is **HTTP** (`PROXY_HTTP = 2`), not SOCKS5 | **Yes** | Vendor `MT5APIManager.h`; C# `ApplyProxy` hard-codes `PROXY_HTTP` |
| StarwaveFX uses the same hop | **No** | `ProxyEnabled = false`; no Starwave whitelist |
| Fetch **ALL** Achiever + Starwave groups | **Implemented + measured** | `GroupRequestArray("*")` then cache fallback; live **8 + 10 = 18** |
| Fetch **ALL** manager traders (not `demo\Maxmaster` only) | **Implemented + measured** | `GetAccountsAsync(null)` walks every group; live **6512 + 1948 = 8460** |
| Copy to cTrader may send live orders today | **No** | `RealCopyEnabled=false`; product C# has **0** `35=D` builders |
| Risk to capital from catalog fetch / this slot | **None** | Read-only Manager APIs + FIX Logon `35=A` only |

Honest one-liner:

```text
1012 = MT_RET_AUTH_MANAGER_IPBLOCK.
This LAN must ProxySet PROXY_HTTP 81.29.145.69:49527 for Achiever or Connect returns 1012.
Live census already pulled all manager-visible groups/traders (18 / 8460).
NewOrderSingle stays off — SAFE_BY_ABSENCE — so copy cannot lose capital yet.
```

Do **not** treat `demo\Maxmaster` as the universe (it is **absent** from the live Achiever dump). Do **not** enable `REAL_COPY_EXECUTION_ENABLED`. Do **not** point Starwave at `:49527`.

---

## 1. `MT_RET_AUTH_MANAGER_IPBLOCK = 1012` (measured, not folklore)

### 1.1 Official MetaQuotes enum

Same literal in **three** owned trees (YoPips vendor, Prop vendor, Prop C++ wrapper). Adjacent codes 1011 / 1013 prove this is not a collision with `ACTION_ACCOUNT_ARCHIVE=1012` or leverage condition 1012 in other headers.

```46:46:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIConstants.h
   MT_RET_AUTH_MANAGER_IPBLOCK  =1012,    // IP address unallowed for manager
```

```46:46:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIConstants.h
   MT_RET_AUTH_MANAGER_IPBLOCK  =1012,    // IP address unallowed for manager
```

Format table (same meaning, same enum):

```125:125:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIFormat.h
      { MT_RET_AUTH_MANAGER_IPBLOCK,  L"IP address unallowed for manager" },
```

Vendor examples (not product, but same constant):

| File | Binding |
|---|---|
| `MetaTrader5SDK\Examples\Web\PHP\mt5_api\mt5_retcode.php:42` | `const MT_RET_AUTH_MANAGER_IPBLOCK =1012;` |
| `MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\MTRetCode.cs:46` | `MT_RET_AUTH_MANAGER_IPBLOCK = 1012` |

**Do not confuse** other `=1012` tokens in automation/dataset/VPS headers. Those are **field/action IDs**, not Manager retcodes. Auth-family neighbors are 1011 `MT_RET_AUTH_MANAGER_NOCONFIG` and 1013 `MT_RET_AUTH_GROUP_INVALID`.

### 1.2 Product mappings (both C++ backends + C# native connector)

YoPips / Prop C++ wrapper (identical text):

```61:68:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
static std::string mt5ErrorReason(MTAPIRES code) {
    switch (code) {
        case 7:    return "Network timeout (MT_RET_ERR_NETWORK). MT5 server unreachable - check proxy/firewall and MT5 server IP whitelist.";
        case 1012: return "IP blocked by MT5 server (MT_RET_AUTH_MANAGER_IPBLOCK). Ask MT5 server admin to whitelist this machine's IP.";
        case 5:    return "No connection to MT5 server (MT_RET_ERR_NOCONNECT). Server may be offline or wrong address configured.";
        case 3:    return "Wrong credentials (MT_RET_AUTH_MANAGER_FAILED). Check MT5 manager login/password in config.";
```

```64:64:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
        case 1012: return "IP blocked by MT5 server (MT_RET_AUTH_MANAGER_IPBLOCK). Ask MT5 server admin to whitelist this machine's IP.";
```

C# native connector (Achiever-specific operator hint; same integer):

```443:454:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
        var hint = (int)code switch
        {
            7 => "network/timeout — check proxy, firewall, server",
            1012 => "manager IP blocked — Achiever requires the whitelist HTTP proxy",
            3 => "params/auth — check manager login",
            5 => "disk/no-connect in some builds — server unreachable",
            10 => "no connection",
            9 => "timeout",
            _ => code.ToString()
        };
        return $"{op} failed: {(int)code} {code} ({hint})";
```

Connect is **fail-closed**: if pump `Connect` is not `MT_RET_OK`, it retries `PUMP_MODE_NONE`; if that is also not OK it throws `InvalidOperationException` with the 1012 string. It does **not** invent groups or fall back to `FakeMt5BrokerConnector`.

**What 1012 is not:** TCP closed, wrong password (that is **3**), timeout (**7**), or “broker has zero groups.” R012 measured `57.128.141.65:443` **OPEN** from this LAN while local Connect with proxy disabled still returned **1012**. Reachability ≠ allow-list.

---

## 2. Achiever requires HTTP proxy `81.29.145.69:49527` (this LAN)

### 2.1 Two non-secret keys, one hop

`81.29.145.69` is **not a secret** (C55). It is used as two keys:

| Key | Value | Role | Failure if wrong |
|---|---|---|---|
| `ACHIEVER_EGRESS_IP` | `81.29.145.69` | Source IP Achiever’s Manager ACL must see. **Not** a `Connect` argument. | **1012** |
| `ACHIEVER_PROXY_HOST` | `81.29.145.69` | HTTP CONNECT listener | Cannot present the allow-list from this desktop |
| `ACHIEVER_PROXY_PORT` | `49527` | Listener port. **Not** Manager `:443`. | Same |
| Type | `PROXY_HTTP = 2` | Manager `ProxySet` HTTP (incl. NTLM) | SOCKS5 is the wrong type |

Vendor enum:

```80:83:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
      PROXY_SOCKS4   =0,                     // SOCKS4
      PROXY_SOCKS5   =1,                     // SOCKS5
      PROXY_HTTP     =2,                     // HTTP (including NTLM)
```

Architecture §7 (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 379–393): required whitelisted outbound IP is the literal `81.29.145.69`. Same section: `demo\Maxmaster` is **not** the only group; the system must dynamically enumerate **all groups accessible to the Manager login**.

Architecture §56 sample (lines 2047–2069): `ACHIEVER_EGRESS_IP=81.29.145.69`, `ACHIEVER_PROXY_ENABLED=true`, `ACHIEVER_PROXY_HOST=81.29.145.69`, `ACHIEVER_PROXY_PORT=49527`, and `MT5_STARWAVEFX_PROXY_ENABLED=false`. Proxy username/password in that sample are `<SECRET>` placeholders only.

Catalog seed (non-secret host/port only; live `ProxySet` still comes from env):

```27:29:D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs
                ProxyEnabled = true,
                ProxyHost = "81.29.145.69",
                ProxyPort = 49527,
```

### 2.2 C# applies the hop **only** on Achiever, **only** when the flag is true

```30:46:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
            ProxyEnabled = bool.TryParse(config["ACHIEVER_PROXY_ENABLED"], out var pe) && pe,
            ProxyHost = config["ACHIEVER_PROXY_HOST"],
            ProxyPort = int.TryParse(config["ACHIEVER_PROXY_PORT"], out var pp) ? pp : 0,
            ProxyUser = config["ACHIEVER_PROXY_USERNAME"],
            ProxyPassword = config["ACHIEVER_PROXY_PASSWORD"],
…
            ProxyEnabled = false,   // StarwaveFX — do not reuse Achiever hop
```

```115:129:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
        if (_manager is null || !_opt.ProxyEnabled || string.IsNullOrWhiteSpace(_opt.ProxyHost))
            return;
        var proxy = new MTProxyInfo
        {
            enable = 1,
            type = MTProxyInfo.Type.PROXY_HTTP,
            address = $"{_opt.ProxyHost}:{_opt.ProxyPort}",
            auth = string.IsNullOrEmpty(_opt.ProxyUser) ? "" : $"{_opt.ProxyUser}:{_opt.ProxyPassword}"
        };
        var set = _manager.ProxySet(proxy);
```

Missing / false / typo `ACHIEVER_PROXY_ENABLED` → `ApplyProxy` returns immediately → **direct** `Connect` to `57.128.141.65:443` → **1012** on this workstation.

C++ `Connect` reapplies `ProxySet` before logon when `m_proxyConfig.enabled` (`mt5_manager.cpp` 76–95, both YoPips and Prop trees). That path is **Achiever-relevant**; it is **not** wired for Starwave in C# registration.

### 2.3 This desktop is not the allow-list (R012, measured)

| Fact | Measured value | Source |
|---|---|---|
| Achiever allow-list / proxy host | `81.29.145.69` | architecture §7 / C55 / seed |
| This host public egress (no proxy) | `106.219.132.213` | R012: ipify / ifconfig.me / icanhazip, all 200 |
| Equal? | **No** | R012 |
| `57.128.141.65:443` | **OPEN** 309 ms | R012 SYN-only |
| `81.29.145.69:49527` | **OPEN** 199 ms | R012 SYN-only |
| `81.29.145.69:443` / `:80` | REFUSED / TIMEOUT | R012 — hop is **:49527**, not 443 |
| `84.201.6.142:443` (Starwave) | **OPEN** 189 ms | R012; no whitelist |
| Process `ACHIEVER_PROXY_*` / `HTTP_PROXY` at R012 measure | **UNSET** | R012; keys live in `.env` |
| Historical YoPips local, proxy disabled in-process | **1012** pump **and** no-pump | `runtime-backend-3001.out.log` 2026-07-16T15:49:48 and four later local logs |

YoPips `.env` (secrets redacted; R012): `MT5_MODE=local`, `MT5_PROXY_TYPE=HTTP`, `MT5_PROXY_ADDRESS=81.29.145.69`, `MT5_PROXY_PORT=49527`, `MT5_PROXY_ENABLED=true`. Binding bug: C++ `AppConfig` reads **`IS_MT5_PROXY_ENABLED`** (absent) so those local starts logged `MT5 proxy mode: DISABLED (global)` then **1012**. That is the strongest empirical proof the hop is required **here**, not decorative.

Prop `.env` (gitignored; R012 SHA `556ACAA9…`): `ACHIEVER_PROXY_ENABLED=true`, host/port as above, proxy user/password **PRESENT** (redacted). C# `bool.TryParse("true")` succeeds. `HTTP_PROXY` must stay **unset** (R022): a process-wide proxy would steal Starwave and FIX onto the Achiever hop.

**Scope pin (R022):** Achiever Manager **YES**. Starwave Manager **NO**. cTrader FIX **NO**. NuGet / Postgres / Vite **NO**.

A host whose default SNAT **is already** `81.29.145.69` would not need the HTTP hop (A54 preference 1). **`DESKTOP-FQPFPKE` is not that host.**

---

## 3. Goal: fetch ALL groups and ALL manager traders

### 3.1 Law

Architecture §7: `demo\Maxmaster` is **not** the only group. Enumerate every group the Manager login can see.

A39: ALL = `IMTManagerAPI` group list after the server applied the manager ACL. Plan-map / `MT5_GROUP_*` / default group are **write-path subsets**, not enumerators.

### 3.2 C# implementation (current worktree; A001 “zero GroupRequestArray hits” is stale)

Pump includes groups + users so the cache is warm, then **request** APIs so a no-pump fallback is still complete:

```88:101:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            var pump = CIMTManagerAPI.EnPumpModes.PUMP_MODE_GROUPS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_USERS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_POSITIONS;
            var res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, pump, 30000);
            …
            res = _manager.Connect(..., PUMP_MODE_NONE, 30000);
```

```155:183:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
                var res = _manager.GroupRequestArray("*", arr);
                …
            if (list.Count == 0)
            {
                var total = _manager.GroupTotal();
                for (uint i = 0; i < total; i++)
                    … GroupNext …
```

`GetAccountsAsync(null)` walks **every** group from that list, then per group:

1. `UserRequestArray(gname)`
2. else `UserGetByGroup`
3. else `UserLogins` + `UserRequestByLogins`
4. plus `UserAccountRequestArray` / `UserAccountGetByGroup` for balance/equity

`DealIngestionService.SyncCatalogAsync` calls `GetGroupsAsync` + `GetAccountsAsync(null)` — no `demo\Maxmaster` filter, no `Take(200)`.

`LiveIngestHostedService` iterates **every** registered connector (Achiever **and** Starwave), catalog first, then deals/positions/score. Dummy data is **not** substituted on failure.

`LiveBrokerProbe` (`D:\Prop\tools\LiveBrokerProbe\Program.cs`) uses the same `CreateConnectorsFromEnvironment()` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`. It writes logins only. **No** `DealerSend`, **no** `TradeRequest`, **no** `35=D`.

C++ YoPips `MT5Manager::GetAllGroups` is `GroupTotal`/`GroupNext` only (`mt5_manager.cpp` 962–981). It does **not** call `GroupRequestArray`. After a no-pump connect that cache can be empty. **C# is the complete enumerator** for this goal. Do not run a C++ probe with the wrong proxy key and declare “zero groups.”

### 3.3 Live census (already on disk — do not greenwash as “this process reconnected”)

Probe: `LiveBrokerProbe`  
UTC: `2026-08-18T08:42:16.8519545+00:00`  
File: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Pin: `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`

| Broker | Connect | Groups | Traders | Open positions | Path |
|---|---|---:|---:|---:|---|
| ACHIEVER | `connected: true` (7213 ms) | **8** | **6512** | 1506 | HTTP proxy + `GroupRequestArray("*")` + `UserRequestArray` |
| STARWAVEFX | `connected: true` (6413 ms) | **10** | **1948** | 478 | **direct** (no proxy) |
| **Total** | both | **18** | **8460** | 1984 | manager-visible set only |

Achiever groups (sum of per-group counts = 6512):

| Group | Accounts |
|---|---:|
| `contest\yo-1step` | 2 |
| `contest\yo-2step` | 179 |
| `contest\yo-instant` | 4 |
| `contest\yo-payp` | 5 |
| `demo\yo-1step` | 4 |
| `demo\yo-2step` | 6295 |
| `demo\yo-instant` | 0 |
| `demo\yo-payp` | 23 |

Starwave groups (sum = 1948):

| Group | Accounts |
|---|---:|
| `Starwave\cent\FX1\grp1` | 11 |
| `Starwave\cent\FX1\grp2` | 4 |
| `Starwave\demo\FX2\grp1` | 170 |
| `Starwave\demo\FX2\grp2` | 1735 |
| `Starwave\real\FX3\grp1` | 22 |
| `Starwave\real\FX3\grp2` | 0 |
| `Starwave\real\FX3\grp3` | 0 |
| `Starwave\real\FX3\grp4` | 4 |
| `Starwave\real\FX3\grp5` | 0 |
| `Starwave\real\FX3\LP` | 2 |

**`demo\Maxmaster` is not in the live dump** (`grep` of `LIVE_GROUPS_AND_TRADERS.json`: **0** hits). A fetch limited to the architecture default group would have returned **zero** Achiever traders. That is why slot 3’s “ALL groups” rule is not optional.

Honesty bound: this is **every group these two manager logins may see**. Server groups outside the manager ACL are invisible by design (A39). Empty-account groups (`demo\yo-instant`, three Starwave real grps) **were still listed** — enumerator is not “groups with balance > 0.”

---

## 4. Copy to cTrader must not send live orders (no loss)

Goal conjunction: **fetch the desk** and **do not lose capital**. Those are not the same as “turn on live copy.”

### 4.1 Flags pinned false

| Surface | Value |
|---|---|
| `DependencyInjection.AddTraderIntelligence` | `RealCopyEnabled = false` — comment: “Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.” |
| `CTraderFixLogonHostedService` after optional logon | `_runtime.RealCopyEnabled = false` |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** (“Default OFF”) |
| `D:\Prop\.env` `REAL_COPY_EXECUTION_ENABLED` | `false` (key present; this pass does not print other `.env` values) |
| `/api/settings` | `featureFlags.REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled` (false) **and** `FEATURE_COPY_TRADING_ENABLED = false` |
| `apps/fix-worker` | `GetValue("CTrader:RealCopyExecutionEnabled", false)`; even if true, worker only logs a warning and stamps TRADE `NewOrderSingle remains off` |

Runtime snapshot copy note when false (`LiveRuntimeStatus.cs` 42–43):

> `NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.`

### 4.2 No `35=D` builder (`SAFE_BY_ABSENCE`)

`CTraderFixSession.BuildLogon` emits tag **35=`A`** only (fields 34/49/56/50/57/52/98/108/141/553/554). The only socket write in `Fix.CTrader` is that Logon.

E034 (83 product `*.cs`): literal `35=D` / `(35, "D")` / `MsgType = "D"` = **0**. `NewOrderSingle` appears only as comments, `LastError` English, and `MayRetryNewOrderSingle` (status math). `GuardedNewOrderSingle` **MISSING**.

`grep` this pass on `D:\Prop\src\Fix.CTrader`: `NewOrderSingle` / `35=D` = **2** hits, both non-senders (XML comment + log line).

`grep` this pass on `D:\Prop\src` for `DealerSend` / `TradeRequest` / `OrderAdd`: **no order-send APIs**. `PositionCreateArray` is a **read** buffer for `PositionRequest`.

Seeded TRADE `LastError`: `session up for logon/recon only; NewOrderSingle off`.

### 4.3 What is allowed now vs forbidden

| Action | Allowed for slot-3 goal? | Why |
|---|---|---|
| Achiever `Connect` via HTTP `81.29.145.69:49527` | **Yes** | Else 1012 |
| Starwave `Connect` direct | **Yes** | No whitelist |
| `GroupRequestArray("*")` / `UserRequestArray` / deal+position **request** | **Yes** | Read-only Manager |
| Persist catalog / scores / shadow rows | **Yes** | Local DB; not a venue send |
| FIX TLS Logon `35=A` QUOTE/TRADE | **Session only** | Not an order |
| `NewOrderSingle` `35=D`, cancel `35=F`, replace `35=G` | **Forbidden** | No builder; flag false; A100 0/19; A101 0/14 |
| `REAL_COPY_EXECUTION_ENABLED=true` | **Forbidden** | Would not send today, still must not be armed |

**Copy to cTrader is the destination. No-loss is the constraint. The constraint wins.** Current safety is **`SAFE_BY_ABSENCE`**, not a unit-tested refuse-on-LoggedOn-TRADE gate. Do not tick §68 / §70 from this file.

---

## 5. Operator pin (slot 3)

1. Treat **1012** as `MT_RET_AUTH_MANAGER_IPBLOCK`. Fix **routing / `ProxySet` / ACL**, not the manager password (password fail is **3**).
2. On this LAN, Achiever **must** `ProxySet(PROXY_HTTP, 81.29.145.69:49527, user, pass)` **before** `Connect(57.128.141.65:443, 2027, …)`. Load `ACHIEVER_PROXY_ENABLED=true` (C# name). Do not export `HTTP_PROXY`.
3. Starwave stays **direct** to `84.201.6.142:443` login `9904`. A Starwave 1012 is unexpected; do **not** “fix” it with the Achiever hop.
4. Enumerate with `GroupRequestArray("*")` + `GetAccountsAsync(null)`. Do not hard-code `demo\Maxmaster`. Live measured set is **18 groups / 8460 traders**.
5. Keep `RealCopyEnabled=false`. Do not add `35=D`. Fetch + score + shadow only.

---

## 6. What this agent did **not** do

- Did not print or copy `MT5_PASSWORD`, `MT5_STARWAVEFX_PASSWORD`, `ACHIEVER_PROXY_USERNAME`, `ACHIEVER_PROXY_PASSWORD`, `CTRADER_FIX_PASSWORD`.
- Did not call `Connect`, `ProxySet`, or TCP SYN this pass (reused R012 + `LIVE_GROUPS_AND_TRADERS.json`).
- Did not edit `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `D:\Prop\mt5-sdk`, either `.env`, or YoPips source.
- Did not enable live copy, flatten, or write FIX `35=D/F/G`.
- Did not claim EX5 decompile progress or ≥95% copy parity.

---

## 7. Checklist

- [x] `MT_RET_AUTH_MANAGER_IPBLOCK=1012` confirmed in official SDK + YoPips C++ + Prop C++ + Prop C#.
- [x] Achiever HTTP proxy `81.29.145.69:49527` confirmed required on this LAN; type HTTP; Starwave excluded.
- [x] ALL groups / ALL manager traders: implemented (`*` / `null`) and measured (8+10 / 6512+1948).
- [x] Copy-to-cTrader live send **off**; no capital path from this process.
- [x] Product source not edited. Secrets not printed.
