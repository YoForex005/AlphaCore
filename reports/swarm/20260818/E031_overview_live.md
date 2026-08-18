# E031 — Live `GET /api/overview`: 2 SHADOW, 1 RISK_BLOCKED, 0 LIVE

| Field | Value |
|---|---|
| Agent | E031 (senior engineer, live overview state rollup only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:26+05:30 (first GET) / 2026-08-18T13:50:49+05:30 (headers + state filters) / Kestrel `Date` `Tue, 18 Aug 2026 08:20:49 GMT` |
| Artifact | `D:\Prop\reports\swarm\20260818\E031_overview_live.md` |
| Assigned | API overview has 2 SHADOW, 1 RISK_BLOCKED, 0 LIVE. Write this file. **Do not modify product source.** |
| Workspace | `D:\Prop` (API host `apps/api`; Vite is **not** under `D:\Prop\src`) |
| Product source modified | **No.** This report (plus catalog notes in `INDEX.md` / `SWARM_LOG.md`) is the only write. |
| Test source modified | **No** |
| Method | Live `Invoke-WebRequest` against already-running Kestrel on `127.0.0.1:5000` and `localhost:5000`. Capture `/api/overview`, `/api/v1/overview`, `/api/traders`, `?state=SHADOW` / `LIVE` / `RISK_BLOCKED`, `/health`, `/api/settings`. Read `Program.cs` L54, `OverviewDto`, `GetOverviewAsync`, `GetTradersAsync`, `DemoSeeder`, `TraderStateMachine.FromBaseline`, `TraderState`, `FakeMt5BrokerConnector` tape. SHA-256 + `git hash-object` + `git status --short`. Did **not** start/kill the API. Did **not** edit product source. |
| Binding law | Architecture §22 states; §23 trade #3 → SHADOW never LIVE; §47 Overview tiles; A22 I5 / R5–R6; A91 nested snapshot; A26 `GET /api/v1/overview`; A49 `REAL_COPY_EXECUTION_ENABLED` default false |
| Honesty siblings | D77 (page vs §47 + prior 13:43 capture), D21 (query), D48 (shadow rows), D12 / D97 (`CanPromoteToLive => false`), C42 / C43 (no live MT5 / FIX), CREDENTIALS_AND_COPY_STATUS.md |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`) |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

This is a **live rollup capture**. It is **not** a claim that §47 is complete, that `/api/v1/overview` exists, that these are live-venue traders, or that 0 LIVE is a go-live gate pass.

---

## 0. Verdict

**Confirmed on the running demo API: `shadow=2`, `riskBlocked=1`, `live=0`.** Same body on `127.0.0.1` and `localhost`. HTTP **200**, bare camelCase object, no `{ data }` envelope, no `Authorization`, no `Cache-Control: no-store`.

| Rollup (wire key) | Live JSON | Cross-check `GET /api/traders` |
|---|---:|---|
| `shadow` | **2** | 10001 ACHIEVER + 99001 STARWAVEFX, both `"state":"SHADOW"` |
| `riskBlocked` | **1** | 10002 ACHIEVER `"state":"RISK_BLOCKED"` |
| `live` | **0** | `?state=LIVE` → `[]` |
| `liveCandidates` | **0** | no `LIVE_CANDIDATE` row |
| `watch` | **0** | no `WATCH` row |

The fourth demo score is **not** in those three buckets: login **10003** is `"state":"INSUFFICIENT_DATA"` (`completedXauTrades: 0`). Overview does not expose that leftover as a tile.

**Do not read this as live copy.** `realCopyEnabled` is constructor literal **`false`**. `/api/settings` repeats `REAL_COPY_EXECUTION_ENABLED: false`. `CanPromoteToLive(_) => false`. Fake tape + `DemoSeeder` rebuild produced the three states. Destination book is **off**.

| Question | Measured answer |
|---|---|
| Does live `GET /api/overview` say 2 / 1 / 0? | **Yes.** |
| Does `GET /api/traders` agree? | **Yes** (2 SHADOW + 1 RISK_BLOCKED + 0 LIVE + 1 leftover INSUFFICIENT_DATA). |
| Is `GET /api/v1/overview` the contract path? | **404.** A26 / A91 catalog is **MISSING** on this host. |
| Are these live Achiever / Starwave / Pepperstone books? | **No.** Fake connector + InMemory/demo seed. |
| Does 0 LIVE mean promotion is gated by A22 R5? | **No.** Vacuous lock (`CanPromoteToLive` hard-false; `FromBaseline` reachable set never includes `LIVE`). |
| Does the Overview **page** paint `live: 0`? | **No.** DTO field exists; `OverviewPage.tsx` never reads `data.live` (D77). |
| Product source edited by E031? | **No.** |

Classification:

| Slice | §73.B |
|---|---|
| Demo rollup 2 / 1 / 0 on `/api/overview` | **EXISTS_AND_GOOD** as a measured Fake-book fact |
| Same numbers as architecture §47 / A91 health grid | **MISSING** (flat DTO, no envelope, no v1) |
| `live: 0` as go-live evidence | **UNSAFE** to treat as a gate pass — it is a seeder + hard-false promote |
| `mt5Healthy: true` on the same body | **UNSAFE** (`brokers.Enabled > 0`; C42 still unproven) |
| `shadowPnl: 248.20` as A24 net | **UNSAFE** (Σ `SourceVsShadowSlippage`; D48) |
| Product source this pass | **unchanged** |

---

## 1. Method (this pass)

1. `Invoke-WebRequest` `http://127.0.0.1:5000/api/overview` and `http://localhost:5000/api/overview` — both HTTP 200, identical JSON.
2. Same host: `/api/traders` (full), `?state=SHADOW`, `?state=LIVE`, `?state=RISK_BLOCKED`, `/health`, `/api/settings`.
3. `GET /api/v1/overview` → **404**.
4. Read `apps/api/Program.cs` L54 map; `OverviewDto`; `EfDashboardQueries.GetOverviewAsync` L14–43 and `GetTradersAsync` L74–117; `DemoSeeder` rebuild loop; `TraderStateMachine.FromBaseline`; `TraderState` enum; Fake tape logins 10001 / 10002 / 10003 / 99001.
5. PowerShell SHA-256 + length + last-write; `git hash-object`; `git rev-parse HEAD`; `git status --short` on the four dirty/untracked subjects.
6. **Did not** `dotnet run`, seed again, or `POST /api/ops/resync`. **Did not** edit product source.

---

## 2. Measured files (unchanged this pass)

| Path | Bytes | Phys. (non-blank) | SHA-256 | git blob | Worktree | Role |
|---|---:|---:|---|---|---|---|
| `src/Infrastructure/Dashboard/EfDashboardQueries.cs` | 8708 | 182 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | `d9bed4fc73f4e189cd21c1d31e58ab4a2c291dac` | **untracked** (`??`) | counts `TraderScores.CurrentState` |
| `src/Application/Dashboard/DashboardModels.cs` | 3088 | 104 | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` | `16be45846cd3b3c16c745ef3a5842b2d5621c7b7` | unstaged (`M`) | `OverviewDto` 17 fields including `Live` |
| `apps/api/Program.cs` | 4731 | 86 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | `9d623e1da0adc050b15a6d04f330169be2a125e5` | unstaged (`M`) | L54 `MapGet("/api/overview")` |
| `src/Infrastructure/Seeding/DemoSeeder.cs` | 5082 | 129 | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | `d65f09fa48d9045537c4fff358f523d9e4440896` | **untracked** (`??`) | rebuilds 10001 / 10002 / 10003 / 99001 |
| `src/Domain/Scoring/BaselineScorer.cs` | 8143 | 187 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` | `26095e9b58fed40d57e767c9c9676d8409e87350` | tracked | `FromBaseline` + `CanPromoteToLive => false` |
| `apps/web/src/pages/OverviewPage.tsx` | 2078 | 33 | `6497193F190445CCF76AED218E01E3BC85050238CB89D06002837FC9C502825C` | — | **untracked** | paints `shadow` / `riskBlocked`; **drops** `live` |

Hashes of query / DTO / Program / seeder / scorer / page are **unchanged vs D77 / E008 / D12**. The live JSON is the same 17-key object D77 captured at 13:43:05, remeasured ~7 minutes later.

---

## 3. Live wire capture

### 3.1 `GET http://127.0.0.1:5000/api/overview`

HTTP **200**. `Content-Type: application/json; charset=utf-8`. `Server: Kestrel`. **No** `Cache-Control`. **No** `X-Correlation-Id`. **No** `WWW-Authenticate`. CORS was already proven on this pair by E012 (`Access-Control-Allow-Origin: *`).

```json
{
  "totalAccounts": 4,
  "connectedBrokers": 2,
  "xauTraders": 3,
  "tradersWithThreeTrades": 3,
  "watch": 0,
  "shadow": 2,
  "liveCandidates": 0,
  "live": 0,
  "riskBlocked": 1,
  "shadowPnl": 248.20,
  "destinationRealPnl": 0,
  "xauGross": 0,
  "xauNet": 0,
  "mt5Healthy": true,
  "quoteHealthy": false,
  "tradeHealthy": false,
  "realCopyEnabled": false
}
```

`GET http://localhost:5000/api/overview` → **identical body**.

### 3.2 Adjacent live maps (same process)

| Request | HTTP | Result that locks the 2 / 1 / 0 claim |
|---|---|---|
| `GET /api/traders` | 200 | 4 rows; states `SHADOW`, `SHADOW`, `RISK_BLOCKED`, `INSUFFICIENT_DATA` |
| `GET /api/traders?state=SHADOW` | 200 | **2** rows: 10001, 99001 |
| `GET /api/traders?state=RISK_BLOCKED` | 200 | **1** row: 10002 |
| `GET /api/traders?state=LIVE` | 200 | **`[]`** |
| `GET /api/v1/overview` | **404** | A26/A91 path is not mapped |
| `GET /health` | 200 | `{"status":"ok","utc":"2026-08-18T08:20:26.9163369+00:00"}` |
| `GET /api/settings` | 200 | `featureFlags.REAL_COPY_EXECUTION_ENABLED = false` |

Trader filter uses `Enum.TryParse<TraderState>(state, true, …)` (`GetTradersAsync` L113–114) plus `JsonStringEnumConverter` on the host (D74). String `"LIVE"` is a real filter, not a missing-enum no-op.

### 3.3 Per-login ledger (from `/api/traders`)

| Broker | Login | Group | N XAU | Net source P&L | Early score | Risk | Flags | `state` |
|---|---:|---|---:|---:|---:|---:|---|---|
| ACHIEVER | 10001 | `demo\Maxmaster` | 3 | 223.60 | 95.50 | 10 | none | **SHADOW** |
| STARWAVEFX | 99001 | `real\standard` | 3 | 108.2 | 95.50 | 10 | none | **SHADOW** |
| ACHIEVER | 10002 | `demo\yo-2step` | 3 | −2107.0 | 42.50 | 70 | martingale + lotEscalation | **RISK_BLOCKED** |
| ACHIEVER | 10003 | `contest\yo-2step` | 0 | 0 | 40 | 10 | none | **INSUFFICIENT_DATA** |

`mlProbability` is `null` on every row (C44 / B39: ML not built). Per-row `shadowPnl` on the trader DTO is the query **literal 0** (`GetTradersAsync` L106) — that is **not** a contradiction of overview `shadowPnl: 248.20`. Overview sums `shadow_orders.SourceVsShadowSlippage`; the trader card does not.

`lastScored` is `2026-08-18T08:12:17Z` (process seed / first rebuild), **not** this recapture time. The API was **not** re-seeded for E031.

---

## 4. How the three integers are produced

Host map (`apps/api/Program.cs` L54):

```csharp
app.MapGet("/api/overview", (IDashboardQueries q, CancellationToken ct) => q.GetOverviewAsync(ct));
```

Anonymous. No v1 prefix. Return type is the `OverviewDto` record (camelCase).

Query (`EfDashboardQueries.GetOverviewAsync` L18–34):

```csharp
var scores = await _db.TraderScores.ToListAsync(ct);
// …
scores.Count(s => s.CurrentState == TraderState.WATCH),
scores.Count(s => s.CurrentState == TraderState.SHADOW),
scores.Count(s => s.CurrentState == TraderState.LIVE_CANDIDATE),
scores.Count(s => s.CurrentState == TraderState.LIVE),
scores.Count(s => s.CurrentState == TraderState.RISK_BLOCKED),
```

| DTO field | Predicate | Live count |
|---|---|---:|
| `Watch` | `CurrentState == WATCH` | 0 |
| `Shadow` | `CurrentState == SHADOW` | **2** |
| `LiveCandidates` | `CurrentState == LIVE_CANDIDATE` | 0 |
| `Live` | `CurrentState == LIVE` | **0** |
| `RiskBlocked` | `CurrentState == RISK_BLOCKED` | **1** |
| *(not a field)* | `INSUFFICIENT_DATA` / `EARLY_SCORE` / `PAUSED` / `DISQUALIFIED` | 1 leftover (10003) |

Nine §22 states exist on the enum. Overview rollups **five** of them. 2 + 0 + 0 + 0 + 1 = **3**; the fourth score is the leftover.

Seeder (`DemoSeeder.cs` L134–138) rebuilds exactly those four logins after Fake ingest. It does **not** assign `TraderState` literals. `RebuildTraderAsync` persists `BaselineScorer` `SuggestedState`.

`TraderStateMachine.FromBaseline` reachable set (`BaselineScorer.cs` L189–207):

```text
INSUFFICIENT_DATA | RISK_BLOCKED | SHADOW | WATCH | EARLY_SCORE
```

`LIVE` and `LIVE_CANDIDATE` are **not** in that set. `AfterHighEarlyScore() => SHADOW`. `CanPromoteToLive(_) => false` (L211; D97). Persist copies `SuggestedState` blindly — it cannot become LIVE from this machine.

Why these three Fake books land where they do (tape in `FakeMt5BrokerConnector`):

| Login | Tape | Why `FromBaseline` |
|---|---|---|
| 10001 | 3 closed XAU, net +223.60, no martingale | `quality=95.50 >= 70` and `risk=10 < 40` → **SHADOW** |
| 99001 | 3 closed XAU, net +108.2, no martingale | same thresholds → **SHADOW** |
| 10002 | 3 closed XAU, lots 0.10 → 0.20 → 0.40, net −2107, `Martingale=true` | `Martingale && MaxDrawdown > 0 && NetPnl < 0` → **RISK_BLOCKED** (risk 70 is below the `>= 80` shortcut; the martingale+loss clause fires) |
| 10003 | account only, **0** closed XAU | `CompletedXauTrades == 0` → **INSUFFICIENT_DATA** |

That is the entire 2 / 1 / 0 story. It is a **deterministic demo fixture**, not a live desk.

---

## 5. Adjacent fields on the same body (honesty)

These are **not** the assigned 2 / 1 / 0 claim, but they ride the same GET and must not be laundered into a green dashboard.

| Field | Live | Honest? |
|---|---|---|
| `totalAccounts` | 4 | **Yes** for the Fake book (10001, 10002, 10003, 99001) |
| `connectedBrokers` | 2 | **No.** `COUNT(brokers WHERE Enabled)` — Achiever + StarwaveFX catalog rows, not live Manager heartbeats |
| `xauTraders` | 3 | Demo-ok: scores with `CompletedXauTrades > 0` (drops 10003) |
| `tradersWithThreeTrades` | 3 | Demo-ok: same three books have N=3 |
| `shadowPnl` | 248.20 | **Wrong grain.** Σ `SourceVsShadowSlippage` on 6 demo `shadow_orders` (D48). Not A24 net. Trader-row `shadowPnl` is 0 |
| `destinationRealPnl` / `xauGross` / `xauNet` | 0 / 0 / 0 | Constructor literals. Allowed while live send is off **only** with quality metadata — quality field is absent |
| `mt5Healthy` | **true** | **Lie.** `brokers > 0`. Fake connector is not Manager (C42) |
| `quoteHealthy` / `tradeHealthy` | false / false | Honest **today** because seeder/worker stamp `Disconnected` (E008). Wrong type vs A91 five-value health |
| `realCopyEnabled` | false | **Yes** vs A49 default. Literal, not env |

`CREDENTIALS_AND_COPY_STATUS.md` line 34 already states the same sentence this file remeasures: “Demo overview: 2 SHADOW, 1 RISK_BLOCKED, 0 LIVE, `realCopyEnabled=false`.”

---

## 6. What the React page would paint (not re-driven in a browser)

`OverviewPage.tsx` (D77; SHA `6497193F…`) binds:

| Card | Source | This body |
|---|---|---|
| Shadow | `data.shadow` | **2** |
| Risk blocked | `data.riskBlocked` | **1** |
| Live candidates | `data.liveCandidates` | 0 |
| Live copied | **not bound** | wire `live: 0` is **invisible** |
| Footer | `data.realCopyEnabled` | OFF + “Trade #3 never auto-promotes to LIVE.” |

So the **API** has 0 LIVE. The **page** does not display that integer. The footer sentence is the only LIVE-related chrome.

---

## 7. Stale-vs-later

| Claim | Source | This pass |
|---|---|---|
| Demo overview is 2 SHADOW / 1 RISK_BLOCKED / 0 LIVE | CREDENTIALS_AND_COPY_STATUS; D77 §7 (13:43:05) | **Still exact** at 13:50:26 and 13:50:49 |
| `GET /api/v1/overview` exists | A26 / A91 / A63 | **Still 404** |
| Seeder forges FIX `LoggedOn` so health tiles are green | D22 / C13 | **Stale for FIX bits.** Live `quoteHealthy`/`tradeHealthy` = false (E008) |
| `shadowPnl` is 0 | D21 era | **Stale.** Live 248.20 after D48 side-effect |
| Overview page MISSING | A91 §0 / A62 | **Stale.** File exists; `live` still unpainted |
| 0 LIVE means A22 R5-before-R6 is implemented | — | **False.** Vacuous lock (D97 / D12) |

D77’s JSON capture is **byte-identical** to this pass (same 17 keys, same numbers). E031 adds the traders-filter lock and the explicit 2 / 1 / 0 verdict the assignment asked for.

---

## 8. Non-goals / do not

- Do **not** treat 2 / 1 / 0 as “we have a live desk with two shadow traders.”
- Do **not** treat `live: 0` as a §68 / §70 / A101 pass.
- Do **not** invent a fifth LIVE demo row to make the tile look busy.
- Do **not** hand-write MQ5 or mutate product source from this file.
- Do **not** claim `/api/v1/overview` or the A91 envelope from a 200 on `/api/overview`.
- When a coding wave touches Overview: keep the no-auto-promote rule; replace the flat DTO; paint `live` as its own tile; keep `realCopyEnabled` false until gates pass.

---

## 9. Honesty

- **True:** running demo API, this minute, reports **2 SHADOW, 1 RISK_BLOCKED, 0 LIVE**.
- **True:** `/api/traders` names the two SHADOW logins (10001, 99001) and the one RISK_BLOCKED login (10002). `?state=LIVE` is empty.
- **True:** `realCopyEnabled=false`. No `NewOrderSingle` sender (E002).
- **Not true if implied:** these are live MT5 accounts, live FIX sessions, or A91 Overview.
- **Not true if implied:** the leftover 10003 is counted in the three rollups. It is `INSUFFICIENT_DATA`.
- **Not true if implied:** the Overview page shows Live = 0. It drops the field.
- Product source was **not** modified.
