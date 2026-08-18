# P500_VERIFY_23 — Adversarial: session / promote / RealCopy pin / send-as-profit / SHADOW≠dest

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_23.md` |
| Slot | **23** |
| Agent | P500_VERIFY_23 (adversarial verifier; independent HEAD re-read) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Topic | Confirm five claims from assigned files. **FAIL any claim not proven from a file or live GET.** |
| Assigned SUT | `CTraderFixSession.cs`, `BaselineScorer.cs`, `RiskEngine.cs`, `LiveCopyPage.tsx` |
| Adjacent (needed to prove/disprove 3–5) | `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `CTraderFixCopyOpen.cs`, `ShadowCopyEngine.cs`, `CopyRosterEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `EfDashboardQueries.cs`, `apps/api/Program.cs`, `.env` **booleans / public dest ids only** |
| Product source modified | **No.** Report + INDEX / SWARM_LOG / `P500_MANIFEST.tsv` pins only. |
| Test source modified | **No.** |
| Config / `.env` edited | **No.** `REAL_COPY` not flipped. |
| Live `35=D` / NewOrderSingle this slot | **Not sent. Not constructed.** |
| Secrets printed | **None.** Quoted only boolean `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73), public dest id `5328266`, live refuse id `1369850`. |
| Localhost API this slot | **Attempted, blocked.** `GET http://127.0.0.1:5000/api/health`, `/api/settings`, `/api/copy/status` → worker HTTP `SSRF blocked: 127.0.0.1`. Runtime `realCopyEnabled` **not** live-proven. File-only for claim 3. |

**Honesty rule:** A015 / “logon re-pins `RealCopyEnabled=false`” and “product `35=D=0` / `NOS=const false`” are **STALE vs HEAD**. This slot re-read the files. Chat is not evidence.

```text
CTraderFixSession outbound is 35=A only.
Product 35=D builders exist (CopyOpen / DemoTestTrade / DemoMatrix).
CanPromoteToLive => false.
RealCopyEnabled is NOT forced false after logon (DI binds env; .env true).
Hosted 20s tick can 35=D on demo dest; live 1369850 refused.
SHADOW ledger is SimulateEntry, not dest P&L.
Live GET this slot = SSRF blocked.
```

---

## 0. Verdict (binding)

**FAIL.** Two of five claims are file-proven. Three fail the assigned FAIL-if-unproven rule.

| # | Claim | Verdict | Why |
|---|---|---|---|
| 1 | no `35=D` builder | **FAIL** (product). **PASS_SCOPED** on `CTraderFixSession.cs` only | Assigned session file is Logon `35=A` only. Same folder + hosted hop assemble and send `35=D`. Unqualified “no builder” is false. |
| 2 | `CanPromoteToLive` is false | **PASS** | Literal `=> false`. `FromBaseline` never returns `LIVE`. Unit lock exists. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL (disproven)** | Logon host **reads** the flag; **never assigns** it. Sole write is DI bind from `REAL_COPY_EXECUTION_ENABLED`. `.env` L73 is `true`. Live GET of `/api/settings` not available this slot. |
| 4 | sending now cannot be the profit path | **FAIL (unproven / contradicted)** | Demo dest auto-copy **is** a send path (`ExecuteDemoCopyAsync` → `Build("D")`). On-disk dest fill exists. Dashboard dest PnL `0` is a constructor lie, not proof dest has no P&L. Live `1369850` send is refused — that is **not** the whole claim. |
| 5 | SHADOW on demo is not destination profit | **PASS** | `SHADOW` is source-shape. `ShadowOrder` is `SimulateEntry`. Overview `ShadowPnl` is slippage sum. `DestinationRealPnl` is literal `0` (not sourced from shadow). Residual: a SHADOW+`ADMITTED` name can still trigger **demo dest** `35=D`. That dest fill is still not the SHADOW number. |

```text
OVERALL = FAIL
  because claim 3 is disproven from HEAD files
  and claim 1/4 cannot be confirmed as stated.

PASS only: (2) CanPromoteToLive; (5) SHADOW ledger ≠ dest profit.
```

**Risk to capital:** **NONE on live `1369850`** (`CTraderFixCopyOpen` refuse + session hop `35=A` only + persist `AllowFixSend=false` + `VenueReconciled=const false` + `CanPromoteToLive=>false`). **Not absent on demo dest `5328266`** (hosted 20 s hop; ledger 305750 / dest 237339770 / 0.01 / 4390.2 / `DestClosed=false`). This slot sent **0**.

