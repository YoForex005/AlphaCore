# A82 — `IMTDeal::EnDealReason`: real trading vs ignore for reconstruction

**Agent:** A82 (senior engineer, read-only of SDK / product / A21)  
**Date:** 2026-08-18  
**Question:** which `EnDealReason` values should deal reconstruction treat as **real trading** vs **ignore**.  
**Product source was not modified.** This file is the only write from A82.

---

## 0. Verdict (binding for this product)

Filter **action first**, then **reason**. Reason never overrides a non-BUY/SELL action.

| Bucket | Values | Reconstruction |
|---|---|---|
| **REAL_TRADING** | `0,1,2,3,4,5,7,9,10,16,17` | Apply to the position book. Count toward lifecycle open/scale/partial/complete, first-3, scoring, copy. |
| **SERVICE_MONEY** (ignore as a trade) | `6,8` (`ROLLOVER`, `VMARGIN`) | **Do not** open, close, scale, or complete a lifecycle. **Do not** change remaining / VWAP. If a lifecycle is already open, fold `profit` / `storage` / `commission` / `fee` into it. |
| **SERVICE_STRUCTURAL** (ignore as a trader trade) | `11,12,13,14,15,18,19` | **Do not** count as a trader decision / first-3 / scoring. **Do** keep the book aligned with the broker (apply volume, or mark the lifecycle **dirty** if volume would change remaining). Completing OUT that flats the book is persisted with `was_service_close=true` and **excluded** from first-3. |
| **UNKNOWN** | `< 0` impossible (`uint32_t`); `> 19`; or reason **absent** from ingest | Same as SERVICE_STRUCTURAL + `RECON_UNKNOWN_REASON`. **Do not** default missing reason to `CLIENT` (0). |

**One-line answer:** treat only client / expert / dealer / SL / TP / SO / external-client / gateway / signal / mobile / web as real trading. Ignore rollover, variation margin, settlement, transfer, sync, external-service, migration, split, and corporate action as trader activity.

This matches official MetaQuotes report helpers (`IsService` + `trade_deal`) plus `MIGRATION` (explicit on `IMTOrder`) and `CORPORATE_ACTION` (present in the C++ header, omitted from older WebAPI samples).

---

## 1. Sources (quoted, not modified)

