# P500_VERIFY_56 — Adversarial verifier (slot 56)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_56.md` |
| Agent / slot | P500 adversarial verifier **56** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (live `src/`, `apps/`, lab `.env`) |
| Assigned SUT (read in full this slot) | `CTraderFixSession.cs` (135/135), `BaselineScorer.cs` (212/212), `RiskEngine.cs` (189/189), `LiveCopyPage.tsx` (70/70) |
| Supporting hop files (claims 1/3/4/5 only) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `CopyGroupFilter.cs`, `EfDashboardQueries.cs`, `DealIngestionService.cs`, `EfTradingStore.cs`, `EnvFile.cs`, `apps/api/Program.cs`, `apps/web/src/api/hooks.ts`, `CTraderFixOptions.cs` |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Live `35=D` sent this slot | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None.** Quoted only booleans `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73) and `FEATURE_COPY_TRADING_ENABLED=true` (`.env` L106). Public dest ids `5328266` / `1369850`. No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live GET this pass | `GET http://127.0.0.1:5000/api/health`, `/api/settings`, `/api/copy/status` **blocked** (loopback SSRF). Runtime bits **not** live-proven. File proof is enough to score claim 3. |
| Binding rule | **FAIL any assigned claim that cannot be proven from a live file or a live GET.** Sibling swarm books are not evidence. |

**Honesty:** Wanting dest profit is not an edge. Copy-all of the catalog would copy `RISK_BLOCKED` source losses. `SAFE_BY_ABSENCE` on live `1369850` is **not** “`RealCopyEnabled` stays false.” An armed env bit with no live sender is still an armed bit. A demo dest `35=D` is dest **exposure**, not booked dest profit. SHADOW is a source state / paper book.

---

## 0. Verdict (binding)

**FAIL.** Claim 3 is **disproven** on disk. Unscoped claim 1 is **disproven** by a hosted sibling builder. Unscoped claim 4 is **unproven** (demo dest hopper can send now; dest-account P&L was not live-GET). Claims 2 and 5 are file-proven.

| # | Assigned claim | Measured | Class |
|---|---|---|---|
| 1 | no `35=D` builder | **DISPROVEN unscoped** — product `Build("D")` ×5; hosted `CTraderFixCopyOpen` L95. **PROVEN** only on assigned `CTraderFixSession.cs` (135/135 is `35=A` only). | **FAIL** (unscoped) / **PASS_SESSION** |
| 2 | `CanPromoteToLive` is false | **PROVEN** (`TraderStateMachine.CanPromoteToLive => false`) | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **DISPROVEN** — only C# assignment is DI bind of `.env=true`; logon host never writes the bit | **FAIL** |
| 4 | sending now cannot be the profit path | **UNPROVEN unscoped.** Live `1369850` cannot send. Product `DestinationRealPnl` is constructor `0`. Demo dest 20s hop **can** `35=D` now (ledger dest still open). Dest-account P&L not live-GET. | **FAIL** |
| 5 | SHADOW on demo is not destination profit | **PROVEN** — source state + paper `SimulateEntry`; dest DTO is literal `0`. Residual: SHADOW is the dest ADMIT floor (exposure, not profit). | **PASS** |

One-line:

```text
FAIL slot 56: CTraderFixSession 35=A only; product hosted Build("D") exists; CanPromoteToLive=>false; RealCopyEnabled NOT forced false after logon (.env true + DI bind + logon logs-only); send-now is not booked dest-profit (DTO 0; live 1369850 refused) but demo dest hop can 35=D; SHADOW-on-demo is source/paper not dest PnL. Risk NONE on live 1369850. Live GET blocked.
```

Do **not** treat this FAIL as a license to send. Do **not** flip leftover `.env=true` into go-live. Operator should restore `REAL_COPY_EXECUTION_ENABLED=false` (this slot did **not** edit it).

---

## 1. Claim 1 — no `35=D` builder — FAIL unscoped / PASS_SESSION

### 1.1 Assigned file — `CTraderFixSession` has no D builder

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

Single `WriteAsync` (L49) of that logon. Then one `ReadAsync` (L53). Socket disposed in `using`. Inbound `Extract(reply, "35")` (L55, L73) is reply parse, not a builder. Tokens in this file: `Build("D")` = 0, `(35, "D")` = 0, `NewOrderSingle` = 0.

Hosted caller of this type is logon-only:

```48:58:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            ...);
        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            ...);
```

Scoped to the assigned session file, “no `35=D` builder” is **true**.

