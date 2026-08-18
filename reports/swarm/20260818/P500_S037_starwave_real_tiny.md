# P500_S037 — Starwave `real\FX3` is ~28 accounts; copying Starwave demo is the same adverse selection

| Field | Value |
|---|---|
| Slot | **S037** |
| Angle | Measured Starwave live book: six `Starwave\real\FX3\*` groups sum to **28** logins. That is not a real-money source universe. Copying `Starwave\demo\FX2\*` (1905 / 1948) onto live Pepperstone is the **same** adverse-selection error as Achiever `demo\yo-*` (P500_S004). |
| Verdict | **Almost no real-money source book.** Do not treat Starwave as the funded alternative to Achiever. Do not rank or copy `demo\FX2`. Do not pool 1905 demo names with 28 `real\FX3` names. Do not copy `FX3\LP`. Ingest all groups; promote none of this slice to `35=D`. |
| Date | 2026-08-18 |
| Product source modified | **No.** Report only. |
| Secrets | **None.** No passwords, no proxy creds, no FIX secrets. Logins below are already in the public census artifact. |
| Law | Architecture v2 §3 (future dest-net P&L), §9 (group class), §17 (source ticks ≠ dest ticks), §19–§21 (OOS dest-net, beat highest historical P&L), §23 (trade #3 → SHADOW). Measured: `LIVE_MANAGER_FETCH_MEASURED.md` + `LIVE_GROUPS_AND_TRADERS.json`. Sibling: `P500_S004_demo_adverse_selection.md`, `P500_S012_starwave_unscored.md`. |

**Honesty:** This is a **census + selection-economics** finding, not a measured Pepperstone loss. `REAL_COPY_EXECUTION_ENABLED` is false; `CTraderFixSession` has no `35=D`. Destination real P&L is **0 by absence**. Starwave `scored=0` (P500_S012) is a queue, not proof that FX3 has edge. Do not read this note as “we have a live Starwave copy book.”

---

## 0. Binding claim

Achiever cannot seed a live Pepperstone book (P500_S004: 0 funded-live groups). The remaining hope was Starwave `real\FX3`. The measured table kills that hope:

```text
Starwave\real\FX3\*  =  22 + 0 + 0 + 4 + 0 + 2  =  28 logins
Starwave total       =  1948 manager traders
real share           =  28 / 1948  =  1.44%
demo share           =  1905 / 1948 =  97.79%
```

Twenty-eight names is a **tiny list**, not a source book. Half of them have zero equity. Two of them are `LP`. Copying the other 97.8% (`demo\FX2`) is **the same adverse selection** as copying Achiever `demo\yo-2step`.

---

## 1. Measured Starwave groups (2026-08-18)

Native Manager census (`LIVE_MANAGER_FETCH_MEASURED.md`; JSON broker block `STARWAVEFX`, `connected: true`, `elapsedMs` 6413, `groups` 10, `accounts` 1948, `openPositions` 478). Path: `GroupRequestArray` + `UserRequestArray`. Direct connect (no proxy). These are **all groups this manager login can see**.

| Group | Currency | Accounts | Share of 1948 | Class |
|---|---|---:|---:|---|
| `Starwave\cent\FX1\grp1` | USC | 11 | 0.56% | cent / micro, not dest-live |
| `Starwave\cent\FX1\grp2` | USC | 4 | 0.21% | cent / micro, not dest-live |
| `Starwave\demo\FX2\grp1` | USD | 170 | 8.73% | **DEMO** |
| `Starwave\demo\FX2\grp2` | USD | **1735** | **89.07%** | **DEMO** |
| `Starwave\real\FX3\grp1` | USD | 22 | 1.13% | broker-real label |
| `Starwave\real\FX3\grp2` | USD | 0 | 0% | empty |
| `Starwave\real\FX3\grp3` | USD | 0 | 0% | empty |
| `Starwave\real\FX3\grp4` | USD | 4 | 0.21% | broker-real label |
| `Starwave\real\FX3\grp5` | USD | 0 | 0% | empty |
| `Starwave\real\FX3\LP` | USD | 2 | 0.10% | **LP / house — not a copy source** |

**Sums**

| Bucket | Accounts | % of Starwave |
|---|---:|---:|
| `demo\FX2\*` | **1905** | **97.79%** |
| `real\FX3\*` (incl. LP + three empty groups) | **28** | **1.44%** |
| `real\FX3` minus `LP` | **26** | **1.33%** |
| `cent\FX1\*` | **15** | **0.77%** |
| **Total** | **1948** | **100%** |

Arithmetic check: 11+4+170+1735+22+0+0+4+0+2 = **1948**. FX3 only: 22+0+0+4+0+2 = **28**. Three of six FX3 groups are empty.

Compare Achiever (same census): 6512 traders, **0** `real\*` groups, 6295 `demo\yo-2step` (96.67%). Starwave is the same shape with a different brand: **demo mass + a decorative real path**.

---

## 2. The 28 FX3 logins are not a book

Balances/equity from `LIVE_GROUPS_AND_TRADERS.json` (no passwords). Snapshot at census time; not a P&L series.

### 2.1 `Starwave\real\FX3\grp1` — 22

| Login | Lev | Balance | Equity | Note |
|---|---:|---:|---:|---|
| 55555 | 100 | 2037.35 | 2027.71 | small live-looking |
| 1129596 | 500 | 0 | 0 | dead |
| 1299796 | 500 | 1.74 | 1.74 | dust |
| 1918386 | 500 | 684.24 | 684.24 | small |
| 2592450 | **2000** | 26406.70 | 26406.70 | size + extreme lev |
| 4471638 | 500 | 0 | 0 | dead |
| 5029126 | 500 | 1.27 | 21.27 | dust |
| 5342821 | 500 | 0 | 0 | dead |
| 5360131 | 500 | 100402.83 | 100402.83 | large; one name, not a book |
| 5651611 | 500 | 20839.00 | 20839.00 | mid |
| 6411778 | 500 | 0 | 0 | dead |
| 6741425 | 500 | 0 | 0 | dead |
| 7147351 | 500 | 0.46 | 2.46 | dust |
| 7228813 | 500 | 0 | 0 | dead |
| 7506824 | 500 | 0 | 0 | dead |
| 7546781 | 500 | 0 | 0 | dead |
| 8071969 | 500 | 0 | 0 | dead |
| 9053833 | 500 | 0 | 0 | dead |
| 9336296 | 500 | 0 | 0 | dead |
| 900909612713 | **1000** | 510998.19 | 510998.19 | house/IB-shaped id + size; **not** a retail copy lead |
| 900909629402 | 100 | 1000.64 | 1000.64 | `900909*` prefix also lives on demo |
| 900909629523 | 200 | 2001.00 | 2601.00 | same prefix |

### 2.2 `Starwave\real\FX3\grp4` — 4

| Login | Lev | Balance | Equity | Note |
|---|---:|---:|---:|---|
| 1078742 | 500 | 0 | 0 | dead |
| 3735228 | 500 | 0 | 0 | dead |
| 6554973 | **5000** | 1007.32 | 1381.20 | 1:5000 is not a dest-copy template |
| 9008009 | 100 | 1149.90 | 1843.49 | small |

### 2.3 `Starwave\real\FX3\LP` — 2 — **never a source**

| Login | Lev | Balance | Equity | Note |
|---|---:|---:|---:|---|
| 370040 | 500 | 85.48 | 82.96 | LP / warehouse residue |
| 8242064 | 500 | 0.80 | 5.80 | LP / warehouse residue |

`LP` is a **liquidity / house** bucket. Architecture already says we are not an LP (`A87`). Copying LP tickets is copying the other side of the book (or internal netting), not a trader to follow.

### 2.4 Equity census of the 28

| Bucket | Count | Who |
|---|---:|---|
| Equity = 0 | **13** | 11 in grp1 + 2 in grp4 |
| Equity &lt; $25 (incl. one LP) | **4** | 1299796, 5029126, 7147351, 8242064 |
| LP remainder | **1** | 370040 (~$83) |
| Equity ≥ $100, not LP | **10** | 55555, 1918386, 2592450, 5360131, 5651611, 900909612713, 900909629402, 900909629523, 6554973, 9008009 |

So the “real-money source book” collapses to **about ten** non-zero, non-LP names — and that ten still mixes 1:2000 / 1:5000 leverage, a 511k `900909*` house-shaped login, and several ~$1k stubs. **N≈10 is not a rankable universe.** Any “top Starwave real” sort is a coin flip plus one or two large IDs.

`900909*` also appears on `Starwave\demo\FX2\grp2` (e.g. login `900909629831`). Treat that prefix as **internal / test / IB until proven retail**. Do not let one 511k login become “the Starwave edge.”

---

## 3. Copying Starwave demo is the same adverse selection as Achiever demo

P500_S004 already forbade Achiever `demo\` / `contest\`. Starwave `demo\FX2` is the **same class**, larger than Achiever’s leftover contest slice and almost as dominant as `demo\yo-2step`.

| | Achiever `demo\yo-2step` + `demo\yo-payp` | Starwave `demo\FX2\*` |
|---|---|---|
| Virtual money | Yes | Yes |
| Share of that broker | 97.08% demo (plus 2.92% contest) | **97.79% demo** |
| Trader objective | pass hurdle / look profitable | same or “demo P&amp;L leaderboard” |
| Personal ruin on source | ~0 per tick | ~0 per tick |
| Dest if copied 1:1 lots | live Pepperstone cash, no reset | same |
| Rank by source $ | selects lottery + size-up + survivors | **same** |
| Source ticks = dest ticks (§17) | No | No |

Objective of a demo FX2 login:

```text
print a P&L path on virtual USD
(no overnight ruin, cheap reset, demo fill model)
```

Objective of the live Pepperstone book:

```text
positive future execution-venue-net P&L
inside hard risk limits
```

Those are **not the same function**. Ranking `demo\FX2\grp2` (1735 names) by source profit selects path luck, martingale that has not hit DD yet, and lot explosion. A 1:1 shadow (`RequestedQuantity = MaxVolumeLots`) transfers that size onto dest. Failed demo is “new login.” Failed dest is cash.

**Do not launder the rule** as “Achiever demo is toxic but Starwave demo is a broker book.” The path token is `demo\`. That is the class.

Cent `FX1` (15 USC accounts) is also **not** a dest-live template: different currency scale, different contract psychology. Ingest it; do not promote it.

---

## 4. Why pooling would drown the 28

Dashboard ranks by `EarlyScore` DESC (P500_S012). Until Starwave is scored, those rows sit at `INSUFFICIENT_DATA` / 0 and **cannot** outrank Achiever-demo SHADOW. After they are scored:

| If we… | What happens |
|---|---|
| Pool Achiever + Starwave, rank by early score / source $ | 6295 + 1905 demo names dominate. 28 FX3 names are invisible. |
| Rank Starwave only, still include `demo\FX2` | 1905 demo vs 28 real. Same drown. |
| Rank only `real\FX3` including `LP` | Copy warehouse / dust / house IDs. |
| Rank only non-zero non-LP FX3 | **N≈10.** No OOS power. One 511k login or one 1:2000 book becomes the “model.” |
| Wait for Starwave `scored` to leave 0, then copy top demo | Scoring completeness ≠ eligibility. P500_S012: `scored=0` is a queue. Scoring demo FX2 still produces the P500_S004 trap. |
| Treat 91 966 Starwave deal inserts as a real book | Those inserts are **mostly demo FX2** by headcount. Deal count is not group class. |

§3 forbids “who made the most so far.” On this census that statistic is almost surely a **demo** login.

---

## 5. Rule (promotion — not implemented here)

**Default exclude** from `LIVE_CANDIDATE` / `LIVE` / any path that can emit `35=D`:

```text
group like  Starwave\demo\%
group like  Starwave\cent\%
group like  Starwave\real\FX3\LP
group like  demo\%
group like  contest\%
```

**Allow ingest + reconstruct + baseline score + SHADOW research** on every visible group, including the 1905 demo names. Exclusion is a **promotion** filter, not a fetch filter (§7 / §9). Discovery must still walk all 10 Starwave groups.

**`real\FX3` minus `LP` is not an automatic admit.** It is only *eligible to be studied*. Admit to live only if later proven, all of:

1. Chronological OOS (§20–§21) on **destination-net** (or honest dest-cost shadow), not source $.
2. Beat “highest historical P&L” and random on dest-net (§21).
3. Group class is an explicit / stratified feature. A model that works because 97.79% of Starwave rows are `demo\FX2\grp2` **is** the demo baseline.
4. Drop `LP`, drop equity-0 shells, treat `900909*` as internal until proven retail.
5. Risk veto remains (§1.8 / §39). `CanPromoteToLive` stays false. `REAL_COPY` stays false.

Until that proof exists: **Starwave has no live source book.** Higher demo profit ⇒ stronger exclude, not stronger copy weight.

Suggested operator filter (dashboard / persist), not product code in this slot:

```text
copy_eligible =
    broker == STARWAVEFX
    AND group like 'Starwave\real\FX3\%'
    AND group not like '%\LP'
    AND equity > operational_floor          -- not the 13 zeros
    AND login prefix not in {internal/IB}   -- 900909* until proven
    AND dest_shadow_net > 0 after costs     -- when a dest book exists
    AND no martingale / lot-explosion / lev-absurd flag
```

Honest implication: **that filter may match fewer than ten names.** That is the measured state. Do not widen the filter to demo to “get a book.”

---

## 6. What this is not

| Claim | Status |
|---|---|
| EX5 decompiled / 95% parity | **No.** Unrelated. |
| Live copy already losing on Pepperstone | **No.** Send path off; dest P&L 0 by absence. |
| Starwave `real\FX3` is a funded prop / Pepperstone-equivalent universe | **No.** 28 logins, 13 flat, 2 LP. |
| Empty `grp2` / `grp3` / `grp5` mean the server has no more real groups | **Unknown.** These are all groups **this manager** can see. |
| `scored=0` means FX3 has no deals | **No.** P500_S012: queue. Deals are not scores; scores are not eligibility. |
| Cent USC is “almost real” | **No.** Different scale. Ingest only. |
| `FX3\LP` is a lead | **No.** Warehouse. |
| Product was changed to exclude these groups | **No.** This note only. |
| ML on 1948 Starwave rows will find the 28 | **No.** It will fit `demo\FX2\grp2` unless class is stratified and dest-net OOS is the label. |

---

## 7. Cross-links (no product edits)

| Artifact | Role |
|---|---|
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | Table: FX3 22+0+0+4+0+2; Starwave 10/1948 |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | Per-login lev/balance/equity; no passwords |
| `D:\Prop\reports\swarm\20260818\P500_S004_demo_adverse_selection.md` | Achiever demo/contest exclude; already flags FX3 N=28 |
| `D:\Prop\reports\swarm\20260818\P500_S012_starwave_unscored.md` | `deals-done` / `scored=0` is a queue |
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §3, §9, §17, §19–§23 | Goal, group class, ticks, OOS, SHADOW |
| `D:\Prop\reports\swarm\20260818\A87_not_an_lp.md` | We are not an LP; do not copy `FX3\LP` |

---

## 8. One-line standing rule

**Starwave `real\FX3` is 28 manager logins (13 flat, 2 LP, ~10 with equity). That is not a real-money source book.** Copying `Starwave\demo\FX2` (1905 / 1948) is the same adverse selection as Achiever demo: virtual-money profit is the wrong rank. Do not pool, do not promote, do not emit `35=D`. Study the handful of non-LP FX3 names only if later dest-net OOS proves them.

*End of P500_S037. Product source was not modified.*
