# P500_BOOK_5 — Official cTrader FIX: QUOTE 5211 / TRADE 5212 / TargetCompID `cServer`. Logon is not a fill

| Field | Value |
|---|---|
| Slot | **5** |
| Agent | P500_BOOK_5 (senior quant / FIX / risk) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Topic | Official cTrader FIX identity: QUOTE TLS **5211**, TRADE TLS **5212**, `TargetCompID` **cServer**. A Logon (`35=A`) is **not** a fill. |
| Angle | Measured evidence for **higher dest profit** and **lower dest loss** on one Pepperstone / cServer venue |
| Verdict | **CONFIRMED_OFFICIAL_PORTS_AND_COMPID. LOGON_IS_NOT_A_FILL. NO_DEST_EDGE.** Official ports and issued `cServer` match the worktree. Session authentication is not ExecutionReport `150=F`. Copying the **8463**-login catalog would copy the **`RISK_BLOCKED`** left tail (**−$241,580** source). Dest real PnL is a constructor **0**. |
| Product source modified | **No.** This report is the only write. |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None** (no password, no `.env` values) |
| Local API this slot | **Not re-probed.** `GET http://127.0.0.1:5000/api/overview` and `/api/traders` are blocked from this runner (SSRF to `127.0.0.1`). Live integers below are same-day on-disk measures, not invented. |

**Honesty pin.** Wanting profit does not create an edge. A TLS Logon that returns `35=A` proves the gateway accepted session credentials. It does not put XAUUSD on the book, does not print bid/ask, and does not make the scored challenge tape destination-positive. Copying all **8463** manager logins onto one retail Pepperstone login would copy **`RISK_BLOCKED` losses**. `SAFE_BY_ABSENCE` of NewOrderSingle is why dest equity is still **$0**, not because LoggedOn is an edge.

```text
35=A  = session auth. Not a fill.
35=D  = NewOrderSingle. Not implemented on the copy hop.
35=8 / 150=F / 39=2 = fill. Never received on the copy hop.
QUOTE :5211 ≠ TRADE :5212. Price socket cannot take orders.
TargetCompID issued form = cServer. RoE table = CSERVER. Do not silent-fold.
Copy-all 8463 = copy RISK_BLOCKED (−$241,580 source) + demo grids.
LoggedOn ≠ profit. Wanting profit ≠ expectancy after dest costs.
```

---

## 0. Direct answer (higher profit / lower loss)

The Pepperstone / cTrader account does **not** become more profitable by connecting FIX, logging on, or spraying the Manager census. Official cTrader FIX 4.4 is a **two-port venue**, not an LP and not a fill printer.

| Ask | Measured answer |
|---|---|
| Official QUOTE port | **5211 SSL** / 5201 plain. Price connection only. |
| Official TRADE port | **5212 SSL** / 5202 plain. Orders live here, and only here. |
| Official / issued `TargetCompID` (tag 56) | RoE table + official examples: **`CSERVER`**. Issued Pepperstone form, architecture §25, worktree defaults: **`cServer`**. Preserve issued case. Do not silent-`ToUpper`. |
| Is Logon a fill? | **No.** Official: Logon is client authentication; fills are Execution Report after NewOrderSingle on TRADE. Product: one-shot `35=A`, then dispose. |
| Does LoggedOn create dest PnL? | **No.** `GetOverviewAsync` hard-codes `destinationRealPnl = 0`. Bid/ask on FIX cards are null. `CTraderQuoteService` is not in DI. |
| Higher dest profit | **Not by sending more.** Filter the left tail, refuse demo/contest unless later OOS-proven, size far below source, prove **shadow after dest costs** on a **standing** QUOTE tape. |
| Lower dest loss | **Do not send.** Never copy `RISK_BLOCKED`. Never copy-all **8463**. Never treat `35=A` as permission to emit `35=D`. |

If we flipped `REAL_COPY` and implemented `35=D` against this book today, expected dest PnL is **negative**. Same-day scored XAU source book is already **−$154,425**. The `RISK_BLOCKED` bucket alone is **−$241,580**. Destination real PnL is **$0** because there is no sender.

