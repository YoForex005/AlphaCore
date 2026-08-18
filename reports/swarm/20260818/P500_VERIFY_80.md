# P500_VERIFY_80 — Adversarial verifier (slot 80)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_80.md` |
| Agent / slot | P500 adversarial verifier **80** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Role | Independent verifier. **Did not trust** sibling `P500_BOOK` / `P500_VERIFY` / `CREDENTIALS` / README prose. Re-read the four assigned files + adjacent send/logon hop this pass. |
| Assigned SUT | `CTraderFixSession.cs` (135/135), `BaselineScorer.cs` (212/212), `RiskEngine.cs` (189/189), `LiveCopyPage.tsx` (70/70) — all read in full this slot |
| Supporting files (claims 1/3–5 hop) | `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs` (gate + `Build("D")`), `CTraderFixDemoMatrix.cs`, `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `apps/api/Program.cs`, `EnvFile.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `CopyTradingModels.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `CopyGroupFilter.cs`, `DemoCopyLedger.cs`, `data/demo_copy_ledger.json` (public dest ids only), `DealIngestionService.cs`, `CTraderFixOptions.cs`, `BaselineScorerTests.cs`, `hooks.ts`, `launchSettings.json` |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Live `35=D` sent this slot | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None.** Quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73) and already-public dest ids `5328266` / `1369850`. No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live GET this pass | **Blocked.** `web_fetch` `http://127.0.0.1:5000/api/health` and `http://localhost:5000/api/copy/status` → SSRF private-IP deny. **No** live body. Any claim that needs a live GET is **FAIL**. |
| Binding rule | **FAIL any assigned claim that cannot be proven from a live file or a live GET.** Sibling swarm books are not evidence. |

**Honesty:** Wanting dest profit is not an edge. A TLS Logon `35=A` is not a NewOrderSingle. `SAFE_BY_ABSENCE` on live `1369850` is **not** “`RealCopyEnabled` stays false.” Destination constructor `$0` is not a measured dest book. A demo hopper that can `Build("D")` is not `CTraderFixSession`. Copy-all of the catalog would copy `RISK_BLOCKED` source losses. Do not print secrets.

---

## 0. Verdict (binding)

**FAIL.** Claim 2 is file-proven. Claims **1, 3, 4, 5** fail the bar as written (unscoped / disproven / not proven).

| # | Assigned claim | Measured | Class |
|---|---|---|---|
| 1 | no `35=D` builder | Assigned `CTraderFixSession` is `35=A` only. Unscoped “no builder” is **false**: product `Build("D")` ×5; hosted hop calls `CTraderFixCopyOpen`. | **FAIL** as written (**PASS_SESSION**) |
| 2 | `CanPromoteToLive` is false | `TraderStateMachine.CanPromoteToLive` is `=> false`. `FromBaseline` never returns `LIVE`. | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **DISPROVEN.** Only assignment is DI bind of `.env=true`. Logon host logs the bit and never writes false. Live GET blocked. | **FAIL** |
| 4 | sending now cannot be the profit path | **Cannot prove.** Hosted `ExecuteDemoCopyAsync` **sends now** on DemoDest (`Build("D")`). Ledger has an **open** dest fill. Dest DTO constructor `0` is not dest-account P&L. Live dest book not GET-able. | **FAIL** |
| 5 | SHADOW on demo is not destination profit | Paper `SimulateEntry` is not dest cash (**PASS_PAPER**). Unscoped: `SHADOW` is the dest `AUTO_ADMIT` floor; hopper sends dest `35=D` for `ADMITTED` without requiring `LIVE`. | **FAIL** as written |

One-line:

```text
FAIL slot 80: CTraderFixSession 35=A only (PASS_SESSION); product Build("D") x5 hosted. CanPromoteToLive=>false PASS. RealCopyEnabled NOT forced false after logon (.env true + DI bind + logon logs-only) FAIL. Send-now CAN be demo dest cash (ExecuteDemoCopyAsync + ledger 305750/237339770 open; dest DTO 0 is constructor) FAIL. SHADOW paper != dest PnL (PASS_PAPER) but SHADOW is dest AUTO_ADMIT FAIL_UNSCOPED. Live GET blocked. Risk NONE on live 1369850; demo dest not absent.
```

