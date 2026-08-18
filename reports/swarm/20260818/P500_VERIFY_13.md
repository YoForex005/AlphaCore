# P500_VERIFY_13 — Adversarial verifier, slot 13

| Field | Value |
|---|---|
| Slot | **13** |
| Date | 2026-08-18 |
| Role | Adversarial verifier (independent re-read; no rubber-stamp) |
| Workspace | `D:\Prop` |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_13.md` |
| Assigned reads | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` |
| Product source modified | **No** |
| Test source modified | **No** |
| Secrets printed | **None** (no tag 554 / FIX / manager / DB passwords; `.env` quoted only as boolean `REAL_COPY_EXECUTION_ENABLED=true`) |
| Live GET this slot | `GET http://127.0.0.1:5000/api/health`, `/api/overview`, `/api/copy/status` — **blocked** (loopback SSRF). Runtime `realCopyEnabled` / dest PnL **not remasured**. File proof only. |
| SHA-256 of assigned files | **Unmeasured** (no shell this worker). |

**Honesty:** wanting dest profit is not an edge. A TLS Logon is not a fill. A SHADOW score is not dest money. A hardcoded `DestinationRealPnl=0` is not a measured dest book. **FAIL any claim not proven from a file read this slot or a live GET.**

---

## 0. Overall verdict

**FAIL**

The five-claim package cannot be stamped PASS. Claim **(3)** is **false in source**. Claim **(1)** is true **only** for `CTraderFixSession.cs`, not product-wide. Claim **(4)** is true for the LIVE / session hop and **false** as an absolute “no dest send exists.” Claims **(2)** and **(5)** hold from the assigned files.

| # | Claim | Verdict |
|---|---|---|
| 1 | no `35=D` builder | **PASS_SCOPED** (`CTraderFixSession.cs` only). **FAIL** if read as product-wide. |
| 2 | `CanPromoteToLive` is false | **PASS** |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** |
| 4 | sending now cannot be the profit path | **FAIL** as absolute. **PASS_NARROW** for session / LIVE persist hop / unmeasured dest book. |
| 5 | SHADOW on demo is not destination profit | **PASS** (shadow book ≠ dest money). Residual: SHADOW + `ADMITTED` can still trigger demo `35=D`. |

One-line:

```text
Slot 13 FAIL. Session has no 35=D builder (only 35=A). CanPromoteToLive=>false. RealCopyEnabled is NOT forced false after logon (DI from env; .env=true). Demo hopper Build("D") is dest exposure, not a measured profit engine. ShadowOrder/ShadowPnl are not dest PnL. Live 1369850 refused. Live GET blocked.
```

---

## 1. Assigned files (read this slot, full)

| File | Path | Lines read |
|---|---|---|
| FIX session | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | **135 / 135** |
| Scorer | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | **212 / 212** |
| Risk | `D:\Prop\src\Domain\Risk\RiskEngine.cs` | **189 / 189** |
| Live page | `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | **70 / 70** |

Adjacent files read only to prove or refute the five claims (not to expand the SUT):

- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` (post-logon assignments)
- `D:\Prop\src\Infrastructure\DependencyInjection.cs` (only `RealCopyEnabled =` writer)
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` (`Build("D")`)
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` (`DestinationRealPnl` ctor `0`)
- `D:\Prop\src\Application\Dashboard\DashboardModels.cs`
- `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs`
- `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs`
- `D:\Prop\src\Infrastructure\Copy\DemoCopyLedger.cs`
- `D:\Prop\data\demo_copy_ledger.json` (fill record; no secrets)
- `D:\Prop\apps\api\Program.cs` (exposes `runtime.RealCopyEnabled`; no post-logon pin)
- `D:\Prop\apps\web\src\api\hooks.ts` (`useCopyStatus` / `useCopyIntents` = GET only)
- Unit tests: `BaselineScorerTests.cs`, `RiskEngineTests.cs`

`reports\CREDENTIALS_AND_COPY_STATUS.md` (`REAL_COPY` “false (forced)”; “Live 35=D method does not exist”) is **STALE** vs HEAD files read this slot.

---

## 2. Claim (1) — no `35=D` builder

### 2.1 `CTraderFixSession.cs` — **PASS**

Token census this file only:

| Pattern | Hits |
|---|---:|
| literal `35=D` | **0** |
| `(35, "D")` / `Build("D")` | **0** |
| `NewOrderSingle` | **0** |
| `OrderQty` / `ClOrdID` / tag 11 / 38 / 54 as outbound | **0** |
| outbound tag 35 built | **1** — `(35, "A")` at `BuildLogon` L96 |
| `ssl.WriteAsync` | **1** (L49) — bytes of that Logon |
| `Extract(..., "35")` | **1** (L55) — **inbound** reply only |
| standing socket after return | **No** — `using TcpClient` + `await using SslStream` dispose on every path |

