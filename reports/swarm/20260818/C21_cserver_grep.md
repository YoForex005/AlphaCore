# C21 — `CSERVER` grep under `D:\Prop\src` (intended: `cServer`)

| Field | Value |
|---|---|
| Agent | C21 (senior engineer, grep census only) |
| Date | 2026-08-18 13:25 +0530 |
| Assigned | Grep `CSERVER` under `D:\Prop\src`. After intended fix should be `cServer`. Write this report. Do not modify product source. |
| Scope | `D:\Prop\src` only (product tree). Apps/tests/docs noted only as out-of-scope context. |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §25–§26 (never silently change `cServer` → `CSERVER`) |
| Product source modified | **No.** This report is the only write. |
| HEAD at measure | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`, 2026-08-18 13:24:21 +0530) |
| Prior | `B27_cserver_case.md`, `C09_cserver_fixed.md` |

---

## 0. Verdict

| Tree | Exact `CSERVER` under `D:\Prop\src` | vs intended `cServer` |
|---|---|---|
| **Worktree disk** | **0 hits** | **PASS** — every CompID literal is already `cServer` |
| **Committed HEAD** | **7 hits / 2 files** | **FAIL** — still the RoE spelling §26 forbids as a silent default |

**Worktree is the intended spelling. HEAD is not.** Do not treat `CSERVER` as gone from the product. `git status` still has ` M src/Fix.CTrader/Configuration/CTraderFixOptions.cs` and ` M src/Fix.CTrader/Testing/FixSimulationHarness.cs` (unstaged). `git blame` on every flipped line is `Not Committed Yet` (2026-08-18 13:25:31 +0530). This agent did not make or revert those edits.

PowerShell `Select-String` without `-CaseSensitive` is a false friend: it matches `cServer` when searching `CSERVER`. All counts below are **ordinal / case-sensitive** (`git grep` default; `Select-String -CaseSensitive`).

---

## 1. Commands run (this measure)

From `D:\Prop` (repo root; **not** `D:\Prop\src`, which would double-prefix `src/src/...`):

```text
git grep -n -I --untracked "CSERVER" -- src          → 0 lines, exit 1
git grep -n -I "CSERVER" HEAD -- src                 → 7 lines, exit 0
git grep -n -I --untracked "cServer" -- src          → 9 lines
git grep -n -I -i --untracked "cserver" -- src       → 9 lines (same set; no other casings)
git grep -n -I --untracked "CServer" -- src          → 0
git grep -n -I --untracked "cSERVER" -- src          → 0
```

Filesystem walk of every file under `D:\Prop\src` (`Select-String -SimpleMatch -CaseSensitive "CSERVER"`, files < 5 MB): **count = 0**.

`git grep CSERVER` on `apps`, `tests`, `.env.example` (out of assigned path): **0**.

---

## 2. Worktree (`D:\Prop\src`) — intended spelling present

Exact `CSERVER`: **none**.

Exact `cServer` (9 lines / 3 files):

| File | Line | Literal | Role |
|---|---:|---|---|
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | 49 | `= "cServer"` | `QuoteFixOptions.TargetCompId` default (tag 56) |
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | 70 | `= "cServer"` | `TradeFixOptions.TargetCompId` default (tag 56) |
| `src/Fix.CTrader/Testing/FixSimulationHarness.cs` | 16 | default `"cServer"` | `SimulateLogonSuccess` TargetCompID |
| `src/Fix.CTrader/Testing/FixSimulationHarness.cs` | 30 | default `"cServer"` | `SimulateLogonFail` TargetCompID |
| `src/Fix.CTrader/Testing/FixSimulationHarness.cs` | 129 | default `"cServer"` | `SimulateSecurityList` TargetCompID |
| `src/Fix.CTrader/Testing/FixSimulationHarness.cs` | 155 | `(56, "cServer")` | MD snapshot tag 56 |
| `src/Fix.CTrader/Testing/FixSimulationHarness.cs` | 187 | `(56, "cServer")` | ExecutionReport tag 56 |
| `src/Infrastructure/Seeding/DemoSeeder.cs` | 77 | `TargetCompId = "cServer"` | seed QUOTE `FixSessionState` |
| `src/Infrastructure/Seeding/DemoSeeder.cs` | 94 | `TargetCompId = "cServer"` | seed TRADE `FixSessionState` |

`DemoSeeder.cs` is **untracked** (`git ls-files` empty; `?? src/Infrastructure/Seeding/`). It never contained `CSERVER` on this tree.

`src/Domain/Entities/FixSessionState.cs` line 13: `TargetCompId` defaults to `string.Empty` — no CompID spelling.

No other casing (`CServer`, `cSERVER`, `Cserver`) exists under `src`.

---

## 3. HEAD (`src`) — `CSERVER` still committed (7 hits)

`git grep -n -I "CSERVER" HEAD -- src`:

```text
HEAD:src/Fix.CTrader/Configuration/CTraderFixOptions.cs:49:        public string TargetCompId { get; set; } = "CSERVER";
HEAD:src/Fix.CTrader/Configuration/CTraderFixOptions.cs:70:        public string TargetCompId { get; set; } = "CSERVER";
HEAD:src/Fix.CTrader/Testing/FixSimulationHarness.cs:16:    ... targetCompId = "CSERVER" ...
HEAD:src/Fix.CTrader/Testing/FixSimulationHarness.cs:30:    ... targetCompId = "CSERVER" ...
HEAD:src/Fix.CTrader/Testing/FixSimulationHarness.cs:129:   ... targetCompId = "CSERVER" ...
HEAD:src/Fix.CTrader/Testing/FixSimulationHarness.cs:155:            (56, "CSERVER"),
HEAD:src/Fix.CTrader/Testing/FixSimulationHarness.cs:187:            (56, "CSERVER"),
```

`git grep -c "CSERVER" HEAD -- src`:

| HEAD path | Count |
|---|---:|
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | 2 |
| `src/Fix.CTrader/Testing/FixSimulationHarness.cs` | 5 |
| **Total** | **7** |

Those 7 are the **entire** committed `CSERVER` surface under `src`. No JSON / XML / `.cfg` / `.env` under `src` has `CSERVER` on HEAD or worktree.

---

## 4. Blobs / hashes (do not confuse trees)

| Path | Tree | Git blob | SHA-256 (worktree file) |
|---|---|---|---|
| `CTraderFixOptions.cs` | HEAD / index | `204f9d58a913022c31cdb4fa2eefef9d92916795` | — |
| `CTraderFixOptions.cs` | worktree | `f2cd089d29304a3e107dbc1e58957421a65296d6` | `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` |
| `FixSimulationHarness.cs` | HEAD | `433326b250476b920f1b10c104a8bb83c160e45f` | — |
| `FixSimulationHarness.cs` | worktree | `19dd8a7a1069d8f451d2e0149f2e474893ee6eef` | `99A28D8F3F49028706C75B9C4DC46B4CCB3FF98E90AAED3B4B874DD1B4351616` |

Index still equals HEAD for both files (staged diff empty). Unstaged `git diff HEAD` is only the seven `"CSERVER"` → `"cServer"` replacements (two in options, five in harness).

Same `CTraderFixOptions` blob pair B27 / C09 recorded. HEAD moved (`406511a8` / `cacdb70` → `398a1420`) but the options blob did not.

---

## 5. Correction vs C09

C09 grepped the **worktree** (`git grep CSERVER -- *.cs` → 0) and listed harness tag 56 as already `cServer`. That is true of disk.

C09 did **not** `git grep CSERVER HEAD -- src`. HEAD harness still has **five** `CSERVER` literals. The committed bug is **not** only `CTraderFixOptions`. A checkout of HEAD would restore both files to `CSERVER`.

C09’s “worktree default is `cServer` / HEAD default is `CSERVER`” for `TargetCompId` remains true.

---

## 6. Adjacent (not `CSERVER`, not a mutate of tag 56)

| Location | What | CompID? |
|---|---|---|
| `src/Infrastructure/Dashboard/EfDashboardQueries.cs:130` | `Qualifier.ToString().ToUpperInvariant()` | No — session qualifier display |
| `src/Domain/Execution/ExecutionOrderStateMachine.cs:45` | `key.ToUpperInvariant()` on OrdStatus | No |

No product C# under `src` applies `ToUpper` / `ToUpperInvariant` / `ToLower` to `TargetCompId`, tag 56, or `CTRADER_FIX_*_TARGET_COMP_ID`.

---

## 7. What this grep does **not** prove

| Claim | Measured |
|---|---|
| Worktree `src` has zero exact `CSERVER` | **True** |
| Intended issued-form spelling `cServer` is on disk | **True** (9 literals) |
| HEAD `src` has zero `CSERVER` | **False** (7 literals) |
| Flip is committed | **False** (unstaged `M` on both files) |
| §26 defect closed | **No** — unbound env, no header builder, no ordinal unit lock test (same residual as B27 / C09) |
| Operator override `CSERVER` is forbidden | **No** — allowed only as **explicit, logged** `CTRADER_FIX_*_TARGET_COMP_ID`, never as a compiled default |

---

## 8. Answer to the assigned question

**Grep `CSERVER` under `D:\Prop\src`:**

- **Worktree:** **0** exact matches. After the (uncommitted) intended fix, the spelling is **`cServer`** (9 literals in `CTraderFixOptions.cs`, `FixSimulationHarness.cs`, `DemoSeeder.cs`).
- **HEAD:** **7** exact matches in `CTraderFixOptions.cs` (2) and `FixSimulationHarness.cs` (5).

Intended post-fix value **`cServer`** is already on disk. It is **not** what a clean checkout of HEAD would compile.

---

## 9. Sources

- `D:\Prop\src` (87 `*.cs` + other extensions walked; case-sensitive `CSERVER` = 0)
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` (HEAD blob `204f9d5` / WT blob `f2cd089`)
- `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs` (HEAD blob `433326b` / WT blob `19dd8a7`)
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (untracked; `cServer` only)
- `D:\Prop\src\Domain\Entities\FixSessionState.cs`
- `git grep` from `D:\Prop` on worktree + `HEAD -- src`
- Prior: `reports/swarm/20260818/B27_cserver_case.md`, `C09_cserver_fixed.md`

*End of C21. Product source was not modified.*
