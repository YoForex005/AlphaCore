# A37 — MT5 Manager SDK deal enums (`EnDealAction`, `EnDealEntry`) and volume scale

**Agent:** A37 (senior engineer, read-only of SDK / product source)  
**Date:** 2026-08-18  
**Scope:** quote official Manager API constants for deal action, deal entry (IN / OUT / INOUT / OUT_BY), and integer volume scale.  
**Product source was not modified.**

---

## 0. Sources (quoted, not modified)

| Role | Absolute path |
|---|---|
| Canonical deal interface (enums + volume accessors) | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h` |
| Volume scale macros + conversion helpers | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h` |
| Human strings for action / entry | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIFormat.h` |
| Dataset field / column types | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDataset.h` |
| Automation condition / type ids (name only) | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Config\MT5APIConfigAutomation.h` |
| PHP WebAPI mirror (older border) | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\PHP\mt5_api\mt5_deal.php` |
| C# WebAPI mirror (older border) | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\Common\MTDeal.cs` |

Primary class: `IMTDeal` in `MT5APIDeal.h` (copyright MetaQuotes Ltd., 2000–2026).

---

## 1. Grep result: `DEAL_ENTRY` is **not** the Manager-API enumerator name

Workspace grep for `DEAL_ENTRY` under `D:\Prop` hits **only** dataset / automation **field and type ids**, never the entry enumerator itself:

| Identifier | Value | File | Meaning |
|---|---|---|---|
| `IMTDatasetColumn::TYPE_DEAL_ENTRY` | `501` | `MT5APIDataset.h:55` | column type “Deal Entry (UInt32)” |
| `IMTDatasetField::FIELD_DEAL_ENTRY` | `2007` | `MT5APIDataset.h:454` | dataset field, `uint32_t`, **typed as `IMTDeal::EnDealEntry`** |
| `CONDITION_DEAL_ENTRY` | `7006` | `MT5APIConfigAutomation.h:813` | automation condition id |
| `TYPE_DEAL_ENTRY` (automation) | `19` | `MT5APIConfigAutomation.h:972` | automation type id |

There are **no** identifiers `DEAL_ENTRY_IN`, `DEAL_ENTRY_OUT`, `DEAL_ENTRY_INOUT`, or `DEAL_ENTRY_OUT_BY` anywhere in this Manager SDK tree.

Those `DEAL_ENTRY_*` names belong to the **MQL5 client terminal** enum `ENUM_DEAL_ENTRY`. Numeric values match the Manager API (`0/1/2/3`), but the Manager C++ names are `ENTRY_*` inside `IMTDeal::EnDealEntry`.

When reading Manager reports or C++ plugins, match against `IMTDeal::ENTRY_IN` / `ENTRY_OUT` / `ENTRY_INOUT` / `ENTRY_OUT_BY`. Do not expect `DEAL_ENTRY_*` to compile against `IMTDeal`.

Dataset reports already do this, e.g. `BalanceCache.cpp` / `DepositCache.cpp`:

```cpp
res=composer.FieldAddWhereUInt(IMTDeal::ENTRY_IN);
```

---

## 2. `IMTDeal::EnDealAction` — official constants

Quoted from `MT5APIDeal.h` lines 16–42:

```16:42:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h
   enum EnDealAction
     {
      DEAL_BUY                 =0,     // buy
      DEAL_SELL                =1,     // sell
      DEAL_BALANCE             =2,     // deposit operation
      DEAL_CREDIT              =3,     // credit operation
      DEAL_CHARGE              =4,     // additional charges
      DEAL_CORRECTION          =5,     // correction deals
      DEAL_BONUS               =6,     // bonus
      DEAL_COMMISSION          =7,     // commission
      DEAL_COMMISSION_DAILY    =8,     // daily commission
      DEAL_COMMISSION_MONTHLY  =9,     // monthly commission
      DEAL_AGENT_DAILY         =10,    // daily agent commission
      DEAL_AGENT_MONTHLY       =11,    // monthly agent commission
      DEAL_INTERESTRATE        =12,    // interest rate charges
      DEAL_BUY_CANCELED        =13,    // canceled buy deal
      DEAL_SELL_CANCELED       =14,    // canceled sell deal
      DEAL_DIVIDEND            =15,    // dividend
      DEAL_DIVIDEND_FRANKED    =16,    // franked dividend
      DEAL_TAX                 =17,    // taxes
      DEAL_AGENT               =18,    // instant agent commission
      DEAL_SO_COMPENSATION     =19,    // negative balance compensation after stop-out
      DEAL_SO_COMPENSATION_CREDIT=20,  // credit compensation after stop-out
      //--- enumeration borders
      DEAL_FIRST               =DEAL_BUY,
      DEAL_LAST                =DEAL_SO_COMPENSATION_CREDIT
     };
```

