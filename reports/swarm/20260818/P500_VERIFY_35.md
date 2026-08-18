# P500_VERIFY_35 — Adversarial five-claim confirm (slot 35)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_35.md` |
| Agent | P500_VERIFY_35 (adversarial verifier, slot **35**) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned SUTs | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` (full re-read this slot) |
| Adjacent hops (disprove only) | `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `CTraderFixLogonHostedService.cs`, `LiveRuntimeStatus.cs`, `DependencyInjection.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `CopyGroupFilter.cs`, `CopyLifecycle.cs`, `ShadowCopyEngine.cs`, `DemoCopyLedger.cs`, `EfDashboardQueries.cs`, `DealIngestionService.cs`, `apps/api/Program.cs`, `apps/web/src/api/hooks.ts`, `apps/api/Properties/launchSettings.json`, lab `.env` **booleans / public dest ids only** |
| Product source modified | **No.** This report is the only write from this slot. |
| Test source modified | **No.** |
| `.env` modified | **No.** |
| Secrets printed | **None.** Quoted only already-on-disk boolean `REAL_COPY_EXECUTION_ENABLED=true`, `FEATURE_COPY_TRADING_ENABLED=true`, public host prefix `demo-`, public dest ids `5328266` / `1369850`. Tag 554 / passwords / proxy / DB strings never dumped. |
| Live GET this slot | **Blocked.** `web_fetch` SSRF-denied `http://localhost:5000/api/health` and `http://127.0.0.1:5000/api/copy/status`. No shell. Any claim that needs a live DTO is **not proven**. |
| This slot sent `35=D` | **No.** |
| `REAL_COPY` flipped | **No.** |
| Method | Independent full `read_file` of the four assigned files. Targeted `grep` for `35=D` / `Build("D")` / `CanPromoteToLive` / `RealCopyEnabled\s*=` / `TraderState.LIVE`. Adjacent hops opened only to try to **disprove** a claim. Prior swarm text is **untrusted**. |

**Honesty rule:** FAIL any assigned claim not proven from a file this slot or a live GET this slot. A comment that says “NewOrderSingle still unimplemented” is not a missing builder. `CTraderFixSession` having no `35=D` is not “the process has no `35=D` builder.” `const bool VenueReconciled = false` on the *risk hop* is not the absence of a *demo dest* hop. `CanPromoteToLive => false` does not stop `ExecuteDemoCopyAsync`. Dashboard `DestinationRealPnl = 0` is a constructor literal, not dest cash. Do **not** treat live GET as measured.

---

## 0. Verdict (binding)

**FAIL.** The five-claim bundle is not proven as written. Claim 2 is file-true. Claim 1 is file-true only if silently scoped to `CTraderFixSession.cs`. Claims 3–5 are false or unproven as written.

| # | Claim | Result | Class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS only if scoped to `CTraderFixSession.cs`.** **FAIL** as a process/product claim. | Assigned file is `35=A` logon only. Hosted sibling `CTraderFixCopyOpen.Build("D")` exists and is called. |
| 2 | `CanPromoteToLive` is false | **PASS** | Hardcoded `=> false`. `FromBaseline` never emits `LIVE` / `LIVE_CANDIDATE`. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** (disproven) | Logon host never writes the flag. Only assignment is DI bind of `.env` `true`. |
| 4 | sending now cannot be the profit path | **FAIL** as written | 20s `ExecuteDemoCopyAsync` sends dest `35=D` and books `DestFillPrice`. |
| 5 | SHADOW on demo is not destination profit | **FAIL** as written | `SHADOW` is the dest AUTO_ADMIT floor; dest hop does not require `LIVE`. |

One-line:

```text
FAIL. CTraderFixSession is 35=A only. CanPromoteToLive=>false.
RealCopyEnabled is NOT forced false after logon (DI binds .env true; logon logs only).
Hosted ExecuteDemoCopyAsync -> CTraderFixCopyOpen.Build("D") on demo dest.
SHADOW is dest-eligible. Live 1369850 refused. Live GET blocked.
```

