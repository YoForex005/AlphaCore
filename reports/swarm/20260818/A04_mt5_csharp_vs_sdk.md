# A04 — C# `src/Mt5` vs C++ `mt5-sdk` (Architecture §§6–12)

**Date:** 2026-08-18  
**Agent:** A04 (senior engineer audit)  
**Product source:** not modified  
**Scope:** map C++ `IMT5Client` onto the C# `IMt5BrokerConnector` that architecture §§6–12 require, with quoted evidence.

---

## 0. Verdict (measured, not aspirational)

| Surface | Measured state |
|---|---|
| C# `IMt5BrokerConnector` | **Does not exist.** Zero types, zero methods, zero DTOs. |
| C# `TraderIntelligence.Mt5` | Empty `Class1` stub. Coverage of `IMT5Client` = **0 / 41**. |
| C# `TraderIntelligence.Mt5Worker` | Template `BackgroundService` that logs every 1s. No broker loop. |
| C++ `IMT5Client` | Real, transport-agnostic contract. Two implementations: `MT5Manager` (local Manager API) and `MT5HttpClient` (remote HTTP). |
| C++ config | **Single-broker.** Achiever-shaped `MT5_*` keys exist. **No** `MT5_STARWAVEFX_*` fields. |
| C++ DTOs | Login/ticket/position fields exist. **No `broker_id`.** Identity is not multi-broker safe. |
| Fit to §§6–12 | C++ is a **prop-firm admin + dealer** layer extracted from YoPips. The C# collector must **subset** it (read/subscribe) and **not** copy create/deposit/SendTrade onto the source connector. |

**PASS/FAIL vs architecture Phase 1 (“Achiever connected / StarwaveFX connected / all groups discovered / history backfilled / live deals persisted”):** **FAIL.** C# has no connector. C++ cannot yet be two brokers. No C# persistence of raw MT5 tables.

Architecture itself authorises adjusting the C# sketch to the real SDK:

> The exact interface may be adjusted to the actual SDK.  
> Do not build two mostly identical connector codebases.  
> — `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §6, lines 353–355

---

## 1. Evidence inventory (read-only)

| Path | What was inspected |
|---|---|
| `D:\Prop\src\Mt5\Class1.cs` | Entire file: empty class. |
| `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj` | net8.0 lib; refs Domain + Application; no native/HTTP packages. |
| `D:\Prop\src\Domain\Class1.cs`, `Application\Class1.cs`, `Infrastructure\Class1.cs` | Empty stubs. |
| `D:\Prop\apps\mt5-worker\{Program.cs,Worker.cs,appsettings.json,TraderIntelligence.Mt5Worker.csproj}` | Template worker; no MT5 config. |
| `D:\Prop\Mt5TraderIntelligence.sln` | Scaffold only. |
| `D:\Prop\mt5-sdk\src\core\imt5_client.h` | Full 177-line interface. |
| `D:\Prop\mt5-sdk\src\core\mt5_types.h` | Full DTO + event queue + JSON serde. |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.{h,cpp}` | Local impl, Connect/pump, extractors, GetDeals/GetAllGroups. |
| `D:\Prop\mt5-sdk\src\core\mt5_http_client.{h,cpp}` | Remote impl; paging GetDeals; GetOrders/GetGroupDetails **not** overridden. |
| `D:\Prop\mt5-sdk\src\core\mt5_pool.h` | Request-only session pool (no pump). |
| `D:\Prop\mt5-sdk\src\core\mt5_watchdog.h` | Reconnect supervisor for `MT5Manager`. |
| `D:\Prop\mt5-sdk\src\core\mt5_tick_bridge.h` | Pump-thread enqueue / poll fallback. |
| `D:\Prop\mt5-sdk\config\app_config.{h,cpp}` | Single-broker + plan-group keys. |
| `D:\Prop\mt5-sdk\.env.example` | Documented env surface. |
| `D:\Prop\mt5-sdk\README.md` | Two-transport model. |
| `D:\Prop\mt5-sdk\src\services\mt5_ledger_store.h` | Optional immutable deal ledger (not the §11 table set). |
| `D:\Prop\mt5-sdk\src\services\mt5_account_helper.h` | Plan→group **provisioning** helper (wrong layer for collector). |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | `PUMP_MODE_*` enum; `DealRequest` / `DealRequestPage`. |
| Architecture §§6–12 | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 322–571. |

Workspace grep over `*.cs` for `IMt5BrokerConnector`, `IMt5`, `DllImport`, `PInvoke`, `NativeLibrary`, `Mt5Deal`, `broker_id`: **no matches**.

---

## 2. C# `src/Mt5` as found

`TraderIntelligence.Mt5` is a placeholder library. Entire production source:

```1:6:D:\Prop\src\Mt5\Class1.cs
namespace TraderIntelligence.Mt5;

public class Class1
{

}
```

Project:

```1:14:D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\Domain\TraderIntelligence.Domain.csproj" />
    <ProjectReference Include="..\Application\TraderIntelligence.Application.csproj" />
  </ItemGroup>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    ...
  </PropertyGroup>
</Project>
```

No `AllowUnsafeBlocks`, no C++/CLI, no `DllImport` of `MT5APIManager64.dll`, no HttpClient to an MT5 microservice, no options types for Achiever/StarwaveFX.

