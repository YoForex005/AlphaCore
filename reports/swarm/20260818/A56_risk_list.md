# A56 — Risk List (Architecture §73.D)

**Artifact:** `D:\Prop\reports\swarm\20260818\A56_risk_list.md`  
**Date:** 2026-08-18  
**Agent:** A56 (senior engineer)  
**Authority:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §73.D (and the named sections cited below)  
**Product source modified:** **none**  
**Status:** Phase 0 risk register. Binding input to implementation sequence. Not a license to start live FIX or live copy.

Required topics (verbatim §73.D):

```text
MT5 SDK constraints
Windows/native DLL constraints
source tick-data availability
cTrader FIX credential/header ambiguity
symbol/quantity mapping
live-account safety
```

This file is the on-disk answer to those six items. Chat is not storage.

---

## 0. Verdict (measured)

The product is **not** in a state where live copy, live `NewOrderSingle`, or “exact MFE/MAE” can be claimed.

| Area | Current measured state | Dominant risk class |
|---|---|---|
| MT5 local SDK (`mt5-sdk`) | Real Windows Manager wrapper | **P0/P1 SDK contract holes** if treated as a complete collector |
| C# `apps/mt5-worker` | Template `Task.Delay(1000)` loop | Ingestion not started; **false PASS** if a green build is treated as “Achiever connected” |
| C# `apps/fix-worker` | Same template loop | **P0** if anyone points it at account `1369850` without the gates below |
| `src/Fix.CTrader` | Options stub + **wrong** NuGet (`QuickFix.Net` 1.8.0); no sessions | Header defaults already pick a side of the `cServer`/`CSERVER` trap |
| Domain mapping / volume | `CanonicalInstrument`, `SourceSymbolMapping`, `VolumeConverter` only | **P0 100× / lots=qty** if those types are treated as a sizing engine |
| Tick tape / MFE/MAE | Live transport optional; **no durable source tape** | Fabrication risk if features are filled from deals or destination quotes |
| Live account `1369850` | Real Pepperstone / cServer money | **P0** until flags, lease, persist-before-send, and recon exist |

Honest classification for the *system* (architecture §73.B language):

| Slice | Class |
|---|---|
| Native Manager transport | `EXISTS_AND_GOOD` as a Windows SDK adapter; `EXISTS_NEEDS_REFACTOR` as this product’s collector |
| C# workers | `EXISTS_NEEDS_REFACTOR` (hosts) / `MISSING` (jobs) |
| FIX adapter | `MISSING` (engine/sessions) + `UNSAFE` defaults if wired to the live host |
| Sizing / mapping | `MISSING` destination convention; `EXISTS_AND_GOOD` only for MT5 native→lots scale |
| Tick warehouse | `MISSING` |
| Live-execution safety controls | `MISSING` (enums/options exist; no enforcement path) |

Do **not** treat this risk list as a go-live waiver. Do **not** enable `REAL_COPY_EXECUTION_ENABLED` to “see if it works.”

---

## 0.1 Severity scale (used below)

| Sev | Meaning |
|---|---|
| **P0** | Can lose real money, double-send, 100× size, invent source trades, or silently corrupt the raw ledger. Block live send. Prefer false negative. |
| **P1** | Will become a live defect the first time the path is used (data hole, header reject, incomplete history, stale source treated as live). Must be designed before the owning phase exits. |
| **P2** | Operational / completeness. Breaks scale, observability, or multi-broker identity if ignored. |
| **P3** | Hygiene, naming drift, stale comments. Dangerous only if copied as law. |

---

## 1. MT5 SDK constraints

**Sources:** `imt5_client.h`, `mt5_manager.{h,cpp}`, `mt5_pool.{h,cpp}`, `mt5_watchdog.{h,cpp}`, `mt5_types.h`, `vendor/MetaTrader5SDK/Include/MT5APIManager.h`, architecture §§7–14, §62, §72.6–7. Swarm: A12, A13, A14, A15, A16, A07.

The Manager API is the **correct** source integration (§1 / §6). It is **not** a complete event bus and it is **not** a trade-copy engine. The binding constraints:

### 1.1 There is no `PUMP_MODE_DEALS` — **P0** for “live deals persisted”

`IMTManagerAPI::EnPumpModes` (`MT5APIManager.h` 125–144) has USERS, ORDERS, POSITIONS, SYMBOLS, GROUPS, … There is **no** `PUMP_MODE_DEALS`.

Default local connect (`mt5_manager.cpp`) pumps `USERS | ORDERS | POSITIONS | SYMBOLS` only. `DealSubscribe` is still registered, but product comments (and A12/A14) treat `OnDealAdd` / `OnDealUpdate` as **likely silent**.

**Risk:** a collector designed only around deal callbacks stores **zero** live deals and still looks “connected.”

**Required behaviour:** live deal capture = **paged/windowed `GetDeals` + order/position pump + reconciliation**. Never wait for `OnDealAdd`. `CacheExecutedDeal` / `SendTrade` is the **old prop-firm execution** path; Trader Intelligence is a **read/collector** on source brokers and must not populate 5,000 accounts from its own dealer fills.

### 1.2 Local `GetDeals` does not page — **P1** (silent incomplete history)

Interface contract (`imt5_client.h`): follow every page/cursor for `[from,to]` or return `false`. Callers treat `false` as `dependency_unavailable` and **must not** pass/fail or advance a checkpoint.

Local `MT5Manager::GetDeals` issues **one** `DealRequest(login, from, to)` and returns `true` if that call succeeds. Native `DealRequestPage` exists and is **unused** (A14). `MT5Pool` sessions do the same one-shot. Only `MT5HttpClient` follows cursors / `has_more` and fails closed on partial history.

**Risk:** a wide window or busy login is silently truncated; a checkpoint advances; the gap is permanent. Prefer `false` / no checkpoint over a short page treated as complete.

### 1.3 Boolean semantics are not uniform — **P1**

| Call | `false` means |
|---|---|
| `GetDeals` | History incomplete / dependency unavailable — **not** “no deals” |
| `GetRecentDeals` / `GetOrders` / `SubscribeTicks` | Unsupported on this transport |
| `GetAllGroups` / `GetGroupDetails` | Local impl still returns **`true` after skipped `GroupNext` failures** |
| `GetUserLogins` / `GetGroupLogins` | Fail-closed; empty group with null pointer looks like API failure |
| `GroupTotal` / `IsConnected` | Disconnected `GroupTotal()==0` looks like “server has no groups” |

