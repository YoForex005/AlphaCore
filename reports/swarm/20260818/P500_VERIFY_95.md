# P500_VERIFY_95 — Adversarial four-file verify (slot 95)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_95.md` |
| Agent / slot | P500 adversarial verifier **95** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (live `src/`, `apps/`, lab `.env`) |
| Assigned SUT | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` (read in full this slot) |
| Supporting files (claims 1–5 hop) | `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CopyRosterEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyGroupFilter.cs`, `ShadowCopyEngine.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `DealIngestionService.cs`, `EfTradingStore.cs`, `apps/api/Program.cs`, `EnvFile.cs`, `TraderState.cs`, `data/demo_copy_ledger.json` |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Live `35=D` sent this slot | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None.** Quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73) and public dest/source ids `5328266` / `1369850` / `305750` / `237339770`. No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live GET this pass | `GET http://127.0.0.1:5000/api/health`, `http://localhost:5000/api/copy/status`, `http://127.0.0.1:18720/api/health` **SSRF-blocked**. Runtime flag **not** live-proven. File proof is enough to score claim 3. Live dest cash **unproven**. |
| Binding rule | **FAIL any assigned claim that cannot be proven from a live file or a live GET.** Sibling swarm books are not evidence. This slot re-read the four SUT files and the hop files independently. |

**Honesty:** Wanting dest profit is not an edge. Copy-all of the catalog would copy `RISK_BLOCKED` source losses. `SAFE_BY_ABSENCE` on live `1369850` is **not** “`RealCopyEnabled` stays false.” An armed env bit with no live sender is still an armed bit. Demo dest send is dest **exposure**, not dest-profit accounting. `DestinationRealPnl` constructor `0` is not a mark-to-market of dest `5328266`. SHADOW paper is not dest cash; SHADOW **is** the dest AUTO_ADMIT floor.

---

## 0. Verdict (binding)

**FAIL.** Claim 2 is file-proven. Claims **1** (as written, unscoped), **3**, **4** (as written, unscoped), and **5** (as dest-safety) do **not** pass as a five-claim bundle. Paper-scoped 4/5 survive only as narrower restatements.

| # | Assigned claim | Measured | Class |
|---|---|---|---|
| 1 | no `35=D` builder | **PROVEN** on `CTraderFixSession.cs` (135/135, only `(35, "A")`). **DISPROVEN** product-wide: sibling `Build("D")` ×5, one hosted. | **FAIL** unscoped / **PASS_SESSION** |
| 2 | `CanPromoteToLive` is false | **PROVEN** (`=> false`; unused `current`) | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **DISPROVEN** — only assignment is DI bind of `.env=true`; logon host never writes the bit | **FAIL** |
| 4 | sending now cannot be the profit path | Session/persist hop cannot book dest profit (`AllowFixSend=false`; dest DTO constructor `0`). Hosted demo hopper **can send `35=D` now** and ledger dest is still open. Venue dest P&L **unproven** (no live GET). | **FAIL** unscoped / **PASS_NOT_BOOKED_DEST_PROFIT** |
| 5 | SHADOW on demo is not destination profit | Paper `SimulateEntry` / slippage sum ≠ dest cash (**PASS_PAPER**). SHADOW is dest AUTO_ADMIT floor; hopper ignores `LIVE` and can `35=D` dest `5328266`. Dest cash from that hop **unproven** (no live GET) → dest-safety **not proven**. | **FAIL** dest-safety / **PASS_PAPER** |

One-line:

```text
FAIL slot 95: CTraderFixSession 35=A only (no D builder); product Build("D")×5 hosted; CanPromoteToLive=>false; RealCopyEnabled NOT forced false after logon (.env true + DI bind + logon logs-only); send-now is not booked dest-profit (DTO 0) but demo dest hopper can 35=D now; SHADOW paper ≠ dest PnL but SHADOW is dest AUTO_ADMIT. Risk NONE on live 1369850. Live GET blocked.
```

Do **not** treat this FAIL as a license to send. Do **not** flip leftover `.env=true` into go-live. Operator should restore `REAL_COPY_EXECUTION_ENABLED=false` (this slot did **not** edit it).

---

