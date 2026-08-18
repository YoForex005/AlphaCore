# P500_VERIFY_6 — Adversarial four-file re-read (slot 6)

| Field | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | P500_VERIFY_6 |
| Slot | **6** |
| Role | Adversarial verifier. Read the four assigned files myself. Do not trust other agents. |
| Assigned | Confirm: (1) no `35=D` builder; (2) `CanPromoteToLive` is false; (3) `RealCopyEnabled` forced false after logon; (4) sending now cannot be the profit path; (5) SHADOW on demo is not destination profit. |
| Product source modified | **No** |
| Test source modified | **No** |
| Live attach / live GET this slot | **No.** `GET http://127.0.0.1:5000/api/health`, `/api/copy/status`, `/api/overview`, `/api/settings` were **SSRF-blocked** from this worker. Runtime flag is **file-proven**, not GET-proven. |
| Secret values printed | **None.** Quoted only the already-on-disk boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`. No passwords, no connection strings, no FIX tag 554. |
| Live `35=D` sent this slot | **No** |
| `REAL_COPY` flipped this slot | **No** |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_6.md` |

**Honesty rule (binding):** prove every claim from a file on this disk or a live GET. FAIL any claim that is not proven. Prior swarm notes (A014, A015, W500_*, P500_BOOK_*) are **not** evidence. Wanting profit does not create an edge. A TLS Logon (`35=A`) is not a fill. Source SHADOW dollars are not dest cash.

**Assigned files (full `read_file` this pass):**

| File | Lines read | SHA not taken |
|---|---:|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | **135 / 135** | ends L135 `}` |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | **212 / 212** | ends L212 `}` |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | **189 / 189** | ends L189 `}` |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | **70 / 70** | ends L70 `}` |

Adjacent (needed to prove / disprove claims 1 and 3; not a substitute for the four files): `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs` L39–42, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `CopyGroupFilter.cs`, `XauUsdOneToOneCopyPolicy.cs`, `ShadowCopyEngine.cs`, `apps/api/Program.cs`, `.env` L73 **boolean only**.

---

## Verdict

**FAIL** — claims (2), (4), (5) proven from files. Claim (1) is **PASS only if scoped to `CTraderFixSession`**; **FAIL as a product-wide “no 35=D builder” statement**. Claim (3) is **disproven**.

| # | Claim | Result | Why (file proof) |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** on assigned session; **FAIL** product-wide | `CTraderFixSession` outbound is only `(35, "A")` L96; `WriteAsync=1`; no `Build("D")`. Sibling `CTraderFixCopyOpen.Build("D")` L95 is **hosted** via `ExecuteDemoCopyAsync`. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive` is `=> false` (`BaselineScorer.cs` L211). `FromBaseline` never returns `LIVE` or `LIVE_CANDIDATE`. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | Hosted logon **reads** the flag (L70) and **never assigns** `_runtime.RealCopyEnabled = false`. DI L41 binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`. |
| 4 | sending now cannot be the profit path | **PASS** (as profit, not as “send is impossible”) | Persist hop hard-writes `AllowFixSend=false` L324; `VenueReconciled=const false`; dest DTO constructor `0`; scorer cannot LIVE. Demo hopper can still `35=D` — that is dest activity, **not** a measured dest-profit book. |
| 5 | SHADOW on demo is not destination profit | **PASS** | `FromBaseline` max is source `SHADOW` from reconstructed source PnL. `ShadowCopyEngine` is `SimulateEntry`. Overview `DestinationRealPnl=0`. Trader-row `ShadowPnl` literal `0`. `CopyGroupFilter` requires demo/contest. |

One-line:

```text
Slot 6 FAIL. CTraderFixSession is 35=A only (PASS scoped). CanPromoteToLive=false. RealCopyEnabled is NOT forced false after logon (DI binds .env true). Sending is not a dest-profit path (dest DTO $0; persist AllowFixSend=false). SHADOW on demo is source paper, not dest cash. Live 1369850 NONE; demo dest 5328266 not SAFE_BY_ABSENCE if hopper runs.
```

Risk to capital: **NONE** on live Pepperstone `1369850` (`CTraderFixSession` cannot send; `CTraderFixCopyOpen` refuses that account). **Not** `SAFE_BY_ABSENCE` on demo dest `5328266` if the 20 s hosted tick is running. This slot sent **0**.

---

## 1. No `35=D` builder — PASS (`CTraderFixSession`) / FAIL (product)

