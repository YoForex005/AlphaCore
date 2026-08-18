# P500_VERIFY_40 — Adversarial four-file confirm (slot 40)

| Field | Value |
|---|---|
| Slot | **40** |
| Date | 2026-08-18 |
| Agent | P500_VERIFY_40 (adversarial; independent HEAD re-read; sibling `P500_*` numbers are **not** evidence) |
| Assigned | Read `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`. Confirm: (1) no `35=D` builder; (2) `CanPromoteToLive` is false; (3) `RealCopyEnabled` forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. **FAIL any claim that cannot be proved from a file or live GET.** Never print secrets. |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **None.** No password / proxy / FIX password values quoted. Only the already-on-disk boolean key `REAL_COPY_EXECUTION_ENABLED=true` is named. Public dest ids `5328266` / `1369850` appear in product source and are repeated only as identifiers. |
| Live GET this slot | **Attempted and blocked.** `web_fetch` to `http://127.0.0.1:5000/api/health`, `/api/settings`, `/api/copy/status`: SSRF blocked (loopback). `open_page` to `/api/health`: retrieve error. **No live JSON.** File proof only. |
| Live `35=D` sent this slot | **No.** |
| `REAL_COPY` flipped this slot | **No.** |
| Method | Full `read_file` of the four assigned files (135 / 212 / 189 / 70 lines). Then the logon / DI / copy hop those files actually call (`CTraderFixLogonHostedService`, `DependencyInjection`, `CopyTradingService`, `CopyTradingHostedService`, `CTraderFixCopyOpen`, `ShadowCopyEngine`, `EfDashboardQueries`, `DealIngestionService`, `EnvFile`, `Program.cs`). Targeted `grep` for `35=D` / `Build("D")` / `CanPromoteToLive` / `RealCopyEnabled` / `AllowFixSend` / `DestinationRealPnl`. `.env` inspected **for that flag key only**. |
| Honesty rule | A comment is not a pin. A dashboard label is not dest cash. `GetStatusAsync.VenueReconciled: DemoDest` is not the const used on the send branch. Sibling swarm PnL / census integers are not evidence. |

---

## 0. Verdict (binding)

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** (scoped to assigned `CTraderFixSession.cs`) | File **135/135**. Only outbound MsgType is `(35, "A")` at L96. This file: `35=D` / `(35, "D")` / `Build("D")` / `NewOrderSingle` = **0**. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(TraderState current) => false` at `BaselineScorer.cs` L211. Argument unused. `FromBaseline` max state is `SHADOW`. Unit test asserts SHADOW ≠ LIVE. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | **Disproved**, not merely unproved. Logon host **reads** the flag and **never assigns** `false`. Sole product `RealCopyEnabled =` write is DI bind of env. `.env` L73 is `true`. |
| 4 | sending now cannot be the profit path | **PASS_NOT_BOOKED_DEST_PROFIT** | Assigned session cannot send. Persist `AllowFixSend=false`. `CanPromoteToLive` hard-false. `DestinationRealPnl` constructor literal `0`. Residual: sibling `CTraderFixCopyOpen.Build("D")` **is** a demo dest sender on the 20s tick — dest **activity**, not a booked dest-profit constructor. |
| 5 | SHADOW on demo is not destination profit | **PASS** | `SHADOW` is a **source-trader state**. Dashboard `ShadowPnl` is `Sum(SourceVsShadowSlippage)`. `DestinationRealPnl` is constructor `0`. `LiveCopyPage` shows SHADOW **counts**, no dest cash. `ShadowCopyEngine.SimulateEntry` is paper, not a venue fill. |

**Slot verdict: `FAIL`.**

Claim 3 is **disproved** from files. Four of five assigned claims hold (claim 1 scoped to the assigned session file). One FAIL is enough. Unscoped “product `35=D=0`” is **false** on HEAD and is recorded as residual, not used to invent a sixth claim.

**Risk to capital:** **NONE on live `1369850`** (`SAFE_BY_ABSENCE` for `CTraderFixSession`; `CTraderFixCopyOpen` refuses live host/sender/account). **Not absent on demo dest** if `DemoDest` is true: hosted `ExecuteDemoCopyAsync` can emit `35=D` **without** `RiskEngine.Evaluate` and **without** reading `RealCopyEnabled`. This slot sent **0**.

---

## 1. Claim 1 — no `35=D` builder — **PASS** (`CTraderFixSession.cs` only)

Assigned file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135** lines, full read).

The only outbound builder is `BuildLogon`:

```89:109:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
    private static string BuildLogon(
        string sender, string target, string senderSub, string targetSub,
        string username, string password, int seq)
    {
        var sendingTime = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
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
    }
