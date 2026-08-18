# W500_RESEARCH_24 — StarwaveFX must connect **direct** (no proxy)

| Field | Value |
|---|---|
| Slot | **24** |
| Date | 2026-08-18 |
| Topic | Confirm Starwave must connect **direct with no proxy** |
| Goal context | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy-to-cTrader must **not** send live orders (no loss). |
| Method | `read_file` / `grep` on `D:\Prop` product + reports; `D:\Projects\YoPips\Backend\C++ Backend PropFirm` (`main.cpp`, `app_config.*`, `mt5_manager.cpp`). No Manager reconnect this slot. No product source edit. |
| Product source modified | **No** |
| Secrets printed | **None.** No Manager passwords, no proxy user/pass, no FIX password, no `DATABASE_URL`. Auth classified PRESENT/length only where already published. |
| Verdict | **CONFIRMED** |

---

## 0. Verdict (binding)

**StarwaveFX Manager must connect direct. Do not call `ProxySet`. Do not reuse the Achiever HTTP hop.**

| Claim | Result | Class |
|---|---|---|
| Architecture §8 / §56 set `MT5_STARWAVEFX_PROXY_ENABLED=false` | **Yes** | design law |
| Product C# **forces** Starwave `NativeMt5Options.ProxyEnabled = false` | **Yes** (`LiveMt5Registration.cs` L45). Env key is **not** read. | code pin |
| Starwave seed / catalog rows leave `Broker.ProxyEnabled` default **false** | **Yes** (`BrokerCatalogSeed` / `DemoSeeder`) | catalog |
| Live Manager census succeeded **direct** | **Yes** — 10 groups / 1948 traders / 478 open positions | measured 2026-08-18T08:42:16Z |
| This LAN can TCP-reach Starwave `:443` without a hop | **Yes** — `84.201.6.142:443` **OPEN** 189 ms (R012) | measured SYN |
| Starwave has an Achiever-style IP whitelist | **No** (§8: “No IP whitelist is currently required”) | design |
| Achiever HTTP hop (`81.29.145.69:49527`) is in Starwave’s scope | **No** — Achiever-only (R022). Hairpin is **forbidden**. | scope pin |
| C++ YoPips PropFirm has a Starwave slot / `MT5_STARWAVEFX_*` | **No** — single-broker `IS_MT5_PROXY_ENABLED` + `MT5_SERVER` | C++ census |
| Starwave-via-Achiever-proxy was attempted this slot | **No** | honesty |
| Copy-to-cTrader can place a live order from this tree | **No** — no `35=D` sender; `RealCopyEnabled` forced **false** | no-loss |

Honest one-liner: **Starwave = `Connect(84.201.6.142:443, 9904, password, pump)` with `ProxySet` skipped. Achiever’s HTTP proxy exists to present `81.29.145.69` to Achiever’s ACL, not to Starwave. Live census of all manager-visible groups/logins already ran on that direct path. NewOrderSingle stays off.**

Do **not** “fix” a Starwave failure by flipping `MT5_STARWAVEFX_PROXY_ENABLED` or pointing Starwave at `ACHIEVER_PROXY_HOST`. Do **not** export process `HTTP_PROXY` (would steal Starwave + FIX onto the Achiever hop).

---

## 1. Why “must” (three layers; do not collapse)

### 1.1 Operational MUST (this lab)

R022 lab pin: Achiever Manager TCP **must** go through HTTP `81.29.145.69:49527`. StarwaveFX Manager TCP **must not**.

The hop is an **Achiever ACL identity presenter**. Sharing it would:

1. Invent `MT5_STARWAVEFX_PROXY_HOST` (A58 reject list — §56 has **no** Starwave proxy host/port/user/pass keys).
2. Hairpin Starwave through a proxy that exists to present Achiever’s allow-list IP.
3. Risk contaminating FIX / API / restore if applied as process `HTTP_PROXY` (R022 §3.2 **FORBIDDEN**).

### 1.2 Code MUST (current product)

`LiveMt5Registration.CreateConnectors` is the only live binder. Starwave’s `ProxyEnabled` is a **literal `false`**. `MT5_STARWAVEFX_PROXY_ENABLED` in `.env` is documentation paint — **unread**.

`NativeMt5BrokerConnector.ApplyProxy()` returns immediately when `!_opt.ProxyEnabled` (or empty host). Starwave therefore **cannot** `ProxySet` without a source change.

### 1.3 Measured working path (census)

`tools/LiveBrokerProbe` builds connectors via `LiveMt5Registration.CreateConnectorsFromEnvironment()` — same forced-direct Starwave slot — and wrote `LIVE_GROUPS_AND_TRADERS.json` at **2026-08-18T08:42:16.8519545+00:00** with `STARWAVEFX.connected=true`, `elapsedMs=6413.478`, **10 / 1948 / 478**.

