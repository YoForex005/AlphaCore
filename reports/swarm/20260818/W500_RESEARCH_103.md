# W500_RESEARCH_103 — Confirm `MT_RET_AUTH_MANAGER_IPBLOCK=1012` and Achiever HTTP proxy `81.29.145.69:49527`

| Field | Value |
|---|---|
| Slot | **103** |
| Agent | W500_RESEARCH_103 (senior engineer, read-only) |
| Date | 2026-08-18 |
| Measured at | This pass re-read official SDK + wrappers in **both** Prop and YoPips trees; C# `ApplyProxy` / `Describe`; YoPips historical **1012** logs (`runtime-backend-3001.out.log` 2026-07-16T15:49:48); live census JSON already on disk (`2026-08-18T08:42:16.8519545+00:00`) — group counts independently re-summed |
| Assigned | Confirm `MT_RET_AUTH_MANAGER_IPBLOCK=1012` and that Achiever requires HTTP proxy `81.29.145.69:49527`. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_103.md` |
| Product source modified | **No.** Report + INDEX/SWARM_LOG only. |
| Test source modified | **No.** |
| Config / `.env` edited | **No.** Keys classified PRESENT + non-secret identifiers only. |
| Secret values printed | **None.** No Manager password, no proxy user/password, no FIX password. |
| Live Manager logon this pass | **Not re-run.** Prior measured census: `LIVE_GROUPS_AND_TRADERS.json` + `LIVE_MANAGER_FETCH_MEASURED.md`. |
| Binding law | Official MetaQuotes `EnMTAPIRetcode`; architecture §§7, 8, 56, 68, 70; C55 (egress IP non-secret); R012 (this LAN needs the hop); R022 (Achiever-only scope); A39 (ALL groups = manager-visible set); E002 (`SAFE_BY_ABSENCE`) |

This is a **slot-103 confirmation pin**. It does not whitelist IPs, does not send `35=D`, and does not invent groups outside the manager ACL.

---

## 0. Verdict (binding)

**CONFIRMED on every assigned claim.**

| Claim | Result | Evidence class |
|---|---|---|
| Official retcode `MT_RET_AUTH_MANAGER_IPBLOCK` equals **1012** | **Yes** | MetaQuotes `EnMTAPIRetcode` in Prop **and** YoPips Manager SDK. Same integer in C++ wrapper, PHP / .NET WebAPI examples, C# `Describe`. |
| Official English | “IP address unallowed for manager” | `MT5APIFormat.h` L125 + PHP/NET examples |
| Achiever Manager from this lab needs HTTP proxy **`81.29.145.69:49527`** | **Yes** | Allow-list identity `81.29.145.69` (non-secret). Type `MTProxyInfo::PROXY_HTTP = 2`. This desktop public egress measured `106.219.132.213` ≠ allow-list (R012). Historical YoPips local Connect with proxy **disabled** = **1012** on pump **and** no-pump (re-read this pass). Live census later connected Achiever **via that HTTP hop**. |
| StarwaveFX uses the same hop | **No** | `LiveMt5Registration` hard-sets `ProxyEnabled = false`. Architecture §56 `MT5_STARWAVEFX_PROXY_ENABLED=false`. R022: do **not** hairpin Starwave through `:49527`. |
| Fetch **ALL** Achiever + Starwave groups | **Implemented + measured** | `GroupRequestArray("*")` then `GroupTotal`/`GroupNext` fallback. Live **8 + 10 = 18**. |
| Fetch **ALL** manager traders | **Implemented + measured** | `GetAccountsAsync(null)` walks every group via `UserRequestArray` / `UserGetByGroup` / `UserLogins`+`UserRequestByLogins`. Live **6512 + 1948 = 8460**. |
| Copy to cTrader may send live orders today | **No** | `RealCopyEnabled` forced **false**. Product C# / JSON / csproj have **0** `35=D` builders. FIX logon is `35=A` only. |
| Risk to capital from this slot / catalog fetch | **None** | Read-only Manager APIs. 1012 is a **logon deny** (no session → no dealer mutators). Copy path is `SAFE_BY_ABSENCE`. |

Honest one-liner:

```text
1012 = MT_RET_AUTH_MANAGER_IPBLOCK (official).
Achiever local Manager on this LAN requires ProxySet PROXY_HTTP 81.29.145.69:49527.
Live census already pulled all manager-visible groups/traders (18 / 8460).
NewOrderSingle stays off — SAFE_BY_ABSENCE — so copy cannot lose capital yet.
```

Do **not** treat `demo\Maxmaster` as the universe (it is **absent** from the live Achiever dump — this-pass grep: 0 hits). Do **not** enable `REAL_COPY_EXECUTION_ENABLED`. Do **not** point Starwave at `:49527`. Do **not** confuse **1012** (manager source-IP ACL) with **1016** (`MT_RET_AUTH_INVALID_IP`, server-identity).

---

## 1. `MT_RET_AUTH_MANAGER_IPBLOCK = 1012` (official, not a house constant)

Read this pass from **both** trees. Same enum member, same integer, same comment:

```46:46:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIConstants.h
   MT_RET_AUTH_MANAGER_IPBLOCK  =1012,    // IP address unallowed for manager
