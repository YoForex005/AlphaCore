# P500_VERIFY_10 — Adversarial verifier (slot 10)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_10.md` |
| Agent / slot | P500 adversarial verifier **10** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (live `src/`, `apps/`, lab `.env`) |
| Assigned SUT | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` (read in full this slot) |
| Supporting files (claim 3–5 hop) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `EfDashboardQueries.cs`, `DealIngestionService.cs`, `apps/api/Program.cs` |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Live `35=D` sent this slot | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None.** Quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73). No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live GET this pass | `GET http://127.0.0.1:5000/api/health`, `/api/copy/status`, `/api/settings`, `/api/ingest/status` **blocked** (loopback SSRF). Runtime flag **not** live-proven. File proof is enough to score claim 3. |
| Binding rule | **FAIL any assigned claim that cannot be proven from a live file or a live GET.** Sibling swarm books are not evidence. |

**Honesty:** Wanting dest profit is not an edge. Copy-all of the catalog would copy `RISK_BLOCKED` source losses. `SAFE_BY_ABSENCE` on live `1369850` is **not** “`RealCopyEnabled` stays false.” An armed env bit with no live sender is still an armed bit. Demo dest send is dest exposure, not dest profit.

---

## 0. Verdict (binding)

**FAIL.** Four of five assigned claims are file-proven. Claim **(3) `RealCopyEnabled` forced false after logon** is **disproven** on disk.

| # | Assigned claim | Measured | Class |
|---|---|---|---|
| 1 | no `35=D` builder | **PROVEN** on `CTraderFixSession.cs` (135/135). Residual: sibling `CTraderFixCopyOpen.Build("D")` is hosted. | **PASS_SCOPED** |
| 2 | `CanPromoteToLive` is false | **PROVEN** (`=> false`) | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **DISPROVEN** — only assignment is DI bind of `.env=true`; logon host never writes the bit | **FAIL** |
| 4 | sending now cannot be the profit path | **PROVEN** as dest-profit path: constructor `DestinationRealPnl=0`; live `1369850` refused; session cannot send. Residual: demo dest hop can `35=D` now (exposure, not dest-profit accounting). | **PASS_SCOPED** |
| 5 | SHADOW on demo is not destination profit | **PROVEN** — source state + paper `SimulateEntry`; dest DTO is literal `0` | **PASS** |

One-line:

```text
FAIL slot 10: CTraderFixSession 35=A only (no D builder); CanPromoteToLive=>false; RealCopyEnabled NOT forced false after logon (.env true + DI bind + logon logs-only); send-now is not dest-profit (DTO 0; live 1369850 refused) but demo dest hop can 35=D; SHADOW-on-demo is source/paper not dest PnL. Risk NONE on live 1369850. Live GET blocked.
```

Do **not** treat this FAIL as a license to send. Do **not** flip leftover `.env=true` into go-live. Operator should restore `REAL_COPY_EXECUTION_ENABLED=false` (this slot did **not** edit it).

---

## 1. Claim 1 — no `35=D` builder — PASS_SCOPED (`CTraderFixSession`)

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` is **135** lines. Read in full.

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
            // ... 49/56/50/57/52/98/108/141/553/554 ...
        };
        return Assemble(fields);
    }
```

Single `WriteAsync` (L49) of that logon. Then one `ReadAsync`. Socket disposed. Inbound `Extract(reply, "35")` is reply parse, not a builder. Tokens in this file: `Build("D")` = 0, `(35, "D")` = 0, `NewOrderSingle` = 0.

Hosted caller is logon-only:

```48:58:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            ...);
        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            ...);
```

