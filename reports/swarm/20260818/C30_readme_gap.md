# C30 — `D:\Prop\README.md` existence + landing-page gaps

| Field | Value |
|---|---|
| Agent | C30 (senior engineer, README existence + gap only) |
| Date | 2026-08-18 |
| Measured at (local) | 2026-08-18T13:26:07.6594355+05:30 (file `LastWriteTime`; hash taken this pass) |
| Measured at (UTC) | 2026-08-18T07:56:07.6594355Z |
| Workspace | `D:\Prop` |
| Assigned question | Is there `D:\Prop\README.md`? If missing, note it. Write this report. |
| Product source modified | **No.** This report is the only write. `D:\Prop\README.md`, `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\docs` were not edited. |
| Binding law | Architecture v2 §66 (docs tree; root README is **not** a §66 name), §55–§56 (secrets / `.env.example`), §5 (Windows MT5), §41 / §69 (real copy off; first useful version), §71 / §73.B |
| Relates | A30, A57, A66, A75, A87, B38, B40, B41, C04, C11, C12 |
| Classification vocabulary | Architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE` |

---

## 0. Assigned answer

**`D:\Prop\README.md` exists. It is not missing.**

Do **not** invent a second root README. Do **not** treat this file as a missing-doc ticket. The remaining work is **content quality**, not creation.

| Question | Answer | Evidence |
|---|---|---|
| Does `D:\Prop\README.md` exist? | **Yes.** | `Get-Item` + `read_file` this pass |
| Bytes / lines | **1746** bytes, **49** lines, **33** non-blank | PowerShell census |
| SHA-256 | `C023B4227A1C511F346A1FD96B6648B901001551731D1472FE7A33468B1A2764` | `Get-FileHash SHA256` |
| Encoding | UTF-8, **no BOM**, **LF only** (49 LF, 0 CRLF) | Byte + regex census |
| Attributes | `Archive` | `Get-Item` |
| `docs/README.md` | **MISSING** (optional A30 / A66 index; **not** the assigned file) | `list_dir` `D:\Prop\docs` |
| `mt5-sdk/README.md` | **EXISTS** (SDK-local; not the product landing page) | `D:\Prop\mt5-sdk\README.md` |
| Class of the root file | **EXISTS_NEEDS_REFACTOR** | Present, honest on safety, incomplete as a landing page |

Honest one-liner: **the product has a 49-line root README that points at the v2 spec, embeds a placeholder PNG, and can start the API + Vite demo. It is not missing. It is not a complete operator landing page.**

---

## 1. Method

1. `read_file` `D:\Prop\README.md` in full (49 lines). File **opened**; it is **not** a missing-path error.
2. Re-measure size, SHA-256, `LastWriteTime` / UTC, line endings, BOM, non-blank count (`Get-Item` + `Get-FileHash` + `[IO.File]::ReadAllBytes`).
3. Inventory sibling names: `D:\Prop\README*`, `D:\Prop\docs\README.md`, `D:\Prop\mt5-sdk\README.md`.
4. Claim-check every README sentence against the tree: `docs/architecture.png` / `.svg`, `.env.example`, `Mt5TraderIntelligence.sln`, `apps/api` + `apps/web` ports, `DemoSeeder`, `DependencyInjection` InMemory fallback, `CTraderFixOptions.RealCopyExecutionEnabled`, `vite.config.ts`, `launchSettings.json`, `docker-compose.yml`, C04 / C11 / C12 / B40 / B41.
5. Compare to architecture §66 (does **not** require a root README), A66 optional `docs/README.md`, A75 / B40 `.env.example` law.

No `dotnet`, no `npm`, no product edit, no rewrite of `README.md`.

---

## 2. On-disk file (authoritative)

| Field | Value |
|---|---|
| Path | `D:\Prop\README.md` |
| Exists | **True** |
| Bytes | **1746** |
| Lines (file) | **49** (ends after the Native MT5 paragraph; no trailing H2) |
| Non-blank lines | **33** |
| SHA-256 | `C023B4227A1C511F346A1FD96B6648B901001551731D1472FE7A33468B1A2764` |
| LastWriteTime | `2026-08-18T13:26:07.6594355+05:30` |
| LastWriteUtc | `2026-08-18T07:56:07.6594355Z` |
| Line endings | **LF only** |
| UTF-8 BOM | **No** |
| H1 count | **2** (`# Trader Intelligence` line 1; `# MT5 XAUUSD Trader Intelligence + cTrader FIX 4.4` line 20) |
| H2 count | **3** (`Safety`, `Run (demo)`, `Native MT5`) |