Path: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`  
Read: **135 / 135**.

This type is a one-shot TLS Logon probe. Two types only: `CTraderFixSessionResult` + static `CTraderFixSession`. There is no order builder, no heartbeat loop, no quote subscribe, no sequence store.

### 1.1 Token census (this file only)

| Pattern | Hits |
|---|---:|
| Literal `35=D` | **0** |
| `(35, "D")` / `Build("D")` | **0** |
| Identifier `NewOrderSingle` | **0** |
| `OrderQty` / tag 38 / `ClOrdID` | **0** |
| `(35, "A")` | **1** (`BuildLogon` L96) |
| `ssl.WriteAsync` | **1** (L49) — bytes of that Logon |
| `Extract(..., "35")` | **1** — **inbound** reply only (L55) |
| Socket kept for a later `35=D` | **No** — `using TcpClient` / `await using SslStream` dispose on every return |

Outbound builder that exists:

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

`TryLogonAsync` writes that Logon once, reads one reply, returns. If inbound `35=A`, `LoggedOn=true`. Otherwise `LoggedOn=false`. Exceptions become `Disconnected`. The password field is assembled; this report does **not** print it.

**Scoped claim 1 = PASS.** I can prove this file has no `35=D` builder.

**Product-wide claim 1 = FAIL.** I cannot prove “no `35=D` builder” for the tree. Same folder, same assembly:

```95:95:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
            await Write(ssl, Build("D", sender, target, seq, extra.ToArray()), timeout.Token);
```

`Build(string type, ...)` L142–156 prefixes `(35, type)`. Callers also exist at `CTraderFixDemoMatrix` L93 and `CTraderFixDemoTestTrade` L139 / L163 / L197 (**5** `Build("D")` sites). `CopyTradingHostedService` L30 calls `ExecuteDemoCopyAsync`, which calls `CTraderFixCopyOpen.SendAsync` (close L528, open L566). CopyOpen L37–41 refuses live identity `1369850` and requires `demo-` host + `demo.` sender. That is a **demo dest sender**, not absence of a builder.

LiveCopyPage empty copy **admits** the hopper:

```56:58:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      {intents.length === 0 && (
        <p className="text-gray-500 text-sm">No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.</p>
      )}
```

A015 / older “no `35=D` assembler” pins are **stale** on this disk.

---

## 2. `CanPromoteToLive` is false — PASS

Path: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` **212 / 212**.

```187:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
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

- `CanPromoteToLive` ignores `current` and is a constant `false`. Grep of `*.cs` writers: this line + unit test + a `_tmp` console. **0** product callers that treat it as true.
- `FromBaseline` reachable states: `INSUFFICIENT_DATA`, `RISK_BLOCKED`, `SHADOW`, `WATCH`, `EARLY_SCORE`. **Never** `LIVE`, **never** `LIVE_CANDIDATE`.
- `AfterHighEarlyScore()` is `SHADOW`, not `LIVE`.
- `BaselineScorer.Score` L162 uses `FromBaseline` only.
- Unit test `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` asserts `SuggestedState == SHADOW` and `CanPromoteToLive == false`.
- `LiveCopyPage` shows a `LIVE traders` chip from API count; the scorer cannot populate that state. `CopyTradingService.BuildBlockers` (non-demo dest) still says `0 traders in LIVE (promotion is manual; trade #3 cannot auto-LIVE)`.
- `TraderDetailPage.tsx` L44: “First 3 completed XAUUSD trades unlock EARLY_SCORE / SHADOW only. Live promotion is not automatic.”

`TraderState` enum still *has* `LIVE=5` (`TraderState.cs` L10). That is a label, not a promotion path. Claim 2 is about `CanPromoteToLive`, which is false.

---

## 3. `RealCopyEnabled` forced false after logon — FAIL

This is the claim that dies on HEAD.

### 3.1 Hosted logon does not write the flag

`D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` L60–70 (full file 112 lines):

```60:70:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.Quote.LoggedOn = quote.LoggedOn;
        _runtime.Quote.Status = quote.Status;
        _runtime.Quote.LastError = quote.LastError;
        _runtime.Quote.UpdatedAt = DateTimeOffset.UtcNow;
        _runtime.Trade.LoggedOn = trade.LoggedOn;
        _runtime.Trade.Status = trade.Status;
        _runtime.Trade.LastError = trade.LastError;
        _runtime.Trade.UpdatedAt = DateTimeOffset.UtcNow;
        _log.LogInformation(
            "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
            quote.LoggedOn, trade.LoggedOn, _runtime.RealCopyEnabled, account);
```

Grep of this file for `RealCopyEnabled` = **1 hit** (the log interpolation). There is **no** `_runtime.RealCopyEnabled = false`. A015 L92 / L197 / L224 (“forces `_runtime.RealCopyEnabled = false` after logon”) is **stale**. The log string still says “NewOrderSingle still unimplemented” while the copy host **implements** a demo sender. Stale log, not a pin.

### 3.2 DI binds the env key; lab env is `true`

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

API `Program.cs` L10 `EnvFile.FindAndLoad()` then L13 `AddEnvironmentVariables()`. Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no other `.env` keys quoted).

