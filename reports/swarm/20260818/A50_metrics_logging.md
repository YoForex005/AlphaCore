# A50 — Serilog enrichers, central redaction, OpenTelemetry metric names (§57–§58)

| Field | Value |
|---|---|
| Agent | A50 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A50_metrics_logging.md` |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§5, 27, 36, 52, 55–58, 62 |
| Supporting specs | A06, A07, A19, A23, A25, A26, A27 |
| Official FIX | cTrader RoE Logon (`35=A`) tags **553 / 554**; FIX 4.4 dictionary `FIX44.xml` Logon + UserRequest |
| Product source edited | **No** |

This is a **binding implementation spec** for logging and metrics. It does not implement code. When a later wave wires Serilog / OpenTelemetry, it must follow this file. Do not invent parallel metric names. Do not log FIX authentication tags that contain passwords.

---

## 0. Verdict (honest)

| Capability | Classification | Evidence |
|---|---|---|
| Structured Serilog on API | **MISSING** | `apps/api` references `Serilog.AspNetCore 8.0.2` and never calls `UseSerilog` (`Program.cs` is still weatherforecast) |
| Serilog on workers | **MISSING** | `apps/mt5-worker` and `apps/fix-worker` have only `Microsoft.Extensions.Hosting`; default MEL template logger |
| OpenTelemetry packages | **MISSING** | no `OpenTelemetry*` package on any product csproj |
| Central redaction | **MISSING** | no enricher, no destructuring policy, no FIX wire sanitizer |
| §57 identifier properties | **MISSING** | no `LogContext` / activity baggage for `correlation_id` … `fix_session` |
| §58 instruments | **MISSING** | no `Meter`, no Prometheus / OTLP export |
| C++ `metrics_service.h` names (`terminal_*`) | **DEPRECATED** for this product | A03 / A07 — different tree, Redis-fast-outbox era |
| QuickFIX default `FileLog` / `ScreenLog` | **UNSAFE if enabled** | would persist raw `554=<password>` |

`CTraderFixOptions.Password` already exists (`D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`) and is documented “Must never be logged.” That comment is not a control. The first `LogInformation("{@Options}", options)` or QuickFIX `FileLogPath` will violate §57.

---

## 1. Binding law (quoted, not paraphrased into something weaker)

### 1.1 §57 Logging

Use structured logging.

Every relevant event should include available identifiers:

```text
correlation_id
broker_id
source_login
source_trade_id

copy_intent_id
risk_decision_id
execution_intent_id

cl_ord_id
cserver_order_id
destination_position_id

fix_session
```

**Never log authentication tags containing passwords.**

Redact sensitive values centrally.

### 1.2 §58 Metrics — instrument names are frozen

These tokens are the **OpenTelemetry instrument names**. Do not rename to dotted OTel-style (`mt5.connected`) in v1. Do not prefix `ti_`. Do not copy C++ `terminal_*`. Dashboard `GET /api/v1/health` (A26 §6.14) projects these same names as camelCase JSON fields.

### 1.3 Adjacent hard laws

| Source | Rule |
|---|---|
| §52 | Never show FIX password (UI). Logs have the same bar. |
| §55 | Never expose MT5 / proxy / cTrader / FIX / DB / Redis passwords. |
| A19 rec 5 | Reject logging of tags 553/554 and any `Password=` field before wiring FIX. |
| A25 §3.3 / test 12 | Never log 554. Password never appears in structured log output. |
| A25 §7.8 | Same §57 identifier list, plus `fencing_token`. |
| A26 §3 | Denylist applies to JSON, SignalR, CSV, **and** `audit_logs` blobs. |
| A18 / `metrics_service.h` | Never put login / user / request id on a metric label. |
| §27 | QUOTE and TRADE have **independent** metrics and logs. |

---

## 2. Target layout (no new csproj)

Keep observability in existing projects. Do **not** add `TraderIntelligence.Observability`.

```text
src/Infrastructure/Observability/
  Logging/
    LogPropertyNames.cs
    TradingLogScope.cs
    ServiceInstanceEnricher.cs
    CorrelationIdEnricher.cs
    ActivityBaggageEnricher.cs
    SensitiveDataEnricher.cs          // last enricher: mutate properties in place
    SensitiveDestructuringPolicy.cs
    RedactingConsoleFormatter.cs      // belt: message template + rendered message
  Redaction/
    RedactionPolicy.cs                // single source of truth
    SecretNameCatalog.cs
    FixPasswordTagCatalog.cs          // HARD LAW
    FixWireRedactor.cs
    ConnectionStringRedactor.cs
    ExceptionRedactor.cs
  Metrics/
    TradingMeters.cs                  // Meter("TraderIntelligence")
    MetricNames.cs                    // const strings == §58
    MetricAttributes.cs
    TradingMetrics.cs                 // instruments + helpers

src/Fix.CTrader/Logging/
  RedactingQuickFixLog.cs             // QuickFix.ILog
  RedactingQuickFixLogFactory.cs      // never FileLogFactory / ScreenLogFactory