### 2.1 Table

| Constant | Value | SDK comment |
|---|---:|---|
| `IMTDeal::DEAL_BUY` | 0 | buy |
| `IMTDeal::DEAL_SELL` | 1 | sell |
| `IMTDeal::DEAL_BALANCE` | 2 | deposit operation |
| `IMTDeal::DEAL_CREDIT` | 3 | credit operation |
| `IMTDeal::DEAL_CHARGE` | 4 | additional charges |
| `IMTDeal::DEAL_CORRECTION` | 5 | correction deals |
| `IMTDeal::DEAL_BONUS` | 6 | bonus |
| `IMTDeal::DEAL_COMMISSION` | 7 | commission |
| `IMTDeal::DEAL_COMMISSION_DAILY` | 8 | daily commission |
| `IMTDeal::DEAL_COMMISSION_MONTHLY` | 9 | monthly commission |
| `IMTDeal::DEAL_AGENT_DAILY` | 10 | daily agent commission |
| `IMTDeal::DEAL_AGENT_MONTHLY` | 11 | monthly agent commission |
| `IMTDeal::DEAL_INTERESTRATE` | 12 | interest rate charges |
| `IMTDeal::DEAL_BUY_CANCELED` | 13 | canceled buy deal |
| `IMTDeal::DEAL_SELL_CANCELED` | 14 | canceled sell deal |
| `IMTDeal::DEAL_DIVIDEND` | 15 | dividend |
| `IMTDeal::DEAL_DIVIDEND_FRANKED` | 16 | franked dividend |
| `IMTDeal::DEAL_TAX` | 17 | taxes |
| `IMTDeal::DEAL_AGENT` | 18 | instant agent commission |
| `IMTDeal::DEAL_SO_COMPENSATION` | 19 | negative balance compensation after stop-out |
| `IMTDeal::DEAL_SO_COMPENSATION_CREDIT` | 20 | credit compensation after stop-out |
| `IMTDeal::DEAL_FIRST` | 0 (`DEAL_BUY`) | enumeration border |
| `IMTDeal::DEAL_LAST` | 20 (`DEAL_SO_COMPENSATION_CREDIT`) | enumeration border |

Accessor (same header, lines 116–118):

```116:118:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h
   //--- EnDealAction
   virtual uint32_t  Action(void) const=0;
   virtual MTAPIRES  Action(const uint32_t action)=0;
```

Dataset field: `FIELD_DEAL_ACTION = 2006` (`uint32_t`, `IMTDeal::EnDealAction`).

### 2.2 Trade vs balance/cash actions

- **Market / position-forming:** `DEAL_BUY` (0), `DEAL_SELL` (1). Canceled counterparts: `DEAL_BUY_CANCELED` (13), `DEAL_SELL_CANCELED` (14).
- **Balance / ledger (typically volume 0, no symbol required):** `DEAL_BALANCE` (2), `DEAL_CREDIT` (3), `DEAL_CHARGE` (4), `DEAL_CORRECTION` (5), `DEAL_BONUS` (6), commission family (7–11, 18), `DEAL_INTERESTRATE` (12), dividends/tax (15–17), stop-out compensation (19–20).

Manager examples (`BalanceExample.NET`) expose only a subset of ledger actions for deposit UI: `DEAL_BALANCE`, `DEAL_CREDIT`, `DEAL_CHARGE`, `DEAL_CORRECTION`, `DEAL_BONUS`, `DEAL_COMMISSION`.

### 2.3 Format strings (`SMTFormat::FormatDealAction`)

Quoted from `MT5APIFormat.h` 760–787:

