# D24 — `FakeMt5BrokerConnector`: in-process demo book, not a broker

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D24_fake.md` |
| Agent | D24 (senior engineer, Fake connector only) |
| Date | 2026-08-18 |
| Assigned | Read `FakeMt5BrokerConnector.cs`. Write this report. Do not modify product source. |
| Primary file | `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` |
| Bytes / lines | **7049** / **170** (three types in one file) |
| SHA-256 | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` |
| Product source modified | **No.** This report is the only write. |
| Siblings (do not contradict) | A58, A79 (target spec), B04 (src/Mt5 inventory), C10 (plan-filter PASS), C42 (no live MT5), D14 (volume 10 000) |

Hash matches C10 / C42. The Fake did **not** grow a Manager, HTTP, event, or 5 000-account path between those reviews and this one.

---

## 0. Verdict (measured)

`FakeMt5BrokerConnector` is an **in-process list book** that implements the thin Application port `IMt5BrokerConnector`. It is the **only** implementor in the C# tree. `ConnectAsync` flips `_connected = true`. There is no socket, no `MT5APIManager64.dll`, no HTTP collector, no password, no server time, no orders, no ticks, no subscribe.

It is **useful** as a canned XAUUSD reconstruction fixture (18 closed deals, 4 logins, 2 broker codes). It is **not** a collector, **not** the A79 `InMemoryMt5BrokerConnector`, and **not** evidence that Achiever or StarwaveFX is connected.

| Surface | Classification |
|---|---|
| `FakeMt5BrokerConnector` | **EXISTS — demo only.** Production-wired. |
| `BrokerRegistry` (same file) | **EXISTS_NEEDS_REFACTOR.** String dictionary. |
| `DemoBrokerFactory` (same file) | **EXISTS.** Hard-coded 4/4/18/0 census. |
| `IBrokerConnector` (sibling file) | **DEAD.** Fake does **not** implement it. Correct. |
| `Mt5ManagerBrokerConnector` / HTTP adapter | **MISSING.** |
| A79 `InMemoryMt5BrokerConnector` under `tests/` | **MISSING.** This type is the stand-in, in the wrong place. |
| Dedicated Fake unit tests | **0 classes.** Integration seed test uses it only as a side effect. |

**Honest one-liner:** C# can demo-ingest **18 canned XAUUSD deals** across **4 logins** on **2 fake brokers**. C# cannot talk to MT5. Dashboard `Connected = true` is a literal in `EfDashboardQueries.GetBrokersAsync`, not `IsConnectedAsync`.

Do **not** treat a green worker log, a `dotnet test` seed fact, or an emerald Brokers cell as a live Manager proof. A100 G01 remains **FAIL**.

---

## 1. Method

1. Read `FakeMt5BrokerConnector.cs` in full (`FakeMt5BrokerConnector` + `BrokerRegistry` + `DemoBrokerFactory`).
2. Trace the Application port, ingestion, DI, seeder, worker, dashboard, volume converter, reconstructor, and the unused `IBrokerConnector`.
3. Recompute every default-tape `Lots` value and XAU P/L against `$100 × lots × Δprice`.
4. Diff the file against A79 acceptance and A79 forbidden list.
5. Hash the file; confirm SHA-256 matches C10 / C42.
6. Grep product C# for a second `: IMt5BrokerConnector` — **none**.
7. **Did not** edit `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, or `D:\Prop\mt5-sdk`.
8. **Did not** open a Manager session or claim a live attach.

---

## 2. File shape — three types, one path

```text
D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
  FakeMt5BrokerConnector   lines 6–68     : IMt5BrokerConnector
  BrokerRegistry           lines 70–87    : IBrokerRegistry
  DemoBrokerFactory        lines 89–170   static seed