### 1.2 Product-wide the claim is false

The assigned wording is “no `35=D` builder,” not “`CTraderFixSession` has no `35=D` builder.” Unscoped, the tree has five `Build("D")` writes:

| File | Line | Hosted? |
|---|---|---|
| `src/Fix.CTrader/Sessions/CTraderFixCopyOpen.cs` | L95 `Build("D", ...)` | **Yes** — `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566 |
| `src/Fix.CTrader/Sessions/CTraderFixDemoMatrix.cs` | L93 | CLI / matrix helper |
| `src/Fix.CTrader/Sessions/CTraderFixDemoTestTrade.cs` | L139, L163, L197 | `tools/DemoFixTestTrade` |

Generic builder on the hosted sibling:

```142:156:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
    private static string Build(string type, string sender, string target, int seq, params (int, string)[] extra)
    {
        var fields = new List<(int, string)>
        {
            (35, type), (34, seq.ToString(CultureInfo.InvariantCulture)),
            ...
        };
        fields.AddRange(extra);
        ...
    }
```

Called as `Build("D", sender, target, seq, extra.ToArray())` at L95 after a demo-only gate (L37–41: refuse unless host starts `demo-` **and** sender starts `demo.` **and** `account != "1369850"`).

`CopyTradingHostedService` L30 calls `ExecuteDemoCopyAsync` every 20s after an 8s start delay. That is a **hosted** `35=D` builder. Product-wide “no `35=D` builder” is therefore **FAIL**. Pins that still say tree `35=D=0` or “single FIX writer” are **STALE**.

---

## 2. Claim 2 — `CanPromoteToLive` is false — PASS

`D:\Prop\src\Domain\Scoring\BaselineScorer.cs` read in full (212 lines).

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

The unused `current` argument cannot change the result. `FromBaseline` (L189–207) returns only `INSUFFICIENT_DATA`, `RISK_BLOCKED`, `SHADOW`, `WATCH`, `EARLY_SCORE`. It never returns `LIVE` or `LIVE_CANDIDATE`.

Ingest writes `CurrentState = score.SuggestedState` (`DealIngestionService.cs` L140). Store upsert copies that field (`EfTradingStore.cs` L232). Product callers of `CanPromoteToLive` are the unit test (`BaselineScorerTests.cs` L26) asserting false after three disciplined winners go to `SHADOW`.

`LiveCopyPage.tsx` does not implement promotion. Blocker copy in `CopyTradingService.BuildBlockers` L615: `"0 traders in LIVE (promotion is manual; trade #3 cannot auto-LIVE)"` when not `DemoDest`.

No live GET is required: the method is a constant.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL

This is a hard FAIL. The files show the opposite of a post-logon force-false.

### 3.1 Only assignment in product C# / TSX

`grep RealCopyEnabled\s*=` under `*.cs` / `*.tsx` = **one** hit:

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

`LiveRuntimeStatus.RealCopyEnabled` is a public `{ get; set; }` (`LiveRuntimeStatus.cs` L32). Nothing after construction assigns `false`.

### 3.2 `.env` is `true` and is loaded

`D:\Prop\.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only). L106: `FEATURE_COPY_TRADING_ENABLED=true` (boolean only).

`apps/api/Program.cs` L10 `EnvFile.FindAndLoad()` then L13 `AddEnvironmentVariables()` then L15 `AddTraderIntelligence`. `EnvFile` candidates include the literal `D:\Prop\.env` (`EnvFile.cs` L14).

### 3.3 Logon host does not re-pin

`CTraderFixLogonHostedService.ExecuteAsync` after both `TryLogonAsync` calls:

- Writes `Quote` / `Trade` `LoggedOn`, `Status`, `LastError`, `UpdatedAt` (L60–67).
- **Logs** `_runtime.RealCopyEnabled` (L68–70: `"RealCopyArmed={Armed} NewOrderSingle still unimplemented"`). Does **not** assign it.
- `PersistAsync` updates `FixSessionState` host/port/status/timestamps only (L101–107). No runtime flag.

There is no `RealCopyEnabled = false` anywhere after logon. The claim “forced false after logon” is the opposite of the file.

### 3.4 Assigned files do not force the bit either

