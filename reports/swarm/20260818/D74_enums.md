# D74 — Does the API use `JsonStringEnumConverter`?

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D74_enums.md` |
| Agent | D74 (senior engineer, HTTP enum wire format only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (file hashes + isolated STJ eval) |
| Assigned | API `JsonStringEnumConverter`? Write this file. Do not modify product source. |
| Primary SUT | `D:\Prop\apps\api\Program.cs` (minimal-API host; **not** `D:\Prop\src\apps\api` — that path does not exist) |
| Product source modified | **No.** Report + throwaway eval under `_tmp_d74_enums/` only. |
| Method | Full read of `apps/api/Program.cs`, `TraderIntelligence.Api.csproj`, dead `Controllers/SettingsController.cs`, `Application/Dashboard/DashboardModels.cs`, `Infrastructure/Dashboard/EfDashboardQueries.cs`, `Infrastructure/DependencyInjection.cs`, all 15 `Domain/Enums/*.cs`, live-leaked `ReconstructedTrade`, workers’ `Program.cs`, web pages/hooks/types. Token grep for `JsonStringEnumConverter` / `ConfigureHttpJsonOptions` / `AddJsonOptions` / `AddControllers` / `Newtonsoft` / `[JsonConverter]` / `[EnumMember]` / `JsonStringEnumMemberName`. SHA-256 of host + DTOs + enums. Isolated `dotnet run` of `JsonSerializerDefaults.Web` ± the same converter against **real** `TraderRowDto` / `TradeHighlightDto` / `ReconstructedTrade`. **API process was not launched.** No live HTTP capture. No Swagger document capture. |
| Relates | D06 / D21 / D30 / D39 (converter present). **Supersedes** B10 §219 and B29 header “no converter / integer wire” for `Program.cs` SHA `61B1E0D1…`. Does **not** make B29’s DTO-vs-TS field-name mismatches go away. |
| Architecture | A26 wants `"state": "SHADOW"` (string tokens). A48 wants `STOP_NEW_EXECUTION` / `EMERGENCY_FLATTEN` (not the C# `KillSwitchMode` identifiers). A69 vocabulary = the nine `TraderState` names. |

Classification vocabulary is architecture §73: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE` / `GONE`.

---

## 0. Headline answer (measured)

**Yes.** The live dashboard host registers `System.Text.Json.Serialization.JsonStringEnumConverter` on **HTTP JSON options** used by minimal APIs.

```10:13:D:\Prop\apps\api\Program.cs
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
```

| Question | Answer |
|---|---|
| Is the converter in the live host? | **Yes.** One call, `Program.cs` L12, fully-qualified name (no `using`). |
| Which options bag? | `ConfigureHttpJsonOptions` → `Microsoft.AspNetCore.Http.Json.JsonOptions` (minimal-API / `Results.Ok` / typed returns). |
| MVC `AddJsonOptions`? | **No.** Token count `AddJsonOptions` = 0. |
| `AddControllers` / `MapControllers`? | **No.** `SettingsController` is **unmapped dead code**. |
| Newtonsoft? | **No.** Zero package, zero tokens. |
| Per-enum `[JsonConverter]` / `[EnumMember]` / `[JsonStringEnumMemberName]`? | **None** in `src/` or `apps/`. |
| Constructor arguments? | **Default ctor.** Equivalent to `namingPolicy: null`, `allowIntegerValues: true`. |
| Property names? | Still **camelCase** (`JsonSerializerDefaults.Web`). Converter does **not** rename properties. |
| Enum **values** on the wire? | JSON **strings** = C# identifier text. **Not** integers. **Not** camelCased unless a naming policy is passed (it is not). |
| B10 / B29 “integers / no converter”? | **STALE** vs this `Program.cs` hash. |

Honest one-liner: **string enums on the live minimal-API wire; identifier spelling is whatever the C# member is named; integers are still accepted on read; MVC/SignalR/manual `JsonSerializer` are not covered because those stacks are not wired.**

Classification of this setting: **EXISTS_AND_GOOD** for the demo BFF’s current 15 maps. **EXISTS_NEEDS_REFACTOR** as a platform contract (mixed SCREAMING vs Pascal identifiers; kill-switch already `ToString()`s PascalCase; A48 tokens not produced; no test lock; converter not registered on any future MVC/SignalR bag).

---

## 1. Files hashed (inputs)

| Path | Bytes | Lines | SHA-256 |
|---|---:|---:|---|
| `D:\Prop\apps\api\Program.cs` | 4731 | 95 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | 21 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | — | — | `B19274DC71F6BECB54B6C1A270D3CA9F47C9B1CCD340D0C303F4C663CA50C23F` |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | 3088 | 114 | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | 8708 | 205 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | — | — | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` |
| `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` | — | — | `06A1A7651EDFD9C7E4482293774F9F9BBEA778AEAB6ECFBAF95E5B49F90F8014` |
| `D:\Prop\apps\web\src\types\index.ts` | — | — | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` |
| `D:\Prop\apps\web\src\pages\TradersPage.tsx` | — | — | `0AF0FF5BD2EE6B7B4BB06F483B065589A1235FE94EE63B2F4491EC00C510518F` |
| `D:\Prop\apps\web\src\pages\TradeExplorerPage.tsx` | — | — | `7EE11EB97DBBE0ED077E72A5E15D0DA8EC9E53F8D60CBD76B48985449016AE31` |

`Program.cs` hash **matches** D06 / D21 / D30 / D50 / D53. Converter registration is not a later drift — it is this host.

`DependencyInjection.cs` has **zero** JSON / converter tokens. Workers (`apps/mt5-worker/Program.cs`, `apps/fix-worker/Program.cs`) are generic hosts — no HTTP JSON bag.

### 1.1 Domain enum files (`Get-FileHash` SHA-256)

| File | Bytes | Lines | SHA-256 |
|---|---:|---:|---|
| `CopyIntentAction.cs` | 182 | 10 | `94BA143D84459E2DB8C04E5E9199A4D548443A5C4BF99C015046E995E22C7AF6` |
| `DealAction.cs` | 622 | 29 | `6E87BFB536D43A57B48D548A0718E3C8C2E4914CE3CD0577410E6CB61D5054F1` |
| `DealEntry.cs` | 239 | 12 | `C0A217FC3C44B1DEB2CB50F705C3C7D03103760D61B01C3FEBAB6FCC74A49E08` |
| `DealReason.cs` | 1149 | 50 | `3A4D92122D72155ACA3C0D9174758A966741A8B6E830917E6463CD905659E593` |
| `ExecutionOrderStatus.cs` | 260 | 13 | `801D89D0A5D0E73F76EC195776C5A4D2BA3A09630F13A148C1B9C0AF27D9E7AF` |
| `FeatureQuality.cs` | 131 | 8 | `474EA06DCDE7B3F8E20F8A8B9E0DECD50D87C1D8D9DF8F0519A9FCE609AA9D20` |
| `FixSessionQualifier.cs` | 118 | 8 | `E35184BFFD5DD540448535D75F156198E75CDE4A82EECD719FE587DA24BF34C9` |
| `FixSessionStatus.cs` | 266 | 14 | `49AD4FD0DB6DF8DF2AD57365822CCA70E0106E49BCD7F153D8CD332EF8FF3268` |
| `KillSwitchMode.cs` | 140 | 8 | `528429B0DF8023E3DAB465BC6C8D1C025DCE651EA31E11A2E8FA68DDE8BFBC82` |
| `OutboxEventType.cs` | 211 | 11 | `163ED842EE9AF0C94EA912A91845F31C8644F2A1A373A67C77E7FA16154BAADA` |
| `PriceSource.cs` | 195 | 10 | `0ACB91DC3A5EF2CAF09E65B656A0CFCF9D10E200FB13AE1BBDE399805E3F4AFA` |
| `ReconciliationIssueType.cs` | 286 | 12 | `11609961FD2FD8B3C978775BEB71F43E2C70F36802377E90C5C237513B20C914` |
| `RiskDecisionOutcome.cs` | 206 | 12 | `A0753C0FAA97261E1E26717AB3E6465F30C9F2D9024A3FF3675B1377C7D26951` |
| `TradeDirection.cs` | 112 | 8 | `584F1BB4D9D33967089A52931E36B1B4B9EB34CEC00418848E99914B40925342` |
| `TraderState.cs` | 264 | 15 | `E509C59F1597EE0F424A9F9408D0B27B2C7063D724B3D0A63880E2558B930D68` |

---

## 2. What is **not** registered

Token counts on `apps/api/Program.cs` (case-sensitive):

| Token | Hits | Meaning |
|---|---:|---|
| `JsonStringEnumConverter` | **1** | Live. |
| `ConfigureHttpJsonOptions` | **1** | The only JSON options mutation. |
| `AddJsonOptions` | 0 | MVC JSON bag untouched (and MVC not added). |
| `AddControllers` / `MapControllers` | 0 | Controller pipeline off. |
| `Newtonsoft` | 0 | No Newtonsoft stack. |
| `JsonNamingPolicy` | 0 | No camelCase / snake_case policy on the **enum converter**. |
| `JsonSerializerDefaults` | 0 | Host relies on the framework default (`Web`) inside `HttpJsonOptions`. |
| `AllowIntegerValues` | 0 | Default **true** (not overridden). |
| `JsonStringEnumMemberName` | 0 | No per-member wire aliases. |
| `AddSignalR` / `MapHub` | 0 | No hub JSON (D50). |
| `AddAuthentication` | 0 | Unrelated; still anonymous. |

Repo-wide under `D:\Prop\src` + `D:\Prop\apps` `*.cs`: the **only** `JsonStringEnumConverter` / `ConfigureHttpJsonOptions` / `JsonSerializer` converter registration is that one `Program.cs` line. No `[JsonConverter(typeof(JsonStringEnumConverter))]` on any enum or DTO property.

---

## 3. What the default constructor actually does

`new JsonStringEnumConverter()` on .NET 8 is:

```text
namingPolicy: null
allowIntegerValues: true
```

Effects that matter to this host:

| Direction | Behavior (measured in `_tmp_d74_enums`) |
|---|---|
| Write `TraderState.WATCH` | `"WATCH"` |
| Write `TradeDirection.Long` | `"Long"` |
| Write `TradeDirection.Short` | `"Short"` |
| Write **without** converter | `2` / `0` (integers) |
| Write with `JsonNamingPolicy.CamelCase` on the converter (not what the API does) | `"long"` |
| Read `"WATCH"` | `WATCH` |
| Read `"watch"` / `"Watch"` | `WATCH` (case-insensitive) |
| Read `2` (JSON number) | `WATCH` (`AllowIntegerValues=true`) |
| Read `"2"` (JSON string of the number) | `WATCH` |
| Read `"WATCH"` **without** converter | `JsonException` |

So: **output is strings; input is strings or integers.** Clients that still send `state: 2` would deserialize if a body ever bound an enum (none of the 15 maps do). Query `?state=` is **not** JSON — see §6.

`HttpJsonOptions.SerializerOptions` starts as `new JsonSerializerOptions(JsonSerializerDefaults.Web)`:

- property names camelCase (`state`, `direction`, `completedXauTrades`)
- property-name case-insensitive on read
- **no** enum-as-string until this converter is added

That is why the measured payload is `{"state":"WATCH",...}` and not `{"State":"WATCH"}` or `{"state":2}`.

---

## 4. Measured JSON (isolated eval, same options as the host)

Eval: `D:\Prop\reports\swarm\20260818\_tmp_d74_enums\` referencing live `Domain` + `Application`. Release `dotnet run`. Not an HTTP round-trip.

### 4.1 `TraderRowDto` with converter (what `GET /api/traders` serializes)

```json
{"broker":"ACHIEVER","login":10001,"group":"demo","completedXauTrades":3,"netSourcePnl":12.3,"earlyScore":70,"mlProbability":null,"riskScore":10,"martingale":false,"averagingDown":false,"lotEscalation":false,"state":"WATCH","shadowPnl":0,"lastScored":"2026-08-18T00:00:00+00:00"}
```

Without converter, the same enum field is `{"state":2}`.

### 4.2 `TradeHighlightDto` with converter (what `GET /api/traders/{broker}/{login}` embeds)

```json
{"positionId":1,"sourceSymbol":"XAUUSD.s","canonicalSymbol":"XAUUSD","direction":"Long","openedAt":"2026-08-18T00:00:00+00:00","closedAt":null,"netRealizedPnl":1.25,"maxVolumeLots":0.10,"completed":true,"isFirstThree":true}
```

Without converter: `{"direction":0}`.

### 4.3 `ReconstructedTrade.Direction` (what `GET /api/trades` leaks)

With converter: `{"direction":"Short"}`.

### 4.4 Already-stringified fields (converter never sees an enum)

| Expression in `EfDashboardQueries` | Measured `ToString()` | JSON type |
|---|---|---|
| `KillSwitchMode.StopNewExecution.ToString()` | `StopNewExecution` | string |
| `FixSessionQualifier.Quote.ToString().ToUpperInvariant()` | `QUOTE` | string |
| `FixSessionStatus.LoggedOn.ToString()` | `LoggedOn` | string |

These are `string` DTO properties. `JsonStringEnumConverter` is irrelevant. A48’s tokens `STOP_NEW_EXECUTION` / `EMERGENCY_FLATTEN` are **not** what `/api/risk` returns.

---

## 5. Live maps: where an enum actually hits the serializer

Fifteen anonymous maps (D30). Only **three** response shapes carry a **C# enum-typed** property. Everything else is already a `string`, `int`, `bool`, or anonymous object with no enums.

| # | Method | Path | Enum-typed JSON fields | Wire token (this converter) |
|---|---|---|---|---|
| 10 | GET | `/api/traders` | `TraderRowDto.State` : `TraderState` | `"INSUFFICIENT_DATA"` … `"DISQUALIFIED"` |
| 11 | GET | `/api/traders/{broker}/{login}` | `header.state` + `trades[].direction` | `"WATCH"` + `"Long"` / `"Short"` |
| 14 | GET | `/api/trades` | raw `ReconstructedTrade.Direction` | `"Long"` / `"Short"` |

**Stringified before serialize (converter idle):**

| Path | Field | How it becomes a string | Example |
|---|---|---|---|
| `GET /api/fix/sessions` | `qualifier` | `Qualifier.ToString().ToUpperInvariant()` | `"QUOTE"` / `"TRADE"` |
| `GET /api/fix/sessions` | `status` | `Status.ToString()` | `"LoggedOn"`, `"Disconnected"`, … |
| `GET /api/risk` and `/api/risk/status` | `killSwitch` | `(ks?.Mode ?? None).ToString()` | `"None"`, `"StopNewExecution"`, `"EmergencyFlatten"` |

**No enum on the wire:**

`/health`, `/api/health`, `/api/reconciliation/status`, `/api/settings` (anonymous object), `/ready`, `/api/overview` (state **counts** are `int`s: `watch`, `shadow`, `liveCandidates`, …), `/api/brokers`, `/api/groups`, `POST /api/ops/resync`.

`OverviewDto` property names are camelCase counts, not enum values. Operators do not see `"WATCH"` on `/api/overview`; they see `"watch": <int>`.

`Mt5DealDto.Action` / `Entry` / `Mt5PositionDto.Direction` exist on **application contracts** but **no map returns those DTOs**. They would become `"Buy"` / `"In"` / `"Long"` if someone later returned them through this host.

---

## 6. Query binding is not the converter

`GET /api/traders?broker=&state=` binds `state` as `string?` (`Program.cs` L57–58). Filter is:

```113:114:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        if (!string.IsNullOrWhiteSpace(state) && Enum.TryParse<TraderState>(state, true, out var st))
            filtered = filtered.Where(t => t.State == st);
```

`Enum.TryParse(..., ignoreCase: true)` accepts `WATCH`, `watch`, and **`"2"`**. An unrecognized token is silently ignored (no 400). This path never touches `JsonStringEnumConverter`.

No live map binds a route/query/body parameter as a C# enum type.

---

## 7. Domain enum inventory vs wire spelling

Identifier style is **not uniform**. The converter copies the identifier. That is the entire naming policy.

| Enum | C# style | Members (binding names) | On live HTTP as enum? |
|---|---|---|---|
| `TraderState` | SCREAMING_SNAKE | `INSUFFICIENT_DATA`, `EARLY_SCORE`, `WATCH`, `SHADOW`, `LIVE_CANDIDATE`, `LIVE`, `PAUSED`, `RISK_BLOCKED`, `DISQUALIFIED` | **Yes** — `TraderRowDto.state` |
| `TradeDirection` | PascalCase | `Long`, `Short` | **Yes** — highlight + raw trade |
| `KillSwitchMode` | PascalCase | `None`, `StopNewExecution`, `EmergencyFlatten` | No — already `ToString()` |
| `FixSessionQualifier` | PascalCase | `Quote`, `Trade` | No — uppercased string `"QUOTE"`/`"TRADE"` |
| `FixSessionStatus` | PascalCase | `Disconnected` … `Error` | No — `ToString()` |
| `DealAction` | PascalCase | `Buy` … `StopOutCompensationCredit` | Not on any map |
| `DealEntry` | PascalCase | `In`, `Out`, `InOut`, `OutBy` | Not on any map |
| `DealReason` | PascalCase | `Client` … `CorporateAction` | Not on entity `Mt5Deal` either (only `NormalizedDeal`) |
| `CopyIntentAction` | PascalCase | `OpenExposure` … `CloseExposure` | Not on any map |
| `ExecutionOrderStatus` | PascalCase | `NotSent` … `ExecutionStateUnknown` | Not on any map (`ExecutionIntent.Status` is a **string** `"Pending"`) |
| `FeatureQuality` | PascalCase | `Exact`, `Approximate`, `Unavailable` | Not on any map |
| `OutboxEventType` | PascalCase | `TradeCompleted` … `NotificationEvent` | Not on any map |
| `PriceSource` | PascalCase | `Unknown` … `CTraderQuoteSession` | Not on any map |
| `ReconciliationIssueType` | PascalCase | `UnknownExternalPosition` … `UnresolvedExecutionState` | Not on any map (`/api/reconciliation/status` is hardcoded zeros) |
| `RiskDecisionOutcome` | PascalCase | `Approve` … `GlobalStop` | Not on any map (`/api/risk` returns `reason` strings only) |

A26 sample `"state": "SHADOW"` **matches** `TraderState` identifiers.

A26 / A48 kill-switch tokens `STOP_NEW_EXECUTION` **do not match** `KillSwitchMode.ToString()` = `StopNewExecution`. Adding the converter later on that field would not fix it unless the enum is renamed or `[JsonStringEnumMemberName]` is added — and today the field is already a `string`.

---

## 8. Persistence ≠ wire

`TraderDbContext.OnModelCreating` has **no** `HasConversion<string>()` on any enum. EF Core default = **store as int**. Demo seed and queries compare `TraderState.WATCH` in CLR. JSON stringification is HTTP-only.

`FixSessionState.Qualifier` is unique-indexed as the enum’s numeric value.

---

## 9. Frontend (consumer of the converter)

| Surface | What it does |
|---|---|
| `hooks.ts` | `useTraders` / `useTraderDetail` / `useTrades` pass JSON through as `r.data`. No mapping. |
| `TradersPage.tsx` / `ScoringPage.tsx` | `{t.state}` — prints the wire token. With converter: `WATCH`. Without: `2`. |
| `TraderDetailPage.tsx` | `{h.state}` via `String(v)`. |
| `TradeExplorerPage.tsx` | `{t.direction}` — `Long` / `Short`, not `0`/`1`. |
| `RiskPage.tsx` | `{data.killSwitch}` — already a string from `ToString()`. |
| `FixSessionsPage.tsx` | `{s.qualifier}` / `{s.status}` — already strings. |
| `types/index.ts` | `state: string`, `direction: string`. **Unused** (B29: 0 imports). Pages use `any`. Compatible with string enums; would also accept the old integers as numbers if someone typed it loosely. |

B10’s “operator sees `2`” is **false** on this host hash. Operator sees `WATCH` / `Long`.

---

## 10. Dead `SettingsController` (do not confuse with the live map)

`D:\Prop\apps\api\Controllers\SettingsController.cs` is an `[ApiController]` with `GET/PUT /api/settings`. It has **no enums**. It is **not on the wire**: `Program.cs` never calls `AddControllers` / `MapControllers`. Live `GET /api/settings` is the anonymous object at L42–47.

If someone later maps controllers **without** `AddJsonOptions` + the same converter, MVC would integer-serialize any future enum on that pipeline. Today that is a landmine, not a live bug.

SignalR client still dials `/hubs/dashboard` (`apps/web/src/api/signalr.ts`). Host has no hub (D50). Hub JSON options are irrelevant until `AddSignalR` exists.

Swagger: `AddSwaggerGen` is called; `UseSwaggerUI` is not. OpenAPI enum schema (`string` vs `integer`) was **not** captured. Do not claim Swagger documents strings without a `/swagger/v1/swagger.json` fetch.

---

## 11. Tests

`tests/Unit` + `tests/Integration`: **zero** `JsonStringEnumConverter` / `ConfigureHttpJsonOptions` / wire-format assertions. No contract test locks `"WATCH"` vs `2`. The isolated eval in `_tmp_d74_enums` is a report measurement, not a product test.

---

## 12. Stale reports (same day, earlier host)

| Report | Claim | Status vs `Program.cs` `61B1E0D1…` |
|---|---|---|
| B10 | No converter; `{t.state}` prints `2` | **STALE** |
| B29 header + §420 | No converter; wire integers; propose adding it | **STALE** on the converter. Field-name mismatches still live. |
| D21 / D30 / D06 / C34 | Converter registered L10–13 | **CONFIRMED** (this recensus) |
| A96 | Per-DTO `[JsonConverter]` + `[EnumMember]` | **Not implemented.** Global converter, no `EnumMember`. |
| A67 replay harness | Custom string-only decimal + string enums | **Different stack.** Not this host. |

---

## 13. Verdict

| Claim | Result |
|---|---|
| API uses `JsonStringEnumConverter`? | **YES** — one registration, live minimal-API JSON bag. |
| Enums on trader/trade JSON are strings? | **YES** — measured `"WATCH"` / `"Long"` / `"Short"`. |
| Integers gone from **read**? | **NO** — `AllowIntegerValues` default still accepts `2` and `"2"`. |
| Naming policy applied to enum values? | **NO** — identifiers as written. `TraderState` SCREAMING; `TradeDirection` Pascal. |
| Architecture A26 `state` tokens? | **Aligned** (`"SHADOW"`, `"WATCH"`, …). |
| Architecture A48 kill-switch tokens? | **Not produced.** `/api/risk.killSwitch` is `"None"` / `"StopNewExecution"` / `"EmergencyFlatten"`. |
| Locked by tests? | **NO.** |
| Product source changed by this agent? | **NO.** |

**Do not add a second converter. Do not hand-write MQ5. Do not treat B10/B29 integer-wire as current.** If a follow-up changes spelling (`Long` → `LONG` / `long`, or kill-switch to `STOP_NEW_EXECUTION`), that is a **new** contract change and must not be done in this report.

---

## 14. Scratch (not product)

`D:\Prop\reports\swarm\20260818\_tmp_d74_enums\` — isolated `D74EnumEval.csproj` + stdout of the serializer facts in §3–§4. Safe to delete; this markdown is the permanent record.
