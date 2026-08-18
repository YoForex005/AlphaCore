# P500_VERIFY_69 — Adversarial four-file verify (slot 69)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_69.md` |
| Agent | P500_VERIFY_69 (adversarial verifier, slot **69**) |
| Date | 2026-08-18 |
| Assigned | Read `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`. Confirm: (1) no `35=D` builder, (2) `CanPromoteToLive` is false, (3) `RealCopyEnabled` forced false after logon, (4) sending now cannot be the profit path, (5) SHADOW on demo is not destination profit. **FAIL any claim not proven from a file or live GET.** |
| SUT (full read this slot) | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135), `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212), `D:\Prop\src\Domain\Risk\RiskEngine.cs` (190/190), `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (71/71) |
| Hop (not assigned; required to test 1/3/4/5) | `CTraderFixLogonHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `apps/api/Program.cs`, `EnvFile.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `DealIngestionService.cs`, `EfTradingStore.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `CopyGroupFilter.cs`, `CopyLifecycle.cs`, `ShadowCopyEngine.cs`, `DemoCopyLedger.cs`, `data/demo_copy_ledger.json`, `apps/web/src/api/hooks.ts`, `apps/api/Properties/launchSettings.json`, `CTraderFixOptions.cs`, `apps/fix-worker/Worker.cs`, `BaselineScorerTests.cs`, `RiskEngineTests.cs`, `.env` flag + public host/account prefix only |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Live `35=D` sent this slot | **No** |
| `REAL_COPY` flipped this slot | **No** |
| Secrets printed | **None** (boolean `REAL_COPY_EXECUTION_ENABLED=true`, public host prefix `demo-…`, public dest ids `5328266` / `1369850` only; tag 554 never dumped) |
| Live GET this slot | `GET http://127.0.0.1:5000/api/health`, `GET http://127.0.0.1:5000/api/copy/status`, `GET http://localhost:5000/api/settings` → **SSRF private-IP deny**. **No** live body. Runtime `realCopyEnabled` / dest book **not** re-probed. File proof only. |
| Method | Full `read_file` of the four assigned files. Independent grep of `Build("D")` / `(35, "D")` / `RealCopyEnabled =` / `CanPromoteToLive` / `CurrentState =` / `DestinationRealPnl`. Did **not** trust sibling P500 / W500 / CREDENTIALS / README prose. |

Classification: `SESSION_NO_35D` / `PRODUCT_HAS_35D` / `CAN_PROMOTE_FALSE` / `REALCOPY_NOT_REPINNED` / `DEMO_DEST_SEND_EXISTS` / `DEST_PNL_CONSTRUCTOR_0` / **FAIL**.

---

## 0. Verdict (binding)

**FAIL.** Instruction: FAIL any claim that cannot be proven from a file this slot or a live GET. Only claim 2 is fully proven as written. Claim 1 is session-only. Claim 3 is **disproven**. Claims 4–5 cannot be proven (demo dest `35=D` is hosted; SHADOW is the dest ADMIT floor). Live GET absent.

