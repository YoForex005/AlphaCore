# A67 — Architecture §60 Replay Harness Fixture JSON Format

**Artifact:** `D:\Prop\reports\swarm\20260818\A67_replay_harness.md`  
**Date:** 2026-08-18  
**Agent:** A67  
**Status:** Binding fixture-format spec for `tests/Replay`. Implementation spec only.  
**Product source:** **not modified**. No `.cs`, `.csproj`, `.json` fixture files, or test projects were created in this pass.

**Source of law**

| Doc | Role |
|---|---|
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §10–18, §20, §22–24, §31–41, §45, §60–64, §66, §69 | Architecture |
| `A20_table_catalog.md` | Durable identities |
| `A22_scoring_spec.md` | `baseline.v1` features / scores / states |
| `A23_risk_engine_spec.md` | OPEN vs CLOSE policy, sizing |
| `A24_shadow_copy_spec.md` | Destination-quote shadow book |
| `A27_test_inventory.md` §6 | Replay classes and must-prove list |
| `A30_implementation_sequence.md` I4 | Proposed `tests/Replay/Fixtures/*.json` names |
| `A37_mt5_deal_enums.md`, `A38_mt5_volume_units.md`, `A13_mt5_types_map.md` | Deal enums + volume scale |
| `A45_mfe_mae_policy.md` | `price_source` / `feature_quality` |

**Domain types already in tree (read-only alignment):**  
`NormalizedDeal`, `TradeReconstructor`, `ReconstructedTradeResult`, `FeatureSnapshot`, `BaselineScorer`, `ShadowCopyEngine`, `RiskEngine`, `QuantityNormalizer`, `VolumeConverter` (`scale = 10_000`), `SymbolNormalizer`.

---

## 0. Purpose

Architecture §60 Replay tests must drive one deterministic pipeline:

```text
historical MT5 events
        ↓
replay
        ↓
reconstruction
        ↓
features
        ↓
scores
        ↓
shadow copy
```

A27 names the harness (`ReplayHarness`, `Mt5EventReplayer`, `DeterministicClock`, `ReplaySnapshotAsserter`) and nine test classes. A30 names the first five gold files. **None of that exists yet.** This document locks the **on-disk JSON fixture format** those types will load, so later implementers do not invent a second dialect.

Same fixture + same clock + same `model_version` → bit-identical artifacts (except time-UUIDs, which the harness must mint from a fake clock / deterministic factory — never `Guid.NewGuid()`).

---

## 1. Non-goals

| Forbidden | Why |
|---|---|
| Live MT5 Manager / live cTrader TCP | Replay is offline. §61 live account is not the first test. |
| `REAL_COPY_EXECUTION_ENABLED=true` | Shadow only. Fixture `flags.real_copy_execution_enabled` must default `false`; harness refuses `true`. |
| Source deal price as a shadow fill | A24: destination QUOTE bid/ask only. |
| Fabricating MFE/MAE from entry/exit VWAP | §17, A45, A22 I7. |
| Mixing Achiever ticks with StarwaveFX ticks or cTrader quotes in one MFE path | A45 silent-mix ban. |
| Counting a partial close / SL modify / order place as trade #N | §15. |
| `login` or `deal_ticket` as a global unique key | §10. Always `(broker_id, …)`. |
| Hundredths-of-a-lot volume (`* 100`) | A38: Manager `Volume()` is `lots * 10_000`. |
| Hand-written product `.mq5` / mutating live EX5 | Out of this repo’s replay scope. |
| Creating the `tests/Replay` project in this agent pass | Format only. |

---

## 2. On-disk layout (proposed; do not create in this pass)

Matches A30 + A27:

```text
tests/Replay/
  TraderIntelligence.Tests.Replay.csproj
  ReplayHarness.cs
  Mt5EventReplayer.cs
  DeterministicClock.cs
  ReplaySnapshotAsserter.cs
  Fixtures/
    schema/ti.replay.fixture.v1.schema.json
    catalog.tsv
    first_three.json
    partial_close.json
    scale_in.json
    full_close.json
    reversal.json
    dual_broker_isolation.json
    duplicate_deal_tape.json
    no_blind_catchup.json
    leakage_first3.json
    mfe_mae_exact_ticks.json
    mfe_mae_missing_ticks.json
    shadow_stale_quote_open.json
    shadow_stale_quote_close.json
    end_to_end_determinism.json
```

A fixture is **one JSON document** (UTF-8, no BOM, LF). Large historical dumps may split the event tape:

| Mode | How |
|---|---|
| **Inline** (default, small gold files) | `events: [ … ]` inside the document |
| **External JSONL** | `events_ref: { format: "jsonl", path: "tapes/foo.jsonl" }` relative to the fixture file. Each line is one event object (same schema as `events[]`). |

The loader treats split files as one document after resolve. SHA-256 in `catalog.tsv` is over the **resolved** document (canonical JSON, see §15) so a tape edit fails the catalog.

---

## 3. Document envelope

```json
{
  "$schema": "ti.replay.fixture/v1",
  "schema_version": "1.0.0",
  "fixture_id": "first_three",
  "title": "Three completed XAUUSD lifecycles → EARLY_SCORE + SHADOW",
  "architecture_refs": ["§15", "§18", "§22", "§23", "§60", "§69.4-8", "§69.11"],
  "a27_tests": [
    "Replay.FirstUsefulVersionReplayAcceptanceTests",
    "Replay.ScoreComputationFromReplayTests"
  ],
  "notes": "Optional human string. Harness ignores.",
  "clock": { },
  "universe": { },
  "config": { },
  "events": [ ],
  "expect": { }
}
```

| Field | Required | Type | Rule |
|---|---|---|---|
| `$schema` | yes | string | Exactly `ti.replay.fixture/v1` |
| `schema_version` | yes | string | Semver. Loader accepts `1.x.y` only. Unknown major → hard fail |
| `fixture_id` | yes | string | `[a-z0-9_]{3,64}`, unique in `catalog.tsv`, equals file stem for single-file fixtures |
| `title` | yes | string | One line |
| `architecture_refs` | no | string[] | Documentation |
| `a27_tests` | no | string[] | Which A27 classes consume this file |
| `notes` | no | string | Ignored |
| `clock` | yes | object | §5 |
| `universe` | yes | object | §6 |
| `config` | yes | object | §7 |
| `events` xor `events_ref` | yes | array / object | §8. Exactly one of the two |
| `expect` | yes | object | §10. May be sparse (only stages the test asserts) |

Unknown **top-level** keys other than `notes` and `$comment` → **hard fail** (prevents silent schema drift). Nested objects may carry `notes` only.

---

## 4. Encoding laws (apply to the whole document)

### 4.1 Keys and enums

- JSON keys: `snake_case`.
- Enums: **uppercase snake** strings, not integers, except raw MT5 `action` / `entry` / `type` / `state` / `flags` on *source event bodies*, which stay the official Manager **uint** (A37).
- Closed string catalogs are listed in §16. Unknown value → hard fail.

### 4.2 Time

Two clocks exist. They must never be silently mixed.

