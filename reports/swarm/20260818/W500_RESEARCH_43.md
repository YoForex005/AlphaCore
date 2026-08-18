# W500_RESEARCH_43 — Confirm `MT_RET_AUTH_MANAGER_IPBLOCK=1012` and Achiever HTTP proxy `81.29.145.69:49527`

| Field | Value |
|---|---|
| Slot | **43** |
| Agent | W500_RESEARCH_43 (senior engineer, read-only) |
| Date | 2026-08-18 |
| Measured at | This pass: official SDK headers + wrappers in **both** Prop and YoPips trees; C# `ApplyProxy` / `Describe`; live census JSON already on disk (`2026-08-18T08:42:16.8519545+00:00`) |
| Assigned | Confirm `MT_RET_AUTH_MANAGER_IPBLOCK=1012` and that Achiever requires HTTP proxy `81.29.145.69:49527`. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_43.md` |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Config / `.env` edited | **No.** Keys classified PRESENT + non-secret identifiers only. |
| Secret values printed | **None.** No Manager password, no proxy user/password, no FIX password. |
| Live Manager logon this pass | **Not re-run.** Prior measured census: `LIVE_GROUPS_AND_TRADERS.json` + `LIVE_MANAGER_FETCH_MEASURED.md`. |
| Binding law | Official MetaQuotes `EnMTAPIRetcode`; architecture §§7, 8, 41, 56, 68, 70; C55 (egress IP non-secret); R012 (this LAN needs the hop); R022 (Achiever-only scope); A39 (ALL groups = manager-visible set); E002/E034 (`SAFE_BY_ABSENCE`) |

This is a **slot-43 confirmation pin**. It does not whitelist IPs, does not send `35=D`, and does not invent groups outside the manager ACL.

---

## 0. Verdict (binding)

**CONFIRMED on every assigned claim.**

| Claim | Result | Evidence class |
|---|---|---|
| Official retcode `MT_RET_AUTH_MANAGER_IPBLOCK` equals **1012** | **Yes** | MetaQuotes `EnMTAPIRetcode` in Prop **and** YoPips Manager SDK. Same integer in C++ wrapper, PHP, .NET WebAPI, C# `Describe`. |
| Official English | “IP address unallowed for manager” | `MT5APIFormat.h` + PHP/NET examples |
| Achiever Manager from this lab needs HTTP proxy **`81.29.145.69:49527`** | **Yes** | Allow-list identity `81.29.145.69` (non-secret). Type `MTProxyInfo::PROXY_HTTP = 2`. This desktop public egress measured `106.219.132.213` ≠ allow-list. Historical YoPips local Connect with proxy **disabled** = **1012** on pump **and** no-pump. Live census later connected Achiever **via that HTTP hop**. |
| StarwaveFX uses the same hop | **No** | `LiveMt5Registration` hard-sets `ProxyEnabled = false`. Architecture §56 `MT5_STARWAVEFX_PROXY_ENABLED=false`. R022: do **not** hairpin Starwave through `:49527`. |
| Fetch **ALL** Achiever + Starwave groups | **Implemented + measured** | `GroupRequestArray("*")` then `GroupTotal`/`GroupNext` fallback. Live **8 + 10 = 18**. |
| Fetch **ALL** manager traders | **Implemented + measured** | `GetAccountsAsync(null)` walks every group via `UserRequestArray` / `UserGetByGroup` / `UserLogins`+`UserRequestByLogins`. Live **6512 + 1948 = 8460**. |
| Copy to cTrader may send live orders today | **No** | `RealCopyEnabled` forced **false**. Product C# has **0** `(35, "D")` builders. FIX logon is `35=A` only. |
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

| Surface | Path | Mapping |
|---|---|---|
| Prop C++ wrapper | `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp:64` | `case 1012:` → “IP blocked by MT5 server (`MT_RET_AUTH_MANAGER_IPBLOCK`)…” |
| YoPips C++ wrapper | `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp:64` | **identical** string |
| Official PHP example | `mt5_retcode.php:42` | `const MT_RET_AUTH_MANAGER_IPBLOCK = 1012` |
| Official .NET WebAPI example | `MTRetCode.cs:46` (Prop + YoPips) | `MT_RET_AUTH_MANAGER_IPBLOCK = 1012` |
| Prop C# native connector | `NativeMt5BrokerConnector.cs:447` | `1012 => "manager IP blocked — Achiever requires the whitelist HTTP proxy"` |

Do **not** confuse with other official `1012` tokens in unrelated enums (`ACTION_ACCOUNT_ARCHIVE`, `CONDITION_ACCOUNT_LEVERAGE`, `FIELD_CLIENT_COMPLIANCE_TIME_APPROVAL`, Win32 `IDC_BUTTON_DEPOSIT`). Those are **not** Manager auth retcodes.

**What 1012 is not:** a closed TCP port. R012 measured `57.128.141.65:443` **OPEN** (309 ms) from this LAN while historical Connect still returned **1012**. Reachability ≠ allow-list. A 1012 deny happens **before** a Manager session exists: no pump, no `GroupRequestArray`, no `DealAdd` / `OrderAdd` / `DealerBalance`. Equity cannot move on a connection that never authenticated.

---

## 2. Achiever requires HTTP proxy `81.29.145.69:49527` (this workstation)

Two non-secret keys share the same IPv4 (C55). Mixing them is how operators send Starwave through the wrong hop or treat the port as a secret.

| Key | Job | Failure if omitted on this LAN |
|---|---|---|
| `ACHIEVER_EGRESS_IP=81.29.145.69` | Source IP Achiever’s Manager ACL must see. **Not** a `Connect()` argument. | **1012** |
| `ACHIEVER_PROXY_HOST=81.29.145.69` + `ACHIEVER_PROXY_PORT=49527` | Optional hop that **presents** that source IP via Manager `ProxySet`. | Direct Connect from this LAN → **1012** |

Architecture §7 (binding, non-secret):

```text
Required whitelisted outbound IP:
81.29.145.69
```

Architecture §56 secret-safe sample (passwords are `<SECRET>` only):

```text
ACHIEVER_EGRESS_IP=81.29.145.69
ACHIEVER_PROXY_ENABLED=true
ACHIEVER_PROXY_HOST=81.29.145.69
ACHIEVER_PROXY_PORT=49527
```

Prop `.env` (gitignored; this pass classified keys only, **values of secrets not copied**):

| Key | Presence | Published here |
|---|---|---|
| `ACHIEVER_EGRESS_IP` | PRESENT | `81.29.145.69` |
| `ACHIEVER_PROXY_ENABLED` | PRESENT | `true` |
| `ACHIEVER_PROXY_HOST` | PRESENT | `81.29.145.69` |
| `ACHIEVER_PROXY_PORT` | PRESENT | `49527` |
| `ACHIEVER_PROXY_USERNAME` / `PASSWORD` | PRESENT | **REDACTED** |

Catalog seed (non-secret host/port; live `ProxySet` still comes from env via `LiveMt5Registration`, not this row):

```27:29:D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs
                ProxyEnabled = true,
                ProxyHost = "81.29.145.69",
                ProxyPort = 49527,
