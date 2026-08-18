# B27 — `cServer` vs `CSERVER` (architecture §26)

| Field | Value |
|---|---|
| Agent | B27 (senior engineer, header-case only) |
| Date | 2026-08-18 |
| Assigned | Read `CTraderFixOptions.cs`. Architecture says never silently change `cServer` to `CSERVER`. Confirm the bug. |
| Primary file | `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §25–§26 (lines 1023–1104) |
| Product source modified | **No.** This report is the only write. |
| Worktree SHA-256 | `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` |
| HEAD blob | `204f9d58a913022c31cdb4fa2eefef9d92916795` |
| Worktree blob | `f2cd089d29304a3e107dbc1e58957421a65296d6` |
| HEAD at measure | `406511a88405a3560b0e8f1842ef1f8529f58f2d` |

---

## 0. Verdict

**BUG CONFIRMED in committed HEAD.**  
**NOT a runtime `ToUpper` mutate.**  
**Working tree already contains an uncommitted default flip** (`CSERVER` → `cServer`). This agent did not make that edit.

| Surface | `TargetCompID` (tag 56) | vs §26 |
|---|---|---|
| Architecture §25 / §56 issued-form env sample | `cServer` | required default |
| Official RoE table + official examples | `CSERVER` | allowed only as **explicit, logged override** |
| `CTraderFixOptions` **HEAD** (`Quote` + `Trade`) | **`CSERVER`** | **FAIL** — default silently picked RoE spelling |
| `CTraderFixOptions` **worktree** (uncommitted) | `cServer` | default now matches issued form |
| Runtime case-fold (`ToUpper` / `ToUpperInvariant` on CompID) | **none in product C#** | no second mutate path |
| `DemoSeeder` / harness / `.env.example` / integration assert | `cServer` | consistent with issued form |
| Header builder / QuickFIX `SessionSettings` | **MISSING** | default would have been the wire value |

`CTraderFixOptions` is a pair of auto-properties. There is no setter, no binder, and no header factory. The defect is the **compiled default** in HEAD: both sessions ship `= "CSERVER"`, which is exactly the silent case change §26 forbids unless the issued configuration requires it. The Pepperstone form and architecture env sample require `cServer`.

Do **not** treat the uncommitted worktree flip as done. It is uncommitted, untested (`CTraderHeaderMappingTests` is listed in A89 as EXISTS and is **absent**), and does not bind `CTRADER_FIX_*_TARGET_COMP_ID`.

---

## 1. Binding law (quoted, not paraphrased)

Architecture §26, item 4 (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` line 1101):

> never silently change case such as `cServer` to `CSERVER` unless the issued configuration/spec requires it

Architecture §25 env sample (lines 1041, 1051):

```env
CTRADER_FIX_QUOTE_TARGET_COMP_ID=cServer
CTRADER_FIX_TRADE_TARGET_COMP_ID=cServer
```

Repo mirrors:

- `D:\Prop\docs\architecture.md` — “TargetCompID = `cServer` (case preserved)”
- `D:\Prop\docs\ctrader-fix.md` rule 3 — “Never rewrite `cServer` to `CSERVER`.”
- `D:\Prop\.env.example` lines 57 and 65 — same `cServer` values

A25 §3.3 (implementation spec, not product code): if Logon fails on `cServer`, try `CSERVER` only as an **explicit, logged override** (`CTRADER_FIX_*_TARGET_COMP_ID`), never as a hidden mutate.

Official RoE table (A32 / help.ctrader.com/fix/specification/): tag 56 valid value is `CSERVER`. Official send/receive samples use `56=CSERVER`. Official prose also says “usually it is cServer.” That conflict is **why** §26 exists. Picking one spelling in a compiled default is picking a side.

---

## 2. Measured `CTraderFixOptions.cs`

### 2.1 HEAD (committed — the bug)

`git show HEAD:src/Fix.CTrader/Configuration/CTraderFixOptions.cs` (blob `204f9d5`):

```csharp
public string TargetCompId { get; set; } = "CSERVER";   // QuoteFixOptions
public string TargetCompId { get; set; } = "CSERVER";   // TradeFixOptions
```

Both session option types. Same literal. No comment that this is an operator override. No env bind.

Introduced in `6c41447` (“Initial commit”, 2026-08-18 13:12:17 +0530) and still the HEAD content of those two lines.

### 2.2 Worktree (uncommitted — not this agent)

