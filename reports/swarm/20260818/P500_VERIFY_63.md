# P500_VERIFY_63 — Adversarial verifier (slot 63)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_63.md` |
| Agent / slot | P500 adversarial verifier **63** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (live `src/`, `apps/`, lab `.env`) |
| Assigned SUT | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` — **read in full this slot** |
| Supporting files (claims 3–5 hop) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `CopyGroupFilter.cs`, `EfDashboardQueries.cs`, `DealIngestionService.cs`, `EfTradingStore.cs`, `EnvFile.cs`, `apps/api/Program.cs`, `apps/web/src/api/hooks.ts` |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Live `35=D` sent this slot | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None.** Quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73), public host prefix `demo-`, public sender prefix `demo.`, public dest ids `5328266` / `1369850`. No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live GET this pass | `GET http://127.0.0.1:5000/api/health` (and `/api/copy/status`, `/api/settings`) **blocked** — `web_fetch` SSRF reject; `open_page` retrieve fail. Runtime bit **not** live-proven. File proof is enough to **FAIL** claim 3. |
| Binding rule | **FAIL any assigned claim that cannot be proven from a live file or a live GET.** Sibling swarm books are not evidence. This slot re-read the four assigned files and the hop. |

**Honesty:** Wanting dest profit is not an edge. Copy-all of the catalog would copy `RISK_BLOCKED` source losses. `SAFE_BY_ABSENCE` on live `1369850` is **not** “`RealCopyEnabled` stays false after logon.” An armed env bit with no live sender is still an armed bit. Demo dest send is dest **exposure**, not dest-profit accounting.

---

## 0. Verdict (binding)

**FAIL.** Claim 3 is **disproven** on disk. Claim 1 is proven only when scoped to `CTraderFixSession`. Claims 2 / 4 (dest-profit path) / 5 are file-proven.

| # | Assigned claim | Measured | Class |
|---|---|---|---|
| 1 | no `35=D` builder | **PROVEN** on `CTraderFixSession.cs` (135/135, only `(35, "A")`). Unscoped product-wide: **FAIL** — sibling `CTraderFixCopyOpen.Build("D")` is hosted. | **PASS_SCOPED** / **FAIL_UNSCOPED** |
| 2 | `CanPromoteToLive` is false | **PROVEN** — `TraderStateMachine.CanPromoteToLive(...) => false` | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **DISPROVEN** — only C# assignment is DI bind of `.env=true`; logon host logs the bit and never writes false | **FAIL** |
| 4 | sending now cannot be the profit path | **PROVEN** as dest-profit path: `OverviewDto.DestinationRealPnl` constructor `0`; `CTraderFixSession` cannot send a ticket; live `1369850` refused; persist `AllowFixSend=false`. Residual: hosted demo hop can `35=D` now (exposure, not dest-profit accounting). | **PASS_NOT_BOOKED_DEST_PROFIT** |
| 5 | SHADOW on demo is not destination profit | **PROVEN** — source state + paper `SimulateEntry`; dest DTO is literal `0` | **PASS** |

One-line:

```text
FAIL slot 63: CTraderFixSession 35=A only (no D builder in assigned file; product Build("D") residual); CanPromoteToLive=>false; RealCopyEnabled NOT forced false after logon (.env true + DI bind + logon logs-only); send-now is not dest-profit (DTO 0; live 1369850 refused) but demo dest hop can 35=D; SHADOW-on-demo is source/paper not dest PnL. Risk NONE on live 1369850. Live GET blocked.
```

Do **not** treat this FAIL as a license to send. Do **not** flip leftover `.env=true` into go-live. Operator should restore `REAL_COPY_EXECUTION_ENABLED=false` (this slot did **not** edit it).

---

## 1. Claim 1 — no `35=D` builder — PASS_SCOPED (`CTraderFixSession`) / FAIL_UNSCOPED

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` is **135** lines. Read in full this slot.

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

Single `WriteAsync` (L49) of that logon. Then one `ReadAsync`. Socket disposed via `using`. Inbound `Extract(reply, "35")` (L55) is reply parse, not a builder. File tokens this slot: `(35, "A")` = 1; `Build("D")` = 0; `(35, "D")` = 0; `NewOrderSingle` = 0. The only other `35` hits are inbound extract and the reject string `"Logon rejected 35="`.

Hosted caller is logon-only:

```48:58:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            ...);
        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            ...);
