# A73 — Copy latency: timestamps on every signal and metrics (Architecture §36)

| Field | Value |
|---|---|
| Agent | A73 (copy latency / signal timestamps) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A73_copy_latency.md` |
| Product source edited | **No** |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§36 Copy Timing Rules** |
| Supporting sections | §12–13, §27, §31–34, §37, §39, §44–45, §52, §57–58, §60, §62–64, §68, §70.11, §72.17 |
| Binding siblings | A20 table catalog, A23 risk, A24 shadow, A25 FIX, A26 dashboard, A27 tests, A41 outbox, A50 metrics/logging, A53 failure rules |
| Scope | XAUUSD copy path only: source MT5 event → collector → (optional score) → CopyIntent → risk → (shadow or live FIX) → fill / reject |
| Method | Read architecture §36 and adjacent law; inventory every Domain / Application / FIX / EF timestamp that exists on 2026-08-18; specify the missing contract. Nothing invented from memory. |

This file is the **canonical timestamp contract** for §36. A50 freezes **instrument names**. A23/A53 freeze **stale-entry reject policy**. A24 freezes **shadow analogs**. This file freezes **which clocks are stamped on which signal**, **when**, **where they persist**, and **how every latency metric is derived**. Do not invent a parallel vocabulary.

---

## 0. Verdict (honest, measured)

§36 is two sentences of product law:

1. Every source signal **must carry** five instants: `source_event_time`, `collector_receive_time`, `decision_time`, `fix_send_time`, `execution_time`.
2. The system **must measure** seven hops: MT5→collector, collector→scoring, risk, FIX outbound, cServer ack, fill, **total source-to-fill**. Reject entries that become too stale. Thresholds are **configurable and measured**, not hardcoded forever.

**Current tree does not implement §36.** Partial vocabulary exists. The pipeline is not timed. Stale-entry rejection is a unit-testable function plus one risk check; it is not a durable latency record.

| Capability | Classification | Evidence |
|---|---|---|
| Five §36 instants on `CopyIntent` | **MISSING** (1 of 5 present) | `D:\Prop\src\Domain\Entities\CopyIntent.cs`: `SourceEventTime`, `CreatedAt`, `ExpiresAt` only. No `CollectorReceiveTime`, `DecisionTime`, `FixSendTime`, `ExecutionTime`, `MaxSignalAge`, `SourceEventId`. |
| `CopyIntentExpiry` | **EXISTS_NEEDS_REFACTOR** | `D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs` — single predicate `now - sourceEventTime > maxSignalAge`. Does **not** evaluate `expires_at`. Does **not** distinguish OPEN vs CLOSE. |
| Risk `SIGNAL_STALE` | **EXISTS_NEEDS_REFACTOR** | `RiskEngine` line 113–115: `signal_age = DecisionTime - SourceEventTime`; rejects OPEN/INCREASE only. Request carries `DecisionTime` but that field is **not persisted** on the intent. Default `MaxSourceSignalAge = 15s` in `RiskLimits` is a **code default**, not a measured production policy (A23 §6: do not hardcode production numbers). |
| Quote timestamps | **EXISTS_NEEDS_REFACTOR** | `DestinationQuoteSnapshot.ReceivedAt` + `VenueTimestamp`; `RiskEngine.DestinationQuote` same. No durable `destination_quotes` mapping in the 5 EF configs that actually exist. |
| Execution / FIX send / fill times | **MISSING** | `ExecutionIntent` has only `CreatedAt`. No `fix_orders` / `fix_execution_reports` entities. Harness writes tag 60 as `yyyyMMdd-HH:mm:ss.fff` (`FixSimulationHarness.cs` line 195) but nothing persists it. |
| Shadow analogs | **PARTIAL** | `ShadowFill.FilledAt` + `QuoteAge`. No `shadow_sim_send_time`. `ShadowOrder.FilledAt` only. Modeled delay exists (`ShadowCopyEngine.SimulateEntry` `modeledDelay`, 250 ms adverse-slippage threshold). |
| §58 / §36 histograms | **MISSING** | No OpenTelemetry / `Meter` (A50). C++ `mt5-sdk/src/services/metrics_service.h` is **DEPRECATED** for this product (dealer / terminal / `propfirm_*` names). |
| `IUtcClock` | **MISSING** | `DateTimeOffset.UtcNow` is called at ingest (`DealIngestionService` lines 36, 100), FIX ownership, and the simulation harness. Tests cannot freeze the copy clock. |
| Persist-before-measure | **MISSING** | `TraderDbContext` references `CopyIntentsConfiguration` / `RiskDecisionsConfiguration` / `ExecutionIntentsConfiguration` / `DestinationQuotesConfiguration` / `ShadowFillsConfiguration` — **those files are not on disk**. The five configs that exist use shadow `DateTime` properties (`open_time` / `close_time`) that do **not** match Domain `Mt5Deal.DealTime` / `IngestedAt`. |
| Unit tests for expiry / hop formulas | **MISSING as source** | A01/A27 name `CopyIntentExpiryTests` / `StaleCopyIntentExpiryTests`. `D:\Prop\tests\Unit\` contains **no `.cs` test files** (csproj + bin/obj only). |
| Dashboard latency | **MISSING** | `FixSessionDto.QuoteAgeSeconds` exists as a DTO field. No `source_to_fill` / hop percentiles. Web `FixSession.quoteAge` is a type only. |

**Go-live gate §68 “stale signal rejection works” is unchecked.** It cannot be checked until the five instants exist, hop metrics exist, and the A53 20-intent / 3-minute FIX-gap test is green.

---

## 1. Architecture law (binding, not paraphrased weaker)

### 1.1 §36 Copy Timing Rules (verbatim contract)

For XAUUSD, stale trade copying can destroy expected edge.

Each source signal should carry:

```text
source_event_time
collector_receive_time
decision_time
fix_send_time
execution_time
```

Measure:

```text
MT5 → collector latency
collector → scoring latency
risk latency
FIX outbound latency
cServer acknowledgement latency
fill latency
total source-to-fill latency
```

Reject entries that become too stale according to configurable policy.

### 1.2 Adjacent hard law this spec does not reopen

| Source | Rule this spec obeys |
|---|---|
| §31 | Quote carries `quote_received_timestamp` and `venue_timestamp` if available. `quote_age` is a **separate** freshness check from signal age. |
| §32 | Never send FIX from an MT5 callback. Timestamps after `collector_receive_time` happen on workers, not in the pump. |
| §33 | Persist `created_at` + identity **before** send. `fix_send_time` is stamped only after that persist and after the socket write is attempted. |
| §34 | Disconnect after send → `EXECUTION_STATE_UNKNOWN`. Do not invent `execution_time`. Leave it null until an ExecutionReport supplies TransactTime or the order is reconciled as never-received. |
| §37 | Price-move / spread / quote-stale are **price** guards, not substitutes for §36 signal age. |
| §39 | Hard limits include `max quote age` **and** `max source-signal age`. Both are required. |
| §57 | Every latency log line carries the identifier list. Never log FIX 553/554. |
| §58 | Instrument **names** are frozen in A50. This file defines **formulas**. Do not rename. |
| §62 | Stale source → no new copied positions. TRADE down → do not queue an unlimited stale-entry backlog. |
| §63 | Every `CopyIntent` has `expires_at` and `max_signal_age`. Blind catch-up is forbidden (A53 §5). |
| §64 | OPEN/INCREASE vs REDUCE/CLOSE use **different** age caps. |
| §68 | “stale quote rejection works” and “stale signal rejection works” are go-live gates. |
| §72.17 | New entries must expire when stale. |
| A50 §6.8 | Additive hop histograms. Do **not** overload `source_to_fill_latency`. |
| A50 §7 | **No** `copy_intent_id` / login / `cl_ord_id` on metric labels. |

---

## 2. Clock law

### 2.1 One clock type

| Layer | Type | Rule |
|---|---|---|
| Domain / Application / workers | `DateTimeOffset` | Offset **must** be `+00:00`. Reject `DateTimeKind.Unspecified`. |
| PostgreSQL | `timestamptz` | Store UTC. Never `timestamp without time zone`. |
| FIX wire | UTC `yyyyMMdd-HH:mm:ss.fff` | Tag 52 `SendingTime` (A25). Tag 60 `TransactTime` on ER / NOS. |
| JSON / SignalR / dashboard | ISO-8601 with `Z` | e.g. `2026-08-18T09:12:00.123Z` |
| Metric histograms | seconds, `double` | A50 buckets. Non-negative. |

**Forbidden:** `DateTime.Now`, `DateTime.UtcNow` as a persisted authority, `DateTimeOffset.Now` (local offset), EF shadow `DateTime` columns for copy-path instants (the current `Mt5DealsConfiguration.open_time` / `close_time` pattern is a defect; do not copy it).

### 2.2 `IUtcClock` (required before any more `UtcNow` on this path)

```csharp
public interface IUtcClock
{
    DateTimeOffset UtcNow { get; }
}
```

Production: `SystemUtcClock`. Tests: `FrozenUtcClock`. Shadow replay: the replay clock, never wall clock.

All five §36 instants and both quote instants are assigned from `IUtcClock.UtcNow` **except**:

| Instant | Authority | Not our clock |
|---|---|---|
| `source_event_time` | MT5 deal / order time from the broker (see §3.1) | Yes — venue/source |
| `venue_timestamp` on a quote | FIX MD entry time / sending time if present | Yes — destination venue |
| `execution_time` (fill) | ExecutionReport tag 60 `TransactTime` | Yes — cServer |
| `fix_sending_time` (wire, informational) | outbound tag 52 | Written by us, but it is the FIX field, not the persist column |

Our clock still records `*_received_at` next to every venue time so skew is measurable.

### 2.3 Clock-skew policy (fail closed on OPEN)

Compute signed deltas. Persist them. Histograms record `max(0, delta)`.

```text
skew_source_vs_collector = collector_receive_time - source_event_time
if source_event_time > collector_receive_time + max_source_clock_skew:
    OPEN/INCREASE → REJECT  CLOCK_SKEW_SOURCE_AHEAD
    REDUCE/CLOSE  → proceed (risk-reducing); record skew; do not invent source time
