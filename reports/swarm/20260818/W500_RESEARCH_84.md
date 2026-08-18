# W500_RESEARCH_84 — StarwaveFX must connect **direct** (no proxy)

| Field | Value |
|---|---|
| Slot | **84** |
| Agent | W500_RESEARCH_84 (senior engineer; read-only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_84.md` |
| Assigned | Confirm Starwave must connect **direct with no proxy**. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** Report + swarm index/log only. |
| Test / `.env` edited | **No.** Keys classified PRESENT + already-published non-secret identifiers only. |
| Secret values printed | **None.** No Manager password, no proxy user/password, no FIX password. |
| Live Manager logon this pass | **Not re-run.** Prior `LiveBrokerProbe` utc `2026-08-18T08:42:16.8519545+00:00`: `STARWAVEFX` `connected=true`, `elapsedMs=6413.478`, **direct**. |
| C++ tree consulted | `D:\Projects\YoPips\Backend\C++ Backend PropFirm` (`AppConfig` is **single-broker**; no `MT5_STARWAVEFX_*` fields). |
| Sibling pin | `W500_RESEARCH_44.md` (same question, earlier slot). This file re-reads product source independently. |

This is a **confirmation pin**. It does not call `Connect`, does not send `35=D`, and does not invent extra groups beyond the manager ACL.

---

## 0. Verdict (binding)

**CONFIRMED: StarwaveFX Manager must connect direct. Do not call `SetProxy` / `ProxySet`. Do not reuse Achiever’s HTTP hop.**

| Claim | Measured result | Class |
|---|---|---|
| Architecture says Starwave proxy is **off** | **Yes.** §8: `MT5_STARWAVEFX_PROXY_ENABLED=false`. “No IP whitelist is currently required.” | `EXISTS_AND_GOOD` |
| Product C# can turn Starwave proxy on via env | **No.** `LiveMt5Registration` **hardcodes** `ProxyEnabled = false` (line 45). `MT5_STARWAVEFX_PROXY` / `MT5_STARWAVEFX_PROXY_ENABLED` **0 hits** under `D:\Prop\src` and `D:\Prop\apps`. | `HARD_PIN` |
| Native `ApplyProxy` fires for Starwave | **No.** Guard is `!_opt.ProxyEnabled \|\| host empty`. Starwave never sets either. | `HARD_PIN` |
| Catalog seed paints a Starwave proxy row | **No.** `BrokerCatalogSeed` Achiever `ProxyEnabled=true` / `81.29.145.69:49527`. Starwave seed **omits** proxy fields (`bool` default `false`). | `EXISTS_AND_GOOD` |
| Operator `.env` agrees | **Yes.** `MT5_STARWAVEFX_PROXY_ENABLED=false`. `IS_MT5_PROXY_ENABLED` **KEY_ABSENT**. | `CONFIG` |
| This LAN can TCP to Starwave Manager without the hop | **Yes (R012).** `84.201.6.142:443` **OPEN** 189 ms. Public egress `106.219.132.213` is **not** an Achiever-style allow-list problem for Starwave. | `MEASURED_TCP` (not re-probed here) |
| Live Starwave census used the direct path | **Yes (prior).** `LiveBrokerProbe` → `CreateConnectorsFromEnvironment` → Starwave `ProxyEnabled=false` → `connected=true` **10 groups / 1948 traders / 478 open positions**. | `MEASURED_LIVE` (not re-run here) |
| Starwave needs Achiever’s HTTP proxy `81.29.145.69:49527` | **No.** That hop exists to present Achiever allow-list identity `81.29.145.69`. Architecture §8 has **no** Starwave whitelist. | `MUST_NOT` |
| Starwave-via-Achiever-proxy was measured to 1012 | **Not tested.** Do **not** claim a Starwave 1012. Binding rule is “direct is required / proven,” not “proxy was proven to fail.” | `HONEST_GAP` |
| YoPips C++ `AppConfig` has a Starwave proxy field | **No.** Single triple: `MT5_SERVER` / `MT5_LOGIN` / `IS_MT5_PROXY_ENABLED`. A Starwave probe run must **leave the Achiever proxy stanza off**. Grep of YoPips `config/` for `STARWAVEFX`: **0**. | `C++_SINGLE_BROKER` |
| Fetch ALL Achiever + Starwave groups + traders | **Allowed + already measured** as Manager **read**: Achiever **8 / 6512** (HTTP proxy) + Starwave **10 / 1948** (direct) = **18 / 8460**. Group account sums re-added this slot. | `READ_ONLY` |
| Copy to cTrader can place a live order today | **No.** `CTraderFixSession` builds **35=A Logon only**. Product C# `(35, "D")` = **0**. `RealCopyEnabled` forced **false**. | `SAFE_BY_ABSENCE` |
| Risk to capital from Starwave **direct** catalog fetch | **None.** Manager `GroupRequestArray` / `UserRequestArray` / `UserLogins` do not open a destination position. | `NO_LOSS` |

Honest one-liner:

```text
Starwave = Connect(84.201.6.142:443, 9904, password) with ProxySet SKIPPED.
Achiever HTTP 81.29.145.69:49527 is the OTHER broker's allow-list hop — do not point Starwave at it.
C# hardcodes ProxyEnabled=false (env MT5_STARWAVEFX_PROXY_ENABLED is ignored; 0 product reads).
Live census already pulled ALL manager-visible Starwave groups/traders DIRECT (10 / 1948).
cTrader 35=D does not exist; REAL_COPY=false — copy cannot lose capital yet.
```

Do **not** enable `ACHIEVER_PROXY_*` on the Starwave connector. Do **not** flip `REAL_COPY_EXECUTION_ENABLED`. Do **not** treat `demo\Maxmaster` as the universe (architecture §7: “not the only group”).

---

## 1. Goal split (do not collapse)

| Job | Live I/O allowed? | Starwave path | Capital at risk? |
|---|---|---|---|
| **A.** Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders | **Yes** — native Manager **read** | Achiever: HTTP `ProxySet` then Connect. **Starwave: Connect only.** | **No** |
| **B.** Copy those traders to cTrader | **Not yet** — SHADOW / CopyIntent only | Irrelevant — no `35=D` | **Would be yes** the moment a NewOrderSingle exists |

Job A on Starwave is **direct TCP to `84.201.6.142:443`**. Job A on Achiever is a **different** hop. Job A does **not** license Job B.

```text
Starwave Manager census (direct read)  = ALLOWED now
Achiever Manager census (proxy read)   = ALLOWED now (other slot: W500_3 / R012)
FIX 35=A Logon (QUOTE 5211 / TRADE 5212) = allowed for session proof
FIX 35=D NewOrderSingle                  = FORBIDDEN until gates + flag
```

---

## 2. What “direct / no proxy” means (do not mix)

```
Starwave MT5_MODE=local
  → SMTManagerAPIFactory.Initialize + CreateManager
  → ApplyProxy() RETURNS IMMEDIATELY  (ProxyEnabled=false; ProxyHost unset)
  → Connect("84.201.6.142:443", 9904, password, PUMP_GROUPS|USERS|POSITIONS, 30s)
       fallback Connect(..., PUMP_MODE_NONE, 30s)
  → broker sees TCP source = this NIC / NAT (measured public 106.219.132.213)
```

| Term | This slot’s meaning | Not this |
|---|---|---|
| Direct | No `IMTManagerAPI::ProxySet` before `Connect`. Native TCP to `84.201.6.142:443`. | “The C++ probe binary is on the same box.” |
| No proxy | `NativeMt5Options.ProxyEnabled = false` **and** `ApplyProxy` early-return. | Process `HTTP_PROXY` / `HTTPS_PROXY` (unset; irrelevant). YoPips `TRUSTED_PROXIES` (web). |
| Achiever HTTP hop | Manager `ProxySet` type `PROXY_HTTP=2` to `81.29.145.69:49527` so Achiever sees allow-list IP `81.29.145.69`. | A Starwave setting. A system proxy. |
| Must | Operator + product **pin**: Starwave Connect **skips** `ProxySet`. Live success is on that path. | A measured Starwave-via-`:49527` failure (not run). |

Achiever **without** that hop from this LAN is the proven **1012** (`MT_RET_AUTH_MANAGER_IPBLOCK`). That failure mode is **Achiever-only**. `NativeMt5BrokerConnector.Describe` maps `1012` to *“manager IP blocked — Achiever requires the whitelist HTTP proxy”* — the hint names Achiever, not Starwave. Starwave has **no** documented manager IP allow-list.

---

## 3. Product C# — Starwave proxy is a hard pin, not a flag

### 3.1 Registration (binding)

`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`

Achiever **reads** the §56 proxy keys:

```23:36:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.Achiever,
            Server = config["MT5_SERVER"] ?? "",
            Port = int.TryParse(config["MT5_PORT"], out var ap) ? ap : 443,
            Login = ulong.TryParse(config["MT5_LOGIN"], out var al) ? al : 0,
            Password = config["MT5_PASSWORD"] ?? "",
            ProxyEnabled = bool.TryParse(config["ACHIEVER_PROXY_ENABLED"], out var pe) && pe,
            ProxyHost = config["ACHIEVER_PROXY_HOST"],
            ProxyPort = int.TryParse(config["ACHIEVER_PROXY_PORT"], out var pp) ? pp : 0,
            ProxyUser = config["ACHIEVER_PROXY_USERNAME"],
            ProxyPassword = config["ACHIEVER_PROXY_PASSWORD"],
            NativeDllDirectory = dllDir
        });
