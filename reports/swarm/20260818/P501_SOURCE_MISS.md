# P501 — Source miss: 90-day batch + score loop never sees live milliseconds

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P501_SOURCE_MISS.md` |
| Slot | **P501** |
| Date | 2026-08-18 |
| Product source edited | **No** (report only) |
| Assigned reads | `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`, `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` |
| Adjacent reads | `DealIngestionService.cs`, `Mt5Contracts.cs`, `Worker.cs`, `CopyTradingHostedService.cs`, `docs/risk.md`, architecture §12, `MT5APIManager.h`, `MT5APIDeal.h`, C++ `mt5_manager.cpp` `DealSubscribe` / `OnDealAdd` |
| Verdict | **CONFIRMED_SOURCE_MISS.** Live C# ingest is a **one-shot −90 d `DealRequest` batch then a score loop**. Pump is **connect-only** (`GROUPS\|USERS\|POSITIONS`). **`DealSubscribe` / `CIMTDealSink` / `OnDealAdd` are absent** from the C# connector. That sink is the no-miss live path. Current path **cannot** satisfy a 100–2000 ms copy clock. |
| NewOrderSingle / FIX send | Not sent this slot |
| Risk to capital | **NONE** (`SAFE_BY_ABSENCE` on live send; this slot is ingest-shape only) |

Empty PASS is forbidden. Both assigned files were opened in full.

---

## 0. One-line

`LiveIngestHostedService` freezes `from = UtcNow.AddDays(-90)` / `to = UtcNow.AddMinutes(1)`, pulls history through `DealRequestByGroup` in 14-day **second** windows, then scores stored logins, then **exits**. Nothing is subscribed. Pump fills group/user/position cache at `Connect`. Live deals that print in milliseconds **never enter** the C# book.

---

## 1. What “live ingest” actually does

`D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` is a sealed `BackgroundService` (**141** lines). `ExecuteAsync` is **not** a loop.

Measured sequence:

| Step | Code | Effect |
|---|---|---|
| 1 | `Task.Delay(2s)` | Host idle |
| 2 | `from = UtcNow.AddDays(-90)`, `to = UtcNow.AddMinutes(1)` | Window **frozen at T0**. Any deal after `T0+1min` is out of range **by construction** |
| 3 | For each connector: `ConnectAsync` → `SyncCatalogAsync` | Groups + accounts only. Phase `connecting` → `catalog` → `catalog-done` |
| 4 | For each connected broker: `SyncBrokerAsync(code, from, to)` | History RPC. Phase `deals` → `deals-done` |
| 5 | For each connected broker: `ListLoginsWithDealsAsync` → `RebuildTraderAsync` | Reconstruct + score **already stored** deals. Phase `scoring` → `done` |
| 6 | `ExecuteAsync` returns | **No second tick.** Host is dead. |

Quoted window (the entire “live” range):

```37:38:D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs
            var from = DateTimeOffset.UtcNow.AddDays(-90);
            var to = DateTimeOffset.UtcNow.AddMinutes(1);
```

Quoted deals then score (no `while`, no subscribe, no checkpoint advance):

```81:125:D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs
                    st.Phase = "deals";
                    st.UpdatedAt = DateTimeOffset.UtcNow;
                    var deals = await ingest.SyncBrokerAsync(connector.BrokerCode, from, to, stoppingToken);
                    st.DealsInserted = deals;
                    st.Phase = "deals-done";
                    // ...
                    var logins = await store.ListLoginsWithDealsAsync(brokerId, stoppingToken);
                    st.Phase = "scoring";
                    // foreach login → RebuildTraderAsync
                    st.Phase = "done";
                    _log.LogInformation("{Broker} scored {Scored} logins that have deals", connector.BrokerCode, scored);
