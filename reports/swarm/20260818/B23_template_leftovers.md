# B23 — Template leftovers (`Class1`, `weatherforecast`)

| Field | Value |
|---|---|
| Agent | B23 (template leftovers only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:19:48+05:30 (hashes / `Test-Path`); 2026-08-18T13:20:49+05:30 (`rg --stats`, DLL strings) |
| Workspace | `D:\Prop` |
| Ask | `rg` `D:\Prop` for `Class1` and `weatherforecast` in `*.cs`; write this report |
| Product source modified | **No.** This report is the only write. |
| Precedence | This file supersedes A01 / A02 / A03 / A05 / A06 / A11 / A29 / A55 / A77 on **whether those two names still exist in product C#**. It does not supersede A55 on worker-loop dead code, or A06/A63 on dashboard-route completeness. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict

**No `Class1` and no `weatherforecast` / `WeatherForecast` remain in any `*.cs` under `D:\Prop`.**

| Question | Count | Answer |
|---|---|---|
| `*.cs` hits for `Class1` (all 179 files, including `bin/` `obj/`) | **0** | `rg --glob "*.cs"` exit 1 |
| `*.cs` hits for `weatherforecast` / `WeatherForecast` (case-insensitive) | **0** | same, 179 files, 1 016 886 bytes |
| Live `Class1.cs` on disk | **0** | all five historical paths `Test-Path` = False |
| Product `*.cs` (exclude `bin/` `obj/`) | **77** | 63 `src` + 5 `apps` + 9 `tests`; none of the names |
| Live **non-`.cs`** product leftovers of the weather template | **2** | `.http` sample + IIS Express `launchUrl` |
| `GET /weatherforecast` mapped in `Program.cs` | **0** | host is `/health` + `/api/*` (see §4) |
| `WeatherForecast` type in Debug `TraderIntelligence.Api.dll` | **0** | ASCII and UTF-16 |

`Class1` source is **gone** (correct). The weather **C# type and route** are **gone** (correct). What is left is config/sample glue, plus **stale Release 4 KB DLLs** from 12:56 that still embed the type name `Class1`. That is output, not source.

A-series reports that still say “API is weatherforecast” or “src is Class1 stubs” are **stale**. Do not resurrect `Class1`. Do not re-add `/weatherforecast`.

---

## 1. Method

| Step | Command / source | Result |
|---|---|---|
| C# `Class1` | `rg -n --glob "*.cs" "Class1" D:\Prop` (with and without `bin/` `obj/`) | 0 matches, 179 files |
| C# weather | `rg -n -i --glob "*.cs" "weatherforecast" D:\Prop` | 0 matches, 179 files |
| Product-wide (exclude `reports/`, `bin/`, `obj/`) | `rg -n -i Class1` / `weatherforecast` | Class1: 0; weather: 2 files (not `.cs`) |
| Word-boundary | `rg -n -w Class1` / `-iw weatherforecast` excluding `reports/` | Class1: 0; weather: `launchSettings.json` line 35 |
| Path census | `Get-ChildItem -Filter Class1.cs -Recurse`; `Test-Path` of the five historical files | 0 files |
| Host surface | read `D:\Prop\apps\api\Program.cs` | no `MapGet("/weatherforecast")`, no `record WeatherForecast` |
| Compose | read `D:\Prop\docker-compose.yml` | no weather probe |
| DLL strings | ASCII + UTF-16 search of Debug/Release `TraderIntelligence.*.dll` | Debug Api: 0; Debug Mt5: `DisplayClass` false positive; Release Mt5: real `Class1` type name |
| Prior notes | A01–A06, A29 §3, A55, A77 | historical quotes only |

Vendor `D:\Prop\mt5-sdk\` was inside the `rg D:\Prop` walk. It contributed **0** `Class1` / `weatherforecast` `.cs` hits.

---

## 2. `Class1` — `dotnet new classlib` leftover

### 2.1 Product C# (this snapshot)

| Path (historical) | Exists now | Status |
|---|---|---|
| `D:\Prop\src\Domain\Class1.cs` | no | deleted; Domain has 47 product sources (B01) |
| `D:\Prop\src\Application\Class1.cs` | no | deleted; Application has Contracts / Dashboard / Ingestion |
| `D:\Prop\src\Infrastructure\Class1.cs` | no | deleted; Infrastructure has DbContext / store / seeder (B03) |
| `D:\Prop\src\Mt5\Class1.cs` | no | deleted; Mt5 has options + fake connector |
| `D:\Prop\src\Fix.CTrader\Class1.cs` | no | deleted; Fix.CTrader has options / parser / ownership / harness |

No consumer ever referenced `TraderIntelligence.*.Class1` (A01/A55). There is still **no** `class Class1` anywhere in product C#.

**Classification:** source `Class1` = **gone** (treat as closed). Recreating it = defect.

### 2.2 Stale Release binaries (not source)

`dotnet new classlib` empty assemblies were ~4 KB. Those **Release** copies were not rebuilt after `Class1.cs` was deleted:

| File | Bytes | LastWriteTime | ASCII |
|---|---|---|---|
| `D:\Prop\src\Mt5\bin\Release\net8.0\TraderIntelligence.Mt5.dll` | 4096 | 2026-08-18T12:56:37+05:30 | `Class1.TraderIntelligence.Mt5` |
| `D:\Prop\apps\mt5-worker\bin\Release\net8.0\TraderIntelligence.Mt5.dll` | 4096 | same | same family |
| `D:\Prop\apps\mt5-worker\bin\Release\net8.0\TraderIntelligence.Application.dll` | 4096 | 12:56:37 | Class1-era stub size |
| `D:\Prop\apps\mt5-worker\bin\Release\net8.0\TraderIntelligence.Domain.dll` | 4096 | 12:56:37 | Class1-era stub size |
| `D:\Prop\apps\mt5-worker\bin\Release\net8.0\TraderIntelligence.Infrastructure.dll` | 4608 | 12:56:37 | Class1-era stub size |

Measured ASCII slice (Release Mt5):

```text
Class1.TraderIntelligence.Mt5.<Module>.System.Runt
```

That is the **old type**, frozen in `bin/Release`. Debug Mt5 (`22016` bytes, 13:20:48) has **no** `WeatherForecast` and its three ASCII `Class1` hits are compiler closures:

```text
<>c__DisplayClass13_0.<>c__DisplayClass14_0.<>c__DisplayClass15_0
```

Do **not** count `DisplayClass` as a leftover. Do **not** check in a “fix” that only `clean`s Release; it is build output.

### 2.3 Reports that still say Class1 is live

At least **34** markdown files under `D:\Prop\reports` mention `Class1` (audit history). The ones that describe **current** `src/*/Class1.cs` as existing are **wrong as of this measurement**: A01, A02, A03, A04, A05, A07, A11, A29 (partially — A29 already said Domain/Infra/Fix were deleted but Application/Mt5 remained), A40, A49, A57. A55 already recorded the five files as gone; this report confirms that still holds and that C# grep is now **zero**.

---

## 3. `weatherforecast` — ASP.NET weather template leftover

### 3.1 Product C# (this snapshot)

| Check | Result |
|---|---|
| `MapGet("/weatherforecast")` | **absent** from `Program.cs` |
| `record WeatherForecast(...)` | **absent** |
| `new WeatherForecast` | **absent** |
| Debug `TraderIntelligence.Api.dll` `weatherforecast` / `WeatherForecast` | ASCII 0, UTF-16 0 |
| `docker-compose.yml` health probe `/weatherforecast` | **absent** (A65 proposed it; compose on disk does not have it) |

`D:\Prop\apps\api\Program.cs` SHA-256 `13CF80036BDE8832122D1D059902AFF0C557CE8341C51255C2C76E7F8ADA62B4` (4503 bytes, 2026-08-18T13:16:00+05:30). Live routes:

```text
GET  /health
GET  /api/health
GET  /api/risk/status
GET  /api/reconciliation/status
GET  /api/settings
GET  /ready
GET  /api/overview
GET  /api/brokers
GET  /api/groups
GET  /api/traders
GET  /api/traders/{broker}/{login}
GET  /api/fix/sessions
GET  /api/risk
GET  /api/trades
POST /api/ops/resync
```

**Classification of the C# weather surface:** **gone**. A06 / A26 / A55 / A63 / A77 / A91–A95 statements that the host is “still only `/weatherforecast`” are **stale**. Whether those `/api/*` routes match architecture `/api/v1/*` is **out of this report’s scope** (see A63).

### 3.2 Live non-C# leftovers (still on disk)

Two product files still contain the token. Neither is `*.cs`.

| Path | SHA-256 | Bytes | Written | Hit |
|---|---|---|---|---|
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | `353BB5D9718D6F86F218C1CE0885A55D8F49F68C249F04DA18E363DEA334543A` | 157 | 2026-08-18T12:54:17+05:30 | line 3 `GET {{TraderIntelligence.Api_HostAddress}}/weatherforecast/` |
| `D:\Prop\apps\api\Properties\launchSettings.json` | `E092DE590CC74329369741650044845BFFF555D87B75D7B0C3C80E00F6E00E78` | 1133 | 2026-08-18T13:15:01+05:30 | line 35 IIS Express `"launchUrl": "weatherforecast"` |

`.http` hash is **unchanged** from A55 (same 157-byte template). Host variable is still `http://localhost:5160` — the original `dotnet new webapi` port. Current `http` / `https` profiles listen on `http://localhost:5000` (and `https://localhost:7294`). So the sample is **wrong twice**: dead path and dead port.

`launchSettings.json` **did** change since A55 (`5CFA6A24…` → `E092DE59…`). Profiles `http` and `https` now use `"launchUrl": "swagger"`. Only **IIS Express** still launches `weatherforecast`.

Workers have their own `launchSettings.json` with **no** weather token.

**Classification:**

| Item | §73 |
|---|---|
| `TraderIntelligence.Api.http` weather GET | **DEPRECATED** (also stale `:5160`) |
| IIS Express `launchUrl` | **DEPRECATED** |
| `http`/`https` `launchUrl` = `swagger` | **EXISTS_NEEDS_REFACTOR** (template swagger, not a leftover of this grep) |
| `GET /weatherforecast` C# | **gone** |
| Anonymous weather as the only API surface | **no longer true**; do not re-open A06-03 as if the route still exists |

### 3.3 Reports that still say weatherforecast is the API

At least **26** report files mention it. Treat A06, A11, A19, A26, A29 §3.2, A30, A49–A51, A54, A55 §1.3/§3.1, A57, A62, A63, A65, A77, A91–A95, A97 as **historical** for the C# route. A55’s “3 live product files” is now **2** (Program.cs dropped out).

---

## 4. Adjacent leftovers (not in the asked grep, noted only)

Out of scope to clean in this agent. Listed so B23 is not mistaken for a full dead-code pass.

| Item | Path | Note |
|---|---|---|
| Filename `UnitTest1.cs` | `D:\Prop\tests\Unit\UnitTest1.cs` | type is `SmokeTests`, not `UnitTest1` |
| Filename `UnitTest1.cs` | `D:\Prop\tests\Integration\UnitTest1.cs` | type is `PlaceholderRemoved` |
| Swagger template knobs | `Program.cs` `AddSwaggerGen` / `UseSwagger` | not weatherforecast |
| Open CORS | `AllowAnyOrigin` in `Program.cs` | **UNSAFE** for a real BFF; not this grep |

`docker-compose.yml` does **not** probe `/weatherforecast`. A65’s sample compose snippet was never landed.

---

## 5. What a later implementer should do (when authorized)

This agent did **not** edit product files.

1. Replace or delete `D:\Prop\apps\api\TraderIntelligence.Api.http`. Point it at `/health` or `/api/health` on `http://localhost:5000`. Do **not** keep `/weatherforecast/`.
2. Change IIS Express `launchUrl` from `weatherforecast` to `swagger` or `/health` (same file as the working `http`/`https` profiles).
3. Do **not** recreate `Class1.cs`. Do **not** rename a leftover into a domain type (A30).
4. Optional: `dotnet clean` / delete stale `bin/Release` 4 KB DLLs so string scans stop reporting `Class1`. Not a source change.
5. Do not use `/weatherforecast` as a compose or k8s probe (A77). Current compose already does not.

---

## 6. Counts (honest)

| Metric | Measured |
|---|---|
| `*.cs` matches `Class1` | **0** / 179 files |
| `*.cs` matches `weatherforecast` (any case) | **0** / 179 files |
| `Class1.cs` files | **0** |
| Product `*.cs` excluding `bin/` `obj/` | **77** |
| Product non-`.cs` files still containing `weatherforecast` | **2** |
| `docker-compose.yml` weather probes | **0** |
| Debug Api.dll weather strings | **0** |
| Release Mt5.dll still named `Class1` | **yes** (stale 4 KB, 12:56) |
| Product source edited by B23 | **No** |

**C# leftover scan: PASS (clean).**  
**Template glue: FAIL (two files).**  
**A-series “still Class1 / still weatherforecast API”: STALE.**
