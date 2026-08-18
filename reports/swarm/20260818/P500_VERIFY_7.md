# P500_VERIFY_7 — Adversarial verifier (slot 7)

| Field | Value |
|---|---|
| Slot | **7** |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_7.md` |
| Date | 2026-08-18 |
| Role | Adversarial verifier. No product source edited. |
| Assigned reads | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` (full `read_file` this slot) |
| Follow-up reads (to FAIL/PASS residuals) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `ShadowCopyEngine.cs`, `EfDashboardQueries.cs`, `CopyLifecycle.cs`, `DemoCopyLedger.cs`, `apps/api/Program.cs`, `BaselineScorerTests.cs`, `RiskEngineTests.cs`, `data/demo_copy_ledger.json` |
| Live GET | **Not obtained.** `open_page`/`web_fetch` of `http://localhost:5000/api/health`, `/api/copy/status`, `/api/overview`, `/api/settings` failed (SSRF / retrieve block). Any claim that needs a live process flag is **FAIL**. |
| Secrets printed | **None.** No passwords, no `.env` dump. Account **1369850** is the live-refuse literal already in source. |

**Honesty rule:** FAIL any claim not proven from a file this slot or a live GET. Older swarm text that said `_runtime.RealCopyEnabled = false` after logon is **stale vs HEAD**. `SAFE_BY_ABSENCE` is **stale** for demo dest: a `35=D` assembler exists and is called from the 20 s copy tick.

---

## 0. Overall verdict

**FAIL** as a five-claim confirm.

| # | Claim | Verdict |
|---|---|---|
| 1 | no `35=D` builder | **PASS** only inside `CTraderFixSession.cs`. **FAIL** if read as process-wide. |
| 2 | `CanPromoteToLive` is false | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** (file contradicts; live GET missing) |
| 4 | sending now cannot be the profit path | **FAIL** as written (demo dest `35=D` is a dest path). **PASS** only for live **1369850** / booked `DestinationRealPnl`. |
| 5 | SHADOW on demo is not destination profit | **PASS** for `ShadowCopyEngine.SimulateEntry` / `SHADOW_ONLY` rows. **FAIL** as “SHADOW state cannot dest-send”. |

Slot 7 does **not** rubber-stamp the packet. Two of five claims fail on HEAD. Live capital on **1369850** is still refused. Demo dest is not “no send.”

---

## 1. Claim 1 — no `35=D` builder

### 1.1 `CTraderFixSession.cs` (assigned file) — PASS

Full file read. The only outbound builder is `BuildLogon`:

```89:110:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
    private static string BuildLogon(
        string sender, string target, string senderSub, string targetSub,
        string username, string password, int seq)
    {
        var sendingTime = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            // ... 49/56/50/57/52/98/108/141/553/554 ...
        };
        return Assemble(fields);
    }
```

`TryLogonAsync` writes that logon, reads one buffer, extracts tag `35` for accept/reject (`A` vs other). No `Build("D")`, no NewOrderSingle, no tag `38`/`54`/`40`. **This class cannot assemble a NewOrderSingle.**

### 1.2 Process-wide — FAIL if that was the claim

Grep `Build("D"` / `(35, "D")` under `D:\Prop\src` this slot:

| File | What it does |
|---|---|
| `CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra)` then `Write` on TRADE **5212** |
| `CTraderFixDemoTestTrade.cs` L139, L163, L197 | `Build("D", …)` flatten / open / close |
| `CTraderFixDemoMatrix.cs` L93 | `SendD` → `Build("D", …)` |

`CTraderFixCopyOpen` is **product-called** from `CopyTradingService.ExecuteDemoCopyAsync` (open L566, close L528). `CopyTradingHostedService` runs that every **20 s** after roster + shadow.

Claim 1 is **true only for the named session type**. It is **false** as “this tree has no `35=D` builder.”

---

## 2. Claim 2 — `CanPromoteToLive` is false — PASS

Same compilation unit as `BaselineScorer`:

```187:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
public static class TraderStateMachine
{
    public static TraderState FromBaseline(...)
    {
        // returns INSUFFICIENT_DATA | RISK_BLOCKED | SHADOW | WATCH | EARLY_SCORE
        // never LIVE, never LIVE_CANDIDATE
    }

    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
}
```

`FromBaseline` quality≥70 and risk<40 → `SHADOW` (L200–201). High early score stays `SHADOW`. The `current` argument is unused.

Unit pin: `tests/Unit/BaselineScorerTests.cs` L21–26 — three disciplined winners → `SHADOW`; `CanPromoteToLive(...).Should().BeFalse()`.

**Proven.** Auto-LIVE from this scorer cannot happen.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — FAIL

### 3.1 Hosted logon does not assign the flag

`CTraderFixLogonHostedService.ExecuteAsync` after both `TryLogonAsync` calls:

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

It **reads** `_runtime.RealCopyEnabled` for the log line. It does **not** write `= false`. Persist path only updates `FixSessionState` rows.