```

Scoring is gated on `ListLoginsWithDealsAsync`. A login whose **only** fills happen after the frozen `to`, or during the hours-long catalog/deals walk, is **not scored** and is **not re-pulled**.

`ITradingStore` has `sync_checkpoints` named in EF (`TraderDbContext`) but this host **never reads or writes a cursor**. A59 already classified the §12 three-loop pattern as **MISSING**. This slot re-confirms: still missing.

---

## 2. The batch itself is request history, not a tape

`D:\Prop\src\Application\Ingestion\DealIngestionService.cs` `SyncBrokerAsync`:

1. `ConnectAsync` again.
2. `SyncCatalogAsync` again (groups + every account).
3. If `IMt5BulkDealReader`: **for each group** `GetGroupDealsAsync(group.Name, from, to)`.
4. Else per-login `GetDealsAsync`.
5. Position snapshot (`GetGroupPositionsAsync("*")` or per login).
6. Return insert count.

There is **no** event enumerator. There is **no** overlap window after `to`. There is **no** page cursor. Completeness is “whatever `DealRequestByGroup` returned for that 14-day slice.”

`NativeMt5BrokerConnector` implements that bulk reader with **unix-second** RPC:

```296:316:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5DealDto> GetGroupDealsCore(string group, DateTimeOffset from, DateTimeOffset to)
    {
        lock (_gate)
        {
            Ensure();
            var all = new List<Mt5DealDto>();
            foreach (var (start, end) in Windows(from, to))
            {
                var arr = _manager!.DealCreateArray();
                try
                {
                    var res = _manager.DealRequestByGroup(group, start.ToUnixTimeSeconds(), end.ToUnixTimeSeconds(), arr);
                    // ...
                    all.AddRange(ReadDeals(arr));
```

`Windows` is 14-day chunks (`AddDays(14)`). A −90 d pull is ~7 RPCs **per group**, serialized on `_gate`. Catalog of thousands of accounts happens **before** those RPCs. Wall clock of step 3+4 on an 8+10 group book is **minutes to hours**. The frozen `to = T0+1min` has already expired while the first group is still paging history.

---

## 3. Pump is used for connect (groups) — not for deals

`ConnectCore` pump mask:

```88:111:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            var pump = CIMTManagerAPI.EnPumpModes.PUMP_MODE_GROUPS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_USERS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_POSITIONS;
            var res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, pump, 30000);
            if (res == MTRetCode.MT_RET_OK)
            {
                _connected = true;
                _pumpEnabled = true;
                // ...
                return;
            }

            res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, CIMTManagerAPI.EnPumpModes.PUMP_MODE_NONE, 30000);
            // ...
            _pumpEnabled = false;
```

What that mask is for (SDK `EnPumpModes`, `MT5APIManager.h` L125–143):

| Flag | Bit | What it pumps | Used by C# connector |
|---|---|---|---|
| `PUMP_MODE_USERS` | `0x00000001` | User cache (`UserTotal` / `UserGet` / `UserGetByGroup`) | Connect only |
| `PUMP_MODE_ORDERS` | `0x00000008` | Open orders | **Not set** |
| `PUMP_MODE_POSITIONS` | `0x00000080` | Open positions cache | Connect only; live reads still prefer `PositionRequest*` |
| `PUMP_MODE_GROUPS` | `0x00000100` | Group **configs** (`GroupTotal` / `GroupNext`) | Connect only; live reads prefer `GroupRequestArray("*")` |
| `PUMP_MODE_DEALS` | — | **Does not exist** | — |
| `PUMP_MODE_NONE` | fallback | Cold cache; request APIs still valid | Fallback path |

Pump is a **connect-time cache fill** so group/user/position `Get*` can hit RAM. It is **not** a deal tape. There is no `PUMP_MODE_DEALS` in Manager API 5570 (`30 Jan 2026`). Deals are **not** pumped.

On pump-connect failure the product retries `PUMP_MODE_NONE` and keeps `DealRequest*`. That is the honest “we only needed groups/users to enumerate, then we batch-request history” design. It is also why a successful `_pumpEnabled = true` does **not** imply live deal delivery.

`PumpEnabled` is exposed and never consulted by `LiveIngestHostedService`. The host does not branch on pump vs request.

---

## 4. The no-miss path: Deal sink / `OnDealAdd`

Manager API surface (`D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` L267–272):

```text
DealCreate / DealCreateArray
DealRequest(ticket) / DealRequest(login, from, to, array)
DealSubscribe(IMTDealSink* sink)
DealUnsubscribe(IMTDealSink* sink)
```

Deal events (`D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h` L303–312):

```text
class IMTDealSink
  OnDealAdd(const IMTDeal* deal)
  OnDealUpdate(const IMTDeal* deal)
  OnDealDelete(const IMTDeal* deal)
  OnDealClean(login)
  OnDealSync()
  OnDealPerform(...)
  OnDealPerformCloseBy(...)
```

`IMTDeal` carries **sub-second** time (`TimeMsc`, header: “deal creation datetime in us since 1970.01.01”, L197–199). `Time()` is the second clock.

**C# product implements none of the sink.** Grep of `D:\Prop\src` for `OnDealAdd`, `DealSubscribe`, `CIMTDealSink`, `IMTDealSink`, `SubscribeEventsAsync`: **0 hits**.

`NativeMt5BrokerConnector` class line:

```24:24:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
public sealed class NativeMt5BrokerConnector : IMt5BrokerConnector, IMt5BulkDealReader, IMt5BulkPositionReader, IDisposable
```

Port `IMt5BrokerConnector` (`Mt5Contracts.cs`) is request-only: `Connect` / `GetGroups` / `GetAccounts` / `GetDeals` / `GetPositions`. No subscribe member.

The sibling C++ stack **does** take the no-miss path after a pumped connect:

```138:142:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
    m_manager->PositionSubscribe(this);
    m_manager->DealSubscribe(this);
    m_manager->OrderSubscribe(this);
    m_manager->UserSubscribe(this);
```

```1389:1408:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
void MT5Manager::OnDealAdd(const IMTDeal* deal) {
    // ...
    DealData dd = extractDeal(deal);
    cacheRecentDeal(dd);
    MT5Event evt;
    evt.type = MT5EventType::DealAdd;
    evt.login = deal->Login();
    evt.data = std::move(dd);
    m_eventQueue.push(std::move(evt));
}
```

That is the path that can see a fill **when the manager emits it**, without waiting for a −90 d sweep or a 30 s worker tick.

C++ comments elsewhere (`imt5_client.h`, `OnDealAdd` body) warn that **without `PUMP_MODE_DEALS` the callback may stay silent** and that `CacheExecutedDeal` was added for the **dealer/execution** ring. Those comments do **not** license the C# collector to skip the sink. This product is a **source collector**, not a `SendTrade` dealer — `CacheExecutedDeal` is the wrong substitute (A59 L14). The collector must:

1. `DealSubscribe(this)` after `Connect` (pumped or not).
2. Implement `OnDealAdd` / `OnDealUpdate` / `OnDealDelete` as **validate → dedup → persist raw → outbox → commit** (architecture §12 live flow). Keep the callback light (A59 L7).
3. Keep `DealRequest*` as **historical backfill + periodic reconciliation**, not as the live clock.
4. Persist `TimeMsc`, not only `Time()`.

Until (1)+(2) exist in C#, live milliseconds are **structurally missed**, even if the manager would have fired `OnDealAdd`.

---

## 5. Milliseconds are dropped even on the deals that *are* pulled

`ReadDeals` maps only the second clock:

```416:430:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            rows.Add(new Mt5DealDto(
                (long)d.Deal(),
                (long)d.Login(),
                (long)d.Order(),
                (long)d.PositionID(),
                d.Symbol(),
                (DealAction)d.Action(),
                (DealEntry)d.Entry(),
                d.Volume(),
                (decimal)d.Price(),
                (decimal)d.Profit(),
                (decimal)d.Commission(),
                (decimal)d.Storage(),
                DateTimeOffset.FromUnixTimeSeconds(d.Time()),
                d.Comment()));
```

Same truncation in C++ `extractDeal` (`d.time = deal->Time()`). Domain `Mt5Deal.DealTime` is a single `DateTimeOffset` with no `TimeMsc` column. `Mt5DealDto` has no millisecond field.

Consequences:

- Two XAU fills in the same UTC second collapse to the same `DealTime`.
- Copy delay (`docs/risk.md`: **100 ms min / 2000 ms max** from **MT5 deal timestamp** to dest send) **cannot be measured** from stored time. Every deal looks like it happened on a 1 s tick.
- `DealRequest(from,to)` arguments are `ToUnixTimeSeconds()`. A fill at `to + 1 ms` is either in or out by second rounding, not by the manager’s `TimeMsc`.

Pump vs sink vs request, on the millisecond question:

| Path | Latency to persist | Time precision available | In C# product |
|---|---|---|---|
| `OnDealAdd` / `DealSubscribe` | Manager callback (ms) | `TimeMsc()` | **Absent** |
| `DealRequest*` batch | Minutes–hours after fill; window frozen | `Time()` seconds only, and C# throws `TimeMsc` away | **This is the live host** |
| Position pump cache | Open-position snapshot, not deal tape | `TimeCreate` seconds | Connect only |

---

## 6. Why this fails the product’s own live laws

Architecture §12 (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L525–571) requires **three** loops:

```text
Historical Backfill
+
Live Event Subscription
+
Periodic Reconciliation
```

Live flow is: MT5 event → validate → deduplicate → persist raw → transactional outbox → commit. Background workers then reconstruct/score/shadow.

Measured vs law:

| Law | Required | Measured C# host |
|---|---|---|
| Historical backfill | Checkpoint → fetch → upsert → persist checkpoint | −90 d one-shot, **no checkpoint** |
| Live event subscription | `DealSubscribe` / `OnDealAdd` | **0** |
| Periodic reconciliation | Lagged `DealRequest` vs `mt5_deals` | **0** after `Phase=done` |
| Persist-before-score | Callback stays light | Score runs **inside** the same host after the batch, blocking any future pull |
| Copy window | 100–2000 ms from deal time (`docs/risk.md`) | Frozen window + hour-scale batch + 20 s shadow ticker |

`CopyTradingHostedService` waits **8 s**, then ticks `GenerateShadowIntentsAsync` every **20 s**. That ticker is already **10×** the 2000 ms max copy delay. It consumes **stored** reconstructed trades, not `OnDealAdd`. Even a perfect sink would still miss the dest window until copy is event-driven.

`apps/mt5-worker/Worker.cs` is not a rescue: `while` + `AddDays(-30)` + `Task.Delay(30s)` + score of `{10001,10002,10003,99001}` only. Still a batch. Still no sink. Still 30 s >> 2 s.

Phase 1 deliverable “**live deals persisted**” is therefore **not met**. What is persisted is a **historical snapshot** whose trailing edge is `T0+1min` on a clock that already finished cataloging.

---

## 7. Concrete miss modes (do not treat as theory)

Given census shape **Achiever 8 groups / ~6512 logins + Starwave 10 groups / ~1948 logins**:

1. **Frozen `to`.** Host starts 12:00:00. `to = 12:01:00`. Catalog of 8460 accounts finishes 12:20. Deals RPCs finish 13:00. Scoring runs into the afternoon. Every fill after 12:01 is **out of the request range** and **never requested again**.
2. **In-flight same-second scalps.** IN at 12:00:00.400 and OUT at 12:00:00.900 store as the same second. Hold time becomes 0. First-3 / scalp features lie.
3. **No sink ⇒ no catch-up.** A deal that lands while `_gate` is held inside `DealRequestByGroup` is not queued. There is no `IMTDealSink` instance to receive it.
4. **Pump-none fallback.** If Achiever pump is refused (whitelist / 1012 class failures historically), `_pumpEnabled=false`. Groups still come from `GroupRequestArray`. Deals still come from `DealRequestByGroup`. Live events stay off. Host still reports `Connected` and walks the same 90-day batch.
5. **Score-only-if-deals.** Starwave `deals-done` / `scored 0` (prior P500 pins) means that broker contributed **census**, not a live book. A later live fill will not start scoring unless someone re-runs the host.
6. **Copy clock.** `docs/risk.md` max delay 2000 ms. Batch+score is 10³–10⁶× slower. SHADOW intents from `CopyTradingHostedService` are **stale by law**, not merely “slow.”

---

## 8. What must exist before “live source” can be claimed

Do **not** implement in this slot (report only). The no-miss shape, pinned:

1. **`NativeMt5BrokerConnector` becomes an `IMTDealSink` / `CIMTDealSink`.** After successful `Connect`, call `DealSubscribe(this)` (and unsubscribe on `Disconnect`). Do **not** pretend pump flags deliver deals.
2. **`OnDealAdd` / `OnDealUpdate` persist only.** Map `TimeMsc` onto `Mt5Deal`. Dedup `(broker_id, deal_ticket)`. Outbox. No reconstruction, no score, no FIX on the pump thread.
3. **Keep the −90 d (or checkpointed) `DealRequest*` walk as backfill + reconcile**, with an advancing `sync_checkpoints` cursor and overlap. Never freeze `to` at process start for the rest of the process lifetime.
4. **`LiveIngestHostedService` must not exit.** After backfill it should drain the sink/outbox and leave scoring to a **separate** worker. Today it is a one-shot ETL job misnamed “Live.”
5. **Do not use `CacheExecutedDeal`.** That is execution-side synthesis. This process does not `SendTrade` on the source.

Until those land, `/api/ingest/status` `Phase=done` means **“historical batch finished,”** not **“live milliseconds are captured.”**

---

## 9. Honesty close

| Claim someone might make | Measured |
|---|---|
| “Live ingest is running” | A hosted **one-shot −90 d batch + score**. Name is aspirational. |
| “Pump keeps us live” | Pump = `GROUPS\|USERS\|POSITIONS` at `Connect`. **No deal pump exists.** |
| “We will see the next gold fill” | Only if it falls inside the **frozen** `[T0−90d, T0+1min]` and the RPC returns it **before** the host exits. After `done`, **no**. |
| “Time is good enough for a 2 s copy window” | Stored time is **`d.Time()` seconds**. `TimeMsc` discarded. Copy ticker is **20 s**. |
| “C++ already solved it” | C++ **subscribes** `DealSubscribe` + `OnDealAdd`. C# **does not**. C++ also dropped `TimeMsc` in `extractDeal`. Neither stack is a finished collector; C# is the one the API host uses. |

**P501 CONFIRMED_SOURCE_MISS:** current ingest is a 90-day batch then a score loop; pump is connect/groups; **Deal sink / `OnDealAdd` is the no-miss path and is not wired.**
)
