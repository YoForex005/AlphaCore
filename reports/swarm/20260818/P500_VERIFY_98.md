# P500_VERIFY_98 — Adversarial verifier (slot 98)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_98.md` |
| Agent / slot | P500 adversarial verifier **98** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (live `src/`, `apps/`, lab `.env`) |
| Assigned SUT (read in full this slot) | `CTraderFixSession.cs` (135/135), `BaselineScorer.cs` (212/212), `RiskEngine.cs` (189/189), `LiveCopyPage.tsx` (70/70) |
| Supporting files (claims 1/3–5 hop; not trusted from sibling books) | `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `CTraderFixLogonHostedService.cs`, `CTraderFixOptions.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CopyTradingModels.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `CopyGroupFilter.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `DealIngestionService.cs`, `EfTradingStore.cs` (`PersistDemoShadowAsync`), `EnvFile.cs`, `apps/api/Program.cs`, `apps/web/src/api/hooks.ts`, `data/demo_copy_ledger.json`, `tests/Unit/BaselineScorerTests.cs` |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Live `35=D` sent this slot | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None.** Quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73) and public dest ids `5328266` / `1369850`. No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live GET this pass | `GET http://127.0.0.1:5000/api/health` **SSRF-blocked**. `GET http://localhost:5000/api/health` **SSRF-blocked**. `GET http://localhost:5000/api/copy/status` **SSRF-blocked**. `open_page` `http://127.0.0.1:5000/api/health` retrieve fail. **No live JSON.** Runtime `realCopyEnabled` bit and dest cash **not** live-proven. |
| Binding rule | **FAIL any assigned claim that cannot be proven from a live file or a live GET.** Sibling swarm books are not evidence. Constructor `0` is not a dest mark. A comment is not a runtime pin. |

**Honesty:** Wanting dest profit is not an edge. Copy-all of the catalog would copy `RISK_BLOCKED` source losses. `SAFE_BY_ABSENCE` on live `1369850` is **not** “`RealCopyEnabled` stays false.” An armed env bit with no live sender is still an armed bit. Demo dest send is dest **exposure**, not dest-profit accounting. Constructor `DestinationRealPnl=0` is not a dest mark.

This slot did **not** trust `P500_VERIFY_57` / `P500_VERIFY_72` integers. Those reports were used only as a format template. Every citation below was re-read from disk this pass.

---

## 0. Verdict (binding)

**FAIL.** Claim 3 is **disproven** on disk. Unscoped claim 1 is **false**. Unscoped claim 4 is **unproven** as written (demo hopper can `35=D` now; dest DTO `0` is not a mark). Claims 2 and 5 are file-proven. Live GET did not attach.

| # | Assigned claim | Measured | Class |
|---|---|---|---|
| 1 | no `35=D` builder | **PROVEN** on `CTraderFixSession.cs` (135/135, only `(35,"A")`). **DISPROVEN** product-wide: `Build("D")` ×5 + hosted caller. | **FAIL** unscoped / **PASS_SESSION** |
| 2 | `CanPromoteToLive` is false | **PROVEN** (`=> false`) | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **DISPROVEN** — only assignment is DI bind of `.env=true`; logon host never writes the bit | **FAIL** |
| 4 | sending now cannot be the profit path | Session send is logon-only; dest DTO is constructor `0`; live `1369850` refused. **Not** proven that send-now cannot book dest P&L: 20s `ExecuteDemoCopyAsync` bypasses `Evaluate` and can `35=D` on demo dest. Ledger dest still open. | **FAIL** unscoped / **PASS_NOT_BOOKED_DEST_PROFIT** |
| 5 | SHADOW on demo is not destination profit | **PROVEN** — source state + paper `SimulateEntry`; dest DTO is literal `0`. Residual: SHADOW is dest AUTO_ADMIT floor. | **PASS** |

One-line:

```text
FAIL slot 98: CTraderFixSession 35=A only (product Build("D")×5 hosted); CanPromoteToLive=>false; RealCopyEnabled NOT forced false after logon (.env true + DI bind + logon logs-only); send-now is not booked dest-profit (DTO 0; live 1369850 refused) but demo dest hop can 35=D now; SHADOW-on-demo is source/paper not dest PnL. Risk NONE on live 1369850. Live GET blocked.
```

Do **not** treat this FAIL as a license to send. Do **not** flip leftover `.env=true` into go-live. Operator should restore `REAL_COPY_EXECUTION_ENABLED=false` (this slot did **not** edit it).

