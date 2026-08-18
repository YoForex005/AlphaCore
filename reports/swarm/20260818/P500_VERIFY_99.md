# P500_VERIFY_99 — Adversarial verifier (slot 99)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_99.md` |
| Agent / slot | P500 adversarial verifier **99** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (live `src/`, `apps/`, lab `.env`) |
| Assigned SUT | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` (read in full this slot) |
| Supporting files (claims 3–5 hop) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `CopyGroupFilter.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `DealIngestionService.cs`, `EfTradingStore.cs`, `EnvFile.cs`, `apps/api/Program.cs`, `DemoCopyLedger.cs`, `D:\Prop\data\demo_copy_ledger.json` |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Live `35=D` sent this slot | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None.** Quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73) and public dest ids `5328266` / `1369850`. No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live GET this pass | `GET http://127.0.0.1:5000/api/health` and `GET http://localhost:5000/api/health` **SSRF-blocked**. Launch URLs also include `:18720` / `:7294` — not live-proven. Runtime flag **not** live-proven. File proof is enough to score claim 3. |
| Binding rule | **FAIL any assigned claim that cannot be proven from a live file or a live GET.** Sibling swarm books are not evidence. |

**Honesty:** Wanting dest profit is not an edge. Copy-all of the catalog would copy `RISK_BLOCKED` source losses. `SAFE_BY_ABSENCE` on live `1369850` is **not** “`RealCopyEnabled` stays false.” An armed env bit with no live sender is still an armed bit. Demo dest send is dest **exposure**, not dest profit.

---

## 0. Verdict (binding)

**FAIL.** Claims 2 and 5 are file-proven. Claim 1 is proven only when scoped to `CTraderFixSession.cs`. Claim 4 is proven only as “not the booked dest-profit path.” Claim **(3) `RealCopyEnabled` forced false after logon** is **disproven** on disk.

| # | Assigned claim | Measured | Class |
|---|---|---|---|
| 1 | no `35=D` builder | **PROVEN** on `CTraderFixSession.cs` (135/135, only `(35, "A")`). **FAIL** if unscoped: product `Build("D")` ×5 hosted/tools. | **FAIL unscoped / PASS_SESSION** |
| 2 | `CanPromoteToLive` is false | **PROVEN** (`=> false`) | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **DISPROVEN** — only assignment is DI bind of `.env=true`; logon host never writes the bit | **FAIL** |
| 4 | sending now cannot be the profit path | **PROVEN** as dest-profit path: constructor `DestinationRealPnl=0`; live `1369850` refused; session cannot send. Residual: demo dest hop can `35=D` now (exposure, not dest-profit accounting). Absolute “demo dest EV cannot be positive” **unproven** (no live dest GET). | **PASS_NOT_BOOKED_DEST_PROFIT / FAIL if read as “no send exists”** |
| 5 | SHADOW on demo is not destination profit | **PROVEN** — source state + paper `SimulateEntry`; dest DTO is literal `0` | **PASS** |

One-line:

```text
FAIL slot 99: CTraderFixSession 35=A only (no D builder); product Build("D") x5 residual; CanPromoteToLive=>false; RealCopyEnabled NOT forced false after logon (.env true + DI bind + logon logs-only); send-now is not dest-profit (DTO 0; live 1369850 refused) but demo dest hop can 35=D; SHADOW-on-demo is source/paper not dest PnL. Risk NONE on live 1369850. Live GET blocked.
```

Do **not** treat this FAIL as a license to send. Do **not** flip leftover `.env=true` into go-live. Operator should restore `REAL_COPY_EXECUTION_ENABLED=false` (this slot did **not** edit it).

---

## 1. Claim 1 — no `35=D` builder — FAIL unscoped / PASS_SESSION

Assigned file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135/135**, read in full).

Outbound builder is only `BuildLogon`. Tag 35 is hardcoded `"A"`:

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

Single `WriteAsync` (L49) of that logon. Then one `ReadAsync` (L53). `using` TcpClient + SslStream — sockets disposed. Inbound `Extract(reply, "35")` (L55) is reply parse, not a builder. Tokens in this file: `Build("D")` = 0, `(35, "D")` = 0, `NewOrderSingle` = 0.

Hosted caller is logon-only:

```48:58:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            ...);
        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            ...);
```