apps/api/Program.cs                   // composition only: UseSerilog + OTel
apps/mt5-worker/Program.cs
apps/fix-worker/Program.cs
```

`Fix.CTrader` already references Application + Domain. Give it a project reference to Infrastructure **or** keep `FixWireRedactor` duplicated-free by moving the redactor interface to Application:

```text
src/Application/Observability/ILogRedactor.cs
src/Application/Observability/ITradingMetrics.cs
```

Ports in Application, implementations in Infrastructure, QuickFIX adapter in Fix.CTrader. Domain stays package-free (A01).

---

## 3. Serilog pipeline

### 3.1 Packages (add when implementing; do not add in this wave)

| Package | Where | Why |
|---|---|---|
| `Serilog.AspNetCore` 8.0.2 | API (already referenced, unused) | host integration |
| `Serilog.Extensions.Hosting` | both workers | `UseSerilog` |
| `Serilog.Settings.Configuration` | all hosts | `appsettings` sink/level |
| `Serilog.Formatting.Compact` | all hosts | `RenderedCompactJsonFormatter` / `CompactJsonFormatter` |
| `Serilog.Enrichers.Environment` | all hosts | `MachineName` |
| `Serilog.Enrichers.Thread` | all hosts | `ThreadId` (debug only; not a §57 id) |
| `Serilog.Sinks.Console` | all hosts | stdout → collector |
| `Serilog.Sinks.File` | optional, **off by default in prod** | local lab only; still redacted |

Do **not** add Seq / Elasticsearch sinks until a collector exists. First useful version: compact JSON on stdout.

### 3.2 Bootstrap order (mandatory)

```text
1. Create RedactionPolicy (static, no IConfiguration dump)
2. Create LoggerConfiguration
     .Destructure.With<SensitiveDestructuringPolicy>()
     .Enrich.FromLogContext()
     .Enrich.With<ServiceInstanceEnricher>()
     .Enrich.With<CorrelationIdEnricher>()
     .Enrich.With<ActivityBaggageEnricher>()
     .Enrich.With<SensitiveDataEnricher>()     // LAST enricher
     .WriteTo.Console(new CompactJsonFormatter())
