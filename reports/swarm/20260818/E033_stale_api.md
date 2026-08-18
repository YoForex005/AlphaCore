# E033 — Stale API process vs `quoteHealthy: true` (restart still needed)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\E033_stale_api.md` |
| Agent | E033 (stale Kestrel / InMemory / `quoteHealthy` pin) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:55+05:30 (process/DLL census) / 2026-08-18T08:21:31Z (`/health` utc) / 2026-08-18T13:53:34+05:30 (reconfirm overview) |
| Assigned | Old API process still reports `quoteHealthy` **true**. Restart needed. Write this file. **Do not modify product source.** |
| Workspace | `D:\Prop` (API host `apps/api`; Vite is **not** under `D:\Prop\src`) |
| Product source modified | **No.** This report (plus a `SWARM_LOG.md` catalog line) is the only write. |
| Test source modified | **No.** |
| Config / `.env` / `appsettings` edited | **No.** |
| Process killed / restarted | **No.** Did not recycle pid **54468** / parent **53816**. |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback…`) |
| Binding law | Architecture §§25–26, 41, 47, 62, 70; A25 / A72 / A77; A91 health strip; C43 (Logon **NOT PROVEN**); E008 (current seeder/worker stamp `Disconnected`) |
| Honesty siblings | D22 (mid-wave seeder **forged** `LoggedOn` / `ReadyForMarketData`); D32 / D94 / E008 (writers now `Disconnected`); D77 / E016 / E031 (live overview `quoteHealthy: false` on **this** pid); E012 (same pid on `:5000`); E026 (`/api/health` hardcoded) |
| Method | `Get-NetTCPConnection` `:5000`; `Win32_Process` command line + `CreationDate`; `Get-Process` modules; SHA-256 + LastWrite of loaded vs `src/Infrastructure/bin` DLLs; live GET `/health` `/api/health` `/api/overview` `/api/fix/sessions` `/ready`; full read of `DemoSeeder` FIX block, `EfDashboardQueries.GetOverviewAsync` L40, `DependencyInjection` InMemory branch, `Program.cs` seed-on-boot. Product source **not** edited. Process **not** recycled. |

**Honesty rule:** a live `quoteHealthy: true` is **not** a QUOTE Logon. A live `quoteHealthy: false` is **not** a measured TCP drop. An InMemory row written at process start cannot change when `DemoSeeder.cs` is edited on disk. A newer `Infrastructure.dll` on disk is **not** the DLL inside a process that already started. Do not greenwash a recycle that did not happen. Do not invent a `true` this minute to match the assignment if the wire says `false`.

---

## 0. Verdict

**Restart is still required. The assigned sentence “still reports `quoteHealthy: true`” is *not* the current HTTP body.**

| Assigned claim | Measured this pass |
|---|---|
| Old API process is still bound on `:5000` | **Yes.** pid **54468** `TraderIntelligence.Api.exe --urls http://127.0.0.1:5000`, parent **53816** `dotnet run … --no-launch-profile`, start **2026-08-18T13:42:16+05:30** / **08:12:16Z** |
| That process still reports `quoteHealthy: true` | **No.** Live `GET /api/overview` → **`quoteHealthy: false`**, `tradeHealthy: false`, QUOTE+TRADE **`Disconnected`** |
| Restart needed | **Yes — for binary/store freshness**, not to flip this bit. Loaded `Infrastructure.dll` is **8 min older** than `src/Infrastructure/bin` and the InMemory catalog is **frozen at boot**. |

This process **already** went through one recycle at 13:42:16, **after** the honest seeder write (13:34:59). That is why `quoteHealthy` is `false` now. It is **not** a D22-era Kestrel that still holds `ReadyForMarketData`. Treat any swarm note that says “the API on `:5000` still paints QUOTE green” as **stale against this pid**.

The process is **still stale** in the only sense that matters for the next edit:

```text
disk source / latest compile  ≠  bytes mapped in pid 54468
InMemory "trader-intelligence"   =  first DemoSeeder of THIS process
fix-worker                       =  not running; cannot overwrite these rows
```

Classification:

