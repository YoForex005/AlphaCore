# MT5 Trader Intelligence + cTrader FIX 4.4 Execution Platform
## Unbiased Architecture Review + Senior Engineer Implementation Prompt

**Version:** 2.0  
**Primary use case:** Identify high-quality XAUUSD traders from ~5,000+ MT5 accounts, shadow-copy them, and route approved real trades to a cTrader/cServer FIX 4.4 execution account.  
**AI/LLM dependency:** None.  
**Execution default:** Disabled until shadow/reconciliation/risk controls are proven.

---

# 1. Executive Verdict

The overall direction is **correct**, but the earlier plan should be simplified and separated into stronger boundaries.

The correct system is **not**:

```text
MT5 Manager API
    ↓
ML
    ↓
cTrader FIX
```

The correct system is:

```text
MT5 SOURCE BROKERS
(Achiever + StarwaveFX + future brokers)
        ↓
MT5 Manager API collectors
        ↓
Raw immutable trading data
        ↓
Trade reconstruction
        ↓
XAUUSD feature/scoring engine
        ↓
Rules + statistics + ML ranking
        ↓
Shadow copy
        ↓
Copy intent
        ↓
Deterministic risk engine
        ↓
Execution intent
        ↓
cTrader FIX 4.4 adapter
        ↓
Pepperstone / cServer account
        ↓
Execution reports + reconciliation
```

## Unbiased assessment

### What is strong in the original plan

1. MT5 Manager API is the correct source-side integration.
2. Multi-broker normalization is necessary.
3. PostgreSQL is sufficient for the initial scale.
4. C#/.NET is a good fit for MT5/FIX/risk/execution.
5. Python/XGBoost is a good fit for the scoring model.
6. React is appropriate for the operational dashboard.
7. Shadow-copying before live deployment is essential.
8. A hard risk engine must sit between scoring and execution.
9. Dynamic MT5 group discovery is correct.
10. XAUUSD-first is sensible because most of the available trading data is concentrated there.

### What should be changed

1. **Do not attempt to build everything at once.**
   First prove data accuracy, then reconstruction, then shadow copying, then scoring, then controlled live execution.

2. **Three trades are not enough to declare skill.**
   Trade #3 should trigger an *early probability/ranking score*, not permanent classification.

3. **Do not use ML first.**
   Build a deterministic statistical baseline first. ML should have to beat that baseline out-of-sample.

4. **Do not send a trader to real money immediately after trade #3.**
   The default action after a strong early score should be SHADOW. Live execution should require additional evidence.

5. **Do not calculate MFE/MAE from closed deals alone.**
   Exact MFE/MAE requires price/tick observations while a position is open. If source-side tick data is not available, do not fabricate these features.

6. **Do not call the cTrader account an LP unless it actually is your contractual LP relationship.**
   Technically this architecture treats Pepperstone/cServer FIX as the **external execution venue**. The software must not assume institutional LP semantics that the account does not provide.

7. **Use two independent cTrader FIX sessions.**
   QUOTE and TRADE are separate connections and must have independent connection/session state.

8. **Do not write a FIX engine from raw TcpClient unless absolutely necessary.**
   Prefer a mature FIX engine such as QuickFIX/n with a cTrader-specific Rules-of-Engagement dictionary/configuration.

9. **Do not use multiple simultaneous active TRADE sessions for the same FIX account.**
   Use active/passive ownership or a distributed lock. Duplicate execution reports can otherwise occur.

10. **Never blindly convert MT5 lots directly into cTrader OrderQty.**
    Symbol IDs, quantity units, precision, minimum/maximum quantity, and sizing conversion must be explicitly normalized and tested.

---

# 2. Role for the Engineering Agent

Act as a **Principal Trading Systems Engineer / Senior Software Architect / FIX Engineer / MT5 Integration Engineer / Quant Engineer / DevOps Engineer / Security Engineer**.

You are responsible for:

- auditing the existing repository first,
- refactoring before adding major features,
- preserving working production functionality,
- building deterministic and testable boundaries,
- designing for failure/reconnect/replay,
- avoiding future-data leakage in ML,
- preventing duplicate orders,
- protecting credentials,
- building a professional React dashboard,
- documenting every architectural decision that materially affects production trading.

Do not behave like a junior developer who immediately creates new folders and services.

Before implementation, understand what already exists.

---

# 3. Primary Business Goal

We have roughly:

```text
5,000+ MT5 trader accounts
```

Most trading activity is expected to be:

```text
XAUUSD
```

The platform must identify traders whose behavior after their first few completed XAUUSD trades suggests a higher probability of **future copyable profitability**.

The target is not:

```text
Who made the most money in their first 3 trades?
```

The target is:

```text
Given the behavior visible up to now,
which traders have the highest probability of generating
positive future execution-venue-net P&L
while remaining inside risk limits?
```

---

# 4. Revised High-Level Architecture

```text
                    ┌──────────────────────────────┐
                    │         React Dashboard      │
                    │       TypeScript + Vite      │
                    └──────────────┬───────────────┘
                                   │
                                   ▼
                    ┌──────────────────────────────┐
                    │      ASP.NET Core API        │
                    └──────────────┬───────────────┘
                                   │
                 ┌─────────────────┼─────────────────┐
                 │                 │                 │
                 ▼                 ▼                 ▼
            PostgreSQL           Redis         SignalR/WebSocket
                 ▲
                 │
        ┌────────┴─────────┐
        │                  │
        │ Domain + Outbox  │
        │                  │
        └────────▲─────────┘
                 │
      ┌──────────┴──────────────────────────────────┐
      │                                             │
      │                                             │
┌─────┴───────────┐                         ┌───────┴───────────┐
│ MT5 Source Side │                         │ Execution Side    │
└─────┬───────────┘                         └───────┬───────────┘
      │                                             │
      ▼                                             ▼
Achiever MT5                                 cTrader FIX 4.4
StarwaveFX MT5                               QUOTE + TRADE
Future MT5 brokers                           Pepperstone/cServer
      │                                             ▲
      ▼                                             │
MT5 Manager collectors                              │
      │                                             │
      ▼                                             │
Raw events                                          │
      │                                             │
      ▼                                             │
Trade reconstruction                                │
      │                                             │
      ▼                                             │
Features + scoring                                  │
      │                                             │
      ▼                                             │
Shadow copy ──> CopyIntent ──> Risk Engine ─────────┘
```

