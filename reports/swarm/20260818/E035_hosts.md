# E035 — Achiever server IP `57.128.141.65` in seed is non-secret

| Field | Value |
|---|---|
| Agent | E035 (senior engineer, seed-host classification only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (SHA + line census of current worktree `DemoSeeder`) |
| Assigned | Non-secret Achiever server IP in seed. Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E035_hosts.md` |
| Product source modified | **No.** This report is the only write. |
| HEAD at measure | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`) |
| Binding law | Architecture §§7, 56, 73.B; A19 (live identifier FLAG vs secret); A58 (`MT5_SERVER` not secret); A76 (log Keep); C42 (seed IPs are catalog paint); C55 (sibling: egress IP class) |
| Literal under classification | `57.128.141.65` |
| Seed SUT | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` |
| Seed bytes / lines / SHA-256 | **5082** / **140** / `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` |
| Seed git | untracked (`??`); not in HEAD `398a142` |
| LastWriteTimeUtc | 2026-08-18T08:04:59.2131544Z |

Classification vocabulary is architecture §73.B. The **value class** of this IPv4 is **`NON-SECRET`**. That is the standing order for this report.

---

## 0. Verdict

**`57.128.141.65` in `DemoSeeder` is not a secret.**

It is the documented Achiever Manager **host** (`MT5_SERVER`). Architecture §7 prints it in the open under the heading **“Non-secret configuration currently includes.”** Architecture §56 repeats the same digits inside the **Secret-Safe** example (the block that uses `<SECRET>` only for passwords). A58 marks `MT5_SERVER` **not secret** and **required to connect**. A76 says **Keep** it in logs.

The seed writes the same string onto `Broker.Server`. That is catalog paint. It is **not** a Manager TCP attach, not a password, and not a reason to redact the report.

| Claim | Ruling |
|---|---|
| Is `57.128.141.65` a password / token / API key / connection-string secret? | **No.** |
| Does architecture classify `MT5_SERVER` as non-secret? | **Yes.** §7 heading + §56 secret-safe block. |
| May reports, architecture, and operator docs print the literal? | **Yes.** |
| May logs / health / dashboard `server` mention it? | **Yes.** Do not redact it as if it were `MT5_PASSWORD`. |
| Must Connect send it as the Manager endpoint host? | **Yes, when a live connector exists.** Today the Fake never reads the field. |
| Does printing it leak Achiever Manager password, proxy user, or proxy password? | **No.** Those remain `<SECRET>`. `Broker` has **no** password column. |
| Does “non-secret” mean “we are connected to that host”? | **No.** C42: seeded IPs are catalog paint. A100 G01 remains **FAIL**. |
| Does “non-secret” mean “advertise on a public marketing site”? | **No.** Need-to-know still applies to live venue identity (A19 FLAG). It does **not** reclassify the IP as a credential. |
| Is the only product-C# occurrence of the IPv4 the seed? | **Yes.** `src/` + `apps/` grep: **one** hit, `DemoSeeder.cs:35`. |

Honest one-liner: **`DemoSeeder` `Broker.Server = "57.128.141.65"` is the same non-secret `MT5_SERVER` identifier as architecture §7. Treat it like C55’s egress IP, not like `MT5_PASSWORD`.**

---

## 1. Binding quotes

Architecture §7 (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md:361–377`):

> Non-secret configuration currently includes:
>
> ```env
> MT5_SERVER=57.128.141.65
> MT5_PORT=443
> MT5_LOGIN=2027
> MT5_DEFAULT_GROUP=demo\Maxmaster
> MT5_MODE=local
> MT5_POOL_SIZE=8
> MT5_SERVER_NAME=AchieverGlobalMarkets-Server
> ```
>
> Secret:
>
> ```env
> MT5_PASSWORD=<SECRET>
> ```

The host is listed **inside** the non-secret block. The adjacent secret is **only** `MT5_PASSWORD`.

Architecture §56 (Secret-Safe Example Configuration, same file `:2038–2041`):

```env
MT5_SERVER=57.128.141.65
MT5_PORT=443
MT5_LOGIN=2027
MT5_PASSWORD=<SECRET>
```

