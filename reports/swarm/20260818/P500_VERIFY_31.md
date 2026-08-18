# P500_VERIFY_31 — Adversarial confirm of five profit-path claims (slot 31)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_31.md` |
| Agent / slot | P500 adversarial verifier **31** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned SUTs | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` (full re-read this slot) |
| Adjacent hops (not assigned SUTs) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyGroupFilter.cs`, `DealIngestionService.cs`, `apps/api/Program.cs`, `apps/web/src/api/hooks.ts`, lab `.env` L73 **boolean only** |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Secrets printed | **None.** Quoted only the already-on-disk boolean `REAL_COPY_EXECUTION_ENABLED=true` and public dest ids `5328266` / `1369850`. Tag 554 / passwords / proxy / DB strings never dumped. |
| Live GET this slot | **Blocked.** `web_fetch` SSRF-denied `http://127.0.0.1:5000/api/health`, `/api/copy/status`, `http://localhost:5000/api/settings`. No shell. Any claim that needs a live DTO is **FAIL**. |
| This slot sent `35=D` | **No** |
| `REAL_COPY` flipped | **No** |
| Method | Independent `read_file` of the four assigned files (full). Targeted `grep` for `35=D` / `Build("D")` / `CanPromoteToLive` / `RealCopyEnabled` / `TraderState.LIVE`. Adjacent hops opened only to try to *disprove* a claim. Prior swarm text (`E002`, `W500_VERIFY_*`, `P500_BOOK_*`) treated as **untrusted**. |

**Honesty rule:** FAIL any assigned claim that is not proven from a file this slot or a live GET this slot. A compile-time default is not a runtime pin. `CTraderFixSession` having no `35=D` is not “the product has no `35=D` builder.” `AllowFixSend` on a risk DTO is not a socket write. `SHADOW` is a source state, not dest cash. Demo dest fills are not live Pepperstone profit. Do **not** print secrets.

---

## 0. Verdict (binding)

**FAIL — claim 3 is disproven from live files. Live GET unobtainable this slot (does not rescue claim 3).**

| # | Assigned claim | File-proven result | Class |
|---|---|---|---|
| 1 | no `35=D` builder | **PASS** on assigned `CTraderFixSession.cs` (135/135). **FAIL** if read as product-wide. | Assigned file outbound MsgType is only `(35, "A")`. Sibling `CTraderFixCopyOpen.Build("D")` is a real builder and is **on** the hosted 20s hop. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive` is `=> false`. `FromBaseline` never emits `LIVE` or `LIVE_CANDIDATE`. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** | Hosted logon **never writes** the flag. DI binds `.env` `REAL_COPY_EXECUTION_ENABLED=true`. |
| 4 | sending now cannot be the profit path | **PASS** (as worded) | Scorer cannot mint `LIVE`. Persist `AllowFixSend=false`. Venue const unreconciled. UI is GET-only. Demo `35=D` exists and is **not** live dest +EV. |
| 5 | SHADOW on demo is not destination profit | **PASS** | `SHADOW` is a quality/risk label on **source** XAU. Paper `ShadowOrder` is `SimulateEntry`. Policy admits demo/contest source groups only. That is not dest cash PnL. |

One-line:

```text
FAIL. CTraderFixSession is 35=A only. CanPromoteToLive is false. RealCopyEnabled is NOT forced false after logon (.env true + DI bind + no re-pin). Sending now is not the live profit path. SHADOW-on-demo is not dest profit. Demo dest hop can still Build("D") (refuses 1369850). Live GET blocked. Risk NONE on live 1369850; DEMO dest hop exists.
```

---

## 1. Claim 1 — no `35=D` builder — **PASS** (assigned file) / **FAIL** (product-wide)

Assigned file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines, full read).

Outbound builder is only `BuildLogon`:

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
| Tag 35 outbound | `"A"` only (`L96`) |
| `WriteAsync` count | **1** (`L49`) — the logon |
| `NewOrderSingle` / `Build("D")` / `"D"` MsgType | **0** |
| Socket lifetime | `using TcpClient` + `await using SslStream` (`L35–L39`) — disposed after one read |
| Inbound `35` | `Extract(reply, "35")` (`L55`) to accept Logon (`"A"`) or record reject. Not a builder. |

`grep` `35=D` under `*.cs` = **0** string literals. Builders use `Build("D", ...)`.

**Adjacent (does not flip the assigned-file PASS; kills a product-wide reading):**

| File | What it does |
|---|---|
| `CTraderFixCopyOpen.cs` L95 | `Build("D", sender, target, seq, extra)` then `Write` on TRADE **5212** |
| `CTraderFixDemoMatrix.cs` L93 | `Build("D", ...)` |
| `CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | `Build("D", ...)` ×3 |
| `CopyTradingService.ExecuteDemoCopyAsync` L528 / L566 | **calls** `CTraderFixCopyOpen.SendAsync` |
| `CopyTradingHostedService` L30 | 20s tick calls `ExecuteDemoCopyAsync` |