3. Log.Logger = cfg.CreateLogger()
4. UseSerilog()
5. Only then Bind options / AddDbContext / AddQuickFix
```

If a host logs during options bind (connection string, `CTraderFixOptions`), redaction must already be installed. A06: “Wire Serilog + request `correlation_id` **before** any FIX/MT5 options exist so passwords never hit console.”

### 3.3 Levels

| Area | Default | Notes |
|---|---|---|
| Host / lifetime | Information | |
| ASP.NET request summary | Information | status, route, elapsed; **no** query string dump |
| EF Core `Microsoft.EntityFrameworkCore.Database.Command` | Warning in prod | `EnableSensitiveDataLogging` **off** outside Development |
| FIX admin (Logon/Logout/Heartbeat/TestRequest) | Information (events), Debug (wire) | wire always redacted |
| FIX application (D/8/V/W/X/y/j) | Debug (wire), Information (state change) | |
| Risk decision | Information | include `primary_reason`, not raw options |
| Unredacted-path attempt | **Error** + drop payload | fail closed (see §5.6) |

Production default: `Information`. `Debug` FIX wire is opt-in via `Logging:FixWire`. Even at `Verbose`, password tags stay redacted.

### 3.4 Formatter

Use **compact JSON** (Serilog CLEF). Property names on the wire are the §57 snake_case tokens (not PascalCase) so Loki/Elastic queries match the architecture and the Postgres column names.

Example (illustrative; values fake):

```json
{
  "@t": "2026-08-18T12:00:00.123Z",
  "@l": "Information",
  "@mt": "Execution report accepted",
  "service": "fix-worker",
  "service_instance": "fix-worker-01",
  "environment": "staging",
  "correlation_id": "b7c1e2d3-0000-4000-8000-000000000009",
  "broker_id": "3a0c9b12-1111-4aaa-8bbb-0123456789ab",
  "source_login": 6100421,
  "source_trade_id": "pos:881122",
  "copy_intent_id": "...",
  "risk_decision_id": "...",
  "execution_intent_id": "...",
  "cl_ord_id": "CI-...",
  "cserver_order_id": "101",
  "destination_position_id": "721:101",
  "fix_session": "TRADE",
  "msg_type": "8",
  "exec_type": "F",
  "ord_status": "2"
}
```

No `fix_raw` field unless it has passed `FixWireRedactor`. Prefer structured fields over raw.

---

## 4. Serilog enrichers

### 4.1 Property catalog (binding names)

`LogPropertyNames` constants — **exact spelling**:

| Constant | Property | Type | Required when |
|---|---|---|---|
| `CorrelationId` | `correlation_id` | UUID string | every HTTP request; every worker unit of work that has one |
| `BrokerId` | `broker_id` | UUID string | any MT5 / reconstruction / score / copy path |
| `SourceLogin` | `source_login` | ulong | trader-scoped work |
| `SourceTradeId` | `source_trade_id` | string | reconstructed trade / source position ticket |
| `CopyIntentId` | `copy_intent_id` | UUID | shadow or live intent |
| `RiskDecisionId` | `risk_decision_id` | UUID | after risk engine |
| `ExecutionIntentId` | `execution_intent_id` | UUID | after approve |
| `ClOrdId` | `cl_ord_id` | string | FIX order path |
| `CserverOrderId` | `cserver_order_id` | string | tag 37 `OrderID` |
| `DestinationPositionId` | `destination_position_id` | string | tag 721 `PosMaintRptID` |
| `FixSession` | `fix_session` | `QUOTE` \| `TRADE` | any FIX I/O |
| `FencingToken` | `fencing_token` | string (lease id, **not** a secret) | TRADE send / lease events (A25) |

Plus host-wide (not in §57 but required for ops):

| Property | Source |
|---|---|
| `service` | `api` / `mt5-worker` / `fix-worker` |
| `service_instance` | `HOST_NAME` + process id, or `SERVICE_INSTANCE_ID` |
| `environment` | `ASPNETCORE_ENVIRONMENT` / `DOTNET_ENVIRONMENT` |
| `machine_name` | Serilog environment enricher |

**Omit** a property when it is unknown. Do not write empty strings or `00000000-0000-0000-0000-000000000000`.

### 4.2 `ServiceInstanceEnricher`

Adds `service`, `service_instance`, `environment`. Reads `IHostEnvironment` + optional env `SERVICE_INSTANCE_ID`. One instance per process. No secrets.

### 4.3 `CorrelationIdEnricher`

**API:**

1. Accept inbound `X-Correlation-Id` if it is a UUID; otherwise mint `Guid.NewGuid()`.
2. Store on `HttpContext.Items` and `Activity.Current.SetBaggage("correlation_id", …)`.
3. Echo response header `X-Correlation-Id`.
4. Push `LogContext` for the request scope.

**Workers:** each outbox row, MT5 event, or FIX application message starts a scope. Prefer the inbound `correlation_id` on the outbox payload; if absent, mint one and persist it on the row so retries share the id.

W3C `traceparent` is complementary, not a substitute. If an `Activity` exists, also set `trace_id` / `span_id` from `Activity.Current` (OTel already does this when the OTel Serilog enricher is added later). v1 may skip the extra package and copy `Activity.TraceId` in `ActivityBaggageEnricher`.

### 4.4 `ActivityBaggageEnricher`

Copies **allow-listed** baggage keys into the log event: the §57 names only. Ignores unknown baggage (prevents a future caller from stuffing `password` into baggage and surviving).

### 4.5 `TradingLogScope` (application API)

Call-site helper so workers do not sprinkle magic strings:

```csharp
using var _ = TradingLogScope.Begin(new TradingLogScopeState
{
    CorrelationId = id,
    BrokerId = brokerId,
    SourceLogin = login,
    FixSession = FixSessionQualifier.Trade, // renders "TRADE"
    ClOrdId = clOrdId,
    // …
});
```

Implementation: `LogContext.Push(ILogEventEnricher)` with one enricher that adds only set fields.

Map `FixSessionQualifier` (`Quote=0`, `Trade=1` in `src/Domain/Enums/FixSessionQualifier.cs`) to the RoE strings `QUOTE` / `TRADE` (uppercase). Do not log the enum integer.

### 4.6 What enrichers must **never** add

- `CTraderFixOptions` / `IConfiguration` / `IOptionsSnapshot<T>` as a property
- Connection strings, `Password`, `CTRADER_FIX_PASSWORD`, proxy auth
- Raw FIX `Message` / `GetRawData()` / `ToString()` without `FixWireRedactor`
- Metric labels that include login / ClOrdID (logs may include those identifiers; **metrics may not**)

`source_login` on a **log** is required by §57. `source_login` on a **metric attribute** is forbidden (cardinality + identity leak). See §7.

---

## 5. Central redaction (hard law)

One policy object. Every sink, every exception, every audit blob, every QuickFIX `ILog`, every OTel span attribute goes through it. Call-site “please don’t log the password” is not accepted as a control (A19: `logger.h` relied on call sites — that is **not** enough here).

### 5.1 FIX password tags — never log

cTrader production Logon uses **plaintext tag 554**. Official RoE example (do not treat the sample password as a real secret):

```text
8=FIX.4.4|9=126|35=A|…|553=12345|554=passw0rd!|10=131|
```

`EncryptMethod (98) = 0` (RoE: transport TLS only). Generic FIX 4.4 Logon still allows `95/96` as encrypted password payload. UserRequest (`35=BE`) allows `554`, `925`, `95`, `96`.

**Deny-list of FIX tags. Values are replaced. Tags remain so operators can see that auth was present.**

| Tag | Name | Action | Authority |
|---|---|---|---|
| **554** | `Password` | **Always redact** | cTrader RoE Logon; architecture §57 |
| **925** | `NewPassword` | **Always redact** | FIX 4.4 UserRequest (`FIX44.xml`) |
| **96** | `RawData` | **Always redact** | FIX 4.4 Logon + UserRequest password/raw secret |
| **95** | `RawDataLength` | Keep (integer length only) | not a secret |
| **1401** | `EncryptedPassword` | **Always redact** | FIX 5.x; future-proof if a dictionary upgrade appears |
| **1402** | `EncryptedPasswordLen` | Keep (length) | |
| **1403** | `EncryptedNewPassword` | **Always redact** | FIX 5.x |
| **1404** | `EncryptedNewPasswordLen` | Keep (length) | |
| **1400** | `EncryptedPasswordMethod` | Keep (enum) | not a secret |

**Tag 553 `Username`:** this is the numeric trader login (RoE), not a password. Architecture §57 requires `source_login` as a structured field. A19/A25 asked to redact 553 **on the raw wire dump** so a pasted Logon line cannot be replayed as a credential pair.

| Surface | 553 Username | 554 Password |
|---|---|---|
| Raw FIX wire string (`fix_raw`, QuickFIX OnIncoming/OnOutgoing) | replace value with `***` | replace value with `***` |
| Structured log property `source_login` / destination account | **allowed** (identifier) | n/a |
| Metric attributes | **forbidden** | **forbidden** |
| Dashboard JSON | allowed as trader login; **never** as FIX password | **never** |
| `CTraderFixOptions.Password` / env `CTRADER_FIX_PASSWORD` | n/a | **never** |

Replacement format (stable for tests):

```text
554=***
553=***
96=***
925=***
1401=***
1403=***
```

Do **not** use the original value’s length as padding (that leaks password length). Do **not** hash the password into the log (hashes of low-entropy broker passwords are still secrets).

### 5.2 `FixWireRedactor` algorithm

Input: `ReadOnlySpan<char>` or `string` that may contain SOH (`\u0001`), display `|`, `^A`, or the literal `\x01`.

Rules:

1. Normalize delimiters to a working copy (do not mutate the original buffer that QuickFIX still needs to send).
2. Split on SOH / `|`.
3. For each field `tag=value`, if `tag` is in the redact set, emit `tag=***`.
4. Also match **names** if a logger printed `Password=…` or `NewPassword=…` or `EncryptedPassword=…` (QuickFIX `Message.ToString()` is tag-numeric; humans and exception messages are not).
5. Re-join with the **same delimiter style as the input** (keep `|` if the caller used `|`).
6. Recompute nothing about BodyLength/CheckSum — this string is for logs only. Never send the redacted copy to the socket.
7. If parsing fails (no `=`, binary garbage), **drop the entire string** and return `fix_raw=[UNPARSEABLE_REDACTED]`. Fail closed.

Regex fallback (after structured split), case-sensitive for tags, case-insensitive for names:

```regex
(?<![0-9])(?<tag>554|925|96|1401|1403)=(?<val>[^\x01|]*)
(?i)(?<name>Password|NewPassword|EncryptedPassword|EncryptedNewPassword|RawData)=(?<val>[^\x01;,&\s]*)
```

Unit tests **must** use the official RoE Logon line (A32) and assert `passw0rd!` is absent and `554=***` is present. Also test SOH-delimited (`\u0001`) and `|`-delimited copies.

### 5.3 Property / key denylist (`SecretNameCatalog`)

Match after normalizing the key: lowercase, strip `_`, `-`, `:`.

Always redact if the normalized name **equals** or **ends with** one of:

```text
password
passwd
pwd
secret
newpassword
rawdata
encryptedpassword
encryptednewpassword
connectionstring
connstr
authorization
proxyusername          // A26 denylist; username+password pair
proxypassword
apikey
accesstoken
refreshtoken
privatekey
clientsecret
investorpassword
```

Exact env / options keys (normalized contains):

```text
ctraderfixpassword
mt5password
mt5starwavefxpassword
achieverproxypassword
mt5apikey
mt5passwordencryptionkey
```

Value replacement: `***`. For connection strings, run `ConnectionStringRedactor` which preserves non-secret keys:

```text
Host=pg;Port=5432;Database=ti;Username=ti;Password=***
```

### 5.4 `SensitiveDataEnricher` (last enricher)

Walk `LogEvent.Properties`:

| Property type | Action |
|---|---|
| Name on denylist | replace with scalar `***` |
| Scalar string | run `FixWireRedactor` if it looks like FIX (`8=FIX.` or contains `\u00018=` or `|35=`); run connection-string redactor if it contains `Password=` / `pwd=` |
| Structure / dictionary | recurse |
| Sequence | recurse |
| Exception (via `LogEvent.Exception`) | see §5.5 |

This is the backstop for `"{@options}"`, `"Failed {Message}"`, and MEL `ILogger` payloads that bypass the destructuring policy.

### 5.5 Exceptions and EF / Npgsql / Redis

| Source | Risk | Control |
|---|---|---|
| `NpgsqlException` / `PQerrorMessage` | URI with password (A19-08) | `ExceptionRedactor` copies type + rewritten `Message` / `ToString()` |
| `RedisConnectionException` | may echo the endpoint + AUTH | strip `password=` |
| QuickFIX `ConfigError` / `FieldNotFound` | rarely includes 554; still run wire redactor | |
| `OptionsValidationException` | may print bound values | never validate-by-logging the password field; redact |

Never log `ex.ToString()` without passing it through the redactor. Serilog’s default exception enricher must be wrapped.

### 5.6 Fail closed

| Situation | Behaviour |
|---|---|
| Redactor throws | swallow; emit a **new** event `redaction_failed` with `msg_type` / `seq` only; **do not** attach the original string |
| Wire logger asked to write Logon (`35=A`) | always go through redactor even if `Logging:FixWire=false` (the Information “logon sent” event still must not include 554) |
| `EnableSensitiveDataLogging` | forced off when `environment != Development` |
| QuickFIX `FileLogPath` / `ScreenLog` | **forbidden** in product session settings. Factory constructor throws if those keys are present. |

### 5.7 Surfaces that share this policy

| Surface | How |
|---|---|
| Serilog sinks | enricher + destructuring + formatter |
| QuickFIX `ILog` | `RedactingQuickFixLog` calls `FixWireRedactor` then `ILogger` |
| ASP.NET request logging | do not log `Authorization`, `Cookie`; do not log raw body |
| `audit_logs.before` / `.after` | same denylist as A26 §3; FIX password keys rejected on write (`422 SECRET_FIELD_REJECTED`) |
| OpenTelemetry span attributes | allow-list only (the §57 names + `msg_type` + `exec_type`). Never `fix.raw` unredacted. |
| Metric attributes | §7 allow-list; no secrets, no logins |
| Health JSON (A26 §6.14) | already has no credential fields; keep it that way |
| `IConfiguration.AsEnumerable()` debug dumps | **banned** |

### 5.8 QuickFIX adapter (Fix.CTrader)

Product package today: `QuickFix.Net 1.11.2` (`TraderIntelligence.Fix.CTrader.csproj`). Do not enable its file logger.

```text
OnIncoming(string)  → redact → Log.Debug("FIX inbound {fix_session} {msg_type} {seq} {fix_raw}")
OnOutgoing(string)  → redact → Log.Debug("FIX outbound …")
OnEvent(string)     → redact names → Log.Information("FIX event …")
```

Extract `msg_type` (35) and `seq` (34) **before** redaction (they are not secrets). Never extract 554.

Logon Information event shape:

```text
FIX Logon sent
  fix_session=TRADE
  sender_comp_id=live.pepperstone.1369850
  target_comp_id=cServer
  reset_seq=Y
  encrypt_method=0
  heart_bt_int=30
  username_present=true
  password_present=true
