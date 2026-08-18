# A008 — Environment wiring (key names only)

| Field | Value |
|---|---|
| Agent | A008 (env wiring) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A008_env_wiring.md` |
| Product source edited | **No** |
| Config / `.env*` edited | **No** |
| Secrets printed | **No** (key names only) |

**Read (current tree):**

- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (`NativeMt5Options` + `ProxySet`)
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (`TryLogonAsync` signature only)
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\api\appsettings.Development.json`
- `D:\Prop\apps\api\Properties\launchSettings.json`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\mt5-sdk\.env.example` (key names)

**Not on disk:** `D:\Prop\.env.example` — **ABSENT**.  
**Present, gitignored:** `D:\Prop\.env` — key names listed below; **values omitted**.

---

## 0. Verdicts (one screen)

| Question | Measured answer |
|---|---|
| Does API load `D:\Prop\.env` **before** `CreateBuilder`? | **NO** |
| Does any product C# host call `EnvFile.Load`? | **NO** — `EnvFile` has **zero callers** |
| How does `IConfiguration` see env vars? | Only if they are already in the **process environment**. `WebApplication.CreateBuilder` / `Host.CreateApplicationBuilder` add process env after start. They do **not** parse a dotenv file. |
| `DATABASE_URL` missing / empty / contains `<SECRET>`? | **In-memory** EF (`UseInMemoryDatabase("trader-intelligence-live")`) |
| `appsettings.json` `ConnectionStrings:Postgres` used by DI? | **NO** — DI looks at `ConnectionStrings:TraderIntelligence` then `DATABASE_URL` |
| Root `.env.example`? | **ABSENT** |
| C++ SDK example? | `D:\Prop\mt5-sdk\.env.example` exists; proxy key **names differ** from product C# |

---

## 1. `EnvFile` — loader exists, never invoked

`D:\Prop\src\Mt5\Env\EnvFile.cs` is a static dotenv parser:

- skip missing path
- skip blank / `#` comments / lines without `=`
- first `=` splits key/value
- strip surrounding double quotes
- `Environment.SetEnvironmentVariable(key, value)`

Ripgrep of `*.cs` under `D:\Prop`: the **only** hit is the type declaration.  
`apps/api/Program.cs` line 7 is immediately:

```csharp
var builder = WebApplication.CreateBuilder(args);
```

No `EnvFile.Load(...)`, no path to `D:\Prop\.env`, no `DotNetEnv`, no `AddEnvironmentVariables` extra call.  
Workers use `Host.CreateApplicationBuilder(args)` the same way.

**Implication:** filling `D:\Prop\.env` does nothing until an operator (or a missing host line) copies those keys into the process environment, or until a host calls `EnvFile.Load` **before** `CreateBuilder` so `AddEnvironmentVariables` can see them.

---

## 2. API `appsettings.json` — what it does **not** wire

`D:\Prop\apps\api\appsettings.json` keys (names only):

| Section / key | Consumed by live MT5 / FIX logon? |
|---|---|
| `ConnectionStrings:Postgres` | **No** (wrong name vs DI) |
| `ConnectionStrings:Redis` | **No** (no Redis client in DI) |
| `Serilog:*` | No Serilog package wired in current `Program.cs` |
| `Cors:AllowedOrigins` | Unused — `Program.cs` uses `AllowAnyOrigin` |
| `CTraderFix:QuoteHost` / `QuotePort` / `TradeHost` / `TradePort` / `SenderCompId` / `TargetCompId` / `HeartBeatInterval` / `ResetOnLogon` / `FileStorePath` / `FileLogPath` | **No** — logon reads **flat** `CTRADER_FIX_*` |
| `RiskEngine:*` | Not bound in `AddTraderIntelligence` |
| `FeatureFlags:*` | Not bound in `AddTraderIntelligence` |
| `AllowedHosts` | ASP.NET default |

`appsettings.Development.json` repeats `ConnectionStrings:Postgres` / `Redis`, `Serilog`, `Cors`. Same unused names.

`launchSettings.json` process env: **`ASPNETCORE_ENVIRONMENT` only**. No MT5 / FIX / `DATABASE_URL`.

---

