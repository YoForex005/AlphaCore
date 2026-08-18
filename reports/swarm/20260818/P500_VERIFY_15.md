# P500_VERIFY_15 — Adversarial verifier (slot 15)

| Field | Value |
|---|---|
| Slot | **15** |
| Date | 2026-08-18 |
| Role | Adversarial verifier. Read the four named sources. Confirm five claims. FAIL any claim not proven from a file or a live GET. |
| Workspace | `D:\Prop` |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_15.md` |
| Product / test source edited | **No** |
| Secrets printed | **None.** Tag 554 / FIX / manager passwords not dumped. `.env` quoted only as public dest ids (`demo-us-eqx-01.p.c-trader.com`, `5328266`, `1369850`) and boolean `REAL_COPY_EXECUTION_ENABLED=true`. |
| Live GET this slot | **UNPROVEN.** `web_fetch` / `open_page` of `http://127.0.0.1:5000/api/health` and `/api/copy/status` were **SSRF-blocked** on loopback. No shell. Runtime `realCopyEnabled` after logon is therefore **not** live-probed. File bind is enough to fail claim 3. |

Assigned reads (full file, this slot):

| File | Lines read | Role |
|---|---:|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | **135 / 135** | FIX session / logon |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | **212 / 212** | `TraderStateMachine.CanPromoteToLive` |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | **189 / 189** | `AllowFixSend` |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | **70 / 70** | live-copy UI |

Adjacent files read only to attack or support a claim (not used as a substitute for the four named sources):

- `CTraderFixLogonHostedService.cs` (logon does **not** write `RealCopyEnabled`)
- `DependencyInjection.cs` L39–42 (the **only** `RealCopyEnabled =` assignment in `*.cs`)
- `LiveRuntimeStatus.cs`
- `CopyTradingService.cs` (demo dest `35=D` hop; persist `AllowFixSend = false`)
- `CopyTradingHostedService.cs` (20s tick calls `ExecuteDemoCopyAsync`)
- `CTraderFixCopyOpen.cs` / `CTraderFixDemoTestTrade.cs` / `CTraderFixDemoMatrix.cs` (`Build("D", ...)`)
- `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `CopyGroupFilter.cs`
- `EfDashboardQueries.cs` (`ShadowPnl` / hardcoded `DestinationRealPnl = 0`)
- `apps/api/Program.cs`, `apps/web/src/api/hooks.ts` (GET-only copy endpoints)
- `D:\Prop\.env` L49–50, L64, L73 (boolean + public dest ids only)
- On-disk dest fill: `D:\Prop\reports\swarm\20260818\DEMO_COPY_OPEN.json`, `D:\Prop\data\demo_copy_ledger.json`

Stale reports that **must not** be reused as evidence: `P500_BOOK_102.md` claims `NewOrderSingleImplemented` is `const false` and workspace `35=D` = 0. HEAD now has `NewOrderSingleImplemented => DemoDest` and three `Build("D")` senders.

---

## 0. Verdict

**FAIL.**

| # | Claim | Result | Why |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** only inside `CTraderFixSession.cs`. **FAIL** if claimed for the product. | Assigned file builds `(35, "A")` only. Three sibling session types build `Build("D", ...)`. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive` is `=> false`. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL (disproven)** | Only assignment is DI from env. `.env` is `true`. Logon host **reads** the flag and does not write it. |
| 4 | sending now cannot be the profit path | **FAIL (disproven as a product claim)** | `ExecuteDemoCopyAsync` sends dest `35=D` on the 20s tick when `DemoDest`. Risk persist path is not the dest hop. |
| 5 | SHADOW on demo is not destination profit | **PASS_NARROW / FAIL_IF_OVERCLAIMED** | `ShadowCopyEngine.SimulateEntry` + dashboard `ShadowPnl` are **not** dest P&L. SHADOW **state** traders **can** be ADMITTED and then dest-sent. |

Overall slot verdict is **FAIL** because claims **3** and **4** do not hold on HEAD, and claim **1** is true only if scoped to `CTraderFixSession.cs`. Live GET of `/api/health` / `/api/copy/status` was **not** obtained; those runtime fields are **not** cited as proven.