```

`username_present` / `password_present` are booleans. **No values.**

---

## 6. OpenTelemetry metric names

### 6.1 Meter

| Item | Value |
|---|---|
| Meter name | `TraderIntelligence` |
| Version | assembly informational version |
| Export | OTLP gRPC/HTTP to collector **or** Prometheus scrape |
| First useful version | Prometheus on API `GET /metrics` (authenticated, not the anonymous `/health/live`) + worker OTLP push or localhost scrape |
| Resource attributes | `service.name`, `service.instance.id`, `deployment.environment` |

One meter. Instrument names already carry the subsystem prefix (`mt5_`, `fix_`, …).

Do **not** use the C++ names in `D:\Prop\mt5-sdk\src\services\metrics_service.h` (`terminal_fast_outbox_frames_total`, trade-stage histograms with login-bearing labels, …). Those are **DEPRECATED** for this product (A03, A07).

### 6.2 Instrument types and units

Architecture lists bare tokens. This table is the OTel mapping. **Name column is binding.** Type/unit/description are how we implement them.

Convention:

- Counters that already end in `_total` stay that way (do not double-suffix).
- Histograms use unit `s` (seconds). Record `TimeSpan.TotalSeconds`. Do **not** rename to `*_seconds` — A26 health JSON and §58 use the architecture token.
- Gauges that are booleans use unit `1` and values `{0,1}`.
- Independent QUOTE / TRADE: either a dedicated instrument (`fix_quote_connected`) **or** a shared instrument with `fix_session`. §58 already split the connected gauges; follow that. For message counters, use **one** instrument + `fix_session` attribute so we do not explode the catalog.

### 6.3 Catalog — MT5 (§58)

| Instrument | OTel type | Unit | Attributes (low card) | Description |
|---|---|---|---|---|
| `mt5_connected` | ObservableGauge | `1` | `broker` | 1 if Manager/session for that broker is up |
| `mt5_reconnects` | Counter | `{reconnect}` | `broker` | successful or attempted reconnects; increment on each reconnect **start** |
| `mt5_events_total` | Counter | `{event}` | `broker`, `event_kind` | raw ingestion events |
| `mt5_deals_total` | Counter | `{deal}` | `broker` | persisted deals (after validate) |
| `mt5_duplicate_deals_total` | Counter | `{deal}` | `broker` | idempotent hits (same broker+ticket) |
| `mt5_backfill_lag` | ObservableGauge | `s` | `broker` | `now - last_persisted_deal_time` (0 if caught up) |
| `mt5_outbox_backlog` | ObservableGauge | `{event}` | `broker` (or `_all`) | unprocessed `outbox_events` rows |

`broker` attribute values: stable slugs `achiever` / `starwavefx` (or the `brokers.id` UUID — pick **one** per process and keep it; slugs are preferred for dashboards). Never the manager login.

`event_kind` allow-list: `deal` \| `order` \| `position` \| `account` \| `group` \| `other`.

### 6.4 Catalog — Reconstruction (§58)

| Instrument | OTel type | Unit | Attributes | Description |
|---|---|---|---|---|
| `reconstructed_trades_total` | Counter | `{trade}` | `broker`, `canonical_symbol` | completed logical trades |
| `reconstruction_failures_total` | Counter | `{failure}` | `broker`, `reason` | unrecoverable reconstruct errors |
| `trade_completion_latency` | Histogram | `s` | `broker`, `canonical_symbol` | `closed_at - opened_at` of the **source** trade (holding time), **or** reconstruct-processing time? |

**Clarification (binding):** `trade_completion_latency` is **processing latency** (last deal persist → reconstructed row commit), not holding time. Holding time is a **feature**, not an SLO. If both are needed later, add `source_holding_time` as a separate histogram — do not overload this name.

`canonical_symbol` is expected to be `XAUUSD` almost always (low card). Do not put source broker symbol strings (`XAUUSDm`, `GOLD`) on the attribute.

`reason` allow-list for failures: `missing_deals` \| `inconsistent_volume` \| `unknown_symbol` \| `clock` \| `other`.

### 6.5 Catalog — Scoring (§58)

| Instrument | OTel type | Unit | Attributes | Description |
|---|---|---|---|---|
| `score_requests_total` | Counter | `{request}` | `broker` | score jobs started |
| `score_failures_total` | Counter | `{failure}` | `broker`, `reason` | |
| `prediction_latency` | Histogram | `s` | `model_stage` | wall time of a score/predict call |
| `shadow_candidates` | ObservableGauge | `{trader}` | `broker` | current count in `SHADOW` |
| `live_candidates` | ObservableGauge | `{trader}` | `broker` | current count in `LIVE_CANDIDATE` |

`model_stage`: `baseline` \| `ml` \| `combined`. Finite.

Do **not** label with `TraderState` beyond those two gauges. Other states belong on the dashboard query, not as a high-card time series per state unless we add a single `traders_by_state` gauge with `state` from the Domain enum (9 values — acceptable). Out of scope for the §58 minimum; add only if the Overview page needs it.

### 6.6 Catalog — FIX (§58 + A25 ownership)

Per §27, QUOTE and TRADE are independent. Connected gauges are **separate instruments** as named in §58.

| Instrument | OTel type | Unit | Attributes | Description |
|---|---|---|---|---|
| `fix_quote_connected` | ObservableGauge | `1` | _(none)_ | 1 when QUOTE status ≥ LoggedOn |
| `fix_trade_connected` | ObservableGauge | `1` | _(none)_ | 1 when TRADE status ≥ LoggedOn |
| `fix_logon_failures` | Counter | `{failure}` | `fix_session`, `reason` | Logout-on-bad-Logon, timeout, TLS |
| `fix_reconnects` | Counter | `{reconnect}` | `fix_session` | reconnect starts |
| `fix_inbound_messages_total` | Counter | `{message}` | `fix_session`, `msg_type` | after parse |
| `fix_outbound_messages_total` | Counter | `{message}` | `fix_session`, `msg_type` | after socket write |
| `fix_rejects_total` | Counter | `{reject}` | `fix_session`, `session_reject_reason` | `35=3` |
| `fix_business_rejects_total` | Counter | `{reject}` | `fix_session`, `business_reject_reason` | `35=j` |
| `fix_execution_reports_total` | Counter | `{report}` | `exec_type`, `ord_status` | `35=8` only (TRADE) |
| `fix_unknown_execution_states` | ObservableGauge | `{order}` | _(none)_ | open `EXECUTION_STATE_UNKNOWN` rows |

A25 additive (required for lease safety; not in §58 but do not skip):

| Instrument | Type | Unit | Attributes |
|---|---|---|---|
| `fix_lease_held` | ObservableGauge | `1` | `fix_session=TRADE` |
| `fix_lease_lost` | Counter | `{event}` | `reason` |
| `fix_fenced_sends_total` | Counter | `{send}` | `result=allowed\|denied` |

`fix_session` values: `QUOTE` \| `TRADE` only.

`msg_type` values: FIX tag 35 as **short string** (`0`,`1`,`2`,`3`,`4`,`5`,`A`,`D`,`8`,`V`,`W`,`X`,`Y`,`j`,`…`). Finite catalog. If an unknown type arrives, use `other` — do not create unbounded tag values.

`exec_type` / `ord_status`: the single RoE characters (`0`,`1`,`2`,`4`,`8`,`C`,`F`,`I`, …).

`reason` for logon failures: `invalid_data` \| `timeout` \| `tls` \| `seq` \| `other`.

### 6.7 Catalog — Execution (§58)

| Instrument | OTel type | Unit | Attributes | Description |
|---|---|---|---|---|
| `copy_intents_total` | Counter | `{intent}` | `path`, `action` | created intents |
| `risk_rejections_total` | Counter | `{rejection}` | `primary_reason`, `action` | one increment per **blocking** decision (A23) |
| `execution_orders_total` | Counter | `{order}` | `action`, `result` | NewOrderSingle attempts that left the process |
| `execution_fills_total` | Counter | `{fill}` | `action` | ER `exec_type=F` applied |
| `execution_rejections_total` | Counter | `{rejection}` | `source` | venue or risk; `source=risk\|venue\|gate` |
| `source_to_fill_latency` | Histogram | `s` | `action` | `source_event_time → fill TransactTime` (§36 total) |
| `slippage` | Histogram | `{price}` | `action`, `side` | `fill_px - expected_destination_px` (absolute buckets; also record signed value as a separate optional histogram `slippage_signed` later) |

`path`: `shadow` \| `live`.

`action`: `open` \| `increase` \| `reduce` \| `close` (map from `CopyIntentAction`).

`primary_reason`: A23 §4.3 codes **exactly** (`QUOTE_STALE`, `STOP_NEW_EXECUTION`, …). Finite list. Unknown → `OTHER` (and log the raw code).

`result` for orders: `accepted` \| `rejected` \| `unknown`.

`side`: `buy` \| `sell`.

### 6.8 Additive histograms from §36 (not named in §58)

Implement as soon as the pipeline timestamps exist. Do not overload `source_to_fill_latency`.

| Instrument | Unit | Span |
|---|---|---|
| `mt5_to_collector_latency` | `s` | `source_event_time → collector_receive_time` |
| `collector_to_scoring_latency` | `s` | collector → score request start |
| `risk_latency` | `s` | risk engine wall time (A23 §4.4) |
| `fix_outbound_latency` | `s` | `decision_time → fix_send_time` |
| `cserver_ack_latency` | `s` | `fix_send_time → first ER` |
| `fill_latency` | `s` | `fix_send_time → fill ER` |

These names are **additive** and snake_case like §58. Do not introduce dotted names beside them.

### 6.9 Histogram buckets

Explicit boundaries so Prometheus histograms stay aligned across processes:

**Latency (`s`):** `0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 30`

**Slippage (`{price}`, XAUUSD):** `0.01, 0.05, 0.10, 0.20, 0.50, 1, 2, 5, 10`

Configure via `Meter` views at host startup (`AddView`). Do not rely on SDK defaults.

### 6.10 `MetricNames` constants

```csharp
public static class MetricNames
{
    // MT5
    public const string Mt5Connected = "mt5_connected";
    public const string Mt5Reconnects = "mt5_reconnects";
    public const string Mt5EventsTotal = "mt5_events_total";
    public const string Mt5DealsTotal = "mt5_deals_total";
    public const string Mt5DuplicateDealsTotal = "mt5_duplicate_deals_total";
    public const string Mt5BackfillLag = "mt5_backfill_lag";
    public const string Mt5OutboxBacklog = "mt5_outbox_backlog";