This slot did **not** re-run `Connect`. It **does** treat that artifact as the standing live proof. Older C42 / R022 “live attach NOT PROVEN / two fakes” lines are **stale** relative to that JSON.

### 1.4 What is **not** proven

A Starwave `Connect` **through** `81.29.145.69:49527` was **not** executed. There is **no** measured Starwave `1012` on the proxy path. “Must connect direct” is **not** “proxy would return 1012.” It is: documented off, coded off, measured working off, and **must not** reuse Achiever’s hop.

Architecture §8 still says “design the connector so proxy/whitelist routing can be enabled later.” That is a **future field**, not a license to turn the flag on or to guess Achiever’s host.

---

## 2. Product C# (measured)

### 2.1 Binder forces Starwave direct

```38:47:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
        var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.StarwaveFx,
            Server = config["MT5_STARWAVEFX_SERVER"] ?? "",
            Port = int.TryParse(config["MT5_STARWAVEFX_PORT"], out var sp) ? sp : 443,
            Login = ulong.TryParse(config["MT5_STARWAVEFX_LOGIN"], out var sl) ? sl : 0,
            Password = config["MT5_STARWAVEFX_PASSWORD"] ?? "",
            ProxyEnabled = false,
            NativeDllDirectory = dllDir
        });
```

Contrast Achiever in the same method (L23–36): `ProxyEnabled` is `bool.TryParse(config["ACHIEVER_PROXY_ENABLED"], …) && pe` plus `ACHIEVER_PROXY_HOST` / `PORT` / `USERNAME` / `PASSWORD`.

`src/` grep `ProxyEnabled`: **8** hits. The only assignment of `true` is Achiever (`LiveMt5Registration` bind + `BrokerCatalogSeed` Achiever row). Starwave never gets a host/port.

### 2.2 `ApplyProxy` is the only `ProxySet` path