```

**Residual (does not fail the assigned-file claim; fails an unscoped “product has no 35=D builder” claim):** sibling `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L142–156 is a generic `Build(string type, ...)` that writes tag `(35, type)`. L95:

```95:95:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
            await Write(ssl, Build("D", sender, target, seq, extra.ToArray()), timeout.Token);
```

Hosted `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566 calls `CTraderFixCopyOpen.SendAsync`. CLI siblings `CTraderFixDemoTestTrade` / `CTraderFixDemoMatrix` also `Build("D")`. Product-wide “no `35=D` builder” is **false**. The assigned claim, after “Read `CTraderFixSession.cs`”, is proven.

`CTraderFixCopyOpen` refuses live dest at L37–41 when host does not start with `demo-` **or** sender does not start with `demo.` **or** `account == "1369850"`.

---

## 2. Claim 2 — `CanPromoteToLive` is false — PASS

`D:\Prop\src\Domain\Scoring\BaselineScorer.cs` read in full this slot (212 lines).

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

The unused `current` argument cannot change the result. `FromBaseline` never returns `LIVE` or `LIVE_CANDIDATE` (enum values exist at `TraderState.cs` L8–10; the machine does not emit them).

Ingest writes `CurrentState = score.SuggestedState` (`DealIngestionService.cs` L140). Product callers of `CanPromoteToLive` are the unit test (`BaselineScorerTests.cs` L26) asserting false after three disciplined winners go to `SHADOW`.

`appsettings.json` `FeatureFlags.AutoPromotionEnabled=false` is unused by `TraderStateMachine`. `SettingsController` can persist a Redis string; it does not call `CanPromoteToLive` and does not rewrite `TraderScores`. Promotion is closed in the state machine, not by that flag.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL

This is the FAIL trigger. Independent file proof this slot:

### 3.1 Only assignment in product C#

`grep RealCopyEnabled\s*=` under `*.cs` / `*.tsx` = **one** product hit:

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

No other assignment. Property is a public setter (`LiveRuntimeStatus.cs` L32) but nothing after logon writes it.

### 3.2 `.env` is `true` and is loaded

`D:\Prop\.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret).

`apps/api/Program.cs` L10 `EnvFile.FindAndLoad()` then L13 `AddEnvironmentVariables()` then L15 `AddTraderIntelligence`. `EnvFile` candidates include the hard path `D:\Prop\.env` (`EnvFile.cs` L14).

### 3.3 Logon host does not re-pin

`CTraderFixLogonHostedService.ExecuteAsync` after both `TryLogonAsync` calls:

- Writes `Quote` / `Trade` `LoggedOn`, `Status`, `LastError`, `UpdatedAt` (L60–67).
- **Logs** `_runtime.RealCopyEnabled` (L68–70). Does **not** assign it.
- `PersistAsync` updates `FixSessionState` host/port/status/timestamps only (L101–107). No runtime flag.

There is no `RealCopyEnabled = false` anywhere after logon. The claim “forced false after logon” is the opposite of the file.

```68:70:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _log.LogInformation(
            "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
            quote.LoggedOn, trade.LoggedOn, _runtime.RealCopyEnabled, account);
```

### 3.4 Live GET

Loopback GET blocked this slot (`web_fetch` SSRF; `open_page` retrieve fail). That does **not** rescue claim 3: the hosted process, if started with this `.env`, constructs the singleton as `true` and never forces it false.

`LiveCopyPage` will render `REAL_COPY armed = YES` when `status.realCopyArmed` is true (`LiveCopyPage.tsx` L13). `CopyGateStatus.RealCopyArmed` is `_runtime.RealCopyEnabled` (`CopyTradingService.cs` L64). `GET /api/health` and `GET /api/settings` both expose the same bit (`Program.cs` L55, L76). Those endpoints were **not** live-proven this slot; they are cited only as the file-defined surface.

`CTraderFixOptions.RealCopyExecutionEnabled` defaults false (`CTraderFixOptions.cs` L35) and is **not** what DI binds. Citing that POCO default as “forced false after logon” would be a lie.

---

## 4. Claim 4 — sending now cannot be the profit path — PASS_NOT_BOOKED_DEST_PROFIT

Proven from the assigned files + dest constructor + live refuse. **Not** proven as “no dest fill can exist.”

### 4.1 `CTraderFixSession` cannot send a ticket

Claim 1: only `35=A`. Logon is not a fill.

### 4.2 Gated hop cannot approve a live send