Do **not** treat this FAIL as a license to send. Do **not** flip leftover `.env=true` into go-live. Operator should restore `REAL_COPY_EXECUTION_ENABLED=false` (this slot did **not** edit it).

**Risk to capital:** **NONE on live Pepperstone `1369850`** (`CTraderFixSession` cannot send; `CTraderFixCopyOpen` refuses `account == "1369850"`). **Not absent on demo dest `5328266`** (20s hosted `ExecuteDemoCopyAsync` → `Build("D")`). Flag may be **armed**; that is **not** a live-send license.

Stale siblings this slot contradicts: any book that pins product `35=D=0` / `NOS=const false` / persist L306 / logon re-pin false; `CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**”; README “Real NewOrderSingle is **off** (`…=false`)”.

---

## 1. Claim 1 — no `35=D` builder — FAIL as written (PASS_SESSION only)

### 1.1 Assigned session file (full read, 135/135)

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`

Public API is **only** `TryLogonAsync`. The sole outbound builder is `BuildLogon`. Tag 35 is hardcoded `"A"`:

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

| Fact | Measured this pass |
|---|---|
| Physical lines | **135 / 135** (ends L135 `}`) |
| Literal `35=D` / `(35, "D")` / `Build("D")` | **0** |
| `NewOrderSingle` identifier | **0** |
| Outbound tag 35 actually built | **`"A"` only** |
| `WriteAsync` | **1** (L49) of that logon, then one `ReadAsync`, socket disposed |
| Inbound `Extract(reply, "35")` | Reply parse, **not** a builder |

Hosted caller is logon-only:

```48:58:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            ...);
        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            ...);
```

**Scoped to `CTraderFixSession.cs` the claim is proven.** That is not the assigned English.

### 1.2 Unscoped product — `35=D` builder exists (this is why claim 1 FAILs)

`grep Build("D")` under product `*.cs` this pass = **5** call sites, **0** of them in `CTraderFixSession.cs`:

| File | Sites |
|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra.ToArray())` after generic `Build(string type, ...)` L142–156 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | L139, L163, L197 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | L93 |

`CTraderFixCopyOpen.Build` is a generic MsgType assembler. Hosted hop **does** call it:

```528:530:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
            var close = await CTraderFixCopyOpen.SendAsync(
                host, sender, target, account, password,
                fill.SourceLogin, fill.SourcePositionId, fill.IsLong, fill.Lots, ct, fill.DestPositionId);
```

```566:569:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                var fill = await CTraderFixCopyOpen.SendAsync(
                    host, sender, target, account, password,
                    seat.SourceLogin.ToString(), trade.PositionId.ToString(),
                    trade.Direction == TradeDirection.Long, trade.MaxVolumeLots, ct);