| Clock | Field suffix | Meaning |
|---|---|---|
| **Source broker** | `time` (unix seconds), optional `time_msc` | MT5 server time on deals/orders/positions/ticks |
| **Harness / destination receive** | `*_utc` ISO-8601 | Deterministic wall used for `quote_received_at`, `decision_time`, `expires_at` |

Rules:

1. ISO fields are UTC with `Z` (`2026-01-15T10:00:00.000Z`). Offset times are rejected.
2. If both `time` and `time_utc` appear on one event, they **must** convert to the same instant (`time` = unix seconds of `time_utc`, truncated). Mismatch → hard fail.
3. Tick `time_msc` is MT5 server unix **milliseconds**. If omitted, derive `time * 1000` (A13). Prints derived this way **cannot** support `feature_quality=EXACT` (A45 §4.1.5).
4. The harness **never** calls `DateTime.UtcNow`. `DeterministicClock` starts at `clock.start_utc` and only advances as specified in §5.

### 4.3 Money, price, lots

| Kind | JSON type | Why |
|---|---|---|
| Native MT5 volume (`volume`) | integer ≥ 0 | A13/A38: `1.00 lot = 10000`. Fits `ulong` |
| Lots, prices, PnL, scores, qty, spread | **string decimal** | Bit-identical gold. Example `"2650.15"` |
| Counts, tickets, logins, seq | integer | `login` / tickets are JSON numbers (IEEE-safe below 2^53; lab tickets are) |

Optional documentary `volume_lots` may sit next to `volume`. If both present, harness **checks** `volume == round(volume_lots * config.volume.scale)` and then **uses only `volume`**. It never prefers lots.

Score gold values are `Round2` (`decimal.Round(x, 2, MidpointRounding.AwayFromZero)`) as strings `"72.50"`.

### 4.4 Identity

Every source row carries `broker_code` (and, after materialize, `broker_id`).

```text
source trader     = (broker_id, login)
source deal       = (broker_id, deal_ticket)
source order      = (broker_id, order_ticket)
source position   = (broker_id, position_id)
source trade      = reconstructed_trades.id   // lifecycle, not ticket
destination quote = (venue_code, instrument_id, fix_msg_seq_num)
copy intent       = (broker_id, login, source_trade_id, source_event_id, action)
```

`login = 1001` on `ACHIEVER` and `login = 1001` on `STARWAVEFX` are **different traders**. Dual-broker fixtures must prove this.

Stable UUIDs are **pinned in the fixture** (`universe.brokers[].id`, optional `expect.*.id`). If omitted, the harness mints:

```text
Guid = DeterministicGuid("ti.replay.v1", fixture_id, broker_code, entity_kind, natural_key)
```

Never random.

### 4.5 Volume scale (binding)

```text
config.volume.scale                 default 10000
config.volume.scale_name            "MTAPI_VOLUME_DIV"
lots = volume / scale
```

`scale = 100` (MT4 hundredths) or `100000000` (`VolumeExt`) is illegal unless `scale_name` is set and a test is explicitly labeled `volume_scale=ext`. Default fixtures use classic Manager `Volume()`.

Worked: `0.10` lots → `volume: 1000`.

---

## 5. `clock`

```json
"clock": {
  "start_utc": "2026-01-15T10:00:00.000Z",
  "tz": "UTC",
  "auto_advance": "event_time",
  "resolution_ms": 1
}
```

| Field | Required | Default | Meaning |
|---|---|---|---|
| `start_utc` | yes | — | Clock before the first event |
| `tz` | no | `UTC` | Only `UTC` is legal in v1 |
| `auto_advance` | no | `event_time` | `event_time` = set clock to event `t_utc` before dispatch. `explicit` = only `clock.advance` events move time |
| `resolution_ms` | no | `1` | Sub-ms timestamps rejected |

Shadow delay (§7, A24 §11.2): after a shadow decision the harness **advances the synthetic clock** by `shadow.execution_delay_ms`. It does **not** sleep.

---

## 6. `universe`

Static reference data materialized **before** the first event. Not replayed as events.

```json
"universe": {
  "brokers": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "code": "ACHIEVER",
      "display_name": "Achiever",
      "server_name": "Achiever-Demo"
    }
  ],
  "accounts": [
    {
      "broker_code": "ACHIEVER",
      "login": 1001,
      "group_name": "demo\\yo-2step",
      "leverage": 100,
      "currency": "USD"
    }
  ],
  "canonical_instruments": [
    { "code": "XAUUSD", "description": "Gold vs US Dollar" }
  ],
  "source_symbol_mappings": [
    { "broker_code": "ACHIEVER", "source_symbol": "XAUUSD.", "canonical": "XAUUSD" }
  ],
  "destination_venues": [
    {
      "venue_code": "PEPPERSTONE_CSERVER",
      "quote_port": 5211,
      "trade_port": 5212
    }
  ],
  "destination_symbols": [
    {
      "venue_code": "PEPPERSTONE_CSERVER",
      "instrument_id": "123456",
      "canonical": "XAUUSD",
      "min_qty": "0.01",
      "max_qty": "50",
      "step": "0.01",
      "precision": 2,
      "contract_value_per_unit": "1"
    }
  ]
}
```

Rules:

- `brokers[].code` is unique. Required codes for dual-broker tests: `ACHIEVER`, `STARWAVEFX`.
- Every `events[]` `broker_code` must exist here.
- Unmapped `source_symbol` does **not** become `XAUUSD` (A30 / §16). Reconstruction still emits a trade with `canonical_symbol` empty or raw; first-3 counter **ignores** it.
- `destination_symbols[].instrument_id` is a **string** (cTrader numeric id). Never assume FIX tag 55 equals `"XAUUSD"`.
- Do not put passwords, hosts of production TRADE, or manager secrets in fixtures.

Recommended pinned UUIDs (optional convention, not a secret):

| Code | UUID |
|---|---|
| `ACHIEVER` | `11111111-1111-1111-1111-111111111111` |
| `STARWAVEFX` | `22222222-2222-2222-2222-222222222222` |
| `PEPPERSTONE_CSERVER` | `33333333-3333-3333-3333-333333333333` |

---

## 7. `config`

All tunables the pipeline is allowed to read. Missing keys take the v1 defaults below. Extra keys → hard fail.

```json
"config": {
  "flags": {
    "real_copy_execution_enabled": false,
    "shadow_copy_enabled": true,
    "ctrader_fix_quote_enabled": true,
    "stop_new_shadow_opens": false,
    "shadow_book_flatten": false
  },
  "volume": {
    "scale": 10000,
    "scale_name": "MTAPI_VOLUME_DIV"
  },
  "score": {
    "version": "baseline.v1",
    "window": "EXPANDING"
  },
  "shadow": {
    "model_version": "shadow.v1",
    "cost_model_id": "assumed.pepperstone.xau.v0",
    "cost_quality": "ASSUMED",
    "execution_delay_ms": 250,
    "adverse_ticks": "0",
    "allocation_factor": "0.10",
    "fill_assumption": "SINGLE_FILL_FULL_QTY"
  },
  "timing": {
    "max_quote_age_open_ms": 3000,
    "max_quote_age_close_ms": 15000,
    "max_quote_age_close_stale_fallback_ms": 60000,
    "max_signal_age_open_ms": 15000,
    "max_signal_age_close_ms": 300000,
    "open_expires_at_offset_ms": 15000,
    "close_expires_at_offset_ms": 300000,
    "max_quote_age_mark_ms": 5000
  },
  "risk": {
    "max_allowed_spread": "2.00",
    "max_price_move": "3.00",
    "max_slippage": "1.50",
    "max_position_quantity": "5",
    "max_open_positions": 20,
    "max_xau_gross": "20",
    "max_xau_net": "10",
    "max_loss_per_trader": "500",
    "max_daily_execution_loss": "2000",
    "max_portfolio_drawdown": "3000",
    "block_martingale": true,
    "block_abnormal_sizing": true
  },
  "features": {
    "allow_mfe_mae": true,
    "exact_requires_bid_ask": true
  }
}
```

