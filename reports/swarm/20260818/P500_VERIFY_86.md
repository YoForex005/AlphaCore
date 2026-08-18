# P500_VERIFY_86 — Adversarial profit-path verify (slot 86)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_86.md` |
| Agent / slot | P500 adversarial **verify 86** |
| Date | 2026-08-18 |
| Role | Independent verifier. **Did not trust** sibling P500_BOOK / P500_VERIFY / W500 / CREDENTIALS prose. Re-read the four assigned files + adjacent send/logon hop this pass. |
| Assigned files | `CTraderFixSession.cs` (135/135), `BaselineScorer.cs` (212/212), `RiskEngine.cs` (189/189), `LiveCopyPage.tsx` (70/70) |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Quoted only boolean keys + already-public host prefix / account ids. |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live GET this pass | **Blocked.** `web_fetch` `http://127.0.0.1:5000/api/health` and `http://localhost:5000/api/health` → SSRF private-IP deny. **No** live GET body. Any claim that needs a live body is **FAIL**. |
| Live attach / send this pass | **No.** No Manager Connect. No TLS. No Logon. No `35=D`. This slot sent **0**. |
| Method | Full `read_file` of the four assigned files. Adjacent this pass: `CTraderFixLogonHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `CopyTradingService.cs` (625), `CopyTradingHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `apps/api/Program.cs`, `EnvFile.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `CopyLifecycle.cs`, `DemoCopyLedger.cs`, `data/demo_copy_ledger.json` (public dest ids only), `EfTradingStore.cs` persist-shadow, `hooks.ts`, `launchSettings.json` (API `:5000`), `BaselineScorerTests.cs`, `CTraderFixOptions.cs`, `apps/fix-worker/Worker.cs`. Grep: `Build("D")` / `RealCopyEnabled =` / `CanPromoteToLive` / `DestinationRealPnl`. Flag-only `.env` L49/L50/L56/L64/L73/L106. |

**Honesty rule:** FAIL any claim that cannot be proven from a file this slot or a live GET. Prior swarm prose is not evidence. A TLS Logon `35=A` is not a NewOrderSingle. `SAFE_BY_ABSENCE` on live `1369850` is not “flag stays false.” A demo hopper that can `Build("D")` is not `CTraderFixSession`. Destination constructor `$0` is not a measured dest book. Wanting profit is not an edge. Do not print secrets.

---

## 0. Verdict (binding)

**FAIL.** Only claim 2 is fully proven as written. Claim 1 is session-only. Claims 3–5 fail the bar (disproven / dest send exists / SHADOW is dest ADMIT class). Live GET absent.

| # | Assigned claim | Verdict | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **FAIL** as written | Assigned `CTraderFixSession` is `35=A` only (**PASS_SESSION**). Unscoped “no builder” is **false**: `Build("D")` ×5 in sibling session files; hosted hop calls `CTraderFixCopyOpen`. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive` in `BaselineScorer.cs` L211 is `=> false`. Parameter discarded. `FromBaseline` never returns `LIVE`. Product `src` callers: **0**. Unit test locks SHADOW-not-LIVE. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** (disproven) | DI L41 binds env. Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad` then `AddEnvironmentVariables`. Hosted logon **reads** `_runtime.RealCopyEnabled` at L70 and **never assigns false**. Product `RealCopyEnabled =` writers: **1** (DI bind). Live GET blocked. |
| 4 | sending now cannot be the profit path | **FAIL** | Cannot prove. Hosted `ExecuteDemoCopyAsync` **sends now** on DemoDest (`Build("D")`, no `RiskEngine.Evaluate`). On-disk ledger has an **open** dest fill. Dest DTO constructor `0` is **not** dest-account P&L. Live GET of dest book blocked. Live `1369850` still refused. |
| 5 | SHADOW on demo is not destination profit | **FAIL** as dest-safety | Paper SHADOW (`SimulateEntry` / Σ slippage / source `NetRealizedPnl`) is **not** dest cash (**PASS_PAPER**). Residual is load-bearing: policy eligibility floor **is** SHADOW; roster `AUTO_ADMIT`s it; `ExecuteDemoCopyAsync` ignores `CurrentState`/`LIVE`. SHADOW-on-demo **can** become dest `35=D`. |

**Overall slot verdict: FAIL** (instruction: FAIL any claim that cannot be proven from a file or live GET).

**Risk to capital:** **NONE on live Pepperstone `1369850`** (`SAFE_BY_ABSENCE` on `CTraderFixSession` + CopyOpen refuse). **Not absent on demo dest `5328266`** (hosted `ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.Build("D")`; ledger open dest `237339770` @ 0.01). Flag may be **armed**; that is **not** a live-send license. Do not paper over claim 3.

Stale siblings this slot contradicts: `CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**” and “Live `35=D` … method does not exist”; README “Real NewOrderSingle is **off** (`…=false`)”; any BOOK that still pins product `35=D=0` / `NOS=const false` / persist L306 / logon re-pin false.