```

### 2.1 Hop type is HTTP, applied before Connect

Vendor:

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

Live registration (Achiever from env; Starwave **forced off**):

```30:45:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
            ProxyEnabled = bool.TryParse(config["ACHIEVER_PROXY_ENABLED"], out var pe) && pe,
            ProxyHost = config["ACHIEVER_PROXY_HOST"],
            ProxyPort = int.TryParse(config["ACHIEVER_PROXY_PORT"], out var pp) ? pp : 0,
            ...
            BrokerCode = BrokerCodes.StarwaveFx,
            ...
            ProxyEnabled = false,
```

This is **not** a process-wide `HTTP_PROXY`. R012: Process/User/Machine `HTTP_PROXY` / `HTTPS_PROXY` were **UNSET**. Exporting `HTTP_PROXY=http://81.29.145.69:49527` is **forbidden** (would steal Starwave, FIX, restore, and health onto the Achiever hop).

### 2.2 Measured topology (R012, same host class, 2026-08-18)

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

TCP OPEN on `:443` **and** `:49527` proves reachability. Historical YoPips **local** starts on this class of machine logged `MT5 proxy mode: DISABLED (global)` then **1012** on pump **and** no-pump (`runtime-backend-3001.out.log` 2026-07-16T15:49:48 and four later local-runtime logs — R012 §4). That is the measured failure mode this pin exists to avoid.