**Risk:** empty + `false` collapsed to “nothing to do” invents a clean book. Empty + `true` after a partial group walk invents a complete universe.

### 1.4 Deal history index lags; recent-deals ring is not history — **P1**

Comments in `imt5_client.h` / `mt5_manager.cpp`: broker `DealRequest` index can lag **>40 s on demo**. The in-process ring (`kRecentDealCap = 4096`) plus `CacheExecutedDeal` exists for **just-executed dealer trades**, not for thousands of third-party source accounts.

**Risk:** scoring or copy on “no recent deals” immediately after a source fill is a false negative; using the ring as the ledger is a false positive / overflow drop.

### 1.5 Pump-thread contract — **P0** if violated

`SubscribeTicks` / sinks: `OnTick` (and other sinks) run on the **SDK pump thread**. They must **only enqueue** and return. They must not: block, send sockets, hit the DB, send FIX, or re-enter the client under `m_mutex`.

Architecture §72.6–7: callbacks lightweight; persist before async work.

**Risk:** a DB write or `NewOrderSingle` on a sink stalls the Manager pump, drops events, or deadlocks the process. An MT5 callback that sends FIX is an explicit architecture violation (§32) and a live-money defect.

### 1.6 Connect can silently fall back to no-pump — **P1**

`MT5Manager::Connect`: pump connect failure retries `mode=0` (request-only). `GetDeals` still works; sinks are **not** live. IP not whitelisted (`MT_RET_AUTH_MANAGER_IPBLOCK` = 1012) is a typical cause.

**Risk:** `IsConnected()==true` with no live events is reported as healthy. Must persist pump vs no-pump on `broker_connections` and expose **stale-source**. Copy from a no-pump source without a deal-poll is `SOURCE_STALE`.

### 1.7 Default pump omits `PUMP_MODE_GROUPS` — **P2**

Group lists are a `GroupTotal` / `GroupNext` config-cache walk. No `GroupSubscribe`, no `GroupRequest` refresh.

**Risk:** new groups after connect are invisible until a manual resync. Plan-map (`MT5_GROUP_*` / `getMt5Group`) must **not** substitute for discovery (§9). Using the old account-creation helper as the universe is `UNSAFE`.

### 1.8 One mutex serializes almost every local call — **P2**

`MT5Manager::m_mutex` is held across network `DealRequest` / `UserRequest` / dealer send. Concurrent history of ~5,000 logins on the **pump** object will stall ticks and sinks.

The intended design is **N request sessions in `MT5Pool`** (no pump) **plus one** pump `MT5Manager`. In this tree `MT5_POOL_SIZE` is loaded into `AppConfig` but **no call site** passes it to `MT5Pool::Initialize` (A15). Watchdog and `healthCheck` have **no in-tree scheduler**.

**Risk:** production “pool of 8” is imaginary unless a consumer wires it. Tick poll (`Borrow(200)`) skips when the pool is saturated. Health-check drain can delay borrows by `K × 5 s`.

### 1.9 Write-side Manager APIs are on the same interface — **P0** if the collector calls them

`IMT5Client` is a prop-firm control plane surface: `CreateUser`, `DeleteUser`, `DealerBalance`, `Deposit`, `Withdraw`, `DealerSendOrder`, `SendTrade`, `UpdateUserRights`. Defaults on remote/HTTP fail closed for some mutate paths; **local `MT5Manager` implements them**.

**Risk:** a “just fill in Worker.cs” collector that reuses `MT5Manager` as a god object can **create accounts, move money, or place source-broker trades**. Phase 1 worker must be **read-only**. Those methods are out of scope for Trader Intelligence ingestion.

### 1.10 Transport split is not feature-complete — **P1**

| Capability | Local `MT5Manager` | Remote `MT5HttpClient` |
|---|---|---|
| `GetGroupDetails` | yes (cache walk) | **false** (unsupported) |
| `SubscribeTicks` | `TickSubscribe` | **false** → poll last tick |
| `GetOrders` | cache then request | **false** |
| `GetDeals` paging | **one shot** | cursor / page, fail-closed |
| `SendTrade` modify/cancel | dealer path | often 501 / unsupported |

`MT5_MODE=remote` without a real Windows sidecar that implements the HTTP contract is a stub. There is **no** product sidecar host under `apps/`.

### 1.11 Encoding / identity / time — **P1–P2**

- Group **names** are `std::string` (UTF-8); group **selectors** on `GetUserLogins` / `GetGroupLogins` are `std::wstring`. Mix-ups drop whole groups.
- C++ DTOs have `server_key` on the ledger, **not** architecture `broker_id`. Achiever login `N` and StarwaveFX login `N` are different accounts. Ticket uniqueness is **compound** (`broker_id + deal_ticket`). Collapsing them is ledger corruption (**P0**).
- `AppConfig` is **single-broker** (`MT5_SERVER` / `MT5_LOGIN`). Architecture requires Achiever **and** StarwaveFX. A second broker is an outer-product concern, not a second copy of the SDK.
- History windows must use **`GetServerTime()`**, not host clock (`mt5_time_window`). Host-local `DateTime.UtcNow` ranges miss or double-count deals around TZ/skew.
- `OrderData.volume` is **initial** volume (`VolumeInitial`), not remaining. Using it as open qty is a mapping defect.
- `DealData.action` / `entry` are raw SDK enums (0–20 / 0–3). Incomplete comments (`0=BUY…`) omit balance/credit/SO compensation. Reconstructing “trades” from every deal including `DEAL_BALANCE` invents XAU volume.

### 1.12 Factory / API version coupling — **P1**

`CreateManager(MTManagerAPIVersion, …)` must match the loaded DLL. A newer header against an older `MT5APIManager64.dll` (or the reverse) fails closed at init — or worse, if someone bypasses the version check, struct layouts desync.

**Risk:** “it compiled” ≠ “this DLL speaks this header.” Pin SDK dir + DLL hashes in the run record. Do not mix AVX2 / vanilla / ARM binaries from different SDK drops.

---

## 2. Windows / native DLL constraints

**Sources:** architecture §5, `CMakeLists.txt` 49–128, `MT5APIManager.h` `CMTManagerAPIFactory`, `mt5-sdk/README.md`, A14, A15, A07.

### 2.1 Local Manager is Windows x64 only — **P0** for deployment topology

