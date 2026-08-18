# Swarm Log

Permanent log of `D:\Prop` research / audit waves. Chat is not storage.

---

## 2026-08-18 — P500_VERIFY_88 adversarial profit-path verify (slot 88)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_88 |
| Slot | 88 |
| Purpose | Adversarial confirm: (1) no 35=D builder (2) CanPromoteToLive false (3) RealCopyEnabled forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_88.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` **SSRF-blocked**. `GET :5000/api/copy/status` no body. Runtime flag not process-proven. |
| Verdict | **FAIL.** Claim 2 PASS. Claim 1 PASS only if scoped to `CTraderFixSession` (`35=A` only; product `Build("D")` hosted). Claim 3 **disproved** (DI binds `.env` `true`; logon host logs, no re-pin). Claim 4 persist-hop cannot send / unscoped FAIL (hosted `ExecuteDemoCopyAsync` can `Build("D")`; ledger 305750 dest open). Claim 5 PASS_PAPER (SHADOW/slippage ≠ dest cash; residual AUTO_ADMIT). Risk **NONE** on live `1369850`; demo dest send **wired**. |

---

## 2026-08-18 — P500_VERIFY_97 adversarial profit-path verify (slot 97)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_97 |
| Slot | 97 |
| Purpose | Adversarial confirm: (1) no 35=D builder (2) CanPromoteToLive false (3) RealCopyEnabled forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_97.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` **SSRF-blocked**. No live body. |
| Verdict | **FAIL.** Claim 2 PASS. Claim 1 PASS only if scoped to `CTraderFixSession` (`35=A` only; product `Build("D")` ×5 hosted). Claim 3 **disproved** (DI binds `.env` `true`; logon host logs, no re-pin). Claim 4 **FAIL** (hosted `ExecuteDemoCopyAsync` can `Build("D")`; ledger 305750 dest open). Claim 5 FAIL as written (PASS_PAPER; residual AUTO_ADMIT). Risk **NONE** on live `1369850`; demo dest send **wired**. |

---

## 2026-08-18 — P500_VERIFY_91 adversarial profit-path (slot 91)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_91 |
| Slot | 91 |
| Purpose | Adversarial re-read: `CTraderFixSession`, `BaselineScorer`, `RiskEngine`, `LiveCopyPage`. Confirm (1) no 35=D builder (2) `CanPromoteToLive` false (3) `RealCopyEnabled` forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_91.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health`, `:18720/api/health`, `/api/copy/status` **blocked** (localhost SSRF). No live body. |
| Verdict | **FAIL.** Claim 2 PASS. Claim 1 PASS_SESSION only (`CTraderFixSession` 135/135 is `35=A`; product `Build("D")` ×5 hosted). Claim 3 **disproven**: `.env` L73 `true` + DI L41 bind; logon logs, no re-pin. Claim 4 **FAIL** unscoped: 20s `ExecuteDemoCopyAsync` → dest `35=D`; ledger 305750/`237339770` open; dest DTO `0` is constructor. Claim 5 PASS_PAPER / dest-class **FAIL**; dest-cash-absent **UNPROVEN**. Risk: live `1369850` **NONE**; demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_93 adversarial profit-path verify (slot 93)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_93 |
| Slot | 93 |
| Purpose | Adversarial re-read: `CTraderFixSession`, `BaselineScorer`, `RiskEngine`, `LiveCopyPage`. Confirm (1) no 35=D builder (2) `CanPromoteToLive` false (3) `RealCopyEnabled` forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_93.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` **SSRF-blocked**. No live body. |
| Verdict | **FAIL.** (1) FAIL unscoped / PASS_SESSION (`CTraderFixSession` 135/135 is `35=A` only; product `Build("D")` ×5 + hosted hopper). (2) PASS (`CanPromoteToLive => false`). (3) FAIL — DI binds `.env` L73 `true`; logon logs-only. (4) FAIL unscoped — demo hopper can send now; dest DTO 0 is constructor. (5) PASS_PAPER / FAIL_AS_DEST_CLASS — SHADOW paper ≠ dest P&L; SHADOW is AUTO_ADMIT. Live `1369850` **NONE**. Demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_94 adversarial profit-path verify (slot 94)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_94 |
| Slot | 94 |
| Purpose | Adversarial re-read: `CTraderFixSession`, `BaselineScorer`, `RiskEngine`, `LiveCopyPage`. Confirm (1) no 35=D builder (2) `CanPromoteToLive` false (3) `RealCopyEnabled` forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_94.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only booleans `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` **SSRF-blocked**; `GET :5000/api/copy/status` and `:18720` retrieve fail. No live body. |
| Verdict | **FAIL.** (1) FAIL unscoped / PASS_SESSION (`CTraderFixSession` 135/135 is `35=A` only; hosted `CopyOpen.Build("D")`). (2) PASS (`CanPromoteToLive => false`). (3) FAIL — DI binds `.env` L73 `true`; logon logs-only. (4) FAIL unscoped / PASS_NOT_BOOKED_DEST_PROFIT — demo hopper can send now; dest DTO 0 is constructor; ledger dest open. (5) PASS — SHADOW paper ≠ dest P&L. Live `1369850` **NONE**. Demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_90 adversarial profit-path (slot 90)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_90 |
| Slot | 90 |
| Purpose | Adversarial verify from live files: (1) no 35=D builder; (2) CanPromoteToLive is false; (3) RealCopyEnabled forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. FAIL any unproven claim. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_90.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health`, `/api/copy/status`, `:18720/api/health` **blocked** (localhost SSRF). No live body. |
| Verdict | **FAIL.** Claim 2 PASS. Claim 1 PASS_SESSION only. Claim 3 disproven (env `true`, no logon re-pin). Claim 4 FAIL (hosted demo `Build("D")`; dest DTO ctor `0`; no live dest mark). Claim 5 PASS_PAPER / FAIL_AS_DEST_CLASS. Risk: live `1369850` **NONE**; demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_92 adversarial profit-path verify (slot 92)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_92 |
| Slot | 92 |
| Purpose | Adversarial re-read: `CTraderFixSession`, `BaselineScorer`, `RiskEngine`, `LiveCopyPage`. Confirm (1) no 35=D builder (2) `CanPromoteToLive` false (3) `RealCopyEnabled` forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_92.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` **SSRF-blocked** (`127.0.0.1` and `localhost`). No live body. |
| Verdict | **FAIL.** (1) FAIL unscoped / PASS_SESSION (`CTraderFixSession` 135/135 is `35=A` only; hosted `CopyOpen.Build("D")`). (2) PASS (`CanPromoteToLive => false`). (3) FAIL — DI binds `.env` L73 `true`; logon logs-only. (4) FAIL — demo hopper can send now; dest DTO 0 is constructor; ledger dest open. (5) PASS — SHADOW paper ≠ dest P&L. Live `1369850` **NONE**. Demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_83 adversarial profit-path (slot 83)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_83 |
| Slot | 83 |
| Purpose | Adversarial confirm: no 35=D builder; CanPromoteToLive false; RealCopyEnabled forced false after logon; sending now cannot be the profit path; SHADOW on demo is not dest profit. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_83.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** this slot (on-disk prior fill cited only) |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` **blocked** (localhost SSRF) |
| Verdict | **FAIL.** Claim 2 PASS. Claim 1 PASS only on `CTraderFixSession`. Claim 3 disproven. Claims 4–5 FAIL as written (5 PASS_PAPER). Risk: live `1369850` **NONE**; demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_77 adversarial profit-path verify (slot 77)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_77 |
| Slot | 77 |
| Purpose | Adversarial confirm: (1) no 35=D builder (2) CanPromoteToLive false (3) RealCopyEnabled forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_77.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/copy/status` **SSRF-blocked**. No live body. |
| Verdict | **FAIL.** Claim 2 PASS. Claim 1 PASS only if scoped to `CTraderFixSession` (`35=A` only; product `Build("D")` ×5 hosted). Claim 3 **disproved** (DI binds `.env` `true`; logon host logs, no re-pin). Claim 4 persist-hop PASS / unscoped FAIL (hosted `ExecuteDemoCopyAsync` can `Build("D")`; ledger 305750 dest open). Claim 5 PASS_PAPER (SHADOW/slippage ≠ dest cash; residual AUTO_ADMIT). Risk **NONE** on live `1369850`; demo dest send **wired**. |

---

## 2026-08-18 — P500_VERIFY_84 adversarial profit-path verify (slot 84)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_84 |
| Slot | 84 |
| Purpose | Adversarial confirm: (1) no 35=D builder (2) CanPromoteToLive false (3) RealCopyEnabled forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_84.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` **SSRF-blocked**. No live body. |
| Verdict | **FAIL.** Claim 2 PASS. Claim 1 PASS only if scoped to `CTraderFixSession` (`35=A` only; product `Build("D")` ×5 hosted). Claim 3 **disproved** (DI binds `.env` `true`; logon host logs, no re-pin). Claim 4 **FAIL** (hosted `ExecuteDemoCopyAsync` can `Build("D")`; ledger 305750 dest open). Claim 5 FAIL as written (PASS_PAPER; residual AUTO_ADMIT). Risk **NONE** on live `1369850`; demo dest send **wired**. |

---

## 2026-08-18 — P500_VERIFY_80 adversarial profit-path (slot 80)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_80 |
| Slot | 80 |
| Purpose | Adversarial re-read: `CTraderFixSession`, `BaselineScorer`, `RiskEngine`, `LiveCopyPage`. Confirm (1) no 35=D builder (2) `CanPromoteToLive` false (3) `RealCopyEnabled` forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_80.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` and `/api/copy/status` **blocked** (localhost SSRF). No live body. |
| Verdict | **FAIL.** Claim 2 PASS. Claim 1 PASS_SESSION only (`CTraderFixSession` 135/135 is `35=A`; product `Build("D")` ×5 hosted). Claim 3 **disproven**: `.env` L73 `true` + DI L41 bind; logon logs, no re-pin. Claim 4 **FAIL**: 20s `ExecuteDemoCopyAsync` → dest `35=D`; ledger 305750/`237339770` open; dest DTO `0` is constructor. Claim 5 PASS_PAPER / **FAIL** unscoped (SHADOW is dest AUTO_ADMIT). Risk: live `1369850` **NONE**; demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_75 adversarial (CTraderFixSession / scorer / risk / LiveCopyPage)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_75 |
| Slot | 75 |
| Purpose | Adversarial re-read: `CTraderFixSession`, `BaselineScorer`, `RiskEngine`, `LiveCopyPage`. Confirm (1) no 35=D builder (2) `CanPromoteToLive` false (3) `RealCopyEnabled` forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_75.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` **SSRF-blocked**; `/api/copy/status` **failed**. No live body. |
| Verdict | **FAIL.** Claim 1 FAIL unscoped / PASS_FILE (`CTraderFixSession` `35=A` only; product `Build("D")` ×5 hosted). Claim 2 PASS (`CanPromoteToLive => false`). Claim 3 **disproven** (`.env` L73 `true` + DI L41; logon logs, no re-pin). Claim 4 FAIL (demo `ExecuteDemoCopyAsync` can `35=D` now; dest DTO 0 not a mark). Claim 5 PASS_PAPER / FAIL_UNSCOPED (SHADOW can AUTO_ADMIT). Risk **NONE** on live `1369850`; demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_79 adversarial profit-path verify (slot 79)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_79 |
| Slot | 79 |
| Purpose | Adversarial re-read: `CTraderFixSession`, `BaselineScorer`, `RiskEngine`, `LiveCopyPage`. Confirm (1) no 35=D builder (2) `CanPromoteToLive` false (3) `RealCopyEnabled` forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_79.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` **SSRF-blocked**. No live body. |
| Verdict | **FAIL.** (1) FAIL unscoped / PASS_SESSION (`CTraderFixSession` 135/135 is `35=A` only; product `Build("D")` ×5 + hosted hopper). (2) PASS (`CanPromoteToLive => false`). (3) FAIL — DI binds `.env` L73 `true`; logon logs-only. (4) FAIL unscoped — demo hopper can send now; dest DTO 0 is constructor. (5) PASS — SHADOW paper ≠ dest P&L. Live `1369850` **NONE**. Demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_81 adversarial profit-path verify (slot 81)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_81 |
| Slot | 81 |
| Purpose | Adversarial confirm: (1) no 35=D builder (2) CanPromoteToLive false (3) RealCopyEnabled forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_81.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET http://127.0.0.1:5000/api/health` **SSRF blocked**. No live body. |
| Verdict | **FAIL.** Claim 2 PASS. Claim 1 PASS_SESSION only. Claim 3 disproven (env `true`, no logon re-pin). Claim 4 FAIL (hosted demo `Build("D")` + open dest `237339770`). Claim 5 PASS_PAPER / FAIL_AS_DEST_CLASS (SHADOW AUTO_ADMIT). Risk: live `1369850` **NONE**; demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_74 adversarial profit-path (slot 74)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_74 |
| Slot | 74 |
| Purpose | Adversarial verify from live files: (1) no 35=D builder; (2) CanPromoteToLive is false; (3) RealCopyEnabled forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. FAIL any unproven claim. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_74.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health`, `/api/copy/status`, `/api/settings` **blocked** (localhost SSRF). No live body. |
| Verdict | **FAIL.** Claim 2 PASS. Claim 1 PASS_SESSION only. Claim 3 disproven (env `true`, no logon re-pin). Claim 4 FAIL (hosted demo `Build("D")`; dest DTO ctor `0`; no live dest mark). Claim 5 PASS (SHADOW/slippage ≠ dest cash; residual ADMIT floor). Risk: live `1369850` **NONE**; demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_76 adversarial profit-path verify (slot 76)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_76 |
| Slot | 76 |
| Purpose | Adversarial re-read: `CTraderFixSession`, `BaselineScorer`, `RiskEngine`, `LiveCopyPage`. Confirm (1) no 35=D builder (2) `CanPromoteToLive` false (3) `RealCopyEnabled` forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_76.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only booleans `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` **SSRF-blocked**; `GET :5000/api/copy/status` fetch failed. No live body. |
| Verdict | **FAIL.** (1) FAIL unscoped / PASS_SESSION (`CTraderFixSession` 135/135 is `35=A` only; hosted `CopyOpen.Build("D")`). (2) PASS (`CanPromoteToLive => false`). (3) FAIL — DI binds `.env` L73 `true`; logon logs-only. (4) FAIL — demo hopper can send now; dest DTO 0 is constructor; ledger dest open. (5) PASS — SHADOW paper ≠ dest P&L. Live `1369850` **NONE**. Demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_78 adversarial profit-path (slot 78)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_78 |
| Slot | 78 |
| Purpose | Adversarial re-read: `CTraderFixSession`, `BaselineScorer`, `RiskEngine`, `LiveCopyPage`. Confirm (1) no 35=D builder (2) `CanPromoteToLive` false (3) `RealCopyEnabled` forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_78.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** this slot (on-disk prior fill cited only) |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` **blocked** (localhost SSRF) |
| Verdict | **FAIL.** Claim 2 PASS. Claim 1 PASS only on `CTraderFixSession`. Claim 3 **disproven**. Claims 4–5 FAIL as written (demo dest hop; SHADOW AUTO_ADMIT). Risk: live `1369850` **NONE**; demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_72 adversarial profit-path verify (slot 72)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_72 |
| Slot | 72 |
| Purpose | Adversarial confirm: (1) no 35=D builder (2) CanPromoteToLive false (3) RealCopyEnabled forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_72.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` and `/api/copy/status` **SSRF-blocked**. No live body. |
| Verdict | **FAIL.** (1) FAIL unscoped / PASS_SESSION (`CTraderFixSession` 135/135 is `35=A` only; product `Build("D")` ×5 + hosted hopper). (2) PASS `CanPromoteToLive => false`. (3) FAIL — DI binds `.env` L73 `true`; logon logs, no re-pin. (4) FAIL unscoped (demo dest `ExecuteDemoCopyAsync` now; dest DTO `0` is constructor). (5) PASS — SHADOW paper ≠ dest P&L. Live dest **NONE**. Demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_65 adversarial profit-path verify (slot 65)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_65 |
| Slot | 65 |
| Purpose | Adversarial confirm: (1) no 35=D builder (2) CanPromoteToLive false (3) RealCopyEnabled forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_65.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/copy/status` **SSRF-blocked**. No live body. |
| Verdict | **FAIL.** Claim 2 PASS. Claim 1 PASS only if scoped to `CTraderFixSession` (`35=A` only; product `Build("D")` ×5 hosted). Claim 3 **disproved** (DI binds `.env` `true`; logon host logs, no re-pin). Claim 4 **FAIL** (hosted `ExecuteDemoCopyAsync` can `Build("D")`; ledger 305750 dest open). Claim 5 PASS_PAPER (SHADOW/slippage ≠ dest cash; residual AUTO_ADMIT). Risk **NONE** on live `1369850`; demo dest send **wired**. |

---

## 2026-08-18 — P500_VERIFY_67 adversarial confirm (slot 67)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_67 |
| Slot | 67 |
| Purpose | Adversarial verify from assigned files: (1) no 35=D builder; (2) CanPromoteToLive is false; (3) RealCopyEnabled forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. FAIL any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_67.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** (this slot) |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` **blocked** (localhost SSRF). `/api/settings` `/api/copy/status` not live-proven. |
| Verdict | **FAIL.** Claim 2 PASS (`CanPromoteToLive=>false`). Claim 5 PASS (SHADOW/slippage ≠ dest cash). Claim 1 FAIL unscoped (`CopyOpen.Build("D")` hosted; session is `35=A` only). Claim 3 **disproven** (DI binds `.env` L73 `true`; logon does not re-pin). Claim 4 FAIL (`ExecuteDemoCopyAsync` dest hop + ledger 305750/`237339770` open). Risk **NONE** on live `1369850`; demo dest send **wired**. This slot sent **0**. |

---

## 2026-08-18 — P500_VERIFY_68 adversarial confirm (slot 68)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_68 |
| Slot | 68 |
| Purpose | Adversarial verify from assigned files: (1) no 35=D builder; (2) CanPromoteToLive is false; (3) RealCopyEnabled forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. FAIL any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_68.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** (this slot) |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` and `:18720/api/health` **blocked** (localhost SSRF). |
| Verdict | **FAIL.** Claim 2 PASS. Claim 5 PASS. Claim 1 PASS_SESSION / FAIL unscoped (`Build("D")` ×5 hosted). Claim 3 **disproven** (DI binds `.env` true; logon no re-pin). Claim 4 FAIL unscoped (demo dest hopper) / PASS_NOT_BOOKED_DEST_PROFIT (DTO ctor 0). Risk **NONE** on live `1369850`; demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_57 adversarial (CTraderFixSession / scorer / risk / LiveCopyPage)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_57 |
| Slot | 57 |
| Purpose | Adversarial re-read: `CTraderFixSession`, `BaselineScorer`, `RiskEngine`, `LiveCopyPage`. Confirm (1) no 35=D builder (2) `CanPromoteToLive` false (3) `RealCopyEnabled` forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_57.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` **SSRF-blocked**; `/api/copy/status` **failed**. No live body. |
| Verdict | **FAIL.** Claim 1 FAIL unscoped / PASS_SESSION (`CTraderFixSession` `35=A` only; product `Build("D")` ×5 hosted). Claim 2 PASS (`CanPromoteToLive => false`). Claim 3 **disproven** (`.env` L73 `true` + DI L41; logon logs, no re-pin). Claim 4 FAIL unscoped (demo `ExecuteDemoCopyAsync` can `35=D` now; dest DTO 0 not a mark) / PASS_NOT_BOOKED_DEST_PROFIT. Claim 5 PASS (SHADOW paper ≠ dest). Risk **NONE** on live `1369850`; demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_59 adversarial profit-path (slot 59)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_59 |
| Slot | 59 |
| Purpose | Adversarial confirm: no 35=D builder; CanPromoteToLive false; RealCopyEnabled forced false after logon; sending now cannot be the profit path; SHADOW on demo is not dest profit. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_59.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** this slot (on-disk prior fill cited only) |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` **blocked** (localhost SSRF) |
| Verdict | **FAIL.** Claim 2 PASS. Claim 1 PASS only on `CTraderFixSession`. Claims 3–5 FAIL as written. Risk: live `1369850` **NONE**; demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_62 adversarial profit-path (slot 62)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_62 |
| Slot | 62 |
| Purpose | Adversarial verify from live files: (1) no 35=D builder; (2) CanPromoteToLive is false; (3) RealCopyEnabled forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. FAIL any unproven claim. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_62.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` and `/api/copy/status` **blocked** (localhost SSRF). No live body. |
| Verdict | **FAIL.** Claim 2 PASS. Claim 1 PASS_SESSION only. Claim 3 disproven (env `true`, no logon re-pin). Claim 4 FAIL (hosted demo `Build("D")` + open dest `237339770`). Claim 5 FAIL as dest-safety (SHADOW AUTO_ADMIT); PASS_PAPER. Risk: live `1369850` **NONE**; demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_60 adversarial profit-path verify (slot 60)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_60 |
| Slot | 60 |
| Purpose | Adversarial confirm: (1) no 35=D builder (2) CanPromoteToLive false (3) RealCopyEnabled forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_60.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/{health,copy/status,settings}` **SSRF-blocked**. No live body. |
| Verdict | **FAIL.** (1) FAIL unscoped / PASS_SESSION. (2) PASS. (3) FAIL — DI binds `.env` true; no logon re-pin. (4) FAIL — demo hopper sends now; dest DTO 0 unproven. (5) FAIL unscoped (SHADOW is AUTO_ADMIT) / PASS_SCOPED paper. Live dest **NONE**. Demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_56 adversarial profit-path verify (slot 56)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_56 |
| Slot | 56 |
| Purpose | Adversarial re-read: `CTraderFixSession`, `BaselineScorer`, `RiskEngine`, `LiveCopyPage`. Confirm (1) no 35=D builder (2) `CanPromoteToLive` false (3) `RealCopyEnabled` forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_56.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only booleans `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/{health,settings,copy/status}` **SSRF-blocked**. No live body. |
| Verdict | **FAIL.** (1) FAIL unscoped / PASS_SESSION (`CTraderFixSession` 135/135 is `35=A` only; hosted `CopyOpen.Build("D")`). (2) PASS (`CanPromoteToLive => false`). (3) FAIL — DI binds `.env` L73 `true`; logon logs-only. (4) FAIL unscoped — demo hopper can send now; dest DTO 0 unproven as a mark. (5) PASS — SHADOW paper ≠ dest P&L. Live `1369850` **NONE**. Demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_58 adversarial profit-path verify (slot 58)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_58 |
| Slot | 58 |
| Purpose | Adversarial confirm: (1) no 35=D builder (2) CanPromoteToLive false (3) RealCopyEnabled forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_58.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` **SSRF-blocked**. No live body. |
| Verdict | **FAIL.** (1) FAIL unscoped / PASS_SESSION (`CTraderFixSession` 135/135 is `35=A` only; product `Build("D")` ×5 + hosted hopper). (2) PASS `CanPromoteToLive => false`. (3) FAIL — DI binds `.env` L73 `true`; logon logs, no re-pin. (4) FAIL unscoped (demo dest `ExecuteDemoCopyAsync` now; dest DTO `0` is constructor). (5) PASS — SHADOW paper ≠ dest P&L. Live dest **NONE**. Demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_47 adversarial confirm of five send/profit claims

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_47 |
| Slot | 47 |
| Purpose | Adversarial verify: (1) no 35=D builder (2) CanPromoteToLive false (3) RealCopyEnabled forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproved claims. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_47.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET http://127.0.0.1:5000/api/health` `/api/settings` `/api/copy/status` **blocked** (loopback SSRF / open_page fail). Process bits not remeasured. |
| Verdict | **FAIL.** Claims 1 (session-scoped) / 2 / 4 (booked dest profit) / 5 **PASS** from files. Claim 3 **disproved**: `CTraderFixLogonHostedService` L60–70 does not assign `RealCopyEnabled`; DI L41 binds `.env` L73 `true`. Product residual `Build("D")` ×5 + hosted `ExecuteDemoCopyAsync`. Live `1369850` **NONE** (`SAFE_BY_ABSENCE`). Demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_54 adversarial profit-path verify (slot 54)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_54 |
| Slot | 54 |
| Purpose | Adversarial confirm: (1) no 35=D builder (2) CanPromoteToLive false (3) RealCopyEnabled forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_54.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/{health,settings,copy/status}` **SSRF-blocked**. No live body. |
| Verdict | **FAIL.** Claim 2 PASS. Claim 1 PASS only if scoped to `CTraderFixSession` (`35=A` only; product `Build("D")` ×5 hosted). Claim 3 **disproved** (DI binds `.env` `true`; logon host logs, no re-pin). Claim 4 PASS_NOT_BOOKED_DEST_PROFIT (persist `AllowFixSend=false`; dest DTO constructor 0; residual demo hopper). Claim 5 PASS (SHADOW/slippage ≠ dest cash). Risk **NONE** on live `1369850`; demo dest send **wired**. |

---

## 2026-08-18 — P500_VERIFY_42 adversarial: session / promote / RealCopy pin / send-as-profit / SHADOW≠dest

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_42 |
| Slot | 42 |
| Purpose | Adversarial confirm: no 35=D builder; CanPromoteToLive false; RealCopyEnabled forced false after logon; sending now cannot be the profit path; SHADOW on demo is not dest profit. FAIL any unproven claim. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_42.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** this slot (on-disk prior fill cited only) |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` **blocked** (localhost SSRF) |
| Verdict | **FAIL.** Claim 2 PASS. Claim 1 PASS only on `CTraderFixSession`. Claims 3–4 FAIL. Claim 5 paper-only / dest-class FAIL. Risk: live `1369850` **NONE**; demo dest **P&L active**. |

---

## 2026-08-18 — P500_VERIFY_49 adversarial confirm (slot 49)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_49 |
| Slot | 49 |
| Purpose | Adversarial verify from assigned files: (1) no 35=D builder; (2) CanPromoteToLive is false; (3) RealCopyEnabled forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. FAIL any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_49.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** (this slot) |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` `/api/copy/status` `/api/settings` `/api/overview` **blocked** (localhost SSRF). |
| Verdict | **FAIL.** Claim 2 PASS. Claim 5 PASS. Claim 1 PASS_SESSION / FAIL unscoped (`Build("D")` ×5 hosted). Claim 3 **disproven** (DI binds `.env` true; logon no re-pin). Claim 4 FAIL unscoped (demo dest hopper). Risk **NONE** on live `1369850`; demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_53 adversarial profit-path verify (slot 53)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_53 |
| Slot | 53 |
| Purpose | Adversarial confirm: (1) no 35=D builder (2) CanPromoteToLive false (3) RealCopyEnabled forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_53.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/{health,copy/status}` **SSRF-blocked**. No live body. |
| Verdict | **FAIL.** (1) FAIL unscoped / PASS_SESSION. (2) PASS. (3) FAIL — DI binds `.env` true; no logon re-pin. (4) FAIL — demo hopper sends now; dest DTO 0 unproven. (5) PASS — SHADOW paper ≠ dest P&L. Live dest **NONE**. Demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_52 adversarial confirm (slot 52)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_52 |
| Slot | 52 |
| Purpose | Adversarial verify from assigned files: (1) no 35=D builder; (2) CanPromoteToLive is false; (3) RealCopyEnabled forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. FAIL any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_52.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** (this slot) |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` `/api/copy/status` `/api/settings` **blocked** (localhost SSRF). |
| Verdict | **FAIL.** Claim 2 PASS (`CanPromoteToLive=>false`). Claim 5 PASS (SHADOW/slippage ≠ dest cash). Claim 1 FAIL unscoped (`CopyOpen.Build("D")` hosted). Claim 3 **disproven** (DI binds `.env` L73 `true`; logon does not re-pin). Claim 4 FAIL (`ExecuteDemoCopyAsync` dest hop + ledger 305750 open). Risk **NONE** on live `1369850`; demo dest send **wired**. This slot sent **0**. |

---

## 2026-08-18 — P500_VERIFY_46 adversarial confirm (slot 46)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_46 |
| Slot | 46 |
| Purpose | Adversarial verify from assigned files: (1) no 35=D builder; (2) CanPromoteToLive is false; (3) RealCopyEnabled forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. FAIL any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_46.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** (this slot) |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` `/api/copy/status` `/api/settings` **blocked** (localhost SSRF). |
| Verdict | **FAIL.** Claims 2 and 5 PASS from files. Claim 1 FAIL unscoped (`CTraderFixSession` is `35=A` only; sibling `Build("D")` hosted). Claim 3 **disproven**: DI binds `.env` L73 `true`; logon host logs, no re-pin. Claim 4 **disproven**: 20s `ExecuteDemoCopyAsync` → dest `35=D` on demo `5328266`; ledger 305750/`237339770` open. Risk **NONE** on live `1369850`; demo dest send **wired**. |

---

## 2026-08-18 — P500_VERIFY_38 adversarial confirm of five send/profit claims

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_38 |
| Slot | 38 |
| Purpose | Adversarial verify from assigned files: (1) no 35=D builder; (2) CanPromoteToLive is false; (3) RealCopyEnabled forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. FAIL any unproven claim. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_38.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` `/api/settings` `/api/copy/status` **blocked** (localhost SSRF / retrieve fail). |
| Verdict | **FAIL.** Claim 1 PASS_SESSION / FAIL_UNSCOPED. Claim 2 PASS (`CanPromoteToLive => false`). Claim 3 **disproved**: DI binds `.env` L73 `true`; logon host logs, no re-pin. Claim 4 PASS_NOT_BOOKED_DEST_PROFIT. Claim 5 PASS (SHADOW ≠ dest PnL). Live `1369850` **NONE**. Demo dest hop **wired**. |

---

## 2026-08-18 — P500_VERIFY_36 adversarial confirm (slot 36)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_36 |
| Slot | 36 |
| Purpose | Adversarial verify from live files: (1) no 35=D builder; (2) CanPromoteToLive is false; (3) RealCopyEnabled forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. FAIL any unproven claim. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_36.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` **blocked** (localhost SSRF / retrieve error). |
| Verdict | **FAIL.** Claims 1 (session-scoped) / 2 / 5 file-proven. Claim 3 **disproved**: `.env` L73 `true` + `DependencyInjection.cs` L41 bind; `CTraderFixLogonHostedService` logs `RealCopyEnabled` and never assigns false. Claim 4 **FAIL**: demo dest `ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.Build("D")` exists; dest DTO `0` is not dest cash; no live GET. `CTraderFixSession` 135/135 is `35=A` only. `CanPromoteToLive => false`. SHADOW is source/paper. Live `1369850` refused. Risk to capital **NONE** on live; demo dest hop wired. |

---

## 2026-08-18 — P500_VERIFY_37 adversarial profit-path (slot 37)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_37 |
| Slot | 37 |
| Purpose | Adversarial confirm: no 35=D builder; CanPromoteToLive false; RealCopyEnabled forced false after logon; sending now cannot be the profit path; SHADOW on demo is not dest profit. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_37.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** this slot (on-disk prior fill cited only) |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` and `/api/copy/status` **blocked** (localhost SSRF) |
| Verdict | **FAIL.** Claim 2 PASS. Claim 1 PASS only on `CTraderFixSession`. Claims 3–5 FAIL as written. Risk: live `1369850` **NONE**; demo dest **P&L active**. |

---

## 2026-08-18 — P500_VERIFY_28 adversarial profit-path verify (slot 28)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_28 |
| Slot | 28 |
| Purpose | Adversarial confirm: (1) no 35=D builder (2) CanPromoteToLive false (3) RealCopyEnabled forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_28.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/{health,copy/status,overview,settings,risk}` **SSRF-blocked**. No live body. |
| Verdict | **FAIL.** (1) FAIL unscoped / PASS_SESSION. (2) PASS. (3) FAIL — DI binds `.env` true; no logon re-pin. (4) FAIL — demo hopper sends now; dest DTO 0 unproven. (5) PASS — SHADOW paper ≠ dest P&L. Live dest **NONE**. Demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_14 adversarial confirm of five send/profit claims

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_14 |
| Slot | 14 |
| Purpose | Adversarial verify: (1) no 35=D builder (2) CanPromoteToLive false (3) RealCopyEnabled forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproved claims. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_14.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET http://127.0.0.1:5000/api/copy/status` **blocked** (loopback SSRF / open_page fail). Process bits not remeasured. |
| Verdict | **FAIL.** Claims 1 (session-scoped) / 2 / 4 (live hop) / 5 **PASS** from files. Claim 3 **disproved**: `CTraderFixLogonHostedService` L60–70 does not assign `RealCopyEnabled`; DI L41 binds `.env` L73 `true`. Product residual `Build("D")` ×5 + hosted `ExecuteDemoCopyAsync`. Live `1369850` **NONE** (`SAFE_BY_ABSENCE`). Demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_26 adversarial confirm (slot 26)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_26 |
| Slot | 26 |
| Purpose | Adversarial verify from assigned files: (1) no 35=D builder; (2) CanPromoteToLive is false; (3) RealCopyEnabled forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. FAIL any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_26.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** (this slot) |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` `/api/copy/status` `/api/settings` **blocked** (localhost SSRF). |
| Verdict | **FAIL.** Claims 1 (session-scoped), 2, 4 (live-capital hop), 5 PASS from files. Claim 3 **disproven**: DI binds `.env` L73 `true`; `CTraderFixLogonHostedService` logs `RealCopyArmed` and does not re-pin. Sibling `Build("D")` on demo dest. Risk **NONE** on live `1369850`; demo dest send **wired**. |

---

## 2026-08-18 — P500_VERIFY_11 adversarial profit-path (slot 11)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_11 |
| Slot | 11 |
| Purpose | Adversarial confirm: no 35=D builder; CanPromoteToLive false; RealCopyEnabled forced false after logon; sending now cannot be the profit path; SHADOW on demo is not dest profit. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_11.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** this slot (on-disk prior fill cited only) |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` and `/api/copy/status` **blocked** (localhost SSRF) |
| Verdict | **FAIL.** Claims 2 PASS. Claim 1 PASS only on `CTraderFixSession`. Claims 3–4 FAIL. Claim 5 paper-only. Risk: live `1369850` **NONE**; demo dest **P&L active**. |

---

## 2026-08-18 — P500_VERIFY_10 adversarial (CTraderFixSession / scorer / risk / LiveCopyPage)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_10 |
| Slot | 10 |
| Purpose | Adversarial verify from live files: (1) no 35=D builder; (2) CanPromoteToLive is false; (3) RealCopyEnabled forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. FAIL any unproven claim. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_10.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true`) |
| Live API this pass | `GET :5000/api/health` `/api/copy/status` `/api/settings` `/api/ingest/status` **blocked** (localhost SSRF). |
| Verdict | **FAIL.** Claims 1/2/4/5 file-proven (1 and 4 scoped). Claim 3 **disproven**: `.env` L73 `true` + `DependencyInjection.cs` L41 bind; `CTraderFixLogonHostedService` logs `RealCopyEnabled` and never assigns false. `CTraderFixSession` 135/135 is `35=A` only. `CanPromoteToLive => false`. Dest constructor `0`. SHADOW is source/paper. Demo dest hop (`ExecuteDemoCopyAsync` → `Build("D")`) is dest exposure, not dest profit. Live `1369850` refused. Risk to capital **NONE** on live. |

---

## 2026-08-18 — P500_VERIFY_22 adversarial (slot 22)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_22 |
| Slot | 22 |
| Purpose | Adversarial re-read: `CTraderFixSession`, `BaselineScorer`, `RiskEngine`, `LiveCopyPage`. Confirm (1) no 35=D builder (2) `CanPromoteToLive` false (3) `RealCopyEnabled` forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL unproven claims. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_22.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/{copy/status,health,settings,overview}` **blocked** (localhost SSRF). File proof only. |
| Verdict | **FAIL.** Claims 1 (session-scoped), 2, 4, 5 **PASS**. Claim 3 **FAIL** (`.env` L73 `true` + DI L41; logon read-only). Residual hosted demo `Build("D")`. Risk **NONE** on live 1369850 (`SAFE_BY_ABSENCE`); demo dest **not** absent. |

---

## 2026-08-18 — P500_VERIFY_29 adversarial (slot 29)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_29 |
| Slot | 29 |
| Purpose | Adversarial verify of five claims from `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`. FAIL any claim not proven from a file or live GET. No secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_29.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/health`, `/api/copy/status`, `/api/settings` **blocked** (localhost SSRF). |
| Verdict | **FAIL.** Claim 1 PASS_FILE (`35=A` only in assigned session) / FAIL unscoped (`CopyOpen.Build("D")` hosted). Claim 2 PASS (`CanPromoteToLive => false`). Claim 3 FAIL (logon does not re-pin; DI binds `.env` `true`). Claim 4 FAIL (hosted demo hopper is a dest P&L path; ledger 305750 / 237339770 @ 0.01). Claim 5 FAIL (SHADOW is AUTO_ADMIT class; send ignores LIVE). Live `1369850` **NONE**. Demo dest **not** `SAFE_BY_ABSENCE`. W500_VERIFY_29 copy-hop absence pin **STALE**. |

---

## 2026-08-18 — P500_VERIFY_23 adversarial: session / promote / RealCopy pin / send-as-profit / SHADOW≠dest

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_23 |
| Slot | 23 |
| Purpose | Adversarial confirm: (1) no 35=D builder (2) CanPromoteToLive false (3) RealCopyEnabled forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL any unproven claim. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_23.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health`, `/api/settings`, `/api/copy/status` **blocked** (localhost SSRF). File-only for claim 3. Ledger remasured: 305750 @ 0.01 dest 237339770 still open. |
| Verdict | **FAIL.** Claims 2 and 5 PASS from files. Claim 1 FAIL as product (session is 35=A only; siblings + hosted hop `Build("D")`). Claim 3 **disproven**: DI binds `.env` L73 `true`; logon host does not re-pin. Claim 4 FAIL: demo dest send is the dest path; DTO dest PnL `0` is not venue proof. Risk to capital **NONE on live 1369850**; demo dest residual (not this slot). |

---

## 2026-08-18 — P500_VERIFY_24 adversarial five-claim confirm

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_24 |
| Slot | 24 |
| Purpose | Adversarial confirm: no `35=D` builder; `CanPromoteToLive` false; `RealCopyEnabled` forced false after logon; sending not profit path; SHADOW on demo not dest profit. FAIL unproven claims. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_24.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** (this slot) |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` `/api/copy/status` `/api/settings` `/api/ingest/status` `/api/risk` **blocked** (localhost SSRF). |
| Verdict | **FAIL.** Claim 2 PASS. Claim 1 PASS only if scoped to `CTraderFixSession`. Claims 3–5 **FAIL** (flag not re-pinned; hosted demo dest hop; SHADOW AUTO_ADMIT). Risk **NONE** on live `1369850`; **not** absent on demo dest. |

---

## 2026-08-18 — P500_VERIFY_19 adversarial confirm (slot 19)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_19 |
| Slot | 19 |
| Purpose | Adversarial verify from assigned files: (1) no 35=D builder; (2) CanPromoteToLive is false; (3) RealCopyEnabled forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. FAIL any unproven claim. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_19.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live GET | `http://127.0.0.1:5000/api/health` **blocked** (loopback SSRF). File proof only. |
| Verdict | **FAIL.** Claims 1/2/5 PASS from files. Claim 4 PASS_NOT_BOOKED_DEST_PROFIT (`CTraderFixSession` `35=A` only; persist `AllowFixSend=false`; dest constructor $0; residual demo `Build("D")`). Claim 3 **disproved**: DI binds `.env` `true`; logon host no re-pin. Risk **NONE** on live `1369850`; demo dest send **wired**. |

---

## 2026-08-18 — P500_VERIFY_16 adversarial confirm of five live-path claims

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_16 |
| Slot | 16 |
| Purpose | Adversarial verify: (1) no 35=D builder (2) CanPromoteToLive false (3) RealCopyEnabled forced false after logon (4) sending now cannot be the profit path (5) SHADOW on demo is not dest profit. FAIL any unproven claim. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_16.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET http://127.0.0.1:5000/api/health` **blocked** (localhost SSRF). No claim proven from live GET. |
| Verdict | **FAIL.** Claim 1 FAIL unscoped (`CTraderFixSession` PASS_SESSION `35=A` only; product `Build("D")` hosted). Claim 2 PASS (`CanPromoteToLive => false`). Claim 3 FAIL (no logon re-pin; DI binds `.env` true). Claim 4 PASS (not a dest-profit path). Claim 5 PASS (SHADOW/demo ≠ dest). Risk **NONE** on live 1369850; demo dest hop **exists**. |

---

## 2026-08-18 — P500_VERIFY_21 adversarial: 35=A / CanPromoteToLive false / RealCopy not re-pinned / send ≠ profit / SHADOW ≠ dest PnL

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_21 |
| Slot | 21 |
| Purpose | Adversarial verify from live files: (1) no 35=D builder; (2) CanPromoteToLive is false; (3) RealCopyEnabled forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. FAIL any unproven claim. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_21.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true`) |
| Live API this pass | `GET :5000/api/health` `/api/copy/status` `/api/overview` `/api/settings` **blocked** (localhost SSRF). |
| Verdict | **FAIL.** Claims 1–2 / 4–5 file-proven (`CTraderFixSession` 135/135 is `35=A` only; `CanPromoteToLive => false`; dest PnL constructor 0; SHADOW is paper/source score). Claim 3 **disproven**: `.env` L73 `true` + DI L41 bind; logon host logs the flag and does **not** re-pin false. Sibling `CTraderFixCopyOpen.Build("D")` exists (hosted demo hop). Risk to capital **NONE** on live `1369850` (`SAFE_BY_ABSENCE`); demo dest residual. |

---

## 2026-08-18 — P500_BOOK_197 allocation must stay 0.01–0.05 until dest shadow EV after costs is positive

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_197 |
| Slot | 197 |
| Purpose | Measured evidence: allocation factor must be tiny (0.01–0.05 of source) until shadow expectancy after costs is positive. Higher profit / lower loss. Honesty: wanting profit is not an edge; copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_197.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-day pin (`P500_S007` / synthesis: 8463 / XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**). Manager census independently re-summed 18/8460. Ledger: 305750 @ 0.01 dest 237339770 still open. |
| Verdict | **ALLOCATION_MUST_STAY_TINY.** HEAD `AllocationFactor=1m` (1:1) is dest-ruin if sent. Dest 0.05 ticket cap **MISSING** on policy/shadow/flatten. `MaxAutoLots=0.05` is the **inverse** (1:1-sends ≤0.05 scalps). Shadow EV after costs **not proven** (hosted hop `VenueReconciled=false` → `VENUE_NOT_RECONCILED`; dest PnL $0). HEAD **AUTO_ADMITS** demo/contest at α=1. Copy-all 8463 copies `RISK_BLOCKED` −$241k. Tiny α shrinks the hole; it does not mint an edge. Slot 17 `0.50×0.01=0.01` cell **wrong** (SUT **0**). BOOK_157 `NOS=false` / product `35=D=0` **STALE**. BOOK_177 on disk, was unindexed. Risk to capital **NONE** on live 1369850 (`SAFE_BY_ABSENCE`); **not** absent on demo dest. |

---

## 2026-08-18 — P500_BOOK_196 FIX quote bid/ask are null; cannot size or guard spread

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_196 |
| Slot | 196 |
| Purpose | Measured evidence: FIX quote bid/ask are null. Cannot size or guard spread without a quote tape. Higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_196.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** (this slot) |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0 / bid/ask **null**). |
| Verdict | **NO_TAPE_NO_SIZE_NO_SPREAD_GUARD.** Live DTO bid/ask/age **null**. Hosted FIX is one-shot `35=A` then dispose — no QUOTE `35=x`/`35=V`. `CTraderQuoteService` **0 callers**, not in DI. Only `DestinationQuotes.Add` is `DemoSeeder` forged 2399.45/2399.85 (`VenueInstrumentId=null`); live host uses `BrokerCatalogSeed` (no quote row). `QuantityNormalizer` has no quote params; HEAD `AllocationFactor=1m`. `SPREAD_TOO_WIDE` cannot fire; shadow hop hits `VENUE_NOT_RECONCILED` first; `MaxSlippage` unread. HEAD `CopyGroupFilter` **admits demo/contest only**. **HEAD `ExecuteDemoCopyAsync` can emit `35=D` after TRADE-session SecurityList without reading bid/ask** (BOOK_156 hop-absent / `NOS=const false` **STALE**). Live `1369850` refused. `shadowPnl=0` is absence. Copy-all 8463 copies `RISK_BLOCKED` −$241,580. Risk to capital **NONE** on live 1369850 (`SAFE_BY_ABSENCE`); **HIGH** if demo dest / 1:1 / `--copy-open` armed against a null book. |

---

## 2026-08-18 — P500_BOOK_190 SHADOW 100% demo; no real Starwave/contest in measured copy set

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_190 |
| Slot | 190 |
| Purpose | Measured evidence for higher profit / lower loss. Topic: SHADOW group is 100 percent demo. No real Starwave or contest live book in the copy set. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_190.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` + `P500_S004` (49+ / 6 demo SHADOW, 0 contest / Starwave / real) + `P500_S007` (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**) + Manager census 18/8460 remasured. Dest ledger remasured: **305750** `demo\yo-2step` 0.01 on demo `5328266`. |
| Verdict | **CONFIRMED_SHADOW_100PCT_DEMO; NO_REAL_STARWAVE_OR_CONTEST_IN_MEASURED_COPY_SET; HEAD_ADMITS_DEMO_CONTEST_REJECTS_REAL; DEST_FILL_IS_DEMO_YO_2STEP; COPY_ALL_8463_NEGATIVE_EV.** 70 SHADOW = `demo\yo-2step`/`demo\yo-payp`. Contest 190 not in copy set. Starwave real 28 scored=0 + HEAD-rejected. Only dest fill is 305750 challenge demo. Copy-all 8463 copies `RISK_BLOCKED` −$241,580 + 8266 un-scored. Dest constructor $0. Risk **NONE** on live `1369850` (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_191 RISK_BLOCKED source PnL is hundreds of thousands negative

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_191 |
| Slot | 191 |
| Purpose | Measured evidence for higher profit and lower loss. Topic: RISK_BLOCKED source PnL is hundreds of thousands negative. Copying them is how the venue blows up. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_191.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave P500 pin (8463 / XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**). Manager census 18/8460 remasured from JSON. Unscored remainder remasured **8266** (`8463−197`). Seed 10002 risk remasured **70**. |
| Verdict | **NEVER_COPY_RISK_BLOCKED; COPY_ALL_8463_DEST_RUIN; SAFE_BY_ABSENCE.** Tail −$241,580 (29, all martingale, mean −$8,330) > SHADOW+WATCH +$86,454 (3.09× SHADOW). Copy-all EV = scored XAU −$154,425. Hopper `{SHADOW,LIVE_CANDIDATE,LIVE}` L202–203 + triple policy gate + roster dest-flatten (wired, paper). RiskEngine 0 `TRADER_RISK_BLOCKED`. Dest PnL constructor 0. Hosted `35=A` only. Off-hop `Build("D")` ×5 refuses live `1369850` (BOOK_171 tree-wide `35=D=0` **STALE**). HEAD demo-required is not a tail filter. Risk to capital **NONE** today; **HIGH / ruin** if blocked tail or catalog 8463 is sent 1:1. |

---

## 2026-08-18 — P500_BOOK_198 never flatten MT5 source; dest-only flatten

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_198 |
| Slot | 198 |
| Purpose | Measured evidence for higher profit / lower loss: never flatten the MT5 source; flatten is destination-only. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_198.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** (this slot). Live `1369850` refused. Demo dest hop exists; not called. |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Localhost API | Not re-probed (SSRF block on 127.0.0.1). Used Manager pin 8460 + synthesis 8463 / RISK_BLOCKED 29 / −$241,580. |
| Verdict | **DEST_ONLY_FLATTEN_LAW.** C# Manager GET-only (0 DealerSend). Roster WIRED dest `FLATTEN_LOSS_CUT` only. Venue flatten on REMOVE missing. `ShouldFlattenOpenCopy` DEAD. NOS=>DemoDest (158 STALE). Copy-all 8463 copies RISK_BLOCKED −$241,580 inside scored XAU −$154,425. Live dest NONE (`SAFE_BY_ABSENCE`). Wanting profit ≠ edge. |

---

## 2026-08-18 — P500_BOOK_195 in-memory DB: scores vanish on restart; cannot run a live book

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_195 |
| Slot | 195 |
| Purpose | Measured evidence for higher profit / lower loss. In-memory EF: scores vanish on restart. Cannot run a live book on RAM. Honesty: wanting profit ≠ edge. Copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_195.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**). Manager census independently re-summed 18/8460. |
| Verdict | **BLOCK_NO_LIVE_BOOK_ON_RAM.** DI fail-open `UseInMemoryDatabase("trader-intelligence-live")` when CS empty / `<SECRET>`. 0 `Migrations/`; **20** DbSets; empty `Configurations/`; `EnsureCreated` ×3; workers skip `EnvFile`; Compose Postgres unwired; health `healthy:true` constant. Hopper **L202–205** dies with scores. Persist `AllowFixSend=false` **L324**. `NOS => DemoDest` L50 (BOOK_155 const-false **STALE**). Lab `.env` **is** DemoDest; file ledger 1 open 0.01 survives kill. HEAD policy **requires** demo/contest. Copy-all 8463 imports −$241k tail. Live dest **NONE** (`SAFE_BY_ABSENCE` + `1369850` refuse); demo dest residual. **HIGH** if send armed on InMemory / copy-all. |

---

## 2026-08-18 — P500_BOOK_199 official FIX trade-copier listing is not a license to send

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_199 |
| Slot | 199 |
| Purpose | Trade-copier on cTrader FIX is officially listed; Spotware says other APIs may fit copy better. Still no license to send today. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_199.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**) + Manager census 18/8460 re-summed. Official Help + RoE + Open API terms re-fetched. |
| Verdict | **NO_LICENSE; COPY_ALL_8463_NEGATIVE_EV; DEMO_DEST_SEND_WIRED_NOT_LIVE_LICENSE.** Official https://help.ctrader.com/fix/ lists trade copiers then: “other Spotware APIs are more suitable.” RoE has TRADE `35=D`. Live hop `SAFE_BY_ABSENCE` (`CTraderFixSession` `35=A` only; CopyOpen refuses `1369850`). HEAD `NOS => DemoDest`; hosted tick can `Build("D")` on demo `5328266` without `Evaluate` (BOOK_159 product-`35=D=0` **STALE**). §68 0/19; §70 0/14. Open API terms still require trader-explicit approval. HEAD `CopyGroupFilter` **requires** demo/contest. Copy-all 8463 would copy `RISK_BLOCKED` −$241,580 inside scored XAU −$154,425. Dest PnL $0. Risk to live capital **NONE** (`SAFE_BY_ABSENCE` on `1369850`). |

---

## 2026-08-18 — P500_BOOK_187 XAUUSD copy cost: spread + slippage + 15s MaxSourceSignalAge. Scalps die.

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_187 |
| Slot | 187 |
| Purpose | Measured evidence for higher dest profit / lower dest loss. Topic: XAUUSD copy cost = dest spread + slippage + 15s `MaxSourceSignalAge` reject. Scalps die. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_187.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| SUT | `RiskEngine` L14–18 / L95–115; hosted tick 8s+20s + `ExecuteDemoCopyAsync`; hop `OpenedAt` L270/L300; `AllowFixSend=false` L324; `MaxSlippage` unread (1 src hit); `CopyIntentExpiry` unused; `AverageHoldSeconds` unused; HEAD admits demo/contest |
| Prior same-topic | 7/27/47/67/87/107/127/147. **167 missing.** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = synthesis 8463 / XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**. Manager census 18/8460. 322947 JSON remasure `demo\yo-payp` 104949.8. |
| Verdict | **SCALPS_DIE_AFTER_COSTS; DEMO_SEND_BYPASSES_15S; COPY_ALL_8463_NEGATIVE_EV.** Dest taker spread (seed 0.40 / allowed 2.0 = $40–$200 per 1.00 lot) + unread `MaxSlippage=1.5` + 15s `SIGNAL_STALE` on Evaluate OPEN. Hosted poll **20 s > 15 s** (≥25% first-sight miss). Roster **no hold gate**. HEAD **admits demo/contest**. Incremental: `ExecuteDemoCopyAsync` can emit demo `35=D` with **no** Evaluate / bid-ask / age (live `1369850` refused). 322947 ~163s / +$4,950 is source demo, not dest EV. Copy-all 8463 imports `RISK_BLOCKED` **−$241,580** (29). Dest **$0**. Risk to capital **NONE** on live 1369850 (`SAFE_BY_ABSENCE`); **HIGH** if demo dest / scalps / copy-all sent 1:1. |

---

## 2026-08-18 — P500_BOOK_194 ML is not built; deterministic baseline only

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_194 |
| Slot | 194 |
| Purpose | Measured evidence for higher dest profit / lower dest loss. Topic: **ML is not built.** Do not invent a model. Deterministic baseline only. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_194.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**). Manager census 18/8460. |
| Verdict | **ML_NOT_BUILT; DETERMINISTIC_BASELINE_ONLY; DO_NOT_INVENT_MODEL; COPY_ALL_8463_IMPORTS_RISK_BLOCKED; HEAD_DEMO_REQUIRED_ADVERSE; BOOK_154_NOS_STALE.** `services/` empty; `src` 0 XGBoost/`IScoringService`; `mlProbability` literal null; dest PnL constructor 0; `CanPromoteToLive=false`; persist `AllowFixSend=false` **L324**; hopper **L202**; `NOS => DemoDest` L50 (BOOK_154 const-false **STALE**); product logon hop `35=A` only; hosted demo hop can `35=D`; live `1369850` refused. HEAD policy **requires** demo/contest. Copy-all **8463** copies `RISK_BLOCKED` **−$241,580**. Wanting profit is not an edge. Risk to capital **NONE** on live 1369850 (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_192 Persist ClOrdID before send; unknown must not retry (lower-loss, not higher-profit)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_192 |
| Slot | 192 |
| Purpose | Persist `ClOrdID` before send. Unknown state must not retry. That is lower-loss, not higher-profit. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge; copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_192.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only public dest ids `5328266` / `1369850` and boolean `REAL_COPY_EXECUTION_ENABLED=true`) |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-day pin (8463 / −$154,425 / RISK_BLOCKED 29 / −$241,580 / dest $0). Manager census 18/8460. |
| Verdict | **LOWER_LOSS_NOT_HIGHER_PROFIT; UNKNOWN_MUST_NOT_RETRY; SYSTEM_ARM_MISSING; LAB_DEMODEST_ARMED.** Helper `MayRetry(unknown)=false`; 0 product callers; 0 `ExecutionIntent` writers; factory clock-based; no `35=H`. Lab `.env` satisfies `DemoDest` (`5328266`); hosted tick persist-after-fill + retry-unknown-with-new-11. Copy-all 8463 copies `RISK_BLOCKED` −$241,580. Dest PnL $0. Live `1369850` risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_193 MFE/MAE FeatureQuality Unavailable; exact excursion unused

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_193 |
| Slot | 193 |
| Purpose | MFE/MAE `FeatureQuality` is Unavailable. Exact excursion not used. Do not claim MAE-based stops. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_193.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Local API | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-day P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). |
| Verdict | **FEATURE_QUALITY_UNAVAILABLE; EXACT_EXCURSION_UNUSED; NO_MAE_STOPS.** Scorer always stamps `MaeMfeQuality=Unavailable`; `AverageMfe`/`AverageMae` null; `Score()` never reads them. A22 MAE floors not wired (`FLAG_MAE`/`mfe_mae_used` = 0 hits in `src`); `MfeMaeCalculator` + `mt5_xau_ticks` MISSING. Copy hopper SL = `FinalSl ?? InitialSl` (**L252**). Reconstructor L234 is `deal.StopLoss` clone, not MAE. Demo `35=D` extra tags have **no SL/TP**. Persist `AllowFixSend=false` L324. Hopper L202. `NOS => DemoDest` L50. D57 VWAP mutation scores identical. Copy-all **8463** would copy `RISK_BLOCKED` **−$241,580**. Wanting profit is not an edge. Risk to capital **NONE** (this slot report-only; live hop `SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_182 CTraderFixSession outbound is only 35=A

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_182 |
| Slot | 182 |
| Purpose | Read `CTraderFixSession.cs`. Prove outbound MsgType is only `A`. No `35=D`. Measured evidence for higher profit and lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_182.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| Live API this slot | `GET :5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` + `P500_S007` (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**) + Manager census 18/8460 remasured. |
| Verdict | **PASS_35A_ONLY; COPY_ALL_8463_NEGATIVE_EV; DEMO_HOPPER_WIRED_VS_162.** Assigned 135/135: outbound MsgType `(35,"A")` only; `WriteAsync=1`; `35=D=0`; sockets disposed. Persist overwrite is **L324** (162 L306 stale). `NOS => DemoDest` (162 const-false stale). Hosted hopper can `Build("D")` on demo 5328266; live 1369850 refused. Wanting profit is not an edge. Copy-all 8463 would copy `RISK_BLOCKED` losses (pin 29 / −$241,580 inside scored XAU −$154,425). Dest PnL $0. This slot sent 0. Live risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_189 Starwave scored 0 after 91966 deals; do not size from Achiever-only scores

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_189 |
| Slot | 189 |
| Purpose | Starwave scored 0 while dealsInserted 91966. Book is incomplete. Do not size from Achiever-only scores. Measured evidence for higher profit and lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_189.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = P500 pin (8463 / Starwave **91966 / scored 0 / deals-done** / scored XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**). Manager census 18/8460 remasured from JSON. HEAD remasured independently. |
| Verdict | **BOOK_INCOMPLETE_DO_NOT_SIZE; COPY_ALL_8463_NEGATIVE_EV.** Starwave `deals-done` / `Scored=0` after **91,966** inserts is pipeline order (Achiever scores first), not an empty tape. Deal-share **26.10%** (`91966/352318`) vs score-share **0%**. Dashboard `EarlyScore=0` is a missing join (empty-XAU scorer writes **40/10**). Achiever-only SHADOW **+$78,276** is 100% demo and not a dest size. HEAD `AllocationFactor=1m`; HEAD **admits** `Starwave\demo` (`AUTO_ADMIT`). `NOS => DemoDest` (const-false stale); persist `AllowFixSend=false` **L324**. Copy-all 8463 would copy `RISK_BLOCKED` 29 / **−$241,580** inside scored XAU **−$154,425**, plus **8266** names with no `TraderScore` (entire Starwave catalog). Dest PnL literal **$0**. Risk to live capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_188 MaxDailyExecutionLoss=2000 / MaxLossPerTrader=500 are loss caps, not an edge

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_188 |
| Slot | 188 |
| Purpose | Measured evidence: `MaxDailyExecutionLoss=2000` and `MaxLossPerTrader=500` are loss caps, not an edge. Wanting profit does not create expectancy. Copy-all 8463 would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_188.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Localhost API | Not re-probed (SSRF block on 127.0.0.1). Used on-disk probe 18/8460 + P500 remasure 8463 / RISK_BLOCKED 29 / −$241,580 / dest PnL literal 0. |
| Verdict | **LOSS_CAPS_NOT_EDGE.** $500 / $2,000 fire after dest (or one source ticket) is already lost. Not §40 kill switch. Copy hop zeros `DailyExecutionPnl`; recon short-circuits OPEN; close + flatten skip `Evaluate`. HEAD `ExecuteDemoCopyAsync` can emit demo `35=D` without `Evaluate`. Settings 5%/10-lot unbound; DI singleton unused. Copy-all 8463 copies `RISK_BLOCKED` **−$241,580** inside scored XAU **−$154,425**. Live dest **NONE** (`SAFE_BY_ABSENCE`). Wanting profit ≠ edge. |

---

## 2026-08-18 — P500_BOOK_176 FIX quote bid/ask are null; cannot size or guard spread

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_176 |
| Slot | 176 |
| Purpose | Measured evidence: FIX quote bid/ask are null. Cannot size or guard spread without a quote tape. Higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_176.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** (`ExecuteDemoCopyAsync` / `CTraderFixCopyOpen` **not invoked**) |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave `P500_PROFIT_SYNTHESIS.md` pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0 / bid/ask **null**). |
| Verdict | **NO_TAPE_NO_SIZE_NO_SPREAD_GUARD.** Live DTO bid/ask/age **null**. Hosted FIX is one-shot `35=A` then dispose — no QUOTE `35=x`/`35=V`. `CTraderQuoteService` **0 callers**, not in DI. Only `DestinationQuotes.Add` is `DemoSeeder` forged 2399.45/2399.85 (`VenueInstrumentId=null`); live host uses `BrokerCatalogSeed` (no quote row). `QuantityNormalizer` has no quote params; HEAD `AllocationFactor=1m`. `SPREAD_TOO_WIDE` cannot fire; shadow hop hits `VENUE_NOT_RECONCILED` first (`VenueReconciled=const false`; Evaluate **L291** / Reconciled **L304** / AllowFixSend **L324**). `MaxSlippage` unread. **BOOK_156 STALE:** `ExecuteDemoCopyAsync` is on the 20s tick; `NewOrderSingleImplemented => DemoDest`; `CTraderFixCopyOpen` can `35=D` without reading bid/ask (live `1369850` refused). `shadowPnl=0` is absence. Copy-all 8463 copies `RISK_BLOCKED` −$241,580. Risk to live capital **NONE** (`SAFE_BY_ABSENCE`); **HIGH** if `DemoDest` / 1:1 send is armed against a null book. |

---

## 2026-08-18 — P500_BOOK_178 dest-only flatten (never flatten MT5 source)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_178 |
| Slot | **178** |
| Purpose | Measured evidence for higher profit / lower loss. Never flatten the MT5 source. Destination-only flatten. Honesty: wanting profit ≠ edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_178.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave P500 pin (8463 / XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**). Manager census 18/8460 re-summed from JSON 08:42Z. |
| Verdict | **DEST_ONLY_FLATTEN_LAW.** Source C# path GET-only (0 `DealerSend`; `PositionRequest`/`PositionGetByGroup` only). Roster **WIRED**: dest `FLATTEN_LOSS_CUT` only; never MT5. BOOK_158 482-line / NOS=false hop **STALE** (HEAD `CopyTradingService` **625**; hosted `ExecuteDemoCopyAsync`; dest `35=D` dest-721 only; refuses 1369850). Product FIX flatten run **MISSING**. `FLATTEN_LOSS_CUT` not consumed by demo closer. Copy-all 8463 imports `RISK_BLOCKED` −$241,580 inside scored XAU −$154,425. Dest PnL **$0** (`SAFE_BY_ABSENCE` on live 1369850). HEAD `AllocationFactor=1m` **UNSAFE if sent**. Risk to capital **NONE** today; **DEST_RUIN_IF_SENT** if copy-all / blocked tail / 1:1 dest send. |

---

## 2026-08-18 — P500_BOOK_179 Official FIX trade-copier listing is not a license to send

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_179 |
| Slot | 179 |
| Purpose | Trade-copier on cTrader FIX is officially listed; Spotware says other APIs may fit copy better. Still no license to send today. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_179.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Demo CLI invoked | **No** |
| Secret values printed | **None** (boolean + already-public dest host/account ids only) |
| This-slot `:5000` GET | Blocked (SSRF). Pins from `P500_PROFIT_SYNTHESIS.md` + Manager 18/8460 re-summed. Official Help + Open API terms re-fetched. |
| Verdict | **NO_LICENSE; COPY_ALL_8463_NEGATIVE_EV; DEMO_DEST_SEND_WIRED_NOT_LIVE_LICENSE.** Official https://help.ctrader.com/fix/ lists trade copiers then: “other Spotware APIs are more suitable.” RoE has TRADE `35=D`. Logon hop is `35=A` only; `src/`+`apps/` `35=D=0`. HEAD now wires `ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.Build("D")` on the 20s tick when `DemoDest` (demo host/sender, account ≠ `1369850`). Persist `AllowFixSend=false`. `CanPromoteToLive=false`. §68 0/19; §70 0/14. Open API terms still require trader-explicit approval. Copy-all 8463 would copy `RISK_BLOCKED` −$241,580 inside scored XAU −$154,425. Achiever 100% demo/contest; Starwave real = 28. Dest PnL $0. Risk to live capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_167 XAUUSD copy cost: spread + slippage + 15s MaxSourceSignalAge. Scalps die.

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_167 |
| Slot | 167 |
| Purpose | Measured evidence for higher dest profit / lower dest loss. Topic: XAUUSD copy cost = dest spread + slippage + 15s `MaxSourceSignalAge` reject. Scalps die. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_167.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** (this slot). Prior P504 demo fill 305750/21250421 @ 4390.2 remains open on ledger. |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None** |
| Live API this slot | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-day pin (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **−$241,580** / dest $0). 322947 + 305750 cards remasured from JSON. |
| Verdict | **SCALPS_DIE_AFTER_COSTS; DEMO_SEND_BYPASSES_15S; COPY_ALL_8463_NEGATIVE_EV.** Dest taker spread (seed 0.40 / allowed 2.0 = $40–$200 per 1.00 lot) + unread `MaxSlippage=1.5` + 15s `SIGNAL_STALE` on Evaluate. Hosted poll **20 s > 15 s**. Hop now calls `ExecuteDemoCopyAsync` → product `Build("D")` with **no Evaluate**. `MaxAutoLots=0.05`. BOOK_147 no-sender / L251 pins **STALE**. 322947 ~163s / +$4,950 is source demo, not dest EV. Copy-all 8463 imports `RISK_BLOCKED` **−$241,580** (29). Dashboard dest **$0**. Live **1369850 NONE**; demo **5328266 not SAFE_BY_ABSENCE**. Risk if scalps / copy-all sent: **HIGH**. |

---

## 2026-08-18 — P500_BOOK_183 TradeReconstructor / 303274 same-second 0.05 grid

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_183 |
| Slot | **183** |
| Purpose | Read `TradeReconstructor` and 303274-style overlapping 0.05-lot same-second entries. Is grid flagged? Evidence for higher profit / lower loss. Do not modify product. Never enable REAL_COPY. Never send 35=D. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_183.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **SSRF-blocked**. Book integers from `P500_PROFIT_SYNTHESIS.md` + Manager census 18/8460. Catalog 303274 re-read. Demo ledger re-read (`305750` / 0.01 only). |
| SUT | `TradeReconstructor.cs` 347 lines; `GroupBy(PositionId)` L46 + `ScaleIn` worse-than-VWAP latch only; `src/**/*.cs` grep `grid` = **0**; `src`/`tests` grep WasGrid/IsGrid/same-second = **0** |
| HEAD vs BOOK_143 | Grid hole **same**. Hop **drift**: `CopyTradingService` **625** lines; `NewOrderSingleImplemented => DemoDest`; hosted L30 `ExecuteDemoCopyAsync`; `MaxAutoLots=0.05` **selects** 303274 tickets; `CTraderFixCopyOpen` is **on-hop**. Persist `AllowFixSend=false` is **L324**. Live `1369850` still refused. Agrees with BOOK_163 remasure. |
| Catalog | login **303274** `demo\yo-2step` 16228.24 (`LIVE_GROUPS_AND_TRADERS.json` L2564–2568) |
| Verdict | **GRID_NOT_FLAGGED.** Distinct hedge 0.05s never `ScaleIn`. No `WasGrid`. 303274-class averaging/martingale false; SHADOW reachable. Demo+roster **admit** is not a grid detector. `MaxAutoLots=0.05` is not a grid flag. Copy-all 8463 would copy `RISK_BLOCKED` losses (−$241,580). Live dest **NONE**; demo dest **HIGH** if `DemoDest`+`ADMITTED`. This slot did not send. |

---

## 2026-08-18 — P500_BOOK_181 RiskEngine reject reasons that cut dest loss if live send existed

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_181 |
| Slot | 181 |
| Purpose | Read `RiskEngine.cs`. List every reject reason that reduces dest loss if live send existed. Measured evidence for higher dest profit / lower dest loss. Honesty: wanting profit ≠ edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_181.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** (this slot) |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = synthesis 8463 / XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest constructor **$0**. Manager census 18/8460. Demo ledger **0.01** lot open (`305750`). |
| Verdict | **16/19_NEW_EXPOSURE_CUT; 3_TRAP_EXITS; 0_TRADER_RISK_BLOCKED; DEMO_SEND_BYPASSES_EVALUATE; COPY_ALL_8463_DEST_RUIN; LIVE_SAFE_BY_ABSENCE.** SUT 190 lines / 19 `return Reject(`. Engine grep `RISK_BLOCKED`/`TraderState`=0. Hop Evaluate **L291** / persist `AllowFixSend=false` **L324**. `NewOrderSingleImplemented => DemoDest`. Hosted `ExecuteDemoCopyAsync` can `35=D` without Evaluate; live `1369850` refused. Policy **requires** demo/contest. BOOK_161 “NOS=false / 35=D writers=0” **STALE**. Risk to capital **NONE** on live `1369850`; **HIGH / ruin** if blocked tail or catalog 8463 is sent 1:1. |

---

## 2026-08-18 — P500_BOOK_174 ML is not built; deterministic baseline only

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_174 |
| Slot | 174 |
| Purpose | ML is not built. Do not invent a model. Deterministic baseline only. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_174.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **−$241,580** / dest PnL **$0**). Manager census 18/8460. |
| HEAD vs BOOK_154 | `NewOrderSingleImplemented => DemoDest` (L50). Hosted tick `ExecuteDemoCopyAsync`. Persist `AllowFixSend=false` **L324**. `CTraderFixCopyOpen` `Build("D")` demo-only; refuses live `1369850`. BOOK_154 `NOS=false` **STALE**. Still **not** ML. |
| Verdict | **ML_NOT_BUILT; DETERMINISTIC_BASELINE_ONLY; DO_NOT_INVENT_MODEL; COPY_ALL_8463_IMPORTS_RISK_BLOCKED; HEAD_REQUIRES_DEMO_NOT_EDGE; BOOK_154_NOS_FALSE_STALE.** `services/` empty; `src` 0 XGBoost/`IScoringService`; `mlProbability` literal null; dest PnL constructor 0; `CanPromoteToLive => false`. Copy-all 8463 copies `RISK_BLOCKED` −$241,580. Wanting profit ≠ edge. Dest dashboard $0. Risk **NONE** on live 1369850 (`SAFE_BY_ABSENCE`). DemoDest hop can emit `35=D` independently of ML (this slot did not invoke). |

---

## 2026-08-18 — P500_BOOK_186 prop-challenge demo is adverse selection

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_186 |
| Slot | 186 |
| Purpose | Measured evidence: copying prop-challenge demo accounts is adverse selection. Most accounts exist to pass a profit target then blow. Higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_186.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** (this slot). Live `1369850` refused. Demo dest hop exists; not called. |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Localhost API | Not re-probed (SSRF block on 127.0.0.1). Used on-disk probe 18/8460 + P500 remasure 8463 / RISK_BLOCKED 29 / −$241,580 + `demo_copy_ledger.json`. |
| Verdict | **ADVERSE_SELECTION_CONFIRMED. HEAD_SELECTS_CHALLENGE_FACTORY. COPY_ALL_8463_NEGATIVE_EV. DEMO_DEST_SEND_PATH_EXISTS.** 6295/6512 Achiever = `demo\yo-2step`. Combined 8417/8460 (99.49%) challenge/demo/contest. SHADOW 70 / +$78,276 is 100% demo. `RISK_BLOCKED` 29 / −$241,580 (3.09× head) inside scored XAU −$154,425. HEAD AUTO_ADMITs demo/contest and rejects real. Copy-all 8463 copies the blow. Ledger already filled 305750 (`demo\yo-2step` $1,015.98) on demo dest. Live dest **NONE**. Demo dest **NOT** `SAFE_BY_ABSENCE`. Wanting profit ≠ edge. |

---

## 2026-08-18 — P500_BOOK_185 official FIX QUOTE 5211 TRADE 5212 TargetCompID cServer; Logon is not a fill

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_185 |
| Slot | 185 |
| Purpose | Measured evidence: official cTrader FIX QUOTE 5211 / TRADE 5212 / TargetCompID cServer. Logon is not a fill. Higher profit / lower loss. Honesty: wanting profit is not an edge; copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_185.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None** |
| Local API | Not re-probed (SSRF to 127.0.0.1 blocked). Pins from synthesis 8463 / RISK_BLOCKED 29 / −$241,580 / dest $0 + Manager 18/8460. |
| Verdict | **CONFIRMED_OFFICIAL_PORTS_AND_COMPID. LOGON_IS_NOT_A_FILL. NO_DEST_EDGE. COPY_ALL_8463_NEGATIVE_EV.** Official SSL 5211/5212; issued `cServer` (RoE table `CSERVER`; no silent fold). Product probe is one-shot `35=A` then dispose. HEAD DemoDest helper exists; live `1369850` refused. Copy-all 8463 copies `RISK_BLOCKED` −$241k. Dest $0. Risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_184 architecture §3 dest-net not first-3 dollars

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_184 |
| Slot | 184 |
| Purpose | Read architecture §3 business goal. Future destination-net PnL is the target, not first-3 dollars. Measured evidence for higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_184.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Localhost API this slot | **Attempted, blocked.** `GET :5000/api/overview` and `/api/traders` SSRF-blocked (`127.0.0.1`). Book integers = P500 pin 8463 / XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**. Manager census 18/8460 remasured from JSON. |
| Verdict | **PASS_§3_DEST_NET_NOT_FIRST3; COPY_ALL_8463_NEGATIVE_EV; HEAD_GROUP_FILTER_SELECTS_THE_ANTI_TARGET.** §3 anti-target is first-3 $. Coded n≥20 + drop `RISK_BLOCKED` remain. Group polarity **requires** demo/contest (`NOT_DEMO_OR_CONTEST_GROUP`). Copy-all 8463 would import `RISK_BLOCKED` 29 / −$241,580. Scored XAU −$154,425. Dest PnL literal **$0**. Live `1369850` refused by `CTraderFixCopyOpen`. Risk to capital **NONE** (`SAFE_BY_ABSENCE` on live). |

---

## 2026-08-18 — P500_BOOK_175 in-memory DB: scores vanish on restart; cannot run a live book

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_175 |
| Slot | 175 |
| Purpose | Measured evidence for higher profit / lower loss. In-memory EF: scores vanish on restart. Cannot run a live book on RAM. Honesty: wanting profit ≠ edge. Copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_175.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Demo CLI invoked | **No** |
| Secret values printed | **None** (key names + literal `<SECRET>` + DemoDest prefixes only) |
| Local API | Not re-probed (SSRF to 127.0.0.1 blocked). Pins from synthesis 8463 / RISK_BLOCKED 29 / −$241,580 / dest $0 + Manager 18/8460 (JSON header remasured). |
| Verdict | **BLOCK_NO_LIVE_BOOK_ON_RAM.** DI fail-open `UseInMemoryDatabase("trader-intelligence-live")` when CS empty / `<SECRET>`. 0 `Migrations/`; **20** DbSets; empty `Configurations/`; `EnsureCreated` ×3; workers skip `EnvFile`; Compose Postgres unwired; health `healthy:true` constant. Hopper **L202–203**; persist `AllowFixSend=false` **L324**. HEAD `NewOrderSingleImplemented => DemoDest`; hosted tick calls `ExecuteDemoCopyAsync`. BOOK_155 L184/L306/`NOS=false` **STALE**. Copy-all 8463 imports −$241k tail. Live `1369850` **NONE** (CopyOpen refuse). Demo dest `5328266` not `SAFE_BY_ABSENCE` (this slot did not send). **HIGH** if copy-all is sent on InMemory. |

---

## 2026-08-18 — P500_BOOK_177 allocation must stay 0.01–0.05 until dest shadow EV after costs is positive

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_177 |
| Slot | 177 |
| Purpose | Measured evidence for higher profit / lower loss. Allocation factor must be tiny (0.01–0.05 of source) until shadow expectancy after costs is positive. Honesty: wanting profit ≠ edge. Copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_177.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** (this slot) |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Local API | Not re-probed (SSRF to 127.0.0.1 blocked). Pins from synthesis 8463 / RISK_BLOCKED 29 / −$241,580 / dest $0 + Manager 18/8460. On-disk ledger remasured: dest fill 305750/0.01. |
| Verdict | **ALLOCATION_MUST_STAY_TINY.** HEAD `AllocationFactor=1m` (1:1) is dest-ruin if sent. `MaxAutoLots=0.05` is a **source skip + 1:1 demo send** (inverse of tiny α; 0.05 grids print). Hosted risk hop cannot emit after-cost shadow fills (`VenueReconciled=false` → `VENUE_NOT_RECONCILED`). Demo SHADOW is AUTO_ADMIT. Copy-all 8463 copies `RISK_BLOCKED` −$241k. Tiny α shrinks the hole; it does not mint an edge. Live 1369850 refused. Demo dest hop is no longer absent. BOOK_157 cap-MISSING / NOS=false / SAFE_BY_ABSENCE-only **STALE**. Risk to capital this slot: **NONE** (did not send). |

---

## 2026-08-18 — P500_BOOK_180 quality 95.50 vs negative netSourcePnl

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_180 |
| Slot | 180 |
| Purpose | Read `BaselineScorer.cs`. Recalculate how quality 95.50 can coexist with negative `netSourcePnl`. Quote the formula. Measured evidence for higher profit / lower loss. Honesty: wanting profit is not an edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_180.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = synthesis 8463 / XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**. Catalog 302252/303174 remasured `1000−balance`. |
| Verdict | **CONFIRMED_SPLIT_NOT_EDGE.** 95.50 = `50+15+10+5+18−2.5` at `(b,r)=(90,10)` only; requires XAU `NetPnl>0` and `PF>=1.8`. Dashboard `netSourcePnl` is all-symbol Σ. Live ingest forces unused-SL. 302252 (−68.46) / 303174 (−29.38) match catalog `1000−balance`. HEAD requires demo (302252 fails N=11). Copy-all 8463 would copy `RISK_BLOCKED` −$241,580. Dest PnL $0. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_169 Starwave scored 0 after 91966 deals; do not size from Achiever-only scores

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_169 |
| Slot | 169 |
| Purpose | Starwave scored 0 while dealsInserted 91966. Book is incomplete. Do not size from Achiever-only scores. Measured evidence for higher profit and lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_169.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = P500 pin (8463 / Starwave **91966 / scored 0 / deals-done** / scored XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**). Manager census 18/8460 remasured from JSON. HEAD remasured independently. |
| Verdict | **BOOK_INCOMPLETE_DO_NOT_SIZE; COPY_ALL_8463_NEGATIVE_EV.** Starwave `deals-done` / `Scored=0` after **91,966** inserts is loop-3 queue (Achiever first), not an empty tape and not a lagging every-25 counter. Deal-share **26.10%** / account-share **23.03%** / score-share **0%**. Achiever-only SHADOW **+$78,276** is 100% demo and not a dest size. HEAD `AllocationFactor=1m`; `MaxAutoLots=0.05` is a DemoDest **skip**, not a haircut. HEAD **admits** `Starwave\demo` (`AUTO_ADMIT`). `NewOrderSingleImplemented => DemoDest` (BOOK_143 unwired-CopyOpen **stale**); live `1369850` refused. Copy-all 8463 would copy `RISK_BLOCKED` 29 / **−$241,580** inside scored XAU **−$154,425**, plus **8266** names with no `TraderScore` (entire Starwave catalog). Dest PnL literal **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_172 Persist ClOrdID before send; unknown must not retry (lower-loss, not higher-profit)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_172 |
| Slot | 172 |
| Purpose | Persist `ClOrdID` before send. Unknown state must not retry. That is lower-loss, not higher-profit. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge; copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_172.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**). Manager census 18/8460. |
| Verdict | **LOWER_LOSS_NOT_HIGHER_PROFIT; UNKNOWN_MUST_NOT_RETRY; SYSTEM_ARM_MISSING.** Domain `MayRetry(AfterSendAttempt/AfterDisconnectUnknown)=false`; 0 product callers. Persist-before-send **MISSING** (0 `ExecutionIntent` writers; factory clock+seq). Hosted demo hop `ExecuteDemoCopyAsync` writes clock-id `35=D` then persists tag 11 **after fill**; unknown retries with a **new** 11 (`ShouldOpenDest`). Copy-all 8463 imports `RISK_BLOCKED` −$241,580. Dest PnL **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE` on live 1369850); **HIGH** if DemoDest / live sender retries unknown. |

---

## 2026-08-18 — P500_BOOK_171 RISK_BLOCKED source PnL is hundreds of thousands negative

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_171 |
| Slot | 171 |
| Purpose | Measured evidence: `RISK_BLOCKED` source PnL is hundreds of thousands negative. Copying them is how the venue blows up. Higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_171.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-day pin (8463 / −$154,425 / RISK_BLOCKED 29 / −$241,580 / dest $0). Manager census independently re-summed 18/8460. |
| Verdict | **NEVER_COPY_RISK_BLOCKED; COPY_ALL_8463_BLOWS_THE_VENUE.** Live pin 29 / **−$241,580** (all martingale, mean −$8,330) dominates scored XAU **−$154,425** (SHADOW +$78,276 < tail). HEAD demo-required (`NOT_DEMO_OR_CONTEST_GROUP`) does **not** drop the tail. Copy-all 8463 copies that tail plus **8266** unscored. Hopper excludes blocked; persist `AllowFixSend=false` **L324**; `NOS => DemoDest` (BOOK_131 const-false **STALE**); hosted outbound `35=A` only; live dest `1369850` refused. Dest PnL **$0**. Risk to capital **NONE** today (`SAFE_BY_ABSENCE`); **HIGH / ruin** if the tail is sent. |

---

## 2026-08-18 — P500_BOOK_173 MFE/MAE FeatureQuality Unavailable; exact excursion unused

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_173 |
| Slot | 173 |
| Purpose | MFE/MAE `FeatureQuality` is Unavailable. Exact excursion not used. Do not claim MAE-based stops. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_173.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Local API | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-day P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). |
| Verdict | **FEATURE_QUALITY_UNAVAILABLE; EXACT_EXCURSION_UNUSED; NO_MAE_STOPS.** Scorer always stamps `MaeMfeQuality=Unavailable`; `AverageMfe`/`AverageMae` null; `Score()` never reads them. A22 MAE floors not wired (`FLAG_MAE`/`mfe_mae_used` = 0 hits in `src`); `MfeMaeCalculator` + `mt5_xau_ticks` MISSING. Copy SL = `FinalSl ?? InitialSl` (**L252**; 133/153 L234 **STALE**). Persist `AllowFixSend=false` L324. Hopper L202. `NOS => DemoDest` L50. D57 VWAP mutation scores identical. Copy-all **8463** would copy `RISK_BLOCKED` **−$241,580**. Wanting profit is not an edge. Risk to capital **NONE** (this slot report-only; live hop `SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_165 official FIX QUOTE 5211 / TRADE 5212 / TargetCompID cServer; Logon is not a fill

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_165 |
| Slot | 165 |
| Purpose | Official cTrader FIX identity: QUOTE TLS 5211, TRADE TLS 5212, issued TargetCompID cServer. Prove Logon (35=A) is not a fill. Measured evidence for higher dest profit / lower dest loss. Honesty: wanting profit ≠ edge; copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_165.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public host/account names) |
| Live API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**). Manager census re-summed 18/8460. Official Help/RoE/FAQs/comms-model + Spotware C# + Python config re-fetched this slot. |
| Verdict | **CONFIRMED_OFFICIAL_PORTS_AND_COMPID. LOGON_IS_NOT_A_FILL. NO_DEST_EDGE. COPY_ALL_8463_NEGATIVE_EV.** Official SSL QUOTE 5211 / TRADE 5212; issued TargetCompID `cServer` (RoE/comms-model `CSERVER`; no silent fold). Logon hop is one-shot `35=A` then dispose. Demo-gated `CTraderFixCopyOpen` can `Build("D")` on :5212 after its own Logon and still refuses live `1369850`. Copy-all 8463 copies `RISK_BLOCKED` **−$241,580**. Dest dashboard **$0**. Risk to live capital **NONE** (`SAFE_BY_ABSENCE` + account gate). Wanting profit is not an edge. |

---

## 2026-08-18 — P500_BOOK_170 SHADOW 100% demo; no real Starwave/contest in measured copy set

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_170 |
| Slot | 170 |
| Purpose | SHADOW group is 100% demo. No real Starwave or contest live book in the copy set. Measured evidence for higher dest profit / lower dest loss. Honesty: wanting profit ≠ edge; copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_170.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** (this slot). Off-hop `CTraderFixCopyOpen.Build("D")` exists for demo dest only; not invoked. |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Local API this slot | **Attempted, SSRF-blocked** (`127.0.0.1:5000`). Pins: `P500_PROFIT_SYNTHESIS.md` 8463 / SHADOW 70 +$78,276 / 100% demo; `RISK_BLOCKED` 29 −$241,580; scored XAU −$154,425; dest $0. Manager JSON re-sum 18/8460. |
| Verdict | **CONFIRMED_SHADOW_100PCT_DEMO; NO_REAL_STARWAVE_OR_CONTEST_IN_MEASURED_COPY_SET; HEAD_ADMITS_DEMO_CONTEST_REJECTS_REAL; COPY_ALL_8463_NEGATIVE_EV.** Named SHADOW 302252/303174/303274/303310/322947 are `demo\yo-2step` or `demo\yo-payp`. Contest 190 not SHADOW. Starwave real 28 / scored 0. HEAD `CopyGroupFilter` requires demo/contest (`NOT_DEMO_OR_CONTEST_GROUP` on real) — BOOK_10/50/70/90/110 reject-demo pin is stale. BOOK_130 `NOS const false` / product `35=D=0` **STALE** (demo dest `5328266` can receive `35=D`; live `1369850` refused). Dest $0. Risk **NONE on live 1369850**. |

---

## 2026-08-18 — P500_BOOK_163 TradeReconstructor / 303274 same-second 0.05 grid

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_163 |
| Slot | 163 |
| Purpose | Read `TradeReconstructor` and 303274-style overlapping 0.05-lot same-second entries. Is grid flagged? Evidence for higher profit / lower loss. Do not modify product. Never enable REAL_COPY. Never send 35=D. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_163.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| SUT | `TradeReconstructor.cs` 347 lines; `GroupBy(PositionId)` L46 + `ScaleIn` worse-than-VWAP latch only; `src` grep WasGrid/GridFlag/IsGrid/same-second/grid = **0** |
| Catalog | login **303274** `demo\yo-2step` 16228.24 (`LIVE_GROUPS_AND_TRADERS.json` L2564–2568) |
| Policy drift vs BOOK_83/103 | HEAD `CopyGroupFilter` **requires** demo/contest. 303274 is **eligible** / roster `AUTO_ADMIT`. Unit `Demo_group_blocked` **gone**. |
| Hop vs BOOK_143 | `CopyTradingService` **625** lines (was 482). Hosted tick calls `ExecuteDemoCopyAsync`. `MaxAutoLots=0.05` **admits** this ticket. Live 1369850 refused. Persist `AllowFixSend=false` **L324**. |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = synthesis 8463 / XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest columns **$0**. Manager census 18/8460. Ledger: 305750 @ 0.01, **not** 303274. |
| Verdict | **GRID_NOT_FLAGGED.** Distinct hedge 0.05s never `ScaleIn`. No `WasGrid`. 303274-class averaging/martingale false; SHADOW reachable. HEAD demo filter **admits** this login. `MaxAutoLots=0.05` is the grid ticket, not a brake. Copy-all 8463 would copy `RISK_BLOCKED` losses (−$241,580). Live capital **NONE** today (`SAFE_BY_ABSENCE` on 1369850). Demo dest residual if host is running. |

---

## 2026-08-18 — P500_BOOK_168 MaxDailyExecutionLoss=2000 / MaxLossPerTrader=500 are loss caps, not an edge

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_168 |
| Slot | 168 |
| Purpose | Measured evidence: `MaxDailyExecutionLoss=2000` and `MaxLossPerTrader=500` are loss caps, not an edge. Wanting profit does not create expectancy. Copy-all 8463 would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_168.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Local API this slot | `GET :5000/api/overview` and `/api/traders` SSRF-blocked. Book pin = `P500_PROFIT_SYNTHESIS.md` + S007 + CREDENTIALS 18/8460. Manager JSON re-sum 6512+1948=8460. |
| Verdict | **LOSS_CAPS_NOT_EDGE.** Caps fire after dest (or a mis-fed source ticket) is already ≤ −$500 / −$2000; they do not read `RISK_BLOCKED`; copy hop zeros `DailyExecutionPnl` so the daily line is dead; recon short-circuits OPEN; close + `FLATTEN_LOSS_CUT` skip `Evaluate` (A71 G21–G22 FAIL if later wired). HEAD `ExecuteDemoCopyAsync` sends demo `35=D` **without** `Evaluate`. HEAD `AllocationFactor=1` makes $2,000 = one legal 5-lot $4/oz print. Copy-all 8463 EV is the scored XAU book −$154,425 (blocked tail −$241,580). Live dest risk today **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P503_V_2 losers left on roster? (adversarial slot 2)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_V_2 |
| Slot | 2 |
| Purpose | Adversarial confirm: losers are not left on the copy roster. FAIL if remove/flatten is missing. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_V_2.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Verdict | **FAIL.** Remove/flatten **present** (`CopyRosterEngine.RemoveAndFlatten` + hosted `TickRosterAsync` + `FLATTEN_LOSS_CUT`). Confirmation fails: streak/DD (still-green) losers **re-ADMIT** next 20 s because cuts are `alreadyOnRoster` only and `onRoster` is `Status==ADMITTED`. `ExecuteDemoCopyAsync` sends dest for bounced seats; does not consume flatten. `ShouldFlattenOpenCopy` 0 callers. Net-red / blocked / size-pattern stay off. P503_R_1/R_20 UNWIRED **STALE**. Live 1369850 **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P503_V_18 losers left on roster? (adversarial)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_V_18 |
| Slot | 18 |
| Purpose | Adversarial read of `src/Domain/Copy` + `CopyTradingService`. Confirm losers are not left on the roster. FAIL if remove/flatten is missing. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_V_18.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Verdict | **FAIL.** Paper `RemoveAndFlatten` is hosted (`TickRosterAsync` L165–191 writes `REMOVED:{reason}` + dest `FLATTEN_LOSS_CUT` intents). **Venue flatten on REMOVE is missing** — `ExecuteDemoCopyAsync` dest-closes only when the MT5 source trade is `Completed`. Streak/DD losers **re-ADMIT** next 20 s (`onRoster` requires `Status==ADMITTED`; ADMIT skips those predicates). `ShouldFlattenOpenCopy` 0 `src/` callers. P503_R_1 UNWIRED **STALE**. Live `1369850` refused; demo dest can keep loser copies. Risk to live Pepperstone **NONE**; demo dest **EXPOSED**. |

---

## 2026-08-18 — P503_V_16 losers not left on roster (remove/flatten present)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_V_16 |
| Slot | 16 |
| Purpose | Adversarial confirm: losers are not left on the copy roster. FAIL if remove/flatten is missing. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_V_16.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** |
| Verdict | **PASS.** Remove/flatten **present** (`RosterAction.RemoveAndFlatten`, `TickRosterAsync` `REMOVED:{reason}`, `FlattenOpenCopiesAsync` `FLATTEN_LOSS_CUT`). Book losers / blocked / size-pattern / real-group leave `ADMITTED`. Hopper + `ExecuteDemoCopyAsync` require `ADMITTED`. Residual: green-net streak/DD re-admit; venue flatten consume missing. Never flatten MT5 source. |

---

## 2026-08-18 — P500_BOOK_166 prop-challenge demo is adverse selection

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_166 |
| Slot | 166 |
| Purpose | Measured evidence: copying prop-challenge demo accounts is adverse selection. Most accounts exist to pass a profit target then blow. Higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_166.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None** (quoted only the already-on-disk boolean `REAL_COPY_EXECUTION_ENABLED=true`) |
| Local API this slot | **Attempted, SSRF-blocked** (`GET http://127.0.0.1:5000/api/overview` and `/api/traders`). Book integers from `P500_PROFIT_SYNTHESIS.md` + `P500_S007` + Manager census 18/8460 (`LIVE_GROUPS_AND_TRADERS.json` 08:42Z, header re-summed). Named balances 302252/303174/303274/303310/322947 + PASSFIRST 333103/333104/333106 re-checked. |
| Verdict | **ADVERSE_SELECTION_CONFIRMED; HEAD_SELECTS_CHALLENGE_FACTORY; COPY_ALL_8463_NEGATIVE_EV.** Achiever 6295/6512 = `demo\yo-2step` (0 `real\`). Combined 8417/8460 (99.49%) challenge/demo/contest. SHADOW 70 is 100% demo (+$78,276). `RISK_BLOCKED` 29 / −$241,580 (all martingale). HEAD `CopyGroupFilter` + roster **AUTO_ADMIT** demo/contest and reject real (`NOT_DEMO_OR_CONTEST_GROUP`). Copy-all 8463 imports the blocked tail. Scored XAU −$154,425. Dest PnL **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_161 RiskEngine reject reasons that cut dest loss if live send existed

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_161 |
| Slot | 161 |
| Purpose | Read `RiskEngine.cs`. List every reject reason that reduces dest loss if live send existed. Measured evidence for higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_161.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None** |
| Local API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**) + JSON census re-sum 18/8460. |
| Verdict | **16_OF_19_CUT_NEW_DEST_LOSS; THREE_TRAP_EXITS; ZERO_TRADER_RISK_BLOCKED; COPY_ALL_8463_COPIES_RISK_BLOCKED; SAFE_BY_ABSENCE.** 16 increasing-family rejects would refuse new gold if they sat in front of a `35=D`. 3 book-loss reasons freeze closes. Engine grep `TRADER_RISK_BLOCKED`=0. Copy-all 8463 copies `RISK_BLOCKED` **−$241,580** inside scored XAU **−$154,425**. HEAD `AllocationFactor=1m`. Dest **$0**. Risk to capital **NONE** today; **HIGH / ruin** if blocked tail or catalog 8463 is sent 1:1. |

---

## 2026-08-18 — P503_V_12 losers must not remain on roster

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_V_12 |
| Slot | 12 |
| Purpose | Adversarial read of `src/Domain/Copy` + `CopyTradingService`. Confirm losers are not left on the roster. FAIL if remove/flatten is missing. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_V_12.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Verdict | **FAIL.** Remove/flatten **present** (`RosterAction.RemoveAndFlatten`, `TickRosterAsync` L165–191, `FlattenOpenCopiesAsync` → `FLATTEN_LOSS_CUT`). Missing-path FAIL **not** triggered. Book-negative stays `REMOVED` (policy `XauNetPnl<=0`). Streak/DD losers **re-ADMIT** next 20 s because `onRoster` is `Status==ADMITTED` only; first-seen L-L-L is `AUTO_ADMIT`. `ShouldFlattenOpenCopy` DEAD. Demo dest close is source-complete only, not REMOVE. |

---

## 2026-08-18 — P503_V_0 losers not left on roster (remove/flatten present)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_V_0 |
| Slot | 0 |
| Purpose | Adversarial read of `src/Domain/Copy` + `CopyTradingService`. Confirm losers are not left on the roster. FAIL if remove/flatten is missing. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_V_0.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Verdict | **PASS.** Remove/flatten **not** missing. `CopyRosterEngine.Remove` → `RemoveAndFlatten` + `FlattenDestination=true`. `TickRosterAsync` persists `REMOVED:{reason}` and `FLATTEN_LOSS_CUT` dest close intents. Hopper L231 and `ExecuteDemoCopyAsync` L542 require `ADMITTED`. Completed-book/state/pattern losers are ejected. Residuals: streak/DD re-ADMIT (no latch); `ShouldFlattenOpenCopy` DEAD; dest send closes only on source complete. P503_R_1 UNWIRED **STALE**. Risk to capital **NONE** (`SAFE_BY_ABSENCE` of flatten run / live 1369850). |

---

## 2026-08-18 — P503_V_7 losers must not stay on the roster (FAIL if remove/flatten missing)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_V_7 |
| Slot | 7 |
| Purpose | Adversarial: read `src/Domain/Copy` + `CopyTradingService`. Confirm losers are not left on the roster. FAIL if remove/flatten is missing. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_V_7.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Verdict | **PASS.** Remove/flatten **present and wired** (`RemoveAndFlatten` + `TickRosterAsync` `REMOVED:{reason}` + dest `FLATTEN_LOSS_CUT`). After TickRoster a coded loser is not `ADMITTED`. Net-red / `STATE_*` / `SIZE_PATTERN` / real-group **stay off**. Still-green streak/DD **re-ADMIT** (no latch). `ShouldFlattenOpenCopy` **DEAD**. `ExecuteDemoCopyAsync` does not consume flatten intents. P503_R_20 UNWIRED **STALE**. |

---

## 2026-08-18 — P503_V_13 losers not left on roster (remove/flatten present?)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_V_13 |
| Slot | 13 |
| Purpose | Adversarial: read `src/Domain/Copy` + `CopyTradingService`. Confirm losers are not left on the roster. FAIL if remove/flatten is missing. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_V_13.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_REMOVE_FLATTEN_WIRED.** Remove+dest flatten **not missing** (`Decide`→`RemoveAndFlatten`; hop L147–173; `FlattenOpenCopiesAsync` `FLATTEN_LOSS_CUT`; hosted `TickRosterAsync`). On-roster losers cut this 20 s tick. Confirmation **PARTIAL**: T2/T3 still-green books **re-ADMIT** next tick (`alreadyOnRoster` skip; no S03 latch). `ShouldFlattenOpenCopy` 0 hop callers. Dest **$0** (`SAFE_BY_ABSENCE`). P503_R_1 UNWIRED **STALE**. |

---

## 2026-08-18 — P500_BOOK_164 architecture §3 dest-net vs first-3 / copy-all

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_164 |
| Slot | 164 |
| Purpose | Read architecture §3 business goal. Future destination-net PnL is the target, not first-3 dollars. Measured evidence for higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_164.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Localhost API | Not re-probed (SSRF block on 127.0.0.1). Used on-disk probe 18/8460 + P500 remasure 8463 / RISK_BLOCKED 29 / −$241,580 / dest PnL literal 0. |
| Verdict | **PASS as §3 reading. FAIL as a live profit claim. COPY_ALL_8463_NEGATIVE_EV. HEAD_GROUP_FILTER_SELECTS_THE_ANTI_TARGET.** First-3 / `EarlyQualityScore` is source quality, not dest-net. Copy-all 8463/8460 would spray 29 martingale `RISK_BLOCKED` names (−$241k source) plus a 100% Achiever demo/contest book onto one Pepperstone login. HEAD now **requires** demo/contest (`NOT_DEMO_OR_CONTEST_GROUP` rejects Starwave real). Higher dest profit / lower dest loss = keep `35=D` off; keep n≥20 / no RISK_BLOCKED / no lookahead; **invert** the group polarity back off challenge books; do not rank by first-3 dollars. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_157 allocation must stay 0.01–0.05 until dest shadow EV after costs is positive

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_157 |
| Slot | 157 |
| Purpose | Measured evidence: allocation factor must be tiny (0.01–0.05 of source) until shadow expectancy after costs is positive. Higher profit / lower loss. Honesty: wanting profit is not an edge; copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_157.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None** |
| Local API | Not re-probed (SSRF to 127.0.0.1 blocked). Pins from synthesis 8463 / RISK_BLOCKED 29 / −$241,580 / dest $0 + Manager 18/8460. |
| Verdict | **ALLOCATION_MUST_STAY_TINY.** HEAD `AllocationFactor=1m` (1:1) is dest-ruin if sent. Dest 0.05 ticket cap **MISSING**. Shadow EV after costs **not proven** (hosted hop `VenueReconciled=false` → `VENUE_NOT_RECONCILED`; dest PnL $0; live shadow $0). HEAD **AUTO_ADMITS** demo/contest at α=1 (BOOK_117 §6 polarity **STALE**). BOOK_137 “117 absent” **FALSE**. Copy-all 8463 copies `RISK_BLOCKED` −$241k. Tiny α shrinks the hole; it does not mint an edge. Slot 17 `0.50×0.01=0.01` cell **wrong** (SUT **0**). W500 `×0.05` hop **STALE**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_156 FIX quote bid/ask are null; cannot size or guard spread

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_156 |
| Slot | 156 |
| Purpose | Measured evidence: FIX quote bid/ask are null. Cannot size or guard spread without a quote tape. Higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_156.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** (copy hop unimplemented; `CTraderFixCopyOpen` / demo helpers **not invoked**) |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0 / bid/ask **null**). |
| Verdict | **NO_TAPE_NO_SIZE_NO_SPREAD_GUARD.** Live DTO bid/ask/age **null**. Hosted FIX is one-shot `35=A` then dispose — no QUOTE `35=x`/`35=V`. `CTraderQuoteService` **0 callers**, not in DI. Only `DestinationQuotes.Add` is `DemoSeeder` forged 2399.45/2399.85 (`VenueInstrumentId=null`); live host uses `BrokerCatalogSeed` (no quote row). `QuantityNormalizer` has no quote params; HEAD `AllocationFactor=1m`. `SPREAD_TOO_WIDE` cannot fire; hop hits `VENUE_NOT_RECONCILED` first; `MaxSlippage` unread. HEAD `CopyGroupFilter` **admits demo/contest only**. Off-hop `CTraderFixCopyOpen` can emit `35=D` after TRADE-session SecurityList **without reading bid/ask** (BOOK_136 product-`35=D=0` **STALE**). `shadowPnl=0` is absence. Copy-all 8463 copies `RISK_BLOCKED` −$241,580. Risk to capital **NONE** (`SAFE_BY_ABSENCE`); **HIGH** if 1:1 / `--copy-open` / demo-only send armed against a null book. |

---

## 2026-08-18 — P500_BOOK_158 never flatten MT5 source (dest-only flatten)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_158 |
| Slot | 158 |
| Purpose | Measured evidence for higher dest profit / lower dest loss. Topic: never flatten the MT5 source. Destination-only flatten. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_158.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None** |
| Live API | `GET :5000/api/overview` and `/api/traders` **SSRF blocked**. Pins from synthesis + Manager census. |
| Verdict | **DEST_ONLY_FLATTEN_LAW.** Source C# path GET-only (0 `DealerSend`; `PositionRequest`/`PositionGetByGroup` only). Roster **WIRED**: dest `FLATTEN_LOSS_CUT` intents only; P503_R_1 UNWIRED **STALE**. `ShouldFlattenOpenCopy` DEAD on hop. Hosted FIX `35=A` only. Product `EmergencyFlatten` blocks opens only. Copy hop hardcodes `KillSwitch=None` L287. Close/flatten hops skip `Evaluate`; qty = source lots × `AllocationFactor=1m`. Demo dest-721 flatten refuses `1369850`. Scored XAU **−$154,425**; `RISK_BLOCKED` **−$241,580** (29). Dest PnL **$0** (`SAFE_BY_ABSENCE`). Copy-all 8463 would import that tail. Risk to capital **NONE** today; **DEST_RUIN_IF_SENT** if copy-all / blocked tail / 1:1 or 5-lot dest flatten. |

---

## 2026-08-18 — P500_BOOK_159 Official FIX trade-copier listing is not a license to send

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_159 |
| Slot | 159 |
| Purpose | Trade-copier on cTrader FIX is officially listed; Spotware says other APIs may fit copy better. Still no license to send today. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_159.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Demo CLI invoked | **No** |
| Secret values printed | **None** (boolean only) |
| Live API this pass | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**) + Manager census 18/8460 re-summed. Official Help + RoE + Open API terms re-fetched. |
| Verdict | **NO_LICENSE; COPY_ALL_8463_NEGATIVE_EV.** Official https://help.ctrader.com/fix/ lists trade copiers then: “other Spotware APIs are more suitable.” RoE has TRADE `35=D`. Product hop is `35=A` only; `src/`+`apps/` `35=D=0`; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. Off-hop `CTraderFixCopyOpen` is demo-`5328266` only (live `1369850` refused). §68 0/19; §70 0/14. Open API terms still require trader-explicit approval. HEAD `CopyGroupFilter` **requires** demo/contest. Copy-all 8463 would copy `RISK_BLOCKED` −$241,580 inside scored XAU −$154,425. Achiever 100% demo/contest; Starwave real = 28. Dest PnL $0. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_155 in-memory DB: scores vanish on restart; cannot run a live book

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_155 |
| Slot | 155 |
| Purpose | Measured evidence for higher profit / lower loss. In-memory EF: scores vanish on restart. Cannot run a live book on RAM. Honesty: wanting profit ≠ edge. Copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_155.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (key names + literal `<SECRET>` only) |
| Local API | Not re-probed (SSRF to 127.0.0.1 blocked). Pins from synthesis 8463 / RISK_BLOCKED 29 / −$241,580 / dest $0 + Manager 18/8460 (JSON header remasured). |
| Verdict | **BLOCK_NO_LIVE_BOOK_ON_RAM.** DI fail-open `UseInMemoryDatabase("trader-intelligence-live")` when CS empty / `<SECRET>`. 0 `Migrations/`; **20** DbSets; empty `Configurations/`; `EnsureCreated` ×3; workers skip `EnvFile`; Compose Postgres unwired; health `healthy:true` constant. Hopper L184–187 dies with scores. HEAD policy **requires** demo/contest. Copy-all 8463 imports −$241k tail. Dest capital **NONE** today (`SAFE_BY_ABSENCE`); **HIGH** if send armed on InMemory. |

---

## 2026-08-18 — P500_BOOK_147 XAUUSD copy cost: spread + slippage + 15s MaxSourceSignalAge. Scalps die.

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_147 |
| Slot | 147 |
| Purpose | Measured evidence for higher profit / lower loss. Topic: XAUUSD copy cost = dest spread + slippage + 15s `MaxSourceSignalAge` reject. Scalps die. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_147.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = synthesis 8463 / XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**. Manager census 18/8460. 322947 JSON remasure `demo\yo-payp` 104949.8. |
| Verdict | **SCALPS_DIE_AFTER_COSTS; COPY_ALL_8463_NEGATIVE_EV.** Dest taker spread (seed 0.40 / allowed 2.0 = $40–$200 per 1.00 lot) + unread `MaxSlippage=1.5` + 15s `SIGNAL_STALE` on OPEN. Hosted poll **20 s > 15 s** clock (≥25% first-sight miss). Roster **no hold gate**. HEAD **admits demo/contest** (BOOK_107 reject **STALE**; agrees with 127). 322947 ~163s / +$4,950 is source demo, not dest EV. Copy-all 8463 imports `RISK_BLOCKED` **−$241,580** (29). Dest **$0** (`SAFE_BY_ABSENCE`). Risk to capital **NONE** today; **HIGH** if scalps / copy-all sent 1:1. |

---

## 2026-08-18 — P500_BOOK_162 CTraderFixSession outbound is only 35=A

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_162 |
| Slot | 162 |
| Purpose | Read `CTraderFixSession.cs`. Prove outbound MsgType is only `A`. No `35=D`. Measured evidence for higher profit and lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_162.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| HTTP this slot | `GET :5000/api/overview` and `/api/traders` **not reachable** (SSRF block; no shell) |
| Verdict | **PASS_35A_ONLY; COPY_ALL_8463_NEGATIVE_EV.** Assigned file 135/135 is Logon `35=A` only (`WriteAsync=1`, sockets disposed). Product literal `35=D=0`. Copy-all 8463 copies `RISK_BLOCKED` **−$241,580**. Dest **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_154 ML is not built; deterministic baseline only

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_154 |
| Slot | 154 |
| Purpose | Measured evidence for higher dest profit / lower dest loss. Topic: **ML is not built.** Do not invent a model. Deterministic baseline only. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_154.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Local API | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF; no curl). Book integers = same-day P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). |
| Verdict | **ML_NOT_BUILT; DETERMINISTIC_BASELINE_ONLY; DO_NOT_INVENT_MODEL; COPY_ALL_8463_IMPORTS_RISK_BLOCKED.** `services/` empty; `src` 0 XGBoost/`IScoringService`; `mlProbability` literal null; dest PnL constructor 0; `CanPromoteToLive=false`; persist `AllowFixSend=false` L306; product hop `35=A` only. HEAD policy **requires** demo/contest (BOOK_14–114 exclude-demo **STALE**). Copy-all **8463** copies `RISK_BLOCKED` **−$241,580**. Wanting profit is not an edge. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_150 SHADOW group is 100% demo; no real Starwave or contest in the copy set

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_150 |
| Slot | **150** |
| Purpose | Measured evidence for higher profit / lower loss. Topic: SHADOW group is 100 percent demo. No real Starwave or contest live book in the copy set. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_150.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` + `P500_S004` (49+ / 6 demo SHADOW, 0 contest / Starwave / real) + `P500_S007` (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**) + Manager census 18/8460. |
| Verdict | **CONFIRMED_SHADOW_100PCT_DEMO; NO_REAL_STARWAVE_OR_CONTEST_IN_MEASURED_COPY_SET; HEAD_ADMITS_S004_FACTORY; COPY_ALL_8463_NEGATIVE_EV.** 70 SHADOW = `demo\yo-2step`/`demo\yo-payp`. Contest 190 not in copy set. Starwave real 28 scored=0 + HEAD-rejected. Copy-all 8463 copies `RISK_BLOCKED` −$241,580 + 8266 un-scored. Dest $0. Risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_149 Starwave scored 0 after 91966 deals; do not size from Achiever-only scores

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_149 |
| Slot | 149 |
| Purpose | Starwave scored 0 while dealsInserted 91966. Book is incomplete. Do not size from Achiever-only scores. Measured evidence for higher profit and lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_149.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = P500 pin (8463 / Starwave **91966 / scored 0 / deals-done** / scored XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**). Manager census 18/8460 remasured from JSON. |
| Verdict | **BOOK_INCOMPLETE_DO_NOT_SIZE; COPY_ALL_8463_NEGATIVE_EV.** Starwave `deals-done` / `Scored=0` after **91,966** inserts is pipeline order (Achiever scores first), not an empty tape. Deal-share **26.10%** (`91966/352318`) vs score-share **0%**. Achiever-only SHADOW **+$78,276** is 100% demo and not a dest size. HEAD `AllocationFactor=1m` (0.05 pin stale); HEAD **admits** `Starwave\demo` (`AUTO_ADMIT`). Copy-all 8463 would copy `RISK_BLOCKED` 29 / **−$241,580** inside scored XAU **−$154,425**, plus **8266** names with no `TraderScore` (entire Starwave catalog). Dest PnL literal **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_140 quality 95.50 vs negative netSourcePnl

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_140 |
| Slot | 140 |
| Purpose | Read `BaselineScorer.cs`. Recalculate how quality 95.50 can coexist with negative `netSourcePnl`. Quote the formula. Measured evidence for higher profit / lower loss. Honesty: wanting profit is not an edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_140.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| This-slot `:5000` GET | Blocked (SSRF). Pins from synthesis + Manager 18/8460. 302252/303174 dollars re-checked against `LIVE_GROUPS_AND_TRADERS.json`. Independent re-read: ingest drops SL (`Mt5Deal` has no SL field). HEAD policy **requires** demo. |
| Verdict | **CONFIRMED_SPLIT_NOT_EDGE.** 95.50 = `50+15+10+5+18−2.5` at `(b,r)=(90,10)` only; requires XAU `NetPnl>0` and `PF>=1.8`. Dashboard `netSourcePnl` is all-symbol Σ. Live ingest forces unused-SL. 302252 (−68.46) / 303174 (−29.38) match catalog `1000−balance`. HEAD requires demo (302252 fails N=11). Copy-all 8463 would copy `RISK_BLOCKED` −$241,580. Dest PnL $0. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_152 Persist ClOrdID before send; unknown must not retry (lower-loss, not higher-profit)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_152 |
| Slot | 152 |
| Purpose | Persist `ClOrdID` before send. Unknown state must not retry. That is lower-loss, not higher-profit. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge; copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_152.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Local API | Not re-probed (SSRF to 127.0.0.1 blocked). Pins from synthesis 8463 / RISK_BLOCKED 29 / −$241,580 / dest $0 + Manager 18/8460. |
| Verdict | **LOWER_LOSS_NOT_PROFIT; SYSTEM_ARM_MISSING; COPY_ALL_8463_COPIES_RISK_BLOCKED.** Helper `MayRetry(unknown)=false`; 0 product callers; 0 intent writers; factory clock-based; no `35=H` recovery. Off-hop `Build("D")` ×5 (clock ids). Copy-all 8463 copies `RISK_BLOCKED` −$241,580. Dest PnL $0. Risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_153 MFE/MAE FeatureQuality Unavailable; exact excursion unused

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_153 |
| Slot | 153 |
| Purpose | MFE/MAE `FeatureQuality` is Unavailable. Exact excursion not used. Do not claim MAE-based stops. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_153.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **−$241,580** / dest PnL **$0**). |
| Verdict | **FEATURE_QUALITY_UNAVAILABLE; EXACT_EXCURSION_UNUSED; NO_MAE_STOPS.** Scorer always stamps `MaeMfeQuality=Unavailable`; `AverageMfe`/`AverageMae` null; `Score()` never reads them. A22 MAE floors not wired (`FLAG_MAE`/`mfe_mae_used` = 0 hits in `src`); `MfeMaeCalculator` + `mt5_xau_ticks` MISSING. Copy SL = `FinalSl ?? InitialSl` (L234 fill clone). D57 VWAP mutation scores identical. HEAD **requires** demo/contest. Copy-all **8463** would copy `RISK_BLOCKED` **−$241,580**. Wanting profit is not an edge. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_143 TradeReconstructor / 303274 same-second 0.05 grid

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_143 |
| Slot | 143 |
| Purpose | Read `TradeReconstructor` and 303274-style overlapping 0.05-lot same-second entries. Is grid flagged? Evidence for higher profit / lower loss. Do not modify product. Never enable REAL_COPY. Never send 35=D. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_143.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| SUT | `TradeReconstructor.cs` 347 lines; `GroupBy(PositionId)` L46 + `ScaleIn` worse-than-VWAP latch only; `src/**/*.cs` grep `grid` = **0**; `src`/`tests` grep WasGrid/IsGrid/same-second = **0** |
| HEAD vs BOOK_103 | Copy hop now **admits** demo/contest. Still **not** a grid flag. 303274 `demo\yo-2step` **AUTO_ADMIT**s (`AllocationFactor=1`). Slots 3–103 demo-reject **STALE**. |
| Catalog | login **303274** `demo\yo-2step` 16228.24 (`LIVE_GROUPS_AND_TRADERS.json` L2564–2568) |
| Local API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book integers from same-day `P500_PROFIT_SYNTHESIS.md`; census re-summed from `LIVE_GROUPS_AND_TRADERS.json` 08:42Z (18/8460). |
| Verdict | **GRID_NOT_FLAGGED.** Distinct hedge 0.05s never `ScaleIn`. No `WasGrid`. 303274-class averaging/martingale false; SHADOW reachable. Demo+roster **admit** is not a grid detector. Copy-all 8463 would copy `RISK_BLOCKED` losses (−$241,580). Dest capital **NONE** today (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_138 dest-only flatten (never flatten MT5 source)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_138 |
| Slot | 138 |
| Purpose | Measured evidence for higher profit / lower loss. Never flatten the MT5 source. Destination-only flatten. Honesty: wanting profit ≠ edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_138.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave P500 pin (8463 / XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**). Manager census 18/8460. |
| Verdict | **DEST_ONLY_FLATTEN_LAW.** Source C# path GET-only (0 `DealerSend`; `PositionRequest`/`PositionGetByGroup` only). Roster **WIRED**: dest `FLATTEN_LOSS_CUT` only; never MT5. Product FIX flatten run **MISSING**. BOOK_118 demo-reject **STALE** (HEAD admits demo/contest). Copy-all 8463 imports `RISK_BLOCKED` −$241,580 inside scored XAU −$154,425. Dest PnL **$0** (`SAFE_BY_ABSENCE`). HEAD `AllocationFactor=1m` **UNSAFE if sent**. Risk to capital **NONE** today; **DEST_RUIN_IF_SENT** if copy-all / blocked tail / 1:1 dest send. |

---

## 2026-08-18 — P500_BOOK_144 architecture §3 dest-net vs first-3 / copy-all

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_144 |
| Slot | 144 |
| Purpose | Read architecture §3 business goal. Future destination-net PnL is the target, not first-3 dollars. Measured evidence for higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_144.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Local API this slot | **Not re-probed** (SSRF-blocks `127.0.0.1`). Book integers from P500 pin + Manager census 18/8460 (re-summed) + HEAD re-read. |
| Verdict | **PASS_§3_DEST_NET_NOT_FIRST3; COPY_ALL_8463_NEGATIVE_EV; HEAD_GROUP_FILTER_SELECTS_THE_ANTI_TARGET.** §3 anti-target is first-3 $. Coded n≥20 + drop `RISK_BLOCKED` remain. Group polarity **requires** demo/contest (`NOT_DEMO_OR_CONTEST_GROUP`). Copy-all 8463 would import `RISK_BLOCKED` 29 / −$241,580. Scored XAU −$154,425. Dest PnL literal **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_137 allocation must stay 0.01–0.05 until dest shadow EV after costs is positive

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_137 |
| Slot | 137 |
| Purpose | Measured evidence: allocation factor must be tiny (0.01–0.05 of source) until shadow expectancy after costs is positive. Higher profit / lower loss. Honesty: wanting profit is not an edge; copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_137.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Local API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers from same-day `P500_PROFIT_SYNTHESIS.md`; census 18/8460. HEAD remasured independently. |
| Verdict | **ALLOCATION_MUST_STAY_TINY.** HEAD `AllocationFactor=1m` (1:1) is dest-ruin if sent. Hosted hop cannot emit after-cost shadow fills (`VenueReconciled=false` → `VENUE_NOT_RECONCILED`). Demo SHADOW **is** AUTO_ADMIT (BOOK_97 “0 of 70” **wrong**). Close hopper **does** `Evaluate` (BOOK_97 §9 stale). Copy-all 8463 copies `RISK_BLOCKED` −$241k. Tiny α shrinks the hole; it does not mint an edge. 70×2.00 same-side at α=1 = 14,000 oz ($14k per $1/oz). Slot 17 `0.50×0.01=0.01` cell **wrong** (SUT **0**). W500 `×0.05` hop **STALE**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_127 XAUUSD copy cost: spread + slippage + 15s MaxSourceSignalAge. Scalps die.

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_127 |
| Slot | 127 |
| Purpose | Measured evidence for higher dest profit / lower dest loss. Topic: XAUUSD copy cost = dest spread + slippage + 15s `MaxSourceSignalAge` reject. Scalps die. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_127.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None** |
| Live API this slot | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-day pin (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **−$241,580** / dest $0). 322947 card remasured from JSON. |
| Verdict | **SCALPS_DIE_AFTER_COSTS; HEAD_ADMITS_DEMO_SCALPS; COPY_ALL_8463_NEGATIVE_EV.** Dest taker spread (seed 0.40 / allowed 2.0 = $40–$200 per 1.00 lot) + unread `MaxSlippage=1.5` + 15s `SIGNAL_STALE` on OPEN. Hosted poll **20 s > 15 s** clock (≥25% first-sight miss). Hop clocks `OpenedAt`; recon short-circuits first. BOOK_107 `DEMO_OR_CONTEST_GROUP` remove is **wrong**: HEAD tests admit demo/contest. 322947 ~163s / +$4,950 is source demo, not dest EV. Copy-all 8463 imports `RISK_BLOCKED` **−$241,580** (29). Dest **$0** (`SAFE_BY_ABSENCE`). Risk to capital **NONE** today; **HIGH** if scalps / copy-all sent 1:1. |

---

## 2026-08-18 — P500_BOOK_146 prop-challenge demo is adverse selection / copy-all copies RISK_BLOCKED

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_146 |
| Slot | 146 |
| Purpose | Measured evidence: copying prop-challenge demo accounts is adverse selection. Most accounts exist to pass a profit target then blow. Higher dest profit / lower dest loss. Honesty: wanting profit ≠ edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_146.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this slot | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = P500 pin (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**) + this-slot JSON remasure (18/8460; Achiever `demo\yo-2step` **6295/6512**; 302252 **931.54**; 322947 **104949.8**). |
| Verdict | **ADVERSE_SELECTION_CONFIRMED; HEAD_SELECTS_CHALLENGE_FACTORY; COPY_ALL_8463_NEGATIVE_EV.** 99.49% of 8460 is challenge/demo/contest. SHADOW +$78,276 is 100% demo pass-target. HEAD `CopyGroupFilter` **requires** demo/contest (BOOK_106 reject polarity **STALE**). Copy-all 8463 imports `RISK_BLOCKED` **−$241,580**. Dest **$0**. Risk **NONE** (`SAFE_BY_ABSENCE`). Wanting profit is not an edge. |

---

## 2026-08-18 — P500_BOOK_141 RiskEngine reject reasons that cut dest loss if live send existed

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_141 |
| Slot | 141 |
| Purpose | Read `RiskEngine.cs`. List every reject reason that reduces dest loss if live send existed. Measured evidence for higher dest profit / lower dest loss. Honesty: wanting profit ≠ edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_141.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = synthesis 8463 / XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**. Manager census 18/8460. |
| Verdict | **16/19_NEW_EXPOSURE_CUT; 3_TRAP_EXITS; 0_TRADER_RISK_BLOCKED; COPY_ALL_8463_DEST_RUIN; SAFE_BY_ABSENCE.** SUT 190 lines / 19 `return Reject(`. Engine grep `RISK_BLOCKED`/`TraderState`=0. Hop L273 Evaluate / L306 persist `AllowFixSend=false`. Policy **requires** demo/contest (BOOK_110/121 stale). Dest **$0**. Risk to capital **NONE** today; **HIGH / ruin** if blocked tail or catalog 8463 is sent 1:1. |

---

## 2026-08-18 — P500_BOOK_136 FIX quote bid/ask are null; cannot size or guard spread

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_136 |
| Slot | 136 |
| Purpose | Measured evidence: FIX quote bid/ask are null. Cannot size or guard spread without a quote tape. Higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_136.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Secret values printed | **None** |
| REAL_COPY flipped | **No** |
| Live `35=D` sent | **No** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave `P500_PROFIT_SYNTHESIS.md` pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0 / bid/ask **null**). |
| Verdict | **NO_TAPE_NO_SIZE_NO_SPREAD_GUARD.** Live DTO bid/ask/age **null**. Hosted FIX is one-shot `35=A` then dispose — no `35=x`/`35=V`. `CTraderQuoteService` **0 callers**, not in DI. Only `DestinationQuotes.Add` is `DemoSeeder` forged 2399.45/2399.85 (`VenueInstrumentId=null`); live host uses `BrokerCatalogSeed` (no quote row). `QuantityNormalizer` has no quote params; HEAD `AllocationFactor=1m`. `SPREAD_TOO_WIDE` / `QUOTE_STALE` / `PRICE_MOVED_TOO_FAR` cannot fire without a print; live hop hits `VENUE_NOT_RECONCILED` first (`VenueReconciled=const false`; Evaluate L273 / Reconciled L286). `MaxSlippage` unread. `shadowPnl=0` is absence. Copy-all 8463 copies `RISK_BLOCKED` −$241,580. Risk to capital **NONE** (`SAFE_BY_ABSENCE`); **HIGH** if 1:1 send armed against a null book. |

---

## 2026-08-18 — P500_BOOK_134 ML not built / deterministic baseline only

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_134 |
| Slot | 134 |
| Purpose | Measured evidence for higher dest profit / lower dest loss. Topic: ML is not built. Do not invent a model. Deterministic baseline only. Honesty: wanting profit does not create an edge; copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_134.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Secret values printed | **None** |
| REAL_COPY flipped | **No** |
| Live `35=D` sent | **No** |
| Local API | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-day P500 pin (8463 / −$154,425 / RISK_BLOCKED 29 / −$241,580 / dest $0). Manager census 18/8460. |
| Verdict | **ML_NOT_BUILT; DETERMINISTIC_BASELINE_ONLY; DO_NOT_INVENT_MODEL; COPY_ALL_8463_IMPORTS_RISK_BLOCKED; HEAD_REQUIRES_DEMO_NOT_EDGE.** `services/` empty; `src` 0 XGBoost/`IScoringService`; `mlProbability` literal null; dest PnL constructor 0; `CanPromoteToLive => false`; persist `AllowFixSend=false`; `CTraderFixSession` `35=A` only. HEAD `CopyGroupFilter` **requires** demo/contest (BOOK_114 exclude-demo **STALE**). Copy-all 8463 copies `RISK_BLOCKED` −$241,580. Dest PnL $0. Risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_139 official FIX lists trade copiers; still no send license

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_139 |
| Slot | 139 |
| Purpose | Trade-copier on cTrader FIX is officially listed; Spotware says other APIs may fit copy better. Still no license to send today. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_139.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| This-slot `:5000` GET | Blocked (SSRF). Pins from `P500_PROFIT_SYNTHESIS.md` + Manager 18/8460 re-summed. Official Help + Open API terms re-fetched. |
| Verdict | **NO_LICENSE; COPY_ALL_8463_NEGATIVE_EV.** Official https://help.ctrader.com/fix/ lists trade copiers then: “other Spotware APIs are more suitable.” RoE has TRADE `35=D`. Product hop is `35=A` only; `src/`+`apps/` `35=D=0`; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. Off-hop `CTraderFixCopyOpen` filled DEMO `5328266` (refuses live `1369850`; not invoked this slot). HEAD `CopyGroupFilter` **requires** demo/contest (BOOK_119 reject-demo stale). §68 0/19; §70 0/14. Open API terms still require trader-explicit approval. Copy-all 8463 would copy `RISK_BLOCKED` −$241,580 inside scored XAU −$154,425. Achiever 100% demo/contest; Starwave real = 28. Dest PnL $0. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_142 CTraderFixSession outbound is only 35=A

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_142 |
| Slot | 142 |
| Purpose | Read `CTraderFixSession.cs`. Prove outbound MsgType is only `A`. No `35=D`. Measured evidence for higher profit and lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_142.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| HTTP this slot | `GET :5000/api/overview` and `/api/traders` **not reachable** (SSRF block; no shell) |
| Verdict | **PASS_35A_ONLY; COPY_ALL_8463_NEGATIVE_EV.** Assigned file 135/135 is Logon `35=A` only (`WriteAsync=1`, sockets disposed). Product literal `35=D=0`. Copy-all 8463 copies `RISK_BLOCKED` **−$241,580**. Dest **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_145 Official FIX QUOTE 5211 / TRADE 5212 / cServer. Logon is not a fill

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_145 |
| Slot | 145 |
| Purpose | Measured evidence for higher profit / lower loss. Topic: Official cTrader FIX QUOTE 5211 TRADE 5212 TargetCompID cServer. Logon is not a fill. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_145.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**). Official Help/RoE/FAQs/comms-model + Spotware C# + Python config re-fetched this slot. |
| Verdict | **CONFIRMED_OFFICIAL_PORTS_AND_COMPID; LOGON_IS_NOT_A_FILL; COPY_ALL_8463_NEGATIVE_EV.** Issued `cServer` on 5211/5212; RoE table `CSERVER`; no silent fold. Copy hop one-shot `35=A` then dispose (`WriteAsync=1`; product `35=D=0`). Wanting profit ≠ edge. Copy-all 8463 imports `RISK_BLOCKED` **−$241,580**. Dest **$0**. Risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P503_R_38 never flatten MT5 source (dest-only flatten)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_38 |
| Slot | 38 |
| Purpose | Do not flatten MT5 source. Destination only. ADMIT/REMOVE/FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_38.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None** |
| Verdict | **DEST_ONLY_FLATTEN_LAW.** **ADMIT** marks dest-roster `ADMITTED` (`FlattenDestination=false`); source is GET-only signal. **REMOVE** writes `REMOVED:{reason}` and stops new dest opens; never `DealerSend`. **FLATTEN** inserts dest `CloseExposure` `FLATTEN_LOSS_CUT` (`copy:` keys); venue flatten **MISSING** (`NOS=false`, hosted `35=A` only). `ShouldFlattenOpenCopy` unwired. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). Source flatten would be unauthorized Manager dealer, not a dest hedge. |

---

## 2026-08-18 — P500_BOOK_135 in-memory DB: scores vanish on restart; cannot run a live book

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_135 |
| Slot | 135 |
| Purpose | Measured evidence for higher profit / lower loss. In-memory EF: scores vanish on restart. Cannot run a live book on RAM. Honesty: wanting profit ≠ edge. Copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_135.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (key names + literal `<SECRET>` only) |
| Local API | Not re-probed (SSRF to 127.0.0.1 blocked). Pins from synthesis 8463 / RISK_BLOCKED 29 / −$241,580 / dest $0 + Manager 18/8460. |
| Verdict | **BLOCK_NO_LIVE_BOOK_ON_RAM.** DI fail-open `UseInMemoryDatabase("trader-intelligence-live")` when CS empty / `<SECRET>`. 0 `Migrations/`; **20** DbSets (not 21); empty `Configurations/`; `EnsureCreated` ×3; workers skip `EnvFile`; Compose Postgres unwired; health `healthy:true` constant. HEAD policy **requires** demo/contest. Hosted copy filter dies with scores. Copy-all 8463 imports −$241k tail. Dest capital **NONE** today (`SAFE_BY_ABSENCE`); **HIGH** if send armed on InMemory. |

---

## 2026-08-18 — P500_BOOK_130 SHADOW 100% demo; no real Starwave/contest in measured copy set

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_130 |
| Slot | 130 |
| Purpose | SHADOW group is 100% demo. No real Starwave or contest live book in the copy set. Measured evidence for higher dest profit / lower dest loss. Honesty: wanting profit ≠ edge; copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_130.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Config / `.env` edited | **No** |
| `REAL_COPY` flipped | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true`) |
| Local API this slot | **Attempted, SSRF-blocked** (`127.0.0.1:5000`). Pins: `P500_PROFIT_SYNTHESIS.md` 8463 / SHADOW 70 +$78,276 / 100% demo; `RISK_BLOCKED` 29 −$241,580; scored XAU −$154,425; dest $0. Manager JSON re-sum 18/8460. |
| Verdict | **CONFIRMED_SHADOW_100PCT_DEMO; NO_REAL_STARWAVE_OR_CONTEST_IN_MEASURED_COPY_SET; HEAD_ADMITS_DEMO_CONTEST_REJECTS_REAL; COPY_ALL_8463_NEGATIVE_EV.** Named SHADOW 302252/303174/303274/303310/322947 are `demo\yo-2step` or `demo\yo-payp`. Contest 190 not SHADOW. Starwave real 28 / scored 0. HEAD `CopyGroupFilter` + policy/roster require demo/contest (`NOT_DEMO_OR_CONTEST_GROUP` on real) — BOOK_10/50/70/90/110 reject-demo pin is stale. Dest $0. Risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_133 MFE/MAE FeatureQuality Unavailable; exact excursion unused

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_133 |
| Slot | 133 |
| Purpose | MFE/MAE `FeatureQuality` is Unavailable. Exact excursion not used. Do not claim MAE-based stops. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_133.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Local API | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-day P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). |
| Verdict | **FEATURE_QUALITY_UNAVAILABLE; EXACT_EXCURSION_UNUSED; NO_MAE_STOPS.** Scorer always stamps `MaeMfeQuality=Unavailable`; `AverageMfe`/`AverageMae` null; `Score()` never reads them. A22 MAE floors not wired (`FLAG_MAE`/`mfe_mae_used` = 0 hits in `src`); `MfeMaeCalculator` + `mt5_xau_ticks` MISSING. Copy SL = `FinalSl ?? InitialSl` (fill clone L234). D57 VWAP mutation scores identical. Copy-all **8463** would copy `RISK_BLOCKED` **−$241,580**. Wanting profit is not an edge. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_151 RISK_BLOCKED source PnL is hundreds of thousands negative

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_151 |
| Slot | 151 |
| Purpose | Measured evidence for higher profit and lower loss. Topic: RISK_BLOCKED source PnL is hundreds of thousands negative. Copying them is how the venue blows up. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_151.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave P500 pin (8463 / XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**). Manager census 18/8460. Unscored remainder remasured **8266** (`8463−197`). Seed 10002 risk remasured **70** (not the stale 50). |
| Verdict | **NEVER_COPY_RISK_BLOCKED; COPY_ALL_8463_DEST_RUIN; SAFE_BY_ABSENCE.** Tail −$241,580 (29, all martingale, mean −$8,330) > SHADOW+WATCH +$86,454 (3.09× SHADOW). Copy-all EV = scored XAU −$154,425. HEAD exclude `{SHADOW,LIVE_CANDIDATE,LIVE}` + triple policy gate + roster dest-flatten (wired, paper). RiskEngine 0 `TRADER_RISK_BLOCKED`. Dest PnL constructor 0. `35=A` only. HEAD demo-required is not a tail filter. Risk to capital **NONE** today; **HIGH / ruin** if blocked tail or catalog 8463 is sent 1:1. |

---

## 2026-08-18 — P500_BOOK_131 RISK_BLOCKED source PnL is hundreds of thousands negative

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_131 |
| Slot | 131 |
| Purpose | Measured evidence: `RISK_BLOCKED` source PnL is hundreds of thousands negative. Copying them is how the venue blows up. Higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_131.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-day pin (8463 / −$154,425 / RISK_BLOCKED 29 / −$241,580 / dest $0). |
| Verdict | **NEVER_COPY_RISK_BLOCKED; COPY_ALL_8463_BLOWS_THE_VENUE.** Live pin 29 / **−$241,580** (all martingale, mean −$8,330) dominates scored XAU **−$154,425** (SHADOW +$78,276 < tail). HEAD demo-required (`NOT_DEMO_OR_CONTEST_GROUP`) does **not** drop the tail. Copy-all 8463 copies that tail plus **8266** unscored. Hopper excludes blocked; persist `AllowFixSend=false`; NOS unimplemented; outbound `35=A` only. Dest PnL **$0**. Risk to capital **NONE** today (`SAFE_BY_ABSENCE`); **HIGH / ruin** if the tail is sent. |

## 2026-08-18 — P500_BOOK_129 Starwave scored 0 after 91,966 deals; do not size from Achiever-only scores

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_129 |
| Slot | 129 |
| Purpose | Measured evidence for higher dest profit / lower dest loss. Topic: Starwave scored **0** while `dealsInserted=91966`. Book is incomplete. Do not size from Achiever-only scores. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_129.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` (8463 / Starwave **91966 / scored 0 / deals-done** / scored XAU **−$154,425** / `RISK_BLOCKED` **−$241,580** / dest PnL **$0**). Manager census re-summed 18/8460. HEAD remasured independently. |
| Verdict | **BOOK_INCOMPLETE_DO_NOT_SIZE; HEAD_POLICY_ADMITS_DEMO; COPY_ALL_8463_NEGATIVE_EV.** Starwave `deals-done` / `Scored=0` after **91,966** inserts is loop-3 queue (Achiever first), not an empty tape and not a lagging every-25 counter. Deal-share **26.10%** / score-share **0%**. Achiever-only SHADOW **+$78,276** is 100% demo and not a dest size. HEAD `CopyGroupFilter` **requires** demo/contest (`NOT_DEMO_OR_CONTEST_GROUP`); BOOK_109 L386 is **stale**. HEAD `AllocationFactor=1m`. Copy-all 8463 would copy `RISK_BLOCKED` 29 / **−$241,580** inside scored XAU **−$154,425**, plus 8266 no-score rows (entire Starwave catalog). Dest PnL literal **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_128 kill-switch $2000 / $500 are loss caps, not an edge

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_128 |
| Slot | 128 |
| Purpose | Measured evidence: `MaxDailyExecutionLoss=2000` and `MaxLossPerTrader=500` are loss caps, not an edge. Wanting profit does not create expectancy. Copy-all 8463 would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_128.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| REAL_COPY flipped | **No** |
| Secret values printed | **None** |
| Local API this slot | `GET :5000/api/overview` and `/api/traders` SSRF-blocked. Book pin = `P500_PROFIT_SYNTHESIS.md` + S007 + CREDENTIALS 18/8460. |
| Verdict | **LOSS_CAPS_NOT_EDGE.** Caps fire after dest (or a mis-fed source ticket) is already ≤ −$500 / −$2000; they do not read `RISK_BLOCKED`; copy hop zeros `DailyExecutionPnl` so the daily line is dead; recon short-circuits OPEN; close + `FLATTEN_LOSS_CUT` skip `Evaluate` (A71 G21–G22 FAIL if later wired). HEAD `AllocationFactor=1` makes $2,000 = one legal 5-lot $4/oz print. Copy-all 8463 EV is the scored XAU book −$154,425 (blocked tail −$241,580). Dest risk today **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — workflow live-mt5-all-groups COMPLETE

| Item | Value |
|---|---|
| Display name | `live-mt5-all-groups` |
| Planned agents | 500 |
| On-disk reports | **500** (`W500_SLICE_*` 200 + `W500_RESEARCH_*` 200 + `W500_VERIFY_*` 100) |
| Path | `D:\Prop\reports\swarm\20260818\W500_*.md` |
| Host result | slice_ok=200 research_ok=200 verify_ok=100 |
| Product source modified by workflow | No (read-write reports only) |

---

## 2026-08-18 — P500_BOOK_126 prop-challenge demo is adverse selection

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_126 |
| Slot | 126 |
| Purpose | Measured evidence: copying prop-challenge demo accounts is adverse selection. Most accounts exist to pass a profit target then blow. Higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_126.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Config / `.env` edited | **No** |
| `REAL_COPY` flipped | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the already-on-disk boolean `REAL_COPY_EXECUTION_ENABLED=true`) |
| Local API this slot | **Not re-probed** (loopback GET blocked / SSRF). Book integers from `P500_PROFIT_SYNTHESIS.md` + `P500_S007` + Manager census 18/8460 (`LIVE_GROUPS_AND_TRADERS.json` 08:42Z, header re-summed). Named balances 302252/303174/303274/303310/322947 re-checked. |
| Verdict | **ADVERSE_SELECTION_CONFIRMED; HEAD_SELECTS_CHALLENGE_FACTORY; COPY_ALL_8463_NEGATIVE_EV.** Achiever 6295/6512 = `demo\yo-2step` (0 `real\`). Combined 8417/8460 (99.49%) challenge/demo/contest. SHADOW 70 is 100% demo (+$78,276). `RISK_BLOCKED` 29 / −$241,580 (all martingale). HEAD `CopyGroupFilter` + roster **AUTO_ADMIT** demo/contest and reject real (`NOT_DEMO_OR_CONTEST_GROUP`); BOOK_86 reject-demo quote is **STALE**. Copy-all 8463 imports the blocked tail. Scored XAU −$154,425. Dest PnL **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_120 quality 95.50 vs negative netSourcePnl

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_120 |
| Slot | 120 |
| Purpose | Read `BaselineScorer.cs`. Recalculate how quality 95.50 can coexist with negative `netSourcePnl`. Quote the formula. Measured evidence for higher dest profit / lower dest loss. Honesty: wanting profit ≠ edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_120.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Verdict | **95.50_IS_XAU_SHAPE_NOT_PROFIT.** Formula `50 + 15 I_net + 10 I_12 + 5 I_18 + 0.20 b − 0.25 r`. Unique lattice `(b,r)=(90,10)` with `I_net=I_12=I_18=1`. **Cannot** sit on negative XAU `features.NetPnl` (`quality_max(I_net=0)=70`). **Can** sit on negative dashboard `netSourcePnl` because `GetTradersAsync` sums **all completed symbols**. Existence: 302252 SHADOW 95.50 / −68.46; 303174 SHADOW 95.50 / −29.38. HEAD policy **requires** demo/contest (`CopyGroupFilter`) and still ignores dashboard PnL. Copy-all 8463 copies 29 `RISK_BLOCKED` names (source tail **−$241,580**) inside scored XAU **−$154,425**. Dest PnL **$0** (`SAFE_BY_ABSENCE`). Risk to capital **NONE** this process. |

---

## 2026-08-18 — P500_BOOK_124 architecture §3 dest-net vs first-3 / copy-all

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_124 |
| Slot | 124 |
| Purpose | Read architecture §3 business goal. Future destination-net PnL is the target, not first-3 dollars. Measured evidence for higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_124.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| Localhost API | Not re-probed (SSRF block on 127.0.0.1). Used on-disk probe 18/8460 + P500 remasure 8463 / RISK_BLOCKED 29 / −$241,580 / dest PnL literal 0. |
| Verdict | **PASS as §3 reading. FAIL as a live profit claim. COPY_ALL_8463_NEGATIVE_EV. HEAD_GROUP_FILTER_SELECTS_THE_ANTI_TARGET.** First-3 / `EarlyQualityScore` is source quality, not dest-net. Copy-all 8463/8460 would spray 29 martingale `RISK_BLOCKED` names (−$241k source) plus a 100% Achiever demo/contest book onto one Pepperstone login. HEAD now **requires** demo/contest (`NOT_DEMO_OR_CONTEST_GROUP` rejects Starwave real). Higher dest profit / lower dest loss = keep `35=D` off; keep n≥20 / no RISK_BLOCKED / no lookahead; **invert** the group polarity back off challenge books; do not rank by first-3 dollars. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_132 Persist ClOrdID before send; unknown must not retry (lower-loss, not higher-profit)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_132 |
| Slot | 132 |
| Purpose | Persist `ClOrdID` before send. Unknown state must not retry. That is lower-loss, not higher-profit. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge; copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_132.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Local API | Not re-probed (SSRF to 127.0.0.1 blocked). Pins from synthesis 8463 / RISK_BLOCKED 29 / −$241,580 / dest $0 + Manager 18/8460. |
| Verdict | **LOWER_LOSS_NOT_PROFIT; SYSTEM_ARM_MISSING; COPY_ALL_8463_COPIES_RISK_BLOCKED.** Helper `MayRetry(unknown)=false`; 0 product callers; 0 intent writers; factory clock-based; no `35=H` recovery. Copy-all 8463 copies `RISK_BLOCKED` −$241,580. Dest PnL $0. Risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_117 allocation must stay 0.01–0.05 until dest shadow EV after costs is positive

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_117 |
| Slot | 117 |
| Purpose | Measured evidence: allocation factor must be tiny (0.01–0.05 of source) until shadow expectancy after costs is positive. Higher profit / lower loss. Honesty: wanting profit is not an edge; copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_117.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**) + Manager census 18/8460. |
| Verdict | **ALLOCATION_MUST_STAY_TINY.** HEAD `AllocationFactor=1m` (1:1) is dest-ruin if sent. Dest 0.05 ticket cap **MISSING**. Shadow EV after costs **not proven** (hosted hop `VenueReconciled=false` → `VENUE_NOT_RECONCILED`; dest PnL $0; live shadow $0). Copy-all 8463 copies `RISK_BLOCKED` −$241k. Tiny α shrinks the hole; it does not mint an edge. Slot 17 `0.50×0.01=0.01` cell **wrong** (SUT **0**). W500 `×0.05` hop **STALE**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_125 Official cTrader FIX QUOTE 5211 / TRADE 5212 / TargetCompID cServer

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_125 |
| Slot | 125 |
| Purpose | Official cTrader FIX: QUOTE 5211 TRADE 5212 TargetCompID cServer. Logon is not a fill. Measured evidence for higher profit and lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_125.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-day pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). |
| Verdict | **CONFIRMED_OFFICIAL_PORTS_AND_COMPID. LOGON_IS_NOT_A_FILL. NO_DEST_EDGE. COPY_ALL_8463_NEGATIVE_EV.** Official SSL QUOTE 5211 / TRADE 5212; issued `cServer` (RoE `CSERVER`; no silent fold). Hosted hop one-shot `35=A` then dispose. Copy-all 8463 copies `RISK_BLOCKED` −$241,580. Dest $0. Risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_123 TradeReconstructor / 303274 same-second 0.05 grid

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_123 |
| Slot | 123 |
| Purpose | Read `TradeReconstructor` and 303274-style overlapping 0.05-lot same-second entries. Is grid flagged? Evidence for higher profit / lower loss. Do not modify product. Never enable REAL_COPY. Never send 35=D. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_123.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API | `GET :5000/api/overview` and `/api/traders` **SSRF-blocked**; census from synthesis + catalog |
| SUT | `TradeReconstructor.cs` 347 lines; `GroupBy(PositionId)` L46 + `ScaleIn` worse-than-VWAP latch only; `src` grep WasGrid/GridFlag/IsGrid/same-second = **0** |
| Catalog | login **303274** `demo\yo-2step` 16228.24 (`LIVE_GROUPS_AND_TRADERS.json` L2564–2568) |
| Policy drift vs BOOK_83 | HEAD `CopyGroupFilter` **requires** demo/contest. 303274 is **eligible** / roster `AUTO_ADMIT`. Unit `Demo_group_blocked` **gone**. |
| Verdict | **GRID_NOT_FLAGGED.** Distinct hedge 0.05s never `ScaleIn`. No `WasGrid`. 303274-class averaging/martingale false; SHADOW reachable. HEAD demo filter **admits** this login. Copy-all 8463 would copy `RISK_BLOCKED` losses (−$241,580). Dest capital **NONE** today (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_97 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_97 |
| Slot | 97 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_97.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 file-proven (DemoSeeder off API boot; Native `GroupRequestArray("*")`/`GroupTotal`; `UserRequestArray`/`UserLogins`; `CTraderFixSession` 135/135 is `35=A` only). Claim 5 **disproved**: DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`; logon host no re-pin. Copy hop `SAFE_BY_ABSENCE`. Risk **NONE**. |

---

## 2026-08-18 — P503_R_24 WATCH→SHADOW+20 XAU+PnL>0 auto-ADMIT (no human click)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_24 |
| Slot | 24 |
| Purpose | Auto-add: when a WATCH trader reaches SHADOW + 20 XAU + PnL>0, must roster admit without a human click. ADMIT/REMOVE/FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_24.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Verdict | **ADMIT=auto (no click)** once WATCH becomes SHADOW ∧ n≥20 ∧ XAU PnL>0 ∧ demo/contest ∧ clean. `Decide` → `Admit`/`AUTO_ADMIT`/`FlattenDestination=false`. `TickRosterAsync` persists `ADMITTED`. **REMOVE=not this path** (still-WATCH is `Keep`/`NOT_YET`). **FLATTEN=false on ADMIT**. Real groups `NOT_DEMO_OR_CONTEST_GROUP`. Dest capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_115 in-memory DB: scores vanish on restart; cannot run a live book

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_115 |
| Slot | 115 |
| Purpose | Measured evidence for higher profit / lower loss. In-memory EF: scores vanish on restart. Cannot run a live book on RAM. Honesty: wanting profit ≠ edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_115.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this slot | Loopback GET blocked (SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **−$241,580** / dest PnL **$0** / wipe ~09:01Z). HEAD remasured independently. |
| Verdict | **BLOCK_NO_LIVE_BOOK_ON_RAM.** DI fail-open `UseInMemoryDatabase("trader-intelligence-live")` when CS empty / `<SECRET>`. 0 `Migrations/`; empty `Configurations/`; `EnsureCreated` ×3; workers skip `EnvFile`; Compose Postgres unwired; health `healthy:true` constant. Hosted copy hopper L184–187 dies with scores. Copy-all 8463 imports −$241k tail. Dest capital **NONE** today (`SAFE_BY_ABSENCE`); **HIGH** if send armed on InMemory. |

---

## 2026-08-18 — P500_BOOK_118 never flatten MT5 source (dest-only flatten)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_118 |
| Slot | **118** |
| Purpose | Measured evidence for higher dest profit / lower dest loss. Topic: never flatten the MT5 source. Destination-only flatten. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_118.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**) + Manager census 18/8460 re-summed. |
| Verdict | **DEST_ONLY_FLATTEN_LAW.** Source C# path GET-only (0 `DealerSend`; `PositionRequest`/`PositionGetByGroup` only). Roster **WIRED**: dest `FLATTEN_LOSS_CUT` intents only; P503_R_1 UNWIRED **STALE**. `ShouldFlattenOpenCopy` DEAD on hop. Hosted FIX `35=A` only. Product `EmergencyFlatten` blocks opens only. Copy hop hardcodes `KillSwitch=None` L287. Close/flatten hops skip `Evaluate`; qty = source lots × `AllocationFactor=1m`. Demo dest-721 flatten refuses `1369850`. Scored XAU **−$154,425**; `RISK_BLOCKED` **−$241,580** (29). Dest PnL **$0** (`SAFE_BY_ABSENCE`). Copy-all 8463 would import that tail. Risk to capital **NONE** today; **DEST_RUIN_IF_SENT** if copy-all / blocked tail / 1:1 or 5-lot dest flatten. |

---

## 2026-08-18 — P504 copied ACHIEVER 305750 open 0.01 XAU to demo cTrader

| Item | Value |
|---|---|
| Fill | **4390.2** dest pos 237339770 |
| Source | 305750 / 21250421 Long 0.01 still open |
| Dest | demo 5328266 only |
| Artifact | `P504_COPY_SENT.md` |

---

## 2026-08-18 — P503_R_34 auto-add WATCH→SHADOW + 20 XAU + PnL>0

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_34 |
| Slot | 34 |
| Purpose | Auto-add: when a WATCH trader reaches SHADOW + 20 XAU + PnL>0, must roster admit without a human click. Verdict must state ADMIT / REMOVE / FLATTEN. Never print secrets. Never send 35=D. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_34.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** |
| Verdict | **CLICKLESS ADMIT (demo/contest only).** **ADMIT=`AUTO_ADMIT`** when scorer writes SHADOW and n≥20 and XAU net>0 and no size flags and `IsDemoOrContest`; `FlattenDestination=false`; no UI/POST. **WATCH-only = no ADMIT** (`Keep` / `NOT_YET_TRADER_NOT_SHADOW_YET`). **Real groups = REMOVE + dest-only FLATTEN** (`NOT_DEMO_OR_CONTEST_GROUP`) even at SHADOW+20+PnL. Never flatten MT5. Dest **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_116 FIX quote bid/ask are null; cannot size or guard spread

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_116 |
| Slot | 116 |
| Purpose | Measured evidence: FIX quote bid/ask are null. Cannot size or guard spread without a quote tape. Higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_116.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0 / bid/ask **null**). |
| Verdict | **NO_TAPE_NO_SIZE_NO_SPREAD_GUARD.** Live DTO bid/ask/age **null**. Hosted FIX is one-shot `35=A` then dispose — no `35=x`/`35=V`. `CTraderQuoteService` **0 callers**, not in DI. Only `DestinationQuotes.Add` is `DemoSeeder` forged 2399.45/2399.85 (`VenueInstrumentId=null`); live host uses `BrokerCatalogSeed` (no quote row). `QuantityNormalizer` has no quote params; HEAD `AllocationFactor=1m` (BOOK_16 0.05 **STALE**). `SPREAD_TOO_WIDE` cannot fire; hop hits `VENUE_NOT_RECONCILED` first; `MaxSlippage` unread. HEAD `CopyGroupFilter` **admits demo/contest only** (BOOK_90/96 demo-reject **STALE**). `shadowPnl=0` is absence. Copy-all 8463 copies `RISK_BLOCKED` −$241,580. Risk to capital **NONE** (`SAFE_BY_ABSENCE`); **HIGH** if 1:1 / demo-only send armed against a null book. |

---

## 2026-08-18 — W500_VERIFY_99 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_99 |
| Slot | 99 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_99.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files (2–3 capability only). Claim 5 **disproven**: `.env` L73 `true` + API `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

---

## 2026-08-18 — W500_VERIFY_96 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_96 |
| Slot | 96 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_96.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files. Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; logon host does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_VERIFY_98 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_98 |
| Slot | 98 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_98.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files (2–3 capability only). Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — P503_R_37 Peak-to-trough XAU drawdown should remove the trader

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_37 |
| Slot | 37 |
| Purpose | Peak-to-trough XAU drawdown should remove the trader. Verdict must state ADMIT / REMOVE / FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_37.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** |
| Verdict | **REMOVE + DEST-ONLY FLATTEN; DO_NOT_ADMIT.** On-roster completed-XAU `maxDd/peak >= 0.40` → `RemoveAndFlatten` / `DRAWDOWN_FROM_PEAK` + dest `FLATTEN_LOSS_CUT` intents. **ADMIT forbidden** on the same breach; HEAD `AUTO_ADMIT`s when `alreadyOnRoster=false` and **re-ADMITs** after REMOVE (no latch / oscillation). Never flatten MT5 source. No `35=D`. Dest capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_94 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_94 |
| Slot | 94 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_94.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files (2–3 capability only). Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_107 XAUUSD copy cost: spread + slippage + 15s MaxSourceSignalAge. Scalps die.

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_107 |
| Slot | 107 |
| Purpose | Measured evidence for higher profit / lower loss. Topic: XAUUSD copy cost = dest spread + slippage + 15s `MaxSourceSignalAge` reject. Scalps die. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_107.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = synthesis 8463 / XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**. Manager census 18/8460. 322947 JSON remasure `demo\yo-payp` 104949.8. |
| Verdict | **SCALPS_DIE_AFTER_COSTS; COPY_ALL_8463_NEGATIVE_EV.** Dest taker spread (seed 0.40 / allowed 2.0 = $40–$200 per 1.00 lot) + unread `MaxSlippage=1.5` + 15s `SIGNAL_STALE` on OPEN. Hosted poll **20 s > 15 s** clock (≥25% first-sight miss). Roster **no hold gate**. 322947 ~163s / +$4,950 is source demo, not dest EV. Copy-all 8463 imports `RISK_BLOCKED` **−$241,580** (29). Dest **$0** (`SAFE_BY_ABSENCE`). Risk to capital **NONE** today; **HIGH** if scalps / copy-all sent 1:1. |

---

## 2026-08-18 — P503_R_13 single dest loser UPnL cap must not flatten

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_13 |
| Slot | 13 |
| Purpose | Should a single losing open dest trade be flattened if unrealized loss exceeds a cap? Verdict must state ADMIT/REMOVE/FLATTEN implication. |
| Artifact | `reports/swarm/20260818/P503_R_13.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Secret values printed | **None** |
| REAL_COPY flipped | **No** |
| Live `35=D` sent | **No** |
| Verdict | **NO_SINGLE_DEST_UPNL_FLATTEN.** ADMIT unchanged. REMOVE not on one open dest loser. FLATTEN dest not at `MaxUnrealizedLossLotsUsd=150` (`ShouldFlattenOpenCopy` unwired; `Decide`/`TickRoster` never mark dest). Dest flatten remains login REMOVE or source close, dest-only. `$150` is a loss cap not an edge (1:1 5-lot = $0.30/oz). Dest $0 `SAFE_BY_ABSENCE`. |

---

## 2026-08-18 — P503_R_36 consecutive-loss pause + dest flatten

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_36 |
| Slot | 36 |
| Purpose | Consecutive loss streak should pause the trader and flatten opens. ADMIT/REMOVE/FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_36.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Verdict | **REMOVE+DEST_FLATTEN_INTENT; ADMIT_IGNORES_STREAK; NO_TRADERSTATE_PAUSE.** ≥3 completed XAU losses **should** pause new opens and flatten dest. **ADMIT** does not check streak and re-ADMITS after REMOVE. **REMOVE** is roster `REMOVED:CONSECUTIVE_LOSSES_n` only if already admitted (`Decide` L66–68; `TickRosterAsync` wired). **FLATTEN** is dest-only `FLATTEN_LOSS_CUT` intent (15 s, no dest id, no NOS). Never MT5. Risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_95 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_95 |
| Slot | 95 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_95.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| REAL_COPY flipped | **No** |
| Live `35=D` sent | **No** |
| Live attach this slot | **No** |
| Verdict | **FAIL.** Claims 1–4 PASS from live files. Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_VERIFY_86 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_86 |
| Slot | 86 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_86.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 file-proven (2–3 capability only). Claim 5 disproven: `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` + `DependencyInjection.cs` L41 bind + no hosted re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_122 CTraderFixSession outbound is only 35=A

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_122 |
| Slot | 122 |
| Purpose | Read `CTraderFixSession.cs`. Prove outbound MsgType is only `A`. No `35=D`. Measured evidence for higher profit and lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_122.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). |
| Verdict | **PASS_35A_ONLY; COPY_ALL_8463_NEGATIVE_EV.** Assigned 135/135: outbound MsgType `(35,"A")` only; `WriteAsync=1`; `35=D=0`; sockets disposed. Persist overwrite is **L306** (older L211 pin stale). Wanting profit is not an edge. Copy-all 8463 would copy `RISK_BLOCKED` losses (pin 29 / −$241,580 inside scored XAU −$154,425). Dest PnL $0. Env REAL_COPY may be true; sender missing. Risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_113 MFE/MAE FeatureQuality Unavailable; exact excursion unused

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_113 |
| Slot | 113 |
| Purpose | MFE/MAE `FeatureQuality` is Unavailable. Exact excursion not used. Do not claim MAE-based stops. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_113.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Live API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**). |
| Verdict | **FEATURE_QUALITY_UNAVAILABLE; EXACT_EXCURSION_UNUSED; NO_MAE_STOPS.** Scorer always stamps `MaeMfeQuality=Unavailable`; `AverageMfe`/`AverageMae` null; `Score()` never reads them. A22 MAE floors not wired (`FLAG_MAE`/`mfe_mae_used` = 0 hits in `src`); `MfeMaeCalculator` + `mt5_xau_ticks` MISSING. Copy SL = `FinalSl ?? InitialSl` (L234 fill clone). D57 VWAP mutation scores identical. Copy-all **8463** would copy `RISK_BLOCKED` **−$241,580**. Wanting profit is not an edge. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_94 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_94 |
| Slot | 94 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_94.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files (2–3 capability only). Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_VERIFY_80 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_80 |
| Slot | 80 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_80.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from files (2/3 PASS_SOURCE). Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

---

## 2026-08-18 — W500_VERIFY_90 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_90 |
| Slot | 90 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_90.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files (2–3 capability only). Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_VERIFY_82 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_82 |
| Slot | 82 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_82.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 file-proven (DemoSeeder off API boot; `GroupRequestArray("*")`/`GroupTotal`; `UserRequestArray`/`UserLogins`; `CTraderFixSession` 135/135 is `35=A` only). Claim 5 **disproved**: DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`; logon host no re-pin. Copy hop `SAFE_BY_ABSENCE`. Risk **NONE**. |

---

## 2026-08-18 — W500_VERIFY_91 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_91 |
| Slot | 91 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_91.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files. Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_119 official FIX lists trade copiers; still no send license

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_119 |
| Slot | 119 |
| Purpose | Trade-copier on cTrader FIX is officially listed; Spotware says other APIs may fit copy better. Still no license to send today. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_119.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| This-slot `:5000` GET | Blocked (SSRF). Pins from `P500_PROFIT_SYNTHESIS.md` + Manager 18/8460. Official Help + Open API terms re-fetched. |
| Verdict | **NO_LICENSE; COPY_ALL_8463_NEGATIVE_EV.** Official https://help.ctrader.com/fix/ lists trade copiers then: “other Spotware APIs are more suitable.” RoE has TRADE `35=D`. Product hop is `35=A` only; `src/`+`apps/` `35=D=0`; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. §68 0/19; §70 0/14. Open API terms still require trader-explicit approval. Copy-all 8463 would copy `RISK_BLOCKED` −$241,580 inside scored XAU −$154,425. Achiever 100% demo/contest; Starwave real = 28. Dest PnL $0. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_110 SHADOW group is 100% demo; no real Starwave or contest in the copy set

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_110 |
| Slot | **110** |
| Purpose | Measured evidence for higher profit / lower loss. Topic: SHADOW group is 100 percent demo. No real Starwave or contest live book in the copy set. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_110.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = P500 pin (8463 / SHADOW 70 100% demo / RISK_BLOCKED −$241,580 / dest $0). |
| Siblings | 10 / 50 / 70 / 90 same question; this slot re-measures. Increment: roster `ADMITTED` double-gate empties layer C. |
| Verdict | **CONFIRMED_SHADOW_100PCT_DEMO; NO_REAL_STARWAVE_OR_CONTEST_IN_COPY_SET; COPY_ALL_8463_NEGATIVE_EV.** 70 SHADOW = `demo\yo-2step`/`demo\yo-payp`. Contest 190 not in copy set. Starwave real 28 scored=0. Copy-all 8463 copies `RISK_BLOCKED` −$241,580 + 8266 un-scored. Dest $0. Risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_93 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_93 |
| Slot | 93 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_93.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1/4 PASS. 2/3 PASS_SOURCE. Claim 5 **disproven**: `.env` L73 `true` + API `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

---

## 2026-08-18 — W500_VERIFY_83 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_83 |
| Slot | 83 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_83.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from files (2/3 PASS_SOURCE). Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; logon host does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_106 prop-challenge demo is adverse selection / copy-all copies RISK_BLOCKED

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_106 |
| Slot | 106 |
| Purpose | Measured evidence: copying prop-challenge demo accounts is adverse selection. Most accounts exist to pass a profit target then blow. Higher dest profit / lower dest loss. Honesty: wanting profit ≠ edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_106.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API | `:5000` not re-probed (`web_fetch` SSRF on `127.0.0.1`). Pins: synthesis 8463 / Manager 8460 / dest $0. |
| Verdict | **ADVERSE_SELECTION_CONFIRMED. DEMO_CHALLENGE_NOT_DEST_EDGE. COPY_ALL_8463_NEGATIVE_EV.** Achiever `demo\yo-2step` 6295/6512. Combined challenge/demo 8417/8460 (99.49%). SHADOW 70 / +$78,276 is 100% demo. `RISK_BLOCKED` 29 / −$241,580 (all martingale). Scored XAU −$154,425. Dest PnL literal **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_84 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_84 |
| Slot | 84 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_84.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files (2–3 capability only). Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; logon host does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_109 Starwave scored 0 after 91966 deals; do not size from Achiever-only scores

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_109 |
| Slot | 109 |
| Purpose | Starwave scored 0 while dealsInserted 91966. Book is incomplete. Do not size from Achiever-only scores. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_109.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| This-slot `:5000` GET | Blocked (SSRF). Pins from `P500_PROFIT_SYNTHESIS.md` + Manager 18/8460 + HEAD remasure. |
| Verdict | **BOOK_INCOMPLETE_DO_NOT_SIZE; COPY_ALL_8463_NEGATIVE_EV.** Starwave `deals-done` / `Scored=0` after **91,966** inserts is pipeline order (Achiever scores first), not an empty tape. Achiever-only SHADOW **+$78,276** is 100% demo and not a dest size. HEAD `AllocationFactor=1m` (0.05 pin stale). Copy-all 8463 would copy `RISK_BLOCKED` 29 / **−$241,580** inside scored XAU **−$154,425**, plus **8266** names with no `TraderScore` (entire Starwave catalog). Dest PnL literal **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_111 RISK_BLOCKED source PnL is hundreds of thousands negative

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_111 |
| Slot | 111 |
| Purpose | Measured evidence for higher profit and lower loss. Topic: RISK_BLOCKED source PnL is hundreds of thousands negative. Copying them is how the venue blows up. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_111.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave P500 pin (8463 / XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**). Manager census 18/8460. Unscored remainder remasured **8266** (`8463−197`). |
| Verdict | **NEVER_COPY_RISK_BLOCKED; COPY_ALL_8463_DEST_RUIN; SAFE_BY_ABSENCE.** Tail −$241,580 (29, all martingale, mean −$8,330) &gt; SHADOW+WATCH +$86,454 (3.09× SHADOW). Copy-all EV = scored XAU −$154,425. HEAD exclude `{SHADOW,LIVE_CANDIDATE,LIVE}` + triple policy gate + roster dest-flatten. RiskEngine 0 `TRADER_RISK_BLOCKED`. Dest PnL constructor 0. `35=A` only. Risk to capital **NONE** today; **HIGH / ruin** if blocked tail or catalog 8463 is sent 1:1. |

---

## 2026-08-18 — P503_R_27 peak-to-trough XAU drawdown should remove the trader

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_27 |
| Slot | 27 |
| Purpose | Does completed-trade peak-to-trough XAU drawdown remove the trader? ADMIT / REMOVE / FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_27.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** |
| Verdict | **REMOVE+dest-FLATTEN** when `alreadyOnRoster` and `DrawdownFromPeak` `dd/peak ≥ 0.40` (`DRAWDOWN_FROM_PEAK`). **ADMIT skips DD**, so a still-green giveback is auto-admitted and **re-admitted** after `REMOVED:*` (`onRoster` requires `Status==ADMITTED`). Hop `TickRosterAsync` is live (482-line `CopyTradingService`). Flatten writes dest `FLATTEN_LOSS_CUT` close intents only — no `35=D`, never MT5 source. Dest capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_103 TradeReconstructor / 303274 same-second 0.05 grid

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_103 |
| Slot | 103 |
| Purpose | Read `TradeReconstructor` and 303274-style overlapping 0.05-lot same-second entries. Is grid flagged? Evidence for higher profit / lower loss. Do not modify product. Never enable REAL_COPY. Never send 35=D. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_103.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| SUT | `TradeReconstructor.cs` 347 lines; `GroupBy(PositionId)` L46 + `ScaleIn` worse-than-VWAP latch only; `src`/`tests` grep grid/WasGrid/same-second = **0** |
| HEAD vs BOOK_83 | Copy hop now `CopyRosterEngine` + `ADMITTED` gate. Still **not** a grid flag. Live-group 0.05 spray `AUTO_ADMIT`s (`AllocationFactor=1`). |
| Catalog | login **303274** `demo\yo-2step` 16228.24 (`LIVE_GROUPS_AND_TRADERS.json` L2564–2568) |
| Live API | `:5000` not re-probed (`web_fetch` SSRF on `127.0.0.1`). Pins: synthesis 8463 / Manager 8460 / dest $0. |
| Verdict | **GRID_NOT_FLAGGED.** Distinct hedge 0.05s never `ScaleIn`. No `WasGrid`. 303274-class averaging/martingale false; SHADOW reachable. Demo+roster reject is not a grid detector. Copy-all 8463 would copy `RISK_BLOCKED` losses (−$241,580). Dest capital **NONE** today (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_89 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_89 |
| Slot | 89 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_89.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from files (2–3 capability only). Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — P503_R_8 dest-only flatten (ADMIT / REMOVE / FLATTEN)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_8 |
| Slot | 8 |
| Purpose | Do not flatten MT5 source. Destination-only flatten. Verdict must state ADMIT / REMOVE / FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_8.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** |
| Verdict | **DEST_ONLY_FLATTEN_LAW.** ADMIT never flattens (dest roster `ADMITTED`, `FlattenDestination=false`). REMOVE dest-flattens dest copy intents only (`FLATTEN_LOSS_CUT`; no `DealerSend`). FLATTEN is dest remaining / dest copy-intent qty; product FIX flatten run **MISSING**; engine `EmergencyFlatten` blocks opens only. Source C# path GET-only. Hosted FIX `35=A` only. Copy-all 8463 imports `RISK_BLOCKED` −$241,580 inside scored XAU −$154,425. Dest PnL **$0** (`SAFE_BY_ABSENCE`). HEAD `AllocationFactor=1m` **UNSAFE if sent**. Risk to capital **NONE** today; **DEST_RUIN_IF_SENT** if copy-all / blocked tail / 1:1 dest send. |

---

## 2026-08-18 — W500_VERIFY_88 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_88 |
| Slot | 88 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_88.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 file-proven (DemoSeeder off API boot; Native `GroupRequestArray("*")`/`GroupTotal`; `UserRequestArray`/`UserLogins`; `CTraderFixSession` 135/135 is `35=A` only). Claim 5 **disproved**: `.env` L73 `true` + API `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop `SAFE_BY_ABSENCE`. Risk to capital **NONE**. |

---

## 2026-08-18 — P503_R_16 consecutive loss streak pause+flatten

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_16 |
| Slot | 16 |
| Purpose | Consecutive loss streak should pause the trader and flatten opens. ADMIT/REMOVE/FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_16.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** |
| Verdict | **REMOVE+DEST_FLATTEN; DO_NOT_ADMIT; NOT_A_PAUSE_STATE.** On-roster streak ≥3 → `RemoveAndFlatten` / `CONSECUTIVE_LOSSES_n` + dest `FLATTEN_LOSS_CUT`. No `TraderState.PAUSED`. Off-roster streak AUTO_ADMIT oscillates. Never flatten MT5 source. Dest capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_77 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_77 |
| Slot | 77 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_77.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 file-proven: DemoSeeder off API boot; Native `GroupRequestArray("*")`/`GroupTotal`; `UserRequestArray`/`UserLogins`; `CTraderFixSession` 135/135 is `35=A` only. Claim 5 **disproven**: `.env` L73 `true` + DI L41 binds it; logon host no re-pin. Copy hop `SAFE_BY_ABSENCE`. Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_112 Persist ClOrdID before send; unknown must not retry

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_112 |
| Slot | 112 |
| Purpose | Persist `ClOrdID` before send. Unknown state must not retry. That is lower-loss, not higher-profit. Honesty: wanting profit does not create an edge; copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_112.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None** |
| Local API | Not re-probed (SSRF to 127.0.0.1 blocked). Pins from synthesis 8463 / RISK_BLOCKED 29 / −$241,580 / dest $0 + Manager 18/8460. |
| Verdict | **LOWER_LOSS_NOT_PROFIT; SYSTEM_ARM_MISSING; COPY_ALL_8463_COPIES_RISK_BLOCKED.** Helper `MayRetry(unknown)=false`; 0 product callers; 0 intent writers; factory clock-based; no `35=H` recovery. Copy-all 8463 copies `RISK_BLOCKED` −$241,580. Dest PnL $0. Risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P503_R_23 single dest loser unrealized-cap flatten

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_23 |
| Slot | 23 |
| Purpose | Should a single losing open dest trade be flattened if unrealized loss exceeds a cap? ADMIT/REMOVE/FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_23.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** |
| Verdict | **KEEP_NOT_REMOVE; DO_NOT_ADMIT_$150_DEST_MAE_STOP; FLATTEN_DEST_ONLY_THAT_COPY_UNWIRED.** `ShouldFlattenOpenCopy(<= -150)` is unit-tested and uncalled. `TickRosterAsync` flattens all `:open` rows only on trader `RemoveAndFlatten` (`FLATTEN_LOSS_CUT` intents, no dest 721, no `35=D`). $150@α=1 is noise (1 lot = $1.50/oz). MAE Unavailable. A48 forbids auto-flatten-from-threshold. Dest PnL $0 (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_87 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_87 |
| Slot | 87 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_87.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 file-proven (DemoSeeder off API boot; Native `GroupRequestArray("*")`/`GroupTotal`; `UserRequestArray`/`UserLogins`; `CTraderFixSession` 135/135 is `35=A` only). Claim 5 **disproved**: DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`; logon host no re-pin. Copy hop `SAFE_BY_ABSENCE`. Risk **NONE**. |

---

## 2026-08-18 — P503_R_14 Auto-add WATCH→SHADOW+20+PnL>0 must ADMIT without a click

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_14 |
| Slot | 14 |
| Purpose | Auto-add: when a WATCH trader reaches SHADOW + 20 XAU + PnL>0, must roster admit without a human click. Verdict must say ADMIT/REMOVE/FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_14.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Verdict | **ADMIT=AUTO (no click)** at SHADOW∧N≥20∧XAU PnL>0∧not demo/size-pattern/blocked (`CopyRosterEngine` `AUTO_ADMIT` + hosted `TickRosterAsync` writes `ADMITTED`; hopper requires it). **WATCH-only=NOT ADMIT** (`NOT_YET_TRADER_NOT_SHADOW_YET`). **Later fail=REMOVE+dest-intent FLATTEN** (`FLATTEN_LOSS_CUT`; never MT5 source; no `35=D`). Dest capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_105 Official cTrader FIX: QUOTE 5211 TRADE 5212 TargetCompID cServer. Logon is not a fill

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_105 |
| Slot | 105 |
| Purpose | Official cTrader FIX: QUOTE 5211 / TRADE 5212 / TargetCompID `cServer`. Logon is not a fill. Measured evidence for higher profit / lower loss. Honesty: wanting profit ≠ edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_105.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Local API | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-day P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). Manager census 18/8460. |
| Verdict | **LOGON_IS_NOT_A_FILL; COPY_ALL_8463_NEGATIVE_EV.** Official SSL QUOTE **5211** / TRADE **5212**; issued `TargetCompID=cServer` (RoE table `CSERVER`; no silent fold). Copy hop one-shot `35=A` then dispose. `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. Copy-all 8463 copies `RISK_BLOCKED` **−$241,580**. Dest PnL literal **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_79 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_79 |
| Slot | 79 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_79.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 file-proven (2–3 capability only). Claim 5 disproved: `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` + `DependencyInjection.cs` L41 bind + no hosted re-pin. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P503_R_29 CloseExposure after REMOVE so dest can flatten

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_29 |
| Slot | 29 |
| Purpose | CloseExposure must still be allowed after remove so dest can flatten. Verdict must state ADMIT/REMOVE/FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_29.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** |
| Verdict | **ADMIT=new opens only; REMOVE=stop opens; FLATTEN=dest-only CloseExposure that MUST remain legal after REMOVE.** Hop mints `FLATTEN_LOSS_CUT` (bypass). Policy `Evaluate` + source-close hopper reject Close after remove. Risk L117–124 would trap dest if wired. No sender. Dest $0 (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P503_R_8 dest-only flatten (ADMIT / REMOVE / FLATTEN)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_8 |
| Slot | 8 |
| Purpose | Do not flatten MT5 source. Destination-only flatten. Verdict must state ADMIT / REMOVE / FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_8.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** |
| Verdict | **DEST_ONLY_FLATTEN_LAW.** ADMIT never flattens (dest roster `ADMITTED`, `FlattenDestination=false`). REMOVE dest-flattens dest copy intents only (`FLATTEN_LOSS_CUT`; no `DealerSend`). FLATTEN is dest remaining / dest copy-intent qty; product FIX flatten run **MISSING**; engine `EmergencyFlatten` blocks opens only. Source C# path GET-only. Hosted FIX `35=A` only. Copy-all 8463 imports `RISK_BLOCKED` −$241,580 inside scored XAU −$154,425. Dest PnL **$0** (`SAFE_BY_ABSENCE`). HEAD `AllocationFactor=1m` **UNSAFE if sent**. Risk to capital **NONE** today; **DEST_RUIN_IF_SENT** if copy-all / blocked tail / 1:1 dest send. |

---

## 2026-08-18 — P500_BOOK_108 kill-switch dollars are loss caps, not an edge

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_108 |
| Slot | 108 |
| Purpose | Measured evidence: `MaxDailyExecutionLoss=2000` and `MaxLossPerTrader=500` are loss caps, not an edge. Wanting profit does not create expectancy. Copy-all 8463 would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_108.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** |
| Verdict | **LOSS_CAPS_NOT_EDGE.** Caps fire after dest (or a mis-fed source ticket) is already ≤ −$500 / −$2000; they do not read `RISK_BLOCKED`; copy hop zeros `DailyExecutionPnl` so the daily line is dead; `VenueReconciled=false` short-circuits OPEN before L117; close hop skips `Evaluate` (A71 G21–G22 FAIL if later wired). HEAD `AllocationFactor=1` makes $2,000 = one 5-lot $4/oz print. Copy-all 8463 EV is the scored XAU book −$154,425 (blocked tail −$241,580). Dest risk today **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P503_R_6 consecutive loss streak pause + flatten opens

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_6 |
| Slot | 6 |
| Purpose | Consecutive loss streak should pause the trader and flatten opens. ADMIT/REMOVE/FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_6.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Verdict | **ADMIT ignores streak and re-ADMITS after REMOVE. REMOVE is non-sticky roster drop (`REMOVED:CONSECUTIVE_LOSSES_n`), not `TraderState.PAUSED`. FLATTEN is dest-only paper `FLATTEN_LOSS_CUT` (no `35=D`, never MT5 source). Pause does not happen. Dest capital NONE (`SAFE_BY_ABSENCE`).** |

---

## 2026-08-18 — P503_R_17 peak-to-trough XAU drawdown removes the trader

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_17 |
| Slot | 17 |
| Purpose | Peak-to-trough XAU drawdown should remove the trader. Verdict must state ADMIT/REMOVE/FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_17.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** |
| Verdict | **REMOVE + DEST-ONLY FLATTEN; do not ADMIT.** Completed-XAU `dd/peak >= 0.40` (`peak > 0`) → `RosterAction.RemoveAndFlatten` / `DRAWDOWN_FROM_PEAK` even if net still green (fixture +$300 / 70%). Hop writes `REMOVED:DRAWDOWN_FROM_PEAK` + `FLATTEN_LOSS_CUT` dest-close intents. Never flatten MT5 source. HEAD still **ADMIT**s first-seen 40%+ curves (`alreadyOnRoster` gate). Venue flatten `SAFE_BY_ABSENCE` (no `35=D`). Risk to capital **NONE** today. |

---

## 2026-08-18 — W500_VERIFY_70 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_70 |
| Slot | 70 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL any unproven claim. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_70.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 file-proven (DemoSeeder off API boot; `GroupRequestArray("*")`/`GroupTotal`; `UserRequestArray`/`UserLogins`; `CTraderFixSession` 135/135 is `35=A` only). Claim 5 **disproved**: DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`; logon host no re-pin. Copy hop `SAFE_BY_ABSENCE`. Risk **NONE**. |

## 2026-08-18 — P503_R_28 dest-only flatten (ADMIT / REMOVE / FLATTEN)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_28 |
| Slot | 28 |
| Purpose | Do not flatten MT5 source. Destination-only flatten. Verdict must state ADMIT / REMOVE / FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_28.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** |
| Verdict | **DEST_ONLY_FLATTEN_LAW.** ADMIT implication: roster `ADMITTED`, `FlattenDestination=false`, source untouched. REMOVE implication: eject dest-copy (`REMOVED:{reason}`), stop new dest opens, never Manager close. FLATTEN implication: dest `CloseExposure` `FLATTEN_LOSS_CUT` qty = dest-open remaining; C# `DealerSend`/`SendTrade` = 0; `IMt5BrokerConnector` GET-only (`PositionRequest`/`PositionRequestByGroup`). Live dest flatten **MISSING** (`CTraderFixSession` `35=A` only; `NewOrderSingleImplemented=false`). Demo CLI refuses dest `1369850`. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_100 quality 95.50 vs negative netSourcePnl

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_100 |
| Slot | 100 |
| Purpose | Read `BaselineScorer.cs`. Recalculate how quality 95.50 can coexist with negative `netSourcePnl`. Quote the formula. Measured evidence for higher profit / lower loss. Honesty: wanting profit is not an edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_100.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| This-slot `:5000` GET | Blocked (SSRF). Pins from synthesis + Manager 18/8460. 302252/303174 dollars re-checked against `LIVE_GROUPS_AND_TRADERS.json`. Independent re-read: ingest drops SL (`Mt5Deal` has no SL field). |
| Verdict | **CONFIRMED_SPLIT_NOT_EDGE.** 95.50 = `50+15+10+5+18−2.5` at `(b,r)=(90,10)` only; requires XAU `NetPnl>0` and `PF>=1.8`. Dashboard `netSourcePnl` is all-symbol Σ. Live ingest forces unused-SL. 302252 (−68.46) / 303174 (−29.38) match catalog `1000−balance`. Copy-all 8463 would copy `RISK_BLOCKED` −$241,580. Dest PnL $0. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_78 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_78 |
| Slot | 78 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_78.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files. Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; logon host does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_97 allocation must stay 0.01–0.05 until dest shadow EV after costs is positive

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_97 |
| Slot | 97 |
| Purpose | Measured evidence: allocation factor must be tiny (0.01–0.05 of source) until shadow expectancy after costs is positive. Higher profit / lower loss. Honesty: wanting profit is not an edge; copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_97.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-day P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). Census remainder **8266** (8463−197). |
| Verdict | **ALLOCATION_MUST_STAY_TINY.** HEAD `AllocationFactor=1m` (1:1) is dest-ruin if sent. Hosted hop cannot emit after-cost shadow fills (`VenueReconciled=false` → `VENUE_NOT_RECONCILED`). Copy-all 8463 copies `RISK_BLOCKED` −$241k. Tiny α shrinks the hole; it does not mint an edge. 70×2.00 same-side at α=1 = 14,000 oz ($14k per $1/oz). Slot 17 `0.50×0.01=0.01` cell **wrong** (SUT **0**). W500 `×0.05` hop **STALE**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_73 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_73 |
| Slot | 73 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL any unproven claim. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_73.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files (2–3 capability only). Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; logon host does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

## 2026-08-18 — P503_R_7 peak-to-trough XAU DD should remove

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_7 |
| Slot | 7 |
| Purpose | Peak-to-trough XAU drawdown should remove the trader. Verdict must state ADMIT/REMOVE/FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_7.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** |
| Verdict | **REMOVE + DEST-ONLY FLATTEN; do not ADMIT.** `MaxDrawdownVsPeak=0.40` on completed-XAU equity, rostered only (`DRAWDOWN_FROM_PEAK`). First-tick `AUTO_ADMIT` still possible if net>0. Flatten is `FLATTEN_LOSS_CUT` intents. Source MT5 never flattened. Dest risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P503_R_15 demo groups stay excluded even if profitable

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_15 |
| Slot | 15 |
| Purpose | Demo/contest groups must stay excluded even if they look profitable. State ADMIT / REMOVE / FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_15.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Secret values printed | **None** |
| REAL_COPY flipped | **No** |
| Live `35=D` sent | **No** |
| Local API this slot | Not re-probed. Book pin = `P500_PROFIT_SYNTHESIS.md` (SHADOW 70 / +$78,276 / 100% demo; RISK_BLOCKED 29 / −$241,580). |
| Verdict | **KEEP_EXCLUDED. ADMIT=FORBIDDEN** for `demo\`/`contest\` even if SHADOW + n≥20 + XAU-net>0. **REMOVE=`RemoveAndFlatten` / `REMOVED:DEMO_OR_CONTEST_GROUP`.** **FLATTEN=dest-only** intent `FLATTEN_LOSS_CUT` (never MT5 source; no `35=D`). Unit lock `Demo_group_never_admitted`. Residuals: null `GroupName` fail-open; `Starwave\demo\*` prefix miss. Dest $0. Risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_76 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_76 |
| Slot | 76 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_76.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files (2–3 capability only). Claim 5 **disproven**: `.env` L73 `true` + API `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

---

## 2026-08-18 — P503_R_25 demo groups stay excluded even if they look profitable

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_25 |
| Slot | 25 |
| Purpose | Demo groups must stay excluded even if they look profitable. State ADMIT / REMOVE / FLATTEN implication. Honesty: SHADOW challenge dollars ≠ dest edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_25.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Local API this slot | **Not re-probed** (SSRF to 127.0.0.1 blocked). Pins: `P500_PROFIT_SYNTHESIS.md` 8463 / SHADOW 70 +$78,276 / `RISK_BLOCKED` 29 −$241,580 / scored XAU −$154,425 / dest $0; Manager 18/8460. |
| Verdict | **DO_NOT_ADMIT; REMOVE_AND_DEST_FLATTEN (`DEMO_OR_CONTEST_GROUP`).** Green demo (303310 +$41,634; SHADOW head +$78,276) still never `Admit`. Roster writes `REMOVED:DEMO_OR_CONTEST_GROUP` and dest-only `FLATTEN_LOSS_CUT` if dest copies exist. Source flatten forbidden. Residual: `Starwave\demo\` prefix hole (1905) + null `GroupName` fail-open. Dest capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_74 adversarial live-path (slot 74)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_74 |
| Slot | 74 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_74.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 proven from files (2–3 capability only). Claim 5 disproven: `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` + `DependencyInjection.cs` L41 bind + no hosted re-pin. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_75 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_75 |
| Slot | 75 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL any unproven claim. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_75.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1/4 PASS. 2/3 PASS_SOURCE. Claim 5 **disproven**: `.env` L73 `true` + API `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

---

## 2026-08-18 — P503_R_9 CloseExposure after REMOVE so dest can FLATTEN

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_9 |
| Slot | 9 |
| Purpose | Reviewer: CloseExposure must still be allowed after remove so dest can flatten. Verdict must state ADMIT/REMOVE/FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_9.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| Verdict | **REMOVE stops ADMIT/opens; CloseExposure must still be allowed so dest can FLATTEN.** `CopyRosterEngine.Remove` + `TickRosterAsync`/`FlattenOpenCopiesAsync` honor it on paper (`FLATTEN_LOSS_CUT`, bypass `Evaluate`). `XauUsdOneToOneCopyPolicy.Evaluate` L119 and `GenerateShadowIntentsAsync` L213–216 after `REMOVED` veto Close (trap dest). `RiskEngine` L117–124 would freeze flatten if wired. Live dest flatten **MISSING**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_67 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_67 |
| Slot | 67 |
| Purpose | Adversarial verify from live files: DemoSeeder not API startup; Native can list all groups via GroupRequestArray or GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_67.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from files (2–3 capability only). Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_VERIFY_66 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_66 |
| Slot | 66 |
| Purpose | Adversarial verify from live files: DemoSeeder not API startup; Native can list all groups via GroupRequestArray or GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_66.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files (2–3 capability only; not re-attached). Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; logon host does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_104 architecture §3 dest-net vs first-3 / copy-all

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_104 |
| Slot | 104 |
| Purpose | Read architecture §3 business goal. Future destination-net PnL is the target, not first-3 dollars. Measured evidence for higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_104.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Secret values printed | **None** |
| REAL_COPY flipped | **No** |
| Live `35=D` sent | **No** |
| Local API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book integers from same-day `P500_PROFIT_SYNTHESIS.md`; census re-summed from `LIVE_GROUPS_AND_TRADERS.json` 08:42Z (18/8460). Named balances 302252/303174/303274/303310/322947 re-checked. |
| Verdict | **PASS_§3_DEST_NET_NOT_FIRST3; COPY_ALL_8463_NEGATIVE_EV.** Target is future dest-net inside risk limits. First-3 $ are the anti-target. Wanting profit ≠ edge. Copy-all 8463 imports `RISK_BLOCKED` −$241,580. Dest PnL literal 0. Risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P503_R_22 RiskEngine MaxLossPerTrader: per copied trader on each tick?

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_22 |
| Slot | 22 |
| Purpose | Read `RiskEngine` `MaxLossPerTrader`. Is it applied per copied trader on each tick? Verdict must state ADMIT / REMOVE / FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_22.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Verdict | **NO_PER_TRADER_NO_PER_TICK.** `MaxLossPerTrader=500` is a stateless `TraderRealizedLoss <= -500` on **one source ticket**, called once per new OPEN intent. Not keyed by login. Not on MT5/FIX ticks. Hosted loop is 20 s: `TickRosterAsync` ADMIT/REMOVE/FLATTEN via `CopyRosterEngine` (does not read `$500`); flatten writes `FLATTEN_LOSS_CUT` without `Evaluate`. Product OPEN short-circuits at `VENUE_NOT_RECONCILED` before L117. **ADMIT=none; REMOVE=none; FLATTEN=none today; ANTI-FLATTEN if Evaluate is put on close (A71 G21 FAIL).** Tests 0 facts. Dest capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_71 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_71 |
| Slot | 71 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_71.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1/4 PASS. 2/3 PASS_SOURCE. Claim 5 **disproven**: `.env` L73 `true` + API `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

---

## 2026-08-18 — W500_VERIFY_68 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_68 |
| Slot | 68 |
| Purpose | Adversarial verify from live files: DemoSeeder not API startup; Native groups via GroupRequestArray or GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. FAIL if unproven. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_68.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from files. Claim 5 FAIL: `.env` L73 `true`, `EnvFile.FindAndLoad`, DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`, logon host does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — P503_R_19 CloseExposure after REMOVE so dest can FLATTEN

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_19 |
| Slot | 19 |
| Purpose | CloseExposure must still be allowed after remove so dest can flatten. Verdict must state ADMIT/REMOVE/FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_19.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** |
| Verdict | **ADMIT opens only; REMOVE stops new opens not dest exits; FLATTEN is dest-only CloseExposure after REMOVE. HEAD inverts: IsTraderEligible + {SHADOW,LIVE_CANDIDATE,LIVE} hopper reject Close on the same conditions that emit RemoveAndFlatten. Roster engine DEAD (unwired). Dest cannot flatten. Risk NONE (`SAFE_BY_ABSENCE`).** |

---

## 2026-08-18 — P500_BOOK_101 RiskEngine reject reasons that cut dest loss if live send existed

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_101 |
| Slot | 101 |
| Purpose | Read `RiskEngine.cs`. List every reject reason that reduces dest loss if live send existed. Honesty: wanting profit is not an edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_101.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-day pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). |
| Verdict | **16 / 19** `Reject()` reasons cut **new-exposure** dest loss if they sat in front of a sender. **3 / 19** (`MAX_LOSS_PER_TRADER`, `MAX_DAILY_EXECUTION_LOSS`, `MAX_PORTFOLIO_DRAWDOWN`) freeze closes and **increase** trapped loss. **0** emit `TRADER_RISK_BLOCKED`. Copy-all 8463 remains **−EV**. Dest capital **NONE** today (`SAFE_BY_ABSENCE`: no NOS, persist `AllowFixSend=false`). Policy `AllocationFactor=1` (BOOK_1 0.05 pin stale). |

---

## 2026-08-18 — P500_BOOK_102 CTraderFixSession outbound is only 35=A

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_102 |
| Slot | 102 |
| Purpose | Read `CTraderFixSession.cs`. Prove outbound MsgType is only `A`. No `35=D`. Measured evidence for higher profit and lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_102.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). |
| Verdict | **PASS_35A_ONLY; COPY_ALL_8463_NEGATIVE_EV.** Assigned 135/135: outbound MsgType `(35,"A")` only; `WriteAsync=1`; `35=D=0`; sockets disposed. Wanting profit is not an edge. Copy-all 8463 would copy `RISK_BLOCKED` losses (pin 29 / −$241,580 inside scored XAU −$154,425). Dest PnL $0. Env REAL_COPY may be true; sender missing. Risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_98 never flatten MT5 source (dest-only flatten)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_98 |
| Slot | 98 |
| Purpose | Measured evidence for higher profit / lower loss. Never flatten MT5 source. Destination-only flatten. Honesty: wanting profit ≠ edge. Copy-all 8463 imports RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_98.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| REAL_COPY flipped | **No** |
| Secret values printed | **None** |
| Live API | `:5000` not re-probed (`web_fetch` SSRF on `127.0.0.1`). Pins: synthesis 8463 / Manager 8460. |
| Verdict | **DEST_ONLY_FLATTEN_LAW.** Source C# path GET-only (0 `DealerSend`; `PositionRequest`/`PositionGetByGroup` only). Hosted FIX `35=A` only. Product `EmergencyFlatten` blocks opens only — no dest run. Demo dest-721 flatten refuses `1369850`. Scored XAU **−$154,425**; `RISK_BLOCKED` **−$241,580** (29). Dest PnL **$0** (`SAFE_BY_ABSENCE`). HEAD `AllocationFactor=1m` **UNSAFE if sent**. Copy-all 8463 would import that tail. BOOK_78 absent. Risk to capital **NONE** today; **DEST_RUIN_IF_SENT** if copy-all / blocked tail / 1:1 or 5-lot dest flatten. |

---

## 2026-08-18 — P500_BOOK_96 FIX quote bid/ask are null; cannot size or guard spread

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_96 |
| Slot | 96 |
| Purpose | Measured evidence: FIX quote bid/ask are null. Cannot size or guard spread without a quote tape. Higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_96.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave `P500_PROFIT_SYNTHESIS.md` pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0 / bid/ask **null**). |
| Verdict | **NO_TAPE_NO_SIZE_NO_SPREAD_GUARD.** Live DTO bid/ask/age **null**. Hosted FIX is one-shot `35=A` then dispose — no `35=x`/`35=V`. `CTraderQuoteService` **0 callers**, not in DI. Only `DestinationQuotes.Add` is `DemoSeeder` forged 2399.45/2399.85 (`VenueInstrumentId=null`); live host uses `BrokerCatalogSeed` (no quote row). `QuantityNormalizer` has no quote params; HEAD `AllocationFactor=1m`. `SPREAD_TOO_WIDE` / `QUOTE_STALE` / `PRICE_MOVED_TOO_FAR` cannot fire without a print; live hop hits `VENUE_NOT_RECONCILED` first (`VenueReconciled=const false`). `MaxSlippage` unread. `shadowPnl=0` is absence. Copy-all 8463 copies `RISK_BLOCKED` −$241,580. Risk to capital **NONE** (`SAFE_BY_ABSENCE`); **HIGH** if 1:1 send armed against a null book. |

---

## 2026-08-18 — W500_VERIFY_65 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_65 |
| Slot | 65 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL any unproven claim. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_65.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from files. Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; logon host does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — P503_R_20 policy has no auto-remove when XAU PnL goes negative after admit

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_20 |
| Slot | 20 |
| Purpose | Read `XauUsdOneToOneCopyPolicy.cs`. Is there auto-remove when XAU PnL goes negative after admit? ADMIT/REMOVE/FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_20.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Verdict | **NO_AUTO_REMOVE_IN_POLICY.** ADMIT requires `XauNetPnl>0`. After admit, `XauNetPnl<=0` is REJECT (`XAU_BOOK_NOT_PROFITABLE`), not REMOVE, not FLATTEN. `CopyRosterEngine` would `RemoveAndFlatten` dest-only (`XAU_BOOK_TURNED_NEGATIVE`) but is UNWIRED. Production hop `continue`s the trader (also blocks source-close copy). Dest flatten MISSING. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_94 ML is not built; deterministic baseline only

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_94 |
| Slot | 94 |
| Purpose | Measured evidence for higher dest profit / lower dest loss. Topic: ML is not built. Do not invent a model. Deterministic baseline only. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_94.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). |
| Verdict | **ML_NOT_BUILT_CORRECT.** `D:\Prop\services` empty; `mlProbability` literal null; ranker is `BaselineScorer` rules (`CanPromoteToLive => false`). Copy policy already excludes `RISK_BLOCKED` / n<20 / demo. Copy-all 8463 would include 29 `RISK_BLOCKED` names (source tail **−$241,580**) and a scored XAU book **−$154,425**. Dest PnL **$0** (`SAFE_BY_ABSENCE`). Inventing XGBoost/LLM does not create dest edge. Risk to capital **NONE** this process. |

---

## 2026-08-18 — W500_VERIFY_63 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_63 |
| Slot | 63 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_63.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files. Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; logon host does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — P503_R_1 CopyTradingService later RISK_BLOCKED dest flatten?

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P503_R_1 |
| Slot | 1 |
| Purpose | Read `CopyTradingService`. Does it flatten dest when trader is later `RISK_BLOCKED`? ADMIT/REMOVE/FLATTEN implication. |
| Artifact | `D:\Prop\reports\swarm\20260818\P503_R_1.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Verdict | **NO_DEST_FLATTEN_ON_LATER_RISK_BLOCKED.** **ADMIT=never** (L94–95 hopper + policy reject). **REMOVE=silent drop** (blocked names leave `scores`; no new OpenExposure; close-mirror skipped). **FLATTEN=absent** (`CopyRosterEngine.RemoveAndFlatten` / `FlattenDestination=true` unwired; 0 product callers; `KillSwitch=None`; persist `AllowFixSend=false`; `NewOrderSingleImplemented=false`). Orphan dest lots if send is later wired. Dest capital **NONE** today (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_89 Starwave scored 0 after 91,966 deals

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_89 |
| Slot | 89 |
| Purpose | Measured evidence for higher dest profit / lower dest loss. Topic: Starwave scored **0** while `dealsInserted=91966`. Book is incomplete. Do not size from Achiever-only scores. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_89.md` |
| Product edited | **No** |
| Live `35=D` | **Not sent** |
| `REAL_COPY` flipped | **No** |
| Live API this slot | Loopback GET blocked (SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` (8463 / Starwave **91966 / scored 0 / deals-done** / scored XAU **−$154,425** / `RISK_BLOCKED` **−$241,580** / dest PnL **$0**). HEAD remasured independently. |
| Verdict | **BOOK_INCOMPLETE.** `scored=0` + `phase=deals-done` is loop-3 queue (Achiever first), not an empty tape and not a lagging every-25 counter. Achiever-only SHADOW ranks are not a size book. HEAD `α=1`. Copy-all 8463 copies the blocked tail. Dest **$0** (`SAFE_BY_ABSENCE`). Risk to capital **NONE** this process. |

---

## 2026-08-18 — W500_VERIFY_62 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_62 |
| Slot | 62 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_62.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files. Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; logon host does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

## 2026-08-18 — W500_VERIFY_59 adversarial live-path (slot 59)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_59 |
| Slot | 59 |
| Purpose | Adversarial verify from live files: DemoSeeder not API startup; Native ALL groups via GroupRequestArray/GroupTotal; ALL traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_59.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files. Claim 5 FAIL: `.env` L73 `true` + API `EnvFile.FindAndLoad()` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_VERIFY_64 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_64 |
| Slot | 64 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_64.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files. Claim 5 FAIL: DI L41 binds env; `.env` L73 is `true`; hosted logon does not re-pin; `/api/settings` echoes runtime. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_VERIFY_60 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_60 |
| Slot | 60 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_60.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the boolean `REAL_COPY_EXECUTION_ENABLED=true`) |
| Verdict | **FAIL.** Claims 1/4 PASS. 2/3 PASS_SOURCE. Claim 5 **disproven**: `.env` L73 `true` + API `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_91 RISK_BLOCKED source PnL is hundreds of thousands negative

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_91 |
| Slot | 91 |
| Purpose | Measured evidence: `RISK_BLOCKED` source PnL is hundreds of thousands negative. Copying them is how the venue blows up. Higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_91.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** (loopback GET blocked / SSRF). Book numbers from `P500_PROFIT_SYNTHESIS.md` §1 + `P500_S007` + CREDENTIALS 18/8460. Code re-read independently of sibling BOOK_51/71. |
| Verdict | **NEVER_COPY_RISK_BLOCKED; COPY_ALL_8463_BLOWS_THE_VENUE.** Live pin 29 / **−$241,580** (all martingale, mean −$8,330) dominates scored XAU **−$154,425** (SHADOW +$78,276 < tail). Copy-all 8463 copies that tail plus **8266** unscored. Product allow-list excludes blocked; persist `AllowFixSend=false`; NOS unimplemented; outbound `35=A` only. Dest PnL **$0**. Risk to capital **NONE** today (`SAFE_BY_ABSENCE`); **HIGH / ruin** if the tail is sent. |

## 2026-08-18 — W500_VERIFY_58 adversarial live-path (slot 58)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_58 |
| Slot | 58 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_58.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 proven from files (2–3 capability only). Claim 5 disproven: `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` + `DependencyInjection.cs` L41 bind + no hosted re-pin. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_55 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_55 |
| Slot | 55 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_55.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files. Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_VERIFY_49 adversarial live-path re-read

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_49 |
| Slot | 49 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_49.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS (2–3 source-only). Claim 5 disproven: `.env` L73 `true` + DI L41 bind + no hosted re-pin. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P503 auto-roster admit/remove/flatten

| Item | Value |
|---|---|
| Workflow | `copy-roster-harden` (60 agents) |
| Code | `CopyRosterEngine` + `TickRosterAsync` |
| Tests | 21/21 PASS |
| Verdict | Losers removed + dest flatten intents. Winners auto-admitted. Source MT5 never flattened. `35=D` still off. |

---

## 2026-08-18 — W500_VERIFY_48 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_48 |
| Slot | 48 |
| Purpose | Adversarial verify from live files: DemoSeeder not API startup; Native lists all groups via GroupRequestArray or GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_48.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from files (2/3 PASS_SOURCE). Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; logon host does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_88 kill-switch $2000 / $500 are loss caps, not an edge

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_88 |
| Slot | 88 |
| Purpose | Measured evidence: `MaxDailyExecutionLoss=2000` and `MaxLossPerTrader=500` are loss caps, not an edge. Wanting profit does not create expectancy. Copy-all 8463 would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_88.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). |
| Verdict | **LOSS_CAPS_NOT_EDGE.** Caps fire after dest (or a mis-fed source ticket) is already ≤ −$500 / −$2000; they do not read `RISK_BLOCKED`; copy hop zeros `DailyExecutionPnl` so the daily line is dead; `VenueReconciled=false` short-circuits OPEN before L117; close hop skips `Evaluate` (A71 G21–G22 FAIL if later wired); settings 5%/10-lot catalog unbound; DI `AddSingleton<RiskEngine>` unused (`new()` on hop). Copy-all 8463 EV is the scored XAU book −$154,425 (blocked tail −$241,580). Dest risk today **NONE** (`SAFE_BY_ABSENCE`). |

## 2026-08-18 — P500_BOOK_78 never flatten MT5 source (dest-only flatten)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_78 |
| Slot | **78** |
| Purpose | Measured evidence for higher dest profit / lower dest loss. Topic: never flatten the MT5 source. Destination-only flatten. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_78.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**) + Manager census 18/8460. |
| Verdict | **DEST_ONLY_FLATTEN_LAW.** Source C# path GET-only (0 `DealerSend`; `PositionRequest`/`PositionGetByGroup` only). Hosted FIX `35=A` only. Product `EmergencyFlatten` blocks opens only — no dest run. Copy hop hardcodes `KillSwitch=None` (DB flatten **DEAD**). Close hop skips `Evaluate` and sizes source lots × `AllocationFactor=1m`. Demo dest-721 flatten refuses `1369850`. Scored XAU **−$154,425**; `RISK_BLOCKED` **−$241,580** (29). Dest PnL **$0** (`SAFE_BY_ABSENCE`). Copy-all 8463 would import that tail. Risk to capital **NONE** today; **DEST_RUIN_IF_SENT** if copy-all / blocked tail / 1:1 or 5-lot dest flatten. |

---

## 2026-08-18 — W500_VERIFY_53 adversarial live-path re-read

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_53 |
| Slot | 53 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_53.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files. Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_92 Persist ClOrdID before send; unknown must not retry

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_92 |
| Slot | 92 |
| Purpose | Persist `ClOrdID` before send. Unknown state must not retry. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge; copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_92.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Local API | Not re-probed (SSRF to 127.0.0.1 blocked). Pins from synthesis 8463 / RISK_BLOCKED 29 / −$241,580 / dest $0 + Manager 18/8460. |
| Verdict | **LOWER_LOSS_NOT_PROFIT; SYSTEM_ARM_MISSING; COPY_ALL_8463_COPIES_RISK_BLOCKED.** Helper `MayRetry(unknown)=false`; 0 product callers; 0 intent writers; factory clock-based; no `35=H` recovery. Copy-all 8463 copies `RISK_BLOCKED` −$241,580. Dest PnL $0. Risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_93 MFE/MAE FeatureQuality Unavailable; exact excursion unused

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_93 |
| Slot | 93 |
| Purpose | MFE/MAE `FeatureQuality` is Unavailable. Exact excursion not used. Do not claim MAE-based stops. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_93.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| This-slot `:5000` GET | Blocked (SSRF). Pins from `P500_PROFIT_SYNTHESIS.md`. |
| Verdict | **FEATURE_QUALITY_UNAVAILABLE; EXACT_EXCURSION_UNUSED; NO_MAE_STOPS.** Scorer always stamps `MaeMfeQuality=Unavailable`; `AverageMfe`/`AverageMae` null; `Score()` never reads them. A22 MAE floors not wired (`FLAG_MAE`/`mfe_mae_used` = 0 hits in `src`); `MfeMaeCalculator` + `mt5_xau_ticks` MISSING. Copy SL = `FinalSl ?? InitialSl` (fill clone). D57 VWAP mutation scores identical. Copy-all **8463** would copy `RISK_BLOCKED` **−$241,580**. Wanting profit is not an edge. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_61 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_61 |
| Slot | 61 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_61.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from files. Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; logon host does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

## 2026-08-18 — W500_VERIFY_46 adversarial live-path (DemoSeeder / Native request APIs / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_46 |
| Slot | 46 |
| Purpose | Adversarial verify from live files: DemoSeeder not API startup; Native lists all groups via GroupRequestArray or GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. FAIL any unproven claim. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_46.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** (lab `.env` L73 already `true`; not changed) |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 file-proven (DemoSeeder off API boot; `GroupRequestArray("*")`/`GroupTotal`; `UserRequestArray`/`UserLogins`; `CTraderFixSession` 135/135 is `35=A` only). Claim 5 **disproved**: DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`; logon host no re-pin. Copy hop `SAFE_BY_ABSENCE`. Risk **NONE**. |

---

## 2026-08-18 — W500_VERIFY_51 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_51 |
| Slot | 51 |
| Purpose | Adversarial verify from live files: DemoSeeder not API startup; Native lists all groups via GroupRequestArray or GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. FAIL any unproven claim. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_51.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| Verdict | **FAIL.** Claims 1–4 PASS from files. Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; logon host does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_83 TradeReconstructor / 303274 same-second 0.05 grid

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_83 |
| Slot | 83 |
| Purpose | Read `TradeReconstructor` and 303274-style overlapping 0.05-lot same-second entries. Is grid flagged? Evidence for higher profit / lower loss. Do not modify product. Never enable REAL_COPY. Never send 35=D. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_83.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| This-slot `:5000` GET | Blocked (SSRF). Pins from `P500_PROFIT_SYNTHESIS.md` + `LIVE_GROUPS_AND_TRADERS.json`. |
| SUT | `TradeReconstructor.cs` 347 lines; `GroupBy(PositionId)` L46 + `ScaleIn` worse-than-VWAP latch only |
| Catalog | login **303274** `demo\yo-2step` 16228.24 (`LIVE_GROUPS_AND_TRADERS.json` L2564–2568) |
| Verdict | **GRID_NOT_FLAGGED.** Distinct hedge 0.05s never `ScaleIn`. No `WasGrid`. 303274-class averaging/martingale false; SHADOW reachable. 1:1 `DEMO_OR_CONTEST_GROUP` is not a grid detector. Copy-all 8463 would copy `RISK_BLOCKED` losses (−$241,580). Dest capital **NONE** today (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_90 SHADOW group is 100% demo; no real Starwave or contest in the copy set

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_90 |
| Slot | 90 |
| Purpose | Measured evidence for higher profit / lower loss. Topic: SHADOW group is 100 percent demo. No real Starwave or contest live book in the copy set. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_90.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Local API this slot | **Not re-probed** (SSRF to 127.0.0.1 blocked). Book pin = `P500_PROFIT_SYNTHESIS.md` + `P500_S007` (8463 / SHADOW **70** 100% demo / `RISK_BLOCKED` **29 / −$241,580** / scored XAU **−$154,425** / dest PnL **$0**). Manager census independently re-summed 8/6512 + 10/1948 = 18/8460 (08:42Z JSON). |
| Verdict | **CONFIRMED_SHADOW_100PCT_DEMO; NO_REAL_STARWAVE_OR_CONTEST_IN_COPY_SET; COPY_ALL_8463_NEGATIVE_EV.** Named SHADOW 302252/303174/303274/303310/322947 are `demo\yo-2step` or `demo\yo-payp`. Achiever 190 contest + 0 real. Starwave real **28** unscored. Policy rejects `demo\`/`contest\` so layer-C copy set is empty vs the 70 SHADOW rows. Copy-all 8463 would import `RISK_BLOCKED` −$241k. Dest **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_54 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_54 |
| Slot | 54 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_54.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files. Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; logon host does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_VERIFY_42 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_42 |
| Slot | 42 |
| Purpose | Adversarial verify from live files: DemoSeeder not API startup; Native groups via GroupRequestArray or GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_42.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from files. Claim 5 FAIL: DI L41 binds env; `.env` L73 is `true`; hosted logon does not re-pin; `/api/settings` echoes runtime. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

## 2026-08-18 — P500_BOOK_75 in-memory DB: scores vanish on restart; cannot run a live book

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_75 |
| Slot | 75 |
| Purpose | Measured evidence for higher profit / lower loss. In-memory EF: scores vanish on restart. Cannot run a live book on RAM. Honesty: wanting profit ≠ edge. Copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_75.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (key names + literal token `<SECRET>` only) |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = synthesis 8463 / XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**. Manager census 18/8460. Measured wipe pin: synthesis addendum ~09:01Z. |
| Verdict | **BLOCK_NO_LIVE_BOOK_ON_RAM.** DI fail-open `UseInMemoryDatabase("trader-intelligence-live")` when CS empty / `<SECRET>`. 0 `Migrations/`; empty `Configurations/`; `EnsureCreated` ×3; workers skip `EnvFile`; Compose Postgres unwired; health `healthy:true` constant. Hosted copy filter dies with scores. Copy-all 8463 imports −$241k tail. Dest capital **NONE** today (`SAFE_BY_ABSENCE`); **HIGH** if send armed on InMemory. |

---

## 2026-08-18 — W500_VERIFY_50 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_50 |
| Slot | 50 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL any unproven claim. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_50.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from files. Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; logon host does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_86 prop-challenge demo is adverse selection

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_86 |
| Slot | 86 |
| Purpose | Measured evidence: copying prop-challenge demo accounts is adverse selection. Most accounts exist to pass a profit target then blow. Higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_86.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Local API this slot | **Not re-probed** (loopback GET blocked / SSRF). Book integers from `P500_PROFIT_SYNTHESIS.md` + Manager census 18/8460 (`LIVE_GROUPS_AND_TRADERS.json` 08:42Z, header re-read). |
| Verdict | **ADVERSE_SELECTION_CONFIRMED; COPY_ALL_8463_NEGATIVE_EV.** Achiever 6295/6512 = `demo\yo-2step` (0 `real\`). Combined 8417/8460 (99.49%) challenge/demo/contest. SHADOW 70 is 100% demo. `RISK_BLOCKED` 29 / −$241,580 (all martingale). Scored XAU −$154,425. Policy rejects `demo\`/`contest\` (`DEMO_OR_CONTEST_GROUP`). Copy-all 8463 imports the blocked tail. Dest PnL **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_85 official cTrader FIX ports / CompID / logon≠fill

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_85 |
| Slot | 85 |
| Purpose | Official cTrader FIX: QUOTE TLS 5211 / TRADE TLS 5212 / issued TargetCompID `cServer`. Prove Logon is not a fill. Measured evidence for higher dest profit / lower dest loss. Honesty: wanting profit ≠ edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_85.md` |
| Product source modified | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** (lab `.env` already `true`; this slot did not change it) |
| This-slot `:5000` GET | Blocked (SSRF). Pins from `P500_PROFIT_SYNTHESIS.md` + Manager 18/8460. Official Help + Spotware sample re-fetched. |
| Verdict | **CONFIRMED_OFFICIAL_PORTS_AND_COMPID. LOGON_IS_NOT_A_FILL. NO_DEST_EDGE.** Hosted hop is one-shot `35=A` then dispose. Copy-all 8463 would copy `RISK_BLOCKED` −$241,580. Dest PnL $0. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_47 adversarial live-path re-read

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_47 |
| Slot | 47 |
| Purpose | Adversarial verify from live files: DemoSeeder not API startup; Native groups via GroupRequestArray/GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_47.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true`) |
| Verdict | **FAIL.** Claims 1–4 PASS from files. Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; logon host does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_VERIFY_41 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_41 |
| Slot | 41 |
| Purpose | Adversarial verify from live files: DemoSeeder not API startup; Native groups via GroupRequestArray or GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. FAIL any unproven claim. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_41.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Live attach this pass | **No** |
| Verdict | **FAIL.** Claims 1–4 file-proven: DemoSeeder off API boot; Native `GroupRequestArray("*")`/`GroupTotal`; `UserRequestArray`/`UserLogins`; `CTraderFixSession` 135/135 is `35=A` only. Claim 5 **disproven**: `.env` L73 `true` + DI L41 binds it; logon host no re-pin. Copy hop `SAFE_BY_ABSENCE`. Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_80 quality 95.50 vs negative netSourcePnl

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_80 |
| Slot | 80 |
| Purpose | Read `BaselineScorer.cs`. Recalculate how quality 95.50 can coexist with negative `netSourcePnl`. Quote the formula. Measured evidence for higher dest profit / lower dest loss. Honesty: wanting profit ≠ edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_80.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Secret values printed | **None** |
| REAL_COPY flipped | **No** |
| Live `35=D` sent | **No** |
| Local API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book integers from same-day `P500_PROFIT_SYNTHESIS.md`; 302252/303174 dollars re-checked against `LIVE_GROUPS_AND_TRADERS.json` balances 931.54 / 970.62. |
| Verdict | **95.50_IS_XAU_SHAPE_NOT_PROFIT.** Formula `50 + 15 I_net + 10 I_12 + 5 I_18 + 0.20 b − 0.25 r`. Unique lattice `(b,r)=(90,10)` with `I_net=I_12=I_18=1`. **Cannot** sit on negative XAU `features.NetPnl` (`quality_max(I_net=0)=70`). **Can** sit on negative dashboard `netSourcePnl` because `GetTradersAsync` sums **all completed symbols**. Existence: 302252 SHADOW 95.50 / −68.46; 303174 SHADOW 95.50 / −29.38. Copy-all 8463 copies 29 `RISK_BLOCKED` names (source tail **−$241,580**) inside scored XAU **−$154,425**. Dest PnL **$0** (`SAFE_BY_ABSENCE`). Risk to capital **NONE** this process. |

---

## 2026-08-18 — W500_VERIFY_52 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_52 |
| Slot | 52 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists all groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL any unproven claim. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_52.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files. Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; logon host does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_VERIFY_44 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_44 |
| Slot | 44 |
| Purpose | Adversarial verify from live files: DemoSeeder not API startup; Native groups via GroupRequestArray/GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_44.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from files. Claim 5 FAIL: DI L41 binds env; API `EnvFile.FindAndLoad()`; `.env` L73 `true`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

---

## 2026-08-18 — W500_VERIFY_37 Adversarial live-path verify

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_37 |
| Slot | 37 |
| Purpose | Adversarial verify from live files: DemoSeeder not API startup; Native ALL groups via GroupRequestArray/GroupTotal; ALL traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. FAIL if unproven. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_37.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from files. Claim 5 FAIL: `.env` L73 `true`, API `EnvFile.FindAndLoad()`, DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`, logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` 35=A only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_84 architecture §3 dest-net vs first-3 / copy-all

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_84 |
| Slot | 84 |
| Purpose | Read architecture v2 §3 Primary Business Goal. Future destination-net PnL is the target, not first-3 dollars. Measured evidence for higher profit / lower loss. Honesty: wanting profit ≠ edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_84.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API | `:5000` not re-probed (`web_fetch` SSRF on `127.0.0.1`). Pins: synthesis 8463 / Manager 8460 / dest $0. |
| Verdict | **PASS_§3_DEST_NET_NOT_FIRST3; COPY_ALL_8463_NEGATIVE_EV.** §3 anti-target is first-3 $. Coded filter is `MinCompletedXauTrades=20` + drop `RISK_BLOCKED`/`demo\`. Copy-all 8463 would import `RISK_BLOCKED` 29 / −$241,580. Scored XAU −$154,425. Dest PnL literal **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_81 RiskEngine reject reasons that cut dest loss if live send existed

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_81 |
| Slot | 81 |
| Purpose | Read `RiskEngine.cs`. List every reject reason that reduces dest loss if live send existed. Measured evidence for higher dest profit / lower dest loss. Honesty: wanting profit ≠ edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_81.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Local API | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-day P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). |
| Verdict | **16 / 19** `Reject()` reasons cut **new-exposure** dest loss if they sat in front of a sender. **3 / 19** (`MAX_LOSS_PER_TRADER`, `MAX_DAILY_EXECUTION_LOSS`, `MAX_PORTFOLIO_DRAWDOWN`) freeze closes and **increase** trapped loss. **0** emit `TRADER_RISK_BLOCKED`. Copy-all 8463 remains **−EV**. Dest capital **NONE** today (`SAFE_BY_ABSENCE`: no NOS, persist `AllowFixSend=false`). Policy `AllocationFactor=1` (BOOK_1 0.05 pin stale). |

---

## 2026-08-18 — W500_VERIFY_43 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_43 |
| Slot | 43 |
| Purpose | Adversarial verify from live files: DemoSeeder not API startup; Native groups via GroupRequestArray or GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. FAIL if unproven. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_43.md` |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the boolean `REAL_COPY_EXECUTION_ENABLED=true`) |
| Verdict | **FAIL.** Claims 1–4 PASS from files. Claim 5 FAIL: `.env` L73 `true`, DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`, logon host does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

## 2026-08-18 — P500_BOOK_76 FIX quote bid/ask are null; cannot size or guard spread

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_76 |
| Slot | 76 |
| Purpose | Measured evidence: FIX quote bid/ask are null. Cannot size or guard spread without a quote tape. Higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_76.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). |
| Verdict | **NO_TAPE_NO_SIZE_NO_SPREAD_GUARD.** Live DTO bid/ask/age **null**. Hosted FIX is one-shot `35=A` then dispose — no `35=x`/`35=V`. `CTraderQuoteService` **0 callers**, not in DI. Only `DestinationQuotes.Add` is `DemoSeeder` forged 2399.45/2399.85 (`VenueInstrumentId=null`); live host uses `BrokerCatalogSeed` (no quote row). `QuantityNormalizer` has no quote params; HEAD `AllocationFactor=1m` (BOOK_16 0.05 **STALE**). `SPREAD_TOO_WIDE` / `QUOTE_STALE` / `PRICE_MOVED_TOO_FAR` cannot fire without a print; `MaxSlippage` unread. `shadowPnl=0` is absence. Copy-all 8463 copies `RISK_BLOCKED` −$241,580. Risk to capital **NONE** (`SAFE_BY_ABSENCE`); **HIGH** if 1:1 send armed against a null book. |

---

## 2026-08-18 — P500_BOOK_77 allocation must stay 0.01–0.05 until dest shadow EV after costs is positive

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_77 |
| Slot | 77 |
| Purpose | Measured evidence: allocation factor must be tiny (0.01–0.05 of source) until shadow expectancy after costs is positive. Higher profit / lower loss. Honesty: wanting profit is not an edge; copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_77.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). |
| Verdict | **ALLOCATION_MUST_STAY_TINY.** HEAD `AllocationFactor=1m` (1:1) is dest-ruin if sent. Dest 0.05 ticket cap **MISSING**. Shadow EV after costs **not proven** (dest PnL $0; live shadow $0). Copy-all 8463 copies `RISK_BLOCKED` −$241k. Tiny α shrinks the hole; it does not mint an edge. Slot 17 `0.50×0.01=0.01` cell **wrong** (SUT **0**). W500 `×0.05` hop **STALE**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_4 adversarial live-path (slot 4)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_4 |
| Slot | 4 |
| Purpose | Independently confirm: DemoSeeder not API startup; Native all groups via GroupRequestArray/GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY stays false. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_4.md` |
| Product source modified | **No** |
| Secret values printed | **None** (booleans only) |
| Live attach | **No** |
| Verdict | **FAIL.** Claims 1–4 proven from files (2–3 capability only). Claim 5 disproven: `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` + `DependencyInjection.cs` L41 bind + no hosted re-pin. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_82 CTraderFixSession outbound is only 35=A

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_82 |
| Slot | 82 |
| Purpose | Read `CTraderFixSession.cs`. Prove outbound MsgType is only `A`. No `35=D`. Measured evidence for higher profit and lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_82.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** (lab `.env` L73 already `true`; not edited) |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| This-slot `:5000` GET | Blocked (SSRF). Pins from synthesis + Manager 18/8460. |
| Verdict | **PASS_35A_ONLY; COPY_ALL_8463_NEGATIVE_EV.** Assigned 135/135: only outbound MsgType is `A`; `WriteAsync=1`; `35=D=0`; sockets disposed. Copy hop `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`; 0 `ExecutionIntent` writers. Copy-all 8463 would copy `RISK_BLOCKED` −$241,580. Dest PnL $0. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_40 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_40 |
| Slot | 40 |
| Purpose | Adversarial verify from live files: DemoSeeder not API startup; Native groups via GroupRequestArray or GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_40.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Live attach this pass | **No** |
| Verdict | **FAIL.** Claims 1–4 PASS from files. Claim 5 FAIL: DI L41 binds env; `.env` L73 is `true`; hosted logon does not re-pin; `/api/settings` echoes runtime. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

---

## 2026-08-18 — W500_VERIFY_39 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_39 |
| Slot | 39 |
| Purpose | Adversarial verify from live files: DemoSeeder not API startup; Native lists all groups via GroupRequestArray or GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_39.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| Verdict | **FAIL.** Claims 1–4 PASS from files. Claim 5 FAIL: DI L41 binds `.env` L73 `true` onto `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

## 2026-08-18 — P500_BOOK_74 ML is not built (deterministic baseline only)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_74 |
| Slot | 74 |
| Purpose | Measured evidence for higher profit / lower loss. Topic: ML is not built. Do not invent a model. Deterministic baseline only. Honesty: wanting profit ≠ edge. Copy-all 8463 imports RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_74.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| REAL_COPY flipped | **No** |
| Live API | `:5000` attempted, SSRF blocked. Pins: synthesis 8463 / Manager 8460. |
| Verdict | **ML_NOT_BUILT; DETERMINISTIC_BASELINE_ONLY.** `services/` empty; `src` 0 XGBoost/`IScoringService`; `mlProbability` literal null; dest PnL constructor 0. Scorer + `XauUsdOneToOneCopyPolicy` are named-constant rules (block `RISK_BLOCKED`, demo, N&lt;20). Copy-all 8463 would import `RISK_BLOCKED` **−$241,580** (29). Scored XAU **−$154,425**. Dest **$0** (`SAFE_BY_ABSENCE`). Risk to capital **NONE** today; **DEST_RUIN_IF_SENT** if copy-all / invented-ML spray. |

---

## 2026-08-18 — W500_VERIFY_35 adversarial live-path (slot 35)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_35 |
| Slot | 35 |
| Purpose | Adversarial verify from live files: DemoSeeder not API startup; Native ALL groups via GroupRequestArray/GroupTotal; ALL traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_35.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from files. Claim 5 FAIL: DI L41 binds env; API `EnvFile.FindAndLoad()`; `.env` L73 `true`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_69 Starwave scored 0 after 91966 deals; do not size from Achiever-only scores

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_69 |
| Slot | 69 |
| Purpose | Starwave scored 0 while dealsInserted 91966. Book is incomplete. Do not size from Achiever-only scores. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_69.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| This-slot `:5000` GET | Blocked (SSRF). Pins from `P500_PROFIT_SYNTHESIS.md` + Manager 18/8460 + HEAD remasure. |
| Verdict | **BOOK_INCOMPLETE_DO_NOT_SIZE; COPY_ALL_8463_NEGATIVE_EV.** Starwave `deals-done` / `Scored=0` after **91,966** inserts is pipeline order (Achiever scores first), not an empty tape. Achiever-only SHADOW **+$78,276** is 100% demo and not a dest size. HEAD `AllocationFactor=1m` (BOOK_29 0.05 pin stale). Copy-all 8463 would copy `RISK_BLOCKED` 29 / **−$241,580** inside scored XAU **−$154,425**, plus ~8285 `INSUFFICIENT_DATA` (entire Starwave catalog). Dest PnL literal **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

## 2026-08-18 — P500_BOOK_71 RISK_BLOCKED source PnL is hundreds of thousands negative

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_71 |
| Slot | 71 |
| Purpose | Measured evidence for higher profit and lower loss. Topic: RISK_BLOCKED source PnL is hundreds of thousands negative. Copying them is how the venue blows up. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_71.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave P500 pin (8463 / −$154,425 / `RISK_BLOCKED` 29 / −$241,580 / dest **$0**). Manager census 18/8460. |
| Verdict | **NEVER_COPY_RISK_BLOCKED; COPY_ALL_8463_DEST_RUIN; SAFE_BY_ABSENCE.** Tail −$241,580 (29, all martingale) &gt; SHADOW+WATCH +$86,454. Copy-all EV = scored XAU −$154,425. HEAD exclude `{SHADOW,LIVE_CANDIDATE,LIVE}` + `TRADER_BLOCKED_RISK_BLOCKED`. RiskEngine 0 `TRADER_RISK_BLOCKED`. Dest PnL constructor 0. `35=A` only. Risk to capital **NONE** today; **HIGH / ruin** if blocked tail or catalog 8463 is sent 1:1. |

---

## 2026-08-18 — P500_BOOK_67 XAUUSD copy cost: spread + slippage + 15s MaxSourceSignalAge. Scalps die.

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_67 |
| Slot | 67 |
| Purpose | Measured evidence for higher profit / lower loss. Topic: XAUUSD copy cost = dest spread + slippage + 15s `MaxSourceSignalAge` reject. Scalps die. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_67.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Live API this slot | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-day pin (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **−$241,580** / dest $0). |
| Verdict | **SCALPS_DIE_AFTER_COSTS; COPY_ALL_8463_NEGATIVE_EV.** Dest taker spread (seed 0.40 / allowed 2.0 = $40–$200 per 1.00 lot) + unread `MaxSlippage=1.5` + 15s `SIGNAL_STALE` on OPEN. Hosted poll **20 s > 15 s** clock (≥25% first-sight miss). 322947 ~163s / +$4,950 is source demo, not dest EV. Copy-all 8463 imports `RISK_BLOCKED` **−$241,580** (29). Dest **$0** (`SAFE_BY_ABSENCE`). Risk to capital **NONE** today; **HIGH** if scalps / copy-all sent 1:1. |

---

## 2026-08-18 — P500_BOOK_73 MFE/MAE FeatureQuality Unavailable; no MAE stops

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_73 |
| Slot | 73 |
| Purpose | MFE/MAE `FeatureQuality` is Unavailable. Exact excursion not used. Do not claim MAE-based stops. Measured evidence for higher profit and lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_73.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| This-slot `:5000` GET | Blocked (SSRF). Pins from `P500_PROFIT_SYNTHESIS.md`. |
| Verdict | **FEATURE_QUALITY_UNAVAILABLE; EXACT_EXCURSION_UNUSED; NO_MAE_STOPS.** Scorer writes `MaeMfeQuality=Unavailable` and leaves averages null. `Score()` never reads them. Copy SL = `FinalSl ?? InitialSl` (fill clone). `RiskEngine` 0 MAE reads. D57 VWAP mutation score-identical. Product docs silent on MFE (C60 docs cite stale). Copy-all 8463 copies `RISK_BLOCKED` −$241,580. Dest PnL $0. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_13 adversarial live-path re-read

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_13 |
| Slot | 13 |
| Purpose | Independently confirm five live-path claims from files. FAIL any claim not proven from the file. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_13.md` |
| Product source modified | **No** |
| Live attach | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true`) |
| Verdict | **FAIL.** Claims 1–4 PASS. Claim 5 disproven: `.env` L73 `true` + DI L41 bind + no hosted re-pin. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_64 architecture §3 dest-net vs first-3 / copy-all

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_64 |
| Slot | 64 |
| Purpose | Read architecture §3 business goal. Future destination-net PnL is the target, not first-3 dollars. Measured evidence for higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_64.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| Localhost API | Not re-probed (SSRF block on 127.0.0.1). Used on-disk probe 18/8460 + P500 remasure 8463 / RISK_BLOCKED 29 / −$241,580 / dest PnL literal 0. |
| Verdict | **PASS as §3 reading. FAIL as a live profit claim.** First-3 / `EarlyQualityScore` is source quality, not dest-net. Copy-all 8463/8460 would spray 29 martingale `RISK_BLOCKED` names (−$241k source) plus a 100% Achiever demo/contest book onto one Pepperstone login. Higher dest profit / lower dest loss = keep `35=D` off; keep `XauUsdOneToOneCopyPolicy` (n≥20, no RISK_BLOCKED, no demo, no lookahead); do not rank by first-3 dollars. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_68 kill-switch $2000 / $500 are loss caps, not an edge

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_68 |
| Slot | 68 |
| Purpose | Measured evidence: `MaxDailyExecutionLoss=2000` and `MaxLossPerTrader=500` are loss caps, not an edge. Wanting profit does not create expectancy. Copy-all 8463 would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_68.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). |
| Verdict | **LOSS_CAPS_NOT_EDGE.** Caps fire after dest (or a mis-fed source ticket) is already ≤ −$500 / −$2000; they do not read `RISK_BLOCKED`; copy hop zeros `DailyExecutionPnl` so the daily line is dead; close is frozen after the cap (A71 G21–G22 FAIL). HEAD `AllocationFactor=1` makes $2,000 = one legal 5-lot $4/oz print. Copy-all 8463 EV is the scored XAU book −$154,425 (blocked tail −$241,580). Dest risk today **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_70 SHADOW group is 100% demo; no real Starwave or contest in the copy set

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_70 |
| Slot | 70 |
| Purpose | Measured evidence for higher profit / lower loss. Topic: SHADOW group is 100 percent demo. No real Starwave or contest live book in the copy set. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_70.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Local API this slot | **Not re-probed** (SSRF to 127.0.0.1 blocked). Book pin = `P500_PROFIT_SYNTHESIS.md` + `P500_S007` (8463 / SHADOW **70** 100% demo / `RISK_BLOCKED` **29 / −$241,580** / scored XAU **−$154,425** / dest PnL **$0**). Manager census independently re-summed 8/6512 + 10/1948 = 18/8460 (08:42Z JSON). |
| Verdict | **CONFIRMED_SHADOW_100PCT_DEMO; NO_REAL_STARWAVE_OR_CONTEST_IN_COPY_SET; COPY_ALL_8463_NEGATIVE_EV.** Named SHADOW 302252/303174/303274/303310/322947 are `demo\yo-2step` or `demo\yo-payp`. Achiever 190 contest + 0 real. Starwave real **28** unscored. Policy rejects `demo\`/`contest\` so layer-C copy set is empty vs the 70 SHADOW rows. Copy-all 8463 would import `RISK_BLOCKED` −$241k. Dest **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

## 2026-08-18 — P500_BOOK_63 TradeReconstructor / 303274 same-second 0.05 grid

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_63 |
| Slot | 63 |
| Purpose | Read `TradeReconstructor` and 303274-style overlapping 0.05-lot same-second entries. Is grid flagged? Evidence for higher profit / lower loss. Do not modify product. Never enable REAL_COPY. Never send 35=D. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_63.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| SUT | `TradeReconstructor.cs` 347 lines; `GroupBy(PositionId)` + `ScaleIn` worse-than-VWAP latch only; `src` grep grid/WasGrid/same-second = **0** |
| Catalog | login **303274** `demo\yo-2step` 16228.24 (`LIVE_GROUPS_AND_TRADERS.json` L2564–2568) |
| API | localhost `/api/overview` + `/api/traders` blocked (SSRF); census from synthesis 8463 / Manager 8460 / RISK_BLOCKED 29 / −$241,580 |
| HEAD correction vs BOOK_3/23 | Policy **does** reject this login via `DEMO_OR_CONTEST_GROUP`. Grid is still **not** flagged. Live-group clone of the same 0.05 spray would pass (`AllocationFactor=1`). |
| Verdict | **GRID_NOT_FLAGGED.** Distinct hedge `PositionId`s never set `WasAveragedDown` / martingale. SHADOW 93.50 legal. Copy-all 8463 copies RISK_BLOCKED losses. Wanting +$1,228 is not dest edge. Risk to capital **NONE** now (no send). |

---

## 2026-08-18 — P500_BOOK_57 allocation must stay 0.01–0.05 until dest shadow EV after costs is positive

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_57 |
| Slot | 57 |
| Purpose | Measured evidence: allocation factor must be tiny (0.01–0.05 of source) until shadow expectancy after costs is positive. Higher profit / lower loss. Honesty: wanting profit is not an edge; copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_57.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). |
| Verdict | **ALLOCATION_MUST_STAY_TINY.** HEAD `AllocationFactor=1m` (1:1) is dest-ruin if sent. Hosted hop cannot emit after-cost shadow fills (`VenueReconciled=false` → `VENUE_NOT_RECONCILED`). Copy-all 8463 copies `RISK_BLOCKED` −$241k. Tiny α shrinks the hole; it does not mint an edge. Slot 17 `0.50×0.01=0.01` cell **wrong** (SUT **0**). W500 `×0.05` hop **STALE**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_70 SHADOW group is 100% demo; no real Starwave or contest in the copy set

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_70 |
| Slot | 70 |
| Purpose | Measured evidence for higher profit / lower loss. Topic: SHADOW group is 100 percent demo. No real Starwave or contest live book in the copy set. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_70.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Local API this slot | **Not re-probed** (SSRF to 127.0.0.1 blocked). Book pin = `P500_PROFIT_SYNTHESIS.md` + `P500_S007` (8463 / SHADOW **70** 100% demo / `RISK_BLOCKED` **29 / −$241,580** / scored XAU **−$154,425** / dest PnL **$0**). Manager census independently re-summed 8/6512 + 10/1948 = 18/8460 (08:42Z JSON). |
| Verdict | **CONFIRMED_SHADOW_100PCT_DEMO; NO_REAL_STARWAVE_OR_CONTEST_IN_COPY_SET; COPY_ALL_8463_NEGATIVE_EV.** Named SHADOW 302252/303174/303274/303310/322947 are `demo\yo-2step` or `demo\yo-payp`. Achiever 190 contest + 0 real. Starwave real **28** unscored. Policy rejects `demo\`/`contest\` so layer-C copy set is empty vs the 70 SHADOW rows. Copy-all 8463 would import `RISK_BLOCKED` −$241k. Dest **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

## 2026-08-18 — W500_VERIFY_29 adversarial live-path verify

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_29 |
| Slot | 29 |
| Purpose | Adversarial verify from live files: (1) DemoSeeder is not API startup; (2) Native lists groups via GroupRequestArray or GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_29.md` |
| Product source modified | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Live attach this pass | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Verdict | **FAIL.** Claims 1/2/3/4 file-proven (2–3 capability only). Claim 5 **disproven**: `.env` L73 `true`, `EnvFile` + DI bind `RealCopyEnabled`, hosted logon no re-pin. Copy hop still `35=A` only + `NewOrderSingleImplemented=false` + persist `AllowFixSend=false`. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_66 prop-challenge demo is adverse selection

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_66 |
| Slot | 66 |
| Purpose | Measured evidence: copying prop-challenge demo accounts is adverse selection. Most accounts exist to pass a profit target then blow. Higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_66.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Local API this slot | **Not re-probed** (loopback GET blocked / SSRF). Book integers from `P500_PROFIT_SYNTHESIS.md` + Manager census 18/8460 (`LIVE_GROUPS_AND_TRADERS.json` 08:42Z). |
| Verdict | **CONFIRMED_ADVERSE_SELECTION; COPY_ALL_8463_NEGATIVE_EV.** Achiever 6295/6512 = `demo\yo-2step` (0 `real\`). SHADOW 70 is 100% demo. `RISK_BLOCKED` 29 / −$241,580 (all martingale). Scored XAU −$154,425. Policy rejects `demo\`/`contest\` (`DEMO_OR_CONTEST_GROUP`). Copy-all 8463 imports the blocked tail. Dest PnL **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_11 adversarial live-path (slot 11)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_11 |
| Slot | 11 |
| Purpose | Adversarial re-read of live files. Confirm: DemoSeeder is not API startup; Native can list all groups via GroupRequestArray or GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_11.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** (lab `.env` L73 already `true`; not edited) |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Live attach this pass | **No** (census 18/8460 prior, not re-probed) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files. Claim 5 FAIL: DI binds env; hosted logon does not re-pin; `/api/settings` echoes runtime. Copy hop `CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_6 Adversarial live-path verify

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_6 |
| Slot | 6 |
| Purpose | Adversarial verify: DemoSeeder not API startup; Native groups via GroupRequestArray/GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession no 35=D; REAL_COPY_EXECUTION stays false. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_6.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files. Claim 5 FAIL: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; logon host does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_65 Official cTrader FIX QUOTE 5211 / TRADE 5212 / TargetCompID cServer

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_65 |
| Slot | 65 |
| Purpose | Official cTrader FIX identity (QUOTE 5211, TRADE 5212, TargetCompID cServer). Prove Logon is not a fill. Measured higher-profit / lower-loss evidence. Honesty: wanting profit ≠ edge; copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_65.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Local API | Not re-probed (SSRF to 127.0.0.1 blocked). Used same-day CREDENTIALS 18/8460 + P500 synthesis 8463 / RISK_BLOCKED 29 / −$241,580. |
| Verdict | **CONFIRMED_OFFICIAL_PORTS_AND_COMPID. LOGON_IS_NOT_A_FILL. NO_DEST_EDGE.** Official SSL 5211/5212 + issued `cServer`. Product copy hop is one-shot `35=A` then dispose. Dest PnL constructor 0. Copy-all would import RISK_BLOCKED tail. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_15 adversarial live-path (slot 15)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_15 |
| Slot | 15 |
| Purpose | Adversarial verify from live files: DemoSeeder not API startup; Native groups via GroupRequestArray/GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_15.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** (lab `.env` L73 already `true`; this slot did not change it) |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Live attach this slot | **No** |
| Verdict | **FAIL.** Claims 1–4 PASS from files. Claim 5 FAIL: DI L41 binds env; `.env` L73 is `true`; hosted logon does not re-pin; `/api/settings` echoes runtime. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_60 quality 95.50 vs negative netSourcePnl

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_60 |
| Slot | 60 |
| Purpose | Read `BaselineScorer.cs`. Recalculate how quality 95.50 can coexist with negative `netSourcePnl`. Quote the formula. Measured evidence for higher profit / lower loss. Honesty: wanting profit is not an edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_60.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| This-slot `:5000` GET | Blocked (SSRF). Pins from synthesis + Manager 18/8460. 302252/303174 dollars re-checked against `LIVE_GROUPS_AND_TRADERS.json`. |
| Verdict | **CONFIRMED_SPLIT_NOT_EDGE.** 95.50 = `50+15+10+5+18−2.5` at `(b,r)=(90,10)` only; requires XAU `NetPnl>0` and `PF>=1.8`. Dashboard `netSourcePnl` is all-symbol Σ. 302252 (−68.46) / 303174 (−29.38) match catalog `1000−balance`. Copy-all 8463 would copy `RISK_BLOCKED` −$241,580. Dest PnL $0. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_61 RiskEngine reject reasons vs dest loss

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_61 |
| Slot | 61 |
| Purpose | Read `RiskEngine.cs`. List every reject reason that reduces dest loss if live send existed. Honesty: wanting profit is not an edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_61.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| This-slot `:5000` GET | Blocked (SSRF). Pins from synthesis 8463 / RISK_BLOCKED 29 / −$241,580 / dest $0 + Manager 18/8460. |
| Verdict | **16 / 19** `Reject()` reasons cut **new-exposure** dest loss if they sat in front of a sender. **3 / 19** (`MAX_LOSS_PER_TRADER`, `MAX_DAILY_EXECUTION_LOSS`, `MAX_PORTFOLIO_DRAWDOWN`) freeze closes and **increase** trapped loss. **0** emit `TRADER_RISK_BLOCKED`. HEAD `AllocationFactor=1m` (BOOK_1 0.05 stale). Product Evaluate on opens only; persist `AllowFixSend=false`; closes + `PersistDemoShadowAsync` skip Evaluate. Copy-all 8463 remains **−EV**. Dest capital **NONE** today (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_9 adversarial live-path verify

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_9 |
| Slot | 9 |
| Purpose | Independently confirm: DemoSeeder not API startup; Native lists all groups via GroupRequestArray or GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. FAIL any unproven claim. Never print secrets. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_9.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Live attach | **No** |
| Verdict | **FAIL.** Claims 1–4 proven from live files. Claim 5 **disproven**: lab `.env` L73 is `true` and `DependencyInjection.cs` L41 binds it; `CTraderFixLogonHostedService` no longer re-pins false. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

---

## 2026-08-18 — W500_VERIFY_14 Adversarial live-path verify

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_14 |
| Slot | 14 |
| Purpose | Independent re-read. Confirm: DemoSeeder not API startup; Native `GroupRequestArray`/`GroupTotal`; all traders `UserRequestArray`/`UserLogins`; `CTraderFixSession` no `35=D`; `REAL_COPY_EXECUTION` stays false. FAIL any unproved claim. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_14.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Live attach this pass | **No** |
| Verdict | **FAIL.** Claims 1–4 PASS from files. Claim 5 FAIL: `.env` L73 `true` + DI L41 bind; logon re-pin gone. Copy hop still `35=A` only / `NewOrderSingleImplemented=false` / persist `AllowFixSend=false`. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_2 adversarial live-path verify (slot 2)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_2 |
| Slot | 2 |
| Purpose | Independent confirm: DemoSeeder not API startup; Native all groups via GroupRequestArray/GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession no 35=D; REAL_COPY_EXECUTION stays false. FAIL if unproven. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_2.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Live attach | **No** |
| Verdict | **FAIL.** Claims 1–4 PASS from files. Claim 5 FAIL: `.env` L73 `true`, DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`, logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` 35=A only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_62 CTraderFixSession outbound 35=A only; copy-all 8463 copies RISK_BLOCKED losses

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_62 |
| Slot | 62 |
| Purpose | Read `CTraderFixSession.cs`. Prove outbound MsgType is only `A`. No `35=D`. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge; copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_62.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| This-slot `:5000` GET | Blocked (SSRF). Pins from `P500_PROFIT_SYNTHESIS.md` + Manager 18/8460. |
| Verdict | **PASS_35A_ONLY; COPY_ALL_8463_NEGATIVE_EV.** Assigned 135/135: outbound MsgType `(35,"A")` only; `WriteAsync=1`; `35=D=0`; sockets disposed. Wanting profit is not an edge. Copy-all 8463 would copy `RISK_BLOCKED` losses (pin 29 / −$241,580 inside scored XAU −$154,425). Dest PnL $0. Env REAL_COPY may be true; sender missing. Risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_27 adversarial live-path (DemoSeeder / Native ALL / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_27 |
| Slot | 27 |
| Purpose | Adversarial verify from live files: DemoSeeder not API startup; Native ALL groups via GroupRequestArray/GroupTotal; ALL traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_27.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** (lab `.env` L73 already `true`; not edited) |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Live attach this pass | **No.** Census not re-probed. |
| Verdict | **FAIL.** Claims 1–4 PASS from files. Claim 5 FAIL: DI L41 binds env; API `EnvFile.FindAndLoad()`; `.env` L73 `true`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

---

## 2026-08-18 — W500_VERIFY_5 adversarial live-path (slot 5)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_5 |
| Slot | 5 |
| Purpose | Adversarial re-read. Confirm DemoSeeder off API startup; native ALL groups (`GroupRequestArray`/`GroupTotal`) and ALL traders (`UserRequestArray`/`UserLogins`); `CTraderFixSession` no `35=D`; `REAL_COPY_EXECUTION` stays false. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_5.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** (lab `.env` already `true`; not changed) |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Live attach this pass | **No** |
| Verdict | **FAIL.** Claims 1–4 PASS from file. Claim 5 FAIL: `.env` L73 `true` and DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_30 adversarial live-path verify

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_30 |
| Slot | 30 |
| Purpose | Independent read of live path. Confirm: (1) DemoSeeder not API startup; (2) Native groups via GroupRequestArray/GroupTotal; (3) all traders via UserRequestArray/UserLogins; (4) CTraderFixSession has no 35=D; (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproved. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_30.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| Live attach | **No** (18/8460 not re-measured) |
| Verdict | **FAIL.** Claims 1/4 PASS. 2/3 PASS_SOURCE. Claim 5 **FAIL**: `.env` L73 is `true`; API `EnvFile.FindAndLoad`; DI binds `LiveRuntimeStatus.RealCopyEnabled`; no re-pin. Capital **NONE** (`SAFE_BY_ABSENCE`: session is 35=A only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). |

---

## 2026-08-18 — P500_BOOK_58 never flatten MT5 source (dest-only flatten)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_58 |
| Slot | 58 |
| Purpose | Measured evidence for higher profit / lower loss. Never flatten MT5 source. Destination-only flatten. Honesty: wanting profit ≠ edge. Copy-all 8463 imports RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_58.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| REAL_COPY flipped | **No** |
| Live `35=D` sent | **No** |
| Live API | `:5000` not re-probed (`web_fetch` SSRF on `127.0.0.1`). Pins: synthesis 8463 / Manager 8460. |
| Verdict | **DEST_ONLY_FLATTEN_LAW.** Source C# path GET-only (0 `DealerSend`; `PositionRequest`/`PositionGetByGroup` only). Hosted FIX `35=A` only. Product `EmergencyFlatten` blocks opens only — no dest run. Demo dest-721 flatten refuses `1369850`. Scored XAU **−$154,425**; `RISK_BLOCKED` **−$241,580** (29). Dest PnL **$0** (`SAFE_BY_ABSENCE`). HEAD `AllocationFactor=1m` **UNSAFE if sent**. Copy-all 8463 would import that tail. Risk to capital **NONE** today; **DEST_RUIN_IF_SENT** if copy-all / blocked tail / 1:1 or 5-lot dest flatten. |

---

## 2026-08-18 — W500_VERIFY_8 adversarial live-path (slot 8)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_8 |
| Slot | 8 |
| Purpose | Independently confirm: DemoSeeder not API startup; native ALL groups (`GroupRequestArray`/`GroupTotal`); ALL traders (`UserRequestArray`/`UserLogins`); `CTraderFixSession` has no `35=D`; `REAL_COPY_EXECUTION` stays false. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_8.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files. Claim 5 **FAIL**: `.env` L73 `true`; `EnvFile`+DI L41 bind; settings L76 exposes `runtime.RealCopyEnabled`; logon does not re-pin. Copy hop still `35=A` only; NOS `const false`; persist `AllowFixSend=false`. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_VERIFY_16 adversarial live-path (DemoSeeder / Native walk / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_16 |
| Slot | 16 |
| Purpose | Adversarial verify: (1) DemoSeeder not API startup, (2) Native groups via GroupRequestArray or GroupTotal, (3) all traders via UserRequestArray/UserLogins, (4) CTraderFixSession has no 35=D, (5) REAL_COPY_EXECUTION stays false. FAIL if any claim unproven. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_16.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** (lab `.env` L73 already `true`; DI binds it; this slot did not change it) |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Live attach this pass | **No** |
| Verdict | **FAIL.** 1–4 PASS from files. 5 FAIL: flag does not stay false (`DependencyInjection.cs` L41 binds env). Copy hop still `SAFE_BY_ABSENCE`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_VERIFY_22 Adversarial live-path verify

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_22 |
| Slot | 22 |
| Purpose | Adversarial verify: DemoSeeder not API startup; Native groups via GroupRequestArray/GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession no 35=D; REAL_COPY_EXECUTION stays false. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_22.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL.** Claims 1–4 PASS from live files. Claim 5 FAIL: `.env` L73 `true` + DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`; logon host does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_56 FIX quote bid/ask are null; cannot size or guard spread

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_56 |
| Slot | 56 |
| Purpose | Measured evidence: FIX quote bid/ask are null. Cannot size or guard spread without a quote tape. Higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_56.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave `P500_PROFIT_SYNTHESIS.md` pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0 / bid/ask **null**). |
| Verdict | **NO_TAPE_NO_SIZE_NO_SPREAD_GUARD.** Live DTO bid/ask/age **null**. Hosted FIX is one-shot `35=A` then dispose — no `35=x`/`35=V`. `CTraderQuoteService` **0 callers**, not in DI. Only `DestinationQuotes.Add` is `DemoSeeder` forged 2399.45/2399.85 (`VenueInstrumentId=null`); live host uses `BrokerCatalogSeed` (no quote row). `QuantityNormalizer` has no quote params; HEAD `AllocationFactor=1m` (BOOK_16 0.05 **STALE**). `SPREAD_TOO_WIDE` / `QUOTE_STALE` / `PRICE_MOVED_TOO_FAR` cannot fire without a print; `MaxSlippage` unread. `shadowPnl=0` is absence. Copy-all 8463 copies `RISK_BLOCKED` −$241,580. Risk to capital **NONE** (`SAFE_BY_ABSENCE`); **HIGH** if 1:1 send armed against a null book. |

---

## 2026-08-18 — P500_BOOK_55 In-memory DB: scores vanish on restart

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_55 |
| Slot | 55 |
| Purpose | In-memory DB: scores vanish on restart. Cannot run a live book on that. Measured evidence for higher profit and lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_55.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| This-slot `:5000` GET | Blocked (SSRF). Pins from synthesis + Manager 18/8460. |
| Verdict | **BLOCK_LIVE_BOOK_ON_INMEMORY.** DI fail-opens to `UseInMemoryDatabase("trader-intelligence-live")` when `DATABASE_URL` contains `<SECRET>`. Scores / RISK_BLOCKED / intents die on restart (synthesis ~09:01Z wipe). Copy-all 8463 EV = scored XAU **−$154k**; blocked tail **−$241k** (29 martingale). Dest PnL **$0**. `35=D` absent. Risk to capital **NONE** today (`SAFE_BY_ABSENCE`); copy-all if send existed = **HIGH expected dest loss**. |

---

## 2026-08-18 — P500_BOOK_40 quality 95.50 vs negative netSourcePnl

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_40 |
| Slot | 40 |
| Purpose | Read `BaselineScorer.cs`. Recalculate how quality 95.50 can coexist with negative `netSourcePnl`. Quote the formula. Measured evidence for higher dest profit / lower dest loss. Honesty: wanting profit ≠ edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_40.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Local API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book integers from same-day `P500_PROFIT_SYNTHESIS.md`; 302252/303174 dollars re-checked against `LIVE_GROUPS_AND_TRADERS.json` balances 931.54 / 970.62. |
| Verdict | **95.50_IS_XAU_SHAPE_NOT_PROFIT.** Formula `50 + 15 I_net + 10 I_12 + 5 I_18 + 0.20 b − 0.25 r`. Unique lattice `(b,r)=(90,10)` with `I_net=I_12=I_18=1`. **Cannot** sit on negative XAU `features.NetPnl` (`quality_max(I_net=0)=70`). **Can** sit on negative dashboard `netSourcePnl` because `GetTradersAsync` sums **all completed symbols**. Existence: 302252 SHADOW 95.50 / −68.46; 303174 SHADOW 95.50 / −29.38. Copy-all 8463 copies 29 `RISK_BLOCKED` names (source tail **−$241,580**) inside scored XAU **−$154,425**. Dest PnL **$0** (`SAFE_BY_ABSENCE`). Risk to capital **NONE** this process. |

---

## 2026-08-18 — W500_VERIFY_20 adversarial live-path (DemoSeeder / Native request APIs / 35=D / REAL_COPY)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_20 |
| Slot | 20 |
| Purpose | Adversarial verify from live files: DemoSeeder not API startup; Native lists all groups via GroupRequestArray or GroupTotal; all traders via UserRequestArray/UserLogins; CTraderFixSession has no 35=D; REAL_COPY_EXECUTION stays false. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_20.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** (lab `.env` L73 already `true`; not changed this slot) |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| Live Manager this pass | **Not attached** |
| Verdict | **FAIL.** Claims 1–4 PASS from files. Claim 5 FAIL: DI L41 binds `.env` L73 `true` onto `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does not re-pin. Copy hop still `SAFE_BY_ABSENCE` (`CTraderFixSession` `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_51 RISK_BLOCKED source PnL is hundreds of thousands negative

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_51 |
| Slot | 51 |
| Purpose | Measured evidence: `RISK_BLOCKED` source PnL is hundreds of thousands negative. Copying them is how the venue blows up. Higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_51.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** (loopback GET blocked / SSRF). Book numbers from `P500_PROFIT_SYNTHESIS.md` §1 + `P500_S007` + CREDENTIALS 18/8460. |
| Verdict | **NEVER_COPY_RISK_BLOCKED; COPY_ALL_8463_BLOWS_THE_VENUE.** Live pin 29 / **−$241,580** (all martingale) dominates scored XAU **−$154,425** (SHADOW +$78,276 < tail). Copy-all 8463 copies that tail plus ~8284 `INSUFFICIENT_DATA`. Product allow-list excludes blocked; persist `AllowFixSend=false`; NOS unimplemented; outbound `35=A` only. Dest PnL **$0**. Risk to capital **NONE** today (`SAFE_BY_ABSENCE`); **HIGH / ruin** if the tail is sent. |

---

## 2026-08-18 — P500_BOOK_54 ML not built; deterministic baseline only

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_54 |
| Slot | 54 |
| Purpose | Measured evidence for higher dest profit / lower dest loss. Topic: ML is not built. Do not invent a model. Deterministic baseline only. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_54.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| `REAL_COPY` flipped | **No** |
| Live `35=D` sent | **No** |
| Live API this slot | **Not re-probed** (loopback GET blocked). Book integers from same-day `P500_PROFIT_SYNTHESIS.md` pin. |
| Verdict | **ML_NOT_BUILT_CORRECT.** `D:\Prop\services` empty; `mlProbability` literal null; ranker is `BaselineScorer` rules (`CanPromoteToLive => false`). Copy policy already excludes `RISK_BLOCKED` / n<20 / demo. Copy-all 8463 would include 29 `RISK_BLOCKED` names (source tail **−$241,580**) and a scored XAU book **−$154,425**. Dest PnL **$0** (`SAFE_BY_ABSENCE`). Inventing XGBoost/LLM does not create dest edge. Risk to capital **NONE** this process. |

---

## 2026-08-18 — P500_BOOK_53 MFE/MAE FeatureQuality Unavailable; exact excursion unused

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_53 |
| Slot | 53 |
| Purpose | MFE/MAE `FeatureQuality` is Unavailable. Exact excursion not used. Do not claim MAE-based stops. Measured path to higher profit / lower loss without inventing an edge. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_53.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). |
| Verdict | **FEATURE_QUALITY_UNAVAILABLE; EXACT_EXCURSION_UNUSED; NO_MAE_STOPS.** Scorer always stamps `MaeMfeQuality=Unavailable`; `AverageMfe`/`AverageMae` null; A22 MAE floors not wired (`FLAG_MAE`/`mfe_mae_used` = 0 hits in `src`); `MfeMaeCalculator` + `mt5_xau_ticks` MISSING. D57 VWAP mutation scores identical. Copy-all **8463** would copy `RISK_BLOCKED` **−$241,580**. Wanting profit is not an edge. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_49 Starwave scored 0 after 91,966 deals

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_49 |
| Slot | 49 |
| Purpose | Measured evidence for higher dest profit / lower dest loss. Topic: Starwave scored **0** while `dealsInserted=91966`. Book is incomplete. Do not size from Achiever-only scores. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_49.md` |
| Product edited | **No** |
| Live `35=D` | **Not sent** |
| `REAL_COPY` flipped | **No** |
| Live API this slot | Loopback GET blocked (SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` (8463 / Starwave **91966 / scored 0 / deals-done** / scored XAU **−$154,425** / `RISK_BLOCKED` **−$241,580** / dest PnL **$0**). |
| Verdict | **BOOK_INCOMPLETE.** `scored=0` is loop-3 queue (Achiever first), not an empty tape. Achiever-only SHADOW ranks are not a size book. HEAD `α=1`. Copy-all 8463 copies the blocked tail. Dest **$0** (`SAFE_BY_ABSENCE`). Risk to capital **NONE** this process. |

---

## 2026-08-18 — P500_BOOK_43 TradeReconstructor / 303274 same-second 0.05 grid

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_43 |
| Slot | 43 |
| Purpose | Read `TradeReconstructor` and 303274-style overlapping 0.05-lot same-second entries. Is grid flagged? Evidence for higher profit / lower loss. Do not modify product. Never enable REAL_COPY. Never send 35=D. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_43.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| SUT | `TradeReconstructor.cs` 347 lines; `GroupBy(PositionId)` L46 + `ScaleIn` worse-than-VWAP latch only |
| Catalog | login **303274** `demo\yo-2step` 16228.24 (`LIVE_GROUPS_AND_TRADERS.json` L2564–2568) |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = P500 pin + Manager JSON 6512+1948=8460 (8463 unreconciled). |
| Verdict | **GRID_NOT_FLAGGED.** Distinct hedge 0.05s never `ScaleIn`. No `WasGrid`. 303274-class averaging/martingale false; SHADOW reachable. 1:1 `DEMO_OR_CONTEST_GROUP` is not a grid detector. Copy-all 8463 would copy `RISK_BLOCKED` losses (−$241,580). Dest capital **NONE** today (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_48 kill-switch $2000 / $500 are loss caps, not an edge

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_48 |
| Slot | 48 |
| Purpose | Measured evidence: `MaxDailyExecutionLoss=2000` and `MaxLossPerTrader=500` are loss caps, not an edge. Wanting profit does not create expectancy. Copy-all 8463 would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_48.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** (loopback GET blocked / SSRF). Book numbers from `P500_PROFIT_SYNTHESIS.md` + CREDENTIALS 18/8460. |
| Verdict | **LOSS_CAPS_NOT_EDGE.** Caps fire after dest (or a mis-fed source ticket) is already ≤ −$500 / −$2000; they do not read `RISK_BLOCKED`; copy hop zeros `DailyExecutionPnl` so the daily line is dead; `VenueReconciled=false` short-circuits OPEN before L117; close hop skips `Evaluate` (A71 G21–G22 FAIL if later wired). Copy-all 8463 EV is the scored XAU book −$154,425 (blocked tail −$241,580). Dest risk today **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_52 Persist ClOrdID before send; unknown must not retry

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_52 |
| Slot | 52 |
| Purpose | Persist `ClOrdID` before send. Unknown state must not retry. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge; copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_52.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). |
| Verdict | **LOWER_LOSS_NOT_PROFIT; SYSTEM_ARM_MISSING.** Persist-before-send is a duplicate-size brake, not an edge. Helper `MayRetry` false on `SentAcknowledgementUnknown` / `ExecutionStateUnknown` (D98 eval). Unique index exists; **0** `ExecutionIntent` writers; factory is clock+seq (not A42 `From(id)`); product `35=H` **MISSING**. Copy-all 8463 copies `RISK_BLOCKED` 29 / **−$241,580**. Scored XAU **−$154,425**. Dest PnL **$0**. `AllocationFactor=1m` makes a retry 2× full lots. Risk to capital **NONE** today (`SAFE_BY_ABSENCE`); **HIGH** if a sender retries unknown. |

---

## 2026-08-18 — P500_BOOK_87 XAUUSD copy cost: spread + slippage + 15s MaxSourceSignalAge; scalps die

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_87 |
| Slot | 87 |
| Purpose | Measured evidence for higher dest profit / lower dest loss. Topic: XAUUSD copy cost = dest spread + slippage + 15s `MaxSourceSignalAge` reject. Scalps die. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_87.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Local API | `GET :5000/api/overview` + `/api/traders` blocked (SSRF). Book integers = same-wave pin (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **−$241,580** / dest **$0**). Census 18/8460. 322947 group/balance remasured from JSON. |
| Verdict | **SCALPS_DIE_IN_SPREAD_PLUS_SLIPPAGE_PLUS_15S_AGE. COPY_ALL_8463_NEGATIVE_EV.** Dest long pays ask/exits bid (seed 0.40 ⇒ $40/lot RT; allowed 2.0 ⇒ $200/lot). `MaxSlippage=1.5` unread. `SIGNAL_STALE` at 15s on OPEN/INCREASE; hop clocks `OpenedAt`. Hosted tick **8s then 20s** > 15s (25% geometry miss). Hold computed, unused, not on `TraderScore`. 322947 ~163s / `demo\yo-payp` 104949.8. Close hop skips `Evaluate`. Copy-all imports −$241k tail. Dest risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_47 XAUUSD copy cost: spread + slippage + 15s MaxSourceSignalAge; scalps die

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_47 |
| Slot | 47 |
| Purpose | Measured evidence for higher dest profit / lower dest loss. Topic: XAUUSD copy cost = dest spread + slippage + 15s `MaxSourceSignalAge` reject. Scalps die. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_47.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Local API | `GET :5000/api/overview` + `/api/traders` blocked (SSRF). Book integers = same-wave pin (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **−$241,580** / dest **$0**). Census 18/8460 unreconciled vs 8463. |
| Verdict | **SCALPS_DIE_IN_SPREAD_PLUS_SLIPPAGE_PLUS_15S_AGE. COPY_ALL_8463_NEGATIVE_EV.** Dest long pays ask/exits bid (seed 0.40 ⇒ $40/lot RT; allowed 2.0 ⇒ $200/lot). `MaxSlippage=1.5` unread. `SIGNAL_STALE` at 15s on OPEN/INCREASE; hop clocks `OpenedAt` so catch-up scalps reject. Hold computed, unused. 322947 ~163s. Copy-all imports −$241k tail. Dest risk **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_50 SHADOW is 100% demo; no real Starwave / contest live copy book

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_50 |
| Slot | 50 |
| Purpose | Measured evidence for higher profit / lower loss. Topic: SHADOW group is 100% demo. No real Starwave or contest live book in the copy set. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_50.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** (loopback GET SSRF-blocked). Census re-summed from `LIVE_GROUPS_AND_TRADERS.json` 08:42Z (18/8460). Book pin = `P500_PROFIT_SYNTHESIS.md` 8463 / SHADOW 70 / RISK_BLOCKED 29 / −$241,580. |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Verdict | **CONFIRMED_SHADOW_100PCT_DEMO.** Named SHADOW logins 302252/303174/303274/303310 = `demo\yo-2step`; 322947 = `demo\yo-payp`. Achiever 0 `real\`; contest 190 not in SHADOW set; Starwave real 28 / scored 0. Policy `IsTraderEligible` → `DEMO_OR_CONTEST_GROUP` vs current 70. Copy-all 8463 EV = scored XAU **−$154,425** (blocked tail **−$241,580**). Dest PnL **$0**. Residual: `Starwave\demo\` prefix hole (1905) if scoring finishes. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_59 official FIX copier listing is not a send license

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_59 |
| Slot | 59 |
| Purpose | Trade-copier on cTrader FIX is officially listed; Spotware says other APIs may fit copy better. Still no license to send today. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_59.md` |
| Product source modified | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| Live `35=D` sent | **No** |
| This-slot `:5000` GET | Blocked (SSRF). Pins from synthesis + Manager 18/8460. Official pages re-fetched. |
| Verdict | **NO_LICENSE; COPY_ALL_8463_NEGATIVE_EV.** Official https://help.ctrader.com/fix/ lists trade copiers then: “other Spotware APIs are more suitable.” RoE has TRADE `35=D`. Product hop is `35=A` only; `src/`+`apps/` `35=D=0`; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. §68 0/19; §70 0/14. Open API terms still require trader-explicit approval. Copy-all 8463 would copy `RISK_BLOCKED` −$241,580 inside scored XAU −$154,425. Achiever 100% demo/contest; Starwave real = 28. Dest PnL $0. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

## 2026-08-18 — P500_BOOK_46 prop-challenge demo is adverse selection

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_46 |
| Slot | 46 |
| Purpose | Measured evidence for higher dest profit / lower dest loss. Topic: copying prop-challenge demo accounts is adverse selection — most exist to pass a profit target then blow. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_46.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| `REAL_COPY` flipped | **No** |
| Live `35=D` | **No** |
| Local API | Not re-probed (SSRF to 127.0.0.1 blocked). Pins: `P500_PROFIT_SYNTHESIS.md` 8463 / SHADOW 70 +$78,276 / RISK_BLOCKED 29 −$241,580 / scored XAU −$154,425 / dest PnL $0; Manager 18/8460. |
| Verdict | **ADVERSE_SELECTION_CONFIRMED.** Achiever 6512/6512 demo+contest (6295 `demo\yo-2step`). Combined 8417/8460 (99.49%) challenge/demo/contest. SHADOW 100% demo pass-target look; blocked tail −$241k is the blow. Copy-all 8463 is −EV. Dest PnL **$0** (`SAFE_BY_ABSENCE`). Wanting profit ≠ edge. Risk to capital **NONE** this process. |

---

## 2026-08-18 — P500_BOOK_45 Official cTrader FIX QUOTE 5211 / TRADE 5212 / TargetCompID cServer

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_45 |
| Slot | 45 |
| Purpose | Official cTrader FIX identity (QUOTE 5211, TRADE 5212, TargetCompID cServer). Prove Logon is not a fill. Measured higher-profit / lower-loss evidence. Honesty: wanting profit ≠ edge; copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_45.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| Local API | Not re-probed (SSRF to 127.0.0.1 blocked). Used same-day CREDENTIALS 18/8460 + P500 synthesis 8463 / RISK_BLOCKED 29 / −$241,580. |
| Verdict | **CONFIRMED_OFFICIAL_PORTS_AND_COMPID. LOGON_IS_NOT_A_FILL. NO_DEST_EDGE.** Official SSL 5211/5212 + issued `cServer`. Product copy hop is one-shot `35=A` then dispose. Dest PnL constructor 0. Copy-all would import RISK_BLOCKED tail. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_35 in-memory DB: scores vanish on restart; cannot run a live book

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_35 |
| Slot | 35 |
| Purpose | Measured evidence for higher profit / lower loss. In-memory EF: scores vanish on restart. Cannot run a live book on RAM. Honesty: wanting profit ≠ edge. Copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_35.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = synthesis 8463 / XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**. Manager census 18/8460. Measured wipe pin: synthesis addendum ~09:01Z. |
| Verdict | **BLOCK_NO_LIVE_BOOK_ON_RAM.** DI fail-open `UseInMemoryDatabase("trader-intelligence-live")` when CS empty / `<SECRET>`. 0 `Migrations/`; `EnsureCreated` ×3; workers skip `EnvFile`; Compose Postgres unwired; health `healthy:true` constant. Hosted copy filter dies with scores. Copy-all 8463 imports −$241k tail. Dest capital **NONE** today (`SAFE_BY_ABSENCE`); **HIGH** if send armed on InMemory. |

---

## 2026-08-18 — P500_BOOK_37 allocation must stay 0.01–0.05 until dest shadow EV after costs is positive

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_37 |
| Slot | 37 |
| Purpose | Measured evidence: allocation factor must be tiny (0.01–0.05 of source) until shadow expectancy after costs is positive. Higher profit / lower loss. Honesty: wanting profit is not an edge; copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_37.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). |
| Verdict | **ALLOCATION_MUST_STAY_TINY.** HEAD `AllocationFactor=1m` (1:1) is dest-ruin if sent. Shadow EV after costs **not proven** (dest PnL $0; live shadow $0). Copy-all 8463 copies `RISK_BLOCKED` −$241k. Tiny α shrinks the hole; it does not mint an edge. Slot 17 `0.50×0.01=0.01` cell **wrong** (SUT **0**). W500 `×0.05` hop **STALE**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_44 architecture §3 dest-net vs first-3 / copy-all

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_44 |
| Slot | 44 |
| Purpose | Read architecture v2 §3 Primary Business Goal. Future destination-net PnL is the target, not first-3 dollars. Measured evidence for higher profit / lower loss. Honesty: wanting profit ≠ edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_44.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Local API this slot | **Not re-probed** (loopback GET blocked / SSRF). Book integers from same-day `P500_PROFIT_SYNTHESIS.md` + Manager census 18/8460. |
| Verdict | **PASS_§3_DEST_NET_NOT_FIRST3; COPY_ALL_8463_NEGATIVE_EV.** §3 anti-target is first-3 $. Coded filter is `MinCompletedXauTrades=20` + drop `RISK_BLOCKED`/`demo\`. Copy-all 8463 would import `RISK_BLOCKED` 29 / −$241,580. Scored XAU −$154,425. Dest PnL literal **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_41 RiskEngine reject reasons that cut dest loss if live send existed

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_41 |
| Slot | 41 |
| Purpose | Read `RiskEngine.cs`. List every reject reason that reduces dest loss **if live send existed**. Measured higher-profit / lower-loss evidence. Honesty: wanting profit ≠ edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_41.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** |
| Local API | Not re-probed (SSRF to 127.0.0.1 blocked). Book pin = `P500_PROFIT_SYNTHESIS.md` (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **−$241,580** / dest PnL **$0**). |
| Verdict | **16 / 19** `Reject()` reasons cut **new-exposure** dest loss if they sat in front of a sender. **3 / 19** (`MAX_LOSS_PER_TRADER`, `MAX_DAILY_EXECUTION_LOSS`, `MAX_PORTFOLIO_DRAWDOWN`) freeze closes and **increase** trapped loss. **0** emit `TRADER_RISK_BLOCKED`. Copy-all 8463 remains **−EV**. Dest capital **NONE** today (`SAFE_BY_ABSENCE`: no NOS, persist `AllowFixSend=false`). Policy `AllocationFactor=1` (BOOK_1 0.05 pin stale). |

---

## 2026-08-18 — P500_BOOK_34 ML is not built (deterministic baseline only)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_34 |
| Slot | 34 |
| Purpose | Measured evidence for higher profit / lower loss. Topic: ML is not built. Do not invent a model. Deterministic baseline only. Honesty: wanting profit ≠ edge. Copy-all 8463 imports RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_34.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| REAL_COPY flipped | **No** |
| Live API | `:5000` attempted, SSRF blocked. Pins: synthesis 8463 / Manager 8460. |
| Verdict | **ML_NOT_BUILT; DETERMINISTIC_BASELINE_ONLY.** `services/` empty; `src` 0 XGBoost/`IScoringService`; `mlProbability` literal null; dest PnL constructor 0. Scorer + `XauUsdOneToOneCopyPolicy` are named-constant rules (block `RISK_BLOCKED`, demo, N&lt;20). Copy-all 8463 would import `RISK_BLOCKED` **−$241,580** (29). Scored XAU **−$154,425**. Dest **$0** (`SAFE_BY_ABSENCE`). Risk to capital **NONE** today; **DEST_RUIN_IF_SENT** if copy-all / invented-ML spray. |

---

## 2026-08-18 — P500_BOOK_38 never flatten MT5 source (dest-only flatten)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_38 |
| Slot | 38 |
| Purpose | Measured evidence for higher profit / lower loss. Never flatten MT5 source. Destination-only flatten. Honesty: wanting profit ≠ edge. Copy-all 8463 imports RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_38.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Live API | `:5000` not re-probed (SSRF). Pins: synthesis 8463 / Manager 8460. |
| Verdict | **DEST_ONLY_FLATTEN_LAW.** Source C# path GET-only (0 `DealerSend`). Hosted FIX `35=A` only. Product `EmergencyFlatten` blocks opens only — no dest run. Demo dest-721 flatten refuses `1369850`. Scored XAU **−$154,425**; `RISK_BLOCKED` **−$241,580** (29). Dest PnL **$0** (`SAFE_BY_ABSENCE`). Copy-all 8463 would import that tail. Policy remasure: `AllocationFactor=1` (UNSAFE if sent). Risk to capital **NONE** today; **DEST_RUIN_IF_SENT** if copy-all / blocked tail / 1:1 5-lot dest flatten. |

---

## 2026-08-18 — P500_BOOK_33 MFE/MAE FeatureQuality Unavailable; no MAE stops

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_33 |
| Slot | 33 |
| Purpose | MFE/MAE `FeatureQuality` is Unavailable. Exact excursion not used. Do not claim MAE-based stops. Measured evidence for higher profit / lower loss. Honesty: wanting profit is not an edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_33.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| This-slot `:5000` GET | Blocked (SSRF). Pins from `P500_PROFIT_SYNTHESIS.md`. |
| Verdict | **FEATURE_QUALITY_UNAVAILABLE; EXACT_EXCURSION_UNUSED; NO_MAE_STOPS.** Scorer writes `MaeMfeQuality=Unavailable` and leaves averages null. `Score()` never reads them. Copy SL = `FinalSl ?? InitialSl` (fill clone). `RiskEngine` 0 MAE reads. D57 VWAP mutation score-identical. Copy-all 8463 copies `RISK_BLOCKED` −$241,580. Dest PnL $0. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_42 CTraderFixSession outbound is only 35=A

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_42 |
| Slot | 42 |
| Purpose | Read `CTraderFixSession.cs`. Prove outbound MsgType is only `A`. No `35=D`. Measured evidence for higher profit and lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_42.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| This-slot `:5000` GET | Blocked (SSRF). Pins from synthesis + Manager 18/8460. |
| Verdict | **PASS_35A_ONLY; COPY_ALL_8463_NEGATIVE_EV.** Assigned 135/135: only outbound MsgType is `A`; `WriteAsync=1`; `35=D=0`; sockets disposed. Copy hop `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`; 0 `ExecutionIntent` writers. Copy-all 8463 would copy `RISK_BLOCKED` −$241,580. Dest PnL $0. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_194 LiveMt5Registration.HasRealPasswords fail-closed

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_194 |
| Slot | 194 |
| Purpose | Check `LiveMt5Registration.HasRealPasswords` fail-closed. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_194.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** |
| Verdict | **PASS_FAIL_CLOSED_DI.** Dual-AND of `MT5_PASSWORD` + `MT5_STARWAVEFX_PASSWORD` via 3-clause `IsSecret`. DI throws `Real MT5 passwords are required...` before `CreateConnectors`; no Fake substitution. After pass: Native ×2, ingest `GroupRequestArray("*")` + `GetAccountsAsync(null)`. Census re-summed 8/6512 + 10/1948 = 18/8460 (1984 positions; 08:42Z JSON, not re-probed). Residuals: Ordinal case hole; factory/probe bypass; 0 tests; DI env-binds lab `.env` `REAL_COPY=true`. Copy hop `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. Demo `Build("D")` off-hop. YoPips C++ has no dual-AND. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500 slot 197 FEATURE_COPY / REAL_COPY defaults

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_197 |
| Slot | 197 |
| Purpose | Check `FEATURE_COPY_TRADING_ENABLED` and `REAL_COPY_EXECUTION_ENABLED` defaults. Fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_197.md` |
| Product source modified | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Live `35=D` sent | **No** |
| Verdict | **PASS_NO_LIVE_SEND_ENV_ARMED.** Architecture/POCO REAL_COPY default still **false**. Local `.env` L73+L106 both **true**. DI binds `REAL_COPY_EXECUTION_ENABLED` (`DependencyInjection.cs` L41). Logon re-pin **removed**. API FEATURE literal **true**. Fetch ALL flag-blind (census 18/8460 re-summed from 08:42Z JSON). `35=D` absent; `NewOrderSingleImplemented=false`; `AllowFixSend` persisted false. Shadow policy now 1:1 (`AllocationFactor=1m`) but paper-only. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). Residual: next sender would see runtime armed on the API host. |

---

## 2026-08-18 — P500_BOOK_15 In-memory DB: scores vanish; cannot run a live book

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_15 |
| Slot | 15 |
| Purpose | In-memory DB: scores vanish on restart. Cannot run a live book. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_15.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** (loopback GET blocked). Book numbers from `P500_PROFIT_SYNTHESIS.md` + CREDENTIALS 18/8460. |
| Verdict | **BLOCK_LIVE_BOOK_ON_INMEMORY.** DI fail-opens to `UseInMemoryDatabase("trader-intelligence-live")` when `DATABASE_URL` contains `<SECRET>`. Scores / RISK_BLOCKED / intents die on restart (synthesis ~09:01Z wipe). Copy-all 8463 EV = scored XAU **−$154k**; blocked tail **−$241k** (29 martingale). Dest PnL **$0**. `35=D` absent. Risk to capital **NONE** today (`SAFE_BY_ABSENCE`); copy-all if send existed = **HIGH expected dest loss**. |

---

## 2026-08-18 — P502 XAUUSD 1:1 selection + demo FIX matrix

| Item | Value |
|---|---|
| Artifact | `reports/swarm/20260818/P502_XAUUSD_ONE_TO_ONE.md` |
| Policy | `src/Domain/Copy/XauUsdOneToOneCopyPolicy.cs` |
| Unit | 12/12 PASS |
| Demo | sell fill 4392.86 / flatten 4392.81; limit+stop rest; 1000/1002 illegal on 35=D; F forbids 54 and 55 |
| Verdict | Select traders not closed winners. 1:1 lots/side. Close on source close event. SL/TP cannot ride on NOS. |

---

## 2026-08-18 — W500_RESEARCH_198 QuantityNormalizer lots ↛ FIX OrderQty

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_198 |
| Slot | 198 |
| Purpose | Check `QuantityNormalizer` never blindly converts MT5 lots to FIX `OrderQty`. Fetch ALL Achiever+Starwave groups/traders. Copy-to-cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_198.md` |
| Product source modified | **No** |
| Secret values printed | **None** (flag booleans only) |
| Verdict | **EXISTS_NEEDS_REFACTOR** as dest-grid floor; **MISSING** as `IQuantityConverter`. `Normalize(0.10,1,dest)=0.10` (G7/G10 FAIL). Product hop is now `XauUsdOneToOneCopyPolicy` (`AllocationFactor=1`; persist `instruction.Lots`; unused `FixOrderQtyUnits=lots×100`). Copy hop has no `35=D`. `NewOrderSingleImplemented=false`. 118–178 `×0.05` STALE. 178 `38=1000` STALE (demo helper now `(38,"1")` / 391 lines). Census 18/8460 re-summed. Risk to capital **NONE** (`SAFE_BY_ABSENCE` on copy hop). |

---

## 2026-08-18 — P500_BOOK_22 CTraderFixSession outbound 35=A only; copy-all 8463 copies RISK_BLOCKED losses

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_22 |
| Slot | 22 |
| Purpose | Read `CTraderFixSession.cs`. Prove outbound MsgType is only `A`. No `35=D`. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge; copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_22.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| HTTP | Local `:5000` not reachable from this worker (SSRF-blocked). Book cited from `P500_PROFIT_SYNTHESIS.md` + Manager census 18/8460. |
| Verdict | **PASS_35A_ONLY; COPY_ALL_8463_NEGATIVE_EV.** Assigned 135/135: `(35,"A")` only; `WriteAsync=1`; `35=D=0`; sockets disposed. Product literal `35=D=0`. Copy `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. Residual: `.env` L73 **true**; DI binds; hosted no re-pin; demo `Build("D")` off-hop. Copy-all 8463 would include `RISK_BLOCKED` (pin 29 / −$241,580); scored XAU −$154,425; dest PnL $0. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500 slot 196 `GetTradersAsync` scores-only vs all `Mt5Accounts`

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_196 |
| Slot | 196 |
| Purpose | Check `EfDashboardQueries.GetTradersAsync` only scores vs all `Mt5Accounts`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_196.md` |
| Product source modified | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| Live attach this slot | **No** (census re-summed from 08:42Z JSON) |
| Verdict | **PASS_ALL_ACCOUNTS_NO_LIVE_SEND.** Driver is `foreach (var account in accounts)` L99 + left-join scores (A005 scores-only is stale). Catalog `*` + all users (prior 18/8460 re-summed; P500 8463 unreconciled). Hosted score = `ListLoginsWithDealsAsync`. Copy hop `35=D` absent (`CTraderFixSession` is `35=A`; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`; 0 `ExecutionIntent` writers). Mid-slot drift: shadow `AllocationFactor` now `1m` (`XauUsdOneToOneCopyPolicy`) — dest-ruin **if** sent. Residual: DI binds `.env` `REAL_COPY=true`; demo `CTraderFixDemoTestTrade.Build("D")` ×3 (391 lines; L139/L163/L197) is CLI-only + demo-gated (W176 349/L126 map stale). Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_0 quality 95.50 vs negative netSourcePnl

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_0 |
| Slot | 0 |
| Purpose | Read `BaselineScorer.cs`. Recalculate how quality 95.50 can coexist with negative `netSourcePnl`. Quote the formula. Measured evidence for higher profit / lower loss. Honesty: wanting profit is not an edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_0.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| This-slot `:5000` GET | Blocked (SSRF). Pins from synthesis + catalog JSON. |
| Verdict | **CONFIRMED_SPLIT_NOT_EDGE.** 95.50 = `50+15+10+5+18−2.5` at `(b,r)=(90,10)` only; requires XAU `NetPnl>0` and `PF>=1.8`. Dashboard `netSourcePnl` is all-symbol Σ. 302252 (−68.46) / 303174 (−29.38) match catalog `1000−balance`. Copy-all 8463 would copy `RISK_BLOCKED` −$241,580. Dest PnL $0. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_17 allocation must stay 0.01–0.05 until dest shadow EV after costs is positive

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_17 |
| Slot | 17 |
| Purpose | Measured evidence: allocation factor must be tiny (0.01–0.05 of source) until shadow expectancy after costs is positive. Higher profit / lower loss. Honesty: wanting profit is not an edge; copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_17.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave P500 pin (8463 / −$154,425 / RISK_BLOCKED −$241,580 / dest $0). |
| Verdict | **ALLOCATION_MUST_STAY_TINY.** Product HEAD `XauUsdOneToOneCopyPolicy.AllocationFactor = 1m` (CopyTradingService aliases it). Prior 0.05 hop **stale**. Dest shadow EV after costs **not proven**. Copy-all 8463 = import −$241k tail. Risk to capital **NONE** (`SAFE_BY_ABSENCE`); **HIGH** if 1:1 send armed. |

---

## 2026-08-18 — P500_BOOK_23 TradeReconstructor / 303274 same-second 0.05 grid

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_23 |
| Slot | 23 |
| Purpose | Read `TradeReconstructor` and 303274-style overlapping 0.05-lot same-second entries. Is grid flagged? Evidence for higher profit / lower loss. Do not modify product. Never enable REAL_COPY. Never send 35=D. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_23.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| SUT | `TradeReconstructor.cs` 347 lines; `GroupBy(PositionId)` + `ScaleIn` worse-than-VWAP latch only |
| Catalog | login **303274** `demo\yo-2step` 16228.24 (`LIVE_GROUPS_AND_TRADERS.json`) |
| API | localhost `/api/overview` + `/api/traders` blocked (SSRF); census from synthesis 8463 / RISK_BLOCKED 29 / −$241,580 |
| Verdict | **GRID_NOT_FLAGGED.** Distinct hedge `PositionId`s never set `WasAveragedDown` / martingale. SHADOW 93.50 legal. Copy-all 8463 copies RISK_BLOCKED losses. Wanting +$1,228 is not dest edge. Risk to capital **NONE** now (no send). |

---

## 2026-08-18 — P500_BOOK_4 architecture §3 dest-net vs first-3 / copy-all

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_4 |
| Slot | 4 |
| Purpose | Read architecture §3 business goal. Future destination-net PnL is the target, not first-3 dollars. Measured evidence for higher dest profit / lower dest loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_4.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` | **Not sent** |
| Localhost API | Not re-probed (SSRF block on 127.0.0.1). Used on-disk probe 18/8460 + P500 remasure 8463 / RISK_BLOCKED 29 / −$241,580 / dest PnL literal 0. |
| Verdict | **PASS as §3 reading. FAIL as a live profit claim.** First-3 / `EarlyQualityScore` is source quality, not dest-net. Copy-all 8463/8460 would spray 29 martingale `RISK_BLOCKED` names (−$241k source) plus a 100% Achiever demo/contest book onto one Pepperstone login. Higher dest profit / lower dest loss = keep `35=D` off; keep `XauUsdOneToOneCopyPolicy` (n≥20, no RISK_BLOCKED, no demo, no lookahead); do not rank by first-3 dollars. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_8 kill-switch $2000 / $500 are loss caps, not an edge

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_8 |
| Slot | 8 |
| Purpose | Measured evidence: `MaxDailyExecutionLoss=2000` and `MaxLossPerTrader=500` are loss caps, not an edge. Wanting profit does not create expectancy. Copy-all 8463 would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_8.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` | **No** |
| Verdict | **LOSS_CAPS_NOT_EDGE.** Caps fire after dest (or a mis-fed source ticket) is already ≤ −$500 / −$2000; they do not read `RISK_BLOCKED`; copy hop zeros `DailyExecutionPnl` so the daily line is dead; close is frozen after the cap (A71 G21–G22 FAIL). Copy-all 8463 EV is the scored XAU book −$154,425 (blocked tail −$241,580). Dest risk today **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_21 RiskEngine reject reasons vs dest loss

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_21 |
| Slot | 21 |
| Purpose | Read `RiskEngine.cs`. List every reject reason that reduces dest loss if live send existed. Honesty: wanting profit is not an edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_21.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` | **No** |
| Verdict | **19 reject reasons.** 16 would cut *new* dest loss if send honored `AllowFixSend`; 3 loss/DD also freeze closes. `TRADER_RISK_BLOCKED` **never emitted**. Copy-all 8463/8460 would copy −$241,580 `RISK_BLOCKED` tail. Dest **$0** only via `SAFE_BY_ABSENCE`. |

---

## 2026-08-18 — P500_BOOK_14 ML not built; deterministic baseline only

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_14 |
| Slot | 14 |
| Purpose | Measured evidence for higher dest profit / lower dest loss. Topic: ML is not built. Do not invent a model. Deterministic baseline only. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_14.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Live API this slot | **Not re-probed** (loopback GET blocked). Book integers from same-day `P500_PROFIT_SYNTHESIS.md` pin. |
| Verdict | **ML_NOT_BUILT_CORRECT.** `D:\Prop\services` empty; `mlProbability` literal null; ranker is `BaselineScorer` rules (`CanPromoteToLive => false`). Copy-all 8463 would include 29 `RISK_BLOCKED` names (source tail **−$241,580**) and a scored XAU book **−$154,425**. Dest PnL **$0** (`SAFE_BY_ABSENCE`). Inventing XGBoost/LLM does not create dest edge. Risk to capital **NONE** this process. |

---

## 2026-08-18 — P500_BOOK_3 TradeReconstructor / 303274 same-second 0.05 grid

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_3 (slot 3) |
| Purpose | Read `TradeReconstructor` and 303274-style overlapping 0.05 lot same-second entries. Is grid flagged? Higher profit / lower loss. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_3.md` |
| Product source modified | **No** |
| Secrets printed | **None** |
| `35=D` | **Not sent** |
| Live GET this pass | **Not re-probed** (`127.0.0.1` blocked) |
| Verdict | **GRID_NOT_FLAGGED.** `GroupBy(PositionId)` + `ScaleIn` only on same ticket. No `WasGrid`. 303274-class averaging/martingale false; SHADOW reachable. Copy-all 8463 would copy `RISK_BLOCKED` losses. Dest capital **NONE** today (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_5 Official cTrader FIX QUOTE 5211 / TRADE 5212 / TargetCompID cServer

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_5 |
| Slot | 5 |
| Purpose | Official cTrader FIX identity (QUOTE 5211, TRADE 5212, TargetCompID cServer). Prove Logon is not a fill. Measured higher-profit / lower-loss evidence. Honesty: wanting profit ≠ edge; copy-all 8463 copies RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_5.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| Local API | Not re-probed (SSRF to 127.0.0.1 blocked). Used same-day CREDENTIALS 18/8460 + P500 synthesis 8463 / RISK_BLOCKED 29 / −$241,580. |
| Verdict | **CONFIRMED_OFFICIAL_PORTS_AND_COMPID. LOGON_IS_NOT_A_FILL. NO_DEST_EDGE.** Official SSL 5211/5212 + issued `cServer`. Product copy hop is one-shot `35=A` then dispose. Dest PnL constructor 0. Copy-all would import RISK_BLOCKED tail. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_1 RiskEngine reject reasons that cut dest loss

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_1 |
| Slot | 1 |
| Purpose | Read `RiskEngine.cs`. List every reject reason that reduces dest loss **if live send existed**. Measured evidence for higher profit / lower loss. Honesty: wanting profit ≠ edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_1.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| API this slot | Loopback GET blocked (SSRF). Book pin = `P500_PROFIT_SYNTHESIS.md` (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **−$241,580** / dest PnL **$0**). |
| Verdict | **16 / 19** `Reject()` reasons cut **new-exposure** dest loss if they sat in front of a sender. **3 / 19** (`MAX_LOSS_PER_TRADER`, `MAX_DAILY_EXECUTION_LOSS`, `MAX_PORTFOLIO_DRAWDOWN`) freeze closes and **increase** trapped loss. **0** emit `TRADER_RISK_BLOCKED`. Copy-all 8463 remains **−EV**. Dest capital **NONE** today (`SAFE_BY_ABSENCE`: no NOS, persist `AllowFixSend=false`). |

---

## 2026-08-18 — W500 slot 199 RiskEngine between CopyIntent and ExecutionIntent

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_199 |
| Slot | 199 |
| Purpose | Check whether `RiskEngine` sits between `CopyIntent` and `ExecutionIntent`. Goal: fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_199.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** |
| Verdict | **NO_HOP.** Architecture §4/§32/§39/§75 require CopyIntent → Evaluate → ExecutionIntent. Product: 1 Evaluate caller (`CopyTradingService.GenerateShadowIntentsAsync`); `RiskDecisionRecord` written with `AllowFixSend` forced false; `VenueReconciled`/`NewOrderSingleImplemented` const false; 0 `ExecutionIntent` writers; `PersistDemoShadowAsync` still bypasses Evaluate; no hop `35=D`. Catalog still ALL groups/users (prior 18/8460). Slots 19/59 “0 callers” stale. 119 PARTIAL_HOP overstates. 139/159/179 same tree (no product drift). Risk to capital: **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_188 REAL_COPY_EXECUTION_ENABLED must stay false

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_188 |
| Slot | 188 |
| Purpose | Confirm `REAL_COPY_EXECUTION_ENABLED` must stay false. No `35=D` NewOrderSingle until risk/recon gates. Fetch ALL Achiever+Starwave groups/traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_188.md` |
| Product source modified | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| Live attach this pass | **No** |
| Verdict | **CONFIRMED_MUST_STAY_FALSE.** §68 **0/19**, §70 **0/14**, §69 **0/12**. Copy hop product `35=D=0`; `CTraderFixSession` outbound is `35=A`. Residual: lab `.env` L73 is `true` and DI binds it; hosted logon no longer re-pins (W500_68/108 pin-false **stale**). `CTraderFixDemoTestTrade.Build("D")` is tools-only + demo-gated (refuses `1369850`; W500_148 “only 35=A” **stale**). `CopyTradingService` const `NewOrderSingleImplemented=false` / `VenueReconciled=false`; persist `AllowFixSend=false`. YoPips `src` 0 cTrader senders. Census 18/8460 read-only. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_18 never flatten MT5 source (dest-only flatten)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_18 |
| Slot | 18 |
| Purpose | Measured evidence for higher profit / lower loss. Never flatten MT5 source. Destination-only flatten. Honesty: wanting profit ≠ edge. Copy-all 8463 imports RISK_BLOCKED losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_18.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| REAL_COPY flipped | **No** |
| Live API | `:5000` not re-probed (SSRF). Pins: synthesis 8463 / Manager 8460. |
| Verdict | **DEST_ONLY_FLATTEN_LAW.** Source C# path GET-only (0 `DealerSend`). Hosted FIX `35=A` only. Product `EmergencyFlatten` blocks opens only — no dest run. Demo dest-721 flatten refuses `1369850`. Scored XAU **−$154,425**; `RISK_BLOCKED` **−$241,580** (29). Dest PnL **$0** (`SAFE_BY_ABSENCE`). Copy-all 8463 would import that tail. Risk to capital **NONE** today; **DEST_RUIN_IF_SENT** if copy-all / blocked tail / 5-lot dest flatten. |

---

## 2026-08-18 — W500_RESEARCH_189 trade #3 EARLY_SCORE/SHADOW never auto LIVE

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_189 |
| Slot | 189 |
| Purpose | Confirm trade #3 is EARLY_SCORE/SHADOW never auto LIVE. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_189.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** `FromBaseline` reachable set `{INSUFFICIENT_DATA, EARLY_SCORE, WATCH, SHADOW, RISK_BLOCKED}` — no LIVE. `CanPromoteToLive => false`. Copy `SHADOW_ONLY`. Hosted `35=D` absent. `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. Residual: DI binds env `REAL_COPY=true`; off-path `CTraderFixDemoTestTrade.Build("D")` ×3 (demo-gated, not DI). Census 18/8460 (re-summed JSON). Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_193 Api.csproj TFM vs MT5APIManager64

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_193 |
| Slot | 193 |
| Purpose | Check `Api.csproj` TargetFramework. `net8.0` without windows/x64 vs `MT5APIManager64` load. ALL Achiever+Starwave groups/traders. No live cTrader orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_193.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** |
| Verdict | **PASS.** API is `net8.0-windows` + `PlatformTarget` x64; restore `success: true`; trio in `bin\Debug\net8.0-windows\`; `bases/` 2027+9904 prove prior LoadLibrary. Isolated `net8.0` x64 can Initialize (R021). Product `net8.0` hosts still NU1201. Census 18/8460. `35=D` `SAFE_BY_ABSENCE`. Env REAL_COPY=true armed; sender missing. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_187 cTrader venue / cServer / 5211-5212 / no live send

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_187 |
| Slot | 187 |
| Purpose | Confirm cTrader is destination venue not LP. TargetCompID `cServer` case preserved. Ports 5211 QUOTE / 5212 TRADE SSL. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_187.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Venue ≠ LP. Live path `56=cServer` (no fold). QUOTE TLS 5211 / TRADE TLS 5212. Census 18/8460 (re-summed, not re-probed). Hosted `35=D` absent — `SAFE_BY_ABSENCE`. Residual: DI binds `.env REAL_COPY=true` (slots 27/47/67/87/107/127 hard-false / re-pin **stale**); sender still unimplemented on copy path. Dead leftover: API JSON `CSERVER`+5201/5202 unbound. Demo CLI can `Build("D")` on demo only (not this process). Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_180 MT5APIManager.h request APIs work without pump

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_180 |
| Slot | 180 |
| Purpose | Read `MT5APIManager.h` `GroupRequestArray` / `UserRequestArray` / `UserLogins` / `PositionRequestByGroup` / `DealRequestByGroup`. Confirm request APIs work without pump. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_180.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Five APIs are network RPCs; pump optional (Admin MAIL/NEWS-only enum still has four of five; pool `Connect(...,0)` still calls `UserLogins`). C# request-first, no `_pumpEnabled` branch. Live census 18 groups / 8460 traders / 1984 pos (re-summed; not re-attached). `35=D` absent (`SAFE_BY_ABSENCE`). Residual: DI binds env `REAL_COPY_EXECUTION_ENABLED` (slots 80/100/120 hard-false pin is stale); sender still unimplemented. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_192 NativeMt5BrokerConnector GroupRequestArray / UserRequestArray

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_192 |
| Slot | 192 |
| Purpose | Search `NativeMt5BrokerConnector` for `GroupRequestArray` and `UserRequestArray`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_192.md` |
| Product source modified | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| Live attach this pass | **No** |
| Verdict | **PASS.** Primary walks are `GroupRequestArray("*")` L155 and per-group `UserRequestArray` L223. Ingest/`LiveBrokerProbe` use `GetAccountsAsync(null)`. Live census 8/6512 + 10/1948 = 18/8460 (re-summed, not re-probed). Host `35=D` absent; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. Env `REAL_COPY=true` is bound by current DI but cannot emit a ticket (`SAFE_BY_ABSENCE`). Residual: demo-only `CTraderFixDemoTestTrade.Build("D")` L139/163/197 is not on the host hop. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_185 UserGetByGroup pump-cache vs UserRequestArray

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_185 |
| Slot | 185 |
| Purpose | Confirm `UserGetByGroup` is pump-cache and `UserRequestArray` is the request path for ALL traders. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_185.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** |
| Verdict | **CONFIRMED.** `UserGetByGroup` = pump-cache (`PUMP_MODE_USERS`; absent on Admin). `UserRequestArray` = network; C# primary at `ReadAccountsForGroup` L223; cache fallback only on hard fail; empty → `UserLogins`. Census 18/8460 re-summed (08:42Z, not re-probed). Hosted hop `35=A` only; `NewOrderSingleImplemented=false`. Env REAL_COPY may be true; sender missing. Demo-tool `Build("D")` is off-host. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_183 slot 183 (1012 + Achiever HTTP proxy)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_183 |
| Slot | 183 |
| Purpose | Confirm `MT_RET_AUTH_MANAGER_IPBLOCK=1012` and Achiever HTTP proxy `81.29.145.69:49527`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_183.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** 1012 is the official Manager IP-block retcode. This LAN needs `ProxySet PROXY_HTTP 81.29.145.69:49527` for Achiever (else 1012). Starwave stays direct. Live census 18 groups / 8460 traders (re-summed; not re-attached). Copy hop `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`). Residual: DI binds env `REAL_COPY_EXECUTION_ENABLED` and lab `.env` L73 is `true`; standalone demo helper can `Build("D")` but refuses live identity and is unused by copy. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_170 CTraderFixSession 35=D / NewOrderSingle

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_170 |
| Slot | 170 |
| Purpose | Search `CTraderFixSession.cs` for `35=D` or `NewOrderSingle`. FAIL if live send exists. ALL Achiever+Starwave groups/traders; copy must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_170.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS.** Assigned file 135/135: `NewOrderSingle=0`, `35=D=0`; only outbound MsgType is `(35, "A")` Logon; one `WriteAsync`; sockets disposed. W500_130/150 “product `35=D=0` / single FIX writer” **STALE**: sibling `CTraderFixDemoTestTrade` (371) `Build("D")` ×3 is demo-gated (refuse `live-*` / `live.*` / account `1369850`) and called only from `tools/DemoFixTestTrade` (not copy/DI/API). Copy `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. Residual: `.env` `REAL_COPY_EXECUTION_ENABLED=true` and DI binds it; hosted no longer re-pins false. Census cited 18/8460. Risk to capital **NONE** (`SAFE_BY_ABSENCE` on assigned+copy; demo tool not invoked). |

---

## 2026-08-18 — W500_RESEARCH_190 CTraderFixSession 35=D / NewOrderSingle

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_190 |
| Slot | 190 |
| Purpose | Search `CTraderFixSession.cs` for `35=D` or `NewOrderSingle`. FAIL if live send exists. ALL Achiever+Starwave groups/traders; copy must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_190.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS.** Assigned file 135/135: `NewOrderSingle=0`, `35=D=0`; only outbound MsgType is `(35, "A")` Logon; one `WriteAsync`; sockets disposed. Product literal `35=D=0`. YoPips C++ `src` has 0 cTrader FIX senders. Copy hop const `NewOrderSingleImplemented=false` + persist `AllowFixSend:=false`. Residual: DI binds env `REAL_COPY_EXECUTION_ENABLED=true`; hosted logon no longer re-pins false; sibling `CTraderFixDemoTestTrade` can `Build("D")` (demo-gated, tools-only). Census cited 18/8460. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_181 YoPips Connect pump-none + proxy IP:port / login:password

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_181 |
| Slot | 181 |
| Purpose | Read YoPips `mt5_manager.cpp` Connect fallback to pump-none and proxy `IP:port` / `login:password`. ALL Achiever+Starwave groups/traders. No live cTrader orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_181.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED_WITH_GROUPS_CACHE_GAP.** Fallback `Connect(...,0)` exists; proxy packs `IP:port`+`login:password`. Wrapper `0` remaps to 649 and omits GROUPS. YoPips `GetAllGroups` cache-only (`GroupRequestArray` 0 in `src\`). ALL traders via `UserLogins`. C# ALL path `GroupRequestArray("*")` + `UserRequestArray`. Census 18/8460 re-summed (not re-attached). Copy hop `SAFE_BY_ABSENCE` (`NewOrderSingleImplemented=false`, persist `AllowFixSend=false`). Residual: DI binds `.env` `REAL_COPY=true`; demo `CTraderFixDemoTestTrade.Build("D")` exists off-hop. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_178 QuantityNormalizer lots ↛ FIX OrderQty (slot 178)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_178 |
| Slot | 178 |
| Purpose | Check `QuantityNormalizer` never blindly converts MT5 lots to FIX `OrderQty`. Fetch ALL Achiever+Starwave groups/traders. Copy-to-cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_178.md` |
| Product source modified | **No** |
| Secret values printed | **None** (flag booleans only) |
| Live attach this pass | **No** |
| Verdict | **EXISTS_NEEDS_REFACTOR** as dest-grid floor; **MISSING** as `IQuantityConverter`. `Normalize(0.10,1,dest)=0.10` (G7/G10 FAIL). Product now calls `Normalize(lots,0.05,GoldSpec)` (`1.00→0.05 ≠ 5.00 oz`). Copy hop has no `35=D` / tag 38. `NewOrderSingleImplemented=false`. Env `REAL_COPY` may be true; persist `AllowFixSend=false`. 78/98 “zero callers” STALE. 138/158 “0 `35=D` in product” STALE: `CTraderFixDemoTestTrade` hardcodes `38=1000`; demo JSON `OrderSent=true`/`Filled=false` (not this type). Census 18/8460 independent. Risk to capital **NONE** (`SAFE_BY_ABSENCE` on copy hop). |

---

## 2026-08-18 — W500 slot 186 `IMTDeal.Volume` scale 10000

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_186 |
| Slot | 186 |
| Purpose | Confirm `IMTDeal.Volume` scale is **10000**, not hundredths, not `VolumeExt` 1e8. Goal: fetch ALL Achiever+Starwave groups/traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_186.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Official `MTAPI_VOLUME_DIV=10000.0`; extractors copy `deal->Volume()` (0 `VolumeExt` calls). C# default `10_000`. E004 3/3 VolumeConverter tests Passed. D92 eval `ctor_default_Scale=10000`. Hundredths is a `mt5_types.h` comment bug. Census 18/8460 re-summed (08:42Z). `35=D` absent; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. Residual: DI binds env `REAL_COPY` (may be true). Risk to capital: **NONE**. |

---

## 2026-08-18 — W500 slot 175 `DealIngestionService` `Take(200)` positions cap

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_175 |
| Slot | 175 |
| Purpose | Check `DealIngestionService` `Take(200)` positions cap. Fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_175.md` |
| Product source modified | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| Live `35=D` sent | **No** |
| Verdict | **PASS_CAP_REMOVED.** Current `DealIngestionService` (146 lines) has zero `Take(`/`Skip`. Live path uses `GetGroupPositionsAsync("*")` or `foreach` all accounts. Only leftover `Take(200)` is `GET /api/trades` reconstructed rows (`Program.cs` L110). Probe JSON re-summed 18/8460/1984 (header + group sums + login-array arithmetic). `35=D` absent on copy hop (`SAFE_BY_ABSENCE`). Hosted scoring is `ListLoginsWithDealsAsync`. Residual: DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`; `CTraderFixDemoTestTrade.Build("D")` exists but is un-wired + demo-gated. Risk to capital: **NONE**. |

---

## 2026-08-18 — W500 slot 182 YoPips `mt5_group_probe` (no password echo)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_182 |
| Slot | 182 |
| Purpose | Read YoPips `mt5_group_probe.cpp`. How does a proven probe enumerate all groups without echoing passwords? Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_182.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** |
| Verdict | **CONFIRMED_GROUPS_ONLY_NO_PASSWORD_ECHO.** C++ `mt5_group_probe` prints manager-visible group names via `GetAllGroups` (`GroupTotal`+`GroupNext`), never passwords (`spdlog` off; JSON has no secret keys). Traders are a sibling walk (`UserLogins`/`UserRequestArray`) already measured by `LiveBrokerProbe`: Achiever 8/6512, Starwave 10/1948. Probe exe absent (vcxproj generated, FileListAbsolute empty). Copy hop no `35=D`; `NewOrderSingleImplemented=false`. Demo helper `Build("D")` is CLI-only + demo-gated. This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_184 Starwave must connect direct (no proxy)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_184 |
| Slot | 184 |
| Purpose | Confirm Starwave must connect direct with no proxy. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_184.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Starwave `Connect(84.201.6.142:443)` with `ProxySet` skipped. C# hardcodes `ProxyEnabled=false`. Achiever HTTP hop is the other broker. Live census 10/1948 direct (total 18/8460 re-summed). Hosted `35=D` absent; `NewOrderSingleImplemented=false`. Residual: DI binds env `REAL_COPY_EXECUTION_ENABLED` and lab `.env` L73 is `true`. Demo helper MsgType D is gated off live account. Risk to capital **NONE**. |

---

## 2026-08-18 — W500 slot 177 FEATURE_COPY / REAL_COPY defaults

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_177 |
| Slot | 177 |
| Purpose | Check `FEATURE_COPY_TRADING_ENABLED` and `REAL_COPY_EXECUTION_ENABLED` defaults. Fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_177.md` |
| Product source modified | **No** |
| Secret values printed | **None** (quoted only flag names `=true`/`=false`) |
| Live attach this pass | **No** |
| Verdict | **PASS_NO_LIVE_SEND_ENV_ARMED.** Architecture/POCO REAL_COPY default still **false**. Local `.env` L73+L106 both **true**. DI now binds `REAL_COPY_EXECUTION_ENABLED` (`DependencyInjection.cs` L41). Logon re-pin **removed**. API FEATURE literal **true**. Fetch ALL flag-blind (prior census 18/8460). Copy hop `35=D` absent; `NewOrderSingleImplemented=false`; `AllowFixSend` persisted false. Residual: next sender would see runtime armed; demo helper `Build("D")` exists but is unused by copy and demo-gated. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500 slot 176 `GetTradersAsync` scores-only vs all `Mt5Accounts`

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_176 |
| Slot | 176 |
| Purpose | Check `EfDashboardQueries.GetTradersAsync` only scores vs all `Mt5Accounts`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_176.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** |
| Verdict | **PASS_ALL_ACCOUNTS_NO_LIVE_SEND.** Driver is `foreach (var account in accounts)` L99 + left-join scores (A005 scores-only is stale). Catalog `*` + all users (prior 18/8460 re-summed; P500 8463 unreconciled). Hosted score = `ListLoginsWithDealsAsync`. Copy/API `35=D` absent (`CTraderFixSession` = `35=A` only). Residual: `CTraderFixDemoTestTrade.Build("D")` is demo-gated CLI only (slots 136/156 “product 35=D=0” stale). DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`; `CopyTradingService` writes SHADOW only (`NewOrderSingleImplemented=false`). Risk to capital **NONE** (`SAFE_BY_ABSENCE`). This slot did not live-attach. |

---

## 2026-08-18 — W500_RESEARCH_168 REAL_COPY_EXECUTION_ENABLED must stay false

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_168 |
| Slot | 168 |
| Purpose | Confirm `REAL_COPY_EXECUTION_ENABLED` must stay false. No `35=D` NewOrderSingle until risk/recon gates. Fetch ALL Achiever+Starwave groups/traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_168.md` |
| Product source modified | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| Live attach this pass | **No** |
| Verdict | **CONFIRMED_MUST_STAY_FALSE.** Law §41 / §68 **0/19** / §70 **0/14**. Live hop `35=D=0`; only outbound on `CTraderFixSession` is `35=A`. Copy `NewOrderSingleImplemented`+`VenueReconciled` const false; persist `AllowFixSend=false`. Residual: `.env` L73 **true**; DI L41 binds it; hosted no re-pin (W500_68/108 stale); demo `CTraderFixDemoTestTrade.Build("D")` is CLI-only + demo-gated. Census 18/8460 read-only. YoPips `src` 0 cTrader senders. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_161 YoPips Connect pump-none + proxy packing

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_161 |
| Slot | 161 |
| Purpose | Read YoPips `mt5_manager.cpp` Connect fallback to pump-none and proxy `IP:port` / `login:password`. ALL Achiever+Starwave groups/traders. No live cTrader orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_161.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** (census re-summed from 08:42Z JSON) |
| Verdict | **CONFIRMED_WITH_GROUPS_CACHE_GAP.** Fallback `Connect(...,0)` exists. Proxy packs `address=IP:port` `auth=login:password`. Wrapper `pumpMode=0` remaps (omits GROUPS). `GetAllGroups` is cache-only. `UserLogins` is request-complete. YoPips `.env` `MT5_PROXY_ENABLED` unread (`IS_MT5_PROXY_ENABLED`). Copy hop `35=D` absent; demo helper `Build("D")` off-hop/demo-gated. Env `REAL_COPY=true` bound; copy sender missing. Census 18/8460 prior. Risk to capital **NONE**. |

---

## 2026-08-18 — W500 slot 179 RiskEngine between CopyIntent and ExecutionIntent

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_179 |
| Slot | 179 |
| Purpose | Check whether `RiskEngine` sits between `CopyIntent` and `ExecutionIntent`. Goal: fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_179.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this slot | **No** |
| Verdict | **NO_HOP.** Architecture §4/§32/§39/§75 require CopyIntent → Evaluate → ExecutionIntent. Product: 1 Evaluate caller (`CopyTradingService` L159) + `RiskDecisions.Add` with `AllowFixSend=false` hardcoded; DI registers unused `RiskEngine` singleton; hosted copy every 20s. Still 0 `ExecutionIntent` writers; no product `35=D`; `NewOrderSingleImplemented`/`VenueReconciled` const false. Demo `PersistDemoShadowAsync` still bypasses Evaluate. Catalog still ALL groups/users (prior 18/8460). Slots 19/39/59/99 Evaluate=0 stale; slot 119 PARTIAL_HOP overstates; 139/159 same tree. Risk to capital: **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_164 Starwave must connect direct (no proxy)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_164 |
| Slot | 164 |
| Purpose | Confirm Starwave must connect direct with no proxy. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_164.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** |
| Verdict | **CONFIRMED.** Starwave `Connect(84.201.6.142:443)` with `ProxySet` skipped. C# hardcodes `ProxyEnabled=false`. Achiever HTTP hop is the other broker. Live census 10/1948 direct (total 18/8460 re-summed). Copy hop `35=D` absent; `NewOrderSingleImplemented=false`. Residual: DI binds env `REAL_COPY_EXECUTION_ENABLED` and lab `.env` L73 is `true`; demo helper `CTraderFixDemoTestTrade` can `Build("D")` off-hop, demo-gated. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_169 trade #3 EARLY_SCORE/SHADOW never auto LIVE

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_169 |
| Slot | 169 |
| Purpose | Confirm trade #3 is EARLY_SCORE/SHADOW never auto LIVE. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_169.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** |
| Verdict | **CONFIRMED.** `FromBaseline` reachable set `{INSUFFICIENT_DATA, EARLY_SCORE, WATCH, SHADOW, RISK_BLOCKED}` — no LIVE. `CanPromoteToLive => false`. Copy `SHADOW_ONLY`. Copy-hop `35=D` absent (`CTraderFixSession` is `35=A`). Residual demo `CTraderFixDemoTestTrade.Build("D")` off-hop. `REAL_COPY` env-driven (may be true; no copy sender). Census 18/8460 (re-summed JSON). Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_167 cTrader venue / cServer / 5211-5212 / no live send

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_167 |
| Slot | 167 |
| Purpose | Confirm cTrader is destination venue not LP. TargetCompID `cServer` case preserved. Ports 5211 QUOTE / 5212 TRADE SSL. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_167.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** |
| Verdict | **CONFIRMED.** Venue ≠ LP. Live path `56=cServer` (no fold). QUOTE TLS 5211 / TRADE TLS 5212. Census 18/8460 (re-summed, not re-probed). Copy `35=D` absent — `SAFE_BY_ABSENCE`. Residual: DI binds `.env REAL_COPY=true` (slots 107/127 re-pin stale); demo-only `Build("D")` helper exists, not on copy hop, live account gated. Dead leftover: API JSON `CSERVER`+5201/5202 unbound. Risk to capital **NONE**. |

---

## 2026-08-18 — W500 slot 182 YoPips `mt5_group_probe` (no password echo)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_182 |
| Slot | 182 |
| Purpose | Read YoPips `mt5_group_probe.cpp`. How does a proven probe enumerate all groups without echoing passwords? Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_182.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** |
| Verdict | **CONFIRMED_GROUPS_ONLY_NO_PASSWORD_ECHO.** C++ `mt5_group_probe` prints manager-visible group names via `GetAllGroups` (`GroupTotal`+`GroupNext`), never passwords (`spdlog` off; JSON has no secret keys). Traders are a sibling walk (`UserLogins`/`UserRequestArray`) already measured by `LiveBrokerProbe`: Achiever 8/6512, Starwave 10/1948. Probe exe absent (vcxproj generated, FileListAbsolute empty). No `35=D`. `NewOrderSingleImplemented=false`. This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — W500 slot 162 YoPips `mt5_group_probe` (no password echo)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_162 |
| Slot | 162 |
| Purpose | Read YoPips `mt5_group_probe.cpp`. How does a proven probe enumerate all groups without echoing passwords? Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_162.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this slot | **No** |
| Verdict | **CONFIRMED_GROUPS_ONLY_NO_PASSWORD_ECHO.** C++ `mt5_group_probe` prints manager-visible group names via `GetAllGroups` (`GroupTotal`+`GroupNext`), never passwords (`spdlog` off; JSON has no secret keys). Traders are a sibling walk (`UserLogins`/`UserRequestArray`) already measured by `LiveBrokerProbe`: Achiever 8/6512, Starwave 10/1948. Probe exe absent (vcxproj generated, FileListAbsolute empty). No `35=D`. `NewOrderSingleImplemented=false`. This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — W500 slot 159 RiskEngine between CopyIntent and ExecutionIntent

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_159 |
| Slot | 159 |
| Purpose | Check whether `RiskEngine` sits between `CopyIntent` and `ExecutionIntent`. Goal: fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_159.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** |
| Verdict | **NO_HOP.** Architecture §4/§32/§39/§75 require CopyIntent → Evaluate → ExecutionIntent. Product: 1 Evaluate caller (`CopyTradingService.GenerateShadowIntentsAsync`); `RiskDecisionRecord` written with `AllowFixSend` forced false; `VenueReconciled`/`NewOrderSingleImplemented` const false; 0 `ExecutionIntent` writers; `PersistDemoShadowAsync` still bypasses Evaluate; no `35=D`. Catalog still ALL groups/users (prior 18/8460). Slots 19/59 “0 callers” stale. 119/139 same tree (no product drift). Risk to capital: **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_173 Api.csproj TFM vs MT5APIManager64

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_173 |
| Slot | 173 |
| Purpose | Check `Api.csproj` TargetFramework. `net8.0` without windows/x64 vs `MT5APIManager64` load. ALL Achiever+Starwave groups/traders. No live cTrader orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_173.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** |
| Verdict | **PASS.** API is `net8.0-windows` + `PlatformTarget` x64; restore `success: true`; trio in `bin\Debug\net8.0-windows\`; `bases/` 2027+9904 prove prior LoadLibrary. Isolated `net8.0` x64 can Initialize (R021). Product `net8.0` hosts still NU1201. Census 18/8460. `35=D` `SAFE_BY_ABSENCE`. Env REAL_COPY=true armed; sender missing. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_163 slot 163 (1012 + Achiever HTTP proxy)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_163 |
| Slot | 163 |
| Purpose | Confirm `MT_RET_AUTH_MANAGER_IPBLOCK=1012` and Achiever HTTP proxy `81.29.145.69:49527`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_163.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** (census re-summed from `LIVE_GROUPS_AND_TRADERS.json` 08:42Z) |
| Verdict | **CONFIRMED.** 1012 is the official Manager IP-block retcode. This LAN needs `ProxySet PROXY_HTTP 81.29.145.69:49527` for Achiever (else 1012). Starwave stays direct. Live census 18 groups / 8460 traders (re-summed; not re-attached). Copy hop `SAFE_BY_ABSENCE` (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`). Residual: DI binds env `REAL_COPY_EXECUTION_ENABLED` and lab `.env` L73 is `true`; standalone demo helper can `Build("D")` but refuses live identity and is unused by copy. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_171 Program.cs DemoSeeder FakeMt5 10001 10002 dummy

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_171 |
| Slot | 171 |
| Purpose | Search `Program.cs` for DemoSeeder / FakeMt5 / 10001 / 10002 / dummy. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_171.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** |
| Verdict | **PASS_HOST_NO_DUMMY.** Product `Program.cs` (API 160 + both workers + probe 86): 0 hits for DemoSeeder/FakeMt5/10001/10002/dummy. Startup seed is `BrokerCatalogSeed` only. DI fail-closed Native only. Census re-summed 8/6512 + 10/1948 = 18/8460; dummy logins 0 in live JSON. Residual Worker 4-login scorer. `35=D` SAFE_BY_ABSENCE; `NewOrderSingleImplemented=false`. Delta: DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true` (91/111/CREDENTIALS forced-false is stale). Risk to capital **NONE**. This slot did not live-attach. |

---

## 2026-08-18 — W500_RESEARCH_165 UserGetByGroup pump-cache vs UserRequestArray

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_165 |
| Slot | 165 |
| Purpose | Confirm `UserGetByGroup` is pump-cache and `UserRequestArray` is the request path for ALL traders. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_165.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** |
| Verdict | **CONFIRMED.** `UserGetByGroup` = pump-cache (`PUMP_MODE_USERS`; absent on Admin). `UserRequestArray` = network; C# primary at `ReadAccountsForGroup` L223; cache fallback only on hard fail; empty → `UserLogins`. Census 18/8460/1984 re-summed (08:42Z, not re-probed). Copy hop `35=D` absent (`CTraderFixSession` only `35=A`); `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. Env REAL_COPY may be true; sender missing. Residual: demo helper `CTraderFixDemoTestTrade` can `Build("D")` off-hop, demo-gated. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_172 NativeMt5BrokerConnector GroupRequestArray / UserRequestArray

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_172 |
| Slot | 172 |
| Purpose | Search `NativeMt5BrokerConnector` for `GroupRequestArray` and `UserRequestArray`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_172.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** (census re-summed from 08:42Z `LIVE_GROUPS_AND_TRADERS.json`) |
| Verdict | **PASS.** Primary walks are `GroupRequestArray("*")` L155 and per-group `UserRequestArray` L223. Ingest/`LiveBrokerProbe` use `GetAccountsAsync(null)`. Live census 8/6512 + 10/1948 = 18/8460 (re-summed, not re-probed). Host `35=D` absent; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. Env `REAL_COPY=true` is bound by current DI but cannot emit a ticket (`SAFE_BY_ABSENCE`). Residual: demo-only `CTraderFixDemoTestTrade.Build("D")` is not on the host hop. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_160 MT5APIManager.h request APIs work without pump

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_160 |
| Slot | 160 |
| Purpose | Read `MT5APIManager.h` `GroupRequestArray` / `UserRequestArray` / `UserLogins` / `PositionRequestByGroup` / `DealRequestByGroup`. Confirm request APIs work without pump. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_160.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** |
| Verdict | **PASS_REQUEST_APIS_NO_PUMP.** Five Manager Request APIs are network RPCs (no `DealGet`; no `PUMP_MODE_DEALS`; Admin MAIL/NEWS-only enum still has four of five; pool `Connect(...,0)` still calls `UserLogins`). C# request-first (`GroupRequestArray("*")` L155, `UserRequestArray` L223, `UserLogins` L230, `DealRequestByGroup` L307, `PositionRequestByGroup` L344). `_pumpEnabled` never gates fetch. Census 18/8460/1984 re-summed (08:42Z, not re-probed). Product hop `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. Residual: DI binds env `REAL_COPY=true`; `CTraderFixDemoTestTrade` can emit `35=D` under a demo-host gate (not wired to API/workers). Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_149 trade #3 EARLY_SCORE/SHADOW never auto LIVE

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_149 |
| Slot | 149 |
| Purpose | Confirm trade #3 is EARLY_SCORE/SHADOW never auto LIVE. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_149.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** `FromBaseline` reachable set `{INSUFFICIENT_DATA, EARLY_SCORE, WATCH, SHADOW, RISK_BLOCKED}` — no LIVE. `CanPromoteToLive => false`. Copy `SHADOW_ONLY`. `35=D` absent. `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. Residual: DI binds env `REAL_COPY=true` (slots 9/69/89/109 hard-false pin stale). Census 18/8460 (re-summed JSON). Risk to capital **NONE**. |

---

## 2026-08-18 — W500 slot 155 `DealIngestionService` `Take(200)` positions cap

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_155 |
| Slot | 155 |
| Purpose | Check `DealIngestionService` `Take(200)` positions cap. Fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_155.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_CAP_REMOVED.** Current `DealIngestionService` (146 lines) has zero `Take(`/`Skip`. Live path uses `GetGroupPositionsAsync("*")` or `foreach` all accounts. Only leftover `Take(200)` is `GET /api/trades` reconstructed rows (`Program.cs` L110). Probe JSON re-summed 18/8460/1984. `35=D` absent (`SAFE_BY_ABSENCE`). Hosted scoring is `ListLoginsWithDealsAsync`. Residual: DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`; sender still unimplemented. Risk to capital: **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_154 LiveMt5Registration.HasRealPasswords fail-closed

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_154 |
| Slot | 154 |
| Purpose | Check `LiveMt5Registration.HasRealPasswords` fail-closed. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_154.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_FAIL_CLOSED_DI.** DI throws unless both `MT5_PASSWORD` + `MT5_STARWAVEFX_PASSWORD` pass `IsSecret`; Native ×2 only; no Fake. Residual: Ordinal case hole; `CreateConnectors*` ungated; probe whitespace-only; 0 tests; DI now env-binds `REAL_COPY` (lab `.env` `true`; 14/34/54/114 hard-false pin stale). Census 18/8460 prior. `35=D` `SAFE_BY_ABSENCE`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_158 QuantityNormalizer lots ↛ FIX OrderQty

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_158 |
| Slot | 158 |
| Purpose | Check `QuantityNormalizer` never blindly converts MT5 lots to FIX `OrderQty`. ALL Achiever+Starwave groups/traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_158.md` |
| Product source modified | **No** |
| Secret values printed | **None** (flag booleans only) |
| Verdict | **FAIL as §38 converter; SAFE_BY_ABSENCE on the wire.** `Normalize(0.10,1,dest)=0.10` not `10.00`. Product calls `Normalize(lots,0.05,GoldSpec)` (`1.00→0.05 ≠ 5.00 oz`). No `35=D`/`OrderQty`. `NewOrderSingleImplemented=false`. Env `REAL_COPY` may be true; persist `AllowFixSend=false`. 78/98/D18 “zero callers” + 108/CREDENTIALS forced-false + 127 logon-repin **STALE**. Capital risk **none**. Census 18/8460 independent. |

---

## 2026-08-18 — W500 slot 156 `GetTradersAsync` scores-only vs all `Mt5Accounts`

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_156 |
| Slot | 156 |
| Purpose | Check `EfDashboardQueries.GetTradersAsync` only scores vs all `Mt5Accounts`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_156.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_ALL_ACCOUNTS_NO_LIVE_SEND.** Driver is `foreach (var account in accounts)` L99 + left-join scores (A005 scores-only is stale). Catalog `*` + all users (prior 18/8460 re-summed; P500 8463 unreconciled). Hosted score = `ListLoginsWithDealsAsync`. `35=D` absent. DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`; FIX host no longer pins false; `CopyTradingService` writes SHADOW only (`NewOrderSingleImplemented=false`). W116 stale: `GetRiskAsync` now env-bound; FEATURE copy flag `true`; `/api/copy/*` exists. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). This slot did not live-attach. |

---

## 2026-08-18 — W500_RESEARCH_149 trade #3 EARLY_SCORE/SHADOW never auto LIVE

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_149 |
| Slot | 149 |
| Purpose | Confirm trade #3 is EARLY_SCORE/SHADOW never auto LIVE. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_149.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** `FromBaseline` reachable set `{INSUFFICIENT_DATA, EARLY_SCORE, WATCH, SHADOW, RISK_BLOCKED}` — no LIVE. `CanPromoteToLive => false`. Copy `SHADOW_ONLY`. `35=D` absent. `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. Residual: DI binds env `REAL_COPY=true` (slots 9/69/89/109 hard-false pin stale). Census 18/8460 (re-summed JSON). Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_148 REAL_COPY_EXECUTION_ENABLED must stay false

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_148 |
| Slot | 148 |
| Purpose | Confirm `REAL_COPY_EXECUTION_ENABLED` must stay false. No `35=D` NewOrderSingle until risk/recon gates. Fetch ALL Achiever+Starwave groups/traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_148.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** |
| Verdict | **CONFIRMED_MUST_STAY_FALSE.** §68 **0/19**, §70 **0/14**, §69 **0/12**. Product `35=D=0`; only outbound MsgType is `35=A`. Residual: lab `.env` L73 is `true` and DI binds it; hosted logon no longer re-pins (W500_68/108 pin-false **stale**). `CopyTradingService` const `NewOrderSingleImplemented=false` / `VenueReconciled=false`; persist `AllowFixSend=false`. YoPips `src` 0 cTrader senders. Census 18/8460 read-only. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_153 Api.csproj TFM vs MT5APIManager64

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_153 |
| Slot | 153 |
| Purpose | Check `Api.csproj` TargetFramework. `net8.0` without windows/x64 vs `MT5APIManager64` load. ALL Achiever+Starwave groups/traders. No live cTrader orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_153.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** |
| Verdict | **PASS.** API is `net8.0-windows` + `PlatformTarget` x64; restore `success: true`; trio in `bin\Debug\net8.0-windows\`; `bases/` 2027+9904 prove prior LoadLibrary. Isolated `net8.0` x64 can Initialize (R021). Product `net8.0` hosts still NU1201. Census 18/8460. `35=D` `SAFE_BY_ABSENCE`. Env `REAL_COPY` may be true; sender still unimplemented (slot 113 “forced false” stale). Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_151 Program.cs DemoSeeder / FakeMt5 / 10001 / 10002 dummy

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_151 |
| Slot | 151 |
| Purpose | Search `Program.cs` for DemoSeeder / FakeMt5 / 10001 / 10002 / dummy. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_151.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this slot | **No** |
| Verdict | **PASS_HOST_NO_DUMMY.** API+workers+probe `Program.cs` have 0 `DemoSeeder`/`FakeMt5`/`10001`/`10002`/`dummy` hits. Startup seed is `BrokerCatalogSeed` only. Residual: `DemoSeeder` tests + `Worker.cs` four-login scorer. Hosted score = `ListLoginsWithDealsAsync`. Prior census 18/8460. Copy pipeline SHADOW-only. Env `REAL_COPY` may be true; `35=D` absent. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). This slot did not live-attach. |

---

## 2026-08-18 — W500_RESEARCH_147 cTrader venue / cServer / 5211-5212 / no live send

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_147 |
| Slot | 147 |
| Purpose | Confirm cTrader is destination venue not LP. TargetCompID `cServer` case preserved. Ports 5211 QUOTE / 5212 TRADE SSL. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_147.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Venue ≠ LP. Live path `56=cServer` (no fold). QUOTE TLS 5211 / TRADE TLS 5212. Census 18/8460 (re-summed, not re-probed). `35=D` absent — `SAFE_BY_ABSENCE`. Residual: DI binds `.env REAL_COPY=true` (slots 27/47/67/87/107 hard-false pin is stale); sender still unimplemented. Dead leftover: API JSON `CSERVER`+5201/5202 unbound. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_138 QuantityNormalizer lots ↛ FIX OrderQty (slot 138)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_138 |
| Slot | 138 |
| Purpose | Check `QuantityNormalizer` never blindly converts MT5 lots to FIX `OrderQty`. Fetch ALL Achiever+Starwave groups/traders. Copy-to-cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_138.md` |
| Product source modified | **No** |
| Secret values printed | **None** (flag booleans only) |
| Live attach this pass | **No** |
| Verdict | **EXISTS_NEEDS_REFACTOR** as dest-grid floor; **MISSING** as `IQuantityConverter`. `Normalize(0.10,1,dest)=0.10` (G7/G10 FAIL). Product now calls `Normalize(lots,0.05,GoldSpec)` (`1.00→0.05 ≠ 5.00 oz`). No `35=D` / tag 38. `NewOrderSingleImplemented=false`. Env `REAL_COPY` may be true; persist `AllowFixSend=false`. 78/98 “zero callers” STALE. Census 18/8460 independent. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_150 CTraderFixSession 35=D / NewOrderSingle

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_150 |
| Slot | 150 |
| Purpose | Search `CTraderFixSession.cs` for `35=D` or `NewOrderSingle`. FAIL if live send exists. ALL Achiever+Starwave groups/traders; copy must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_150.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS.** Assigned file 135/135: `NewOrderSingle=0`, `35=D=0`; only outbound MsgType is `(35, "A")` Logon; one `WriteAsync`; sockets disposed. Product `*.cs`/`*.json`/`*.csproj` have 0 `35=D`. YoPips C++ `src` has 0 cTrader FIX senders. Copy hop const `NewOrderSingleImplemented=false` + persist `AllowFixSend:=false`. Residual: DI binds env `REAL_COPY_EXECUTION_ENABLED=true`; hosted logon no longer re-pins false. Census cited 18/8460. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500 slot 146 `IMTDeal.Volume` scale 10000

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_146 |
| Slot | 146 |
| Purpose | Confirm `IMTDeal.Volume` scale is **10000**, not hundredths, not `VolumeExt` 1e8. Goal: fetch ALL Achiever+Starwave groups/traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_146.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** |
| Verdict | **CONFIRMED.** Official `MTAPI_VOLUME_DIV=10000.0`; extractors copy `deal->Volume()` (0 `VolumeExt` calls). C# default `10_000`. E004 3/3 VolumeConverter tests Passed. D92 eval `ctor_default_Scale=10000`. Hundredths is a `mt5_types.h` comment bug. Census 18/8460 re-summed (08:42Z). `35=D` absent; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. Residual: DI binds env `REAL_COPY` (may be true). Risk to capital: **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_128 slot 128 REAL_COPY must stay false

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_128 |
| Slot | 128 |
| Purpose | Confirm `REAL_COPY_EXECUTION_ENABLED` must stay false. No `35=D` NewOrderSingle until risk/recon gates. Fetch ALL Achiever+Starwave groups/traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_128.md` |
| Product source modified | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| Verdict | **CONFIRMED_MUST_STAY_FALSE.** Law §41 / §68 **0/19** / §70 **0/14**. Product `35=D=0`; only outbound MsgType is `35=A`. Copy `NewOrderSingleImplemented`+`VenueReconciled` const false; persist `AllowFixSend=false`. Residual: `.env` L73 **true**; DI L41 now binds it; hosted no longer re-pins (W500_68/108 stale). Census 18/8460 read-only. YoPips `src` 0 cTrader senders. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_S055 dest-account ruin

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_S055 |
| Slot | S055 |
| Purpose | Ruin math for one retail Pepperstone dest vs copy-all / default RiskLimits / 70 same-side SHADOW. No product edit. No secrets. No live NewOrderSingle. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S055_ruin.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| Verdict | **DEST_RUIN_IF_SENT.** Copy-all EV −$154k; blocked tail −$242k; 5-lot / 10-net / 0.70 margin / $2,000 daily are blow-up caps; Evaluate is called with a zero book; dest is one retail login. Today dest PnL $0 by `SAFE_BY_ABSENCE`. Never flatten MT5 source. Profit = filter tail + 0.05 lot + shadow after costs. |

---

## 2026-08-18 — W500_RESEARCH_133 Api.csproj TFM vs MT5APIManager64

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_133 |
| Slot | 133 |
| Purpose | Check `Api.csproj` TargetFramework. `net8.0` without windows/x64 vs `MT5APIManager64` load. ALL Achiever+Starwave groups/traders. No live cTrader orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_133.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS.** API is `net8.0-windows` + `PlatformTarget` x64; restore `success: true`; trio in `bin\Debug\net8.0-windows\`; `bases/` 2027+9904 prove prior LoadLibrary. Isolated `net8.0` x64 can Initialize (R021). Product `net8.0` hosts still NU1201. Census 18/8460. `35=D` `SAFE_BY_ABSENCE`. Env REAL_COPY=true armed; sender missing. Risk to capital **NONE**. |

---

## 2026-08-18 — W500 slot 137 copy-flag defaults (`FEATURE_COPY` / `REAL_COPY`)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_137 |
| Slot | 137 |
| Purpose | Check `FEATURE_COPY_TRADING_ENABLED` and `REAL_COPY_EXECUTION_ENABLED` defaults. Fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_137.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** |
| Verdict | **PASS_NO_LIVE_SEND_ENV_ARMED.** FEATURE display/pipeline **ON** (`/api/settings` literal `true`; hosted shadow tick flag-blind). REAL_COPY architecture/POCO/worker-fallback still **false**, but lab `.env` L73 `=true` is **now bound** by `DependencyInjection` onto `LiveRuntimeStatus.RealCopyEnabled`; logon host no longer re-pins false (57/97/108 **STALE**). Catalog walk `GroupRequestArray("*")` + `GetAccountsAsync(null)` flag-blind (census 18/8460 prior). Product `35=D=0`; persist `AllowFixSend=false`; `NewOrderSingleImplemented=false`. YoPips 0 senders. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_130 CTraderFixSession 35=D / NewOrderSingle

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_130 |
| Slot | 130 |
| Purpose | Search `CTraderFixSession.cs` for `35=D` or `NewOrderSingle`. FAIL if live send exists. ALL Achiever+Starwave groups/traders; copy must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_130.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS.** Assigned file 135/135: `NewOrderSingle=0`, `35=D=0`; only outbound MsgType is `(35, "A")` Logon; one `WriteAsync`; sockets disposed. Product `*.cs`/`*.json`/`*.csproj` have 0 `35=D`. YoPips C++ `src` has 0 cTrader FIX senders. `NewOrderSingleImplemented` const false. Residual: `.env` `REAL_COPY_EXECUTION_ENABLED=true` and DI binds it; hosted service no longer re-pins false (W500_90/110 stale). Census cited 18/8460. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_145 UserGetByGroup pump-cache vs UserRequestArray

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_145 |
| Slot | 145 |
| Purpose | Confirm `UserGetByGroup` is pump-cache and `UserRequestArray` is the request path for ALL traders. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_145.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** `UserGetByGroup` = pump-cache (`PUMP_MODE_USERS`; absent on Admin). `UserRequestArray` = network; C# primary at `ReadAccountsForGroup` L223; cache fallback only on hard fail; empty → `UserLogins`. Census 18/8460 re-summed (08:42Z, not re-probed). `35=D` absent; `NewOrderSingleImplemented=false`. Env REAL_COPY may be true; sender missing. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_129 trade #3 EARLY_SCORE/SHADOW never auto LIVE

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_129 |
| Slot | 129 |
| Purpose | Confirm trade #3 is EARLY_SCORE/SHADOW never auto LIVE. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_129.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** `FromBaseline` reachable set `{INSUFFICIENT_DATA, EARLY_SCORE, WATCH, SHADOW, RISK_BLOCKED}` — no LIVE. `CanPromoteToLive => false`. Copy `SHADOW_ONLY`. `35=D` absent. `REAL_COPY` env-driven (may be true; no sender). Census 18/8460 (re-summed JSON). Risk to capital **NONE**. |

---

## 2026-08-18 — W500 slot 136 `GetTradersAsync` scores-only vs all `Mt5Accounts`

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_136 |
| Slot | 136 |
| Purpose | Check `EfDashboardQueries.GetTradersAsync` only scores vs all `Mt5Accounts`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_136.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_ALL_ACCOUNTS_NO_LIVE_SEND.** Driver is `foreach (var account in accounts)` L99 + left-join scores (A005 scores-only is stale). Catalog `*` + all users (prior 18/8460 re-summed this slot). Hosted score = `ListLoginsWithDealsAsync`. `35=D` absent. Residual: DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`; settings `FEATURE_COPY=true`; `/api/copy*` exists but SHADOW only (`NewOrderSingleImplemented=false`). Risk to capital **NONE** (`SAFE_BY_ABSENCE`). This slot did not live-attach. |

---

## 2026-08-18 — W500_RESEARCH_134 `LiveMt5Registration.HasRealPasswords` fail-closed

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_134 |
| Slot | 134 |
| Purpose | Check `LiveMt5Registration.HasRealPasswords` fail-closed. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_134.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_FAIL_CLOSED_DI.** Dual-AND + DI throw blocks empty / exact `<SECRET>` / `(a/c` / one-sided keys (no FakeMt5). After pass: Native ×2 + ALL groups/traders. Residuals: Ordinal case hole, dummy words, factory/probe bypass, 0 tests. Sibling 114 “RealCopyEnabled hardcoded false” is **stale** — DI binds env; `.env` is `true` (flag armed, **not** a sender). Product `35=D=0`; NOS `const false`; persist `AllowFixSend=false`. Census pin 18/8460 (08:42Z, not re-probed). Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_144 Starwave must connect direct (no proxy)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_144 |
| Slot | 144 |
| Purpose | Confirm Starwave must connect direct with no proxy. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_144.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Starwave `Connect(84.201.6.142:443)` with `ProxySet` skipped. C# hardcodes `ProxyEnabled=false`. Achiever HTTP hop is the other broker. Live census 10/1948 direct (total 18/8460 re-summed). `35=D` absent; `NewOrderSingleImplemented=false`. Residual: DI binds env `REAL_COPY_EXECUTION_ENABLED` and lab `.env` L73 is `true`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500 slot 142 YoPips `mt5_group_probe` (no password echo)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_142 |
| Slot | 142 |
| Purpose | Read YoPips `mt5_group_probe.cpp`. How does a proven probe enumerate all groups without echoing passwords? Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_142.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED_GROUPS_ONLY_NO_PASSWORD_ECHO.** C++ `mt5_group_probe` prints manager-visible group names via `GetAllGroups` (`GroupTotal`+`GroupNext`), never passwords (`spdlog` off; JSON has no secret keys). Traders are a sibling walk (`UserLogins`/`UserRequestArray`) already measured by `LiveBrokerProbe`: Achiever 8/6512, Starwave 10/1948. Probe exe absent (vcxproj generated, FileListAbsolute empty). No `35=D`. `NewOrderSingleImplemented=false`. This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_143 slot 143 (1012 + Achiever HTTP proxy)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_143 |
| Slot | 143 |
| Purpose | Confirm `MT_RET_AUTH_MANAGER_IPBLOCK=1012` and Achiever HTTP proxy `81.29.145.69:49527`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_143.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| Verdict | **CONFIRMED.** 1012 is the official Manager IP-block retcode. This LAN needs `ProxySet PROXY_HTTP 81.29.145.69:49527` for Achiever (else 1012). Starwave stays direct. Live census 18 groups / 8460 traders (re-summed; not re-attached). `35=D` absent (`SAFE_BY_ABSENCE`). Residual: DI now binds env `REAL_COPY_EXECUTION_ENABLED` and lab `.env` L73 is `true` (slots 3/63/83 hard-false pin is stale); sender still unimplemented. Risk to capital **NONE**. |

---

## 2026-08-18 — W500 slot 135 `DealIngestionService` `Take(200)` positions cap

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_135 |
| Slot | 135 |
| Purpose | Check `DealIngestionService` `Take(200)` positions cap. Fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_135.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_CAP_REMOVED.** Current `DealIngestionService` (146 lines) has zero `Take(`/`Skip`. Live path uses `GetGroupPositionsAsync("*")` or `foreach` all accounts. Only leftover `Take(200)` is `GET /api/trades` reconstructed rows. Probe JSON 18/8460/1984 re-summed. `35=D` absent (`SAFE_BY_ABSENCE`). Hosted scoring is `ListLoginsWithDealsAsync`. Residual: DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`; FIX host no longer pins false; sender still unimplemented. Risk to capital: **NONE**. |

---

## 2026-08-18 — W500 slot 117 copy-flag defaults (`FEATURE_COPY` / `REAL_COPY`)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_117 |
| Slot | 117 |
| Purpose | Check `FEATURE_COPY_TRADING_ENABLED` and `REAL_COPY_EXECUTION_ENABLED` defaults. Fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_117.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_NO_LIVE_SEND_ENV_ARMED.** Architecture/POCO REAL_COPY default still **false**. Local `.env` L73+L106 both **true**. DI now binds `REAL_COPY_EXECUTION_ENABLED` (`DependencyInjection.cs` L41). Logon re-pin **removed**. API FEATURE literal **true**. Fetch ALL flag-blind (prior census 18/8460). `35=D` absent; `NewOrderSingleImplemented=false`; `AllowFixSend` persisted false. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). Residual: next sender would see runtime armed on the API host. |

---

## 2026-08-18 — W500_RESEARCH_131 Program.cs DemoSeeder FakeMt5 10001 10002 dummy

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_131 |
| Slot | 131 |
| Purpose | Search `Program.cs` for DemoSeeder / FakeMt5 / 10001 / 10002 / dummy. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_131.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_HOST_NO_DUMMY.** Product `Program.cs` (API 160 + both workers + probe 86): 0 hits for DemoSeeder/FakeMt5/10001/10002/dummy. Startup seed is `BrokerCatalogSeed` only. DI fail-closed Native only. Census cited 8/6512 + 10/1948 = 18/8460; dummy logins 0 in live JSON. Residual Worker 4-login scorer. `35=D` SAFE_BY_ABSENCE; `NewOrderSingleImplemented=false`. Delta: DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true` (91/111 forced-false is stale). Risk to capital **NONE**. This slot did not live-attach. |

---

## 2026-08-18 — W500_RESEARCH_132 NativeMt5BrokerConnector GroupRequestArray / UserRequestArray

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_132 |
| Slot | 132 |
| Purpose | Search `NativeMt5BrokerConnector` for `GroupRequestArray` and `UserRequestArray`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_132.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS.** Primary walks are `GroupRequestArray("*")` L155 and per-group `UserRequestArray` L223. Ingest/`LiveBrokerProbe` use `GetAccountsAsync(null)`. Live census 8/6512 + 10/1948 = 18/8460 (08:42Z, re-summed, not re-probed). `35=D` absent; `NewOrderSingleImplemented=false`; `AllowFixSend=false`. Env `REAL_COPY=true` is bound by current DI but cannot emit a ticket (`SAFE_BY_ABSENCE`). Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_127 cTrader venue / cServer / 5211-5212 / no live send

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_127 |
| Slot | 127 |
| Purpose | Confirm cTrader is destination venue not LP. TargetCompID `cServer` case preserved. Ports 5211 QUOTE / 5212 TRADE SSL. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_127.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Venue ≠ LP. Live path `56=cServer` (no fold). QUOTE TLS 5211 / TRADE TLS 5212. Census 18/8460 (prior measure). `35=D` absent — `SAFE_BY_ABSENCE`. Dead leftover: API JSON `CSERVER`+5201/5202 unbound. Env `REAL_COPY=true` leftover; logon re-pins false. Risk to capital **NONE**. |

---

## 2026-08-18 — W500 slot 119 RiskEngine between CopyIntent and ExecutionIntent

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_119 |
| Slot | 119 |
| Purpose | Check whether `RiskEngine` sits between `CopyIntent` and `ExecutionIntent`. Goal: fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_119.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PARTIAL_HOP.** Architecture §4/§32/§39/§75 require CopyIntent → Evaluate → ExecutionIntent. Product: 1 Evaluate caller (`CopyTradingService.GenerateShadowIntentsAsync`); `RiskDecisionRecord` written with `AllowFixSend` forced false; `VenueReconciled`/`NewOrderSingleImplemented` const false; 0 `ExecutionIntent` writers; `PersistDemoShadowAsync` still bypasses Evaluate; no `35=D`. Catalog still ALL groups/users (prior 18/8460). Slots 19/59 “0 callers” stale. Risk to capital: **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_124 Starwave must connect direct (no proxy)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_124 |
| Slot | 124 |
| Purpose | Confirm Starwave must connect direct with no proxy. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_124.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live attach this pass | **No** |
| Verdict | **CONFIRMED.** Starwave `ProxyEnabled` hardcoded `false`; `MT5_STARWAVEFX_PROXY*` unread (0 hits in `src`/`apps`/`tools`). Do not `ProxySet` / do not reuse Achiever HTTP `81.29.145.69:49527`. Prior live census Starwave **10/1948 direct**. `35=D` absent (`SAFE_BY_ABSENCE`). Residual: DI now binds env `REAL_COPY_EXECUTION_ENABLED` and lab `.env` is `true`; sender still unimplemented. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_118 QuantityNormalizer lots ↛ FIX OrderQty

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_118 |
| Slot | 118 |
| Purpose | Check `QuantityNormalizer` never blindly converts MT5 lots to FIX `OrderQty`. ALL Achiever+Starwave groups/traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_118.md` |
| Product source modified | **No** |
| Secret values printed | **None** (flag booleans only) |
| Verdict | **FAIL as §38 converter; SAFE_BY_ABSENCE on the wire.** `Normalize(0.10,1,dest)=0.10` not `10.00`. Product now calls `Normalize(lots,0.05,GoldSpec)` (`1.00→0.05 ≠ 5.00 oz`). No `35=D`/`OrderQty`. `NewOrderSingleImplemented=false`. Env `REAL_COPY` may be true; persist `AllowFixSend=false`. 78/98 “zero callers” STALE. Capital risk **none**. Census 18/8460 independent. |

---

## 2026-08-18 — Switch FIX to Pepperstone DEMO 5328266

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Purpose | User supplied demo FIX host/account. Leave live 1369850. Enable copy pipeline without NewOrderSingle. |
| Host | demo-us-eqx-01.p.c-trader.com |
| Account | 5328266 |
| SenderCompID | demo.pepperstone.5328266 |
| Password | stored in `.env` only, not logged |
| Measured | QUOTE logon=True TRADE logon=True on account 5328266 |
| Live send | **still unimplemented** |
| Product source modified | Yes — env + seed/defaults/fallbacks point at demo |

---

## 2026-08-18 — W500 slot 126 `IMTDeal.Volume` scale 10000

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_126 |
| Slot | 126 |
| Purpose | Confirm `IMTDeal.Volume` scale is **10000**, not hundredths, not `VolumeExt` 1e8. Goal: fetch ALL Achiever+Starwave groups/traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_126.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Official `MTAPI_VOLUME_DIV=10000.0`; extractors copy `deal->Volume()` (0 `VolumeExt` calls). C# default `10_000`. E004 3/3 VolumeConverter tests Passed. Hundredths is a `mt5_types.h` comment bug. Slot 66 DI-false pin is stale: env flag may be true; `35=D` still absent (`NewOrderSingleImplemented=false`). Census 18/8460 prior. Risk to capital: **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_122 YoPips `mt5_group_probe` (no password echo)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_122 |
| Slot | 122 |
| Purpose | Read YoPips `mt5_group_probe.cpp`. How does a proven probe enumerate all groups without echoing passwords? Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_122.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED_GROUPS_ONLY_NO_PASSWORD_ECHO.** C++ `mt5_group_probe` prints manager-visible group names via `GetAllGroups` (`GroupTotal`+`GroupNext`), never passwords (`spdlog` off; JSON has no secret keys). Traders are a sibling walk (`UserLogins`/`UserRequestArray`) already measured by `LiveBrokerProbe`: Achiever 8/6512, Starwave 10/1948. Probe exe absent (FileListAbsolute empty). No `35=D`. `RealCopyEnabled=false`. This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — W500 slot 115 `DealIngestionService` `Take(200)` positions cap

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_115 |
| Slot | 115 |
| Purpose | Check `DealIngestionService` `Take(200)` positions cap. Fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_115.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_CAP_REMOVED.** Current `DealIngestionService` (146 lines) has zero `Take(`/`Skip`. Live path uses `GetGroupPositionsAsync("*")` or `foreach` all accounts. Only leftover `Take(200)` is `GET /api/trades` reconstructed rows. Probe JSON 18/8460/1984. `35=D` absent (`SAFE_BY_ABSENCE`). Hosted scoring is `ListLoginsWithDealsAsync`. Risk to capital: **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_113 Api.csproj TFM vs MT5APIManager64

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_113 |
| Slot | 113 |
| Purpose | Check `Api.csproj` TargetFramework. `net8.0` without windows/x64 vs `MT5APIManager64` load. ALL Achiever+Starwave groups/traders. No live cTrader orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_113.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS.** API is `net8.0-windows` + `PlatformTarget` x64; restore `success: true`; trio in `bin\Debug\net8.0-windows\`; `bases/` 2027+9904 prove prior LoadLibrary. Isolated `net8.0` x64 can Initialize (R021). Product `net8.0` hosts still NU1201. Census 18/8460. `35=D` `SAFE_BY_ABSENCE`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500 slot 116 `GetTradersAsync` scores-only vs all `Mt5Accounts`

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_116 |
| Slot | 116 |
| Purpose | Check `EfDashboardQueries.GetTradersAsync` only scores vs all `Mt5Accounts`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_116.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_ALL_ACCOUNTS_NO_LIVE_SEND.** Driver is `foreach (var account in accounts)` L99 + left-join scores (A005 scores-only is stale). Catalog `*` + all users (prior 18/8460 re-summed). Hosted score = `ListLoginsWithDealsAsync`. `35=D` absent. **New residual:** DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`; FIX host no longer pins false; `CopyTradingService` writes SHADOW only (`NewOrderSingleImplemented=false`). Risk to capital **NONE** (`SAFE_BY_ABSENCE`). This slot did not live-attach. |

---

## 2026-08-18 — W500 slot 106 `IMTDeal.Volume` scale 10000

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_106 |
| Slot | 106 |
| Purpose | Confirm `IMTDeal.Volume` scale is **10000**, not hundredths, not `VolumeExt` 1e8. Goal: fetch ALL Achiever+Starwave groups/traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_106.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Classic `Volume()` = `/10000` (`MTAPI_VOLUME_DIV`). Hundredths `/100` is a wrong `mt5_types.h` comment. `VolumeExt` `/1e8` unused (0 product calls). Extractors copy `Volume()`. Census 18/8460 prior. `35=D` `SAFE_BY_ABSENCE`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_123 slot 123 (1012 + Achiever HTTP proxy)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_123 |
| Slot | 123 |
| Purpose | Confirm `MT_RET_AUTH_MANAGER_IPBLOCK=1012` and Achiever HTTP proxy `81.29.145.69:49527`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_123.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** 1012 is the official Manager IP-block retcode. This LAN needs `ProxySet PROXY_HTTP 81.29.145.69:49527` for Achiever (else 1012). Starwave stays direct. Live census 18 groups / 8460 traders (re-summed; not re-attached). `35=D` absent (`SAFE_BY_ABSENCE`). Residual: DI now binds env `REAL_COPY_EXECUTION_ENABLED` and lab `.env` L73 is `true` (slots 3/63/83 hard-false pin is stale); sender still unimplemented. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_120 MT5APIManager.h request APIs work without pump

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_120 |
| Slot | 120 |
| Purpose | Read `MT5APIManager.h` `GroupRequestArray` / `UserRequestArray` / `UserLogins` / `PositionRequestByGroup` / `DealRequestByGroup`. Confirm request APIs work without pump. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_120.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_REQUEST_APIS_NO_PUMP.** Five Manager Request APIs are network RPCs (no `PUMP_MODE_DEALS`; no `DealGet`). C# uses them first; Connect retries `PUMP_MODE_NONE`. Census 18/8460/1984 (08:42Z, not re-probed). `35=D` absent; `RealCopyEnabled=false`. Risk to capital: **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_109 trade #3 EARLY_SCORE/SHADOW never auto LIVE

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_109 |
| Slot | 109 |
| Purpose | Confirm trade #3 is EARLY_SCORE/SHADOW never auto LIVE. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_109.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** `FromBaseline` reachable set `{INSUFFICIENT_DATA, EARLY_SCORE, WATCH, SHADOW, RISK_BLOCKED}` — no LIVE. `CanPromoteToLive => false`. Copy `SHADOW_ONLY`. `35=D` absent. Census 18/8460 (re-summed JSON). Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_114 LiveMt5Registration.HasRealPasswords fail-closed

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_114 |
| Slot | 114 |
| Purpose | Check `LiveMt5Registration.HasRealPasswords` fail-closed. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_114.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_FAIL_CLOSED_DI.** Dual-AND of `MT5_PASSWORD` + `MT5_STARWAVEFX_PASSWORD` via `IsSecret`; DI throws `Real MT5 passwords are required. Dummy/fake broker data is disabled.` before `CreateConnectors`. Native ×2 only; no Fake on throw path. Residuals: Ordinal `<secret>`/`(A/C` hole; dummy words pass; factory/probe ungated; 0 tests. After true, ingest is `GroupRequestArray("*")` + `GetAccountsAsync(null)`. Census cited 8/6512 + 10/1948 = 18/8460 (08:42Z, not re-probed). `RealCopyEnabled=false`; `CTraderFixSession` is `35=A` only. C++ AppConfig has no dual-password AND. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_108 slot 108 REAL_COPY must stay false

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_108 |
| Slot | 108 |
| Purpose | Confirm `REAL_COPY_EXECUTION_ENABLED` must stay false. No `35=D` NewOrderSingle until risk/recon gates. Fetch ALL Achiever+Starwave groups/traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_108.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED_MUST_STAY_FALSE.** Flag pinned false (POCO L35, DI L41, hosted L68, `.env` L73, `/api/settings`). Product `35=D=0`; only outbound MsgType is `35=A`. §68 **0/19**, §70 **0/14**. `RiskEngine.Evaluate` product callers=0. Recon API stub. YoPips `src` 0 cTrader senders. Census 18/8460 read-only. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_107 cTrader venue / cServer / 5211-5212 / no live send

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_107 |
| Slot | 107 |
| Purpose | Confirm cTrader is destination venue not LP. TargetCompID `cServer` case preserved. Ports 5211 QUOTE / 5212 TRADE SSL. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_107.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Venue ≠ LP. Live path `56=cServer` (no fold). QUOTE TLS 5211 / TRADE TLS 5212. Census 18/8460 (re-summed, not re-probed). `35=D` absent — `SAFE_BY_ABSENCE`. Dead leftover: API JSON `CSERVER`+5201/5202 unbound. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_112 NativeMt5BrokerConnector GroupRequestArray / UserRequestArray

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_112 |
| Slot | 112 |
| Purpose | Search `NativeMt5BrokerConnector` for `GroupRequestArray` and `UserRequestArray`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_112.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS.** Primary walks are `GroupRequestArray("*")` L155 and per-group `UserRequestArray` L223. Ingest/`LiveBrokerProbe` use `GetAccountsAsync(null)`. Live census 8/6512 + 10/1948 = 18/8460. `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_110 CTraderFixSession 35=D / NewOrderSingle

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_110 |
| Slot | 110 |
| Purpose | Search `CTraderFixSession.cs` for `35=D` or `NewOrderSingle`. FAIL if live send exists. ALL Achiever+Starwave groups/traders; copy must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_110.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS.** Assigned file 135/135: `NewOrderSingle=0`, `35=D=0`; only outbound MsgType is `(35, "A")` Logon; one `WriteAsync`; sockets disposed. Product `*.cs`/`*.json`/`*.csproj` have 0 `35=D`. YoPips C++ `src` has 0 cTrader FIX senders. `RealCopyEnabled` forced false. Census cited 18/8460. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_111 Program.cs DemoSeeder / FakeMt5 / 10001 / 10002 dummy

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_111 |
| Slot | 111 |
| Purpose | Search `Program.cs` for DemoSeeder / FakeMt5 / 10001 / 10002 / dummy. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_111.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_HOST_NO_DUMMY.** API+workers+probe `Program.cs` have 0 `DemoSeeder`/`FakeMt5`/`10001`/`10002`/`dummy` hits. Startup seed is `BrokerCatalogSeed` only. Residual: `DemoSeeder` tests + `Worker.cs` four-login scorer. Hosted score = `ListLoginsWithDealsAsync`. Prior census 18/8460. `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). This slot did not live-attach. |

---

## 2026-08-18 — W500 slot 99 RiskEngine between CopyIntent and ExecutionIntent

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_99 |
| Slot | 99 |
| Purpose | Check whether `RiskEngine` sits between `CopyIntent` and `ExecutionIntent`. Goal: fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_99.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **NO_HOP.** Architecture §4/§32/§39/§75 require CopyIntent → Evaluate → ExecutionIntent. Product: 0 Evaluate callers (definition + 5 unit facts only); `IRiskEngine` missing; only CopyIntent writer is `PersistDemoShadowAsync` (`SHADOW_ONLY`, no risk); 0 `ExecutionIntent` writers; no `35=D`. Catalog still ALL groups/users (prior 18/8460). Agrees slots 19/39/59/79. Risk to capital: **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500 cTrader profit path (500-agent workflow + 56 subagents)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Workflow | `ctrader-profit-path` (500 planned agents, budget 1024) at `.grok/workflows/ctrader-profit-path.rhai` |
| Subagents | 56 named explore/general-purpose slots S001–S056 |
| Purpose | How the Pepperstone cTrader account can be profitable: higher profit, lower loss. User also asked to connect and send. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_PROFIT_SYNTHESIS.md` + `P500_S*.md` + `P500_MANIFEST.tsv` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** |
| Live measure | 8463 accounts; Achiever scoring; Starwave scored 0; SHADOW all demo; XAU book ≈ −$154k; blocked ≈ −$242k; dest PnL $0; FIX LoggedOn; `REAL_COPY=false` |
| Verdict | **SEND_NOW_NEGATIVE_EV.** Connect is already true. Send is absent and must stay absent. Profit = filter left tail + tiny size + shadow after real quotes. Copy-all and scalp-copy lose. |

---

## 2026-08-18 — W500_RESEARCH_100 request APIs without pump

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_100 |
| Slot | 100 |
| Purpose | Read `MT5APIManager.h` `GroupRequestArray` / `UserRequestArray` / `UserLogins` / `PositionRequestByGroup` / `DealRequestByGroup`. Confirm request APIs work without pump. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_100.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Five APIs are network RPCs; pump optional (Admin MAIL/NEWS-only enum still has four of five; pool `Connect(...,0)` still calls `UserLogins`). C# request-first, no `_pumpEnabled` branch. Live census 18 groups / 8460 traders / 1984 pos (re-summed; not re-attached). `35=D` absent (`SAFE_BY_ABSENCE`). `RealCopyEnabled=false`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_103 slot 103 (1012 + Achiever HTTP proxy)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_103 |
| Slot | 103 |
| Purpose | Confirm `MT_RET_AUTH_MANAGER_IPBLOCK=1012` and Achiever HTTP proxy `81.29.145.69:49527`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_103.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** 1012 is the official Manager IP-block retcode. This LAN needs `ProxySet PROXY_HTTP 81.29.145.69:49527` for Achiever (else 1012). Starwave stays direct. Live census 18 groups / 8460 traders (re-summed; not re-attached). `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_101 YoPips Connect pump-none + proxy packing

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_101 |
| Slot | 101 |
| Purpose | Read YoPips `mt5_manager.cpp` Connect fallback to pump-none and proxy `IP:port` / `login:password`. ALL Achiever+Starwave groups/traders. No live cTrader orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_101.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED_WITH_GROUPS_CACHE_GAP.** Fallback `Connect(...,0)` exists. Proxy packs `address=IP:port` `auth=login:password`. Wrapper `pumpMode=0` remaps (omits GROUPS). `GetAllGroups` is cache-only. `UserLogins` is request-complete. YoPips `.env` `MT5_PROXY_ENABLED` unread (`IS_MT5_PROXY_ENABLED`). cTrader `35=D` absent; `REAL_COPY` false. Census 18/8460 prior. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_104 Starwave must connect direct (no proxy)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_104 |
| Slot | 104 |
| Purpose | Confirm Starwave must connect direct with no proxy. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_104.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Starwave `Connect(84.201.6.142:443)` with `ProxySet` skipped. C# hardcodes `ProxyEnabled=false`. Achiever HTTP hop is the other broker. Live census 10/1948 direct (total 18/8460 re-summed). `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_105 UserGetByGroup pump-cache vs UserRequestArray

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_105 |
| Slot | 105 |
| Purpose | Confirm `UserGetByGroup` is pump-cache and `UserRequestArray` is the request path for ALL traders. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_105.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** `UserGetByGroup` = pump-cache (`PUMP_MODE_USERS`; absent on Admin). `UserRequestArray` = network; C# primary at `ReadAccountsForGroup` L223; cache fallback only on hard fail; empty → `UserLogins`. Census 18/8460 (08:42Z, not re-probed). `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500 slot 102 YoPips `mt5_group_probe` (no password echo)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_102 |
| Slot | 102 |
| Purpose | Read YoPips `mt5_group_probe.cpp`. How does a proven probe enumerate all groups without echoing passwords? Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_102.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED_GROUPS_ONLY_NO_PASSWORD_ECHO.** C++ `mt5_group_probe` prints manager-visible group names via `GetAllGroups` (`GroupTotal`+`GroupNext`), never passwords (`spdlog` off; JSON has no secret keys). Traders are a sibling walk (`UserLogins`/`UserRequestArray`) already measured by `LiveBrokerProbe`: Achiever 8/6512, Starwave 10/1948. Probe exe absent (vcxproj generated, FileListAbsolute empty). No `35=D`. `RealCopyEnabled=false`. This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_98 QuantityNormalizer vs FIX OrderQty (slot 98)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_98 |
| Slot | 98 |
| Purpose | Check `QuantityNormalizer` never blindly converts MT5 lots to FIX `OrderQty`. Fetch ALL Achiever+Starwave groups/traders. Copy-to-cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_98.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **EXISTS_NEEDS_REFACTOR** as dest-grid floor; **MISSING** as `IQuantityConverter`. `Normalize(0.10,1,dest)=0.10` (G7/G10 FAIL). Zero product callers. No `35=D` / tag 38. `RealCopyEnabled=false`. Census 18/8460 independent of this class. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_93 Api.csproj TFM vs MT5APIManager64

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_93 |
| Slot | 93 |
| Purpose | Check `Api.csproj` TargetFramework. `net8.0` without windows/x64 vs `MT5APIManager64` load. ALL Achiever+Starwave groups/traders. No live cTrader orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_93.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS.** API is `net8.0-windows` + `PlatformTarget` x64; restore `success: true`; trio in `bin\Debug\net8.0-windows\`; `bases/` 2027+9904 prove prior LoadLibrary. Isolated `net8.0` x64 can Initialize (R021). Product `net8.0` hosts still NU1201. Census 18/8460. `35=D` `SAFE_BY_ABSENCE`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500 slot 96 `GetTradersAsync` scores-only vs all `Mt5Accounts`

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_96 |
| Slot | 96 |
| Purpose | Check `EfDashboardQueries.GetTradersAsync` only scores vs all `Mt5Accounts`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_96.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_ALL_ACCOUNTS_NO_LIVE_SEND.** Driver is `foreach (var account in accounts)` + left-join scores (A005 scores-only is stale). Catalog = `GroupRequestArray("*")` / `GetAccountsAsync(null)`. Hosted score = `ListLoginsWithDealsAsync` only (list still shows rest as `INSUFFICIENT_DATA`). Census 18/8460/1984 (08:42Z, not re-probed). `35=D` absent; `RealCopyEnabled=false`. Risk to capital: **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_87 cTrader venue / cServer / 5211-5212 / no live send

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_87 |
| Slot | 87 |
| Purpose | Confirm cTrader is destination venue not LP. TargetCompID `cServer` case preserved. Ports 5211 QUOTE / 5212 TRADE SSL. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_87.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Venue ≠ LP. Live path `56=cServer` (no fold). QUOTE TLS 5211 / TRADE TLS 5212. Census 18/8460 (prior measure). `35=D` absent — `SAFE_BY_ABSENCE`. Dead leftover: API JSON `CSERVER`+5201/5202 unbound. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_92 NativeMt5BrokerConnector GroupRequestArray / UserRequestArray

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_92 |
| Slot | 92 |
| Purpose | Search `NativeMt5BrokerConnector` for `GroupRequestArray` and `UserRequestArray`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_92.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS.** Primary walks are `GroupRequestArray("*")` L155 and per-group `UserRequestArray` L223. Ingest/`LiveBrokerProbe` use `GetAccountsAsync(null)`. Live census 8/6512 + 10/1948 = 18/8460. `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_90 CTraderFixSession 35=D / NewOrderSingle

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_90 |
| Slot | 90 |
| Purpose | Search `CTraderFixSession.cs` for `35=D` or `NewOrderSingle`. FAIL if live send exists. ALL Achiever+Starwave groups/traders; copy must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_90.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS.** Assigned file 135/135: `NewOrderSingle=0`, `35=D=0`; only outbound MsgType is `(35, "A")` Logon; one `WriteAsync`; sockets disposed. Product `*.cs`/`*.json`/`*.csproj` have 0 `35=D`. YoPips C++ `src` has 0 cTrader FIX senders. `RealCopyEnabled` forced false. Census cited 18/8460. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500 slot 95 `DealIngestionService` `Take(200)` positions cap

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_95 |
| Slot | 95 |
| Purpose | Check `DealIngestionService` `Take(200)` positions cap. Fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_95.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_CAP_REMOVED.** Current `DealIngestionService` (146 lines) has zero `Take(`/`Skip`. Live path uses `GetGroupPositionsAsync("*")` or `foreach` all accounts. Only leftover `Take(200)` is `GET /api/trades` reconstructed rows. Probe JSON 18/8460/1984. `35=D` absent (`SAFE_BY_ABSENCE`). Hosted scoring is `ListLoginsWithDealsAsync`. Risk to capital: **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_91 Program.cs DemoSeeder FakeMt5 10001 10002 dummy

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_91 |
| Slot | 91 |
| Purpose | Search `Program.cs` for DemoSeeder / FakeMt5 / 10001 / 10002 / dummy. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_91.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_HOST_NO_DUMMY.** Product `Program.cs` (API + both workers + probe): 0 hits for DemoSeeder/FakeMt5/10001/10002/dummy. Startup seed is `BrokerCatalogSeed` only. DI fail-closed Native only. Census cited 8/6512 + 10/1948 = 18/8460; dummy logins 0 in live JSON. Residual: `mt5-worker/Worker.cs` still scores `{10001,10002,10003,99001}`; hosted ingest scores `ListLoginsWithDealsAsync` only. `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). This slot did not live-attach. |

---

## 2026-08-18 — W500_RESEARCH_82 YoPips `mt5_group_probe` (no password echo)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_82 |
| Slot | 82 |
| Purpose | Read YoPips `mt5_group_probe.cpp`. How does a proven probe enumerate all groups without echoing passwords? Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_82.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED_GROUPS_ONLY_NO_PASSWORD_ECHO.** C++ `mt5_group_probe` prints manager-visible group names via `GetAllGroups` (`GroupTotal`+`GroupNext`), never passwords (`spdlog` off; JSON has no secret keys). Traders are a sibling walk (`UserLogins`/`UserRequestArray`) already measured by `LiveBrokerProbe`: Achiever 8/6512, Starwave 10/1948. Probe exe absent (FileListAbsolute empty). No `35=D`. `RealCopyEnabled=false`. This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — W500 slot 66 `IMTDeal.Volume` scale 10000

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_66 |
| Slot | 66 |
| Purpose | Confirm `IMTDeal.Volume` scale is **10000**, not hundredths, not `VolumeExt` 1e8. Goal: fetch ALL Achiever+Starwave groups/traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_66.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Official `MTAPI_VOLUME_DIV=10000.0`; extractors copy `deal->Volume()` (0 `VolumeExt` calls). C# default `10_000`. E004 3/3 VolumeConverter tests Passed. Hundredths is a `mt5_types.h` comment bug. `35=D` absent; `RealCopyEnabled=false`. Census 18/8460 prior. Risk to capital: **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_80 MT5APIManager.h request APIs work without pump

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_80 |
| Slot | 80 |
| Purpose | Read `MT5APIManager.h` `GroupRequestArray` / `UserRequestArray` / `UserLogins` / `PositionRequestByGroup` / `DealRequestByGroup`. Confirm request APIs work without pump. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_80.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_REQUEST_APIS_NO_PUMP.** Five Manager Request APIs are network RPCs (no `PUMP_MODE_DEALS`; no `DealGet`). C# uses them first; Connect retries `PUMP_MODE_NONE`. Census 18/8460/1984 (08:42Z, not re-probed). `35=D` absent; `RealCopyEnabled=false`. Risk to capital: **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_89 trade #3 EARLY_SCORE/SHADOW never auto LIVE

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_89 |
| Slot | 89 |
| Purpose | Confirm trade #3 is EARLY_SCORE/SHADOW never auto LIVE. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_89.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** `FromBaseline` reachable set `{INSUFFICIENT_DATA, EARLY_SCORE, WATCH, SHADOW, RISK_BLOCKED}` — no LIVE. `CanPromoteToLive => false`. Copy `SHADOW_ONLY`. `35=D` absent. Census 18/8460 (re-summed JSON). Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_78 QuantityNormalizer lots ↛ FIX OrderQty

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_78 |
| Slot | 78 |
| Purpose | Check `QuantityNormalizer` never blindly converts MT5 lots to FIX `OrderQty`. ALL Achiever+Starwave groups/traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_78.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **FAIL as §38 converter; SAFE_BY_ABSENCE on the wire.** `Normalize(0.10,1,dest)=0.10` not `10.00`. Zero product callers. No `35=D`/`OrderQty`. `RealCopyEnabled=false`. Capital risk **none**. Census 18/8460 independent. |

---

## 2026-08-18 — W500 slot 76 `GetTradersAsync` scores-only vs all `Mt5Accounts`

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_76 |
| Slot | 76 |
| Purpose | Check `EfDashboardQueries.GetTradersAsync` only scores vs all `Mt5Accounts`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_76.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_ALL_ACCOUNTS_NO_LIVE_SEND.** Driver is `foreach (var account in accounts)` L99 + left-join scores (A005 scores-only is stale). Catalog `*` + all users (prior 18/8460 re-summed). Hosted score = `ListLoginsWithDealsAsync`. No `35=D`; `RealCopyEnabled=false`. Risk to capital **NONE**. This slot did not live-attach. |

---

## 2026-08-18 — W500_RESEARCH_83 slot 83 (1012 + Achiever HTTP proxy)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_83 |
| Slot | 83 |
| Purpose | Confirm `MT_RET_AUTH_MANAGER_IPBLOCK=1012` and Achiever HTTP proxy `81.29.145.69:49527`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_83.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** 1012 is the official Manager IP-block retcode. This LAN needs `ProxySet PROXY_HTTP 81.29.145.69:49527` for Achiever (else 1012). Starwave stays direct. Live census 18 groups / 8460 traders (re-summed; not re-attached). `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_84 Starwave must connect direct (no proxy)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_84 |
| Slot | 84 |
| Purpose | Confirm Starwave must connect direct with no proxy. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_84.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Starwave `ProxyEnabled` hardcoded `false`; `MT5_STARWAVEFX_PROXY*` unread (0 hits in `src`/`apps`). Do not `ProxySet` / do not reuse Achiever HTTP `81.29.145.69:49527`. Prior live census Starwave **10/1948 direct**. `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE**. This slot did not live-attach. |

---

## 2026-08-18 — W500_RESEARCH_85 UserGetByGroup pump-cache vs UserRequestArray

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_85 |
| Slot | 85 |
| Purpose | Confirm `UserGetByGroup` is pump-cache and `UserRequestArray` is the request path for ALL traders. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_85.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** `UserGetByGroup` = pump-cache (`PUMP_MODE_USERS`; absent on Admin). `UserRequestArray` = network; C# primary at `ReadAccountsForGroup` L223; cache fallback only on hard fail; empty → `UserLogins`. Census 18/8460 (08:42Z, not re-probed). `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_71 Program.cs DemoSeeder / FakeMt5 / 10001 / 10002 dummy

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_71 |
| Slot | 71 |
| Purpose | Search `Program.cs` for DemoSeeder / FakeMt5 / 10001 / 10002 / dummy. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_71.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_HOST_NO_DUMMY.** API+workers+probe `Program.cs` have 0 `DemoSeeder`/`FakeMt5`/`10001`/`10002`/`dummy` hits. Startup seed is `BrokerCatalogSeed` only. Residual: `DemoSeeder` tests + `Worker.cs` four-login scorer. Hosted score = `ListLoginsWithDealsAsync` (slot 11 `ListLoginsAsync` stale). Prior census 18/8460. `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). This slot did not live-attach. |

---

## 2026-08-18 — W500 slot 68 REAL_COPY must stay false

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_68 |
| Slot | 68 |
| Purpose | Confirm `REAL_COPY_EXECUTION_ENABLED` must stay false. No `35=D` NewOrderSingle until risk/recon gates. Fetch ALL Achiever+Starwave groups/traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_68.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED_MUST_STAY_FALSE.** Flag pinned false (POCO L35, DI L41, hosted L68, `.env` L73, `/api/settings`). Product `35=D=0`; only outbound MsgType is `35=A`. §68 **0/19**, §70 **0/14**. `RiskEngine.Evaluate` product callers=0. Recon API stub. YoPips `src` 0 cTrader senders. Census 18/8460 read-only. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500 slot 75 `DealIngestionService` `Take(200)` positions cap

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_75 |
| Slot | 75 |
| Purpose | Check whether ingest still silently snapshots only the first 200 accounts' positions. Goal: fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_75.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_CAP_REMOVED.** Current `DealIngestionService` (146 lines) has zero `Take(`/`Skip`. Live path uses `GetGroupPositionsAsync("*")` or `foreach` all accounts. Only leftover `Take(200)` is `GET /api/trades` reconstructed rows. Probe JSON 18/8460/1984. `35=D` absent (`SAFE_BY_ABSENCE`). Hosted scoring is `ListLoginsWithDealsAsync`. Risk to capital: **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_67 cTrader venue / cServer / 5211-5212 / no live send

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_67 |
| Slot | 67 |
| Purpose | Confirm cTrader is destination venue not LP. TargetCompID `cServer` case preserved. Ports 5211 QUOTE / 5212 TRADE SSL. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_67.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Venue ≠ LP. Live path `56=cServer` (no fold). QUOTE TLS 5211 / TRADE TLS 5212. Census 18/8460 (prior measure). `35=D` absent — `SAFE_BY_ABSENCE`. Dead leftover: API JSON `CSERVER`+5201/5202 unbound. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_70 CTraderFixSession 35=D / NewOrderSingle

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_70 |
| Slot | 70 |
| Purpose | Search `CTraderFixSession.cs` for `35=D` or `NewOrderSingle`. FAIL if live send exists. ALL Achiever+Starwave groups/traders; copy must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_70.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS.** Assigned file 135/135: `NewOrderSingle=0`, `35=D=0`; only outbound MsgType is `(35, "A")` Logon; one `WriteAsync`; sockets disposed. Product `*.cs`/`*.json`/`*.csproj` have 0 `35=D`. YoPips C++ `src` has 0 cTrader FIX senders. `RealCopyEnabled` forced false. Census cited 18/8460. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_72 NativeMt5BrokerConnector GroupRequestArray / UserRequestArray

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_72 |
| Slot | 72 |
| Purpose | Search `NativeMt5BrokerConnector` for `GroupRequestArray` and `UserRequestArray`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_72.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS.** Primary walks are `GroupRequestArray("*")` L155 and per-group `UserRequestArray` L223. Ingest/`LiveBrokerProbe` use `GetAccountsAsync(null)`. Live census 8/6512 + 10/1948 = 18/8460. `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_63 slot 63 (1012 + Achiever HTTP proxy)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_63 |
| Slot | 63 |
| Purpose | Confirm `MT_RET_AUTH_MANAGER_IPBLOCK=1012` and Achiever HTTP proxy `81.29.145.69:49527`. Fetch ALL groups/traders. No live cTrader orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_63.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** 1012 is the official Manager IP-block retcode. This LAN needs `ProxySet PROXY_HTTP 81.29.145.69:49527` for Achiever (else 1012). Live census 18 groups / 8460 traders. `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_65 UserGetByGroup pump-cache / UserRequestArray ALL-traders

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | 65 |
| Agent | W500_RESEARCH_65 |
| Purpose | Confirm `UserGetByGroup` is pump-cache and `UserRequestArray` is the request path for ALL traders. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_65.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** SDK `UserGetByGroup` (h:672) is pump-cache (`PUMP_MODE_USERS`); `UserRequestArray` (h:410) is the request enumerator. C# `ReadAccountsForGroup` calls `UserRequestArray` first, cache `UserGetByGroup` only on hard fail, then `UserLogins`+`UserRequestByLogins`. Live probe: Achiever 8/6512 + Starwave 10/1948. `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_54 LiveMt5Registration.HasRealPasswords fail-closed

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_54 |
| Slot | 54 |
| Purpose | Check `LiveMt5Registration.HasRealPasswords` fail-closed. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_54.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_WITH_RESIDUALS.** Dual-AND + DI throw blocks empty / exact `<SECRET>` / `(a/c` / one-sided keys (no FakeMt5). Residuals: Ordinal case hole, dummy words, factory/LiveBrokerProbe bypass, 0 product tests. Census pin 18/8460. `35=D` `SAFE_BY_ABSENCE`; `RealCopyEnabled=false`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_61 YoPips Connect pump-none + proxy packing

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_61 |
| Slot | 61 |
| Purpose | Read YoPips `mt5_manager.cpp` Connect fallback to pump-none and proxy `IP:port` / `login:password`. ALL Achiever+Starwave groups/traders. No live cTrader orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_61.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED_WITH_GROUPS_CACHE_GAP.** Fallback `Connect(...,0)` exists. Proxy packs `address=IP:port` `auth=login:password`. Wrapper `pumpMode=0` remaps (omits GROUPS). `GetAllGroups` is cache-only. `UserLogins` is request-complete. YoPips `.env` `MT5_PROXY_ENABLED` unread (`IS_MT5_PROXY_ENABLED`). cTrader `35=D` absent; `REAL_COPY` false. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_64 Starwave must connect direct (no proxy)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_64 |
| Slot | 64 |
| Purpose | Confirm Starwave must connect direct with no proxy. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_64.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Starwave `Connect(84.201.6.142:443)` with `ProxySet` skipped. C# hardcodes `ProxyEnabled=false`. Achiever HTTP hop is the other broker. Live census 10/1948 direct (total 18/8460). `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_60 request APIs without pump

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_60 |
| Slot | 60 |
| Purpose | Read `MT5APIManager.h` `GroupRequestArray` / `UserRequestArray` / `UserLogins` / `PositionRequestByGroup` / `DealRequestByGroup`. Confirm request APIs work without pump. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_60.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Five APIs are network RPCs; pump optional (Admin MAIL/NEWS-only enum still has four of five; pool `Connect(...,0)` still calls `UserLogins`). C# request-first, no `_pumpEnabled` branch. Live census 18 groups / 8460 traders / 1984 pos. `35=D` absent (`SAFE_BY_ABSENCE`). `RealCopyEnabled=false`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500 slot 62 YoPips `mt5_group_probe` (no password echo)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_62 |
| Slot | 62 |
| Purpose | How a proven probe enumerates ALL groups without echoing passwords; ALL Achiever+Starwave groups/traders; cTrader copy must not send live orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_62.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED_GROUPS_ONLY_NO_PASSWORD_ECHO.** C++ `mt5_group_probe` prints manager-visible group names via `GetAllGroups` (`GroupTotal`+`GroupNext`), never passwords (`spdlog` off; JSON has no secret keys). Traders are a sibling walk (`UserLogins`/`UserRequestArray`) already measured by `LiveBrokerProbe`: Achiever 8/6512, Starwave 10/1948. Probe exe absent (vcxproj generated, FileListAbsolute empty). No `35=D`. `RealCopyEnabled=false`. This slot did not live-attach. Risk to capital **NONE**. |

---

## 2026-08-18 — W500 slot 59 RiskEngine between CopyIntent and ExecutionIntent

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_59 |
| Slot | 59 |
| Purpose | Check whether `RiskEngine` sits between `CopyIntent` and `ExecutionIntent`. Goal: fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_59.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **NO_HOP.** Architecture §4/§32/§39/§75 require CopyIntent → Evaluate → ExecutionIntent. Product: 0 Evaluate callers (definition + 5 unit facts only); `IRiskEngine` missing; only CopyIntent writer is `PersistDemoShadowAsync` (`SHADOW_ONLY`, no risk); 0 `ExecutionIntent` writers; no `35=D`. Catalog still ALL groups/users (prior 18/8460). Risk to capital: **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500 slot 57 copy-flag defaults (`FEATURE_COPY` / `REAL_COPY`)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_57 |
| Slot | 57 |
| Purpose | Check `FEATURE_COPY_TRADING_ENABLED` and `REAL_COPY_EXECUTION_ENABLED` defaults. Fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_57.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_DEFAULTS_FALSE_NO_LIVE_SEND.** Both flags default false. FEATURE is API literal / unused env. REAL_COPY is arch §41 floor + POCO false + DI/logon pin; worker reads a different key and only logs. Fetch is flag-blind. `35=D` absent (`SAFE_BY_ABSENCE`). Risk to capital: **NONE**. |

---

## 2026-08-18 — W500 slot 55 `DealIngestionService` `Take(200)` positions cap

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_55 |
| Slot | 55 |
| Purpose | Check whether ingest still silently snapshots only the first 200 accounts' positions. Goal: fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_55.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_CAP_REMOVED.** Current `DealIngestionService` (146 lines) has zero `Take(`/`Skip`. Live path uses `GetGroupPositionsAsync("*")` or `foreach` all accounts. Only leftover `Take(200)` is `GET /api/trades` reconstructed rows. Probe JSON 18/8460/1984. `35=D` absent (`SAFE_BY_ABSENCE`). Hosted scoring is `ListLoginsWithDealsAsync`. Risk to capital: **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_58 QuantityNormalizer vs FIX OrderQty (slot 58)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_58 |
| Slot | 58 |
| Purpose | Check `QuantityNormalizer` never blindly converts MT5 lots to FIX `OrderQty`. Fetch ALL Achiever+Starwave groups/traders. Copy-to-cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_58.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **EXISTS_NEEDS_REFACTOR** as dest-grid floor; **MISSING** as `IQuantityConverter`. `Normalize(0.10,1,dest)=0.10` (G7/G10 FAIL). Zero product callers. No `35=D` / tag 38. `RealCopyEnabled=false`. Census 18/8460 independent of this class. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_49 trade #3 EARLY_SCORE/SHADOW never auto LIVE

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | 49 |
| Agent | W500_RESEARCH_49 |
| Purpose | Confirm trade #3 is EARLY_SCORE/SHADOW never auto LIVE. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_49.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** `FromBaseline` reachable set `{INSUFFICIENT_DATA, EARLY_SCORE, WATCH, SHADOW, RISK_BLOCKED}` — no LIVE. `CanPromoteToLive => false`. Copy `SHADOW_ONLY`. `35=D` absent. Census 18/8460 (re-summed JSON). Risk to capital **NONE**. |

---

## 2026-08-18 — W500 slot 34 `LiveMt5Registration.HasRealPasswords` fail-closed

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_34 |
| Slot | 34 |
| Purpose | Check `LiveMt5Registration.HasRealPasswords` fail-closed. Goal: fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_34.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_FAIL_CLOSED_DI.** `AddTraderIntelligence` throws unless both password keys pass `IsSecret` (non-whitespace, no exact `<SECRET>`, no `(a/c`); then registers Native ×2 only. Residual: `IsSecret` is case-sensitive / template words pass; `CreateConnectors*` ungated; LiveBrokerProbe whitespace-only; 0 tests. `35=D` absent (`SAFE_BY_ABSENCE`). `RealCopyEnabled` forced false. Risk to capital: **NONE**. |

---

## 2026-08-18 — W500 slot 56 `GetTradersAsync` scores-only vs all `Mt5Accounts`

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_56 |
| Slot | 56 |
| Purpose | Check `EfDashboardQueries.GetTradersAsync` only scores vs all `Mt5Accounts`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_56.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_ALL_ACCOUNTS_NO_LIVE_SEND.** Driver is `foreach (var account in accounts)` + left-join scores (A005 scores-only is stale). Catalog = `GroupRequestArray("*")` / `GetAccountsAsync(null)`. Hosted score = `ListLoginsWithDealsAsync` only (list still shows rest as `INSUFFICIENT_DATA`). Census 18/8460/1984 (08:42Z, not re-probed). `35=D` absent; `RealCopyEnabled=false`. Risk to capital: **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_33 Api.csproj TFM vs MT5APIManager64

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_33 |
| Slot | 33 |
| Purpose | Check `Api.csproj` TargetFramework. `net8.0` without windows/x64 vs `MT5APIManager64` load. ALL Achiever+Starwave groups/traders. No live cTrader orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_33.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS.** API is `net8.0-windows` + x64; restore `success: true`; trio in `bin\Debug\net8.0-windows\`. Isolated `net8.0` x64 can still LoadLibrary (R021); product `net8.0` host cannot ProjectReference Mt5 (NU1201). Workers+Integration still fail restore. Census 18/8460. `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_44 Starwave must connect direct (no proxy)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_44 |
| Slot | 44 |
| Purpose | Confirm Starwave must connect direct with no proxy. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_44.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Starwave `ProxyEnabled` hardcoded `false`; `MT5_STARWAVEFX_PROXY_ENABLED` unread. Do not `ProxySet` / do not reuse Achiever HTTP `81.29.145.69:49527`. Prior live census Starwave **10/1948 direct**. `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE**. This slot did not live-attach. |

---

## 2026-08-18 — W500_RESEARCH_52 NativeMt5BrokerConnector GroupRequestArray / UserRequestArray

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_52 |
| Slot | 52 |
| Purpose | Search `NativeMt5BrokerConnector` for `GroupRequestArray` and `UserRequestArray`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_52.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS.** Primary walks are `GroupRequestArray("*")` L155 and per-group `UserRequestArray` L223. Ingest/`LiveBrokerProbe` use `GetAccountsAsync(null)`. Live census 8/6512 + 10/1948 = 18/8460. `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_47 cTrader venue / cServer / 5211-5212 / no live send

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | 47 |
| Agent | W500_RESEARCH_47 |
| Purpose | Confirm cTrader is destination venue not LP. TargetCompID `cServer` case preserved. Ports 5211 QUOTE / 5212 TRADE SSL. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_47.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Venue ≠ LP. Live path `56=cServer` (no fold). QUOTE TLS 5211 / TRADE TLS 5212. Census 18/8460 (prior measure). `35=D` absent — `SAFE_BY_ABSENCE`. Dead leftover: API JSON `CSERVER`+5201/5202 unbound. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_36 GetTradersAsync scores-only vs all Mt5Accounts

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_36 |
| Slot | 36 |
| Purpose | Check `EfDashboardQueries.GetTradersAsync` only scores vs all `Mt5Accounts`. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_36.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED_ALL_MT5ACCOUNTS_NO_35D.** Driver is `foreach (var account in accounts)` + left-join scores (A005 scores-only is stale). Catalog 18/8460 last measure; `/api/traders` listed 8460. Auto-score is `ListLoginsWithDealsAsync` (slot-16 “score every login” stale). `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_50 CTraderFixSession 35=D / NewOrderSingle

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_50 |
| Slot | 50 |
| Purpose | Search `CTraderFixSession.cs` for `35=D` or `NewOrderSingle`. FAIL if live send exists. ALL Achiever+Starwave groups/traders; copy must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_50.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS.** Assigned file 135/135: `NewOrderSingle=0`, `35=D=0`; only outbound MsgType is `(35, "A")` Logon; one `WriteAsync`; sockets disposed. Product `*.cs`/`*.json`/`*.csproj` have 0 `35=D`. YoPips C++ `src` has 0 cTrader FIX senders. `RealCopyEnabled` forced false. Census cited 18/8460. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_38 QuantityNormalizer lots ↛ FIX OrderQty

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_38 |
| Slot | 38 |
| Purpose | Check `QuantityNormalizer` never blindly converts MT5 lots to FIX `OrderQty`. ALL Achiever+Starwave groups/traders. Copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_38.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **FAIL as §38 converter; SAFE_BY_ABSENCE on the wire.** `Normalize(0.10,1,dest)=0.10` not `10.00`. Zero product callers. No `35=D`/`OrderQty`. `RealCopyEnabled=false`. Capital risk **none**. Census 18/8460 independent. |

---

## 2026-08-18 — W500 slot 35 `DealIngestionService` `Take(200)` positions cap

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_35 |
| Slot | 35 |
| Purpose | Check whether ingest still silently snapshots only the first 200 accounts' positions. Goal: fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_35.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **PASS_CAP_REMOVED.** Current `DealIngestionService` (145 lines) has zero `Take(`/`Skip`. Live path uses `GetGroupPositionsAsync("*")` or `foreach` all accounts. Only leftover `Take(200)` is `GET /api/trades` reconstructed rows. Probe JSON 18/8460/1984. `35=D` absent (`SAFE_BY_ABSENCE`). Hosted scoring is `ListLoginsWithDealsAsync` (W500_15 “all logins” is stale). Risk to capital: **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_1 YoPips Connect pump-none + proxy packing

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_1 |
| Slot | 1 |
| Purpose | Read YoPips `mt5_manager.cpp` Connect fallback to pump-none and proxy `IP:port` / `login:password`. ALL Achiever+Starwave groups/traders. No live cTrader orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_1.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED_WITH_GROUPS_CACHE_GAP.** Fallback `Connect(...,0)` exists. Proxy packs `address=IP:port` `auth=login:password`. Wrapper `pumpMode=0` remaps (omits GROUPS). `GetAllGroups` is cache-only. `UserLogins` is request-complete. cTrader `35=D` absent; `REAL_COPY` false. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_3 slot 3 (1012 + Achiever HTTP proxy)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | 3 |
| Agent | W500_RESEARCH_3 |
| Purpose | Confirm `MT_RET_AUTH_MANAGER_IPBLOCK=1012` and Achiever HTTP proxy `81.29.145.69:49527`. Fetch ALL groups/traders. No live cTrader orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_3.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** 1012 is the official Manager IP-block retcode. This LAN needs `ProxySet PROXY_HTTP 81.29.145.69:49527` for Achiever (else 1012). Live census 18 groups / 8460 traders. `35=D` absent; `RealCopyEnabled=false`. |

---

## 2026-08-18 — W500 slot 22 YoPips `mt5_group_probe` (no password echo)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_22 |
| Slot | 22 |
| Purpose | How a proven probe enumerates ALL groups without echoing passwords; ALL Achiever+Starwave groups/traders; cTrader copy must not send live orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_22.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | C++ `mt5_group_probe` prints manager-visible group names via `GetAllGroups` (`GroupTotal`+`GroupNext`), never passwords. Traders are a sibling walk (`UserLogins`/`UserRequestArray`) already measured by `LiveBrokerProbe`: Achiever 8/6512, Starwave 10/1948. No `35=D`. `RealCopyEnabled=false`. This slot did not live-attach. |

---

## 2026-08-18 — W500_RESEARCH_27 cTrader venue / cServer / 5211-5212 / no live send

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | 27 |
| Purpose | Confirm cTrader is destination venue not LP. TargetCompID `cServer` case preserved. Ports 5211 QUOTE / 5212 TRADE SSL. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_27.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Venue ≠ LP. Live path `56=cServer` (no fold). QUOTE TLS 5211 / TRADE TLS 5212. Census 18/8460 (prior measure). `35=D` absent — `SAFE_BY_ABSENCE`. Dead leftover: API JSON `CSERVER`+5201/5202 unbound. |

---

## 2026-08-18 — Live Manager all-groups/all-traders (measured)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T08:45Z |
| Purpose | Fetch ALL Achiever + Starwave groups and manager traders. No dummy seed. Copy-to-cTrader without live loss. |
| Artifact | `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` + `LIVE_GROUPS_AND_TRADERS.json` |
| Orchestration | Workflow `live-mt5-all-groups` 500 agents + parent review wave |
| Product source modified | **Yes** — Native Manager connector, catalog-first ingest, DemoSeeder removed from API startup |
| Secret values printed | **None** |
| Verdict | **LIVE CENSUS PROVEN.** Achiever 8 groups / 6512 traders (proxy). Starwave 10 groups / 1948 traders (direct). FIX QUOTE+TRADE logon **true** after tag 553=account id. **NewOrderSingle still off.** |

---

## 2026-08-18 — R005 secret locations (`MT5_PASSWORD` in `.env` / `appsettings`)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T08:26:34Z / this pass ~08:28Z |
| Agent | R005 |
| Purpose | Search `D:\Prop` and sibling folders for `.env` / `appsettings` containing `MT5_PASSWORD`. Path + PLACEHOLDER vs PRESENT only. Do not write the password. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\R005_secret_locations.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **3 files have the key.** `D:\Prop\.env` **PRESENT**. `D:\Prop\mt5-sdk\.env.example` **PLACEHOLDER**. `D:\Projects\YoPips\Backend\C++ Backend PropFirm\.env` **PRESENT**. No `appsettings*` contains `MT5_PASSWORD`. |

---

## 2026-08-18 — R030 official cTrader FIX headers

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:56:36+05:30 |
| Agent | R030 |
| Purpose | Official cTrader: SenderSubID=QUOTE/TRADE, TargetCompID=cServer, SSL 5211/5212. Password not a real secret. Do not invent one. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\R030_fix_headers.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **Official form (screenshot): SenderSubID=QUOTE/TRADE, TargetCompID=cServer, SSL 5211/5212.** RoE qualifier is **tag 57**, not 50; tag 50 must be QUOTE when 57=QUOTE. Options default `cServer` + ports 5211/5212. Options `SenderSubId` still empty. Process password **ABSENT**. Live Logon **NOT PROVEN**. |

---

## 2026-08-18 — R003 refuse Fake when USE_REAL_MT5=true (plan only)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | R003 |
| Purpose | Read DemoSeeder + DI. Plan how to refuse Fake connector when `USE_REAL_MT5=true`. Do not modify product source. |
| Artifact | `reports/swarm/20260818/R003_no_fake.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| DI SHA-256 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` |
| Seeder SHA-256 | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` |
| Fake SHA-256 | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` |
| Verdict | **Gate MISSING.** Product C# has 0 `USE_REAL_MT5` hits. Both graphs always `DemoBrokerFactory.CreateDefault()`. Gitignored `.env` has `USE_REAL_MT5=true` but hosts do not load it; process env ABSENT. Plan: fail-closed at registration, seeder, and type-check; no real implementor ⇒ throw at start. Not a copy license. G01 still FAIL. |

---

## 2026-08-18 — E037 FIX host in options (no password)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:52:12+05:30 |
| Agent | E037 |
| Purpose | FIX host in options. No password. Write report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E037_fixhost.md` |
| Product source modified | **No** |
| Options SHA-256 | `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` (`CTraderFixOptions.cs`, 2344 B) |
| Verdict | **`Host=live-us-eqx-01.p.c-trader.com`. `Password=""`.** Unbound. Process/user-secrets password **absent**. API JSON `fix.ctrader.com` is a dead unofficial alias. Live `/api/fix/sessions` shows seeder host, `loggedOn=false`. Logon **NOT PROVEN**. |

---

## 2026-08-18 — R010 C# Manager API connect / groups / users / deals

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | R010 |
| Purpose | Read `BalanceExample.NET` + `SimpleManager`. Document how C# connects and lists groups, users, deals. Method names only. No product source edits. Never copy passwords. |
| Artifact | `D:\Prop\reports\swarm\20260818\R010_csharp_manager.md` |
| Product source modified | **No** |
| Vendor source modified | **No** |
| Passwords copied | **None** |
| Verdict | `BalanceExample.NET` is C# Manager API (`SMTManagerAPIFactory` → `CIMTManagerAPI.Connect` + `PUMP_MODE_FULL`). `SimpleManager` is **C++**, not C#. Neither sample lists groups. Users = one `UserRequest`. Deals = one-login `DealRequest` (C# only). List APIs exist on `CIMTManagerAPI` (`GroupTotal`/`GroupNext`/`GroupRequestArray`, `UserLogins`/`UserRequestArray`, `DealRequestByGroup`). Web API is a separate C# surface (`GroupTotal`/`UserLogins`/`DealGetPage`). |

---

## 2026-08-18 — R006 how to build `mt5_group_probe` on Windows

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:53:51+05:30 |
| Agent | R006 |
| Purpose | Read `mt5-sdk/CMakeLists.txt` and document how to build `mt5_group_probe` on Windows. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\R006_cmake.md` |
| Product source modified | **No** |
| CMakeLists SHA-256 | `98345532CA0D33888E919D14F680B933EB60C6C2A2CE85DBBF1F0D05419719E9` (173 lines; MATCH D66) |
| Host | CMake 4.4.0; VS Build Tools 2022 (14.44.35207); vcpkg `C:\tools\vcpkg` (`nlohmann-json`/`spdlog`/`curl` `x64-windows`) |
| Verdict | Target is **opt-in + WIN32 only**. Recipe: `-G "Visual Studio 17 2022" -A x64` + vcpkg toolchain + `-DMT5SDK_BUILD_PROBES=ON`, then `--config Release --target mt5_group_probe`. README first `cmake -B` snippet omits the flag. Exe **not** built this pass. |

---

## 2026-08-18 — E033 stale API process vs quoteHealthy true

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:55+05:30 / 2026-08-18T08:21:31Z / reconfirm 13:53:34+05:30 |
| Agent | E033 |
| Purpose | Old API still reports `quoteHealthy` true? Restart needed? Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E033_stale_api.md` |
| Product source modified | **No** |
| Live process | `:5000` pid **54468** (parent **53816** `dotnet run --no-launch-profile`) started **13:42:16+05:30** |
| Loaded Infrastructure.dll | `EB43953E…` @ 13:40:18 (`apps/api/bin`) |
| src/Infrastructure/bin | `63C78E11…` @ 13:48:16 — **not loaded** |
| Live `GET /api/overview` | `quoteHealthy=false`, `tradeHealthy=false`, `mt5Healthy=true` |
| Live FIX rows | QUOTE+TRADE `Disconnected`; LastError admits no socket; seed clock = process start |
| Verdict | **Assigned `true` is STALE as HTTP** (this pid seeded honest `Disconnected` at 13:42:16; same false as D77/E016/E031). **Restart still needed** for DLL/InMemory freshness. E033 did **not** recycle. |

---

## 2026-08-18 — E032 Vite SPA routes return HTTP 200

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:51:00+05:30 / HTTP Date 2026-08-18T08:21:45Z |
| Agent | E032 |
| Purpose | Vite routes returned 200. Write `E032_pages_200.md`. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E032_pages_200.md` |
| Capture | `D:\Prop\reports\swarm\20260818\_tmp_e032\` |
| Product source modified | **No** |
| Listener | `127.0.0.1:3000` node pid **49100** `vite.js --host 127.0.0.1 --port 3000` |
| `App.tsx` SHA-256 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` |
| Shell SHA-256 | `26270EBBA1F0ED45E5B2362F38589802C1DEB612C59180AD292F7C87E9DF4C6F` (624 B) |
| Verdict | **16/16 destinations GET 200.** HEAD sampled 200. 15/15 page modules 200 JS. Unmapped `/login` `/models` `/api/overview` also 200 HTML (SPA fallback). Only `/favicon.ico` 404. 200 ≠ painted widgets. |

---

## 2026-08-18 — E030 honest live vs demo scorecard

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:16+05:30 (hashes); HTTP 13:51:13+05:30 |
| Agent | E030 |
| Purpose | Write an honest live vs demo scorecard. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E030_honesty.md` |
| Product source modified | **No** |
| Live API | `127.0.0.1:5000` HTTP 200: 4 accounts, 2 SHADOW, 1 RISK_BLOCKED, 0 LIVE, `mt5Healthy` true (lie), FIX `Disconnected`, `realCopyEnabled` false, `shadowPnl` 248.20 (Σ slip) |
| Workers | **Not running.** InMemory book is API-local. |
| Verdict | **Demo YES / Live NO.** §69 accepted **0/12** (demo shape 7/12). §68 **0/19**. §70 **0/14**. Send **SAFE_BY_ABSENCE**. `CanPromoteToLive` false. |

---

## 2026-08-18 — E038 settings featureFlags REAL_COPY false

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:51:56+05:30 / 2026-08-18T08:22:14Z |
| Agent | E038 |
| Purpose | Settings `featureFlags` `REAL_COPY` false. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E038_flag_api.md` |
| Product source modified | **No** |
| Live GET | `http://127.0.0.1:5000/api/settings` **200** `featureFlags.REAL_COPY_EXECUTION_ENABLED=false` (literal in `Program.cs` L45; SHA `61B1E0D1…`) |
| Writes | PUT/PATCH/POST/DELETE `/api/settings` **405** `Allow=GET`; `/api/v1/settings*` **404** |
| Dead twin | `SettingsController` SHA `B19274DC…` unmapped; `LiveCopyEnabled` ≠ architecture name |
| Verdict | Display floor is **false** and correct vs §41. Not a binder. Not a send gate. Do not MapControllers the Redis PUT. |

---

## 2026-08-18 — E039 22 skipped conversion tests

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:51:43+05:30 |
| Agent | E039 |
| Purpose | 22 skipped conversion tests. Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E039_skipped.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Test SHAs | `SourceDestinationQuantityConversionTests` `AA1FA307…` (7344 B / 184); `QuantityNormalizerStepMinMaxTests` `63D2691D…` (5174 B / 162) |
| SUT SHA-256 | `QuantityNormalizer` `B6CC53E8…` (1041 B / 31; unchanged vs D18) |
| `dotnet test` | conversion filter **33 passed / 0 failed / 22 skipped / 55 total** (exit 0) |
| Verdict | **All 22 unit skips are A43 conversion backlog.** 21 = missing `IQuantityConverter` (`Assert.Fail` / `BeFalse`); 1 = E23 raw `MaxQuantity` 5.09 vs FloorToStep 5.00. Passing Facts lock `0.10 → 0.10` (want 10.00). G7/G10 FAIL. Do not un-skip first. |

---

## 2026-08-18 — E019 `BaselineScorerTests` coverage

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:49:27+05:30 |
| Agent | E019 |
| Purpose | List `BaselineScorerTests`. Write coverage inventory vs SUT. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E019_score_cov.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Tests SHA-256 | `61E34A07D76B104CF5D8B818242104522A8B59D12422C5EF4555C2447308D408` (2414 B, 74 lines; untracked) |
| SUT SHA-256 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` (8143 B; unchanged vs D34) |
| `dotnet test` | **3 passed / 0 failed / 0 skipped** (0.3819 s) |
| Verdict | Complete list: `Two_trades_remain_insufficient`; `Three_disciplined_winners_go_to_shadow_not_live`; `Martingale_after_losses_is_risk_blocked`. **7 asserts.** FeatureSnapshot **1/18**. Numeric scores **0/3**. Reachable states **3/5**. A89 scoring/FSM classes **0/21**. Smoke, not A22. |

---

## 2026-08-18 — E018 TradeReconstructionTests inventory + coverage

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:08+05:30 |
| Agent | E018 |
| Purpose | List `TradeReconstructionTests`. Inventory coverage vs A21 / §14–15 / §60 / A89. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E018_recon_cov.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Subject SHA-256 | `CB223DDE3D8FC90BB39C15C8369640B6164A09B7FB30523BF40D8A0BA8E78B9D` (`TradeReconstructionTests.cs`, 4895 bytes) |
| SUT SHA-256 | `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` (`TradeReconstructor.cs`, 12768 bytes) |
| Measurement | `dotnet test …~TradeReconstructionTests` → **6/6 passed** (smoke) |
| Verdict | **FAIL / INSUFFICIENT.** 6 facts, 26 asserts, 0/25 A21 bit-for-bit, F17 cousin only, 1/22 A89 classes. D33 5-fact census is stale. |

---

## 2026-08-18 — E024 canceled position excluded from first-3 (helper only)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:43+05:30 |
| Agent | E024 |
| Purpose | Answer: is a canceled position excluded from first-3? Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E024_first3.md` |
| Product source modified | **No** |
| Reconstructor SHA-256 | `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` (unchanged vs D72/D73) |
| Unit fact SHA-256 | `CB223DDE3D8FC90BB39C15C8369640B6164A09B7FB30523BF40D8A0BA8E78B9D` |
| Eval | `D:\Prop\reports\swarm\20260818\_tmp_e024_first3\stdout.txt` (9361 B, SHA `26BABB7F…`) |
| `dotnet test` | cancel/first-3/balance/rollover/client filter **5 passed / 0 failed** |
| Verdict | **Helper YES / production NO.** Extra-ticket 13/14 dirties that `position_id` (`EligibleForFirstThree=false`); `CountCompletedXauUsdTrades` drops it (UNIT helper 2 / false). Score + dashboard + persist ignore the flag (UNIT/M5/SELL_CXL score 3 / true / `SHADOW`; DASH highlights dirty pos 3). Official 0→13 hidden by first-write-wins. C31 C9 / A83 §0 **stale**. |

---

## 2026-08-18 — E026 `/api/health` mapping + demo vs live wording

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:49:57+05:30 |
| Agent | E026 |
| Purpose | Read `/api/health` mapping. Demo vs live wording. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E026_health.md` |
| Product source modified | **No** |
| `Program.cs` SHA-256 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` |
| Verdict | **Anonymous hardcoded inventory at `Program.cs` L26–33. Not a probe. Not A26/A63. Demo-admit + live-deny strings; Achiever/DB still `healthy: true`; QUOTE/redis `false`; `outboxBacklog` literal `0`. Live Manager / live TLS NOT PROVEN.** |

---

## 2026-08-18 — E009 GetTraderDetailAsync + TraderDetailPage vs §51/A93

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:12+05:30 |
| Agent | E009 |
| Purpose | Read `GetTraderDetailAsync` and `TraderDetailPage`. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E009_detail.md` |
| Product source modified | **No** |
| Query SHA-256 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` (`EfDashboardQueries.cs` 8708 B / 205 lines) |
| Page SHA-256 | `C849449B6B76E6E4147AD2503DF00FD5E101C5B5D05ADB7E05708130A8556EB2` (`TraderDetailPage.tsx` 2402 B / 56 lines, untracked) |
| Live HTTP | `GET /api/traders/ACHIEVER/10001` 200 header+3 first-three; miss 99999 = **200 `null`** (not 404/204); `achiever/10001` header + **empty trades** |
| Verdict | **Chrome YES / §51 NO.** Wrapper around A92 row + unbounded `isFirstThree` dump. Page: 8 chips + 4-col table. **0/13** A93 roots, **~2/16** §51 blocks, **1/16** T-tests (T9). D39 204 claim stale. |

---

## 2026-08-18 — E025 DashboardLayout nav vs pages

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:14+05:30 |
| Agent | E025 |
| Purpose | List `DashboardLayout` nav vs `pages/`. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E025_nav.md` |
| Product source modified | **No** |
| Layout SHA-256 | `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` (1854 B, 44 CRLF; unstaged vs HEAD) |
| App.tsx SHA-256 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` (2062 B, 42 CRLF; unstaged) |
| Verdict | **14/14** nav `to`s join a routed page file. **15** pages; Trader Detail is the only page without a sidebar row (A62-correct). Models + Login still absent. **7/14** labels abbreviated vs §46. `/groups` ≠ A26 `/mt5-groups`. HEAD nav is **12** items and `pages/` is not in git. |

---

## 2026-08-18 — E004 test projects passing vs skipped

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:48:18+05:30 |
| Agent | E004 |
| Purpose | Read test projects and list passing vs skipped. Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E004_tests.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Scratch | `D:\Prop\reports\swarm\20260818\_tmp_e004\` (`unit.trx`, `integration.trx`) |
| Measured | Unit **86 / 64 passed / 22 skipped / 0 failed**; Integration **3 / 3 / 0 / 0**; combined **89 / 67 / 22 / 0** |
| Verdict | Both .sln test projects exit 0. All 22 skips are A43 `IQuantityConverter` / dest re-floor. C17 83/60/1/22 is stale. C++ `mt5-sdk/tests` not built (CMake default OFF). |

---

## 2026-08-18 — E005 architecture risk/copy rules → RiskEngine + tests

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | E005 |
| Purpose | Map architecture risk/copy rules to `RiskEngine` + tests. Write the matrix. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E005_rules_matrix.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| SUT SHA-256 | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` (8567 B, 189 NL; unchanged vs B13/D13) |
| Tests SHA-256 | `7B95236489E2FE169BFC8E9F57A9E2C89F6F5F047116D9DC82CFA8820FB2DF51` (2909 B, 87 NL; unchanged vs C03/D35) |
| Inventory | **110** rules (R001–R110). Engine reasons **21**. Facts **5**. A89 #50–59 on disk **0/10**. Product `Evaluate` callers **0**. |
| Verdict | Vocabulary stub with the right §64 *names*. **18** MATCH, **22** PARTIAL, **11** STUB_WRONG (red-day freeze, send-under-stop-new, `ReduceSize` qty 0, exclusive kill enum, unsigned mid, unmapped close), **41** MISSING. Live copy **SAFE_BY_ABSENCE**. §68/§70 boxes this file owns stay **unchecked**. |

---

## 2026-08-18 — E022 confirm no `.env`

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:45+05:30 / 2026-08-18T08:20:45Z |
| Agent | E022 |
| Purpose | Confirm no `.env`. Write the report. Do not print secrets. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E022_no_env.md` |
| Product source modified | **No** |
| Root `.env` | **YES** — 3408 B, SHA-256 `56C81786F2B4DCCF5BB7EC18072BB7D001E0523B4F2C2F60317F288D66C8D6DA`, gitignored (`!! .env`), `git hash-object` = HEAD `.env.example` |
| `src\.env` / apps / tests / `mt5-sdk\.env` | **ABSENT** |
| Verdict | Assigned “no `.env`” **REJECTED** at repo root. File is the renamed example (placeholder password slots). Never tracked (`rev-list` empty). Live MT5/FIX still **NOT PROVEN**. |

---

## 2026-08-18 — E011 creds block: no filled .env, no user-secrets, live copy cannot start

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:48:16+05:30 |
| Agent | E011 |
| Purpose | Confirm no usable `.env`, no user-secrets, live copy cannot start. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E011_creds_block.md` |
| Product source modified | **No** |
| Report SHA-256 | `16E39DDD59B73EF474ECA5B156391F23D4C5976CE24EB847A55A9E97FE5AEE19` (20870 B, 360 lines) |
| Verdict | **BLOCKED.** No filled operator `.env` (gitignored `D:\Prop\.env` is the unfilled example, SHA `56C81786…`). User-secrets roots + both worker ID folders **absent**. Process `MT5_PASSWORD` / `CTRADER_FIX_PASSWORD` **absent**. Hosts do not load dotenv. Live copy **cannot start**. |

---

## 2026-08-18 — E006 TradeReconstructor dirty canceled positions

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | E006 |
| Purpose | Read `TradeReconstructor` dirty canceled positions. Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E006_cancel_dirty.md` |
| Product source modified | **No** |
| SUT SHA-256 | `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` (`TradeReconstructor.cs`, 12 768 B / 347 lines) |
| Tests | `TradeReconstructionTests` **6/6** passed (includes `Canceled_deal_on_a_position_excludes_it_from_first_three`) |
| Eval | `D:\Prop\reports\swarm\20260818\_tmp_e006_cancel\` (reports-only; Domain reference) |
| Verdict | **Helper taint exists; A21 dirty does not.** 13/14 skip the volume book and set `EligibleForFirstThree=false` on **every** lifecycle of that `position_id`. No `Dirty` / `RECON_CANCELED_DEAL` / persist column. Production score + dashboard + shadow use `Completed && IsXauUsd` (`M5` helper 2/false vs score 3/true `SHADOW`). Official 0→13 hidden by first-write-wins upsert. A83/C31 “never dirties” is stale. |

---

## 2026-08-18 — D101 untested recon edges (OUT_BY / zero volume / mixed broker)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D101 |
| Purpose | List untested recon edges: OUT_BY, zero volume, mixed broker. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D101_recon_edges.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| SUT SHA-256 | `TradeReconstructor` `AEA3930B…` (12768 B); tests `CB223DDE…` (4895 B, 6 facts) |
| `dotnet test` | TradeReconstruction + DealReason + VolumeConverter **11 passed / 0 failed** |
| Verdict | **All 3 families untested in product tests.** 0 `DealEntry.OutBy`; 0 tradeable `VolumeNative=0`; 0 `STARWAVEFX` reconstruct. A21 F09/F23/`RECON_ZERO_VOLUME` missing. A89 #6/#13/#19 absent. Z4/Z8 still first-3 poison (eligible stays true). Mixed isolation holds in C31 harness only. C31/D33 stale on cancel only. |

---

## 2026-08-18 — E007 PersistDemoShadowAsync SHADOW only?

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:48:36+05:30 |
| Agent | E007 |
| Purpose | Read `PersistDemoShadowAsync`. SHADOW only? Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E007_shadow.md` |
| Product source modified | **No** |
| Store SHA-256 | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` (12097 B / 338; untracked blob `543c1432…`) |
| Infrastructure build | **GREEN** 0/0 (D47 RED stale) |
| Verdict | **YES for copy/shadow rows; NO for the method.** Hard `state != SHADOW` return before `new CopyIntent` / `new ShadowOrder`; `Status="SHADOW_ONLY"`. Method always writes `ScoreUpdate` outbox for any state. Never `ExecutionIntent` / LIVE / FIX send. Not A24. Seed (same SHA as D48): 6+6 SHADOW rows, 4 outbox. |

---

## 2026-08-18 — D102 emergency flatten vs close (risk edges)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:47:10+05:30 |
| Agent | D102 |
| Purpose | Emergency flatten vs close. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D102_risk_edges.md` |
| Product source modified | **No** |
| `RiskEngine.cs` SHA-256 | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` (unchanged vs D13/D35/D70/C33) |
| Verdict | **Three close-like ops, one reducing pipe.** Source `CLOSE_EXPOSURE` ≠ remainder flatten (G32) ≠ `EMERGENCY_FLATTEN` run. Engine `EmergencyFlatten` only blocks opens; `IsReducing` passthrough has no dest id / coalesce / flatten qty. `AllowFixSend` requires `None`+`Real` (C3/C4 inverted). Loss/DD freeze both exits. Live book `SAFE_BY_ABSENCE`. Do not implement `docs/risk.md` auto-flatten. |

---

## 2026-08-18 — E012 API `:5000` / web `:3000`

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:48:09+05:30 / 2026-08-18T08:18:29Z |
| Agent | E012 |
| Purpose | API 5000 web 3000. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E012_ports.md` |
| Product source modified | **No** |
| Live binds | `:5000` pid 54468 `TraderIntelligence.Api --urls http://127.0.0.1:5000`; `:3000` pid 49100 `vite --host 127.0.0.1 --port 3000` |
| Verdict | **Intended split, not a conflict.** API **5000**, web **3000**. `/health` 200; Vite `/` 200; CORS Origin `:3000` OPTIONS 204 / GET 200 `*`. Hub negotiate 404. `:5160` gone from worktree, still in HEAD launchSettings. |

---

## 2026-08-18 — E008 DemoSeeder + fix-worker: still forging LoggedOn?

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:48:31+05:30 |
| Agent | E008 |
| Purpose | Re-read `DemoSeeder` and fix-worker. Still forging `LoggedOn`? Write `E008_fix_status.md`. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E008_fix_status.md` |
| Product source modified | **No** |
| Seeder SHA-256 | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` (5082 B / 140 lines) |
| `Worker.cs` SHA-256 | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` (2093 B / 51 lines) |
| Verdict | **No.** Neither seeder nor worker assigns `LoggedOn`. Both persist `Disconnected` + no-socket LastError. Zero product `Status = FixSessionStatus.LoggedOn` writers. Dashboard health bits stay false. Dest quote `2399.45/2399.85` still invented. Live Logon still **NOT PROVEN**. D22 seeder-LoggedOn is stale. Send still **SAFE_BY_ABSENCE**. |

---

## 2026-08-18 — E003 React route × API endpoint matrix

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:47:06+05:30 |
| Agent | E003 |
| Purpose | List all React routes in `apps/web` and matching API endpoints. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E003_route_matrix.md` |
| Product source modified | **No** |
| `App.tsx` SHA-256 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` |
| `Program.cs` SHA-256 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` |
| Verdict | **16 React destinations; 15 live unversioned maps; 11/11 hook paths exist on the host; 0 `/api/v1`; 3 pages have no fetch (Shadow/Live/Audit); SignalR `/hubs/dashboard` is 404; `SettingsController` is unmapped.** Demo pairing is not A26/A63. |

---

## 2026-08-18 — D92 volume vote: B14 10k over A81 1e8 default

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:46:01+05:30 |
| Agent | D92 |
| Purpose | Vote A81 constructor default 1e8 vs B14 10 000. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D92_volume_vote.md` |
| Product source modified | **No** |
| Converter SHA-256 | `C6C5E3FD26343532EF047F46D7728A5FED7027B82312A225B9CC3AA881EAC0A2` (1318 B) |
| Eval | `D:\Prop\reports\swarm\20260818\_tmp_d92_vote\stdout.txt` |
| Verdict | **B14.** Compiled `new VolumeConverter().Scale == 10000`. Extractors copy `Volume()`; zero `VolumeExt()` in product C++/C#. A81 1e8 is the official ext scale, not the default. Flip → 10 000× recon undersize / send oversize. |

---

## 2026-08-18 — D78 TradersPage is not the §50 leaderboard

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:45:04+05:30 |
| Agent | D78 |
| Purpose | Read `TradersPage.tsx`. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D78_traders.md` |
| Product source modified | **No** |
| Page SHA-256 | `0AF0FF5BD2EE6B7B4BB06F483B065589A1235FE94EE63B2F4491EC00C510518F` (1604 B, 42 LF lines, untracked) |
| Verdict | **Chrome YES / §50 NO.** 9-column demo table via `useTraders({})` → `GET /api/traders`. Missing ML, Shadow P&L, Live allocation, Last scored. No filters/sort/pager/URL state. Detail link uses broker **code**. B29 numeric-enum claim stale (`JsonStringEnumConverter` on). §69 item 8 still FAIL. |

---

## 2026-08-18 — D76 `types/index.ts` vs live API

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:43:29+05:30 |
| Agent | D76 |
| Purpose | Compare `apps/web/src/types/index.ts` vs live API. Write this file. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D76_types.md` |
| Product source modified | **No** |
| Types SHA-256 | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` (2905 B, 135 lines; 0 imports) |
| DashboardModels SHA-256 | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` (8 records) |
| Program.cs SHA-256 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` (`JsonStringEnumConverter` + `GetTraderDetailAsync`) |
| Verdict | **DEPRECATED unused stub.** 0/8 dashboard pairs have field parity. 4/13 TS types match anonymous health/recon/settings. B29 stale (ints / no detail DTO). Do not type hooks from `index.ts`. |

---

## 2026-08-18 — D95 scale: not 5,000 accounts

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:44:43+05:30 |
| Agent | D95 |
| Purpose | Confirm the tree is **not** 5,000 accounts. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D95_scale.md` |
| Product source modified | **No** |
| Fake SHA-256 | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` (unchanged vs D24) |
| Verdict | **Not 5,000. Measured 4 logins** (10001, 10002, 10003, 99001) = **0.08%** of §69.3. 18 canned deals. A79 `SeedFiveThousandAccounts` **MISSING**. Score loops hard-code the same four. Checkpoints unused. README L22 overclaims. Port 5000 / quote-age 5000 / balance 5_000 are **not** a census. §69.3 still **FAIL**. |

---

## 2026-08-18 — D82 AuditPage remesure

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:45:05+05:30 |
| Agent | D82 |
| Purpose | Read `AuditPage.tsx`. Write this file. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D82_auditpage.md` |
| Product source modified | **No** |
| Page SHA-256 | `8DE2F9B0AA9B14798C1C6F548E41837F6B5FF80869A3DBBFC91A13769A8E7B38` (324 B, 8 LF; **untracked** vs HEAD `398a142`) |
| Verdict | **Chrome stub, not a reader.** `/audit` + exact §46 label on worktree. 0 table / hook / `GET /api/v1/audit[/logs]` / writer. C38 page bytes unchanged. Program.cs now `61B1E0D1…` (still no audit map). |

---

## 2026-08-18 — D94 “fix-worker stamps LoggedOn” is anti-evidence (and stale)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:44:41+05:30 |
| Agent | D94 |
| Purpose | Re-measure the sentence “fix-worker stamps LoggedOn”. Treat it as anti-evidence. Write `D94_lie.md`. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D94_lie.md` |
| Product source modified | **No** |
| `Worker.cs` SHA-256 | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` (2093 B / 51 lines) |
| Verdict | **Assignment sentence FALSE against current bytes.** Worker has 0 `LoggedOn` tokens; stamps QUOTE+TRADE `Disconnected` every 15 s (no socket). Mid-wave forge (`B48033A5…` / `real ? LoggedOn : LoggedOn`) is gone and was **anti-evidence** of Logon. HEAD is the 1 s template. D22 seeder `LoggedOn` is stale (`A6416491…` seeds `Disconnected`). Dashboard still maps `LoggedOn` → healthy (latent). `A101` item 1 / §70.1 still **FAIL**. Send still **SAFE_BY_ABSENCE**. |

---

## 2026-08-18 — D98 MayRetryNewOrderSingle false after send

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:46:11+05:30 |
| Agent | D98 |
| Purpose | `MayRetryNewOrderSingle` false after send? Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D98_noretry.md` |
| Eval | `D:\Prop\reports\swarm\20260818\_tmp_d98_noretry\stdout.txt` |
| Product source modified | **No** |
| FSM SHA-256 | `CDF7B67EB0D032513C2EBF73BC5B3F208F665D6A2A18327E39975198DCF12219` (2177 B, 56 NL; unchanged vs B16/D17) |
| Unit | `Unknown_ack_cannot_retry_new_order` **1/1 Passed** |
| Verdict | **Yes at the helper.** `AfterSendAttempt()` → `SentAcknowledgementUnknown`; `MayRetry==false`; `RequiresReconciliation==true`. Eval `MAY_RETRY_AFTER_SEND=False`. System still unproven (zero callers, no T3 arm, string `ExecutionIntent.Status`). G09 stays FAIL. |

---

## 2026-08-18 — D83 ShadowPortfolioPage: §46 / A26 page?

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:43:57+05:30 |
| Agent | D83 |
| Purpose | Read `ShadowPortfolioPage.tsx`. Is it the §46 / A26 Shadow Portfolio? Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D83_shadowpage.md` |
| Product source modified | **No** |
| Page SHA-256 | `608C8C2D2D0F3FE89EC7632159217191809EB92805051EE9529626B78AE36276` (628 B, 14 lines; unchanged vs C08/D08) |
| Verdict | **Chrome only.** Route `/shadow` + abbreviated nav `Shadow`. No hook, no `GET /api/v1/shadow/portfolio` (A63 **in** v1 — §69 blocker, unlike Live). Six demo `SHADOW_ONLY` rows exist (D48) and are not painted. Copy claiming approved CopyIntent + stale expiry is **false**. NOS-off and “not source ticks” are the only true sentences. |

---

## 2026-08-18 — D93 A57 0/12 inventory is STALE

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:44:45+05:30 / 2026-08-18T08:14:45Z |
| Agent | D93 |
| Purpose | Pin that A57’s 0/12 is a stale *inventory* of an empty tree. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D93_a57_stale.md` |
| Product source modified | **No** |
| A57 SHA-256 | `C1E94C992B28818FAF23D9D6923E2EF56877FE205BA1D64334E5294BC784455E` (36 916 B) |
| D93 SHA-256 | `278EF0B5044D12D67C72316E41D0608427C24F85DBAB5B6EA01233A6412FC6C6` (17 290 B) |
| Current scorecard | D41 (SHA `A9B68AB9…`; hashes unchanged vs this pass) |
| Verdict | **A57 inventory STALE.** `Class1` / weatherforecast / 0 pages / 0 tests / non-compiling plural EF are gone. Demo path exists (items 2, 4–8, 11 + React shell). **§69 accepted still 0/12.** Do not paste A57’s item table. Do not increment the gate. |

---

## 2026-08-18 — D96 harness `123456` must not seed

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:48:00+05:30 |
| Agent | D96 |
| Purpose | Pin: harness `55=123456` must not seed. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D96_id.md` |
| Product source modified | **No** |
| Harness SHA-256 | `99A28D8F3F49028706C75B9C4DC46B4CCB3FF98E90AAED3B4B874DD1B4351616` (8970 B, L141 FLAG) |
| Seeder SHA-256 | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` (5082 B, untracked; `VenueInstrumentId=null`) |
| Verdict | **Must not seed; measured not seeded.** Product `123456` lives only in `FixSimulationHarness`. Mapper/options/apps/Infrastructure: 0 hits. Do not wire harness → quote persist. §69.10 still NO. |

---

## 2026-08-18 — D84 ReconciliationPage vs §54

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:43:40+05:30 |
| Agent | D84 |
| Purpose | Read `ReconciliationPage.tsx`. Write this file. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D84_reconpage.md` |
| Product source modified | **No** |
| Page SHA-256 | `BC036D09A78AECBABD47A8DD9AC0B58E934C7DBDF51930B136545797BEFE8886` (490 B, 12 lines; unchanged vs B20/C08/D08) |
| Git | `?? apps/web/src/pages/` — entire pages tree untracked; HEAD `App.tsx` already imports this module |
| Verdict | **Chrome exists; §54 does not.** Title + one sentence + `JSON.stringify` of `GET /api/reconciliation/status` (`UtcNow` + three zeros). 0/8 §54 widgets. A96 DTO 0 fields. Host map **UNSAFE** (looks like a clean successful reconcile). Nav label `Recon` ≠ §46 `Reconciliation`. |

---

## 2026-08-18 — D77 OverviewPage vs §47

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:43:05+05:30 |
| Agent | D77 |
| Purpose | Read `OverviewPage.tsx`. Write the Overview close-read vs architecture §47. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D77_overview.md` |
| Product source modified | **No** |
| Page SHA-256 | `6497193F190445CCF76AED218E01E3BC85050238CB89D06002837FC9C502825C` (2078 B, 35 LF; **untracked** vs HEAD `398a142`) |
| Live `GET /api/overview` | HTTP 200: 4 accounts, 2 “connected”, 3 XAU, 2 SHADOW, 1 RISK_BLOCKED, `shadowPnl` 248.20, `mt5Healthy` true, FIX bits false, `realCopyEnabled` false |
| Verdict | **File exists; §47 is not implemented.** 11/18 dedicated tiles + merged QUOTE/TRADE. `live` / `xauGross` / `xauNet` dropped. MT5 OK is `brokers.Enabled > 0`. A91/A62/A29 “page MISSING” is stale. |

---

## 2026-08-18 — D74 API `JsonStringEnumConverter`

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D74 |
| Purpose | Does the API use `JsonStringEnumConverter`? Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D74_enums.md` |
| Product source modified | **No** |
| `Program.cs` SHA-256 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` (4731 B / 95 lines; same as D06/D30) |
| Verdict | **YES.** One `ConfigureHttpJsonOptions` registration, default ctor (`namingPolicy: null`, `AllowIntegerValues: true`). Live enum fields serialize as identifier strings (`"WATCH"`, `"Long"`). B10/B29 “no converter / integers” is stale. Kill-switch is already `ToString()` (`StopNewExecution`, not A48 `STOP_NEW_EXECUTION`). |

---

## 2026-08-18 — D75 launchSettings weather leftover

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:43:29+05:30 |
| Agent | D75 |
| Purpose | Is there a `launchSettings` weather leftover? Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D75_launch.md` |
| Product source modified | **No** |
| API `launchSettings.json` SHA-256 | `BC0228981D29FFCF8D94737AF3446D96AB29415ACFF494687250265E266CD7F0` (1125 B, 13:32:01) |
| Verdict | **No leftover on the worktree.** 0 `weatherforecast` in all 3 launch files; API profiles are `swagger` ×3. C04/C15 IIS leftover **closed** (same SHA as D06). `HEAD` blob `36903867…` still has 3× `weatherforecast` + `:5160`. `swagger` without `UseSwaggerUI()` is a 404 half-migration, not weather. |

---

## 2026-08-18 — D97 CanPromoteToLive is false

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:45:18+05:30 |
| Agent | D97 |
| Purpose | Confirm `CanPromoteToLive` is false. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D97_nolive.md` |
| Product source modified | **No** |
| SUT SHA-256 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` (unchanged vs D12/D34) |
| `dotnet test` | `BaselineScorerTests` **3 passed / 0 failed / 0 skipped** |
| Verdict | **CONFIRMED false.** `CanPromoteToLive(TraderState current) => false` (L211). Parameter discarded. Product callers: **none**. One unit fact locks `SHADOW` only. Vacuous lock, not A22 R5-before-R6. Persist copies `SuggestedState` blindly. §68 0/19 and §70 0/14 unchanged. |

---

## 2026-08-18 — D87 Infra → Mt5 layering

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D87 |
| Purpose | Answer “Infra references Mt5 OK?” with a re-measured layering census. |
| Artifact | `D:\Prop\reports\swarm\20260818\D87_layer.md` |
| Product source modified | **No** |
| Infra csproj SHA-256 | `4DABF29CA934261CFC46C72514CB7AA04D5E8F9CC8FFAC1BA051BF0CD0668EED` (1035 B, unchanged vs C35) |
| DI SHA-256 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` (unchanged vs C35) |
| Verdict | **YES for Fake demo; NO as A54/go-live graph.** Class `EXISTS_NEEDS_REFACTOR`. Persistence/dashboard still 0 Mt5 usings. Dual `CreateDefault()` remains. API + FIX-worker load `TraderIntelligence.Mt5.dll` transitively. Invert before native Manager lands in `src/Mt5`. Not a §69 FAIL. |

---

## 2026-08-18 — D72 first-3 is completed XAU only (helper), increment not done

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D72 |
| Purpose | Answer: is first-3 reconstructed completed XAU only? Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D72_first3.md` |
| Product source modified | **No** |
| Reconstructor SHA-256 | `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` (12768 B) |
| Eval | `D:\Prop\reports\swarm\20260818\_tmp_d72_first3\stdout.txt` |
| Verdict | **Helper YES / engine NO / increment NO.** `CountCompletedXauUsdTrades` = `Completed && IsXauUsd && EligibleForFirstThree` (2 XAU + 1 EUR → count 2). `Reconstruct` still emits EUR/XAG. No `first3_keys`. Score + dashboard ignore dirty (`M5` helper 2 / score 3). Over-map `XAUUSDFUT`/`GOLD.` counts as XAU. |

---

## 2026-08-18 — D62 root `.gitignore` recensus

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:40:18+05:30 |
| Agent | D62 |
| Purpose | Read `.gitignore`. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D62_gitignore.md` |
| Product source modified | **No** |
| `.gitignore` SHA-256 | `FAE817C1C2F9AD9BEA4353D89A82ED015585A449FC1339561F2C966A0C2B21E0` (1107 B, 73 lines, LF; HEAD blob `f4c00707…`; clean vs `398a142`) |
| Verdict | **EXISTS_NEEDS_REFACTOR.** Env rules work (`.env` ignored; `!.env.example` correct). A103 §6 unapplied. Worktree deleted `.env.example`; same blob is ignored `.env` (placeholders only). Dirty API `FileStorePath=./fixstore` + `FileLogPath=./fixlogs` are **OPEN**. |

---

## 2026-08-18 — D70 STOP_NEW vs FLATTEN distinct?

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:42:01+05:30 |
| Agent | D70 |
| Purpose | Are `STOP_NEW` and `FLATTEN` distinct? Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D70_kill.md` |
| Product source modified | **No** |
| Verdict | **Specified YES / implemented NO.** §40 two independent controls; tree stores exclusive `KillSwitchMode`. Flatten does not flatten. `{stop-new ON × flatten ACTIVE}` unrepresentable. §68 / §70.13 stay `[ ]`. |

---

## 2026-08-18 — D79 FixSessionsPage: password shown?

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:42:52+05:30 |
| Agent | D79 |
| Purpose | Read `FixSessionsPage.tsx`. Answer: password shown? Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D79_fixpage.md` |
| Product source modified | **No** |
| Page SHA-256 | `EC93326688719E10D3ED5CB275D9BF1E7113C7F61EEA99803F42E1EA268BB886` (1312 B, 26 LF; untracked vs HEAD `398a142`) |
| Verdict | **No. Password is not shown.** Line 8 is the disclaimer only. No `s.password`, no input, no `JSON.stringify`. `FixSessionDto` / `FixSessionState` have no password field. `CTraderFixOptions.Password` is off this path. |

---

## 2026-08-18 — D41 §69 FUV scored against CURRENT repo

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D41 |
| Purpose | Score architecture §69 12 items against current worktree, not A57 stale inventory |
| Artifact | `D:\Prop\reports\swarm\20260818\D41_fuv_now.md` |
| Product source modified | **No** |
| Verdict | **Accepted 0/12.** DEMO: 2, 4, 5, 6, 7, 8, 11. FAIL: 1, 3, 9, 10. PARTIAL: 12. FIX worker now honestly `Disconnected`; shadow persist writes from invented dest quote (`VenueInstrumentId=null`). Live MT5 / QUOTE / discovered tag 55 still absent. |

---

## 2026-08-18 — D48 ShadowOrders in seeder?

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D48 |
| Purpose | Are `ShadowOrders` created in seeder? |
| Artifact | `D:\Prop\reports\swarm\20260818\D48_shadow_rows.md` |
| Eval | `D:\Prop\reports\swarm\20260818\_tmp_d48_shadow\stdout.txt` |
| Product source modified | **No** |
| Verdict | **YES as a rebuild side-effect, not a direct seeder insert.** `DemoSeeder` has no `ShadowOrders` token. First `SeedAsync` → `RebuildTraderAsync` → `PersistDemoShadowAsync` writes **6** `shadow_orders` + **6** `SHADOW_ONLY` intents (10001×3, 99001×3). 10002/10003 get none. Dashboard `ShadowPnl=248.20` is Σ slippage, not P&L. Not §24. |

---

## 2026-08-18 — D51 migrations folder (none)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D51 |
| Purpose | Answer “Migrations folder?” with a measured census. |
| Artifact | `D:\Prop\reports\swarm\20260818\D51_migrations.md` |
| Product source modified | **No** |
| `TraderDbContext.cs` SHA-256 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` (5951 B, unchanged vs C29/D19) |
| Verdict | **MISSING.** No `Persistence/Migrations/` (disk or git). A30 **0/15**. `EnsureCreatedAsync` × 3. Default InMemory. API `ConnectionStrings:Postgres` unused (DI reads `TraderIntelligence` / `DATABASE_URL`). HEAD 5 stub configs deleted; not migrations. §60 / §72.3 **FAIL**. |

---

## 2026-08-18 — D43 §70 live FIX all FAIL

| Item | Value |
|---|---|
| Date | 2026-08-18T08:10:29Z |
| Agent | D43 |
| Purpose | Confirm architecture §70 (14 live FIX acceptance items) all FAIL for **live**. Re-measure tree; do not inherit A101 worker-LoggedOn narrative. |
| Artifact | `D:\Prop\reports\swarm\20260818\D43_s70.md` |
| Product source modified | **No** |
| Verdict | **0 / 14 FAIL.** Same integer as A101. Worker/seeder now stamp `Disconnected` (D32) — honesty, not Logon. No QuickFIX/n, no `GuardedNewOrderSingle`, no `tests/Fix`, no `LOGON_OK`. Domain helpers (RiskEngine / ClOrdId / FSM) are not a send path. `SAFE_BY_ABSENCE` ≠ pass. Live copy stays off. |

---

## 2026-08-18 — D56 `mt5_xau_ticks` table

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D56 |
| Purpose | Re-measure whether `mt5_xau_ticks` exists; write this file. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D56_ticks.md` |
| Product source modified | **No** |
| `TraderDbContext` SHA-256 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` (unchanged vs C60/D19) |
| Verdict | **MISSING.** No entity, no `DbSet`, no `ToTable`, no migration, no `.sql`, no C++/C# writer. Exact MFE **UNAVAILABLE**. Scorer omission is correct (`Unavailable` + null averages). Do not stand in dest quotes or deals. C60 still holds. |

---

## 2026-08-18 — D47 CopyIntent after score SHADOW

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:38:00+05:30 |
| Agent | D47 |
| Purpose | Is CopyIntent created after score SHADOW? |
| Artifact | `D:\Prop\reports\swarm\20260818\D47_copyintent.md` |
| Product source modified | **No** |
| Verdict | **YES by control flow.** `RebuildTraderAsync` persists score then `PersistDemoShadowAsync`; that method `new CopyIntent` only when `state == SHADOW` (plus dest quote). Demo OPEN backfill, not A24. Infrastructure build **RED** (entity rewrite 13:37 vs writer 13:35). C59 writers claim **stale**. |

---

## 2026-08-18 — D63 docker-compose: MT5 not in Linux

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:40:45+05:30 |
| Agent | D63 |
| Purpose | Read `docker-compose.yml`. Confirm MT5 is **not** in Linux. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D63_compose.md` |
| Product source modified | **No** |
| Compose SHA-256 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` (687 B, 30 LF, LastWriteUtc `2026-08-18T07:48:40.1339443Z`; unchanged vs B37/C12) |
| Docker CLI | **MISSING** (`docker` / `docker-compose` not on PATH) |
| Verdict | **CONFIRMED: MT5 is not in Linux.** Services are `postgres`, `redis`, Linux `api` only. No `mt5-worker`. Line 30: stay-on-Windows comment. Native Manager PE `0x8664`. |

---

## 2026-08-18 — D50 API MapHub / SignalR hub

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:40:00+05:30 |
| Agent | D50 |
| Purpose | Answer **API map hub?** Confirm whether `apps/api` calls `MapHub` / exposes `/hubs/ops` |
| Artifact | `D:\Prop\reports\swarm\20260818\D50_signalr.md` |
| Product source modified | **No** |
| `Program.cs` SHA-256 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` (4731 B; same as D06) |
| Verdict | **No hub mapped.** `AddSignalR` 0 / `MapHub` 0 / no `OpsHub` / no `/hubs/ops`. Unused `SignalR.Common` 8.0.4. Client still dials forbidden `/hubs/dashboard` and swallows failure. Workers correctly do not host SignalR. C28 conclusion holds; C28 hashes and D06 “no Controllers/” are stale. |

---

## 2026-08-18 — D37 SeedingAndStoreTests integration recensus

| Item | Value |
|---|---|
| Date | 2026-08-18 13:38 +05:30 |
| Agent | D37 |
| Purpose | Read `tests/Integration/SeedingAndStoreTests.cs`. Recensus vs §60 / A90 / C16. |
| Artifact | `D:\Prop\reports\swarm\20260818\D37_integ.md` |
| Product source modified | **No** |
| Test SHA-256 | `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` (unchanged vs C16) |
| Verdict | **PARTIAL** InMemory smoke. §60 **0/8**. Fresh rebuild **RED** (`CS8858` on `ReconstructedTradeResult`). Stale-bin 2/2 class facts PASS. `NotBe(LIVE)` vacuous; unique index unproven. Seeder now `Disconnected` (D22 stale) but test does not lock status. |

---

## 2026-08-18 — D54 Serilog package used?

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:38:57+05:30 |
| Agent | D54 |
| Purpose | Confirm whether the Serilog package is used. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D54_serilog.md` |
| Product source modified | **No** |
| API csproj SHA-256 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` (803 B; HEAD = worktree) |
| `Program.cs` SHA-256 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` (4731 B; matches D06) |
| `appsettings.json` SHA-256 | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` (1254 B; dirty vs HEAD) |
| Verdict | **Package YES / used NO.** `Serilog.AspNetCore` 8.0.2 is the only product Serilog reference (API). Zero C# call sites (0/85). Worktree `"Serilog"` JSON is unread (C25 “no JSON” is stale). Workers have no package/DLLs. Pipeline / §57 / A50 **MISSING**. |

---

## 2026-08-18 — D33 TradeReconstructionTests coverage gaps

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D33 |
| Purpose | Read `TradeReconstructionTests.cs`. Inventory coverage gaps vs A21 / §14–15 / §60 / A89. |
| Artifact | `D:\Prop\reports\swarm\20260818\D33_recon_tests.md` |
| Subject SHA-256 | `5D99BA22B0FEFC248568E6CB0B462A31126DF825F57D34F9DD8C1586B661FBF2` (`TradeReconstructionTests.cs`, 3939 bytes) |
| SUT SHA-256 | `E20457B398DB6CCC5F78ADE295A340CBC0646F5668F9F79F6AFBCC09D35741DD` (`TradeReconstructor.cs`, 12307 bytes) |
| Product source modified | **No** |
| Test source modified | **No** |
| Measurement | `dotnet test …~TradeReconstructionTests` → **5/5 passed** (smoke) |
| Verdict | **FAIL / INSUFFICIENT.** 5 fused smokes; **0/25** A21 fixtures; **1/22** A89 recon classes on disk; INOUT money double-count unguarded. |

---

## 2026-08-18 — D39 hooks.ts vs Program.cs endpoints

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:37:27+05:30 |
| Agent | D39 |
| Purpose | Compare `apps/web/src/api/hooks.ts` HTTP paths to `apps/api/Program.cs` maps |
| Artifact | `D:\Prop\reports\swarm\20260818\D39_hooks.md` |
| hooks.ts SHA-256 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` (1935 B, unchanged vs D08/B30) |
| Program.cs SHA-256 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` (4731 B; grew vs C04 `E914FA98…`) |
| Product source modified | **No** |
| Verdict | **11/11** hook GETs match a live `MapGet`. **11/15** host maps have a hook. **0/11** use `/api/v1`. Host-only: `/health`, `/ready`, `/api/risk/status`, `POST /api/ops/resync`. SignalR `/hubs/dashboard` has **no** `MapHub`. Trader detail now returns `TraderDetailDto` (`GetTraderDetailAsync`) — B30/C04/D02 stale. |

---

## 2026-08-18 — D30 API endpoints + secrets

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:35:15+05:30 |
| Agent | D30 |
| Purpose | Read `apps/api/Program.cs`. List endpoints. Secrets? |
| Artifact | `D:\Prop\reports\swarm\20260818\D30_api.md` |
| Product source modified | **No** |
| `Program.cs` SHA-256 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` (4731 bytes, 95 lines) |
| Verdict | **15** maps (14 GET + 1 POST), all anonymous. `weatherforecast` **GONE**. No §55 secret on the wire (safe by absence, no sanitizer). `CTrader:Password` empty; `AccountId` `1369850` committed. `POST /api/ops/resync` + CORS `*` **UNSAFE** as an ops door. |

---

## 2026-08-18 — D38 App.tsx + DashboardLayout routes

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:37:02+05:30 |
| Agent | D38 |
| Purpose | Read `App.tsx` and layout; census every React route vs A26 §5.2 / architecture §46 |
| Artifact | `D:\Prop\reports\swarm\20260818\D38_routes.md` |
| Product source modified | **No** |
| `App.tsx` SHA-256 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` (2062 B, 42 lines) |
| Layout SHA-256 | `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` (1854 B, 44 lines) |
| Verdict | **EXISTS_NEEDS_REFACTOR.** 16 destinations, 14 sidebar links, 15/15 page imports resolve. A26 exact paths **14/17** (`/login` missing, `/models` missing-by-design, `/groups` ≠ `/mt5-groups`). No catch-all, no auth, no header strip. Live+Audit routes exist on worktree only (unstaged +4/+2 vs HEAD). |

---

## 2026-08-18 — D27 FixMessageParser review

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D27 |
| Purpose | Read `FixMessageParser.cs`. Write parser review. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D27_parser.md` |
| Product source modified | **No** |
| SUT | `src/Fix.CTrader/Parsing/FixMessageParser.cs` — 145 lines / 6016 bytes / SHA-256 `C58681E761D43052B53D2A8D00883C461A9E3CEB5B7DF8995D50F8155F710E3D` |
| Eval | `reports/swarm/20260818/_tmp_d27_parser/stdout.txt` (project-referenced `dotnet run`) |
| Verdict | **EXISTS_NEEDS_REFACTOR** as pipe fixture; **UNSAFE** as wire decoder / MD decoder / live outbound. Checksum 163 proven. `Build` always emits `\|\|10=`. Last-wins map. A89 #60/#61/#74 **MISSING**. Zero tests. |

---

## 2026-08-18 — D34 BaselineScorerTests surface

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D34 |
| Purpose | Read `BaselineScorerTests.cs`. Inventory asserts vs SUT. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D34_score_tests.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Tests SHA-256 | `61E34A07D76B104CF5D8B818242104522A8B59D12422C5EF4555C2447308D408` (2414 bytes) |
| `dotnet test` | 3 passed / 0 failed / 0 skipped (2026-08-18 13:36:43) |
| Verdict | 3 facts / 7 asserts lock B35 qualitative trio only. No numeric gold. No A22. No WATCH/EARLY_SCORE/N=0. Winning martingale SHADOW hole unlocked. `AfterHighEarlyScore` never called. A89 #26–#41 / #75–#79 not on disk. |

---

## 2026-08-18 — D07 workers census

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D07 |
| Purpose | Inventory `apps/mt5-worker` and `apps/fix-worker` |
| Artifact | `D:\Prop\reports\swarm\20260818\D07_workers_census.md` |
| Product source modified | **No** |
| Verdict | Two net8 Worker hosts, 6 product files each. mt5-worker: 30 s Fake ingest + score of 4 logins (SHA `57499700…`). fix-worker: 15 s stamps QUOTE/TRADE **Disconnected** (SHA `92A8F492…`; B07/C07 `B48033A5…` stale). A64 jobs **0/7** + **0/10**. No health port, no outbox, no QuickFIX, no Manager DLL. Real send **SAFE_BY_ABSENCE**. Default store InMemory. |

---

## 2026-08-18 — D06 apps/api census (no weatherforecast route)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D06 |
| Purpose | Inventory `D:\Prop\apps\api`. Confirm **no weatherforecast route**. |
| Artifact | `D:\Prop\reports\swarm\20260818\D06_api_census.md` |
| Product source modified | **No** |
| `Program.cs` SHA-256 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` (4731 bytes) |
| Verdict | **Confirmed: no weatherforecast route.** 0 `MapGet("/weatherforecast")`, 0 `WeatherForecast` type, 0 product-source string under `apps/api` (`.http` + all launch profiles included), 0 Debug DLL hits, 0 hits in `apps`/`src`/`tests` authored files. Host is **15** anonymous unversioned maps (`14` GET + `1` POST `/api/ops/resync`) on `:5000`. `/api/v1` **MISSING**. IIS leftover `launchUrl=weatherforecast` (C04/C15) is **gone** (now `swagger`). Do not treat this as first-useful v1. |

---

## 2026-08-18 — D05 Fix.CTrader census

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D05 |
| Purpose | Inventory `D:\Prop\src\Fix.CTrader` (files, types, packages, consumers, HEAD vs worktree) |
| Artifact | `D:\Prop\reports\swarm\20260818\D05_fix_census.md` |
| Product source modified | **No** |
| Verdict | **EXISTS_NEEDS_REFACTOR.** 4 product `.cs` (options, pipe parser, in-memory fence, unused harness). Official QuickFIX/n **absent**. Session types **0/2**. Assembly types have **0** external call sites. Live `NewOrderSingle` **SAFE_BY_ABSENCE**. A05 `Class1` snapshot is stale. |

---

## 2026-08-18 — D21 EfDashboardQueries catalog

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D21 |
| Purpose | Read `EfDashboardQueries.cs`. Catalog every method, field source (query vs literal), table touch, and API map. |
| Artifact | `D:\Prop\reports\swarm\20260818\D21_queries.md` |
| Product source modified | **No** |
| SUT | `src/Infrastructure/Dashboard/EfDashboardQueries.cs` — 168 lines / 7407 bytes / SHA-256 `37A4DDD23305708566888F0BBE2A6CC5DE253FB7151BDEE848195DE715EF4ACE` (unchanged vs C36) |
| Verdict | **EXISTS_NEEDS_REFACTOR** on demo; **UNSAFE** as a 5k read plane. 7/7 port methods wired. ~28–32 sequential SQL on a cold paint. 16 DTO fields hardcoded. Groups N+1; leaderboard full-table + O(n²) join + no page; `GetTraderAsync` reloads the leaderboard; latest-quote / reject / shadow-sum lack supporting indexes. 0 tests. Same SHA as C36. |

---

## 2026-08-18 — D25 duplicate collector ports

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D25 |
| Purpose | Compare `IBrokerConnector` vs `IMt5BrokerConnector`; pick one surface |
| Artifact | `D:\Prop\reports\swarm\20260818\D25_dup_iface.md` |
| Product source modified | **No** |
| Verdict | Keep Application `IMt5BrokerConnector`. Delete unused `src/Mt5/Connectors/IBrokerConnector.cs` (+ `Mt5BrokerEvent`). Zero implementors / consumers. B24 SHA-256 values unchanged. Winner is still incomplete vs A58/§6. |

---

## 2026-08-18 — D19 TraderDbContext vs architecture §45

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D19 |
| Purpose | Re-measure `TraderDbContext` `DbSet`/`ToTable` names against architecture §45 (43-table full initial set) |
| Artifact | `D:\Prop\reports\swarm\20260818\D19_dbcontext.md` |
| Subject SHA-256 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` (`TraderDbContext.cs`, 5951 bytes) |
| Product source modified | **No** |
| Verdict | **FAIL.** **18 / 43** §45 tables present by name; **25** missing; **2** extra (`execution_intents` keep / `kill_switches` not §45). Table-name coverage **41.9%**; A20/A61 completeness **0/43**. B19 counts unchanged. 0 configurations, 0 named UNIQUEs, 0 FKs, 0 migrations. |

---

## 2026-08-18 — D08 web page census

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:33:46+05:30 |
| Agent | D08 |
| Purpose | Inventory `D:\Prop\apps\web\src` and list every page |
| Artifact | `D:\Prop\reports\swarm\20260818\D08_web_census.md` |
| Product source modified | **No** |
| Verdict | **15** page modules, **16** routed destinations (`/` → `/overview` + 15 pages), **14** sidebar links. Import graph 15/15. No orphans. No `LoginPage` / `ModelsPage`. Groups lives at `/groups` not A26 `/mt5-groups`. Live/Audit/Shadow are stubs. Recon/Health/Settings are `JSON.stringify` dumps. Same 15 SHAs as C08. |

## 2026-08-18 — Wave D (100+ agents, standing order)

User: **100+ sub agents always**. Launched **D01–D103**. Orchestrator also stopped forging FIX `LoggedOn`, added `DealReason` skip for rollover/service, trader-detail payload, and demo shadow book for `SHADOW` only.

---

## 2026-08-18 — D22 DemoSeeder LoggedOn without FIX

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D22 |
| Purpose | Read `DemoSeeder.cs`. Note TRADE `LoggedOn` (and QUOTE `ReadyForMarketData`) with no FIX session. |
| Artifact | `D:\Prop\reports\swarm\20260818\D22_seeder.md` |
| Product source modified | **No** |
| Seeder SHA-256 | `139D8F872DC473F0C5381AF2393BDBBE60E1D9A2A5179DD1D1737E04CCC00BEF` (4942 bytes) |
| Verdict | **FORGED.** `DemoSeeder` line 90 writes `FixSessionStatus.LoggedOn` on TRADE; line 73 writes `ReadyForMarketData` on QUOTE. No `Fix.CTrader` call, no TLS, no `35=A`. Dashboard `QuoteHealthy`/`TradeHealthy` become true. Live host `live-us-eqx-01.p.c-trader.com` + `live.pepperstone.1369850` are literals. Confirm C43: live Logon still **NOT PROVEN**. |

---

## 2026-08-18 — C47 next increment (plan only)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | C47 |
| Purpose | Propose next increment: Windows live MT5 connect, QuickFIXn net8 QUOTE logon, EF migrations, RBAC |
| Artifact | `D:\Prop\reports\swarm\20260818\C47_next_increment.md` |
| Product source modified | **No** |
| Verdict | Increment **C47 / I-Live-Foundation**, four slices in order: **47.1** versioned EF migrations (replace `EnsureCreated`); **47.2** first-useful RBAC + audit writer; **47.3** Windows `mt5-collector` ×2 wrapping preserved `IMT5Client` + C# HTTP client; **47.4** official `QuickFIXn.Core`+`QuickFIXn.FIX44` **1.14.1** QUOTE TLS Logon only. Live `35=D` stays off. §69 stays ≤2/12 even if all exits measure; §68 stays 0/19. |

---

## 2026-08-18 — Wave 1 (Phase 0 §73 audit + binding specs)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Wave | **1** |
| Purpose | Architecture §73 A–D *before* large implementation: repository audit, gap analysis, implementation sequence, risk list; plus official cTrader FIX research and first-useful-version specs |
| Law | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| Report dir | `D:\Prop\reports\swarm\20260818\` |
| Index | `D:\Prop\reports\INDEX.md` |
| `reports/agents/` | empty |
| Product source modified by cataloger | **No** |
| Product source modified by wave-1 report agents | **No** (reports only; later code already on disk is from other work, not these markdowns) |

### Inventory (catalog snapshot)

| Band | Count | Notes |
|---|---:|---|
| A01–A105 | **105** | Consecutive; no missing A IDs |
| B-series markdown | **34** | B01–B10, B12–B16, B18–B27, B29–B33, B37–B40 |
| C-series markdown | **2+** | C06, C07 present; rest still landing |
| Scratch | `_tmp_b35_score/` | Not a report (throwaway compile) |
| **Report total** | **141+** | All under `swarm/20260818/` |

B-band gaps at catalog time: B11, B17, B28, B34, B35. **B36 landed** (`B36_risk_fixtures.md`). C-band just started. Do not treat missing IDs as written.

### What wave 1 produced

1. **§73.A Repository audit** — A01–A19, A29, A55, A57, A100, A101; B01–B03, B05–B06, B09 refresh the measured tree after Domain/Application grew.
2. **§73.B Gap analysis** — A29 (Phase 0 / early Domain vocabulary). B02/B03/B05 reclassify layers that A01–A05 still called `Class1`.
3. **§73.C Implementation sequence** — A30 (exact files / modules / migrations for §69).
4. **§73.D Risk list** — A56 (SDK, Windows DLL, ticks, FIX headers, sizing, live-account safety).
5. **Official cTrader FIX** — A31–A36 (overview, RoE, send/recv, FAQ, QuickFIX/n 1.14.1, cServer dictionary).
6. **MT5 SDK binding** — A12–A18, A37–A39, A81–A85, B14 (volume default **10 000**).
7. **First-useful specs** — reconstruction, scoring, risk, shadow, FIX session, outbox, workers, dashboard DTOs A91–A97, indexes, Redis keys, flags, kill switch.

### Honest measured scoreboard

| Gate | Score | Source |
|---|---|---|
| First useful version (§69) | **0 / 12** | A57 |
| Go-live gates (§68) | **0 PASS / 19 FAIL** | A100 |
| Live FIX acceptance (§70) | **0 / 14 FAIL** | A101 |
| Domain compile | **0 errors / 0 warnings** | B01 |
| Live passwords in tree | **NONE FOUND** | A19 |
| Solution membership | **10/10** `.csproj` present | A11 / A88 / B09 |
| Live `NewOrderSingle` | **OFF** (safe by absence, not by a proven flag) | A08 / A49 / A101 |

**Do not claim a trading platform.** Domain algorithms and some Application ports exist. Hosts, EF migrations, FIX sessions, workers, dashboard API, and tests required by §60 are not a first useful version.

### Binding pins this wave must not be walked back

- Pepperstone / cTrader is an **execution venue**, not an LP (A87, A25).
- Generic FIX 4.4 dictionary is **insufficient** (A36).
- Persist `ClOrdID` before send; never retry unknown as `35=D` (A42).
- Discover tag 55; never hardcode (A86).
- Plan-group env is **not** the group-fetch filter (A39, A40).
- Volume wire scale is **10 000**, not hundredths (A81, B14).
- No Kafka / K8s / ClickHouse / LLM / DNN / RL (A80).
- No ML until Phase 6 (A52, A104).
- `REAL_COPY_EXECUTION_ENABLED=false` until A100 + A101 are all PASS.

### Stale A-reports (keep on disk; do not delete)

A01–A06, A09/A10, A11 extras, A19 “no `.gitignore`”, A62 “0 page files”, A65 compose MISSING — superseded by B01–B08, A89, A88/B09, A103, B22, B23, B37.

### Next

Continue B-band to close compile/gap review, then implement only from A30 increments with reviewer + test gates. Do not enable live FIX.

---

## 2026-08-18 — C56 Directory.Build.props measured

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | C56 |
| Purpose | Read `Directory.Build.props`. Record what it actually sets. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\C56_directory_build.md` |
| Product source modified | **No** |
| Path | `D:\Prop\Directory.Build.props` (not under `src/`) |
| SHA-256 | `5ACD33B0F8E1A8D2E66956EF2B04A11E321661A5E3297F8F5C13051345562DD0` (269 B, 9 lines, LF, matches C19/C28; == HEAD) |
| Verdict | **EXISTS_NEEDS_REFACTOR.** Imported by all ten product projects. Sets `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=false`, `Deterministic=true`. No `TargetFramework`. A30 I0 warnings-as-errors **not met**. `Directory.Build.targets` / `Directory.Packages.props` / `global.json` **MISSING**. A11/A30 “MISSING” is stale; A102 plan is not applied. |

---

## 2026-08-18 — C51 ScaleIn long add-lower is averaging down

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | C51 |
| Purpose | Read `TradeReconstructor.ScaleIn` after the long/short averaging-down change. Confirm long add-lower is averaging down. |
| Artifact | `D:\Prop\reports\swarm\20260818\C51_avg_down.md` |
| Product source modified | **No** |
| SUT SHA-256 (WT) | `E20457B398DB6CCC5F78ADE295A340CBC0646F5668F9F79F6AFBCC09D35741DD` |
| Verdict | **CONFIRMED.** Working tree: LONG `price < EntryVwap`, SHORT `price > EntryVwap`, compared **before** VWAP update. `Scale_in_and_partial_close` (0.10 @ 2300 then 0.10 @ 2290) **Passed**. HEAD `6c41447` still inverted (`>` / `<`). Change is **uncommitted**. §60 averaging-down remains PARTIAL (one fused fact; no F07/F08 / add-in-profit / short cell). B08 / A89 G1 stale vs working tree. |

---

## 2026-08-18 — C36 EfDashboardQueries remaining query / perf

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | C36 |
| Purpose | Read `EfDashboardQueries`. Remaining query issues? |
| Artifact | `D:\Prop\reports\swarm\20260818\C36_query_perf.md` |
| Product source modified | **No** |
| SUT | `src/Infrastructure/Dashboard/EfDashboardQueries.cs` — 168 lines / 7407 bytes / SHA-256 `37A4DDD23305708566888F0BBE2A6CC5DE253FB7151BDEE848195DE715EF4ACE` |
| Verdict | **YES — remaining issues.** N+1 on `GetGroupsAsync` (and `GetBrokersAsync`); `GetTradersAsync` full-table + O(n²) account join + no page; `GetTraderAsync` reloads the leaderboard; overview `ToList` of all scores; `destination_quotes` / `shadow_orders` / reject feed have no supporting index. Demo seed hides cost. No `EXPLAIN`. 0 tests. **FAIL as a 5k dashboard read path.** |

---

## 2026-08-18 — C28 SignalR package vs mapped hub

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | C28 |
| Purpose | Confirm whether `apps/api` has a SignalR package and whether any hub is mapped |
| Artifact | `D:\Prop\reports\swarm\20260818\C28_signalr_gap.md` |
| Product source modified | **No** |
| Verdict | **Package YES / hub NO.** Direct `PackageReference` `Microsoft.AspNetCore.SignalR.Common` 8.0.4 (restored, unused). Zero `AddSignalR`, zero `MapHub`, no `Hub` type, no `Hubs/` folder. Binding `/hubs/ops` is **MISSING**. Web stub still targets `/hubs/dashboard` and swallows start failure. Workers correctly do not host SignalR. A97 §0 “weatherforecast host” is stale (C04 hashes still match). |

---

## 2026-08-18 — C27 Redis lease gap

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | C27 |
| Purpose | `StackExchange.Redis` referenced — do workers implement / use a lease? |
| Artifact | `D:\Prop\reports\swarm\20260818\C27_redis_gap.md` |
| Product source modified | **No** |
| Verdict | **Package present, lease absent.** Infrastructure pins `StackExchange.Redis` 2.8.0; DLL sits next to both worker exes. **0** `using StackExchange.Redis` / multiplexer / Lua. `FixSessionOwnership` is an unused process-local `ConcurrentDictionary`. Workers never acquire/renew/release. §28 dual-owner protection is **MISSING** (vacuous `SAFE_BY_ABSENCE` of a TRADE socket only). |

---

## 2026-08-18 — C10 Fake MT5 group-discovery verify

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | C10 |
| Purpose | Read `FakeMt5BrokerConnector.cs`. Is group discovery plan-filtered? |
| Artifact | `D:\Prop\reports\swarm\20260818\C10_fake_mt5_review.md` |
| Product source modified | **No** |
| Verdict | **PASS** — `GetGroupsAsync` returns seeded `_groups` with no `MT5_GROUP_*` / `PlanMapping` intersection. Unmapped `demo\Maxmaster` and `real\standard` remain. This is the required §7/§9 shape, not a missing filter. |
| Not claimed | Live Manager connector; A40 overlay table; A79 discovery unit tests; complete Manager-visible catalog |

---

## 2026-08-18 — C07 workers send-off review

| Item | Value |
|---|---|
| Agent | C07 |
| Artifact | `reports/swarm/20260818/C07_workers_review.md` |
| Question | mt5-worker + fix-worker Program/Worker — real send off? |
| Product source modified | **No** |
| Verdict | **YES — real send is OFF** (`SAFE_BY_ABSENCE`). No `35=D`, no MT5 `SendTrade`. `CTrader:RealCopyExecutionEnabled=true` only logs. Dashboard LoggedOn/Ready is forged. |

### B25 (2026-08-18)

`reports/swarm/20260818/B25_secrets_rescan.md` — 79 product C# + 6 source `appsettings*.json`. **No live passwords.** Empty `CTrader:Password` and empty `ConnectionStrings:TraderIntelligence` in `apps/api/appsettings.json`. Live FIX/MT5 identifiers now in `CTraderFixOptions` / `DemoSeeder` / API JSON. Product source not modified.

### B26 (2026-08-18)

`reports/swarm/20260818/B26_ef_config_break.md` — committed `BrokersConfiguration` + 4 siblings bind missing plural types (`Brokers`, `Mt5Groups`, `Mt5Accounts`, `Mt5Deals`, `Mt5Positions`). Files deleted in WT; HEAD `TraderDbContext` also references 19 missing `DbSet<T>` types and 15 never-created `*Configuration` classes. Product source not edited.

### B39 (2026-08-18)

`reports/swarm/20260818/B39_ml_status.md` — `Get-ChildItem -Force D:\Prop\services` = **0 children**. No `ml-service`, no product Python, no XGBoost. Phase 6 remains closed. C# `BaselineScorer` exists (not A22, not ML). `EfDashboardQueries` hard-codes `mlProbability=null`. Product source not modified.

### B36 (2026-08-18)

`reports/swarm/20260818/B36_risk_fixtures.md` — designed five risk fixture families (stale quote `RF-SQ`, stale signal `RF-SS`, kill switch `RF-KS`, reduce allowed `RF-RA`, real send blocked `RF-RB`). Dual `expect_stub` / `expect_law` lanes. Recording send probe required for `submit_new_count=0`. Product source not modified. No JSON/tests created. G12/G13/G16 remain FAIL.

### C06 (2026-08-18)

`reports/swarm/20260818/C06_dbcontext_review.md` — new `TraderDbContext` has **0 composite PKs** (20× `HasKey(Id)`). Compound identity = 7 unnamed unique indexes. §10 solid only on `mt5_accounts` + `mt5_deals`. `reconstructed_trades` 4-col index is **not unique**. `fix_sessions.Qualifier` is globally unique (wrong). 0 named `*_uk`, 0 compound FKs, 0 migrations. Product source not modified.

### C23 (2026-08-18)

`reports/swarm/20260818/C23_empty_trader.md` — Demo Achiever **10003** (`contest\yo-2step`) has **0 deals / 0 positions**. `DemoSeeder` + `BaselineScorer` persist **`INSUFFICIENT_DATA`** (`N=0`). Measured persist **10 / 90 / 40.00** (empty snapshot still takes SL-rate penalty; B12 `0/100/40` is stale). Leaderboard still emits `EarlyScore=40` (A92 L7 leak). Product source not modified. Eval: `_tmp_c23_empty/stdout.txt` `VERDICT=PASS_INSUFFICIENT_DATA`.

### C17 (2026-08-18)

`reports/swarm/20260818/C17_unit_coverage.md` — `tests/Unit` vs Architecture §60: **0/17 COVERED**, 13 PARTIAL, 4 MISSING (deal dedup, drawdown, MFE/MAE, copy-intent idempotency). Measured `dotnet test` **83** total / **60** pass / **1** fail / **22** skip. Red fact is `Allocation_scales_before_step` (test math: expects `0.10×0.10=0.10`, SUT `0.01`). B08 averaging-down FAIL is stale (SUT polarity fixed). Product source not modified.

### C37 (2026-08-18)

`reports/swarm/20260818/C37_live_copy_page.md` — Architecture §46 **Live Copy Portfolio**: **page missing, chrome not.** `/live` + `LiveCopyPage.tsx` (321 B, SHA `F85CF339…`) + sidebar **`Live`**. No `GET /api/v1/live/portfolio`, no hook, no DTO, no `destination_positions`. A63 parks the GET (out of v1). Do not recreate the file; do not enable send to fill it. Product source not modified.

### C29 (2026-08-18)

`reports/swarm/20260818/C29_migrations_gap.md` — **no** `Migrations/` directory in the product tree. Schema path is `EnsureCreatedAsync` on api + mt5-worker + fix-worker. Default provider is `UseInMemoryDatabase("trader-intelligence")` because connection strings are empty / absent. `UseNpgsql` has no `MigrationsAssembly` and no `Migrate()`. A30 **0/15**. §60 PostgreSQL-migrations tests **0**. Product source not modified.

### C42 (2026-08-18)

`reports/swarm/20260818/C42_honesty_no_live_mt5.md` — Live Achiever and StarwaveFX Manager/HTTP sessions are **NOT proven**. Sole `IMt5BrokerConnector` is `FakeMt5BrokerConnector`; `ConnectAsync` sets `_connected = true`; DI always registers `DemoBrokerFactory.CreateDefault()`. Seeded IPs `57.128.141.65` / `84.201.6.142` are catalog paint. Dashboard `Connected` is literal `true`. C++ `mt5-sdk` is preserved, not wired. A100 G01 remains **FAIL**. Product source not modified.

### C44 (2026-08-18)

`reports/swarm/20260818/C44_honesty_no_ml.md` — **ML is not built, correctly.** `Get-ChildItem -Force D:\Prop\services` = **0 children**. No `ml-service`, no product Python scorer, no XGBoost. Phase 6 remains closed (`ML_NOT_IN_USE`, not `ML_UNAVAILABLE`). C# `BaselineScorer` exists (not A22, not ML). `EfDashboardQueries` hard-codes `mlProbability=null`. Product source not modified.

### C50 (2026-08-18)

`reports/swarm/20260818/C50_http_file.md` — `apps/api/TraderIntelligence.Api.http` **needs update**. Weather/`:5160` leftover is **GONE** (193 B, SHA `2AEC0F4A…`, `@api=:5000`). Live coverage **7/15** maps; **0** `###` separators so the file is one malformed request. Do **not** paste B06 §5.3 `/api/v1` yet (404). Product source not modified.

### C39 (2026-08-18)

`reports/swarm/20260818/C39_models_page.md` — Architecture §46 **Models** page is **missing by design**. No `ModelsPage.tsx`, no `/models`, no `useModels`, no `GET /api/v1/models`. Phase 6 closed; A63 parks `/models` out of v1; A30/A57 allow omitting the nav. Scoring + `BaselineScorer` + `mlProbability=null` is the Phase 0–5 substitute. Do not create the page to “complete” §46. Product source not modified.

### C54 (2026-08-18)

`reports/swarm/20260818/C54_remaining_gaps.md` — Honest remaining gaps vs architecture **§69**. Accepted still **0/12**. Three venue gaps remain: **live MT5** (Fake only; C++ unused; health lie), **live QUOTE logon** (no QuickFIX/n; 15 s `ReadyForMarketData` stamp), **real shadow fills from dest quotes** (`ShadowCopyEngine` unused; seeded bid/ask with null instrument id). Demo reconstruct/score/rank does not flip items 1, 9, 11. Product source not modified.

### D12 (2026-08-18)

`reports/swarm/20260818/D12_scorer_review.md` — Re-read `BaselineScorer.cs` (SHA `ECA2EEE8…`, 8143 B). **No LIVE promotion:** `FromBaseline` reachable set is `{INSUFFICIENT_DATA, RISK_BLOCKED, SHADOW, WATCH, EARLY_SCORE}` only; `CanPromoteToLive(_) => false`; `AfterHighEarlyScore() => SHADOW`; persist copies `SuggestedState` (cannot be LIVE). Vacuous lock, not A22 R5-before-R6. Case B still `WATCH`; mild martingale still `SHADOW`. Product source not modified.

---

## 2026-08-18 — Wave 2 (report recensus + INDEX table)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Wave | **2** |
| Purpose | Recatalog every markdown file under `D:\Prop\reports\swarm\20260818\` into `INDEX.md` as a three-column table (filename, first heading, one-line summary). Wave 1 catalog was a filename list at **141+** while B/C were still landing. |
| Report dir | `D:\Prop\reports\swarm\20260818\` |
| Index | `D:\Prop\reports\INDEX.md` |
| Markdown report count | **236** (A **105** + B **41** + C **60** + D **30**) |
| Product source modified | **No** |

### Inventory (wave-2 snapshot)

| Band | Count | Notes |
|---|---:|---|
| A01–A105 | **105** | Consecutive; no missing A IDs |
| B01–B41 | **41** | Consecutive; Wave 1 B-gaps B11/B17/B28/B34–B36 are closed |
| C01–C60 | **60** | Consecutive; Wave 1 C-band “just started” is closed |
| D-series | **30** | D-band landing (D01–D32 + D35 measured in this catalog; other D IDs still arriving) |
| Scratch | `_tmp_b35_cv/`, `_tmp_b35_score/`, `_tmp_c23_empty/` | Throwaway compile trees, not reports |
| **Report total** | **236** | All `*.md` directly under `swarm/20260818/` |

### What wave 2 measured (do not greenwash)

1. **§69 first useful version** still **accepted 0/12** (A57, C13). Demo Fake+InMemory ingest is not the bar.
2. **§68 go-live** still **0 PASS / 19 FAIL** (A100, C14). Live `NewOrderSingle` stays **off** (C07 `SAFE_BY_ABSENCE`).
3. **§70 live FIX** still **0/14 FAIL** (A101). Live QUOTE/TRADE Logon is **not proven** (C43).
4. Live Achiever/StarwaveFX Manager sessions are **not proven** (C42). Official QuickFIX/n is **not referenced** (C19).
5. ML is **not built**, correctly (B39, C44). Models page missing **by design** (C39).
6. Domain compiles clean (B01). Volume default is **10 000** (B14, D14).

Do not claim a trading platform. Product source was not modified by this cataloger.

---

## 2026-08-18 — D81 LiveCopyPage stub recensus

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D81 |
| Purpose | Read `LiveCopyPage.tsx`. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D81_livepage.md` |
| Product source modified | **No** |
| SUT | `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` |
| SUT SHA-256 | `F85CF339AAD7B2A9F639DA83466DC7949EF765EB3321C985044044978010BC82` |
| Measure | 321 B, 8 lines, 13:20:38, **untracked** vs HEAD `398a142`; same SHA as C37/D08 |
| Verdict | Chrome `/live` + sidebar `Live` exist. A26 §6.10 book **MISSING**. No hook, no GET, no dest table. Flag is a JSX literal. |

---

## 2026-08-18 — D57 scorer MFE fabrication

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | D57 |
| Purpose | Does `BaselineScorer` fabricate MFE? |
| Artifact | `D:\Prop\reports\swarm\20260818\D57_mfe.md` |
| Product source modified | **No** |
| SUT SHA-256 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` |
| Measure | `_tmp_d57_mfe/D57_measured.tsv` — 8 books, all `AvgMfe=NULL`; VWAP 2300/2301 vs 2000/3000 scores identical |
| Verdict | **NO fabrication.** Omit + `FeatureQuality.Unavailable`. Persist/API/web have no MFE columns. Not a PASS of “MFE when valid”; `MfeMaeCalculator` still MISSING. |

---

## 2026-08-18 — E028 `client.ts` baseURL 5000

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | E028 |
| Purpose | Confirm `apps/web/src/api/client.ts` `baseURL` is port 5000. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E028_client.md` |
| Product source modified | **No** |
| SUT | `D:\Prop\apps\web\src\api\client.ts` |
| SUT SHA-256 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` |
| Measure | 232 B, 9 lines, clean vs HEAD `398a142`. `VITE_API_URL` absent. Live Kestrel pid 54468 `:5000` `/health` 200. Worktree launchSettings `:5000`; HEAD still `:5160`. |
| Verdict | **CONFIRMED.** Fallback `http://localhost:5000` is the live axios base. Lab MATCH on worktree + running process. Not A26/A62 catalog client. `\|\|` empty-string trap latent. |

---

## 2026-08-18 — E031 live overview 2 SHADOW / 1 RISK_BLOCKED / 0 LIVE

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:26+05:30 / 13:50:49+05:30 |
| Agent | E031 |
| Purpose | Confirm API overview rollup. Write the report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\E031_overview_live.md` |
| Product source modified | **No** |
| Query SHA-256 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` (unchanged vs D77) |
| Live `GET /api/overview` | HTTP 200: `shadow=2`, `riskBlocked=1`, `live=0`, `liveCandidates=0`, `watch=0`, `realCopyEnabled=false` |
| Cross-check | `/api/traders?state=SHADOW` → 10001+99001; `?state=RISK_BLOCKED` → 10002; `?state=LIVE` → `[]`; 10003 is `INSUFFICIENT_DATA` |
| Verdict | **Confirmed demo fixture, not a live desk.** `/api/v1/overview` still 404. `FromBaseline` cannot emit LIVE. Page still drops `live`. |

---

## 2026-08-18 — R012 local Achiever connect needs HTTP proxy

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:56:14+05:30 |
| Agent | R012 |
| Purpose | Architecture egress `81.29.145.69` + YoPips `.env` HTTP proxy: does local connect need the proxy? Do not copy proxy password. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\R012_proxy.md` |
| Product source modified | **No** |
| Public egress (no proxy) | `106.219.132.213` (ipify / ifconfig.me / icanhazip) |
| Achiever allow-list | `81.29.145.69` |
| TCP | `57.128.141.65:443` OPEN; `81.29.145.69:49527` OPEN; no auth / no Manager logon |
| YoPips evidence | `.env` `MT5_MODE=local` + `MT5_PROXY_TYPE=HTTP`; process used `IS_MT5_PROXY_ENABLED` (absent) → logs `proxy mode: DISABLED` then **1012** |
| Verdict | **YES — local Achiever connect from this workstation needs the HTTP proxy** (or native SNAT as `81.29.145.69`). StarwaveFX does not. |

---

## 2026-08-18 — W500 slot 139 RiskEngine between CopyIntent and ExecutionIntent

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_139 |
| Slot | 139 |
| Purpose | Check whether `RiskEngine` sits between `CopyIntent` and `ExecutionIntent`. Goal: fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_139.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **NO_HOP.** Architecture §4/§32/§39/§75 require CopyIntent → Evaluate → ExecutionIntent. Product drift vs 99: 1 Evaluate caller (`CopyTradingService` L159) + `RiskDecisions.Add` with `AllowFixSend=false` hardcoded; DI registers unused `RiskEngine` singleton; hosted copy every 20s. Still 0 `ExecutionIntent` writers; no `35=D`; `NewOrderSingleImplemented`/`VenueReconciled` const false. Demo `PersistDemoShadowAsync` still bypasses Evaluate. Catalog still ALL groups/users (prior 18/8460). Risk to capital: **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — W500_RESEARCH_140 MT5APIManager.h request APIs work without pump

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_140 |
| Slot | 140 |
| Purpose | Read `MT5APIManager.h` `GroupRequestArray` / `UserRequestArray` / `UserLogins` / `PositionRequestByGroup` / `DealRequestByGroup`. Confirm request APIs work without pump. Fetch ALL Achiever+Starwave groups and ALL manager traders. Copy to cTrader must not send live orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_140.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED.** Five APIs are network RPCs; pump optional (Admin MAIL/NEWS-only enum still has four of five; pool `Connect(...,0)` still calls `UserLogins`). C# request-first, no `_pumpEnabled` branch. Live census 18 groups / 8460 traders / 1984 pos (re-summed; not re-attached). `35=D` absent (`SAFE_BY_ABSENCE`). Residual: DI binds env `REAL_COPY_EXECUTION_ENABLED=true` (slots 80/100/120 hard-false pin is stale); sender still unimplemented. Risk to capital **NONE**. |

---

## 2026-08-18 — W500_RESEARCH_141 YoPips Connect pump-none + proxy IP:port / login:password

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_141 |
| Slot | 141 |
| Purpose | Read YoPips `mt5_manager.cpp` Connect fallback to pump-none and proxy `IP:port` / `login:password`. ALL Achiever+Starwave groups/traders. No live cTrader orders. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_141.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Verdict | **CONFIRMED_WITH_GROUPS_CACHE_GAP.** Fallback `Connect(...,0)` exists. Proxy packs `address=IP:port` `auth=login:password`. Wrapper `pumpMode=0` remaps (omits GROUPS). `GetAllGroups` is cache-only. `UserLogins` is request-complete. YoPips `.env` `MT5_PROXY_ENABLED` unread (`IS_MT5_PROXY_ENABLED`). cTrader `35=D` absent; env `REAL_COPY=true` bound by DI but sender unimplemented. Census 18/8460 prior (re-summed). Risk to capital **NONE**. |

---

## 2026-08-18 — P500_BOOK_13 MFE/MAE FeatureQuality Unavailable

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_13 |
| Slot | 13 |
| Purpose | MFE/MAE `FeatureQuality` is Unavailable. Exact excursion not used. Do not claim MAE-based stops. Measured path to higher profit / lower loss without inventing an edge. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_13.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` / `REAL_COPY` | **Not sent / not enabled** |
| Localhost `/api/overview` + `/api/traders` | **Not remeasured** (SSRF deny on 127.0.0.1). Book integers from `P500_PROFIT_SYNTHESIS.md` pin. |
| Verdict | **FEATURE_QUALITY_UNAVAILABLE; EXACT_EXCURSION_UNUSED; NO_MAE_STOPS.** Scorer always stamps `MaeMfeQuality=Unavailable`; `AverageMfe`/`AverageMae` null; A22 MAE floors not wired; `MfeMaeCalculator` + `mt5_xau_ticks` MISSING. D57 VWAP mutation scores identical. Copy-all **8463** would copy `RISK_BLOCKED` **−$241,580**. Wanting profit is not an edge. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_79 official FIX copier listing is not a send license

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_79 |
| Slot | 79 |
| Purpose | Trade-copier on cTrader FIX is officially listed; Spotware says other APIs may fit copy better. Still no license to send today. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_79.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` boolean) |
| Local API this slot | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave P500 pin. Census re-summed from `LIVE_GROUPS_AND_TRADERS.json` (18/8460). |
| `REAL_COPY` flipped | **No** |
| Verdict | **NO_LICENSE; COPY_ALL_8463_NEGATIVE_EV.** Official https://help.ctrader.com/fix/ lists trade copiers then: “other Spotware APIs are more suitable.” RoE has TRADE `35=D`. Product hop is `35=A` only; `src/`+`apps/` `35=D=0`; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. §68 0/19; §70 0/14. Open API terms still require trader-explicit approval. Copy-all 8463 would copy `RISK_BLOCKED` −$241,580 inside scored XAU −$154,425. Achiever 100% demo/contest; Starwave real = 28. Dest PnL $0. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_72 Persist ClOrdID before send; unknown must not retry (lower-loss, not higher-profit)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_72 |
| Slot | 72 |
| Purpose | Measured evidence for higher profit / lower loss. Topic: persist unique `ClOrdID` before send; unknown state must not retry `35=D`. That is lower-loss, not higher-profit. Honesty: wanting profit ≠ edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_72.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Live API this pass | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-wave P500 pin (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **29 / −$241,580** / dest **$0**). Manager census 18/8460. |
| Verdict | **LOWER_LOSS_NOT_HIGHER_PROFIT; UNKNOWN_MUST_NOT_RETRY.** Domain `MayRetry(AfterSendAttempt/AfterDisconnectUnknown)=false`. Persist-before-send **MISSING** (0 `ExecutionIntent` writers; `ClOrdId` nullable; factory is time+seq). Recovery `35=H`/`AF`/`AN` = **0**. Copy-all 8463 would import `RISK_BLOCKED` −$241,580. Dest PnL **$0**. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_99 official FIX lists trade copiers; still no send license

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_99 |
| Slot | 99 |
| Purpose | Trade-copier on cTrader FIX is officially listed; Spotware says other APIs may fit copy better. Still no license to send today. Measured evidence for higher profit / lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_99.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true`) |
| Local API | `GET :5000/api/overview` + `/api/traders` blocked (SSRF). Book pin = synthesis 8463 / Manager 8460 / RISK_BLOCKED 29 / −$241,580 / dest $0. |
| Verdict | **NO_LICENSE; COPY_ALL_8463_NEGATIVE_EV.** Official https://help.ctrader.com/fix/ lists trade copiers then: “other Spotware APIs are more suitable.” RoE has TRADE `35=D`. Product hop is `35=A` only; `src/`+`apps/` `35=D=0`; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`. §68 0/19; §70 0/14. Open API terms still require trader-explicit approval. Copy-all 8463 would copy `RISK_BLOCKED` −$241,580 inside scored XAU −$154,425. Achiever 100% demo/contest; Starwave real = 28. Dest PnL $0. Risk to capital **NONE** (`SAFE_BY_ABSENCE`). |

---

## 2026-08-18 — P500_BOOK_160 quality 95.50 vs negative netSourcePnl

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_160 |
| Slot | 160 |
| Purpose | Read `BaselineScorer.cs`. Recalculate how quality 95.50 can coexist with negative `netSourcePnl`. Quote the formula. Measured evidence for higher dest profit / lower dest loss. Honesty: wanting profit ≠ edge; copy-all 8463 copies `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_160.md` |
| Product source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** |
| Local API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (loopback SSRF). Book integers from same-day `P500_PROFIT_SYNTHESIS.md`; 302252/303174 dollars re-checked against `LIVE_GROUPS_AND_TRADERS.json` balances 931.54 / 970.62. Manager census re-summed 18/8460. |
| Verdict | **95.50_IS_XAU_SHAPE_NOT_PROFIT.** Formula `50 + 15 I_net + 10 I_12 + 5 I_18 + 0.20 b − 0.25 r`. Unique lattice `(b,r)=(90,10)` with `I_net=I_12=I_18=1`. **Cannot** sit on negative XAU `features.NetPnl` (`quality_max(I_net=0)=70`). **Can** sit on negative dashboard `netSourcePnl` because `GetTradersAsync` sums **all completed symbols**. Existence: 302252 SHADOW 95.50 / −68.46; 303174 SHADOW 95.50 / −29.38. Live ingest forces unused-SL (`Mt5Deal` has no SL). HEAD policy **requires** demo/contest (`CopyGroupFilter`) and still ignores dashboard PnL. Copy-all 8463 copies 29 `RISK_BLOCKED` names (source tail **−$241,580**) inside scored XAU **−$154,425**. Dest PnL **$0** (`SAFE_BY_ABSENCE`). Risk to capital **NONE** this process. |

---

## 2026-08-18 — P500_BOOK_95 In-memory DB: scores vanish on restart; cannot run a live book

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_BOOK_95 |
| Slot | 95 |
| Purpose | In-memory DB: scores vanish on restart. Cannot run a live book on that. Measured evidence for higher profit and lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_95.md` |
| Product source modified | **No** |
| Secret values printed | **None** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Live API this slot | `GET :5000/api/overview` and `/api/traders` **blocked** (localhost SSRF). Book integers = same-day pin (8463 / scored XAU **−$154,425** / `RISK_BLOCKED` **−$241,580** / dest $0). Restart wipe pin ~09:01Z. |
| Verdict | **BLOCK_LIVE_BOOK_ON_INMEMORY.** DI fail-opens to `UseInMemoryDatabase("trader-intelligence-live")` when `DATABASE_URL` contains `<SECRET>`. Scores / RISK_BLOCKED / intents die on restart (synthesis ~09:01Z wipe). Copy-all 8463 EV = scored XAU **−$154k**; blocked tail **−$241k** (29 martingale). Dest PnL **$0**. `35=D` absent. Risk to capital **NONE** today (`SAFE_BY_ABSENCE`); copy-all if send existed = **HIGH expected dest loss**. |

---

## 2026-08-18 — P500_VERIFY_50 adversarial profit-path (slot 50)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_50 |
| Slot | 50 |
| Purpose | Adversarial confirm: no 35=D builder; CanPromoteToLive false; RealCopyEnabled forced false after logon; sending now cannot be the profit path; SHADOW on demo is not dest profit. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_50.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** this slot (on-disk prior fill cited only) |
| `REAL_COPY` flipped | **No** |
| Secret values printed | **None** (quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`) |
| Live API this pass | `GET :5000/api/health` `/api/copy/status` `/api/settings` **blocked** (localhost SSRF) |
| Verdict | **FAIL.** Claim 2 PASS. Claim 1 PASS only on `CTraderFixSession`. Claims 3–4 FAIL. Claim 5 PASS_PAPER / FAIL absolute. Risk: live `1369850` **NONE**; demo dest **P&L active**. |

---
