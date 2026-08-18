# P500_VERIFY_93 — Adversarial profit-path verify (slot 93)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_VERIFY_93.md` |
| Agent / slot | P500 adversarial **verify 93** |
| Date | 2026-08-18 |
| Role | Independent verifier. **Did not trust** sibling `P500_BOOK` / `P500_VERIFY` / `W500` / `CREDENTIALS` / README prose. Re-read assigned files + adjacent send/logon hop this pass. |
| Assigned files | `CTraderFixSession.cs` (**135/135**), `BaselineScorer.cs` (**212/212**), `RiskEngine.cs` (**189/189**), `LiveCopyPage.tsx` (**70/70**) |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Quoted boolean keys + already-public host prefix / account ids only. |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live GET this pass | **Blocked.** `web_fetch` `http://127.0.0.1:5000/api/health` → `SSRF blocked: 127.0.0.1 resolves to private/internal IP`. **No** live GET body. Any claim that needs a live body is **FAIL**. |
| Live attach / send this pass | **No.** No Manager Connect. No TLS. No Logon. No `35=D`. This slot sent **0**. |
| Method | Full `read_file` of the four assigned files. Adjacent this pass: `CTraderFixLogonHostedService.cs`, `CTraderFixCopyOpen.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixDemoMatrix.cs`, `CopyTradingService.cs` (625), `CopyTradingHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `apps/api/Program.cs`, `EnvFile.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs`, `ShadowCopyEngine.cs`, `XauUsdOneToOneCopyPolicy.cs`, `CopyRosterEngine.cs`, `CopyGroupFilter.cs`, `CopyLifecycle.cs`, `DemoCopyLedger.cs`, `data/demo_copy_ledger.json` (public dest ids only), `hooks.ts`, `launchSettings.json` (API `:5000`), `BaselineScorerTests.cs`. Grep: `Build("D")` / `RealCopyEnabled =` / `CanPromoteToLive` / `DestinationRealPnl`. Flag-only `.env` L49/L50/L56/L64/L73/L106. |

**Honesty rule:** FAIL any claim that cannot be proven from a file this slot or a live GET. Prior swarm prose is not evidence. A TLS Logon `35=A` is not a NewOrderSingle. `SAFE_BY_ABSENCE` on live `1369850` is not “flag stays false.” A demo hopper that can `Build("D")` is not `CTraderFixSession`. Destination constructor `$0` is not a measured dest book. Wanting profit is not an edge. Do not print secrets.

```text
CTraderFixSession outbound is 35=A only (135/135).
Product 35=D builders exist (CopyOpen / DemoTestTrade / DemoMatrix) ×5.
CanPromoteToLive => false.
RealCopyEnabled is NOT forced false after logon (DI binds env; .env L73 true).
Hosted 20s tick can 35=D on demo dest; live 1369850 refused.
SHADOW ledger is SimulateEntry / slippage sum, not dest P&L.
SHADOW is also the dest AUTO_ADMIT floor.
Live GET this slot = SSRF blocked.
```

---

## 0. Verdict (binding)

**FAIL.** Only claim 2 is fully proven as written. Claim 1 is session-only. Claim 3 is **disproven**. Claims 4–5 fail the dest-safety bar (demo hopper sends; SHADOW is dest ADMIT class). Live GET absent.

| # | Assigned claim | Verdict | Proof class |
|---|---|---|---|
| 1 | no `35=D` builder | **FAIL** as written | Assigned `CTraderFixSession` is `35=A` only (**PASS_SESSION**). Unscoped “no builder” is **false**: `Build("D")` ×5 in sibling session files; hosted hop calls `CTraderFixCopyOpen`. |
| 2 | `CanPromoteToLive` is false | **PASS** | `TraderStateMachine.CanPromoteToLive` in `BaselineScorer.cs` L211 is `=> false`. Parameter discarded. `FromBaseline` never returns `LIVE`. Product `src` callers: **0**. Unit test locks SHADOW-not-LIVE. |
| 3 | `RealCopyEnabled` forced false after logon | **FAIL** (disproven) | DI L41 binds env. Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad` then `AddEnvironmentVariables`. Hosted logon **reads** `_runtime.RealCopyEnabled` at L70 and **never assigns false**. Product `RealCopyEnabled =` writers: **1** (DI bind). Live GET blocked. |
| 4 | sending now cannot be the profit path | **FAIL** | Cannot prove. Hosted `ExecuteDemoCopyAsync` **sends now** on DemoDest (`Build("D")`, no `RiskEngine.Evaluate`). On-disk ledger has an **open** dest fill. Dest DTO constructor `0` is **not** dest-account P&L. Live GET of dest book blocked. Live `1369850` still refused. |
| 5 | SHADOW on demo is not destination profit | **FAIL** as dest-safety | Paper SHADOW (`SimulateEntry` / Σ slippage / source `NetRealizedPnl`) is **not** dest cash (**PASS_PAPER**). Residual is load-bearing: policy eligibility floor **is** SHADOW; roster `AUTO_ADMIT`s it; `ExecuteDemoCopyAsync` ignores `CurrentState`/`LIVE`. SHADOW-on-demo **can** become dest `35=D`. |