| Constant | Format string |
|---|---|
| `DEAL_BUY` | `buy` |
| `DEAL_SELL` | `sell` |
| `DEAL_BALANCE` | `balance` |
| `DEAL_CREDIT` | `credit` |
| `DEAL_CHARGE` | `charge` |
| `DEAL_CORRECTION` | `correction` |
| `DEAL_BONUS` | `bonus` |
| `DEAL_COMMISSION` | `commission` |
| `DEAL_COMMISSION_DAILY` | `daily commission` |
| `DEAL_COMMISSION_MONTHLY` | `monthly commission` |
| `DEAL_AGENT_DAILY` | `daily agent commission` |
| `DEAL_AGENT_MONTHLY` | `monthly agent commission` |
| `DEAL_INTERESTRATE` | `interest rate` |
| `DEAL_BUY_CANCELED` | `canceled buy` |
| `DEAL_SELL_CANCELED` | `canceled sell` |
| `DEAL_DIVIDEND` | `dividend` |
| `DEAL_DIVIDEND_FRANKED` | `franked dividend` |
| `DEAL_TAX` | `tax` |
| `DEAL_AGENT` | `agent commission` |
| `DEAL_SO_COMPENSATION` | `so compensation` |

**Gap:** `FormatDealAction` has **no** `case` for `DEAL_SO_COMPENSATION_CREDIT` (20). That action formats as an empty string.

### 2.4 Binding drift (do not trust older samples for `DEAL_LAST`)

| Binding | `DEAL_LAST` | `DEAL_SO_COMPENSATION_CREDIT` |
|---|---|---|
| C++ Manager header `IMTDeal` (canonical) | `20` | present (`=20`) |
| PHP `MTEnDealAction` (`mt5_deal.php`) | `DEAL_SO_COMPENSATION` (`19`) | **absent** |
| C# `MTDeal.EnDealAction` (`MTDeal.cs`) | `DEAL_SO_COMPENSATION` (`19`) | **absent** |

Treat the C++ header as authoritative for the installed SDK.

---

## 3. `IMTDeal::EnDealEntry` — official constants (IN / OUT / INOUT / OUT_BY)

Quoted from `MT5APIDeal.h` lines 43–53:

```43:53:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h
   //--- deal entry direction
   enum EnDealEntry
     {
      ENTRY_IN                 =0,     // in market
      ENTRY_OUT                =1,     // out of market
      ENTRY_INOUT              =2,     // reverse
      ENTRY_OUT_BY             =3,     // closed by  hedged position
      //--- enumeration borders
      ENTRY_FIRST              =ENTRY_IN,
      ENTRY_LAST               =ENTRY_OUT_BY
     };
```

### 3.1 Table (Manager name vs MQL5 client alias)

| Manager API (`IMTDeal::EnDealEntry`) | Value | SDK comment | MQL5 client alias (same number, **not** in this SDK) | `SMTFormat::FormatDealEntry` |
|---|---:|---|---|---|
| `ENTRY_IN` | 0 | in market | `DEAL_ENTRY_IN` | `in` |
| `ENTRY_OUT` | 1 | out of market | `DEAL_ENTRY_OUT` | `out` |
| `ENTRY_INOUT` | 2 | reverse | `DEAL_ENTRY_INOUT` | `in/out` |
| `ENTRY_OUT_BY` | 3 | closed by hedged position | `DEAL_ENTRY_OUT_BY` | `out by` |
| `ENTRY_FIRST` | 0 | border | — | — |
| `ENTRY_LAST` | 3 | border (`ENTRY_OUT_BY`) | — | — |

Format switch, `MT5APIFormat.h` 800–805:

```800:805:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIFormat.h
      case IMTDeal::ENTRY_IN    : str.Assign(L"in");     break;
      case IMTDeal::ENTRY_OUT   : str.Assign(L"out");    break;
      case IMTDeal::ENTRY_INOUT : str.Assign(L"in/out"); break;
      case IMTDeal::ENTRY_OUT_BY: str.Assign(L"out by"); break;
```

Accessor (header lines 119–121; comment says `EnEntryFlags`, but the enum type is `EnDealEntry`):

```119:121:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h
   //--- EnEntryFlags
   virtual uint32_t  Entry(void) const=0;
   virtual MTAPIRES  Entry(const uint32_t entry)=0;
```

Dataset: `FIELD_DEAL_ENTRY = 2007` (`uint32_t`, `IMTDeal::EnDealEntry`). Column type `TYPE_DEAL_ENTRY = 501`.

### 3.2 Semantics (from SDK comments + official report samples)

