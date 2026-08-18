# P500_VERIFY_37 — Adversarial four-file verify (slot 37)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_37.md` |
| Agent | P500_VERIFY_37 (adversarial verifier, slot 37) |
| Slot | **37** |
| Date | 2026-08-18 |
| Assigned | Read `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` yourself. Confirm: (1) no `35=D` builder, (2) `CanPromoteToLive` is false, (3) `RealCopyEnabled` forced false after logon, (4) sending now cannot be the profit path, (5) SHADOW on demo is not destination profit. FAIL any claim not proven from a file or live GET. Never print secrets. |
| Product source modified | **No.** This report (plus index/log pointers) is the only write. |
| Test source modified | **No.** |
| Secrets printed | **No.** Key names, already-public dest ids (`5328266` / `1369850`), and boolean `REAL_COPY_EXECUTION_ENABLED=true` only. Password / tag 554 never quoted. |
| Live GET this pass | `GET http://localhost:5000/api/health` and `GET http://127.0.0.1:5000/api/copy/status` **blocked** (tool SSRF on loopback). Runtime JSON is **not** process-proven this slot. |

**Honesty rule:** a comment that says “NewOrderSingle still unimplemented” is not a missing builder. `CanPromoteToLive => false` does not stop `ExecuteDemoCopyAsync`. `VenueReconciled = false` on the *risk hop* is not the absence of a *demo dest* hop. `DestinationRealPnl` constructor `0` is a dashboard lie if dest already filled. FAIL the bundle if any of the five claims is false or only true after a silent scope change. This slot re-read HEAD files; it does not inherit prior P500 verdicts.

---

## 0. Verdict (binding)

**FAIL.** The five-claim bundle is not proven as written.

| # | Claim | Result | Class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS only if scoped to `CTraderFixSession.cs`.** **FAIL** as a process/product claim. | assigned session is `35=A` only; siblings **have** `Build("D")` and one is hosted |
| 2 | `CanPromoteToLive` is false | **PASS** | hardcoded `=> false`; `FromBaseline` ceiling is `SHADOW` |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | no post-logon write; DI binds `.env` `true` |
| 4 | sending now cannot be the profit path | **FAIL** as written | hosted demo dest hop **is** dest P&L |
| 5 | SHADOW on demo is not destination profit | **FAIL** as written | `TraderState.SHADOW` is the AUTO_ADMIT dest gate |

One-line:

```text
CTraderFixSession 135/135 is 35=A only. CanPromoteToLive=>false.
RealCopyEnabled is NOT forced false (DI binds .env true; logon host logs the flag).
Hosted ExecuteDemoCopyAsync -> CTraderFixCopyOpen.Build("D") on demo dest.
SHADOW is dest-eligible. Live 1369850 refused. Live GET blocked.
```

Operating mode (honest):

```text
ALLOW:  treat CTraderFixSession as logon-proof only
        treat CanPromoteToLive as a hard no-LIVE from scoring
        treat ShadowOrder / dashboard ShadowPnl as simulated, not dest $
FORBID: claim the process has no 35=D builder
        claim RealCopyEnabled is forced false after logon
        claim "sending now cannot be the profit path"
        treat SHADOW/demo as non-dest (roster + ExecuteDemoCopyAsync send dest)
        treat live GET as measured this slot
```

---

## 1. Files read (this slot; not memory)

Assigned four, plus the hops required to try to **disprove** the claims.