**Residual (fails the claim if product-scoped):** `grep Build("D")` under `*.cs` = **5** product hits:

| File | Count | Role |
|---|---|---|
| `src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 | 1 | Hosted demo dest hopper (`CopyTradingService.ExecuteDemoCopyAsync` L528 close / L566 open) |
| `src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | 3 | CLI `tools/DemoFixTestTrade` |
| `src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` L93 | 1 | CLI matrix |

`CTraderFixCopyOpen.Build` is generic `(35, type)` (L142–156). Live dest identity is refused at L37–41 (`host` not `demo-` **or** `sender` not `demo.` **or** `account == "1369850"`). That refuse is **not** “no `35=D` builder.”

Unscoped “no `35=D` builder” **FAIL**. Assigned-file “`CTraderFixSession` has no `35=D` builder” **PASS**.

---

## 2. Claim 2 — `CanPromoteToLive` is false — PASS

Assigned file: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (**212/212**, read in full).

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

The unused `current` argument cannot change the result. Reachable `FromBaseline` set is `{INSUFFICIENT_DATA, RISK_BLOCKED, SHADOW, WATCH, EARLY_SCORE}`. Never `LIVE` or `LIVE_CANDIDATE`.

Persist copies that suggestion, not a promotion:

- `DealIngestionService.cs` L140: `CurrentState = score.SuggestedState`
- `EfTradingStore.UpsertScoreAsync` L232: `existing.CurrentState = score.CurrentState`

Unit test `BaselineScorerTests.cs` L21–26: three disciplined winners go to `SHADOW`; `CanPromoteToLive(...)` is asserted false.

`TraderState` enum still *defines* `LIVE = 5` (`TraderState.cs` L10). That is a name, not a reachable scorer output.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL

This is the FAIL trigger. Independent file proof. Live GET did **not** rescue the claim.

**3.1 Only assignment in product C#**

`grep RealCopyEnabled\s*=` under `*.cs` = **one** hit:

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

**3.2 `.env` is `true` and is loaded**

`D:\Prop\.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret quoted).

`apps/api/Program.cs` L10 `EnvFile.FindAndLoad()` then L13 `AddEnvironmentVariables()` then L15 `AddTraderIntelligence`. `EnvFile` candidates include `D:\Prop\.env` (`EnvFile.cs` L14).

**3.3 Logon host does not re-pin**

`CTraderFixLogonHostedService.ExecuteAsync` after both `TryLogonAsync` calls:

- Writes `Quote` / `Trade` `LoggedOn`, `Status`, `LastError`, `UpdatedAt` (L60–67).
- **Logs** `_runtime.RealCopyEnabled` (L68–70). Does **not** assign it.
- Log text: `"RealCopyArmed={Armed} NewOrderSingle still unimplemented."` — comment, not a write.
- `PersistAsync` updates `FixSessionState` host/port/status/timestamps only (L101–107). No runtime flag.

There is no `RealCopyEnabled = false` anywhere after logon. The claim “forced false after logon” is the opposite of the file.

**3.4 POCO default is not the runtime bit**

`CTraderFixOptions.RealCopyExecutionEnabled` defaults false (`CTraderFixOptions.cs` L35) and is **not** what DI binds. Citing that POCO as “forced false after logon” would be a lie.

**3.5 Live GET**

`web_fetch` of `http://127.0.0.1:5000/api/health` and `http://localhost:5000/api/health` returned **SSRF blocked**. Launch settings also list `:18720` / `:7294`. This slot has **no** live body. That does **not** rescue claim 3: the hosted process, if started with this `.env`, constructs the singleton as `true` and never forces it false.

`LiveCopyPage` will render `REAL_COPY armed = YES` when `status.realCopyArmed` is true (`LiveCopyPage.tsx` L13; `CopyGateStatus.RealCopyArmed` is `_runtime.RealCopyEnabled` at `CopyTradingService.cs` L64).

---

## 4. Claim 4 — sending now cannot be the profit path — PASS_NOT_BOOKED_DEST_PROFIT

Proven from the assigned files + dest constructor + live refuse. **Not** proven as “no dest fill can exist.”

**4.1 `CTraderFixSession` cannot send a ticket**

Claim 1: only `35=A`. Logon is not a fill.

**4.2 Gated hop cannot approve a live send**