```

| Check | Measured |
|---|---|
| Outbound MsgType in this file | **only** `(35, "A")` L96 |
| `WriteAsync` count | **1** (L49, the logon bytes) |
| `35=D` / `(35, "D")` / `Build("D")` / `NewOrderSingle` in this file | **0** |
| Socket lifetime | `using TcpClient` + `await using SslStream`; disposed after one read |
| Inbound `35=A` treated as | `LoggedOn = true` (L56–64) — **logon ack, not a fill** |
| Inbound other `35` | `LoggedOn = false`, status Error (L67–75) |

`TryLogonAsync` is called twice from `CTraderFixLogonHostedService` (QUOTE 5211, TRADE 5212). Both calls are logon-only. The session class has no keep-alive, no NewOrderSingle, no ExecutionReport sender.

Adversarial residual (**does not fail the assigned-file claim**): sibling product files **do** have a `35=D` builder.

| File | What this slot measured |
|---|---|
| `src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra)` after TRADE logon + SecurityList. **Refuses** non-`demo-` host, non-`demo.` sender, or account `1369850` (L37–42). |
| `src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` L93 / L105 | `Build("D", …)` / `SendD` helper (demo matrix). |
| `src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | `Build("D", …)` ×3 (demo test helper). |

Claim 1 as written against **`CTraderFixSession.cs`** is true. Tree-wide “product `35=D=0`” is **false** on HEAD. This slot does **not** treat that residual as a live `1369850` sender.

---

## 2. Claim 2 — `CanPromoteToLive` is false — **PASS**

Assigned file: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (**212** lines, full read).

```188:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
public static class TraderStateMachine
{
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
}
```

| Check | Measured |
|---|---|
| Highest state `FromBaseline` can emit | `SHADOW` (L200–201) |
| `LIVE` / `LIVE_CANDIDATE` in this file | **0** |
| `CanPromoteToLive` body | literal `false`; `current` unused |
| Product callers of `CanPromoteToLive` | **0** under `src\` / `apps\` (only unit test + report `_tmp_*`) |
| Score persist path | `DealIngestionService` L140 `CurrentState = score.SuggestedState` |
| Unit test | `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` asserts `SuggestedState == SHADOW` and `CanPromoteToLive(...) == false` |

Enum still **has** `LIVE_CANDIDATE` / `LIVE` (`TraderState.cs` L9–10). `CopyTradingService` counts `LIVE` and branches on it (L58, L330). That is a **capability hole**, not a scorer promotion: nothing in the assigned scorer (or ingest persist) writes those states. Claim 2 as written is true.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL**

None of the four assigned files force `RealCopyEnabled = false` after logon.

| Assigned file | `RealCopyEnabled` hits |
|---|---|
| `CTraderFixSession.cs` | **0** — logon only; no runtime flag |
| `BaselineScorer.cs` | **0** |
| `RiskEngine.cs` | reads `request.RealExecutionEnabled` (input); does not own the process flag |
| `LiveCopyPage.tsx` | displays `status?.realCopyArmed` (L13); does not write it |

Logon host (`D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`, full read):

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

After logon the host **reads** `_runtime.RealCopyEnabled` for a log line. There is **no** `_runtime.RealCopyEnabled = false`. Persist path writes `FixSessionState` rows only.

Sole product assignment of the process flag:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

API boot (`apps\api\Program.cs` L10) calls `EnvFile.FindAndLoad()`, which loads `D:\Prop\.env` (hardcoded candidate at `EnvFile.cs` L14) into the process environment before `AddTraderIntelligence`.

`.env` L73 (flag key only, value not a secret): `REAL_COPY_EXECUTION_ENABLED=true`.

Therefore after a successful logon the process flag is **whatever DI bound at startup**, currently **true**, not forced false.

`CTraderFixOptions.RealCopyExecutionEnabled` defaults to `false` (`CTraderFixOptions.cs` L35). That POCO is **not** what `LiveRuntimeStatus.RealCopyEnabled` is bound from. A default on an unused options class is **not** a post-logon pin.

`apps\fix-worker\Worker.cs` reads `CTrader:RealCopyExecutionEnabled` (default false) and still does not send; it also does not write `LiveRuntimeStatus.RealCopyEnabled`.

**Claim 3 is false on HEAD.**

---

## 4. Claim 4 — sending now cannot be the profit path — **PASS_NOT_BOOKED_DEST_PROFIT**

### 4.1 Assigned files

**`CTraderFixSession.cs`:** one `35=A` write. Cannot be a dest-profit sender.

**`RiskEngine.cs` (189/189):**

```90:93:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.RealExecutionEnabled == false && request.Action != CopyIntentAction.CloseExposure)
        {
            // Shadow path still evaluates risk but never allows FIX send.
        }
