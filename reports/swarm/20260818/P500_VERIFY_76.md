# P500_VERIFY_76 — Adversarial four-file verify (slot 76)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_76.md` |
| Slot | **76** |
| Agent | P500_VERIFY_76 (adversarial verifier; independent HEAD re-read) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Topic | Confirm five claims from assigned files. **FAIL any claim not proven from a file or live GET.** |
| Assigned SUT (read in full) | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135), `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212), `D:\Prop\src\Domain\Risk\RiskEngine.cs` (189/189), `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (70/70) |
| Adjacent (needed to prove/disprove 1, 3–5) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `CTraderFixOptions.cs`, `ShadowCopyEngine.cs`, `CopyRosterEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyGroupFilter.cs`, `CopyLifecycle.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `DemoCopyLedger.cs`, `DealIngestionService.cs`, `EfTradingStore.cs`, `apps/api/Program.cs`, `apps/web/src/api/hooks.ts`, `data/demo_copy_ledger.json`, `.env` **booleans / public dest ids only** |
| Product source modified | **No.** Report + INDEX / SWARM_LOG pins only. |
| Test source modified | **No.** |
| Config / `.env` edited | **No.** `REAL_COPY` was not flipped. |
| Live `35=D` / NewOrderSingle this slot | **Not sent. Not constructed.** |
| Secrets printed | **None.** Quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73), `FEATURE_COPY_TRADING_ENABLED=true` (`.env` L106), public dest id `5328266`, live refuse id `1369850`. No password / FIX `554=` / token / connection string. |
| Localhost API this slot | **Attempted, blocked.** `GET http://127.0.0.1:5000/api/health` → tool `SSRF blocked`. `GET http://localhost:5000/api/copy/status` → fetch failed. Runtime `realCopyEnabled` **not** live-proven. File-only for claim 3. |

**Honesty rule:** Chat, sibling P500_VERIFY slots, and comments such as “NewOrderSingle still unimplemented” are **not** evidence. This slot re-read HEAD files. `VenueReconciled = const false` on the *risk persist hop* is not absence of the *demo dest hop*. `CanPromoteToLive => false` does not stop `ExecuteDemoCopyAsync`. Dashboard `DestinationRealPnl = 0` is a constructor, not a measured dest book. `CTraderFixOptions.RealCopyExecutionEnabled` default `false` is **unbound** to `LiveRuntimeStatus`.

```text
CTraderFixSession outbound is 35=A only (BuildLogon).
Product 35=D builders exist (CopyOpen L95 + DemoTestTrade x3 + DemoMatrix L93).
CanPromoteToLive => false.
RealCopyEnabled is NOT forced false after logon (DI binds env; .env L73 true; logon logs-only).
Hosted 20s tick can 35=D on demo dest; live 1369850 refused.
SHADOW ledger is SimulateEntry, not dest P&L.
Live GET this slot = SSRF blocked.
```

---

## 0. Verdict (binding)

**FAIL.** Two of five assigned claims are file-proven as stated. Three fail the FAIL-if-unproven / FAIL-if-disproven rule.

| # | Assigned claim | Measured | Class |
|---|---|---|---|
| 1 | no `35=D` builder | **PROVEN** on `CTraderFixSession.cs` (135/135, only `(35, "A")`). **DISPROVEN** as an unscoped product claim (`CTraderFixCopyOpen.Build("D")` is hosted). | **FAIL_UNSCOPED** / **PASS_SESSION** |
| 2 | `CanPromoteToLive` is false | **PROVEN** (`=> false`). `FromBaseline` never emits `LIVE` / `LIVE_CANDIDATE`. | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **DISPROVEN** — sole assignment is DI bind of `.env=true`; logon host never writes the bit. | **FAIL** |
| 4 | sending now cannot be the profit path | **NOT PROVEN.** Hosted demo hop can `35=D` now. Dest DTO `0` is a constructor. Ledger dest `237339770` is open. Live GET of dest cash blocked. | **FAIL** |
| 5 | SHADOW on demo is not destination profit | **PROVEN** for the paper SHADOW book (`SimulateEntry` / slippage sum). Residual: SHADOW is dest `AUTO_ADMIT` floor. | **PASS** |