## 3. Database wiring

`AddTraderIntelligence` (`DependencyInjection.cs`):

```text
connection = ConnectionStrings:TraderIntelligence  ??  DATABASE_URL
if missing/whitespace OR value contains token "<SECRET>"
    → UseInMemoryDatabase("trader-intelligence-live")
else
    → UseNpgsql(connection)
```

| Key | Required for Postgres? |
|---|---|
| `ConnectionStrings:TraderIntelligence` | Yes (first choice) — **not** present in API appsettings |
| `DATABASE_URL` | Yes (fallback) — not in appsettings |

**Placeholder rule (code, not convention):** any `DATABASE_URL` whose string contains `<SECRET>` is treated as unset → **in-memory**. Empty/`<SECRET>`-token example files therefore never open Npgsql.

---

## 4. Fail-closed MT5 gate

`LiveMt5Registration.HasRealPasswords` requires **both**:

- `MT5_PASSWORD`
- `MT5_STARWAVEFX_PASSWORD`

Each must be non-whitespace and must **not** contain `<SECRET>` or `(a/c`.

If either fails, `AddTraderIntelligence` throws:

`Real MT5 passwords are required. Dummy/fake broker data is disabled.`

Because hosts do not load `D:\Prop\.env`, an API process started from Visual Studio / `dotnet run` with only `launchSettings` will **throw at startup** unless the OS already has those two keys.

---

## 5. Key names required — Achiever + proxy

Product C# (`LiveMt5Registration.CreateConnectors` + `NativeMt5BrokerConnector.ConnectCore`). **Names only.**

### 5.1 Always read (Achiever = unprefixed `MT5_*`)

| Key | Role | If missing |
|---|---|---|
| `MT5_PASSWORD` | Manager password; **gate** | Startup throw (`HasRealPasswords`) |
| `MT5_SERVER` | Manager host / IP (no scheme) | `""` → connect fails |
| `MT5_LOGIN` | Manager login (ulong) | `0` → connect fails |
| `MT5_PORT` | Manager port | default **443** |

### 5.2 Proxy (Achiever only — actually applied)

Proxy is used only when `ACHIEVER_PROXY_ENABLED` parses `true` **and** `ProxyHost` is non-whitespace (`ProxySet` + HTTP type).

| Key | Role | If missing while enabled |
|---|---|---|
| `ACHIEVER_PROXY_ENABLED` | Master switch | treated false → **direct** connect |
| `ACHIEVER_PROXY_HOST` | Proxy address | `ProxySet` skipped even if enabled |
| `ACHIEVER_PROXY_PORT` | Proxy port | `0` (invalid if host set) |
| `ACHIEVER_PROXY_USERNAME` | Auth user | empty auth string |
| `ACHIEVER_PROXY_PASSWORD` | Auth password | empty if user empty |

**Required set for Achiever-through-proxy:**  
`MT5_SERVER`, `MT5_PORT`, `MT5_LOGIN`, `MT5_PASSWORD`, `ACHIEVER_PROXY_ENABLED`, `ACHIEVER_PROXY_HOST`, `ACHIEVER_PROXY_PORT`, `ACHIEVER_PROXY_USERNAME`, `ACHIEVER_PROXY_PASSWORD`.

**Not read by C# Achiever connector (present on operator sheet / docs only):**  
`MT5_DEFAULT_GROUP`, `MT5_MODE`, `MT5_POOL_SIZE`, `MT5_SERVER_NAME`, `ACHIEVER_EGRESS_IP`.

**Do not confuse with C++ SDK names** (`IS_MT5_PROXY_ENABLED`, `MT5_PROXY_*`) — those are **not** bound in `LiveMt5Registration`.

---

## 6. Key names required — Starwave direct

Starwave `NativeMt5Options.ProxyEnabled` is **hardcoded `false`**.  
`MT5_STARWAVEFX_PROXY_ENABLED` is **not** read.

| Key | Role | If missing |
|---|---|---|
| `MT5_STARWAVEFX_PASSWORD` | Manager password; **gate** | Startup throw |
| `MT5_STARWAVEFX_SERVER` | Manager host / IP | `""` → connect fails |
| `MT5_STARWAVEFX_LOGIN` | Manager login | `0` → connect fails |
| `MT5_STARWAVEFX_PORT` | Manager port | default **443** |