**Overall slot verdict: FAIL** (instruction: FAIL any claim that cannot be proven from a file or live GET).

**Risk to capital:** **NONE on live Pepperstone `1369850`** (`SAFE_BY_ABSENCE` on `CTraderFixSession` + CopyOpen refuse). **Not absent on demo dest `5328266`** (hosted `ExecuteDemoCopyAsync` → `CTraderFixCopyOpen.Build("D")`; ledger open dest `237339770` @ 0.01). Flag may be **armed**; that is **not** a live-send license. Do not paper over claim 3.

Stale siblings this slot contradicts: `CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**” and “Live `35=D` … method does not exist”; README “Real NewOrderSingle is **off** (`…=false`)”; any BOOK that still pins product `35=D=0` / `NOS=const false` / persist L306 / logon re-pin false.

---

## 1. no `35=D` builder — FAIL as written (PASS_SESSION only)

### 1.1 Assigned session file (full read, 135/135)

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`

Public API is **only** `TryLogonAsync`. The sole outbound builder:

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

| Fact | Measured this pass |
|---|---|
| Physical lines | **135 / 135** (ends L135 `}`) |
| Literal `35=D` / `(35, "D")` / `Build("D")` | **0** |
| `NewOrderSingle` identifier | **0** |
| Outbound tag 35 actually built | **`"A"` only** (L96) |
| Other tag-35 uses | inbound extract L55 (`msgType == "A"`); reject text L73 (`Logon rejected 35={msgType}`) |
| `WriteAsync` | **1** (L49 logon bytes) |
| Socket / SSL | `using` TcpClient + `await using` SslStream — disposed on return |
| Called from | `CTraderFixLogonHostedService` L48–58, twice (QUOTE 5211, TRADE 5212) |

A Logon `35=A` is **not** a NewOrderSingle. Session-scoped “no `35=D` builder” **PASS_SESSION**.

### 1.2 Product tree (required because the claim is unscoped)

Grep `Build("D")` on `*.cs` this pass = **5** call sites:

| File | Line | Role |
|---|---|---|
| `src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs` | L95 | `await Write(ssl, Build("D", …))` after SecurityList; **hosted hop** |
| `src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | L139, L163, L197 | flatten / market / close |
| `src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | L93 | `SendD` → `Build("D", …)` |

Generic builders that accept any MsgType (including `"D"`):

```142:146:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixCopyOpen.cs
    private static string Build(string type, string sender, string target, int seq, params (int, string)[] extra)
    {
        var fields = new List<(int, string)>
        {
            (35, type), (34, seq.ToString(CultureInfo.InvariantCulture)),
```

CopyOpen **refuses live** `1369850` / non-`demo-` host / non-`demo.` sender (L37–42), then **does** emit `35=D` on demo dest. `CopyTradingService.ExecuteDemoCopyAsync` L528 and L566 call `CTraderFixCopyOpen.SendAsync`. `CopyTradingHostedService` L30 invokes that every 20s.

Unscoped “no `35=D` builder” is **false**. **FAIL**.

---

## 2. `CanPromoteToLive` is false — PASS