```

A79 / A58 wanted:

```text
tests/Shared/Fakes/InMemoryMt5BrokerConnector.cs     # test-only
src/Mt5/Registry/Mt5BrokerRegistry.cs                # production registry
```

Measured: test double, production registry, and demo catalog share one compilation unit under `src/Mt5`. Infrastructure DI registers that Fake as the **only** broker. A79 §1 / §15 (“do not put the class in `src/Mt5` as the production connector”) is **violated by placement**, not by extra transport.

Sibling `IBrokerConnector` (`D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs`) is a second unused port over Domain entities. Fake does **not** implement it. A58: delete it. Dual-implementing it would freeze the wrong shape.

`Mt5BrokerOptions` (`Password`, `ProxyPassword`, `ApiKey`) is **unreferenced** by this file. The Fake constructor cannot accept host/login/password. Seeded IPs in `DemoSeeder` (`57.128.141.65` / `84.201.6.142`) are catalog paint.

`DeterministicGuid` exists in `src/Mt5/Utils` and is **unused** here. There is no `CatalogId`.

---

## 3. Port the Fake actually implements

Application contract (`D:\Prop\src\Application\Contracts\Mt5Contracts.cs`):

```53:63:D:\Prop\src\Application\Contracts\Mt5Contracts.cs
public interface IMt5BrokerConnector
{
    string BrokerCode { get; }
    Task ConnectAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);
    Task<bool> IsConnectedAsync(CancellationToken ct);
    Task<IReadOnlyList<Mt5GroupDto>> GetGroupsAsync(CancellationToken ct);
    Task<IReadOnlyList<Mt5AccountDto>> GetAccountsAsync(string? group, CancellationToken ct);
    Task<IReadOnlyList<Mt5DealDto>> GetDealsAsync(long login, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
    Task<IReadOnlyList<Mt5PositionDto>> GetPositionsAsync(long login, CancellationToken ct);
}
```

Coverage vs that thin port: **8/8 methods exist.** Coverage vs A58 collector: **orders / users / group-logins / server time / symbols / ticks / subscribe / LastError / CatalogId / fail-closed history = 0.**

DTOs have **no** `BrokerCode` / `CatalogId`. Identity is stamped later by `DealIngestionService` via `ITradingStore.ResolveBrokerIdAsync`. Logins and tickets are `long`, not SDK `uint64_t`. Volume is `ulong` native — correct family.

---

## 4. `FakeMt5BrokerConnector` behaviour

```14:67:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
    public FakeMt5BrokerConnector(
        string brokerCode,
        IEnumerable<Mt5GroupDto>? groups = null,
        IEnumerable<Mt5AccountDto>? accounts = null,
        IEnumerable<Mt5DealDto>? deals = null,
        IEnumerable<Mt5PositionDto>? positions = null)
    {
        BrokerCode = brokerCode;
        _groups = groups?.ToList() ?? new List<Mt5GroupDto>();
        _accounts = accounts?.ToList() ?? new List<Mt5AccountDto>();
        _deals = deals?.ToList() ?? new List<Mt5DealDto>();
        _positions = positions?.ToList() ?? new List<Mt5PositionDto>();
    }
    ...
    public Task ConnectAsync(CancellationToken ct)
    {
        _connected = true;
        return Task.CompletedTask;
    }
    ...
    public void AddDeal(Mt5DealDto deal) => _deals.Add(deal);
