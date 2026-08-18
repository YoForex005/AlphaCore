# W500_RESEARCH_104 — Confirm StarwaveFX must connect **direct** (no proxy)

| Field | Value |
|---|---|
| Slot | **104** |
| Agent | W500_RESEARCH_104 (senior engineer; read-only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_104.md` |
| Assigned | Confirm Starwave **must** connect **direct with no proxy**. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** Report + swarm index/log only. |
| Test / `.env` / config edited | **No.** Keys classified PRESENT + already-published non-secret identifiers only. |
| Secret values printed | **None.** No Manager password, no proxy user/password, no FIX password, no connection string. |
| Live Manager logon this pass | **Not re-run.** Census already measured `2026-08-18T08:42:16.8519545+00:00` by `LiveBrokerProbe`. |
| Trees read | `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tools\LiveBrokerProbe`, `D:\Prop\.env` (keys only), `D:\Prop\mt5-sdk`, `D:\Projects\YoPips\Backend\C++ Backend PropFirm` |
| Sibling pins (same question; independently re-read) | `W500_RESEARCH_44.md`, `W500_RESEARCH_64.md`, `W500_RESEARCH_84.md` |
| Binding law | Architecture §§7, 8, 41, 56; `LiveMt5Registration` Starwave `ProxyEnabled = false`; R012 / R022 / A004 / A008 / A58; LIVE census JSON |

This is a **confirmation pin** for slot 104. It does not call `Connect`, does not send `35=D`, and does not invent groups beyond the manager ACL.

---

## 0. Verdict (binding)

**CONFIRMED: StarwaveFX Manager must connect direct.**  
Do **not** call `ProxySet` / `SetProxy`. Do **not** reuse Achiever’s HTTP hop `81.29.145.69:49527`. Do **not** export process `HTTP_PROXY` for this broker. Do **not** invent `MT5_STARWAVEFX_PROXY_HOST`.

| Claim | Measured result | Class |
|---|---|---|
| Architecture §8 sets Starwave proxy **off** | **Yes.** `MT5_STARWAVEFX_PROXY_ENABLED=false`. Quote: “No IP whitelist is currently required.” | `LAW` |
| Architecture §56 sample agrees | **Yes.** Same flag `false` next to Achiever `ACHIEVER_PROXY_ENABLED=true` / host `81.29.145.69` / port `49527`. §56 has **no** Starwave proxy host/port/user/pass keys. | `LAW` |
| Product C# can turn Starwave proxy on via env | **No.** Only live factory hardcodes `ProxyEnabled = false` (L45). `MT5_STARWAVEFX_PROXY_ENABLED` is **unread** (`0` hits under `D:\Prop\src`). | `HARD_PIN` |
| Native `ApplyProxy` runs for Starwave | **No.** Guard is `_opt.ProxyEnabled && host non-empty`. Starwave sets neither. | `HARD_PIN` |
| Catalog seed paints a Starwave proxy row | **No.** Achiever seed `ProxyEnabled=true` + `81.29.145.69:49527`. Starwave seed omits proxy fields (`bool` default **false**). | `SEED` |
| Operator `.env` agrees | **Yes.** `MT5_STARWAVEFX_PROXY_ENABLED` PRESENT = `false`. No `MT5_STARWAVEFX_PROXY_HOST` / `_PORT` / `_USERNAME` / `_PASSWORD` keys. Achiever hop keys are a **different** broker. | `CONFIG` |
| This LAN can TCP to Starwave without the hop | **Yes (R012).** `84.201.6.142:443` **OPEN** 189 ms. Public egress `106.219.132.213` is **not** a Starwave allow-list problem. | `MEASURED_TCP` (prior) |
| Live Starwave census used the **direct** path | **Yes.** Probe constructs connectors via `LiveMt5Registration.CreateConnectorsFromEnvironment` (Starwave `ProxyEnabled=false`) → `connected=true`, **10 groups / 1948 traders / 478 positions**, `elapsedMs=6413.478`. | `MEASURED_LIVE` (prior) |
| Starwave needs Achiever’s HTTP proxy | **No.** That hop exists to present Achiever allow-list identity `81.29.145.69`. Starwave has **no** documented whitelist. | `MUST_NOT` |
| Starwave-via-Achiever-proxy was measured to 1012 | **Not tested.** Do **not** claim a Starwave 1012. Binding rule is “direct is required + proven,” not “proxy was proven to fail.” | `HONEST_GAP` |
| YoPips C++ has a Starwave proxy field | **No.** Single-broker `AppConfig`: `MT5_SERVER` / `MT5_LOGIN` / `IS_MT5_PROXY_ENABLED`. Zero `Starwave` / `84.201` hits under YoPips `src/` and `config/`. | `C++_SINGLE_BROKER` |
| Fetch ALL Achiever + Starwave groups + traders | **Implemented + already measured** as Manager **read**: Achiever **8 / 6512** (HTTP proxy) + Starwave **10 / 1948** (direct) = **18 / 8460**. Group account sums re-added this slot. | `READ_ONLY` |
| Copy to cTrader can place a live order today | **No.** Product `*.cs` under `D:\Prop\src` has **0** `35=D` / `MsgType="D"` / `(35, "D")`. `RealCopyEnabled` forced **false** in DI L41 and FIX hosted L68. Session builder is **35=A Logon only**. | `SAFE_BY_ABSENCE` |
| Risk to capital from this slot | **None.** Catalog APIs are Manager reads. Destination send path does not exist. | `NO_LOSS` |

Honest one-liner:

```text
Starwave = Connect(84.201.6.142:443, 9904, password) with ProxySet SKIPPED.
Achiever HTTP 81.29.145.69:49527 is the OTHER broker's allow-list hop — do not point Starwave at it.
C# hardcodes ProxyEnabled=false (env MT5_STARWAVEFX_PROXY_ENABLED is ignored; 0 product reads).
Live census already pulled ALL manager-visible Starwave groups/traders DIRECT (10 / 1948).
cTrader 35=D does not exist; RealCopyEnabled=false — copy cannot lose capital yet.
```

Do **not** enable `ACHIEVER_PROXY_*` on the Starwave connector. Do **not** invent `MT5_STARWAVEFX_PROXY_HOST`. Do **not** flip `REAL_COPY_EXECUTION_ENABLED`. Do **not** treat `demo\Maxmaster` as the universe (architecture §7: it is **absent** from both live dumps).

---

## 1. Goal split (do not collapse)

| Job | Live I/O allowed? | Path | Capital at risk? |
|---|---|---|---|
| **A.** Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders | **Yes** — native Manager **read** | Achiever: HTTP `ProxySet` then `Connect`. **Starwave: `Connect` only.** | **No** |
| **B.** Copy those traders to cTrader | **Not yet** — SHADOW / CopyIntent only | Irrelevant — no `35=D` builder | **Would be yes** the moment NewOrderSingle exists |

```text
Starwave Manager census (direct read)  = ALLOWED now
Achiever Manager census (proxy read)   = ALLOWED now (other slot: W500_3 / R012)
FIX 35=A Logon (QUOTE 5211 / TRADE 5212) = allowed for session proof
FIX 35=D NewOrderSingle                  = FORBIDDEN until gates + flag
```

Slot 104 confirms the Starwave half of job A. Job B stays **off**.

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

Binding prose immediately after that stanza (L435–437):

> No IP whitelist is currently required.
>
> Still design the connector so proxy/whitelist routing can be enabled later.

Contrast §7 (Achiever, L379–387): required whitelisted outbound IP `81.29.145.69`. That sentence **does not** apply to Starwave.

§56 secret-safe sample (L2049–2069) repeats the split in one file:

| Broker | Proxy keys in §56 |
|---|---|
| Achiever | `ACHIEVER_PROXY_ENABLED=true`, `ACHIEVER_PROXY_HOST=81.29.145.69`, `ACHIEVER_PROXY_PORT=49527` |
| StarwaveFX | **only** `MT5_STARWAVEFX_PROXY_ENABLED=false` |

A58 / R022: there is **no** §56 `MT5_STARWAVEFX_PROXY_HOST` / `_PORT` / `_USERNAME` / `_PASSWORD`. Inventing those names (A75 proposed example), or copying Achiever’s hop onto Starwave, is a **reject**.

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

Achiever (same method, L23–36) **does** bind `ACHIEVER_PROXY_ENABLED` / `HOST` / `PORT` / `USERNAME` / `PASSWORD`. Starwave does **not** bind any proxy key. The literal `false` is the pin.

Grep of `D:\Prop\src` for `MT5_STARWAVEFX_PROXY`: **0 hits**. The operator key exists only in `.env` / architecture docs. Flipping it to `true` **does nothing**.

DI (`DependencyInjection.cs` L45–46) registers **exactly** those two `CreateConnectors` instances. There is no third factory that could sneak a Starwave proxy on.

### 3.2 Native `ApplyProxy` is a no-op when disabled

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

`ConnectCore` always calls `ApplyProxy()` (L86) **before** `Connect`. For Starwave:

1. `ProxyEnabled` is `false` → return immediately.
2. `ProxyHost` is unset (`null`) → would also return even if someone flipped the bool in a debugger.

Then:

```88:92:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            var endpoint = $"{_opt.Server}:{_opt.Port}";
            var pump = CIMTManagerAPI.EnPumpModes.PUMP_MODE_GROUPS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_USERS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_POSITIONS;
            var res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, pump, 30000);