| Path | Role |
|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | assigned; 135/135 logon-only |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | assigned; `TraderStateMachine` |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | assigned; `AllowFixSend` |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | assigned; UI honesty |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | dest `35=D` builder (hosted) |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | off-hop `35=D` ×3 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | off-hop `35=D` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | post-logon flag? |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | flag storage |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | **only** `RealCopyEnabled =` write |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | risk hop + demo dest hop |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 20s tick |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | SHADOW eligibility |
| `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs` | AUTO_ADMIT |
| `D:\Prop\src\Domain\Copy\CopyLifecycle.cs` | dest open/close predicates |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | simulate-only |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | dest $ constructor 0 |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `PersistDemoShadowAsync` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `CurrentState = SuggestedState` |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | `DestinationRealPnl` field |
| `D:\Prop\src\Domain\Enums\TraderState.cs` | SHADOW=3, LIVE=5 exist |
| `D:\Prop\apps\api\Program.cs` | `/api/health` `/api/copy/*` `/api/settings` |
| `D:\Prop\apps\api\Properties\launchSettings.json` | `:5000` |
| `D:\Prop\apps\web\src\api\hooks.ts` + `client.ts` | live GET targets |
| `D:\Prop\apps\fix-worker\Worker.cs` | separate `CTrader:RealCopyExecutionEnabled` default false |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | 3 winners → SHADOW, not LIVE |
| `D:\Prop\tests\Unit\XauUsdOneToOneCopyPolicyTests.cs` | GoodTrader = SHADOW |
| `D:\Prop\tests\Unit\CopyRosterEngineTests.cs` | dest-admit fixture = SHADOW |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | real-flag false never allows FIX on **that request** |
| `D:\Prop\src\Infrastructure\Copy\DemoCopyLedger.cs` + `D:\Prop\data\demo_copy_ledger.json` | dest fill residual |
| `D:\Prop\reports\swarm\20260818\DEMO_COPY_OPEN.json` | prior dest fill ER |
| `D:\Prop\.env` | `REAL_COPY_EXECUTION_ENABLED` boolean + public host/sender prefixes (password not quoted) |

---

## 2. Claim 1 — “no 35=D builder”

### 2.1 Scoped to assigned `CTraderFixSession.cs` — PASS

The file is **135** lines. The only outbound builder is `BuildLogon`. The only outbound `35` literal is Logon `A`.

```89:110:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
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

Grep of this file for `(35,` → **one** hit, line 96, `"A"`. There is **one** `WriteAsync` (L49), then `using TcpClient` / `await using SslStream` dispose. No keep-alive, no `35=D`, no `35=F`, no `35=G`. Inbound `Extract(reply, "35")` accepts `A` as logon-ok and otherwise reports `Logon rejected 35={msgType}` — that is a **reader**, not a builder.

`CTraderFixLogonHostedService` is the only product caller of `CTraderFixSession.TryLogonAsync` (QUOTE port 5211, then TRADE port 5212). After return, the socket is gone. Log line L69 says `NewOrderSingle still unimplemented` — that is a **comment on this session object**, not a census of the assembly.

### 2.2 Process / product — FAIL

Same folder, same assembly, three other builders emit MsgType `D`:

| File | Evidence |
|---|---|
| `CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra.ToArray())` |
| `CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | `Build("D", …)` flatten / open / close |
| `CTraderFixDemoMatrix.cs` L93 | `Build("D", sender, target, seq++, extra)` |

`CTraderFixCopyOpen.Build` is generic (`Build(string type, …)` then `(35, type)`). `SendAsync` is **hosted**, not CLI-only:

```28:30:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

`CopyTradingService.ExecuteDemoCopyAsync` L528 and L566 call `CTraderFixCopyOpen.SendAsync`. `NewOrderSingleImplemented => DemoDest` (L50), **not** a const false. Lab `.env` is DemoDest: `CTRADER_FIX_HOST` starts `demo-`, `CTRADER_FIX_TRADE_SENDER_COMP_ID` starts `demo.`, `CTRADER_FIX_ACCOUNT_ID=5328266` (not `1369850`).

**Claim 1 as written (“no 35=D builder”) is therefore false.** Scoped salvage that is true: *`CTraderFixSession` has no `35=D` builder.* That is not the claim.

---

## 3. Claim 2 — `CanPromoteToLive` is false — PASS

```187:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
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

`FromBaseline` never returns `LIVE` or `LIVE_CANDIDATE`. Ceiling is `SHADOW` (or `RISK_BLOCKED` / `INSUFFICIENT_DATA` / `WATCH` / `EARLY_SCORE`). `CanPromoteToLive` ignores `current` and is unconditionally `false`.

`DealIngestionService` writes `CurrentState = score.SuggestedState` (L140). Unit test `Three_disciplined_winners_go_to_shadow_not_live` asserts `SuggestedState == SHADOW` and `CanPromoteToLive(...).Should().BeFalse()`.