| Role | Absolute path |
|---|---|
| Canonical enum + accessor | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h` |
| Parallel order reasons (migration wording) | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIOrder.h` |
| Dataset field | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDataset.h` |
| Official “service deal” + “trade deal” filters | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Trades.Standard.Reports\Reports\FastProfitDeals.cpp` |
| Same filters on position history | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Trades.Standard.Reports\Reports\PositionsHistory.cpp` |
| Official reason display names | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Capital.Standard.Reports\Reports\DealReasonReport.cpp` |
| Execution-type grouping (SL+TP+SO) | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Trades.Standard.Reports\Reports\ExecutionType.cpp` |
| Rollover / vmargin as non-trade actions | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Trades.Transaction.Reports\PluginInstance.cpp` |
| Older WebAPI mirrors (omit 19) | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\PHP\mt5_api\mt5_deal.php`, `...\Web\NET\MetaQuotes.MT5WebAPI\Common\MTDeal.cs` |
| MQL5 client docs (`ENUM_DEAL_REASON`) | https://www.mql5.com/en/docs/constants/tradingconstants/dealproperties |
| Reconstruction contract | `D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md` |
| Action/entry companion | `D:\Prop\reports\swarm\20260818\A37_mt5_deal_enums.md` |
| Current reconstructor | `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs`, `NormalizedDeal.cs` |

`SMTFormat` in `MT5APIFormat.h` formats **action** and **entry** only. There is **no** `FormatDealReason`.

---

## 2. Official enum (`IMTDeal::EnDealReason`)

Quoted from `MT5APIDeal.h` lines 54–80:

```54:80:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h
   //--- deal creation reasons
   enum EnDealReason
     {
      DEAL_REASON_CLIENT       =0,     // deal placed manually
      DEAL_REASON_EXPERT       =1,     // deal placed by expert
      DEAL_REASON_DEALER       =2,     // deal placed by dealer
      DEAL_REASON_SL           =3,     // deal placed due SL
      DEAL_REASON_TP           =4,     // deal placed due TP
      DEAL_REASON_SO           =5,     // deal placed due Stop-Out
      DEAL_REASON_ROLLOVER     =6,     // deal placed due rollover
      DEAL_REASON_EXTERNAL_CLIENT=7,   // deal placed from the external system by client
      DEAL_REASON_VMARGIN      =8,     // deal placed due variation margin
      DEAL_REASON_GATEWAY      =9,     // deal placed by gateway
      DEAL_REASON_SIGNAL       =10,    // deal placed by signal service
      DEAL_REASON_SETTLEMENT   =11,    // deal placed due to settlement
      DEAL_REASON_TRANSFER     =12,    // deal placed due position transfer
      DEAL_REASON_SYNC         =13,    // deal placed due position synchronization
      DEAL_REASON_EXTERNAL_SERVICE=14, // deal placed from the external system due service issues
      DEAL_REASON_MIGRATION    =15,    // deal placed due migration
      DEAL_REASON_MOBILE       =16,    // deal placed manually by mobile terminal
      DEAL_REASON_WEB          =17,    // deal placed manually by web terminal
      DEAL_REASON_SPLIT        =18,    // deal placed due split
      DEAL_REASON_CORPORATE_ACTION=19, // deal placed due corporate action
      //--- enumeration borders
      DEAL_REASON_FIRST        =DEAL_REASON_CLIENT,
      DEAL_REASON_LAST         =DEAL_REASON_CORPORATE_ACTION
     };
```

Accessor (get is `Reason()`, set is `ReasonSet()`; comment on the setter says `EnOrderReason` because the numeric map is shared with `IMTOrder::EnOrderReason`):

```200:218:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h
   //--- EnDealReason
   virtual uint32_t  Reason(void) const=0;
   ...
   //--- EnOrderReason
   virtual MTAPIRES  ReasonSet(const uint32_t reason)=0;