Operating mode (honest):

```text
ALLOW:  treat CTraderFixSession as logon-proof only
        treat CanPromoteToLive as a hard no-LIVE from scoring
        treat ShadowOrder / dashboard ShadowPnl as simulated, not dest $
        treat live 1369850 as refused by dest identity gate
FORBID: claim the process has no 35=D builder
        claim RealCopyEnabled is forced false after logon
        claim "sending now cannot be the profit path"
        treat SHADOW/demo as non-dest (roster + ExecuteDemoCopyAsync send dest)
        treat live GET / dashboard DestinationRealPnl=0 as measured dest cash
        treat older SAFE_BY_ABSENCE / NewOrderSingleImplemented=false as current
```

---

## 1. Claim 1 — no `35=D` builder — **PASS** (assigned file) / **FAIL** (unscoped)

Assigned file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135, full read this slot).

The only outbound builder is `BuildLogon`. Tag 35 is hardcoded `"A"` (Logon). There is no `"D"` literal, no `NewOrderSingle`, no second writer.

```89:108:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
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

Measured on this file:

| Fact | Evidence |
|---|---|
| Outbound MsgType | `(35, "A")` only (`L96`) |
| `WriteAsync` | **1** (`L49`) — the logon bytes |
| Inbound `35` | `Extract(reply, "35")` (`L55`); success iff `"A"` (`L56`) |
| `"D"` / `NewOrderSingle` / `35=D` | **0** hits |
| Socket lifetime | `using TcpClient` + `await using SslStream` (`L35–39`); disposed on return |
| Session stays up | **No.** Connect → logon → one read → return. Not a live TRADE socket. |

Unscoped product claim is **false**. Same directory, `grep Build("D")` this slot:

| File | `Build("D")` sites | Wired how |
|---|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | `L95` | **Hosted.** `CopyTradingService.ExecuteDemoCopyAsync` `L528` (close) and `L566` (open) |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | `L139`, `L163`, `L197` | CLI `tools/DemoFixTestTrade` (not DI). Demo-gated. |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | `L93` (`SendD` → `Build("D")`) | Off-hop matrix. Demo-gated. |

Dest identity gate on the hosted builder (public ids only):

```37:42:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
        if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
            || !sender.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
            || account == "1369850")
        {
            return Fail("Refused: dest must be demo FIX, not live 1369850.", host, account, sourceLogin);
        }
```

That is a **demo dest `35=D` builder**, not “no builder.” A comment on the logon host (`CTraderFixLogonHostedService.cs` `L69`: “NewOrderSingle still unimplemented”) is stale against `CTraderFixCopyOpen`.

**Claim 1 verdict:** PASS only with the silent scope `CTraderFixSession.cs`. FAIL as a product/process claim. Bundle does not get to claim “no 35=D builder.”

---

## 2. Claim 2 — `CanPromoteToLive` is false — **PASS**

Assigned file `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (full read). The machine is in the same file.

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

| Fact | Evidence |
|---|---|
| Promotion API | `CanPromoteToLive(...) => false` (`L211`). Argument `current` is unused. |
| `FromBaseline` outputs | `INSUFFICIENT_DATA` / `RISK_BLOCKED` / `SHADOW` / `WATCH` / `EARLY_SCORE` only. **Never** `LIVE` or `LIVE_CANDIDATE`. |
| High early score | `AfterHighEarlyScore() => SHADOW` (`L209`) |
| Production call sites of `CanPromoteToLive` | **0** in `src/` / `apps/`. Only unit test `BaselineScorerTests.cs` `L26` and a tmp harness. |
| Score persistence | `DealIngestionService` writes `CurrentState = score.SuggestedState` (SuggestedState comes from `FromBaseline`). No other writer in `src/` assigns `TraderState.LIVE`. |

Unit test `Three_disciplined_winners_go_to_shadow_not_live` asserts `SuggestedState == SHADOW` and `CanPromoteToLive(...) == false`. That supports the scorer claim. It does **not** prove dest send is off (claim 4/5).

