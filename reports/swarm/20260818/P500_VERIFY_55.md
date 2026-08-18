# P500_VERIFY_55 — Adversarial verifier (slot 55)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_55.md` |
| Agent / slot | P500 adversarial verifier **55** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned reads (this pass, full file) | `CTraderFixSession.cs` **135/135**; `BaselineScorer.cs` **212/212**; `RiskEngine.cs` **189/189**; `LiveCopyPage.tsx` **70/70** |
| Adjacent (this pass) | `CTraderFixLogonHostedService.cs`; `DependencyInjection.cs` L39–42; `CopyTradingService.cs`; `CopyTradingHostedService.cs`; `CTraderFixCopyOpen.cs`; `CTraderFixDemoTestTrade.cs`; `CTraderFixDemoMatrix.cs`; `XauUsdOneToOneCopyPolicy.cs`; `CopyRosterEngine.cs`; `CopyGroupFilter.cs`; `ShadowCopyEngine.cs`; `EfDashboardQueries.cs` L21–52; `DealIngestionService.cs` L127–144; `EfTradingStore.PersistDemoShadowAsync`; `LiveRuntimeStatus.cs`; `apps/api/Program.cs` `/api/health` + `/api/settings` + `/api/copy/status`; `.env` boolean `REAL_COPY_EXECUTION_ENABLED=true` (L73) + public demo host/account/sender prefixes only; `D:\Prop\data\demo_copy_ledger.json`; `DemoCopyLedger.cs`; `DashboardModels.OverviewDto`; `tests/Unit/BaselineScorerTests.cs` L21–27 |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Secrets printed | **None.** No tag 554, no manager/FIX/DB passwords. Quoted only already-public demo host prefix, account ids used as gates (`5328266` ≠ `1369850`), and the boolean `REAL_COPY_EXECUTION_ENABLED=true`. |
| Live GET this pass | **Blocked.** `web_fetch` `http://127.0.0.1:5000/api/health` → SSRF reject on loopback. Runtime RAM (`realCopyEnabled`, quote/trade logon, dest PnL, intent rows) is **not** proven this slot. |
| Live `35=D` sent this slot | **No** (report-only). |
| `REAL_COPY` flipped this slot | **No.** |
| Method | Independent `read_file` of the four assigned files. Targeted `grep` on product `*.cs`/`*.tsx`. Prior swarm text treated as **untrusted**. **FAIL** any claim not proven from a file or a live GET. |

**Honesty rule:** a logon (`35=A`) is not a fill. `CanPromoteToLive => false` is not a send interlock. Env `REAL_COPY_EXECUTION_ENABLED=true` is not a send license. A paper `ShadowOrder` is not dest PnL. A constructor `DestinationRealPnl=0` is not dest PnL. A sibling `Build("D")` on the 20 s tick **is** a dest path. Wanting profit is not an edge.

Assigned claims:

1. no `35=D` builder
2. `CanPromoteToLive` is false
3. `RealCopyEnabled` forced false after logon
4. sending now cannot be the profit path
5. SHADOW on demo is not destination profit

---

## 0. Verdict (binding)

**FAIL.** Two claims hold on the assigned files. Three do not. Slot verdict is FAIL.

| # | Assigned claim | Result | Why |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS_FILE / FAIL_UNSCOPED** | `CTraderFixSession.cs` outbound tag 35 is `"A"` only (L96). Process-wide there **is** a `35=D` builder (`CTraderFixCopyOpen.Build("D")` L95; also `CTraderFixDemoTestTrade` / `CTraderFixDemoMatrix`). Unscoped “no builder” is **false**. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive(TraderState current) => false` at `BaselineScorer.cs` L211. `FromBaseline` reachable set never includes `LIVE`. Persist copies `SuggestedState`. **Does not** block demo dest send. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | Hosted logon **never assigns** `_runtime.RealCopyEnabled`. DI **binds** `.env`. `.env` L73 is `true`. Live GET of `/api/health` **not obtained**. Claim is disproven from files; RAM value unproven. |
| 4 | sending now cannot be the profit path | **FAIL** | Hosted 20 s tick calls `ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.SendAsync` → `Build("D")`. Bypasses `RiskEngine.Evaluate`. Lab `.env` satisfies `DemoDest`. Ledger already holds dest fill `305750` / dest pos `237339770`. Live `1369850` refused. Dashboard dest PnL is constructor `0`, not a measurement. |
| 5 | SHADOW on demo is not destination profit | **FAIL** | Paper `ShadowCopyEngine.SimulateEntry` is not dest (that hop **PASS**). Unscoped claim **FAIL**: `SHADOW` is AUTO_ADMIT-eligible; `ExecuteDemoCopyAsync` sends on `ADMITTED` and **does not** require `LIVE`. `LiveCopyPage.tsx` L57 tells the operator dest auto-sends after ADMIT. |

