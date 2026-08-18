# R007 — API must refuse to start if `USE_DEMO_DATA=false` and passwords are placeholders

| Field | Value |
|---|---|
| Agent | R007 (fail-closed boot gate / `USE_DEMO_DATA`) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:54:14+05:30 (2026-08-18T08:24:14Z) |
| Host | `DESKTOP-FQPFPKE` / user `ADMIN` |
| Workspace | `D:\Prop` |
| Artifact | `D:\Prop\reports\swarm\20260818\R007_fail_closed.md` |
| Assigned | Recommend the API **refuse to start** if `USE_DEMO_DATA` is false **and** passwords are placeholders. Write this file. Do not modify product source. |
| Product source modified | **No.** This report is the only write. |
| `src/` / `apps/` / `tests/` / `mt5-sdk/` edited | **No.** |
| Config / `.env*` / `appsettings` / user-secrets edited | **No.** |
| Secret values printed | **None.** Classification + lengths only. |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`main`) |
| Binding law | Architecture v2 §41 / §55–§56 / §62; A49 / A53 / A75 / A77 / A79 / D23 / E011 / R001 |
| Siblings (do not collapse) | R001 (filled `MT5_PASSWORD` hunt), E001 / E011 / E022 (`.env` presence), D23 / C05 (silent InMemory + Fake), D61 / A75 (placeholder catalog), D69 (`REAL_COPY` default false), C42 (no live MT5), C43 (no live FIX), A79 (fake is test-only) |
| Method | `Test-Path` + SHA-256 of hosts / DI / seeder / options / ignored `.env`. Classify password slots; **discard values** after class + length. Grep product `*.cs` / `*.json` / `*.ts*` for `USE_DEMO_DATA` / `USE_REAL_MT5` / `AddUserSecrets` / `DotNetEnv`. Process / User / Machine **name** presence only. Read `Program.cs`, `DependencyInjection.cs`, `DemoSeeder`, `DemoBrokerFactory`, `launchSettings`, compose. **No** Logon. **No** product edit. |

This is a **recommendation + measured pin**. It does **not** implement the gate. It does not invent passwords, load `.env` into the process, flip `REAL_COPY_EXECUTION_ENABLED`, or start Kestrel.

**Masking rule:** live passwords, tokens, API keys, and credentialed URI userinfo are never copied. Placeholder tokens (`<SECRET>`, `<BROKER_ISSUED_VALUE>`, `<BASE64_ENCODED_256BIT_KEY>`, `replace_with_*`) may be named because they are sentinels, not operator secrets. Non-secret flags (`false` / `true`) may be quoted.

---

## 0. Verdict (binding — do not greenwash)

**Recommend: the API must fail closed at boot.**  
If `USE_DEMO_DATA` is **false** (or unparseable / Production-unset treated as false) **and** any **required** password slot is empty, absent, or a placeholder token, the process must **exit non-zero before** `EnsureCreated`, `DemoSeeder`, Kestrel bind, or `/health`.

**Measured today: the API does the opposite.** It always starts. It does not read `USE_DEMO_DATA`. It treats a `<SECRET>` database URL as a license to **fall open** onto EF InMemory + `FakeMt5BrokerConnector` + `DemoSeeder`. That is the defect this file names.

| Assigned claim | Measured / recommended |
|---|---|
| Does product C# bind `USE_DEMO_DATA`? | **No.** `PRODUCT_HITS=0` under `src/`, `apps/`, `tests/`. |
| Does any host load `.env`? | **No.** Zero `DotNetEnv` / `AddUserSecrets` / dotenv loaders. |
| Ignored `.env` flag | `USE_DEMO_DATA=false` (and `USE_REAL_MT5=true`) |
| Are **all** venue passwords still `<SECRET>`? | **No** — mixed. See §3. E001 / D61 / D69 SHA `56C81786…` is **stale**. Use R001 + this file. |
| Would the recommended gate refuse **this** `.env` if it were loaded? | **Yes.** `CTRADER_FIX_PASSWORD` = `PLACEHOLDER_SECRET`; `DATABASE_URL` password = `PLACEHOLDER_SECRET`. Conjunction is already true. |
| Would the recommended gate refuse **today’s process** (no `.env` load, flag unset in Process/User/Machine)? | **Only after a default is chosen.** Flag is **absent** from the running process. Production-unset → treat as `false` → empty/absent passwords → **refuse**. Current Development boot with implicit demo is **fail-open by absence**. |
| Does the API refuse to start today? | **No.** `WebApplication.CreateBuilder` → `AddTraderIntelligence` → maps → `EnsureCreated` + `DemoSeeder` → `app.Run()`. |
| Product source edited by R007? | **No.** |