Exact body as of the hash above:

```markdown
# Trader Intelligence

Short architecture overview and where to find implementation details.

![Architecture](docs/architecture.png)

Summary: lightweight C#/.NET backend that ingests MT5 manager events, reconstructs trades, scores XAUUSD traders, shadow-copies approved trades and routes execution to a cTrader FIX 4.4 adapter.

Key components:

- **Ingest / Collectors:** `apps/mt5-worker`
- **API:** `apps/api`
- **Workers:** `apps/mt5-worker`, `apps/fix-worker`
- **Domain logic:** `src/Domain`
- **Persistence / Infrastructure:** `src/Infrastructure`
- **FIX adapter:** `src/Fix.CTrader`
- **Web dashboard:** `apps/web`

For the full architecture spec see `MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` and `docs/architecture.md`.
# MT5 XAUUSD Trader Intelligence + cTrader FIX 4.4

Identify copyable XAUUSD traders from ~5,000 MT5 accounts, shadow them, and only then (explicit flag) route risk-approved orders to Pepperstone/cServer FIX 4.4.

Architecture: `MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`

## Safety

- Real NewOrderSingle is **off** (`REAL_COPY_EXECUTION_ENABLED=false`).
- Trade #3 is early evidence, never LIVE promotion.
- Secrets stay in environment / `.env` (see `.env.example`). Never sent to React.

## Run (demo)

```powershell
dotnet test D:\Prop\Mt5TraderIntelligence.sln
dotnet run --project D:\Prop\apps\api\TraderIntelligence.Api.csproj
cd D:\Prop\apps\web
npm install
npm run dev
```

API: http://localhost:5000  
Dashboard: http://localhost:3000  

Without Postgres the API uses EF InMemory and seeds Achiever + StarwaveFX demo accounts.

## Native MT5

`mt5-sdk` is Windows-only for local Manager API. Keep the worker on Windows. Do not Linux-container the native DLL.
```

Two untitled blocks are concatenated: a short “Trader Intelligence” map, then a second H1 that restarts the product name. No blank line between the spec pointer (line 19) and the second H1 (line 20).

---

## 3. Sibling README inventory

| Path | Status | Role |
|---|---|---|
| `D:\Prop\README.md` | **EXISTS** | Product landing page (this ticket) |
| `D:\Prop\docs\README.md` | **MISSING** | Optional A30 I0 / A66 §15 index of the eleven §66 files. **Not** required by architecture §66. **Not** a substitute for the root file. |
| `D:\Prop\mt5-sdk\README.md` | **EXISTS** | C++ SDK-local (`IMT5Client`, local vs HTTP). Cite from a future `docs/mt5-integration.md`. |
| `D:\Prop\apps\*\README.md` | **MISSING** | No per-app README (acceptable; root should point at apps) |
| Other `D:\Prop\README*` | **None** | `Get-ChildItem -Filter README*` returned only the root file |

Architecture §66 names **eleven** files under `/docs`. It does **not** name a root `README.md`. Absence of `docs/README.md` is an **optional-index gap**, not a §66 fail. Absence of the **root** file would have been a landing-page fail. That fail did **not** occur.

---

## 4. Claim-check matrix (README sentence → tree)

