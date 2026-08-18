# R012 — Local Achiever connect needs the HTTP proxy

| Field | Value |
|---|---|
| Agent | R012 |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:56:14+05:30 (host/env); TCP + public-IP probes immediately after |
| Host | `DESKTOP-FQPFPKE` / user `ADMIN` |
| Artifact | `D:\Prop\reports\swarm\20260818\R012_proxy.md` |
| Assigned | Architecture says Achiever needs egress `81.29.145.69`. YoPips `.env` has HTTP proxy. Write whether **local connect** needs that proxy. Do **not** copy the proxy password. Do **not** modify product source. |
| Product source modified | **No.** This report is the only product-adjacent write. `.env` files were **read**, not rewritten. |
| Live Manager logon this pass | **Not attempted** (no `Connect` with manager password; no authenticated HTTP CONNECT). |
| Secret values printed | **None.** Proxy user/password, `MT5_PASSWORD`, and unrelated YoPips secrets are classified + length only. |

This is a **read-only network / config ruling**. It does not claim a live Achiever session for the C# product.

---

## 0. Verdict

**YES. Local Achiever Manager connect from this Windows workstation needs the HTTP proxy** (or a host whose default public source IP is already `81.29.145.69`).

It is **not** optional on `DESKTOP-FQPFPKE` today. Measured public egress is **`106.219.132.213`**, which is **not** the Achiever allow-list. Historical YoPips **local** Manager connects from this same class of machine, with the proxy **disabled in process**, failed **1012** (`MT_RET_AUTH_MANAGER_IPBLOCK`) on both pump and no-pump fallback.

| Question | Answer |
|---|---|
| Does Achiever require a specific source IP? | **Yes.** Architecture §7: required whitelisted outbound IP `81.29.145.69` (non-secret; C55). |
| Does *this* host already present that IP? | **No.** Three public-IP oracles returned `106.219.132.213`. LAN default route is `192.168.1.59` → `192.168.1.1`. |
| Does “local connect” mean `MT5_MODE=local` (native Manager DLL), not `MT5_MODE=remote` HTTP? | **Yes.** |
| Is the YoPips “HTTP proxy” a system `HTTP_PROXY` env? | **No.** Process/User/Machine `HTTP_PROXY` / `HTTPS_PROXY` are **UNSET**. It is Manager `ProxySet` type `PROXY_HTTP` (`MTProxyInfo::PROXY_HTTP = 2`) to `81.29.145.69:49527`. |
| Can TCP reach Achiever without the proxy? | **Yes** — `57.128.141.65:443` **OPEN** (309 ms). Reachability is not the failure mode. **Allow-list** is. |
| Can TCP reach the proxy listener? | **Yes** — `81.29.145.69:49527` **OPEN** (199 ms). `:443` on that host **REFUSED**; `:80` **TIMEOUT**. |
| Does StarwaveFX local connect need this proxy? | **No** (architecture §8 / Prop `MT5_STARWAVEFX_PROXY_ENABLED=false`; no whitelist documented). |
| Would local connect work **without** proxy if the worker ran *on* `81.29.145.69`? | **Yes, in principle.** A54 preference 1: native egress **is** the allow-list. That is **not** this desktop. |
| Did YoPips actually *apply* the HTTP proxy on recorded local starts? | **No.** Logs: `MT5 proxy mode: DISABLED (global)` then **1012**. Toggle key mismatch (see §4). |

Honest one-liner: **on this LAN box, Achiever local `Connect` without presenting `81.29.145.69` is a known 1012; the YoPips HTTP proxy is the intended hop; it is required here, not decorative.**

---

## 1. What “local connect” and “HTTP proxy” are (do not mix)

```
MT5_MODE=local
  → LoadLibrary MT5APIManager64.dll
  → optional IMTManagerAPI::ProxySet (SOCKS4 / SOCKS5 / HTTP)
  → Connect(57.128.141.65:443, manager login, password, pump, 30s)
  → broker sees TCP source = this NIC/NAT  OR  the proxy's egress
```

| Term | This report's meaning | Not this |
|---|---|---|
| Local connect | `MT5_MODE=local`: in-process Manager API (A14). | Loopback HTTP. `MT5_MODE=remote` / `MT5HttpClient`. |
| HTTP proxy | `MT5_PROXY_TYPE=HTTP` → `MTProxyInfo::PROXY_HTTP`. HTTP CONNECT (incl. NTLM) in the Manager DLL. | Process `HTTP_PROXY`. YoPips `TRUSTED_PROXIES` (web reverse-proxy CIDRs). `MT5_REMOTE_URL`. |
| Egress IP | Source address Achiever allow-lists: `81.29.145.69`. | A Connect() argument. A58: `ACHIEVER_EGRESS_IP` is documentation / `ExpectedEgressIp`. |
| Isolated YoPips `local-runtime/.env` | `MT5_MODE=remote` → `http://127.0.0.1:65535` (intentionally not live). | Evidence about live local Manager. |

