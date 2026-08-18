# P500_VERIFY_9 — Adversarial verifier (slot 9)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_9.md` |
| Agent / slot | P500 adversarial verifier **9** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned SUT | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` (full `read_file` this slot) |
| Adjacent (needed to prove/fail) | `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `CTraderFixLogonHostedService.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CTraderFixOptions.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `CopyLifecycle.cs`, `ShadowCopyEngine.cs`, `DealIngestionService.cs`, `apps/api/Program.cs`, lab `.env` **boolean/host/account keys only**, on-disk `D:\Prop\data\demo_copy_ledger.json`, `DEMO_COPY_OPEN.json` |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Secrets printed | **None.** Quoted only `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73), `FEATURE_COPY_TRADING_ENABLED=true` (L106), public host `demo-us-eqx-01.p.c-trader.com`, public account `5328266`, public refuse-id `1369850`, public dest pos `237339770`. Tag 554 / FIX / MT5 / DB passwords not dumped. |
| Live GET this slot | **Blocked.** `web_fetch` of `http://127.0.0.1:5000/api/copy/status`, `/api/settings`, `/api/health` → SSRF (loopback). Those claims are **unverified** live. |
| Live attach / send this slot | **No.** No Manager Connect. No TLS. No Logon. No order. |
| Binding rule | **FAIL any claim that cannot be proven from a file just read or a live GET this slot.** Prior swarm reports are **not** evidence. A comment is not a choke. A default is not a pin. |

**Honesty:** `CTraderFixSession` having no `35=D` is **not** “the product has no `35=D` builder.” `SAFE_BY_ABSENCE` on the logon class is **stale** against the copy hop. W500 reports that still say copy-hop `NewOrderSingleImplemented=false` / “demo `Build(D)` tools-only” are **stale** against current `CopyTradingService` + `CopyTradingHostedService`.

---

## 0. Verdict (binding)

**FAIL.** Assigned AND of five claims does **not** hold. Two claims are proven. Three are not (one is file-true / product-false; one is disproven; one cannot be proven as dest-profit-safe).

| # | Assigned claim | Measured from file / GET | Class |
|---|---|---|---|
| 1 | no `35=D` builder | `CTraderFixSession` 135/135 is `(35,"A")` only. Product **has** `CTraderFixCopyOpen.Build("D")` and the 20s copy tick **calls it**. | **FAIL** (product). File-scoped logon class is PASS only. |
| 2 | `CanPromoteToLive` is false | `TraderStateMachine.CanPromoteToLive` ⇒ `false` (`BaselineScorer.cs` L211). `FromBaseline` never returns `LIVE`. | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **Disproven.** Logon host **reads/logs** the bit. Only C# write is DI bind of env. Lab `.env` L73 is `true`. | **FAIL** |
| 4 | sending now cannot be the profit path | Demo dest **does** send `35=D` on the 20s tick (`ExecuteDemoCopyAsync`). On-disk dest fill exists. Live `1369850` is refused. Cannot prove “no dest P&L path.” | **FAIL** |
| 5 | SHADOW on demo is not destination profit | `ShadowCopyEngine.SimulateEntry` is paper. `SHADOW_ONLY` persist. `FromBaseline` max is `SHADOW`. Residual: ADMITTED SHADOW can still dest-send via claim-4 path. | **PASS** (paper SHADOW ≠ dest). Residual dest send is claim 4. |

One-line:

```text
FAIL slot 9: CTraderFixSession is 35=A only, but copy hop has 35=D (CTraderFixCopyOpen) and ExecuteDemoCopyAsync sends it on demo dest. CanPromoteToLive=false. RealCopyEnabled NOT forced false after logon (.env true + DI bind). Sending now IS dest execution on 5328266 (not 1369850). SHADOW SimulateEntry is paper, not dest profit. Live GET unverified (SSRF).
```

Do **not** treat this FAIL as a license to send live `1369850`. Do **not** treat demo dest P&L as live Pepperstone profit. This slot did **not** flip `.env` and did **not** send.

---

## 1. Claim 1 — no `35=D` builder — **FAIL** (product)

### 1.1 Assigned file: `CTraderFixSession.cs` (135/135) — no builder **in this class**