The worker that §66 places at `/apps/mt5-worker` does not call the library:

```1:21:D:\Prop\apps\mt5-worker\Worker.cs
namespace TraderIntelligence.Mt5Worker;
public class Worker : BackgroundService
{
    ...
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ...
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

`appsettings.json` has only `Logging`. No `MT5_SERVER`, no broker registry.

Adjacent C# projects are equally empty (`Domain` / `Application` / `Infrastructure` `Class1`). Package *intent* is visible (`Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.4, `StackExchange.Redis` 2.8.0, `FluentValidation` 11.9.2) but there are **no entities, no outbox, no checkpoints**.

Honest metric: **C# implements none of the architecture §6 interface and none of `IMT5Client`.**

---

## 3. C++ `mt5-sdk` as found

README states the product shape:

> A reusable C++20 MetaTrader 5 integration layer, extracted from the YoPips prop-firm backend…  
> `IMT5Client` — the transport-agnostic interface everything else codes against  
> Both `MT5Manager` and `MT5HttpClient` implement `IMT5Client`.  
> — `D:\Prop\mt5-sdk\README.md` lines 3–42

Class comment:

```12:15:D:\Prop\mt5-sdk\src\core\imt5_client.h
// Abstract interface for MT5 operations.
// Implemented by MT5Manager (local SDK) and MT5HttpClient (remote microservice).
// All services and controllers depend on this interface, not the concrete implementation.
class IMT5Client {
```

Two transports, one interface:

| Transport | Type | Platform | Events | Notes |
|---|---|---|---|---|
| `local` | `MT5Manager` | Windows x64 Manager DLL | Pump sinks → `MT5EventQueue` | `Initialize` / `Connect` / `SetProxy` / `Disconnect` are **on the concrete class, not on `IMT5Client`**. |
| `remote` | `MT5HttpClient` | Cross-platform curl | SSE → same queue | `GetOrders`, `GetGroupDetails`, `GetRecentDeals`, `SubscribeTicks`, `GetChart` stay at interface defaults (fail closed / empty). |

Supporting machinery **not** on `IMT5Client` but required to run a collector:

| Piece | Path | Role for §§6–12 |
|---|---|---|
| `MT5Manager::Connect` | `mt5_manager.cpp:71–150` | Pump users/orders/positions/symbols; fallback to no-pump if pump connect fails. |
| `MT5Pool` / `MT5Session` | `mt5_pool.h` | Request-only sessions so history reads do not own the pump mutex. |
| `MT5Watchdog` | `mt5_watchdog.h` | 30s health + exponential reconnect. |
| `MT5TickBridge` | `mt5_tick_bridge.h` | Non-blocking `OnTick` enqueue; poll `GetTickLast` if push unsupported. |
| `ProxyConfig` | `mt5_types.h:501–508` | SOCKS/HTTP; re-applied on every `Connect`. |
| `AppConfig` | `app_config.h` | Single `MT5_*` endpoint + plan-group map. |
| `mt5_ledger::Store` | `mt5_ledger_store.h` | Optional immutable deal revisions. **Not** the §11 table set. |
| `MT5AccountHelper` | `mt5_account_helper.h` | Plan→group **account creation**. Collector must not use this as a group filter. |

Default pump mask (when `pumpMode == 0`):

```103:108:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
        mode = IMTManagerAPI::PUMP_MODE_USERS |
               IMTManagerAPI::PUMP_MODE_ORDERS |
               IMTManagerAPI::PUMP_MODE_POSITIONS |
               IMTManagerAPI::PUMP_MODE_SYMBOLS;
```

MetaQuotes confirms there is **no `PUMP_MODE_DEALS`**:

```127:143:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
      PUMP_MODE_USERS         =0x00000001,   // pump users
      PUMP_MODE_ACTIVITY      =0x00000002,   // pump users online activity
      PUMP_MODE_MAIL          =0x00000004,   // pump mails
      PUMP_MODE_ORDERS        =0x00000008,   // pump orders
      PUMP_MODE_NEWS          =0x00000020,   // pump news
      PUMP_MODE_POSITIONS     =0x00000080,   // pump positions
      ...
      PUMP_MODE_FULL          =0xffffffff    // full pumping
```

