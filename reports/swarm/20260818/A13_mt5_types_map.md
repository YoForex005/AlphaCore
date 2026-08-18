# A13 — `mt5_types.h` binding map (C# DTOs)

**Status:** BINDING for C# DTOs. Do not invent extra fields, rename keys, or rescale volumes.  
**Source of truth:** `D:\Prop\mt5-sdk\src\core\mt5_types.h` (read in full, 572 lines).  
**Populators:** `MT5Manager::extract*` / `GetSymbol*` / `GetTick*` / `GetGroupDetails` in `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`.  
**Official enum / volume law:** MetaTrader 5 Manager SDK headers under `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\`.  
**Product source:** not modified. This file is documentation only.

Wire JSON is produced by the nlohmann `to_json` / `from_json` adapters in `mt5_types.h`. Keys are **snake_case literals**, not camelCase. C# DTOs **must** bind with `[JsonPropertyName("…")]` (or equivalent) to those exact keys. Do not rely on default System.Text.Json camelCase of PascalCase property names.

C++ → C# integer / float map (use these and only these):

| C++ | C# | JSON number shape |
|---|---|---|
| `uint64_t` | `ulong` | unsigned 64-bit integer |
| `uint32_t` | `uint` | unsigned 32-bit integer |
| `int64_t` | `long` | signed 64-bit integer |
| `int32_t` | `int` | signed 32-bit integer |
| `double` | `double` | IEEE-754; money and prices are raw doubles |
| `bool` | `bool` | JSON boolean |
| `std::string` | `string` | UTF-8 |

Missing JSON keys deserialize to the C++ `from_json` default shown below. C# should use the same defaults.

---

## 1. Volume units (BINDING — do not follow the “hundredths” comment)

### 1.1 What the comment says

`PositionData::volume` is the **only** volume field with an in-file comment:

```cpp
uint64_t volume = 0;  // in hundredths of lots
```

`DealData::volume`, `OrderData::volume`, `SymbolData::{volume_min,volume_max,volume_step}`, `TickData::volume`, and `MT5TradeRequest::volume` have **no** “hundredths” comment. `MT5TradeRequest` says “MT5 native integer units (see `MT5_VOLUME_PER_LOT` in `trade_execution_service.cpp`)”. That constant is **not** defined in `mt5_types.h` and is not present under `D:\Prop\mt5-sdk\src`.

### 1.2 What the code actually stores

Extractors copy **raw Manager-API `Volume()` integers** with no rescale:

| Struct field | SDK source | Method |
|---|---|---|
| `PositionData.volume` | `IMTPosition::Volume()` | current position volume |
| `DealData.volume` | `IMTDeal::Volume()` | deal volume |
| `OrderData.volume` | `IMTOrder::VolumeInitial()` | **initial** volume, not remaining |
| `SymbolData.volume_min/max/step` | `IMTConSymbol::VolumeMin/Max/Step()` | symbol limits |
| `TickData.volume` | `MTTickShort::volume` / `MTTick::volume` | last-trade volume (live bridge only) |
| `MT5TradeRequest.volume` | written to `IMTRequest::Volume()` | same integer units |

This codebase does **not** call `VolumeExt()` / `VolumeMinExt()`.

### 1.3 Official scale (`MT5APIMath.h`)

```
MTAPI_VOLUME_DIV    = 10000.0
MTAPI_VOLUME_DIGITS = 4
lots = Volume / 10000.0
Volume = lots * 10000
```

That is **ten-thousandths of a lot**, not hundredths.

| Lots | Integer `volume` |
|---:|---:|
| 1.00 | 10 000 |
| 0.10 | 1 000 |
| 0.01 | 100 |
| 0.001 | 10 |
| 0.0001 | 1 |

`VolumeExt` (not used here) is 1 lot = 100 000 000 (`MTAPI_VOLUME_EXT_DIV`).

### 1.4 Binding rule for C# DTOs

1. Keep every `volume*` field as `ulong`. Do **not** convert to `decimal` lots on the DTO.
2. Display / domain lots = `volume / 10000.0`. Never `/ 100.0`.
3. Treat the comment `// in hundredths of lots` as **wrong**. Following it makes every size 100× too large.
4. `TickData.volume` uses the same 1 lot = 10 000 scale when populated. `GetTickLast` / `GetAllTicksLast` in `mt5_manager.cpp` / `mt5_pool.cpp` **do not copy** `volume` (it stays `0`). Only `MT5TickBridge::onSdkTick` assigns `td.volume = tick.volume`.

