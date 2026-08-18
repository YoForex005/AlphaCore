# P500_VERIFY_14 — Adversarial verifier (slot 14)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_14.md` |
| Agent / slot | P500 adversarial verifier **14** |
| Date | 2026-08-18 |
| Role | Independent re-read. **Do not trust other agents.** FAIL any claim not proved from a file or a live GET this slot. |
| Assigned files (full `read_file`) | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135/135**); `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (**212/212**); `D:\Prop\src\Domain\Risk\RiskEngine.cs` (**189/189**); `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (**70/70**) |
| Adjacent files (this pass) | `CTraderFixLogonHostedService.cs` (112/112); `DependencyInjection.cs` L39–42; `LiveRuntimeStatus.cs` (67/67); `CopyTradingService.cs` (625/625); `CopyTradingHostedService.cs` (44/44); `CTraderFixCopyOpen.cs` (gate + `Build("D")`); `CTraderFixDemoTestTrade.cs` / `CTraderFixDemoMatrix.cs` (sibling encoders); `ShadowCopyEngine.cs`; `EfTradingStore.PersistDemoShadowAsync`; `ReconstructionScoringService`; `EfDashboardQueries.GetOverviewAsync`; `XauUsdOneToOneCopyPolicy`; `CopyRosterEngine`; `DemoCopyLedger.cs`; `apps/api/Program.cs` `/api/copy/status` + `/api/settings`; `.env` boolean **only** |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Quoted only `REAL_COPY_EXECUTION_ENABLED=true` (boolean) and public dest ids `5328266` / `1369850`. |
| Secrets printed | **None.** Tag 554 / FIX / Manager / DB passwords not dumped. |
| Live attach / send this pass | **No.** No Manager Connect. No TLS. No Logon. No `35=D`. |
| Live GET this pass | `GET http://127.0.0.1:5000/api/copy/status` **blocked** (`web_fetch` SSRF on loopback; `open_page` failed). Process bits (`realCopyArmed`, quote/trade LoggedOn, dest PnL) are **not** remeasured. |
| SHA-256 this slot | **Not computed** (no shell). Evidence is line-cited file text. |

**Honesty rule:** wanting dest profit does not create an edge. A TLS Logon (`35=A`) is not a fill. A SHADOW label is not dest money. An armed `REAL_COPY` bit is not a ticket. Sibling `Build("D")` is not `CTraderFixSession`. A constructor `0` is not a venue mark. Copying all **8463** logins would copy `RISK_BLOCKED` losses. **FAIL any claim this slot cannot prove from a file or a live GET.**

Claims to confirm:

1. no `35=D` builder
2. `CanPromoteToLive` is false
3. `RealCopyEnabled` forced false after logon
4. sending now cannot be the profit path
5. SHADOW on demo is not destination profit

---

## 0. Verdict (binding)

**FAIL.**

Claims **1 (file-scoped), 2, 4 (live-capital / persist hop), 5** are proved from the files. Claim **3 is disproved** on disk: hosted logon does **not** force `RealCopyEnabled = false`. Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true` and DI binds it. Live GET of the process bit **failed** this slot — that does **not** rescue claim 3, because the claimed force-false assignment is absent from the only writer after logon.

One failed required confirmation ⇒ slot verdict **FAIL**. Copy hop still cannot emit a live Pepperstone ticket (`SAFE_BY_ABSENCE` on `CTraderFixSession` + persist `AllowFixSend=false` + `CanPromoteToLive => false` + CopyOpen refuse of `1369850`). That does **not** make claim 3 true.

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** on `CTraderFixSession.cs`. **FAIL** if read product-wide. | Assigned file 135/135: outbound tag 35 is `"A"` only (`BuildLogon` L96). Grep this file: `35=D` / `(35, "D")` / `Build("D")` / `NewOrderSingle` = **0**. `WriteAsync` = **1** (that Logon). Sockets `using`-disposed. Product residual: sibling `Build("D")` ×5 + hosted `ExecuteDemoCopyAsync`. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(TraderState current) => false` at `BaselineScorer.cs` L211. `FromBaseline` reachable set has **no** `LIVE` / `LIVE_CANDIDATE`. Persist copies `SuggestedState` (`ReconstructionScoringService` L140). Unit test asserts the same (`BaselineScorerTests` L26). |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | `CTraderFixLogonHostedService` L60–70 stamps QUOTE/TRADE and **logs** `_runtime.RealCopyEnabled`. Zero assignments `_runtime.RealCopyEnabled = false` under `src/`. Sole product write is DI L41 from config. `.env` L73 `=true`. `P500_PROFIT_SYNTHESIS` §2.1 / A015 “host forces false” are **STALE**. Process bit not live-GETted this slot. |
| 4 | sending now cannot be the profit path | **PASS** (live capital / assigned hop). Residual: demo dest can still `35=D`. | `CTraderFixSession` cannot send. Persist hop hard-`AllowFixSend=false` (L324) and `VenueReconciled=false` (L20) so Evaluate cannot approve an open. `CanPromoteToLive` is false. Overview `DestinationRealPnl` is constructor literal **0**. Wanting send ≠ +EV. **Not** proved: “no dest send exists.” |
| 5 | SHADOW on demo is not destination profit | **PASS** | `FromBaseline` SHADOW is a **score state**. `ShadowCopyEngine.SimulateEntry` writes in-memory `ShadowOrder` rows (`SHADOW_ONLY`). Trader `ShadowPnl` hardcoded **0** (`EfDashboardQueries` L118). Dest DTO literal **0**. Roster/demo hopper is a **different** path and still is not a measured dest-profit book. |

