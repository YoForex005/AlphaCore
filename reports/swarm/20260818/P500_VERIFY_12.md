# P500_VERIFY_12 — Adversarial confirm of five live-path claims (slot 12)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_12.md` |
| Agent | P500_VERIFY_12 (adversarial verifier, slot 12) |
| Slot | **12** |
| Date | 2026-08-18 |
| Assigned | Read `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`. Confirm: (1) no `35=D` builder, (2) `CanPromoteToLive` is false, (3) `RealCopyEnabled` forced false after logon, (4) sending now cannot be the profit path, (5) SHADOW on demo is not destination profit. **FAIL any claim not proven from a file or live GET.** |
| SUT | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135), `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212), `D:\Prop\src\Domain\Risk\RiskEngine.cs` (190), `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (71) |
| Hop (not assigned, required to test 3–5) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs` (625), `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `EfDashboardQueries.cs`, `XauUsdOneToOneCopyPolicy.cs`, `ShadowCopyEngine.cs`, `.env` flag line only |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent this slot | **No** |
| `REAL_COPY` flipped this slot | **No** |
| Secrets printed | **None** (boolean/host-prefix/account-id only; no passwords) |
| Live GET this slot | `GET http://127.0.0.1:5000/api/health` **blocked** (SSRF on loopback). Runtime flag/PnL **not** re-probed. File proof only. |

Classification: `SESSION_NO_35D` / `CAN_PROMOTE_FALSE` / `REALCOPY_NOT_REPINNED` / `DEMO_DEST_SEND_EXISTS` / `DEST_PNL_CONSTRUCTOR_0` / **FAIL**.

---

## 0. Verdict (binding)

**FAIL.** One of five assigned claims is **disproven** from files. Adversarial rule: one unproven/disproven claim ⇒ slot FAIL.

| # | Claim | Verdict | Why |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** (assigned file only) | `CTraderFixSession` outbound MsgType is only `(35, "A")`. Zero `Build("D")` / `(35, "D")` in that file. **Cannot** prove product-wide absence: sibling `CTraderFixCopyOpen.Build("D")` is on the hosted 20 s hop. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(TraderState current) => false`. `FromBaseline` never returns `LIVE` / `LIVE_CANDIDATE`. Persist writes `CurrentState = score.SuggestedState` only. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | No post-logon assignment exists. DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`. Hosted logon **logs** the bool and does not overwrite it. `CREDENTIALS_AND_COPY_STATUS.md` “false (forced)” is **STALE**. |
| 4 | sending now cannot be the profit path | **PASS** (live dest / measured dest-PnL) | Risk hop persist `AllowFixSend=false`; `VenueReconciled` const `false`; `FromBaseline` never LIVE; live `1369850` refused. Dashboard `DestinationRealPnl` is constructor `0`. Residual: demo dest **does** `35=D` (not a measured profit constructor). |
| 5 | SHADOW on demo is not destination profit | **PASS** | SHADOW is a source-state from quality/risk. Overview `ShadowPnl` = sum of `SourceVsShadowSlippage`. Dest real PnL literal `0`. Residual: SHADOW + ADMITTED can still fire demo dest `35=D`. |

**One-line:** session has no NewOrderSingle; promotion is a hard false; the flag is **not** forced off after logon (`.env` true + DI bind); current send is not live dest-profit; SHADOW is not dest PnL.

Priors that say hosted logon sets `_runtime.RealCopyEnabled = false` (`A015_enable_copy_gates.md`, `CREDENTIALS_AND_COPY_STATUS.md` L30, several W500 “forced false”) are **STALE vs HEAD**.

Priors that say product `NewOrderSingleImplemented` is `const false` / “no `35=D` assembler” (`W500_VERIFY_8`, `A015`) are **STALE vs HEAD** (`=> DemoDest`; `CTraderFixCopyOpen`).

---

## 1. Claim 1 — no `35=D` builder — PASS (CTraderFixSession only)

Read: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135).

