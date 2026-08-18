# P500_VERIFY_75 — Adversarial four-file verify (slot 75)

| Field | Value |
|---|---|
| Slot | **75** |
| Date | 2026-08-18 |
| Agent | P500_VERIFY_75 (adversarial; sibling `P500_*` / `W500_*` numbers are **not** evidence) |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_75.md` |
| Assigned | Read `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`. Confirm: (1) no `35=D` builder; (2) `CanPromoteToLive` is false; (3) `RealCopyEnabled` forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. **FAIL any claim that cannot be proved from a file or live GET.** |
| Assigned reads (this pass, full file) | `CTraderFixSession.cs` **135/135**; `BaselineScorer.cs` **212/212**; `RiskEngine.cs` **189/189**; `LiveCopyPage.tsx` **70/70** |
| Adjacent (this pass) | `CTraderFixLogonHostedService.cs`; `DependencyInjection.cs`; `CopyTradingService.cs`; `CopyTradingHostedService.cs`; `CTraderFixCopyOpen.cs`; `CTraderFixDemoTestTrade.cs`; `CTraderFixDemoMatrix.cs`; `CTraderFixOptions.cs`; `LiveRuntimeStatus.cs`; `ShadowCopyEngine.cs`; `EfDashboardQueries.cs` L21–52; `DashboardModels.cs` L5–22; `XauUsdOneToOneCopyPolicy.cs`; `CopyRosterEngine.cs`; `CopyGroupFilter.cs`; `DemoCopyLedger.cs`; `apps/api/Program.cs`; `apps/web/src/api/hooks.ts`; `tests/Unit/BaselineScorerTests.cs`; `TraderState.cs`; `.env` L49/L50/L64/L73 **flag + public dest identity only**; `D:\Prop\data\demo_copy_ledger.json` |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| `.env` modified | **No.** |
| Secrets printed | **None.** No tag 554, no manager/FIX/DB/proxy passwords. Quoted only the already-on-disk boolean `REAL_COPY_EXECUTION_ENABLED=true`, public dest ids `5328266` / `1369850`, public host prefix `demo-`, public sender prefix `demo.`. |
| Live GET this slot | **Attempted and blocked.** `web_fetch` `http://127.0.0.1:5000/api/health` → SSRF reject on loopback. `open_page` `http://127.0.0.1:5000/api/health` and `http://localhost:5000/api/copy/status` → retrieve fail. **No live JSON** for `realCopyEnabled`, quote/trade logon, dest PnL, or intents. |
| Live `35=D` sent this slot | **No.** |
| `REAL_COPY` flipped this slot | **No.** |
| Method | Independent full `read_file` of the four assigned files, then the logon/DI/copy hop they actually call. Targeted `grep` for `35=D` / `Build("D")` / `CanPromoteToLive` / `RealCopyEnabled =` / `REAL_COPY_EXECUTION_ENABLED` / `DestinationRealPnl`. Prior swarm text treated as **untrusted**. |
| Honesty rule | A `35=A` logon is not a fill. A comment is not a runtime pin. A dashboard constructor `0` is not dest cash. `CanPromoteToLive => false` is not a send interlock. Env `true` is not a live-`1369850` license. Sibling hop `Build("D")` **is** a dest path. Wanting profit is not an edge. |

Assigned claims:

1. no `35=D` builder
2. `CanPromoteToLive` is false
3. `RealCopyEnabled` forced false after logon
4. sending now cannot be the profit path
5. SHADOW on demo is not destination profit

---

## 0. Verdict (binding)