---

# 5. Tech Stack

## Backend / Trading Services

Use:

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

Use a version compatible with the existing MT5 SDK/native DLL.

Do not upgrade runtime versions simply for fashion if the MT5 SDK depends on older/native runtime behavior.

## FIX Engine

Preferred:

```text
QuickFIX/n
FIX 4.4
cTrader-specific session configuration
cTrader-specific Rules-of-Engagement/data dictionary
```

Do not assume the generic FIX 4.4 dictionary is sufficient.

cTrader uses a defined subset and its own instrument identifiers/custom fields.

Pin compatible stable QuickFIX/n package versions.

## Database

```text
PostgreSQL
```

PostgreSQL remains the durable source of truth.

## Cache / Coordination

```text
Redis
```

Use for:

- live scores,
- short-lived cache,
- distributed execution-session ownership,
- short-lived locks,
- live dashboard data.

Do not use Redis as the authoritative store for orders, positions, or balances.

## ML / Research

```text
Python
FastAPI
XGBoost
scikit-learn
Polars
NumPy
```

Optional later:

```text
MLflow
```

No LLM API is required.

## Frontend

```text
React
TypeScript
Vite
TanStack Query
React Router
Zustand only when genuinely needed
SignalR/WebSocket
ECharts or Recharts
```

## Deployment

```text
Docker where compatible
Windows Worker if MT5 Manager DLL requires Windows
Linux for API/Postgres/Redis/Python/React if appropriate
```

Do not force native MT5 SDK components into Linux containers if the SDK does not support it cleanly.

---

# 6. Source Broker Architecture

The system currently has two MT5 source brokers:

```text
Achiever
StarwaveFX
```

The design must support more brokers without duplicating business logic.

Create a broker registry.

Conceptually:

```csharp
public interface IMt5BrokerConnector
{
    Task ConnectAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);

    Task<IReadOnlyCollection<Mt5Group>> GetGroupsAsync(CancellationToken ct);
    Task<IReadOnlyCollection<Mt5Account>> GetAccountsAsync(...);
    Task<IReadOnlyCollection<Mt5Deal>> GetDealsAsync(...);
    Task<IReadOnlyCollection<Mt5Order>> GetOrdersAsync(...);
    Task<IReadOnlyCollection<Mt5Position>> GetPositionsAsync(...);

    IAsyncEnumerable<Mt5Event> SubscribeAsync(CancellationToken ct);
}
```

The exact interface may be adjusted to the actual SDK.

Do not build two mostly identical connector codebases.

---

# 7. Achiever Configuration

Non-secret configuration currently includes:

```env
MT5_SERVER=57.128.141.65
MT5_PORT=443
MT5_LOGIN=2027
MT5_DEFAULT_GROUP=demo\Maxmaster
MT5_MODE=local
MT5_POOL_SIZE=8
MT5_SERVER_NAME=AchieverGlobalMarkets-Server
```

Secret:

```env
MT5_PASSWORD=<SECRET>
```

Required whitelisted outbound IP:

```text
81.29.145.69
```

If proxying is required, credentials must be in secret storage/environment variables.

Never log proxy credentials.

## Important

`demo\Maxmaster` is not the only group.

The system must dynamically enumerate **all groups accessible to the Manager login**.

Startup/resync:

```text
Connect
  ↓
Enumerate groups
  ↓
Upsert groups
  ↓
Enumerate accounts
  ↓
Associate accounts with broker + group
  ↓
Sync history
```

---

# 8. StarwaveFX Configuration

Non-secret configuration currently includes:

```env
MT5_STARWAVEFX_DISPLAY_NAME=StarwaveFX
MT5_STARWAVEFX_PROVISIONING_ENABLED=true
MT5_STARWAVEFX_MODE=local
MT5_STARWAVEFX_SERVER=84.201.6.142
MT5_STARWAVEFX_PORT=443
MT5_STARWAVEFX_LOGIN=9904
MT5_STARWAVEFX_SERVER_NAME=StarwaveFX
MT5_STARWAVEFX_POOL_SIZE=4
MT5_STARWAVEFX_PROXY_ENABLED=false
```

Secret:

```env
MT5_STARWAVEFX_PASSWORD=<SECRET>
```

No IP whitelist is currently required.

Still design the connector so proxy/whitelist routing can be enabled later.

---

# 9. Existing MT5 Plan-to-Group Mapping

Preserve:

```env
MT5_GROUP_2STEP_DEMO=demo\yo-2step
MT5_GROUP_1STEP_DEMO=demo\yo-1step

MT5_GROUP_2STEP_REAL=contest\yo-2step
MT5_GROUP_1STEP_REAL=contest\yo-1step

MT5_GROUP_INSTANT_REAL=contest\yo-instant

MT5_GROUP_CORE_DEMO=demo\yo-2step
MT5_GROUP_CORE_REAL=contest\yo-2step

MT5_GROUP_PASSFIRST_DEMO=demo\yo-payp
MT5_GROUP_PASSFIRST_REAL=contest\yo-payp
```

But these mappings must not determine which MT5 groups are fetched.

Correct:

```text
MT5 Manager API → discover all groups
                         ↓
                   optional plan mapping
```

Incorrect:

```text
Known plan mappings → only sync these groups
```

---

# 10. Multi-Broker Identity Rules

Never assume login or ticket IDs are globally unique.

Use compound identities:

```text
broker_id + login
broker_id + deal_ticket
broker_id + order_ticket
broker_id + position_id
```

All source-side tables must carry:

```text
broker_id
```

---

# 11. Raw MT5 Data Layer

Store source data before interpreting it.

Tables should include:

```text
mt5_accounts
mt5_account_snapshots
mt5_orders
mt5_deals
mt5_positions_current
mt5_groups
mt5_symbol_metadata
mt5_ticks_xauusd   # if source SDK/feed supports it
sync_checkpoints
ingestion_events
```

The raw layer should be as immutable as practical.

