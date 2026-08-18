# P500_BOOK_2 — `CTraderFixSession` outbound is `35=A` only; copy-all 8463 is −EV

| Field | Value |
|---|---|
| Slot | **2** |
| Agent | P500_BOOK_2 (senior quant / trading-systems) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Assigned file | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` |
| Angle | Prove outbound MsgType is **only A**. No `35=D`. Measured evidence for higher profit and lower loss. Honesty: wanting profit does not create an edge. Copying all 8463 logins would copy `RISK_BLOCKED` losses. |
| Verdict | **CONFIRMED_35A_ONLY / COPY_ALL_IS_LOSS.** Assigned file outbound tag 35 is **`(35, "A")` only**. `35=D` count = **0**. `NewOrderSingle` count = **0**. One `WriteAsync`, then sockets dispose. Copy-all of the 8463-login catalog is **not** an edge: scored XAU book already **−$154,425**; `RISK_BLOCKED` tail **−$241,580**. Dest real PnL is constructor **0**. |
| Product source modified | **No.** This report is the only write. |
| Live `35=D` sent | **No.** This slot did not open TLS, did not Logon, did not build or send NewOrderSingle, did not flip `REAL_COPY`. |
| Secrets printed | **None** (flag name + boolean only). |

**Honesty rule:** wanting higher profit and lower loss does not create an edge. A TLS Logon (`35=A`) is not a fill. Copying every Achiever+Starwave login onto one Pepperstone account copies the martingale left tail. `SAFE_BY_ABSENCE` is why dest capital is still $0 — not because the book is profitable.

---

## 0. Direct answer

| Ask | Measured answer |
|---|---|
| Outbound MsgType on `CTraderFixSession` | **Only `A`.** `BuildLogon` hardcodes `(35, "A")`. The compilation unit has no other MsgType. |
| Is there a live `35=D` / NewOrderSingle on this session? | **No.** File grep: `35=D` = **0**, `NewOrderSingle` = **0**, `OrderQty` / `38=` = **0**. One `ssl.WriteAsync` of the Logon bytes. |
| Does wanting profit create dest edge? | **No.** Desire is not expectancy. The scored XAU book is **net negative**. |
| Copy all 8463 logins? | **That is how you lose.** 29 `RISK_BLOCKED` rows (all `martingale=true`) sum **−$241,580**. Copy-all EV of the scored XAU book is **−$154,425**. Dest is one retail Pepperstone login. |
| Risk to Pepperstone capital **today** | **NONE** (`SAFE_BY_ABSENCE`). There is no copy-hop sender. |

**If we sprayed `35=D` for every login now, expected destination PnL is negative.** Destination real PnL is **$0** because `GetOverviewAsync` hardcodes the third PnL slot to `0` and because no ticket exists.

---

## 1. Method (this slot)

- Full `read_file` of `CTraderFixSession.cs` (**135 / 135** physical lines).
- `grep` of that file: `(35,` / `35=D` / `NewOrderSingle` / `WriteAsync`.
- Adjacent `read_file` / `grep` only: `CTraderFixLogonHostedService.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderQuoteService.cs`, `CTraderFixOptions.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `RiskEngine.cs`, `BaselineScorer.cs` / `TraderStateMachine`, `LiveRuntimeStatus.cs`, `EfDashboardQueries.GetOverviewAsync`, `DependencyInjection.cs`, `apps/fix-worker/Worker.cs`, `FakeMt5BrokerConnector` login **10002**, `BaselineScorerTests` + `SeedingAndStoreTests`, `tools/DemoFixTestTrade/Program.cs`.
- On-disk census: `LIVE_GROUPS_AND_TRADERS.json` (probe `2026-08-18T08:42:16Z`), `CREDENTIALS_AND_COPY_STATUS.md`, `P500_PROFIT_SYNTHESIS.md` live pin.
- `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **not re-probed this slot** (localhost fetch blocked). Numbers below are cited with their source; **8463 vs 8460 is unreconciled**.
- **No** product edit. **No** `.env` write. **No** password / tag-554 value printed.

---

## 2. Proof — outbound MsgType is only `A`

Assigned file is two types: `CTraderFixSessionResult` + static `CTraderFixSession`. There is no order builder, no heartbeat loop, no quote subscribe.

### 2.1 The only wire write is Logon

```33:50:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
        try
        {
            using var tcp = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(20));
            await tcp.ConnectAsync(host, sslPort, timeoutCts.Token);
            await using var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = host,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            }, timeoutCts.Token);

            var seq = 1;
            var logon = BuildLogon(senderCompId, targetCompId, senderSubId, targetSubId, username, password, seq);
            var bytes = Encoding.ASCII.GetBytes(logon);
            await ssl.WriteAsync(bytes, timeoutCts.Token);
            await ssl.FlushAsync(timeoutCts.Token);