---

## 1. What was read (HEAD, this slot)

| File | Lines (this read) | Used for |
|---|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 135 | Claim 1 |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | 212 | Claim 2 |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | 189 | Claim 3–4 (AllowFixSend) |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | 70 | Claim 4–5 (UI honesty) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | 112 | Claim 3 |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 61 | Claim 3 |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | 66 | Claim 3 |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | 625 | Claim 1, 4, 5 |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 44 | Claim 4 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | 223 | Claim 1, 4 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | 391 | Claim 1 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | 287 | Claim 1 |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | 91 | Claim 5 |
| `D:\Prop\src\Domain\Copy\CopyRosterEngine.cs` | 136 | Claim 4–5 (ADMIT SHADOW) |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | 188 | Claim 4–5 |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | dest ctor | Claim 4–5 |
| `D:\Prop\apps\api\Program.cs` | 159 | Claim 3 (exposes runtime flag) |
| `D:\Prop\data\demo_copy_ledger.json` | 11 | Claim 4 (on-disk dest fill) |
| `D:\Prop\.env` L49/L50/L64/L73 | booleans + public ids | Claim 3–4 (`DemoDest` + REAL_COPY) |
| `tests/Unit/BaselineScorerTests.cs` | 74 | Claim 2 lock |

No password, token, or FIX `554=` value is quoted.

---

## 2. Claim 1 — no `35=D` builder — **FAIL** (product) / **PASS_SCOPED** (`CTraderFixSession`)

### 2.1 Assigned file: no NewOrderSingle assembler

`CTraderFixSession` has one outbound builder, `BuildLogon`, and one `WriteAsync`. Tag 35 is hardcoded `"A"`. The only other `35` uses extract the **inbound** logon reply.

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

Grep of this file: three `35` hits — inbound extract L55, reject text L73, outbound `(35, "A")` L96. **Zero** `"D"`. Sockets disposed via `using`. **PASS for this type.**

### 2.2 Product-wide “no builder” is false

Same namespace `TraderIntelligence.Fix.CTrader.Sessions` contains three `Build(type, …)` helpers that accept `"D"` and write it:

| Type | Outbound `Build("D"` / `SendD` | Role |
|---|---|---|
| `CTraderFixCopyOpen` | L95 | **Hosted** dest open/close |
| `CTraderFixDemoTestTrade` | L139, L163, L197 | Demo test flatten / open / close |
| `CTraderFixDemoMatrix` | L93 (`SendD`) | Demo scenario matrix |

`CopyTradingHostedService` (20 s) calls `ExecuteDemoCopyAsync`, which calls `CTraderFixCopyOpen.SendAsync` when `DemoDest` is true. `.env` host starts with `demo-`, trade sender starts with `demo.`, account is `5328266` (≠ `1369850`) → **`DemoDest` is true** from files.

```95:95:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
            await Write(ssl, Build("D", sender, target, seq, extra.ToArray()), timeout.Token);
```

```142:149:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
    private static string Build(string type, string sender, string target, int seq, params (int, string)[] extra)
    {
        var fields = new List<(int, string)>
        {
            (35, type), (34, seq.ToString(CultureInfo.InvariantCulture)),
            (49, sender), (56, target), (50, "TRADE"), (57, "TRADE"),
            (52, DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture))
        };
```

`NewOrderSingleImplemented => DemoDest` (`CopyTradingService` L50). Reports that still say `NOS=const false` or “product `35=D=0`” are **STALE**.

**Claim 1 as stated cannot be confirmed.** FAIL.

---

## 3. Claim 2 — `CanPromoteToLive` is false — **PASS**

