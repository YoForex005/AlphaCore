# W500_RESEARCH_23 — `MT_RET_AUTH_MANAGER_IPBLOCK=1012` and Achiever HTTP proxy `81.29.145.69:49527`

| Field | Value |
|---|---|
| Slot | **23** |
| Agent | W500_RESEARCH_23 (senior engineer, read-only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_23.md` |
| Assigned | Confirm `MT_RET_AUTH_MANAGER_IPBLOCK=1012` and that Achiever requires HTTP proxy `81.29.145.69:49527`. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy-to-cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Config / `.env` / `appsettings` edited | **No.** |
| Secret values printed | **None.** Proxy user/password, `MT5_PASSWORD`, `MT5_STARWAVEFX_PASSWORD`, `CTRADER_FIX_PASSWORD` classified PRESENT only where prior reports already recorded presence. |
| Method | `read_file` / `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Official vendor headers + C#/C++ wrappers + live census JSON + FIX send-path. No Manager reconnect this pass. No HTTP CONNECT. No FIX send. |
| Siblings (do not treat as this file) | R012 (need proxy on this LAN), R022 (Achiever-only hop), C55 (egress IP non-secret), LIVE_MANAGER_FETCH_MEASURED (census), E002 / E034 (no `35=D`), W500_SLICE_23 (`EnvFile.cs` — different assignment) |

This is a **confirm-and-pin** research note for the all-groups / all-traders fetch. It does not add a sender.

---

## 0. Verdict (binding)

**CONFIRMED on both assigned claims. Goal path is catalog-only. Live copy stays off.**

| Claim | Result | Class |
|---|---|---|
| Official retcode `MT_RET_AUTH_MANAGER_IPBLOCK` equals **1012** | **Yes** | Official `EnMTAPIRetcode` in MetaQuotes Manager SDK (Prop copy **and** YoPips copy). Same integer in PHP / .NET WebAPI examples, C++ `mt5ErrorReason`, C# `Describe`. |
| Comment / format string is “IP address unallowed for manager” | **Yes** | `MT5APIConstants.h:46`, `MT5APIFormat.h:125` |
| Achiever Manager from this lab needs HTTP proxy **`81.29.145.69:49527`** | **Yes** | Allow-list identity is `81.29.145.69` (non-secret). Type is `MTProxyInfo::PROXY_HTTP = 2`. Historical YoPips local Connect with proxy **disabled** = **1012** on pump **and** no-pump. Measured live census later connected Achiever **via that HTTP hop**. |
| StarwaveFX needs that hop | **No** | `LiveMt5Registration` hard-codes `ProxyEnabled = false`. Live census: Starwave **OK direct**. |
| Fetch **all** groups + **all** manager traders (not `demo\Maxmaster` only) | **Yes, measured 2026-08-18T08:42:16Z** | Achiever **8 / 6512**; Starwave **10 / 1948**; total **18 / 8460**. `demo\Maxmaster` is **absent** from the live group list. |
| Copy to cTrader may send live orders in this process | **No** | `RealCopyEnabled` forced **false**. `CTraderFixSession` builds only `35=A`. Product C# has **0** `35=D` builders. SAFE_BY_ABSENCE + forced flag. |

Honest one-liner:

```text
1012 = MT_RET_AUTH_MANAGER_IPBLOCK (official).
Achiever local Manager on this LAN requires ProxySet PROXY_HTTP 81.29.145.69:49527
  (or native SNAT as that IP). Starwave stays direct.
LiveBrokerProbe already listed every visible group+login (8+10 / 6512+1948).
NewOrderSingle is not implemented. No capital at risk from copy.
```

---

## 1. `MT_RET_AUTH_MANAGER_IPBLOCK = 1012` (official, not a house constant)

Vendor enum (identical in both trees):

```34:47:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIConstants.h
   MT_RET_AUTH_CLIENT_INVALID   =1000,    // Invalid terminal type
   ...
   MT_RET_AUTH_MANAGER_NOCONFIG =1011,    // Manager account doesn't have manager config
   MT_RET_AUTH_MANAGER_IPBLOCK  =1012,    // IP address unallowed for manager
   MT_RET_AUTH_GROUP_INVALID    =1013,    // Group is not initialized (server restart neccesary)
```

```46:46:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIConstants.h
   MT_RET_AUTH_MANAGER_IPBLOCK  =1012,    // IP address unallowed for manager
```

Format table (same integer, human string):

```125:125:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIFormat.h
      { MT_RET_AUTH_MANAGER_IPBLOCK,  L"IP address unallowed for manager" },
```

Cross-language copies (still 1012; not a different family of “1012” like `IDC_BUTTON_DEPOSIT` or `CONDITION_ACCOUNT_LEVERAGE`):

| Tree | File | Binding |
|---|---|---|
| Prop C++ wrapper | `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp:64` | `case 1012:` → “IP blocked by MT5 server (`MT_RET_AUTH_MANAGER_IPBLOCK`)…” |
| YoPips C++ wrapper | `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp:64` | **identical** string |
| Official PHP example | `mt5_retcode.php:42` | `const MT_RET_AUTH_MANAGER_IPBLOCK = 1012` |
| Official .NET WebAPI example | `MTRetCode.cs:46` (Prop + YoPips) | `MT_RET_AUTH_MANAGER_IPBLOCK = 1012` |
| Prop C# native connector | `NativeMt5BrokerConnector.cs:447` | `1012 => "manager IP blocked — Achiever requires the whitelist HTTP proxy"` |

Do **not** confuse with other official `1012` tokens in unrelated enums (`ACTION_ACCOUNT_ARCHIVE`, `CONDITION_ACCOUNT_LEVERAGE`, `FIELD_CLIENT_COMPLIANCE_TIME_APPROVAL`, Win32 `IDC_BUTTON_DEPOSIT`). Those are **not** Manager auth retcodes.

Do **not** confuse with `MT_RET_AUTH_INVALID_IP = 1016` (“Unallowed address [check server's ip address]”). **1012** is **manager access-list** (source IP of the Manager API client). **1016** is server-identity.

**What 1012 is not:** a closed TCP port. R012 measured `57.128.141.65:443` **OPEN** from this LAN while historical Connect still returned **1012**. Reachability ≠ allow-list.

---

## 2. Achiever requires HTTP proxy `81.29.145.69:49527` (this workstation)

### 2.1 Two non-secret keys, one hop

`81.29.145.69` is **not a secret** (C55). It is two operator keys that share an IPv4:

| Key | Meaning | Failure if wrong |
|---|---|---|
| `ACHIEVER_EGRESS_IP=81.29.145.69` | Source IP Achiever’s Manager ACL must see. **Not** a `Connect()` argument. | **1012** |
| `ACHIEVER_PROXY_HOST=81.29.145.69` + `ACHIEVER_PROXY_PORT=49527` | Optional hop that **presents** that source IP via Manager `ProxySet`. | Direct Connect from this LAN → **1012** |

Architecture §7 (binding):

> Required whitelisted outbound IP: `81.29.145.69`  
> If proxying is required, credentials must be in secret storage/environment variables.  
> Never log proxy credentials.

Architecture §56 turns the hop **on**:

```
ACHIEVER_EGRESS_IP=81.29.145.69
ACHIEVER_PROXY_ENABLED=true
ACHIEVER_PROXY_HOST=81.29.145.69
ACHIEVER_PROXY_PORT=49527
ACHIEVER_PROXY_USERNAME=<SECRET>
ACHIEVER_PROXY_PASSWORD=<SECRET>
```

Catalog seed matches the non-secret hop (no credentials stored):

```27:29:D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs
                ProxyEnabled = true,
                ProxyHost = "81.29.145.69",
                ProxyPort = 49527,
```

Proxy **type** is official HTTP, not SOCKS:

```79:85:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   enum
     {
      PROXY_SOCKS4   =0,                     // SOCKS4
      PROXY_SOCKS5   =1,                     // SOCKS5
      PROXY_HTTP     =2,                     // HTTP (including NTLM)
      PROXY_LAST     =PROXY_HTTP             // last type
     };
```

C# applies that type **before** `Connect`:

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

Achiever is the **only** connector that may set it. Starwave is forced off:

```23:47:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            ...
            ProxyEnabled = bool.TryParse(config["ACHIEVER_PROXY_ENABLED"], out var pe) && pe,
            ProxyHost = config["ACHIEVER_PROXY_HOST"],
            ProxyPort = int.TryParse(config["ACHIEVER_PROXY_PORT"], out var pp) ? pp : 0,
            ...
        });

        var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            ...
            ProxyEnabled = false,
            ...
        });
```

Missing / false / typo `ACHIEVER_PROXY_ENABLED` → `ApplyProxy()` returns immediately → **direct** Manager TCP → **1012** on this LAN.

Do **not** set process `HTTP_PROXY` / `HTTPS_PROXY` (R022): that would steal Starwave and cTrader FIX onto the Achiever hop. Scope is **per Achiever Manager session** via `ProxySet`.

### 2.2 Why *this* desktop requires the hop

R012 (measured, no auth):

| Fact | Value |
|---|---|
| Host | `DESKTOP-FQPFPKE` |
| Public egress (no proxy) | **`106.219.132.213`** (three oracles) |
| Equals allow-list `81.29.145.69`? | **No** |
| `57.128.141.65:443` (Achiever Manager) | **OPEN** (309 ms) — not the failure mode |
| `81.29.145.69:49527` (HTTP proxy listener) | **OPEN** (199 ms) |
| `81.29.145.69:443` / `:80` | REFUSED / TIMEOUT |
| `84.201.6.142:443` (Starwave) | **OPEN** (189 ms); no whitelist |

Historical YoPips **local** starts on this class of machine logged `MT5 proxy mode: DISABLED (global)` then **1012** on pump **and** no-pump (`runtime-backend-3001.out.log` 2026-07-16T15:49:48 and four later local-runtime logs — R012 §4). That is the measured failure mode this pin exists to avoid.

C++ toggle trap (still live in both trees): YoPips / Prop `AppConfig` binds the master switch from **`IS_MT5_PROXY_ENABLED`**, default **false**. A file that only sets `MT5_PROXY_ENABLED=true` leaves C++ on the **direct** path → **1012** here.

```172:172:D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp
    cfg.mt5_proxy_enabled = get_bool("IS_MT5_PROXY_ENABLED", false);
```

```135:135:D:\Prop\mt5-sdk\config\app_config.cpp
    cfg.mt5_proxy_enabled = get_bool("IS_MT5_PROXY_ENABLED", false);
```

C# live path uses the §56 names (`ACHIEVER_PROXY_*`). That is the correct key family for the all-groups fetch.

Native SNAT as `81.29.145.69` would make the HTTP hop unnecessary (A54 preference 1). **That is not this desktop.**

### 2.3 Later measured Connect (supersedes C42 / R022 “not proven”)

`LiveBrokerProbe` (`D:\Prop\tools\LiveBrokerProbe\Program.cs`) loads env, builds the same `LiveMt5Registration` pair, `ConnectAsync`, `GetGroupsAsync`, `GetAccountsAsync(null)` (null = **every** group), writes `LIVE_GROUPS_AND_TRADERS.json`.

UTC **2026-08-18T08:42:16.8519545+00:00**, `envLoaded: true`, `connected: true` for **both** brokers. CREDENTIALS_AND_COPY_STATUS + LIVE_MANAGER_FETCH_MEASURED classify Achiever as **OK via whitelist HTTP proxy**. C42 “FakeMt5 only” and R022 “live Connect not proven” are **stale** relative to that artifact. This pass did **not** re-run Connect.

---

## 3. Goal: ALL Achiever + Starwave groups and ALL manager traders

Architecture §7: `demo\Maxmaster` is **not** the universe. Startup must `Connect → enumerate groups → upsert → enumerate accounts`.

Code path (all groups, all logins the manager can see):

| Step | Where | What |
|---|---|---|
| Connect + `ProxySet` HTTP on Achiever only | `NativeMt5BrokerConnector.ConnectCore` | pump `GROUPS\|USERS\|POSITIONS`, fallback `PUMP_MODE_NONE` |
| Groups | `GetGroupsCore` | `GroupRequestArray("*")`, fallback `GroupTotal` / `GroupNext` |
| Accounts | `GetAccountsCore(null)` | walks **every** group; `UserRequestArray` / `UserGetByGroup` / `UserLogins` + `UserRequestByLogins` |
| Ingest | `DealIngestionService.SyncCatalogAsync` | `GetGroupsAsync` + `GetAccountsAsync(null)` — no default-group filter |
| Probe | `LiveBrokerProbe` | same APIs; JSON dump, **no passwords** |

Measured census (`LIVE_GROUPS_AND_TRADERS.json` header + group rows). Account sums re-added this pass:

### Achiever (HTTP proxy) — 8 groups, 6512 traders, 1506 open positions

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
| **Sum** | **6512** |

`demo\Maxmaster` **does not appear**. Grep of the live JSON for that name: **0 hits**. Treating `MT5_DEFAULT_GROUP=demo\Maxmaster` as the catalog is **wrong**.

### StarwaveFX (direct, no proxy) — 10 groups, 1948 traders, 478 open positions

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
| **Sum** | **1948** |

**Total visible to these two manager logins: 18 groups / 8460 traders.** Logins are **not** globally unique; persist `(broker_id, login)`.

Honesty: this is **every group / login these manager accounts can see**. If the brokers have more groups outside this ACL, they are invisible and must not be invented.

Dummy/FakeMt5 seed is **off** on the live API path (`HasRealPasswords` required; `DemoSeeder` not used at API startup).

---

## 4. Copy to cTrader must not send live orders (no loss)

Goal of this fetch is **catalog + intelligence**, not destination fills.

| Gate | Measured state |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | initializer **`false`** (“Default OFF”) |
| `DependencyInjection` | `RealCopyEnabled = false` with comment “Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.” |
| `GET /api/settings` | `REAL_COPY_EXECUTION_ENABLED` = runtime flag; `FEATURE_COPY_TRADING_ENABLED` = **false** literal |
| `LiveRuntimeStatus.copyNote` when flag false | “NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.” |
| `CTraderFixSession` outbound tag 35 | **only** `(35, "A")` Logon. No `(35, "D")`. File ends after logon parse. |
| Product C# `35=D` / `(35, "D")` | **0** (E034) |
| `GuardedNewOrderSingle` / `SubmitNewOrder*` | **0** |
| `apps/fix-worker` | even if `CTrader:RealCopyExecutionEnabled` is true, it **logs a warning and still does not send** |
| TRADE seed `LastError` | “session up for logon/recon only; NewOrderSingle off” |
| Adjacent FIX builders | Quote `35=V` MDR; harness `35=X` snapshot. Neither is a live order. |

A 1012 on Achiever is a **Manager logon deny**. Native `Connect` throws; `LiveIngestHostedService` catch sets `Connected=false` and **does not substitute dummy data**. 1012 cannot open, close, or resize cTrader risk.

**Risk to capital from this slot:** **none.** Research-only. No Connect this pass. No FIX send path exists to arm.

Do **not** flip `REAL_COPY_EXECUTION_ENABLED`. Do **not** add `35=D` to satisfy the fetch goal.

---

## 5. Operator pin (slot 23)

1. **1012** is official `MT_RET_AUTH_MANAGER_IPBLOCK` — manager source IP not on the access list. Not a closed port. Not `1016`.
2. **Achiever** on this LAN: `ACHIEVER_PROXY_ENABLED=true` + `ProxySet` `PROXY_HTTP` to **`81.29.145.69:49527`** (user/pass in env only; never log). Else historical **1012**.
3. **StarwaveFX**: direct `84.201.6.142:443`. Do not point it at the Achiever hop.
4. C# key = `ACHIEVER_PROXY_*`. C++ key = `IS_MT5_PROXY_ENABLED` (not `MT5_PROXY_ENABLED`).
5. Fetch **all** groups (`GroupRequestArray("*")` / `GroupTotal`) and **all** logins (`GetAccountsAsync(null)`). Do not stop at `demo\Maxmaster`.
6. Measured inventory to reuse: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (8+10 groups, 6512+1948 traders). Re-fetch if ACL changes; do not invent rows.
7. Copy-to-cTrader stays **SHADOW / CopyIntent**. Live `NewOrderSingle` remains **off**.

---

## 6. What this agent did **not** do

- Did not print or copy proxy user/password, MT5 passwords, or FIX password.
- Did not run Manager `Connect` or HTTP CONNECT this pass (census already on disk).
- Did not send FIX `35=D` / `35=F` / `35=G`.
- Did not edit `D:\Prop\src`, `apps`, `tests`, `mt5-sdk`, either `.env`, or YoPips source.
- Did not whitelist `106.219.132.213` at the broker.
- Did not claim EX5 decompile, ≥95% copy-trading live, or that `/ready` proves Manager health.

---

## 7. Done criteria

- [x] Official SDK pin: `MT_RET_AUTH_MANAGER_IPBLOCK = 1012` cited from MetaQuotes headers in **both** Prop and YoPips trees.
- [x] Wrapper + C# `Describe` mappings cited.
- [x] Achiever HTTP hop `81.29.145.69:49527` (`PROXY_HTTP=2`) confirmed as required on this LAN; Starwave excluded.
- [x] Historical 1012-without-proxy + later live census-with-proxy both cited.
- [x] All-groups / all-traders counts re-summed from `LIVE_GROUPS_AND_TRADERS.json`.
- [x] Copy-to-cTrader live send confirmed **off** (flag + absence of `35=D`).
- [x] Product source not modified. Secrets not printed.
