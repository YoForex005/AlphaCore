# A006 — Volume scale, scoring, risk, and copy-quantity gates

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\A006_volume_scoring_risk.md` |
| Agent | A006 (volume / scoring / risk confirmation) |
| Date | 2026-08-18 |
| Product source modified | **No.** Report only. |
| Secrets printed | **No.** No passwords, `.env` values, FIX SubIDs, or live tokens. |
| Authority | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§1, 15–18, 22–23, 37–41 |
| SUTs | `VolumeConverter`, `TradeReconstructor`, `BaselineScorer` / `TraderStateMachine`, `RiskEngine`, `ShadowCopyEngine`, `QuantityNormalizer` |

**One-line:** Classic Manager volume is **÷ 10_000**; trade #3 is **SHADOW / WATCH / EARLY_SCORE / RISK_BLOCKED**, never LIVE; MFE/MAE stay **Unavailable/null**; lots are **not** a FIX `OrderQty`; `RiskEngine` is the only legal FIX-send authority; `REAL_COPY` defaults **false**. Pipeline wiring is still incomplete — fail-closed by absence, not a full live path.

---

## 0. Assigned confirmations (binding)

These six statements are the task. Verdicts are measured from product source + architecture, not from prior reports.

| # | Claim | Verdict | Evidence class |
|---|---|---|---|
| C1 | Volume scale is **10_000** (classic `Volume()`, not hundredths, not `VolumeExt`) | **CONFIRMED** | `VolumeConverter.ManagerVolumeScale = 10_000m`; reconstructor default `VolumeConverter.Manager` |
| C2 | Trade #3 never auto-promotes to **LIVE** | **CONFIRMED** | `FromBaseline` never returns `LIVE` / `LIVE_CANDIDATE`; `CanPromoteToLive` is unconditionally `false` |
| C3 | Never invent **MFE/MAE** from closed deals | **CONFIRMED** | Scorer always writes `MaeMfeQuality = Unavailable` and leaves `AverageMfe` / `AverageMae` null |
| C4 | Never convert MT5 lots blindly to FIX **OrderQty** | **CONFIRMED as policy + last-stage helper; NOT as a complete converter** | `QuantityNormalizer` is dest min/step/max only. No `IQuantityConverter`. No NOS builder. Shadow demo still copies `MaxVolumeLots` 1:1 into `CopyIntent` (not FIX). |
| C5 | Risk sits **between score and FIX** | **CONFIRMED as design + domain law; NOT wired in DI / live send** | `RiskEngine.AllowFixSend` requires `RealExecutionEnabled && KillSwitch.None && Reconciled && VenueHealthy`. Not registered in `AddTraderIntelligence`. Scoring → demo shadow skips risk. |
| C6 | **`REAL_COPY` default is false** | **CONFIRMED** | `CTraderFixOptions.RealCopyExecutionEnabled = false`; API `featureFlags.REAL_COPY_EXECUTION_ENABLED = false`; FIX worker `GetValue(..., false)` |

Do **not** read C4/C5 as “live sizing/risk pipeline is production-ready.” They are **safe-by-gate + safe-by-absence**.

---

## 1. Architecture law (quoted intent, no secrets)

Source: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`

