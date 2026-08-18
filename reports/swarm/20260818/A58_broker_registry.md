# A58 — `IMt5BrokerConnector` + broker registry (Achiever + StarwaveFX)

**Artifact:** `D:\Prop\reports\swarm\20260818\A58_broker_registry.md`  
**Source of truth:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§6–8  
**Config law:** **§56 placeholders only** — no invented env / `appsettings` keys  
**Supporting sections:** §4, §9–12, §45, §48, §55, §57–58, §62, §66–67, §72  
**Sibling reports:** A02 (ports), A04 (SDK map), A07 (worker), A12 (`IMT5Client`), A15 (pool/watchdog), A20 (tables), A30 (sequence), A39 (group discovery), A40 (plan map is not a filter)  
**Date:** 2026-08-18  
**Status:** specification only — **no product source modified**  
**Scope:** one collector contract, one implementation, two configured instances. Business logic (discover → upsert → backfill → live → reconcile) is written **once** against the registry.

---

## 0. Binding laws

From architecture §6:

```text
Achiever
StarwaveFX
```

1. Support more brokers **without duplicating business logic**.
2. Create a **broker registry**.
3. One `IMt5BrokerConnector` surface. **Do not build two mostly identical connector codebases.**
4. The exact interface **may be adjusted to the actual SDK** (`IMT5Client` / `MT5Manager` / `MT5Pool`).
5. Source side is **read/collect**. Destination is cTrader FIX. Do not put dealer/provisioning verbs on this port.

From §7 / §8:

- Achiever: Manager login `2027`, `57.128.141.65:443`, pool `8`, server name `AchieverGlobalMarkets-Server`, required egress `81.29.145.69`, optional proxy.
- StarwaveFX: Manager login `9904`, `84.201.6.142:443`, pool `4`, server name `StarwaveFX`, **no whitelist today**, `PROXY_ENABLED=false`, still **design** so proxy/whitelist can be enabled later **without a second connector type**.
- `demo\Maxmaster` is **not** the only Achiever group. Enumerate **all** groups the Manager login can see.

From this assignment:

> Config keys from section 56 placeholders only.

That is a hard allow-list. Binders, `appsettings`, user-secrets, and `.env` may **only** read the keys printed in §56. They may not invent `Brokers:Achiever:CollectorBaseUrl`, `MT5_REMOTE_URL`, `MT5_STARWAVEFX_PROXY_HOST`, `MT5_GROUP_*`, or C++ `MT5_PROXY_*` aliases.

---

## 1. Current measured state (honest)

| Surface | Path | Classification |
|---|---|---|
| Architecture sketch | §6 `IMt5BrokerConnector` + “create a broker registry” | specified, not implemented |
| Application ports | `D:\Prop\src\Application` — csproj only, **zero** interfaces | **MISSING** |
| Domain catalog | `D:\Prop\src\Domain\Entities\Broker.cs` (`Code`, `DisplayName`, host/port/login, `Mode`, `PoolSize`, proxy flags, **no secrets**) | **EXISTS_NEEDS_REFACTOR** (mixes catalog + non-secret connection; no `broker_connections` split) |
| Domain raw rows | `Mt5Group` / `Mt5Account` / `Mt5Deal` / `Mt5Position` carry `Guid BrokerId` | **EXISTS** — persistence shape, not connector DTOs |
| Reconstruction | `NormalizedDeal.BrokerId` is a **string** | **EXISTS_NEEDS_REFACTOR** — must accept registry `Code` |
| Draft C# port | `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs` | **DEPRECATED** name + wrong layer + incomplete vs §6 |
| Draft options | `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs` | **EXISTS_NEEDS_REFACTOR / UNSAFE-shape** — defaults `Mode=remote`, `PoolSize=25`, **requires `RemoteUrl`**, invents `ApiKey` / `ProxyType` (none of those are §56 keys) |
| Deterministic ids | `D:\Prop\src\Mt5\Utils\DeterministicGuid.cs` | **EXISTS_AND_GOOD** for catalog `brokers.id` without a `BROKER_ID` env key |
| Worker | `D:\Prop\apps\mt5-worker` template 1s loop; `appsettings.json` is logging only | **MISSING** registry |
| C++ `AppConfig` | `D:\Prop\mt5-sdk\config\app_config.h` — **one** `MT5_SERVER` / `MT5_LOGIN` / `MT5_PASSWORD` | **EXISTS_NEEDS_REFACTOR** — cannot attach StarwaveFX in-process |
| C++ StarwaveFX keys | zero `MT5_STARWAVEFX_*` fields | **MISSING** |
| EF `brokers` | `BrokersConfiguration` maps type `Brokers` (`id/code/name/created_at`); Domain type is `Broker` | **EXISTS_NEEDS_REFACTOR** — type name + column set drift vs A20 |
| Plan-map env | §9 `MT5_GROUP_*` (also C++ `AppConfig`) | **out of this binder** — not in §56; A40: label overlay only |

**Verdict:** Phase 1 (“Achiever connected / StarwaveFX connected”) is **not started** as a C# registry. Drafts in `src/Mt5` must be **replaced or rewritten** to this contract. Do not ship `IBrokerConnector` next to `IMt5BrokerConnector`.

---

## 2. Target composition (no duplicated business logic)