---

## 2. Deal action / entry enums (BINDING)

`DealData.action` and `DealData.entry` are raw `uint32_t` copies of `IMTDeal::Action()` / `IMTDeal::Entry()`. C# must store them as `uint` (or an enum with these exact numeric values). Source: `MT5APIDeal.h` `IMTDeal::EnDealAction` / `EnDealEntry`.

The in-file comment is incomplete: `// 0=BUY, 1=SELL, 2=BALANCE, etc.` and `// 0=IN, 1=OUT, 2=INOUT, 3=OUT_BY`. Full official table:

### 2.1 `DealData.action` — `IMTDeal::EnDealAction`

| Value | SDK name | Meaning |
|---:|---|---|
| 0 | `DEAL_BUY` | buy |
| 1 | `DEAL_SELL` | sell |
| 2 | `DEAL_BALANCE` | deposit / withdrawal (balance operation) |
| 3 | `DEAL_CREDIT` | credit operation |
| 4 | `DEAL_CHARGE` | additional charges |
| 5 | `DEAL_CORRECTION` | correction |
| 6 | `DEAL_BONUS` | bonus |
| 7 | `DEAL_COMMISSION` | commission |
| 8 | `DEAL_COMMISSION_DAILY` | daily commission |
| 9 | `DEAL_COMMISSION_MONTHLY` | monthly commission |
| 10 | `DEAL_AGENT_DAILY` | daily agent commission |
| 11 | `DEAL_AGENT_MONTHLY` | monthly agent commission |
| 12 | `DEAL_INTERESTRATE` | interest-rate charges |
| 13 | `DEAL_BUY_CANCELED` | canceled buy deal |
| 14 | `DEAL_SELL_CANCELED` | canceled sell deal |
| 15 | `DEAL_DIVIDEND` | dividend |
| 16 | `DEAL_DIVIDEND_FRANKED` | franked dividend |
| 17 | `DEAL_TAX` | taxes |
| 18 | `DEAL_AGENT` | instant agent commission |
| 19 | `DEAL_SO_COMPENSATION` | negative-balance compensation after stop-out |
| 20 | `DEAL_SO_COMPENSATION_CREDIT` | credit compensation after stop-out |

Borders: `DEAL_FIRST = 0`, `DEAL_LAST = 20`. Values outside 0–20 are illegal / future.

C# name suggestion (numeric values frozen):

```csharp
public enum Mt5DealAction : uint
{
    Buy = 0,
    Sell = 1,
    Balance = 2,
    Credit = 3,
    Charge = 4,
    Correction = 5,
    Bonus = 6,
    Commission = 7,
    CommissionDaily = 8,
    CommissionMonthly = 9,
    AgentDaily = 10,
    AgentMonthly = 11,
    InterestRate = 12,
    BuyCanceled = 13,
    SellCanceled = 14,
    Dividend = 15,
    DividendFranked = 16,
    Tax = 17,
    Agent = 18,
    SoCompensation = 19,
    SoCompensationCredit = 20,
}
```

### 2.2 `DealData.entry` — `IMTDeal::EnDealEntry`

| Value | SDK name | Meaning |
|---:|---|---|
| 0 | `ENTRY_IN` | in market (open) |
| 1 | `ENTRY_OUT` | out of market (close) |
| 2 | `ENTRY_INOUT` | reverse |
| 3 | `ENTRY_OUT_BY` | closed by a hedged position |

