# P500_S035 — Live ingest `positions=0` vs Manager census ~1984

**Date:** 2026-08-18  
**Slot:** P500_S035  
**Scope:** Why `/api/ingest/status` (and the live book consumers) show **positions=0** while `LiveBrokerProbe` / Manager census measured **1984** open positions. Read `ReplaceBrokerPositionsAsync` and the ingest host.  
**Product source:** not edited.

---

## 1. Verdict

The Manager book is **not** empty. Probe JSON at `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (`utc=2026-08-18T08:42:16Z`) measured Achiever **1506** + StarwaveFX **478** = **1984** via the same `GetGroupPositionsAsync("*")` the ingest path calls.

Live ingest still reports **positions=0** for two independent, currently true reasons:

1. **Status lie (always).** `BrokerLiveStatus.Positions` is serialized on `/api/ingest/status` but **never assigned** anywhere in product C#. It stays the `int` default **0** even if `ReplaceBrokerPositionsAsync` just wrote 1984 rows.
2. **Book can actually be empty.** Positions are fetched **after** a 90-day deal pull. If that throw/timeout happens, the host **skips** the replace. If the group-position RPC fails, the connector returns **empty** and `ReplaceBrokerPositionsAsync` **wipes** `mt5_positions_current` for the broker. The API uses **in-memory EF** (`DATABASE_URL` placeholder / no `ConnectionStrings:TraderIntelligence`), so a restart also yields an empty book.

Open-position copy cannot run off an empty (or unseen) book. Even a full 1984-row table would not help today: **nothing in dashboard/copy/scoring reads `Mt5Positions`**. `RealCopyEnabled` is forced **false**. This is not a Manager-empty problem.

---

## 2. Census vs ingest (measured)

| Source | When | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| Manager probe (`LiveBrokerProbe`) | 08:42Z | 18 | 8460 | **1984** (1506 + 478) |
| `CREDENTIALS_AND_COPY_STATUS.md` | same census | 18 | 8460 | **1984** |
| `/api/ingest/status` → `brokers[].Positions` | any time | (catalog) | (catalog) | **0 (never written)** |

Probe path (`D:\Prop\tools\LiveBrokerProbe\Program.cs` L27–29, L53):

```text
var positions = connector is IMt5BulkPositionReader bulk
    ? await bulk.GetGroupPositionsAsync("*", CancellationToken.None)
    : Array.Empty<Mt5PositionDto>();
// ...
openPositions = positions.Count
```

Ingest uses the **same** bulk call (`DealIngestionService.SyncBrokerAsync` L82–85). The 1984 figure is therefore a live Manager fact, not a probe-only trick.

---

## 3. Ingest host never publishes a position count

`GET /api/ingest/status` is `runtime.Snapshot()` (`apps/api/Program.cs` L59). Snapshot includes `b.Positions` (`LiveRuntimeStatus.cs` L13, L55).

Assignment sites for `st.Positions` / `status.Positions` in product `*.cs`: **zero**.

`LiveIngestHostedService` writes:

| Phase | Fields set | Positions |
|---|---|---|
| catalog | `Groups`, `Accounts`, `Connected`, `Phase` | **untouched → 0** |
| deals | `DealsInserted = SyncBrokerAsync(...)` (an `int` deal count only) | **untouched → 0** |
| scoring | `Scored` | **untouched → 0** |
| any catch | `LastError`, `Phase=failed` | **untouched → 0** |

Catalog result is `BrokerSyncResult(groups.Count, accounts.Count, 0, 0)` (`DealIngestionService.cs` L51). The fourth field **is** `Positions`, hardcoded **0**. The host copies only `catalog.Groups` / `catalog.Accounts` (`LiveIngestHostedService.cs` L56–58).

`SyncBrokerAsync` **does** pull and persist positions, but its return type is `Task<int>` = **deals inserted only** (L97). The host logs `"{Broker} deals inserted={Deals}"` and never logs a position count.

Manual `POST /api/ops/resync` (`Program.cs` L111–146) repeats the same omission: sets `Groups` / `Accounts` / `DealsInserted` / `Scored`, never `Positions`.

**Implication:** a green catalog (18 groups / 8460 accounts) with `Positions: 0` is the **designed** status shape today. It does **not** prove the Manager book is empty.

---

## 4. What `SyncBrokerAsync` actually does

`D:\Prop\src\Application\Ingestion\DealIngestionService.cs` L54–98:

1. `ConnectAsync`
2. **`SyncCatalogAsync` again** (groups + accounts upsert; still returns Positions=0)
3. Re-fetch groups + accounts
4. Deals: `IMt5BulkDealReader` → per-group `GetGroupDealsAsync` over `[now-90d, now+1m]`, else per-login `GetDealsAsync`
5. Positions:
   - if `IMt5BulkPositionReader` (native **is**): `GetGroupPositionsAsync("*")` → `ReplaceBrokerPositionsAsync(brokerId, positions)`
   - else: per-login `GetPositionsAsync` → `ReplacePositionsAsync`

Native implements both bulk interfaces (`NativeMt5BrokerConnector.cs` L24). Live ingest always takes the `"*"` bulk path.

Host order (`LiveIngestHostedService.cs`):

1. All brokers: catalog
2. All **connected** brokers: `SyncBrokerAsync` (catalog + **90-day deals** + positions)
3. All connected brokers: score `ListLoginsWithDealsAsync` only

Positions are **last inside step 2**. A deal exception is caught at L89–94: *“Catalog data is kept.”* Positions are **not** kept because they have not run. Phase becomes `failed`. `Positions` stays 0.

90-day deal pull is the long pole: `GetGroupDealsCore` slices 14-day windows (`NativeMt5BrokerConnector.cs` L355–365). One `DealRequestByGroup` hard-fail (`!= OK / OK_NONE / NOTFOUND`) throws (`L308–309`) and aborts the whole `SyncBrokerAsync`, including the position replace.

---

## 5. `ReplaceBrokerPositionsAsync` — full-book replace, including empty

`D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` L475–501:

```text
var existing = _db.Mt5Positions.Where(p => p.BrokerId == brokerId);
_db.Mt5Positions.RemoveRange(existing);
foreach (var p in positions)
    _db.Mt5Positions.Add(...);