- `mt5_manager.h` includes `<Windows.h>`. Will not compile elsewhere.
- CMake appends `mt5_manager.cpp` / `mt5_pool.cpp` / `mt5_watchdog.cpp` **only** `if(WIN32)`.
- Factory uses `LoadLibraryW` / `GetProcAddress` / `FreeLibrary`.
- README: MSVC 2022; Manager API is **Windows x64 only**.
- Architecture §5: “Windows Worker if MT5 Manager DLL requires Windows.” “Do not force native MT5 SDK components into Linux containers.”

**Risk:** a Linux `mt5-worker` container in `MT5_MODE=local` is a hard fail. Remote mode still needs a **Windows sidecar** that actually hosts the DLL. That sidecar is **not** `apps/mt5-worker` today.

Correct split: **Windows** for local Manager (Achiever + StarwaveFX slots); **Linux** allowed for API / Postgres / Redis / Python / React / (later) FIX worker **only if** FIX is pure managed QuickFIX/n.

### 2.2 Runtime DLLs must sit beside the consuming exe — **P1**

CMake copies (WIN32 only):

```text
MT5APIManager64.dll
MetaQuotes.MT5ManagerAPI64.dll
MetaQuotes.MT5CommonAPI64.dll
```

`FindLibrary` **prefers** `MT5APIManager64avx2.dll` / `avx` / `arm` when the CPU supports it, then falls back to `MT5APIManager64.dll`, then `.\libs\`, then **PATH** (always returns `true` with a bare filename — LoadLibrary search).

**Risks:**

1. CMake does **not** copy the AVX2/AVX/ARM variants. Most lab boxes will load the vanilla DLL via fallback — acceptable if versions match; **not** acceptable if PATH resolves a **different** MetaQuotes install.
2. C# `TraderIntelligence.Mt5Worker` has **no** native copy step, **no** P/Invoke, **no** HTTP client to `mt5-sdk`. Shipping the worker exe without the trio of DLLs (or without a sidecar) is a silent “not connected.”
3. PATH hijack / DLL planting: a random `MT5APIManager64.dll` on PATH loads first when the search falls through. Pin an absolute `dllPath` and refuse PATH fallback in production.

### 2.3 Manager license slots and IP whitelist — **P1**

Each live `IMTManagerAPI::Connect` consumes a **manager connection slot**. Local design is **N pool sessions + 1 pump** = **N+1** slots (Achiever intended 8+1; StarwaveFX 4+1). `MT5_POOL_SIZE` is **not clamped**; a typo (`8000`) will try to open 8000 manager sessions.

Achiever whitelist / proxy (architecture §7): outbound `81.29.145.69`, optional SOCKS/HTTP proxy. Error 1012 = manager IP blocked. Connect then falls back to no-pump (see §1.6) or fails.

**Risks:**

- Exhausting the broker’s manager limit takes down **other** firm tools sharing the same manager login.
- Connecting from a non-whitelisted build agent looks like “SDK broken.”
- Logging proxy user/password (architecture §55/§57) is a secret leak. `UserSecretsId` on the worker is unused.

### 2.4 Bitness / toolchain — **P1**

- DLLs are **64-bit**. A 32-bit host cannot load them.
- ARM64 Windows would want `MT5APIManager64arm.dll` (present under `vendor/.../Libs/`, **not** in the CMake copy list).
- Do not “upgrade runtime versions simply for fashion if the MT5 SDK depends on older/native runtime behavior” (architecture §5). Stay on **.NET 8** + **MSVC 2022 x64** until measured otherwise.
- Mixed C++/C# in one process (P/Invoke) inherits STA/MTA, exception, and CRT rules. Prefer **out-of-process** local sidecar (C++ or C++/CLI host) talking HTTP/gRPC to the C# worker rather than loading the Manager DLL inside `dotnet`. That sidecar does not exist yet.

### 2.5 `MT5_MODE=remote` is not a free Linux ticket — **P1**

Remote `MT5HttpClient` is cross-platform **only if** something Windows-side still holds the DLL and exposes the REST/SSE surface. Paths in A16 (`/mt5/accounts/{login}/deals`, `/mt5/events/stream`, …) are a **contract with a microservice that is not this repo’s worker**.

**Risk:** setting `MT5_MODE=remote` and pointing at a missing or third-party URL looks like progress and produces empty books / `false` on required methods.

### 2.6 Headless / service-session constraints — **P2**

Manager API is a native service, not a GUI, but:

- Session 0 / Windows Service isolation can break named-pipe or per-user proxy settings if someone later adds them.
- Antivirus often quarantines `LoadLibrary` of unsigned-looking trading DLLs.
- Multiple workers on one box multiply slots (see §2.3) and can duplicate pumps.

Treat the Windows host as a **singleton per broker manager login** unless the pool is the only extra connections.

---

## 3. Source tick-data availability

**Sources:** architecture §1.5, §11, §17, §45, §51, §60; `mt5_tick_bridge.{h,cpp}`; `imt5_client.h`; `mt5_ledger_store.{h,cpp}`; Domain `PriceSource` / `FeatureQuality`; A17.

### 3.1 Exact MFE/MAE is **impossible** with what is persisted today — **P0** if numbers are emitted anyway

§17 / §1.5: exact MFE, MAE, excursion, entry spread, in-trade volatility need a **source-side time series while the position is open**. Closed deals are not that series. **Do not fabricate.**

Measured:

| Layer | Persists a source tick tape? |
|---|---|
| `MT5TickBridge` | **No** — RAM queue, drain to `IQuoteSink` (quote hub) |
| `GetTickLast` / `GetAllTicksLast` | **No** — snapshot |
| `TickHistoryRequest` (SDK) | **Not wrapped** on `IMT5Client` |
| `mt5_ledger::Store` | **No** — `mt5_raw_events` + `mt5_deals_ledger` only |
| `mt5_ticks_xauusd` / `mt5_xau_ticks` | **Named in §11 / §45; no table, no writer** |
| C# MFE calculator | **Does not exist** |

`PriceSource` and `FeatureQuality` enums exist in Domain (`AchieverMt5Ticks`, `StarwaveMt5Ticks`, `BarApproximation`, `CTraderQuoteSession` / `Exact`, `Approximate`, `Unavailable`). They are **labels without a tape**. That is the correct interim position **only if MFE/MAE stay null**.

### 3.2 Live tick transport is not a warehouse — **P1**

Push path: `TickSubscribe` is **stream-wide** (every symbol the manager streams). Bridge refcounts **per symbol** and drops ticks for unsubscribed names. Cap **50,000**; **oldest-first drop**; drop counter logged on shutdown only — **no gap marker**.

Poll fallback (when `SubscribeTicks` is false — HTTP client, default interface): **250 ms**, **≤ 64 symbols / cycle**, skip entire cycle if `MT5Pool::Borrow(200)` fails. `GetTickLast` **omits volume**.

`onSdkTick` **backfills wall-clock** when SDK `datetime` / `datetime_msc` are zero. That is a **fabricated timestamp**. Excursion aligned to host clock is not broker time.

Build flag `MT5SDK_WITH_DROGON` defaults **OFF**. Without it the bridge is not compiled.

### 3.3 Historical ticks cannot be backfilled through the product client — **P1**

SDK exposes `TickHistoryRequest` / `TickHistoryRequestRaw`. `IMT5Client` has **no** method. Closed trades that predate a future tape have **no legal EXACT window**.

`GetChart` (M1 + aggregate) is the only historical price path. Allowed **only** as `price_source=BAR_APPROXIMATION`, `feature_quality=APPROXIMATE`. High/low of bars ≠ tick MFE/MAE.

### 3.4 Fabrication blacklist (binding)

Do **not**:

1. Derive MFE/MAE from entry+exit VWAP, `DealData.price`, or `PositionData.price_current`.
2. Use session `TickStat` bid_high / bid_low as a trade window.
3. Label `GetTickLast` / 250 ms poll as `Exact`.
4. Persist **destination** cTrader FIX quotes into a source tick table or score them as source excursion (§17: never silently mix).
5. Interpolate / Brownian-bridge missing prints and leave quality `Exact`.
6. Use Achiever ticks for a StarwaveFX trade (or the reverse). Key ticks by `broker_id` / `server_key`.
7. Write Postgres from `OnTick` (pump-thread rule).
8. Create fake XAU tick rows to “satisfy” the §11 table list (A07 risk #9).

If the window is missing: **leave MFE/MAE null**, `FeatureQuality.Unavailable`, score on deal-legal features only. Dashboard copy is “MFE/MAE **when valid**” (§51).

### 3.5 Destination quotes are a different book — **P0** if mixed

Shadow copy and pre-trade checks **must** use cTrader QUOTE (`destination_quotes`) as the **destination** book (A24). Source MFE/MAE **must not**. Mixing them invents both “exact skill” and “destination slippage” from the wrong venue.

---

## 4. cTrader FIX credential / header ambiguity

**Sources:** architecture §§25–28, §41, §56, §72.11–12; official RoE / FAQ (A32, A33, A34); `CTraderFixOptions.cs`; A05, A25.

This is the defect §26 exists to prevent: **do not infer FIX tags from the human-readable broker form.**

### 4.1 `cServer` vs `CSERVER` — **P0** for first Logon

| Source | `TargetCompID` (56) |
|---|---|
| Official RoE table + official examples | **`CSERVER`** |
| Architecture §25 / §56 env sample (issued form) | **`cServer`** |
| Official send/receive prose | “usually it is cServer” **and** samples use `CSERVER` |
| `CTraderFixOptions` defaults (product, 2026-08-18) | **`CSERVER`** (QUOTE and TRADE) |

**Required behaviour:** do **not** silently fold case. Make `TargetCompID` configurable per session. Persist the **exact** string sent. Prove Logon in diagnostics. If `cServer` fails, try `CSERVER` only as an **explicit, logged override** (`CTRADER_FIX_*_TARGET_COMP_ID`), never as a hidden `ToUpperInvariant()`.

Current defaults already **picked a side**. That is convenient if RoE is right and fatal if this Pepperstone acceptor is case-sensitive the other way. **Unresolved until a diagnostic Logon record exists for both sessions.**

Invalid Logon → Logout `35=5` + `Text` (58). Invalid application FIX → FAQ: **no response at all** (checksum, UTC, tag order, missing tags). A silent socket is not “network down”; it is often a header defect.

### 4.2 `SenderSubID` (50) is not the session qualifier — **P0**

| Tag | Role |
|---|---|
| **57 `TargetSubID`** | **Required session qualifier:** `QUOTE` or `TRADE` |
| **50 `SenderSubID`** | Optional originator. **Must be `QUOTE` when 57=`QUOTE`.** On TRADE, official example is `50=any_string`, **not** `TRADE`. |

Broker forms often label the qualifier “SenderSubID = QUOTE/TRADE.” Mapping that **only** onto tag 50 and leaving 57 empty is a likely Logon failure.

`CTraderFixOptions` defaults: `TargetSubId` = `QUOTE`/`TRADE` (good), `SenderSubId` = **empty**. Empty QUOTE `SenderSubID` **violates** the RoE “must be QUOTE” rule.

Inbound Logon **swaps** Comp/Sub IDs (A32): server `50` carries the qualifier, server `57` echoes the client originator. Treating inbound 50/57 with outbound meaning mis-classifies the session.

Header table values are uppercase `QUOTE`/`TRADE`; some official examples use `Quote`/`Trade`. Send the table values. Do not assume case-insensitive equality until proven.

### 4.3 `SenderCompID` vs `Username` — **P1**

- **49 `SenderCompID`:** `<Environment>.<BrokerUID>.<Trader Login>` → issued `live.pepperstone.1369850`.
- **553 `Username`:** numeric login **only** → `1369850`.
- **554 `Password`:** secret; never log; never show on the dashboard.

Do not put the dotted triple in 553 or the bare login in 49. Do not log 554. Architecture already binds the secret slot to live account `1369850` as a placeholder (`<SECRET: account 1369850 password>`) — not a leak, but a targeting hint.

`CTraderFixOptions` **hardcodes** live host `live-us-eqx-01.p.c-trader.com` and live `SenderCompId = live.pepperstone.1369850`. That is production targeting in source defaults (A19’s “not in product code” is **stale** as of this file). Defaults must not be the thing that lets a laptop Logon the live TRADE port.

### 4.4 Two sessions, two sequence worlds — **P0** if shared

QUOTE SSL **5211**, TRADE SSL **5212** (plain 5201/5202 exist; **production default is TLS**). They are **two TCP sessions**, not one multiplexed connection.

Never share: socket, SessionID, in/out seq, heartbeat clock, last in/out, reconnect state, metrics, log scope, QuickFIX **FileStorePath**.

RoE: sequence numbers **reset on establish** (`ResetSeqNumFlag=Y`, 141). The socket seq is **not** a durable order log. PostgreSQL is.

QUOTE may **omit Heartbeat while quotes stream** (FAQ). Liveness = **quote age**, not `35=0`. Opening a second QUOTE “because there is no heartbeat” creates the duplicate-report condition (below).

A MarketDataRequest on TRADE, or a `NewOrderSingle` on QUOTE, is a defect.

### 4.5 Duplicate connections duplicate **every** report — **P0**

FAQ (verbatim): multiple simultaneous API connections → server sends a **copy of each FIX response to every active connection**.

Two TRADE sockets (prod worker + laptop, or two replicas) ⇒ duplicate `35=8` **and** two senders ⇒ **duplicate orders**. Ownership (architecture §28) is a **safety control**, not a nicety. Implement a lease + fencing token. A fenced sender **must drop the send**.

There is **no** lease type, no Redis/Postgres lock, no `fix_session_leases` table in a working form. `FixWorker` can be started twice today with no gate.

### 4.6 Wrong FIX engine package — **P1**

Architecture / A05: prefer **QuickFIXn.Core + QuickFIXn.FIX44** (pin 1.14.1 or later 1.14.x). Do **not** write a raw `TcpClient` engine. Official Spotware sample is explicitly **not** an engine.

Measured `TraderIntelligence.Fix.CTrader.csproj`:

```xml
<PackageReference Include="QuickFix.Net" Version="1.8.0" />
```

That is **not** the official QuickFIX/n 1.14 line (`QuickFIXn.FIX4.4` was renamed to `QuickFIXn.FIX44`). Generic FIX 4.4 dictionaries **drop** cTrader custom tags: **721** `PosMaintRptID`, **1000–1006** SL/TP, **1007** `SymbolName`, **1008** `SymbolDigits`. Tag **55** is a **Long instrument id**, not `"XAUUSD"`.

**Risk:** a stock dictionary rejects or strips the fields required to attach to a hedge position, discover XAU, or parse SecurityList. Do not port the Spotware string+checksum sample.

### 4.7 Diagnostic Logon gate is missing — **P1**

A25 §3.6: no application messages until a persisted record exists **per session** (host, port, TLS, 49/56/50/57 as sent, 553 numeric only, result `LOGON_OK` | reject | no-response | transport-fail). QUOTE-only success does **not** unlock TRADE application messages.

No `fix_session_events` writer exists. `CTRADER_FIX_DIAGNOSTIC_LOGON_ONLY` is specified, not implemented. `FixWorker` appsettings have **no** `CTRADER_*` keys.

---

## 5. Symbol / quantity mapping

**Sources:** architecture §16, §30, §35, §38, §72.13–14; RoE tag 55 / 1007 / 1008 / 38; `VolumeConverter.cs`; `mt5_types.h` volume comment vs `MT5APIMath.h`; A13; Domain mapping entities.

### 5.1 Source ticker is not canonical XAUUSD — **P1**

Source brokers may expose `XAUUSD`, `XAUUSD.`, `XAUUSDm`, `XAUUSD.a`, `GOLD`, … Execution venue uses a **numeric** cTrader instrument id.

Required:

```text
broker/source symbol  →  CanonicalInstrument (XAUUSD)
cTrader instrument ID →  CanonicalInstrument (XAUUSD)
```

Never assume FIX tag 55 is the string `"XAUUSD"`. Official reject flavour: `Expected numeric symbolId, but got CS8260`.

Discover via SecurityList (`35=x` / `35=y`): persist **55** (id), **1007** (name), **1008** (digits). IDs are **per environment/broker/account**. `55=1` is EURUSD **in the RoE sample**, not a universal constant, and not gold. **Do not hardcode** a Pepperstone XAU id from another account (§30, §72.13).

Measured Domain:

- `CanonicalInstrument(Id, Symbol, Description)` — name only. **No** contract size, digits, step, destination id.
- `SourceSymbolMapping(Id, BrokerId, SourceSymbol, CanonicalInstrumentId)` — source→canonical only. **No** destination instrument row.

`TraderDbContext` names `destination_quotes` / `fix_session_states` but **does not** compile against matching Domain types (plural `Brokers`, missing `CopyIntents`, …). There is **no** working `destination_symbols` mapping service.

### 5.2 MT5 volume scale is 10_000, not 100 — **P0**

Official: `MTAPI_VOLUME_DIV = 10000` (4 decimals). `1.00 lot = 10000`, `0.01 lot = 100`.

`PositionData.volume` comment in `mt5_types.h` says **“hundredths of lots”**. That comment is **wrong**. Following it makes every size **100× too large**.

Extractors copy raw `Volume()` integers with **no** rescale. `VolumeExt()` (1 lot = 100_000_000) is unused.

`VolumeConverter` (Domain) is the correct MT5 law:

- `ManagerVolumeScale = 10_000`
- `ExtendedVolumeScale = 100_000_000`
- documents the hundredths comment as incorrect

C# raw DTOs must keep `ulong` native volume (A13). Convert to lots only in the sizing layer. `Mt5Deal.Volume` is already `ulong` — good. Display/code that does `/ 100m` is a **P0** sizing bug.

`OrderData.volume` is **initial**, not remaining (see §1.11).

### 5.3 Source lots ≠ destination `OrderQty` — **P0**

Architecture executive rule #10 / §38 / §72.14:

```text
source 0.10 MT5 lots   ≠   destination OrderQty 0.10
```

cTrader RoE: tag **38 `OrderQty`** is venue **units**, “maximum precision is 0.01.” Official market-order example sends `38=10000` with `55=1` (EURUSD **in that sample**). That is **units**, not MT5 lots, and not a gold constant.

Legal pipeline only:

```text
source native volume
    → lots via 10_000 (or VolumeExt if ever used)
    → canonical notional / risk  (source contract_size)
    → portfolio allocation (min suggested, remaining caps)
    → destination instrument quantity convention
    → round DOWN to destination step
    → enforce dest min / max
    → re-check book / margin caps
