# A96 — Architecture §54 Reconciliation Dashboard DTO

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\A96_recon_page_dto.md` |
| Date | 2026-08-18 |
| Agent | Grok Build subagent A96 |
| Status | Binding **page DTO** spec. **No product source was modified.** |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§54** (widgets); supporting **§12, §42–44, §46, §52, §55, §57–59, §69, §70.14, §72.5** |
| Product source modified | **None** |

This file owns the **allow-list DTO** for the Reconciliation page (`/reconciliation`). It does **not** own the comparer, FIX `35=AF`/`35=AN` sequence, or repair policy (those stay in A47). It does **not** invent a second REST surface: paths and envelopes stay A26 / A63.

---

## 0. Honesty pin (measured 2026-08-18)

| Item | Measured state |
|---|---|
| Architecture §54 widgets | Specified; **not implemented** |
| `GET /api/v1/reconciliation` | **MISSING** (`apps/api/Program.cs` is still `GET /weatherforecast`) |
| `IDashboardQueries` recon methods | **MISSING** — interface in `src/Application/Dashboard/DashboardModels.cs` covers Overview / Brokers / Groups / Traders / FIX / Risk only |
| C# page records (`ReconciliationPageDto`, …) | **MISSING** |
| Domain `ExecutionReconciliationRun` / `ExecutionReconciliationIssue` | **MISSING** (A01 / A20 / A47) |
| Tables `execution_reconciliation_runs` / `_issues` | Catalogued in A20; **not** on `TraderDbContext` |
| Domain `ReconciliationIssueType` | **PARTIAL** — 7 values; **missing** `OrderMismatch` and `OrphanFill` that §54 / A26 require on the wire |
| `FixSessionState` columns `ready_for_execution` / `last_reconciliation_*` | **MISSING** (A47 §11.4) |
| React page `ReconciliationPage` | `App.tsx` imports it; `apps/web/src/pages/` has **0** files |
| Stub TS `ReconciliationStatus` | **WRONG** — one timestamp + three ints; cannot paint §54 |
| Stub hook | `GET /api/reconciliation/status` — **not** A26 |
| SignalR | stub hub `/hubs/dashboard`; A26 hub is `/hubs/ops` |
| First-useful MT5 recon visibility | **FAIL** (no snapshot DTO, no checkpoint projection) |
| cTrader recon | **NEVER** (Phase 7 not started). DTO must say so; must **not** paint “healthy” |

Do **not** claim the reconciliation dashboard works. U10 in A29 remains **MISSING**.

---

## 1. Binding law

Quoted architecture **§54**:

```text
Last successful MT5 reconciliation
Last successful cTrader reconciliation