Assigned `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` **212/212**.

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
```

| Fact | Measured |
|---|---|
| Body | constant `false`; `current` unused |
| `FromBaseline` returns | `INSUFFICIENT_DATA` / `RISK_BLOCKED` / `SHADOW` / `WATCH` / `EARLY_SCORE` only |
| `LIVE` / `LIVE_CANDIDATE` from scorer | **never** |
| Product `src` callers of `CanPromoteToLive` | **0** (definition + unit test + `_tmp` harness only) |
| Unit lock | `BaselineScorerTests.cs` L25–26: three winners → `SHADOW`; `CanPromoteToLive(...).Should().BeFalse()` |

**PASS.** Promotion to LIVE is not automatic and is not implemented as a function of state.

Caveat (not a FAIL of this claim): dest hop **does not need LIVE**. Roster `AUTO_ADMIT` + `ExecuteDemoCopyAsync` copy **ADMITTED** seats. That is claim 4/5, not a promotion-to-LIVE lie.

---

## 3. `RealCopyEnabled` forced false after logon — FAIL (disproven)

Assigned files do **not** assign `RealCopyEnabled`. The logon host is the only “after logon” writer.

### 3.1 What logon actually does

`CTraderFixLogonHostedService.ExecuteAsync` after two `TryLogonAsync` calls:

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

It **reads** `_runtime.RealCopyEnabled` for a log line. It **does not** assign `false`. Persist path writes `FixSessionState` rows only. Comment “NewOrderSingle still unimplemented” is **not** a runtime pin.

### 3.2 What actually sets the bit

Sole product assignment this pass:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Grep `RealCopyEnabled\s*=` on `*.cs` = **that one line**.

API boot: `Program.cs` L10 `EnvFile.FindAndLoad()` then L13 `AddEnvironmentVariables()`. `EnvFile` L15 hard-includes `D:\Prop\.env`. Lab `.env` L73: `REAL_COPY_EXECUTION_ENABLED=true`. API `/api/settings` L76 exposes `runtime.RealCopyEnabled` (not a literal false). `LiveCopyPage.tsx` L13 renders `status?.realCopyArmed ? 'YES' : 'NO'`.

Live GET that would confirm the **process** bit: **SSRF-blocked**. File proof already **disproves** “forced false after logon.” **FAIL**.

---

## 4. sending now cannot be the profit path — FAIL

### 4.1 What the assigned files prove (not enough)

| Path | Can send `35=D`? | Book dest profit? |
|---|---|---|
| `CTraderFixSession` | **No** (Logon `35=A` only) | No |
| `RiskEngine.Evaluate` | Sets `AllowFixSend` from `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Rejects force `AllowFixSend=false` (L187). Does **not** send. | N/A |
| Persist hop `CopyTradingService` L324 | **Hard-writes** `AllowFixSend = false` (ignores `decision.AllowFixSend`) | No |
| `VenueReconciled` | `const false` L20; Evaluate therefore cannot approve send even if flag is armed | No |
| LIVE send branch L330 | requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` — last is const false | Dead |
| Dashboard `DestinationRealPnl` | `EfDashboardQueries.cs` L44 literal **`0`** | Constructor, **not** a mark |
| `LiveCopyPage.tsx` | Display only. Empty-state L57: “Demo dest auto-sends after a trader is ADMITTED…” | UI documents a send hop |

Session + persist + dest DTO **cannot** book dest profit. That is **PASS_NOT_BOOKED_DEST_PROFIT** for the *dashboard / session* slice only.

### 4.2 Residual that fails the unscoped claim

`NewOrderSingleImplemented => DemoDest` (L50), **not** const false.

`DemoDest` is true when host starts with `demo-`, trade sender starts with `demo.`, and account is **not** `1369850`. Lab `.env` public keys this pass: host `demo-us-eqx-01.p.c-trader.com` (L49), trade sender `demo.pepperstone.5328266` (L64), account `5328266` (L50). File-true DemoDest.

Hosted 20s tick:

```28:30:D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
```

`ExecuteDemoCopyAsync` (L483–605):

- Returns 0 only if `!DemoDest` or password missing.
- **Does not** call `RiskEngine.Evaluate`.
- **Does not** read `RealCopyEnabled`.
- Seeds ledger row `305750` / `21250421` / dest `237339770` if absent (L500–512).
- Closes dest when source completed (`CopyLifecycle.ShouldCloseDest`) via `CTraderFixCopyOpen.SendAsync(..., destPositionId)`.
- Opens dest for each `ADMITTED` roster seat with an open XAU ≤ `MaxAutoLots` (0.05) via `CTraderFixCopyOpen.SendAsync` → `Build("D")`.
- Marks intent `DEMO_SENT`.

On-disk `D:\Prop\data\demo_copy_ledger.json` this pass (public dest ids only):

| Field | Value |
|---|---|
| SourceLogin | `305750` |
| SourcePositionId | `21250421` |
| Lots | `0.01` |
| DestPositionId | `237339770` |
| DestClOrdId | `C20260818093047317` |
| DestFillPrice | `4390.2` |
| DestClosed | **`false`** |

That is an **open dest fill** on demo account `5328266`. Whether that ticket is still live at the venue is **unproven** (no live GET). File proof is enough: sending **can** be dest P&L. Dest DTO `$0` is a **lie about measurement**, not a closed book.

Live `1369850` remains refused. **FAIL** the unscoped claim. **PASS** only as “not booked dest profit on the dashboard / not live `1369850`.”

---

## 5. SHADOW on demo is not destination profit — FAIL as dest-safety (PASS_PAPER)

### 5.1 Paper SHADOW is not dest cash — PASS_PAPER

| Source of “SHADOW profit” | What it actually is |
|---|---|
| `BaselineScorer` / `FromBaseline` L200–201 | Source reconstructed XAU `NetPnl = Sum(NetRealizedPnl)`. Ceiling state `SHADOW`. |
| `ShadowCopyEngine.SimulateEntry` | Modeled ask/bid + 0.05 latency points. **No socket.** |
| Persist L337–359 | On Approve, writes `ShadowOrders` from `SimulateEntry` and status `SHADOW_ONLY`. |
| Dashboard `ShadowPnl` | `EfDashboardQueries` L29: `Sum(SourceVsShadowSlippage)` |
| Dashboard `DestinationRealPnl` | literal `0` |

Paper SHADOW / slippage is **not** dest-account cash. **PASS_PAPER**.

### 5.2 SHADOW is the dest ADMIT class — FAIL dest-safety

`XauUsdOneToOneCopyPolicy.IsTraderEligible` **rejects** `INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` with `TRADER_NOT_SHADOW_YET` (L81–85). Eligible states are therefore **SHADOW / LIVE_CANDIDATE / LIVE** (plus demo/contest group, ≥20 XAU, net>0, no size pattern). Scorer never emits LIVE, so the **live eligibility floor is SHADOW**.

`CopyRosterEngine.Decide` L72–80: if eligible → `AUTO_ADMIT` / `KEEP`. Does **not** require `LIVE`.

`GenerateShadowIntentsAsync` L202: copyable = `{SHADOW, LIVE_CANDIDATE, LIVE}`.

`ExecuteDemoCopyAsync` L542–569: iterates `Status == "ADMITTED"` roster seats. **Ignores** `TraderScores.CurrentState`. **Ignores** `RealCopyEnabled`. Demo/contest group filter is the source-side gate (`CopyGroupFilter.IsDemoOrContest`).

Therefore: a **SHADOW** trader on a **demo/contest** source group **can** be `AUTO_ADMIT`ted and then dest-`35=D`’d on demo `5328266`. That dest fill **is** destination P&L (demo money, still dest book). Claim 5 as dest-safety is **FAIL**.

`LiveCopyPage.tsx` L57 tells the operator the same thing.

---

## 6. Assigned UI (`LiveCopyPage.tsx` 70/70)

Display-only. Hooks: `GET /api/copy/status`, `GET /api/copy/intents` (`hooks.ts` L60–65; `Program.cs` L102–103). No FIX writer.

- L13: `REAL_COPY armed` from `realCopyArmed` (runtime bit, **not** re-pinned false).
- L24: “Live send blockers (Pepperstone cannot be filled)” — true for `1369850`; **does not** mention demo dest hopper.
- L57: “Demo dest auto-sends after a trader is ADMITTED…” — **honest residual**; contradicts any “no send path” claim.

---

## 7. Live GET

| URL | Result |
|---|---|
| `http://127.0.0.1:5000/api/health` | `web_fetch` **SSRF blocked** (private IP) |
| `http://localhost:5000/api/copy/status` | **not fetched** after health deny |
| API listen pin | `apps/api/Properties/launchSettings.json` `http://localhost:5000` |