`RiskEngine.Evaluate` sets `AllowFixSend` only when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Every `Reject` persists `AllowFixSend=false` (L180–188). When `RealExecutionEnabled==false`, the comment at L90–93 says the shadow path never allows FIX send; `allowSend` is still AND-gated. `MaxSlippage` is declared on `RiskLimits` (L18) and is **unread** by `Evaluate` — not a send gate.

`CopyTradingService`:

- `VenueReconciled = false` (const, L20). Increasing intents hit `VENUE_NOT_RECONCILED` (RiskEngine L84–85) before approve.
- Persist **always** `AllowFixSend = false` (L324), even if `Evaluate` returned true.
- Live branch L330 requires `decision.AllowFixSend && CurrentState==LIVE && NewOrderSingleImplemented && VenueReconciled`. `CanPromoteToLive=>false` plus `FromBaseline` never emitting `LIVE` means that branch is dead unless someone hand-writes `LIVE` into `TraderScores`.
- `NewOrderSingleImplemented => DemoDest` (L50). Reports that still say `NOS=const false` are **STALE**.

### 4.3 Product dest-profit path is a literal zero

```33:46:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
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
```

`OverviewDto` field order (`DashboardModels.cs` L5–22): the first `0` after `shadowPnl` is `DestinationRealPnl`. No dest realized-PnL aggregator exists. `RiskDashboardDto` is likewise constructed as five literal zeros (`EfDashboardQueries.cs` L208).

`LiveCopyPage` has **no** dest-PnL column and **no** send control. Blocker copy: “Pepperstone cannot be filled” (L24). Empty-state text admits demo dest auto-sends (L57) — that is dest **exposure**, not dest-profit accounting.

### 4.4 Live dest identity is refused

`CTraderFixCopyOpen.SendAsync` L37–41 returns without writing `35=D` when `account == "1369850"` (or non-demo host/sender).

### 4.5 Residual — do not over-claim SAFE_BY_ABSENCE on demo

Lab `.env` public dest identity (no secrets):

- L49 `CTRADER_FIX_HOST=demo-us-eqx-01.p.c-trader.com` → starts with `demo-`
- L64 `CTRADER_FIX_TRADE_SENDER_COMP_ID=demo.pepperstone.5328266` → starts with `demo.`
- L50 `CTRADER_FIX_ACCOUNT_ID=5328266` → **not** `1369850`

Therefore `CopyTradingService.DemoDest` is **true** when this `.env` is loaded, and `NewOrderSingleImplemented => DemoDest` is **true**.

`CopyTradingHostedService` 20s tick (after 8s delay) calls `TickRosterAsync` → `GenerateShadowIntentsAsync` → `ExecuteDemoCopyAsync` (L28–30). `ExecuteDemoCopyAsync` **bypasses** `RiskEngine.Evaluate` and can `Build("D")` on demo dest when `DemoDest`, roster `ADMITTED`, open XAU, `MaxAutoLots=0.05`.

On-disk ledger `D:\Prop\data\demo_copy_ledger.json`: source `305750` / dest `237339770` / 0.01 / `DestFillPrice=4390.2` / `DestClosed=false`. That is dest exposure. It is **not** `DestinationRealPnl`. It is **not** a measured dest edge. Sending now is therefore **not** the product profit path.

This slot did not send. Absolute “demo dest EV cannot be positive” is **unproven** (no live GET of dest account). That does not convert send-now into a booked dest-profit path.

Unscoped reading “sending now cannot happen” would **FAIL**. The assigned wording is “cannot be the **profit** path.” Dest DTO remains `0`.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — PASS

### 5.1 SHADOW is a source scoring state

`TraderStateMachine.FromBaseline` emits `SHADOW` when `quality >= 70 && risk < 40` after early eligibility (`BaselineScorer.cs` L200–201). `FeatureSnapshot.NetPnl` is **source** reconstructed XAU (`ComputeFeatures` L66, L111).

### 5.2 Paper shadow is not dest

Hopper `GenerateShadowIntentsAsync` for `{SHADOW, LIVE_CANDIDATE, LIVE}` writes `Status="SHADOW_ONLY"` and `ShadowCopyEngine.SimulateEntry` (`CopyTradingService.cs` L336–359). `SimulateEntry` marks a synthetic fill from a quote (`ShadowCopyEngine.cs` L35–61). Dashboard `shadowPnl` is `Sum(SourceVsShadowSlippage)` (`EfDashboardQueries.cs` L29) — slippage vs source, not dest realized PnL.