---

## 1. Claim 1 — no `35=D` builder — FAIL unscoped / PASS_SESSION

### 1.1 Assigned file — no builder

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` is **135** lines. Read in full this slot.

Grep of tag `35` in this file = **3** hits, all logon:

| Line | Role |
|---|---|
| L55 | inbound parse `Extract(reply, "35")` |
| L73 | error string `Logon rejected 35={msgType}` |
| L96 | outbound `(35, "A")` |

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

Hosted caller of this type is logon-only:

```48:58:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            ...);
        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            ...);
```

The other three assigned files have **zero** FIX assemblers (`BaselineScorer`, `RiskEngine`, `LiveCopyPage`).

### 1.2 Product-wide claim is false

`rg Build\("D"\)` on `*.cs` under `D:\Prop` (excluding reports) = **5** writers:

| File | Count | Role |
|---|---|---|
| `CTraderFixCopyOpen.cs` L95 | 1 | generic `Build(string type, …)` + `Build("D", …)` |
| `CTraderFixDemoTestTrade.cs` L139/163/197 | 3 | CLI demo helper |
| `CTraderFixDemoMatrix.cs` L93 | 1 | CLI matrix |

`CTraderFixCopyOpen.SendAsync` is **hosted**. `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566 calls it. `CopyTradingHostedService` 20s tick L30 calls `ExecuteDemoCopyAsync`.

`CTraderFixCopyOpen` refuses live dest (`host` not `demo-` **or** `sender` not `demo.` **or** `account == "1369850"`) at L37–41. That is a live-identity refuse, **not** “no builder.”

Unscoped “no `35=D` builder” **FAIL**. Assigned-session “no `35=D` builder” **PASS**.

---

## 2. Claim 2 — `CanPromoteToLive` is false — PASS

`D:\Prop\src\Domain\Scoring\BaselineScorer.cs` read in full (212 lines).

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

The unused `current` argument cannot change the result. `FromBaseline` (L189–207) returns only `INSUFFICIENT_DATA`, `RISK_BLOCKED`, `SHADOW`, `WATCH`, `EARLY_SCORE`. It never returns `LIVE` or `LIVE_CANDIDATE`.

Ingest writes `CurrentState = score.SuggestedState` (`DealIngestionService.cs` L140). Product callers of `CanPromoteToLive` are the unit test (`BaselineScorerTests.cs` L26) asserting false after three disciplined winners go to `SHADOW`.

`CanPromoteToLive` is therefore **hard-false** in HEAD. PASS.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL

This is a hard FAIL. Independent file proof. Live GET not required and was not obtained.

### 3.1 Only assignment in product C#

`rg RealCopyEnabled\s*=` under `*.cs` / `*.tsx` = **one** write in product source:

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

### 3.2 `.env` is `true` and is loaded