```

Starwave **does not**. Proxy is a literal `false`. Host/port/user/password are never assigned:

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

Independent grep this slot:

| Search | Path | Hits |
|---|---|---:|
| `MT5_STARWAVEFX_PROXY` | `D:\Prop\src` | **0** |
| `MT5_STARWAVEFX_PROXY` | `D:\Prop\apps` | **0** |
| `ProxyEnabled = false` | `LiveMt5Registration.cs` | **1** (Starwave only) |

Consequences:

- Setting `MT5_STARWAVEFX_PROXY_ENABLED=true` in `.env` is **inert** on the C# live path.
- There is no `STARWAVEFX_PROXY_HOST` / `_PORT` binding in the factory.
- An operator cannot accidentally share `ACHIEVER_PROXY_*` onto Starwave through this factory.
- `LiveBrokerProbe` uses `CreateConnectorsFromEnvironment()` — same factory — so the measured Starwave census is **direct by construction**.
- `AddTraderIntelligence` registers those same two connectors (`DependencyInjection.cs` 45–46). Live ingest is also **direct** for Starwave.

A008 independently: “Starwave `NativeMt5Options.ProxyEnabled` is **hardcoded `false`**. `MT5_STARWAVEFX_PROXY_ENABLED` is **not** read.” Required Starwave set is only `MT5_STARWAVEFX_SERVER` / `_PORT` / `_LOGIN` / `_PASSWORD`.

### 3.2 `ApplyProxy` cannot fire if the pin holds

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

`ConnectCore` always calls `ApplyProxy()` then `Connect(endpoint, login, password, …)`. For Starwave the first call is a no-op (two independent reasons: `ProxyEnabled=false` **and** `ProxyHost` default `null`). Failed-connect error text still records `proxy={_opt.ProxyEnabled}` — that string will be `proxy=False` on Starwave.

### 3.3 Catalog seed matches the pin

`D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`

| Broker | `Server` | `Port` | `ManagerLogin` | `ProxyEnabled` | Proxy host:port |
|---|---|---:|---:|---|---|
| Achiever | `57.128.141.65` | 443 | 2027 | **true** | `81.29.145.69:49527` |
| StarwaveFX | `84.201.6.142` | 443 | 9904 | **unset → `false`** (`Broker.ProxyEnabled` bool default) | none |

Starwave seed block (lines 36–52) sets `Server` / `Port` / `ManagerLogin` / `ServerName` / `Mode=local` / `PoolSize=4` and **never** `ProxyEnabled` / `ProxyHost` / `ProxyPort`. That is catalog paint, not the live `ProxySet` path — live wiring is `LiveMt5Registration` — but it is consistent.

---

## 4. Architecture §8 + operator env (non-secret)

Architecture §8 (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` 417–437) publishes `MT5_STARWAVEFX_SERVER=84.201.6.142`, port `443`, login `9904`, `MT5_STARWAVEFX_PROXY_ENABLED=false`, password as `<SECRET>` only, then:

