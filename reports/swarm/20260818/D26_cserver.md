# D26 — Confirm `TargetCompId` default is `cServer`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D26_cserver.md` |
| Agent | D26 (senior engineer, `cServer` recensus only) |
| Date | 2026-08-18 13:34:27 +05:30 |
| Assigned | Read `CTraderFixOptions.cs`. Confirm `cServer`. Write this file. Do not modify product source. |
| Primary file | `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §25–§26 (lines 1023–1104) |
| Product source modified | **No.** This report is the only write. |
| Measure HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`, 2026-08-18 13:24:21 +0530) |
| Worktree SHA-256 (`CTraderFixOptions.cs`) | `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` |
| HEAD blob | `204f9d58a913022c31cdb4fa2eefef9d92916795` |
| Index blob | `204f9d58a913022c31cdb4fa2eefef9d92916795` (equals HEAD; **not staged**) |
| Worktree blob | `f2cd089d29304a3e107dbc1e58957421a65296d6` |
| Prior | `B27_cserver_case.md`, `C09_cserver_fixed.md`, `C21_cserver_grep.md` |

---

## 0. Verdict

**Worktree `CTraderFixOptions`: CONFIRMED `cServer`.**  
**Committed HEAD: still `CSERVER`. Not a closed §26 fact.**

Both `QuoteFixOptions.TargetCompId` (line 49) and `TradeFixOptions.TargetCompId` (line 70) on **disk** default to the issued-form literal `"cServer"`. The same two properties in **HEAD** still default to `"CSERVER"`. `git status` is ` M src/Fix.CTrader/Configuration/CTraderFixOptions.cs` (unstaged). `git blame` on lines 49 and 70 is `Not Committed Yet` (2026-08-18 13:33:59 +0530). This agent did not make or revert that edit.

| Surface | Tag 56 / `TargetCompID` | vs §26 |
|---|---|---|
| Architecture §25 / §56 env sample | `cServer` | required issued-form default |
| Official RoE table + official examples | `CSERVER` | allowed only as **explicit, logged override** |
| `CTraderFixOptions` **HEAD** (`Quote` + `Trade`) | **`CSERVER`** | **FAIL** — silent RoE spelling |
| `CTraderFixOptions` **worktree** (this read) | **`cServer`** | **PASS** — matches issued form |
| Runtime case-fold (`ToUpper` / `ToUpperInvariant` on CompID) | **none in product C#** | no second mutate path |
| `DemoSeeder` / harness / `.env.example` / integration seed assert | `cServer` | consistent with issued form |

Do **not** treat this filename as “fixed in git.” A clean checkout of HEAD still compiles `56=CSERVER`.

---

## 1. Binding law (quoted)

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

Official RoE (A32 / help.ctrader.com/fix/specification/) lists tag 56 valid value `CSERVER`. Official send/receive samples use `56=CSERVER`. Official prose also says “usually it is cServer.” That conflict is **why** §26 exists. The issued Pepperstone form and architecture env sample require `cServer`. `CSERVER` is legal only as an **operator override**, never as a compiled default.

---

## 2. Measured `CTraderFixOptions.cs` (this read)

File read in full: 80 lines. Namespace `TraderIntelligence.Fix.CTrader.Configuration`. Auto-properties only. No getter rewrite. No `ToUpper` / `ToLower`. No binder. No header factory.

### 2.1 Worktree (current disk — the confirmation target)

```41:78:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    public sealed class QuoteFixOptions
    {
        public int SslPort { get; set; } = 5211;

        public int PlainPort { get; set; } = 5201;

        public string SenderCompId { get; set; } = "live.pepperstone.1369850";

        public string TargetCompId { get; set; } = "cServer";

        public string TargetSubId { get; set; } = "QUOTE";

        /// <summary>
        /// SenderSubID for QUOTE session (configurable).
        /// </summary>
        public string SenderSubId { get; set; } = string.Empty;
    }

    public sealed class TradeFixOptions
    {
        public int SslPort { get; set; } = 5212;

        public int PlainPort { get; set; } = 5202;

        /// <summary>
        /// cTrader FIX gateway SenderCompID (configurable).
        /// </summary>
        public string SenderCompId { get; set; } = "live.pepperstone.1369850";

        public string TargetCompId { get; set; } = "cServer";

        public string TargetSubId { get; set; } = "TRADE";

        /// <summary>
        /// SenderSubID for TRADE session (configurable).
        /// </summary>
        public string SenderSubId { get; set; } = string.Empty;
    }
```

**Confirmed:** both session option types default to ordinal `"cServer"`. Assign `"cServer"` stays `"cServer"`; assign `"CSERVER"` stays `"CSERVER"`. There is no silent fold in this type.