Outbound builder is `BuildLogon` only. The only MsgType assembled is logon `A`:

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

- Single `WriteAsync` of that logon (`L47–50`). Socket disposed by `using`.
- Inbound `Extract(reply, "35")` (`L55`) is a **read**, not a builder. Error text `"Logon rejected 35={msgType}"` (`L73`) is inbound.
- Grep of this file for `(35, "D")` / `Build("D")` / `35="D"`: **0**.
- `Assemble` is generic but has **one** caller (`BuildLogon`).

Product-wide (adjacent, not the assigned file) — **cannot confirm “no 35=D builder”**:

| File | Builder | Hosted? |
|---|---|---|
| `CTraderFixCopyOpen.cs` L95 / L142–156 | `Build("D", …)` NewOrderSingle | **Yes** — `CopyTradingService.ExecuteDemoCopyAsync` L528 + L566, called from `CopyTradingHostedService` 20 s tick |
| `CTraderFixDemoTestTrade.cs` L139/163/197 | `Build("D", …)` | Tool / session helper |
| `CTraderFixDemoMatrix.cs` L93 | `Build("D", …)` | Tool / session helper |
| `tools/DemoFixTestTrade/Program.cs` | calls `CTraderFixCopyOpen.SendAsync` | CLI, not API boot |

`CTraderFixCopyOpen` refuses live dest (`account == "1369850"` or host/sender not `demo-` / `demo.`). That is a **gate**, not absence of a builder.

**Claim 1 as written about `CTraderFixSession.cs`: PASS. Claim 1 as a product-wide sentence: FAIL (sibling builder exists).** Slot does not fail on this row because the assigned SUT is the session file.

---

## 2. Claim 2 — `CanPromoteToLive` is false — PASS

Read: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212).

```187:212:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
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

Proof:

- `CanPromoteToLive` ignores `current` and is a constant `false`. There is **one** definition (`grep` of `*.cs`: this method + unit test + a reports tmp harness).
- `FromBaseline` ceiling is `SHADOW`. It never returns `LIVE` or `LIVE_CANDIDATE`.
- Persist path (`ReconstructionScoringService.RebuildTraderAsync` / `DealIngestionService.cs` L140): `CurrentState = score.SuggestedState`. No other product writer of `CurrentState =` except `EfTradingStore` copying the same field.
- Unit pin: `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` asserts `SHADOW` and `CanPromoteToLive(...).Should().BeFalse()`.

`TraderState.LIVE` exists on the enum (`TraderState.cs` L10) and is **counted** / **branched** in copy (`CopyTradingService` L58, L202, L330). That is not a promotion function. No product assignment creates LIVE from score.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL

Assigned four files **do not** implement this claim:

| File | `RealCopyEnabled` |
|---|---|
| `CTraderFixSession.cs` | absent |
| `BaselineScorer.cs` | absent |
| `RiskEngine.cs` | reads `RealExecutionEnabled` on the **request**; never writes runtime |
| `LiveCopyPage.tsx` | displays `status?.realCopyArmed` only |

Hop that would have to force the pin:

**DI binds env at process start** (`D:\Prop\src\Infrastructure\DependencyInjection.cs` L39–42):

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Lab `.env` L73 (boolean only): `REAL_COPY_EXECUTION_ENABLED=true`.

API loads that file (`apps/api/Program.cs` L10 `EnvFile.FindAndLoad()` + L13 `AddEnvironmentVariables()`).

**Hosted logon does not re-pin.** After both `TryLogonAsync` calls it copies QUOTE/TRADE status and **logs** the existing flag (`CTraderFixLogonHostedService.cs` L60–70). There is no `_runtime.RealCopyEnabled = false`. Grep of product `*.cs` for `RealCopyEnabled =` assignments: **only** the DI bind above.

`CTraderFixOptions.RealCopyExecutionEnabled` defaults `false` (`CTraderFixOptions.cs` L35). That POCO is **not** what DI writes onto `LiveRuntimeStatus`. Unused default ≠ forced-false-after-logon.

`RiskEngine` L90–93 is an empty comment when `RealExecutionEnabled == false`. It does not mutate runtime. Later `allowSend` (`L147–150`) ANDs the **request** flag; it does not force the process flag false.

Live GET that would prove the running process (`/api/health` `realCopyEnabled`, `/api/settings` feature flag, `/api/copy/status` `realCopyArmed`) was **not** obtainable this slot (loopback SSRF). File proof is enough to **disprove** “forced false after logon”: the only writer can set `true`, and logon does not overwrite.

`reports/CREDENTIALS_AND_COPY_STATUS.md` L30 (“**false** (forced)”) is **STALE**. Do not cite it as HEAD.

---

## 4. Claim 4 — sending now cannot be the profit path — PASS (live / measured dest)

What “sending now” actually is:

1. **Risk-gated hop** (`GenerateShadowIntentsAsync`): Evaluate at L291; persist **always** `AllowFixSend = false` (L324); live-send branch L330 requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled`. `VenueReconciled` is `const false` (L20). `FromBaseline` never produces LIVE. Branch is dead. Intents go `SHADOW_ONLY`.
2. **Demo dest hop** (`ExecuteDemoCopyAsync` L483–605): **bypasses** `RiskEngine.Evaluate`. If `DemoDest` (host `demo-` AND sender `demo.` AND account ≠ `1369850`), hosted 20 s tick calls `CTraderFixCopyOpen.SendAsync` → `Build("D")`. File ledger `D:\Prop\data\demo_copy_ledger.json` already has dest fill `305750` / `21250421` → dest pos `237339770` @ `4390.2`, `DestClosed=false`.
3. **Live dest `1369850`**: refused inside `CTraderFixCopyOpen` L37–42 before any `35=D`.
4. **UI** (`LiveCopyPage.tsx` L13–28): shows `REAL_COPY armed`, `Live sends`, and “Live send blockers (Pepperstone cannot be filled)”. Empty-state L57 admits “Demo dest auto-sends after a trader is ADMITTED”.