Architecture §7 (binding):

> Required whitelisted outbound IP: `81.29.145.69`  
> If proxying is required, credentials must be in secret storage/environment variables.  
> Never log proxy credentials.

“If required” = **when the worker does not already source-NAT as that IP**. This host does not.

Architecture §56 sample turns the proxy **on** at the same digits (`ACHIEVER_PROXY_HOST=81.29.145.69`, port `49527`). Same IPv4, two keys (C55 §2.3): allow-list identity vs optional hop that **presents** that identity.

---

## 2. Measured topology (this machine, this pass)

| Fact | Value |
|---|---|
| Hostname | `DESKTOP-FQPFPKE` |
| Default route | Ethernet `192.168.1.59/24` → `192.168.1.1` |
| Other IPv4 | Tailscale `100.65.45.13`; three Wi-Fi APIPA `169.254.*` |
| Public egress (no proxy) | **`106.219.132.213`** (`api.ipify.org`, `ifconfig.me/ip`, `icanhazip.com` — all 200) |
| Equals `ACHIEVER_EGRESS_IP`? | **No** |
| Process / User / Machine `IS_MT5_PROXY_ENABLED`, `ACHIEVER_PROXY_*`, `HTTP_PROXY` | **All UNSET** (only `.env` files carry the stanza) |

TCP probes (SYN only; **no** credentials, **no** HTTP CONNECT, **no** Manager login):

| Target | Result |
|---|---|
| `57.128.141.65:443` (Achiever Manager) | **OPEN** 309 ms |
| `84.201.6.142:443` (StarwaveFX Manager) | **OPEN** 189 ms |
| `81.29.145.69:49527` (documented proxy port) | **OPEN** 199 ms |
| `81.29.145.69:443` | **REFUSED** |
| `81.29.145.69:80` | **TIMEOUT** ~3 s |

So: the desktop can speak TCP to the Manager port, and can speak TCP to the proxy port. Direct Manager TCP from `106.219.132.213` is still the 1012 path.

---

## 3. YoPips `.env` HTTP proxy (secrets redacted)

Path: `D:\Projects\YoPips\Backend\C++ Backend PropFirm\.env`  
SHA-256 `E3F07B1595AEB8CB8D66A5410BCA7E26974090EF05857165C7190FBBF1A0A40A` / 4585 B / mtime 2026-07-16T07:54:21Z.

| Key | Presence | Value published here |
|---|---|---|
| `MT5_MODE` | PRESENT | `local` |
| `MT5_SERVER` | PRESENT | `57.128.141.65` |
| `MT5_PORT` | PRESENT | `443` |
| `MT5_LOGIN` | PRESENT | `2027` (non-secret manager number; same as architecture §7) |
| `MT5_PASSWORD` | PRESENT | **REDACTED** (length 8) |
| `MT5_PROXY_TYPE` | PRESENT | `HTTP` |
| `MT5_PROXY_ADDRESS` | PRESENT | `81.29.145.69` |
| `MT5_PROXY_PORT` | PRESENT | `49527` |
| `MT5_PROXY_LOGIN` | PRESENT | **REDACTED** (length 15) |
| `MT5_PROXY_PASSWORD` | PRESENT | **REDACTED** (length 18) |
| `MT5_PROXY_ENABLED` | PRESENT | `true` |
| `IS_MT5_PROXY_ENABLED` | **KEY_ABSENT** | — |
| `TRUSTED_PROXIES` | PRESENT | RFC1918 + loopback CIDRs — **web** trusted hops, **not** Manager `ProxySet` |

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\local-runtime\.env` is a **different** file: `MT5_MODE=remote`, dead `MT5_REMOTE_URL=http://127.0.0.1:65535`. It does **not** carry the HTTP Manager proxy. Comment in that file: keep local DB/API testing independent from the live manager.

The production YoPips `.env` **intends** HTTP `ProxySet` for local Achiever. That is the operator answer this report was asked for.

---

## 4. Toggle bug: YoPips `.env` does not enable the code path