| Entry | Meaning in this SDK | Typical use in MetaQuotes report plugins |
|---|---|---|
| `ENTRY_IN` (0) | “in market” — opens / increases a position | deposit/balance caches filter `ENTRY_IN`; `DealUserCache` treats IN as the open-price side |
| `ENTRY_OUT` (1) | “out of market” — closes / reduces a position | `CDealCache::s_entries[] = { ENTRY_OUT, ENTRY_INOUT }`; daily/execution reports require OUT or INOUT for realized P/L |
| `ENTRY_INOUT` (2) | “reverse” — close remaining volume and open opposite in one deal | treated as both IN and OUT by `DealUserCache` (`inout \|\| entry==ENTRY_IN` / `ENTRY_OUT`) |
| `ENTRY_OUT_BY` (3) | “closed by hedged position” | close-by / hedge close; `IMTDealSink::OnDealPerformCloseBy` is the paired-deal callback |

`PositionsHistory.h` helpers:

```cpp
bool IsOut(void)   const { return(entry==IMTDeal::ENTRY_OUT); }
bool IsInOut(void) const { return(entry==IMTDeal::ENTRY_INOUT); }
```

### 3.3 WebAPI extra: `ENTRY_STATE = 255` (not in C++ `IMTDeal`)

PHP `MTEnEntryFlags` and C# `MTDeal.EnEntryFlags` add:

```text
ENTRY_STATE = 255  // state record
ENTRY_LAST  = ENTRY_STATE   // WebAPI only
```

The **C++ Manager header does not define `ENTRY_STATE`**. `IMTDeal::ENTRY_LAST` is `ENTRY_OUT_BY` (3). Do not treat 255 as a valid `IMTDeal::EnDealEntry` when talking to the Manager API.

---

## 4. Volume scale (integer lots, not doubles)

`IMTDeal` stores volume as **`uint64_t` integers**, never as lot doubles. Two scales exist.

### 4.1 Accessors (`MT5APIDeal.h`)

| Method | Comment in header | Type |
|---|---|---|
| `Volume()` / `Volume(uint64_t)` | “deal volume” | `uint64_t` — **legacy 4-digit** scale |
| `VolumeClosed()` / setter | “closed volume” | `uint64_t` — same 4-digit scale |
| `VolumeExt()` / setter | “deal volume with extended accuracy” | `uint64_t` — **8-digit** scale |
| `VolumeClosedExt()` / setter | “closed volume with extended accuracy” | `uint64_t` — 8-digit scale |
| `VolumeGatewayExt()` / setter | “confirmed gateway volume with extended accuracy” | `uint64_t` — 8-digit scale |

Quoted:

```140:142:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h
   //--- deal volume
   virtual uint64_t  Volume(void) const=0;
   virtual MTAPIRES  Volume(const uint64_t volume)=0;
```

```229:234:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h
   //--- deal volume with extended accuracy
   virtual uint64_t  VolumeExt(void) const=0;
   virtual MTAPIRES  VolumeExt(const uint64_t volume)=0;
   //--- closed volume with extended accuracy
   virtual uint64_t  VolumeClosedExt(void) const=0;
   virtual MTAPIRES  VolumeClosedExt(const uint64_t volume)=0;
```

Dataset reports pull **extended** volume: `FIELD_DEAL_VOLUME_EXT = 2014`, `FIELD_DEAL_VOLUME_CLOSED_EXT = 2025`. There is no `FIELD_DEAL_VOLUME` (non-ext) in the deal field block.

### 4.2 Scale macros (`MT5APIMath.h` lines 12–20)

```12:20:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h
#define MTAPI_VOLUME_DIV        (10000.0)
#define MTAPI_VOLUME_DIGITS     (4)
#define MTAPI_VOLUME_MAX        ((uint64_t)10000000000)
//+------------------------------------------------------------------+
//| Volume with extended accuracy constants                          |
//+------------------------------------------------------------------+
#define MTAPI_VOLUME_EXT_DIV    (100000000.0)
#define MTAPI_VOLUME_EXT_DIGITS (8)
#define MTAPI_VOLUME_EXT_MAX    ((uint64_t)10000000000000000000u)
```

| Scale | Macro | Divisor | Digits | Max integer | Lots formula |
|---|---|---:|---:|---|---|
| Legacy (`Volume` / `VolumeClosed`) | `MTAPI_VOLUME_DIV` | **10 000** | 4 | `MTAPI_VOLUME_MAX` = 10 000 000 000 | `lots = Volume / 10000.0` |
| Extended (`VolumeExt` / `VolumeClosedExt`) | `MTAPI_VOLUME_EXT_DIV` | **100 000 000** | 8 | `MTAPI_VOLUME_EXT_MAX` = 10^19 | `lots = VolumeExt / 100000000.0` |

Worked examples:

| Lots | `Volume()` (÷ 10 000) | `VolumeExt()` (÷ 100 000 000) |
|---:|---:|---:|
| 0.01 | 100 | 1 000 000 |
| 0.10 | 1 000 | 10 000 000 |
| 1.00 | 10 000 | 100 000 000 |
| 1.23 | 12 300 | 123 000 000 |
| 0.00000001 (1e-8) | 0 (underflows 4-digit) | 1 |

### 4.3 Official conversion helpers (`SMTMath`)

```255:320:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h
inline uint64_t SMTMath::VolumeToInt(const double volume)
  {
   return(PriceToIntPos(volume,MTAPI_VOLUME_DIGITS));
  }
// ...
inline double SMTMath::VolumeToDouble(const uint64_t volume)
  {
   return(PriceNormalize(volume/double(MTAPI_VOLUME_DIV),MTAPI_VOLUME_DIGITS));
  }
// ...
inline uint64_t SMTMath::VolumeFromVolumeExt(const uint64_t volume_ext)
  {
   return(volume_ext/10000);
  }
// ...
inline double SMTMath::VolumeExtToDouble(const uint64_t volume)
  {
   return(PriceNormalize(volume/double(MTAPI_VOLUME_EXT_DIV),MTAPI_VOLUME_EXT_DIGITS));
  }
// ...
inline uint64_t SMTMath::VolumeExtFromVolume(const uint64_t volume)
  {
   return(volume*10000);
  }
```

| Helper | Direction | Implementation |
|---|---|---|
| `VolumeToInt(lots)` | lots → 4-digit int | `PriceToIntPos(lots, 4)` |
| `VolumeToDouble(vol)` | 4-digit int → lots | `normalize(vol / 10000.0, 4)` |
| `VolumeToSize(vol, contract)` | 4-digit int → units | `(vol / 10000.0) * contract_size` |
| `VolumeFromSize(size, contract)` | units → 4-digit int | `(size / contract) * 10000` |
| `VolumeFromVolumeExt(ext)` | 8-digit → 4-digit | **`ext / 10000`** (integer) |
| `VolumeExtToInt(lots)` | lots → 8-digit int | `PriceToIntPos(lots, 8)` |
| `VolumeExtToDouble(ext)` | 8-digit int → lots | `normalize(ext / 100000000.0, 8)` |
| `VolumeExtToSize(ext, contract)` | 8-digit int → units | `(ext / 1e8) * contract_size` |
| `VolumeExtFromSize(size, contract)` | units → 8-digit int | `(size / contract) * 1e8` |
| `VolumeExtFromVolume(vol)` | 4-digit → 8-digit | **`vol * 10000`** |

Ratio between scales is exactly **10 000** (`MTAPI_VOLUME_EXT_DIV / MTAPI_VOLUME_DIV = 10 000`). That is why `VolumeFromVolumeExt` divides by 10000 and `VolumeExtFromVolume` multiplies by 10000.

### 4.4 How official reports convert

- 4-digit path: `SMTMath::VolumeToDouble(deal->Volume())` (`AgentsDetailed.cpp`), `deal->Volume()/10000.0` (`ExecutionType.cpp`).
- 8-digit path (dataset `FIELD_DEAL_VOLUME_EXT`): `deal.volume/100000000.0` (`DealCache.cpp`, `DealWeekCache.cpp`).
- New orders in examples: `request->Volume(SMTMath::VolumeToInt(1.0))` → `10000` for 1.00 lot.

**Rule for consumers:** if the integer came from `VolumeExt()` or `FIELD_DEAL_VOLUME_EXT`, divide by **1e8**. If it came from `Volume()`, divide by **1e4**. Mixing the two off-by-10 000 is a common bug.

---

## 5. Related enums on the same interface (not requested, listed for bounds)

### 5.1 `IMTDeal::EnDealReason` (`MT5APIDeal.h` 55–80)

