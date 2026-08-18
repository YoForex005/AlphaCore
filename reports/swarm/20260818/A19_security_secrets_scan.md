# A19 — Security / secrets scan

**Agent:** A19 (senior engineer, read-only)  
**Date:** 2026-08-18  
**Scope:** `D:\Prop` (product, architecture, `mt5-sdk`, vendor examples, `appsettings*.json`, env templates, logs).  
**Compared to:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§ 7–8, 25, 52, **55–56**, 57.  
**Product source modified:** no.  
**Secrets policy for this report:** no live password, token, key, or connection-string value is copied. Placeholders are shown as they appear. Live **non-secret** hosts / logins / account IDs from the architecture are listed so they can be flagged.

---

## 1. Verdict

| Question | Result |
|---|---|
| Live `MT5_PASSWORD` / FIX password / DB / Redis / proxy password in `D:\Prop`? | **NONE FOUND** |
| Live connection string with credentials? | **NONE FOUND** |
| `appsettings*.json` contain secrets or `ConnectionStrings`? | **No** (logging only) |
| Architecture §§55–56 use secret placeholders? | **Yes** (`<SECRET>`, `<BROKER_ISSUED_VALUE>`) |
| Architecture contains **live host / login / account** (non-secret)? | **YES — FLAG** |
| Real `.env` / `secrets.json` / key material on disk under `D:\Prop`? | **NONE FOUND** |

**Overall:** tree is **secret-clean**. It is **not identifier-clean**. The architecture doc is the only place that publishes live venue targeting data (IPs, manager logins, cTrader account, SenderCompID, egress/proxy). That is not a password leak; it is production targeting data in a markdown file at repo root.

No P0 (committed live secret) finding.

---

## 2. Method

Searched `D:\Prop` for:

- `MT5_PASSWORD`, `MT5_STARWAVEFX_PASSWORD`, `CTRADER_FIX_PASSWORD`, `FIX_PASSWORD`, `PASSWORD=`
- `ConnectionString` / `connectionString` / `Server=` / `User ID=` / URI schemes (`postgres://`, `redis://`, …)
- `password` / `secret` / `api_key` / `token` assigned to quoted literals
- Hidden `.env*`, `secrets.json`, `appsettings.{Production,Staging,Local}.json`, `*.pfx` / `*.pem` / `*.key`
- Private-key PEM blocks, AWS-style key IDs
- Live identifiers from architecture (`57.128…`, `84.201…`, `1369850`, `live-us-eqx`, `81.29…`, `pepperstone`)
- `build-release.log` / `test-release.log` for password assignments

Also read every `appsettings*.json` (source + mt5-worker Release output), all three `launchSettings.json`, `mt5-sdk/.env.example`, `mt5-sdk/config/app_config.*`, product `Program.cs` / `Worker.cs`, and architecture §§7–8, 25, 52, 55–57, 59.

Filesystem probe (`Get-ChildItem -Force -Recurse` for env/secrets/key files): only `D:\Prop\mt5-sdk\.env.example`.  
`D:\Prop\mt5-sdk\.env` does not exist. `D:\Prop\.env` does not exist.  
`D:\Prop` has **no** `.git`. `D:\Prop\mt5-sdk` **is** a git repo; tracked env file is **only** `.env.example`.

---

## 3. Architecture §§55–56 (required read)

### 3.1 §55 Security (doc lines 2002–2027)

Requires never exposing to React:

```text
MT5 passwords
proxy credentials
cTrader account password
FIX password
database passwords
Redis passwords
```

Requires storage via environment variables / OS secret store / Vault.  
**Production secrets must not be committed to Git.**  
Create **only placeholders** in `.env.example`.

§52 separately: “Never show FIX password.”  
§57: “Never log authentication tags containing passwords.”

### 3.2 §56 Secret-Safe Example Configuration (doc lines 2031–2104)

Example block is **placeholder-correct for secrets**:

| Key | Value in doc | Class |
|---|---|---|
| `MT5_PASSWORD` | `<SECRET>` | secret placeholder |
| `ACHIEVER_PROXY_USERNAME` | `<SECRET>` | secret placeholder |
| `ACHIEVER_PROXY_PASSWORD` | `<SECRET>` | secret placeholder |
| `MT5_STARWAVEFX_PASSWORD` | `<SECRET>` | secret placeholder |
| `CTRADER_FIX_PASSWORD` | `<SECRET>` | secret placeholder |
| `CTRADER_FIX_QUOTE_SENDER_SUB_ID` / `TARGET_SUB_ID` | `<BROKER_ISSUED_VALUE>` | secret-ish / broker-issued |
| `CTRADER_FIX_TRADE_SENDER_SUB_ID` / `TARGET_SUB_ID` | `<BROKER_ISSUED_VALUE>` | secret-ish / broker-issued |