```

Dataset: `IMTDatasetField::FIELD_DEAL_REASON = 2030` (`uint32_t`, `EnDealReason`).

`IMTOrder::ORDER_REASON_MIGRATION` comment is more explicit than the deal header: *“order placed due account migration from MetaTrader 4 or MetaTrader 5”* (`MT5APIOrder.h:98`). Deal reason `15` is the same event.

---

## 3. Full table (value, official comment, MQL5 text, MQ label)

MQL5 `ENUM_DEAL_REASON` uses the same numbers for the subset it documents. Manager-only values (`DEALER`, `EXTERNAL_CLIENT`, `GATEWAY`, `SIGNAL`, `SETTLEMENT`, `TRANSFER`, `SYNC`, `EXTERNAL_SERVICE`, `MIGRATION`, plus the later `CORPORATE_ACTION`) exist on `IMTDeal` even when the client reference page omits them.

| Constant | Value | SDK comment | Official report label (`DealReasonReport`) | MQL5 client text (if published) |
|---|---:|---|---|---|
| `DEAL_REASON_CLIENT` | 0 | deal placed manually | Client | executed from a **desktop** terminal |
| `DEAL_REASON_EXPERT` | 1 | deal placed by expert | Expert | MQL5 program (EA / script) |
| `DEAL_REASON_DEALER` | 2 | deal placed by dealer | Dealer | *(Manager-only)* |
| `DEAL_REASON_SL` | 3 | deal placed due SL | Stop loss | Stop Loss activation |
| `DEAL_REASON_TP` | 4 | deal placed due TP | Take profit | Take Profit activation |
| `DEAL_REASON_SO` | 5 | deal placed due Stop-Out | Stop-Out | Stop Out event |
| `DEAL_REASON_ROLLOVER` | 6 | deal placed due rollover | Rollover | executed due to a rollover |
| `DEAL_REASON_EXTERNAL_CLIENT` | 7 | from the external system by client | External system | *(Manager-only)* |
| `DEAL_REASON_VMARGIN` | 8 | due variation margin | Variation margin | after charging the variation margin |
| `DEAL_REASON_GATEWAY` | 9 | deal placed by gateway | Gateway | *(Manager-only)* |
| `DEAL_REASON_SIGNAL` | 10 | deal placed by signal service | Signal | *(Manager-only)* |
| `DEAL_REASON_SETTLEMENT` | 11 | due to settlement | Settlement | *(Manager-only)* |
| `DEAL_REASON_TRANSFER` | 12 | due position transfer | Transfer | *(Manager-only)* |
| `DEAL_REASON_SYNC` | 13 | due position synchronization | Synchronization | *(Manager-only)* |
| `DEAL_REASON_EXTERNAL_SERVICE` | 14 | external system due service issues | Service in external system | *(Manager-only)* |
| `DEAL_REASON_MIGRATION` | 15 | due migration | Migration | *(Manager-only; order header: MT4/MT5 account migration)* |
| `DEAL_REASON_MOBILE` | 16 | manually by mobile terminal | Mobile | mobile application |
| `DEAL_REASON_WEB` | 17 | manually by web terminal | Web | web platform |
| `DEAL_REASON_SPLIT` | 18 | due split | Split | after a split (price reduction) with an open position |
| `DEAL_REASON_CORPORATE_ACTION` | 19 | due corporate action | **no case** (formats as “None”) | merge / rename / transferring a client, etc. |
| `DEAL_REASON_FIRST` | 0 | border | — | — |
| `DEAL_REASON_LAST` | 19 | border (`CORPORATE_ACTION`) | — | — |

`CExecutionType::GetReasonTypeName` **collapses** SL+TP+SO into one display bucket *“S/L, T/P and Stop-Out”* (it still stores them as 3/4/5). Reconstruction must **not** collapse them: they are distinct close reasons on a real trade.

`DealReasonReport` has **no** `case` for `DEAL_REASON_CORPORATE_ACTION` (19) → `nullptr` → UI “None”. Do not treat 19 as unknown; the C++ `IMTDeal` header is authoritative (`DEAL_REASON_LAST = 19`).

MQL5 also states: *for non-trading deals (balance, credit, commission, …) `DEAL_REASON_CLIENT` is indicated*. So **reason `0` is not proof of a trade**. Action is the first gate.

---

## 4. Binding drift (do not trust older samples for `LAST`)

| Binding | `DEAL_REASON_LAST` | `DEAL_REASON_CORPORATE_ACTION` |
|---|---|---|
| C++ `IMTDeal` (canonical, this tree) | `19` | present (`=19`) |
| PHP `MTEnDealReason` | `DEAL_REASON_SPLIT` (`18`) | **absent** |
| C# WebAPI `MTDeal.EnDealReason` | `DEAL_REASON_SPLIT` (`18`) | **absent** |

Same pattern A37 already recorded for `DEAL_LAST` / `DEAL_SO_COMPENSATION_CREDIT`. Use the C++ header.

---

## 5. How official MetaQuotes reports already split these

They do **not** treat every BUY/SELL as a trader fill. Two overlapping helpers:

### 5.1 `IsService()` — comment / “not a human initiator”

`FastProfitDeals.cpp` 812–817 and `PositionsHistory.cpp` 770–775 (identical):

```812:817:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Trades.Standard.Reports\Reports\FastProfitDeals.cpp
inline bool CFastProfitDeals::DealRecord::IsService(void) const
  {
   return(reason==IMTDeal::DEAL_REASON_ROLLOVER   || reason==IMTDeal::DEAL_REASON_VMARGIN  ||
          reason==IMTDeal::DEAL_REASON_SETTLEMENT || reason==IMTDeal::DEAL_REASON_TRANSFER ||
          reason==IMTDeal::DEAL_REASON_SYNC       || reason==IMTDeal::DEAL_REASON_EXTERNAL_SERVICE || reason==IMTDeal::DEAL_REASON_SPLIT);
  }
