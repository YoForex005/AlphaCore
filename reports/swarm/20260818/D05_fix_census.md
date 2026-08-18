# D05 — `src/Fix.CTrader` census (measured worktree)

| Field | Value |
|---|---|
| Agent | D05 (inventory only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\D05_fix_census.md` |
| Scope | `D:\Prop\src\Fix.CTrader` product source + restore/build graph + consumers |
| Product source modified | **No** |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`main`) |
| Method | Full read of every product `.cs` / `.csproj`; SHA-256 + byte/line counts; `git status` / `git diff` / `git ls-tree`; restore `project.assets.json` + `deps.json` + `project.nuget.cache`; grep of type names under `src/`, `apps/`, `tests/` |
| Siblings (do not treat as this snapshot) | A05 (stale `Class1`), B05 (spec + older csproj pin), B28 (parser/lease review), C19 (QuickFIX/n package verify) |

**Honesty rule:** a class library with four helpers is not a FIX adapter. A pipe-delimited string factory is not a venue. An unused `ProjectReference` is not wiring. `fix_sessions.Status = LoggedOn` written by the worker/seeder is not a session.

---

## 0. Verdict

`TraderIntelligence.Fix.CTrader` is a **net8.0 class library with 4 product `.cs` files and 0 FIX engine**. Worktree has **no** `PackageReference`. Official QuickFIX/n (`QuickFIXn.Core` + `QuickFIXn.FIX44` 1.14.1) is **absent**. Unofficial `QuickFix.Net` 1.8.0 is on **HEAD only** (unstaged deletion). There is **no** `CTraderQuoteSession`, **no** `CTraderTradeSession`, **no** `CTraderFixSimulator` venue, **no** `*.xml` dictionary, **no** `*.cfg`, **no** `tests/Fix`.

What exists:

| Object | Path | Class |
|---|---|---|
| Options bag (host/ports/headers/flags) | `Configuration/CTraderFixOptions.cs` | **EXISTS_NEEDS_REFACTOR** — unbound; live host default |
| Pipe/`|` parse + checksum builder | `Parsing/FixMessageParser.cs` | **EXISTS_NEEDS_REFACTOR** — last-wins `Dictionary`; test codec |
| In-memory fencing lock | `Services/FixSessionOwnership.cs` | **EXISTS_NEEDS_REFACTOR** — process-local; unused |
| Checksummed `\|` fixture factory | `Testing/FixSimulationHarness.cs` | **EXISTS_NEEDS_REFACTOR** — not a venue; unused by tests |
| Two independent session types | — | **MISSING** (0 / 2) |
| In-process venue (`ICTraderFixVenue`) | — | **MISSING** |
| Official QuickFIX/n initiator | — | **MISSING** |
| Live `NewOrderSingle` send path | — | **ABSENT** (safe by absence) |

**Consumer wiring of Fix.CTrader types: 0.** `apps/fix-worker`, `tests/Unit`, and `tests/Integration` reference the project and call **zero** types from it. `apps/api` does not reference the project.

Classification of the project vs architecture §73.B: **EXISTS_NEEDS_REFACTOR** (scaffold + helpers). Adapter completeness vs §§25–34, 41–44, 61: **still ~0% of required behaviour.**

---

## 1. Tree (product source only)

Exclude `bin/` and `obj/`.

```
D:\Prop\src\Fix.CTrader\
  TraderIntelligence.Fix.CTrader.csproj
  Configuration\
    CTraderFixOptions.cs
  Parsing\
    FixMessageParser.cs
  Services\
    FixSessionOwnership.cs
  Testing\
    FixSimulationHarness.cs
```

**Absent folders / files (required later, not present):**

| Path | Expected role |
|---|---|
| `Sessions/CTraderQuoteSession.cs` | Independent QUOTE initiator (§27) |
| `Sessions/CTraderTradeSession.cs` | Independent TRADE initiator (§27) |
| `Simulation/CTraderFixSimulator.cs` | In-process venue (§61) |
| `Simulation/SimQuoteSession.cs` / `SimTradeSession.cs` | Simulator sessions |
| `Parsing/FixFieldList.cs` | Repeating-group codec |
| `Spec/FIX44-CSERVER.xml` | cTrader RoE dictionary (A36) |
| `Spec/quote.cfg` / `trade.cfg` | QuickFIX `SessionSettings` |
| `QuickFix/` | Live `IApplication` / settings factory |
| `Class1.cs` | **Gone** (A05 stale) |
| any `*.xml` / `*.cfg` under this project | — |

No `TcpClient`, `SslStream`, `SocketInitiator`, `SessionSettings`, `IApplication`, or `using QuickFix` in product source. The only `NewOrderSingle` token is a **comment** on `RealCopyExecutionEnabled`.

---

## 2. File metrics (worktree, 2026-08-18)

| Rel path | Bytes | Lines (all) | Non-blank | SHA-256 | git blob |
|---|---:|---:|---:|---|---|
| `TraderIntelligence.Fix.CTrader.csproj` | 419 | 14 | 11 | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` | `529a3a1c11def916ce8038388af8a7a2913505a9` |
| `Configuration/CTraderFixOptions.cs` | 2344 | 80 | 55 | `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` | `f2cd089d29304a3e107dbc1e58957421a65296d6` |
| `Parsing/FixMessageParser.cs` | 6016 | 145 | 120 | `C58681E761D43052B53D2A8D00883C461A9E3CEB5B7DF8995D50F8155F710E3D` | `f6aca38c1363f6259a787849ab0230059bffd26c` |
| `Services/FixSessionOwnership.cs` | 4719 | 134 | 114 | `30029E29EE66C2114643AAF8FD0E0D8566C075A0FF693CCA7043CEADED5E6D20` | `2e42ef7053c8304476f637bc5e149f8a38f98ee8` |
| `Testing/FixSimulationHarness.cs` | 8970 | 205 | 185 | `99A28D8F3F49028706C75B9C4DC46B4CCB3FF98E90AAED3B4B874DD1B4351616` | `19dd8a7a1069d8f451d2e0149f2e474893ee6eef` |
| **Product total** | **22468** | **578** | **485** | — | — |

`Class1.cs`: **not on disk, not in HEAD tree** for this project (HEAD already has the four files above).

---

## 3. Project / restore / packages

Worktree `TraderIntelligence.Fix.CTrader.csproj` (entire file):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\Domain\TraderIntelligence.Domain.csproj" />
    <ProjectReference Include="..\Application\TraderIntelligence.Application.csproj" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

| Item | Measured |
|---|---|
| Target | `net8.0` class library |
| Direct `PackageReference` (worktree) | **none** |
| Direct `PackageReference` (HEAD) | unofficial `QuickFix.Net` **1.8.0** (unused by any `.cs`) |
| Official `QuickFIXn.Core` / `QuickFIXn.FIX44` | **not referenced** |
| `Directory.Packages.props` | **does not exist** |
| `Directory.Build.props` | lang/nullable only; **no** package versions |
| Transitive restore package | FluentValidation **11.9.2** (via Application) |
| `project.assets.json` `projectFileDependencyGroups.net8.0` | `TraderIntelligence.Application`, `TraderIntelligence.Domain` |
| `obj/project.nuget.cache` `expectedPackageFiles` | `fluentvalidation/11.9.2` only |
| `bin/Debug/net8.0/TraderIntelligence.Fix.CTrader.deps.json` libraries | this project + Application + Domain + FluentValidation 11.9.2 |
| `QuickFix*.dll` / `QuickFIXn*.dll` under `src/` `apps/` `tests/` | **none** |
| SDK used by restore graph | `C:\Program Files\dotnet\sdk\8.0.424` |

Layering: Fix.CTrader → Application is a **defect** (adapter should implement Application ports and depend on Domain + engine only). **Measured extra:** none of the four `.cs` files `using` Domain or Application. Both project references are **compile-unused** today.

---

## 4. Type census (this assembly)

Four namespaces. **Seven** public types (including two nested option bags, one nested interface, one nested lock). **Zero** interfaces at namespace scope. **Zero** abstract session/venue ports.

### 4.1 `TraderIntelligence.Fix.CTrader.Configuration.CTraderFixOptions` (sealed)

Nested: `QuoteFixOptions`, `TradeFixOptions`.

| Member | Default (worktree) | Notes |
|---|---|---|
| `Host` | `live-us-eqx-01.p.c-trader.com` | **Live Pepperstone hostname** as C# default |
| `AccountId` | `""` | Comment: FIX username; “Must never be logged” |
| `Password` | `""` | Comment: “Must never be logged” |
| `Quote` | new `QuoteFixOptions` | Nested bag |
| `Trade` | new `TradeFixOptions` | Nested bag |
| `UseSsl` | `true` | |
| `QuoteEnabled` | `true` | Flag field only; not bound |
| `TradeSessionEnabled` | `true` | Flag field only; not bound |
| `RealCopyExecutionEnabled` | `false` | Correct default; **not bound** to worker |
| `HeartbeatIntervalSec` | `30` | |
| `MaxQuoteAgeMs` | `5000` | Conflicts with `RiskLimits.MaxQuoteAge = 3s` (Domain) |

`QuoteFixOptions` / `TradeFixOptions` (same shape):

| Member | Quote default | Trade default |
|---|---|---|
| `SslPort` | **5211** | **5212** |
| `PlainPort` | 5201 | 5202 |
| `SenderCompId` | `live.pepperstone.1369850` | same (issued live form) |
| `TargetCompId` | **`cServer`** (worktree) | **`cServer`** (worktree); HEAD was `CSERVER` |
| `TargetSubId` | `QUOTE` | `TRADE` |
| `SenderSubId` | `""` | `""` |

**Not present on this type:** `VenueMode`, `CtraderFixEnabled`, `DiagnosticLogonOnly`, `FileStorePath`, `FileLogPath`, binder attributes, `IValidateOptions`.

**Not registered:** `IOptions<CTraderFixOptions>` is never `Configure`’d. `Infrastructure.DependencyInjection.AddTraderIntelligence` does not mention this type.

### 4.2 `TraderIntelligence.Fix.CTrader.Parsing.FixMessageParser` (sealed)

Public API:

| Method | Signature | Behaviour |
|---|---|---|
| `Parse` | `IReadOnlyDictionary<int,string> Parse(string fixPipeDelimited)` | Split on `\|`; require last field `10=`; numeric checksum; compare 3-digit ASCII sum mod 256; last-wins map |
| `BuildFixMessage` | `string BuildFixMessage(IEnumerable<KeyValuePair<int,string>> fields)` | Require tag 8; drop 9/10; order 8 then 35 then remaining **ascending**; compute BodyLength + checksum; emit `\|` |

Private: `JoinSohFields`, `ComputeChecksum`, `ComputeChecksumFromRaw`. Separators: display `\|`, wire calc `SOH` (`\u0001`). Encoding: ASCII.

HEAD→worktree: `EndsWith(SeparatorChar, StringComparison.Ordinal)` → `EndsWith(SeparatorChar)` (net8 `char` overload). Ownership file unchanged.

**Cannot:** parse repeating groups (MD `268/269/270`, SecurityList instruments). **Must not** emit live outbound (RoE field order not implemented).

### 4.3 `TraderIntelligence.Fix.CTrader.Services.FixSessionOwnership` (sealed)

Nested:

- `IDistributedLockWithFencing` — `TryAcquireAsync(lockKey, ownerId, ttl, ct) → (bool acquired, long fencingToken)`; `ReleaseAsync(...)`
- `InMemoryDistributedLockWithFencing` — `ConcurrentDictionary` + `Interlocked` global token; process-local; no renew; expired lock is stealable

Outer members:

| Member | Role |
|---|---|
| ctor `(IDistributedLockWithFencing, ownerId, lockKey, ttl)` | Stores fields; no DI registration |
| `HasOwnership` | last acquire result |
| `FencingToken` | last token (0 after release) |
| `ExecutionIntentsAllowed` | `_hasOwnership && _reconciled` |
| `AcquireAsync` | one-shot `TryAcquire`; does **not** fail closed on provider throw beyond await |
| `MarkReconciled` | sets `_reconciled = true` with **no** token check |
| `ReleaseAsync` | release if owned; clears flags |

This is **not** A46 (`ti:fix:lease:{session_key}` Redis + Postgres-minted fence). Worker never constructs it.

### 4.4 `TraderIntelligence.Fix.CTrader.Testing.FixSimulationHarness` (sealed)

Owns a private `FixMessageParser`. All public methods return **pipe strings** (or echo input). Comment: “no live FIX connection required.”

| Method | MsgType (35) | Notes / RoE defects |
|---|---|---|
| `SimulateLogonSuccess` | `A` | 98=0, 108=30, 141=Y. Default TargetCompId worktree `cServer` |
| `SimulateLogonFail` | `3` Reject | Official failed Logon is **Logout 35=5** + 58; tag **371** used as reason (wrong) |
| `SimulateExecutionReport_New` | `8` | 150/execType param default `"0"`; 39=`0` |
| `SimulateExecutionReport_Fill` | `8` | 150=`F`, 39=`2`; optional 32/31 |
| `SimulateExecutionReport_PartialFill` | `8` | 150=`F`, 39=`1` |
| `SimulateExecutionReport_Canceled` | `8` | 150=`4`, 39=`4` |
| `SimulateExecutionReport_Rejected` | `8` | 150=`8`, 39=`8`; optional 58 |
| `SimulateExecutionReport_Expired` | `8` | 150=`C`, 39=`C` |
| `SimulateExecutionReport_UnknownState` | `8` | 150=`I` — **misnamed**; 150=I is Order Status, not unknown |
| `SimulateDuplicateExecutionReport` | n/a | identity; no second seq |
| `SimulateDisconnect` | `0` Heartbeat | **Not a transport drop**; invented 1128=text |
| `SimulateSecurityList` | `y` | Hardcodes `55=123456`, `1007=XAUUSD`; no repeating group |
| `SimulateMarketDataSnapshot` | `X` | Snapshot official is **`W`**; invented **1320/1321** bid/ask |

Shared ER header defects: tag **55 default `"XAUUSD"`** (cServer wants numeric Spotware id); inbound-looking `49=SENDER`, `57=TRADE` (server ER is `49=CSERVER`, `50=TRADE`); missing 14/151/38/54/721/17.

**Does not:** accept 35=D, own a book, increment independent QUOTE/TRADE seq, open/refuse a socket, implement `ICTraderFixVenue`.

Grep of `FixSimulationHarness` / `SimulateLogon` / `SimulateExecutionReport` / `FixMessageParser` under `D:\Prop\tests`: **no matches**. The only product caller of `BuildFixMessage` is the harness itself.

---

## 5. HEAD vs worktree (this project)

`git status --short -- src/Fix.CTrader`:

```
 M src/Fix.CTrader/Configuration/CTraderFixOptions.cs
 M src/Fix.CTrader/Parsing/FixMessageParser.cs
 M src/Fix.CTrader/Testing/FixSimulationHarness.cs
 M src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj
```

`FixSessionOwnership.cs` is **clean** (HEAD blob = worktree blob `2e42ef70…`).

`git diff --stat -- src/Fix.CTrader`: **4 files, +8 / −9**.

| File | HEAD | Worktree |
|---|---|---|
| csproj | `QuickFix.Net` 1.8.0 | line **deleted** |
| `CTraderFixOptions` Quote/Trade `TargetCompId` | `"CSERVER"` | `"cServer"` |
| harness Logon/SecurityList/MD/ER `56` / default TargetCompId | `"CSERVER"` | `"cServer"` |
| parser `EndsWith` | `(char, StringComparison)` | `(char)` |

Git history for the folder: single commit `6c41447 Initial commit`. All four `.cs` files already existed at initial commit; later unstaged edits are the table above. This agent did not make those edits.

HEAD blobs: csproj `1b394e9d…`, options `204f9d58…`, parser `c799bb15…`, ownership `2e42ef70…`, harness `433326b2…`.

---

## 6. Solution membership and consumers

| Consumer | ProjectReference? | Uses a Fix.CTrader type? |
|---|---|---|
| `Mt5TraderIntelligence.sln` | project `{76085664-A639-4C0D-8F92-264963416855}` under `src` folder | N/A |
| `apps/fix-worker` | **yes** | **no** |
| `tests/Unit` | **yes** | **no** |
| `tests/Integration` | **yes** | **no** |
| `apps/api` | **no** | no |
| `apps/mt5-worker` | **no** | no |
| `src/Infrastructure` | **no** | no |
| `src/Application` | **no** (reverse: Fix.CTrader refs Application) | no |
| `tests/Fix` | **project missing** | — |

`apps/fix-worker/TraderIntelligence.FixWorker.csproj`: Hosting 8.0.1 + Domain + Application + Infrastructure + Fix.CTrader.

`apps/fix-worker/Worker.cs` (49 lines): reads `CTrader:RealCopyExecutionEnabled` (default false); every **15 s** stamps EF `FixSessionStates` QUOTE → `ReadyForMarketData`, TRADE → `LoggedOn` **in both flag branches**; logs a refuse-NewOrderSingle warning if flag true — **there is no send function**. Does not `new` any Fix.CTrader type.

`apps/fix-worker/Program.cs`: `AddTraderIntelligence` + `EnsureCreated` + `DemoSeeder`. No FIX venue registration.

`apps/fix-worker/appsettings.json` and `appsettings.Development.json`: **Logging only**. No `CTrader` block.

`apps/api/appsettings.json` has a `CTrader` section (`Host`, `AccountId`, empty `Password`, SSL/flags) that is **not** bound to `CTraderFixOptions` (API does not reference Fix.CTrader).

`Infrastructure.DependencyInjection`: registers EF, fake MT5 connectors, dashboard queries, reconstructor/scorer/ingestion. **Zero** Fix.CTrader services.

`tests/Unit/ExecutionAndSizingTests.cs` covers Domain `ExecutionOrderStateMachine` / `ClOrdIdFactory` / `QuantityNormalizer` / `CopyIntentExpiry` — **not** this assembly.

`tests/Integration/SeedingAndStoreTests.cs` asserts seeded `FixSessionStates` count=2 and `TargetCompId == "cServer"` — Domain/Infrastructure, not Fix.CTrader.

---

## 7. Adjacent Domain / Application / Infrastructure (not in this project)

These types are **outside** Fix.CTrader but are the only FIX-shaped surface the rest of the repo can see. Census lists them so implementers do not invent a second vocabulary.

| Type | Path | Role vs adapter |
|---|---|---|
| `FixSessionQualifier` | `Domain/Enums` | `Quote=0`, `Trade=1` |
| `FixSessionStatus` | `Domain/Enums` | Disconnected…ReadyForExecution…Error |
| `FixSessionState` | `Domain/Entities` | `fix_sessions` row (unique Qualifier) |
| `ExecutionOrderStatus` | `Domain/Enums` | includes `ExecutionStateUnknown` |
| `ExecutionOrderStateMachine` | `Domain/Execution` | apply ER / unknown / `MayRetryNewOrderSingle` |
| `ClOrdIdFactory` | `Domain/Execution` | `TI{yyyyMMddHHmmss}{seq}{compact}` |
| `ExecutionIntent` | `Domain/Entities` | `execution_intents` + unique `ClOrdId` |
| `CopyIntent` | `Domain/Entities` | `copy_intents` |
| `DestinationQuoteSnapshot` | `Domain/Entities` | `destination_quotes` |
| `PriceSource.CTraderQuoteSession` | `Domain/Enums` | enum token only |
| `RiskLimits.MaxQuoteAge` | `Domain/Risk` | **3s** (vs options 5000 ms) |
| `FixSessionDto` / `IDashboardQueries` | `Application/Dashboard` | read model |
| `TraderDbContext.FixSessionStates` | Infrastructure | table `fix_sessions` |
| `DemoSeeder` | Infrastructure | plants QUOTE `ReadyForMarketData` + TRADE `LoggedOn` against live host:5211/5212, `SenderCompId=live.pepperstone.1369850`, `TargetCompId=cServer` |
| `EfDashboardQueries` | Infrastructure | `QuoteHealthy` / `TradeHealthy` from those statuses; `ExecutionEnabled` **hardcoded false** |
| `GET /api/fix/sessions` | `apps/api` | reads seeder/worker rows |

**Application FIX ports (`ICTraderFixVenue`, `IFixSession`, `IFixQuoteClient`, `IFixTradeClient`, `IFixClock`):** **MISSING.**

---

## 8. Tag / message surface actually encoded

Parser accepts any `tag=value` pairs. Harness **emits** these tags:

`8, 9 (computed), 10 (computed), 11, 31, 32, 35, 37, 39, 45, 49, 50, 55, 56, 57, 58, 60, 98, 108, 141, 150, 371, 1007, 1128, 1320, 1321`

**Not emitted** (required for a honest ER/MD/SecurityList later): `14, 17, 34, 38, 40, 52, 54, 59, 103, 151, 268, 269, 270, 553, 554, 721, 1000–1006, 1008`.

MsgTypes the harness can name: `A`, `3`, `8`, `0`, `y`, `X`. Missing as first-class builders: `5` Logout, `W` Snapshot, `Y` MD reject, `D/F/G/H/AF/AN`, `AP`, `j`, `9`.

---

## 9. Missing-type checklist (this layer)

Names locked by A05/B05/A35/A36/A68. **None exist in `src/Fix.CTrader` except the four files above.**

| Required | Status |
|---|---|
| `CTraderQuoteSession` | **MISSING** |
| `CTraderTradeSession` | **MISSING** |
| `CTraderSessionRuntime` | **MISSING** |
| `CTraderFixSettingsFactory` / `quote.cfg` / `trade.cfg` | **MISSING** |
| `QuickFixCTraderVenue` + two `IApplication` | **MISSING** |
| `FIX44-CSERVER.xml` | **MISSING** |
| `ICTraderFixVenue` / `IFixSession` / quote+trade clients (Application) | **MISSING** |
| `CTraderFixSimulator` / `SimQuoteSession` / `SimTradeSession` | **MISSING** |
| `FixFieldList` | **MISSING** |
| `ITradeSessionOwnershipLease` Redis+PG (Infrastructure) | **MISSING** (in-memory stub only, unused) |
| `QuickFIXn.Core` + `QuickFIXn.FIX44` **1.14.1** | **MISSING** |
| `tests/Fix` project + A27/A68 class list | **MISSING** |

---

## 10. Counts

| Metric | Count |
|---|---:|
| Product `.cs` files | **4** |
| Product `.csproj` | **1** |
| Namespaces | **4** |
| Public types (incl. nested) | **7** |
| Public methods on harness | **13** |
| Public methods on parser | **2** |
| Direct NuGet packages (worktree) | **0** |
| Official QuickFIX/n packages | **0** |
| `TcpClient` FIX engines | **0** (correct absence) |
| Session objects QUOTE+TRADE | **0 / 2** |
| Dictionary / session cfg files | **0** |
| Call sites of assembly types outside itself | **0** |
| FIX unit/integration/replay tests that touch this assembly | **0** |
| Unstaged files vs HEAD | **4** (ownership clean) |

---

## 11. Safety notes (census, not a license)

1. Live `NewOrderSingle` is **off because nothing can send**, plus `RealCopyExecutionEnabled` default false on an **unbound** options type. That is **SAFE_BY_ABSENCE**, not a gate.
2. Default `Host` + `SenderCompId` document a **real** Pepperstone account form. Do not point a future initiator at this default as a first test.
3. Worker + seeder **lie** about QUOTE/TRADE health. Dashboard will show connected/logged-on without this assembly doing anything.
4. Do **not** re-add `QuickFix.Net`. Pin is A35: `QuickFIXn.Core` + `QuickFIXn.FIX44` 1.14.1.
5. Do **not** write a Spotware-sample `TcpClient` engine.
6. Do **not** treat `FixSimulationHarness` as §61 done.
7. `TargetCompId` case is still a **diagnostic Logon** question (`cServer` vs `CSERVER`). Worktree defaults `cServer`; do not silently fold.

---

## 12. Stale-report map

| Report | Claim | D05 measure |
|---|---|---|
| A05 | Empty `Class1`, 0 types, 0 packages | **Stale.** Four `.cs` files exist. |
| A08 | Fix.CTrader has zero `.cs` | **Stale.** |
| B05 | csproj still has `QuickFix.Net` 1.8.0; parser 6042 B / 120 lines | **True of HEAD csproj.** **False of worktree** (package line gone; parser 6016 B / 145 lines). |
| B28 / C19 | four files, no QuickFIX/n, unused harness | **Still true** of worktree. Hashes for the four `.cs` + worktree csproj **match** C19/B28. |

Use **this file** for the file/type/package inventory of `src/Fix.CTrader` as of this snapshot. Use B05 for the session+simulator **spec**. Use C19 for the QuickFIX/n **package** question. Use B28 for parser/lease **review**.

---

## 13. Sources

- `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Parsing\FixMessageParser.cs`
- `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs`
- `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs`
- `D:\Prop\src\Fix.CTrader\obj\project.assets.json`
- `D:\Prop\src\Fix.CTrader\obj\project.nuget.cache`
- `D:\Prop\src\Fix.CTrader\bin\Debug\net8.0\TraderIntelligence.Fix.CTrader.deps.json`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Domain\Entities\FixSessionState.cs`
- `D:\Prop\Mt5TraderIntelligence.sln`
- `D:\Prop\Directory.Build.props`
- `git -C D:\Prop` (`398a142`, unstaged Fix.CTrader diffs)

---

*End of D05. Product source was not modified.*