```

That `if` is a **comment**. It does not `return`. `AllowFixSend` is computed later:

```147:171:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;

        if (IsReducing(request.Action))
        {
            return new RiskDecision
            {
                CopyIntentId = request.CopyIntentId,
                Outcome = RiskDecisionOutcome.Approve,
                ApprovedQuantity = request.RequestedQuantity,
                Reason = "RISK_REDUCTION",
                AllowFixSend = allowSend
            };
        }

        return new RiskDecision
        {
            CopyIntentId = request.CopyIntentId,
            Outcome = RiskDecisionOutcome.Approve,
            ApprovedQuantity = request.RequestedQuantity,
            Reason = "APPROVED",
            AllowFixSend = allowSend
        };
```

So `RiskEngine` **can** set `AllowFixSend=true` if the caller lies `RealExecutionEnabled=true` **and** `Reconciled=true` **and** venue healthy **and** kill-switch none. Unit test `Real_flag_false_never_allows_fix_send` only covers the `false` input (`RiskEngineTests.cs` L21–26). A comment is not a gate.

**`LiveCopyPage.tsx`:** title “Live copy portfolio”; shows `realCopyArmed`, `liveSends`, `shadowFills`; blockers header “Live send blockers (Pepperstone cannot be filled)”. Empty-state copy L57: “Demo dest auto-sends after a trader is ADMITTED…”. The page **advertises** a demo dest send. It does **not** book dest cash.

### 4.2 Copy hop (required to interpret “sending now”)

`CopyTradingService.GenerateShadowIntentsAsync`:

- `VenueReconciled` **const false** (L20) is passed into `RiskEngine` (L304) → increasing opens reject `VENUE_NOT_RECONCILED` unless they are not increasing.
- Persist always writes `AllowFixSend = false` (L324), **ignoring** `decision.AllowFixSend`.
- Live-send branch (L330) requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled`. The const is false, so that branch is dead. Else status is `SHADOW_ONLY` + paper `SimulateEntry`.

`GetStatusAsync` **lies about recon**: `VenueReconciled: DemoDest` (L67), not the const. That is a status DTO, not a send gate.