YoPips `AppConfig` (and the extracted Prop SDK) bind the master switch from **`IS_MT5_PROXY_ENABLED`**, default **false**:

```172:172:D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp
    cfg.mt5_proxy_enabled = get_bool("IS_MT5_PROXY_ENABLED", false);
```

```135:135:D:\Prop\mt5-sdk\config\app_config.cpp
    cfg.mt5_proxy_enabled = get_bool("IS_MT5_PROXY_ENABLED", false);
```

`main.cpp` only calls `SetProxy` when that flag is true; else it logs `MT5 proxy mode: DISABLED (global)` and connects **direct**.

`MT5_PROXY_ENABLED=true` in the YoPips `.env` is **not read**. There is no `IS_MT5_PROXY_ENABLED` in that file, and none in Windows env. Result: **proxy stanza is inert**.

Recorded local starts (same tree) all match that prediction. No log line `proxy configured` / `proxy applied` exists. Samples:

| Log | Mode line | Proxy line | Connect |
|---|---|---|---|
| `runtime-backend-3001.out.log` 2026-07-16T15:49:48 | `MT5 mode: LOCAL (SDK)` / `mt5=57.128.141.65` | `MT5 proxy mode: DISABLED (global)` | **1012** pump; **1012** no-pump; pool sessions 1012 then a 7 (network) |
| `runtime-smoke-3002.out.log` | LOCAL | DISABLED | **1012** |
| `local-runtime/backend-20260717-142524.stdout.log` | LOCAL | DISABLED | **1012** |
| `local-runtime/backend-20260717-144426.stdout.log` | LOCAL | DISABLED | **1012** |
| `local-runtime/backend-20260717-163621.stdout.log` | LOCAL | DISABLED | **1012** |
| `propfirm_backend.log` 2026-07-17T16:38:33 | LOCAL | DISABLED | pump failed (same class) |

SDK mapping (`mt5_manager.cpp`): code **1012** → “IP blocked by MT5 server (`MT_RET_AUTH_MANAGER_IPBLOCK`). Ask MT5 server admin to whitelist this machine's IP.” That is exactly “egress is not `81.29.145.69`.”

**Config implication (not a product edit):** a C++ `mt5_group_probe` / YoPips backend that only sees `MT5_PROXY_ENABLED` will **repeat 1012** on this desktop even though the HTTP proxy row is filled in.

---

## 5. Prop `.env` vs C# vs C++ (this repo)

Path: `D:\Prop\.env`  
SHA-256 `556ACAA9EFF6106D601E4BCC556811C149A5140477B974AF77A3F9B5D77396FF` / 3422 B / mtime 2026-08-18T08:26:34Z.  
Gitignored. **Not** committed. Values below are non-secret identifiers or redacted lengths.

| Key | Presence | Value published here |
|---|---|---|
| `MT5_MODE` | PRESENT | `local` |
| `MT5_SERVER` | PRESENT | `57.128.141.65` |
| `ACHIEVER_EGRESS_IP` | PRESENT | `81.29.145.69` |
| `ACHIEVER_PROXY_ENABLED` | PRESENT | `true` |
| `ACHIEVER_PROXY_HOST` | PRESENT | `81.29.145.69` |
| `ACHIEVER_PROXY_PORT` | PRESENT | `49527` |
| `ACHIEVER_PROXY_USERNAME` | PRESENT | **REDACTED** (length 15) |
| `ACHIEVER_PROXY_PASSWORD` | PRESENT | **REDACTED** (length 15) |
| `IS_MT5_PROXY_ENABLED` | **KEY_ABSENT** | C++ `AppConfig` will treat proxy as **off** |
| `MT5_PROXY_*` | **KEY_ABSENT** | C++ probe keys not populated from this file |
| `MT5_STARWAVEFX_PROXY_ENABLED` | PRESENT | `false` |

C# live registration **does** read the Achiever §56 names and hard-codes HTTP type:

```27:32:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
            ProxyEnabled = bool.TryParse(config["ACHIEVER_PROXY_ENABLED"], out var pe) && pe,
            ProxyHost = config["ACHIEVER_PROXY_HOST"],
            ProxyPort = int.TryParse(config["ACHIEVER_PROXY_PORT"], out var pp) ? pp : 0,
            ProxyUser = config["ACHIEVER_PROXY_USERNAME"],
            ProxyPassword = config["ACHIEVER_PROXY_PASSWORD"],
```