**Required set for Starwave direct:**  
`MT5_STARWAVEFX_SERVER`, `MT5_STARWAVEFX_PORT`, `MT5_STARWAVEFX_LOGIN`, `MT5_STARWAVEFX_PASSWORD`.

**Not read by C# Starwave connector:**  
`MT5_STARWAVEFX_DISPLAY_NAME`, `MT5_STARWAVEFX_PROVISIONING_ENABLED`, `MT5_STARWAVEFX_MODE`, `MT5_STARWAVEFX_SERVER_NAME`, `MT5_STARWAVEFX_POOL_SIZE`, `MT5_STARWAVEFX_PROXY_ENABLED`.

---

## 7. Key names required — FIX logon

`CTraderFixLogonHostedService` (registered from DI on **API and both workers**).

### 7.1 Gate (skip, not throw)

| Key | Role |
|---|---|
| `CTRADER_FIX_PASSWORD` | FIX tag 554. If empty or contains `<SECRET>`, service logs warning and **does not connect** |

### 7.2 Read for the Logon (defaults exist in code)

| Key | Used as | Code default if unset |
|---|---|---|
| `CTRADER_FIX_HOST` | TLS target + TCP host | host string in source |
| `CTRADER_FIX_ACCOUNT_ID` | logged / persist label only (not passed as Username) | numeric default in source |
| `CTRADER_FIX_QUOTE_SENDER_COMP_ID` | tag 49 **and** Username (553) for **both** sessions | CompID default in source |
| `CTRADER_FIX_QUOTE_TARGET_COMP_ID` | tag 56 for **both** sessions | `cServer` |
| `CTRADER_FIX_QUOTE_SENDER_SUB_ID` | QUOTE tag 50 | `QUOTE` |
| `CTRADER_FIX_QUOTE_TARGET_SUB_ID` | QUOTE tag 57 | `QUOTE` |
| `CTRADER_FIX_TRADE_SENDER_SUB_ID` | TRADE tag 50 | `TRADE` |
| `CTRADER_FIX_TRADE_TARGET_SUB_ID` | TRADE tag 57 | `TRADE` |

Ports are **not** env-driven: QUOTE **5211**, TRADE **5212** hardcoded.

### 7.3 Not read by logon (operator / docs / nested JSON)

`CTRADER_FIX_USE_SSL`, `CTRADER_FIX_QUOTE_SSL_PORT`, `CTRADER_FIX_QUOTE_PLAIN_PORT`, `CTRADER_FIX_TRADE_SSL_PORT`, `CTRADER_FIX_TRADE_PLAIN_PORT`, `CTRADER_FIX_TRADE_SENDER_COMP_ID`, `CTRADER_FIX_TRADE_TARGET_COMP_ID`, `CTRADER_FIX_QUOTE_SESSION_QUALIFIER`, `CTRADER_FIX_TRADE_SESSION_QUALIFIER`, `CTRADER_FIX_ENABLED`, `CTRADER_FIX_QUOTE_ENABLED`, `CTRADER_FIX_TRADE_SESSION_ENABLED`, `REAL_COPY_EXECUTION_ENABLED`.

`apps/fix-worker/Worker.cs` reads **`CTrader:RealCopyExecutionEnabled`** (nested), **not** `REAL_COPY_EXECUTION_ENABLED`. That flag only logs; worker still stamps `Disconnected` and does not send `35=D`.

**Minimum to attempt FIX logon:** `CTRADER_FIX_PASSWORD` plus (recommended, not gated) `CTRADER_FIX_HOST`, `CTRADER_FIX_QUOTE_SENDER_COMP_ID`, `CTRADER_FIX_QUOTE_TARGET_COMP_ID`, four SubID keys. Username on the wire is the **SenderCompID**, not `CTRADER_FIX_ACCOUNT_ID`.

---

## 8. Key-name catalog on disk (names only)

### 8.1 `D:\Prop\.env` (gitignored; values **not** reproduced)

Achiever / Starwave / FIX / DB names observed as assignment keys:

`MT5_SERVER`, `MT5_PORT`, `MT5_LOGIN`, `MT5_PASSWORD`, `MT5_DEFAULT_GROUP`, `MT5_MODE`, `MT5_POOL_SIZE`, `MT5_SERVER_NAME`, `ACHIEVER_EGRESS_IP`, `ACHIEVER_PROXY_ENABLED`, `ACHIEVER_PROXY_HOST`, `ACHIEVER_PROXY_PORT`, `ACHIEVER_PROXY_USERNAME`, `ACHIEVER_PROXY_PASSWORD`, `MT5_STARWAVEFX_DISPLAY_NAME`, `MT5_STARWAVEFX_PROVISIONING_ENABLED`, `MT5_STARWAVEFX_MODE`, `MT5_STARWAVEFX_SERVER`, `MT5_STARWAVEFX_PORT`, `MT5_STARWAVEFX_LOGIN`, `MT5_STARWAVEFX_PASSWORD`, `MT5_STARWAVEFX_SERVER_NAME`, `MT5_STARWAVEFX_POOL_SIZE`, `MT5_STARWAVEFX_PROXY_ENABLED`, `MT5_GROUP_2STEP_DEMO`, `MT5_GROUP_1STEP_DEMO`, `MT5_GROUP_2STEP_REAL`, `MT5_GROUP_1STEP_REAL`, `MT5_GROUP_INSTANT_REAL`, `MT5_GROUP_CORE_DEMO`, `MT5_GROUP_CORE_REAL`, `MT5_GROUP_PASSFIRST_DEMO`, `MT5_GROUP_PASSFIRST_REAL`, `CTRADER_FIX_HOST`, `CTRADER_FIX_ACCOUNT_ID`, `CTRADER_FIX_PASSWORD`, `CTRADER_FIX_USE_SSL`, `CTRADER_FIX_QUOTE_SSL_PORT`, `CTRADER_FIX_QUOTE_PLAIN_PORT`, `CTRADER_FIX_QUOTE_SENDER_COMP_ID`, `CTRADER_FIX_QUOTE_TARGET_COMP_ID`, `CTRADER_FIX_QUOTE_SESSION_QUALIFIER`, `CTRADER_FIX_QUOTE_SENDER_SUB_ID`, `CTRADER_FIX_QUOTE_TARGET_SUB_ID`, `CTRADER_FIX_TRADE_SSL_PORT`, `CTRADER_FIX_TRADE_PLAIN_PORT`, `CTRADER_FIX_TRADE_SENDER_COMP_ID`, `CTRADER_FIX_TRADE_TARGET_COMP_ID`, `CTRADER_FIX_TRADE_SESSION_QUALIFIER`, `CTRADER_FIX_TRADE_SENDER_SUB_ID`, `CTRADER_FIX_TRADE_TARGET_SUB_ID`, `CTRADER_FIX_ENABLED`, `CTRADER_FIX_QUOTE_ENABLED`, `CTRADER_FIX_TRADE_SESSION_ENABLED`, `REAL_COPY_EXECUTION_ENABLED`, `DATABASE_URL`, `REDIS_URL`, `ASPNETCORE_ENVIRONMENT`, `MT5_VOLUME_SCALE`, `LOG_LEVEL`, `LOG_FORMAT`, `RISK_MAX_DAILY_LOSS_PCT`, `RISK_MAX_TOTAL_LOSS_PCT`, `RISK_MAX_POSITION_SIZE_LOTS`, `RISK_MAX_OPEN_POSITIONS`, `RISK_MAX_DAILY_TRADES`, `RISK_SLIPPAGE_TOLERANCE_POINTS`, `RISK_COPY_MIN_DELAY_MS`, `RISK_COPY_MAX_DELAY_MS`, `RISK_EMERGENCY_FLATTEN_ENABLED`, `RISK_KILL_SWITCH_ENABLED`, `FEATURE_COPY_TRADING_ENABLED`, `FEATURE_CTRADER_HEDGING_ENABLED`, `FEATURE_ML_SCORING_ENABLED`, `FEATURE_NEWS_FILTER_ENABLED`, `FEATURE_TRADE_RECONSTRUCTION_ENABLED`, `MT5_PASSWORD_ENCRYPTION_KEY`, `USE_REAL_MT5`, `USE_DEMO_DATA`.