| # | Claim | Verdict | Why |
|---|---|---|---|
| 1 | no `35=D` builder | **FAIL** as written | Assigned `CTraderFixSession` outbound MsgType is only `(35, "A")` (**PASS_SESSION**). Unscoped “no builder” is **false**: sibling `CTraderFixCopyOpen.Build("D")` is on the hosted 20 s hop; `CTraderFixDemoTestTrade` / `CTraderFixDemoMatrix` also assemble `D`. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(TraderState current) => false` (`BaselineScorer.cs` L211). Parameter discarded. `FromBaseline` never returns `LIVE` / `LIVE_CANDIDATE`. Product `src` callers: **0**. Unit test locks SHADOW-not-LIVE. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** (disproven) | No post-logon assignment exists. DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`. Hosted logon **logs** the bool and does not overwrite it. Product `RealCopyEnabled =` writers: **1** (DI bind). Live GET blocked — cannot measure the running process; file proof already **disproves** “forced false after logon”. |
| 4 | sending now cannot be the profit path | **FAIL** | Cannot prove. Hosted `ExecuteDemoCopyAsync` **sends now** on DemoDest (`CTraderFixCopyOpen.Build("D")`, **no** `RiskEngine.Evaluate`, **no** `RealCopyEnabled` check). On-disk ledger has an **open** dest fill. `DestinationRealPnl` constructor `0` is **not** dest-account P&L. Live GET of dest book blocked. Live `1369850` is still refused. |
| 5 | SHADOW on demo is not destination profit | **FAIL** as dest-safety | Paper SHADOW (`SimulateEntry` / Σ `SourceVsShadowSlippage` / source `NetRealizedPnl`) is **not** dest cash (**PASS_PAPER**). Residual is load-bearing: policy eligibility floor **is** SHADOW; roster `AUTO_ADMIT`s it; `ExecuteDemoCopyAsync` ignores `CurrentState` / `LIVE`. SHADOW-on-demo **can** become dest `35=D`. Cannot prove dest mark is $0 without live GET. |

**One-line:** session is `35=A` only; promotion is a hard false; the flag is **not** forced off after logon (`.env` true + DI bind); current send **can** be dest paper profit on demo; SHADOW is the dest admit class, not a dest-PnL lock.

Priors that say hosted logon sets `_runtime.RealCopyEnabled = false` (`A015_enable_copy_gates.md`, `CREDENTIALS_AND_COPY_STATUS.md` “false (forced)”, several W500 “forced false”) are **STALE vs HEAD**.

Priors that say product `NewOrderSingleImplemented` is `const false` / “no `35=D` assembler” / “Live NewOrderSingle remains disabled” as a product-wide fact (`W500_VERIFY_8`, `A015`, `ShadowPortfolioPage.tsx` L7) are **STALE vs HEAD** (`=> DemoDest`; `CTraderFixCopyOpen`).

Priors that say `EfDashboardQueries` hard-codes `RealCopyEnabled=false` (`C37`, `D03`) are **STALE vs HEAD** (`_runtime.RealCopyEnabled` at L52).

README L28 (“Real NewOrderSingle is **off** (`REAL_COPY_EXECUTION_ENABLED=false`)”) is **STALE vs lab `.env` L73**.

---

## 1. Claim 1 — no `35=D` builder — FAIL as written (PASS_SESSION only)

### 1.1 Assigned session file (full read, 135/135)

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`

Public API is **only** `TryLogonAsync`. The sole outbound builder is `BuildLogon`:

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

| Fact | Measured this pass |
|---|---|
| Physical lines | **135 / 135** (ends L135 `}`) |
| Literal `35=D` / `(35, "D")` / `Build("D")` | **0** |
| `NewOrderSingle` identifier | **0** |
| Outbound tag 35 actually built | **`"A"` only** |
| `WriteAsync` sites | **1** — the logon bytes (`L47–50`). Socket disposed by `using`. |
| Inbound `Extract(reply, "35")` (`L55`) | **read**, not a builder |
| Error text `"Logon rejected 35={msgType}"` (`L73`) | inbound echo |
| `Assemble` callers | **1** (`BuildLogon`) |

`CTraderFixSession` cannot emit NewOrderSingle. A TLS Logon `35=A` is not a ticket.

### 1.2 Product-wide (adjacent; required because the claim is unscoped)

Grep of product `*.cs` for `Build("D")` / `(35, "D")`:

| File | Builder | Hosted on API boot? |
|---|---|---|
| `CTraderFixCopyOpen.cs` L95 + generic `Build(string type, …)` L142–156 | `Build("D", sender, target, seq, extra)` NewOrderSingle (open + close via tag 721) | **Yes** — `CopyTradingService.ExecuteDemoCopyAsync` L528 + L566, called from `CopyTradingHostedService` 20 s tick L30 |
| `CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | `Build("D", …)` flatten / open / close | Tool / session helper (`tools/DemoFixTestTrade`) |
| `CTraderFixDemoMatrix.cs` L93 | `Build("D", …)` | Tool / session helper |