Borders: `ENTRY_FIRST = 0`, `ENTRY_LAST = 3`.

```csharp
public enum Mt5DealEntry : uint
{
    In = 0,
    Out = 1,
    InOut = 2,
    OutBy = 3,
}
```

Balance / credit / commission deals typically have `entry = 0` and empty `symbol`. Do not assume every deal is a market fill.

---

## 3. Related enums carried by the same structs

These are not named in `mt5_types.h` but the integer fields are the official enums.

### 3.1 `PositionData.action` — `IMTPosition::EnPositionAction`

Comment in header: `// 0=BUY, 1=SELL`. Matches SDK.

| Value | SDK name |
|---:|---|
| 0 | `POSITION_BUY` |
| 1 | `POSITION_SELL` |

### 3.2 `OrderData.type` — `IMTOrder::EnOrderType` (`mt5_op::*` mirrors 0–5 only)

| Value | `mt5_op` / SDK | In `mt5_op` namespace? |
|---:|---|---|
| 0 | `BUY` / `OP_BUY` | yes |
| 1 | `SELL` / `OP_SELL` | yes |
| 2 | `BUY_LIMIT` / `OP_BUY_LIMIT` | yes |
| 3 | `SELL_LIMIT` / `OP_SELL_LIMIT` | yes |
| 4 | `BUY_STOP` / `OP_BUY_STOP` | yes |
| 5 | `SELL_STOP` / `OP_SELL_STOP` | yes |
| 6 | `OP_BUY_STOP_LIMIT` | **no** — still a legal `OrderData.type` |
| 7 | `OP_SELL_STOP_LIMIT` | **no** |
| 8 | `OP_CLOSE_BY` | **no** |

C# order-type DTO must include 6–8. Do not stop at `mt5_op`.

### 3.3 `OrderData.state` — `IMTOrder::EnOrderState`

| Value | SDK name |
|---:|---|
| 0 | `ORDER_STATE_STARTED` |
| 1 | `ORDER_STATE_PLACED` |
| 2 | `ORDER_STATE_CANCELED` |
| 3 | `ORDER_STATE_PARTIAL` |
| 4 | `ORDER_STATE_FILLED` |
| 5 | `ORDER_STATE_REJECTED` |
| 6 | `ORDER_STATE_EXPIRED` |
| 7 | `ORDER_STATE_REQUEST_ADD` |
| 8 | `ORDER_STATE_REQUEST_MODIFY` |
| 9 | `ORDER_STATE_REQUEST_CANCEL` |

### 3.4 `SymbolData.trade_mode` — `IMTConSymbol::EnTradeMode`

| Value | SDK name |
|---:|---|
| 0 | `TRADE_DISABLED` |
| 1 | `TRADE_LONGONLY` |
| 2 | `TRADE_SHORTONLY` |
| 3 | `TRADE_CLOSEONLY` |
| 4 | `TRADE_FULL` |

### 3.5 `TickData.flags`

Live ticks come from `MTTickShort` (`GetTickLast` / tick bridge), so flags are `EnTickShortFlags`:

| Bit | Value | SDK name |
|---|---:|---|
| raw | `0x00000001` | `TICK_SHORT_FLAG_RAW` |
| bid changed | `0x00000002` | `TICK_SHORT_FLAG_BID` |
| ask changed | `0x00000004` | `TICK_SHORT_FLAG_ASK` |
| last changed | `0x00000008` | `TICK_SHORT_FLAG_LAST` |
| volume changed | `0x00000010` | `TICK_SHORT_FLAG_VOLUME` |
| buy | `0x00000020` | `TICK_SHORT_FLAG_BUY` |
| sell | `0x00000040` | `TICK_SHORT_FLAG_SELL` |