`PersistDemoShadowAsync` (`EfTradingStore.cs` L251–330) only writes `SHADOW_ONLY` + `SimulateEntry` when `state == TraderState.SHADOW`. That is paper. It does not call `CTraderFixCopyOpen`.

### 5.3 Dest profit is a different column and is hard-zero

`DestinationRealPnl` constructor `0` (claim 4.3). `LiveCopyPage` shows `SHADOW traders` as a count of `TraderState.SHADOW` (`CopyTradingService.GetStatusAsync` L59), next to `Live sends` = count of `ExecutionIntents` with `SentAt != null` (L57) — not dest PnL.

### 5.4 Policy / roster do not turn SHADOW into dest profit

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `WATCH` / `EARLY_SCORE` / `INSUFFICIENT_DATA` as `TRADER_NOT_SHADOW_YET` (L81–85). That is an eligibility label, not dest PnL. It **allows** `SHADOW` (and `LIVE_CANDIDATE` / `LIVE`) if 20 completed XAU, net > 0, no size pattern, and demo/contest group.

`CopyGroupFilter` **requires** demo/contest groups for admit (`CopyGroupFilter.cs` L9–23). A SHADOW source on a demo group can be `AUTO_ADMIT` (`CopyRosterEngine.cs` L72–80) and then demo-copied (claim 4 residual). The dest fill, if any, is dest exposure. The SHADOW badge and the paper shadow book remain source/paper. They are **not** destination profit.

---

## 6. `LiveCopyPage.tsx` (70/70) — assigned file, not a send path

Read in full. GETs `/api/copy/status` and `/api/copy/intents` only (`hooks.ts` L60–65). No POST. Renders `realCopyArmed`, SHADOW/LIVE counts, `liveSends`, `shadowFills`, QUOTE/TRADE, blockers, intent table. Cannot be the profit path. Cannot force `RealCopyEnabled` false. Does not implement `35=D`. Does not implement `CanPromoteToLive`.

Empty-state (L57) is honest about demo dest auto-sends after `ADMITTED`. That sentence is dest exposure, not dest profit.

---

## 7. RiskEngine.cs (assigned) — send gate, not a profit engine

Read in full (190 lines). `Evaluate` can `Approve` with `AllowFixSend=true` only if all four AND-gates hold (L147–150). Persist hop then **overwrites** that to `false` (`CopyTradingService.cs` L324). Demo dest hop **skips** `Evaluate`. So the engine is not the live-profit path and is not the demo-send path.

`MaxSlippage` (L18) is unused. Citing unused slippage as a live-send block would be a lie.

---

## 8. Risk to capital

| Book | Risk |
|---|---|
| Live Pepperstone `1369850` | **NONE** this slot — `CTraderFixSession` is `35=A` only; `CopyOpen` refuses that account; persist `AllowFixSend=false`; `CanPromoteToLive=>false`. |
| Demo dest (lab `DemoDest` + 20s `ExecuteDemoCopyAsync`, account `5328266`) | **Not absent.** File-proven hop can emit `35=D`. Existing ledger dest `237339770` still open. Not dest-profit accounting (`DestinationRealPnl=0`). |
| This slot | No attach. No send. No `.env` edit. Live GET blocked. |

---

## 9. Stale pins this slot refuses to repeat

| Pin | Status |
|---|---|
| `NewOrderSingleImplemented = const false` | **STALE** — HEAD is `=> DemoDest` (`CopyTradingService.cs` L50). Lab `.env` makes `DemoDest` true. |
| Product / tree `35=D=0` | **STALE** — `CTraderFixCopyOpen.Build("D")` + hosted caller. Assigned `CTraderFixSession` is still `35=A` only. |
| `RealCopyEnabled` forced false after logon / W500 “stays false” | **FALSE** — claim 3. |
| `CTraderFixOptions.RealCopyExecutionEnabled` default false as runtime | **UNBOUND** — DI reads env key, not that POCO. |
| Live GET dest PnL / armed bit | **UNPROVEN this slot** (SSRF). Not used as PASS evidence. |
| “Sending now cannot happen” | **FALSE** on demo dest. Claim 4 is dest-**profit** path, not dest-send absence. |

---

End of P500_VERIFY_63. Product source was not modified. No secrets printed. This slot did not send `35=D`. `REAL_COPY` was not flipped.