```

`CopyTradingHostedService` L30 invokes `ExecuteDemoCopyAsync` every **20s** after an 8s startup delay.

CopyOpen refuses live dest (`host` not `demo-` **or** `sender` not `demo.` **or** `account == "1369850"`) at L37–41. DemoTestTrade has the same live refuse (L43–47). That **does not** make “no `35=D` builder” true.

**Claim 1 FAIL as written.** PASS only if silently re-scoped to `CTraderFixSession`. Adversarial verifier does not re-scope.

---

## 2. Claim 2 — `CanPromoteToLive` is false — PASS

`D:\Prop\src\Domain\Scoring\BaselineScorer.cs` read in full (212 lines).

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

| Fact | Measured |
|---|---|
| Expression | `=> false` — unused `current` cannot change it |
| `FromBaseline` returns `LIVE` / `LIVE_CANDIDATE` | **Never.** Ceiling is `SHADOW` |
| Product callers of `CanPromoteToLive` | Unit test only (`BaselineScorerTests.cs` L26 asserts false after three disciplined winners go to `SHADOW`) |
| Ingest write | `DealIngestionService.cs` L140 `CurrentState = score.SuggestedState` — so persisted state cannot become `LIVE` via the scorer |

`TraderState` enum still *has* `LIVE_CANDIDATE=4` and `LIVE=5` (`TraderState.cs`). That is a type, not a promotion path. `CTraderFixOptions.RealCopyExecutionEnabled` default `false` is a **different** unused POCO (`CTraderFixOptions.cs` L35) and is **not** `CanPromoteToLive`.

**Claim 2 PASS.** File-proven.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL (disproven)

None of the four assigned files force `RealCopyEnabled = false` after logon.

- `CTraderFixSession` has **no** `RealCopyEnabled` identifier.
- `BaselineScorer` has **no** runtime flag.
- `RiskEngine` has `RealExecutionEnabled` as an **input**. L90–93 is an **empty** comment block; it does not write any runtime singleton.
- `LiveCopyPage.tsx` L13 renders `status?.realCopyArmed ? 'YES' : 'NO'` — display only.

### 3.1 Only assignment in product C#

`grep RealCopyEnabled\s*=` under `*.cs` / `*.tsx` this pass = **one** hit:

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

Zero writers of `RealCopyEnabled = false`.

### 3.2 `.env` is `true` and is loaded

`D:\Prop\.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; value not a secret).

`apps/api/Program.cs` L10 `EnvFile.FindAndLoad()` then L13 `AddEnvironmentVariables()` then L15 `AddTraderIntelligence`. `EnvFile.FindAndLoad` candidates include `D:\Prop\.env` (`EnvFile.cs` L14).

Therefore a process started from this tree constructs the singleton as **`true`**.

### 3.3 Logon host does not re-pin

`CTraderFixLogonHostedService.ExecuteAsync` after both `TryLogonAsync` calls:

- Writes `Quote` / `Trade` `LoggedOn`, `Status`, `LastError`, `UpdatedAt` (L60–67).
- **Logs** `_runtime.RealCopyEnabled` (L68–70). Does **not** assign it.
- `PersistAsync` updates `FixSessionState` host/port/status/timestamps only (L101–107). No runtime flag.

There is no `RealCopyEnabled = false` anywhere after logon. The assigned claim is the **opposite** of the file.

`CTraderFixOptions.RealCopyExecutionEnabled` defaults false (`CTraderFixOptions.cs` L35) and is **not** what DI binds. Citing that POCO default as “forced false after logon” would be a lie.

### 3.4 Live GET

Loopback GET blocked this slot (SSRF). That does **not** rescue claim 3: the hosted process, if started with this `.env`, constructs the singleton as `true` and never forces it false.

`CopyTradingService.GetStatusAsync` L64: `RealCopyArmed: _runtime.RealCopyEnabled`.
`LiveCopyPage.tsx` L13 will render `REAL_COPY armed = YES` when that bit is true.
`/api/health` L55 and `/api/settings` L76 also expose the same singleton. **Not live-proven this pass.**

**Claim 3 FAIL.** Disproven from files. Live GET not available and not required to score the force-false claim.

---

## 4. Claim 4 — sending now cannot be the profit path — FAIL

Cannot prove from a file or live GET that “sending now cannot be the profit path.” The opposite is wired on demo dest.

### 4.1 What the assigned files actually say

