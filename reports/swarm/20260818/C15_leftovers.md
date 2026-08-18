# C15 — Leftovers (`weatherforecast`, `Class1`) in `apps` / `src` / `tests`

| Field | Value |
|---|---|
| Agent | C15 (leftover scan only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:23:42+05:30 (`rg --stats`); 2026-08-18T13:25:49+05:30 (SHA-256 / `Test-Path`) |
| Workspace | `D:\Prop` |
| Ask | Grep `weatherforecast` and `Class1` in `D:\Prop\apps`, `D:\Prop\src`, `D:\Prop\tests`. Write this report. |
| Product source modified | **No.** This report is the only write. |
| Precedence | Supersedes A06 / A29 §3 / A55 §1.3 / B06 §2 / B23 §3.2 on **how many live product files still contain those two names** under `apps`/`src`/`tests`. Does **not** supersede B08 on §60 coverage, A63 on `/api/v1` completeness, or A55 on worker-loop dead code. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict

**No `Class1` and no `weatherforecast` / `WeatherForecast` remain in any product `*.cs` under `apps`, `src`, or `tests`.**  
**`tests` is clean on both strings (source and binaries).**  
**One live product leftover remains:** IIS Express `launchUrl` = `weatherforecast`.

| Question | Count | Answer |
|---|---|---|
| Product `*.cs` hits for `Class1` (exclude `bin/` `obj/`) | **0** | `rg --glob "*.cs"` exit 1 on all three roots |
| Product `*.cs` hits for `weatherforecast` (case-insensitive) | **0** | same, three roots |
| Live `Class1.cs` on disk (five historical paths) | **0** | all `Test-Path` = False |
| Product `*.cs` excluding `bin/` `obj/` | **79** | 5 `apps` + 63 `src` + 11 `tests` |
| Live **non-`.cs`** product leftovers of the weather name | **1** | `apps/api/Properties/launchSettings.json` line 35 |
| `TraderIntelligence.Api.http` still naming `/weatherforecast` | **0** | file retargeted to `/health` + `/api/*` on `:5000` |
| `GET /weatherforecast` mapped in `Program.cs` | **0** | host is `/health` + `/ready` + `/api/*` |
| `WeatherForecast` type in Debug `TraderIntelligence.Api.dll` | **0** | ASCII and UTF-16 |
| Word-boundary `Class1` in `apps`/`src`/`tests` (including binaries) | **8 files** | stale `mt5-worker` **Release** 4 KB DLLs + matching PDBs only |
| `Class1` / `weatherforecast` in `tests` (any file, `-a`) | **0** | 0 matched lines |

`Class1` **source** is gone (correct). The weather **C# type and route** are gone (correct). The `.http` sample that B06/B23 still listed is **gone** (hash changed). What is left is one launch-profile string, plus **stale `apps/mt5-worker` Release copies** from 12:56 that still embed the type name `Class1`. That is output, not source.

Do not resurrect `Class1`. Do not re-add `/weatherforecast`.

---

## 1. Method

| Step | Command / source | Result |
|---|---|---|
| C# `Class1` | `rg -n --glob "!**/bin/**" --glob "!**/obj/**" --glob "*.cs" "Class1"` on `D:\Prop\apps` `D:\Prop\src` `D:\Prop\tests` | 0 matches, exit 1 |
| C# weather | same globs, `-i weatherforecast` | 0 matches, exit 1 |
| Product-wide (exclude `bin/` `obj/`) | `rg -n -i Class1` / `weatherforecast` on the three roots | Class1: 0; weather: **1** file |
| All files including binaries | `rg -a --hidden --no-ignore -i weatherforecast` | **1** match / 8749 files / 173 738 234 bytes |
| Word-boundary Class1 in binaries | `rg -a -w Class1` | **8** files, all `apps\mt5-worker\bin\Release\net8.0\` |
| Path census | `Get-ChildItem -Filter Class1.cs -Recurse`; `Test-Path` of the five historical files | 0 files |
| Host surface | read `D:\Prop\apps\api\Program.cs` | no `MapGet("/weatherforecast")`, no `record WeatherForecast` |
| `.http` | read `D:\Prop\apps\api\TraderIntelligence.Api.http` | **no** weather token |
| Compose / web (adjacent, not in the three roots’ leftover count) | `rg -i` `docker-compose.yml` + `apps/web` | 0 (web is inside `apps`, already in the 8749-file walk) |
| DLL strings | ASCII + UTF-16 of Debug Api + rebuilt src Release Mt5 + stale worker Release 4 KB DLLs | Debug Api weather 0; src Release Mt5 = `DisplayClass` only; worker Release = real `Class1` |
| Prior notes | A06, A29 §3, A55, B06, B23 | historical / partially stale |

`D:\Prop\mt5-sdk\` and `D:\Prop\reports\` were **out of this ask**. They were not walked.

Do **not** count compiler `<>c__DisplayClass*` or FluentValidation `DefaultClassLevelCascadeMode` as a leftover. Naive `rg -a Class1` on binaries is **1160** matches / **147** files for that reason. Word-boundary (`-w Class1`) is the honest leftover count.

---

## 2. Scope file counts

| Root | Product `*.cs` (no `bin/` `obj/`) | All `*.cs` (incl. generated) | Asked names in product source |
|---|---:|---:|---|
| `D:\Prop\apps` | **5** | 18 | weather: 1 JSON line; Class1: 0 |
| `D:\Prop\src` | **63** | 87 | **0 / 0** |
| `D:\Prop\tests` | **11** | 17 | **0 / 0** |
| **Total** | **79** | 122 | **1** weather string |

Product `apps` C#: `api\Program.cs`, `fix-worker\Program.cs`, `fix-worker\Worker.cs`, `mt5-worker\Program.cs`, `mt5-worker\Worker.cs`.

Product `tests` C#:

| Bytes | SHA-256 | Path | Type name (not `Class1`) |
|---:|---|---|---|
| 224 | `6B1A127F1810FF0A0E1C07F0913A415CBE61D31FE56DF3BD46378C97EB77E6A5` | `tests\Unit\UnitTest1.cs` | `SmokeTests` |
| 162 | `49671A3C7C367ED87C7711E2204865AA2ABB8A7A5783AD785CD66A1F6DA7F4D6` | `tests\Integration\UnitTest1.cs` | `PlaceholderRemoved` |
| 2414 | — | `tests\Unit\BaselineScorerTests.cs` | `BaselineScorerTests` |
| 2144 | — | `tests\Unit\ExecutionAndSizingTests.cs` | (product tests) |
| 2909 | — | `tests\Unit\RiskEngineTests.cs` | `RiskEngineTests` |
| 896 | — | `tests\Unit\SymbolNormalizerTests.cs` | `SymbolNormalizerTests` |
| 3939 | — | `tests\Unit\TradeReconstructionTests.cs` | `TradeReconstructionTests` |
| 791 | — | `tests\Unit\VolumeConverterTests.cs` | `VolumeConverterTests` |
| 7344 | — | `tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs` | (product tests) |
| 5174 | — | `tests\Unit\Sizing\QuantityNormalizerStepMinMaxTests.cs` | (product tests) |
| 3119 | — | `tests\Integration\SeedingAndStoreTests.cs` | `SeedingAndStoreTests` |

Neither test project’s product source contains `Class1` or `weatherforecast`. Integration Debug DLL ASCII `Class1` = **−1**. Unit Debug DLL’s only `Class1` substring is `DisplayClass` (closures), not the template type.

---

## 3. `Class1` — `dotnet new classlib` leftover

### 3.1 Product C# (this snapshot)

| Path (historical) | Exists now | Status |
|---|---|---|
| `D:\Prop\src\Domain\Class1.cs` | no | deleted; Domain has entities / engines |
| `D:\Prop\src\Application\Class1.cs` | no | deleted; Application has Contracts / Dashboard / Ingestion |
| `D:\Prop\src\Infrastructure\Class1.cs` | no | deleted; Infrastructure has DbContext / store / seeder |
| `D:\Prop\src\Mt5\Class1.cs` | no | deleted; Mt5 has options + fake connector |
| `D:\Prop\src\Fix.CTrader\Class1.cs` | no | deleted; Fix.CTrader has options / parser / ownership / harness |

No consumer references `TraderIntelligence.*.Class1`. There is still **no** `class Class1` anywhere in product C# under the three roots.

**Classification:** source `Class1` = **gone** (closed). Recreating it = defect.

### 3.2 Stale Release binaries (not source)

`dotnet new classlib` empty assemblies were ~4 KB. `src\Mt5\bin\Release` **has been rebuilt** since B23 (21504 bytes, 13:23:09, SHA-256 `3F4FE0FDCEE4ECAFF683213518AD689FD20B4A2FED2DA3E4E2EA49D812FA0E2C`). Word-boundary `Class1` in that copy is **absent**; its ASCII `Class1` hit is `<>c__DisplayClass13_0`.

The **worker** Release copies from 12:56 were **not** rebuilt. Word-boundary `rg -a -w Class1` hits exactly these eight files:

| File | Bytes | LastWriteTime | SHA-256 | ASCII |
|---|---:|---|---|---|
| `D:\Prop\apps\mt5-worker\bin\Release\net8.0\TraderIntelligence.Mt5.dll` | 4096 | 2026-08-18T12:56:37+05:30 | `3E1653FABCC5E10F978CF58E6FC9EAF9E7A0C239EA234A0399603A1BF6F57D10` | `Class1.TraderIntelligence.Mt5` |
| `D:\Prop\apps\mt5-worker\bin\Release\net8.0\TraderIntelligence.Application.dll` | 4096 | same | `FE359CC8C9410C014652B1FB813CEDBFDF649060EF0365D00956BFC5C74F87C7` | `Class1.<Module>` |
| `D:\Prop\apps\mt5-worker\bin\Release\net8.0\TraderIntelligence.Domain.dll` | 4096 | same | `195590FC25B7EFB4C9177354CE999E3C1086EB75CA6421088719A7D9977DF7D9` | `Class1.<Module>` |
| `D:\Prop\apps\mt5-worker\bin\Release\net8.0\TraderIntelligence.Infrastructure.dll` | 4608 | same | `39D772C18C974D9FF8C103A28F135DB329EB38B56C53442093A98095A7A84028` | `Class1.<Module>` |
| same folder `TraderIntelligence.Mt5.pdb` | 10544 | same | `7392E3E452AC8280BD62448A04F34E6E8E5048E9B869E2167C393D85BD821D7D` | path `src\Mt5\Class1.cs` |
| same folder `TraderIntelligence.Application.pdb` | 10508 | same | `A900B14C9C115ED7E95C19F357B500D9736DC7932A71B490AF425FDDCEEE8AE8` | path `src\Application\Class1.cs` |
| same folder `TraderIntelligence.Domain.pdb` | 10388 | same | `CB43E2C46A15391D556D2708CFD6980A4295629AA994761D6FE9EBD8BBAACC43` | path `src\Domain\Class1.cs` |
| same folder `TraderIntelligence.Infrastructure.pdb` | 11668 | same | `8767F99A5359AC46D16F608612E1564C635A503B0678C0C654B29348BEF164B3` | path `src\Infrastructure\Class1.cs` |

Measured ASCII slice (stale worker Release Mt5):

```text
Class1.TraderIntelligence.Mt5.<Module>.System.Runtime.Debugg
```

`apps\mt5-worker\bin\Release\net8.0\TraderIntelligence.Mt5Worker.dll` (7680 bytes) has **no** `Class1`.  
Debug trees under `apps\api\bin`, `apps\mt5-worker\bin\Debug`, `src\Mt5\bin\Debug`, `src\Mt5\bin\Release`, and both test `bin\` trees: word-boundary `Class1` **exit 1**.

Do **not** check in a “fix” that only `clean`s Release; it is build output.

### 3.3 Reports that still say Class1 is live source

A01–A05, A07, A11, A29 (Application/Mt5 only), A40, A49, A57 describe `src/*/Class1.cs` as existing. **Wrong as of this measurement.** A55 / B23 already recorded the five files as gone; this pass confirms that still holds and that product C# grep is **zero**. B23’s claim that `src\Mt5\bin\Release\TraderIntelligence.Mt5.dll` is still a 4 KB Class1 stub is **stale** — that copy was rebuilt. Only the **worker** Release copies remain frozen.

---

## 4. `weatherforecast` — ASP.NET weather template leftover

### 4.1 Product C# (this snapshot)

| Check | Result |
|---|---|
| `MapGet("/weatherforecast")` | **absent** from `Program.cs` |
| `record WeatherForecast(...)` | **absent** |
| `new WeatherForecast` | **absent** |
| Debug `TraderIntelligence.Api.dll` `weatherforecast` / `WeatherForecast` | ASCII **−1**, UTF-16 **−1** (32256 bytes, SHA-256 `C03959FFCF1BBED89AC573CB47792CD5E4468B56373150FFF6AEDBF12DE5E6CD`) |
| `docker-compose.yml` health probe `/weatherforecast` | **absent** (A65 proposed it; compose on disk does not have it) |
| `apps/web` | **0** hits |

`D:\Prop\apps\api\Program.cs` SHA-256 `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` (4658 bytes, 2026-08-18T13:22:04+05:30). Live maps (B23 listed the same set; hash moved because the file grew 4503 → 4658):

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

**Classification of the C# weather surface:** **gone**. A06 / A26 / A55 / A63 / A77 / A91–A95 statements that the host is “still only `/weatherforecast`” are **stale**. Whether those `/api/*` routes match architecture `/api/v1/*` is **out of this report’s scope** (see A63 / B06).

### 4.2 Live non-C# leftover (still on disk)

**One** product file still contains the token. It is not `*.cs`.

| Path | SHA-256 | Bytes | Written | Hit |
|---|---|---:|---|---|
| `D:\Prop\apps\api\Properties\launchSettings.json` | `E092DE590CC74329369741650044845BFFF555D87B75D7B0C3C80E00F6E00E78` | 1133 | 2026-08-18T13:15:01+05:30 | line 35 IIS Express `"launchUrl": "weatherforecast"` |

Profiles `http` and `https` already use `"launchUrl": "swagger"` and listen on `http://localhost:5000` (https also `https://localhost:7294`). Only **IIS Express** still launches `weatherforecast`. That path is **not mapped**; a browser opened from that profile will 404.

Workers’ `launchSettings.json` have **no** weather token.

### 4.3 `.http` is no longer a leftover (B06 / B23 stale)

`D:\Prop\apps\api\TraderIntelligence.Api.http` SHA-256 `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` (193 bytes, 2026-08-18T13:20:38+05:30). Entire file:

```http
@api=http://localhost:5000

GET {{api}}/health
GET {{api}}/api/overview
GET {{api}}/api/brokers
GET {{api}}/api/groups
GET {{api}}/api/traders
GET {{api}}/api/fix/sessions
GET {{api}}/api/risk
```

A55 / B06 / B23 quoted the 157-byte template `GET {{TraderIntelligence.Api_HostAddress}}/weatherforecast/` (hash `353BB5D9718D6F86…`). **That content is gone.** Do not reopen A06-03 against the `.http` file.

**Classification:**

| Item | §73 |
|---|---|
| IIS Express `launchUrl` | **DEPRECATED** |
| `TraderIntelligence.Api.http` weather GET | **gone** (was DEPRECATED) |
| `http`/`https` `launchUrl` = `swagger` | **EXISTS_NEEDS_REFACTOR** (template swagger, not this grep) |
| `GET /weatherforecast` C# | **gone** |
| Anonymous weather as the only API surface | **no longer true** |

### 4.4 Reports that still say weatherforecast is the API

Treat A06, A11, A19, A26, A29 §3.2, A30, A49–A51, A54, A55 §1.3/§3.1, A57, A62, A63, A65, A77, A91–A95, A97 as **historical** for the C# route. B06/B23 “2 leftover product files” is now **1** (`.http` dropped out).

---

## 5. Adjacent leftovers (not in the asked grep, noted only)

Out of scope to clean in this agent. Listed so C15 is not mistaken for a full dead-code pass.

| Item | Path | Note |
|---|---|---|
| Filename `UnitTest1.cs` | `D:\Prop\tests\Unit\UnitTest1.cs` | type is `SmokeTests`, not `UnitTest1`; file still named for the xUnit template |
| Filename `UnitTest1.cs` | `D:\Prop\tests\Integration\UnitTest1.cs` | type is `PlaceholderRemoved`; `[Fact] Integration_project_loads` → `Assert.True(true)` (B08 false-green) |
| Swagger template knobs | `Program.cs` `AddSwaggerGen` / `UseSwagger` | not weatherforecast |
| Open CORS | `AllowAnyOrigin` in `Program.cs` | **UNSAFE** for a real BFF; not this grep |

`docker-compose.yml` (SHA-256 `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1`) does **not** probe `/weatherforecast`. A65’s sample compose snippet was never landed.

---

## 6. What a later implementer should do (when authorized)

This agent did **not** edit product files.

1. Change IIS Express `launchUrl` from `weatherforecast` to `health` or `swagger` (same file as the working `http`/`https` profiles). Do **not** keep `weatherforecast`.
2. Do **not** recreate `Class1.cs`. Do **not** rename a leftover into a domain type (A30).
3. Optional: rebuild or `dotnet clean` `apps/mt5-worker` Release so string scans stop reporting `Class1` in the four 4 KB DLLs / four PDBs. Not a source change.
4. Optional: rename `tests/*/UnitTest1.cs` to match the types already inside them. Not a `Class1` / `weatherforecast` defect.
5. Do not use `/weatherforecast` as a compose or k8s probe (A77). Current compose already does not.

---

## 7. Counts (honest)

| Metric | Measured |
|---|---|
| `apps`+`src`+`tests` product `*.cs` matches `Class1` | **0** / 79 files |
| same, `weatherforecast` (any case) | **0** / 79 files |
| `Class1.cs` files | **0** |
| Product files still containing `weatherforecast` | **1** (`launchSettings.json`) |
| `TraderIntelligence.Api.http` weather sample | **0** (cleaned since B23) |
| `docker-compose.yml` weather probes | **0** |
| Debug Api.dll weather strings | **0** |
| Word-boundary `Class1` binaries | **8** (stale `mt5-worker` Release only) |
| `src\Mt5` Release DLL still named `Class1` | **no** (rebuilt 21504 bytes) |
| `tests` hits for either asked name | **0** |
| Product source edited by C15 | **No** |

**C# leftover scan (`apps` / `src` / `tests`): PASS (clean).**  
**`tests` leftover scan: PASS (clean).**  
**Template glue: FAIL (one file).**  
**Stale worker Release `Class1` type: FAIL (output only).**  
**A-series “still Class1 / still weatherforecast API” and B06/B23 “two leftover files”: STALE.**