```83:93:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            if (_opt.ProxyEnabled && !string.IsNullOrWhiteSpace(_opt.ProxyHost))
            {
                var proxy = new MTProxyInfo
                {
                    enable = 1,
                    type = MTProxyInfo.Type.PROXY_HTTP,
                    address = $"{_opt.ProxyHost}:{_opt.ProxyPort}",
                    auth = string.IsNullOrEmpty(_opt.ProxyUser) ? "" : $"{_opt.ProxyUser}:{_opt.ProxyPassword}"
                };
                _manager.ProxySet(proxy);
            }
```

`bool.TryParse("true")` succeeds. So **if** the worker loads `D:\Prop\.env` into `IConfiguration` and `HasRealPasswords` passes, the native C# connector **will** `ProxySet` HTTP before `Connect`. That is the correct direction for this host.

This pass **did not** run that Connect. C42's “fake only” inventory is **stale relative to current worktree** (`NativeMt5BrokerConnector` + `LiveMt5Registration` exist). Stale inventory is not a live-session PASS.

C++ `mt5-sdk` probes still want `IS_MT5_PROXY_ENABLED` + `MT5_PROXY_TYPE/ADDRESS/PORT`. Copying only `ACHIEVER_PROXY_*` into a C++ cwd `.env` is **not** enough.

Proxy password lengths differ (YoPips 18 vs Prop 15). Values were **not** compared and are **not** printed. Treat them as independent secret slots.

---

## 6. Decision table

| Situation | Need HTTP `ProxySet`? |
|---|---|
| Achiever `MT5_MODE=local` on `DESKTOP-FQPFPKE` (egress `106.219.132.213`) | **YES** — else 1012 |
| Achiever local on a Windows worker whose default SNAT **is** `81.29.145.69` | **NO** (A54 preference 1) |
| Achiever local through Linux compose NAT | **Still 1012** unless that NAT is the allow-list or you proxy (A54: do not SNAT Achiever through Linux) |
| StarwaveFX local on this host | **NO** (`PROXY_ENABLED=false`; no documented whitelist) |
| YoPips `local-runtime` `MT5_MODE=remote` to `:65535` | **N/A** — not a live local Manager connect |
| C++ probe with only `MT5_PROXY_ENABLED=true` (wrong key) | Proxy **not applied** → behaves as “need proxy but don't use it” → **1012** on this host |
| C# `NativeMt5BrokerConnector` with `ACHIEVER_PROXY_ENABLED=true` and loaded secrets | **Should apply** HTTP proxy; live success **not measured** this pass |

---

## 7. What this agent did **not** do

- Did not print or copy `ACHIEVER_PROXY_PASSWORD`, `MT5_PROXY_PASSWORD`, `MT5_PASSWORD`, proxy username, or any YoPips JWT / PSP / KYC / DB secret (those exist in the YoPips `.env`; they are out of scope and unpublished here).
- Did not send HTTP CONNECT or Manager login (would be a live attach, not required to answer “need proxy?”).
- Did not edit `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `D:\Prop\mt5-sdk`, either `.env`, or YoPips source.
- Did not whitelist `106.219.132.213` at the broker (not ours to change from this report).
- Did not prove C# `Connect` through the proxy. TCP OPEN on `:49527` ≠ authenticated tunnel ≠ Manager OK.

---

## 8. Operator pin

1. **Need:** Achiever local connect from this lab PC **needs** the HTTP proxy at `81.29.145.69:49527` (or move the worker so egress **is** `81.29.145.69`).
2. **Why:** allow-list is `81.29.145.69`; this PC is `106.219.132.213`; historical direct local Connect = **1012**.
3. **C# env names:** `ACHIEVER_PROXY_ENABLED=true` + host/port + secret user/pass (architecture §56).
4. **C++ env names:** `IS_MT5_PROXY_ENABLED=true` + `MT5_PROXY_TYPE=HTTP` + `MT5_PROXY_ADDRESS` / `PORT` / login / password. **`MT5_PROXY_ENABLED` is the wrong key.**
5. **Never log** proxy user/password (§7, A76). Host/port/egress IP are non-secret (C55).
6. StarwaveFX stays direct unless a future whitelist appears.

---

## 9. Done criteria

- [x] Architecture egress `81.29.145.69` vs measured desktop egress compared.
- [x] YoPips `.env` HTTP proxy stanza documented **without** password (or username).
- [x] Local-connect 1012 evidence cited from YoPips logs (proxy disabled).
- [x] TCP reachability of Manager and proxy ports measured (no auth).
- [x] C# vs C++ toggle-key split recorded.
- [x] Product source not modified.
