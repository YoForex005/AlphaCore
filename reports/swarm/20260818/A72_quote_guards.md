# A72 — Configurable quote-age, spread, and price-move guards (Architecture §31, §37)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\A72_quote_guards.md` |
| Agent | A72 (quote / spread / price-move guards) |
| Date | 2026-08-18 |
| Status | **BINDING design** — specification only; **no product source modified** |
| Source of truth | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§31 Destination Quote Feed** (lines 1230–1262), **§37 Slippage / Price-Move Guard** (lines 1432–1452) |
| Supporting architecture | §16, §24, §30, §32, §36, §39, §41–§42, §44–§45, §52–§53, §57–§58, §60, §62 QUOTE, §63–§64, §68, §70.11, §72.17–18 |
| Sibling swarm (do not contradict) | A20 `destination_quotes`, A23 risk engine, A24 shadow copy, A25 FIX send gate, A26 dashboard, A27 tests, A28 phases, A43 sizing, A45 dest-tape isolation, A48 flatten skip of **entry** guards, A49 `MAX_QUOTE_AGE_MS` |
| Scope | Canonical **XAUUSD** only. Destination tape = Pepperstone / cServer FIX 4.4 **QUOTE** session. Guards apply to live copy **and** shadow. |
| Product source edited | **No** |

Go-live checkbox this document owns (`§68`):

```text
[ ] stale quote rejection works
```

That box stays **unchecked** until:

1. Thresholds are **configuration**, not compile-time constants treated as production law (§23, §31).
2. `quote_age > configured_max_quote_age` rejects **OPEN/INCREASE** with `QUOTE_STALE` (live send path **and** shadow).
3. Independent `SPREAD_TOO_WIDE` and `PRICE_MOVED_TOO_FAR` reject OPEN/INCREASE (§37).
4. QUOTE “connected / logged on” is **not** accepted as a substitute for measured `quote_age` (A25 §7.2).
5. A27 classes `Risk.QuoteFreshnessGuardTests` and `Risk.PriceMoveGuardTests` (plus the A23 hard-limit slice) **pass** on fixtures — not on the live Pepperstone account (§61).

---

## 0. Verdict (measured 2026-08-18)

Architecture §31 requires a destination quote feed **and** a configurable, measured stale-quote reject. Architecture §37 requires a pre-send deviation check that may emit `PRICE_MOVED_TOO_FAR`, `QUOTE_STALE`, `SPREAD_TOO_WIDE`. XAUUSD around news is the motivating case. There is **no v1 news calendar** — these three guards *are* the news control.

Current tree vs that law:

| Item | Path / evidence | Class |
|---|---|---|
| In-process OPEN/INCREASE checks for the three reason codes | `D:\Prop\src\Domain\Risk\RiskEngine.cs` (`QUOTE_STALE`, `SPREAD_TOO_WIDE`, `PRICE_MOVED_TOO_FAR`) | **EXISTS_NEEDS_REFACTOR** |
| `RiskLimits` with `MaxQuoteAge` / `MaxAllowedSpread` / `MaxPriceMove` / `MaxSlippage` | same file | **EXISTS_NEEDS_REFACTOR** — defaults are **hardcoded lab numbers**, not measured config |
| `RiskEngine.Evaluate` called from Application / API / workers | `grep Evaluate(` under `D:\Prop\src` → **only the definition** | **MISSING** (dead path) |
| `QuoteFreshnessGuard` / `PriceMoveGuard` types | A27 names; **no** `.cs` | **MISSING** |
| Dual clock (`quote_age` **and** `venue_quote_age`) | A23 §6.1; engine uses only `DecisionTime - Quote.ReceivedAt` | **MISSING** |
| Taker-touch signed adverse move | A23 §6.3 / A24 §7.3; engine uses **unsigned `|mid - expected|`** | **UNSAFE if shipped** |
| `MaxSlippage` evaluated | property exists; **never read** in `Evaluate` | **MISSING** |
| Quote usability (`bid<=0`, `ask<bid`, unmapped) | A24 §2.2; engine does not validate | **MISSING** |
| Destination quote cache + durable upsert | A20 `destination_quotes`; `DestinationQuoteSnapshot` exists; `TraderDbContext` references `DestinationQuotes` / `DestinationQuotesConfiguration` **which are not on disk** | **MISSING** (compile hole in Infrastructure) |
| FIX QUOTE MD → cache | `apps/fix-worker/Worker.cs` is a 1 s heartbeat stub; no MD handler | **MISSING** |
| `CTraderFixOptions.MaxQuoteAgeMs = 5000` | `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | **EXISTS_NEEDS_REFACTOR** — **not bound** to `RiskLimits.MaxQuoteAge` (3 s). Two silent defaults. |
| Config / env keys | no `MAX_QUOTE_AGE_*` / `MAX_SPREAD_*` / `MAX_ADVERSE_*` in any `appsettings*.json` | **MISSING** |
| Unit tests for these guards | `tests/Unit` has **no** `*.cs` test sources | **MISSING** |
| Dashboard live bid/ask/age/spread | DTOs have optional fields (`FixSessionDto.QuoteAgeSeconds`, web `quoteAge` / `spread`); no query implementation | **MISSING** |
| A29 E09 “quote cache + stale-quote reject” | still **no cache** | E09 remains **MISSING** |
| A29 E17 “Slippage / PRICE_MOVED / SPREAD” | logic now **exists in RiskEngine**, unused and mid-based | reclassify to **EXISTS_NEEDS_REFACTOR** (do not treat A29’s “absent” as current) |

**Do not claim §31 or §37 are implemented.** A method that nobody calls, with two disagreeing magic numbers and a mid-price move test, is not a go-live guard.

This file is the binding design for later implementation. It does not implement.

---

## 1. Architecture quotes (binding)

### 1.1 §31 Destination Quote Feed

Use the cTrader QUOTE session for:

```text
best available destination bid/ask
quote freshness
shadow pricing
slippage reference
pre-trade price checks
```

Maintain:

```text
latest quote
quote received timestamp
venue timestamp if available
symbol ID
bid
ask
```

Risk engine must reject stale quotes.

```text
if quote_age > configured_max_quote_age:
    reject new copy order