One-line:

```text
CTraderFixSession is 35=A only. CanPromoteToLive=>false. RealCopyEnabled is NOT forced false after logon (.env true, DI binds, logon does not re-pin). Demo dest 35=D via CTraderFixCopyOpen IS a dest path. Shadow book is not dest P&L. FAIL.
```

---

## 1. Claim 1 — no `35=D` builder

### 1.1 Assigned file: PASS

`CTraderFixSession.cs` (135/135) is a one-shot TLS logon probe.

- Single outbound tag 35: `(35, "A")` at L96 inside `BuildLogon`.
- `ssl.WriteAsync` at L49 writes that logon only.
- Inbound `Extract(reply, "35")` at L55 accepts `"A"` as logon OK; any other type is `"Logon rejected"`.
- `grep` `(35,` on this file: **1** hit, value `"A"`.
- `grep` `35=D` / `Build("D"` / `NewOrderSingle` on this file: **0**.
- `using TcpClient` / `await using SslStream` dispose on every return. No standing TRADE socket for a later D.

This file **cannot** be a NewOrderSingle builder.

### 1.2 Product: FAIL if the claim is global

Same directory, same namespace, **not** the assigned file:

| File | Builder | Wired from product hop? |
|---|---|---|
| `CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra)` | **Yes.** `CopyTradingService.ExecuteDemoCopyAsync` L528 and L566. |
| `CTraderFixDemoTestTrade.cs` L139, L163, L197 | `Build("D", ...)` | Tool `tools/DemoFixTestTrade`, not the API tick. |
| `CTraderFixDemoMatrix.cs` L93 | `SendD` → `Build("D", ...)` | Same tool (`--matrix`). |

`CTraderFixCopyOpen.Build` at L142–156 is a generic FIX assembler: first field is `(35, type)`. Callers pass `"D"`. That **is** a `35=D` builder.

Adversarial note: a verifier who only greps the literal string `35=D` will miss `Build("D", ...)`. HEAD has **zero** product `*.cs` matches for the literal `35=D` and **five** `Build("D"` writes.

**Claim 1 scoped to `CTraderFixSession.cs`: PASS. Claim 1 as “this process has no 35=D builder”: FAIL.**

---

## 2. Claim 2 — `CanPromoteToLive` is false

**PASS.** Proven in `BaselineScorer.cs` L211:

```csharp
public static bool CanPromoteToLive(TraderState current) => false;
```

`FromBaseline` (L189–207) can emit `INSUFFICIENT_DATA`, `RISK_BLOCKED`, `SHADOW`, `WATCH`, `EARLY_SCORE`. It **never** returns `LIVE` or `LIVE_CANDIDATE`.

`AfterHighEarlyScore()` (L209) returns `TraderState.SHADOW`.

Unit pin: `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` asserts `SuggestedState == SHADOW` and `CanPromoteToLive(...) == false`.

**Caveat (does not fail the claim):** `CanPromoteToLive == false` does **not** gate `ExecuteDemoCopyAsync`. Demo dest send keys off roster `ADMITTED`, not `TraderState.LIVE`. Claim 2 is still true as written.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon

**FAIL. Disproven from files.** Cannot be rescued by a live GET even if one existed, unless the running process were a different tree.

### 3.1 The only assignment

Workspace `grep` `RealCopyEnabled\s*=` on `*.cs` / `*.tsx` / `*.ts` / `*.json`: **one** write.

`D:\Prop\src\Infrastructure\DependencyInjection.cs` L39–42:

```csharp
var runtime = new LiveRuntimeStatus
{
    RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
};
```

Default `bool` is false **only if** the env/config string is not `"true"`.

### 3.2 Config is true

`D:\Prop\.env` L73 (boolean only): `REAL_COPY_EXECUTION_ENABLED=true`.