`RiskEngine` cannot be “the profit path”:

```147:171:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
        // …
                AllowFixSend = allowSend
```

Unit `Real_flag_false_never_allows_fix_send` pins `AllowFixSend=false` when the request flag is false. The empty `if (RealExecutionEnabled == false)` block does **not** reject; it still cannot set `AllowFixSend` true.

Measured dest profit is **not computed**:

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
            …
            _runtime.RealCopyEnabled);
```

`OverviewDto` field `DestinationRealPnl` is the first `0`. `XauGross` / `XauNet` are the next two `0`s. `ShadowPnl` is `Sum(SourceVsShadowSlippage)` — **not** dest marks (`EfDashboardQueries.cs` L29).

Cannot treat “sending now” as a live-capital profit path. Cannot treat dashboard dest dollars as venue PnL. **Can** treat demo dest `35=D` as paper dest exposure (ledger open). That residual is **not** `SAFE_BY_ABSENCE` on demo account `5328266`. It is still **not** a measured +EV constructor and **not** live Pepperstone.

`GetStatusAsync` L67 reports `VenueReconciled: DemoDest` (true on this lab host) while the Evaluate hop still passes const `false`. Status honesty ≠ send license on live.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — PASS

SHADOW is a **source** state from `FromBaseline` (`quality >= 70 && risk < 40` after 3+ XAU trades). It is not dest marks, not dest realized, not a ticket.

| Object | What it is | Dest profit? |
|---|---|---|
| `TraderState.SHADOW` | Source score state | No |
| `ShadowCopyEngine.SimulateEntry` | Synthetic fill into `ShadowOrders` | No (model) |
| Overview `ShadowPnl` | Σ `SourceVsShadowSlippage` | No |
| Overview `DestinationRealPnl` | constructor `0` | Unmeasured, not SHADOW |
| Policy `IsTraderEligible` | SHADOW / LIVE_CANDIDATE / LIVE + n≥20 + source XAU net>0 + demo/contest group | Admission, not dest PnL |
| Demo dest `35=D` | Venue paper fill if ADMITTED + `DemoDest` | Dest **exposure**, not “SHADOW profit” |

`XauUsdOneToOneCopyPolicy` L81–85 rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET`. That names SHADOW as the **admission floor**, not dest PnL. Comment on the policy (`XauUsdOneToOneCopyPolicy.cs` L57–61) states copy does not wait until a ticket is profitable.