Unknown external positions
Missing internal positions
Order mismatches
Quantity mismatches
Orphan fills
Unresolved execution states
```

Quoted: **“Nothing unresolved should be silently ignored.”**

Quoted architecture **§12** (source, not destination): historical backfill + live subscription + **periodic reconciliation** of broker history vs `mt5_deals` / `mt5_positions_current`. That clock is **Last successful MT5 reconciliation**.

Quoted architecture **§42–§43** (destination): TRADE mass-status + positions vs internal book. That clock is **Last successful cTrader reconciliation**. Issue families in §43 plus **unresolved execution states** from §54.

Quoted architecture **§52** TRADE card: `lastReconciliation: { at, status }` is a **pointer** into the same dest run, not a second truth.

Quoted architecture **§55 / §72.5**: never send MT5 / proxy / cTrader / FIX / database / Redis passwords (or FIX tag 96 / RawData) to React.

Quoted architecture **§70.14**: inconsistent book blocks new execution. The page DTO must surface the gate; ACK is not a bypass (A47).

### 1.1 Precedence (do not re-litigate)

| Rank | Source | Owns |
|---|---|---|
| 1 | Architecture §54 / §12 / §42–43 / §55 | Widgets, two clocks, no silent drop, no secrets |
| 2 | A26 §2, §6.13, §7, §10 | `/api/v1` paths, envelopes, RBAC, SignalR event name, JSON camelCase + issue-type tokens |
| 3 | A47 §5, §11–12 | Dest run/issue schema, gate enum, `ACCEPTED_EXTERNAL`, execution-impacting rules |
| 4 | A59 §7 | Source (MT5) reconcile is a **different book**; do not reuse dest issue table in Phase 1 |
| 5 | A63 §5.10 | First-useful honesty: cTrader may be `NEVER`; MT5 tile must still exist |
| 6 | This file (A96) | Exact C# records, TS types, count-object keys, empty-state flags, projection |

**Wrong / stale (do not implement):**

| Topic | Wrong | Binding |
|---|---|---|
| Hook path | stub `/api/reconciliation/status` | `GET /api/v1/reconciliation` |
| Stub type | `{ lastReconciliation, unknownPositions, mismatches, orphanFills }` | Full `ReconciliationPageDto` below |
| A06-only reads | only `/reconciliation/runs` + `/issues` | A26 snapshot **plus** runs + issues |
| A63 snapshot without `issues[]` | counts only | A26 includes a preview list; A96 keeps it + `issuesTruncated` |
| Domain enum numeric | `ReconciliationIssueType = 0` | Wire tokens in §3.2 |
| Nav label | layout stub `"Recon"` | Architecture §46 **`Reconciliation`** |
| Hub | `/hubs/dashboard` | `/hubs/ops` event `reconciliation.issue` |
| One clock | a single `lastReconciliation` | **Two** clocks: MT5 and cTrader |
| Empty list = healthy | omit zeros / hide tiles at 0 | All nine types **always** present; `NEVER` is not `HEALTHY` |
| Source issues in dest table | new `source_reconciliation_issues` | A59: Phase 1 projects `system_events`; A20 catalog stays 47 tables |

### 1.2 Two books, one page

| Clock / book | Authority | Persist | DTO tile |
|---|---|---|---|
| Last successful **MT5** reconciliation | Source Manager history vs `mt5_deals` / `mt5_positions_current` / census (A59, §12) | `sync_checkpoints` streams `deals_reconcile` / `positions_reconcile` / `account_snapshot`; mismatches in `system_events` (`event_type = source_reconcile_mismatch`) | `mt5` |
| Last successful **cTrader** reconciliation | Venue TRADE snapshot vs `fix_orders` / `destination_positions` / unknown intents (A47, §42–43) | `execution_reconciliation_runs` + `_issues` | `cTrader` |

Never mix `broker_id` into a dest match key. Never use dest `venue_id` as an MT5 broker id. Login is **never** globally unique (§10).

---

## 2. Scope

Specify:

1. Wire enums and JSON names.
2. C# allow-list records under `TraderIntelligence.Application.Dashboard`.
3. TypeScript types that replace the stub.
4. Page snapshot + list + mutation DTOs.
5. Projection rules (what to read; what to leave null).
6. Empty-state / honesty flags so “nothing unresolved is silently ignored.”
7. Secret denylist for this page.
8. Query-interface methods and TanStack keys.
9. Tests that must exist before the page is called done.

Out of scope: implementing API/React, comparer, FIX send, new tables, flattening unknown positions, inventing fills.

Suggested on-disk names **when a later coding wave lands** (do not create from this task):

```text
src/Application/Dashboard/ReconciliationDtos.cs
src/Infrastructure/Dashboard/EfDashboardQueries.cs     # add methods only
apps/web/src/types/reconciliation.ts
apps/web/src/pages/reconciliation/ReconciliationPage.tsx
tests/Unit/Dashboard/ReconciliationDtoSerializationTests.cs
tests/Integration/Api/NoSecretsInReconDtoTests.cs
```

---

## 3. Wire contracts

### 3.1 Shared conventions (inherit A26 §2)

| Item | Contract |
|---|---|
| Envelope | `{ "data": { … } }` for the snapshot; list envelope for runs/issues |
| Time | ISO-8601 UTC with `Z` |
| Field names | camelCase **except** `openIssueCounts` **keys**, which are the §3.2 tokens |
| Money | unused on this page |
| Quantity | JSON `number`; unit is explicit (`quantityUnit`) |
| Tickets / FIX ids / `clOrdId` / dest position id / dest order id | JSON **string** (do not use JS `number`) |
| MT5 login | JSON `number` (int64 fits; still pair with `brokerId`) |
| Pagination | `page` 1-based, `pageSize` default 50, max 200 |
| Correlation | echo `X-Correlation-Id` |
| Domain entities | **never** serialize EF / `FixSessionState` / options / connection strings |

`System.Text.Json` for this page:

- `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`
- Enums on recon DTOs use **string** wire tokens via `[JsonConverter(typeof(JsonStringEnumConverter))]` + `[EnumMember(Value = "…")]`, **or** `string` properties already holding the token.
- **Never** emit domain numeric (`0`, `1`, …) for `ReconciliationIssueType`.

### 3.2 `reconciliationIssueType` (nine tokens — complete, always)

Architecture §54 names six families; A26 / A47 / A63 add the rest of the same family. The page **always** shows all nine, including zeros.

```text
UNKNOWN_EXTERNAL_POSITION
MISSING_INTERNAL_POSITION
ORDER_MISMATCH
QUANTITY_MISMATCH
SIDE_MISMATCH
ORPHAN_FILL
ORPHAN_EXECUTION_REPORT
UNEXPECTED_FILL
UNRESOLVED_EXECUTION_STATE
```

| Token | §54 widget | Blocks dest READY? (A47) |
|---|---|---|
| `UNKNOWN_EXTERNAL_POSITION` | Unknown external positions | Yes (unless that `721` is `ACCEPTED_EXTERNAL`) |
| `MISSING_INTERNAL_POSITION` | Missing internal positions | Yes |
| `ORDER_MISMATCH` | Order mismatches | Yes |
| `QUANTITY_MISMATCH` | Quantity mismatches | Yes |
| `SIDE_MISMATCH` | (A26 extra; keep visible) | Yes |
| `ORPHAN_FILL` | Orphan fills | Yes |
| `ORPHAN_EXECUTION_REPORT` | Orphan fills (same widget + own count) | Yes |
| `UNEXPECTED_FILL` | (A26 extra; keep visible) | Yes |
| `UNRESOLVED_EXECUTION_STATE` | Unresolved execution states | Yes |

`WONT_FIX_AUDITED` stays visible and **still blocks**. Only `ACCEPTED_EXTERNAL` on a specific dest `721` is excluded from the dest compare (A47). ACK is **not** a safety bypass.

**Domain gap (do not hide in the DTO):** `src/Domain/Enums/ReconciliationIssueType.cs` today is:

```text
UnknownExternalPosition
MissingInternalPosition
QuantityMismatch
SideMismatch
OrphanExecutionReport
UnexpectedFill
UnresolvedExecutionState
```

Missing: `OrderMismatch`, `OrphanFill`. A later domain wave must add them. The **page DTO** still emits all nine tokens; a coding wave must not map “unknown enum” to a dropped tile.

### 3.3 Other wire enums

`issueStatus`:

```text
OPEN
ACKNOWLEDGED
RESOLVED
WONT_FIX_AUDITED
ACCEPTED_EXTERNAL
```

A26 listed the first four. A47 adds `ACCEPTED_EXTERNAL`. A96 includes it. First useful may never emit it (no dest book); the union is still closed.

`reconciliationVenue`:

```text
MT5
CTRADER
```

`reconciliationRunType` (dest, A47):

```text
STARTUP
PERIODIC
POST_DISCONNECT
LEADERSHIP
UNKNOWN_RECOVERY
MANUAL
```

`sourceReconLane` (MT5, A59 — display only; not dest `run_type`):

```text
DEALS_HOT
DEALS_WARM
DEALS_COLD
POSITIONS
CENSUS
DEEP_AUDIT
MANUAL
```

`reconciliationRunStatus`:

```text
IN_PROGRESS
SUCCESS
DEGRADED
FAILED
CANCELLED
```

`cTraderGate` (A47 `ReadyForExecutionState` + TRADE pointer in A26 §6.11 / A47 §12.4):

```text
NEVER
IN_PROGRESS
READY_FOR_EXECUTION
BLOCKED_PENDING_RECON
BLOCKED_INCONSISTENT
BLOCKED_STALE
BLOCKED_NO_SESSION
FAILED
DEGRADED
```

`healthStatus` (MT5 tile + A26 shared): `HEALTHY` | `DEGRADED` | `UNHEALTHY` | `STALE` | `UNKNOWN`

`quantityUnit`:

```text
VENUE_NATIVE     // cTrader compare units (A47: do not convert to lots in the comparer)
MT5_NATIVE       // source volume as stored on mt5_deals / positions
MT5_LOTS         // only if the projector already converted; say so — do not guess
```

`reconAlertCode` (page honesty strip):

```text
CTRADER_NEVER
CTRADER_MISSING_RUN_WHILE_SESSION_UP
CTRADER_IN_PROGRESS
CTRADER_BLOCKED
CTRADER_STALE
MT5_NEVER
MT5_STALE
OPEN_EXECUTION_IMPACTING_ISSUES
UNRESOLVED_EXECUTION_STATE
SOURCE_MISMATCH_OPEN
```

### 3.4 Error codes this page can receive

| HTTP | `error.code` | When |
|---|---|---|
| 400 | `VALIDATION_FAILED` | Bad `type` / `venue` / body |
| 401 | `UNAUTHENTICATED` | |
| 403 | `FORBIDDEN` | Analyst/ReadOnly posting ack/run; ReadOnly is fine on GET |
| 404 | `NOT_FOUND` | Unknown `issueId` / `runId` |
| 409 | `CONFLICT` | `POST .../run` while a run is `IN_PROGRESS` (A47) |
| 412 | `PRECONDITION_FAILED` | Live-entry mutations elsewhere while not READY (not a GET failure) |
| 422 | `SECRET_FIELD_REJECTED` | Denylisted key in body/query |
| 429 | `RATE_LIMITED` | Privileged-action throttle |
| 503 | `DEPENDENCY_UNAVAILABLE` | DB / workers down |

---

## 4. Endpoints this DTO serves

| Method | Path | Roles | DTO |
|---|---|---|---|
| `GET` | `/api/v1/reconciliation` | ReadOnly+ | `ReconciliationPageDto` |
| `GET` | `/api/v1/reconciliation/runs` | ReadOnly+ | paged `ReconciliationRunRowDto` |
| `GET` | `/api/v1/reconciliation/issues` | ReadOnly+ | paged `ReconciliationIssueDto` |
| `POST` | `/api/v1/reconciliation/run` | RiskManager+ | `ReconciliationRunRequest` → A26 mutation envelope |
| `POST` | `/api/v1/reconciliation/issues/{issueId}/ack` | RiskManager+ | `ReconciliationAckRequest` → envelope |
| `POST` | `/api/v1/reconciliation/issues/{issueId}/accept-external` | SuperAdmin | `AcceptExternalRequest` → envelope. Phase 7. First useful: **404** |

Query for runs: `venue=MT5|CTRADER`, `runType`, `status`, `from`, `to`, `page`, `pageSize`, `sort` (default `startedAt:desc`).

Query for issues: `venue`, `type` (repeatable), `status` (repeatable; default `OPEN` + `ACKNOWLEDGED` so resolved noise does not hide the page), `clOrdId`, `destinationPositionId`, `brokerId`, `login`, `page`, `pageSize`, `sort` (default `detectedAt:desc`).

**Never drop `OPEN` rows** that match the filter. If `totalItems` exceeds `pageSize`, page them — do not truncate the set.

`POST /run` body does **not** invent fills (A26 / A47 / A63). First useful default `venue=MT5`. `venue=CTRADER` before Phase 7 → `409` with `code=CONFLICT` / message `CTRADER_RECON_NOT_ENABLED` (honest), not a fake SUCCESS.

---

## 5. C# records (Application.Dashboard)

Add to a **new** file `ReconciliationDtos.cs` in a later wave. Extend `IDashboardQueries`; do not overload `OverviewDto` or `RiskDashboardDto`.

Json names that differ from camelCase are called out. Implement with `[JsonPropertyName]` on the count record.

```csharp
namespace TraderIntelligence.Application.Dashboard;