```

Intended Starwave wire:

```text
SMTManagerAPIFactory.Initialize + CreateManager
  → ApplyProxy() RETURNS IMMEDIATELY  (ProxyEnabled=false; ProxyHost unset)
  → Connect("84.201.6.142:443", 9904, password, PUMP_GROUPS|USERS|POSITIONS, 30s)
       fallback Connect(..., PUMP_MODE_NONE, 30s)
```

Native Manager API does **not** honor process `HTTP_PROXY`. The only hop is `ProxySet`. Starwave never reaches that call.

### 3.3 Catalog seed agrees (paint only; live path does not read it)

```36:52:D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs
        if (!await db.Brokers.AnyAsync(b => b.Code == BrokerCodes.StarwaveFx, ct))
        {
            db.Brokers.Add(new Broker
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"),
                Code = BrokerCodes.StarwaveFx,
                DisplayName = "StarwaveFX",
                Server = "84.201.6.142",
                Port = 443,
                ManagerLogin = 9904,
                ServerName = "StarwaveFX",
                Mode = "local",
                PoolSize = 4,
                Enabled = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
```

`Broker.ProxyEnabled` (`Domain/Entities/Broker.cs` L14) defaults to `false`. Achiever seed (L27–29) explicitly sets `ProxyEnabled = true`, `ProxyHost = "81.29.145.69"`, `ProxyPort = 49527`. Starwave seed **omits** those three fields.

Honesty: the seed row is **catalog paint**. Live `ProxySet` uses `LiveMt5Registration` options, not the EF row.

---

## 4. Operator `.env` (keys only — no values that are secrets)

`D:\Prop\.env` classified this pass:

| Key | Presence | Non-secret value / class |
|---|---|---|
| `MT5_STARWAVEFX_SERVER` | PRESENT | `84.201.6.142` |
| `MT5_STARWAVEFX_PORT` | PRESENT | `443` |
| `MT5_STARWAVEFX_LOGIN` | PRESENT | `9904` |
| `MT5_STARWAVEFX_PASSWORD` | PRESENT | **SECRET — not printed** |
| `MT5_STARWAVEFX_PROXY_ENABLED` | PRESENT | `false` |
| `MT5_STARWAVEFX_PROXY_HOST` / `_PORT` / `_USERNAME` / `_PASSWORD` | **KEY_ABSENT** | correct |
| `ACHIEVER_PROXY_ENABLED` | PRESENT | `true` (other broker) |
| `ACHIEVER_PROXY_HOST` | PRESENT | `81.29.145.69` (other broker) |
| `ACHIEVER_PROXY_PORT` | PRESENT | `49527` (other broker) |
| `ACHIEVER_PROXY_USERNAME` / `_PASSWORD` | PRESENT | **SECRET — not printed** |
| `IS_MT5_PROXY_ENABLED` | **KEY_ABSENT** | C++ name; unread by C# |
| `HTTP_PROXY` / `HTTPS_PROXY` | **KEY_ABSENT** (R012: process/user/machine also UNSET) | n/a |
| `REAL_COPY_EXECUTION_ENABLED` | PRESENT | `false` |

---

## 5. This LAN — Starwave TCP is open without the hop

R012 (`D:\Prop\reports\swarm\20260818\R012_proxy.md`) SYN-only probes (no credentials, no HTTP CONNECT, no Manager login):

| Target | Result |
|---|---|
| `84.201.6.142:443` (StarwaveFX Manager) | **OPEN** 189 ms |
| `57.128.141.65:443` (Achiever Manager) | **OPEN** 309 ms — reachability ≠ allow-list |
| `81.29.145.69:49527` (Achiever HTTP hop) | **OPEN** 199 ms |
| Public egress (no proxy) | `106.219.132.213` |

Achiever from this desktop without presenting `81.29.145.69` is a known **1012** (`MT_RET_AUTH_MANAGER_IPBLOCK`). Starwave has **no** such documented ACL. Direct TCP succeeding is consistent with “no whitelist required.”

This slot did **not** re-probe TCP and did **not** live-attach.

---

## 6. Fetch ALL groups + ALL manager traders (read-only)

### 6.1 Walk is request-first, group-complete

`GetGroupsCore` (`NativeMt5BrokerConnector.cs` L155): `GroupRequestArray("*")`, then cache `GroupTotal`/`GroupNext` only if the request list is empty.

`GetAccountsCore` (L189–214): when `group` is `null`, walks **every** group from `GetGroupsCore`, then `ReadAccountsForGroup`.

`ReadAccountsForGroup` (L223–232): `UserRequestArray` first; cache `UserGetByGroup` only on hard fail; empty → `UserLogins` + `UserRequestByLogins`.

Ingest (`DealIngestionService.cs` L45–48) and `LiveBrokerProbe` (`tools/LiveBrokerProbe/Program.cs` L19–26) both call `GetGroupsAsync` + `GetAccountsAsync(null)`. That is the ALL-groups / ALL-traders catalog.

### 6.2 Live census (prior measure; re-summed this slot)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`

| Field | Value |
|---|---|
| Probe | `LiveBrokerProbe` |
| UTC | `2026-08-18T08:42:16.8519545+00:00` |
| Factory | `LiveMt5Registration.CreateConnectorsFromEnvironment()` → Starwave `ProxyEnabled=false` |
| `STARWAVEFX.connected` | `true` |
| `STARWAVEFX.elapsedMs` | `6413.478` |
| `STARWAVEFX.groups` / `accounts` / `openPositions` | **10 / 1948 / 478** |
| `ACHIEVER.groups` / `accounts` / `openPositions` | **8 / 6512 / 1506** (HTTP proxy; other slot) |
| **Total** | **18 groups / 8460 traders / 1984 positions** |

Starwave group account sums re-added from JSON `groupNames[].accounts`:

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
| **Sum** | **1948** | matches `accounts` |

Achiever group account sums re-added:

`contest\yo-1step` 2 + `contest\yo-2step` 179 + `contest\yo-instant` 4 + `contest\yo-payp` 5 + `demo\yo-1step` 4 + `demo\yo-2step` 6295 + `demo\yo-instant` 0 + `demo\yo-payp` 23 = **6512**.

`demo\Maxmaster` is **absent** from both dumps. Architecture §7: “not the only group.” These 18 names are **all groups this pair of manager logins can see**.

JSON note: `"Passwords never written. Groups and manager logins only."`

Dashboard pin (`CREDENTIALS_AND_COPY_STATUS.md`): `/api/traders` listed **8460**; `/api/groups` listed **18**. Not re-queried this slot.

---

## 7. YoPips C++ — single broker; do not reuse Achiever hop for Starwave

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.h` has **one** MT5 triple (`mt5_server` / `mt5_login` / `mt5_password`) and **one** proxy stanza (`mt5_proxy_enabled` bound from **`IS_MT5_PROXY_ENABLED`**, default **false**). There is **no** `MT5_STARWAVEFX_*` field.

Grep of YoPips `src/` + `config/` for `Starwave` / `STARWAVE` / `84.201`: **0**.

`src/main.cpp` L201–224: `SetProxy` only when `config.mt5_proxy_enabled` and type/address/port complete; else `"MT5 proxy mode: DISABLED (global)"`.

`src/core/mt5_manager.cpp` L33–52 / L76–90: `ProxySet` packs `address=IP:port` and `auth=login:password`. That hop is the **Achiever** allow-list path on this LAN.

A Starwave C++ probe run **must**:

1. Point `MT5_SERVER` at `84.201.6.142`, `MT5_PORT=443`, `MT5_LOGIN=9904`.
2. Leave `IS_MT5_PROXY_ENABLED` **false / unset**.
3. **Not** copy Achiever `MT5_PROXY_*`.

YoPips production `.env` is the Achiever stanza (`MT5_SERVER=57.128.141.65`, `MT5_PROXY_TYPE=HTTP`, `MT5_PROXY_ADDRESS=81.29.145.69`, `MT5_PROXY_PORT=49527`). Reusing that file for Starwave would either (a) hit Achiever with the Starwave login, or (b) if someone swapped only server/login but left proxy on, send Starwave through the Achiever hop — **forbidden by this pin**. C++ also reads `IS_MT5_PROXY_ENABLED`, **not** `MT5_PROXY_ENABLED` (R012). That toggle bug is Achiever’s problem. For Starwave the safe C++ state is the default: proxy disabled.

Prop `mt5-sdk` is the same single-broker shape (`config/app_config.h` L39–45). Same rule.

---

## 8. Copy to cTrader must **not** send live orders (no loss)

| Check | Measured |
|---|---|
| `CTraderFixSession.cs` length | **135 / 135** |
| `NewOrderSingle` in that file | **0** |
| `35=D` / `(35, "D")` in that file | **0** |
| Only outbound MsgType | `(35, "A")` Logon (`BuildLogon` L96) |
| Wire writes | one `ssl.WriteAsync` of that Logon (L49); sockets disposed via `using` |
| Product `D:\Prop\src` `*.cs` `35=D` | **0** |
| Product `NewOrderSingle` hits | comments / flag docs / `MayRetryNewOrderSingle` helper only — **no sender** |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **false** (L35) |
| `DependencyInjection` | `RealCopyEnabled = false` (L38–41) with comment “Live NewOrderSingle is not implemented” |
| `CTraderFixLogonHostedService` | `_runtime.RealCopyEnabled = false` **after** QUOTE/TRADE logon (L68) |
| `/api/settings` | `featureFlags.REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled` |
| `.env` | `REAL_COPY_EXECUTION_ENABLED=false` (unread by DI pin; still operator-agrees) |
| `apps/fix-worker/Worker.cs` | even if `CTrader:RealCopyExecutionEnabled=true`, worker **refuses** and only logs (L45–46) |
| YoPips C++ `src` cTrader / `35=D` sender | **0** |

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
            (52, sendingTime),
            (98, "0"),
            (108, "30"),
            (141, "Y"),
            (553, username),
            (554, password)
        };
        return Assemble(fields);
    }
```

`LiveRuntimeStatus.Snapshot` copyNote when flag is false: `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."`

FIX QUOTE+TRADE logon may be **true** (tag 553 = integer account id). Logon ≠ send. `SAFE_BY_ABSENCE` ≠ a proven go-live gate. Do **not** flip the flag.

---

## 9. What must **not** be done

1. Do **not** set Starwave `ProxyEnabled=true` or reuse `ACHIEVER_PROXY_HOST=81.29.145.69:49527`.
2. Do **not** invent `MT5_STARWAVEFX_PROXY_HOST` (A58 reject list).
3. Do **not** treat `MT5_STARWAVEFX_PROXY_ENABLED=true` as a live switch — C# never reads it.
4. Do **not** enable YoPips `IS_MT5_PROXY_ENABLED` on a process aimed at Starwave.
5. Do **not** send `35=D` / arm `REAL_COPY_EXECUTION_ENABLED`.
6. Do **not** claim this slot re-attached Manager. Census is prior measure + arithmetic re-sum.
7. Do **not** claim Starwave-via-proxy was proven to 1012. It was **not** tested.
8. Do **not** print Manager / proxy / FIX passwords.

---

## 10. Honesty residuals

| Residual | Status |
|---|---|
| This slot live-attach | **No.** Prior `LiveBrokerProbe` utc `08:42:16Z`. |
| Starwave + Achiever HTTP hop | **Not measured.** Direct is required + proven; proxy-fail is untested. |
| Architecture “design so proxy can be enabled later” | **Not implemented.** Env key unread; no Starwave proxy fields on `NativeMt5Options` wiring. |
| A75 invented Starwave proxy host keys | **Rejected** by A58 / R022 / this pin. |
| C42 “live MT5 not proven” | **Stale** vs `LIVE_GROUPS_AND_TRADERS.json` (`connected=true` both brokers). Use the later file. |
| Dashboard `/api/groups` / `/api/traders` this pass | **Not re-queried.** Prior pin 18 / 8460. |
| YoPips cannot hold both brokers in one `AppConfig` | **True.** Dual-broker is the C# `LiveMt5Registration` path only. |

---

## 11. Files read (absolute)

| Path | Why |
|---|---|
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Starwave `ProxyEnabled = false` hard pin |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | `ApplyProxy` skip + `Connect` + `GroupRequestArray` / `UserRequestArray` |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | Starwave seed omits proxy; Achiever seed has hop |
| `D:\Prop\src\Domain\Entities\Broker.cs` | `ProxyEnabled` default false |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Native ×2; `RealCopyEnabled=false` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Catalog via `SyncCatalogAsync` (Manager read) |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetAccountsAsync(null)` = ALL logins |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 35=A only |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Forces `RealCopyEnabled=false` after logon |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | Default `RealCopyExecutionEnabled=false` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Snapshot copyNote / no-loss text |
| `D:\Prop\apps\api\Program.cs` | `/api/settings` flag bind |
| `D:\Prop\apps\fix-worker\Worker.cs` | Refuses NewOrderSingle even if config true |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | Factory + ALL groups/accounts dump |
| `D:\Prop\.env` | Key presence only |
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` | §§7, 8, 56 |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | Measured 10/1948 + 8/6512 |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | Census write-up |
| `D:\Prop\reports\swarm\20260818\R012_proxy.md` | TCP OPEN 189 ms; Starwave does not need hop |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | Dashboard 18/8460; copy off |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.h` | Single-broker; no Starwave |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\main.cpp` | `SetProxy` only if `IS_MT5_PROXY_ENABLED` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | `ProxySet` packing |

---

## 12. Verdict recap

**CONFIRMED.** StarwaveFX Manager **must** connect **direct** (`84.201.6.142:443`, login `9904`) with `ProxySet` skipped. C# hardcodes `ProxyEnabled=false`; `MT5_STARWAVEFX_PROXY_ENABLED` is unread. Achiever HTTP `81.29.145.69:49527` is the **other** broker. Prior live census: Starwave **10 groups / 1948 traders / 478 positions** direct; combined **18 / 8460**. Product has **0** `35=D`; `RealCopyEnabled` forced **false**. Risk to capital **NONE**. This slot did not live-attach. Product source was not modified. No secrets printed.

*End of W500_RESEARCH_104.*