---

## 1. Official cTrader FIX identity (remeasured 2026-08-18)

Primary official pages fetched this slot:

| Page | URL |
|---|---|
| Get credentials | https://help.ctrader.com/fix/getting-credentials/ |
| Rules of Engagement | https://help.ctrader.com/fix/specification/ |
| Communication model | https://help.ctrader.com/fix/communication-model/ |
| Official C# sample (Help-linked) | https://raw.githubusercontent.com/spotware/FIX-API-Sample/master/FIX%20API%20Sample.cs |

### 1.1 Two connections, two ports — trading cannot ride QUOTE

Quoted from https://help.ctrader.com/fix/getting-credentials/ :

> “There are 2 types of connection, price connection and trade connection, and each type has its own separate set of credentials. Trading operations requests cannot be sent through the price connection's credentials and vice versa.”

Official Get-credentials screenshot (`https://help.ctrader.com/fix/img/getting-fix-api-0.png`, already extracted in `A31_ctrader_fix_overview.md` §4.3):

| UI block | Official port line | Session qualifier on the same screenshot |
|---|---|---|
| **Price Connection** | `Port: 5211 (SSL), 5201 (Plain text)` | `SenderSubID: QUOTE` |
| **Trade Connection** | `Port: 5212 (SSL), 5202 (Plain text)` | `SenderSubID: TRADE` |

Official Spotware C# sample (current GitHub, Help-linked) hard-codes the **SSL** pair and wraps both sockets in `SslStream`:

```csharp
private int _pricePort = 5211;
private int _tradePort = 5212;
// ...
private string _targetCompID = "CSERVER";
```

Source: `FIX API Sample.cs` on `spotware/FIX-API-Sample`. Sample also contains a demo password; **value not quoted here**.

RoE does **not** publish those numbers as prose in the Connectivity section. FAQ still says check **your** host/port/CompIDs. The numeric pair **5211 / 5212** is official via the credentials screenshot + current vendor sample, not a community rumor.

### 1.2 `TargetCompID` — issued `cServer` vs RoE `CSERVER`