```115:130:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

Called once from `ConnectCore` **before** `Connect`. Starwave: `_opt.ProxyEnabled == false` → no `ProxySet` → `Connect($"{Server}:{Port}", login, password, pump)`.

Describe map (same file): `1012 => "manager IP blocked — Achiever requires the whitelist HTTP proxy"`. That hint is **Achiever-scoped**. A Starwave 1012 would be unexpected (§8 no whitelist). R022 rule 6: do **not** “fix” it with Achiever’s hop.

### 2.3 Catalog / seed: Starwave `ProxyEnabled` left default false

`BrokerCatalogSeed.cs` Achiever row: `ProxyEnabled = true`, `ProxyHost = "81.29.145.69"`, `ProxyPort = 49527`. Starwave row (`L36–52`): server `84.201.6.142`, port `443`, login `9904`, pool `4` — **no** `Proxy*` fields. `Broker.ProxyEnabled` defaults **false**.

`DemoSeeder.cs` Starwave row matches (no `Proxy*`). Catalog paint is **not** what `ApplyProxy` reads; live hop is env via `LiveMt5Registration`.

### 2.4 Env key unread

| Key | In `D:\Prop\.env` | Read by C# Starwave connector? |
|---|---|---|
| `MT5_STARWAVEFX_SERVER` / `PORT` / `LOGIN` / `PASSWORD` | Yes | **Yes** |
| `MT5_STARWAVEFX_PROXY_ENABLED` | Yes, value **`false`** | **No** (A008) |
| `MT5_STARWAVEFX_PROXY_HOST` / `_PORT` / `_USERNAME` / `_PASSWORD` | **NO_KEY** (R022) | n/a — do not invent |
| `IS_MT5_PROXY_ENABLED` / `MT5_PROXY_*` | C++ SDK vocabulary | **No** (not bound in `LiveMt5Registration`) |
| Process `HTTP_PROXY` / `HTTPS_PROXY` | must stay **UNSET** | not used |

Required Starwave set (A008): `MT5_STARWAVEFX_SERVER`, `MT5_STARWAVEFX_PORT` (default 443), `MT5_STARWAVEFX_LOGIN`, `MT5_STARWAVEFX_PASSWORD`. Password is the secret; host/port/login are non-secret (E036).

---

## 3. Architecture + YoPips C++ (measured)

### 3.1 Architecture §8 (binding)

`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`:

```417:437:D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
MT5_STARWAVEFX_DISPLAY_NAME=StarwaveFX
MT5_STARWAVEFX_PROVISIONING_ENABLED=true
MT5_STARWAVEFX_MODE=local
MT5_STARWAVEFX_SERVER=84.201.6.142
MT5_STARWAVEFX_PORT=443
MT5_STARWAVEFX_LOGIN=9904
MT5_STARWAVEFX_SERVER_NAME=StarwaveFX
MT5_STARWAVEFX_POOL_SIZE=4
MT5_STARWAVEFX_PROXY_ENABLED=false
```

> No IP whitelist is currently required.
>
> Still design the connector so proxy/whitelist routing can be enabled later.

§56 secret-safe sample repeats `MT5_STARWAVEFX_PROXY_ENABLED=false` next to Achiever `ACHIEVER_PROXY_ENABLED=true` / host `81.29.145.69` / port `49527`. Same digits, two jobs (C55 / R022): allow-list identity vs HTTP listener. Starwave has **no** egress twin.

### 3.2 C++ PropFirm is single-broker — Starwave is a **second process env**

`D:\Projects\YoPips\Backend\C++ Backend PropFirm` grep `STARWAVE` / `84.201` / `MT5_STARWAVEFX`: **0 hits**.

`config/app_config.cpp` binds one triple: `MT5_SERVER` / `MT5_LOGIN` / `MT5_PASSWORD` plus `IS_MT5_PROXY_ENABLED` + `MT5_PROXY_*` (default **false**).

`src/main.cpp` L201–225: if `config.mt5_proxy_enabled` and type/address/port complete → `SetProxy` **before** `Connect`; else log `MT5 proxy mode: DISABLED (global)` and connect **direct**. Pool sessions copy the same toggle (L249–268).

`src/core/mt5_manager.cpp` `SetProxy` (L33–57) + re-apply on `Connect` (L76–95): `MTProxyInfo.enable=1`, address `"IP:port"`, auth `"login:password"` only if login non-empty. Logs **type + address + port only**.

Operator matrix (A004; still binding):

| Step | Achiever (this LAN) | StarwaveFX |
|---|---|---|
| `IS_MT5_PROXY_ENABLED` | `true` | `false` / unset |
| `MT5_PROXY_TYPE` | `HTTP` (`PROXY_HTTP=2`) | unused |
| `SetProxy` | **required** (else historical **1012**) | **must not** |
| `Connect` | `57.128.141.65:443` login `2027` | `84.201.6.142:443` login `9904` |
| First fail if proxy omitted here | **1012** | n/a (direct is the path) |

C++ probe default type is **SOCKS5** if type empty — **wrong** for the Achiever hop; irrelevant for Starwave because the toggle stays off.

`D:\Prop\mt5-sdk\.env.example` L59–71: proxy “fully off unless `IS_MT5_PROXY_ENABLED` is true.” No `MT5_STARWAVEFX_*`.

---

## 4. Live census — ALL groups, ALL manager traders (direct)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `D:\Prop\tools\LiveBrokerProbe\Program.cs` (`CreateConnectorsFromEnvironment` → `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`).  
Summary: `D:\Prop\reports\LIVE_MANAGER_FETCH_MEASURED.md`, `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`.

| Broker | Path | Groups | Traders | Open positions | `elapsedMs` |
|---|---|---:|---:|---:|---:|
| ACHIEVER | HTTP `ProxySet` then `Connect` | 8 | 6512 | 1506 | 7212.5885 |
| STARWAVEFX | **direct** (`ProxyEnabled=false`) | **10** | **1948** | 478 | 6413.478 |
| **Total** | | **18** | **8460** | **1984** | |

Starwave groups (manager-visible; empty groups are still **fetched**):

| Group | Accounts | Ccy |
|---|---:|---|
| `Starwave\cent\FX1\grp1` | 11 | USC |
| `Starwave\cent\FX1\grp2` | 4 | USC |
| `Starwave\demo\FX2\grp1` | 170 | USD |
| `Starwave\demo\FX2\grp2` | 1735 | USD |
| `Starwave\real\FX3\grp1` | 22 | USD |
| `Starwave\real\FX3\grp2` | 0 | USD |
| `Starwave\real\FX3\grp3` | 0 | USD |
| `Starwave\real\FX3\grp4` | 4 | USD |
| `Starwave\real\FX3\grp5` | 0 | USD |
| `Starwave\real\FX3\LP` | 2 | USD |

How “ALL” is implemented (not first-3, not plan-filter):

- Groups: `GroupRequestArray("*")`, fallback `GroupTotal`/`GroupNext` (`NativeMt5BrokerConnector.GetGroupsCore`).
- Accounts: `GetAccountsAsync(null)` walks **every** group → `UserRequestArray` / `UserGetByGroup` / `UserLogins` (`GetAccountsCore` L189–214).
- Ingest: `DealIngestionService.SyncCatalogAsync` calls those two methods and batch-upserts (`L37–50`). `LiveIngestHostedService` runs that for **every** `registry.All()` connector (Achiever + Starwave).

Honesty: these are **all groups / logins this manager login can see**. Server-side groups outside the manager ACL are invisible. Logins are **not** globally unique — persist `(broker_id, login)`.

TCP (R012, SYN only, no auth): `84.201.6.142:443` **OPEN** 189 ms from `DESKTOP-FQPFPKE`. Desktop public egress `106.219.132.213` ≠ Achiever allow-list `81.29.145.69` — that is why **Achiever** needs the hop and **Starwave does not**.

---

## 5. Copy-to-cTrader — no live orders (no loss)

Goal constraint: fetch may be live; **copy must not send**.

| Gate | Measured state |
|---|---|
| `LiveRuntimeStatus.RealCopyEnabled` | **forced `false`** in `DependencyInjection.cs` L38–42 (“Live NewOrderSingle is not implemented”) and again after FIX logon (`CTraderFixLogonHostedService` L68) |
| `GET /api/settings` | `REAL_COPY_EXECUTION_ENABLED` = `runtime.RealCopyEnabled`; `FEATURE_COPY_TRADING_ENABLED` = **false** (`apps/api/Program.cs` L73–76) |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** |
| `CTraderFixSession` | **`TryLogonAsync` / `BuildLogon` only** (`35=A`). No `35=D` builder. |
| Product `Fix.CTrader` `35=` types | `A` (logon), `V` (MD request), harness `0`/`3`/`8`/`X`/`y`. **No `D`.** |
| E002 pin | **SAFE_BY_ABSENCE** — no function emits FIX MsgType=D to a socket |
| Snapshot copy note | `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."` |