One-line:

```text
SLOT 14 FAIL. CTraderFixSession 35=A only; CanPromoteToLive=>false; SHADOW≠dest PnL; sending is not a live profit path. RealCopyEnabled is NOT forced false after logon (.env true + DI bind; host only logs). Live 1369850 SAFE_BY_ABSENCE; demo dest hopper residual. Live GET blocked.
```

---

## 1. Claim 1 — no `35=D` builder

### 1.1 Assigned file — PASS

Path: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`  
Read: **135 / 135**.

The type is a **one-shot TLS Logon probe**. Two types only: `CTraderFixSessionResult` + static `CTraderFixSession`. There is no order builder, no heartbeat, no quote subscribe, no sequence store, no `NewOrderSingle` identifier.

Only outbound MsgType:

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

`ssl.WriteAsync` is **one** call (L49) of those Logon bytes. `Extract(reply, "35")` (L55) is **inbound** only. `using TcpClient` / `await using SslStream` dispose on every return — no socket is kept for a later `35=D`.

Token census **this file only**:

| Pattern | Hits |
|---|---:|
| `35=D` | **0** |
| `(35, "D")` / `Build("D")` | **0** |
| `NewOrderSingle` | **0** |
| `(35, "A")` | **1** (`BuildLogon` L96) |
| `WriteAsync` | **1** (L49) |

The only product caller of `TryLogonAsync` is `CTraderFixLogonHostedService` (QUOTE **5211**, TRADE **5212**). That host never calls a `35=D` builder.

`RiskEngine.cs` and `LiveCopyPage.tsx` contain **0** FIX builders.

### 1.2 Product-wide residual — do not greenwash

Unqualified “the product has no `35=D` builder” is **false**.

| File | Encoder | Gate |
|---|---|---|
| `CTraderFixCopyOpen.cs` L95 / L142–156 | `Build("D", …)` | host must start `demo-`, sender `demo.`, account **≠ 1369850** |
| `CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | `Build("D", …)` | same demo-only refuse of live `1369850` |
| `CTraderFixDemoMatrix.cs` L93 (`SendD`) | `Build("D", …)` | same demo gate |

`CopyTradingHostedService` L30 calls `ExecuteDemoCopyAsync`, which calls `CTraderFixCopyOpen.SendAsync` (close L528, open L566). `NewOrderSingleImplemented => DemoDest` (`CopyTradingService` L50), **not** `const false` (older pins **STALE**). Lab `.env` satisfies `DemoDest` (demo host / demo sender / account **5328266**).

Literal string `35=D` in product `*.cs` is still **0** (encoders use `Build("D")` / `(35, type)`). That is not “no builder.”

**Claim 1 as scoped to `CTraderFixSession.cs`: PASS.**  
**Claim 1 as a whole-product sentence: FAIL.** This slot records the file-scoped PASS and the residual.

---

## 2. Claim 2 — `CanPromoteToLive` is false — PASS