---

## 1. no `35=D` builder — FAIL as written (PASS_SESSION only)

### 1.1 Assigned session file (full read, 135/135)

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`

Public API is **only** `TryLogonAsync`. The sole outbound builder:

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
| `ssl.WriteAsync` | **1** (L49) — bytes of that Logon |
| `Extract(..., "35")` | **1** — **inbound** reply only (L55) |
| Socket kept for a later `35=D` | **No** — `using TcpClient` / `await using SslStream` dispose on every return |
| Generic `Assemble` callers in this file | **1** — `BuildLogon` |

A one-shot Logon probe is **not** a NewOrderSingle builder. **Session-scoped** “`CTraderFixSession` has no `35=D` builder” is proven. The assigned claim text is **unscoped**.

### 1.2 Why the unscoped claim FAILs

Grep `Build("D")` on product `*.cs` this pass = **5** call sites, **none** in the four assigned files:

| File | Lines |
|---|---|
| `src/Fix.CTrader/Sessions/CTraderFixCopyOpen.cs` | L95 `Build("D", …)` — **hosted** via `CopyTradingService.ExecuteDemoCopyAsync` L528 close / L566 open |
| `src/Fix.CTrader/Sessions/CTraderFixDemoTestTrade.cs` | L139 flatten, L163 open, L197 close — CLI `tools/DemoFixTestTrade` only |
| `src/Fix.CTrader/Sessions/CTraderFixDemoMatrix.cs` | L93 `SendD` → `Build("D", …)` — same CLI |

Live identity gate on CopyOpen (`account == "1369850"` / host not `demo-` / sender not `demo.`) **refuses** live dest:

```36:42:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
        if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
            || !sender.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
            || account == "1369850")
        {
            return Fail("Refused: dest must be demo FIX, not live 1369850.", host, account, sourceLogin);
        }
```

Lab `.env` **is** DemoDest (flag/ids only; no secrets):

| Key | Value (public) |
|---|---|
| L49 `CTRADER_FIX_HOST` | `demo-us-eqx-01.p.c-trader.com` |
| L50 `CTRADER_FIX_ACCOUNT_ID` | `5328266` |
| L56/L64 sender CompIDs | `demo.pepperstone.5328266` |

`CopyTradingService.DemoDest` (L45–48) is host `demo-` **AND** TRADE sender `demo.` **AND** account ≠ `1369850`. That conjunction is **true** on this lab.

`CopyTradingHostedService` L30 calls `ExecuteDemoCopyAsync` every 20s. That is a product hop, not a CLI leftover.

`LiveCopyPage.tsx` has **0** FIX builders. It only renders `/api/copy/status` + `/api/copy/intents`. Empty-state L57 honestly says demo dest auto-sends after ADMITTED — UI copy, not an encoder, and evidence the product **does** send.

Claim 1 **FAIL** (unscoped). Session-only remainder: **PASS_SESSION**.

---

## 2. `CanPromoteToLive` is false — PASS

`D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (full read, 212/212). The machine lives in the same file:

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