public sealed record ReconciliationPageDto(
    DateTimeOffset GeneratedAt,
    Mt5ReconTileDto Mt5,
    CTraderReconTileDto CTrader,
    ReconciliationBookDto Book,
    ReconciliationIssueCountsDto OpenIssueCounts,
    IReadOnlyList<ReconciliationIssueDto> Issues,
    int TotalOpenIssues,
    int TotalAcknowledgedIssues,
    bool IssuesTruncated,
    IReadOnlyList<ReconciliationAlertDto> Alerts,
    ReconciliationHonestyDto Honesty);

public sealed record Mt5ReconTileDto(
    DateTimeOffset? LastSuccessfulAt,
    string Status,                 // healthStatus
    Guid? LastCheckpointId,
    string? LastStream,            // deals_reconcile | positions_reconcile | account_snapshot
    int BrokerCount,
    int StaleBrokerCount,
    int MissingDeals,
    int ExtraDeals,
    int PositionMismatches,
    int IncompleteFetches,
    IReadOnlyList<Mt5BrokerReconRowDto> Brokers);

public sealed record Mt5BrokerReconRowDto(
    Guid BrokerId,
    string BrokerCode,
    string DisplayName,
    DateTimeOffset? LastSuccessfulAt,
    DateTimeOffset? LastEventAt,
    string Status,                 // healthStatus
    string? LastStream,
    int MissingDeals,
    int ExtraDeals,
    int PositionMismatches,
    int IncompleteFetches);

public sealed record CTraderReconTileDto(
    DateTimeOffset? LastSuccessfulAt,
    string Health,                 // healthStatus — NEVER maps to UNKNOWN, not HEALTHY
    string Gate,                   // cTraderGate
    bool ReadyForExecution,
    Guid? LastRunId,
    string? LastRunType,           // reconciliationRunType or null
    string? LastRunStatus,         // reconciliationRunStatus or null
    string? FailReason,
    DateTimeOffset? LastRunStartedAt,
    DateTimeOffset? LastRunCompletedAt,
    int? MassStatusExpected,
    int MassStatusReceived,
    int? PosExpected,
    int PosReceived,
    bool SnapshotComplete,
    string DestinationAccountMasked, // last two digits only, e.g. "**50"
    string? VenueCode);              // execution venue code, never "LP" (A87)

public sealed record ReconciliationBookDto(
    int InternalWorkingOrders,
    int InternalOpenPositions,
    int InternalUnknownIntents,
    int? VenueWorkingOrders,       // null if no dest snapshot
    int? VenueOpenPositions,
    bool DisagreesWithLastSnapshot);

public sealed record ReconciliationIssueCountsDto(
    [property: JsonPropertyName("UNKNOWN_EXTERNAL_POSITION")] int UnknownExternalPosition,
    [property: JsonPropertyName("MISSING_INTERNAL_POSITION")] int MissingInternalPosition,
    [property: JsonPropertyName("ORDER_MISMATCH")] int OrderMismatch,
    [property: JsonPropertyName("QUANTITY_MISMATCH")] int QuantityMismatch,
    [property: JsonPropertyName("SIDE_MISMATCH")] int SideMismatch,
    [property: JsonPropertyName("ORPHAN_FILL")] int OrphanFill,
    [property: JsonPropertyName("ORPHAN_EXECUTION_REPORT")] int OrphanExecutionReport,
    [property: JsonPropertyName("UNEXPECTED_FILL")] int UnexpectedFill,
    [property: JsonPropertyName("UNRESOLVED_EXECUTION_STATE")] int UnresolvedExecutionState);

public sealed record ReconciliationIssueDto(
    Guid IssueId,
    Guid? RunId,
    string Venue,                  // MT5 | CTRADER
    string Type,                   // §3.2 token
    string Status,                 // issueStatus
    DateTimeOffset DetectedAt,
    DateTimeOffset? AcknowledgedAt,
    string? AcknowledgedBy,
    DateTimeOffset? ResolvedAt,
    Guid? ResolvedByRunId,
    string? ClOrdId,
    string? DestOrderId,
    string? DestinationPositionId,
    string? InstrumentId,          // tag 55 as string
    string? CanonicalSymbol,
    Guid? BrokerId,
    string? BrokerCode,
    long? Login,
    string? PositionTicket,        // string
    string? DealTicket,            // string
    string? InternalSide,
    string? ExternalSide,
    decimal? InternalQuantity,
    decimal? ExternalQuantity,
    string? QuantityUnit,
    string? Note,
    string Fingerprint,
    bool AcceptedExternal,
    bool ExecutionImpacting);

public sealed record ReconciliationRunRowDto(
    Guid RunId,
    string Venue,
    string RunType,
    string? SourceLane,            // MT5 only
    bool IsDaily,
    string Status,
    bool BlockedNewExecution,
    bool ReadyForExecution,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? FailReason,
    int IssueCount,
    string? MassStatusReqId,
    string? PosReqId,
    int? MassStatusExpected,
    int MassStatusReceived,
    int? PosExpected,
    int PosReceived,
    Guid CorrelationId);