The only outbound encoder is `BuildLogon` → `Assemble`. Fields start with `(35, "A")` (FIX Logon). `TryLogonAsync` writes that once, reads one reply, returns. There is no order builder, no heartbeat, no quote subscribe, no sequence store, no second write.

Inbound `35=A` sets `LoggedOn=true`. Any other inbound type is an error string (`Logon rejected 35={msgType}`). That is not a NewOrderSingle.

### 2.2 Product-wide — **FAIL** if the claim is unscoped

Sibling builders exist (not in the assigned session file):

| File | Evidence |
|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L142–156 | generic `Build(string type, ...)` emits `(35, type)` |
| same, L95 | `Build("D", sender, target, seq, extra)` — NewOrderSingle on the wire |
| `CTraderFixDemoTestTrade.cs` | `Build("D", ...)` at L139 / L163 / L197 |
| `CTraderFixDemoMatrix.cs` | `Build("D", ...)` at L93 |

`CTraderFixCopyOpen.SendAsync` refuses live identity `1369850` (L37–41) and requires `demo-` host + `demo.` sender. That is a **gated** `35=D` builder, not “no builder.”

`LiveCopyPage.tsx` has no FIX encoder (display-only). `RiskEngine.cs` / `BaselineScorer.cs` have no FIX encoder.

**Claim (1) as written, scoped to the first assigned file: PASS. As a product statement: FAIL.**

---

## 3. Claim (2) — `CanPromoteToLive` is false — **PASS**

Proven in the assigned scorer file:

```211:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static bool CanPromoteToLive(TraderState current) => false;
```

`TraderStateMachine.FromBaseline` reachable set (L189–207):

| Condition | State |
|---|---|
| `CompletedXauTrades == 0` | `INSUFFICIENT_DATA` |
| `risk >= 80` or (martingale ∧ DD>0 ∧ NetPnl<0) | `RISK_BLOCKED` |
| not early-eligible (`< 3` XAU) | `INSUFFICIENT_DATA` |
| `quality >= 70 && risk < 40` | `SHADOW` |
| `quality >= 55` | `WATCH` |
| else | `EARLY_SCORE` |

**`LIVE` and `LIVE_CANDIDATE` are not in the reachable set.** `AfterHighEarlyScore()` returns `SHADOW` (L209). `BaselineScorer.Score` assigns `SuggestedState` only from `FromBaseline` (L162).

Unit file `tests\Unit\BaselineScorerTests.cs` L21–26: three disciplined winners → `SHADOW`; `CanPromoteToLive(...).Should().BeFalse()`.

`TraderState.LIVE = 5` still exists on the enum (`src\Domain\Enums\TraderState.cs`). Existence of the enum value is not promotion. No assigned-file writer sets `CurrentState = LIVE`.

---

## 4. Claim (3) — `RealCopyEnabled` forced false after logon — **FAIL**

This is the package-breaking claim.

### 4.1 Logon hosted service does **not** assign the flag

`CTraderFixLogonHostedService.ExecuteAsync` after both `TryLogonAsync` calls (L60–70) writes:

- `_runtime.Quote.LoggedOn / Status / LastError / UpdatedAt`
- `_runtime.Trade.LoggedOn / Status / LastError / UpdatedAt`

Then **logs** `_runtime.RealCopyEnabled` as `RealCopyArmed={Armed}`. There is **no** `_runtime.RealCopyEnabled = false` (or `true`) anywhere in that file. Persist path (L91–111) updates `FixSessionState` rows only.

### 4.2 Only writer in product C#

Workspace `grep` `RealCopyEnabled =` on `*.cs`:

| Location | What it does |
|---|---|
| `DependencyInjection.cs` L39–42 | **startup** `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` |
| `LiveRuntimeStatus.cs` L32 | public **setter** (anyone can flip; **nobody after logon does**) |

`apps\api\Program.cs` L55 / L76 **reads** `runtime.RealCopyEnabled` into `/api/health` and `/api/settings`. It does not pin it false.

### 4.3 Lab env is the opposite of “forced false”

`D:\Prop\.env` L73 (boolean only): `REAL_COPY_EXECUTION_ENABLED=true`.

So at API process start, DI sets `RealCopyEnabled=true`. After a successful QUOTE/TRADE logon, the flag **stays whatever DI set**. It is **not** forced false.

### 4.4 What *is* false, and must not be confused with this claim