`CopyGroupFilter.IsDemoOrContest` admits `demo` / `contest` path segments and rejects real groups. SHADOW-on-demo is the **copyable source set**, not dest dollars.

`LiveCopyPage` Stat “SHADOW traders” is a count from `/api/copy/status`, not dest PnL.

`CanPromoteToLive => false` keeps SHADOW from becoming LIVE via the state machine. Demo dest send does **not** go through that promotion; it keys off `ADMITTED` roster seats. That is dest paper, still not “SHADOW profit”.

---

## 6. Live GET

Attempted `GET http://127.0.0.1:5000/api/health` (and page fetch). **Blocked** (loopback SSRF). Did not invent runtime JSON. File-side consequences if the API loaded `.env`:

- `/api/health` `realCopyEnabled` and `/api/settings` `REAL_COPY_EXECUTION_ENABLED` would be **true**
- `/api/overview` `destinationRealPnl` would still be **0** (constructor)
- `/api/copy/status` `realCopyArmed` would follow the same runtime bool; `summary` would be the DemoDest string if host/sender/account match `.env` demo triple (`demo-…`, `demo.…`, account `5328266`)

Those rows are **conditional on a live process**. This slot does not claim them as measured.

---

## 7. Risk to capital

| Venue | Risk | Proof |
|---|---|---|
| Live Pepperstone `1369850` | **NONE** (`SAFE_BY_ABSENCE` + hard refuse) | `CTraderFixCopyOpen` L37–42; session class cannot send `35=D`; Evaluate hop dead (`VenueReconciled` const false + persist `AllowFixSend=false` + no LIVE state) |
| Demo dest `5328266` (lab `.env` host/sender prefixes) | **RESIDUAL paper dest** | Hosted `ExecuteDemoCopyAsync` → `Build("D")`; ledger dest pos still open. Not live cash. Not measured dest PnL. |
| This slot | **NONE added** | No attach, no send, no flag flip |

Armed `REAL_COPY_EXECUTION_ENABLED=true` is **not** a live ticket. It is also **not** “forced false after logon”.

---

## 8. ALLOW / FORBID

```text
ALLOW:  treat CTraderFixSession as 35=A-only;
        treat CanPromoteToLive as a hard false;
        treat DestinationRealPnl constructor 0 as unmeasured dest;
        treat SHADOW as source state, not dest profit;
        keep live 1369850 off this hop.

FORBID: claim RealCopyEnabled is forced false after logon;
        cite CREDENTIALS / A015 / W500 “forced false” as HEAD;
        claim the product has no 35=D builder;
        claim NewOrderSingleImplemented is const false (HEAD => DemoDest);
        treat demo dest 35=D as SAFE_BY_ABSENCE;
        treat ShadowPnl or SHADOW count as dest profit;
        print FIX/MT5 passwords.
```

---

## 9. Files read (this slot)

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
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Application\Dashboard\DashboardModels.cs`
- `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs`
- `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs`
- `D:\Prop\src\Domain\Copy\CopyGroupFilter.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` (score persist)
- `D:\Prop\tests\Unit\BaselineScorerTests.cs`
- `D:\Prop\tests\Unit\RiskEngineTests.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\web\src\pages\OverviewPage.tsx`
- `D:\Prop\.env` (flag + host/account prefix only)
- `D:\Prop\data\demo_copy_ledger.json` (no secrets)

End of P500_VERIFY_12. Product source was not modified. No secrets printed. This slot did not send `35=D`.