Secret-safe here means: **the example may contain this IP**. The same stanza still placeholders the only Achiever Manager secret (`MT5_PASSWORD`).

A58 §4.1 (`D:\Prop\reports\swarm\20260818\A58_broker_registry.md`):

| §56 key | Secret? | Required to connect? | Maps to |
|---|---|---|---|
| `MT5_SERVER` | **no** | **yes** | `EndpointHost` (`57.128.141.65`) |
| `MT5_PORT` | **no** | **yes** (default 443) | `EndpointPort` |
| `MT5_LOGIN` | **no** | **yes** | `ManagerLogin` (`2027`) |
| `MT5_PASSWORD` | **yes** | **yes** | `Password` |

A76 (`D:\Prop\reports\swarm\20260818\A76_log_redaction.md`):

| Key | Class | Log rule |
|---|---|---|
| `MT5_SERVER` / `MT5_PORT` / `MT5_SERVER_NAME` | **Non-secret** | **Keep** |
| `Password` / `ProxyPassword` / `ProxyLogin` / `ApiKey` | **SECRET** | **Never log** |

A19 §4.1 lists `57.128.141.65` under **“Live identifiers present (non-secret)”**, same table as manager login `2027` and egress `81.29.145.69`. That FLAG is “this is a live venue identity in the architecture,” **not** “this is a committed password.”

C55 already used this host as the **non-secret peer** of the egress IP:

> Treat [`81.29.145.69`] like `MT5_SERVER=57.128.141.65`, not like `MT5_PASSWORD`.

E035 is the inverse pin: the **seed** copy of that same `MT5_SERVER` value is the same class.

---

## 2. Where the seed writes it (measured)

File: `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`  
SHA-256 `A6416491…` (matches E008 / D94 current seeder; **not** D22’s stale `139D8F87…` / 4942 bytes / 138 lines).