**Honest one-liner:** `USE_DEMO_DATA=false` plus leftover `<SECRET>` slots must be a **boot crash**, not a silent demo. Today it is a silent demo.

```text
USE_DEMO_DATA (process)        = ABSENT
USE_DEMO_DATA (ignored .env)   = false          (unloaded)
PASSWORD SLOTS                 = MIXED (MT5 filled; FIX + DB still <SECRET>)
HOST LOADS .env                = NO
PLACEHOLDER DETECTOR           = ONLY Database URL, and it FAIL-OPENS to InMemory
BOOT GATE                      = MISSING
API STARTS                     = YES (demo path)
RECOMMENDED WHEN FLAG=false
  AND ANY REQUIRED SLOT IS
  PLACEHOLDER / EMPTY / ABSENT = REFUSE START (exit 1)
```

Do **not** treat a green `/health` as “real mode is configured.” Do **not** treat filled Achiever/Starwave slots as “FIX and Postgres are ready.” Do **not** implement this gate in this pass.

---

## 1. Assigned recommendation (normative)

```text
IF  parse(USE_DEMO_DATA) == false
AND any required password slot is placeholder OR empty OR absent
THEN the API MUST NOT start.
```

This is a **configuration integrity** gate, not a send license and not a liveness probe.

| It is | It is not |
|---|---|
| A boot-time **AND** of “operator asked for non-demo” × “creds are still template tokens” | A substitute for `REAL_COPY_EXECUTION_ENABLED` (A49 / D69) |
| Fail-closed on missing config (§62 spirit: missing required state is **down**) | Permission to emit `35=D` once passwords are filled |
| Shared by `apps/api` as the assigned host; workers should call the **same** helper | A `/ready` 503 (A77: `/ready` is Postgres reachability, not a secret audit) |
| Logged with **key names only** | A place to print tag 554 / Manager passwords |

### 1.1 Why this conjunction

`USE_DEMO_DATA=false` is an operator claim: “this process is **not** the canned Fake + seeder lab.” Placeholder passwords (`<SECRET>`, empty, `replace_with_*`) are the **example sheet**. Combining them means the operator **thinks** they left demo while the secret store is still the architecture §56 template. The only safe action is **refuse to start**. Falling through to InMemory / Fake / forged `/api/health` `healthy: true` is how a dashboard lies.

Architecture §55–§56 **require** placeholders in `.env.example`. They do **not** authorize those tokens in a non-demo runtime. Architecture §62 fail-closes execution when required state is missing. Boot is the earliest required state.

`USE_DEMO_DATA` itself is **not** in §56. It is an extra operator flag now sitting on the ignored `.env` (with `USE_REAL_MT5`). Until a later increment binds it, it is **dead config**. This file is the contract for that bind.

### 1.2 Flag parse (fail closed)

| Raw value | `useDemoData` |
|---|---|
| `true` / `1` / `yes` (case-insensitive, trimmed) | `true` — demo path allowed |
| `false` / `0` / `no` | `false` — **this gate applies** |
| unset, Development | **Recommend default `true`** only if the host is explicitly the local demo (current FUV). Document the default in the log line. |
| unset, Production / Staging | **`false`** — require real slots |
| garbage / unparseable | **`false`** — do not treat junk as demo |