| Slice | Class |
|---|---|
| Assigned `quoteHealthy: true` as a **current** wire fact | **STALE CLAIM** — false on pid 54468 (also D77 13:43:05, E016 13:49:31, E031 13:50:26) |
| Pid 54468 vs latest `src/Infrastructure/bin` | **STALE PROCESS** — loaded DLL `EB43953E…` @ 13:40:18; src bin `63C78E11…` @ 13:48:16 |
| InMemory store | **BOOT SNAPSHOT** — seeder `Brokers.Any` no-op after first write; no shared Postgres |
| Mid-wave D22 `LoggedOn` / `ReadyForMarketData` seed | **REMOVED from source**; would have stayed `true` **until recycle** if that pid were still up |
| Overview `quoteHealthy` contract (enum ∈ `{LoggedOn, Ready*}`) | **LATENT LIE** — green again the moment any writer puts those enums back |
| Live QUOTE TLS / `35=A` | **NOT PROVEN** (`C43`) |
| `mt5Healthy: true` on the same body | **CURRENT LIE** (`brokers.Enabled > 0`; Fake connector) |
| Dest book `2399.45` / `2399.85` | **FORGED** + **AGING** (`quoteAgeSeconds` ~554 at 08:21:31Z; seed clock = process start) |
| Product source this pass | **unchanged** |

Do **not** tick A101 item 1 from `quoteHealthy: false`. Do **not** treat a recycle as a FIX pass. Do **not** edit product source to “fix” a bit that is already false on the wire.

---

## 1. Direct answers

**Does the old API process still report `quoteHealthy: true`?**  
**No — not this minute, not this pid.** `GET http://127.0.0.1:5000/api/overview` returns `"quoteHealthy":false`. Same value on the 13:43:05 (D77), 13:49:31 (E016), and 13:50:26 (E031) captures against the **same** process.

**Is a restart still needed?**  
**Yes.** Pid 54468 started 13:42:16 and still maps `D:\Prop\apps\api\bin\Debug\net8.0\TraderIntelligence.Infrastructure.dll` written **13:40:18** (SHA-256 `EB43953E68EB4F87ABF9CC2D72900F7BA32887900F3B10249C4559D7A6E4EF4F`). `src/Infrastructure/bin/Debug/net8.0/TraderIntelligence.Infrastructure.dll` was rebuilt **13:48:16** (SHA-256 `63C78E11502F68A7F15C73FC9D8CAD366AFAE14EE4DEEF4BAA132F84DE12C196`). The host will not load that file until `dotnet run` is recycled. InMemory will not re-seed until recycle (and even then only if `Brokers` is empty — a new process is a new InMemory name-scope).

**Did E033 recycle it?**  
**No.**

---

## 2. Live process (this host, this check)

### 2.1 Bind

| Probe | Result |
|---|---|
| TCP listen `:5000` | **Yes** — `127.0.0.1:5000` Listen, pid **54468** |
| TCP listen `:5160` / `:7294` | **No** |
| Image | `D:\Prop\apps\api\bin\Debug\net8.0\TraderIntelligence.Api.exe` |
| Command line | `"D:\Prop\apps\api\bin\Debug\net8.0\TraderIntelligence.Api.exe" --urls http://127.0.0.1:5000` |
| Parent | pid **53816** `"C:\Program Files\dotnet\dotnet.exe" run --project D:\Prop\apps\api\TraderIntelligence.Api.csproj --urls http://127.0.0.1:5000 --no-launch-profile` |
| Parent start | 2026-08-18 **13:42:15** +05:30 |
| Child start | 2026-08-18 **13:42:16** +05:30 = **08:12:16Z** |
| Working set (13:50:55) | ~128 MB |
| `ProductVersion` | `1.0.0+398a14200ec65714c4077eed55c46808382ca1e3` (informational; worktree is dirty) |
| Other `TraderIntelligence.*` hosts | **None.** No `FixWorker`. No `Mt5Worker`. |

Vite `:3000` pid 49100 is up (E012). It is **not** the API and cannot change `quoteHealthy`.

### 2.2 Loaded product modules vs disk

