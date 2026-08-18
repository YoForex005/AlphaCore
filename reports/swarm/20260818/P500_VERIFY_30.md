# P500_VERIFY_30 — Adversarial verify of five copy/profit claims (slot 30)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_30.md` |
| Agent / slot | P500 adversarial verifier **30** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned SUTs (full re-read) | `src/Fix.CTrader/Sessions/CTraderFixSession.cs` (135 lines), `src/Domain/Scoring/BaselineScorer.cs` (212), `src/Domain/Risk/RiskEngine.cs` (189), `apps/web/src/pages/LiveCopyPage.tsx` (70) |
| Adjacent hops (read, not assigned) | `CTraderFixLogonHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `LiveRuntimeStatus.cs`, `DependencyInjection.cs`, `apps/api/Program.cs`, `CopyRosterEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `ShadowCopyEngine.cs`, `DemoCopyLedger.cs`, `CopyGroupFilter.cs`, `CopyLifecycle.cs`, `CTraderFixOptions.cs`, `apps/fix-worker/Worker.cs`, unit tests for scorer/risk, `.env` **boolean/host/public dest ids only** |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Secrets printed | **None.** Quoted only public dest ids `5328266` / `1369850`, host `demo-us-eqx-01.p.c-trader.com`, and boolean `REAL_COPY_EXECUTION_ENABLED=true`. Tag **554** never dumped. |
| Live GET this slot | **Blocked.** `web_fetch`/`open_page` to `http://127.0.0.1:5000/api/{copy/status,health,settings}` = SSRF / retrieve-fail. No process integers claimed. |
| Method | Independent `read_file` of the four assigned files. Targeted `grep` for `35=D` / `Build("D")` / `RealCopyEnabled` / `CanPromoteToLive`. Prior swarm (`P500_PROFIT_SYNTHESIS.md`, `E002_no_live_send.md`, `CREDENTIALS_AND_COPY_STATUS.md`, W500 `SAFE_BY_ABSENCE` pins) treated as **untrusted / stale**. Verdict **FAIL** if any assigned claim is false or cannot be proven from a file or a live GET. |

**Honesty rule:** `CTraderFixSession` is not the whole FIX tree. A compile-time `CanPromoteToLive => false` is not a dest-send interlock. A DI bind that can be `true` is not “forced false after logon.” A `ShadowOrder` row is not dest PnL. An on-disk ExecReport `150=F` **is** dest activity. Do not print secrets.

---

## 0. Verdict (binding)

**FAIL — claims 1, 3, and 4 do not hold as stated. Claims 2 and 5 hold from the assigned files.**

| # | Assigned claim | Result | Class |
|---|---|---|---|
| 1 | no `35=D` builder | **FAIL** (scoped PASS) | `CTraderFixSession` outbound MsgType is only `A`. Product has **three** `Build("D")` senders; hosted copy **calls** one of them. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive` is `=> false`. `FromBaseline` never returns `LIVE` / `LIVE_CANDIDATE`. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | Logon host **does not write** the flag. DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`. Only assignment in tree is DI L41. |
| 4 | sending now cannot be the profit path | **FAIL** | Cannot prove. Demo dest `35=D` is wired, ticked every 20s, and an on-disk ExecReport already shows a **fill**. Live `1369850` is refused. |
| 5 | SHADOW on demo is not destination profit | **PASS** | Scorer `SHADOW` + `ShadowCopyEngine.SimulateEntry` are paper. Dest fills live on a **different** path (`DEMO_SENT` / `CTraderFixCopyOpen`). |

One-line:

```text
FAIL. CTraderFixSession is 35=A only, but CTraderFixCopyOpen/DemoTestTrade/DemoMatrix build and send 35=D. CanPromoteToLive is hard-false. RealCopyEnabled is NOT forced false after logon (DI binds .env true). Demo dest send is a dest-P&L path (ledger fill 4390.2 on 5328266). SHADOW/ShadowOrder is not dest profit. Live GET unverified.
```

Stale text this slot **rejects** (do not reuse):

- `P500_PROFIT_SYNTHESIS.md` §0 / §2.1: “send impossible / `SAFE_BY_ABSENCE` / hosted sets `RealCopyEnabled = false` / `NewOrderSingleImplemented=false`.”
- `E002_no_live_send.md`: “no function emits FIX MsgType=D to a socket.”
- `CREDENTIALS_AND_COPY_STATUS.md`: “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**; Live `35=D` method does not exist.”
- W500 pins that `NewOrderSingleImplemented` is a const `false` and copy hop has zero `35=D`.

---

## 1. Claim 1 — no `35=D` builder — **FAIL** as stated; **PASS** if scoped only to `CTraderFixSession.cs`

### 1.1 Assigned file — outbound is Logon only