Path: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`  
Read: **212 / 212**.

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

The method ignores `current`. It cannot return true.

`FromBaseline` (L189–207) reachable set:

| Condition | State |
|---|---|
| `CompletedXauTrades == 0` | `INSUFFICIENT_DATA` |
| `risk >= 80` **or** (martingale ∧ DD>0 ∧ NetPnl<0) | `RISK_BLOCKED` |
| `!earlyEligible` (N<3) | `INSUFFICIENT_DATA` |
| `quality >= 70 && risk < 40` | `SHADOW` |
| `quality >= 55` | `WATCH` |
| else | `EARLY_SCORE` |

**Never** `LIVE`, **never** `LIVE_CANDIDATE`. Enum still *defines* those values (`TraderState.cs` L8–10). Definition ≠ assignment.

Persist path copies the scorer output:

```126:140:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore
        {
            ...
            CurrentState = score.SuggestedState,
```

`grep CanPromoteToLive` under product `*.cs` = this method + `BaselineScorerTests` L26 (`Should().BeFalse()`). No other implementation.

Vacuous lock: nothing can promote to LIVE through this machine. That is **not** a measured LIVE-book of zero on a live GET (GET blocked). It **is** a file proof that the scorer cannot emit LIVE.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL

Required sentence: after FIX logon, runtime `RealCopyEnabled` is **forced false**.

### 3.1 Hosted logon does not assign the field

`CTraderFixLogonHostedService.ExecuteAsync` after both probes:

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

It **reads** `_runtime.RealCopyEnabled` for the log line. It does **not** write `false`. Password-skip path (L34–38) returns without touching the flag.

`grep` `RealCopyEnabled\s*=` under product `*.cs` = **1** hit:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

API `Program.cs` L10 loads `.env` via `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()`. `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret quoted).

`/api/health` L55 and `/api/settings` L76 expose `runtime.RealCopyEnabled` / `featureFlags["REAL_COPY_EXECUTION_ENABLED"]` **as bound**, not as a forced false.

`CTraderFixOptions.RealCopyExecutionEnabled` **defaults** false (L35). That default is unused by DI for `LiveRuntimeStatus`. Default ≠ post-logon force.

### 3.2 Live GET did not confirm the process bit

This slot could not `GET :5000/api/health` / `/api/settings` / `/api/copy/status` (SSRF / open_page fail). I therefore **do not** claim the running process bit is `true` or `false`. I **do** claim the files cannot force it false after logon.

Stale reports that still quote `_runtime.RealCopyEnabled = false` after logon (A015, `P500_PROFIT_SYNTHESIS` §2.1, many `P500_CODE_*` / `W500_RESEARCH_*`): **STALE vs this disk.**

**Claim 3: FAIL.**

---

## 4. Claim 4 — sending now cannot be the profit path — PASS (live / persist hop)

Interpretation proved: **live Pepperstone dest profit cannot be earned by sending from this stack now.** Enabling the flag / logging on TRADE is not a +EV send.

### 4.1 Assigned hop cannot send

`CTraderFixSession` outbound is Logon only (§1). Socket disposed. Official cTrader FIX 4.4 *defines* NewOrderSingle; this class does not implement it. Definition ≠ license.

### 4.2 Persist / risk hop cannot send a live ticket

`CopyTradingService`:

| Pin | Value | Line |
|---|---|---|
| `VenueReconciled` const | **`false`** | L20 |
| Evaluate `Reconciled` | that const | L304 |
| Persist `AllowFixSend` | **hard `false`** (ignores `decision.AllowFixSend`) | L324 |
| LIVE send branch | sets `LIVE_SEND_BLOCKED_UNIMPLEMENTED` — **still no write** | L330–332 |
| `CanPromoteToLive` | `false` (§2) so `CurrentState==LIVE` is not scorer-reachable | — |

`RiskEngine.Evaluate` **can** set `AllowFixSend=true` if `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Hosted Evaluate passes `Reconciled=false`, so increasing actions reject `VENUE_NOT_RECONCILED` (L84–85) and `AllowFixSend` stays false. The empty `RealExecutionEnabled==false` block (L91–93) is a comment, not a reject. Test `Real_flag_false_never_allows_fix_send` assumes `Reconciled=true` and still expects `AllowFixSend=false` when the flag is false — it does **not** prove the host force-false in claim 3.

Dashboard dest book:

```33:46:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        return new OverviewDto(
            ...
            shadowPnl,
            0,
            0,
            0,
            ...
            _runtime.RealCopyEnabled);