`RiskEngine.Evaluate` (`D:\Prop\src\Domain\Risk\RiskEngine.cs`, **189/189**):

```90:93:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.RealExecutionEnabled == false && request.Action != CopyIntentAction.CloseExposure)
        {
            // Shadow path still evaluates risk but never allows FIX send.
        }
```

That comment is **not** an early return. `AllowFixSend` is computed later:

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

Every `Reject` persists `AllowFixSend=false` (L180–188). Approve paths still AND-gate send.

`CopyTradingService`:

- `VenueReconciled = false` (const, L20). Increasing intents hit `VENUE_NOT_RECONCILED` (RiskEngine L84–85) before approve.
- Persist **always** `AllowFixSend = false` (L324) — even if `Evaluate` would have set true.
- Live branch L330 requires `decision.AllowFixSend && CurrentState==LIVE && NewOrderSingleImplemented && VenueReconciled`. `CanPromoteToLive=>false` plus `FromBaseline` never emitting `LIVE` means that branch is dead unless someone hand-writes `LIVE` into `TraderScores`.
- `NewOrderSingleImplemented => DemoDest` (L50). Reports that still say `NOS=const false` are **STALE**.

**4.3 Product dest-profit path is a literal zero**

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

`OverviewDto` (`DashboardModels.cs` L16): `DestinationRealPnl` is the second `0`. No dest realized-PnL aggregator. `GetRiskAsync` returns `DailyPnl=0`, `Drawdown=0`, `XauLong=0`, `XauShort=0`, `XauNet=0` (L208). Trader row `ShadowPnl` is hardcoded `0` (L118).

`LiveCopyPage` has **no** dest-PnL column and **no** send control. Blocker copy: “Pepperstone cannot be filled” (L24). Empty-state text admits demo dest auto-sends (L57) — that is dest **exposure**, not dest-profit accounting.

**4.4 Live dest identity is refused**

`CTraderFixCopyOpen.SendAsync` L37–41 returns without writing `35=D` when `account == "1369850"` (or non-demo host/sender). `CTraderFixDemoTestTrade` has the same live-identity refuse (L43–47).

**4.5 Residual — do not over-claim SAFE_BY_ABSENCE on demo**

`CopyTradingHostedService` 20s tick (L21–41) calls, in order: `TickRosterAsync` → `GenerateShadowIntentsAsync` → `ExecuteDemoCopyAsync`. That last method **bypasses** `RiskEngine.Evaluate` and can `Build("D")` on demo dest when `DemoDest` (host `demo-*`, sender `demo.*`, account ≠ `1369850`), roster `ADMITTED`, open XAU, `MaxAutoLots=0.05`.

Ledger `D:\Prop\data\demo_copy_ledger.json` (read this slot): one open dest fill — source `305750` / pos `21250421` / dest `237339770` / clord `C20260818093047317` / 0.01 / `DestFillPrice=4390.2` / `DestClosed=false`. `ExecuteDemoCopyAsync` L500–511 will re-seed that same row if missing. That is dest exposure. It is **not** `DestinationRealPnl`. It is **not** a measured dest edge.

This slot did not send. Absolute “demo dest EV cannot be positive” is **unproven** (no live GET of dest account). That does not convert send-now into the product profit path.

If the assigned claim is read as “no send can exist now,” it **FAIL**s (hosted hopper). If read as “sending now is not how dest profit is booked,” it **PASS**es.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — PASS

**5.1 SHADOW is a source scoring state**

`TraderStateMachine.FromBaseline` emits `SHADOW` when `quality >= 70 && risk < 40` after early eligibility (`BaselineScorer.cs` L200–201). `FeatureSnapshot.NetPnl` is **source** reconstructed XAU (`ComputeFeatures` L66, L111). Quality formula (L152–160) can be high while `NetPnl` is later negative on a larger book — that is source scoring, not dest cash.

**5.2 Paper shadow is not dest**

Hopper `GenerateShadowIntentsAsync` for `{SHADOW, LIVE_CANDIDATE, LIVE}` writes `Status="SHADOW_ONLY"` and `ShadowCopyEngine.SimulateEntry` (`CopyTradingService.cs` L336–359). `SimulateEntry` marks a synthetic fill from a quote (`ShadowCopyEngine.cs` L35–61). Dashboard `shadowPnl` is `Sum(SourceVsShadowSlippage)` (`EfDashboardQueries.cs` L29) — slippage vs source, not dest realized PnL.