FIX QUOTE/TRADE logon (when password present) is **recon/quotes only**. Tag 553 = integer account id. That is **not** a send path.

`risk_to_capital` this slot: **none** from Starwave connect-direct or from dashboard copy. Capital risk would require a new `35=D` sender **and** an armed flag. Neither exists.

---

## 6. Operator rules (do not walk back)

1. Starwave: `Connect` `84.201.6.142:443` as manager `9904`. Leave proxy **off**. Do not call `SetProxy`.
2. Achiever (this LAN): `ACHIEVER_PROXY_ENABLED=true`, HTTP `81.29.145.69:49527`, then `Connect` `57.128.141.65:443`. Toggle the C# keys (`ACHIEVER_PROXY_*`), not C++ `IS_MT5_PROXY_ENABLED`, when running the product host.
3. Never set process/user/machine `HTTP_PROXY` to the Achiever hop.
4. Never invent `MT5_STARWAVEFX_PROXY_HOST` = Achiever host. If a future flag is true with empty host → **fail closed** (A58).
5. A 1012 on Achiever is routing/ACL. A 1012 on Starwave is unexpected — do not “fix” with Achiever’s proxy.
6. C++ YoPips: Starwave is a **second process** with `IS_MT5_PROXY_ENABLED=false` and `MT5_SERVER` swapped. One `AppConfig` cannot hold both brokers.
7. Keep dummy seed **off**. Live catalog is the Manager census above.
8. Keep `NewOrderSingle` **off** until TRADE logon + recon + risk + explicit go-live. This slot does not arm anything.

---

## 7. Siblings (do not treat as this file)

| File | Relation |
|---|---|
| `R012_proxy.md` | Achiever needs HTTP hop on this desktop; Starwave TCP OPEN; Starwave does **not** need the hop |
| `R022_proxy_scope.md` | Scope pin: hop is Achiever-only. Starwave **must not**. (C42/fake lines in §0/§4.2 are stale vs live JSON) |
| `A004_yopips_group_probe.md` | Operator matrix: Starwave `SetProxy` **must not** |
| `A008_env_wiring.md` | `MT5_STARWAVEFX_PROXY_ENABLED` unread; Starwave forced direct |
| `A58_broker_registry.md` | No Starwave proxy host keys; fail closed if flag true + empty host |
| `E036_swx.md` | `84.201.6.142` non-secret |
| `E002_no_live_send.md` / `E034_no_35d.md` | No `35=D` sender |
| `LIVE_MANAGER_FETCH_MEASURED.md` | Standing 8+10 / 6512+1948 census |
| `W500_SLICE_24.md` | Different angle (architecture vs Achiever 1012). Complements; does not replace this pin. |

---

## 8. Slot close

| Item | Value |
|---|---|
| Slot | 24 |
| Verdict | **CONFIRMED** — Starwave must connect **direct**, no proxy |
| Evidence | Code hardcodes `ProxyEnabled=false`; §8 `PROXY_ENABLED=false`; live JSON `STARWAVEFX` 10/1948/478 on that path; C++ has no Starwave proxy slot |
| Risk to capital | **None** — no NewOrderSingle; `RealCopyEnabled=false` |
| Product edited | **No** |