| Fact | File | Not the same as claim 3 |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` **default** `false` | `CTraderFixOptions.cs` L35 | default, not post-logon |
| FIX worker reads `CTrader:RealCopyExecutionEnabled` default false | `apps\fix-worker\Worker.cs` L21 | **different config key** than API `REAL_COPY_EXECUTION_ENABLED` |
| Copy persist hardcodes `AllowFixSend = false` | `CopyTradingService.cs` L324 | send gate, not the runtime flag |
| `VenueReconciled` const `false` | `CopyTradingService.cs` L20 | recon, not the flag |
| Session sockets disposed after logon | `CTraderFixSession.cs` L35–39 | SAFE_BY_ABSENCE on **that** hop |

`RiskEngine` L90–93 is an **empty** `if (RealExecutionEnabled == false && Action != CloseExposure)` — comment says shadow never allows FIX send, but the **actual** send bit is L147–150 (`allowSend = RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`). If `RealExecutionEnabled` is true (API runtime when env is true) **and** the other three are true, `AllowFixSend` **can be true**. That is the opposite of “forced false after logon.”

`RiskEngineTests.Real_flag_false_never_allows_fix_send` proves the **false** case only (`RealExecutionEnabled = false` in the test `Base()`).

**Live GET of `realCopyEnabled` was blocked this slot. File proof is already enough to FAIL claim 3.** A live `false` would still not prove “forced after logon”; it would only show the process started with env ≠ `true`.

---

## 5. Claim (4) — sending now cannot be the profit path

### 5.1 What the four assigned files prove

**`CTraderFixSession`:** cannot send a ticket. One Logon write, sockets disposed. Not a profit path.

**`RiskEngine`:** `AllowFixSend` is true only when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Rejects always set `AllowFixSend=false` (L187). This is a **gate**, not a sender. Caps (`MaxLossPerTrader=500`, `MaxDailyExecutionLoss=2000`, …) are loss caps, not an edge.

**`BaselineScorer`:** never promotes to LIVE (claim 2). Quality / SHADOW is source-book scoring, not dest PnL.

**`LiveCopyPage.tsx`:** GET-only (`useCopyStatus`, `useCopyIntents`). No POST, no send button. Renders `status.summary`, `realCopyArmed`, `liveSends`, `shadowFills`, blockers titled “Live send blockers (Pepperstone cannot be filled).” Empty-state text L57: *“Demo dest auto-sends after a trader is ADMITTED…”* — the **page** does not send; it **discloses** a hopper.

### 5.2 Why the absolute claim **FAIL**s

`CopyTradingHostedService` 20s tick (L28–30) calls `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**.

`ExecuteDemoCopyAsync` (`CopyTradingService.cs` L483–605):

- returns 0 if `!DemoDest` (demo host + `demo.` sender + account ≠ `1369850`)
- **bypasses** `RiskEngine.Evaluate`
- calls `CTraderFixCopyOpen.SendAsync` (close L528, open L566)
- that helper **builds and writes `35=D`** (CopyOpen L95)

On-disk dest fill (read this slot, no secrets):

`D:\Prop\data\demo_copy_ledger.json` — login `305750` / pos `21250421` / dest `237339770` / px `4390.2` / lots `0.01` / `DestClosed=false`.

`reports\swarm\20260818\DEMO_COPY_OPEN.json` records `OrderSent=true`, `Filled=true`, `35=8` / `150=F` / `39=2` on demo account `5328266`.

Dashboard dest book is **not** that fill:

```43:46:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            shadowPnl,
            0,
            0,
            0,
```

`OverviewDto.DestinationRealPnl` (DashboardModels L16) is constructor literal **0**. That is **unmeasured dest**, not proof dest P&L is zero, and not proof a send cannot produce dest P&L.

LIVE persist hop still cannot send:

- `VenueReconciled` const **false** (CopyTradingService L20) is what Evaluate gets (L304) → new opens `VENUE_NOT_RECONCILED`
- persist **overwrites** `AllowFixSend = false` (L324)
- even the “would send” branch only sets status `LIVE_SEND_BLOCKED_UNIMPLEMENTED` (L330–333) — **no wire write**
- `GetStatusAsync` L67 reports `VenueReconciled: DemoDest` — **honesty split** vs the const

So:

| Hop | Can `35=D` now? | Is it a *measured profit* path? |
|---|---|---|
| `CTraderFixSession` | No | No |
| Risk persist / LIVE branch | No | No |
| `LiveCopyPage` | No | No |
| Hosted `ExecuteDemoCopyAsync` → `CTraderFixCopyOpen` | **Yes, demo dest only** | **Not measured** (`DestinationRealPnl` hardcoded 0). Dest exposure **exists**. |
| Live Pepperstone `1369850` | Refused (CopyOpen L37–41) | No |