API boot (`apps/api/Program.cs` L10) calls `EnvFile.FindAndLoad()`, which loads `D:\Prop\.env` (absolute fallback in `EnvFile.cs` L14). Then `AddEnvironmentVariables()` and `AddTraderIntelligence`. A process started from this tree **binds `RealCopyEnabled = true` at construction**.

### 3.3 Logon does not force it false

`CTraderFixLogonHostedService.ExecuteAsync` after both `TryLogonAsync` calls:

- Writes `_runtime.Quote.*` and `_runtime.Trade.*` (L60–67).
- Logs `_runtime.RealCopyEnabled` (L68–70). **Read, not write.**
- Persists `FixSessionState` rows. No `RealCopyEnabled` column update.

`CTraderFixSession.TryLogonAsync` has no `LiveRuntimeStatus` parameter and never mentions the flag.

`CTraderFixOptions.RealCopyExecutionEnabled` defaults `false` (L35) and is **not** the object DI copies into `LiveRuntimeStatus`. Different type. Irrelevant to the claim.

### 3.4 Live GET

Need `GET /api/health` (`realCopyEnabled`) and `GET /api/settings` (`featureFlags.REAL_COPY_EXECUTION_ENABLED`) to prove the **running** process. This slot: **SSRF-blocked**. File bind is still a **disproof** of “forced false after logon.” A live `true` would confirm; a live `false` would only show a different process or a different env, not a post-logon force-off in this source.

**Claim 3: FAIL.**

---

## 4. Claim 4 — sending now cannot be the profit path

**FAIL** as a product claim. **PASS_NARROW** only if rewritten as “`CTraderFixSession` sending cannot be the profit path.”

### 4.1 What the four named files prove