```

Measured on this file:

| Check | Count / fact |
|---|---|
| Physical lines | **135** |
| `ssl.WriteAsync` | **1** (line 49) |
| `BuildLogon` callers | **1** (`TryLogonAsync`) |
| Seq used | **1** (never incremented) |
| Socket lifetime | `using TcpClient` + `await using SslStream` — **disposed on return** |
| Timeout | `CancelAfter(20s)` on connect + TLS + one write + one 4096-byte read |
| Inbound accept | `Extract(reply, "35") == "A"` → `LoggedOn=true` |
| Heartbeat loop | **None.** Tag `108=30` is advertised, never scheduled. |
| Market data / SecurityList | **None** in this file. |

### 2.2 Body tags start with `(35, "A")` — no `D`

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

Outbound tag census of `BuildLogon`:

| Tag | Value | Meaning |
|---|---|---|
| 35 | **`A`** | Logon — **only MsgType this class can emit** |
| 34 | `1` | MsgSeqNum |
| 49 / 56 | sender / target | CompIDs |
| 50 / 57 | QUOTE or TRADE subs | Set by host, not by a trade builder |
| 52 | UTC sending time | Clock only |
| 98 | `0` | EncryptMethod none |
| 108 | `30` | HeartBtInt advertised, **not looped** |
| 141 | `Y` | ResetSeqNumFlag |
| 553 / 554 | username / password | Present as parameters; **values not quoted here** |

Absent from this compilation unit (grep = **0**): `35=D`, `(35, "D")`, `NewOrderSingle`, `OrderQty`, `38=`, `ClOrdID`, `11=`, `54=`, `40=`, `35=F`, `35=G`, `35=8`, `35=V`, `35=x`, `35=0` (heartbeat send).

`Assemble` prefixes `8=FIX.4.4` + `9=` body length + `10=` checksum. It does **not** choose MsgType. MsgType is whatever `BuildLogon` put in the list — always `A`.

### 2.3 Result type cannot carry a fill

```10:17:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
public sealed class CTraderFixSessionResult
{
    public required FixSessionQualifier Qualifier { get; init; }
    public required bool LoggedOn { get; init; }
    public required string Status { get; init; }
    public string? LastError { get; init; }
    public string? RawLogonType { get; init; }
}
```

No Bid, Ask, Spread, `ClOrdID`, `ExecType`, `OrdStatus`, `LastPx`, or dest PnL. `LoggedOn` answers “did inbound 35 equal A?”, not “did we take risk?”.

### 2.4 Product hop that calls this file also does not send `D`

`CTraderFixLogonHostedService` calls `TryLogonAsync` twice (QUOTE **5211**, TRADE **5212**), stamps `LiveRuntimeStatus`, persists `FixSessionState` **status only**, and logs `NewOrderSingle still unimplemented`. It never writes a second FIX message. Persist maps `LoggedOn` → `FixSessionStatus.LoggedOn` else `Error` (catch-path `Disconnected` is collapsed).

`apps/fix-worker/Worker.cs` stamps both sessions `Disconnected` every 15 s with `NewOrderSingle remains off`.

`CTraderQuoteService` can *build* tag lists `35=y` / `35=V` in memory. Grep of `src/` for `CTraderQuoteService` callers = **definition only** (not called from the session, the logon host, copy, or API). After `TryLogonAsync` returns there is **no socket** to send them on anyway.

---

## 3. Residual that is **not** this file (do not collapse)

| Residual | Bound | On copy hop? | Live identity? |
|---|---|---|---|
| `CTraderFixDemoTestTrade.Build("D")` × **3** (flatten existing / open 1 / close 1) | `tools/DemoFixTestTrade` CLI only. Not in DI, API, workers, or `CopyTradingService`. | **No** | **Refused.** Gate rejects `live-*` host, `live.*` sender, and account `1369850`. |
| `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` | `DependencyInjection.cs` L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`. Logon host **no longer re-pins false**. | Flag only. **No sender.** | N/A |
| `CTraderFixOptions.RealCopyExecutionEnabled` default | **`false`** (POCO L35) | Unread by `CTraderFixSession` | N/A |
| Product `src/` literal `35=D` | **0** outside the demo helper | N/A | N/A |

W500 reports that say “product `35=D=0` / single FIX writer” are **stale** if they ignore the demo helper. They are **correct** if they mean the **copy hop**. This slot did **not** invoke the demo tool.

