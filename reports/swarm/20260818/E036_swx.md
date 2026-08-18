# E036 — StarwaveFX server IP `84.201.6.142` is non-secret

| Field | Value |
|---|---|
| Agent | E036 (classification document only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:51:48+05:30 (2026-08-18T08:21:48Z) |
| Assigned | Starwave server IP non-secret. Write `E036_swx.md`. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E036_swx.md` |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Config / `.env` / `appsettings` edited | **No.** |
| HEAD at measure | `398a14200ec65714c4077eed55c46808382ca1e3` |
| Binding law | Architecture §§8, 55, 56; A19 (identifier FLAG vs secret); A26 (dashboard may show server host); A58 (registry map); A76 (log Keep); C42 (seeded IP is catalog paint); C55 (sibling class for Achiever egress) |
| Literal under classification | `84.201.6.142` |
| Env key | `MT5_STARWAVEFX_SERVER` |
| Adjacent secret (not this report) | `MT5_STARWAVEFX_PASSWORD` — still `<SECRET>` / process-absent |

Classification vocabulary is architecture §73.B. The **value class** of this IPv4 is **`NON-SECRET`**. That is the standing order for this report.

---

## 0. Verdict (binding — do not greenwash)

**`84.201.6.142` is not a secret.**

It is the documented StarwaveFX Manager **endpoint host**. Architecture §8 prints it in the open under the heading **“Non-secret configuration currently includes”** as `MT5_STARWAVEFX_SERVER=84.201.6.142`. Architecture §56 repeats the same assignment inside the **Secret-Safe** example (the block that uses `<SECRET>` only for passwords). A58 marks the key **Secret? no** and **Required to connect? yes**. A76 says **Keep** it in logs. A26 §3.2 lists “server host” as **safe to show** on the dashboard (manager login is what gets masked).

| Claim | Ruling |
|---|---|
| Is `84.201.6.142` a password / token / API key / connection-string secret? | **No.** |
| May reports, architecture, operator docs, and this file print the literal? | **Yes.** |
| May logs / health / `LastError` / `Broker.Server` mention it? | **Yes.** Do not redact it as if it were `MT5_STARWAVEFX_PASSWORD`. |
| May the React Brokers page render `b.server` unmasked? | **Yes** (A26). Masking applies to **manager login**, not the host. |
| Must Connect send the IPv4 as a secret argument? | **No.** It is the TCP destination. The secret on that socket is the Manager password. |
| Is it required for a live `Connect` to *succeed*? | **Operationally yes** (A58: required-to-connect host). **Today’s C# never dials it** (C42). |
| Does printing it leak `MT5_STARWAVEFX_PASSWORD`, Achiever proxy user, or FIX password? | **No.** Those remain `<SECRET>` / process-absent (E001). |
| Does “non-secret” mean “we are connected to StarwaveFX”? | **No.** Seeded IP is catalog paint. Live Manager session is **NOT PROVEN** (C42). |
| Does “non-secret” mean “advertise on a public marketing site”? | **No.** Need-to-know still applies to live venue identity (A19 FLAG / A75). It does **not** reclassify the IP as a credential. |
| Does StarwaveFX have an Achiever-style egress whitelist? | **No.** §8: “No IP whitelist is currently required.” `ExpectedEgressIp` stays null (A58). |

**Honest one-liner:** **`84.201.6.142` is an operational identifier. Treat it like `MT5_SERVER=57.128.141.65` and like C55’s `81.29.145.69`, not like `MT5_STARWAVEFX_PASSWORD`.**

Do **not** replace this value with `<SECRET>` in docs or logs. Do **not** treat a dashboard “connected” cell next to this IP as a live Manager proof.

---

## 1. Binding quotes

Architecture §8 (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`, SHA-256 `0B3C0EDC09081C25D097FF0E6AADC7A638562EBB8DB345DC325DC54EC904D37E`, 50966 B):