```187:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
public static class TraderStateMachine
{
    public static TraderState FromBaseline(bool earlyEligible, decimal quality, decimal risk, FeatureSnapshot features)
    {
        if (features.CompletedXauTrades == 0)
            return TraderState.INSUFFICIENT_DATA;
        // ...
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

- `CanPromoteToLive` ignores `current` and returns **false**.
- Ceiling of `FromBaseline` is `SHADOW` (or `RISK_BLOCKED` / `WATCH` / `EARLY_SCORE` / `INSUFFICIENT_DATA`). **Never `LIVE` / `LIVE_CANDIDATE`.**
- `BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live` asserts `CanPromoteToLive(...).Should().BeFalse()`.

`LiveCopyPage` does not call promote. It only displays `status?.liveTraders`. That cannot flip the scorer.

**PASS.** File-proven.

---

## 4. Claim 3 — `RealCopyEnabled` forced false after logon — **FAIL (disproven)**

### 4.1 Logon host does not write the flag

`CTraderFixSession.TryLogonAsync` never mentions `RealCopyEnabled`.

After both QUOTE and TRADE attempts, `CTraderFixLogonHostedService` updates session health and **logs** the current armed bit. There is **no** `_runtime.RealCopyEnabled = false`.

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

A015 L197 / L224 (“forces `_runtime.RealCopyEnabled = false` after logon”) is **STALE vs this HEAD**.

### 4.2 Sole assignment binds env

Workspace grep of `RealCopyEnabled =` under `src/`: **one hit**.

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`.env` L73 (boolean only): `REAL_COPY_EXECUTION_ENABLED=true`.

API boot (`apps/api/Program.cs` L10) calls `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()`. `/api/health` and `/api/settings` **expose** `runtime.RealCopyEnabled` — they do not pin it false.

`CTraderFixOptions.RealCopyExecutionEnabled` **defaults** false (POCO). That type is **not** what DI writes onto `LiveRuntimeStatus`. The fix-worker reads a **different** key (`CTrader:RealCopyExecutionEnabled`, default false) and still does not send. Irrelevant to “forced false after logon.”

### 4.3 Live GET

`GET :5000/api/health` and `/api/settings` were **SSRF-blocked**. Cannot prove the running process’s boolean this slot. File bind + `.env` `true` + no post-logon write is enough to **disprove** “forced false after logon.”

**FAIL.**

---

## 5. Claim 4 — sending now cannot be the profit path — **FAIL**

Two different hops exist. Collapsing them is how stale reports greenwashed this claim.

### 5.1 Paper hop cannot send

`CopyTradingService.GenerateShadowIntentsAsync`:

- `VenueReconciled = const false` (L20) → `RiskEngine` `allowSend` is false (`Reconciled` required at `RiskEngine` L147–150).
- Persist **hardcodes** `AllowFixSend = false` (L324) even if Evaluate returned true.
- Live-send branch (L330) requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` → **dead**. Status becomes `SHADOW_ONLY`.

`RiskEngine` L90–93 when `RealExecutionEnabled == false` is an **empty** comment. It does **not** reject. It also does not matter: the persist overwrite and `VenueReconciled=false` already kill this hop.

`LiveCopyPage` “Live sends” is `ExecutionIntents.Count(SentAt != null)` (`GetStatusAsync` L57). **Zero writers** of `SentAt` were found in product `*.cs` besides the count. That counter is not the dest path.

### 5.2 Demo dest hop **can** send — and has

