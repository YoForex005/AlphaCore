# R004 — Windows CLI: `mt5-dump` via `IMT5Client` → groups/accounts/deals JSON for C# ingest

| Field | Value |
|---|---|
| Agent | R004 (senior engineer, collector CLI design only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\R004_collector.md` |
| Assigned | Design a Windows CLI that uses mt5-sdk `IMT5Client` to dump groups/accounts/deals JSON for C# ingest. Write this file. Do not modify product source. |
| Product source modified | **No.** This report is the only product of this agent. |
| Classification | PLAN — not implemented |
| Authority | Architecture v2 §§5–12, 55–56, 62, 67 Phase 1, 72.6–7 |
| Binding siblings | A04, A12, A13, A14, A16, A30 I2, A37, A38, A39, A40, A58, A59, A75, A78, A81, A84, A105, C20, C42, C47.3, D67 |

**Honesty line:** there is **no** `apps/mt5-collector` / `mt5-dump.exe` on disk. C# ingestion today is `FakeMt5BrokerConnector` + `DealIngestionService`. Live Achiever / StarwaveFX dumps are **not proven** (C42). This file is the binding design so a later coder does not invent a second JSON dialect or copy dealer verbs onto a source collector.

```text
REAL_COPY_EXECUTION_ENABLED = false     -- dump is read-only; never SendTrade
C++ mt5-sdk                             -- reuse IMT5Client; do not rewrite (C20)
C# IMt5BrokerConnector                  -- consume files; do not DllImport Manager
Product source in this agent            -- not touched
Linux / Wine / compose PE               -- forbidden (A105)
```

---

## 0. Verdict

**Build a Windows x64 batch CLI** (`mt5-dump`) that:

1. Loads `MT5APIManager64.dll` beside the exe (`mt5sdk_copy_runtime_dlls`).
2. Connects **one** Manager slot per process (`--broker=achiever|starwavefx`).
3. Talks **only** through `IMT5Client` (concrete `MT5Manager` in `MT5_MODE=local`).
4. Walks **all** manager-visible groups (`GetGroupDetails`), then logins, then users/accounts, then `GetDeals([from,to])`, then `GetPositions`.
5. Writes a versioned dump directory whose JSON deserializes 1:1 onto the **existing** C# ingest DTOs (`Mt5GroupDto` / `Mt5AccountDto` / `Mt5DealDto` / `Mt5PositionDto`).
6. Exits non-zero and stamps `manifest.complete=false` if any required `IMT5Client` call returns `false` (incomplete history ≠ empty).

**This is the first increment of C47.3**, not a competing stack:

| Surface | Role | When |
|---|---|---|
| `mt5-dump` (this file) | Offline / operator batch: connect → snapshot → files | Now (lab proof + C# ingest without HTTP) |
| `mt5-collector serve` (C47.3) | Long-lived loopback HTTP `:9101/:9102` | After dump JSON + C# mapper are green |
| `apps/mt5-worker` | `DealIngestionService` against `IMt5BrokerConnector` | Already loops; swap Fake → File dump → HTTP |

Same read path, same JSON field law. Dump first. Serve later. Do not start a Drogon/HTTP server in this increment.

**What “done” means for the later coding wave (conjunction):**

1. `mt5-dump.exe` is a PE32+ AMD64 that copies the A105 DLL trio.
2. Against a **lab** Manager login it writes `groups.json` + `accounts.json` + `deals.jsonl` + `positions.json` + `manifest.json`.
3. `FileMt5DumpConnector` implements `IMt5BrokerConnector` and `DealIngestionService.SyncBrokerAsync` upserts at least one **non-canned** deal ticket.
4. `manifest.complete=false` is refused by the C# connector (fail closed).
5. No dealer / provision / password / balance method is referenced from the dump TU.

Until a probe log exists under `reports/swarm/` (no passwords, no customer names), C42 stays **NOT PROVEN**.

---

## 1. Measured baseline (do not re-litigate)

Re-read 2026-08-18. Product source not changed.

| Surface | Path | Measured now | Class |
|---|---|---|---|
| C++ contract | `mt5-sdk/src/core/imt5_client.h` | Transport-agnostic. Groups/deals/positions/account/user are pure virtual. Connect is **not** on the interface. | `EXISTS_AND_GOOD` |
| Local impl | `mt5-sdk/src/core/mt5_manager.{h,cpp}` | `Initialize` + `Connect` + `DealRequest` (one shot, **no** `DealRequestPage`). `GetGroupDetails` = `GroupTotal`/`GroupNext`. | `EXISTS_AND_GOOD` unused by C# |
| HTTP impl | `mt5-sdk/src/core/mt5_http_client.cpp` | `GetGroupDetails` **always false** (D67). No dump server in this repo (A16 is a *client* of a missing YoPips service). | **do not use for dump** |
| Probe template | `mt5-sdk/tests/mt5_group_probe.cpp` | Live connect, `GetAllGroups`, JSON to stdout, no password echo. `pumpMode=0`. | copy the *safety* pattern, not the schema |
| C# DTOs | `src/Application/Contracts/Mt5Contracts.cs` | `Mt5GroupDto` / `Mt5AccountDto` / `Mt5DealDto` / `Mt5PositionDto` + `IMt5BrokerConnector` | **ingest target** |
| C# ingest | `src/Application/Ingestion/DealIngestionService.cs` | groups → accounts → deals → **always** `ReplacePositionsAsync` | must dump positions or wipe them |
| C# live connector | `src/Mt5/Connectors/FakeMt5BrokerConnector.cs` | 3+1 canned groups, 4 logins, 18 deals | demo only |
| Worker | `apps/mt5-worker/Worker.cs` | 30 s Fake sync, last 30 days, hard-coded `10001…99001` | no dump reader |
| AppConfig | `mt5-sdk/config/app_config.h` | **Single** `MT5_SERVER`/`MT5_LOGIN`. Proxy keys are `IS_MT5_PROXY_ENABLED` / `MT5_PROXY_*` (not §56). | cannot attach two brokers in one process |
| nlohmann `DealData` | `mt5_types.h:335–340` | **Omits `position`.** C++ `extractDeal` fills `PositionID()`. | wire gap — dump **must not** reuse raw `to_json(DealData)` |
| `GroupDetail` JSON | `mt5_types.h` | **No** `to_json`. | dump must emit its own object |
| Volume | A13 / A38 / `VolumeConverter` | `Volume()` = lots × **10 000**. Header “hundredths” comment is wrong (A81). | dump raw `ulong`, never rescale |
| Collector host | `apps/mt5-collector/` | **MISSING** | this design |

---

## 2. Why a file CLI (not HTTP, not P/Invoke)

| Option | Reject / accept | Why |
|---|---|---|
| C# `DllImport` `MT5APIManager64.dll` | **Reject** | C20 / A14: reuse C++ `IMT5Client`. Manager API is not a stable P/Invoke surface. |
| `MT5HttpClient` → missing YoPips URL | **Reject** | `GetGroupDetails` stub (D67). No server in this repo. `Mode=remote` is a lie until `mt5-collector serve` exists. |
| Long-lived HTTP sidecar first | **Defer** | C47.3. Needs auth, bind address, process supervisor. Dump proves the *read + JSON* contract with one process that exits. |
| Write Postgres from C++ (`mt5_ledger_store`) | **Reject as SoT** | A17/A78: `server_key` ledger is a sibling, not `mt5_deals`. Phase 1 SoT is C# `ITradingStore`. |
| File dump + C# `FileMt5DumpConnector` | **Accept (this increment)** | Windows owns the DLL. C# stays portable. Same DTOs `DealIngestionService` already upserts. |

Topology (one broker per process — `AppConfig` is single-slot):

```text
Windows x64
  mt5-dump --broker=achiever    --out D:\Prop\data\dumps\<run>\ACHIEVER
  mt5-dump --broker=starwavefx  --out D:\Prop\data\dumps\<run>\STARWAVEFX
        │                              │
        │  LoadLibraryW(MT5APIManager64.dll)
        │  MT5Manager::Connect  (read-only IMT5Client verbs)
        ▼                              ▼
  ti.mt5.dump.v1 JSON directory
        │
        ▼
C# FileMt5DumpConnector : IMt5BrokerConnector
        │
        ▼
DealIngestionService.SyncBrokerAsync → EfTradingStore upserts
```

Two processes beat a C++ rewrite that loads two Manager logins in one `AppConfig`.

---

## 3. Binding laws (freeze before coding)

### 3.1 Read-only surface (hard)

The dump translation unit may **call** only these `IMT5Client` methods:

| Method | Why |
|---|---|
| `IsConnected` / `GetLastError` | health + fail reason |
| `GetServerTime` | deal window (`mt5_time_window`) |
| `GroupTotal` / `GetAllGroups` / `GetGroupDetails` / `GetGroupLogins` | discovery (A39: **all** visible groups) |
| `GetUserLogins` | alias of group logins on local impl |
| `GetUser` | group + leverage + overlay money |
| `GetAccount` | money snapshot if `GetUser` overlay misses |
| `GetDeals` | complete `[from,to]` or `false` |
| `GetPositions` | required because ingest **replaces** the position book |
| `GetRecentDeals` | optional merge for last ~40 s lag; never a substitute for `GetDeals==false` |

**Forbidden to call** (present on `IMT5Client`, YoPips dealer/admin — A04 §5.8):

```text
CreateUser  UpdateUser  DeleteUser  ChangePassword  CheckPassword
UpdateUserLeverage  UpdateUserGroup  UpdateUserRights
DealerBalance  Deposit  Withdraw  DealerSendOrder  SendTrade
CacheExecutedDeal          -- synthetic high-bit tickets; not broker evidence (A59 L14)
GroupUpdate*               -- not on IMT5Client, but do not reach IMTManagerAPI writes
```

Static review gate: `dump_command.cpp` must not contain those identifiers. A later `serve` binary may keep the same allow-list.

### 3.2 Discovery is not the plan map (A39 / A40 / §9)

```text
correct:  IMT5Client::GetGroupDetails  →  every group this manager login can see
wrong:    MT5_GROUP_2STEP_DEMO / getMt5Group(plan)  →  only those names
```

`--group` is a **post-filter** for a lab subset, never the enumerator. Default = no filter.

### 3.3 Completeness (A12 / A59 L10)

`GetDeals(...) == false` means `dependency_unavailable`. It is **not** “this login has no deals.”

- Do not write a `complete=true` shard for that login.
- Do not advance a C# `sync_checkpoints` cursor from that shard.
- Empty `out` after `true` **is** a valid zero-deal window (`MT_RET_OK_NONE` / `NOTFOUND` already mapped in `MT5Manager::GetDeals`).

Local `GetDeals` is **one** `DealRequest` (`mt5_manager.cpp:485–509`). SDK also has `DealRequestPage` / `DealRequestByGroup` (`MT5APIManager.h:520–526`) but they are **not** on `IMT5Client`. This increment **does not** reach around the interface. Document the one-shot limit in `manifest.limitations[]`. A later SDK wrap of `DealRequestPage` is a separate change to `mt5-sdk` (not this CLI’s job to fork).

### 3.4 Identity (architecture §10)

Never treat login or ticket as globally unique. Every dump file is rooted at `brokerCode`. C# persist key remains `(broker_id, deal_ticket)` (A78). Achiever ticket `1001` and StarwaveFX ticket `1001` are two rows.

### 3.5 Volume and enums

- Emit Manager `Volume()` as `volumeNative` (`ulong`). 1.00 lot = `10000`. Never `/ 100`. Never `/ 1e8` (that is `VolumeExt`, unused).
- `action` / `entry` are **raw uint** matching `IMTDeal::EnDealAction` / `EnDealEntry` (A37, `DealAction` / `DealEntry`).
- Do **not** emit `reason` in v1: `DealData` does not carry it and `Mt5DealDto` has no `Reason`. Reconstruction’s `DealReasons` stays a later extractor change.

### 3.6 Time

Windows are **MT5 server unix seconds**, resolved by `resolveMt5TimeWindow` (`mt5_time_window.h`). Do not default `to` from host clock when `GetServerTime()` is sane.

C# `Mt5DealDto.Time` is `DateTimeOffset`. Dump `time` as ISO-8601 UTC derived from that unix value (`1970-01-01Z + seconds`). Also emit `timeUnix` for audit. Do not emit a JSON number in `time` — `System.Text.Json` will not bind it to `DateTimeOffset` without a custom converter we do not have.

### 3.7 Secrets (A19 / A75 / A76)

Never write to disk or stdout: manager password, proxy password, `MT5_API_KEY`, user master/investor passwords, `name` / `email` / `phone` from `UserData`.

`--include-pii` is **off**. If an operator later needs forensics, that is a **separate** gitignored file, not the ingest dump.

Deal `comment` is broker-supplied; leave it (may contain tickets / “deposit”). Do not treat it as a secret field.

### 3.8 Config keys (A58 allow-list = architecture §56)

`--broker=achiever` binds **only**:

```text
MT5_SERVER  MT5_PORT  MT5_LOGIN  MT5_PASSWORD  MT5_SERVER_NAME  MT5_MODE
ACHIEVER_EGRESS_IP
ACHIEVER_PROXY_ENABLED  ACHIEVER_PROXY_HOST  ACHIEVER_PROXY_PORT
ACHIEVER_PROXY_USERNAME  ACHIEVER_PROXY_PASSWORD
```

`--broker=starwavefx` binds **only**:

```text
MT5_STARWAVEFX_SERVER  MT5_STARWAVEFX_PORT  MT5_STARWAVEFX_LOGIN
MT5_STARWAVEFX_PASSWORD  MT5_STARWAVEFX_SERVER_NAME  MT5_STARWAVEFX_MODE
MT5_STARWAVEFX_PROXY_ENABLED
```

Do **not** invent `Brokers:Achiever:CollectorBaseUrl`, `MT5_REMOTE_URL`, or `MT5_STARWAVEFX_PROXY_HOST` in this increment. Do **not** silently read YoPips `IS_MT5_PROXY_ENABLED` / `MT5_PROXY_*` as the primary path (those names are not §56). If both are present, §56 wins; log `proxy_source=ACHIEVER_PROXY_*`.

`MT5_MODE` must be `local`. `remote` → exit 3 (same as `mt5_group_probe`).

`AppConfig::load` is **not** sufficient (one slot + wrong proxy names). Write `DumpBrokerSlot` that reads process env then optional `--env` file. Reuse `AppConfig::parse` ideas, not the struct as-is.

---

## 4. Process, CLI, exit codes

### 4.1 Binary

```text
apps/mt5-collector/                 # CMake, not in the .sln (A30)
  CMakeLists.txt                    # WIN32 exe; target_link mt5sdk::mt5sdk
  src/main.cpp                      # argv → dump (later: serve)
  src/dump_command.{h,cpp}
  src/dump_schema.{h,cpp}           # JSON builders (do not use to_json(DealData))
  src/broker_slot.{h,cpp}           # §56 binder
  src/read_only_guard.h             # compile-time / review allow-list comment
```

CMake (sketch, implement later):

```cmake
add_executable(mt5-dump src/main.cpp src/dump_command.cpp src/dump_schema.cpp src/broker_slot.cpp)
target_link_libraries(mt5-dump PRIVATE mt5sdk::mt5sdk)
mt5sdk_copy_runtime_dlls(mt5-dump)
```

MSVC `/W3 /utf-8`. `_WIN32_WINNT=0x0A00`. No Drogon. No Postgres. `MT5SDK_WITH_POSTGRES` stays **OFF**.

At start, log (never secrets):

- absolute DLL directory
- SHA-256 of the A105 trio (pin: `51A590CD…`, `41A66C5D…`, `DB28E45E…`)
- `MTManagerAPIVersion` if the factory exposes it
- broker code, server:port, manager login, proxy enabled (bool only)
- `GetServerTime` vs host time

### 4.2 Arguments

```text
mt5-dump --broker=<achiever|starwavefx> --out=<dir> [options]

  --broker            required. Slot selector. Case-insensitive. Maps to BrokerCodes.
  --out               required. Empty or new directory. Refuse to overwrite a complete dump.
  --env               optional .env path (default: cwd .env then D:\Prop\.env). Gitignored.
  --dll-dir           optional. Default = directory of the exe (post-copy-dlls).
  --from              unix seconds or YYYY-MM-DD (start of day, server time).
  --to                unix seconds or YYYY-MM-DD (end of day if date-only). Default = GetServerTime.
  --lookback-days     used when --from omitted. Default 30 (matches Worker.cs).
  --login             repeatable. If set, skip other logins (still dump all groups).
  --group             repeatable post-filter on group name (exact, UTF-8, backslash preserved).
  --max-logins        default 50 (C47.3 first-connect cap). --max-logins=0 means no cap.
  --pump-groups       default ON. Connect with PUMP_MODE_GROUPS|PUMP_MODE_USERS.
  --merge-recent      default ON. Union GetRecentDeals into the login window (dedupe by ticket).
  --pretty            pretty-print groups/accounts/manifest. deals.jsonl stays one object/line.
  --dry-run           connect + count; write only manifest.json with counts, no account PII.
```

`--broker` values map to C# `BrokerCodes`:

| `--broker` | `brokerCode` in JSON | Env prefix |
|---|---|---|
| `achiever` | `ACHIEVER` | `MT5_*` + `ACHIEVER_PROXY_*` |
| `starwavefx` | `STARWAVEFX` | `MT5_STARWAVEFX_*` |

Refuse any other token. Do not accept `1` / `2`.

### 4.3 Exit codes (stable; C# / CI may switch on them)

| Code | Meaning |
|---:|---|
| 0 | Connected, walk finished, `manifest.complete=true` |
| 2 | Missing credentials / bad argv / SDK `Initialize` failed |
| 3 | `MT5_MODE=remote` (dump is local-only) |
| 4 | `Connect` failed (`GetLastError` copied to stderr JSON + manifest) |
| 5 | Group discovery failed or returned 0 groups after a successful connect |
| 6 | Partial: some logins’ `GetDeals`/`GetUser`/`GetPositions` returned false. Files exist, `complete=false` |
| 7 | `--out` exists and is a previous **complete** dump (no clobber) |
| 8 | DLL SHA-256 mismatch vs A105 (warn-by-default; `--strict-dll` makes this fatal) |

Stdout on success: one-line summary JSON (`ok`, `out`, `groups`, `accounts`, `deals`, `incompleteLogins`). Full rows go to files only (do not spam 5k deals to the console).

Stderr: spdlog. `log_level` from env `LOG_LEVEL` (default `info`). Passwords never interpolated.

### 4.4 Connect sequence (local)

Mirror `mt5_group_probe.cpp` safety; fix its pump gap (A39):

```text
DumpBrokerSlot::load(--broker, --env)
  missing server/login/password → exit 2
  mode != local → exit 3

MT5Manager manager
manager.Initialize(dllDir)           -- not on IMT5Client
if proxy enabled: manager.SetProxy   -- SOCKS5 default; type from host only if we add a §56 type later
                                     -- Achiever: ACHIEVER_PROXY_*; StarwaveFX: PROXY_ENABLED=false still compiles the branch

pump = PUMP_MODE_GROUPS | PUMP_MODE_USERS
       (override 0 only if --pump-groups=off; then empty GroupTotal is a hard fail)

manager.Connect(server:port, login, password, pump)
  on fail: existing no-pump fallback inside MT5Manager still runs
  if !IsConnected → exit 4 with GetLastError (already sanitized: codes 3/5/7/1012)

IMT5Client& client = manager        -- all further calls through the interface

resolveMt5TimeWindow(&client, 0, "mt5-dump", lookbackSeconds, from, to)

GetGroupDetails(groups)
  false or (empty && GroupTotal()==0) → exit 5
  do not sort-unique away distinct names; names are the identity

for each GroupDetail (post-filter --group):
  GetGroupLogins(toWide(name), logins)
  false → mark group incomplete; do not invent an empty login list

for each login (--login / --max-logins apply here, stable sort by login):
  GetUser + GetAccount
  GetDeals(login, window.from, window.to)
  optional GetRecentDeals merge (ticket dedupe; prefer history row if both)
  GetPositions(login)

Disconnect on every path (RAII in dump_command)
```

`Connect` is **not** on `IMT5Client`. That is expected (A04). The CLI is allowed to use `MT5Manager` for lifecycle only.

Default dump pump does **not** need `PUMP_MODE_ORDERS|POSITIONS|SYMBOLS` (those are for live sinks). `GetPositions` already cache-misses into `PositionRequest`. Keep the dump pump small so the operator does not look like a second live collector fighting YoPips for the same slot.

---

## 5. On-disk dump format (`ti.mt5.dump.v1`)

### 5.1 Layout

```text
<out>/
  manifest.json           # required
  groups.json             # required — array of group objects
  accounts.json           # required — array of account objects
  deals.jsonl             # required — one deal object per line, UTF-8, LF
  positions.json          # required — array (may be empty) keyed by login
  errors.jsonl            # required if any incomplete login/group; else omit
```

UTF-8, no BOM. Paths must be writable on Windows (`MAX_PATH` — use `\\?\` only if the implementer hits the limit; default `D:\Prop\data\dumps\...` is fine).

`D:\Prop\data\` is **gitignored** operator output. Do not commit dumps. Do not write under `src/`.

JSONL for deals: a 30-day window across even 50 logins can be large; line-delimited lets C# stream. Groups/accounts/positions stay single JSON arrays.

### 5.2 `manifest.json`

```json
{
  "schema": "ti.mt5.dump.v1",
  "brokerCode": "ACHIEVER",
  "serverName": "AchieverGlobalMarkets-Server",
  "server": "<host from env, not a secret>",
  "port": 443,
  "managerLogin": 2027,
  "connected": true,
  "pumpGroups": true,
  "pumpFallbackNoPump": false,
  "lastError": "",
  "dll": {
    "dir": "D:\\Prop\\apps\\mt5-collector\\build\\Release",
    "mt5ApiManager64Sha256": "51A590CD435B19005621EA5B419E86587C1BA513D4E2138617997F6842B430A9",
    "mt5ManagerApi64Sha256": "41A66C5D65BAE8B114737FB18E330B19A424B1B295BC4FCB5FF9DC251AAAEDAB",
    "mt5CommonApi64Sha256": "DB28E45E082B9FAF86169739B5B08FF725C056A974A7A0A4955B649794C0DD2F"
  },
  "window": {
    "fromUnix": 1748736000,
    "toUnix": 1755475199,
    "fromUtc": "2026-06-01T00:00:00+00:00",
    "toUtc": "2026-08-17T23:59:59+00:00",
    "source": "mt5_server_time",
    "usedFallback": false,
    "lookbackSeconds": 2592000
  },
  "counts": {
    "groups": 0,
    "accounts": 0,
    "deals": 0,
    "positions": 0,
    "incompleteLogins": 0,
    "truncatedByMaxLogins": false
  },
  "files": {
    "groups": { "name": "groups.json", "sha256": "<hex>", "rows": 0 },
    "accounts": { "name": "accounts.json", "sha256": "<hex>", "rows": 0 },
    "deals": { "name": "deals.jsonl", "sha256": "<hex>", "rows": 0 },
    "positions": { "name": "positions.json", "sha256": "<hex>", "rows": 0 }
  },
  "limitations": [
    "IMT5Client::GetDeals is one DealRequest; DealRequestPage is not wrapped",
    "DealData nlohmann to_json omits position; this dump emits positionId from extractDeal",
    "history index can lag >40s; merge-recent is best-effort"
  ],
  "startedAtUtc": "2026-08-18T12:00:00+00:00",
  "finishedAtUtc": "2026-08-18T12:07:00+00:00",
  "complete": false
}
```

`complete` is true **only if** connect succeeded **and** every selected login produced `GetUser` + `GetDeals` + `GetPositions` == true **and** every selected group produced `GetGroupLogins` == true.

C# **must** refuse `complete=false` unless an explicit `allowIncomplete=true` is passed to the connector (default false).

### 5.3 `groups.json` → `Mt5GroupDto`

C# record (`Mt5Contracts.cs:5–12`):

```csharp
Mt5GroupDto(string Name, string? Currency, int CurrencyDigits, string? Company,
            decimal? MarginCall, decimal? MarginStopOut, bool ConnectionsAllowed)
```

Dump object (camelCase, one per `GroupDetail`):

```json
{
  "name": "demo\\Maxmaster",
  "currency": "USD",
  "currencyDigits": 2,
  "company": "Achiever",
  "marginCall": 100.0,
  "marginStopOut": 50.0,
  "connectionsAllowed": true
}
```

Source: `MT5Manager::GetGroupDetails` (`mt5_manager.cpp:984–1012`): `Group()`, `Currency()`, `CurrencyDigits()`, `Company()`, `MarginCall()`, `MarginStopOut()`, `PermissionsFlags() & 0x00000002`.

Do **not** emit plan-mapping labels here. `Mt5Group.PlanMapping` / `EnabledForAnalysis` are C# catalog fields, defaulted by `EfTradingStore.UpsertGroupAsync` (`EnabledForAnalysis = true`).

Backslash in group names is a single JSON-escaped `\`. C# compares `Name` as the raw .NET string `demo\Maxmaster` (same as Fake).

### 5.4 `accounts.json` → `Mt5AccountDto`

C# record (`Mt5Contracts.cs:14–22`):

```csharp
Mt5AccountDto(long Login, string? GroupName, int Leverage,
              decimal Balance, decimal Equity, decimal Margin,
              decimal MarginFree, decimal Profit)
```

Dump object:

```json
{
  "login": 10001,
  "groupName": "demo\\Maxmaster",
  "leverage": 100,
  "balance": 10000.0,
  "equity": 10240.0,
  "margin": 200.0,
  "marginFree": 9800.0,
  "profit": 240.0
}
```

Assembly (do **not** dump raw `AccountData` / `UserData`):

| DTO field | C++ source |
|---|---|
| `login` | `UserData.login` (`GetUser`) |
| `groupName` | `UserData.group` |
| `leverage` | `UserData.leverage` |
| `balance`…`profit` | prefer `AccountData` (`GetAccount`); else `GetUser` overlay (`mt5_manager.cpp:260–272`) |

`AccountData` has **no** group/leverage (`mt5_types.h:47–58`). Using nlohmann `to_json(AccountData)` would drop the fields C# requires.

Integer range: C# `Login` is `long`. If `login > Int64.MaxValue`, skip the account, write `errors.jsonl` `{ "login", "reason": "login_exceeds_int64" }`, mark incomplete. Live MT5 logins fit.

Do **not** put `name` / `email` / `phone` / `rights` on this file.

### 5.5 `deals.jsonl` → `Mt5DealDto`

C# record (`Mt5Contracts.cs:24–38`):

```csharp
Mt5DealDto(long DealTicket, long Login, long OrderTicket, long PositionId,
           string Symbol, DealAction Action, DealEntry Entry,
           ulong VolumeNative, decimal Price, decimal Profit,
           decimal Commission, decimal Swap, DateTimeOffset Time, string? Comment)
```

One line:

```json
{"dealTicket":10501,"login":10001,"orderTicket":20501,"positionId":501,"symbol":"XAUUSD","action":0,"entry":0,"volumeNative":1000,"price":2320.1,"profit":0.0,"commission":-0.6,"swap":0.0,"time":"2026-06-01T08:00:00+00:00","timeUnix":1748764800,"comment":"open"}
```

| DTO | C++ `DealData` / extractor |
|---|---|
| `dealTicket` | `ticket` ← `IMTDeal::Deal()` |
| `login` | `login` |
| `orderTicket` | `order` ← `Order()` |
| **`positionId`** | **`position` ← `PositionID()`** — **must emit**. Raw `to_json(DealData)` **drops this key** (`mt5_types.h:335–340`). Using that adapter would zero every `Mt5Deal.PositionId` and break reconstruction. |
| `symbol` | `symbol` (empty on balance ops — keep the row) |
| `action` | `action` uint 0–20 (`DealAction`) |
| `entry` | `entry` uint 0–3 (`DealEntry`) |
| `volumeNative` | `volume` (`Volume()`, 1 lot = 10 000) |
| `price` / `profit` / `commission` | same doubles → JSON numbers → C# `decimal` |
| `swap` | **`storage`** (A13: storage is swap) |
| `time` | ISO-8601 from `time` unix seconds |
| `comment` | `comment` |

`action` / `entry` are **numbers**, not `"Buy"` / `"In"`. The dashboard API uses `JsonStringEnumConverter` (D76) for *HTTP responses*; ingest DTOs are Manager integers. Fake deals already construct `DealAction.Buy` as enum 0.

Sort lines by `(login, timeUnix, dealTicket)` so a human diff is stable. Deduplicate by `dealTicket` within one broker dump (history ∪ recent).

Unknown `action` > 20: keep the uint, do not drop the row. C# will store the enum’s underlying value. Reconstruction already has to tolerate non-trade actions (`DEAL_BALANCE=2`, canceled 13/14 — A83).

### 5.6 `positions.json` → `Mt5PositionDto`

Required even if the operator only “wanted deals.” `DealIngestionService` (lines 55–56) **always** calls `ReplacePositionsAsync`. A missing file that deserializes as “no positions” would **delete** the current book.

C# record (`Mt5Contracts.cs:40–51`):

```csharp
Mt5PositionDto(long PositionTicket, long Login, string Symbol, TradeDirection Direction,
               ulong VolumeNative, decimal PriceOpen, decimal PriceCurrent,
               decimal PriceSl, decimal PriceTp, decimal Profit, DateTimeOffset TimeCreate)
```

```json
{
  "positionTicket": 501,
  "login": 10001,
  "symbol": "XAUUSD",
  "direction": 0,
  "volumeNative": 1000,
  "priceOpen": 2320.1,
  "priceCurrent": 2325.0,
  "priceSl": 0.0,
  "priceTp": 0.0,
  "profit": 49.0,
  "timeCreate": "2026-06-01T08:00:00+00:00",
  "timeCreateUnix": 1748764800
}
```

| DTO | C++ `PositionData` |
|---|---|
| `positionTicket` | `ticket` |
| `direction` | `action` 0=BUY→`TradeDirection.Long`, 1=SELL→`Short`. Emit **0/1**, not `"Long"`. |
| `volumeNative` | `volume` (same 10 000 scale; ignore the “hundredths” comment) |
| `timeCreate` | ISO-8601 from `time_create` |

`storage` / `time_update` / `comment` are **not** on `Mt5PositionDto`. Omit from ingest JSON (they may go in a later raw sidecar).

### 5.7 `errors.jsonl`

```json
{"scope":"login","login":10002,"groupName":"demo\\yo-2step","op":"GetDeals","reason":"dependency_unavailable"}
{"scope":"group","groupName":"real\\standard","op":"GetGroupLogins","reason":"false"}
```

No secrets. This file existing implies `manifest.complete=false`.

### 5.8 What this format is **not**

| Format | Why not |
|---|---|
| A13 raw `DealData` / `UserData` nlohmann | Missing `position`; account missing group/leverage; unix-only times; `storage` not `swap` |
| A67 replay fixture `ti.replay.fixture.v1` | Offline gold tape for reconstruction tests. Different schema, different purpose. A dump may later *feed* a fixture converter; it is not the fixture. |
| C++ `mt5_ledger` rows | `server_key` + revision. Not `Mt5DealDto`. |
| HTTP `/mt5/accounts/{login}/deals` envelope (`data` / `next_cursor`) | That is the future **serve** dialect (A16). Dump files are not wrapped in `{success,data}`. |

---

## 6. C# ingest (consume the dump; do not modify in this pass)

This section is the contract a later C# increment must implement. **This agent does not add those types.**

### 6.1 Connector

```text
src/Mt5/Dumps/FileMt5DumpConnector.cs     : IMt5BrokerConnector
src/Mt5/Dumps/Mt5DumpManifest.cs
src/Mt5/Dumps/Mt5DumpJson.cs              // camelCase + numeric enums + DateTimeOffset
```

```csharp
// Binding deserializer — implement later
public static readonly JsonSerializerOptions Ingest = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    // Do NOT add JsonStringEnumConverter here.
};
```

`FileMt5DumpConnector` behavior:

| `IMt5BrokerConnector` | Dump behavior |
|---|---|
| `BrokerCode` | `manifest.brokerCode` (`ACHIEVER` / `STARWAVEFX`) |
| `ConnectAsync` | Read + SHA-verify `manifest.json`. Fail if `schema != ti.mt5.dump.v1` or (`complete==false` && !allowIncomplete) or file hashes mismatch. |
| `IsConnectedAsync` | manifest loaded and hashes ok |
| `DisconnectAsync` | release file streams |
| `GetGroupsAsync` | `groups.json` |
| `GetAccountsAsync(group)` | `accounts.json`, filter `GroupName` when `group` not null |
| `GetDealsAsync(login, from, to)` | stream `deals.jsonl`, filter login + `Time` inclusive |
| `GetPositionsAsync(login)` | filter `positions.json` |

DI:

```text
TI_MT5_TRANSPORT=fake | dump | live
TI_MT5_DUMP_DIR=D:\Prop\data\dumps\<run>     # contains ACHIEVER/ and STARWAVEFX/ or a single broker dir
```

- `fake` — current `DemoBrokerFactory` (CI default, Linux-legal).
- `dump` — two `FileMt5DumpConnector`s. Missing dir → process fail-closed (do not silently Fake).
- `live` — reserved for C47.3 `Mt5CollectorClient` HTTP. Not this increment.

`DealIngestionService.SyncBrokerAsync` is **unchanged**. Worker stops hard-coding `10001…` when transport is `dump` (score logins present in `mt5_accounts` after ingest). That worker change is a later slice.

### 6.2 Mapping tests (must exist before a live probe is called PASS)

```text
tests/Unit/Mt5/Mt5DumpJsonMappingTests.cs
  - groups.json sample → Mt5GroupDto name contains backslash
  - deals.jsonl positionId == 501 (proves we did not use nlohmann DealData)
  - volumeNative 1000 → VolumeConverter.ToLots == 0.10m
  - action 13 deserializes as DealAction.BuyCanceled
  - storage is not a DTO field; swap binds from "swap"
  - time ISO-8601 binds DateTimeOffset with Offset==Zero
  - complete=false throws on ConnectAsync
  - ACHIEVER dealTicket 10501 and STARWAVEFX 10501 are two store rows
