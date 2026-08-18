# D90 — Is `reports/INDEX.md` current?

| Field | Value |
|---|---|
| Agent | D90 (INDEX currency check only) |
| Date | 2026-08-18 |
| Assigned | Read `reports/INDEX.md`. Is it current? Write this file. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D90_index.md` |
| SUT | `D:\Prop\reports\INDEX.md` |
| Tree cataloged by INDEX | `D:\Prop\reports\swarm\20260818\` |
| Product source modified | **No.** This report (plus a scratch listing `_tmp_d90_census.txt`) is the only write. `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests` were not edited. `INDEX.md` was **not** rewritten. |
| Measured at (local) | 2026-08-18T13:46:18+05:30 |
| Measured at (UTC) | 2026-08-18T08:16:18Z |
| Prior snapshots | D10 (INDEX one tick behind at 13:35:21), C41 (bare `158`), D89 (272 pre-write at 13:43:51) |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict

**No. `D:\Prop\reports\INDEX.md` is not current.**

The file exists and is being patched while D-band reports land. That is not the same as a current catalog.

At the freeze below, the swarm tree had **286** top-level `*.md` files. INDEX advertised **three different totals in one file** (header **236**, footer **243**, table **252**), listed only **46** of **80** D-band files, and still carried a snapshot stamp of **13:33:33** even though its own `LastWriteTime` was **13:46:07**.

| Question | Measured answer |
|---|---|
| Does `INDEX.md` exist? | **Yes** — `D:\Prop\reports\INDEX.md` |
| Is it a complete catalog of `swarm\20260818\*.md`? | **No** |
| Are its own count fields consistent? | **No** (236 ≠ 243 ≠ 252 ≠ 286) |
| Is the A-band current? | **Yes** — A01–A105, 105/105, all tabled |
| Is the B-band current? | **Yes** — B01–B41, 41/41, all tabled |
| Is the C-band current? | **Yes** — C01–C60, 60/60, all tabled |
| Is the D-band current? | **No** — 80 on disk, 46 tabled, 34 missing |
| Does INDEX invent files that are not on disk? | **No** — 0 table rows missing from disk |
| Is the scratch-dir line current? | **No** |
| Is `reports/agents/` empty (INDEX claim)? | **Yes** — 0 children |
| Was product source modified to answer this? | **No** |

`INDEX.md` classification: **EXISTS_NEEDS_REFACTOR** as a catalog. Do not treat header **236**, footer **243**, C41 **158**, or D89 **272** as the live tree size.

Honest one-liner: **A/B/C are fully tabled; D is mid-landing; the header was frozen at wave-2 close and was not recensus'd.**

---

## 1. Frozen measurement

Two close reads, then this write. Concurrent D-agents kept appending both disk reports and INDEX rows between the reads. Numbers below are **T1** unless marked T0.

| Clock | Local | INDEX SHA-256 | INDEX bytes | INDEX lines | INDEX LastWrite | Table rows | Disk `*.md` | Disk D | Indexed D | On disk, not in table |
|---|---|---|---:|---:|---|---:|---:|---:|---:|---:|
| T0 | 13:45:36+05:30 | `F33CDCD46214C3CF8260198192963578E2DE2F2BD4889BD54E9ADAE6CA03E01A` | 47 677 | 285 | 13:45:28 | 251 | 280 | 74 | 45 | 29 |
| **T1 (this verdict)** | **13:46:18+05:30** | **`8DED01552366BD46C6B8317D45F5E5A59DFC581D754BD2CDDE1B09A9A0E71FCC`** | **47 905** | **286** | **13:46:07** | **252** | **286** | **80** | **46** | **34** |

T0 listing: `D:\Prop\reports\swarm\20260818\_tmp_d90_census.txt` (not a report).

Between T0 and T1 the tree gained at least `D65_docs.md`, `D74_enums.md`, `D75_launch.md`, `D80_settings.md`, `D81_livepage.md`, `D87_layer.md` (and INDEX absorbed `D81`). A later eye-read of INDEX (after T1, before this paragraph was typed) had already appended `D87` and bumped the footer to **243**. That is the point: **the catalog is a live append log, not a recensus.**

This file (`D90_index.md`) is **not** in the 286. After this write the live folder is at least **287**, and other D IDs may land in the same minute.

Recursive `*.md` count equals the top-level count: **0** markdown files under `_tmp_*`.

---

## 2. INDEX disagrees with itself

Quoted from `INDEX.md` at T1 (header still the wave-2 block; footer already past 236):

| Location | Claim | Live (T1) |
|---|---|---|
| Header `Cataloged` | `2026-08-18 (wave 2 recensus)` | Last write is an incremental D-row append, not a recensus |
| Header `Snapshot time` | `2026-08-18 13:33:33 +05:30` | **12 min 45 s** stale vs INDEX `LastWriteTime` 13:46:07 |
| Header `Markdown report count` | **236** | Disk **286** |
| Band table `D-series` | **30** | Disk **80** |
| Band table `Report total` | **236** | Disk **286** |
| File table row count | (not restated in header) | **252** backtick filename rows |
| Footer | `Counted markdown files: 243 (D87 row added; not a full recensus)` (post-T1 eye-read) | Footer admits it is **not** a recensus |
| Scratch line | `_tmp_b35_cv/`, `_tmp_b35_score/`, `_tmp_c23_empty/` | See §6 |
| `reports/agents/` | empty | **Holds** — 0 children |

Wave-2 arithmetic still holds as a **historical** close: 105 + 41 + 60 + 30 = 236. That close is real. It is no longer the tree.

Footer arithmetic does not even match the table: 206 (A+B+C) + 46 indexed D = **252** table rows at T1, not 243. Rows were appended without keeping the footer in lock-step.

---

## 3. Band census (disk, T1)

| Band | On disk | Id span | Gaps in 1..N | In INDEX table | Missing from INDEX |
|---|---:|---|---|---:|---:|
| A | **105** | A01–A105 | none | 105 | 0 |
| B | **41** | B01–B41 | none | 41 | 0 |
| C | **60** | C01–C60 | none | 60 | 0 |
| D | **80** | D01–D75, D79–D81, D87, D89 | 76, 77, 78, 82–86, 88, **90** | 46 | 34 |
| Other prefixes | **0** | — | — | 0 | 0 |
| **Total** | **286** | | | **252** | **34** |

D-band ids **on disk** at T1:

`1–75, 79, 80, 81, 87, 89`

D-band ids **in the INDEX table** at T1:

`1–10, 12–26, 28, 29, 31–33, 35, 38, 39, 41, 48, 50, 51, 53, 54, 56, 57, 62, 63, 72, 79, 81`

D-band ids **on disk and not in the table** at T1:

`11, 27, 30, 34, 36, 37, 40, 42, 43, 44, 45, 46, 47, 49, 52, 55, 58, 59, 60, 61, 64, 65, 66, 67, 68, 69, 70, 71, 73, 74, 75, 80, 87, 89`

`D90` is this report (gap at freeze). `_tmp_d92_vote/` and `_tmp_d98_noretry/` existed as scratch trees at T1 — later D ids are already in flight.

The D **file table is not in id order**. After `D26` the committed order at T1 was `D33, D38, D39, D28, D29, D31, D32, D51, D35, D41, D48, D50, D53, D54, D56, D57, D62, D63, D79, D72, D81` (then `D87` on the next append). That is append-as-landed, not a sorted catalog.

---

## 4. On disk, missing from INDEX (T1)

First headings were read from the files (UTF-8). Times are local `LastWriteTime`. SHA-256 is the first 12 hex chars.

| filename | bytes | mtime | sha12 | first heading |
|---|---:|---|---|---|
| `D11_recon_bugs.md` | 31433 | 13:41:46 | `A5A3A25F36C4` | # D11 — Adversarial bugs in `TradeReconstructor` |
| `D27_parser.md` | 19922 | 13:39:34 | `E0BFED0977C3` | # D27 — `FixMessageParser` (pipe codec, not a FIX engine) |
| `D30_api.md` | 19665 | 13:39:35 | `554CFF5AE21E` | # D30 — `apps/api` endpoints and secrets (measured from `Program.cs`) |
| `D34_score_tests.md` | 26096 | 13:39:18 | `1A206BB7CA6C` | # D34 — `BaselineScorerTests` surface (3 facts, not A22) |
| `D36_exec_tests.md` | 23823 | 13:39:20 | `700CD2EF5C99` | # D36 — `ExecutionAndSizingTests` review (FSM + qty + ClOrdID + expiry) |
| `D37_integ.md` | 33064 | 13:41:41 | `40B7601FA68A` | # D37 — `SeedingAndStoreTests` integration recensus (InMemory smoke ≠ §60) |
| `D40_secrets.md` | 17343 | 13:41:59 | `2F4EE6FBC7D8` | # D40 — Password / secrets grep (product source; vendor + reports excluded) |
| `D42_gates_now.md` | 38269 | 13:43:08 | `3EA1CE8ED0B3` | # D42 — Architecture §68 go-live gates scored vs current tests |
| `D43_s70.md` | 24402 | 13:42:49 | `FB0362AB5394` | # D43 — Architecture §70 live FIX acceptance: **all 14 FAIL for live** |
| `D44_reason_gap.md` | 16576 | 13:40:21 | `FD09BB8304C2` | # D44 — `DealReason` persist gap |
| `D45_outbox.md` | 14037 | 13:40:28 | `C50D82720C31` | # D45 — Is `OutboxEvent` written anywhere? |
| `D46_checkpoint.md` | 20654 | 13:41:23 | `0C95F41D3767` | # D46 — Is `SyncCheckpoint` written? |
| `D47_copyintent.md` | 21022 | 13:41:40 | `73D836674A35` | # D47 — Is `CopyIntent` created after score `SHADOW`? |
| `D49_detail_thin.md` | 33902 | 13:43:05 | `B88F5434C2E3` | # D49 — `GetTraderAsync` vs A93 (detail is thin) |
| `D52_qfn.md` | 17297 | 13:40:54 | `F84227358525` | # D52 — csproj QuickFIX? **No official QuickFIX/n on any product project** |
| `D55_redis.md` | 17595 | 13:40:55 | `61E087AC73C2` | # D55 — Is `StackExchange.Redis` used? |
| `D58_lp.md` | 15627 | 13:42:10 | `582BF44247EC` | # D58 — Product-code grep for `LP` |
| `D59_tmp_junk.md` | 12043 | 13:40:48 | `46154E326710` | # D59 — `reports/swarm/20260818/_tmp_*` is not product |
| `D60_sln.md` | 24042 | 13:41:52 | `4AC437A4B4E1` | # D60 — `Mt5TraderIntelligence.sln` project list (remeasured) |
| `D61_env.md` | 15411 | 13:42:05 | `B425911B5A47` | # D61 — `D:\Prop\.env.example`: placeholders only? |
| `D64_readme.md` | 32890 | 13:45:27 | `59E2E303A5D2` | # D64 — `D:\Prop\README.md` vs the as-built tree |
| `D65_docs.md` | — | 13:46:02 | — | # D65 — `docs/*.md` completeness vs architecture §66 |
| `D66_sdk.md` | 20188 | 13:45:27 | `A8F178781656` | # D66 — Confirm `mt5-sdk` C++ was not rewritten |
| `D67_http_groups.md` | 15096 | 13:42:58 | `CC2A6E35C81F` | # D67 — Confirm `MT5HttpClient::GetGroupDetails` is a hard-false stub |
| `D68_plan_filter.md` | 21419 | 13:43:27 | `82D041EEBB07` | # D68 — Does ingestion filter by plan groups? |
| `D69_flag.md` | 10612 | 13:43:18 | `3973353501EB` | # D69 — `RealCopyExecutionEnabled` default is **`false`** |
| `D70_kill.md` | 24473 | 13:44:02 | `CEF135CE0006` | # D70 — Are `STOP_NEW` and `FLATTEN` distinct? |
| `D71_expire.md` | 18500 | 13:43:28 | `C9BAF76519AC` | # D71 — Is `CopyIntentExpiry` used? |
| `D73_canceled.md` | 14239 | 13:44:01 | `7AC73C7EC22B` | # D73 — Does `IsTradingDeal` exclude canceled? |
| `D74_enums.md` | — | 13:45:49 | — | # D74 — Does the API use `JsonStringEnumConverter`? |
| `D75_launch.md` | — | 13:45:57 | — | # D75 — `launchSettings` weather leftover? |
| `D80_settings.md` | — | 13:46:09 | — | # D80 — `SettingsPage.tsx` (route `/settings`) |
| `D87_layer.md` | — | 13:45:51 | — | # D87 — Infrastructure references Mt5: still OK? |
| `D89_count.md` | 9459+ | 13:44:41 / 13:45:38 | `E6B530A11B95` | # D89 — Markdown file count for `reports/swarm/20260818` |

Blank byte/sha cells are T1 landings after the T0 hashed listing. Headings were re-read from disk. `D87` was still **absent** from the T1 table and **present** on the next INDEX append (footer “D87 row added”).

T0→T1 also added `D81_livepage.md` **into** the table (so it is not in the missing list).

---

## 5. What in INDEX is still usable

Do not throw the file away. These claims still matched disk at T1:

| INDEX claim | Status |
|---|---|
| A01–A105 complete, no gaps | **Holds** |
| B01–B41 complete, no gaps | **Holds** |
| C01–C60 complete, no gaps | **Holds** |
| Other bands = 0 | **Holds** |
| Every table filename exists on disk | **Holds** (0 ghosts) |
| `reports/agents/` empty | **Holds** |
| Catalog did not modify product source | Not re-proven here; this agent also did not touch product source |
| Wave-2 headline integers (sec69 0/12, sec68 0/19, sec70 0/14; Domain compiles; live NOS off; live MT5/FIX unproven; ML not built) | **Not re-scored by D90.** Later files that *do* re-score those gates (`D41`, `D42`, `D43`) exist; `D42`/`D43` are **not even in the table**. |
| Stale-vs-later map (A01→B01 … A57→D41, A65→D63, A103→D62, …) | **Partially updated**, still incomplete — see §7 |
| `C41_report_count.md` summary “stale integer snapshot (158)” | **Holds** — file body is still `158` |
| First-heading column vs disk for tabled D rows | **No heading mismatches** on the rows that exist |

---

## 6. Scratch directories — INDEX line is stale

INDEX header (unchanged since wave 2):

```text
Scratch (not markdown reports): `_tmp_b35_cv/`, `_tmp_b35_score/`, `_tmp_c23_empty/` - throwaway compile trees.
```

| Name | INDEX claims it | On disk at T1 |
|---|---|---|
| `_tmp_b35_cv/` | yes | **No** |
| `_tmp_b35_score/` | yes | **No** |
| `_tmp_c23_empty/` | yes | **Yes** |
| `_tmp_c31_recon/` | no | Yes |
| `_tmp_c32_score/` | no | Yes |
| `_tmp_d11_recon/` | no | Yes |
| `_tmp_d27_parser/` | no | Yes |
| `_tmp_d37_eval/` | no | Yes |
| `_tmp_d48_shadow/` | no | Yes |
| `_tmp_d57_mfe/` | no | Yes |
| `_tmp_d72_first3/` | no | Yes |
| `_tmp_d74_enums/` | no | Yes |
| `_tmp_d92_vote/` | no | Yes |
| `_tmp_d98_noretry/` | no | Yes |
| `_tmp_d90_census.txt` (file, not dir) | no | Yes — D90 method listing |

`_index_extract.tsv` (called **stale** by D10, 165 data rows) is **gone** from `D:\Prop\reports\`.

None of these scratch trees contain `*.md`. D59 (`_tmp_*` is not product) is on disk and **not** in INDEX.

---

## 7. Stale-vs-later map is behind the D-band

INDEX tells readers to prefer later files for some A/B/C rows. At T1 it already names `D05`, `D06`, `D07`, `D08`, `D41`, `D62`, `D63`. It does **not** point at later D files that exist and are the current measurement for the same questions:

| Topic | INDEX still sends you to | Later file on disk, **not in INDEX table** at T1 |
|---|---|---|
| §68 go-live 0/19 | A100 / C14 (headline) | `D42_gates_now.md` |
| §70 live FIX 0/14 | A101 (headline) | `D43_s70.md` |
| Markdown file count | C41 (`158`) in the table | `D89_count.md` (272 pre-write; live recount 278) |
| CopyIntent after SHADOW | C59 “reconstruction does not emit CopyIntent” | `D47_copyintent.md` (claims C59 writers stale) |
| §66 docs completeness | D10 (tabled) / C11 | `D65_docs.md` |
| mt5-sdk not rewritten | C20 | `D66_sdk.md` |
| README vs tree | C30 / C45 | `D64_readme.md` |
| QuickFIX/n package | C19 | `D52_qfn.md` |
| Redis used? | C27 | `D55_redis.md` |
| sln membership | C57 | `D60_sln.md` |

D90 did **not** re-judge those product questions. The catalog failure is: **those later files are invisible if you trust INDEX as complete.**

---

## 8. Prior count artifacts (do not mix)

| Artifact | When | Count it published | Role |
|---|---|---|---|
| `C41_report_count.md` | early wave | **158** (file body, no heading) | Obsolete |
| D10 vs INDEX | 13:35:21 | INDEX table **207**; disk matched **before** D10 | D10 itself was already off-catalog |
| INDEX header | 13:33:33 stamp | **236** = 105+41+60+30 | Wave-2 close; frozen |
| INDEX footer (post-T1 eye-read) | ~13:46 | **243** “D87 row added; not a full recensus” | Incremental, self-confessed incomplete |
| INDEX table | T1 13:46:18 | **252** rows | Best INDEX-internal number; still short |
| `D89_count.md` | 13:43:51 / 08:15:21Z recount | **272** pre-write, **278** after | Honest count, already behind T1 **286** |
| **This file (D90)** | **13:46:18** | **286** disk / **252** table / **34** missing | Currency check, not a replacement INDEX |

---

## 9. Method

| Step | Action |
|---|---|
| Read | Full `D:\Prop\reports\INDEX.md` (header, band table, stale-vs-later, file table, footer) |
| Hash | `Get-FileHash -Algorithm SHA256` on INDEX at T0 and T1 |
| Disk | `Get-ChildItem -LiteralPath D:\Prop\reports\swarm\20260818 -File -Filter '*.md'` and `-Recurse` |
| Table extract | Regex `^\| \`([^`]+)\` \|` on every INDEX line |
| Diff | Set-compare table names vs disk names |
| Headings | First `# ` line of each missing file (UTF-8) |
| Bands | Parse `^[ABCD](\d+)_` |
| Scratch | `Get-ChildItem -Directory` of the swarm folder |
| Agents | `D:\Prop\reports\agents` child count |
| Related | D10, D89, C41, SWARM_LOG existence only |
| Not done | No `INDEX.md` rewrite, no `SWARM_LOG.md` append, no `dotnet`, no product edit |

Windows `-Filter '*.md'` is case-insensitive. Every match on disk uses lowercase `.md`.

---

## 10. Honesty

- **INDEX is useful and stale at the same time.** A/B/C are complete. D is not. Header counts are a 13:33:33 photograph.
- **Do not greenwash** “INDEX was just written” (LastWrite ~13:46) as “INDEX is current.” The write is an append of one D row. The header still says 236 / D=30.
- **Do not greenwash** footer “243” or table “252” as the disk count. Disk at T1 is **286**.
- Counts in this file will be stale within minutes while D76–D88 / D91+ land. The **verdict** (not current) remains true until someone recensus-es the header, the band table, the scratch line, and every on-disk `*.md`.
- D89’s 272 is a correct earlier listing, not the T1 number.
- C41’s 158 is a correct earlier listing of an older tree, not a catalog.
- This agent did not verify product claims inside INDEX (sec69/68/70 integers, Domain compile, etc.). Currency here means **catalog completeness and internal consistency**, not a re-audit of the platform.
- `_tmp_d90_census.txt` is method scratch, not a swarm report, not product.

**`reports/INDEX.md` is not current. Recensus it; do not hand-edit product source to make the catalog look finished.**