No live password, proxy user, or FIX password appears in §55 or §56.

---

## 4. FLAG — live host / login (non-secret) vs secrets

The architecture **does** treat hosts and numeric logins as non-secret configuration (§7 “Non-secret configuration currently includes”, §8 same). Those live values are **repeated in §56**, which is titled “Secret-Safe Example Configuration.” Secret-safe ≠ identifier-safe.

### 4.1 Live identifiers present (non-secret)

Repeated in §§7, 8, 25, and **56**:

| Kind | Keys / location | Live value in doc |
|---|---|---|
| Achiever MT5 host | `MT5_SERVER` | `57.128.141.65` |
| Achiever manager login | `MT5_LOGIN` | `2027` |
| Achiever server name | `MT5_SERVER_NAME` | `AchieverGlobalMarkets-Server` |
| Achiever default group | `MT5_DEFAULT_GROUP` | `demo\Maxmaster` |
| Achiever egress / proxy host | `ACHIEVER_EGRESS_IP`, `ACHIEVER_PROXY_HOST` | `81.29.145.69` |
| Achiever proxy port | `ACHIEVER_PROXY_PORT` | `49527` |
| StarwaveFX host | `MT5_STARWAVEFX_SERVER` | `84.201.6.142` |
| StarwaveFX manager login | `MT5_STARWAVEFX_LOGIN` | `9904` |
| cTrader FIX host | `CTRADER_FIX_HOST` | `live-us-eqx-01.p.c-trader.com` |
| cTrader account | `CTRADER_FIX_ACCOUNT_ID` | `1369850` |
| FIX SenderCompID (QUOTE + TRADE) | `…_SENDER_COMP_ID` | `live.pepperstone.1369850` |
| FIX SSL / plain ports | QUOTE 5211/5201, TRADE 5212/5202 | live venue ports |

These values appear **only** in `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` (16 hits). They do **not** appear in product C# / `appsettings` / `mt5-sdk` config / `.env.example`.

### 4.2 Secrets in the same document

All secret slots are placeholders. Closest-to-leaky wording is §25:

```env
CTRADER_FIX_PASSWORD=<SECRET: account 1369850 password>
```

That is still a placeholder, but it **binds the secret slot to a live account ID**. Not a password leak.

### 4.3 Classification

| Item | Class |
|---|---|
| Architecture secret slots | `EXISTS_AND_GOOD` (placeholders only) |
| Architecture live host/login/account in §7–8, §25, §56 | `EXISTS_NEEDS_REFACTOR` if this markdown will be shared / git-published; identifiers are live production targeting |
| Product code copying those identifiers | `MISSING` (good — not hardcoded yet) |

**Risk if the markdown is copied to a public or wide-internal git remote:** attackers get manager login numbers, broker IPs, whitelist/proxy hop, and the live Pepperstone cTrader account + SenderCompID. They still need the passwords. That is reconnaissance, not a credential dump.

---

## 5. `appsettings*.json` (complete)

No `appsettings.Production.json` / `Staging` / `Local`. No `ConnectionStrings` section. No password keys.

| Path | Contents of interest |
|---|---|
| `D:\Prop\apps\api\appsettings.json` | `Logging` + `"AllowedHosts": "*"` |
| `D:\Prop\apps\api\appsettings.Development.json` | `Logging` only |
| `D:\Prop\apps\fix-worker\appsettings.json` | `Logging` only |
| `D:\Prop\apps\fix-worker\appsettings.Development.json` | `Logging` only |
| `D:\Prop\apps\mt5-worker\appsettings.json` | `Logging` only |
| `D:\Prop\apps\mt5-worker\appsettings.Development.json` | `Logging` only |
| `D:\Prop\apps\mt5-worker\bin\Release\net8.0\appsettings.json` | same as source |
| `D:\Prop\apps\mt5-worker\bin\Release\net8.0\appsettings.Development.json` | same as source |

`launchSettings.json` (api, fix-worker, mt5-worker): localhost URLs + `ASPNETCORE_ENVIRONMENT` / `DOTNET_ENVIRONMENT` = Development. No credentials.

`D:\Prop\apps\api\TraderIntelligence.Api.http`: `http://localhost:5160` weatherforecast only.