**FAIL.** The five-claim bundle does not hold as written. Claim 2 is file-proved. Claim 1 holds **only** if scoped to `CTraderFixSession.cs`. Claim 3 is **disproved** on disk. Claim 4 is **disproved** as an unscoped “sending cannot be dest P&L” statement (hosted demo hopper). Claim 5 holds for paper SHADOW / booked dest PnL and **fails** as “SHADOW cannot become dest cash.” Live RAM was not obtained; unproved runtime state is **FAIL**, not assumed.

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS_FILE / FAIL_UNSCOPED** | Assigned session **135/135** outbound MsgType is `(35, "A")` only. Product `Build("D")` exists (`CTraderFixCopyOpen` L95 hosted; `CTraderFixDemoTestTrade` ×3; `CTraderFixDemoMatrix` L93). |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(TraderState current) => false` at `BaselineScorer.cs` L211. `FromBaseline` ceiling is `SHADOW`. Unit test asserts it. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | **Disproved.** Logon host **reads** the flag and **never assigns** `false`. Sole `RealCopyEnabled =` write is DI bind of `.env` `REAL_COPY_EXECUTION_ENABLED=true`. Live GET of RAM not obtained. |
| 4 | sending now cannot be the profit path | **FAIL** | Hosted 20 s tick calls `ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.SendAsync` → `Build("D")` **without** `RiskEngine.Evaluate` and **without** reading `RealCopyEnabled`. Lab `.env` satisfies `DemoDest`. Ledger holds dest fill `305750` / dest pos `237339770` / px `4390.2` / `DestClosed=false`. Dashboard `DestinationRealPnl` is constructor `0`, not a mark. Live `1369850` refused. |
| 5 | SHADOW on demo is not destination profit | **PASS_PAPER / FAIL_UNSCOPED** | Paper: `SHADOW` is a source state; `ShadowPnl` = sum of `SourceVsShadowSlippage`; dest DTO is `0`; UI shows counts. Unscoped: `SHADOW` is AUTO_ADMIT-eligible; hopper sends on `ADMITTED` and does **not** require `LIVE`. `LiveCopyPage` L57 admits dest auto-send after ADMIT. |

One-line:

```text
FAIL. CTraderFixSession is 35=A only (PASS_FILE). CanPromoteToLive=>false (PASS). RealCopyEnabled is NOT forced false after logon (FAIL). Hosted demo hopper CAN 35=D (FAIL as “sending cannot be profit”). SHADOW paper ≠ dest PnL; SHADOW can ADMIT and dest-send (FAIL unscoped). Live 1369850 refused. Live GET blocked. This slot sent 0.
```

**Risk to capital:** **NONE on live `1369850`** (`SAFE_BY_ABSENCE`: assigned session has no `35=D`; `CTraderFixCopyOpen` refuses non-`demo-` host, non-`demo.` sender, or account `1369850`). **Not absent on demo dest `5328266`:** hosted hop can emit `35=D` and the on-disk ledger already records an open dest fill. This slot sent **0**.

---

## 1. Claim 1 — no `35=D` builder — **PASS_FILE / FAIL_UNSCOPED**

Assigned file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135** lines, full read this pass).

Token census **this file only**:

| Pattern | Hits |
|---|---:|
| Literal `35=D` | **0** |
| `NewOrderSingle` | **0** |
| `(35, "D")` / `Build("D")` | **0** |
| `(35, "A")` | **1** (`BuildLogon` L96) |
| `ssl.WriteAsync` | **1** (L49) — bytes of that Logon |
| `Extract(..., "35")` | **1** — **inbound** reply (L55) |

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

| Check | Measured this pass |
|---|---|
| Outbound MsgType in this file | **only** `(35, "A")` L96 |
| Socket lifetime | `using TcpClient` + `await using SslStream`; disposed after one read |
| Inbound `35=A` treated as | `LoggedOn = true` (L56–64) — **logon ack, not a fill** |
| Hosted caller | `CTraderFixLogonHostedService` L48–58 calls `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212). Same builder. |

Adversarial residual (**fails the unscoped wording**): product `*.cs` has **five** `Build("D")` call sites. This slot read those files.