```

`OverviewDto.DestinationRealPnl` (12th arg, `DashboardModels.cs` L16) is constructor literal **0**. That is **not** a venue mark-to-market. It is an unmeasured dest book.

### 4.3 Residual that would make an unqualified “no send” FAIL

`LiveCopyPage.tsx` L56–57 (empty state) **admits** dest send on demo:

```56:58:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      {intents.length === 0 && (
        <p className="text-gray-500 text-sm">No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.</p>
      )}
```

Hosted 20s tick: `TickRosterAsync` → `GenerateShadowIntentsAsync` → `ExecuteDemoCopyAsync`. The last method **bypasses** `RiskEngine.Evaluate` and can emit `35=D` on demo dest (lot skip `MaxAutoLots=0.05`, refuse live `1369850`). File ledger `D:\Prop\data\demo_copy_ledger.json` (read this pass, no secrets) still has source **305750** / dest pos **237339770** / fill **4390.2** / `DestClosed=false`. That is dest **risk**, not a measured dest-profit series. Unrealized dest P&L on demo `5328266` is **UNKNOWN** this slot (no live GET, dashboard dest is hardcoded 0).

Copy-all **8463** remains negative-EV at source (cited pin, not re-GETted: scored XAU **−$154,425**, `RISK_BLOCKED` **29 / −$241,580**). Wanting a sender does not invert that book.

**Claim 4: PASS** as “send is not the live profit path.” **FAIL** if rewritten as “no dest send exists.”

---

## 5. Claim 5 — SHADOW on demo is not destination profit — PASS

`SHADOW` is `TraderState = 3`. `FromBaseline` assigns it when N≥3, quality≥70, risk<40. Quality formula (`BaselineScorer.Score` L152–160):

```text
quality = 50
        + 15 if NetPnl > 0
        + 10 if PF ≥ 1.2
        +  5 if PF ≥ 1.8
        + behavior * 0.2
        − risk * 0.25
        clamp 0..100; if N<3 cap 40