## 1. Claim 1 — no `35=D` builder — FAIL unscoped / PASS_SESSION

### 1.1 Assigned file `CTraderFixSession.cs` (135/135)

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` read in full this slot. The only outbound builder is `BuildLogon`. Tag 35 is hardcoded `"A"`:

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

Single `WriteAsync` (L49) of that logon. Then one `ReadAsync` (L53). Socket disposed. Inbound `Extract(reply, "35")` (L55, L122–134) is reply parse, not a builder. Tokens in this file: `Build("D")` = 0, `(35, "D")` = 0, `NewOrderSingle` = 0. Generic `Assemble` (L112–119) is only called from `BuildLogon`.

Hosted caller of this class is logon-only:

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

`rg Build\("D"\)` under product `src\` `*.cs` (this slot):

| File | Hits | Hosted? |
|---|---|---|
| `src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 | 1 | **Yes** — `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566 |
| `src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | 3 | CLI only (`tools\DemoFixTestTrade\Program.cs`) |
| `src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` L93 | 1 | CLI only |

`CTraderFixCopyOpen.Build` is generic (`Build(string type, ...)`, L142–156) and L95 writes `Build("D", ...)`. The generic body sets `(35, type)`:

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
        static string Pair(int t, string v) => t.ToString(CultureInfo.InvariantCulture) + "=" + v + "\u0001";
        var body = string.Concat(fields.Select(f => Pair(f.Item1, f.Item2)));
        var head = Pair(8, "FIX.4.4") + Pair(9, body.Length.ToString(CultureInfo.InvariantCulture));
        var soFar = head + body;
        return soFar + Pair(10, (soFar.Sum(ch => (int)ch) % 256).ToString("000", CultureInfo.InvariantCulture));
    }
```

Hosted hopper:

```19:34:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
        await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var copy = scope.ServiceProvider.GetRequiredService<CopyTradingService>();
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

`CTraderFixCopyOpen` refuses live dest (`host` not `demo-` **or** `sender` not `demo.` **or** `account == "1369850"`) at L37–41 and returns without writing `35=D`. That is a refuse, not absence of a builder.

Assigned wording is “no `35=D` builder” with no session scope. Product-wide that sentence is **false**.

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

Persist hop that would send live also requires `score.CurrentState == TraderState.LIVE` (`CopyTradingService.cs` L330) — unreachable from this scorer.

**Score:** **PASS.**

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL

This is a hard FAIL. Independent file proof. Live GET was **not** used as PASS evidence (blocked).

### 3.1 Only assignment in product C#

`rg RealCopyEnabled\s*=` under `*.cs` / `*.tsx` = **one** product hit:

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

If the API process loads lab `.env`, the singleton is **true** at construction.

### 3.3 Logon host does not re-pin

`CTraderFixLogonHostedService.ExecuteAsync` after both `TryLogonAsync` calls writes only `Quote`/`Trade` status fields, then **logs** the existing bit:

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

Read, not write. The log line even advertises “NewOrderSingle still unimplemented” while the armed bit stays whatever DI bound.

`CTraderFixOptions.RealCopyExecutionEnabled` defaults `false` (`CTraderFixOptions.cs` L35) but that options class is **not** the writer of `LiveRuntimeStatus.RealCopyEnabled`. Different object. Does not rescue claim 3.

### 3.4 Live GET

Loopback GET blocked (SSRF). Cannot confirm the **running** process bit. File proof already **disproves** “forced false after logon”: there is no force. The claim is not “the live process currently reads false.”

**Score:** **FAIL.**

---

## 4. Claim 4 — sending now cannot be the profit path — FAIL unscoped / PASS_NOT_BOOKED_DEST_PROFIT

Assigned `RiskEngine.cs` (189/189) and `LiveCopyPage.tsx` (71/71) plus hop.

### 4.1 Persist hop cannot book dest profit — PASS_NOT_BOOKED_DEST_PROFIT

`RiskEngine.Evaluate` computes:

```147:150:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
```

`RealExecutionEnabled == false` is a **no-op** at L90–93 (comment only; does not `Reject`). Send permission is the later `allowSend` conjunction.

Persist caller **overwrites** the decision:

```317:337:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
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
                _db.RiskDecisions.Add(rec);
                intent.RiskDecisionId = rec.Id;

                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
                else
                {
                    intent.Status = "SHADOW_ONLY";
```

Pinned facts this slot:

- Persist `AllowFixSend = false` is **L324** (not a stale L306 pin).
- `CopyTradingService.VenueReconciled` is `const false` (**L20**). Evaluate is fed `Reconciled = VenueReconciled` (L304) so computed `AllowFixSend` is already false even if `.env` armed the bit.
- Live-send branch also requires `TraderState.LIVE`, which claim 2 proves this scorer cannot emit.
- `NewOrderSingleImplemented => DemoDest` (**L50**). Not a const-false. Demo dest **is** NOS-true when env matches.
- Dashboard `DestinationRealPnl` is constructor literal `0` (`EfDashboardQueries.cs` L44; `OverviewDto` L16). That is a DTO zero, **not** a venue mark.

`LiveCopyPage.tsx` has **no send control**. It displays `realCopyArmed` and intents. Empty-state copy (L56–57) states dest auto-sends after ADMIT:

```56:58:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      {intents.length === 0 && (
        <p className="text-gray-500 text-sm">No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.</p>
      )}
```

UI honesty: sending is not a dashboard click. Hosted hopper is.

### 4.2 Hosted demo hopper can send now — FAIL unscoped “cannot be the profit path”

`ExecuteDemoCopyAsync` (L483–605) **bypasses** `RiskEngine.Evaluate`. If `DemoDest`:

```45:50:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public bool DemoDest =>
        (_config["CTRADER_FIX_HOST"] ?? "").StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
        && (_config["CTRADER_FIX_TRADE_SENDER_COMP_ID"] ?? "").StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
        && _config["CTRADER_FIX_ACCOUNT_ID"] != "1369850";
```

Lab `.env` (non-secret keys only):

- L49 `CTRADER_FIX_HOST=demo-us-eqx-01.p.c-trader.com`
- L50 `CTRADER_FIX_ACCOUNT_ID=5328266`
- L64 `CTRADER_FIX_TRADE_SENDER_COMP_ID=demo.pepperstone.5328266`

All three DemoDest predicates match. Hopper then calls `CTraderFixCopyOpen.SendAsync` (open L566, close L528) → `Build("D")`.

On-disk ledger `D:\Prop\data\demo_copy_ledger.json` (this slot):

```json
SourceLogin 305750 / SourcePositionId 21250421 / Lots 0.01 / DestPositionId 237339770 / DestFillPrice 4390.2 / DestClosed false
```

`ExecuteDemoCopyAsync` L500–512 **re-seeds** that same open dest row if missing. Dest is not absent.

GetStatus summary when DemoDest (L76–77): “Demo dest auto-copy ON. Eligible demo/contest opens send on the 20s tick.”

Sending **can** be dest cash P&L on demo `5328266`. I cannot measure that cash (live GET blocked). I also cannot prove it is zero. Unscoped “cannot be the profit path” is therefore **false or unproven**. Booked dashboard dest-profit remains constructor `0`.

Live dest `1369850` is refused at CopyOpen L37–41. That is **not** the same as “sending cannot profit.” Demo dest send is a profit-or-loss path on a real (demo) book.

**Score:** **FAIL** unscoped. **PASS_NOT_BOOKED_DEST_PROFIT** only.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — FAIL dest-safety / PASS_PAPER

### 5.1 Paper SHADOW is not dest cash — PASS_PAPER

`ShadowCopyEngine.SimulateEntry` (`ShadowCopyEngine.cs` L35–61) returns a `ShadowFill` (price/qty/slippage). No socket. No tag 35.

`EfDashboardQueries.GetOverviewAsync` L29:

```csharp
var shadowPnl = await _db.ShadowOrders.SumAsync(s => (decimal?)s.SourceVsShadowSlippage, ct) ?? 0;
```

That sum is **slippage**, then passed as `OverviewDto.ShadowPnl`. Next three decimals are `0, 0, 0` including `DestinationRealPnl`.