---

## 4. Measured book — copy-all is −EV

### 4.1 Census (do not greenwash 8463 vs 8460)

| Source | When | Groups | Accounts | Notes |
|---|---|---:|---:|---|
| `LIVE_GROUPS_AND_TRADERS.json` | 2026-08-18T08:42:16Z | 8 + 10 = **18** | 6512 + 1948 = **8460** | Manager probe. Passwords not in file. |
| `CREDENTIALS_AND_COPY_STATUS.md` | same day | **18** | **8460** | `/api/traders` then returned 8460 |
| `P500_PROFIT_SYNTHESIS.md` live pin | same day, mid-scoring | — | **8463** (Achiever 6512 + Starwave ~1951) | In-memory API during Achiever scoring |
| This slot | 2026-08-18 | — | **not re-probed** | localhost GET blocked |

**+3 is unreconciled.** User brief says 8463. Disk Manager census is **8460**. Do not pretend they are the same measurement.

Achiever group mix (probe, sums to 6512):

| Group | Accounts |
|---|---:|
| `contest\yo-1step` | 2 |
| `contest\yo-2step` | 179 |
| `contest\yo-instant` | 4 |
| `contest\yo-payp` | 5 |
| `demo\yo-1step` | 4 |
| **`demo\yo-2step`** | **6295** |
| `demo\yo-instant` | 0 |
| `demo\yo-payp` | 23 |

**96.7%** of Achiever logins sit in `demo\yo-2step`. That is a **challenge / pass-target** book, not a live edge sample. Copying “all 8463” is mostly copying demo contestants.

### 4.2 Same-day scored XAU tape (P500 pin — not re-summed here)

| Bucket | Count | Source PnL |
|---|---:|---:|
| Accounts in catalog | 8463 pin / 8460 probe | — |
| XAU traders with a score | 197 (rising; Achiever only) | — |
| ≥3 completed XAU | 178 | — |
| `SHADOW` | 70 | **+$78,276** |
| `WATCH` | 79 | **+$8,178** |
| **`RISK_BLOCKED`** | **29** (all `martingale=true`) | **−$241,580** |
| `LIVE` / `LIVE_CANDIDATE` | **0 / 0** | — |
| `INSUFFICIENT_DATA` | ~8284 | not a copy set |
| Starwave scored | **0** (phase `deals-done`) | unknown |
| **All scored XAU** | | **−$154,425** |
| Destination real PnL | | **$0** (literal) |
| Shadow PnL | | **$0** (no quote tape) |
| SHADOW groups | 100% `demo\yo-2step` + `demo\yo-payp` | adverse selection |

Arithmetic check (pin): `+78,276 + 8,178 − 241,580 = −155,126` before the remaining scored-but-not-SHADOW/WATCH/BLOCKED remainder that brings the published all-scored total to **−$154,425**. The blocked tail is **larger than the SHADOW head**. Copy-all cannot be +EV when the left tail dominates the right.

### 4.3 Dashboard dest PnL is not measured dest money

```33:52:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        return new OverviewDto(
            accounts,
            brokers,
            xauTraders,
            three,
            scores.Count(s => s.CurrentState == TraderState.WATCH),
            scores.Count(s => s.CurrentState == TraderState.SHADOW),
            scores.Count(s => s.CurrentState == TraderState.LIVE_CANDIDATE),
            scores.Count(s => s.CurrentState == TraderState.LIVE),
            scores.Count(s => s.CurrentState == TraderState.RISK_BLOCKED),
            shadowPnl,
            0,
            0,
            0,
            ...
            _runtime.RealCopyEnabled);
```

`destinationRealPnl`, `xauGross`, `xauNet` are constructor literals **0**. `GetRiskAsync` also returns `DailyPnl/Drawdown/Xau* = 0`. `GetFixSessionsAsync` hardcodes `ExecutionEnabled: false`. Wanting those KPIs to be green does not fill a ticket.

---

## 5. Why copying all 8463 copies `RISK_BLOCKED` losses

### 5.1 How a login becomes `RISK_BLOCKED`

```189:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState FromBaseline(bool earlyEligible, decimal quality, decimal risk, FeatureSnapshot features)
    {
        if (features.CompletedXauTrades == 0)
            return TraderState.INSUFFICIENT_DATA;

        if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
            return TraderState.RISK_BLOCKED;
        ...
    }

    public static bool CanPromoteToLive(TraderState current) => false;
```