Grep of product `CurrentState =` → only `DealIngestionService` (SuggestedState) and `EfTradingStore.UpsertScoreAsync` (copies that score). No other writer promotes to LIVE.

**Proven.** Scoring cannot auto-LIVE. This does **not** prove dest cannot send (claims 4/5). `TraderState.LIVE` still exists as an enum value; `CopyTradingService` still lists it as copyable. Dead branch, not dest-off.

---

## 4. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL

Grep of `RealCopyEnabled` across product `*.cs` → **one** assignment:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret). `apps/api/Program.cs` L10 loads that file via `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()`.

`CTraderFixLogonHostedService.ExecuteAsync` after both logons:

- writes `Quote.LoggedOn` / `Trade.LoggedOn` / timestamps / errors
- **logs** `_runtime.RealCopyEnabled`
- does **not** assign `_runtime.RealCopyEnabled = false` (or true)

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

`RiskEngine` does not mutate the runtime flag. The `if (request.RealExecutionEnabled == false)` block (L90–93) is an empty comment. `AllowFixSend` is computed from the **request** and never writes `LiveRuntimeStatus`.

`CopyTradingService.BuildBlockers` L621 adds the string `"REAL_COPY_EXECUTION_ENABLED is false"` **when the flag is already false**. That is a UI list item, not a force-false.

`apps/fix-worker/Worker.cs` reads a **different** key (`CTrader:RealCopyExecutionEnabled`, default false) and only logs. It does not write `LiveRuntimeStatus`.

`LiveCopyPage` displays `status?.realCopyArmed` from `/api/copy/status` (`RealCopyArmed: _runtime.RealCopyEnabled`). If the API is up with this `.env`, the page will show **YES**, not a forced NO. That runtime paint is **unverified this slot** (live GET blocked). The **code path** that would force false after logon **does not exist**.

**Claim 3 is disproved from files.** Do not launder “session disposed after 35=A” into “flag forced false.” Those are different facts. I do **not** claim the running process’s current JSON (GET blocked).

---

## 5. Claim 4 — “sending now cannot be the profit path” — FAIL

Split the hops. Do not mix them.

### 5.1 `CTraderFixSession` hop — not a profit path (true, insufficient)

One-shot `35=A`, socket disposed. Cannot fill. Not dest P&L.

### 5.2 Risk / shadow hop — cannot send FIX (true, insufficient)

`CopyTradingService.VenueReconciled` is `const false` (L20). Hopper Evaluate passes `Reconciled = VenueReconciled` (L304). `RiskEngine` rejects increasing actions with `VENUE_NOT_RECONCILED` when `!Reconciled` (L84–85). Persist then **overwrites**:

```317:337:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
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
                // ...
                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
                else
                {
                    intent.Status = "SHADOW_ONLY";
```

Even the “live send” branch does **not** call a builder; it stamps a blocked status. `CanPromoteToLive => false` plus `VenueReconciled == false` makes this branch dead. This hop is **not** dest profit.

`ShadowCopyEngine.SimulateEntry` / `SimulateExit` write `ShadowOrder` rows. `EfDashboardQueries.GetOverviewAsync` sets `DestinationRealPnl` / `XauGross` / `XauNet` to **literal 0** (L44–46) and `ShadowPnl = Sum(SourceVsShadowSlippage)` (simulated slippage, not dest mark).

`RiskEngine` approve path:

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

Rejects always set `AllowFixSend = false`. Test `Real_flag_false_never_allows_fix_send` uses a fixture with `RealExecutionEnabled=false` and `Reconciled=true`. That proves the **engine**, not the hosted dest hop.

### 5.3 Hosted demo dest hop — **is** a dest P&L path

`ExecuteDemoCopyAsync` (L483–605):

- returns 0 only if `!DemoDest` or password empty
- **does not** read `RealCopyEnabled`
- **does not** call `RiskEngine.Evaluate`
- **does not** require `TraderState.LIVE`
- **does not** require `AllowFixSend`
- sends `CTraderFixCopyOpen.SendAsync` (which `Build("D")`) for:
  - dest close when source reconstructed trade is completed (`CopyLifecycle.ShouldCloseDest`)
  - dest open when roster row is `ADMITTED` and source XAU is still open and `MaxVolumeLots <= MaxAutoLots` (0.05)