```

Quality can reach 70 with **NetPnl ≤ 0** (50 + 20 behavior, risk 0). SHADOW is **not** “source book is profitable,” let alone dest book.

Demo SHADOW persist (`EfTradingStore.PersistDemoShadowAsync` L267–333): if state ≠ SHADOW, return; else `ShadowCopyEngine.SimulateEntry` into `ShadowOrders` and `CopyIntent.Status = "SHADOW_ONLY"`. **No** `CTraderFixCopyOpen`. **No** tag 35.

`ShadowCopyEngine.SimulateEntry` (L35–61) prices off a `DestinationQuote` POCO with modeled 0.05-point latency. That is a **shadow fill**, not a venue execution. `MarkToMarket` has **0** product callers on the dest book.

Dashboard:

| Field | What it is |
|---|---|
| Overview `ShadowPnl` | `Sum(ShadowOrders.SourceVsShadowSlippage)` — slippage vs source, **not** dest $ |
| Overview `DestinationRealPnl` | literal **0** |
| Trader row `ShadowPnl` | literal **0** (`GetTradersAsync` L118) |

`LiveCopyPage` shows `SHADOW traders` as a **count** (`status?.shadowTraders`) and `Shadow fills` as another count. Counts are not dest PnL.

Roster may AUTO_ADMIT a SHADOW (or LIVE) demo/contest trader with N≥20 and `XauNetPnl>0` (`XauUsdOneToOneCopyPolicy` L74–112). That admission can later drive **demo** `35=D` (§4.3). The **SHADOW label itself** is still not dest profit. Policy `XAU_BOOK_NOT_PROFITABLE` is **source** XAU net, not dest mark.

**Claim 5: PASS.**

---

## 6. `RiskEngine` vs send (adjacent to claims 3–4)

Read: **189 / 189**.

Reject reasons that set `AllowFixSend=false` (via `Reject()` L180–188): `STOP_NEW_EXECUTION`, `EMERGENCY_FLATTEN_BLOCKS_NEW`, `VENUE_NOT_RECONCILED`, `VENUE_UNHEALTHY`, `QUOTE_MISSING`, `QUOTE_STALE`, `SPREAD_TOO_WIDE`, `PRICE_MOVED_TOO_FAR`, `SIGNAL_STALE`, `MAX_LOSS_PER_TRADER`, `MAX_DAILY_EXECUTION_LOSS`, `MAX_PORTFOLIO_DRAWDOWN`, `MAX_OPEN_POSITIONS`, `MAX_POSITION_QUANTITY`, `MAX_XAU_GROSS`, `MAX_XAU_NET`, `MAX_MARGIN_USAGE`, `MARTINGALE_BLOCK`, `ABNORMAL_SIZING_BLOCK`.

These would cut dest **loss if they sat in front of a sender**. Hosted demo hopper **does not call Evaluate**. Caps are not an edge. `MaxLossPerTrader=500` / `MaxDailyExecutionLoss=2000` do not mint expectancy.

`allowSend` (L147–150) is the only true path. It is **not** hard-false. Combined with claim 3 FAIL, a future Evaluate with `Reconciled=true` and armed flag **would** return `AllowFixSend=true`. Persist still overwrites false **today**. That overwrite is a persist pin, not a post-logon `RealCopyEnabled` force.

---

## 7. `LiveCopyPage.tsx` honesty (70/70)

The page is a **status chrome**, not a sender.

- Hooks: `useCopyStatus` → `GET /api/copy/status`; `useCopyIntents` → `GET /api/copy/intents`.
- Displays `realCopyArmed`, SHADOW/LIVE counts, `liveSends`, intents, shadow fills, QUOTE/TRADE up/down.
- Blocker list title: “Live send blockers (Pepperstone cannot be filled)” — honest for **live** dest; **not** honest if read as “demo dest cannot be filled.”
- Empty-state text documents demo auto-send. That text **contradicts** any “product cannot send at all” slogan and supports the §1.2 / §4.3 residual.

No `35=D` builder in the page. No POST. No order ticket UI.

---

## 8. What this slot did **not** prove

| Item | Status |
|---|---|
| Process `realCopyEnabled` / `realCopyArmed` | Live GET **blocked** |
| FIX QUOTE/TRADE LoggedOn now | Live GET **blocked** |
| Overview dest / shadow $ now | Live GET **blocked**; dest is constructor 0 **in source** |
| Book 8463 / −$154,425 / RISK_BLOCKED −$241,580 | **Cited** same-day pin (`P500_PROFIT_SYNTHESIS` / S007). Not re-GETted. |
| Manager census 18/8460 | Cited, not re-attached |
| Demo dest unrealized on 237339770 | Ledger proves an **open dest id + fill px**. PnL **unknown** |
| SHA-256 of the four files | No shell |

Cited pins are **not** this-slot measurements. They are not used to PASS any claim.

---

## 9. Risk to capital

| Book | Risk this slot |
|---|---|
| Live Pepperstone **1369850** | **NONE** — `CTraderFixSession` `SAFE_BY_ABSENCE`; CopyOpen L37–41 refuse; persist `AllowFixSend=false`; `CanPromoteToLive=>false`. This slot sent **0**. |
| Demo dest **5328266** | **Not** `SAFE_BY_ABSENCE` if the hosted 20s tick is running (`ExecuteDemoCopyAsync` → `Build("D")`). Ledger still shows **305750** dest **237339770** open. This slot sent **0**. |
| MT5 source book | Copy path does not flatten source (`CopyRosterEngine` dest-only flatten). |

---

## 10. Operating law (slot 14)

Do not treat `REAL_COPY_EXECUTION_ENABLED=true` as a send license or as a force-false after logon — the force is **gone**. Do not treat SHADOW / shadow fills / dest constructor `$0` as dest profit. Do not add a live `35=D` builder. Do not copy all 8463. Wanting profit is not an edge.

```text
P500_VERIFY_14 FAIL. 1 PASS (session 35=A only; product Build("D") residual). 2 PASS (CanPromoteToLive=>false). 3 FAIL (no post-logon RealCopyEnabled=false; .env true + DI). 4 PASS live profit-path (persist hop cannot send; dest DTO 0); demo hopper residual. 5 PASS (SHADOW≠dest $). Live GET blocked. Capital NONE on 1369850; demo dest not absent.
```
