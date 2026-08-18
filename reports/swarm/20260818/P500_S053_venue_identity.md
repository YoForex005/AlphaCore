# P500_S053 — Venue identity: one retail Pepperstone cTrader account

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S053_venue_identity.md` |
| Agent | P500_S053 |
| Date | 2026-08-18 |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secret handling | `CTRADER_FIX_PASSWORD` is **named only**. `.env` was **not** opened. Value **not** printed. |
| Binding law | Architecture §§1.6 item 6, 25–27, 39, 41, 65; A87; A25; A71; P500_S018 |
| Sources read | `CTraderFixOptions.cs`, `CTraderFixLogonHostedService.cs`, `CTraderFixSession.cs`, `CTraderQuoteService.cs`, `DependencyInjection.cs`, `DemoSeeder.cs`, `BrokerCatalogSeed.cs`, `apps/fix-worker/Worker.cs`, `docs/ctrader-fix.md`, architecture §25, A87, A25, `RiskLimits` |

---

## 0. Verdict (binding)

**The destination is one retail Pepperstone cTrader login (`1369850`) on the live cServer FIX gateway `live-us-eqx-01.p.c-trader.com`. It is an external execution venue. It is not an LP. It is not a hedge book.**

Capacity is **tiny**. Copying **70 gold traders** into this single account will **saturate** margin, position count, XAU net/gross, and the one TRADE session. Leaderboard diversity does not multiply venue capacity.

```text
1 retail Pepperstone cTrader account
  ≠ liquidity provider
  ≠ prime / hedge book
  ≠ 70 independent dest books

70 same-side XAU copies
  = 1 gold thesis
  × 70 fills on one login
  = saturated retail account