`CTraderFixOptions.RealCopyExecutionEnabled` still **defaults false** (`CTraderFixOptions.cs` L35). That POCO is **not** what `LiveRuntimeStatus` reads. Appsettings `FeatureFlags.LiveCopyEnabled=false` is also **unbound** to `LiveRuntimeStatus`.

### 3.3 Live GET not available this slot

`GET http://127.0.0.1:5000/api/health` (and `/api/settings`, `/api/copy/status`, `/api/overview`) **failed** (`SSRF blocked` on loopback). I therefore **cannot** prove the *running process* flag from a live GET. I **can** prove the process law: if the API loaded this `.env`, `runtime.RealCopyEnabled` is **true** after construct, and logon does not flip it.

Claim 3 as written (“forced false after logon”) is **false on disk**. FAIL.

`RiskEngine` does **not** rescue the claim. When `RealExecutionEnabled == false` it comments and **continues** (L90–93); `AllowFixSend` is computed later as `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Persist hop then **overwrites** `AllowFixSend = false` (CopyTradingService L324) regardless of the engine. That is a persist pin, not a logon pin of `RealCopyEnabled`.

`LiveCopyPage` L13 renders `status?.realCopyArmed ? 'YES' : 'NO'`. If the API is up with this `.env`, the chip can read **YES**. This slot did not GET it.

---

## 4. Sending now cannot be the profit path — PASS (profit, not impossibility)

What I can prove:

1. **Assigned FIX hop cannot send an order.** §1. One `35=A`, sockets disposed.
2. **Scorer cannot create a LIVE trader.** §2. Persist hop live-send branch requires `score.CurrentState == TraderState.LIVE` **and** `decision.AllowFixSend` **and** `NewOrderSingleImplemented` **and** `VenueReconciled` (`CopyTradingService` L330). `VenueReconciled` is `const false` (L20). Persist then writes `AllowFixSend = false` (L324) even if `Evaluate` returned true. Branch is dead; intents go `SHADOW_ONLY`.
3. **Dest book is not measured.** `OverviewDto.DestinationRealPnl` is constructor literal `0` (`EfDashboardQueries` L44; DTO field order in `DashboardModels.cs` L16). `XauGross=0`, `XauNet=0` (L45–46). Trader-row `ShadowPnl` is literal `0` (L118). Overview `ShadowPnl` is `Sum(SourceVsShadowSlippage)` (L29) — a paper slippage sum, not dest cash.
4. **RiskEngine is not an edge.** Caps fire after dest (or a mis-fed source ticket) is already lost. `DailyExecutionPnl` is hard-`0` at Evaluate L307. `ExecuteDemoCopyAsync` **does not call** `Evaluate` at all. Unit test `Real_flag_false_never_allows_fix_send` only proves the engine AND, not dest EV.
5. **UI does not show dest profit.** `LiveCopyPage` chips: REAL_COPY armed, SHADOW/LIVE counts, live sends, intents, shadow fills, QUOTE/TRADE. No dest PnL field. Amber copy: “Live send blockers (Pepperstone cannot be filled).”

What I **cannot** prove, and will not claim: “no process can emit `35=D`.” The hosted demo hopper **can**. That send is **not** a profit path:

- Dest is demo FIX (host `demo-`, sender `demo.`, account ≠ `1369850`). Demo cash is not live Pepperstone profit.
- Hopper bypasses `RiskEngine.Evaluate`, bid/ask, and the 15 s signal clock. Blind send ≠ edge.
- `MaxAutoLots=0.05` **selects** tiny source tickets and sends them **1:1** (`AllocationFactor=1m`). That is a skip + full-size scalp, not a haircut.
- Policy `XauNetPnl` is **source** reconstructed PnL. Source dollars on a challenge demo book are not dest-net after spread.
- Dashboard dest remains constructor `0` even if a dest fill exists on the file ledger. Unmarked dest is not a profit proof.

So: **sending now cannot be the profit path.** Wanting a fill does not mint dest EV. PASS as stated. FAIL if someone rewrites the claim as “send is impossible.”

---

## 5. SHADOW on demo is not destination profit — PASS

`FromBaseline` awards `SHADOW` when `quality >= 70 && risk < 40` after ≥3 completed source XAU trades. Inputs are `ReconstructedTradeResult.NetRealizedPnl` (source MT5), not dest fills.

```201:201:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
            return TraderState.SHADOW;