`GetAllTicksLast` copies `MTTick::flags`, whose documented bits are only buy=`1` / sell=`2`. Do not assume the two flag layouts are interchangeable.

### 3.6 `UserData.rights` — `IMTUser::EnUsersRights` (bitmask, `ulong`)

| Bit | SDK name |
|---|---|
| `0x0001` | `USER_RIGHT_ENABLED` |
| `0x0002` | `USER_RIGHT_PASSWORD` |
| `0x0004` | `USER_RIGHT_TRADE_DISABLED` |
| `0x0008` | `USER_RIGHT_INVESTOR` |
| `0x0010` | `USER_RIGHT_CONFIRMED` |
| `0x0020` | `USER_RIGHT_TRAILING` |
| `0x0040` | `USER_RIGHT_EXPERT` |
| `0x0100` | `USER_RIGHT_REPORTS` |
| `0x0200` | `USER_RIGHT_READONLY` |
| `0x0400` | `USER_RIGHT_RESET_PASS` |
| `0x0800` | `USER_RIGHT_OTP_ENABLED` |
| `0x2000` | `USER_RIGHT_SPONSORED_HOSTING` |
| `0x4000` | `USER_RIGHT_API_ENABLED` |
| `0x8000` | `USER_RIGHT_PUSH_NOTIFICATION` |
| `0x10000` | `USER_RIGHT_TECHNICAL` |
| `0x20000` | `USER_RIGHT_EXCLUDE_REPORTS` |

Default new-account mask in the SDK: `ENABLED | PASSWORD | TRAILING | EXPERT | REPORTS`.

---

## 4. `UserData`

Populated by `extractUser` (identity / rights from `IMTUser`) plus `GetUser` overlay of `IMTAccount` money fields (`Balance`, `Credit`, `Equity`, `Margin`, `MarginFree`, `MarginLevel`, `Profit`, `Floating`). Sink `UserAdd`/`UserUpdate` events use `extractUser` only — money fields may be `0` on those events.

Has nlohmann JSON. All 19 fields are on the wire.

| C++ field | C++ type | Default | JSON key | C# type | Notes |
|---|---|---|---|---|---|
| `login` | `uint64_t` | `0` | `login` | `ulong` | MT5 login |
| `name` | `std::string` | `""` | `name` | `string` | |
| `email` | `std::string` | `""` | `email` | `string` | |
| `group` | `std::string` | `""` | `group` | `string` | e.g. `real\challenge_phase1_10k` |
| `leverage` | `uint32_t` | `100` | `leverage` | `uint` | JSON default 100 if omitted |
| `balance` | `double` | `0` | `balance` | `double` | deposit currency |
| `credit` | `double` | `0` | `credit` | `double` | |
| `equity` | `double` | `0` | `equity` | `double` | |
| `margin` | `double` | `0` | `margin` | `double` | used margin |
| `margin_free` | `double` | `0` | `margin_free` | `double` | |
| `margin_level` | `double` | `0` | `margin_level` | `double` | percent |
| `profit` | `double` | `0` | `profit` | `double` | |
| `floating` | `double` | `0` | `floating` | `double` | |
| `country` | `std::string` | `""` | `country` | `string` | |
| `city` | `std::string` | `""` | `city` | `string` | |
| `phone` | `std::string` | `""` | `phone` | `string` | |
| `registration` | `uint64_t` | `0` | `registration` | `ulong` | unix seconds (`IMTUser::Registration`) |
| `last_access` | `uint64_t` | `0` | `last_access` | `ulong` | unix seconds (`IMTUser::LastAccess`) |
| `rights` | `uint64_t` | `0` | `rights` | `ulong` | bitmask §3.6 |

Not on this struct: password, investor password, `AccountData.storage`. Those live on `UserParams` / `AccountData` (out of this agent’s named list).

C# DTO skeleton:

```csharp
public sealed class UserDataDto
{
    [JsonPropertyName("login")] public ulong Login { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("email")] public string Email { get; set; } = "";
    [JsonPropertyName("group")] public string Group { get; set; } = "";
    [JsonPropertyName("leverage")] public uint Leverage { get; set; } = 100;
    [JsonPropertyName("balance")] public double Balance { get; set; }
    [JsonPropertyName("credit")] public double Credit { get; set; }
    [JsonPropertyName("equity")] public double Equity { get; set; }
    [JsonPropertyName("margin")] public double Margin { get; set; }
    [JsonPropertyName("margin_free")] public double MarginFree { get; set; }
    [JsonPropertyName("margin_level")] public double MarginLevel { get; set; }
    [JsonPropertyName("profit")] public double Profit { get; set; }
    [JsonPropertyName("floating")] public double Floating { get; set; }
    [JsonPropertyName("country")] public string Country { get; set; } = "";
    [JsonPropertyName("city")] public string City { get; set; } = "";
    [JsonPropertyName("phone")] public string Phone { get; set; } = "";
    [JsonPropertyName("registration")] public ulong Registration { get; set; }
    [JsonPropertyName("last_access")] public ulong LastAccess { get; set; }
    [JsonPropertyName("rights")] public ulong Rights { get; set; }
}
```

---

## 5. `DealData`

Extract: `ticket=Deal()`, `login=Login()`, `order=Order()`, `position=PositionID()`, `symbol`, `action`, `entry`, `volume=Volume()`, `price`, `profit`, `commission`, `storage=Storage()` (swap), `time=Time()`, `comment`.

**Wire gap (BINDING):** `position` is a first-class C++ field and is filled from `IMTDeal::PositionID()`, but **`to_json` / `from_json` omit `position`**. HTTP / nlohmann JSON consumers will not see a `position` key. In-process C++ (`extractDeal`, event queue, recent-deals ring) still has it.

| C++ field | C++ type | Default | JSON key | C# type | Notes |
|---|---|---|---|---|---|
| `ticket` | `uint64_t` | `0` | `ticket` | `ulong` | deal id (`IMTDeal::Deal`) |
| `login` | `uint64_t` | `0` | `login` | `ulong` | |
| `order` | `uint64_t` | `0` | `order` | `ulong` | originating order ticket |
| `position` | `uint64_t` | `0` | **not serialized** | `ulong` | `PositionID`; JSON always default 0 |
| `symbol` | `std::string` | `""` | `symbol` | `string` | empty on balance ops |
| `action` | `uint32_t` | `0` | `action` | `uint` / `Mt5DealAction` | §2.1 |
| `entry` | `uint32_t` | `0` | `entry` | `uint` / `Mt5DealEntry` | §2.2 |
| `volume` | `uint64_t` | `0` | `volume` | `ulong` | 1 lot = 10 000 |
| `price` | `double` | `0` | `price` | `double` | |
| `profit` | `double` | `0` | `profit` | `double` | |
| `commission` | `double` | `0` | `commission` | `double` | |
| `storage` | `double` | `0` | `storage` | `double` | swap |
| `time` | `int64_t` | `0` | `time` | `long` | unix seconds (`IMTDeal::Time`) |
| `comment` | `std::string` | `""` | `comment` | `string` | |

```csharp
public sealed class DealDataDto
{
    [JsonPropertyName("ticket")] public ulong Ticket { get; set; }
    [JsonPropertyName("login")] public ulong Login { get; set; }
    [JsonPropertyName("order")] public ulong Order { get; set; }
    // Position exists in C++ only. Do not expect "position" on JSON.
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    [JsonPropertyName("action")] public uint Action { get; set; }
    [JsonPropertyName("entry")] public uint Entry { get; set; }
    [JsonPropertyName("volume")] public ulong Volume { get; set; }
    [JsonPropertyName("price")] public double Price { get; set; }
    [JsonPropertyName("profit")] public double Profit { get; set; }
    [JsonPropertyName("commission")] public double Commission { get; set; }
    [JsonPropertyName("storage")] public double Storage { get; set; }
    [JsonPropertyName("time")] public long Time { get; set; }
    [JsonPropertyName("comment")] public string Comment { get; set; } = "";
}
```

