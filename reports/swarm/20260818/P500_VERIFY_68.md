# P500_VERIFY_68 — Adversarial four-file verify (slot 68)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_68.md` |
| Agent / slot | P500 adversarial verifier **68** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (live `src/`, `apps/`, lab `.env`) |
| Assigned SUT | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` (read in full this slot) |
| Supporting files (claims 3–5 hop) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CopyRosterEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyGroupFilter.cs`, `ShadowCopyEngine.cs`, `EfDashboardQueries.cs`, `DealIngestionService.cs`, `apps/api/Program.cs`, `EnvFile.cs`, `data/demo_copy_ledger.json` |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Live `35=D` sent this slot | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None.** Quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73) and public dest/source ids `5328266` / `1369850` / `305750` / `237339770`. No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live GET this pass | `GET http://127.0.0.1:5000/api/health` and `http://127.0.0.1:18720/api/health` **blocked** (loopback SSRF). Runtime flag **not** live-proven. File proof is enough to score claim 3. |
| Binding rule | **FAIL any assigned claim that cannot be proven from a live file or a live GET.** Sibling swarm books are not evidence. This slot re-read the four SUT files and the hop files. |

**Honesty:** Wanting dest profit is not an edge. Copy-all of the catalog would copy `RISK_BLOCKED` source losses. `SAFE_BY_ABSENCE` on live `1369850` is **not** “`RealCopyEnabled` stays false.” An armed env bit with no live sender is still an armed bit. Demo dest send is dest **exposure**, not dest-profit accounting. `DestinationRealPnl` constructor `0` is not a mark-to-market of dest `5328266`.

---

## 0. Verdict (binding)

**FAIL.** Claim 2 is file-proven. Claim 5 is file-proven as paper/source ≠ dest profit. Claims **1** (as written, unscoped), **3**, and **4** (as written, unscoped) do **not** pass.

| # | Assigned claim | Measured | Class |
|---|---|---|---|
| 1 | no `35=D` builder | **PROVEN** on `CTraderFixSession.cs` (135/135, only `(35, "A")`). **DISPROVEN** product-wide: sibling `Build("D")` ×5, one hosted. | **FAIL** unscoped / **PASS_SESSION** |
| 2 | `CanPromoteToLive` is false | **PROVEN** (`=> false`; unused `current`) | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **DISPROVEN** — only assignment is DI bind of `.env=true`; logon host never writes the bit | **FAIL** |
| 4 | sending now cannot be the profit path | Session/persist hop cannot book dest profit (`AllowFixSend=false`; dest DTO constructor `0`). Hosted demo hopper **can send `35=D` now** and ledger dest is still open. Venue dest P&L **unproven** (no live GET). | **FAIL** unscoped / **PASS_NOT_BOOKED_DEST_PROFIT** |
| 5 | SHADOW on demo is not destination profit | **PROVEN** — source state + paper `SimulateEntry` + dest DTO literal `0`. Residual: SHADOW is AUTO_ADMIT floor (dest **exposure**, not dest profit). | **PASS** |

One-line:

```text
FAIL slot 68: CTraderFixSession 35=A only (no D builder); product Build("D")×5 hosted; CanPromoteToLive=>false; RealCopyEnabled NOT forced false after logon (.env true + DI bind + logon logs-only); send-now is not booked dest-profit (DTO 0) but demo dest hopper can 35=D now; SHADOW-on-demo is source/paper not dest PnL. Risk NONE on live 1369850. Live GET blocked.
```

Do **not** treat this FAIL as a license to send. Do **not** flip leftover `.env=true` into go-live. Operator should restore `REAL_COPY_EXECUTION_ENABLED=false` (this slot did **not** edit it).

---

## 1. Claim 1 — no `35=D` builder — FAIL unscoped / PASS_SESSION

