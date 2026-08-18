# P500_VERIFY_20 — Adversarial verifier (slot 20)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_20.md` |
| Agent / slot | P500 **VERIFY 20** (adversarial; did not trust sibling reports) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned reads | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` (full files, this slot) |
| Supporting reads | `CTraderFixLogonHostedService.cs`, `LiveRuntimeStatus.cs`, `DependencyInjection.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs` (header + `Build("D")`), `CTraderFixOptions.cs`, `EnvFile.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `CopyGroupFilter.cs`, `XauUsdOneToOneCopyPolicy.cs`, `ShadowCopyEngine.cs`, `TraderState.cs`, `apps/api/Program.cs`, `.env` **L70–73 only** (boolean), `data/demo_copy_ledger.json` |
| Product / test / `.env` modified | **No** |
| Live attach | **No.** No Manager, no TLS order, no `35=D` this slot. |
| Live GET | **Blocked.** `GET http://127.0.0.1:5000/api/health` and `/api/copy/status` → SSRF deny. Claims that need a live process value are **unproven**, not assumed. |
| Binding rule | **FAIL any claim that cannot be proven from a file read this slot or a live GET.** Prior swarm dollars / SHADOW counts are **not** evidence here. Never print secrets (no tag 554, no passwords). |

**Honesty:** A logon `35=A` is not a fill. An armed flag is not a ticket. Source `SHADOW` dollars are not destination realized PnL. Wanting profit is not an edge. `SAFE_BY_ABSENCE` applies to live `1369850` only.

---

## 0. Verdict (binding)

**FAIL.**

Four of five assigned claims are proven from files. Claim **3** (`RealCopyEnabled` forced false after logon) is **disproven**. A product-wide reading of claim **1** is also false: the assigned session file has no `35=D` builder, but a sibling in the same folder does, and the 20 s hosted tick calls it.

| # | Claim | Result | Class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** on `CTraderFixSession.cs` (135/135). **FAIL** as a product-wide statement | session has only `(35,"A")`; sibling `CTraderFixCopyOpen.Build("D")` exists |
| 2 | `CanPromoteToLive` is false | **PASS** | constant `=> false`; `FromBaseline` never returns `LIVE` |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | logon never writes the bit; lab `.env` L73=`true`; DI copies it |
| 4 | sending now cannot be the profit path | **PASS** (as dest-profit, not as “cannot send”) | session/page/risk persist cannot book dest profit; demo hop ≠ dest EV |
| 5 | SHADOW on demo is not destination profit | **PASS** | source state + paper slippage; DTO `DestinationRealPnl` is literal `0` |

One-line:

```text
FAIL. CTraderFixSession is 35=A only (PASS scoped). CanPromoteToLive=false (PASS). RealCopyEnabled NOT forced false after logon (.env true + DI; logon only logs) — FAIL. Sending is not dest profit (PASS); product still has demo Build("D") on 20s tick. SHADOW/demo is source/paper, not dest PnL (PASS). Live GET blocked. Live 1369850 refused. Demo dest not SAFE_BY_ABSENCE.
```

Risk to capital this slot: **NONE on live `1369850`** (`CTraderFixCopyOpen` hard-refuse). **Not absent** on demo dest if the API host is running (`ExecuteDemoCopyAsync` ignores `RealCopyEnabled`). This slot sent **0**.

---

## 1. no 35=D builder — PASS (session) / FAIL (product)

Assigned file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` is **135/135**. Single outbound builder is `BuildLogon`. The only `35` value constructed is `"A"`. Single `WriteAsync` sends that logon. Socket is `using`/`await using` and disposed after one read. There is no `Build("D")`, no NewOrderSingle, no ClOrdID, no tag 11/38/54/40.

```94:108:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            (49, sender),
            (56, target),
            (50, senderSub),
            (57, targetSub),
            (52, sendingTime),
            (98, "0"),
            (108, "30"),
            (141, "Y"),
            (553, username),
            (554, password)
        };
        return Assemble(fields);
```

```46:50:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
            var seq = 1;
            var logon = BuildLogon(senderCompId, targetCompId, senderSubId, targetSubId, username, password, seq);
            var bytes = Encoding.ASCII.GetBytes(logon);
            await ssl.WriteAsync(bytes, timeoutCts.Token);
            await ssl.FlushAsync(timeoutCts.Token);
```

Reply handling accepts `35=A` as logon-ok or records reject text. No second write.

**Residual (why a product-wide “no builder” claim FAILs):** same folder `CTraderFixCopyOpen.cs` L142–156 is a generic `Build(string type, …)` and L95 sends `Build("D", …)` after a TRADE logon + SecurityList. Hosted caller:

```28:30:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

`ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.SendAsync` (open L566, close L528). Live account is refused (`account == "1369850"`). `CTraderFixDemoTestTrade` also `Build("D")` (flatten + open); callers are `tools/DemoFixTestTrade` only, not the hosted tick.