`CTraderFixCopyOpen` refuses live dest identity (`host` must start `demo-`, `sender` must start `demo.`, `account == "1369850"` fails closed) (`L37–L42`). That is a **demo dest sender**, not absence of a builder.

**Stale reports to ignore:** any `E002` / `W500_*` / `P500_BOOK_*` line that still says product `35=D=0` or `NewOrderSingleImplemented` const `false`. HEAD is `NewOrderSingleImplemented => DemoDest` (`CopyTradingService.cs` L50).

Claim 1 as “`CTraderFixSession` has no `35=D` builder” is **proven**. Claim 1 as “there is no `35=D` builder in this tree” is **false**. Assigned wording after “Read `CTraderFixSession.cs`” is the scoped reading → **PASS**.

---

## 2. Claim 2 — `CanPromoteToLive` is false — **PASS**

Assigned file `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212 lines, full read).

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

| Fact | Evidence |
|---|---|
| `CanPromoteToLive` | literal `=> false`; parameter `current` unused |
| `FromBaseline` terminals | `INSUFFICIENT_DATA` / `RISK_BLOCKED` / `SHADOW` / `WATCH` / `EARLY_SCORE` only |
| `LIVE` / `LIVE_CANDIDATE` emitted | **0** |
| Score persistence | `DealIngestionService` L140 `CurrentState = score.SuggestedState` — no later promotion writer found |
| Unit pin | `BaselineScorerTests` “three disciplined winners go to **SHADOW not LIVE**” + `CanPromoteToLive(...).Should().BeFalse()` |

`TraderState.LIVE = 5` exists on the enum (`TraderState.cs` L10). Existence of the enum value is **not** promotion. `CopyTradingService` still *counts* `CurrentState == LIVE` (L58) and *branches* on it (L330). That branch cannot fire from this scorer.

No live GET required. **PASS.**

---

## 3. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL**

None of the four assigned files force `RealCopyEnabled = false` after logon.

| Assigned file | `RealCopyEnabled` |
|---|---|
| `CTraderFixSession.cs` | **absent**. Logon only. Does not touch runtime. |
| `BaselineScorer.cs` | **absent**. |
| `RiskEngine.cs` | reads `request.RealExecutionEnabled` as a **caller bit** (L90–93 comment; L147 `allowSend`). Does not force it false. |
| `LiveCopyPage.tsx` | **displays** `status?.realCopyArmed` (`L13`). GET-only. Does not write the flag. |

The only product write of `LiveRuntimeStatus.RealCopyEnabled` is DI construction:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Lab `.env` L73 (boolean only, no other keys quoted):

```env
REAL_COPY_EXECUTION_ENABLED=true
```

API host loads that env (`apps/api/Program.cs` L10 `EnvFile.FindAndLoad()` + L13 `AddEnvironmentVariables()`), then `AddTraderIntelligence` (L15).

Hosted FIX logon **reads** the flag for a log line and **does not re-pin it**:

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

`grep` `RealCopyEnabled\s*=` under product `*.cs` = **one** hit: DI L41.

`CTraderFixOptions.RealCopyExecutionEnabled` still defaults `false` (`CTraderFixOptions.cs` L35). That POCO is **not** what `LiveRuntimeStatus` uses. A compile-time default is not a post-logon force.

Live GET `/api/health` / `/api/settings` would have shown the runtime boolean. Those GETs were **blocked**. Claim 3 is already **disproven from files**; a GET is not required to FAIL it.

**FAIL.**

---

## 4. Claim 4 — sending now cannot be the profit path — **PASS** (as worded)

The claim is **not** “nothing can send.” It is “sending **now** cannot be **the profit path**.”

### 4.1 Assigned UI cannot send

`LiveCopyPage.tsx` (70 lines, full read):

- Hooks: `useCopyStatus` + `useCopyIntents` only (`L1–L5`).
- Those hooks are `client.get('/api/copy/status')` and `client.get('/api/copy/intents')` (`hooks.ts` L60–L65). **No POST.**
- No button, no `fetch` write, no “send now.”
- Empty-state copy (`L57`) admits demo dest auto-send after `ADMITTED` — display honesty, not a click-to-send control.

API `MapPost` in `Program.cs` is `/api/ops/resync` only (ingest/score). No order endpoint.

### 4.2 Scorer + risk hop cannot mint a live ticket

- Claim 2: `CanPromoteToLive => false`; `FromBaseline` never `LIVE`.
- `CopyTradingService.VenueReconciled` is `const false` (`L20`). Generate hop passes `Reconciled = VenueReconciled` (`L304`).
- `RiskEngine` L84–85: unreconciled **increasing** action → `Reject` / `VENUE_NOT_RECONCILED` / `AllowFixSend = false`.
- Persist overwrites even a theoretical approve:

```317:333:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
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
```

Even if `AllowFixSend` were true **and** state were `LIVE` **and** `VenueReconciled` were true, the live branch writes a **status string** and does not call a FIX writer.

`RiskEngine` L147–150 can theoretically set `AllowFixSend=true` when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. That is a **boolean on a record**, not a profit path. Unit fixture keeps `RealExecutionEnabled=false` (`RiskEngineTests` L21–L26).

### 4.3 Demo dest send exists — still not the profit path

`CopyTradingHostedService` L27–30 every 20s: `TickRosterAsync` → `GenerateShadowIntentsAsync` → `ExecuteDemoCopyAsync`.

`ExecuteDemoCopyAsync` (`CopyTradingService.cs` L483–605):

- Returns 0 unless `DemoDest` (host `demo-` + sender `demo.` + account ≠ `1369850`).
- Bypasses `RiskEngine.Evaluate`.
- Calls `CTraderFixCopyOpen.SendAsync` (L528 close, L566 open) which emits `Build("D")`.
- Caps `MaxAutoLots = 0.05` (L23, L558).
- Opens only `ADMITTED` roster seats (`L542–L544`).
- On-disk ledger `D:\Prop\data\demo_copy_ledger.json` already records dest pos `237339770` for source `305750` / `0.01` / dest fill `4390.2` / `DestClosed=false`. **This slot did not send it.**

That hop can place a **demo** ticket. It cannot be the **profit path** this swarm is scoring:

- Live dest `1369850` is refused in the builder (`CTraderFixCopyOpen` L39).
- Policy copies **demo/contest source groups** (`CopyGroupFilter.IsDemoOrContest`; `XauUsdOneToOneCopyPolicy` L105–109 `NOT_DEMO_OR_CONTEST_GROUP`). Challenge-pass tape is not dest expectancy.
- `AllocationFactor = 1m` (`XauUsdOneToOneCopyPolicy` L67) is 1:1 size, not a measured +EV clip.
- `CTraderFixSession` itself still cannot send (claim 1). Hosted logon disposes the socket after `35=A`.
- No live GET of dest PnL this slot. I **do not** claim a dollar dest PnL. Absence of a measured dest-net is enough: sending now is not a proven profit path.

**PASS** the assigned wording. **Do not** upgrade this to `SAFE_BY_ABSENCE` of all `35=D`.

---

## 5. Claim 5 — SHADOW on demo is not destination profit — **PASS**

`SHADOW` is assigned only when `quality >= 70 && risk < 40` after 3+ completed XAU (`BaselineScorer.cs` L200–201). Inputs are **source** reconstructed trades (`NetRealizedPnl`, martingale, SL use, …). There is no dest fill, dest spread, or dest cash field on `FeatureSnapshot` / `BaselineScore`.

Paper dest:

```337:360:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    intent.Status = "SHADOW_ONLY";
                    if (quote is not null && decision.Outcome == RiskDecisionOutcome.Approve)
                    {
                        var fill = _shadow.SimulateEntry(
                            intent.Id.ToString(),
                            trade.Direction,
                            qty,
                            trade.EntryVwap,
                            quote,
                            now,
                            TimeSpan.FromMilliseconds(80));
                        _db.ShadowOrders.Add(new ShadowOrder
                        {
                            ...
                        });
                    }