Full read of `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`.

Outbound constructor is **only** `BuildLogon`. Tag 35 is hard-coded `"A"` (Logon):

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

Single `WriteAsync` of that logon (L47–50). Reply parse accepts `35=A` as LoggedOn (L55–64). No `NewOrderSingle`. No `(35, "D")`. No `Build("D"`. Sockets disposed via `using`.

**File-scoped:** this class cannot emit NewOrderSingle. That is **not** the assigned product claim.

### 1.2 Product builders that **do** emit `35=D` (same assembly)

`grep` of product `*.cs` for NewOrderSingle / `Build("D"` / tag 35:

| File | What it builds | Wired? |
|---|---|---|
| `CTraderFixSession.cs` | `(35, "A")` only | `CTraderFixLogonHostedService` QUOTE+TRADE logon |
| `CTraderFixCopyOpen.cs` L95, L142–156 | `Build("D", …)` NewOrderSingle | **YES** — `CopyTradingService.ExecuteDemoCopyAsync` L528, L566 |
| `CTraderFixDemoTestTrade.cs` L139, L163, L197 | `Build("D", …)` | `tools/DemoFixTestTrade` only (not copy tick) |
| `CTraderFixDemoMatrix.cs` L91–93 + `SendD` | `Build("D", …)` | tools matrix only |

`CTraderFixCopyOpen.Build`:

```142:156:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
    private static string Build(string type, string sender, string target, int seq, params (int, string)[] extra)
    {
        var fields = new List<(int, string)>
        {
            (35, type), (34, seq.ToString(CultureInfo.InvariantCulture)),
            (49, sender), (56, target), (50, "TRADE"), (57, "TRADE"),
            (52, DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture))
        };
        fields.AddRange(extra);
        // ...
    }
```

Call site on the hosted copy tick:

```19:34:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
            try
            {
                using var scope = _scopes.CreateScope();
                var copy = scope.ServiceProvider.GetRequiredService<CopyTradingService>();
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

`ExecuteDemoCopyAsync` (when `DemoDest`) calls `CTraderFixCopyOpen.SendAsync` for dest **close** (L528) and dest **open** (L566). That method writes `Build("D", …)` at L95.

`DemoDest` is true when host starts with `demo-`, trade sender starts with `demo.`, and account is not `1369850` (`CopyTradingService.cs` L45–48). Lab `.env` (keys only): host `demo-us-eqx-01.p.c-trader.com`, sender `demo.pepperstone.5328266`, account `5328266`. Therefore `NewOrderSingleImplemented => DemoDest` is **true** on this lab composition.

On-disk dest fill (not a live GET; file evidence):

- `D:\Prop\data\demo_copy_ledger.json`: source `305750` / `21250421` → dest pos `237339770`, ClOrdId `C20260818093047317`, fill `4390.2`, `DestClosed: false`, lots `0.01`.
- `D:\Prop\reports\swarm\20260818\DEMO_COPY_OPEN.json`: same ids, `OrderSent: true`, `Filled: true`, host demo, account `5328266`, exec `150=F` / `39=2`.

**Cannot prove “no 35=D builder.”** The assigned logon class has none. The copy hop **has one and uses it.** Claim 1 as a product/safety statement is **FAIL**. W500 “copy hop SAFE_BY_ABSENCE / demo Build(D) tools-only” is **STALE**.

---

## 2. Claim 2 — `CanPromoteToLive` is false — **PASS**

Full read of `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212).

`TraderStateMachine` in the same file:

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

Proven:

- `CanPromoteToLive` is a constant `false`. The `current` argument is unused.
- `FromBaseline` ceiling is `SHADOW`. It never returns `LIVE` or `LIVE_CANDIDATE`.
- Unit lock: `tests/Unit/BaselineScorerTests.cs` L21–26: three disciplined winners → `SHADOW`; `CanPromoteToLive(...).Should().BeFalse()`.

Scoring persist copies `SuggestedState` into `CurrentState` (`DealIngestionService.cs` L140). Product `grep CurrentState =` under `src/` does not assign `TraderState.LIVE`. Enum still **has** `LIVE = 5` (`TraderState.cs` L10) — unused by the scorer.