### 1.1 Assigned file `CTraderFixSession.cs` (135/135)

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` read in full. The only outbound builder is `BuildLogon`. Tag 35 is hardcoded `"A"`:

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

Single `WriteAsync` (L49) of that logon. Then one `ReadAsync`. Socket disposed. Inbound `Extract(reply, "35")` (L55, L122–134) is reply parse, not a builder. Tokens in this file: `Build("D")` = 0, `(35, "D")` = 0, `NewOrderSingle` = 0.

Hosted caller is logon-only:

```48:58:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            _config["CTRADER_FIX_QUOTE_SENDER_SUB_ID"] ?? "QUOTE",
            _config["CTRADER_FIX_QUOTE_TARGET_SUB_ID"] ?? "QUOTE",
            username, password, stoppingToken);

        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            _config["CTRADER_FIX_TRADE_SENDER_SUB_ID"] ?? "TRADE",
            _config["CTRADER_FIX_TRADE_TARGET_SUB_ID"] ?? "TRADE",
            username, password, stoppingToken);
```

The other three assigned files (`BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`) contain **zero** FIX builders.

### 1.2 Unscoped product claim — FAIL

`rg Build\("D"\)` under product `*.cs` (this slot):

| File | Hits |
|---|---|
| `src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 | 1 hosted sender |
| `src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | 3 CLI |
| `src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` L93 | 1 CLI |

`CTraderFixCopyOpen.Build` is generic (`Build(string type, ...)`, L142–156) and L95 writes `Build("D", ...)`. Hosted `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566 calls `CTraderFixCopyOpen.SendAsync`. Product-wide “no `35=D` builder” is **false**.

`CTraderFixCopyOpen` refuses live dest (`host` not `demo-` **or** `sender` not `demo.` **or** `account == "1369850"`) at L37–41 and returns without writing `35=D`. That is a refuse, not absence of a builder.

**Score:** PASS only if scoped to `CTraderFixSession`. Assigned wording is unscoped → **FAIL**.

---

## 2. Claim 2 — `CanPromoteToLive` is false — PASS

`D:\Prop\src\Domain\Scoring\BaselineScorer.cs` read in full (212 lines).

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

The unused `current` argument cannot change the result. `FromBaseline` (L189–207) returns only `INSUFFICIENT_DATA`, `RISK_BLOCKED`, `SHADOW`, `WATCH`, `EARLY_SCORE`. It never returns `LIVE` or `LIVE_CANDIDATE`.

Ingest writes `CurrentState = score.SuggestedState` (`DealIngestionService.cs` L140). Product callers of `CanPromoteToLive` include the unit test (`BaselineScorerTests.cs` L26) asserting false after three disciplined winners go to `SHADOW`.

`TraderState.LIVE` exists on the enum (`TraderState.cs` L10) but this machine cannot emit it.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL

This is a hard FAIL. Independent file proof. Live GET was **not** used as PASS evidence (blocked).

### 3.1 Only assignment in product C#

`rg RealCopyEnabled\s*=` under `*.cs` / `*.tsx` = **one** hit:

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

No other writer. No post-logon `RealCopyEnabled = false`.

### 3.2 `.env` is `true` and is loaded

