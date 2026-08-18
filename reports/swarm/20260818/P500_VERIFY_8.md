# P500_VERIFY_8 — Adversarial: five safety claims vs HEAD files

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_8.md` |
| Agent | P500_VERIFY_8 (adversarial verifier, slot **8**) |
| Slot | **8** |
| Date | 2026-08-18 |
| Assigned | Read `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx`. Confirm: (1) no `35=D` builder, (2) `CanPromoteToLive` is false, (3) `RealCopyEnabled` forced false after logon, (4) sending now cannot be the profit path, (5) SHADOW on demo is not destination profit. **FAIL any claim that cannot be proven from a file or live GET.** |
| SUT | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135), `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212), `D:\Prop\src\Domain\Risk\RiskEngine.cs` (189), `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (70) |
| Hop (read, not assigned) | `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `CTraderFixLogonHostedService.cs`, `CopyTradingService.cs` (625), `CopyTradingHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `DemoCopyLedger.cs`, `data\demo_copy_ledger.json`, `EfDashboardQueries.cs`, `EfTradingStore.cs`, `apps\api\Program.cs` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent this slot | **No** |
| `REAL_COPY` flipped this slot | **No** |
| Secrets printed | **None** (no `.env` dump, no tag 554, no manager/FIX passwords) |
| Live GET | **Not proven.** `GET http://127.0.0.1:5000/api/health` and `/api/copy/status` → worker HTTP client `SSRF blocked`. Claims that need a 200 are **FAIL**. |
| SHA-256 | **Not recomputed** (no shell this slot). Identity is full line census of the four assigned files. |

**Honesty rule:** a comment, log line, DTO bit, or page string is **not** a socket write. `AllowFixSend` is **not** a sender. `35=A` Logon is **not** NewOrderSingle. `Build("D", …)` **is** a NewOrderSingle encoder. Paper `ShadowOrder` / `ShadowPnl` is **not** dest P&L. Demo dest fills **are** dest P&L (demo money). Wanting profit is **not** an edge. Copy-all 8463 would copy `RISK_BLOCKED` losses. Do not treat W500_RESEARCH_170 / E002 / D81 as HEAD.

---

## 0. Verdict (binding)

**FAIL. Cannot confirm all five claims.**

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | No `35=D` builder | **FAIL** (product / hop). **PASS** only inside `CTraderFixSession.cs` | Assigned file has no builder. Siblings `Build("D")` ×5. Hosted hop calls `CTraderFixCopyOpen.SendAsync`. |
| 2 | `CanPromoteToLive` is false | **PASS** | `BaselineScorer.cs` L211 `=> false`. `FromBaseline` never emits `LIVE`. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | Zero post-logon assignment. DI binds env. `.env` key is `true`. Hosted service only **logs** the bit. |
| 4 | Sending now cannot be the profit path | **FAIL** as a no-send / no-dest-P&L claim. Residual **PASS** only for live `1369850` + “wanting send ≠ edge” | Demo dest hop sends `35=D` on the 20 s tick. Ledger already has an open dest fill. Dashboard dest PnL is a constructor `0`, not a measurement. |
| 5 | SHADOW on demo is not destination profit | **PASS** for paper `ShadowCopyEngine`. **FAIL** if read as “SHADOW demo cannot produce dest dollars” | Paper `SHADOW_ONLY` / `ShadowPnl` ≠ dest. `SHADOW` is the admission class for `ExecuteDemoCopyAsync`. |

```text
OVERALL=FAIL
1=FAIL_GLOBAL (PASS_SCOPED CTraderFixSession 135/135 35=A only)
2=PASS CanPromoteToLive(*)=>false L211
3=FAIL no post-logon pin; DI binds REAL_COPY_EXECUTION_ENABLED
4=FAIL demo dest send hop exists; live 1369850 still refused
5=PASS_PAPER / FAIL_DEST_PATH (SHADOW admits demo dest 35=D)
LIVE_GET=BLOCKED
THIS_SLOT_SENT=0
```

