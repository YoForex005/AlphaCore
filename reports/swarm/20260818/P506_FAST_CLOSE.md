# P506 — Senior close path: Manager positions, not 0.5s HTTP

Polling reconstructed `/api/traders` is limited by **one-shot ingest**. After scoring, deals are not refreshed, so `completed=false` stays false.

**Fast path:** keep Manager connected; every 500ms `GetPositionsAsync` for ledger logins only; if `PositionTicket` missing and the probe succeeded, dest close.

Measured: **312762 / 21251046** dest **237342655** closed @ **4394.76**. API still reported `completed=false`. HTTP watch would have left dest open.

Do not flatten on empty/error position replies (fail-open on probe fail).

**Shipped:** `tools/FastCopyWatch` is the running closer. Independent review of ingest + HTTP watch agrees: speeding the reconstructed flag cannot see master exits.