Corrections should be auditable.

---

# 12. Ingestion Pattern

Use:

```text
Historical Backfill
+
Live Event Subscription
+
Periodic Reconciliation
```

## Historical backfill

For each broker/account:

```text
Read checkpoint
    ↓
Fetch history
    ↓
Normalize
    ↓
Upsert idempotently
    ↓
Persist checkpoint
```

## Live flow

```text
MT5 event
   ↓
validate
   ↓
deduplicate
   ↓
persist raw record
   ↓
write transactional outbox event
   ↓
commit
```

Then background workers process the outbox.

This avoids coupling MT5 callbacks directly to ML or execution.

---

# 13. Why Use an Outbox Instead of Kafka Initially

At ~5,000 users, introducing Kafka on day one is likely unnecessary.

Use:

```text
PostgreSQL transactional outbox
```

for:

- trade-completed events,
- score-update requests,
- shadow-copy intents,
- risk-check requests,
- notification events.

If measured throughput later requires a dedicated broker, migrate behind an event-bus abstraction.

Do not preemptively introduce distributed infrastructure.

---

# 14. Trade Reconstruction Is Mandatory

MT5:

```text
Order != Deal != Position != Logical Trade
```

One position may contain:

- multiple entries,
- partial fills,
- scale-ins,
- partial closes,
- SL/TP modifications,
- multiple closing deals.

Create a canonical:

```text
ReconstructedTrade
```

Example fields:

```text
id
broker_id
login
position_id
canonical_symbol
source_symbol

direction

opened_at
closed_at

entry_vwap
exit_vwap

initial_volume
max_volume
closed_volume

gross_realized_pnl
commission
swap
fees
net_realized_pnl

deal_count
order_count

initial_sl
initial_tp
final_sl
final_tp

was_scaled_in
was_partial_close
was_averaged_down

completed
```

---

# 15. Exact Meaning of "First 3 Trades"

Count only:

```text
3 completed reconstructed XAUUSD position lifecycles
```

Do not count:

- order placement,
- deal fill,
- partial close,
- SL modification,
- TP modification

as a separate trade.

Trade #3 closure triggers:

```text
EARLY_SCORE_ELIGIBLE
```

not:

```text
PROVEN_PROFITABLE
```

---

# 16. XAUUSD Symbol Normalization

Source brokers may expose:

```text
XAUUSD
XAUUSD.
XAUUSDm
XAUUSD.a
GOLD
```

Execution venue may use a numeric cTrader instrument ID.

Create canonical symbol identity:

```text
CanonicalInstrument
  XAUUSD
```

and venue mappings:

```text
broker/source symbol → canonical XAUUSD
cTrader instrument ID → canonical XAUUSD
```

Never assume FIX tag 55 is the string `"XAUUSD"`.

For cTrader, retrieve the Security List and map the returned symbol/instrument ID to the canonical symbol.

Persist this mapping.

---

# 17. Source-Side Market Data Requirement

If we want exact features such as:

```text
MFE
MAE
price excursion
entry spread
volatility during trade
```

we need time-series price data while each source trade is open.

Preferred:

```text
MT5 source broker tick feed / Manager symbol tick subscription
```

If unavailable:

1. store the best available price feed explicitly,
2. mark the feature source,
3. do not pretend another broker's cTrader quote feed is identical to the source MT5 price stream.

Feature metadata should include:

```text
price_source
feature_quality
```

Example:

```text
price_source=ACHIEVER_MT5_TICKS
feature_quality=EXACT
```

or:

```text
price_source=BAR_APPROXIMATION
feature_quality=APPROXIMATE
```

Never silently mix them.

---

# 18. Deterministic Baseline Before ML

Before XGBoost, build a rules/statistics baseline.

Example score inputs:

```text
net pnl
profit/loss ratio
lot consistency
loss-size consistency
martingale detection
averaging down
holding time
SL use
maximum adverse excursion
maximum favorable excursion
risk escalation after loss
drawdown
trade frequency
session
```

The first baseline should generate:

```text
risk_score
behavior_score
early_quality_score
```

This baseline becomes the benchmark that ML must beat.

---

# 19. ML Objective

Once sufficient clean historical data exists, train:

```text
Input:
behavior/features observable through completed trade #3

Target:
future execution-venue-net profitability
over trades #4 through #23
subject to drawdown constraint
```

Initial label:

```text
label = 1
if:
    future_net_copy_pnl > 0
    AND future_max_drawdown <= configured_limit
else:
    label = 0
```

Use:

```text
XGBoost
```

initially.

Do not use a deep neural network first.

---

# 20. Data Leakage Protection

Never expose future information to the model.

A trade-#3 sample may only use information available up to the exact timestamp that trade #3 completed.

Do not include:

- final challenge result,
- future balance,
- future drawdown,
- trade #4 onward,
- eventual pass/fail,
- future market information.

Split training chronologically.

Example:

```text
oldest 70% → training
next 15%   → validation
newest 15% → untouched final test
```

---

# 21. Model Evaluation

Do not optimize around raw accuracy.

Evaluate:

```text
Top 1% selected traders
Top 5%
Top 10%
Top 20%
```

For each calculate future:

```text
net copied P&L
max drawdown
profit factor
return volatility
CVaR
trade count
execution cost
slippage sensitivity
```

Compare against:

```text
all traders
random traders
simple rules baseline
highest historical P&L baseline
highest win-rate baseline
```

ML is justified only if it beats simple baselines out-of-sample.

---

# 22. Continuous Rescoring

Trade #3 is the first score.

Then rescore:

```text
after trade 4
after trade 5
after trade 6
...
```

Maintain score history.

Suggested states:

```text
INSUFFICIENT_DATA
EARLY_SCORE
WATCH
SHADOW
LIVE_CANDIDATE
LIVE
PAUSED
RISK_BLOCKED
DISQUALIFIED
```

---

# 23. Default Safety Gate for Three-Trade Traders

Recommended default:

```text
Trade #3 + high score
        ↓
SHADOW only
```

Do not automatically send real capital after three trades.

A configurable later gate may require:

```text
minimum completed trades
minimum shadow trades
minimum shadow net P&L
maximum shadow DD
minimum current score
no severe risk flags
```