One-line:

```text
FAIL slot 76: session 35=A only but product has hosted Build("D"); CanPromoteToLive=>false; RealCopyEnabled NOT forced false after logon (.env true + DI bind + logon logs-only); send-now CAN be demo dest exposure/P&L (20s hopper + open ledger); SHADOW paper ≠ dest PnL. Live GET blocked. Live 1369850 NONE. Demo dest not absent.
```

Do **not** treat this FAIL as a license to send. Do **not** flip leftover `.env=true` into go-live. Operator should restore `REAL_COPY_EXECUTION_ENABLED=false` (this slot did **not** edit it).

---

## 1. Claim 1 — no `35=D` builder — FAIL_UNSCOPED / PASS_SESSION

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

Single `WriteAsync` (L49) of that logon. Then one `ReadAsync`. Socket disposed on return. Inbound `Extract(reply, "35")` is reply parse, not a builder. Tokens in this file: `Build("D")` = 0, `(35, "D")` = 0, `NewOrderSingle` = 0.

Hosted caller is logon-only:

```48:58:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            ...);
        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            ...);
```

**Unscoped claim fails.** Same folder + hosted hop assemble and send `35=D`. `rg Build\("D"\) --glob *.cs` this slot = **5** product hits:

| File | Line | Role |
|---|---|---|
| `CTraderFixCopyOpen.cs` | 95 | Hosted. `CopyTradingService.ExecuteDemoCopyAsync` L528 (close) and L566 (open). |
| `CTraderFixDemoTestTrade.cs` | 139, 163, 197 | CLI `tools/DemoFixTestTrade`. Not in API DI. |
| `CTraderFixDemoMatrix.cs` | 93 | Demo matrix helper. |

Generic assembler:

```142:156:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
    private static string Build(string type, string sender, string target, int seq, params (int, string)[] extra)
    {
        var fields = new List<(int, string)>
        {
            (35, type), (34, seq.ToString(CultureInfo.InvariantCulture)),
            ...
        };
```

L95 writes `Build("D", sender, target, seq, extra.ToArray())`. Live dest identity is refused (`host` not `demo-` **or** `sender` not `demo.` **or** `account == "1369850"`) at L37–41. That refuse is **not** “no builder.”

Unqualified “no `35=D` builder” is therefore **false**. Assigned-file “`CTraderFixSession` has no `35=D` builder” is **true**. Under FAIL-any-unproven, the assigned wording is scored **FAIL_UNSCOPED**.

---

## 2. Claim 2 — `CanPromoteToLive` is false — PASS

`D:\Prop\src\Domain\Scoring\BaselineScorer.cs` read in full (212 lines).

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

The unused `current` argument cannot change the result. `FromBaseline` (L189–207) returns only `INSUFFICIENT_DATA`, `RISK_BLOCKED`, `SHADOW`, `WATCH`, `EARLY_SCORE`. It never returns `LIVE` or `LIVE_CANDIDATE`.

`rg CanPromoteToLive --glob *.{cs,tsx}` this slot = **one** production definition (`BaselineScorer.cs` L211) + unit lock (`BaselineScorerTests.cs` L26) asserting false after three disciplined winners go to `SHADOW`.

Ingest writes `CurrentState = score.SuggestedState` (`DealIngestionService.cs` L140). No other writer promotes to `LIVE`.

`appsettings.json` `FeatureFlags.AutoPromotionEnabled=false` is unused config, not a second promotion gate. Promotion is closed in the state machine, not by that flag.

**Residual (does not fail claim 2):** `CanPromoteToLive => false` does **not** stop dest demo send. `CopyRosterEngine.Decide` admits via `XauUsdOneToOneCopyPolicy.IsTraderEligible` (SHADOW / LIVE_CANDIDATE / LIVE + 20 completed XAU + `XauNetPnl > 0` + demo/contest group). `ExecuteDemoCopyAsync` then keys off `Status == "ADMITTED"`, not `LIVE`.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL (disproven)

This is a hard FAIL. Independent file proof:

### 3.1 Only assignment in product C#

`rg RealCopyEnabled\s*=` under `*.cs` / `*.tsx` = **one** hit:

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