```

The threshold **must be configurable and measured**.

### 1.2 §37 Slippage / Price-Move Guard

Before sending, hold:

```text
expected destination price
current destination quote
source price
```

Calculate price deviation. Risk policy may reject:

```text
PRICE_MOVED_TOO_FAR
QUOTE_STALE
SPREAD_TOO_WIDE
```

Especially important for **XAUUSD around news**.

### 1.3 Adjacent law this file does not reopen

| Topic | Where | Implication for these guards |
|---|---|---|
| Hard limits list includes max allowed spread, max quote age, max tolerated price move, max slippage | §39 | These are first-class risk limits, not FIX-session niceties |
| QUOTE FIX unavailable → do not create new live copy that needs fresh pricing | §62 | Missing/unhealthy quote ≡ reject OPEN/INCREASE |
| Stale **entries** expire; do not catch up a 3-minute FIX gap | §36, §63, §72.17 | Re-evaluate against a **live** quote after reconnect; expired intents die |
| Reduce/close ≠ open/increase | §64, §72.18, A23, A24, A48 | Entry guards do **not** block known-position CLOSE; they may flag fill quality |
| QUOTE liveness ≠ Heartbeat | A25 §2.5 | Streaming quotes may omit HB; **quote age** is liveness |
| Quote session “connected” ≠ fresh | A25 §7.2 | Send gate must test `quote_age <= configured_max_quote_age` |
| Dest quotes are the **wrong** tape for source MFE/MAE | A45 §6.2 | These guards consume dest tape only; they never write `mfe`/`mae` |
| Flatten closes skip **entry** quote-age / price-move | A48 §3.2 | Still need TRADE + known dest ids |
| Re-check immediately before socket write | A25 §6.3, A49 §3.2 | Risk approval is not a capability token |

---

## 2. Why XAUUSD needs three independent guards

Gold prints through CPI / NFP / FOMC with:

- **stale books** (QUOTE socket up, last increment seconds old),
- **wide books** (fresh increment, `ask - bid` blows out),
- **gapped books** (fresh increment, tight spread, price already jumped vs the source fill / expected dest touch).

Any **one** control is insufficient:

| If we only had… | Failure mode on XAU |
|---|---|
| Quote age | A 50 ms post-NFP print is “fresh” and still 8 USD worse than the source fill |
| Spread | A locked 0.20 spread after a 4 USD gap still looks “tight” |
| Unsigned mid move | A favorable 2 USD improvement rejects; an adverse 2 USD ask jump vs a mid that barely moved can pass |
| Session connected | Heartbeat-less QUOTE with a 12 s last print is “connected” and unusable |

§37 therefore names **three** reject codes. They are evaluated independently. A fresh but gapped XAU print still fails the move guard (A23 §6.3). A tight but ancient book still fails quote age (§31). A fresh, unmoved but 3-USD-wide book still fails spread.

v1 does **not** special-case a news calendar. If someone later adds a calendar, it is an **additional** pause, never a replacement for these three.

---

## 3. Destination quote snapshot (input contract)

Authority: §31, A20 `destination_quotes`, A24 §2.2.

### 3.1 Fields the guards are allowed to see

| Field | Required | Clock / source | Notes |
|---|---|---|---|
| `venue_id` / `venue_code` | yes | dest | e.g. `PEPPERSTONE_CSERVER` |
| `canonical_symbol` | yes | map | must be `XAUUSD` (§16). Never raw FIX tag 55 text |
| `destination_symbol_id` | yes | Security List (§30) | integer cServer instrument id. **Never hardcoded** |
| `bid` | yes | QUOTE MD | best dest bid |
| `ask` | yes | QUOTE MD | best dest ask |
| `quote_received_at` | yes | **our** UTC when the FIX message was accepted | mandatory age clock |
| `venue_timestamp` | if present | venue-supplied | never invent |
| `fix_msg_seq_num` | yes | QUOTE | replay / audit |
| `md_entry_source` | yes | | `SNAPSHOT` \| `INCREMENTAL` |
| `spread` | derived | | `ask - bid` (destination **price units**) |
| `mid` | derived | | `(bid+ask)/2` — **display only**. Not a fill. Not the default move guard |

Latest-only upsert key: `(venue_id, instrument_id)` (A20). Tick **history** is a different table if ever added; do not overload this one.

### 3.2 Unusable snapshot → treat as no quote

Reject the snapshot **before** age/spread/move when any of:

```text
bid <= 0
ask <= 0
ask < bid
destination_symbol_id not mapped to canonical XAUUSD
```

Reason: `QUOTE_INVALID` (shadow A24) / `QUOTE_UNAVAILABLE` (live A23). Do not run arithmetic on a crossed book and emit `SPREAD_TOO_WIDE` as if the market were merely wide.

### 3.3 Two clocks

```text
quote_age       = decision_time - quote_received_at
venue_quote_age = decision_time - venue_timestamp     # only if venue_timestamp present AND sane
```

**Sane venue timestamp** (A24 §2.3):

- not in the future by more than `VENUE_TS_SKEW_MS` (config; measurement target, not a guess baked into this file),
- not older than `quote_received_at` by an absurd margin (`VENUE_TS_MAX_LAG_VS_RECEIVED_MS`).

Insane venue timestamps are **ignored** (do not invent age; do not fail open). They are logged.

**OPEN/INCREASE reject (A23 §6.1, conservative, binding here):** if a sane `venue_timestamp` exists, reject when **either** clock exceeds the active `max_quote_age_*`. A24’s “use receive clock unless config selects venue” is the **dashboard / fill-quality primary**; it does not waive the dual reject on live/shadow OPEN.

**Receive clock is always computed.** QUOTE “logged on” is not a clock.

### 3.4 Taker touch (never mid)

| Intent | Side | `current_quote` used by move / slippage |
|---|---|---|
| OPEN/INCREASE long | buy | `ask` |
| OPEN/INCREASE short | sell | `bid` |
| REDUCE/CLOSE long | sell | `bid` |
| REDUCE/CLOSE short | buy | `ask` |

Mid is for cards and optional telemetry (`price_deviation_mid`). Using mid as the guard is a **§37 defect**: it under-states taker cost on XAU.

---

## 4. Guard definitions

All numeric thresholds are **configuration**. This file does **not** publish production numbers (§23: do not hardcode before measurement). Lab values currently in `RiskLimits` / `CTraderFixOptions` are **not** law.

### 4.1 Quote-age guard (§31)

```text
if quote missing OR QUOTE session not logged on OR snapshot unusable:
    OPEN/INCREASE → REJECT  QUOTE_UNAVAILABLE   (alias QUOTE_FIX_UNAVAILABLE when the session is down)