```text
                    §56 env (allow-listed keys only)
                              │
                              ▼
                 Mt5BrokerSlotBinder   ← the ONLY Achiever/StarwaveFX switch
                              │
              ┌───────────────┴───────────────┐
              │  Mt5BrokerConnectionOptions   │  one type, N filled slots
              └───────────────┬───────────────┘
                              │
                 IMt5BrokerConnectorFactory.Create(options)
                              │
              ┌───────────────┴───────────────┐
              │   Mt5ManagerBrokerConnector   │  ONE class
              │   (pump + pool + watchdog)    │
              └───────────────┬───────────────┘
                              │
                    IMt5BrokerRegistry
                     achiever → instance A
                     starwavefx → instance B
                     (future) → instance N
                              │
          ┌───────────────────┼───────────────────┐
          ▼                   ▼                   ▼
   DiscoverGroups      SynchronizeAccounts    HistoricalBackfill
   LiveIngestion       PeriodicReconcile      BrokerHealth
          │                   │                   │
          └───────────────────┴───────────────────┘
                    foreach registry.All
                    (no if (Achiever) / if (StarwaveFX))
```

**Allowed broker-specific code (exactly two places):**

1. `Mt5BrokerSlotBinder` — maps the **asymmetric** §56 names onto `Mt5BrokerConnectionOptions`.
2. Seed / catalog upsert of `brokers.code` (`achiever`, `starwavefx`).

**Forbidden:** `Mt5AchieverConnector` vs `Mt5StarwaveConnector`; duplicated hosted services; `switch (brokerCode)` inside backfill, reconstruction, scoring, or copy.

cTrader keys in the same §56 block (`CTRADER_FIX_*`, `REAL_COPY_EXECUTION_ENABLED`) belong to the **execution** worker. They are **not** registered here.

---

## 3. Identity

Architecture §10: never assume login or ticket IDs are globally unique.

| Kind | Runtime | Persistence (A20) |
|---|---|---|
| Broker code | `BrokerCode` (`achiever`, `starwavefx`) | `brokers.code` UNIQUE, stored lowercase |
| Broker id | `Guid` catalog PK | `brokers.id` |
| Account | `(BrokerCode, login)` | `(broker_id, login)` |
| Deal | `(BrokerCode, deal_ticket)` | `(broker_id, deal_ticket)` |
| Order | `(BrokerCode, order_ticket)` | `(broker_id, order_ticket)` |
| Position | `(BrokerCode, position_id)` | `(broker_id, position_id)` |

**How to get a `Guid` without a §56 `BROKER_ID` key:**

```text
brokers.id(achiever)    = DeterministicGuid.FromString("broker:achiever")
brokers.id(starwavefx)  = DeterministicGuid.FromString("broker:starwavefx")
```

Reuse `D:\Prop\src\Mt5\Utils\DeterministicGuid.cs`. Same input ⇒ same uuid across worker restarts. Do **not** use `MT5_SERVER_NAME` as a primary key (display label; C++ derives it from host if blank).

Every connector DTO is **stamped at the instance boundary** (`options.Code` / `options.CatalogId`). C++ `DealData` / `UserData` have **no** `broker_id` (A04). Do not wait for the SDK to grow the field.

`PriceSource.AchieverMt5Ticks` / `StarwaveMt5Ticks` stay tick-source enums. They are **not** the registry key.

---

## 4. Config — §56 allow-list only

Quoted from architecture §56 (source-MT5 blocks only). Placeholder values are **examples**, not hardcoded production constants — the **names** are binding.

### 4.1 Achiever slot (`BrokerCode = achiever`)

| §56 key | Secret? | Required to connect? | Maps to |
|---|---|---|---|
| `MT5_SERVER` | no | **yes** | `EndpointHost` (`57.128.141.65`) |
| `MT5_PORT` | no | **yes** (default **only if unset**: `443`, because §56 prints `443`) | `EndpointPort` |
| `MT5_LOGIN` | no | **yes** | `ManagerLogin` (`2027`) |
| `MT5_PASSWORD` | **yes** | **yes** | `Password` |
| `MT5_DEFAULT_GROUP` | no | no | `DefaultGroupHint` (`demo\Maxmaster`) — **never a fetch filter** |
| `MT5_MODE` | no | yes (placeholder `local`) | `Mode` |
| `MT5_POOL_SIZE` | no | no (placeholder `8`) | `PoolSize` |
| `MT5_SERVER_NAME` | no | no | `ServerName` (`AchieverGlobalMarkets-Server`) |
| `ACHIEVER_EGRESS_IP` | no | no | `ExpectedEgressIp` (`81.29.145.69`) — **not** a Connect argument |
| `ACHIEVER_PROXY_ENABLED` | no | no | `Proxy.Enabled` |
| `ACHIEVER_PROXY_HOST` | no | required **iff** proxy enabled | `Proxy.Host` |
| `ACHIEVER_PROXY_PORT` | no | required **iff** proxy enabled | `Proxy.Port` |
| `ACHIEVER_PROXY_USERNAME` | **yes** | no | `Proxy.Username` |
| `ACHIEVER_PROXY_PASSWORD` | **yes** | no | `Proxy.Password` |

Display name: **there is no `MT5_DISPLAY_NAME` / `ACHIEVER_DISPLAY_NAME` in §56.** Use architecture name `"Achiever"` (§6 / §7 heading).

### 4.2 StarwaveFX slot (`BrokerCode = starwavefx`)