### 3.2 `.env` is `true` and is loaded

`D:\Prop\.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only). L106: `FEATURE_COPY_TRADING_ENABLED=true` (unused by DI for the runtime bit; API FEATURE flag is a literal `true` at `Program.cs` L77).

`apps/api/Program.cs` L10 `EnvFile.FindAndLoad()` then L13 `AddEnvironmentVariables()` then L15 `AddTraderIntelligence`. `EnvFile` candidates include `D:\Prop\.env` (`EnvFile.cs` L14).

### 3.3 Logon host does not re-pin

`CTraderFixLogonHostedService.ExecuteAsync` after both `TryLogonAsync` calls:

- Writes `Quote` / `Trade` `LoggedOn`, `Status`, `LastError`, `UpdatedAt` (L60–67).
- **Logs** `_runtime.RealCopyEnabled` (L68–70). Does **not** assign it.
- `PersistAsync` updates `FixSessionState` host/port/status/timestamps only (L101–107). No runtime flag.

There is no `RealCopyEnabled = false` anywhere after logon. The claim “forced false after logon” is the opposite of the file.

### 3.4 Unbound POCO default is not the runtime bit

`CTraderFixOptions.RealCopyExecutionEnabled` defaults false (`CTraderFixOptions.cs` L35) and is **not** what DI binds. `AddTraderIntelligence` never `Configure<CTraderFixOptions>`. Citing that POCO default as “forced false after logon” would be a lie.

FIX worker reads a **different** key (`CTrader:RealCopyExecutionEnabled`, default false, `Worker.cs` L21) and still does not send. That is not a re-pin of `LiveRuntimeStatus.RealCopyEnabled`.

### 3.5 Live GET

Loopback GET blocked this slot (`127.0.0.1:5000/api/health` SSRF; `localhost:5000/api/copy/status` fetch failed). That does **not** rescue claim 3: the hosted process, if started with this `.env`, constructs the singleton as `true` and never forces it false. `LiveCopyPage` will render `REAL_COPY armed = YES` when `status.realCopyArmed` is true (`LiveCopyPage.tsx` L13; `CopyGateStatus.RealCopyArmed` is `_runtime.RealCopyEnabled` at `CopyTradingService.cs` L64).

`/api/health` exposes `realCopyEnabled = runtime.RealCopyEnabled` (`Program.cs` L55). `/api/settings` exposes the same bit under `featureFlags.REAL_COPY_EXECUTION_ENABLED` (L76). File path is enough to FAIL.

---

## 4. Claim 4 — sending now cannot be the profit path — FAIL

The assigned files cannot *send*. That is not the whole claim. “Sending now cannot be the profit path” is an unscoped product claim. FAIL-if-unproven: dest send exists; dest cash is unmeasured.

### 4.1 What the assigned files prove (not enough)

`CTraderFixSession` cannot send a ticket (claim 1 session-scoped). Logon is not a fill.

`RiskEngine.Evaluate` sets `AllowFixSend` only when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Every `Reject` persists `AllowFixSend=false` (L180–188). When `RealExecutionEnabled==false`, the comment at L90–93 says the shadow path never allows FIX send; `allowSend` is still AND-gated. Unit lock: `Real_flag_false_never_allows_fix_send` (`RiskEngineTests.cs` L21–26).

`LiveCopyPage.tsx` is GET-only (`useCopyStatus` / `useCopyIntents` → `/api/copy/status`, `/api/copy/intents`; `hooks.ts` L60–65). No POST. No send button. Empty-state text **admits** dest send: “Demo dest auto-sends after a trader is ADMITTED…” (L57).

### 4.2 Risk persist hop cannot approve a live send (dead branch)

`CopyTradingService.GenerateShadowIntentsAsync`:

- Passes `Reconciled = VenueReconciled` and `VenueReconciled = const false` (L20, L304). Increasing intents hit `VENUE_NOT_RECONCILED` before approve.
- Persist **always** `AllowFixSend = false` (L324) even if `Evaluate` would have set true.
- Live branch L330 requires `decision.AllowFixSend && CurrentState==LIVE && NewOrderSingleImplemented && VenueReconciled`. `CanPromoteToLive=>false` plus `FromBaseline` never emitting `LIVE` plus const `VenueReconciled=false` means that branch is dead unless someone hand-writes `LIVE` **and** flips the const.

`NewOrderSingleImplemented => DemoDest` (L50). Reports that still say `NOS=const false` are **STALE**.

`GetStatusAsync` L67 reports `VenueReconciled: DemoDest` — a **different** boolean than the const used in `Evaluate`. Status DTO can say venue reconciled while the risk hop still uses `false`.

### 4.3 Hosted dest hop **can** send now

`CopyTradingHostedService` 20s tick (`L28–30`) calls `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**.