| § | Binding rule |
|---|---|
| §1.2 | Three trades are not skill. Trade #3 is an *early* score, not classification. |
| §1.4 | Do **not** send real money after trade #3. Default after a strong early score is **SHADOW**. |
| §1.5 | Do **not** calculate MFE/MAE from closed deals alone. No ticks while open → do not fabricate. |
| §1.8 | A hard risk engine **must** sit between scoring and execution. |
| §1.10 | **Never** blindly convert MT5 lots into cTrader `OrderQty`. |
| §14–15 | One reconstructed trade = one position lifecycle back to flat (or a reversal close). Count only **3 completed reconstructed XAUUSD** lifecycles. Trade #3 → `EARLY_SCORE_ELIGIBLE`, not `PROVEN_PROFITABLE`. |
| §17 | Exact MFE/MAE needs source-side ticks. Persist `price_source` + `feature_quality`. Never pretend cTrader quotes are the source tape. |
| §18 | Deterministic baseline first: `risk_score`, `behavior_score`, `early_quality_score`. ML later must beat it. |
| §22–23 | Trade #3 + high score → **SHADOW only**. No automatic real capital. |
| §37–38 | Quote/spread/move guards. Sizing: source volume → notional/risk → allocation → dest qty (min/step/max). |
| §39 | Scoring/ML may only emit candidate / confidence / suggested allocation. Risk is final: approve / reduce / reject / pause trader / pause venue / global stop. |
| §40–41 | `STOP_NEW_EXECUTION` ≠ `EMERGENCY_FLATTEN`. Default `REAL_COPY_EXECUTION_ENABLED=false`. NOS requires the flag **and** a healthy risk engine. |

Correct pipeline (architecture §1):

```text
MT5 deals → reconstruct → XAU features/score → SHADOW
    → copy intent → RiskEngine → execution intent → FIX TRADE
```

Scoring is **not** allowed to emit `NewOrderSingle`.

---

## 2. VolumeConverter — scale 10_000

File: `D:\Prop\src\Domain\Volume\VolumeConverter.cs`

```text
ManagerVolumeScale   = 10_000          // IMTDeal::Volume() / SMTMath::VolumeToDouble
ExtendedVolumeScale  = 100_000_000     // IMTDeal::VolumeExt()
HundredthsScale      = 100             // MT4 comment in mt5_types.h — WRONG for this product
```

| Method | Formula |
|---|---|
| `ToLots(native)` | `native / Scale` |
| `ToNative(lots)` | `round(lots * Scale)` away-from-zero; rejects negative lots |
| Default ctor | `ManagerVolumeScale` |

Comment in the class is the measured unit law: existing `mt5_manager.cpp` copies `deal->Volume()`, so ingested integers are classic 4-decimal units. `1.00 lot = 10_000`. `0.10 lot = 1_000`.

Locked by `D:\Prop\tests\Unit\VolumeConverterTests.cs`:

- `Manager.Scale == 10_000`
- `ToNative(0.10m) == 1000`
- `ToLots(1000) == 0.10m`
- `Extended` maps `1` lot ↔ `100_000_000`
- `Manager.Scale != HundredthsScale`

**Do not divide by 100.** That overstates lots by **100×**. **Do not divide `Volume()` by 100_000_000.** That understates lots by **10_000×**.

`TradeReconstructor` injects `volume ?? VolumeConverter.Manager`. Every `NormalizedDeal.VolumeNative` becomes lots only through this converter (`ReconstructPosition` L89).

---

## 3. TradeReconstructor — lots in, completed XAU out

File: `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs`  
Inputs: `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs` (`VolumeNative` is `ulong`, never “lots”).  
Outputs: `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs`

What it **does**:

- Scope by `brokerId` + `login`.
- Drop canceled-deal position ids from first-three eligibility (`EligibleForFirstThree = false`).
- Keep only `IsTradingDeal` (`Buy`/`Sell` + `DealReasons.CountsAsTraderActivity`).
- Group by `PositionId`. Apply `In` / `Out` / `OutBy` / `InOut` (reversal).
- Convert volume **once**: `_volume.ToLots(deal.VolumeNative)`.
- Persist VWAP, max/initial/closed/remaining **lots**, deal PnL + commission + swap.
- Detect scale-in, partial close, averaging-down (add at a worse price vs entry VWAP).

What it **does not** do:

- No MFE/MAE, no tick walk, no destination quantity, no FIX, no score, no LIVE flag.
- `Fees` is hard `0m` (honest zero, not invented venue fees).
- `CompletedXauUsdTrades` requires `Completed && IsXauUsd && EligibleForFirstThree`.
- `IsEarlyScoreEligible` is **`Count >= 3`**, not “promote to LIVE”.