    // Reconstruction
    public const string ReconstructedTradesTotal = "reconstructed_trades_total";
    public const string ReconstructionFailuresTotal = "reconstruction_failures_total";
    public const string TradeCompletionLatency = "trade_completion_latency";

    // Scoring
    public const string ScoreRequestsTotal = "score_requests_total";
    public const string ScoreFailuresTotal = "score_failures_total";
    public const string PredictionLatency = "prediction_latency";
    public const string ShadowCandidates = "shadow_candidates";
    public const string LiveCandidates = "live_candidates";

    // FIX
    public const string FixQuoteConnected = "fix_quote_connected";
    public const string FixTradeConnected = "fix_trade_connected";
    public const string FixLogonFailures = "fix_logon_failures";
    public const string FixReconnects = "fix_reconnects";
    public const string FixInboundMessagesTotal = "fix_inbound_messages_total";
    public const string FixOutboundMessagesTotal = "fix_outbound_messages_total";
    public const string FixRejectsTotal = "fix_rejects_total";
    public const string FixBusinessRejectsTotal = "fix_business_rejects_total";
    public const string FixExecutionReportsTotal = "fix_execution_reports_total";
    public const string FixUnknownExecutionStates = "fix_unknown_execution_states";

    // Execution
    public const string CopyIntentsTotal = "copy_intents_total";
    public const string RiskRejectionsTotal = "risk_rejections_total";
    public const string ExecutionOrdersTotal = "execution_orders_total";
    public const string ExecutionFillsTotal = "execution_fills_total";
    public const string ExecutionRejectionsTotal = "execution_rejections_total";
    public const string SourceToFillLatency = "source_to_fill_latency";
    public const string Slippage = "slippage";
}
```

A compile-time test (source generator or reflection test) must assert this set **equals** the §58 list. Additive instruments live in `MetricNames.Extended`.

---

## 7. Metric attributes — cardinality firewall

### 7.1 Allowed

```text
broker            # achiever | starwavefx
fix_session       # QUOTE | TRADE
event_kind        # deal | order | position | account | group | other
canonical_symbol  # XAUUSD (and later a tiny allow-list)
msg_type          # FIX 35, mapped; unknown → other
exec_type         # ER tag 150
ord_status        # ER tag 39
action            # open | increase | reduce | close
path              # shadow | live
side              # buy | sell
primary_reason    # A23 closed set
reason            # closed set per instrument
result            # closed set
model_stage       # baseline | ml | combined
source            # risk | venue | gate
service           # resource attribute, not a point label if already on resource
```

### 7.2 Forbidden on any instrument

```text
correlation_id
trace_id
source_login
manager_login
cl_ord_id
cserver_order_id
destination_position_id
copy_intent_id
risk_decision_id
execution_intent_id
deal_ticket
order_ticket
position_ticket
password / any secret
sender_comp_id          # includes trader login (live.pepperstone.1369850)
account_id
host:port unique
fencing_token           # logs only
```

A unit test must construct `TradingMetrics`, attempt to pass a `login` tag, and fail the build if the helper API even accepts it (typed `MetricTags` record with only allow-listed fields).

---

## 8. Host wiring (composition only)

### 8.1 API (`apps/api`)

```text
UseSerilog
AddOpenTelemetry().WithMetrics(m => m
    .AddMeter("TraderIntelligence")
    .AddAspNetCoreInstrumentation()
    .AddRuntimeInstrumentation()
    .AddPrometheusExporter()   // or OTLP
)
Map /metrics  (authenticated; SuperAdmin / ops)
Map /health/live anonymous (A26) — no metric dump
Correlation middleware before endpoints
```

Do **not** expose `/metrics` anonymously. Do **not** put metric values that include credentials (they never should).

### 8.2 Workers

Same Serilog + OTel meter. Export OTLP to the collector, or a localhost Prometheus port bound to loopback.

Each worker sets `service.name` to `mt5-worker` / `fix-worker`.

FIX worker registers `RedactingQuickFixLogFactory` only.

### 8.3 Configuration sketch (placeholders; no secrets)

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "QuickFix": "Information"
      }
    }
  },
  "Logging": {
    "FixWire": false
  },
  "OpenTelemetry": {
    "OtlpEndpoint": "",
    "PrometheusEnabled": true
  }
}
```