if quote_age > max_quote_age_<class>:
    OPEN/INCREASE → REJECT  QUOTE_STALE

if venue_quote_age is defined AND venue_quote_age > max_quote_age_<class>:
    OPEN/INCREASE → REJECT  QUOTE_STALE
```

`<class>` is `open` for OPEN/INCREASE. REDUCE/CLOSE uses a **separate, larger** threshold and is **not** a blocker at the OPEN value (A24 §8, §64). See §5.

Equality: `quote_age == max` **passes** (`>` only), matching the architecture example and A23.

### 4.2 Spread guard (§37, §39)

```text
spread = ask - bid          # destination price units (XAUUSD USD/oz on cTrader)

if spread > max_allowed_spread_<class>:
    OPEN/INCREASE → REJECT  SPREAD_TOO_WIDE
```

Units are **price**, not “points” and not MT5 points, unless `destination_symbols.tick_size` is measured and config explicitly says `ticks`. Default unit: **absolute dest price**.

Spread is independent of age. A 40 ms quote with a 2.50 USD book still fails if `max_allowed_spread_open` is below 2.50.

### 4.3 Price-move guard (§37)

Hold three prices (architecture):

```text
expected_destination_price   # persisted on the CopyIntent / execution intent at create
current_destination_quote    # taker touch NOW
source_price                 # source fill / signal (informational + optional extra cap)
```

**Signed adverse** vs expected (A24 §7.3 — binding; replaces unsigned mid):

```text
# Open / increase long (we buy the ask)
adverse_vs_expected = dest_ask_now - expected_destination_price