| §56 key | Secret? | Required to connect? | Maps to |
|---|---|---|---|
| `MT5_STARWAVEFX_DISPLAY_NAME` | no | no (placeholder `StarwaveFX`) | `DisplayName` |
| `MT5_STARWAVEFX_PROVISIONING_ENABLED` | no | no | `ProvisioningEnabled` — **collector ignores** |
| `MT5_STARWAVEFX_MODE` | no | yes (placeholder `local`) | `Mode` |
| `MT5_STARWAVEFX_SERVER` | no | **yes** | `EndpointHost` (`84.201.6.142`) |
| `MT5_STARWAVEFX_PORT` | no | **yes** (placeholder `443`) | `EndpointPort` |
| `MT5_STARWAVEFX_LOGIN` | no | **yes** | `ManagerLogin` (`9904`) |
| `MT5_STARWAVEFX_PASSWORD` | **yes** | **yes** | `Password` |
| `MT5_STARWAVEFX_SERVER_NAME` | no | no | `ServerName` (`StarwaveFX`) |
| `MT5_STARWAVEFX_POOL_SIZE` | no | no (placeholder `4`) | `PoolSize` |
| `MT5_STARWAVEFX_PROXY_ENABLED` | no | no (placeholder `false`) | `Proxy.Enabled` |

There is **no** StarwaveFX proxy host / port / username / password key in §56. The options object still has those **fields** (so the same connector can apply a proxy later) but the binder **must not invent env names**. If `MT5_STARWAVEFX_PROXY_ENABLED=true` and `Proxy.Host` is empty → **fail closed** (misconfiguration), do not guess `ACHIEVER_PROXY_HOST`.

There is **no** `MT5_STARWAVEFX_DEFAULT_GROUP` and **no** StarwaveFX egress-IP key. Leave `DefaultGroupHint` and `ExpectedEgressIp` null.

### 4.3 Explicitly rejected keys (do not bind in this registry)

| Key / pattern | Why rejected |
|---|---|
| `CTRADER_FIX_*`, `REAL_COPY_EXECUTION_ENABLED` | §56 execution block — other worker |
| `MT5_GROUP_*` | §9 catalog, **not** §56; A40 overlay only |
| `MT5_REMOTE_URL`, `MT5_API_KEY`, `MT5_HTTP_*` | C++ remote transport; **not** in §56 |
| `IS_MT5_PROXY_ENABLED`, `MT5_PROXY_*` | C++ Achiever-shaped aliases; §56 uses `ACHIEVER_PROXY_*` |
| `MT5_STARWAVEFX_PROXY_HOST` / `_PORT` / `_USERNAME` / `_PASSWORD` | not printed in §56 |
| `ACHIEVER_PROXY_TYPE` / `MT5_PROXY_TYPE` | not printed in §56 |
| `Brokers:Achiever:CollectorBaseUrl` (A30) | invented; violates this assignment |
| `MT5_ENABLED` / `BROKER_ID` / `ACHIEVER_DISPLAY_NAME` | not printed in §56 |

`appsettings.json` may nest the **same names** for documentation (`"MT5_SERVER": "..."`) but must not introduce a second vocabulary. Prefer process environment / user-secrets for the four secret values. Never commit filled passwords.

### 4.4 Slot enablement (no `ENABLED` key)

A slot is **configured** when all required-to-connect keys are present **and** the password is not the sentinel `<SECRET>` and not empty.

- Both slots configured → registry has two connectors (Phase 1 target).
- One slot configured → registry has one; worker must **log a structured warning** (`broker_missing`) and still run the **same** jobs over `registry.All`. Do not special-case “Achiever-only path”.
- Zero slots → fail host start (nothing to collect).

`MT5_STARWAVEFX_PROVISIONING_ENABLED` does **not** enable or disable collection.

### 4.5 Mode

Placeholders are `MT5_MODE=local` and `MT5_STARWAVEFX_MODE=local`.

| Value | Action |
|---|---|
| `local` (case-insensitive) | Use Manager API adapter (pump + pool). |
| `remote` or anything else | **Fail closed.** §56 has no remote URL / API key. Do not fall back to `Mt5BrokerOptions.RemoteUrl`. |

When architecture later adds remote keys to §56, extend the binder. Until then, `remote` is not a supported product mode for this worker.

### 4.6 Secrets handling