Binding defaults that tests rely on:

- `real_copy_execution_enabled` **must** be `false`. `true` → loader reject (A24, §41, §70.12).
- `score.version` default `baseline.v1` (A22). Gold `expect.scores` are meaningless across versions.
- `shadow.allocation_factor` is the source-lots → dest-qty scalar **before** step/min/max (`QuantityNormalizer`). `0.10` source lots × `0.10` factor = `0.01` dest if step allows — **not** `0.10` dest (A24 acceptance #8).
- Commission/swap on shadow come from `cost_model_id`, never from source `commission` / `storage` (A24 §11.6).

Current `BaselineScorer` in `src/Domain/Scoring/BaselineScorer.cs` is a **stub** (different martingale ratio `1.25` vs A22 `1.80`, population CV vs sample CV, no MFE). Replay gold for scores binds to **A22 `baseline.v1`**, not the stub. Until the real calculator lands, score-stage tests stay `pending` or compare only qualitative `state` / `early_score_eligible` if the fixture sets `expect.scores.mode = "qualitative"`.

---

## 8. `events[]` — the historical tape

### 8.1 Common event envelope

```json
{
  "seq": 1,
  "kind": "mt5.deal",
  "t_utc": "2026-01-15T10:00:01.000Z",
  "broker_code": "ACHIEVER",
  "source_event_id": "deal:ACHIEVER:900001",
  "body": { }
}
```

| Field | Required | Rule |
|---|---|---|
| `seq` | yes | Unique positive integer in the document. Stable tie-break |
| `kind` | yes | Closed catalog §8.2 |
| `t_utc` | yes | Dispatch instant on the harness clock |
| `broker_code` | yes if kind starts with `mt5.` | Must exist in `universe.brokers` |
| `venue_code` | yes if kind starts with `dest.` | Must exist in `universe.destination_venues` |
| `source_event_id` | yes for persistable source events | Idempotency key with `broker_code`. See §8.3 |
| `body` | yes | Kind-specific payload |

**Dispatch order** (normative):

```text
ORDER BY t_utc ASC, seq ASC
```

The file need not be pre-sorted; the replayer sorts. A test may set `config.replay.require_pre_sorted: true` to fail unsorted tapes (catches export bugs).

Duplicate `source_event_id` on the same `broker_code`: **first persist wins**, later is dropped and counted in `expect.ingest.duplicates_dropped` (A27 `HistoricalMt5EventReplayTests`). Same `deal_ticket` with a **different** `source_event_id` is still a duplicate deal key `(broker_id, deal_ticket)` and is dropped.

### 8.2 `kind` catalog

| `kind` | Stage that consumes it | Persist target |
|---|---|---|
| `mt5.deal` | ingest → reconstruct | `mt5_deals` |
| `mt5.order` | ingest → reconstruct (order_count, SL/TP) | `mt5_orders` |
| `mt5.position_upsert` | live book only; **not** a trade | `mt5_positions_current` |
| `mt5.position_delete` | live book close | delete current row |
| `mt5.account_snapshot` | optional equity path; **not** score input at v1 | `mt5_account_snapshots` |
| `mt5.tick` | MFE/MAE only | `mt5_xau_ticks` |
| `mt5.bar` | `BAR_APPROXIMATION` only | optional; never `EXACT` |
| `dest.quote` | shadow + risk | `destination_quotes` |
| `dest.quote_down` | QUOTE unavailable | session flag |
| `dest.quote_up` | QUOTE restored | session flag |
| `control.kill_switch` | risk / shadow | kill-switch mode |
| `control.clock_advance` | clock only | — |
| `control.checkpoint` | snapshot compare | — |

No `dest.trade.*` / `fix.new_order_single` in v1 replay fixtures. Shadow does not send TRADE.

### 8.3 `source_event_id` construction

| Kind | Canonical id |
|---|---|
| `mt5.deal` | `deal:{broker_code}:{ticket}` |
| `mt5.order` | `order:{broker_code}:{ticket}:{state}` |
| `mt5.position_upsert` | `pos:{broker_code}:{ticket}:{time_update}` |
| `mt5.tick` | `tick:{broker_code}:{symbol}:{time_msc}:{flags}:{ingest_seq}` |
| `dest.quote` | `q:{venue_code}:{instrument_id}:{fix_msg_seq_num}` |

Do not use a bare ticket. Two brokers can share ticket `1`.

### 8.4 `mt5.deal` body

Aligns with `DealData` in `mt5_types.h` **plus the fields the C++ `to_json` currently drops**. Replay is the product ingest contract, not a dump of the incomplete adapter.

| JSON key | Required | Maps to | Notes |
|---|---|---|---|
| `ticket` | yes | `DealTicket` | uint64 |
| `login` | yes | `Login` | |
| `order` | yes | `OrderTicket` | 0 allowed |
| `position` | **yes for action 0/1** | `PositionId` | **Not in C++ `to_json` today.** Reconstruction groups by this. Missing → hard fail for trading deals |
| `symbol` | yes | `SourceSymbol` | raw broker string |
| `action` | yes | `DealAction` | uint `0..20` (A37). Only `0`/`1` form positions |
| `entry` | yes | `DealEntry` | `0=IN 1=OUT 2=INOUT 3=OUT_BY` |
| `volume` | yes | `VolumeNative` | integer, scale 10_000 |
| `price` | yes | `Price` | string decimal |
| `profit` | yes | `Profit` | string; source realized on this deal |
| `commission` | no | `Commission` | string, default `"0"` |
| `storage` | no | `Swap` | string, default `"0"` (SDK name `storage`) |
| `time` | yes* | `DealTime` | unix seconds UTC |
| `time_utc` | yes* | `DealTime` | ISO; at least one of `time` / `time_utc` |
| `comment` | no | `Comment` | |
| `price_sl` | no | `StopLoss` | not on `DealData`; needed for `initial_sl` |
| `price_tp` | no | `TakeProfit` | same |

`action` 2–20 (balance, credit, commission, …) are ingested and **ignored** by `TradeReconstructor` (`IsTradingDeal` is Buy/Sell only). Fixtures that mix them prove they do not increment first-3.

`entry=2` (`INOUT`) is a reversal on the same `position` (close remaining + leftover opposite). Expect **two** reconstructed lifecycles (A24 §4.1, A27 reversal tests).

Example — open 0.10 long:

```json
{
  "seq": 10,
  "kind": "mt5.deal",
  "t_utc": "2026-01-15T10:05:00.000Z",
  "broker_code": "ACHIEVER",
  "source_event_id": "deal:ACHIEVER:900001",
  "body": {
    "ticket": 900001,
    "login": 1001,
    "order": 800001,
    "position": 700001,
    "symbol": "XAUUSD.",
    "action": 0,
    "entry": 0,
    "volume": 1000,
    "price": "2650.10",
    "profit": "0",
    "commission": "-0.30",
    "storage": "0",
    "time": 1768473900,
    "time_utc": "2026-01-15T10:05:00.000Z",
    "price_sl": "2640.00",
    "price_tp": "2670.00"
  }
}
```

### 8.5 `mt5.order` body

| JSON key | Required | Notes |
|---|---|---|
| `ticket` | yes | |
| `login` | yes | |
| `symbol` | yes | |
| `type` | yes | `mt5_op`: 0 BUY … 5 SELL_STOP |
| `state` | yes | raw Manager order state uint |
| `volume` | yes | **initial** volume (`VolumeInitial`) |
| `price_order` | yes | string |
| `price_current` | no | |
| `price_sl` / `price_tp` | no | SL/TP modifications are **not** trades |
| `time_setup` / `time_utc` | yes* | |
| `position` | no | when known |
| `comment` | no | |

An isolated SL change (`mt5.order` only, no new deal) must not create a `ReconstructedTrade` or increment `N`.

### 8.6 `mt5.position_upsert` / `mt5.position_delete` body

| JSON key | Required | Notes |
|---|---|---|
| `ticket` | yes | `position_id` |
| `login` | yes | |
| `symbol` | yes | |
| `action` | upsert yes | 0 BUY / 1 SELL |
| `volume` | upsert yes | current remaining native volume |
| `price_open` / `price_current` | upsert yes | |
| `price_sl` / `price_tp` | no | |
| `profit` / `storage` | no | |
| `time_create` / `time_update` | upsert yes | unix seconds |
| `comment` | no | |

`position_delete` body needs `ticket` + `login` only. Current book is **not** history. Reconstruction never reads it as a lifecycle.

### 8.7 `mt5.tick` body (source book only)

| JSON key | Required | Notes |
|---|---|---|
| `symbol` | yes | source symbol, same broker as the trade |
| `bid` / `ask` | yes for EXACT | `bid > 0`, `ask >= bid` |
| `last` | no | last-only prints cannot make `EXACT` |
| `volume` | no | last-trade volume, same 10_000 scale |
| `time` / `time_msc` | yes* | A45: fabricated timestamps disqualify EXACT |
| `flags` | no | default `0` |
| `ingest_seq` | yes if same `time_msc`+`flags` repeats | A20 unique key |
| `timestamp_quality` | no | `BROKER` (default) \| `WALL_DERIVED` |

Harness labels the tape `price_source = {BROKER}_MT5_TICKS` from `broker_code`. It **must not** attach ticks to another broker’s trades.

If `events` contain **no** ticks and no bars, MFE/MAE are omitted (`feature_quality` absent / `UNAVAILABLE`, `mfe_mae_used=false`). That is the correct outcome (A45 §4.3).

### 8.8 `mt5.bar` body

`time`, `open`, `high`, `low`, `close`, `symbol`. Forces `price_source=BAR_APPROXIMATION`, `feature_quality=APPROXIMATE` when coverage meets A45 §4.2. Never `EXACT`.

### 8.9 `dest.quote` body

Destination QUOTE tape. **This** prices shadow. Never a source tick.

| JSON key | Required | Notes |
|---|---|---|
| `canonical` | yes | `XAUUSD` |
| `instrument_id` | yes | cTrader id string, must map |
| `bid` / `ask` | yes | `bid > 0`, `ask >= bid` else unusable |
| `quote_received_at_utc` | yes | harness receive time (clock) |
| `venue_timestamp_utc` | no | never invent |
| `fix_msg_seq_num` | yes | replay/audit |
| `md_entry_source` | yes | `SNAPSHOT` \| `INCREMENTAL` |

Unusable quotes (`ask < bid`, non-positive) are stored as rejected snapshots and must **not** fill OPEN.

### 8.10 Control events

`dest.quote_down` / `dest.quote_up` — body `{ "reason": "DISCONNECT" }`. While down: no OPEN/INCREASE shadow (A24 / §62). REDUCE/CLOSE uses close waterfall.

`control.kill_switch` body:

```json
{ "mode": "NONE" | "STOP_NEW_EXECUTION" | "EMERGENCY_FLATTEN" }
```

`control.clock_advance` body `{ "to_utc": "..." }` or `{ "by_ms": 180000 }`.

`control.checkpoint` body `{ "id": "after_trade_3" }` — forces an `expect.checkpoints[]` compare at this instant **before** later events (leakage test).

---

## 9. Pipeline semantics the fixture assumes

The harness is in-process. No PostgreSQL is required for unit-speed replay (A27: “No live venue I/O in Unit or Replay”). Persistence may be an in-memory store that still enforces the unique keys.

```text
for event in sorted(events):
    clock.Set(event.t_utc)
    switch kind:
      mt5.*     → validate → dedupe → persist raw → outbox
                  TradeReconstructor.Reconstruct(broker, login, deals)
      mt5.tick  → append source tape (never dest)
      dest.quote→ quote cache + history
      dest.quote_down/up → venue quote health
      kill_switch → mode
      checkpoint → snapshot as_of clock
    after each completed XAU lifecycle close:
      features at N (as_of = closed_at, trades[1..N] only)
      if N >= 3: score baseline.v1
      if trader state in {SHADOW} and shadow_copy_enabled:
          classify action_class
          CopyIntent (expires_at, max_signal_age)
          RiskEngine (OPEN stricter than CLOSE)
          shadow order persist-before-sim
          clock += execution_delay_ms
          re-read dest quote at new clock
          fill from dest bid/ask (A24 §11.1)
          update shadow position / pnl / slippage
```

Invariants the loader/asserter always checks, even if `expect` omits them:

1. No `NewOrderSingle` / TRADE send.
2. No shadow fill whose `price` equals source deal price unless that number also equals the dest touch **and** `quote_id` is recorded.
3. `N` counts only `completed && canonical==XAUUSD && closed_volume>0`.
4. At any checkpoint with `N==3`, state ∉ {`LIVE`, `LIVE_CANDIDATE`} (A22 I4/I5).
5. Score at checkpoint `after_trade_3` must not see deals with `time_utc > as_of` (A22 I6, A27 leakage).
6. Dual-broker same ticket does not collapse.

---

## 10. `expect` — golden snapshots

Sparse: include only stages the test cares about. Absent stage = not compared. Present stage = compared with the rules in §11.

```json
"expect": {
  "ingest": { },
  "reconstruction": { },
  "features": { },
  "scores": { },
  "shadow": { },
  "invariants": { },
  "checkpoints": [ ]
}
```

### 10.1 `expect.ingest`

```json
"ingest": {
  "deals_accepted": 6,
  "deals_duplicate_dropped": 1,
  "orders_accepted": 4,
  "ticks_accepted": 0,
  "quotes_accepted": 12,
  "quotes_rejected_unusable": 0,
  "brokers_seen": ["ACHIEVER"],
  "logins_seen": [
    { "broker_code": "ACHIEVER", "login": 1001 }
  ]
}
```

### 10.2 `expect.reconstruction`

Array of logical trades in **stable order**:

```text
ORDER BY opened_at ASC, position_id ASC, id ASC
```

Completed-XAU counting order (different list) is:

```text
WHERE completed && canonical_symbol==XAUUSD && closed_volume>0
ORDER BY closed_at ASC, opened_at ASC, id ASC
```

Each element matches §14 / `ReconstructedTradeResult`:

```json
{
  "id": "ACHIEVER:1001:700001:1768473900000",
  "broker_code": "ACHIEVER",
  "login": 1001,
  "position_id": 700001,
  "canonical_symbol": "XAUUSD",
  "source_symbol": "XAUUSD.",
  "direction": "LONG",
  "opened_at_utc": "2026-01-15T10:05:00.000Z",
  "closed_at_utc": "2026-01-15T10:50:00.000Z",
  "entry_vwap": "2650.10",
  "exit_vwap": "2655.40",
  "initial_volume_lots": "0.10",
  "max_volume_lots": "0.10",
  "closed_volume_lots": "0.10",
  "remaining_volume_lots": "0.00",
  "gross_realized_pnl": "53.00",
  "commission": "-0.60",
  "swap": "0",
  "fees": "0",
  "net_realized_pnl": "52.40",
  "deal_count": 2,
  "order_count": 2,
  "deal_tickets": [900001, 900002],
  "initial_sl": "2640.00",
  "initial_tp": "2670.00",
  "final_sl": "2640.00",
  "final_tp": "2670.00",
  "was_scaled_in": false,
  "was_partial_close": false,
  "was_averaged_down": false,
  "completed": true
}
```

`id` may be omitted if the test compares on `(broker_code, login, position_id, opened_at_utc)`. Current `TradeReconstructor` uses `{BrokerId}:{Login}:{PositionId}:{OpenedAtUnixMs}`.

`direction`: `LONG` \| `SHORT` (maps `TradeDirection`).

Open (incomplete) lifecycles are allowed (`completed=false`, `closed_at_utc=null`) and **must not** appear in first-3.

Compare set: if `reconstruction.match = "completed_xau_only"` (default for score fixtures), ignore open/non-XAU. If `"all"`, compare the full array including open and unmapped symbols.

### 10.3 `expect.features`

One snapshot per `(broker_code, login, n, window)`.

```json
{
  "broker_code": "ACHIEVER",
  "login": 1001,
  "n": 3,
  "as_of_utc": "2026-01-15T12:00:00.000Z",
  "window": "FIRST3",
  "feature_schema_version": "baseline.v1",
  "price_source": null,
  "feature_quality": "UNAVAILABLE",
  "mfe_mae_used": false,
  "completed_xau_trades": 3,
  "net_pnl": "100.00",
  "gross_profit": "140.00",
  "gross_loss": "40.00",
  "profit_factor": "3.50",
  "lot_cv": "0",
  "loss_size_cv": "0",
  "martingale": false,
  "averaging_down": false,
  "lot_escalation": false,
  "martingale_events": 0,
  "average_hold_seconds": "2500",
  "sl_use_rate": "1.00",
  "max_drawdown": "40.00",
  "trade_frequency_per_day": "3.00",
  "average_mfe": null,
  "average_mae": null
}
```

Rules:

- `mfe_mae_used` is `true` **only** if `feature_quality == EXACT` and both averages are non-null (A22 I7).
- If the fixture has no tick tape: `feature_quality` is `UNAVAILABLE` (or omitted) and MFE/MAE keys are JSON `null`. **Do not write `"0"`.**
- `price_source` closed catalog: `ACHIEVER_MT5_TICKS` | `STARWAVEFX_MT5_TICKS` | `{CODE}_MT5_TICKS` | `{CODE}_MT5_TICK_HISTORY` | `{CODE}_MT5_TICK_POLL` | `BAR_APPROXIMATION` | `null`.  
  **`CTRADER_FIX_QUOTES` is illegal here** (destination book).
- A22 Case A–F (A22 §12) are the qualitative acceptance fixtures; numeric gold is pinned once `DeterministicScorer` exists (`expect.scores.mode = "numeric"`).

### 10.4 `expect.scores`

```json
{
  "mode": "qualitative",
  "version": "baseline.v1",
  "broker_code": "ACHIEVER",
  "login": 1001,
  "n": 3,
  "as_of_utc": "2026-01-15T12:00:00.000Z",
  "window": "EXPANDING",
  "early_score_eligible": true,
  "early_score_eligible_emitted": 1,
  "risk_score": "12.00",
  "behavior_score": "88.00",
  "early_quality_score": "70.00",
  "state": "SHADOW",
  "forbidden_states": ["LIVE", "LIVE_CANDIDATE"],
  "flags": [],
  "rank": {
    "universe": "fixture",
    "position": 1,
    "ordered_logins": [
      { "broker_code": "ACHIEVER", "login": 1001, "early_quality_score": "70.00" }
    ]
  }
}
```

| `mode` | Compare |
|---|---|
| `qualitative` | `state`, `early_score_eligible`, `forbidden_states`, flags. Ignore numeric scores if omitted |
| `numeric` | all present decimals, exact `Round2` (tolerance `0` once pinned; A22 temporary `0.05` only if `score_abs_tol` set) |
| `absent` | assert **no** official score row (Case D, `N<3`) |

`early_score_eligible_emitted` must be `1` under replay of trade #3 (A22 T3) — idempotent if the close deal is duplicated on the tape.

Ranking (`Replay.ScoreComputationFromReplayTests`) uses `early_quality_score` DESC, then `behavior_score` DESC, then `(broker_code, login)` ASC. **Not** raw `net_pnl` (A22 I9).

### 10.5 `expect.shadow`

```json
{
  "model_version": "shadow.v1",
  "orders": [
    {
      "source_event_id": "deal:ACHIEVER:900001",
      "action_class": "OPEN_EXPOSURE",
      "direction": "LONG",
      "status": "FILLED",
      "qty": "0.01",
      "decision_quote_seq": 4,
      "fill_quote_seq": 5,
      "fill_price": "2650.35",
      "fill_side_px": "ASK",
      "spread": "0.30",
      "quote_age_ms": 250,
      "source_price": "2650.10",
      "signed_slippage": "0.25",
      "fill_quality": "LIVE_QUOTE",
      "cost_quality": "ASSUMED"
    }
  ],
  "positions": [
    {
      "source_trade_ref": { "broker_code": "ACHIEVER", "login": 1001, "position_id": 700001, "opened_at_utc": "2026-01-15T10:05:00.000Z" },
      "direction": "LONG",
      "qty": "0.00",
      "open": false,
      "entry_price": "2650.35",
      "exit_price": "2655.10",
      "realized_pnl": "4.75",
      "unrealized_pnl": null
    }
  ],
  "rejects": [
    { "source_event_id": "deal:ACHIEVER:900099", "action_class": "OPEN_EXPOSURE", "reason": "QUOTE_STALE" }
  ],
  "expired_intents": 20,
  "new_order_single_count": 0
}
```

Fill price law (A24 §11.1) — asserter re-derives and must match `fill_price`:

```text
OPEN/INCREASE LONG  → dest ASK
OPEN/INCREASE SHORT → dest BID
REDUCE/CLOSE  LONG  → dest BID
REDUCE/CLOSE  SHORT → dest ASK
```

`decision_quote_seq` / `fill_quote_seq` refer to `seq` of `dest.quote` events (not `fix_msg_seq_num`). After delay the fill quote is the latest quote with `quote_received_at_utc >= sim_send_at`, else latest (A24 §11.2).

`unrealized_pnl` is JSON `null` when unpriced — **never `"0"`** (A24 §12).

`new_order_single_count` must be `0` on every fixture.

Reversal: expect **two** orders, CLOSE then OPEN, never one blended order.

### 10.6 `expect.invariants`

Boolean / count locks. All default to the architecture-true value if omitted; listing them makes the failure message explicit.

```json
"invariants": {
  "no_live_at_n_le_3": true,
  "no_source_priced_shadow_fill": true,
  "no_mfe_without_ticks": true,
  "no_cross_broker_tick_mix": true,
  "no_catchup_open_after_quote_gap": true,
  "partial_close_does_not_increment_n": true,
  "first3_sees_only_trades_1_to_3": true,
  "real_copy_execution_enabled": false,
  "determinism_hash_sha256": "optional-after-first-green-run"
}
```

### 10.7 `expect.checkpoints[]`

For leakage and mid-tape score tests:

```json
"checkpoints": [
  {
    "id": "after_trade_3",
    "as_of_utc": "2026-01-15T12:00:00.000Z",
    "n": 3,
    "features_ref": { "window": "FIRST3" },
    "scores": { "state": "SHADOW", "early_score_eligible": true },
    "must_not_include_deal_tickets": [900010]
  }
]
```

The harness either hits a `control.checkpoint` with the same `id` or synthesizes a cut at `as_of_utc` (include events with `t_utc <= as_of_utc` only).

---

## 11. Comparison rules (`ReplaySnapshotAsserter`)

| Kind | Rule |
|---|---|
| Integers, enums, bools, ids, tickets | Exact |
| ISO timestamps | Exact string after normalize to `yyyy-MM-ddTHH:mm:ss.fffZ` |
| Decimal strings | `decimal` equality (so `"1.0"` == `"1.00"`). Do not parse as `double` |
| Scores in `numeric` mode | `Round2` equality unless fixture sets `score_abs_tol` |
| Arrays of trades / orders | Order-sensitive after the stable ORDER BY in §10.2 / fill time |
| `null` vs missing | `null` is a value (unpriced, omitted MFE). Missing key = “do not compare this field” |
| Extra actual rows not in expect | Fail (unexpected reconstructed trade / shadow order) |
| Extra expect rows | Fail |

Determinism test (A27 `EndToEndReplayDeterminismTests`): run twice, hash canonical JSON of `{reconstruction, features, scores, shadow}` with SHA-256, compare. Optional `invariants.determinism_hash_sha256` pins that hash once green.

---

## 12. Required fixture catalog (maps A27 / A30 / A22)

| `fixture_id` | File (A30 / this spec) | Proves | Minimum tape |
|---|---|---|---|
| `full_close` | `full_close.json` | One IN + one OUT → `completed=true`, net = gross+commission+swap+fees | 2 deals, dest quotes for both legs if shadow asserted |
| `partial_close` | `partial_close.json` | OUT that leaves remainder: `was_partial_close=true`, `N` does **not** increment until flat | 3+ deals same `position` |
| `scale_in` | `scale_in.json` | Second IN same side: one trade, `was_scaled_in`, `max_volume` grows | 2 IN + 1 OUT |
| `reversal` | `reversal.json` | `entry=INOUT` or opposite close+open → two lifecycles, no ticket reuse | 1 IN + 1 INOUT (or OUT+IN) |
| `first_three` | `first_three.json` | Exactly 3 completed XAU → `EARLY_SCORE_ELIGIBLE`, state SHADOW/WATCH/EARLY_SCORE, never LIVE | 3 lifecycles + dest quotes |
| `dual_broker_isolation` | `dual_broker_isolation.json` | Same login/ticket on ACHIEVER vs STARWAVEFX → two traders / two deals | 2 brokers |
| `duplicate_deal_tape` | `duplicate_deal_tape.json` | Live + backfill same `(broker, ticket)` → one row | deal then duplicate |
| `no_blind_catchup` | `no_blind_catchup.json` | `dest.quote_down` 3 min + 20 source opens → 0 shadow opens; intents expired (§63) | gap + 20 IN |
| `leakage_first3` | `leakage_first3.json` | Checkpoint at trade #3; tape continues with trade #4; FIRST3 unchanged (A22 T8) | 4 lifecycles |
| `mfe_mae_exact_ticks` | `mfe_mae_exact_ticks.json` | Same-broker bid/ask coverage → `EXACT`, `mfe_mae_used=true` | ticks meeting A45 §4.1 |
| `mfe_mae_missing_ticks` | `mfe_mae_missing_ticks.json` | No ticks → no numbers, quality unavailable | deals only |
| `shadow_stale_quote_open` | `shadow_stale_quote_open.json` | OPEN rejected `QUOTE_STALE` / missing quote | old quote + open |
| `shadow_stale_quote_close` | `shadow_stale_quote_close.json` | Existing shadow still closes (lenient waterfall) | open filled, then stale close |
| `end_to_end_determinism` | `end_to_end_determinism.json` | Two-run hash lock | small complete tape |
| `case_a_clean_three` | optional alias of `first_three` or sibling | A22 Case A qualitative SHADOW | 3 equal-lot SL trades |
| `case_b_martingale` | recommended | A22 Case B `RISK_BLOCKED` despite huge NET | 0.10 / 0.20 / 0.40 after losses |
| `case_d_two_trades` | recommended | `INSUFFICIENT_DATA`, no official rank | 2 lifecycles |

A27 also requires reconstruction of those shapes in unit tests; the **same JSON** should be loaded by `XauUsdFirstThreeTradeFixture` / `Mt5HistoricalEventFixture` so unit and replay do not diverge.

---

## 13. Worked document — `first_three` (normative shape, illustrative numbers)

Illustrative prices/PnL. When the real scorer lands, replace `expect.scores.mode` with `numeric` and pin Round2 values from the reference implementation.

```json
{
  "$schema": "ti.replay.fixture/v1",
  "schema_version": "1.0.0",
  "fixture_id": "first_three",
  "title": "Three completed XAUUSD lifecycles on Achiever login 1001",
  "architecture_refs": ["§15", "§18", "§22", "§23", "§60", "§69.6-8", "§69.11"],
  "a27_tests": [
    "Replay.FirstUsefulVersionReplayAcceptanceTests",
    "Replay.ReconstructionFromReplayTests",
    "Replay.ScoreComputationFromReplayTests",
    "Replay.ShadowCopyFromReplayTests"
  ],
  "clock": {
    "start_utc": "2026-01-15T09:59:00.000Z",
    "tz": "UTC",
    "auto_advance": "event_time"
  },
  "universe": {
    "brokers": [
      {
        "id": "11111111-1111-1111-1111-111111111111",
        "code": "ACHIEVER",
        "display_name": "Achiever"
      }
    ],
    "accounts": [
      { "broker_code": "ACHIEVER", "login": 1001, "group_name": "demo\\yo-2step" }
    ],
    "canonical_instruments": [{ "code": "XAUUSD" }],
    "source_symbol_mappings": [
      { "broker_code": "ACHIEVER", "source_symbol": "XAUUSD.", "canonical": "XAUUSD" }
    ],
    "destination_venues": [{ "venue_code": "PEPPERSTONE_CSERVER" }],
    "destination_symbols": [
      {
        "venue_code": "PEPPERSTONE_CSERVER",
        "instrument_id": "123456",
        "canonical": "XAUUSD",
        "min_qty": "0.01",
        "max_qty": "50",
        "step": "0.01",
        "precision": 2
      }
    ]
  },
  "config": {
    "flags": {
      "real_copy_execution_enabled": false,
      "shadow_copy_enabled": true,
      "ctrader_fix_quote_enabled": true
    },
    "volume": { "scale": 10000, "scale_name": "MTAPI_VOLUME_DIV" },
    "score": { "version": "baseline.v1", "window": "EXPANDING" },
    "shadow": {
      "model_version": "shadow.v1",
      "execution_delay_ms": 250,
      "allocation_factor": "0.10",
      "fill_assumption": "SINGLE_FILL_FULL_QTY",
      "cost_quality": "ASSUMED"
    }
  },
  "events": [
    {
      "seq": 1,
      "kind": "dest.quote",
      "t_utc": "2026-01-15T10:00:00.000Z",
      "venue_code": "PEPPERSTONE_CSERVER",
      "source_event_id": "q:PEPPERSTONE_CSERVER:123456:1",
      "body": {
        "canonical": "XAUUSD",
        "instrument_id": "123456",
        "bid": "2650.00",
        "ask": "2650.30",
        "quote_received_at_utc": "2026-01-15T10:00:00.000Z",
        "fix_msg_seq_num": 1,
        "md_entry_source": "SNAPSHOT"
      }
    },
    {
      "seq": 2,
      "kind": "mt5.deal",
      "t_utc": "2026-01-15T10:05:00.000Z",
      "broker_code": "ACHIEVER",
      "source_event_id": "deal:ACHIEVER:900001",
      "body": {
        "ticket": 900001,
        "login": 1001,
        "order": 800001,
        "position": 700001,
        "symbol": "XAUUSD.",
        "action": 0,
        "entry": 0,
        "volume": 1000,
        "price": "2650.10",
        "profit": "0",
        "commission": "-0.30",
        "storage": "0",
        "time_utc": "2026-01-15T10:05:00.000Z",
        "price_sl": "2640.00",
        "price_tp": "2670.00"
      }
    },
    {
      "seq": 3,
      "kind": "dest.quote",
      "t_utc": "2026-01-15T10:49:59.000Z",
      "venue_code": "PEPPERSTONE_CSERVER",
      "source_event_id": "q:PEPPERSTONE_CSERVER:123456:2",
      "body": {
        "canonical": "XAUUSD",
        "instrument_id": "123456",
        "bid": "2655.10",
        "ask": "2655.40",
        "quote_received_at_utc": "2026-01-15T10:49:59.000Z",
        "fix_msg_seq_num": 2,
        "md_entry_source": "INCREMENTAL"
      }
    },
    {
      "seq": 4,
      "kind": "mt5.deal",
      "t_utc": "2026-01-15T10:50:00.000Z",
      "broker_code": "ACHIEVER",
      "source_event_id": "deal:ACHIEVER:900002",
      "body": {
        "ticket": 900002,
        "login": 1001,
        "order": 800002,
        "position": 700001,
        "symbol": "XAUUSD.",
        "action": 1,
        "entry": 1,
        "volume": 1000,
        "price": "2655.40",
        "profit": "53.00",
        "commission": "-0.30",
        "storage": "0",
        "time_utc": "2026-01-15T10:50:00.000Z"
      }
    }
  ],
  "expect": {
    "ingest": {
      "deals_accepted": 2,
      "deals_duplicate_dropped": 0
    },
    "reconstruction": {
      "match": "completed_xau_only",
      "completed_xau_count": 1,
      "trades": [
        {
          "broker_code": "ACHIEVER",
          "login": 1001,
          "position_id": 700001,
          "canonical_symbol": "XAUUSD",
          "source_symbol": "XAUUSD.",
          "direction": "LONG",
          "opened_at_utc": "2026-01-15T10:05:00.000Z",
          "closed_at_utc": "2026-01-15T10:50:00.000Z",
          "entry_vwap": "2650.10",
          "exit_vwap": "2655.40",
          "initial_volume_lots": "0.10",
          "max_volume_lots": "0.10",
          "closed_volume_lots": "0.10",
          "remaining_volume_lots": "0.00",
          "was_scaled_in": false,
          "was_partial_close": false,
          "was_averaged_down": false,
          "completed": true,
          "deal_tickets": [900001, 900002]
        }
      ]
    },
    "features": [
      {
        "n": 1,
        "window": "PROVISIONAL",
        "completed_xau_trades": 1,
        "feature_quality": "UNAVAILABLE",
        "mfe_mae_used": false,
        "average_mfe": null,
        "average_mae": null
      }
    ],
    "scores": {
      "mode": "qualitative",
      "n": 1,
      "early_score_eligible": false,
      "state": "INSUFFICIENT_DATA",
      "forbidden_states": ["LIVE", "LIVE_CANDIDATE", "SHADOW", "EARLY_SCORE"]
    },
    "shadow": {
      "new_order_single_count": 0,
      "orders": [
        {
          "source_event_id": "deal:ACHIEVER:900001",
          "action_class": "OPEN_EXPOSURE",
          "direction": "LONG",
          "status": "FILLED",
          "fill_side_px": "ASK",
          "qty": "0.01"
        },
        {
          "source_event_id": "deal:ACHIEVER:900002",
          "action_class": "CLOSE_EXPOSURE",
          "direction": "LONG",
          "status": "FILLED",
          "fill_side_px": "BID",
          "qty": "0.01"
        }
      ]
    },
    "invariants": {
      "no_live_at_n_le_3": true,
      "no_source_priced_shadow_fill": true,
      "no_mfe_without_ticks": true,
      "real_copy_execution_enabled": false
    }
  }
}
```

The checked-in `first_three.json` must extend this tape to **three** lifecycles (six deals minimum) so `early_score_eligible=true`. The snippet above is the **shape**, not the full gold.

---

## 14. Harness types ↔ fixture (A27)

| Class | Reads | Writes / asserts |
|---|---|---|
| `Mt5HistoricalEventFixture` | whole document | materialized `universe` + sorted `events` |
| `XauUsdFirstThreeTradeFixture` | `first_three.json` | same, plus asserts `completed_xau_count==3` |
| `Mt5EventReplayer` | `events` | ingest/outbox, dedupe |
| `DeterministicClock` | `clock` + delay + control events | `now` |
| `ReplayHarness` | `config` + replayer + reconstruct + features + scores + shadow | stage artifacts |
| `ReplaySnapshotAsserter` | `expect` | comparisons in §11 |
| `MarketDataIncrementalRefreshReplayer` | **not** this format | §61 FIX `.fix` tapes (separate). Replay fixtures use already-decoded `dest.quote` |

Do not overload this JSON with raw FIX SOH strings. §61 recorded ER/MD files stay under `tests/Fix/Fixtures`. A replay fixture may *reference* a quote tape that was **derived** from MD incremental, but the object the shadow engine sees is `dest.quote`.

---

## 15. Canonicalization (hash / catalog)

When computing `catalog.tsv` SHA-256 or `determinism_hash_sha256`:

1. Resolve `events_ref` into `events`.
2. Sort `events` by `(t_utc, seq)`.
3. Remove `notes` / `$comment`.
4. Normalize ISO times to `yyyy-MM-ddTHH:mm:ss.fffZ`.
5. Decimal strings: apply `decimal` parse and rewrite with trim of trailing zeros except at least one digit after a decimal if the value is non-integer (`"1.50"` stays; `"1.0"` → `"1.0"` is **not** required — hash the **parsed artifact object**, not the source file).
6. UTF-8 JSON, object keys sorted lexicographically, no insignificant whitespace, `\u` only when required.

`catalog.tsv` columns (proposed):

```text
fixture_id	path	schema_version	sha256	a27_tests
```

---

## 16. Closed string catalogs (v1)

| Field | Values |
|---|---|
| `$schema` | `ti.replay.fixture/v1` |
| `kind` | §8.2 |
| `direction` | `LONG`, `SHORT` |
| `action_class` | `OPEN_EXPOSURE`, `INCREASE_EXPOSURE`, `REDUCE_EXPOSURE`, `CLOSE_EXPOSURE` |
| `state` (trader) | `INSUFFICIENT_DATA`, `EARLY_SCORE`, `WATCH`, `SHADOW`, `LIVE_CANDIDATE`, `LIVE`, `PAUSED`, `RISK_BLOCKED`, `DISQUALIFIED` |
| `window` | `EXPANDING`, `FIRST3`, `PROVISIONAL` |
| `feature_quality` | `EXACT`, `APPROXIMATE`, `UNAVAILABLE` |
| `price_source` | A45 §5 (no `CTRADER_FIX_QUOTES` on source features) |
| `md_entry_source` | `SNAPSHOT`, `INCREMENTAL` |
| `fill_assumption` | `SINGLE_FILL_FULL_QTY` |
| `fill_quality` | `LIVE_QUOTE`, `STALE_QUOTE`, `UNPRICED` |
| `cost_quality` | `ASSUMED`, `MEASURED` |
| `shadow order status` | `PENDING_PERSISTED`, `SIM_SENT`, `PARTIAL`, `FILLED`, `REJECTED`, `EXPIRED`, `CANCELLED`, `UNPRICED_CLOSE_HELD` |
| `kill_switch.mode` | `NONE`, `STOP_NEW_EXECUTION`, `EMERGENCY_FLATTEN` |
| `scores.mode` | `qualitative`, `numeric`, `absent` |
| `reconstruction.match` | `completed_xau_only`, `all` |
| `fill_side_px` | `BID`, `ASK` |

Raw MT5 ints stay ints: `action` 0–20, `entry` 0–3.

---

## 17. Loader hard-fail checklist

Reject the fixture (do not run the pipeline) if any of:

1. `$schema` / `schema_version` major ≠ 1.  
2. Both or neither of `events` / `events_ref`.  
3. Duplicate `seq` or duplicate `source_event_id` **declared as distinct** without `kind` that is a control. (Duplicate *deals* are legal tape content and are dropped at ingest, not at load.)  
4. Trading `mt5.deal` missing `position`.  
5. `volume` present but not an integer, or `scale` not `10000` without an explicit ext label.  
6. `real_copy_execution_enabled: true`.  
7. `expect.scores.state` in `{LIVE, LIVE_CANDIDATE}` while `n <= 3`.  
8. `expect.features` has non-null MFE/MAE with `mfe_mae_used: true` but the tape has no `mt5.tick`/`mt5.bar`.  
9. `price_source=CTRADER_FIX_QUOTES` on a **feature** snapshot.  
10. `dest.quote` `instrument_id` not in `universe.destination_symbols`.  
11. `broker_code` on an event not in `universe.brokers`.  
12. ISO timestamp with non-`Z` offset.  
13. Unknown top-level key (except `notes`).  
14. `expect.shadow` present, `shadow_copy_enabled=true`, and zero `dest.quote` events while any expected order status is `FILLED`.

---

## 18. Known gaps in current product (do not paper over in fixtures)

| Gap | Evidence | Fixture consequence |
|---|---|---|
| C++ `DealData` JSON omits `position` | `mt5_types.h` `to_json` vs struct field | Fixtures **must** include `position`; live ingest will need the same field before replay-from-production-export works |
| `BaselineScorer` ≠ A22 | `1.25` vs `1.80` martingale; no sample CV; MFE always unavailable | Gold `numeric` scores wait for `DeterministicScorer`; use `qualitative` until then |
| `tests/Replay` missing | A27 §12, A30 I4 | This spec is the contract those files will satisfy |
| No `mt5_xau_ticks` / `destination_quotes` history table in production SQL yet | A20 / A17 | In-memory replay store is enough for the harness |
| Shadow cost model assumed | A24 `cost_quality=ASSUMED` | Fixtures must label it; never copy source commission into shadow expect |

---

## 19. Implementation notes (when someone builds the harness later)

Suggested C# DTOs (names only — **not created here**):

```text
ReplayFixtureDocument
ReplayClockSpec
ReplayUniverse
ReplayConfig
ReplayEvent
ReplayExpect
```

Serializer: `System.Text.Json` with `JsonPropertyName` snake_case, `JsonStringEnumConverter` for catalogs, custom `decimal` converter that **only** accepts JSON strings (reject numbers for money to catch `2650.1` binary drift). Native volumes: `ulong`.

`Mt5EventReplayer` maps `mt5.deal.body` → `NormalizedDeal`:

```text
BrokerId     ← universe broker id / code
Login        ← body.login
DealTicket   ← body.ticket
OrderTicket  ← body.order
PositionId   ← body.position
SourceSymbol ← body.symbol
Action       ← (DealAction)body.action
Entry        ← (DealEntry)body.entry
VolumeNative ← body.volume
Price, Profit, Commission, Swap=storage
Time         ← time_utc
StopLoss     ← price_sl
TakeProfit   ← price_tp
```

Then `TradeReconstructor.Reconstruct` is the reconstruction SUT. Do not reimplement lifecycle math inside the harness.

---

## 20. Acceptance of this format

The format is accepted when a future (not this) change-set can:

1. Add `tests/Replay` and deserialize `first_three.json` without custom per-test parsers.  
2. Feed the same file through reconstruct → features → scores → shadow.  
3. Assert A27 must-prove bullets for reconstruction, first-3, leakage, catch-up, and destination-priced shadow.  
4. Hash two runs identically with `DeterministicClock`.  
5. Refuse `REAL_COPY_EXECUTION_ENABLED` and source-priced fills.

Until those files exist, this document is the **only** binding fixture schema.

---

*End of A67. Product source was not modified.*