```

`REAL_COPY_EXECUTION_ENABLED` defaults **false**. Hosted logon still **hard-sets** `_runtime.RealCopyEnabled = false`. That is a send-off switch, not extra capacity.

---

## 1. What was (and was not) read

| Path | Read? | Why |
|---|---|---|
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | **Yes** | POCO defaults for venue identity |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | **Yes** | Runtime env fallbacks + ports |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | **Yes** | TLS 5211/5212 logon only |
| `D:\Prop\.env` | **No** | Would contain `CTRADER_FIX_PASSWORD`. Forbidden. |
| Product C# / TS | **Not edited** | Standing instruction |

Password property exists on `CTraderFixOptions` and is read from `CTRADER_FIX_PASSWORD` in the hosted service. The value is never logged in this report. If the env key is empty or contains `<SECRET>`, logon is **skipped**.

---

## 2. `CTraderFixOptions` defaults (measured)

Path: `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`

| Property | Default in source | Meaning |
|---|---|---|
| `Host` | `live-us-eqx-01.p.c-trader.com` | **Live** Pepperstone / Spotware cServer name |
| `AccountId` | `""` (empty) | POCO does **not** hardcode 1369850 here |
| `Password` | `""` (empty) | Must never be logged; not bound from this report |
| `UseSsl` | `true` | Production transport = TLS |
| `QuoteEnabled` | `true` | Session may start (not a send license) |
| `TradeSessionEnabled` | `true` | Session may start (not a send license) |
| `RealCopyExecutionEnabled` | **`false`** | NewOrderSingle off |
| `HeartbeatIntervalSec` | `30` | Logon tag 108 |
| `MaxQuoteAgeMs` | `5000` | Quote-service stale gate (unbound vs `RiskLimits.MaxQuoteAge` = 3s) |

### 2.1 QUOTE nested defaults

| Field | Default |
|---|---|
| `SslPort` | **5211** |
| `PlainPort` | 5201 (must not be production default) |
| `SenderCompId` | `live.pepperstone.1369850` |
| `TargetCompId` | `cServer` (issued-form spelling) |
| `TargetSubId` | `QUOTE` |
| `SenderSubId` | **empty** (hosted service fills `QUOTE` from env fallback) |

### 2.2 TRADE nested defaults

| Field | Default |
|---|---|
| `SslPort` | **5212** |
| `PlainPort` | 5202 (must not be production default) |
| `SenderCompId` | `live.pepperstone.1369850` |
| `TargetCompId` | `cServer` |
| `TargetSubId` | `TRADE` |
| `SenderSubId` | **empty** (hosted service fills `TRADE` from env fallback) |

**Identity encoded in the CompID:** `live` + `pepperstone` + trader login `1369850`. That is **one retail broker login**, not a venue code for a wholesale book.

The POCO is **not** bound via `IOptions<CTraderFixOptions>` in `DependencyInjection`. The hosted service reads **env keys** and applies its own fallbacks. Do not assume the empty `AccountId` on the POCO means “no account.” The live login is still the hosted-service / seed default.

---

## 3. Hosted service defaults (measured)

Path: `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`  
Registered in `D:\Prop\src\Infrastructure\DependencyInjection.cs` line 58: `AddHostedService<CTraderFixLogonHostedService>()`.

| Config key | Fallback if unset | Used as |
|---|---|---|
| `CTRADER_FIX_PASSWORD` | *(none — skip logon)* | Tag 554. **Not printed.** |
| `CTRADER_FIX_HOST` | `live-us-eqx-01.p.c-trader.com` | TLS target host |
| `CTRADER_FIX_ACCOUNT_ID` | **`1369850`** | Tag 553 username **and** log account |
| `CTRADER_FIX_QUOTE_SENDER_COMP_ID` | `live.pepperstone.1369850` | Tag 49 (both sessions; TRADE reuses this sender var) |
| `CTRADER_FIX_QUOTE_TARGET_COMP_ID` | `cServer` | Tag 56 |
| `CTRADER_FIX_QUOTE_SENDER_SUB_ID` | `QUOTE` | Tag 50 QUOTE |
| `CTRADER_FIX_QUOTE_TARGET_SUB_ID` | `QUOTE` | Tag 57 QUOTE |
| `CTRADER_FIX_TRADE_SENDER_SUB_ID` | `TRADE` | Tag 50 TRADE |
| `CTRADER_FIX_TRADE_TARGET_SUB_ID` | `TRADE` | Tag 57 TRADE |

Ports are **literals**, not env:

| Session | SSL port | Purpose |
|---|---|---|
| QUOTE | **5211** | Market data / SecurityList |
| TRADE | **5212** | Orders (when send exists) |

Comment in the hosted service (line 45–46): **tag 553 must be the integer account id, not SenderCompID.** Username = `1369850`.

After both `TryLogonAsync` calls:

- `_runtime.RealCopyEnabled = false` (hard, ignores config).
- Log line names **account**, not password: `Account {Account}`.
- Persist updates existing `FixSessionState` rows’ host/port/status only.

`apps/fix-worker/Worker.cs` does **not** call this logon path. It stamps both sessions `Disconnected` every 15s and reads a **different** key `CTrader:RealCopyExecutionEnabled` (default false). Two surfaces, same venue identity in seed.

---

## 4. Seed / catalog identity (same one account)

Both seeders write the **same** live host + CompID. They do **not** invent a second dest book.

| File | Host | Ports | SenderCompID | TargetCompID |
|---|---|---|---|---|
| `DemoSeeder.cs` | `live-us-eqx-01.p.c-trader.com` | 5211 / 5212 | `live.pepperstone.1369850` | `cServer` |
| `BrokerCatalogSeed.cs` | same | same | same | same |

There is **one** QUOTE row and **one** TRADE row. Architecture §1 item 9: do not run multiple simultaneous active TRADE sessions for the same FIX account. One login ⇒ one TRADE owner.

---

## 5. What this venue is

| Claim | Evidence | Class |
|---|---|---|
| Pepperstone **retail** cTrader login | CompID `live.pepperstone.1369850`; architecture §25 “provided Pepperstone cTrader account”; A87 “retail/prop broker FIX gateway” | **identity** |
| **External execution venue** | Architecture §1.6 item 6, §25 title; A25; A87 | **law** |
| Two TLS FIX 4.4 sessions | Hosted service 5211/5212; `UseSsl=true`; RoE QUOTE+TRADE | **protocol** |
| Live hostname in **source defaults** | `CTraderFixOptions.Host` and hosted-service fallback | **dangerous default**, still the issued venue |

Architecture §25 (quoted sense): real approved copy trades route to the provided Pepperstone cTrader account through cServer FIX 4.4. Host env sample is exactly `CTRADER_FIX_HOST=live-us-eqx-01.p.c-trader.com`. Account env sample is `CTRADER_FIX_ACCOUNT_ID=1369850`.

Official retail FIX ports (A35): no Spotware client `.pfx` required for usual QUOTE/TRADE SSL. That is **retail initiator** semantics, not a prime LP link.

---

## 6. What this venue is **not**

### 6.1 Not an LP

Architecture §1.6 item 6 (binding):

> Do not call the cTrader account an LP unless it actually is your contractual LP relationship.
> Technically this architecture treats Pepperstone/cServer FIX as the **external execution venue**.

A87: no contractual LP relationship is recorded in this repo. Table name is `execution_venues`, never `lps`. Product C# does not type the dest as `LiquidityProvider`. Do not map MetaQuotes Ultency LP APIs onto `1369850`.

Institutional LP implies wholesale credit, last-look, streamed LP books. This login is a **broker gateway** with two FIX sessions.

### 6.2 Not a hedge book

Do **not** confuse:

| Word people reuse | What it actually is here |
|---|---|
| `docs/ctrader-fix.md` “hedging execution venue” | Informal copy-destination wording. **Not** a hedge-fund book. |
| cTrader **hedged** account (tag 721 `PosMaintRptID`) | Position-netting **model** on a retail login (attach-to-position). Not a multi-strategy hedge book. |
| “Hedge the prop book on an LP” | **Forbidden assumption.** One retail login cannot absorb a prop-firm hedge book. |

The destination is **one** Pepperstone trader account. It cannot be treated as:

- a prime-of-prime hedge account,
- a matched principal book,
- an internalization desk,
- N isolated dest accounts (one per source login).

All approved copy fills, if send is ever armed, land on **the same equity, the same margin, the same XAU instrument, the same TRADE sequence**.

---

## 7. Capacity is tiny — 70 gold traders saturates

Source side (architecture) wants ~5,000 MT5 accounts and a SHADOW set that can number in the tens. Destination side is **one** retail FIX login.

### 7.1 Lab risk caps on that one book

`D:\Prop\src\Domain\Risk\RiskEngine.cs` `RiskLimits` (lab defaults, **not** a measured live Pepperstone limit sheet):

| Limit | Default | Saturation meaning on one dest login |
|---|---|---|
| `MaxXauNetExposure` | **10** dest qty | ~10 same-side XAU units and the book is full |
| `MaxXauGrossExposure` | **20** | 10 long + 10 short still fills the account |
| `MaxPositionQuantity` | **5** | One ticket cannot be “institutional size” |
| `MaxOpenPositions` | **20** | 70 open source books **cannot** each keep a dest ticket |
| `MaxLossPerTrader` | 500 | Per source login; **misses** the cluster (P500_S018) |
| `MaxDailyExecutionLoss` | 2_000 | One gold dump can print this once, not 70 times independently |
| `MaxPortfolioDrawdown` | 3_000 | Same single-account DD |

P500_S018 arithmetic (same-side burst, assuming the caller actually accumulates book qty):

| Per-login dest `q` | 70 × stack | vs net 10 | vs gross 20 |
|---|---|---|---|
| 0.10 | 7.0 | under net | under gross |
| 0.15 | 10.5 | **net binds** | under |
| 0.30 | 21.0 | net then gross | **binds** |
| 1.00 | 70.0 | binds after ~10 qty | binds after ~20 |

Without book rollup (`CurrentNetXau = 0` on every intent), **all 70 would approve** and the **account** — not the engine — would be the binder: margin, max volume, dealer reject, or a blown retail login.

`MaxOpenPositions = 20` alone means **70 concurrent gold copies cannot exist**. 50 extra source “traders” have nowhere to live.

### 7.2 Correlation: 70 logins ≠ 70 edges

Architecture §65: do not copy 50 “different traders” if they are the same XAUUSD strategy. **70 is the same failure, scaled** (P500_S018).

```text
70 gold traders × same side × same minute
    = 1 XAUUSD direction
    × 70 copies of spread + slippage + news gap
    on 1 Pepperstone equity