`CTraderFixCopyOpen` refuses live identity (`host` must start `demo-`, `sender` must start `demo.`, `account != "1369850"`). That is a **live-1369850** refuse, not “no send.”

`LiveCopyPage.tsx` L57 states this in the empty-state copy:

> Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.

`GetStatusAsync` summary when `DemoDest` (L76–77):

> Demo dest auto-copy ON. Eligible demo/contest opens send on the 20s tick; dest closes when the MT5 source closes. Live 1369850 is never used.

On-disk dest residual (no secrets):

| Artifact | Fact |
|---|---|
| `D:\Prop\data\demo_copy_ledger.json` | login `305750` / pos `21250421` / **0.01** lot / dest `237339770` / clord `C20260818093047317` / px `4390.2` / `DestClosed: false` |
| `DEMO_COPY_OPEN.json` | `OrderSent: true`, `Filled: true`, host `demo-us-eqx-01.p.c-trader.com`, account `5328266`, ER `35=8` `150=F` `39=2` |

`ExecuteDemoCopyAsync` L500–512 **re-seeds** that same 305750/21250421 row if missing. The 20s tick will try to close dest when the source reconstructed trade completes.

That is a **destination fill** on demo Pepperstone. Open dest inventory can win or lose. That **is** a profit/loss path. It is **not** live account `1369850`.

`LiveCopyPage` still lists “Live send blockers (Pepperstone cannot be filled)” from `BuildBlockers`. When `DemoDest` is true, the `SAFE_BY_ABSENCE` / “0 LIVE traders” blockers are **skipped** (only added inside `if (!DemoDest)`). Remaining blockers are quote/trade logged-on and `REAL_COPY_EXECUTION_ENABLED is false`. Those blockers **do not gate** `ExecuteDemoCopyAsync`.

**Claim 4 as written is false.** Narrow salvage that is true: *live 1369850 sending is not the profit path; CTraderFixSession sending is not the profit path; the risk-hop cannot send.* Those are not the claim.

---

## 6. Claim 5 — “SHADOW on demo is not destination profit” — FAIL

Two different objects share the word SHADOW.

### 6.1 `ShadowOrder` / dashboard `ShadowPnl` — not dest $ (true, insufficient)

`EfTradingStore.PersistDemoShadowAsync` only simulates when `state == SHADOW`; writes `Status = "SHADOW_ONLY"`; never calls FIX. `ShadowCopyEngine` has no socket. Overview dest constructor is 0. **That metric is not dest profit.** Proven.

### 6.2 `TraderState.SHADOW` on a demo/contest source — dest-eligible (disproves the claim as written)

`XauUsdOneToOneCopyPolicy.IsTraderEligible`:

- rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET`
- does **not** reject `SHADOW` / `LIVE_CANDIDATE` / `LIVE`
- requires ≥20 completed XAU (`MinCompletedXauTrades = 20`), XAU net > 0, no size-pattern, **demo/contest group**

`CopyRosterEngine.Decide` AUTO_ADMITs when `IsTraderEligible` is true. Tests use `State = TraderState.SHADOW` as the **good** dest-copy trader (`XauUsdOneToOneCopyPolicyTests.GoodTrader`, `CopyRosterEngineTests.Shadow`).

`ExecuteDemoCopyAsync` iterates `Status == "ADMITTED"` roster seats and sends dest `35=D`. It never re-checks `CurrentState`. Because scoring cannot promote to LIVE (claim 2 PASS), **SHADOW is the production dest-admission state.**

Nuance that does **not** save the claim: 3-trade SHADOW (scoring ceiling after `EarlyScoreTradeCount=3`) is **not** dest-admitted (`NEED_MORE_XAU_HISTORY` until 20). Dest send is SHADOW **and** 20+ profitable demo/contest XAU. The claim said “SHADOW on demo is not destination profit,” not “3-trade SHADOW is not dest.” Once the book qualifies, SHADOW is how dest demo fills happen.

Ledger fill `305750` is that class (dest 0.01, still open). Treating “SHADOW on demo” as non-dest is how an operator wires dest risk while believing they are only shadowing.

**Claim 5 as written is false.** Narrow salvage: *the ShadowOrders table is not dest P&L.* That is not the claim.

---

## 7. Assigned `RiskEngine.cs` and `LiveCopyPage.tsx` — what they do and do not prove

`RiskEngine` never sends FIX. It returns a boolean. Product hopper throws that boolean away (`AllowFixSend = false` persist). `ExecuteDemoCopyAsync` never constructs a `RiskEvaluationRequest`. RiskEngine **cannot** be cited as proof that sending is off.

`LiveCopyPage.tsx` is a consumer of `/api/copy/status` + `/api/copy/intents`. It paints `realCopyArmed`, SHADOW/LIVE counts, live sends, quote/trade, and blockers. It has **no** order builder. Its empty-state string (L57) **admits** dest auto-send. The blockers box title “Pepperstone cannot be filled” is **not** a gate — and on DemoDest the strongest blockers are not even added.

---

## 8. Live GET

Attempted, blocked (this slot):

- `http://localhost:5000/api/health` — SSRF blocked (localhost → 127.0.0.1)
- `http://127.0.0.1:5000/api/copy/status` — SSRF blocked