| File | What it proves | What it does **not** prove |
|---|---|---|
| `CTraderFixSession` | This class cannot send a ticket (`35=A` only) | Product cannot send |
| `RiskEngine` | `AllowFixSend` is `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Rejects set `AllowFixSend=false` (L187). Empty L90–93 does **not** hard-stop evaluate | Hosted dest send goes through Evaluate |
| `LiveCopyPage.tsx` L57 | UI copy: “Demo dest auto-sends after a trader is ADMITTED…” | Dest cash is zero |
| `BaselineScorer` | Source quality / SHADOW ceiling | Dest P&L |

`LiveCopyPage` empty-state **advertises** dest send. That is evidence **against** “sending cannot be the profit path,” not for it.

### 4.2 Hosted send-now hop (file-proven)

```19:42:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
        await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            ...
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
            ...
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
```

`ExecuteDemoCopyAsync` (`CopyTradingService.cs` L483–605):

- Returns 0 only if `!DemoDest` (host not `demo-` / sender not `demo.` / account is `1369850`) or password blank.
- **Does not call `RiskEngine.Evaluate`.**
- Opens dest via `CTraderFixCopyOpen.SendAsync` → `Build("D")` for every `ADMITTED` roster seat with an open XAU source ≤ `MaxAutoLots=0.05`.
- Closes dest via the same `Build("D")` + tag 721 when source is completed.
- Caps `maxPerTick = 5`.

`NewOrderSingleImplemented => DemoDest` (L50). `VenueReconciled` **const false** at L20, but `GetStatusAsync` **reports** `VenueReconciled: DemoDest` (L67) — status lie, not a send interlock on the dest hop.

Shadow persist **forces** `AllowFixSend = false` at L324 regardless of `decision.AllowFixSend`. That gates the **intent** table, not `ExecuteDemoCopyAsync`.

### 4.3 Dest “profit” in the dashboard is a constructor zero — not a mark

```33:52:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        return new OverviewDto(
            accounts,
            brokers,
            xauTraders,
            three,
            ...
            shadowPnl,
            0,
            0,
            0,
            ...
            _runtime.RealCopyEnabled);
```

`OverviewDto` position 11 is `DestinationRealPnl` (`DashboardModels.cs` L16) — literal **`0`**. That is **not** proof dest cash is 0. It is proof the API does not measure dest cash.

`ShadowPnl` is `Sum(SourceVsShadowSlippage)` — paper slippage, not dest realized.

### 4.4 On-disk dest fill (not a live GET)

`D:\Prop\data\demo_copy_ledger.json` this pass:

| Field | Value (public dest/source ids only) |
|---|---|
| SourceLogin | `305750` |
| SourcePositionId | `21250421` |
| Lots | `0.01` |
| DestPositionId | `237339770` |
| DestClOrdId | `C20260818093047317` |
| DestFillPrice | `4390.2` |
| DestClosed | **`false`** |

`ExecuteDemoCopyAsync` L500–512 **re-seeds** this same open row if missing. An open dest ticket on demo `5328266` **is** dest-account P&L (unmarked). This slot did not GET the venue book (SSRF). Cannot claim dest cash is flat. Cannot claim sending “cannot be the profit path.”

Live `1369850` is still refused (CopyOpen L37–41). That is `SAFE_BY_ABSENCE` on **live**, not “no profit path.”

**Claim 4 FAIL.** File-proven dest send hop + open dest ledger. Dest DTO `0` is not dest profit. Live dest book not GET-able.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — FAIL as written (PASS_PAPER)

### 5.1 What SHADOW is (assigned scorer)

`SHADOW` is a **source-trader state** from `FromBaseline` when `quality >= 70 && risk < 40` (L200–201). Features (`NetPnl`, martingale, …) are computed from **completed source XAU** (`ComputeFeatures` L42–127). That number is source book, not dest cash.

`AfterHighEarlyScore() => SHADOW` (L209). Promotion to LIVE is closed (`CanPromoteToLive => false`).

`LiveCopyPage.tsx` L14 shows `status?.shadowTraders` — a **count** of source scores in `SHADOW`, not dest PnL.

### 5.2 Paper path (PASS_PAPER)

`GenerateShadowIntentsAsync` hopper `{SHADOW, LIVE_CANDIDATE, LIVE}` (L202). After Evaluate, persist `AllowFixSend=false` (L324) and (unless the dead LIVE+NOS+Reconciled branch) status `SHADOW_ONLY` + `ShadowCopyEngine.SimulateEntry` (L336–360).

`SimulateEntry` (`ShadowCopyEngine.cs` L35–61) writes a modeled price / slippage. `MarkToMarket` exists; dest cash is never booked. Dashboard `ShadowPnl` = sum of `SourceVsShadowSlippage`. Dest DTO = `0`.

**Paper SHADOW is not destination profit.** That much is file-proven.

### 5.3 Why the unscoped claim still FAILs

`XauUsdOneToOneCopyPolicy.IsTraderEligible` **rejects** `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET` (L81–85). Combined with `FromBaseline` never emitting `LIVE`/`LIVE_CANDIDATE`, **`SHADOW` is the dest AUTO_ADMIT floor.**

`CopyRosterEngine.Decide` returns `AUTO_ADMIT` when `IsTraderEligible` (L72–80), after also requiring demo/contest group (`CopyGroupFilter.IsDemoOrContest`). `TickRosterAsync` writes `Status = "ADMITTED"` (L154).

`ExecuteDemoCopyAsync` then sends dest `35=D` for every `ADMITTED` seat with an open XAU ≤ 0.05 lots — **no `CurrentState == LIVE` check**, no `Evaluate`, no `CanPromoteToLive`.

So: a source trader that the scorer labeled `SHADOW` on a demo/contest group **can** be the reason a dest `35=D` is sent. Dest cash from that send is dest P&L (unmarked). The claim “SHADOW on demo is not destination profit” is therefore **not proven** as dest-safety. It is only proven as “the SHADOW **ledger row** is paper.”

Live GET of dest PnL blocked. Cannot prove dest cash is 0.

**Claim 5 FAIL as written.** PASS_PAPER residual.

---

## 6. Assigned-file census (this slot)

| File | Lines read | `35=D` builder | `CanPromoteToLive` | `RealCopyEnabled` force-false | Dest profit |
|---|---|---|---|---|---|
| `src/Fix.CTrader/Sessions/CTraderFixSession.cs` | 135/135 | **No** (`35=A` only) | n/a | n/a (no identifier) | n/a (cannot send) |
| `src/Domain/Scoring/BaselineScorer.cs` | 212/212 | n/a | **`=> false`** | n/a | Source `NetPnl` only |
| `src/Domain/Risk/RiskEngine.cs` | 189/189 | n/a | n/a | Empty L90–93; `AllowFixSend` follows input flag | No dest cash field |
| `apps/web/src/pages/LiveCopyPage.tsx` | 70/70 | n/a | n/a | Displays `realCopyArmed` | Empty-state **advertises** demo dest auto-send |

---

## 7. Live GET

Attempted this pass:

- `GET http://127.0.0.1:5000/api/health` — **SSRF blocked** (private IP).
- `GET http://localhost:5000/api/copy/status` — **SSRF blocked**.

