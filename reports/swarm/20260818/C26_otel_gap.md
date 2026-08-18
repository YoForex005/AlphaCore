# C26 — OpenTelemetry not added (gap confirmation)

| Field | Value |
|---|---|
| Agent | C26 (senior engineer, observability gap only) |
| Date | 2026-08-18 |
| Measured at (UTC) | `2026-08-18T07:56:42.7204371Z` |
| Artifact | `D:\Prop\reports\swarm\20260818\C26_otel_gap.md` |
| Workspace | `D:\Prop` |
| Assigned question | Confirm OpenTelemetry is **not** added. Write this report. |
| Product source modified | **No.** This report is the only write. `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `D:\Prop\mt5-sdk` were not edited. |
| Law | Architecture v2 §5 (tech stack lists `OpenTelemetry`), §57 (structured logging), §58 (metric tokens) |
| Binding implementation spec | `D:\Prop\reports\swarm\20260818\A50_metrics_logging.md` |
| Pin that forbids adding OTel in the current CPM set | `D:\Prop\reports\swarm\20260818\A102_build_props.md` §6.3 |
| Relates | A06, A07, A29 T06, A50, A73, A76, A102, B07, C04, C07 |
| Supersedes | A29 T06 / A50 §0 **as a re-measure on this timestamp**. Classification is unchanged: **MISSING**. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict

**CONFIRMED: OpenTelemetry is not added to this product.**

There is no `OpenTelemetry*` NuGet package on any product `.csproj`. Restore graphs (`project.assets.json` + `project.nuget.cache`) for all ten product/test projects contain **zero** `OpenTelemetry` strings. Host `Program.cs` files never call `AddOpenTelemetry`. There is no `Meter` / `ActivitySource` / `MetricNames` / `TradingMetrics` type. There is no `/metrics` route, no OTLP exporter, no Prometheus scrape endpoint, no collector service in Compose, no `OTEL_*` environment variable, and no `OpenTelemetry` configuration section.

Architecture §5 still names OpenTelemetry as a required stack item. A50 still names the meter `TraderIntelligence` and the §58 instrument catalog. Those are **specs**, not implementations. A102 §6.3 still says **do not** add OpenTelemetry packages in the current pin set. Absence is therefore a **measured gap vs §5 / §58 / A50**, and simultaneously **compliant with the A102 pin**.

Do not greenwash the unused `Serilog.AspNetCore 8.0.2` reference, the transitive `System.Diagnostics.DiagnosticSource` library, `Microsoft.Extensions.Diagnostics*`, or the C++ `metrics_service.h` as “OTel is present.”

| Question | Measured answer | Evidence |
|---|---|---|
| Is any `OpenTelemetry*` package referenced by a product/test `.csproj`? | **No** | §3 |
| Does any restore graph contain `OpenTelemetry`? | **No** (0 matches across 10 `project.assets.json` + 10 `project.nuget.cache`) | §4 |
| Is `AddOpenTelemetry` / `UseSerilog` / `AddPrometheus` / `OtlpEndpoint` present in product C# / JSON / YAML / props? | **No** (0 hits under `src/`, `apps/`, `tests/` excluding `bin`/`obj`/`node_modules`) | §2, §5 |
| Is there a `Meter`, `ActivitySource`, or `MetricNames` type? | **No** | §5 |
| Does the API expose `GET /metrics`? | **No** | §6 |
| Does Compose run a collector / Prometheus / Jaeger / Tempo? | **No** (services: `postgres`, `redis`, `api` only) | §7 |
| Does `apps/web` depend on `@opentelemetry/*`? | **No** (`package.json` + `package-lock.json` = 0 matches) | §8 |
| Does local C++ `mt5-sdk/src` contain OpenTelemetry? | **No.** `metrics_service.h` is a **DEPRECATED** custom collector, not OTel | §9 |
| Architecture §5 / §58 / A50 obligation discharged? | **No** | §1 |
| A102 “do not add OTel in this pin set” violated? | **No** | §1.3 |

Honest one-liner: **OpenTelemetry is specified and unimplemented. The product has no OTel package, no meter, no export, no `/metrics`. That is a gap, not a PASS.**

---

## 1. Binding law (quoted)

### 1.1 Architecture §5 — tech stack (required)

`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 216–231:

```text
C#
.NET 8+ compatible stack
ASP.NET Core
.NET Worker Services
Entity Framework Core or existing proven data layer
Npgsql
Serilog
OpenTelemetry
```

`OpenTelemetry` is a **named** stack item, same rank as Serilog and Npgsql. It is not optional flavor text.

### 1.2 Architecture §58 — instrument names (required)

`# 58. Metrics` (line 2137) requires these tokens to be **exposed**. A50 §6 freezes them as the OpenTelemetry instrument names. They are **not** present as code constants or as scrape output.

MT5: `mt5_connected`, `mt5_reconnects`, `mt5_events_total`, `mt5_deals_total`, `mt5_duplicate_deals_total`, `mt5_backfill_lag`, `mt5_outbox_backlog`

Reconstruction: `reconstructed_trades_total`, `reconstruction_failures_total`, `trade_completion_latency`

Scoring: `score_requests_total`, `score_failures_total`, `prediction_latency`, `shadow_candidates`, `live_candidates`

FIX: `fix_quote_connected`, `fix_trade_connected`, `fix_logon_failures`, `fix_reconnects`, `fix_inbound_messages_total`, `fix_outbound_messages_total`, `fix_rejects_total`, `fix_business_rejects_total`, `fix_execution_reports_total`, `fix_unknown_execution_states`

Execution: `copy_intents_total`, `risk_rejections_total`, `execution_orders_total`, `execution_fills_total`, `execution_rejections_total`, `source_to_fill_latency`, `slippage`

**Count: 32 architecture tokens. Implemented as OTel instruments: 0.**

### 1.3 A50 host wiring (not implemented)

A50 §8.1 requires API composition:

```text
AddOpenTelemetry().WithMetrics(m => m
    .AddMeter("TraderIntelligence")
    .AddAspNetCoreInstrumentation()
    .AddRuntimeInstrumentation()
    .AddPrometheusExporter()   // or OTLP
)
Map /metrics  (authenticated; SuperAdmin / ops)
```

A50 §8.3 requires an `OpenTelemetry` config block (`OtlpEndpoint`, `PrometheusEnabled`). **Neither exists** in any `appsettings*.json`.

### 1.4 A102 pin (why this wave must not sneak packages in)

A102 §6.3 (line 299):

> Do **not** add Seq / Elasticsearch / OpenTelemetry packages in this pin set (A50: stdout compact JSON first). OTel is a later increment with its own versions, still on the 8.x Microsoft.Extensions line.

A102 line 406: do not add Kafka, MassTransit, OpenTelemetry, Seq, EF 10, Serilog 10, FluentValidation 12.

This report **confirms absence**. It does **not** authorize adding packages. A later increment must follow A50 names and stay on the 8.x Microsoft.Extensions line.

---

## 2. Method

1. Recurse product `.csproj` under `D:\Prop\apps`, `D:\Prop\src`, `D:\Prop\tests` (exclude `reports\`, `mt5-sdk\vendor\`, `node_modules\`, `_tmp_*`). Inventory every `PackageReference`.
2. Confirm `Directory.Packages.props`, `Packages.props`, `global.json`, `nuget.config` / `NuGet.Config`, `Directory.Build.targets` are **absent**. Read `Directory.Build.props` (no package pins).
3. Parse all ten `project.assets.json` graphs. Union of `libraries` keys = **196** ids. Filter for `OpenTelemetry|opentelemetry|prometheus|Otlp|ApplicationInsights|App.Metrics|Datadog|NewRelic|Jaeger|Zipkin|Honeycomb|Seq\.|Serilog.Sinks.OpenTelemetry` → **NONE**.
4. Grep the same ten `project.nuget.cache` files for `OpenTelemetry` → **0**.
5. Grep product `*.cs` / `*.csproj` / `*.json` / `*.yml` / `*.yaml` / `*.props` (exclude `bin`/`obj`/`node_modules`/`reports`/`vendor`) for `OpenTelemetry|AddOpenTelemetry|OTEL_|opentelemetry|OtlpEndpoint|AddPrometheus|/metrics|UseSerilog|ActivitySource|System.Diagnostics.Metrics` → **NO_PRODUCT_HITS**.
6. Second pass on product `*.cs` for `\bMeter\b|ActivitySource|AddMeter|Counter<|Histogram<|ObservableGauge|DiagnosticSource` → **NONE**.
7. Full read of `apps/api/Program.cs`, both worker `Program.cs` / `Worker.cs`, `src/Infrastructure/DependencyInjection.cs`, all `appsettings*.json`, `docker-compose.yml`, `apps/web/package.json`, API `launchSettings.json`.
8. `apps/web/package-lock.json` (128 411 bytes) search for `opentelemetry|@opentelemetry` → **0**.
9. Local C++ `D:\Prop\mt5-sdk\src` search for `OpenTelemetry|OTEL_` → **NONE**.
10. SHA-256 + byte sizes via PowerShell `Get-FileHash` / `Get-Item` at the timestamp in the header. Process was **not** launched. No scrape of a live `/metrics`.

Product C# file count (`src` + `apps` + `tests`, exclude `bin`/`obj`/`node_modules`): **79**.

---

## 3. Product PackageReference inventory (complete)

| Project | Direct packages | OpenTelemetry? |
|---|---|---|
| `apps/api/TraderIntelligence.Api.csproj` | `Microsoft.AspNetCore.SignalR.Common 8.0.4`, **`Serilog.AspNetCore 8.0.2`**, `Swashbuckle.AspNetCore 6.6.2` | **No** |
| `apps/mt5-worker/TraderIntelligence.Mt5Worker.csproj` | `Microsoft.Extensions.Hosting 8.0.1` | **No** |
| `apps/fix-worker/TraderIntelligence.FixWorker.csproj` | `Microsoft.Extensions.Hosting 8.0.1` | **No** |
| `src/Domain/TraderIntelligence.Domain.csproj` | *(none)* | **No** |
| `src/Application/TraderIntelligence.Application.csproj` | `FluentValidation 11.9.2` | **No** |
| `src/Infrastructure/TraderIntelligence.Infrastructure.csproj` | EF Design / InMemory / Npgsql 8.0.4, `StackExchange.Redis 2.8.0` | **No** |
| `src/Mt5/TraderIntelligence.Mt5.csproj` | *(none)* | **No** |
| `src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj` | *(none)* | **No** |
| `tests/Unit/TraderIntelligence.Tests.Unit.csproj` | coverlet 6.0.0, FluentAssertions 6.12.0, Test.Sdk 17.8.0, Moq 4.20.70, xunit 2.5.3 | **No** |
| `tests/Integration/TraderIntelligence.Tests.Integration.csproj` | coverlet, FluentAssertions, EF InMemory 8.0.4, Test.Sdk, xunit 2.5.3 | **No** |

`Directory.Build.props` (269 bytes, SHA-256 `5ACD33B0F8E1A8D2E66956EF2B04A11E321661A5E3297F8F5C13051345562DD0`) sets `LangVersion` / `Nullable` / `ImplicitUsings` / `Deterministic` only. **No** `PackageReference`, **no** CPM, **no** OTel.

There is **no** `Directory.Packages.props`. OTel cannot be hiding in a central pin file that does not exist.

### 3.1 Serilog is not OpenTelemetry

API references `Serilog.AspNetCore 8.0.2`. `Program.cs` never calls `UseSerilog` / `AddSerilog` (0 hits in product C#). That is A29 **T05** (`EXISTS_NEEDS_REFACTOR` — dead package), not T06. Transitive Serilog graph from the API restore:

- `Serilog/3.1.1`
- `Serilog.AspNetCore/8.0.2`
- `Serilog.Extensions.Hosting/8.0.0`
- `Serilog.Extensions.Logging/8.0.0`
- `Serilog.Formatting.Compact/2.0.0`
- `Serilog.Settings.Configuration/8.0.2`
- `Serilog.Sinks.Console/5.0.0`
- `Serilog.Sinks.Debug/2.0.0`
- `Serilog.Sinks.File/5.0.0`

**Not present:** `Serilog.Sinks.OpenTelemetry`, `Serilog.Sinks.Grafana.Loki` used as an OTel stand-in, Seq, Elasticsearch.

Workers have **no** Serilog package at all.

---

## 4. Restore graphs — 196 libraries, 0 OpenTelemetry

Union of `libraries` keys from:

- `D:\Prop\apps\api\obj\project.assets.json` (108 934 B, SHA-256 `6F04CB981CB73F58A7A7C5865AC46C16C48A434D6EA7DC7D87EF874E2A1A34CD`)
- `D:\Prop\apps\mt5-worker\obj\project.assets.json` (98 397 B, `EED0832A4E3C5F233B860600743662F381F3E8211E8365C3C93CE9583B17A9B6`)
- `D:\Prop\apps\fix-worker\obj\project.assets.json` (99 164 B, `310F742B9C8A34137BF83013521A97D9036A85882B170227678474C7F31A008B`)
- `D:\Prop\src\Infrastructure\obj\project.assets.json` (120 008 B, `FE26769B1FF3280BFD706FA2927F435DE7E237331E372758713F602AB5333B1F`)
- `D:\Prop\src\Application\obj\project.assets.json`
- `D:\Prop\src\Domain\obj\project.assets.json`
- `D:\Prop\src\Mt5\obj\project.assets.json`
- `D:\Prop\src\Fix.CTrader\obj\project.assets.json`
- `D:\Prop\tests\Unit\obj\project.assets.json`
- `D:\Prop\tests\Integration\obj\project.assets.json`

`OpenTelemetry` substring matches in those ten assets files: **0**. Same for the ten `project.nuget.cache` files: **0**.

OTel-like filter (`OpenTelemetry`, `prometheus`, `Otlp`, Application Insights, App.Metrics, Datadog, New Relic, Jaeger, Zipkin, Honeycomb, Seq, `Serilog.Sinks.OpenTelemetry`): **NONE**.

### 4.1 Do not confuse these restore entries with OpenTelemetry

| Library in graph | What it actually is |
|---|---|
| `System.Diagnostics.DiagnosticSource/4.3.0` and `/8.0.0` | BCL diagnostics used by EF / MEL. **Not** the OpenTelemetry SDK. Product C# never constructs an `ActivitySource`. |
| `Microsoft.Extensions.Diagnostics/8.0.1` and `.Abstractions` | .NET 8 hosting metrics/health abstractions. **Not** wired. **Not** OTLP. |
| `Microsoft.OpenApi/1.6.14` | Swashbuckle transitive. Name collision only. |

If a later reader greps `DiagnosticSource` in `obj/` and claims “telemetry exists,” that is a false positive.

---

## 5. Host and DI — no composition

Hashed at measure time:

| Path | Bytes | SHA-256 |
|---|---:|---|
| `D:\Prop\apps\api\Program.cs` | 4658 | `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` |
| `D:\Prop\apps\mt5-worker\Program.cs` | 859 | `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` |
| `D:\Prop\apps\fix-worker\Program.cs` | 859 | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` |

`AddTraderIntelligence` registers EF (in-memory or Npgsql), two fake MT5 connectors, `EfTradingStore`, `EfDashboardQueries`, `TradeReconstructor`, `BaselineScorer`, `DealIngestionService`, `ReconstructionScoringService`. It does **not** register a `Meter`, `IMetrics`, exporter, redactor, or activity source.

API `Program.cs` composition (actual): `CreateBuilder` → `AddTraderIntelligence` → JSON enum converter → Swagger → open CORS → map demo routes → `EnsureCreated` + `DemoSeeder` → `Run`. Missing vs A50 §8.1: `UseSerilog`, `AddOpenTelemetry`, `AddMeter("TraderIntelligence")`, Prometheus/OTLP exporter, authenticated `/metrics`, `/health/live`, correlation middleware.

Workers: `Host.CreateApplicationBuilder` → `AddTraderIntelligence` → `AddHostedService<Worker>` → seed → `Run`. Default MEL console logger. No OTel resource `service.name=mt5-worker` / `fix-worker`.

`launchSettings.json` for API / both workers: **0** matches for `OTEL_`, `OpenTelemetry`, or hosting-startup assemblies.

`appsettings.json` / `appsettings.Development.json` on all three hosts: MEL `Logging:LogLevel` only (API also has empty `ConnectionStrings:TraderIntelligence` and a `CTrader` block). **No** `Serilog` block. **No** `OpenTelemetry` block. **No** `OtlpEndpoint`. **No** `PrometheusEnabled`.

---

## 6. HTTP surface — no `/metrics`

Live maps in `apps/api/Program.cs` (C04 hash unchanged on this pass):

| Method | Path | Metrics? |
|---|---|---|
| GET | `/health` | stub `{ status, utc }` — **not** A50 `/health/live`, **not** a metric dump |
| GET | `/api/health` | hardcoded demo JSON (`mt5Connections`, `fixSessions`, `database`, `redis`, `outboxBacklog`) — **not** A26 §6.14 live gauges |
| GET | `/ready` | counts `Brokers` after `EnsureCreated` |
| GET | `/api/risk/status`, `/api/reconciliation/status`, `/api/settings`, `/api/overview`, `/api/brokers`, `/api/groups`, `/api/traders`, `/api/traders/{broker}/{login}`, `/api/fix/sessions`, `/api/risk`, `/api/trades` | dashboard / stubs |
| POST | `/api/ops/resync` | demo ingestion |

**Absent:** `GET /metrics`, `GET /health/live`, Prometheus content-type, authenticated metrics gate.

`/api/health` returning `healthy = true` for MT5/FIX is a **demo lie**, not an OTel gauge. Do not treat it as `mt5_connected` / `fix_quote_connected`.

---

## 7. Compose / deploy — no collector

`D:\Prop\docker-compose.yml` — 687 bytes, SHA-256 `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` (same as C12).

Services: `postgres`, `redis`, `api`. Grep for `otel|jaeger|zipkin|prometheus|collector|tempo|grafana` (case-insensitive): **no matches**.

No `OTEL_EXPORTER_OTLP_ENDPOINT`, no sidecar, no scrape config. `services/` at repo root is an **empty** directory (no Python/OTel collector).

---

## 8. Web frontend — no JS OTel

`D:\Prop\apps\web\package.json` — 739 bytes, SHA-256 `F76288B73111845848A5961BFEBEE40B887EAD40E2E35C5773D149443432B7D6`.

Dependencies: `react`, `react-dom`, `react-router-dom`, `@tanstack/react-query`, `axios`, `recharts`, `@microsoft/signalr`. Dev: Vite/TS/Tailwind/PostCSS.

`package-lock.json` — 128 411 bytes, SHA-256 `72A7570A2E43C80146482FC3701E727ABB6BABEEAC08EFC032031AAE0CB4D7BE`. Matches for `opentelemetry` / `@opentelemetry`: **0**.

Architecture does not require browser OTel in v1. Recorded so a later `npm i @opentelemetry/sdk-trace-web` cannot be back-dated.

---

## 9. C++ `metrics_service.h` is not OpenTelemetry

`D:\Prop\mt5-sdk\src\services\metrics_service.h` is a **custom** singleton (`MetricsService`) with `terminal_*` / dealer / `propfirm_*`-era names and login-unsafe historical labels. Local `mt5-sdk/src` has **zero** `OpenTelemetry` / `OTEL_` tokens.

A50 §0 / A03 / A07 / A73 classify those C++ names **DEPRECATED for this product**. They must **not** be copied into the C# meter. They are **not** evidence that OTel was added.

Vendor false positive (do not cite as OTel): `INDUSTRY_REIT_HOTEL_MOTEL = 406` in MetaQuotes SDK symbol enums.

---

## 10. Classification vs prior reports

| ID | Capability | Class | Still true on this pass? |
|---|---|---|---|
| A29 T06 | OpenTelemetry | **MISSING** — no package, no code | **Yes** (re-measured) |
| A50 §0 | OpenTelemetry packages | **MISSING** | **Yes** |
| A50 §0 | §58 instruments | **MISSING** — no `Meter`, no Prometheus/OTLP | **Yes** |
| A06 | API does not reference OTel | **Yes** | **Yes** |
| A07 / B07 / C07 | Workers have no OTel | **Yes** | **Yes** |
| A73 | §58 / §36 histograms | **MISSING** | **Yes** |
| A102 | Do not add OTel in current pin set | pin | **Honored** (packages still absent) |

A29 T13 (“no `apps/web`”) is **stale**. Web exists now and still has no OTel. T06 is **not** stale.

---

## 11. What a later increment must add (not done here)

When a wave is **authorized** to leave the A102 pin, follow A50 — do not invent dotted names (`mt5.connected`) or `ti_` prefixes. Minimum product deltas (report only; **not implemented**):

| Surface | Required |
|---|---|
| Packages (8.x Microsoft.Extensions line) | `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Runtime`, exporter (`OpenTelemetry.Exporter.Prometheus.AspNetCore` **or** `OpenTelemetry.Exporter.OpenTelemetryProtocol`). Versions chosen in that increment, not here. |
| Meter | `TraderIntelligence` |
| Constants | A50 `MetricNames` = exact §58 set; additive names in `MetricNames.Extended` |
| API | `AddOpenTelemetry().WithMetrics(...)`; authenticated `GET /metrics`; anonymous `/health/live` **without** a metric dump |
| Workers | same meter; `service.name` = `mt5-worker` / `fix-worker`; OTLP or loopback Prometheus |
| Config | A50 §8.3 `OpenTelemetry` block; no secrets |
| Cardinality | A50 §7 allow-list only — never `source_login` / `cl_ord_id` / passwords on attributes |
| Tests | A50 §10 (set equality vs §58; reject login tags) |

Do **not** implement that in a “just add the package” drive-by. A102 still forbids it in the current pin set.

---

## 12. False friends checklist (so this report cannot be misread)

| Artifact | OTel? |
|---|---|
| `Serilog.AspNetCore 8.0.2` on API (unwired) | **No** |
| MEL `Logging` in appsettings | **No** |
| `System.Diagnostics.DiagnosticSource` transitive | **No** |
| `Microsoft.Extensions.Diagnostics*` transitive | **No** |
| `Microsoft.OpenApi` | **No** |
| `/api/health` stub JSON | **No** |
| C++ `MetricsService` | **No** (DEPRECATED custom) |
| Vendor `HOTEL_MOTEL` enum | **No** |
| Architecture / A50 / A76 mentioning OpenTelemetry | **Spec text only** |
| This file | **Report only** |

---

## 13. Files hashed this pass (product, non-`bin`/`obj`)

| Path | Bytes | SHA-256 |
|---|---:|---|
| `D:\Prop\Directory.Build.props` | 269 | `5ACD33B0F8E1A8D2E66956EF2B04A11E321661A5E3297F8F5C13051345562DD0` |
| `D:\Prop\Mt5TraderIntelligence.sln` | 7019 | `AD5030070166D81EE478B632BCE3F381F2C9D3FFBBC6EB0FDD407047C5B5A7B4` |
| `D:\Prop\docker-compose.yml` | 687 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` |
| `D:\Prop\apps\api\Program.cs` | 4658 | `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` |
| `D:\Prop\apps\api\appsettings.json` | 431 | `8DCE4CBECDD1F8E7B03DDF1C25430BACCD05795D64B19798A6B0CDAACE85902B` |
| `D:\Prop\apps\api\appsettings.Development.json` | 127 | `73F95F9E0CEB205FC1C4DC50C07697FCFA29D7087868C2AEF1D504CB38C771EC` |
| `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` | 840 | `E0321028B0E12EEFE97A9BE2D0A08E8E8F89B819CCA4403D301B76A90C56B91C` |
| `D:\Prop\apps\mt5-worker\Program.cs` | 859 | `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` |
| `D:\Prop\apps\mt5-worker\Worker.cs` | 1882 | `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` |
| `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj` | 856 | `D7572CBFF273089587D4A68B2A95EB1933C2DF388177F8665BE7C3CC338B5DB4` |
| `D:\Prop\apps\fix-worker\Program.cs` | 859 | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` |
| `D:\Prop\apps\fix-worker\Worker.cs` | 1971 | `B48033A5A13C56DB747D3C9F0B94E36CB8DC2866FBCF3789A62C3D7B318B0D48` |
| `D:\Prop\src\Domain\TraderIntelligence.Domain.csproj` | 218 | `E151F959964EB450A5B86B72765E3F9C505645FA9516EAE485743D2B43911C8E` |
| `D:\Prop\src\Application\TraderIntelligence.Application.csproj` | 433 | `44E3448AE56A9D79BF562F6D68B6CC52915E6B334C3F49D7AE9E9C2313AA9DE2` |
| `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | 1035 | `4DABF29CA934261CFC46C72514CB7AA04D5E8F9CC8FFAC1BA051BF0CD0668EED` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` |
| `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj` | 419 | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` |
| `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` | 419 | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` |
| `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj` | 1113 | `EB7A4ECA27D4953313F58129C6494BE556AE616FDB9260DCA1112D4C2FEC7F50` |
| `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` | 1328 | `E749992347A22BB8241B76DA8A9008CFCA2C74F567C070A64D7B7B79B4F6E4F4` |
| `D:\Prop\apps\web\package.json` | 739 | `F76288B73111845848A5961BFEBEE40B887EAD40E2E35C5773D149443432B7D6` |
| `D:\Prop\apps\web\package-lock.json` | 128411 | `72A7570A2E43C80146482FC3701E727ABB6BABEEAC08EFC032031AAE0CB4D7BE` |
| Worker `appsettings.json` (mt5 + fix, identical) | 137 | `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` |

Worker `appsettings.json` hash matches A08 / B25 / C07 (logging-only). API `Program.cs` hash matches C04 (weatherforecast gone; still no OTel).

---

## 14. Close

**OpenTelemetry is not in the product.** Architecture §5 / §58 / A50 remain **MISSING**. A102’s “do not add it yet” pin is **intact**. Product source was not modified.

Re-measure before claiming otherwise: `PackageReference` on all ten `.csproj`, `OpenTelemetry` in every `project.assets.json`, and `AddOpenTelemetry` in host `Program.cs`. Until those three are true **and** `/metrics` or OTLP actually exports the 32 §58 names, this gap stands.