| File | What this slot measured |
|---|---|
| `src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra)` after TRADE logon + SecurityList. **Refuses** non-`demo-` host, non-`demo.` sender, or account `1369850` (L37–42). **Called from** `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566, which is on the 20 s hosted tick. |
| `src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | `Build("D", …)` at L139, L163, L197. Called from `tools/DemoFixTestTrade` only (not DI). |
| `src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` L93 | `SendD` → `Build("D", …)`. Tools/matrix only. |
| `src\Fix.CTrader\Testing\FixSimulationHarness.cs` | Builds inbound `35=8` ExecutionReport. **Not** an outbound NewOrderSingle. |

Claim 1 as written against **`CTraderFixSession.cs`** is true. Tree-wide “no `35=D` builder” is **false** on HEAD. Unscoped confirmation **FAIL**.

---

## 2. Claim 2 — `CanPromoteToLive` is false — **PASS**

Assigned file: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (**212** lines, full read this pass).

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
| Ceiling of `FromBaseline` | `SHADOW` (L200–201). No `LIVE` / `LIVE_CANDIDATE` return. |
| `AfterHighEarlyScore` | `=> TraderState.SHADOW` (L209) |
| `CanPromoteToLive` | **hard `false`**. Parameter `current` is unused. |
| Enum still contains `LIVE` | `TraderState.cs` L9–10 (`LIVE_CANDIDATE=4`, `LIVE=5`) — unused by this scorer. |
| Product callers of `CanPromoteToLive` | **0** in `src\` / `apps\`. Only `tests\Unit\BaselineScorerTests.cs` L21–26. |
| Test | three disciplined winners → `SHADOW`; `CanPromoteToLive(...).Should().BeFalse()`. |

There is **no** code path in this file that emits `TraderState.LIVE`. Promotion to live is not a scorer function.

**Does not** interlock the demo dest sender (claim 4). `ExecuteDemoCopyAsync` keys on roster `ADMITTED`, not `LIVE`.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL**

This claim is **false on disk**. Live RAM was **not** fetched. Per the assignment, a claim that cannot be proved from a file or live GET is **FAIL**. Here the file evidence **disproves** the claim.

### 3.1 Logon host does not re-pin

Assigned-adjacent file (the only post-logon writer of `LiveRuntimeStatus` FIX fields): `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`.

After both `TryLogonAsync` calls it writes Quote/Trade status and **logs** the flag. It never assigns it:

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

`grep` `RealCopyEnabled =` under product `*.cs`: **one hit**.

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

That is a **bind**, not a force-false.

### 3.2 Lab env is `true`

`D:\Prop\.env` L73 (flag key only): `REAL_COPY_EXECUTION_ENABLED=true`.

`apps\api\Program.cs` L10 loads that env (`EnvFile.FindAndLoad()`), then L15 `AddTraderIntelligence`. `/api/health` L55 and `/api/settings` L76 echo `runtime.RealCopyEnabled` — they do **not** override it.

### 3.3 Options default is unused by logon

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (`CTraderFixOptions.cs` L35). `CTraderFixLogonHostedService` does **not** read that POCO. `apps\fix-worker\Worker.cs` L21 reads `CTrader:RealCopyExecutionEnabled` with default `false` — that is the **fix-worker**, not API logon, and it still **does not send**.

Assigned `RiskEngine.cs` L90–93 is a **comment no-op** when `RealExecutionEnabled == false`. It does not write `LiveRuntimeStatus.RealCopyEnabled`.

### 3.4 What “after logon” actually leaves

| Object | After successful `35=A` |
|---|---|
| `_runtime.Quote.LoggedOn` / `_runtime.Trade.LoggedOn` | set from logon result |
| `_runtime.RealCopyEnabled` | **unchanged** from DI bind of env |
| This slot live GET of `/api/health` or `/api/settings` | **not obtained** (loopback blocked) |

Cannot claim “forced false after logon.” The opposite wiring is on disk. Claim 3 is **FAIL**.

---

## 4. Claim 4 — sending now cannot be the profit path — **FAIL**

Scope used: **destination cash activity**, not “booked `DestinationRealPnl`.” A constructor `0` is not proof that sending cannot be dest P&L. The assigned session cannot send. The **hosted hop can**.

### 4.1 Assigned session cannot send a ticket

`CTraderFixSession` is Logon-only (claim 1). A `35=A` ack is not dest PnL. **Not sufficient** to prove the process cannot send.

### 4.2 Scorer cannot put anyone in LIVE; RiskEngine paper wall is not the hopper

`CanPromoteToLive => false` (claim 2). Hopper live-send branch `CopyTradingService.cs` L330 requires `TraderState.LIVE` **and** `VenueReconciled` (const `false` at L20). That branch is **dead**. Persist hard-codes `AllowFixSend = false` (L324).

Assigned `RiskEngine` allow-send formula (L147–150):

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

Hopper `Evaluate` passes `Reconciled = VenueReconciled` (const `false`) → increasing actions reject `VENUE_NOT_RECONCILED` (`RiskEngine.cs` L84–85).

L90–93 (“Shadow path … never allows FIX send”) is a **comment no-op**. It does not return. The real paper wall is persist-false + dead LIVE branch.

### 4.3 Dest profit is not computed (does **not** prove claim 4)

`OverviewDto.DestinationRealPnl` exists (`DashboardModels.cs` L16). The product assignment in `EfDashboardQueries.GetOverviewAsync` is the literal `0` (positional arg after `shadowPnl`, L44). That is a constructor, **not** a mark-to-market of dest fills.

`LiveCopyPage.tsx` has **no** dest-PnL field, **no** send button, **no** “profit” column. It shows counts + blockers titled “Pepperstone cannot be filled” (L24). Hooks `useCopyStatus` / `useCopyIntents` are GETs only (`hooks.ts` L60–65). Empty-state copy at L57:

```56:58:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      {intents.length === 0 && (
        <p className="text-gray-500 text-sm">No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.</p>
      )}
