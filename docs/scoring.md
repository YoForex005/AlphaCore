# Deterministic scoring

Trade #3 completed XAUUSD ⇒ `EARLY_SCORE_ELIGIBLE`.

Outputs: `risk_score`, `behavior_score`, `early_quality_score`.

High quality + low risk ⇒ `SHADOW`, never `LIVE`.

Martingale / large sequential size-up after losses ⇒ `RISK_BLOCKED`.

ML is Phase 6 and must beat this baseline out of sample.