public sealed record ReconciliationAlertDto(
    string Code,                   // reconAlertCode
    string Severity,               // INFO | WARNING | ERROR
    string Message);

public sealed record ReconciliationHonestyDto(
    bool CTraderNeverSucceeded,
    bool CTraderSessionUpWithoutRun,
    bool EmptyIssuesMeanAllClear,
    bool ReadyForExecution);

public sealed record ReconciliationPointerDto(
    DateTimeOffset? At,
    string Status);                // cTraderGate — reused by FIX TRADE card (A26 §6.11)

public sealed record ReconciliationRunRequest(
    string Venue,                  // MT5 | CTRADER
    string Reason,
    bool IsDaily = false);

public sealed record ReconciliationAckRequest(
    string Reason);

public sealed record AcceptExternalRequest(
    string ConfirmPhrase,          // exact ACCEPT_EXTERNAL
    string Reason);
```

Extend the existing port:

```csharp
public interface IDashboardQueries
{
    // existing methods unchanged …
    Task<ReconciliationPageDto> GetReconciliationAsync(CancellationToken ct);
    Task<PagedResult<ReconciliationRunRowDto>> GetReconciliationRunsAsync(
        string? venue, string? runType, string? status,
        DateTimeOffset? from, DateTimeOffset? to,
        int page, int pageSize, CancellationToken ct);
    Task<PagedResult<ReconciliationIssueDto>> GetReconciliationIssuesAsync(
        ReconciliationIssueQuery query, CancellationToken ct);
}

public sealed record ReconciliationIssueQuery(
    string? Venue,
    IReadOnlyList<string> Types,
    IReadOnlyList<string> Statuses,
    string? ClOrdId,
    string? DestinationPositionId,
    Guid? BrokerId,
    long? Login,
    int Page,
    int PageSize);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Data,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
```

Mutations are **not** on `IDashboardQueries`. They belong on an application service (`IReconciliationCommands`) that writes `audit_logs` (A51). This file only pins their request DTOs.

---

## 6. JSON snapshot (binding examples)

### 6.1 First useful / Phase 7-not-started (expected now)

Empty issue list is legal **only** together with `cTrader.gate = NEVER` and the `CTRADER_NEVER` alert. `honesty.emptyIssuesMeanAllClear` is **false**.

```json
{
  "data": {
    "generatedAt": "2026-08-18T12:00:00.000Z",
    "mt5": {
      "lastSuccessfulAt": null,
      "status": "UNKNOWN",
      "lastCheckpointId": null,
      "lastStream": null,
      "brokerCount": 0,
      "staleBrokerCount": 0,
      "missingDeals": 0,
      "extraDeals": 0,
      "positionMismatches": 0,
      "incompleteFetches": 0,
      "brokers": []
    },
    "cTrader": {
      "lastSuccessfulAt": null,
      "health": "UNKNOWN",
      "gate": "NEVER",
      "readyForExecution": false,
      "lastRunId": null,
      "lastRunType": null,
      "lastRunStatus": null,
      "failReason": null,
      "lastRunStartedAt": null,
      "lastRunCompletedAt": null,
      "massStatusExpected": null,
      "massStatusReceived": 0,
      "posExpected": null,
      "posReceived": 0,
      "snapshotComplete": false,
      "destinationAccountMasked": null,
      "venueCode": null
    },
    "book": {
      "internalWorkingOrders": 0,
      "internalOpenPositions": 0,
      "internalUnknownIntents": 0,
      "venueWorkingOrders": null,
      "venueOpenPositions": null,
      "disagreesWithLastSnapshot": false
    },
    "openIssueCounts": {
      "UNKNOWN_EXTERNAL_POSITION": 0,
      "MISSING_INTERNAL_POSITION": 0,
      "ORDER_MISMATCH": 0,
      "QUANTITY_MISMATCH": 0,
      "SIDE_MISMATCH": 0,
      "ORPHAN_FILL": 0,
      "ORPHAN_EXECUTION_REPORT": 0,
      "UNEXPECTED_FILL": 0,
      "UNRESOLVED_EXECUTION_STATE": 0
    },
    "issues": [],
    "totalOpenIssues": 0,
    "totalAcknowledgedIssues": 0,
    "issuesTruncated": false,
    "alerts": [
      {
        "code": "CTRADER_NEVER",
        "severity": "WARNING",
        "message": "cTrader TRADE reconciliation has never succeeded. This is not healthy. NewOrderSingle stays blocked."
      },
      {
        "code": "MT5_NEVER",
        "severity": "WARNING",
        "message": "No successful MT5 source reconcile checkpoint yet."
      }
    ],
    "honesty": {
      "cTraderNeverSucceeded": true,
      "cTraderSessionUpWithoutRun": false,
      "emptyIssuesMeanAllClear": false,
      "readyForExecution": false
    }
  }
}
```

### 6.2 Degraded dest book (Phase 7+)

```json
{
  "data": {
    "generatedAt": "2026-08-18T12:04:00.123Z",
    "mt5": {
      "lastSuccessfulAt": "2026-08-18T11:55:00.000Z",
      "status": "HEALTHY",
      "lastCheckpointId": "11111111-0000-4000-8000-000000000001",
      "lastStream": "deals_reconcile",
      "brokerCount": 2,
      "staleBrokerCount": 0,
      "missingDeals": 0,
      "extraDeals": 0,
      "positionMismatches": 0,
      "incompleteFetches": 0,
      "brokers": [
        {
          "brokerId": "a1111111-0000-4000-8000-000000000001",
          "brokerCode": "ACHIEVER",
          "displayName": "Achiever",
          "lastSuccessfulAt": "2026-08-18T11:55:00.000Z",
          "lastEventAt": "2026-08-18T12:03:50.000Z",
          "status": "HEALTHY",
          "lastStream": "deals_reconcile",
          "missingDeals": 0,
          "extraDeals": 0,
          "positionMismatches": 0,
          "incompleteFetches": 0
        }
      ]
    },
    "cTrader": {
      "lastSuccessfulAt": "2026-08-18T11:50:00.000Z",
      "health": "DEGRADED",
      "gate": "BLOCKED_INCONSISTENT",
      "readyForExecution": false,
      "lastRunId": "22222222-0000-4000-8000-000000000002",
      "lastRunType": "PERIODIC",
      "lastRunStatus": "DEGRADED",
      "failReason": null,
      "lastRunStartedAt": "2026-08-18T11:48:00.000Z",
      "lastRunCompletedAt": "2026-08-18T11:48:02.000Z",
      "massStatusExpected": 1,
      "massStatusReceived": 1,
      "posExpected": 1,
      "posReceived": 1,
      "snapshotComplete": true,
      "destinationAccountMasked": "**50",
      "venueCode": "PEPPERSTONE_CTRADER"
    },
    "book": {
      "internalWorkingOrders": 0,
      "internalOpenPositions": 0,
      "internalUnknownIntents": 1,
      "venueWorkingOrders": 0,
      "venueOpenPositions": 1,
      "disagreesWithLastSnapshot": true
    },
    "openIssueCounts": {
      "UNKNOWN_EXTERNAL_POSITION": 1,
      "MISSING_INTERNAL_POSITION": 0,
      "ORDER_MISMATCH": 0,
      "QUANTITY_MISMATCH": 2,
      "SIDE_MISMATCH": 0,
      "ORPHAN_FILL": 0,
      "ORPHAN_EXECUTION_REPORT": 0,
      "UNEXPECTED_FILL": 0,
      "UNRESOLVED_EXECUTION_STATE": 1
    },
    "issues": [
      {
        "issueId": "cc888888-0000-4000-8000-000000000600",
        "runId": "22222222-0000-4000-8000-000000000002",
        "venue": "CTRADER",
        "type": "UNRESOLVED_EXECUTION_STATE",
        "status": "OPEN",
        "detectedAt": "2026-08-18T11:48:00.000Z",
        "acknowledgedAt": null,
        "acknowledgedBy": null,
        "resolvedAt": null,
        "resolvedByRunId": null,
        "clOrdId": "TI-20260818-000099",
        "destOrderId": null,
        "destinationPositionId": null,
        "instrumentId": "185",
        "canonicalSymbol": "XAUUSD",
        "brokerId": "a1111111-0000-4000-8000-000000000001",
        "brokerCode": "ACHIEVER",
        "login": 6100421,
        "positionTicket": null,
        "dealTicket": null,
        "internalSide": "BUY",
        "externalSide": null,
        "internalQuantity": 0.05,
        "externalQuantity": null,
        "quantityUnit": "VENUE_NATIVE",
        "note": "NOS sent; disconnect before ER",
        "fingerprint": "sha256:…",
        "acceptedExternal": false,
        "executionImpacting": true
      }
    ],
    "totalOpenIssues": 4,
    "totalAcknowledgedIssues": 0,
    "issuesTruncated": true,
    "alerts": [
      {
        "code": "CTRADER_BLOCKED",
        "severity": "ERROR",
        "message": "cTrader gate BLOCKED_INCONSISTENT. New live entries return 412 PRECONDITION_FAILED."
      },
      {
        "code": "OPEN_EXECUTION_IMPACTING_ISSUES",
        "severity": "ERROR",
        "message": "4 OPEN execution-impacting issues. ACK does not set READY_FOR_EXECUTION."
      },
      {
        "code": "UNRESOLVED_EXECUTION_STATE",
        "severity": "ERROR",
        "message": "1 unresolved execution state. Do not retry NewOrderSingle."
      }
    ],
    "honesty": {
      "cTraderNeverSucceeded": false,
      "cTraderSessionUpWithoutRun": false,
      "emptyIssuesMeanAllClear": false,
      "readyForExecution": false
    }
  }
}
```

A26’s shorter `mt5` / `cTrader` objects are a **subset**. Servers **may** add the A96 fields; they **must** keep `lastSuccessfulAt`, `status`/`gate`, `readyForExecution`, `openIssueCounts` (nine keys), and a non-dropping `issues` preview. Prefer shipping the full A96 snapshot so the page does not N+1.

Compatibility alias: if a client written only to A26 reads `cTrader.status`, the server **also** emits `"status"` as a synonym of `"health"` **or** of `"gate"`? **Do not dual-write `status`.** A26 used `cTrader.status` as a coarse health (`DEGRADED`). A96 splits `health` vs `gate`. New clients use A96. A26-only readers should be updated with this file. First useful React is written against A96.

### 6.3 Mutation envelope (A26)

```json
{
  "data": {
    "accepted": true,
    "actionId": "8f2c1a0e-3b77-4d91-9c1a-2e6f0b8d4a11",
    "auditId": "c91e0d44-0a1b-4c2d-9e3f-112233445566",
    "status": "APPLIED"
  }
}
```

`POST /run` may return `status: "QUEUED"` when the worker has not started. `409` when `IN_PROGRESS`. Body:

```json
{ "venue": "MT5", "reason": "manual dashboard run", "isDaily": false }
```

Ack body:

```json
{ "reason": "reviewed; waiting next periodic run" }
```

Accept-external body (SuperAdmin, Phase 7+):

```json
{ "confirmPhrase": "ACCEPT_EXTERNAL", "reason": "known manual hedge 721=101" }
```

Wrong phrase → `400 VALIDATION_FAILED`. Type not `UNKNOWN_EXTERNAL_POSITION` → `400`. ACK after accept is unnecessary; status becomes `ACCEPTED_EXTERNAL`.

---

## 7. TypeScript (replace the stub)

`apps/web/src/types/index.ts` today:

```ts
export interface ReconciliationStatus {
  lastReconciliation: string;
  unknownPositions: number;
  mismatches: number;
  orphanFills: number;
}
```

**Non-compliant.** A later wave deletes that interface and adds `apps/web/src/types/reconciliation.ts`:

```ts
export type ReconciliationIssueType =
  | 'UNKNOWN_EXTERNAL_POSITION'
  | 'MISSING_INTERNAL_POSITION'
  | 'ORDER_MISMATCH'
  | 'QUANTITY_MISMATCH'
  | 'SIDE_MISMATCH'
  | 'ORPHAN_FILL'
  | 'ORPHAN_EXECUTION_REPORT'
  | 'UNEXPECTED_FILL'
  | 'UNRESOLVED_EXECUTION_STATE';