`CTraderFixCopyOpen` refuse gate (`L37–42`): dest must be `host` prefix `demo-` **and** `sender` prefix `demo.` **and** `account != "1369850"`. That is a **gate**, not absence of a builder.

`CTraderFixLogonHostedService` L69 log line still says “NewOrderSingle still unimplemented.” That log is **STALE vs HEAD** (`CTraderFixCopyOpen` exists and is called).

**Claim 1 as written about `CTraderFixSession.cs`: PASS_SESSION. Claim 1 as a product-wide sentence: FAIL.** Slot fails this row because the assigned sentence is unscoped and a sibling builder is on the hosted hop.

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

Proof this pass:

- `CanPromoteToLive` ignores `current` and is a constant `false`. Grep of `*.cs`: **one** product definition (`BaselineScorer.cs` L211), **one** unit pin (`BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` L25–26), **one** reports tmp harness. **Zero** `src` callers. A method with no callers that returns `false` cannot promote anyone.
- `FromBaseline` ceiling is `SHADOW`. It never returns `LIVE` or `LIVE_CANDIDATE`. `AfterHighEarlyScore()` is also `SHADOW`.
- Persist path: `DealIngestionService` L140 `CurrentState = score.SuggestedState`. `EfTradingStore.UpsertScoreAsync` L232 copies `score.CurrentState`. No other product writer of `CurrentState =` exists outside counts / tmp harnesses.
- Unit pin: three disciplined winners → `SHADOW` and `CanPromoteToLive(...).Should().BeFalse()`.

`TraderState.LIVE` exists on the enum (`TraderState.cs` L10) and is **counted** / **branched** in copy (`CopyTradingService` L58, L202, L330). That is not a promotion function. No product assignment creates LIVE from score.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL (disproven)

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

API loads that file (`apps/api/Program.cs` L10 `EnvFile.FindAndLoad()` + L13 `AddEnvironmentVariables()`). `EnvFile.FindAndLoad` (`src/Mt5/Env/EnvFile.cs` L5–20) walks cwd / parents and hard-path `D:\Prop\.env`, then `Environment.SetEnvironmentVariable`.

**Hosted logon does not re-pin.** After both `TryLogonAsync` calls it copies QUOTE/TRADE status and **logs** the existing flag:

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

There is no `_runtime.RealCopyEnabled = false`. Grep of product `*.cs` for `RealCopyEnabled =` assignments: **only** the DI bind above.

`CTraderFixOptions.RealCopyExecutionEnabled` defaults `false` (`CTraderFixOptions.cs` L35). That POCO is **not** what DI writes onto `LiveRuntimeStatus`. Unused default ≠ forced-false-after-logon.

`apps/fix-worker/Worker.cs` L21 reads `CTrader:RealCopyExecutionEnabled` (different key, default false) and still **does not** write `LiveRuntimeStatus`. Irrelevant to the claim.

`RiskEngine` L90–93 is an empty comment when `RealExecutionEnabled == false`. It does not mutate runtime. Later `allowSend` (`L147–150`) ANDs the **request** flag; it does not force the process flag false.

`LiveRuntimeStatus.Snapshot()` (`L42–44`) even documents the armed branch: “REAL_COPY armed. NewOrderSingle still unimplemented…”. The string is stale on NOS, but it proves the runtime bool is allowed to be true.

Live GET that would prove the running process (`/api/health` `realCopyEnabled`, `/api/settings` feature flag, `/api/copy/status` `realCopyArmed`) was **not** obtainable this slot (loopback SSRF). File proof is enough to **disprove** “forced false after logon”: the only writer can set `true`, and logon does not overwrite.

`reports/CREDENTIALS_AND_COPY_STATUS.md` “**false** (forced)” is **STALE**. Do not cite it as HEAD.

---

## 4. Claim 4 — sending now cannot be the profit path — FAIL

Cannot prove the negative. What “sending now” actually is, measured this pass:

### 4.1 Risk-gated hop (cannot send)

`CopyTradingService.GenerateShadowIntentsAsync`:

- Evaluate at L291–315 with `RealExecutionEnabled = _runtime.RealCopyEnabled`, `Reconciled = VenueReconciled`.
- `VenueReconciled` is `const false` (`L20`).
- Persist **always** `AllowFixSend = false` (`L324`) — even if `decision.AllowFixSend` is true.
- Live-send branch L330 requires `decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled`. `VenueReconciled` const false + `FromBaseline` never LIVE ⇒ branch is **dead**.
- Intents go `SHADOW_ONLY` + `ShadowCopyEngine.SimulateEntry` (paper).

`RiskEngine` cannot be “the profit path” on this hop:

```147:171:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
        // …
                AllowFixSend = allowSend
```

Unit `Real_flag_false_never_allows_fix_send` pins `AllowFixSend=false` when the request flag is false. The empty `if (RealExecutionEnabled == false)` block (`L90–93`) does **not** reject; it still cannot set `AllowFixSend` true unless the request flag, kill switch, recon, and venue all pass. The persist hop then **overwrites** `AllowFixSend` to false anyway.

### 4.2 Demo dest hop (sends now; IS the copy-for-edge path)

`CopyTradingHostedService` every 20 s (`L28–30`): `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**.

`ExecuteDemoCopyAsync` (`CopyTradingService.cs` L483–605):

- Gate is **only** `DemoDest` (host `demo-` AND sender `demo.` AND account ≠ `1369850`). Lab `.env` L49/L50/L64 match: host `demo-us-eqx-01.p.c-trader.com`, account `5328266`, trade sender `demo.pepperstone.5328266`.
- **Does not** read `_runtime.RealCopyEnabled`.
- **Does not** call `RiskEngine.Evaluate`.
- **Does not** require `TraderState.LIVE` or `CanPromoteToLive`.
- Seeds ledger with dest fill `305750` / `21250421` → dest pos `237339770` @ `4390.2` if missing (`L500–512`).
- Closes dest when source completes (`L517–540` → `CTraderFixCopyOpen.SendAsync` with `destPositionId`).
- Opens dest for **ADMITTED** roster seats with open XAUUSD ≤ `MaxAutoLots` 0.05 (`L542–598` → `CTraderFixCopyOpen.SendAsync`).

`CTraderFixCopyOpen.SendAsync` L95: `Build("D", …)` NewOrderSingle. On fill, intent status becomes `DEMO_SENT` (`L593`).

`NewOrderSingleImplemented => DemoDest` (`L50`). On this lab triple that is **true**. `GetStatusAsync` L67 reports `VenueReconciled: DemoDest` (true on this lab host) while the Evaluate hop still passes const `false`. Status honesty ≠ send license on live. Status **does** advertise dest auto-copy:

```76:78:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
            Summary: DemoDest
                ? "Demo dest auto-copy ON. Eligible demo/contest opens send on the 20s tick; dest closes when the MT5 source closes. Live 1369850 is never used."
                : "Copy pipeline ON. Shadow intents only. Live Pepperstone will not receive NewOrderSingle.");
```

`XauUsdOneToOneCopyPolicy` L57–61 states the product intent in code: select traders with a measured XAUUSD edge, then copy next XAUUSD events 1:1. That **is** the profit path. `ExecuteDemoCopyAsync` implements it on demo dest **now**.

On-disk ledger (`D:\Prop\data\demo_copy_ledger.json`): dest pos `237339770`, `DestClosed=false`, `Lots=0.01`, `DestFillPrice=4390.2`. An open dest fill can have venue mark-to-market. This slot did not query the dest book (live GET blocked).

### 4.3 Live dest

`CTraderFixCopyOpen` L37–42 refuses `account == "1369850"` or non-`demo-` host / non-`demo.` sender **before** any `35=D`. `CTraderFixSession` cannot send `35=D`. Live Pepperstone is `SAFE_BY_ABSENCE` + hard refuse.

### 4.4 Measured dest profit is not computed

```33:52:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        return new OverviewDto(
            …
            shadowPnl,
            0,
            0,
            0,
            …
            _runtime.RealCopyEnabled);
