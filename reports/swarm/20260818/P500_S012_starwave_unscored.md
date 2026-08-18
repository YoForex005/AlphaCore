# P500_S012 — Starwave book is ingested and unscored

| Field | Value |
|---|---|
| Slot | S012 |
| Evidence | live `/api/ingest/status`, `LiveIngestHostedService` |

STARWAVEFX: `dealsInserted≈91966`, `scored=0`, `phase=deals-done`. Achiever is still in `scoring`. Hosted service scores `ListLoginsWithDealsAsync` **per broker sequentially** (Achiever first).

Dashboard SHADOW set is **Achiever-demo only**. Using it as the live copy set is selection bias.

## Profit implication

Do not size Pepperstone from an incomplete book. Wait for Starwave scores; still apply demo/real filters.
