# D99 — Session law: 100+ agents every non-trivial turn

| Field | Value |
|---|---|
| Agent | D99 (orchestration policy pin) |
| Date | 2026-08-18 |
| Measured at (UTC) | 2026-08-18T08:15:42Z |
| Measured at (local) | 2026-08-18T13:45:42+05:30 |
| Artifact | `D:\Prop\reports\swarm\20260818\D99_100plus_policy.md` |
| Assigned | Write this file. Pin: this session requires **100+ agents** every non-trivial turn. **Do not modify product source.** |
| Product source modified | **No.** Report only. `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `D:\Prop\mt5-sdk` (owned wrappers), `Mt5TraderIntelligence.sln` were not edited. |
| INDEX / SWARM_LOG rewritten by this agent | **No.** Orchestrator catalog duty. |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` |
| Binding siblings | `D:\Prop\reports\SWARM_LOG.md` Wave D entry; `D89_count.md` (stale count); `C41_report_count.md` (obsolete `158`); `D10_docs_census.md`; `D59_tmp_junk.md` |
| Architecture | Does **not** change §68 / §69 / §70 scores. This is lab-process law, not a go-live gate. |

**Law (this session, binding):** every **non-trivial** user turn on `D:\Prop` must launch and land **≥100 distinct agents**, each leaving a **permanent on-disk** report. Prefer **103+** (Wave D roster `D01`–`D103`). Under-use is a **hard policy FAIL** even if product tests look green.

Chat is not storage. “We thought about 100 topics” is not 100 agents.

---

## 0. Verdict

| Question | Measured answer |
|---|---|
| Floor this session | **100 agents / non-trivial turn** |
| Wave D launched roster (SWARM_LOG) | **D01–D103** = **103** slots (≥100) |
| D-band `*.md` on disk **before this write** | **71** |
| D-ids present before this write | `1–63, 67–71, 73, 79, 89` |
| D-ids missing in `1..103` before this write | **32**: `64–66, 72, 74–78, 80–88, 90–103` |
| This file (`D99`) present at snapshot | **No** |
| Expected D-count after this write (no concurrent landings) | **72** |
| Expected remaining gaps after this write | **31** (still includes `100–103` and the rest of the missing set except `99`) |
| Swarm `*.md` total before this write | **277** (`A=105` + `B=41` + `C=60` + `D=71`) |
| Expected swarm `*.md` after this write | **278** |
| Byte sum of 277 pre-write `*.md` | **7,299,656** |
| `C41_report_count.md` | **4 bytes**, body `158` — **obsolete** |
| `D89_count.md` pre-write snapshot | **272** (then +D89 → 273 claim) — **stale vs 277** |
| `reports/agents/` | **Empty** |
| Product source touched by D99 | **No** |
| §69 / §68 / §70 flipped by this file | **No.** Still **0/12**, **0/19**, **0/14** (D41 / D42 / D43). |

**Headline:** the **rule** is 100+. Wave D **intended** 103. Disk **has not yet** 100 D-files. Launch ≠ land. Do not greenwash 71 (or 72 after this file) as “100+ agents completed.”

---

## 1. Why this file exists

`SWARM_LOG.md` records the user order for 2026-08-18 Wave D:

> User: **100+ sub agents always**. Launched **D01–D103**.

D99 is the **durable pin** of that order so later turns do not silently drop back to a 50-agent floor, a “best effort” fan-out, or a single-agent recensus that writes one markdown and calls the turn done.

This is **session / lab process** law for `D:\Prop`. It does not authorize:

- live `NewOrderSingle`
- enabling `REAL_COPY_EXECUTION_ENABLED`
- incrementing first-useful or go-live scoreboards
- rewriting product source from a report agent

---

## 2. Binding rules

### 2.1 Floor

| Token | Meaning |
|---|---|
| **100+** | At least **100 distinct agent IDs** produced **permanent** artifacts this turn. |
| **Prefer 103+** | Wave D roster is `D01`–`D103`. Later waves may use `E01`–`E103+` (or continue D-ids). Do not reuse a spent id as a new agent. |
| **Hard FAIL if <100** | The user rejects the turn. Green `dotnet test` / a complete A/B/C catalog does not excuse it. |
| **Queue is allowed** | Platform concurrent caps do not lower the floor. Launch **sequential parallel waves** until ≥100 outputs exist on disk. |

### 2.2 What is a “turn”

A **turn** is one user message that asks for work (audit, implement, review, test, plan, continue, recensus, score, research, “write the D-band”, “keep going”).

The orchestrator must treat that message as **non-trivial** unless §2.3 applies.

### 2.3 Trivial exception (narrow)

**Trivial** (may skip the 100+ launch):

- a one-line factual ack with no repo work (“yes”, “received”)
- clock / path / “did file X land?” answered from a single `Test-Path` / `Get-ChildItem` already on screen
- a user message that is **only** “stop” / “cancel swarm”

**Not trivial** (100+ required):