```

If gold dumps, they dump together. That is not diversification. That is **saturating a tiny retail book with one thesis**.

### 7.3 Session / protocol capacity

| Resource | Count on this venue | Why 70 copies hurt |
|---|---|---|
| TRADE TCP/TLS session | **1** (single-owner, §1.9 / A46) | One sequence stream; burst 35=D (if armed) is one socket |
| QUOTE session | **1** | One XAU book for all copies; stale quote rejects **everyone** |
| Discovered XAU instrument | **1** id (not hardcoded) | All 70 size against the same contract spec |
| Heartbeat | 30s | Does not add throughput |
| Retail max volume / margin | Broker-side, unknown in repo | Unmeasured ⇒ treat as **smaller** than 70 full-size gold tickets |

There is no second Pepperstone account in options, seed, or architecture §25.

### 7.4 Docs vs code (do not use the larger fiction)

`docs/risk.md` lists `Max Position Size 50 lots` and `Max Open Positions 25`. Those are **not** the C# `RiskLimits`. Even those larger numbers are still a **retail** envelope, not an LP. Prefer the **smaller** code defaults when arguing saturation.

---

## 8. Naming law (for later types)

When `ExecutionVenue` is added (A87 / A20):

| Do | Do not |
|---|---|
| `ExecutionVenue` / `execution_venues` | `LiquidityProvider`, `Lp`, `hedge_book` |
| `venue_code` e.g. `PEPPERSTONE_CSERVER` | `lp_id`, `hedge_account_id` |
| `DestinationAccount` = `1369850` (one) | N dest accounts implied by 70 source logins |
| UI: “Pepperstone cTrader (retail FIX)” | “our LP”, “the hedge book” |
| Caps: dest net/gross XAU + open-position count | “70 independent allocations” |

Keep A87’s `.env.example` comment sense: **cTrader FIX execution venue (not an LP)**.

---

## 9. Operating implication

Until (and unless) there is a **contractual** larger venue:

1. Treat `1369850` as a **single-digit-to-low-tens** dest-qty lab account.
2. **Never** fan out a 70-login gold SHADOW set as live `35=D`.
3. De-duplicate same-side same-minute XAU (P500_S018). Cap **net** first.
4. Keep `RealCopyExecutionEnabled = false`. Hosted service already forces runtime false after diagnostic logon.
5. Do not design flatten / hedge-basket logic as if this login were a prop hedge book.

**One-line:** `live-us-eqx-01.p.c-trader.com` + `live.pepperstone.1369850` is one retail Pepperstone cTrader account — not an LP, not a hedge book — and copying 70 gold traders will saturate it.

---

## 10. Files cited (absolute)

- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\docs\ctrader-fix.md`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§1.6, 25
- `D:\Prop\reports\swarm\20260818\A87_not_an_lp.md`
- `D:\Prop\reports\swarm\20260818\A25_fix_session_spec.md`
- `D:\Prop\reports\swarm\20260818\P500_S018_correlation.md`