`USE_DEMO_DATA=true` **may** keep today’s Fake + InMemory + `DemoSeeder` path. That is the only mode in which leftover `<SECRET>` is legal.

`USE_DEMO_DATA=false` **forbids** that path, even if the operator also left `ASPNETCORE_ENVIRONMENT=Development`.

### 1.3 What counts as a placeholder

A slot **fails** the gate when, after trim, it matches **any** row:

| Class | Detector | Example (tokens only) |
|---|---|---|
| `ABSENT` | key not in Configuration | process has no `MT5_PASSWORD` today |
| `EMPTY` | `string.IsNullOrWhiteSpace` | `appsettings` `Password=` |
| `PLACEHOLDER_SECRET` | exact `<SECRET>` | ignored `.env` FIX + DB slots |
| `PLACEHOLDER_ANGLE` | entire value is `<…>` | `<BROKER_ISSUED_VALUE>` is **not** a password; do not require it here |
| `PLACEHOLDER_BASE64_KEY` | exact `<BASE64_ENCODED_256BIT_KEY>` | encryption key slot |
| `PLACEHOLDER_REPLACE_WITH` | prefix `replace_with_` | SDK `mt5-sdk/.env.example` |
| `PLACEHOLDER_WORD` | exact `changeme` / `change_me` / `password` / `todo` / `xxx` / `your-password-here` / `placeholder` (case-insensitive) | common paste mistakes |
| `PLACEHOLDER_EMBEDDED` | value **contains** `<SECRET>` | `Password=<SECRET>` inside `DATABASE_URL` |
| `FILLED_NON_PLACEHOLDER` | otherwise non-empty | **pass** this slot (length may be logged; **never** the value) |

DI already implements **one** of these tests:

```22:26:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (string.IsNullOrWhiteSpace(connection) || connection.Contains("<SECRET>", StringComparison.Ordinal))
        {
            services.AddDbContext<TraderDbContext>(o => o.UseInMemoryDatabase("trader-intelligence"));
        }
```

That is a **detector used as a fallback**, not a gate. When `USE_DEMO_DATA=false`, the same predicate must **throw**, not `UseInMemoryDatabase`.

### 1.4 Required slots when `USE_DEMO_DATA=false`

| # | Slot | Why required | Trip this host if loaded today? |
|---|---|---|---|
| 1 | `MT5_PASSWORD` | Achiever Manager Connect | **No** — `FILLED_NON_PLACEHOLDER` len 8 (R001). Process still **ABSENT**. |
| 2 | `MT5_STARWAVEFX_PASSWORD` | StarwaveFX Manager Connect | **No** — filled len 11 in `.env`. Process **ABSENT**. |
| 3 | `CTRADER_FIX_PASSWORD` | FIX Logon tag 554 | **Yes** — `PLACEHOLDER_SECRET` len 8 (`<SECRET>`). |
| 4 | Postgres password (`DATABASE_URL` `Password=` **or** `ConnectionStrings:TraderIntelligence`) | SoT; §62 no execution from memory | **Yes** — URL slot `PLACEHOLDER_SECRET`. DI key `TraderIntelligence` is empty / missing. |
| 5 | `ACHIEVER_PROXY_USERNAME` + `ACHIEVER_PROXY_PASSWORD` | **Iff** `ACHIEVER_PROXY_ENABLED=true` | Slots filled in `.env`; process **ABSENT**. Flag in `.env` is `true`. |

Not in the v1 required set (document, do not silently require):

| Slot | Why deferred |
|---|---|
| `MT5_PASSWORD_ENCRYPTION_KEY` | Product does not encrypt stored Manager passwords yet. Class today: `PLACEHOLDER_BASE64_KEY`. Require it the day a store writes ciphertext. |
| `REDIS_URL` password | Current URL has **no** `Password=` field. Require only if Redis AUTH is configured. |
| `EmergencyFlattenApiKey` | Empty in `appsettings.json`; not a venue password. |
| `CTraderFixOptions.Password` default `""` | Unbound POCO. Once bound, empty **is** a failed slot. |
| `Mt5BrokerOptions.Password` default `null` | Unbound POCO. Same. |