Would local connect work **without** proxy if the worker already source-NATed as `81.29.145.69`? **Yes, in principle** (A54 preference 1). That is **not** this desktop.

### 2.3 C++ toggle trap (YoPips + extracted Prop SDK)

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

`main.cpp` only calls `SetProxy` when that flag is true; else it logs `MT5 proxy mode: DISABLED (global)` and connects **direct**. `MT5_PROXY_ENABLED=true` is **not read**. A C++ probe that only sets `MT5_PROXY_ENABLED` will **repeat 1012** here.

C# product uses **different** keys (`ACHIEVER_PROXY_*`). Do not mix the two families.

---

## 3. Fetch ALL groups and ALL manager traders

Architecture §7: `demo\Maxmaster` is **not** the only group. The system must enumerate **all groups accessible to the Manager login**.

C# implementation (`NativeMt5BrokerConnector`):

1. Pump includes `PUMP_MODE_GROUPS | PUMP_MODE_USERS | PUMP_MODE_POSITIONS`.
2. `GetGroupsCore`: `GroupRequestArray("*")`; if empty, `GroupTotal` + `GroupNext`.
3. `GetAccountsCore(null)`: walk **every** group name, then `UserRequestArray` → `UserGetByGroup` → `UserLogins` + `UserRequestByLogins`, plus `UserAccountRequestArray` / `UserAccountGetByGroup` for balances.

There is **no** hard-coded `demo\Maxmaster` filter on the live walk. `DealAdd` / `OrderAdd` / `DealerBalance` / `TradeRequest` are **absent** from `D:\Prop\src` (this pass grep: **0**). Catalog is read-only.

### 3.1 Measured live census (not re-run this slot)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` at `2026-08-18T08:42:16.8519545+00:00`  
Summary: `LIVE_MANAGER_FETCH_MEASURED.md`

| Broker | Connect | Groups | Traders | Open positions | Path |
|---|---|---:|---:|---:|---|
| ACHIEVER | **OK via HTTP proxy** | 8 | 6512 | 1506 | `GroupRequestArray` + `UserRequestArray` |
| STARWAVEFX | **OK direct** | 10 | 1948 | 478 | same |
| **Total** | | **18** | **8460** | **1984** | |

Achiever groups (manager-visible set; `demo\Maxmaster` **not** in the dump):

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
| **sum** | **1948** |

Honesty: these are **all groups this manager login can see**. If the server has more groups, they are outside this manager’s permission set. Empty groups (`demo\yo-instant`, several Starwave real slots) are still **enumerated** — that is required for “ALL”.

JSON `utc` + `connected: true` on **both** brokers is the empirical proof that Achiever `ProxySet` HTTP `81.29.145.69:49527` worked on the census machine. This slot did **not** re-authenticate (no password use).

---

## 4. Copy to cTrader must not send live orders yet (no loss)

Goal language is “copy to cTrader” **and** “no loss.” Those two cannot both be live-fill today. The only honest operating mode is **catalog + FIX logon/recon + SHADOW intents**. Live `35=D` stays off.

