# D59 — `reports/swarm/20260818/_tmp_*` is not product

| Field | Value |
|---|---|
| Agent | D59 (scratch-tree classification, read-only of product) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (directory listing + SHA-256 of authored files + sln grep) |
| Workspace | `D:\Prop` |
| Target | `D:\Prop\reports\swarm\20260818\_tmp_*` |
| Product source modified | **No.** This report is the only write. |
| Ask | Record that `_tmp_*` under this swarm day is throwaway eval/compile junk, not product. |
| Supersedes for *scratch identity* | INDEX header scratch line (`_tmp_b35_*`); D10 §10.8 (three trees only); C57 §4.1 (only `_tmp_c23_empty`) |
| Does **not** supersede | C57 on product sln membership; C56 on `Directory.Build.props` walk-up; C15 on product leftovers; any Domain/Application/FIX finding from the harnesses |

Classification: these trees are **not** `EXISTS_AND_GOOD` product. They are **scratch**. Treat them as `DEPRECATED` for shipping, catalog counts, and `dotnet sln`.

---

## 0. Verdict (binding)

**Every `D:\Prop\reports\swarm\20260818\_tmp_*` directory is throwaway measurement junk. None of it is product.**

Do **not**:

- add any `_tmp_*` `.csproj` to `Mt5TraderIntelligence.sln`
- count `_tmp_*` folders as swarm markdown reports
- count `_tmp_*` `.cs` / `.csproj` in product file censuses (`src/`, `apps/`, `tests/`)
- treat `_tmp_*\bin` / `_tmp_*\obj` as a release artifact
- copy harness `Program.cs` into `src/` or `tests/`
- cite a `_tmp_*` executable as a go-live or §60 test

Do:

- keep the companion `*.md` reports (C23, C31, C32, D11, D27, D37, …) as the durable findings
- exclude `_tmp_*` from layering, package, leftover, and coverage greps (C19 / C26 already do)
- leave the trees on disk unless the owner asks to delete them (they are large, not secret product)

