# W500_RESEARCH_63 — Confirm `MT_RET_AUTH_MANAGER_IPBLOCK=1012` and Achiever HTTP proxy `81.29.145.69:49527`

| Field | Value |
|---|---|
| Slot | **63** |
| Agent | W500_RESEARCH_63 (senior engineer, read-only) |
| Date | 2026-08-18 |
| Measured at | This pass re-read official SDK + wrappers in **both** Prop and YoPips trees; C# `ApplyProxy` / `Describe`; YoPips historical **1012** logs; live census JSON already on disk (`2026-08-18T08:42:16.8519545+00:00`) |
| Assigned | Confirm `MT_RET_AUTH_MANAGER_IPBLOCK=1012` and that Achiever requires HTTP proxy `81.29.145.69:49527`. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_63.md` |
| Product source modified | **No.** Report + INDEX/SWARM_LOG only. |
| Test source modified | **No.** |
| Config / `.env` edited | **No.** Keys classified PRESENT + non-secret identifiers only. |
| Secret values printed | **None.** No Manager password, no proxy user/password, no FIX password. |
| Live Manager logon this pass | **Not re-run.** Prior measured census: `LIVE_GROUPS_AND_TRADERS.json` + `LIVE_MANAGER_FETCH_MEASURED.md`. |
| Binding law | Official MetaQuotes `EnMTAPIRetcode`; architecture §§7, 8, 56, 68, 70; C55 (egress IP non-secret); R012 (this LAN needs the hop); R022 (Achiever-only scope); A39 (ALL groups = manager-visible set); E002 (`SAFE_BY_ABSENCE`) |

This is a **slot-63 confirmation pin**. It does not whitelist IPs, does not send `35=D`, and does not invent groups outside the manager ACL.

---

## 0. Verdict (binding)

**CONFIRMED on every assigned claim.**

| Claim | Result | Evidence class |
|---|---|---|
| Official retcode `MT_RET_AUTH_MANAGER_IPBLOCK` equals **1012** | **Yes** | MetaQuotes `EnMTAPIRetcode` in Prop **and** YoPips Manager SDK. Same integer in C++ wrapper, PHP, .NET WebAPI, C# `Describe`. |
| Official English | “IP address unallowed for manager” | `MT5APIFormat.h` L125 + PHP/NET examples |
| Achiever Manager from this lab needs HTTP proxy **`81.29.145.69:49527`** | **Yes** | Allow-list identity `81.29.145.69` (non-secret). Type `MTProxyInfo::PROXY_HTTP = 2`. This desktop public egress measured `106.219.132.213` ≠ allow-list (R012). Historical YoPips local Connect with proxy **disabled** = **1012** on pump **and** no-pump (re-read this pass). Live census later connected Achiever **via that HTTP hop**. |
| StarwaveFX uses the same hop | **No** | `LiveMt5Registration` hard-sets `ProxyEnabled = false`. Architecture §56 `MT5_STARWAVEFX_PROXY_ENABLED=false`. R022: do **not** hairpin Starwave through `:49527`. |
| Fetch **ALL** Achiever + Starwave groups | **Implemented + measured** | `GroupRequestArray("*")` then `GroupTotal`/`GroupNext` fallback. Live **8 + 10 = 18**. |
| Fetch **ALL** manager traders | **Implemented + measured** | `GetAccountsAsync(null)` walks every group via `UserRequestArray` / `UserGetByGroup` / `UserLogins`+`UserRequestByLogins`. Live **6512 + 1948 = 8460**. |
| Copy to cTrader may send live orders today | **No** | `RealCopyEnabled` forced **false**. Product C# has **0** `35=D` builders. FIX logon is `35=A` only. |
| Risk to capital from this slot / catalog fetch | **None** | Read-only Manager APIs. 1012 is a **logon deny** (no session → no dealer mutators). Copy path is `SAFE_BY_ABSENCE`. |

Honest one-liner:

```text
1012 = MT_RET_AUTH_MANAGER_IPBLOCK (official).
Achiever local Manager on this LAN requires ProxySet PROXY_HTTP 81.29.145.69:49527.
Live census already pulled all manager-visible groups/traders (18 / 8460).
NewOrderSingle stays off — SAFE_BY_ABSENCE — so copy cannot lose capital yet.
```

Do **not** treat `demo\Maxmaster` as the universe (it is **absent** from the live Achiever dump). Do **not** enable `REAL_COPY_EXECUTION_ENABLED`. Do **not** point Starwave at `:49527`. Do **not** confuse **1012** (manager source-IP ACL) with **1016** (`MT_RET_AUTH_INVALID_IP`, server-identity).

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

Official string table (Prop vendor copy):

```125:125:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIFormat.h
      { MT_RET_AUTH_MANAGER_IPBLOCK,  L"IP address unallowed for manager" },