> No IP whitelist is currently required.
> Still design the connector so proxy/whitelist routing can be enabled later.

“Design so proxy can be enabled later” is **future-proofing the connector type** (`NativeMt5Options` already has the fields; Achiever uses them). It is **not** a license to turn the Achiever hop on today. Same connector class, **different options object**.

Architecture §7 (Achiever, contrast): required whitelisted outbound IP `81.29.145.69`. That sentence does **not** appear in §8.

`D:\Prop\.env` (gitignored; **not** rewritten; values published here are identifiers already in §8 or the boolean pin):

| Key | Presence | Value published here |
|---|---|---|
| `MT5_STARWAVEFX_SERVER` | PRESENT | `84.201.6.142` |
| `MT5_STARWAVEFX_PORT` | PRESENT | `443` |
| `MT5_STARWAVEFX_LOGIN` | PRESENT | `9904` |
| `MT5_STARWAVEFX_PASSWORD` | PRESENT | **REDACTED** (length 11; matches `CREDENTIALS_AND_COPY_STATUS.md`) |
| `MT5_STARWAVEFX_PROXY_ENABLED` | PRESENT | **`false`** |
| `MT5_STARWAVEFX_MODE` | PRESENT | `local` |
| `MT5_STARWAVEFX_POOL_SIZE` | PRESENT | `4` |
| `ACHIEVER_PROXY_ENABLED` | PRESENT | `true` — **Achiever only** |
| `ACHIEVER_PROXY_HOST` | PRESENT | `81.29.145.69` — **Achiever only** |
| `ACHIEVER_PROXY_PORT` | PRESENT | `49527` — **Achiever only** |
| `IS_MT5_PROXY_ENABLED` | **KEY_ABSENT** | C++ `AppConfig` default **false** |