| Source | Send / profit? |
|---|---|
| `CTraderFixSession` | Sends `35=A` only. Not a fill. Not dest P&L. |
| `RiskEngine` | `AllowFixSend` is `true` only if `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Rejects set `AllowFixSend=false`. Empty `if (RealExecutionEnabled == false …)` at L90–93 does **not** reject; it still later denies send when the flag is false. |
| `BaselineScorer` | No FIX. Caps state at SHADOW. |
| `LiveCopyPage.tsx` | GET `/api/copy/status` + `/api/copy/intents` only (`hooks.ts` L60–65). No POST. Empty copy (L57): **“Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position.”** The page itself contradicts “cannot send.” |

`RiskEngine` with `RealExecutionEnabled=true` **can** return `AllowFixSend=true`. That is not a send. The persist hop overwrites it.

### 4.2 Persist hop is not the dest hop

`CopyTradingService.GenerateShadowIntentsAsync`:

- Passes `RealExecutionEnabled = _runtime.RealCopyEnabled` into `RiskEngine` (L303).
- Passes `Reconciled = VenueReconciled` where `VenueReconciled` is **`const false`** (L20, L304). Increasing actions therefore reject `VENUE_NOT_RECONCILED` (`RiskEngine` L84–85).
- Persists `AllowFixSend = false` **unconditionally** (L324).
- LIVE send branch (L330) also requires `score.CurrentState == LIVE` and `VenueReconciled`. Both fail on HEAD (`CanPromoteToLive => false`; const false). Status becomes `"SHADOW_ONLY"` (L336).

So the **risk-intent** path cannot emit a live Pepperstone ticket. That is **not** the only hop.

### 4.3 Dest hop that bypasses risk

`CopyTradingHostedService` L28–30, every 20s:

```text
TickRosterAsync → GenerateShadowIntentsAsync → ExecuteDemoCopyAsync
```

`ExecuteDemoCopyAsync` (`CopyTradingService` L483–605):

- Returns 0 only if `!DemoDest` or password empty.
- `DemoDest` (L45–48) is true when host starts with `demo-`, trade sender starts with `demo.`, and account ≠ `1369850`.
- `.env` L49–50, L64: host `demo-us-eqx-01.p.c-trader.com`, account `5328266`, sender `demo.pepperstone.5328266`. **DemoDest is true on this tree.**
- Does **not** read `RealCopyEnabled`.
- Does **not** read `CanPromoteToLive`.
- Does **not** read `RiskDecision.AllowFixSend`.
- Does **not** require `TraderState.LIVE`.
- Calls `CTraderFixCopyOpen.SendAsync` (L528 close, L566 open) which writes `Build("D", ...)`.

Open gate: roster row `Status == "ADMITTED"`, source XAU still open, `MaxVolumeLots <= 0.05`, `CopyLifecycle.ShouldOpenDest`. Close gate: ledger dest still open and source completed.

`GetStatusAsync` L67 **lies about recon**: DTO `VenueReconciled: DemoDest` while the const used by risk is `false`. UI can show venue reconciled on demo while risk still rejects.

### 4.4 On-disk dest fill (file, not this-slot send)

`DEMO_COPY_OPEN.json`: `OrderSent=true`, `Filled=true`, account `5328266`, host demo, `ClOrdId=C20260818093047317`, `PosId=237339770`, `LastPx=4390.2`, `35=8` / `150=F` / `39=2`.

`data/demo_copy_ledger.json`: same dest id, `DestClosed: false`.

That is destination inventory on the demo venue. It is **not** live account `1369850` (`CTraderFixCopyOpen` L37–42 refuses that account). It **is** a dest P&L path.

`EfDashboardQueries.GetOverviewAsync` L44 still hardcodes `DestinationRealPnl = 0`. Dashboard dest P&L is **not** proof that dest tickets do not exist. It is proof the dashboard does not mark them.

**Claim 4: FAIL.** Sending now (demo dest auto-copy) **can** be a destination P&L path. It is not live Pepperstone. It is not `CTraderFixSession`. It is not the SHADOW simulator.

---

## 5. Claim 5 — SHADOW on demo is not destination profit

**PASS** for the shadow **book**. **FAIL** if read as “a SHADOW trader on demo cannot produce dest P&L.”

### 5.1 Shadow book is not dest P&L — PASS

`GenerateShadowIntentsAsync` L336–359: non-LIVE (always, given claim 2) intents become `SHADOW_ONLY`. Fills are `_shadow.SimulateEntry(...)` written to `ShadowOrders`.

`ShadowCopyEngine.SimulateEntry` (L35–61): prices from a `DestinationQuote` bid/ask plus a 0.05-point model delay. No socket. No tag 35.

Dashboard `ShadowPnl` (`EfDashboardQueries` L29) = `Sum(ShadowOrders.SourceVsShadowSlippage)`. That is modeled slippage, not dest realized P&L.

`DestinationRealPnl` is the constructor literal `0` (L44). Overview shows it (`OverviewPage.tsx` L27). That number is **not** a mark-to-market of `demo_copy_ledger.json`.

`LiveCopyPage` L14 displays `status.shadowTraders`. That is a **count of `TraderState.SHADOW`**, not dest profit.

### 5.2 SHADOW state can still reach dest send — FAIL if overclaimed

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET` (L81–85). SHADOW, LIVE_CANDIDATE, and LIVE pass the state gate.

Other gates: not blocked, no size pattern, `CompletedXauTrades >= 20`, `XauNetPnl > 0`, demo/contest group (`CopyGroupFilter`).

`CopyRosterEngine.Decide` admits when `IsTraderEligible` (L72–80). `TickRosterAsync` writes `Status = "ADMITTED"`.

`ExecuteDemoCopyAsync` then dest-sends **ADMITTED** seats. It does not re-check `CurrentState == LIVE`.

Therefore: **SHADOW + 20 XAU + book > 0 + demo/contest group + lots ≤ 0.05 ⇒ dest `35=D`.** The shadow **simulator** is not that profit. The **trader state named SHADOW** can be on the dest hop.

`ShadowPortfolioPage.tsx` L6 (“Live NewOrderSingle remains disabled”) is **stale relative to HEAD** `ExecuteDemoCopyAsync`.

**Claim 5: PASS_NARROW** (shadow fills / `ShadowPnl` ≠ dest profit). **Not** a proof that SHADOW-state demo traders cannot be dest-copied.

---

## 6. `LiveCopyPage.tsx` (assigned) — honesty notes

