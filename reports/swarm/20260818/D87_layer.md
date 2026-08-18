# D87 — Infrastructure references Mt5: still OK?

| Field | Value |
|---|---|
| Agent | D87 (senior engineer, layering re-measure only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (hashes + full product-file read; after C35) |
| Artifact | `D:\Prop\reports\swarm\20260818\D87_layer.md` |
| Assigned question | Infra references Mt5 OK? Write this report. Do not modify product source. |
| Product source modified | **No.** This report (plus swarm catalog/log) is the only write. |
| Workspace | `D:\Prop` |
| Binding spec | Architecture v2 §§6, 54/A54, 66, 69, 71, 73.B |
| Prior decision (do not inherit blindly) | `C35_layering.md` |
| Adjacent (read, not rewritten) | A11 (stale graph), A54, A57, A58, A79, B03, B04, B24, C05, C10, C35, D03, D04, D22 (seeder hash **stale**), D23, D24, D63 |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict (honest, one screen)

**Yes — still acceptable for the in-process Fake / first-useful *demo* slice. No — still not the A54 / go-live topology.**

The C35 decision is **reconfirmed** on a later tree. The `ProjectReference` itself did **not** move. Persistence grew (`EfTradingStore` + `PersistDemoShadowAsync`, `EfDashboardQueries` extra methods, honest `Disconnected` seeder) and **still does not import Mt5 types**. The leak is still composition-only.

| Question | Answer |
|---|---|
| Does `TraderIntelligence.Infrastructure` reference `TraderIntelligence.Mt5`? | **Yes.** csproj line 6. Unchanged SHA vs C35. |
| Circular project graph? | **No.** Mt5 → Domain + Application only. |
| Do persistence / dashboard files import Mt5? | **No.** Only `DependencyInjection.cs` and `DemoSeeder.cs`. |
| Why does the reference exist? | `AddTraderIntelligence` and `DemoSeeder` both `new` `DemoBrokerFactory` / `BrokerRegistry` / `FakeMt5BrokerConnector`. |
| Does §69 require onion-pure Infra? | **No.** Operating bar, not a NetArchTest bar. |
| Does the edge block any of the 12 §69 items? | **No.** Removing it now does not connect a broker. |
| Intended long-term graph (A54 §4.2)? | **No.** Hosts compose adapters. `TraderIntelligence.Mt5` may be referenced **only** from `apps/mt5-worker` once native code exists. |
| Does API declare Mt5? | **No.** It still **loads** `TraderIntelligence.Mt5.dll` transitively. |
| Does FIX-worker declare Mt5? | **No.** Same transitive load. |
| Is Mt5 native / P/Invoke today? | **No.** 4 product `.cs` files, 0 `DllImport` / `AllowUnsafeBlocks` / `MT5APIManager64`. |
| When does the edge become **not** OK? | The day `TraderIntelligence.Mt5` grows a Manager / P/Invoke / native DLL load — Linux API + Linux FIX-worker already ship that assembly. |
| Classification of the edge | **`EXISTS_NEEDS_REFACTOR`** (composition leak). **Not** `UNSAFE` while Fake-only. **Not** a §69 FAIL. |

**Do not stop Phase 1–5 work to invert this.** Invert (or split the Mt5 assembly) **before** a Windows-only connector lands in the same project Infrastructure already references.

Contrast that is still correct: **Infrastructure does not reference `Fix.CTrader`.** FIX composition stays on `apps/fix-worker`. MT5 composition is still stuffed into `AddTraderIntelligence` instead of `apps/mt5-worker`. That remains the whole defect.

Honest one-liner: **persistence is clean; the persistence *library* appointed itself the MT5 composition root, so every Infrastructure host loads the collector assembly.**

---

## 1. Method

Read-only. Did **not** edit `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `D:\Prop\mt5-sdk`. Did **not** start hosts. Did **not** run `dotnet` against product. Did **not** add ArchUnit / NetArchTest.

| Source | What was measured |
|---|---|
| All six Infrastructure product files | usings + ctor / factory graph |
| All four Mt5 product `.cs` + csproj | implementors, native deps |
| Application `Mt5Contracts.cs`, `DealIngestionService.cs` | port ownership |
| Host / test csprojs | who *declares* Mt5 vs who *inherits* it |
| Infra + API + FIX-worker `deps.json` + `TraderIntelligence.Mt5.dll` on disk | restore-time + publish-time leak |
| Architecture §§6, 66, 69, 71; A54 §4.2 | intended folders vs operating bar |
| C35, C05, D03, D23, D24 | prior notes; hashes **recomputed** |

Grep (product, not swarm scratch):

- `using TraderIntelligence.Mt5` under `src/Infrastructure` → **2 files** (`DependencyInjection.cs`, `DemoSeeder.cs`).
- `ProjectReference` to `TraderIntelligence.Mt5.csproj` (product) → Infrastructure, `apps/mt5-worker`, Integration tests. **Not** Application, Domain, Fix.CTrader, API (direct), FIX-worker (direct), Unit tests.
- `DllImport` / `AllowUnsafeBlocks` / `MT5APIManager` / `LoadLibrary` / `NativeLibrary` under `src/Mt5` → **0**.

---

## 2. File identity (re-hashed 2026-08-18)

SHA-256 uppercase. Physical lines include blanks.

| Bytes | SHA-256 | Phys. | Non-blank | Last write | Path | vs C35 / D03 |
|---:|---|---:|---:|---|---|---|
| 1035 | `4DABF29CA934261CFC46C72514CB7AA04D5E8F9CC8FFAC1BA051BF0CD0668EED` | 25 | 21 | 13:15:01 | `Infrastructure.csproj` | **unchanged** |
| 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | 44 | 39 | 13:14:18 | `DependencyInjection.cs` | **unchanged** |
| 5082 | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | 140 | 129 | 13:34:59 | `Seeding\DemoSeeder.cs` | **changed** (was 4942 / `139D8F87…`) |
| 12097 | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` | 338 | 310 | 13:35:59 | `Persistence\EfTradingStore.cs` | **changed** (was 9020 / `05103CE5…`) |
| 5951 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | 174 | 151 | 13:12:48 | `Persistence\TraderDbContext.cs` | **unchanged** |
| 8708 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | 205 | 182 | 13:35:15 | `Dashboard\EfDashboardQueries.cs` | **changed** (was 7407 / `37A4DDD2…`) |
| 419 | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` | 14 | 11 | 12:54:51 | `Mt5.csproj` | **unchanged** |
| 7049 | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` | 170 | 145 | 13:13:42 | `FakeMt5BrokerConnector.cs` | **unchanged** |
| 1858 | `8430978B9ADD541B0B84639A0FF74644673C534DE7AA30B479FC49F048BEB132` | 69 | 62 | 13:09:51 | `Application\Contracts\Mt5Contracts.cs` | **unchanged** |
| 4535 | `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` | 106 | 92 | 13:35:29 | `DealIngestionService.cs` | **changed** (port grew `PersistDemoShadowAsync`) |

**Layering-relevant conclusion from the hash table:** the *edge* (csproj + DI using) is bit-identical to C35. The *consumers that grew* (store, dashboard, seeder, ingestion) stayed on Application / Domain types.

D22 (`DemoSeeder` SHA `139D8F87…`, “FORGED LoggedOn”) is **stale**. Current seeder writes `FixSessionStatus.Disconnected` and an honest `LastError`. That honesty change does **not** remove the Mt5 using.

---

## 3. Measured graph (2026-08-18)

### 3.1 Compile-time DAG

```text
Domain                         (0 project refs)
   ▲
   │
Application  → Domain only
   ▲
   ├── Mt5           → Domain, Application          (adapter; Fake only)
   ├── Fix.CTrader   → Domain, Application          (adapter)
   └── Infrastructure → Domain, Application, **Mt5**  ← this report
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

**Back-edge Mt5 → Infrastructure: none.** Cycle question stays closed (C05 §4).

A11 §7 drew Infrastructure pointing *at* hosts. That snapshot is **still stale**. Use C35 / this file for the edge.

A54 §4.2 wanted:

```text
Api        → Domain, Application, Infrastructure     (no Mt5)
Mt5Worker  → Domain, Application, Infrastructure, Mt5
FixWorker  → Domain, Application, Infrastructure, Fix.CTrader
```

Declared csproj edges match A54. **Runtime load does not:** API and FIX-worker both contain `TraderIntelligence.Mt5.dll` because Infrastructure lists it as a dependency.

### 3.2 Evidence of the reference

`D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` (1035 B, SHA-256 `4DABF29CA934261CFC46C72514CB7AA04D5E8F9CC8FFAC1BA051BF0CD0668EED`):

```3:7:D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj
  <ItemGroup>
    <ProjectReference Include="..\Domain\TraderIntelligence.Domain.csproj" />
    <ProjectReference Include="..\Application\TraderIntelligence.Application.csproj" />
    <ProjectReference Include="..\Mt5\TraderIntelligence.Mt5.csproj" />
  </ItemGroup>
```

`TraderIntelligence.Infrastructure/1.0.0` dependencies in both Debug and Release `deps.json`:

```text
TraderIntelligence.Application: 1.0.0
TraderIntelligence.Domain: 1.0.0
TraderIntelligence.Mt5: 1.0.0
```

Same key appears **under Infrastructure**, not under the API/FIX-worker roots:

| Host deps.json | Direct project deps | How Mt5 appears |
|---|---|---|
| `apps/api/.../TraderIntelligence.Api.deps.json` | Application, Domain, Infrastructure | nested under `TraderIntelligence.Infrastructure/1.0.0` |
| `apps/fix-worker/.../TraderIntelligence.FixWorker.deps.json` | Application, Domain, Infrastructure, Fix.CTrader | same nested key |

On-disk `TraderIntelligence.Mt5.dll` (Debug 22016 B, last write 13:40:17):

| Location | Present? |
|---|---|
| `src/Infrastructure/bin/Debug/net8.0/` | **yes** |
| `src/Infrastructure/bin/Release/net8.0/` | **yes** (21504 B) |
| `apps/api/bin/Debug/net8.0/` | **yes** (undeclared) |
| `apps/fix-worker/bin/Debug/net8.0/` | **yes** (undeclared) |
| `apps/mt5-worker/bin/Debug/net8.0/` | **yes** (declared — correct host) |
| `tests/Integration/bin/Debug/net8.0/` | **yes** (declared) |
| `tests/Unit/bin/Debug/net8.0/` | **no** (correct) |

Any host that references Infrastructure **loads the collector assembly** even if its csproj never names Mt5.

### 3.3 Who actually uses Mt5 types

| File | `using TraderIntelligence.Mt5`? | Role |
|---|---|---|
| `DependencyInjection.cs` | **yes** | registers two `FakeMt5BrokerConnector` + `BrokerRegistry` |
| `Seeding/DemoSeeder.cs` | **yes** | **second** `CreateDefault()` + `new BrokerRegistry` + `new DealIngestionService` (C05 split graph; still true) |
| `Persistence/EfTradingStore.cs` | no | Application `Mt5*Dto` + Domain entities. Now 9/9 `ITradingStore` methods including `PersistDemoShadowAsync`. |
| `Persistence/TraderDbContext.cs` | no | Domain `DbSet`s |
| `Dashboard/EfDashboardQueries.cs` | no | EF reads (now includes `GetTraderDetailAsync`) |
| `Infrastructure.csproj` | n/a | the edge itself |

Mt5 symbols used in Infrastructure C# (complete):

```31:34:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var (achiever, starwave) = DemoBrokerFactory.CreateDefault();
        services.AddSingleton<IMt5BrokerConnector>(achiever);
        services.AddSingleton<IMt5BrokerConnector>(starwave);
        services.AddSingleton<IBrokerRegistry>(sp => new BrokerRegistry(sp.GetServices<IMt5BrokerConnector>()));
```

```126:128:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
        var (achiever, starwave) = DemoBrokerFactory.CreateDefault();
        var registry = new BrokerRegistry(new IMt5BrokerConnector[] { achiever, starwave });
        var ingestion = new DealIngestionService(registry, store);
```

`IMt5BrokerConnector` / `IBrokerRegistry` are Application ports (`Mt5Contracts.cs`). The **types that force the project reference** are `DemoBrokerFactory` and `BrokerRegistry`, both in `TraderIntelligence.Mt5.Connectors`.

If those two composition files moved to a host (or an `AddDemoMt5` extension in the Mt5 project), the Infrastructure csproj reference would have **zero remaining consumers** and could be deleted. Store growth did not create a new consumer.

### 3.4 What Mt5 is today (why the leak is still cheap)

`D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj` (419 B, SHA-256 `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F`):

- `net8.0` class library.
- Project refs: Domain + Application only.
- **No** package references, **no** `HttpClient` package, **no** `AllowUnsafeBlocks`, **no** native items.

On-disk product C# (exclude `bin/` `obj/`):

```text
Connectors/FakeMt5BrokerConnector.cs   Fake + BrokerRegistry + DemoBrokerFactory
Connectors/IBrokerConnector.cs         DEPRECATED unused draft (B24 / D25: delete)
Configuration/Mt5BrokerOptions.cs      unused sketch (Password property; unused)
Utils/DeterministicGuid.cs             unused helper
```

`FakeMt5BrokerConnector` implements Application `IMt5BrokerConnector` (correct layer for an adapter). Ports and DTOs live in `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` (B24 winner). `DealIngestionService` depends only on `IBrokerRegistry` / `ITradingStore` — it does **not** need the Mt5 assembly.

So the Infra→Mt5 edge is **not** “persistence coupled to Manager API.” It is “the persistence library appointed itself composition root for the demo fake.” D24 still holds: `ConnectAsync` flips a bool. There is no socket.

---

## 4. What architecture actually requires

### 4.1 §66 — folders, not an onion law

§66 lists `/src/Infrastructure` and `/src/Mt5` as **siblings**, then says “Adapt to the existing repo; do not create duplicates unnecessarily.”

It does **not** say Infrastructure may reference Mt5. It does **not** say it must not. Sibling folders imply **hosts compose both**. That is the usual clean-architecture reading (A02 / B02: implementations in Infrastructure / Mt5 / Fix.CTrader / workers — contracts in Application).

### 4.2 §6 — port vs adapter

§6 names `IMt5BrokerConnector` and a broker registry. The live port is already in Application. The live implementor is in Mt5. That split is **`EXISTS_AND_GOOD`**.

The defect is who **new**s the implementor. A persistence `Add*` method should register `TraderDbContext`, `ITradingStore`, `IDashboardQueries`. Connector instances belong in `apps/mt5-worker` (and a test host). API only needs them if it runs `/api/ops/resync` in-process — that is a host choice, not an Infrastructure compile dependency.

### 4.3 §69 — first useful version is an operating bar

None of the 12 items is “Infrastructure csproj is onion-pure.”

| # | Item | Does Infra→Mt5 help or hurt? |
|---|---|---|
| 1 | Connect to both MT5 brokers | **Neither.** Fake registration is not a connection. Real connect is MISSING (B04 / C42 / D24). |
| 2–4 | Groups / ~5k accounts / XAU deals | Demo path uses the Fake *through* this edge. A real connector does not need the edge in Infrastructure. |
| 5–8 | Reconstruct / first-3 / score / rank | Persistence + Domain. **No Mt5 types required.** Store growth (`PersistDemoShadowAsync`) stayed Application/Domain. |
| 9–11 | QUOTE FIX / instrument / shadow | `Fix.CTrader` + Domain. Infra correctly does **not** reference FIX. |
| 12 | React | API reads EF. Transitive `Mt5.dll` is unused on those endpoints. |

A80 / §71: do not invent a mesh or extra microservice to “fix layering.” Splitting composition into the existing hosts is enough.

### 4.4 A54 — the sunset condition (unchanged)

A54: Windows owns `MT5APIManager64.dll`; Linux owns API / Postgres / Redis / React / (preferred) FIX-worker. **Do not force native SDK components into Linux containers.**

D63 reconfirmed Compose does **not** Linux-ize `mt5-worker`. That split is correct **at the process boundary**.

The *assembly* boundary is still leaky: Linux API (and a Linux FIX-worker, if that is the deploy) already load `TraderIntelligence.Mt5.dll` because of this reference.

Today Mt5 is managed Fake-only, so Linux API loading that DLL is harmless.

The moment someone adds `Mt5ManagerBrokerConnector` + `LoadLibrary` / P/Invoke **into the same csproj**, every `AddTraderIntelligence` host — **including Linux API and Linux FIX-worker** — ships that assembly. That would be **`UNSAFE`** relative to A54 even if the native path is “not called.”

Therefore: **the current reference is acceptable only while `TraderIntelligence.Mt5` stays a managed adapter (Fake, later HTTP).** Native Manager code must be a **different** project referenced only by `apps/mt5-worker` (Windows RID).

---

## 5. Drift vs C35 (what this pass adds)

| Item | C35 | D87 now |
|---|---|---|
| Infra csproj / DI SHA | `4DABF29…` / `EF0E0E46…` | **same** |
| Mt5 csproj / Fake SHA | `0AD91D39…` / `AE7C1B1B…` | **same** |
| Persistence Mt5 usings | 0 | **0** |
| `ITradingStore` methods | 8 | **9** (`PersistDemoShadowAsync`) — still no Mt5 types |
| Seeder FIX status | D03/D22: `LoggedOn` / `ReadyForMarketData` | **`Disconnected`** + honest `LastError` (D22 hash stale) |
| Dual `CreateDefault()` | yes | **yes** (C05 still holds) |
| Transitive `Mt5.dll` on API + FIX-worker | yes | **yes** (re-measured 13:40:17) |
| Native in `src/Mt5` | none | **none** |
| Verdict | demo-OK / invert before native | **same** |

C35 is not stale on the *decision*. It is stale on seeder honesty and on store/dashboard line counts. Use this file for those hashes.

---

## 6. Side effects that are real but not §69 blockers

These are why the class is `EXISTS_NEEDS_REFACTOR`, not `EXISTS_AND_GOOD`.

| # | Effect | Severity for first useful |
|---|---|---|
| 1 | API csproj does not name Mt5, but `AddTraderIntelligence` always registers two Fakes. `/api/health` then reports `"demo FakeMt5BrokerConnector — not live Manager"` (current `Program.cs` is honest on that string). | Demo-honest today; **false** once item 1 is claimed without invert. |
| 2 | FIX-worker pulls `TraderIntelligence.Mt5.dll` for no FIX reason. | Noise. Becomes A54 risk if native code is added. |
| 3 | Dual `DemoBrokerFactory.CreateDefault()` (DI vs seeder) — C05. The project reference *enables* both call sites inside Infrastructure. | Converges today (immutable seed). Still a fork. |
| 4 | Cannot swap Fake → HTTP/Manager without editing Infrastructure (or adding a second `Add*` that still lives in the persistence library). | Blocks item 1 cleanliness, not compile. |
| 5 | Unit tests do not reference Mt5 (correct). Integration tests reference both Infra and Mt5 and call `DemoSeeder` (fine). Still **no** test that `AddTraderIntelligence` builds a valid container (C05). | Test gap, not a layering FAIL. |
| 6 | Orphan `IBrokerConnector` in Mt5 (B24 / D25). Infra never uses it. Dead file does not justify the project reference. | `DEPRECATED` (already decided). |
| 7 | `Mt5BrokerOptions.Password` lives in the assembly API now loads. Unused, but it widens the secret-shaped surface on Linux hosts. | Low until bound. |

None of these is a reason to rewrite the csproj **this week** if the alternative is delaying ingest / reconstruction / scoring tests.

---

## 7. Target graph (later; not applied)

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

Until that edit is authorized: **leave the reference.** It is the cheapest way the current demo hosts seed the canned XAU book.

---

## 8. Answers to the assigned question

| Question | Answer |
|---|---|
| Infra references Mt5? | **Yes.** csproj line 6, both `deps.json`, two C# usings. |
| Is that OK? | **Yes for the Fake demo.** **No as the production / A54 graph.** |
| Cycle? | **No.** |
| Persistence coupled to collector types? | **No.** Only DI + seeder. Store/dashboard growth did not change that. |
| Acceptable for first useful version? | **Yes, as a temporary composition shortcut**, while Mt5 remains the in-memory Fake (C42 / D24). |
| Acceptable as the go-live / §69.1 topology? | **No** if “connected” means a real Manager session on Windows. Invert or split first. |
| Blocker for §69 0/12? | **No.** Do not treat this smell as the reason the first useful version is red. |
| Should product source be changed in this pass? | **No.** Frozen. This file is the D-wave decision record. |

**Overall class:** `EXISTS_NEEDS_REFACTOR`.

**Reviewer one-liner:** keep the edge for the demo; do not grow a native connector behind it; invert when hosts start composing a real `IMt5BrokerConnector`.

---

## 9. Evidence index

| Path | Why |
|---|---|
| `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | the reference |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Fake registration |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | second Fake graph |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | persist is Application/Domain only |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | dashboard is Application/Domain only |
| `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj` | managed-only adapter |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | port ownership |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | undeclared transitive Mt5 |
| `D:\Prop\apps\api\bin\Debug\net8.0\TraderIntelligence.Api.deps.json` | Infrastructure lists Mt5 |
| `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` | declared Mt5 (correct host) |
| `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj` | undeclared transitive Mt5 |
| `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj` | no Mt5 (correct) |
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` | §§6, 66, 69, 71 |
| `D:\Prop\reports\swarm\20260818\A54_deployment_split.md` | Windows DLL law |
| `D:\Prop\reports\swarm\20260818\C35_layering.md` | first decision; edge SHAs still match |
| `D:\Prop\reports\swarm\20260818\C05_di_review.md` | cycle PASS; fork FAIL |
| `D:\Prop\reports\swarm\20260818\D24_fake.md` | Fake is not a broker |
| `D:\Prop\reports\swarm\20260818\D63_compose.md` | process split is correct; assembly split is not |

---

No product source was modified. This file is the D87 layering re-measure for Infrastructure → Mt5.