**Residual (does not revive LIVE):** `CopyTradingService` **never calls** `CanPromoteToLive`. Roster admit uses `XauUsdOneToOneCopyPolicy.IsTraderEligible`, which **allows** `SHADOW` (rejects only `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` / blocked states). Dest send in `ExecuteDemoCopyAsync` keys off roster `ADMITTED`, **not** `TraderState.LIVE`. Claim 2 is still true as written.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL**

### 3.1 Not in `CTraderFixSession`

`CTraderFixSession` has **zero** references to `RealCopyEnabled`. Logon success does not touch runtime flags.

### 3.2 Hosted logon does **not** re-pin false

`CTraderFixLogonHostedService.cs` L60–70 writes Quote/Trade `LoggedOn` / `Status` / `LastError` only. It **logs** `_runtime.RealCopyEnabled` (`RealCopyArmed={Armed}`) and **does not assign** `_runtime.RealCopyEnabled = false`.

### 3.3 Only C# write is DI env bind

`grep RealCopyEnabled\s*=` under `*.cs` = **one** assignment:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`LiveRuntimeStatus.RealCopyEnabled` is `{ get; set; }` (`LiveRuntimeStatus.cs` L32). Default is `false` **until** that bind.

Lab `.env` L73: `REAL_COPY_EXECUTION_ENABLED=true`. API `Program.cs` L10 `EnvFile.FindAndLoad()` then L13 `AddEnvironmentVariables()`. Therefore an API process that loaded this `.env` has `LiveRuntimeStatus.RealCopyEnabled == true`.

`CTraderFixOptions.RealCopyExecutionEnabled` defaults `false` (`CTraderFixOptions.cs` L35) and is **unbound** (no `Configure<CTraderFixOptions>` in DI). A POCO default is **not** a pin.

`/api/settings` L76 and `/api/health` L55 expose `runtime.RealCopyEnabled` (not a hardcoded `false`). Live GET of those routes was **SSRF-blocked** this slot — display not re-measured. File contract is: **follows runtime**.

`CopyTradingService.GetStatusAsync` L64: `RealCopyArmed: _runtime.RealCopyEnabled`. `BuildBlockers` L621–622 adds `"REAL_COPY_EXECUTION_ENABLED is false"` **only if** the bit is already false. An armed host **drops** that blocker.

**Cannot prove “forced false after logon.”** The opposite is on disk. Older A014 / A015 / W500_68 / CREDENTIALS “hosted re-pin false” reports are **STALE**.

---

## 4. Claim 4 — sending now cannot be the profit path — **FAIL**

Assigned UI (`LiveCopyPage.tsx` 70/70) is a **status surface**, not a sender. It does not POST orders. It prints `status.summary`, `realCopyArmed`, `liveSends`, and “Live send blockers (Pepperstone cannot be filled).” Empty-state copy **admits dest send**:

```56:58:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      {intents.length === 0 && (
        <p className="text-gray-500 text-sm">No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.</p>
      )}
```

That sentence is implemented.

### 4.1 Risk engine is not the dest-send choke

`RiskEngine.Evaluate` (`RiskEngine.cs` 67–172):

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

If `RealExecutionEnabled == false`, `AllowFixSend` is false (L90–93 is a **comment only** — no `return`; the later `allowSend` conjunction is the real brake). Unreconciled increasing orders reject `VENUE_NOT_RECONCILED` (L84–85).