```

`REDUCE` / `CLOSE` quantity comes from the **mapped destination position**, not from re-running source lots through allocation (§35, §64). Reversals are **two** events (close then open), not one `OrderQty`.

`VolumeConverter` stops at **MT5 lots**. There is **no** destination convention, no step/min, no contract-size table, no fixture of known Achiever/StarwaveFX → Pepperstone XAU examples. §38 / §68: **unit tests against real known examples are a go-live gate.** Until those fixtures exist, live send is forbidden.

### 5.4 Contract size, digits, and hedge attach — **P1**

`SymbolData.contract_size` exists on the C++ DTO (gold often 100 oz; some symbols 1). Destination `1008 SymbolDigits` is **price decimals**, **not** lot size and not tag 55.

Hedge accounts: attach with tag **721 `PosMaintRptID`**. Omitting 721 **opens a new position**. A scale-in that should increase exposure can flip the book into a hedge pair and blow net/gross limits.

Netting vs hedge on source MT5 vs destination cTrader is a mapping problem: source `position` ticket ≠ destination 721. `source_destination_links` is specified, not implemented.

### 5.5 Cross-broker symbol identity — **P1**

Achiever `XAUUSD` and StarwaveFX `XAUUSD.` are different **source** rows, same canonical. Ticks, contract size, and volume step can differ. A single global `"XAUUSD" → destId` map without `broker_id` will size one broker with the other’s contract.

Plan-group names (`contest\yo-*` vs C++ helper `Flexy\yo-*`) must not filter which symbols are fetched.

---

## 6. Live-account safety

**Sources:** architecture §§32–34, §40–43, §55–56, §61–64, §68–70, §72.8–10; A23, A24, A25, A19; `CTraderFixOptions`; `KillSwitchMode`; `TraderState`; workers.

Account `1369850` / SenderCompID `live.pepperstone.1369850` is **real money**. Diagnostic **Logon** may be allowed under a single owner. Diagnostic **`NewOrderSingle` is not**.

### 6.1 Feature flags are specified; enforcement does not exist — **P0**

Specified defaults (§41):

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (good). `FixWorker` **does not read** this object. `appsettings.json` has **logging only**. There is no send-path conjunction check.

Sending `35=D` requires **all** of (A25 §6.3): both FIX flags, `REAL_COPY_EXECUTION_ENABLED=true`, TRADE `READY_FOR_EXECUTION` (Logon **+** recon), lease + current fence, risk healthy, `STOP_NEW_EXECUTION=false`, fresh quote if the order needs a price, persist-before-send, `status=not_sent`, intent not expired.

Runtime must **not** flip `REAL_COPY_EXECUTION_ENABLED` on if config is false. Turning the flag on must **not** flush a backlog (no blind catch-up, §63).

### 6.2 Persist-before-send and unknown state — **P0**

Critical case: persist intent + `cl_ord_id` → send `35=D` → disconnect. **Did cServer get it?**

**Illegal:** retry same ClOrdID; retry new ClOrdID; increment a retry counter and fire again.

**Legal:** `EXECUTION_STATE_UNKNOWN` → block further sends for that intent → OrderStatus (`35=H`) → MassStatus (`35=AF`, type 7) → Positions (`35=AN`) → adopt venue or prove absent → only then a **new** row / new ClOrdID.

Crash between persist and write, or persist of `sent_at` uncertain → treat as **unknown**, not `not_sent`. If persist **failed**, **do not send**.

None of: `ClOrdId` factory, `execution_intents` writer used by a worker, state machine, unknown recovery — exist as a working path. Unique ClOrdID is required for Order Status (RoE) and is a **safety property**.

### 6.3 Never send FIX from an MT5 callback — **P0**

Correct path only (§32):

```text
source event → CopyIntent (persist) → RiskEngine → ApprovedExecutionIntent (persist)
            → FIX worker (lease owner) → NewOrderSingle → ER persist → dest position → reconcile
