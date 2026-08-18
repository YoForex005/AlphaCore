# P500_VERIFY_49 — Adversarial verifier (slot 49)

| Field | Value |
|---|---|
| Slot | **49** |
| Date | 2026-08-18 |
| Role | Adversarial verifier. Re-read assigned files this slot. Sibling `P500_VERIFY_*` / `P500_BOOK_*` are **not** evidence. |
| Assigned files | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135), `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212), `D:\Prop\src\Domain\Risk\RiskEngine.cs` (189/189), `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (70/70) |
| Adjacent (cite only to prove/fail) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `ShadowCopyEngine.cs`, `EfDashboardQueries.cs`, `EfTradingStore.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `CopyGroupFilter.cs`, `apps/api/Program.cs`, `CTraderFixOptions.cs`, `DemoCopyLedger.cs`, `D:\Prop\.env` L73 (boolean only), `D:\Prop\data\demo_copy_ledger.json` (public dest ids only) |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent this slot | **No** |
| `REAL_COPY` flipped this slot | **No** |
| Live GET this slot | **Blocked.** `web_fetch` / `open_page` SSRF-reject `localhost` and `127.0.0.1`. `GET http://127.0.0.1:5000/api/health`, `/api/copy/status`, `/api/settings`, `/api/overview` **not** executed. Runtime `realCopyArmed` / `liveSends` / SHADOW count **unverified**. |
| Secrets printed | **None** (boolean flags + public dest ids `5328266` / `1369850` / source login `305750` only) |

**Rule used:** FAIL any claim that cannot be proven from a file this slot re-read, or from a live GET this slot actually performed. Stale reports (`E002`, `CREDENTIALS_AND_COPY_STATUS`, any `NOS=const false`, any “flag forced false after logon”) are **not** evidence.

---

## Overall verdict: **FAIL**

The five-claim bundle does **not** hold as written. Claim 3 is **disproven** on disk. Claim 1 and claim 4 fail if read as product-wide statements. Claims 2 and 5 hold from assigned files.

| # | Claim | Verdict | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **FAIL** unscoped. **PASS_SESSION** on `CTraderFixSession` only (`35=A`). | File |
| 2 | `CanPromoteToLive` is false | **PASS** | File + unit lock |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** (disproven) | File + `.env` boolean |
| 4 | sending now cannot be the profit path | **FAIL** unscoped (demo dest hopper sends now). **PASS_LIVE_HOP** (Pepperstone `1369850` / persist LIVE branch cannot send). | File + on-disk ledger (not a live GET) |
| 5 | SHADOW on demo is not destination profit | **PASS** | File |

**One-liner:** session is logon-only `35=A`; promotion is hard-false; the runtime flag is **not** re-pinned after logon; demo dest `Build("D")` is hosted and can fill; SHADOW/slippage is not dest cash; live `1369850` is refused.

---

## 1. No `35=D` builder — **FAIL** (unscoped) / **PASS_SESSION**

Read in full: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`.

Grep of this file for tag 35: L55 inbound parse, L73 reject text, L96 outbound literal `"A"`. One builder: `BuildLogon`. One `ssl.WriteAsync` of that logon. Socket is `using`-disposed after the reply. Zero `NewOrderSingle`, zero `Build("D")`, zero tag 38, zero `OrderQty`.

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

**Assigned-file claim holds.** A TLS Logon is not a fill and not dest P&L.

**Unscoped claim fails.** The same `Sessions` directory builds MsgType `D` and writes it to TRADE `:5212`:

| File | Sites |
|---|---|
| `CTraderFixCopyOpen.cs` L95 | `Build("D", …)` after SecurityList `35=x`/`35=y`. Called by `CopyTradingService.ExecuteDemoCopyAsync`. |
| `CTraderFixDemoTestTrade.cs` L139, L163, L197 | flatten / market / close |
| `CTraderFixDemoMatrix.cs` L93 | local `SendD` → `Build("D", …)` |

`CTraderFixCopyOpen` L37–L42 refuses non-`demo-` host, non-`demo.` sender, and live account `1369850`. Demo dest `5328266` is **inside** the allow set. Hosted tick (`CopyTradingHostedService` 8s delay + 20s loop) calls `ExecuteDemoCopyAsync` whenever `DemoDest` is true.

Therefore “no `35=D` builder” as a product claim is **false**. Older “product `35=D` = 0 / no sender” lines are **STALE**.

---

## 2. `CanPromoteToLive` is false — **PASS**

Read in full: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`.

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

Unconditional. Parameter `current` is unused. Product callers of `CanPromoteToLive`: **none** (definition + `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` + a `_tmp` eval). The unit lock asserts `CanPromoteToLive(...) == false` after three disciplined winners land in `SHADOW`.

