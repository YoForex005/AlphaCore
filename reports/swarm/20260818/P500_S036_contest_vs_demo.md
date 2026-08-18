# P500_S036 — `contest\yo-2step` (179) vs `demo\yo-2step` (6295); SHADOW is demo-only today

| Field | Value |
|---|---|
| Slot | **P500_S036** |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S036_contest_vs_demo.md` |
| Agent | P500_S036 (senior engineer; contest vs demo universe only) |
| Assigned | Read `LIVE_MANAGER_FETCH_MEASURED.md` groups. `contest\yo-2step` has 179 vs demo 6295. Write this file. If any SHADOW are contest not demo, list how to filter. Currently SHADOW are demo only. Profit filter: prefer later funded/real groups if they appear. Do not edit product. |
| Product source modified | **No.** Report only. |
| Secrets | **None.** Manager logins and group paths only. No passwords, no proxy auth, no FIX secrets. |

**One-line:** Achiever’s 2-step book is **6295 demo vs 179 contest** (35.2:1). Measured SHADOW names sit on **`demo\`** only. `contest\` is **not** funded live even though §9 env tokens say `*_REAL`. Do not rank either bucket by source profit for Pepperstone. If contest SHADOW appear later, exclude them the same way as demo. Prefer any later **funded / `real\`** group as the profit-filter universe.

---

## 0. Verdict

| Claim | Result |
|---|---|
| `contest\yo-2step` account count | **179** (measured Manager census) |
| `demo\yo-2step` account count | **6295** |
| Ratio | **35.17 demo : 1 contest** (96.67% vs 2.75% of Achiever 6512) |
| Any current SHADOW on `contest\*` | **No.** Given + re-checked: live SHADOW examples are `demo\yo-2step` |
| `contest\` == funded / broker-live | **No.** §9 `MT5_GROUP_*_REAL=contest\yo-*` is a **label**, not live capital |
| Achiever funded / `real\*` in this manager view | **0** |
| Product already filters SHADOW by group | **No.** `PersistDemoShadowAsync` and `GetTradersAsync` ignore group class |
| Use source $ profit to pick live copy from demo or contest | **Forbidden.** Prefer later funded/real groups when they appear |
| Product edited this slot | **No** |

Risk to live capital from this note: **NONE.** `REAL_COPY` is off; no `35=D`. Residual risk is **operator ranking**: sorting SHADOW by `earlyScore` / source P&L over a 97% demo book.

---

## 1. Measured groups (do not re-invent)

Source: `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`  
Companion dump: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe`, UTC **2026-08-18T08:42:16.8519545+00:00**, `envLoaded: true`, passwords never written.

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | OK via HTTP proxy | 8 | 6512 | 1506 |
| STARWAVEFX | OK direct | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

### Achiever (this manager can see **only** these)

| Group | §9 plan token | Accounts | Share of 6512 | Class |
|---|---|---:|---:|---|
| `demo\yo-2step` | `MT5_GROUP_2STEP_DEMO` / `CORE_DEMO` | **6295** | **96.67%** | challenge **DEMO** |
| `demo\yo-payp` | `PASSFIRST_DEMO` | 23 | 0.35% | challenge DEMO |
| `demo\yo-1step` | `1STEP_DEMO` | 4 | 0.06% | challenge DEMO |
| `demo\yo-instant` | — | 0 | 0% | empty DEMO |
| `contest\yo-2step` | `MT5_GROUP_2STEP_REAL` / `CORE_REAL` | **179** | **2.75%** | **contest**, not funded |
| `contest\yo-payp` | `PASSFIRST_REAL` | 5 | 0.08% | contest |
| `contest\yo-instant` | `INSTANT_REAL` | 4 | 0.06% | contest |
| `contest\yo-1step` | `1STEP_REAL` | 2 | 0.03% | contest |

Sums: **demo 6322 (97.08%)**, **contest 190 (2.92%)**, **funded / broker-live = 0**.

`contest\yo-2step` is the only non-trivial contest bucket. It is still **35× smaller** than `demo\yo-2step`. A pooled rank over Achiever is a **demo rank**.