export type IssueStatus =
  | 'OPEN'
  | 'ACKNOWLEDGED'
  | 'RESOLVED'
  | 'WONT_FIX_AUDITED'
  | 'ACCEPTED_EXTERNAL';

export type ReconciliationVenue = 'MT5' | 'CTRADER';

export type HealthStatus = 'HEALTHY' | 'DEGRADED' | 'UNHEALTHY' | 'STALE' | 'UNKNOWN';

export type CTraderGate =
  | 'NEVER'
  | 'IN_PROGRESS'
  | 'READY_FOR_EXECUTION'
  | 'BLOCKED_PENDING_RECON'
  | 'BLOCKED_INCONSISTENT'
  | 'BLOCKED_STALE'
  | 'BLOCKED_NO_SESSION'
  | 'FAILED'
  | 'DEGRADED';

export type ReconciliationRunStatus =
  | 'IN_PROGRESS'
  | 'SUCCESS'
  | 'DEGRADED'
  | 'FAILED'
  | 'CANCELLED';

export interface ReconciliationIssueCounts {
  UNKNOWN_EXTERNAL_POSITION: number;
  MISSING_INTERNAL_POSITION: number;
  ORDER_MISMATCH: number;
  QUANTITY_MISMATCH: number;
  SIDE_MISMATCH: number;
  ORPHAN_FILL: number;
  ORPHAN_EXECUTION_REPORT: number;
  UNEXPECTED_FILL: number;
  UNRESOLVED_EXECUTION_STATE: number;
}