Full read of `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135). The only builder is `BuildLogon`. Tag 35 is literal `"A"`. The only `WriteAsync` sends that logon. The socket is `using`-disposed after one read. Inbound `Extract(reply, "35")` is a parser, not a builder. The error string `$"Logon rejected 35={msgType}"` echoes the **reply**.

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

Grep of this file: **0** hits for `NewOrderSingle`, `Build("D")`, `(35, "D")`, `35=D` as an outbound constructor.

`CTraderFixLogonHostedService` is the only hosted caller of `CTraderFixSession.TryLogonAsync` (QUOTE `:5211`, TRADE `:5212`). After logon it persists session rows. It never sends D.

### 1.2 Unqualified claim is false — three builders exist

Product `*.cs` `Build("D")` / `Build("D",` (re-grep this slot):

| File | What it does | Wired to hosted copy? |
|---|---|---|
| `src/Fix.CTrader/Sessions/CTraderFixCopyOpen.cs` L95 | `await Write(ssl, Build("D", sender, target, seq, extra.ToArray()), …)` after demo-host gate | **Yes** — `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566; `CopyTradingHostedService` L30 every 20s |
| `src/Fix.CTrader/Sessions/CTraderFixDemoTestTrade.cs` L139, L163, L197 | `Build("D", …)` flatten / open / close | Tools CLI (`tools/DemoFixTestTrade`); not DI |
| `src/Fix.CTrader/Sessions/CTraderFixDemoMatrix.cs` L93 | `SendD` → `Build("D", …)` market/limit/stop matrix | Tools CLI `--matrix` |

`CTraderFixCopyOpen.Build` is a generic MsgType assembler. `SendAsync` passes `"D"` after SecurityList gold lookup. Gate (L37–41) refuses non-`demo-` host, non-`demo.` sender, or account `1369850`. Current lab `.env` (public keys only):

- `CTRADER_FIX_HOST=demo-us-eqx-01.p.c-trader.com`
- `CTRADER_FIX_ACCOUNT_ID=5328266`
- `CTRADER_FIX_TRADE_SENDER_COMP_ID=demo.pepperstone.5328266`

That **passes** the gate. `CopyTradingService.DemoDest` (L45–48) is the same three predicates. `NewOrderSingleImplemented => DemoDest` is **not** const false.

On-disk dest fill (not a live GET; file evidence of a prior send):

- `D:\Prop\data\demo_copy_ledger.json` — dest pos `237339770`, `DestFillPrice=4390.2`, `DestClosed=false`
- `D:\Prop\reports\swarm\20260818\DEMO_COPY_OPEN.json` — `OrderSent=true`, `Filled=true`, inbound `35=8` `150=F` `39=2` on account `5328266`

**Claim 1 FAIL.** “No 35=D builder” is only true inside `CTraderFixSession.cs`. It is false for the product and false for the copy hop that the Live Copy page describes.

---

## 2. Claim 2 — `CanPromoteToLive` is false — **PASS**

Full read of `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`. The machine is in the same file:

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

Proven from this file:

- `CanPromoteToLive` **ignores** `current` and is unconditionally `false`.
- Ceiling of `FromBaseline` is `SHADOW` (or `RISK_BLOCKED` / `WATCH` / `EARLY_SCORE` / `INSUFFICIENT_DATA`). **Never** `LIVE` or `LIVE_CANDIDATE`.
- `AfterHighEarlyScore()` is `SHADOW`, not `LIVE`.
- Quality can be 100 and risk 0; still `SHADOW`.

Unit pin (`tests/Unit/BaselineScorerTests.cs` L21–26): three disciplined winners → `SHADOW` and `CanPromoteToLive(...).Should().BeFalse()`.

`LiveCopyPage.tsx` does not call the machine. It only renders `status.liveTraders` / `status.shadowTraders` from `GET /api/copy/status`.

**Residual (does not break claim 2):** `CopyRosterEngine` can `AUTO_ADMIT` a `SHADOW` trader who passes `XauUsdOneToOneCopyPolicy` (≥20 completed XAU, `XauNetPnl > 0`, demo/contest group). That is dest-roster, not a `LIVE` promotion.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL**

None of the four assigned files force the flag false.

| Surface | What it does to `RealCopyEnabled` |
|---|---|
| `CTraderFixSession.cs` | Does not mention the flag. |
| `BaselineScorer.cs` | Does not mention the flag. |
| `RiskEngine.cs` | Reads `request.RealExecutionEnabled`. If false, empty comment at L90–93 then `AllowFixSend` ANDs the bit at L147–150. **Does not mutate runtime.** |
| `LiveCopyPage.tsx` | Displays `status?.realCopyArmed ? 'YES' : 'NO'`. Display only. |
| `CTraderFixLogonHostedService.cs` L60–70 | Writes Quote/Trade `LoggedOn`/`Status`/`LastError`. **Reads** `_runtime.RealCopyEnabled` for a log line. **No assignment.** |
| `DependencyInjection.cs` L39–42 | **Only write in the product tree** (`grep RealCopyEnabled\s*=` → this one hit): `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` |
| `apps/api/Program.cs` L10+L13 | `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()`. |
| Lab `.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` |
| `CTraderFixOptions.RealCopyExecutionEnabled` | POCO default `false` — **not** the object `LiveRuntimeStatus` uses. Fix-worker reads a **different** key `CTrader:RealCopyExecutionEnabled` (fallback false) and still does not send. |

`CTraderFixLogonHostedService` after a successful `35=A` logon:

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

The log **lies** (“NewOrderSingle still unimplemented”) while `CopyTradingService.NewOrderSingleImplemented => DemoDest`. It does **not** pin the flag false.

`CopyTradingService.BuildBlockers` adds `"REAL_COPY_EXECUTION_ENABLED is false"` only when the runtime bit is already false (L621–622). With `.env` true, that blocker is **absent**.

Live GET of `/api/health` / `/api/settings` would have been the process-level proof. **This slot could not GET.** File proof is enough to **FAIL** “forced false after logon”: there is no post-logon write, and DI can (and lab env does) start the bit **true**.

---

## 4. Claim 4 — sending now cannot be the profit path — **FAIL**

“Cannot” is a strong claim. Adversarial bar: prove from a file or live GET that dest sending cannot produce dest PnL. That proof is **not** available. Opposite files exist.

### 4.1 What the four assigned files prove

- `CTraderFixSession` cannot be a profit path. It sends `35=A`, reads one reply, disposes. Logon is not a fill.
- `CanPromoteToLive` is false; scorer will not mint `LIVE`.
- `RiskEngine.Evaluate` sets `AllowFixSend = RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Rejects always `AllowFixSend=false`. Unit test `Real_flag_false_never_allows_fix_send` only covers the false fixture. If all four bits are true, **this class will approve a send**.
- `LiveCopyPage.tsx` empty state (L57) tells the operator dest auto-sends after `ADMITTED`. Title “Live copy portfolio” is UI, not a socket.

### 4.2 What the copy hop actually does (required to judge “sending now”)

`CopyTradingService.GenerateShadowIntentsAsync`:

- `VenueReconciled` is `const false` (L20).
- Risk request passes `Reconciled = VenueReconciled` → increasing actions reject `VENUE_NOT_RECONCILED`.
- Persist **overwrites** `AllowFixSend = false` (L324) regardless of `decision.AllowFixSend`.
- Live branch (L330) requires `decision.AllowFixSend && State==LIVE && NewOrderSingleImplemented && VenueReconciled`. With `VenueReconciled==false` and no scorer `LIVE`, this branch is dead. Intents go `SHADOW_ONLY`.

**That path is not the send path.**

`CopyTradingHostedService` every 20s (L27–30): `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**.

`ExecuteDemoCopyAsync` (L483–605):

- Returns 0 only if `!DemoDest` or password empty.
- Does **not** read `RealCopyEnabled`.
- Does **not** call `RiskEngine`.
- Does **not** require `TraderState.LIVE`.
- Calls `CTraderFixCopyOpen.SendAsync` to **close** ledger dests when the MT5 source is completed, and to **open** dests for `ADMITTED` roster seats with open XAU and `MaxVolumeLots <= 0.05`.
- Marks intents `DEMO_SENT` on fill.

`.env` makes `DemoDest==true`. Hosted loop + `CTraderFixCopyOpen` **is** sending now (capability). `DEMO_COPY_OPEN.json` is a completed dest fill on demo `5328266` (not `1369850`).

`LiveCopyPage` summary comes from `GetStatusAsync`: when `DemoDest`, summary is *“Demo dest auto-copy ON. Eligible demo/contest opens send on the 20s tick…”* (L76–77). Blockers skip `SAFE_BY_ABSENCE` when `DemoDest` (L610–616).

### 4.3 Why this is still not *live* Pepperstone profit — and why that does not save the claim

- Live identity `1369850` is refused in `CTraderFixCopyOpen` / `DemoDest`.
- Demo dest PnL is not withdrawable live capital.
- Scorer quality is **not** dest expectancy (`NetPnl` is **source** reconstructed XAU).
- `AllocationFactor = 1m` (1:1 lots) + `MaxAutoLots = 0.05m` is a size cap, not an edge proof.
- I did **not** live-GET dest balance / open dest tickets this slot.

The assigned claim is “sending now **cannot** be the profit path.” Dest sending **can** produce dest PnL (already filled once on disk). Whether that PnL is +EV is unmeasured. Unmeasured ≠ “cannot.”

**Claim 4 FAIL.** Do not recycle `SAFE_BY_ABSENCE` as a profit-path proof.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **PASS**

### 5.1 Scorer `SHADOW` is a source-tape label

`FromBaseline` returns `SHADOW` when `quality >= 70 && risk < 40` after 3 completed XAU (`EarlyScoreTradeCount = 3`). Inputs are `ReconstructedTradeResult.NetRealizedPnl` on **source** MT5 deals. No dest ticket, no dest mark, no FIX fill.

`CopyGroupFilter.IsDemoOrContest` only inspects the **source group name**. A demo/contest SHADOW trader is still a source-state, not dest PnL.

### 5.2 `ShadowOrder` is `SimulateEntry`

`GenerateShadowIntentsAsync` writes `Status = "SHADOW_ONLY"` and, if a quote row exists and risk outcome is `Approve`, `ShadowCopyEngine.SimulateEntry` (modeled 80 ms, ±0.05 point latency). `ShadowFill` never touches a socket.

`RiskEngine` L90–93 comment: *“Shadow path still evaluates risk but never allows FIX send.”* That comment is about `AllowFixSend` on this evaluator, not about `ExecuteDemoCopyAsync`.

Dashboard `shadowPnl` (`EfDashboardQueries` L29) sums `ShadowOrders.SourceVsShadowSlippage` — a **slippage model**, not dest realized.

### 5.3 `LiveCopyPage` does not turn SHADOW into dest profit

The page shows `SHADOW traders` and `Shadow fills` as separate stats from `Live sends`. It has no POST. It cannot mark dest PnL.

### 5.4 Caveat (does not flip claim 5)

A `SHADOW` trader who later passes policy (≥20 XAU, source XAU book > 0, demo/contest, not martingale) can be `ADMITTED` and then `ExecuteDemoCopyAsync` can dest-send. That dest PnL is **`DEMO_SENT`**, not SHADOW. Claim 5 as written (“SHADOW on demo is not destination profit”) is true of the SHADOW object. It is **not** a proof that demo dest is idle.

---

## 6. RiskEngine notes (assigned file; not a sixth claim)

Full read of `D:\Prop\src\Domain\Risk\RiskEngine.cs`.

- `AllowFixSend` is the only send bit this class emits. It is **not** a socket.
- `RealExecutionEnabled == false` does **not** reject. It can still `APPROVE` with `AllowFixSend=false` (test L21–26).
- Unreconciled **increasing** actions reject `VENUE_NOT_RECONCILED` (L84–85). Copy hop always passes `Reconciled=false`, so shadow-intent opens die here — then the hosted loop **sends anyway** via `ExecuteDemoCopyAsync`.
- `MaxPositionQuantity` default **5**, `MaxXauNet` **10**, daily loss **2000** are blow-up caps. Demo auto-copy uses `MaxAutoLots = 0.05` instead. RiskEngine is **bypassed** on that path.
- Claim 4 cannot be rescued by this file.

---

## 7. Live GET

Attempted this slot:

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/copy/status` | `web_fetch` SSRF-blocked; `open_page` retrieve-fail |
| `http://127.0.0.1:5000/api/health` | SSRF-blocked |
| `http://127.0.0.1:5000/api/settings` | SSRF-blocked |

No process `realCopyEnabled` / `shadowTraders` / `liveSends` integers are claimed. File + `.env` + on-disk ExecReport are the evidence. `P500_PROFIT_SYNTHESIS.md` mid-scoring table is **not** re-probed and is **stale** on send/flag.

---

## 8. Residuals

1. Hosted log line still says “NewOrderSingle still unimplemented” after TRADE logon — **false** when `DemoDest`.
2. `CopyTradingService` persist `AllowFixSend=false` does not bind `CTraderFixCopyOpen`.
3. `VenueReconciled` is `const false` in the risk/intent path and `DemoDest` in `GetStatusAsync` (`VenueReconciled: DemoDest`). The status DTO **lies** relative to the const used in `Evaluate`.
4. Demo dest account `5328266` already has (or had) dest pos `237339770` at 4390.2 with `DestClosed=false`. If the source trade is still open, flatten will not fire (`ShouldCloseDest` requires source completed).
5. `REAL_COPY_EXECUTION_ENABLED=true` is armed in lab env. It is **not** the demo-send gate. Turning it false would **not** stop `ExecuteDemoCopyAsync`.

---

## 9. Slot close

| Item | Value |
|---|---|
| Slot | **30** |
| Bundle verdict | **FAIL** |
| PASS | (2) `CanPromoteToLive => false`; (5) SHADOW / `ShadowOrder` ≠ dest profit |
| FAIL | (1) unqualified “no 35=D builder”; (3) RealCopy forced false after logon; (4) sending cannot be the profit path |
| Risk to capital | **DEMO dest `5328266` file-proven send/fill path.** Live `1369850` refused. Not `NONE`. Process live-state **unverified** (GET blocked). |
| Secrets | None printed |
| Product edits | None |