```

```46:46:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIConstants.h
   MT_RET_AUTH_MANAGER_IPBLOCK  =1012,    // IP address unallowed for manager
```

Neighboring official auth codes (same `EnMTAPIRetcode` block) prove 1012 is not a typo and is not 1016:

| Code | Token | Meaning |
|---:|---|---|
| 1011 | `MT_RET_AUTH_MANAGER_NOCONFIG` | Manager account has no manager config |
| **1012** | **`MT_RET_AUTH_MANAGER_IPBLOCK`** | **IP address unallowed for manager** |
| 1013 | `MT_RET_AUTH_GROUP_INVALID` | Group not initialized |
| 1016 | `MT_RET_AUTH_INVALID_IP` | Unallowed address [check **server's** ip address] |

**1012** = source IP of the Manager API client is not on the broker’s manager access list.  
**1016** = server-identity / bind-address check. Different failure. Operators who “fix 1012 by changing `MT5_SERVER`” are solving the wrong code.

Official string table (Prop vendor copy, this-pass read):

```125:125:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIFormat.h
      { MT_RET_AUTH_MANAGER_IPBLOCK,  L"IP address unallowed for manager" },
```

Cross-language copies (still 1012; not a different family of “1012” like `IDC_BUTTON_DEPOSIT` or `CONDITION_ACCOUNT_LEVERAGE`):

| Copy | Path | Token |
|---|---|---|
| Official PHP example | `mt5_retcode.php:42` (YoPips; Prop has the same file) | `const MT_RET_AUTH_MANAGER_IPBLOCK  =1012;` |
| Official .NET WebAPI | `MTRetCode.cs:46` (YoPips + Prop) | `MT_RET_AUTH_MANAGER_IPBLOCK = 1012` |
| Prop C++ wrapper | `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp:64` | `case 1012:` → “IP blocked by MT5 server (`MT_RET_AUTH_MANAGER_IPBLOCK`)…” |
| YoPips C++ wrapper | `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp:64` | **identical** `case 1012:` string |
| Prop C# live connector | `NativeMt5BrokerConnector.cs:447` | `1012 => "manager IP blocked — Achiever requires the whitelist HTTP proxy"` |

Wrapper mapping (both C++ trees, same line number; this-pass read):

```61:68:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
static std::string mt5ErrorReason(MTAPIRES code) {
    switch (code) {
        case 7:    return "Network timeout (MT_RET_ERR_NETWORK). ...";
        case 1012: return "IP blocked by MT5 server (MT_RET_AUTH_MANAGER_IPBLOCK). Ask MT5 server admin to whitelist this machine's IP.";
        case 5:    return "No connection to MT5 server (MT_RET_ERR_NOCONNECT). ...";
        case 3:    return "Wrong credentials (MT_RET_AUTH_MANAGER_FAILED). ...";
```

Password fail is **3**. Network timeout is **7**. IP ACL fail is **1012**. Do not mix them.

C# `Describe` (this-pass read) maps the same integer and names Achiever, not Starwave:

```442:454:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private static string Describe(MTRetCode code, string op)
    {
        var hint = (int)code switch
        {
            7 => "network/timeout — check proxy, firewall, server",
            1012 => "manager IP blocked — Achiever requires the whitelist HTTP proxy",
            3 => "params/auth — check manager login",
            ...
        };
        return $"{op} failed: {(int)code} {code} ({hint})";
    }
```

Connect is **fail-closed**: if pump `Connect` is not `MT_RET_OK`, it retries `PUMP_MODE_NONE`; if that is also not OK it throws `InvalidOperationException` with the 1012 string. It does **not** invent groups or fall back to `FakeMt5BrokerConnector`.

**What 1012 is not:** a closed TCP port, a wrong password (that is **3**), or a timeout (**7**). R012 measured `57.128.141.65:443` **OPEN** (309 ms) from this LAN while historical Connect still returned **1012**. Reachability ≠ allow-list. A 1012 deny happens **before** a Manager session exists: no pump, no `GroupRequestArray`, no `DealAdd` / `OrderAdd` / `DealerBalance`. Equity cannot move on a connection that never authenticated.

Nearby vendor `1012` tokens in **other** enums (`CONDITION_ACCOUNT_LEVERAGE`, `ACTION_ACCOUNT_ARCHIVE`, dataset field ids, `IDC_BUTTON_DEPOSIT`) are **not** this retcode.

---

## 2. Achiever requires HTTP proxy `81.29.145.69:49527` (this workstation)

### 2.1 Two non-secret keys that share the same IPv4

`81.29.145.69` is **not a secret** (C55). It is two jobs. Mixing them is how operators send Starwave through the wrong hop or treat the port as a secret.

| Key | Job | Failure if omitted on this LAN |
|---|---|---|
| `ACHIEVER_EGRESS_IP=81.29.145.69` | Source IP Achiever’s Manager ACL must see. **Not** a `Connect()` argument. | **1012** |
| `ACHIEVER_PROXY_HOST=81.29.145.69` + `ACHIEVER_PROXY_PORT=49527` | Optional hop that **presents** that source IP via Manager `ProxySet`. | Direct Connect from this LAN → **1012** |
| Type | `PROXY_HTTP = 2` (HTTP CONNECT, incl. NTLM) | SOCKS5 is the wrong type |

Architecture §7 (binding, this-pass read of `MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L379–387):

```text
Required whitelisted outbound IP:
81.29.145.69
```

> If proxying is required, credentials must be in secret storage/environment variables.  
> Never log proxy credentials.

“If required” = **when the worker does not already source-NAT as that IP**. This host does not.

Architecture §56 secret-safe sample (passwords are `<SECRET>` only; this-pass read L2047–2069):

```text
ACHIEVER_EGRESS_IP=81.29.145.69
ACHIEVER_PROXY_ENABLED=true
ACHIEVER_PROXY_HOST=81.29.145.69
ACHIEVER_PROXY_PORT=49527
MT5_STARWAVEFX_PROXY_ENABLED=false
```

Prop `.env` (gitignored; classified from R012 SHA `556ACAA9EFF6106D601E4BCC556811C149A5140477B974AF77A3F9B5D77396FF`; this slot did **not** reprint secret values):

| Key | Presence | Published here |
|---|---|---|
| `ACHIEVER_EGRESS_IP` | PRESENT | `81.29.145.69` |
| `ACHIEVER_PROXY_ENABLED` | PRESENT | `true` |
| `ACHIEVER_PROXY_HOST` | PRESENT | `81.29.145.69` |
| `ACHIEVER_PROXY_PORT` | PRESENT | `49527` |
| `ACHIEVER_PROXY_USERNAME` / `PASSWORD` | PRESENT | **REDACTED** |
| `MT5_STARWAVEFX_PROXY_ENABLED` | PRESENT | `false` |
| `IS_MT5_PROXY_ENABLED` / `MT5_PROXY_*` | **KEY_ABSENT** | C++ `AppConfig` will treat proxy as **off** |

Catalog seed (non-secret host/port; live `ProxySet` still comes from env via `LiveMt5Registration`, not this row):

```27:29:D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs
                ProxyEnabled = true,
                ProxyHost = "81.29.145.69",
                ProxyPort = 49527,
```

Starwave seed has **no** `ProxyHost` / `ProxyPort` / `ProxyEnabled=true`.

### 2.2 Hop type is HTTP, applied before Connect

Vendor (this-pass read):

```80:83:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
      PROXY_SOCKS4   =0,                     // SOCKS4
      PROXY_SOCKS5   =1,                     // SOCKS5
      PROXY_HTTP     =2,                     // HTTP (including NTLM)
```

C# product (`ApplyProxy` hard-codes HTTP; skipped unless `ProxyEnabled` **and** host non-empty):

```115:129:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private void ApplyProxy()
    {
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
        if (set != MTRetCode.MT_RET_OK)
            throw new InvalidOperationException(Describe(set, $"{BrokerCode} ProxySet"));
    }
```

`ConnectCore` calls `ApplyProxy()` **before** `Connect` (pump `GROUPS|USERS|POSITIONS`, then no-pump fallback). Missing / false / typo `ACHIEVER_PROXY_ENABLED` → `ApplyProxy()` returns immediately → **direct** Manager TCP → **1012** on this LAN.

Live registration (Achiever from env; Starwave **forced off** — the Starwave env key is unread):

```30:45:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
            ProxyEnabled = bool.TryParse(config["ACHIEVER_PROXY_ENABLED"], out var pe) && pe,
            ProxyHost = config["ACHIEVER_PROXY_HOST"],
            ProxyPort = int.TryParse(config["ACHIEVER_PROXY_PORT"], out var pp) ? pp : 0,
            ProxyUser = config["ACHIEVER_PROXY_USERNAME"],
            ProxyPassword = config["ACHIEVER_PROXY_PASSWORD"],
            ...
            BrokerCode = BrokerCodes.StarwaveFx,
            ...
            ProxyEnabled = false,
```

This is **not** a process-wide `HTTP_PROXY`. R012: Process/User/Machine `HTTP_PROXY` / `HTTPS_PROXY` were **UNSET**. Exporting `HTTP_PROXY=http://81.29.145.69:49527` is **forbidden** (would steal Starwave, FIX, restore, and health onto the Achiever hop). Scope is **per Achiever Manager session** via `ProxySet`.

C++ `Connect` (both trees) reapplies `ProxySet` before logon when `m_proxyConfig.enabled` (`mt5_manager.cpp` L76–95). That path is **Achiever-relevant**; it is **not** wired for Starwave in C# registration.

### 2.3 Measured topology (R012, same host class, 2026-08-18)

| Fact | Value |
|---|---|
| Host | `DESKTOP-FQPFPKE` |
| Public egress (no proxy) | **`106.219.132.213`** (three oracles, all 200) |
| Equals `ACHIEVER_EGRESS_IP`? | **No** |
| `57.128.141.65:443` (Achiever Manager) | **OPEN** 309 ms |
| `84.201.6.142:443` (StarwaveFX Manager) | **OPEN** 189 ms |
| `81.29.145.69:49527` (HTTP proxy listener) | **OPEN** 199 ms |
| `81.29.145.69:443` | **REFUSED** |
| `81.29.145.69:80` | **TIMEOUT** ~3 s |

TCP OPEN on `:443` **and** `:49527` proves reachability. Historical YoPips **local** starts on this class of machine logged `MT5 proxy mode: DISABLED (global)` then **1012** on pump **and** no-pump. That is the measured failure mode this pin exists to avoid.

This-pass re-read of `D:\Projects\YoPips\Backend\C++ Backend PropFirm\runtime-backend-3001.out.log`:

| Line | Time (2026-07-16) | Message |
|---:|---|---|
| 5 | 15:49:48.083 | `MT5 mode: LOCAL (SDK)` + `mt5=57.128.141.65` |
| 8 | 15:49:48.094 | `MT5 proxy mode: DISABLED (global)` |
| 16 | 15:50:16.391 | `MT5 Connect failed: 1012 — retrying without pump mode` |
| 18 | 15:50:18.953 | `MT5 Connect (no-pump fallback) also failed: 1012` |
| 20–23 | 15:50:22+ | pool sessions **1012**; later one **7** |

Same 1012-on-both-attempts pattern in `runtime-smoke-3002.out.log` and `local-runtime/backend-20260717-142524.stdout.log` / `144426` (this-pass grep). Pump-none does **not** bypass the allow-list.

Would local connect work **without** proxy if the worker already source-NATed as `81.29.145.69`? **Yes, in principle** (A54 preference 1). That is **not** this desktop.

### 2.4 C++ toggle trap (YoPips + extracted Prop SDK)

YoPips `.env` (SHA-256 `E3F07B1595AEB8CB8D66A5410BCA7E26974090EF05857165C7190FBBF1A0A40A` per R012) carries:

| Key | Presence | Published here |
|---|---|---|
| `MT5_MODE` | PRESENT | `local` |
| `MT5_SERVER` | PRESENT | `57.128.141.65` |
| `MT5_PORT` | PRESENT | `443` |
| `MT5_LOGIN` | PRESENT | `2027` (non-secret) |
| `MT5_PROXY_TYPE` | PRESENT | `HTTP` |
| `MT5_PROXY_ADDRESS` | PRESENT | `81.29.145.69` |
| `MT5_PROXY_PORT` | PRESENT | `49527` |
| `MT5_PROXY_ENABLED` | PRESENT | `true` |
| `IS_MT5_PROXY_ENABLED` | **KEY_ABSENT** | — |
| `MT5_PASSWORD` / proxy auth | PRESENT | **REDACTED** |

`AppConfig` binds the master switch from **`IS_MT5_PROXY_ENABLED`**, default **false**:

```172:172:D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp
    cfg.mt5_proxy_enabled = get_bool("IS_MT5_PROXY_ENABLED", false);
```

```135:135:D:\Prop\mt5-sdk\config\app_config.cpp
    cfg.mt5_proxy_enabled = get_bool("IS_MT5_PROXY_ENABLED", false);
```

YoPips `main.cpp` only calls `SetProxy` when that flag is true and `TYPE/ADDRESS/PORT` are complete; else it logs `MT5 proxy mode: DISABLED (global)` and connects **direct**. Type string `"HTTP"` maps to `MTProxyInfo::PROXY_HTTP`. `MT5_PROXY_ENABLED=true` is **not read**. A C++ probe that only sets `MT5_PROXY_ENABLED` will **repeat 1012** here.

C# product uses **different** keys (`ACHIEVER_PROXY_*`). Do not mix the two families.

---

## 3. Fetch ALL groups and ALL manager traders

Architecture §7 (this-pass read L389–393): `demo\Maxmaster` is **not** the only group. The system must enumerate **all groups accessible to the Manager login**.

C# implementation (`NativeMt5BrokerConnector`, this-pass read L144–233):

| Walk | Call | Purpose |
|---|---|---|
| Groups (primary) | `GroupRequestArray("*")` L155 | Request-complete, all manager-visible groups |
| Groups (fallback) | `GroupTotal` + `GroupNext` L174–179 | Pump cache only — used if request array returns empty |
| Accounts (`group == null`) | every group name from above L201–203 | **ALL** manager traders |
| Accounts (per group) | `UserRequestArray` L223 → `UserGetByGroup` L225 → `UserLogins` + `UserRequestByLogins` L230–232 | Request-complete, not first-200 |

`LiveBrokerProbe` (`D:\Prop\tools\LiveBrokerProbe\Program.cs`) uses `LiveMt5Registration.CreateConnectorsFromEnvironment()` then `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`. Achiever therefore **does** `ProxySet PROXY_HTTP` before Connect when `ACHIEVER_PROXY_ENABLED=true`. Passwords are never written to the JSON (`note`: “Passwords never written. Groups and manager logins only.”).

### 3.1 Measured census (already on disk; independently re-summed this pass)

Source: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` at **`2026-08-18T08:42:16.8519545+00:00`**.  
Write-up: `LIVE_MANAGER_FETCH_MEASURED.md`.

| Broker | Connect | Groups | Traders | Open positions | Hop |
|---|---|---:|---:|---:|---|
| ACHIEVER | `connected: true` (7212.5885 ms) | **8** | **6512** | 1506 | HTTP proxy |
| STARWAVEFX | `connected: true` (6413.478 ms) | **10** | **1948** | 478 | **direct** |
| **Total** | both OK | **18** | **8460** | 1984 | — |

Achiever groups (JSON `groupNames`; column `accounts` re-sum **6512** = 2+179+4+5+4+6295+0+23):

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
| **sum** | **6512** |

Starwave groups (re-sum **1948** = 11+4+170+1735+22+0+0+4+0+2):

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
| **sum** | **1948** |

`demo\Maxmaster` is **absent** from the live Achiever dump (this-pass grep of the JSON: **0** hits). Architecture still lists it as a *default label*, not the universe. Empty groups (`demo\yo-instant`, three Starwave real buckets) are still **fetched** — ALL means visible names, including zero-login groups.

These are **all groups this manager login can see**. If the server has more groups, they are outside this manager's permission set. Do not invent extra names.

This slot **did not** re-attach. Counts are prior measure, re-summed from the on-disk JSON, not a new Connect.

---

## 4. Copy to cTrader must not send live orders (no loss)

| Gate | Measured this pass |
|---|---|
| `CTraderFixSession` outbound MsgType | **only** `(35, "A")` Logon (`BuildLogon` L96). One `WriteAsync`. Sockets disposed. File has **0** `NewOrderSingle` / `35=D`. |
| Product `*.cs` / `*.json` / `*.csproj` `35=D` or `(35, "D")` | **0** hits (this-pass grep of `D:\Prop\src`, `D:\Prop\apps`, and product globs) |
| `SendTrade` / `DealerSend` / `GuardedNewOrderSingle` under `src/` | **0** hits |
| `RealCopyEnabled` | forced `false` in `DependencyInjection.cs:41` **and** `CTraderFixLogonHostedService.cs:68` |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** (comment: “Default OFF”) |
| API `/api/settings` | exposes `featureFlags.REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled` (forced false) |
| fix-worker | reads `CTrader:RealCopyExecutionEnabled` (different key), fallback **false**; even if true it **only logs** and still stamps TRADE `Disconnected` / “NewOrderSingle remains off” |
| Shadow path | `ShadowCopyEngine.SimulateEntry` is in-memory. No socket. |
| YoPips C++ `src` | no cTrader FIX sender (prior W500_50 pin; this slot did not re-audit YoPips FIX) |

```32:35:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;
```

```38:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
```

```68:71:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.RealCopyEnabled = false;
        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
            quote.LoggedOn, trade.LoggedOn, account);