```

| Call | Measured behaviour | A79 / A58 / §62 |
|---|---|---|
| `ConnectAsync` | `_connected = true`; ignores `ct` | Should honour cancel; optional `FailNextConnect`. |
| `DisconnectAsync` | `_connected = false`; no subscribers to complete | No event channel exists. |
| `IsConnectedAsync` | returns the bool; starts **false** | Fine as a flag; never probes `/mt5/health`. |
| Query while disconnected | **returns the seed** | **FAIL-OPEN.** Must throw `Mt5BrokerUnavailableException`, not look like “zero/all groups.” |
| `GetGroupsAsync` | identity return of `_groups` (live list) | Snapshot copy; no plan filter. Plan-filter = **PASS** (C10). Encapsulation = **FAIL**. |
| `GetAccountsAsync(null)` | live `_accounts` list | Snapshot; census of **all** accounts. |
| `GetAccountsAsync(group)` | `a.GroupName == group` (ordinal), `ToList()` copy | Manager-style exact path, **not** `MT5_GROUP_*`. |
| `GetDealsAsync` | `Login == login && Time >= from && Time <= to`, copy | Inclusive `[from,to]` matches **current** Application contract. No sort (`Time`, `DealTicket`). No throw on incomplete. |
| `GetPositionsAsync` | filter by login, copy | Current book only. Default fixture is **empty**. |
| `AddDeal` | append; **no ticket uniqueness** | A79 default `DealCollision = Throw`. |
| Cancellation | accepted, never observed | Cancelled worker still “succeeds.” |
| Thread safety | raw `List<T>` | A79 requires lock / concurrent dictionaries. |
| Call log | **absent** | Cannot prove “one instance, 5 000 GetDeals.” |

`DealIngestionService.SyncBrokerAsync` calls `ConnectAsync` every cycle and never reads `IsConnectedAsync`. The Fake never disconnects itself. There is no watchdog.

### 4.1 Encapsulation leak

`GetGroupsAsync` and unfiltered `GetAccountsAsync` return the **backing lists**. A caller can `Add` / `Clear` the fixture through an `IReadOnlyList` cast. Filtered account/deal/position queries copy. This is not theoretical: ingestion walks the returned list; a future mutation would change the next poll.

### 4.2 Group discovery is not plan-filtered (reconfirm C10)

Zero tokens in this file: `MT5_GROUP`, `PlanMapping`, `EnabledForAnalysis`, `DefaultGroup`, `allowedGroups`, `Environment.GetEnvironmentVariable`. `GetGroupsAsync` takes `CancellationToken` only. Unmapped seed paths `demo\Maxmaster` and `real\standard` are still returned. **C10 PASS stands.** That is not a claim the catalog is a Manager-visible set (it is four names).

---

## 5. `BrokerRegistry`

```70:87:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
public sealed class BrokerRegistry : IBrokerRegistry
{
    private readonly Dictionary<string, IMt5BrokerConnector> _connectors;

    public BrokerRegistry(IEnumerable<IMt5BrokerConnector> connectors)
    {
        _connectors = connectors.ToDictionary(c => c.BrokerCode, StringComparer.OrdinalIgnoreCase);
    }

    public IMt5BrokerConnector Get(string brokerCode)
    {
        if (!_connectors.TryGetValue(brokerCode, out var connector))
            throw new KeyNotFoundException($"Unknown broker '{brokerCode}'.");
        return connector;
    }

    public IReadOnlyList<IMt5BrokerConnector> All() => _connectors.Values.ToList();
}
```

| Check | Result |
|---|---|
| Case-insensitive `Get` | **Yes** (`OrdinalIgnoreCase`). `"achiever"` resolves `ACHIEVER`. |
| Unknown code | **`KeyNotFoundException`** — fail closed. No silent empty connector. |
| Duplicate code at ctor | `ToDictionary` throws — fail fast. |
| `All()` | copy of `Values`; **order not pinned** to achiever-then-starwavefx. |
| `TryGet` / `Snapshot()` | **absent.** Dashboard invents `Connected = true`. |
| Secrets | registry holds connectors only; Fake has no password field. |

`DealIngestionService` is broker-agnostic (`registry.Get(brokerCode)`). The **worker is not**: it hard-codes `BrokerCodes.Achiever` then `StarwaveFx`, then `login >= 99000` to pick a code. A third Fake registered in DI would **not** be ingested. Opposite of A58 “foreach `registry.All`.”

---

## 6. `DemoBrokerFactory` — measured census

```89:93:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
public static class DemoBrokerFactory
{
    public const decimal VolumeScale = 10_000m;