# Open / increase short (we sell the bid)
adverse_vs_expected = expected_destination_price - dest_bid_now
```

```text
if adverse_vs_expected > max_tolerated_price_move_open:
    REJECT  PRICE_MOVED_TOO_FAR
```

**Favorable** move (`adverse_vs_expected <= 0`) does **not** reject. Unsigned `|mid - expected|` is forbidden as the production predicate.

Optional second cap (off unless enabled):

```text
adverse_vs_source_long  = dest_ask_now - source_price
adverse_vs_source_short = source_price - dest_bid_now

if ENABLE_ADVERSE_VS_SOURCE=true AND adverse_vs_source_* > max_adverse_vs_source_open:
    REJECT  PRICE_MOVED_TOO_FAR
```

`expected_destination_price` is typically the taker touch **at intent creation** (or last approved touch). It is **not** the source MT5 price unless mapping explicitly says so. Persist it on `copy_intents` / `execution_intents` so send-time re-check uses the same expected (A23 §4.2 already lists `expected_destination_price`).

### 4.4 Slippage cap (§37 / §39 — sibling of move, not a substitute)

```text
expected_slippage = adverse_vs_source_*   # taker dest vs source, same sign as §4.3
# or, if cost model exists: modeled_dest_cost + adverse_vs_expected

if expected_slippage > max_slippage:
    OPEN/INCREASE → REJECT  MAX_SLIPPAGE_EXCEEDED
```

`MaxSlippage` on today’s `RiskLimits` is a stub until this predicate exists. Price-move answers “did dest move vs what we expected?” Slippage answers “is the dest touch too far from the **source** fill / modeled cost?” Both can fire on the same tick; first blocking code is `primary_reason` (A23 §4.3).

### 4.5 What these guards do **not** do

- They do not size the order (A43).
- They do not replace `expires_at` / `max_signal_age` (§36 — **signal** age ≠ **quote** age).
- They do not mean QUOTE TCP is down (that is `QUOTE_UNAVAILABLE` / venue health, evaluated **before** these guards — A23 §5 steps 5 then 9).
- They do not invent a dest price when the book is empty.
- They do not read source MT5 ticks as dest quotes (A45).

---

## 5. Exposure-class policy

Authority: §64, A23 §2, A24 §7–§8, A48.

| Class | Quote age | Spread | Price-move | Slippage |
|---|---|---|---|---|
| `OPEN_EXPOSURE` | block at `max_quote_age_open` | block at `max_spread_open` | block at `max_adverse_move_open` | block at `max_slippage` |
| `INCREASE_EXPOSURE` | same as OPEN | same | same | same |
| `REDUCE_EXPOSURE` | **not** a blocker at OPEN threshold | record only | record only | record only |
| `CLOSE_EXPOSURE` (copy / shadow) | see waterfall | record only | record only | record only |
| `EMERGENCY_FLATTEN` close | skip **entry** age/move (A48) | skip as blocker | skip as blocker | skip as blocker |

### 5.1 CLOSE / REDUCE pricing waterfall (not a reject of the close)

A24 §8.3, applied to **shadow** always and to **live mark / last-look** when a close needs a price:

1. QUOTE up and `quote_age <= max_quote_age_close` → use current taker touch. Quality `LIVE`.
2. Else last usable quote with `quote_age <= max_quote_age_close_stale_fallback` → use it. Quality `STALE_QUOTE`. Persist age.
3. Else **do not invent a price**. Live: still **attempt** the close if dest position id is known (risk-reduction; market order / venue last). Shadow: `SOURCE_CLOSED_UNPRICED`, `unrealized=null` (not zero).

Invariant:

```text
max_quote_age_open  <  max_quote_age_close  <  max_quote_age_close_stale_fallback
```

`max_quote_age_mark` (dashboard mark) is independent; exceeding it sets `mark_quality=STALE_QUOTE` and still publishes the last mark **with age visible** (A24 §761). Never display `0` P&L because the quote is old.

### 5.2 Live send gate still needs a fresh quote for priced OPEN

A25 §6.3 conjunction (repeat immediately before `35=D`):

```text
QUOTE usable if the order requires a fresh price
  (CTRADER_FIX_QUOTE_ENABLED
   AND quote_age <= configured_max_quote_age
   AND instrument mapped)