---

## 6. `OrderData`

Extract: `ticket=Order()`, `login`, `symbol`, `type=Type()`, `state=State()`, `volume=VolumeInitial()`, prices, `time_setup=TimeSetup()`, `comment`. Remaining volume (`VolumeCurrent`) is **not** mapped.

| C++ field | C++ type | Default | JSON key | C# type | Notes |
|---|---|---|---|---|---|
| `ticket` | `uint64_t` | `0` | `ticket` | `ulong` | order ticket |
| `login` | `uint64_t` | `0` | `login` | `ulong` | |
| `symbol` | `std::string` | `""` | `symbol` | `string` | |
| `type` | `uint32_t` | `0` | `type` | `uint` | §3.2 `EnOrderType` |
| `state` | `uint32_t` | `0` | `state` | `uint` | §3.3 `EnOrderState` |
| `volume` | `uint64_t` | `0` | `volume` | `ulong` | **initial** volume; 1 lot = 10 000 |
| `price_order` | `double` | `0` | `price_order` | `double` | |
| `price_current` | `double` | `0` | `price_current` | `double` | |
| `price_sl` | `double` | `0` | `price_sl` | `double` | |
| `price_tp` | `double` | `0` | `price_tp` | `double` | |
| `time_setup` | `int64_t` | `0` | `time_setup` | `long` | unix seconds |
| `comment` | `std::string` | `""` | `comment` | `string` | |

Not mapped: expiration, fill policy, `VolumeCurrent`, stop-limit trigger, position id.

```csharp
public sealed class OrderDataDto
{
    [JsonPropertyName("ticket")] public ulong Ticket { get; set; }
    [JsonPropertyName("login")] public ulong Login { get; set; }
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    [JsonPropertyName("type")] public uint Type { get; set; }
    [JsonPropertyName("state")] public uint State { get; set; }
    [JsonPropertyName("volume")] public ulong Volume { get; set; }
    [JsonPropertyName("price_order")] public double PriceOrder { get; set; }
    [JsonPropertyName("price_current")] public double PriceCurrent { get; set; }
    [JsonPropertyName("price_sl")] public double PriceSl { get; set; }
    [JsonPropertyName("price_tp")] public double PriceTp { get; set; }
    [JsonPropertyName("time_setup")] public long TimeSetup { get; set; }
    [JsonPropertyName("comment")] public string Comment { get; set; } = "";
}
```

---

## 7. `PositionData`

Extract: `ticket=Position()`, `login`, `symbol`, `action`, `volume=Volume()`, `price_open`, `price_current`, `price_sl`, `price_tp`, `profit`, `storage=Storage()` (swap), `time_create`, `time_update`, `comment`.

All 14 fields are on the JSON wire.

| C++ field | C++ type | Default | JSON key | C# type | Notes |
|---|---|---|---|---|---|
| `ticket` | `uint64_t` | `0` | `ticket` | `ulong` | position ticket |
| `login` | `uint64_t` | `0` | `login` | `ulong` | |
| `symbol` | `std::string` | `""` | `symbol` | `string` | |
| `action` | `uint32_t` | `0` | `action` | `uint` | 0=BUY, 1=SELL |
| `volume` | `uint64_t` | `0` | `volume` | `ulong` | comment says hundredths; **actual 1 lot = 10 000** |
| `price_open` | `double` | `0` | `price_open` | `double` | |
| `price_current` | `double` | `0` | `price_current` | `double` | |
| `price_sl` | `double` | `0` | `price_sl` | `double` | |
| `price_tp` | `double` | `0` | `price_tp` | `double` | |
| `profit` | `double` | `0` | `profit` | `double` | |
| `storage` | `double` | `0` | `storage` | `double` | swap |
| `time_create` | `int64_t` | `0` | `time_create` | `long` | unix seconds |
| `time_update` | `int64_t` | `0` | `time_update` | `long` | unix seconds |
| `comment` | `std::string` | `""` | `comment` | `string` | |