### 2.2 HEAD (committed — still the bug)

`git show HEAD:src/Fix.CTrader/Configuration/CTraderFixOptions.cs` (blob `204f9d5`):

```csharp
public string TargetCompId { get; set; } = "CSERVER";   // QuoteFixOptions line 49
public string TargetCompId { get; set; } = "CSERVER";   // TradeFixOptions line 70
```

Introduced in `6c41447` (“Initial commit”, 2026-08-18 13:12:17 +0530). That is still the only commit that touches this file. HEAD moved since B27 (`406511a8` → `398a1420`) but **this blob did not**.

### 2.3 Unstaged diff (not this agent)

```diff
-        public string TargetCompId { get; set; } = "CSERVER";
+        public string TargetCompId { get; set; } = "cServer";
```

twice (QUOTE line 49, TRADE line 70). Staged diff is empty. Index still equals HEAD.

Same worktree SHA-256 / blob pair B27 / C09 / C21 recorded.

### 2.4 Adjacent defaults (not the case bug)

| Property | Default | Note |
|---|---|---|
| `Host` | `live-us-eqx-01.p.c-trader.com` | live hostname compiled in |
| `Quote.SenderCompId` / `Trade.SenderCompId` | `live.pepperstone.1369850` | issued CompID, case preserved |
| `Quote.TargetSubId` / `Trade.TargetSubId` | `QUOTE` / `TRADE` | qualifier; independent of CompID case |
| `Quote.SenderSubId` / `Trade.SenderSubId` | `""` | §26 / RoE: QUOTE `SenderSubID` must be `QUOTE` when 57=`QUOTE` — **separate defect** |
| `UseSsl` | `true` | correct floor |
| `RealCopyExecutionEnabled` | `false` | correct floor |
| `HeartbeatIntervalSec` | `30` | |
| `MaxQuoteAgeMs` | `5000` | |

`CTraderFixOptions` is **not** registered: no `Configure<CTraderFixOptions>`, no `IOptions<CTraderFixOptions>`. `git grep CTraderFixOptions` under `apps` / `src` / `tests` hits **only this file**. `apps/fix-worker` reads `CTrader:RealCopyExecutionEnabled` only (`Worker.cs` line 21). The POCO default is unused **today** because there is no initiator. The day a session factory does `new CTraderFixOptions()`, HEAD would emit `56=CSERVER` unless config overwrote it — and there is no binder for `CTRADER_FIX_*_TARGET_COMP_ID`.

---

## 3. Repo-wide CompID census (this measure)

From `D:\Prop` (repo root):

```text
git grep -n -I --untracked "CSERVER" -- src     → 0 lines (worktree)
git grep -n -I "CSERVER" HEAD -- src            → 7 lines / 2 files
git grep -n -I --untracked "cServer" -- src     → 9 lines / 3 files
```

`git grep CSERVER -- *.cs` on the worktree: **zero hits**.

| Location | Value | Role |
|---|---|---|
| `CTraderFixOptions` HEAD | `CSERVER` | **still the committed bug** (2 literals) |
| `CTraderFixOptions` worktree | `cServer` | **confirmed this read** (lines 49, 70) |
| `FixSimulationHarness.cs` HEAD | `CSERVER` | 5 literals (defaults + tag 56) |
| `FixSimulationHarness.cs` worktree | `cServer` | unstaged flip; same 5 sites |
| `DemoSeeder.cs` QUOTE + TRADE | `cServer` | untracked seed of `FixSessionState` |
| `FixSessionState.TargetCompId` | `string.Empty` | no default fold |
| `tests/Integration/SeedingAndStoreTests.cs` | asserts exact `"cServer"` | ordinal FluentAssertions `.Equal` |
| `.env.example` | `cServer` | operator sample |
| `EfDashboardQueries.cs` | `Qualifier.ToString().ToUpperInvariant()` | **qualifier only**, not tag 56 |
| `ExecutionOrderStateMachine.cs` | `ToUpperInvariant` on OrdStatus | **not** CompID |

Worktree `cServer` hits under `src` (9):

| File | Line | Literal |
|---|---:|---|
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | 49 | `= "cServer"` |
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | 70 | `= "cServer"` |
| `src/Fix.CTrader/Testing/FixSimulationHarness.cs` | 16 | default `"cServer"` |
| `src/Fix.CTrader/Testing/FixSimulationHarness.cs` | 30 | default `"cServer"` |
| `src/Fix.CTrader/Testing/FixSimulationHarness.cs` | 129 | default `"cServer"` |
| `src/Fix.CTrader/Testing/FixSimulationHarness.cs` | 155 | `(56, "cServer")` |
| `src/Fix.CTrader/Testing/FixSimulationHarness.cs` | 187 | `(56, "cServer")` |
| `src/Infrastructure/Seeding/DemoSeeder.cs` | 77 | `TargetCompId = "cServer"` |
| `src/Infrastructure/Seeding/DemoSeeder.cs` | 94 | `TargetCompId = "cServer"` |