Reachable automatic set: `{INSUFFICIENT_DATA, EARLY_SCORE, WATCH, SHADOW, RISK_BLOCKED}`. **No `LIVE`.** Trade #3 cannot auto-LIVE. `CanPromoteToLive` is hard-`false`.

Martingale flag (same file): after a **losing** trade, next lot `> 1.25×` prior lot. Demo fixture 10002 is 0.10 → 0.20 → 0.40 after −200 / −500 / −1400.

### 5.2 Measured fixture: login 10002 is the copy-all warning in one account

`FakeMt5BrokerConnector` Achiever 10002 (`demo\yo-2step`):

| Ticket | Lots | Gross | Commission | Net |
|---|---:|---:|---:|---:|
| 601 | 0.10 | −200 | −1 | −201 |
| 602 | 0.20 | −500 | −2 | −502 |
| 603 | 0.40 | −1400 | −4 | −1404 |
| **Sum** | | **−2100** | **−7** | **−2107** |

`SeedingAndStoreTests`: `Login == 10002` → `CurrentState == RISK_BLOCKED`.  
`BaselineScorerTests.Martingale_after_losses_is_risk_blocked`: same shape → `RISK_BLOCKED`.

Copying 10002 onto Pepperstone copies **−$2,107 of already-realized martingale**, not “a trader who wants profit.” Scale that pattern across the live pin’s 29 blocked rows and you get the **−$241,580** tail.

### 5.3 Product shadow path **excludes** `RISK_BLOCKED` — copy-all would not

```94:96:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
        var copyable = new[] { TraderState.SHADOW, TraderState.LIVE_CANDIDATE, TraderState.LIVE };
        var scores = await _db.TraderScores.Where(s => copyable.Contains(s.CurrentState)).ToListAsync(ct);
```

`RISK_BLOCKED` is **not** in `copyable`. `WATCH` is not either. `GenerateShadowIntentsAsync` will not emit intents for the −$241k tail **unless an operator (or a future “copy everyone”) bypasses this filter**.

That is the honesty point: **copy-all 8463 is a different policy from the product filter.** The catalog walk (`GroupRequestArray("*")` + all users) is **read-only fetch**. Promoting that catalog into dest tickets would:

1. Include ~8284 `INSUFFICIENT_DATA` (0 completed XAU — noise / non-gold / one-ticket luck).
2. Include 29 `RISK_BLOCKED` martingale accounts (**−$241,580**).
3. Include WATCH names the scorer already refused to SHADOW.
4. Include Starwave **unscored** (1948 probe / ~1951 pin) with **unknown** expectancy.
5. Land every fill on **one** Pepperstone login (`MaxXauNetExposure = 10` lots = 1,000 oz; `MaxPositionQuantity = 5` lots; `MaxMarginUsage = 0.70`; `MaxDailyExecutionLoss = $2,000`). Those are **blow-up caps**, not a working book.

### 5.4 `RiskEngine` does **not** read `TraderState.RISK_BLOCKED`

Grep of `RiskEngine.cs` for `RISK_BLOCKED` / `TraderState` = **0**. `Evaluate` will `MARTINGALE_BLOCK` / `ABNORMAL_SIZING_BLOCK` only if the request flags are set. `CopyTradingService` does pass `score.Martingale` / `score.LotEscalation`, and it **forces**:

```192:192:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    AllowFixSend = false,
```