    public static ulong Lots(decimal lots) => (ulong)decimal.Round(lots * VolumeScale, 0, MidpointRounding.AwayFromZero);
```

`VolumeScale = 10_000` matches `VolumeConverter.ManagerVolumeScale` and official `MTAPI_VOLUME_DIV`. Independently confirms D14. **Does not** repeat the `mt5_types.h` “hundredths” comment. `0.10` lot → `1_000`. `0.20` → `2_000`. `0.40` → `4_000`. `0.05` → `500`.

Clock origin: `t0 = 2026-06-01T08:00:00+00:00`. Each close is `open + 45 minutes`. StarwaveFX tape is `t0 + 1 day`.

Codes match `BrokerCodes` **uppercase** (`ACHIEVER` / `STARWAVEFX`), not A58 lowercase.

### 6.1 Census (exact)

| Broker | Groups | Accounts | Deals | Positions | Symbols |
|---|---:|---:|---:|---:|---|
| `ACHIEVER` | 3 | 3 | 12 | **0** | `XAUUSD` only |
| `STARWAVEFX` | 1 | 1 | 6 | **0** | `XAUUSD` only |
| **Total** | **4** | **4** | **18** | **0** | 1 |

`CreateDefault()` never passes `positions:`. Every `GetPositionsAsync` after the factory is `[]`. Account `Margin` / `Profit` fields therefore describe a book the position API does not have.

No `SeedFiveThousandAccounts`. No unmapped extras required by A79 (`demo\standard`, `real\vip`, `contest\internal`, `demo\default`, `contest\other`).

### 6.2 Groups vs §9

| Broker | Path | In §9 `MT5_GROUP_*`? | Returned? |
|---|---|---|---|
| ACHIEVER | `demo\Maxmaster` | **No** (§7 default) | **Yes** |
| ACHIEVER | `demo\yo-2step` | Yes | Yes |
| ACHIEVER | `contest\yo-2step` | Yes | Yes |
| STARWAVEFX | `real\standard` | **No** | **Yes** |

Plan-map intersection would drop Maxmaster + `real\standard`. It does not. Complementary bug (A40): Achiever is seeded with YoPips `yo-*` paths, and Starwave omits every `yo-*` path. That is **catalog composition**, not a fetch filter.

### 6.3 Accounts (canned; not derived from deals)

| Login | Group | Leverage | Balance | Equity | Margin | Free | Profit | Deals |
|---:|---|---:|---:|---:|---:|---:|---:|---:|
| 10001 | `demo\Maxmaster` | 100 | 10 000 | 10 240 | 200 | 9 800 | +240 | 6 (3 RT) |
| 10002 | `demo\yo-2step` | 100 | 5 000 | 4 820 | 150 | 4 670 | −180 | 6 (3 RT) |
| 10003 | `contest\yo-2step` | 200 | 25 000 | 25 000 | 0 | 25 000 | 0 | **0** |
| 99001 | `real\standard` | 100 | 8 000 | 8 110 | 80 | 7 920 | +110 | 6 (3 RT) |

Login **10003** is the empty-success fixture (C23): `GetDealsAsync` → `[]`, scorer `INSUFFICIENT_DATA`. The Fake does **not** invent a book. Correct.

### 6.4 Closed-round-trip generator

```150:169:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
    private static IEnumerable<Mt5DealDto> ClosedRoundTrip(...)
    {
        var vol = Lots(lots);
        var inAction = shortSide ? DealAction.Sell : DealAction.Buy;
        var outAction = shortSide ? DealAction.Buy : DealAction.Sell;
        yield return new Mt5DealDto(10_000 + seq, login, 20_000 + seq, positionId, "XAUUSD",
            inAction, DealEntry.In, vol, entry, 0, commission / 2, 0, open, "open");
        yield return new Mt5DealDto(10_500 + seq, login, 20_500 + seq, positionId, "XAUUSD",
            outAction, DealEntry.Out, vol, exit, profit, commission / 2, swap, open.AddMinutes(45), "close");
    }
```

Rules measured:

- One position id → one IN + one OUT. No scale-in, no reversal, no `InOut` / `OutBy`.
- `Action` is only `Buy`/`Sell` (`0`/`1`). **No** balance / credit / commission / SO-compensation deals (A37 / A79 §8.3).
- `Entry` is `In`/`Out` only (`0`/`1`).
- Commission split half/half; **swap only on OUT**.
- IN profit is always `0`; OUT carries the canned `profit`.
- Comments are literals `"open"` / `"close"`, not broker comments.
- Symbol is always `"XAUUSD"` — no `XAUUSDm` / `GOLD` alias (canonicalization is not done here; good).
- Tickets `10_000+seq` / `10_500+seq` are unique **inside one factory graph**. Dual-broker colliding tickets are **not** seeded.

Short side (login 10001, seq 2): IN=`Sell`, OUT=`Buy`. Correct hedge of a short.

### 6.5 Tape vs `$100 × lots × Δprice` (XAU 100 oz/lot)

| Login | Pos | Seq | Side | Lots | Native | Entry → Exit | Δ | Expected $ | Seeded profit | Match |
|---:|---:|---:|---|---:|---:|---|---:|---:|---:|---|
| 10001 | 501 | 1 | long | 0.10 | 1 000 | 2320.10 → 2335.40 | +15.30 | +153 | +153 | **Yes** |
| 10001 | 502 | 2 | short | 0.10 | 1 000 | 2338.00 → 2329.20 | −8.80 | −88 | −88 | **Yes** |
| 10001 | 503 | 3 | long | 0.10 | 1 000 | 2325.50 → 2341.80 | +16.30 | +163 | +163 | **Yes** |
| 10002 | 601 | 11 | long | 0.10 | 1 000 | 2320 → 2300 | −20 | −200 | −200 | **Yes** |
| 10002 | 602 | 12 | long | 0.20 | 2 000 | 2300 → 2275 | −25 | −500 | −500 | **Yes** |
| 10002 | 603 | 13 | long | 0.40 | 4 000 | 2275 → 2240 | −35 | −1400 | −1400 | **Yes** |
| 99001 | 701 | 21 | long | 0.05 | 500 | 2340 → 2348 | +8 | +40 | +40 | **Yes** |
| 99001 | 702 | 22 | long | 0.05 | 500 | 2348 → 2356 | +8 | +40 | +40 | **Yes** |
| 99001 | 703 | 23 | long | 0.05 | 500 | 2356 → 2362 | +6 | +30 | +30 | **Yes** |

The **per-deal profit field is internally consistent** with a 100 oz XAU lot. The factory does **not** compute it; it hard-codes the same number.

Login 10002 lots `0.10 → 0.20 → 0.40` after sequential losers is a **martingale / lot-escalation** fixture (scores `RISK_BLOCKED` in `SeedingAndStoreTests`). Three independent position ids, not one scaled-in position. Reconstruction therefore emits **three** completed trades, not one averaging-down lifecycle. That is enough to trip lot-escalation; it is **not** an A21 scale-in / reversal golden.

### 6.6 Account snapshot vs tape — disconnected

Closed P/L should sit in **balance**, not floating profit, when the position book is empty.

| Login | Tape net (profit + comm + swap) | Account.Profit | Account.Equity − Balance | Positions |
|---:|---:|---:|---:|---:|
| 10001 | 153−88+163 −3.5 −0.9 = **+223.6** | +240 | +240 | 0 |
| 10002 | −200−500−1400 −7 = **−2107** | −180 | −180 | 0 |
| 10003 | 0 | 0 | 0 | 0 |
| 99001 | 40+40+30 −1.8 = **+108.2** | +110 | +110 | 0 |

10001 / 99001 look like “gross deal profit, ignore commission, pretend still open.” 10002’s −180 has **no** relation to the −2107 martingale tape. **Account rows are decorative.** Tests that assert ledger ↔ snapshot equality against this factory will fail. Do not treat equity 10 240 as a measured Achiever balance.

---

## 7. Two factory graphs + 30-day worker window

`DependencyInjection.AddTraderIntelligence` and `DemoSeeder.SeedAsync` each call `DemoBrokerFactory.CreateDefault()` **separately**.

| Graph | Who holds it | Used for |
|---|---|---|
| DI singletons | `IMt5BrokerConnector` ×2 + `BrokerRegistry` | Worker 30 s loop |
| Seeder locals | discarded after `SeedAsync` | First-boot year-window ingest |

`AddDeal` on the DI singleton **cannot** change rows the seeder already upserted.

Windows:

| Caller | `from` | `to` | Hits June 2026 tape on 2026-08-18? |
|---|---|---|---|
| `DemoSeeder` | 2026-01-01 | 2026-12-31 | **Yes** (inclusive) |
| `Worker` | `UtcNow.AddDays(-30)` ≈ 2026-07-19 | `UtcNow.AddMinutes(1)` | **No** |

After first seed, the worker’s `GetDealsAsync` returns **empty**. Positions stay empty. That is a **Fake + caller window** mismatch, not live ingest. If `Brokers.Any()` skips re-seed and the deal table is empty, the 30 s loop will **not** backfill June deals.

`GetDealsAsync` itself is not wrong: `d.Time >= from && d.Time <= to`. Inclusive both ends matches today’s Application contract. A58 half-open `[from, to)` is **not** implemented (no A58 adapter).

---

## 8. Production wiring (why this is not “test-only”)

| Consumer | Path | How it uses the Fake |
|---|---|---|
| DI | `D:\Prop\src\Infrastructure\DependencyInjection.cs` 31–34 | Always `CreateDefault()` → two singletons. No slot binder, no `Mode=local`, no env. |
| Seeder | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` 124–130 | Second pair → `SyncBrokerAsync` both codes, year window. |
| Ingestion | `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Connect → groups → accounts(null) → per-login deals + replace positions. |
| Worker | `D:\Prop\apps\mt5-worker\Worker.cs` | Same sync every 30 s, last 30 days; scores hard-coded logins `10001,10002,10003,99001`. |
| Dashboard | `EfDashboardQueries.GetBrokersAsync` | `Connected` literal **`true`**. Never calls `IsConnectedAsync`. |
| Integration | `tests/Integration/SeedingAndStoreTests.cs` | `DemoSeeder` side effect. Asserts `Mt5Groups.Count() > 2`, `10001` has 3 completed XAU, `10002` is `RISK_BLOCKED`. |
| Unit | `tests/Unit` | **No** `TraderIntelligence.Mt5` reference. **0** Fake tests. |

`tests/Integration/TraderIntelligence.Tests.Integration.csproj` **does** reference `src/Mt5` (needed by the seeder). B04’s “Integration does not reference Mt5” is **stale**. There is still **no** `InMemoryMt5BrokerConnectorTests` / `InMemoryFiveThousandAccountSeedTests` / `DualBrokerIsolationTests`.

---

## 9. A79 acceptance — measured scorecard

A79 §16. Implementation is **not** done. Spec file exists; this type is a thinner, production-wired cousin.

| A79 acceptance item | Measured |
|---|---|
| Type lives under `tests/` only | **FAIL** — `src/Mt5/Connectors/` |
| Implements Application `IMt5BrokerConnector` (current) | **PASS** (thin port) |
| Implements A58 port (`CatalogId`, orders, subscribe, server time, …) | **FAIL** |
| `SeedFiveThousandAccounts(5000)` < 200 ms + invariants | **FAIL** — method absent; census = 4 |
| Dual instance + same numeric logins isolated by `CatalogId` | **PARTIAL** — two instances exist; no `CatalogId`; factory does not plant colliding tickets |
| `GetGroupsAsync` returns unmapped groups | **PASS** (Maxmaster + `real\standard`) |
| `GetDeals` fail-closed on incomplete page | **FAIL** — always success + list |
| `SubscribeAsync` broadcasts user/order/position; deals stay polled | **FAIL** — no subscribe |
| `SyncBrokerAsync` + in-memory store upserts 5 000 accounts (measured) | **FAIL** — 4 accounts |
| No product source in `src/` required to land the fake | **FAIL** — this **is** product source |
| No live Manager / passwords in fixtures | **PASS** (hermetic lists) |

Until a test prints a measured **5 000 upsert** count, §69.3 remains **FAIL**. Seeding 4 dictionaries is not “accounts synchronized.”

### 9.1 A79 forbidden list vs this file

| Forbidden | Here |
|---|---|
| Put the class in `src/Mt5` as the production connector | **Committed.** DI registers it. |
| Filter groups by `MT5_GROUP_*` | **Not done** (good). |
| Return empty list when disconnected | Worse: returns the **full seed** (fail-open, not empty). |
| Return a partial deal page as success | No paging; cannot simulate `FailAfterDealPages`. |
| Auto-emit `DealAdd` on `AddDeal` | N/A — no events. |
| `CreateUser` / `Deposit` / `SendTrade` on the port | **Absent** (good). `AddDeal` is a public helper, not a port member. |
| Convert volume to lots / cTrader qty on the wire | `Lots()` is seed-only; DTO carries `VolumeNative`. **Good.** |
| Canonicalize `GOLD` → `XAUUSD` inside the connector | Not done. Symbol is already `XAUUSD`. |
| Open HTTP/TCP or read `.env` passwords | **Absent** (good). |
| One global static book for both brokers | **Two instances** (good). |
| Sleep per account in a 5k seed | No 5k seed. |
| Claim “5k sync done” because a seed method exists | **Do not.** Method does not exist. |

---

## 10. What this Fake is allowed to prove

Valid (if tests actually lock them):

1. `DealIngestionService` walks **all** seeded groups/accounts, including unmapped paths.
2. Dual-broker registry lookup is case-insensitive; unknown code throws.
3. Native volume `10_000 = 1.00` lot round-trips through reconstruction (`VolumeConverter.Manager`).
4. Login 10001 → three completed XAU trades → early-score eligible, **not** `LIVE`.
5. Login 10002 lot escalation → `RISK_BLOCKED`.
6. Login 10003 empty deals → empty success, `INSUFFICIENT_DATA` (C23).
7. Deal upsert idempotency is a **store** fact (`SeedingAndStoreTests`), not a Fake uniqueness fact (`AddDeal` still allows duplicates).

Invalid (do not claim):

1. Achiever / StarwaveFX Manager connected.
2. Group discovery against `GroupTotal` / `GroupNext`.
3. 5 000-account sync.
4. Live deal pump / SSE.
5. Complete-history-or-fail.
6. Position book / margin consistency.
7. Tick-based MFE/MAE.
8. Go-live gate G01.

---

## 11. Tests that do **not** exist

A79 §14 names. Grep of `tests/**/*.cs` for `FakeMt5` / `InMemoryMt5` / `DemoBrokerFactory` / `BrokerRegistry` as SUT: **no dedicated class.** Integration seed is the only consumer.

| Required class | On disk |
|---|---|
| `InMemoryMt5BrokerConnectorTests` | **MISSING** |
| `InMemoryGroupDiscoveryTests` | **MISSING** |
| `InMemoryAccountBookTests` | **MISSING** |
| `InMemoryFiveThousandAccountSeedTests` | **MISSING** |
| `InMemoryDealWindowTests` | **MISSING** |
| `InMemoryVolumeAndEnumTests` | **MISSING** (volume is covered on `VolumeConverter`, not this Fake) |
| `InMemoryEventStreamTests` | **MISSING** |
| `InMemoryFaultTests` | **MISSING** |
| `InMemoryRegistryTests` | **MISSING** |
| `Mt5BackfillRestartTests` | **MISSING** |
| `Mt5LiveIngestIdempotencyTests` | **MISSING** |
| `DualBrokerIsolationTests` | **MISSING** |

Current proof of the Fake is **source inspection** + one integration seed fact (`Count > 2` groups, 10001 has 3 XAU, 10002 blocked).

---

## 12. Acceptance of this D24 pass

| # | Question | Answer |
|---|---|---|
| 1 | Is there a live C# MT5 connector? | **No.** This Fake is the only `IMt5BrokerConnector`. |
| 2 | Does `ConnectAsync` talk to a broker? | **No.** Bool flip. |
| 3 | Is group discovery plan-filtered? | **No** (C10 reconfirm). |
| 4 | Is volume scale 10 000? | **Yes** (`DemoBrokerFactory.VolumeScale`). |
| 5 | How many canned deals / logins / groups? | **18 / 4 / 4.** Positions **0**. |
| 6 | Is the per-deal XAU P/L internally consistent? | **Yes** ($100 × lots × Δ). Account snapshots are **not**. |
| 7 | Does the worker re-ingest the June tape on 2026-08-18? | **No.** 30-day window misses `t0`. |
| 8 | Does A79 `InMemoryMt5BrokerConnector` exist under `tests/`? | **No.** |
| 9 | Product source changed by this agent? | **No.** |

---

## 13. Files read (not modified)

- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs`
- `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs`
- `D:\Prop\src\Mt5\Utils\DeterministicGuid.cs`
- `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj`
- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Dashboard\DashboardModels.cs`
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\src\Domain\Enums\DealAction.cs`, `DealEntry.cs`
- `D:\Prop\src\Domain\Volume\VolumeConverter.cs`
- `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs`, `NormalizedDeal.cs`
- `D:\Prop\src\Domain\Entities\Mt5Deal.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`, `Program.cs`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj`
- `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj`
- Sibling reports A58, A79, A100, B04, C10, C23, C42, D14

**Written:** `D:\Prop\reports\swarm\20260818\D24_fake.md` (this file).  
**Product source modified:** none.
