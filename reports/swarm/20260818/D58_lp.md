# D58 — Product-code grep for `LP`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D58_lp.md` |
| Agent | D58 (product `LP` recensus) |
| Date | 2026-08-18 13:40:11 +05:30 |
| Assigned | Grep product code for `LP`. Write this file. **Do not modify product source.** |
| Product source edited | **No** |
| Test source edited | **No** |
| Measure HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`, 2026-08-18 13:24:21 +0530) |
| Binding | Architecture v2 §1.6 item 6, §25, §44–45; A87 naming law |
| Prior (not copied as verdict) | `A87_not_an_lp.md`, A25, A20, A18, A75, C30 |
| Method | `rg` over product trees with `bin` / `obj` / `node_modules` / `vendor` excluded. Token forms: `\bLP\b` (cs/ci), PCRE identifier `(?<![A-Za-z0-9_])[Ll][Pp](?![A-Za-z0-9_])`, `LiquidityProvider`, `liquidity provider`, `IsLp`, `is_lp`, `LpAccount`, `lp_account`, `LpVenue`, `LpId`, `OurLp`, `lp_id`, `lp_code`, `lp_login`, `LastPrice`, `LimitPrice`, `LongPosition`, plus `ExecutionVenue` / `execution_venues`. Hashed key files. Compared worktree vs HEAD `.env.example`. Nothing answered from memory. |

**Assigned answer:** Product C# / TS / JSON under `src`, `apps` (minus `node_modules`), and `tests` contains **zero** identifiers, table names, flags, or comments that call the Pepperstone/cTrader destination an `LP` or `LiquidityProvider`. The only product-adjacent **prohibition** hits are architecture §1.6, `docs/ctrader-fix.md` line 5, HEAD `.env.example` line 47 (file **deleted** from the worktree), and the operator `.env` comment. Vendor Ultency `LiquidityProvider` APIs stay under `mt5-sdk\vendor` and are **not** referenced by owned wrapper code. Product source was not modified.

---

## 0. Verdict

**PASS on product naming (forbidden `LP` identifier is absent).**  
**OPEN on the venue model (`ExecutionVenue` / `execution_venues` still do not exist).**  
**REGRESSION vs A87: repo-root `.env.example` is deleted from the worktree** (`git status` = ` D .env.example`). The committed HEAD blob still carries `# cTrader FIX execution venue (not an LP)`.

| Check | Class | One-line |
|---|---|---|
| Product `*.cs` word `LP` / identifier `Lp` | **ZERO** | 66 + 6 + 12 C# files (`src` / `apps` / `tests`) |
| Product `*.ts` / `*.tsx` word `LP` | **ZERO** | 28 files under `apps/web/src` |
| Product `appsettings*.json` `LP` | **ZERO** | `CTraderFix` block uses host / CompId, not `lp` |
| Owned `mt5-sdk/src` `LiquidityProvider` / `\bLP\b` | **ZERO** | 25 `.cpp`/`.h`; 4× `LPCWSTR` (Win32, not LP) |
| Docs product prose “not a liquidity provider” | **1** | `docs/ctrader-fix.md:5` — **prohibition**, correct |
| Architecture §1.6 item 6 | **1 block** | Prohibition, not a type name |
| `class ExecutionVenue` / `ToTable("execution_venues")` | **ABSENT** | 20 EF tables mapped; none is `lps` or `execution_venues` |
| Worktree `.env.example` | **MISSING** | Tracked in HEAD, deleted on disk |
| Vendor Ultency `LiquidityProvider` | **13** in 3 headers | Not product; do not import |
| Product source changed by D58 | **NO** | Report only |

Do **not** claim “no LP anywhere in the repo.” Vendor SDK + reports + architecture prohibition exist.

Do **not** claim `ExecutionVenue` is implemented. Absence of the forbidden word is not a venue table.

Do **not** treat `DealReason.Gateway = 9` or `CTraderFixOptions` “FIX gateway” comments as LP types.

---

## 1. Binding law (quoted)

Architecture `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` SHA-256 `0B3C0EDC09081C25D097FF0E6AADC7A638562EBB8DB345DC325DC54EC904D37E` (50 966 bytes).

§1.6 item 6 (lines 88–89):

> **Do not call the cTrader account an LP unless it actually is your contractual LP relationship.**
> Technically this architecture treats Pepperstone/cServer FIX as the **external execution venue**. The software must not assume institutional LP semantics that the account does not provide.