No password keys in this block.

---

## 9. Traces (minimum, so they do not become a leak)

v1 may ship metrics without a full trace sampler. If/when `AddAspNetCoreInstrumentation` / custom activities are added:

- Activity names: `mt5.ingest`, `reconstruct.trade`, `score.trader`, `risk.decide`, `fix.send`, `fix.exec_report`.
- Baggage / tags: same allow-list as log properties (§4.1).
- **Never** set `fix.raw` or `message` to an unredacted FIX string.
- Span events for Logon record `password_present=true`, never 554.

---

## 10. Tests (acceptance)

Add to `tests/Unit` (A27 class names suggested). These are required before FIX Logon against a real host (A25 test 12).

| Test | Assert |
|---|---|
| `Observability.FixWireRedactorTests.OfficialRoELogon_Redacts554` | official A32 line; output contains `554=***`; does not contain `passw0rd!` |
| `Observability.FixWireRedactorTests.SohDelimited_Redacts554` | `\u0001554=secret\u0001` → `554=***` |
| `Observability.FixWireRedactorTests.UserRequest_Redacts925And96` | `925=` and `96=` gone |
| `Observability.FixWireRedactorTests.EncryptedPasswordTags_Redacted` | 1401/1403 |
| `Observability.FixWireRedactorTests.Unparseable_Dropped` | returns `UNPARSEABLE_REDACTED`; original absent |
| `Observability.SecretNameCatalogTests.OptionsDump_RedactsPassword` | destructure `CTraderFixOptions` with `Password=x` → `***` |
| `Observability.SecretNameCatalogTests.EnvKey_CTraderFixPassword` | property name `CTRADER_FIX_PASSWORD` |
| `Observability.ConnectionStringRedactorTests` | `Password=` stripped; `Host=` kept |
| `Observability.ExceptionRedactorTests.NpgsqlUri` | postgres URI password stripped |
| `Observability.SerilogPipelineTests.LogonMessage_NeverEmitsPassword` | full logger to `CollectingSink`; search all rendered properties |
| `Observability.SerilogPipelineTests.CorrelationId_IsUuid` | |
| `Observability.MetricNamesTests.MatchesArchitectureSection58` | reflection vs frozen list in this file |
| `Observability.MetricCardinalityTests.RejectsLoginAttribute` | compile or runtime guard |
| `Observability.QuickFixLogFactoryTests.FileLogPath_Throws` | |

