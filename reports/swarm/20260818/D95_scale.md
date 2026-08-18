# D95 — Scale: this tree is **not** 5,000 accounts

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D95_scale.md` |
| Agent | D95 (senior engineer, §69.3 scale census, read-only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:44:43+05:30 |
| Assigned | **Not 5000 accounts.** Write this file. Do not modify product source. |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Law | Architecture v2 **§3** (`5,000+ MT5 trader accounts`), **§13** (Postgres outbox at ~5k; no Kafka), **§69.3** (`Synchronize ~5,000 accounts`) |
| Sibling specs | A57 item 3, A79 §7.3 `SeedFiveThousandAccounts`, A98 planning envelope, A27 `Mt5BackfillRestartTests`, C13/D41 §69.3 **FAIL** |
| Method | Re-read architecture §3 / §13 / §69.3. Count every `Mt5AccountDto` in `DemoBrokerFactory.CreateDefault`. Trace ingest (`GetAccountsAsync` → `UpsertAccountAsync`), score loops (hard-coded 4 logins), dashboard `CountAsync` / `Take`. Grep product `*.cs` for `SeedFiveThousandAccounts`, `AccountCount = 5000`, paging. SHA-256 via `Get-FileHash`. Prefer false negatives over fake PASS. |

**Assigned answer:** **Not 5,000 accounts. Measured: 4 canned logins (0.08% of the §69.3 bar).** Achiever 10001 / 10002 / 10003 + StarwaveFX 99001. There is no 5k generator, no paged Manager walk, no checkpointed census, no `Mt5BackfillRestartTests`. README line 22 and architecture §3 describe a **goal**, not this worktree.

**One-line:** Demo tape = **4** accounts / **4** groups / **18** deals. §69.3 remains **FAIL**.

---

## 0. Verdict (honest)

| Question | Measured answer | Class |
|---|---|---|
| Are there ~5,000 `mt5_accounts` in any product path? | **No. 4.** | **FAIL** vs §69.3 |
| Does `DemoBrokerFactory` emit 5k? | **No.** 3 Achiever + 1 StarwaveFX | demo fixture |
| Does A79 `SeedFiveThousandAccounts` / `InMemoryMt5BrokerConnector` exist? | **No.** 0 hits under `src/`, `apps/`, `tests/` | **MISSING** |
| Does ingest cap at 4, or only the Fake? | Fake returns 4; `SyncBrokerAsync` would persist whatever `GetAccountsAsync(null)` returns | no 5k source |
| Does scoring walk ingested accounts? | **No.** Seeder / worker / `/api/ops/resync` hard-code the same 4 logins | even a 5k Fake would score 4 |
| Is `SyncCheckpoint` written? | **No** (D46) | restart-safe 5k **MISSING** |
| Is there paging / batch upsert / measured 5k timing? | **No.** One `SaveChanges` per account. `Skip(`/`pageSize` **0** on accounts | **UNSAFE** as a 5k write path |
| Do dashboard queries survive 5k? | **No.** Full-table loads; leaderboard in-memory (C36 / D21) | **UNSAFE** as a 5k read path |
| Do tests lock N=5000 or even N=4? | **No.** `SeedingAndStoreTests` asserts brokers=2, groups>2, deals>0. **0** `Mt5Accounts.Count` facts | unproven |
| Does C++ `GetUserLogins` feed C#? | **No.** Only `IMt5BrokerConnector` implementor is the Fake | C# cannot enumerate live logins |
| Is Kafka required at 5k? | Architecture §13: **No.** Irrelevant today; N=4 | law, not evidence of scale |
| Does Overview / Brokers show 5k? | After seed they show **4** and **3+1** | honest count of a toy census |

Do **not** treat:

- README “Identify copyable XAUUSD traders from ~5,000 MT5 accounts”
- Architecture §3 / §69.3
- A79’s unbuilt 5k fake
- A98’s planning envelope (~5k–8k rows)
- API listen port **:5000**
- `CTraderFixOptions.MaxQuoteAgeMs = 5000`
- login **10002** `Balance = 5_000`
- eval-fixture volumes of `5000` (0.50 lot on scale 10 000)

as a measured 5,000-account census.

4 / 5,000 = **0.08%**. Shortfall = **4,996** source logins (planning floor; live brokers may be more).

---

## 1. Files hashed (this pass)

| Bytes | Lines | SHA-256 | Path |
|---:|---:|---|---|
| 7049 | 145 | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` | `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` |
| 5082 | 129 | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` |
| 4535 | 92 | `2637D97B563798934DAAD374A0DE5F28046F7AD7F4009A59E64B3686166BC7E3` | `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` |
| 12097 | 310 | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` | `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` |
| 8708 | 182 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` |
| 5951 | 151 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` |
| 639 | 17 | `B13CB025741FB7DDF290B67070727C9FAFC0FDF071572FCD1DB7CCADDB6DA549` | `D:\Prop\src\Domain\Entities\Mt5Account.cs` |
| 391 | 11 | `15FF40719E5FE3ADBA8B2F0E6D7215C02D2B813EC84A1E092EC1D5BE9CB83056` | `D:\Prop\src\Domain\Entities\SyncCheckpoint.cs` |
| 1882 | 40 | `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` | `D:\Prop\apps\mt5-worker\Worker.cs` |
| 4731 | 86 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | `D:\Prop\apps\api\Program.cs` |
| 3119 | 58 | `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` | `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` |
| 3088 | 104 | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` | `D:\Prop\src\Application\Dashboard\DashboardModels.cs` |
| 1746 | 33 | `C023B4227A1C511F346A1FD96B6648B901001551731D1472FE7A33468B1A2764` | `D:\Prop\README.md` |
| 50966 | 2116 | `0B3C0EDC09081C25D097FF0E6AADC7A638562EBB8DB345DC325DC54EC904D37E` | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |

Fake SHA matches D24 / C10 / C42. Seeder / ingest / store / checkpoint hashes match D46. README SHA matches C45. `TraderDbContext` SHA matches D19 / D51 / D56. Product files were **not** rewritten for this report.

Grep of `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests` `*.cs` (exclude `bin`/`obj`):

| Token | Hits |
|---|---|
| `SeedFiveThousandAccounts` | **0** |
| `InMemoryMt5BrokerConnector` | **0** |
| `AccountCount == 5000` / `AccountCount = 5000` | **0** |
| `Mt5BackfillRestartTests` | **0** |
| `new Mt5AccountDto(` | **4** (all in `DemoBrokerFactory.CreateDefault`) |
| `new long[] { 10001, 10002, 10003, 99001 }` | **3** (seeder, worker, API resync) |
| `Skip(` / `pageSize` / `PageSize` on accounts | **0** |
| `Take(` in `src/` | **2** — FIX checksum slice; risk rejects `Take(20)`. **Not** account paging |

---

## 2. Law (goal, not inventory)

Architecture v2 opening line and §3 set the business envelope:

```5:5:D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
**Primary use case:** Identify high-quality XAUUSD traders from ~5,000+ MT5 accounts, shadow-copy them, and route approved real trades to a cTrader/cServer FIX 4.4 execution account.
```

```129:134:D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
# 3. Primary Business Goal

We have roughly:

5,000+ MT5 trader accounts
```

§69 item 3 is the first-useful-version gate (accepted only when **all 12** are true):

```2639:2642:D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md
1. Connect to both MT5 brokers.
2. Discover all groups.
3. Synchronize ~5,000 accounts.
```

§13 says ~5k users do **not** justify Kafka. That is a **non-goal**, not a count of rows in this repo.

A98 planning bounds (not measured production): `mt5_accounts` ~5,000–8,000; deals conservative 0.4M / upper 2.5M; reconstructed ~0.1M–0.8M. Cite those as **design**, never as “we have N deals.”

A79 required a **test-only** `InMemoryMt5BrokerConnector.SeedFiveThousandAccounts` with `AccountCount == 5000`, distinct logins, skew across groups including unmapped, seed < 200 ms. That type is **not on disk**. D24: Fake is the stand-in, in the wrong place, with a 4-login tape.

---

## 3. Measured demo census (source of every dashboard number)

`DemoBrokerFactory.CreateDefault` (`FakeMt5BrokerConnector.cs` L95–127) is the **only** account catalog DI registers (`DependencyInjection.cs` L31–34).

| Login | Broker | Group | Leverage | Balance | Deals (IN+OUT) | Completed XAU | Intended score |
|---:|---|---|---:|---:|---:|---:|---|
| 10001 | ACHIEVER | `demo\Maxmaster` | 100 | 10,000 | 6 | 3 | SHADOW |
| 10002 | ACHIEVER | `demo\yo-2step` | 100 | **5,000** (equity, not N) | 6 | 3 | RISK_BLOCKED (martingale) |
| 10003 | ACHIEVER | `contest\yo-2step` | 200 | 25,000 | **0** | 0 | INSUFFICIENT_DATA |
| 99001 | STARWAVEFX | `real\standard` | 100 | 8,000 | 6 | 3 | SHADOW |

```107:124:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
            accounts: new[]
            {
                new Mt5AccountDto(10001, @"demo\Maxmaster", 100, 10_000, 10_240, 200, 9_800, 240),
                new Mt5AccountDto(10002, @"demo\yo-2step", 100, 5_000, 4_820, 150, 4_670, -180),
                new Mt5AccountDto(10003, @"contest\yo-2step", 200, 25_000, 25_000, 0, 25_000, 0)
            },
            deals: BuildAchieverDeals(t0));
        // ...
            accounts: new[]
            {
                new Mt5AccountDto(99001, @"real\standard", 100, 8_000, 8_110, 80, 7_920, 110)
            },
```

Positions argument omitted → empty list. `AddDeal` exists; nothing in product calls it after construction.

| Set | Demo N | §69.3 / A98 planning | Ratio |
|---|---:|---:|---:|
| Brokers | 2 | 2 | OK as count, **FAIL** as live connect |
| Groups | 4 | tens–hundreds (all Manager-visible) | fixture names |
| **Accounts** | **4** | **~5,000–8,000** | **0.08%** |
| Deals | 18 | 0.4M–2.5M | canned XAUUSD only |
| Open positions | 0 | hundreds–thousands | unused |
| Trader scores | 4 | ~5,000 | same 4 logins |
| Completed reconstructed XAU | 9 | 0.1M–0.8M | 3+3+0+3 |
| Shadow orders (D48 eval) | 6 | grows with shadow fills | 10001×3 + 99001×3 |
| Sync checkpoints | 0 | 15k–40k (5k × 3–5 streams) | entity unused |

`DemoSeeder` does not insert accounts by hand. It seeds 2 brokers + 1 instrument + 2 FIX rows + 1 invented quote + 1 kill switch, then:

```126:138:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
        var (achiever, starwave) = DemoBrokerFactory.CreateDefault();
        var registry = new BrokerRegistry(new IMt5BrokerConnector[] { achiever, starwave });
        var ingestion = new DealIngestionService(registry, store);
        // ...
        await ingestion.SyncBrokerAsync(BrokerCodes.Achiever, from, to, ct);
        await ingestion.SyncBrokerAsync(BrokerCodes.StarwaveFx, from, to, ct);

        foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
        {
            var code = login >= 99000 ? BrokerCodes.StarwaveFx : BrokerCodes.Achiever;
            await scoring.RebuildTraderAsync(code, login, ct);
        }
```

Second `SeedAsync` early-returns on `Brokers.Any`. D48 measured post-seed account population stays **4**.

---

## 4. Ingest would scale with the Fake — the Fake does not

```43:57:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var accounts = await connector.GetAccountsAsync(null, ct);
        var insertedDeals = 0;
        foreach (var account in accounts)
        {
            await _store.UpsertAccountAsync(brokerId, account, now, ct);
            var deals = await connector.GetDealsAsync(account.Login, from, to, ct);
            foreach (var deal in deals)
            {
                if (await _store.UpsertDealAsync(brokerId, deal, now, ct))
                    insertedDeals++;
            }

            var positions = await connector.GetPositionsAsync(account.Login, ct);
            await _store.ReplacePositionsAsync(brokerId, account.Login, positions, ct);
        }
```

`GetAccountsAsync(null)` returns the **entire in-memory list**. There is no cursor, no `fromLogin`, no group page, no `max_items`. `IMt5BrokerConnector` has no `GetUserLogins` / `GetAccountCount`.

`UpsertAccountAsync` does `SingleOrDefault` + **one `SaveChanges` per login** (`EfTradingStore.cs` L53–83). At 5k that is 5k round-trips plus 5k deal/position loops. A57 required batch upserts + bound Manager-pool concurrency **before claiming 5k**. Not written.

Identity shape is correct: unique `(BrokerId, Login)` on `mt5_accounts` (`TraderDbContext.cs` L51–56). Uniqueness without a census is not §69.3.

`SyncCheckpoint` is mapped and never constructed (D46). Worker uses host-clock `[UtcNow-30d, UtcNow+1m]` every 30 s — not a restart cursor, and it does **not** walk a 5k login list.

C++ `IMT5Client::GetUserLogins(group, logins)` / `GetAccount` exist (`mt5-sdk/src/core/imt5_client.h` L38–41). C# has **zero** P/Invoke / C++/CLI / HTTP adapter calling them. D24 / C42: Fake `ConnectAsync` flips `_connected = true`. Live Achiever / StarwaveFX account walk is **not** in this process.

---

## 5. Scoring is independently capped at 4

Even if someone stuffed 5,000 `Mt5AccountDto`s into the Fake tomorrow, these three loops would still rebuild **only** the canned quartet:

| Site | Lines | List |
|---|---|---|
| `DemoSeeder.SeedAsync` | 134–138 | `{ 10001, 10002, 10003, 99001 }` |
| `apps/mt5-worker/Worker.cs` | 31–35 | same |
| `apps/api/Program.cs` `/api/ops/resync` | 79–80 | same |

There is no `foreach (var a in db.Mt5Accounts)`. Leaderboard size is therefore **≤ 4** after a demo run, regardless of ingest width.

---

## 6. Read path is a 4-row accident, not a 5k plane

`GetOverviewAsync` L16: `Mt5Accounts.CountAsync` — honest. After seed this is **4**, not 5,000. React `OverviewPage` metric **“MT5 accounts”** binds `data.totalAccounts` (ASP.NET camelCase of `OverviewDto.TotalAccounts`).

`GetBrokersAsync` N+1 `CountAsync` per broker with **`Connected = true` literal** (L53). Brokers page shows `accountCount` → **3** and **1**. The emerald “connected” cell is not Manager attach (D24 / D41).

`GetTradersAsync` loads **all** scores, **all** accounts, **all** completed-trade PnL groups, then filters/sorts in process (L76–116). `GetTraderAsync` reloads that whole leaderboard. `GetGroupsAsync` N+1 account counts. Only `Take` on a product query is risk rejects **20** and `/api/trades` **200**. Account list is unpaged.

C36 still holds as a 5k judgement: demo “feels fast” because N=4 hides every seq-scan. A98 indexes are **intent** (no migrations; D51). Default DI is EF InMemory when the connection string is empty / `<SECRET>`.

`apps/web/src/types/index.ts` `Overview` still names `totalBrokers` / `tradersByState` — **stale vs** `OverviewDto`. The page does not use that interface; it reads the JSON. Do not treat the TS file as a 5k contract.

---

## 7. Tests do not claim 5k (and barely claim 4)

```27:30:D:\Prop\tests\Integration\SeedingAndStoreTests.cs
        db.Brokers.Should().HaveCount(2);
        db.Mt5Groups.Count().Should().BeGreaterThan(2);
        db.Mt5Deals.Count().Should().BeGreaterThan(0);
        db.ReconstructedTrades.Any(t => t.Completed && t.CanonicalSymbol == "XAUUSD").Should().BeTrue();
```

No `db.Mt5Accounts.Should().HaveCount(4)`. No `HaveCount(5000)`. A27 / A90 `Mt5BackfillRestartTests` (kill mid-sync, restart, no duplicate `(broker_id, login)`, checkpoint advances) is **absent**. A79 5k unit facts are **absent**. A green `dotnet test` is **not** a scale proof.

---

## 8. Docs that overclaim vs this tree

| Source | Claim | Honest reading |
|---|---|---|
| `README.md` L22 | “Identify copyable XAUUSD traders from ~5,000 MT5 accounts…” | Goal sentence. C45: **overclaim**. Demo is 4 logins. |
| Architecture §3 / header | “5,000+ MT5 trader accounts” | Business envelope for **when** collectors exist. |
| Architecture §69.3 | Synchronize ~5,000 accounts | Gate. D41 / C13 / C54 / this file: **FAIL**. |
| `docs/architecture.md` | “Implemented toward first useful version” | Softer; does not say 5k is done. Still not a census. |
| A98 | index families for 5k | Design only. No applied DDL. |
| A79 | `SeedFiveThousandAccounts` | Spec for a type that was never added. |

---

## 9. Confusable “5000” tokens (not account scale)

| Token | Meaning | Path |
|---|---|---|
| `http://localhost:5000` | API listen port | `launchSettings.json`, README, Vite fallback |
| `MaxQuoteAgeMs = 5000` | 5 second quote-age placeholder | `CTraderFixOptions.cs` |
| `Balance = 5_000` | login 10002 equity fixture | `FakeMt5BrokerConnector.cs` L110 |
| `refetchInterval: 5000` | 5 s React poll | `hooks.ts` |
| `Take(200)` / `Take(20)` | trade / reject caps | `Program.cs` / `EfDashboardQueries` |
| Eval volumes `5000` | 0.50 lot @ scale 10 000 | `_tmp_*` only, not product |

Do not add these into a “we have 5000” sentence.

---

## 10. What would have to be true to say “~5,000 accounts”

Copied from A57 item 3 / D41 “Done when”, re-measured as **all false** today:

1. Both live Managers connected (not Fake `_connected = true`). **FAIL** (C42 / D24 / D41.1).
2. Every Manager-visible group discovered (not 4 fixture names). **FAIL** (D41.2).
3. Accessible logins upserted on `(broker_id, login)` at **~5k order of magnitude** (A79 dual-broker isolation would be **10,000** rows if both fakes seeded 5k with shared login numbers). **FAIL — 4.**
4. Checkpointed account + deal streams; kill mid-sync, restart, no duplicates. **FAIL** (D46).
5. Batch / pooled / measured wall-clock; not 5k × `SaveChanges`. **FAIL.**
6. Scoring / leaderboard / Overview count **that** population, not a hard-coded quartet. **FAIL.**
7. Integration fact `Mt5BackfillRestartTests` green on Postgres (not InMemory smoke). **MISSING.**
8. Overview tile shows the real count without `mt5Healthy = brokers > 0`. **FAIL** as honesty (D41).

Until then the correct operator sentence is:

> The demo seeds **four** MT5 logins (three Achiever, one StarwaveFX) and eighteen canned XAUUSD deals. Architecture wants ~5,000. That work is not started.

---

## 11. Classification (this slice only)

| Surface | Class |
|---|---|
| §69.3 Synchronize ~5,000 accounts | **FAIL** |
| `DemoBrokerFactory` 4-login tape | **EXISTS** (demo only) |
| `DealIngestionService` account walk | **EXISTS_NEEDS_REFACTOR** (shape OK, source is Fake, 1-row tx) |
| `mt5_accounts` unique `(BrokerId, Login)` | **EXISTS_AND_GOOD** as identity; empty of scale |
| `SyncCheckpoint` writer | **MISSING** |
| A79 5k fake + tests | **MISSING** |
| Paged `GET /mt5/accounts` | **MISSING** |
| Hard-coded score list of 4 | **UNSAFE** if anyone treats it as “all accounts” |
| Dashboard at 5k | **UNSAFE** (unproven; N=4 hides it) |
| README / §3 “~5,000 accounts” as current fact | **UNSAFE** (goal stated as inventory) |

---

## 12. Do not claim

- “We sync 5,000 accounts.” **False.**
- “InMemory 18 deals is a 5k census.” **False** (C42).
- “A79 landed.” **False.**
- “Overview `totalAccounts` will be ~5k in demo.” **False — 4.**
- “Unique index = 5k ready.” **False.**
- “§13 no-Kafka means scale is solved.” **False.**
- “Port 5000 / quote-age 5000 / balance 5,000 is the census.” **False.**

C13 / D41 item 3 **FAIL** is still the measured gate. This file only re-counts the same four logins and writes the scale sentence in one place.

**Product source modified: No.**