await _db.SaveChangesAsync(ct);
```

| Property | Fact |
|---|---|
| Scope | **Entire broker**, not per-login |
| Empty input | Deletes every row for that `brokerId`, inserts **nothing** |
| Return | `Task` — no count back to host/status |
| Table | `mt5_positions_current` (`TraderDbContext` L68) |
| Cap | None (`Take`/`Skip` absent) — not a 200-slice |

Per-login `ReplacePositionsAsync` (L116–142) is the same shape but scoped `BrokerId + Login`. Live native never uses it.

There is **no** “keep previous book if fetch failed” guard. Empty list is treated as “true empty book.”

---

## 6. Connector can fetch 1984 — and can also swallow a miss

`GetGroupPositionsCore` (`NativeMt5BrokerConnector.cs` L336–352):

1. `PositionRequestByGroup(mask, arr)` — network RPC, **no pump required** (`MT5APIManager.h` L534)
2. On non-OK/OK_NONE/NOTFOUND: `PositionGetByGroup` — **cache only**, needs `PUMP_MODE_POSITIONS` (`h` L286)
3. Still failing: **`return Array.Empty<Mt5PositionDto>()`** — **no throw, no `LastError`**

Connect tries pump (`GROUPS|USERS|POSITIONS`) then falls back to `PUMP_MODE_NONE` (`L89–111`). Request-first is correct (probe proved `"*"` works). The silent-empty fallback is the hazard: ingest then **wipes** the local book.

Per-login `GetPositionsCore` (L319–333) has the same empty-on-error pattern.

Connect **does** pump positions when the first `Connect` succeeds. C# never reads the pump cache unless Request fails. Probe success means Request works on this LAN; ingest should see the same 1984 **if it reaches the call**.

---

## 7. Persistence is in-memory — book dies with the process

`DependencyInjection.cs` L23–28:

```text
var connection = configuration.GetConnectionString("TraderIntelligence")
                 ?? configuration["DATABASE_URL"];
if (string.IsNullOrWhiteSpace(connection) || connection.Contains("<SECRET>"))
    UseInMemoryDatabase("trader-intelligence-live");
```

`apps/api/appsettings.json` has `ConnectionStrings:Postgres`, **not** `TraderIntelligence`. `CREDENTIALS_AND_COPY_STATUS.md`: `DATABASE_URL` is a **placeholder**. API therefore uses **in-memory**.

Consequences:

- Probe JSON on disk ≠ API `Mt5Positions` table.
- Catalog 8460 traders live only in that process heap.
- Restart / rebuild → book empty again.
- `/ready` counts brokers/groups/accounts only (`Program.cs` L84–89) — **not** positions.

---

## 8. Nobody reads the position book (copy cannot use it)

`Mt5Position` / `Mt5Positions` writers:

- `EfTradingStore.ReplacePositionsAsync`
- `EfTradingStore.ReplaceBrokerPositionsAsync`

Readers in product (dashboard, risk, shadow, scoring, API maps): **none**.  
`EfDashboardQueries.GetOverviewAsync` hardcodes three zeros (L44–46). Trader rows hardcode open-count `0` (L118). There is no `/api/positions`. Reconstruction/scoring use **deals**, not `mt5_positions_current`.

Copy / capital:

| Gate | State |
|---|---|
| `LiveRuntimeStatus.RealCopyEnabled` | **false** (DI comment: NewOrderSingle not implemented) |
| `FEATURE_COPY_TRADING_ENABLED` | **false** (`Program.cs` L76) |
| `35=D` NewOrderSingle | **absent** (`SAFE_BY_ABSENCE`) |
| Shadow | deal-reconstructed demo fills, not live open book |

So even if ingest wrote 1984 rows and status told the truth, **open-position copy still has no reader and no send path**. An empty book makes that copy **impossible**; a full book would still not copy today.

---

## 9. Why this blocks “copy currently open source positions”

Intended pipeline for open-book copy:

```text
Manager PositionRequestByGroup("*")
  → IReadOnlyList<Mt5PositionDto>   // probe: 1984
  → ReplaceBrokerPositionsAsync     // may never run / may wipe
  → mt5_positions_current           // in-memory; unread
  → LiveRuntimeStatus.Positions     // NEVER SET → 0
  → copy / shadow / risk open-book  // NO READER
  → cTrader NewOrderSingle          // DOES NOT EXIST