`GenerateShadowIntentsAsync` feeds `Reconciled = VenueReconciled` where `public const bool VenueReconciled = false` (`CopyTradingService.cs` L20, L304). Persist then **overwrites** `AllowFixSend = false` (L324). The LIVE-send branch (L330) is dead: it requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` — last conjunct is the **const false**.

**That path is not the sender.** The sender is `ExecuteDemoCopyAsync`, which **does not call** `RiskEngine`. It does **not** check `RealCopyEnabled`, `AllowFixSend`, `TraderState.LIVE`, or `CanPromoteToLive`. Gates are only: `DemoDest`, password non-empty, `MaxAutoLots = 0.05m`, `CopyLifecycle.ShouldOpenDest` / `ShouldCloseDest`.

### 4.2 Dest send exists; live 1369850 does not

`CTraderFixCopyOpen.SendAsync` L37–42 refuses unless host `demo-*`, sender `demo.*`, and account ≠ `1369850`.

`GetStatusAsync` summary when `DemoDest` (L76–78): *“Demo dest auto-copy ON. Eligible demo/contest opens send on the 20s tick … Live 1369850 is never used.”*

On-disk ledger + `DEMO_COPY_OPEN.json` show a dest fill on account `5328266` (demo), 0.01 lot, dest `237339770`, **still open** (`DestClosed: false`). `ExecuteDemoCopyAsync` L500–512 will **re-seed** that row if missing.

`AllocationFactor = 1m` (`XauUsdOneToOneCopyPolicy.cs` L66) is 1:1 lots. Auto-send caps at `0.05` (`CopyTradingService.MaxAutoLots`) and 5 tickets per 20s tick.

**P500 operating law** (`P500_PROFIT_SYNTHESIS.md` §5) says “Send now = donate the Pepperstone account” and “35=D stays OFF.” That law is **policy**, not a choke. Current files **do send** `35=D` to the **demo** dest. Demo dest mark-to-market **is** destination P&L (demo money). Live GET of dest PnL was not possible this slot; dest `$0` pins in other reports are **not** re-proven here.

**Cannot prove “sending now cannot be the profit path.”** Sending now **is** the dest execution path on demo `5328266`. It is **not** live `1369850`. It is **not** a proven +EV engine. Claim as written (no dest-profit path from sending now) **FAIL**.

Honesty split (not a PASS):

| Dest | Can receive `35=D` from this hop? | Is that live Pepperstone capital? |
|---|---|---|
| `1369850` | **No** (string refuse + `DemoDest` false if that account) | N/A |
| `5328266` demo | **Yes** (builder + hosted tick + on-disk fill) | **No** (demo host/sender/account) |

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **PASS**

Proven from assigned + adjacent files:

1. **Scorer SHADOW is a state, not a dest fill.** `FromBaseline` returns `SHADOW` at quality ≥ 70 and risk < 40 after 3 XAU trades (`BaselineScorer.cs` L200–201). That is source-book classification.
2. **Paper fills.** `ShadowCopyEngine.SimulateEntry` (`ShadowCopyEngine.cs` L35–60) computes a modeled price from a stored quote + 0.05 point latency slip. It does not open a socket. `GenerateShadowIntentsAsync` writes `Status = "SHADOW_ONLY"` and `ShadowOrder` rows (L336–359). `EfTradingStore.PersistDemoShadowAsync` same (`SHADOW_ONLY` + `SimulateEntry`).
3. **Copy eligibility is demo/contest source groups**, not dest PnL. `CopyGroupFilter.IsDemoOrContest` (`CopyGroupFilter.cs` L9–23) keys off path segments `demo` / `contest`. `IsTraderEligible` also requires 20 completed XAU, `XauNetPnl > 0`, no size-pattern flags (`XauUsdOneToOneCopyPolicy.cs` L73–112). Source demo book PnL is **not** dest.
4. **UI.** `LiveCopyPage` shows `SHADOW traders` as a count and dest blockers separately. `ShadowPortfolioPage.tsx` L6: “Live NewOrderSingle remains disabled” — **stale against ExecuteDemoCopyAsync**, but the shadow page still describes quote-modeled fills.

**Residual (does not convert SHADOW paper into dest profit, but does couple SHADOW traders to dest send):** a trader who is `SHADOW` (the scorer ceiling) **and** eligible **and** `ADMITTED` is dest-sent by `ExecuteDemoCopyAsync` without a `LIVE` check. That dest fill is **claim 4**, not a `ShadowOrder` row. Claim 5 as “SHADOW paper / demo source score is not dest profit” holds.

---

## 6. Live GET (required; not obtained)

Attempted:

- `GET http://127.0.0.1:5000/api/copy/status`
- `GET http://127.0.0.1:5000/api/settings`
- `GET http://127.0.0.1:5000/api/health`

All **SSRF-blocked** (loopback). Therefore this slot **cannot** prove live `realCopyEnabled`, `realCopyArmed`, `quoteLoggedOn`, `tradeLoggedOn`, `liveTraders`, or dest mark-to-market. File contracts above still stand. Do not cite other reports’ `:5000` integers as this-slot measures.