`CTraderFixSession` itself is **not** a `35=D` builder. Confirming “no `35=D` builder anywhere” would be a lie.

---

## 2. CanPromoteToLive is false — PASS

Proven in assigned `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` L211. Parameter is ignored.

```189:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState FromBaseline(bool earlyEligible, decimal quality, decimal risk, FeatureSnapshot features)
    {
        if (features.CompletedXauTrades == 0)
            return TraderState.INSUFFICIENT_DATA;

        if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
            return TraderState.RISK_BLOCKED;

        if (!earlyEligible)
            return TraderState.INSUFFICIENT_DATA;

        if (quality >= 70 && risk < 40)
            return TraderState.SHADOW;

        if (quality >= 55)
            return TraderState.WATCH;

        return TraderState.EARLY_SCORE;
    }

    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

Ceiling of `FromBaseline` is `SHADOW`. Enum still lists `LIVE_CANDIDATE` / `LIVE` (`TraderState.cs` L9–10) but this machine never emits them. Unit lock: `tests/Unit/BaselineScorerTests.cs` L21–26 — three winners → `SHADOW` and `CanPromoteToLive` false.

Grep of product `*.cs` (excluding `_tmp_*`): the only other `CanPromoteToLive` hit is that unit test. No caller sets `TraderState.LIVE` from this function.

---

## 3. RealCopyEnabled forced false after logon — FAIL

**Not in any of the four assigned files.** `CTraderFixSession` does not mention the flag. `LiveCopyPage` only displays `status?.realCopyArmed`. `RiskEngine` takes `RealExecutionEnabled` as an **input** and does not write runtime state. The empty branch at L90–93 is a comment, not a force-false:

```90:93:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.RealExecutionEnabled == false && request.Action != CopyIntentAction.CloseExposure)
        {
            // Shadow path still evaluates risk but never allows FIX send.
        }
```

Later `AllowFixSend` is:

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

If the inbound flag is true and the other three hold, the engine **approves a send**. That is the opposite of “forced false.”

Logon hosted service **reads** the bit after `TryLogonAsync` and logs it. It does **not** assign `false`:

```60:70:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.Quote.LoggedOn = quote.LoggedOn;
        _runtime.Quote.Status = quote.Status;
        _runtime.Quote.LastError = quote.LastError;
        _runtime.Quote.UpdatedAt = DateTimeOffset.UtcNow;
        _runtime.Trade.LoggedOn = trade.LoggedOn;
        _runtime.Trade.Status = trade.Status;
        _runtime.Trade.LastError = trade.LastError;
        _runtime.Trade.UpdatedAt = DateTimeOffset.UtcNow;
        _log.LogInformation(
            "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
            quote.LoggedOn, trade.LoggedOn, _runtime.RealCopyEnabled, account);
```

The **only** product assignment of `RealCopyEnabled =` (grep `src/` + `apps/`) is DI construction:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

API boot loads `D:\Prop\.env` via `EnvFile.FindAndLoad()` (`apps/api/Program.cs` L10; hard path in `EnvFile.cs` L14). Quoted **boolean only** from `.env` L73: `REAL_COPY_EXECUTION_ENABLED=true`. No other `.env` keys quoted.

Therefore: if the API process is up with that file, `RealCopyEnabled` is **true** before logon and **still true** after logon. There is no post-logon writer that pins it false.

`CTraderFixOptions.RealCopyExecutionEnabled` defaults `false` and is a **different** object. Fix-worker reads `CTrader:RealCopyExecutionEnabled` (default false) — not the API `LiveRuntimeStatus` bit. That default does not prove claim 3.

Live GET of `/api/health` → `realCopyEnabled` was **not obtained** (SSRF). File proof already FAILs the “forced false after logon” wording. A live `false` would still only be “currently false,” not “forced after logon.”

---

## 4. sending now cannot be the profit path — PASS (dest-profit meaning)

Proven combination from assigned + hop files. **Not** proven as “cannot send.”

Assigned session: one-shot `35=A`, dispose, no order. Assigned page: GET-only (`useCopyStatus` / `useCopyIntents` → `/api/copy/status`, `/api/copy/intents`). No POST, no button, no FIX client. Assigned engine: persist path in hop **overwrites** `AllowFixSend = false` regardless of engine output:

```317:324:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                var rec = new RiskDecisionRecord
                {
                    Id = Guid.NewGuid(),
                    CopyIntentId = intent.Id,
                    Outcome = decision.Outcome,
                    ApprovedQuantity = decision.ApprovedQuantity,
                    Reason = decision.Reason,
                    AllowFixSend = false,
                    DecidedAt = now
                };