| Fact | Measured this pass |
|---|---|
| Body | Unconditional `false`. `current` unused. |
| `FromBaseline` reachable set | `{INSUFFICIENT_DATA, RISK_BLOCKED, SHADOW, WATCH, EARLY_SCORE}` — **no** `LIVE` / `LIVE_CANDIDATE` |
| Product `src/**/*.cs` callers of `CanPromoteToLive` | **0** (definition only) |
| Other product callers | Test lock + `_tmp_c23_empty` harness. No runtime promotion gate. |
| Unit lock | `tests/Unit/BaselineScorerTests.cs` L21–26: three winners → `SHADOW`; `CanPromoteToLive(...) == false` |

`TraderState` enum still **defines** `LIVE_CANDIDATE=4` / `LIVE=5` (`src/Domain/Enums/TraderState.cs`). The scorer never emits them. Vacuous lock (no live branch exists to refuse), not A22 R5-before-R6. Still **file-proven false**. Claim 2 **PASS**.

---

## 3. `RealCopyEnabled` forced false after logon — FAIL (disproven)

None of the four assigned files mention `RealCopyEnabled`. Adjacent hop this pass:

**Bind (startup, before any logon):**

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`apps/api/Program.cs` L10: `EnvFile.FindAndLoad()` (includes hard path `D:\Prop\.env` at `src/Mt5/Env/EnvFile.cs` L14). L13: `AddEnvironmentVariables()`. `.env` L73: `REAL_COPY_EXECUTION_ENABLED=true`. `.env` L106: `FEATURE_COPY_TRADING_ENABLED=true` (API FEATURE flag is a separate literal `true` at `Program.cs` L77).

**After logon:** `CTraderFixLogonHostedService` L60–70 writes Quote/Trade `LoggedOn`/`Status`/`LastError`/`UpdatedAt` only, then **logs** `_runtime.RealCopyEnabled`. There is **no** `_runtime.RealCopyEnabled = false` (or `true`) assignment.

Grep product `src` `RealCopyEnabled =`: **1 hit** — the DI bind above.

`LiveRuntimeStatus.Snapshot` even advertises the armed branch: `"REAL_COPY armed. NewOrderSingle still unimplemented…"`. That comment is **stale** vs HEAD `NewOrderSingleImplemented => DemoDest` (`CopyTradingService` L50).

`CTraderFixOptions.RealCopyExecutionEnabled` still **defaults false** (L35). That POCO is **not** what DI binds onto `LiveRuntimeStatus`. The fix-worker reads a **different** key (`CTrader:RealCopyExecutionEnabled`, default false) and still does not send — it is not a re-pin of the API runtime flag.

`LiveCopyPage` L13 renders `status?.realCopyArmed ? 'YES' : 'NO'`. Without a live GET this slot, UI state is **unproven**. File path is: armed **can** be YES.

Claim 3 is not “unproven.” It is **false** on this tree. **FAIL.**

---

## 4. sending now cannot be the profit path — FAIL

### 4.1 What the assigned files prove

| File | What it can / cannot send |
|---|---|
| `CTraderFixSession` | Logon `35=A` only. Cannot be a profit path. |
| `RiskEngine` L147–150 | `AllowFixSend = RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. Comment L90–93 says shadow “never allows FIX send,” but the `if` body is empty. The actual send bit is the conjunction, **not** a hard false. |
| `LiveCopyPage` L57 | UI: “Demo dest auto-sends after a trader is ADMITTED…” |

Copy hop persist **overwrites** the engine bit:

```317:325:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                var rec = new RiskDecisionRecord
                {
                    Id = Guid.NewGuid(),
                    CopyIntentId = intent.Id,
                    Outcome = decision.Outcome,
                    ApprovedQuantity = decision.ApprovedQuantity,
                    Reason = decision.Reason,
                    AllowFixSend = false,
                    DecidedAt = now
                };