Integration (later): spin API, `GET /metrics`, assert §58 names exist (values may be 0).

**Forbidden in fixtures:** live `CTRADER_FIX_PASSWORD`, live MT5 passwords. Use `passw0rd!` from the **public** RoE sample only, and only inside redactor tests.

---

## 11. Implementation sequence (when a coding wave is assigned)

Do this **before** the first real FIX Logon or the first `IOptions<CTraderFixOptions>` bind that could be logged.

1. `RedactionPolicy` + `FixWireRedactor` + unit tests (including official 554 sample).
2. Serilog host wiring on API + both workers; `SensitiveDataEnricher` last.
3. `TradingLogScope` + API correlation middleware.
4. `MetricNames` + `TradingMetrics` + empty instruments (zeros).
5. Prometheus / OTLP export.
6. `RedactingQuickFixLogFactory` **before** any `SocketInitiator`.
7. Increment real counters as MT5 / reconstruct / risk / FIX land.

Do not wait for Phase 8 to redact. Phase 4 QUOTE Logon already sends 554.

---

## 12. What this file does **not** do

- Does not add packages or edit `Program.cs`.
- Does not invent dotted OTel names that disagree with §58.
- Does not port C++ `terminal_*` or trade-stage login labels.
- Does not claim Serilog/OTel is implemented. It is **MISSING**.
- Does not authorize `FileLogPath` “just for a day”.
- Does not treat `CTraderFixOptions` XML-doc comments as a security control.

---

## 13. Honesty close

Measured state on 2026-08-18: architecture §§57–58 are **specified here and unimplemented** in product source. API has a dead `Serilog.AspNetCore` reference. Workers have no Serilog. No OpenTelemetry. No redactor. The first QuickFIX `FileLog` or `{@Options}` log will print tag **554**.

**Product source was not modified.**