**`AllowedHosts: "*"`** on the API is the only appsettings security finding. It is not a secret. It is a host-filter disable (template default). Flag for production hardening, not a leak.

Worker csproj files have template `UserSecretsId` GUIDs (`FixWorker-400770db-…`, `Mt5Worker-6850a13e-…`). No `secrets.json` exists under `D:\Prop`. Those IDs are not secrets.

---

## 6. Targeted search results

### 6.1 `MT5_PASSWORD` / FIX password assignments

| Location | Value class |
|---|---|
| Architecture §§7, 25, 56 | `<SECRET>` / `<SECRET: account 1369850 password>` — placeholder |
| `D:\Prop\mt5-sdk\.env.example` | `MT5_PASSWORD=replace_with_manager_password` — template, not live |
| `mt5-sdk/config/app_config.cpp` | `get("MT5_PASSWORD", "")` — reads env; no default secret |
| Product `src/` and `apps/` (excluding nuget obj) | **no hits** |

No `CTRADER_FIX_PASSWORD` anywhere except the architecture placeholders.

### 6.2 Connection strings

Grep for `ConnectionString`, `Server=.*;`, `User ID=`, `postgres://`, `redis://`, `mongodb://`, `mysql://` across json/cs/config/xml/env/md: **no product hits**.

`Infrastructure` references Npgsql + StackExchange.Redis in the csproj only. No connection string is configured. `mt5-sdk/src/db/pg_pool.cpp` takes `connStr` at `Initialize()`; nothing in-tree supplies a credentialed URL.

`mt5-sdk/.env.example`:

```env
DATABASE_URL=
DB_HOST=127.0.0.1
DB_PORT=5432
DB_NAME=mt5_sdk
DB_USER=postgres
DB_PASSWORD=
```

Empty password + local default user name. Not a live secret.

### 6.3 Other secret-shaped keys

| Key | Where | Value |
|---|---|---|
| `MT5_API_KEY` | `.env.example` | `replace_with_shared_service_secret` |
| `MT5_PASSWORD_ENCRYPTION_KEY` | `.env.example` | empty |
| `MT5_PROXY_PASSWORD` | `.env.example` | empty |
| `MT5_REMOTE_URL` | `.env.example` | `http://127.0.0.1:9100` (cleartext HTTP, loopback) |

`AppConfig` loads `MT5_API_KEY` / `MT5_PASSWORD_ENCRYPTION_KEY` / proxy password from environment first, then `.env`. No compiled-in defaults.

### 6.4 Product C# / workers

`apps/api/Program.cs` is the stock weatherforecast template.  
`apps/fix-worker` and `apps/mt5-worker` `Worker.cs` only log the current time.  
`src/Application`, `Domain`, `Infrastructure`, `Mt5`, `Fix.CTrader` are empty `Class1` templates.  
**No FIX session config, no MT5 password field, no Redis multiplexer, no EF connection.**

---

## 7. Vendor / third-party samples (not live Prop credentials)