- Load `MT5_PASSWORD`, `MT5_STARWAVEFX_PASSWORD`, `ACHIEVER_PROXY_USERNAME`, `ACHIEVER_PROXY_PASSWORD` from environment / user-secrets only.
- Never write them to `brokers` / `broker_connections` (A20: “Do not store passwords, proxy credentials”).
- Never log them. Do not interpolate `login:password` into Serilog (C++ `SetProxy` builds that string internally — keep it off C# logs).
- `ToString()` / dashboard snapshot / health DTO: mask manager login (`2027` → `2***`), omit password fields entirely (§48, §55).

### 4.7 Options type (one class, both slots)

```csharp
public readonly record struct BrokerCode
{
    public static BrokerCode Achiever { get; } = new("achiever");
    public static BrokerCode StarwaveFx { get; } = new("starwavefx");

    public string Value { get; }
    public BrokerCode(string value) => Value = Normalize(value);

    public static string Normalize(string value) =>
        value.Trim().ToLowerInvariant();
}

public sealed class Mt5BrokerProxyOptions
{
    public bool Enabled { get; init; }
    public string? Host { get; init; }
    public int? Port { get; init; }
    public string? Username { get; init; }   // secret
    public string? Password { get; init; }   // secret
    // No Type field: §56 has no PROXY_TYPE. Implementation default = SOCKS5
    // (MetaQuotes manager proxy). Not an env key.
}

public sealed class Mt5BrokerConnectionOptions
{
    public required BrokerCode Code { get; init; }
    public required Guid CatalogId { get; init; }
    public required string DisplayName { get; init; }
    public required string EndpointHost { get; init; }
    public required int EndpointPort { get; init; }
    public required ulong ManagerLogin { get; init; }
    public required string Password { get; init; }          // secret
    public required string Mode { get; init; }              // "local"
    public required int PoolSize { get; init; }
    public string? ServerName { get; init; }
    public string? DefaultGroupHint { get; init; }          // Achiever only
    public string? ExpectedEgressIp { get; init; }          // Achiever only
    public bool ProvisioningEnabled { get; init; }          // StarwaveFX only; unused by collector
    public Mt5BrokerProxyOptions Proxy { get; init; } = new();

    public string ConnectEndpoint => $"{EndpointHost}:{EndpointPort}";
}
```

`PoolSize` is **manager slot count**, not CPU count (A15). Achiever placeholder `8`, StarwaveFX placeholder `4`. Live usage is `PoolSize + 1` (pool + pump). Do not silently default to `25` (current `Mt5BrokerOptions` bug).

---

## 5. `IMt5BrokerConnector` (Application port)

Architecture §6 sketch, **adjusted to the SDK** as §6 explicitly allows. Place in:

`src/Application/Abstractions/Brokers/IMt5BrokerConnector.cs`

Delete / do not implement `TraderIntelligence.Mt5.Connectors.IBrokerConnector`.

```csharp
public interface IMt5BrokerConnector
{
    BrokerCode Code { get; }
    Guid CatalogId { get; }
    string DisplayName { get; }

    Mt5BrokerConnectionState State { get; }
    bool IsConnected { get; }
    string LastError { get; }                 // IMT5Client::GetLastError; never secrets
    bool PumpEventsAvailable { get; }         // false if Connect fell back to no-pump

    Task ConnectAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);

    // §7 / §9 / A39: Manager-visible set. MUST NOT read DefaultGroupHint or MT5_GROUP_*.
    Task<IReadOnlyList<Mt5GroupInfo>> GetGroupsAsync(CancellationToken ct);

    Task<IReadOnlyList<ulong>> GetGroupLoginsAsync(string groupName, CancellationToken ct);

    Task<Mt5UserInfo?> GetUserAsync(ulong login, CancellationToken ct);
    Task<Mt5AccountSnapshotInfo?> GetAccountAsync(ulong login, CancellationToken ct);

    // COMPOSE: all groups → logins → GetUser + GetAccount. Stamp Code/CatalogId.
    Task<IReadOnlyList<Mt5AccountInfo>> GetAccountsAsync(CancellationToken ct);

    // Complete-history contract: follow every page/cursor for [from,to] or FAIL.
    // Merge GetRecentDeals. Empty + success = no deals. Failure ≠ empty list.
    Task<IReadOnlyList<Mt5DealInfo>> GetDealsAsync(
        ulong login, DateTimeOffset fromInclusive, DateTimeOffset toExclusive, CancellationToken ct);

    Task<IReadOnlyList<Mt5OrderInfo>> GetOrdersAsync(ulong login, CancellationToken ct);
    Task<IReadOnlyList<Mt5PositionInfo>> GetPositionsAsync(ulong login, CancellationToken ct);

    Task<IReadOnlyList<Mt5SymbolInfo>> GetSymbolsAsync(CancellationToken ct);
    Task<Mt5TickInfo?> GetTickLastAsync(string sourceSymbol, CancellationToken ct);

    // Must not silently substitute host clock (A04 / mt5_time_window).
    Task<Mt5ServerTime> GetServerTimeAsync(CancellationToken ct);

    IAsyncEnumerable<Mt5SourceEvent> SubscribeAsync(CancellationToken ct);

    ValueTask<bool> TrySubscribeTicksAsync(IMt5TickSink sink, CancellationToken ct);
}
```

`IMt5BrokerConnector` starts from architecture §6 and is adjusted to the collector (A04 / A12):

| §6 sketch | Adjustment | SDK primitive |
|---|---|---|
| `ConnectAsync` / `DisconnectAsync` | keep; **not** on `IMT5Client` | `MT5Manager::Connect` / `Disconnect` |
| — | add `IsConnected`, `LastError`, `PumpEventsAvailable` | `IsConnected`, `GetLastError`; no-pump fallback `mt5_manager.cpp:114–135` |
| `GetGroupsAsync` | compose details, **no plan filter** | `GetAllGroups` + `GetGroupDetails` |
| `GetAccountsAsync(...)` | compose; add `GetGroupLoginsAsync` / `GetUserAsync` / `GetAccountAsync` | no single C++ `GetAccounts` |
| `GetDealsAsync(...)` | **require** login + window; fail closed; merge recent ring | `GetDeals` + `GetRecentDeals` |
| `GetOrdersAsync` / `GetPositionsAsync` | **require** login | `GetOrders` (local only; remote default `false`), `GetPositions` |
| `SubscribeAsync` | keep; **not** the only deal path (no `PUMP_MODE_DEALS`) | `GetEventQueue` |
| — | symbols / last tick / server time | `GetSymbol*`, `GetTickLast`, `GetServerTime` |
| — | optional ticks | `SubscribeTicks` (fail-closed) |

### 5.1 Connector DTOs (not EF entities)

Do **not** return `Domain.Entities.Mt5Group` / `Mt5Deal` from the port. Those rows have persistence `Id` / `IngestionEventId`. Connector types live in `src/Application/Abstractions/Brokers/Models/` and always carry `BrokerCode` + `CatalogId`.

Minimum fields (from C++ `mt5_types.h`, A04 §10):

| DTO | Fields |
|---|---|
| `Mt5GroupInfo` | name, currency, currency_digits, company, margin_call, margin_stop_out, connections_allowed |
| `Mt5UserInfo` | login, name, email, group, leverage, country, city, phone, registration, last_access, rights |
| `Mt5AccountSnapshotInfo` | login, balance, credit, equity, margin, margin_free, margin_level, profit, floating, storage |
| `Mt5AccountInfo` | user + snapshot + group name |
| `Mt5DealInfo` | ticket, login, **order**, **position**, symbol, action, entry, volume (native integer), price, profit, commission, storage, time, comment |
| `Mt5OrderInfo` | ticket, login, symbol, type, state, volume, prices, sl/tp, time_setup, comment |
| `Mt5PositionInfo` | ticket, login, symbol, action, volume, prices, sl/tp, profit, storage, times, comment |
| `Mt5SymbolInfo` | symbol, path, description, digits, contract_size, volume min/max/step, trade_mode |
| `Mt5TickInfo` | symbol, bid, ask, last, volume, time, time_msc, flags |
| `Mt5SourceEvent` | type, login, payload (deal/order/position/user), server time if known |
| `Mt5ServerTime` | `UtcTimestamp`, `UsedHostFallback` (must be **false** for checkpoint math) |

Volume stays **MT5 native integer units** (hundredths of lots on positions). Do not convert to cTrader `OrderQty` here (§1.10).

### 5.2 Failure contract

| Situation | Connector must |
|---|---|
| Not connected / borrow timeout | throw `Mt5BrokerUnavailableException` (or `Result` fail) — **not** empty collections |
| `GetDeals` incomplete (no page follow-through) | fail — callers must **not** advance `sync_checkpoints` (A04 complete-history contract) |
| `GetOrders` unsupported (if a future remote mode appears) | fail closed, not empty |
| `GetGroupDetails` empty + unsupported | fail — do not persist “broker has zero groups” |
| `GetServerTime` host fallback | set `UsedHostFallback=true`; backfill **must not** persist that as checkpoint time |
| Connect IP-blocked | `LastError` may mention `ExpectedEgressIp` for Achiever; keep retrying (§62) |
| Pump connect fails, request-only succeeds | `IsConnected=true`, `PumpEventsAvailable=false`, health = **degraded / stale-source** — not “live” |

Empty + success is allowed only when the broker **authoritatively** has no rows.

### 5.3 Explicitly **not** on this interface

These exist on C++ `IMT5Client` because that SDK was extracted from a dealer/provisioning backend (A04 §5.8). Putting them on the collector invites mutating ~5,000 source accounts.

| C++ | Why OUT |
|---|---|
| `CreateUser` / `DeleteUser` / `UpdateUser*` | provisioning |
| `ChangePassword` / `CheckPassword` | secrets |
| `DealerBalance` / `Deposit` / `Withdraw` | moves money |
| `DealerSendOrder` / `SendTrade` | execution is cTrader FIX |
| `CacheExecutedDeal` | only valid after **our** `SendTrade` |
| `GetNewsCalendarItems` / `GetCalendarEvents` | not a §11 table |

If a later admin tool needs them, add a **separate** `IMt5AdminClient`. The mt5-worker must not take that dependency. `ProvisioningEnabled` is stored on options only so a future admin host can read the same slot binder — the collector never branches on it.

`DefaultGroupHint` is also unused by `GetGroupsAsync` / `GetAccountsAsync` (§7 “`demo\Maxmaster` is not the only group”, §9, A39, A40).

### 5.4 Subscribe vs deals (do not get this wrong)

There is **no** `PUMP_MODE_DEALS`. `OnDealAdd` is expected silent (A07 / A12). Therefore:

```text
SubscribeAsync          → users / orders / positions / symbols (if pump up)
GetDealsAsync poll      → live deals + lag >40s DealRequest hole
GetRecentDeals merge    → pump ring if anything ever arrives
Periodic reconciliation → mandatory third leg (§12)
```

`SubscribeAsync` must **only enqueue** on the pump/SSE thread. Persist + outbox run on worker threads (§12, §72.6).

---

## 6. `IMt5BrokerRegistry` (Application port)

`src/Application/Abstractions/Brokers/IMt5BrokerRegistry.cs`

```csharp
public interface IMt5BrokerRegistry
{
    IReadOnlyList<IMt5BrokerConnector> All { get; }

    bool TryGet(BrokerCode code, out IMt5BrokerConnector connector);
    IMt5BrokerConnector GetRequired(BrokerCode code);

    IReadOnlyList<Mt5BrokerHealthSnapshot> Snapshot();
}

public sealed class Mt5BrokerHealthSnapshot
{
    public required BrokerCode Code { get; init; }
    public required Guid CatalogId { get; init; }
    public required string DisplayName { get; init; }
    public required string Server { get; init; }          // host:port, not secret
    public required string MaskedManagerLogin { get; init; }
    public required string? ServerName { get; init; }
    public required string Mode { get; init; }
    public required int PoolSize { get; init; }
    public required int PoolInUse { get; init; }
    public required bool ProxyEnabled { get; init; }      // never host/user/pass
    public required string? ExpectedEgressIp { get; init; }
    public required bool IsConnected { get; init; }
    public required bool PumpEventsAvailable { get; init; }
    public required string? LastError { get; init; }
    public required int ReconnectCount { get; init; }
    public required DateTimeOffset? LastEventUtc { get; init; }
    public required DateTimeOffset? LastHistorySyncUtc { get; init; }
    public required DateTimeOffset? SourceStaleSinceUtc { get; init; }
}
```

Dashboard §48 fields map 1:1 onto `Mt5BrokerHealthSnapshot` plus counts from persistence (`mt5_groups`, `mt5_accounts`, ingest rate). **No secret values.**

Registry implementation is an in-memory dictionary built at host start:

```text
IReadOnlyDictionary<BrokerCode, IMt5BrokerConnector>
```

It is **not** a second source of connection secrets. PostgreSQL `brokers` / `broker_connections` store the **non-secret catalog** (A20) so the API can list brokers when the worker is down. Secrets stay in the process that called `ConnectAsync`.

---

## 7. Single implementation (`src/Mt5`)

§66: `/src/Mt5` is the adapter. **One** class:

`src/Mt5/Connectors/Mt5ManagerBrokerConnector.cs`

Internal layout **per instance** (A15):

```text
Mt5ManagerBrokerConnector (Code = achiever | starwavefx)
  ├─ pump  MT5Manager + MT5Watchdog     (events)
  └─ pool  MT5Pool[PoolSize]            (GetDeals / GetUser / GetAccount / GetGroupLogins)
```

Watchdog (A15 contract, apply **per broker**): check ~30s; backoff `5 → 10 → 20 → 40 → 60`s; reset on success; metric `mt5_reconnects{broker_code}`.

Pool: pass `options.PoolSize` into `Initialize` (A15: C++ currently **loads** `MT5_POOL_SIZE` but does **not** pass it — do not copy that bug). Bound fan-out to pool size. Never open a Manager connection per login.

Connect sequence per instance:

```text
if Mode != local → throw (no §56 remote keys)
if Proxy.Enabled → SetProxy(SOCKS5, Host, Port, Username, Password)   // never log
Connect(ConnectEndpoint, ManagerLogin, Password, default pump)
if pump fail → Connect(..., pumpMode=0) and mark PumpEventsAvailable=false
record LastError; increment mt5_reconnects on retry
```

Default pump mask in C++ is `USERS | ORDERS | POSITIONS | SYMBOLS` (no GROUPS, no DEALS). Group discovery uses **request** APIs (`GroupTotal` / `GroupNext`), which work without `PUMP_MODE_GROUPS` (A39).

Transport: C# talks to the existing C++ Manager either by:

- hosting the already-built local SDK in-process / sidecar **without new config keys**, or
- P/Invoke / named pipe that still uses `MT5_SERVER` + `MT5_PORT` from §56.

Do **not** require `RemoteUrl`. The current `Mt5BrokerOptions.RemoteUrl` `[Required]` is **wrong** for this design.

Factory:

```csharp
public interface IMt5BrokerConnectorFactory
{
    IMt5BrokerConnector Create(Mt5BrokerConnectionOptions options);
}
```

`Create` always returns `Mt5ManagerBrokerConnector`. Adding a third broker later is `Create(newOptions)`, not a new class.

---

## 8. Slot binder (the only name switch)

`src/Mt5/Configuration/Mt5BrokerSlotBinder.cs`

```text
Bind():
  slots = []
  if Achiever required keys present and password real → slots += MapAchiever()
  if StarwaveFX required keys present and password real → slots += MapStarwaveFx()
  return slots
```

Mapping is **mechanical**. Example (names only; do not invent extras):

```text
Achiever
  Code            = achiever
  CatalogId       = DeterministicGuid("broker:achiever")
  DisplayName     = "Achiever"                          // not an env key
  EndpointHost    = MT5_SERVER
  EndpointPort    = MT5_PORT
  ManagerLogin    = MT5_LOGIN
  Password        = MT5_PASSWORD
  Mode            = MT5_MODE
  PoolSize        = MT5_POOL_SIZE
  ServerName      = MT5_SERVER_NAME
  DefaultGroupHint= MT5_DEFAULT_GROUP
  ExpectedEgressIp= ACHIEVER_EGRESS_IP
  Proxy.Enabled   = ACHIEVER_PROXY_ENABLED
  Proxy.Host      = ACHIEVER_PROXY_HOST
  Proxy.Port      = ACHIEVER_PROXY_PORT
  Proxy.Username  = ACHIEVER_PROXY_USERNAME
  Proxy.Password  = ACHIEVER_PROXY_PASSWORD
  ProvisioningEnabled = false

StarwaveFX
  Code            = starwavefx
  CatalogId       = DeterministicGuid("broker:starwavefx")
  DisplayName     = MT5_STARWAVEFX_DISPLAY_NAME
  EndpointHost    = MT5_STARWAVEFX_SERVER
  EndpointPort    = MT5_STARWAVEFX_PORT
  ManagerLogin    = MT5_STARWAVEFX_LOGIN
  Password        = MT5_STARWAVEFX_PASSWORD
  Mode            = MT5_STARWAVEFX_MODE
  PoolSize        = MT5_STARWAVEFX_POOL_SIZE
  ServerName      = MT5_STARWAVEFX_SERVER_NAME
  DefaultGroupHint= null
  ExpectedEgressIp= null
  Proxy.Enabled   = MT5_STARWAVEFX_PROXY_ENABLED
  Proxy.Host/Port/Username/Password = unset (no §56 keys)
  ProvisioningEnabled = MT5_STARWAVEFX_PROVISIONING_ENABLED
```

Boolean parse: `true` / `1` / `yes` (case-insensitive) → true; else false.  
`<SECRET>` or empty password → slot **not** configured (fail that slot, do not connect with a literal placeholder).

Worker DI:

```csharp
services.AddSingleton<IMt5BrokerConnectorFactory, Mt5ManagerBrokerConnectorFactory>();
services.AddSingleton<IMt5BrokerRegistry>(sp =>
{
    var factory = sp.GetRequiredService<IMt5BrokerConnectorFactory>();
    var slots = Mt5BrokerSlotBinder.BindFromEnvironment();
    var connectors = slots.Select(factory.Create).ToArray();
    return new Mt5BrokerRegistry(connectors);
});
```

---

## 9. Application use-cases (once, for every broker)

These services take `IMt5BrokerRegistry` (or a single `IMt5BrokerConnector` when invoked per item). They **must not** import `Mt5BrokerSlotBinder`.

Startup / resync (§7):

```text
host start
  → foreach connector in registry.All: ConnectAsync
  → Enumerate groups          GetGroupsAsync
  → Upsert mt5_groups         (broker_id, group_name)
  → Enumerate accounts        GetGroupLogins + GetUser + GetAccount
  → Associate broker + group  (broker_id, login)
  → Sync history              GetDeals + checkpoints
  → SubscribeAsync + deal poll
  → Periodic reconciliation
```

Jobs (A07 names; exact class names may vary) are **not** cloned per broker:

| Job | Loop |
|---|---|
| Connection supervisor | `foreach All` connect / watchdog / `mt5_connected{broker_code}` |
| Group discovery | `foreach All` `GetGroupsAsync` → upsert |
| Account sync | `foreach All` compose accounts → upsert + snapshots |
| Historical backfill | `foreach All` × each `(broker, login)` checkpoint |
| Live ingestion | `foreach All` `SubscribeAsync` + deal poll |
| Reconciliation | `foreach All` positions / missed deals |
| Outbox drain | shared, already keyed by `broker_id` in payload |

Group discovery law (A39 / A40): **ALL = Manager `GroupNext` set**. Never intersect with `DefaultGroupHint` or `plan_group_mappings`. Plan labels attach **after** upsert.

Identity law: upsert keys always include `CatalogId`. Achiever login `9904` and StarwaveFX login `9904` are **two rows**.

---

## 10. Persistence mapping (catalog vs runtime)

A20 tables this design owns:

| Table | Writer | Notes |
|---|---|---|
| `brokers` | worker on start | `id` = `CatalogId`, `code` unique, `name` = `DisplayName` |
| `broker_connections` | worker on start / reconnect | non-secret: host, port, manager login **number**, pool, mode, `proxy_enabled`, pump/no-pump |
| `mt5_groups` | group job | UNIQUE `(broker_id, group_name)` |
| `mt5_accounts` | account job | UNIQUE `(broker_id, login)` |
| `mt5_account_snapshots` | account job | append |
| `mt5_deals` / `mt5_orders` / `mt5_positions_current` | ingest jobs | §10 compound uniques |
| `sync_checkpoints` | backfill / live | **per broker** (and typically per login / stream). Two brokers must not share a deals cursor (A20) |

Domain `Broker` currently inlines connection fields. Prefer: keep `Broker` as catalog (`Id`, `Code`, `DisplayName`); persist connection snapshot on `broker_connections`. Until that split, **still never persist passwords**. Existing `BrokersConfiguration` targeting type `Brokers` must be aligned to `Broker` when implementation is authorized — out of scope for this file (no product edits).

---

## 11. Metrics, logs, health

§58 labels: add `broker_code` (or `broker_id`) on every MT5 series.

```text
mt5_connected{broker_code}
mt5_reconnects{broker_code}
mt5_events_total{broker_code,type}
mt5_deals_total{broker_code}
mt5_duplicate_deals_total{broker_code}
mt5_backfill_lag{broker_code}
mt5_pool_in_use{broker_code}
mt5_pool_size{broker_code}
mt5_source_stale{broker_code}
```

§57: every log line that can include `broker_code`, `login`, tickets, `correlation_id`. Never passwords / proxy credentials / raw `ACHIEVER_PROXY_USERNAME`.

§62: MT5 unavailable → do not invent trades; continue retrying; expose stale-source; do not open copied positions from stale source. A dead StarwaveFX must **not** stop Achiever ingestion (supervisor retries per connector).

§48 Brokers page reads `Snapshot()` + DB counts. Masked login only.

---

## 12. Adding a third MT5 broker later

Without touching backfill / reconstruct / score / copy:

1. Architecture adds a **new §56-style block** (new prefixed keys). Until those names exist, do not invent them.
2. Add one `MapFutureBroker()` branch in `Mt5BrokerSlotBinder`.
3. Add `BrokerCode` static + deterministic `CatalogId`.
4. `factory.Create(options)` — same class.
5. Registry.All grows by one. Jobs already loop.

Do **not** add `src/Mt5/Achiever` or `src/Mt5/StarwaveFx` projects.

---

## 13. Existing draft cleanup (when implementation is authorized)

| Current | Action |
|---|---|
| `src/Mt5/Connectors/IBrokerConnector.cs` | **Delete.** Application owns `IMt5BrokerConnector`. Missing `GetOrdersAsync`; events omit orders/users; lives in the wrong layer. |
| `src/Mt5/Configuration/Mt5BrokerOptions.cs` | **Replace** with `Mt5BrokerConnectionOptions` + slot binder. Remove `RemoteUrl` / `ApiKey` / `ProxyType` / `PoolSize=25` / `Mode=remote`. |
| `src/Mt5/Utils/DeterministicGuid.cs` | **Keep.** Use for `brokers.id`. |
| C++ single `AppConfig` | Stay a **native adapter** config inside one connector instance. The C# registry supplies per-instance values. Do not add a second C++ process *as the only way* to get two brokers — two instances in one worker is the §6 registry. A second OS process is allowed only if it still consumes the same §56 keys and the same C# jobs (not a forked codebase). |

A30’s `Brokers:Achiever:CollectorBaseUrl=http://127.0.0.1:9101` is **superseded** by this assignment: no keys beyond §56.

---

## 14. Tests (must exist before claiming Phase 1)

| Test | Proves |
|---|---|
| Binder maps every §56 Achiever key and no others | allow-list |
| Binder maps every §56 StarwaveFX key and no others | allow-list + no invented proxy host |
| `<SECRET>` / empty password → slot skipped | no placeholder connect |
| `MT5_MODE=remote` → fail closed | no `RemoteUrl` invention |
| `PROXY_ENABLED=true` on StarwaveFX without host → fail closed | later-proxy design without new keys |
| Two fake connectors, **one** `DiscoverGroupsService` | no duplicated business logic |
| Same login `9904` on both brokers → two account rows | §10 |
| `GetGroupsAsync` result **not** filtered by `DefaultGroupHint` or a planted `MT5_GROUP_2STEP_DEMO` | §7 / §9 / A40 |
| Incomplete `GetDeals` → exception / fail, checkpoint unchanged | A04 contract |
| Snapshot JSON contains no password / proxy user / proxy password | §55 / §48 |
| Achiever proxy enabled does not appear in log text | §7 “Never log proxy credentials” |
| Registry.All empty → host fails fast | no silent no-op worker |

Fakes implement `IMt5BrokerConnector`. Production `Mt5ManagerBrokerConnector` is not required for Application unit tests.

---

## 15. File plan (implementation later — not done in this task)

```text
src/Application/Abstractions/Brokers/IMt5BrokerConnector.cs
src/Application/Abstractions/Brokers/IMt5BrokerRegistry.cs
src/Application/Abstractions/Brokers/IMt5BrokerConnectorFactory.cs
src/Application/Abstractions/Brokers/Models/*.cs
src/Application/Brokers/DiscoverGroupsService.cs
src/Application/Brokers/SynchronizeAccountsService.cs
src/Application/Brokers/BrokerHealthService.cs

src/Mt5/Configuration/Mt5BrokerConnectionOptions.cs
src/Mt5/Configuration/Mt5BrokerSlotBinder.cs
src/Mt5/Connectors/Mt5ManagerBrokerConnector.cs
src/Mt5/Registry/Mt5BrokerRegistry.cs
src/Mt5/DependencyInjection.cs
src/Mt5/Utils/DeterministicGuid.cs          # keep

apps/mt5-worker/Program.cs                  # register registry + jobs
apps/mt5-worker/Hosting/*.cs
```

No product source was created or edited for this report.

---

## 16. Acceptance (this design is DONE when implementation later proves)

Phase 1 §67, restricted to the registry:

```text
Achiever connected          — ConnectAsync on Code=achiever using only MT5_* / ACHIEVER_* keys
StarwaveFX connected        — ConnectAsync on Code=starwavefx using only MT5_STARWAVEFX_* keys
all groups discovered       — GetGroupsAsync, not DefaultGroupHint / MT5_GROUP_*
one connector class         — no Achiever/StarwaveFX fork
one job loop                — jobs iterate registry.All
secrets not logged/stored   — passwords + ACHIEVER_PROXY_* auth
broker_id on every raw row  — CatalogId stamped at the boundary
```

Until those are measured in a running worker, report the gap as **open**. A green `Worker running at:` log is not “Achiever connected”.

---

## 17. Risk list (specific to this component)

1. **Asymmetric env names** — Achiever uses unprefixed `MT5_*`; StarwaveFX uses `MT5_STARWAVEFX_*`. A “symmetric” `Mt5:Brokers:0:Server` redesign would invent keys. Keep the binder ugly and the options object clean.
2. **Copying `IBrokerConnector` / `Mt5BrokerOptions` forward** — ships `remote` + `RemoteUrl` + pool `25`, which contradicts §56.
3. **One C++ `AppConfig`** — if the adapter process still reads global `MT5_SERVER` only, the second instance silently becomes Achiever. Each native instance must receive **injected** host/login/password/pool from C# options.
4. **Treating `SubscribeAsync` as live deals** — no `PUMP_MODE_DEALS`.
5. **Using `MT5_DEFAULT_GROUP` or `MT5_STARWAVEFX_PROVISIONING_ENABLED` as enable/filter flags.**
6. **Sharing checkpoints or unique keys across brokers.**
7. **Inventing StarwaveFX proxy host keys** when `PROXY_ENABLED` flips to true. Fail closed until architecture prints the keys.
8. **Logging `SetProxy` material** or putting secrets on `Domain.Broker`.
9. **`GetServerTime` host fallback** written into `sync_checkpoints`.
10. **Duplicating hosted services** per broker “for isolation” — isolation is **per connector instance**, not per forked job type.

---

## 18. Cross-links

| Need | Where specified |
|---|---|
| Port vs adapter layering | A02 B1–B2; §66 |
| SDK method coverage / OUT verbs | A04, A12 |
| Pool size + watchdog numbers | A15 |
| Group enumeration primitive | A39 |
| Plan map must not filter | A40, §9 |
| Table identities | A20 `brokers`, `broker_connections` |
| Worker job list | A07 §5 |
| Phase 1 exit | §67, A28 |

This document is the **binding design** for `IMt5BrokerConnector` + the registry. Config surface is the §56 source-MT5 key list and nothing else.