```

`max_source_clock_skew` is configuration (suggested lab default `2s`). Do not silently clamp `source_event_time` forward — that would hide broker-clock failure and make a stale tape look fresh.

If `execution_time` (tag 60) is unparsable or missing: persist `execution_time` null, set `execution_time_source = MISSING`, use `er_received_at` **only** for `cserver_ack_latency` / `fill_latency` fallbacks, and increment `copy_timing_venue_time_missing_total`. Do not fabricate tag 60.

If `execution_time` is earlier than `fix_send_time` by more than `max_venue_clock_skew`: keep both raw values, set `execution_time_source = SKEWED`, use `er_received_at` for hop metrics, increment `copy_timing_venue_clock_skew_total`.

### 2.4 Monotonicity inside one intent

On a single `copy_intent_id` the expected order is:

```text
source_event_time
  ≤ collector_receive_time
  ≤ intent_created_at
  ≤ decision_time            # last evaluation
  ≤ fix_send_time            # live only; null on shadow / reject
  ≤ first_ack_time           # first 35=8
  ≤ execution_time           # fill TransactTime (or er_received_at fallback)
```

A later risk re-eval **overwrites** `decision_time` (and writes a new `risk_decisions` row). It must **not** overwrite earlier instants. `fix_send_time` is write-once.

---

## 3. What a “signal” is

§36 says “each source signal.” In this product a signal is **any durable event that can cause a copy decision**. Every one of the rows below **must** carry the timestamps in its table. Missing required timestamps = `INTENT_INCOMPLETE` / fail closed (A23), not “treat as now.”

### 3.1 Source deal (raw MT5) — creates the tape

| Field | Required | Clock | When stamped |
|---|---|---|---|
| `deal_time` | yes | **Source broker** | From Manager API / history. Domain today: `Mt5Deal.DealTime`. DTO: `Mt5DealDto.Time`. |
| `ingested_at` | yes | Collector `IUtcClock` | First successful persist of that `(broker_id, deal_ticket)`. Domain today: `Mt5Deal.IngestedAt`. |
| `callback_received_at` | yes on live pump | Collector | Instant the worker dequeued the pump event. History backfill may equal `ingested_at`. |

`source_event_time` for a copy intent **is** this deal’s `deal_time` (the triggering deal of the reconstructed event), not `ingested_at`. Using ingest time as the signal start would hide collector lag and defeat §36.

**Backfill law (A53):** history sync may persist deals and stamp `ingested_at`. It must **not** emit live `OPEN_EXPOSURE` copy intents. `mt5_to_collector_latency` on backfill will be large; that is `mt5_backfill_lag` (A50), not a license to copy.

**Current defect:** `Mt5DealsConfiguration` maps `open_time` / `close_time` as `DateTime` and does not map `DealTime` / `IngestedAt`. That mapping cannot satisfy this spec.

### 3.2 Reconstructed trade event — the only legal copy trigger (§32)

Reconstruction does not send FIX. It emits an outbox event (`trade-completed` / later a dedicated copy candidate). Timestamps:

| Field | Required | Clock | Meaning |
|---|---|---|---|
| `opened_at` / `closed_at` | yes | Derived from source deal times | Domain `ReconstructedTrade` already has these. **Holding time**, not copy latency. |
| `reconstructed_at` | yes | Collector | Commit time of the reconstructed row / outbox write. |
| `trigger_deal_time` | yes | Source | Deal time of the **event** that produced this action (open, scale-in, partial, close). This becomes `source_event_time`. |
| `trigger_deal_ingested_at` | yes | Collector | That deal’s `ingested_at`. |

`trade_completion_latency` (A50) remains **processing** latency (`last deal persist → reconstructed row commit`), not holding time.

### 3.3 Outbox event (A41)

| Field | Required | Meaning |
|---|---|---|
| `occurred_at` | yes | Domain `OutboxEvent.OccurredAt` — business time of the cause (usually `trigger_deal_time` or `reconstructed_at`). |
| `created_at` | yes | Row insert (`IUtcClock`). |
| `processed_at` | yes when done | Handler completion. Domain already has this. |
| Payload must include | yes | The five §36 fields **as known at write time** (later ones null). Handler must not restamp `source_event_time`. |

If a `risk-check-request` or `shadow-copy-intent` sits in the outbox until `now >= expires_at`, the handler marks the **intent** `INTENT_EXPIRED` / `SIGNAL_STALE` and the outbox `processed` (A41, A53). It does **not** retry until the age is “better.”

### 3.4 CopyIntent — the §36 signal (canonical)

Created and persisted **before** risk and **before** FIX/shadow fill (A23, A24).

| Column / property | Required at insert | Required later | Clock | Notes |
|---|---|---|---|---|
| `source_event_time` | **yes** | immutable | Source deal | §36 #1 |
| `collector_receive_time` | **yes** | immutable | Collector | §36 #2. First time **this event** was seen by our process (`trigger_deal_ingested_at` or pump receive). |
| `intent_created_at` | **yes** | immutable | Collector | Domain `CreatedAt`. Not a substitute for `collector_receive_time`. |
| `expires_at` | **yes** | immutable | Collector | §63. Set at creation. Extending because FIX was down **is** catch-up (A53). |
| `max_signal_age` | **yes** | immutable | duration | Interval. Family-specific (OPEN vs CLOSE). |
| `decision_time` | no | **yes** at every eval | Collector | §36 #3. Last risk/shadow policy evaluation. |
| `fix_send_time` | no | live send only | Collector | §36 #4. Write-once, after persist-before-send **and** after `Send` is invoked. Null on shadow and on reject. |
| `execution_time` | no | on fill / terminal venue time | Venue tag 60 | §36 #5. Null until a fill (or explicit venue reject TransactTime). |
| `first_ack_time` | no | on first 35=8 | Collector receive of ER | Needed for `cserver_ack_latency`. Not one of the five, but mandatory once a send occurs. |
| `correlation_id` | **yes** | immutable | — | §57 |
| `source_event_id` | **yes** | immutable | — | A20 unique key |

`CreatedAt` **is not** `collector_receive_time`. A deal can sit in the ledger for seconds (or a backfill hour) before reconstruction emits an intent. Both instants are required or `mt5_to_collector` and “intent factory delay” collapse into one lie.

**Current `CopyIntent` vs this table**

```15:17:D:\Prop\src\Domain\Entities\CopyIntent.cs
    public DateTimeOffset SourceEventTime { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
```

Present: `source_event_time`, `intent_created_at`, `expires_at`.  
Absent: `collector_receive_time`, `decision_time`, `fix_send_time`, `execution_time`, `first_ack_time`, `max_signal_age`, `source_event_id`, `correlation_id`.

### 3.5 Risk decision

| Field | Required | Clock |
|---|---|---|
| `decided_at` | yes | Same instant written to `copy_intents.decision_time` | Domain `RiskDecisionRecord.DecidedAt` |
| `eval_started_at` | yes | Collector, immediately before `RiskEngine.Evaluate` |
| `eval_finished_at` | yes | Collector, immediately after return |

`risk_latency = eval_finished_at - eval_started_at` (wall time of the **pure** evaluate call, A23 §4.4). Snapshot assembly (DB reads) is **not** folded into `risk_latency`. If we need that later, add `risk_snapshot_latency` — do not overload.

A23 §4.4 telemetry on **every** decision (approve and reject):

```text
risk_latency
quote_age
signal_age
price_deviation
spread
requested_quantity_in
approved_quantity_out
primary_reason
decision
```

Plus the §57 identifiers.

### 3.6 ExecutionIntent (live path only)

| Field | Required | Clock |
|---|---|---|
| `created_at` | yes, **before** send | Collector | Domain already has `CreatedAt` |
| `fix_send_time` | yes at send | Collector | Copied onto the parent intent |
| `first_ack_time` | on first ER | Collector |
| `execution_time` | on fill | Venue tag 60 |
| `unknown_since` | if §34 | Collector | When status became `EXECUTION_STATE_UNKNOWN` |

No send ⇒ `fix_send_time` stays null. A reject **before** send still has `decision_time` and a full latency row up to risk.

### 3.7 Destination quote (§31 companion — not a source signal, still timestamped)

| Field | Required | Clock |
|---|---|---|
| `received_at` | yes | Collector | Domain `DestinationQuoteSnapshot.ReceivedAt` / `RiskEngine.DestinationQuote.ReceivedAt` |
| `venue_timestamp` | if present | Venue | Nullable. Domain already has this. |

```text
quote_age        = decision_time - quote.received_at
venue_quote_age  = decision_time - quote.venue_timestamp   # if venue_timestamp != null
```

Reject OPEN/INCREASE if **either** exceeds `max_quote_age` (A23 §6.1). Quote session “connected” is not a substitute (A25 §7.2).

### 3.8 FIX messages

| Message | Timestamps to persist |
|---|---|
| Outbound `NewOrderSingle` (35=D) | `fix_send_time` (our clock, write-once), tag 52 `SendingTime`, persisted `cl_ord_id` |
| Inbound `ExecutionReport` (35=8) | `er_received_at` (our clock), tag 52 inbound `SendingTime`, tag 60 `TransactTime`, `exec_id`, `exec_type`, `ord_status` |
| Session health | `last_inbound_at`, `last_outbound_at` | Domain `FixSessionState` already has these. These are **session** clocks, not copy-signal clocks. Do not use heartbeat time as `execution_time`. |

### 3.9 Shadow path (A24 §6) — same five instants, different names for the last two

| §36 name | Shadow name | Clock |
|---|---|---|
| `source_event_time` | same | Source |
| `collector_receive_time` | same | Collector |
| `decision_time` | same | Collector |
| `fix_send_time` | `shadow_sim_send_time` | Collector, instant simulation starts |
| `execution_time` | `shadow_fill_time` | Collector, `ShadowFill.FilledAt` (destination quote is the price authority; there is no venue TransactTime) |

`ShadowCopyEngine.SimulateEntry(..., now, modeledDelay)` already takes `now` and a modeled delay for **price** adversity (`DefaultLatencySlippagePoints` after 250 ms). That delay is **not** a substitute for recording the five instants. It must not be used as fake `execution_time` on the live path.

### 3.10 Score / ML (off the copy hot path)

Trader scoring is **not** a copy signal. It still gets timestamps because §36 lists `collector → scoring latency`.

| Field | Required | Meaning |
|---|---|---|
| `score_request_at` | yes when a score job starts | Collector |
| `score_completed_at` | yes | Domain `TraderScore.LastScoredAt` / `TraderScoreHistory.RecordedAt` |

If this source event did **not** start a score job, `collector_to_scoring_latency` is **null**. Record `0` is forbidden (A24 §6). A last week’s `LastScoredAt` is not this hop.

### 3.11 Operational signals (not copy-edge, still timestamped)

| Signal | Instant |
|---|---|
| Kill switch change | `KillSwitch.UpdatedAt` (exists) + `audit_logs.at` |
| FIX logon / logout / reconnect | `fix_session_events.occurred_at` (A20) |
| Sync checkpoint | `SyncCheckpoint.LastTimestamp` (source stream watermark) + `UpdatedAt` |
| Audit | `AuditLog.At` |

These do not carry the five §36 fields. They must still be UTC `DateTimeOffset` / `timestamptz`.

---

## 4. Derived ages and hop formulas

All ages used for **policy** are computed at `decision_time` (or `now` if re-checking at send — A25 §6.3 / A53). All hops used for **metrics** use the instants in §3. Never use `DateTimeOffset.UtcNow` at scrape time to invent a completed hop.

### 4.1 Policy ages (risk / expiry)

```text
signal_age      = decision_time - source_event_time
intent_age      = now           - intent_created_at
quote_age       = decision_time - quote.received_at
venue_quote_age = decision_time - quote.venue_timestamp    # if present
```

OPEN/INCREASE (A23 §6.2, A53 §5.2) — **stricter bound wins**:

```text
if now >= expires_at:                        REJECT  INTENT_EXPIRED
if signal_age > intent.max_signal_age:       REJECT  SIGNAL_STALE
if signal_age > max_source_signal_age:       REJECT  SIGNAL_STALE   # global cap
if quote missing / session unhealthy:        REJECT  QUOTE_UNAVAILABLE
if quote_age > max_quote_age
   OR venue_quote_age > max_quote_age:       REJECT  QUOTE_STALE
```

REDUCE/CLOSE: use `max_signal_age_close` (much larger; may be hours) so a delayed close of an **already-mapped** destination/shadow position is not discarded (A24 §6.1, §64). `expires_at` still exists. OPEN-only guards (`SIGNAL_STALE` at the **open** threshold, `SPREAD_TOO_WIDE`, `PRICE_MOVED_TOO_FAR`, `STOP_NEW_EXECUTION`) do **not** block REDUCE/CLOSE (A53).

`CopyIntentExpiry` must become:

```text
IsExpired(sourceEventTime, expiresAt, now, maxSignalAge) =>
    now >= expiresAt || now - sourceEventTime > maxSignalAge
```

Today it only implements the second clause. That is a §63 hole: an operator could set a short `expires_at` and the helper would ignore it.

**Re-check at send instant.** An intent that was fresh at `decision_time` can expire on the FIX worker queue. The worker recomputes `signal_age` with `now = IUtcClock.UtcNow` **before** `NewOrderSingle`. If stale: persist reject, do **not** send, still emit hop metrics up to that point.

### 4.2 §36 hop metrics (binding formulas)

Names and units are A50. This table is the **definition of done** for “measure.”

| §36 phrase | Instrument (A50) | Formula | Null when | Attribute `path` |
|---|---|---|---|---|
| MT5 → collector | `mt5_to_collector_latency` | `collector_receive_time - source_event_time` | never, if both exist | `shadow` \| `live` |
| collector → scoring | `collector_to_scoring_latency` | `score_request_at - collector_receive_time` | no score job for this event | n/a (scoring instruments use `model_stage`) |
| risk latency | `risk_latency` | `eval_finished_at - eval_started_at` | evaluate not run | `action` |
| FIX outbound | `fix_outbound_latency` | `fix_send_time - decision_time` | no send (shadow / reject) | `live` only |
| cServer ack | `cserver_ack_latency` | `first_ack_time - fix_send_time` prefer `er_received_at`; also persist `TransactTime` delta | no ER yet | `live` |
| fill latency | `fill_latency` | `(execution_time ?? fill_er_received_at) - fix_send_time` | no fill | `live` |
| total source-to-fill | `source_to_fill_latency` | `(execution_time ?? fill_er_received_at) - source_event_time` | no fill | `action` |

Shadow analogs (do **not** write them into the live instruments):

| Shadow instrument (`MetricNames.Extended`) | Formula |
|---|---|
| `shadow_sim_send_latency` | `shadow_sim_send_time - decision_time` |
| `shadow_fill_latency` | `shadow_fill_time - shadow_sim_send_time` |
| `source_to_shadow_fill_latency` | `shadow_fill_time - source_event_time` |

A50 already reserved `source_to_fill_latency` for **live** `source_event_time → fill TransactTime`. Shadow must not increment that histogram.

### 4.3 Measure even on reject

A23 §6.2 / A53: the latency chain is stored **even when** the outcome is `SIGNAL_STALE`, `INTENT_EXPIRED`, `QUOTE_STALE`, `CATCH_UP_SUPPRESSED`, kill-switch, or risk reject.

- Persist `copy_timing` (see §5) with whatever suffix instants are null.
- Increment `risk_rejections_total{primary_reason,action}` (A50).
- Record completed hops only (e.g. a `SIGNAL_STALE` reject still records `mt5_to_collector_latency` and `risk_latency`; it does **not** record `source_to_fill_latency`).
- Do not emit a 0-second fill latency for a reject.

### 4.4 Histogram buckets

A50 §6.9, unchanged:

```text
Latency (s):     0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 30
Slippage (price): 0.01, 0.05, 0.10, 0.20, 0.50, 1, 2, 5, 10
```

XAUUSD source-to-fill will often land in the `0.25`–`5` s buckets when healthy and in `10`/`30`/`+Inf` when the collector or TRADE session is sick. That distribution is the operational signal. Do not widen buckets to hide `+Inf`.

### 4.5 Cardinality firewall (repeat of A50, so implementers do not “just add intent_id”)

Allowed labels on latency instruments: `action` (`open`\|`increase`\|`reduce`\|`close`), `path` (`shadow`\|`live`), `broker` (`achiever`\|`starwavefx`) on **collector** hops only, `canonical_symbol` (`XAUUSD`).

Forbidden: `copy_intent_id`, `correlation_id`, `source_login`, `cl_ord_id`, `deal_ticket`, any secret, `sender_comp_id`.

---

## 5. Persistence — `copy_timing` (1:1 with `copy_intents`)

A20 already requires `copy_intents` to **carry** `source_event_time`, `collector_receive_time`, `decision_time`, `expires_at`, `max_signal_age`. This spec **adds** a dedicated measurement row so hop metrics survive re-eval and can be rebuilt after a metrics-process crash. PostgreSQL remains authority (A03). Redis is not a latency store.

### 5.1 Table `copy_timing`

```text
copy_timing
  copy_intent_id              uuid PK FK → copy_intents
  correlation_id              uuid NOT NULL
  path                        text NOT NULL CHECK (path IN ('shadow','live'))
  action                      text NOT NULL   -- open|increase|reduce|close

  source_event_time           timestamptz NOT NULL
  collector_receive_time      timestamptz NOT NULL
  intent_created_at           timestamptz NOT NULL
  decision_time               timestamptz NULL
  eval_started_at             timestamptz NULL
  eval_finished_at            timestamptz NULL
  expires_at                  timestamptz NOT NULL
  max_signal_age              interval NOT NULL

  quote_received_at           timestamptz NULL
  quote_venue_timestamp       timestamptz NULL
  quote_age_ms                integer NULL     -- computed at last decision
  signal_age_ms               integer NULL

  score_request_at            timestamptz NULL
  score_completed_at          timestamptz NULL

  fix_send_time               timestamptz NULL
  first_ack_time              timestamptz NULL
  execution_time              timestamptz NULL
  execution_time_source       text NULL       -- TRANSACT_TIME | ER_RECEIVED | MISSING | SKEWED
  fill_er_received_at         timestamptz NULL

  shadow_sim_send_time        timestamptz NULL
  shadow_fill_time            timestamptz NULL

  terminal_reason             text NULL       -- A23 primary_reason or FILLED
  clock_skew_source_ms        integer NULL    -- collector - source, signed
  clock_skew_venue_ms         integer NULL    -- er_received - transact, signed

  updated_at                  timestamptz NOT NULL
```

Insert in the **same transaction** as `copy_intents`. Update in place as instants appear. Never delete.

Derived hop milliseconds may be generated columns or computed in the projector. Do not store only the milliseconds and drop the instants — instants are the source of truth.

### 5.2 Columns that stay on sibling tables (do not fork a second meaning)

| Table | Timestamp columns |
|---|---|
| `mt5_deals` | `deal_time`, `ingested_at` (replace the current `open_time`/`close_time` defect when the real mapping lands) |
| `reconstructed_trades` | `opened_at`, `closed_at`, `reconstructed_at` |
| `outbox_events` | `occurred_at`, `created_at`, `processed_at`, `expires_at` (A41) |
| `copy_intents` | the five §36 fields + `expires_at` + `max_signal_age` + `created_at` (A20 + this spec) |
| `risk_decisions` | `decided_at`, plus snapshot of `signal_age_ms` / `quote_age_ms` (A20 “carry quote age…”) |
| `execution_intents` | `created_at`, `fix_send_time`, `first_ack_time`, `execution_time`, `unknown_since` |
| `fix_execution_reports` | `transact_time`, `sending_time`, `received_at` (A20 already indexes `(cl_ord_id, transact_time)`) |
| `destination_quotes` | `quote_received_at`, `venue_timestamp` (A20) |
| `shadow_fills` | `filled_at`, `quote_age` (engine already returns these) |
| `fix_sessions` | `last_inbound_at`, `last_outbound_at` |

### 5.3 EF / Domain work (later implementation — **not done by this agent**)

When a later wave implements (not this file):

1. Add the missing properties to `CopyIntent`, `ExecutionIntent`, `RiskDecisionRecord`, `Mt5Deal` mapping, `ReconstructedTrade`.
2. Add `CopyTiming` entity + configuration. Map `DateTimeOffset` → `timestamptz`.
3. Fix `TraderDbContext`: it currently names `CopyIntents` / `Brokers` / `Mt5Deals` (plural) while Domain types are singular (`CopyIntent`, `Broker`, `Mt5Deal`). The referenced `*Configuration` types for copy/risk/execution/quotes/shadow **are not on disk**. That is a compile-break, not a latency feature.
4. Inject `IUtcClock`. Stop calling `DateTimeOffset.UtcNow` on this path.

---

## 6. Configuration (measured, not frozen as “the” production numbers)

A23: do not hardcode production numbers in the spec. Record **what the tree currently defaults to** so operators can see the starting point.

| Knob | Code / dashboard today | Role |
|---|---|---|
| `max_source_signal_age` | `RiskLimits.MaxSourceSignalAge = 15s` | Global OPEN/INCREASE cap |
| `max_signal_age` on intent | **not on entity** | Per-intent; may be tighter than global |
| `max_signal_age_open` | not present | Family default used at intent creation |
| `max_signal_age_close` | not present | Family default for REDUCE/CLOSE |
| `expires_at` offset | not present | A24: `OPEN_EXPIRES_AT_OFFSET_MS` / `CLOSE_EXPIRES_AT_OFFSET_MS` |
| `max_quote_age` | `RiskLimits.MaxQuoteAge = 3s`; A26 example `maxQuoteAgeMs: 1500` | Quote freshness |
| `max_source_clock_skew` | not present | Suggested lab start `2s` |
| `max_venue_clock_skew` | not present | Suggested lab start `2s` |
| Shadow `modeledDelay` | caller-supplied; 250 ms adverse price step | Price model only |

Dashboard A26 example `maxSourceSignalAgeMs: 3000` **disagrees** with `RiskLimits` 15 s. That is not a conflict to “pick a winner” in code today — it proves the numbers are not yet a single config surface. Implementation must bind **one** options object (e.g. `CopyTimingOptions`) read by risk, expiry, dashboard, and workers.

Env-style names (for the later binder; do not add a second flag vocabulary beside A49):

```env
COPY_MAX_SOURCE_SIGNAL_AGE=00:00:15
COPY_MAX_SIGNAL_AGE_OPEN=00:00:15
COPY_MAX_SIGNAL_AGE_CLOSE=06:00:00
COPY_OPEN_EXPIRES_OFFSET=00:00:15
COPY_CLOSE_EXPIRES_OFFSET=06:00:00
COPY_MAX_QUOTE_AGE=00:00:03
COPY_MAX_SOURCE_CLOCK_SKEW=00:00:02
COPY_MAX_VENUE_CLOCK_SKEW=00:00:02
```

---

## 7. Logging (§57) — every hop line

Every structured event that stamps or uses a §36 instant includes, when known:

```text
correlation_id
broker_id
source_login
source_trade_id
source_event_id
copy_intent_id
risk_decision_id
execution_intent_id
cl_ord_id
fix_session
path
action
source_event_time
collector_receive_time
decision_time
fix_send_time
execution_time
signal_age_ms
quote_age_ms
primary_reason
```

Never log tag 554 / passwords (A50). Never put those identifiers on metric **labels**.

---

## 8. Dashboard / API (additive to A26)

A26 does not yet specify copy-latency. Add **without** renaming A26 resources:

### 8.1 `GET /api/v1/copy/latency`

Window query (`from`, `to`, optional `path`, `action`, `broker`). Auth: ReadOnly+.

Returns percentiles computed from `copy_timing` (DB), not from in-process histograms (those die on restart).

```json
{
  "windowStart": "2026-08-18T08:00:00.000Z",
  "windowEnd": "2026-08-18T09:00:00.000Z",
  "hops": [
    { "name": "mt5_to_collector_latency", "unit": "s", "count": 0, "p50": null, "p95": null, "p99": null },
    { "name": "collector_to_scoring_latency", "unit": "s", "count": 0, "p50": null, "p95": null, "p99": null },
    { "name": "risk_latency", "unit": "s", "count": 0, "p50": null, "p95": null, "p99": null },
    { "name": "fix_outbound_latency", "unit": "s", "count": 0, "p50": null, "p95": null, "p99": null },
    { "name": "cserver_ack_latency", "unit": "s", "count": 0, "p50": null, "p95": null, "p99": null },
    { "name": "fill_latency", "unit": "s", "count": 0, "p50": null, "p95": null, "p99": null },
    { "name": "source_to_fill_latency", "unit": "s", "count": 0, "p50": null, "p95": null, "p99": null }
  ],
  "rejects": { "SIGNAL_STALE": 0, "INTENT_EXPIRED": 0, "QUOTE_STALE": 0, "CLOCK_SKEW_SOURCE_AHEAD": 0 }
}
```

Until data exists, return `count: 0` and null percentiles — do not fabricate.

### 8.2 Intent / risk detail

Any CopyIntent or risk-decision JSON includes the five §36 instants (nulls allowed) plus `signalAgeMs`, `quoteAgeMs`, `expiresAt`, `maxSignalAge`. A26 rejected-intent `at` is `decision_time`.

### 8.3 FIX page

Keep `quoteAge` on the QUOTE card (already in web `FixSession` / `FixSessionDto.QuoteAgeSeconds`). Add source-to-fill p95 on the Risk / Live Copy pages when `path=live` has samples. Do not show FIX passwords (§52).

---

## 9. Tests required (A27 names reused / extended)

No product tests are added by this agent. Implementation is not done until these are green.

| Test class | Must prove |
|---|---|
| `Risk.StaleCopyIntentExpiryTests` | `expires_at` **or** `max_signal_age` expires OPEN; 20 intents after a 3-minute TRADE gap → 0 `NewOrderSingle` (A53 worked example). |
| `Risk.CopyIntentExpiryBothClausesTests` | Helper rejects when only `expires_at` is hit and when only `max_signal_age` is hit. |
| `Risk.OpenVsCloseSignalAgeTests` | CLOSE uses `max_signal_age_close`; an hour-old close of a mapped position is not `SIGNAL_STALE` at the OPEN threshold. |
| `Risk.SignalAgeUsesSourceEventTimeTests` | `ingested_at` / `CreatedAt` are **not** the start of `signal_age`. |
| `Risk.ClockSkewSourceAheadTests` | Source time `> now + skew` → `CLOCK_SKEW_SOURCE_AHEAD` on OPEN; REDUCE proceeds. |
| `Risk.QuoteAgeIndependentOfSignalAgeTests` | Fresh signal + stale quote → `QUOTE_STALE`; stale signal + fresh quote → `SIGNAL_STALE`. |
| `Risk.ReCheckStaleAtSendInstantTests` | Fresh at decision, expired on FIX worker queue → no send. |
| `Execution.CopyTimingWriteOnceFixSendTests` | Second write to `fix_send_time` is rejected. |
| `Execution.MissingTransactTimeFallbackTests` | Null tag 60 → `execution_time_source=MISSING`, hops use `er_received_at`, counter increments. |
| `Execution.CopyTimingPersistedOnRejectTests` | `SIGNAL_STALE` still inserts `copy_timing` with `mt5_to_collector` + `risk_latency` populated and `fix_send_time` null. |
| `Shadow.ShadowTimingAnalogsTests` | Shadow writes `shadow_sim_send_time` / `shadow_fill_time`; does **not** increment `source_to_fill_latency`. |
| `Observability.MetricNamesExtendedEqualsA50Tests` | Hop instrument strings equal A50 §6.8. |
| `Observability.LatencyHistogramNoIdentityLabelsTests` | Recording helper refuses login / intent id labels. |
| `Observability.FrozenClockCopyPathTests` | `FrozenUtcClock` makes hop math deterministic. |

`DeterministicClock` is already named in A27. That is `IUtcClock`.

---

## 10. Implementation sequence (later waves — this agent stops here)

1. `IUtcClock` + Frozen clock in tests.
2. Domain fields on `CopyIntent` / `ExecutionIntent` / `CopyTiming` / deal mapping (`deal_time`, `ingested_at`).
3. `CopyIntentExpiry` both clauses + OPEN vs CLOSE family ages.
4. Risk engine: persist `decision_time`, `eval_started_at`/`eval_finished_at`, `signal_age`, `quote_age` on `risk_decisions` + `copy_timing`. Keep `SIGNAL_STALE` / `QUOTE_STALE` behavior; add `CLOCK_SKEW_SOURCE_AHEAD`.
5. Outbox payloads include the five fields (null suffix).
6. Shadow stamps analogs; live FIX stamps `fix_send_time` write-once and ER `TransactTime` / `received_at`.
7. A50 instruments + `MetricNames.Extended` hops.
8. `GET /api/v1/copy/latency`.
9. Tests in §9. Only then may §68 “stale signal rejection works” be considered.

Do **not** implement any of the above in this task. Do **not** reuse C++ `MetricsService` / `propfirm_*` / dealer `TradeLatencyStage` names.

---

## 11. Non-goals

| Forbidden | Why |
|---|---|
| Using `CreatedAt` as `source_event_time` | Hides broker-to-collector lag; destroys §36. |
| Using source MT5 last price time as destination quote time | Different venue (A24). |
| Emitting `source_to_fill_latency` on shadow or reject | Overloads the live SLO. |
| Extending `expires_at` after a FIX outage | Blind catch-up (A53). |
| Stamping `execution_time` with our clock on live fills when tag 60 exists | Venue time is the fill authority; keep `er_received_at` beside it. |
| Metric labels with identity | A50 §7. |
| Kafka / extra message bus for hops | §71. Outbox + PostgreSQL. |
| Treating C++ `metrics_service.h` as the copy-latency implementation | Wrong product, wrong names, dealer send path. |

---

## 12. Classification roll-up (this slice only)

| ID | Component | Class |
|---|---|---|
| T01 | Architecture §36 law | EXISTS_AND_GOOD (spec) |
| T02 | A50 hop instrument names | EXISTS_AND_GOOD (spec) |
| T03 | `CopyIntent.SourceEventTime` / `CreatedAt` / `ExpiresAt` | EXISTS_NEEDS_REFACTOR |
| T04 | `CopyIntent` collector / decision / send / fill instants | MISSING |
| T05 | `CopyIntentExpiry` | EXISTS_NEEDS_REFACTOR |
| T06 | `RiskEngine` SIGNAL_STALE + QUOTE_STALE | EXISTS_NEEDS_REFACTOR |
| T07 | `IUtcClock` | MISSING |
| T08 | `copy_timing` table / entity | MISSING |
| T09 | Deal `ingested_at` vs EF `open_time`/`close_time` | EXISTS_NEEDS_REFACTOR (unsafe to treat as §36) |
| T10 | Destination quote received + venue timestamps (Domain records) | EXISTS_NEEDS_REFACTOR (unwired) |
| T11 | ExecutionIntent / ER TransactTime persistence | MISSING |
| T12 | Shadow `FilledAt` / `QuoteAge` | EXISTS_NEEDS_REFACTOR |
| T13 | OTel hop histograms | MISSING |
| T14 | C++ `metrics_service.h` | DEPRECATED for copy latency |
| T15 | Dashboard copy latency API | MISSING |
| T16 | Unit tests for this contract | MISSING |

---

## 13. One-page cheat sheet (print this next to the risk engine)

```text
SOURCE SIGNAL CLOCKS (every CopyIntent)
  source_event_time        = triggering MT5 deal_time          (broker)
  collector_receive_time   = first ingest / pump receive       (us)
  decision_time            = last RiskEngine.Evaluate finish   (us)
  fix_send_time            = after persist + Send()            (us, live only)
  execution_time           = ER tag 60 TransactTime            (cServer)

POLICY
  signal_age = decision_time - source_event_time
  quote_age  = decision_time - quote.received_at
  OPEN/INCREASE: expire on expires_at OR max_signal_age OR global cap
  REDUCE/CLOSE:  long close cap; do not apply OPEN stale-entry rules
  Re-check age at send instant. Never catch up a 3-minute backlog.

METRICS (A50 names, seconds, even on reject for completed hops)
  mt5_to_collector_latency           collector_receive - source_event
  collector_to_scoring_latency       score_request - collector_receive   (null if no score)
  risk_latency                       eval_finished - eval_started
  fix_outbound_latency               fix_send - decision                 (live send)
  cserver_ack_latency                first_ack - fix_send
  fill_latency                       execution_or_er_recv - fix_send
  source_to_fill_latency             execution_or_er_recv - source_event (live fill only)

FAIL CLOSED if any required prefix instant is missing.
UTC DateTimeOffset / timestamptz only. IUtcClock. No identity on labels.
```

---

**End of A73.** Specification only. Product source was not modified.