`git diff src/Fix.CTrader/Configuration/CTraderFixOptions.cs` at measure time:

```diff
-        public string TargetCompId { get; set; } = "CSERVER";
+        public string TargetCompId { get; set; } = "cServer";
```

twice (QUOTE line 49, TRADE line 70). `git blame` marks those two lines `Not Committed Yet` (2026-08-18 13:21:18). All other lines remain `6c41447`.

Worktree excerpt (current disk):

```49:51:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
        public string TargetCompId { get; set; } = "cServer";

        public string TargetSubId { get; set; } = "QUOTE";
```

```70:72:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
        public string TargetCompId { get; set; } = "cServer";

        public string TargetSubId { get; set; } = "TRADE";
```

Auto-properties. **No** getter rewrite. **No** `ToUpperInvariant()`. A later assign of `"cServer"` would stay `"cServer"`; a later assign of `"CSERVER"` would stay `"CSERVER"`. The HEAD default is the only fold.

### 2.3 Rest of the type (adjacent, not this bug)

| Property | Default | Note |
|---|---|---|
| `Host` | `live-us-eqx-01.p.c-trader.com` | live hostname compiled in |
| `Quote.SenderCompId` / `Trade.SenderCompId` | `live.pepperstone.1369850` | issued CompID, case preserved |
| `Quote.TargetSubId` / `Trade.TargetSubId` | `QUOTE` / `TRADE` | qualifier; independent of CompID case |
| `Quote.SenderSubId` / `Trade.SenderSubId` | `""` | §26 / RoE: QUOTE `SenderSubID` must be `QUOTE` when 57=`QUOTE` — **separate defect** |
| `RealCopyExecutionEnabled` | `false` | correct floor |
| `UseSsl` | `true` | correct floor |

`CTraderFixOptions` is **not** registered (`no Configure<CTraderFixOptions>`, no `IOptions<>`). `apps/fix-worker` reads `CTrader:RealCopyExecutionEnabled` only. The POCO default is unused **today** because there is no initiator. The day a session factory does `new CTraderFixOptions()`, HEAD would emit `56=CSERVER` unless config overwrote it — and there is no binder for `CTRADER_FIX_*_TARGET_COMP_ID`.

---

## 3. Repo-wide CompID case census (product, not reports)