```

```94:108:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            ...
            (553, username),
            (554, password)
        };
```

Safety class is **`SAFE_BY_ABSENCE`**, not a unit-tested `GuardedNewOrderSingle` refuse-path. Flipping `REAL_COPY_EXECUTION_ENABLED=true` would **still** not place an order (no builder). Do **not** tick Architecture §70.12 as a coded gate PASS. Do **not** enable the flag.

FIX Logon (`35=A`) on QUOTE 5211 / TRADE 5212 is session-only. It is not an order. Seed TRADE row last-error: `"session up for logon/recon only; NewOrderSingle off"`.

`LiveRuntimeStatus.Snapshot` copy note when the flag is false: *“NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.”*

---

## 5. Decision table (operator pin)

| Situation | Need HTTP `ProxySet` `81.29.145.69:49527`? |
|---|---|
| Achiever `MT5_MODE=local` on `DESKTOP-FQPFPKE` (egress `106.219.132.213`) | **YES** — else **1012** |
| Achiever local on a Windows worker whose default SNAT **is** `81.29.145.69` | **NO** (A54 preference 1) |
| Achiever local through Linux compose NAT | **Still 1012** unless that NAT is the allow-list or you proxy |
| StarwaveFX local on this host | **NO** (`ProxyEnabled=false`; no documented whitelist) |
| C++ probe with only `MT5_PROXY_ENABLED=true` (wrong key) | Proxy **not applied** → **1012** on this host |
| C# `NativeMt5BrokerConnector` with `ACHIEVER_PROXY_ENABLED=true` + loaded secrets | **Applies** HTTP `ProxySet`; live success already measured by `LiveBrokerProbe` (prior; not this slot) |

---

## 6. What this agent did **not** do

- Did not print or copy `ACHIEVER_PROXY_PASSWORD`, `MT5_PROXY_PASSWORD`, `MT5_PASSWORD`, `MT5_STARWAVEFX_PASSWORD`, `CTRADER_FIX_PASSWORD`, proxy username, or any YoPips JWT / PSP / KYC / DB secret.
- Did not send HTTP CONNECT or Manager login this pass.
- Did not send FIX `35=D`.
- Did not edit `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `D:\Prop\mt5-sdk`, either `.env`, or YoPips source.
- Did not whitelist `106.219.132.213` at the broker.
- Did not claim “EX5 decompiled” or “≥95% copy-trading live.” This is a retcode + hop + catalog pin.

---

## 7. Checklist

- [x] Official `MT_RET_AUTH_MANAGER_IPBLOCK = 1012` confirmed in Prop + YoPips vendor headers.
- [x] Achiever HTTP hop `81.29.145.69:49527` required on this LAN (else 1012). Type `PROXY_HTTP`.
- [x] Starwave stays direct.
- [x] ALL manager-visible groups/traders already measured: **18 / 8460** (re-summed).
- [x] `demo\Maxmaster` is not the live universe.
- [x] Copy cannot lose capital: no `35=D`, `RealCopyEnabled=false`.
- [x] Secrets not printed. Product source not edited.

**Slot 103 verdict: CONFIRMED. Risk to capital: NONE.**
