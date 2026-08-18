# R022 — HTTP proxy scope: Achiever only (`81.29.145.69:49527`)

| Field | Value |
|---|---|
| Agent | R022 (senior engineer, proxy **scope** pin only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:54:43+05:30 (2026-08-18T08:24:43Z) |
| Assigned | Achiever needs HTTP proxy `81.29.145.69:49527`. Starwave does not. Write this report. Do not copy passwords. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\R022_proxy_scope.md` |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Config / `.env` / `appsettings` edited | **No.** |
| Secret values printed | **None.** Username/password classified PRESENT + length only. |
| HEAD at measure | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`) |
| Binding law | Architecture §§7, 8, 55, 56; A54 (Windows worker / 1012); A58 (slot binder; no invented Starwave proxy host keys); A76 (redact proxy auth); C42 (live Manager **NOT PROVEN**); C55 (egress IP class, sibling — not this pin) |
| Lab pin (this file) | Achiever Manager TCP **must** go through HTTP proxy `81.29.145.69:49527`. StarwaveFX Manager TCP **must not**. |

This is a **scope** document, not a live Connect proof and not a rewrite of C55. C55 classifies `81.29.145.69` as a non-secret identifier. R022 answers **which sockets may use the HTTP hop at that host:port**.

---

## 0. Verdict (binding)

**Achiever needs the HTTP proxy `81.29.145.69:49527`.**  
**StarwaveFX does not.**  
**Do not apply that hop process-wide.**

| Surface | Proxy `81.29.145.69:49527`? | Why |
|---|---|---|
| Achiever Manager to `57.128.141.65:443` (login `2027`) | **YES — required** | Lab pin + §56 `ACHIEVER_PROXY_ENABLED=true` + Achiever IP allow-list (`ACHIEVER_EGRESS_IP=81.29.145.69`). Type is **HTTP** (`MTProxyInfo::PROXY_HTTP = 2`), not SOCKS5. |
| StarwaveFX Manager to `84.201.6.142:443` (login `9904`) | **NO** | §8 / §56 `MT5_STARWAVEFX_PROXY_ENABLED=false`. No Starwave whitelist. No §56 Starwave proxy host/port/user/pass keys. |
| cTrader FIX QUOTE / TRADE | **NO** | Different venue (`live-us-eqx-01.p.c-trader.com`). Not an `ACHIEVER_PROXY_*` consumer. |
| `apps/api`, Postgres, Redis, React, NuGet | **NO** | Linux compose / local loopback. Compose has **zero** `HTTP_PROXY` keys. |
| Process `HTTP_PROXY` / `HTTPS_PROXY` / `ALL_PROXY` | **FORBIDDEN** | Would steal Starwave, FIX, package restore, and health checks onto the Achiever hop. Scope is **per Achiever Manager session** via `MT5Manager::SetProxy`, not WinHTTP / .NET default proxy. |
| `FakeMt5BrokerConnector` / demo seed | **N/A** | No Manager TCP. Seed does not set `Broker.Proxy*`. C42: live attach is **NOT PROVEN**. |

Honest one-liner: **HTTP `81.29.145.69:49527` is an Achiever-only Manager hop. Starwave stays direct. Never copy proxy user/password into this tree, logs, or dashboards.**

This file does **not** claim a measured live `Connect` through that proxy. C# still runs two fakes (C42). C++ `SetProxy` exists and is unused by the C# worker.

---

## 1. Binding quotes