`launchSettings.json` profile `http` binds `http://localhost:5000`. No live body. Claims that need a live flag/PnL snapshot stay **FAIL**, not “assumed from last swarm.”

This slot: no Manager Connect, no TLS, no Logon, no `35=D`.

---

## 8. Risk to capital (measured, not hoped)

| Book | Exposure this pass |
|---|---|
| Live Pepperstone `1369850` | **NONE.** `CTraderFixSession` outbound is `35=A` only. `CTraderFixCopyOpen` / `CTraderFixDemoTestTrade` refuse `account == "1369850"`. `DemoDest` is false if account is that id. |
| Demo dest `5328266` (default in logon host L41; lab `.env` DemoDest-shaped) | **Not absent.** 20s hosted `ExecuteDemoCopyAsync` → `Build("D")`. On-disk ledger `305750` / dest `237339770` **open** (`DestClosed=false`). Dest DTO `$0` is constructor, not a mark. |
| `REAL_COPY` bit | **Armed in files** (`.env` L73 `true` + DI L41). **Not** a live-send license. **Not** forced false after logon. |
| This slot | Report only. 0 product edits. 0 sends. 0 flag flips. |

An armed env bit with no live sender is still an armed bit. Demo dest send is dest exposure, not dest-profit accounting.

---

## 9. Slot complete

Wrote `D:\Prop\reports\swarm\20260818\P500_VERIFY_80.md`. Overall **FAIL**. Claim 2 PASS. Claims 1/3/4/5 FAIL as written. Live GET blocked. Risk **NONE** on live `1369850`. Demo dest residual.