`FromBaseline` never emits `LIVE` or `LIVE_CANDIDATE`:

```189:207:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
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
```

Ceiling auto-state is **SHADOW**. Quality is a source-shape score (`NetPnl>0` +15, PF, `behavior*0.2`, `-risk*0.25`), not dest expectancy. `MaeMfeQuality` is hardcoded `Unavailable`.

**Adversarial remainder (does not fail the claim):** nothing in the copy hop calls `CanPromoteToLive`. `XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET` and **accepts** `SHADOW` (and would accept `LIVE_CANDIDATE`/`LIVE` if those states existed). Hard-false promotion does **not** block demo dest send.

---

## 3. `RealCopyEnabled` forced false after logon — **FAIL** (disproven)

`CTraderFixSession` never mentions the flag.

The logon host **reads** the bit after QUOTE+TRADE `TryLogonAsync` and logs it. It does **not** assign `false`.

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

The only product write of `LiveRuntimeStatus.RealCopyEnabled` is DI construction:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

API boot (`apps/api/Program.cs` L10, L13): `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()`. Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret quoted). After a successful logon the bit **stays** whatever DI bound.

Grep `RealCopyEnabled =` under `*.cs`: **one** assignment (DI L41). No post-logon re-pin exists.

`CTraderFixOptions.RealCopyExecutionEnabled` still defaults `false` — a **different identifier**, unused by `LiveRuntimeStatus`. FIX worker reads `CTrader:RealCopyExecutionEnabled` (default false) and still does not send; that is not the hosted API flag.

`/api/health` and `/api/settings` echo `runtime.RealCopyEnabled`. This slot did **not** GET them. A process with that `.env` loaded would report **true**. Any report that says “forced false after logon” is **STALE**.

Armed ≠ ticket. Fail of this claim is a failed safety pin, not a live fill.

---

## 4. Sending now cannot be the profit path — **FAIL** (unscoped) / **PASS_LIVE_HOP**

### What the assigned files prove

- `CTraderFixSession` can send only `35=A`. Logon is not dest profit.
- `RiskEngine` `AllowFixSend` is `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). `Reject` always sets `AllowFixSend=false` (L187). Hosted Evaluate passes `Reconciled = CopyTradingService.VenueReconciled` which is `const false` (service L20), so new exposure is `VENUE_NOT_RECONCILED` before approve. Persist then **overwrites** `AllowFixSend = false` (service L324).
- The LIVE send branch (service L330) requires `decision.AllowFixSend && score.CurrentState == LIVE && NewOrderSingleImplemented && VenueReconciled`. `FromBaseline` never returns `LIVE`. `VenueReconciled` is const false. Branch is dead.
- `LiveCopyPage.tsx` cannot send. It GETs `/api/copy/status` and `/api/copy/intents`. Blocker header: “Live send blockers (Pepperstone cannot be filled)”.
- `OverviewDto.DestinationRealPnl` is a **constructor `0`** (`EfDashboardQueries` L44). That is not a marked dest book.

Unit lock: `RiskEngineTests.Real_flag_false_never_allows_fix_send` — when `RealExecutionEnabled=false` and `Reconciled=true`, outcome is `Approve` and `AllowFixSend` is false. `MaxSlippage` (L18, default 1.5) is **unread** by `Evaluate` (single src hit).

### Why the unscoped claim still FAILs

`CopyTradingService.NewOrderSingleImplemented => DemoDest` (L50). `DemoDest` is demo host + demo sender + account ≠ `1369850`. `CopyTradingHostedService` every 20s: `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**.

`ExecuteDemoCopyAsync` (L483–605):

- Returns 0 if `!DemoDest`.
- Calls `CTraderFixCopyOpen.SendAsync` (which `Build("D")`) for dest closes and dest opens.
- **Does not** call `RiskEngine.Evaluate`.
- Cap `MaxAutoLots = 0.05` is a **source skip**, then 1:1 send of remaining lots (`AllocationFactor = 1m`).
- Writes `DEMO_SENT` + dest pos/clOrd/px onto the intent.

`LiveCopyPage` empty-state is honest about that hop:

```56:58:D:\Prop\apps\web\src\pages\LiveCopyPage.tsx
      {intents.length === 0 && (
        <p className="text-gray-500 text-sm">No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.</p>
      )}
```

On-disk `D:\Prop\data\demo_copy_ledger.json` still has dest pos `237339770` for source `305750` / `21250421`, lots `0.01`, fill `4390.2`, `DestClosed=false`. That is dest market activity, not paper SHADOW. It is **not** a live GET this slot; it is dest-send evidence on disk. `ExecuteDemoCopyAsync` L500–511 will re-seed that same row if missing.