```

And `VenueReconciled = const false` (L20), so `Evaluate` itself cannot return `AllowFixSend=true` on that hop. The LIVE_SEND branch (L330–333) is dead even if flags flip (`NewOrderSingleImplemented && VenueReconciled` cannot both be true because `VenueReconciled` is the const). `GetStatusAsync` L67 **lies** `VenueReconciled: DemoDest` (true on this lab) while Evaluate uses the const.

That **paper** hop is not dest profit. It is not the only hop.

### 4.2 What actually sends now

`CopyTradingHostedService` L28–30, every 20s after an 8s startup delay:

1. `TickRosterAsync`
2. `GenerateShadowIntentsAsync` (paper)
3. **`ExecuteDemoCopyAsync`**

`ExecuteDemoCopyAsync` (L483–605):

- Returns 0 when `!DemoDest`. Lab `.env` **is** DemoDest (see §1.2).
- Does **not** read `_runtime.RealCopyEnabled`.
- Does **not** call `RiskEngine.Evaluate`.
- Does **not** read `AllowFixSend`, `CanPromoteToLive`, or `CurrentState == LIVE`.
- Closes then opens via `CTraderFixCopyOpen.SendAsync` → `Build("D")`.
- Caps `MaxAutoLots=0.05` (source skip, not dest clip). `AllocationFactor=1m` (1:1).
- `CopyLifecycle.ShouldOpenDest(true, already)` / `ShouldCloseDest(true, true, destClosed)` — boolean open/close only.

On-disk `D:\Prop\data\demo_copy_ledger.json` (public ids only):

| Field | Value |
|---|---|
| SourceLogin | `305750` |
| SourcePositionId | `21250421` |
| Lots | `0.01` |
| DestPositionId | `237339770` |
| DestClOrdId | `C20260818093047317` |
| DestFillPrice | `4390.2` |
| DestClosed | **`false`** |

`ExecuteDemoCopyAsync` L500–512 **re-seeds** that same open row if missing. An open dest ticket at a fill price is dest **exposure**. It is a dest P&L path even if the dashboard does not mark it.

Overview dest field is a constructor literal, not a venue rollup:

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

`DashboardModels.OverviewDto` field order: `ShadowPnl`, **`DestinationRealPnl`**, `XauGross`, `XauNet`. The first `0` after `shadowPnl` is dest real. It is **unmeasured**, not proof the dest book is flat. Grep `DestinationRealPnl` writers: **definition only** (no venue mark).

Live GET of `/api/copy/status` / `/api/overview` blocked → cannot prove live process dest PnL this slot.

`apps/fix-worker/Worker.cs` still writes “NewOrderSingle remains off” into FIX session rows and does not send. That worker is **not** the hosted copy hopper.

Claim 4 as written (“sending now cannot be the profit path”) **FAIL**. Sending now **can** be dest P&L on demo `5328266`. It **cannot** be live `1369850`. Constructor `$0` is not a mark.

---

## 5. SHADOW on demo is not destination profit — FAIL as dest-safety (PASS_PAPER)

### 5.1 Paper SHADOW is not dest cash (proven)

Assigned `BaselineScorer`:

- Features are Σ `ReconstructedTradeResult.NetRealizedPnl` on **completed source XAU**.
- `TraderState.SHADOW` is a source-quality landing (`quality >= 70 && risk < 40`).
- No dest fill, dest mark, or venue PnL field exists in this file.

Copy paper path (`GenerateShadowIntentsAsync`):

- Hopper states `{SHADOW, LIVE_CANDIDATE, LIVE}` (L202–203).
- Persist status **`SHADOW_ONLY`**.
- Optional `ShadowCopyEngine.SimulateEntry` (ask/bid + 0.05 latency slip). No socket.

`EfTradingStore.PersistDemoShadowAsync` also paper-only: skips unless `state == SHADOW`, writes `SHADOW_ONLY` + `SimulateEntry`.

Dashboard `ShadowPnl` = Σ `ShadowOrders.SourceVsShadowSlippage` (`EfDashboardQueries` L29). That is modeled slip, not dest realized.

Assigned `LiveCopyPage` shows `shadowTraders` / `shadowFills` as counts. No dest PnL column.

So: the **SHADOW number** is not destination profit. **PASS_PAPER.**

### 5.2 SHADOW-on-demo is the dest ADMIT class (cannot claim dest-safe)

`XauUsdOneToOneCopyPolicy.IsTraderEligible` **rejects** `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET` (L81–85). It **accepts** `SHADOW` (and `LIVE_CANDIDATE` / `LIVE`) if n≥20, XAU net>0, no size pattern, demo/contest group.

`CopyRosterEngine.Decide`: if eligible and `alreadyOnRoster=false` → `AUTO_ADMIT` (L72–80). Flatten is dest-only (class comment L31; `Remove` sets `FlattenDestination=true`).

`ExecuteDemoCopyAsync` iterates `CopyIntents` with `Status=="ADMITTED"` and `IdempotencyKey` `roster:*` (L542–544). **No** `CurrentState` check. An ADMITTED SHADOW demo source with an open XAU ≤0.05 lots is a dest `35=D` candidate.

Therefore “SHADOW on demo is not destination profit” **fails as a dest-safety claim**. SHADOW is the **floor** that can put a demo source onto the hopper. Claim 5 **FAIL**.

---

## 6. Assigned UI (LiveCopyPage) — honesty, not a sender

`D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (70/70). Hooks: `GET /api/copy/status`, `GET /api/copy/intents` (`hooks.ts` L60–65). Launch profile binds API `http://localhost:5000` (`apps/api/Properties/launchSettings.json` L17/L27).

