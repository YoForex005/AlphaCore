# A74 — Persist source reconstructed trade → destination orders / positions (§35)

| Field | Value |
|---|---|
| Agent | A74 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A74_source_dest_links.md` |
| Authority | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§35** |
| Supporting authority | §§10, 14–16, 32–34, 38, 40, 42–45, 51, 57–58, 60, 62–64, 68, 70.3–70.5, 72.18 |
| Sibling specs (do not contradict) | A20 table catalog, A21 reconstruction, A23 risk, A24 shadow analog, A25 FIX unknown-state, A26 dashboard map, A27 `DestinationPositionMappingTests`, A30 (links **not** in v1 migrations), A32/A36 cTrader `721`, A42 ClOrdID / ER bind, A43 REDUCE/CLOSE qty, A47 dest recon, A48 flatten-only-known |
| Product source modified | **No** |
| Classification | Live source↔destination mapping = **MISSING** (gap **E15** / table **D26**) |

This file is the binding implementation contract for Architecture §35. It does not implement types, migrations, or FIX send. It does not invent a second mapping table.

---

## 0. Verdict

Architecture §35 is three sentences and one hard prohibition:

```text
source reconstructed trade
        ↓
destination execution orders
        ↓
destination cTrader position ID(s)

Persist destination Position ID returned by FIX execution/position reports.

Support: initial entry, source scale-in, source partial close, full close, source reversal.

Do not assume one source event equals one destination order forever.
```

Measured tree (2026-08-18):

| Item | Path / evidence | Class |
|---|---|---|
| `SourceDestinationLink` entity | not present (`A01` listed `Domain\Copy\SourceDestinationLink.cs` as required; file absent) | **MISSING** |
| `DestinationPosition` entity | not present | **MISSING** |
| `source_destination_links` / `destination_positions` tables | no EF config, no migration (`A03`, `A20`, `A30` “not in v1”) | **MISSING** |
| `DestinationPositionMapper` | not present (`A27` future SUT) | **MISSING** |
| `CopyIntent` / `ExecutionIntent` carry `SourceTradeId` | `D:\Prop\src\Domain\Entities\CopyIntent.cs`, `ExecutionIntent.cs` | **EXISTS — correlation only; no dest 721 bind** |
| `CopyIntentAction` OPEN/INCREASE/REDUCE/CLOSE | `D:\Prop\src\Domain\Enums\CopyIntentAction.cs` | **EXISTS_AND_GOOD** (class names; not a map) |
| Reconstruction flags `WasScaledIn` / `WasPartialClose` + INOUT split | `TradeReconstructor` + `ReconstructedTrade` | **EXISTS — source book only** |
| Tag `721` persist / attach-on-NOS | `Fix.CTrader` has no TRADE send (`A05`, `A08`) | **MISSING** |
| Dashboard `GET .../source-destination-map` | specified in `A26` §6.5; no API | **MISSING** |

Honest state: **no live mapping exists**. Shadow’s `(source_broker_id, source_trade_id)` unique on `shadow_positions` (`A20`, `A24`) is the **analog**, not a substitute. Flatten (`A48`) and REDUCE/CLOSE sizing (`A43`) are unimplementable until this contract is persisted.

---

## 1. Binding quotes

### 1.1 §35 (this document’s law)

Quoted in §0. The map is **explicit** and **durable**. Memory, Redis, or “the last ClOrdID for this login” is not a map.

### 1.2 Adjacent law this design must honor

| Clause | Obligation for the mapper |
|---|---|
| §10 | Source identity is `(broker_id, …)`. Dest identity is `(venue_id, destination_account, destination_position_id)`. Never unique dest by MT5 ticket. |
| §14 / A21 | One reconstructed trade = one **position lifecycle**. Scale-in / partial close are **flags and events**, not extra `source_trade_id`s. INOUT reversal **completes** lifecycle *N* and **opens** lifecycle *N+1* (new `source_trade_id`). |
| §32 | Never send FIX from an MT5 callback. Chain is event → `CopyIntent` → risk → `ExecutionIntent` → FIX → **then** dest position / link update. |
| §33 / A42 | Persist `execution_intent` + `cl_ord_id` **before** `35=D`. One intent = one dest **order**. Replacement = **new** intent + new `cl_ord_id`. |
| §34 / A25 / A47 | Disconnect after send → `EXECUTION_STATE_UNKNOWN`. Recover with `35=H` / `35=AF` / `35=AN`. Bind `721` from ER / Position Report. No blind second `35=D`. |
| §38 / A43 | OPEN/INCREASE go through ounces → dest `OrderQty`. REDUCE/CLOSE qty comes from the **mapped dest position**, not a second source conversion. |
| §40 / A48 | `EMERGENCY_FLATTEN` closes **only** known `destination_position_id`s found via these links. Unknown venue 721s are recon issues, not flatten targets. |
| §42–43 / A47 | Dest book qty is the sum of applied Trade ERs (`150=F`). Position Report snapshot **compares**, it does not overwrite. |
| §57 | Logs carry `source_trade_id`, `execution_intent_id`, `cl_ord_id`, `cserver_order_id`, `destination_position_id`. |
| §64 / §72.18 | Reversal is **two** exposure classes: `CLOSE_EXPOSURE` then `OPEN_EXPOSURE`. Never one blended dest order. |

### 1.3 Official cTrader identity of “destination Position ID”

cTrader RoE remaps generic FIX `PosMaintRptID(721)` to **the cTrader position ID** (`A32`, `A36`):

| Message | `721` meaning |
|---|---|
| `NewOrderSingle` `35=D` | Optional. “A position ID where this order should be placed. If not set, a **new** position will be created and its ID will be returned in the Execution Report. **It can be specified only for hedged accounts.**” |
| `ExecutionReport` `35=8` | “A position ID.” Official New→Fill examples echo `721=101` on both the New ack and the Trade fill. |
| `RequestForPositions` `35=AN` | Optional filter. **Omit** to receive the full open book (`A25`, `A47`). |
| `PositionReport` `35=AP` | Identity of the reported position. Official short-position example: `721=101`, `704=0`, `705=30000`. |

Type is **String**, not integer. Persist as `text`. Official ER also carries `37` (cServer `OrderID`) — that is the **order**, not the position. Do not store `37` in `destination_position_id`.

v1 destination account (Pepperstone / cServer) is treated as **hedged**. Netting dest is **out of v1**. If a future venue row is `NETTING`, do not send `721` on NOS (RoE forbids attach except on hedge). That is a later spec, not a silent branch in this mapper.

---

## 2. Why “one source event ≠ one dest order forever”

These are all legal and **required**:

| Source fact | Dest fact |
|---|---|
| One reconstructed trade | Many dest orders (entry + *N* scale-ins + *M* partials + close) |
| One source deal / event | Usually one dest order **attempt**; after `not_on_venue`, a **replacement** intent (new `cl_ord_id`) |
| One `ENTRY_INOUT` deal | **Two** dest orders: close remaining mapped dest, then open opposite (new dest 721) |
| One dest order | Many ExecutionReports (New, partials, fill) — **one** link row |
| One dest position (`721`) | Many links (ENTRY + SCALE_IN + PARTIAL_CLOSE + CLOSE) |
| One source trade + two dest accounts | Two dest positions (one per `copy_allocations` account) |
| Three source hedge tickets | Three reconstructed trades → three dest 721s. **Not** one scale-in |

Illegal collapses:

| Collapse | Failure mode |
|---|---|
| Unique `(source_broker_id, source_trade_id)` on links | Second scale-in cannot persist (`A20` “do **not** unique this”) |
| Unique `(source_broker_id, position_id)` as the only reconstructed-trade key | INOUT reuse of MT5 `position_id` **overwrites** the closed trade that dest links still FK | See §4 |
| One dest NOS for INOUT | Leaves old dest side open **or** opens opposite without flattening — both are live-book disasters |
| “Current ClOrdID” as the dest position | IOC market orders leave the working book; `721` survives, `11` does not |
| Treat two hedge source `position_id`s as scale-in | Copies three tickets as one dest position (`A21` §8.1) |
| Scale-in NOS **without** `721` on a hedge dest | cServer **opens a second dest position**; mapper later sees two 721s → `BLOCKED_INCONSISTENT` |

---

## 3. Object graph (do not invent extra aggregates)

```text
reconstructed_trades.id                    = source_trade_id
        │
        │  1..*  (one per source event that we attempt to copy)
        ▼