`IMT5Client` already documents the consequence (do not ignore this when designing C# live ingestion):

```79:87:D:\Prop\mt5-sdk\src\core\imt5_client.h
    // ... without depending on the deals pump (there
    // is NO PUMP_MODE_DEALS in the MT5 SDK, so OnDealAdd/OnDealUpdate likely never
    // fire at runtime). Callers synthesize this from a successful SendTrade result
```

That synthesis path (`CacheExecutedDeal` after `SendTrade`) is **execution-side**. A **source collector** does not place the trader’s deals, so it **cannot** populate the recent-deals ring that way. Live deal ingestion must be `GetDeals` + reconciliation, plus `OnDealAdd` *if* it ever fires.

---

## 4. Architecture §6 sketch vs C++ reality

Architecture wants one connector per broker:

```338:350:D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
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

| C# sketch method | C++ method(s) that actually implement it | 1:1? |
|---|---|---|
| `ConnectAsync` | **Not on `IMT5Client`.** `MT5Manager::Initialize` + `SetProxy` + `Connect`; `MT5HttpClient::connect`. | No — must be added on the C# connector. |
| `DisconnectAsync` | **Not on `IMT5Client`.** `MT5Manager::Disconnect` (also `m_eventQueue.stop()`). | No — add on C# connector. |
| `GetGroupsAsync` | `GroupTotal` + `GetAllGroups` + `GetGroupDetails` | Compose. Names-only is insufficient for `mt5_groups`. |
| `GetAccountsAsync` | **No single call.** `GetAllGroups`/`GetGroupLogins`/`GetUserLogins` → `GetUser` + `GetAccount` per login. | Compose. |
| `GetDealsAsync` | `GetDeals(login, from, to)` **plus** `GetRecentDeals` (lag >40s) **plus** checkpoint window. | Almost; must merge + fail closed on incomplete history. |
| `GetOrdersAsync` | `GetOrders` — **default `false`**; HTTP client does not override. | Yes on local; **unsupported on remote**. |
| `GetPositionsAsync` | `GetPositions` | Yes. |
| `SubscribeAsync` | `GetEventQueue()` (`pop` with timeout), not `IAsyncEnumerable`. Tick stream is a **separate** `SubscribeTicks`. | Adapt. Two streams: trading events vs ticks. |

`IsConnected` / `GetLastError` are on `IMT5Client` but missing from the architecture sketch. C# must expose them for §7/§8 health and the dashboard “Brokers” page.

---

## 5. Full `IMT5Client` → C# `IMt5BrokerConnector` coverage map

Legend:

- **MUST** — public on `IMt5BrokerConnector` (or a thin sibling on the same connector) for §§6–12.
- **INTERNAL** — connector implementation needs it (connect, pool, proxy, watchdog) but it is not a product-facing collector verb.
- **COMPOSE** — no 1:1 C++ method; C# builds the architecture method from several C++ calls.
- **OUT** — YoPips admin/dealer. Do **not** put on the source collector. Keep off the Phase-1 interface.
- **LATER** — useful after Phase 1 (MFE/MAE, charts, news). Do not block ingestion.

C# column is the **measured** state today (all **ABSENT**).

### 5.1 Lifecycle / session (Connect is *not* on `IMT5Client`)

| C++ | Kind | C# `IMt5BrokerConnector` must cover? | Why (§) | C# now |
|---|---|---|---|---|
| `IMT5Client::IsConnected` | pure | **MUST** | §7 startup/resync; §8 health; fail closed when MT5 down (§62). | ABSENT |
| `IMT5Client::GetLastError` | default `""` | **MUST** | Human connect failure (IP block / timeout / bad creds). `MT5Manager` overrides. | ABSENT |
| `MT5Manager::Initialize(dllPath)` | concrete | **INTERNAL** | Load `MT5APIManager64.dll`. Windows worker only. | ABSENT |
| `MT5Manager::Connect(server, login, password, pumpMode)` | concrete | **MUST** as `ConnectAsync` | §6/§7/§8. Default pump = users+orders+positions+symbols. Falls back to no-pump (`mt5_manager.cpp:114–135`). | ABSENT |
| `MT5Manager::SetProxy(...)` + `ProxyConfig` | concrete | **INTERNAL** (options) | §7 whitelist `81.29.145.69`; §8 “design so proxy can be enabled later”. Never log proxy password. | ABSENT |
| `MT5Manager::Disconnect` | concrete | **MUST** as `DisconnectAsync` | §6. Stops event queue. | ABSENT |
| `MT5HttpClient::connect` | concrete | **INTERNAL** alt transport | Remote mode SSE + health. | ABSENT |
| `MT5Pool::{Initialize,Borrow,Return,healthCheck}` | sibling | **INTERNAL** | Backfill `GetDeals`/`GetUser` must not hold the pump mutex. `MT5_POOL_SIZE` is in §7/§8. | ABSENT |
| `MT5Watchdog` | sibling | **INTERNAL** | Reconnect with backoff; required for live §12. | ABSENT |

Quoted connect fallback (collector must treat no-pump as “events unavailable”, not “connected = live”):

```114:135:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
    if (res != MT_RET_OK) {
        spdlog::error("MT5 Connect failed: {} — retrying without pump mode", res);
        ...
        res = m_manager->Connect(..., 0, 30000);
        ...
        spdlog::warn("MT5 connected in no-pump mode — real-time events disabled, request API available");
        return true;
    }
```

### 5.2 Groups — §6 `GetGroupsAsync`, §7 dynamic enum, §9 “do not filter by plan map”

| C++ | Kind | Cover? | Evidence |
|---|---|---|---|
| `GroupTotal()` | pure | **MUST** (or derive from GetAllGroups) | `imt5_client.h:164` |
| `GetAllGroups(vector<string>&)` | pure | **MUST** | Local walks `GroupNext` (`mt5_manager.cpp:962–981`). HTTP `GET /mt5/groups`. |
| `GetGroupDetails(vector<GroupDetail>&)` | pure | **MUST** | `GroupDetail` = name, currency, digits, company, margin_call, margin_stop_out, connections_allowed. HTTP **returns false** (`mt5_http_client.cpp:666–669`). Local-only for a complete `mt5_groups` row. |
| `GetGroupLogins(group)` | pure | **MUST** (feeds `GetAccountsAsync`) | Delegates to `GetUserLogins` on local (`mt5_manager.cpp:1015–1016`). |
| `GetUserLogins(group)` | pure | **MUST** (same) | `UserLogins` Manager API (`mt5_manager.cpp:315–327`). |

§7 required startup/resync (C# worker must implement; C++ does not):

```397:409:D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
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

§9 law (do **not** implement `GetGroupsAsync` by reading `MT5_GROUP_*`):

```461:475:D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
But these mappings must not determine which MT5 groups are fetched.

Correct:
MT5 Manager API → discover all groups → optional plan mapping

Incorrect:
Known plan mappings → only sync these groups
```

C++ already has the mapping keys (`app_config.h:27–37`, loaded in `app_config.cpp:118–127`). They belong in a **provisioning** options object, **not** in the collector’s group query.

### 5.3 Accounts — §6 `GetAccountsAsync`, §11 `mt5_accounts` / `mt5_account_snapshots`

| C++ | Kind | Cover? | Evidence |
|---|---|---|---|
| `GetUser(login, UserData&)` | pure | **MUST** | UserRequest + optional UserAccountRequest overlay (`mt5_manager.cpp:245–277`). Fields: login, name, email, group, leverage, country/city/phone, registration, last_access, rights, plus balances if account fetch succeeds. |
| `GetAccount(login, AccountData&)` | pure | **MUST** | Cache-first `UserAccountGet` then `UserAccountRequest` (`mt5_manager.cpp:332–364`). balance/credit/equity/margin/margin_free/margin_level/profit/floating/storage. |

**There is no `GetAccounts()`.** C# `GetAccountsAsync` is **COMPOSE**:

```
foreach group in GetAllGroups/GetGroupDetails
    logins = GetGroupLogins(group)
    foreach login
        user = GetUser(login)
        acct = GetAccount(login)
        stamp broker_id
```

At 5,000+ logins this **must** use `MT5Pool`, not the pump `MT5Manager` mutex.

### 5.4 Positions / orders / deals — §6 + §11 + §12

| C++ | Kind | Cover? | Evidence / trap |
|---|---|---|---|
| `GetPositions(login)` | pure | **MUST** | Pump cache `PositionGet` then `PositionRequest` (`mt5_manager.cpp:396–426`). Maps to `mt5_positions_current`. |
| `GetOrders(login)` | default **false** | **MUST** | Open/pending only. Local implemented (`mt5_manager.cpp:428–481`). HTTP **does not override** → `false`. Architecture §6 requires it; remote transport cannot satisfy it today. |
| `GetDeals(login, from, to)` | pure | **MUST** | Complete-history contract (`imt5_client.h:61–65`). Local is a **single** `DealRequest` with **no** `DealRequestPage` (`mt5_manager.cpp:485–509`). HTTP **does** follow cursor/page up to 10 000 requests and **returns false on partial** (`mt5_http_client.cpp:505–548`). C# must fail closed the same way. MetaQuotes exposes `DealRequestPage` (`MT5APIManager.h:526`) — local C++ does not use it. |
| `GetRecentDeals(login, from, to)` | default false | **MUST** (merge into `GetDealsAsync`) | Pump-only ring, 4096 deals, `>40s` DealRequest lag (`imt5_client.h:67–77`, `mt5_manager.cpp:529–536`). Empty+true means “cache authoritative and empty”, not “no deals in history”. |
| `CacheExecutedDeal` | default no-op | **OUT** | Filled from **our** `SendTrade`. Source collector is not the dealer. Do not put on `IMt5BrokerConnector`. |

`GetDeals` contract (copy this into C# comments; do not weaken):

```61:65:D:\Prop\mt5-sdk\src\core\imt5_client.h
    // Complete-history contract: implementations must follow every provider
    // page/cursor for [from,to], or return false. Callers treat false as
    // dependency_unavailable and must not make a pass/fail decision.
    virtual bool GetDeals(uint64_t login, int64_t from, int64_t to, std::vector<DealData>& out) = 0;
```

`DealData.position` **is** extracted locally:

```1508:1524:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
DealData MT5Manager::extractDeal(const IMTDeal* deal) {
    ...
    d.order = deal->Order();
    d.position = deal->PositionID();
    ...
}
```

…but **omitted** from JSON `to_json`/`from_json` (`mt5_types.h:335–349`). Remote HTTP history therefore **drops `position`**, which is the reconstruction key (`broker_id + position_id`, §10). C# local path must keep `position`. C# HTTP path must not trust the current serde.

### 5.5 Events — §6 `SubscribeAsync`, §12 live flow

| C++ | Kind | Cover? | Evidence |
|---|---|---|---|
| `GetEventQueue()` | pure | **MUST** as `SubscribeAsync` | `imt5_client.h:26–29`. Local: sink callbacks. Remote: SSE. |
| `MT5EventType` | type | **MUST** DTO | Position/Deal/Order/User Add/Update/Delete (`mt5_types.h:511–524`). |
| `MT5EventQueue::pop(timeout)` | type | **INTERNAL** adapt to `IAsyncEnumerable` | Blocking queue with stop flag (`mt5_types.h:539–571`). C# should wrap, not expose native queue. |

Pump callbacks enqueue without doing I/O (`mt5_manager.cpp:1362–1485`). §12 requires the same split: callback/queue → persist+outbox on a worker thread. **Do not** call EF/Postgres from a P/Invoke callback / pump thread.

Deal-event caveat (repeat because it breaks naïve “SubscribeAsync = live deals”):

```1391:1395:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
    // There is no PUMP_MODE_DEALS in the SDK enum, so with POSITIONS-mode
    // pumping this is expected to be silent.
```

§12 “Live Event Subscription” therefore **cannot be the only deal path**. Periodic `GetDeals` reconciliation is mandatory even when `SubscribeAsync` is running.

### 5.6 Symbols / ticks / charts — §11 `mt5_symbol_metadata` / `mt5_ticks_xauusd` (and later MFE/MAE)

| C++ | Kind | Cover? | Evidence |
|---|---|---|---|
| `SymbolTotal` / `GetSymbol` / `GetSymbolByName` | pure | **MUST** | Populate `mt5_symbol_metadata`; XAUUSD suffix variants (§16, out of this file’s section but required to store metadata). |
| `GetManagerSymbols` / `GetGroupSymbols` | default empty | **MUST** (group tradable set) | Local overrides. Default `{}` is a silent empty — C# must treat empty+unsupported as fail, not “broker has no symbols”. |
| `GetTickLast` | pure | **MUST** if persisting ticks | Snapshot. |
| `GetAllTicksLast` | default false | **LATER** / optional snapshot | Local override. |
| `SubscribeTicks` / `UnsubscribeTicks` | default false | **MUST** for `mt5_ticks_xauusd` live | Fail-closed. Pump-thread contract: enqueue only (`imt5_client.h:118–128`). HTTP default false → poll `GetTickLast` like `MT5TickBridge`. |
| `GetChart` | default false | **LATER** | Bar approximation if ticks unavailable (arch §17). Not Phase-1 raw layer. |

### 5.7 Server time — §12 checkpoints / history windows

| C++ | Kind | Cover? | Evidence |
|---|---|---|---|
| `GetServerTime()` | pure | **MUST** | Window arithmetic. Local: `TimeServer()`, **falls back to host `time(nullptr)` if disconnected** (`mt5_manager.cpp:1110–1117`). C# must **not** silently use host clock for checkpoints — mark `usedFallback` the way `resolveMt5TimeWindow` does (`mt5_time_window.h:7–15`). |

### 5.8 Out of scope for `IMt5BrokerConnector` (do not copy onto the collector)

These are real `IMT5Client` methods. They exist because this SDK was extracted from an account-provisioning / dealer backend. Architecture §§6–12 is a **source collector**. Putting them on `IMt5BrokerConnector` would invite accidental mutation of 5,000 trader accounts.

| C++ | Why OUT |
|---|---|
| `CreateUser` / `DeleteUser` | Provisioning. |
| `UpdateUser` / `UpdateUserLeverage` / `UpdateUserGroup` / `UpdateUserRights` | Mutates live source accounts. |
| `ChangePassword` / `CheckPassword` | Secrets; never needed to read history. |
| `DealerBalance` / `Deposit` / `Withdraw` | Moves money. |
| `DealerSendOrder` / `SendTrade` | Places orders. Destination is cTrader FIX, not source MT5. |
| `CacheExecutedDeal` | Only valid after **our** SendTrade. |
| `GetNewsCalendarItems` / `GetCalendarEvents` | Default unsupported; not in §11 tables. |

If a later admin tool needs these, give them a **separate** `IMt5AdminClient` so the collector worker cannot call `Withdraw`.

---

## 6. Architecture §7 — Achiever configuration

Architecture non-secret keys (`Architecture_v2.md:363–371`):

```
MT5_SERVER=57.128.141.65
MT5_PORT=443
MT5_LOGIN=2027
MT5_DEFAULT_GROUP=demo\Maxmaster
MT5_MODE=local
MT5_POOL_SIZE=8
MT5_SERVER_NAME=AchieverGlobalMarkets-Server
```

C++ `AppConfig` binds the same **names** (`app_config.cpp:108–116`) but:

| Key | Architecture | C++ default / example | C# |
|---|---|---|---|
| `MT5_DEFAULT_GROUP` | `demo\Maxmaster` | `demo\\default` (`app_config.h:23`, `.env.example:33`) | none |
| `MT5_SERVER` | `57.128.141.65` | empty until env | none |
| `MT5_MODE` | `local` | `"local"` | none |
| `MT5_POOL_SIZE` | 8 | 8 | none |
| `MT5_PASSWORD` | secret | loaded, never logged in snippets reviewed | none |
| Proxy | optional; never log creds | `IS_MT5_PROXY_ENABLED` + `MT5_PROXY_*` | none |
| Whitelist IP `81.29.145.69` | required | encoded only as error text `MT_RET_AUTH_MANAGER_IPBLOCK` (`mt5_manager.cpp:64`) | none |

C# worker `appsettings.json` has **no** Achiever section. Launch profile sets only `DOTNET_ENVIRONMENT`.

`MT5_DEFAULT_GROUP` is a **provisioning landing group**, not a fetch filter (§7 “`demo\Maxmaster` is not the only group” + §9).

---

## 7. Architecture §8 — StarwaveFX configuration

Architecture keys (`Architecture_v2.md:417–427`): `MT5_STARWAVEFX_DISPLAY_NAME`, `_PROVISIONING_ENABLED`, `_MODE`, `_SERVER=84.201.6.142`, `_PORT`, `_LOGIN=9904`, `_SERVER_NAME`, `_POOL_SIZE=4`, `_PROXY_ENABLED`, plus secret `_PASSWORD`.

**C++ `AppConfig` has zero `MT5_STARWAVEFX_*` fields.** Confirmed in `app_config.h` (single `mt5_server` / `mt5_login` / `mt5_password`) and `.env.example` (no StarwaveFX block).

Implication: the C++ SDK is **one process ↔ one manager login**. Two source brokers require **two connector instances** (two `MT5Manager`/`MT5Pool`/`MT5Watchdog` triples), not a second field on the same struct.

C# must introduce a broker registry (exactly what §6 asks) of the form:

```
IReadOnlyDictionary<BrokerId, IMt5BrokerConnector>
  achiever   → options from MT5_* (or Mt5:Achiever:*)
  starwavefx → options from MT5_STARWAVEFX_* (or Mt5:StarwaveFx:*)
```

Do **not** fork `TraderIntelligence.Mt5` into `Mt5.Achiever` / `Mt5.StarwaveFx` (“Do not build two mostly identical connector codebases”).

Proxy: StarwaveFX currently `PROXY_ENABLED=false`, but “still design the connector so proxy/whitelist routing can be enabled later” — reuse C++ `ProxyConfig` shape (`type`, `address`, `port`, `login`, `password`, `enabled`).

---

## 8. Architecture §9 — plan-to-group mapping

C++ preserves the **same env names** as the architecture list:

```118:127:D:\Prop\mt5-sdk\config\app_config.cpp
    cfg.mt5_group_2step_demo     = get("MT5_GROUP_2STEP_DEMO",     ...);
    ...
    cfg.mt5_group_passfirst_real = get("MT5_GROUP_PASSFIRST_REAL", ...);
```

`.env.example` documents them as **optional provisioning** only (lines 43–57).

`MT5AccountHelper::getMt5Group` is the **incorrect** pattern for a collector: it maps product plan → one group for **CreateUser**. Compile-time defaults also **drift** from architecture:

| Env | Architecture §9 | `mt5_account_helper.h` default |
|---|---|---|
| `MT5_GROUP_2STEP_REAL` | `contest\yo-2step` | `Flexy\\yo-2step` |
| `MT5_GROUP_1STEP_REAL` | `contest\yo-1step` | `Flexy\\yo-1step` |
| `MT5_GROUP_INSTANT_REAL` | `contest\yo-instant` | `Flexy\\yo-instant` |

C# collector rule: **ignore these keys when enumerating**. Persist them as optional metadata on `mt5_groups` (“this group is the 2-step demo landing group”) after discovery, never as the discovery source.

---

## 9. Architecture §10 — multi-broker identity

Architecture:

```481:496:D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
Never assume login or ticket IDs are globally unique.
Use compound identities:
  broker_id + login
  broker_id + deal_ticket
  broker_id + order_ticket
  broker_id + position_id
All source-side tables must carry: broker_id
```

C++ DTOs (`UserData`, `AccountData`, `DealData`, `OrderData`, `PositionData`, `MT5Event`) have `login` / `ticket` / `position` and **no `broker_id`**. `mt5_ledger::RawEvent` uses `serverKey` (string), not a first-class broker id.

C# must **stamp `broker_id` at the connector boundary** (the instance knows which broker it is). Do not wait for C++ to grow the field. Do not use `MT5_SERVER_NAME` as a primary key (it is a display label; C++ derives it from host if blank).

`DealData.position` is the position half of `broker_id + position_id`. Losing it on the HTTP JSON path (see §5.4) is a §10 defect if remote mode is used.

---

## 10. Architecture §11 — raw MT5 data layer vs SDK types

Required tables vs C++ / C# today:

| Table | C++ type / store | C# entity | Connector methods that fill it |
|---|---|---|---|
| `mt5_groups` | `GroupDetail` | **none** | `GetAllGroups` + `GetGroupDetails` |
| `mt5_accounts` | `UserData` | **none** | `GetUser` (+ group association) |
| `mt5_account_snapshots` | `AccountData` | **none** | `GetAccount` on a schedule |
| `mt5_orders` | `OrderData` | **none** | `GetOrders` + `Order*` events |
| `mt5_deals` | `DealData` + optional `mt5_ledger::DealRevision` | **none** | `GetDeals` + `GetRecentDeals` + `Deal*` events |
| `mt5_positions_current` | `PositionData` | **none** | `GetPositions` + `Position*` events |
| `mt5_symbol_metadata` | `SymbolData` | **none** | `GetSymbol` / `GetSymbolByName` / `GetGroupSymbols` |
| `mt5_ticks_xauusd` | `TickData` | **none** | `SubscribeTicks` or polled `GetTickLast` |
| `sync_checkpoints` | **none** in SDK | **none** | C# owns this (`GetServerTime` + last deal time) |
| `ingestion_events` | `mt5_ledger::RawEvent` (optional Postgres) | **none** | wrap every persist |

Ledger store is closer to §11 immutability than anything in C#, but it is deal-centric and opt-in (`MT5SDK_WITH_POSTGRES`). It is **not** a substitute for the table list.

C# `Infrastructure` already references EF Core + Npgsql — the right place for these tables — but `Class1.cs` is empty.

DTO field checklist the C# records must keep (from `mt5_types.h`):

| C++ struct | Fields the collector must persist |
|---|---|
| `GroupDetail` | name, currency, currency_digits, company, margin_call, margin_stop_out, connections_allowed |
| `UserData` | login, name, email, group, leverage, country, city, phone, registration, last_access, rights |
| `AccountData` | login, balance, credit, equity, margin, margin_free, margin_level, profit, floating, storage |
| `PositionData` | ticket, login, symbol, action, volume (hundredths of lots), price_open/current/sl/tp, profit, storage, time_create, time_update, comment |
| `DealData` | ticket, login, **order**, **position**, symbol, action, entry, volume, price, profit, commission, storage, time, comment |
| `OrderData` | ticket, login, symbol, type, state, volume, price_order/current/sl/tp, time_setup, comment |
| `SymbolData` | symbol, path, description, digits, contract_size, volume_min/max/step, trade_mode |
| `TickData` | symbol, bid, ask, last, volume, time, time_msc, flags |
| `MT5Event` | type, login, variant payload |

Volume is **MT5 native integer units** (“hundredths of lots” on `PositionData`, comment at `mt5_types.h:75`). Do not treat as cTrader `OrderQty` (arch §1 item 10).

---

## 11. Architecture §12 — ingestion pattern vs what exists

Required pattern:

```529:535:D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
Historical Backfill
+
Live Event Subscription
+
Periodic Reconciliation
```

| Leg | C++ today | C# today | Gap |
|---|---|---|---|
| Historical backfill | `GetDeals(from,to)` exists. **No checkpoint type.** Local does not page. HTTP pages or returns false. | none | C# must own `sync_checkpoints`, call `GetServerTime` for windows, fail closed on `GetDeals==false`. Prefer pool sessions. Consider Manager `DealRequestPage` / `DealRequestByGroup` (SDK has them; `IMT5Client` does not wrap them). |
| Live events | `GetEventQueue` + sinks. Deal callbacks **likely silent**. | none | Wrap queue as `IAsyncEnumerable`. Persist raw + outbox. **Poll deals** because no `PUMP_MODE_DEALS`. |
| Periodic reconciliation | No scheduler in SDK. `GetPositions` / `GetOrders` / `GetDeals` / `GetAccount` / `GetUser` are the primitives. | 1s log loop | Worker must snapshot positions + catch missed deals. |
| Outbox | Not in SDK. Ledger is immutable revisions, not an outbox. | none | §12: persist raw + transactional outbox, then workers. Do not couple pump callbacks to scoring/FIX. |
| Dedup | `cacheRecentDeal` dedups last 32 by ticket in-memory only. | none | Durable unique `(broker_id, deal_ticket)` / `(broker_id, order_ticket)`. |

Live flow in the architecture (validate → dedup → persist raw → outbox → commit) has **no C# types**. C++ event callbacks only `push` onto `MT5EventQueue` — that part is the correct split; C# must keep it.

---

## 12. Recommended C# surface (adjusted to the real SDK)

Place in `TraderIntelligence.Mt5` (or Domain port + Mt5 adapter). One implementation, N broker instances.

```csharp
// Adjusted from architecture §6 to the actual IMT5Client.
// Connect/Disconnect are NOT on IMT5Client (they live on MT5Manager).
public interface IMt5BrokerConnector
{
    string BrokerId { get; }

    Task ConnectAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);
    bool IsConnected { get; }
    string LastError { get; }          // IMT5Client::GetLastError

    Task<IReadOnlyList<Mt5Group>> GetGroupsAsync(CancellationToken ct);
    // COMPOSE: GetAllGroups + GetGroupDetails

    Task<IReadOnlyList<ulong>> GetGroupLoginsAsync(string group, CancellationToken ct);
    // IMT5Client::GetGroupLogins / GetUserLogins

    Task<Mt5User?> GetUserAsync(ulong login, CancellationToken ct);
    Task<Mt5AccountSnapshot?> GetAccountAsync(ulong login, CancellationToken ct);
    Task<IReadOnlyList<Mt5Account>> GetAccountsAsync(CancellationToken ct);
    // COMPOSE: groups → logins → GetUser+GetAccount; stamp BrokerId

    Task<IReadOnlyList<Mt5Deal>> GetDealsAsync(ulong login, long fromUnix, long toUnix, CancellationToken ct);
    // IMT5Client::GetDeals; merge GetRecentDeals; return fail (not empty) if incomplete

    Task<IReadOnlyList<Mt5Order>> GetOrdersAsync(ulong login, CancellationToken ct);
    Task<IReadOnlyList<Mt5Position>> GetPositionsAsync(ulong login, CancellationToken ct);

    Task<IReadOnlyList<Mt5Symbol>> GetSymbolsAsync(CancellationToken ct);
    Task<Mt5Tick?> GetTickLastAsync(string symbol, CancellationToken ct);
    long GetServerTimeUnix();

    IAsyncEnumerable<Mt5Event> SubscribeAsync(CancellationToken ct);
    // wraps GetEventQueue; must not block pump/SSE thread with IO

    // Optional same interface, fail-closed:
    ValueTask<bool> TrySubscribeTicksAsync(IMt5TickSink sink, CancellationToken ct);
}
```

Explicitly **not** on this interface: `CreateUser`, `DeleteUser`, password, leverage/group/rights updates, `DealerBalance`/`Deposit`/`Withdraw`, `DealerSendOrder`, `SendTrade`, `CacheExecutedDeal`, news/calendar.

Transport choice (do not invent a third):

| Mode | How C# talks to C++ | When |
|---|---|---|
| `local` | P/Invoke / C++/CLI / named-pipe host around `MT5Manager`+`MT5Pool` | Windows mt5-worker; lowest lag; consumes manager slots (`README.md` lines 39–40). |
| `remote` | HTTP client matching `MT5HttpClient` paths | If a C++ microservice already owns the DLL. Accept `GetOrders`/`GetGroupDetails`/`SubscribeTicks` as unsupported until the remote side grows them. |

Do not load Manager DLLs inside Linux API containers (arch §5 Deployment; CMake only compiles `mt5_manager.cpp` on `WIN32`).

---

## 13. Coverage scorecard

| Architecture need | C++ primitive | C# `IMt5BrokerConnector` |
|---|---|---|
| Connect / Disconnect | concrete Manager/Http, **not** `IMT5Client` | **0%** |
| Discover all groups | `GetAllGroups` + `GetGroupDetails` | **0%** |
| Enumerate accounts | compose logins + `GetUser`/`GetAccount` | **0%** |
| History deals | `GetDeals` (+ page contract) | **0%** |
| Live deals | `GetEventQueue` **unreliable**; need poll | **0%** |
| Orders / positions | `GetOrders` / `GetPositions` | **0%** |
| Ticks | `SubscribeTicks` / `GetTickLast` | **0%** |
| Server time / checkpoints | `GetServerTime`; no checkpoint store | **0%** |
| Two brokers | one `AppConfig` | **0%** |
| `broker_id` | absent on DTOs | **0%** |
| Plan map not used as filter | map exists for **provisioning** | N/A (no C# fetch yet) |
| Outbox + raw tables | optional ledger only | **0%** |
| Admin/dealer verbs | present on `IMT5Client` | correctly **not** started — keep them off the collector |

**41 `IMT5Client` methods. C# implements 0.**  
**~22 of 41 are MUST/INTERNAL for §§6–12.**  
**~12 of 41 are OUT (admin/dealer/news) and must stay off `IMt5BrokerConnector`.**  
**~7 of 41 are LATER (charts, news, all-ticks, manager-symbol helpers).**

---

## 14. Risks if C# copies the C++ SDK blindly

1. **Copying the whole `IMT5Client` onto the collector** exposes `Withdraw`/`SendTrade` next to history reads.
2. **Assuming `SubscribeAsync` delivers deals** — there is no `PUMP_MODE_DEALS`; `OnDealAdd` is expected silent.
3. **Treating `GetDeals()==true` + empty as complete** after a local single `DealRequest` — no paging; 5,000-account backfill will silently truncate. HTTP path is stricter (false on partial). C# must be at least as strict as HTTP.
4. **Using `MT5_GROUP_*` as the group list** — violates §9; misses `demo\Maxmaster` siblings.
5. **One `AppConfig` / one worker connection** — cannot attach StarwaveFX without a second instance.
6. **Persisting C++ DTOs as primary keys** — login/ticket collide across brokers.
7. **HTTP `DealData` JSON drops `position`** — reconstruction and §10 identity break in remote mode.
8. **Calling Manager APIs on the pump mutex from C# backfill** — C++ already split pump vs `MT5Pool` for this reason.
9. **Using `GetServerTime` host fallback as checkpoint time** — writes host clock into `sync_checkpoints`.
10. **Logging proxy / manager passwords** — §7 explicit ban; C++ `SetProxy` formats `login:password` into `MTProxyInfo` (keep that off Serilog).

---

## 15. What “done” looks like for this gap (not implemented; audit only)

Phase 1 (architecture §67) is not started in C#. Minimum to claim `IMt5BrokerConnector` covers `IMT5Client` for §§6–12:

1. Types exist in C# (`IMt5BrokerConnector` + DTOs with `broker_id`).
2. One implementation, two configured instances (Achiever + StarwaveFX).
3. `GetGroupsAsync` uses Manager discovery, not `MT5_GROUP_*`.
4. `GetDealsAsync` fail-closed + merge `GetRecentDeals`; checkpoints persisted.
5. `SubscribeAsync` + periodic `GetDeals`/`GetPositions` reconciliation.
6. Raw tables + outbox in Infrastructure.
7. Admin/dealer methods **not** on the collector interface.
8. Tests: identity collisions across two fake brokers; incomplete history returns error not empty; plan-map filter rejected.

Until then, report this gap as **open**. Do not claim the C# Mt5 project “wraps the existing SDK”.