So:

- **Sending now cannot be the live-capital / booked-Pepperstone profit path.** Session is `35=A`. Live account refused. Persist LIVE branch dead. Dest DTO is constructor 0.
- **Sending now can be a demo-dest ticket** on `5328266`. That ticket can have dest P&L even if the dashboard writes `0`. Constructor 0 is **not** a mark-to-market of a flat book.

Unscoped “sending now cannot be the profit path” is therefore **not proven** and is contradicted by the hosted hopper. FAIL.

Wanting dest profit is not an edge. `AllocationFactor=1m` + no Evaluate + unread `MaxSlippage` + no standing QUOTE tape on the official session is not a measured +EV path. It is still a **send**.

---

## 5. SHADOW on demo is not destination profit — **PASS**

Assigned scorer: SHADOW is `quality >= 70 && risk < 40 && CompletedXauTrades >= 3`. It is a **source XAU shape** state. `NetPnl` in `FeatureSnapshot` is reconstructed **source** PnL.

Assigned UI: `LiveCopyPage` shows `shadowTraders` / `shadowFills` as separate cells from `liveSends` / `LIVE traders`. No dest P&L column.

Adjacent proof that SHADOW rows are not dest fills:

- `ShadowCopyEngine.SimulateEntry` / `SimulateExit` price off a `DestinationQuote` bid/ask + 0.05 modeled slip. **No socket.**
- `EfTradingStore.PersistDemoShadowAsync`: if `state != SHADOW` return; else `Status = "SHADOW_ONLY"` + `SimulateEntry`.
- `CopyTradingService.GenerateShadowIntentsAsync`: non-LIVE path `Status = "SHADOW_ONLY"` + same simulate.
- `EfDashboardQueries.GetOverviewAsync` L29 / L43–44: `ShadowPnl = Sum(ShadowOrders.SourceVsShadowSlippage)`; `DestinationRealPnl = 0` literal.

Demo **group** membership (`CopyGroupFilter.IsDemoOrContest`) is the admission filter, not dest P&L. A SHADOW name on a demo/contest group can `AUTO_ADMIT` (`CopyRosterEngine` L72–80 + policy L80–85) and then hit `ExecuteDemoCopyAsync`. Those dest fills (if any) are **destination positions on demo `5328266`**, not the SHADOW score and not `ShadowPnl`.

Claim 5 as written holds for the **score / shadow-order metric**. It does **not** mean “no demo dest ticket can exist for a SHADOW name.”

---

## Risk to capital

| Surface | This slot |
|---|---|
| Live Pepperstone `1369850` | **NONE** (`SAFE_BY_ABSENCE`). Session is `35=A` only. CopyOpen refuses that account. LIVE hop dead (`CanPromoteToLive` false, `VenueReconciled` false, persist `AllowFixSend=false`). |
| Demo dest `5328266` | **NOT NONE.** Hosted 20s `ExecuteDemoCopyAsync` → `Build("D")` when `DemoDest`. Ledger row `305750` / `237339770` still open on disk. This slot did not send. |
| Flag after logon | **Armed possible.** `.env` L73 `true`; DI binds; host does not re-pin. Armed ≠ fill. Armed + a live sender would be a loss path, not a proven edge. |

`SAFE_BY_ABSENCE` applies to **live** dest only. Do not stamp it on demo dest.

---

## Stale lines this slot must not recycle

| Claim / artifact | HEAD vs stale |
|---|---|
| Product-wide “no `35=D` / no sender” (`E002`, early CREDENTIALS) | Three sibling builders; hosted hopper wired. |
| `NewOrderSingleImplemented = false` const | HEAD is `=> DemoDest` (`CopyTradingService` L50). |
| `RealCopyEnabled` forced false after logon | **Gone.** Host logs only. DI binds `.env` `true`. |
| Dest P&L “measured $0” | Constructor literal, not a venue mark. |
| `NOS` off means no dest send | Demo dest send bypasses the persist LIVE branch. |

---

## Method / honesty

- Assigned four files re-read in full this slot. Adjacent hop files re-read for claims 1, 3–5.
- No live GET. Process `realCopyArmed` / `liveSends` / current SHADOW count **unverified**.
- No product edit. No password, FIX `554`, Manager secret, or connection string printed.
- Empty-PASS? **No.** All four assigned files have substance.
- Copy-all catalog PnL figures from sibling BOOK reports were **not** re-measured and are **not** used as PASS/FAIL evidence here.

**DONE for this slot:** reviewer-grade **FAIL** on the five-claim bundle (claim 3 disproven; claims 1 and 4 fail unscoped). Live capital on `1369850` remains **NONE**.