**Honesty gap:** `ReconstructionScoringService.RebuildTraderAsync` scores `trades.Where(Completed && IsXauUsd)` and **does not** apply `EligibleForFirstThree`. Dirty/canceled positions can still enter the scorer even though `CompletedXauUsdTrades` excludes them. First-three **count API** is stricter than the **score path**.

---

## 4. BaselineScorer — trade #3 is never LIVE

Files: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`, `D:\Prop\src\Domain\Enums\TraderState.cs`

`EarlyScoreTradeCount = 3`. Features are computed only from `Completed && IsXauUsd`.

| Output | Role |
|---|---|
| `RiskScore` | Additive 0–100 (martingale +35, avg-down +20, lot escalation +15, lot CV +10, low SL +10, DD > gross profit +10) |
| `BehaviorScore` | 100 minus behavior penalties, clamped |
| `EarlyQualityScore` | 50 ± PnL/PF/behavior/risk; **capped at 40** if `n < 3` |
| `EarlyScoreEligible` | `n >= 3` |
| `SuggestedState` | `TraderStateMachine.FromBaseline` |

`FromBaseline` legal results:

| Condition | State |
|---|---|
| `n == 0` | `INSUFFICIENT_DATA` |
| `risk >= 80` **or** (martingale ∧ DD ∧ net < 0) | `RISK_BLOCKED` |
| `n < 3` | `INSUFFICIENT_DATA` |
| `quality >= 70` and `risk < 40` | **`SHADOW`** |
| `quality >= 55` | `WATCH` |
| else | `EARLY_SCORE` |

**Never returned:** `LIVE`, `LIVE_CANDIDATE`, `PAUSED`, `DISQUALIFIED`.

```211:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static bool CanPromoteToLive(TraderState current) => false;
```

Locked by `D:\Prop\tests\Unit\BaselineScorerTests.cs`:

- 2 trades → `INSUFFICIENT_DATA`, not eligible.
- 3 disciplined winners → `SHADOW`, `CanPromoteToLive == false`.
- Martingale losers → `RISK_BLOCKED`.

This is architecture §1.4 / §23 implemented as a **hard function**, not a comment.

`TraderScore` persistence (`D:\Prop\src\Domain\Entities\TraderScore.cs`) stores the three scores, trade count, three risk flags, and `CurrentState`. It has **no** MFE/MAE columns.

---

## 5. Never invent MFE/MAE

Architecture §1.5 / §17: excursion features require a source tick tape **while the position is open**. Closed-deal reconstruction cannot invent them.

`FeatureSnapshot` *can* carry:

- `MaeMfeQuality` (default `FeatureQuality.Unavailable`)
- `AverageMfe` / `AverageMae` (`decimal?`)
- `PriceSource` (default `Unknown`)

`ComputeFeatures` **always** writes:

```text
MaeMfeQuality = FeatureQuality.Unavailable
PriceSource   = Unknown
```

and **never assigns** `AverageMfe` / `AverageMae` (they remain null).

There is no tick ledger in this path. Destination quotes are not substituted. `PriceSource.CTraderQuoteSession` exists on the enum and is **not** used by the scorer.

If a later layer publishes a number with `Unavailable` or a cTrader quote labeled as source MT5, that is a **policy break**. Current scorer does not do that.

---

## 6. QuantityNormalizer — not a FIX OrderQty mapper

File: `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs`

```text
raw  = sourceLots * allocationFactor
qty  = truncate(raw / step) * step
qty  = round(qty, precision, ToZero)
if qty < dest.MinQuantity → 0
if qty > dest.MaxQuantity → dest.Max
```

This is the **last-stage dest grid** (architecture §38 tail). It is **not**:

- source native ticks → lots (that is `VolumeConverter`)
- lots × contract size → ounces / units
- Lots vs BaseUnits convention
- confidence / margin / risk-cap
- a FIX tag 38 writer

`InstrumentQuantitySpec` has min/max/step/precision only. No `contract_size`, no `QuantityConvention`, no `spec_status`.

`D:\Prop\tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs` **skips** the A43 `IQuantityConverter` cases and states the honest gap: *“QuantityNormalizer is unused by ShadowCopyEngine and RiskEngine”* and *“No FIX NOS builder consumes QuantityNormalizer output.”*

**Blind-lots prohibition status:**

| Layer | What happens |
|---|---|
| Domain helper | Exists; does not claim dest units == MT5 lots |
| Shadow demo (`PersistDemoShadowAsync`) | `RequestedQuantity = trade.MaxVolumeLots` — **1:1 lots copy into a SHADOW_ONLY intent**, not into FIX |
| `ShadowCopyEngine` | Uses the quantity it is given; no dest spec |
| FIX TRADE | **No NewOrderSingle send path** in product workers |

So: we do **not** emit FIX `OrderQty` from lots. We also do **not** yet have the §38 converter the architecture requires before any live send.

---

## 7. RiskEngine — between score and FIX (when FIX is allowed)

File: `D:\Prop\src\Domain\Risk\RiskEngine.cs`

Scoring outputs a **candidate**. Risk is the only object that may set `AllowFixSend`.

`AllowFixSend` is true **only** if all of:

```text
RealExecutionEnabled == true
KillSwitch == None
Reconciled == true
VenueHealthy == true
```

and the request was not rejected.

Rejects (increasing actions unless noted): kill switch, unreconciled venue, unhealthy venue, missing/stale quote, wide spread, price move, stale signal, max loss / daily loss / portfolio DD, max positions / qty / XAU gross, net exposure (`ReduceSize`), margin, martingale, abnormal sizing.

Reducing actions (`ReduceExposure` / `CloseExposure`) can approve through `STOP_NEW_EXECUTION` (architecture §40: stop new, leave existing). `AllowFixSend` still requires the real-copy conjunction above. Tests lock: default `RealExecutionEnabled = false` → `Approve` **and** `AllowFixSend == false`.

Empty comment at L91–93 documents the shadow path: *evaluate, never allow FIX send.*

**Wiring honesty:**

- `RiskEngine` is **not** registered in `D:\Prop\src\Infrastructure\DependencyInjection.cs`.
- `ReconstructionScoringService` scores then `PersistDemoShadowAsync` — **no** `Evaluate`.
- `apps/fix-worker/Worker.cs` does not call `RiskEngine`. It refuses NOS even if config is flipped, and logs a warning.
- Safe today because **there is no live send**. Not because the score→intent→risk→FIX chain is complete.

---

## 8. ShadowCopyEngine — modeled fills, not live

File: `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`

- Entry: buy at ask / sell at bid; +/− `0.05` points if modeled delay > 250 ms.
- Exit: flatten at the opposite side.
- `MarkToMarket` on dest quote.

No FIX, no `OrderQty`, no promotion. Demo persistence only writes `CopyIntent` / `ShadowOrder` when `state == TraderState.SHADOW`, status literal `"SHADOW_ONLY"`. Quantity is still source `MaxVolumeLots` (see §6).

---

## 9. REAL_COPY default false

Measured product defaults (no secret values):

| Location | Default |
|---|---|
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | `RealCopyExecutionEnabled { get; set; } = false` — comment: NOS default OFF |
| `D:\Prop\apps\api\Program.cs` | `/api/settings` hardcodes `featureFlags["REAL_COPY_EXECUTION_ENABLED"] = false` |
| `D:\Prop\apps\fix-worker\Worker.cs` | `GetValue("CTrader:RealCopyExecutionEnabled", false)` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | When false: `"NewOrderSingle disabled. SHADOW/CopyIntent only."` |
| Architecture §41 / §56 example block | `REAL_COPY_EXECUTION_ENABLED=false` |

Committed config and the public settings API must stay false until A100/A101 gates pass. Enabling the flag **still** must not send until risk + reconcile + ownership pass. Current worker refuses NOS even if the flag is true.

Do not log `CTRADER_FIX_PASSWORD`, SenderSubID, or account passwords. `CTraderFixLogonHostedService` may connect QUOTE/TRADE for session proof; it logs `NewOrderSingle still disabled`.

---

## 10. Pipeline map (measured vs required)

```text
NormalizedDeal.VolumeNative
        │  VolumeConverter.Manager  (÷ 10_000)
        ▼