> Non-secret configuration currently includes:
>
> ```env
> MT5_STARWAVEFX_DISPLAY_NAME=StarwaveFX
> MT5_STARWAVEFX_PROVISIONING_ENABLED=true
> MT5_STARWAVEFX_MODE=local
> MT5_STARWAVEFX_SERVER=84.201.6.142
> MT5_STARWAVEFX_PORT=443
> MT5_STARWAVEFX_LOGIN=9904
> MT5_STARWAVEFX_SERVER_NAME=StarwaveFX
> MT5_STARWAVEFX_POOL_SIZE=4
> MT5_STARWAVEFX_PROXY_ENABLED=false
> ```
>
> Secret:
>
> ```env
> MT5_STARWAVEFX_PASSWORD=<SECRET>
> ```
>
> No IP whitelist is currently required.

The IPv4 is listed **inside** the non-secret block and **outside** the only Starwave secret slot. Port `443`, login `9904`, and server name `StarwaveFX` share the same **non-secret** class. The adjacent secret is the **password**, not the address.

Architecture §56 (Secret-Safe Example Configuration, same file `:2060–2069`) repeats the identical host assignment next to `MT5_STARWAVEFX_PASSWORD=<SECRET>`. Secret-safe here means: **the example may contain this IP**. The same stanza still placeholders the only real Starwave secret.

A58 §4.2 (`D:\Prop\reports\swarm\20260818\A58_broker_registry.md`):

| §56 key | Secret? | Required to connect? | Maps to |
|---|---|---|---|
| `MT5_STARWAVEFX_SERVER` | **no** | **yes** | `EndpointHost` (`84.201.6.142`) |
| `MT5_STARWAVEFX_PORT` | **no** | **yes** | `EndpointPort` (`443`) |
| `MT5_STARWAVEFX_LOGIN` | **no** | **yes** | `ManagerLogin` (`9904`) |
| `MT5_STARWAVEFX_PASSWORD` | **yes** | **yes** | `Password` |

A76 §4.3 (`D:\Prop\reports\swarm\20260818\A76_log_redaction.md`):

| Key | Class | Log rule |
|---|---|---|
| **`MT5_STARWAVEFX_PASSWORD`** | **SECRET** | Always redact |
| `MT5_STARWAVEFX_LOGIN` | Identifier | Same as `MT5_LOGIN` (mask in list views) |
| `MT5_STARWAVEFX_SERVER` / `PORT` / `SERVER_NAME` / `MODE` / `POOL_SIZE` / `DISPLAY_NAME` / `PROVISIONING_ENABLED` / `PROXY_ENABLED` | **Non-secret** | **Keep** |

A19 §4.1 lists `84.201.6.142` under **“Live identifiers present (non-secret)”**, same table as Achiever `57.128.141.65` and manager login `9904`. That FLAG is “this is a live venue identity in the architecture,” **not** “this is a committed password.”

A26 §3.2: **“Safe to show: broker display name, server host, port … masked manager login.”** Server IP is an allow-listed dashboard field. Password is not.

C55 is the sibling pin for Achiever egress `81.29.145.69`. Same class family. Starwave has **no** egress twin.

---

## 2. What the IP is (and is not)

### 2.1 What it is

| Fact | Detail |
|---|---|
| Role | StarwaveFX Manager **TCP destination** (host of `MT5_STARWAVEFX_SERVER`). |
| Env key | `MT5_STARWAVEFX_SERVER` |
| Port (also non-secret) | `MT5_STARWAVEFX_PORT=443` |
| C# field (product) | `Mt5BrokerOptions.Server` — unbound; worker `appsettings` has no `Mt5` section |
| Domain column | `Broker.Server` (`D:\Prop\src\Domain\Entities\Broker.cs`) — **no password column** on the entity |
| Seed literal | `DemoSeeder` writes `Server = "84.201.6.142"` for `BrokerCodes.StarwaveFx` |
| Dashboard | `BrokerStatusDto.Server` is the raw host; `ManagerLoginMasked` is the only masked field |
| Topology (A54) | Same Windows `mt5-worker` as Achiever; `TCP 443 → 84.201.6.142`. No second OS. |
| Whitelist | **None today** (§8). Do not invent `MT5_STARWAVEFX_EGRESS_IP`. |
| Public-web note | StarwaveFX is a named MT5 venue (WikiFX lists `StarwaveFX-Server`). This report did **not** DNS/WHOIS/connect the IPv4. Architecture already classifies the digits. |