| Module (mapped in 54468) | Path | Bytes | LastWrite +05:30 | SHA-256 |
|---|---|---:|---|---|
| `TraderIntelligence.Api.exe` | `apps/api/bin/Debug/net8.0\` | 152064 | 13:40:38 | `0701EC855091EF94611CB85AD8310A9A4EC21BD44DD2EB462AF50203E9D6F46B` |
| `TraderIntelligence.Api.dll` | same | 42496 | 13:40:38 | `B346B3B7143779C648EBEB60325A7C0E3F7491BCBDEA89255DBC661C65F9ED75` |
| **`TraderIntelligence.Infrastructure.dll` (LOADED)** | same | 86016 | **13:40:18** | **`EB43953E68EB4F87ABF9CC2D72900F7BA32887900F3B10249C4559D7A6E4EF4F`** |
| `TraderIntelligence.Application.dll` | same | 64000 | 13:40:17 | `49CE66608FEA23EB5A29458D02E73375EB2477BE1DE3880E55C8B4114A7C9DEE` |
| `TraderIntelligence.Domain.dll` | same | 118784 | 13:40:17 | `38DB367DF0B0080D54881692CBF996226045FE5B5A658C26A284B2E88E031807` |
| **`Infrastructure.dll` (NOT loaded)** | `src/Infrastructure/bin/Debug/net8.0\` | 86016 | **13:48:16** | **`63C78E11502F68A7F15C73FC9D8CAD366AFAE14EE4DEEF4BAA132F84DE12C196`** |

`Application.dll` / `Domain.dll` hashes match between `apps/api/bin` and `src/*/bin`. **Only Infrastructure drifted after boot.** A later compile (13:48:16, +6.0 min after start) did **not** replace the mapped image. That is the restart trigger.

---

## 3. Live HTTP (same pid)

`GET http://127.0.0.1:5000/health` → **200** `{"status":"ok","utc":"2026-08-18T08:21:31.1971016+00:00"}` `Server: Kestrel`.

### 3.1 `GET /api/overview` — assigned bit

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

Reconfirm ~13:53:34 +05:30: **identical 17 keys**, `quoteHealthy=False`.

Same object already captured by D77 (13:43:05, 49 s after this pid started), E016, E031. This is **not** a mid-request flip.

### 3.2 `GET /api/fix/sessions` — why the bit is false

| Qualifier | `status` | `connected` | `loggedOn` | `executionEnabled` | `lastError` | `lastInbound` |
|---|---|---|---|---|---|---|
| QUOTE | `Disconnected` | false | false | false | `No live QUOTE socket. Demo seed only.` | `2026-08-18T08:12:16.7895326+00:00` |
| TRADE | `Disconnected` | false | false | false | `No live TRADE socket. NewOrderSingle off.` | `2026-08-18T08:12:16.7895326+00:00` |

`lastInbound` **equals process start** (08:12:16Z). That is the seeder clock, not a Heartbeat. Bid/ask **2399.45 / 2399.85**, `instrumentId: null`, `quoteAgeSeconds` **554.5** at 08:21:31Z and still growing. The FIX **page** can look “quoted” while Overview `quoteHealthy` is false. Do not confuse those two surfaces.

### 3.3 Adjacent maps (do not launder)

| Request | HTTP | Load-bearing |
|---|---|---|
| `GET /api/health` | 200 | Hardcoded: Achiever `healthy: true` + “demo FakeMt5BrokerConnector — not live Manager”; QUOTE `healthy: false` + “no live TLS socket”. **Does not read** `FixSessionStates`. |
| `GET /ready` | 200 | `{ ready: true, brokers: 2 }` — InMemory count, not A77 Postgres. |
| `GET /api/v1/overview` | 404 | Catalog path still unmapped (E031). |

`/api/health` QUOTE `healthy: false` and Overview `quoteHealthy: false` **agree today**. They are **different writers**. A future seeder `LoggedOn` would green Overview and leave `/api/health` red.

---

## 4. Why `quoteHealthy` is a boot-time snapshot

### 4.1 Query (current source; SHA `328D0924…`)

```40:42:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            quote?.Status is FixSessionStatus.LoggedOn or FixSessionStatus.ReadyForMarketData or FixSessionStatus.ReadyForExecution,
            trade?.Status is FixSessionStatus.LoggedOn or FixSessionStatus.Reconciling or FixSessionStatus.ReadyForExecution,
            false);
```

`QuoteHealthy` is **not** “last quote age < 3 s”. It is **not** a TLS session object. It is an enum membership test. `Disconnected` → **false**. `LoggedOn` / `ReadyForMarketData` / `ReadyForExecution` → **true**.

### 4.2 Seed (current source; SHA `A6416491…`)

`DemoSeeder` L68–103 writes **both** rows `FixSessionStatus.Disconnected` with LastError text that admits no socket (E008). LastWrite **13:34:59** — **7 min before** this pid started. This process therefore seeded the **honest** enum. That is why the wire is false.

Guard at L23–24:

```csharp
if (await db.Brokers.AnyAsync(ct))
    return;
```

First writer wins. Editing `DemoSeeder.cs` **after** boot is a no-op for this pid.

### 4.3 Store is process-private InMemory

`DependencyInjection.cs` L19–24:

```csharp
var connection = configuration.GetConnectionString("TraderIntelligence")
                 ?? configuration["DATABASE_URL"];
if (string.IsNullOrWhiteSpace(connection) || connection.Contains("<SECRET>", StringComparison.Ordinal))
    services.AddDbContext<TraderDbContext>(o => o.UseInMemoryDatabase("trader-intelligence"));
```

`apps/api/appsettings.json` names `ConnectionStrings:Postgres`, **not** `TraderIntelligence`. `DATABASE_URL` is unset on this `dotnet run`. Result: **InMemory** name `"trader-intelligence"` **inside pid 54468 only**.

Consequences:

1. A later `fix-worker` (none running) would have its **own** InMemory and could **not** smash these rows to `Disconnected`.
2. A later source edit cannot mutate the mapped catalog.
3. Recycle is the only way to apply a new seeder **or** a new `Infrastructure.dll`.
4. If this were real Postgres with D22-era `LoggedOn` rows still in `fix_sessions`, recycle **alone** would **not** flip the bit (`Brokers.Any` → seeder returns). That is **not** today’s topology, but it is why “just restart” is not a universal un-lie.

### 4.4 What a *D22-era* leftover process would have shown

D22 hashed seeder `139D8F87…` / 4942 B: QUOTE `ReadyForMarketData`, TRADE `LoggedOn`. Against the **same** `GetOverviewAsync` predicate that is still on disk:

| Bit | D22-era InMemory | Pid 54468 (this pass) |
|---|---|---|
| `quoteHealthy` | **true** (no socket) | **false** (no socket) |
| `tradeHealthy` | **true** (no socket) | **false** (no socket) |
| Live `35=A` | **none** | **none** |

That is the scenario the assignment names. **That pid is gone.** The 13:42:16 recycle already applied the E008 seeder. Anyone still citing “API says QUOTE healthy” is reading **D22 / C13 / A101 mid-wave**, a **browser tab from before 13:42**, or **`mt5Healthy: true`** on the current body (different key).

---

## 5. Timeline (do not mix epochs)

| Clock +05:30 | Event | `quoteHealthy` on `:5000` |
|---|---|---|
| ~mid-wave (D22 / C07 / C13) | Seeder SHA `139D8F87…` forges QUOTE `ReadyForMarketData` + TRADE `LoggedOn`. Any API that booted then would paint green. | **true** (forged) |
| 13:34:59 | `DemoSeeder.cs` LastWrite — current honest body `A6416491…` | n/a (disk only) |
| 13:35:15 | `EfDashboardQueries.cs` / `Program.cs` LastWrite (predicate unchanged) | n/a |
| 13:40:18 | `apps/api/bin` `Infrastructure.dll` `EB43953E…` | n/a |
| 13:40:38 | `TraderIntelligence.Api.exe` / `.dll` | n/a |
| **13:42:15–16** | **This** `dotnet run` + child pid **54468** seeds InMemory `Disconnected` | **false** from first request |
| 13:43:05 | D77 live capture | false |
| 13:48:09 | E012 same pid, `/health` 200 | (overview not re-litigated) |
| **13:48:16** | `src/Infrastructure/bin` rebuild `63C78E11…` — **not loaded** | still false (old image) |
| 13:49:31 | E016 overview | false |
| 13:50:26 | E031 overview | false |
| 13:50:55–13:53:34 | **this file** | **false** |

---

## 6. Source hashes (worktree; not edited)

| Path | Bytes | SHA-256 | git | Role |
|---|---:|---|---|---|
| `src/Infrastructure/Seeding/DemoSeeder.cs` | 5082 | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | untracked blob `d65f09fa…` | FIX rows `Disconnected` |
| `src/Infrastructure/Dashboard/EfDashboardQueries.cs` | 8708 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | untracked blob `d9bed4fc…` | `QuoteHealthy` enum test |
| `src/Infrastructure/DependencyInjection.cs` | 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | untracked blob `5d65dbc8…` | InMemory if no `TraderIntelligence` CS |
| `apps/api/Program.cs` | 4731 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | dirty blob `9d623e1d…` | seed-on-boot; `/api/overview` map |
| `src/Application/Dashboard/DashboardModels.cs` | 3088 | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` | dirty | `OverviewDto.QuoteHealthy` |
| `apps/web/src/pages/OverviewPage.tsx` | 2078 | `6497193F190445CCF76AED218E01E3BC85050238CB89D06002837FC9C502825C` | untracked | paints `Q` / `-` from the bool |
| `apps/web/src/api/hooks.ts` | 1935 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | — | `useOverview` has **no** `refetchInterval` (stale React cache possible) |

`useOverview` does **not** poll. `useFixSessions` / `useRiskStatus` poll every 5 s. A browser tab that fetched overview **before** 13:42 and was never remounted can still show the **previous** pid’s body, including a forged `true`, until the operator hard-refreshes. That is a **client cache** lie, not the current Kestrel. E033 did not drive a browser.

---

## 7. What “restart needed” means (ops, not a coding task)

When a later wave is **authorized** to touch the host (not this agent):

1. Stop pid **53816** (that tears down 54468). Do not leave a second `dotnet run` fighting `:5000`.
2. Rebuild so `apps/api/bin/Debug/net8.0/TraderIntelligence.Infrastructure.dll` **equals** `src/Infrastructure/bin` (`63C78E11…` or whatever the tree is **then**).
3. Start `dotnet run --project apps/api/TraderIntelligence.Api.csproj --urls http://127.0.0.1:5000 --no-launch-profile` again.
4. Re-GET `/api/overview` and `/api/fix/sessions`. Expect `quoteHealthy: false` + `Disconnected` **from the current seeder**. If you see `true`, the loaded seeder or the row writer has regressed — that is a **new** forge, not “stale process.”
5. Hard-refresh the Vite tab (`:3000`) so `useOverview` drops any pre-13:42 cache.
6. Do **not** treat post-recycle `false` as Logon proof. Do **not** set `REAL_COPY_EXECUTION_ENABLED=true`.

If Postgres is ever bound (`ConnectionStrings:TraderIntelligence` without `<SECRET>`), recycle is **insufficient**: leftover `LoggedOn` rows survive `Brokers.Any`. Then the **worker** (or a one-shot SQL/status rewrite) is the writer — and today’s worker is **not running**.

---

## 8. Adjacent lies that a recycle will **not** fix

These are **source** defects. Restarting pid 54468 re-seeds the same demo.

| Surface | After a clean recycle of current source | Why |
|---|---|---|
| `quoteHealthy` | **false** | honest `Disconnected` seed |
| `tradeHealthy` | **false** | same |
| `mt5Healthy` | **true** | `brokers > 0` (Fake) |
| `/api/health` Achiever | **true** | hardcoded in `Program.cs` |
| Dest bid/ask 2399.45 / 2399.85 | **back**, age reset to ~0 | seeder still invents the book |
| Live host / `SenderCompId` on FIX cards | **still live identifiers** | seeder literals |
| `realCopyEnabled` | **false** | constructor literal |
| `/api/v1/overview` | **404** | unmapped |
| SignalR `/hubs/dashboard` | **404** | D50 |
| Live QUOTE TLS | **still absent** | no initiator |

Recycle fixes **staleness**. It does not fix **honesty of the dashboard contract**.

---

## 9. Stale-vs-later (use this file for the process; not for FIX)

| Claim | Source | This pass |
|---|---|---|
| Live `:5000` `quoteHealthy` is **true** | assignment text; D22-era implication | **STALE as HTTP.** False on pid 54468 since 13:42:16 |
| D22 seeder still writes `LoggedOn` | D22 / C13 / D07 sentence | **STALE as source.** E008 / current `A6416491…` |
| Current process matches latest Infrastructure compile | — | **FALSE.** Hash drift 13:40:18 vs 13:48:16 |
| `quoteHealthy: false` means QUOTE is measured down | — | **FALSE.** Enum + seed clock; no TCP probe |
| `mt5Healthy: true` is Manager | same overview body | **FALSE** (C42) |
| E012 pid 54468 | E012 | **SAME PROCESS** |
| D76 empty-all-zero overview | D76 | **Different capture** (empty store / other moment). Not this pid’s seeded book |

---

## 10. Honest limits

- Did not start or stop pid 54468 / 53816.
- Did not start Compose, IIS Express, `fix-worker`, or `mt5-worker`.
- Did not attach a debugger or dump the InMemory graph beyond HTTP.
- Did not decompile the two `Infrastructure.dll` hashes instruction-by-instruction. Drift is proven by SHA-256 + LastWrite; content delta of the 13:48:16 rebuild is **not** attributed beyond “not the mapped image.”
- Did not drive Chromium; React-cache scenario is inferred from `useOverview` having no `refetchInterval`.
- Did not print `.env` secrets.
- Product source was **not** modified.

---

## 11. One-line scorecard

| Question | Answer |
|---|---|
| Live `quoteHealthy` right now? | **`false`** (pid 54468, InMemory, honest `Disconnected` seed) |
| Assigned “still `true`”? | **Not on this process.** True only of a **pre-13:42** Kestrel (D22 seed) or a **stale browser cache** |
| Restart needed? | **Yes** — load `Infrastructure.dll` `63C78E11…` (or newer) and reset InMemory |
| Restart done by E033? | **No** |
| Live QUOTE? | **No** |
| Product source edited? | **No** |