`ExecuteDemoCopyAsync` (L483–605):

- Gates on `DemoDest` only (host `demo-*` AND sender `demo.*` AND account ≠ `1369850`). **Does not read** `RealCopyEnabled`. **Does not call** `RiskEngine.Evaluate`.
- L528 / L566 call `CTraderFixCopyOpen.SendAsync` → `Build("D")`.
- Caps `MaxAutoLots = 0.05m`, `maxPerTick = 5`.
- Hard-seeds ledger row `305750` / `21250421` / dest `237339770` / `DestFillPrice=4390.2` if missing (L500–512).

On-disk ledger `D:\Prop\data\demo_copy_ledger.json` this slot: one open dest fill, `DestClosed=false`, dest pos `237339770`, 0.01 lots, fill `4390.2`. That is dest **exposure**. Whether it is dest **profit** is unmeasured (no dest realized-PnL aggregator; live GET blocked).

### 4.4 Product dest-profit *book* is a literal zero — not a proof dest has no P&L

```43:46:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            shadowPnl,
            0,
            0,
            0,
```

`OverviewDto` field order (`DashboardModels.cs` L16–18): `ShadowPnl`, **`DestinationRealPnl`**, `XauGross`, `XauNet`. The second `0` is dest PnL. `GetRiskAsync` also constructs `DailyPnl/Drawdown/Xau*` as `0` (L208). `GetFixSessions` hardcodes `ExecutionEnabled` false (L195).

A constructor `0` is **not** a measured dest book. It cannot prove “sending cannot be the profit path.” It can only prove the dashboard **does not book** dest P&L.

### 4.5 Live dest identity is refused (different claim)

`CTraderFixCopyOpen.SendAsync` L37–41 returns without writing `35=D` when `account == "1369850"` (or non-demo host/sender). Logon default account in the hosted service is `"5328266"` (`CTraderFixLogonHostedService.cs` L41) — the **demo** id, not the live refuse id.

So: live Pepperstone send is refused. Demo dest send is **wired**. The assigned claim did not say “live 1369850.” FAIL.

This slot did not send. Absolute “demo dest EV cannot be positive” is **unproven**. That does not convert send-now into a booked dest-profit column. It also does not let the claim PASS.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — PASS

### 5.1 SHADOW is a source scoring state

`TraderStateMachine.FromBaseline` emits `SHADOW` when `quality >= 70 && risk < 40` after early eligibility (L200–201). `FeatureSnapshot.NetPnl` is **source** reconstructed XAU (`BaselineScorer.ComputeFeatures` L66, L111).

### 5.2 Paper shadow is not dest

Hopper `GenerateShadowIntentsAsync` for `{SHADOW, LIVE_CANDIDATE, LIVE}` writes `Status="SHADOW_ONLY"` and `ShadowCopyEngine.SimulateEntry` (L336–359). `SimulateEntry` marks a synthetic fill from a quote (`ShadowCopyEngine.cs` L35–61). Dashboard `shadowPnl` is `Sum(SourceVsShadowSlippage)` (`EfDashboardQueries.cs` L29) — slippage vs source, not dest realized PnL.

`PersistDemoShadowAsync` also writes `SHADOW_ONLY` + `SimulateEntry` and early-returns unless `state == SHADOW` (`EfTradingStore.cs` L267–312).

### 5.3 Dest profit is a different column