```

Used to **skip overwriting the position comment**. Not used as the volume filter.

### 5.2 `trade_deal` — include in open/close VWAP and volume

`FastProfitDeals.cpp` 858–861 and `PositionsHistory.cpp` 870–873 (identical):

```858:861:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Trades.Standard.Reports\Reports\FastProfitDeals.cpp
   const bool trade_deal=
   deal.reason!=IMTDeal::DEAL_REASON_ROLLOVER && deal.reason!=IMTDeal::DEAL_REASON_VMARGIN          &&
   deal.reason!=IMTDeal::DEAL_REASON_TRANSFER && deal.reason!=IMTDeal::DEAL_REASON_SYNC             &&
   deal.reason!=IMTDeal::DEAL_REASON_SPLIT    && deal.reason!=IMTDeal::DEAL_REASON_CORPORATE_ACTION;
```

Comment in `PositionsHistory.cpp:882`: *“if it is a variation margin or swap, we do not include it in prices and volumes”*.

Money (`profit` / `storage` / `commission`) is still added for non-`trade_deal` rows.

### 5.3 Official inconsistency (do not copy blindly)

| Reason | In `IsService`? | Excluded from `trade_deal` (volume/VWAP)? |
|---|---|---|
| `ROLLOVER` (6) | yes | yes |
| `VMARGIN` (8) | yes | yes |
| `SETTLEMENT` (11) | yes | **no** |
| `TRANSFER` (12) | yes | yes |
| `SYNC` (13) | yes | yes |
| `EXTERNAL_SERVICE` (14) | yes | **no** |
| `SPLIT` (18) | yes | yes |
| `CORPORATE_ACTION` (19) | **no** | yes |
| `MIGRATION` (15) | **no** | **no** |

MQ’s own reports therefore:

- never treat rollover / vmargin / transfer / sync / split as trade volume;
- treat settlement and external-service as “service” for comments but still let them move volume;
- treat corporate action as a volume-ignore even though it is not `IsService`;
- never special-case migration.

This product cannot inherit that inconsistency. Reconstruction needs a **closed** policy (section 7).

### 5.4 Transaction report: rollover / vmargin are not trades

`PluginInstance.cpp` 643–709: after balance-action handling, the deal sink **returns unless** reason is `ROLLOVER` or `VMARGIN`, then records them as `ACTION_ROLLOVER` / `ACTION_VMARGIN` — a third action class, not buy/sell trading.

### 5.5 Other official groupings (do **not** ignore these)

| Report | Reasons treated as **real initiator** |
|---|---|
| `DailyExpertReport.cpp` | `EXPERT`, `SIGNAL` |
| `AccountsLifetime.cpp` | `MOBILE`, `CLIENT`, `EXPERT`, `SIGNAL`, `WEB` |
| `WhiteLabel.cpp` | **keeps only** `GATEWAY` (WL fill filter, not a “ignore gateway” signal) |
| `ExecutionType.cpp` | counts every reason; SL/TP/SO merged only for display |

---

## 6. Action vs reason (two independent gates)

A21 already classifies by **action** (`D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md` §6):

```
is_tradeable(action)  = DEAL_BUY | DEAL_SELL
is_canceled(action)   = DEAL_BUY_CANCELED | DEAL_SELL_CANCELED
is_balance_like(action) = everything else  → skip (skipped_non_trade)
```

That gate is **necessary and not sufficient**.

- Balance / credit / commission / bonus / tax / SO-compensation rows are **ignored regardless of reason**. MQL5 even stamps them `DEAL_REASON_CLIENT`.
- Canceled BUY/SELL stay `RECON_CANCELED_DEAL` (dirty, no inverse fill) regardless of reason.
- **Reason is a second gate on tradeable BUY/SELL only.**

Current product code does **only** the action gate:

```25:25:D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs
    public bool IsTradingDeal => Action is DealAction.Buy or DealAction.Sell;
