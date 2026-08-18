# C35 — Infrastructure references Mt5: first-useful-version layering

| Field | Value |
|---|---|
| Agent | C35 (senior engineer, layering only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\C35_layering.md` |
| Assigned question | Infra references Mt5. Is that acceptable for first useful version? |
| Product source modified | **No.** This report is the only write. |
| Workspace | `D:\Prop` |
| Binding spec | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§5–6, 54/A54, 66–67, 69, 71, 73.B |
| Adjacent (read, not rewritten) | A11 (stale graph), A54, A57, A58, A79, A80, B02, B03, B04, B05, B24, C05, C10, `docs/architecture.md` |

Classification (architecture §73.B): `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict (honest, one screen)

**Yes — acceptable for the first useful *demo* slice. No — not the production topology, and not a free pass once a real Manager adapter exists.**

| Question | Answer |
|---|---|
| Does `TraderIntelligence.Infrastructure` reference `TraderIntelligence.Mt5`? | **Yes.** Measured `ProjectReference` + `deps.json` + `TraderIntelligence.Mt5.dll` in Infra `bin/`. |
| Circular project graph? | **No.** Mt5 does not reference Infrastructure. C05 already measured this. |
| Does persistence (`TraderDbContext` / `EfTradingStore` / `EfDashboardQueries`) import Mt5 types? | **No.** Those files talk Application DTOs + Domain entities only. |
| Why does the reference exist? | Composition only: `AddTraderIntelligence` and `DemoSeeder` construct `DemoBrokerFactory` / `FakeMt5BrokerConnector` / `BrokerRegistry`. |
| Does §69 (12-item first useful version) require onion-pure Infra? | **No.** §69 is an operating bar, not a NetArchTest bar. |
| Does the current reference block any of the 12 items? | **No.** Removing it now does not connect a broker, persist a deal, or log on QUOTE. |
| Is it the intended long-term graph? | **No.** Hosts should compose adapters. Persistence should not pull the collector assembly. |
| When does it become **not** acceptable? | The day `TraderIntelligence.Mt5` grows a native Manager / P/Invoke / `MT5APIManager64.dll` load — because API and FIX-worker already load that assembly **transitively** without declaring it. |
| Classification of the edge | **EXISTS_NEEDS_REFACTOR** (composition leak). **Not** `UNSAFE`. **Not** a §69 FAIL. |

**Do not stop Phase 1–5 work to invert this.** Invert (or split the Mt5 assembly) **before** a Windows-only connector lands in the same project Infrastructure already references.

Contrast that is already correct in this tree: **Infrastructure does not reference `Fix.CTrader`.** FIX composition stays on `apps/fix-worker`. MT5 composition was stuffed into `AddTraderIntelligence` instead of `apps/mt5-worker`. That is the whole defect.

---

## 1. Method

Read, no edits under `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `D:\Prop\mt5-sdk`.

| Source | What was measured |
|---|---|
| All six Infrastructure product files | usings + ctor graph |
| All four Mt5 product files + csproj | implementors, native deps |
| Application `Mt5Contracts.cs`, `DealIngestionService.cs` | port ownership |
| Host / test csprojs | who *declares* Mt5 vs who *inherits* it |
| `Infrastructure/bin/Debug/net8.0/TraderIntelligence.Infrastructure.deps.json` | restore-time dependency |
| Architecture §§6, 66, 69, 71 | intended folders vs operating bar |
| A11, A54, A57, B03, B04, B05, C05 | prior layering notes (A11 graph is **stale**) |

Grep:

- `using TraderIntelligence.Mt5` under `src/Infrastructure` → **2 files** (`DependencyInjection.cs`, `DemoSeeder.cs`).
- `ProjectReference` to `TraderIntelligence.Mt5.csproj` → Infrastructure, mt5-worker, Integration tests (plus a swarm scratch csproj). **Not** Application, Domain, Fix.CTrader, API (direct), FIX-worker (direct), Unit tests.

Did not start hosts. Did not run `dotnet` against product. Did not add ArchUnit / NetArchTest.

---

## 2. Measured graph (2026-08-18)

### 2.1 Project references (compile-time DAG)

```text
Domain                         (0 project refs)
   ▲
   │
Application  → Domain only     EXISTS_AND_GOOD (B02 P2)
   ▲
   ├── Mt5           → Domain, Application     (adapter)
   ├── Fix.CTrader   → Domain, Application     (adapter; B05 notes this Application ref)
   └── Infrastructure → Domain, Application, **Mt5**   ← this report
            ▲
            ├── apps/api          → Domain, Application, Infrastructure
            │                       (Mt5 is **transitive**, not declared)
            ├── apps/mt5-worker   → Domain, Application, Infrastructure, Mt5
            ├── apps/fix-worker   → Domain, Application, Infrastructure, Fix.CTrader
            │                       (Mt5 is **transitive**, not declared)
            ├── tests/Integration → Domain, Application, Infrastructure, Fix.CTrader, Mt5
            └── tests/Unit        → Domain, Application, Fix.CTrader
                                    (no Infrastructure, no Mt5)
```

**Back-edge Mt5 → Infrastructure: none.** Cycle question is closed (C05 §4).

A11 §7 drew:

```text
Infrastructure  →  Api, Mt5Worker, FixWorker, Tests.Integration
Mt5             →  Mt5Worker
```

That snapshot is **stale**. A11 did not see `Infrastructure → Mt5`. Use this file for the edge.

### 2.2 Evidence of the reference

`D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` (1035 B, SHA-256 `4DABF29CA934261CFC46C72514CB7AA04D5E8F9CC8FFAC1BA051BF0CD0668EED`):

```xml
<ItemGroup>
  <ProjectReference Include="..\Domain\TraderIntelligence.Domain.csproj" />
  <ProjectReference Include="..\Application\TraderIntelligence.Application.csproj" />
  <ProjectReference Include="..\Mt5\TraderIntelligence.Mt5.csproj" />
</ItemGroup>
```

`D:\Prop\src\Infrastructure\bin\Debug\net8.0\TraderIntelligence.Infrastructure.deps.json`:

```text
TraderIntelligence.Infrastructure/1.0.0
  dependencies:
    … EF / Npgsql / Redis …
    TraderIntelligence.Application: 1.0.0
    TraderIntelligence.Domain: 1.0.0
    TraderIntelligence.Mt5: 1.0.0
```

Same `bin/Debug/net8.0/` and `bin/Release/net8.0/` folders contain `TraderIntelligence.Mt5.dll`. Any host that references Infrastructure **loads the collector assembly** even if its csproj never names Mt5.

### 2.3 Who actually uses Mt5 types

| File | Bytes | SHA-256 | `using TraderIntelligence.Mt5`? | Role |
|---|---:|---|---|---|
| `DependencyInjection.cs` | 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | **yes** | registers two `FakeMt5BrokerConnector` + `BrokerRegistry` |
| `Seeding/DemoSeeder.cs` | 4942 | `139D8F872DC473F0C5381AF2393BDBBE60E1D9A2A5179DD1D1737E04CCC00BEF` | **yes** | **second** `CreateDefault()` + `new BrokerRegistry` + `new DealIngestionService` (C05 split graph) |
| `Persistence/EfTradingStore.cs` | 9020 | `05103CE5D8F73CD8096E949F736D21594F7FA0033AEA179C9CB47C0EE1D673DB` | no | Application `Mt5*Dto` → Domain entities |
| `Persistence/TraderDbContext.cs` | 5951 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | no | Domain `DbSet`s |
| `Dashboard/EfDashboardQueries.cs` | 7407 | `37A4DDD23305708566888F0BBE2A6CC5DE253FB7151BDEE848195DE715EF4ACE` | no | EF reads |
| `TraderIntelligence.Infrastructure.csproj` | 1035 | `4DABF29…` | n/a | the edge itself |

If the two composition files moved to a host (or an `AddDemoMt5` extension in the Mt5 project), the Infrastructure csproj reference would have **zero remaining consumers** and could be deleted.

### 2.4 What Mt5 is today (why the leak is cheap)

`D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj` (419 B, SHA-256 `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F`):

- `net8.0` class library.
- Project refs: Domain + Application only.
- **No** `HttpClient` package, **no** `AllowUnsafeBlocks`, **no** native items, **no** `DllImport` in the project.

On-disk product C# (B04 inventory, still true):

```text
Connectors/FakeMt5BrokerConnector.cs   Fake + BrokerRegistry + DemoBrokerFactory
Connectors/IBrokerConnector.cs         DEPRECATED unused draft (B24: delete)
Configuration/Mt5BrokerOptions.cs      unused sketch (Password property; unused)
Utils/DeterministicGuid.cs             unused helper
```

`FakeMt5BrokerConnector` implements Application `IMt5BrokerConnector` (correct layer for an adapter). Ports and DTOs live in `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` (B24 winner). Ingestion (`DealIngestionService`) depends only on `IBrokerRegistry` / `ITradingStore` — it does **not** need the Mt5 assembly.

So the Infra→Mt5 edge is **not** “persistence coupled to Manager API.” It is “the persistence library appointed itself composition root for the demo fake.”

---

## 3. What architecture actually requires

### 3.1 §66 — folders, not an onion law

§66 lists `/src/Infrastructure` and `/src/Mt5` as **siblings**, then says:

> Adapt to the existing repo; do not create duplicates unnecessarily.

It does **not** say Infrastructure may reference Mt5. It does **not** say it must not. Sibling folders imply **hosts compose both**. That is the usual clean-architecture reading (A02 / B02: “Implementations belong in Infrastructure / Mt5 / Fix.CTrader / workers — contracts belong in Application”).

### 3.2 §6 — port vs adapter

§6 names `IMt5BrokerConnector` and a broker registry. The live port is already in Application (`Mt5Contracts.cs`). The live implementor is in Mt5 (`FakeMt5BrokerConnector`). That split is **EXISTS_AND_GOOD**.

The defect is who **new**s the implementor:

```31:34:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var (achiever, starwave) = DemoBrokerFactory.CreateDefault();
        services.AddSingleton<IMt5BrokerConnector>(achiever);
        services.AddSingleton<IMt5BrokerConnector>(starwave);
        services.AddSingleton<IBrokerRegistry>(sp => new BrokerRegistry(sp.GetServices<IMt5BrokerConnector>()));
```

A persistence `Add*` method should register `TraderDbContext`, `ITradingStore`, `IDashboardQueries`. Connector instances belong in `apps/mt5-worker` (and a test host). API only needs them if it runs `/api/ops/resync` in-process — that is a host choice, not an Infrastructure compile dependency.

### 3.3 §69 — first useful version is an operating bar

A57 quotes the 12 items. None of them is “Infrastructure csproj is onion-pure.”

The items that *touch* this edge:

| # | Item | Does Infra→Mt5 help or hurt? |
|---|---|---|
| 1 | Connect to both MT5 brokers | **Neither.** Fake registration is not a connection. Real connect is MISSING (B04). |
| 2–4 | Groups / ~5k accounts / XAU deals | Demo path uses the Fake *through* this edge. A real connector does not need the edge to live in Infrastructure. |
| 5–8 | Reconstruct / first-3 / score / rank | Persistence + Domain. **No Mt5 types required.** |
| 9–11 | QUOTE FIX / instrument / shadow | `Fix.CTrader` + Domain. Infra correctly does **not** reference FIX. |
| 12 | React | API reads EF. Transitive Mt5.dll is unused on those endpoints. |

A80 / §71: do not invent a mesh or extra microservice to “fix layering.” Splitting composition into the existing hosts is enough.

### 3.4 A54 — the sunset condition

A54: Windows owns `MT5APIManager64.dll`; Linux owns API / Postgres / Redis / React / (preferred) FIX-worker. **Do not force native SDK components into Linux containers.**

Today Mt5 is managed Fake-only, so Linux API loading `TraderIntelligence.Mt5.dll` is harmless.

The moment someone adds `Mt5ManagerBrokerConnector` + `LoadLibrary` / P/Invoke **into the same csproj**, every `AddTraderIntelligence` host — **including Linux API and Linux FIX-worker** — ships that assembly. That would be **UNSAFE** relative to A54 even if the native path is “not called.”

Therefore: **the current reference is acceptable only while `TraderIntelligence.Mt5` stays a managed adapter (Fake, later HTTP).** Native Manager code must be a **different** project referenced only by `apps/mt5-worker` (Windows RID).

---

## 4. Side effects that are real but not §69 blockers

These are why the class is `EXISTS_NEEDS_REFACTOR`, not `EXISTS_AND_GOOD`.

| # | Effect | Severity for first useful |
|---|---|---|
| 1 | API csproj does not name Mt5, but `AddTraderIntelligence` always registers two Fakes. `/api/health` then lies with `"demo connector"` / `healthy = true`. | Demo-honest today; **false** once item 1 is claimed. |
| 2 | FIX-worker pulls `TraderIntelligence.Mt5.dll` for no FIX reason. | Noise. Becomes A54 risk if native code is added. |
| 3 | Dual `DemoBrokerFactory.CreateDefault()` (DI vs seeder) — C05. The project reference *enables* both call sites inside Infrastructure. | Converges today (immutable seed). Still a fork. |
| 4 | Cannot swap Fake → HTTP/Manager without editing Infrastructure (or adding a second `Add*` that still lives in the persistence library). | Blocks item 1 cleanliness, not compile. |
| 5 | Unit tests do not reference Mt5 (correct). Integration tests reference both Infra and Mt5 (fine). There is still **no** test that `AddTraderIntelligence` builds a valid container (C05). | Test gap, not a layering FAIL. |
| 6 | Orphan `IBrokerConnector` in Mt5 (B24). Infra never uses it. Dead file does not justify the project reference. | DEPRECATED (already decided). |
| 7 | `Mt5BrokerOptions.Password` lives in the assembly API now loads. Unused, but it widens the secret-shaped surface on Linux hosts. | Low until bound. |

None of these is a reason to rewrite the csproj **this week** if the alternative is delaying ingest / reconstruction / scoring tests.

---

## 5. Target graph (later; not applied)

When someone is allowed to edit product source, the first-useful-compatible end state is:

```text
Infrastructure  → Domain, Application          // EF, outbox, Redis façade
Mt5             → Domain, Application          // Fake + (later) HTTP collector
Mt5.Native      → Mt5 or Application           // OPTIONAL, Windows-only, worker-only
Fix.CTrader     → Domain, Application
apps/api        → Infrastructure               // no Mt5 unless it hosts resync
apps/mt5-worker → Infrastructure + Mt5 [+ Native]
apps/fix-worker → Infrastructure + Fix.CTrader
```

Concrete split of `AddTraderIntelligence`:

1. `AddPersistence` / `AddTraderIntelligence` in Infrastructure: DbContext, `ITradingStore`, `IDashboardQueries`, reconstructor, scorer, ingestion **service type** (not connector instances).
2. `AddDemoMt5Brokers` (or `AddMt5Connectors`) in the **Mt5** project, called from `apps/mt5-worker` and from a test host. API calls it only if in-process resync stays.
3. `DemoSeeder` takes `DealIngestionService` from the same scope (C05 fix). Delete seeder’s `CreateDefault`.
4. Delete Infrastructure’s `<ProjectReference Include="..\Mt5\..."/>`.
5. Do **not** put `MT5APIManager64` / P/Invoke into `TraderIntelligence.Mt5`.

Until that edit is authorized: **leave the reference.** It is the cheapest way the current demo hosts seed 18 canned XAU deals.

---

## 6. Answers to the assigned question

| Question | Answer |
|---|---|
| Infra references Mt5? | **Yes.** csproj line 6, `deps.json`, two C# usings. |
| Cycle? | **No.** |
| Persistence coupled to collector types? | **No.** Only DI + seeder. |
| Acceptable for first useful version? | **Yes, as a temporary composition shortcut**, while Mt5 remains the in-memory Fake (PHASE0_AUDIT / B04). |
| Acceptable as the go-live / §69.1 topology? | **No** if “connected” means a real Manager session on Windows. Invert or split first. |
| Blocker for A57’s 0/12? | **No.** Do not treat this smell as the reason the first useful version is red. |
| Should product source be changed in this pass? | **No.** Frozen. This file is the decision record. |

**Overall class:** `EXISTS_NEEDS_REFACTOR`.

**Reviewer one-liner:** keep the edge for the demo; do not grow a native connector behind it; invert when hosts start composing a real `IMt5BrokerConnector`.

---

## 7. Evidence index

| Path | Why |
|---|---|
| `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | the reference |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Fake registration |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | second Fake graph |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | proves persist is Application/Domain only |
| `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj` | managed-only adapter |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | port ownership |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | undeclared transitive Mt5 |
| `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` | declared Mt5 (correct host) |
| `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj` | undeclared transitive Mt5 |
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` | §§6, 66, 69, 71 |
| `D:\Prop\reports\swarm\20260818\A54_deployment_split.md` | Windows DLL law |
| `D:\Prop\reports\swarm\20260818\A57_first_useful_version.md` | 0/12 operating bar |
| `D:\Prop\reports\swarm\20260818\B03_infra_gap.md` | first note of the smell |
| `D:\Prop\reports\swarm\20260818\C05_di_review.md` | cycle PASS; fork FAIL |

---

No product source was modified. This file is the C35 layering decision for Infrastructure → Mt5.