| UI field | Source |
|---|---|
| `REAL_COPY armed` YES/NO | `status.realCopyArmed` ← `_runtime.RealCopyEnabled` |
| SHADOW / LIVE traders | score counts |
| Live sends | `ExecutionIntents` with `SentAt != null` (paper table; demo hop writes ledger/intents, not this count) |
| Blockers header | “Live send blockers (Pepperstone cannot be filled)” |
| Empty-state L57 | “Demo dest auto-sends after a trader is ADMITTED…” |

UI is a **readout**. It cannot prove dest PnL. It **does** disclose auto-send. Live GET blocked → rendered values this slot **unproven**.

---

## 7. Stale pins this slot contradicts

| Pin | HEAD this pass |
|---|---|
| `CREDENTIALS_AND_COPY_STATUS.md` `REAL_COPY` **false (forced)** | `.env` L73 `true` + DI bind; logon does not re-pin |
| Same file: Live `35=D` “method does not exist” | `CTraderFixCopyOpen.Build("D")` + hosted hopper |
| README: NewOrderSingle **off** (`=false`) | Env true; `NOS => DemoDest` |
| BOOK `NOS=const false` / product `35=D=0` / persist L306 | `NOS => DemoDest` L50; persist overwrite **L324**; `Build("D")` ×5 |
| W500 “copy hop SAFE_BY_ABSENCE” as product-wide | Live `1369850` still absent; **demo dest not absent** |
| `LiveRuntimeStatus.Snapshot` “NewOrderSingle still unimplemented” | `NewOrderSingleImplemented => DemoDest` is **true** on this lab |

---

## 8. What this slot did not do

- Did not modify product, tests, `.env`, ledger.
- Did not send `35=D`.
- Did not live-attach MT5 or FIX.
- Did not obtain a live GET body (SSRF).
- Did not print secrets.

---

## 9. Slot-86 one-line

**FAIL.** `CTraderFixSession` is `35=A` only; `CanPromoteToLive => false`; `RealCopyEnabled` stays env-`true` after logon; hosted demo hopper **can** `Build("D")` now; SHADOW is dest AUTO_ADMIT, not dest cash. Live `1369850` **NONE**. Demo dest **not** `SAFE_BY_ABSENCE`.