```

`NormalizedDeal`, `Mt5Deal`, `Mt5DealDto` have **no reason field**. `TradeReconstructor` never reads `IMTDeal::Reason()`. C++ `DealData` / `extractDeal` also omit `deal->Reason()` (`mt5_types.h:87-102`, `mt5_manager.cpp:1508-1524`). Ledger SQL *column* `reason_code` exists in `mt5_ledger_store.cpp` but the extractor never fills it.

A21 lists `reason` on `DealIn` as optional, default **0**, and stores it only as `close_reason` of a completing deal. It does **not** filter by reason. That default is unsafe: missing ingest becomes “desktop client”.

---

## 7. Reconstruction policy (this product)

Goal (architecture §§14–15 / A21): count **completed XAUUSD position lifecycles** that represent **trader (or EA / signal / protective) market activity**, then score them. A nightly rollover close+reopen, a stock split, or an MT4→MT5 migration IN must **not** become trade #1/#2/#3.

### 7.1 Predicates (copy-paste)

```
# EnDealReason — C++ IMTDeal numbers only

REAL_TRADING = {
  0,  # CLIENT            desktop
  1,  # EXPERT            EA / script
  2,  # DEALER            dealing desk
  3,  # SL
  4,  # TP
  5,  # SO                stop-out (forced close of a real book)
  7,  # EXTERNAL_CLIENT   FIX / external client API (source traders can be this)
  9,  # GATEWAY           LP / gateway fill of a real order
 10,  # SIGNAL            MQL5 signal copy (still a real lifecycle)
 16,  # MOBILE
 17,  # WEB
}

SERVICE_MONEY = {
  6,  # ROLLOVER          swap / session roll (MQ: exclude from volume/VWAP)
  8,  # VMARGIN           futures variation margin (rare on XAUUSD CFD)
}

SERVICE_STRUCTURAL = {
 11,  # SETTLEMENT        expiry / settlement
 12,  # TRANSFER          position transfer
 13,  # SYNC              position synchronization
 14,  # EXTERNAL_SERVICE  external service issue
 15,  # MIGRATION         MT4/MT5 account migration
 18,  # SPLIT             instrument split
 19,  # CORPORATE_ACTION  merge / rename / account move
}

is_real_trading_reason(r)     = r in REAL_TRADING
is_service_money_reason(r)    = r in SERVICE_MONEY
is_service_structural_reason(r) = r in SERVICE_STRUCTURAL
is_unknown_reason(r)          = r is missing OR r > 19 OR not in any set above
```

`SERVICE_IGNORE` (binary “ignore as trading”) = `SERVICE_MONEY ∪ SERVICE_STRUCTURAL ∪ UNKNOWN`.

### 7.2 Per-deal pipeline (insert after A21 §6 step 4, before §7 apply)

After sort, after action classification, after XAUUSD symbol gate:

```
5. if reason missing or > DEAL_REASON_LAST:
     treat as UNKNOWN (do not coerce to CLIENT)
6. if is_service_money_reason:
     skipped_service_reason++
     if book.current: apply_money(book.current, deal); record ticket as money-only
     do not touch remaining / VWAP / opening_event_count
     do not emit OPENED / INCREASED / REDUCED / COMPLETED
     return
7. if is_service_structural_reason or is_unknown_reason:
     skipped_service_reason++  (or skipped_unknown_reason)
     apply_service_structural(book, deal)   # §7.3
     return
8. else REAL_TRADING → A21 §7 apply unchanged
     completing deal sets t.close_reason = deal.reason