```29:59:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
        db.Brokers.AddRange(
            new Broker
            {
                Id = achieverId,
                Code = BrokerCodes.Achiever,
                DisplayName = "Achiever",
                Server = "57.128.141.65",
                Port = 443,
                ManagerLogin = 2027,
                ServerName = "AchieverGlobalMarkets-Server",
                Mode = "local",
                PoolSize = 8,
                Enabled = true,
                CreatedAt = now,
                UpdatedAt = now
            },
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

| Seed field | Value | Class | Secret? |
|---|---|---|---|
| `Broker.Id` | `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1` | Demo catalog UUID | No |
| `Broker.Code` | `ACHIEVER` (`BrokerCodes.Achiever`) | Registry key | No |
| `Broker.DisplayName` | `Achiever` | Label | No |
| **`Broker.Server`** | **`57.128.141.65`** | **`MT5_SERVER` / live Manager host** | **No** |
| `Broker.Port` | `443` | `MT5_PORT` | No |
| `Broker.ManagerLogin` | `2027` | `MT5_LOGIN` (A19 FLAG) | No |
| `Broker.ServerName` | `AchieverGlobalMarkets-Server` | `MT5_SERVER_NAME` | No |
| `Broker.Mode` | `local` | `MT5_MODE` | No |
| `Broker.PoolSize` | `8` | `MT5_POOL_SIZE` | No |
| `Broker.Enabled` | `true` | Catalog flag | No |
| Manager password | **absent** | `Broker` has no password property (`D:\Prop\src\Domain\Entities\Broker.cs`) | n/a |

Assigned scope is the **Achiever server IP**. StarwaveFX `84.201.6.142` is the same identifier class (architecture §8 / A58) and is listed only so the seed pair is not misread. It is not a second secret.

---

## 3. What the IP is (and is not)

### 3.1 What it is

| Fact | Detail |
|---|---|
| Role | Achiever Manager **endpoint host**. Worker (when live) opens TCP **to** this address on port **443**. |
| Env key | `MT5_SERVER` |
| Architecture name | Non-secret configuration (§7) |
| Seed column | `brokers.server` via `Broker.Server` |
| C# options field | `Mt5BrokerOptions.Server` (`D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs` L18) — property exists; **not** bound in worker appsettings; **not** hardcoded to this IPv4 |
| A58 map | `EndpointHost` — **required to connect** when a real connector exists |
| Sibling (not this IP) | Egress / whitelist `81.29.145.69` is the **source** the broker must see (C55). Different address, same **NON-SECRET** class. |
| Failure mode if wrong | Connect never reaches Achiever (timeout / refuse / wrong venue). Distinct from 1012 IP-block (that is egress). |

### 3.2 What it is not

- Not `MT5_PASSWORD`.
- Not `ACHIEVER_PROXY_USERNAME` / `ACHIEVER_PROXY_PASSWORD`.
- Not a Manager login (`2027`).
- Not the egress / proxy host (`81.29.145.69`).
- Not a StarwaveFX host (`84.201.6.142`).
- Not a FIX host (`live-us-eqx-01.p.c-trader.com`).
- Not proof of a live Manager session. `FakeMt5BrokerConnector` / `DemoBrokerFactory.CreateDefault()` never accept or read `Server` (C42 §7, D24).
- Not malware, not a license, not a crack. It is a broker Manager listener address.

### 3.3 Two Achiever IPv4s (do not collapse)

| Literal | Key | Direction | Seeded? |
|---|---|---|---|
| **`57.128.141.65`** | `MT5_SERVER` | **Destination** of Manager TCP | **Yes** — `DemoSeeder` L35 |
| `81.29.145.69` | `ACHIEVER_EGRESS_IP` / optional `ACHIEVER_PROXY_HOST` | **Source** the broker allow-lists | **No** — not on `Broker`; C55 only |

Both are **NON-SECRET**. Confusing them is an ops error (1012 vs “host unreachable”), not a secrets error.

---

## 4. Measured locations of the literal (this tree)

Read-only grep of `57.128.141.65` on 2026-08-18. **No product file was edited.**

| Surface | Path | How it appears | Secret? |
|---|---|---|---|
| **Seed (assigned)** | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs:35` | `Server = "57.128.141.65"` | **No** |
| Architecture §7 | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md:364` | `MT5_SERVER=57.128.141.65` under “Non-secret configuration” | **No** |
| Architecture §56 | same file `:2038` | Secret-safe example | **No** |
| Deployment notes | `D:\Prop\docs\deployment.md:29` | `set MT5_SERVER=57.128.141.65` | **No** |
| Product C# except seed | `src/` (other files), `apps/` | **zero** hits | n/a |
| `FakeMt5BrokerConnector` | `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | **no** IPv4; constructor takes broker code + in-memory groups/accounts/deals | n/a |
| `Mt5BrokerOptions.Server` | `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs` | property only — **no** hardcoded IPv4 | n/a |
| `apps/mt5-worker` appsettings | `appsettings*.json` | **no** `MT5_*` / `Server` bind | n/a |
| `Broker` entity | `D:\Prop\src\Domain\Entities\Broker.cs` | `string Server` — no default IPv4 | n/a |
| C++ `mt5-sdk` product config | `mt5-sdk/src` | **no** this IPv4 in the seed path | n/a |
| Swarm reports | A04, A07, A19, A26, A54, A58, A76, C42, C55, D22, D40, … | operational docs | **No** |

A19 §4.1 (early wave) said these identifiers “do **not** appear in product C#.” That sentence is **stale**. The worktree seeder **does** hardcode the Achiever host. Class stays **NON-SECRET**. Presence in C# is an A19 FLAG / need-to-know fact, **not** a password leak.

---

## 5. Catalog paint ≠ connection

C42 (`D:\Prop\reports\swarm\20260818\C42_honesty_no_live_mt5.md`) already measured:

> Seeded IPs mean we are on those hosts — **False.** `DemoSeeder` writes `57.128.141.65` / `84.201.6.142` onto `Broker` rows. `FakeMt5BrokerConnector` never reads `Server`, `Port`, `ManagerLogin`, or any password.

After the broker `AddRange`, the seeder still:

1. `SaveChangesAsync`
2. `DemoBrokerFactory.CreateDefault()` — in-memory Achiever/Starwave tapes
3. `DealIngestionService.SyncBrokerAsync(BrokerCodes.Achiever, …)`

No DNS, no SYN to `:443`, no `MT5APIManager64.dll`, no `Mt5BrokerOptions` bind (C07 / D31). Dashboard “connected” is the Fake’s `_connected = true` (C42), not a socket to this IP.

**Do not** tick A100 G01 from this file. **Do not** treat a green InMemory seed test as Achiever connected (C16 / D37).

---

## 6. How operators / later agents should treat it

| Action | Required |
|---|---|
| Quote `57.128.141.65` in reports, architecture, operator sheets | **Yes** — it is the documented host |
| Redact it from logs as `***` | **No** (A76 Keep) |
| Replace it with `<SECRET>` in `.env.example` / docs | **No.** A75 may prefer `<MT5_MANAGER_HOST>` as a *publish-placeholder*; that is **not** a secrets decision (same resolution as C55 §5) |
| Commit `MT5_PASSWORD` next to it | **Never** |
| Persist it on `Broker.Server` in a demo seed | Allowed as identifier; does **not** authorize a live Connect |
| Pass it into `FakeMt5BrokerConnector` | **No** — Fake has no host argument |
| Hit it from unit tests | **No** (A18: do not probe live Achiever) |
| Assume Linux compose egress + this dest = connected | **No** — live Manager is Windows/`MT5_MODE=local` (A54 / A105); still unproven (C42) |

A75 / A19-02 = **need-to-know for a public dump**.  
E035 / §7 / §56 / A58 / A76 / C55 = **class = non-secret**.

If a later agent rewrites the seed to `test.invalid` (A90 §1.5 preference), that is a **demo-vs-live-identity** choice, not a “we leaked a password” fix.

---

## 7. Classification table (pin)

| Item | Class | Redact? | May appear in seed / docs / reports? |
|---|---|---|---|
| **`57.128.141.65`** (`DemoSeeder` `Broker.Server` / `MT5_SERVER`) | **NON-SECRET** operational identifier | No | **Yes** |
| `MT5_PORT=443` | **NON-SECRET** | No | Yes |
| `MT5_LOGIN=2027` | **NON-SECRET** identifier (A19 FLAG) | No | Yes |
| `MT5_SERVER_NAME=AchieverGlobalMarkets-Server` | **NON-SECRET** | No | Yes |
| `81.29.145.69` (`ACHIEVER_EGRESS_IP`) | **NON-SECRET** (C55) | No | Yes |
| `84.201.6.142` (StarwaveFX seed `Server`) | **NON-SECRET** sibling identifier | No | Yes |
| `MT5_PASSWORD` | **SECRET** | Always | Placeholder only |
| `ACHIEVER_PROXY_USERNAME` / `ACHIEVER_PROXY_PASSWORD` | **SECRET** | Always | Placeholder only |

---

## 8. Product-source status (unchanged)

| Check | Result |
|---|---|
| This agent edited `D:\Prop\src` | **No** |
| This agent edited `D:\Prop\apps` / `mt5-sdk` / `.env` / `.env.example` | **No** |
| Seed still contains `Server = "57.128.141.65"` | **Yes** (L35, current SHA `A6416491…`) |
| Product C# hardcode of this IPv4 outside the seeder | **Absent** |
| Live Achiever Manager proven | **No** (C42) |
| Classification change required in C# | None for this task |

Worktree has unrelated dirty / untracked product files (seeder itself is `??` vs HEAD). E035 did not touch them.

---

## 9. Done criteria

- [x] Standing classification recorded: Achiever seed server **`57.128.141.65` is non-secret**.
- [x] Bound to architecture §7 / §56, A58, A76, A19 FLAG, C55 sibling class.
- [x] Distinguished from `MT5_PASSWORD` and from egress `81.29.145.69`.
- [x] Measured the seed write (L35) and confirmed it is the only product-C# occurrence.
- [x] Honest: seed IP is catalog paint; live Manager remains unproven.
- [x] Product source not modified.