Example defaults can be chosen later from data.

Do not hardcode arbitrary numbers before backtesting.

---

# 24. Shadow Copy Engine

Shadow copy must model the actual destination venue as closely as practical.

Input:

```text
source MT5 trade event
```

Use destination quote feed:

```text
cTrader QUOTE FIX session
```

to simulate:

- entry price,
- spread,
- quote freshness,
- slippage,
- execution delay,
- partial fill assumptions where applicable,
- swap/commission model.

Persist:

```text
shadow_copy_order
shadow_copy_fill
shadow_position
shadow_pnl
source_vs_shadow_slippage
```

---

# 25. New Execution Venue: cTrader / cServer FIX 4.4

Real approved copy trades will route to the provided Pepperstone cTrader account through cServer FIX 4.4.

## Host

```env
CTRADER_FIX_HOST=live-us-eqx-01.p.c-trader.com
```

## QUOTE session

Provided connection details:

```env
CTRADER_FIX_QUOTE_SSL_PORT=5211
CTRADER_FIX_QUOTE_PLAIN_PORT=5201
CTRADER_FIX_QUOTE_SENDER_COMP_ID=live.pepperstone.1369850
CTRADER_FIX_QUOTE_TARGET_COMP_ID=cServer
CTRADER_FIX_QUOTE_SESSION_QUALIFIER=QUOTE
```

## TRADE session

```env
CTRADER_FIX_TRADE_SSL_PORT=5212
CTRADER_FIX_TRADE_PLAIN_PORT=5202
CTRADER_FIX_TRADE_SENDER_COMP_ID=live.pepperstone.1369850
CTRADER_FIX_TRADE_TARGET_COMP_ID=cServer
CTRADER_FIX_TRADE_SESSION_QUALIFIER=TRADE
```

## Login secret

```env
CTRADER_FIX_ACCOUNT_ID=1369850
CTRADER_FIX_PASSWORD=<SECRET: account 1369850 password>
```

### Production transport

Use SSL/TLS endpoints by default:

```text
QUOTE = 5211
TRADE = 5212
```

Plain-text ports must not be the production default.

---

# 26. Important cTrader FIX Header Mapping Warning

Do not blindly infer FIX tag placement from the human-readable credential form.

The provided connection details label the session qualifier as:

```text
SenderSubID = QUOTE / TRADE
```

The official cTrader FIX Rules of Engagement define session-related header fields including:

```text
SenderCompID
TargetCompID
TargetSubID
SenderSubID
```

and specify QUOTE/TRADE semantics in those headers.

Therefore the implementation must:

1. preserve the exact broker-issued credentials,
2. make both `SenderSubID` and `TargetSubID` configurable,
3. follow the current official cTrader Rules of Engagement,
4. never silently change case such as `cServer` to `CSERVER` unless the issued configuration/spec requires it,
5. prove successful Logon for both sessions in staging/diagnostics before enabling execution.

Do not hardcode assumptions from an old sample.

---

# 27. cTrader FIX Session Separation

Maintain two independent FIX session objects:

```text
CTraderQuoteSession
CTraderTradeSession
```

They must have independent:

```text
connection state
message sequence state
heartbeat state
last inbound timestamp
last outbound timestamp
reconnect state
metrics
logs
```

Do not share one sequence counter between QUOTE and TRADE.

---

# 28. FIX Session Ownership

For one cTrader trading account, do not allow two production instances to simultaneously own the same TRADE session.

Implement:

```text
single-active-session ownership
```

using one of:

- deployment singleton,
- database advisory lock,
- Redis lease with fencing token,
- leader election.

The database must remain the authority for execution state.

If leadership changes:

```text
new instance
    ↓
establish FIX session
    ↓
reconcile positions/orders
    ↓
only then accept new execution intents
```

---

# 29. cTrader FIX Capabilities to Use

Implement at minimum the cTrader-supported workflows needed for execution and reconciliation:

```text
Logon
Logout
Heartbeat
TestRequest
ResendRequest / sequence handling
Reject handling

SecurityListRequest
SecurityList

MarketDataRequest
MarketDataSnapshot
MarketDataIncrementalRefresh

NewOrderSingle
ExecutionReport

OrderStatusRequest
OrderMassStatusRequest

RequestForPositions
PositionReport

OrderCancelRequest
OrderCancelReject

OrderCancelReplaceRequest

BusinessMessageReject
```

Do not implement only "send market order" and call the FIX integration complete.

---

# 30. Instrument Discovery on cTrader

On startup:

```text
TRADE/QUOTE session active
        ↓
Security List Request
        ↓
Security List response
        ↓
find XAUUSD instrument
        ↓
persist:
    cTrader instrument ID
    symbol name
    precision/digits
```

Do not hardcode an instrument ID from another cTrader account or broker.

---

# 31. Destination Quote Feed

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

Example policy:

```text
if quote_age > configured_max_quote_age:
    reject new copy order
```

The threshold must be configurable and measured.

---

# 32. FIX Trade Execution Flow

Correct production flow:

```text
Source MT5 event
      ↓
Copy candidate?
      ↓
Create CopyIntent
      ↓
Persist
      ↓
RiskEngine evaluates
      ↓
ApprovedExecutionIntent
      ↓
Persist
      ↓
FIX Execution Worker
      ↓
NewOrderSingle
      ↓
ExecutionReport(s)
      ↓
Persist fills/order state
      ↓
Update destination position
      ↓
Reconcile
```

Never send a FIX order directly from an MT5 event callback.

---

# 33. Idempotent Order Submission

Every destination order must have a unique client order ID.

Persist before sending:

```text
execution_intent_id
cl_ord_id
source_broker_id
source_login
source_trade_id
source_event_id
destination_account
canonical_symbol
side
requested_quantity
created_at
status
```

The execution service must be able to distinguish:

```text
not sent
sent but acknowledgement unknown
accepted
partially filled
filled
rejected
cancelled
```

Never simply retry a NewOrderSingle because the TCP connection broke.

First reconcile.

---

# 34. Unknown Execution State

Critical case:

```text
send order
   ↓
network disconnects
   ↓
did cServer receive it?
```