```

`OverviewDto.DestinationRealPnl` (`DashboardModels.cs` L16) is the first `0`. `XauGross` / `XauNet` are the next two `0`s. `ShadowPnl` is `Sum(SourceVsShadowSlippage)` (`EfDashboardQueries.cs` L29) — **not** dest marks.

Constructor `0` is **unmeasured**, not proof that dest P&L is zero. Cannot treat dashboard dest dollars as venue PnL. **Can** treat demo dest `35=D` as paper dest exposure (ledger open). That residual is **not** `SAFE_BY_ABSENCE` on demo account `5328266`. It is still **not** live Pepperstone.

### 4.5 UI (assigned `LiveCopyPage.tsx`, 71/71)

```13:28:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
        <Stat label="REAL_COPY armed" value={status?.realCopyArmed ? 'YES' : 'NO'} hot={status?.realCopyArmed} />
        …
        <Stat label="Live sends" value={status?.liveSends ?? 0} />
        …
          <div className="font-medium mb-1">Live send blockers (Pepperstone cannot be filled)</div>
```

Empty-state L57 admits the hop: “Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position.” The page is a **display**; it does not send. It also does not hide dest send.

**Claim 4 as “live capital cannot be the profit path”: residual PASS on `1369850`. Claim 4 as written: FAIL.** Sending now **can** be the dest-paper profit path.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — FAIL as dest-safety

### 5.1 What SHADOW is (PASS_PAPER)

SHADOW is a **source** state from `FromBaseline` (`quality >= 70 && risk < 40` after 3+ XAU trades). It is not dest marks, not dest realized, not a ticket by itself.

| Object | What it is | Dest profit? |
|---|---|---|
| `TraderState.SHADOW` | Source score state | No |
| `ShadowCopyEngine.SimulateEntry` | Synthetic fill into `ShadowOrders` | No (model) |
| Overview `ShadowPnl` | Σ `SourceVsShadowSlippage` | No |
| Overview `DestinationRealPnl` | constructor `0` | Unmeasured, not SHADOW |
| `PersistDemoShadowAsync` | only if `state == SHADOW`; status `SHADOW_ONLY`; simulate | No |

`LiveCopyPage` Stat “SHADOW traders” is a count from `/api/copy/status`, not dest PnL.

### 5.2 Residual that fails dest-safety (load-bearing)

`XauUsdOneToOneCopyPolicy.IsTraderEligible` L81–85 rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET`. That **names SHADOW as the admission floor**. SHADOW / LIVE_CANDIDATE / LIVE + n≥20 + source XAU net>0 + demo/contest group (`CopyGroupFilter.IsDemoOrContest`) are eligible.

`CopyRosterEngine.Decide` L72–80 `AUTO_ADMIT`s when `IsTraderEligible`. `FromBaseline` never produces LIVE, so the only auto-admissible scored state is **SHADOW**.

`ExecuteDemoCopyAsync` iterates `Status == "ADMITTED"` roster seats (`L542–544`) and sends dest `35=D` **without reading `CurrentState`**. `CopyLifecycle.ShouldOpenDest(sourceStillOpen, destAlreadyFilled)` is a boolean pair only.

Therefore: **SHADOW-on-demo + 20 XAU + source book > 0 + demo/contest group → ADMITTED → dest `35=D` on the 20 s tick.** That dest fill has venue P&L even if our DTO writes `0`.

`CanPromoteToLive => false` keeps SHADOW from becoming LIVE via the state machine. Demo dest send does **not** go through that promotion; it keys off `ADMITTED`. Promotion-false is **not** a dest-send lock.

Paper SHADOW is not dest cash. SHADOW-on-demo **can** become dest exposure. Claim as dest-safety: **FAIL**. Live GET of dest PnL: **absent**.

---

## 6. Live GET