Identity placeholders (`<BROKER_ISSUED_VALUE>` SubIDs, live hosts, logins) are **out of scope** for this password gate. A75 still wants them placeholder-only in `.env.example`. A non-demo boot with missing SubIDs is a **later** FIX-options validator (A25), not this file.

### 1.5 Fail action (API)

Run the check **after** configuration is built and **before**:

1. `AddTraderIntelligence` InMemory fallback (or make that fallback throw when flag is false),
2. `builder.Build()` side effects that seed,
3. `DemoSeeder.SeedAsync`,
4. `app.Run()` / Kestrel listen.

Recommended shape (spec only — **do not add this file in this pass**):

```text
StartupCredentialGate.ValidateOrThrow(configuration)

on failure:
  log Error BOOT_REFUSED
       reason=PLACEHOLDER_OR_EMPTY_PASSWORD
       use_demo_data=false
       keys=CTRADER_FIX_PASSWORD,DATABASE_URL
       // names only — never values, never ConnectionString dump
  Environment.ExitCode = 1
  throw InvalidOperationException("BOOT_REFUSED: USE_DEMO_DATA=false requires filled venue passwords")
```

HTTP must **not** come up. Do not serve `/health` `ok` on a refused boot. A77 liveness is for a process that **intended** to run.

Workers (`apps/mt5-worker`, `apps/fix-worker`) must call the **same** helper. Assigned text says API; a worker-only hole would still seed Fake deals into a shared Postgres the day Npgsql is bound.

### 1.6 Allowed boot matrix

| `USE_DEMO_DATA` | Required passwords | Fake + DemoSeeder + InMemory | API start |
|---|---|---|---|
| `true` | placeholders OK | **allowed** (current lab) | **yes** |
| `true` | filled | allowed (odd but not this gate) | yes |
| `false` | all required `FILLED_NON_PLACEHOLDER` | **forbidden** (adjacent: still refuse Fake; see §10) | **yes only if live composition exists** |
| `false` | any required placeholder / empty / absent | forbidden | **NO — this file** |
| unset + Production | treat as `false` | forbidden | same as false |
| unset + Development | treat as `true` **until** the flag is bound; log `USE_DEMO_DATA_DEFAULT=true` | current path | yes (today’s implicit demo) |

Row “false + all filled” is **not** a license to keep `DemoBrokerFactory`. That is §10. This file’s unique pin is the **placeholder** row.

---

## 2. Measured tree (2026-08-18T13:54:14+05:30)

### 2.1 File identity