```

That is UI admission of a dest send path, not proof it is absent.

### 4.4 Why the claim is **false**

HEAD **does** send on demo dest. Lab identity matches `DemoDest`:

| `.env` key (public) | Value | `DemoDest` predicate (`CopyTradingService.cs` L45–48) |
|---|---|---|
| `CTRADER_FIX_HOST` L49 | `demo-us-eqx-01.p.c-trader.com` | `StartsWith("demo-")` |
| `CTRADER_FIX_TRADE_SENDER_COMP_ID` L64 | `demo.pepperstone.5328266` | `StartsWith("demo.")` |
| `CTRADER_FIX_ACCOUNT_ID` L50 | `5328266` | `!= "1369850"` |

`CopyTradingHostedService.cs` L28–30 every 20 s: `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**.

`ExecuteDemoCopyAsync` (L483–605):

- returns 0 only if `!DemoDest` or empty password
- **does not** read `RealCopyEnabled`
- **does not** call `RiskEngine.Evaluate`
- seeds / keeps ledger row `305750` / `21250421` / dest `237339770` if missing (L500–512)
- closes dest via `CTraderFixCopyOpen.SendAsync(..., destPositionId)` when source completed
- opens dest via `CTraderFixCopyOpen.SendAsync` for each `ADMITTED` roster seat with an open XAUUSD trade ≤ `MaxAutoLots` (0.05)

On-disk ledger `D:\Prop\data\demo_copy_ledger.json` this pass:

| Field | Value |
|---|---|
| SourceLogin | `305750` |
| SourcePositionId | `21250421` |
| Lots | `0.01` |
| DestPositionId | `237339770` |
| DestClOrdId | `C20260818093047317` |
| DestFillPrice | `4390.2` |
| DestClosed | **false** |

That is an **open dest fill**, not paper SHADOW. `DestinationRealPnl = 0` does not mark it. Live `1369850` is refused at `CTraderFixCopyOpen` L37–42. Claim 4 as “sending now cannot be the profit path” **FAIL** (demo dest cash path exists now). Scoped “not booked dest profit on the dashboard / not live Pepperstone” would be a different claim.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **PASS_PAPER / FAIL_UNSCOPED**