`D:\Prop\.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret quoted).

`apps/api/Program.cs` L10 `EnvFile.FindAndLoad()` then L13 `AddEnvironmentVariables()` then L15 `AddTraderIntelligence`. `EnvFile` candidates include `D:\Prop\.env` (`EnvFile.cs` L14).

### 3.3 Logon host does not re-pin

`CTraderFixLogonHostedService.ExecuteAsync` after both `TryLogonAsync` calls:

- Writes `Quote` / `Trade` `LoggedOn`, `Status`, `LastError`, `UpdatedAt` (L60–67).
- **Logs** `_runtime.RealCopyEnabled` (L69–70). Does **not** assign it.
- `PersistAsync` updates `FixSessionState` host/port/status/timestamps only (L101–107). No runtime flag.

There is no `RealCopyEnabled = false` anywhere after logon. The claim “forced false after logon” is the **opposite** of the file.

### 3.4 Assigned UI would display the armed bit

`LiveCopyPage.tsx` L13 renders `REAL_COPY armed` as `YES` when `status.realCopyArmed` is true. `CopyGateStatus.RealCopyArmed` is `_runtime.RealCopyEnabled` (`CopyTradingService.cs` L64). If the process started with this `.env`, the singleton is constructed `true` and never forced false.

`CTraderFixOptions.RealCopyExecutionEnabled` defaults false (`CTraderFixOptions.cs` L35) and is **not** what DI binds. Citing that POCO default as “forced false after logon” would be a lie.

Live GET of `/api/health` (`realCopyEnabled`) and `/api/settings` (`featureFlags.REAL_COPY_EXECUTION_ENABLED`) did **not** return JSON this slot. That does **not** rescue claim 3: file proof already disproves the force-false pin.

---

## 4. Claim 4 — sending now cannot be the profit path — FAIL unscoped

Proven as “not booked dest-profit accounting.” **Not** proven as “no dest fill can exist now.”

### 4.1 `CTraderFixSession` cannot send a ticket

Claim 1 session scope: only `35=A`. Logon is not a fill. Assigned `LiveCopyPage` has no POST / no send control (`hooks.ts` L60–65 are GET only).

### 4.2 Gated hop cannot approve a live send

`RiskEngine.Evaluate` sets `AllowFixSend` only when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Every `Reject` persists `AllowFixSend=false` (L180–188). When `RealExecutionEnabled==false`, the comment at L90–93 says the shadow path never allows FIX send; `allowSend` is still AND-gated.

`CopyTradingService`:

- `VenueReconciled = false` (const, L20). Increasing intents hit `VENUE_NOT_RECONCILED` before approve. **Honesty residual:** `GetStatusAsync` reports `VenueReconciled: DemoDest` (L67) — DTO can say true while `Evaluate` still uses the const false.
- Persist **always** `AllowFixSend = false` (L324), discarding the engine bit.
- Live branch L330 requires `decision.AllowFixSend && CurrentState==LIVE && NewOrderSingleImplemented && VenueReconciled`. `CanPromoteToLive=>false` plus `FromBaseline` never emitting `LIVE` means that branch is dead unless someone hand-writes `LIVE` into `TraderScores`.
- `NewOrderSingleImplemented => DemoDest` (L50). Reports that still say `NOS=const false` are **STALE**.

### 4.3 Product dest-profit path is a literal zero

```43:46:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            shadowPnl,
            0,
            0,
            0,