```

`CopyGroupFilter.IsDemoOrContest` requires a `demo` or `contest` path segment. `XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects anything else with `NOT_DEMO_OR_CONTEST_GROUP` (L105–108). Roster `AUTO_ADMIT` uses the same filter. HEAD **selects** challenge/demo books. That is source-group polarity, not dest PnL.

Shadow path:

- Hopper set `{SHADOW, LIVE_CANDIDATE, LIVE}` (`CopyTradingService` L202) then policy + roster `ADMITTED`.
- `RiskEngine.Evaluate` still runs; persist `AllowFixSend=false`.
- On `Approve`, `ShadowCopyEngine.SimulateEntry` writes a `ShadowOrder` (paper ask/bid + 0.05 point delay model). No FIX. No dest position.
- Close intents are stamped `SHADOW_ONLY` with **no** shadow exit fill.

`LiveCopyPage` L14 shows `SHADOW traders` as a **count**. That is a state census, not dest cash. `ShadowPortfolioPage` still says “Live NewOrderSingle remains disabled” and that shadow orders exist only after a SHADOW `CopyIntent` — paper, not dest.

Therefore: a demo login in `SHADOW` is a **source-quality label**. It is not destination profit. Dest DTO is `0`. PASS.

---

## 6. `RiskEngine` / `LiveCopyPage` residuals (not extra claims)

### 6.1 `RiskEngine` (189 / 189)

- 19 `return Reject(` sites. `AllowFixSend` is **false** on every reject (L180–188).
- Approve path `AllowFixSend` requires `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150).
- Copy hop feeds `Reconciled = VenueReconciled` (`const false`) → increasing actions die with `VENUE_NOT_RECONCILED` (L84–85) **before** the approve path. That is why persist-hop shadow opens usually have no fill (quote path never reached) unless a quote row already exists **and** the action is not treated as increasing — opens are increasing.
- `RealExecutionEnabled == false` does **not** reject (empty comment L90–93). Engine honesty: flag-off still “approves” for paper; send AND is false.
- Grep of this file: `35=D` = **0**, `NewOrderSingle` = **0**, `TraderState` = **0**, `RISK_BLOCKED` as a trader state = **0**. The engine does not read SHADOW/LIVE. It cannot be the promotion gate and cannot be the dest-profit ledger.

### 6.2 `LiveCopyPage` (70 / 70)

Display-only. Hooks `useCopyStatus` / `useCopyIntents` → `GET /api/copy/status` and `/api/copy/intents`. No POST. No “arm REAL_COPY” button. No dest PnL. The page will show `REAL_COPY armed = YES` if the API bound `.env=true`. That chip is **not** a send license and **not** dest profit.

---

## 7. What this slot did **not** prove

| Item | Status |
|---|---|
| Running process `realCopyEnabled` via GET | **Unproven** (loopback SSRF-blocked) |
| Manager census 18 / 8460 | **Not re-attached.** Cited only as prior pin if others need it; **not** used as proof here. |
| Dest ledger fill 305750 / 21250421 | **Not re-read** this slot. Not required for the five claims. |
| Copy-all 8463 source tail −$241,580 | Prior pin; **not** re-measured here. Honesty still: copying all logins is not an edge. |
| This slot sent `35=D` | **No.** |

---

## 8. Stale pins this re-read kills

| Pin | Status |
|---|---|
| A015 / CREDENTIALS “hosted service sets `_runtime.RealCopyEnabled = false`” | **STALE.** Log line only. |
| A015 / DI `RealCopyEnabled = false` with “do not arm” comment | **STALE.** DI binds env. |
| BOOK / W500 “product `35=D` writers = 0” / `NewOrderSingleImplemented const false` | **STALE.** `NOS => DemoDest` L50; hosted `Build("D")`. |
| Persist `AllowFixSend=false` at L306 / L211 | **STALE.** HEAD is **L324**. |
| “Logon is a send license if REAL_COPY is true” | **False.** Assigned hop still cannot send. Demo hopper is a **separate** path. |

---

## 9. Binding close

I read all four assigned files in full. I fail claim (3) because I can **disprove** it from `CTraderFixLogonHostedService` + DI + `.env` L73 boolean. I pass claim (1) only as “`CTraderFixSession` has no `35=D` builder.” I will not certify the product has no builder. Claims (2), (4), (5) hold from the files. Sending, SHADOW, and demo source dollars are not destination profit. Live capital on `1369850` is **NONE** this slot (`SAFE_BY_ABSENCE` on the assigned hop + CopyOpen refuse). Demo dest is the residual. Do not enable live send. Do not treat env `true` as a ticket.