```

Hop also sets `Reconciled = VenueReconciled` and `VenueReconciled = const false` (L20, L304), so even with `RealCopyEnabled=true` the engine `allowSend` is false. Live branch additionally requires `TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled` (L330) — `LIVE` is unreachable from `FromBaseline` + `CanPromoteToLive=false`.

Dashboard dest PnL is a constructor literal `0` (third numeric after `shadowPnl`):

```33:52:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        return new OverviewDto(
            accounts,
            brokers,
            xauTraders,
            three,
            scores.Count(s => s.CurrentState == TraderState.WATCH),
            scores.Count(s => s.CurrentState == TraderState.SHADOW),
            scores.Count(s => s.CurrentState == TraderState.LIVE_CANDIDATE),
            scores.Count(s => s.CurrentState == TraderState.LIVE),
            scores.Count(s => s.CurrentState == TraderState.RISK_BLOCKED),
            shadowPnl,
            0,
            0,
            0,
            ...
            _runtime.RealCopyEnabled);
```

`OverviewDto.DestinationRealPnl` is that `0` (`DashboardModels.cs` L16). No file this slot computes dest realized from fills.

**Why this is not “cannot send”:** `NewOrderSingleImplemented => DemoDest` (L50). `ExecuteDemoCopyAsync` does **not** read `RealCopyEnabled`. On demo host/sender and account ≠ `1369850` it can emit `35=D` (≤ `MaxAutoLots=0.05`, 5/tick). `LiveCopyPage.tsx` L57 states that in the empty-table copy. File ledger `D:\Prop\data\demo_copy_ledger.json` already records dest pos `237339770` / px `4390.2` for source `305750`/`21250421` (0.01, still open). That is a dest **fill**, not dest **profit**, and not live `1369850`.

So: sending now is not a measured destination-profit path. Sending now **can** still be a demo-capital path.

---

## 5. SHADOW on demo is not destination profit — PASS

Assigned scorer: `SHADOW` is a **source** label from completed reconstructed XAU (`ComputeFeatures` sums `t.NetRealizedPnl`, quality/risk blend, then `FromBaseline` → `SHADOW` when `quality >= 70 && risk < 40` and n≥3). No dest account, no fill, no cash.

Assigned page: `SHADOW traders` is `status?.shadowTraders` (count of `TraderState.SHADOW` rows). `Shadow fills` is `ShadowOrders` count. Neither is dest realized.

Policy requires demo/contest groups for eligibility (`CopyGroupFilter.IsDemoOrContest`; reject reason `NOT_DEMO_OR_CONTEST_GROUP`). That means a SHADOW name that can be admitted is a **challenge/demo source**, not a live Starwave book. Source challenge PnL is not Pepperstone dest PnL.

Paper shadow mark is `ShadowCopyEngine.SimulateEntry` into `ShadowOrders`. Overview `ShadowPnl` is `Sum(SourceVsShadowSlippage)` — a modeled slip number, not venue cash. Dest DTO remains literal `0` (claim 4).

No live GET of `/api/traders?state=SHADOW` this slot, so **this slot does not re-prove** “70 SHADOW / +$78,276 / 100% demo.” Those sibling pins are **unverified here**. The type-level claim (SHADOW-on-demo-source ≠ dest profit) is proven from the files above.

---

## Assigned-file notes (what they are not)

| File | Lines read | Cannot do |
|---|---|---|
| `CTraderFixSession.cs` | 135/135 | place an order; keep a socket; set `RealCopyEnabled` |
| `BaselineScorer.cs` | 212/212 | send FIX; promote to `LIVE`; book dest cash |
| `RiskEngine.cs` | 190/190 | send; persist; force the runtime flag false |
| `LiveCopyPage.tsx` | 70/70 | POST; send `35=D`; compute dest PnL |

`RiskEngine` comment “never allows FIX send” is **not** implemented as an early return. Tests (`RiskEngineTests` L21–26) only cover `RealExecutionEnabled=false`.

---

## Live GET

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | **SSRF blocked** — process `realCopyEnabled` / FIX logon **unproven this slot** |
| `http://127.0.0.1:5000/api/copy/status` | **SSRF blocked** — `realCopyArmed` / `shadowTraders` / `liveSends` **unproven this slot** |

File evidence is sufficient to FAIL claim 3 without the GET.

---

## Risk to capital

| Surface | This-slot proof |
|---|---|
| Live Pepperstone `1369850` | **NONE** — `CTraderFixCopyOpen` L37–42 refuse; session file cannot `35=D` |
| Demo dest (lab `.env` account id is the demo path; not printed beyond already-public refuse of `1369850`) | **NOT `SAFE_BY_ABSENCE`** if hosted tick is running — `ExecuteDemoCopyAsync` can `Build("D")` without `RealCopyEnabled` |
| This process / this slot | **0 sends** (read-only verify) |
| Source SHADOW / paper `ShadowOrders` | **Not dest cash** |

Do not treat an armed `REAL_COPY_EXECUTION_ENABLED=true` as a live ticket. Do not treat a demo dest fill as destination profit. Do not copy-all the catalog as an edge — that claim was **not** remeasured this slot.

---

## Secrets

No passwords, no tag 554 values, no SenderCompID/account dumps beyond the public refuse of live `1369850`. `.env` quoted L73 boolean only.
