# A87 — Do not call the cTrader account an LP

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\A87_not_an_lp.md` |
| Date | 2026-08-18 |
| Agent | A87 |
| Product source modified | **None** |
| Scope | Architecture naming law + full-tree scan for `LP` / `LiquidityProvider` applied to the Pepperstone/cTrader destination account |
| Binding source | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §1.6 item 6, §25, §44–45 |

---

## 0. Verdict

**The Pepperstone cTrader / cServer FIX account is an external execution venue. It is not an LP.**

Product source does **not** currently name that account `LP`, `LiquidityProvider`, or any camel/snake variant. Vendor MetaTrader 5 SDK Ultency “liquidity provider” types are **unrelated** and must not be copied onto the destination account.

When the destination entity/table is added, the name is **`ExecutionVenue` / `execution_venues`**. Never `Lp`, `LiquidityProvider`, or `lp_account`.

---

## 1. Binding architecture law

### 1.1 §1.6 item 6 (quoted)

> **Do not call the cTrader account an LP unless it actually is your contractual LP relationship.**
> Technically this architecture treats Pepperstone/cServer FIX as the **external execution venue**. The software must not assume institutional LP semantics that the account does not provide.

This is the only architecture occurrence of “LP” applied to cTrader. It is a **prohibition**, not a type name.

### 1.2 §25 title (quoted)

> **# 25. New Execution Venue: cTrader / cServer FIX 4.4**
>
> Real approved copy trades will route to the provided Pepperstone cTrader account through cServer FIX 4.4.

The destination is a **FIX 4.4 execution venue** (QUOTE + TRADE sessions to `live-us-eqx-01.p.c-trader.com`, account `1369850`). It is a retail/prop broker FIX gateway, not a prime-of-prime or contractual liquidity provider.

### 1.3 §44–45 table name

Architecture lists the destination identity table as:

```text
execution_venues
```

Not `lps`, `liquidity_providers`, or `lp_accounts`.

Related destination tables use the same vocabulary:

| Table | Role |
|---|---|
| `execution_venues` | Destination identity (`venue_id`, `venue_code`) |
| `destination_symbols` | Venue-native instrument map |
| `destination_quotes` | Latest quote per venue instrument |
| `destination_positions` | Venue-side open book |
| `fix_sessions` / `fix_session_events` | QUOTE + TRADE session state |
| `fix_orders` / `fix_execution_reports` | Venue order / fill evidence |

Source MT5 brokers stay in `brokers` / `mt5_*`. **Do not reuse `broker_id` as the venue primary key.** A20: destination identity is `(venue_id, venue-native id)`.

### 1.4 Why “LP” is the wrong word here

| Institutional LP | This account |
|---|---|
| Contractual wholesale liquidity (prime, ECN aggregator, Ultency-style LP) | Pepperstone cTrader login `1369850` over cServer FIX |
| Streaming LP quotes, LP fill semantics, last-look, LP credit lines | Two FIX 4.4 sessions (QUOTE + TRADE) to a broker gateway |
| Software may assume LP book, LP symbol list, LP performance tags | Software must assume **broker execution venue** semantics only |
| MetaQuotes Ultency `LiquidityProvider` APIs | **Do not map** those APIs onto this destination |

There is **no evidence in this repo** of a contractual LP relationship with Pepperstone/cServer. Until a signed LP contract exists and is recorded, the name stays **execution venue**.

---

## 2. Scan method

Searched 2026-08-18 (case-insensitive and token forms):

`\bLP\b`, `LiquidityProvider`, `liquidity provider`, `IsLp`, `is_lp`, `LpAccount`, `lp_account`, `LpVenue`, `LpId`

Trees:

| Tree | Result for cTrader-as-LP |
|---|---|
| `D:\Prop\src` (Domain, Application, Infrastructure, Fix.CTrader, Mt5) | **Zero hits** |
| `D:\Prop\apps` (api, fix-worker, mt5-worker, web) | **Zero hits** |
| `D:\Prop\tests` | **Zero hits** |
| `D:\Prop\docs` | **Zero hits** |
| `D:\Prop\mt5-sdk\src` (owned C++ wrapper) | **Zero hits** |
| `D:\Prop\services` | **Zero hits** |
| `D:\Prop\.env.example` | Comment **forbids** LP: `# cTrader FIX execution venue (not an LP)` |
| Architecture v2 | §1.6 prohibition only |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK` | Ultency / Win32 `lp` only — see §4 |

Also searched product C# for `class ExecutionVenue` / `record ExecutionVenue` / `interface IExecutionVenue`: **no type exists yet**.

---

## 3. What product source actually says

### 3.1 No LP type, field, table, flag, or comment

Product C# / TS / JSON under `src`, `apps`, `tests` contains **no** identifier that calls the destination an LP.

`TraderDbContext` maps destination-adjacent tables as `destination_quotes`, `fix_sessions`, `execution_intents`. It does **not** map `execution_venues` and does **not** map any `lps` table.

### 3.2 Existing vocabulary is already “venue” / “destination”

| Location | Token | Notes |
|---|---|---|
| `D:\Prop\src\Domain\Entities\DestinationQuote.cs` | `VenueInstrumentId`, `VenueTimestamp` | Snapshot only; no venue FK |
| `D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs` | `TryMapVenueInstrumentId`, `RegisterVenueInstrument` | Venue instrument id → canonical |
| `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs` | `VenueOrderId` | Destination broker order id |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | `VenueHealthy`, `VENUE_UNHEALTHY` | Gate, not LP health |
| `D:\Prop\src\Domain\Enums\RiskDecisionOutcome.cs` | `PauseVenue = 4` | Correct verb |
| `D:\Prop\src\Domain\Entities\ExecutionIntent.cs` | `DestinationAccount` | String account, **no** `VenueId` yet |
| `D:\Prop\src\Domain\Brokers\BrokerCodes.cs` | `ACHIEVER`, `STARWAVEFX` | Source MT5 only |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | Host / AccountId / QUOTE / TRADE | Named cTrader FIX gateway, not LP |
| `D:\Prop\apps\web\src\types\index.ts` | `FixSession`, `realPnl` | No LP label |

`Broker` / `IBrokerConnector` are **source MT5** types. Do not extend them to mean “the cTrader LP”.

### 3.3 Naming gap (not an LP violation)

`ExecutionIntent.BrokerId` is a source-broker foreign key. There is still no `ExecutionVenue` entity and no `venue_id` on intents/orders/quotes. That is a **missing venue model** (A01 / A20 / A29), not a mislabel. When those types are added:

| Do | Do not |
|---|---|
| `ExecutionVenue` | `LiquidityProvider`, `Lp`, `LpAccount` |
| `execution_venues` | `lps`, `liquidity_providers` |
| `venue_id`, `venue_code` (e.g. `PEPPERSTONE_CSERVER`) | `lp_id`, `lp_code` |
| `DestinationAccount` / `fix_account_id` | `lp_login` |
| UI / logs: “execution venue”, “destination”, “cTrader FIX” | “LP”, “liquidity provider”, “our LP” |

### 3.4 Env comment already correct

`D:\Prop\.env.example` line 47:

```text
# cTrader FIX execution venue (not an LP)
```

Keep that comment. Do not “simplify” it to `CTRADER_LP_*`.

---

## 4. Hits that are **not** the cTrader account

### 4.1 Vendor MetaTrader 5 Manager SDK (Ultency)

`D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\` contains MetaQuotes **Ultency** liquidity-provider APIs, for example:

- `MT5APIUltLiquidity.h` — “Liquidity Provider configuration”
- `MT5APIUltDeal.h` — `LiquidityProvider()` / `LiquidityProviderID()`
- `MT5APIDataset.h` — `FIELD_ULTENCY_DEAL_LIQUIDITY_PROVIDER`, `FIELD_ULTENCY_DEAL_LP_PERFOMANCE_TIME`
- `MT5APIReport.h` / `MT5APIConfigParam.h` — `TYPE_ULTENCY_PROVIDER`

These describe an **MT5 server-side Ultency LP**, not Pepperstone cTrader account `1369850`. Owned wrapper code under `mt5-sdk\src` does not reference them. Do not import Ultency LP names into Domain/FIX.

### 4.2 Vendor noise

| Hit | Meaning |
|---|---|
| `DealerExampleDlg` `LPARAM lp` | Win32 message parameter |
| Gateway `TextFeeder` HTML `class="lp"` | News markup |

Ignore.

### 4.3 Prior swarm reports (already aligned)

These reports already state the same law. They are documentation, not product source:

| Report | Statement |
|---|---|
| A18 | “Do not assume cTrader is an LP.” |
| A20 | “Pepperstone / cServer account is an **external execution venue**, not an LP unless contractually true (§1.6).” |
| A25 | “Pepperstone / cServer FIX 4.4 is the **external execution venue**, not an LP.” |
| A01 | Missing type is `ExecutionVenue`, table `execution_venues` |

---

## 5. Semantics the software must not invent

Because this is **not** an LP, product code must not assume:

1. **LP credit / last-look / internalization.** Fills are broker execution reports (`8` ExecutionReport), not LP book matches.
2. **LP symbol catalog.** Discover cTrader instrument ids from Security List / credentials; do not treat Ultency LP symbol lists as the destination map.
3. **Source broker = destination venue.** Achiever and StarwaveFX are MT5 **sources**. Pepperstone/cServer is the **execution venue**.
4. **One multiplexed “LP session”.** Official model is two independent FIX sessions (QUOTE + TRADE).
5. **UI copy** such as “LP health”, “LP fill”, “send to LP”. Use “venue health”, “destination fill”, “send to execution venue”.

Allowed factual phrases: *Pepperstone cTrader account*, *cServer FIX gateway*, *execution venue*, *destination*.

---

## 6. Required names (when implemented)

No product source is changed by this report. Later waves must use:

```text
src/Domain/Entities/ExecutionVenue.cs          (or Domain/Venues/ExecutionVenue.cs)
table: execution_venues
code:  PEPPERSTONE_CSERVER
role:  ExternalExecutionVenue
```

Display string: **“Pepperstone cTrader (execution venue)”**.

Forbidden identifiers in product source, API JSON, dashboard copy, metrics, and logs:

```text
Lp, LP, LiquidityProvider, liquidity_provider, lp_id, LpAccount, OurLp
```

Exception: quoting this prohibition, or documenting that a **future signed** LP contract would be required before those names are legal.

---

## 7. Honesty pin

- Measured: **zero** product-source uses of LP for the cTrader account.
- Measured: **no** `ExecutionVenue` type and **no** `execution_venues` EF mapping yet.
- Vendor Ultency LP APIs exist under the MT5 SDK and are out of scope for destination naming.
- This file is a naming/architecture pin only. It does not implement the venue table.

**PASS** on current product naming (absence of the forbidden word). **OPEN** until `ExecutionVenue` exists and is wired with `venue_id` instead of overloading `Broker`.