`FromBaseline` can emit `SHADOW` at quality ≥ 70 and risk < 40 (`BaselineScorer.cs` L200–201). That is a **source** classification.

`EfTradingStore.PersistDemoShadowAsync` writes `SHADOW_ONLY` intents + `SimulateEntry` fills (L307–320). Paper.

### 5.2 SHADOW is dest AUTO_ADMIT — dest-safety not proven

`XauUsdOneToOneCopyPolicy.IsTraderEligible` **rejects** `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET` (L81–85). It does **not** reject `SHADOW`. With ≥20 completed XAU, positive XAU book, no size-pattern flags, and demo/contest group, `SHADOW` returns eligible.

`CopyRosterEngine.Decide` then `AUTO_ADMIT` (L72–80). Hopper `ExecuteDemoCopyAsync` iterates `Status == "ADMITTED"` roster seats (L542–544) and sends dest `35=D` **without** checking `TraderState.LIVE`.

`GenerateShadowIntentsAsync` copyable set is `{ SHADOW, LIVE_CANDIDATE, LIVE }` (L202). Persist of those intents is paper (`SHADOW_ONLY`). The dest hop is the 20s `ExecuteDemoCopyAsync`, not the shadow ledger.

Policy also **requires** demo/contest groups (`CopyGroupFilter.IsDemoOrContest`; `NOT_DEMO_OR_CONTEST_GROUP`). “SHADOW on demo” is exactly the admit class.

Assigned claim: “SHADOW on demo is not destination profit.”

- As **artifact identity** (shadow ledger / slippage / DTO dest 0): **PASS_PAPER**.
- As **dest-safety** (SHADOW-on-demo cannot become dest P&L): **not proven**. Hopper can send dest `35=D` for ADMITTED SHADOW seats. Venue dest P&L unmeasured (no live GET). Ledger dest still open.

Binding rule: FAIL claims that cannot be proven. Dest-safety wording is **not** file-proven. I will not launder it into PASS.

**Score:** **FAIL** dest-safety / **PASS_PAPER**.

---

## 6. Live GET

Attempted this slot (no secrets in URLs):

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | SSRF blocked |
| `http://localhost:5000/api/copy/status` | SSRF blocked |
| `http://127.0.0.1:18720/api/health` | SSRF blocked |

`apps/api/Properties/launchSettings.json` binds `:5000` (http profile) and IIS Express `:18720`. Neither was readable from this verifier. **No live process bit, no live dest mark, no live `realCopyEnabled` JSON.** File proof stands alone.

---

## 7. Risk to capital

| Book | Measured | Class |
|---|---|---|
| Live Pepperstone `1369850` | CopyOpen L37–41 refuse; DemoDest false if account is `1369850`; persist hop dead (`LIVE` unreachable + `VenueReconciled` const false + persist `AllowFixSend=false`) | **NONE** (`SAFE_BY_ABSENCE`) |
| Demo dest `5328266` | Hosted 20s hopper + `Build("D")` + ledger dest `237339770` **open** 0.01 lot | **NOT absent.** Dest exposure **wired**. Dest cash **unmeasured** |
| This slot | 0 `35=D` written by this agent | **NONE from this verifier** |

Armed `.env` `REAL_COPY_EXECUTION_ENABLED=true` is **not** a live-send license. It is also **not** “forced false after logon.” Operator should pin it false. This slot did not edit `.env`.

---

## 8. What this slot did not do

- Did not modify product, tests, `.env`, or ledger.
- Did not send FIX.
- Did not print secrets.
- Did not treat sibling `P500_VERIFY_*` books as evidence (they were used only to match report shape).
- Did not invent a live dest mark from DTO `0`.
- Did not claim EX5 / ML / ≥95% decompile (out of scope).

---

## 9. Binding restatement

Five-claim bundle **FAIL**. Proven: session has no `35=D` builder; `CanPromoteToLive => false`; persist hop cannot book dest profit; SHADOW paper ≠ dest cash. Disproven / unproven: product has no `35=D` builder; `RealCopyEnabled` forced false after logon; sending cannot be a dest P&L path; SHADOW-on-demo cannot become dest profit. Live GET blocked. Live `1369850` **NONE**. Demo dest residual.
