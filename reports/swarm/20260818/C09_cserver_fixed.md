# C09 — Is `TargetCompId` `cServer` now?

| Field | Value |
|---|---|
| Agent | C09 (senior engineer, default-case verify only) |
| Date | 2026-08-18 |
| Assigned | Read `CTraderFixOptions.cs`. Is `TargetCompId` `cServer` now? Write this report. Do not modify product source. |
| Primary file | `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §25–§26 |
| Product source modified | **No.** This report is the only write. |
| Worktree SHA-256 | `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` |
| HEAD blob | `204f9d58a913022c31cdb4fa2eefef9d92916795` |
| Worktree blob | `f2cd089d29304a3e107dbc1e58957421a65296d6` |
| HEAD at measure | `cacdb7085972f96b842d5b65775e024d0288fd9b` (`fix(docs): remove XML prolog from SVG for GitHub rendering`) |
| Prior measure | `B27_cserver_case.md` (HEAD then `406511a8`; same two blobs) |

---

## 0. Verdict

**Worktree: YES. Committed HEAD: NO. Not “fixed” as a committed product fact.**

Both `QuoteFixOptions.TargetCompId` and `TradeFixOptions.TargetCompId` on **disk** default to the issued-form literal `"cServer"`. The same two properties in **HEAD** still default to `"CSERVER"`. `git status` is ` M src/Fix.CTrader/Configuration/CTraderFixOptions.cs` (unstaged). `git blame` on lines 49 and 70 is `Not Committed Yet` (2026-08-18 13:23:28 +0530). This agent did not make or revert that edit.

| Surface | `TargetCompID` (tag 56) | vs §26 |
|---|---|---|
| Architecture §25 / §56 env sample | `cServer` | required issued-form default |
| Official RoE table + official examples | `CSERVER` | allowed only as **explicit, logged override** |
| `CTraderFixOptions` **HEAD** (`Quote` + `Trade`) | **`CSERVER`** | **still FAIL** |
| `CTraderFixOptions` **worktree** | `cServer` | default matches issued form |
| Runtime case-fold (`ToUpper` / `ToUpperInvariant` on CompID) | **none in product C#** | no second mutate path |
| `DemoSeeder` / harness / `.env.example` / integration assert | `cServer` | consistent with issued form |

Do **not** treat the filename `C09_cserver_fixed` as a PASS. The compiled default that would ship from HEAD is still the silent RoE spelling §26 forbids.

---

## 1. Binding law (quoted)

Architecture §26, item 4 (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`):

> never silently change case such as `cServer` to `CSERVER` unless the issued configuration/spec requires it

Architecture §25 env sample:

```env
CTRADER_FIX_QUOTE_TARGET_COMP_ID=cServer
CTRADER_FIX_TRADE_TARGET_COMP_ID=cServer
```

Repo mirrors:

- `D:\Prop\docs\architecture.md` — “TargetCompID = `cServer` (case preserved)”
- `D:\Prop\docs\ctrader-fix.md` rule 3 — “Never rewrite `cServer` to `CSERVER`.”
- `D:\Prop\.env.example` lines 57 and 65 — same `cServer` values

---

## 2. Measured `CTraderFixOptions.cs`

### 2.1 Worktree (current disk — the question “now”)

`D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` lines 41–78:

```csharp
public sealed class QuoteFixOptions
{
    public int SslPort { get; set; } = 5211;
    public int PlainPort { get; set; } = 5201;
    public string SenderCompId { get; set; } = "live.pepperstone.1369850";
    public string TargetCompId { get; set; } = "cServer";
    public string TargetSubId { get; set; } = "QUOTE";
    public string SenderSubId { get; set; } = string.Empty;
}

public sealed class TradeFixOptions
{
    public int SslPort { get; set; } = 5212;
    public int PlainPort { get; set; } = 5202;
    public string SenderCompId { get; set; } = "live.pepperstone.1369850";
    public string TargetCompId { get; set; } = "cServer";
    public string TargetSubId { get; set; } = "TRADE";
    public string SenderSubId { get; set; } = string.Empty;
}
```

Auto-properties. No getter rewrite. No `ToUpperInvariant()`. Assign `"cServer"` stays `"cServer"`; assign `"CSERVER"` stays `"CSERVER"`.

