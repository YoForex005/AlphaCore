# D40 — Password / secrets grep (product source; vendor + reports excluded)

| Field | Value |
|---|---|
| Agent | D40 (senior engineer, password grep only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (UTC file stamps on secret-slot files: 07:02–08:07) |
| Artifact | `D:\Prop\reports\swarm\20260818\D40_secrets.md` |
| Workspace | `D:\Prop` |
| Product source modified | **No.** This report is the only write. |
| Compared to | `A19_security_secrets_scan.md`, `B25_secrets_rescan.md` (both stale vs current API JSON / `.env` layout) |

**Masking rule used in this report:** a live password, token, API key, or credentialed URI value is never copied. It appears only as `<MASKED len=N>` plus first/last character with asterisks when `N ≥ 3`. Empty strings, `string.Empty`, `null`, `...`, `<SECRET>`, `<BROKER_ISSUED_VALUE>`, `<BASE64_ENCODED_256BIT_KEY>`, and instructional `replace_with_*` placeholders are shown as they appear. They are not secrets.

---

## 0. Verdict

| Question | Result |
|---|---|
| Live MT5 / FIX / proxy / Redis / encryption-key password in product source? | **NONE FOUND** |
| Live `CTRADER_FIX_PASSWORD` / `MT5_PASSWORD` / `MT5_STARWAVEFX_PASSWORD`? | **NONE FOUND** (placeholders only) |
| Live credentialed URI (`postgres://user:pass@…`, `Password=<non-empty>`)? | **NONE FOUND** |
| Live API key / PEM / PFX / AWS / GitHub / Slack token? | **NONE FOUND** |
| Committed lab Postgres password in `docker-compose.yml`? | **YES — one value, masked below** |
| Real `.env` with filled secrets? | **No.** `D:\Prop\.env` exists but is the old `.env.example` text (all `<SECRET>`) |
| `D:\Prop\.env.example` | **ABSENT** (same SHA now lives at `.env`) |
| `mt5-sdk\.env` / user-secrets folders / `secrets.json` | **ABSENT** |
| P0 venue/FIX/MT5 credential in Git-shaped product files? | **No** |

**Overall:** product source is **secret-clean for venue credentials**. It is **not identifier-clean**. The only non-placeholder password literal is the local-compose Postgres password (lab-dev, not a broker/FIX secret). No value in C#, appsettings, `.env`, or docs required masking except that one compose value.

---

## 1. Scope and method

Read-only. Product source was not edited. Vendor `mt5-sdk\vendor\**` and `reports\**` were excluded. Also excluded from hit counts: `node_modules\`, `bin\`, `obj\`, `.git\`, swarm `_tmp_*`.

| Included (product source) | Excluded |
|---|---|
| `D:\Prop\src\**` | `vendor\**` |
| `D:\Prop\apps\**` (web `src` only; not `node_modules`) | `reports\**` |
| `D:\Prop\tests\**` | `bin\`, `obj\` |
| `D:\Prop\mt5-sdk\` minus `vendor\` | MetaQuotes sample `Password1` (vendor) |
| `D:\Prop\docs\`, root compose / env / README / architecture `.md` | published `apps\*\bin\**\appsettings*.json` copies |

| Step | What |
|---|---|
| Keyword grep | `password`, `passwd`, `pwd`, `POSTGRES_PASSWORD`, `Password=`, `SECRET`, `apikey` / `api_key`, `EmergencyFlattenApiKey` |
| Literal assignment | quoted `Password=…`, JSON `"Password": "…"`, unquoted compose `POSTGRES_PASSWORD:` |
| Credential URI / key material | `postgres://`, `redis://`, `User ID=`, PEM/PFX, `AKIA…`, `ghp_`, `sk-`, `xox` |
| Hidden files | `.env*`, `secrets.json`, `appsettings.{Production,Staging,Local}`, `*.pem` / `*.pfx` / `*.key` |
| Config bind | `GetConnectionString`, `DATABASE_URL`, `Configure<`, `AddUserSecrets` |
| Host / DTO leak | `Program.cs` `/api/settings`, `SettingsController`, dashboard DTOs, React types |

**Census (authored, exclude bin/obj/vendor/node_modules):** 84 product `.cs` under `src` + `apps` + `tests`. Password-keyword files (this pass): **28** paths listed in §8.

---

## 2. The only live password literal (masked)

`D:\Prop\docker-compose.yml` line 6, `postgres` service:

| Key | Class | Report value |
|---|---|---|
| `POSTGRES_USER` | lab identifier | `ti` (not a password) |
| `POSTGRES_DB` | lab identifier | `trader_intelligence` |
| `POSTGRES_PASSWORD` | **lab-dev password, committed** | `<MASKED len=11>` (`t*********y`) |

Not a venue / FIX / MT5 / Redis password. Compose does not interpolate `${DB_PASSWORD}`; the literal is in the file. Redis service has **no** `REDIS_PASSWORD` / `requirepass`.

If this stack is ever reachable beyond a laptop, rotate the local Postgres password. **Do not paste the unmasked value into other reports.** Product source was not edited.

SHA-256 `docker-compose.yml`: `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1`.

---

## 3. `.env` / `.env.example`

| Path | Present | SHA-256 | Secret class |
|---|---|---|---|
| `D:\Prop\.env` | **Yes** (3408 bytes) | `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA` | **Placeholders only** |
| `D:\Prop\.env.example` | **No** | — | Template was this SHA when B25 measured `.env.example` |
| `D:\Prop\mt5-sdk\.env` | **No** | — | — |
| `D:\Prop\mt5-sdk\.env.example` | Yes (4999 bytes) | `937F7CB0A6912A05BEE0E5B672C696D6D4B41F63FFD530D2451C56715020C47C` | Instructional placeholders |
| `apps\web\.env` / `.env.local` / `.env.development` | **No** | — | — |

`D:\Prop\.gitignore` ignores `.env` and `.env.*`, un-ignores `!.env.example`. Consequence: the product template now sits at a **gitignored** path, so a clone does not get the example unless someone restores `.env.example`. That is a process defect, not a credential dump. `.env` itself is **not** filled with live passwords.

Password-shaped keys in `D:\Prop\.env` (values as on disk — all placeholders):

| Key | Value |
|---|---|
| `MT5_PASSWORD` | `<SECRET>` |
| `ACHIEVER_PROXY_USERNAME` | `<SECRET>` |
| `ACHIEVER_PROXY_PASSWORD` | `<SECRET>` |
| `MT5_STARWAVEFX_PASSWORD` | `<SECRET>` |
| `CTRADER_FIX_PASSWORD` | `<SECRET>` |
| `DATABASE_URL` | `Host=localhost;Port=5432;Database=trader_intelligence;Username=ti;Password=<SECRET>` |
| `MT5_PASSWORD_ENCRYPTION_KEY` | `<BASE64_ENCODED_256BIT_KEY>` |

Non-secret identifiers in the same file (not masked): Achiever `57.128.141.65` / login `2027`; StarwaveFX `84.201.6.142` / `9904`; cTrader host `live-us-eqx-01.p.c-trader.com`; account `1369850`; SenderCompID `live.pepperstone.1369850`; egress `81.29.145.69`.

`mt5-sdk\.env.example` (not live):

| Key | Value |
|---|---|
| `MT5_PASSWORD` | `replace_with_manager_password` |
| `MT5_PROXY_PASSWORD` | empty |
| `MT5_API_KEY` | `replace_with_shared_service_secret` |
| `MT5_PASSWORD_ENCRYPTION_KEY` | empty |
| `DB_PASSWORD` | empty |
| `DATABASE_URL` | empty |

SDK `AppConfig` loads those from the environment / `.env` with default `""`. No compiled-in manager password.

---

## 4. `appsettings*.json` (source of truth)

No `appsettings.Production.json` / `Staging` / `*.local.json`. No `secrets.json` under `D:\Prop`. `%APPDATA%\Microsoft\UserSecrets` **does not exist**.

| Path | SHA-256 | Password / secret values |
|---|---|---|
| `D:\Prop\apps\api\appsettings.json` | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` | `ConnectionStrings:Postgres` has `Password=` **empty**; `EmergencyFlattenApiKey` = `""` |
| `D:\Prop\apps\api\appsettings.Development.json` | `81B5E6DC0290CB48038DD67C6F9C37851C16F8362A6350BD1A43D9B27E8B0481` | same empty `Password=` on `Postgres`; gitignored |
| `apps\fix-worker\appsettings.json` | `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` | Logging only |
| `apps\fix-worker\appsettings.Development.json` | same as worker json | Logging only |
| `apps\mt5-worker\appsettings.json` | same | Logging only |
| `apps\mt5-worker\appsettings.Development.json` | same | Logging only |

### 4.1 API `appsettings.json` secret-shaped keys (empty — nothing to mask)

```json
"ConnectionStrings": {
  "Postgres": "Host=localhost;Database=trader_intelligence;Username=postgres;Password=",
  "Redis": "localhost:6379"
}
```

```json
"EmergencyFlattenApiKey": ""
```

`CTraderFix` block has host/ports/CompIDs and **no** `Password` key. `SenderCompId` is `""`. `TargetCompId` is `CSERVER` (case differs from architecture `cServer` — identifier, not a password).

B25’s `CTrader:Password` / `AccountId` `1369850` / `live-us-eqx-01…` snapshot is **stale**. Those keys are gone from current API JSON.

### 4.2 Connection string is unused by current DI

```19:29:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var connection = configuration.GetConnectionString("TraderIntelligence")
                         ?? configuration["DATABASE_URL"];

        if (string.IsNullOrWhiteSpace(connection) || connection.Contains("<SECRET>", StringComparison.Ordinal))
        {
            services.AddDbContext<TraderDbContext>(o => o.UseInMemoryDatabase("trader-intelligence"));
        }
        else
        {
            services.AddDbContext<TraderDbContext>(o => o.UseNpgsql(connection));
        }
```

There is **no** `ConnectionStrings:TraderIntelligence` key now. `Postgres` is not read. Empty/`<SECRET>` still forces InMemory. Filling `Password=` later in the unused `Postgres` slot is still a one-edit git leak (`appsettings.json` is **not** gitignored; `appsettings.Development.json` is).

---

## 5. Product C# — password slots (no live values)

| File | Member | Default | Bound? |
|---|---|---|---|
| `src\Fix.CTrader\Configuration\CTraderFixOptions.cs:20` | `Password` | `string.Empty` | **No** `Configure<CTraderFixOptions>` / `GetSection("CTrader")` in any host |
| `src\Mt5\Configuration\Mt5BrokerOptions.cs:21` | `Password` | `null` | **No** bind; no `Mt5` section in appsettings |
| `src\Mt5\Configuration\Mt5BrokerOptions.cs:38` | `ProxyPassword` | `null` | **No** bind |
| `src\Mt5\Configuration\Mt5BrokerOptions.cs:44` | `ApiKey` | `null` | **No** bind |

SHA-256: `CTraderFixOptions.cs` `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308`; `Mt5BrokerOptions.cs` `64A840278433587B55805042873545D0535C64E7E50DDDD9BF8FDC72E635FAB7`.

Grep `Log.*(Password|ApiKey)` in `src` → **0**. FIX tags `553`/`554` as password emit → **0** in product C#. `FixMessageParser` / `FixSimulationHarness` do not write a password tag.

Domain entities (`Broker`, `FixSessionState`, `Mt5Account`) have **no** password columns. `TraderDbContext` maps none. `DemoSeeder` writes hosts + manager login **numbers** only (SHA `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20`). `FakeMt5BrokerConnector` has no credentials.

Worker `UserSecretsId` GUIDs (`FixWorker-400770db-…`, `Mt5Worker-6850a13e-…`) are template IDs, not secrets. API csproj has **no** `UserSecretsId`. No `AddUserSecrets` call.

Tests (`tests\Unit\*`, `tests\Integration\SeedingAndStoreTests.cs`): InMemory only. Hits on `password` are `CancellationToken` only.

---

## 6. Hosts / dashboard / React — no password echo

| Surface | Password leak? |
|---|---|
| `apps\api\Program.cs` `GET /api/settings` | **No.** Risk limits, feature flags, broker **names** only. |
| `apps\api\Controllers\SettingsController.cs` | **No.** Documents “Passwords and API keys are never exposed.” GET returns risk + flags. PUT writes Redis flag keys only. **Not wired** (`AddControllers` / `MapControllers` absent). |
| `GET /api/health`, `/health`, `/ready` | **No** credentials. |
| `EfDashboardQueries` | Broker DTO uses `MaskLogin` (floor to hundreds). No password field. |
| `DashboardModels` / web `types/index.ts` | No `password` / `apiKey` properties. |
| `FixSessionsPage.tsx` | Copy: “Password is never shown.” |
| `SettingsPage.tsx` | Copy: “Secrets are never returned to the browser.” |
| `apps\web\src\api\client.ts` | `VITE_API_URL` or `http://localhost:5000`. No `Authorization`. |
| `TraderIntelligence.Api.http` | Health/dashboard GETs only. |
| `launchSettings.json` (all three hosts) | localhost + `Development`. No credentials. |

`CTraderFix:FileLogPath` = `./fixlogs` is a **future** 554 leak surface if a real initiator is added without redaction. No log files with passwords exist in product source.

---

## 7. Docs / architecture / SDK (placeholders)

Architecture `MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` password assignments are all `<SECRET>` (lines 376, 432, 1059, 2041, 2054, 2066, 2078). Line 1059 is `<SECRET: account 1369850 password>` — placeholder plus **identifier**, not a password.

`docs\deployment.md`: `set MT5_PASSWORD=...`, `ACHIEVER_PROXY_PASSWORD=...`, compose example `POSTGRES_PASSWORD: ${DB_PASSWORD}`. Ellipsis / env interpolation. No live value.

`docs\architecture.md`: “API … no secrets.” `README.md`: “Secrets stay in environment / `.env` (see `.env.example`).” (`.env.example` is currently missing; see D40-04.)

`mt5-sdk` (non-vendor): password **APIs** (`ChangePassword` / `CheckPassword`, `UserParams.password`, proxy `login:password` auth string). Group probe comment: do not print the password; `GetLastError` is treated as non-secret. HTTP client PUTs `{"password": …}` to the Manager bridge — runtime body, not a committed secret. `pg_pool` has **no** `password` hits.

---

## 8. Password-keyword file inventory (product only)

Hit counts are `password|passwd|POSTGRES_PASSWORD` (case-insensitive). Not every hit is a secret.

| Hits | Path |
|---:|---|
| 8 | `D:\Prop\.env` |
| 1 | `D:\Prop\apps\api\appsettings.json` |
| 1 | `D:\Prop\apps\api\appsettings.Development.json` |
| 1 | `D:\Prop\apps\api\Controllers\SettingsController.cs` |
| 1 | `D:\Prop\apps\web\src\pages\FixSessionsPage.tsx` |
| 1 | `D:\Prop\docker-compose.yml` |
| 4 | `D:\Prop\docs\deployment.md` |
| 14 | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| 6 | `D:\Prop\mt5-sdk\.env.example` |
| 3 | `D:\Prop\mt5-sdk\config\app_config.cpp` |
| 4 | `D:\Prop\mt5-sdk\config\app_config.h` |
| 3 | `D:\Prop\mt5-sdk\README.md` |
| 2 / 6 / 2 | `imt5_client.h` / `mt5_http_client.cpp` / `.h` |
| 15 / 4 | `mt5_manager.cpp` / `.h` |
| 19 / 7 | `mt5_pool.cpp` / `.h` |
| 6 / 1 / 1 | `mt5_types.h` / `mt5_watchdog.cpp` / `.h` |
| 5 / 4 / 2 | SDK tests (`group_probe`, `news_calendar_probe`, `time_window_test`) |
| 2 | `src\Fix.CTrader\Configuration\CTraderFixOptions.cs` |
| 2 | `src\Mt5\Configuration\Mt5BrokerOptions.cs` |

---

## 9. FLAG — live identifiers (not passwords)

Same targeting set as A19/B25. Still in compiled defaults + seed + `.env` + architecture. **Removed** from current API `appsettings.json`.

| Kind | Where |
|---|---|
| cTrader FIX host | `CTraderFixOptions.Host` default; `.env` `CTRADER_FIX_HOST`; architecture |
| Account / SenderCompID | `1369850` / `live.pepperstone.1369850` in C# defaults, `.env`, seed |
| Achiever host + manager login | `DemoSeeder`, `.env` — `57.128.141.65` / `2027` |
| StarwaveFX host + manager login | `DemoSeeder`, `.env` — `84.201.6.142` / `9904` |
| Egress IP | `.env` `ACHIEVER_EGRESS_IP=81.29.145.69` |

Classification: reconnaissance if the tree is published. Attackers still need the passwords.

---

## 10. Findings

| ID | Sev | Finding | Evidence |
|---|---|---|---|
| D40-01 | P0 | Live MT5 / FIX / proxy / Redis / encryption password in product source | **Not found.** No venue value required masking |
| D40-02 | P2 | Lab Postgres password committed in compose | `docker-compose.yml:6` — `<MASKED len=11>` |
| D40-03 | P2 | Empty `Password=` on committed `ConnectionStrings:Postgres` (and gitignored Development twin). Filling it is a one-edit leak. Key is unused (`TraderIntelligence` / `DATABASE_URL` are what DI reads) | `apps\api\appsettings.json:3`; `DependencyInjection.cs:19–22` |
| D40-04 | P2 | Product `.env.example` missing; identical placeholder file now at gitignored `.env` | SHA `56C81786…`; `.gitignore` `.env` / `!.env.example` |
| D40-05 | P3 | Empty `EmergencyFlattenApiKey` slot in committed API JSON; no C# reader found | `appsettings.json:42` |
| D40-06 | Info | Password slots in C# remain empty / unbound | `CTraderFixOptions`, `Mt5BrokerOptions` |
| D40-07 | Info | Persistence and dashboard contracts have no password fields; Settings GET does not return secrets | `Broker.cs`, `DashboardModels.cs`, `SettingsController.cs` |
| D40-08 | Info | Live venue identifiers remain in C# defaults + seed + `.env`; no longer in API JSON | §9 |
| D40-09 | Info | B25 `CTrader:Password=""` / `AccountId=1369850` JSON is stale | Current SHA `69D41CAD…` |

---

## 11. Recommendations (not implemented — product source untouched)

1. Keep every password slot empty / `<SECRET>`. If a live value is pasted into `appsettings.json` or `.env` that might be committed, treat as P0, rotate, move to user-secrets / Vault.
2. Restore a tracked `.env.example` (placeholders only). Leave `.env` gitignored and unfilled.
3. Prefer `${POSTGRES_PASSWORD}` from a local env file over a literal in `docker-compose.yml`.
4. When FIX is bound, redact tag 554 / `Password=` at the sink before `FileLogPath` is used.
5. Do not copy the compose lab password unmasked.

---

## 12. Honesty close

Measured **now:** **zero live venue passwords** in product source (vendor + reports excluded). **One** committed lab Postgres password, **masked** in this file. **Empty** `Password=` / `EmergencyFlattenApiKey` slots in API JSON. **Placeholder-only** `D:\Prop\.env` (former `.env.example`). **No** `secrets.json`, **no** user-secrets directory, **no** PEM/PFX, **no** credentialed URI with a non-empty password.

I did not mask empty strings or `<SECRET>`. I did not edit product source.

**Product source was not modified.**
