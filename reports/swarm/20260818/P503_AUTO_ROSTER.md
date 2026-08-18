# P503 — Auto-admit traders, auto-remove losers, flatten dest only

| Field | Value |
|---|---|
| Date | 2026-08-18 |
| Engine | `src/Domain/Copy/CopyRosterEngine.cs` |
| Tick | `CopyTradingService.TickRosterAsync` + hosted 20s loop |
| Tests | **21/21 PASS** (8 roster + 13 one-to-one) |
| Orchestrator | workflow `copy-roster-harden` (60 agents) |
| Live 35=D | still unimplemented — flatten is **intent** `FLATTEN_LOSS_CUT` |

## What the stronger backend does

Every ~20s:

1. **Auto-add** any trader who now meets eligibility (`SHADOW+`, ≥20 XAU, XAU PnL>0, no size pattern, **demo/contest group only**) → roster row `ADMITTED`.
2. **Auto-remove** if they later fail: book ≤0, 3 consecutive XAU losses, peak drawdown ≥40%, martingale/averaging/lot-escalation, RISK_BLOCKED/PAUSED/DISQUALIFIED, **or not a demo/contest group**.
3. On remove: emit `CloseExposure` for every dest open copy of that login (`FLATTEN_LOSS_CUT`). **Never** flatten MT5 source.
4. New opens only if roster status is `ADMITTED`.
5. Single-copy unrealized loss cap $150 (`ShouldFlattenOpenCopy`) is ready; dest mark-to-market still needs a quote tape.

## What it does **not** do

- Guarantee profit.
- Instant millisecond flatten (still a poll).
- Live Pepperstone send (no `35=D`).
- Admit **real/live** groups (`NOT_DEMO_OR_CONTEST_GROUP`). This book is **demo/contest only**.