Product C# currently binds only the subsets in §§5–7 plus `DATABASE_URL` / `ConnectionStrings:TraderIntelligence`. `USE_REAL_MT5` has **zero** C# hits.

### 8.2 `D:\Prop\mt5-sdk\.env.example` (C++ SDK — different surface)

`MT5_MODE`, `MT5_SERVER`, `MT5_PORT`, `MT5_LOGIN`, `MT5_PASSWORD`, `MT5_DEFAULT_GROUP`, `MT5_SERVER_NAME`, `MT5_POOL_SIZE`, `MT5_GROUP_*` (plan paths), `IS_MT5_PROXY_ENABLED`, `MT5_PROXY_TYPE`, `MT5_PROXY_ADDRESS`, `MT5_PROXY_PORT`, `MT5_PROXY_LOGIN`, `MT5_PROXY_PASSWORD`, `MT5_REMOTE_URL`, `MT5_API_KEY`, `MT5_HTTP_TIMEOUT_MS`, `MT5_HTTP_POOL_SIZE`, `MT5_HTTP_POOL_ACQUIRE_TIMEOUT_MS`, `MT5_PASSWORD_ENCRYPTION_KEY`, `DATABASE_URL`, `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`, `DB_POOL_SIZE`, `LOG_LEVEL`, `LOG_FORMAT`.

No `MT5_STARWAVEFX_*`, no `ACHIEVER_PROXY_*`, no `CTRADER_FIX_*`.

---

## 9. Host matrix

| Host | First statement | Loads `D:\Prop\.env`? | MT5 connectors | FIX logon hosted service |
|---|---|---|---|---|
| `apps/api` | `WebApplication.CreateBuilder(args)` | **No** | Yes (DI) | Yes (DI) |
| `apps/mt5-worker` | `Host.CreateApplicationBuilder(args)` | **No** | Yes (DI) | Yes (DI) |
| `apps/fix-worker` | `Host.CreateApplicationBuilder(args)` | **No** | Yes (DI) | Yes (DI); Worker itself does not logon |

---

## 10. Wiring gap (honest)

1. Loader exists (`EnvFile`) but **no host calls it before CreateBuilder**.
2. API JSON uses nested `CTraderFix:*` and `ConnectionStrings:Postgres`; runtime reads `CTRADER_FIX_*` and `DATABASE_URL` / `TraderIntelligence`.
3. Achiever proxy keys are `ACHIEVER_PROXY_*`. Starwave is forced direct.
4. `DATABASE_URL` containing `<SECRET>` (or unset) ⇒ **in-memory**.
5. Without process-env `MT5_PASSWORD` **and** `MT5_STARWAVEFX_PASSWORD`, the API **does not start**.

**Required key-name cheat sheet (product C#, no values):**

```
# Achiever + proxy
MT5_SERVER
MT5_PORT
MT5_LOGIN
MT5_PASSWORD
ACHIEVER_PROXY_ENABLED
ACHIEVER_PROXY_HOST
ACHIEVER_PROXY_PORT
ACHIEVER_PROXY_USERNAME
ACHIEVER_PROXY_PASSWORD

# Starwave direct
MT5_STARWAVEFX_SERVER
MT5_STARWAVEFX_PORT
MT5_STARWAVEFX_LOGIN
MT5_STARWAVEFX_PASSWORD

# FIX logon (password is the only hard gate)
CTRADER_FIX_PASSWORD
CTRADER_FIX_HOST
CTRADER_FIX_QUOTE_SENDER_COMP_ID
CTRADER_FIX_QUOTE_TARGET_COMP_ID
CTRADER_FIX_QUOTE_SENDER_SUB_ID
CTRADER_FIX_QUOTE_TARGET_SUB_ID
CTRADER_FIX_TRADE_SENDER_SUB_ID
CTRADER_FIX_TRADE_TARGET_SUB_ID

# Postgres (else in-memory)
DATABASE_URL
# or ConnectionStrings:TraderIntelligence
```