| Control | Measured state |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** |
| `DependencyInjection` | `RealCopyEnabled = false` with comment: “Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.” |
| `CTraderFixLogonHostedService` | forces `_runtime.RealCopyEnabled = false`; log: “NewOrderSingle still disabled” |
| `/api/settings` | `featureFlags.REAL_COPY_EXECUTION_ENABLED` = `runtime.RealCopyEnabled` (false) |
| `/api/reconciliation/status` | note: “NewOrderSingle still off” |
| `LiveRuntimeStatus.copyNote` when false | “NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.” |
| `CTraderFixSession.BuildLogon` | `(35, "A")` only. Tags 34/49/56/50/57/52/98/108/141/553/554. **No** `(35, "D")`. |
| `Fix.CTrader` product send of `35=D` | **0 builders** (harness has `35=8` ExecutionReport / `35=A` Logon / MD — not a live sender) |
| `apps/fix-worker/Worker.cs` | even if `CTrader:RealCopyExecutionEnabled=true`, it **stamps Disconnected** and **refuses** NewOrderSingle |
| `EfTradingStore.PersistDemoShadowAsync` | writes `CopyIntent` with `Status = "SHADOW_ONLY"`; `ShadowCopyEngine.SimulateEntry` — **no** FIX write |
| MT5 mutators in product C# | **0** `DealAdd` / `OrderAdd` / `DealerBalance` |
| Architecture §41 / §56 / §68 / §70 | `REAL_COPY_EXECUTION_ENABLED=false` until 19/19 + 14/14 + explicit review. Current go-live scoreboards remain **0 PASS**. |

A 1012 on Achiever is a **Manager logon deny**. Native `Connect` throws; `LiveIngestHostedService` catch sets `Connected=false` and **does not substitute dummy data**. 1012 cannot open, close, or resize cTrader risk.

FIX QUOTE/TRADE logon (`35=A`) may be up for quotes/recon. **Session-on is not a send license.** Tag 553 must be the integer account id (not SenderCompID). Password never logged.

---

## 5. Operator checklist (this LAN)

1. **Achiever:** `ACHIEVER_PROXY_ENABLED=true`, host `81.29.145.69`, port `49527`, type **HTTP**, user/pass in env only. Then `Connect` `57.128.141.65:443` login `2027`.
2. **C++ probes:** set `IS_MT5_PROXY_ENABLED=true` (not only `MT5_PROXY_ENABLED`). Else historical **1012**.
3. **Starwave:** `ProxyEnabled=false`. Direct `84.201.6.142:443` login `9904`. Do **not** reuse `:49527`.
4. Do **not** export process-wide `HTTP_PROXY` / `HTTPS_PROXY` / `ALL_PROXY`.
5. Enumerate with `GroupRequestArray("*")` + per-group user walk. Do **not** assume `demo\Maxmaster`.
6. Keep `REAL_COPY_EXECUTION_ENABLED=false`. Do **not** add `35=D` to “try one lot.”
7. If `sdk_reason` / `Describe` shows **1012**: fix routing/proxy/toggle — **not** the password slot. If **3**: credentials. If **7/5**: network / host:port. If **1016**: server-identity, not manager ACL.

---

## 6. What this slot did **not** do

- Did not print or copy Manager / proxy / FIX passwords.
- Did not edit product, test, or `.env` files.
- Did not re-run `Connect` / `ProxySet` / TCP probes this pass (R012 + live census already measured).
- Did not enable live copy.
- Did not claim EX5 decompilation or ≥95% copy-trading parity.
- Did not claim Starwave would return 1012 through the Achiever hop (that path is **forbidden**, not measured).

---

## 7. Checklist

- [x] Official SDK pin: `MT_RET_AUTH_MANAGER_IPBLOCK = 1012` cited from MetaQuotes headers in **both** Prop and YoPips trees.
- [x] Distinguished from `MT_RET_AUTH_INVALID_IP = 1016` and from unrelated `1012` tokens in other enums.
- [x] Achiever HTTP hop `81.29.145.69:49527` (`PROXY_HTTP=2`) confirmed as required on this LAN; Starwave excluded.
- [x] C# `ACHIEVER_PROXY_*` vs C++ `IS_MT5_PROXY_ENABLED` toggle trap recorded.
- [x] Historical 1012-without-proxy + later live census-with-proxy both cited.
- [x] ALL manager-visible groups/traders: **18 / 8460** (8+10 / 6512+1948).
- [x] Copy-to-cTrader live send **off** (`SAFE_BY_ABSENCE` + flag false).
- [x] Secrets not printed. Product source not edited.

**Slot 43 verdict: CONFIRMED. Risk to capital: none.**