| # | README claim | Tree fact | Class |
|---|---|---|---|
| 1 | File presents itself as the landing README | File is at repo root, 1746 bytes | **EXISTS** |
| 2 | `![Architecture](docs/architecture.png)` | `docs/architecture.png` **exists** (12081 bytes, SHA-256 `0F7BAF6D2461A5A055C83C278FCD0A8F718B3C2B86C19886221FDCB259EC98C9`) | **Link target exists** |
| 3 | Implied: the PNG is an architecture diagram | Visual + `scripts/svg_to_png.py`: Pillow **placeholder** (“Architecture diagram (placeholder)”), truncated pipeline text. C11 §6.2. SVG sibling is the real diagram (2697 bytes, SHA-256 `23F51B89D6CA6FC4A649E9A3F7DC04AFCB42485892D8604E3ACAD18EAFEB4327`) | **EXISTS_NEEDS_REFACTOR** (wrong asset embedded) |
| 4 | Ingest = `apps/mt5-worker` | Folder + `TraderIntelligence.Mt5Worker.csproj` exist | **True** |
| 5 | API = `apps/api` | Exists | **True** |
| 6 | Workers = `mt5-worker` + `fix-worker` | Both exist | **True** |
| 7 | Domain = `src/Domain` | Exists | **True** |
| 8 | Persistence = `src/Infrastructure` | Exists; InMemory + Npgsql split in `DependencyInjection.cs` 19–29 | **True** |
| 9 | FIX = `src/Fix.CTrader` | Exists; options + parser + harness; **no** live `NewOrderSingle` send | **True as a path**; “routes execution” in the summary is **overclaim** |
| 10 | Web = `apps/web` | Vite + React pages exist | **True** |
| 11 | Points at v2 spec + `docs/architecture.md` | Both exist (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` 50966 bytes; `docs/architecture.md` 28-line honest stub) | **True** |
| 12 | Goal: ~5,000 MT5 accounts → shadow → flag-gated Pepperstone/cServer | Matches architecture §1 direction | **True as intent**; not claimed as done |
| 13 | `REAL_COPY_EXECUTION_ENABLED=false` | `.env.example` line 73 `false`; `CTraderFixOptions.RealCopyExecutionEnabled` default `false`; API `/api/settings` hardcodes `false`; `CanPromoteToLive` ⇒ `false` | **True** |
| 14 | Trade #3 is early evidence, never LIVE | Architecture §15 / §23; scorer hard-blocks live promotion | **True / EXISTS_AND_GOOD** |
| 15 | Secrets in env / `.env.example`; never sent to React | `D:\Prop\.env.example` **exists** (3408 bytes, SHA-256 `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA`). C04: no §55 secret on live maps. | **Pointer true**; example is **not** placeholder-only (B40 FLAG — live hosts / login `2027` / `9904` / FIX account `1369850`) |
| 16 | `dotnet test D:\Prop\Mt5TraderIntelligence.sln` | `D:\Prop\Mt5TraderIntelligence.sln` exists; lists Domain / Application / Infrastructure / Mt5 / Fix.CTrader / three hosts / two test projects | **Path true**; this agent did **not** run tests |
| 17 | `dotnet run` API csproj | Path exists | **Path true**; process not started |
| 18 | `cd apps/web` + `npm install` + `npm run dev` | `package.json` scripts `dev` = `vite`; `vite.config.ts` `server.port = 3000` | **True** |
| 19 | API `http://localhost:5000` | `launchSettings.json` `profiles.http.applicationUrl` = `http://localhost:5000`; web `client.ts` fallback same. B41: default-profile mismatch **CLOSED**. IIS Express still `:18720`. | **True for `dotnet run` / `http` profile** |
| 20 | Dashboard `http://localhost:3000` | Vite port 3000 | **True** |
| 21 | Without Postgres → EF InMemory + Achiever + StarwaveFX seed | `DependencyInjection.cs` 22–25 InMemory when connection empty / contains `<SECRET>`; `apps/api/Program.cs` 84–93 `EnsureCreatedAsync` + `DemoSeeder`; seeder writes Achiever + StarwaveFX brokers and logins `10001`/`10002`/`10003`/`99001` | **True** |
| 22 | `mt5-sdk` Windows-only; do not Linux-container the native DLL | `mt5-sdk/CMakeLists.txt` `WIN32` copy gate; C12: compose has **no** `mt5-worker` service; compose comment line 30 matches this sentence | **EXISTS_AND_GOOD** |

No secrets (passwords) appear in the README body. No live FIX password. No “Phase 1 Done.” No ML-as-shipped claim. Those absences are **correct**.

---

## 5. Gaps (why this is not `EXISTS_AND_GOOD`)

The file is **present**. These are **content / structure** gaps, not a missing-file finding.

### 5.1 Structural / editorial

| ID | Sev | Gap |
|---|---|---|
| C30-01 | P3 | **Two H1 titles** for one page. GitHub / most renderers treat both as top-level. Looks like two READMEs pasted together. |
| C30-02 | P3 | No blank line before the second H1. Spec pointer on line 19 runs into `# MT5 XAUUSD…`. |
| C30-03 | P3 | Absolute lab paths (`D:\Prop\...`) in the run block. Clone-to-another-drive / CI will not match. Relative `Mt5TraderIntelligence.sln` / `apps/api/...` would travel. |
| C30-04 | P3 | **Ingest** and **Workers** both name `apps/mt5-worker`. Redundant; hides `src/Mt5` (C# connector) and `mt5-sdk` (native). |

### 5.2 Diagram embed

| ID | Sev | Gap |
|---|---|---|
| C30-05 | P2 | README embeds `docs/architecture.png`, which is a **Pillow placeholder** (`scripts/svg_to_png.py` fallback after cairosvg missing). The real pipeline drawing is `docs/architecture.svg` (C11 §6). A later README edit should embed the SVG (or a real raster of it), not this PNG. **Not done here.** |

### 5.3 Component map incomplete vs the tree

Listed: `apps/mt5-worker`, `apps/api`, `apps/fix-worker`, `src/Domain`, `src/Infrastructure`, `src/Fix.CTrader`, `apps/web`.

**On disk and omitted:**

| Missing from README | Why it matters |
|---|---|
| `src/Application` | Ingestion + dashboard contracts live here, not in Domain |
| `src/Mt5` | Fake / connector layer the workers actually compile against |
| `mt5-sdk/` | Named only in the last paragraph; not in the component list |
| `tests/Unit`, `tests/Integration` | The `dotnet test` line has no map of what those tests cover |
| `docker-compose.yml` | Postgres 16 + Redis 7 + Linux `api` (C12). Demo run never mentions it |
| `docs/*.md` specialist set | Only `architecture.md` is linked. Five §66 names are still missing (C11); four more stubs exist unlinked |

### 5.4 Run (demo) is API + Vite only

The block does **not** say:

- Prerequisites: .NET 8 SDK, Node.js, optional Docker Engine (C12: `docker` **not** on PATH on this host).
- Copy `.env.example` → `.env` (and that the example still carries live **identifiers** — B40).
- `apps/mt5-worker` / `apps/fix-worker` are **optional** for the InMemory demo because `Program.cs` seeds on API startup.
- Compose `api` and host `dotnet run` **cannot** share `:5000` (C12).
- IIS Express profile is **not** `:5000` (B41).
- `npm install` is a one-time step; lockfile already present.

None of those omissions make the listed commands wrong. They make the page incomplete for a new engineer.

### 5.5 Overclaims (wording, not “phase done”)

| Sentence | Risk | Honest restatement (for a later edit, not this agent) |
|---|---|---|
| “shadow-copies approved trades and **routes execution** to a cTrader FIX 4.4 adapter” | Reader can hear “orders already go to Pepperstone.” | Adapter **project** exists. Live `NewOrderSingle` is **off**. First useful version (A57 / §69) is **not** accepted. |
| “lightweight C#/.NET backend that **ingests MT5 manager events**” | Demo ingest is `FakeMt5BrokerConnector` + `DemoSeeder`, not Manager `LoadLibrary`. | True of the **target** pipeline. Demo path is seeded fakes. Native DLL stays Windows-only. |

Safety bullets **correct** the overclaim if the reader continues past the summary. The summary is still the first paragraph GitHub shows.

### 5.6 Operator / law topics the landing page does not carry

A landing README is not `docs/deployment.md`. It still needs a **short** pointer set so a new hire does not invent process:

| Topic | In README? | Where the fact lives today |
|---|---|---|
| What the system **is not** (not an LP; A87) | **No** | A87 + architecture §1.6 |
| First useful version = 12 §69 items, currently **not** accepted | **No** | A57 (0/12 at that measurement; later C-wave added pages/API but §69 e2e still not claimed here) |
| §66 docs 6/11 stubs, 5 missing | **No** | C11 |
| Feature-flag trio `CTRADER_FIX_*` + real-copy floor | Only the last flag | A49, A75, `.env.example` |
| Compose: Linux API/Postgres/Redis; **never** MT5 worker | Last paragraph only (native DLL) | C12 |
| `docs/README.md` index | N/A (file missing) | A30 / A66 optional |
| Prerequisites / troubleshooting / license | **No** | nowhere as product docs |

### 5.7 `.env.example` pointer is a half-truth

README: “see `.env.example`.” The file **exists**. A75 required **placeholders only**. B40 measured live Achiever / StarwaveFX / Pepperstone **identifiers** in the committed example (passwords are `<SECRET>`). A later README sentence should say: copy `.env.example` → `.env`; do not commit `.env`; treat host/login/account values as **lab identifiers**, not a license to send. **Do not** paste those identifiers into the README.

---

## 6. What the README already gets right (do not regress)

| Item | Why keep |
|---|---|
| File **exists** at the repo root | Assigned question |
| Points at v2 spec by **filename** (not a second clone) | A66 / §66 |
| Points at `docs/architecture.md` | Correct specialist entry |
| `REAL_COPY_EXECUTION_ENABLED=false` in bold | §41 / §56 |
| Trade #3 ≠ LIVE | §15 / §23 |
| Secrets not in React | §55; C04 measured |
| InMemory demo + named seed brokers | Matches `DemoSeeder` + DI |
| Ports **5000** / **3000** | Matches `http` profile + Vite (B41 closed) |
| Native Manager stays on Windows; no Linux-container DLL | §5; C12 PASS |
| No ML claim, no “Phase 1 Done”, no live password | Honesty |

Class of those bullets: **EXISTS_AND_GOOD**. The page’s problem is **missing map + placeholder figure + dual H1**, not unsafe fiction.

---

## 7. `docs/README.md` (out of the assigned path, recorded so it is not confused)

A30 Increment 0 suggested `docs/README.md` as a 20-line index at the v2 spec + eleven §66 files. A66 marked it **optional**. C11 confirmed it is still **absent**.

That absence is **not** “`D:\Prop\README.md` is missing.” Do **not** create `docs/README.md` from this agent. Do **not** move the root README into `docs/`.

---

## 8. Recommended later README edit (authorized docs task — **not this agent**)

Single file: keep `D:\Prop\README.md`. Do not add a second root name.

Suggested shape (do not implement here):

1. **One** H1: `MT5 XAUUSD Trader Intelligence + cTrader FIX 4.4`.
2. One-paragraph **is / is-not** (observe ~5k XAUUSD traders; not an LP; not live-by-default).
3. Embed `docs/architecture.svg` (or a real PNG of it). Stop embedding the placeholder.
4. Component table that adds `src/Application`, `src/Mt5`, `mt5-sdk`, `tests/`, `docker-compose.yml`.
5. Safety block (keep the three bullets; add “not an LP”).
6. Run (demo) with **relative** paths; note InMemory seed; note workers optional for demo; note Compose port clash.
7. Link list: v2 spec, `docs/architecture.md`, and the other `docs/*.md` **that exist** (do not link the five missing names as if they were written).
8. Keep the Native MT5 Windows sentence.

Do **not** paste architecture §56 live IPs, manager logins, or FIX account `1369850` into the README.

---

## 9. Secrets / greenwash checks on the README itself

| Check | Result |
|---|---|
| Live FIX password / MT5 password / DB password in `README.md` | **Absent** |
| Account `1369850` / manager logins / broker IPs in `README.md` | **Absent** |
| “Phase 1 Done” / API Gateway / live-by-default | **Absent** |
| ML claimed as shipped | **Absent** |
| Real `NewOrderSingle` claimed on | **Absent** (explicitly off) |
| Wholesale paste of v2 | **No** (1746 bytes) |

---

## 10. Stale swarm sentences (do not reuse)

| Prior claim | Now |
|---|---|
| B38: README embeds the **SVG** | **Stale.** Current line 5 embeds **`docs/architecture.png`**. |
| A66 / early A-wave implying docs-only landing | Root README **exists** and is the operator front door. |
| “README missing” as a create-ticket | **False.** File is on disk at the assigned path. |
| A06 / A54 “API is `:5160`” if copied into a README rewrite | **Stale** (B41). Keep **5000**. |

---

## 11. Out of scope (not done)

- Did **not** edit `D:\Prop\README.md`.
- Did **not** create `docs/README.md`.
- Did **not** replace `docs/architecture.png`.
- Did **not** run `dotnet test` / `dotnet run` / `npm`.
- Did **not** modify product source under `src/` or `apps/`.
- Did **not** rewrite `INDEX.md` / `SWARM_LOG.md` (out of this ticket’s write set).

**Deliverable:** `D:\Prop\reports\swarm\20260818\C30_readme_gap.md` only.

---

## 12. Assigned one-liner

**`D:\Prop\README.md` is present** (1746 bytes, 49 lines, SHA-256 `C023B4227A1C511F346A1FD96B6648B901001551731D1472FE7A33468B1A2764`). It is **not** missing. Class **EXISTS_NEEDS_REFACTOR**: dual H1, placeholder PNG, incomplete component/run map, mild “routes execution” overclaim; safety / ports / Windows-MT5 / InMemory-seed claims check out.