### 2.2 HEAD (committed — still the bug)

`git show HEAD:src/Fix.CTrader/Configuration/CTraderFixOptions.cs` (blob `204f9d5`):

```csharp
public string TargetCompId { get; set; } = "CSERVER";   // QuoteFixOptions
public string TargetCompId { get; set; } = "CSERVER";   // TradeFixOptions
```

Introduced in `6c41447` (“Initial commit”, 2026-08-18 13:12:17 +0530). That is still the only commit that touches this file. HEAD moved since B27 (`406511a8` → `cacdb70`) but **this blob did not**.

### 2.3 Diff (unstaged, not this agent)

```diff
-        public string TargetCompId { get; set; } = "CSERVER";
+        public string TargetCompId { get; set; } = "cServer";
```

twice (QUOTE line 49, TRADE line 70). Staged diff is empty.

Same worktree SHA-256 / blob pair B27 recorded. The flip is older than this agent and still uncommitted.

---

## 3. Repo-wide CompID census (product C#, this measure)

`git grep CSERVER -- *.cs` at `D:\Prop`: **zero hits** (worktree already flipped the only two C# `CSERVER` literals).

| Location | Value | Role |
|---|---|---|
| `CTraderFixOptions` HEAD | `CSERVER` | **still the committed bug** |
| `CTraderFixOptions` worktree | `cServer` | uncommitted correction |
| `Fix.CTrader/Testing/FixSimulationHarness.cs` defaults + tags 56 | `cServer` | test venue; preserves case |
| `Infrastructure/Seeding/DemoSeeder.cs` QUOTE + TRADE | `cServer` | persisted `FixSessionState` |
| `Domain/Entities/FixSessionState.TargetCompId` | `string.Empty` | no default fold |
| `tests/Integration/SeedingAndStoreTests.cs` | asserts exact `"cServer"` | ordinal FluentAssertions `.Equal` |
| `.env.example` | `cServer` | operator sample |
| `Infrastructure/Dashboard/EfDashboardQueries.cs` | `Qualifier.ToString().ToUpperInvariant()` | **qualifier only**, not tag 56 |
| `Domain/Execution/ExecutionOrderStateMachine.cs` | `ToUpperInvariant` on OrdStatus | **not** CompID |

No product C# applies `ToUpper` / `ToUpperInvariant` / `ToLower` to `TargetCompId`, tag 56, or `CTRADER_FIX_*_TARGET_COMP_ID`.

---

## 4. What “fixed” still is **not**

| Claim | Measured |
|---|---|
| HEAD default is `cServer` | **False.** HEAD is `CSERVER`. |
| Worktree default is `cServer` | **True.** Both session option types. |
| Flip is committed | **False.** `git status` = ` M` that one file. |
| `IOptions<CTraderFixOptions>` binds `CTRADER_FIX_*_TARGET_COMP_ID` | **Not present** (same as B27). |
| Header builder / QuickFIX `SessionSettings` emits tag 56 | **MISSING.** Default is unused until an initiator exists. |
| `CTraderHeaderMappingTests` / `CTraderFixOptionsSafetyDefaultsTests` | **ABSENT** under `D:\Prop\tests\Unit\`. Seed test never reads `CTraderFixOptions`. |
| Live Logon proved on both spellings | **Not done.** |

If a later process does `new CTraderFixOptions()` from **HEAD**, tag 56 would be `CSERVER` unless config overwrote it — and there is still no binder for the env names in `.env.example`.

---

## 5. Answer to the assigned question

**Is `TargetCompId` `cServer` now?**

- **On the file you asked to read (worktree):** **yes** — `QuoteFixOptions` line 49 and `TradeFixOptions` line 70 are both `= "cServer"`.
- **On the last committed tree:** **no** — both are still `= "CSERVER"`.
- **As a closed §26 defect:** **no** — uncommitted, unbound, untested, no wire header.

---

## 6. Sources

- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` (HEAD + worktree)
- `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\.env.example`
- `D:\Prop\docs\architecture.md`, `D:\Prop\docs\ctrader-fix.md`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§25–26
- Prior: `reports/swarm/20260818/B27_cserver_case.md`

*End of C09. Product source was not modified.*