```

Breaks at every stage after the Manager RPC. The first **visible** break is ingest `positions=0`. The first **data** break is “positions run after 90-day deals, or wipe-on-empty, or in-memory.” The first **product** break is “no consumer of `Mt5Positions`.”

Copying **open** positions from deals-only reconstruction is the wrong book: deals are a 90-day history window; open tickets live in `PositionRequest*`.

---

## 10. Root-cause list (honest)

| # | Cause | Makes status 0? | Makes DB book empty? | Manager actually empty? |
|---|---|---|---|---|
| A | `BrokerLiveStatus.Positions` never assigned | **Yes, always** | No | No |
| B | `SyncCatalogAsync` hardcodes `Positions=0`; host only uses catalog for status | Yes (catalog phase) | No | No |
| C | `SyncBrokerAsync` returns deal count only | Yes | No | No |
| D | Positions after 90-day deals; deal throw skips replace | Yes | **Yes** (never written) | No |
| E | `GetGroupPositionsCore` empty-on-error + replace wipe | Yes | **Yes** (deleted) | No |
| F | In-memory EF; restart | Yes | **Yes** | No |
| G | Phase still `catalog` / `deals` | Yes | Yes (not yet) | No |
| H | Manager has 0 opens | — | Yes | Only if probe also 0 (it is **1984**) |

**Primary explanation of the number on the ingest card:** A+B+C.  
**Primary explanation if `mt5_positions_current.Count()==0` after `deals-done`/`done`:** D, E, or F.  
**Not the explanation:** a 200-row cap (removed), missing `"*"` API (probe used it), or FakeMt5 (DI throws without real passwords).

---

## 11. What a fix would look like (not applied)

Product not edited. If a later slot changes this:

1. After `GetGroupPositionsAsync`, set `st.Positions = positions.Count` and log it. Do **not** treat catalog’s hardcoded 0 as the book.
2. Return `(deals, positions)` from `SyncBrokerAsync` (or persist then `CountAsync` the table).
3. Do **not** call `ReplaceBrokerPositionsAsync` on empty unless Request returned `OK`/`OK_NONE` with `Total()==0`. Distinguish “RPC failed” from “broker flat.” Throw or keep previous book on fail.
4. Optionally snapshot positions **before** the 90-day deal loop so a deal timeout does not leave the open book blank.
5. Persist to Postgres (`ConnectionStrings:TraderIntelligence`) so the book survives process recycle.
6. Add a reader (`/ready` position count, dashboard, copy source) before any open-position copy work.
7. Keep `RealCopyEnabled=false` / no `35=D` until the book is measured non-zero **and** a send path exists.

---

## 12. Safety

- This slot did **not** attach Manager, send FIX, or edit product.
- `35=D` NewOrderSingle: **absent**.
- `RealCopyEnabled`: **false**.
- Risk to capital from this report: **NONE**.

---

## 13. Files read

| Path | Why |
|---|---|
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | catalog/broker sync; bulk `"*"` replace |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `ReplaceBrokerPositionsAsync` wipe+insert |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | status fields; deals-then-positions; catch skips book |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | `Positions` default 0, snapshot |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Request/Get + empty-on-error |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | in-memory vs Npgsql |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | no `Mt5Positions` read |
| `D:\Prop\apps\api\Program.cs` | `/api/ingest/status`, resync, recon zeros |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | 1984 measurement path |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | 1506+478 |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | census table |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | Request vs Get |

---

## 14. One-liner

**Manager has ~1984 opens; live ingest `positions=0` because the status field is never written, and the real book is either not yet replaced (deals-first), wiped on silent empty, or sitting unread in an in-memory table — open-position copy cannot start from that.**