HEAD `CSERVER` under `src` remains the C21 set of **7** (options 2 + harness 5). Harness worktree blob `19dd8a7` / HEAD blob `433326b` / SHA-256 `99A28D8F3F49028706C75B9C4DC46B4CCB3FF98E90AAED3B4B874DD1B4351616`.

No product C# applies `ToUpper` / `ToUpperInvariant` / `ToLower` to `TargetCompId`, tag 56, or `CTRADER_FIX_*_TARGET_COMP_ID`.

---

## 4. What “confirmed `cServer`” still is **not**

| Claim | Measured 2026-08-18 13:34 +05:30 |
|---|---|
| Worktree `CTraderFixOptions` default is `cServer` | **True.** Both session types. |
| HEAD default is `cServer` | **False.** HEAD is `CSERVER`. |
| Flip is committed | **False.** `git status` = unstaged `M` on options + harness. |
| `IOptions<CTraderFixOptions>` binds `CTRADER_FIX_*_TARGET_COMP_ID` | **Not present.** |
| Header builder / QuickFIX `SessionSettings` emits tag 56 | **MISSING.** Default unused until an initiator exists. |
| `CTraderHeaderMappingTests` / `CTraderFixOptionsSafetyDefaultsTests` | **ABSENT.** A89 rows 62/72 say EXISTS. `tests/Unit` has no `Fix/` folder and no class with those names. Seed test never reads `CTraderFixOptions`. |
| Live Logon proved on both spellings | **Not done.** |
| §26 defect closed | **No.** |

If a later process does `new CTraderFixOptions()` from **HEAD**, tag 56 would be `CSERVER` unless config overwrote it — and there is still no binder for the env names in `.env.example`.

---

## 5. Answer to the assigned question

**Confirm `cServer`?**

- **On the file you asked to read (worktree `CTraderFixOptions.cs`):** **yes** — `QuoteFixOptions` line 49 and `TradeFixOptions` line 70 are both `= "cServer"`.
- **On the last committed tree:** **no** — both are still `= "CSERVER"`.
- **As a closed §26 defect:** **no** — uncommitted, unbound, untested, no wire header.

Same conclusion as C09 / C21. Blobs unchanged. HEAD still `398a1420`. This is a recensus, not a new fix.

---

## 6. Residual (out of scope to fix here)

1. Commit the worktree flip (`CTraderFixOptions` + `FixSimulationHarness`) or it dies on the next clean checkout.
2. Bind `CTRADER_FIX_QUOTE_TARGET_COMP_ID` / `CTRADER_FIX_TRADE_TARGET_COMP_ID` **verbatim**. An operator value of `CSERVER` is a logged override, not a library mutate. Flat env names will not bind to `CTraderFix:Quote:TargetCompId` without an explicit mapper.
3. Add `CTraderHeaderMappingTests`: default `cServer`; assign `"cServer"` stays `"cServer"`; assign `"CSERVER"` stays `"CSERVER"`; QUOTE/TRADE options are independent (`StringComparison.Ordinal`).
4. Diagnostic Logon must persist the **exact** tag 56 sent.
5. Do **not** “align with RoE” by editing the default back to `CSERVER`.
6. QUOTE `SenderSubId` default empty is a **separate** §26 defect. Do not “fix” it by inferring tags from form labels.
7. Live host + live SenderCompID remain compiled defaults. Not this case bug.

---

## 7. Sources

- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` (HEAD blob `204f9d5` / WT blob `f2cd089`; file read in full)
- `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs` (no options bind)
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs`
- `D:\Prop\src\Domain\Entities\FixSessionState.cs`
- `D:\Prop\apps\fix-worker\Program.cs`, `Worker.cs`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\tests\Unit\` (no Fix header-mapping test)
- `D:\Prop\.env.example`
- `D:\Prop\docs\architecture.md`, `D:\Prop\docs\ctrader-fix.md`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§25–26
- Commands: `git grep` / `git hash-object` / `git blame` / `Get-FileHash` from `D:\Prop`
- Prior: `reports/swarm/20260818/B27_cserver_case.md`, `C09_cserver_fixed.md`, `C21_cserver_grep.md`

*End of D26. Product source was not modified.*