**Absolute “sending now cannot be the profit path”: FAIL** — demo send is dest exposure and has already filled.  
**Narrow “session / LIVE pipeline / dashboard dest book is not a dest-profit engine”: PASS.**  
Wanting a send is not an edge. Unmeasured dest ≠ dest profit.

---

## 6. Claim (5) — SHADOW on demo is not destination profit — **PASS**

### 6.1 Proven from assigned + shadow files

`TraderStateMachine.FromBaseline` best case is `SHADOW` (scorer L200–201). That is a **source trader classification**.

`GenerateShadowIntentsAsync` (`CopyTradingService.cs` L201–360):

- copyable states include `SHADOW` (L202)
- after Evaluate, persist `AllowFixSend=false`
- non-LIVE path sets `intent.Status = "SHADOW_ONLY"` and calls `ShadowCopyEngine.SimulateEntry` (L337–346)
- writes `ShadowOrder` with simulated price / slippage — **no FIX write**

`ShadowCopyEngine.SimulateEntry` / `SimulateExit` / `MarkToMarket` (`src\Domain\Shadow\ShadowCopyEngine.cs`) take a `DestinationQuote` and return a **modeled** fill. No socket.

Dashboard `ShadowPnl` (`EfDashboardQueries` L29) = `Sum(ShadowOrders.SourceVsShadowSlippage)`. That is a **slippage sum**, not dest account money. It is a different field from `DestinationRealPnl` (hardcoded 0).

`LiveCopyPage` shows `SHADOW traders` and `Shadow fills` as **separate** stats from `Live sends` / `REAL_COPY armed`. It does not label shadow fills as dest profit.

`XauUsdOneToOneCopyPolicy.IsTraderEligible` rejects `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET` (L81–84). SHADOW is an **eligibility gate**, not dest PnL.

### 6.2 Residual (does not flip claim 5)

Roster `IsTraderEligible` can **AUTO_ADMIT** a SHADOW trader (`CopyRosterEngine` L72–80). `ExecuteDemoCopyAsync` then sends `35=D` for `ADMITTED` seats with open XAU ≤ `MaxAutoLots` (0.05). That dest fill is **demo dest exposure**, not the SHADOW book.

Claim 5 as stated — SHADOW (state / ShadowOrder / ShadowPnl) on demo is not destination profit — **holds**. Do not read it as “SHADOW traders cannot be demo-sent.”

---

## 7. Risk to capital (this slot)

| Book | Risk | Proof this slot |
|---|---|---|
| Live Pepperstone dest `1369850` | **NONE** | Session has no `35=D`. CopyOpen L37–41 refuses that account. Persist hop never writes FIX. Page has no send. |
| Demo dest `5328266` | **NOT `SAFE_BY_ABSENCE`** | Hosted hopper + `Build("D")`. Ledger row open (`DestClosed=false`, 0.01 lot). This slot **did not send**. Dest P&L **unmeasured**. |
| MT5 source books | **Not flattened by this hop** | Roster flatten is dest-intent only (not read as a send to MT5). |
| `REAL_COPY` flag | **Armed in env** | `.env` boolean `true`; **not** forced false after logon. Flag ≠ send license on live. |

Copy-all of the Manager census is **not remasured** this slot (live GET blocked). I will not stamp the −$154,425 / −$241,580 pins as this-slot evidence. Policy still **rejects** `RISK_BLOCKED` (`XauUsdOneToOneCopyPolicy` L75–78; roster L46–47). That is file fact, not a dest-profit claim.

---

## 8. Stale docs called out (so they are not reused as proof)

| Doc / pin | Why stale vs files read now |
|---|---|
| `reports\CREDENTIALS_AND_COPY_STATUS.md` | `REAL_COPY` “false (forced)”; “Live 35=D method does not exist” |
| `README.md` / `docs\architecture.md` “NewOrderSingle off / flag false” | Lab `.env` is `true`; demo builder exists |
| Any pin “product `35=D=0`” / `NOS const false` / CopyOpen CLI-only | `NOS => DemoDest` (CopyTradingService L50); hosted `ExecuteDemoCopyAsync`; sibling `Build("D")` |
| `C37` “LiveCopyPage 321 B / missing” | Current page is 70 lines, wired to `/api/copy/status` + `/api/copy/intents` |

---

## 9. What this slot did **not** do

- Did not modify product or tests.
- Did not send `35=D`.
- Did not flip `REAL_COPY`.
- Did not print secrets.
- Did not remasure live overview / copy status (SSRF).
- Did not hash files (no shell).

**Package verdict remains FAIL on claim (3).** Do not tell a profit story that depends on “flag forced false after logon.”