export interface ReconciliationIssue {
  issueId: string;
  runId: string | null;
  venue: ReconciliationVenue;
  type: ReconciliationIssueType;
  status: IssueStatus;
  detectedAt: string;
  acknowledgedAt: string | null;
  acknowledgedBy: string | null;
  resolvedAt: string | null;
  resolvedByRunId: string | null;
  clOrdId: string | null;
  destOrderId: string | null;
  destinationPositionId: string | null;
  instrumentId: string | null;
  canonicalSymbol: string | null;
  brokerId: string | null;
  brokerCode: string | null;
  login: number | null;
  positionTicket: string | null;
  dealTicket: string | null;
  internalSide: string | null;
  externalSide: string | null;
  internalQuantity: number | null;
  externalQuantity: number | null;
  quantityUnit: 'VENUE_NATIVE' | 'MT5_NATIVE' | 'MT5_LOTS' | null;
  note: string | null;
  fingerprint: string;
  acceptedExternal: boolean;
  executionImpacting: boolean;
}

export interface ReconciliationPage {
  generatedAt: string;
  mt5: Mt5ReconTile;
  cTrader: CTraderReconTile;
  book: ReconciliationBook;
  openIssueCounts: ReconciliationIssueCounts;
  issues: ReconciliationIssue[];
  totalOpenIssues: number;
  totalAcknowledgedIssues: number;
  issuesTruncated: boolean;
  alerts: { code: string; severity: 'INFO' | 'WARNING' | 'ERROR'; message: string }[];
  honesty: {
    cTraderNeverSucceeded: boolean;
    cTraderSessionUpWithoutRun: boolean;
    emptyIssuesMeanAllClear: boolean;
    readyForExecution: boolean;
  };
}

export interface Mt5ReconTile {
  lastSuccessfulAt: string | null;
  status: HealthStatus;
  lastCheckpointId: string | null;
  lastStream: string | null;
  brokerCount: number;
  staleBrokerCount: number;
  missingDeals: number;
  extraDeals: number;
  positionMismatches: number;
  incompleteFetches: number;
  brokers: Mt5BrokerReconRow[];
}

export interface Mt5BrokerReconRow {
  brokerId: string;
  brokerCode: string;
  displayName: string;
  lastSuccessfulAt: string | null;
  lastEventAt: string | null;
  status: HealthStatus;
  lastStream: string | null;
  missingDeals: number;
  extraDeals: number;
  positionMismatches: number;
  incompleteFetches: number;
}

export interface CTraderReconTile {
  lastSuccessfulAt: string | null;
  health: HealthStatus;
  gate: CTraderGate;
  readyForExecution: boolean;
  lastRunId: string | null;
  lastRunType: string | null;
  lastRunStatus: ReconciliationRunStatus | null;
  failReason: string | null;
  lastRunStartedAt: string | null;
  lastRunCompletedAt: string | null;
  massStatusExpected: number | null;
  massStatusReceived: number;
  posExpected: number | null;
  posReceived: number;
  snapshotComplete: boolean;
  destinationAccountMasked: string | null;
  venueCode: string | null;
}

export interface ReconciliationBook {
  internalWorkingOrders: number;
  internalOpenPositions: number;
  internalUnknownIntents: number;
  venueWorkingOrders: number | null;
  venueOpenPositions: number | null;
  disagreesWithLastSnapshot: boolean;
}

export interface ReconciliationPointer {
  at: string | null;
  status: CTraderGate;
}

export interface ReconciliationRunRow {
  runId: string;
  venue: ReconciliationVenue;
  runType: string;
  sourceLane: string | null;
  isDaily: boolean;
  status: ReconciliationRunStatus;
  blockedNewExecution: boolean;
  readyForExecution: boolean;
  startedAt: string;
  completedAt: string | null;
  failReason: string | null;
  issueCount: number;
  massStatusReqId: string | null;
  posReqId: string | null;
  massStatusExpected: number | null;
  massStatusReceived: number;
  posExpected: number | null;
  posReceived: number;
  correlationId: string;
}
```

Do **not** import C# numeric enums. Tickets stay `string`. `login` stays `number` **and** is always shown with `brokerCode` / `brokerId`.

TanStack Query keys (A63 / A62):

```text
['reconciliation']
['reconciliation', 'runs', filters]
['reconciliation', 'issues', filters]
```

Invalidate all three on `reconciliation.issue` and after ack/run mutations.

Hook rewrite (later wave; stub is deprecated):

```text
GET  /api/v1/reconciliation
GET  /api/v1/reconciliation/runs
GET  /api/v1/reconciliation/issues
POST /api/v1/reconciliation/run
POST /api/v1/reconciliation/issues/{issueId}/ack
```

---

## 8. §54 widget → field map (page binding)

Nav label **Reconciliation** (not “Recon”). Route `/reconciliation` (A26 / A62).

| Architecture widget | DTO field | UI rule |
|---|---|---|
| Last successful MT5 reconciliation | `mt5.lastSuccessfulAt` + `mt5.status` | `null` → render **Never / N/A**, badge `UNKNOWN`/`STALE`, never a green “healthy” |
| Last successful cTrader reconciliation | `cTrader.lastSuccessfulAt` + `cTrader.gate` | `gate=NEVER` → **Never / N/A**. `readyForExecution` is a separate banner |
| Unknown external positions | `openIssueCounts.UNKNOWN_EXTERNAL_POSITION` | tile always visible, including `0` |
| Missing internal positions | `openIssueCounts.MISSING_INTERNAL_POSITION` | always visible |
| Order mismatches | `openIssueCounts.ORDER_MISMATCH` | always visible |
| Quantity mismatches | `openIssueCounts.QUANTITY_MISMATCH` | always visible |
| Orphan fills | `openIssueCounts.ORPHAN_FILL` **and** `ORPHAN_EXECUTION_REPORT` | two tiles (or one tile with two numbers). Do not fold ER orphans away |
| Unresolved execution states | `openIssueCounts.UNRESOLVED_EXECUTION_STATE` | always visible |
| (A26 extras) | `SIDE_MISMATCH`, `UNEXPECTED_FILL` | always visible |
| TRADE card last recon (§52) | `ReconciliationPointerDto` = `{ at: cTrader.lastSuccessfulAt, status: cTrader.gate }` | same object; do not compute a second clock |
| Gate / 412 warning | `cTrader.gate`, `honesty.readyForExecution`, `alerts[]` | while not READY, show banner: live entries return 412 |
| Open issue table | `issues` + `GET .../issues` | default filter OPEN+ACK. `issuesTruncated` → link “view all N” |
| Alerts | `alerts[]` | render every alert; do not toast-and-forget |

Count tiles use `MetricCard`-class UI. Zero is a real value (gray), not a hidden card. Non-zero OPEN execution-impacting counts use warning/error color. `gate=READY_FOR_EXECUTION` is the only dest green.

Formatters: `timeAgo(lastSuccessfulAt)` is fine for the subtitle; the primary text is the ISO timestamp or **Never**.

---

## 9. Honesty rules (DTO-layer implementation of §54)

Compute on the **server** so a thin client cannot paint “all clear” by accident.

| Condition | `alerts` | `honesty` |
|---|---|---|
| No dest SUCCESS run | `CTRADER_NEVER` WARNING | `cTraderNeverSucceeded=true`, `emptyIssuesMeanAllClear=false`, `readyForExecution=false` |
| TRADE session logged on / reconciling **and** no dest run row | `CTRADER_MISSING_RUN_WHILE_SESSION_UP` ERROR | `cTraderSessionUpWithoutRun=true` — this is a **bug**, not all-clear (A47 §12.1) |
| Dest run `IN_PROGRESS` | `CTRADER_IN_PROGRESS` INFO | READY false |
| Dest gate in `BLOCKED_*` / `FAILED` / `DEGRADED` | `CTRADER_BLOCKED` ERROR | READY false |
| `now - lastSuccessfulAt > CTRADER_RECON_STALE_AFTER_SEC` (A47 default 180s) | `CTRADER_STALE` ERROR | gate must already be `BLOCKED_STALE` |
| No MT5 checkpoint SUCCESS | `MT5_NEVER` WARNING | do not set MT5 `status=HEALTHY` |
| Any broker `stale` (A59 §7.5) | `MT5_STALE` WARNING | tile `DEGRADED`/`STALE` |
| `totalOpenIssues > 0` with `executionImpacting` | `OPEN_EXECUTION_IMPACTING_ISSUES` ERROR | `emptyIssuesMeanAllClear=false` |
| Any OPEN `UNRESOLVED_EXECUTION_STATE` | `UNRESOLVED_EXECUTION_STATE` ERROR | |
| Any OPEN source mismatch projected from `system_events` | `SOURCE_MISMATCH_OPEN` WARNING | MT5 tile not HEALTHY |

`emptyIssuesMeanAllClear === true` **only if**:

```text
cTrader.gate == READY_FOR_EXECUTION
AND cTrader.lastSuccessfulAt != null
AND totalOpenIssues == 0
AND honesty.cTraderSessionUpWithoutRun == false
AND mt5.lastSuccessfulAt != null
AND mt5.status in { HEALTHY, DEGRADED }   -- DEGRADED source does not claim dest READY
```

An empty `issues` array with `cTrader.gate == NEVER` is **not** all-clear.

Preview size: newest **20** OPEN+ACK issues by `detectedAt` desc. If `totalOpenIssues + totalAcknowledgedIssues > 20`, set `issuesTruncated=true`. The preview **must** prefer OPEN over ACK if mixed.

---

## 10. Projection (what to read — no new tables in this task)

### 10.1 MT5 tile

| Field | Source |
|---|---|
| `lastSuccessfulAt` | `max(sync_checkpoints.UpdatedAt)` where `Stream` ∈ `deals_reconcile`, `positions_reconcile` (and `account_snapshot` if present) **and** the checkpoint is a success. Today’s entity is `SyncCheckpoint` (`BrokerId`, `Login`, `Stream`, `LastTimestamp`, `LastTicket`, `UpdatedAt`) — **login-scoped**, not broker-scoped as A59’s target unique `(scope_type, scope_id, stream_name)`. Projector: per-broker max of login rows until A59 schema lands |
| `status` | `UNKNOWN` if no success; `STALE` if success older than source stale threshold (A59 `stale_source_sec`, suggest 120s once caught up); `DEGRADED` if `incompleteFetches>0` or any mismatch count > 0; else `HEALTHY` |
| mismatch counts | Phase 1: count `system_events` with `event_type = source_reconcile_mismatch` in the open window (A59 §7.3). Until `system_events` exists, emit **0** and `MT5_NEVER` / `UNKNOWN` — **do not invent** mismatches |
| `brokers[]` | one row per enabled `brokers` row; mask nothing secret; no manager password; no raw manager login |

Do **not** read dest `execution_reconciliation_*` for the MT5 tile.

### 10.2 cTrader tile

Until Phase 7 tables exist:

```text
lastSuccessfulAt = null
health           = UNKNOWN
gate             = NEVER
readyForExecution = false
snapshotComplete = false
venue working/open = null
```

After A47 tables exist:

```text
lastSuccessfulAt = latest execution_reconciliation_runs
                   where run_type in (STARTUP, PERIODIC, POST_DISCONNECT, LEADERSHIP, MANUAL)
                     and status = SUCCESS
                     and ready_for_execution = true
                   completed_at