TradeReconstructor  (lots, VWAP, flags, completed XAU)
        │
        ▼
BaselineScorer      (no MFE/MAE; n<3 insufficient; best case SHADOW)
        │
        ▼
TraderScore + PersistDemoShadowAsync  (SHADOW_ONLY intents)
        │
        ✗  RiskEngine not invoked
        ✗  QuantityNormalizer not invoked
        ✗  No ExecutionIntent / NewOrderSingle
        ▼
FIX TRADE session (optional logon) — send remains off
```

Required before any live copy:

1. Keep scale **10_000** on Manager `Volume()`.
2. Keep trade #3 off LIVE (`CanPromoteToLive` stays false until an explicit later gate exists **and** is tested).
3. Keep MFE/MAE null unless `feature_quality == Exact` on a real source tape.
4. Implement `IQuantityConverter` (contract size + dest convention) **before** tag 38.
5. Insert `RiskEngine.Evaluate` between copy intent and FIX send; register it in DI.
6. Keep `REAL_COPY_EXECUTION_ENABLED=false` in committed config and the settings API.

---

## 11. Tests that pin this report

| Test | Pin |
|---|---|
| `D:\Prop\tests\Unit\VolumeConverterTests.cs` | Scale 10_000; 0.10 lots ↔ 1000; not hundredths |
| `D:\Prop\tests\Unit\TradeReconstructionTests.cs` | Reconstructor uses `VolumeConverter.Manager` |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | n=3 → SHADOW; `CanPromoteToLive == false` |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | `RealExecutionEnabled=false` ⇒ `AllowFixSend=false`; stale quote / kill switch / unreconciled |
| `D:\Prop\tests\Unit\ExecutionAndSizingTests.cs` | Dest step/min only; no NOS retry after unknown ack |
| `D:\Prop\tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs` | Documents missing `IQuantityConverter` (skipped) |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | Seeded score state is not `LIVE` |

---

## 12. Gaps (honest, not blockers for the six claims)

1. Scorer first-three set ≠ `CompletedXauUsdTrades` (`EligibleForFirstThree` dropped on the score path).
2. `IQuantityConverter` missing; demo shadow copies lots 1:1 into intents.
3. `RiskEngine` / `QuantityNormalizer` / `ShadowCopyEngine` not in DI (except ad-hoc `new ShadowCopyEngine()` in the store).
4. No tick store → MFE/MAE correctly **absent**, not “exact.”
5. No live FIX send. Safety is default-off + missing pipeline, which is correct for now.

None of these reverse C1–C3 or C6. C4 and C5 are **policy confirmed**, implementation **partial**.

---

## 13. Verdict for the swarm log

**PASS on the six assigned invariants as product law.**

- Volume = **10_000**.
- Trade 3 = **never auto LIVE**.
- MFE/MAE = **never invented**.
- Lots ≠ FIX `OrderQty` (no NOS; helper is dest-grid only).
- Risk is the **only** FIX-send authority (`AllowFixSend`).
- `REAL_COPY` default = **false**.

Do not claim “live copy is risk-gated in production.” Claim: **live copy cannot fire from these components as they stand.**