Stale reports (`A015_enable_copy_gates.md` L92/L197/L224, and copies) claimed `_runtime.RealCopyEnabled = false` after logon. **That assignment is not on HEAD.**

### 3.2 DI binds env, it does not pin false

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Exact ordinal-ignore-case `"true"` arms the in-process flag. Default CLR `false` only if the key is missing or not `"true"`. That is **boot binding**, not “forced false after logon.”

`LiveRuntimeStatus.RealCopyEnabled` is a public setter. Grep of `src` assignments: DI construct only. No post-logon re-pin.

### 3.3 Live GET

Required to prove the **running** value. This slot’s GETs to `:5000` did not return a body. **Cannot confirm** `realCopyEnabled=false` on a live process. Claim 3 stays **FAIL**.

`apps/api/appsettings.json` has `FeatureFlags.LiveCopyEnabled: false` and no `REAL_COPY_EXECUTION_ENABLED` key. That file is not the DI key. Do not treat it as the runtime pin.

---

## 4. Claim 4 — sending now cannot be the profit path — FAIL as written

Split the paths. Do not merge them.

### 4.1 Paths that cannot send live Pepperstone — PASS

| Gate | File proof |
|---|---|
| Session type | `CTraderFixSession` sends `35=A` only |
| Live account refuse | `CTraderFixCopyOpen.SendAsync` returns if `account == "1369850"` or host/sender not `demo-` / `demo.` |
| Persist | `CopyTradingService` L324 `AllowFixSend = false` always on the `RiskDecisionRecord` |
| Intent LIVE branch | L330–332 requires `decision.AllowFixSend && CurrentState==LIVE && NewOrderSingleImplemented && VenueReconciled` then sets **`LIVE_SEND_BLOCKED_UNIMPLEMENTED`** — it still does not call `SendAsync` |
| `VenueReconciled` | `CopyTradingService` L20 `public const bool VenueReconciled = false` (the field passed into `RiskEngine`) |
| Risk allow | `RiskEngine` L147–150 `allowSend = RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` |
| Promotion | `CanPromoteToLive => false`; `FromBaseline` never `LIVE` |
| UI | `LiveCopyPage.tsx` has no POST / no send button |

`RiskEngineTests.Real_flag_false_never_allows_fix_send` pins `AllowFixSend=false` when `RealExecutionEnabled=false`.

`OverviewDto.DestinationRealPnl` is the constructor literal **0** (`EfDashboardQueries.GetOverviewAsync` L44). Booked dest P&L is not a venue rollup.

### 4.2 Path that **can** send now — dest demo `35=D` — FAIL the absolute

`CopyTradingHostedService` (registered in DI L59):