**Residual (does not fail the assigned file claim):** sibling `CTraderFixCopyOpen.cs` L142–156 is a generic `Build(string type, ...)` and L95 writes `Build("D", ...)`. Hosted `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566 calls `CTraderFixCopyOpen.SendAsync`. CLI siblings `CTraderFixDemoTestTrade` / `CTraderFixDemoMatrix` also `Build("D")`. Product-wide “no `35=D` builder” would **FAIL**. The assigned claim, after “Read `CTraderFixSession.cs`”, is proven.

`CTraderFixCopyOpen` refuses live dest (`host` not `demo-` **or** `sender` not `demo.` **or** `account == "1369850"`) at L37–41.

---

## 2. Claim 2 — `CanPromoteToLive` is false — PASS

`D:\Prop\src\Domain\Scoring\BaselineScorer.cs` read in full (212 lines).

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

The unused `current` argument cannot change the result. `FromBaseline` (L189–207) returns only `INSUFFICIENT_DATA`, `RISK_BLOCKED`, `SHADOW`, `WATCH`, `EARLY_SCORE`. It never returns `LIVE` or `LIVE_CANDIDATE`.

Ingest writes `CurrentState = score.SuggestedState` (`DealIngestionService.cs` L140). Product callers of `CanPromoteToLive` are the unit test (`BaselineScorerTests.cs` L26) asserting false after three disciplined winners go to `SHADOW`.

`appsettings.json` `FeatureFlags.AutoPromotionEnabled=false` is unused config, not a second promotion gate. Promotion is closed in the state machine, not by that flag.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL

This is the FAIL trigger. Independent file proof:

**3.1 Only assignment in product C#**

`grep RealCopyEnabled\s*=` under `*.cs` / `*.tsx` = **one** hit:

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

**3.2 `.env` is `true` and is loaded**

`D:\Prop\.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only).

`apps/api/Program.cs` L10 `EnvFile.FindAndLoad()` then L13 `AddEnvironmentVariables()` then L15 `AddTraderIntelligence`. `EnvFile` candidates include `D:\Prop\.env` (`EnvFile.cs` L14).

**3.3 Logon host does not re-pin**

`CTraderFixLogonHostedService.ExecuteAsync` after both `TryLogonAsync` calls:

- Writes `Quote` / `Trade` `LoggedOn`, `Status`, `LastError`, `UpdatedAt` (L60–67).
- **Logs** `_runtime.RealCopyEnabled` (L69–70). Does **not** assign it.
- `PersistAsync` updates `FixSessionState` host/port/status/timestamps only (L101–107). No runtime flag.

There is no `RealCopyEnabled = false` anywhere after logon. The claim “forced false after logon” is the opposite of the file.

**3.4 Live GET**

Loopback GET blocked this slot. That does **not** rescue claim 3: the hosted process, if started with this `.env`, constructs the singleton as `true` and never forces it false. `LiveCopyPage` will render `REAL_COPY armed = YES` when `status.realCopyArmed` is true (`LiveCopyPage.tsx` L13; `CopyGateStatus.RealCopyArmed` is `_runtime.RealCopyEnabled` at `CopyTradingService.cs` L64).

`CTraderFixOptions.RealCopyExecutionEnabled` defaults false (`CTraderFixOptions.cs` L35) and is **not** what DI binds. Citing that POCO default as “forced false after logon” would be a lie.

---

## 4. Claim 4 — sending now cannot be the profit path — PASS_SCOPED

Proven from the assigned files + dest constructor + live refuse. Not proven as “no dest fill can exist.”

**4.1 `CTraderFixSession` cannot send a ticket**

Claim 1: only `35=A`. Logon is not a fill.

**4.2 Gated hop cannot approve a live send**

`RiskEngine.Evaluate` sets `AllowFixSend` only when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Every `Reject` persists `AllowFixSend=false` (L180–188). When `RealExecutionEnabled==false`, the comment at L90–93 says the shadow path never allows FIX send; `allowSend` is still AND-gated.

`CopyTradingService`:

- `VenueReconciled = false` (const, L20). Increasing intents hit `VENUE_NOT_RECONCILED` before approve.
- Persist **always** `AllowFixSend = false` (L324).
- Live branch L330 requires `decision.AllowFixSend && CurrentState==LIVE && NewOrderSingleImplemented && VenueReconciled`. `CanPromoteToLive=>false` plus `FromBaseline` never emitting `LIVE` means that branch is dead unless someone hand-writes `LIVE` into `TraderScores`.
- `NewOrderSingleImplemented => DemoDest` (L50). Reports that still say `NOS=const false` are **STALE**.

**4.3 Product dest-profit path is a literal zero**

```43:46:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            shadowPnl,
            0,
            0,
            0,
```

`OverviewDto.DestinationRealPnl` is the second `0`. No dest realized-PnL aggregator. `LiveCopyPage` has **no** dest-PnL column and **no** send control. Blocker copy: “Pepperstone cannot be filled” (L24). Empty-state text admits demo dest auto-sends (L57) — that is dest **exposure**, not dest-profit accounting.

**4.4 Live dest identity is refused**

`CTraderFixCopyOpen.SendAsync` L37–41 returns without writing `35=D` when `account == "1369850"` (or non-demo host/sender).

**4.5 Residual — do not over-claim SAFE_BY_ABSENCE on demo**

