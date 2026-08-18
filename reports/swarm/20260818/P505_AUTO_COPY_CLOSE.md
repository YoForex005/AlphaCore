# P505 — Why Live Copy showed blockers, and auto open/close

The Live Copy page was **not** the demo fill. It reports the API pipeline:

| Tile | Meaning |
|---|---|
| Copy pipeline ON. Shadow intents only | `CopyTradingService` was writing `SHADOW_ONLY` rows. Product `NewOrderSingleImplemented` was **false**. |
| REAL_COPY armed YES | Env flag true. A flag is not a sender. |
| SHADOW 390 / LIVE 0 | Scorer never promotes to LIVE (`CanPromoteToLive => false`). |
| Live sends 0 | `ExecutionIntent.SentAt` count — the tool fill never wrote that table. |
| Intents 378 | Paper copy/roster rows in the in-memory DB. |
| Shadow fills 0 | No dest quote tape, so shadow simulator does not fill. |
| QUOTE/TRADE up | One-shot logon probe succeeded. |
| SAFE_BY_ABSENCE | No `35=D` in the API hop (old). |
| Venue not reconciled | `VenueReconciled` const false. |
| 0 traders in LIVE | Trade #3 cannot auto-LIVE. **Not a crash.** |

The 305750 fill was a **manual** `--copy-open` because that hop was not wired.

**Now wired:**

- `ExecuteDemoCopyAsync` on the 20s hosted tick (demo FIX host only).
- Opens: ADMITTED + open XAU ≤ 0.05 lot, not already in `data/demo_copy_ledger.json`.
- Closes: when MT5 reconstructed trade `Completed`, send dest close on tag `721`.
- Ledger seeded with 305750 / 21250421 → dest 237339770.
- `--watch` loop polls the API and auto-closes that dest when 305750 closes on MT5.
- Live `1369850` still refused.

API must be **rebuilt/restarted** to pick up the hosted tick. Watch loop covers close without waiting for that.