```

Hermetic: check in a 2-group / 1-login / 2-deal **synthetic** dump under `tests/Unit/Mt5/Fixtures/dump_v1/` with canned Fake-shaped numbers. No live Manager in CI.

### 6.3 Persist path (already written)

`EfTradingStore` already upserts these DTOs:

- groups: insert-or-touch `LastSyncedAt` (update currently only writes `Currency` — known thin update; dump still supplies the full DTO)
- accounts: insert-or-update group/balance/equity
- deals: insert-if-absent on `(BrokerId, DealTicket)` — first write wins (A78)
- positions: replace-by-login

Dump ingest is therefore enough to light the existing Fake pipeline on **real** tickets.

---

## 7. Relationship to C47.3 HTTP sidecar

Keep one CMake project. Two commands, later:

```text
mt5-dump   --broker=achiever --out ...          # this design (batch)
mt5-collector serve --broker=achiever --http 127.0.0.1:9101
```

`serve` reuses `dump_schema` field names so `GET /mt5/groups` JSON **is** `groups.json`’s array (plus an HTTP envelope if A16 literals require `success`). Do not invent a third dialect.

Refuse dealer POSTs with 405 when serve lands. Dump has no HTTP listener at all.

Compose: **still no** collector image (C12 / A65). Windows host only.

---

## 8. Implementation sequence (later coding wave)

Do **not** execute in this agent.

| Step | Work | Exit |
|---|---|---|
| 1 | CMake `mt5-dump` + copy-dlls + `main` that prints `--help` | links `mt5sdk`, WIN32 |
| 2 | `DumpBrokerSlot` §56 binder + dry-run connect (probe-level) | exit 4 on bad creds without writing rows |
| 3 | `GetGroupDetails` → `groups.json` | names ⊄ `{demo\Maxmaster, demo\yo-2step, contest\yo-2step, real\standard}` on a live box (or document that the manager ACL really is that small) |
| 4 | logins + `GetUser`/`GetAccount` → `accounts.json` | `--max-logins=1` works |
| 5 | `GetDeals` + recent merge → `deals.jsonl` with **`positionId`** | unit test fixture + live probe |
| 6 | `GetPositions` → `positions.json` | never omit the file |
| 7 | C# `FileMt5DumpConnector` + mapping tests | `dotnet test` green on Fake + fixture |
| 8 | Operator probe log under `reports/swarm/` | C42 can move only with that artifact |

Quality loop per step: CODER → REVIEWER → TEST. Live probe is **not** CI.

---

## 9. Operator runbook (Windows lab, not CI)

```text
# 1. secrets in user-secrets / process env — never in git
#    MT5_PASSWORD, MT5_STARWAVEFX_PASSWORD, ACHIEVER_PROXY_PASSWORD

