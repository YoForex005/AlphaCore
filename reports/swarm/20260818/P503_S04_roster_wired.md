# P503_S04 — Copy roster is wired (auto-admit / auto-remove / dest flatten only)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P503_S04_roster_wired.md` |
| Slot | P503_S04 |
| Date | 2026-08-18 |
| Product source modified | **No.** Report only. |
| Secrets printed | **No.** No passwords, tokens, `.env` values, or broker credentials. |
| Method | Full read of `CopyRosterEngine.cs`, `CopyTradingService.TickRosterAsync` + `FlattenOpenCopiesAsync`, `XauUsdOneToOneCopyPolicy`, `CopyTradingHostedService`, `CopyIntent`, `IMt5BrokerConnector`, `CopyRosterEngineTests`. Grep for `TickRosterAsync` / `ShouldFlattenOpenCopy` / `IMt5` write paths. |

**Honesty:** “wired” means the hosted 20s tick **calls** `TickRosterAsync`, which **calls** `CopyRosterEngine.Decide` and **persists** `ADMITTED` / `REMOVED:*` rows plus dest-only flatten intents. It does **not** mean dest venue flatten executes, and it does **not** mean live copy is armed.

---

## 0. Verdict

| Claim | Measured | Class |
|---|---|---|
| Auto-admit eligible XAU traders | **YES** — `Decide` → `RosterAction.Admit` / `AUTO_ADMIT`; `TickRosterAsync` writes `CopyIntent.Status = "ADMITTED"` | **WIRED** |
| Remove on XAU book ≤ 0 | **YES** — `XAU_BOOK_TURNED_NEGATIVE` if already on roster | **WIRED** |
| Remove on consecutive XAU losses | **YES** — default 3 (`CONSECUTIVE_LOSSES_{n}`) | **WIRED** |
| Remove on drawdown vs peak | **YES** — default 40% (`DRAWDOWN_FROM_PEAK`) | **WIRED** |
| Remove on martingale / averaging / lot escalation | **YES** — `SIZE_PATTERN` | **WIRED** |
| Remove on demo / contest group | **YES** — `demo\` / `contest\` prefix (`DEMO_OR_CONTEST_GROUP`) | **WIRED** |
| Flatten destination opens only | **YES** — `FlattenOpenCopiesAsync` inserts dest `CloseExposure` intents with `copy:` keys | **INTENT-ONLY** |
| Never touch MT5 source book | **YES** — no connector, no `SendTrade`, `IMt5BrokerConnector` is Get-only | **CONFIRMED** |
| Per-copy unrealized-loss flatten (`ShouldFlattenOpenCopy`) | **NOT WIRED** — engine method exists; service never calls it | **HOLE** |

One-line:

```text
ROSTER ENGINE + TickRosterAsync WIRED (admit / remove / dest flatten intents).
FLATTEN = dest CopyIntent rows only. NOS unimplemented. NEVER MT5 SOURCE.
ShouldFlattenOpenCopy = UNWIRED. No TickRoster integration tests.
```

---

## 1. Call graph (measured)

```
CopyTradingHostedService.ExecuteAsync   (DI: AddHostedService)
  delay 8s, then every 20s
  → CopyTradingService.TickRosterAsync
       loads ALL TraderScores
       builds CopyTraderSnapshot + completed XAU ReconstructedTradeResult
       roster row key = "roster:{BrokerId}:{Login}"
       onRoster = row exists AND Status == "ADMITTED"
       → CopyRosterEngine.Decide(snapshot, completed, onRoster)
       Admit  → upsert CopyIntent Status=ADMITTED, Action=OpenExposure
       Remove → upsert Status="REMOVED:"+reason, Action=CloseExposure
                if FlattenDestination → FlattenOpenCopiesAsync (dest intents)
       Keep   → no persist
  → CopyTradingService.GenerateShadowIntentsAsync
       skips unless roster Status == "ADMITTED"