Do NOT blindly send the order again.

Set:

```text
EXECUTION_STATE_UNKNOWN
```

Then use:

```text
OrderStatusRequest
OrderMassStatusRequest
ExecutionReports
Position reconciliation
```

to determine the real state.

Only after reconciliation may the system decide whether another order is required.

---

# 35. Source-to-Destination Position Mapping

Maintain explicit mapping:

```text
source reconstructed trade
        ↓
destination execution orders
        ↓
destination cTrader position ID(s)
```

Persist destination `Position ID` returned by FIX execution/position reports.

Support:

- initial entry,
- source scale-in,
- source partial close,
- full close,
- source reversal.

Do not assume one source event equals one destination order forever.

---

# 36. Copy Timing Rules

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

---

# 37. Slippage / Price-Move Guard

Before sending:

```text
expected destination price
current destination quote
source price
```

Calculate price deviation.

Risk policy may reject:

```text
PRICE_MOVED_TOO_FAR
QUOTE_STALE
SPREAD_TOO_WIDE
```

This is especially important for XAUUSD around news.

---

# 38. Position Sizing

Never blindly:

```text
source 0.10 MT5 lots
=
destination OrderQty 0.10
```

Create a normalized sizing layer:

```text
source volume
    ↓
canonical notional/risk
    ↓
portfolio allocation
    ↓
destination instrument quantity
```

Inputs:

```text
source symbol contract size
destination symbol quantity convention
destination minimum quantity
destination step size
account leverage
available margin
risk allocation
current XAU exposure
trader confidence
```

Build unit tests against real known examples before live execution.

---

# 39. Risk Engine

The risk engine is the final authority.

Scoring/ML may only produce:

```text
candidate
confidence
suggested allocation
```

Risk engine decides:

```text
approve
reduce size
reject
pause trader
pause venue
global stop
```

Hard limits include:

```text
max loss per selected trader
max daily execution-account loss
max portfolio drawdown
max XAUUSD gross exposure
max XAUUSD net exposure
max position quantity
max number of open positions
max allowed spread
max quote age
max source-signal age
max tolerated price move
max slippage
max execution account margin usage
martingale block
abnormal sizing block
venue health requirement
```

---

# 40. Kill Switch

Provide:

```text
STOP_NEW_EXECUTION
```

and a separately permissioned:

```text
EMERGENCY_FLATTEN
```

Do not conflate them.

`STOP_NEW_EXECUTION` prevents new copy orders but leaves existing positions untouched.

`EMERGENCY_FLATTEN` attempts to close destination positions and therefore requires stronger authorization/confirmation.

---

# 41. Real Execution Feature Flags

Default:

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

This allows:

- connecting,
- receiving prices,
- requesting orders/positions,
- validating FIX connectivity,

without automatically placing new real orders.

Actual NewOrderSingle submission should require:

```env
REAL_COPY_EXECUTION_ENABLED=true
```

plus runtime risk-engine healthy state.

---

# 42. cTrader Startup Reconciliation

On trade-session login:

```text
Login successful
    ↓
block new executions
    ↓
OrderMassStatusRequest
    ↓
RequestForPositions
    ↓
consume Execution/Position reports
    ↓
compare with internal DB
    ↓
repair/update state
    ↓
only if reconciled:
READY_FOR_EXECUTION
```

Never assume the database is correct after restart.

---

# 43. Daily / Periodic Reconciliation

Periodically compare:

```text
internal open orders
internal destination positions
vs
cServer order/position state
```

Raise alerts for:

```text
unknown external position
missing internal position
quantity mismatch
side mismatch
orphan execution report
unexpected fill
```

---

# 44. cTrader Execution Tables

Add durable tables such as:

```text
execution_venues
fix_sessions
fix_session_events

destination_symbols
destination_quotes

copy_intents
risk_decisions
execution_intents

fix_orders
fix_execution_reports
destination_positions

source_destination_links
execution_reconciliation_runs
execution_reconciliation_issues
```

---

# 45. Recommended Core Database Tables

Full initial set:

```text
brokers
broker_connections
mt5_groups
plan_group_mappings

mt5_accounts
mt5_account_snapshots
mt5_orders
mt5_deals
mt5_positions_current
mt5_symbols
mt5_xau_ticks

reconstructed_trades
canonical_instruments
source_symbol_mappings

trader_feature_snapshots
trader_scores
trader_score_history
trader_states
trader_risk_flags

model_versions
model_predictions
model_evaluations

shadow_orders
shadow_fills
shadow_positions
shadow_performance

copy_intents
copy_allocations

risk_decisions
risk_events

execution_venues
destination_symbols
destination_quotes

fix_sessions
fix_session_events
fix_orders
fix_execution_reports
destination_positions

source_destination_links

sync_checkpoints
outbox_events
audit_logs
system_events
```

---

# 46. React Dashboard

Build a professional operational dashboard.

## Main navigation

```text
Overview
Brokers
MT5 Groups
Traders
Trader Detail
Trade Explorer
Scoring
Models
Shadow Portfolio
Live Copy Portfolio
cTrader FIX
Risk
Reconciliation
System Health
Audit
Settings
```

---

# 47. Overview Page

Show:

```text
Total MT5 accounts
Connected source brokers

XAUUSD traders
Traders with >= 3 completed trades

Watch
Shadow
Live candidates
Live copied
Risk blocked

Shadow P&L
Destination real P&L

Current XAU gross exposure
Current XAU net exposure
Destination free margin
Destination margin level

MT5 ingestion health
FIX quote health
FIX trade health
```

---

# 48. Brokers Page

For each MT5 broker:

```text
Display name
Connection status
Server
Manager login masked where appropriate
Group count
Account count
Deal ingest rate
Last event
Last successful history sync
Connection pool usage
Reconnect count
```

No secret values.

---

# 49. MT5 Groups Page

Display every dynamically discovered group.

Columns:

```text
Broker
Group
Accounts
Enabled for analysis
Plan mapping
Last discovered
Last synced
```

---

# 50. Trader Leaderboard

Columns:

```text
Broker
Login
Group
Completed XAU trades

Net source P&L
Early score
ML probability
Risk score

Martingale flag
Averaging-down flag
Lot escalation flag

Current state
Shadow P&L
Live allocation
Last scored
```

Filters:

```text
broker
group
state
score
risk
trade count
martingale
date
```

---

# 51. Trader Detail Page

Show:

```text
Account overview
XAU trade history
First 3 trades highlighted

Score timeline
Risk flags
Behavior features

Lot-size timeline
Holding-time distribution
SL/TP behavior
Drawdown
MFE/MAE when valid

Shadow copied positions
Shadow P&L
Live copied positions
Live P&L

Source-to-destination mapping
```

---

# 52. cTrader FIX Page

Show separate cards:

```text
QUOTE SESSION
TRADE SESSION
```

Each displays:

```text
Host
SSL Port
Connected?
Logged on?
Session status
Last inbound
Last outbound
Message sequence state
Reconnect count
Last heartbeat/test request
Errors
```

For QUOTE also show:

```text
XAUUSD mapped?
Instrument ID
Bid
Ask
Quote age
Spread
```

For TRADE show:

```text
Execution enabled?
Open orders
Open destination positions
Last execution report
Last reconciliation
```

Never show FIX password.

---

# 53. Risk Dashboard

Show:

```text
Execution account equity if available
Balance
Free margin
Margin level

Daily P&L
Current drawdown

XAU long quantity
XAU short quantity
Net XAU exposure

Risk by copied trader
Risk by source broker

Rejected copy intents
Reasons for rejection

STOP_NEW_EXECUTION state
EMERGENCY_FLATTEN availability
```

---

# 54. Reconciliation Dashboard

Show:

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

Nothing unresolved should be silently ignored.

---

# 55. Security

Never expose:

```text
MT5 passwords
proxy credentials
cTrader account password
FIX password
database passwords
Redis passwords
```

to React.

Use:

```text
environment variables
OS secret store
Vault/cloud secrets manager
```

Production secrets must not be committed to Git.

Create only placeholders in `.env.example`.

---

# 56. Secret-Safe Example Configuration

```env
# =========================
# Achiever MT5
# =========================

MT5_SERVER=57.128.141.65
MT5_PORT=443
MT5_LOGIN=2027
MT5_PASSWORD=<SECRET>
MT5_DEFAULT_GROUP=demo\Maxmaster
MT5_MODE=local
MT5_POOL_SIZE=8
MT5_SERVER_NAME=AchieverGlobalMarkets-Server

ACHIEVER_EGRESS_IP=81.29.145.69

# Optional proxy
ACHIEVER_PROXY_ENABLED=true
ACHIEVER_PROXY_HOST=81.29.145.69
ACHIEVER_PROXY_PORT=49527
ACHIEVER_PROXY_USERNAME=<SECRET>
ACHIEVER_PROXY_PASSWORD=<SECRET>

# =========================
# StarwaveFX MT5
# =========================

MT5_STARWAVEFX_DISPLAY_NAME=StarwaveFX
MT5_STARWAVEFX_PROVISIONING_ENABLED=true
MT5_STARWAVEFX_MODE=local
MT5_STARWAVEFX_SERVER=84.201.6.142
MT5_STARWAVEFX_PORT=443
MT5_STARWAVEFX_LOGIN=9904
MT5_STARWAVEFX_PASSWORD=<SECRET>
MT5_STARWAVEFX_SERVER_NAME=StarwaveFX
MT5_STARWAVEFX_POOL_SIZE=4
MT5_STARWAVEFX_PROXY_ENABLED=false

# =========================
# cTrader FIX execution
# =========================

CTRADER_FIX_HOST=live-us-eqx-01.p.c-trader.com

CTRADER_FIX_ACCOUNT_ID=1369850
CTRADER_FIX_PASSWORD=<SECRET>

CTRADER_FIX_QUOTE_SSL_PORT=5211
CTRADER_FIX_QUOTE_PLAIN_PORT=5201
CTRADER_FIX_QUOTE_SENDER_COMP_ID=live.pepperstone.1369850
CTRADER_FIX_QUOTE_TARGET_COMP_ID=cServer
CTRADER_FIX_QUOTE_SESSION_QUALIFIER=QUOTE
CTRADER_FIX_QUOTE_SENDER_SUB_ID=<BROKER_ISSUED_VALUE>
CTRADER_FIX_QUOTE_TARGET_SUB_ID=<BROKER_ISSUED_VALUE>

CTRADER_FIX_TRADE_SSL_PORT=5212
CTRADER_FIX_TRADE_PLAIN_PORT=5202
CTRADER_FIX_TRADE_SENDER_COMP_ID=live.pepperstone.1369850
CTRADER_FIX_TRADE_TARGET_COMP_ID=cServer
CTRADER_FIX_TRADE_SESSION_QUALIFIER=TRADE
CTRADER_FIX_TRADE_SENDER_SUB_ID=<BROKER_ISSUED_VALUE>
CTRADER_FIX_TRADE_TARGET_SUB_ID=<BROKER_ISSUED_VALUE>

CTRADER_FIX_USE_SSL=true

CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true

REAL_COPY_EXECUTION_ENABLED=false
```

The engineer must populate SenderSubID/TargetSubID according to the current broker-issued FIX form and cTrader Rules of Engagement rather than guessing from labels.

---

# 57. Logging

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

Never log authentication tags containing passwords.

Redact sensitive values centrally.

---

# 58. Metrics

Expose:

## MT5

```text
mt5_connected
mt5_reconnects
mt5_events_total
mt5_deals_total
mt5_duplicate_deals_total
mt5_backfill_lag
mt5_outbox_backlog
```

## Reconstruction

```text
reconstructed_trades_total
reconstruction_failures_total
trade_completion_latency
```

## Scoring

```text
score_requests_total
score_failures_total
prediction_latency
shadow_candidates
live_candidates
```

## FIX

```text
fix_quote_connected
fix_trade_connected
fix_logon_failures
fix_reconnects
fix_inbound_messages_total
fix_outbound_messages_total
fix_rejects_total
fix_business_rejects_total
fix_execution_reports_total
fix_unknown_execution_states
```

## Execution