```

### 7.3 Structural service apply (keep the book, poison first-3)

These deals **can** change broker volume (split, settlement flatten, transfer, sync, migration IN). Silent skip desynchronizes `remaining_h`. Silent count inflates first-3.

```
apply_service_structural(book, deal):
  if book.current:
    apply_money(book.current, deal)
  if volume_h == 0:
    return

  # volume-mutating service event
  book.current.dirty = true          # or open a dirty book if remaining was 0
  apply A21 §7 entry rules for remaining only
  if this completes the lifecycle:
    t.completed = true
    t.was_service_close = true
    t.close_reason = deal.reason
    persist
    do NOT increment First3State.completed_count
    do NOT emit EARLY_SCORE_ELIGIBLE
  if this would open on a flat book:
    open_lifecycle(...)
    t.dirty = true                   # excluded from first-3 by A21 §7.7
    do NOT emit XAU_LIFECYCLE_OPENED as a trader event
    increment recon_service_open_total
```

This is the same conservatism A21 already uses for canceled deals: **dirty + exclude beats a silent wrong first-3**.

### 7.4 Why each REAL_TRADING value is in

| Reason | Why it is a real reconstruction input |
|---|---|
| `CLIENT` / `MOBILE` / `WEB` | Human order from a terminal. Core source-trader activity. |
| `EXPERT` | EA — still the trader’s system. Must count for martingale / lot-escalation scoring. |
| `DEALER` | Desk fill of a client request. Position still moved because the trader (or their risk) asked. |
| `SL` / `TP` | Protective exits. Completing a lifecycle. First-3 **must** count an SL’d gold ticket as a completed trade. |
| `SO` | Forced flatten. Completes the book. Risk-engine / scoring **want** this close (stop-out behaviour). |
| `EXTERNAL_CLIENT` | Client via FIX / external API. This product’s source side may appear as this. Ignoring it would drop the traders we copy. |
| `GATEWAY` | Gateway / LP execution of a real order. Official WhiteLabel report *selects* this reason as the fill. |
| `SIGNAL` | MQL5 signal subscription. Still a real position lifecycle on the login (copy-follower, not corporate). |

### 7.5 Why each IGNORE value is out

| Reason | What it is | If treated as a trade | If dropped with no book rule |
|---|---|---|---|
| `ROLLOVER` (6) | Session / swap roll. Often paired OUT+IN of the same volume at roll price. MQ excludes from VWAP. | Every roll night **completes** a lifecycle and opens another → first-3 saturates on swap, not skill. | Usually safe: net remaining unchanged if both legs skipped. Fold storage/profit. |
| `VMARGIN` (8) | Futures variation-margin charge. Rare on XAUUSD CFD. | Fake scale / close. | Same as rollover. |
| `SETTLEMENT` (11) | Contract settlement / expiry. | A flatten is a real close of *exposure*, but **not** a trader exit. Must not be first-3. | Remaining stuck open forever if it was the flatten. |
| `TRANSFER` (12) | Position moved between accounts / servers. | Fake close on source, fake open on dest. | Book wrong on both sides. |
| `SYNC` (13) | Gateway / hedge-account synchronization. | Technical IN/OUT counted as trades. | Remaining drift vs `mt5_positions_current`. |
| `EXTERNAL_SERVICE` (14) | External-system service correction. | Service bust counted as skill. | Possible volume drift. |
| `MIGRATION` (15) | MT4→MT5 (or server) account move. Historical IN of the whole book. | Entire migrated history becomes “new” first-3. | Open exposure invisible. |
| `SPLIT` (18) | Instrument split: volume up, price down. MQ: exclude from volume/VWAP. | Fake scale-in + bogus VWAP. | `remaining_h` no longer matches broker lots. |
| `CORPORATE_ACTION` (19) | Merge, rename, client transfer (MQL5 text). MQ `trade_deal` excludes it. | Corporate event as a trade. | Same as split / transfer. |

XAUUSD CFD at typical prop brokers will see **CLIENT / EXPERT / SL / TP / SO / MOBILE / WEB** daily. `ROLLOVER` appears if the symbol is configured to close/reopen for swap. `SPLIT` / `CORPORATE_ACTION` / `SETTLEMENT` are rare on spot gold and still must be classified so a future symbol or an odd broker cannot poison the book.

### 7.6 `close_reason` (A21 §4.1)

On a **REAL_TRADING** complete, set `close_reason` to the completing deal’s reason (already in A21 §7.7). Useful values: `SL`, `TP`, `SO`, `CLIENT`, `EXPERT`, `MOBILE`, `WEB`, `SIGNAL`, `DEALER`, `GATEWAY`, `EXTERNAL_CLIENT`.

On a **service** complete, set `close_reason` to the service reason **and** `was_service_close=true`. Scoring (A22) must ignore `was_service_close` / `dirty` rows — A21 already excludes dirty from first-3.

Do **not** invent a close reason from comments (`[sl]`, `so:`, …) when `Reason()` is present.

---

## 8. Binary cheat-sheet (what the title asked)

Treat as **real trading** (apply + count):

```
DEAL_REASON_CLIENT            0
DEAL_REASON_EXPERT            1
DEAL_REASON_DEALER            2
DEAL_REASON_SL                3
DEAL_REASON_TP                4
DEAL_REASON_SO                5
DEAL_REASON_EXTERNAL_CLIENT   7
DEAL_REASON_GATEWAY           9
DEAL_REASON_SIGNAL           10
DEAL_REASON_MOBILE           16
DEAL_REASON_WEB              17
```

**Ignore** as trader activity (do not count; money-only or dirty-book):

```
DEAL_REASON_ROLLOVER          6     # money-only
DEAL_REASON_VMARGIN           8     # money-only
DEAL_REASON_SETTLEMENT       11     # structural
DEAL_REASON_TRANSFER         12     # structural
DEAL_REASON_SYNC             13     # structural
DEAL_REASON_EXTERNAL_SERVICE 14     # structural
DEAL_REASON_MIGRATION        15     # structural
DEAL_REASON_SPLIT            18     # structural
DEAL_REASON_CORPORATE_ACTION 19     # structural
```

Borders `FIRST`/`LAST` are not stored on deals.

---

## 9. Worked examples (XAUUSD, hundredths, A21 notation)

### E1 — SL close (REAL, must count)

| ticket | action | entry | reason | volume_h |
|---:|---:|---:|---:|---:|
| 1 | 0 BUY | IN | CLIENT (0) | 100 |
| 2 | 1 SELL | OUT | SL (3) | 100 |

One completed LONG. `close_reason=3`. `completed_count += 1`.

### E2 — Stop-out (REAL, must count)

Same as E1 with reason `SO (5)`. Still a completed lifecycle. Scoring wants this.

### E3 — Overnight rollover (IGNORE as trade)

| ticket | action | entry | reason | volume_h | storage |
|---:|---:|---:|---:|---:|---:|
| 1 | 0 BUY | IN | CLIENT | 100 | 0 |
| 2 | 1 SELL | OUT | ROLLOVER | 100 | -0.40 |
| 3 | 0 BUY | IN | ROLLOVER | 100 | 0 |
| 4 | 1 SELL | OUT | CLIENT | 100 | 0 |

Naive A21 (reason-blind): two completed lifecycles (1–2 and 3–4). First-3 slot burned on swap.

Correct: 2 and 3 are SERVICE_MONEY. Money from 2 folds into the still-open trade. Remaining stays +100 through the roll. Ticket 4 completes **one** trade. `deal_count` of real legs = 2 (tickets 1,4); rollover tickets may be listed as money-only.

### E4 — Migration dump (IGNORE as trader open)

A migrated login emits many `ENTRY_IN` `DEAL_BUY/SELL` with `reason=MIGRATION`. Reason-blind reconstructor opens a lifecycle per position and will complete them as “first trades” when the trader later closes.

Correct: open dirty / `was_service_open`, exclude from first-3. Subsequent REAL_TRADING OUT may complete the dirty book (still excluded) or, if product prefers, start first-3 only from the first post-migration REAL_TRADING IN.

**Recommendation for this lab:** first-3 starts at the first **REAL_TRADING** IN after ingest begin; migration INs never occupy first-3.

### E5 — Split (IGNORE as scale-in)

Open 1.00 @ 2400, then `SPLIT` BUY IN 1.00 @ 1200. Reason-blind: `was_scaled_in`, `was_averaged_down`, VWAP 1800 — all false. Correct: remaining becomes 2.00 (book), VWAP **unchanged** or marked dirty, not a scale-in flag.

---

## 10. Product gaps (measured; not fixed in this agent)

| Surface | Reason support |
|---|---|
| `IMTDeal::Reason()` | exists |
| C++ `DealData` / `extractDeal` | **omits** reason |
| C++ ledger `reason_code` column | declared, not populated by extractor |
| `Mt5DealDto` / `Mt5Deal` / `NormalizedDeal` | **no** reason |
| `DealAction` / `DealEntry` enums | exist; **no** `DealReason` enum |
| `TradeReconstructor` | action-only `IsTradingDeal` |
| A21 | reason optional, default 0, stored as `close_reason` only |

Until ingest persists `Reason()`, the predicates in §7 cannot run. **Do not** backfill `CLIENT`. Leave reason nullable / unknown.

---

## 11. Suggested persist / metrics (not a migration)

On `NormalizedDeal` / `mt5_deals`: `reason uint32 NULL`.

On `ReconstructedTrade`: `close_reason uint32 NULL`, `was_service_close bool`, `dirty` already in A21.

Counters:

```
skipped_service_reason_total{reason}
skipped_unknown_reason_total
recon_service_open_total
recon_service_close_total
```

---

## 12. Findings

1. **`EnDealReason` is 0–19.** Canonical last is `DEAL_REASON_CORPORATE_ACTION=19`. PHP/C# WebAPI samples stop at 18.
2. **Reason is not a substitute for action.** Balance ops arrive as `CLIENT`. Always `is_tradeable(action)` first.
3. **Eleven reasons are real trading** for reconstruction: `CLIENT, EXPERT, DEALER, SL, TP, SO, EXTERNAL_CLIENT, GATEWAY, SIGNAL, MOBILE, WEB`.
4. **Nine reasons are ignore-as-trading:** `ROLLOVER, VMARGIN` (money-only) and `SETTLEMENT, TRANSFER, SYNC, EXTERNAL_SERVICE, MIGRATION, SPLIT, CORPORATE_ACTION` (structural / dirty-book).
5. Official MQ `IsService` / `trade_deal` already exclude most of the ignore set from volume/VWAP, but they disagree on settlement / external-service / corporate-action / migration. This document **closes** that set.
6. **SL / TP / SO are real.** Ignoring them would drop the actual close of almost every protective exit.
7. **`EXTERNAL_CLIENT` and `GATEWAY` are real.** Ignoring them would drop FIX/API source traders and LP fills.
8. **Rollover is the high-frequency foot-gun** on symbols that close/reopen for swap: reason-blind A21 will fabricate completed lifecycles every roll.
9. Structural ignores **must still move remaining** (or dirty the book). Blind drop of `SPLIT` / `SETTLEMENT` / `TRANSFER` desyncs volume.
10. Current C# reconstructor and C++ `extractDeal` **do not read `Reason()`**. A21’s default `reason=0` would mis-label every deal as desktop client once a field is added without ingest. Persist nullable reason; treat absence as UNKNOWN.

**Product source was not modified.** This file is the only write from A82.