copy_intents                               action = OPEN|INCREASE|REDUCE|CLOSE
        │
        │  1..* dest accounts
        ▼
copy_allocations
        │
        │  0..1 approved send
        ▼
execution_intents                          cl_ord_id UNIQUE; persist before 35=D
        │
        │  1:1
        ▼
fix_orders                                 dest_order_id = tag 37 when known
        │
        │  1..* reports
        ▼
fix_execution_reports                      721 on Trade/New when venue sends it
        │
        │  bind
        ▼
destination_positions                      UK (venue_id, destination_account, 721)
        ▲
        │  many links share one 721
source_destination_links                   UK (source_broker_id, source_trade_id,
                                               link_role, execution_intent_id)
```

Shadow (`shadow_orders` / `shadow_positions`) **must not** write these tables (`A24`, `A30`). Shadow keeps its own 1:1 `(source_broker_id, source_trade_id)` simulated position.

---

## 4. Source identity — `source_trade_id` must survive reversal

`A20` currently unique-constrains `reconstructed_trades` as `(broker_id, position_id)` and leaves ticket-reuse as an open question. **`A21` is binding for this mapper:**

```text
reconstructed trade key = (broker_id, login, position_id, lifecycle_seq)
source_trade_id         = reconstructed_trades.id   (stable UUID / deterministic id of that key)
```

`lifecycle_seq` increments when that MT5 `position_id` goes flat and later reopens, **including `ENTRY_INOUT`**. The INOUT deal ticket is listed on **both** trades (`A21` §7.6). Dest links of the **closed** lifecycle must keep pointing at the **old** `id`.

Therefore:

1. Implementing live links **requires** `reconstructed_trades` unique `(broker_id, login, position_id, lifecycle_seq)` (or an equivalent that does not reuse `id` across seq). Widening `A20`’s `(broker_id, position_id)` is a **versioned migration**, not a silent upsert that recycles the UUID.
2. `source_destination_links.source_trade_id` FKs that `id`. It does **not** FK `(broker_id, position_id)` alone.
3. Carry `source_position_id` + `source_lifecycle_seq` on the link as correlation (logs, dashboard). They are not the unique key.

Dirty reconstructed lifecycles (`A21` §4.4) are **not copy-eligible**. Mapper refuses `SOURCE_TRADE_DIRTY`.

---

## 5. Event → exposure class → `link_role` → FIX `721`

Reconstruction emits events (`A21` §4.3). Copy classifies **before** risk (`A23` §2, `A24` §4). The mapper assigns `link_role` at **execution-intent persist** (same transaction as `execution_intents` insert).

| Reconstruction event | Typical source entry | `CopyIntent.action` | `link_role` | NOS tag `721` | New dest 721? |
|---|---|---|---|---|---|
| `XAU_LIFECYCLE_OPENED` | first `ENTRY_IN` (or INOUT-open leftover) | `OPEN_EXPOSURE` | `ENTRY` | **omit** | Yes — adopt from ER/AP |
| `XAU_LIFECYCLE_INCREASED` | later `ENTRY_IN` same lifecycle | `INCREASE_EXPOSURE` | `SCALE_IN` | **required** = resolved dest 721 | No — same dest row |
| `XAU_LIFECYCLE_REDUCED` | `ENTRY_OUT` / `OUT_BY`, remaining > 0 | `REDUCE_EXPOSURE` | `PARTIAL_CLOSE` | **required** | No — qty down; stay open |
| `XAU_LIFECYCLE_COMPLETED` via OUT | last OUT, remaining = 0 | `CLOSE_EXPOSURE` | `CLOSE` | **required** | No — dest `is_open=false` |
| `XAU_LIFECYCLE_COMPLETED` via INOUT (old seq) | close half of INOUT | `CLOSE_EXPOSURE` | **`REVERSAL`** | **required** | Dest of **old** trade flattened |
| `XAU_LIFECYCLE_OPENED` via INOUT (new seq) | open half of same deal | `OPEN_EXPOSURE` | `ENTRY` (`origin=REVERSAL_OPEN`) | **omit** | **New** dest 721 |

### 5.1 Reversal is two rows, two intents, two `source_trade_id`s

Same MT5 deal ticket, **not** the same reconstructed trade:

```text
Deal INOUT  (ticket T, position_id P, volume = closed + new)

  copy_intents #1
    source_trade_id = trade(P, seq=N)          # just completed
    source_event_id = deal:T
    action          = CLOSE_EXPOSURE
    link_role       = REVERSAL
    reversal_leg    = CLOSE
    721             = dest of trade N

  copy_intents #2   (only if #1 dest close is filled, or is not required — see §8.5)
    source_trade_id = trade(P, seq=N+1)        # newly opened
    source_event_id = deal:T                   # same ticket, different trade + action
    action          = OPEN_EXPOSURE
    link_role       = ENTRY
    origin          = REVERSAL_OPEN
    reversal_of_source_trade_id = trade N
    721             = omit