```

`REAL_COPY_EXECUTION_ENABLED=true` with a missing or stale quote is still `QUOTE_STALE` / venue unhealthy. Flatten / known CLOSE may send without passing OPEN age (A48), but must not send if TRADE is down or dest id is unknown.

---

## 6. When the guards run

Fail closed on the first blocking check. A23 §5 order is binding for the engine; this file owns **step 9** and the send-time repeat.

```text
… venue health, source health, trader, expires_at / signal age …
9. Quote usability → quote age → spread → price-move → slippage
10. Sizing (A43)
…
persist risk_decision
```

**Three evaluation moments** (all must see a snapshot, not “whatever is on the dashboard now”):

| Moment | Who | OPEN/INCREASE | CLOSE/REDUCE |
|---|---|---|---|
| CopyIntent risk evaluate | `RiskEngine` | block | do not apply OPEN blockers |
| After shadow simulated delay | `ShadowCopyEngine` (A24 §7.4) | **re-read** quote; repeat §4; no fill on fail | waterfall |
| Immediately before FIX write | fix-worker send gate (A25/A49) | repeat age + usability at minimum; prefer full §4 | flatten policy |

A quote that was fresh at decision and stale at send → `QUOTE_STALE`, **no** `NewOrderSingle`. Do not use the decision-time book as a fallback for OPEN.

After a QUOTE outage: do **not** drain an OPEN backlog (§63). Re-evaluate only still-unexpired intents against a **new** live quote.

---

## 7. Configuration (measured; no silent hardcodes)

Authority: §23, §31, A24 §17, A25 §6.6, A26 `controls.limits`, A49.

### 7.1 Keys

Bind **one** options object (suggested name `QuoteGuardOptions` / `RiskLimits` document) from env + `appsettings` + audited `PATCH /api/v1/risk/limits` (A26). Config is the **floor** for enabling; runtime may tighten, never silently loosen past a SuperAdmin/RiskManager audited change.

```env
# Clocks
MAX_QUOTE_AGE_MS=                      # send-gate alias; if set alone, copies to OPEN
MAX_QUOTE_AGE_OPEN_MS=
MAX_QUOTE_AGE_CLOSE_MS=
MAX_QUOTE_AGE_CLOSE_STALE_FALLBACK_MS=
MAX_QUOTE_AGE_MARK_MS=
VENUE_TS_SKEW_MS=
VENUE_TS_MAX_LAG_VS_RECEIVED_MS=
QUOTE_AGE_REQUIRE_VENUE_WHEN_PRESENT=true   # A23 dual-clock; do not default false

# Spread (destination price units unless UNIT=ticks)
MAX_SPREAD_OPEN=
MAX_SPREAD_UNIT=price                  # price | ticks  (ticks only after tick_size measured)

# Price-move / slippage (same unit family as spread)
MAX_ADVERSE_MOVE_OPEN=
MAX_ADVERSE_VS_SOURCE_OPEN=
ENABLE_ADVERSE_VS_SOURCE=false
MAX_SLIPPAGE=