| Constant | Value |
|---|---:|
| `DEAL_REASON_CLIENT` | 0 |
| `DEAL_REASON_EXPERT` | 1 |
| `DEAL_REASON_DEALER` | 2 |
| `DEAL_REASON_SL` | 3 |
| `DEAL_REASON_TP` | 4 |
| `DEAL_REASON_SO` | 5 |
| `DEAL_REASON_ROLLOVER` | 6 |
| `DEAL_REASON_EXTERNAL_CLIENT` | 7 |
| `DEAL_REASON_VMARGIN` | 8 |
| `DEAL_REASON_GATEWAY` | 9 |
| `DEAL_REASON_SIGNAL` | 10 |
| `DEAL_REASON_SETTLEMENT` | 11 |
| `DEAL_REASON_TRANSFER` | 12 |
| `DEAL_REASON_SYNC` | 13 |
| `DEAL_REASON_EXTERNAL_SERVICE` | 14 |
| `DEAL_REASON_MIGRATION` | 15 |
| `DEAL_REASON_MOBILE` | 16 |
| `DEAL_REASON_WEB` | 17 |
| `DEAL_REASON_SPLIT` | 18 |
| `DEAL_REASON_CORPORATE_ACTION` | 19 |
| `DEAL_REASON_FIRST` | 0 |
| `DEAL_REASON_LAST` | 19 (`DEAL_REASON_CORPORATE_ACTION`) |

PHP/C# WebAPI samples stop at `DEAL_REASON_SPLIT` (18) and omit `DEAL_REASON_CORPORATE_ACTION`.

### 5.2 `IMTDeal::EnTradeModifyFlags` (`MT5APIDeal.h` 82–96)

`MODIFY_FLAGS_ADMIN=0x1`, `MANAGER=0x2`, `POSITION=0x4`, `RESTORE=0x8`, `API_ADMIN=0x10`, `API_MANAGER=0x20`, `API_SERVER=0x40`, `API_GATEWAY=0x80`, `NONE=0`, `ALL=` OR of the eight bits.

---

## 6. Copy-paste constants for integrators

Use these numbers only; they are the C++ Manager SDK values.

```text
# EnDealAction (IMTDeal::Action)
DEAL_BUY=0  DEAL_SELL=1  DEAL_BALANCE=2  DEAL_CREDIT=3  DEAL_CHARGE=4
DEAL_CORRECTION=5  DEAL_BONUS=6  DEAL_COMMISSION=7
DEAL_COMMISSION_DAILY=8  DEAL_COMMISSION_MONTHLY=9
DEAL_AGENT_DAILY=10  DEAL_AGENT_MONTHLY=11  DEAL_INTERESTRATE=12
DEAL_BUY_CANCELED=13  DEAL_SELL_CANCELED=14
DEAL_DIVIDEND=15  DEAL_DIVIDEND_FRANKED=16  DEAL_TAX=17  DEAL_AGENT=18
DEAL_SO_COMPENSATION=19  DEAL_SO_COMPENSATION_CREDIT=20
DEAL_FIRST=0  DEAL_LAST=20

# EnDealEntry (IMTDeal::Entry)  — NOT named DEAL_ENTRY_* in this SDK
ENTRY_IN=0          # MQL5: DEAL_ENTRY_IN
ENTRY_OUT=1         # MQL5: DEAL_ENTRY_OUT
ENTRY_INOUT=2       # MQL5: DEAL_ENTRY_INOUT
ENTRY_OUT_BY=3      # MQL5: DEAL_ENTRY_OUT_BY
ENTRY_FIRST=0  ENTRY_LAST=3

# Volume scale
MTAPI_VOLUME_DIV=10000.0            # Volume() lots = n / 10000
MTAPI_VOLUME_DIGITS=4
MTAPI_VOLUME_EXT_DIV=100000000.0    # VolumeExt() lots = n / 100000000
MTAPI_VOLUME_EXT_DIGITS=8
# VolumeExt = Volume * 10000 ; Volume = VolumeExt / 10000
```

---

## 7. Findings that affect mapping

1. **Do not search this SDK for `DEAL_ENTRY_IN`.** The Manager enum is `IMTDeal::EnDealEntry` / `ENTRY_IN|OUT|INOUT|OUT_BY`. `DEAL_ENTRY` in this tree is a **dataset field/type name**, not an enumerator prefix.
2. **Prefer `VolumeExt` / `FIELD_DEAL_VOLUME_EXT`** for new code (8 decimal digits). Convert with `/ 100000000.0` or `SMTMath::VolumeExtToDouble`.
3. **Never treat `Volume()` and `VolumeExt()` as interchangeable.** Off-by-10 000 if the wrong divisor is used.
4. **`DEAL_LAST` is 20** in the installed C++ header (`DEAL_SO_COMPENSATION_CREDIT`). Older PHP/C# samples still say 19.
5. **`ENTRY_STATE=255` is WebAPI-only.** Not in `IMTDeal::EnDealEntry`.
6. `FormatDealAction` does not label action 20.

**Product source was not modified.** This file is the only write from A37.