```

Hosted log (explicit dest-only):

> `Copy roster changes={Roster} new intents={Intents}. Dest flatten is SHADOW/intent only; NewOrderSingle still unimplemented.`

Constants on the service (fail-closed for live send):

| Constant | Value |
|---|---|
| `NewOrderSingleImplemented` | `false` |
| `VenueReconciled` | `false` |

`IMt5BrokerConnector` members used by the **collector** (not by copy): `Connect` / `Disconnect` / `IsConnected` / `GetGroups` / `GetAccounts` / `GetDeals` / `GetPositions`. **No** `SendTrade`, `DealerSend`, `PositionClose`, `DealAdd`. `CopyTradingService` does not take `IMt5BrokerConnector`.

---

## 2. Auto-admit

Domain (`CopyRosterEngine.Decide`):

1. Hard reject first (state / size pattern / demo) → `RemoveAndFlatten` even if not yet admitted.
2. If already on roster: negative book / streak ≥ 3 / DD ≥ 40% of peak → `RemoveAndFlatten`.
3. Else `XauUsdOneToOneCopyPolicy.IsTraderEligible`:
   - **eligible + not on roster** → `Admit`, `AUTO_ADMIT`, `FlattenDestination=false`, `AllowNewOpens=true`
   - **eligible + on roster** → `Keep`, `KEEP`
   - **not eligible + on roster** → `RemoveAndFlatten`, `NO_LONGER_ELIGIBLE_{reason}`
   - **not eligible + not on roster** → `Keep` (misnamed), `NOT_YET_{reason}`, `AllowNewOpens=false`

Eligibility floors (policy):

| Gate | Rule |
|---|---|
| State | Blocked / Disqualified / Paused → no. Insufficient / Early / Watch → `TRADER_NOT_SHADOW_YET`. Need SHADOW / LIVE_CANDIDATE / LIVE (implicit: not those blocked sets). |
| Size pattern | Martingale **or** AveragingDown **or** LotEscalation → block |
| History | `CompletedXauTrades >= 20` (`MinCompletedXauTrades`) |
| Book | `XauNetPnl > 0` |
| Group | reject `demo\` / `contest\` (case-insensitive prefix) |

Service persist on Admit:

- New row: `IdempotencyKey = roster:{broker}:{login}`, `Status = ADMITTED`, `CanonicalSymbol = XAUUSD`, `Action = OpenExposure`, `ExpiresAt = now+20y`.
- Existing row (e.g. prior `REMOVED:*`): overwrite `Status = ADMITTED`, bump `CreatedAt`. **No cooldown.**

Unit proof: `CopyRosterEngineTests.New_eligible_trader_is_auto_admitted`.

---

## 3. Auto-remove (negative / streak / DD / martingale / demo)

| Trigger | When | Reason | Flatten dest? | On-roster required? |
|---|---|---|---|---|
| `RISK_BLOCKED` / `DISQUALIFIED` / `PAUSED` | always | `STATE_{state}` | yes | no |
| Martingale / AveragingDown / LotEscalation | always | `SIZE_PATTERN` | yes | no |
| Group `demo\` or `contest\` | always | `DEMO_OR_CONTEST_GROUP` | yes | no |
| Completed XAU net ≤ 0 | already on roster | `XAU_BOOK_TURNED_NEGATIVE` | yes | **yes** |
| Trailing consecutive losses ≥ 3 | already on roster | `CONSECUTIVE_LOSSES_{n}` | yes | **yes** |
| `(peak-equity)/peak ≥ 0.40` and `peak > 0` | already on roster | `DRAWDOWN_FROM_PEAK` | yes | **yes** |
| Policy no longer eligible | already on roster | `NO_LONGER_ELIGIBLE_{reason}` | yes | **yes** |

`Remove(...)` always sets `Action = RemoveAndFlatten`, `FlattenDestination = true`, `AllowNewOpens = false`.

Service on `RemoveAndFlatten`:

- Upsert roster row `Status = "REMOVED:" + reason`, `Action = CloseExposure`.
- If `decision.FlattenDestination` (always true for this action) → `FlattenOpenCopiesAsync`.

Unit proof: book-negative, 3-loss streak, martingale, demo, peak DD facts in `CopyRosterEngineTests`.

---

## 4. Flatten = destination opens only; never MT5 source

`FlattenOpenCopiesAsync(brokerId, login, now, ct)`:

1. Selects **our** `CopyIntents` where `BrokerId` + `SourceLogin` match, `Action == OpenExposure`, and `IdempotencyKey.StartsWith("copy:")`.
2. Roster membership rows use `roster:` — **excluded**.
3. For each dest open, if `copy:{broker}:{login}:{SourcePositionId}:close` is absent, insert a **destination** `CopyIntent`:
   - `Action = CloseExposure`
   - `Status = "FLATTEN_LOSS_CUT"`
   - `OrdType = Market`
   - `ExpiresAt = now+15s`
   - qty/price copied from the dest open row (entry `ExpectedPrice`, not a live dest quote)

What it does **not** do:

- No `IMt5BrokerConnector` / native Manager / HTTP dealer call.
- No source `GetPositions` then close.
- No `ExecutionIntent` writer.
- No FIX `35=D` / `NewOrderSingle`.
- No mutation of `ReconstructedTrades` / source deals.

Domain comment (engine L29–31) matches the implementation: *“Flatten is destination-only. Never touches the MT5 source book.”*

Capital risk today: **NONE** (`SAFE_BY_ABSENCE` — dest flatten is a DB row; live send still unimplemented).

---

## 5. Hosted + shadow hopper coupling

`GenerateShadowIntentsAsync` **refuses** new dest opens unless a roster row exists with `Status == "ADMITTED"`. That is the live gate between scoring and copy.

Copyable states for **signals** (separate from roster Decide): `{SHADOW, LIVE_CANDIDATE, LIVE}`. A WATCH name can be auto-admitted only after score state + eligibility pass; Watch-not-admitted is unit-tested.

---

## 6. Holes / residual (do not greenwash)

### 6.1 Behavioral holes

| # | Hole | Evidence | Severity |
|---|---|---|---|
| H1 | `ShouldFlattenOpenCopy(unrealizedPnl)` **never called** by `TickRosterAsync` / flatten / shadow | Grep: only engine + one unit fact | **HIGH** vs advertised $150 dest-loss cut |
| H2 | Flatten is **intent-only**; dest book is not closed on venue | `NewOrderSingleImplemented=false`; no ExecutionIntent writers | **EXPECTED** until live send exists; do not claim dest flatten works |
| H3 | `FLATTEN_LOSS_CUT` used for **all** remove reasons (state, demo, streak, DD) | `FlattenOpenCopiesAsync` hardcodes that Status | MED — telemetry lie |
| H4 | Re-admit has **no cooldown / no lockout** after `REMOVED:*` | Admit path overwrites same `roster:` row | MED — oscillate on net≈0 |
| H5 | Negative / streak / DD only apply **if already on roster** | Off-roster losers stay `NOT_YET_*` / `Keep` | LOW (correct for admit) |
| H6 | Demo/contest detect is **prefix only** (`demo\` / `contest\`) | `demo-` / `real\demo` / null `GroupName` slip through | MED if account row missing |
| H7 | Tick roster XAU query is `CanonicalSymbol == "XAUUSD"` only | Policy also treats GOLD / XAUUSD.PRO / etc. | MED — GOLD tape ignored for book/streak/DD |
| H8 | Snapshot `XauNetPnl` is computed but Decide **recomputes** from `completedXau` | Two sources can diverge from `score.CompletedXauTrades` / score PnL | LOW |
| H9 | `RosterLimits.MinCompletedXauToAdmit` is **dead** | Never read; policy const 20 wins | LOW |
| H10 | `Keep` + `NOT_YET_*` for never-admitted ineligible names | Action name implies membership | LOW (naming) |
| H11 | Flatten skips if `:close` already exists | Source-close intent and dest flatten share the same key — cannot flatten a dest that already has a source-close row, even if dest still “open” conceptually | MED |
| H12 | Flatten does **not** filter dest `Status` | Will emit close for `PENDING_RISK` / `SHADOW_ONLY` / even the roster-adjacent opens that match `copy:` | LOW |
| H13 | No dest unrealized PnL / no dest position table | Cannot implement H1 honestly without a dest book | HIGH for live |
| H14 | Drawdown / streak use **completed** XAU only | Open source losers do not move roster until reconstruct completes | MED |
| H15 | N+1: per-score account + trades + roster lookup | `TickRosterAsync` loops every `TraderScore` | OPS — will hurt at catalog scale |
| H16 | Roster membership stored as `CopyIntent` | Mixes membership with trade intents; `ListIntentsAsync` will show ADMITTED/REMOVED as if they were orders | MED product |
| H17 | `AllowNewOpens` is **ignored** by the service | Engine flag unused; hopper uses ADMITTED status only | LOW |
| H18 | AveragingDown / LotEscalation share `SIZE_PATTERN` | Fine for remove; no distinct reason | LOW |

### 6.2 Test holes

| # | Missing |
|---|---|
| T1 | **Zero** `TickRosterAsync` / `FlattenOpenCopiesAsync` tests (unit or integration). Only domain `CopyRosterEngineTests`. |
| T2 | No test that flatten writes `copy:` closes and **never** calls MT5. |
| T3 | No test for AveragingDown, LotEscalation, PAUSED, DISQUALIFIED, `NO_LONGER_ELIGIBLE`, Keep no-op, re-admit after REMOVED. |
| T4 | `ShouldFlattenOpenCopy` tested in isolation — **false confidence** that dest $150 cut is live. |
| T5 | Streak test depends on `OpenedAt = now.AddDays(-id)` so small `id` is newest after `OrderBy(ClosedAt)`. Fragile. |

### 6.3 Safety that **is** in place

- Source MT5 book: **untouched** (read-only connector surface; copy service has no connector).
- Live dest send: **blocked** (`NOS` const false, `AllowFixSend` forced false on shadow path).
- Demo / contest / martingale: engine removes **before** eligibility; hopper also re-checks `IsTraderEligible`.
- Flatten filter `copy:` prevents closing the `roster:` membership row as if it were a position.

---

## 7. Confirm / deny checklist (assigned)

| Assigned requirement | Confirm? |
|---|---|
| Auto-admit | **CONFIRMED** (engine + persist + hosted tick + hopper gate) |
| Remove on negative | **CONFIRMED** (on-roster only) |
| Remove on streak | **CONFIRMED** (default 3 completed XAU losses, newest-last) |
| Remove on DD | **CONFIRMED** (default 40% from completed-XAU equity peak) |
| Remove on martingale | **CONFIRMED** (`SIZE_PATTERN`; also averaging / lot escalation) |
| Remove on demo | **CONFIRMED** (`demo\` / `contest\` group prefix) |
| Flatten dest opens only | **CONFIRMED** (dest `copy:` CloseExposure intents only) |
| Never MT5 source | **CONFIRMED** |
| Wired end-to-end for **venue** flatten | **DENIED** — intent rows only; NOS absent |
| Per-open $150 dest loss cut | **DENIED** — `ShouldFlattenOpenCopy` unwired |

---

## 8. Files (absolute)

| Path | Role |
|---|---|
| `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs` | Decide + unused `ShouldFlattenOpenCopy` |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | Eligibility + 1:1 signal policy |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | `TickRosterAsync` L94–180; `FlattenOpenCopiesAsync` L398–432 |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 20s tick |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | `AddScoped<CopyTradingService>`; `AddHostedService<CopyTradingHostedService>` |
| `D:\Prop\src\Domain\Entities\CopyIntent.cs` | Shared row for roster + dest intents |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | Get-only `IMt5BrokerConnector` |
| `D:\Prop\tests\Unit\CopyRosterEngineTests.cs` | Domain-only (8 facts) |

No secrets. No product edit this slot.