gate             = derive from latest terminal run + stale watchdog + session (A47 §10, §12.4)
```

`destinationAccountMasked`: last two characters/digits of the dest account (A26 mask algorithm applied to the login string). Example `1369850` → `"*****50"` if more than two digits; keep last two. **Never** the account password. A87: `venueCode` is an execution venue, **not** `LP`.

### 10.3 Book counts

| Field | Source (Phase 7+) | First useful |
|---|---|---|
| `internalWorkingOrders` | `fix_orders` accepted/partial, `leaves_qty > 0` | `0` |
| `internalOpenPositions` | `destination_positions` `is_open` | `0` |
| `internalUnknownIntents` | `execution_intents` in `SentAcknowledgementUnknown` / `ExecutionStateUnknown` (entity exists today) | count those rows, or `0` if none |
| `venueWorkingOrders` / `venueOpenPositions` | last complete snapshot tables (A47 §11.3) | `null` |
| `disagreesWithLastSnapshot` | internal vs last SUCCESS snapshot | `false` when venue side is null |

### 10.4 Dest issues

From `execution_reconciliation_issues` (A47 §11.2). Fingerprint, sides, qtys, `721`, `37`, `11` as specified there.

### 10.5 Source issues (projected, Phase 1)

A59 forbids a new issue table in Phase 1. Projector maps `system_events` payload → `ReconciliationIssueDto` with:

| Source mismatch | `type` | `venue` |
|---|---|---|
| Local deal/position missing vs Manager | `MISSING_INTERNAL_POSITION` | `MT5` |
| Extra local current position vs Manager | `ORDER_MISMATCH` (no dest order id) | `MT5` |
| Qty differs | `QUANTITY_MISMATCH` | `MT5` |
| Side differs | `SIDE_MISMATCH` | `MT5` |
| Hash / ticket correction | `UNEXPECTED_FILL` if a deal revision; else `ORDER_MISMATCH` | `MT5` |

`runId` is null. `issueId` is the `system_events.id` (stable). `positionTicket` / `dealTicket` are **strings**. `executionImpacting` is **false** for source issues in Phase 1 (they do not drive `READY_FOR_EXECUTION`; dest gate does). They still **must appear** (§54 “nothing silently ignored”).

If `system_events` is absent, source issues list is empty **and** the MT5 tile is `UNKNOWN`/`MT5_NEVER`, not HEALTHY.

### 10.6 Unknown execution states

`execution_intents` with `Status` in `{ SentAcknowledgementUnknown, ExecutionStateUnknown }` **always** produce (or match) an `UNRESOLVED_EXECUTION_STATE` issue on the dest book once Phase 7 runs. Before Phase 7, still **project them onto the page** from the existing `ExecutionIntents` DbSet so a sent-then-disconnect row cannot hide. `venue=CTRADER`, `clOrdId` set, `executionImpacting=true`.

---

## 11. What this DTO must never contain

Hard denylist (A26 §3, A76, §55) — fail closed in the sanitizer before serialize:

```text
password, passwd, pwd, secret, rawdata, connectionstring, privatekey, proxyuser
MT5_PASSWORD, CTRADER_FIX_PASSWORD, ACHIEVER_PROXY_*, fixPassword, mt5Password
FIX tag 96 / RawData / Logon body
managerLogin (raw)
destination account password
Redis AUTH / database Password=
PEM / clientSecret
```

Allowed operational: masked dest account, venue code, host/port are **not** on this page (they live on Brokers / FIX). This page may show `destinationAccountMasked`, `venueCode`, `clOrdId`, dest order/position ids, instrument id, broker **code**, login.

`note` is free text from our comparer — **never** copy raw FIX Logon or env blobs into `note`.

Do not serialize:

- `CTraderFixOptions`
- `Mt5BrokerOptions`
- EF entities
- `fencingToken` (keep on the run row internally; omit from v1 page DTO — not a password, but unused by the page)
- SenderSubID if broker-issued (A63)

---

## 12. SignalR

Hub: `/hubs/ops` (A26). Event:

| Event | Payload | Client |
|---|---|---|
| `reconciliation.issue` | one `ReconciliationIssueDto` | invalidate `['reconciliation']` and issues list; do not merge a dropped OPEN |
| `reconciliation.run` | one `ReconciliationRunRowDto` (additive; recommended) | invalidate snapshot + runs |

Same sanitizer as REST. No polling of secrets. Polling `GET /api/v1/reconciliation` every 15s is enough if the hub is down (A06: hub is not a §69 gate).

---

## 13. RBAC (this page only)

| Verb | ReadOnly | Analyst | RiskManager | SuperAdmin |
|---|---|---|---|---|
| GET snapshot / runs / issues | Y | Y | Y | Y |
| POST run | N | N | Y | Y |
| POST ack | N | N | Y | Y |
| POST accept-external | N | N | N | Y (Phase 7; else 404) |

Audit actions (A51 / A63): `RECONCILIATION_RUN`, `RECONCILIATION_ISSUE_ACK`, `RECONCILIATION_ACCEPT_EXTERNAL`. ACK does **not** set READY. Accept-external does **not** hide the row.

UI: hide run/ack buttons for ReadOnly/Analyst. API still 403.

---

## 14. `IDashboardQueries` / FIX pointer reuse

`FixSessionDto` in `DashboardModels.cs` today has **no** `lastReconciliation`. When FIX page DTO is implemented (A26 §6.11), nest:

```csharp
ReconciliationPointerDto? LastReconciliation
```

built from the **same** projector as `CTraderReconTileDto.Gate` / `LastSuccessfulAt`. One function:

```text
ProjectCTraderGate(fix_sessions TRADE row, latest dest run, now) → (gate, lastSuccessfulAt, ready)
```

Do not let the FIX page invent a third clock.

---

## 15. Tests (DTO, not the comparer)

Must exist before the page is marked done. Comparer tests stay in A47 / A27.

| Test | Must prove |
|---|---|
| `ReconciliationDtoSerializationTests.OpenIssueCounts_always_emits_nine_keys` | zeros present; names are SCREAMING_SNAKE |
| `ReconciliationDtoSerializationTests.Tickets_are_strings` | `positionTicket` / `dealTicket` / `clOrdId` never JSON numbers |
| `ReconciliationDtoSerializationTests.IssueType_is_not_numeric` | no `type: 0` |
| `ReconciliationHonestyTests.Never_is_not_healthy` | `lastSuccessfulAt=null` → not HEALTHY, `emptyIssuesMeanAllClear=false` |
| `ReconciliationHonestyTests.Session_up_without_run_sets_missing_run_alert` | |
| `ReconciliationHonestyTests.Ack_does_not_flip_ready` | projector ignores ACK for READY |
| `NoSecretsInReconDtoTests` | password/RawData/connection-string fields absent; sanitizer fail-closed |
| `ReconciliationPageContractTests.Section54_widgets_have_fields` | snapshot JSON contains both clocks + all nine counts |
| `ReconciliationIssueQueryTests.Open_rows_are_not_dropped` | paging does not hide OPEN |
| `FirstUsefulCTraderNeverTests` | no dest tables → example 6.1 shape |

Do not use live account `1369850` as the first fixture (architecture §61). Use canned run/issue rows.

---

## 16. Implementation notes for a later coding wave (not this task)

1. Add domain enum values `OrderMismatch`, `OrphanFill` so the projector can bind without stringly mapping forever. Wire tokens stay §3.2.
2. Add `ReconciliationIssueStatus`, `ReconciliationRunType`, `ReconciliationRunStatus` in Domain (A47 §3.1) — page DTO still emits strings.
3. Add A20 tables + snapshot tables; do **not** create `source_reconciliation_issues`.
4. Implement `GetReconciliationAsync` as allow-list projection only.
5. Replace stub `ReconciliationStatus` and hook path.
6. Build `ReconciliationPage` from A62 §10.8 + this field map.
7. First useful: MT5 tile from checkpoints + honest `NEVER` cTrader tile + projected unknown intents.
8. Phase 7: fill dest tile from runs; enable CTRADER `POST /run`; enable accept-external.

Do **not** modify product source from this task.

---

## 17. Current-tree gaps this DTO depends on

| Dependency | Path / note | Blocks |
|---|---|---|
| API host | `D:\Prop\apps\api\Program.cs` weatherforecast | all GETs |
| Query port | `D:\Prop\src\Application\Dashboard\DashboardModels.cs` — no recon methods | projector |
| Dest entities | missing `ExecutionReconciliationRun` / `Issue` | dest tile |
| Dest tables | not in `TraderDbContext` | dest tile |
| Domain issue enum | `D:\Prop\src\Domain\Enums\ReconciliationIssueType.cs` incomplete | honest mapping |
| `system_events` | missing entity | source issue projection |
| `SyncCheckpoint` shape | login+stream unique, not A59 scope unique | MT5 aggregate is a **best-effort max**; document in `honesty` until A59 lands |
| React page | missing file imported by `App.tsx` | compile of web |
| Stub types / hooks | `D:\Prop\apps\web\src\types\index.ts`, `api/hooks.ts` | wrong contract |

---

## 18. Sources

- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§12, 42–44, 46, 52, 54, 55, 59, 69, 70.14
- `D:\Prop\reports\swarm\20260818\A20_table_catalog.md`
- `D:\Prop\reports\swarm\20260818\A26_dashboard_api_spec.md` §2, §6.11, §6.13, §7, §10
- `D:\Prop\reports\swarm\20260818\A47_reconciliation_design.md` §5, §11–12
- `D:\Prop\reports\swarm\20260818\A59_ingestion_checkpoints.md` §7
- `D:\Prop\reports\swarm\20260818\A62_react_scaffold.md` §10.8
- `D:\Prop\reports\swarm\20260818\A63_api_catalog.md` §5.10
- `D:\Prop\reports\swarm\20260818\A51_rbac_audit.md`
- `D:\Prop\reports\swarm\20260818\A87_not_an_lp.md`
- `D:\Prop\src\Application\Dashboard\DashboardModels.cs`
- `D:\Prop\src\Domain\Enums\ReconciliationIssueType.cs`
- `D:\Prop\apps\web\src\types\index.ts`

**Product source under `D:\Prop\src` and `D:\Prop\apps` was not modified.**