| Path | Bytes | SHA-256 | Role |
|---|---:|---|---|
| `D:\Prop\apps\api\Program.cs` | 4731 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | always seeds, always `Run` |
| `D:\Prop\apps\api\appsettings.json` | 1254 | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` | `ConnectionStrings:Postgres` `Password=` **empty**; no `TraderIntelligence` key; `LiveCopyEnabled=false` |
| `D:\Prop\apps\api\Properties\launchSettings.json` | 1125 | `BC0228981D29FFCF8D94737AF3446D96AB29415ACFF494687250265E266CD7F0` | only `ASPNETCORE_ENVIRONMENT=Development` |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` | no `UserSecretsId` |
| `D:\Prop\apps\mt5-worker\Program.cs` | 859 | `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` | same seed-then-run |
| `D:\Prop\apps\fix-worker\Program.cs` | 859 | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` | same |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | `<SECRET>` → InMemory; always Fake |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | 5082 | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | always called |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | 7049 | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` | only `IMt5BrokerConnector` |
| `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs` | 1609 | `64A840278433587B55805042873545D0535C64E7E50DDDD9BF8FDC72E635FAB7` | unbound; comment “secret placeholder” |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | 2344 | `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` | `Password` default `""`; unbound |
| `D:\Prop\docker-compose.yml` | 687 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` | API env = Development only; **no** `USE_DEMO_DATA`, **no** `DATABASE_URL` |
| `D:\Prop\.env` (gitignored) | 3484 | `A4EF94B990EE389C7E7900B599A60AE10E0C16E96E4B5DA612302759958982D7` | unloaded operator sheet |

`.env` git: ignored (`.gitignore:28:.env`); `git hash-object` = `0079d9e3070152c394ae3507f837da98a01e8091`; **not** equal to HEAD `.env.example` blob `b71480a8d9f0cd30166c25e1d124ab744a08fa2f`. LastWrite `2026-08-18T13:52:24.1111072+05:30`.

E011 / D61 / D69 recorded 3408 B / SHA `56C81786…` / blob `b71480a8…` (byte-identical to the example). **Stale.** R001 already pinned the rewrite.

### 2.2 Product bind (absent)

| Probe | Result |
|---|---|
| `USE_DEMO_DATA` / `USE_REAL_MT5` / `UseDemoData` / `UseRealMt5` in product `*.cs` `*.json` `*.ts*` | **0** |
| `AddUserSecrets` / `DotNetEnv` / `LoadEnv` in `src/` + `apps/` | **0** |
| Process / User / Machine `USE_DEMO_DATA` | **absent / absent / absent** |
| Process venue passwords (`MT5_PASSWORD`, `MT5_STARWAVEFX_PASSWORD`, `CTRADER_FIX_PASSWORD`, `DATABASE_URL`) | **all absent** |
| API `UserSecretsId` | **none** |
| `launchSettings` password / flag env | **none** |
| compose `environment` for API | `ASPNETCORE_ENVIRONMENT=Development` only |

Hosts cannot see the ignored `.env`. From the process’s point of view the flag **does not exist**.

### 2.3 Ignored `.env` slots (values discarded)

Safe-to-print flags:

| Key | Value |
|---|---|
| `USE_DEMO_DATA` | `false` |
| `USE_REAL_MT5` | `true` |
| `REAL_COPY_EXECUTION_ENABLED` | `false` |
| `FEATURE_COPY_TRADING_ENABLED` | `false` |
| `CTRADER_FIX_ENABLED` | `true` |
| `CTRADER_FIX_QUOTE_ENABLED` | `true` |
| `CTRADER_FIX_TRADE_SESSION_ENABLED` | `true` |
| `ACHIEVER_PROXY_ENABLED` | `true` |
| `ASPNETCORE_ENVIRONMENT` | `Development` |

Password / key classification (**no values**):

| Key | Class | Length |
|---|---|---:|
| `MT5_PASSWORD` | `FILLED_NON_PLACEHOLDER` | 8 |
| `MT5_STARWAVEFX_PASSWORD` | `FILLED_NON_PLACEHOLDER` | 11 |
| `CTRADER_FIX_PASSWORD` | `PLACEHOLDER_SECRET` (`<SECRET>`) | 8 |
| `ACHIEVER_PROXY_USERNAME` | `FILLED_NON_PLACEHOLDER` | 15 |
| `ACHIEVER_PROXY_PASSWORD` | `FILLED_NON_PLACEHOLDER` | 15 |
| `MT5_PASSWORD_ENCRYPTION_KEY` | `PLACEHOLDER_BASE64_KEY` | 27 |
| `DATABASE_URL` `Password=` | `PLACEHOLDER_SECRET` | 8 |

`LIVE_PASSWORD_SLOT_ALL_FILLED=False`.  
`REQUIRED_V1_PLACEHOLDER_REMAINING=CTRADER_FIX_PASSWORD,DATABASE_URL`.

Filled MT5/proxy slots are **not** a live Manager proof (C42: Fake never reads `Server` / `Password`). They only mean those two keys would **pass** §1.3 if the file were loaded.

---

## 3. Conjunction if the gate existed

Evaluate **three** views. Do not collapse them.

### 3.1 View A — ignored `.env` as if loaded (operator sheet)

| Input | Value |
|---|---|
| `USE_DEMO_DATA` | `false` |
| Required placeholders remaining | FIX password, Postgres password |
| Gate | **REFUSE** |

This is the assigned story: someone set “not demo” and left the §56 tokens on FIX + DB.

### 3.2 View B — actual process now (nothing loaded)

| Input | Value |
|---|---|
| `USE_DEMO_DATA` | **unset** |
| Required passwords | **all ABSENT** |
| If Development default = `true` | start (current implicit demo) |
| If Production default = `false` | **REFUSE** (absent ≡ failed slot) |
| Measured environment | `Development` via launchSettings / compose / `.env` (unloaded) |

Today the API takes the demo path **without reading the flag**. That is fail-open.

### 3.3 View C — current DI detector only

`DATABASE_URL` / `ConnectionStrings:TraderIntelligence` empty or containing `<SECRET>` → **InMemory**, then seed, then listen.

| Detector | Action today | Action under this recommendation |
|---|---|---|
| `<SECRET>` in DB URL | start demo | if `USE_DEMO_DATA=false` → **throw** |
| empty `TraderIntelligence` | start demo | same |
| no `USE_DEMO_DATA` read | n/a | bind first |

---

## 4. Current fail-open surfaces (API)

These are why “the API starts” is not a PASS.

### 4.1 No boot validator

`apps/api/Program.cs` (SHA `61B1E0D1…`) builds, maps anonymous JSON, seeds, runs. There is no `if` on `USE_DEMO_DATA`. There is no password scan. Lines 84–93 always:

```84:95:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DemoSeeder.SeedAsync(
        db,
        scope.ServiceProvider.GetRequiredService<ITradingStore>(),
        scope.ServiceProvider.GetRequiredService<ReconstructionScoringService>(),
        CancellationToken.None);
}