`CopyTradingHostedService` 20s tick calls `ExecuteDemoCopyAsync` (L30). That method **bypasses** `RiskEngine.Evaluate` and can `Build("D")` on demo dest when `DemoDest` (host `demo-*`, sender `demo.*`, account ≠ `1369850`), roster `ADMITTED`, open XAU, `MaxAutoLots=0.05`. Ledger `D:\Prop\data\demo_copy_ledger.json` has one open dest fill: source `305750` / dest `237339770` / 0.01 / `DestFillPrice=4390.2` / `DestClosed=false`. That is dest exposure. It is **not** `DestinationRealPnl`. It is **not** a measured dest edge. Sending now is therefore **not** the product profit path.

This slot did not send. Absolute “demo dest EV cannot be positive” is **unproven** (no live GET of dest account). That does not convert send-now into a profit path.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — PASS

**5.1 SHADOW is a source scoring state**

`TraderStateMachine.FromBaseline` emits `SHADOW` when `quality >= 70 && risk < 40` after early eligibility (L200–201). `FeatureSnapshot.NetPnl` is **source** reconstructed XAU (`BaselineScorer.ComputeFeatures` L66, L111).

**5.2 Paper shadow is not dest**

Hopper `GenerateShadowIntentsAsync` for `{SHADOW, LIVE_CANDIDATE, LIVE}` writes `Status="SHADOW_ONLY"` and `ShadowCopyEngine.SimulateEntry` (L336–359). `SimulateEntry` marks a synthetic fill from a quote (L35–61 of `ShadowCopyEngine.cs`). Dashboard `shadowPnl` is `Sum(SourceVsShadowSlippage)` (`EfDashboardQueries.cs` L29) — slippage vs source, not dest realized PnL.

**5.3 Dest profit is a different column and is hard-zero**

`DestinationRealPnl` constructor `0` (claim 4.3). `LiveCopyPage` shows `SHADOW traders` as a count of `TraderState.SHADOW` (`CopyTradingService.GetStatusAsync` L59), next to `Live sends` = count of `ExecutionIntents` with `SentAt != null` (L57) — not dest PnL.

**5.4 Policy / roster do not turn SHADOW into dest profit**

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `WATCH` / `EARLY_SCORE` / `INSUFFICIENT_DATA` as `TRADER_NOT_SHADOW_YET` (L81–85). That is an eligibility label, not dest PnL. `CopyGroupFilter` **requires** demo/contest groups for admit. A SHADOW source on a demo group can be `ADMITTED` and then demo-copied (claim 4 residual). The dest fill, if any, is dest exposure. The SHADOW badge and the paper shadow book remain source/paper. They are **not** destination profit.

---

## 6. `LiveCopyPage.tsx` (70/70) — assigned file, not a send path

Read in full. GETs `/api/copy/status` and `/api/copy/intents` only (`hooks.ts` L60–65). No POST. Renders `realCopyArmed`, SHADOW/LIVE counts, `liveSends`, `shadowFills`, QUOTE/TRADE, blockers, intent table. Cannot be the profit path. Cannot force `RealCopyEnabled` false. Does not implement `35=D`. Does not implement `CanPromoteToLive`.

---

## 7. Risk to capital

| Book | Risk |
|---|---|
| Live Pepperstone `1369850` | **NONE** this slot — `CTraderFixSession` is `35=A` only; `CopyOpen` refuses that account; persist `AllowFixSend=false`; `CanPromoteToLive=>false`. |
| Demo dest (lab `DemoDest` + 20s `ExecuteDemoCopyAsync`) | **Not absent.** File-proven hop can emit `35=D`. Existing ledger dest `237339770` still open. Not dest-profit accounting (`DestinationRealPnl=0`). |
| This slot | No attach. No send. No `.env` edit. |

---

## 8. Stale pins this slot refuses to repeat

| Pin | Status |
|---|---|
| `NewOrderSingleImplemented = const false` | **STALE** — HEAD is `=> DemoDest` (`CopyTradingService.cs` L50). |
| Product / tree `35=D=0` | **STALE** — `CTraderFixCopyOpen.Build("D")` + hosted caller. Assigned `CTraderFixSession` is still `35=A` only. |
| `RealCopyEnabled` forced false after logon / W500 “stays false” | **FALSE** — claim 3. |
| `CTraderFixOptions.RealCopyExecutionEnabled` default false as runtime | **UNBOUND** — DI reads env key, not that POCO. |
| Live GET dest PnL / armed bit | **UNPROVEN this slot** (SSRF). Not used as PASS evidence. |