```csharp
public sealed class PositionDataDto
{
    [JsonPropertyName("ticket")] public ulong Ticket { get; set; }
    [JsonPropertyName("login")] public ulong Login { get; set; }
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    [JsonPropertyName("action")] public uint Action { get; set; }
    [JsonPropertyName("volume")] public ulong Volume { get; set; }
    [JsonPropertyName("price_open")] public double PriceOpen { get; set; }
    [JsonPropertyName("price_current")] public double PriceCurrent { get; set; }
    [JsonPropertyName("price_sl")] public double PriceSl { get; set; }
    [JsonPropertyName("price_tp")] public double PriceTp { get; set; }
    [JsonPropertyName("profit")] public double Profit { get; set; }
    [JsonPropertyName("storage")] public double Storage { get; set; }
    [JsonPropertyName("time_create")] public long TimeCreate { get; set; }
    [JsonPropertyName("time_update")] public long TimeUpdate { get; set; }
    [JsonPropertyName("comment")] public string Comment { get; set; } = "";
}
```

---

## 8. `SymbolData`

Filled by `GetSymbol` / `GetSymbolByName` from `IMTConSymbol`. Has nlohmann JSON.

| C++ field | C++ type | Default | JSON key | C# type | Notes |
|---|---|---|---|---|---|
| `symbol` | `std::string` | `""` | `symbol` | `string` | e.g. `XAUUSD` |
| `path` | `std::string` | `""` | `path` | `string` | tree path |
| `description` | `std::string` | `""` | `description` | `string` | |
| `digits` | `int32_t` | `0` | `digits` | `int` | price digits |
| `contract_size` | `double` | `0` | `contract_size` | `double` | |
| `volume_min` | `uint64_t` | `0` | `volume_min` | `ulong` | 1 lot = 10 000 |
| `volume_max` | `uint64_t` | `0` | `volume_max` | `ulong` | same units |
| `volume_step` | `uint64_t` | `0` | `volume_step` | `ulong` | same units |
| `trade_mode` | `uint32_t` | `0` | `trade_mode` | `uint` | §3.4 |

```csharp
public sealed class SymbolDataDto
{
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    [JsonPropertyName("path")] public string Path { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("digits")] public int Digits { get; set; }
    [JsonPropertyName("contract_size")] public double ContractSize { get; set; }
    [JsonPropertyName("volume_min")] public ulong VolumeMin { get; set; }
    [JsonPropertyName("volume_max")] public ulong VolumeMax { get; set; }
    [JsonPropertyName("volume_step")] public ulong VolumeStep { get; set; }
    [JsonPropertyName("trade_mode")] public uint TradeMode { get; set; }
}
```

---

## 9. `TickData`

Struct comment: `time` / `time_msc` / `flags` mirror `MTTickShort::{datetime,datetime_msc,flags}`.

JSON includes all 8 fields. `from_json`: if `time_msc` is omitted, it is derived as `time * 1000`. `flags` defaults to `0`.

| C++ field | C++ type | Default | JSON key | C# type | Notes |
|---|---|---|---|---|---|
| `symbol` | `std::string` | `""` | `symbol` | `string` | |
| `bid` | `double` | `0` | `bid` | `double` | |
| `ask` | `double` | `0` | `ask` | `double` | |
| `last` | `double` | `0` | `last` | `double` | |
| `volume` | `uint64_t` | `0` | `volume` | `ulong` | last-trade volume, 1 lot = 10 000; often 0 (see §1.4) |
| `time` | `int64_t` | `0` | `time` | `long` | MT5 server unix **seconds** |
| `time_msc` | `int64_t` | `0` | `time_msc` | `long` | MT5 server unix **milliseconds** |
| `flags` | `uint64_t` | `0` | `flags` | `ulong` | §3.5 |