§44 / §45 required destination table name (lines 1650, 1715): `execution_venues`.  
Not `lps`, `liquidity_providers`, or `lp_accounts`.

This grep asks: does **product code** violate that law? **No.**

---

## 2. Trees searched (product)

| Tree | Files searched (this run) | `\bLP\b` | `LiquidityProvider` / `lp_*` identifiers |
|---|---|---|---|
| `D:\Prop\src` (`*.cs`, exclude `bin`/`obj`) | **66** | 0 | 0 |
| `D:\Prop\apps` (`*.cs`, exclude `bin`/`obj`) | **6** | 0 | 0 |
| `D:\Prop\tests` (`*.cs`, exclude `bin`/`obj`) | **12** | 0 | 0 |
| `D:\Prop\apps\web\src` (`*.ts`/`*.tsx`) | **28** | 0 | 0 |
| `D:\Prop\apps` `appsettings*.json` | 3 hosts | 0 | 0 |
| `D:\Prop\docs` | **9** files | 0 word `LP` | 1 phrase “liquidity provider” (forbid) |
| `D:\Prop\mt5-sdk\src` (owned C++/H) | **25** | 0 | 0 |
| `D:\Prop\services` | empty directory | — | — |
| `D:\Prop\README.md` | 1 | 0 | 0 |
| `D:\Prop\docker-compose.yml` | 1 | 0 | 0 |
| `D:\Prop\Directory.Build.props` | 1 | 0 | 0 |
| `D:\Prop\mt5-sdk\.env.example` | 1 | 0 | 0 |

Substring `lp` (case-insensitive) in `src` `*.cs` is **8 lines / 5 files**, all false positives: `DestinationRealPnl`, `MlProbability`, `FillPrice`, `SslPort` (×2 classes + launchSettings), `UnknownExternalPosition`, `MissingInternalPosition`. None is the token `LP`.

`apps/web/package-lock.json` integrity hashes and `@babel/helper-*` contain the letters `lp`. Ignored (not product source).

---

## 3. Exact product-tree commands (measured empty)

```text
rg --glob '*.cs' '\bLP\b'                          src apps tests     → 0
rg -i --glob '*.{cs,ts,tsx}' '\bLP\b'              src apps/web/src   → 0
rg --pcre2 '(?<![A-Za-z0-9_])[Ll][Pp](?![A-Za-z0-9_])|LiquidityProvider|lp_account|LpAccount|IsLp'
                                                   src apps tests     → 0
rg -i 'LiquidityProvider|liquidity.?provider|IsLp|is_lp|LpAccount|lp_account|LpVenue|LpId|OurLp|lp_id|lp_code|lp_login'
                                                   src apps tests mt5-sdk/src  → 0 in code
rg -i 'LastPrice|LimitPrice|LongPosition'          src apps tests     → 0
rg 'class ExecutionVenue|record ExecutionVenue|interface IExecutionVenue|ToTable("execution_venues")|ToTable("lps")'
                                                   src apps tests     → 0
```

`rg` exit code 2 (no matches) on product identifier queries. That is the measured PASS.

---

## 4. What product source actually says (venue / destination, not LP)

`Venue*` / `venue` in `src` `*.cs`: **26 matching lines / 8 files / 66 searched**. Vocabulary is already “venue” / “destination”.