```

Idempotency still holds (`A20` `copy_intents` unique `(source_broker_id, source_login, source_trade_id, source_event_id, action)`): the two rows differ on **both** `source_trade_id` and `action`.

`link_role=REVERSAL` is **only** the close-leg. The open-leg is a normal `ENTRY` on the new trade so “current dest 721 for this `source_trade_id`” stays a single-role query (see §7). Dashboard can still show a reversal pair via `reversal_of_source_trade_id`.

### 5.2 What the mapper will not do

- Promote `INCREASE` → `ENTRY` because dest mapping is missing (mid-book collector start). Reject `MAPPING_MISSING`. Do not invent a dest from source remaining.
- Promote `REDUCE` → `CLOSE` except the **dust** path already specified in `A43` §4.7 (`REDUCE_PROMOTED_TO_CLOSE_DUST`), which still uses the **same** dest 721.
- Emit a dest order for SL/TP modify (not a deal; not a reconstructed event).
- Copy non-XAUUSD or incomplete (`completed=false` is fine for open lifecycles; `dirty=true` is not).
- Attach `721` on `ENTRY`. That would require a dest position that does not exist yet, or would illegally join a foreign 721.

### 5.3 Hedge source vs scale-in (do not confuse)

| Source account mode | Additional BUY while long exists | Reconstruction | Dest map |
|---|---|---|---|
| **Netting** | same `position_id`, `ENTRY_IN` | `was_scaled_in` on **one** trade | `SCALE_IN` + attach `721` |
| **Hedging** | new `position_id` | **new** trade | new `ENTRY`, new dest 721 |

`TradeReconstructor` already groups by `position_id`. The mapper never merges two `source_trade_id`s.

---

## 6. Persistence contract

### 6.1 `destination_positions` (venue book)

Identity (`A20`):

```text
UNIQUE destination_positions_uk (venue_id, destination_account, destination_position_id)
```

`destination_position_id` = FIX `721` **text**. Do **not** unique quantity, avg price, or `is_open`.

| Column | Type | Notes |
|---|---|---|
| `id` | uuid PK | surrogate |
| `venue_id` | uuid NOT NULL | FK `execution_venues` |
| `destination_account` | text NOT NULL | cServer trader / account as used on the TRADE session |
| `destination_position_id` | text NOT NULL | tag `721` |
| `instrument_id` | bigint NOT NULL | tag `55` numeric; never `"XAUUSD"` |
| `canonical_symbol` | text NOT NULL | mapped; v1 `XAUUSD` |
| `side` | text NOT NULL | `BUY` / `SELL` (Long / Short) |
| `quantity` | numeric NOT NULL | dest OrderQty units (`A43`); ≥ 0 |
| `qty_oz` | numeric NOT NULL | canonical ounces (`A43`); recon unit-safe |
| `avg_price` | numeric NULL | VWAP of applied opening fills |
| `is_open` | bool NOT NULL | `quantity = 0` ⇒ `false` |
| `opened_at` | timestamptz NOT NULL | first applied Trade ER |
| `closed_at` | timestamptz NULL | when flattened |
| `close_reason` | text NULL | `COPIED_CLOSE` / `COPIED_REVERSAL` / `EMERGENCY_FLATTEN` / `VENUE_ABSENT_AFTER_FILLS` (`A47`) |
| `absolute_sl` / `absolute_tp` | numeric NULL | tags 1002 / 1000 informational only |
| `last_er_at` | timestamptz NULL | last applied Trade ER |
| `created_at` / `updated_at` | timestamptz | |

Indexes: `(venue_id, canonical_symbol, is_open)`, `(venue_id, side)`, `(venue_id, destination_position_id)`.

**Qty law (`A47`):** the only legal `quantity` mutation is applying signed `LastQty` (`32`) from `ExecType=Trade` (`150=F`) that we have accepted, or an audited close repair. A `35=AP` snapshot is **compare input**, never an upsert of `quantity`.

Side: dest Long increases on Buy fills without `721`-close semantics; dest Short on Sell. A close/reduce NOS is the **opposite** side of the open dest position **with** `721` set — those Trade fills **decrease** `quantity`. Implementation must classify the ER using the **link_role** (or the intent action), not by assuming Buy always increases.

### 6.2 `source_destination_links` (the map)

Identity (`A20`):

```text
UNIQUE source_destination_links_uk
  (source_broker_id, source_trade_id, link_role, execution_intent_id)