### 2.2 What it is not

- Not `MT5_STARWAVEFX_PASSWORD`.
- Not `MT5_PASSWORD` / `ACHIEVER_PROXY_USERNAME` / `ACHIEVER_PROXY_PASSWORD` / `CTRADER_FIX_PASSWORD`.
- Not a Manager login (`9904` is a separate **identifier**; mask in list views, still not a password).
- Not Achiever host `57.128.141.65` and not Achiever egress `81.29.145.69`.
- Not a reason to redact `Broker.Server` on `/api/brokers`.
- Not a live session. `FakeMt5BrokerConnector` never reads `Server` / `Port` / `ManagerLogin` / password (C42).
- Not malware, not a license, not a crack. It is a broker access-point address the lab already published in §8.

### 2.3 Required-to-connect ≠ currently-connected

A58’s “Required to connect? **yes**” is a **future binder** rule: a live Starwave slot is not configured until host + port + login + a **real** password are present. Today:

- password slot = placeholder `<SECRET>` (gitignored `D:\Prop\.env` L30);
- process env `MT5_STARWAVEFX_PASSWORD` / `MT5_STARWAVEFX_SERVER` = **absent** (this measure);
- C# connector = `FakeMt5BrokerConnector` only;
- `ConnectAsync` flips `_connected = true` with **no socket**.

So the IP can be **non-secret**, **required for a real Connect**, and **still unused**. Those three facts coexist. Printing the IPv4 does not open port 443.

---

## 3. Measured locations of the literal (this tree)

Read-only. **No product file was edited.** Product C# hit count for `84.201.6.142` under `D:\Prop\src` + `D:\Prop\apps` + `D:\Prop\tests` (exclude `bin` / `obj` / `node_modules`): **1**.