**Claim 2 verdict:** PASS. Scoring cannot mint LIVE. That is not dest-send absence.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL**

None of the four assigned files force the flag false after logon.

- `CTraderFixSession.cs`: no `RealCopyEnabled` symbol.
- `BaselineScorer.cs`: no runtime flag.
- `RiskEngine.cs`: reads `request.RealExecutionEnabled`; does not write `LiveRuntimeStatus`.
- `LiveCopyPage.tsx`: displays `status?.realCopyArmed` (`L13`); GET-only; no setter.

The **only** `RealCopyEnabled =` assignment in product `*.cs` this slot:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Lab `.env` L73 (boolean only): `REAL_COPY_EXECUTION_ENABLED=true`.

API boot loads that file (`apps/api/Program.cs` `L10` `EnvFile.FindAndLoad()`; `EnvFile` includes `D:\Prop\.env` as a candidate). So a normally started API process binds **true**, not false.

Post-logon host **reads** the flag and logs it. It does not write it:

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

`grep RealCopyEnabled\s*=` this slot: **one** hit (`DependencyInjection.cs` `L41`). Zero `RealCopyEnabled = false` in `src/` / `apps/`.

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (`CTraderFixOptions.cs` `L35`). That POCO default is **not** the runtime pin. DI does not copy the POCO onto `LiveRuntimeStatus` after logon.

Live GET of `/api/health` / `/api/settings` / `/api/copy/status` would have shown the process boolean. This slot **could not** GET. File evidence already **disproves** “forced false after logon.” A missing live DTO cannot rescue the claim.

**Claim 3 verdict:** FAIL. Disproven. Flag is env-bound `true` and is not re-pinned after logon.

---

## 4. Claim 4 — sending now cannot be the profit path — **FAIL** as written

Two hops exist. Do not conflate them.

### 4.1 Risk hop — cannot send (file-true; not the only hop)

`RiskEngine.Evaluate` computes:

```147:171:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
        // ...
                AllowFixSend = allowSend
```

The empty block at `L90–93` (“Shadow path still evaluates risk but never allows FIX send”) is a **comment**. It does not assign `AllowFixSend`. The later `allowSend` expression does, via `RealExecutionEnabled`.

Copy risk hop then **throws the decision away**:

```20:20:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
```

```303:336:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    RealExecutionEnabled = _runtime.RealCopyEnabled,
                    Reconciled = VenueReconciled,
                    // ...
                    AllowFixSend = false,
                    // ...
                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
                else
                {
                    intent.Status = "SHADOW_ONLY";
```

Even if every gate were true, the LIVE branch only writes `LIVE_SEND_BLOCKED_UNIMPLEMENTED`. Persist `AllowFixSend` is hardcoded `false`. `CTraderFixSession` cannot place an order. `LiveCopyPage` is GET-only (`useCopyStatus` / `useCopyIntents`).

That hop is **not** dest profit.

### 4.2 Demo dest hop — **is** a dest P&L path (disproves the claim as written)

`NewOrderSingleImplemented` is **not** a const false (older swarm `SAFE_BY_ABSENCE` is stale):

```45:50:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public bool DemoDest =>
        (_config["CTRADER_FIX_HOST"] ?? "").StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
        && (_config["CTRADER_FIX_TRADE_SENDER_COMP_ID"] ?? "").StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
        && _config["CTRADER_FIX_ACCOUNT_ID"] != "1369850";

    public bool NewOrderSingleImplemented => DemoDest;
```

Lab `.env` public dest: host `demo-us-eqx-01.p.c-trader.com`, sender `demo.pepperstone.5328266`, account `5328266` (≠ `1369850`). `DemoDest` is **true** when that env is loaded.

Hosted 20s tick:

```27:30:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

`ExecuteDemoCopyAsync` (`CopyTradingService.cs` `L483–605`) does **not** consult `RiskEngine`, `AllowFixSend`, `CanPromoteToLive`, or `TraderState.LIVE`. If `DemoDest`, it:

1. Auto-closes dest when source reconstructed trade is completed (`CTraderFixCopyOpen.SendAsync` + dest pos id `L528–530`).
2. Auto-opens dest for `ADMITTED` roster seats with open XAUUSD ≤ `MaxAutoLots` (`0.05m`) (`L542–569`).
3. Persists `DestPositionId` / `DestClOrdId` / `DestFillPrice` and sets intent `DEMO_SENT` (`L576–594`).

`LiveCopyPage.tsx` empty-state **admits** this:

```56:58:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      {intents.length === 0 && (
        <p className="text-gray-500 text-sm">No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.</p>
      )}
```

On-disk dest residual (`D:\Prop\data\demo_copy_ledger.json`; public dest ids only):

| Field | Value |
|---|---|
| SourceLogin | `305750` |
| SourcePositionId | `21250421` |
| DestPositionId | `237339770` |
| DestClOrdId | `C20260818093047317` |
| DestFillPrice | `4390.2` |
| DestClosed | `false` |

That is a dest fill price on a dest position. `ExecuteDemoCopyAsync` also **hard-seeds** this same open row if missing (`L500–512`). Dashboard `OverviewDto.DestinationRealPnl` is constructed as literal `0` (`EfDashboardQueries.cs` `L44–46`) — a **display lie**, not proof dest cash is zero.

Live Pepperstone `1369850` is refused (`CTraderFixCopyOpen` `L39`). Demo dest `5328266` is **not** live capital. The claim as written is “sending now cannot be **the profit path**,” not “cannot be live-1369850 +EV.” Dest auto-send with fill prices **is** a destination profit path (demo book).

**Claim 4 verdict:** FAIL as written. Scoped PASS only if “profit path” is redefined as live `1369850`. This slot will not silently redefine it.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **FAIL** as written

Paper SHADOW fills are not dest cash. That subset is true:

```35:60:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
    public ShadowFill SimulateEntry(
        // ...
        return new ShadowFill
        {
            ShadowOrderId = shadowOrderId,
            Price = raw,
            Quantity = quantity,
            // ...
            SourceVsShadowSlippage = slippage
        };
```

`GenerateShadowIntentsAsync` writes `ShadowOrder` from `SimulateEntry` and status `SHADOW_ONLY` (`CopyTradingService.cs` `L336–359`). Overview `ShadowPnl` is the sum of `SourceVsShadowSlippage` (`EfDashboardQueries.cs` `L29`). That is slippage theater.

The claim as written is “**SHADOW on demo** is not destination profit.” `SHADOW` is also the **eligibility floor** for dest AUTO_ADMIT:

```81:85:D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs
        if (trader.State is TraderState.INSUFFICIENT_DATA or TraderState.EARLY_SCORE or TraderState.WATCH)
        {
            reason = "TRADER_NOT_SHADOW_YET";
            return false;
        }
```

`SHADOW`, `LIVE_CANDIDATE`, and `LIVE` are **not** blocked by that gate. Combined with `CompletedXauTrades >= 20`, `XauNetPnl > 0`, no size-pattern flags, and `CopyGroupFilter.IsDemoOrContest` (source group path contains `demo` / `contest`), `IsTraderEligible` returns true.

Roster then AUTO_ADMITs:

```72:80:D:\Prop\src\Domain\Copy\CopyRosterEngine.cs
        if (_policy.IsTraderEligible(trader, out var reason))
        {
            return new RosterDecision
            {
                Action = alreadyOnRoster ? RosterAction.Keep : RosterAction.Admit,
                Reason = alreadyOnRoster ? "KEEP" : "AUTO_ADMIT",
                FlattenDestination = false,
                AllowNewOpens = true
            };
        }
```

`ExecuteDemoCopyAsync` iterates `Status == "ADMITTED"` roster seats (`L542–544`) and sends dest `35=D`. It does **not** check `CurrentState == LIVE`. A SHADOW trader on a demo/contest source group who meets the 20-trade / net-PnL gate **is** dest-sent.

Scorer best output is `SHADOW` (`FromBaseline` `L200–201`; `AfterHighEarlyScore` `L209`). That is the state the dest hop is built to copy.

**Claim 5 verdict:** FAIL as written. Paper `ShadowOrder` ≠ dest $. SHADOW-on-demo **is** the dest admit/send path. Bundle may not claim otherwise.

---

## 6. Assigned UI (`LiveCopyPage.tsx`) — honesty vs. residual

Full file read (70 lines). GET-only. No POST. No FIX.

| UI fact | Line | Honesty |
|---|---|---|
| Title “Live copy portfolio” | `L10` | Cosmetic. Does not send. |
| `REAL_COPY armed` YES/NO | `L13` | Reflects `_runtime.RealCopyEnabled` (env-bound, not forced false). |
| LIVE traders / Live sends | `L15–16` | Counts. Scorer cannot mint LIVE; `LiveSends` counts `ExecutionIntents.SentAt != null` (not dest ledger). |
| Blockers header “Pepperstone cannot be filled” | `L24` | True for live `1369850`. **False comfort** if read as “no dest fill exists.” |
| Empty-state dest auto-send | `L57` | **Honest.** Matches `ExecuteDemoCopyAsync`. |

UI cannot prove claims 3–5. It disproves a reading of claim 4 that “the page cannot send, therefore nothing sends.”

---

## 7. Live GET (this slot)

Attempted:

- `GET http://localhost:5000/api/health` — tool SSRF blocked (`localhost` → `127.0.0.1`).
- `GET http://127.0.0.1:5000/api/copy/status` — tool SSRF blocked.

`launchSettings.json` binds API `http://localhost:5000`. Web client default `VITE_API_URL || http://localhost:5000`. No process DTO this slot.

Unproven from live GET (and therefore not used as PASS evidence): process `realCopyEnabled`, `realCopyArmed`, `quoteLoggedOn`, `tradeLoggedOn`, `liveTraders`, `liveSends`, `blockers[]`.

File-proven without GET: DI bind, `.env` L73 `true`, no post-logon write.

---

## 8. Risk to capital

| Book | Proven this slot |
|---|---|
| Live Pepperstone dest `1369850` | **NONE.** Identity gate refuses (`CTraderFixCopyOpen` `L39`; same gate on `CTraderFixDemoTestTrade` / `CTraderFixDemoMatrix`). `CTraderFixSession` cannot send `35=D`. |
| Demo dest `5328266` (public id; host `demo-*`, sender `demo.*`) | **Not absent.** Hosted `ExecuteDemoCopyAsync` → `Build("D")`. On-disk ledger has open dest pos `237339770` @ `4390.2`, `DestClosed=false`. Demo money / demo P&L can move. |
| Source MT5 books | Copy flatten is destination-only (`CopyRosterEngine` xml-doc). This slot did not attach Manager. |
| Paper SHADOW | Not dest cash. |

`risk_to_capital` for the JSON footer: **NONE on live `1369850`; demo dest hop can book dest P&L.**

This slot did not send `35=D`. This slot did not flip `REAL_COPY`. This slot did not print secrets.

---

## 9. What would be required to PASS the bundle

All five, as written, from a file or a live GET:

1. Delete or disconnect hosted `CTraderFixCopyOpen.Build("D")` (and the other two `Build("D")` sites) **or** change the claim to “`CTraderFixSession` has no `35=D` builder.”
2. Already true: `CanPromoteToLive => false`.
3. After successful FIX logon, assign `_runtime.RealCopyEnabled = false` (and prove via GET `/api/health` that the process boolean is false). Binding `.env` `true` without a re-pin **fails** this claim.
4. Remove or hard-disable `ExecuteDemoCopyAsync` dest send, **or** change the claim to “cannot be live-1369850 profit.” A constructor `DestinationRealPnl=0` is not enough.
5. Stop dest-sending ADMITTED `SHADOW` seats, **or** change the claim to “`ShadowOrder.SimulateEntry` is not dest cash.”

Until then the bundle is **FAIL**.

---

End of P500_VERIFY_35. Product source was not modified. No `35=D` was built or sent by this slot. No secrets printed. `REAL_COPY` was not flipped.