app.Run();
```

### 4.2 Placeholder → InMemory (fail open)

`AddTraderIntelligence` (D23): empty / `<SECRET>` connection is **not** an error. It is the **default** lab database. Compose starts Postgres and does **not** pass `DATABASE_URL`, so the API ignores the container DB.

### 4.3 Fake is the only connector

`DemoBrokerFactory.CreateDefault()` is unconditional (DI L31–33). `ConnectAsync` sets `_connected = true` with no socket (C42). A79 law: the fake is **test-only**. Production DI violates that the moment `USE_DEMO_DATA=false` is claimed.

### 4.4 Health wording vs start

`GET /health` → `{ status: "ok" }` after listen.  
`GET /api/health` → Achiever `healthy: true` with details **“demo FakeMt5BrokerConnector — not live Manager”** (forged healthy bit + honest string).  
`GET /ready` → `ready: true` after counting seeded brokers.

A refused boot must never emit those. A running demo boot may emit them **only** when `USE_DEMO_DATA=true`.

### 4.5 Copy flags still false

`REAL_COPY_EXECUTION_ENABLED=false` (unloaded `.env` + hardcoded `/api/settings` + POCO default). Filling passwords + passing this gate still does **not** start live copy (E002 / E011 / D69). This gate is **necessary** for a non-demo process, **not sufficient** for `35=D`.

---

## 5. Suggested implementation site (do **not** implement here)

| Piece | Where (later increment) | Note |
|---|---|---|
| Placeholder classifier | new helper under Application or Infrastructure, e.g. `StartupCredentialGate` | Pure; no I/O; no logging of values |
| Flag parse | same helper | §1.2 |
| API call site | `apps/api/Program.cs` immediately after `CreateBuilder`, **before** `AddTraderIntelligence` or inside it as the first lines | Must beat InMemory fallback |
| Worker call sites | `apps/mt5-worker/Program.cs`, `apps/fix-worker/Program.cs` | Same helper |
| Config names | `USE_DEMO_DATA`, then existing `MT5_*` / `CTRADER_FIX_PASSWORD` / `DATABASE_URL` | Do not invent a second password env vocabulary |
| `.env` loader | still **MISSING** | Gate is useless until Configuration can see the keys (dotenv **or** process env **or** user-secrets). Loading is a sibling increment; this file only specifies the **refuse** rule. |
| Tests | `tests/Unit` + one host factory test | §6 |

Do **not** put the check only in `/ready`. A 503 after listen still opened a port and may have seeded.

Do **not** put live passwords in `appsettings.json` to “make the gate pass.”

Do **not** log `configuration.AsEnumerable()` (will dump `Password=`).

---

## 6. Tests that must exist before the increment is DONE

Names are recommendations (A27 / A89 style). **0** of these exist (`PRODUCT_HITS=0` includes tests).

| Class / fact | Arrange | Assert |
|---|---|---|
| `StartupCredentialGateTests.False_and_secret_token_throws` | `USE_DEMO_DATA=false`, `CTRADER_FIX_PASSWORD=<SECRET>` | throws; message contains `BOOT_REFUSED` and key **name**; message does **not** contain a filled password |
| `StartupCredentialGateTests.False_and_embedded_db_secret_throws` | `DATABASE_URL=…Password=<SECRET>` | throws; name `DATABASE_URL` |
| `StartupCredentialGateTests.False_and_empty_mt5_throws` | `MT5_PASSWORD=` | throws |
| `StartupCredentialGateTests.False_and_absent_mt5_throws` | key missing | throws |
| `StartupCredentialGateTests.True_and_placeholders_ok` | `USE_DEMO_DATA=true`, all `<SECRET>` | **no** throw |
| `StartupCredentialGateTests.False_and_all_required_filled_ok` | flag false; required slots `FILLED_NON_PLACEHOLDER` (fixture strings, **not** lab `.env`) | no throw **from this gate** |
| `StartupCredentialGateTests.Unparseable_treated_as_false` | `USE_DEMO_DATA=maybe` | throw if slots are tokens |
| `StartupCredentialGateTests.Proxy_required_only_when_enabled` | proxy off + empty proxy password | no throw on proxy keys |
| `ApiHostRefuseTests.Does_not_bind_http_when_refused` | WebApplicationFactory / `Program` entry with false + tokens | process / host start fails; no `/health` 200 |
| `AddTraderIntelligenceTests.No_inmemory_fallback_when_demo_false` | false + `<SECRET>` URL | **must not** register InMemory |

Integration tests must use **synthetic** filled strings (`test-not-a-venue`). Do not read `D:\Prop\.env` in CI.

---

## 7. Adjacent recommendations (not the assigned sentence)

These are true and must not be smuggled in as “the password gate passed, therefore live.”

| # | Adjacent rule | Why |
|---|---|---|
| A | `USE_DEMO_DATA=false` → **do not** call `DemoSeeder.SeedAsync` | Seeder writes live hosts / CompIDs and canned deals. Non-demo catalog comes from Manager + migrations. |
| B | `USE_DEMO_DATA=false` → **do not** register `DemoBrokerFactory` | A79: fake is test-only. Need a real `IMt5BrokerConnector` or refuse. |
| C | `USE_DEMO_DATA=false` → **do not** `UseInMemoryDatabase` | Even with a non-`<SECRET>` typo URL, refuse unless Npgsql can be configured. |
| D | `USE_REAL_MT5=true` + Fake only → refuse | Ignored `.env` already sets this `true`. Same lie as B. |
| E | Load `.env` **or** document process-env / user-secrets as the only stores | A dead flag cannot protect anyone. |
| F | Restore tracked `.env.example` as **placeholder-only** (A75 / D61) | Operator sheet stays gitignored. |
| G | `REAL_COPY_EXECUTION_ENABLED` stays **false** until §68 / §70 | This gate does not flip copy. |
| H | Workers share the helper | Dual seed into one Postgres. |

R007 **owns** only: false flag **and** placeholder/empty/absent required passwords → API does not start.

---

## 8. Stale siblings (do not cite as current for `.env` bytes)

| Report | What it said | vs this measure |
|---|---|---|
| E001 / E011 / D40 / D61 / D69 | `.env` 3408 B, SHA `56C81786…`, password slots all `<SECRET>`, blob = HEAD example | **Stale file identity.** Sheet was rewritten 13:52. FIX + DB still tokens; MT5/proxy now `FILLED_NON_PLACEHOLDER`. |
| A19 / B25 “no live passwords in tree” | scan-time claim | **Stale for the ignored `.env`.** Still true for tracked Git. Do not reprint values. Use R001 + this classification. |
| A77 “API has no `/health`” | template-era | **Stale.** `/health` and `/ready` exist; they are not this gate. |
| A75 “env binder MISSING” | still true | **Holds.** |
| C42 / C43 live venues NOT PROVEN | still true | **Holds.** Filled MT5 slot ≠ Manager session. |
| E011 “live copy cannot start” | still true | **Holds.** This gate does not start copy. |

---

## 9. What this file does **not** do

- Does not implement `StartupCredentialGate` or edit `Program.cs` / DI.
- Does not print, log, or invent a password.
- Does not `set` process env, write `secrets.json`, or restore `.env.example`.
- Does not load `.env` into this process.
- Does not connect to Achiever, StarwaveFX, or `*.c-trader.com`.
- Does not flip `REAL_COPY_EXECUTION_ENABLED` or `USE_DEMO_DATA`.
- Does not claim “if FIX password is pasted, we are live.” Still no initiator (E002), no bound options, Fake still registered.
- Does not treat R001 `MT5_PASSWORD PRESENT` as a go-live.

---

## 10. Assigned answer (do not paraphrase away)

**Recommend: yes. The API must refuse to start when `USE_DEMO_DATA` is false and any required password is a placeholder (or empty / absent).**

Measured now, that rule is **not implemented**. The only placeholder check in product C# **starts InMemory instead**. The ignored `.env` already has `USE_DEMO_DATA=false` with `CTRADER_FIX_PASSWORD=<SECRET>` and `DATABASE_URL` password `<SECRET>` — the conjunction the gate is built to catch. Hosts do not load the file, so today’s start is the **implicit demo** path, which is a different fail-open.

**Do not implement in this pass. Do not start the API in claimed non-demo mode on leftover `<SECRET>` tokens.**

---

## 11. Reproduction (names / paths only — do not print values)

```powershell
# Flag + class only. Never Write-Output the right-hand side of password keys.
Select-String -Path D:\Prop\.env -Pattern '^USE_DEMO_DATA='
Select-String -Path D:\Prop\src,D:\Prop\apps,D:\Prop\tests -Pattern 'USE_DEMO_DATA' -Include *.cs,*.json |
  Where-Object { $_.Path -notmatch '\\(bin|obj|node_modules)\\' }
# Expect: one hit in ignored .env; zero in product.
```

Expected at measure time: `.env` has `USE_DEMO_DATA=false`; product grep empty; API `Program.cs` still reaches `app.Run()` with no credential throw.

---

## 12. Sign-off

| Item | Result |
|---|---|
| Recommendation | **API refuse-to-start** on `USE_DEMO_DATA=false` ∧ placeholder/empty/absent required passwords |
| Implemented? | **No** |
| Product source touched? | **No** |
| Secret values in this report? | **No** |
| `.env` loaded by hosts? | **No** |
| `USE_DEMO_DATA` bound? | **No** |
| Ignored `.env` flag | `false` |
| Required slots still tokens | `CTRADER_FIX_PASSWORD`, `DATABASE_URL` password |
| Current API boot | **FAIL-OPEN** (demo InMemory + Fake + seeder) |
| Live MT5 proven? | **No** (C42) |
| Live FIX proven? | **No** (C43) |
| Live copy can start? | **No** (E011) |

*End of R007. Product source was not modified. Recommend fail-closed boot; do not ship `USE_DEMO_DATA=false` on leftover `<SECRET>`.*