`git grep CSERVER -- *.cs` at repo root: **zero hits** (worktree already flipped the only two C# literals).

| Location | Value | Role |
|---|---|---|
| `CTraderFixOptions` HEAD | `CSERVER` | **the bug** |
| `CTraderFixOptions` worktree | `cServer` | uncommitted correction |
| `Fix.CTrader/Testing/FixSimulationHarness.cs` defaults + tags 56 | `cServer` | test venue; preserves case |
| `Infrastructure/Seeding/DemoSeeder.cs` QUOTE + TRADE | `cServer` | persisted `FixSessionState` |
| `Domain/Entities/FixSessionState.TargetCompId` | `string.Empty` | no default fold |
| `tests/Integration/SeedingAndStoreTests.cs` | asserts exact `"cServer"` | ordinal FluentAssertions `.Equal` |
| `.env.example` | `cServer` | operator sample |
| `apps/web/src/pages/FixSessionsPage.tsx` | copy “stays `cServer`” | UI text only; DTO has **no** CompID field |
| `Infrastructure/Dashboard/EfDashboardQueries.cs` | `Qualifier.ToString().ToUpperInvariant()` | **qualifier only**, not tag 56 |
| `Domain/Execution/ExecutionOrderStateMachine.cs` | `ToUpperInvariant` on OrdStatus | **not** CompID |

No product C# applies `ToUpper` / `ToUpperInvariant` / `ToLower` to `TargetCompId`, tag 56, or `CTRADER_FIX_*_TARGET_COMP_ID`.

Dashboard `FixSessionDto` does **not** carry `TargetCompId`. The UI claim cannot drift from a live session header because the header is not shown.

---

## 4. Why this is a real Logon hazard (not style)

FIX CompIDs are compared as **opaque strings**. Acceptor case-sensitivity is not specified as “ignore case” in the RoE. Outcomes if HEAD default is sent against an acceptor that wants `cServer`:

| Acceptor behaviour | Symptom |
|---|---|
| Case-sensitive, wants issued `cServer` | Logon fail → Logout `35=5` + `Text` (58), or FAQ silent drop |
| Case-sensitive, wants RoE `CSERVER` | HEAD would Logon; issued-form `cServer` would fail |
| Case-insensitive | both work; still a §26 violation (silent pick) |

§26’s required path: persist the issued string, make tag 56 configurable, prove diagnostic Logon on **both** sessions, and only then allow an **explicit** `CSERVER` override. HEAD skipped that and hardcoded RoE.

Stale swarm notes that already named this default (now superseded **only** on the worktree, not on HEAD):

- `A49_feature_flags.md` §7.1 — “`TargetCompId` defaults to `"CSERVER"`”
- `A56_risk_list.md` §4.1 — product default **`CSERVER`** listed as P0
- `A57_first_useful_version.md` identity hazard 4 — same
- `A64_worker_pipelines.md` §13 — “stub currently defaults `CSERVER` — change that when implementing”
- `A75_env_example.md` — “`TargetCompId` default `CSERVER` (case-fold risk)”

Those reports measured HEAD correctly. They must not be treated as still true of the **worktree**.

---

## 5. What is **not** confirmed

| Hypothesis | Result |
|---|---|
| A live QuickFIX/header builder uppercases tag 56 | **No such builder exists** |
| `FixMessageParser` folds CompID case | **No.** Parser copies values; “Normalize” only trims `\|` |
| `FixSimulationHarness` rewrites `cServer` → `CSERVER` | **No.** Defaults and hard-coded 56 are `cServer` |
| Dashboard uppercases TargetCompID | **No.** It uppercases `FixSessionQualifier` for display |
| Seed / integration test disagree with issued form | **No.** Both are `cServer` |
| Worktree still has `CSERVER` | **No.** Uncommitted flip already applied |

A89 rows 62 / 72 (`CTraderHeaderMappingTests`, `CTraderFixOptionsSafetyDefaultsTests`) are marked EXISTS. `D:\Prop\tests\Unit\` has no `Fix/` folder and no class with those names. **The lock test is missing.** The only existing case assert is the integration seed check on `FixSessionState`, which never reads `CTraderFixOptions`.

---

## 6. Residual gaps (out of scope to fix here)

1. **HEAD still has the bug.** Worktree flip is not committed.
2. **No `IOptions<CTraderFixOptions>` bind** to `CTRADER_FIX_QUOTE_TARGET_COMP_ID` / `CTRADER_FIX_TRADE_TARGET_COMP_ID`. Flat env names will not bind to `CTraderFix:Quote:TargetCompId` without an explicit mapper (A49 / A75).
3. **No header builder** to prove tag 56 is the configured string on the wire (A25 §8 test 2).
4. **No unit test** that `new CTraderFixOptions().Quote.TargetCompId` equals `"cServer"` with `StringComparison.Ordinal` and is not rewritten after assign.
5. **QUOTE `SenderSubId` default empty** still violates RoE “must be `QUOTE` when TargetSubID=`QUOTE`”. Separate §26 defect; do not “fix” it by inferring tags from form labels.
6. Live host + live SenderCompID remain compiled defaults (A19 / A56 R22). Not this case bug.

---

## 7. Required behaviour when a later coding task touches this (not this file)

1. Default both session `TargetCompId` to issued-form **`cServer`** (worktree already does this).
2. Never `ToUpper` / `ToLower` CompIDs, SubIDs, or SenderCompID.
3. Bind `CTRADER_FIX_*_TARGET_COMP_ID` verbatim. An operator value of `CSERVER` is a **logged override**, not a library mutate.
4. Add `CTraderHeaderMappingTests`: default `cServer`; assign `"cServer"` stays `"cServer"`; assign `"CSERVER"` stays `"CSERVER"`; QUOTE/TRADE options are independent.
5. Diagnostic Logon record (A25 §3.6) must persist the **exact** tag 56 sent.
6. Do not “align with RoE” by editing the default back to `CSERVER`.

---

## 8. Sources

- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` (HEAD + worktree)
- `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs`
- `D:\Prop\src\Fix.CTrader\Parsing\FixMessageParser.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs` (no options bind)
- `D:\Prop\apps\fix-worker\Program.cs`, `Worker.cs`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\.env.example`
- `D:\Prop\docs\architecture.md`, `D:\Prop\docs\ctrader-fix.md`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§25–26, §56
- Prior notes: A25 §3, A49 §7.1, A56 §4.1, A57 #4, A64 §13, A75, A89 rows 62/72

*End of B27. Product source was not modified.*