One-line:

```text
FAIL. CTraderFixSession is 35=A only (PASS_FILE). CanPromoteToLive=>false (PASS). RealCopyEnabled is NOT forced false after logon (FAIL). Hosted demo hopper CAN 35=D (FAIL as “sending cannot be profit”). SHADOW can ADMIT and dest-send (FAIL). Live 1369850 refused. Live GET blocked. This slot sent 0.
```

---

## 1. Claim 1 — no `35=D` builder

### 1.1 Assigned file — PASS

Path: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`  
Read: **135 / 135**.

Token census **this file only** (this-pass `grep`):

| Pattern | Hits |
|---|---:|
| Literal `35=D` | **0** |
| `NewOrderSingle` | **0** |
| `(35, "D")` / `Build("D")` | **0** |
| `(35, "A")` | **1** (`BuildLogon` L96) |
| `ssl.WriteAsync` | **1** (L49) — bytes of that Logon |
| `Extract(..., "35")` | **1** — **inbound** reply (L55) |
| Outbound tag-35 values | **`"A"` only** |

The only outbound builder is `BuildLogon`:

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

`Assemble` (L112–120) is generic. Its only caller in this file is `BuildLogon`. One `WriteAsync`, then the socket is disposed (`using TcpClient` / `await using SslStream`). This is a one-shot Logon probe, not a standing TRADE session.

`CTraderFixLogonHostedService` is the only product caller of `TryLogonAsync` (QUOTE **5211**, TRADE **5212**). After reply it persists session rows. It does not keep the socket and does not send an order.

**File-scoped claim 1: PASS.**

### 1.2 Process-wide — FAIL if the claim is unscoped

Sibling builders (not the assigned file; still product `src`):

| File | Encoder | Hosted? |
|---|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` L95 / L142–156 | `Build("D", ...)` then `Write` | **Yes** — `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566 via `CopyTradingHostedService` L30 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | `Build("D", ...)` | CLI `tools/DemoFixTestTrade` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` L93 | `Build("D", ...)` | test/matrix tool |

`CTraderFixCopyOpen.Build` is a real NewOrderSingle assembler (tag 35 = caller `type`; open/close uses `"D"`; tag 38 qty; tag 11 ClOrdID; optional 721 dest pos):

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
        // ... FIX.4.4 + checksum ...
    }
```

Call site that emits the order:

```83:95:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
            var closing = !string.IsNullOrWhiteSpace(destPositionId);
            // ...
            await Write(ssl, Build("D", sender, target, seq, extra.ToArray()), timeout.Token);
```

Refuse gate on that sender (live identity, not a missing builder):

```37:42:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
        if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
            || !sender.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
            || account == "1369850")
        {
            return Fail("Refused: dest must be demo FIX, not live 1369850.", host, account, sourceLogin);
        }
```

**Unscoped “no 35=D builder”: FAIL.** Older “product `35=D` string = 0” pins are **stale** as a no-sender claim: the encoder uses `Build("D")`, not the three-character literal `35=D`.

---

## 2. Claim 2 — `CanPromoteToLive` is false — **PASS**

Path: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` **212/212**.

