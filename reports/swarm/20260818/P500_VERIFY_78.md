# P500_VERIFY_78 — Adversarial verifier (slot 78)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_78.md` |
| Agent / slot | P500 adversarial verifier **78** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (live `src/`, `apps/`, lab `.env`) |
| Assigned SUT (read in full this slot) | `CTraderFixSession.cs` (135/135), `BaselineScorer.cs` (212/212), `RiskEngine.cs` (189/189), `LiveCopyPage.tsx` (70/70) |
| Supporting files (claims 3–5 hop only) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `EnvFile.cs`, `apps/api/Program.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `CTraderFixOptions.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `CopyGroupFilter.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `DealIngestionService.cs`, `hooks.ts`, `data/demo_copy_ledger.json` |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Live `35=D` sent this slot | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None.** Quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73) and public dest ids `5328266` / `1369850`. No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live GET this pass | `GET http://127.0.0.1:5000/api/health` (and copy/settings) **blocked** (loopback SSRF). Runtime armed-bit **not** live-proven. File proof is enough to **FAIL** claim 3. |
| Binding rule | **FAIL any assigned claim that cannot be proven from a live file or a live GET.** Sibling swarm books are not evidence. This slot re-read the four named files. |

**Honesty:** Wanting dest profit is not an edge. Copy-all of the catalog would copy `RISK_BLOCKED` source losses. `SAFE_BY_ABSENCE` on live `1369850` is **not** “`RealCopyEnabled` stays false.” An armed env bit with no live sender is still an armed bit. Demo dest send is dest **exposure**, not dest-profit accounting. Constructor `DestinationRealPnl=0` is not a mark-to-market.

---

## 0. Verdict (binding)

**FAIL.** Claim 2 is the only claim that is proven as written. Claim 1 is proven only on `CTraderFixSession`. Claim 3 is **disproven**. Claims 4–5 are not proven as absolute product statements (demo dest hop + SHADOW as AUTO_ADMIT floor).

| # | Assigned claim | Measured | Class |
|---|---|---|---|
| 1 | no `35=D` builder | **PROVEN** on `CTraderFixSession.cs` 135/135 (`35=A` only). **NOT proven** product-wide: sibling `CTraderFixCopyOpen.Build("D")` is hosted. | **PASS_SESSION / FAIL_UNSCOPED** |
| 2 | `CanPromoteToLive` is false | **PROVEN** (`=> false`) | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **DISPROVEN** — only assignment is DI bind of `.env=true`; logon host never writes the bit | **FAIL** |
| 4 | sending now cannot be the profit path | **NOT proven** as written. Booked dest PnL is constructor `0`; live `1369850` refused. Residual: 20s `ExecuteDemoCopyAsync` → dest `35=D` on demo. | **FAIL** (as written) / **PASS_NOT_BOOKED_DEST_PROFIT** |
| 5 | SHADOW on demo is not destination profit | Paper SHADOW / slippage **is not** dest cash. SHADOW **is** the dest AUTO_ADMIT floor. Dest DTO is still `0`. | **PASS_PAPER / FAIL_AS_DEST_CLASS** |

One-line:

```text
FAIL slot 78: CTraderFixSession 35=A only (no D builder in that file); product Build("D") hosted; CanPromoteToLive=>false; RealCopyEnabled NOT forced false after logon (.env true + DI bind + logon logs-only); send-now is not booked dest-profit (DTO 0; live 1369850 refused) but demo dest hop can 35=D; SHADOW paper ≠ dest PnL but SHADOW is dest AUTO_ADMIT. Risk NONE on live 1369850. Live GET blocked.
```

Do **not** treat this FAIL as a license to send. Do **not** flip leftover `.env=true` into go-live. Operator should restore `REAL_COPY_EXECUTION_ENABLED=false` (this slot did **not** edit it).

---

## 1. Claim 1 — no `35=D` builder — PASS_SESSION / FAIL_UNSCOPED

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

Single `WriteAsync` (L49) of that logon. Then one `ReadAsync`. Socket disposed. Inbound `Extract(reply, "35")` is reply parse, not a builder. Tokens in this file: `Build("D")` = 0, `(35, "D")` = 0, `NewOrderSingle` = 0.

The other three assigned files also contain **zero** FIX NewOrderSingle builders:

- `BaselineScorer.cs` — scoring only.
- `RiskEngine.cs` — `AllowFixSend` bool only; no socket, no tag 35.
- `LiveCopyPage.tsx` — GET `/api/copy/status` + `/api/copy/intents`; no POST; no FIX.

Hosted caller of the assigned session is logon-only:

```48:58:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            ...);
        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            ...);
```

**Why the unscoped claim FAILs:** product C# has a generic `Build(string type, ...)` that emits tag 35 from the argument. Hits this slot:

| File | `Build("D")` |
|---|---|
| `CTraderFixCopyOpen.cs` L95 | hosted dest open/close |
| `CTraderFixDemoTestTrade.cs` L139, L163, L197 | CLI helper |
| `CTraderFixDemoMatrix.cs` L93 | CLI helper |

`CopyTradingHostedService` 20s tick calls `ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.SendAsync` (`CopyTradingService.cs` L528 close / L566 open). That is a live product hop, not a dead tool.

`CTraderFixCopyOpen` refuses live dest (`host` not `demo-` **or** `sender` not `demo.` **or** `account == "1369850"`) at L37–41. That refuse is **not** “no builder.”

Assigned-file claim: **PASS**. Product-wide “no `35=D` builder”: **FAIL**.

---

## 2. Claim 2 — `CanPromoteToLive` is false — PASS

`D:\Prop\src\Domain\Scoring\BaselineScorer.cs` read in full (212 lines). The method lives on `TraderStateMachine` in the same file:

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

The unused `current` argument cannot change the result. `FromBaseline` never returns `LIVE` or `LIVE_CANDIDATE`.

Ingest writes `CurrentState = score.SuggestedState` (`DealIngestionService.cs` L140). Unit test `BaselineScorerTests.cs` L26 asserts `CanPromoteToLive` is false after three disciplined winners go to `SHADOW`.

This is the only assigned claim that is proven **as written**, with no residual that reverses it.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL (disproven)

This is a hard FAIL. Independent file proof this slot:

**3.1 Only assignment in product C#**

`grep RealCopyEnabled\s*=` under `*.cs` / `*.tsx` / `*.json` = **one** hit:

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

That assignment is at **process start**, not after logon. It is `true` when the env key equals `"true"` (ignore-case).

**3.2 `.env` is `true` and is loaded**

`D:\Prop\.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret).

`apps/api/Program.cs` L10 `EnvFile.FindAndLoad()` then L13 `AddEnvironmentVariables()` then L15 `AddTraderIntelligence`. `EnvFile` candidates include `D:\Prop\.env` (`EnvFile.cs` L14).

**3.3 Logon host does not re-pin**

`CTraderFixLogonHostedService.ExecuteAsync` after both `TryLogonAsync` calls:

- Writes `Quote` / `Trade` `LoggedOn`, `Status`, `LastError`, `UpdatedAt` (L60–67).
- **Logs** `_runtime.RealCopyEnabled` (L69–70). Does **not** assign it.
- `PersistAsync` updates `FixSessionState` host/port/status/timestamps only (L101–107). No runtime flag.

There is **no** `RealCopyEnabled = false` anywhere after logon. The claim “forced false after logon” is the opposite of the file.

**3.4 Live GET**

Loopback GET blocked this slot (`SSRF blocked: 127.0.0.1`). That does **not** rescue claim 3: the hosted process, if started with this `.env`, constructs the singleton as `true` and never forces it false.

`LiveCopyPage` will render `REAL_COPY armed = YES` when `status.realCopyArmed` is true (`LiveCopyPage.tsx` L13; `CopyGateStatus.RealCopyArmed` is `_runtime.RealCopyEnabled` at `CopyTradingService.cs` L64). `/api/health` and `/api/settings` also echo `runtime.RealCopyEnabled` (`Program.cs` L55 / L76).

`CTraderFixOptions.RealCopyExecutionEnabled` defaults false (`CTraderFixOptions.cs` L35) and is **not** what DI binds. Citing that POCO default as “forced false after logon” would be a lie.

---

## 4. Claim 4 — sending now cannot be the profit path — FAIL as written

The assigned files prove **booked dest profit is not computed**. They do **not** prove “sending now cannot be dest P&L.” Instruction: FAIL unproven claims.

**4.1 `CTraderFixSession` cannot send a ticket**

Claim 1: only `35=A`. Logon is not a fill. Proven.

**4.2 Gated hop cannot approve a *live* send**

`RiskEngine.Evaluate` sets `AllowFixSend` only when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Every `Reject` persists `AllowFixSend=false` (L180–188). The comment at L90–93 (“never allows FIX send” when `RealExecutionEnabled==false`) is **not** a second assignment; `allowSend` is still AND-gated at L147.

`CopyTradingService`:

- `VenueReconciled = false` (const, L20). Increasing intents hit `VENUE_NOT_RECONCILED` before approve.
- Persist **always** `AllowFixSend = false` (L324), ignoring `decision.AllowFixSend`.
- Live branch L330 requires `decision.AllowFixSend && CurrentState==LIVE && NewOrderSingleImplemented && VenueReconciled`. `CanPromoteToLive=>false` plus `FromBaseline` never emitting `LIVE` means that branch is dead unless someone hand-writes `LIVE` into `TraderScores`.
- `NewOrderSingleImplemented => DemoDest` (L50). Reports that still say `NOS=const false` are **STALE**.

**4.3 Product dest-profit path is a literal zero (accounting only)**

```43:46:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            shadowPnl,
            0,
            0,
            0,