```21:34:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
            try
            {
                using var scope = _scopes.CreateScope();
                var copy = scope.ServiceProvider.GetRequiredService<CopyTradingService>();
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

`ExecuteDemoCopyAsync` **bypasses** `RiskEngine.Evaluate`. If `DemoDest`, it calls `CTraderFixCopyOpen.SendAsync` for `ADMITTED` roster seats with open XAU ≤ `MaxAutoLots` (0.05) and for ledger closes when source `Completed`.

`.env` makes `DemoDest` true (host `demo-…`, sender `demo.…`, account `5328266` ≠ `1369850`).

On-disk dest fill (`D:\Prop\data\demo_copy_ledger.json`):

- source login `305750`, source pos `21250421`
- dest pos `237339770`, dest px `4390.2`, lots `0.01`, **`DestClosed=false`**

`LiveCopyPage` empty-state (L57) and `GetStatusAsync` summary when `DemoDest` (L76–77) **tell the operator** dest auto-sends on the 20 s tick. The page heading is “Live copy portfolio.”

### 5.3 Why “cannot be the profit path” is unproven

| Interpretation | File result |
|---|---|
| Live Pepperstone `1369850` cannot be the send/profit path | **True** — CopyOpen refuse + session `35=A` only |
| Dashboard dest P&L is not computed from sends | **True** — `EfDashboardQueries` passes literal `0` into `DestinationRealPnl` |
| Therefore sending cannot produce dest P&L | **False / unproven** — dest fill + price exist on the demo ledger; DTO `0` is a lie, not a venue statement |
| Sending is off so it cannot be how we make money | **False** — hosted hop is on |

Cannot prove the unqualified claim from a file or live GET. **FAIL.**

`LiveCopyPage` blockers (“Pepperstone cannot be filled”) are **omitted when `DemoDest`** (`BuildBlockers` L610–616 only adds `SAFE_BY_ABSENCE` / “0 LIVE” when `!DemoDest`). UI does not hide the demo send path.

---

## 6. Claim 5 — SHADOW on demo is not destination profit — **PASS**

### 6.1 SHADOW is a source-shape label

`FromBaseline` emits `SHADOW` when `quality >= 70 && risk < 40` after ≥3 completed XAU (`BaselineScorer` L200–201). Inputs are reconstructed **source** trades (`NetRealizedPnl`, martingale, SL use). No dest fill, no dest commission, no dest spread.

`CanPromoteToLive => false` keeps that label off `LIVE`.

### 6.2 Shadow ledger is simulated

`ShadowCopyEngine.SimulateEntry` marks a modeled bid/ask + 0.05 pt latency slip. `GenerateShadowIntentsAsync` writes `ShadowOrder` rows from that fill when a dest **quote row** exists and Evaluate `Approve`s — still **no venue send**.

Overview “shadow P&L” is `Sum(SourceVsShadowSlippage)` (`EfDashboardQueries` L29), **not** dest realized. `DestinationRealPnl` is the next constructor argument and is **literal `0`** (L44).

`ShadowOrder` has no dest ticket / dest PnL field.

### 6.3 Residual (does not flip PASS)

`XauUsdOneToOneCopyPolicy.IsTraderEligible` **allows** `SHADOW` (rejects only `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` / blocked). `CopyRosterEngine` can `AUTO_ADMIT` those names on demo/contest groups. `ExecuteDemoCopyAsync` then sends dest `35=D` for `ADMITTED` seats.

So: **SHADOW traders on demo can cause dest fills.** Those fills are dest execution. They are **still not** the SHADOW score, the shadow slippage sum, or “SHADOW profit.” Claim 5 as written is about not treating the SHADOW number as dest profit. **PASS.**

`LiveCopyPage` shows `SHADOW traders` and `Shadow fills` as separate stats from `Live sends`. It does not add them into dest P&L.

---

## 7. RiskEngine notes (assigned file, not a sixth claim)

- 16 increasing-exposure rejects force `AllowFixSend=false`.
- Approvals set `AllowFixSend = RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`.
- Hosted paper hop passes `Reconciled=false` and then overwrites persist to `false`.
- Hosted **demo send hop does not call** `Evaluate`. RiskEngine is not a capital gate on dest `35=D`.

This supports claim 4 FAIL (send bypass) and does not change claim 2/5.

---

## 8. Live GET matrix (this slot)

| URL | Result | Usable as proof? |
|---|---|---|
| `http://127.0.0.1:5000/api/health` | SSRF blocked | **No** |
| `http://127.0.0.1:5000/api/settings` | SSRF blocked | **No** |
| `http://127.0.0.1:5000/api/copy/status` | SSRF blocked | **No** |

Process `realCopyEnabled`, quote/trade logon, and live dest P&L are **unverified at runtime**. File + `.env` boolean used instead. That is enough to fail claim 3; not enough to pass any runtime-only claim.

---

## 9. Stale pins this slot invalidates

| Pin | Why stale |
|---|---|
| A015 / “logon forces `RealCopyEnabled=false`” | Hosted service logs the flag; DI binds env |
| “product `35=D=0`” / “NOS `const false`” | `NOS => DemoDest`; `Build("D")` on CopyOpen + two demo types |
| “sending is off so it cannot be the profit path” | 20 s `ExecuteDemoCopyAsync`; ledger fill 305750 open |
| “SAFE_BY_ABSENCE everywhere” | True for live `1369850` only |

---

## 10. One-line

**FAIL.** `CTraderFixSession` is still `35=A` only and `CanPromoteToLive` is still `false`; SHADOW paper is not dest P&L. But `RealCopyEnabled` is **env-bound `true` and not re-pinned after logon**, and demo dest **does** have a `35=D` builder on the hosted tick — so “no 35=D builder / sending cannot be the profit path / RealCopy forced false after logon” cannot be confirmed.

Live capital **NONE** (`1369850` refused). Demo dest residual **not** this slot’s send.