C# live Starwave **does not** consume `MT5_STARWAVEFX_PROXY_ENABLED`. The key is documentation / C++-run hygiene. The **code** pin is `ProxyEnabled = false`.

---

## 5. Why the Achiever hop must not be reused

R012 (this workstation, SYN-only; **no** Manager password):

| Target | Result |
|---|---|
| `57.128.141.65:443` (Achiever) | **OPEN** 309 ms — TCP is not the Achiever failure mode |
| `84.201.6.142:443` (Starwave) | **OPEN** 189 ms — direct path is reachable |
| `81.29.145.69:49527` (Achiever HTTP proxy) | **OPEN** 199 ms |
| This host public egress (no proxy) | `106.219.132.213` ≠ Achiever allow-list `81.29.145.69` |

A004 operator matrix (binding, still true):

| Step | Achiever | StarwaveFX |
|---|---|---|
| `SetProxy` | **required** on this LAN | **must not** |
| `Connect` host:port | `57.128.141.65:443` | `84.201.6.142:443` |
| Manager login | `2027` | `9904` |
| Expected first fail if proxy omitted here | **1012** | n/a |
| Expected first fail if creds wrong | **3** | **3** |

A004 recipe text: *“Do **not** call `SetProxy`. Do **not** reuse Achiever’s HTTP hop — that hop exists to present Achiever’s allow-list IP, not Starwave’s.”*