`EfTradingStore.PersistDemoShadowAsync` also writes `SHADOW_ONLY` paper rows when `state == SHADOW` (L267–318). Same engine. No FIX write.

**5.3 Dest profit is a different column and is hard-zero**

`DestinationRealPnl` constructor `0` (claim 4.3). `LiveCopyPage` shows `SHADOW traders` as a count of `TraderState.SHADOW` (`CopyTradingService.GetStatusAsync` L59), next to `Live sends` = count of `ExecutionIntents` with `SentAt != null` (L57) — not dest PnL.

**5.4 Policy / roster do not turn SHADOW into dest profit**

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `WATCH` / `EARLY_SCORE` / `INSUFFICIENT_DATA` as `TRADER_NOT_SHADOW_YET` (L81–85). That is an eligibility label, not dest PnL. `CopyGroupFilter` **requires** demo/contest groups for admit (`CopyGroupFilter.cs` L9–23; roster L52–53). A SHADOW source on a demo group can be `AUTO_ADMIT` (`CopyRosterEngine.cs` L72–80) and then demo-copied (claim 4 residual). The dest fill, if any, is dest exposure. The SHADOW badge and the paper shadow book remain source/paper. They are **not** destination profit.

---

## 6. `LiveCopyPage.tsx` (70/70) — assigned file, not a send path

Read in full. GETs `/api/copy/status` and `/api/copy/intents` only (`hooks.ts` L60–65). No POST. Renders `realCopyArmed`, SHADOW/LIVE counts, `liveSends`, `shadowFills`, QUOTE/TRADE, blockers, intent table. Cannot be the profit path. Cannot force `RealCopyEnabled` false. Does not implement `35=D`. Does not implement `CanPromoteToLive`.

Empty-state L57 is honest about demo dest auto-send. That sentence is UI copy, not a ticket.

---

## 7. Risk to capital

| Book | Risk |
|---|---|
| Live Pepperstone `1369850` | **NONE** this slot — `CTraderFixSession` is `35=A` only; `CopyOpen` refuses that account; persist `AllowFixSend=false`; `CanPromoteToLive=>false`. |
| Demo dest (lab `DemoDest` + 20s `ExecuteDemoCopyAsync`) | **Not absent.** File-proven hop can emit `35=D`. Existing ledger dest `237339770` still open. Not dest-profit accounting (`DestinationRealPnl=0`). |
| This slot | No attach. No send. No `.env` edit. Live GET blocked. |

---

## 8. Stale pins this slot refuses to repeat

| Pin | Status |
|---|---|
| `NewOrderSingleImplemented = const false` | **STALE** — HEAD is `=> DemoDest` (`CopyTradingService.cs` L50). |
| Product / tree `35=D=0` | **STALE** — `CTraderFixCopyOpen.Build("D")` + hosted caller + CLI siblings. Assigned `CTraderFixSession` is still `35=A` only. |
| `RealCopyEnabled` forced false after logon / W500 “stays false” | **FALSE** — claim 3. |
| `CTraderFixOptions.RealCopyExecutionEnabled` default false as runtime | **UNBOUND** — DI reads env key, not that POCO. |
| Live GET dest PnL / armed bit | **UNPROVEN this slot** (SSRF). Not used as PASS evidence. |
| Dest DTO `0` as a mark-to-market of dest `237339770` | **NOT A MARK** — constructor literal. |

---

## 9. Files read this slot (absolute)

- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` (header + live refuse)
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Application\Dashboard\DashboardModels.cs`
- `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs`
- `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs`
- `D:\Prop\src\Domain\Copy\CopyGroupFilter.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Infrastructure\Copy\DemoCopyLedger.cs`
- `D:\Prop\data\demo_copy_ledger.json`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\web\src\api\hooks.ts` (copy hooks)
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` (score persist)
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (upsert + paper shadow)
- `D:\Prop\src\Domain\Enums\TraderState.cs`
- `D:\Prop\tests\Unit\BaselineScorerTests.cs`
- `D:\Prop\.env` L73 boolean only

End of P500_VERIFY_99. Product source was not modified. No secrets printed. This slot did not send `35=D`.