- `CTraderFixSession` never mentions `RealCopyEnabled`.
- `BaselineScorer` never mentions it.
- `RiskEngine` L90–93 is an **empty** `if (request.RealExecutionEnabled == false && …)` with a comment. It does not mutate runtime. Later `allowSend` ANDs the request flag (L147–150); that is per-request, not a post-logon force-false.
- `LiveCopyPage.tsx` L13 renders `status?.realCopyArmed ? 'YES' : 'NO'`. Display only. `useCopyStatus` is GET `/api/copy/status` (`hooks.ts` L60–61). No POST. Cannot force false.

`CopyTradingService.GetStatusAsync` L64: `RealCopyArmed: _runtime.RealCopyEnabled`. `/api/health` L55 and `/api/settings` L76 echo the same singleton. `GetRiskAsync` L208 echoes it again.

### 3.5 Unbound POCO default is not a re-pin

`CTraderFixOptions.RealCopyExecutionEnabled` defaults `false` (`CTraderFixOptions.cs` L35). DI does **not** bind that POCO for the API runtime flag. `apps/fix-worker/Worker.cs` L21 reads `CTrader:RealCopyExecutionEnabled` (default false) — a **different** key from `REAL_COPY_EXECUTION_ENABLED`. Citing the POCO default as “forced false after logon” would be a lie.

### 3.6 Live GET

Loopback GET blocked this slot. That does **not** rescue claim 3: a process started with this `.env` constructs the singleton as `true` and never forces it false.

---

## 4. Claim 4 — sending now cannot be the profit path — FAIL (unscoped)

The assigned claim is unscoped. I can prove the **live** hop is not a dest-profit path. I **cannot** prove sending now cannot be dest P&L.

### 4.1 What is proven (live hop / booked dest profit)

**`CTraderFixSession` cannot send a ticket.** Claim 1.1: only `35=A`. Logon is not a fill.

**Gated copy-intent hop cannot approve a live send.**

`RiskEngine.Evaluate` sets `AllowFixSend` only when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Every `Reject` persists `AllowFixSend=false` (L180–188).

`CopyTradingService`:

- `VenueReconciled = false` (const, L20). Increasing intents hit `VENUE_NOT_RECONCILED` before approve.
- Persist **always** `AllowFixSend = false` (L324), ignoring `decision.AllowFixSend`.
- Live branch L330 requires `decision.AllowFixSend && CurrentState==LIVE && NewOrderSingleImplemented && VenueReconciled`. `CanPromoteToLive=>false` plus `FromBaseline` never emitting `LIVE` means that branch is dead unless someone hand-writes `LIVE` into `TraderScores`.
- `NewOrderSingleImplemented => DemoDest` (L50). Reports that still say `NOS = const false` are **STALE**.

**Product dest-profit path is a literal zero:**

```43:46:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            shadowPnl,
            0,
            0,
            0,
```

`OverviewDto.DestinationRealPnl` is the second `0` (`DashboardModels.cs` L16). No dest realized-PnL aggregator exists. `LiveCopyPage` has **no** dest-PnL column and **no** send control. Blocker copy: “Pepperstone cannot be filled” (L24).

**Live dest identity is refused:**

`CTraderFixCopyOpen.SendAsync` L37–41 returns without writing `35=D` when `account == "1369850"` (or non-demo host/sender).

### 4.2 Why the unscoped claim still FAILs

`CopyTradingHostedService` 20s tick calls `ExecuteDemoCopyAsync` (L30). That method:

- Returns 0 unless `DemoDest` (host `demo-*`, sender `demo.*`, account ≠ `1369850`) — L45–48, L485–488.
- **Bypasses** `RiskEngine.Evaluate`.
- Can `CTraderFixCopyOpen.SendAsync` → `Build("D")` for dest close (L528) and dest open (L566).
- Caps `maxPerTick = 5`, `MaxAutoLots = 0.05`.
- Writes `intent.Status = "DEMO_SENT"` on fill (L593).

On-disk ledger `D:\Prop\data\demo_copy_ledger.json` (no secrets): source `305750` / pos `21250421` / dest `237339770` / 0.01 lot / `DestFillPrice=4390.2` / `DestClosed=false`. `ExecuteDemoCopyAsync` L500–512 **re-seeds** that same open row if missing. That is dest **exposure**. Dest-account P&L on demo `5328266` was **not** live-GET this slot, so “cannot be the profit path” is **unproven** as a universal. Constructor `0` is a dashboard lie-by-omission, not a mark.

`LiveCopyPage.tsx` L57 empty-state text admits the hop: *“Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position.”* The page itself cannot send (GET only). The hosted tick can.

Therefore: sending now **is not** the live-`1369850` profit path (`SAFE_BY_ABSENCE`). Sending now **can** be dest P&L on the demo dest. Unscoped claim **FAIL**.