# 2. build (after CMake is added)
cmake -B D:\Prop\apps\mt5-collector\build -S D:\Prop\apps\mt5-collector ^
  -DCMAKE_TOOLCHAIN_FILE=<vcpkg>/scripts/buildsystems/vcpkg.cmake ^
  -DVCPKG_TARGET_TRIPLET=x64-windows
cmake --build D:\Prop\apps\mt5-collector\build --config Release

# 3. confirm PE + DLL trio beside exe; SHA-256 == A105 §2.1

# 4. dump both brokers (two processes, two manager slots — do not also run YoPips)
mt5-dump --broker=achiever   --lookback-days 7 --max-logins 50 --out D:\Prop\data\dumps\20260818\ACHIEVER
mt5-dump --broker=starwavefx --lookback-days 7 --max-logins 50 --out D:\Prop\data\dumps\20260818\STARWAVEFX

# 5. C# (later)
#    TI_MT5_TRANSPORT=dump
#    TI_MT5_DUMP_DIR=D:\Prop\data\dumps\20260818
#    apps/mt5-worker  → DealIngestionService

# 6. PASS evidence (write under reports/swarm, redact names)
#    - exit 0 both processes
#    - groups count > canned 3+1  OR honest ACL explanation
#    - at least one dealTicket not in {10501,10502,…} Fake set
#    - positionId != 0 on a market IN/OUT deal
#    - kill dump dir hashes: ConnectAsync fails
```

Egress: Achiever manager IP allowlist `MT_RET_AUTH_MANAGER_IPBLOCK=1012`. Required outbound `81.29.145.69` (C55, non-secret). That is ops, not a code defect.

Two manager licenses. Do not pair this dump with a second live `MT5Manager` on the same login.

---

## 10. Anti-greenwash (reviewer reject list)

| Claim | Reject unless |
|---|---|
| “Collector exists” | `mt5-dump.exe` on disk **and** this report’s schema **and** a probe log |
| “We dumped DealData JSON” | Files use **§5 camelCase DTO names**, including `positionId` / `volumeNative` / `swap` |
| “Used IMT5Client” | No `DealRequest` / `UserAdd` / `DealerSend` identifiers in `apps/mt5-collector/src` except through `IMT5Client` + `MT5Manager` lifecycle |
| “All groups” | `GetGroupDetails`, not `MT5_GROUP_*` |
| “History complete” | `manifest.complete=true` **and** no login with `GetDeals==false` |
| “C# talks to MT5” | Either `dump` ingest of a **live** dump, or C47.3 HTTP. Fake 18 deals ≠ live |
| “Volume in lots” | `volumeNative` integer; lots only via `VolumeConverter` (10 000) |
| “Safe to SendTrade” | still **no** |
| “First useful version / go-live” | still **no** — this is a read snapshot |

---

## 11. Residual risks

1. **`DealRequest` one-shot.** Very large `[from,to]` on a hyperactive login may truncate inside the server. `complete=true` then overclaims. Mitigate: short windows (`--lookback-days 7` first); later wrap `DealRequestPage` on `IMT5Client`.
2. **`PUMP_MODE_GROUPS`.** Probe used `pumpMode=0`. Cache `GroupTotal` may be 0 without the groups pump. Default `--pump-groups` on; empty groups after connect is exit 5, not “broker has no groups.”
3. **History lag >40 s.** Last-minute deals may be missing even when `GetDeals==true`. `--merge-recent` helps only if `OnDealAdd` fired (comment: likely silent — no `PUMP_MODE_DEALS`). Reconciliation remains a later loop (A59).
4. **`GetServerTime` fallback** (`mt5_manager.cpp:1112–1114`) returns **host** `time(nullptr)` when disconnected. Dump must not call it before `IsConnected`. `resolveMt5TimeWindow` already marks `usedFallback`.
5. **Signed tickets.** C# `long` vs C++ `uint64_t`. Fail the login rather than wrap.
6. **Clobber.** Refuse overwrite of `complete=true` dirs so a bad second run cannot erase the only evidence.
7. **PII in comments.** Redact in *reports*; do not rewrite broker comments in the dump.
8. **Manager slot contention** with YoPips / a second dump / a future `serve`.

---

## 12. Sources (read, not modified)

- `D:\Prop\mt5-sdk\src\core\imt5_client.h` — interface; `GetDeals` complete-history contract; group ops; Connect **absent**
- `D:\Prop\mt5-sdk\src\core\mt5_types.h` — `GroupDetail`, `UserData`, `AccountData`, `DealData` (JSON omits `position`), `PositionData`
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` — `Connect` pump + no-pump fallback; `GetUser` overlay; `GetDeals`/`DealRequest`; `GetGroupDetails`; `extractDeal` (`PositionID`); `GetServerTime`
- `D:\Prop\mt5-sdk\src\core\mt5_http_client.cpp` — not a dump transport; `GetGroupDetails` stub (D67)
- `D:\Prop\mt5-sdk\src\services\mt5_time_window.{h,cpp}`
- `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp` — stdout JSON probe; credential silence; `pumpMode=0`
- `D:\Prop\mt5-sdk\CMakeLists.txt` — `mt5sdk_copy_runtime_dlls`
- `D:\Prop\mt5-sdk\config\app_config.{h,cpp}` — single-broker; YoPips proxy key names
- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` — ingest DTOs + `IMt5BrokerConnector`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` — group/account/deal/position loop
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` — upserts
- `D:\Prop\src\Domain\Enums\DealAction.cs`, `DealEntry.cs`, `TradeDirection.cs`
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\src\Domain\Volume\VolumeConverter.cs` — scale 10 000
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` — canned baseline to beat
- `D:\Prop\apps\mt5-worker\Worker.cs` — 30-day Fake poll
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§5–12, 55–56
- Swarm: A04, A12, A13, A14, A16, A30, A39, A40, A58, A59, A75, A78, A81, A105, C20, C42, C47, D67

---

## 13. One-page operator view

```text
R004 mt5-dump                                              2026-08-18  PLAN ONLY
==============================================================================
Binary          Windows x64 CLI (apps/mt5-collector, CMake)
Transport       IMT5Client / MT5Manager local.  MT5_MODE=remote refused
Brokers         one process per slot: achiever | starwavefx  (§56 keys only)
Walk            GetGroupDetails → GetGroupLogins → GetUser/GetAccount
                → GetDeals[from,to] → GetPositions
JSON            ti.mt5.dump.v1  camelCase = Mt5*Dto  (NOT nlohmann DealData)
Must emit       positionId, volumeNative, swap, ISO-8601 time
Must not emit   passwords, UserData PII, dealer results
Must not call   SendTrade / Deposit / Withdraw / CreateUser / CacheExecutedDeal
C# ingest       FileMt5DumpConnector (later). TI_MT5_TRANSPORT=fake|dump|live
Complete        GetDeals false ⇒ complete=false ⇒ C# refuse
HTTP serve      NOT this increment (C47.3)
Live proven     NO until probe log exists (C42)
§69 / §68       unchanged (still 0)
==============================================================================
```

**Bottom line:** the Windows collector starts as a **read-only dump CLI** over the preserved `IMT5Client`, writing C#-native `ti.mt5.dump.v1` files (groups, accounts, deals, positions) so `DealIngestionService` can ingest live tickets without P/Invoke, without a missing HTTP microservice, and without copying YoPips dealer verbs. Product source was not modified.

*End of R004.*