- anything that reads more than one product area
- anything that writes a report, plan, or scoreboard
- “continue”, “recensus”, “fix”, “review”, “test”, “go-live”, “first useful”
- any request that names a swarm id (`Dxx`) or a wave
- any request the user marks as non-trivial

The user may demand 100+ **even on a trivial question**. Then 100+ is required.

### 2.4 What counts as an agent

An agent **counts** only if **all** of these are true:

1. It has a **unique id** in the current wave (`D01` … `D103`, later `E…`).
2. It was given a **single, named assignment** (one question, one file, one census).
3. It wrote a **non-empty** markdown under `D:\Prop\reports\swarm\<YYYYMMDD>\` whose filename starts with that id (`D99_100plus_policy.md`).
4. The file has a heading, a metadata table, **Product source modified = Yes/No**, a **measured** verdict (paths, counts, SHA-256 or command output), and a one-line INDEX blurb.
5. The content is **not** a copy-paste of another id with the number changed.

**Does not count:**

| Non-count | Why |
|---|---|
| Chat reply with no file | Chat is not storage |
| Orchestrator claiming “launched 103” | Launch ≠ land |
| One agent writing 100 files | That is **1** agent |
| `_tmp_*` eval hosts / `bin` / `obj` | Throwaway; D59 |
| Empty or 4-byte stub (`C41` = `158`) | Not a measured report |
| Editing `src/` / `apps/` / `tests/` | Product work, not an agent output for this floor |
| Re-using `D41_fuv_now.md` as “D99” | Id collision; overwrite is a FAIL |
| `reports/agents/` copies (dir is empty) | Not used this session |

Sub-subagents count **only** if each leaves its **own** uniquely IDed file. Nested chatter does not.

### 2.5 Product-source ban (this wave / this file)

Wave D report agents, including D99, **do not modify product source**.

Product trees:

- `D:\Prop\src\`
- `D:\Prop\apps\`
- `D:\Prop\tests\`
- `D:\Prop\mt5-sdk\` owned wrappers (`src\`, `config\`, `tests\` — not vendor)
- `D:\Prop\Mt5TraderIntelligence.sln`
- `D:\Prop\Directory.Build.props`
- `D:\Prop\docker-compose.yml`
- `D:\Prop\docs\` (architecture §66 docs are product docs; do not silently rewrite)

Allowed writes for a report agent:

- `D:\Prop\reports\swarm\<day>\<ID>_*.md` (its own artifact)
- throwaway `_tmp_<id>_*/` **only if** the assignment requires a compile probe, never `dotnet sln add`

Orchestrator-only (not D99): `INDEX.md`, `SWARM_LOG.md`, batch TSV, later product increments under a **separate** explicit implement turn.

HEAD `398a142` already has dirty product files (`apps/api/Program.cs`, workers, etc.). Those pre-exist this report. D99 did not touch them.

### 2.6 Honesty / no greenwash

- Quote **disk**. Never “100+ complete” unless `Get-ChildItem D*.md` shows ≥100 distinct D-ids (or the current wave’s ids).
- Stale counts stay labeled stale: **C41 = 158**, **D89 = 272**, **INDEX header 236** (written mid-landing).
- §69 **0/12**, §68 **0/19**, §70 **0/14** are independent. More reports do not raise those integers.
- Fake MT5 + InMemory + `EnsureCreated` remains **DEMO**, not live.

---

## 3. How the orchestrator must run a non-trivial turn

```text
1. Classify the user turn (trivial vs non-trivial). If non-trivial → 100+ floor.
2. Mint ≥100 unique ids (Wave D: D01–D103). Do not skip the high ids.
3. Fan out in parallel waves (platform concurrent cap → queue the rest).
4. Each child writes exactly one `reports/swarm/<day>/<ID>_<slug>.md`.
5. Do not modify product source unless the user turn is an explicit implement
   pass *and* that implement is itself staffed at 100+ (coder/reviewer/test
   ids included in the 100).
6. After landings: recount `*.md`, list missing ids, append SWARM_LOG,
   refresh INDEX. Missing ids are a wave defect, not “close enough.”
7. Reviewer pass on a sample of reports (contradictions, stale hashes,
   health lies). Test pass only when the turn included product edits.
8. DONE = reviewer PASS + (tests PASS if code changed) + ≥100 permanent
   agent files + SWARM_LOG/INDEX updated. Otherwise NOT DONE.