```

This is **per dest-order attempt**. It is idempotent with `execution_intents` (1:1). It is **not** “one row per source trade.”

| Column | Type | Notes |
|---|---|---|
| `id` | uuid PK | |
| `source_broker_id` | uuid NOT NULL | same as `brokers.id` / `reconstructed_trades.broker_id` |
| `source_login` | bigint NOT NULL | correlation (§10, §57) |
| `source_trade_id` | uuid NOT NULL | FK `reconstructed_trades.id` |
| `source_position_id` | bigint NOT NULL | MT5 ticket; correlation only |
| `source_lifecycle_seq` | int NOT NULL | `A21` seq |
| `source_event_id` | text NOT NULL | same string as `copy_intents` (e.g. `deal:{ticket}`) |
| `copy_intent_id` | uuid NOT NULL | FK `copy_intents` |
| `execution_intent_id` | uuid NOT NULL | FK `execution_intents` |
| `cl_ord_id` | text NOT NULL | denorm of intent; log join |
| `venue_id` | uuid NOT NULL | |
| `destination_account` | text NOT NULL | |
| `link_role` | text NOT NULL | `ENTRY` / `SCALE_IN` / `PARTIAL_CLOSE` / `CLOSE` / `REVERSAL` |
| `origin` | text NOT NULL | `NORMAL` / `REVERSAL_OPEN` / `FLATTEN` / `REPLACEMENT` |
| `reversal_of_source_trade_id` | uuid NULL | set on REVERSAL-close? no — set on the **new** `ENTRY` (`REVERSAL_OPEN`) |
| `reversal_leg` | text NULL | `CLOSE` only when `link_role=REVERSAL` |
| `destination_position_id` | text NULL | bound `721`; NULL until ER/AP/recovery |
| `dest_order_id` | text NULL | tag `37` once known |
| `source_event_volume_ticks` | bigint NULL | source deal ticks (1 lot = 10 000, `A38`/`A43`); audit |
| `dest_requested_qty` | numeric NOT NULL | intent `requested_quantity` |
| `dest_filled_qty` | numeric NOT NULL DEFAULT 0 | sum of applied Trade `LastQty` for this `cl_ord_id` |
| `status` | text NOT NULL | §6.3 |
| `correlation_id` | uuid NOT NULL | §57 |
| `created_at` | timestamptz NOT NULL | persist-before-send |
| `bound_at` | timestamptz NULL | first time `721` written |
| `closed_at` | timestamptz NULL | dest flatten observed for CLOSE/REVERSAL |

Indexes:

- `(venue_id, destination_position_id)`
- `(source_broker_id, source_login)`
- `(source_broker_id, source_trade_id, status)`
- `(execution_intent_id)` unique (1:1 with intent; stronger than the composite UK but documents the join)

FKs (`A20`):

```text
source_trade_id        → reconstructed_trades.id
execution_intent_id    → execution_intents.id
copy_intent_id         → copy_intents.id
(venue_id, destination_account, destination_position_id)
                       → destination_positions
                         (composite; only when destination_position_id IS NOT NULL)
```

Use a **partial** FK / application check for the dest triple: a PENDING link has no 721 yet.

### 6.3 Link `status`

Aligns with `ExecutionOrderStatus` (`A42`) but is about **the map**, not the wire:

| `status` | Meaning | `destination_position_id` |
|---|---|---|
| `PENDING` | Intent persisted; not yet a bound 721 | NULL (ENTRY) or **pre-filled** with resolved 721 (SCALE_IN / PARTIAL / CLOSE / REVERSAL) |
| `BOUND` | `721` known; dest row exists; order not terminal | NOT NULL |
| `PARTIAL` | Trade ER(s) applied; leaves > 0 | NOT NULL |
| `FILLED` | Intent filled; dest qty applied | NOT NULL |
| `REJECTED` | Venue / risk-after-send reject | NULL or unchanged |
| `CANCELLED` | Remainder dead (`39=4`) | keep last 721 if any fills happened |
| `NOT_ON_VENUE` | Recovered absent (`A42` §8.4) | NULL; **do not** invent a dest |
| `UNKNOWN` | `EXECUTION_STATE_UNKNOWN` | last known; do not send another order for this event |
| `SUPERSEDED` | Replacement intent exists (`origin=REPLACEMENT` on the new row) | forensic |

`PENDING` for SCALE_IN/PARTIAL/CLOSE/REVERSAL **already carries** the target `721` (copied from §7 resolve at persist time). That is what the FIX worker puts on tag `721`. ENTRY `PENDING` must have `destination_position_id` NULL — worker **omits** the tag.

### 6.4 Application invariants (not all expressible as UNIQUE)

1. **At most one non-failed `ENTRY`** per `(source_broker_id, source_trade_id, venue_id, destination_account)`. Suggested partial unique:

   ```text
   UNIQUE (source_broker_id, source_trade_id, venue_id, destination_account)
   WHERE link_role = 'ENTRY'
     AND status NOT IN ('REJECTED','CANCELLED','NOT_ON_VENUE','SUPERSEDED')
   ```

   `UNKNOWN` **holds** the slot (same reason `A42` holds `cl_ord_id`).

2. **SCALE_IN / PARTIAL_CLOSE / CLOSE / REVERSAL** rows that are `PENDING`+ must have `destination_position_id IS NOT NULL`.

3. All non-failed links for one `(source_trade_id, venue_id, destination_account)` share **the same** `destination_position_id`. A second 721 is `MAP_SPLIT` / `BLOCKED_INCONSISTENT`.

4. `dest_filled_qty` ≤ `dest_requested_qty` (IOC leftover is not a second order).

5. Do not unique `(source_broker_id, source_trade_id)` or `(source_trade_id, link_role)` — many SCALE_IN / PARTIAL_CLOSE.

### 6.5 When rows are written (same DB transaction as the cause)

| Cause | Writes |
|---|---|
| Risk approves / reduce-size → new `execution_intents` | INSERT link `PENDING` (ENTRY: 721 NULL; others: 721 = resolve()) |
| First inbound ER with `721` for that `cl_ord_id` | UPSERT `destination_positions`; SET link `destination_position_id`, `dest_order_id`, `bound_at`, `status=BOUND` |
| Trade ER `150=F` | apply signed qty to dest row; `dest_filled_qty += LastQty`; `PARTIAL` or `FILLED` |
| Terminal reject / cancel / not_on_venue | link status only; dest qty unchanged except filled remainder |
| CLOSE/REVERSAL fill that drives dest `quantity` to 0 | dest `is_open=false`, `closed_at`; link `FILLED` |
| `A42` recovery: `ORDER_NOT_FOUND` + new unmatched 721 matching side/qty | treat as fill; **create/bind** dest + link (`A42` line “filled + create link (§35)”) |
| `A47` repair: venue 721 we created (our Trade ERs) but dest row missing | INSERT dest from snapshot **identity** (721/side/55); qty still from fills, not snapshot |
| Flatten (`A48`) | new `CLOSE` link, `origin=FLATTEN`, 721 from existing open dest via links |

Never write a link inside the MT5 ingest callback. The copy/risk/execution worker that persists the intent writes the link.

---

## 7. Resolve “current dest position” for a source trade

Used by INCREASE / REDUCE / CLOSE / REVERSAL-close / flatten of **one** source trade.

```text
resolve_dest(source_broker_id, source_trade_id, venue_id, destination_account)
  -> DestRef | MAPPING_MISSING | MAP_SPLIT | MAP_UNKNOWN | MAP_FLAT