### 5.1 Paper SHADOW is not dest cash — **PASS**

| Object | Measured |
|---|---|
| Scorer `SHADOW` | Source-tape state from `FromBaseline` (`BaselineScorer.cs` L200–201). Inputs are reconstructed MT5 XAU trades, not dest fills. |
| Shadow fill | `ShadowCopyEngine.SimulateEntry` (`ShadowCopyEngine.cs` L35–60) — modeled ask/bid + 0.05 pt latency. No socket. |
| Persist | `CopyTradingService.GenerateShadowIntentsAsync` L336–359 writes `ShadowOrder` after `SHADOW_ONLY`. |
| Dashboard `ShadowPnl` | `EfDashboardQueries.cs` L29: `Sum(SourceVsShadowSlippage)`. Slippage vs source, **not** dest realized. |
| Dashboard dest | constructor `0` (`EfDashboardQueries.cs` L44). |
| `LiveCopyPage` | SHADOW **count** (`status?.shadowTraders`) + shadow **fills** count. No dest-PnL cell. |

Assigned files do not book dest cash under the SHADOW label. Paper SHADOW ≠ destination profit. **PASS_PAPER**.

### 5.2 SHADOW is the dest AUTO_ADMIT floor — **FAIL_UNSCOPED**

`XauUsdOneToOneCopyPolicy.IsTraderEligible` (`XauUsdOneToOneCopyPolicy.cs` L81–85) rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET`. Eligible states are `SHADOW` and above, plus 20 completed XAU, source book > 0, demo/contest group, no size-pattern flags.

`CopyRosterEngine.Decide` (`CopyRosterEngine.cs` L72–80) returns `AUTO_ADMIT` when eligible and not already on roster.

`ExecuteDemoCopyAsync` iterates `Status == "ADMITTED"` roster rows (L542–569) and sends. It does **not** re-check `TraderState.LIVE`. `CopyGroupFilter` restricts **source** groups to demo/contest; dest is the lab demo FIX account `5328266`.

Therefore: a SHADOW demo/contest trader can be AUTO_ADMITTED and then receive a dest `35=D`. That is dest cash risk, even though the `ShadowPnl` column is paper slippage. Unscoped “SHADOW on demo is not destination profit” **FAIL**.

---

## 6. Live GET (this slot)

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` via `web_fetch` | **SSRF blocked** (loopback) |
| `http://127.0.0.1:5000/api/health` via `open_page` | retrieve fail |
| `http://localhost:5000/api/copy/status` via `open_page` | retrieve fail |

No live body. Runtime `realCopyEnabled` / FIX logon / intent rows **unproved**. File claims above stand. Runtime-only claims **FAIL**.

---

## 7. What this slot did not do

- Did not send `35=D`.
- Did not flip `REAL_COPY_EXECUTION_ENABLED`.
- Did not modify product, tests, or `.env`.
- Did not print secrets.
- Did not treat sibling swarm markdown as proof.

---

## 8. Binding close

**FAIL** the five-claim confirmation.

1. `CTraderFixSession` has no `35=D` builder (**PASS_FILE**). Product has hosted `Build("D")` (**FAIL_UNSCOPED**).
2. `CanPromoteToLive => false` (**PASS**).
3. `RealCopyEnabled` is **not** forced false after logon (**FAIL** / disproved).
4. Sending **can** be demo dest P&L now via `ExecuteDemoCopyAsync` (**FAIL**). Live `1369850` still refused. Dest DTO `0` is not a mark.
5. Paper SHADOW ≠ dest PnL (**PASS_PAPER**). SHADOW is dest AUTO_ADMIT (**FAIL_UNSCOPED**).

Live GET blocked. This slot sent **0**. Risk **NONE** on live `1369850`. Demo dest `5328266` hop **not** `SAFE_BY_ABSENCE`.