W500_3 independently: *“Do **not** point Starwave at `:49527`.”*

Reasons the hop is **wrong**, even if Starwave might accept a connection from `81.29.145.69` (untested):

1. **Identity.** `:49527` presents `81.29.145.69`. That IPv4 is Achiever’s allow-list, not a Starwave requirement.
2. **Credentials.** Proxy auth is Achiever-path secret storage. Do not send it toward a broker that does not need it.
3. **Failure class.** Achiever 1012 is “egress is not 81.29.145.69.” Starwave has no such pin. Routing Starwave through the hop can only add **7** (timeout / CONNECT fail) or a confused source IP — it cannot “fix” a Starwave problem we have not measured.
4. **Live proof is direct.** The only measured Starwave Manager success from this product is `ProxyEnabled=false`.

Honesty: this slot did **not** run `ProxySet` against `84.201.6.142`. A “must not” here is **policy + code pin + measured-success-on-direct**, not a second 1012 log.

---

## 6. YoPips C++ — single-broker; Starwave run = proxy **off**

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.h` has **one** MT5 triple and **one** proxy stanza (`mt5_proxy_enabled` bound from **`IS_MT5_PROXY_ENABLED`**, default **false**). There is **no** `MT5_STARWAVEFX_*` field. Grep of that `config/` directory for `STARWAVEFX` / `starwave`: **0**.

`tests\mt5_group_probe.cpp` `configureProxyIfNeeded`:

```49:66:D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp
bool configureProxyIfNeeded(MT5Manager& manager, const AppConfig& config) {
    if (!config.mt5_proxy_enabled) return true;
    // ... require type/address/port, then manager.SetProxy(...)
}
```

`src\core\mt5_manager.cpp` `SetProxy` packs `address="IP:port"` and `auth="login:password"` (never log `auth`) and `Connect` re-applies once if `m_proxyConfig.enabled`.

Operator implication for a C++ Starwave group probe:

1. Same binary, **new** env: `MT5_SERVER=84.201.6.142`, `MT5_PORT=443`, `MT5_LOGIN=9904`, password from Starwave secret (never echo).
2. `IS_MT5_PROXY_ENABLED` **false / unset**. Do **not** copy Achiever `MT5_PROXY_*`.
3. `configureProxyIfNeeded` returns true without `SetProxy`.
4. `Connect(L"84.201.6.142:443", 9904, password, 0)` then `GetAllGroups`.

YoPips production `.env` (R012) is the **Achiever** stanza (`MT5_SERVER=57.128.141.65`, `MT5_PROXY_TYPE=HTTP`, `MT5_PROXY_ADDRESS=81.29.145.69`, `MT5_PROXY_PORT=49527`). Reusing that file for Starwave would either (a) hit Achiever with Starwave login **3**, or (b) if someone swapped only the server/login but left proxy on, send Starwave through the Achiever hop — **forbidden by this pin**. C++ also has the toggle bug: it reads `IS_MT5_PROXY_ENABLED`, **not** `MT5_PROXY_ENABLED` (R012). That bug is Achiever’s problem. For Starwave the safe C++ state is the default: proxy disabled.

Prop `mt5-sdk` wrappers (`D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` 33–57) are the same four methods (`SetProxy` / `Connect` / `GetAllGroups` / `GetUserLogins`). Same rule.

---

## 7. ALL groups + ALL manager traders (already measured; not re-fetched)

`LiveBrokerProbe` (`D:\Prop\tools\LiveBrokerProbe\Program.cs`) walks `LiveMt5Registration.CreateConnectorsFromEnvironment()` — **both** brokers — then `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`. Passwords are never written. Artifact:

`D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
utc `2026-08-18T08:42:16.8519545+00:00`

Re-read this slot (header + Starwave object at JSON line 45644):