Under `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\` MetaQuotes ships documented dummy values. These are **not** Achiever / StarwaveFX / Pepperstone secrets:

| File | What |
|---|---|
| `Examples/Web/NET/WebTrader/Web.config` | `metatrader_password` = well-known sample `Password1`; host `192.168.9.43`, login `1014` |
| `Examples/Web/PHP/web_registration/index.php` | `MT5_SERVER_WEB_PASSWORD` = same sample; `MYSQL_PASSWORD='mysql_password'`; `SMTP_PASSWORD='password'` |
| `Examples/Gateway/UniNewsServer/.../UniNewsServerPort.h` | `DEFAULT_NEWS_SERVER_PASSWORD` = `L"password"` |

Do not treat these as production. Do not copy them into product `appsettings`. They are vendor demo noise.

---

## 8. Secret-handling code (no live values; future leak surface)

These are **not** findings of secrets on disk. They are how secrets will flow once wired.

| Surface | Evidence | Risk |
|---|---|---|
| `AppConfig::load` | env first, then `.env`, then empty default | Correct order. Root `D:\Prop` has **no** `.gitignore`; only `mt5-sdk/.gitignore` ignores `.env`. |
| `MT5HttpClient` | `X-API-Key: ` + key on every REST/SSE call | Fine if TLS. `.env.example` default remote URL is **HTTP**. |
| `CreateUser` / `ChangePassword` / `CheckPassword` | JSON bodies include plaintext `password` / `investor_password` (`mt5_types.h` `UserParams` `to_json`) | Expected for Manager API. HTTP client does **not** log bodies (good). |
| `MT5Manager::SetProxy` / pool | builds `login:password` into `MTProxyInfo.auth`; logs **type + address:port only** | Password not logged. |
| `PgPool::createConnection` | `spdlog::error(..., PQerrorMessage(conn))` | libpq errors can echo the connection URI (password). When Postgres is wired, redact. |
| `logger.h` | no redaction filter | Relies on call sites not logging secrets. Matches §57 intent only if FIX/MT5 adapters never dump Logon (tag 554) or env dumps. |
| `mt5-sdk/README.md` | tells operators to keep manager password, `MT5_API_KEY`, `MT5_PASSWORD_ENCRYPTION_KEY` out of git | Policy exists for the SDK repo. |

§55 required product `.env.example` with placeholders only: **missing at `D:\Prop` root**. The .NET apps have no env template for FIX / Postgres / Redis / MT5 at all.

---

## 9. Gap vs §55 (process, not a leak)

| §55 requirement | Tree state |
|---|---|
| Never expose listed secrets to React | No React app yet; no API secret endpoints |
| Use env / OS store / Vault | SDK: env + `.env`. .NET: template UserSecrets IDs only; nothing loaded |
| Production secrets not in Git | No live secrets. `D:\Prop` is not a git repo. `mt5-sdk` tracks `.env.example` only |
| Placeholders only in `.env.example` | SDK example is placeholder-only. **No product-level `.env.example`** covering FIX / DB / Redis |
| Never log FIX auth tags | No FIX engine code yet |

---

## 10. Findings table

| ID | Sev | Finding | Evidence |
|---|---|---|---|
| A19-01 | P0 | Live password / FIX password / connection-string credential in tree | **Not found** |
| A19-02 | P1 | Architecture publishes live hosts, manager logins, cTrader account, SenderCompID, egress/proxy | §§7–8, 25, 56; 16 hits, architecture file only |
| A19-03 | P2 | No repo-root `.gitignore`; accidental `.env` at `D:\Prop` would not be ignored | `D:\Prop\.gitignore` missing; `D:\Prop` has no git; `mt5-sdk/.gitignore` covers SDK `.env` only |
| A19-04 | P2 | No product `.env.example` for MT5 / FIX / Postgres / Redis as §55 requires | Only `mt5-sdk/.env.example` |
| A19-05 | P2 | API `AllowedHosts: "*"` | `apps/api/appsettings.json` |
| A19-06 | P3 | Remote MT5 example URL is cleartext HTTP | `MT5_REMOTE_URL=http://127.0.0.1:9100` |
| A19-07 | P3 | Vendor examples contain dummy `Password1` / `password` | MetaQuotes samples; not live Prop |
| A19-08 | Info | `appsettings*.json` have no `ConnectionStrings` / password keys | All 8 files (6 source + 2 published) |
| A19-09 | Info | Product workers/API do not yet read `MT5_PASSWORD` or `CTRADER_FIX_PASSWORD` | empty templates |

---

## 11. Recommendations (do not implement in this audit)

1. Keep **all** password slots as `<SECRET>` / empty / `replace_with_*`. Do not paste live FIX/MT5/DB passwords into markdown, `appsettings`, or this reports tree.
2. If architecture v2 is published beyond the lab, split §56 into (a) public placeholder example with fake hosts/logins and (b) a **non-git** operator sheet for live IPs/logins. Live identifiers in §7–8 are documented as non-secret; still treat them as need-to-know.
3. When git is initialized at `D:\Prop`, add a root `.gitignore` covering `.env`, `.env.*`, `secrets.json`, `*.pfx`, `appsettings.*.local.json`, and keep only `.env.example`.
4. Add a product `.env.example` (placeholders only) for `MT5_*`, `CTRADER_FIX_PASSWORD`, `ConnectionStrings:Postgres`, Redis. Never a real value.
5. Before wiring FIX: reject logging of tags 553/554 and any `Password=` field (architecture §57).
6. Before wiring Postgres: do not log raw `PQerrorMessage` / Npgsql exceptions without redaction.
7. Tighten `AllowedHosts` when the API leaves the weatherforecast template.

---

## 12. Honesty close

Measured state: **zero live secrets** under `D:\Prop`. **Live non-secret venue identifiers exist only in the architecture markdown.** Product `appsettings*.json` are logging stubs. I did not invent a PASS on “no sensitive data in the architecture”; hosts and logins are in the open by design of §§7–8 and are repeated in the §56 example.

**Product source was not modified.**