**Product trees are only** `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests` (plus tracked `docs/`, `mt5-sdk/`, compose, and the product `.sln`). Reports live under `D:\Prop\reports\`. Scratch is a third class.

Measured: **0** `_tmp_` hits under `D:\Prop\src`. Measured: **0** `_tmp_` / `C23EmptyEval` / `C31ReconAdv` / `C32ScoreEval` / `D11ReconBugs` / `D27ParserEval` / `D37SeedEval` strings in `D:\Prop\Mt5TraderIntelligence.sln`.

---

## 1. What is on disk right now

Six directories. No other `_tmp_*` under `D:\Prop` at depth ≤5. `_tmp_b35_cv/`, `_tmp_b35_score/`, and `_tmp_qfn/` are **gone** (INDEX / A57 names are stale).

| Directory | Role | `.csproj` | Authored inputs | Output captured | Files | Bytes (tree) |
|---|---|---|---|---|---:|---:|
| `_tmp_c23_empty/` | C23 empty-login seeder/score eval | `C23EmptyEval.csproj` | `Program.cs` (208 lines) | `stdout.txt` | 57 | 9 783 921 |
| `_tmp_c31_recon/` | C31 adversarial `TradeReconstructor` | `C31ReconAdv.csproj` | `Program.cs` (594 lines) | console only | 31 | 589 021 |
| `_tmp_c32_score/` | C32 adversarial `BaselineScorer` | `C32ScoreEval.csproj` | `Program.cs` (303 lines) | `C32_measured.tsv` | 32 | 555 649 |
| `_tmp_d11_recon/` | D11 recon bug dump | `D11ReconBugs.csproj` | `Program.cs` (518 lines) | console only | 31 | 585 228 |
| `_tmp_d27_parser/` | D27 `FixMessageParser` eval | `D27ParserEval.csproj` | `Program.cs` (317 lines) | `stdout.txt` (UTF-16) | 37 | 1 184 259 |
| `_tmp_d37_eval/` | D37 `DemoSeeder` table dump | `D37SeedEval.csproj` | `Program.cs` (72 lines) | `stdout.txt` | 52 | 8 128 425 |

**Totals:** 6 trees, **241 files**, **20 828 748 bytes**. Almost all of that is `bin/` + `obj/` (copied product DLLs + NuGet graph). Authored source + captured stdout/tsv is on the order of **~100 KB**, not 20 MB.

Every tree has `bin/` and `obj/`. Those are build residue, not product.

---

## 2. Authored file hashes (this pass)

Only non-`bin` / non-`obj` authored files. Use these if a later agent claims a “new” harness in the same folder.

| Path | Bytes | SHA-256 |
|---|---:|---|
| `_tmp_c23_empty\C23EmptyEval.csproj` | 753 | `F602F573029EF6E7B1A429404BC6310061375C342755AC276A06444F995D240C` |
| `_tmp_c23_empty\Program.cs` | 10 189 | `03E68FE8D08C06C64DCF7EC02C5ACBC06C687CB14A06E623FC8DC2F994689BDF` |
| `_tmp_c23_empty\stdout.txt` | 1 760 | `FFE3D1729766A4091405BC81A35FC1D66981A0B1E7E1263F3A38F9770D01131E` |
| `_tmp_c31_recon\C31ReconAdv.csproj` | 353 | `46C9DE91B03B82D1D54C6CB3612F47306DB88F6B7DBB72657297D76947280AFF` |
| `_tmp_c31_recon\Program.cs` | 27 183 | `FED2C7C1E88A9AA8052342811C8CC0BD2F2D154FD09D4420CE1EF21116720D52` |
| `_tmp_c32_score\C32ScoreEval.csproj` | 353 | `46C9DE91B03B82D1D54C6CB3612F47306DB88F6B7DBB72657297D76947280AFF` |
| `_tmp_c32_score\Program.cs` | 12 179 | `6726036111C9BC5BC1C2A7038F18068872330476CDB311698964021768312EEC` |
| `_tmp_c32_score\C32_measured.tsv` | 3 448 | `7D1D7885B6F670F5EF445EB49EC98AA313BA694E783E866A2D3C6FFAF2ABCCE5` |
| `_tmp_d11_recon\D11ReconBugs.csproj` | 353 | `46C9DE91B03B82D1D54C6CB3612F47306DB88F6B7DBB72657297D76947280AFF` |
| `_tmp_d11_recon\Program.cs` | 18 510 | `B7771E8572BE052560B857E21F13AC75CAC3F52070E370A950D5B9133BC6CC23` |
| `_tmp_d27_parser\D27ParserEval.csproj` | 367 | `7590DA17E3A9D21395DF4273869ED3F3CAA689E1A0421120AC9F14D2E1C7B11D` |
| `_tmp_d27_parser\Program.cs` | 11 525 | `0A48860ACC662DD0B104CB2A142EE1B9426D12954508A866743AA63B8268661B` |
| `_tmp_d27_parser\stdout.txt` | 4 776 | `7AB055D39FDD3FDA3484312BC37CFD4CAAE0B9F488411CF0238A7A3D1A3BCD09` |
| `_tmp_d37_eval\D37SeedEval.csproj` | 1 052 | `91E4489A4D98FF8DFF4EE8B6E92710C586FECB534EFD57DD6AEF638C8645628B` |
| `_tmp_d37_eval\Program.cs` | 3 468 | `4B65BC0E3BFC00A01F7E73D9503245F710C33D0F5605B116008078FFE907A0A6` |
| `_tmp_d37_eval\stdout.txt` | 2 245 | `CE8D60B8861DFE45D8974A29D443FA8BA21FBF4BF87E0FB6033347D0755CF7E7` |

`C31ReconAdv.csproj`, `C32ScoreEval.csproj`, and `D11ReconBugs.csproj` share SHA-256 `46C9DE91…` because they are the same 353-byte Domain-only console stub. That is a content coincidence, not a product identity.

---

## 3. How they attach to product (consumers, not members)

| Harness | Reference style | Product projects touched |
|---|---|---|
| C23 | `ProjectReference` (absolute `D:\Prop\src\…`) | Domain, Application, Infrastructure, Mt5 + package `Microsoft.EntityFrameworkCore.InMemory` 8.0.4 |
| C31 | `ProjectReference` | Domain only |
| C32 | `ProjectReference` | Domain only |
| D11 | `ProjectReference` | Domain only |
| D27 | `ProjectReference` (relative `..\..\..\..\src\Fix.CTrader\…`) | Fix.CTrader (pulls Application + Domain) |
| D37 | **`<Reference HintPath>`** to `tests\Integration\bin\Debug\net8.0\*.dll` | Domain, Application, Infrastructure, Mt5 **binaries**, not the `.csproj` graph + InMemory 8.0.4 |

D37 is the worst junk pattern: it binds to a **Debug test output folder**, not a project. Rebuilds of Integration tests can break or silently retarget it. That is acceptable only because the host is not product.

C23 and D37 copy a large EF/Npgsql/Redis graph into `reports\…\bin\`. That is why those two trees are ~9 MB and ~8 MB. The DLLs under `_tmp_*\bin` are **copies**, not the source of truth.

---

## 4. Product solution (measured keep-out)

`D:\Prop\Mt5TraderIntelligence.sln` members (C57, re-read this pass):

| Folder | Projects |
|---|---|
| `src\` | Domain, Application, Infrastructure, Mt5, Fix.CTrader |
| `apps\` | Api, Mt5Worker, FixWorker |
| `tests\` | Tests.Unit, Tests.Integration |

**10** product `.csproj` files. **0** scratch `.csproj`.

C57 §4.1 already said keep `C23EmptyEval.csproj` out. That rule now applies to **all six** `_tmp_*` projects, including the three that did not exist when C57 landed (`D11`, `D27`, `D37`).

Root `Directory.Build.props` still walk-up-imports into these scratch trees (C56). They inherit `LangVersion=latest`, `Nullable=enable`, `TreatWarningsAsErrors=false`, `Deterministic=true`. Inheritance does **not** make them product. It is a reason **not** to put future scratch under `D:\Prop` if a different policy is needed.

Root `.gitignore` ignores `bin/` and `obj/` globally. It does **not** name `_tmp_*`. Authored scratch `.cs` / `.csproj` / `stdout.txt` / `.tsv` are therefore commitable noise if someone stages `reports/`. They are still not product.

---

## 5. What each host is (so nobody “promotes” it)

These one-liners exist so a later agent does not mistake a harness for a missing §60 test.

| Tree | Calls (product types) | Not a substitute for |
|---|---|---|
| `_tmp_c23_empty` | `DemoBrokerFactory`, `TradeReconstructor`, `BaselineScorer`, `DemoSeeder`, `EfDashboardQueries`, in-memory `TraderDbContext` | `tests/Unit` or `tests/Integration` empty-trader case. stdout ends `VERDICT=PASS_INSUFFICIENT_DATA` for login **10003**. Durable write-up: `C23_empty_trader.md`. |
| `_tmp_c31_recon` | `TradeReconstructor` + `VolumeConverter.Manager`; reflects for missing `Dirty` / `FailureCode` | A21 dirty-channel tests. Durable write-up: `C31_recon_adversarial.md`. |
| `_tmp_c32_score` | `BaselineScorer.Score` over synthetic `ReconstructedTradeResult`s | A22 / B12 / unit score tests. Durable dump: `C32_measured.tsv`. Write-up: `C32_score_adversarial.md`. |
| `_tmp_d11_recon` | Same reconstructor, INOUT / cancel / pos0 / symbol dumps | Product unit fixtures. Findings belong in D11 markdown, not this exe. |
| `_tmp_d27_parser` | `FixMessageParser.BuildFixMessage` / `Parse` | QuickFIX/n, a FIX session, or §60 FIX tests. stdout is UTF-16 and starts with a `Build_HB` mismatch. Write-up: D27 markdown. |
| `_tmp_d37_eval` | `DemoSeeder.SeedAsync` twice; prints every `DbSet` count | Migrations, PostgreSQL, or a seeder unit test. stdout reprints demo broker host/login fixture rows. Write-up: D37 markdown. |

None of the hosts implement `IBrokerConnector`, a worker, an API controller, or a web page. None ship.

---

## 6. Catalog hygiene

| Catalog | Scratch sentence today | Correction |
|---|---|---|
| `reports/INDEX.md` header | `_tmp_b35_cv/`, `_tmp_b35_score/`, `_tmp_c23_empty/` | First two directories are gone. On disk now: the six trees in §1. INDEX must not count `_tmp_*` in the markdown report total. |
| `reports/SWARM_LOG.md` | lists `_tmp_b35_*` and `_tmp_c23_empty` | Same stale set. |
| D10 §10.8 | `_tmp_c23_empty`, `_tmp_c31_recon`, `_tmp_c32_score` | Incomplete after D11/D27/D37 landed. This file is the census. |
| A57 | `_tmp_qfn\` local nupkgs | Directory **absent**. Do not hunt product QuickFIX there. |
| C41 / report counts | markdown `*.md` only | `_tmp_*` must stay out of the  A/B/C/D heading count. |

This file (`D59_tmp_junk.md`) **is** a swarm report. The directories it describes are **not**.

---

## 7. Rules for later agents

1. **Product source stays in `src/`, `apps/`, `tests/`.** If a finding needs a regression, add it under `tests/` in a later product change — do not graduate `_tmp_*`.
2. **New throwaway hosts** (if any) stay named `_tmp_<id>_…` under `reports/swarm/<day>/`, never beside live `.csproj` files, never `*.MUTATED*`.
3. **Do not** `dotnet sln add` a `_tmp_*` project. **Do not** `ProjectReference` a `_tmp_*` project from product.
4. **Grep scope:** exclude `reports\`, `bin\`, `obj\`, `node_modules\`, `vendor\`, `_tmp_*` when counting product packages, leftovers, or layering.
5. **Do not delete** these trees in this pass (owner standing: keep dumps/reports unless asked). They are junk, not contraband.
6. **Honesty:** a green `_tmp_*` `VERDICT=` line is a harness printout. It is not `dotnet test` and not a go-live gate.

---

## 8. What this report did not do

- Did not modify `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, or `Mt5TraderIntelligence.sln`.
- Did not delete `_tmp_*`.
- Did not rewrite INDEX / SWARM_LOG (stale scratch names remain there until a catalog pass).
- Did not re-run the harnesses; hashes and stdout are as found.
- Did not promote any harness assertion into a product test.

---

## 9. One-line for INDEX

`D59_tmp_junk.md` — six `_tmp_*` trees under `reports/swarm/20260818` are throwaway eval hosts (~21 MB bin/obj); **not product**, not sln members, not §60 tests.