| Broker | `connected` | `elapsedMs` | Groups | Traders | Open positions | Path |
|---|---|---:|---:|---:|---:|---|
| ACHIEVER | true | 7212.5885 | 8 | 6512 | 1506 | HTTP `ProxySet` then request APIs |
| STARWAVEFX | **true** | **6413.478** | **10** | **1948** | **478** | **direct**; same request APIs |
| **Total** | | | **18** | **8460** | **1984** |

Starwave groups (manager-visible set; JSON `groupNames`; currencies re-read this slot):

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
| **Sum (re-added)** | | **1948** |

`11+4+170+1735+22+0+0+4+0+2 = 1948`. Empty groups are still groups — do not drop them. If the server has more, they are outside manager `9904` ACL.

Achiever group-account sum independently: `2+179+4+5+4+6295+0+23 = 6512`.

`NativeMt5BrokerConnector.GetGroupsCore`: `GroupRequestArray("*")` then fallback `GroupTotal`/`GroupNext`.  
`GetAccountsAsync(null)`: every group name → `UserRequestArray` → fallback `UserGetByGroup` → if still empty `UserLogins` + `UserRequestByLogins`. That is the ALL-traders walk. Mapping keys `MT5_GROUP_*` are **not** a filter on this path.

Product ingest uses the same walk: `DealIngestionService.SyncCatalogAsync` calls `GetGroupsAsync` then `GetAccountsAsync(null)` (`DealIngestionService.cs` 45–49). No group-name filter. No `Take(`.

Dashboard prior pin (`CREDENTIALS_AND_COPY_STATUS.md`): `/api/traders` **8460**, `/api/groups` **18**. This slot did not re-query HTTP.

C++ `mt5_group_probe` prints **groups only**. ALL traders are the sibling request walk the C# connector already ran.

---

## 8. Copy to cTrader — no live orders (no loss)

| Check | Measured |
|---|---|
| `LiveRuntimeStatus.RealCopyEnabled` | Forced **false** in `AddTraderIntelligence` (`DependencyInjection.cs` 38–42). Comment: “Live NewOrderSingle is not implemented.” |
| `CTraderFixLogonHostedService` | Re-pins `_runtime.RealCopyEnabled = false` after QUOTE/TRADE logon (line 68). Log: “NewOrderSingle still disabled.” |
| `CTraderFixSession` | Builds **35=A** Logon only (tags 35/34/49/56/50/57/52/98/108/141/553/554). **No** `35=D`. |
| Product C# `(35, "D")` / `"35=D"` builders | **0** (`grep` over `D:\Prop\src`: no matches). `NewOrderSingle` appears only in comments / options / copyNote / FSM name. |
| `/api/settings` | `featureFlags.REAL_COPY_EXECUTION_ENABLED` = `runtime.RealCopyEnabled` (false). `FEATURE_COPY_TRADING_ENABLED=false` (literal). |
| Architecture §41 | `REAL_COPY_EXECUTION_ENABLED=false`. Logon / quotes / recon allowed; live send requires the flag **plus** healthy risk. |
| `CTraderFixOptions.RealCopyExecutionEnabled` | Default **false**. |
| `ShadowCopyEngine` | In-memory `SimulateEntry` / `SimulateExit`. No socket. |
| `LiveIngestHostedService` | Connect → catalog → deals → score. **No** FIX send. |
| `apps/fix-worker/Worker.cs` | Even if config `CTrader:RealCopyExecutionEnabled=true`, worker **refuses** and only stamps Disconnected + “NewOrderSingle remains off.” |
| `BrokerCatalogSeed` TRADE row | `LastError = "session up for logon/recon only; NewOrderSingle off"`. |

Class: **`SAFE_BY_ABSENCE`**, not a passed §68 / §70 review. A TLS Logon is **not** a NewOrderSingle. Do **not** flip the flag “to try one lot.”

---

## 9. Two-broker operator matrix (this desktop)