rows = source_destination_links
       where source_broker_id, source_trade_id, venue_id, destination_account
         and destination_position_id is not null
         and status in (PENDING, BOUND, PARTIAL, FILLED, UNKNOWN)
         and link_role in (ENTRY, SCALE_IN, PARTIAL_CLOSE, CLOSE, REVERSAL)

ids = distinct destination_position_id from rows

if ids is empty:
    return MAPPING_MISSING          # cannot INCREASE/REDUCE/CLOSE

if ids.count > 1:
    return MAP_SPLIT                # forgot 721 on a scale-in, or bad repair

id = ids.single
pos = destination_positions[venue, account, id]

if any row.status == UNKNOWN
   and execution_intents[that].status in (sent_ack_unknown, EXECUTION_STATE_UNKNOWN):
    return MAP_UNKNOWN              # A34 — do not send another order

if pos is null:
    return MAPPING_MISSING          # should not happen if FK held

if pos.is_open == false or pos.quantity == 0:
    return MAP_FLAT(id)             # CLOSE is idempotent no-send; INCREASE rejects

return DestRef(id, pos.side, pos.quantity, pos.qty_oz)
```

Risk (`A23` §3.1) requires `linked_destination_position_id` on REDUCE/CLOSE. The application **fills that field from `resolve_dest`** before the engine runs. Engine still fail-closes on `MAPPING_MISSING` (`A43` E43).

Flatten (`A48`): enumerate **open** dest 721s that appear on any non-failed link for the copy venue/account (optionally filtered by trader). Do **not** flatten `UNKNOWN_EXTERNAL_POSITION` 721s from recon.

---

## 8. Algorithms

### 8.1 ENTRY (initial dest position)

Preconditions: no successful ENTRY for this `(source_trade, venue, account)`; `resolve_dest` is `MAPPING_MISSING` (or only failed rows).

```text
persist execution_intent (cl_ord_id allocated, status=not_sent)
persist link:
  link_role=ENTRY, origin=NORMAL or REVERSAL_OPEN
  destination_position_id=NULL
  status=PENDING
send 35=D  WITHOUT tag 721     # only after persist commits
on 35=8:
  adopt 37
  if 721 present:
      upsert destination_positions (qty 0 until Trade)
      bind link
  on 150=F:
      apply +LastQty to dest (open side)
      dest.is_open=true
      link dest_filled_qty += LastQty
```

If the New ack has `721` and the Trade fill omits it, keep the ack’s 721. If **no** 721 appears on any ER, leave dest unbound and, after fill, recover via `35=AN` (`A47`) and bind the unmatched new 721 that matches side/qty (`A42` §8.4). If still unbound → `UNKNOWN` + recon issue, **not** a second `35=D`.

### 8.2 SCALE_IN (same dest 721)

```text
r = resolve_dest(...)
if r != DestRef: REJECT (MAPPING_MISSING | MAP_SPLIT | MAP_UNKNOWN | MAP_FLAT)
require dest.side == source lifecycle direction
qty = A43 OPEN/INCREASE converter (not dest.qty)
if qty < min: REJECT SIZE_BELOW_MIN     # do not send 0
persist intent + link:
  link_role=SCALE_IN
  destination_position_id=r.id          # worker MUST set 721
  status=PENDING
send 35=D  721=r.id  same side as dest
on Trade: dest.quantity += LastQty; update avg_price; link FILLED/PARTIAL
```

If the worker strips `721` (generic DD — `A36`), cServer opens a **second** position. Next `resolve_dest` returns `MAP_SPLIT`. That is why the cTrader dictionary is a **safety** property, not a style preference.

### 8.3 PARTIAL_CLOSE

```text
r = resolve_dest(...)
if r != DestRef: REJECT ...
qty = A43 §4.7 REDUCE (fraction = source_closed_ticks / source_position_ticks_before)
      # may promote to CLOSE (dust)
persist intent + link:
  link_role = PARTIAL_CLOSE or CLOSE if promoted
  destination_position_id = r.id
