# A010 — Prior swarm: what is proven vs still dummy; live group + trader fetch blockers

| Field | Value |
|---|---|
| Agent | A010 (senior engineer; prior-swarm synthesis) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A010_prior_swarm.md` |
| Inputs (read, not copied) | `D:\Prop\reports\INDEX.md`, `SWARM_LOG.md`, `CREDENTIALS_AND_COPY_STATUS.md`, `PHASE0_AUDIT.md` plus later pins C42, C43, C46, D24, D41, D67, E008, E011, E022, E030, R003, R012, R021 |
| Product source modified | **No** |
| Secret values printed | **None.** Password / proxy-auth slots classified by presence + class only. |

This file does **not** invent credentials, does **not** send `NewOrderSingle`, and does **not** treat a running dashboard as a live Manager session.

---

## 0. Verdict (binding — do not greenwash)

**Demo path is proven. Live group + trader fetch is not.**

The 2026-08-18 swarm (INDEX wave-2 recensus + later R/E pins) measured a working **in-process demo**: Domain algorithms compile; Fake/demo ingest of **4 logins / 18 canned deals** reconstructs and scores; React + API serve HTTP 200 against that InMemory book; live send is **off** (`SAFE_BY_ABSENCE`).

Live Achiever / StarwaveFX **Manager Connect → GroupTotal/GroupNext → UserGetByGroup** has **never been measured succeeding** from this product. Native C# walk code now exists (`NativeMt5BrokerConnector`) but it has **no proven session**.

Accepted gates (later files win; A57 inventory is stale — pin **D93** / scorecard **D41**):

| Gate | Score | Pin |
|---|---|---|
| §69 first useful version (accepted) | **0/12** | D41 / E030 |
| §68 go-live | **0/19** | A100 / C14 |
| §70 live FIX | **0/14** | A101 / D43 |
| Live MT5 connect | **NOT PROVEN** | C42 / E011 |
| Live FIX Logon | **NOT PROVEN** | C43 / E008 |
| ML | **not built (correct)** | C44 / B39 |
| `REAL_COPY_EXECUTION_ENABLED` | **false** | E038 / CREDENTIALS |

`PHASE0_AUDIT.md` is a **rubber-stamp** (C46): live rows are honest `MISSING`; Domain / SDK `EXISTS_AND_GOOD` is over-grade; no hashes. Do not use it as Phase 0 closeout.

`CREDENTIALS_AND_COPY_STATUS.md` is **half-stale**: “`D:\Prop\.env` = No” is **false as a filesystem claim**. E011 / E022 measured a gitignored placeholder clone (same blob as HEAD `.env.example`). “No **usable** operator secrets” is **true**. Process / user-secrets passwords **absent**. Do not treat that status sheet as the current secret-layout pin.

---

## 1. Already proven (measured, not marketing)

### 1.1 Lab / swarm process

- Wave-2 INDEX: **236+** markdown reports under `D:\Prop\reports\swarm\20260818\`; later recensus **312+** / **364** `.md` on disk. Product source was **not** the catalog write.
- Honesty pins exist and still bind: C42 (no live MT5), C43 (no live FIX), C44 (no ML), E008 (no forged `LoggedOn` writer), D94 (worker stamps `Disconnected`).
- Tests: Unit **64 passed / 22 skipped / 0 failed** (E004); skips are A43 `IQuantityConverter` backlog (E039). Reconstruction **6/6 smoke** (E018), **0/25** A21 bit-for-bit. That is smoke, not §60 coverage.

### 1.2 Product shape that actually exists

| Surface | Proven fact | Classification |
|---|---|---|
| Domain compile | B01: Domain compiles clean; Class1 gone | EXISTS |
| Solution membership | All product `.csproj` in `.sln` (A11/C57) | Membership PASS only |
| Volume scale | Default **10 000** lots (B14 / D14 / D92) | Binding |
| Reconstruction happy path | Scale-in / partial / reverse netting works in-process (B11) | Demo algorithm |
| First-3 / no LIVE auto-promote | `CanPromoteToLive` hard `false` (D97); demo scores SHADOW / RISK_BLOCKED / INSUFFICIENT_DATA | Vacuous lock, still correct |
| Scorer does not invent empty books | Login 10003 → `INSUFFICIENT_DATA` (C23) | PASS |
| Scorer does not fabricate MFE | `MaeMfeQuality=Unavailable` (D57) | Correct omission |
| Ingestion shape vs plan map | `GetGroupsAsync()` then all accounts; **not** filtered by `MT5_GROUP_*` (B32 / C10) | Shape PASS |
| Group walk **code** on C++ local Manager | `GroupTotal` + `GroupNext` (A39 / A84) | Capability of `mt5-sdk`, not a live attach |
| Group walk **code** on C# native connector | `NativeMt5BrokerConnector.GetGroupsCore` / `GetAccountsCore` (`UserGetByGroup`) | **Unproven at Connect** |
| net8 can **load** Manager wrapper | R021: factory `Initialize` + `CreateManager` on win-x64 when `MT5APIManager64.dll` is beside the process | Load ≠ Connect |
| Dashboard SPA | Vite `:3000` 16/16 destinations HTTP 200 (E032); API `:5000` demo maps 200 (E012 / E031) | Chrome + demo JSON |
| Live send | No `35=D`, flag false, `SAFE_BY_ABSENCE` (C07 / E002 / E034) | Correct |
| Compose topology | postgres / redis / Linux api only; **no** mt5-worker in Linux (D63) | Correct split |
| C++ SDK tree | Not deleted / not rewritten (C20) | Preserved |

Demo overview last measured (E031 / CREDENTIALS): **2 SHADOW, 1 RISK_BLOCKED, 0 LIVE**, `realCopyEnabled=false`. That is the **Fake / seeder book**, not venue state.

### 1.3 What “dashboard groups / traders” currently are

`GET /api/groups` and `GET /api/traders` are `EfDashboardQueries` over **InMemory + `DemoSeeder`**. Census is **4 logins** (10001, 10002, 10003, 99001) = **0.08%** of the §69 “5 000 accounts” bar (D95). Broker `Connected = true` has been a **literal** in the query layer (C42), not `IsConnectedAsync`. `/api/health` still advertises **“demo FakeMt5BrokerConnector — not live Manager”**.

---

## 2. Still dummy / unproven (do not cite as live)

| Surface | Why it is dummy |
|---|---|
| `FakeMt5BrokerConnector` | In-process list book. `ConnectAsync` flips a bool. No socket, no DLL, no password (D24 / C42). |
| `DemoSeeder` | Still called from API + mt5-worker `Program.cs` when `Brokers` is empty. Seeds catalog IPs/logins and canned tape. |
| `DealIngestionService` against Fake | Syncs **18** deals, not Manager history. |
| Dashboard `mt5Healthy` / brokers-connected | Inventory / `Enabled` / literals. E026 / E033: not a probe. |
| Destination quote `2399.45/2399.85` | Invented; `VenueInstrumentId=null` (E008 / D96). |
| Shadow rows | Rebuild side-effect of demo SHADOW scores; not A24 / not venue (D48 / E007). |
| FIX `LoggedOn` | **No** product writer today (E008). Rows are `Disconnected`. `SimulateLogon` ≠ TLS. |
| QuickFIX/n | Not referenced; pipe simulator only (C19 / D05). |
| EF | **0** `Migrations/`; `EnsureCreated` + InMemory unless a real `DATABASE_URL` (D51 / C29). 18/43 §45 tables (D19). |
| Outbox | Entity exists; no dispatcher (C58). |
| SignalR / Redis lease / RBAC / Serilog use / OTel | Package or entity only; unused or unmapped (D50 / C27 / D53 / D54 / C26). |
| Live / Audit / Shadow / Recon pages | Nav chrome / stubs, not §46–§54 books (D81–D84). |
| `MT5HttpClient::GetGroupDetails` | Hard `return false` (D67). Remote HTTP cannot fill `GroupDetail`. |
| `mt5-dump` / collector CLI | R004 is a **plan**. No dump JSON from a live Manager. |
| `mt5_group_probe` | Recipe known (R006); **exe not built** on that pass. |
| `USE_REAL_MT5` | Flag **unread** by product C# (R003). Later DI fail-closes on missing **both** passwords instead. |
| `EnvFile.Load` | **Zero callers** (R030 / R031). Flat `.env` keys do not enter `IConfiguration`. |
| PHASE0 “Domain algorithms EXISTS_AND_GOOD” | Over-grade vs unused shadow engine, thin tests, cancel-dirty leak (C46 / E006 / E024). |

**Stale report sentences (do not repeat):**

- A57 empty-tree inventory (Class1 / weatherforecast / 0 pages) — **D93**.
- D22 / C13 “seeder / worker stamps `LoggedOn`” — **E008 / D94**.
- CREDENTIALS “no `.env` file” — **E011 / E022** (file exists; **unfilled**).
- C42 / D24 “only implementor is Fake” — **superseded as source inventory**. `NativeMt5BrokerConnector` now implements `IMt5BrokerConnector` + `IMt5BulkDealReader`. The **honesty claim** (no live Connect) still holds.

---

## 3. Blockers remaining for **live group + trader fetch**

Goal (architecture §7–§9 / A39): Manager login → **all** visible groups (`GroupTotal`/`GroupNext`) → **all** users/accounts in those groups → persist → dashboard. Plan mappings are **labels**, never the fetch filter (A40 / B32 already match that shape).

These blockers are **conjunctive**. Filling one does not start fetch.

### B1. No host-readable operator secrets (first hard gate)

Measured (E011 / E022 / CREDENTIALS process-env rows / R005 class-only):

- Gitignored `D:\Prop\.env` is the **unfilled example** (placeholder slots, including `<SECRET>`).
- Process / User / Machine `MT5_PASSWORD` and `MT5_STARWAVEFX_PASSWORD` **absent**.
- .NET user-secrets stores for both worker IDs **absent**. API has no `UserSecretsId`.
- `LiveMt5Registration.HasRealPasswords` requires **both** Achiever **and** StarwaveFX slots to be non-empty and not placeholder. One filled broker cannot start the graph.

**Do not invent passwords. Do not copy sibling YoPips secrets into this tree.**

### B2. Hosts do not load dotenv

`EnvFile.Load` exists and is unused. `apps/api/Program.cs` and `apps/mt5-worker/Program.cs` call `AddTraderIntelligence(builder.Configuration)` with no `.env` ingest. Even a later **filled** `.env` would not reach `config["MT5_PASSWORD"]` until someone wires load (or sets process env / user-secrets).

Current composition (`DependencyInjection`): if passwords are not real, **throw** — “Dummy/fake broker data is disabled.” That is fail-closed for Fake, and also **blocks host start** until B1+B2 are solved. A running `:5000` dashboard is therefore **not** evidence that this graph is live; E033 already flagged a **stale API process**.

### B3. Achiever egress allow-list (this workstation)

R012 / C55: Achiever requires source IP **`81.29.145.69`** (non-secret). This desktop public egress measured **`106.219.132.213`**. TCP to Achiever `:443` is **OPEN**; the failure mode is **1012 IP block**, not reachability. Intended hop is Manager `ProxySet` HTTP to `81.29.145.69:49527` (listener OPEN). Process `ACHIEVER_PROXY_*` / `HTTP_PROXY` **unset**. Historical YoPips local connects with proxy disabled in-process failed **1012**.

StarwaveFX does **not** need that proxy (architecture §8). Achiever **does**, from this LAN box.

`NativeMt5BrokerConnector` can `ProxySet` **if** `ACHIEVER_PROXY_ENABLED` + host/port/user are in `IConfiguration` (same B2 problem). Proxy password must never be logged.

### B4. Native runtime beside the process (load, still not Connect)

R021: mixed-mode `MetaQuotes.MT5ManagerAPI64.dll` loads on **Windows x64 .NET 8** only when native `MT5APIManager64.dll` is in the factory directory. `mt5-worker.csproj` has **no** copy-dlls item (A105 still a gap). Linux / Compose worker is correctly **forbidden** (D63). Live fetch cannot run in the Linux API container.

### B5. Connect + pump never proven for this product

`NativeMt5BrokerConnector.ConnectCore` uses `PUMP_MODE_GROUPS | USERS | POSITIONS` (good for group/user cache; deals go through `DealRequest` / `DealRequestByGroup`). **No** swarm report records `MT_RET_OK` from product `Connect` against Achiever or StarwaveFX. C++ `mt5_group_probe` was **not** built (R006). R021 stopped at factory create. R012 **did not** attempt authenticated Connect.

Until a dated log / probe JSON shows `connection.success` **and** `GroupTotal > 0` (or honest 0), group fetch is **unproven**.

### B6. Demo seeder + InMemory still sit on the same hosts

Even after a real Connect, API and mt5-worker still:

1. `EnsureCreatedAsync` (no migrations; default InMemory if `DATABASE_URL` is missing / placeholder).
2. `DemoSeeder.SeedAsync` if `Brokers` is empty — writes the **4-login demo catalog** (including live-looking IPs) **before** `LiveIngestHostedService` walks the native connector.

A live group walk dumped into InMemory that already contains the demo book is not a production fetch. Need: real Postgres + skip / refuse demo seed when native connectors are registered.

### B7. Dashboard / ingest still think in demo logins

`POST /api/ops/resync` hard-codes rebuild of **10001 / 10002 / 10003 / 99001**. That is a demo door, not “score every fetched trader.” `LiveIngestHostedService` is the correct shape (sync → `ListLoginsAsync` → rebuild) **if** Connect works and the store is the live book.

### B8. Remote / HTTP path cannot replace local Manager for details

If someone tries `MT5_MODE=remote` / C++ `MT5HttpClient`: `GetGroupDetails` is a **hard-false stub** (D67). Names/count/logins HTTP exist on the C++ client; C# product does **not** call them. Live group+trader fetch for this increment is **Windows local Manager**, not remote HTTP.

### B9. Unrelated but still blocking “traders” as a product noun

Once groups/accounts land:

- Checkpoints / idempotent `(broker_id, ticket)` Phase-1 path **unproven** (A59 / D20).
- Cancelled-deal dirty flag is **helper-only**; score/dashboard leak dirty first-3 (E006 / E024).
- Queries are N+1 / full-table (C36) — **UNSAFE** at 5k accounts.
- No RBAC on `POST /api/ops/resync` (D53 / D30) — do not expose that door on a live Manager host.

These do not prevent the first `GroupTotal` call. They prevent calling the result “Phase 1 done.”

---

## 4. What is **not** a blocker for group + trader fetch

| Item | Why it is out of this increment |
|---|---|
| Live FIX QUOTE/TRADE Logon | Destination venue. Needed for shadow/live copy, **not** for MT5 group/user list. |
| `REAL_COPY_EXECUTION_ENABLED` / `35=D` | Must stay **false** / absent. Fetch is read-only. |
| QuickFIX/n, tag 55 discovery, TRAIL session lease | Destination. |
| ML / Models page | Phase 6 closed (C39 / C44). |
| Kafka / K8s / ClickHouse | Non-goals (A80). |
| Filling `GetGroupDetails` on `MT5HttpClient` | Only if remote mode is chosen; local native walk does not need it. |

---

## 5. Minimum sequence to **prove** live group + trader fetch

Do not start this sequence by inventing secrets or enabling copy.

1. Operator supplies Manager passwords (and Achiever proxy auth) into a host-readable store. Classify only; never commit.
2. Wire `EnvFile.Load` (or user-secrets) **before** `AddTraderIntelligence`. Fail-closed if placeholder.
3. Copy Manager native DLLs beside the **Windows** worker (A105). Confirm factory init (R021 already did this in a scratch tree).
4. Enable Achiever HTTP proxy on this LAN box; StarwaveFX direct.
5. Run a **read-only** Connect + `GroupTotal`/`GroupNext` + `UserGetByGroup` (product connector or `mt5_group_probe`). Persist counts + `MTRetCode` + timestamp. **No** `SendTrade`.
6. Disable `DemoSeeder` when native connectors are live. Point at Postgres (migrations still missing — at least do not mix the 4-login tape).
7. Drive `LiveIngestHostedService` / `DealIngestionService` and show dashboard groups/traders **≠** `{10001,10002,10003,99001}`.

Until step 5 produces a measured `MT_RET_OK`, every “groups fetched” sentence is **dummy**.

---

## 6. One-liner

**Proven:** demo reconstruct/score/dashboard against 4 fake logins; send off; native **code** can walk groups/users; net8 can **load** the Manager DLL.  
**Dummy:** live Achiever/Starwave sessions, live group list, live trader census, live FIX.  
**Next blockers for live group+trader fetch:** usable secrets in a store the host actually loads, Achiever proxy/allow-list, DLL-beside-process, measured Connect, refuse demo seed.

```text
LIVE_GROUP_FETCH     = NOT PROVEN
LIVE_TRADER_FETCH    = NOT PROVEN
DEMO_BOOK            = YES (4 logins / 18 deals)
NATIVE_CONNECTOR     = CODE EXISTS / SESSION UNPROVEN
FAKE_REFUSE          = DI throws without both real passwords
ENV_LOADED_BY_HOST   = NO
USABLE_PASSWORDS     = NO
ACHIEVER_EGRESS_OK   = NO (this desktop)
§69 / §68 / §70      = 0/12 , 0/19 , 0/14
```
