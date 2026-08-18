# E037 — FIX host in options: architecture host, **no password**

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\E037_fixhost.md` |
| Agent | E037 (FIX host in `CTraderFixOptions` only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:52:12+05:30 (hashes / env names) / 2026-08-18T08:23:52Z (live `/api/health` utc) |
| Host | `DESKTOP-FQPFPKE` / user `ADMIN` / India Standard Time |
| Workspace | `D:\Prop` |
| Assigned | FIX host in options. **No password.** Write this file. **Do not modify product source.** |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Config / `.env` / `appsettings` / user-secrets edited | **No.** |
| Secret values printed | **None.** Password slots classified by token / length only. |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback…`) |
| Binding law | Architecture v2 **§25** (host), **§26** (do not guess; do not treat a hostname as Logon), **§41** / **§56** (`CTRADER_FIX_HOST`, `CTRADER_FIX_PASSWORD=<SECRET>`), **§55** (never commit / expose FIX password). A25 §3.6; A31 (host is form-issued); A75 env catalog; A101 item 1; C43 (Logon **NOT PROVEN**). |
| Siblings (do not treat as this snapshot) | B25 (API `CTrader:Host` **stale** — that JSON is gone), D05 (options inventory), D40 / E001 (`.env` + process password **names**), E008 (seeded host + `Disconnected`), E002 / E016 (send off), C43 / D43 |
| Method | Full read of `CTraderFixOptions.cs`, `CTraderQuoteService.cs`, `DemoSeeder` FIX rows, `FixSessionState`, `EfDashboardQueries.GetFixSessionsAsync`, `DashboardModels.FixSessionDto`, `DependencyInjection`, `apps/api` `Program.cs` + `appsettings*.json` + `SettingsController`, `apps/fix-worker` Program/Worker/appsettings, `apps/mt5-worker` appsettings, Settings + FIX pages, architecture §25/§56. `Get-FileHash SHA256` + `git hash-object` + `git diff` of the options POCO. Process / User / Machine env **names** only for `CTRADER_FIX_HOST` / `CTRADER_FIX_PASSWORD` / nested `__Host`/`__Password`. Classify gitignored `.env` FIX keys without reprinting secret values. Live `GET /api/fix/sessions`, `/api/settings`, `/api/health` against already-running Kestrel. Did **not** `dotnet run`, did **not** open TLS, did **not** send `35=A`. |

**Honesty rule:** a hostname in a POCO is **not** a FIX session. An empty `Password` is **not** a proven secret store. A gitignored `CTRADER_FIX_HOST` line is **not** bound to `CTraderFixOptions.Host`. Live dashboard `host` is the **seeder literal**, not a resolver over options. Official RoE does **not** publish a global host; `fix.ctrader.com` in API JSON is **not** the operator sheet.

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

This is a **read-only host/password recensus**. It does not rewrite `CTraderFixOptions`, bind `IOptions<>`, or invent a password.

---

## 0. Verdict (binding)

**The FIX host in options is `live-us-eqx-01.p.c-trader.com`. The password in options is empty. No live password is present on this process.**