Read 70/70. Display only.

- L13: `REAL_COPY armed` = `status.realCopyArmed` = `_runtime.RealCopyEnabled` (not forced false; see claim 3).
- L23–27: “Live send blockers (**Pepperstone cannot be filled**)” — true for account `1369850`. False as a statement that **no** dest can be filled.
- L57: explicitly documents demo dest auto-send. That line alone fails a naive “UI cannot send / cannot profit” reading.
- No button, no `fetch` POST, no FIX client in the page.

---

## 7. `RiskEngine.cs` (assigned) — what it does **not** prove

- Default limits exist. They are unused by `ExecuteDemoCopyAsync`.
- `RealExecutionEnabled == false` does not reject (L90–93 empty body). `AllowFixSend` later becomes false unless the four-AND at L147–150 holds.
- With `.env` `REAL_COPY_EXECUTION_ENABLED=true`, a caller who also passed `Reconciled=true` and `VenueHealthy=true` would get `AllowFixSend=true`. HEAD persist caller passes `Reconciled=false` and then **overwrites** `AllowFixSend=false` anyway.
- Tests pin `RealExecutionEnabled=false` ⇒ `AllowFixSend=false` (`RiskEngineTests.Real_flag_false_never_allows_fix_send`). They do **not** pin the demo dest bypass.

---

## 8. Live GET (required bar)

Attempted this slot:

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | `web_fetch` SSRF blocked |
| `http://127.0.0.1:5000/api/copy/status` | `web_fetch` SSRF blocked |
| `http://localhost:5000/api/health` | `open_page` failed |

**Not cited:** prior-session `/api/overview` numbers from other P500 reports. Those are not this slot’s GET.

Unproven at runtime (do not claim):

- process `realCopyEnabled` after a successful TRADE logon
- `quoteLoggedOn` / `tradeLoggedOn`
- live `shadowTraders` / `liveTraders` / `liveSends`
- whether `ExecuteDemoCopyAsync` has sent **this process** a new D since `DEMO_COPY_OPEN.json`

`liveSends` would also undercount dest hops: it counts `ExecutionIntents` with `SentAt != null` (`CopyTradingService` L57). `ExecuteDemoCopyAsync` does not write `ExecutionIntent`. A GET of `liveSends=0` would **not** prove no dest D.

---

## 9. Risk to capital

| Book | Risk now (file-proven) |
|---|---|
| Live Pepperstone account `1369850` | **NONE.** `CTraderFixCopyOpen` refuses that account. `DemoDest` is false if that account id is configured. `CTraderFixSession` cannot D. |
| Demo dest `5328266` on `demo-us-eqx-01.p.c-trader.com` | **EXISTS.** Auto-copy can open/close dest gold. Ledger shows dest `237339770` **not** closed. Demo money / demo margin, not withdrawable live cash. Still dest inventory. |
| MT5 source books (Achiever / Starwave) | Copy path does not place source tickets. Ingest only. |
| Shadow book | Model only. Not dest cash. |

Do **not** write `risk_to_capital=NONE` without scoping it to `1369850`. Do **not** write “no 35=D in the product.”

---

## 10. What this slot will not claim

- “EX5 decompiled” / any ≥95% figure — out of scope.
- “API is up” / live `realCopyEnabled` value — GET failed.
- “Demo dest is an edge” — a fill is not expectancy. No dest mark-to-market in `DestinationRealPnl`.
- “Safe by absence of a D builder” — **false on HEAD** (`CTraderFixCopyOpen`).
- Passwords, tag 554 values, connection strings.

---

## 11. Claim scoreboard (binding)

| # | Claim | Verdict |
|---|---|---|
| 1 | no 35=D builder | **PASS** (`CTraderFixSession.cs` only) / **FAIL** (product) |
| 2 | `CanPromoteToLive` is false | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** |
| 4 | sending now cannot be the profit path | **FAIL** |
| 5 | SHADOW on demo is not destination profit | **PASS_NARROW** |

**Slot 15 overall: FAIL.**
