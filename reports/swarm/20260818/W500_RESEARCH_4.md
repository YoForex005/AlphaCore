# W500_RESEARCH_4 — Starwave must connect direct (no proxy)

| Field | Value |
|---|---|
| Agent | W500 slot 4 (senior engineer, read-only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (this pass: re-read of source + prior measured artifacts; **no new Manager Connect**) |
| Slot | **4** |
| Topic | Confirm StarwaveFX must connect **direct with no proxy** |
| Goal context | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders (no loss) |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_4.md` |
| Product source modified | **No.** Report only. |
| `.env` / credentials printed | **None.** Key names, flags, non-secret hosts/ports only. |
| Live Manager logon this pass | **Not re-run.** Cites the 2026-08-18T08:42:16Z `LiveBrokerProbe` census. |

---

## 0. Verdict (do not greenwash)

**CONFIRMED: StarwaveFX Manager TCP must stay DIRECT.**  
Do **not** call `ProxySet`. Do **not** reuse Achiever’s HTTP hop `81.29.145.69:49527`. Do **not** export process-wide `HTTP_PROXY`.

| Claim | Measured ruling |
|---|---|
| Architecture requires Starwave proxy off | **Yes.** §8 + §56: `MT5_STARWAVEFX_PROXY_ENABLED=false`. Quote: “No IP whitelist is currently required.” |
| Product C# applies a proxy to Starwave | **No.** `LiveMt5Registration` hardcodes `ProxyEnabled = false`. The env flag is **not read**. |
| Catalog/seed Starwave row has a proxy | **No.** `BrokerCatalogSeed` writes Starwave without `ProxyEnabled` / `ProxyHost` / `ProxyPort` (entity default `false` / null). Achiever is the opposite. |
| Operator `.env` Starwave proxy flag | **PRESENT = `false`.** No `MT5_STARWAVEFX_PROXY_HOST` / `_PORT` / `_USERNAME` / `_PASSWORD` keys (R022). |
| Direct TCP to Starwave Manager from this lab | **OPEN** `84.201.6.142:443` in **189 ms** (R012 SYN-only; no auth). |
| Live Starwave Manager session on the direct path | **PROVEN** 2026-08-18T08:42:16Z: `connected=true`, **10 groups / 1948 traders / 478 open positions**, `elapsedMs=6413`. |
| Achiever uses the same hop | **No.** Achiever **requires** HTTP `ProxySet` on this desktop (else **1012**). Different broker, different ACL. |
| YoPips C++ backend has a Starwave slot | **No.** Single `AppConfig` broker. Proxy toggle is global `IS_MT5_PROXY_ENABLED`. YoPips `.env` is Achiever + HTTP proxy keys; **no** `MT5_STARWAVEFX_*`. |
| Using Achiever’s proxy for Starwave is implemented | **No.** A58 / R022: inventing `MT5_STARWAVEFX_PROXY_HOST` = Achiever hop is a **reject**. |
| Starwave-via-Achiever-proxy A/B Connect exists | **No.** Direct success is measured. Hairpin failure is **not** experimentally proven; it is **forbidden by design**. |
| Copy to cTrader can place a live order today | **No.** `RealCopyEnabled` forced `false`. `CTraderFixSession` builds **35=A only**. **Zero** `35=D` NewOrderSingle. |

Honest one-liner: **Starwave = `Connect(84.201.6.142:443, 9904, password, pump)` with `ProxySet` skipped; Achiever = HTTP proxy then Connect; both catalogs already measured; live cTrader send remains impossible.**

---

## 1. Why “must” is the right word

Three independent constraints agree. None of them is “Starwave TCP is closed without a proxy.”

1. **Law (architecture).** Starwave has no allow-list. The published flag is `false`. The Achiever hop exists to present `81.29.145.69` to **Achiever’s** Manager ACL, not Starwave’s.
2. **Product wiring.** The only live C# factory **cannot** turn Starwave proxy on without a code change. Setting `MT5_STARWAVEFX_PROXY_ENABLED=true` is a no-op.
3. **Measured success.** The all-groups / all-traders census that satisfies the parent goal used that forced-direct connector and returned a full Starwave book.

R022 (scope pin, same day) is explicit: **StarwaveFX Manager TCP must not** use `81.29.145.69:49527`. Process-wide `HTTP_PROXY` is **FORBIDDEN** because it would steal Starwave, FIX, restore, and health onto the Achiever hop.

What is **not** claimed: that a hypothetical generic HTTP CONNECT in front of `84.201.6.142:443` would fail. No such experiment was run. The standing order is still **do not try it** — wrong identity, extra SPOF, and it would invent keys A58 rejected.

---

## 2. Architecture (binding text)

File: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`

### 2.1 Achiever contrast — §7

Required whitelisted outbound IP: `81.29.145.69`.  
If the worker’s public egress is not that address, Manager `ProxySet` is how the lab presents it.

This desktop’s public egress (R012, three oracles): **`106.219.132.213`** ≠ allow-list. That is why Achiever needs the hop.

### 2.2 Starwave — §8 (non-secret)

```env
MT5_STARWAVEFX_SERVER=84.201.6.142
MT5_STARWAVEFX_PORT=443
MT5_STARWAVEFX_LOGIN=9904
MT5_STARWAVEFX_PROXY_ENABLED=false
```

Quoted: **“No IP whitelist is currently required.”**  
Quoted: **“Still design the connector so proxy/whitelist routing can be enabled later.”**

“Design later” is a **field on the same connector**, not a license to flip the flag or reuse Achiever’s host:port.

### 2.3 Secret-safe example — §56

Same digits again: Achiever proxy **on** (`ACHIEVER_PROXY_ENABLED=true`, host `81.29.145.69`, port `49527`); Starwave **`MT5_STARWAVEFX_PROXY_ENABLED=false`**.  
`REAL_COPY_EXECUTION_ENABLED=false` in the same block.

§56 has **no** `MT5_STARWAVEFX_PROXY_HOST` / `_PORT` / `_USERNAME` / `_PASSWORD`.

---

## 3. Product C# — Starwave cannot take the proxy path

### 3.1 Factory hardcodes `ProxyEnabled = false`

`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` (current tree):

- Achiever (lines 23–36): reads `ACHIEVER_PROXY_ENABLED` / `HOST` / `PORT` / `USERNAME` / `PASSWORD`.
- Starwave (lines 38–47):

```csharp
var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
{
    BrokerCode = BrokerCodes.StarwaveFx,
    Server = config["MT5_STARWAVEFX_SERVER"] ?? "",
    Port = int.TryParse(config["MT5_STARWAVEFX_PORT"], out var sp) ? sp : 443,
    Login = ulong.TryParse(config["MT5_STARWAVEFX_LOGIN"], out var sl) ? sl : 0,
    Password = config["MT5_STARWAVEFX_PASSWORD"] ?? "",
    ProxyEnabled = false,          // literal
    NativeDllDirectory = dllDir
});
```

`MT5_STARWAVEFX_PROXY_ENABLED` is **not** a configuration key this factory binds (A008). Operator can set it; the process ignores it.

### 3.2 `ProxySet` is skipped when the flag is false

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `ApplyProxy()` (lines 115–118):

```csharp
if (_manager is null || !_opt.ProxyEnabled || string.IsNullOrWhiteSpace(_opt.ProxyHost))
    return;
```

Starwave therefore never builds `MTProxyInfo` and never calls `ProxySet`.  
`Connect` uses `$"{Server}:{Port}"` only — for Starwave that is `84.201.6.142:443`.

Pump: `PUMP_MODE_GROUPS | USERS | POSITIONS`, then `PUMP_MODE_NONE` fallback. Proxy is independent of pump.

Error hint at line 447 is Achiever-scoped: `1012 => "manager IP blocked — Achiever requires the whitelist HTTP proxy"`. A Starwave 1012 would be unexpected (R022 rule 6: **do not** “fix” it by pointing Starwave at `81.29.145.69:49527`).

### 3.3 Catalog seed matches the split

`D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`:

| Broker | Server | Port | Manager | ProxyEnabled | ProxyHost:Port |
|---|---|---:|---:|---|---|
| Achiever | `57.128.141.65` | 443 | 2027 | **true** | `81.29.145.69:49527` |
| StarwaveFX | `84.201.6.142` | 443 | 9904 | **omitted → false** | omitted / null |

`Broker` entity (`D:\Prop\src\Domain\Entities\Broker.cs`): `ProxyEnabled` defaults false; **no** proxy-auth columns (correct).

### 3.4 ALL groups / ALL logins walk (no Take(N) on the manager)

`GetGroupsCore`: `GroupRequestArray("*")`, then `GroupTotal` / `GroupNext` if empty.  
`GetAccountsCore(null)`: walks **every** group from `GetGroupsCore`, then `UserRequestArray` / `UserGetByGroup` / `UserLogins` + `UserRequestByLogins`. Dedupes by login. **No** `.Take(200)` on the live account walk.

`LiveBrokerProbe` (`D:\Prop\tools\LiveBrokerProbe\Program.cs`) constructs connectors via `LiveMt5Registration.CreateConnectorsFromEnvironment()` — same forced-direct Starwave slot — then `GetGroupsAsync` + `GetAccountsAsync(null)` + bulk positions `"*"`.

---

## 4. Measured live census (direct Starwave)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe`  
UTC: **2026-08-18T08:42:16.8519545+00:00**  
`envLoaded`: **true**  
Note in file: “Passwords never written. Groups and manager logins only.”

| Broker | `connected` | `elapsedMs` | Groups | Accounts | Open positions | Path |
|---|---|---:|---:|---:|---:|---|
| ACHIEVER | true | 7212.5885 | 8 | 6512 | 1506 | HTTP `ProxySet` then Connect |
| STARWAVEFX | true | 6413.478 | 10 | 1948 | 478 | **direct** (`ProxyEnabled=false`) |
| **Total** | | | **18** | **8460** | **1984** | |

Starwave groups (JSON `groupNames`; account counts sum to **1948**):

| Group | Currency | Accounts |
|---|---|---:|
| `Starwave\cent\FX1\grp1` | USC | 11 |
| `Starwave\cent\FX1\grp2` | USC | 4 |
| `Starwave\demo\FX2\grp1` | USD | 170 |
| `Starwave\demo\FX2\grp2` | USD | 1735 |
| `Starwave\real\FX3\grp1` | USD | 22 |
| `Starwave\real\FX3\grp2` | USD | 0 |
| `Starwave\real\FX3\grp3` | USD | 0 |
| `Starwave\real\FX3\grp4` | USD | 4 |
| `Starwave\real\FX3\grp5` | USD | 0 |
| `Starwave\real\FX3\LP` | USD | 2 |
| **Sum** | | **1948** |

11+4+170+1735+22+0+0+4+0+2 = **1948**. Matches `accounts`.

Cross-check write-up: `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` and `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` (Starwave: **OK direct**).  
`D:\Prop\reports\SWARM_LOG.md` live-census row: “Starwave 10 groups / 1948 traders **(direct)**.”  
`D:\Prop\reports\INDEX.md` same integers.

These are **all groups this manager login can see**. Empty real groups are still enumerated (mapping-blind). Logins are not globally unique: persist `(broker_id, login)` (A004).

### 4.1 Network (no auth) — R012

Host `DESKTOP-FQPFPKE`. Default route `192.168.1.59` → `192.168.1.1`. Public egress **`106.219.132.213`**.

| Target | Role | Result |
|---|---|---|
| `84.201.6.142:443` | Starwave Manager | **OPEN 189 ms** |
| `57.128.141.65:443` | Achiever Manager | **OPEN 309 ms** (TCP open ≠ ACL pass) |
| `81.29.145.69:49527` | Achiever HTTP proxy | **OPEN 199 ms** |
| `81.29.145.69:443` | not the hop | **REFUSED** |

Starwave does not need the hop for reachability. Achiever does not fail on reachability; it fails on **1012** when source ≠ `81.29.145.69`.

### 4.2 Operator env (names/flags only)

`D:\Prop\.env` (gitignored). R012/R022 classification, reconfirmed this pass by key-name grep only:

| Key | Class | Published here |
|---|---|---|
| `MT5_STARWAVEFX_SERVER` | non-secret | `84.201.6.142` (architecture / seed) |
| `MT5_STARWAVEFX_PORT` | non-secret | `443` |
| `MT5_STARWAVEFX_LOGIN` | non-secret | `9904` |
| `MT5_STARWAVEFX_PASSWORD` | **SECRET** | PRESENT (not printed) |
| `MT5_STARWAVEFX_PROXY_ENABLED` | non-secret | **`false`** |
| `MT5_STARWAVEFX_PROXY_HOST` etc. | — | **NO_KEY** (correct) |
| `ACHIEVER_PROXY_ENABLED` | non-secret | `true` |
| `ACHIEVER_PROXY_HOST` / `PORT` | non-secret | `81.29.145.69` / `49527` |
| `ACHIEVER_PROXY_*` auth | **SECRET** | PRESENT, not printed |
| `IS_MT5_PROXY_ENABLED` / `MT5_PROXY_*` | C++ names | **KEY_ABSENT** in Prop `.env` |

---

## 5. YoPips C++ backend — no Starwave slot; proxy is global

Tree: `D:\Projects\YoPips\Backend\C++ Backend PropFirm`

| Surface | Fact |
|---|---|
| `config\app_config.h` | One `mt5_server` / `mt5_login` / `mt5_password`. One `mt5_proxy_enabled` (default **false**). **Zero** `STARWAVE` identifiers in `.h` / `.cpp`. |
| `config\app_config.cpp` L172 / Prop copy L135 | `cfg.mt5_proxy_enabled = get_bool("IS_MT5_PROXY_ENABLED", false);` |
| `src\main.cpp` L201–225 | `SetProxy` **only** if that flag is true and type/address/port complete; else logs `MT5 proxy mode: DISABLED (global)` and connects **direct**. Pool uses the same toggle (L249–269). |
| Probe | `tests\mt5_group_probe.cpp` — same recipe. Achiever and Starwave are **two process env sets**, not one dual-broker binary (A004). |
| YoPips `.env` | Achiever host `57.128.141.65`, `MT5_PROXY_TYPE=HTTP`, `MT5_PROXY_ADDRESS=81.29.145.69`, `MT5_PROXY_PORT=49527`, `MT5_PROXY_ENABLED=true`. **`IS_MT5_PROXY_ENABLED` KEY_ABSENT** → recorded local starts logged DISABLED then **1012**. Auth values **not** copied here. **No `MT5_STARWAVEFX_*` keys.** |

Operator implication if someone “just points C++ at Starwave”:

1. Swap `MT5_SERVER=84.201.6.142`, `MT5_LOGIN=9904`, password env.  
2. Leave **`IS_MT5_PROXY_ENABLED` unset/false**. Do **not** turn the Achiever HTTP stanza on.  
3. Do **not** run Achiever and Starwave in one C++ process that shares one proxy flag.

Prop `mt5-sdk` `AppConfig` is the same single-broker shape (`D:\Prop\mt5-sdk\config\app_config.h`). C# is the dual-broker process; C++ is not.

---

## 6. Copy to cTrader — no live orders (no loss)

Parent goal: fetch the full books **and** do not send live destination orders.

| Gate | Evidence | Result |
|---|---|---|
| `LiveRuntimeStatus.RealCopyEnabled` | `DependencyInjection.cs` L38–41 **forced false** (“Live NewOrderSingle is not implemented”) | **OFF** |
| Hosted FIX | `CTraderFixLogonHostedService.cs` L68 `_runtime.RealCopyEnabled = false` after logon | **OFF** |
| Settings API | `apps/api/Program.cs` L75 `featureFlags.REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled` | **false** |
| Wire builder | `CTraderFixSession.BuildLogon` tags: **35=A**, 34, 49, 56, 50, 57, 52, 98, 108, 141, **553**, **554**. **No 35=D.** | Logon only |
| `NewOrderSingle` in `Fix.CTrader` | 2 hits: XML comment on `RealCopyExecutionEnabled`; log string “NewOrderSingle still disabled” | name only |
| FIX worker | `apps/fix-worker/Worker.cs` stamps TRADE `LastError = "No live TRADE socket. NewOrderSingle remains off."` even if nested flag true | refuse |
| Tag 553 | Hosted service uses **integer account id**, not SenderCompID (comment L45–46) | logon-only fix |
| Snapshot copy note | `LiveRuntimeStatus.Snapshot`: “NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.” | honest |

A003 / CREDENTIALS_AND_COPY_STATUS: live `35=D` **OFF — method does not exist**. Safety is **`SAFE_BY_ABSENCE`**, not a fully tested refuse-on-LoggedOn-TRADE gate. That is still **no capital at risk** from this process.

Do **not** set `REAL_COPY_EXECUTION_ENABLED=true`. Do **not** add NewOrderSingle while fetching groups.

---

## 7. Stale reports (do not resurrect)

| Report | Stale claim | Current measured state |
|---|---|---|
| C42 (`C42_honesty_no_live_mt5.md`) | Live Achiever/Starwave **NOT proven**; Fake only | **Overturned** by `LIVE_GROUPS_AND_TRADERS.json` + native connector |
| R022 §0 last paragraph | “C# still runs two fakes (C42)” | **Stale.** `NativeMt5BrokerConnector` + `LiveMt5Registration` are wired; DemoSeeder is off API startup |
| A008 §9 | API does **not** load `D:\Prop\.env` | **Stale.** `apps/api/Program.cs` L9 `EnvFile.FindAndLoad()` **before** `CreateBuilder` |
| Architecture “optional proxy” comment | Sounds optional for Achiever | Lab pin: Achiever hop **required** on this PC; Starwave still **off** |

---

## 8. Operator pin (slot 4)

1. **Starwave Connect:** `84.201.6.142:443`, manager `9904`, password from `MT5_STARWAVEFX_PASSWORD`. **No `ProxySet`.**  
2. **Do not** set `MT5_STARWAVEFX_PROXY_ENABLED=true`. C# ignores it today; turning it on later **must not** copy Achiever’s host.  
3. **Do not** export `HTTP_PROXY` / `HTTPS_PROXY` / `ALL_PROXY` to `81.29.145.69:49527`.  
4. **Do not** enable YoPips `IS_MT5_PROXY_ENABLED` on a process aimed at Starwave.  
5. **Achiever stays proxied** on this LAN (`ACHIEVER_PROXY_ENABLED=true`, HTTP, `81.29.145.69:49527`). Mixing the two slots is how you get Starwave hairpinned or Achiever 1012.  
6. Enumerate **all** manager-visible groups (`*` / `GroupTotal`) and **all** logins per group. Dummy seed is off.  
7. Copy path: logon/recon only. **`RealCopyEnabled=false`.** No `35=D`.

---

## 9. What this slot did **not** do

- Did not print manager passwords, proxy user/password, or FIX password.  
- Did not run a new `Connect` / `ProxySet` / HTTP CONNECT.  
- Did not edit `D:\Prop\src`, `apps`, `mt5-sdk`, either `.env`, or YoPips source.  
- Did not A/B Starwave through the Achiever hop (forbidden; not required to confirm the standing path).  
- Did not claim live copy-trading.

---

## 10. Done criteria

- [x] Architecture §8 / §56 Starwave `PROXY_ENABLED=false` and “no whitelist” quoted.  
- [x] C# factory + `ApplyProxy` skip path line-cited.  
- [x] Catalog seed / entity defaults recorded.  
- [x] Live census integers (10 / 1948 / 478, 6413 ms, `connected=true`) cited from JSON.  
- [x] R012 TCP OPEN 189 ms on `84.201.6.142:443` cited.  
- [x] YoPips C++ single-broker + global proxy toggle + no Starwave keys recorded.  
- [x] Process-wide `HTTP_PROXY` forbidden (R022).  
- [x] Copy/no-loss: no `35=D`, `RealCopyEnabled` forced false.  
- [x] Secrets not printed. Product source not modified.

**Slot 4 verdict: CONFIRMED — Starwave must connect direct, no proxy.**