This slot did not send.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — PASS

### 5.1 SHADOW is a source scoring state

`TraderStateMachine.FromBaseline` emits `SHADOW` when `quality >= 70 && risk < 40` after early eligibility (L200–201). `FeatureSnapshot.NetPnl` is **source** reconstructed XAU (`BaselineScorer.ComputeFeatures` L66, L111). Quality can be high while net is negative (quality formula L152–160 adds behavior and subtracts risk; net>0 is only +15). That source score is not dest cash.

### 5.2 Paper shadow is not dest

Hopper `GenerateShadowIntentsAsync` for `{SHADOW, LIVE_CANDIDATE, LIVE}` writes `Status="SHADOW_ONLY"` and `ShadowCopyEngine.SimulateEntry` (L336–359). `SimulateEntry` marks a synthetic fill from a quote (`ShadowCopyEngine.cs` L35–61). Dashboard `shadowPnl` is `Sum(SourceVsShadowSlippage)` (`EfDashboardQueries.cs` L29) — slippage vs source, not dest realized PnL.

`EfTradingStore.PersistDemoShadowAsync` also writes `SHADOW_ONLY` + `SimulateEntry` only when `state == TraderState.SHADOW` (L267–312). Still paper.

### 5.3 Dest profit is a different column and is hard-zero

`DestinationRealPnl` constructor `0` (claim 4.1). `LiveCopyPage` shows `SHADOW traders` as a count of `TraderState.SHADOW` (`CopyTradingService.GetStatusAsync` L59), next to `Live sends` = count of `ExecutionIntents` with `SentAt != null` (L57) — not dest PnL. `shadowFills` is `ShadowOrders.Count` (L56).

### 5.4 Policy / roster do not turn the SHADOW badge into dest profit

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `WATCH` / `EARLY_SCORE` / `INSUFFICIENT_DATA` as `TRADER_NOT_SHADOW_YET` (L81–85). That is an eligibility **label**, not dest PnL. It also **requires** demo/contest groups (`CopyGroupFilter.IsDemoOrContest`, L105–108) and `XauNetPnl > 0` (L99–103) and `CompletedXauTrades >= 20` (L93–97).

`CopyRosterEngine.Decide` AUTO_ADMIT when `IsTraderEligible` (L72–79). A SHADOW source on a demo/contest group **can** be `ADMITTED` and then demo-copied (claim 4 residual). The dest fill, if any, is dest **exposure**. The SHADOW badge and the paper shadow book remain source/paper. They are **not** destination profit.

Slot-37 style “SHADOW is dest AUTO_ADMIT ⇒ claim 5 FAIL” **over-claims**. AUTO_ADMIT is a dest-open gate, not dest-profit accounting. The assigned claim is “not destination profit.” That is proven.

---

## 6. `LiveCopyPage.tsx` (70/70) — assigned file, not a send path

Read in full. GETs `/api/copy/status` and `/api/copy/intents` only (`hooks.ts` L60–65). No POST. Renders `realCopyArmed`, SHADOW/LIVE counts, `liveSends`, `shadowFills`, QUOTE/TRADE, blockers, intent table. Cannot be the profit path. Cannot force `RealCopyEnabled` false. Does not implement `35=D`. Does not implement `CanPromoteToLive`. Empty-state L57 is an honesty string about the **hosted** demo hopper, not a page-side sender.

---

## 7. Risk to capital

| Book | Risk |
|---|---|
| Live Pepperstone `1369850` | **NONE** this slot — `CTraderFixSession` is `35=A` only; `CopyOpen` refuses that account; persist `AllowFixSend=false`; `CanPromoteToLive=>false`. `SAFE_BY_ABSENCE`. |
| Demo dest (lab `DemoDest` + 20s `ExecuteDemoCopyAsync`) | **Not absent.** File-proven hop can emit `35=D`. Existing ledger dest `237339770` still open. Not dest-profit accounting (`DestinationRealPnl=0`). Dest-account P&L **unmeasured** this slot. |
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
| `CTraderFixSession` line count 136 | **STALE** — file is 135 lines this slot. |

---

## 9. What this slot did not do

- Did not start or attach the API.
- Did not live-GET dest account PnL, FIX logon state, or `realCopyArmed`.
- Did not send `35=D`.
- Did not edit `.env`, product, or tests.
- Did not treat sibling `P500_VERIFY_*` books as evidence (re-read the files).