```187:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
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

Proven from this file:

- `CanPromoteToLive` is a hard `false`. Argument `current` is unused.
- `FromBaseline` reachable set is `{INSUFFICIENT_DATA, RISK_BLOCKED, SHADOW, WATCH, EARLY_SCORE}`. **No `LIVE`. No `LIVE_CANDIDATE`.**
- Ceiling of a clean book is `SHADOW` (quality ≥ 70 and risk < 40).
- `AfterHighEarlyScore()` also returns `SHADOW`.

Product persist (adjacent, this pass): `DealIngestionService` L140 `CurrentState = score.SuggestedState`. No product assignment of `TraderState.LIVE` found (`grep` `CurrentState =` / `TraderState.LIVE` on `src/**/*.cs` = counts, dashboard, and a dead compare at `CopyTradingService` L330).

Product callers of `CanPromoteToLive`: **0** (`grep` `*.cs` = definition + unit test + `_tmp` scratch). Vacuous lock, not an A22 R5-before-R6 gate.

Unit lock: `tests/Unit/BaselineScorerTests.cs` L21–27 `CanPromoteToLive(score.SuggestedState).Should().BeFalse()` on the 3-winner SHADOW case.

**Residual (does not fail claim 2):** `ExecuteDemoCopyAsync` does not consult `CanPromoteToLive` or `TraderState.LIVE`. Claim 2 is still true as stated.

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL**

### 3.1 Hosted logon does not write the flag

Full read of `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` (112 lines). After both `TryLogonAsync` calls it writes Quote/Trade `LoggedOn`/`Status`/`LastError`/`UpdatedAt` only, then **logs** the current flag:

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

There is **no** `_runtime.RealCopyEnabled = false` (or `true`) in this file. `grep` `RealCopyEnabled\s*=` on product `*.cs` = **one** assignment:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Lab `.env` L73 (boolean only): `REAL_COPY_EXECUTION_ENABLED=true`.  
API `Program.cs` L10 `EnvFile.FindAndLoad()` + L13 `AddEnvironmentVariables()`.

If the API process loaded that file, DI sets `RealCopyEnabled=true` and logon **leaves it true**. Older “hosted re-pin false” quotes are **STALE**.

`LiveRuntimeStatus.RealCopyEnabled` is a public `{ get; set; }` (L32). Nothing after logon forces it false.

Assigned `LiveCopyPage.tsx` L13 renders `status?.realCopyArmed ? 'YES' : 'NO'`. That bit is `_runtime.RealCopyEnabled` (`CopyTradingService` L64). The page does not force it false.

### 3.2 Live GET not obtained

`GET http://127.0.0.1:5000/api/health` would have shown `realCopyEnabled` (`Program.cs` L55). `GET /api/settings` would have shown `featureFlags.REAL_COPY_EXECUTION_ENABLED` (L76). Fetch this slot: **SSRF-blocked** on loopback. RAM value this slot: **unproven**.

Per standing rule: a claim that needs live GET and did not get one **cannot PASS**. The file evidence already **disproves** “forced false after logon.”

**Claim 3: FAIL.**

---

## 4. Claim 4 — sending now cannot be the profit path — **FAIL**

### 4.1 Assigned `RiskEngine` can approve a send

`D:\Prop\src\Domain\Risk\RiskEngine.cs` **189/189**.

The `RealExecutionEnabled == false` branch is a **no-op comment** (L90–93). It does not `Reject`. Final allow:

```147:171:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;

        if (IsReducing(request.Action))
        {
            return new RiskDecision
            {
                CopyIntentId = request.CopyIntentId,
                Outcome = RiskDecisionOutcome.Approve,
                ApprovedQuantity = request.RequestedQuantity,
                Reason = "RISK_REDUCTION",
                AllowFixSend = allowSend
            };
        }

        return new RiskDecision
        {
            CopyIntentId = request.CopyIntentId,
            Outcome = RiskDecisionOutcome.Approve,
            ApprovedQuantity = request.RequestedQuantity,
            Reason = "APPROVED",
            AllowFixSend = allowSend
        };
```

If a caller passed `RealExecutionEnabled=true`, `KillSwitch=None`, `Reconciled=true`, `VenueHealthy=true`, `AllowFixSend` is **true**. That is not “cannot send.”

Persist hop today:

- `CopyTradingService` L20 `VenueReconciled = false` (const) is what Evaluate sees (L304).
- Persist then **overwrites** `AllowFixSend = false` (L324).
- Even the dead branch L330–333 only sets status `LIVE_SEND_BLOCKED_UNIMPLEMENTED`. It does not call `CTraderFixCopyOpen`.

So the **Evaluate persist hop** cannot emit dest tickets. That is **not** the only hop.

### 4.2 Assigned session cannot send D — still not the only hop

`CTraderFixSession` writes only `35=A` then disposes. That hop cannot be dest profit. Assigned claim 4 is **not** scoped to that file.

### 4.3 Hosted demo hopper **is** a send path