```

`CopyIntentAction` / `RiskDecisionOutcome` / `KillSwitchMode` / `TraderState` enums exist. There is **no** risk engine, no outbox consumer that creates intents, no FIX gateway. The danger is a future shortcut from `OnPositionAdd` / deal poll straight to `35=D`.

### 6.4 Kill switches must not be conflated — **P0**

| Control | Effect | Positions |
|---|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | Master enable for **new** copy `35=D`. Default false. | untouched |
| `STOP_NEW_EXECUTION` | Operational halt of new open/increase | **untouched** |
| `EMERGENCY_FLATTEN` | Close destination positions; separate permission + confirm | reduced |

`KillSwitchMode` enum has `None`, `StopNewExecution`, `EmergencyFlatten`. No durable flag, no RBAC, no dashboard wiring, no flatten persist-before-send.

Flatten may send reducing orders when real-copy is false **only if** TRADE is logged on, lease owned, authorized, and still uses unknown-state rules. Failure to flatten is an **alert**, not a retry storm.

### 6.5 Reconciliation is a gate, not a report — **P0**

On TRADE Logon: **block new executions** → mass status → positions → compare DB → only then `READY_FOR_EXECUTION` (§42). Periodic compare (§43) must drop that gate on unknown / missing / qty / side / orphan / unexpected fill.

Database down → **fail closed**; no real execution from memory only (§62).

`TraderDbContext` is **not** a working execution store (type names do not match Domain; most configurations missing). In-memory “we know the position” is **UNSAFE**.

### 6.6 Trader state and shadow are the promotion fence — **P1**

`TraderState`: `INSUFFICIENT_DATA` … `SHADOW` → `LIVE_CANDIDATE` → `LIVE` → `PAUSED` / `RISK_BLOCKED` / `DISQUALIFIED`.

Live copy only from `LIVE` (or an explicit candidate gate). Shadow (A24) uses **QUOTE only**, destination bid/ask, **never** `35=D`. Shadow P&L on rotten quotes is forbidden (same freshness rules).

ML / score **never** bypasses risk (§72.15). Trade #3 is early evidence, not skill (§72.16). Concentration clustering is **Phase 2 after** basic copy is stable (§65) — v1 must still have book-level XAU caps.

### 6.7 Stale signal / no 20-order catch-up — **P0**

If TRADE is down 3 minutes and sources open 20 trades, reconnect must **not** fire 20 `NewOrderSingle`s (§63). Expired / over-age **OPEN/INCREASE** intents die. Already-sent unknown orders **do not expire into a resend**.

QUOTE down or `quote_age > max` → reject open/increase (`QUOTE_STALE` / `QUOTE_UNAVAILABLE`). A fresh but gapped XAU print still fails `PRICE_MOVED_TOO_FAR`. `CTraderFixOptions.MaxQuoteAgeMs = 5000` is a **placeholder**, not a measured production number. Do not hardcode live thresholds in the engine (§23, §31).

### 6.8 Source collector write APIs and live source accounts — **P0**

See §1.9. Pointing `SendTrade` / `DealerBalance` at Achiever or StarwaveFX from this product is **out of scope and unsafe**. Those are customer/prop-firm books, not a sandbox.

Provisioning-map drift (`Flexy\yo-*` vs `contest\yo-*`) must not create or move users.

### 6.9 Secrets, identifiers, and “just test on live” — **P0 / P1**

A19 (re-checked against current tree):

- **No** live passwords in repo. Architecture uses `<SECRET>`.
- **Live targeting data** is in the architecture markdown **and** now in `CTraderFixOptions` defaults (host, account-shaped SenderCompID).
- `AllowedHosts: "*"` on the API template.
- Never send passwords to React (§55). Never log 554 / MT5 / proxy passwords (§57).

Architecture §61 / A25: **do not use account 1369850 as the first integration test.** Required: in-process simulator (recorded ER, MD, disconnect, dup ER, partial, reject, unknown-state). `tests/Unit` and `tests/Integration` are empty `Fact` stubs. `tests/Fix` does not exist.

A developer laptop TRADE session while production holds the same account **is a duplicate-connection incident** (§4.5). Diagnostics against live: production TRADE **disabled**, or production stopped and the diagnostic process holds the lease.

### 6.10 “Worker running at:” is not safety — **P0** false PASS

Both workers log a 1 Hz heartbeat. That proves the process is alive. It does **not** prove: Manager connected, pump on, groups discovered, FIX logged on, lease held, recon clean, or send blocked.

A Release exe in `apps/mt5-worker/bin` exists because the **template compiles**.

---

## 7. Compound risks (interactions)

These are how the six topics **stack**. A single-control “fix” does not clear them.

| ID | Compound | Why it is worse together |
|---|---|---|
| X1 | No deal pump + unpaged `GetDeals` + checkpoint advance | Permanent missing source trades **or** invented completeness |
| X2 | Linux worker + `MT5_MODE=local` + ignored DLL | Silent no-data; copy later treats empty as “no one is trading” |
| X3 | `SubscribeTicks` false + poll skip + fabricated MFE | Scoring promotes the wrong traders |
| X4 | Header guess + two replicas + no lease | Logon may work **and** duplicate every fill |
| X5 | `volume / 100` + `lots = OrderQty` + hardcoded `55=XAU` | 100× gold on the wrong instrument |
| X6 | Template FixWorker + hardcoded live CompID + `RealCopyExecutionEnabled` flipped in config | First real order with **zero** recon / ClOrdID / risk |
| X7 | `SendTrade` left callable on the source Manager used by the collector | Live source-side orders from an “ingestion” process |
| X8 | QUOTE heartbeat FAQ + operator opens a second session | Duplicate MD/ER; quote-age clock desync |
| X9 | DB types don’t match Domain + “fail closed if DB down” unimplemented | Future send path runs from memory |
| X10 | Shadow using source last price + live using dest qty guess | Promotion evidence is not the venue that will fill |

---

## 8. Risk register (condensed)

| ID | Topic | Sev | Risk | Current mitigation (measured) | Required control before live send |
|---|---|---|---|---|---|
| R01 | SDK | P0 | Live deals never arrive (`no PUMP_MODE_DEALS`) | Comment-only | Deal-lag poll + recon; never depend on `OnDealAdd` |
| R02 | SDK | P1 | Unpaged `DealRequest` silent truncate | HTTP client pages; local does not | `DealRequestPage` or fail-closed; no checkpoint on partial |
| R03 | SDK | P1 | `false` vs empty collapsed | Interface comments | Typed results; never advance cursor on `false` |
| R04 | SDK | P0 | Work on pump thread / FIX from callback | None in C# (no callbacks yet) | Enqueue-only sinks; §32 path only |
| R05 | SDK | P1 | No-pump fallback looks healthy | Logged in C++ connect | Persist pump flag; `SOURCE_STALE` |
| R06 | SDK | P0 | Collector calls `SendTrade` / balance | Not wired in C# | Read-only connector; 501/deny mutate |
| R07 | SDK | P2 | Pool size / watchdog unwired | Library only | Wire N+1; clamp pool; schedule health |
| R08 | SDK | P0 | Cross-broker ticket clash | Domain `BrokerId` on some records | Compound keys everywhere |
| R09 | DLL | P0 | Local mode on non-Windows | CMake `if(WIN32)` | Windows worker / sidecar; no fake Linux LoadLibrary |
| R10 | DLL | P1 | DLL not beside exe / PATH hijack | CMake copy for C++ targets only | Absolute `dllPath`; hash pin; C# copy or sidecar |
| R11 | DLL | P1 | AVX vs vanilla vs wrong SDK version | Fallback to `MT5APIManager64.dll` | Record loaded path + version |
| R12 | DLL | P1 | Manager slot exhaustion / 1012 | Documented | Size to broker limit; proxy/whitelist runbook |
| R13 | DLL | P1 | Remote mode without sidecar | HTTP client exists; no host | Do not claim Phase 1 on remote stubs |
| R14 | Ticks | P0 | Fabricated MFE/MAE | Feature not implemented (safe) | Null + `Unavailable` until labeled tape |
| R15 | Ticks | P1 | No `TickHistoryRequest` wrap | SDK has it | Wrap or omit historical EXACT |
| R16 | Ticks | P1 | Bridge drops / wall-clock backfill | Optional Drogon | Gap markers; never `Exact` on poll |
| R17 | Ticks | P0 | Dest FIX quotes as source ticks | Neither wired | Separate tables + `PriceSource` |
| R18 | FIX hdr | P0 | `cServer`/`CSERVER` silent mutate | Options default `CSERVER` (unproven) | Configurable; diagnostic Logon both sessions |
| R19 | FIX hdr | P0 | Qualifier on tag 50 not 57 | Options: 57 set, 50 empty | QUOTE `SenderSubID=QUOTE`; both configurable |
| R20 | FIX hdr | P0 | Shared seq / two TRADE sockets | No engine yet | Two stores; lease + fence |
| R21 | FIX hdr | P1 | Wrong NuGet / generic dictionary | `QuickFix.Net` 1.8.0 | QuickFIXn 1.14.x + RoE dictionary |
| R22 | FIX hdr | P1 | Live host/CompID hardcoded | `CTraderFixOptions` | Config/secrets; no live default in source |
| R23 | Map | P0 | Hundredths vs 10_000 | `VolumeConverter` correct; C++ comment wrong | Never `/100`; tests on 0.01 / 0.10 / 1.00 |
| R24 | Map | P0 | Lots = `OrderQty` | Converter is lots-only | §38 pipeline + known fixtures |
| R25 | Map | P0 | Tag 55 = `"XAUUSD"` | Mapping types incomplete | SecurityList persist; no hardcoded id |
| R26 | Map | P1 | Missing dest contract/step/721 | `CanonicalInstrument` is a name | `destination_symbols` + hedge attach |
| R27 | Live | P0 | `35=D` without flags/recon/lease | Flag default false; no send path | Conjunction + `READY_FOR_EXECUTION` |
| R28 | Live | P0 | Retry after TCP break | Unimplemented (safe only while no send) | Unknown-state recovery; no second D |
| R29 | Live | P0 | Kill-switch conflation | Enum only | Distinct perms; flatten ≠ stop-new |
| R30 | Live | P0 | Blind catch-up after FIX gap | Spec only (A23/A25) | `expires_at` / `max_signal_age` |
| R31 | Live | P0 | Live account as first test | Tests are empty stubs | Simulator first (§61) |
| R32 | Live | P1 | Secrets / password logs | Repo secret-clean | Redact 554; no UI password |
| R33 | Live | P0 | False PASS on template workers | 1 Hz log | Measured Phase 1 / Phase 7 gates |

---

## 9. What must not be done (standing bans)

1. Do not send FIX from an MT5 sink or from `GetDeals` completion.
2. Do not blindly retry `NewOrderSingle` because TCP dropped.
3. Do not enable `REAL_COPY_EXECUTION_ENABLED` to debug headers.
4. Do not use account `1369850` as the first integration test.
5. Do not write a `TcpClient` FIX engine or keep `QuickFix.Net` 1.8.0 as the long-term engine.
6. Do not silently uppercase `cServer` or invent tag 50 as the session qualifier.
7. Do not put `"XAUUSD"` in tag 55 or hardcode a Spotware id from another account.
8. Do not convert `source_lots → OrderQty` 1:1 or divide MT5 `Volume()` by 100.
9. Do not compute MFE/MAE from deals, last ticks, session stats, bars, or destination quotes without labels — prefer **omit**.
10. Do not invent source trades when MT5 is down; do not advance checkpoints on partial history.
11. Do not load `MT5APIManager64.dll` in Linux. Do not open two production TRADE sessions.
12. Do not call `CreateUser` / `DealerBalance` / `SendTrade` from the intelligence collector.
13. Do not flatten via `STOP_NEW_EXECUTION`. Do not treat QUOTE Logon as TRADE ready.
14. Do not commit passwords. Do not log 554 / MT5 / proxy secrets.
15. Do not treat `Worker running at:` as Achiever connected or FIX logged on.
16. Do not modify product source from this task (already honoured).

---

## 10. Phase gates this list owns

Architecture §73.D is a **Phase 0** deliverable (A28). Clearing a row in §8 is **not** automatic when a later phase starts. Minimum mapping:

| Phase | Risks that must be designed before that phase exits |
|---|---|
| **0** (this file) | All six topics identified on disk |
| **1** ingestion | R01–R08, R09–R13, R03, R05 |
| **2** reconstruction | R08, R23 (lots for features), no MFE fabrication (R14) |
| **4** QUOTE | R18–R22, R16 as dest quotes (not source tape), TLS |
| **5** shadow | R24–R26, R17 (do not mix), R30 for shadow opens |
| **7** TRADE read/recon | R20, R27 recon half, R28 machinery, R31 simulator |
| **8** live send | **Every P0** in §8; §68 / §70 checkboxes; flags default off until explicitly flipped |

§68 / §70 excerpts this list will refuse to waive:

```text
[ ] position sizing conversion verified (known fixtures)
[ ] stale quote / stale signal rejection works
[ ] kill switch tested (stop-new ≠ flatten)
[ ] risk rejection happens before FIX send (zero outbound)
[ ] header mapping proven both sessions (no silent case change)
[ ] single-active TRADE ownership with fencing
[ ] unknown-state recovery proven (no second 35=D)
[ ] reconciliation blocks execution while inconsistent
[ ] REAL_COPY_EXECUTION_ENABLED default false
[ ] secrets absent from repo / logs / dashboard
[ ] simulator / recorded ER — not account 1369850 — is the first test
```

---

## 11. Traceability

| Topic | Architecture | Evidence in this tree |
|---|---|---|
| MT5 SDK constraints | §§6–14, 62, 72.6–7 | `imt5_client.h`, `mt5_manager.cpp`, `MT5APIManager.h` EnPumpModes, A12–A16, A07 |
| Windows / native DLL | §5, §7 | `CMakeLists.txt` WIN32 + DLL copy, `LoadLibraryW`, README, A14 |
| Source tick availability | §§1.5, 11, 17, 45, 51, 60 | `mt5_tick_bridge`, ledger store, missing tick tables, A17, `PriceSource`/`FeatureQuality` |
| FIX header ambiguity | §§25–28, 41, 56, 74 | RoE/FAQ (A32–A34), `CTraderFixOptions`, A05, A25 |
| Symbol / quantity | §§16, 30, 35, 38, 72.13–14 | `VolumeConverter`, `mt5_types.h` comment, `SourceSymbolMapping`, RoE tag 55/38, A13 |
| Live-account safety | §§32–34, 40–43, 55, 61–64, 68–70 | Flags/options, empty workers, A19, A23–A25, A28 |

Related swarm notes (read, not duplicated as authority): A07, A12–A17, A19, A23–A25, A28, A32–A34, A37.

---

## 12. Explicit non-goals of this artifact

- No product source changes.
- No implementation of FIX, Manager worker, sizing, or kill switch.
- No hardcoded production numeric limits (quote age 5000 ms in options is recorded as a **placeholder**, not blessed).
- No claim that EX5 / MQ5 work is in scope (it is not).
- No claim that Domain/Infrastructure currently compile as a coherent model — `TraderDbContext` vs entity names is a **separate** build-break, noted only where it removes a safety net (R09/R27/R33).

---

**DONE.** Phase 0 §73.D risk list is on disk. Product source was not modified.