`D:\Prop\.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; value not a secret).

`apps/api/Program.cs` L10 `EnvFile.FindAndLoad()` then L13 `AddEnvironmentVariables()` then L15 `AddTraderIntelligence`. `EnvFile` candidates include `D:\Prop\.env` (`EnvFile.cs` L14).

### 3.3 Logon host does not re-pin

`CTraderFixLogonHostedService.ExecuteAsync` after both `TryLogonAsync` calls:

- Writes `Quote` / `Trade` `LoggedOn`, `Status`, `LastError`, `UpdatedAt` (L60–67).
- **Logs** `_runtime.RealCopyEnabled` (L68–70): `"FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}"`.
- Does **not** assign `RealCopyEnabled`.
- `PersistAsync` updates `FixSessionState` host/port/status/timestamps only (L101–107). No runtime flag.

There is no `RealCopyEnabled = false` anywhere after logon. The claim “forced false after logon” is the **opposite** of the file.

### 3.4 Unbound POCO default is not runtime

`CTraderFixOptions.RealCopyExecutionEnabled` defaults false (`CTraderFixOptions.cs` L35) and is **not** what DI binds. Citing that POCO default as “forced false after logon” would be a lie.

### 3.5 Live GET

Loopback GET blocked this slot (`web_fetch` SSRF on `127.0.0.1:5000` and `:18720`). That does **not** rescue claim 3: the hosted process, if started with this `.env`, constructs the singleton as `true` and never forces it false. `LiveCopyPage` will render `REAL_COPY armed = YES` when `status.realCopyArmed` is true (`LiveCopyPage.tsx` L13; `CopyGateStatus.RealCopyArmed` is `_runtime.RealCopyEnabled` at `CopyTradingService.cs` L64). `Program.cs` L55 / L76 expose the same bit on `/api/health` and `/api/settings`.

**Claim 3 is disproven from files. FAIL.**

---

## 4. Claim 4 — sending now cannot be the profit path — FAIL unscoped / PASS_NOT_BOOKED_DEST_PROFIT

Proven from assigned files + dest constructor + live refuse as **not booked dest profit**. **Not** proven as “no dest fill can exist now.” Unscoped “cannot be the profit path” **FAIL**s because the hosted demo hopper can send now and dest cash on `5328266` is unproven.

### 4.1 `CTraderFixSession` cannot send a ticket

Claim 1: only `35=A`. Logon is not a fill. Not a profit path.

### 4.2 Assigned `RiskEngine` never books dest profit

`D:\Prop\src\Domain\Risk\RiskEngine.cs` read in full (189 lines).

`Evaluate` sets `AllowFixSend` only when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Every `Reject` persists `AllowFixSend=false` (L180–188). When `RealExecutionEnabled==false`, L90–93 is an empty comment; `allowSend` is still AND-gated.

`CopyTradingService` persist hop:

- `VenueReconciled = false` (const, L20). Increasing intents hit `VENUE_NOT_RECONCILED` before approve.
- Persist **always** `AllowFixSend = false` (L324), even if `Evaluate` would have returned true.
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

`OverviewDto` ctor (`DashboardModels.cs` L5–22): after `ShadowPnl` the next three decimals are `DestinationRealPnl`, `XauGross`, `XauNet`. `DestinationRealPnl` is the first `0`. No dest realized-PnL aggregator exists. Constructor `0` is **not** a live dest mark.

### 4.4 Assigned `LiveCopyPage` cannot send

`D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` read in full (70/70). Hooks are GET only (`hooks.ts` L60–65: `/api/copy/status`, `/api/copy/intents`). No POST. No dest-PnL column. No send control. Blocker copy: “Pepperstone cannot be filled” (L24). Empty-state text admits dest auto-sends (L57) — that is dest **exposure**, not dest-profit accounting.

### 4.5 Residual that FAILS the unscoped claim — hosted demo send-now

`CopyTradingHostedService` 20s tick (L28–30) calls `TickRosterAsync` → `GenerateShadowIntentsAsync` → `ExecuteDemoCopyAsync`.

`ExecuteDemoCopyAsync`:

- Returns 0 if `!DemoDest` (host not `demo-*` **or** sender not `demo.*` **or** account `== 1369850`) (L45–48, L485–488).
- **Bypasses** `RiskEngine.Evaluate`.
- Closes then opens via `CTraderFixCopyOpen.SendAsync` (L528, L566) → `Build("D")`.
- Gates: roster `ADMITTED`, open XAU, `MaxAutoLots=0.05`.

`D:\Prop\data\demo_copy_ledger.json` (this slot): one open dest fill — source `305750` / pos `21250421` / dest `237339770` / 0.01 lots / `DestFillPrice=4390.2` / `DestClosed=false`. Hardcoded seed in `ExecuteDemoCopyAsync` L500–511 re-inserts that same open row if missing.

That is dest **exposure**. It is **not** `DestinationRealPnl`. It is **not** a measured dest edge. Absolute “demo dest EV cannot be positive” is **unproven** (no live GET of dest account). Therefore the unscoped claim “sending now cannot be the profit path” **FAIL**s: a send-now hop exists; dest cash is unproven; constructor `0` is not a mark.

Live dest identity `1369850` is still refused (`CTraderFixCopyOpen` L37–41). This slot did not send.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — PASS

### 5.1 SHADOW is a source scoring state

`TraderStateMachine.FromBaseline` emits `SHADOW` when `quality >= 70 && risk < 40` after early eligibility (L200–201). `FeatureSnapshot.NetPnl` is **source** reconstructed XAU (`BaselineScorer.ComputeFeatures` L66, L111). Three disciplined winners go to `SHADOW`, not `LIVE` (unit test L21–26).

### 5.2 Paper shadow is not dest

Hopper `GenerateShadowIntentsAsync` for `{SHADOW, LIVE_CANDIDATE, LIVE}` writes `Status="SHADOW_ONLY"` and `ShadowCopyEngine.SimulateEntry` (`CopyTradingService.cs` L336–359). `SimulateEntry` (`ShadowCopyEngine.cs` L35–61) marks a synthetic fill from a quote. Dashboard `shadowPnl` is `Sum(SourceVsShadowSlippage)` (`EfDashboardQueries.cs` L29) — slippage vs source, not dest realized PnL.

### 5.3 Dest profit is a different column and is hard-zero

`DestinationRealPnl` constructor `0` (claim 4.3). `LiveCopyPage` shows `SHADOW traders` as a count of `TraderState.SHADOW` (`CopyTradingService.GetStatusAsync` L59), next to `Live sends` = count of `ExecutionIntents` with `SentAt != null` (L57) — not dest PnL.

### 5.4 Policy / roster do not turn SHADOW into dest profit

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `WATCH` / `EARLY_SCORE` / `INSUFFICIENT_DATA` as `TRADER_NOT_SHADOW_YET` (L81–85). Also requires `CompletedXauTrades >= 20`, `XauNetPnl > 0`, and `CopyGroupFilter.IsDemoOrContest` (L93–109). `CopyRosterEngine` AUTO_ADMITs when eligible (L72–80) and **requires** demo/contest (L52–53). A SHADOW source on a demo group can be `ADMITTED` and then demo-copied (claim 4 residual). The dest fill, if any, is dest **exposure**. The SHADOW badge and the paper shadow book remain source/paper. They are **not** destination profit.

Claim 5 is **not** “SHADOW cannot open dest exposure.” That stronger claim would FAIL. The assigned claim is “not destination profit.” Proven.

---

## 6. `LiveCopyPage.tsx` (70/70) — assigned file, not a send path

Read in full. GETs `/api/copy/status` and `/api/copy/intents` only. No POST. Renders `realCopyArmed`, SHADOW/LIVE counts, `liveSends`, `shadowFills`, QUOTE/TRADE, blockers, intent table. Cannot be the profit path. Cannot force `RealCopyEnabled` false. Does not implement `35=D`. Does not implement `CanPromoteToLive`. Empty-state L57 is honest about demo dest auto-send after `ADMITTED` — that text is UI, not a sender.

---

## 7. Risk to capital

| Book | Risk |
|---|---|
| Live Pepperstone `1369850` | **NONE** this slot — `CTraderFixSession` is `35=A` only; `CopyOpen` refuses that account; persist `AllowFixSend=false`; `CanPromoteToLive=>false`. `SAFE_BY_ABSENCE`. |
| Demo dest (lab `DemoDest` + 20s `ExecuteDemoCopyAsync`, public account `5328266`) | **Not absent.** File-proven hop can emit `35=D`. Ledger dest `237339770` still open. Not dest-profit accounting (`DestinationRealPnl=0`). Venue dest P&L **unproven** (no live GET). |
| This slot | No attach. No send. No `.env` edit. |

---

## 8. Stale pins this slot refuses to repeat

| Pin | Status |
|---|---|
| `NewOrderSingleImplemented = const false` | **STALE** — HEAD is `=> DemoDest` (`CopyTradingService.cs` L50). |
| Product / tree `35=D=0` | **STALE** — `CTraderFixCopyOpen.Build("D")` + hosted caller. Assigned `CTraderFixSession` is still `35=A` only. |
| `RealCopyEnabled` forced false after logon / “stays false” | **FALSE** — claim 3. |
| `CTraderFixOptions.RealCopyExecutionEnabled` default false as runtime | **UNBOUND** — DI reads env key, not that POCO. |
| Live GET dest PnL / armed bit | **UNPROVEN this slot** (SSRF). Not used as PASS evidence. |
| `DestinationRealPnl=0` as dest cash proof | **CONSTRUCTOR**, not a mark. |

---

## 9. What this slot did not do

- Did not edit product, tests, or `.env`.
- Did not send `35=D`.
- Did not print secrets.
- Did not live-attach the API (SSRF block).
- Did not claim EX5/MT5 decompile work (wrong tree).

End of P500_VERIFY_68. Product source was not modified. No secrets printed. This slot did not send `35=D`.