`CopyTradingHostedService` (20s tick) calls `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**.

`ExecuteDemoCopyAsync` (L483–605):

- Returns 0 if `!DemoDest` (demo- host + demo. sender + account ≠ `1369850`).
- Does **not** read `RealCopyEnabled`.
- Does **not** call `RiskEngine.Evaluate`.
- Calls `CTraderFixCopyOpen.SendAsync`, which **builds `35=D`** on demo dest.
- Marks intent `DEMO_SENT` on fill.

That is dest **activity**. It is **not** a booked destination-profit constructor.

Dashboard dest cash:

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

`OverviewDto` position 11 is `DestinationRealPnl` (`DashboardModels.cs` L16) — literal `0`. `ShadowPnl` (L29) is `Sum(SourceVsShadowSlippage)`, not dest realized.

Claim 4 as “the current send path is not how dest profit is booked” is **proved**. Claim 4 as “nothing can send” is **false** (demo dest hop). This slot uses the booked-profit reading and records the residual.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **PASS**

| Object | What it is | Dest profit? |
|---|---|---|
| `TraderState.SHADOW` | Source-trader state from `FromBaseline` (quality≥70, risk<40, ≥3 XAU) | No |
| `AfterHighEarlyScore()` | Always `SHADOW` | No |
| `LiveCopyPage` “SHADOW traders” | `status?.shadowTraders` count | No cash |
| `OverviewDto.ShadowPnl` | `Sum(ShadowOrders.SourceVsShadowSlippage)` | Slippage vs source, not dest realized |
| `OverviewDto.DestinationRealPnl` | constructor `0` | Explicitly not booked |
| `ShadowCopyEngine.SimulateEntry` | paper fill at quote bid/ask ± 0.05 modeled slip | No venue ticket |
| `PersistDemoShadowAsync` | only if `state == SHADOW`; writes `SHADOW_ONLY` + simulated fill | Paper |
| `GenerateShadowIntentsAsync` non-LIVE path | `Status = "SHADOW_ONLY"` | Paper |
| Policy `IsTraderEligible` | rejects WATCH/EARLY as `TRADER_NOT_SHADOW_YET`; requires **20** completed XAU + source book PnL > 0 + demo/contest group | Eligibility on **source** book, not dest cash |

`CopyRosterEngine` admits SHADOW (and any non-blocked state that passes policy) onto a dest roster. That admit can later fire `ExecuteDemoCopyAsync` dest `35=D` (claim 4 residual). Admit is **not** dest profit. Source `XauNetPnl > 0` is **source** edge, explicitly “no lookahead” (`XauUsdOneToOneCopyPolicy` class comment). Dest cash remains constructor `0`.

---

## 6. Live GET

Attempted this slot:

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | `web_fetch` SSRF blocked; `open_page` retrieve error |
| `http://127.0.0.1:5000/api/settings` | `web_fetch` SSRF blocked |
| `http://127.0.0.1:5000/api/copy/status` | `web_fetch` SSRF blocked |

`launchSettings.json` advertises `http://localhost:5000`. Web client default is that same origin (`apps\web\src\api\client.ts` L4). **No live JSON** to confirm `realCopyEnabled` at runtime. File proof of the bind (`.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41) is sufficient to **fail claim 3**. Runtime value is **not** independently observed by this slot.

---

## 7. What this slot will not claim

- Will not claim product `35=D=0`. Sibling builders exist.
- Will not claim `RealCopyEnabled` is false in a running API. Live GET blocked; file bind is `true`.
- Will not claim demo dest cannot receive a ticket. `ExecuteDemoCopyAsync` is wired.
- Will not claim `RiskEngine` hard-blocks send. `AllowFixSend` follows caller flags.
- Will not print secrets.
- Will not treat sibling `P500_VERIFY_*` verdicts as proof.

---

## 8. Slot result

| Item | Value |
|---|---|
| Slot | 40 |
| Verdict | **FAIL** |
| Evidence | Claim 3 disproved: logon host never assigns `RealCopyEnabled=false`; DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`. Claims 1 (session-scoped), 2, 4 (not booked dest profit), 5 hold from files. Live GET blocked. |
| Risk to capital | **NONE** on live `1369850`. Demo dest send **wired** if `DemoDest`. This slot sent 0. |

**End P500_VERIFY_40.** Slot **40**. Verdict **FAIL** (claim 3 disproved). Risk to live capital **NONE**; demo dest send **wired**. Product source was not modified. No secrets printed.