```

`OverviewDto.DestinationRealPnl` is the second `0` (`DashboardModels.cs` L16). No dest realized-PnL aggregator. `GetRiskAsync` also constructs five leading zeros (`EfDashboardQueries.cs` L208). `LiveCopyPage` has **no** dest-PnL column and **no** send control. Blocker copy: “Pepperstone cannot be filled” (L24). Empty-state text admits demo dest auto-sends (L57) — that is dest **exposure**, not dest-profit accounting.

**4.4 Live dest identity is refused**

`CTraderFixCopyOpen.SendAsync` L37–41 returns without writing `35=D` when `account == "1369850"` (or non-demo host/sender). Default hosted account in the logon service is demo `5328266` (`CTraderFixLogonHostedService.cs` L41).

**4.5 Residual that FAILs the absolute claim**

`CopyTradingHostedService` 20s tick (`L21–41`) calls `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**. That last method:

- Returns 0 only if `!DemoDest` (host not `demo-*` / sender not `demo.*` / account is `1369850`) or password blank.
- **Does not** call `RiskEngine.Evaluate`.
- Can `CTraderFixCopyOpen.SendAsync` → `Build("D")` for ADMITTED roster seats with open XAU ≤ `MaxAutoLots` (0.05) and for dest closes.

On-disk ledger `D:\Prop\data\demo_copy_ledger.json` (and the same seed inside `ExecuteDemoCopyAsync` L500–511) records dest fill:

- source login `305750` / source pos `21250421`
- dest pos `237339770` / dest clord `C20260818093047317`
- lots `0.01` / `DestFillPrice=4390.2` / `DestClosed=false`

That is dest exposure at the venue. Absolute “sending now cannot be dest P&L” is **unproven** (no live GET of dest account). Constructor `0` is not a venue mark. Therefore claim 4 **as written** **FAIL**.

Scoped weaker claim “sending now is not the **booked product profit path**” would be PASS_NOT_BOOKED_DEST_PROFIT. That is not what was assigned.

This slot did not send.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — PASS_PAPER / FAIL_AS_DEST_CLASS

**5.1 SHADOW is a source scoring state — proven**

`TraderStateMachine.FromBaseline` emits `SHADOW` when `quality >= 70 && risk < 40` after early eligibility (L200–201). `FeatureSnapshot.NetPnl` is **source** reconstructed XAU (`BaselineScorer.ComputeFeatures` L66, L111). Quality can be high while `NetPnl` is still modest because quality starts at 50 and adds behavior/risk terms (L152–160). That number is not dest cash.

**5.2 Paper shadow is not dest — proven**

Hopper `GenerateShadowIntentsAsync` for `{SHADOW, LIVE_CANDIDATE, LIVE}` writes `Status="SHADOW_ONLY"` and `ShadowCopyEngine.SimulateEntry` (`CopyTradingService.cs` L336–359). `SimulateEntry` (`ShadowCopyEngine.cs` L35–61) marks a **synthetic** fill from a quote + 0.05-point modeled delay. Dashboard `shadowPnl` is `Sum(SourceVsShadowSlippage)` (`EfDashboardQueries.cs` L29) — slippage vs source, not dest realized PnL.