Launch profile is `http://localhost:5000`. Web client default is the same. No process snapshot this slot. File binds: `.env` `REAL_COPY_EXECUTION_ENABLED=true` → DI `RealCopyEnabled=true` unless a different process env overrides. I do **not** claim the running API’s current JSON.

---

## 9. Risk to capital

| Book | Exposure |
|---|---|
| Live Pepperstone `1369850` | **NONE this hop.** CopyOpen / DemoTestTrade / DemoMatrix refuse that account and non-demo host/sender. `CTraderFixSession` cannot send `35=D`. |
| Demo dest `5328266` | **NOT NONE.** Hosted 20s `ExecuteDemoCopyAsync` can and has sent `35=D`. Ledger 0.01 still open (`DestClosed: false`). RiskEngine / `RealCopyEnabled` / `CanPromoteToLive` do not sit in front of this hop. |
| Source MT5 | Not flattened by this code (`CopyRosterEngine` comment: dest-only). |

`SAFE_BY_ABSENCE` is **live-1369850 only**. Do not stamp it on demo dest.

---

## 10. What would be required to make the five-claim bundle PASS

1. Delete or disconnect `CTraderFixCopyOpen` / DemoTestTrade / DemoMatrix from any hosted tick, **or** change claim 1 to “`CTraderFixSession` has no `35=D` builder.”
2. Leave `CanPromoteToLive => false` (already true).
3. After logon (or at DI), assign `RealCopyEnabled = false` and prove it with a live GET of `/api/health` or `/api/settings`.
4. Gate `ExecuteDemoCopyAsync` behind the same `AllowFixSend` / LIVE / reconciled predicates, **or** stop calling it from `CopyTradingHostedService`.
5. Stop AUTO_ADMIT / dest send for `TraderState.SHADOW`, **or** change the claim to “`ShadowOrder` rows are not dest P&L.”

None of that is done on HEAD. This slot did not edit product.

---

## 11. Slot close

| Item | Value |
|---|---|
| Slot | 37 |
| Verdict | **FAIL** |
| Evidence | Assigned four files + dest hop + `.env` boolean + ledger/ER artifacts. Live GET blocked. |
| Claim 1 | PASS scoped to `CTraderFixSession`; FAIL process-wide (`Build("D")` hosted + tools) |
| Claim 2 | **PASS** (`CanPromoteToLive => false`) |
| Claim 3 | **FAIL** (no post-logon force-false; DI binds `true`) |
| Claim 4 | **FAIL** (demo dest hop is a P&L path) |
| Claim 5 | **FAIL** (SHADOW AUTO_ADMIT + dest send) |
| Risk to capital | **NONE** on live `1369850`; **not absent** on demo dest `5328266` |
| This slot sent `35=D` | **No** |
| `REAL_COPY` flipped | **No** |

End of P500_VERIFY_37. Product source was not modified. No secrets printed. This slot did not send `35=D`.