Architecture §7 (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`):

> Required whitelisted outbound IP:
>
> `81.29.145.69`
>
> If proxying is required, credentials must be in secret storage/environment variables.
>
> Never log proxy credentials.

Architecture §8 (StarwaveFX):

```env
MT5_STARWAVEFX_PROXY_ENABLED=false
```

> No IP whitelist is currently required.
>
> Still design the connector so proxy/whitelist routing can be enabled later.

Architecture §56 (Secret-Safe Example Configuration):

```env
ACHIEVER_EGRESS_IP=81.29.145.69

# Optional proxy
ACHIEVER_PROXY_ENABLED=true
ACHIEVER_PROXY_HOST=81.29.145.69
ACHIEVER_PROXY_PORT=49527
ACHIEVER_PROXY_USERNAME=<SECRET>
ACHIEVER_PROXY_PASSWORD=<SECRET>
```

```env
MT5_STARWAVEFX_PROXY_ENABLED=false
```

§56’s comment says “Optional proxy.” **This lab pin overrides the comment for Achiever:** the hop is **required**. The same stanza already prints `ENABLED=true` and the host:port used here. Starwave stays `false`.

A58 §4.2:

> There is **no** StarwaveFX proxy host / port / username / password key in §56. … If `MT5_STARWAVEFX_PROXY_ENABLED=true` and `Proxy.Host` is empty → **fail closed** (misconfiguration), do **not** guess `ACHIEVER_PROXY_HOST`.

A54 §7.2 listed native NIC/NAT as preference #1 and this proxy as preference #2. **R022 supersedes that preference order for this lab:** Achiever uses the HTTP proxy. Native-egress-only is not the standing path.

---

## 2. Same digits, two jobs (do not collapse)

`81.29.145.69` is **two** non-secret keys (C55). Mixing them is how operators send Starwave through the wrong hop or treat the port as a secret.

| Key | Value | Job |
|---|---|---|
| `ACHIEVER_EGRESS_IP` | `81.29.145.69` | Source address Achiever’s Manager ACL must see. Not a Connect argument. Failure mode **1012** `MT_RET_AUTH_MANAGER_IPBLOCK`. |
| `ACHIEVER_PROXY_HOST` | `81.29.145.69` | HTTP proxy listen address. |
| `ACHIEVER_PROXY_PORT` | `49527` | HTTP proxy listen port. **This port is the hop.** It is not the Manager port (`443`). |
| `ACHIEVER_PROXY_ENABLED` | `true` | Achiever slot master. |

The broker destination remains `MT5_SERVER=57.128.141.65` port `443`. The worker dials the **proxy**; the proxy presents `81.29.145.69` to Achiever.

Starwave destination remains `MT5_STARWAVEFX_SERVER=84.201.6.142` port `443`, **direct**. Starwave has **no** `ExpectedEgressIp` (A58, E036).

Vendor type enum (`D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`):

| `MTProxyInfo` type | Value | This lab |
|---|---:|---|
| `PROXY_SOCKS4` | 0 | **No** |
| `PROXY_SOCKS5` | 1 | **No** (C++ probe default if type empty — **wrong** for this hop) |
| `PROXY_HTTP` | 2 | **Yes** — Achiever only |

§56 has **no** `ACHIEVER_PROXY_TYPE` key (A58 explicitly rejects inventing it). When a live binder is written, Achiever’s `SetProxy` type must still be **HTTP / 2**. Do not inherit `mt5_group_probe.cpp`’s SOCKS5 default.

---

## 3. Scope matrix (apply / do not apply)

### 3.1 Apply (exactly one socket class)

| Rule | Detail |
|---|---|
| Who | Windows `mt5-worker` Achiever Manager **session** (and only that session’s pool members). |
| How | `MT5Manager::SetProxy(PROXY_HTTP, L"81.29.145.69", 49527, user, pass)` **before** `Connect` to `57.128.141.65:443`. C++ already reapplies on Connect (`mt5_manager.cpp` lines 76–95). |
| Auth | `ACHIEVER_PROXY_USERNAME` / `ACHIEVER_PROXY_PASSWORD` from env / user-secrets. Never commit, never log, never put on `Broker` (entity has no password columns — keep it that way). |
| Registry | Same `IMt5BrokerConnector` type as Starwave. Broker-specific code lives only in the **slot binder** (A58). `if (Achiever) SetProxy` is allowed there; a second connector class is not. |

### 3.2 Do not apply

| Target | Why forbidden |
|---|---|
| StarwaveFX slot | `MT5_STARWAVEFX_PROXY_ENABLED=false`. Sharing Achiever’s host:port would hairpin Starwave through an Achiever-ACL proxy and invent `MT5_STARWAVEFX_PROXY_HOST` (A58 reject list). |
| Process / user / machine `HTTP_PROXY` | Out of scope. Contaminates Starwave, FIX, API, and restore. Grep of product `*.{cs,json,yml,env}`: **zero** `HTTP_PROXY` / `HTTPS_PROXY` / `ALL_PROXY` keys. Keep it that way. |
| Linux `docker-compose.yml` | Compose runs postgres / redis / api only. Comment: native MT5 workers stay on Windows. No proxy env. |
| `docs/deployment.md` example `proxy.example.com:8080` | Illustrative, **not** this lab’s hop. Operators must not copy that host:port. |
| Dashboard / `GET /settings` / React | Host/port may be shown (A76 Keep). Username/password **never**. |
| C++ `IS_MT5_PROXY_ENABLED` / `MT5_PROXY_*` as a second vocabulary | A58: those are C++ aliases. Product binder uses `ACHIEVER_PROXY_*`. Do not enable the C++ single-broker proxy and then also attach Starwave in-process. |

### 3.3 Design-later (not this pin)

§8 still requires the **same** connector to accept a future Starwave proxy **without** a second codebase. That is a field on the options object, not a license to turn Starwave’s flag on or to reuse `81.29.145.69:49527`. There is still **no** Starwave whitelist.

---

## 4. Measured tree (read-only; no secret values)

### 4.1 Local `.env` (gitignored) — flags and non-secret hop only

Path `D:\Prop\.env`: **3484** bytes, SHA-256 `A4EF94B990EE389C7E7900B599A60AE10E0C16E96E4B5DA612302759958982D7`, LastWriteTimeUtc `2026-08-18T08:22:24.1111072Z`. Values below are **flags / host / port**. Auth is **length only**.

| Key | Class | What may be written |
|---|---|---|
| `ACHIEVER_EGRESS_IP` | non-secret | `81.29.145.69` |
| `ACHIEVER_PROXY_ENABLED` | non-secret | `true` |
| `ACHIEVER_PROXY_HOST` | non-secret | `81.29.145.69` |
| `ACHIEVER_PROXY_PORT` | non-secret | `49527` |
| `ACHIEVER_PROXY_USERNAME` | **SECRET** | **PRESENT**, length **15**. Value **not** copied. |
| `ACHIEVER_PROXY_PASSWORD` | **SECRET** | **PRESENT**, length **15**. Value **not** copied. |
| `MT5_STARWAVEFX_PROXY_ENABLED` | non-secret | `false` |
| `MT5_STARWAVEFX_PROXY_HOST` / `_PORT` / `_USERNAME` / `_PASSWORD` | — | **NO_KEY** (correct; do not invent). |

This report did **not** print, quote, or hash the username/password strings.

### 4.2 Product C# — fields exist, nothing binds the hop

| Surface | Proxy fact |
|---|---|
| `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs` | `ProxyEnabled` / `ProxyType` / `ProxyHost` / `ProxyPort` / `ProxyLogin` / `ProxyPassword` exist. `ProxyType` is **not** a §56 key. No hardcoded `81.29.145.69` or `49527`. |
| `D:\Prop\src\Domain\Entities\Broker.cs` | `ProxyEnabled` / `ProxyHost` / `ProxyPort` only. **No** proxy auth columns (correct). |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Achiever + StarwaveFX rows: `Server`/`Port`/`ManagerLogin`/`PoolSize` only. `ProxyEnabled` left default **false**. Host/port **null**. |
| `D:\Prop\apps\mt5-worker\appsettings.json` | Logging only. No proxy bind. |
| `D:\Prop\apps\mt5-worker\Program.cs` | DI + `DemoSeeder`. No `SetProxy`. |
| `FakeMt5BrokerConnector.CreateDefault()` | Two in-memory brokers. Never reads `Proxy*`. |
| `src/` + `apps/` grep `81.29.145.69` / `49527` | **Absent** from product C#. Literals live in architecture + reports + local `.env`. |

### 4.3 C++ capability (not a live proof)

| Surface | Fact |
|---|---|
| `mt5_manager.cpp` `SetProxy` | Builds `MTProxyInfo`, logs **type + address + port only**, applies again on `Connect`. Auth goes into `proxy.auth` as `login:password` — that buffer must never hit Serilog (A04 / A76). |
| `mt5-sdk/config` | `IS_MT5_PROXY_ENABLED` + `MT5_PROXY_*`. Single-broker. Cannot attach Starwave in-process today (A58). |
| Probes | Default type **SOCKS5** unless `MT5_PROXY_TYPE=HTTP`. Wrong default for this lab hop. |
| `MT5HttpClient::GetGroupDetails` | Unrelated stub (D67). “HTTP client proxy mode” there means the **remote HTTP bridge**, not `81.29.145.69:49527`. |

### 4.4 Compose / docs

| Surface | Fact |
|---|---|
| `D:\Prop\docker-compose.yml` | postgres / redis / api. **No** proxy env. Comment keeps Manager DLL off Linux. |
| `D:\Prop\docs\deployment.md` | Mentions Achiever egress `81.29.145.69` and a **fictional** `proxy.example.com:8080`. Do not treat that example as the lab hop. |

---

## 5. Classification table (pin)

| Item | Class | Redact? | In this report? |
|---|---|---|---|
| `81.29.145.69` | **NON-SECRET** (C55) | No | Yes |
| `49527` | **NON-SECRET** | No | Yes |
| HTTP proxy type (`PROXY_HTTP` / 2) | **NON-SECRET** | No | Yes |
| `ACHIEVER_PROXY_ENABLED=true` | **NON-SECRET** | No | Yes |
| `MT5_STARWAVEFX_PROXY_ENABLED=false` | **NON-SECRET** | No | Yes |
| `ACHIEVER_PROXY_USERNAME` | **SECRET** | Always | PRESENT, len 15 only |
| `ACHIEVER_PROXY_PASSWORD` | **SECRET** | Always | PRESENT, len 15 only |
| `MT5_PASSWORD` / `MT5_STARWAVEFX_PASSWORD` | **SECRET** | Always | Not copied |
| Starwave server `84.201.6.142` | **NON-SECRET** (E036) | No | Yes (destination, direct) |

---

## 6. Operator rules (when a live connector exists)

1. Enable Achiever proxy only: `ACHIEVER_PROXY_ENABLED=true`, host `81.29.145.69`, port `49527`, type **HTTP**.
2. Leave `MT5_STARWAVEFX_PROXY_ENABLED=false`. Do not set Starwave proxy host to the Achiever hop.
3. Do not export `HTTP_PROXY=http://81.29.145.69:49527` (or authenticated URL form) on the Windows worker, Linux compose, or developer shells used for `dotnet` / `npm`.
4. Never log or persist proxy user/password. C++ `SetProxy` already concatenates `login:password` into `MTProxyInfo.auth` — keep that off structured logs (architecture §7).
5. A 1012 on **Achiever** is routing/proxy/ACL, not a reason to flip Starwave’s flag.
6. A 1012 on **Starwave** is unexpected (no whitelist today). Do **not** “fix” it by pointing Starwave at `81.29.145.69:49527`.
7. Dashboard “Connected” is still a lie until C42 is overturned with a measured Manager session.

This agent did **not** run `Connect`, `SetProxy`, or any TCP probe to `81.29.145.69:49527` or `57.128.141.65:443`.

---

## 7. Tension with earlier reports (resolved here)

| Earlier text | R022 ruling |
|---|---|
| §56 comment “Optional proxy” | Comment is stale **for Achiever in this lab**. Keys already say enabled + host + port. |
| A54 §7.2 “prefer native egress, else proxy” | **Superseded for Achiever.** Use the HTTP proxy. Native-only is not the standing path. |
| C55 “optional hop when worker cannot egress as that IP” | Classification of the **IPv4** still stands (non-secret). **Scope** of the **:49527 HTTP hop** is this file: Achiever required, Starwave off. |
| A75 / B40 / D61 example `ACHIEVER_PROXY_ENABLED=false` | Fail-closed **example** files. Local operator `.env` is `true`. Do not “fix” the lab by copying the example’s `false`. |
| A75 invented `MT5_STARWAVEFX_PROXY_HOST` in a proposed example | **Rejected** by A58 §56 allow-list. R022 does not add those keys. |
| `docs/deployment.md` `proxy.example.com:8080` | Not the lab hop. |

---

## 8. Product-source status (unchanged)

| Check | Result |
|---|---|
| This agent edited `D:\Prop\src` / `apps` / `mt5-sdk` / `.env` | **No** |
| Hardcoded `81.29.145.69:49527` under `src/` or `apps/` | **Absent** |
| Live Achiever proxy Connect proven | **No** (C42) |
| Starwave routed through this proxy | **No** (flag false; no binder; fake only) |
| Passwords copied into this report | **No** |

---

## 9. Done criteria

- [x] Achiever HTTP proxy scoped to `81.29.145.69:49527` and marked **required**.
- [x] StarwaveFX marked **no proxy** (`PROXY_ENABLED=false`, no host reuse).
- [x] Process-wide `HTTP_PROXY` forbidden.
- [x] Proxy type pinned **HTTP**, not SOCKS5 default.
- [x] Same-IP egress vs proxy-port distinguished.
- [x] Secret username/password not copied (PRESENT, length 15 only).
- [x] Product source not modified.
- [x] Live Connect not claimed.
