# B25 — Secrets rescan (new C# + appsettings)

| Field | Value |
|---|---|
| Agent | B25 (senior engineer, secrets rescan only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:21:18+05:30 |
| Workspace | `D:\Prop` |
| Scope | Product C# under `src\`, `apps\`, `tests\` (79 files, exclude `bin`/`obj`/`vendor`) and every `appsettings*.json` (6 source + 8 published copies) |
| Compared to | `A19_security_secrets_scan.md` (pre-implementation stubs) |
| Product source modified | **No.** This report is the only write. No masking applied on disk. |
| User-secrets dirs present | **No** (both Worker `UserSecretsId` folders absent under `%APPDATA%\Microsoft\UserSecrets`) |
| `D:\Prop\.env` / `mt5-sdk\.env` | **Absent** |

**Masking rule used in this report:** if a live password, token, or credentialed URI had been found, the value would appear only as `<MASKED len=N>` (first/last character + asterisks when length ≥ 3). Empty strings, `string.Empty`, `null`, and `<SECRET>` placeholders are shown as they appear. They are not secrets.

---

## 0. Verdict

| Question | Result |
|---|---|
| Live password / FIX password / MT5 password / proxy password / API key in **new C#**? | **NONE FOUND** |
| Live password in **source `appsettings*.json`**? | **NONE FOUND** (`CTrader:Password` is `""`) |
| Live `ConnectionStrings:TraderIntelligence`? | **NONE FOUND** (empty string) |
| Live credentialed URI (`postgres://…@…`, `User ID=` / `Password=` with a value) in C# or appsettings? | **NONE FOUND** |
| Password **slots** added since A19? | **YES** — empty / `string.Empty` / `null` only |
| Live **non-secret** venue identifiers now in C# / API appsettings (not just architecture markdown)? | **YES — FLAG** |
| P0 committed live secret in C# or appsettings? | **No** |

**Overall:** product C# and appsettings remain **secret-clean**. They are **no longer identifier-clean**. A19’s “empty Class1 / logging-only appsettings” snapshot is **stale**. The tree now has real options types, a `CTrader` JSON section, and a seed that copies live hosts / manager logins / SenderCompID into the demo database. Those are reconnaissance values, not passwords.

No value in C# or appsettings required masking.

---

## 1. Method

Read-only. Product source was not edited. Vendor `mt5-sdk\vendor\**` C# was not treated as product.

| Step | What |
|---|---|
| Census | `Get-ChildItem` of `*.cs` under `src`, `apps`, `tests` excluding `bin`/`obj` → **79** files |
| Appsettings census | Every `appsettings*.json` under `D:\Prop` excluding vendor → **14** files (6 source + 8 `bin` copies) |
| Keyword scan | `Password`, `ProxyPassword`, `ApiKey`, `Secret`, `ConnectionString`, `User ID`, `DATABASE_URL`, `REDIS`, `SenderComp`, live IPs / `1369850` / `pepperstone` |
| Literal assignment scan | `Password\s*=\s*["']…["']`, `"Password"\s*:\s*"[^"]+"`, credential URI schemes |
| Config bind scan | `Configure<`, `GetSection(`, `GetConnectionString`, `AddUserSecrets` |
| Adjacent (out of primary scope, noted only) | `docker-compose.yml`, `.env.example`, `.gitignore`, user-secrets folders |

SHA-256 of the files that now carry secret **slots** or live identifiers:

| SHA-256 | Path |
|---|---|
| `8DCE4CBECDD1F8E7B03DDF1C25430BACCD05795D64B19798A6B0CDAACE85902B` | `D:\Prop\apps\api\appsettings.json` (source == `bin\Debug\net8.0` copy) |
| `73F95F9E0CEB205FC1C4DC50C07697FCFA29D7087868C2AEF1D504CB38C771EC` | `D:\Prop\apps\api\appsettings.Development.json` |
| `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` | `apps\fix-worker\appsettings.json` == `apps\mt5-worker\appsettings.json` == both `*.Development.json` |
| `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` | `src\Fix.CTrader\Configuration\CTraderFixOptions.cs` |
| `64A840278433587B55805042873545D0535C64E7E50DDDD9BF8FDC72E635FAB7` | `src\Mt5\Configuration\Mt5BrokerOptions.cs` |
| `139D8F872DC473F0C5381AF2393BDBBE60E1D9A2A5179DD1D1737E04CCC00BEF` | `src\Infrastructure\Seeding\DemoSeeder.cs` |
| `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | `src\Infrastructure\DependencyInjection.cs` |

---

## 2. Delta vs A19

A19 measured empty `Class1` templates and logging-only appsettings. That is no longer true.

| Surface | A19 | This rescan |
|---|---|---|
| Product C# | stub `Class1` | **79** implemented files (domain, ingestion, FIX options, seed, API host) |
| `appsettings*.json` | Logging only; no `ConnectionStrings`; no password keys | API `appsettings.json` now has `ConnectionStrings` + `CTrader.Password` |
| `CTraderFixOptions` / `Mt5BrokerOptions` | did not exist | exist; password properties default empty/`null` |
| Live host / login / CompID in C# | **absent** (architecture markdown only) | **present** in `CTraderFixOptions` defaults + `DemoSeeder` + API appsettings `AccountId` |
| Connection-string loader | none | `GetConnectionString("TraderIntelligence")` / `DATABASE_URL`; empty or `<SECRET>` → InMemory |
| User-secrets folders | none | still none |
| Repo-root `.gitignore` | missing | present; ignores `.env` and `appsettings.Development.json` |

---

## 3. Appsettings — complete (source of truth)

No `appsettings.Production.json`, `Staging`, or `*.local.json`. No `secrets.json` under `D:\Prop`.

| Path | Keys | Password / connection values |
|---|---|---|
| `D:\Prop\apps\api\appsettings.json` | `Logging`, `AllowedHosts`, `ConnectionStrings`, `CTrader` | `TraderIntelligence` = `""`; `CTrader.Password` = `""` |
| `D:\Prop\apps\api\appsettings.Development.json` | `Logging` only | none |
| `D:\Prop\apps\fix-worker\appsettings.json` | `Logging` only | none |
| `D:\Prop\apps\fix-worker\appsettings.Development.json` | `Logging` only | none |
| `D:\Prop\apps\mt5-worker\appsettings.json` | `Logging` only | none |
| `D:\Prop\apps\mt5-worker\appsettings.Development.json` | `Logging` only | none |

Published copies (`bin\Debug` / `bin\Release`) match their source files. API source SHA equals `apps\api\bin\Debug\net8.0\appsettings.json`.

### 3.1 The only new secret-shaped JSON (masked policy applied)

`D:\Prop\apps\api\appsettings.json` (lines 8–20), values shown as on disk because they are empty — nothing to mask:

```json
"ConnectionStrings": {
  "TraderIntelligence": ""
},
"CTrader": {
  "Host": "live-us-eqx-01.p.c-trader.com",
  "AccountId": "1369850",
  "Password": "",
  "UseSsl": true,
  "QuoteEnabled": true,
  "TradeSessionEnabled": true,
  "RealCopyExecutionEnabled": false
}
```

| Key | Value class |
|---|---|
| `ConnectionStrings:TraderIntelligence` | empty — **not a secret** |
| `CTrader:Password` | empty — **slot only** |
| `CTrader:AccountId` | live Pepperstone account id — **identifier, not a password** |
| `CTrader:Host` | live cTrader FIX host — **identifier** |
| `CTrader:RealCopyExecutionEnabled` | `false` — correct floor |

`launchSettings.json` (api / fix-worker / mt5-worker): localhost URLs + `Development` only. No credentials. `TraderIntelligence.Api.http` hits `/health` and dashboard GETs; no `Authorization` header.

Worker csproj `UserSecretsId` GUIDs are template IDs, not secrets. Corresponding `%APPDATA%\Microsoft\UserSecrets\…` directories **do not exist**.

---

## 4. New C# — password slots (no live values)

Only **four** product `Password` / `ApiKey` members exist. None is assigned a non-empty literal.

| File | Member | Default | Bound from config? |
|---|---|---|---|
| `src\Fix.CTrader\Configuration\CTraderFixOptions.cs:20` | `Password` | `string.Empty` | **No** `Configure<CTraderFixOptions>` / `GetSection("CTrader")` in any host |
| `src\Mt5\Configuration\Mt5BrokerOptions.cs:21` | `Password` | `null` | **No** bind |
| `src\Mt5\Configuration\Mt5BrokerOptions.cs:38` | `ProxyPassword` | `null` | **No** bind |
| `src\Mt5\Configuration\Mt5BrokerOptions.cs:44` | `ApiKey` | `null` | **No** bind |

Comments on those members say “secret placeholder” / “Must never be logged.” Grep for `Log.*(Password|ApiKey)` in `src` → **0 hits**. Grep for FIX tags `553`/`554` in product C# → **0 hits**. `FixMessageParser` / `FixSimulationHarness` do not emit a password tag.

`CTraderFixOptions` is **not** registered in DI. The only runtime read of the `CTrader` section is:

```21:22:D:\Prop\apps\fix-worker\Worker.cs
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);
```

That does **not** load or log `Password`. Worker `appsettings.json` does not even contain a `CTrader` section, so the flag stays at the code default `false` unless environment / user-secrets override it.

### 4.1 Connection string path (empty → InMemory)

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

Current API appsettings value is `""` → InMemory. The `<SECRET>` guard matches `.env.example`’s placeholder connection string. **No Npgsql password is compiled in.**

### 4.2 Persistence does not store secrets

| Type | Password column? |
|---|---|
| `Domain\Entities\Broker` | **No** (`ManagerLogin` only) |
| `Domain\Entities\FixSessionState` | **No** (host / CompIDs / seq only) |
| `Domain\Entities\Mt5Account` | **No** |
| `TraderDbContext` | no password property mapping |
| `DemoBrokerFactory` / `FakeMt5BrokerConnector` | synthetic deals only; no credentials |

Tests (`tests\Unit\*`, `tests\Integration\SeedingAndStoreTests.cs`) use `UseInMemoryDatabase`. No connection-string or password literals.

### 4.3 Hosts do not echo secrets

`apps\api\Program.cs` `/api/settings` returns risk limits, feature flags, and broker **names**. It does not return `Password`, `AccountId`, or connection strings. `/api/health` is a demo stub (no credential dump).

---

## 5. FLAG — live identifiers now in product C# / appsettings

Not passwords. Same targeting set A19 confined to architecture markdown. **New leak surface:** compiled defaults + demo seed + committed API JSON.

| Kind | Where | Value |
|---|---|---|
| cTrader FIX host | `CTraderFixOptions.Host` default; API `CTrader:Host`; `DemoSeeder` | `live-us-eqx-01.p.c-trader.com` |
| cTrader account | API `CTrader:AccountId` | `1369850` |
| SenderCompID | `CTraderFixOptions.Quote` / `Trade` defaults; `DemoSeeder` (both sessions) | `live.pepperstone.1369850` |
| Achiever host + manager login | `DemoSeeder` | `57.128.141.65` / `2027` |
| StarwaveFX host + manager login | `DemoSeeder` | `84.201.6.142` / `9904` |
| FIX ports | `CTraderFixOptions` + seed | 5211/5201 quote, 5212/5202 trade |

`Broker` rows seeded by `DemoSeeder` persist those hosts and manager logins into whatever store is active (InMemory today; Npgsql the moment a non-empty connection string is supplied).

Classification: `EXISTS_NEEDS_REFACTOR` if this tree is git-published. Attackers still need the passwords. This is reconnaissance, not a credential dump.

---

## 6. Adjacent (not C# / not appsettings) — recorded so it is not mistaken for a C# leak

Primary scope was C# + appsettings. These were checked so a later pass does not reopen B25.

| Location | Secret class | This report |
|---|---|---|
| `D:\Prop\.env.example` | placeholders (`MT5_PASSWORD=<SECRET>`, `CTRADER_FIX_PASSWORD=<SECRET>`, `DATABASE_URL=…Password=<SECRET>`) | **Good.** Not copied further |
| `D:\Prop\.env` | — | **Absent** |
| `docker-compose.yml` `POSTGRES_PASSWORD` | **lab-dev password present** | **Masked:** `<MASKED len=11>` (`t*********y`). Not a venue/FIX/MT5 password. User asked not to modify product source; compose was not edited |
| `D:\Prop\.gitignore` | ignores `.env`, `.env.*` (keeps `.env.example`), `secrets.json`, `appsettings.Development.json`, `appsettings.*.local.json` | **Does not** ignore `apps\api\appsettings.json` — that is the committed password-slot file |
| Vendor MetaQuotes samples | dummy `Password1` etc. | Out of scope; same as A19-07 |

If the compose value must be treated as a committed secret, rotate the local Postgres password when the stack is no longer lab-only. Do not paste the unmasked value into other reports.

---

## 7. Findings

| ID | Sev | Finding | Evidence |
|---|---|---|---|
| B25-01 | P0 | Live password / token / credentialed connection string in new C# or appsettings | **Not found.** No value required masking |
| B25-02 | P1 | Live venue identifiers now hardcoded in C# defaults + API appsettings + demo seed | `CTraderFixOptions.cs` lines 10, 47, 68; `apps\api\appsettings.json` lines 13–14; `DemoSeeder.cs` lines 35–53, 74–93 |
| B25-03 | P2 | Committed `CTrader:Password` key in `apps\api\appsettings.json` is an empty slot sitting next to a live `AccountId`. Filling it later is a one-edit git leak | SHA `8DCE4C…5902B`; `.gitignore` does not cover this file |
| B25-04 | P2 | `Mt5BrokerOptions` password / proxy / API-key slots exist but no host binds them; no `Mt5` section in any appsettings | `Mt5BrokerOptions.cs`; six source appsettings |
| B25-05 | P3 | `CTraderFixOptions` is not bound; API JSON `CTrader` block is mostly dormant. Only `RealCopyExecutionEnabled` is read (by fix-worker, whose appsettings lack the section) | `Worker.cs:21`; no `Configure<CTraderFixOptions>` |
| B25-06 | Info | Empty / `<SECRET>` connection string correctly forces InMemory | `DependencyInjection.cs:22–25` |
| B25-07 | Info | Persistence model has no password columns | `Broker.cs`, `FixSessionState.cs`, `TraderDbContext.cs` |
| B25-08 | Info (adjacent) | Compose lab Postgres password exists; masked above | `docker-compose.yml` line 6 |

---

## 8. Recommendations (do not implement in this rescan)

1. Keep every password slot `""` / `string.Empty` / `<SECRET>`. If a live value is ever pasted into `appsettings.json`, treat it as a P0, rotate the venue/DB credential, and move the slot to env / user-secrets / Vault.
2. Prefer env (`CTRADER_FIX_PASSWORD`, `MT5_PASSWORD`, `DATABASE_URL`) over a committed `CTrader:Password` key beside a live account id.
3. When FIX options are bound, never log `Password`, tag 554, or the raw Logon. `CTraderFixOptions` already documents this; there is still no redaction filter.
4. Split live CompID / host defaults out of compiled C# if the repo is shared; keep A75-style placeholders in `.env.example`.
5. Compose lab password is out of this file’s edit scope; do not copy it unmasked.

---

## 9. Honesty close

Measured state of **new C# and appsettings:** **zero live passwords**. **One empty committed password key** (`CTrader:Password`) and **one empty connection string**. **Live non-secret identifiers have moved from architecture markdown into product C# and API appsettings** — that is the real delta from A19. I did not mask empty strings. I did not edit product source. The only masked value in this report is the out-of-scope compose lab Postgres password.

**Product source was not modified.**