| Step | Achiever | StarwaveFX |
|---|---|---|
| Factory | `LiveMt5Registration` first connector | second connector |
| Server | `57.128.141.65:443` | `84.201.6.142:443` |
| Manager login | `2027` | `9904` |
| `ProxyEnabled` | env `ACHIEVER_PROXY_ENABLED` | **literal `false`** |
| `ProxySet` | HTTP `81.29.145.69:49527` (this LAN) | **must not** |
| Env proxy key the C# path reads | `ACHIEVER_PROXY_*` | **none** |
| Env proxy key that is documentation only | — | `MT5_STARWAVEFX_PROXY_ENABLED=false` |
| C++ same-box probe | `IS_MT5_PROXY_ENABLED=true` + `MT5_PROXY_*` | `IS_MT5_PROXY_ENABLED` unset/false; **no** `MT5_PROXY_*` |
| Measured live Connect | 8 / 6512 / 1506 | 10 / 1948 / 478 |
| Copy `35=D` | off | off |

---

## 10. Honesty / residual risk

| Item | State |
|---|---|
| This slot live-attached | **No** |
| Starwave-via-`:49527` Connect | **Not measured** — do not invent a 1012 |
| Starwave manager IP allow-list | **None documented**; architecture §8 |
| Future whitelist | Connector **type** should stay proxy-capable (Achiever already is). Starwave options stay **off** until a measured allow-list exists. |
| Dummy FakeMt5 10001/10002 | Policy off on API live path (`HasRealPasswords` requires **both** brokers). Not re-proven here. C42 (“live not proven”) is **stale** vs `LIVE_GROUPS_AND_TRADERS.json`. |
| §68 / §70 | Still **0 PASS** on live send. Irrelevant to Starwave direct-connect. |
| Capital at risk from **this** recipe | **None** — Manager read + FIX `35=A` only |
| Sibling slot 44 | Same conclusion; this slot re-measured product source + JSON header independently |

---

## 11. Files read (absolute)

| Path | Why |
|---|---|
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Starwave `ProxyEnabled = false` hard pin |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | `ApplyProxy` guard; ALL-groups / ALL-accounts request walk |
| `D:\Prop\src\Mt5\Connectors\BrokerRegistry.cs` | Dual-broker `All()` |
| `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs` | Documented proxy fields |
| `D:\Prop\src\Domain\Entities\Broker.cs` | `ProxyEnabled` bool default false |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | Starwave seed has no proxy |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | `RealCopyEnabled = false`; live connectors from factory |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Catalog/deals/score only |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetAccountsAsync(null)` = all logins |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 35=A only |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Re-pins RealCopy false |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | Default `RealCopyExecutionEnabled=false` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | copyNote / no-loss string |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | Simulate only |
| `D:\Prop\apps\api\Program.cs` | `/api/settings` flags |
| `D:\Prop\apps\fix-worker\Worker.cs` | Refuses send even if flag true |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | Dual-broker census tool |
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` | §7 / §8 / §41 |
| `D:\Prop\.env` | Key names + non-secret identifiers; **no values of secrets copied out** |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | 10 / 1948 direct |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | Starwave `connected=true`, 10 group names, `elapsedMs=6413.478` |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | Present/length only |
| `D:\Prop\reports\swarm\20260818\R012_proxy.md` | TCP OPEN 189 ms; Starwave does not need hop |
| `D:\Prop\reports\swarm\20260818\A004_yopips_group_probe.md` | “SetProxy must not” |
| `D:\Prop\reports\swarm\20260818\A008_env_wiring.md` | Starwave env set unread |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_44.md` | Sibling confirmation (not copied as evidence) |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.h` | Single-broker; no Starwave fields |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | `SetProxy` + Connect re-apply |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp` | `SetProxy` iff `IS_MT5_PROXY_ENABLED` |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` | Same `SetProxy` packing |

---

*End of W500_RESEARCH_84. Product source was not modified. No secrets printed. This slot did not live-attach.*