RoE standard header (https://help.ctrader.com/fix/specification/ ), tag 56:

> “A message target. The valid value is `CSERVER`.”

Communication-model example table (https://help.ctrader.com/fix/communication-model/ ):

> “Message target is CSERVER. It is the only valid value within cTrader FIX API.”

Official successful Logon example uses `56=CSERVER` outbound and `49=CSERVER` inbound.

Issued Pepperstone / architecture §25 env sample (no secret):

```env
CTRADER_FIX_QUOTE_SSL_PORT=5211
CTRADER_FIX_TRADE_SSL_PORT=5212
CTRADER_FIX_QUOTE_TARGET_COMP_ID=cServer
CTRADER_FIX_TRADE_TARGET_COMP_ID=cServer
```

Architecture §26 item 4:

> never silently change case such as `cServer` to `CSERVER` unless the issued configuration/spec requires it

Worktree `CTraderFixOptions` (this read) matches the issued form:

```41:72:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    public sealed class QuoteFixOptions
    {
        public int SslPort { get; set; } = 5211;

        public int PlainPort { get; set; } = 5201;
        // ...
        public string TargetCompId { get; set; } = "cServer";
        // ...
    }

    public sealed class TradeFixOptions
    {
        public int SslPort { get; set; } = 5212;

        public int PlainPort { get; set; } = 5202;
        // ...
        public string TargetCompId { get; set; } = "cServer";
```

Prior slots (`B27`, `C09`, `D26`) recorded **HEAD still `CSERVER`**, worktree already `cServer`. This slot re-read the worktree: defaults are `cServer`. That is the **issued** spelling. It is **not** permission to fold RoE `CSERVER` at runtime. Hosted logon reads `CTRADER_FIX_QUOTE_TARGET_COMP_ID` and falls back to `"cServer"` — no `ToUpper`.

### 1.3 Official session vs application vs fill

Quoted from https://help.ctrader.com/fix/communication-model/ :

> “The server validates client requests using the Logon message.”

Typical session flow (same page):

1. Client starts the session with a Logon message.
2. Client exchanges **application** messages with the server.
3. Session ends with a Logout message.

Same page classifies:

| Message | Official role |
|---|---|
| Logon (`35=A`) | “client authentication message” |
| New Order Single (`35=D`) | “used to electronically submit the orders to a broker for execution” |
| Execution Report (`35=8`) | “confirmations, **fills** and unsolicited changes” |

RoE Logon (https://help.ctrader.com/fix/specification/ ):

> “A Logon message is sent from the client side application to begin a cTrader FIX session, and a response is sent by cTrader to the client side application. Once the logon is complete, quote and trade flows **can proceed** for the lifecycle of the session.”

“Can proceed” is not “have proceeded.” Official fill pair after a TRADE-session market `35=D` is Execution Report `150=0` / `39=0` (New) then `150=F` / `39=2` (Trade / Filled), with `AvgPx` / `CumQty` / `LastQty`. A lone inbound `35=A` has **none** of those tags.

RoE also: trading ops cannot be sent on the price connection. Opening `:5211` and seeing `35=A` cannot produce a dest fill even if someone later added a `35=D` builder on that socket.

---

## 2. Product mapping (measured this slot)

### 2.1 Hosted logon is a one-shot `35=A` probe on the official SSL pair

`CTraderFixLogonHostedService` hard-codes official SSL ports (does **not** read `CTRADER_FIX_*_SSL_PORT` env) and sends **only** Logon:

```48:70:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            _config["CTRADER_FIX_QUOTE_SENDER_SUB_ID"] ?? "QUOTE",
            _config["CTRADER_FIX_QUOTE_TARGET_SUB_ID"] ?? "QUOTE",
            username, password, stoppingToken);

        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            _config["CTRADER_FIX_TRADE_SENDER_SUB_ID"] ?? "TRADE",
            _config["CTRADER_FIX_TRADE_TARGET_SUB_ID"] ?? "TRADE",
            username, password, stoppingToken);
        // ...
            "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
```

`CTraderFixSession.TryLogonAsync`: `TcpClient` + `SslStream`, write `BuildLogon` (`35=A`, `98=0`, `108=30`, `141=Y`, `553` numeric account, `554` password), read **one** 4096-byte chunk, `LoggedOn=true` iff inbound tag 35 is `A`, then **`using` disposes the socket**. Result type has `LoggedOn` / `Status` / `RawLogonType`. No Bid, Ask, ClOrdID, ExecType, AvgPx, CumQty.

Grep this file: outbound MsgType is only `(35, "A")`. `NewOrderSingle` = **0**. `35=D` = **0**. Heartbeat advertised (`108=30`) is **never scheduled**.

Classification: **session probe**, not a living QUOTE book and not a TRADE initiator. Official FAQ: two simultaneous TRADE sockets duplicate reports. This code does the opposite residual — it **throws the socket away**, so dest cannot flatten even if a fill existed.

### 2.2 Copy hop cannot emit `35=D`

| Gate | Measured |
|---|---|
| `CopyTradingService.NewOrderSingleImplemented` | `const false` |
| `CopyTradingService.VenueReconciled` | `const false` |
| Persist `AllowFixSend` | **forced `false`** on every `RiskDecisionRecord` |
| `TraderStateMachine.CanPromoteToLive` | **always `false`** |
| `FromBaseline` reachable LIVE | **no** — `{INSUFFICIENT_DATA, EARLY_SCORE, WATCH, SHADOW, RISK_BLOCKED}` |
| Product `CTraderFixSession` outbound | **`35=A` only** |
| `CTraderQuoteService` DI registration | **none** (class exists, unused) |
| Overview dest PnL | constructor literal **`0`** |

```15:17:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
    public const decimal AllocationFactor = 0.05m;
```

```192:193:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    AllowFixSend = false,
                    DecidedAt = now
```

```211:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static bool CanPromoteToLive(TraderState current) => false;
```

```33:45:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
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
```

`destinationRealPnl` is the first of those three zeros. It is **not** summed from cTrader Execution Reports. Dashboard “Dest. real P&L = 0” is honest only as “we never booked a dest fill,” not as “the strategy broke even.”

Residual (not on the copy hop): `CTraderFixDemoTestTrade` can `Build("D")` on **TRADE :5212**, but refuses live host / live sender / account `1369850`. CLI `tools/DemoFixTestTrade` is not registered in API/workers. This slot did not run it.

### 2.3 `REAL_COPY` must stay false

`CTraderFixOptions.RealCopyExecutionEnabled` default is **false**. Architecture / `docs/architecture.md` / `docs/ctrader-fix.md` all say keep it false until send + recon exist.

DI now **binds** `REAL_COPY_EXECUTION_ENABLED` from env (`DependencyInjection.cs` L41). Hosted logon **no longer re-pins** false. Same-day siblings and `P500_PROFIT_SYNTHESIS` addendum: lab env may be `true` in-process while `NewOrderSingleImplemented` remains false. **Armed flag ≠ fill.** This slot did not set the flag.

Unit test already pins the fail-closed send bit when the flag is off:

```20:26:D:\Prop\tests\Unit\RiskEngineTests.cs
    public void Real_flag_false_never_allows_fix_send()
    {
        var d = _e.Evaluate(Base());
        d.Outcome.Should().Be(RiskDecisionOutcome.Approve);
        d.AllowFixSend.Should().BeFalse();
    }
```

Approve-without-send is **shadow math**, not Pepperstone risk.

---

## 3. Live book (same-day, not re-probed here)

| Source | What it measured |
|---|---|
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | Achiever **8 / 6512**, Starwave **10 / 1948** = **18 groups / 8460 traders**. `/api/traders` **8460**. FIX QUOTE+TRADE TLS logon **true** after tag 553 = integer account id. Live `35=D` **OFF**. |
| `D:\Prop\reports\swarm\20260818\P500_PROFIT_SYNTHESIS.md` | Accounts **8463** (Achiever 6512 + Starwave ~1951). XAU scored **197** (rising). `SHADOW` **70**, `WATCH` **79**, `RISK_BLOCKED` **29** (all `martingale=true`). `LIVE` **0**. SHADOW source PnL **+$78,276**. WATCH **+$8,178**. **RISK_BLOCKED −$241,580**. All scored XAU **−$154,425**. Dest real PnL **$0**. Shadow PnL **$0**. FIX bid/ask **null**. SHADOW groups **100% demo**. |
| `D:\Prop\reports\INDEX.md` | Pin: FIX LoggedOn, `35=D` off, scored XAU **net negative**, dest **$0**. Manager census **18 / 8460**. |

**+3 (8463 vs 8460) is unreconciled.** Do not greenwash either integer into the other. Neither number is “8463 copy candidates.” `GetTradersAsync` left-joins **all** `Mt5Accounts`; most rows are `INSUFFICIENT_DATA` (~8284). Starwave deals-done scored **0** on the synthesis snapshot.

This slot did not attach Manager and did not GET `:5000`. Those are the last measured pins.

Venue is **one** retail Pepperstone cTrader login on cServer (`A87` / architecture §1.6 item 6): **execution venue, not an LP**. Capacity is tiny. Official FAQ: two TRADE sockets duplicate reports. Copy-all onto that one login is dest ruin math (`P500_S055_ruin.md`), not diversification.

---

## 4. Why copy-all 8463 copies `RISK_BLOCKED` losses

### 4.1 `RISK_BLOCKED` is a scorer state, not a slogan

```194:195:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
            return TraderState.RISK_BLOCKED;
```

Martingale flag: next completed XAU after a loss with lots `> 1.25×` prior. Unit test `Martingale_after_losses_is_risk_blocked` (0.10 → 0.20 → 0.40 lots, all losers) asserts `SuggestedState == RISK_BLOCKED`.

Same-day live: **29** scored rows in that state, **all** `martingale=true`, source PnL sum **−$241,580**. That tail is **larger** than the SHADOW head (**+$78,276**). Copy-all EV of the scored XAU book is **−$154,425** **before** dest spread, commission, and 15 s `MaxSourceSignalAge`.

`CopyTradingService.GenerateShadowIntentsAsync` already selects `SHADOW | LIVE_CANDIDATE | LIVE` only. That is the **shadow** filter. A “copy every login in `/api/traders`” operator action would ignore it and include:

- **29** `RISK_BLOCKED` martingale books (−$241k source)
- **~8284** `INSUFFICIENT_DATA` (no completed-XAU edge)
- Starwave **unscored** real groups
- Demo `yo-2step` / `yo-payp` challenge accounts that exist to **pass a profit target**, then many explode (synthesis §2.5)

Wanting those 8463 rows to become dest profit does not change the sign of the blocked tail.

### 4.2 SHADOW is not dest expectancy

Quality (`BaselineScorer.Score`): `50 + 15 if XAU net>0 + 10 if PF≥1.2 + 5 if PF≥1.8 + 0.20×behavior − 0.25×risk`. SHADOW if `quality≥70 && risk<40 && trades≥3`. Dashboard `netSourcePnl` sums **all symbols**. Score uses **completed XAU only**. Synthesis examples: login **302252** SHADOW **95.50** with dashboard PnL **−68.46**. Hold time is computed and **not used**. `MaxSlippage = 1.5` exists on `RiskLimits` and is **never read** in `Evaluate`.

So even the “good” 70 SHADOW names are **source first-N heuristics on demo groups**, not dest-after-cost expectancy. They are still **one gold bet** if copied same-side same-minute onto one Pepperstone login.

### 4.3 Official FIX does not rescue a negative book

A correct `56=cServer` Logon on `:5211` and `:5212` does not:

- subscribe Market Data (`35=V` — official QUOTE application message)
- resolve tag 55 numeric XAU id (`35=x` / `35=y`)
- send NewOrderSingle (`35=D` — official TRADE application message)
- receive a fill (`35=8`, `150=F`)
- convert MT5 lots to cTrader `OrderQty` (tag 38; official max precision 0.01; product `QuantityNormalizer` is dest-grid only, `IQuantityConverter` missing)

`docs/risk.md` “Max Position Size 50 lots” is **not** the product engine (`RiskLimits.MaxPositionQuantity = 5`). Implementing the doc as a working cap would be dest death. Neither 50 nor 5 is a first-money working size. Synthesis / S055: dest working cap **0.05** lot until a live dest tape is green.

---

## 5. Higher profit / lower loss — what actually moves the number

Do these in order. Skipping to “we are LoggedOn, send” is how the dest account dies.

### Stage A — Lower loss (now, no send)

1. Keep `REAL_COPY_EXECUTION_ENABLED=false`. Do not treat DI-bound env `true` as a go-live.
2. Do **not** add a copy-hop `35=D` builder.
3. Keep `CanPromoteToLive == false`.
4. Never copy `RISK_BLOCKED` / `Martingale` / `LotEscalation` into dest size.
5. Never copy-all **8463** (or **8460**). Catalog size is observation load, not a book.
6. One TRADE owner. Official FAQ: two TRADE connections **duplicate** Execution Reports — do not treat the second `150=F` as a second fill, and do not open a second TRADE to “be sure.”

This is the only stage that is **currently true** (`SAFE_BY_ABSENCE`). Dest real PnL **$0** is the success metric of Stage A.

### Stage B — Build a destination tape (still no send)

Official application catalog required before any dest expectancy exists:

| MsgType | Session / port | Why profit math needs it |
|---|---|---|
| `35=A` then living heartbeat/`35=0` | both / **5211** + **5212** | Logon-and-dispose cannot mark to market |
| `35=x` / `35=y` SecurityList | TRADE **5212** (official examples) | Tag 55 is a **numeric** Spotware id, not `"XAUUSD"` |
| `35=V` / `35=W` / `35=X` | QUOTE **5211** | Bid/ask/age for `QUOTE_STALE` / `SPREAD_TOO_WIDE` |
| Persist `DestinationQuotes` | — | Today empty → shadow no-ops |

Until Stage B, shadow PnL stays **$0** for lack of a quote, not because expectancy is zero.

### Stage C — Shadow expectancy after dest costs

Eligibility (not live):

| Gate | Why (measured) |
|---|---|
| Not `RISK_BLOCKED` / not martingale | Left tail **−$241,580** |
| Not demo/contest unless later OOS-proven | All current SHADOW rows are `demo\yo-2step` / `demo\yo-payp` |
| Completed XAU **≥ 20**, not 3 | First-3 is luck; `earlyScore=95.5` is not dest skill |
| XAU-only PnL **> 0** after a cost haircut | All-symbol dashboard PnL lies |
| Dest qty after `allocationFactor` **≤ 0.05** lot | Login 303310 max **2.0** lots would blow the venue |
| Median hold **≥ 15 minutes** | Login 322947 ~163 s gold scalps die in spread + 15 s signal-age |
| Fresh dest quote, gold-specific spread cap | `MaxAllowedSpread = 2.0` is too loose; `MaxSlippage` unread |

Only after **30+ shadow days** with **destination** (not source) expectancy > 0.

### Stage D — Tiny live, still fail-closed

Only if Stage C is green **and** architecture §68 / §70 are actually PASS (last INDEX pin: **0/19** and **0/14**):

1. Living TRADE session on **5212** with persisted `ClOrdID` **before** send. Never retry `EXECUTION_STATE_UNKNOWN`.
2. `TargetCompID` from issued form (`cServer`); `CSERVER` only as explicit logged override if Logon rejects.
3. Working dest cap **0.05** XAU; net **0.15–0.30**; daily dest loss **$200–500** then `STOP_NEW_EXECUTION`. **Never flatten the MT5 source.**
4. Do not send `35=D` on QUOTE **5211**. Official: trading ops cannot use the price connection.

### What “higher profit” is **not**

- Not “FIX is LoggedOn.”
- Not copying more of the 8463 logins.
- Not raising lot caps toward `docs/risk.md` 50 lots or engine 5 lots.
- Not treating `earlyScore=95.5` as skill.
- Not ML (not built).
- Not wanting it.

---

## 6. What this slot did / did not do

| Action | Done? |
|---|---|
| Read official Help + Spotware sample | **Yes** |
| Read product FIX / copy / score / risk / overview | **Yes** |
| GET `127.0.0.1:5000/api/overview` or `/api/traders` | **No** — SSRF blocked; used same-day CREDENTIALS + P500 synthesis |
| Re-attach Manager / re-sum 8460 vs 8463 | **No** — delta left unreconciled |
| Print secrets / `.env` values | **No** |
| Enable `REAL_COPY` | **No** |
| Send `35=D` / NewOrderSingle | **No** |
| Edit product source | **No** |

---

## 7. Operating law (slot 5)

```text
Official cTrader FIX = QUOTE TLS 5211 + TRADE TLS 5212 + TargetCompID cServer (issued).
RoE table spelling CSERVER is not a silent ToUpper.
35=A is authentication. 35=D is the order. 35=8/150=F is the fill.
Logon-and-dispose is not a quote tape and not a trade session.
destinationRealPnl=0 is a constructor, not a strategy result.
Copy-all 8463 copies RISK_BLOCKED (−$241,580 source) onto one venue.
Wanting profit does not create an edge.
35=D stays OFF.
```

**Verdict:** official ports and issued `TargetCompID` are confirmed. Logon is not a fill. There is **no** dest edge from LoggedOn. Risk to capital **today** is **NONE** (`SAFE_BY_ABSENCE` of copy-hop NewOrderSingle). Risk if someone copies the 8463-login book after a sender exists: **dest ruin** via the `RISK_BLOCKED` tail and demo grids.

Artifact: `D:\Prop\reports\swarm\20260818\P500_BOOK_5.md`.