| File | SHA-256 | Token | Role |
|---|---|---|---|
| `src\Domain\Entities\DestinationQuote.cs` | `E5CFED157370766E6421FCA3C6ADB8127F83B4D9E1BDB38E3621F7BD317EC726` | `VenueInstrumentId`, `VenueTimestamp` | Quote snapshot; no venue FK |
| `src\Domain\Entities\FixSessionState.cs` | (comment only) | “destination venue” | XML summary; fields are host/seq |
| `src\Domain\Enums\RiskDecisionOutcome.cs` | `A0753C0FAA97261E1E26717AB3E6465F30C9F2D9024A3FF3675B1377C7D26951` | `PauseVenue = 4` | Correct verb |
| `src\Domain\Risk\RiskEngine.cs` | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` | `VenueHealthy`, `VENUE_UNHEALTHY`, `VENUE_NOT_RECONCILED` | Gate, not LP health |
| `src\Domain\Instruments\SymbolNormalizer.cs` | `808CBA1F9C9F1FFF1647C0FDC9BD896BA1ECEBB463D22F971D0B4DDF6E687458` | `TryMapVenueInstrumentId`, `RegisterVenueInstrument` | Venue instrument id → canonical |
| `src\Domain\Execution\ExecutionOrderStateMachine.cs` | `CDF7B67EB0D032513C2EBF73BC5B3F208F665D6A2A18327E39975198DCF12219` | `VenueOrderId` | Destination broker order id |
| `src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` | “FIX gateway host (cTrader)” | Named cTrader FIX gateway, **not** LP |
| `src\Infrastructure\Persistence\TraderDbContext.cs` | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | `destination_quotes`, `fix_sessions`, `execution_intents` | No `lps`, no `execution_venues` |
| `src\Domain\Brokers\BrokerCodes.cs` | — | `ACHIEVER`, `STARWAVEFX` | Source MT5 only |
| `src\Domain\Enums\DealReason.cs` | — | `Gateway = 9` | `IMTDeal::EnDealReason`, not an LP type |
| `apps\web\src\types\index.ts` | — | `FixSession`, `realPnl` | No LP label |
| `apps\web\src\pages\ReconciliationPage.tsx:8` | — | “Unresolved **venue** differences…” | Correct UI copy |
| `apps\web\src\pages\OverviewPage.tsx:26` | — | “Dest. real P&L” | Destination, not LP |

`CopyIntent.BrokerId` is the **source** broker FK. `ExecutionIntent` has `DestinationSymbol` / `FixOrderId` and **no** `VenueId` / `DestinationAccount` / `Lp*`. That is a missing venue model, not an LP mislabel.

### 4.1 EF `ToTable` inventory (20)

`brokers`, `mt5_groups`, `mt5_accounts`, `mt5_deals`, `mt5_positions_current`, `reconstructed_trades`, `canonical_instruments`, `source_symbol_mappings`, `trader_scores`, `trader_score_history`, `outbox_events`, `sync_checkpoints`, `copy_intents`, `risk_decisions`, `execution_intents`, `shadow_orders`, `destination_quotes`, `fix_sessions`, `audit_logs`, `kill_switches`.

Architecture-required `execution_venues` is **not** among them. Forbidden `lps` / `liquidity_providers` are also **not** among them.

### 4.2 Quoted product comments (correct)

```4:6:D:\Prop\src\Domain\Entities\FixSessionState.cs
/// <summary>
/// Runtime state of a FIX session (quote or trade) to the destination venue.
/// </summary>
```

```7:9:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    /// <summary>
    /// FIX gateway host (cTrader).
    /// </summary>
```

```87:88:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (!request.VenueHealthy && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.PauseVenue, "VENUE_UNHEALTHY");
```

---

## 5. Allowed prohibition hits (not product identifiers)

These mention “LP” / “liquidity provider” **to forbid** the name. They are not types.

| Location | Text | Status |
|---|---|---|
| Architecture v2:88–89 | “Do not call the cTrader account an LP…” | Binding law |
| `D:\Prop\docs\ctrader-fix.md:5` SHA `52E80263C4D1672121842F17A382FFC691CB9350A1B26BF53EE8252C5ABD0C77` | “cTrader is used as a **hedging execution venue** — not a liquidity provider.” | Correct |
| HEAD `.env.example` blob `b71480a8d9f0cd30166c25e1d124ab744a08fa2f` line 47 | `# cTrader FIX execution venue (not an LP)` | Correct; **file absent from worktree** |
| Worktree `D:\Prop\.env` line 47 (operator, gitignored) | same comment | Correct; not product source. Values not quoted here |

`docs/architecture.md` (SHA `A5FB4FEFD9EFECDDCECDD884D1F1FA2042658AB06989F2155BF35B67BBFE5B3D`) and `README.md` (SHA `C023B4227A1C511F346A1FD96B6648B901001551731D1472FE7A33468B1A2764`) do **not** contain “not an LP”. README still points at `.env.example` (line 30) while that file is deleted on disk. That is a docs/tree gap (C30), not an LP naming violation.

---

## 6. Hits that are **not** the cTrader account

### 6.1 Vendor MetaTrader 5 Manager SDK (Ultency)

`D:\Prop\mt5-sdk\vendor\MetaTrader5SDK` — **740** `*.{h,hpp,cpp,cs}` searched. Product-relevant LP tokens:

| File | Matches | Kind |
|---|---|---|
| `Include\Config\MT5APIConfigUltLiquidity.h` | 4× `LiquidityProvider` | Ultency LP config |
| `Include\Bases\MT5APIUltLiquidityOrder.h` | 5× (`EnLiquidityProviders` + get/set) | Ultency LP order |
| `Include\Bases\MT5APIUltDeal.h` | 4× + comment “processing time in lp” | Ultency deal |
| `Include\Config\MT5APIConfigUltLiquiditySymbol.h` | 7× `QuotesFilterLP*` | Ultency quote filter |
| `Include\Bases\MT5APIDataset.h` | `FIELD_ULTENCY_DEAL_LP_PERFOMANCE_TIME` | Ultency dataset field |
| `Include\MT5APIReport.h` / `MT5APIConfigParam.h` | `TYPE_ULTENCY_PROVIDER` comments | Ultency report param |
| `Examples\Manager\DealerExample\DealerExampleDlg.*` | `LPARAM lp` | Win32 message param |

Owned wrapper `mt5-sdk\src` references **none** of `LiquidityProvider` / `QuotesFilterLP` / `\bLP\b`. It does use Win32 `LPCWSTR` four times (`mt5_manager.h` login/logout sinks; `mt5_tick_bridge.cpp` include comment + `OnTick`). That is a pointer typedef, not liquidity.

Do **not** map Ultency LP APIs onto Pepperstone account `1369850`.

### 6.2 Prometheus `# HELP`

`mt5-sdk\src\services\metrics_service.h`: **37** `# HELP propfirm_*` exposition lines. Substring `LP` inside `HELP`. Not a liquidity-provider token.

### 6.3 Swarm reports

`A87_not_an_lp.md` and INDEX/SWARM_LOG cite the same law. Documentation, not product source. D58 does not re-count report prose as product hits.

---

## 7. Delta vs A87 (same law, newer tree)

| A87 claim (earlier 2026-08-18) | D58 measured now |
|---|---|
| `src` / `apps` / `tests` zero LP identifiers | **Still zero** |
| `docs` zero hits | **Changed:** `docs/ctrader-fix.md:5` now states “not a liquidity provider” (correct prohibition; file is untracked/`??` in this worktree) |
| `.env.example` exists with forbid comment | **Worktree deleted** (` D .env.example`). HEAD blob still has the comment. Operator `.env` still has the comment |
| `DestinationQuote` XML “Latest bid/ask quote received from the destination venue” | **No XML comment** on current `DestinationQuoteSnapshot` (12 lines). Fields `VenueInstrumentId` / `VenueTimestamp` remain |
| `ExecutionIntent.DestinationAccount` | **Gone.** Current type has `DestinationSymbol`, no account, no `VenueId` |
| No `ExecutionVenue` type | **Still absent** |

A87’s naming law is unchanged. D58’s new facts are the `.env.example` deletion and the `docs/ctrader-fix.md` prohibition line.

---

## 8. Semantics product code must still not invent

Because this is **not** an LP (no contractual evidence in-repo):

1. No LP credit / last-look / internalization. Fills are broker `8` ExecutionReports.
2. No Ultency LP symbol catalog as the cTrader map. Discover instrument ids from Security List.
3. Source brokers (`ACHIEVER`, `STARWAVEFX`) ≠ destination venue (Pepperstone/cServer).
4. No single multiplexed “LP session.” Official model is two FIX sessions (QUOTE + TRADE).
5. No UI / log / metric names `LP health`, `LP fill`, `send to LP`. Use venue / destination / cTrader FIX.

Forbidden identifiers if anyone adds destination types later:

```text
Lp, LP, LiquidityProvider, liquidity_provider, lp_id, lp_code, lp_login, LpAccount, OurLp, lps, liquidity_providers
```

Required names (still unimplemented):

```text
ExecutionVenue / execution_venues / venue_id / venue_code=PEPPERSTONE_CSERVER
```

Exception: quoting §1.6, or documenting that a **future signed** LP contract would be required before those names are legal.

---

## 9. Honesty pin

- Measured **2026-08-18 13:40:11 +05:30** against HEAD `398a142`.
- Product C# + TS + appsettings: **0** `LP` / `LiquidityProvider` identifiers.
- Product docs: **1** correct prohibition (`docs/ctrader-fix.md:5`).
- Architecture: **1** prohibition block + **2** `execution_venues` table listings.
- `ExecutionVenue` type / EF mapping: **absent**.
- `.env.example`: **deleted in worktree**, present in HEAD with the forbid comment.
- Vendor Ultency LP APIs exist and are unused by owned code.
- This agent wrote **only** this report. Product source was not modified.

**PASS** on current product naming. **OPEN** until `ExecutionVenue` exists and is wired with `venue_id` instead of overloading `Broker`. **DO NOT CLOSE** the `.env.example` gap — a clean checkout still has the comment; this dirty tree does not.