```28:30:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

20 s tick. `ExecuteDemoCopyAsync` (`CopyTradingService` L483–605):

- Returns 0 only if `!DemoDest` or empty password.
- `DemoDest` (L45–48) is `host` starts with `demo-` **and** trade sender starts with `demo.` **and** account ≠ `1369850`.
- Lab `.env` (public keys only): `CTRADER_FIX_HOST=demo-us-eqx-01.p.c-trader.com`; `CTRADER_FIX_TRADE_SENDER_COMP_ID=demo.pepperstone.5328266`; `CTRADER_FIX_ACCOUNT_ID=5328266`. **File-proven DemoDest = true** if those keys load.
- `NewOrderSingleImplemented => DemoDest` (L50). `const false` pins are **STALE**.
- Does **not** read `RealCopyEnabled`.
- Does **not** call `RiskEngine.Evaluate`.
- Does **not** require `TraderState.LIVE`.
- Opens/closes via `CTraderFixCopyOpen.SendAsync` (L528 close, L566 open) → `Build("D")`.
- Lot skip `MaxAutoLots = 0.05m` (L22 / L558) is a source filter, not a dest clip. Policy `AllocationFactor = 1m`.

Assigned `LiveCopyPage.tsx` L57 states the same product law in the UI:

> “No copy intents yet. Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position. Dest closes when that MT5 position closes.”

That page is **not** an 8-line stub. Current file is 70 lines, `useCopyStatus` + `useCopyIntents`, live-send blocker list, intent table.

### 4.4 Dest book already on disk

`D:\Prop\data\demo_copy_ledger.json` (and the same row is **re-seeded** inside `ExecuteDemoCopyAsync` L500–512 if missing):

| Field | Value (public dest book, not a secret) |
|---|---|
| SourceLogin | `305750` |
| SourcePositionId | `21250421` |
| Lots | `0.01` |
| DestPositionId | `237339770` |
| DestClOrdId | `C20260818093047317` |
| DestFillPrice | `4390.2` |
| DestClosed | `false` |

A dest fill at 4390.2 on 0.01 lot **is** a dest P&L path (profit **or** loss). Dashboard `GetOverviewAsync` still constructs `DestinationRealPnl = 0` (`EfDashboardQueries.cs` L44 positional `0` after `shadowPnl`; DTO field is `OverviewDto.DestinationRealPnl` at `DashboardModels.cs` L16). Constructor zero is **not** proof dest PnL is zero.

`LiveCopyPage` “Live sends” chip counts `ExecutionIntents` with `SentAt != null` (`CopyTradingService` L57). Product `src` has **0** `ExecutionIntents.Add` / `new ExecutionIntent` writers. The chip can stay 0 while the ledger dest exists. Chip 0 is **not** “no dest send.”

**Claim 4: FAIL.** Sending now **can** be a dest P&L path on demo `5328266`. It is **not** a path to live Pepperstone `1369850` (refuse + assigned session has no D). That narrower live-money statement was **not** the assigned claim.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **FAIL**

Two hops must not be collapsed.

### 5.1 Paper SHADOW hop — not dest (narrow PASS)

`GenerateShadowIntentsAsync` copyable set L202: `{SHADOW, LIVE_CANDIDATE, LIVE}`. Persist `AllowFixSend=false`. On non-send it writes `SHADOW_ONLY` and may call `ShadowCopyEngine.SimulateEntry` (in-process bid/ask + 0.05 pt model; **no socket**). `PersistDemoShadowAsync` also calls `SimulateEntry` only when `state == SHADOW` and writes `Status = "SHADOW_ONLY"`.

`shadowPnl` on overview is `Sum(SourceVsShadowSlippage)` (`EfDashboardQueries.cs` L29), not dest realized.

That hop is **not** destination profit.

### 5.2 SHADOW state on a demo/contest source **can** dest-send (FAIL the unscoped claim)

`XauUsdOneToOneCopyPolicy.IsTraderEligible` blocks `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` / blocked/paused/disqualified. It **allows** `SHADOW` (and `LIVE_CANDIDATE` / `LIVE`):

```81:85:D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs
        if (trader.State is TraderState.INSUFFICIENT_DATA or TraderState.EARLY_SCORE or TraderState.WATCH)
        {
            reason = "TRADER_NOT_SHADOW_YET";
            return false;
        }