send 35=D  721=r.id  OPPOSITE side  OrderQty=qty
on Trade: dest.quantity -= LastQty
if dest.quantity == 0: dest.is_open=false  (should not happen unless promoted)
```

Do **not** re-run source closed lots through ounces (`A43` E38). Prior OPEN remainder_discarded is already gone.

### 8.4 CLOSE (full)

```text
r = resolve_dest(...)
if r == MAP_FLAT:  idempotent success; no new intent; no send
if r != DestRef:   REJECT
qty = dest.quantity            # A43 CLOSE — do not FloorToStep a live legal qty
persist link_role=CLOSE (or REVERSAL — §8.5)
send 35=D  721=r.id  opposite side  OrderQty=qty
on full fill: dest.quantity=0, is_open=false, close_reason=COPIED_CLOSE
```

IOC short fill: dest remains open with leftover; link `PARTIAL` / `CANCELLED` remainder; **do not** auto-resend (`A34`). A later CLOSE event or flatten may send a **new** intent for leftover dest qty.

### 8.5 REVERSAL (INOUT)

Order is **strict** (`A23` §2, `A24` §4.1, `A43` §4.7):

```text
1. CLOSE path (§8.4) against source_trade N
     link_role=REVERSAL, reversal_leg=CLOSE, close_reason=COPIED_REVERSAL
2. Only if step 1 status == FILLED (dest of N is flat):
     ENTRY path (§8.1) against source_trade N+1
     origin=REVERSAL_OPEN
     reversal_of_source_trade_id = N
     OPEN sizing from leftover source new_h only (A21 new_h = volume_h - closed_h)
3. If step 1 cannot be sent / not filled:
     do NOT open opposite. Remain whatever dest N still is.
4. If step 1 filled and step 2 rejected (stale, size, risk):
     dest is FLAT. That is required. Never leave dest N open.
```

Step 2 is a **new** `OPEN_EXPOSURE` and takes the **strict** OPEN policy (stale quote, price-move, `STOP_NEW_EXECUTION`). Step 1 takes CLOSE policy (lenient age; still needs TRADE + known 721).

Kill-switch `STOP_NEW_EXECUTION` **must not** block step 1. It **does** block step 2.

### 8.6 Replacement after `not_on_venue` (`A42` §9)

New `execution_intent` + new `cl_ord_id` + new link (`origin=REPLACEMENT`). Old link → `SUPERSEDED`. Same `link_role` and same pre-bound 721 (for non-ENTRY). ENTRY replacement still omits 721.

### 8.7 Apply a Trade ER to dest qty (single function)

```text
apply_trade_er(link, er):
  require er.exec_type == Trade
  q = er.last_qty                    # dest units
  if link_role in (ENTRY, SCALE_IN):
      dest.quantity += q             # same side as dest
      restripe avg_price
  else:                              # PARTIAL_CLOSE, CLOSE, REVERSAL
      dest.quantity -= q
      if dest.quantity < 0: FAIL DEST_QTY_NEGATIVE; block READY
  dest.qty_oz = dest.quantity × dest_oz_per_unit
  if dest.quantity == 0: dest.is_open = false
  link.dest_filled_qty += q
  idempotent on (venue_id, er fingerprint)   # A42 §8.3 / A47