```

Cross-language copies (still 1012; not a different family of “1012” like `IDC_BUTTON_DEPOSIT` or `CONDITION_ACCOUNT_LEVERAGE`):

| Copy | Path | Token |
|---|---|---|
| Official PHP example | `mt5_retcode.php:42` (Prop + YoPips) | `const MT_RET_AUTH_MANAGER_IPBLOCK =1012;` |
| Official .NET WebAPI | `MTRetCode.cs:46` (Prop + YoPips) | `MT_RET_AUTH_MANAGER_IPBLOCK = 1012` |
| Prop C++ wrapper | `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp:64` | `case 1012:` → “IP blocked by MT5 server (`MT_RET_AUTH_MANAGER_IPBLOCK`)…” |
| YoPips C++ wrapper | `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp:64` | **identical** `case 1012:` string |
| Prop C# live connector | `NativeMt5BrokerConnector.cs:447` | `1012 => "manager IP blocked — Achiever requires the whitelist HTTP proxy"` |

Wrapper mapping (both C++ trees, same line number):

```61:68:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
static std::string mt5ErrorReason(MTAPIRES code) {
    switch (code) {
        case 7:    return "Network timeout (MT_RET_ERR_NETWORK). ...";
        case 1012: return "IP blocked by MT5 server (MT_RET_AUTH_MANAGER_IPBLOCK). Ask MT5 server admin to whitelist this machine's IP.";
        case 5:    return "No connection to MT5 server (MT_RET_ERR_NOCONNECT). ...";
        case 3:    return "Wrong credentials (MT_RET_AUTH_MANAGER_FAILED). ...";
```

Password fail is **3**. Network timeout is **7**. IP ACL fail is **1012**. Do not mix them.

Nearby vendor `1012` tokens in **other** enums (`CONDITION_ACCOUNT_LEVERAGE`, `ACTION_ACCOUNT_ARCHIVE`, dataset field ids, `IDC_BUTTON_DEPOSIT`) are **not** this retcode.

---

## 2. Achiever requires HTTP proxy `81.29.145.69:49527` (this LAN)

### 2.1 Two non-secret keys that share the same IPv4

`81.29.145.69` is **not a secret** (C55). It is two jobs:

| Key | Role | Failure if omitted on this LAN |
|---|---|---|
| `ACHIEVER_EGRESS_IP=81.29.145.69` | Source address Achiever’s Manager ACL must see. **Not** a `Connect` argument. | **1012** |
| `ACHIEVER_PROXY_HOST=81.29.145.69` + `ACHIEVER_PROXY_PORT=49527` | Optional hop that **presents** that source IP via Manager `ProxySet`. | Direct Connect from this LAN → **1012** |

Architecture §7 (binding):

> Required whitelisted outbound IP: `81.29.145.69`  
> If proxying is required, credentials must be in secret storage/environment variables.  
> Never log proxy credentials.

“If required” = when the worker does **not** already source-NAT as that IP. This desktop does not.

Architecture §56 sample (lines 2047–2069): `ACHIEVER_EGRESS_IP=81.29.145.69`, `ACHIEVER_PROXY_ENABLED=true`, `ACHIEVER_PROXY_HOST=81.29.145.69`, `ACHIEVER_PROXY_PORT=49527`, and `MT5_STARWAVEFX_PROXY_ENABLED=false`. Proxy username/password in that sample are `<SECRET>` placeholders only.

Catalog seed (non-secret host/port only; live `ProxySet` uses env via `LiveMt5Registration`, **not** this row):

```27:29:D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs
                ProxyEnabled = true,
                ProxyHost = "81.29.145.69",
                ProxyPort = 49527,
```

Starwave seed (`BrokerCatalogSeed.cs` L36–52) has **no** `Proxy*` fields. `Broker.ProxyEnabled` defaults **false**.

### 2.2 Official hop type is HTTP (`PROXY_HTTP = 2`)

```80:83:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
      PROXY_SOCKS4   =0,                     // SOCKS4
      PROXY_SOCKS5   =1,                     // SOCKS5
      PROXY_HTTP     =2,                     // HTTP (including NTLM)
```

C# live path **hard-codes** HTTP (does not read `Mt5BrokerOptions.ProxyType`):

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
```

`ApplyProxy()` is called **before** `Connect` (L86). Address packing is `IP:port`. Auth packing is `login:password` (never log). If `ACHIEVER_PROXY_ENABLED` is missing / not `true`, this function returns immediately and Achiever connects **direct** → 1012 on this LAN.

YoPips `SetProxy` + Connect re-apply (same packing):

```33:52:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::SetProxy(...) {
    ...
    std::wstring addrPort = address + L":" + std::to_wstring(port);
    ...
    if (!proxyLogin.empty()) {
        std::wstring auth = proxyLogin + L":" + proxyPassword;
```

`main.cpp` maps `MT5_PROXY_TYPE=HTTP` → `MTProxyInfo::PROXY_HTTP` only when `config.mt5_proxy_enabled` is true (L201–225).

This is **not** a process-wide `HTTP_PROXY`. R012: Process/User/Machine `HTTP_PROXY` / `HTTPS_PROXY` were **UNSET**. Exporting `HTTP_PROXY=http://81.29.145.69:49527` is **forbidden** (would steal Starwave, FIX, restore, and health onto the Achiever hop).

### 2.3 This workstation is not the allow-list (R012, not re-probed)

| Fact | Value |
|---|---|
| Allow-list / expected egress | `81.29.145.69` (non-secret) |
| This desktop public egress | **`106.219.132.213`** (R012: ipify / ifconfig.me / icanhazip — all 200) |
| `57.128.141.65:443` (Achiever Manager) | **OPEN** 309 ms (R012 SYN-only) |
| `81.29.145.69:49527` (HTTP proxy listener) | **OPEN** 199 ms |
| `81.29.145.69:443` / `:80` | REFUSED / TIMEOUT — hop is **:49527**, not 443 |
| `84.201.6.142:443` (Starwave) | **OPEN** 189 ms — no proxy needed |

TCP OPEN on `:443` **and** `:49527` proves reachability. The failure mode without the hop is **allow-list**, not a closed port.

This slot did **not** re-run TCP SYN or public-IP oracles. Topology above is R012’s measured state (`2026-08-18T13:56:14+05:30`).

### 2.4 Historical 1012: proxy disabled in process (re-read this pass)

YoPips `AppConfig` binds the master switch from **`IS_MT5_PROXY_ENABLED`**, default **false**:

```172:172:D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp
    cfg.mt5_proxy_enabled = get_bool("IS_MT5_PROXY_ENABLED", false);
```

Prop extracted copy is the same (`D:\Prop\mt5-sdk\config\app_config.cpp:135`).

YoPips `.env` (secrets redacted; keys only):

| Key | Presence | Value published here |
|---|---|---|
| `MT5_MODE` | PRESENT | `local` (R012) |
| `MT5_SERVER` | PRESENT | `57.128.141.65` |
| `MT5_PORT` | PRESENT | `443` |
| `MT5_LOGIN` | PRESENT | `2027` |
| `MT5_PROXY_TYPE` | PRESENT | `HTTP` |
| `MT5_PROXY_ADDRESS` | PRESENT | `81.29.145.69` |
| `MT5_PROXY_PORT` | PRESENT | `49527` |
| `MT5_PROXY_LOGIN` / `MT5_PROXY_PASSWORD` | PRESENT | **REDACTED** |
| `MT5_PROXY_ENABLED` | PRESENT | `true` |
| `IS_MT5_PROXY_ENABLED` | **KEY_ABSENT** (R012) | — |

`MT5_PROXY_ENABLED=true` is **not read**. Result: recorded local starts logged `MT5 proxy mode: DISABLED (global)` then **1012**. This pass re-read those logs:

| Log | Proxy line | Connect |
|---|---|---|
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\runtime-backend-3001.out.log` 2026-07-16T15:49:48 | `MT5 proxy mode: DISABLED (global)` | **1012** pump (`15:50:16`); **1012** no-pump (`15:50:18`); pool sessions 1012 |
| `runtime-smoke-3002.out.log` 2026-07-16T15:41:17 | DISABLED | **1012** pump + no-pump |
| `local-runtime\backend-20260717-142524.stdout.log` | DISABLED | **1012** pump + no-pump |
| `local-runtime\backend-20260717-144426.stdout.log` | DISABLED | **1012** pump + no-pump |

That is the strongest empirical proof the HTTP hop is **required here**, not decorative. Pump-none does **not** bypass the allow-list.

### 2.5 C# product keys (Prop `.env`, gitignored)

| Key | Presence | Value published here |
|---|---|---|
| `ACHIEVER_PROXY_ENABLED` | PRESENT | `true` |
| `ACHIEVER_PROXY_HOST` | PRESENT | `81.29.145.69` |
| `ACHIEVER_PROXY_PORT` | PRESENT | `49527` |
| `ACHIEVER_PROXY_USERNAME` / `ACHIEVER_PROXY_PASSWORD` | PRESENT | **REDACTED** |
| `MT5_STARWAVEFX_PROXY_ENABLED` | PRESENT | `false` |
| `REAL_COPY_EXECUTION_ENABLED` | PRESENT | `false` |

`LiveMt5Registration` wires Achiever from `ACHIEVER_PROXY_*` and **hard-sets** Starwave `ProxyEnabled = false` (L45). The C++ key `IS_MT5_PROXY_ENABLED` is **not** what the C# worker reads.

If a host already source-NATs as `81.29.145.69`, HTTP proxy is unnecessary (A54 preference 1). That is **not** this desktop.

---

## 3. ALL Achiever + Starwave groups and ALL manager traders

### 3.1 Code path (no `demo\Maxmaster` universe)

`NativeMt5BrokerConnector.GetGroupsCore`:

1. `GroupRequestArray("*")` (L155) — request-complete, works with pump-none.
2. Fallback `GroupTotal` + `GroupNext` (L174–180) if the array is empty.

`GetAccountsCore(null)` walks **every** group from that list. Per group (`ReadAccountsForGroup`):

1. `UserRequestArray(gname)` (L223)
2. else `UserGetByGroup`
3. if still empty: `UserLogins` + `UserRequestByLogins`
4. account metrics: `UserAccountRequestArray` / `UserAccountGetByGroup`

Ingest / `LiveBrokerProbe` use `GetAccountsAsync(null)` — no `Take(` on the Manager walk. Architecture §7 still prints `MT5_DEFAULT_GROUP=demo\Maxmaster` as a **default**, not the universe. That group is **not** in the live Achiever dump.

### 3.2 Last measured census (not re-attached this slot)

Source: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
`probe=LiveBrokerProbe`, `utc=2026-08-18T08:42:16.8519545+00:00`, `envLoaded=true`, `connected=true` on **both** brokers.  
Write-up: `LIVE_MANAGER_FETCH_MEASURED.md` + `CREDENTIALS_AND_COPY_STATUS.md`.

| Broker | Connect | Groups | Traders | Open positions | Path |
|---|---|---:|---:|---:|---|
| ACHIEVER | OK via HTTP proxy | 8 | 6512 | 1506 | `GroupRequestArray` + `UserRequestArray` |
| STARWAVEFX | OK **direct** | 10 | 1948 | 478 | same |
| **Total** | | **18** | **8460** | **1984** | |

Dashboard (same day): `/api/groups` = **18**, `/api/traders` = **8460**.

Independent arithmetic this pass (JSON `groupNames[].accounts`):

**Achiever (8 groups → 6512):**

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
| **sum** | **6512** |

2+179+4+5+4+6295+0+23 = **6512**. Matches JSON `accounts`.

**Starwave (10 groups → 1948):**

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
| **sum** | **1948** |

11+4+170+1735+22+0+0+4+0+2 = **1948**. Matches JSON `accounts`.

JSON `connected: true` on Achiever is the empirical proof that `ProxySet` HTTP `81.29.145.69:49527` worked on the census machine. This slot did **not** re-authenticate (no password use).

These are **all groups / logins these two manager records can see**. Server-side groups outside the manager ACL are invisible by design (A39). Zero-account groups (`demo\yo-instant`, three Starwave `real` rows) are still **groups** and must stay in the catalog.

---

## 4. Copy to cTrader must not send live orders (no loss)

| Gate | Measured this pass |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` default | **false** (`CTraderFixOptions.cs:35`) |
| DI hard pin | `DependencyInjection.cs:40–41` — `RealCopyEnabled = false` with comment “Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.” |
| FIX hosted service | `CTraderFixLogonHostedService.cs:68` re-forces `_runtime.RealCopyEnabled = false` after QUOTE/TRADE logon |
| `.env` | `REAL_COPY_EXECUTION_ENABLED=false` |
| Settings API | `/api/settings` exposes `featureFlags.REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled` (stays false) |
| Snapshot copy note | “NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.” |
| FIX worker | Even if `CTrader:RealCopyExecutionEnabled=true`, worker **refuses** NewOrderSingle (logs warning; no send method) |

Wire builders this pass (`grep` `35=D` on `D:\Prop` `*.cs` / `*.tsx` / `*.js` / `*.cpp` / `*.h`): **0 hits**.

Only outbound MsgType in `CTraderFixSession` is Logon:

```96:96:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
            (35, "A"),
```

The only `WriteAsync` is that logon. Sockets are disposed. `35=D` / `35=F` / `35=G` builders **do not exist**. Same for `FixSimulationHarness` (also `35=A` only). Product safety is **`SAFE_BY_ABSENCE`**, not a unit-tested refuse-on-LoggedOn-TRADE gate.

FIX QUOTE TLS 5211 / TRADE TLS 5212 logon may be **true** (session only). That is **not** an order. cTrader is the **destination venue**, not an LP.

| Action | Allowed for slot-63 goal? | Why |
|---|---|---|
| Achiever `Connect` via HTTP `81.29.145.69:49527` | **Yes** | Else 1012 |
| Starwave `Connect` direct | **Yes** | No whitelist |
| `GroupRequestArray("*")` / `UserRequestArray` / deal+position **request** | **Yes** | Read-only Manager |
| Persist catalog / scores / shadow rows | **Yes** | Local DB; not a venue send |
| FIX TLS Logon `35=A` QUOTE/TRADE | **Session only** | Not an order |
| `NewOrderSingle` `35=D`, cancel `35=F`, replace `35=G` | **Forbidden** | No builder; flag false |
| `REAL_COPY_EXECUTION_ENABLED=true` | **Forbidden** | Would not send today, still must not be armed |

**Copy to cTrader is the destination. No-loss is the constraint. The constraint wins.** Do not tick §68 / §70 from this file.

---

## 5. Operator pin (slot 63)

1. Treat **1012** as `MT_RET_AUTH_MANAGER_IPBLOCK`. Fix **routing / `ProxySet` / ACL**, not the manager password (password fail is **3**). Do not confuse with **1016**.
2. On this LAN, Achiever **must** `ProxySet(PROXY_HTTP, 81.29.145.69:49527, user, pass)` **before** `Connect(57.128.141.65:443, 2027, …)`. Load `ACHIEVER_PROXY_ENABLED=true` (C# name). Do **not** export `HTTP_PROXY`.
3. Starwave stays **direct** to `84.201.6.142:443` login `9904`. A Starwave 1012 is unexpected; do **not** “fix” it with the Achiever hop.
4. Enumerate with `GroupRequestArray("*")` + `GetAccountsAsync(null)`. Do not hard-code `demo\Maxmaster`. Last measured set is **18 groups / 8460 traders**.
5. Keep `RealCopyEnabled=false`. Do not add `35=D`. Fetch + score + shadow only.
6. YoPips C++ still binds `IS_MT5_PROXY_ENABLED`. A probe that only sets `MT5_PROXY_ENABLED=true` will **repeat 1012** on this desktop.

---

## 6. What this agent did **not** do

- Did not print or copy `MT5_PASSWORD`, `MT5_STARWAVEFX_PASSWORD`, `ACHIEVER_PROXY_USERNAME`, `ACHIEVER_PROXY_PASSWORD`, `CTRADER_FIX_PASSWORD`, YoPips `MT5_PROXY_LOGIN` / `MT5_PROXY_PASSWORD`.
- Did not call `Connect`, `ProxySet`, or TCP SYN this pass (reused R012 + `LIVE_GROUPS_AND_TRADERS.json`).
- Did not edit `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `D:\Prop\mt5-sdk`, either `.env`, or YoPips source.
- Did not enable live copy, flatten, or write FIX `35=D/F/G`.
- Did not claim EX5 decompile progress or ≥95% copy parity.

---

## 7. Checklist

- [x] `MT_RET_AUTH_MANAGER_IPBLOCK=1012` confirmed in official SDK + YoPips C++ + Prop C++ + Prop C#.
- [x] Achiever HTTP proxy `81.29.145.69:49527` confirmed required on this LAN; type HTTP (`PROXY_HTTP=2`); Starwave excluded.
- [x] Historical YoPips `proxy mode: DISABLED` → **1012** pump and no-pump re-read this pass.
- [x] ALL groups / ALL manager traders: implemented (`*` / `null`) and measured (8+10 / 6512+1948). JSON group sums re-added independently.
- [x] Copy-to-cTrader live send **off**; no capital path from this process (`SAFE_BY_ABSENCE`).
- [x] Product source not edited. Secrets not printed.