| Assigned claim | Measured result |
|---|---|
| FIX host in `CTraderFixOptions` | **`live-us-eqx-01.p.c-trader.com`** (C# initializer, line 10). Same string as architecture §25 / §56 and the gitignored `.env` `CTRADER_FIX_HOST` line. |
| Password in options | **`string.Empty`** (line 20). Comment: “Must never be logged.” **No** compiled secret. |
| AccountId in options | **`string.Empty`** (line 15). Not a password. |
| Is `CTraderFixOptions` bound from JSON / env? | **No.** Zero `Configure<CTraderFixOptions>`, zero `IOptions<CTraderFixOptions>`, zero `GetSection("CTrader")` / `GetSection("CTraderFix")`. `AddTraderIntelligence` does not register the type. |
| Does anyone **read** `Host` or `Password` at runtime? | **No product reader.** `CTraderQuoteService` takes the POCO and uses only `Quote` (null-guard) + `MaxQuoteAgeMs`. Worker reads **only** `CTrader:RealCopyExecutionEnabled` (different key). |
| Process `CTRADER_FIX_PASSWORD` | **Absent** (Process / User / Machine). |
| Process `CTRADER_FIX_HOST` | **Absent** (Process / User / Machine). Options default is what a `new CTraderFixOptions()` would use. |
| User-secrets | `%APPDATA%\Microsoft\UserSecrets` **does not exist**. |
| Password on Settings / FIX pages / `FixSessionDto` | **None.** Entity has **no** password column. Live `/api/settings` has **no** host and **no** password. |
| Live `/api/fix/sessions` host | **`live-us-eqx-01.p.c-trader.com`** on **5211** / **5212**, `loggedOn=false`, `connected=false`. Seeded, not from options bind. |
| Does this prove QUOTE/TRADE Logon? | **No** (`C43`). Host literal ≠ TLS ≠ `35=A`. |
| Product source edited? | **No.** |

| Slice | Class |
|---|---|
| `CTraderFixOptions.Host` default vs §25 | **EXISTS_AND_GOOD** as the **operator-sheet hostname string** |
| `CTraderFixOptions.Password` default | **EXISTS_AND_GOOD** as an **empty slot** (no committed secret) |
| Options ↔ host config binder | **MISSING** |
| Flat `CTRADER_FIX_HOST` → `Host` map (A75 §9) | **MISSING** |
| API `CTraderFix:QuoteHost` / `TradeHost` = `fix.ctrader.com` | **EXISTS_NEEDS_REFACTOR** — **wrong key names**, **unofficial host**, **plain ports 5201/5202**, **unbound** |
| Worker / mt5-worker `appsettings` FIX host | **MISSING** (logging stubs only) |
| Live password in options / env / user-secrets / JSON | **ABSENT** (slot empty or `<SECRET>` placeholder) |
| Live FIX Logon | **NOT PROVEN** |
| Live `NewOrderSingle` | **SAFE_BY_ABSENCE** (orthogonal; E002 / E016) |

One-liner:

```text
OPTIONS.HOST = live-us-eqx-01.p.c-trader.com
OPTIONS.PASSWORD = ""
NO PROCESS / USER-SECRETS PASSWORD
HOST IS UNBOUND — NOT A SESSION
API JSON HOST fix.ctrader.com IS A DEAD, UNOFFICIAL ALIAS
```

Do **not** treat this file as A101 item 1. Do **not** paste a password “to make local work.” Do **not** bind `CTraderFix:QuoteHost` onto `CTraderFixOptions.Host` without renaming — the property is a **single** `Host`, not Quote/Trade split hosts.

---

## 1. Method (read-only)

1. Read `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` in full (authoritative options bag).
2. `git show HEAD:…CTraderFixOptions.cs` + `git diff` — confirm **Host / Password were not part of the dirty hunk**.
3. Read the only constructor consumer (`CTraderQuoteService`) and confirm it never touches `Host` / `Password` / `AccountId`.
4. Read seeder FIX rows, `FixSessionState`, dashboard DTO + query (host is **copied from the row**, not from options).
5. Read host JSON: `apps/api/appsettings.json` `CTraderFix` section; worker appsettings; `docker-compose.yml` (no FIX keys).
6. `git show HEAD:apps/api/appsettings.json` — HEAD had **no** `CTrader` / `CTraderFix` block.
7. Env **names** only: Process / User / Machine for `CTRADER_FIX_HOST`, `CTRADER_FIX_PASSWORD`, `CTrader__Host`, `CTraderFix__Host`, `CTraderFix__Password`, `CTrader__Password`, plus adjacent flag names.
8. Classify `D:\Prop\.env` FIX keys (gitignored). Password value **discarded** after token class.
9. Confirm user-secrets root absent.
10. Live GET already-running API `:5000` `/api/fix/sessions`, `/api/settings`, `/api/health`. Did not start or kill Kestrel.
11. Official pin: A31 — cTrader Help **does not** publish a single global host; credentials form issues the host. Architecture §25 is the lab operator sheet for **this** Pepperstone account.

No file under `src/`, `apps/`, `tests/`, or `mt5-sdk/` was written, deleted, or restored.

---

## 2. Options POCO as measured

Path: `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`  
**2344** bytes, **80** physical / **55** non-blank, SHA-256 `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308`  
git blob `f2cd089d29304a3e107dbc1e58957421a65296d6` — worktree **`M`** vs HEAD.  
LastWriteTimeUtc `2026-08-18T07:42:48.0601582Z`.

SHA matches E002 / E016. Dirty hunk is **only** `TargetCompId` `CSERVER` → `cServer` on Quote + Trade (C09 / C21 surface). **`Host` and `Password` are byte-identical to HEAD.**

```10:20:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    public string Host { get; set; } = "live-us-eqx-01.p.c-trader.com";

    /// <summary>
    /// FIX username (AccountId). Must never be logged.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// FIX password. Must never be logged.
    /// </summary>
    public string Password { get; set; } = string.Empty;
```

| Property | Default (this snapshot) | Secret? | Notes |
|---|---|---|---|
| `Host` | `live-us-eqx-01.p.c-trader.com` | **No** (identifier) | Shared QUOTE+TRADE gateway (§25). Not QuoteHost/TradeHost. |
| `AccountId` | `""` | Identifier if filled | Logon tag 553 when a binder exists. Empty today. |
| `Password` | `""` | **Yes, if filled** | Logon tag 554. **Empty.** Must never be logged. |
| `UseSsl` | `true` | No | Production transport law. |
| `Quote.SslPort` / `PlainPort` | **5211** / 5201 | No | SSL is the production pair. |
| `Trade.SslPort` / `PlainPort` | **5212** / 5202 | No | |
| `Quote.SenderCompId` / `Trade.SenderCompId` | `live.pepperstone.1369850` | Identifier | Live CompID compiled in (B25 leak surface; not a password). |
| `Quote.TargetCompId` / `Trade.TargetCompId` | **`cServer`** (worktree) | No | HEAD was `CSERVER`. Unproven on the wire. |
| `Quote.TargetSubId` / `Trade.TargetSubId` | `QUOTE` / `TRADE` | Protocol | |
| `Quote.SenderSubId` / `Trade.SenderSubId` | `""` | Issued if filled | Configurable; empty. |
| `QuoteEnabled` / `TradeSessionEnabled` | `true` / `true` | No | Connect-without-trading (§41). |
| `RealCopyExecutionEnabled` | **`false`** | No | Send floor. Unrelated to host. |
| `HeartbeatIntervalSec` | `30` | No | |
| `MaxQuoteAgeMs` | `5000` | No | Only field `CTraderQuoteService` actually reads. |

`new CTraderFixOptions()` therefore has a **live Pepperstone host** and **no password**. That is the assigned “FIX host in options / no password” state.

---

## 3. Host is unbound (config does not win)

### 3.1 No DI registration

`D:\Prop\src\Infrastructure\DependencyInjection.cs` (1900 B, SHA-256 `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380`) registers EF + Fake MT5 + dashboard/ingest only. **No** `services.Configure<CTraderFixOptions>`. **No** `AddSingleton<CTraderFixOptions>`.

Product `*.cs` hits for `CTraderFixOptions` / `Configure<CTrader` / `IOptions<CTrader`:

| File | Role |
|---|---|
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | type definition |
| `src/Fix.CTrader/Services/CTraderQuoteService.cs` | constructor parameter |

`CTraderQuoteService` (5453 B, SHA-256 `7D2FDE1D33B47D619EA8BB0EC5F943BC21D8D97B46BEA269D70D46A20859B44A`) is **not** registered in DI. Grep of that file for `_options.Host` / `_options.Password` / `_options.AccountId`: **0**. Host in options is **dead data** until a session factory exists.

### 3.2 Architecture env name will not bind by default

A75 §9: flat `CTRADER_FIX_HOST` does **not** map to `CTraderFixOptions.Host` under default ASP.NET nested binding. Expected nested aliases (`CTraderFix__Host`, `CTrader:Host`) are also **absent** from this process.

`apps/fix-worker/Worker.cs` L21:

```csharp
var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
```

That is the **only** host-process `CTrader:*` read. It does **not** load `Host` or `Password`. Worker `appsettings.json` / `appsettings.Development.json` are logging-only (137 B, SHA-256 `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33`). Same hash on mt5-worker stubs.

### 3.3 Process / user-secrets (names only)

| Name | Process | User | Machine |
|---|---|---|---|
| `CTRADER_FIX_HOST` | **absent** | **absent** | **absent** |
| `CTRADER_FIX_PASSWORD` | **absent** | **absent** | **absent** |
| `CTRADER_FIX_ACCOUNT_ID` | absent | absent | absent |
| `CTrader__Host` / `CTraderFix__Host` | absent | absent | absent |
| `CTrader__Password` / `CTraderFix__Password` | absent | absent | absent |
| `REAL_COPY_EXECUTION_ENABLED` | absent | absent | absent |

User-secrets root: **does not exist.**

Consequence: even if a binder landed tomorrow, **this process** would still see POCO defaults (`Host` = live EQX, `Password` = empty) unless a file or secret store supplied overrides.

---

## 4. Adjacent host surfaces (do not confuse with options)

### 4.1 Architecture operator sheet (§25 / §56)

```env
CTRADER_FIX_HOST=live-us-eqx-01.p.c-trader.com
```

Same hostname as `CTraderFixOptions.Host`. Password on that sheet is `<SECRET>` — **not** copied into the POCO.

Official Help (A31, https://help.ctrader.com/fix/getting-credentials/): host comes from **Settings → FIX API**, not a global constant. `*.p.c-trader.com` is the issued pattern. **`fix.ctrader.com` is not on that sheet and is not official RoE.**

### 4.2 Gitignored `D:\Prop\.env` (not bound)

Present: **3408** B, SHA-256 `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA` (same as E001 / D40).  
`D:\Prop\.env.example` is **absent** on disk (` D` vs HEAD).

FIX-related **key classes** (values after `PASSWORD` / secret slots discarded):

| Key | Class |
|---|---|
| `CTRADER_FIX_HOST` | **HOST_LITERAL=`live-us-eqx-01.p.c-trader.com`** |
| `CTRADER_FIX_PASSWORD` | **PLACEHOLDER_SECRET** (`<SECRET>`) — **not** an operator password |
| `CTRADER_FIX_ACCOUNT_ID` | identifier, length 7 (matches `1369850` width; value not re-printed as a secret) |
| `CTRADER_FIX_*_PORT` / `USE_SSL` / `*_ENABLED` | non-secret flags / published ports |
| `CTRADER_FIX_*_SENDER_SUB_ID` / `*_TARGET_SUB_ID` | `<BROKER_ISSUED_VALUE>` |
| `REAL_COPY_EXECUTION_ENABLED` | `false` |

This file is **not** loaded by `fix-worker` / API (`Program.cs` has no dotenv). Host agreement with options is **textual**, not runtime.

### 4.3 API `appsettings.json` — **different host, dead schema**

Path: `D:\Prop\apps\api\appsettings.json`  
**1254** B, SHA-256 `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20`  
Worktree **`M`**. HEAD was logging + `AllowedHosts` only (no FIX block).

Current `CTraderFix` object (measured):

| JSON key | Value | vs `CTraderFixOptions` |
|---|---|---|
| `QuoteHost` | **`fix.ctrader.com`** | **No such property.** Options has one `Host`. **Unofficial hostname.** |
| `TradeHost` | **`fix.ctrader.com`** | same miss |
| `QuotePort` | **5201** | Options production default is **SSL 5211** |
| `TradePort` | **5202** | Options production default is **SSL 5212** |
| `SenderCompId` | `""` | Options default is `live.pepperstone.1369850` |
| `TargetCompId` | `CSERVER` | Worktree options = `cServer` |
| `HeartBeatInterval` | `30` | Name mismatch (`HeartbeatIntervalSec`) |
| `ResetOnLogon` / `FileStorePath` / `FileLogPath` | present | **No matching options properties** |
| `Password` | **key absent** | Options slot exists and is empty |
| `Host` | **key absent** | |

`appsettings.Development.json` has **no** `CTraderFix` override (478 B, SHA `81B5E6DC…`).

B25 §3 quoted a `CTrader:Host = live-us-eqx-01.p.c-trader.com` + empty `CTrader:Password` block. **That JSON is gone.** Treat B25’s API host table as **historical**. Current committed-shape JSON uses **`CTraderFix` + `fix.ctrader.com`** and would **not** bind to `CTraderFixOptions` even if `Configure<>` were added under section `"CTraderFix"` (property names do not match).

No password key in any host `appsettings*.json`. The only JSON “secret-shaped” key under `apps/` is `RiskEngine.EmergencyFlattenApiKey` = **empty** (`LEN=0`).

`docker-compose.yml` (687 B, SHA `1ED8787F…`): **no** `CTRADER_*` / `CTrader` environment.

### 4.4 Seeded / live dashboard host (not options)

`DemoSeeder` (5082 B, SHA-256 `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20`) writes **both** sessions:

| Session | Host | Port | Status | Password column |
|---|---|---|---|---|
| QUOTE | `live-us-eqx-01.p.c-trader.com` | 5211 | `Disconnected` | **none** (`FixSessionState` has no password) |
| TRADE | `live-us-eqx-01.p.c-trader.com` | 5212 | `Disconnected` | **none** |

Same hostname as options. **Hardcoded in the seeder**, not `new CTraderFixOptions().Host`.

Live `GET http://127.0.0.1:5000/api/fix/sessions` (HTTP **200**, 881 B) this pass:

| qualifier | host | port | connected | loggedOn | status | executionEnabled | password field |
|---|---|---:|---|---|---|---|---|
| QUOTE | `live-us-eqx-01.p.c-trader.com` | 5211 | false | false | `Disconnected` | false | **absent** |
| TRADE | `live-us-eqx-01.p.c-trader.com` | 5212 | false | false | `Disconnected` | false | **absent** |

`lastError` admits no live socket (QUOTE: `"No live QUOTE socket. Demo seed only."`). Bid/ask `2399.45` / `2399.85` are the **forged demo book** (E008), not a QUOTE stream.

`GET /api/settings` → flags + broker **names** only. **No host. No password.**  
`GET /api/health` → `fixSessions[0].healthy=false`, details `"no live TLS socket"`.

`FixSessionsPage.tsx` prints `s.host:s.port` and the caption *“Password is never shown.”* That is **true by schema** (`FixSessionDto` has `Host`, not `Password`). Settings page is a `<pre>` of the stub GET — no host editor, no password input.

---

## 5. Host matrix (single source of truth for E037)

| Surface | Host key | Host value | Password | Bound to options? | Session? |
|---|---|---|---|---|---|
| `CTraderFixOptions` C# default | `Host` | `live-us-eqx-01.p.c-trader.com` | `""` | **is** the POCO | **No** |
| Architecture §25 / §56 | `CTRADER_FIX_HOST` | same | `<SECRET>` | No binder | No |
| `D:\Prop\.env` (gitignored) | `CTRADER_FIX_HOST` | same | placeholder `<SECRET>` | **Not loaded** | No |
| Process env | `CTRADER_FIX_HOST` | **absent** | **absent** | — | No |
| User-secrets | — | **absent** | **absent** | — | No |
| API `appsettings.json` | `CTraderFix:QuoteHost` / `TradeHost` | **`fix.ctrader.com`** | key absent | **No** (name + shape miss) | No |
| fix-worker / mt5-worker appsettings | — | **none** | none | — | No |
| `DemoSeeder` / InMemory row | `FixSessionState.Host` | `live-us-eqx-01.p.c-trader.com` | no column | No | **No** (enum `Disconnected`) |
| Live `/api/fix/sessions` | DTO `host` | same as seeder | not serialized | No | **No** (`loggedOn=false`) |
| Settings GET | — | not returned | not returned | — | — |
| Compose | — | none | none | — | No |

**Authoritative options host = architecture host.**  
**Authoritative options password = empty.**  
**`fix.ctrader.com` is a stray, unbound JSON alias. Do not promote it.**

---

## 6. What “no password” does and does not mean

**Means (measured):**

1. `CTraderFixOptions.Password` initializer is `string.Empty`.
2. No `Password` key under API `CTraderFix`.
3. `FixSessionState` / `FixSessionDto` / Settings live JSON cannot carry a FIX password.
4. This process has no `CTRADER_FIX_PASSWORD` in Process / User / Machine.
5. `.env` password **slot** is the placeholder token `<SECRET>`, not an operator secret (E001 class).
6. User-secrets store is not on disk.
7. UI copy “Password is never shown” is backed by **absence of a field**, not a redactor.

**Does not mean:**

- A production secret store is wired (it is **not**).
- Logon can succeed without a password (RoE Logon requires tag **554**). Empty password ⇒ diagnostic Logon would **fail** if a socket existed.
- The tree is “secret-free.” Live **identifiers** (host, CompID, account width-7 id in `.env`) remain (B25 / A19-02). Those are **not** passwords.
- Operators may commit a real password later into appsettings. **Forbidden** (§55). Password belongs in env / user-secrets / Vault, never React `VITE_*`.

---

## 7. File census (this check)

| Path | Bytes | SHA-256 | Role |
|---|---:|---|---|
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | 2344 | `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` | **options host + empty password** |
| `src/Fix.CTrader/Services/CTraderQuoteService.cs` | 5453 | `7D2FDE1D33B47D619EA8BB0EC5F943BC21D8D97B46BEA269D70D46A20859B44A` | holds options; ignores Host/Password |
| `src/Infrastructure/Seeding/DemoSeeder.cs` | 5082 | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | seeds same host, no password |
| `src/Infrastructure/DependencyInjection.cs` | 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | no options bind |
| `src/Infrastructure/Dashboard/EfDashboardQueries.cs` | 8708 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | copies row.Host to DTO |
| `src/Application/Dashboard/DashboardModels.cs` | 3088 | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` | `FixSessionDto.Host`, no password |
| `src/Domain/Entities/FixSessionState.cs` | 979 | `46C20D6A1BF5F84769DB483FD17A0EBEB8BDA8C1C56BBA2B8B30A59FCE44697E` | Host column; no password |
| `apps/api/appsettings.json` | 1254 | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` | stray `fix.ctrader.com` |
| `apps/api/appsettings.Development.json` | 478 | `81B5E6DC0290CB48038DD67C6F9C37851C16F8362A6350BD1A43D9B27E8B0481` | no FIX |
| `apps/api/Program.cs` | 4731 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | settings stub; no host |
| `apps/api/Controllers/SettingsController.cs` | 3732 | `B19274DC71F6BECB54B6C1A270D3CA9F47C9B1CCD340D0C303F4C663CA50C23F` | unmapped; no host/password |
| `apps/fix-worker/appsettings.json` | 137 | `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` | logging only |
| `apps/fix-worker/Worker.cs` | 2093 | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` | flag only |
| `apps/web/src/pages/FixSessionsPage.tsx` | 1312 | `EC93326688719E10D3ED5CB275D9BF1E7113C7F61EEA99803F42E1EA268BB886` | shows host:port |
| `apps/web/src/pages/SettingsPage.tsx` | 459 | `57D41B908C591238ACD375E62EA870E0B373B168F53D036137A045AC91CE03F4` | no host form |
| `docker-compose.yml` | 687 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` | no FIX env |
| `D:\Prop\.env` | 3408 | `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA` | host literal; password placeholder |

---

## 8. Stale-vs-this-file

| Earlier claim | This snapshot |
|---|---|
| B25: API `CTrader:Host` = live EQX, `CTrader:Password` = `""` | **Stale JSON.** Now `CTraderFix:QuoteHost/TradeHost` = **`fix.ctrader.com`**, no password key. |
| C43: “Host is a C# / JSON / `.env.example` string. Unbound in fix-worker.” | **Still true** for C# + `.env`. `.env.example` is **deleted** on disk. API JSON host **diverged**. |
| A75: options ships live host + CompID | **Still true.** Password still empty. Binder still missing. |
| E008: seeder host live EQX, `Disconnected` | **Still true.** Live API confirms. |
| E001: process has no `CTRADER_FIX_PASSWORD` | **Still true.** Re-measured. |
| “We set the FIX host, so we can Logon.” | **False.** Unbound + empty password + no initiator. |

---

## 9. Authorized later work (do **not** apply in E037)

When a coding wave is authorized:

1. Keep `CTraderFixOptions.Host` as the **single** shared gateway host (QUOTE and TRADE share the host; ports differ). Do not invent `QuoteHost`/`TradeHost` unless RoE/form actually issues two hostnames.
2. Bind with an **explicit map** (A75 §9): `CTRADER_FIX_HOST` → `Host`. Do not assume `CTraderFix:QuoteHost` will land.
3. **Delete or replace** API `fix.ctrader.com` before any bind — that hostname is not the operator sheet and uses **plain** ports.
4. Keep `Password` out of committed JSON. Empty default stays. Fill only via env / user-secrets / Vault.
5. Never log `Host` together with `Password` / tags 553–554 (A50 / A76).
6. Do not treat a successful bind of the hostname as A25 §3.6 / A101 item 1.

---

## 10. Honest limits

- Did not resolve `live-us-eqx-01.p.c-trader.com` or `fix.ctrader.com` in DNS. Did not open TCP 5211/5212/5201/5202.
- Did not send Logon. Did not print `.env` password or account id values (account slot classified by length only).
- Did not inspect other processes’ environments (a human-launched worker that dotenv’d `.env` was **not** sampled).
- Did not edit product source. Did not change options defaults.

---

## 11. Direct answers

**FIX host in options?**  
`CTraderFixOptions.Host` = **`live-us-eqx-01.p.c-trader.com`** (SHA-256 `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308`). Matches architecture §25. Unbound. Not a session.

**Password?**  
**None.** Options default `""`. No process/user-secrets password. `.env` slot is `<SECRET>`. Dashboard / DTO / entity cannot carry one. API `CTraderFix` has no password key.

**Product source edited?**  
**No.**