```

`OverviewDto.DestinationRealPnl` is the second `0` (`DashboardModels.cs` L16). No dest realized-PnL aggregator. `LiveCopyPage` has **no** dest-PnL column. Blocker copy: “Pepperstone cannot be filled” (L24). Empty-state text admits demo dest auto-sends (L57) — that is dest **exposure**, not dest-profit accounting.

### 4.4 Live dest identity is refused

`CTraderFixCopyOpen.SendAsync` L37–41 returns without writing `35=D` when `account == "1369850"` (or non-demo host/sender).

### 4.5 Why the unscoped claim FAILs

`CopyTradingHostedService` 20s tick calls `ExecuteDemoCopyAsync` (L30). That method **bypasses** `RiskEngine.Evaluate`, does **not** read `RealCopyEnabled`, and can `Build("D")` on demo dest when `DemoDest` (host `demo-*`, sender `demo.*`, account ≠ `1369850`), roster `ADMITTED`, open XAU, `MaxAutoLots=0.05`.

On-disk ledger `D:\Prop\data\demo_copy_ledger.json` (public ids only):

- source `305750` / pos `21250421`
- dest `237339770` / cl `C20260818093047317`
- 0.01 lot / `DestFillPrice=4390.2` / `DestClosed=false`

That is dest exposure **now**. It is **not** `DestinationRealPnl`. It is **not** a measured dest edge. Absolute “sending now cannot be the profit path” is therefore **unproven**: dest cash on demo `5328266` was not live-marked this slot (GET blocked). Constructor `0` is not a mark. Unscoped claim **FAIL**. Scoped “not booked dest-profit / not live `1369850`” **PASS**.

This slot did not send.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — PASS

### 5.1 SHADOW is a source scoring state

`TraderStateMachine.FromBaseline` emits `SHADOW` when `quality >= 70 && risk < 40` after early eligibility (L200–201). `FeatureSnapshot.NetPnl` is **source** reconstructed XAU (`BaselineScorer.ComputeFeatures` L66, L111).

### 5.2 Paper shadow is not dest

Hopper `GenerateShadowIntentsAsync` for `{SHADOW, LIVE_CANDIDATE, LIVE}` writes `Status="SHADOW_ONLY"` and `ShadowCopyEngine.SimulateEntry` (`CopyTradingService.cs` L336–359). `SimulateEntry` marks a synthetic fill from a quote (`ShadowCopyEngine.cs` L35–61). Dashboard `shadowPnl` is `Sum(SourceVsShadowSlippage)` (`EfDashboardQueries.cs` L29) — slippage vs source, not dest realized PnL.

`PersistDemoShadowAsync` also writes `SHADOW_ONLY` + `SimulateEntry` and returns early unless state is `SHADOW` (`EfTradingStore.cs` L267–312). Still paper.

### 5.3 Dest profit is a different column and is hard-zero

`DestinationRealPnl` constructor `0` (claim 4.3). `LiveCopyPage` shows `SHADOW traders` as a count of `TraderState.SHADOW` (`CopyTradingService.GetStatusAsync` L59), next to `Live sends` = count of `ExecutionIntents` with `SentAt != null` (L57) — not dest PnL.

### 5.4 Residual — SHADOW is dest ADMIT floor, not dest profit

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `WATCH` / `EARLY_SCORE` / `INSUFFICIENT_DATA` as `TRADER_NOT_SHADOW_YET` (L81–85). That is an eligibility label, not dest PnL. `CopyGroupFilter` **requires** demo/contest groups for admit (`CopyGroupFilter.cs` L9–22). A SHADOW source on a demo group can be `AUTO_ADMIT` (`CopyRosterEngine.cs` L72–80) and then demo-copied (claim 4.5). The dest fill, if any, is dest exposure. The SHADOW badge and the paper shadow book remain source/paper. They are **not** destination profit.

---

## 6. `LiveCopyPage.tsx` (70/70) — assigned file, not a send path

Read in full. GETs `/api/copy/status` and `/api/copy/intents` only (`hooks.ts` L60–65). No POST. Renders `realCopyArmed`, SHADOW/LIVE counts, `liveSends`, `shadowFills`, QUOTE/TRADE, blockers, intent table. Cannot be the profit path. Cannot force `RealCopyEnabled` false. Does not implement `35=D`. Does not implement `CanPromoteToLive`. Empty-state L57 documents dest auto-send after `ADMITTED` — UI confession of the residual hop, not dest-profit accounting.

---

## 7. Risk to capital

| Book | Risk |
|---|---|
| Live Pepperstone `1369850` | **NONE** this slot — `CTraderFixSession` is `35=A` only; `CopyOpen` refuses that account; persist `AllowFixSend=false`; `CanPromoteToLive=>false`. |
| Demo dest (lab `DemoDest` + 20s `ExecuteDemoCopyAsync`) | **Not absent.** File-proven hop can emit `35=D` without `RiskEngine.Evaluate` and without reading `RealCopyEnabled`. Existing ledger dest `237339770` still open. Not dest-profit accounting (`DestinationRealPnl=0`). |
| This slot | No attach. No send. No `.env` edit. |

---

## 8. Stale pins this slot refuses to repeat

| Pin | Status |
|---|---|
| `NewOrderSingleImplemented = const false` | **STALE** — HEAD is `=> DemoDest` (`CopyTradingService.cs` L50). |
| Product / tree `35=D=0` | **STALE** — `CTraderFixCopyOpen.Build("D")` + hosted caller + CLI ×4. Assigned `CTraderFixSession` is still `35=A` only. |
| `RealCopyEnabled` forced false after logon / W500 “stays false” | **FALSE** — claim 3. |
| `CTraderFixOptions.RealCopyExecutionEnabled` default false as runtime | **UNBOUND** — DI reads env key, not that POCO. |
| Live GET dest PnL / armed bit | **UNPROVEN this slot** (SSRF). Not used as PASS evidence. |
| Constructor `DestinationRealPnl=0` as a dest mark | **NOT A MARK** — does not prove dest cash is zero. |
| `GetStatusAsync.VenueReconciled: DemoDest` as recon | **DTO LIE vs const** — `Evaluate` uses `VenueReconciled = false`. |

---

## 9. Assigned-file census (this slot)

| File | Lines read | `35=D` builder | `CanPromoteToLive` | `RealCopyEnabled` write | Dest profit |
|---|---|---|---|---|---|
| `CTraderFixSession.cs` | 135/135 | **No** (only `35=A`) | n/a | n/a | n/a |
| `BaselineScorer.cs` | 212/212 | No | `=> false` L211 | n/a | Source `NetPnl` only |
| `RiskEngine.cs` | 189/189 | No | n/a | Reads `RealExecutionEnabled`; does not force it | `AllowFixSend` AND-gated |
| `LiveCopyPage.tsx` | 70/70 | No | n/a | Displays `realCopyArmed` | No dest-PnL column |

---

**Slot 98 verdict: FAIL.**
