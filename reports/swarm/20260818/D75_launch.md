# D75 — `launchSettings` weather leftover?

| Field | Value |
|---|---|
| Agent | D75 (senior engineer, launch-profile leftover only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:43:29+05:30 (`git blame` clock on worktree `launchUrl` lines) / 2026-08-18T13:42:59+05:30 (`Get-FileHash` + product-tree walk) |
| Artifact | `D:\Prop\reports\swarm\20260818\D75_launch.md` |
| Workspace | `D:\Prop` |
| Assigned | `launchSettings` weather leftover? Write this report. **Do not modify product source.** |
| Product source modified | **No.** This report (plus `INDEX.md` / `SWARM_LOG.md` catalog) is the only write. |
| Method | Recurse every `launchSettings.json` under `D:\Prop` (skip `node_modules` / `mt5-sdk/vendor`). SHA-256 + BOM/CRLF of the three product files. Parse profiles with `ConvertFrom-Json`. Case-sensitive `weatherforecast` / `WeatherForecast` walk of `apps` / `src` / `tests` / `scripts` (exclude `bin`/`obj`/`node_modules`). Same token in `bin`/`obj` `json|dll|pdb|cs|http|config`. ASCII + UTF-16 scan of every `TraderIntelligence.Api.dll`. Read `Program.cs`, `.http`, compose. `git show` / `git cat-file blob` of `HEAD:apps/api/Properties/launchSettings.json`. **Did not** `dotnet run`. **Did not** HTTP-probe `:5000` / `:5160` / `:18720` / `/swagger` / `/weatherforecast`. |
| Relates | A06, A55, B06 §2.3, B23, C04 §5.3, C15 §4.2, C22, C50, D06 §5.3 |
| Supersedes | B06 leftover-B, B23 “IIS Express `launchUrl` still weatherforecast”, C04-03, C15 “one leftover remains”, C22-04 (IIS weather clause), C50-10 **for the on-disk `launchSettings.json` string only**. Does **not** supersede D06 as the host census, C22 on CORS / missing `UseSwaggerUI()`, C50 on `.http` coverage, or A63 as the v1 contract. |
| HEAD at measure | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`) |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE` / `GONE`.

---

## 0. Verdict

**No. There is no `weatherforecast` leftover in any on-disk `launchSettings.json`.**

The three product launch files exist. Only the API file has `launchUrl`. All three of its profiles (`http`, `https`, `IIS Express`) are `"launchUrl": "swagger"`. Workers have no `launchUrl` and no weather token. A product-source walk of `apps` / `src` / `tests` / `scripts` (excluding `bin`/`obj`/`node_modules`) is **0** `weatherforecast` / `WeatherForecast`. `bin`/`obj` is also **0**. Debug `TraderIntelligence.Api.dll` ASCII and UTF-16 indexes are **−1**.

The leftover C04 / C15 / B06 / B23 still name is **closed on the worktree**. D06 already recorded the close (`E092DE59…` 1133 B → `BC022898…` 1125 B). This D75 pass **re-hashed the same file** (`BC022898…`, last write **2026-08-18 13:32:01**). It has not grown weather back.

Honest split:

| Surface | Weather leftover? |
|---|---|
| Worktree `apps/api/Properties/launchSettings.json` | **No.** 0 `weatherforecast`. 3× `swagger`. |
| Worktree worker launch files | **No.** Never had the token. |
| Worktree `Program.cs` / `.http` / compose | **No.** Route gone; sample gone; no compose probe. |
| `HEAD` (`398a142…`) `launchSettings.json` | **Yes — still the stock template.** 3× `weatherforecast`, `http`/`https` still `:5160`. Uncommitted worktree is 5 substitutions ahead of git. |
| Browser path `swagger` | **Not weather.** Half-migration: `UseSwagger()` only; **`UseSwaggerUI()` absent** (C22). Expected **404** on `/swagger`. Not a leftover of the forecast route. |

Do **not** re-add `launchUrl: weatherforecast`. Do **not** treat `swagger` as a live UI. Do **not** treat “weather gone from launchSettings” as first-useful v1.

| Question | Answer |
|---|---|
| Is there a live `launchUrl` = `weatherforecast`? | **No** (worktree). |
| How many `launchSettings.json` in the product tree? | **3** (api, fix-worker, mt5-worker). **0** copies under `bin`/`obj`. **0** `.vscode/launch.json`. |
| IIS Express leftover from C15? | **Closed.** Line 35 is `"launchUrl": "swagger"`. |
| `:5160` still in launch / `.http` / product JSON? | **No** (worktree). Lives in `HEAD` + old swarm prose. |
| `GET /weatherforecast` mapped? | **No.** 14 `MapGet` + 1 `MapPost`. 0 weather tokens in `Program.cs`. |
| Product source edited this pass? | **No.** |

---

## 1. Method (what was actually run)

1. `Get-ChildItem -Recurse -Filter launchSettings.json` on `D:\Prop`, excluding `node_modules` and `mt5-sdk\vendor`. Result: **three** files.
2. `Get-FileHash -Algorithm SHA256` + LastWrite + BOM/CR/LF byte counts.
3. `ConvertFrom-Json` of each file → profile name, `commandName`, `launchBrowser`, `launchUrl`, `applicationUrl`, env.
4. `Select-String` `weatherforecast` / `WeatherForecast` / `launchUrl` on `apps`/`src`/`tests`/`scripts` authored files (`json|cs|http|csproj|xml|yml|yaml|md|ts|tsx|js|props|targets`), excluding `bin`/`obj`/`node_modules`.
5. Same `weatherforecast` scan on `bin`/`obj` `json|dll|pdb|cs|http|config`.
6. ASCII + UTF-16 `IndexOf` on every `TraderIntelligence.Api.dll` under `apps/api`.
7. Token counts on `Program.cs`. Full read of `TraderIntelligence.Api.http`. Compose regex `weather|5160|launchSettings|swagger`.
8. `git show` / `git cat-file blob` / `git diff` of `HEAD:apps/api/Properties/launchSettings.json`. `git blame -L 14,36` on the worktree file.
9. Authored-file listing of `apps/api` (non-`bin`/`obj`) to confirm no `WeatherForecast*.cs`.

Did **not** start Kestrel, IIS Express, or Vite. Did **not** `curl` any path. A 404 on `/weatherforecast` is **inferred from a missing map**, not observed.

---

## 2. The three launch files (this snapshot)

| Path | Bytes | LastWrite (local) | SHA-256 | BOM | CR/LF | `weatherforecast` | Profiles |
|---:|---:|---|---|---|---|---:|---|
| `D:\Prop\apps\api\Properties\launchSettings.json` | **1125** | 2026-08-18 13:32:01 | `BC0228981D29FFCF8D94737AF3446D96AB29415ACFF494687250265E266CD7F0` | yes (EF BB BF) | 41 / 41 | **0** | `http`, `https`, `IIS Express` |
| `D:\Prop\apps\fix-worker\Properties\launchSettings.json` | **296** | 2026-08-18 12:54:18 | `25A750D823B04F229FBC49A690F997E969276FFB1A5E5F4EF198DF7DE7CBEF9B` | (schema file; no weather) | — | **0** | `TraderIntelligence.FixWorker` |
| `D:\Prop\apps\mt5-worker\Properties\launchSettings.json` | **296** | 2026-08-18 12:54:40 | `8E2A7548E3EBFF12FDB3E078E06ADA944E3ABB83BA8F9128746542CAA8AA3E36` | — | — | **0** | `TraderIntelligence.Mt5Worker` |

`git status --short`: **`M apps/api/Properties/launchSettings.json`** (worktree dirty vs HEAD). Workers match HEAD (clean).

No other `launchSettings.json` and no `.vscode/launch.json` under `D:\Prop` outside vendor / `node_modules`.

---

## 3. API profiles as parsed (worktree)

`D:\Prop\apps\api\Properties\launchSettings.json` in full (1125 B, UTF-8 BOM, CRLF):

```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true,
    "iisExpress": {
      "applicationUrl": "http://localhost:18720",
      "sslPort": 44389
    }
  },
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:7294;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "IIS Express": {
      "commandName": "IISExpress",
      "launchBrowser": true,
      "launchUrl": "swagger",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

| Profile | `commandName` | `launchUrl` | Listen | Weather? |
|---|---|---|---|---|
| `http` | `Project` | **`swagger`** | `http://localhost:5000` | **No** |
| `https` | `Project` | **`swagger`** | `https://localhost:7294;http://localhost:5000` | **No** |
| `IIS Express` | `IISExpress` | **`swagger`** | IIS `:18720` + SSL `44389` | **No** |

`git blame -L 14,36`: the three `launchUrl` lines (16, 26, 35) are **`Not Committed Yet`**. Surrounding keys are `^6c41447` (initial commit).

Workers (complete behavior):

| File | Profile | Env | `launchUrl` / `applicationUrl` |
|---|---|---|---|
| fix-worker | `TraderIntelligence.FixWorker` | `DOTNET_ENVIRONMENT=Development` | **absent** |
| mt5-worker | `TraderIntelligence.Mt5Worker` | `DOTNET_ENVIRONMENT=Development` | **absent** |

Those two hashes match A08 / B07. They were never a weather leftover.

---

## 4. Hash timeline (same file, same day)

| When | Bytes | SHA-256 | `http`/`https` `launchUrl` | IIS `launchUrl` | Kestrel HTTP |
|---|---:|---|---|---|---|
| A55 / A06 (CRLF worktree of the template) | 1149 | `5CFA6A24…` | `weatherforecast` | `weatherforecast` | `:5160` |
| `HEAD` blob (`git cat-file`, LF + BOM) | **1108** | `36903867B2C1E03F95CC79F157BDD8E49939A977529A2C733B40EF7B4FAF0FC5` | `weatherforecast` ×2 | `weatherforecast` | `:5160` (`5160` count = **2**) |
| B06 / B23 / C04 / C15 | 1133 | `E092DE590CC74329369741650044845BFFF555D87B75D7B0C3C80E00F6E00E78` | `swagger` | **`weatherforecast`** | `:5000` |
| D06 and **this D75** | **1125** | `BC0228981D29FFCF8D94737AF3446D96AB29415ACFF494687250265E266CD7F0` | `swagger` | **`swagger`** | `:5000` |

8-byte shrink 1133 → 1125 is `weatherforecast` (15) → `swagger` (7). D06 already stated that arithmetic. Reconfirmed: worktree file is **byte-identical** to the D06 snapshot (same SHA, same LastWrite **13:32:01**).

`HEAD` still has the A06 template. `git diff --stat`: `apps/api/Properties/launchSettings.json | 10 +++++-----` (five substitutions: three `launchUrl`, two `:5160` → `:5000`). Checking out `HEAD` **reintroduces** the leftover.

---

## 5. Adjacent weather surfaces (not `launchSettings`, still in scope of “is it leftover?”)

| Check | Result | Hash / notes |
|---|---|---|
| `Program.cs` `weatherforecast` / `WeatherForecast` | **0** | 4731 B, `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E`, 2026-08-18 13:35:15. Unchanged vs D06 / D30 / D53. |
| `MapGet` / `MapPost` / `UseSwagger` / `UseSwaggerUI` | 14 / 1 / 1 / **0** | `UseSwagger()` is Development-only. No `MapControllers`, no `MapHub`. |
| `TraderIntelligence.Api.http` | **0** weather; host `:5000` | 193 B, `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651`. Samples 7 maps, no `###` (C50 still true). |
| `docker-compose.yml` | **0** weather / **0** `5160` | 687 B, `1ED8787F…`. API `--urls http://0.0.0.0:5000`. No healthcheck. |
| Product `apps`/`src`/`tests`/`scripts` `weatherforecast` | **0** | Including launch files. |
| `bin`/`obj` `weatherforecast` | **0** | — |
| `TraderIntelligence.Api.dll` (bin + obj) | ASCII **−1**, UTF-16 **−1** | 42496 B, 2026-08-18 13:40:38, SHA `B346B3B7143779C648EBEB60325A7C0E3F7491BCBDEA89255DBC661C65F9ED75`. Grew vs D06’s 32256 B / `C03959FF…` (13:22). Weather still absent. |
| `WeatherForecast.cs` / `Controllers/WeatherForecastController.cs` | **absent** | `Test-Path` False. |
| `apps/api/Controllers/SettingsController.cs` | **present** (3732 B) | **Not** a weather leftover. D06 §5.2 “`Controllers/` False” is **stale**. `Program.cs` still has **0** `AddControllers` / `MapControllers` — the controller is unwired. Out of this leftover ask. |
| `HEAD` `Program.cs` | still `MapGet("/weatherforecast")` + `record WeatherForecast` | Committed host is the template. Worktree host is the 15-map demo. |
| `HEAD` `.http` | `GET …:5160/weatherforecast/` | Worktree retargeted (C04 / C50). |

---

## 6. What is **not** a weather leftover (do not “fix” as if it were)

These remain real, but they are **not** the C15 IIS weather string:

| Item | Class | Why it is not “weather leftover” |
|---|---|---|
| `launchUrl: swagger` ×3 | **EXISTS_NEEDS_REFACTOR** | Half-migration. `UseSwagger()` emits `/swagger/v1/swagger.json`. **`UseSwaggerUI()` is never called.** Browser `swagger` is a likely **404** (C22). Prefer `health` until UI exists (B06 / D06-06). |
| IIS Express `:18720` / SSL `44389` | **EXISTS_NEEDS_REFACTOR** | Vite still calls `:5000` (C24 / B41). Wrong port if F5 uses this profile. Not a forecast path. |
| `https` first URL `:7294` | **EXISTS_NEEDS_REFACTOR** | Same as B41. Not weather. |
| Anonymous 15-map host | **EXISTS_NEEDS_REFACTOR** / `POST /api/ops/resync` **UNSAFE** | D06 / D30 / D53. Deleting a launch string does not make v1. |
| `.http` 7/15, no `###` | C50 **OPEN** | Coverage, not weather. |
| Reports that still *name* `weatherforecast` | historical | A06, A55, A63, A65 sketches, B06, B23, C04, C15, C22, C50. Prose, not product source. |

---

## 7. Stale leftover claims (do not implement them against today’s file)

These sentences were true of `E092DE59…` (1133 B) and are **false** of `BC022898…` (1125 B):

| Report | Stale claim |
|---|---|
| B06 §2.3 / B06-02 | IIS Express `launchUrl` is still `weatherforecast`. |
| B23 §3.2 | One live leftover: `launchSettings.json` line 35. |
| C04 §5.3 / C04-03 | “One leftover string remains.” |
| C15 §0 / §4.2 | “One live product leftover remains: IIS Express `launchUrl` = `weatherforecast`.” |
| C22 §2 / C22-04 | IIS Express still launches deleted `weatherforecast`. (C22’s **`UseSwaggerUI` missing** clause is still true.) |
| C50-10 | IIS Express `launchUrl` is still `weatherforecast`. |

D06 §5.3 / D06-02 already closed the string. D75 is an independent re-measure of **that file**, not a second host census.

---

## 8. Classification (arch §73)

| Component | Class |
|---|---|
| Worktree API `launchUrl` weather string | **GONE** |
| Worktree worker launch files | **EXISTS_AND_GOOD** *as “no weather token”* (they are still stock Worker SDK profiles) |
| `HEAD` API `launchSettings.json` | **DEPRECATED** — 3× `weatherforecast` + `:5160` still committed |
| `GET /weatherforecast` + `WeatherForecast` type (worktree) | **GONE** |
| `.http` weather sample (worktree) | **GONE** |
| Compose `/weatherforecast` probe | **GONE** / never shipped in `docker-compose.yml` |
| `launchUrl: swagger` without UI | **EXISTS_NEEDS_REFACTOR** |
| IIS Express profile / `:18720` | **EXISTS_NEEDS_REFACTOR** (port, not weather) |

---

## 9. Findings

| ID | Sev | Finding |
|---|---|---|
| D75-01 | **PASS** | **0** `weatherforecast` in all three worktree `launchSettings.json`. Only `launchUrl` values are `swagger` (API ×3). |
| D75-02 | **PASS** | C04/C15/B06 IIS leftover is **gone**. File SHA `BC022898…` / 1125 B / 13:32:01 — same as D06. |
| D75-03 | **INFO** | `HEAD` blob 1108 B SHA-256 `36903867…` still has **3** `weatherforecast` and **2** `:5160`. Worktree is dirty. A reset to HEAD reopens the leftover. |
| D75-04 | **MED** | `launchUrl: swagger` without `UseSwaggerUI()` is still a 404 half-migration (C22 / D06-06). **Not** a weather leftover. Do not “fix” it by restoring `weatherforecast`. |
| D75-05 | **INFO** | Product-tree + `bin`/`obj` + Api.dll weather count is **0**. `.http` and compose stay clean. |
| D75-06 | **INFO** | `SettingsController.cs` appeared after D06 (`Controllers/` was False). Unwired (`MapControllers` = 0). **Not** `WeatherForecastController`. |

---

## 10. What this pass did **not** do

- Did not modify `apps/api`, workers, tests, or any product source.
- Did not commit the dirty `launchSettings.json`.
- Did not add `UseSwaggerUI()`, retarget `launchUrl` to `health`, or delete the IIS Express profile.
- Did not HTTP-GET `/weatherforecast`, `/swagger`, or `/health`.
- Did not recensus the 15-map host (D06 / D30 still bind for route lists).
- Did not claim first-useful §69 because a launch string is gone.

**Bottom line:** **`launchSettings` weather leftover = no (worktree). Yes (committed HEAD). The remaining launch smell is `swagger` without Swagger UI, not a forecast route.**