even when `Evaluate` would have set `AllowFixSend`. Live send still requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled`. Last two are **`const false`**. Persist path is `SHADOW_ONLY`.

So: **today** dest cannot take the blocked tail. **Copy-all as a strategy** would, if someone wired the catalog to a sender and skipped the state filter. The engine will not save you by state name.

`RiskLimits` that would *not* stop a copy-all spray in time: `MaxAllowedSpread = 2.0` XAU, `MaxSourceSignalAge = 15 s`, `MaxSlippage = 1.5` (**field exists, never read**), `MaxPositionQuantity = 5`, `MaxXauNet = 10`, `MaxMarginUsage = 0.70`, `MaxDailyExecutionLoss = 2_000`. `docs/risk.md` “50 lots / 100 tickets” is **not** the product engine and must not be implemented.

---

## 6. Wanting profit ≠ edge (what actually raises dest PnL / cuts dest loss)

Architecture target is **future destination-net PnL inside risk limits**, not “who made the most in the first three challenge trades” and not “FIX is LoggedOn.”

### Higher dest profit (only if a sender ever exists)

1. **Do not copy** `RISK_BLOCKED` / martingale / lot-escalation / same-second grids.
2. **Do not copy** `demo\` / `contest\` until a later OOS proof. 70 current SHADOW rows are 100% demo.
3. Require **XAU-only** net > 0 after a dest cost haircut. Dashboard `netSourcePnl` sums **all symbols**; quality uses **completed XAU only** (302252 can be SHADOW 95.50 with dashboard **−68.46**).
4. Raise the sample: completed XAU **≥ 20**, not trade #3. `EarlyScoreTradeCount = 3` is luck.
5. Size at `AllocationFactor = 0.05` **and** a working dest cap **0.05 lot**, not the 5.0 blow-up cap. 303310 SHADOW +41,634 at **2.0** source lots would saturate one retail login.
6. Drop holds that die in spread + 15 s stale + FIX probe latency. 322947 avg hold **~163 s**. This session’s 20 s cold TLS+Logon is already **11%** of a 180 s gold scalp **before any dest order exists**.
7. Keep a **standing** QUOTE (`35=0` heartbeat, `35=x`/`35=V`, persist bid/ask). Today there is no tape; shadow PnL is $0.

### Lower dest loss (now, and later)

1. **Do not send.** Keep `NewOrderSingleImplemented = false`. Do not add a `35=D` builder to `CTraderFixSession`.
2. Keep `CanPromoteToLive => false`. Keep persist `AllowFixSend = false`.
3. Treat lab `.env` `REAL_COPY_EXECUTION_ENABLED=true` as an **armed flag without a gun**. Do not treat dashboard “armed” as profit mode. LiveRuntimeStatus copy note already says *“NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled.”*
4. Never flatten the **MT5 source** to “save” dest. Source tickets are not ours.
5. If a sender is ever built: persist `ClOrdID` **before** send; never retry `EXECUTION_STATE_UNKNOWN`; dest daily loss latch **$200–500** then `STOP_NEW_EXECUTION` (not $2,000).

### What “higher profit” is **not**

- Not connecting FIX (already a one-shot `35=A`).
- Not copying more of the 8463.
- Not raising lot caps.
- Not treating `earlyScore = 95.50` as skill.
- Not ML (Phase 6; not built).
- Not flipping `REAL_COPY`.

---

## 7. Copy-hop fail-closed (why dest PnL is still $0)

| Gate | Value | File |
|---|---|---|
| Outbound MsgType on live session | **`A` only** | `CTraderFixSession.BuildLogon` L96 |
| Socket after probe | **Disposed** | `using` L35 / L39 |
| `NewOrderSingleImplemented` | **`const false`** | `CopyTradingService` L16 |
| `VenueReconciled` | **`const false`** | L15 |
| Persist `AllowFixSend` | **hard `false`** | L192 |
| `CanPromoteToLive` | **`false`** | `TraderStateMachine` L211 |
| `LIVE` traders | **0** (unreachable from `FromBaseline`) | scorer + synthesis pin |
| Overview `destinationRealPnl` | **literal 0** | `EfDashboardQueries` L44 |
| Hosted copy tick | SHADOW intents every 20 s; log *“Live NewOrderSingle still blocked”* | `CopyTradingHostedService` L30 |
| FIX worker | stamps TRADE `NewOrderSingle remains off` | `apps/fix-worker/Worker.cs` L41 |
| `GetFixSessionsAsync` execution flag | **`false`** | `EfDashboardQueries` L196 |

`SAFE_BY_ABSENCE` is the only reason copy-all has not already hit Pepperstone. It is **not** proof the economics work.

---

## 8. Verdict

**Slot 2 CONFIRMED:** `CTraderFixSession.cs` (read 135/135) outbound MsgType is **only `A`**. **No `35=D`.** One write, 20 s timeout, sockets disposed. Official cTrader FIX 4.4 defines NewOrderSingle on TRADE; **this class does not implement it.**

**Slot 2 HONESTY:** wanting profit does not create an edge. Copying all **8463** (pin) / **8460** (probe) logins would copy the **`RISK_BLOCKED` −$241,580** martingale tail and a scored XAU book already **−$154,425**. SHADOW +$78k is **demo**. Dest PnL **$0**. Starwave **unscored**.

**Risk to capital today: NONE** (`SAFE_BY_ABSENCE`). Residual: `.env` L73 **true** and DI binds it; demo CLI `Build("D")` exists off-hop and is live-identity gated. This slot did not send and must not be read as permission to send.

```text
ALLOW:  keep 35=A probe; keep NOS unimplemented; filter RISK_BLOCKED;
        dest working size 0.05 lot after a real quote tape.
FORBID: 35=D on this session; copy-all 8463; treat LoggedOn as edge;
        flatten MT5 source.
```