These are **all groups this manager login can see**. Extra server groups, if any, are outside the ACL (`LIVE_MANAGER_FETCH_MEASURED.md`).

### Starwave (for the “later funded/real” clause)

| Prefix | Accounts | Class |
|---|---:|---|
| `Starwave\demo\FX2\*` | 1905 | demo |
| `Starwave\cent\FX1\*` | 15 | cent (not a prop funded book) |
| `Starwave\real\FX3\*` (grp1+4+LP; empty grp2/3/5 omitted from money) | **28** | **only visible real-ish pool** |

28 real names must not be drowned by 6295 Achiever demo names in one `ORDER BY earlyScore DESC`.

---

## 2. What the two 2-step paths actually are

Architecture §9 (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`):

```env
MT5_GROUP_2STEP_DEMO=demo\yo-2step
MT5_GROUP_2STEP_REAL=contest\yo-2step
MT5_GROUP_CORE_DEMO=demo\yo-2step
MT5_GROUP_CORE_REAL=contest\yo-2step
```

**Honesty on the token `REAL`:** it means “the non-demo *plan* bucket on Achiever,” not “Pepperstone-equivalent going-concern capital.” There is no Achiever `real\*` / funded group in the measured ACL.

| Path | Trader objective | Money | After fail |
|---|---|---|---|
| `demo\yo-2step` | Hit phase profit + stay inside daily/max DD | Virtual challenge cash | Reset / new login / buy another eval |
| `contest\yo-2step` | Leaderboard / contest replica of the same hurdle | Still not dest live | Same class: lottery + reset |
| Later funded / `real\` (not present on Achiever today) | Stay solvent on going-concern rules | Closer to dest-capital economics | Real loss, no eval reset |

Sample contest 2-step balances from the dump (not a full histogram): `301104` 5999.14 / `301105` 6000 / `323069` 1072.55 / `323537` 1000.01 / `323539` 930.38. Typical eval notionals (~1k or ~6k), not a funded book.

Sample demo 2-step: `301101` 15442 / 15718; live SHADOW examples below sit near **~$930–970** — leftover challenge cash, not a scaled live account.

Sibling economics (do not re-derive): `P500_S004_demo_adverse_selection.md`. Contest is the **same adverse-selection class**, smaller N, more lottery.

---

## 3. Current SHADOW set — demo only (re-checked)

**Given (binding for this slot):** current SHADOW traders are **demo only**.

Re-check against measured artifacts (this slot did **not** re-hit live HTTP):

| Evidence | Group |
|---|---|
| P500_S004 standing given | all SHADOW ∈ {`demo\yo-2step`, `demo\yo-payp`} |
| P500_S001 live paint: ACHIEVER `302252` SHADOW `earlyScore=95.50` `netSourcePnl=-68.46` | JSON: `demo\yo-2step`, bal 931.54 |
| P500_S001 live paint: ACHIEVER `303174` SHADOW `95.50` / `-29.38` | JSON: `demo\yo-2step`, bal 970.62 |
| Dummy seed `10001` (off on live API) | Fake `demo\Maxmaster` (not in live Achiever ACL) |
| Dummy seed `10003` `contest\yo-2step` | **0 deals → `INSUFFICIENT_DATA`**, no SHADOW rows |
| Dummy seed `99001` | Fake `real\standard`, not a live Starwave path |
| Starwave live | P500_S012: `scored=0` while Achiever scores — **no Starwave SHADOW yet** |

**No measured SHADOW login is on `contest\yo-2step` (179) or any other `contest\`.**

This is **mass + scoring order**, not a group gate:

- 96.67% of Achiever logins are `demo\yo-2step`.
- `BaselineScorer` / `TraderStateMachine.FromBaseline` never reads `GroupName`.
- `PersistDemoShadowAsync` writes `CopyIntent`/`ShadowOrder` iff `state == SHADOW` (plus a dest-quote row). No `demo\` / `contest\` test.
- `GetTradersAsync` maps every account; optional filter is **broker + state only**; sort is **`EarlyScore` DESC**. No group class.

So if a contest login later completes ≥3 clean XAU with quality ≥ 70 and risk < 40, the **same path will SHADOW it**. Absence today is not a filter.

---

## 4. If SHADOW appear on contest — how to filter (operator, not product edit)

Do **not** stop fetching `contest\` (§7 / §9: plan map is a label, never the fetch list). Filter **promotion / live-candidate / profit rank**, not ingest.

### 4.1 Prefix / exact names (Achiever)

| Match | Action for LIVE / copy-eligible / profit rank |
|---|---|
| `group` starts with `demo\` | **EXCLUDE** |
| `group` starts with `contest\` | **EXCLUDE** (same class) |
| exact `contest\yo-2step` | **EXCLUDE** (the 179) |
| exact `contest\yo-1step` / `yo-instant` / `yo-payp` | **EXCLUDE** (2+4+5) |
| `PlanMapping` in `{2STEP_REAL, 1STEP_REAL, INSTANT_REAL, PASSFIRST_REAL, CORE_REAL}` **when those tokens still point at `contest\`** | **EXCLUDE** — token `REAL` is a lie for dest capital |
| later Achiever path like `funded\` / `real\` / live-funded (not in ACL today) | **PREFER** for profit filter |

Dashboard today cannot do `?group=` — `EfDashboardQueries.GetTradersAsync(string? broker, string? state)` has no group argument (A92 catalog is spec-only). Operator must join group client-side from `/api/traders` rows (`TraderRowDto.Group`).

### 4.2 SQL / dump join (read-only recipe)

```text
-- SHADOW that would be contest (none today; re-run after rescore)
SELECT a.login, a.group_name, s.current_state, s.early_quality_score
FROM mt5_accounts a
JOIN trader_scores s
  ON s.broker_id = a.broker_id AND s.login = a.login
WHERE s.current_state = 'SHADOW'
  AND (
        a.group_name LIKE 'contest\%'
     OR a.group_name LIKE 'demo\%'
      );

-- Profit-filter universe if/when funded or Starwave real is scored
SELECT a.login, a.group_name, s.early_quality_score
FROM mt5_accounts a
JOIN trader_scores s
  ON s.broker_id = a.broker_id AND s.login = a.login
WHERE s.current_state IN ('SHADOW','LIVE_CANDIDATE')
  AND a.group_name NOT LIKE 'demo\%'
  AND a.group_name NOT LIKE 'contest\%'
  AND (
        a.group_name LIKE 'Starwave\real\%'
     OR a.group_name LIKE 'funded\%'
     OR a.group_name LIKE '%\real\%'   -- review cent vs real; Starwave\cent is NOT this
      );
```

JSON equivalent on `LIVE_GROUPS_AND_TRADERS.json`: join later `/api/traders?state=SHADOW` logins to `traders[].group`. Contest 2-step logins start in the dump at `301104` / `301105` / `323069`… — if any of those logins paint SHADOW, they are contest.

### 4.3 Suggested copy-eligible predicate (promotion only)

```text
copy_eligible =
    state in {SHADOW, LIVE_CANDIDATE}
    AND group_class == FUNDED_OR_BROKER_LIVE
    AND group not like 'demo\%'
    AND group not like 'contest\%'
    AND NOT (group like 'Starwave\demo\%' OR group like 'Starwave\cent\%')
    AND dest_shadow_net > 0 after costs     -- when a real dest book exists
    AND martingale = false AND lot_escalation = false
```

`CanPromoteToLive` stays **false**. `REAL_COPY` stays **false**. This predicate is **not** in product. Putting it on ingest would violate §9.

### 4.4 Product holes that would let contest SHADOW leak into a rank

| Site | What it does today | If contest scores SHADOW |
|---|---|---|
| `TraderStateMachine.FromBaseline` | quality/risk/N only | Contest becomes SHADOW |
| `PersistDemoShadowAsync` | `state == SHADOW` → `SHADOW_ONLY` + 1:1 lots | Contest sizes copied into shadow book |
| `GetTradersAsync` | sort `EarlyScore` DESC | Contest (or demo) lottery rises to the top |
| `Mt5Group.PlanMapping` | never set on live upsert (`EnabledForAnalysis = true` for all) | No plan label to filter on |
| Fake seed `10003` | contest, N=0 | Not a live contest SHADOW fixture |

---

## 5. Profit filter — prefer later funded / real groups

`SHADOW` is **not** a profit filter (`P500_S001`). Quality can be 95.50 on **negative** dashboard `netSourcePnl`. Ranking SHADOW by source $ or `earlyScore` over demo/contest **selects hurdle-hacking**, not dest-net edge (`P500_S004`).

**Preference order when ranking who may ever become a live candidate:**

| Priority | Universe | Present in 08:42Z census? | Use as profit rank? |
|---:|---|---|---|
| 1 | Later **funded** Achiever group (not in ACL today) | **No** | **Yes, when it appears** — going-concern rules, no eval reset |
| 2 | `Starwave\real\FX3\*` (28 accounts) | **Yes**, **unscored** (P500_S012) | **Yes, after score** — only current real-ish pool |
| 3 | `contest\yo-*` (190; 179 of them 2-step) | Yes | **Research / SHADOW only.** Not dest profit |
| 4 | `demo\yo-*` (6322; 6295 of them 2-step) | Yes | **Research / SHADOW only.** Default exclude from live rank |
| — | `Starwave\demo\*` / `Starwave\cent\*` | Yes | Same exclude as Achiever demo; cent ≠ funded |

Rules:

1. **Never** let 6295 demo (or 179 contest) drown 28 real names in one list.
2. When a trader **moves** demo/contest → funded, prefer the **later group** for profit/eligibility. Do **not** use the pass itself as a trade-#3 feature (§20 leak).
3. On the preferred (funded/real) set, rank by **destination-net / honest dest-cost shadow**, not source demo $. Until that book exists, **do not** promote.
4. Higher source profit on `demo\yo-2step` or `contest\yo-2step` is a **stronger exclude**, not a stronger copy weight.
5. Ingest **all** groups anyway. Exclusion is promotion-time.

Honest implication today: **Achiever cannot seed a live Pepperstone book.** The profit-filter universe is empty on Achiever and 28 unscored Starwave real names.

---

## 6. What this is not

| Claim | Status |
|---|---|
| EX5 decompiled / 95% copy live | **No** |
| Live Pepperstone already losing on these groups | **No.** Send path off; dest real P&L 0 by absence |
| `contest\yo-2step` is the funded step after demo pass | **Unproven** in this ACL; name is contest. Do not treat `*_REAL` env as funded |
| All 179 contest accounts are skilled / all are lottery | **Not claimed.** The *class* is contest; N is too small and incentives still wrong |
| Product now excludes `contest\` from SHADOW persist | **No.** This file only |
| Re-measured `/api/traders?state=SHADOW` this slot | **No.** Census + P500_S001/S004 join |

---

## 7. Cross-links

| Path | Role |
|---|---|
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | 8+10 groups; 179 vs 6295 |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | Full logins, no passwords |
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §3, §7, §9, §20–§23 | Dest-net goal; fetch-all; plan label; no pass leak; SHADOW after #3 |
| `D:\Prop\reports\swarm\20260818\P500_S004_demo_adverse_selection.md` | Why demo/contest profit is toxic for live copy |
| `D:\Prop\reports\swarm\20260818\P500_S001_scorer_vs_negative_pnl.md` | SHADOW ≠ profit filter; 302252 / 303174 |
| `D:\Prop\reports\swarm\20260818\P500_S012_starwave_unscored.md` | Real pool still `scored=0` |
| `D:\Prop\reports\swarm\20260818\D68_plan_filter.md` | Ingest must not filter by plan |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Rebuild → PersistDemoShadow, no group gate |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | Traders sorted by EarlyScore; no group filter |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | `FromBaseline` → SHADOW; `CanPromoteToLive => false` |

---

## 8. Standing rule

**`contest\yo-2step` (179) is not a better live universe than `demo\yo-2step` (6295).** It is the same challenge/contest class, 35× smaller. Current SHADOW is **demo-only**; if contest SHADOW appear, exclude `contest\` the same as `demo\`. Profit-rank only **later funded / `Starwave\real\`** names. Do not change fetch-all. Do not edit product for this slot.

*End P500_S036. Product source was not modified.*