# Signal age is NOT a quote guard (listed so nobody overloads MAX_QUOTE_AGE)
MAX_SIGNAL_AGE_OPEN_MS=
MAX_SIGNAL_AGE_CLOSE_MS=
```

Dashboard PATCH (A26 example today) exposes `maxAllowedSpread`, `maxQuoteAgeMs`, `maxSourceSignalAgeMs`, `maxSlippage` and **omits** `maxToleratedPriceMove`. That is a **spec hole in A26**: add `maxAdverseMoveOpen` / `maxQuoteAgeCloseMs` / `maxSpreadOpen` when the PATCH is implemented. Until then, implementers must not assume A26’s JSON is the full limit set.

### 7.2 Measurement law (how to pick numbers — not the numbers)

Do **not** ship `RiskLimits` defaults (`MaxQuoteAge = 3s`, `MaxAllowedSpread = 2.0`, `MaxPriceMove = 3.0`, `MaxSlippage = 1.5`) or `CTraderFixOptions.MaxQuoteAgeMs = 5000` as production.

Required before first useful QUOTE phase exit (A30 increment 7 / Phase 4) and again before §68:

1. Record dest XAU bid/ask/`quote_age` for quiet session, London/NY overlap, and at least one high-impact print window.
2. Plot `p50/p95/p99` of `quote_age_ms` while QUOTE is logged on. `max_quote_age_open` sits **above** healthy p99 and **below** the age at which XAU edge is already gone. If those two bounds do not exist, the threshold is not “measured.”
3. Plot dest spread distribution the same way. `max_spread_open` is a **tail cut**, not the mean.
4. Plot `|taker_now - expected_at_intent|` and signed adverse during news. `max_adverse_move_open` cuts the gap tail, not ordinary 1-tick noise.
5. Persist the measurement note (window, venue, instrument id, SHA of the sample) next to the config change in `audit_logs`.
6. Shadow promotion gates (A24) should be able to slice P&L by `fill_quality` and `quote_age_ms`. If we cannot slice, we cannot claim the threshold is measured.

A26’s **example** payload (`maxAllowedSpread: 0.80`, `maxQuoteAgeMs: 1500`, `maxSlippage: 0.50`) is illustration only. It is not a default to copy into `RiskLimits`.

### 7.3 Single source of truth

Today `CTraderFixOptions.MaxQuoteAgeMs` (5000) and `RiskLimits.MaxQuoteAge` (3 s) **disagree**. Binding rule:

```text
one configured duration → RiskEngine + send gate + dashboard + shadow
```

The FIX options type may **read** the same `IOptions<QuoteGuardOptions>` (or a mapped field). It must not own a second default.

---

## 8. Persistence, telemetry, dashboard

### 8.1 `destination_quotes` (A20)

Latest upsert. Carry at least: `venue_id`, `instrument_id`, `canonical_symbol`, `bid`, `ask`, `quote_received_at`, `venue_timestamp` (nullable), derived `spread`, computed `quote_age` (generated column **or** computed at read — do not persist a stale generated age without `now()`).

Every `risk_decision`, shadow fill, and live fill that used a book stores either a FK to `destination_quotes.id` **or** an embedded snapshot (`bid`, `ask`, `quote_received_at`, `quote_age_ms`, `spread`). Later P&L must not reprice from “whatever the latest quote is now” (A24 §2.5).

### 8.2 `risk_decisions` telemetry (A23 §4.4)

Every evaluation emits:

```text
quote_age
venue_quote_age          # null if unused
spread
price_deviation          # signed adverse vs expected
adverse_vs_source        # signed
primary_reason           # QUOTE_STALE | SPREAD_TOO_WIDE | PRICE_MOVED_TOO_FAR | …
```

Plus §57 identifiers. Metrics: `risk_rejections_total{reason=...}`.

### 8.3 Dashboard

| Surface | Architecture | Must show |
|---|---|---|
| QUOTE card | §52 | XAU mapped?, instrument id, bid, ask, **quote age**, **spread** |
| Risk | §53 | rejected intents + reason codes including the three §37 codes |
| Shadow position | A26 §6.9 | `quoteAgeMs` on the mark |
| Limits | A26 `controls.limits` | current configured max age / spread / adverse move |

Never show the FIX password. Quote age is **milliseconds** on the wire (`quoteAgeMs`); UI may format as `140 ms` / `1.2 s`. Do not report age as “connected.”

`FixSessionDto.QuoteAgeSeconds` (`DashboardModels.cs`) vs A26 `quoteAgeMs` is a unit trap. Binding: **milliseconds** on the API. Convert at the edge.

---

## 9. Reason codes (closed set for these guards)

| Code | Class | Predicate |
|---|---|---|
| `QUOTE_UNAVAILABLE` | OPEN/INCREASE | no snapshot, QUOTE down, or unusable book |
| `QUOTE_FIX_UNAVAILABLE` | OPEN/INCREASE | session-level (§62); may be the `primary_reason` instead of `QUOTE_UNAVAILABLE` when the socket is down |
| `QUOTE_INVALID` | OPEN/INCREASE | `bid<=0` / `ask<=0` / `ask<bid` / unmapped |
| `QUOTE_STALE` | OPEN/INCREASE | `quote_age` or sane `venue_quote_age` `>` configured max for that class |
| `SPREAD_TOO_WIDE` | OPEN/INCREASE | `ask-bid > max_spread_*` |
| `PRICE_MOVED_TOO_FAR` | OPEN/INCREASE | signed adverse vs expected (and optional vs source) |
| `MAX_SLIPPAGE_EXCEEDED` | OPEN/INCREASE | expected slippage `>` `max_slippage` |

Do not reuse `SIGNAL_STALE` / `INTENT_EXPIRED` for quote age. Do not reuse `VENUE_UNHEALTHY` when the session is up and the book is merely old — that is `QUOTE_STALE`.

---

## 10. Current code vs this law (file-level)

### 10.1 `RiskEngine` — partial, unused, wrong move predicate

`D:\Prop\src\Domain\Risk\RiskEngine.cs`:

- Defaults: `MaxAllowedSpread = 2.0m`, `MaxQuoteAge = 3s`, `MaxPriceMove = 3.0m`, `MaxSlippage = 1.5m`.
- Missing quote on OPEN/INCREASE → `QUOTE_MISSING` (should be `QUOTE_UNAVAILABLE` to match A23).
- Age: `DecisionTime - Quote.ReceivedAt > MaxQuoteAge` → `QUOTE_STALE` (receive clock only; equality passes — good).
- Spread: `Ask - Bid > MaxAllowedSpread` → `SPREAD_TOO_WIDE` (no validity check; no unit).
- Move: `|mid - ExpectedPrice| > MaxPriceMove` → `PRICE_MOVED_TOO_FAR` (**unsigned mid** — forbidden by §4.3).
- `MaxSlippage` unused.
- CLOSE/REDUCE skip these via `IsIncreasing` — correct **direction**, but no close waterfall / fill-quality flags.
- Evaluation order: kill/reconcile/venue **then** quote **then** signal age. A23 wants signal/expiry **before** quote. Reorder when the engine is wired; tests must lock the A23 order.
- Nobody calls `Evaluate`.

### 10.2 Quote types — three names, no store

| Type | Path | Role |
|---|---|---|
| `DestinationQuote` record | `Domain\Risk\RiskEngine.cs` | in-memory request DTO (`Bid/Ask/ReceivedAt/VenueTimestamp`) |
| `DestinationQuoteSnapshot` | `Domain\Entities\DestinationQuote.cs` | entity-shaped snapshot; **no** `venue_id`, seq, md source, uniqueness |
| `DestinationQuotes` + `DestinationQuotesConfiguration` | referenced by `TraderDbContext` | **files do not exist** |

`ShadowCopyEngine` records `QuoteAge` and `Spread` on `ShadowFill` but **never rejects**. `ShadowOrder` entity persists `Spread` and slippage, **not** `QuoteAge`.

### 10.3 FIX / hosts

- `CTraderFixOptions.MaxQuoteAgeMs = 5000` — unused by risk, unused by `Worker`.
- `FixSimulationHarness.SimulateMarketDataSnapshot` exists for tests; no production MD path.
- `apps/api/appsettings.json` and `apps/fix-worker/appsettings.json` have **no** guard keys.

### 10.4 Tests

A27 required:

| Class | Must prove |
|---|---|
| `Risk.QuoteFreshnessGuardTests` | `quote_age > max` rejects new copy; threshold **injected**, not baked |
| `Risk.PriceMoveGuardTests` | `PRICE_MOVED_TOO_FAR`, `QUOTE_STALE`, `SPREAD_TOO_WIDE` |
| `Risk.RiskEngineHardLimitTests` | includes spread, quote age, price move, slippage |
| `Risk.OpenVsCloseExposurePolicyTests` | OPEN stricter than CLOSE |

None of these files exist under `tests/`. §68 “stale quote rejection works” is **false**.

---

## 11. Required tests (when implemented — not in this change)

Fixtures use **recorded** XAU-like books (e.g. bid `2399.50` / ask `2399.66`). No live account (§61).

**Quote freshness**

1. `quote_age == max_quote_age_open` → approve (if other limits pass).
2. `quote_age == max + 1ms` → `QUOTE_STALE`, `AllowFixSend=false`, zero FIX outbound.
3. Change only config `max_quote_age_open`; same snapshot flips pass↔fail.
4. QUOTE logged on + 30 s last increment → `QUOTE_STALE`, not “healthy.”
5. Missing quote on OPEN → `QUOTE_UNAVAILABLE`.
6. Missing quote on CLOSE of a **known** dest/shadow position → does **not** emit `QUOTE_STALE` as a blocker.
7. Sane `venue_timestamp` older than max, receive clock fresh → `QUOTE_STALE` (dual clock).
8. Insane future `venue_timestamp` → ignored; receive clock decides.

**Spread**

9. `ask - bid == max_spread_open` → pass; `+ tick` → `SPREAD_TOO_WIDE`.
10. Crossed book `ask < bid` → `QUOTE_INVALID`, not `SPREAD_TOO_WIDE`.
11. `bid <= 0` → `QUOTE_INVALID`.

**Price-move / slippage**

12. Open long: ask jumps `max_adverse + epsilon` vs expected → `PRICE_MOVED_TOO_FAR`.
13. Open long: ask **improves** by the same amount → **approve** (favorable).
14. Open short: bid drops adversely → reject; bid rises → approve.
15. Mid moves, taker touch does not → **must not** reject if the engine still used mid (this test **fails today’s** `RiskEngine`).
16. Fresh quote, tight spread, large adverse gap → `PRICE_MOVED_TOO_FAR` only.
17. `expected_slippage > max_slippage` → `MAX_SLIPPAGE_EXCEEDED`.
18. Flatten / `CLOSE_EXPOSURE` with a gapped book → no `PRICE_MOVED_TOO_FAR` block.

**Pipeline**

19. Decision-time pass, send-time age fail → no `35=D`.
20. Shadow post-delay re-check fail → no `shadow_copy_fill`.
21. 3-minute QUOTE gap, 20 OPEN intents → no catch-up of stale entries (§63).
22. High ML score cannot bypass a stale quote (§39, A27 `ScoringCannotBypassRiskTests`).

---

## 12. Implementation sequence (later; not this artifact)

Align with A30 Phase 4 (QUOTE) then Phase 5 (shadow) then risk wiring. Suggested order:

1. Durable `destination_quotes` + in-memory cache updated only from QUOTE MD (Security List map first — §30).
2. Single `QuoteGuardOptions` bound from env; delete the extra `MaxQuoteAgeMs` default or map it to the same object.
3. Extract or keep predicates next to `RiskEngine` (`QuoteFreshnessGuard`, `SpreadGuard`, `PriceMoveGuard`) so A27 names have SUTs. Prefer **pure functions** over a second policy object that can drift.
4. Wire `Evaluate` from the copy-intent worker; persist `risk_decisions` with telemetry §8.2.
5. Send-gate repeat (A25) and shadow re-check (A24).
6. Dashboard QUOTE card + reject reasons + limit PATCH including **adverse move**.
7. Measure (§7.2); only then fill production numbers. Keep `REAL_COPY_EXECUTION_ENABLED=false`.

Do not implement a news calendar, a second dest venue, or source-tick “freshness” as a substitute.

---

## 13. Explicit non-goals (this document)

- No product source changes.
- No production numeric defaults.
- No Kafka quote bus, no tick warehouse, no news API.
- No using cTrader quotes as source MFE/MAE (A45).
- No treating Heartbeat absence on QUOTE as dead if increments are fresh (A25) — **and** no treating Heartbeat presence as fresh if increments are old.
- No writing `.mq5` / EX5 / anything outside this artifact.

---

## 14. Traceability

| Topic | Architecture / swarm |
|---|---|
| Dest quote feed, maintain bid/ask/timestamps, configurable stale reject | **§31** |
| Pre-send deviation; `PRICE_MOVED_TOO_FAR` / `QUOTE_STALE` / `SPREAD_TOO_WIDE`; XAU news | **§37** |
| Limits list | §39 |
| QUOTE unavailable | §62 |
| No stale catch-up | §36, §63, §72.17 |
| OPEN vs CLOSE | §64, §72.18 |
| Go-live “stale quote rejection works” | §68 |
| Risk rejection before FIX | §70.11 |
| `destination_quotes` | A20 |
| Engine predicates / dual clock / unsigned-move ban (via taker) | A23 §6.1–6.3 |
| Shadow OPEN vs CLOSE thresholds; signed adverse; re-check | A24 §2, §7–§8, §17 |
| Send-gate `quote_age`; HB ≠ liveness | A25 |
| Cards + PATCH limits | A26 §52/§53 |
| Test class names | A27 |
| Dest tape ≠ source features | A45 |
| Flatten skips entry guards | A48 |
| `MAX_QUOTE_AGE_MS` flag family | A49 |

---

## 15. One-line law

```text
QUOTE connected ≠ quote fresh.
Fresh ≠ tight.
Tight ≠ unmoved.

if quote_age > configured_max_quote_age:           reject OPEN/INCREASE   QUOTE_STALE
if ask - bid > configured_max_spread:              reject OPEN/INCREASE   SPREAD_TOO_WIDE
if signed_adverse(taker, expected) > configured:   reject OPEN/INCREASE   PRICE_MOVED_TOO_FAR
```

Thresholds are **configured and measured** for XAUUSD. Mid is not a guard. Heartbeat is not a guard. A hardcoded `3` or `5000` in two different files is not a guard.