```27:30:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

`ExecuteDemoCopyAsync`:

- Returns 0 only if `!DemoDest` or blank password.
- `DemoDest` = host starts `demo-` **and** trade sender starts `demo.` **and** account ≠ `1369850`.
- Does **not** read `RealCopyEnabled`.
- Does **not** call `RiskEngine.Evaluate`.
- Calls `CTraderFixCopyOpen.SendAsync` which writes `Build("D", …)` (tag 35=`D`, 40=`1` market, 38=units).

`GetStatusAsync` summary when `DemoDest` (L76–77): *“Demo dest auto-copy ON. Eligible demo/contest opens send on the 20s tick…”*

`LiveCopyPage.tsx` L57 empty-state text: *“Demo dest auto-sends after a trader is ADMITTED…”* L24: *“Live send blockers (Pepperstone cannot be filled)”* — that string is live-1369850, not “no dest send.”

On-disk dest fill (not a live GET; file evidence only):

- `D:\Prop\data\demo_copy_ledger.json` — source `305750` / pos `21250421`, dest pos `237339770`, dest px `4390.2`, `DestClosed: false`
- `reports/swarm/20260818/DEMO_COPY_OPEN.json` — `OrderSent: true`, `Filled: true`, exec `150=F` / `39=2` on demo host

That is a **destination fill**. It is demo, not 1369850. It is still dest P&L on that demo book. The dashboard **lying** `DestinationRealPnl=0` does not erase the fill.

`NewOrderSingleImplemented => DemoDest` (`CopyTradingService` L50). Hosted log line “NewOrderSingle still unimplemented” is **stale vs this property**.

**Claim 4 as an absolute is FAIL.** Sending-now **can** be a dest path on demo. It **cannot** be the live-1369850 profit path. This slot did not observe a live process tick (GET blocked).

---

## 5. Claim 5 — SHADOW on demo is not destination profit — SPLIT

### 5.1 Simulated SHADOW book — PASS (not dest profit)

`GenerateShadowIntentsAsync` eligible states include `SHADOW` (L202). After risk it almost always sets `SHADOW_ONLY` and, if quote+Approve, `ShadowCopyEngine.SimulateEntry` → `ShadowOrders`. That is an in-process overlay (ask/bid ± optional 0.05). No socket write.

`EfDashboardQueries` L29: `shadowPnl = Sum(SourceVsShadowSlippage)` — slippage sum, not dest realized P&L. Dest real P&L stays **0**.

`RiskEngine` L90–93: when `RealExecutionEnabled==false`, comment only; no early return. `AllowFixSend` still false if the flag is false. Persist then **overwrites** `AllowFixSend=false` anyway.

`ShadowPortfolioPage.tsx` L6: “Live NewOrderSingle remains disabled.” That page does not send.

### 5.2 SHADOW **state** + ADMITTED roster — FAIL “cannot dest-profit”

`XauUsdOneToOneCopyPolicy.IsTraderEligible` blocks `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET`. It does **not** block `SHADOW`, `LIVE_CANDIDATE`, or `LIVE`. With ≥20 completed XAU, `XauNetPnl>0`, no size-pattern flags, demo/contest group → eligible.

`CopyRosterEngine.Decide` AUTO_ADMIT / KEEP when eligible. `TickRosterAsync` writes `CopyIntent` `Status=ADMITTED`.

`ExecuteDemoCopyAsync` walks **ADMITTED** roster seats and open XAU ≤ `MaxAutoLots` (0.05). **No `CurrentState == LIVE` check.** A `SHADOW` trader who is ADMITTED gets `CTraderFixCopyOpen` (`35=D`).

Early SHADOW from the scorer (3 trades, quality≥70) still fails eligibility (`NEED_MORE_XAU_HISTORY` at 20). That is a **20-trade** gate, not a “SHADOW cannot dest-send” gate.

So: **SHADOW_ONLY rows are not dest profit. SHADOW+ADMITTED on demo dest can be dest fills.** Claim 5 as a single sentence is **not proven**.

---

## 6. Assigned files vs the five claims (what they actually prove)

| File | Proves | Does not prove |
|---|---|---|
| `CTraderFixSession.cs` | No `35=D` builder **here**. Logon-only `35=A`. | Process has no sender. Post-logon flag pin. |
| `BaselineScorer.cs` | `CanPromoteToLive => false`. Max auto state `SHADOW`. | SHADOW cannot dest-send. |
| `RiskEngine.cs` | `AllowFixSend` needs flag+no KS+reconciled+healthy. Rejects force false. Empty body when flag false (does not itself send). | `ExecuteDemoCopyAsync` bypass. Runtime flag value. |
| `LiveCopyPage.tsx` | Display only. Shows `realCopyArmed` / `liveSends` / SHADOW count. Admits demo dest auto-sends in empty copy. No send control. | Runtime numbers (needs GET `/api/copy/status`). |

---

## 7. Residuals / stale text (do not recycle)

| Stale claim | HEAD |
|---|---|
| Hosted service sets `RealCopyEnabled=false` | **Absent.** Log only. |
| DI hard-pins `RealCopyEnabled=false` | **Absent.** Env `"true"` binds true. |
| No `35=D` assembler in tree | **False.** Three session helpers + product caller. |
| `NewOrderSingleImplemented=false` | **False** when `DemoDest`. Equals `DemoDest`. |
| Hosted log “NewOrderSingle still unimplemented” | **Stale string.** Copy host implements demo sender. |
| `SAFE_BY_ABSENCE` for dest | **False** for demo dest. **True** for 1369850 (account/host/sender gate). |
| `GetStatusAsync.VenueReconciled: DemoDest` vs `const VenueReconciled=false` | **Inconsistent.** Status DTO can report recon true while `RiskEngine` still gets the const false. |

---

## 8. Risk to capital

| Book | Risk |
|---|---|
| Live Pepperstone **1369850** | **NONE** this tree: host/sender/account refuse; scorer cannot LIVE; persist `AllowFixSend=false`; `CTraderFixSession` is logon-only. |
| Demo dest (default host `demo-…`, sender `demo.…`, account ≠ 1369850) | **NONZERO if the API/copy host is running** with a password: 20 s tick → `ExecuteDemoCopyAsync` → `35=D`. Ledger file already records a dest fill. Not live-firm capital; it **is** dest demo capital. |
| SHADOW overlay (`ShadowOrders`) | **NONE** (simulated). |
| `LiveCopyPage` | **NONE** (GET display). |

This slot **did not** attach to a live API, so it cannot say the tick is running **now**. It can say the code path exists and a dest fill was persisted on disk.

---

## 9. Slot 7 close

Confirmations that survive adversarial read:

1. `CTraderFixSession` has **no** `35=D` builder (`35=A` logon only).
2. `TraderStateMachine.CanPromoteToLive` is **`=> false`**. `FromBaseline` never returns `LIVE`.

Refusals:

3. `RealCopyEnabled` is **not** forced false after logon. DI binds env. Live GET missing.
4. Sending **can** be a dest path **now** on demo (`ExecuteDemoCopyAsync` + `CTraderFixCopyOpen`). It cannot be the 1369850 / booked-`DestinationRealPnl` path.
5. SHADOW **simulation** is not dest profit. SHADOW **state** on an ADMITTED demo/contest book **can** dest-send.

**Slot 7 verdict: FAIL.** Do not treat the five-claim packet as proven.