| Surface | Path | How it appears | Secret? |
|---|---|---|---|
| Architecture §8 | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md:421` | `MT5_STARWAVEFX_SERVER=84.201.6.142` under “Non-secret configuration” | **No** |
| Architecture §56 | same file `:2063` | same assignment in Secret-Safe example | **No** |
| Gitignored template | `D:\Prop\.env:27` (3408 B, SHA `56C81786…`, same as E001/D40/D61) | `MT5_STARWAVEFX_SERVER=84.201.6.142`; password line is `<SECRET>` | **No** (host) |
| `D:\Prop\.env.example` | **ABSENT** on disk (HEAD still tracks; porcelain ` D .env.example`) | — | n/a |
| Product C# | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs:50` | `Server = "84.201.6.142"` on `STARWAVEFX` row | **No** (catalog) |
| Options POCO | `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs` | `Server` property only — **no hardcoded IPv4** | n/a |
| Fake connector | `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | **zero** reads of `Server` | n/a |
| Dashboard query | `EfDashboardQueries.GetBrokersAsync` | emits `b.Server` raw; masks login only | **No** (by design) |
| Brokers UI | `apps/web/src/pages/BrokersPage.tsx` | `{b.server}` unmasked; `{b.managerLoginMasked}**` | **No** (A26) |
| Worker / API `appsettings*.json` | no match | — | n/a |
| C++ `AppConfig` | **no** `MT5_STARWAVEFX_*` fields (A04 / A58 MISSING) | — | n/a |
| Process / User / Machine env | this measure | `MT5_STARWAVEFX_SERVER` **absent**; `MT5_STARWAVEFX_PASSWORD` **absent** | n/a |
| Swarm reports | A04, A07, A18, A19, A54, A57, A58, A66, A76, A100, B04, B07, B25, B40, C13–C16, C42, C45, C46, C54, D03, D07, D22, D24, D31, D37, D40, D61, D64, D88, E001 | operational / honesty docs | **No** |

### 3.1 File hashes this verdict stands on

| Path | Bytes | SHA-256 |
|---|---:|---|
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | 5082 | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` |
| `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs` | 1609 | `64A840278433587B55805042873545D0535C64E7E50DDDD9BF8FDC72E635FAB7` |
| `D:\Prop\src\Domain\Brokers\BrokerCodes.cs` | 180 | `CF4165CE7A317B0282B9149B078E5D1E630F72524190AB20E0952BECBBAE1182` |
| `D:\Prop\src\Domain\Entities\Broker.cs` | 778 | `412FF86681DF6189C3673762C38B22622A471C1578B5555E85827AAE02DEF19D` |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | 7049 | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | 8708 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` |
| Architecture v2 | 50966 | `0B3C0EDC09081C25D097FF0E6AADC7A638562EBB8DB345DC325DC54EC904D37E` |
| `D:\Prop\.env` (gitignored; values classified, not dumped beyond the non-secret host line) | 3408 | `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA` |

Seeder SHA matches E008 (not the older C42 `139D8F87…` snapshot). The Starwave `Server = "84.201.6.142"` literal is unchanged. Worktree: `?? src/Infrastructure/Seeding/DemoSeeder.cs`. This agent did not stage or edit it.

### 3.2 Seed vs transport (do not confuse)

```45:58:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
            new Broker
            {
                Id = starwaveId,
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
```

C42: these fields are written to `brokers` and later shown on the dashboard. They are **not** constructor inputs to `FakeMt5BrokerConnector`. A dashboard emerald “connected” next to `84.201.6.142` is a **literal `true`** in `GetBrokersAsync`, not a Manager `Connect`.

---

## 4. How operators / later agents should treat it

| Action | Required |
|---|---|
| Print `84.201.6.142` in swarm reports, architecture cites, and operator sheets | **Yes** — it is the Starwave host |
| Put `MT5_STARWAVEFX_SERVER=84.201.6.142` in Windows-only env when wiring a live slot | Yes (A58 required-to-connect) |
| Redact it from logs as `***` | **No** (A76 Keep) |
| Redact `MT5_STARWAVEFX_PASSWORD` | **Yes** — always |
| Mask manager login `9904` on list views | **Yes** (A26 / A76). Host stays visible. |
| Commit a filled Starwave password next to the IP | **Never** |
| Treat seed / dashboard presence as G01 PASS | **Never** (C42; A100 G01 still FAIL) |
| Hit `84.201.6.142:443` from unit tests | **No** (A18) |
| Invent `MT5_STARWAVEFX_EGRESS_IP` / Starwave proxy env names | **No** (A58 rejected keys) |
| Replace the IPv4 with `<SECRET>` | **No** — that would lie about the secret class |
| Dial the host from this agent | **Not done.** Classification does not require a TCP probe. |

---

## 5. Tension with A75 (resolved, same as C55)

A75 tells `.env.example` authors to write `MT5_STARWAVEFX_SERVER=<MT5_MANAGER_HOST>` and says:

> Do **not** treat “non-secret” as “safe to publish our live account.”

E036 does **not** overturn A75’s *publish-placeholder* preference for a **committed public example**. E036 **does** overturn any reading that:

- the IPv4 is a credential,
- the IPv4 must be redacted in logs or on `/api/brokers`,
- writing `84.201.6.142` in this swarm tree is a secret leak,
- later agents should refuse to quote the address,
- `DemoSeeder` hardcoding the host is a password commit (it is identifier paint; `Broker` has no password column).

A19-02 / A75 = **need-to-know for a public dump**.  
E036 / §8 / §56 / A58 / A76 / A26 / C55 = **class = non-secret**.

If a later agent restores `D:\Prop\.env.example`, it may keep the literal (matches §56 and today’s gitignored `.env`) or use `<MT5_MANAGER_HOST>` (matches A75). Either choice is **not** a secrets decision. **Do not** replace the value with `<SECRET>`.

D61 already measured the ignored `.env` as **not** placeholder-only for identifiers. That FAIL is an A75 content-law FAIL, **not** a live-password leak. Password slot on L30 remains `PLACEHOLDER_SECRET` (E001).

---

## 6. Honesty pin — non-secret ≠ live Starwave

| Gate / claim | This pass |
|---|---|
| Value class of `84.201.6.142` | **NON-SECRET** |
| Live StarwaveFX Manager / HTTP session | **NOT PROVEN** (not attempted; C42 still binds) |
| Process `MT5_STARWAVEFX_PASSWORD` | **absent** |
| Process `MT5_STARWAVEFX_SERVER` | **absent** (file has the key; this process did not load `.env`) |
| A100 G01 | **Still FAIL** |
| Safe to treat dashboard `connected` as venue truth | **No** |

Siblings that remain the honesty pins: `C42_honesty_no_live_mt5.md`, `E001_no_secrets.md`. This file classifies **one IPv4**. It does not reopen those reviews.

---

## 7. Product-source status (unchanged)

| Check | Result |
|---|---|
| This agent edited `D:\Prop\src` | **No** |
| This agent edited `D:\Prop\apps` / `tests` / `mt5-sdk` / `.env` | **No** |
| Hardcoded `84.201.6.142` under product C# | **Only** `DemoSeeder.cs:50` |
| `Mt5BrokerOptions.Server` | Exists; **not** bound by any host |
| Classification change required in C# | **None** for this task. Wiring a live Starwave connector is A58 / Phase 1 work, out of scope. |
| Secret values printed | **None.** Host / port / login identifiers only. |

---

## 8. Classification table (pin)

| Item | Class | Redact? | May appear in committed docs / reports / dashboard `Server`? |
|---|---|---|---|
| `84.201.6.142` | **NON-SECRET** operational identifier | No | **Yes** |
| `MT5_STARWAVEFX_SERVER` | **NON-SECRET** env key | No | **Yes** |
| `MT5_STARWAVEFX_PORT=443` | **NON-SECRET** | No | **Yes** |
| `MT5_STARWAVEFX_SERVER_NAME=StarwaveFX` | **NON-SECRET** | No | **Yes** |
| `MT5_STARWAVEFX_LOGIN=9904` | Identifier (not a password) | Mask in **list** views | Yes as masked / structured |
| `MT5_STARWAVEFX_PASSWORD` | **SECRET** | Always | Placeholder only |
| `MT5_SERVER=57.128.141.65` | **NON-SECRET** (same family) | No | Yes |
| `ACHIEVER_EGRESS_IP=81.29.145.69` | **NON-SECRET** (C55) | No | Yes — **Achiever only** |

---

## 9. What was not done

- Did not modify product source, tests, `.env`, or architecture markdown.
- Did not print, copy, or persist any password value.
- Did not `Connect` / TCP / TLS to `84.201.6.142:443`.
- Did not load `.env` into the process.
- Did not enable real copy.
- Did not restore `D:\Prop\.env.example`.
- Did not rewrite `DemoSeeder` to placeholders (A75 is not this assignment).

---

## 10. Done criteria

- [x] Standing classification recorded: StarwaveFX server **`84.201.6.142` is non-secret**.
- [x] Distinguished from `MT5_STARWAVEFX_PASSWORD` and from Achiever egress `81.29.145.69`.
- [x] Distinguished from A75 “don’t publish live account identity.”
- [x] Measured where the literal exists; only product C# hit is `DemoSeeder.cs:50`.
- [x] Live session still **NOT PROVEN**.
- [x] Product source not modified.
