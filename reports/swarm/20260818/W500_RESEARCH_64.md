# W500_RESEARCH_64 — Confirm StarwaveFX must connect **direct** (no proxy)

| Field | Value |
|---|---|
| Slot | **64** |
| Agent | W500_RESEARCH_64 (senior engineer; read-only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_64.md` |
| Assigned | Confirm Starwave **must** connect **direct with no proxy**. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** Report only. |
| Test / `.env` / config edited | **No.** Keys classified PRESENT + already-published non-secret identifiers only. |
| Secret values printed | **None.** No Manager password, no proxy user/password, no FIX password, no connection string. |
| Live Manager logon this pass | **Not re-run.** Census already measured `2026-08-18T08:42:16.8519545+00:00` by `LiveBrokerProbe`. |
| Trees read | `D:\Prop\src`, `D:\Prop\mt5-sdk`, `D:\Projects\YoPips\Backend\C++ Backend PropFirm` |
| Binding law | Architecture §§7, 8, 41, 56; `LiveMt5Registration` Starwave `ProxyEnabled = false`; R012 / R022 / A004 / A008 / A58; LIVE census JSON |

This is a **confirmation pin** for slot 64. It does not call `Connect`, does not send `35=D`, and does not invent groups beyond the manager ACL.

---

## 0. Verdict (binding)

**CONFIRMED: StarwaveFX Manager must connect direct.**  
Do **not** call `ProxySet` / `SetProxy`. Do **not** reuse Achiever’s HTTP hop `81.29.145.69:49527`. Do **not** export process `HTTP_PROXY`.

| Claim | Measured result | Class |
|---|---|---|
| Architecture §8 sets Starwave proxy **off** | **Yes.** `MT5_STARWAVEFX_PROXY_ENABLED=false`. Quote: “No IP whitelist is currently required.” | `LAW` |
| Architecture §56 sample agrees | **Yes.** Same flag `false` next to Achiever `ACHIEVER_PROXY_ENABLED=true` / host `81.29.145.69` / port `49527`. | `LAW` |
| Product C# can turn Starwave proxy on via env | **No.** Only live factory hardcodes `ProxyEnabled = false`. `MT5_STARWAVEFX_PROXY_ENABLED` is **unread**. | `HARD_PIN` |
| Native `ApplyProxy` runs for Starwave | **No.** Guard is `_opt.ProxyEnabled && host non-empty`. Starwave sets neither. | `HARD_PIN` |
| Catalog seed paints a Starwave proxy row | **No.** Achiever seed `ProxyEnabled=true` + `81.29.145.69:49527`. Starwave seed omits proxy fields (`bool` default **false**). | `SEED` |
| Operator `.env` agrees | **Yes (R012).** `MT5_STARWAVEFX_PROXY_ENABLED` PRESENT = `false`. No `MT5_STARWAVEFX_PROXY_HOST` / `_PORT` / `_USERNAME` / `_PASSWORD` keys. | `CONFIG` |
| This LAN can TCP to Starwave without the hop | **Yes (R012).** `84.201.6.142:443` **OPEN** 189 ms. Public egress `106.219.132.213` is **not** a Starwave allow-list problem. | `MEASURED_TCP` |
| Live Starwave census used the **direct** path | **Yes.** Probe constructs connectors via `LiveMt5Registration` (Starwave `ProxyEnabled=false`) → `connected=true`, **10 groups / 1948 traders / 478 positions**, `elapsedMs=6413.478`. | `MEASURED_LIVE` (prior) |
| Starwave needs Achiever’s HTTP proxy | **No.** That hop exists to present Achiever allow-list identity `81.29.145.69`. Starwave has **no** documented whitelist. | `MUST_NOT` |
| Starwave-via-Achiever-proxy was measured to 1012 | **Not tested.** Do **not** claim a Starwave 1012. Binding rule is “direct is required + proven,” not “proxy was proven to fail.” | `HONEST_GAP` |
| YoPips C++ has a Starwave proxy field | **No.** Single-broker `AppConfig`: `MT5_SERVER` / `MT5_LOGIN` / `IS_MT5_PROXY_ENABLED`. Zero `Starwave` / `84.201` hits under YoPips `src/`. | `C++_SINGLE_BROKER` |
| Fetch ALL Achiever + Starwave groups + traders | **Implemented + already measured** as Manager **read**: Achiever **8 / 6512** (HTTP proxy) + Starwave **10 / 1948** (direct) = **18 / 8460**. | `READ_ONLY` |
| Copy to cTrader can place a live order today | **No.** Product `*.cs` under `D:\Prop\src` has **0** `35=D` / `MsgType="D"` / `(35, "D")`. `RealCopyEnabled` forced **false**. Session builder is **35=A Logon only**. | `SAFE_BY_ABSENCE` |
| Risk to capital from this slot | **None.** Catalog APIs are Manager reads. Destination send path does not exist. | `NO_LOSS` |

Honest one-liner:

```text
Starwave = Connect(84.201.6.142:443, 9904, password) with ProxySet SKIPPED.
Achiever HTTP 81.29.145.69:49527 is the OTHER broker's allow-list hop — do not point Starwave at it.
C# hardcodes ProxyEnabled=false (env MT5_STARWAVEFX_PROXY_ENABLED is ignored).
Live census already pulled ALL manager-visible Starwave groups/traders DIRECT (10 / 1948).
cTrader 35=D does not exist; RealCopyEnabled=false — copy cannot lose capital yet.
```

Do **not** enable `ACHIEVER_PROXY_*` on the Starwave connector. Do **not** invent `MT5_STARWAVEFX_PROXY_HOST`. Do **not** flip `REAL_COPY_EXECUTION_ENABLED`. Do **not** treat `demo\Maxmaster` as the universe (it is **absent** from both live dumps).

---

## 1. Goal split (do not collapse)

| Job | Live I/O allowed? | Path | Capital at risk? |
|---|---|---|---|
| **A.** Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders | **Yes** — native Manager **read** | Achiever: HTTP `ProxySet` then `Connect`. **Starwave: `Connect` only.** | **No** |
| **B.** Copy those traders to cTrader | **Not yet** — SHADOW / CopyIntent only | Irrelevant — no `35=D` builder | **Would be yes** the moment NewOrderSingle exists |

Slot 64 confirms the Starwave half of job A. Job B stays **off**.

---

## 2. Architecture — Starwave is the no-proxy broker

`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §8 (non-secret identifiers only):

```env
MT5_STARWAVEFX_SERVER=84.201.6.142
MT5_STARWAVEFX_PORT=443
MT5_STARWAVEFX_LOGIN=9904
MT5_STARWAVEFX_SERVER_NAME=StarwaveFX
MT5_STARWAVEFX_POOL_SIZE=4
MT5_STARWAVEFX_PROXY_ENABLED=false
```

Binding prose immediately after that stanza:

> No IP whitelist is currently required.
>
> Still design the connector so proxy/whitelist routing can be enabled later.

Contrast §7 (Achiever): required whitelisted outbound IP `81.29.145.69`. That sentence **does not** apply to Starwave.

§56 secret-safe sample repeats the split in one file:

| Broker | Proxy keys in §56 |
|---|---|
| Achiever | `ACHIEVER_PROXY_ENABLED=true`, `ACHIEVER_PROXY_HOST=81.29.145.69`, `ACHIEVER_PROXY_PORT=49527` |
| StarwaveFX | **only** `MT5_STARWAVEFX_PROXY_ENABLED=false` |

A58 / R022: there is **no** §56 `MT5_STARWAVEFX_PROXY_HOST` / `_PORT` / `_USERNAME` / `_PASSWORD`. Inventing those names, or copying Achiever’s hop onto Starwave, is a **reject**.

“Design so proxy can be enabled later” is a **future field**, not a license to hairpin Starwave through `:49527` today.

---

## 3. Product C# — Starwave proxy is impossible without a code change

### 3.1 Only live factory (`LiveMt5Registration.cs`)

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

Achiever (same method) **does** bind `ACHIEVER_PROXY_ENABLED` / `HOST` / `PORT` / `USERNAME` / `PASSWORD`. Starwave does **not** bind any proxy key. The literal `false` is the pin.

Repo-wide under `D:\Prop\src`: **exactly one** `ProxyEnabled = false` assignment — this Starwave constructor.

A008 already catalogued (re-verified this pass): required Starwave set is `MT5_STARWAVEFX_SERVER` / `_PORT` / `_LOGIN` / `_PASSWORD`. Unread: `MT5_STARWAVEFX_DISPLAY_NAME`, `_PROVISIONING_ENABLED`, `_MODE`, `_SERVER_NAME`, `_POOL_SIZE`, **`_PROXY_ENABLED`**.

Setting `MT5_STARWAVEFX_PROXY_ENABLED=true` in `.env` is **inert** on the C# live path.

### 3.2 `ApplyProxy` is skipped when the flag is false

```115:118:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private void ApplyProxy()
    {
        if (_manager is null || !_opt.ProxyEnabled || string.IsNullOrWhiteSpace(_opt.ProxyHost))
            return;
```

`ConnectCore` always calls `ApplyProxy()` then `Connect($"{Server}:{Port}", login, password, pump)`. For Starwave that is:

1. `ApplyProxy` → **return** (no `ProxySet`).
2. `Connect("84.201.6.142:443", 9904, password, PUMP_MODE_GROUPS|USERS|POSITIONS, 30000)`.
3. On fail, retry `PUMP_MODE_NONE`. Fail-closed if that also is not `MT_RET_OK`.

There is no fallback that silently copies Achiever’s proxy onto Starwave.

### 3.3 Catalog seed

`BrokerCatalogSeed`:

- Achiever: `ProxyEnabled = true`, `ProxyHost = "81.29.145.69"`, `ProxyPort = 49527`.
- Starwave: `Server = "84.201.6.142"`, `Port = 443`, `ManagerLogin = 9904`, **no** `ProxyEnabled` / `ProxyHost` / `ProxyPort` writes.

`Broker.ProxyEnabled` is a CLR `bool` default **false**. Dashboard paint matches the live connector.

---

## 4. Why “must” (not merely “can”)

Three independent reasons, in order of strength:

1. **Wired path.** The only `IMt5BrokerConnector` factory that starts the API (`AddTraderIntelligence` → `CreateConnectors`) **cannot** apply a Starwave proxy. Direct is the only implemented Starwave connect.
2. **Architecture + operator config.** §8 / §56 / `D:\Prop\.env` all pin `MT5_STARWAVEFX_PROXY_ENABLED=false`. R022 lab pin: Starwave Manager TCP **must not** use `81.29.145.69:49527`.
3. **Wrong hop if reused.** Achiever’s HTTP listener exists so Achiever sees source IP `81.29.145.69`. Starwave has **no** such allow-list. Hairpinning Starwave through that hop would (a) invent a Starwave proxy host (A58 reject), (b) present Achiever’s identity to a different broker, (c) steal FIX / NuGet if done as process `HTTP_PROXY` (R022 **FORBIDDEN**).

What this slot does **not** claim: a measured Starwave `Connect` through `:49527` returning 1012. That A/B was **not** run. Achiever 1012 without proxy (R012 / YoPips logs) does **not** transfer.

---

## 5. YoPips C++ — single-broker; Starwave is a second process with proxy **off**

`D:\Projects\YoPips\Backend\C++ Backend PropFirm`:

| Surface | Finding |
|---|---|
| `src/` string `Starwave` / `84.201` | **0 hits** |
| `AppConfig` | One triple: `mt5_server` / `mt5_login` / `mt5_password` + `mt5_proxy_*` |
| Master toggle | `IS_MT5_PROXY_ENABLED` (default **false**). `MT5_PROXY_ENABLED` is **not** the binder. |
| `main.cpp` | `SetProxy` only if `config.mt5_proxy_enabled`; else logs `MT5 proxy mode: DISABLED (global)` and `Connect`s direct |
| Production `.env` (R012) | Achiever host `57.128.141.65`, login `2027`, `MT5_PROXY_TYPE=HTTP`, address `81.29.145.69`, port `49527`. That stanza is the **Achiever** hop. |

A Starwave run of YoPips / `mt5_group_probe` **must**:

1. Swap `MT5_SERVER=84.201.6.142`, `MT5_PORT=443`, `MT5_LOGIN=9904`, `MT5_PASSWORD=<Starwave secret>`.
2. Leave `IS_MT5_PROXY_ENABLED` **unset / false**.
3. **Not** call `SetProxy`.

`MT5Manager::SetProxy` packs `address=IP:port` and `auth=login:password` then `ProxySet` before `Connect` — same shape as C# `ApplyProxy`. For Starwave that function **must not** be entered.

---

## 6. Measured topology + live census (not re-run this pass)

### 6.1 TCP (R012, SYN only, no credentials)

| Target | Result |
|---|---|
| `84.201.6.142:443` (StarwaveFX Manager) | **OPEN** 189 ms |
| `57.128.141.65:443` (Achiever Manager) | **OPEN** 309 ms |
| `81.29.145.69:49527` (Achiever HTTP hop) | **OPEN** 199 ms |
| This desktop public egress (no proxy) | `106.219.132.213` |

Starwave Manager is reachable **without** the hop. Achiever is reachable on TCP but historically **1012** without presenting `81.29.145.69`. Those are different brokers.

### 6.2 Live Manager census (`LiveBrokerProbe`)

Probe: `D:\Prop\tools\LiveBrokerProbe\Program.cs`  
Constructs `LiveMt5Registration.CreateConnectorsFromEnvironment()` — **same** Starwave `ProxyEnabled=false` object — then `ConnectAsync` → `GetGroupsAsync` → `GetAccountsAsync(null)` → `GetGroupPositionsAsync("*")`.

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
UTC: `2026-08-18T08:42:16.8519545+00:00`

| Broker | `connected` | Path | Groups | Accounts | Open positions | `elapsedMs` |
|---|---|---|---:|---:|---:|---:|
| ACHIEVER | true | HTTP proxy | 8 | 6512 | 1506 | 7212.5885 |
| STARWAVEFX | true | **direct** | 10 | 1948 | 478 | 6413.478 |
| **Total** | | | **18** | **8460** | **1984** | |

Starwave groups (manager-visible set; empty groups **included**):

| Group | Accounts | Currency |
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
| **Sum** | **1948** | |

Achiever groups (for the ALL-groups goal; not this slot’s connect path): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` 0, `demo\yo-payp` 23. **`demo\Maxmaster` is absent.**

These are **all groups this manager login can see**. Server-side groups outside the manager ACL are not claimed.

### 6.3 How “ALL traders” is implemented

`GetAccountsAsync(null)` walks **every** group from `GetGroupsCore()`, then per group:

1. `UserRequestArray(gname)` (fallback `UserGetByGroup`)
2. if empty: `UserLogins` + `UserRequestByLogins`
3. `UserAccountRequestArray` (fallback `UserAccountGetByGroup`)

Ingest `SyncCatalogAsync` calls that with `group=null` and batch-upserts. `LiveBrokerProbe` uses the same call. There is no `demo\Maxmaster`-only filter on the live path.

---

## 7. Copy to cTrader — live send is off (no loss)

| Check | Result | File |
|---|---|---|
| `CTraderFixSession` outbound MsgType | **Only** `(35, "A")` Logon | `src\Fix.CTrader\Sessions\CTraderFixSession.cs` |
| `35=D` / `MsgType="D"` / `(35, "D")` in `D:\Prop\src` `*.cs` | **0 hits** | grep this pass |
| `NewOrderSingle` in `Fix.CTrader` | comment / log string only | logon host + options |
| `RealCopyEnabled` | forced **false** in DI **and** after FIX logon | `DependencyInjection.cs` L40–41; `CTraderFixLogonHostedService.cs` L68 |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **false** | options L35 |
| FIX sockets | QUOTE TLS **5211**, TRADE TLS **5212**, TargetCompID `cServer`; logon only | logon host L48–58 |
| Dashboard copy note when flag false | “NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.” | `LiveRuntimeStatus.cs` L42–43 |

`SAFE_BY_ABSENCE`: there is no NewOrderSingle builder to refuse. Logon `35=A` is **not** a live order. Manager group/user walks do **not** open a cTrader position.

Do **not** enable `REAL_COPY_EXECUTION_ENABLED`. The flag cannot be honored safely (DI comment: “Live NewOrderSingle is not implemented”).

---

## 8. Operator recipe (Starwave, this LAN)

```text
1. MT5_STARWAVEFX_SERVER=84.201.6.142
2. MT5_STARWAVEFX_PORT=443
3. MT5_STARWAVEFX_LOGIN=9904
4. MT5_STARWAVEFX_PASSWORD=<secret, never log>
5. MT5_STARWAVEFX_PROXY_ENABLED=false   (documentation; C# ignores it)
6. Do NOT set MT5_STARWAVEFX_PROXY_HOST
7. Do NOT copy ACHIEVER_PROXY_* onto the Starwave NativeMt5Options
8. Do NOT set process HTTP_PROXY / HTTPS_PROXY / ALL_PROXY
9. Initialize Manager DLL → Connect(L"84.201.6.142:443", 9904, password, pump)
10. Enumerate GroupRequestArray("*") then UserRequestArray per group
```

Expected first fail if creds wrong: **3** (`MT_RET_AUTH_MANAGER_FAILED`).  
Expected first fail if host:port dead: **7** / **5**.  
Do **not** “fix” a Starwave 3/7 by flipping Achiever’s proxy on.

---

## 9. Honesty / not claimed

- This pass did **not** re-attach Manager. Census cited is `LiveBrokerProbe` at `2026-08-18T08:42:16.8519545+00:00`.
- Starwave-via-Achiever-proxy was **not** A/B tested. No Starwave 1012 is claimed.
- C42 (“live Achiever/Starwave NOT proven”) is **stale** relative to `LIVE_MANAGER_FETCH_MEASURED.md` / the probe JSON. Slot 64 follows the later measured file.
- 18 / 8460 is the **manager-visible** set, not “every account the broker ever created.”
- In-memory DB: restart re-fetches. `DATABASE_URL` remains a placeholder (status report).
- This is **not** live copy-trading and not 95% destination parity.

---

## 10. Sibling pins (do not contradict)

| File | Role |
|---|---|
| `R012_proxy.md` | This LAN needs Achiever HTTP hop; Starwave TCP OPEN 189 ms; `.env` Starwave proxy flag `false` |
| `R022_proxy_scope.md` | Hop scope = Achiever Manager only; Starwave **must not** |
| `A004_yopips_group_probe.md` | Recipe: Starwave `SetProxy` **must not**; two-process C++ |
| `A008_env_wiring.md` | Starwave `ProxyEnabled` hardcoded; env flag unread |
| `A58_broker_registry.md` | Do not invent `MT5_STARWAVEFX_PROXY_HOST` |
| `LIVE_MANAGER_FETCH_MEASURED.md` | 8/6512 proxy + 10/1948 **direct** |
| `W500_RESEARCH_3.md` | Achiever 1012 + hop; Starwave not the same hop |
| `W500_RESEARCH_4.md` / `_24.md` / `_44.md` | Earlier same-claim pins; slot 64 re-reads and re-confirms |

---

## 11. Checklist

- [x] Architecture §8 / §56: Starwave proxy **false**, no whitelist
- [x] C# live factory: `ProxyEnabled = false`; env flag unread
- [x] `ApplyProxy` skipped without flag + host
- [x] Seed: Starwave proxy fields omitted
- [x] YoPips: no Starwave broker slot; proxy is Achiever-only
- [x] TCP OPEN to `84.201.6.142:443` (R012)
- [x] Live census 10 / 1948 on the direct constructor
- [x] ALL groups/traders walk is `GetGroups` + `GetAccountsAsync(null)`
- [x] 0 `35=D` in product C#; `RealCopyEnabled=false`
- [x] Secrets not printed; product source not edited