---

## 7. Stale citations (do not reuse)

| Cite | Why stale against files read this slot |
|---|---|
| W500 / A003 / A014 “product `35=D=0` / copy hop SAFE_BY_ABSENCE” | `CTraderFixCopyOpen.Build("D")` + `CopyTradingHostedService` L30 |
| W500 “`NewOrderSingleImplemented` const false” | Now `=> DemoDest` (`CopyTradingService.cs` L50) |
| W500 “demo `Build(D)` tools-only” | Tools still exist; **copy service also calls** `CTraderFixCopyOpen` |
| A014 / A015 / W500_68 / CREDENTIALS “`RealCopyEnabled` forced false / logon re-pin” | DI binds env; logon only logs |
| E038 “`/api/settings` hardcoded false” | L76 = `runtime.RealCopyEnabled` |
| `ShadowPortfolioPage` “Live NewOrderSingle remains disabled” | Demo dest send is enabled when `DemoDest` |
| `GetStatusAsync` `VenueReconciled: DemoDest` | **Display lie** vs `const VenueReconciled = false` used in `RiskEngine` request |

---

## 8. Risk to capital

| Book | At risk from files proven this slot? |
|---|---|
| Live Pepperstone `1369850` | **No.** `CTraderFixCopyOpen` + `DemoDest` refuse that account / live host / live sender. |
| Demo dest `5328266` | **Yes, demo money.** Hosted 20s tick can (and on disk did) send `35=D`. Open dest `237339770` @ 0.01 lot, `DestClosed=false`. Cap 0.05 lot / 5 per tick. 1:1 `AllocationFactor`. Not live capital. |
| MT5 source book | **Not flattened by dest send.** Roster flatten is dest-only (`CopyRosterEngine` comment L31). Not re-proven with a live GET. |

`risk_to_capital`: **NONE on live `1369850`.** Demo dest can lose/gain demo equity. This slot did not send.

---

## 9. Files read (this slot)

| Path | Lines / scope |
|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 135/135 |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | 212/212 |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | 189/189 |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | 70/70 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | 223/223 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | 391/391 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | header + `SendD` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | 112/112 |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | 80/80 |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | 625/625 |
| `D:\Prop\src\Infrastructure\Copy\DemoCopyLedger.cs` | 40/40 |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 44/44 |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 62/62 |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | 66/66 |
| `D:\Prop\src\Application\Copy\CopyTradingModels.cs` | 30/30 |
| `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs` | 136/136 |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | 188/188 |
| `D:\Prop\src\Domain\Copy\CopyLifecycle.cs` | 10/10 |
| `D:\Prop\src\Domain\Copy\CopyGroupFilter.cs` | 24/24 |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | 91/91 |
| `D:\Prop\src\Domain\Enums\TraderState.cs` | 14/14 |
| `D:\Prop\apps\api\Program.cs` | 160/160 |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | 74/74 |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | header + `Real_flag_false` |
| `D:\Prop\apps\web\src\api\hooks.ts` | copy hooks |
| `D:\Prop\data\demo_copy_ledger.json` | dest ledger |
| `D:\Prop\.env` | boolean + public host/account keys only |

---

## 10. Slot JSON

```json
{
  "slot": 9,
  "verdict": "FAIL",
  "evidence": "C1 FAIL product: CTraderFixSession 135/135 is (35,A) only, but CTraderFixCopyOpen.Build(D) is called from ExecuteDemoCopyAsync on the 20s copy tick (DemoDest true on lab demo host/sender/5328266). C2 PASS: CanPromoteToLive => false; FromBaseline max SHADOW. C3 FAIL: logon host does not pin RealCopyEnabled false; DI binds .env L73 true. C4 FAIL: demo dest 35=D is the send path; ledger dest 237339770 0.01 still open; 1369850 refused. C5 PASS: ShadowCopyEngine.SimulateEntry / SHADOW_ONLY is paper, not dest. Live GET SSRF-blocked.",
  "risk_to_capital": "NONE on live 1369850 (hard refuse). DEMO dest 5328266 can receive 35=D; on-disk 0.01 lot dest 237339770 still open."
}
```