```

Also requires ≥ 20 completed XAU, `XauNetPnl > 0`, no size pattern, and `CopyGroupFilter.IsDemoOrContest` (path segment `demo` or `contest`).

`CopyRosterEngine.Decide` AUTO_ADMITs when eligible (L72–80). `TickRosterAsync` writes `Status = "ADMITTED"`.

`ExecuteDemoCopyAsync` L542–569 walks **`ADMITTED` roster seats**, not `CurrentState == LIVE`. An ADMITTED SHADOW with an open XAU ≤ 0.05 lots is sent.

`FromBaseline` **cannot** emit LIVE. The only auto quality ceiling is SHADOW. Therefore the **only** scorer state that can sit on the dest hopper is SHADOW (plus any hand-written LIVE/LIVE_CANDIDATE rows, of which persist-from-scorer produces none).

So: “SHADOW on demo is not destination profit” is **false** as a process claim. SHADOW-on-demo is the **intended** dest-admission class.

Assigned `LiveCopyPage.tsx` L14–16 shows `SHADOW traders` and `LIVE traders` as separate chips next to `Live sends` / `Shadow fills`. The empty-state sentence (L57) ties dest send to **ADMITTED**, not to LIVE.

305750 dest fill on disk is dest PnL. This slot did **not** live-GET that login’s `CurrentState`. The **code path** does not need that row to fail the claim.

**Claim 5: FAIL** (unscoped). Narrow paper-shadow hop is not dest; that is a different sentence.

---

## 6. `LiveCopyPage.tsx` honesty (assigned)

Path: `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` **70/70**.

| UI | Source | Honesty |
|---|---|---|
| H1 “Live copy portfolio” | chrome | label only |
| `status?.summary` | `GET /api/copy/status` | DemoDest true → “Demo dest auto-copy ON…” (`CopyTradingService` L76–77) |
| `REAL_COPY armed` YES/NO | `realCopyArmed` ← `_runtime.RealCopyEnabled` | **not** forced false |
| SHADOW / LIVE counts | score table | LIVE should be 0 if only scorer persist |
| Live sends | `ExecutionIntents` with `SentAt != null` | **0 writers** of `ExecutionIntent` in `src` — chip can stay 0 while ledger dest exists |
| Shadow fills | `ShadowOrders` count | paper |
| Blockers | `BuildBlockers` | skipped entirely when `DemoDest` except FIX/REAL_COPY; **does not** list “no sender” |
| Empty copy L57 | JSX literal | **admits dest auto-send** |

Older stub quotes (“`REAL_COPY_EXECUTION_ENABLED` is false. This page will stay empty…”) **do not exist** in the current file.

Live `/api/copy/status` **not fetched** this slot. UI values unproven.

---

## 7. Stale pins this slot kills

| Pin | Status |
|---|---|
| Hosted logon `_runtime.RealCopyEnabled = false` | **STALE** — log line only |
| DI `RealCopyEnabled = false` with “do not arm” comment | **STALE** — env bind |
| `NewOrderSingleImplemented` const `false` | **STALE** — `=> DemoDest` L50 |
| Persist `AllowFixSend=false` at L306 | **STALE** — now L324 |
| Product has no `35=D` sender / CopyOpen CLI-only | **STALE** — hosted L30 |
| `LiveCopyPage` 8-line stub / literal false | **STALE** — 70-line hooked page |
| Copy hop `SAFE_BY_ABSENCE` | **STALE** for demo dest |
| `EfDashboardQueries` `RealCopyEnabled=false` literal | **STALE** — now `_runtime.RealCopyEnabled` (L52); dest PnL still literal `0` |
| README “Real NewOrderSingle is off (`REAL_COPY_EXECUTION_ENABLED=false`)” | **STALE vs lab `.env` L73 `true`** |

---

## 8. Risk to capital

| Book | Risk | Proof |
|---|---|---|
| Live Pepperstone `1369850` | **NONE** this slot | `CTraderFixSession` has no D; `CTraderFixCopyOpen` refuses that account; this slot sent 0 |
| Demo dest `5328266` | **NOT** `SAFE_BY_ABSENCE` | Hosted hopper + `DemoDest` + ledger dest fill open at 0.01 / 4390.2 |
| This process (verifier) | **NONE** | Report only. No TLS order. No `.env` edit. |

`AllocationFactor=1m` is dest-ruin **if** the hopper copies a large source ticket (hopper currently skips > 0.05 source lots). Copying **all** catalog logins would still copy `RISK_BLOCKED` losses if a caller bypassed policy/roster. Wanting dest profit is not an edge.

---

## 9. What this slot did **not** prove

- Live process `realCopyEnabled` (GET blocked).
- Live FIX QUOTE/TRADE `LoggedOn` bits.
- `305750` `CurrentState` / group on the running API.
- Whether the 20 s host is actually running right now.
- SHA-256 of the four files (no hash tool this worker).

Those gaps **cannot** rescue claims 3–5. Claims 3–5 are already disproven from files.

---

## 10. Files read (absolute)

- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs`
- `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs`
- `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs`
- `D:\Prop\src\Domain\Copy\CopyGroupFilter.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Application\Dashboard\DashboardModels.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\src\Infrastructure\Copy\DemoCopyLedger.cs`
- `D:\Prop\data\demo_copy_ledger.json`
- `D:\Prop\tests\Unit\BaselineScorerTests.cs`
- `D:\Prop\.env` (boolean + public host/account/sender prefixes only; no secrets quoted)