`LiveCopyPage` shows `SHADOW traders` as a count of `TraderState.SHADOW` (`CopyTradingService.GetStatusAsync` L59), next to `Live sends` = count of `ExecutionIntents` with `SentAt != null` (L57) — not dest PnL.

**5.3 Dest profit column is hard-zero — proven**

`DestinationRealPnl` constructor `0` (claim 4.3). SHADOW-on-demo **paper** is therefore not destination profit.

**5.4 Why FAIL_AS_DEST_CLASS**

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `WATCH` / `EARLY_SCORE` / `INSUFFICIENT_DATA` as `TRADER_NOT_SHADOW_YET` (L81–85). `CopyRosterEngine.Decide` AUTO_ADMIT when `IsTraderEligible` (L72–80). `CopyGroupFilter` **requires** demo/contest groups. Combined with claim 2 (`FromBaseline` never emits `LIVE`), the only scorer-produced state that can sit on the dest roster is **SHADOW** (plus hand-written `LIVE`/`LIVE_CANDIDATE`).

`ExecuteDemoCopyAsync` then dest-sends for `Status=="ADMITTED"` roster seats **without** re-checking `TraderState.SHADOW` as “paper only.” So “SHADOW on demo” **is** the dest selection class. The dest fill, if any, is dest exposure. The SHADOW badge and the paper shadow book remain source/paper.

Assigned wording “SHADOW on demo is not destination profit” is therefore:

- **PASS** for the paper book / slippage sum / dest DTO.
- **FAIL** if read as “SHADOW-on-demo cannot be the dest-selection class.”

Instruction: FAIL unproven absolute. The dest-class reading is not disproven — it is **true that SHADOW is the admit floor**. Score: **PASS_PAPER / FAIL_AS_DEST_CLASS**.

---

## 6. `LiveCopyPage.tsx` (70/70) — assigned file, not a send path

Read in full. GETs `/api/copy/status` and `/api/copy/intents` only (`hooks.ts` L60–65). No POST. Renders `realCopyArmed`, SHADOW/LIVE counts, `liveSends`, `shadowFills`, QUOTE/TRADE, blockers, intent table.

Cannot force `RealCopyEnabled` false. Does not implement `35=D`. Does not implement `CanPromoteToLive`. Empty-state L57 is an honesty leak: it documents dest auto-send, which is why claim 4 cannot be rubber-stamped PASS.

---

## 7. Risk to capital

| Book | Risk |
|---|---|
| Live Pepperstone `1369850` | **NONE** this slot — `CTraderFixSession` is `35=A` only; `CopyOpen` refuses that account; persist `AllowFixSend=false`; `CanPromoteToLive=>false`. `SAFE_BY_ABSENCE` on live dest. |
| Demo dest (lab `DemoDest` + 20s `ExecuteDemoCopyAsync`; default account `5328266`) | **Not absent.** File-proven hop can emit `35=D`. On-disk ledger dest `237339770` still open. Not dest-profit accounting (`DestinationRealPnl=0`). |
| This slot | No attach. No send. No `.env` edit. Live GET blocked. |

---

## 8. Stale pins this slot refuses to repeat

| Pin | Status |
|---|---|
| `NewOrderSingleImplemented = const false` | **STALE** — HEAD is `=> DemoDest` (`CopyTradingService.cs` L50). |
| Product / tree `35=D=0` | **STALE** — `CTraderFixCopyOpen.Build("D")` + hosted caller + CLI helpers. Assigned `CTraderFixSession` is still `35=A` only. |
| `RealCopyEnabled` forced false after logon / W500 “stays false” | **FALSE** — claim 3. |
| `CTraderFixOptions.RealCopyExecutionEnabled` default false as runtime | **UNBOUND** — DI reads env key, not that POCO. |
| Live GET dest PnL / armed bit | **UNPROVEN this slot** (SSRF). Not used as PASS evidence. |
| `SAFE_BY_ABSENCE` on demo dest | **FALSE** — hop is wired. Only live `1369850` is absence-safe. |