```

Mass-status `150=I` does **not** apply qty (`A47`).

---

## 9. Interaction with other subsystems

| Subsystem | Uses this map how |
|---|---|
| Risk (`A23`) | `linked_destination_position_id` required for REDUCE/CLOSE; INCREASE also requires resolve() success |
| Sizing (`A43`) | REDUCE/CLOSE read `dest.quantity`; OPEN/INCREASE ignore dest qty except caps |
| Unknown-state (`A42`) | recovery may **bind** 721 / create dest + link; never second `35=D` while `UNKNOWN` |
| Recon (`A47`) | compares venue `721` set to `destination_positions`; unknown venue 721 ≠ auto-link |
| Flatten (`A48`) | targets only 721s referenced by open links; `origin=FLATTEN`, `link_role=CLOSE` |
| Dashboard (`A26`) | `GET .../source-destination-map` and live portfolio join these tables |
| Shadow (`A24`) | analog only; **no** writes here |
| Logging (`A57`) | every bind/apply/split includes `destination_position_id` |
| First-3 / scoring | **does not** read dest links. Partial dest close is not a source trade. |

---

## 10. Failure / reason codes (mapper + risk)

| Code | When | Send `35=D`? |
|---|---|---|
| `MAPPING_MISSING` | INCREASE/REDUCE/CLOSE and resolve empty | No |
| `MAP_SPLIT` | two dest 721s for one source trade/account | No — `BLOCKED_INCONSISTENT` |
| `MAP_UNKNOWN` | in-flight / unknown dest order on this trade | No |
| `MAP_FLAT` | INCREASE against already-flat dest | No |
| `MAP_ENTRY_EXISTS` | second non-failed ENTRY | No |
| `MAP_721_REQUIRED` | worker about to send SCALE_IN/CLOSE without 721 | No (guard) |
| `MAP_721_FORBIDDEN` | worker about to send ENTRY **with** 721 | No |
| `SOURCE_TRADE_DIRTY` | reconstructed lifecycle dirty | No |
| `REVERSAL_OPEN_BEFORE_FLAT` | attempted step 2 while dest N still open | No |
| `DEST_QTY_NEGATIVE` | apply over-closed dest | No further; recon issue |
| `SIZE_BELOW_MIN` | `A43` | No |
| `REDUCE_PROMOTED_TO_CLOSE_DUST` | `A43` — still send, but `link_role=CLOSE` | Yes (close) |

Fail **closed**. Missing map is not “copy 1:1 lots onto a new dest position.”

---

## 11. Worked fixtures (normative)

Volumes below: source ticks = MT5 `Volume()` (1.00 lot = 10 000). Dest qty is **illustrative** after a 1:1 ounce path that already quantized (`A43`). Tests assert **roles, 721 attach, row counts, and dest qty direction**, not a specific allocation.

Shared ids:

```text
broker_id = B
login     = 6100421
venue     = PEPPERSTONE_CSERVER
account   = 1369850
```

### 11.1 Initial entry

Source: `ENTRY_IN` BUY 0.10 (`1000` ticks), `position_id=9001`, seq=1 → trade `T1`.

| After persist-before-send | After fill `721=101`, `32=10` (example dest units) |
|---|---|
| 1 link `ENTRY` `PENDING` 721 NULL | dest row `(…, 101)` open qty 10; link `FILLED` 721=`101` |
| NOS omits `721` | `resolve_dest(T1)` = `101` |

### 11.2 Scale-in (same trade)

Source: second `ENTRY_IN` BUY 0.05 on `9001` → still `T1`, `was_scaled_in=true`.

| Assert |
|---|
| New `CopyIntent` `INCREASE_EXPOSURE`, new `cl_ord_id` |
| Link `SCALE_IN` persisted with `destination_position_id=101` |
| NOS **includes** `721=101`, Side=Buy |
| After fill dest qty 10+5=15; **one** dest row; **two** links |
| UNIQUE `(B, T1)` alone would have failed — must not exist |

### 11.3 Partial close

Source: `ENTRY_OUT` SELL 0.06, remaining 0.09, `was_partial_close=true`. Still `T1`, `completed=false`. First-3 counter **unchanged** (`A21`).

| Assert |
|---|
| Action `REDUCE_EXPOSURE`; dest qty = `A43` fraction of **15**, not reconverted 0.06 lots |
| NOS `721=101`, Side=Sell |
| After fill dest still `is_open`; one dest row; three links |
| No new `source_trade_id` |

### 11.4 Full close

Source: `ENTRY_OUT` SELL remaining. `T1.completed=true`.

| Assert |
|---|
| Action `CLOSE_EXPOSURE`; dest requested_qty = live dest qty (`A43` E38) |
| After fill dest `is_open=false`; `close_reason=COPIED_CLOSE` |
| Later CLOSE for `T1` is `MAP_FLAT` / no send |

### 11.5 Reversal (INOUT)

Book: `T1` long remaining 0.10. Deal `ENTRY_INOUT` SELL 0.15, `volume_closed=0.10`, leftover 0.05 short. Completes `T1`, opens `T2` seq=2 **same** `position_id=9001`. Same deal ticket on both trades.

| Step | Assert |
|---|---|
| Intent A | `T1`, `CLOSE_EXPOSURE`, `link_role=REVERSAL`, `721=101`, opposite side, dest qty 15 (whatever live is) |
| After A fill | dest `101` flat; `close_reason=COPIED_REVERSAL` |
| Intent B | `T2`, `OPEN_EXPOSURE`, `ENTRY`, `origin=REVERSAL_OPEN`, `reversal_of_source_trade_id=T1`, **no** `721` |
| After B fill | new dest `721=202`, short, qty from leftover sizing |
| `resolve_dest(T1)` | `MAP_FLAT(101)` |
| `resolve_dest(T2)` | `202` |
| Forbidden | one NOS; `721=101` on the open-leg; leaving `101` open if A filled |

If A is rejected / unknown: **no** B. Dest remains long `101`.

If A filled and B rejected: dest **flat**. Correct.

### 11.6 Hedge source is not scale-in

Three BUY tickets `position_id` 11, 12, 13 → trades `T11,T12,T13`. Three `ENTRY` links, three dest 721s. `was_scaled_in=false` on each.

### 11.7 Forgot `721` on scale-in (negative)

ENTRY bound `101`. SCALE_IN sent without `721` → venue creates `202`.

| Assert |
|---|
| `resolve_dest(T1)` = `MAP_SPLIT` |
| Risk rejects further INCREASE/REDUCE/CLOSE |
| Recon: two open dest 721s; one or both may look like `UNKNOWN_EXTERNAL` if the second was not linked |
| Repair is human / SuperAdmin — mapper does not pick a winner |

### 11.8 Unknown after send (`A42`)

ENTRY persisted, socket dropped.

| Assert |
|---|
| Link `UNKNOWN`; no second ENTRY (partial unique) |
| Recovery `ORDER_NOT_FOUND` + new 721 matching side/qty → bind `FILLED` |
| Recovery unchanged book → `NOT_ON_VENUE`; replacement ENTRY allowed (`A42` §9) |
| Recovery ambiguous → stay `UNKNOWN`, `BLOCKED_INCONSISTENT` |

### 11.9 Flatten (`A48`)

Open dest `101` (T1) and `202` (T2). Unlinked venue 721 `999` from a manual cTrader ticket.

| Assert |
|---|
| Flatten emits CLOSE links only for `101` and `202` |
| `999` remains a recon `UNKNOWN_EXTERNAL_POSITION` — not flattened |

---

## 12. Domain types to add later (do not add in this task)

Suggested under `D:\Prop\src\Domain\` when Phase 7/8 implements (names only):

```text
Enums/LinkRole.cs                  ENTRY, SCALE_IN, PARTIAL_CLOSE, CLOSE, REVERSAL
Enums/LinkStatus.cs                PENDING, BOUND, PARTIAL, FILLED, …
Enums/LinkOrigin.cs                NORMAL, REVERSAL_OPEN, FLATTEN, REPLACEMENT
Entities/DestinationPosition.cs
Entities/SourceDestinationLink.cs
Copy/DestinationPositionMapper.cs  resolve + classify + invariants
Copy/DestQtyApplicator.cs          apply_trade_er
```

Infrastructure (when migrations exist — **not** A30 v1):

```text
destination_positions
source_destination_links
```

`TraderDbContext` today (`D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`) has neither `DbSet`. Configurations folder on disk only contains five MT5 table configs. Do not treat the extra `ApplyConfiguration` names in that file as proof those tables exist.

---

## 13. Tests this spec owns

SUT: `DestinationPositionMapper` (+ dest qty applicator).  
Class: `TraderIntelligence.Tests.Unit.Execution.DestinationPositionMappingTests` (`A27`).

Must prove:

| Test | Pin |
|---|---|
| `Entry_omits_721_and_binds_from_er` | §8.1 / 11.1 |
| `Scale_in_attaches_same_721` | §8.2 / 11.2 |
| `Partial_close_is_not_a_new_source_trade` | §11.3; dest qty from `A43` fraction |
| `Full_close_uses_live_dest_qty` | `A43` E38 |
| `Reversal_is_two_intents_two_source_trades` | §8.5 / 11.5 |
| `Reversal_does_not_open_if_close_unfilled` | §8.5.3 |
| `Reversal_leaves_flat_if_open_rejected` | §8.5.4 |
| `Hedge_tickets_are_three_dest_positions` | §11.6 |
| `Unique_source_trade_alone_is_illegal` | schema / mapper insert of two SCALE_IN |
| `Resolve_split_blocks_send` | §11.7 |
| `Unknown_holds_entry_slot` | §11.8 |
| `Flatten_only_linked_721s` | §11.9 / `A48` |
| `Increase_without_map_is_mapping_missing` | §5.2 |
| `Entry_with_721_is_rejected` | `MAP_721_FORBIDDEN` |
| `Scale_in_without_721_is_rejected` | `MAP_721_REQUIRED` |
| `Qty_apply_is_idempotent_on_er_fingerprint` | `A42` / `A47` |
| `Snapshot_ap_does_not_overwrite_qty` | `A47` qty law |

Integration (when FIX harness exists): `PositionReconciliationTests` already owned by `A47` / `A10`. Mapper tests stay offline.

Reconstruction tests (`A09` rows 3–6) prove **source** flags and INOUT split. They do **not** replace this class.

---

## 14. Dashboard / logs / metrics

### 14.1 API (`A26` §6.5)

`GET /api/v1/traders/{brokerId}/{login}/source-destination-map` reads **these** tables (live mode). Shape already sketched; extend items with:

```json
{
  "reconstructedTradeId": "…",
  "sourcePositionId": 9001,
  "lifecycleSeq": 1,
  "linkRole": "SCALE_IN",
  "executionIntentId": "…",
  "clOrdId": "…",
  "destinationOrderId": "37-…",
  "destinationPositionId": "101",
  "linkStatus": "FILLED",
  "mode": "LIVE"
}
```

Shadow mode keeps `A26`’s empty dest ids. Do not populate `destinationPositionId` from shadow.

Live portfolio (`A26` §6.10) joins `destination_positions` ↔ latest non-failed link for trader display.

### 14.2 Logs (§57)

Every bind / apply / resolve-fail:

```text
correlation_id, broker_id, source_login, source_trade_id,
copy_intent_id, risk_decision_id, execution_intent_id,
cl_ord_id, cserver_order_id, destination_position_id,
link_role, link_status
```

Never log FIX `554`.

### 14.3 Metrics (add beside §58 dest family)

```text
source_destination_links_total{link_role,status}
destination_positions_open
destination_map_split_total
destination_map_missing_total
destination_position_bind_latency_ms     # persist intent → first 721
```

---

## 15. Implementation sequence (do not start early)

`A30`: `source_destination_links` and `destination_positions` are **not** in the first useful dashboard migrations. They belong with Phase 7 (dest book + recon) and Phase 8 (live send):

1. Migrate `destination_positions` + recon (`A47`) — book can exist with **zero** links.
2. Migrate `source_destination_links` + mapper unit tests (this file) **before** any `35=D`.
3. Worker: persist link in the same commit as `execution_intents`.
4. Bind `721` from ER; attach `721` on non-ENTRY NOS.
5. Only then enable `REAL_COPY_EXECUTION_ENABLED` (`A25` / `A49`).

Shadow Phase 5 stays on `A24`. Do not “preview” live links from shadow fills.

---

## 16. Explicit non-goals

- No product source, EF, or SQL in this change-set.
- No hand-written MQ5 / EX5.
- No netting-dest attach policy.
- No merging of hedge source tickets.
- No using dest quotes as source MFE/MAE (`A45`).
- No auto-accept / auto-flatten of unknown venue 721s.
- No unique `(source_trade_id)` “to keep the table small.”
- No storing dest 721 on `reconstructed_trades` (that would be one-event-one-order).

---

## 17. Traceability

| Topic | Where |
|---|---|
| Chain reconstructed trade → dest orders → dest 721 | Architecture §35; this file §0, §3 |
| Persist Position ID from ER / AP | §1.3, §6.1, §8.1 |
| Initial entry | §5, §8.1, §11.1 |
| Scale-in | §5, §8.2, §11.2 |
| Partial close | §5, §8.3, §11.3 |
| Full close | §5, §8.4, §11.4 |
| Reversal = two dest orders | §5.1, §8.5, §11.5 |
| One source event ≠ one dest order forever | §2 |
| REDUCE/CLOSE qty from mapped dest | `A43` §4.7; this file §8.3–8.4 |
| Flatten only known 721s | `A48`; §7, §11.9 |
| Table keys | `A20` §5.7; this file §6 |
| Reconstruction lifecycle / INOUT | `A21` §7.4–7.6 |
| Gap E15 / D26 | `A29` |
| Tests | `A27` DestinationPositionMappingTests; this file §13 |

---

## 18. One-line law

```text
source_trade_id  →  many execution_intents / cl_ord_ids
                 →  one dest 721 per (venue, account) while that lifecycle is copied
                 →  a new source_trade_id (and a new 721) on reversal

Persist 721. Attach 721 to add/reduce/close. Omit 721 to open.
Never one INOUT, one NOS, one row.
```