```text
copy_intents_total
risk_rejections_total
execution_orders_total
execution_fills_total
execution_rejections_total
source_to_fill_latency
slippage
```

---

# 59. Authentication and RBAC

Dashboard roles:

```text
SuperAdmin
RiskManager
Analyst
ReadOnly
```

Only authorized roles may:

```text
enable real execution
change risk limits
pause/resume trader copying
change symbol mapping
activate stop-new-orders
request emergency flatten
promote a model
change broker/FIX configuration
```

All actions must be audited.

---

# 60. Testing Strategy

## Unit tests

Required:

```text
MT5 deal deduplication
trade reconstruction
partial close
scale-in
full close
position reversal

XAU canonical mapping
source/destination quantity conversion

drawdown
MFE/MAE where data exists
martingale detection
averaging-down detection

score-state transitions

risk limits

copy-intent idempotency
ClOrdID generation
ExecutionReport state transitions
```

## Integration tests

Required:

```text
PostgreSQL migrations
MT5 backfill/restart
outbox processing

QuickFIX/n session configuration
FIX message parse/build
ExecutionReport handling
position reconciliation
unknown-execution recovery
```

## Replay tests

Build:

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

This allows deterministic debugging.

---

# 61. FIX Simulation / Test Harness

Before using real NewOrderSingle:

Build a FIX adapter test mode.

It should allow:

```text
parse recorded ExecutionReports
replay MarketDataIncrementalRefresh
simulate disconnects
simulate duplicate ExecutionReports
simulate partial fill
simulate rejection
simulate unknown-state disconnect
```

Do not use the real account as the first integration test.

---

# 62. Failure Rules

## MT5 unavailable

```text
Do not invent source trades.
Continue retrying.
Expose stale-source status.
Do not open new copied positions from stale source data.
```

## ML unavailable

```text
Continue ingestion/reconstruction.
Do not promote new traders to live based on missing scoring.
Existing hard risk limits remain active.
```

## QUOTE FIX unavailable

```text
Do not create new live copy trades requiring fresh pricing.
```

## TRADE FIX unavailable

```text
Do not queue an unlimited backlog of stale entries.
Mark new intents appropriately.
Do not resend unknown orders blindly.
```

## Database unavailable

Execution service should fail closed for new orders.

Do not run critical real execution solely from volatile memory.

---

# 63. No Blind Catch-Up Copying

If FIX is disconnected for 3 minutes while source traders opened 20 trades:

Do NOT reconnect and blindly execute all 20 old entries.

Each CopyIntent must have:

```text
expires_at
max_signal_age
```

Stale entries expire.

Closing/reducing risk may have separate policy from opening new exposure.

---

# 64. Source Close Handling

Closing an existing copied position is a risk-reduction action and may deserve different treatment from new entries.

Design separate policy:

```text
OPEN_EXPOSURE
INCREASE_EXPOSURE
REDUCE_EXPOSURE
CLOSE_EXPOSURE
```

Risk engine should normally be stricter about opening/increasing than reducing/closing.

---

# 65. Copy Correlation / Concentration

Do not copy 50 "different traders" if they are effectively the same XAUUSD strategy.

Track correlation by:

```text
direction
entry time
holding time
return series
session
lot behavior
```

Add concentration caps.

Example:

```text
maximum allocation per correlated strategy cluster
```

This can be Phase 2 after basic copy execution is stable.

---

# 66. Suggested Repository Structure

Adapt to the existing repo; do not create duplicates unnecessarily.

Possible:

```text
/apps
  /web
  /api
  /mt5-worker
  /fix-worker

/services
  /ml-service

/src
  /Domain
  /Application
  /Infrastructure
  /Mt5
  /TradeReconstruction
  /Scoring
  /Shadow
  /Risk
  /Execution
  /Fix.CTrader

/tests
  /Unit
  /Integration
  /Replay
  /Fix
  /Risk

/docs
  architecture.md
  mt5-integration.md
  trade-reconstruction.md
  xauusd-normalization.md
  scoring.md
  ml.md
  shadow-copy.md
  risk.md
  ctrader-fix.md
  reconciliation.md
  deployment.md
```

---

# 67. Engineering Phases

## Phase 0 — Audit

Produce:

```text
existing architecture map
existing MT5 code map
existing DB schema map
existing background services
duplicate/dead code report
security issues
migration state
deployment map
```

No major implementation until this is understood.

## Phase 1 — Reliable MT5 ingestion

Deliver:

```text
Achiever connected
StarwaveFX connected
all groups discovered
accounts synchronized
history backfilled
live deals persisted
idempotency proven
reconciliation working
```

## Phase 2 — XAUUSD reconstruction

Deliver:

```text
source symbol mappings
true reconstructed trades
first-3-trade counter
unit tests
```

## Phase 3 — Statistical baseline + dashboard

Deliver:

```text
deterministic feature engine
risk flags
early scoring baseline
React trader dashboard
```

## Phase 4 — cTrader QUOTE integration

Deliver:

```text
SSL FIX quote session
logon/session health
Security List
XAU instrument mapping
live XAU quote
quote persistence/cache
dashboard health
```

No live trading yet.

## Phase 5 — Shadow copy

Deliver:

```text
copy intents
shadow entries/exits
destination quote pricing
shadow P&L
source-vs-shadow analysis
```

## Phase 6 — ML

Deliver only after data quality is proven:

```text
training dataset
chronological split
XGBoost
probability calibration
top-N evaluation
comparison against deterministic baseline
```

## Phase 7 — cTrader TRADE read/reconciliation

Deliver:

```text
SSL FIX trade session
OrderMassStatusRequest
RequestForPositions
ExecutionReport parser
PositionReport parser
reconciliation
```

Still keep real NewOrderSingle disabled.

## Phase 8 — Risk-controlled execution

Deliver:

```text
risk engine
execution intent
idempotency
NewOrderSingle
ExecutionReport lifecycle
cancel/replace
unknown-state recovery
kill switch
```

Enable only with explicit production flag.

---

# 68. Go-Live Gates

Do not enable real copying until all of these are true:

```text
[ ] MT5 historical/live ingestion is stable
[ ] duplicate event handling is proven
[ ] trade reconstruction tests pass
[ ] XAU symbol mappings are verified
[ ] quote session stable
[ ] trade session stable
[ ] cTrader reconciliation works after restart
[ ] copy intents are idempotent
[ ] unknown execution state recovery works
[ ] position sizing conversion is verified
[ ] risk engine unit/integration tests pass
[ ] stale quote rejection works
[ ] stale signal rejection works
[ ] shadow copy has sufficient sample
[ ] destination costs/slippage measured
[ ] kill switch tested
[ ] secrets removed from repo/logs
[ ] dashboard exposes venue health/risk
[ ] manual review completed
```

---

# 69. Acceptance Criteria for the First Useful Version

The first genuinely useful system does **not** need ML.

It should already be able to:

```text
1. Connect to both MT5 brokers.
2. Discover all groups.
3. Synchronize ~5,000 accounts.
4. Capture XAUUSD trades correctly.
5. Reconstruct logical trades.
6. Detect the first 3 completed XAUUSD trades.
7. Produce a deterministic trader/risk score.
8. Rank traders.
9. Connect to cTrader QUOTE FIX securely.
10. Discover the Pepperstone XAUUSD instrument ID.
11. Shadow-copy selected traders using destination quotes.
12. Show all of this in React.
```

Only after this works should ML be judged.

---

# 70. Acceptance Criteria for Live FIX Execution

Before production live execution:

```text
1. TRADE FIX Logon is stable.
2. ExecutionReports are persisted correctly.
3. Position reports reconcile after restart.
4. Unique ClOrdID rules are proven.
5. Duplicate report handling is proven.
6. Unknown-state recovery is proven.
7. Partial fills are supported.
8. Order rejects are supported.
9. Cancel/replace is supported where required.
10. Destination position mapping is correct.
11. Risk-engine rejection happens before FIX send.
12. Real execution is feature flagged.
13. Global stop-new-orders works.
14. Reconciliation blocks execution while inconsistent.
```

---

# 71. What Not to Build Yet

Do not add initially:

```text
Kafka
Kubernetes
ClickHouse
LLM/AI API
deep learning
reinforcement learning
complex microservice mesh
cross-region active-active FIX execution
automated model self-promotion
```

These can be revisited only when measurements justify them.

---

# 72. Senior Engineer Rules

1. Audit first.
2. Preserve production behavior.
3. Use migrations.
4. Never commit secrets.
5. Never expose secrets to the browser.
6. Make MT5 callbacks lightweight.
7. Persist before asynchronous processing.
8. Every execution request must be idempotent.
9. Never blindly retry a possibly-sent order.
10. Reconcile after FIX reconnect.
11. Use independent QUOTE and TRADE session state.
12. Use TLS in production.
13. Discover cTrader symbols/instrument IDs; do not guess.
14. Normalize quantity explicitly.
15. ML never bypasses risk.
16. Trade #3 means early evidence, not proven skill.
17. New entries must expire when stale.
18. Reduce/close exposure must be treated differently from opening more.
19. Every manual override must be audited.
20. Prefer simple systems until data proves more complexity is necessary.

---

# 73. Required Developer Output Before Coding

Before large implementation changes, produce:

## A. Repository audit

```text
Current architecture
Existing MT5 implementation
Existing DB tables/migrations
Existing trading/copy functionality
Existing broker config
Security issues
Dead/duplicate code
```

## B. Gap analysis

Compare current repo against this target architecture.

Classify each component:

```text
EXISTS_AND_GOOD
EXISTS_NEEDS_REFACTOR
MISSING
DEPRECATED
UNSAFE
```

## C. Implementation sequence

List exact files/modules/migrations that will change.

## D. Risk list

Identify:

```text
MT5 SDK constraints
Windows/native DLL constraints
source tick-data availability
cTrader FIX credential/header ambiguity
symbol/quantity mapping
live-account safety
```

Then begin implementation incrementally.

---

# 74. References Used for This Revised Architecture

Reviewed against the official cTrader FIX documentation, including:

- cTrader FIX API overview
- cTrader FIX Rules of Engagement / Specification
- cTrader FIX send/receive guidance
- cTrader FIX FAQ

Key verified points include:

- cTrader supports FIX 4.4.
- price quotation and trade messaging use separate connections.
- NewOrderSingle, ExecutionReport, order status, positions, cancel/replace, market data, and security-list workflows are supported.
- sequence/session handling must follow cTrader Rules of Engagement.
- multiple simultaneous API connections can result in duplicate reports.
- instrument identifiers are provided by Spotware/cServer rather than assuming a text symbol in tag 55.

Official documentation:

```text
https://help.ctrader.com/fix/
https://help.ctrader.com/fix/specification/
https://help.ctrader.com/fix/sending-and-receiving-messages/
https://help.ctrader.com/fix/faqs/
```

QuickFIX/n:

```text
https://github.com/connamara/quickfixn
https://quickfixn.org/
```

---

# 75. Final Target

The finished platform should operate as:

```text
          ACHIEVER MT5
               │
               ├──────────────┐
               │              │
          STARWAVEFX MT5      │
               │              │
               └──────┬───────┘
                      ▼
               MT5 Manager API
                      ↓
               Reliable ingestion
                      ↓
              Trade reconstruction
                      ↓
               XAUUSD analytics
                      ↓
          Rules/statistical baseline
                      ↓
                ML ranking
              when justified
                      ↓
                Trader state
                      ↓
                 SHADOW COPY
                      ↓
                 CopyIntent
                      ↓
                 Risk Engine
                      ↓
              ExecutionIntent
                      ↓
          ┌───────────┴───────────┐
          │                       │
          ▼                       ▼
 cTrader QUOTE FIX          cTrader TRADE FIX
   SSL :5211                  SSL :5212
          │                       │
          │                       ▼
          │                 NewOrderSingle
          │                       ↓
          │                 ExecutionReport
          │                       ↓
          └───────────────► Reconciliation
                                  ↓
                        Pepperstone cServer
```

The software must remain **deterministic, auditable, idempotent, risk-gated, recoverable after restart, and safe by default**.

Real order submission must remain OFF until the system has passed the defined shadow, reconciliation, sizing, and risk-engine gates.