`DestinationRealPnl` constructor `0` (claim 4.4). `LiveCopyPage` shows `SHADOW traders` as a count of `TraderState.SHADOW` (`CopyTradingService.GetStatusAsync` L59), next to `Live sends` = count of `ExecutionIntents` with `SentAt != null` (L57) — not dest PnL. No dest-PnL column on the page.

### 5.4 Residual — SHADOW is dest AUTO_ADMIT floor, not dest cash

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `WATCH` / `EARLY_SCORE` / `INSUFFICIENT_DATA` as `TRADER_NOT_SHADOW_YET` (L81–85). `CopyGroupFilter` **requires** demo/contest groups for admit. A SHADOW source on a demo group with 20 completed XAU and `XauNetPnl > 0` can be `ADMITTED` (`CopyRosterEngine` L72–80) and then demo-copied (claim 4). The dest fill, if any, is dest exposure. The SHADOW badge and the paper shadow book remain source/paper. They are **not** destination profit.

This residual does **not** fail claim 5 as written. It would fail a stronger claim “SHADOW cannot cause dest send.”

---

## 6. `LiveCopyPage.tsx` (70/70) — assigned file, not a send path

Read in full. GETs `/api/copy/status` and `/api/copy/intents` only. Renders `realCopyArmed`, SHADOW/LIVE counts, `liveSends`, `shadowFills`, QUOTE/TRADE, blockers (“Pepperstone cannot be filled”, L24), intent table. Cannot be the profit path. Cannot force `RealCopyEnabled` false. Does not implement `35=D`. Does not implement `CanPromoteToLive`. Empty-state L57 is an honesty leak that dest auto-send exists.

---

## 7. Risk to capital

| Book | Risk |
|---|---|
| Live Pepperstone `1369850` | **NONE** this slot — `CTraderFixSession` is `35=A` only; `CopyOpen` refuses that account; persist `AllowFixSend=false`; `CanPromoteToLive=>false`. |
| Demo dest (lab `DemoDest` + 20s `ExecuteDemoCopyAsync`, public id `5328266`) | **Not absent.** File-proven hop can emit `35=D`. Existing ledger dest `237339770` still open. Dest-profit **accounting** is constructor `0`. Dest-profit **cash** unmeasured (no live GET). |
| This slot | No attach. No send. No `.env` edit. |

---

## 8. Stale pins this slot refuses to repeat

| Pin | Status |
|---|---|
| `NewOrderSingleImplemented = const false` | **STALE** — HEAD is `=> DemoDest` (`CopyTradingService.cs` L50). |
| Product / tree `35=D=0` | **STALE** — `CTraderFixCopyOpen.Build("D")` + hosted caller + two CLI helpers. Assigned `CTraderFixSession` is still `35=A` only. |
| `RealCopyEnabled` forced false after logon / W500 “stays false” | **FALSE** — claim 3. |
| `CTraderFixOptions.RealCopyExecutionEnabled` default false as runtime | **UNBOUND** — DI reads env key, not that POCO. |
| `LiveRuntimeStatus.Snapshot` “NewOrderSingle still unimplemented” when armed | **STALE comment** (`LiveRuntimeStatus.cs` L43). Hopper implements dest `35=D` on `DemoDest`. |
| `SAFE_BY_ABSENCE` on demo dest | **FALSE** — hosted 20s `ExecuteDemoCopyAsync`. |
| Live GET dest PnL / armed bit | **UNPROVEN this slot** (SSRF). Not used as PASS evidence. |

---

## 9. What would flip this FAIL to PASS

1. Unscoped “no `35=D` builder”: delete or un-host `CTraderFixCopyOpen.Build("D")` (and the two CLI helpers), or rephrase the claim to `CTraderFixSession` only.
2. Claim 3: after successful logon, assign `_runtime.RealCopyEnabled = false` **or** stop binding `.env=true` (restore `REAL_COPY_EXECUTION_ENABLED=false`) **and** prove the runtime bit via live GET.
3. Claim 4: stop `ExecuteDemoCopyAsync` / un-host dest `35=D`, **or** prove with a live dest-account GET that dest realized PnL is zero **and** that no dest ticket can fill. Constructor `0` is not that proof.

Until then: **FAIL**. Live `1369850` remains **NONE**. Demo dest remains a send path.