Attempted:

- `GET http://127.0.0.1:5000/api/health`
- `GET http://127.0.0.1:5000/api/copy/status`
- `GET http://localhost:5000/api/settings`

All **blocked** (SSRF private-IP deny). Did not invent runtime JSON. File-side consequences **if** the API loaded `.env` (not claimed as measured):

- `/api/health` `realCopyEnabled` and `/api/settings` `REAL_COPY_EXECUTION_ENABLED` would be **true**
- `/api/overview` `destinationRealPnl` would still be **0** (constructor)
- `/api/copy/status` `realCopyArmed` would follow the same runtime bool; `summary` would be the DemoDest string (host/sender/account match `.env` demo triple); `newOrderSingleImplemented` would be **true**
- Launch profile binds API `:5000` (`apps/api/Properties/launchSettings.json`)

Those rows are **conditional on a live process**. This slot does not claim them as measured.

---

## 7. Risk to capital

| Venue | Risk | Proof |
|---|---|---|
| Live Pepperstone `1369850` | **NONE** (`SAFE_BY_ABSENCE` + hard refuse) | `CTraderFixCopyOpen` L37–42; session class cannot send `35=D`; Evaluate hop dead (`VenueReconciled` const false + persist `AllowFixSend=false` + no LIVE state) |
| Demo dest `5328266` (lab `.env` host/sender prefixes) | **RESIDUAL paper dest** | Hosted `ExecuteDemoCopyAsync` → `Build("D")`; ledger dest pos `237339770` still open (`DestClosed=false`, 0.01 lot). Not live cash. Not measured dest PnL. |
| This slot | **NONE added** | No attach, no send, no flag flip |

Armed `REAL_COPY_EXECUTION_ENABLED=true` is **not** a live ticket. It is also **not** “forced false after logon”. Demo dest send does **not** consult the flag.

---

## 8. ALLOW / FORBID

```text
ALLOW:  treat CTraderFixSession as 35=A-only;
        treat CanPromoteToLive as a hard false;
        treat DestinationRealPnl constructor 0 as unmeasured dest;
        treat paper SHADOW (SimulateEntry / Σ slippage) as not dest cash;
        keep live 1369850 off this hop.

FORBID: claim RealCopyEnabled is forced false after logon;
        cite CREDENTIALS / A015 / W500 “forced false” as HEAD;
        claim the product has no 35=D builder;
        claim NewOrderSingleImplemented is const false (HEAD => DemoDest);
        treat demo dest 35=D as SAFE_BY_ABSENCE;
        treat ShadowPnl or SHADOW count as dest profit;
        treat SHADOW + ADMITTED as dest-send-proof-absent;
        claim sending now cannot be the profit path;
        print FIX/MT5 passwords.
```

---

## 9. Files read (this slot)

- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135)
- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212)
- `D:\Prop\src\Domain\Risk\RiskEngine.cs` (190/190)
- `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (71/71)
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` (L120–205)
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Copy\DemoCopyLedger.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (L215–318)
- `D:\Prop\src\Application\Dashboard\DashboardModels.cs`
- `D:\Prop\src\Application\Copy\CopyTradingModels.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` (L120–145)
- `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs`
- `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs`
- `D:\Prop\src\Domain\Copy\CopyGroupFilter.cs`
- `D:\Prop\src\Domain\Copy\CopyLifecycle.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Domain\Enums\TraderState.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\Properties\launchSettings.json`
- `D:\Prop\apps\web\src\api\hooks.ts`
- `D:\Prop\apps\web\src\pages\ShadowPortfolioPage.tsx`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\tests\Unit\BaselineScorerTests.cs`
- `D:\Prop\tests\Unit\RiskEngineTests.cs`
- `D:\Prop\.env` (flag + host/account prefix only)
- `D:\Prop\data\demo_copy_ledger.json` (no secrets)

End of P500_VERIFY_69. Product source was not modified. No secrets printed. This slot did not send `35=D`. `REAL_COPY` was not flipped by this slot.