```

`ShadowCopyEngine.SimulateEntry` marks a synthetic ask/bid + 0.05 latency slip. That is a **row**, not venue cash.

Policy: a trader in `WATCH` / `EARLY_SCORE` / `INSUFFICIENT_DATA` is `TRADER_NOT_SHADOW_YET` (`XauUsdOneToOneCopyPolicy` L81–85). Eligibility also requires `CopyGroupFilter.IsDemoOrContest` (`L105–109`). So the SHADOW hopper is **demo/contest source**. Copying a demo challenge login onto a demo FIX account is still not **destination profit** on live capital.

`LiveCopyPage` separates “SHADOW traders” / “Shadow fills” from “Live sends” (`L14–L16`). The page does not show dest realized PnL.

I did **not** re-GET `/api/overview` (SSRF). I therefore **do not** certify any dashboard dest-PnL integer this slot. File proof is sufficient: SHADOW ≠ dest profit.

**PASS.**

---

## 6. Risk to capital

| Surface | This slot |
|---|---|
| Live Pepperstone dest `1369850` | **NONE.** `CTraderFixCopyOpen` + `CTraderFixDemoTestTrade` refuse that account. `CTraderFixSession` cannot send `35=D`. |
| Demo dest (host `demo-*`, sender `demo.*`) | **HOP EXISTS.** Hosted 20s `ExecuteDemoCopyAsync` → `Build("D")`. Ledger already has an open 0.01 dest. This slot did not send. |
| MT5 source book | **Not touched** by FIX send. Roster flatten writes dest intents only. |
| UI “send now” | **None.** GET-only page. |
| `REAL_COPY_EXECUTION_ENABLED=true` | **Armed in runtime config**, not a live-1369850 sender. Next real sender would see the bit true. |

---

## 7. What this slot did **not** prove

- Live process `realCopyEnabled` DTO (GET blocked).
- Whether the API process is running, FIX TRADE is logged on, or `DemoDest` is true **in the running host**.
- Dest realized PnL dollars.
- That demo dest is flat or closed (`DestClosed=false` on the on-disk ledger).
- SHA-256 of the four files (no shell).

---

## 8. Files read (this slot)

Assigned:

- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135)
- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (212/212)
- `D:\Prop\src\Domain\Risk\RiskEngine.cs` (190/190)
- `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (70/70)

Adjacent:

- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` (refuse gate)
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs`
- `D:\Prop\src\Domain\Copy\CopyGroupFilter.cs`
- `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs`
- `D:\Prop\src\Domain\Copy\CopyLifecycle.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` (score persist)
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\web\src\api\hooks.ts`
- `D:\Prop\tests\Unit\BaselineScorerTests.cs`
- `D:\Prop\tests\Unit\RiskEngineTests.cs`
- `D:\Prop\data\demo_copy_ledger.json` (public dest ids only)
- `.env` L73 boolean only

---

*End of P500_VERIFY_31. Product source was not modified. No `35=D` was built or sent by this slot. No secrets printed. `REAL_COPY` was not flipped.*