No live JSON. Process `realCopyEnabled`, dest book, FIX logon health: **unproven**. File claims above stand. Runtime-only claims: **FAIL**.

---

## 8. Risk to capital

| Book | Risk this slot |
|---|---|
| Live Pepperstone `1369850` | **NONE** (`SAFE_BY_ABSENCE`). Session has no NewOrderSingle. CopyOpen L37–42 refuses that account / live host / live sender. Persist `AllowFixSend=false`. `VenueReconciled=const false`. `CanPromoteToLive=>false`. |
| Demo dest `5328266` | **Not absent.** Hosted 20s `ExecuteDemoCopyAsync` → `Build("D")`. Ledger open dest `237339770` @ 0.01. Hopper bypasses `RiskEngine.Evaluate` and `RealCopyEnabled`. |
| This slot | **0** sends. **0** TLS. **0** Manager attach. |

Flag armed (`true` in `.env` + DI) is **not** a live-`1369850` license. It **is** a process bit that claim 3 said was forced false. That claim is **false**.

---

## 9. What would flip this slot to PASS

All five, file-proven or live-GET-proven:

1. Either delete/unwire product `Build("D")` **or** scope the claim to `CTraderFixSession` only (and say so).
2. `CanPromoteToLive => false` — already holds.
3. After logon, a product assignment `_runtime.RealCopyEnabled = false` **or** `.env` L73 `false` **and** a live `GET /api/settings` body showing `false`.
4. Hosted `ExecuteDemoCopyAsync` gone / gated off, ledger dest closed or absent, dest mark measured — **or** the claim scoped to “not booked dest profit / not live 1369850.”
5. SHADOW no longer `AUTO_ADMIT` dest class **or** hopper requires a state that scorer cannot emit.

Until then the five-claim bundle is **FAIL**.