`GetAllTicksLast` skips ticks with `ask <= 0 && bid <= 0`. It copies `volume` **never** — C# seed snapshots from that path have `volume == 0`.

```csharp
public sealed class TickDataDto
{
    [JsonPropertyName("symbol")] public string Symbol { get; set; } = "";
    [JsonPropertyName("bid")] public double Bid { get; set; }
    [JsonPropertyName("ask")] public double Ask { get; set; }
    [JsonPropertyName("last")] public double Last { get; set; }
    [JsonPropertyName("volume")] public ulong Volume { get; set; }
    [JsonPropertyName("time")] public long Time { get; set; }
    [JsonPropertyName("time_msc")] public long TimeMsc { get; set; }
    [JsonPropertyName("flags")] public ulong Flags { get; set; }
}
```

---

## 10. `GroupDetail`

No nlohmann `to_json` / `from_json` in `mt5_types.h`. `MT5HttpClient::GetGroupDetails` is a stub that always returns `false`. Only `MT5Manager::GetGroupDetails` fills this struct from `IMTConGroup`.

If C# ever receives this object, keys **must** be the C++ field names below (recommended JSON, not currently emitted by the adapters).

| C++ field | C++ type | Default | Recommended JSON key | C# type | Notes |
|---|---|---|---|---|---|
| `name` | `std::string` | `""` | `name` | `string` | group name, e.g. `real\challenge_phase1_10k` |
| `currency` | `std::string` | `""` | `currency` | `string` | deposit currency, e.g. `USD` |
| `currency_digits` | `uint32_t` | `2` | `currency_digits` | `uint` | |
| `company` | `std::string` | `""` | `company` | `string` | company label on the group |
| `margin_call` | `double` | `0` | `margin_call` | `double` | margin-call level **percent** |
| `margin_stop_out` | `double` | `0` | `margin_stop_out` | `double` | stop-out level **percent** |
| `connections_allowed` | `bool` | `false` | `connections_allowed` | `bool` | `(PermissionsFlags & 0x00000002) != 0` = `PERMISSION_ENABLE_CONNECTION` |

```csharp
public sealed class GroupDetailDto
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("currency")] public string Currency { get; set; } = "";
    [JsonPropertyName("currency_digits")] public uint CurrencyDigits { get; set; } = 2;
    [JsonPropertyName("company")] public string Company { get; set; } = "";
    [JsonPropertyName("margin_call")] public double MarginCall { get; set; }
    [JsonPropertyName("margin_stop_out")] public double MarginStopOut { get; set; }
    [JsonPropertyName("connections_allowed")] public bool ConnectionsAllowed { get; set; }
}
```

---

## 11. C# DTO law (summary)

1. JSON names are the C++ nlohmann keys. Never PascalCase on the wire.
2. Volumes stay `ulong` at 1 lot = **10 000**. The “hundredths of lots” comment is not law.
3. `DealData.action` / `entry` use the full official tables (§2), not just BUY/SELL/BALANCE.
4. `DealData.position` is C++-only until someone adds it to `to_json` (product source is not changed by this agent).
5. `GroupDetail` has no JSON adapter today. Do not pretend HTTP already returns it.
6. Times: `UserData.registration` / `last_access` are `ulong` unix seconds; deal/order/position/tick `time*` are `long` unix seconds (tick also has `time_msc`).
7. `storage` is swap on positions and deals.
8. `OrderData.volume` is initial volume, not remaining.
9. Do not add fields that are not on these structs (expiration, `VolumeCurrent`, `VolumeExt`, reason, digits, dealer, …) to the **wire** DTO. Domain models may join them later from other APIs.

This mapping is binding. C# DTOs that diverge are wrong.