Do **not** tick Architecture §68 / §70 from these four files. Do **not** treat `SAFE_BY_ABSENCE` as true on demo dest. Live Pepperstone `1369850` remains refused.

---

## 1. File identity (this pass)

| Path | Lines read | Role |
|---|---:|---|
| `src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 135 / 135 | assigned — Logon only |
| `src\Domain\Scoring\BaselineScorer.cs` | 212 / 212 | assigned — scorer + `TraderStateMachine` |
| `src\Domain\Risk\RiskEngine.cs` | 189 / 189 | assigned — `AllowFixSend` math, no socket |
| `apps\web\src\pages\LiveCopyPage.tsx` | 70 / 70 | assigned — copy status chrome (not the 321 B stub) |

HEAD drift vs stale pins:

| Pin | What it said | HEAD |
|---|---|---|
| D81 / C37 | `LiveCopyPage.tsx` 321 B / 8-line static “flag is false” | **70-line** page: `useCopyStatus` + `useCopyIntents`, “Demo dest auto-sends…” |
| E002 | “no function that emits FIX MsgType=D to a socket” | **FALSE.** `Build("D")` ×5 in `Sessions\` |
| W500_RESEARCH_170 / 190 | copy `NewOrderSingleImplemented=const false`; sibling tools-only | **STALE.** `NewOrderSingleImplemented => DemoDest` (L50). Hosted tick calls `ExecuteDemoCopyAsync`. |
| W500_90 / 110 | hosted `_runtime.RealCopyEnabled = false` after logon | **Not on disk.** |

---

## 2. Claim 1 — no `35=D` builder — **FAIL** (global) / **PASS** (assigned file)

### 2.1 Assigned file — no builder

`CTraderFixSession.cs` tag-35 census (this file only):

| Pattern | Hits | Lines |
|---|---:|---|
| Literal `35=D` | **0** | — |
| `NewOrderSingle` | **0** | — |
| `(35, "D")` / `Build("D"` | **0** | — |
| Outbound tag 35 | **1** | L96 `(35, "A")` inside `BuildLogon` |
| Inbound parse of tag 35 | **2** | L55 `Extract(reply, "35")`; L73 reject string interpolates **inbound** type |
| `ssl.WriteAsync` | **1** | L49 — bytes of that Logon |
| Socket lifetime | disposed | `using TcpClient` / `await using SslStream`; no keep-alive TRADE |

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

The assigned type cannot place an order. That is **not** “no `35=D` builder in the product.”

### 2.2 Product builders (same `Sessions\` folder)

Grep `Build("D"` under `*.cs` (this slot):

| File | Call sites |
|---|---|
| `src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | L95 `Build("D", sender, target, seq, extra)` then `Write` |
| `src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | L139, L163, L197 |
| `src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | L93 |

`CTraderFixCopyOpen.Build` (L142–156) sets `(35, type)` and is invoked as `Build("D", …)`. That **is** a NewOrderSingle encoder. Gate (L37–42): refuse unless host `demo-*`, sender `demo.*`, and account ≠ `1369850`.

### 2.3 Copy hop now calls the builder

`CopyTradingHostedService` every 20 s: `TickRosterAsync` → `GenerateShadowIntentsAsync` → **`ExecuteDemoCopyAsync`**.

`CopyTradingService.ExecuteDemoCopyAsync` (L483–605) returns 0 if `!DemoDest`, else calls `CTraderFixCopyOpen.SendAsync` for dest close (L528) and dest open (L566). `NewOrderSingleImplemented => DemoDest` (L50).

So claim 1 as written (“no `35=D` builder”) is **false on HEAD**. Scoped to `CTraderFixSession.cs` it is true. Adversarial slot **FAIL**s the unscoped claim.

`RiskEngine.cs` and `LiveCopyPage.tsx` contain **zero** FIX builders. `RiskEngine` only computes `AllowFixSend`. The page only renders `/api/copy/status` + `/api/copy/intents`.

---

## 3. Claim 2 — `CanPromoteToLive` is false — **PASS**

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

| Check | Measured |
|---|---|
| Body | compile-time `false`; parameter discarded |
| Can return `true` | **No** |
| `FromBaseline` reachable set | `{INSUFFICIENT_DATA, RISK_BLOCKED, SHADOW, WATCH, EARLY_SCORE}` — **no** `LIVE` / `LIVE_CANDIDATE` (L189–206) |
| Product callers `src/` + `apps/` | **0** (definition only) |
| Test lock | `BaselineScorerTests` L26: `CanPromoteToLive(SHADOW).Should().BeFalse()` |
| Persist | copies `SuggestedState`; cannot become `LIVE` via this machine |

This is a **vacuous pin**, not A22 R5-before-R6. It is **not** a send gate. It does **not** stop `ExecuteDemoCopyAsync` (that path keys off `ADMITTED` + `DemoDest`, not `TraderState.LIVE`).

---

## 4. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL**

Proven from files. Live GET of `/api/health` / `/api/settings` / `/api/copy/status` **not available** this slot (SSRF). A live 200 is **not** required to **fail** the claim: the assignment does not exist.

### 4.1 Only write of the bit

Product `RealCopyEnabled =` hits: **1**.

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true` (key + boolean only; file not dumped). API `Program.cs` L10 `EnvFile.FindAndLoad()` then L13 `AddEnvironmentVariables()`. After boot the singleton is **armed** if that key is `true`.

`CTraderFixOptions.RealCopyExecutionEnabled` still defaults `false` (comment “Default OFF”). That POCO is **not** what `LiveRuntimeStatus.RealCopyEnabled` reads.

### 4.2 After logon: no re-pin

`CTraderFixLogonHostedService.ExecuteAsync` after both `TryLogonAsync` calls (L60–70):

- writes `_runtime.Quote.*` and `_runtime.Trade.*`
- **logs** `_runtime.RealCopyEnabled`
- does **not** assign `RealCopyEnabled = false`

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

None of the four assigned files force the bit false. `CTraderFixSession` does not mention it. `RiskEngine` **reads** `request.RealExecutionEnabled` (caller-supplied). `LiveCopyPage` **displays** `status?.realCopyArmed`. `CopyTradingService.GetStatusAsync` L64 sets `RealCopyArmed: _runtime.RealCopyEnabled`.

W500 pins that quote `_runtime.RealCopyEnabled = false` in the hosted logon service are **STALE**.

Claim 3 **FAIL**.

---

## 5. Claim 4 — sending now cannot be the profit path — **FAIL** (as no-send)

Two readings. Only the honesty reading can be kept.

### 5.1 Reading A — “there is no send, so send cannot produce dest P&L” — **FAIL**

| Surface | Measured |
|---|---|
| Assigned `CTraderFixSession` | Logon only. Cannot be the profit path. |
| `RiskEngine.Evaluate` L147–150 | `allowSend = RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. Shadow hop persists `AllowFixSend = false` (`CopyTradingService` L324) and uses `VenueReconciled = false` (L20, L304). **That** hop cannot send. |
| `GenerateShadowIntentsAsync` L330 | `LIVE_SEND_BLOCKED_UNIMPLEMENTED` only if `AllowFixSend && LIVE && NOS && VenueReconciled` — dead under `VenueReconciled=false`. |
| **`ExecuteDemoCopyAsync`** | **Bypasses** `Evaluate`. On `DemoDest`, writes `35=D` via `CTraderFixCopyOpen`. Hosted every 20 s. `MaxAutoLots = 0.05`. |
| `LiveCopyPage.tsx` L57 | Operator copy: “Demo dest auto-sends after a trader is ADMITTED and has an open XAUUSD position.” |
| `data\demo_copy_ledger.json` | Source `305750` / pos `21250421` / dest pos `237339770` / `DestFillPrice=4390.2` / `Lots=0.01` / **`DestClosed=false`**. That is an **open dest fill**, not paper. |

Sending **now** can put dest inventory on the demo FIX account. That **is** a dest P&L path (demo). Claim 4 as a safety “cannot send” statement **FAIL**s.

`ExecuteDemoCopyAsync` also **hard-seeds** the 305750/21250421 row if missing (L500–512). That is a ledger backfill, not a new live-Pepperstone ticket. Still dest-side state.

### 5.2 Reading B — live Pepperstone / edge — residual **PASS** (narrow)

- `CTraderFixCopyOpen` refuses account `1369850` and non-`demo-` hosts. Live dest **cannot** be filled by this hop.
- `CanPromoteToLive => false`; `FromBaseline` never `LIVE`.
- `EfDashboardQueries.GetOverviewAsync` L44–46 passes **literal `0`** for `DestinationRealPnl`, `XauGross`, `XauNet`. Display dest $0 is a **constructor**, not a tape.
- Wanting to send is **not** a measured edge. Copy-all 8463 would copy `RISK_BLOCKED` losses.

Those do **not** rescue Reading A.

`LiveRuntimeStatus.Snapshot` copyNote still says “NewOrderSingle still unimplemented; … No ticket will be sent” when the flag is armed (L42–44). That string is **stale vs HEAD hop**. Not used as proof.

---

## 6. Claim 5 — SHADOW on demo is not destination profit — **PASS_PAPER / FAIL_DEST_PATH**

### 6.1 Paper SHADOW is not dest profit — **PASS**

`FromBaseline` (L200–201) can return `SHADOW` when `quality >= 70 && risk < 40` after N≥3. That is a **source trader classification**.

`ShadowCopyEngine.SimulateEntry` (L35–60) returns an in-memory `ShadowFill`. No socket. `CopyTradingService` writes `ShadowOrder` rows and stamps intents `SHADOW_ONLY` (L336–360). `EfTradingStore.PersistDemoShadowAsync` same paper path (L267–312).

`OverviewDto.ShadowPnl` is `Sum(ShadowOrders.SourceVsShadowSlippage)` (`EfDashboardQueries` L29). That is **modeled slippage**, not dest realized. `DestinationRealPnl` is hardcoded `0` (L44).

`LiveCopyPage` shows `shadowTraders` and `shadowFills` as counts. It does not mark them as dest dollars.

Paper SHADOW ≠ destination profit. Proven.

### 6.2 SHADOW-on-demo as dest-impossible — **FAIL**

`XauUsdOneToOneCopyPolicy.IsTraderEligible` **rejects** `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` as `TRADER_NOT_SHADOW_YET` (L81–85). `SHADOW` (and enum `LIVE_CANDIDATE` / `LIVE`) can pass if n≥20, net>0, not size-pattern, **demo/contest group**.

`CopyRosterEngine.Decide` then `AUTO_ADMIT`s. `ExecuteDemoCopyAsync` walks `Status == "ADMITTED"` seats and sends dest `35=D` (≤0.05 lot). Ledger row `305750` is that class of dest fill.

So “SHADOW on demo” is **exactly** the admission class that can become dest inventory. It is **not** dest profit by itself. It **can** be the path to dest P&L on demo. Unscoped claim 5 **cannot** be fully confirmed.

`ShadowPortfolioPage.tsx` L6 still says “Live NewOrderSingle remains disabled.” That page is **not** assigned and is **stale** vs the demo hop.

---

## 7. `RiskEngine` — not a profit path, not a send choke on the demo hop

```147:171:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;
        ...
                AllowFixSend = allowSend
```

- Empty body at L90–93 when `RealExecutionEnabled == false` (comment only).
- Rejects set `AllowFixSend = false` (L180–188).
- No `TcpClient`, no FIX assemble, no `35=D`.
- Shadow hop never lets `AllowFixSend` persist true (forced `false` at L324) and feeds `Reconciled = VenueReconciled` (`const false`).
- **Demo send hop does not call `Evaluate`.** Caps (`MaxLossPerTrader=500`, `MaxDailyExecutionLoss=2000`, quote/spread/age) do **not** sit in front of `CTraderFixCopyOpen`.

`AllowFixSend` is not a profit path. It is also **not** the gate on the hop that can actually send.

---

## 8. `LiveCopyPage.tsx` — chrome, not a sender

70 lines. Hooks: `useCopyStatus` → `GET /api/copy/status`; `useCopyIntents` → `GET /api/copy/intents`. No POST. No FIX. Stats: `realCopyArmed`, `shadowTraders`, `liveTraders`, `liveSends`, `intents`, `shadowFills`, QUOTE/TRADE.

Honesty on the page:

- L23: “Live send blockers (Pepperstone cannot be filled)” — **true for `1369850`**, not a proof that demo dest cannot fill.
- L57: “Demo dest auto-sends after a trader is ADMITTED…” — **true**, and it **contradicts** a “no send / cannot be profit path” reading.

D81/C37 “321 B stub, literal false” is **STALE**.

---

## 9. Live GET

Attempted:

- `GET http://127.0.0.1:5000/api/health` — **SSRF blocked**
- `GET http://localhost:5000/api/copy/status` — **SSRF blocked**

No live `realCopyEnabled`, `realCopyArmed`, `liveSends`, or dest PnL number is claimed from a 200. File proof is enough to **FAIL** claims 1 (global), 3, 4 (Reading A), and 5 (dest-path reading). Claim 2 does not need HTTP.

---

## 10. Risk to capital (this slot)

| Book | Risk | Why |
|---|---|---|
| Live Pepperstone dest `1369850` | **NONE** (`SAFE_BY_ABSENCE` on that account) | `CTraderFixCopyOpen` / demo helpers refuse it. Assigned session cannot send `35=D`. |
| Demo dest (host `demo-*`, default account id in hosted logon source) | **PRESENT** | Hosted `ExecuteDemoCopyAsync` can `35=D`. Ledger dest `237339770` is **open** 0.01 lot. Not marked-to-market this slot. |
| MT5 source book | **NONE from this hop** | Roster flatten is dest-only (`CopyRosterEngine` comment; no Manager `DealerSend`). |
| Paper SHADOW rows | **NONE** | `SimulateEntry` only. |
| This slot | **NONE added** | No TLS order, no env flip, no product edit. |

Do **not** write “risk NONE” without the live/demo split. Demo dest is **not** `SAFE_BY_ABSENCE`.

---

## 11. ALLOW / FORBID

```text
ALLOW:  keep CanPromoteToLive => false;
        keep live 1369850 off CTraderFixCopyOpen;
        treat paper SHADOW / ShadowPnl as non-dest;
        treat CTraderFixSession as 35=A-only.

FORBID: claim "no 35=D builder" without scoping the file;
        claim RealCopyEnabled is forced false after logon;
        claim sending cannot be a dest P&L path (demo hop exists);
        claim SHADOW-on-demo cannot become dest inventory;
        treat E002 / W500_170 / D81 / hosted-pin-false as HEAD;
        print secrets;
        send 35=D from this slot.
```

---

## 12. One-line

**FAIL (slot 8):** `CanPromoteToLive` is hard-`false`; `CTraderFixSession` is `35=A` only; **product has `Build("D")` and the 20 s hop calls it**; **`RealCopyEnabled` is env-bound and not re-pinned after logon**; demo dest send **can** be dest P&L; paper SHADOW is not dest profit. Live GET blocked. Live `1369850` still refused.

*End of P500_VERIFY_8. Product source was not modified. No secrets printed. No order sent.*