```

Platform concurrency is an implementation detail. **The floor is a landing count.**

---

## 4. Required permanent artifacts (every non-trivial turn)

| Artifact | Path | Owner |
|---|---|---|
| Per-agent report | `D:\Prop\reports\swarm\<YYYYMMDD>\<ID>_<slug>.md` | each agent |
| Wave roster | SWARM_LOG section listing launched ids | orchestrator |
| Catalog | `D:\Prop\reports\INDEX.md` row per new file | orchestrator (not this agent) |
| Optional TSV | `D:\Prop\reports\swarm\<day>\_manifest.tsv` | orchestrator |
| Scratch eval (if any) | `...\swarm\<day>\_tmp_<id>_*/` never in the sln | named eval agent |

`D:\Prop\reports\agents\` is **empty** at this measurement. Do not invent copies there unless a later standing order says so.

---

## 5. Measured Wave D landing (2026-08-18T08:15:42Z)

Command (PowerShell):

```powershell
$root = 'D:\Prop\reports\swarm\20260818'
$mds  = Get-ChildItem -Path $root -Filter '*.md' -File
$d    = $mds | Where-Object { $_.Name -match '^D(\d+)_' }
$ids  = $d | ForEach-Object { [int]($_.Name -replace '^D(\d+)_.*','$1') } | Sort-Object -Unique
$missing = 1..103 | Where-Object { $_ -notin $ids }
```

| Prefix | Count | Completeness |
|---|---:|---|
| A01–A105 | **105** | complete |
| B01–B41 | **41** | complete |
| C01–C60 | **60** | complete |
| D (pre-write) | **71** | **71 / 103** launched slots |
| Other prefixes | **0** | — |
| **Total `*.md`** | **277** | D99 not yet in this number |

### 5.1 Present D-ids (71)

`01–63, 67, 68, 69, 70, 71, 73, 79, 89`

### 5.2 Missing D-ids in `01..103` (32, including this id before write)

`64, 65, 66, 72, 74, 75, 76, 77, 78, 80, 81, 82, 83, 84, 85, 86, 87, 88, 90, 91, 92, 93, 94, 95, 96, 97, 98, **99**, 100, 101, 102, 103`

After this file lands, **99** leaves the missing list. **100+ D-files are still not on disk.** The session floor is therefore **not yet satisfied by landing count**, even though the **launch roster** is 103.

### 5.3 Prior count artifacts (do not cite as current)

| File | Body / claim | Status at D99 measure |
|---|---|---|
| `C41_report_count.md` | `158` | Obsolete (4 bytes) |
| `INDEX.md` header | **236** markdown | Mid-wave; behind 277 |
| `D10_docs_census.md` | INDEX table **207** | Pre-most-D landing |
| `D89_count.md` | **272** pre-write / 66 D-files | Behind: 277 / 71 D |

---

## 6. Quality loop (still mandatory)

```text
CODER → REVIEWER (unbiased) → [fix] → REVIEWER → TEST
  → (on FAIL: RESEARCHER → CODER → REVIEWER → TEST)
  → PASS+PASS = DONE
```

For a **report-only** turn (this file’s class):

- CODER = the agent that writes the assigned `*.md`
- REVIEWER = a different id that checks evidence vs disk (no rubber-stamp)
- TEST = recount + “product source unmodified” + no secrets committed

For an **implement** turn: reviewer + `dotnet test` (and any scoped frontend/sdk tests) must PASS on disk. Demo InMemory green is not §68 PASS.

D99 does **not** self-review. A later id (`D100` reviewer slot, or Wave E) should confirm this file’s 277/71 numbers if the wave is still landing.

---

## 7. Relation to other floors

| Floor | Scope | Relation |
|---|---|---|
| User 2026-08-18 “100+ sub agents always” | **This `D:\Prop` session** | **Wins.** This file pins it. |
| Historical 50+ standing order (other labs) | Other trees | **Raised to 100+** here. 50 is a FAIL on this session. |
| Architecture §68 / §69 / §70 | Product acceptance | Unchanged by agent count. |
| D59 scratch rules | `_tmp_*` | Still in force. |
| D-wave “do not modify product source” | Report agents | Still in force for D99. |

---

## 8. Fail / pass checklist for a turn

```text
[ ] ≥100 unique agent ids launched
[ ] ≥100 unique report files on disk under reports/swarm/<day>/
[ ] No id collisions / overwrites
[ ] Each file has measured evidence + Product-source field
[ ] Missing-id list published in SWARM_LOG
[ ] INDEX rows added for new files (orchestrator)
[ ] Product source unchanged unless the turn was an explicit implement
[ ] §69/§68/§70 integers not incremented without a dedicated rescore
[ ] No secrets / broker passwords in reports
[ ] Reviewer PASS on a sample (contradiction hunt)
[ ] Tests PASS if product code changed
```

**This turn (D99 write only):** one file added. The **wave** is still short of 100 D-landings. D99 records the law; it does not by itself make the wave PASS the landing floor.

---

## 9. What this report did not do

- Did not modify `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `mt5-sdk` wrappers, or the `.sln`.
- Did not rewrite `INDEX.md` or `SWARM_LOG.md`.
- Did not delete `_tmp_*`.
- Did not launch the other 31 missing D-ids (orchestrator duty).
- Did not rescore §68 / §69 / §70.
- Did not treat 71 D-files as 100+.

---

## 10. One-line for INDEX

`D99_100plus_policy.md` — binding session law: **100+ agents every non-trivial turn** (Wave D roster D01–D103). Measured pre-write: **71 / 103** D-files, **277** swarm markdowns. Product source **not** modified. Launch ≠ land.
)
